using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed class PersistenceSaveEntry
    {
        public string FileName { get; set; }
        public string InternalName { get; set; }
        public string SaveType { get; set; }
        public string Area { get; set; }
        public string InitialSha256 { get; set; }
        public bool Writable { get; set; }
    }

    // Test-only authority. The caller supplies the already authorized run directory,
    // never an arbitrary SavePath from an archive. All leaves are explicit; names
    // beginning with KMC confer no authority. Native integration must also guard
    // temporary archive creation, rotation/rename and cloud paths before activation.
    internal sealed class PersistenceSaveAuthorization
    {
        private sealed class Entry
        {
            internal string Name;
            internal string Type;
            internal string Area;
            internal bool Writing;
            internal string Hash;
            internal bool Writable;
        }

        private readonly object sync = new object();
        private readonly Dictionary<string, Entry> entries =
            new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
        private readonly string gameId;
        private readonly string gameName;
        private readonly string baselineHash;
        internal string Root { get; }

        internal PersistenceSaveAuthorization(string authorizedRunRoot, string campaignId,
            string campaignName, string protectedBaselineHash, IEnumerable<PersistenceSaveEntry> allowlist)
        {
            var runRoot = Canonical(authorizedRunRoot);
            RequireDirectory(runRoot);
            Root = Path.Combine(runRoot, "Saved Games");
            RequireDirectory(Root);
            gameId = Required(campaignId, 128);
            gameName = Required(campaignName, 256);
            baselineHash = HashText(protectedBaselineHash);
            if (allowlist == null) throw new ArgumentNullException(nameof(allowlist));
            foreach (var item in allowlist)
            {
                if (item == null || entries.Count >= 128) throw new ArgumentException("Invalid bounded save allowlist.");
                var name = Required(item.InternalName, 256);
                var file = Required(item.FileName, 180);
                if (file != Path.GetFileName(file) || file.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
                    !file.EndsWith(".zks", StringComparison.Ordinal) || file.EndsWith(" ", StringComparison.Ordinal) ||
                    file.IndexOf("KMC_AUTOMATION_BASELINE", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    string.Equals(name, RuntimeRequest.BaselineSaveName, StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException("Save leaf or protected baseline alias is invalid.");
                if (item.SaveType != "Manual" && item.SaveType != "Quick" && item.SaveType != "Auto")
                    throw new ArgumentException("Only actual Manual, Quick and Auto save types are supported.");
                if (entries.ContainsKey(file)) throw new ArgumentException("Ambiguous duplicate save leaf.");
                var hash = string.IsNullOrEmpty(item.InitialSha256) ? null : HashText(item.InitialSha256);
                if (hash == baselineHash) throw new ArgumentException("A protected baseline copy is not an authorized fixture.");
                entries.Add(file, new Entry { Name = name, Type = item.SaveType, Area = Required(item.Area, 32), Hash = hash, Writable = item.Writable });
            }
            if (entries.Count == 0) throw new ArgumentException("An exact save allowlist is required.");
            foreach (var path in Directory.EnumerateFileSystemEntries(Root))
            {
                if (!entries.ContainsKey(Path.GetFileName(path)) || Directory.Exists(path))
                    throw new ArgumentException("Isolated root contains an unowned or ambiguous entry.");
            }
            foreach (var item in entries) VerifyFile(item.Key, item.Value);
        }

        internal void AssertReadableArchive(string path)
        {
            lock (sync)
            {
                if (Canonical(path) != path || Path.GetDirectoryName(path) != Root ||
                    !entries.TryGetValue(Path.GetFileName(path), out var entry) || entry.Writing || entry.Hash == null)
                    throw new InvalidOperationException("Native load archive is not a completed exact owned entry.");
                VerifyFile(Path.GetFileName(path), entry);
            }
        }

        internal string Validate(RuntimeSaveOperation operation, RuntimeSaveTarget target, string observedRoot)
        {
            lock (sync) return ValidateLocked(operation, target, observedRoot);
        }

        private string ValidateLocked(RuntimeSaveOperation operation, RuntimeSaveTarget target, string observedRoot)
        {
            try
            {
                if (Canonical(observedRoot) != Root) return "Observed save root differs from the isolated run root.";
                RequireDirectory(Root);
                if (target == null || string.IsNullOrEmpty(target.FileName) ||
                    !entries.TryGetValue(target.FileName, out var entry))
                    return "Save is not an explicit run-owned allowlist entry.";
                if (Canonical(target.FullPath) != Path.Combine(Root, target.FileName) ||
                    target.FileName != Path.GetFileName(target.FullPath))
                    return "Save is not the exact canonical direct child.";
                if (target.InternalName != entry.Name || target.SaveType != entry.Type ||
                    target.GameId != gameId || target.GameName != gameName || target.Area != entry.Area)
                    return "Native save name, type, campaign or area differs from the run contract.";
                if (entry.Writing) return "This save already belongs to an unfinished write.";
                if (operation != RuntimeSaveOperation.Load && operation != RuntimeSaveOperation.Write &&
                    operation != RuntimeSaveOperation.Delete) return "Unknown save operation.";
                if (operation != RuntimeSaveOperation.Load && !entry.Writable)
                    return "This run-owned entry is read-only.";
                VerifyFile(target.FileName, entry);
                if (operation != RuntimeSaveOperation.Write && entry.Hash == null)
                    return "Load/rotation requires a completed owned archive.";
                return null;
            }
            catch (Exception exception)
            {
                return "Isolated save identity check failed (" + exception.GetType().Name + ").";
            }
        }

        // Storage integration must acquire after native ordering and complete only
        // after the real archive commit. Disposing without completion preserves the
        // previous identity; changed partial files remain unauthorized.
        internal WriteLease BeginWrite(RuntimeSaveTarget target, string observedRoot)
        {
            lock (sync)
            {
                var rejection = ValidateLocked(RuntimeSaveOperation.Write, target, observedRoot);
                if (rejection != null) throw new InvalidOperationException(rejection);
                var entry = entries[target.FileName];
                entry.Writing = true;
                return new WriteLease(this, target.FileName);
            }
        }

        // Replacement is admitted only for two completed archives already owned
        // by this run, with the same native slot identity. The native worker owns
        // ordering; this lease does not authorize arbitrary rename destinations.
        internal ReplacementLease BeginReplacement(string source, string destination)
        {
            lock (sync)
            {
                if (Canonical(source) != source || Canonical(destination) != destination ||
                    Path.GetDirectoryName(source) != Root || Path.GetDirectoryName(destination) != Root ||
                    string.Equals(source, destination, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Replacement must use two distinct exact run-owned leaves.");
                var from = Path.GetFileName(source);
                var to = Path.GetFileName(destination);
                if (!entries.TryGetValue(from, out var staged) || !entries.TryGetValue(to, out var original) ||
                    !staged.Writable || !original.Writable || staged.Hash == null || original.Hash == null ||
                    staged.Writing || original.Writing || staged.Name != original.Name ||
                    staged.Type != original.Type || staged.Area != original.Area)
                    throw new InvalidOperationException("Replacement is outside the exact completed native slot contract.");
                VerifyFile(from, staged); VerifyFile(to, original);
                staged.Writing = true; original.Writing = true;
                return new ReplacementLease(this, from, to, staged.Hash);
            }
        }

        internal sealed class ReplacementLease : IDisposable
        {
            private PersistenceSaveAuthorization owner;
            private readonly string source;
            private readonly string destination;
            private readonly string committedHash;
            internal ReplacementLease(PersistenceSaveAuthorization owner, string source, string destination, string hash)
            { this.owner = owner; this.source = source; this.destination = destination; committedHash = hash; }

            internal void Complete()
            {
                var current = owner;
                if (current == null) throw new InvalidOperationException("Replacement scope is no longer active.");
                lock (current.sync)
                {
                    if (owner == null) throw new InvalidOperationException("Replacement scope is no longer active.");
                    RequireDirectory(current.Root);
                    var from = Path.Combine(current.Root, source);
                    if (File.Exists(from) || Directory.Exists(from) ||
                        ReadOwnedHash(Path.Combine(current.Root, destination)) != committedHash)
                        throw new IOException("Native replacement did not move the exact completed archive.");
                    current.entries[source].Hash = null;
                    current.entries[destination].Hash = committedHash;
                    Dispose();
                }
            }

            public void Dispose()
            {
                var current = owner;
                if (current == null) return;
                lock (current.sync)
                {
                    if (owner == null) return;
                    owner = null;
                    current.entries[source].Writing = false;
                    current.entries[destination].Writing = false;
                }
            }
        }

        internal sealed class WriteLease : IDisposable
        {
            private PersistenceSaveAuthorization owner;
            private readonly string leaf;
            internal WriteLease(PersistenceSaveAuthorization owner, string leaf)
            { this.owner = owner; this.leaf = leaf; }

            internal void Complete()
            {
                var current = owner;
                if (current == null) throw new InvalidOperationException("Write scope is no longer active.");
                lock (current.sync)
                {
                    if (owner == null) throw new InvalidOperationException("Write scope is no longer active.");
                    RequireDirectory(current.Root);
                    var hash = ReadOwnedHash(Path.Combine(current.Root, leaf));
                    if (hash == current.baselineHash)
                        throw new InvalidOperationException("Completed write aliases protected baseline bytes.");
                    current.entries[leaf].Hash = hash;
                    Dispose();
                }
            }

            public void Dispose()
            {
                var current = owner;
                if (current == null) return;
                lock (current.sync)
                {
                    if (owner == null) return;
                    owner = null;
                    current.entries[leaf].Writing = false;
                }
            }
        }

        private void VerifyFile(string leaf, Entry entry)
        {
            var path = Path.Combine(Root, leaf);
            if (entry.Hash == null)
            {
                if (File.Exists(path) || Directory.Exists(path))
                    throw new IOException("An uncommitted allowlist leaf already exists.");
                return;
            }
            if (ReadOwnedHash(path) != entry.Hash) throw new IOException("Owned save bytes changed outside a completed operation.");
        }

        private static string Required(string value, int max)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > max || value.Any(char.IsControl))
                throw new ArgumentException("Missing or oversized save identity.");
            return value;
        }

        private static string HashText(string value)
        {
            if (value == null || value.Length != 64 || value.Any(c => !(c >= '0' && c <= '9' || c >= 'a' && c <= 'f')))
                throw new ArgumentException("An exact lowercase SHA256 is required.");
            return value;
        }

        private static string Canonical(string path)
        {
            Required(path, 240);
            if (!Path.IsPathRooted(path) || path.StartsWith(@"\\", StringComparison.Ordinal) ||
                path.IndexOf(':', 2) >= 0)
                throw new ArgumentException("A local absolute path without device/stream syntax is required.");
            var full = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar);
            if (!string.Equals(path.TrimEnd(Path.DirectorySeparatorChar), full, StringComparison.Ordinal) ||
                full == Path.GetPathRoot(full).TrimEnd(Path.DirectorySeparatorChar))
                throw new ArgumentException("Noncanonical or filesystem-root save path.");
            return full;
        }

        private static void RequireDirectory(string path)
        {
            if (!Directory.Exists(path)) throw new DirectoryNotFoundException("Owned save directory must already exist.");
            for (var current = new DirectoryInfo(path); current != null; current = current.Parent)
                if ((current.Attributes & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("Reparse ancestry is not an owned save root.");
        }

        private static string ReadOwnedHash(string path)
        {
            RequireDirectory(Path.GetDirectoryName(path));
            if ((File.GetAttributes(path) & (FileAttributes.ReparsePoint | FileAttributes.Directory)) != 0)
                throw new IOException("Save leaf must be a regular file.");
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (!GetFileInformationByHandle(stream.SafeFileHandle, out var identity) ||
                    identity.NumberOfLinks != 1 || (identity.Attributes & (uint)FileAttributes.ReparsePoint) != 0)
                    throw new IOException("Save file identity is unavailable or multiply linked.");
                using (var sha = SHA256.Create())
                    return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct FileIdentity
        {
            public uint Attributes;
            public System.Runtime.InteropServices.ComTypes.FILETIME CreationTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastAccessTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastWriteTime;
            public uint VolumeSerialNumber;
            public uint FileSizeHigh;
            public uint FileSizeLow;
            public uint NumberOfLinks;
            public uint FileIndexHigh;
            public uint FileIndexLow;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetFileInformationByHandle(SafeFileHandle file, out FileIdentity information);
    }
}
