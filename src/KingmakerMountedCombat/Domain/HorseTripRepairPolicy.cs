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
        Interrupted
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

        // The whole request lifecycle against its host (review R4-F5).
        //  - A stranded claim is settled first: if this session recorded its removal, only the reporting is finished (never a
        //    second removal, also after a reload restored the fact); otherwise it is closed as interrupted, unchanged.
        //  - Pending and HorseNotLoaded keep the request. Malformed, no-candidate and ambiguous requests are refused (result
        //    written, request renamed); nothing changes.
        //  - Apply: claim the request (rename) before any mutation; remove; a removal that throws while the fact is still
        //    present releases the claim and propagates (retried later); once the fact is gone the outcome is recorded in the
        //    session before any reporting, so a failing result write or rename can only be finished as applied.
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
                    return HorseTripRepairDecision.FinishedApplied;
                }
                host.WriteResult("Interrupted: a claimed request for horse " + (claimedId ?? "<unreadable>") + " did not finish in this game " +
                    "session, so KMC made no further change. Check that horse in game (a bite hit followed by a Trip means the fact is still " +
                    "there) and write a new request if you still want it removed.");
                host.Close(true, InterruptedSuffix);
                return HorseTripRepairDecision.Interrupted;
            }
            string text;
            if (!host.TryReadRequest(out text)) return HorseTripRepairDecision.NoRequest;
            var horseId = ParseRequest(text);
            var loaded = false;
            IList<TFact> candidates = null;
            if (horseId != null && gateAdmitted) candidates = host.CandidatesOf(horseId, out loaded);
            var decision = Decide(true, horseId, gateAdmitted, loaded, candidates == null ? 0 : candidates.Count);
            switch (decision)
            {
                case HorseTripRepairDecision.Apply:
                    var fact = candidates[0];
                    host.Claim();
                    try { host.Remove(fact); }
                    catch (Exception)
                    {
                        if (host.StillPresent(fact))
                        {
                            host.ReleaseClaim();
                            throw;
                        }
                    }
                    var result = AppliedResult(horseId);
                    session.MarkApplied(horseId, result);
                    host.WriteResult(result);
                    host.Close(true, AppliedSuffix);
                    session.Complete(horseId);
                    break;
                case HorseTripRepairDecision.Malformed:
                    host.WriteResult("Refused: the request must contain exactly one line of the form horse=<UniqueId> (copy it from the " +
                        "KMC log line 'KMC Horse Trip repair candidate'); blank lines and lines starting with # are ignored. Nothing was changed.");
                    host.Close(false, RefusedSuffix);
                    break;
                case HorseTripRepairDecision.NoCandidate:
                    host.WriteResult("Refused: horse " + horseId + " has no sourceless stock TrippingBite to remove. Nothing was changed.");
                    host.Close(false, RefusedSuffix);
                    break;
                case HorseTripRepairDecision.Ambiguous:
                    host.WriteResult("Refused: horse " + horseId + " has " + candidates.Count +
                        " sourceless stock TrippingBite facts, so the inherited one cannot be singled out. Nothing was changed.");
                    host.Close(false, RefusedSuffix);
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
        void ReleaseClaim();
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

        public void Claim() => File.Move(RequestPath, ClaimPath);

        public void ReleaseClaim()
        {
            if (File.Exists(RequestPath)) throw new IOException("A newer request exists; the claim is kept.");
            File.Move(ClaimPath, RequestPath);
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
