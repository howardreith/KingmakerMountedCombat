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
        // Requests whose removal happened in this game process, until their reporting finished (review R4-F5).
        private readonly HorseTripRepairSession tripRepairSession = new HorseTripRepairSession();
        private float nextTripRepairRequestCheck;
        private string lastTripRepairNotice;
        private bool tripRepairFailureReported;
        private bool inheritedFactRepairFailureReported;

        // Set by the composition root: its update-time write gate (MountedWorldWriteGate). Unset means not allowed.
        internal Func<bool> InheritedFactRepairAllowed { get; set; }

        // Set by the composition root: the KMC user-data folder (<persistence data parent>/KingmakerMountedCombat, review R4-F6),
        // never the Mods tree. Only the owner's request and its outcome live there; candidates are reported in the log. KMC
        // never creates the folder. Unset or missing means no request is read.
        internal Func<string> RepairDirectory { get; set; }

        internal int TripRepairAppliedCount { get; private set; }

        // The exact stock grant: its installed identity and its installed shape, a ManeuverOnAttack that trips on a bite
        // (review R2 R1-F1). Reference comparisons only, so a genuine blueprint is judged the same inside and outside Unity.
        internal static bool IsStockTrippingBite(BlueprintUnitFact fact) =>
            !ReferenceEquals(fact, null) && string.Equals(fact.AssetGuid, StockTrippingBiteGuid, StringComparison.Ordinal) &&
            // PR-4 review R1-F3: exactly the verified stock component list (one ManeuverOnAttack Bite->Trip); a feature carrying any
            // further effect is preserved whole, since its effects cannot be separated safely.
            fact.ComponentsArray != null && fact.ComponentsArray.Length == 1 && fact.ComponentsArray[0] is ManeuverOnAttack maneuver &&
                maneuver.Category == Kingmaker.Enums.WeaponCategory.Bite && maneuver.Maneuver == Kingmaker.RuleSystem.Rules.CombatManeuver.Trip;

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
                return candidates;
            }, out count);
        }

        // Log only (review R4-F6): nothing is written on discovery.
        private void ReportTripCandidate(UnitEntityData horse, int candidates)
        {
            var directory = RepairDirectory?.Invoke();
            var request = HorseTripRepairPolicy.FormatRequestLine(horse.UniqueId);
            logger.Info("KMC Horse Trip repair candidate (MMR-06; not removed automatically): horse=" + horse.UniqueId +
                "; name=" + horse.CharacterName + "; fact=TrippingBite " + StockTrippingBiteGuid + " (sourceless, Bite->Trip); count=" + candidates +
                (candidates == 1 ? string.Empty : " (more than one: a request will be refused, the inherited copy cannot be singled out)") +
                ". KMC 0.2.16-alpha.1 and earlier granted exactly this fact; if you did not grant it yourself and want it removed, back up " +
                "your save and write the single line '" + request + "' into " +
                Path.Combine(directory ?? "<user data>/" + HorseTripRepairPolicy.FolderName, HorseTripRepairPolicy.RequestFileName) +
                " (create the folder if needed). Only that fact on that horse is removed; bite and hoof attacks stay.");
        }

        // The owner's request, read at most every two seconds; a failure leaves the request in place and is retried.
        private void ProcessHorseTripRepairRequest()
        {
            var now = UnityEngine.Time.realtimeSinceStartup;
            if (now < nextTripRepairRequestCheck) { return; }
            nextTripRepairRequestCheck = now + 2f;
            var directory = RepairDirectory?.Invoke();
            HorseTripRepairDecision decision;
            TripRepairFileHost host = null;
            try
            {
                decision = HorseTripRepairPolicy.ProcessFolder(directory, files => host = new TripRepairFileHost(this, files),
                    TripRepairAdmitted(), tripRepairSession);
            }
            catch (Exception exception)
            {
                if (!tripRepairFailureReported) logger.Exception("KMC Horse Trip repair request (kept; retried later)", exception);
                tripRepairFailureReported = true;
                return;
            }
            tripRepairFailureReported = false;
            if (host == null || decision == HorseTripRepairDecision.NoRequest || decision == HorseTripRepairDecision.Pending) { return; }
            var notice = decision + ":" + host.RequestText;
            if (decision != HorseTripRepairDecision.Apply && string.Equals(notice, lastTripRepairNotice, StringComparison.Ordinal)) { return; }
            lastTripRepairNotice = notice;
            logger.Info("KMC Horse Trip repair request: decision=" + decision + "; folder=" + directory +
                (decision == HorseTripRepairDecision.HorseNotLoaded ? " (request kept until that horse is loaded in the party)" : string.Empty) +
                "; result=" + (host.Result ?? "<kept>") + ".");
        }

        // The policy's host: the file lifecycle (HorseTripRepairFiles, user-data folder) and the loaded horse's FeatureCollection.
        private sealed class TripRepairFileHost : IHorseTripRepairHost<Feature>
        {
            private readonly HorseCompanionBlueprintService owner;
            private readonly HorseTripRepairFiles files;
            private UnitEntityData horse;

            internal TripRepairFileHost(HorseCompanionBlueprintService owner, HorseTripRepairFiles files)
            {
                this.owner = owner;
                this.files = files;
            }

            internal string RequestText { get; private set; }

            internal string Result { get; private set; }

            public bool TryReadRequest(out string text)
            {
                var present = files.TryReadRequest(out text);
                RequestText = text;
                return present;
            }

            public bool TryReadClaim(out string text, out string identity)
            {
                var present = files.TryReadClaim(out text, out identity);
                if (present) RequestText = text;
                return present;
            }

            public IList<Feature> CandidatesOf(string horseId, out bool loaded)
            {
                horse = owner.loadedHorsesThisUpdate.FirstOrDefault(unit => string.Equals(unit.UniqueId, horseId, StringComparison.Ordinal));
                loaded = horse?.Descriptor?.Progression?.Features != null;
                return loaded ? TripRepairCandidates(horse) : new List<Feature>();
            }

            // PR-4 reviews R1-F2/R2-F1: the live collection (an unavailable one throws: it is not proof of absence).
            private IEnumerable<Feature> ReadFeatures()
            {
                var features = horse?.Descriptor?.Progression?.Features;
                if (features == null) throw new InvalidOperationException("The horse's feature collection is unavailable.");
                return features.Enumerable;
            }

            // The native boundary alone; every check is HorseTripRemoval's classification after it returns or throws.
            private void NativeRemove(Feature fact) => horse.Descriptor.Progression.Features.RemoveFact(fact);

            // Removes this one instance and classifies the result; an application is counted only for an exact removal.
            // PR-4 review R3-F1: returns the classification with no fallible diagnostic in between; the report is separate.
            public HorseTripRemovalResult RemoveExactly(Feature fact)
            {
                var removal = HorseTripRemoval.RemoveExactly(fact, ReadFeatures, NativeRemove);
                if (removal.Outcome == HorseTripRemovalOutcome.Removed) owner.TripRepairAppliedCount++;
                return removal;
            }

            // Called by the policy after the classified outcome returned, inside its containment (a display-name read or logger fault
            // cannot change the decision).
            public void ReportRemoval(string horseId, HorseTripRemovalResult removal) =>
                owner.logger.Info("KMC Horse Trip repair removal (MMR-06): horse=" + horse?.UniqueId + "; name=" + horse?.CharacterName +
                    "; outcome=" + removal.Outcome + "; " + removal.Detail + ".");

            public void Claim() => files.Claim();

            public bool ReleaseClaim() => files.ReleaseClaim();

            public void WriteResult(string result)
            {
                Result = result;
                files.WriteResult(result);
            }

            public bool Close(bool claimed, string suffix, string boundIdentity) => files.Close(claimed, suffix, boundIdentity);
        }
    }
}
