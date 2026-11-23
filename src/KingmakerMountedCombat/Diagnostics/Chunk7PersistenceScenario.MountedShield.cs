using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Shields;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    // Mounted Shield across a native Manual save and a cold load (MS-18). The source adds Mounted Combat, Shield Focus and
    // Mounted Shield to the first rider as facts and equips a stock heavy shield (setup: the native choice is the Sprint 6
    // shield row's). The native save turns facts off before it serializes, so the contribution is released first and nothing
    // of it is written; after the save, in the same process and after the cold load, the first mount holds exactly one
    // contribution of the current amount, owned by the first rider.
    internal sealed partial class Chunk7PersistenceScenario
    {
        internal const string ShieldCaseName = "mounted-shield-tb-manual";
        private bool ShieldCase => request.PersistenceCase == ShieldCaseName;
        private const string StockHeavyShieldType = "d1b05b901bab9524388ebfa0435902a6";

        private void PrepareShieldBeforeSave()
        {
            var service = MountedShieldService.Current;
            Require(service != null && horses.MountedShieldFeature != null && horses.ShieldFocusFeature != null && horses.MountedCombatFeature != null,
                "Mounted Shield is unavailable: " + horses.MountedShieldFailure);
            Require(Mounted(firstRider, firstMount), "The first pair left its mounted checkpoint before the shield setup.");
            var heavy = ResourcesLibrary.LibraryObject.GetAllBlueprints().OfType<BlueprintItemShield>().Where(shield => shield.ArmorComponent?.Type?.AssetGuid == StockHeavyShieldType &&
                !shield.IsNotable && shield.ArmorComponent.Enchantments.Count == 0 && (shield.WeaponComponent == null || shield.WeaponComponent.Enchantments.Count == 0))
                .OrderBy(shield => shield.name, System.StringComparer.Ordinal).FirstOrDefault();
            Require(heavy != null, "The library lacks a mundane stock heavy shield.");
            var progression = firstRider.Descriptor.Progression.Features;
            if (!progression.HasFact(horses.MountedCombatFeature)) progression.AddFeature(horses.MountedCombatFeature);
            if (!progression.HasFact(horses.ShieldFocusFeature)) progression.AddFeature(horses.ShieldFocusFeature);
            if (!progression.HasFact(horses.MountedShieldFeature)) progression.AddFeature(horses.MountedShieldFeature);
            var equipment = new Sprint6ShieldEquipment(firstRider);
            equipment.Prepare(heavy);
            equipment.Equip(heavy);
            // The write the service makes each frame, made now, so the save starts from the ordinary mounted state.
            service.Update(true);
            var state = ShieldPersistenceState();
            state["equipment"] = equipment.Evidence;
            state["shieldBlueprint"] = heavy.AssetGuid;
            evidence["shieldAtSave"] = state;
            Require(state.Value<int>("ownedCount") == 1, "The first mount does not hold exactly one Mounted Shield contribution before the save.");
        }

        private void ObserveShieldCold() => evidence["shieldColdAtRestore"] = ShieldPersistenceState();

        private void ObserveShieldContinuation() => evidence[Cold ? "shieldColdContinuation" : "shieldWarmContinuation"] = ShieldPersistenceState();

        private JObject ShieldPersistenceState()
        {
            var service = MountedShieldService.Current;
            var owned = MountedShieldService.IdentityModifiers(firstMount.Stats.AC, HorseCompanionBlueprintService.MountedShieldFeatureGuid);
            var otherMountOwned = secondMount == null ? 0 : MountedShieldService.IdentityModifiers(secondMount.Stats.AC, HorseCompanionBlueprintService.MountedShieldFeatureGuid).Length;
            Kingmaker.EntitySystem.Entities.UnitEntityData ledgerMount = null; int ledgerAmount;
            var riderFact = firstRider.Descriptor.Progression.Features.GetFact(horses.MountedShieldFeature);
            var hasLedger = service != null && service.TryGetContribution(firstRider, out ledgerMount, out ledgerAmount);
            var shield = MountedShieldService.EffectiveShield(firstRider);
            return new JObject {
                ["rider"] = firstRider.UniqueId, ["mount"] = firstMount.UniqueId, ["mounted"] = Mounted(firstRider, firstMount),
                ["hasFeat"] = firstRider.Descriptor.Progression.Features.HasFact(horses.MountedShieldFeature),
                ["hasShieldFocus"] = firstRider.Descriptor.Progression.Features.HasFact(horses.ShieldFocusFeature),
                ["shieldBase"] = shield?.ArmorComponent?.Blueprint?.ArmorBonus, ["shieldBlueprint"] = shield?.Blueprint.AssetGuid,
                ["amount"] = service?.Amount(firstRider, firstMount), ["ownedCount"] = owned.Length,
                ["ownedValues"] = new JArray(owned.Select(modifier => modifier.ModValue)),
                ["ownedByRiderFact"] = new JArray(owned.Select(modifier => riderFact != null && ReferenceEquals(modifier.Source, riderFact))),
                ["otherMountOwned"] = otherMountOwned, ["ledgerMount"] = hasLedger ? ledgerMount?.UniqueId : null,
                ["ac"] = firstMount.Stats.AC.ModifiedValue, ["frame"] = UnityEngine.Time.frameCount };
        }
    }
}
