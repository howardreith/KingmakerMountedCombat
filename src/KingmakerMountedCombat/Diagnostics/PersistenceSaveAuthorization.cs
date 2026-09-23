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
        // Verified installed behaviour: SaveRoutine 06008029 is an iterator, so
        // its admission boundary runs when LoadArea 06000CD5 synchronously
        // constructs the enumerator while the departure area is still loaded.
        // Execution, PrepareSave 06008025 and the committed header then follow
        // in the destination, so an authored after-entry autosave legitimately
        // spans two areas. Leave this null unless the run declares a transition;
        // the authority then refuses any pair that is not its exact endpoints.
        public string AdmissionArea { get; set; }
        public string InitialSha256 { get; set; }
        public bool Writable { get; set; }
        // null/"A": the validated fixture campaign. "B": a leaf of the single
        // disposable native new game this run may start; its identity is minted
        // by the engine at run time and frozen on the first admitted write.
        public string Campaign { get; set; }
        // Verified installed behaviour: Game.LoadNewGame 06000CDC constructs the
        // new game's autosave SaveRoutine (IL_0525) right after queueing the
        // area load, while no area is loaded at all, so its admission boundary
        // observes no current area. Only a bootstrap leaf may admit that, and it
        // still commits in the exact declared area.
        public bool AdmitsBeforeArea { get; set; }
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
            internal string AdmissionArea;
            internal bool Writing;
            internal string Hash;
            internal bool Writable;
            // A leaf of the bootstrap campaign: the one native new game this run
            // may start, whose identity the engine mints.
            internal bool Bootstrap;
            internal bool AdmitsBeforeArea;
            internal bool Admits(string area) =>
                area == Area || (AdmissionArea != null && area == AdmissionArea) ||
                (area == null && AdmitsBeforeArea);
        }

        private readonly object sync = new object();
        private readonly Dictionary<string, Entry> entries =
            new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
        private readonly string gameId;
        private readonly string gameName;
        private readonly string baselineHash;
        internal string Root { get; }

        // ---- Bootstrap campaign ----
        //
        // Campaign A is the validated fixture identity every ordinary leaf is
        // bound to. A run that declares bootstrap leaves may start exactly ONE
        // native new game: the scenario opens the window immediately before
        // Game.LoadNewGame, the engine mints the identity, and the first native
        // write that reaches a bootstrap leaf inside that window freezes the
        // exact GameId/GameName it carries -- nothing is assigned by KMC. Before
        // the freeze, bootstrap leaves admit nothing; after it, only that exact
        // identity, and the window is closed. Ordinary leaves never admit it.
        private bool declaresBootstrap;
        private bool bootstrapWindowOpen;
        private string bootstrapGameId;
        private string bootstrapGameName;
        internal bool DeclaresBootstrapCampaign => declaresBootstrap;
        internal bool BootstrapWindowOpen { get { lock (sync) return bootstrapWindowOpen; } }
        internal string BootstrapGameId { get { lock (sync) return bootstrapGameId; } }
        internal string BootstrapGameName { get { lock (sync) return bootstrapGameName; } }
        internal int BootstrapFreezeCount { get; private set; }

        internal void OpenBootstrapWindow()
        {
            lock (sync)
            {
                if (!declaresBootstrap) throw new InvalidOperationException("This run declares no bootstrap campaign leaf.");
                if (bootstrapGameId != null) throw new InvalidOperationException("The bootstrap campaign identity is already frozen.");
                if (bootstrapWindowOpen) throw new InvalidOperationException("The bootstrap window is already open.");
                bootstrapWindowOpen = true;
            }
        }

        // The engine's own first write for the new game mints the identity.
        private string TryFreezeBootstrapLocked(string observedGameId, string observedGameName)
        {
            if (bootstrapGameId != null) return null;
            if (!bootstrapWindowOpen) return "Bootstrap campaign identity is not minted: no native new game was opened.";
            Guid parsed;
            if (string.IsNullOrEmpty(observedGameId) || !Guid.TryParse(observedGameId, out parsed) ||
                string.Equals(observedGameId, gameId, StringComparison.Ordinal))
                return "Bootstrap campaign identity is not a fresh native GameId.";
            if (string.IsNullOrEmpty(observedGameName)) return "Bootstrap campaign has no native GameName.";
            bootstrapGameId = observedGameId;
            bootstrapGameName = observedGameName;
            bootstrapWindowOpen = false;
            BootstrapFreezeCount++;
            return null;
        }

        private string ExpectedGameId(Entry entry) => entry.Bootstrap ? bootstrapGameId : gameId;
        private string ExpectedGameName(Entry entry) => entry.Bootstrap ? bootstrapGameName : gameName;

        internal PersistenceSaveAuthorization(string authorizedRunRoot, string campaignId,
            string campaignName, string protectedBaselineHash, IEnumerable<PersistenceSaveEntry> allowlist)
            : this(authorizedRunRoot, campaignId, campaignName, protectedBaselineHash, allowlist, null, null)
        {
        }

        internal PersistenceSaveAuthorization(string authorizedRunRoot, string campaignId,
            string campaignName, string protectedBaselineHash, IEnumerable<PersistenceSaveEntry> allowlist,
            string declaredTransitionSource, string declaredTransitionTarget)
        {
            var transitionDeclared = declaredTransitionSource != null || declaredTransitionTarget != null;
            if (transitionDeclared && (declaredTransitionSource == null || declaredTransitionTarget == null ||
                declaredTransitionSource == declaredTransitionTarget))
                throw new ArgumentException("A declared area transition needs two distinct exact endpoints.");
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
                var area = Required(item.Area, 32);
                string admission = null;
                if (!string.IsNullOrEmpty(item.AdmissionArea) && item.AdmissionArea != area)
                {
                    admission = Required(item.AdmissionArea, 32);
                    // Only the two declared endpoints of a declared transfer may
                    // span two areas, and only a writable leaf may do so.
                    if (!transitionDeclared || !item.Writable ||
                        !(admission == declaredTransitionSource && area == declaredTransitionTarget ||
                          admission == declaredTransitionTarget && area == declaredTransitionSource))
                        throw new ArgumentException("A two-area save leaf must be a writable declared transition endpoint.");
                }
                if (item.Campaign != null && item.Campaign != "A" && item.Campaign != "B")
                    throw new ArgumentException("A save leaf belongs to campaign A or the bootstrap campaign B.");
                var bootstrap = item.Campaign == "B";
                // The bootstrap campaign does not exist before the run starts it:
                // its leaves are single-area, writable and initially empty.
                if (bootstrap && (!item.Writable || hash != null || admission != null))
                    throw new ArgumentException("A bootstrap campaign leaf must be an empty single-area writable leaf.");
                if (item.AdmitsBeforeArea && (!bootstrap || item.SaveType != "Auto"))
                    throw new ArgumentException("Only a bootstrap autosave leaf is admitted before any area is loaded.");
                declaresBootstrap |= bootstrap;
                entries.Add(file, new Entry { Name = name, Type = item.SaveType, Area = area,
                    AdmissionArea = admission, Hash = hash, Writable = item.Writable, Bootstrap = bootstrap,
                    AdmitsBeforeArea = item.AdmitsBeforeArea });
            }
            if (entries.Count == 0) throw new ArgumentException("An exact save allowlist is required.");
            if (declaresBootstrap && entries.Values.All(entry => entry.Bootstrap))
                throw new ArgumentException("A bootstrap campaign needs the validated fixture campaign beside it.");
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

        // Request creation precedes native queue ordering and slot allocation.
        // A unique, still-empty declared destination can identify the request;
        // BeginWrite still validates the actual PrepareSave path before storage.
        internal RuntimeSaveTarget ProjectNewRequest(RuntimeSaveTarget predicted, string observedRoot)
        {
            lock (sync)
            {
                if (predicted == null || Canonical(observedRoot) != Root ||
                    Canonical(predicted.FullPath) != Path.Combine(Root, predicted.FileName) ||
                    predicted.FileName != Path.GetFileName(predicted.FullPath))
                    throw new InvalidOperationException("New request projection escaped its exact isolated root.");
                RequireDirectory(Root);
                // A request carrying the engine-minted identity of the one open
                // (or already frozen) bootstrap game is projected onto bootstrap
                // leaves only; the fixture campaign never reaches them.
                var fixtureCampaign = predicted.GameId == gameId && predicted.GameName == gameName;
                var bootstrapCampaign = !fixtureCampaign && declaresBootstrap && (bootstrapGameId == null
                    ? bootstrapWindowOpen && !string.Equals(predicted.GameId, gameId, StringComparison.Ordinal)
                    : predicted.GameId == bootstrapGameId && predicted.GameName == bootstrapGameName);
                if (!fixtureCampaign && !bootstrapCampaign)
                    throw new InvalidOperationException("New request campaign differs from its run.");
                var candidates = entries.Where(p => p.Value.Name == predicted.InternalName &&
                    p.Value.Type == predicted.SaveType && p.Value.Admits(predicted.Area) &&
                    p.Value.Bootstrap == bootstrapCampaign &&
                    p.Value.Writable && !p.Value.Writing && p.Value.Hash == null).ToArray();
                // Existing rotation contracts may deliberately declare two leaves.
                // Keep their native prediction and let normal validation decide.
                if (candidates.Length != 1) return predicted;
                var candidate = candidates[0];
                VerifyFile(candidate.Key, candidate.Value);
                return new RuntimeSaveTarget
                {
                    InternalName = predicted.InternalName, FileName = candidate.Key,
                    FullPath = Path.Combine(Root, candidate.Key), SaveType = predicted.SaveType,
                    GameId = predicted.GameId, GameName = predicted.GameName, Area = predicted.Area
                };
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
                if (operation != RuntimeSaveOperation.Load && operation != RuntimeSaveOperation.Write &&
                    operation != RuntimeSaveOperation.Delete) return "Unknown save operation.";
                if (entry.Bootstrap && bootstrapGameId == null)
                {
                    // The first write the engine admits to a bootstrap leaf inside
                    // the open window freezes the identity it minted. Nothing can
                    // be loaded or rotated from that campaign before that.
                    if (operation != RuntimeSaveOperation.Write ||
                        target.InternalName != entry.Name || target.SaveType != entry.Type || !entry.Admits(target.Area))
                        return "Bootstrap campaign identity is not minted: no native new game write reached its leaf.";
                    var minting = TryFreezeBootstrapLocked(target.GameId, target.GameName);
                    if (minting != null) return minting;
                }
                if (target.InternalName != entry.Name || target.SaveType != entry.Type ||
                    target.GameId != ExpectedGameId(entry) || target.GameName != ExpectedGameName(entry) ||
                    !entry.Admits(target.Area))
                    // Naming the observed and declared identity keeps a rejected
                    // boundary self-diagnosing instead of costing another run.
                    return "Native save name, type, campaign or area differs from the run contract" +
                        " (observed name=" + target.InternalName + " type=" + target.SaveType +
                        " area=" + target.Area + " campaign=" + (entry.Bootstrap ? "B" : "A") +
                        "; declared name=" + entry.Name + " type=" + entry.Type +
                        " area=" + entry.Area + (entry.AdmissionArea == null ? string.Empty :
                        " admission=" + entry.AdmissionArea) + ").";
                if (entry.Writing) return "This save already belongs to an unfinished write.";
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
                return new WriteLease(this, target.FileName, entry.Hash);
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

        // Used only by the still-active native worker for its own newly
        // allocated staging path; replacement destinations never enter this path.
        internal void DeleteCompletedStaging(string path)
        {
            lock (sync)
            {
                if (Canonical(path) != path || Path.GetDirectoryName(path) != Root ||
                    !entries.TryGetValue(Path.GetFileName(path), out var entry) ||
                    !entry.Writable || entry.Writing || entry.Hash == null)
                    throw new InvalidOperationException("Cleanup is not a completed owned staging archive.");
                VerifyFile(Path.GetFileName(path), entry);
                File.Delete(path);
                if (File.Exists(path) || Directory.Exists(path)) throw new IOException("Owned staging cleanup failed.");
                entry.Hash = null;
            }
        }

        internal sealed class WriteLease : IDisposable
        {
            private PersistenceSaveAuthorization owner;
            private readonly string leaf;
            private readonly string initialHash;
            internal WriteLease(PersistenceSaveAuthorization owner, string leaf, string initialHash)
            { this.owner = owner; this.leaf = leaf; this.initialHash = initialHash; }

            internal void ClearStaging()
            {
                var current = owner;
                if (current == null) throw new InvalidOperationException("Staging write scope is no longer active.");
                lock (current.sync)
                {
                    if (owner == null || initialHash != null || !current.entries[leaf].Writing ||
                        current.entries[leaf].Hash != null)
                        throw new InvalidOperationException("Cleanup cannot delete an existing completed save.");
                    RequireDirectory(current.Root);
                    var path = Path.Combine(current.Root, leaf);
                    if (Directory.Exists(path)) throw new IOException("Staging path became a directory.");
                    if (!File.Exists(path)) return;
                    if (ReadOwnedHash(path) == current.baselineHash)
                        throw new InvalidOperationException("Staging bytes alias a protected baseline.");
                    File.Delete(path);
                    if (File.Exists(path)) throw new IOException("Owned staging cleanup failed.");
                }
            }

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
