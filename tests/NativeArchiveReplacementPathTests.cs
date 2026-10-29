using System;
using System.IO;
using KingmakerMountedCombat.Integration;

namespace KingmakerMountedCombat.Tests
{
    // The native replacement commit decides which two files it may touch from
    // the engine's own path strings. The engine mints those strings from
    // Unity's forward-slash persistent data path joined with backslash
    // separators, so the player's archives never equal their own GetFullPath.
    // Real temporary files; no game types.
    internal static class NativeArchiveReplacementPathTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("replacement accepts the engine's mixed-separator archive paths", EngineShapedPaths);
            runner.Run("replacement accepts canonical archive paths", CanonicalPaths);
            runner.Run("replacement refuses the same file, other directories, missing files and directories", StructuralRefusals);
            runner.Run("replacement refuses relative, UNC and stream paths", FormRefusals);
        }

        private sealed class Root : IDisposable
        {
            internal readonly string Canonical;
            internal Root()
            {
                Canonical = Path.Combine(Path.GetTempPath(), "kmc-replace-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(Canonical);
            }
            internal string File(string relative, string content)
            {
                var path = Path.Combine(Canonical, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                System.IO.File.WriteAllText(path, content);
                return path;
            }
            public void Dispose() { try { Directory.Delete(Canonical, true); } catch (IOException) { } }
        }

        // Exactly how SaveManager.PrepareSave builds a path: Unity's persistent
        // data path (forward slashes) joined with "Saved Games" and the leaf.
        private static string EngineShaped(string unityParent, string leaf) =>
            Path.Combine(Path.Combine(unityParent, "Saved Games"), leaf) + ".zks";

        private static void EngineShapedPaths()
        {
            using (var root = new Root())
            {
                var unityParent = root.Canonical.Replace('\\', '/');
                var staged = EngineShaped(unityParent, "Quick_2");
                var original = EngineShaped(unityParent, "Quick_1");
                Directory.CreateDirectory(Path.GetFullPath(Path.GetDirectoryName(staged)));
                File.WriteAllText(staged, "new quick save");
                File.WriteAllText(original, "previous quick save");
                TestRunner.True(staged.IndexOf('/') >= 0 && staged.IndexOf('\\') >= 0 && Path.GetFullPath(staged) != staged,
                    "Fixture does not reproduce the engine's mixed-separator path.");
                var resolved = NativeArchiveReplacementPath.Resolve(staged, original);
                TestRunner.Equal(Path.GetFullPath(staged), resolved.Source, "Source was not canonicalized.");
                TestRunner.Equal(Path.GetFullPath(original), resolved.Destination, "Destination was not canonicalized.");
                File.Replace(resolved.Source, resolved.Destination, null);
                TestRunner.Equal("new quick save", File.ReadAllText(original), "Replacement did not land on the previous slot.");
                TestRunner.True(!File.Exists(staged), "Staged archive survived its replacement.");
            }
        }

        private static void CanonicalPaths()
        {
            using (var root = new Root())
            {
                var staged = root.File("Saved Games\\Auto_2.zks", "new");
                var original = root.File("Saved Games\\Auto_1.zks", "old");
                var resolved = NativeArchiveReplacementPath.Resolve(staged, original);
                TestRunner.Equal(staged, resolved.Source, "Canonical source changed.");
                TestRunner.Equal(original, resolved.Destination, "Canonical destination changed.");
                // The parent an isolated run hands the engine: Unity's spelling, and the
                // engine's own Path.Combine then mints the player's mixed form under it.
                var saveRoot = Path.Combine(root.Canonical, "Saved Games");
                var parent = NativeArchiveReplacementPath.EngineShapedParent(saveRoot);
                TestRunner.Equal(root.Canonical.Replace('\\', '/'), parent, "Engine-shaped parent is not Unity's spelling.");
                var minted = EngineShaped(parent, "Auto_2");
                TestRunner.True(minted.IndexOf('/') >= 0 && minted.IndexOf('\\') >= 0 && Path.GetFullPath(minted) == staged,
                    "The engine-shaped parent does not mint the player's path form onto the same file.");
                TestRunner.True(NativeArchiveReplacementPath.Resolve(minted, original).Source == staged, "The minted path was not resolved onto the staged archive.");
            }
        }

        private static void Refused(string source, string destination, string message)
        {
            try { NativeArchiveReplacementPath.Resolve(source, destination); }
            catch (IOException) { return; }
            throw new InvalidOperationException(message);
        }

        private static void StructuralRefusals()
        {
            using (var root = new Root())
            {
                var staged = root.File("Saved Games\\Quick_2.zks", "new");
                var original = root.File("Saved Games\\Quick_1.zks", "old");
                var elsewhere = root.File("Other\\Quick_1.zks", "old");
                Refused(staged, staged, "Same file was accepted.");
                Refused(staged, staged.Replace('\\', '/'), "Same file in another separator form was accepted.");
                Refused(staged, elsewhere, "A destination in another directory was accepted.");
                Refused(staged, Path.Combine(root.Canonical, "Saved Games", "Quick_9.zks"), "A missing destination was accepted.");
                Refused(Path.Combine(root.Canonical, "Saved Games", "Quick_9.zks"), original, "A missing source was accepted.");
                Refused(Path.Combine(root.Canonical, "Saved Games"), original, "A directory source was accepted.");
                Refused(staged, Path.Combine(root.Canonical, "Other"), "A directory destination was accepted.");
            }
        }

        private static void FormRefusals()
        {
            using (var root = new Root())
            {
                var staged = root.File("Saved Games\\Quick_2.zks", "new");
                var original = root.File("Saved Games\\Quick_1.zks", "old");
                Refused("Saved Games\\Quick_2.zks", original, "A relative source was accepted.");
                Refused(staged, @"\\server\share\Quick_1.zks", "A UNC destination was accepted.");
                Refused(staged, original + ":stream", "A stream destination was accepted.");
                Refused(null, original, "A null source was accepted.");
                Refused(staged, string.Empty, "An empty destination was accepted.");
                TestRunner.True(NativeArchiveReplacementPath.Canonical(@"\\?\C:\x.zks") == null, "Device syntax was canonicalized.");
                // Rooted but not fully qualified: these resolve against the current
                // directory or drive, so they must never reach File.Replace even when
                // that resolution happens to land on the real archives.
                var previous = Directory.GetCurrentDirectory();
                try
                {
                    Directory.SetCurrentDirectory(Path.Combine(root.Canonical, "Saved Games"));
                    var driveRelative = root.Canonical.Substring(0, 2) + "Quick_2.zks";
                    var rootRelative = staged.Substring(2);
                    TestRunner.True(Path.IsPathRooted(driveRelative) && Path.IsPathRooted(rootRelative) &&
                        Path.GetFullPath(driveRelative) == staged && Path.GetFullPath(rootRelative) == staged,
                        "Fixture does not reproduce rooted-but-relative resolution onto the staged archive.");
                    Refused(driveRelative, original, "A drive-relative source was accepted.");
                    Refused(staged, rootRelative.Replace("Quick_2", "Quick_1"), "A root-relative destination was accepted.");
                    Refused(rootRelative.Replace('\\', '/'), original, "A forward-slash root-relative source was accepted.");
                    TestRunner.True(NativeArchiveReplacementPath.Canonical(driveRelative) == null &&
                        NativeArchiveReplacementPath.Canonical(rootRelative) == null, "Rooted-but-relative paths were canonicalized.");
                }
                finally { Directory.SetCurrentDirectory(previous); }
            }
        }
    }
}
