using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.UnitLogic;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Integration
{
    // MMR-06 (docs/missions/mounted-alpha2-melee-pose/MISSION.md). The KMC Horse takes its companion mechanics from the
    // stock Mammoth, including the Mammoth's AddFacts. In the installed 2.1.7b data that array is exactly
    // [TrippingBite, NaturalArmor7, TripDefenseFourLegs]: the stock Dog and Wolf carry the same TrippingBite
    // (ManeuverOnAttack: Trip on a bite hit). It never fires for a Mammoth, which has no bite, but the KMC Horse bites
    // ([Bite1d4, Hoof1d4, Hoof1d4]), so every horse bite hit could trip. A Pathfinder horse companion has no such trigger.
    // Only that exact stock grant is withheld from a newly created Horse (review R1-F1): any other fact, including another
    // mod's own attack-maneuver fact, is inherited unchanged; the stock Mammoth, Dog, Wolf, the KMC Riding Dog, the shared
    // bite and the Horse's body limbs are untouched.
    // A Horse created earlier keeps its sourceless copy until its owner asks for this exact repair (review R3 R1-F1): the
    // saved state cannot tell the inherited copy from one granted another way (HorseTripRepairPolicy), so KMC reports each
    // candidate and removes one fact from one horse only on the owner's written request.
    internal sealed partial class HorseCompanionBlueprintService
    {
        // Stock TrippingBite (installed 2.1.7b sharedassets1; scripts/Test-MountedAlpha2NativeContract.ps1 pins it).
        internal const string StockTrippingBiteGuid = "f957b4444b6fb404e84ae2a5765797bb";

        // Candidates are reported once per loaded unit object, never per stable ID (review R1-F2): a reload creates a new
        // object with the same UniqueId, which is examined again.
        private readonly LoadedInstanceRepairLedger<UnitEntityData> tripCandidateReportedHorses =
            new LoadedInstanceRepairLedger<UnitEntityData>();
        private readonly List<UnitEntityData> loadedHorsesThisUpdate = new List<UnitEntityData>();
        private readonly Dictionary<string, string> reportedTripCandidates = new Dictionary<string, string>(StringComparer.Ordinal);
        private float nextTripRepairRequestCheck;
        private string lastTripRepairNotice;
        private bool tripRepairFailureReported;
        private bool inheritedFactRepairFailureReported;

        // Set by the composition root: its update-time write gate (MountedWorldWriteGate). Unset means not allowed.
        internal Func<bool> InheritedFactRepairAllowed { get; set; }

        // Set by the composition root: the mod's own folder (UMM ModEntry.Path), where the candidate report, the owner's
        // request and the result are kept. Unset means report to the log only and read no request.
        internal Func<string> RepairDirectory { get; set; }

        internal int TripRepairAppliedCount { get; private set; }

        // The exact stock grant: its installed identity and its installed shape, a ManeuverOnAttack that trips on a bite
        // (review R2 R1-F1). Reference comparisons only, so a genuine blueprint is judged the same inside and outside Unity.
        internal static bool IsStockTrippingBite(BlueprintUnitFact fact) =>
            !ReferenceEquals(fact, null) && string.Equals(fact.AssetGuid, StockTrippingBiteGuid, StringComparison.Ordinal) &&
            fact.ComponentsArray != null && fact.ComponentsArray.Any(component => component is ManeuverOnAttack maneuver &&
                maneuver.Category == Kingmaker.Enums.WeaponCategory.Bite && maneuver.Maneuver == Kingmaker.RuleSystem.Rules.CombatManeuver.Trip);

        // The Horse's AddFacts: the donor's facts in order, without the stock TrippingBite.
        internal static BlueprintUnitFact[] HorseInheritedFacts(BlueprintUnitFact[] donorFacts) =>
            CloneArray(donorFacts).Where(fact => !IsStockTrippingBite(fact)).ToArray();

        // A repair candidate on a unit of this blueprint: the exact KMC Horse, the exact stock grant, and no recorded
        // provenance (source fact, item or cutscene). Position and neighbours are deliberately not consulted.
        internal static bool IsTripRepairCandidate(string unitBlueprintGuid, Feature feature) =>
            !ReferenceEquals(feature, null) && HorseTripRepairPolicy.IsCandidate(
                string.Equals(unitBlueprintGuid, UnitGuid, StringComparison.Ordinal),
                string.Equals((feature.Blueprint as BlueprintUnitFact)?.AssetGuid, StockTrippingBiteGuid, StringComparison.Ordinal),
                IsStockTrippingBite(feature.Blueprint as BlueprintUnitFact),
                ReferenceEquals(feature.Source, null) && ReferenceEquals(feature.SourceItem, null) && ReferenceEquals(feature.SourceCutscene, null));

        private static List<Feature> TripRepairCandidates(UnitEntityData horse)
        {
            var features = horse?.Descriptor?.Progression?.Features;
            return features == null ? new List<Feature>() :
                features.Enumerable.Where(feature => IsTripRepairCandidate(horse.Blueprint?.AssetGuid, feature)).ToList();
        }

        // A settled world only: the service is live, no native load is in progress or queued, and the composition root's
        // write gate admits writes (no save suspension, area transition, restoration, save worker or owned load).
        private bool TripRepairAdmitted()
        {
            var loading = LoadingProcess.Instance != null && (LoadingProcess.Instance.IsLoadingInProcess || LoadingProcess.Instance.QueuedNames.Any());
            return HorseInheritedFactRepairPolicy.MayRepair(!disposed, !loading, !(InheritedFactRepairAllowed?.Invoke() ?? false));
        }

        // Each party pet this update: a loaded exact KMC Horse is remembered for the request, and its candidates are reported
        // once per loaded object on a settled world. Nothing is removed here.
        private void ObserveInheritedTripCandidates(UnitEntityData pet)
        {
            if (pet == null || !string.Equals(pet.Blueprint?.AssetGuid, UnitGuid, StringComparison.Ordinal)) { return; }
            loadedHorsesThisUpdate.Add(pet);
            if (tripCandidateReportedHorses.IsCompleted(pet)) { return; }
            int count;
            tripCandidateReportedHorses.TryRun(pet, TripRepairAdmitted() && pet.Descriptor?.Progression?.Features != null, unit =>
            {
                var candidates = TripRepairCandidates(unit).Count;
                if (candidates > 0) ReportTripCandidate(unit, candidates);
                else if (reportedTripCandidates.Remove(unit.UniqueId)) WriteTripCandidateReport();
                return candidates;
            }, out count);
        }

        private void ReportTripCandidate(UnitEntityData horse, int candidates)
        {
            var directory = RepairDirectory?.Invoke();
            var request = HorseTripRepairPolicy.FormatRequestLine(horse.UniqueId);
            logger.Info("KMC Horse Trip repair candidate (MMR-06; not removed automatically): horse=" + horse.UniqueId +
                "; name=" + horse.CharacterName + "; fact=TrippingBite " + StockTrippingBiteGuid + " (sourceless, Bite->Trip); count=" + candidates +
                ". To remove it, write the line '" + request + "' into " +
                Path.Combine(directory ?? "<mod folder>", HorseTripRepairPolicy.RequestFileName) + " (see " +
                HorseTripRepairPolicy.CandidateReportFileName + ").");
            reportedTripCandidates[horse.UniqueId] = "Horse: " + horse.CharacterName + " (UniqueId " + horse.UniqueId + ")\r\n" +
                "  Candidate: TrippingBite " + StockTrippingBiteGuid + " with no source, item or cutscene (" + candidates + " found)." +
                (candidates == 1 ? string.Empty : " More than one: the repair will refuse, because the inherited copy cannot be singled out.") + "\r\n" +
                "  Request line: " + request + "\r\n";
            WriteTripCandidateReport();
        }

        private void WriteTripCandidateReport()
        {
            var directory = RepairDirectory?.Invoke();
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory)) { return; }
            var text = new StringBuilder();
            text.Append("Kingmaker Mounted Combat: Horse Trip repair candidates (MMR-06), written ")
                .Append(DateTime.UtcNow.ToString("u", CultureInfo.InvariantCulture)).Append("\r\n\r\n");
            text.Append("KMC 0.2.16-alpha.1 and earlier gave every KMC Horse the stock Mammoth's TrippingBite, so a horse bite hit could\r\n" +
                "trip. New horses no longer get it. An existing horse keeps it until you ask for this repair, because a TrippingBite\r\n" +
                "granted another way (a console command, another mod) looks identical in the save.\r\n\r\n" +
                "What the repair changes: exactly one fact, that TrippingBite, is removed from that one horse. Its bite and hoof attacks\r\n" +
                "and their damage stay. Wolves, dogs, Riding Dogs, other horses and every other fact are untouched.\r\n" +
                "If you granted TrippingBite to this horse deliberately, do not apply it.\r\n\r\n" +
                "To apply: back up your save, load it, then create the file " + HorseTripRepairPolicy.RequestFileName + " in this folder\r\n" +
                "containing the request line below, while the game runs with that horse in the party. Within a few seconds KMC removes the\r\n" +
                "fact, renames the request to " + HorseTripRepairPolicy.RequestFileName + HorseTripRepairPolicy.AppliedSuffix +
                " and writes " + HorseTripRepairPolicy.ResultFileName + ".\r\n" +
                "The change reaches your save file only when the game next saves; to undo it, reload the save you made before.\r\n\r\n");
            if (reportedTripCandidates.Count == 0) text.Append("No outstanding candidates in the current session.\r\n");
            foreach (var entry in reportedTripCandidates.Values) text.Append(entry).Append("\r\n");
            File.WriteAllText(Path.Combine(directory, HorseTripRepairPolicy.CandidateReportFileName), text.ToString());
        }

        // The owner's request, read at most every two seconds; a failure leaves the request in place and is retried.
        private void ProcessHorseTripRepairRequest()
        {
            var now = UnityEngine.Time.realtimeSinceStartup;
            if (now < nextTripRepairRequestCheck) { return; }
            nextTripRepairRequestCheck = now + 2f;
            var directory = RepairDirectory?.Invoke();
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory)) { return; }
            HorseTripRepairDecision decision;
            var host = new TripRepairFileHost(this, directory);
            try { decision = HorseTripRepairPolicy.Process(host, TripRepairAdmitted()); }
            catch (Exception exception)
            {
                if (!tripRepairFailureReported) logger.Exception("KMC Horse Trip repair request (kept; retried later)", exception);
                tripRepairFailureReported = true;
                return;
            }
            tripRepairFailureReported = false;
            if (decision == HorseTripRepairDecision.NoRequest || decision == HorseTripRepairDecision.Pending) { return; }
            var notice = decision + ":" + host.RequestText;
            if (decision != HorseTripRepairDecision.Apply && string.Equals(notice, lastTripRepairNotice, StringComparison.Ordinal)) { return; }
            lastTripRepairNotice = notice;
            logger.Info("KMC Horse Trip repair request: decision=" + decision +
                (decision == HorseTripRepairDecision.HorseNotLoaded ? " (request kept until that horse is loaded in the party)" : string.Empty) +
                "; result=" + (host.Result ?? "<kept>") + ".");
        }

        private sealed class TripRepairFileHost : IHorseTripRepairHost<Feature>
        {
            private readonly HorseCompanionBlueprintService owner;
            private readonly string requestPath;
            private readonly string directory;
            private UnitEntityData horse;

            internal TripRepairFileHost(HorseCompanionBlueprintService owner, string directory)
            {
                this.owner = owner;
                this.directory = directory;
                requestPath = Path.Combine(directory, HorseTripRepairPolicy.RequestFileName);
            }

            internal string RequestText { get; private set; }

            internal string Result { get; private set; }

            public bool TryReadRequest(out string text)
            {
                text = File.Exists(requestPath) ? File.ReadAllText(requestPath) : null;
                RequestText = text;
                return text != null;
            }

            public IList<Feature> CandidatesOf(string horseId, out bool loaded)
            {
                horse = owner.loadedHorsesThisUpdate.FirstOrDefault(unit => string.Equals(unit.UniqueId, horseId, StringComparison.Ordinal));
                loaded = horse?.Descriptor?.Progression?.Features != null;
                return loaded ? TripRepairCandidates(horse) : new List<Feature>();
            }

            // Removes exactly this fact instance and proves it: one candidate fewer and one fact fewer, or the request stays.
            public void Remove(Feature fact)
            {
                var features = horse.Descriptor.Progression.Features;
                var factsBefore = features.Enumerable.Count();
                var before = TripRepairCandidates(horse).Count;
                features.RemoveFact(fact);
                var after = TripRepairCandidates(horse).Count;
                var factsAfter = features.Enumerable.Count();
                if (after != before - 1 || factsAfter != factsBefore - 1 || features.Enumerable.Contains(fact))
                    throw new InvalidOperationException("The requested TrippingBite was not removed exactly (candidates " + before + "->" + after +
                        ", features " + factsBefore + "->" + factsAfter + ").");
                owner.TripRepairAppliedCount++;
                owner.logger.Info("Removed the inherited stock TrippingBite from the KMC Horse at the owner's request (MMR-06): horse=" +
                    horse.UniqueId + "; name=" + horse.CharacterName + "; candidates " + before + "->" + after + "; features " +
                    factsBefore + "->" + factsAfter + ".");
                if (owner.reportedTripCandidates.Remove(horse.UniqueId)) owner.WriteTripCandidateReport();
            }

            public void Consume(string suffix, string result)
            {
                var target = requestPath + suffix;
                if (File.Exists(target))
                    target = requestPath + "." + DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture) + suffix;
                File.Move(requestPath, target);
                Result = result;
                File.WriteAllText(Path.Combine(directory, HorseTripRepairPolicy.ResultFileName),
                    DateTime.UtcNow.ToString("u", CultureInfo.InvariantCulture) + "\r\n" + result + "\r\n");
            }
        }
    }
}
