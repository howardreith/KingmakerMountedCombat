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
            runner.Run("queued native requests use unique declared slots before actual allocation", QueuedRequests);
            runner.Run("queued request projection cannot launder foreign paths campaign type or area", ProjectionBoundaries);
            runner.Run("queued request projection rejects active read-only and unowned existing destinations", ProjectionOwnership);
            runner.Run("ambiguous queued destinations retain native prediction and strict validation", AmbiguousProjection);
            runner.Run("staging failure cleanup preserves completed saves and rejects substituted bytes", StagingCleanup);
            runner.Run("isolated native slot replacement retains exact committed bytes across rotation", ReplacementCommit);
            runner.Run("failed or canceled owned replacement preserves both complete archives", ReplacementFailure);
            runner.Run("replacement rejects read-only native types aliases and outside-root paths", ReplacementBoundaries);
            runner.Run("isolated saves authorize exact native types and immutable input contract", NativeTypes);
            runner.Run("isolated saves reject traversal root substitution and foreign identity", ForeignTargets);
            runner.Run("isolated saves reject baseline aliases and protected bytes", Baseline);
            runner.Run("isolated saves reject ambiguous existing and unowned leaves", Existing);
            runner.Run("isolated saves require actual bytes before cold-load admission", ActualBytes);
            runner.Run("isolated saves retain read-only fixture and detect outside mutation", ReadOnly);
            runner.Run("isolated saves reject multiply linked files", HardLinks);
            runner.Run("isolated native archive scope revalidates exact source bytes", ReadArchiveScope);
        }

        private static void QueuedRequests()
        {
            using (var files = new Files())
            {
                var entries = Enumerable.Range(0, 3).Select(i => new PersistenceSaveEntry {
                    FileName = "Manual_" + (300 + i) + "_queued_" + i + ".zks", InternalName = "queued " + i,
                    SaveType = "Manual", Area = Area, Writable = true }).ToArray();
                var guard = files.Guard(entries);
                for (var i = 0; i < entries.Length; i++)
                {
                    var predicted = files.Target("Manual");
                    predicted.InternalName = entries[i].InternalName;
                    predicted.FileName = "Manual_300_queued_" + i + ".zks";
                    predicted.FullPath = Path.Combine(files.Root, predicted.FileName);
                    var request = guard.ProjectNewRequest(predicted, files.Root);
                    TestRunner.Equal(entries[i].FileName, request.FileName, "Queue creation guessed another native slot.");
                    TestRunner.True(guard.Validate(RuntimeSaveOperation.Write, request, files.Root) == null, "Exact unallocated request rejected.");
                    TestRunner.True(guard.Validate(RuntimeSaveOperation.Load, request, files.Root) != null, "Projection invented completed bytes.");
                    if (i > 0) MustThrow(() => guard.BeginWrite(predicted, files.Root));
                    using (var lease = guard.BeginWrite(request, files.Root))
                    {
                        File.WriteAllText(request.FullPath, "owned queued complete " + i);
                        lease.Complete();
                    }
                    guard.AssertReadableArchive(request.FullPath);
                }
                TestRunner.Equal(3, Directory.GetFiles(files.Root).Length, "Unexpected queue alias created.");
            }
        }

        private static void ProjectionBoundaries()
        {
            using (var files = new Files())
            {
                var entry = Entry("Manual"); entry.FileName = "Manual_302_owned.zks";
                var guard = files.Guard(entry);
                var target = files.Target("Manual");
                target.FullPath = Path.Combine(files.RunRoot, target.FileName);
                MustThrow(() => guard.ProjectNewRequest(target, files.Root));
                target = files.Target("Manual");
                MustThrow(() => guard.ProjectNewRequest(target, files.RunRoot));
                target.GameId = "another campaign";
                MustThrow(() => guard.ProjectNewRequest(target, files.Root));
                target = files.Target("Manual"); target.InternalName = "KMC_unlisted";
                Reject(guard, guard.ProjectNewRequest(target, files.Root), files.Root);
                target = files.Target("Manual"); target.SaveType = "Quick";
                Reject(guard, guard.ProjectNewRequest(target, files.Root), files.Root);
                target = files.Target("Manual"); target.Area = new string('f', 32);
                Reject(guard, guard.ProjectNewRequest(target, files.Root), files.Root);
            }
        }

        private static void ProjectionOwnership()
        {
            using (var files = new Files())
            {
                var entry = Entry("Manual"); entry.FileName = "Manual_302_owned.zks";
                var guard = files.Guard(entry);
                var predicted = files.Target("Manual");
                var actual = guard.ProjectNewRequest(predicted, files.Root);
                using (guard.BeginWrite(actual, files.Root))
                    Reject(guard, guard.ProjectNewRequest(predicted, files.Root), files.Root);
                File.WriteAllText(actual.FullPath, "unowned outside creation");
                MustThrow(() => guard.ProjectNewRequest(predicted, files.Root));
            }
            using (var files = new Files())
            {
                var entry = Entry("Manual"); entry.FileName = "Manual_302_owned.zks"; entry.Writable = false;
                var guard = files.Guard(entry);
                Reject(guard, guard.ProjectNewRequest(files.Target("Manual"), files.Root), files.Root);
            }
        }

        private static void AmbiguousProjection()
        {
            using (var files = new Files())
            {
                var first = Entry("Manual"); first.FileName = "Manual_301_owned.zks";
                var second = Entry("Manual"); second.FileName = "Manual_302_owned.zks";
                var guard = files.Guard(first, second);
                var native = files.Target("Manual");
                TestRunner.True(ReferenceEquals(native, guard.ProjectNewRequest(native, files.Root)), "Ambiguous request guessed a destination.");
                Reject(guard, native, files.Root);
            }
        }

        private static void StagingCleanup()
        {
            using (var files = new Files())
            {
                var target = files.Target("Manual");
                var guard = files.Guard(Entry("Manual"));
                using (var write = guard.BeginWrite(target, files.Root))
                {
                    write.ClearStaging();
                    File.WriteAllText(target.FullPath, "partial owned archive");
                    write.ClearStaging();
                    TestRunner.True(!File.Exists(target.FullPath), "partial staging remained");
                    File.WriteAllText(target.FullPath, "complete owned archive");
                    write.Complete();
                    MustThrow(() => write.ClearStaging());
                }
                var complete = Hash(target.FullPath);
                using (var write = guard.BeginWrite(target, files.Root))
                    MustThrow(() => write.ClearStaging());
                TestRunner.Equal(complete, Hash(target.FullPath), "completed input lost through initial cleanup");
                File.WriteAllText(target.FullPath, "outside replacement");
                MustThrow(() => guard.DeleteCompletedStaging(target.FullPath));
                File.WriteAllText(target.FullPath, "complete owned archive");
                guard.DeleteCompletedStaging(target.FullPath);
                TestRunner.True(!File.Exists(target.FullPath), "completed staging cleanup failed");
                MustThrow(() => guard.AssertReadableArchive(target.FullPath));
                using (guard.BeginWrite(target, files.Root)) { }
            }
        }

        private static void ReplacementCommit()
        {
            foreach (var type in new[] { "Manual", "Quick", "Auto" })
            using (var files = new Files())
            {
                var original = Entry(type);
                var staged = Entry(type); staged.FileName = type + "_2_owned.zks";
                File.WriteAllText(Path.Combine(files.Root, original.FileName), "previous complete guard surrogate");
                File.WriteAllText(Path.Combine(files.Root, staged.FileName), "next complete guard surrogate");
                original.InitialSha256 = Hash(Path.Combine(files.Root, original.FileName));
                staged.InitialSha256 = Hash(Path.Combine(files.Root, staged.FileName));
                var expected = staged.InitialSha256;
                var guard = files.Guard(original, staged);
                var source = Path.Combine(files.Root, staged.FileName);
                var destination = Path.Combine(files.Root, original.FileName);
                using (var replacement = guard.BeginReplacement(source, destination))
                {
                    MustThrow(() => guard.AssertReadableArchive(destination));
                    MustThrow(() => guard.BeginReplacement(source, destination));
                    File.Replace(source, destination, null);
                    replacement.Complete();
                    MustThrow(() => replacement.Complete());
                }
                TestRunner.Equal(expected, Hash(destination), "exact completed archive became the native slot");
                guard.AssertReadableArchive(destination);
                MustThrow(() => guard.AssertReadableArchive(source));
                var next = files.Target(type); next.FileName = staged.FileName; next.FullPath = source;
                using (var write = guard.BeginWrite(next, files.Root))
                {
                    File.WriteAllText(source, "later completed guard surrogate"); write.Complete();
                }
                using (var replacement = guard.BeginReplacement(source, destination))
                { File.Replace(source, destination, null); replacement.Complete(); }
                guard.AssertReadableArchive(destination);
            }
        }

        private static void ReplacementFailure()
        {
            using (var files = new Files())
            {
                var original = Entry("Manual");
                var staged = Entry("Manual"); staged.FileName = "Manual_2_owned.zks";
                var source = Path.Combine(files.Root, staged.FileName);
                var destination = Path.Combine(files.Root, original.FileName);
                File.WriteAllText(destination, "last good");
                File.WriteAllText(source, "next complete");
                original.InitialSha256 = Hash(destination); staged.InitialSha256 = Hash(source);
                var guard = files.Guard(original, staged);
                using (var replacement = guard.BeginReplacement(source, destination))
                using (var held = new FileStream(destination, FileMode.Open, FileAccess.Read, FileShare.Read))
                    MustThrow(() => File.Replace(source, destination, null));
                TestRunner.Equal(original.InitialSha256, Hash(destination), "failed native replacement preserves last-good bytes");
                TestRunner.Equal(staged.InitialSha256, Hash(source), "failed replacement retains its complete staging bytes");
                guard.AssertReadableArchive(destination); guard.AssertReadableArchive(source);
                // A canceled lease also leaves both prior identities readable.
                using (guard.BeginReplacement(source, destination)) { }
                guard.AssertReadableArchive(destination); guard.AssertReadableArchive(source);
            }
        }

        private static void ReplacementBoundaries()
        {
            using (var files = new Files())
            {
                var original = Entry("Manual"); original.Writable = false;
                var staged = Entry("Manual"); staged.FileName = "Manual_2_owned.zks";
                var foreign = Entry("Quick");
                foreach (var entry in new[] { original, staged, foreign })
                {
                    var path = Path.Combine(files.Root, entry.FileName);
                    File.WriteAllText(path, entry.FileName);
                    entry.InitialSha256 = Hash(path);
                }
                var guard = files.Guard(original, staged, foreign);
                var source = Path.Combine(files.Root, staged.FileName);
                var destination = Path.Combine(files.Root, original.FileName);
                MustThrow(() => guard.BeginReplacement(source, destination));
                MustThrow(() => guard.BeginReplacement(source, Path.Combine(files.Root, foreign.FileName)));
                MustThrow(() => guard.BeginReplacement(source, source));
                MustThrow(() => guard.BeginReplacement(source, Path.Combine(files.RunRoot, original.FileName)));
                MustThrow(() => guard.BeginReplacement(source, Path.Combine(files.Root, "..", "Saved Games", original.FileName)));
                MustThrow(() => guard.BeginReplacement(source, Path.Combine(files.Root, "KMC_unowned.zks")));
                guard.AssertReadableArchive(destination);
            }
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

        private static void ReadArchiveScope()
        {
            using (var files = new Files())
            {
                var target = files.Target("Manual");
                File.WriteAllText(target.FullPath, "owned archive");
                var entry = Entry("Manual"); entry.InitialSha256 = Hash(target.FullPath);
                entry.Writable = false;
                var guard = files.Guard(entry);
                guard.AssertReadableArchive(target.FullPath);
                MustThrow(() => guard.AssertReadableArchive(Path.Combine(files.RunRoot, entry.FileName)));
                File.WriteAllText(target.FullPath, "outside mutation");
                MustThrow(() => guard.AssertReadableArchive(target.FullPath));
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