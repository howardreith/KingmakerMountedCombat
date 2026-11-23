using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Shields;
using Kingmaker.EntitySystem.Persistence;
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

        private void PrepareShieldBeforeSave()
        {
            var service = MountedShieldService.Current;
            Require(service != null && horses.MountedShieldFeature != null && horses.ShieldFocusFeature != null && horses.MountedCombatFeature != null,
                "Mounted Shield is unavailable: " + horses.MountedShieldFailure);
            Require(Mounted(firstRider, firstMount), "The first pair left its mounted checkpoint before the shield setup.");
            var identity = new JObject();
            var heavy = Sprint6ShieldEquipment.ResolveStock("heavy", identity);
            var progression = firstRider.Descriptor.Progression.Features;
            if (!progression.HasFact(horses.MountedCombatFeature)) progression.AddFeature(horses.MountedCombatFeature);
            if (!progression.HasFact(horses.ShieldFocusFeature)) progression.AddFeature(horses.ShieldFocusFeature);
            if (!progression.HasFact(horses.MountedShieldFeature)) progression.AddFeature(horses.MountedShieldFeature);
            // Owned by the scenario from before the first hand change (supplemental review PN7-F2), so a setup that fails part
            // way is restored by the cleanup, after the save worker has settled.
            var equipment = shieldEquipment = new Sprint6ShieldEquipment(firstRider);
            equipment.Prepare(heavy);
            equipment.Equip(heavy);
            // The write the service makes each frame, made now, so the save starts from the ordinary mounted state.
            service.Update(true);
            var state = ShieldPersistenceState();
            state["equipment"] = equipment.Evidence;
            state["requestedShieldBlueprint"] = heavy.AssetGuid;
            state["shieldIdentity"] = identity["heavy"];
            evidence["shieldAtSave"] = state;
            Require(state.Value<int>("ownedCount") == 1, "The first mount does not hold exactly one Mounted Shield contribution before the save.");
        }

        // The consumer (cold) observes the restored world before input and reads the loaded archive itself: the native save
        // turned the fact off before serializing, so neither Mounted Shield source tag may appear in its party or area state
        // (supplemental review PN4-F8). The producer (warm) reads the archive it wrote in the same way.
        private Sprint6ShieldEquipment shieldEquipment;

        // The producer's equipment, restored after the measured warm continuation or a failure, once no save work owns the world
        // (the cleanup calls this only after its save, worker and scope obligations are clear). The written Manual keeps the
        // shield it was saved with. True when nothing remains.
        private bool DrainShieldEquipment()
        {
            if (shieldEquipment == null) return true;
            var restored = shieldEquipment.Restore();
            evidence["shieldEquipmentCleanup"] = restored;
            if (!shieldEquipment.Clean) return false;
            shieldEquipment = null;
            return true;
        }

        private void ObserveShieldCold(SaveInfo loaded)
        {
            var state = ShieldPersistenceState();
            state["archive"] = ShieldArchiveScan(loaded, saved?.AreaId);
            evidence["shieldColdAtRestore"] = state;
        }

        private void ObserveShieldContinuation()
        {
            if (Cold) { evidence["shieldColdContinuation"] = ShieldPersistenceState(); return; }
            evidence["shieldWarmContinuation"] = ShieldPersistenceState();
            evidence["shieldSavedArchive"] = ShieldArchiveScan(savedDescriptor, saved?.AreaId);
        }

        // Read-only: a clone of the descriptor's native saver reads party.json and the saved area's state; nothing is written.
        private static JObject ShieldArchiveScan(SaveInfo descriptor, string area)
        {
            var entries = new JArray(); var mountTags = 0; var checkTags = 0; string error = null;
            try
            {
                if (descriptor?.Saver == null) throw new System.InvalidOperationException("The archive descriptor has no native saver.");
                using (var saver = descriptor.Saver.Clone())
                    foreach (var entry in new[] { "party", area }.Where(value => !string.IsNullOrEmpty(value)))
                    {
                        var json = saver.ReadJson(entry);
                        entries.Add(new JObject { ["entry"] = entry + ".json", ["present"] = json != null, ["length"] = json?.Length ?? 0 });
                        if (json == null) continue;
                        mountTags += Occurrences(json, MountedShieldService.MountSourceComponent);
                        checkTags += Occurrences(json, MountedShieldService.CheckSourceComponent);
                    }
            }
            catch (System.Exception exception) { error = exception.GetType().FullName + ": " + exception.Message; }
            return new JObject { ["file"] = descriptor?.FileName, ["area"] = area, ["entries"] = entries, ["mountTags"] = mountTags, ["checkTags"] = checkTags, ["error"] = error };
        }

        private static int Occurrences(string text, string value)
        {
            var count = 0;
            for (var index = text.IndexOf(value, System.StringComparison.Ordinal); index >= 0; index = text.IndexOf(value, index + value.Length, System.StringComparison.Ordinal)) count++;
            return count;
        }

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
                // The item the rider actually holds, observed (supplemental review PN7-F6); never replaced by the requested one.
                ["shieldBase"] = shield?.ArmorComponent?.Blueprint?.ArmorBonus, ["observedShieldBlueprint"] = shield?.Blueprint.AssetGuid,
                // 2.1.7b items carry no persistent id: the process-local object identity binds observations within one process.
                ["observedShieldItem"] = shield == null ? (int?)null : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(shield), ["observedShieldEnhancement"] = shield == null ? (int?)null : Sprint6ShieldEquipment.ArmorEnhancement(shield),
                ["amount"] = service?.Amount(firstRider, firstMount), ["ownedCount"] = owned.Length,
                ["ownedValues"] = new JArray(owned.Select(modifier => modifier.ModValue)),
                ["ownedByRiderFact"] = new JArray(owned.Select(modifier => riderFact != null && ReferenceEquals(modifier.Source, riderFact))),
                ["otherMountOwned"] = otherMountOwned, ["ledgerMount"] = hasLedger ? ledgerMount?.UniqueId : null,
                ["ac"] = firstMount.Stats.AC.ModifiedValue, ["frame"] = UnityEngine.Time.frameCount };
        }
    }
}
