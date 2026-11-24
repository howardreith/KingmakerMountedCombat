using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;

namespace KingmakerMountedCombat.Integration
{
    // MMR-06 (docs/missions/mounted-alpha2-melee-pose/MISSION.md). The KMC Horse takes its companion mechanics from the
    // stock Mammoth, including the Mammoth's AddFacts. In the installed 2.1.7b data that array is exactly
    // [TrippingBite, NaturalArmor7, TripDefenseFourLegs]: the stock Dog and Wolf carry the same TrippingBite
    // (ManeuverOnAttack: Trip on a bite hit). It never fires for a Mammoth, which has no bite, but the KMC Horse bites
    // ([Bite1d4, Hoof1d4, Hoof1d4]), so every horse bite hit could trip. A Pathfinder horse companion has no such trigger.
    // The Horse alone drops an inherited intrinsic attack-maneuver trigger; the stock Mammoth, Dog, Wolf, the KMC Riding
    // Dog, the shared bite and any independently granted maneuver are untouched.
    internal sealed partial class HorseCompanionBlueprintService
    {
        // Stock TrippingBite (installed 2.1.7b, sharedassets1); recorded for evidence and the contract test, not matched by
        // name: the filter keys on the trigger component, so a mod's equivalent copy is excluded the same way.
        internal const string StockTrippingBiteGuid = "f957b4444b6fb404e84ae2a5765797bb";

        private readonly HashSet<string> inheritedFactRepairedHorses = new HashSet<string>(StringComparer.Ordinal);

        // A fact whose components make an attack hit perform a combat maneuver by itself (ManeuverOnAttack). Reference
        // comparisons only, so a genuine blueprint is judged the same inside and outside the Unity player.
        internal static bool GrantsIntrinsicAttackManeuver(BlueprintUnitFact fact) =>
            !ReferenceEquals(fact, null) && fact.ComponentsArray != null &&
            fact.ComponentsArray.Any(component => component is ManeuverOnAttack);

        // The Horse's AddFacts: the donor's facts in order, without an inherited intrinsic attack-maneuver trigger.
        internal static BlueprintUnitFact[] HorseInheritedFacts(BlueprintUnitFact[] donorFacts) =>
            CloneArray(donorFacts).Where(fact => !GrantsIntrinsicAttackManeuver(fact)).ToArray();

        // A horse created before this repair kept the trigger as a Feature the unit blueprint added at creation: the
        // native UnitDescriptor.Initialize adds each BlueprintUnit.AddFacts entry through UnitHelper.AddFact with no
        // source, so its Feature.Source is null. A Feature granted by a class, race, progression or selection records
        // that source and is kept, as is every other fact. Returns the removed facts.
        internal static List<Feature> StaleInheritedAttackManeuvers(IEnumerable<Feature> features) =>
            (features ?? Enumerable.Empty<Feature>())
                .Where(feature => !ReferenceEquals(feature, null) && ReferenceEquals(feature.Source, null) &&
                                  GrantsIntrinsicAttackManeuver(feature.Blueprint))
                .ToList();

        // Idempotent, once per loaded exact KMC Horse unit (the Riding Dog and other companions are never examined).
        private void RepairInheritedHorseFacts(UnitEntityData pet)
        {
            if (pet == null || !string.Equals(pet.Blueprint?.AssetGuid, UnitGuid, StringComparison.Ordinal)) { return; }
            var features = pet.Descriptor?.Progression?.Features;
            if (features == null || !inheritedFactRepairedHorses.Add(pet.UniqueId)) { return; }
            var stale = StaleInheritedAttackManeuvers(features.Enumerable);
            foreach (var feature in stale) { features.RemoveFact(feature); }
            if (stale.Count > 0)
            {
                logger.Info("Removed an inherited intrinsic attack-maneuver trigger from the KMC Horse (MMR-06): pet=" + pet.UniqueId +
                            "; facts=" + string.Join(",", stale.Select(feature => feature.Blueprint?.name + "/" + feature.Blueprint?.AssetGuid)) + ".");
            }
        }
    }
}
