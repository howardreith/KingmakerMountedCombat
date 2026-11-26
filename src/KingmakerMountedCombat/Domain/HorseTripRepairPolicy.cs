using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace KingmakerMountedCombat.Domain
{
    // MMR-06 existing horses (reviews R3 R1-F1, R4-F4, R4-F5, R4-F6). A horse created by KMC 0.2.16-alpha.1 or earlier
    // received the stock Mammoth's TrippingBite as a sourceless fact. A sourceless TrippingBite granted any other way (a
    // console command, another mod) looks exactly the same in the save: neither its list position, its neighbours, its GUID
    // nor the missing source proves who granted it. So nothing is removed automatically. KMC only reports each candidate in
    // its log (exact KMC Horse, exact stock grant by identity and Bite/Trip shape, no source fact, item or cutscene) with the
    // exact request line, and removes that one fact from that one horse only when the owner has written that request in the
    // KMC user-data folder (never the Mods tree). A request is single use: it is claimed before anything changes, and once
    // a decision is made (applied, refused or superseded) it can only finish with that decision (review R7-F5).
    public enum HorseTripRepairDecision
    {
        NoRequest,
        Malformed,
        Pending,
        HorseNotLoaded,
        NoCandidate,
        Ambiguous,
        Apply,
        // A claim stranded by a reporting failure after the removal in this session: reporting finished, nothing removed again.
        FinishedApplied,
        // Review R7-F5: a claim whose refusal or supersession was decided in this session but whose result or archive failed:
        // only the reporting is retried; candidates are never selected again and nothing is removed.
        FinishedDecided,
        // PR-4 review R1-F2: a removal was attempted but its outcome could not be verified (the presence read failed) or the
        // removal changed more than the requested fact: terminal; no further deletion under this claim.
        Uncertain,
        // A claim this process does not hold (for example the game closed mid-request): nothing is removed; the owner checks the
        // horse and writes a new request.
        Interrupted,
        // The claimed request had to wait (its horse is not loaded) but the owner wrote a newer request meanwhile: the claimed
        // one is set aside unchanged (review R5-F1) and the newer one is processed on its own later.
        Superseded
    }

    public static class HorseTripRepairPolicy
    {
        public const string FolderName = "KingmakerMountedCombat";
        public const string RequestFileName = "horse-trip-repair-request.txt";
        public const string ResultFileName = "horse-trip-repair-result.txt";
        public const string ClaimedSuffix = ".claimed";
        public const string AppliedSuffix = ".applied";
        public const string RefusedSuffix = ".refused";
        public const string InterruptedSuffix = ".interrupted";
        public const string SupersededSuffix = ".superseded";
        public const string UncertainSuffix = ".uncertain";
        private const string RequestPrefix = "horse=";

        // A repair candidate: every term must hold. An independently granted copy that records its provenance (a source
        // fact such as a class, race, progression or selection; an item; a cutscene) is never a candidate.
        public static bool IsCandidate(bool exactKmcHorse, bool stockIdentity, bool biteTripShape, bool sourceless) =>
            exactKmcHorse && stockIdentity && biteTripShape && sourceless;

        public static string FormatRequestLine(string horseId) => RequestPrefix + horseId;

        // The whole file is validated (review R4-F4): blank lines and lines starting with '#' are ignored; exactly one other
        // line must remain, and it must be exactly "horse=<UniqueId>" (surrounding spaces ignored). A second line of any kind
        // (another horse, the same horse again, stray text) makes the request malformed, so it never selects a horse.
        public static string ParseRequest(string text)
        {
            if (text == null) return null;
            string id = null;
            var lines = 0;
            foreach (var raw in text.Split('\n'))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line[0] == '#') continue;
                if (++lines > 1) return null;
                if (!line.StartsWith(RequestPrefix, StringComparison.Ordinal)) return null;
                id = line.Substring(RequestPrefix.Length).Trim();
                if (id.Length == 0 || id.IndexOfAny(new[] { ' ', '\t', '=', '#' }) >= 0) return null;
            }
            return id;
        }

        public static HorseTripRepairDecision Decide(bool requestPresent, string requestedHorseId, bool gateAdmitted, bool horseLoaded,
            int candidateCount)
        {
            if (!requestPresent) return HorseTripRepairDecision.NoRequest;
            if (requestedHorseId == null) return HorseTripRepairDecision.Malformed;
            if (!gateAdmitted) return HorseTripRepairDecision.Pending;
            if (!horseLoaded) return HorseTripRepairDecision.HorseNotLoaded;
            if (candidateCount <= 0) return HorseTripRepairDecision.NoCandidate;
            return candidateCount == 1 ? HorseTripRepairDecision.Apply : HorseTripRepairDecision.Ambiguous;
        }

        public static string AppliedResult(string horseId) =>
            "Applied: removed the inherited stock TrippingBite from horse " + horseId + ". Its bite and hoof attacks and damage are " +
            "unchanged. The change is written to your save the next time the game saves; to undo it, reload a save made before this " +
            "repair (if you already reloaded one, the fact is back: write a new request to remove it again).";

        // The whole request lifecycle against its host (reviews R4-F5, R5-F1, R6-F4, R7-F5). The session holds this process's
        // claim as one record tied to the claimed file's exact bytes and its stage: Undecided -> Decided -> Reported -> Archived.
        //  - Only an Undecided claim may select candidates or authorize a removal.
        //  - A refusal or supersession is frozen (Decided, with its result text and archive suffix) BEFORE any publication I/O;
        //    an application is frozen as soon as the removal is verified (the fact is gone), also when Remove threw although
        //    the removal completed.
        //  - A Decided claim retries only the result write, a Reported claim only the archive rename: no reconsideration, no
        //    second removal and no other request consumed while it is pending (the claim file still exists).
        //  - A claim file whose bytes are not this process's record (the game restarted mid-request) is closed as interrupted.
        //  - A present request with the write gate closed is pending: nothing is claimed, read for a decision or changed.
        //  - Otherwise the request is CLAIMED FIRST (atomic rename) and every selection and disposition is made from the bytes of
        //    the claimed file alone (review R5-F1); a waiting claim returns to the request name, or is set aside as superseded
        //    when a newer request appeared.
        public static HorseTripRepairDecision Process<TFact>(IHorseTripRepairHost<TFact> host, bool gateAdmitted, HorseTripRepairSession session)
            where TFact : class
        {
            if (host == null) throw new ArgumentNullException(nameof(host));
            if (session == null) throw new ArgumentNullException(nameof(session));
            string claimText, claimIdentity;
            if (host.TryReadClaim(out claimText, out claimIdentity))
            {
                if (session.Owns(claimIdentity))
                {
                    if (session.Stage != HorseTripClaimStage.Undecided) return Publish(host, session);
                    // Review R6-F4: this process's own undecided claim (a transient failure after the rename) is re-processed
                    // from its exact bytes; the write gate still applies.
                    if (!gateAdmitted) return HorseTripRepairDecision.Pending;
                    return DecideClaimed(host, session, claimText);
                }
                var claimedId = ParseRequest(claimText);
                session.Clear();
                host.WriteResult("Interrupted: a claimed request for horse " + (claimedId ?? "<unreadable>") + " did not finish in this game " +
                    "session, so KMC made no further change. Check that horse in game (a bite hit followed by a Trip means the fact is still " +
                    "there) and write a new request if you still want it removed.");
                host.Close(true, InterruptedSuffix, claimIdentity);
                return HorseTripRepairDecision.Interrupted;
            }
            // No claim file: any record of an earlier claim of this process is stale.
            session.Clear();
            string observed;
            if (!host.TryReadRequest(out observed)) return HorseTripRepairDecision.NoRequest;
            if (!gateAdmitted) return HorseTripRepairDecision.Pending;
            host.Claim();
            // Recorded the moment the rename succeeded (review R6-F4): the claim is this process's, undecided, with its bytes
            // bound at the first successful read, so whatever fails before a decision the next poll re-processes exactly it.
            session.TakeUnread();
            string text, identity;
            if (!host.TryReadClaim(out text, out identity)) throw new IOException("The claimed Horse Trip repair request could not be read back.");
            session.Owns(identity);
            return DecideClaimed(host, session, text);
        }

        // The poll (PR-4 review R1-F1): no folder (an unbound guarded run, or a missing user-data folder) means no request and no
        // claim recovery: nothing is read, renamed or written. Otherwise the folder's request lifecycle is processed.
        public static HorseTripRepairDecision ProcessFolder<TFact>(string directory, Func<HorseTripRepairFiles, IHorseTripRepairHost<TFact>> hostFor,
            bool gateAdmitted, HorseTripRepairSession session) where TFact : class
        {
            var files = new HorseTripRepairFiles(directory);
            if (!files.FolderPresent) return HorseTripRepairDecision.NoRequest;
            return Process(hostFor(files), gateAdmitted, session);
        }

        // Every selection and disposition from the claimed file's bytes (review R5-F1); only reached for an Undecided claim.
        private static HorseTripRepairDecision DecideClaimed<TFact>(IHorseTripRepairHost<TFact> host, HorseTripRepairSession session, string text)
            where TFact : class
        {
            var horseId = ParseRequest(text);
            var loaded = false;
            IList<TFact> candidates = null;
            if (horseId != null) candidates = host.CandidatesOf(horseId, out loaded);
            var decision = Decide(true, horseId, true, loaded, candidates == null ? 0 : candidates.Count);
            switch (decision)
            {
                case HorseTripRepairDecision.HorseNotLoaded:
                    if (host.ReleaseClaim()) { session.Clear(); return decision; }
                    session.Decide(HorseTripRepairDecision.Superseded, "Superseded: the request for horse " + horseId + " was waiting for that " +
                        "horse to be loaded when a newer request was written, so it was set aside unchanged; the newer request is processed on its own.",
                        SupersededSuffix);
                    return Publish(host, session);
                case HorseTripRepairDecision.Apply:
                    // PR-4 reviews R1-F2/R2-F1: one classified removal (HorseTripRemoval.RemoveExactly): the horse's feature identities are
                    // captured before the native removal and the collection is classified after it, on a normal return and on an exception.
                    var removal = host.RemoveExactly(candidates[0]);
                    switch (removal.Outcome)
                    {
                        case HorseTripRemovalOutcome.Removed:
                            // Exactly the requested instance is gone, every other feature is still there (also when native removal then
                            // threw): frozen as applied before any reporting.
                            session.Decide(HorseTripRepairDecision.Apply, AppliedResult(horseId), AppliedSuffix);
                            Publish(host, session);
                            return HorseTripRepairDecision.Apply;
                        case HorseTripRemovalOutcome.Unchanged:
                        case HorseTripRemovalOutcome.NotAttempted:
                            // Verified nothing changed (or nothing was attempted): still Undecided, so the request returns for a retry, or is
                            // set aside unchanged for a newer request through the existing supersession path.
                            if (host.ReleaseClaim()) session.Clear();
                            else
                            {
                                session.Decide(HorseTripRepairDecision.Superseded, "Superseded: removing the TrippingBite from horse " + horseId +
                                    " failed with nothing changed, and a newer request was written meanwhile, so this one was set aside; the newer " +
                                    "request is processed on its own.", SupersededSuffix);
                                Publish(host, session);
                            }
                            if (removal.NativeFailure != null) throw removal.NativeFailure;
                            throw new InvalidOperationException("The Horse Trip repair removal returned without removing the fact.");
                        case HorseTripRemovalOutcome.Inconsistent:
                            session.Decide(HorseTripRepairDecision.Uncertain, "Failed: removing the TrippingBite from horse " + horseId +
                                " did not change exactly that one fact (" + removal.Detail + "). KMC made no further change and will not retry " +
                                "this request; reload a save made before it if the horse is not as expected.", UncertainSuffix);
                            return Publish(host, session);
                        default:
                            session.Decide(HorseTripRepairDecision.Uncertain, "Uncertain: a removal of the TrippingBite from horse " + horseId +
                                " was attempted but its result could not be verified (" + removal.Detail + "). KMC made no further change and will " +
                                "not retry this request; check the horse in game and write a new request if the fact is still there.", UncertainSuffix);
                            return Publish(host, session);
                    }
                case HorseTripRepairDecision.Malformed:
                    session.Decide(decision, "Refused: the request must contain exactly one line of the form horse=<UniqueId> (copy it from the " +
                        "KMC log line 'KMC Horse Trip repair candidate'); blank lines and lines starting with # are ignored. Nothing was changed.",
                        RefusedSuffix);
                    return Publish(host, session);
                case HorseTripRepairDecision.NoCandidate:
                    session.Decide(decision, "Refused: horse " + horseId + " has no sourceless stock TrippingBite to remove. Nothing was changed.",
                        RefusedSuffix);
                    return Publish(host, session);
                case HorseTripRepairDecision.Ambiguous:
                    session.Decide(decision, "Refused: horse " + horseId + " has " + candidates.Count +
                        " sourceless stock TrippingBite facts, so the inherited one cannot be singled out. Nothing was changed.", RefusedSuffix);
                    return Publish(host, session);
            }
            return decision;
        }

        // Review R7-F5: publication of a frozen decision only. Decided -> (result written) Reported -> (claim archived) Archived.
        // A failure leaves the stage where it was; the next poll resumes from there. Returns the decision on the poll that made
        // it, and FinishedApplied / FinishedDecided on a later poll that completes the reporting.
        private static HorseTripRepairDecision Publish<TFact>(IHorseTripRepairHost<TFact> host, HorseTripRepairSession session) where TFact : class
        {
            var decision = session.Decision;
            var resumed = session.PublicationAttempted;
            session.MarkPublicationAttempted();
            if (session.Stage == HorseTripClaimStage.Decided)
            {
                host.WriteResult(session.Result);
                session.Advance(HorseTripClaimStage.Reported);
            }
            if (session.Stage == HorseTripClaimStage.Reported)
            {
                // PR-4 review R1-F4: only the bytes bound to this decision receive its suffix. When the claim file was replaced after
                // the decision, Close leaves the replacement in place for its own (interrupted) disposition and archives nothing here.
                host.Close(true, session.Suffix, session.Identity);
                session.Advance(HorseTripClaimStage.Archived);
            }
            session.Clear();
            if (!resumed) return decision;
            return decision == HorseTripRepairDecision.Apply ? HorseTripRepairDecision.FinishedApplied : HorseTripRepairDecision.FinishedDecided;
        }
    }

    // The KMC user-data folder: <data parent>/KingmakerMountedCombat (normally %USERPROFILE%\AppData\LocalLow\Owlcat Games\
    // Pathfinder Kingmaker\KingmakerMountedCombat), beside the save roots, never the Mods tree.
    public static class HorseTripRepairPaths
    {
        public static string Folder(string dataParent) =>
            string.IsNullOrEmpty(dataParent) ? null : Path.Combine(dataParent, HorseTripRepairPolicy.FolderName);

        // PR-4 review R1-F1: ordinary play uses the player's data parent; a guarded automation run uses only its bound isolated
        // data parent; an automation run without one processes no repair files at all (null: no folder, so neither requests nor
        // stranded claims are touched).
        public static string SelectFolder(bool automationRun, bool isolatedRootBound, string dataParent) =>
            automationRun && !isolatedRootBound ? null : Folder(dataParent);

        // PR-4 review R1-F1 (round 3): the repository's guarded launches. The KMC runtime host (-kmcRuntimeRequest) and the
        // observer-only control (-kmcObserverRequest, with no KMC automation host) are both guarded runs: neither may touch the
        // player's repair folder unless a run-owned isolated root is bound. Exact, ordinal argument names, as both launchers match.
        public const string RuntimeRequestArgument = "-kmcRuntimeRequest";
        public const string ObserverRequestArgument = "-kmcObserverRequest";

        public static bool IsGuardedLaunch(bool automationHostPresent, string[] arguments) =>
            automationHostPresent || (arguments != null && Array.Exists(arguments, argument =>
                string.Equals(argument, RuntimeRequestArgument, StringComparison.Ordinal) || string.Equals(argument, ObserverRequestArgument, StringComparison.Ordinal)));
    }

    // Review R7-F5: the stages of this process's claim. Only Undecided may select candidates or mutate a horse.
    public enum HorseTripClaimStage { None, Undecided, Decided, Reported, Archived }

    // Per game process: the one claim this process took, tied to the claimed file's exact bytes, with its stage and, once
    // decided, its frozen outcome, result text and archive suffix (reviews R4-F5, R6-F4, R7-F5).
    public sealed class HorseTripRepairSession
    {
        private string claimText;
        private bool bytesUnknown;

        public HorseTripClaimStage Stage { get; private set; }
        public HorseTripRepairDecision Decision { get; private set; }
        public string Result { get; private set; }
        public string Suffix { get; private set; }
        internal bool PublicationAttempted { get; private set; }

        // This process renamed the request to the claim and has not yet archived, returned or abandoned it.
        public bool HoldsClaim => Stage != HorseTripClaimStage.None;

        // PR-4 review R1-F4: the SHA-256 of the claim bytes this decision is bound to (null until the bytes were read).
        public string Identity => Stage == HorseTripClaimStage.None || bytesUnknown ? null : claimText;

        // A decided claim whose reporting has not finished (1 or 0).
        public int PendingReports => Stage == HorseTripClaimStage.Decided || Stage == HorseTripClaimStage.Reported ? 1 : 0;

        public void Take(string text)
        {
            claimText = text; bytesUnknown = false; Stage = HorseTripClaimStage.Undecided;
            Decision = HorseTripRepairDecision.Pending; Result = null; Suffix = null; PublicationAttempted = false;
        }

        // The rename succeeded but the bytes could not be read: the next read of the claim file supplies them.
        public void TakeUnread()
        {
            Take(null); bytesUnknown = true;
        }

        // Whether the claim file's raw bytes (their SHA-256, review PR-4 R1-F4) are exactly this process's claim. An undecided claim
        // taken with unknown bytes adopts the identity of its first successful read; a decided claim must match exactly.
        public bool Owns(string identity)
        {
            if (Stage == HorseTripClaimStage.None || identity == null) return false;
            if (bytesUnknown && Stage == HorseTripClaimStage.Undecided) { claimText = identity; bytesUnknown = false; return true; }
            return string.Equals(claimText, identity, StringComparison.Ordinal);
        }

        public void Decide(HorseTripRepairDecision decision, string result, string suffix)
        {
            if (Stage != HorseTripClaimStage.Undecided) throw new InvalidOperationException("Only an undecided claim can be decided.");
            Decision = decision; Result = result; Suffix = suffix; Stage = HorseTripClaimStage.Decided;
        }

        internal void Advance(HorseTripClaimStage stage) => Stage = stage;

        internal void MarkPublicationAttempted() => PublicationAttempted = true;

        public void Clear()
        {
            claimText = null; bytesUnknown = false; Stage = HorseTripClaimStage.None;
            Decision = HorseTripRepairDecision.Pending; Result = null; Suffix = null; PublicationAttempted = false;
        }
    }


    // PR-4 reviews R1-F2/R2-F1: the classified outcome of one native removal of one fact instance.
    public enum HorseTripRemovalOutcome
    {
        // The collection could not be read before the removal: nothing was attempted.
        NotAttempted,
        // Exactly the requested instance is gone and every other captured feature is still present.
        Removed,
        // The collection holds exactly the captured features: nothing changed.
        Unchanged,
        // Anything else (the target and a neighbour gone, or the target present with another change): terminal.
        Inconsistent,
        // The collection could not be read after the removal: terminal.
        Unreadable
    }

    public struct HorseTripRemovalResult
    {
        public HorseTripRemovalResult(HorseTripRemovalOutcome outcome, Exception nativeFailure, string detail)
        {
            Outcome = outcome; NativeFailure = nativeFailure; Detail = detail;
        }

        public HorseTripRemovalOutcome Outcome { get; }
        // The native removal's own exception, if it threw (preserved for the retry path and the log).
        public Exception NativeFailure { get; }
        public string Detail { get; }
    }

    public static class HorseTripRemoval
    {
        // Captures the feature identities, calls the native removal, and classifies the collection afterwards by reference identity,
        // on a normal return and on an exception alike. The caller supplies the read of the live collection (throwing when it is
        // unavailable) and the native removal itself; no check runs inside the native call.
        public static HorseTripRemovalResult RemoveExactly<T>(T target, Func<IEnumerable<T>> readFeatures, Action<T> nativeRemove) where T : class
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            List<T> before;
            try { before = Snapshot(readFeatures()); }
            catch (Exception unreadable) { return new HorseTripRemovalResult(HorseTripRemovalOutcome.NotAttempted, unreadable, "the features could not be read before removal: " + unreadable.Message); }
            Exception nativeFailure = null;
            try { nativeRemove(target); }
            catch (Exception exception) { nativeFailure = exception; }
            List<T> after;
            try { after = Snapshot(readFeatures()); }
            catch (Exception unreadable)
            {
                return new HorseTripRemovalResult(HorseTripRemovalOutcome.Unreadable, nativeFailure,
                    "the features could not be read after removal: " + unreadable.Message + Native(nativeFailure));
            }
            return new HorseTripRemovalResult(Classify(before, target, after), nativeFailure,
                "features " + before.Count + "->" + after.Count + Native(nativeFailure));
        }

        // By reference: Removed only when after is exactly before without one occurrence of the target and holds no other copy of it.
        public static HorseTripRemovalOutcome Classify<T>(IList<T> before, T target, IList<T> after) where T : class
        {
            if (SameMultiset(before, after)) return HorseTripRemovalOutcome.Unchanged;
            var expected = new List<T>(before);
            var index = expected.FindIndex(item => ReferenceEquals(item, target));
            if (index < 0) return HorseTripRemovalOutcome.Inconsistent;
            expected.RemoveAt(index);
            return SameMultiset(expected, after) && !after.Any(item => ReferenceEquals(item, target))
                ? HorseTripRemovalOutcome.Removed : HorseTripRemovalOutcome.Inconsistent;
        }

        private static List<T> Snapshot<T>(IEnumerable<T> features) where T : class
        {
            if (features == null) throw new InvalidOperationException("The horse's feature collection is unavailable.");
            return features.ToList();
        }

        private static bool SameMultiset<T>(IList<T> first, IList<T> second) where T : class
        {
            if (first.Count != second.Count) return false;
            var remaining = new List<T>(second);
            foreach (var item in first)
            {
                var index = remaining.FindIndex(other => ReferenceEquals(other, item));
                if (index < 0) return false;
                remaining.RemoveAt(index);
            }
            return true;
        }

        private static string Native(Exception failure) => failure == null ? string.Empty : "; native removal threw " + failure.GetType().Name + ": " + failure.Message;
    }

    public interface IHorseTripRepairHost<TFact> where TFact : class
    {
        bool TryReadRequest(out string text);
        // PR-4 review R1-F4: identity is the SHA-256 of the claim file's raw bytes (independent of text decoding).
        bool TryReadClaim(out string text, out string identity);
        // The exact candidates on the loaded horse with this UniqueId; loaded is false when no such horse is loaded.
        IList<TFact> CandidatesOf(string horseId, out bool loaded);
        // PR-4 reviews R1-F2/R2-F1: removes this one instance through HorseTripRemoval.RemoveExactly and reports its classified
        // outcome; the integration counts an application only for Removed.
        HorseTripRemovalResult RemoveExactly(TFact fact);
        void Claim();
        // Returns the claim to the request name; false (claim kept) when a newer request exists.
        bool ReleaseClaim();
        void WriteResult(string result);
        // Renames the claim (claimed) or the request to request + suffix. With a bound identity, only those exact claim bytes are
        // archived (PR-4 review R1-F4); false means the claim file held other bytes, which were left for their own disposition.
        bool Close(bool claimed, string suffix, string boundIdentity);
    }

    // The request lifecycle's files, all in the KMC user-data folder the composition root supplies (beside, never inside, the
    // save roots and never in the Mods tree). Nothing here creates that folder: a missing folder means no request.
    public sealed class HorseTripRepairFiles
    {
        public HorseTripRepairFiles(string directory)
        {
            Directory = directory;
            RequestPath = string.IsNullOrEmpty(directory) ? null : Path.Combine(directory, HorseTripRepairPolicy.RequestFileName);
        }

        public string Directory { get; }
        public string RequestPath { get; }
        public string ClaimPath => RequestPath + HorseTripRepairPolicy.ClaimedSuffix;
        public string ResultPath => Path.Combine(Directory, HorseTripRepairPolicy.ResultFileName);
        public bool FolderPresent => !string.IsNullOrEmpty(Directory) && System.IO.Directory.Exists(Directory);

        public bool TryReadRequest(out string text)
        {
            string identity;
            return TryRead(RequestPath, out text, out identity);
        }

        public bool TryReadClaim(out string text, out string identity) => TryRead(ClaimPath, out text, out identity);

        // PR-4 review R1-F4: reads the raw bytes once; absence is only a confirmed missing file or folder, every other failure
        // propagates (a transient access error must not read as "no claim"). Text is decoded as File.ReadAllText does.
        private bool TryRead(string path, out string text, out string identity)
        {
            text = null; identity = null;
            // Not FolderPresent: Directory.Exists also answers false on an access error; only a confirmed missing folder is absence.
            if (string.IsNullOrEmpty(Directory)) return false;
            byte[] bytes;
            try { bytes = File.ReadAllBytes(path); }
            catch (FileNotFoundException) { return false; }
            catch (DirectoryNotFoundException) { return false; }
            using (var reader = new StreamReader(new MemoryStream(bytes), System.Text.Encoding.UTF8, true)) text = reader.ReadToEnd();
            using (var sha = System.Security.Cryptography.SHA256.Create())
                identity = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", string.Empty);
            return true;
        }

        // Test hooks only (review R5-F1 evidence): invoked immediately before and after the claim rename, so a test can replace
        // the request at the exact moment between observation and claim, or write a newer request after it. Unset in production.
        public Action BeforeClaim { get; set; }
        public Action AfterClaim { get; set; }
        // Test hook (PR-4 review R1-F5): invoked between the request-name check and the return rename.
        public Action BeforeReturn { get; set; }

        public void Claim()
        {
            BeforeClaim?.Invoke();
            File.Move(RequestPath, ClaimPath);
            AfterClaim?.Invoke();
        }

        // Returns the claim to the request name; false (claim kept) when a newer request holds that name, including one that
        // appeared during the return itself (PR-4 review R1-F5: the non-overwriting rename's destination collision).
        public bool ReleaseClaim()
        {
            if (File.Exists(RequestPath)) return false;
            BeforeReturn?.Invoke();
            try { File.Move(ClaimPath, RequestPath); }
            catch (IOException) when (File.Exists(RequestPath) && File.Exists(ClaimPath)) { return false; }
            return true;
        }

        public void WriteResult(string result) =>
            File.WriteAllText(ResultPath, DateTime.UtcNow.ToString("u", CultureInfo.InvariantCulture) + "\r\n" + result + "\r\n");

        // Test hook (PR-4 review R1-F4): invoked after a bound claim was moved to its staging name and before its bytes are verified.
        public Action BeforeCloseVerify { get; set; }

        public bool Close(bool claimed, string suffix, string boundIdentity)
        {
            if (!claimed || boundIdentity == null)
            {
                File.Move(claimed ? ClaimPath : RequestPath, ArchiveTarget(suffix));
                return true;
            }
            // PR-4 review R1-F4: the claim is first moved atomically to a staging name of its own (whatever bytes occupy the claim name
            // now), then those staged bytes are verified against the decision's identity; only a match receives the decision's suffix.
            // Other bytes go back to the claim name (or, if a newer claim already holds it, to an interrupted archive), so they keep
            // their own conservative disposition and are never archived under this decision.
            var staging = ClaimPath + "." + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture) + ".closing";
            File.Move(ClaimPath, staging);
            BeforeCloseVerify?.Invoke();
            string identity;
            using (var sha = System.Security.Cryptography.SHA256.Create())
                identity = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(staging))).Replace("-", string.Empty);
            if (string.Equals(identity, boundIdentity, StringComparison.Ordinal))
            {
                File.Move(staging, ArchiveTarget(suffix));
                return true;
            }
            try { File.Move(staging, ClaimPath); }
            catch (IOException) when (File.Exists(ClaimPath) && File.Exists(staging)) { File.Move(staging, ArchiveTarget(HorseTripRepairPolicy.InterruptedSuffix)); }
            return false;
        }

        private string ArchiveTarget(string suffix)
        {
            var target = RequestPath + suffix;
            if (File.Exists(target))
                target = RequestPath + "." + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture) + suffix;
            return target;
        }
    }
}
