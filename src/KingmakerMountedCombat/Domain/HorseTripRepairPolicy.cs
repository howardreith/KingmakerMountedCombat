using System;
using System.Collections.Generic;

namespace KingmakerMountedCombat.Domain
{
    // MMR-06 existing horses (review R1-F1). A horse created by KMC 0.2.16-alpha.1 or earlier received the stock Mammoth's
    // TrippingBite as a sourceless fact. A sourceless TrippingBite granted any other way (a console command, another mod)
    // looks exactly the same in the save: neither its list position, its neighbours, its GUID nor the missing source proves
    // who granted it. So nothing is removed automatically. KMC only reports each candidate (exact KMC Horse, exact stock
    // grant by identity and Bite/Trip shape, no source fact, item or cutscene) with the exact request line, and removes that
    // one fact from that one horse only when the owner has written that request. A request is consumed once applied or
    // refused, so it is never applied again on a later load.
    public enum HorseTripRepairDecision
    {
        NoRequest,
        Malformed,
        Pending,
        HorseNotLoaded,
        NoCandidate,
        Ambiguous,
        Apply
    }

    public static class HorseTripRepairPolicy
    {
        public const string RequestFileName = "horse-trip-repair-request.txt";
        public const string CandidateReportFileName = "horse-trip-repair-candidates.txt";
        public const string ResultFileName = "horse-trip-repair-result.txt";
        public const string AppliedSuffix = ".applied";
        public const string RefusedSuffix = ".refused";
        private const string RequestPrefix = "horse=";

        // A repair candidate: every term must hold. An independently granted copy that records its provenance (a source
        // fact such as a class, race, progression or selection; an item; a cutscene) is never a candidate.
        public static bool IsCandidate(bool exactKmcHorse, bool stockIdentity, bool biteTripShape, bool sourceless) =>
            exactKmcHorse && stockIdentity && biteTripShape && sourceless;

        public static string FormatRequestLine(string horseId) => RequestPrefix + horseId;

        // The first line that is neither blank nor a '#' comment must be exactly "horse=<UniqueId>" (surrounding spaces
        // ignored). Anything else is malformed, so a stray or partial file never selects a horse.
        public static string ParseRequest(string text)
        {
            if (text == null) return null;
            foreach (var raw in text.Split('\n'))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line[0] == '#') continue;
                if (!line.StartsWith(RequestPrefix, StringComparison.Ordinal)) return null;
                var id = line.Substring(RequestPrefix.Length).Trim();
                return id.Length == 0 || id.IndexOfAny(new[] { ' ', '\t', '=' }) >= 0 ? null : id;
            }
            return null;
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

        // The whole request lifecycle against its host. Pending and HorseNotLoaded keep the request for a later update; a
        // removal that throws propagates with the request still in place, so the next settled update retries it. Applied,
        // malformed, no-candidate and ambiguous requests are consumed (renamed) with a written result, never re-read.
        public static HorseTripRepairDecision Process<TFact>(IHorseTripRepairHost<TFact> host, bool gateAdmitted) where TFact : class
        {
            if (host == null) throw new ArgumentNullException(nameof(host));
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
                    host.Remove(fact);
                    host.Consume(AppliedSuffix, "Applied: removed the inherited stock TrippingBite from horse " + horseId +
                        ". Its bite and hoof attacks and damage are unchanged. The change is written to your save the next time the game saves; " +
                        "to undo it, reload a save made before this repair.");
                    break;
                case HorseTripRepairDecision.Malformed:
                    host.Consume(RefusedSuffix, "Refused: the request must contain one line of the form horse=<UniqueId> " +
                        "(copy it from " + CandidateReportFileName + "). Nothing was changed.");
                    break;
                case HorseTripRepairDecision.NoCandidate:
                    host.Consume(RefusedSuffix, "Refused: horse " + horseId + " has no sourceless stock TrippingBite to remove. Nothing was changed.");
                    break;
                case HorseTripRepairDecision.Ambiguous:
                    host.Consume(RefusedSuffix, "Refused: horse " + horseId + " has " + candidates.Count +
                        " sourceless stock TrippingBite facts, so the inherited one cannot be singled out. Nothing was changed.");
                    break;
            }
            return decision;
        }
    }

    public interface IHorseTripRepairHost<TFact> where TFact : class
    {
        bool TryReadRequest(out string text);
        // The exact candidates on the loaded horse with this UniqueId; loaded is false when no such horse is loaded.
        IList<TFact> CandidatesOf(string horseId, out bool loaded);
        void Remove(TFact fact);
        // Renames the request with the suffix and writes the result text.
        void Consume(string suffix, string result);
    }
}
