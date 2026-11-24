using System;
using System.Collections.Generic;
using System.Linq;
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

        // Set by the composition root: true while a save worker can commit or a combat/presentation restoration is pending.
        internal Func<bool> InheritedFactRepairBlocked { get; set; }

        internal int InheritedFactRepairCount { get; private set; }

        private bool inheritedFactRepairFailureReported;

        // The exact stock grant: its installed identity and its bite-Trip trigger (reference comparisons only, so a genuine
        // blueprint is judged the same inside and outside the Unity player).
        internal static bool IsStockTrippingBite(BlueprintUnitFact fact) =>
            !ReferenceEquals(fact, null) && string.Equals(fact.AssetGuid, StockTrippingBiteGuid, StringComparison.Ordinal) &&
            fact.ComponentsArray != null && fact.ComponentsArray.Any(component => component is ManeuverOnAttack);

        // The Horse's AddFacts: the donor's facts in order, without the stock TrippingBite.
        internal static BlueprintUnitFact[] HorseInheritedFacts(BlueprintUnitFact[] donorFacts) =>
            CloneArray(donorFacts).Where(fact => !IsStockTrippingBite(fact)).ToArray();

        // A horse created before this repair kept the stock TrippingBite as a Feature its unit blueprint added at creation:
        // native UnitDescriptor.Initialize adds each BlueprintUnit.AddFacts entry through UnitHelper.AddFact with no source,
        // item or cutscene. Only that attributable grant is stale: the same blueprint granted by a class, race, progression,
        // selection, item or cutscene records its provenance and is kept, as is every other fact.
        internal static List<Feature> StaleInheritedAttackManeuvers(IEnumerable<Feature> features) =>
            (features ?? Enumerable.Empty<Feature>())
                .Where(feature => !ReferenceEquals(feature, null) && IsStockTrippingBite(feature.Blueprint as BlueprintUnitFact) &&
                                  ReferenceEquals(feature.Source, null) && ReferenceEquals(feature.SourceItem, null) &&
                                  ReferenceEquals(feature.SourceCutscene, null))
                .ToList();

        // Once per loaded exact KMC Horse object, only on a settled world (no load or queued load, no save worker, no pending
        // restoration); success is recorded only after every removal completed, so a failed pass is retried on that object.
        private void RepairInheritedHorseFacts(UnitEntityData pet)
        {
            if (pet == null || !string.Equals(pet.Blueprint?.AssetGuid, UnitGuid, StringComparison.Ordinal)) { return; }
            if (inheritedFactRepairedHorses.IsCompleted(pet)) { return; }
            var loading = LoadingProcess.Instance != null && (LoadingProcess.Instance.IsLoadingInProcess || LoadingProcess.Instance.QueuedNames.Any());
            var admitted = HorseInheritedFactRepairPolicy.MayRepair(!disposed, !loading, InheritedFactRepairBlocked?.Invoke() ?? true) &&
                pet.Descriptor?.Progression?.Features != null;
            int removed;
            if (!inheritedFactRepairedHorses.TryRun(pet, admitted, unit =>
                {
                    var features = unit.Descriptor.Progression.Features;
                    var stale = StaleInheritedAttackManeuvers(features.Enumerable);
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
