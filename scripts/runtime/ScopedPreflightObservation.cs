using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace KingmakerMountedCombat.RuntimeTooling
{
    // A bounded, read-only observation lease. This does not authorize native work,
    // cache a historical manifest, or observe paths outside the declared scopes.
    public sealed class ScopedPreflightObservation : IDisposable
    {
        public sealed class Entry
        {
            public string Path { get; internal set; }
            public string Kind { get; internal set; }
            public long Length { get; internal set; }
            public long LastWriteTimeUtcTicks { get; internal set; }
            public string Sha256 { get; internal set; }
        }
        private sealed class Scope
        {
            internal string Path;
            internal bool Tree;
            internal bool Required;
            internal bool MetadataOnly;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct FileIdentity
        {
            internal uint Attributes;
            internal System.Runtime.InteropServices.ComTypes.FILETIME Creation;
            internal System.Runtime.InteropServices.ComTypes.FILETIME Access;
            internal System.Runtime.InteropServices.ComTypes.FILETIME Write;
            internal uint Volume, SizeHigh, SizeLow, Links, IndexHigh, IndexLow;
        }
        [DllImport("kernel32.dll", EntryPoint = "CreateFileW", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern SafeFileHandle CreateFileW(string name, uint access, uint share, IntPtr security, uint disposition, uint flags, IntPtr template);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetFileInformationByHandle(SafeFileHandle handle, out FileIdentity information);
        private readonly Dictionary<string, SafeFileHandle> directories = new Dictionary<string, SafeFileHandle>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> directoryIdentities = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> identities = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly List<Scope> scopes = new List<Scope>();
        private readonly Dictionary<string, FileStream> held = new Dictionary<string, FileStream>(StringComparer.OrdinalIgnoreCase);
        private sealed class PlatformPin
        {
            internal string Identity, Sha256;
            internal uint Links;
            internal long Length;
        }
        public sealed class PlatformIdentity
        {
            public string NativeIdentity { get; internal set; }
            public uint LinkCount { get; internal set; }
            public long Length { get; internal set; }
        }
        private readonly Dictionary<string, PlatformPin> platformPins = new Dictionary<string, PlatformPin>(StringComparer.OrdinalIgnoreCase);
        private readonly int maximumEntries;
        private readonly long maximumBytes;
        private readonly int maximumMilliseconds;
        private List<Entry> before;
        private bool disposed;
        private bool sealedSet;
        private long bytesRead;

        public ScopedPreflightObservation(int maximumEntries, long maximumBytes, int maximumMilliseconds)
        {
            if (maximumEntries < 1 || maximumEntries > 32768 || maximumBytes < 1 || maximumBytes > 8589934592L ||
                maximumMilliseconds < 1 || maximumMilliseconds > 120000)
                throw new ArgumentOutOfRangeException("Bounds must be explicit and finite.");
            this.maximumEntries = maximumEntries;
            this.maximumBytes = maximumBytes;
            this.maximumMilliseconds = maximumMilliseconds;
        }

        public long BytesRead { get { return bytesRead; } }
        public int HeldFileCount { get { return held.Count; } }
        public int HeldDirectoryCount { get { return directories.Count; } }
        public Entry[] Before { get { RequireSealed(); return before.Select(Clone).ToArray(); } }
        public void AddFile(string path) { Add(path, false, true); }
        public void AddOptionalFile(string path) { Add(path, false, false); }
        public void AddTree(string path) { Add(path, true, true); }
        public void AddOptionalTree(string path) { Add(path, true, false); }
        // Git enumerates ignored/untracked namespaces without consuming every byte.
        // Hold exact file objects and names/metadata, but do not invent a byte hash.
        public void AddNamespaceTree(string path) { Add(path, true, true, true); }

        // Platform code can have OS-owned hardlinks. This is a separate exact
        // pin, never a tree-wide exception. The caller must admit only reviewed
        // platform roles; ordinary source/data AddFile and AddTree stay strict.
        public void AddPlatformFile(string path, string nativeIdentity, uint linkCount, long length, string sha256)
        {
            if (nativeIdentity == null || nativeIdentity.Length != 25 || nativeIdentity[8] != ':' ||
                nativeIdentity.Where((c, i) => i != 8).Any(c => !(c >= '0' && c <= '9' || c >= 'a' && c <= 'f')) ||
                linkCount < 1 || linkCount > 1024 || length < 1 || length > maximumBytes ||
                sha256 == null || sha256.Length != 64 || sha256.Any(c => !(c >= '0' && c <= '9' || c >= 'a' && c <= 'f')))
                throw new ArgumentException("Exact platform path, native identity, link count, length and SHA-256 are required.");
            string full = ExactPath(path);
            Add(full, false, true);
            platformPins.Add(full, new PlatformPin { Identity = nativeIdentity, Links = linkCount, Length = length, Sha256 = sha256 });
        }
        // A metadata observation for constructing a separately reviewed platform
        // pin; this return value alone does not admit any later consumer.
        public static PlatformIdentity ReadPlatformIdentity(string path)
        {
            string full = ExactPath(path);
            using (var stream = new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                FileIdentity identity;
                if (!GetFileInformationByHandle(stream.SafeFileHandle, out identity) ||
                    (identity.Attributes & (uint)FileAttributes.ReparsePoint) != 0 || identity.Links < 1)
                    throw new IOException("Cannot observe platform file identity.");
                return new PlatformIdentity { NativeIdentity = Identity(stream.SafeFileHandle, true, identity.Links),
                    LinkCount = identity.Links, Length = stream.Length };
            }
        }

        private void Add(string path, bool tree, bool required, bool metadataOnly = false)
        {
            if (disposed || sealedSet) throw new InvalidOperationException("Observation scope is already sealed or disposed.");
            string full = ExactPath(path);
            if (scopes.Any(x => string.Equals(x.Path, full, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Duplicate observation scope: " + full);
            scopes.Add(new Scope { Path = full, Tree = tree, Required = required, MetadataOnly = metadataOnly });
        }

        public void Seal()
        {
            if (disposed || sealedSet || scopes.Count == 0) throw new InvalidOperationException("Observation cannot be sealed.");
            // Any successfully opened handles remain owned on failure. Dispose is
            // the caller's mandatory final step even when admission is incomplete.
            var captured = Capture(true);
            VerifyDirectoryIdentities();
            before = captured;
            sealedSet = true;
        }

        public void RequirePinnedFile(string path, long length, string sha256)
        {
            RequireSealed();
            if (length < 0 || sha256 == null || sha256.Length != 64 || sha256.Any(c => !(c >= '0' && c <= '9' || c >= 'a' && c <= 'f')))
                throw new ArgumentException("Expected file pin is malformed.");
            string full = ExactPath(path);
            Entry entry = before.SingleOrDefault(x => string.Equals(x.Path, full, StringComparison.Ordinal));
            if (entry == null || entry.Kind != "file" || entry.Length != length || entry.Sha256 != sha256)
                throw new IOException("Observed file differs from its immutable pin: " + full);
        }

        public Entry[] VerifyUnchanged()
        {
            RequireSealed();
            VerifyDirectoryIdentities();
            var after = Capture(false);
            VerifyDirectoryIdentities();
            if (before.Count != after.Count) throw new InvalidOperationException("Observed dependency entry set changed.");
            for (int index = 0; index < before.Count; index++)
            {
                Entry left = before[index], right = after[index];
                if (!string.Equals(left.Path, right.Path, StringComparison.Ordinal) || left.Kind != right.Kind ||
                    left.Length != right.Length || left.LastWriteTimeUtcTicks != right.LastWriteTimeUtcTicks || left.Sha256 != right.Sha256)
                    throw new InvalidOperationException("Observed dependency changed: " + left.Path);
            }
            return after.Select(Clone).ToArray();
        }

        private List<Entry> Capture(bool acquire)
        {
            var clock = Stopwatch.StartNew();
            var found = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
            foreach (var scope in scopes)
            {
                CheckAncestors(scope.Path);
                bool isDirectory = Directory.Exists(scope.Path), isFile = File.Exists(scope.Path);
                if (!isDirectory && !isFile)
                {
                    if (scope.Required) throw new IOException("Observed dependency is missing: " + scope.Path);
                    AddEntry(found, new Entry { Path = scope.Path, Kind = scope.Tree ? "missing-directory" : "missing-file" });
                    continue;
                }
                if (scope.Tree != isDirectory) throw new IOException("Observed dependency kind changed: " + scope.Path);
                if (scope.Tree)
                {
                    var pending = new Queue<string>(); pending.Enqueue(scope.Path);
                    while (pending.Count != 0)
                    {
                        CheckTime(clock);
                        string current = pending.Dequeue();
                        CheckAncestors(current);
                        // Directory timestamps are intentionally omitted: file-byte
                        // and path-set observations do not claim a global instant.
                        AddEntry(found, new Entry { Path = current, Kind = "directory" });
                        foreach (string item in Directory.EnumerateFileSystemEntries(current))
                        {
                            FileAttributes attributes = File.GetAttributes(item);
                            if ((attributes & FileAttributes.ReparsePoint) != 0) throw new IOException("Reparse dependency refused: " + item);
                            if ((attributes & FileAttributes.Directory) != 0)
                            {
                                if (pending.Count + found.Count >= maximumEntries) throw new IOException("Observation entry bound exceeded.");
                                pending.Enqueue(item);
                            }
                            else AddFileEntry(found, item, acquire, clock, scope.MetadataOnly);
                        }
                    }
                }
                else AddFileEntry(found, scope.Path, acquire, clock);
            }
            return found.Values.OrderBy(x => x.Path, StringComparer.Ordinal).ToList();
        }

        private void AddFileEntry(Dictionary<string, Entry> found, string path, bool acquire, Stopwatch clock, bool metadataOnly = false)
        {
            string full = ExactPath(path);
            if (found.ContainsKey(full)) return; // explicit overlapping read scopes share one handle
            CheckAncestors(full); CheckTime(clock);
            var info = new FileInfo(full);
            if (!info.Exists) throw new IOException("Observed dependency is missing: " + full);
            if (info.Length > maximumBytes - found.Values.Sum(x => x.Length)) throw new IOException("Observation byte bound exceeded.");
            PlatformPin platform;
            platformPins.TryGetValue(full, out platform);
            uint expectedLinks = platform == null ? 1U : platform.Links;
            FileStream stream;
            if (!held.TryGetValue(full, out stream))
            {
                if (!acquire) throw new IOException("New observed file appeared: " + full);
                stream = new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, FileOptions.SequentialScan);
                held.Add(full, stream);
                identities.Add(full, Identity(stream.SafeFileHandle, true, expectedLinks));
            }
            using (var pathProbe = new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.Read))
                if (Identity(pathProbe.SafeFileHandle, true, expectedLinks) != identities[full] ||
                    (platform != null && identities[full] != platform.Identity))
                    throw new IOException("Observed dependency object was replaced: " + full);
            long length = stream.Length;
            if (length != info.Length) throw new IOException("Observed dependency changed during admission: " + full);
            if (metadataOnly)
            {
                // Native identity is checked on every capture above. The held
                // FileShare.Read lease also denies content mutation by aliases.
                info.Refresh();
                if (stream.Length != length || info.Length != length) throw new IOException("Observed metadata changed during admission: " + full);
                AddEntry(found, new Entry { Path = full, Kind = "file-metadata", Length = length, LastWriteTimeUtcTicks = info.LastWriteTimeUtc.Ticks });
                return;
            }
            stream.Position = 0;
            string digest;
            using (var algorithm = SHA256.Create())
            {
                byte[] buffer = new byte[65536]; int count;
                while ((count = stream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    CheckTime(clock); bytesRead += count;
                    algorithm.TransformBlock(buffer, 0, count, buffer, 0);
                }
                algorithm.TransformFinalBlock(new byte[0], 0, 0);
                digest = BitConverter.ToString(algorithm.Hash).Replace("-", "").ToLowerInvariant();
            }
            if (platform != null && (length != platform.Length || digest != platform.Sha256))
                throw new IOException("Platform dependency differs from its immutable pin: " + full);
            info.Refresh();
            if (stream.Length != length || info.Length != length) throw new IOException("Observed dependency changed during hashing: " + full);
            AddEntry(found, new Entry { Path = full, Kind = "file", Length = length, LastWriteTimeUtcTicks = info.LastWriteTimeUtc.Ticks, Sha256 = digest });
        }

        private void AddEntry(Dictionary<string, Entry> found, Entry entry)
        {
            Entry existing;
            if (found.TryGetValue(entry.Path, out existing))
            {
                if (existing.Kind != entry.Kind) throw new IOException("Ambiguous observed dependency: " + entry.Path);
                return;
            }
            if (found.Count >= maximumEntries) throw new IOException("Observation entry bound exceeded.");
            found.Add(entry.Path, entry);
        }
        private void CheckTime(Stopwatch clock)
        {
            if (clock.ElapsedMilliseconds > maximumMilliseconds) throw new IOException("Observation time bound exceeded.");
        }
        private void RequireSealed()
        {
            if (disposed || !sealedSet) throw new InvalidOperationException("No active sealed observation.");
        }
        private static Entry Clone(Entry value)
        {
            return new Entry { Path = value.Path, Kind = value.Kind, Length = value.Length,
                LastWriteTimeUtcTicks = value.LastWriteTimeUtcTicks, Sha256 = value.Sha256 };
        }
        private static string ExactPath(string path)
        {
            if (string.IsNullOrEmpty(path) || !System.IO.Path.IsPathRooted(path)) throw new ArgumentException("Observation path must be absolute.");
            if (path.StartsWith(@"\\", StringComparison.Ordinal) || path.Length < 3 || path[1] != ':' ||
                (path[2] != (char)92 && path[2] != '/') || path.IndexOf(':', 2) >= 0)
                throw new ArgumentException("Only literal local file paths are admitted.");
            string full = System.IO.Path.GetFullPath(path).TrimEnd(System.IO.Path.DirectorySeparatorChar);
            if (full.Length < 3) throw new ArgumentException("Volume roots are not bounded observation scopes.");
            return full;
        }
        private void CheckAncestors(string path)
        {
            string current = path;
            while (!string.IsNullOrEmpty(current))
            {
                if (File.Exists(current) || Directory.Exists(current))
                {
                    FileAttributes attributes = File.GetAttributes(current);
                    if ((attributes & FileAttributes.ReparsePoint) != 0)
                        throw new IOException("Reparse dependency ancestor refused: " + current);
                    if ((attributes & FileAttributes.Directory) != 0 && !directories.ContainsKey(current))
                    {
                        // Read attributes; share read/write, deliberately not delete.
                        // Hold every ancestor against rename until all consumers finish.
                        SafeFileHandle handle = CreateFileW(current, 0x80, 0x3, IntPtr.Zero, 3, 0x02200000, IntPtr.Zero);
                        if (handle.IsInvalid) { handle.Dispose(); throw new IOException("Cannot retain dependency directory: " + current, Marshal.GetLastWin32Error()); }
                        directories.Add(current, handle);
                        directoryIdentities.Add(current, Identity(handle, false));
                    }
                }
                string parent = System.IO.Path.GetDirectoryName(current);
                if (string.Equals(parent, current, StringComparison.OrdinalIgnoreCase)) break;
                current = parent;
            }
        }
        private void VerifyDirectoryIdentities()
        {
            // Bind each textual ancestor to the actual retained native object.
            // Do not infer this from successful descendant-file probes or from
            // the usual Windows refusal of an ancestor rename while held.
            foreach (var item in directories)
            {
                string expected;
                if (!directoryIdentities.TryGetValue(item.Key, out expected) ||
                    Identity(item.Value, false) != expected)
                    throw new IOException("Retained directory identity changed: " + item.Key);
                using (SafeFileHandle probe = CreateFileW(item.Key, 0x80, 0x3, IntPtr.Zero, 3, 0x02200000, IntPtr.Zero))
                {
                    if (probe.IsInvalid || Identity(probe, false) != expected)
                        throw new IOException("Observed directory path was replaced: " + item.Key);
                }
            }
        }
        private static string Identity(SafeFileHandle handle, bool file, uint expectedLinks = 1)
        {
            FileIdentity identity;
            if (!GetFileInformationByHandle(handle, out identity))
                throw new IOException("Cannot identify retained filesystem object.", Marshal.GetLastWin32Error());
            if ((identity.Attributes & (uint)FileAttributes.ReparsePoint) != 0 || (file && identity.Links != expectedLinks))
                throw new IOException("Reparse or hardlinked dependency refused.");
            return identity.Volume.ToString("x8") + ":" + identity.IndexHigh.ToString("x8") + identity.IndexLow.ToString("x8");
        }
        public void Dispose()
        {
            if (disposed) return;
            var failures = new List<Exception>();
            foreach (var item in held.ToArray())
            {
                try { item.Value.Dispose(); held.Remove(item.Key); }
                catch (Exception ex) { failures.Add(ex); }
            }
            foreach (var item in directories.ToArray())
            {
                try { item.Value.Dispose(); directories.Remove(item.Key); directoryIdentities.Remove(item.Key); }
                catch (Exception ex) { failures.Add(ex); }
            }
            if (failures.Count != 0) throw new AggregateException("Observation handle release remains incomplete.", failures);
            disposed = true;
        }
    }
}
