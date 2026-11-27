using System;
using System.Collections.Generic;
using System.Globalization;
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
    // candidate and removes one fact from one horse only through the owner's confirmed in-game maintenance action (PR-4 round 12,
    // OWNER AMENDMENT revision 1: the Unity Mod Manager window's Horse Trip repair section; the earlier file request is withdrawn).
    internal sealed partial class HorseCompanionBlueprintService
    {
        // Stock TrippingBite (installed 2.1.7b sharedassets1; scripts/Test-MountedAlpha2NativeContract.ps1 pins it).
        internal const string StockTrippingBiteGuid = "f957b4444b6fb404e84ae2a5765797bb";

        // Candidates are reported once per loaded unit object, never per stable ID (review R1-F2): a reload creates a new
        // object with the same UniqueId, which is examined again.
        private readonly LoadedInstanceRepairLedger<UnitEntityData> tripCandidateReportedHorses =
            new LoadedInstanceRepairLedger<UnitEntityData>();
        private readonly List<UnitEntityData> loadedHorsesThisUpdate = new List<UnitEntityData>();
        // PR-4 round 12: the confirmed maintenance action's consent and records (Domain.HorseTripRepairConsole). The UI only reads its
        // snapshots and records intent; Observe/Execute run here, in the ordinary update.
        private readonly HorseTripRepairConsole<UnitEntityData, Feature> tripRepair = new HorseTripRepairConsole<UnitEntityData, Feature>();
        private object tripRepairSessionMarker, tripRepairSessionPlayer, tripRepairSessionArea;
        private bool tripRepairFailureReported;
        private bool inheritedFactRepairFailureReported;

        // Set by the composition root: its update-time write gate (MountedWorldWriteGate). Unset means not allowed.
        internal Func<bool> InheritedFactRepairAllowed { get; set; }

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

        // Log only: nothing is written or removed on discovery.
        private void ReportTripCandidate(UnitEntityData horse, int candidates)
        {
            logger.Info("KMC Horse Trip repair candidate (MMR-06; not removed automatically): horse=" + horse.UniqueId +
                "; name=" + horse.CharacterName + "; fact=TrippingBite " + StockTrippingBiteGuid + " (sourceless, Bite->Trip); count=" + candidates +
                (candidates == 1 ? string.Empty : " (more than one: the inherited copy cannot be singled out, so no repair is offered)") +
                ". KMC 0.2.16-alpha.1 and earlier granted exactly this fact; if you did not grant it yourself, back up your save and use the " +
                "Horse Trip repair section of Kingmaker Mounted Combat in the Unity Mod Manager window (Ctrl+F10) to remove exactly that fact " +
                "from that horse. Bite and hoof attacks stay.");
        }

        // The load/session context a consent is bound to: a new Player object (every load) or a new loaded area STATE (every area load,
        // also a new visit to the same area blueprint; PR-4 round 13, R12-F2) is a new marker.
        private object TripRepairSessionMarker()
        {
            var player = Kingmaker.Game.Instance?.Player;
            var area = Kingmaker.Game.Instance?.State?.LoadedAreaState;
            if (tripRepairSessionMarker == null || !ReferenceEquals(player, tripRepairSessionPlayer) || !ReferenceEquals(area, tripRepairSessionArea))
            {
                tripRepairSessionPlayer = player; tripRepairSessionArea = area; tripRepairSessionMarker = new object();
            }
            return tripRepairSessionMarker;
        }

        // PR-4 round 12: the update-path half of the confirmed maintenance action, against the horses loaded in this enumeration. Stable
        // snapshots go to the UI; an armed confirmation is executed once after the session, the world-write gate, the live object and its
        // exact single candidate are rechecked. The record is stored by the console before it is logged here, so a logging failure cannot
        // repeat the removal. Old request/result files in the KMC user-data folder are never read.
        // PR-4 round 13 (R12-F7): the production orchestration is HorseTripRepairUpdater.Run, shared with the tests; this host supplies its
        // game-facing calls. A missing party (a world discard or unload) is reported as unavailable, which drops every offer and consent.
        private void ProcessTripRepairConsole(bool partyAvailable = true)
        {
            try
            {
                HorseTripRepairUpdater.Run(tripRepair, new TripRepairHost(this, partyAvailable));
                tripRepairFailureReported = false;
            }
            catch (Exception exception)
            {
                if (!tripRepairFailureReported) logger.Exception("KMC Horse Trip repair action (the recorded outcome is kept; nothing is repeated)", exception);
                tripRepairFailureReported = true;
            }
            if (!partyAvailable) { tripRepairSessionMarker = null; tripRepairSessionPlayer = null; tripRepairSessionArea = null; }
        }

        private sealed class TripRepairHost : IHorseTripRepairHost<UnitEntityData, Feature>
        {
            private readonly HorseCompanionBlueprintService owner;
            internal TripRepairHost(HorseCompanionBlueprintService owner, bool partyAvailable) { this.owner = owner; PartyAvailable = partyAvailable; }
            public bool PartyAvailable { get; }
            public object SessionMarker() => owner.TripRepairSessionMarker();
            public IEnumerable<HorseTripRepairConsole<UnitEntityData, Feature>.Live> LoadedHorses() => owner.loadedHorsesThisUpdate.Select(horse =>
                new HorseTripRepairConsole<UnitEntityData, Feature>.Live
                {
                    Unit = horse, Id = horse.UniqueId, Name = horse.CharacterName, Owner = horse.Descriptor?.Master.Value?.CharacterName,
                    Candidates = horse.Descriptor?.Progression?.Features == null ? new List<Feature>() : TripRepairCandidates(horse)
                }).ToList();
            public bool GateAdmits() => owner.TripRepairAdmitted();
            public bool Loaded(UnitEntityData unit) => owner.loadedHorsesThisUpdate.Contains(unit) && unit.Descriptor?.Progression?.Features != null;
            public string IdOf(UnitEntityData unit) => unit?.UniqueId;
            public IList<Feature> CandidatesOf(UnitEntityData unit) => TripRepairCandidates(unit);
            public HorseTripRemovalResult Remove(UnitEntityData unit, Feature fact) => owner.RemoveTripFact(unit, fact);
            public void Report(HorseTripRepairRecord record) => owner.logger.Info("KMC Horse Trip repair (MMR-06, confirmed in-game action): " + record + ".");
        }

        // PR-4 reviews R1-F2/R2-F1/R3-F1: removes this one instance and classifies the result; an application is counted only for an exact
        // removal; no diagnostic sits between the native call and the classification.
        private HorseTripRemovalResult RemoveTripFact(UnitEntityData horse, Feature fact)
        {
            var removal = HorseTripRemoval.RemoveExactly(fact, () => ReadTripFeatures(horse), feature => NativeRemoveTripFact(horse, feature));
            if (removal.Outcome == HorseTripRemovalOutcome.Removed) TripRepairAppliedCount++;
            return removal;
        }

        // The live collection (an unavailable one throws: it is not proof of absence).
        private static IEnumerable<Feature> ReadTripFeatures(UnitEntityData horse)
        {
            var features = horse?.Descriptor?.Progression?.Features;
            if (features == null) throw new InvalidOperationException("The horse's feature collection is unavailable.");
            return features.Enumerable;
        }

        // The native boundary alone (the only fact removal in this service).
        private static void NativeRemoveTripFact(UnitEntityData horse, Feature fact) => horse.Descriptor.Progression.Features.RemoveFact(fact);

        // PR-4 round 12: the maintenance surface (Unity Mod Manager OnGUI). It reads the console's immutable snapshots and records intent
        // only (Select/Confirm/Cancel); it never reads or changes a unit or a fact.
        internal void DrawTripRepairGui()
        {
            tripRepair.RequestPresentation();   // intent: the next update rebuilds the snapshots (none are built while the panel is closed)
            UnityEngine.GUILayout.Label("Horse Trip repair (MMR-06). KMC 0.2.16-alpha.1 and earlier gave the KMC Horse the stock 'Tripping Bite' " +
                "fact (a bite hit can trip). New horses no longer receive it.");
            var offers = tripRepair.Offers;
            if (offers.Count == 0) UnityEngine.GUILayout.Label("No loaded party Horse carries a sourceless stock Tripping Bite fact.");
            foreach (var offer in offers)
            {
                UnityEngine.GUILayout.Label(offer.HorseName + " (" + offer.HorseId + ")" + (string.IsNullOrEmpty(offer.OwnerName) ? string.Empty : ", companion of " + offer.OwnerName) +
                    ": " + offer.CandidateCount + " sourceless Tripping Bite fact" + (offer.CandidateCount == 1 ? "." : "s."));
                if (!offer.Eligible) { UnityEngine.GUILayout.Label(offer.RefusalText); continue; }
                if (tripRepair.SelectedToken != offer.Token)
                {
                    if (UnityEngine.GUILayout.Button("Repair this horse...")) tripRepair.Select(offer.Token);
                    continue;
                }
                UnityEngine.GUILayout.Label("Remove exactly this one 'Tripping Bite' fact from " + offer.HorseName + "? The save cannot prove where a " +
                    "sourceless copy came from: remove it only if you did not grant it yourself. Back up your save first. Bite and hoof attacks, " +
                    "other facts and other companions stay. The repair is applied on the next game update; saving keeps it.");
                UnityEngine.GUILayout.BeginHorizontal();
                if (UnityEngine.GUILayout.Button(tripRepair.Armed ? "Confirmed - applying..." : "Confirm removal")) tripRepair.Confirm(offer.Token);
                if (UnityEngine.GUILayout.Button("Cancel")) tripRepair.Cancel();
                UnityEngine.GUILayout.EndHorizontal();
            }
            var last = tripRepair.LastRecord;
            if (last != null) UnityEngine.GUILayout.Label("Last repair: " + last.HorseName + " - " + last.Disposition + ".");
        }

        // Unloading (Dispose) drops every consent and releases the session references (PR-4 round 13, R12-F1).
        internal void InvalidateTripRepair()
        {
            tripRepair.Invalidate();
            tripRepairSessionMarker = null; tripRepairSessionPlayer = null; tripRepairSessionArea = null;
        }

        // PR-4 round 14 (R12-F1): the composition root's stop transitions (a successful SetEnabled(false) and PerformFailureCleanup) stop the
        // console (nothing offered, no intent accepted, every consent dropped) and the enable transition starts it again.
        internal void OnServicesStopped()
        {
            tripRepair.Stop();
            tripRepairSessionMarker = null; tripRepairSessionPlayer = null; tripRepairSessionArea = null;
        }

        internal void OnServicesStarted() => tripRepair.Start();
    }
}
