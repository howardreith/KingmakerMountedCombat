using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace KingmakerMountedCombat.Domain
{
    // MMR-06 existing horses (reviews R3 R1-F1, R4-F4, R4-F5, R4-F6). A horse created by KMC 0.2.16-alpha.1 or earlier
    // received the stock Mammoth's TrippingBite as a sourceless fact. A sourceless TrippingBite granted any other way (a
    // console command, another mod) looks exactly the same in the save: neither its list position, its neighbours, its GUID
    // nor the missing source proves who granted it. So nothing is removed automatically. KMC only reports each candidate in
    // its log (exact KMC Horse, exact stock grant by identity and Bite/Trip shape, no source fact, item or cutscene) with the
    // exact request line, and removes that one fact from that one horse only when the owner has written that request in the
    // KMC user-data folder (never the Mods tree). A request is single use: it is claimed before anything changes, and once
    // the removal happened it can only finish as applied.
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
        // A claim stranded without a recorded removal in this session (for example the game closed mid-request): nothing is
        // removed; the owner checks the horse and writes a new request.
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

        // The whole request lifecycle against its host (reviews R4-F5, R5-F1).
        //  - A stranded claim is settled first: if this session recorded its removal, only the reporting is finished (never a
        //    second removal, also after a reload restored the fact); otherwise it is closed as interrupted, unchanged.
        //  - A present request with the write gate closed is pending: nothing is claimed, read for a decision or changed.
        //  - Otherwise the request is CLAIMED FIRST (atomic rename) and every selection and disposition is made from the bytes
        //    of the claimed file alone (review R5-F1): a request the owner replaced before the claim is judged as replaced; one
        //    written after the claim is a separate, later request. The request observed before the claim only signals presence.
        //  - HorseNotLoaded returns the claim to the request name, unless a newer request appeared meanwhile: then the claimed
        //    one is set aside unchanged as superseded. Malformed, no-candidate and ambiguous claimed requests are refused.
        //  - Apply removes the fact named by the claimed bytes; a removal that throws while the fact is still present returns
        //    the claim and propagates (retried later); once the fact is gone the outcome is recorded in the session before any
        //    reporting, so a failing result write or rename can only be finished as applied.
        public static HorseTripRepairDecision Process<TFact>(IHorseTripRepairHost<TFact> host, bool gateAdmitted, HorseTripRepairSession session)
            where TFact : class
        {
            if (host == null) throw new ArgumentNullException(nameof(host));
            if (session == null) throw new ArgumentNullException(nameof(session));
            string claimText;
            if (host.TryReadClaim(out claimText))
            {
                var claimedId = ParseRequest(claimText);
                string applied;
                if (claimedId != null && session.TryGetApplied(claimedId, out applied))
                {
                    host.WriteResult(applied);
                    host.Close(true, AppliedSuffix);
                    session.Complete(claimedId);
                    session.ReleaseClaim();
                    return HorseTripRepairDecision.FinishedApplied;
                }
                // Review R6-F4: a claim this process took and never brought to a removal (a transient read or lookup failure
                // after the rename) is this process's own: it is re-processed from its exact claimed bytes. The write gate still
                // applies (a closed gate keeps the claim untouched). Only a claim this process did not take (the game restarted
                // mid-request) is closed as interrupted.
                if (session.HoldsClaim)
                {
                    if (!gateAdmitted) return HorseTripRepairDecision.Pending;
                    return DecideClaimed(host, session, claimText);
                }
                host.WriteResult("Interrupted: a claimed request for horse " + (claimedId ?? "<unreadable>") + " did not finish in this game " +
                    "session, so KMC made no further change. Check that horse in game (a bite hit followed by a Trip means the fact is still " +
                    "there) and write a new request if you still want it removed.");
                host.Close(true, InterruptedSuffix);
                return HorseTripRepairDecision.Interrupted;
            }
            // No claim file: any record of an earlier claim of this process is stale.
            session.ReleaseClaim();
            string observed;
            if (!host.TryReadRequest(out observed)) return HorseTripRepairDecision.NoRequest;
            if (!gateAdmitted) return HorseTripRepairDecision.Pending;
            host.Claim();
            // Recorded the moment the rename succeeded: whatever fails before the removal, the next poll re-processes this claim.
            session.TakeClaim();
            string text;
            if (!host.TryReadClaim(out text)) throw new IOException("The claimed Horse Trip repair request could not be read back.");
            return DecideClaimed(host, session, text);
        }

        // Every selection and disposition from the claimed file's bytes (review R5-F1), for a fresh claim or this process's own
        // retained claim (review R6-F4). A failure before the removal leaves the claim in place and recorded as this process's.
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
                    if (host.ReleaseClaim()) { session.ReleaseClaim(); break; }
                    host.WriteResult("Superseded: the request for horse " + horseId + " was waiting for that horse to be loaded when a newer " +
                        "request was written, so it was set aside unchanged; the newer request is processed on its own.");
                    host.Close(true, SupersededSuffix);
                    session.ReleaseClaim();
                    return HorseTripRepairDecision.Superseded;
                case HorseTripRepairDecision.Apply:
                    var fact = candidates[0];
                    try { host.Remove(fact); }
                    catch (Exception)
                    {
                        if (host.StillPresent(fact))
                        {
                            if (host.ReleaseClaim()) session.ReleaseClaim();
                            else
                            {
                                host.WriteResult("Superseded: removing the TrippingBite from horse " + horseId + " failed with nothing changed, " +
                                    "and a newer request was written meanwhile, so this one was set aside; the newer request is processed on its own.");
                                host.Close(true, SupersededSuffix);
                                session.ReleaseClaim();
                            }
                            throw;
                        }
                    }
                    var result = AppliedResult(horseId);
                    session.MarkApplied(horseId, result);
                    host.WriteResult(result);
                    host.Close(true, AppliedSuffix);
                    session.Complete(horseId);
                    session.ReleaseClaim();
                    break;
                case HorseTripRepairDecision.Malformed:
                    host.WriteResult("Refused: the request must contain exactly one line of the form horse=<UniqueId> (copy it from the " +
                        "KMC log line 'KMC Horse Trip repair candidate'); blank lines and lines starting with # are ignored. Nothing was changed.");
                    host.Close(true, RefusedSuffix);
                    session.ReleaseClaim();
                    break;
                case HorseTripRepairDecision.NoCandidate:
                    host.WriteResult("Refused: horse " + horseId + " has no sourceless stock TrippingBite to remove. Nothing was changed.");
                    host.Close(true, RefusedSuffix);
                    session.ReleaseClaim();
                    break;
                case HorseTripRepairDecision.Ambiguous:
                    host.WriteResult("Refused: horse " + horseId + " has " + candidates.Count +
                        " sourceless stock TrippingBite facts, so the inherited one cannot be singled out. Nothing was changed.");
                    host.Close(true, RefusedSuffix);
                    session.ReleaseClaim();
                    break;
            }
            return decision;
        }
    }

    // The KMC user-data folder: <data parent>/KingmakerMountedCombat (normally %USERPROFILE%\AppData\LocalLow\Owlcat Games\
    // Pathfinder Kingmaker\KingmakerMountedCombat), beside the save roots, never the Mods tree.
    public static class HorseTripRepairPaths
    {
        public static string Folder(string dataParent) =>
            string.IsNullOrEmpty(dataParent) ? null : Path.Combine(dataParent, HorseTripRepairPolicy.FolderName);
    }

    // Per game process: the requests whose removal happened, until their reporting finished.
    public sealed class HorseTripRepairSession
    {
        private readonly Dictionary<string, string> applied = new Dictionary<string, string>(StringComparer.Ordinal);

        // Review R6-F4: this process renamed the request to the claim and has not yet closed, returned or applied it.
        public bool HoldsClaim { get; private set; }
        public void TakeClaim() => HoldsClaim = true;
        public void ReleaseClaim() => HoldsClaim = false;

        public void MarkApplied(string horseId, string result) => applied[horseId] = result;

        public bool TryGetApplied(string horseId, out string result) => applied.TryGetValue(horseId, out result);

        public void Complete(string horseId) => applied.Remove(horseId);

        public int PendingReports => applied.Count;
    }

    public interface IHorseTripRepairHost<TFact> where TFact : class
    {
        bool TryReadRequest(out string text);
        bool TryReadClaim(out string text);
        // The exact candidates on the loaded horse with this UniqueId; loaded is false when no such horse is loaded.
        IList<TFact> CandidatesOf(string horseId, out bool loaded);
        bool StillPresent(TFact fact);
        void Remove(TFact fact);
        void Claim();
        // Returns the claim to the request name; false (claim kept) when a newer request exists.
        bool ReleaseClaim();
        void WriteResult(string result);
        // Renames the claim (claimed) or the request to request + suffix.
        void Close(bool claimed, string suffix);
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

        public bool TryReadRequest(out string text) => TryRead(RequestPath, out text);

        public bool TryReadClaim(out string text) => TryRead(ClaimPath, out text);

        private bool TryRead(string path, out string text)
        {
            text = FolderPresent && File.Exists(path) ? File.ReadAllText(path) : null;
            return text != null;
        }

        // Test hooks only (review R5-F1 evidence): invoked immediately before and after the claim rename, so a test can replace
        // the request at the exact moment between observation and claim, or write a newer request after it. Unset in production.
        public Action BeforeClaim { get; set; }
        public Action AfterClaim { get; set; }

        public void Claim()
        {
            BeforeClaim?.Invoke();
            File.Move(RequestPath, ClaimPath);
            AfterClaim?.Invoke();
        }

        public bool ReleaseClaim()
        {
            if (File.Exists(RequestPath)) return false;
            File.Move(ClaimPath, RequestPath);
            return true;
        }

        public void WriteResult(string result) =>
            File.WriteAllText(ResultPath, DateTime.UtcNow.ToString("u", CultureInfo.InvariantCulture) + "\r\n" + result + "\r\n");

        public void Close(bool claimed, string suffix)
        {
            var target = RequestPath + suffix;
            if (File.Exists(target))
                target = RequestPath + "." + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture) + suffix;
            File.Move(claimed ? ClaimPath : RequestPath, target);
        }
    }
}
