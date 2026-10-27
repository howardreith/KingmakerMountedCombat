using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace KingmakerMountedCombat.Diagnostics
{
    // Private, request-only measurement equipment. Mono has no unregister path
    // for this allocation callback: retain the library and profiler until the
    // guarded disposable game process exits before Mods restoration.
    internal sealed class F11NativeAllocationProbe
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct Snapshot
        {
            internal ulong Callbacks, ObjectBytes, Overflow, Reentrant, ForeignThread;
            internal uint WindowThreadId, Active;
        }
        internal struct WindowResult
        {
            internal Snapshot Snapshot;
            internal int CloseStatus; // 1 settled immediately, 2 drained before the bounded Read.
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int StartDelegate([MarshalAs(UnmanagedType.LPWStr)] string expectedMonoPath);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int BeginDelegate();
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int EndDelegate();
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int ReadDelegate(out Snapshot snapshot);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string moduleName);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint GetModuleFileName(IntPtr module, StringBuilder path, uint capacity);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string path);
        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr module, string name);
        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        private static F11NativeAllocationProbe current;
        private static bool attempted;
        private readonly IntPtr retainedModule;
        private readonly BeginDelegate begin;
        private readonly EndDelegate end;
        private readonly ReadDelegate read;
        private readonly uint updateThreadId;
        private bool open;
        private Snapshot handshake;

        private F11NativeAllocationProbe(IntPtr retainedModule, BeginDelegate begin,
            EndDelegate end, ReadDelegate read)
        {
            this.retainedModule = retainedModule;
            this.begin = begin; this.end = end; this.read = read;
            updateThreadId = GetCurrentThreadId();
        }

        internal static F11NativeAllocationProbe Attach(F11NativeAllocationSelection selection, string runId, string evidenceRoot)
        {
            if (selection == null) return null; // Entire native boundary is inert without the closed request.
            if (attempted) throw new InvalidOperationException("F11 native profiler installation was already attempted in this process.");
            foreach (var error in selection.Validate(runId, evidenceRoot)) throw new InvalidOperationException(error);
            var path = selection.Path;
            if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0 ||
                !string.Equals(Hash(path), F11NativeAllocationSelection.HelperSha256, StringComparison.Ordinal))
                throw new InvalidOperationException("F11 staged helper bytes or file type differ from the reviewed observer.");
            var mono = GetModuleHandle("mono-2.0-bdwgc.dll");
            if (mono == IntPtr.Zero ||
                !string.Equals(ModulePath(mono), selection.LoadedMonoPath, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(Hash(selection.LoadedMonoPath), F11NativeAllocationSelection.MonoSha256, StringComparison.Ordinal))
                throw new InvalidOperationException("F11 loaded Mono path or bytes differ from the reviewed installed runtime.");
            attempted = true; // A partial Mono install cannot safely be retried or unloaded.
            var module = LoadLibrary(path);
            if (module == IntPtr.Zero || !string.Equals(ModulePath(module), path, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("F11 native helper did not load from the exact guarded run copy.");
            // Never FreeLibrary: even a failed partial install may retain a Mono callback.
            var start = Export<StartDelegate>(module, "KmcF11Start");
            var begin = Export<BeginDelegate>(module, "KmcF11BeginWindow");
            var end = Export<EndDelegate>(module, "KmcF11EndWindow");
            var read = Export<ReadDelegate>(module, "KmcF11Read");
            var status = start(selection.LoadedMonoPath);
            if (status != 1) throw new InvalidOperationException("F11 native profiler start refused: " + status);
            current = new F11NativeAllocationProbe(module, begin, end, read);
            current.VerifyCallback();
            return current;
        }

        // Prove callbacks are delivered inside this actual game process before
        // taking any workload measurement. This window is outside all samples.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void VerifyCallback()
        {
            BeginUpdate();
            var knownAllocation = new byte[256];
            GC.KeepAlive(knownAllocation);
            handshake = EndUpdate().Snapshot;
            if (handshake.Callbacks == 0 || handshake.ObjectBytes == 0)
                throw new InvalidOperationException("F11 in-process managed allocation callback handshake was silent.");
        }

        private static T Export<T>(IntPtr module, string name) where T : class
        {
            var address = GetProcAddress(module, name);
            if (address == IntPtr.Zero) throw new InvalidOperationException("F11 native helper lacks " + name + ".");
            return (T)(object)Marshal.GetDelegateForFunctionPointer(address, typeof(T));
        }

        private static string ModulePath(IntPtr module)
        {
            var value = new StringBuilder(32768);
            var length = GetModuleFileName(module, value, (uint)value.Capacity);
            if (length == 0 || length >= value.Capacity) throw new InvalidOperationException("Native module path is unavailable or truncated.");
            return value.ToString();
        }

        private static string Hash(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }

        internal uint UpdateThreadId => updateThreadId;
        internal ulong HandshakeCallbacks => handshake.Callbacks;
        internal ulong HandshakeObjectBytes => handshake.ObjectBytes;

        internal void BeginUpdate()
        {
            if (retainedModule == IntPtr.Zero || open || GetCurrentThreadId() != updateThreadId || begin() != 1)
                throw new InvalidOperationException("F11 native allocation window refused on the Update thread.");
            open = true;
        }

        internal WindowResult EndUpdate()
        {
            if (!open || GetCurrentThreadId() != updateThreadId)
                throw new InvalidOperationException("F11 native allocation close left its Update thread.");
            var status = end();
            open = false; // End closes the native gate even when callbacks still drain.
            if (status != 1 && status != 2) throw new InvalidOperationException("F11 native allocation close refused: " + status);
            Snapshot snapshot;
            var deadline = Stopwatch.GetTimestamp() + Stopwatch.Frequency / 500; // bounded 2 ms
            while (read(out snapshot) != 1)
            {
                if (Stopwatch.GetTimestamp() >= deadline)
                    throw new InvalidOperationException("F11 native allocation callbacks did not drain within 2 ms.");
                Thread.SpinWait(32);
            }
            if (snapshot.Active != 0 || snapshot.WindowThreadId != updateThreadId ||
                snapshot.Overflow != 0 || snapshot.Reentrant != 0)
                throw new InvalidOperationException("F11 native allocation snapshot is incomplete or overflowed.");
            return new WindowResult { Snapshot = snapshot, CloseStatus = status };
        }
    }
}
