using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using KingmakerMountedCombat.Diagnostics;

namespace KingmakerMountedCombat.Tests
{
    internal static class PersistenceSaveAuthorizationTests
    {
        private const string BaselineHash = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        private const string Area = "0123456789abcdef0123456789abcdef";

        public static void Register(TestRunner runner)
        {
            runner.Run("isolated saves authorize exact native types and immutable input contract", NativeTypes);
            runner.Run("isolated saves reject traversal root substitution and foreign identity", ForeignTargets);
            runner.Run("isolated saves reject baseline aliases and protected bytes", Baseline);
            runner.Run("isolated saves reject ambiguous existing and unowned leaves", Existing);
            runner.Run("isolated saves require actual bytes before cold-load admission", ActualBytes);
            runner.Run("isolated saves retain read-only fixture and detect outside mutation", ReadOnly);
            runner.Run("isolated saves reject multiply linked files", HardLinks);
        }

        private static void NativeTypes()
        {
            using (var files = new Files())
            {
                var entries = new[] { Entry("Manual"), Entry("Quick"), Entry("Auto") };
                var guard = files.Guard(entries);
                entries[0].InternalName = "mutated input";
                foreach (var type in new[] { "Manual", "Quick", "Auto" })
                    TestRunner.Equal<string>(null, guard.Validate(RuntimeSaveOperation.Write, files.Target(type), files.Root), type);
                var target = files.Target("Manual");
                target.SaveType = "IronMan";
                Reject(guard, target, files.Root);
            }
        }

        private static void ForeignTargets()
        {
            using (var files = new Files())
            {
                var guard = files.Guard(Entry("Manual"));
                Action<Action<RuntimeSaveTarget>> check = change =>
                {
                    var target = files.Target("Manual"); change(target); Reject(guard, target, files.Root);
                };
                check(t => t.GameId = "foreign");
                check(t => t.Area = "foreign");
                check(t => t.GameName = "foreign");
                check(t => t.FileName = "KMC_unowned.zks");
                check(t => t.InternalName = "KMC_arbitrary");
                check(t => t.FullPath = Path.Combine(files.Root, "..", "Saved Games", t.FileName));
                check(t => t.FullPath += ":stream");
                check(t => t.FullPath = Path.Combine(files.RunRoot, t.FileName));
                Reject(guard, files.Target("Manual"), files.RunRoot);
            }
        }

        private static void Baseline()
        {
            using (var files = new Files())
            {
                var entry = Entry("Manual"); entry.InternalName = "kmc_automation_baseline";
                MustThrow(() => files.Guard(entry));
                entry = Entry("Manual"); entry.FileName = "copy_KMC_AUTOMATION_BASELINE.zks";
                MustThrow(() => files.Guard(entry));
                entry = Entry("Manual"); entry.InitialSha256 = BaselineHash;
                MustThrow(() => files.Guard(entry));
            }
        }

        private static void Existing()
        {
            using (var files = new Files())
            {
                var entry = Entry("Manual");
                File.WriteAllText(Path.Combine(files.Root, entry.FileName), "ambiguous");
                MustThrow(() => files.Guard(entry));
                entry.InitialSha256 = Hash(Path.Combine(files.Root, entry.FileName));
                File.WriteAllText(Path.Combine(files.Root, "unowned.zks"), "foreign");
                MustThrow(() => files.Guard(entry));
                File.Delete(Path.Combine(files.Root, "unowned.zks"));
                MustThrow(() => files.Guard(entry, entry));
            }
        }

        private static void ActualBytes()
        {
            using (var files = new Files())
            {
                var guard = files.Guard(Entry("Manual"));
                var target = files.Target("Manual");
                TestRunner.True(guard.Validate(RuntimeSaveOperation.Load, target, files.Root) != null, "Missing archive loaded.");
                using (var write = guard.BeginWrite(target, files.Root))
                {
                    MustThrow(() => write.Complete());
                    MustThrow(() => guard.BeginWrite(target, files.Root));
                    File.WriteAllText(target.FullPath, "completed native archive surrogate for guard only");
                    TestRunner.True(guard.Validate(RuntimeSaveOperation.Load, target, files.Root) != null,
                        "Partial archive became loadable before commit.");
                    // COMPONENT evidence only; native adapter must observe actual commit.
                    write.Complete();
                    MustThrow(() => write.Complete());
                }
                TestRunner.Equal<string>(null, guard.Validate(RuntimeSaveOperation.Load, target, files.Root), "Completed bytes rejected.");
            }
        }

        private static void ReadOnly()
        {
            using (var files = new Files())
            {
                var target = files.Target("Manual");
                File.WriteAllText(target.FullPath, "fixture");
                var entry = Entry("Manual"); entry.InitialSha256 = Hash(target.FullPath); entry.Writable = false;
                var guard = files.Guard(entry);
                TestRunner.Equal<string>(null, guard.Validate(RuntimeSaveOperation.Load, target, files.Root), "Read-only fixture load failed.");
                Reject(guard, target, files.Root);
                TestRunner.True(guard.Validate(RuntimeSaveOperation.Delete, target, files.Root) != null, "Fixture rotation allowed.");
                File.WriteAllText(target.FullPath, "new external bytes");
                TestRunner.True(guard.Validate(RuntimeSaveOperation.Load, target, files.Root) != null, "External mutation silently admitted.");
            }
        }

        private static void HardLinks()
        {
            using (var files = new Files())
            {
                var target = files.Target("Manual");
                var other = Path.Combine(files.RunRoot, "owned-link-source");
                File.WriteAllText(other, "owned hardlink test");
                if (!CreateHardLink(target.FullPath, other, IntPtr.Zero))
                    throw new InvalidOperationException("Hardlink regression setup failed: " + Marshal.GetLastWin32Error());
                var entry = Entry("Manual"); entry.InitialSha256 = Hash(target.FullPath);
                MustThrow(() => files.Guard(entry));
            }
        }

        private static PersistenceSaveEntry Entry(string type) => new PersistenceSaveEntry
        { FileName = type + "_1_owned.zks", InternalName = "owned " + type, SaveType = type, Area = Area, Writable = true };
        private static void Reject(PersistenceSaveAuthorization guard, RuntimeSaveTarget target, string root)
        { TestRunner.True(guard.Validate(RuntimeSaveOperation.Write, target, root) != null, "Unsafe target accepted."); }
        private static void MustThrow(Action action)
        {
            var thrown = false;
            try { action(); } catch (ArgumentException) { thrown = true; }
            catch (IOException) { thrown = true; } catch (InvalidOperationException) { thrown = true; }
            TestRunner.True(thrown, "Unsafe operation did not fail closed.");
        }
        private static string Hash(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }

        private sealed class Files : IDisposable
        {
            internal readonly string RunRoot;
            internal readonly string Root;
            internal Files()
            {
                var lab = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."));
                RunRoot = Path.Combine(lab, "analysis-cache", "chunk5-guard-tests", Guid.NewGuid().ToString("N"));
                Root = Path.Combine(RunRoot, "Saved Games");
                Directory.CreateDirectory(Root);
            }
            internal PersistenceSaveAuthorization Guard(params PersistenceSaveEntry[] entries) =>
                new PersistenceSaveAuthorization(RunRoot, "campaign", "fixture campaign", BaselineHash, entries);
            internal RuntimeSaveTarget Target(string type) => new RuntimeSaveTarget
            {
                FileName = Entry(type).FileName, FullPath = Path.Combine(Root, Entry(type).FileName),
                InternalName = Entry(type).InternalName, SaveType = type, GameId = "campaign",
                GameName = "fixture campaign", Area = Area
            };
            public void Dispose()
            {
                // Delete only explicit files in this unique owned test directory.
                foreach (var file in Directory.GetFiles(Root)) File.Delete(file);
                Directory.Delete(Root);
                foreach (var file in Directory.GetFiles(RunRoot)) File.Delete(file);
                Directory.Delete(RunRoot);
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CreateHardLink(string path, string existing, IntPtr attributes);
    }
}