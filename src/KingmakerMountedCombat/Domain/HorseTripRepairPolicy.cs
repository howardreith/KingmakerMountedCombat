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

        // PR-4 round 6 (R5-F2): the owner-facing result of an interrupted claim, naming its own horse and how to retry.
        // PR-4 round 7 (R6-F2): states only what this recovery knows: it made no further change, and an earlier session may already
        // have removed the fact. The retry guidance uses the existing exact diagnostic: KMC logs 'KMC Horse Trip repair candidate' for a
        // loaded horse only while that horse still has the exact sourceless stock TrippingBite.
        public static string InterruptedResult(string horseId) =>
            "Interrupted: the claimed request for horse " + (horseId ?? "<unreadable>") + " could not be finished as claimed (it was left by an " +
            "earlier game session, or it replaced a request KMC was settling). This recovery made no further change; an earlier session may " +
            "already have removed the fact. To check, load the game with that horse: KMC writes the log line 'KMC Horse Trip repair candidate' " +
            "for horse=" + (horseId ?? "<unreadable>") + " only while that exact fact is still present. Write a new request only if that line " +
            "appears for this horse again.";

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
                // PR-4 round 6 (R4-F2, R4-F3, R5-F2): a claim this session does not own (left by an earlier game session, or bytes that
                // replaced this session's claim during its publication) takes the same terminal lifecycle as every other disposition: it is
                // frozen as Interrupted for its exact bytes, named in its own owner-facing result, and published through Publish. A retry
                // after a failed result write, archive commit or retirement therefore finishes only that publication (one result, one
                // interruption archive, no selection or removal), and no newer request is admitted while the claim file exists.
                session.Take(claimIdentity);
                session.Decide(HorseTripRepairDecision.Interrupted, InterruptedResult(ParseRequest(claimText)), InterruptedSuffix);
                return Publish(host, session);
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
                    // PR-4 review R3-F1: the classification is back in the policy before any fallible diagnostic; the removal report
                    // (message construction, display-name read, logger) runs contained, so it can never leave the claim undecided.
                    try { host.ReportRemoval(horseId, removal); }
                    catch (Exception) { }
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
                // the decision, Close leaves the replacement at the claim name, untouched, and the next poll gives it its own Interrupted
                // decision, result and archive through this same lifecycle (round 6, R5-F2) before any newer request is admitted.
                // PR-4 review R4-F2: the committed archive is kept on the session, so a retry after a failed settlement only settles.
                host.Close(true, session.Suffix, session.Identity, session);
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
        // PR-4 review R4-F2: the terminal archive this claim's decision was committed to (null until committed). It belongs to this
        // claim only: a later request, even with identical bytes, is a new claim with its own record.
        public string CommittedArchive { get; private set; }
        public void RecordCommittedArchive(string path) => CommittedArchive = path;

        // This process renamed the request to the claim and has not yet archived, returned or abandoned it.
        public bool HoldsClaim => Stage != HorseTripClaimStage.None;

        // PR-4 review R1-F4: the SHA-256 of the claim bytes this decision is bound to (null until the bytes were read).
        public string Identity => Stage == HorseTripClaimStage.None || bytesUnknown ? null : claimText;

        // A decided claim whose reporting has not finished (1 or 0).
        public int PendingReports => Stage == HorseTripClaimStage.Decided || Stage == HorseTripClaimStage.Reported ? 1 : 0;

        // PR-4 round 7 (R6-F1): taking a claim starts its own record: every publication field of a previous claim (its committed
        // archive included) is reset. Retries of the same claim never call Take again, so they keep their state.
        public void Take(string text)
        {
            claimText = text; bytesUnknown = false; Stage = HorseTripClaimStage.Undecided;
            Decision = HorseTripRepairDecision.Pending; Result = null; Suffix = null; PublicationAttempted = false;
            CommittedArchive = null;
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
            CommittedArchive = null;
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
            catch (Exception unreadable) { return new HorseTripRemovalResult(HorseTripRemovalOutcome.NotAttempted, unreadable, "the features could not be read before removal: " + MessageOf(unreadable)); }
            Exception nativeFailure = null;
            try { nativeRemove(target); }
            catch (Exception exception) { nativeFailure = exception; }
            List<T> after;
            try { after = Snapshot(readFeatures()); }
            catch (Exception unreadable)
            {
                return new HorseTripRemovalResult(HorseTripRemovalOutcome.Unreadable, nativeFailure,
                    "the features could not be read after removal: " + MessageOf(unreadable) + Native(nativeFailure));
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

        private static string Native(Exception failure) => failure == null ? string.Empty : "; native removal threw " + failure.GetType().Name + ": " + MessageOf(failure);

        // PR-4 review R3-F1 (round 5): an exception's text is diagnostic only; an unreadable message (its getter throws) becomes a safe
        // fallback, so the classified outcome always reaches the policy.
        private static string MessageOf(Exception failure)
        {
            try { return failure.Message; }
            catch (Exception) { return "<message unreadable>"; }
        }
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
        // PR-4 review R3-F1: the removal's diagnostic report, called by the policy after the classified outcome returned, contained.
        void ReportRemoval(string horseId, HorseTripRemovalResult removal);
        void Claim();
        // Returns the claim to the request name; false (claim kept) when a newer request exists.
        bool ReleaseClaim();
        void WriteResult(string result);
        // Renames the claim (claimed) or the request to request + suffix. With a bound identity, only those exact claim bytes are
        // archived (PR-4 review R1-F4); false means the claim file held other bytes, which were left for their own disposition. A
        // failure leaves the claim at its name (PR-4 review R3-F2), so the next poll resumes this publication before newer work.
        bool Close(bool claimed, string suffix, string boundIdentity, HorseTripRepairSession session = null);
    }

    // PR-4 rounds 7-8 (R4-F3, R5-F1, R7-F1, R7-F2): file operations bound to the open handle of the exact file that was written or verified.
    // On Windows every step goes straight to kernel32, so no managed FileStream overload (whose handling of the requested rights differs
    // between the .NET Framework and the Unity Mono runtime Kingmaker uses) sits between the request and the native open:
    //  - CreateFileW with an explicit access mask (GENERIC_READ, DELETE, and GENERIC_WRITE for the staged archive), no sharing at all (no
    //    other reader, writer, renamer or deleter while the handle is held) and FILE_FLAG_OPEN_REPARSE_POINT, so a symbolic link or other
    //    reparse point is opened as itself, never followed to a target elsewhere;
    //  - an opened claim that is a reparse point or a directory (GetFileInformationByHandleEx FileAttributeTagInfo) is refused and left in
    //    place, recoverable, for manual handling: nothing is deleted through it;
    //  - ReadFile / WriteFile / FlushFileBuffers on that handle, and SetFileInformationByHandle FileRenameInfo (ReplaceIfExists false) and
    //    FileDispositionInfo, which act on the file the handle refers to, not on a pathname.
    // Other platforms keep the bounded pathname contract (Supported is false there).
    // PR-4 round 9 (R8-F1): a request or claim that is a link, reparse point or directory. It is refused before any selection, removal
    // or archive copy and left in place; the poll logs it once and retries until an ordinary file replaces it.
    public sealed class HorseTripIneligibleEntryException : IOException
    {
        public HorseTripIneligibleEntryException(string path)
            : base("The Horse Trip repair file " + path + " is a link, reparse point or directory. KMC does not follow it and leaves it in place; " +
                   "replace it with an ordinary text file.") { }
    }

    // PR-4 round 10 (owner instruction 2026-11-27a section 7; reviews R5-F1, R9-F2, R9-F3): the Linux/macOS Mono branch's file operations act
    // on DESCRIPTORS, never on a pathname chosen earlier:
    //  - Reading the bytes that decide a request, claim or recovery entry: open(O_RDONLY|O_NOFOLLOW|O_NONBLOCK), regular-file eligibility from
    //    fstat of THAT descriptor, then read through it. A symbolic link at the name fails the open (ELOOP); a directory, FIFO or device is
    //    refused by its descriptor's type; either way HorseTripIneligibleEntryException before any selection, removal or copy.
    //  - Publishing an archive (no staging name exists in either mechanism, so nothing can replace or edit a staged file by name):
    //     1. Linux: the payload is written and flushed into an UNNAMED file of the folder (open(dir, O_TMPFILE)), which no other process can
    //        reach by any name, and published by linkat of /proc/self/fd/<fd> (AT_SYMLINK_FOLLOW): one non-overwriting link of that open file,
    //        so the terminal name appears with the complete bytes or not at all (EEXIST leaves an existing entry untouched). Closing the
    //        descriptor after any failure leaves nothing behind.
    //     2. Where an unnamed file or the descriptor link is unavailable (macOS; a filesystem without O_TMPFILE; no /proc; a failed link): the
    //        terminal name itself is created exclusively (O_CREAT|O_EXCL|O_NOFOLLOW) with permission bits 0, so no other non-root process can
    //        open it for reading or writing while the verified payload (held in memory, never re-read from a file) is written and flushed
    //        through this descriptor; fchmod(0644) on the descriptor is the publication point, so the first readable bytes are the complete
    //        payload. The terminal name must then still refer to the descriptor's file (lstat/fstat device and inode), otherwise the
    //        publication is refused and the replacement left alone. A failure before publication removes the attempt's own unreadable file
    //        only while the name still refers to it, so repeated failed polls leave nothing (a crash can leave at most that one unreadable file).
    // The primitives are behind IPosixFiles (production: LibcPosixFiles); the tests substitute a simulated boundary on Windows.
    internal struct PosixIdentity
    {
        public ulong Device; public ulong Inode; public uint Mode;
        public bool IsRegular => (Mode & 0xF000u) == 0x8000u;
        public bool SameFile(PosixIdentity other) => Device == other.Device && Inode == other.Inode;
    }

    internal sealed class PosixFlags
    {
        public int ReadOnly = 0, WriteOnly = 1, Create, Exclusive, NoFollow, NonBlock, CloseOnExec;
        public int ENOENT = 2, EINTR = 4, EACCES = 13, EEXIST = 17, ENOTDIR = 20, ELOOP, ENOTSUP;
        internal static readonly PosixFlags Linux = new PosixFlags { Create = 0x40, Exclusive = 0x80, NoFollow = 0x20000, NonBlock = 0x800, CloseOnExec = 0x80000, ELOOP = 40, ENOTSUP = 95 };
        internal static readonly PosixFlags Mac = new PosixFlags { Create = 0x200, Exclusive = 0x800, NoFollow = 0x100, NonBlock = 0x4, CloseOnExec = 0x1000000, ELOOP = 62, ENOTSUP = 45 };
    }

    internal interface IPosixFiles
    {
        PosixFlags Flags { get; }
        int Errno { get; }
        int Open(string path, int flags, int mode);
        int OpenUnnamed(string directory);
        int Read(int fd, byte[] buffer, int count);
        int Write(int fd, byte[] buffer, int count);
        int Fsync(int fd);
        int Fchmod(int fd, int mode);
        int Fstat(int fd, out PosixIdentity identity);
        int Lstat(string path, out PosixIdentity identity);
        int LinkDescriptor(int fd, string newPath);
        int Unlink(string path);
        int Close(int fd);
    }

    // Production primitives (x86_64 Linux and macOS, the platforms Kingmaker ships for). struct stat: Linux x86_64 st_dev u64 @0, st_ino u64 @8,
    // st_mode u32 @24 (fstat/lstat through syscall(2) numbers 5 and 6, independent of the glibc version); macOS struct stat64 (the $INODE64
    // entry points): st_dev i32 @0, st_mode u16 @4, st_ino u64 @8. Verified on Linux by the round-10 syscall probe (decomposition/pr4/r10).
    internal sealed class LibcPosixFiles : IPosixFiles
    {
        private static readonly bool Mac = File.Exists("/usr/lib/libSystem.dylib") || System.IO.Directory.Exists("/System/Library/CoreServices");
        private const int StatBufferSize = 256, AtCurrentDirectory = -100, AtSymlinkFollow = 0x400, LinuxUnnamed = 0x410000, OwnerReadWrite = 0x180;
        [System.Runtime.InteropServices.DllImport("libc", EntryPoint = "open", SetLastError = true)] private static extern int open(string path, int flags, int mode);
        [System.Runtime.InteropServices.DllImport("libc", EntryPoint = "read", SetLastError = true)] private static extern IntPtr read(int fd, byte[] buffer, IntPtr count);
        [System.Runtime.InteropServices.DllImport("libc", EntryPoint = "write", SetLastError = true)] private static extern IntPtr write(int fd, byte[] buffer, IntPtr count);
        [System.Runtime.InteropServices.DllImport("libc", EntryPoint = "fsync", SetLastError = true)] private static extern int fsync(int fd);
        [System.Runtime.InteropServices.DllImport("libc", EntryPoint = "fchmod", SetLastError = true)] private static extern int fchmod(int fd, int mode);
        [System.Runtime.InteropServices.DllImport("libc", EntryPoint = "close", SetLastError = true)] private static extern int close(int fd);
        [System.Runtime.InteropServices.DllImport("libc", EntryPoint = "unlink", SetLastError = true)] private static extern int unlink(string path);
        [System.Runtime.InteropServices.DllImport("libc", EntryPoint = "linkat", SetLastError = true)] private static extern int linkat(int olddirfd, string oldpath, int newdirfd, string newpath, int flags);
        [System.Runtime.InteropServices.DllImport("libc", EntryPoint = "syscall", SetLastError = true)] private static extern long LinuxFstat(long number, int fd, byte[] buffer);
        [System.Runtime.InteropServices.DllImport("libc", EntryPoint = "syscall", SetLastError = true)] private static extern long LinuxLstat(long number, string path, byte[] buffer);
        [System.Runtime.InteropServices.DllImport("libc", EntryPoint = "fstat$INODE64", SetLastError = true)] private static extern int MacFstat(int fd, byte[] buffer);
        [System.Runtime.InteropServices.DllImport("libc", EntryPoint = "lstat$INODE64", SetLastError = true)] private static extern int MacLstat(string path, byte[] buffer);

        public PosixFlags Flags => Mac ? PosixFlags.Mac : PosixFlags.Linux;
        public int Errno { get; private set; }
        private int Done(long result) { Errno = result < 0 ? System.Runtime.InteropServices.Marshal.GetLastWin32Error() : 0; return result < 0 ? -1 : (int)result; }
        private int Unsupported() { Errno = Flags.ENOTSUP; return -1; }

        public int Open(string path, int flags, int mode) => Done(open(path, flags, mode));
        public int OpenUnnamed(string directory) => Mac || IntPtr.Size != 8 ? Unsupported() : Done(open(directory, LinuxUnnamed | Flags.WriteOnly | Flags.CloseOnExec, OwnerReadWrite));
        public int Read(int fd, byte[] buffer, int count) => Done(read(fd, buffer, new IntPtr(count)).ToInt64());
        public int Write(int fd, byte[] buffer, int count) => Done(write(fd, buffer, new IntPtr(count)).ToInt64());
        public int Fsync(int fd) => Done(fsync(fd));
        public int Fchmod(int fd, int mode) => Done(fchmod(fd, mode));
        public int Close(int fd) => Done(close(fd));
        public int Unlink(string path) => Done(unlink(path));
        public int LinkDescriptor(int fd, string newPath)
        {
            if (Mac || !System.IO.Directory.Exists("/proc/self/fd")) return Unsupported();
            return Done(linkat(AtCurrentDirectory, "/proc/self/fd/" + fd.ToString(CultureInfo.InvariantCulture), AtCurrentDirectory, newPath, AtSymlinkFollow));
        }
        public int Fstat(int fd, out PosixIdentity identity)
        {
            var buffer = new byte[StatBufferSize]; identity = default(PosixIdentity);
            if (IntPtr.Size != 8) return Unsupported();
            var result = Done(Mac ? MacFstat(fd, buffer) : LinuxFstat(5, fd, buffer));
            if (result == 0) identity = Parse(buffer);
            return result;
        }
        public int Lstat(string path, out PosixIdentity identity)
        {
            var buffer = new byte[StatBufferSize]; identity = default(PosixIdentity);
            if (IntPtr.Size != 8) return Unsupported();
            var result = Done(Mac ? MacLstat(path, buffer) : LinuxLstat(6, path, buffer));
            if (result == 0) identity = Parse(buffer);
            return result;
        }
        private static PosixIdentity Parse(byte[] b) => Mac
            ? new PosixIdentity { Device = (uint)BitConverter.ToInt32(b, 0), Mode = BitConverter.ToUInt16(b, 4), Inode = BitConverter.ToUInt64(b, 8) }
            : new PosixIdentity { Device = BitConverter.ToUInt64(b, 0), Inode = BitConverter.ToUInt64(b, 8), Mode = BitConverter.ToUInt32(b, 24) };
    }

    internal static class UnixFiles
    {
        internal static readonly IPosixFiles Production = new LibcPosixFiles();
        // Test seam: a simulated POSIX boundary replaces the production primitives (null restores them).
        internal static IPosixFiles Calls { get => calls ?? Production; set => calls = value; }
        private static IPosixFiles calls;
        private const int PublishedMode = 0x1A4;   // 0644

        private static int Retry(Func<int> call, IPosixFiles c)
        {
            int result;
            do result = call(); while (result < 0 && c.Errno == c.Flags.EINTR);
            return result;
        }

        // R9-F2: the deciding bytes, from the descriptor whose regular-file type was established.
        internal static byte[] ReadEligible(string path)
        {
            var c = Calls; var f = c.Flags;
            var fd = Retry(() => c.Open(path, f.ReadOnly | f.NoFollow | f.NonBlock | f.CloseOnExec, 0), c);
            if (fd < 0)
            {
                var error = c.Errno;
                if (error == f.ELOOP) throw new HorseTripIneligibleEntryException(path);
                if (error == f.ENOENT || error == f.ENOTDIR) throw new FileNotFoundException("The file " + path + " does not exist (errno " + error + ").", path);
                throw new IOException("The file " + path + " could not be opened for reading (errno " + error + ").");
            }
            try
            {
                PosixIdentity identity;
                if (c.Fstat(fd, out identity) != 0) throw new IOException("The file " + path + " could not be examined (errno " + c.Errno + ").");
                if (!identity.IsRegular) throw new HorseTripIneligibleEntryException(path);
                using (var buffer = new MemoryStream())
                {
                    var chunk = new byte[8192];
                    while (true)
                    {
                        var read = Retry(() => c.Read(fd, chunk, chunk.Length), c);
                        if (read < 0) throw new IOException("The file " + path + " could not be read (errno " + c.Errno + ").");
                        if (read == 0) return buffer.ToArray();
                        buffer.Write(chunk, 0, read);
                    }
                }
            }
            finally { c.Close(fd); }
        }

        // The descriptor as a write-only stream, so the narrow write/flush fault seam wraps the actual payload write.
        private sealed class DescriptorStream : Stream
        {
            private readonly IPosixFiles c; private readonly int fd;
            internal DescriptorStream(IPosixFiles calls, int descriptor) { c = calls; fd = descriptor; }
            public override bool CanRead => false; public override bool CanSeek => false; public override bool CanWrite => true;
            public override long Length => throw new NotSupportedException();
            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
            public override void Flush() { }
            public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count)
            {
                var done = 0;
                while (done < count)
                {
                    var part = new byte[count - done];
                    Buffer.BlockCopy(buffer, offset + done, part, 0, part.Length);
                    var written = Retry(() => c.Write(fd, part, part.Length), c);
                    if (written <= 0) throw new IOException("The archive could not be written (errno " + c.Errno + ").");
                    done += written;
                }
            }
        }

        private static void WriteAndFlush(IPosixFiles c, int fd, byte[] payload, Func<Stream, Stream> seam)
        {
            var stream = new DescriptorStream(c, fd);
            var writer = seam?.Invoke(stream) ?? stream;
            writer.Write(payload, 0, payload.Length);
            writer.Flush();
            if (Retry(() => c.Fsync(fd), c) != 0) throw new IOException("The archive could not be flushed (errno " + c.Errno + ").");
        }

        // R5-F1/R9-F3: publishes the verified payload under terminal (never replacing an entry), or throws having left no file of its own.
        // Returns the mechanism used ("unnamed-link" or "protected-create").
        internal static string Publish(byte[] payload, string terminal, Func<Stream, Stream> seam, Action beforePublication)
        {
            var c = Calls; var f = c.Flags;
            var fd = Retry(() => c.OpenUnnamed(Path.GetDirectoryName(terminal)), c);
            if (fd >= 0)
            {
                try
                {
                    WriteAndFlush(c, fd, payload, seam);
                    beforePublication?.Invoke();
                    if (c.LinkDescriptor(fd, terminal) == 0) return "unnamed-link";
                    if (c.Errno == f.EEXIST) throw new IOException("The archive name " + terminal + " is occupied; nothing was published (no staging file remains).");
                }
                finally { c.Close(fd); }   // the unnamed file ends with its descriptor
            }
            return ProtectedCreate(c, payload, terminal, seam, beforePublication);
        }

        private static string ProtectedCreate(IPosixFiles c, byte[] payload, string terminal, Func<Stream, Stream> seam, Action beforePublication)
        {
            var f = c.Flags;
            var fd = Retry(() => c.Open(terminal, f.WriteOnly | f.Create | f.Exclusive | f.NoFollow | f.CloseOnExec, 0), c);
            if (fd < 0) throw new IOException("The archive " + terminal + " could not be created exclusively (errno " + c.Errno + "); nothing was published.");
            PosixIdentity mine = default(PosixIdentity);
            var known = false;
            try
            {
                if (c.Fstat(fd, out mine) != 0) throw new IOException("The new archive " + terminal + " could not be examined (errno " + c.Errno + ").");
                known = true;
                WriteAndFlush(c, fd, payload, seam);
                beforePublication?.Invoke();
                if (c.Fchmod(fd, PublishedMode) != 0) throw new IOException("The archive " + terminal + " could not be made readable (errno " + c.Errno + ").");
                PosixIdentity named;
                if (c.Lstat(terminal, out named) != 0 || !named.SameFile(mine))
                {
                    known = false;   // the name no longer refers to this file: whatever is there now is someone else's and stays
                    throw new IOException("The archive name " + terminal + " was replaced during publication; the replacement is left in place.");
                }
                return "protected-create";
            }
            catch
            {
                PosixIdentity now;
                if (known && c.Lstat(terminal, out now) == 0 && now.SameFile(mine)) c.Unlink(terminal);
                throw;
            }
            finally { c.Close(fd); }
        }
    }

    internal static class VerifiedFileOperations
    {
        internal static bool Supported => Environment.OSVersion.Platform == PlatformID.Win32NT;

        internal const uint GenericRead = 0x80000000, GenericWrite = 0x40000000, DeleteAccess = 0x00010000;
        internal const uint CreateNewDisposition = 1, OpenExistingDisposition = 3;
        internal const uint OpenReparsePointFlag = 0x00200000, NormalAttribute = 0x80;
        private const uint ReparsePointAttribute = 0x400, DirectoryAttribute = 0x10;
        private const int FileRenameInfoClass = 3, FileDispositionInfoClass = 4, FileAttributeTagInfoClass = 9;

        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern Microsoft.Win32.SafeHandles.SafeFileHandle CreateFileW(string name, uint access, uint share, IntPtr security, uint disposition, uint flags, IntPtr template);
        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadFile(Microsoft.Win32.SafeHandles.SafeFileHandle file, byte[] buffer, int count, out int read, IntPtr overlapped);
        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteFile(Microsoft.Win32.SafeHandles.SafeFileHandle file, byte[] buffer, int count, out int written, IntPtr overlapped);
        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FlushFileBuffers(Microsoft.Win32.SafeHandles.SafeFileHandle file);
        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetFileInformationByHandleEx(Microsoft.Win32.SafeHandles.SafeFileHandle file, int informationClass, out AttributeTagInfo information, int size);
        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetFileInformationByHandle(Microsoft.Win32.SafeHandles.SafeFileHandle file, int informationClass, IntPtr information, uint size);

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct AttributeTagInfo { public uint FileAttributes; public uint ReparseTag; }

        private static IOException Failure(string what)
        {
            var error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            return error == 2 || error == 3 ? new FileNotFoundException(what + " (Win32 error " + error + ").") : new IOException(what + " (Win32 error " + error + ").");
        }

        // The exclusive handle on one exact file, readable and (for the staged archive) writable as a stream.
        internal sealed class Handle : Stream
        {
            internal readonly Microsoft.Win32.SafeHandles.SafeFileHandle File;
            private readonly bool writable;
            internal Handle(Microsoft.Win32.SafeHandles.SafeFileHandle file, bool writable) { File = file; this.writable = writable; }
            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => writable;
            public override long Length => throw new NotSupportedException();
            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
            public override int Read(byte[] buffer, int offset, int count)
            {
                var chunk = new byte[count];
                int read;
                if (!ReadFile(File, chunk, count, out read, IntPtr.Zero)) throw Failure("The verified file could not be read");
                Buffer.BlockCopy(chunk, 0, buffer, offset, read);
                return read;
            }
            public override void Write(byte[] buffer, int offset, int count)
            {
                var chunk = new byte[count];
                Buffer.BlockCopy(buffer, offset, chunk, 0, count);
                var done = 0;
                while (done < count)
                {
                    var part = done == 0 ? chunk : chunk.Skip(done).ToArray();
                    int written;
                    if (!WriteFile(File, part, count - done, out written, IntPtr.Zero)) throw Failure("The staged archive could not be written");
                    done += written;
                }
            }
            public override void Flush() { }
            internal void FlushToDisk() { if (!FlushFileBuffers(File)) throw Failure("The staged archive could not be flushed"); }
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            protected override void Dispose(bool disposing) { if (disposing) File.Dispose(); base.Dispose(disposing); }
        }

        // create: a new staged file (CreateNew, writable); otherwise an existing claim, refused when it is a reparse point or directory.
        internal static Handle Open(string path, bool create)
        {
            var access = GenericRead | DeleteAccess | (create ? GenericWrite : 0u);
            var file = CreateFileW(path, access, 0u, IntPtr.Zero, create ? CreateNewDisposition : OpenExistingDisposition,
                OpenReparsePointFlag | NormalAttribute, IntPtr.Zero);
            if (file.IsInvalid) { var failure = Failure("The file " + path + " could not be opened exclusively"); file.Dispose(); throw failure; }
            var handle = new Handle(file, create);
            if (!create)
            {
                AttributeTagInfo info;
                if (!GetFileInformationByHandleEx(file, FileAttributeTagInfoClass, out info, System.Runtime.InteropServices.Marshal.SizeOf(typeof(AttributeTagInfo))))
                {
                    var failure = Failure("The attributes of " + path + " could not be read"); handle.Dispose(); throw failure;
                }
                if ((info.FileAttributes & (ReparsePointAttribute | DirectoryAttribute)) != 0)
                {
                    handle.Dispose();
                    throw new IOException("The Horse Trip repair claim " + path + " is a link, reparse point or directory; it is left in place for manual handling.");
                }
            }
            return handle;
        }

        // PR-4 round 10 (R9-F1): read and delete sharing only. A handle another process holds with write access refuses this open (sharing
        // violation, an IOException the poll logs and retries), so a request still being written is never decided; read-only holders stay
        // compatible.
        private const uint ShareReadDelete = 5, BackupSemanticsFlag = 0x02000000;
        // Test seam (PR-4 round 9): reports an opened entry as a reparse point (a simulated external boundary for a file symbolic link,
        // which this machine cannot create without privilege). Null in production.
        internal static Func<string, bool> SimulatedReparseForTests { get; set; }

        // PR-4 round 9 (R8-F1): the bytes that decide a request or claim come from a handle opened WITHOUT following links
        // (FILE_FLAG_OPEN_REPARSE_POINT; FILE_FLAG_BACKUP_SEMANTICS so a directory or junction opens as itself and is classified), sharing
        // read and delete (round 10, R9-F1: an eligible file another READER holds is still read; a writer excludes it), and checked (FileAttributeTagInfo) before
        // a single byte is read: a reparse point or directory raises HorseTripIneligibleEntryException, before any selection, removal or
        // archive copy.
        internal static byte[] ReadEligible(string path)
        {
            var file = CreateFileW(path, GenericRead, ShareReadDelete, IntPtr.Zero, OpenExistingDisposition, OpenReparsePointFlag | BackupSemanticsFlag | NormalAttribute, IntPtr.Zero);
            if (file.IsInvalid) { var failure = Failure("The file " + path + " could not be opened for reading"); file.Dispose(); throw failure; }
            using (var handle = new Handle(file, false))
            {
                AttributeTagInfo info;
                if (!GetFileInformationByHandleEx(file, FileAttributeTagInfoClass, out info, System.Runtime.InteropServices.Marshal.SizeOf(typeof(AttributeTagInfo))))
                    throw Failure("The attributes of " + path + " could not be read");
                if ((info.FileAttributes & (ReparsePointAttribute | DirectoryAttribute)) != 0 || SimulatedReparseForTests != null && SimulatedReparseForTests(path))
                    throw new HorseTripIneligibleEntryException(path);
                using (var buffer = new MemoryStream()) { handle.CopyTo(buffer); return buffer.ToArray(); }
            }
        }

        // Renames the file this handle refers to; refuses (IOException) when the target exists.
        internal static void RenameTo(Handle file, string target)
        {
            var name = System.Text.Encoding.Unicode.GetBytes(Path.GetFullPath(target));
            var nameOffset = IntPtr.Size * 2 + 4;   // FILE_RENAME_INFO: BOOLEAN/DWORD, HANDLE RootDirectory, DWORD FileNameLength, WCHAR FileName[]
            var size = nameOffset + name.Length + 2;
            var buffer = System.Runtime.InteropServices.Marshal.AllocHGlobal(size);
            try
            {
                for (var i = 0; i < size; i++) System.Runtime.InteropServices.Marshal.WriteByte(buffer, i, 0);
                System.Runtime.InteropServices.Marshal.WriteInt32(buffer, IntPtr.Size * 2, name.Length);
                System.Runtime.InteropServices.Marshal.Copy(name, 0, IntPtr.Add(buffer, nameOffset), name.Length);
                if (!SetFileInformationByHandle(file.File, FileRenameInfoClass, buffer, (uint)size)) throw Failure("The verified file could not be renamed to " + target);
            }
            finally { System.Runtime.InteropServices.Marshal.FreeHGlobal(buffer); }
        }

        // Marks the file this handle refers to for deletion when the handle closes.
        internal static void DeleteWhenClosed(Handle file)
        {
            var buffer = System.Runtime.InteropServices.Marshal.AllocHGlobal(4);
            try
            {
                System.Runtime.InteropServices.Marshal.WriteInt32(buffer, 1);
                if (!SetFileInformationByHandle(file.File, FileDispositionInfoClass, buffer, 1)) throw Failure("The verified file could not be marked for deletion");
            }
            finally { System.Runtime.InteropServices.Marshal.FreeHGlobal(buffer); }
        }
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

        public bool TryReadClaim(out string text, out string identity)
        {
            // PR-4 round 6: a claim stranded under its retirement name is restored first, so it is always found at the claim name.
            if (!string.IsNullOrEmpty(Directory) && System.IO.Directory.Exists(Directory)) RecoverRetiring();
            return TryRead(ClaimPath, out text, out identity);
        }

        // PR-4 review R1-F4: reads the raw bytes once; absence is only a confirmed missing file or folder, every other failure
        // propagates (a transient access error must not read as "no claim"). Text is decoded as File.ReadAllText does.
        private bool TryRead(string path, out string text, out string identity)
        {
            text = null; identity = null;
            // Not FolderPresent: Directory.Exists also answers false on an access error; only a confirmed missing folder is absence.
            if (string.IsNullOrEmpty(Directory)) return false;
            byte[] bytes;
            try { bytes = ReadEligible(path); }
            catch (FileNotFoundException) { return false; }
            catch (DirectoryNotFoundException) { return false; }
            using (var reader = new StreamReader(new MemoryStream(bytes), System.Text.Encoding.UTF8, true)) text = reader.ReadToEnd();
            using (var sha = System.Security.Cryptography.SHA256.Create())
                identity = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", string.Empty);
            return true;
        }

        // PR-4 round 9 (R8-F1) / round 10 (R9-F2): the influential bytes of a request, claim or recovery entry, from an entry that is not a
        // link, reparse point or directory. Windows: the no-follow handle (VerifiedFileOperations.ReadEligible). Linux/macOS: a no-follow
        // descriptor whose regular-file type is established by fstat and through which the bytes are read (UnixFiles.ReadEligible): one open.
        private static byte[] ReadEligible(string path) => HandleOperations ? VerifiedFileOperations.ReadEligible(path) : UnixFiles.ReadEligible(path);

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

        // Test hooks (PR-4 reviews R1-F4/R3-F2): BeforePublish runs after the claim's bytes were read and verified and before the
        // archive is written; BeforeSettle after the archive was written and before the claim name is settled. Unset in production.
        public Action BeforePublish { get; set; }
        public Action BeforeSettle { get; set; }

        public bool Close(bool claimed, string suffix, string boundIdentity, HorseTripRepairSession session = null)
        {
            if (!claimed || boundIdentity == null)
            {
                File.Move(claimed ? ClaimPath : RequestPath, ArchiveTarget(suffix));
                return true;
            }
            // PR-4 reviews R1-F4/R3-F2/R4-F1/R4-F2/R4-F3/R5-F1 (round 6). One terminal publication for every disposition. The claim never
            // leaves its own name until its retirement, so it stays discoverable (a retry or a fresh session finds it before any newer request).
            //  1. Commit: unless this claim's archive was already committed (the session's record), the claim's bytes are read and verified
            //     against the decision; the verified payload is staged in a private file and published under a non-overwriting terminal
            //     name while the staging handle is still held (Commit), so a terminal name only ever holds the complete payload.
            //  2. Retire: the claim file itself is moved, in one atomic rename, to a private name only this call uses, so the file that is
            //     examined is the file that will be retired. Its bytes are read under an open handle; only the decided bytes are deleted.
            //     Any other bytes (a replacement) are moved back to the claim name untouched and nothing is archived for them here: the
            //     policy gives them their own Interrupted decision, result and archive. If that move-back fails they stay at the private
            //     name, which the next read of the claim restores (RecoverRetiring), so a replacement is never lost. Nothing is deleted
            //     before its identity is established; no delete-on-close is armed in advance.
            var committed = session?.CommittedArchive;
            if (committed == null || !File.Exists(committed))
            {
                var verified = ReadEligible(ClaimPath);
                if (!string.Equals(Digest(verified), boundIdentity, StringComparison.Ordinal)) return false;
                BeforePublish?.Invoke();
                committed = Commit(verified, suffix);
                session?.RecordCommittedArchive(committed);
            }
            BeforeSettle?.Invoke();
            BeforeRetire?.Invoke();
            if (HandleOperations)
            {
                // PR-4 round 7 (R4-F3): the claim is opened exclusively (no other reader, writer, renamer or deleter while held; a claim
                // another process holds open refuses the open, so it stays at its name and is retried), its bytes are verified through
                // that handle, and only then is that exact file marked for deletion through the same handle. A replacement that took the
                // claim name before the open is the file opened and verified: different bytes leave it untouched for its own disposition.
                using (var held = VerifiedFileOperations.Open(ClaimPath, false))
                {
                    if (!string.Equals(Digest(ReadAll(held)), boundIdentity, StringComparison.Ordinal)) return false;
                    BeforeRetireDelete?.Invoke();
                    VerifiedFileOperations.DeleteWhenClosed(held);
                }
                return true;
            }
            // Elsewhere (Linux/macOS Mono: no handle-bound delete, advisory sharing): PR-4 round 8 (R4-F3) nothing is ever deleted by a
            // pathname. The claim is moved, in one atomic rename, to a private name; its bytes are verified there; the verified file is then
            // kept under a settled name (a small leftover beside its archive) instead of being deleted, and verified again under that name.
            // Bytes that are not the decided bytes, at either check, are moved back to the claim name untouched for their own Interrupted
            // disposition, so a replacement is never consumed.
            // PR-4 round 9 (R4-F3): the settled name carries the decided identity (C.<id>.<first 16 hex of the identity>.settled; short enough for
            // the Windows path limit), so a settled entry whose bytes
            // are not that identity is distinguishable from a completed retirement; RecoverRetiring restores it (and any .retiring entry) to
            // the claim name on every claim read, in this session and a fresh one, so a failure at the second check or at the move back
            // never strands a replacement.
            var unique = Guid.NewGuid().ToString("N").Substring(0, 8);
            var retiring = ClaimPath + "." + unique + RetiringSuffix;
            File.Move(ClaimPath, retiring);   // refused (claim kept at its name) while another process holds the claim open
            if (!HoldsBytes(retiring, boundIdentity)) { File.Move(retiring, ClaimPath); return false; }
            BeforeRetireDelete?.Invoke();
            var settled = ClaimPath + "." + unique + "." + boundIdentity.Substring(0, SettledIdentityLength) + SettledSuffix;
            File.Move(retiring, settled);
            AfterSettleMove?.Invoke();
            if (HoldsBytes(settled, boundIdentity)) return true;
            BeforeSettledReturn?.Invoke();
            File.Move(settled, ClaimPath);    // a replacement that took the private name returns to the claim name for its own disposition
            return false;
        }

        // A claim moved aside for retirement and not yet deleted or returned (a failure between the rename and the delete or the move
        // back): restored to the claim name when no claim exists, so its disposition is completed by the ordinary lifecycle.
        public const string RetiringSuffix = ".retiring";
        // Non-Windows only (PR-4 round 8): a verified, retired claim kept instead of deleted.
        public const string SettledSuffix = ".settled";
        // Test seam (PR-4 round 8): runs the pathname branch on Windows so its behaviour can be exercised; false in production.
        internal static bool ForcePathnameOperations { get; set; }
        private static bool HandleOperations => VerifiedFileOperations.Supported && !ForcePathnameOperations;

        // Test hooks (PR-4 round 9, R4-F3): after the settling move and before its verification; before the move back from the settled name.
        public Action AfterSettleMove { get; set; }
        internal const int SettledIdentityLength = 16;
        public Action BeforeSettledReturn { get; set; }

        // Round 10 (R9-F2): recovery and retirement checks read through the same eligible (no-follow, regular-file) open as decisions.
        private static bool HoldsIdentityPrefix(string path, string prefix) => Digest(ReadEligible(path)).StartsWith(prefix, StringComparison.Ordinal);

        private static bool HoldsBytes(string path, string identity) => string.Equals(Digest(ReadEligible(path)), identity, StringComparison.Ordinal);

        // PR-4 round 9 (R4-F3) / round 10 (R9-F4): only POSITIVELY identifiable unfinished work is recovered. The retirement writes exactly
        // two private name shapes, C.<8 lower-case hex>.retiring and C.<8 lower-case hex>.<16 upper-case hex identity>.settled. Pending work
        // is a .retiring entry of that shape, or a .settled entry of that shape whose bytes are not the identity in its name (a replacement
        // stranded by a failed check or move back); a settled entry holding its own identity is a completed retirement. Every other name (the
        // round-8 completed record C.<32 hex>.settled, a backup, anything unrecognized) is not this service's pending work and is left exactly
        // as it is, as is an entry that is a link, directory or other non-regular file (refused by the eligible read). The oldest pending entry
        // is restored to the claim name when no claim exists, so its disposition is completed by the ordinary lifecycle.
        private static readonly System.Text.RegularExpressions.Regex RetiringName = new System.Text.RegularExpressions.Regex("^[0-9a-f]{8}" +
            System.Text.RegularExpressions.Regex.Escape(RetiringSuffix) + "$", System.Text.RegularExpressions.RegexOptions.CultureInvariant);
        private static readonly System.Text.RegularExpressions.Regex SettledName = new System.Text.RegularExpressions.Regex("^[0-9a-f]{8}\\.([0-9A-F]{16})" +
            System.Text.RegularExpressions.Regex.Escape(SettledSuffix) + "$", System.Text.RegularExpressions.RegexOptions.CultureInvariant);

        private void RecoverRetiring()
        {
            if (File.Exists(ClaimPath)) return;
            var prefix = HorseTripRepairPolicy.RequestFileName + HorseTripRepairPolicy.ClaimedSuffix + ".";
            var pending = new List<string>();
            foreach (var entry in System.IO.Directory.GetFiles(Directory, prefix + "*"))
            {
                var name = Path.GetFileName(entry);
                if (!name.StartsWith(prefix, StringComparison.Ordinal)) continue;
                var rest = name.Substring(prefix.Length);
                try
                {
                    if (RetiringName.IsMatch(rest)) { ReadEligible(entry); pending.Add(entry); continue; }
                    var settled = SettledName.Match(rest);
                    if (settled.Success && !HoldsIdentityPrefix(entry, settled.Groups[1].Value)) pending.Add(entry);
                }
                catch (HorseTripIneligibleEntryException) { }   // a link or non-regular entry is not recovered; it stays where it is
                catch (FileNotFoundException) { }
            }
            if (pending.Count == 0) return;
            pending.Sort((x, y) => File.GetLastWriteTimeUtc(x).CompareTo(File.GetLastWriteTimeUtc(y)));
            File.Move(pending[0], ClaimPath);
        }

        // Test hook (PR-4 review R3-F2/R4-F1): invoked after the private payload was written and flushed, before its publication.
        public Action BeforeCommit { get; set; }
        // Test hook (PR-4 reviews R4-F3/R5-F2): invoked after the archive was committed, immediately before the claim is retired.
        public Action BeforeRetire { get; set; }
        // Narrow fault seam (PR-4 review R4-F1, round 6), tests only: wraps the staging stream for the payload write and flush, so a test
        // can fail the actual write or flush after the private file was created. Unset in production.
        public Func<Stream, Stream> StagingWriteSeam { get; set; }
        // Test hooks (PR-4 round 7): after the claim's bytes were verified through the retiring handle and before that handle's delete;
        // and immediately after the staged archive was published under its terminal name, while the staging handle is still held.
        public Action BeforeRetireDelete { get; set; }
        public Action<string> AfterPublishRename { get; set; }

        // PR-4 reviews R4-F1/R5-F1 (round 6): the payload is staged in a private file (CreateNew) and written and flushed through one
        // handle that admits no other writer (FileShare.Read | FileShare.Delete: readers, and this call's own rename). With that handle still
        // held, File.Move publishes the staged file under a terminal name it never overwrites, and the published bytes are then read
        // back from the terminal name and compared with the payload before the handle is released. A staged file that was changed or
        // replaced in between (possible only where file sharing is advisory, as on Mono, or by renaming it away) is refused: the
        // terminal name it reached is removed again and the commit fails, so the next poll stages the payload anew. On any failure the
        // private file is removed; no partial terminal archive is ever left.
        private string Commit(byte[] payload, string suffix)
        {
            if (HandleOperations)
            {
                var pending = ClaimPath + "." + Guid.NewGuid().ToString("N").Substring(0, 8) + ".pending";
                // PR-4 round 7 (R5-F1): the payload is written and flushed through one exclusive handle (no other reader, writer, renamer or
                // deleter), and that same handle is renamed to the non-overwriting terminal name (FileRenameInfo, ReplaceIfExists false):
                // the file that appears under the terminal name is the file this handle wrote, so its first visible bytes are the payload.
                // On any failure before the rename, the staged file is deleted through its own handle; nothing else is touched.
                var staged = VerifiedFileOperations.Open(pending, true);
                var renamed = false;
                try
                {
                    var writer = StagingWriteSeam?.Invoke(staged) ?? staged;
                    writer.Write(payload, 0, payload.Length);
                    writer.Flush();
                    staged.FlushToDisk();
                    BeforeCommit?.Invoke();
                    var terminal = ArchiveTarget(suffix);
                    VerifiedFileOperations.RenameTo(staged, terminal);
                    renamed = true;
                    AfterPublishRename?.Invoke(terminal);
                    return terminal;
                }
                catch
                {
                    if (!renamed) { try { VerifiedFileOperations.DeleteWhenClosed(staged); } catch (Exception) { } }
                    throw;
                }
                finally { staged.Dispose(); }
            }
            // Elsewhere (Linux/macOS Mono): PR-4 round 10 (R5-F1, R9-F3). The verified payload is published by UnixFiles.Publish: an unnamed
            // file linked from its descriptor (Linux), or the terminal name created exclusively with permission bits 0 and made readable through
            // its descriptor once complete. Neither mechanism has a staging name, so nothing can be substituted or edited by name, no alias of
            // the archive survives, and a failed attempt leaves nothing of its own behind; the claim stays and the next poll commits again.
            var target = ArchiveTarget(suffix);
            LastPublicationMechanism = UnixFiles.Publish(payload, target, StagingWriteSeam, BeforeCommit);
            AfterPublishRename?.Invoke(target);
            return target;
        }

        // Diagnostics and tests: the pathname branch's last publication mechanism ("unnamed-link" or "protected-create").
        internal string LastPublicationMechanism { get; private set; }


        // Reads a file this call may still hold open for writing (the staging handle), so the reader shares write and delete access.
        private static byte[] ReadShared(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)) return ReadAll(stream);
        }

        private static byte[] ReadAll(Stream stream)
        {
            using (var buffer = new MemoryStream()) { stream.CopyTo(buffer); return buffer.ToArray(); }
        }

        private static string Digest(byte[] bytes)
        {
            using (var sha = System.Security.Cryptography.SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", string.Empty);
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
