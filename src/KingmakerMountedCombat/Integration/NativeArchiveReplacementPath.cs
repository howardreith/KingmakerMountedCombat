using System;
using System.IO;

namespace KingmakerMountedCombat.Integration
{
    // Which two files a native archive replacement may touch, decided from the
    // paths the ENGINE hands over rather than from a string form it never
    // promised.
    //
    // SaveManager.PrepareSave (0x06008025) mints every archive path as
    // Path.Combine(Path.Combine(ApplicationPaths.persistentDataPath, "Saved Games"), leaf)
    // and ApplicationPaths.persistentDataPath is Unity's Application.persistentDataPath,
    // which on Windows is written with forward slashes. The engine's own path
    // therefore mixes separators and is not equal to its own Path.GetFullPath.
    // The lab's isolated save authority replaces that root with a canonical one,
    // so a validator that demanded the canonical form passed every lab rotation
    // row and refused every player Quick/Auto overwrite. The identity of the
    // files is what matters here: both must be existing regular files in the
    // same directory, and they must be different files. Separator form and
    // redundant segments are normalized, never refused. No game types, so the
    // decision is testable against real files.
    internal static class NativeArchiveReplacementPath
    {
        internal struct Resolved
        {
            internal string Source;
            internal string Destination;
        }

        internal static Resolved Resolve(string source, string destination)
        {
            var from = Canonical(source);
            var to = Canonical(destination);
            if (from == null || to == null ||
                string.Equals(from, to, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(Path.GetDirectoryName(from), Path.GetDirectoryName(to), StringComparison.OrdinalIgnoreCase) ||
                !IsRegularFile(from) || !IsRegularFile(to))
                throw new IOException("Native archive replacement requires two existing regular sibling files.");
            return new Resolved { Source = from, Destination = to };
        }

        // The canonical absolute form of an engine path, or null when it is not
        // a plain rooted local file path: relative, UNC, device or stream
        // syntax, invalid characters, or anything GetFullPath itself rejects.
        internal static string Canonical(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path) ||
                path.StartsWith(@"\\", StringComparison.Ordinal) || path.StartsWith("//", StringComparison.Ordinal) ||
                path.IndexOf(':', 2) >= 0 || path.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                return null;
            try { return Path.GetFullPath(path); }
            catch (ArgumentException) { return null; }
            catch (NotSupportedException) { return null; }
            catch (PathTooLongException) { return null; }
        }

        private static bool IsRegularFile(string path) =>
            File.Exists(path) &&
            (File.GetAttributes(path) & (FileAttributes.ReparsePoint | FileAttributes.Directory)) == 0;
    }
}
