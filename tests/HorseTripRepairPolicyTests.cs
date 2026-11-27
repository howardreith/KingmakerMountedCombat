using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // MMR-06 existing horses (reviews R3 R1-F1, R4-F4, R4-F5, R4-F6): nothing is removed automatically; one fact is removed
    // from one horse only on the owner's exact request, through the production HorseTripRepairPolicy.Process and the
    // production HorseTripRepairFiles on real temporary folders (a KMC user-data folder, and a guarded-style staged Mods
    // folder that must stay byte-identical). Horses and facts are fakes behind the same host interface the integration uses.
    internal static class HorseTripRepairPolicyTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("MMR-06: a candidate needs the exact Horse, the stock identity and shape, and no provenance", CandidatePredicate);
            runner.Run("R4-F4: the whole request is validated; two horses or extra content select nothing", RequestParsing);
            runner.Run("MMR-06/R4-F6: the exact request removes one fact, is claimed and closed in user data, Mods tree untouched", ExactRequestApplies);
            runner.Run("MMR-06: legacy-ordered and independently granted facts stay without a request; refusals change nothing", NegativeCases);
            runner.Run("MMR-06: a closed gate, an unloaded horse and a failed removal keep the request for a retry", PendingAndRetry);
            runner.Run("R4-F5: failures after removal, at the claim, the result write and the final rename never record a refusal", ReportingFailures);
            runner.Run("R4-F5: a stranded claim finishes as applied without a second removal, also after a reload; a restart closes it unchanged", StrandedClaims);
            runner.Run("R5-F1: a request replaced between observation and claim is judged by the claimed bytes (apply and refusal)", ReplacedBeforeClaim);
            runner.Run("R5-F1: a request written after the claim is a separate later request; a waiting claim yields to it unchanged", WrittenAfterClaim);
            runner.Run("R6-F4: a one-time read or lookup failure after the claim keeps this process's exact claim; the next poll applies it once", TransientFailureAfterClaim);
            runner.Run("R7-F5: a refusal, supersession or application frozen before reporting stays terminal through result-write and archive failures and candidate changes", TerminalDecisionsStayTerminal);
            runner.Run("PR-4 R1-F1: a guarded run without an isolated root leaves the player's repair files byte-identical; isolated and ordinary runs use only their own folder", RepairFolderSelection);
            runner.Run("PR-4 R1-F2: an unverifiable or inexact removal is terminal (one removal in total, truthful result); an exact removal that then threw is applied", UncertainRemovalIsTerminal);
            runner.Run("PR-4 R1-F4: the claim is bound to its raw bytes; a transient observation failure keeps the frozen outcome", ClaimIdentityIsBytes);
            runner.Run("PR-4 R1-F5: a newer request winning the return rename supersedes the waiting claim; the waiting horse is untouched", ReturnRenameRace);
            runner.Run("PR-4 R1-F2: a native removal that takes a neighbour and throws before any check is terminal (no Applied, no second removal after reload); an exact removal that throws is applied", NativeBoundaryRemoval);
            runner.Run("PR-4 R2-F1: a native removal that returns with nothing changed keeps the request; the retry removes once; a newer request supersedes it unchanged", UnchangedRemovalRetries);
            runner.Run("PR-4 R1-F4: a claim replaced during publication never receives the decided suffix; each result names its own horse; one removal", ReplacedDuringPublication);
            runner.Run("PR-4 R3-F1: a failing removal diagnostic after an exact removal keeps the applied decision and archive; a reloaded fact is preserved", DiagnosticFailureAfterRemoval);
            runner.Run("PR-4 R3-F2: a failed final archive write is resumed before a newer request; a fresh session settles it as interrupted", FinalPublicationFailureResumes);
            runner.Run("PR-4 R1-F4: bytes changed after verification never enter the decision's archive and keep their own disposition", ChangedAfterVerification);
            runner.Run("PR-4 R3-F1: an exact removal whose native exception message is unreadable is applied once; a reloaded fact is preserved", UnreadableNativeMessage);
            runner.Run("PR-4 R4-F1: an obstructed final archive rename (after a complete write and flush) leaves no partial terminal file; the retry commits one complete archive", FailedCommitLeavesNoPartialArchive);
            runner.Run("PR-4 R4-F1 (round 6): a failure during the actual payload write or flush leaves no partial terminal archive; the retry commits one complete archive, one removal", WriteOrFlushFailureLeavesNoPartialArchive);
            runner.Run("PR-4 R5-F1 (round 6): a change to the staged archive at publication is excluded or refused; the final archive holds exactly A, one removal", PendingArchiveChangeIsExcludedOrRefused);
            runner.Run("PR-4 R4-F3/R5-F2 (round 6): a replacement at retirement keeps its own Interrupted result and archive, survives its failed publication and settles before C; A removed once", ReplacementAtRetirementTakesItsOwnLifecycle);
            runner.Run("PR-4 R4-F2 (round 6): a locked recovered claim in a fresh session produces one interruption archive, then completes; the waiting request then runs", LockedRecoveredClaimArchivesOnce);
            runner.Run("PR-4 R6-F1 (round 7): a replacement after A's committed archive and failed retirement keeps its own archive record and result; C then runs", ReplacementAfterFailedRetirementHasItsOwnRecord);
            runner.Run("PR-4 R5-F1/R4-F3 (round 7): publication and retirement act on the verified file's own handle; late rename-replacements are excluded (Windows)", VerifiedFileOperationsBindTheFinalOperation);
            runner.Run("PR-4 R6-F2 (round 7): fresh-session recovery after an applied removal reports truthfully, with exact retry guidance, and removes nothing more", RestartReportingIsTruthful);
            runner.Run("PR-4 R7-F1 (round 8): retiring a linked claim leaves its sibling target intact; ordinary retirement still works (Windows)", LinkedClaimRetirementLeavesItsTarget);
            runner.Run("PR-4 R4-F3 (round 8): the pathname branch never deletes; a replacement at its final retirement operation returns and settles before C", PathnameRetirementNeverConsumesAReplacement);
            runner.Run("PR-4 R5-F1 (round 9): the pathname branch publishes the complete payload by one non-overwriting link; a partial-write fault leaves no terminal archive and a foreign entry untouched (simulated POSIX boundary since round 10)", PathnamePublicationIsComplete);
            runner.Run("PR-4 R4-F3 (round 9): a failure at the settling check or move back leaves the replacement recoverable in the same and a fresh session (simulated POSIX boundary since round 10)", PathnameRetirementRecoversAfterSettlingFailures);
            runner.Run("PR-4 R8-F1 (round 9): linked requests and claims are refused before selection, removal or copying (actual junctions and a simulated symbolic-link boundary); ordinary and locked-regular controls", LinkedEntriesAreRefusedBeforeActing);
            runner.Run("PR-4 R5-F1/R9-F3 (round 10): Unix publication has no staging name: unnamed-file link, and the protected exclusive create after a failed or unavailable descriptor link; first readable bytes are A's; in-place change and name replacement are excluded or refused (simulated POSIX boundary)", UnixPublicationIsProtected);
            runner.Run("PR-4 R9-F3 (round 10): an obstructed terminal across repeated polls leaves no staging file under either Unix mechanism; then one archive, one removal, no alias, the foreign file preserved (simulated POSIX boundary)", UnixStagingStaysBounded);
            runner.Run("PR-4 R9-F1 (round 10): a request still held by its writer is never decided on Windows (zero removals); the completed two-horse request is refused as malformed; a read-only holder stays compatible", WindowsWriterIsExcluded);
            runner.Run("PR-4 R9-F2 (round 10): a link substituted at the open boundary of an undecided claim or a recovery entry supplies no removal or archive; ordinary-file controls (simulated POSIX boundary)", UnixReadsBindTheDescriptor);
            runner.Run("PR-4 R9-F4 (round 10): a fresh session preserves an older completed record, its Applied result and archive and a similarly named backup; no Interrupted archive (Windows and simulated POSIX)", RecoveryIsConservative);
            runner.Run("PR-4 R4-F2: a blocked claim retirement commits the archive once; the waiting request then proceeds", BlockedRetirementCommitsOnce);
            runner.Run("PR-4 R3-F2/R4-F3: replacement bytes during retirement keep their own disposition before newer work; A removed once", ReplacementDuringRetirement);
        }

        private sealed class Fact
        {
            public string Name;
            public bool StockIdentity, BiteTripShape, Sourceless;
        }

        private sealed class Horse
        {
            public string Id;
            public bool ExactKmcHorse = true;
            public readonly List<Fact> Facts = new List<Fact>();
        }

        private static Fact StockGrant(string name = "TrippingBite") =>
            new Fact { Name = name, StockIdentity = true, BiteTripShape = true, Sourceless = true };

        // The alpha-1 creation order: [TrippingBite, NaturalArmor7, TripDefenseFourLegs] then class and level features.
        private static Horse LegacyHorse(string id)
        {
            var horse = new Horse { Id = id };
            horse.Facts.Add(StockGrant());
            horse.Facts.Add(new Fact { Name = "NaturalArmor7", Sourceless = true });
            horse.Facts.Add(new Fact { Name = "TripDefenseFourLegs", Sourceless = true });
            horse.Facts.Add(new Fact { Name = "AnimalCompanionClassFeature" });
            return horse;
        }

        // Real folders: <root>/Pathfinder Kingmaker/KingmakerMountedCombat (user data) and <root>/Mods/KingmakerMountedCombat (staged).
        private sealed class Folders : IDisposable
        {
            public readonly string Root = Path.Combine(Path.GetTempPath(), "kmc-trip-repair-" + Guid.NewGuid().ToString("N"));
            public readonly string UserData;
            public readonly string StagedMod;

            public Folders(bool createUserData = true)
            {
                UserData = HorseTripRepairPaths.Folder(Path.Combine(Root, "Pathfinder Kingmaker"));
                StagedMod = Path.Combine(Root, "Mods", "KingmakerMountedCombat");
                Directory.CreateDirectory(StagedMod);
                File.WriteAllText(Path.Combine(StagedMod, "Info.json"), "{\"Id\":\"KingmakerMountedCombat\"}");
                File.WriteAllBytes(Path.Combine(StagedMod, "KingmakerMountedCombat.dll"), new byte[] { 77, 90, 1, 2, 3 });
                if (createUserData) Directory.CreateDirectory(UserData);
            }

            public void Request(string text) => File.WriteAllText(Path.Combine(UserData, HorseTripRepairPolicy.RequestFileName), text);

            public string[] UserFiles() => Directory.Exists(UserData)
                ? Directory.GetFiles(UserData).Select(Path.GetFileName).OrderBy(name => name, StringComparer.Ordinal).ToArray() : new string[0];

            public string Result() => File.ReadAllText(Path.Combine(UserData, HorseTripRepairPolicy.ResultFileName));

            public string StagedDigest()
            {
                using (var sha = SHA256.Create())
                    return string.Join("|", Directory.GetFiles(StagedMod, "*", SearchOption.AllDirectories).OrderBy(path => path, StringComparer.Ordinal)
                        .Select(path => path.Substring(StagedMod.Length) + ":" + BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path)))).ToArray());
            }

            public void Dispose() { try { Directory.Delete(Root, true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
        }

        private sealed class Host : IHorseTripRepairHost<Fact>
        {
            public readonly HorseTripRepairFiles files;
            public int Removals;
            // Which horse each removal touched, in order (the integration logs the same horse ID at its removal).
            public readonly List<string> RemovedFrom = new List<string>();
            public bool FailRemovalBefore, FailRemovalAfter, FailClaim, FailResultWrite, FailClose;
            // PR-4 review R3-F1: the removal report throws (as a display-name read or logger fault would in the integration).
            public bool FailReport;
            // PR-4 review R3-F1 (round 5): the native call removes exactly the target and then throws this exception.
            public Exception ThrowAfterRemoval;
            public int ReportCalls;
            // PR-4 reviews R1-F2/R2-F1, all at the simulated native boundary (inside NativeRemove, before any classification): the
            // next read of the collection after the native call throws; the native call also takes a neighbouring fact (and then
            // throws when FailRemovalAfter is also set); the native call returns normally having changed nothing.
            public bool FailReadAfterRemoval, AlsoRemoveNeighbour, NoEffect;
            private bool failNextRead;
            // Invoked inside the native call before it returns (to write a newer request at that moment), and after the result write.
            public Action DuringNativeRemoval, AfterResultWrite;
            // Native removal calls in total (an unchanged call counts here, not in Removals).
            public int NativeCalls;
            // One-time failures armed to fire only after a successful claim rename (review R6-F4).
            public bool FailClaimReadAfterClaim, FailLookupAfterClaim;
            private bool failNextClaimRead, failNextLookup;
            public readonly List<Horse> Loaded = new List<Horse>();
            private Horse current;

            public Host(Folders folders) : this(folders.UserData) { }

            public Host(string directory) { files = new HorseTripRepairFiles(directory); }

            public bool TryReadRequest(out string text) => files.TryReadRequest(out text);
            public bool TryReadClaim(out string text, out string identity)
            {
                if (failNextClaimRead) { failNextClaimRead = false; throw new IOException("simulated read failure of the claimed request"); }
                return files.TryReadClaim(out text, out identity);
            }

            public IList<Fact> CandidatesOf(string horseId, out bool loaded)
            {
                if (failNextLookup) { failNextLookup = false; throw new InvalidOperationException("simulated horse lookup failure"); }
                current = Loaded.FirstOrDefault(horse => horse.Id == horseId);
                loaded = current != null;
                return current == null ? new List<Fact>() : Candidates(current);
            }

            // Like the integration host: the production HorseTripRemoval.RemoveExactly over this horse's live collection and the
            // simulated native removal; the policy receives its classified outcome.
            public HorseTripRemovalResult RemoveExactly(Fact fact) => HorseTripRemoval.RemoveExactly(fact, ReadFeatures, NativeRemove);

            public void ReportRemoval(string horseId, HorseTripRemovalResult removal)
            {
                ReportCalls++;
                if (FailReport) { FailReport = false; throw new InvalidOperationException("simulated removal diagnostic failure"); }
            }

            private IEnumerable<Fact> ReadFeatures()
            {
                if (failNextRead) { failNextRead = false; throw new InvalidOperationException("simulated feature read failure"); }
                if (current == null) throw new InvalidOperationException("The horse's feature collection is unavailable.");
                return current.Facts;
            }

            // The simulated native RemoveFact: every injected failure happens here, inside the native call.
            private void NativeRemove(Fact fact)
            {
                NativeCalls++;
                DuringNativeRemoval?.Invoke();
                if (FailRemovalBefore) { FailRemovalBefore = false; throw new InvalidOperationException("simulated native failure before any change"); }
                if (NoEffect) { NoEffect = false; return; }
                TestRunner.True(current.Facts.Remove(fact), "The removed fact was not the horse's own instance.");
                Removals++;
                RemovedFrom.Add(current.Id);
                if (FailReadAfterRemoval) { FailReadAfterRemoval = false; failNextRead = true; }
                if (AlsoRemoveNeighbour) { AlsoRemoveNeighbour = false; current.Facts.RemoveAt(0); }
                if (FailRemovalAfter) { FailRemovalAfter = false; throw new InvalidOperationException("simulated native failure after the change"); }
                if (ThrowAfterRemoval != null) { var thrown = ThrowAfterRemoval; ThrowAfterRemoval = null; throw thrown; }
            }

            public void Claim()
            {
                if (FailClaim) { FailClaim = false; throw new IOException("simulated claim rename failure"); }
                files.Claim();
                if (FailClaimReadAfterClaim) { FailClaimReadAfterClaim = false; failNextClaimRead = true; }
                if (FailLookupAfterClaim) { FailLookupAfterClaim = false; failNextLookup = true; }
            }

            public bool ReleaseClaim() => files.ReleaseClaim();

            public void WriteResult(string result)
            {
                if (FailResultWrite) { FailResultWrite = false; throw new IOException("simulated result write failure"); }
                files.WriteResult(result);
                AfterResultWrite?.Invoke();
            }

            public bool Close(bool claimed, string suffix, string boundIdentity, HorseTripRepairSession session = null)
            {
                if (FailClose) { FailClose = false; throw new IOException("simulated final rename failure"); }
                return files.Close(claimed, suffix, boundIdentity, session);
            }
        }

        private static List<Fact> Candidates(Horse horse) => horse.Facts.Where(fact =>
            HorseTripRepairPolicy.IsCandidate(horse.ExactKmcHorse, fact.StockIdentity, fact.BiteTripShape, fact.Sourceless)).ToList();

        private static HorseTripRepairDecision Run(Host host, HorseTripRepairSession session, bool gate = true) =>
            HorseTripRepairPolicy.Process(host, gate, session);

        private static bool Throws(Action action)
        {
            try { action(); return false; }
            catch (Exception) { return true; }
        }

        private static readonly string Request = HorseTripRepairPolicy.RequestFileName;
        private static readonly string Claimed = HorseTripRepairPolicy.RequestFileName + HorseTripRepairPolicy.ClaimedSuffix;
        private static readonly string Applied = HorseTripRepairPolicy.RequestFileName + HorseTripRepairPolicy.AppliedSuffix;
        private static readonly string Refused = HorseTripRepairPolicy.RequestFileName + HorseTripRepairPolicy.RefusedSuffix;
        private static readonly string Interrupted = HorseTripRepairPolicy.RequestFileName + HorseTripRepairPolicy.InterruptedSuffix;
        private static readonly string ResultFile = HorseTripRepairPolicy.ResultFileName;
        private static readonly string Superseded = HorseTripRepairPolicy.RequestFileName + HorseTripRepairPolicy.SupersededSuffix;

        private static string Archive(Folders folders, string name) => File.ReadAllText(Path.Combine(folders.UserData, name));

        private static string[] Files(params string[] names) => names.OrderBy(name => name, StringComparer.Ordinal).ToArray();

        // ===== PR-4 round 10 (owner instruction 2026-11-27a section 7) =====
        // SIMULATED EXTERNAL BOUNDARY: the POSIX primitives of the Linux/macOS branch (UnixFiles over IPosixFiles), simulated on this Windows
        // machine over real temporary files so the production C# logic of that branch runs. Mapping: an unnamed O_TMPFILE file is a file in a
        // private directory outside the repair folder (no entry in the folder) that disappears at Close, and LinkDescriptor is a hard link to it
        // (EEXIST when the name is taken); permission bits 0 are an exclusive share mode that still allows rename and delete (other processes
        // can neither read nor write the file, but can rename the name away, as in POSIX); symbolic links are registered names (an entry exists
        // in the folder; O_NOFOLLOW gives ELOOP, a following open reads the target); fstat/lstat identity is the volume serial and file index.
        // The real syscall semantics these stand for were observed on Linux by decomposition/pr4/r10/linux-fs-probe.txt (Perl, Debian 13,
        // WSL2 kernel 6.18); the C# libc calls themselves are not executed on Linux or Unity Mono here.
        internal sealed class SimulatedPosix : IPosixFiles
        {
            private sealed class Fd { public string Path; public FileStream Stream; public bool Directory, Unnamed; }
            private readonly Dictionary<int, Fd> fds = new Dictionary<int, Fd>();
            private int next = 3;
            private readonly string unnamedRoot = Path.Combine(Path.GetTempPath(), "kmc-sim-unnamed-" + Guid.NewGuid().ToString("N"));
            public readonly Dictionary<string, string> Links = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            public bool UnnamedUnsupported, DescriptorLinkFails;
            public Action<string> BeforeOpen;
            public int UnnamedOpened, ProtectedCreated, DescriptorLinks;
            public int LiveUnnamed => fds.Values.Count(f => f.Unnamed);
            public PosixFlags Flags => PosixFlags.Linux;
            public int Errno { get; private set; }
            private int Fail(int error) { Errno = error; return -1; }
            private int Add(Fd fd) { fds[next] = fd; Errno = 0; return next++; }
            private static string Full(string path) => Path.GetFullPath(path);
            public void Link(string name, string target) { File.WriteAllText(name, "(simulated symbolic link)"); Links[Full(name)] = target; }

            public int Open(string path, int flags, int mode)
            {
                BeforeOpen?.Invoke(path);
                string target;
                var linked = Links.TryGetValue(Full(path), out target) && File.Exists(path);
                var reparse = !linked && (File.Exists(path) || System.IO.Directory.Exists(path)) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
                if ((flags & Flags.Create) != 0)
                {
                    if (linked || File.Exists(path) || System.IO.Directory.Exists(path)) return Fail(Flags.EEXIST);
                    var stream = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite, mode == 0 ? FileShare.Delete : FileShare.Read | FileShare.Delete);
                    ProtectedCreated++;
                    return Add(new Fd { Path = path, Stream = stream });
                }
                if ((linked || reparse) && (flags & Flags.NoFollow) != 0) return Fail(Flags.ELOOP);
                if (linked) path = target;
                if (System.IO.Directory.Exists(path)) return Add(new Fd { Path = path, Directory = true });
                if (!File.Exists(path)) return Fail(Flags.ENOENT);
                try { return Add(new Fd { Path = path, Stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete) }); }
                catch (IOException) { return Fail(Flags.EACCES); }
            }
            public int OpenUnnamed(string directory)
            {
                if (UnnamedUnsupported) return Fail(Flags.ENOTSUP);
                System.IO.Directory.CreateDirectory(unnamedRoot);
                var path = Path.Combine(unnamedRoot, Guid.NewGuid().ToString("N"));
                UnnamedOpened++;
                return Add(new Fd { Path = path, Unnamed = true, Stream = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read | FileShare.Delete) });
            }
            public int Read(int fd, byte[] buffer, int count) { var f = fds[fd]; if (f.Directory) return Fail(21); return f.Stream.Read(buffer, 0, count); }
            public int Write(int fd, byte[] buffer, int count) { fds[fd].Stream.Write(buffer, 0, count); return count; }
            public int Fsync(int fd) { fds[fd].Stream.Flush(true); return 0; }
            public int Fchmod(int fd, int mode) => 0;   // readability is observable after Close in this simulation (the exclusive share mode ends there)
            public int Fstat(int fd, out PosixIdentity identity)
            {
                var f = fds[fd];
                identity = f.Directory ? new PosixIdentity { Mode = 0x4000 } : Identity(f.Stream.SafeFileHandle, 0x8000);
                return 0;
            }
            public int Lstat(string path, out PosixIdentity identity)
            {
                identity = default(PosixIdentity);
                if (Links.ContainsKey(Full(path)) && File.Exists(path)) { identity.Mode = 0xA000; return 0; }
                using (var handle = CreateFileW(path, 0x80, 7, IntPtr.Zero, 3, 0x02200000, IntPtr.Zero))
                {
                    if (handle.IsInvalid) return Fail(Flags.ENOENT);
                    identity = Identity(handle, System.IO.Directory.Exists(path) ? 0x4000u : 0x8000u);
                    return 0;
                }
            }
            public int LinkDescriptor(int fd, string newPath)
            {
                if (DescriptorLinkFails) return Fail(Flags.ENOENT);   // no /proc, or the descriptor link failed
                if (File.Exists(newPath) || System.IO.Directory.Exists(newPath)) return Fail(Flags.EEXIST);
                if (!CreateHardLinkW(newPath, fds[fd].Path, IntPtr.Zero)) return Fail(Flags.EEXIST);
                DescriptorLinks++;
                return 0;
            }
            public int Unlink(string path) { File.Delete(path); return 0; }
            public int Close(int fd)
            {
                var f = fds[fd]; fds.Remove(fd);
                f.Stream?.Dispose();
                if (f.Unnamed) File.Delete(f.Path);
                return 0;
            }
            private static PosixIdentity Identity(Microsoft.Win32.SafeHandles.SafeFileHandle handle, uint mode)
            {
                FileInformation info;
                if (!GetFileInformationByHandle(handle, out info)) throw new IOException("simulation: file information unavailable");
                return new PosixIdentity { Device = info.VolumeSerialNumber, Inode = ((ulong)info.FileIndexHigh << 32) | info.FileIndexLow, Mode = mode };
            }
            [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
            private struct FileInformation
            {
                public uint FileAttributes; public System.Runtime.InteropServices.ComTypes.FILETIME Created, Accessed, Written;
                public uint VolumeSerialNumber, FileSizeHigh, FileSizeLow, NumberOfLinks, FileIndexHigh, FileIndexLow;
            }
            [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
            private static extern bool GetFileInformationByHandle(Microsoft.Win32.SafeHandles.SafeFileHandle file, out FileInformation information);
            [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
            private static extern Microsoft.Win32.SafeHandles.SafeFileHandle CreateFileW(string name, uint access, uint share, IntPtr security, uint disposition, uint flags, IntPtr template);
            [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
            private static extern bool CreateHardLinkW(string link, string existing, IntPtr security);
        }

        private static void Posix(SimulatedPosix sim, Action body)
        {
            HorseTripRepairFiles.ForcePathnameOperations = true; UnixFiles.Calls = sim;
            try { body(); }
            finally { HorseTripRepairFiles.ForcePathnameOperations = false; UnixFiles.Calls = null; }
        }

        // The Unix retirement keeps the verified, retired claim as ONE completed record C.<id8>.<identity16>.settled (it never deletes by a
        // pathname); it is the claim's own file, not a name of the archive. These helpers separate it from the other files.
        private static string[] Unsettled(Folders folders) => folders.UserFiles().Where(name => !name.EndsWith(HorseTripRepairFiles.SettledSuffix, StringComparison.Ordinal)).ToArray();
        private static bool CompletedRecordOf(Folders folders, string claimText)
        {
            var settled = Directory.GetFiles(folders.UserData, "*" + HorseTripRepairFiles.SettledSuffix);
            return settled.Length == 1 && File.ReadAllText(settled[0]) == claimText && !SameFileOnDisk(settled[0], Directory.GetFiles(folders.UserData, Request + "*" + HorseTripRepairPolicy.AppliedSuffix).Single());
        }
        private static bool SameFileOnDisk(string first, string second)
        {
            var sim = new SimulatedPosix(); PosixIdentity a, b;
            return sim.Lstat(first, out a) == 0 && sim.Lstat(second, out b) == 0 && a.SameFile(b);
        }

        private static string TryModifyInPlace(string path)
        {
            if (!File.Exists(path)) return "absent";
            try { using (var editor = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete)) editor.WriteByte(88); return "MODIFIED"; }
            catch (IOException) { return "refused"; }
            catch (UnauthorizedAccessException) { return "refused"; }
        }

        // R5-F1/R9-F3 (simulated POSIX boundary): three mechanism paths. At the publication boundary (after the complete write and flush, before
        // the link or fchmod) the folder holds no staging entry; the terminal name is absent (unnamed file) or exists but can be neither read nor
        // changed in place (protected create); the first readable terminal bytes are A's; one archive, one removal, nothing else left. A
        // replacement of the terminal name during the protected create is detected: the replacement is preserved and the retry publishes A.
        private static void UnixPublicationIsProtected()
        {
            var forA = HorseTripRepairPolicy.FormatRequestLine("horse-A");
            foreach (var path in new[] { "unnamed file + descriptor link (Linux)", "descriptor link failed -> protected create", "no unnamed file (macOS) -> protected create" })
            {
                var sim = new SimulatedPosix { DescriptorLinkFails = path.StartsWith("descriptor"), UnnamedUnsupported = path.StartsWith("no unnamed") };
                Posix(sim, () =>
                {
                    using (var folders = new Folders())
                    {
                        var host = new Host(folders); var session = new HorseTripRepairSession(); host.Loaded.Add(LegacyHorse("horse-A"));
                        var plain = Path.Combine(folders.UserData, Applied);
                        folders.Request(forA);
                        string[] atBoundary = null; string readable = null, inPlace = null, firstReadable = null;
                        host.files.BeforeCommit = () => { atBoundary = folders.UserFiles(); readable = ObserveShared(plain); inPlace = TryModifyInPlace(plain); };
                        host.files.AfterPublishRename = terminal => firstReadable = ObserveShared(terminal);
                        var decision = Run(host, session);
                        var protectedCreate = !path.StartsWith("unnamed");
                        var expectedBoundary = protectedCreate ? Files(Applied, Claimed, ResultFile) : Files(Claimed, ResultFile);
                        Console.WriteLine("    R5-F1 (" + path + "): at the publication boundary the folder held [" + string.Join(", ", atBoundary ?? new string[0]) + "], terminal " +
                            readable + ", in-place change " + inPlace + "; mechanism " + host.files.LastPublicationMechanism + "; first readable " + (firstReadable == forA ? "A's complete bytes" : "'" + firstReadable + "'") +
                            "; files after [" + string.Join(", ", folders.UserFiles()) + "], live unnamed " + sim.LiveUnnamed + ", removals " + host.Removals);
                        TestRunner.True(decision == HorseTripRepairDecision.Apply && atBoundary != null && atBoundary.SequenceEqual(expectedBoundary) &&
                            readable == (protectedCreate ? "not readable" : "absent") && inPlace == (protectedCreate ? "refused" : "absent") &&
                            host.files.LastPublicationMechanism == (protectedCreate ? "protected-create" : "unnamed-link") && firstReadable == forA && Archive(folders, Applied) == forA &&
                            Unsettled(folders).SequenceEqual(Files(Applied, ResultFile)) && CompletedRecordOf(folders, forA) && sim.LiveUnnamed == 0 && host.Removals == 1,
                            "R5-F1 (" + path + "): a staging entry existed, partial or changed bytes were readable, the mechanism differed or something was left behind.");
                    }
                });
            }
            // A replacement of the terminal name during the protected create (the name renamed away and another file put there before fchmod).
            var replacing = new SimulatedPosix { UnnamedUnsupported = true };
            Posix(replacing, () =>
            {
                using (var folders = new Folders())
                {
                    var host = new Host(folders); var session = new HorseTripRepairSession(); host.Loaded.Add(LegacyHorse("horse-A"));
                    var plain = Path.Combine(folders.UserData, Applied);
                    var aside = Path.Combine(folders.Root, "renamed-away");
                    const string foreign = "someone else's archive";
                    folders.Request(forA);
                    host.files.BeforeCommit = () => { host.files.BeforeCommit = null; File.Move(plain, aside); File.WriteAllText(plain, foreign); };
                    var failed = Throws(() => Run(host, session));
                    var foreignKept = File.ReadAllText(plain) == foreign;
                    var retried = Run(host, session);
                    var archivesOfA = Directory.GetFiles(folders.UserData, Request + "*" + HorseTripRepairPolicy.AppliedSuffix).Count(f => File.ReadAllText(f) == forA);
                    Console.WriteLine("    R5-F1 (replacement during the protected create): refused=" + failed + ", replacement preserved=" + foreignKept + "; retry " + retried + ", A's archives=" + archivesOfA + ", removals " + host.Removals);
                    TestRunner.True(failed && foreignKept && File.ReadAllText(plain) == foreign && retried == HorseTripRepairDecision.FinishedApplied && archivesOfA == 1 && host.Removals == 1 &&
                        !File.Exists(Path.Combine(folders.UserData, Claimed)),
                        "R5-F1: a replaced terminal name was reported as A's archive, the replacement was touched, or the retry did not publish A once.");
                }
            });
        }

        // R9-F3 (simulated POSIX boundary): a directory occupies the terminal name across three polls; the folder never gains a file (a foreign
        // file named like the old staging files is preserved), and no unnamed file stays open. With the obstruction gone: one archive, one removal.
        private static void UnixStagingStaysBounded()
        {
            var forA = HorseTripRepairPolicy.FormatRequestLine("horse-A");
            foreach (var unnamed in new[] { true, false })
            {
                var sim = new SimulatedPosix { UnnamedUnsupported = !unnamed };
                Posix(sim, () =>
                {
                    using (var folders = new Folders())
                    {
                        var host = new Host(folders); var session = new HorseTripRepairSession(); host.Loaded.Add(LegacyHorse("horse-A"));
                        var plain = Path.Combine(folders.UserData, Applied);
                        var foreignName = Claimed + ".0123abcd.pending";
                        File.WriteAllText(Path.Combine(folders.UserData, foreignName), "foreign");
                        Directory.CreateDirectory(plain);
                        folders.Request(forA);
                        var counts = new List<int>(); var threw = 0;
                        for (var poll = 0; poll < 3; poll++) { if (Throws(() => Run(host, session))) threw++; counts.Add(folders.UserFiles().Length); }
                        var during = folders.UserFiles();
                        Directory.Delete(plain);
                        var finished = Run(host, session);
                        var label = unnamed ? "unnamed-file link" : "protected create";
                        Console.WriteLine("    R9-F3 (" + label + "): 3 obstructed polls threw " + threw + ", folder file counts " + string.Join("/", counts) + " [" + string.Join(", ", during) +
                            "], live unnamed " + sim.LiveUnnamed + "; then " + finished + ", files [" + string.Join(", ", folders.UserFiles()) + "], removals " + host.Removals);
                        TestRunner.True(threw == 3 && counts.All(c => c == 3) && during.SequenceEqual(Files(Claimed, foreignName, ResultFile)) && sim.LiveUnnamed == 0 &&
                            finished == HorseTripRepairDecision.FinishedApplied && Archive(folders, Applied) == forA && host.Removals == 1 &&
                            Unsettled(folders).SequenceEqual(Files(Applied, foreignName, ResultFile)) && CompletedRecordOf(folders, forA) && File.ReadAllText(Path.Combine(folders.UserData, foreignName)) == "foreign",
                            "R9-F3 (" + label + "): staging grew across failed polls, an alias survived, the foreign file changed, or the retry did not publish once.");
                    }
                });
            }
        }

        // R9-F1 (Windows, production handle branch): a writer holding the request (sharing read and delete) after its first horse line; polls
        // while it is held decide nothing and remove nothing; the second line is appended and the writer closes; the completed two-horse request
        // is refused as malformed with zero removals. Control: a read-only holder does not prevent the decision.
        private static void WindowsWriterIsExcluded()
        {
            if (!VerifiedFileOperations.Supported) { Console.WriteLine("    R9-F1: not Windows; skipped"); return; }
            using (var folders = new Folders())
            {
                var host = new Host(folders); var session = new HorseTripRepairSession();
                var a = LegacyHorse("horse-A"); var b = LegacyHorse("horse-B"); host.Loaded.Add(a); host.Loaded.Add(b);
                var path = Path.Combine(folders.UserData, Request);
                var heldPolls = 0; var outcomes = new List<string>();
                using (var writer = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read | FileShare.Delete))
                {
                    var first = System.Text.Encoding.UTF8.GetBytes(HorseTripRepairPolicy.FormatRequestLine("horse-A") + "\r\n");
                    writer.Write(first, 0, first.Length); writer.Flush(true);
                    for (var poll = 0; poll < 2; poll++)
                    {
                        string outcome;
                        try { outcome = Run(host, session).ToString(); } catch (IOException e) { outcome = "IOException(" + e.Message.Substring(0, Math.Min(40, e.Message.Length)) + ")"; }
                        outcomes.Add(outcome); heldPolls++;
                    }
                    var second = System.Text.Encoding.UTF8.GetBytes(HorseTripRepairPolicy.FormatRequestLine("horse-B") + "\r\n");
                    writer.Write(second, 0, second.Length); writer.Flush(true);
                }
                var removalsWhileHeld = host.Removals;
                var completed = Run(host, session);
                Console.WriteLine("    R9-F1: polls while the writer held the request: " + string.Join("; ", outcomes) + " (removals " + removalsWhileHeld + "); completed request -> " + completed +
                    ", removals " + host.Removals + ", files [" + string.Join(", ", folders.UserFiles()) + "]");
                TestRunner.True(outcomes.All(o => o.StartsWith("IOException", StringComparison.Ordinal)) && removalsWhileHeld == 0 && completed == HorseTripRepairDecision.Malformed &&
                    host.Removals == 0 && Candidates(a).Count == 1 && Candidates(b).Count == 1 && folders.UserFiles().SequenceEqual(Files(Refused, ResultFile)),
                    "R9-F1: a request still being written was decided or removed something, or the completed two-horse request was not refused.");
            }
            using (var folders = new Folders())
            {
                var host = new Host(folders); var session = new HorseTripRepairSession(); host.Loaded.Add(LegacyHorse("horse-A"));
                folders.Request(HorseTripRepairPolicy.FormatRequestLine("horse-A"));
                HorseTripRepairDecision decided;
                using (new FileStream(Path.Combine(folders.UserData, Request), FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                {
                    try { decided = Run(host, session); } catch (IOException) { decided = HorseTripRepairDecision.Pending; }
                }
                var removalsWhileRead = host.Removals;
                var finished = Run(host, session);
                Console.WriteLine("    R9-F1 control: with a read-only holder the request was decided (removals " + removalsWhileRead + ", first " + decided + "), then " + finished);
                TestRunner.True(removalsWhileRead == 1 && host.Removals == 1 && Archive(folders, Applied) == HorseTripRepairPolicy.FormatRequestLine("horse-A"),
                    "R9-F1 control: a read-only holder prevented the decision or caused a second removal.");
            }
        }

        // R9-F2 (simulated POSIX boundary): at the moment the claim (undecided, just renamed) or a recovery entry is opened, its name is replaced by
        // a symbolic link to a file holding a request for the loaded horse-B. The no-follow open refuses it: no removal, no archive of B's
        // bytes, B's fact intact. Controls: the same flows with ordinary files apply A, and recover the regular .retiring entry as Interrupted.
        private static void UnixReadsBindTheDescriptor()
        {
            var forA = HorseTripRepairPolicy.FormatRequestLine("horse-A");
            var forB = HorseTripRepairPolicy.FormatRequestLine("horse-B");
            foreach (var substitute in new[] { true, false })
            {
                var sim = new SimulatedPosix();
                Posix(sim, () =>
                {
                    using (var folders = new Folders())
                    {
                        var host = new Host(folders); var session = new HorseTripRepairSession();
                        var b = LegacyHorse("horse-B"); host.Loaded.Add(LegacyHorse("horse-A")); host.Loaded.Add(b);
                        var claim = Path.Combine(folders.UserData, Claimed);
                        var target = Path.Combine(folders.Root, "elsewhere.txt"); File.WriteAllText(target, forB);
                        folders.Request(forA);
                        var swapped = false;
                        if (substitute) sim.BeforeOpen = path =>
                        {
                            if (swapped || !string.Equals(Path.GetFullPath(path), Path.GetFullPath(claim), StringComparison.OrdinalIgnoreCase) || !File.Exists(claim)) return;
                            swapped = true; File.Move(claim, Path.Combine(folders.Root, "original-claim")); sim.Link(claim, target);
                        };
                        string outcome;
                        try { outcome = Run(host, session).ToString(); } catch (HorseTripIneligibleEntryException) { outcome = "refused (ineligible entry)"; }
                        var archivedB = Directory.GetFiles(folders.UserData).Count(f => File.ReadAllText(f) == forB);
                        Console.WriteLine("    R9-F2 undecided claim (" + (substitute ? "link substituted at the open" : "ordinary control") + "): " + outcome + ", removals " + string.Join(",", host.RemovedFrom) +
                            ", archives of B's bytes " + archivedB);
                        if (substitute)
                            TestRunner.True(swapped && outcome == "refused (ineligible entry)" && host.Removals == 0 && Candidates(b).Count == 1 && archivedB == 0,
                                "R9-F2: a link substituted at the claim's open supplied a decision, a removal or an archive copy.");
                        else
                            TestRunner.True(outcome == "Apply" && host.RemovedFrom.SequenceEqual(new[] { "horse-A" }) && Archive(folders, Applied) == forA, "R9-F2 control: the ordinary claim did not apply.");
                    }
                });
            }
            foreach (var substitute in new[] { true, false })
            {
                var sim = new SimulatedPosix();
                Posix(sim, () =>
                {
                    using (var folders = new Folders())
                    {
                        var host = new Host(folders); var b = LegacyHorse("horse-B"); host.Loaded.Add(b);
                        var retiring = Path.Combine(folders.UserData, Claimed + ".0a1b2c3d" + HorseTripRepairFiles.RetiringSuffix);
                        var target = Path.Combine(folders.Root, "elsewhere.txt"); File.WriteAllText(target, forB);
                        File.WriteAllText(retiring, forB);
                        var swapped = false;
                        if (substitute) sim.BeforeOpen = path =>
                        {
                            if (swapped || !string.Equals(Path.GetFullPath(path), Path.GetFullPath(retiring), StringComparison.OrdinalIgnoreCase)) return;
                            swapped = true; File.Delete(retiring); sim.Link(retiring, target);
                        };
                        var outcome = Run(host, new HorseTripRepairSession());
                        var interrupted = File.Exists(Path.Combine(folders.UserData, Interrupted)) ? Archive(folders, Interrupted) : null;
                        Console.WriteLine("    R9-F2 recovery (" + (substitute ? "link substituted at the open" : "ordinary control") + "): " + outcome + ", Interrupted archive " +
                            (interrupted == null ? "none" : interrupted == forB ? "of B's bytes" : "'" + interrupted + "'") + ", entry kept " + File.Exists(retiring) + ", removals " + host.Removals);
                        if (substitute)
                            TestRunner.True(swapped && outcome == HorseTripRepairDecision.NoRequest && interrupted == null && File.Exists(retiring) && host.Removals == 0 && Candidates(b).Count == 1,
                                "R9-F2: a link substituted at a recovery entry's open was recovered or copied.");
                        else
                            TestRunner.True(outcome == HorseTripRepairDecision.Interrupted && interrupted == forB && !File.Exists(retiring) && host.Removals == 0 && Candidates(b).Count == 1,
                                "R9-F2 control: the ordinary stranded .retiring entry was not recovered as its own Interrupted disposition.");
                    }
                });
            }
        }

        // R9-F4: a fresh session over an older (round-8) completed record C.<32 hex>.settled, its valid Applied result and archive, and a backup
        // matching the settled glob: names and bytes stay exactly, nothing is recovered, no Interrupted archive, nothing removed. Both branches.
        private static void RecoveryIsConservative()
        {
            var forA = HorseTripRepairPolicy.FormatRequestLine("horse-A");
            var forB = HorseTripRepairPolicy.FormatRequestLine("horse-B");
            foreach (var pathname in new[] { false, true })
            {
                Action body = () =>
                {
                    using (var folders = new Folders())
                    {
                        var host = new Host(folders); var b = LegacyHorse("horse-B"); host.Loaded.Add(LegacyHorse("horse-A")); host.Loaded.Add(b);
                        var older = Claimed + "." + Guid.NewGuid().ToString("N") + HorseTripRepairFiles.SettledSuffix;
                        var backup = Claimed + ".backup" + HorseTripRepairFiles.SettledSuffix;
                        File.WriteAllText(Path.Combine(folders.UserData, older), forA);
                        File.WriteAllText(Path.Combine(folders.UserData, Applied), forA);
                        File.WriteAllText(Path.Combine(folders.UserData, ResultFile), "2026-11-26 10:00:00Z\r\n" + HorseTripRepairPolicy.AppliedResult("horse-A") + "\r\n");
                        File.WriteAllText(Path.Combine(folders.UserData, backup), forB);
                        var before = folders.UserFiles().Select(name => name + ":" + File.ReadAllText(Path.Combine(folders.UserData, name))).ToArray();
                        var outcome = Run(host, new HorseTripRepairSession());
                        var after = folders.UserFiles().Select(name => name + ":" + File.ReadAllText(Path.Combine(folders.UserData, name))).ToArray();
                        var label = pathname ? "simulated POSIX branch" : "Windows handle branch";
                        Console.WriteLine("    R9-F4 (" + label + "): " + outcome + "; files [" + string.Join(", ", folders.UserFiles()) + "], unchanged " + before.SequenceEqual(after) + ", removals " + host.Removals);
                        TestRunner.True(outcome == HorseTripRepairDecision.NoRequest && before.SequenceEqual(after) && !File.Exists(Path.Combine(folders.UserData, Interrupted)) &&
                            host.Removals == 0 && Candidates(b).Count == 1,
                            "R9-F4 (" + label + "): an unrecognized or completed settled record was treated as pending work.");
                    }
                };
                if (pathname) Posix(new SimulatedPosix(), body); else body();
            }
        }

        private static void CandidatePredicate()
        {
            TestRunner.True(HorseTripRepairPolicy.IsCandidate(true, true, true, true), "The exact sourceless stock grant on the Horse was not a candidate.");
            TestRunner.True(!HorseTripRepairPolicy.IsCandidate(true, true, true, false), "A grant with a source, item or cutscene was a candidate.");
            TestRunner.True(!HorseTripRepairPolicy.IsCandidate(true, false, true, true), "A different GUID with the same shape was a candidate.");
            TestRunner.True(!HorseTripRepairPolicy.IsCandidate(true, true, false, true), "The stock GUID with a different maneuver or category was a candidate.");
            TestRunner.True(!HorseTripRepairPolicy.IsCandidate(false, true, true, true), "A dog, wolf or other unit's TrippingBite was a candidate.");
        }

        private static void RequestParsing()
        {
            TestRunner.Equal("horse=ab12", HorseTripRepairPolicy.FormatRequestLine("ab12"), "The request line format changed.");
            TestRunner.Equal("ab12", HorseTripRepairPolicy.ParseRequest("# copied from the log\r\n\r\n  horse=ab12  \r\n# my second horse stays\r\n"),
                "One valid ID with comments and blank lines was not read.");
            TestRunner.Equal("ab12", HorseTripRepairPolicy.ParseRequest(HorseTripRepairPolicy.FormatRequestLine("ab12")), "The formatted line did not round-trip.");
            foreach (var bad in new[] { "horse=ab12\r\nhorse=cd34", "horse=ab12\nhorse=ab12", "horse=ab12\r\nremove all", "horse=ab12\r\n  x",
                "", "ab12", "horse=", "horse=a b", "horse=a=b", "horse=a#b", "remove all", "\r\n# only a comment\r\n" })
                TestRunner.True(HorseTripRepairPolicy.ParseRequest(bad) == null, "A malformed or ambiguous request selected a horse: '" + bad + "'.");
            TestRunner.True(HorseTripRepairPolicy.ParseRequest(null) == null, "A missing request selected a horse.");
            // Both ambiguous inputs end in a refusal with zero removals, even with both horses loaded.
            foreach (var ambiguous in new[] { "horse=horse-1\r\nhorse=horse-2\r\n", "horse=horse-1\r\nplease also fix horse-2\r\n" })
                using (var folders = new Folders())
                {
                    var host = new Host(folders); var a = LegacyHorse("horse-1"); var b = LegacyHorse("horse-2");
                    host.Loaded.Add(a); host.Loaded.Add(b); folders.Request(ambiguous);
                    TestRunner.True(Run(host, new HorseTripRepairSession()) == HorseTripRepairDecision.Malformed && host.Removals == 0 &&
                        Candidates(a).Count == 1 && Candidates(b).Count == 1 && folders.UserFiles().SequenceEqual(Files(Refused, ResultFile)),
                        "An ambiguous request removed something or was not refused: " + ambiguous);
                }
        }

        private static void ExactRequestApplies()
        {
            using (var folders = new Folders())
            {
                var stagedBefore = folders.StagedDigest();
                var host = new Host(folders);
                var session = new HorseTripRepairSession();
                var horse = LegacyHorse("horse-1");
                var other = LegacyHorse("horse-2");
                host.Loaded.Add(horse); host.Loaded.Add(other);
                // Discovery writes nothing (candidates are logged only).
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.NoRequest && folders.UserFiles().Length == 0,
                    "Candidate discovery wrote into the user-data folder.");
                var target = horse.Facts[0];
                folders.Request("# from the log\r\n" + HorseTripRepairPolicy.FormatRequestLine("horse-1") + "\r\n");
                TestRunner.Equal(HorseTripRepairDecision.Apply, Run(host, session), "The exact request was not applied.");
                TestRunner.True(host.Removals == 1 && !horse.Facts.Contains(target) && horse.Facts.Count == 3 &&
                    horse.Facts.Select(fact => fact.Name).SequenceEqual(new[] { "NaturalArmor7", "TripDefenseFourLegs", "AnimalCompanionClassFeature" }),
                    "Something other than exactly the requested TrippingBite changed on that horse.");
                TestRunner.True(Candidates(other).Count == 1 && other.Facts.Count == 4, "The other horse was touched.");
                TestRunner.True(folders.UserFiles().SequenceEqual(Files(Applied, ResultFile)) && folders.Result().Contains("horse-1") &&
                    folders.Result().Contains("reload") && session.PendingReports == 0, "The applied request was not closed with its result and recovery note.");
                // Consumed: a later update and a reload of the same save (new loaded objects, same IDs) apply nothing.
                TestRunner.Equal(HorseTripRepairDecision.NoRequest, Run(host, session), "A consumed request was read again.");
                host.Loaded.Clear(); host.Loaded.Add(LegacyHorse("horse-1"));
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.NoRequest && host.Removals == 1, "A consumed request was re-applied after a reload.");
                TestRunner.Equal(stagedBefore, folders.StagedDigest(), "The guarded-style staged Mods folder changed.");
            }
            // No user-data folder: no request is read and nothing is created anywhere.
            using (var folders = new Folders(false))
            {
                var stagedBefore = folders.StagedDigest();
                var host = new Host(folders); host.Loaded.Add(LegacyHorse("horse-1"));
                TestRunner.True(Run(host, new HorseTripRepairSession()) == HorseTripRepairDecision.NoRequest && !Directory.Exists(folders.UserData) &&
                    host.Removals == 0 && folders.StagedDigest() == stagedBefore, "A missing user-data folder was created or read.");
            }
        }

        private static void NegativeCases()
        {
            using (var folders = new Folders())
            {
                var session = new HorseTripRepairSession();
                // No request: a legacy-ordered sourceless copy is only a candidate; nothing is removed.
                var host = new Host(folders);
                var legacy = LegacyHorse("horse-1");
                host.Loaded.Add(legacy);
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.NoRequest && host.Removals == 0 &&
                    Candidates(legacy).Count == 1 && legacy.Facts.Count == 4, "A candidate was removed without the owner's request.");
                // An independently granted copy in exactly the legacy visible order, recording its source: not a candidate, so
                // even an exact request for that horse is refused and changes nothing.
                var sourced = LegacyHorse("horse-3");
                sourced.Facts[0].Sourceless = false;
                host.Loaded.Add(sourced);
                folders.Request(HorseTripRepairPolicy.FormatRequestLine("horse-3"));
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.NoCandidate && host.Removals == 0 &&
                    sourced.Facts.Count == 4 && folders.UserFiles().Contains(Refused), "An independently granted (sourced) fact was removed.");
            }
            // Same GUID, different maneuver or category; a different GUID with the same shape: not candidates.
            foreach (var mutate in new Action<Fact>[] { fact => fact.BiteTripShape = false, fact => fact.StockIdentity = false })
                using (var folders = new Folders())
                {
                    var horse = LegacyHorse("horse-4"); mutate(horse.Facts[0]);
                    var h = new Host(folders); h.Loaded.Add(horse); folders.Request(HorseTripRepairPolicy.FormatRequestLine("horse-4"));
                    TestRunner.True(Run(h, new HorseTripRepairSession()) == HorseTripRepairDecision.NoCandidate && h.Removals == 0 && horse.Facts.Count == 4,
                        "A fact that is not the exact stock grant was removed.");
                }
            // A dog or wolf (not the KMC Horse blueprint) keeps its legitimate TrippingBite even if its ID is requested.
            using (var folders = new Folders())
            {
                var wolf = LegacyHorse("wolf-1"); wolf.ExactKmcHorse = false;
                var wh = new Host(folders); wh.Loaded.Add(wolf); folders.Request(HorseTripRepairPolicy.FormatRequestLine("wolf-1"));
                TestRunner.True(Run(wh, new HorseTripRepairSession()) == HorseTripRepairDecision.NoCandidate && wh.Removals == 0 && wolf.Facts.Count == 4,
                    "A wolf's or dog's TrippingBite was removed.");
            }
            // Two sourceless stock copies: the inherited one cannot be singled out; refused, nothing removed.
            using (var folders = new Folders())
            {
                var doubled = LegacyHorse("horse-5"); doubled.Facts.Add(StockGrant("TrippingBite (second)"));
                var dh = new Host(folders); dh.Loaded.Add(doubled); folders.Request(HorseTripRepairPolicy.FormatRequestLine("horse-5"));
                TestRunner.True(Run(dh, new HorseTripRepairSession()) == HorseTripRepairDecision.Ambiguous && dh.Removals == 0 && doubled.Facts.Count == 5 &&
                    folders.UserFiles().SequenceEqual(Files(Refused, ResultFile)), "An ambiguous horse was changed.");
            }
            // A malformed request is refused; nothing is removed.
            using (var folders = new Folders())
            {
                var mh = new Host(folders); var m = LegacyHorse("horse-6"); mh.Loaded.Add(m); folders.Request("please fix my horse");
                TestRunner.True(Run(mh, new HorseTripRepairSession()) == HorseTripRepairDecision.Malformed && mh.Removals == 0 && m.Facts.Count == 4 &&
                    folders.UserFiles().Contains(Refused), "A malformed request changed a horse.");
            }
        }

        private static void PendingAndRetry()
        {
            using (var folders = new Folders())
            {
                var host = new Host(folders);
                var session = new HorseTripRepairSession();
                var horse = LegacyHorse("horse-1");
                folders.Request(HorseTripRepairPolicy.FormatRequestLine("horse-1"));
                // A wrong horse ID (or that horse not loaded in the party): kept for later, nothing removed.
                host.Loaded.Add(LegacyHorse("horse-9"));
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.HorseNotLoaded && folders.UserFiles().SequenceEqual(Files(Request)) &&
                    host.Removals == 0, "A request for an unloaded horse was consumed or changed another horse.");
                host.Loaded.Add(horse);
                // The write gate closed (save, area transition, restoration, load): pending, the horse is not even read.
                TestRunner.True(Run(host, session, false) == HorseTripRepairDecision.Pending && folders.UserFiles().SequenceEqual(Files(Request)) &&
                    host.Removals == 0 && Candidates(horse).Count == 1, "A closed write gate let the repair run.");
                // A removal that throws while the fact is still present releases the claim; the retry applies it once.
                host.FailRemovalBefore = true;
                TestRunner.True(Throws(() => Run(host, session)) && folders.UserFiles().SequenceEqual(Files(Request)) && Candidates(horse).Count == 1 &&
                    session.PendingReports == 0, "A failure before removal consumed the request or changed the horse.");
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.Apply && host.Removals == 1 && Candidates(horse).Count == 0 &&
                    folders.UserFiles().SequenceEqual(Files(Applied, ResultFile)), "The retry did not apply exactly once.");
            }
        }

        private static void ReportingFailures()
        {
            // After RemoveFact: the fact is gone and the removal step throws; the outcome is applied, never a refusal.
            using (var folders = new Folders())
            {
                var host = new Host(folders); var session = new HorseTripRepairSession(); var horse = LegacyHorse("horse-1"); host.Loaded.Add(horse);
                folders.Request(HorseTripRepairPolicy.FormatRequestLine("horse-1"));
                host.FailRemovalAfter = true;
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.Apply && host.Removals == 1 && Candidates(horse).Count == 0 &&
                    folders.UserFiles().SequenceEqual(Files(Applied, ResultFile)) && folders.Result().Contains("Applied"),
                    "A failure after RemoveFact was not finished as applied.");
            }
            // The claim rename fails: nothing changes, the request stays, the retry applies once.
            using (var folders = new Folders())
            {
                var host = new Host(folders); var session = new HorseTripRepairSession(); var horse = LegacyHorse("horse-1"); host.Loaded.Add(horse);
                folders.Request(HorseTripRepairPolicy.FormatRequestLine("horse-1"));
                host.FailClaim = true;
                TestRunner.True(Throws(() => Run(host, session)) && host.Removals == 0 && Candidates(horse).Count == 1 &&
                    folders.UserFiles().SequenceEqual(Files(Request)), "A failed claim changed something.");
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.Apply && host.Removals == 1 &&
                    folders.UserFiles().SequenceEqual(Files(Applied, ResultFile)), "The retry after a failed claim did not apply once.");
            }
            // The result write fails after the removal: the claim stays; the retry finishes as applied with no second removal.
            using (var folders = new Folders())
            {
                var host = new Host(folders); var session = new HorseTripRepairSession(); var horse = LegacyHorse("horse-1"); host.Loaded.Add(horse);
                folders.Request(HorseTripRepairPolicy.FormatRequestLine("horse-1"));
                host.FailResultWrite = true;
                TestRunner.True(Throws(() => Run(host, session)) && host.Removals == 1 && folders.UserFiles().SequenceEqual(Files(Claimed)) &&
                    session.PendingReports == 1, "A failed result write lost the claim or the applied record.");
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.FinishedApplied && host.Removals == 1 &&
                    folders.UserFiles().SequenceEqual(Files(Applied, ResultFile)) && folders.Result().Contains("Applied") && session.PendingReports == 0,
                    "The retry did not finish the applied report, or removed again.");
            }
            // The final rename fails after the result was written: the retry finishes as applied, never refused.
            using (var folders = new Folders())
            {
                var host = new Host(folders); var session = new HorseTripRepairSession(); var horse = LegacyHorse("horse-1"); host.Loaded.Add(horse);
                folders.Request(HorseTripRepairPolicy.FormatRequestLine("horse-1"));
                host.FailClose = true;
                TestRunner.True(Throws(() => Run(host, session)) && host.Removals == 1 && folders.UserFiles().SequenceEqual(Files(Claimed, ResultFile)),
                    "A failed final rename lost the claim.");
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.FinishedApplied && host.Removals == 1 &&
                    folders.UserFiles().SequenceEqual(Files(Applied, ResultFile)) && !folders.Result().Contains("Refused"),
                    "The retry after a failed final rename did not finish as applied.");
            }
        }

        private static void StrandedClaims()
        {
            // Same session, reload of a backed-up save between the failure and the retry: the fact is back on a new horse object,
            // but this request already removed it once, so the retry only finishes reporting (single use).
            using (var folders = new Folders())
            {
                var host = new Host(folders); var session = new HorseTripRepairSession(); host.Loaded.Add(LegacyHorse("horse-1"));
                folders.Request(HorseTripRepairPolicy.FormatRequestLine("horse-1"));
                host.FailResultWrite = true;
                TestRunner.True(Throws(() => Run(host, session)) && host.Removals == 1, "The first attempt did not remove once.");
                var reloaded = LegacyHorse("horse-1");
                host.Loaded.Clear(); host.Loaded.Add(reloaded);
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.FinishedApplied && host.Removals == 1 && Candidates(reloaded).Count == 1 &&
                    folders.UserFiles().SequenceEqual(Files(Applied, ResultFile)) && folders.Result().Contains("write a new request"),
                    "A reload after a stranded claim removed the fact a second time.");
                // A new request for the reloaded horse is an explicit new decision and applies once.
                folders.Request(HorseTripRepairPolicy.FormatRequestLine("horse-1"));
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.Apply && host.Removals == 2 && Candidates(reloaded).Count == 0,
                    "A new explicit request after the reload was not applied once.");
            }
            // A game restart mid-request (a claim with no record in this session): closed as interrupted, nothing removed.
            using (var folders = new Folders())
            {
                var host = new Host(folders); var horse = LegacyHorse("horse-1"); host.Loaded.Add(horse);
                File.WriteAllText(Path.Combine(folders.UserData, Claimed), HorseTripRepairPolicy.FormatRequestLine("horse-1"));
                TestRunner.True(Run(host, new HorseTripRepairSession()) == HorseTripRepairDecision.Interrupted && host.Removals == 0 &&
                    Candidates(horse).Count == 1 && folders.UserFiles().SequenceEqual(Files(Interrupted, ResultFile)) &&
                    folders.Result().Contains("Interrupted"), "A stranded claim from an earlier session removed something or was not closed.");
            }
        }

        // Review R5-F1 counterexample through the production policy and the production file host: the owner replaces a valid
        // request for horse A with one for horse B at the exact moment between the policy's observation and its claim rename.
        private static void ReplacedBeforeClaim()
        {
            var forA = HorseTripRepairPolicy.FormatRequestLine("horse-A") + "\r\n";
            var forB = "# replaced\r\n" + HorseTripRepairPolicy.FormatRequestLine("horse-B") + "\r\n";
            // Applicable: A observed, B claimed. Only B's fact is removed; archive, result and removal log all name B.
            using (var folders = new Folders())
            {
                var host = new Host(folders); var session = new HorseTripRepairSession();
                var a = LegacyHorse("horse-A"); var b = LegacyHorse("horse-B"); host.Loaded.Add(a); host.Loaded.Add(b);
                folders.Request(forA);
                host.files.BeforeClaim = () => folders.Request(forB);
                TestRunner.Equal(HorseTripRepairDecision.Apply, Run(host, session), "The replaced request was not applied.");
                TestRunner.True(host.RemovedFrom.SequenceEqual(new[] { "horse-B" }) && Candidates(b).Count == 0 && Candidates(a).Count == 1 && a.Facts.Count == 4,
                    "The fact removed was not the one named by the claimed bytes: " + string.Join(",", host.RemovedFrom.ToArray()));
                TestRunner.True(folders.UserFiles().SequenceEqual(Files(Applied, ResultFile)) && Archive(folders, Applied) == forB &&
                    folders.Result().Contains("horse-B") && !folders.Result().Contains("horse-A"),
                    "The archive, result and removal disagree about which request was claimed.");
            }
            // Refusal: a valid A replaced by a malformed B before the claim. Nothing is removed; the refused archive is B's bytes.
            using (var folders = new Folders())
            {
                var host = new Host(folders); var a = LegacyHorse("horse-A"); host.Loaded.Add(a);
                folders.Request(forA);
                const string malformed = "horse=horse-A\r\nhorse=horse-B\r\n";
                host.files.BeforeClaim = () => folders.Request(malformed);
                TestRunner.True(Run(host, new HorseTripRepairSession()) == HorseTripRepairDecision.Malformed && host.Removals == 0 && Candidates(a).Count == 1,
                    "A valid observation overrode the malformed claimed bytes.");
                TestRunner.True(folders.UserFiles().SequenceEqual(Files(Refused, ResultFile)) && Archive(folders, Refused) == malformed &&
                    folders.Result().StartsWith(folders.Result().Substring(0, 0)) && folders.Result().Contains("Refused"),
                    "The refusal did not archive the claimed bytes.");
            }
            // Refusal for a horse without a candidate: B names a horse with only a sourced copy; refused quoting B, A untouched.
            using (var folders = new Folders())
            {
                var host = new Host(folders); var a = LegacyHorse("horse-A"); var b = LegacyHorse("horse-B"); b.Facts[0].Sourceless = false;
                host.Loaded.Add(a); host.Loaded.Add(b);
                folders.Request(forA);
                host.files.BeforeClaim = () => folders.Request(forB);
                TestRunner.True(Run(host, new HorseTripRepairSession()) == HorseTripRepairDecision.NoCandidate && host.Removals == 0 &&
                    Candidates(a).Count == 1 && Archive(folders, Refused) == forB && folders.Result().Contains("horse-B") && !folders.Result().Contains("horse-A"),
                    "A refusal of the claimed request acted on, or named, the observed one.");
            }
            // The reverse: a malformed observation replaced by a valid B is applied to B (the observation decides nothing).
            using (var folders = new Folders())
            {
                var host = new Host(folders); var b = LegacyHorse("horse-B"); host.Loaded.Add(b);
                folders.Request("please fix my horse");
                host.files.BeforeClaim = () => folders.Request(forB);
                TestRunner.True(Run(host, new HorseTripRepairSession()) == HorseTripRepairDecision.Apply && host.RemovedFrom.SequenceEqual(new[] { "horse-B" }) &&
                    Archive(folders, Applied) == forB, "A malformed observation refused a valid claimed request.");
            }
            // Control: no replacement; the request for A applies to A with A's bytes archived.
            using (var folders = new Folders())
            {
                var host = new Host(folders); var a = LegacyHorse("horse-A"); var b = LegacyHorse("horse-B"); host.Loaded.Add(a); host.Loaded.Add(b);
                folders.Request(forA);
                TestRunner.True(Run(host, new HorseTripRepairSession()) == HorseTripRepairDecision.Apply && host.RemovedFrom.SequenceEqual(new[] { "horse-A" }) &&
                    Candidates(b).Count == 1 && Archive(folders, Applied) == forA && folders.Result().Contains("horse-A"),
                    "The unreplaced control did not apply to its own horse.");
            }
            // A closed write gate claims nothing: the request (whatever its bytes) stays untouched.
            using (var folders = new Folders())
            {
                var host = new Host(folders); host.Loaded.Add(LegacyHorse("horse-A"));
                folders.Request(forA);
                var claims = 0; host.files.BeforeClaim = () => claims++;
                TestRunner.True(Run(host, new HorseTripRepairSession(), false) == HorseTripRepairDecision.Pending && claims == 0 &&
                    folders.UserFiles().SequenceEqual(Files(Request)) && host.Removals == 0, "A closed gate claimed or changed the request.");
            }
        }

        private static void WrittenAfterClaim()
        {
            var forA = HorseTripRepairPolicy.FormatRequestLine("horse-A");
            var forB = HorseTripRepairPolicy.FormatRequestLine("horse-B");
            // A request written right after the claim is not merged into the claimed one: A applies now, B on the next check.
            using (var folders = new Folders())
            {
                var host = new Host(folders); var session = new HorseTripRepairSession();
                var a = LegacyHorse("horse-A"); var b = LegacyHorse("horse-B"); host.Loaded.Add(a); host.Loaded.Add(b);
                folders.Request(forA);
                host.files.AfterClaim = () => { folders.Request(forB); host.files.AfterClaim = null; };
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.Apply && host.RemovedFrom.SequenceEqual(new[] { "horse-A" }) &&
                    Archive(folders, Applied) == forA && folders.UserFiles().Contains(Request) && folders.Result().Contains("horse-A"),
                    "The claimed request was not processed on its own bytes, or the later request was lost.");
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.Apply && host.RemovedFrom.SequenceEqual(new[] { "horse-A", "horse-B" }) &&
                    Candidates(a).Count == 0 && Candidates(b).Count == 0 && folders.Result().Contains("horse-B") && !folders.UserFiles().Contains(Request),
                    "The later request was not processed separately.");
                var archives = folders.UserFiles().Where(name => name.EndsWith(HorseTripRepairPolicy.AppliedSuffix, StringComparison.Ordinal)).ToArray();
                TestRunner.True(archives.Length == 2 && archives.Select(name => Archive(folders, name)).OrderBy(x => x, StringComparer.Ordinal)
                    .SequenceEqual(new[] { forA, forB }), "The two applied archives do not hold exactly the two claimed requests.");
            }
            // A waiting claim (its horse not loaded) yields to a newer request written meanwhile: set aside unchanged as
            // superseded, the newer request kept and processed on its own; the waiting horse is never touched.
            using (var folders = new Folders())
            {
                var host = new Host(folders); var session = new HorseTripRepairSession(); var b = LegacyHorse("horse-B"); host.Loaded.Add(b);
                folders.Request(forA);
                host.files.AfterClaim = () => { folders.Request(forB); host.files.AfterClaim = null; };
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.Superseded && host.Removals == 0 &&
                    folders.UserFiles().SequenceEqual(Files(Request, Superseded, ResultFile)) && Archive(folders, Superseded) == forA &&
                    Archive(folders, Request) == forB && folders.Result().Contains("horse-A") && folders.Result().Contains("Superseded"),
                    "A waiting claim overwrote, merged or dropped the newer request.");
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.Apply && host.RemovedFrom.SequenceEqual(new[] { "horse-B" }) &&
                    Archive(folders, Applied) == forB, "The newer request was not processed on its own after the superseded one.");
            }
            // Control: a waiting claim with no newer request returns to the request name, byte for byte, for a later update.
            using (var folders = new Folders())
            {
                var host = new Host(folders); host.Loaded.Add(LegacyHorse("horse-Z"));
                folders.Request(forA);
                TestRunner.True(Run(host, new HorseTripRepairSession()) == HorseTripRepairDecision.HorseNotLoaded && host.Removals == 0 &&
                    folders.UserFiles().SequenceEqual(Files(Request)) && Archive(folders, Request) == forA, "A waiting claim was not returned unchanged.");
            }
        }

        // PR-4 review R1-F1: the production folder selection (HorseTripRepairPaths.SelectFolder, the composition root's only
        // repair-folder source) over separate player and run data parents. The player folder holds a stranded claim and an
        // earlier result: a guarded run without a bound isolated root leaves both byte-identical (no request, no claim recovery);
        // an isolated run processes only its own folder; ordinary play processes only the player's folder.
        private static void RepairFolderSelection()
        {
            using (var player = new Folders())
            using (var run = new Folders())
            {
                var playerParent = Path.GetDirectoryName(player.UserData);
                var runParent = Path.GetDirectoryName(run.UserData);
                var line = HorseTripRepairPolicy.FormatRequestLine("horse-1");
                File.WriteAllText(Path.Combine(player.UserData, Claimed), line);
                File.WriteAllText(Path.Combine(player.UserData, ResultFile), "2026-11-20 10:00:00Z\r\nApplied: an earlier result\r\n");
                Func<string> playerDigest = () => DirectoryDigest(player.UserData);
                var playerBefore = playerDigest();

                // PR-4 review R1-F1 (round 3): the repository's observer-only control (Invoke-KingmakerRuntimeScenario.ps1: Steam
                // -applaunch plus -kmcObserverRequest/-kmcObserverToken/-kmcObserverRequestSha256, no -kmcRuntimeRequest) creates no
                // KMC automation host and binds no isolated root; the production classification still calls it guarded, and the
                // production poll (ProcessFolder) then reads, renames and writes nothing in the player's folder.
                var observerOnly = new[] { "Kingmaker.exe", "-applaunch", "640820", HorseTripRepairPaths.ObserverRequestArgument, @"C:\evidence\observer-request.json",
                    "-kmcObserverToken", "token", "-kmcObserverRequestSha256", "ABCDEF" };
                var unbound = HorseTripRepairPaths.SelectFolder(HorseTripRepairPaths.IsGuardedLaunch(false, observerOnly), false, playerParent);
                TestRunner.True(HorseTripRepairPaths.IsGuardedLaunch(false, observerOnly) && unbound == null,
                    "The observer-only guarded launch with a null KMC host selected a repair folder: " + unbound);
                Host guarded = null; var guardedSession = new HorseTripRepairSession();
                for (var poll = 0; poll < 3; poll++)
                    TestRunner.Equal(HorseTripRepairDecision.NoRequest, HorseTripRepairPolicy.ProcessFolder(unbound,
                        files => { guarded = new Host(files.Directory); guarded.Loaded.Add(LegacyHorse("horse-1")); return guarded; }, true, guardedSession),
                        "The observer-only guarded run processed a repair file.");
                TestRunner.True(guarded == null && playerDigest() == playerBefore,
                    "The observer-only guarded run changed the player's stranded claim or result.");
                // A KMC runtime request whose host was not created (null) is guarded too; an ordinary launch is not.
                TestRunner.True(HorseTripRepairPaths.IsGuardedLaunch(false, new[] { "Kingmaker.exe", HorseTripRepairPaths.RuntimeRequestArgument, "r.json" }) &&
                    HorseTripRepairPaths.IsGuardedLaunch(true, new[] { "Kingmaker.exe" }) && !HorseTripRepairPaths.IsGuardedLaunch(false, new[] { "Kingmaker.exe", "-applaunch", "640820" }) &&
                    !HorseTripRepairPaths.IsGuardedLaunch(false, new[] { "Kingmaker.exe", "-KMCOBSERVERREQUEST" }),
                    "The guarded-launch classification is not exactly the two repository launch arguments or the KMC host.");

                run.Request(line);
                var isolated = HorseTripRepairPaths.SelectFolder(HorseTripRepairPaths.IsGuardedLaunch(true, new[] { "Kingmaker.exe", HorseTripRepairPaths.RuntimeRequestArgument, "r.json" }), true, runParent);
                TestRunner.Equal(run.UserData, isolated, "An isolated run did not select its own data parent's folder.");
                var isolatedHost = new Host(isolated); var isolatedHorse = LegacyHorse("horse-1"); isolatedHost.Loaded.Add(isolatedHorse);
                TestRunner.True(Run(isolatedHost, new HorseTripRepairSession()) == HorseTripRepairDecision.Apply && isolatedHost.Removals == 1 &&
                    run.UserFiles().SequenceEqual(Files(Applied, ResultFile)) && playerDigest() == playerBefore,
                    "An isolated run did not process exactly its own folder, or touched the player's.");
                var runAfter = DirectoryDigest(run.UserData);

                var ordinary = HorseTripRepairPaths.SelectFolder(HorseTripRepairPaths.IsGuardedLaunch(false, new[] { "Kingmaker.exe", "-applaunch", "640820" }), false, playerParent);
                TestRunner.Equal(player.UserData, ordinary, "Ordinary play did not select the player's folder.");
                var playerHost = new Host(ordinary); playerHost.Loaded.Add(LegacyHorse("horse-1"));
                TestRunner.True(Run(playerHost, new HorseTripRepairSession()) == HorseTripRepairDecision.Interrupted && playerHost.Removals == 0 &&
                    player.UserFiles().SequenceEqual(Files(Interrupted, ResultFile)) && DirectoryDigest(run.UserData) == runAfter,
                    "Ordinary play did not close only the player's own stranded claim.");
            }
        }

        // PR-4 review R1-F2 (round 3): failures at the native boundary itself, classified after the native call returns or throws.
        private static void NativeBoundaryRemoval()
        {
            var forA = HorseTripRepairPolicy.FormatRequestLine("horse-A");
            var uncertain = HorseTripRepairPolicy.RequestFileName + HorseTripRepairPolicy.UncertainSuffix;
            // The native removal takes the target and a neighbouring fact and then throws, before any adapter check: terminal failure,
            // never applied; a reload with one candidate is not removed again.
            using (var folders = new Folders())
            {
                var host = new Host(folders); var session = new HorseTripRepairSession(); var a = LegacyHorse("horse-A"); host.Loaded.Add(a);
                var factsBefore = a.Facts.Count;
                folders.Request(forA);
                host.AlsoRemoveNeighbour = true; host.FailRemovalAfter = true;
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.Uncertain && host.NativeCalls == 1 && a.Facts.Count == factsBefore - 2 &&
                    folders.UserFiles().SequenceEqual(Files(ResultFile, uncertain)) && Archive(folders, uncertain) == forA &&
                    folders.Result().Contains("Failed:") && folders.Result().Contains("did not change exactly that one fact") &&
                    folders.Result().Contains("native removal threw") && !folders.Result().Contains("Applied") && !session.HoldsClaim,
                    "R1-F2: a native removal that took a neighbour and threw was not a terminal failure: " + folders.Result());
                host.Loaded.Clear(); var reloaded = LegacyHorse("horse-A"); host.Loaded.Add(reloaded);
                for (var poll = 0; poll < 2; poll++) TestRunner.Equal(HorseTripRepairDecision.NoRequest, Run(host, session), "R1-F2: the failed request was processed again.");
                TestRunner.True(host.NativeCalls == 1 && Candidates(reloaded).Count == 1, "R1-F2: a second removal ran after the terminal failure.");
            }
            // The same failure with a failed result write: the terminal outcome is frozen; the retry after a reload only reports it.
            using (var folders = new Folders())
            {
                var host = new Host(folders); var session = new HorseTripRepairSession(); host.Loaded.Add(LegacyHorse("horse-A"));
                folders.Request(forA);
                host.AlsoRemoveNeighbour = true; host.FailRemovalAfter = true; host.FailResultWrite = true;
                TestRunner.True(Throws(() => Run(host, session)) && host.NativeCalls == 1 && session.PendingReports == 1 &&
                    folders.UserFiles().SequenceEqual(Files(Claimed)), "R1-F2: the inconsistent outcome was not frozen before its failed report.");
                host.Loaded.Clear(); var reloaded = LegacyHorse("horse-A"); host.Loaded.Add(reloaded);
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.FinishedDecided && host.NativeCalls == 1 && Candidates(reloaded).Count == 1 &&
                    folders.UserFiles().SequenceEqual(Files(ResultFile, uncertain)) && folders.Result().Contains("Failed:"),
                    "R1-F2: the retry of an inconsistent outcome selected or removed again.");
            }
            // Positive control: the native removal takes exactly the target and then throws: applied once.
            using (var folders = new Folders())
            {
                var host = new Host(folders); var a = LegacyHorse("horse-A"); host.Loaded.Add(a); var factsBefore = a.Facts.Count;
                folders.Request(forA);
                host.FailRemovalAfter = true;
                TestRunner.True(Run(host, new HorseTripRepairSession()) == HorseTripRepairDecision.Apply && host.NativeCalls == 1 && host.Removals == 1 &&
                    a.Facts.Count == factsBefore - 1 && Candidates(a).Count == 0 && folders.UserFiles().SequenceEqual(Files(Applied, ResultFile)) &&
                    folders.Result().Contains("Applied"), "R1-F2 control: an exact native removal followed by an exception was not applied.");
            }
        }

        // PR-4 review R2-F1: a native removal that returns normally having changed nothing is a verified no-change: the request
        // survives for a retry that removes exactly once, and a newer request written meanwhile supersedes it unchanged.
        private static void UnchangedRemovalRetries()
        {
            var forA = HorseTripRepairPolicy.FormatRequestLine("horse-A");
            using (var folders = new Folders())
            {
                var host = new Host(folders); var session = new HorseTripRepairSession(); var a = LegacyHorse("horse-A"); host.Loaded.Add(a);
                var factsBefore = a.Facts.Count;
                folders.Request(forA);
                host.NoEffect = true;
                Exception thrown = null;
                try { Run(host, session); } catch (Exception exception) { thrown = exception; }
                TestRunner.True(thrown is InvalidOperationException && thrown.Message.Contains("returned without removing") && host.NativeCalls == 1 &&
                    host.Removals == 0 && a.Facts.Count == factsBefore && !session.HoldsClaim && folders.UserFiles().SequenceEqual(Files(Request)) &&
                    Archive(folders, Request) == forA, "R2-F1: a verified no-change removal consumed the request: " + thrown);
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.Apply && host.NativeCalls == 2 && host.Removals == 1 &&
                    a.Facts.Count == factsBefore - 1 && folders.UserFiles().SequenceEqual(Files(Applied, ResultFile)) && Archive(folders, Applied) == forA,
                    "R2-F1: the retry after a no-change removal did not remove exactly once.");
            }
            // A newer request written while the no-change removal ran: the waiting claim yields (superseded, unchanged); the newer
            // request is preserved byte for byte and processed on its own.
            using (var folders = new Folders())
            {
                var host = new Host(folders); var session = new HorseTripRepairSession(); var a = LegacyHorse("horse-A"); var b = LegacyHorse("horse-B");
                host.Loaded.Add(a); host.Loaded.Add(b);
                var forB = HorseTripRepairPolicy.FormatRequestLine("horse-B");
                folders.Request(forA);
                host.NoEffect = true;
                host.DuringNativeRemoval = () => { host.DuringNativeRemoval = null; folders.Request(forB); };
                TestRunner.True(Throws(() => Run(host, session)) && host.NativeCalls == 1 && host.Removals == 0 && Candidates(a).Count == 1 &&
                    folders.UserFiles().SequenceEqual(Files(Request, ResultFile, Superseded)) && Archive(folders, Superseded) == forA &&
                    Archive(folders, Request) == forB && folders.Result().Contains("Superseded"), "R2-F1: a newer request was not preserved over the no-change claim.");
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.Apply && host.Removals == 1 && host.RemovedFrom.SequenceEqual(new[] { "horse-B" }) &&
                    Candidates(a).Count == 1 && Candidates(b).Count == 0, "R2-F1: the newer request was not processed on its own.");
            }
        }

        // PR-4 review R3-F1 (round 5): the native boundary removes exactly the target and throws an exception whose Message getter itself
        // throws. The production classification still returns Removed (its text falls back), so the decision is applied and archived;
        // after a reload of the same horse with one candidate nothing is removed again.
        private sealed class UnreadableMessageException : Exception { public override string Message => throw new InvalidOperationException("message getter failed"); }
        private static void UnreadableNativeMessage()
        {
            var forA = HorseTripRepairPolicy.FormatRequestLine("horse-A");
            using (var folders = new Folders())
            {
                var host = new Host(folders); var session = new HorseTripRepairSession(); var a = LegacyHorse("horse-A");
                host.Loaded.Add(a);
                host.ThrowAfterRemoval = new UnreadableMessageException();
                folders.Request(forA);
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.Apply && host.Removals == 1 && Archive(folders, Applied) == forA &&
                    folders.UserFiles().SequenceEqual(Files(Applied, ResultFile)) && folders.Result().Contains("Applied"), "R3-F1: an unreadable native exception message reopened the removal.");
                var reloaded = LegacyHorse("horse-A"); host.Loaded.Clear(); host.Loaded.Add(reloaded);
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.NoRequest && Run(host, new HorseTripRepairSession()) == HorseTripRepairDecision.NoRequest &&
                    host.Removals == 1 && host.NativeCalls == 1 && Candidates(reloaded).Count == 1, "R3-F1: the reloaded fact was removed again.");
            }
        }

        // PR-4 review R4-F1: the archive's commit fails after its private payload was written (the terminal name is occupied by a
        // directory at that moment). No terminal file appears and no private payload survives; the retry commits exactly one complete
        // archive with the original bytes.
        private static void FailedCommitLeavesNoPartialArchive()
        {
            var forA = HorseTripRepairPolicy.FormatRequestLine("horse-A");
            using (var folders = new Folders())
            {
                var host = new Host(folders); var session = new HorseTripRepairSession(); host.Loaded.Add(LegacyHorse("horse-A"));
                var target = Path.Combine(folders.UserData, Applied);
                folders.Request(forA);
                host.files.BeforeCommit = () => { host.files.BeforeCommit = null; Directory.CreateDirectory(target); };
                TestRunner.True(Throws(() => Run(host, session)) && host.Removals == 1 && !File.Exists(target) &&
                    !Directory.GetFiles(folders.UserData).Any(path => path.EndsWith(".pending", StringComparison.Ordinal) || path.EndsWith(HorseTripRepairPolicy.AppliedSuffix, StringComparison.Ordinal)),
                    "R4-F1: a failed commit left a terminal or private payload file.");
                Directory.Delete(target);
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.FinishedApplied && host.Removals == 1 && Archive(folders, Applied) == forA &&
                    Directory.GetFiles(folders.UserData).Count(path => path.EndsWith(HorseTripRepairPolicy.AppliedSuffix, StringComparison.Ordinal)) == 1 &&
                    folders.UserFiles().SequenceEqual(Files(Applied, ResultFile)), "R4-F1: the retry did not commit exactly one complete archive.");
            }
        }

        // PR-4 review R4-F2: the claim is held open (FileShare.Read) by another reader, so verification succeeds and the claim cannot be
        // retired. Two polls commit no second archive; after the handle is released the claim is retired, and a waiting request then
        // proceeds normally. A later request with identical bytes is a new claim and gets its own archive.
        private static void BlockedRetirementCommitsOnce()
        {
            var forA = HorseTripRepairPolicy.FormatRequestLine("horse-A");
            var forB = HorseTripRepairPolicy.FormatRequestLine("horse-B");
            using (var folders = new Folders())
            {
                var host = new Host(folders); var session = new HorseTripRepairSession(); host.Loaded.Add(LegacyHorse("horse-A")); host.Loaded.Add(LegacyHorse("horse-B"));
                var claimPath = Path.Combine(folders.UserData, Claimed);
                folders.Request(forA);
                FileStream holder = null;
                host.files.BeforeSettle = () => { host.files.BeforeSettle = null; holder = new FileStream(claimPath, FileMode.Open, FileAccess.Read, FileShare.Read); };
                Func<int> applied = () => Directory.GetFiles(folders.UserData).Count(path => path.EndsWith(HorseTripRepairPolicy.AppliedSuffix, StringComparison.Ordinal));
                TestRunner.True(Throws(() => Run(host, session)) && applied() == 1 && File.Exists(claimPath), "R4-F2: the blocked retirement lost the claim or the archive.");
                folders.Request(forB);
                TestRunner.True(Throws(() => Run(host, session)) && Throws(() => Run(host, session)) && applied() == 1 && host.Removals == 1 && Archive(folders, Request) == forB,
                    "R4-F2: a blocked retirement republished the archive or let the waiting request run.");
                holder.Dispose();
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.FinishedApplied && applied() == 1 && Archive(folders, Applied) == forA && !File.Exists(claimPath),
                    "R4-F2: after the release the claim was not retired with its single archive.");
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.Apply && host.Removals == 2 && applied() == 2, "R4-F2: the waiting request did not proceed normally.");
            }
        }

        // PR-4 reviews R3-F2/R4-F3: the claim is replaced by B's bytes before retirement (after A's archive committed), and, separately,
        // between the retirement check and the deleting open. B is never deleted without a disposition; with C queued, B is settled
        // (interrupted) before C runs, in the same session and in a fresh one; A is removed once.
        private static void ReplacementDuringRetirement()
        {
            var forA = HorseTripRepairPolicy.FormatRequestLine("horse-A");
            var forB = HorseTripRepairPolicy.FormatRequestLine("horse-B");
            var forC = HorseTripRepairPolicy.FormatRequestLine("horse-C");
            foreach (var fresh in new[] { false, true })
            using (var folders = new Folders())
            {
                var host = new Host(folders); var session = new HorseTripRepairSession();
                host.Loaded.Add(LegacyHorse("horse-A")); host.Loaded.Add(LegacyHorse("horse-B")); var c = LegacyHorse("horse-C"); host.Loaded.Add(c);
                var claimPath = Path.Combine(folders.UserData, Claimed);
                folders.Request(forA);
                host.files.BeforeSettle = () => { host.files.BeforeSettle = null; File.WriteAllText(claimPath, forB); folders.Request(forC); };
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.Apply && Archive(folders, Applied) == forA && Archive(folders, Claimed) == forB && host.Removals == 1,
                    "R3-F2 (" + (fresh ? "fresh" : "same") + " session): A's archive or B's claim was lost.");
                TestRunner.True(Run(host, fresh ? new HorseTripRepairSession() : session) == HorseTripRepairDecision.Interrupted && Archive(folders, Interrupted) == forB &&
                    Archive(folders, Request) == forC && host.Removals == 1 && Candidates(c).Count == 1, "R3-F2: B was not settled before C.");
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.Apply && host.Removals == 2 && host.RemovedFrom.SequenceEqual(new[] { "horse-A", "horse-C" }),
                    "R3-F2: C did not run after B was settled, or A was removed twice.");
            }
            using (var folders = new Folders())
            {
                var host = new Host(folders); var session = new HorseTripRepairSession(); host.Loaded.Add(LegacyHorse("horse-A")); host.Loaded.Add(LegacyHorse("horse-B"));
                var claimPath = Path.Combine(folders.UserData, Claimed);
                folders.Request(forA);
                host.files.BeforeRetire = () => { host.files.BeforeRetire = null; File.WriteAllText(claimPath, forB); };
                // PR-4 round 6 (R5-F2): B is left at the claim name by A's retirement and settled by its own Interrupted lifecycle next.
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.Apply && Archive(folders, Applied) == forA && Archive(folders, Claimed) == forB && host.Removals == 1 &&
                    Run(host, session) == HorseTripRepairDecision.Interrupted && Archive(folders, Interrupted) == forB &&
                    !Directory.GetFiles(folders.UserData).Any(path => path.EndsWith(HorseTripRepairPolicy.AppliedSuffix, StringComparison.Ordinal) && File.ReadAllText(path) == forB),
                    "R4-F3: B's bytes changed during retirement were deleted without a disposition, or reached A's archive.");
            }
        }


        // A staging stream for the narrow write/flush fault seam (HorseTripRepairFiles.StagingWriteSeam): the actual payload write reaches
        // the private file and then fails (Write: half the bytes are written first), or the whole payload is written and the flush fails.
        private sealed class FaultingStream : Stream
        {
            private readonly Stream inner; private readonly bool failWrite;
            public FaultingStream(Stream inner, bool failWrite) { this.inner = inner; this.failWrite = failWrite; }
            public override void Write(byte[] buffer, int offset, int count)
            {
                if (!failWrite) { inner.Write(buffer, offset, count); return; }
                inner.Write(buffer, offset, count / 2); inner.Flush();
                throw new IOException("injected failure during the archive payload write");
            }
            public override void Flush() { if (!failWrite) throw new IOException("injected failure during the archive payload flush"); inner.Flush(); }
            public override bool CanRead => false; public override bool CanSeek => false; public override bool CanWrite => true;
            public override long Length => inner.Length; public override long Position { get => inner.Position; set => throw new NotSupportedException(); }
            public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
        }

        private static int Count(Folders folders, string suffix) =>
            Directory.GetFiles(folders.UserData).Count(path => path.EndsWith(suffix, StringComparison.Ordinal));

        private static bool NoStaging(Folders folders) =>
            !Directory.GetFiles(folders.UserData).Any(path => path.EndsWith(".pending", StringComparison.Ordinal) || path.EndsWith(HorseTripRepairFiles.RetiringSuffix, StringComparison.Ordinal));

        // Owner instruction 2026-11-26d section 7, case 5 (PR-4 review R4-F1): the actual payload write, or its flush, fails after the private
        // file was created (narrow fault seam at that I/O operation). No partial terminal archive and no private file is left; the claim is
        // still present; the retry produces one complete archive with the original bytes, one removal in total.
        private static void WriteOrFlushFailureLeavesNoPartialArchive()
        {
            var forA = HorseTripRepairPolicy.FormatRequestLine("horse-A");
            foreach (var failWrite in new[] { true, false })
            using (var folders = new Folders())
            {
                var label = failWrite ? "write" : "flush";
                var host = new Host(folders); var session = new HorseTripRepairSession(); host.Loaded.Add(LegacyHorse("horse-A"));
                var created = 0;
                host.files.StagingWriteSeam = stream => { created++; host.files.StagingWriteSeam = null; return new FaultingStream(stream, failWrite); };
                folders.Request(forA);
                TestRunner.True(Throws(() => Run(host, session)) && created == 1 && host.Removals == 1 && File.Exists(Path.Combine(folders.UserData, Claimed)) &&
                    Count(folders, Applied) == 0 && NoStaging(folders) && folders.UserFiles().SequenceEqual(Files(Claimed, ResultFile)),
                    "R4-F1 (" + label + " failure): a partial terminal or private archive survived, or the claim was lost.");
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.FinishedApplied && host.Removals == 1 && Archive(folders, Applied) == forA &&
                    Count(folders, Applied) == 1 && NoStaging(folders) && folders.UserFiles().SequenceEqual(Files(Applied, ResultFile)),
                    "R4-F1 (" + label + " failure): the retry did not produce exactly one complete archive with the original bytes.");
            }
        }

        // Owner instruction 2026-11-26d section 7, case 4 (PR-4 review R5-F1): at BeforeCommit (payload staged and flushed, staging handle held)
        // an editor tries to change the actual .pending file in place, or to replace it (rename it away and create a new file under its name).
        // The in-place edit is excluded (Windows sharing: the staging handle admits no writer); the replacement reaches publication and is
        // refused by the published-bytes check, the terminal name it reached is removed and the commit fails. Either way the final archive
        // holds exactly A's bytes, with one removal.
        private static void PendingArchiveChangeIsExcludedOrRefused()
        {
            var forA = HorseTripRepairPolicy.FormatRequestLine("horse-A");
            foreach (var attempt in new[] { "edit in place", "replace by rename" })
            using (var folders = new Folders())
            {
                var host = new Host(folders); var session = new HorseTripRepairSession(); host.Loaded.Add(LegacyHorse("horse-A"));
                folders.Request(forA);
                string outcome = null; string moved = null;
                host.files.BeforeCommit = () =>
                {
                    host.files.BeforeCommit = null;
                    var pending = Directory.GetFiles(folders.UserData, "*.pending").Single();
                    try
                    {
                        if (attempt == "edit in place") File.WriteAllText(pending, "horse=horse-TAMPERED");
                        else { moved = pending + ".moved"; File.Move(pending, moved); File.WriteAllText(pending, "horse=horse-TAMPERED"); }
                        outcome = "change applied";
                    }
                    catch (IOException) { outcome = "change excluded"; }
                    catch (UnauthorizedAccessException) { outcome = "change excluded"; }
                };
                HorseTripRepairDecision first = HorseTripRepairDecision.Pending; var firstThrew = false;
                try { first = Run(host, session); } catch (Exception) { firstThrew = true; }
                if (moved != null && File.Exists(moved)) File.Delete(moved);   // the editor's own leftover
                var archivedAfterFirst = Count(folders, Applied);
                var final = firstThrew ? Run(host, session) : first;
                var exact = Archive(folders, Applied) == forA && Count(folders, Applied) == 1 && host.Removals == 1 && NoStaging(folders) &&
                    !Directory.GetFiles(folders.UserData).Any(path => File.ReadAllText(path).Contains("TAMPERED"));
                Console.WriteLine("    R5-F1 " + attempt + ": " + outcome + "; first poll " + (firstThrew ? "refused (commit failed)" : first.ToString()) +
                    "; archives after first poll " + archivedAfterFirst + "; final " + final + "; archive exact " + exact);
                TestRunner.True(exact && (outcome == "change excluded" ? !firstThrew && first == HorseTripRepairDecision.Apply
                                                                       : firstThrew && archivedAfterFirst == 0 && final == HorseTripRepairDecision.FinishedApplied),
                    "R5-F1 (" + attempt + "): a change to the staged archive reached the terminal archive, or publication did not stop safely.");
            }
        }

        // Owner instruction 2026-11-26d section 7, cases 1 and 2 (PR-4 reviews R4-F3, R5-F2): A is applied; at BeforeRetire (A's archive
        // committed) B replaces A's claim; C is queued. Retirement leaves B at the claim name untouched. B then takes the policy's own
        // Interrupted path: its result names B and how to retry, and its archive holds B. With B's archive commit obstructed, B survives the
        // failure, recovers before C (same session and fresh session), and A is removed once. C runs only after B is settled.
        private static void ReplacementAtRetirementTakesItsOwnLifecycle()
        {
            var forA = HorseTripRepairPolicy.FormatRequestLine("horse-A");
            var forB = HorseTripRepairPolicy.FormatRequestLine("horse-B");
            var forC = HorseTripRepairPolicy.FormatRequestLine("horse-C");
            foreach (var kase in new[] { "B settles", "B publication fails, same session", "B publication fails, fresh session" })
            using (var folders = new Folders())
            {
                var host = new Host(folders); var session = new HorseTripRepairSession();
                var b = LegacyHorse("horse-B"); var c = LegacyHorse("horse-C");
                host.Loaded.Add(LegacyHorse("horse-A")); host.Loaded.Add(b); host.Loaded.Add(c);
                var claimPath = Path.Combine(folders.UserData, Claimed);
                var interruptedTarget = Path.Combine(folders.UserData, Interrupted);
                folders.Request(forA);
                host.files.BeforeRetire = () => { host.files.BeforeRetire = null; File.WriteAllText(claimPath, forB); folders.Request(forC); };
                var obstruct = kase != "B settles";
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.Apply && Archive(folders, Applied) == forA && Archive(folders, Claimed) == forB &&
                    Count(folders, Interrupted) == 0 && Archive(folders, Request) == forC && host.Removals == 1 && NoStaging(folders),
                    "R4-F3 (" + kase + "): retirement did not leave B untouched at the claim name, or archived it inside A's cleanup.");
                if (obstruct)
                {
                    Directory.CreateDirectory(interruptedTarget);
                    TestRunner.True(Throws(() => Run(host, session)) && Archive(folders, Claimed) == forB && Count(folders, Interrupted) == 0 && Archive(folders, Request) == forC &&
                        folders.Result().Contains("horse-B") && host.Removals == 1 && NoStaging(folders),
                        "R4-F3 (" + kase + "): B did not survive its failed archive publication, or C was admitted.");
                    TestRunner.True(Throws(() => Run(host, session)) && Archive(folders, Claimed) == forB && Archive(folders, Request) == forC && host.Removals == 1,
                        "R4-F3 (" + kase + "): a repeated poll lost B or admitted C while B's publication was failing.");
                    Directory.Delete(interruptedTarget);
                }
                var recovering = kase.EndsWith("fresh session", StringComparison.Ordinal) ? new HorseTripRepairSession() : session;
                var settled = Run(host, recovering);
                var expected = kase == "B publication fails, same session" ? HorseTripRepairDecision.FinishedDecided : HorseTripRepairDecision.Interrupted;
                var result = folders.Result();
                Console.WriteLine("    R5-F2 " + kase + ": settled " + settled + "; result: " + result.Replace("\r\n", " ").Trim());
                TestRunner.True(settled == expected && Archive(folders, Interrupted) == forB && Count(folders, Interrupted) == 1 && !File.Exists(claimPath) &&
                    result.Contains("Interrupted") && result.Contains("horse-B") && result.Contains("Write a new request") && Archive(folders, Request) == forC &&
                    Candidates(b).Count == 1 && Archive(folders, Applied) == forA && host.Removals == 1 && NoStaging(folders),
                    "R5-F2 (" + kase + "): B's owner-facing Interrupted result or its archive is missing, B's fact changed, or C ran first.");
                TestRunner.True(Run(host, recovering) == HorseTripRepairDecision.Apply && host.Removals == 2 && host.RemovedFrom.SequenceEqual(new[] { "horse-A", "horse-C" }) &&
                    Count(folders, Interrupted) == 1, "R4-F3 (" + kase + "): C did not run after B was settled, or A was removed twice.");
            }
        }

        // Owner instruction 2026-11-26d section 7, case 3 (PR-4 review R4-F2): a fresh session finds a readable claim (A, stranded by an earlier
        // game session) held open against retirement by another reader (FileShare.Read; Windows sharing semantics), with C waiting. Repeated polls
        // produce one interruption archive and one result, zero removals and no admission of C; after the handle is released the claim is
        // retired and C then runs normally.
        private static void LockedRecoveredClaimArchivesOnce()
        {
            var forA = HorseTripRepairPolicy.FormatRequestLine("horse-A");
            var forC = HorseTripRepairPolicy.FormatRequestLine("horse-C");
            using (var folders = new Folders())
            {
                var host = new Host(folders); var session = new HorseTripRepairSession();
                var a = LegacyHorse("horse-A"); host.Loaded.Add(a); host.Loaded.Add(LegacyHorse("horse-C"));
                var claimPath = Path.Combine(folders.UserData, Claimed);
                File.WriteAllText(claimPath, forA); folders.Request(forC);
                var resultWrites = 0; host.AfterResultWrite = () => resultWrites++;
                using (var holder = new FileStream(claimPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var threw = 0;
                    for (var poll = 0; poll < 4; poll++) if (Throws(() => Run(host, session))) threw++;
                    Console.WriteLine("    R4-F2 locked recovered claim: polls failing while held " + threw + "/4; interrupted archives " + Count(folders, Interrupted) + "; result writes " + resultWrites);
                    TestRunner.True(threw == 4 && Count(folders, Interrupted) == 1 && Archive(folders, Interrupted) == forA && File.Exists(claimPath) && resultWrites == 1 &&
                        host.Removals == 0 && host.NativeCalls == 0 && Archive(folders, Request) == forC && NoStaging(folders),
                        "R4-F2: a locked recovered claim republished its interruption archive or result, removed something, or admitted the waiting request.");
                }
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.FinishedDecided && Count(folders, Interrupted) == 1 && !File.Exists(claimPath) &&
                    host.Removals == 0 && Candidates(a).Count == 1 && resultWrites == 1, "R4-F2: after the release the recovered claim did not complete with its single archive.");
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.Apply && host.Removals == 1 && host.RemovedFrom.SequenceEqual(new[] { "horse-C" }),
                    "R4-F2: the waiting request did not run normally after the recovered claim completed.");
            }
        }


        // Owner instruction 2026-11-26e section 6 (PR-4 review R6-F1): A's archive commits, then A's retirement fails (another reader holds the
        // claim); the reader lets go, the claim is replaced by B and C is queued before the next poll of the SAME session. B is a different
        // claim: it starts without A's committed-archive record, so B gets its own interrupted archive and result; A's applied archive holds
        // A; B's fact stays; C is then processed.
        private static void ReplacementAfterFailedRetirementHasItsOwnRecord()
        {
            var forA = HorseTripRepairPolicy.FormatRequestLine("horse-A");
            var forB = HorseTripRepairPolicy.FormatRequestLine("horse-B");
            var forC = HorseTripRepairPolicy.FormatRequestLine("horse-C");
            using (var folders = new Folders())
            {
                var host = new Host(folders); var session = new HorseTripRepairSession();
                var b = LegacyHorse("horse-B"); host.Loaded.Add(LegacyHorse("horse-A")); host.Loaded.Add(b); host.Loaded.Add(LegacyHorse("horse-C"));
                var claimPath = Path.Combine(folders.UserData, Claimed);
                folders.Request(forA);
                FileStream holder = null;
                host.files.BeforeRetire = () => { host.files.BeforeRetire = null; holder = new FileStream(claimPath, FileMode.Open, FileAccess.Read, FileShare.Read); };
                TestRunner.True(Throws(() => Run(host, session)) && Archive(folders, Applied) == forA && File.Exists(claimPath) && session.CommittedArchive != null && host.Removals == 1,
                    "R6-F1: A's archive did not commit, or its blocked retirement lost the claim.");
                holder.Dispose();
                File.WriteAllText(claimPath, forB); folders.Request(forC);
                var settled = Run(host, session);
                var result = folders.Result();
                Console.WriteLine("    R6-F1 replacement after A's failed retirement (same session): " + settled + "; applied=" + Count(folders, Applied) + " interrupted=" + Count(folders, Interrupted) +
                    "; result: " + result.Replace("\r\n", " ").Trim());
                TestRunner.True(settled == HorseTripRepairDecision.Interrupted && Archive(folders, Applied) == forA && Archive(folders, Interrupted) == forB &&
                    Count(folders, Applied) == 1 && Count(folders, Interrupted) == 1 && !File.Exists(claimPath) && result.Contains("horse-B") && result.Contains("Interrupted") &&
                    Candidates(b).Count == 1 && host.Removals == 1 && Archive(folders, Request) == forC && NoStaging(folders),
                    "R6-F1: B inherited A's archive record (no B archive, or B lost), or C ran first.");
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.Apply && host.RemovedFrom.SequenceEqual(new[] { "horse-A", "horse-C" }),
                    "R6-F1: C was not processed after B settled.");
            }
        }

        // Owner instruction 2026-11-26e section 6 (PR-4 reviews R5-F1, R4-F3; Windows evidence: exclusive handles and handle-bound rename and
        // delete): (1) at the terminal name's first visibility (immediately after the rename, staging handle still held) a reader obtains no
        // bytes other than A's; an attempt to replace the staged file by renaming it away and writing a new one is excluded. (2) After the claim
        // was verified through the retiring handle and before its delete, an attempt to replace the claim's directory entry (rename it away and
        // write B under the claim name) is excluded; only A's verified file is deleted. One removal; exact final bytes.
        private static void VerifiedFileOperationsBindTheFinalOperation()
        {
            var forA = HorseTripRepairPolicy.FormatRequestLine("horse-A");
            var forB = HorseTripRepairPolicy.FormatRequestLine("horse-B");
            using (var folders = new Folders())
            {
                var host = new Host(folders); var session = new HorseTripRepairSession(); host.Loaded.Add(LegacyHorse("horse-A")); host.Loaded.Add(LegacyHorse("horse-B"));
                var claimPath = Path.Combine(folders.UserData, Claimed);
                folders.Request(forA);
                string stagingAttempt = null, firstVisible = null, retireAttempt = null;
                host.files.BeforeCommit = () =>
                {
                    host.files.BeforeCommit = null;
                    var pending = Directory.GetFiles(folders.UserData, "*.pending").Single();
                    try { File.Move(pending, pending + ".moved"); File.WriteAllText(pending, forB); stagingAttempt = "replaced"; }
                    catch (IOException) { stagingAttempt = "excluded"; } catch (UnauthorizedAccessException) { stagingAttempt = "excluded"; }
                };
                host.files.AfterPublishRename = terminal =>
                {
                    host.files.AfterPublishRename = null;
                    try { firstVisible = File.ReadAllText(terminal) == forA ? "A's bytes" : "OTHER bytes"; }
                    catch (IOException) { firstVisible = "not readable while the staging handle is held"; }
                };
                host.files.BeforeRetireDelete = () =>
                {
                    host.files.BeforeRetireDelete = null;
                    try { File.Move(claimPath, claimPath + ".moved"); File.WriteAllText(claimPath, forB); retireAttempt = "replaced"; }
                    catch (IOException) { retireAttempt = "excluded"; } catch (UnauthorizedAccessException) { retireAttempt = "excluded"; }
                };
                var decision = Run(host, session);
                var platform = HorseTripRepairPolicy_VerifiedSupported() ? "Windows handle operations" : "pathname fallback";
                Console.WriteLine("    R5-F1/R4-F3 (" + platform + "): staging replacement " + stagingAttempt + "; terminal at first visibility: " + firstVisible +
                    "; claim replacement after verification " + retireAttempt + "; decision " + decision + "; files " + string.Join(",", folders.UserFiles()));
                TestRunner.True(decision == HorseTripRepairDecision.Apply && stagingAttempt == "excluded" && retireAttempt == "excluded" && firstVisible != "OTHER bytes" &&
                    Archive(folders, Applied) == forA && Count(folders, Applied) == 1 && !File.Exists(claimPath) && host.Removals == 1 && NoStaging(folders) &&
                    !Directory.GetFiles(folders.UserData).Any(path => path.EndsWith(".moved", StringComparison.Ordinal)) &&
                    folders.UserFiles().SequenceEqual(Files(Applied, ResultFile)),
                    "R5-F1/R4-F3: a replacement reached A's terminal name or A's retirement deleted something other than A's verified file.");
            }
        }

        private static bool HorseTripRepairPolicy_VerifiedSupported() => Environment.OSVersion.Platform == PlatformID.Win32NT;

        // Owner instruction 2026-11-26e section 6 (PR-4 review R6-F2): the previous session already removed A's fact (applied) but its archive
        // publication failed; a fresh session finds the stranded claim and settles it as interrupted. The result names the horse, says this
        // recovery made no further change and that the earlier session may already have removed the fact, and points to the exact candidate
        // log line; zero additional removals.
        private static void RestartReportingIsTruthful()
        {
            var forA = HorseTripRepairPolicy.FormatRequestLine("horse-A");
            using (var folders = new Folders())
            {
                var host = new Host(folders); var session = new HorseTripRepairSession(); var a = LegacyHorse("horse-A"); host.Loaded.Add(a);
                var applied = Path.Combine(folders.UserData, Applied);
                folders.Request(forA);
                host.files.BeforeCommit = () => { host.files.BeforeCommit = null; Directory.CreateDirectory(applied); };
                TestRunner.True(Throws(() => Run(host, session)) && host.Removals == 1 && Candidates(a).Count == 0, "R6-F2: the previous session did not remove the fact before its archive failed.");
                Directory.Delete(applied);
                var fresh = new HorseTripRepairSession();
                var decision = Run(host, fresh);
                var result = folders.Result();
                Console.WriteLine("    R6-F2 fresh-session result after an applied removal: " + result.Replace("\r\n", " ").Trim());
                TestRunner.True(decision == HorseTripRepairDecision.Interrupted && host.Removals == 1 && host.NativeCalls == 1 && result.Contains("horse-A") &&
                    result.Contains("This recovery made no further change") && result.Contains("may already have removed the fact") &&
                    result.Contains("KMC Horse Trip repair candidate") && !result.Contains("made no change for it") && !result.Contains("a bite hit followed by a Trip"),
                    "R6-F2: the recovery wording denies a removal that may have happened, omits the horse, or gives no exact retry guidance; or a removal was repeated.");
            }
        }


        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern bool CreateHardLinkW(string link, string existing, IntPtr security);

        // Owner instruction 2026-11-26e section 6, PR-4 review R7-F1 (Windows evidence): the recovered claim is a file link whose data lives
        // in a disposable target in a sibling directory. Unprivileged file symbolic links cannot be created on this machine (Win32 error 1314),
        // so the link is a hard link; production opens the claim with FILE_FLAG_OPEN_REPARSE_POINT and refuses any reparse point, so a symbolic
        // link would be refused and left in place rather than followed. Recovery settles the claim; the sibling target still exists with its
        // original bytes; an ordinary claim then retires normally.
        private static void LinkedClaimRetirementLeavesItsTarget()
        {
            var forA = HorseTripRepairPolicy.FormatRequestLine("horse-A");
            var forC = HorseTripRepairPolicy.FormatRequestLine("horse-C");
            using (var folders = new Folders())
            {
                var host = new Host(folders); var session = new HorseTripRepairSession(); host.Loaded.Add(LegacyHorse("horse-A")); host.Loaded.Add(LegacyHorse("horse-C"));
                var sibling = Path.Combine(folders.Root, "sibling"); Directory.CreateDirectory(sibling);
                var target = Path.Combine(sibling, "disposable-target.txt"); File.WriteAllText(target, forA);
                var claimPath = Path.Combine(folders.UserData, Claimed);
                var linked = CreateHardLinkW(claimPath, target, IntPtr.Zero);
                var decision = linked ? Run(host, session) : HorseTripRepairDecision.Pending;
                var targetIntact = File.Exists(target) && File.ReadAllText(target) == forA;
                folders.Request(forC);
                var ordinary = Run(host, session);
                Console.WriteLine("    R7-F1 linked claim (hard link to a sibling target): linked=" + linked + " recovery=" + decision + " targetIntact=" + targetIntact +
                    " claimLeft=" + File.Exists(claimPath) + " interrupted=" + Count(folders, Interrupted) + " then ordinary=" + ordinary + " applied=" + Count(folders, Applied));
                TestRunner.True(linked && decision == HorseTripRepairDecision.Interrupted && targetIntact && Archive(folders, Interrupted) == forA && !File.Exists(claimPath) &&
                    ordinary == HorseTripRepairDecision.Apply && Count(folders, Applied) == 1 && host.RemovedFrom.SequenceEqual(new[] { "horse-C" }),
                    "R7-F1: retiring a linked claim removed or changed its target, or ordinary retirement stopped working.");
            }
        }

        // Owner instruction 2026-11-26e section 6, PR-4 review R4-F3 on the pathname branch (used where handle-bound operations do not exist:
        // Linux/macOS Mono; exercised here on Windows through the test seam): after the claim's bytes were verified under the private name and
        // before the settling operation, the private entry is replaced by B. Nothing is deleted by pathname: the settling move takes B, the
        // second verification sees B, and B returns to the claim name; B then receives its own Interrupted result and archive before queued C;
        // A is removed once.
        private static void PathnameRetirementNeverConsumesAReplacement()
        {
            var forA = HorseTripRepairPolicy.FormatRequestLine("horse-A");
            var forB = HorseTripRepairPolicy.FormatRequestLine("horse-B");
            var forC = HorseTripRepairPolicy.FormatRequestLine("horse-C");
            HorseTripRepairFiles.ForcePathnameOperations = true; UnixFiles.Calls = new SimulatedPosix();
            try
            {
                using (var folders = new Folders())
                {
                    var host = new Host(folders); var session = new HorseTripRepairSession();
                    var b = LegacyHorse("horse-B"); host.Loaded.Add(LegacyHorse("horse-A")); host.Loaded.Add(b); host.Loaded.Add(LegacyHorse("horse-C"));
                    folders.Request(forA);
                    host.files.BeforeRetireDelete = () =>
                    {
                        host.files.BeforeRetireDelete = null;
                        var retiring = Directory.GetFiles(folders.UserData, "*" + HorseTripRepairFiles.RetiringSuffix).Single();
                        File.Delete(retiring); File.WriteAllText(retiring, forB); folders.Request(forC);
                    };
                    var first = Run(host, session);
                    var claimHoldsB = File.Exists(Path.Combine(folders.UserData, Claimed)) && Archive(folders, Claimed) == forB;
                    var settledFiles = Directory.GetFiles(folders.UserData, "*" + HorseTripRepairFiles.SettledSuffix).Length;
                    var second = Run(host, session);
                    var result = folders.Result();
                    var third = Run(host, session);
                    Console.WriteLine("    R4-F3 pathname branch: first=" + first + " claimHoldsB=" + claimHoldsB + " settled=" + settledFiles + " second=" + second + " (" +
                        result.Replace("\r\n", " ").Trim().Substring(0, 80) + "...) third=" + third + " removals=" + string.Join(",", host.RemovedFrom));
                    TestRunner.True(first == HorseTripRepairDecision.Apply && Archive(folders, Applied) == forA && claimHoldsB && settledFiles == 0 &&
                        second == HorseTripRepairDecision.Interrupted && Archive(folders, Interrupted) == forB && result.Contains("horse-B") && Candidates(b).Count == 1 &&
                        third == HorseTripRepairDecision.Apply && host.RemovedFrom.SequenceEqual(new[] { "horse-A", "horse-C" }),
                        "R4-F3 (pathname branch): a replacement at the final retirement operation was consumed, or B did not settle before C, or A was removed twice.");
                }
            }
            finally { HorseTripRepairFiles.ForcePathnameOperations = false; UnixFiles.Calls = null; }
        }


        // A write that stops after half of the payload: the fault point observes the terminal name with a reader that shares write and
        // delete access, may create an entry of someone else's there (present at cleanup), then fails the write.
        private sealed class HalfWriteFault : Stream
        {
            private readonly Stream inner; private readonly Action atFault;
            public HalfWriteFault(Stream inner, Action atFault) { this.inner = inner; this.atFault = atFault; }
            public override bool CanRead => false; public override bool CanSeek => false; public override bool CanWrite => true;
            public override long Length => throw new NotSupportedException();
            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
            public override void Flush() => inner.Flush();
            public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) { inner.Write(buffer, offset, count / 2); inner.Flush(); atFault(); throw new IOException("simulated write failure"); }
        }

        private static string ObserveShared(string path)
        {
            try
            {
                if (!File.Exists(path)) return "absent";
                using (var reader = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                using (var text = new StreamReader(reader)) return text.ReadToEnd();
            }
            catch (IOException) { return "not readable"; }
        }

        // Owner instruction 2026-11-26f section 7, PR-4 review R5-F1 on the pathname branch (round 9: Windows fallback execution through the test seam; since round 10 over the simulated POSIX boundary;
        // the production Linux linkat/libc link variants are not executed on this machine): the payload is written and flushed through the
        // private file's handle and published by creating a hard link to it. During the partial-write fault the terminal name is observed
        // with a reader sharing write access (absent), and an entry of someone else's is created there; the attempt fails, the claim stays,
        // the foreign entry is preserved. The retry's first readability (the moment its link exists) shows A's complete bytes; one archive of
        // A, one removal.
        private static void PathnamePublicationIsComplete()
        {
            var forA = HorseTripRepairPolicy.FormatRequestLine("horse-A");
            const string foreign = "someone else's file";
            HorseTripRepairFiles.ForcePathnameOperations = true; UnixFiles.Calls = new SimulatedPosix();
            try
            {
                using (var folders = new Folders())
                {
                    var host = new Host(folders); var session = new HorseTripRepairSession(); host.Loaded.Add(LegacyHorse("horse-A"));
                    var plain = Path.Combine(folders.UserData, Applied);
                    folders.Request(forA);
                    string duringFault = null;
                    host.files.StagingWriteSeam = stream => { host.files.StagingWriteSeam = null; return new HalfWriteFault(stream, () => { duringFault = ObserveShared(plain); File.WriteAllText(plain, foreign); }); };
                    var failed = Throws(() => Run(host, session));
                    var claimKept = File.Exists(Path.Combine(folders.UserData, Claimed));
                    var foreignKept = File.ReadAllText(plain) == foreign;
                    string firstReadable = null, published = null;
                    host.files.AfterPublishRename = terminal => { published = terminal; firstReadable = ObserveShared(terminal); };
                    var retried = Run(host, session);
                    var archives = Directory.GetFiles(folders.UserData, HorseTripRepairPolicy.RequestFileName + "*" + HorseTripRepairPolicy.AppliedSuffix)
                        .Where(f => File.ReadAllText(f) == forA).ToList();
                    Console.WriteLine("    R5-F1 pathname branch (simulated POSIX boundary): during the partial-write fault the terminal name was " + duringFault +
                        "; failure threw=" + failed + ", claim kept=" + claimKept + ", foreign entry preserved=" + foreignKept + "; retry " + retried +
                        " published " + Path.GetFileName(published ?? "<none>") + ", first readable " + (firstReadable == forA ? "with A's complete bytes" : "as '" + firstReadable + "'") +
                        "; A's archives=" + archives.Count + " removals=" + host.Removals);
                    TestRunner.True(failed && duringFault == "absent" && claimKept && foreignKept && File.ReadAllText(plain) == foreign && retried == HorseTripRepairDecision.FinishedApplied &&
                        firstReadable == forA && archives.Count == 1 && host.Removals == 1 && !File.Exists(Path.Combine(folders.UserData, Claimed)),
                        "R5-F1 (pathname branch): the terminal name showed a partial archive, the foreign entry was touched, the claim was lost or the retry did not publish exactly A once.");
                }
            }
            finally { HorseTripRepairFiles.ForcePathnameOperations = false; UnixFiles.Calls = null; }
        }

        // Owner instruction 2026-11-26f section 7, PR-4 review R4-F3 on the pathname branch (round 9: Windows fallback execution; since round 10 over the simulated POSIX boundary): a replacement B took the
        // private retiring name after A's bytes were verified there; the settling move takes B; then the second check (variant 1) or the move
        // back (variant 2) fails. B's settled entry names A's identity, so recovery in the same session (and in a fresh session) restores it to
        // the claim name: B receives its byte-exact Interrupted archive and named result before the queued request C, B's fact stays, A is
        // removed once.
        private static void PathnameRetirementRecoversAfterSettlingFailures()
        {
            var forA = HorseTripRepairPolicy.FormatRequestLine("horse-A");
            var forB = HorseTripRepairPolicy.FormatRequestLine("horse-B");
            var forC = HorseTripRepairPolicy.FormatRequestLine("horse-C");
            foreach (var failure in new[] { "second check", "move back" })
                foreach (var fresh in new[] { false, true })
                {
                    HorseTripRepairFiles.ForcePathnameOperations = true; UnixFiles.Calls = new SimulatedPosix();
                    try
                    {
                        using (var folders = new Folders())
                        {
                            var host = new Host(folders); var session = new HorseTripRepairSession();
                            var b = LegacyHorse("horse-B"); host.Loaded.Add(LegacyHorse("horse-A")); host.Loaded.Add(b); host.Loaded.Add(LegacyHorse("horse-C"));
                            folders.Request(forA);
                            host.files.BeforeRetireDelete = () =>
                            {
                                host.files.BeforeRetireDelete = null;
                                var retiring = Directory.GetFiles(folders.UserData, "*" + HorseTripRepairFiles.RetiringSuffix).Single();
                                File.Delete(retiring); File.WriteAllText(retiring, forB); folders.Request(forC);
                            };
                            if (failure == "second check") host.files.AfterSettleMove = () => { host.files.AfterSettleMove = null; throw new IOException("simulated read failure"); };
                            else host.files.BeforeSettledReturn = () => { host.files.BeforeSettledReturn = null; throw new IOException("simulated move-back failure"); };
                            var first = Throws(() => Run(host, session));
                            var stranded = Directory.GetFiles(folders.UserData, "*" + HorseTripRepairFiles.SettledSuffix).Length;
                            var claimAbsent = !File.Exists(Path.Combine(folders.UserData, Claimed));
                            var next = fresh ? new HorseTripRepairSession() : session;
                            var second = Run(host, next);
                            var result = folders.Result();
                            var third = Run(host, next);
                            var pendingLeft = Directory.GetFiles(folders.UserData, "*" + HorseTripRepairFiles.RetiringSuffix).Length;
                            var label = failure + " failure, " + (fresh ? "fresh session" : "same session");
                            Console.WriteLine("    R4-F3 pathname branch (" + label + "): first threw=" + first + " with B under .settled=" + stranded + " (claim absent=" + claimAbsent + "); then " +
                                second + " (" + result.Replace("\r\n", " ").Trim().Substring(0, 70) + "...), then " + third + "; removals=" + string.Join(",", host.RemovedFrom));
                            TestRunner.True(first && stranded == 1 && claimAbsent && Archive(folders, Applied) == forA && second == HorseTripRepairDecision.Interrupted &&
                                Archive(folders, Interrupted) == forB && result.Contains("horse-B") && Candidates(b).Count == 1 && third == HorseTripRepairDecision.Apply &&
                                host.RemovedFrom.SequenceEqual(new[] { "horse-A", "horse-C" }) && pendingLeft == 0,
                                "R4-F3 (pathname, " + label + "): the replacement was stranded, settled out of order or a removal was repeated.");
                        }
                    }
                    finally { HorseTripRepairFiles.ForcePathnameOperations = false; UnixFiles.Calls = null; }
                }
        }

        // Owner instruction 2026-11-26f section 7, PR-4 review R8-F1 (Windows, production handle branch): the bytes that decide a request or a
        // claim come from a no-follow handle checked before reading. Actual reparse points: a directory junction (creatable without privilege)
        // at the request name and at the claim name. Simulated external boundary (VerifiedFileOperations.SimulatedReparseForTests reports the
        // opened entry as a reparse point, standing in for a file symbolic link this machine cannot create): a fresh linked request naming a
        // loaded horse and a recovered linked claim. Each is refused before selection, removal, claim or archive copy, and left in place.
        // Controls: an ordinary request applies; a regular claim that another reader holds open is still read and, once released, settled.
        private static void LinkedEntriesAreRefusedBeforeActing()
        {
            var forA = HorseTripRepairPolicy.FormatRequestLine("horse-A");
            foreach (var kind in new[] { "junction request (actual reparse point)", "junction claim (actual reparse point)", "linked request naming horse-A (simulated)", "recovered linked claim (simulated)" })
            {
                using (var folders = new Folders())
                {
                    var host = new Host(folders); var session = new HorseTripRepairSession(); host.Loaded.Add(LegacyHorse("horse-A"));
                    var request = Path.Combine(folders.UserData, HorseTripRepairPolicy.RequestFileName);
                    var claim = Path.Combine(folders.UserData, Claimed);
                    var sibling = Path.Combine(folders.Root, "sibling"); Directory.CreateDirectory(sibling);
                    File.WriteAllText(Path.Combine(sibling, "target.txt"), forA);
                    var linkPath = kind.Contains("claim") ? claim : request;
                    bool created;
                    if (kind.Contains("junction"))
                    {
                        var p = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("cmd.exe", "/c mklink /J \"" + linkPath + "\" \"" + sibling + "\"") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true });
                        p.WaitForExit();
                        created = Directory.Exists(linkPath) && (File.GetAttributes(linkPath) & FileAttributes.ReparsePoint) != 0;
                    }
                    else
                    {
                        File.WriteAllText(linkPath, forA);
                        VerifiedFileOperations.SimulatedReparseForTests = path => string.Equals(Path.GetFullPath(path), Path.GetFullPath(linkPath), StringComparison.OrdinalIgnoreCase);
                        created = true;
                    }
                    try
                    {
                        Exception refusal = null;
                        try { Run(host, session); } catch (Exception e) { refusal = e; }
                        var archived = Directory.GetFiles(folders.UserData).Count(f => f.EndsWith(HorseTripRepairPolicy.AppliedSuffix) || f.EndsWith(HorseTripRepairPolicy.InterruptedSuffix) || f.EndsWith(".pending"));
                        var leftInPlace = kind.Contains("junction") ? Directory.Exists(linkPath) : File.Exists(linkPath);
                        Console.WriteLine("    R8-F1 " + kind + ": created=" + created + " refusal=" + (refusal?.GetType().Name ?? "<none>") + " removals=" + host.Removals +
                            " archives=" + archived + " result=" + File.Exists(Path.Combine(folders.UserData, HorseTripRepairPolicy.ResultFileName)) + " leftInPlace=" + leftInPlace + " claimed=" + (kind.Contains("request") && File.Exists(claim)));
                        TestRunner.True(created && refusal is HorseTripIneligibleEntryException && host.Removals == 0 && archived == 0 && !File.Exists(Path.Combine(folders.UserData, HorseTripRepairPolicy.ResultFileName)) && leftInPlace &&
                            !(kind.Contains("request") && (File.Exists(claim) || Directory.Exists(claim))) && File.ReadAllText(Path.Combine(sibling, "target.txt")) == forA,
                            "R8-F1 " + kind + ": a linked entry was followed, selected, removed, claimed or copied.");
                    }
                    finally { VerifiedFileOperations.SimulatedReparseForTests = null; if (kind.Contains("junction") && Directory.Exists(linkPath)) Directory.Delete(linkPath); }
                }
            }
            // Controls: an ordinary request applies; a regular recovered claim held open by another reader is read and, once released, settled.
            using (var folders = new Folders())
            {
                var host = new Host(folders); var session = new HorseTripRepairSession(); host.Loaded.Add(LegacyHorse("horse-A"));
                folders.Request(forA);
                var ordinary = Run(host, session);
                TestRunner.True(ordinary == HorseTripRepairDecision.Apply && host.Removals == 1, "R8-F1 control: an ordinary request no longer applies.");
            }
            using (var folders = new Folders())
            {
                var host = new Host(folders); var session = new HorseTripRepairSession(); host.Loaded.Add(LegacyHorse("horse-A"));
                var claim = Path.Combine(folders.UserData, Claimed);
                File.WriteAllText(claim, forA);
                bool firstThrew;
                using (new FileStream(claim, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Write))
                    firstThrew = Throws(() => Run(host, session));
                var settled = Run(host, session);
                Console.WriteLine("    R8-F1 locked regular claim control: while held open by a reader the retirement waited (threw=" + firstThrew + "); released: " + settled +
                    ", interrupted archive exact=" + (Archive(folders, Interrupted) == forA));
                TestRunner.True(firstThrew && (settled == HorseTripRepairDecision.Interrupted || settled == HorseTripRepairDecision.FinishedDecided) && Archive(folders, Interrupted) == forA &&
                    host.Removals == 0 && !File.Exists(claim), "R8-F1 control: a regular claim held by another reader was not recovered.");
            }
        }

        // PR-4 review R3-F1: the removal's diagnostic report throws after an exact removal (message construction, display-name read or
        // logger). The classified outcome already reached the policy, so the decision is applied and archived; after a reload of the
        // same horse ID with one candidate, nothing is removed again and the reloaded fact is preserved.
        private static void DiagnosticFailureAfterRemoval()
        {
            var forA = HorseTripRepairPolicy.FormatRequestLine("horse-A");
            using (var folders = new Folders())
            {
                var host = new Host(folders); var session = new HorseTripRepairSession(); var a = LegacyHorse("horse-A");
                host.Loaded.Add(a);
                host.FailReport = true;
                folders.Request(forA);
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.Apply && host.Removals == 1 && host.ReportCalls == 1 &&
                    folders.UserFiles().SequenceEqual(Files(Applied, ResultFile)) && Archive(folders, Applied) == forA && folders.Result().Contains("Applied") &&
                    !session.HoldsClaim, "R3-F1: a failing removal report changed the applied decision or its archive.");
                var reloaded = LegacyHorse("horse-A");
                host.Loaded.Clear(); host.Loaded.Add(reloaded);
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.NoRequest && Run(host, new HorseTripRepairSession()) == HorseTripRepairDecision.NoRequest &&
                    host.Removals == 1 && host.NativeCalls == 1 && Candidates(reloaded).Count == 1 && folders.UserFiles().SequenceEqual(Files(Applied, ResultFile)),
                    "R3-F1: after the reload the fact was removed again or the archive changed.");
            }
        }

        // PR-4 review R3-F2: the actual final archive write fails after the decision (a directory occupies the archive name). The claim
        // stays discoverable at its own name: a newer request written meanwhile waits; once the obstruction is gone the original bytes
        // reach the applied archive before the newer request runs. A fresh session meeting the same state settles it as interrupted
        // with no further removal.
        private static void FinalPublicationFailureResumes()
        {
            var forA = HorseTripRepairPolicy.FormatRequestLine("horse-A");
            var forB = HorseTripRepairPolicy.FormatRequestLine("horse-B");
            using (var folders = new Folders())
            {
                var host = new Host(folders); var session = new HorseTripRepairSession(); var a = LegacyHorse("horse-A"); var b = LegacyHorse("horse-B");
                host.Loaded.Add(a); host.Loaded.Add(b);
                var obstruction = Path.Combine(folders.UserData, Applied);
                folders.Request(forA);
                host.files.BeforePublish = () => { host.files.BeforePublish = null; Directory.CreateDirectory(obstruction); };
                TestRunner.True(Throws(() => Run(host, session)) && host.Removals == 1 && session.HoldsClaim && File.Exists(Path.Combine(folders.UserData, Claimed)) &&
                    folders.Result().Contains("Applied"), "R3-F2: the failed archive write lost the claim or its decision.");
                folders.Request(forB);
                TestRunner.True(Throws(() => Run(host, session)) && host.Removals == 1 && File.Exists(Path.Combine(folders.UserData, Claimed)) &&
                    Archive(folders, Request) == forB && Candidates(b).Count == 1, "R3-F2: a newer request ran while the original archive was pending.");
                Directory.Delete(obstruction);
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.FinishedApplied && Archive(folders, Applied) == forA && host.Removals == 1 &&
                    Archive(folders, Request) == forB && !File.Exists(Path.Combine(folders.UserData, Claimed)), "R3-F2: the original bytes did not reach the applied archive first.");
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.Apply && host.Removals == 2 && host.RemovedFrom.SequenceEqual(new[] { "horse-A", "horse-B" }),
                    "R3-F2: the waiting newer request was not processed after the original archive completed.");
            }
            using (var folders = new Folders())
            {
                var host = new Host(folders); var a = LegacyHorse("horse-A"); host.Loaded.Add(a);
                var obstruction = Path.Combine(folders.UserData, Applied);
                folders.Request(forA);
                host.files.BeforePublish = () => { host.files.BeforePublish = null; Directory.CreateDirectory(obstruction); };
                TestRunner.True(Throws(() => Run(host, new HorseTripRepairSession())) && host.Removals == 1 && File.Exists(Path.Combine(folders.UserData, Claimed)),
                    "R3-F2 fresh session: the staged state was not left discoverable.");
                var reloaded = LegacyHorse("horse-A"); host.Loaded.Clear(); host.Loaded.Add(reloaded);
                TestRunner.True(Run(host, new HorseTripRepairSession()) == HorseTripRepairDecision.Interrupted && host.Removals == 1 && host.NativeCalls == 1 &&
                    Candidates(reloaded).Count == 1 && Archive(folders, Interrupted) == forA && !File.Exists(Path.Combine(folders.UserData, Claimed)),
                    "R3-F2 fresh session: the stranded claim was not settled conservatively, or a removal ran.");
            }
        }

        // PR-4 review R1-F4: the claim file changes after its bytes were verified and before terminal publication. The archive holds
        // exactly the verified bytes (A's request); the replacement (B's) never acquires A's applied suffix, keeps its own disposition
        // (interrupted on the next poll), and only A's removal ran.
        private static void ChangedAfterVerification()
        {
            var forA = HorseTripRepairPolicy.FormatRequestLine("horse-A");
            var forB = HorseTripRepairPolicy.FormatRequestLine("horse-B");
            foreach (var moment in new[] { "before publication", "before the claim name is settled" })
            using (var folders = new Folders())
            {
                var host = new Host(folders); var session = new HorseTripRepairSession(); var a = LegacyHorse("horse-A"); var b = LegacyHorse("horse-B");
                host.Loaded.Add(a); host.Loaded.Add(b);
                var claimPath = Path.Combine(folders.UserData, Claimed);
                folders.Request(forA);
                Action replace = () => File.WriteAllText(claimPath, forB);
                if (moment == "before publication") host.files.BeforePublish = () => { host.files.BeforePublish = null; replace(); };
                else host.files.BeforeSettle = () => { host.files.BeforeSettle = null; replace(); };
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.Apply && host.Removals == 1 && Archive(folders, Applied) == forA &&
                    Archive(folders, Claimed) == forB, "R1-F4 (" + moment + "): the archive did not hold the verified bytes, or the replacement was lost.");
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.Interrupted && host.Removals == 1 && host.RemovedFrom.SequenceEqual(new[] { "horse-A" }) &&
                    Candidates(b).Count == 1 && Archive(folders, Interrupted) == forB && Archive(folders, Applied) == forA &&
                    !Directory.GetFiles(folders.UserData).Any(path => path.EndsWith(HorseTripRepairPolicy.AppliedSuffix, StringComparison.Ordinal) && File.ReadAllText(path) == forB),
                    "R1-F4 (" + moment + "): B's bytes acquired A's applied suffix or lost their own disposition.");
            }
        }

        // PR-4 review R1-F4: the claim for horse A is replaced by horse B's bytes during publication, after the successful claim read
        // and the decision: B's bytes never receive A's .applied suffix; the result names the right horse each time; one removal.
        private static void ReplacedDuringPublication()
        {
            var forA = HorseTripRepairPolicy.FormatRequestLine("horse-A");
            var forB = HorseTripRepairPolicy.FormatRequestLine("horse-B");
            using (var folders = new Folders())
            {
                var host = new Host(folders); var session = new HorseTripRepairSession(); var a = LegacyHorse("horse-A"); var b = LegacyHorse("horse-B");
                host.Loaded.Add(a); host.Loaded.Add(b);
                folders.Request(forA);
                var claimPath = Path.Combine(folders.UserData, Claimed);
                host.AfterResultWrite = () => { host.AfterResultWrite = null; File.WriteAllText(claimPath, forB); };
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.Apply && host.Removals == 1 && host.RemovedFrom.SequenceEqual(new[] { "horse-A" }) &&
                    folders.Result().Contains("Applied") && folders.Result().Contains("horse-A") && !session.HoldsClaim &&
                    folders.UserFiles().SequenceEqual(Files(Claimed, ResultFile)) && Archive(folders, Claimed) == forB,
                    "R1-F4: the replaced claim was archived under A's decision, or A's application was not reported.");
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.Interrupted && host.Removals == 1 && Candidates(b).Count == 1 &&
                    folders.UserFiles().SequenceEqual(Files(Interrupted, ResultFile)) && Archive(folders, Interrupted) == forB &&
                    folders.Result().Contains("horse-B") && folders.Result().Contains("Interrupted"),
                    "R1-F4: B's replacement bytes did not keep their own conservative disposition.");
                TestRunner.True(!Directory.GetFiles(folders.UserData).Any(path => path.EndsWith(HorseTripRepairPolicy.AppliedSuffix, StringComparison.Ordinal) &&
                    File.ReadAllText(path) == forB), "R1-F4: B's bytes appeared in an .applied archive.");
            }
            // Control: no replacement: the bound bytes are archived under the decision (the staged verification passes).
            using (var folders = new Folders())
            {
                var host = new Host(folders); host.Loaded.Add(LegacyHorse("horse-A"));
                folders.Request(forA);
                TestRunner.True(Run(host, new HorseTripRepairSession()) == HorseTripRepairDecision.Apply && folders.UserFiles().SequenceEqual(Files(Applied, ResultFile)) &&
                    Archive(folders, Applied) == forA, "R1-F4 control: the verified claim was not archived as applied.");
            }
        }

        private static string DirectoryDigest(string directory)
        {
            using (var sha = SHA256.Create())
                return string.Join("|", Directory.GetFiles(directory).OrderBy(path => path, StringComparer.Ordinal)
                    .Select(path => Path.GetFileName(path) + ":" + BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path)))).ToArray());
        }

        // PR-4 review R1-F2: a removal whose outcome cannot be verified, or which changed more than the requested fact, is a
        // terminal Uncertain outcome: never another selection or removal under that claim, also after a reload with one candidate.
        // Control: a removal that completed exactly and then threw is applied once.
        private static void UncertainRemovalIsTerminal()
        {
            var forA = HorseTripRepairPolicy.FormatRequestLine("horse-A");
            var uncertain = HorseTripRepairPolicy.RequestFileName + HorseTripRepairPolicy.UncertainSuffix;
            // Presence read fails after the removal; the horse is then reloaded under the same ID with one candidate.
            using (var folders = new Folders())
            {
                var host = new Host(folders); var session = new HorseTripRepairSession(); host.Loaded.Add(LegacyHorse("horse-A"));
                folders.Request(forA);
                host.FailReadAfterRemoval = true;
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.Uncertain && host.Removals == 1 && !session.HoldsClaim &&
                    folders.UserFiles().SequenceEqual(Files(ResultFile, uncertain)) && Archive(folders, uncertain) == forA &&
                    folders.Result().Contains("Uncertain:") && !folders.Result().Contains("Applied"),
                    "R1-F2: an unverifiable removal was not reported as uncertain.");
                host.Loaded.Clear(); var reloaded = LegacyHorse("horse-A"); host.Loaded.Add(reloaded);
                for (var poll = 0; poll < 2; poll++) TestRunner.Equal(HorseTripRepairDecision.NoRequest, Run(host, session), "R1-F2: the uncertain claim was processed again.");
                TestRunner.True(host.Removals == 1 && Candidates(reloaded).Count == 1, "R1-F2: a reloaded copy was removed after an unverifiable removal.");
            }
            // The same with a failed result write: the retry only reports the uncertain outcome, nothing is selected or removed again.
            using (var folders = new Folders())
            {
                var host = new Host(folders); var session = new HorseTripRepairSession(); host.Loaded.Add(LegacyHorse("horse-A"));
                folders.Request(forA);
                host.FailReadAfterRemoval = true; host.FailResultWrite = true;
                TestRunner.True(Throws(() => Run(host, session)) && host.Removals == 1 && session.PendingReports == 1 &&
                    folders.UserFiles().SequenceEqual(Files(Claimed)), "R1-F2: the uncertain outcome was not frozen before its failed report.");
                host.Loaded.Clear(); var reloaded = LegacyHorse("horse-A"); host.Loaded.Add(reloaded);
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.FinishedDecided && host.Removals == 1 && Candidates(reloaded).Count == 1 &&
                    folders.UserFiles().SequenceEqual(Files(ResultFile, uncertain)) && folders.Result().Contains("Uncertain:"),
                    "R1-F2: the retry of an uncertain outcome selected or removed again.");
            }
            // The native removal took the target and a neighbouring fact and returned: classified inconsistent, the terminal outcome.
            using (var folders = new Folders())
            {
                var host = new Host(folders); var session = new HorseTripRepairSession(); var a = LegacyHorse("horse-A"); host.Loaded.Add(a);
                folders.Request(forA);
                host.AlsoRemoveNeighbour = true;
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.Uncertain && folders.UserFiles().SequenceEqual(Files(ResultFile, uncertain)) &&
                    folders.Result().Contains("Failed:") && folders.Result().Contains("did not change exactly that one fact") && !folders.Result().Contains("Applied") &&
                    Run(host, session) == HorseTripRepairDecision.NoRequest,
                    "R1-F2: a removal that changed more than the requested fact was not reported as an integrity failure.");
            }
            // Control: the removal completed exactly and a later callback threw: applied, once.
            using (var folders = new Folders())
            {
                var host = new Host(folders); var a = LegacyHorse("horse-A"); host.Loaded.Add(a);
                folders.Request(forA);
                host.FailRemovalAfter = true;
                TestRunner.True(Run(host, new HorseTripRepairSession()) == HorseTripRepairDecision.Apply && host.Removals == 1 && Candidates(a).Count == 0 &&
                    folders.UserFiles().SequenceEqual(Files(Applied, ResultFile)), "R1-F2 control: an exact removal followed by a throw was not applied.");
            }
        }

        // PR-4 review R1-F4: the claim is bound to its raw bytes. A decided claim replaced by the same visible line in another
        // encoding is not this process's claim (closed as interrupted, archived byte for byte); a transient failure observing the
        // held claim (the production read under a sharing violation) keeps the session's frozen outcome for the next poll.
        private static void ClaimIdentityIsBytes()
        {
            var forA = HorseTripRepairPolicy.FormatRequestLine("horse-A");
            using (var folders = new Folders())
            {
                var host = new Host(folders); var session = new HorseTripRepairSession(); host.Loaded.Add(new Horse { Id = "horse-A" });
                folders.Request(forA);
                host.FailClose = true;
                TestRunner.True(Throws(() => Run(host, session)) && session.PendingReports == 1 && folders.UserFiles().SequenceEqual(Files(Claimed, ResultFile)),
                    "R1-F4: the refusal was not frozen before its failed archive.");
                var replacement = new System.Text.UTF8Encoding(true).GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(forA)).ToArray();
                File.WriteAllBytes(Path.Combine(folders.UserData, Claimed), replacement);
                TestRunner.Equal(forA, File.ReadAllText(Path.Combine(folders.UserData, Claimed)), "The replacement does not decode to the same visible line.");
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.Interrupted && host.Removals == 0 &&
                    folders.UserFiles().SequenceEqual(Files(Interrupted, ResultFile)) &&
                    File.ReadAllBytes(Path.Combine(folders.UserData, Interrupted)).SequenceEqual(replacement) && !folders.Result().Contains("Refused:"),
                    "R1-F4: a re-encoded replacement received the old refusal or was not archived byte for byte.");
            }
            using (var folders = new Folders())
            {
                var host = new Host(folders); var session = new HorseTripRepairSession(); host.Loaded.Add(new Horse { Id = "horse-A" });
                folders.Request(forA);
                host.FailClose = true;
                TestRunner.True(Throws(() => Run(host, session)) && session.PendingReports == 1, "R1-F4: the refusal was not frozen.");
                using (new FileStream(Path.Combine(folders.UserData, Claimed), FileMode.Open, FileAccess.Read, FileShare.None))
                    TestRunner.True(Throws(() => Run(host, session)) && session.PendingReports == 1 && session.HoldsClaim,
                        "R1-F4: a transient failure observing the held claim cleared the session.");
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.FinishedDecided && host.Removals == 0 &&
                    folders.UserFiles().SequenceEqual(Files(Refused, ResultFile)) &&
                    File.ReadAllBytes(Path.Combine(folders.UserData, Refused)).SequenceEqual(System.Text.Encoding.UTF8.GetBytes(forA)),
                    "R1-F4: the original refusal did not survive a transient observation failure.");
            }
        }

        // PR-4 review R1-F5: a newer request that takes the request name during the waiting claim's return rename supersedes the
        // waiting claim; its horse loading before the next poll is never touched, and the newer request is applied once.
        private static void ReturnRenameRace()
        {
            var forA = HorseTripRepairPolicy.FormatRequestLine("horse-A");
            var forB = HorseTripRepairPolicy.FormatRequestLine("horse-B");
            using (var folders = new Folders())
            {
                var host = new Host(folders); var session = new HorseTripRepairSession();
                folders.Request(forA);
                host.files.BeforeReturn = () => { folders.Request(forB); host.files.BeforeReturn = null; };
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.Superseded && host.Removals == 0 &&
                    folders.UserFiles().SequenceEqual(Files(Request, Superseded, ResultFile)) && Archive(folders, Superseded) == forA &&
                    Archive(folders, Request) == forB, "R1-F5: the return-rename collision was not recorded as supersession.");
                var a = LegacyHorse("horse-A"); var b = LegacyHorse("horse-B"); host.Loaded.Add(a); host.Loaded.Add(b);
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.Apply && host.RemovedFrom.SequenceEqual(new[] { "horse-B" }) &&
                    Candidates(a).Count == 1 && Archive(folders, Applied) == forB, "R1-F5: the waiting horse was touched or the newer request not applied.");
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.NoRequest && host.Removals == 1 && Candidates(a).Count == 1,
                    "R1-F5: a later poll removed from the superseded horse.");
            }
        }

        // Review R7-F5: once a decision is made it is frozen in the session (Undecided -> Decided -> Reported -> Archived) before
        // any publication I/O, so a failed result write or archive rename is retried as reporting only: candidates are never
        // selected again, nothing is removed again and no other request is consumed meanwhile, even when the horse's candidates
        // change before the retry. Controls: a restarted session closes such a claim as interrupted with no removal; failures
        // before the removal and after it are covered by PendingAndRetry, ReportingFailures and TransientFailureAfterClaim.
        private static void TerminalDecisionsStayTerminal()
        {
            var forA = HorseTripRepairPolicy.FormatRequestLine("horse-A");
            var forB = HorseTripRepairPolicy.FormatRequestLine("horse-B");
            // Ambiguous refusal, archive rename fails; the horse is then reloaded with ONE candidate: no removal, refused archive.
            using (var folders = new Folders())
            {
                var host = new Host(folders); var session = new HorseTripRepairSession(); var a = LegacyHorse("horse-A"); a.Facts.Add(StockGrant("TrippingBite-2"));
                host.Loaded.Add(a);
                folders.Request(forA);
                host.FailClose = true;
                TestRunner.True(Throws(() => Run(host, session)) && host.Removals == 0 && folders.UserFiles().SequenceEqual(Files(Claimed, ResultFile)) &&
                    folders.Result().Contains("Refused:") && session.PendingReports == 1,
                    "R7-F5 ambiguous: the refusal was not frozen before its failed archive.");
                host.Loaded.Clear(); var reloaded = LegacyHorse("horse-A"); host.Loaded.Add(reloaded);
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.FinishedDecided && host.Removals == 0 && Candidates(reloaded).Count == 1 &&
                    folders.UserFiles().SequenceEqual(Files(Refused, ResultFile)) && Archive(folders, Refused) == forA &&
                    folders.Result().Contains("Refused:") && !session.HoldsClaim,
                    "R7-F5 ambiguous: the retry reconsidered the reloaded horse or did not archive the refusal.");
            }
            // No-candidate refusal, result write fails; a candidate then appears: the retry only reports the refusal.
            using (var folders = new Folders())
            {
                var host = new Host(folders); var session = new HorseTripRepairSession(); var a = new Horse { Id = "horse-A" }; host.Loaded.Add(a);
                folders.Request(forA);
                host.FailResultWrite = true;
                TestRunner.True(Throws(() => Run(host, session)) && host.Removals == 0 && folders.UserFiles().SequenceEqual(Files(Claimed)),
                    "R7-F5 no-candidate: the failed result write changed the claim.");
                a.Facts.Add(StockGrant());
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.FinishedDecided && host.Removals == 0 && Candidates(a).Count == 1 &&
                    folders.UserFiles().SequenceEqual(Files(Refused, ResultFile)) && folders.Result().Contains("no sourceless stock TrippingBite"),
                    "R7-F5 no-candidate: the retry selected the new candidate.");
            }
            // Supersession, result write fails with the newer request present; the waiting horse is then loaded with one candidate:
            // the retry only reports the supersession, the newer request is not consumed until the superseded claim is archived,
            // and it is then processed once on its own.
            using (var folders = new Folders())
            {
                var host = new Host(folders); var session = new HorseTripRepairSession(); var b = LegacyHorse("horse-B"); host.Loaded.Add(b);
                folders.Request(forA);
                host.files.AfterClaim = () => { folders.Request(forB); host.files.AfterClaim = null; };
                host.FailResultWrite = true;
                TestRunner.True(Throws(() => Run(host, session)) && host.Removals == 0 && folders.UserFiles().SequenceEqual(Files(Claimed, Request)) &&
                    Archive(folders, Request) == forB, "R7-F5 supersession: the failed result write changed the claim or the newer request.");
                var a = LegacyHorse("horse-A"); host.Loaded.Add(a);
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.FinishedDecided && host.Removals == 0 && Candidates(a).Count == 1 &&
                    folders.UserFiles().SequenceEqual(Files(Request, Superseded, ResultFile)) && Archive(folders, Superseded) == forA &&
                    Archive(folders, Request) == forB && folders.Result().Contains("Superseded:"),
                    "R7-F5 supersession: the retry reconsidered horse A or consumed the newer request.");
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.Apply && host.RemovedFrom.SequenceEqual(new[] { "horse-B" }) &&
                    Archive(folders, Applied) == forB && Candidates(a).Count == 1, "R7-F5 supersession: the newer request was not processed once on its own.");
            }
            // Supersession, archive rename fails after the result was written: the retry only archives.
            using (var folders = new Folders())
            {
                var host = new Host(folders); var session = new HorseTripRepairSession(); var b = LegacyHorse("horse-B"); host.Loaded.Add(b);
                folders.Request(forA);
                host.files.AfterClaim = () => { folders.Request(forB); host.files.AfterClaim = null; };
                host.FailClose = true;
                TestRunner.True(Throws(() => Run(host, session)) && folders.UserFiles().SequenceEqual(Files(Claimed, Request, ResultFile)) &&
                    folders.Result().Contains("Superseded:"), "R7-F5 supersession archive: the result was not published first.");
                host.Loaded.Add(LegacyHorse("horse-A"));
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.FinishedDecided && host.Removals == 0 &&
                    folders.UserFiles().SequenceEqual(Files(Request, Superseded, ResultFile)) && Archive(folders, Superseded) == forA,
                    "R7-F5 supersession archive: the retry did more than archive the superseded claim.");
            }
            // Application, result write fails; a reload then restores the fact: finished as applied, never removed again.
            using (var folders = new Folders())
            {
                var host = new Host(folders); var session = new HorseTripRepairSession(); var a = LegacyHorse("horse-A"); host.Loaded.Add(a);
                folders.Request(forA);
                host.FailResultWrite = true;
                TestRunner.True(Throws(() => Run(host, session)) && host.Removals == 1 && Candidates(a).Count == 0, "R7-F5 applied: the removal did not happen once.");
                a.Facts.Insert(0, StockGrant());
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.FinishedApplied && host.Removals == 1 && Candidates(a).Count == 1 &&
                    folders.UserFiles().SequenceEqual(Files(Applied, ResultFile)) && folders.Result().Contains("Applied:"),
                    "R7-F5 applied: the retry removed again or did not finish as applied.");
            }
            // Application, archive rename fails; the horse is then unloaded: the retry only archives, as applied.
            using (var folders = new Folders())
            {
                var host = new Host(folders); var session = new HorseTripRepairSession(); var a = LegacyHorse("horse-A"); host.Loaded.Add(a);
                folders.Request(forA);
                host.FailClose = true;
                TestRunner.True(Throws(() => Run(host, session)) && host.Removals == 1 && folders.UserFiles().SequenceEqual(Files(Claimed, ResultFile)),
                    "R7-F5 applied archive: the result was not published first.");
                host.Loaded.Clear();
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.FinishedApplied && host.Removals == 1 &&
                    folders.UserFiles().SequenceEqual(Files(Applied, ResultFile)), "R7-F5 applied archive: the retry did not finish as applied.");
            }
            // Removal completes although Remove throws: verified gone, so frozen as applied; a failing result write then retries reporting only.
            using (var folders = new Folders())
            {
                var host = new Host(folders); var session = new HorseTripRepairSession(); var a = LegacyHorse("horse-A"); host.Loaded.Add(a);
                folders.Request(forA);
                host.FailRemovalAfter = true; host.FailResultWrite = true;
                TestRunner.True(Throws(() => Run(host, session)) && host.Removals == 1 && session.PendingReports == 1 && folders.UserFiles().SequenceEqual(Files(Claimed)),
                    "R7-F5 removal exception: a completed removal was not frozen as applied.");
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.FinishedApplied && host.Removals == 1 && Archive(folders, Applied) == forA,
                    "R7-F5 removal exception: the retry did not finish as applied once.");
            }
            // Control: a restarted session finds a decided-but-unarchived claim it does not hold: interrupted, nothing removed.
            using (var folders = new Folders())
            {
                var host = new Host(folders); var a = LegacyHorse("horse-A"); a.Facts.Add(StockGrant("TrippingBite-2")); host.Loaded.Add(a);
                folders.Request(forA);
                host.FailClose = true;
                TestRunner.True(Throws(() => Run(host, new HorseTripRepairSession())), "R7-F5 restart control: the archive failure was not injected.");
                a.Facts.RemoveAt(a.Facts.Count - 1);
                TestRunner.True(Run(host, new HorseTripRepairSession()) == HorseTripRepairDecision.Interrupted && host.Removals == 0 && Candidates(a).Count == 1 &&
                    folders.UserFiles().SequenceEqual(Files(Interrupted, ResultFile)), "R7-F5 restart control: a claim this process did not hold was decided.");
            }
        }

        // Review R6-F4: a transient failure after the claim rename and before the removal (reading the claimed bytes, or the
        // horse lookup) keeps this process's exact claim; the next poll in the same session re-processes those bytes and applies
        // them once. Controls: a restarted session closes a claim it did not take as interrupted; a closed gate keeps the claim;
        // a newer request is still respected; post-removal recovery is unchanged (ReportingFailures, StrandedClaims).
        private static void TransientFailureAfterClaim()
        {
            var forA = "# from the log\r\n" + HorseTripRepairPolicy.FormatRequestLine("horse-A") + "\r\n";
            foreach (var failRead in new[] { true, false })
                using (var folders = new Folders())
                {
                    var host = new Host(folders); var session = new HorseTripRepairSession();
                    var a = LegacyHorse("horse-A"); var b = LegacyHorse("horse-B"); host.Loaded.Add(a); host.Loaded.Add(b);
                    folders.Request(forA);
                    if (failRead) host.FailClaimReadAfterClaim = true; else host.FailLookupAfterClaim = true;
                    var label = failRead ? "read" : "lookup";
                    TestRunner.True(Throws(() => Run(host, session)) && host.Removals == 0 && session.HoldsClaim &&
                        folders.UserFiles().SequenceEqual(Files(Claimed)) && Archive(folders, Claimed) == forA,
                        "A " + label + " failure after the claim lost or changed the exact claim.");
                    // A closed gate on the next poll keeps the claim untouched.
                    TestRunner.True(Run(host, session, false) == HorseTripRepairDecision.Pending && host.Removals == 0 &&
                        folders.UserFiles().SequenceEqual(Files(Claimed)), "A closed gate disposed of the retained claim (" + label + ").");
                    TestRunner.True(Run(host, session) == HorseTripRepairDecision.Apply && host.RemovedFrom.SequenceEqual(new[] { "horse-A" }) &&
                        Candidates(a).Count == 0 && Candidates(b).Count == 1 && folders.UserFiles().SequenceEqual(Files(Applied, ResultFile)) &&
                        Archive(folders, Applied) == forA && folders.Result().Contains("horse-A") && !folders.Result().Contains("Interrupted") &&
                        !session.HoldsClaim, "The retained claim was not applied once to its horse after a " + label + " failure.");
                    TestRunner.True(Run(host, session) == HorseTripRepairDecision.NoRequest && host.Removals == 1, "The applied claim was processed again (" + label + ").");
                }
            // Restarted-session control: the same stranded claim in a new process (no record of taking it) is interrupted, unchanged.
            using (var folders = new Folders())
            {
                var host = new Host(folders); var a = LegacyHorse("horse-A"); host.Loaded.Add(a);
                folders.Request(forA);
                host.FailLookupAfterClaim = true;
                TestRunner.True(Throws(() => Run(host, new HorseTripRepairSession())) && host.Removals == 0, "The lookup failure was not injected.");
                TestRunner.True(Run(host, new HorseTripRepairSession()) == HorseTripRepairDecision.Interrupted && host.Removals == 0 && Candidates(a).Count == 1 &&
                    folders.UserFiles().SequenceEqual(Files(Interrupted, ResultFile)) && Archive(folders, Interrupted) == forA,
                    "A claim not taken by this process was re-processed.");
            }
            // A newer request is respected: the retained claim's horse is not loaded on the next poll and the owner wrote a newer
            // request meanwhile, so the retained claim is set aside unchanged and the newer one is processed on its own.
            using (var folders = new Folders())
            {
                var host = new Host(folders); var session = new HorseTripRepairSession(); var b = LegacyHorse("horse-B"); host.Loaded.Add(LegacyHorse("horse-A"));
                folders.Request(forA);
                host.FailClaimReadAfterClaim = true;
                TestRunner.True(Throws(() => Run(host, session)) && session.HoldsClaim, "The read failure was not injected.");
                var forB = HorseTripRepairPolicy.FormatRequestLine("horse-B");
                folders.Request(forB);
                host.Loaded.Clear(); host.Loaded.Add(b);
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.Superseded && host.Removals == 0 && Archive(folders, Superseded) == forA &&
                    Archive(folders, Request) == forB && !session.HoldsClaim, "The retained claim overrode a newer request.");
                TestRunner.True(Run(host, session) == HorseTripRepairDecision.Apply && host.RemovedFrom.SequenceEqual(new[] { "horse-B" }) &&
                    Archive(folders, Applied) == forB, "The newer request was not processed on its own.");
            }
        }
    }
}
