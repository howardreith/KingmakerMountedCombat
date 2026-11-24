using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints.Classes;
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
    // Only that exact stock grant is withheld from the Horse (review R1-F1): any other fact, including another mod's own
    // attack-maneuver fact, is inherited unchanged; the stock Mammoth, Dog, Wolf, the KMC Riding Dog, the shared bite and
    // the Horse's body limbs are untouched.
    internal sealed partial class HorseCompanionBlueprintService
    {
        // Stock TrippingBite (installed 2.1.7b sharedassets1; scripts/Test-MountedAlpha2NativeContract.ps1 pins it).
        internal const string StockTrippingBiteGuid = "f957b4444b6fb404e84ae2a5765797bb";

        // Completion is recorded per loaded unit object, never per stable ID (review R1-F2): a reload creates a new object
        // with the same UniqueId, which is examined again.
        private readonly LoadedInstanceRepairLedger<UnitEntityData> inheritedFactRepairedHorses =
            new LoadedInstanceRepairLedger<UnitEntityData>();

        // Set by the composition root: its update-time write gate (MountedWorldWriteGate). Unset means not allowed.
        internal Func<bool> InheritedFactRepairAllowed { get; set; }

        // The Horse feature that followed the stock TrippingBite in the donor's AddFacts when this Horse was registered: a
        // horse created with the inherited grant holds it immediately before that feature (native Initialize adds the
        // unit's AddFacts in order, before any class or level feature). Null when the donor has none: nothing is provable.
        private BlueprintFeature inheritedTrippingBiteSuccessor;

        internal int InheritedFactRepairCount { get; private set; }

        private bool inheritedFactRepairFailureReported;

        // The exact stock grant: its installed identity and its installed shape, a ManeuverOnAttack that trips on a bite
        // (review R2 R1-F1). Reference comparisons only, so a genuine blueprint is judged the same inside and outside Unity.
        internal static bool IsStockTrippingBite(BlueprintUnitFact fact) =>
            !ReferenceEquals(fact, null) && string.Equals(fact.AssetGuid, StockTrippingBiteGuid, StringComparison.Ordinal) &&
            fact.ComponentsArray != null && fact.ComponentsArray.Any(component => component is ManeuverOnAttack maneuver &&
                maneuver.Category == Kingmaker.Enums.WeaponCategory.Bite && maneuver.Maneuver == Kingmaker.RuleSystem.Rules.CombatManeuver.Trip);

        // The donor feature that followed the stock grant (see inheritedTrippingBiteSuccessor).
        internal static BlueprintFeature DonorSuccessorFeature(BlueprintUnitFact[] donorFacts)
        {
            var facts = CloneArray(donorFacts);
            var index = Array.FindIndex(facts, IsStockTrippingBite);
            return index < 0 ? null : facts.Skip(index + 1).OfType<BlueprintFeature>().FirstOrDefault();
        }

        // The Horse's AddFacts: the donor's facts in order, without the stock TrippingBite.
        internal static BlueprintUnitFact[] HorseInheritedFacts(BlueprintUnitFact[] donorFacts) =>
            CloneArray(donorFacts).Where(fact => !IsStockTrippingBite(fact)).ToArray();

        // A horse created before this repair kept the stock TrippingBite as a Feature its unit blueprint added at creation:
        // native UnitDescriptor.Initialize adds each BlueprintUnit.AddFacts entry through UnitHelper.AddFact with no source,
        // item or cutscene. Only that attributable grant is stale: the same blueprint granted by a class, race, progression,
        // selection, item or cutscene records its provenance and is kept, as is every other fact.
        // Attributable (review R2 R1-F1): the sourceless stock copy must sit immediately before the sourceless donor successor
        // it was created with. A copy granted directly later is appended after the horse's class and level features and is
        // kept; with no provable successor nothing is removed.
        internal static List<Feature> StaleInheritedAttackManeuvers(IEnumerable<Feature> features, BlueprintFeature donorSuccessor)
        {
            var list = (features ?? Enumerable.Empty<Feature>()).ToList();
            var stale = new List<Feature>();
            if (ReferenceEquals(donorSuccessor, null)) return stale;
            for (var i = 0; i + 1 < list.Count; i++)
            {
                var feature = list[i]; var next = list[i + 1];
                if (!ReferenceEquals(feature, null) && IsStockTrippingBite(feature.Blueprint as BlueprintUnitFact) && Sourceless(feature) &&
                    !ReferenceEquals(next, null) && ReferenceEquals(next.Blueprint, donorSuccessor) && Sourceless(next))
                    stale.Add(feature);
            }
            return stale;
        }

        private static bool Sourceless(Feature feature) => ReferenceEquals(feature.Source, null) &&
            ReferenceEquals(feature.SourceItem, null) && ReferenceEquals(feature.SourceCutscene, null);

        // Once per loaded exact KMC Horse object, only on a settled world (no load or queued load, no save worker, no pending
        // restoration); success is recorded only after every removal completed, so a failed pass is retried on that object.
        private void RepairInheritedHorseFacts(UnitEntityData pet)
        {
            if (pet == null || !string.Equals(pet.Blueprint?.AssetGuid, UnitGuid, StringComparison.Ordinal)) { return; }
            if (inheritedFactRepairedHorses.IsCompleted(pet)) { return; }
            var loading = LoadingProcess.Instance != null && (LoadingProcess.Instance.IsLoadingInProcess || LoadingProcess.Instance.QueuedNames.Any());
            var admitted = HorseInheritedFactRepairPolicy.MayRepair(!disposed, !loading, !(InheritedFactRepairAllowed?.Invoke() ?? false)) &&
                pet.Descriptor?.Progression?.Features != null;
            int removed;
            if (!inheritedFactRepairedHorses.TryRun(pet, admitted, unit =>
                {
                    var features = unit.Descriptor.Progression.Features;
                    var stale = StaleInheritedAttackManeuvers(features.Enumerable, inheritedTrippingBiteSuccessor);
                    foreach (var feature in stale) { features.RemoveFact(feature); }
                    return stale.Count;
                }, out removed)) { return; }
            if (removed > 0)
            {
                InheritedFactRepairCount++;
                logger.Info("Removed the inherited stock TrippingBite from the KMC Horse (MMR-06): pet=" + pet.UniqueId +
                            "; removed=" + removed + ".");
            }
        }
    }
}
