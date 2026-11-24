using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Controllers.Combat;
using Kingmaker.Utility;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Items.Shields;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UI.Selection;
using Kingmaker.UI.SettingsUI;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.FactLogic;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // Mounted Shield rows (docs/missions/mounted-shield/ACCEPTANCE.md), in the Sprint 6 feat family: the same fixture,
    // native level-up controller, defense record, rule observer and planned-total check, so the Mounted Combat check is the
    // production one and "equal" is the final native total including Mounted Shield (Sprint6PlannedTotal). Every amount the
    // reader compares comes from native state: the mount's AC stat and its modifiers, native RuleCalculateAC results, the
    // defense record's native check and the production MountedShieldService.Amount the service itself writes. Setup is
    // disclosed: experience, shield items created from stock blueprints, Greater Shield Focus and the second pair's feats
    // granted directly, native Shield-descriptor modifiers standing in for another shield source. The reader owns PASS/FAIL.
    // Custody of the driver's setup modifiers (supplemental review PN5-F3). Each one is recorded before it is written and
    // carries its own source component, so a write whose stat-change callback throws before AddModifier returns a handle is
    // still found on its stat and removed through the stat (MountedShieldService.RemoveExact), verified. An obligation stays
    // until its stat holds no modifier of that identity; Release can be called again for what remains. Every fault (a removal
    // that threw, or a stat-change callback that threw after the native list removal) is kept for the whole row (supplemental
    // review PN7-F3): a faulted removal fails the row and stays in the drain evidence even when a later retry finds nothing left.
    internal sealed class Sprint6ShieldSetupModifiers
    {
        internal const string ComponentPrefix = "KMC_Sprint6Shield_Setup_";
        private readonly List<KeyValuePair<ModifiableValue, string>> obligations = new List<KeyValuePair<ModifiableValue, string>>();
        internal readonly JArray Faults = new JArray();

        internal int Outstanding => obligations.Count;

        internal void Add(ModifiableValue stat, int value, string name, ModifierDescriptor descriptor)
        {
            if (stat == null) throw new ArgumentNullException(nameof(stat));
            var component = ComponentPrefix + name;
            if (!obligations.Any(pair => ReferenceEquals(pair.Key, stat) && pair.Value == component))
                obligations.Add(new KeyValuePair<ModifiableValue, string>(stat, component));
            stat.AddModifier(value, (Kingmaker.Blueprints.Facts.Fact)null, component, descriptor);
        }

        // Releases one named setup (all stats), or every one with name null; returns the faults and what remains.
        internal JObject Release(string name = null)
        {
            var faults = new JArray();
            foreach (var pair in obligations.ToArray())
            {
                if (name != null && pair.Value != ComponentPrefix + name) continue;
                try
                {
                    foreach (var modifier in pair.Key.Modifiers.Where(value => string.Equals(value.SourceComponent, pair.Value, StringComparison.Ordinal)).ToArray())
                        MountedShieldService.RemoveExact(pair.Key, modifier, fault => faults.Add(pair.Value + " callback: " + fault.Message));
                }
                catch (Exception exception) { faults.Add(pair.Value + ": " + exception.Message); }
                if (!pair.Key.Modifiers.Any(value => string.Equals(value.SourceComponent, pair.Value, StringComparison.Ordinal))) obligations.Remove(pair);
            }
            foreach (var fault in faults) Faults.Add(fault.DeepClone());
            return new JObject { ["faults"] = faults, ["allFaults"] = Faults.DeepClone(), ["remaining"] = new JArray(obligations.Select(pair => pair.Value)) };
        }
    }

    // Custody of the hands of one disposable-fixture actor (supplemental review PN4-F4, PN6-F2), through the production
    // FixtureHandCustody bound to the native HandSlot calls: every hand slot of every set is captured before any change, a slot
    // is emptied only when the native slot allows the removal (CanRemoveItem; a non-removable original refuses and stays), an
    // item is placed only into an empty slot that accepts it, each step verified by the slot's occupant. Created shields are
    // tracked before insertion, released out of every slot and collection (verified) and disposed; failures stay recorded and
    // Restore can be called again.
    internal sealed class Sprint6ShieldEquipment
    {
        private readonly UnitEntityData owner;
        private readonly List<ItemEntity> created = new List<ItemEntity>();
        private readonly FixtureHandCustody<HandSlot, ItemEntity> custody;
        internal readonly JObject Evidence = new JObject();
        internal int ShieldSet { get; private set; } = -1;
        internal bool Clean { get; private set; }
        private int originalSet = -1;

        internal Sprint6ShieldEquipment(UnitEntityData owner)
        {
            this.owner = owner;
            custody = new FixtureHandCustody<HandSlot, ItemEntity>(slot => slot.MaybeItem, slot => slot.CanRemoveItem(), slot => slot.RemoveItem(true),
                (slot, item) => slot.CanInsertItem(item), (slot, item) => slot.InsertItem(item), item => item.HoldingSlot as HandSlot, slot => slot.IsPrimaryHand);
            // Restoration failures name the actor, set, hand and the expected and observed items (alpha closure review PN14-F1).
            custody.DescribeSlot = slot => "actor " + owner.UniqueId + " set " + owner.Body.HandsEquipmentSets.FindIndex(set => set.PrimaryHand == slot || set.SecondaryHand == slot) +
                (slot != null && slot.IsPrimaryHand ? " primary" : " secondary");
            custody.DescribeItem = item => item == null ? "none" : item.Blueprint?.AssetGuid + "/" + item.Blueprint?.name + "#" + System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(item);
        }

        // Verified stock shields (supplemental review PN6-F4): the item blueprint GUIDs, names and armor types read from the
        // installed 2.1.7b data (sharedassets1.assets), each resolved by GUID and checked for its exact stock type, name, armor
        // type and asset origin (a positive Unity instance id), with its native armor enhancement read from the created item.
        internal static readonly string[][] StockItems = {
            new[] { "buckler", "78aef04821150bd479314bc974ea73e2", "Buckler", "26fcc43f7d20374498d2e1643381d345", "0" },
            new[] { "light", "a85d51d0fb905f940b951eec60388bac", "LightShield", "d38e8ea23ce653c4582eb3e002555483", "0" },
            new[] { "heavy", "f4cef3ba1a15b0f4fa7fd66b602ff32b", "HeavyShield", "d1b05b901bab9524388ebfa0435902a6", "0" },
            new[] { "tower", "6a1a429f023e5134fb16a4962cb79257", "TowerShield", "5f0f4b6e480e7054b8592b5a8b55854a", "0" },
            new[] { "heavyPlus1", "5c7b898a1bfb6cb4f8c14a0ebc143abe", "HeavyShieldItemPlus1", "d1b05b901bab9524388ebfa0435902a6", "1" },
            new[] { "heavyPlus3", "7e23918977b18524591204c93fae0a37", "HeavyShieldItemPlus3", "d1b05b901bab9524388ebfa0435902a6", "3" },
            new[] { "heavyPlus5", "0b09d902ff100524dbec6e3d3d93dc3b", "HeavyShieldItemPlus5", "d1b05b901bab9524388ebfa0435902a6", "5" } };

        internal static BlueprintItemShield ResolveStock(string key, JObject evidence)
        {
            var row = StockItems.First(value => value[0] == key);
            var shield = ResourcesLibrary.TryGetBlueprint<BlueprintItemShield>(row[1]);
            var stock = shield != null && shield.GetType() == typeof(BlueprintItemShield) && shield.name == row[2] && shield.GetInstanceID() > 0 &&
                shield.ArmorComponent != null && shield.ArmorComponent.Type != null && shield.ArmorComponent.Type.AssetGuid == row[3];
            evidence[key] = new JObject { ["guid"] = row[1], ["name"] = shield?.name, ["expectedName"] = row[2], ["armorType"] = shield?.ArmorComponent?.Type?.AssetGuid,
                ["expectedArmorType"] = row[3], ["requestedEnhancement"] = int.Parse(row[4]), ["instanceId"] = shield == null ? 0 : shield.GetInstanceID(), ["stockVerified"] = stock };
            if (!stock) throw new InvalidOperationException("The verified stock shield " + key + " (" + row[1] + ") is not loaded with its stock identity.");
            return shield;
        }

        // One shield set whose off-hand takes a shield; a set whose occupant the native slot will not release is skipped, never
        // overwritten. Every other set's off-hand must be released of shields, so a set switch has a shieldless side.
        internal void Prepare(BlueprintItemShield probe)
        {
            var sets = owner.Body.HandsEquipmentSets;
            originalSet = owner.Body.CurrentHandEquipmentSetIndex;
            custody.Capture(sets.SelectMany(set => new[] { set.PrimaryHand, set.SecondaryHand }));
            Evidence["sets"] = sets.Count; Evidence["originalSet"] = originalSet;
            var test = Create(probe);
            var refused = new JArray(); Evidence["refusedSets"] = refused;
            for (var index = 0; index < sets.Count && ShieldSet < 0; index++)
            {
                var slot = sets[index].SecondaryHand;
                if (!custody.Vacate(slot)) { refused.Add(index); continue; }
                if (!slot.CanInsertItem(test) && !custody.Vacate(sets[index].PrimaryHand)) { refused.Add(index); continue; }
                if (slot.CanInsertItem(test)) ShieldSet = index;
            }
            if (ShieldSet < 0) { Release(test); throw new InvalidOperationException("No hand set of the actor can be cleared for a shield in its off-hand."); }
            for (var index = 0; index < sets.Count; index++)
                if (index != ShieldSet && sets[index].SecondaryHand.MaybeItem is ItemEntityShield && !custody.Vacate(sets[index].SecondaryHand))
                    throw new InvalidOperationException("An original shield in another set cannot be removed; the set switch would not be shieldless.");
            if (!Release(test)) throw new InvalidOperationException("The probe shield could not be released.");
            owner.Body.CurrentHandEquipmentSetIndex = ShieldSet;
            Evidence["shieldSet"] = ShieldSet;
        }

        internal ItemEntityShield Equip(BlueprintItemShield blueprint)
        {
            string refusal;
            var shield = TryEquip(blueprint, out refusal);
            if (shield == null) throw new InvalidOperationException("The shield set refused " + blueprint.name + ": " + refusal);
            return shield;
        }

        // A genuine native slot refusal (CanInsertItem false) returns null with the reason; every other failure propagates.
        internal ItemEntityShield TryEquip(BlueprintItemShield blueprint, out string refusal)
        {
            refusal = null;
            Unequip();
            var shield = Create(blueprint);
            var slot = owner.Body.HandsEquipmentSets[ShieldSet].SecondaryHand;
            if (!slot.CanInsertItem(shield)) { refusal = "native slot refused " + blueprint.AssetGuid; return null; }
            if (!custody.Place(slot, shield)) throw new InvalidOperationException("Native equip did not keep " + blueprint.name + ".");
            return shield;
        }

        // The created shield leaves the slot and stays in the inventory (MS-06); verified.
        internal ItemEntity Unequip()
        {
            var slot = owner.Body.HandsEquipmentSets[ShieldSet].SecondaryHand;
            var item = slot.MaybeItem;
            if (item != null && created.Contains(item) && !custody.Vacate(slot)) throw new InvalidOperationException("The created shield could not be unequipped.");
            return item;
        }

        private ItemEntityShield Create(BlueprintItemShield blueprint)
        {
            var item = blueprint.CreateEntity<ItemEntityShield>();
            if (item == null) throw new InvalidOperationException("The native item factory created no shield for " + blueprint.name + ".");
            created.Add(item);
            item.Identify();
            var inserted = owner.Inventory.Add(item, true);
            if (!ReferenceEquals(inserted, item)) throw new InvalidOperationException("The inventory changed the created shield's identity.");
            return item;
        }

        internal static int ArmorEnhancement(ItemEntityShield shield) => shield?.ArmorComponent == null ? 0 : Kingmaker.Designers.GameHelper.GetItemEnhancementBonus(shield.ArmorComponent);

        // Out of its slot (only through the native removal, verified) and then out of its collection (verified), then disposed.
        private bool Release(ItemEntity item)
        {
            var held = item.HoldingSlot;
            if (held != null)
            {
                var hand = held as HandSlot;
                if (hand == null ? !held.CanRemoveItem() || !held.RemoveItem(true) : !custody.Vacate(hand)) return false;
                if (item.HoldingSlot != null) return false;
            }
            if (item.Collection != null) item.Collection.Remove(item);
            if (item.HoldingSlot != null || item.Collection != null) return false;
            item.Dispose();
            created.Remove(item);
            return true;
        }

        internal JObject Restore()
        {
            var errors = new JArray();
            foreach (var item in created.Where(value => value.HoldingSlot == null).ToArray())
            {
                try { if (!Release(item)) errors.Add("created shield still held: " + item.Blueprint.AssetGuid); }
                catch (Exception exception) { errors.Add("release: " + exception.Message); }
            }
            try { foreach (var hand in custody.Restore(item => created.Contains(item), Release)) errors.Add(hand); }
            catch (Exception exception) { errors.Add("restore: " + exception.Message); }
            foreach (var item in created.ToArray()) errors.Add("created shield still held: " + item.Blueprint.AssetGuid);
            try { if (originalSet >= 0 && owner.Body.CurrentHandEquipmentSetIndex != originalSet) owner.Body.CurrentHandEquipmentSetIndex = originalSet; }
            catch (Exception exception) { errors.Add("set: " + exception.Message); }
            if (owner.Body.CurrentHandEquipmentSetIndex != originalSet) errors.Add("weapon set not restored");
            Clean = errors.Count == 0 && created.Count == 0;
            var restored = new JObject { ["errors"] = errors, ["createdLeft"] = created.Count, ["handsChecked"] = custody.Captured,
                ["set"] = owner.Body.CurrentHandEquipmentSetIndex, ["originalSet"] = originalSet };
            Evidence["restored"] = restored;
            return restored;
        }
    }

    internal sealed partial class Phase3dHorseScenarioTranche
    {
        internal const string Sprint6ShieldRealTime = "sprint6-shield-rt", Sprint6ShieldTurnBased = "sprint6-shield-tb";
        internal static bool IsSprint6ShieldScenario(string scenario) => scenario == Sprint6ShieldRealTime || scenario == Sprint6ShieldTurnBased;
        internal static readonly string[] Sprint6ShieldRealTimeCases = { "MS-selection", "MS-equipment", "MS-pairs", "MS-check-rt" };
        internal static readonly string[] Sprint6ShieldTurnBasedCases = { "MS-rider-feat", "MS-check-tb" };
        private bool Sprint6Shield => IsSprint6ShieldScenario(request.Scenario);
        // Stock 2.1.7b armor types (scripts/Test-MountedShieldNativeContract.ps1 reads them from the installed data).
        private static readonly string[] Sprint6ShieldTypeNames = { "buckler", "light", "heavy", "tower" };
        private const int Sprint6ShieldSettleFrames = 6, Sprint6ShieldBandBound = 120;

        private JObject s9Measure;
        private Sprint6ShieldEquipment s9Equipment, s9OwnerEquipment;
        private Dictionary<string, BlueprintItemShield> s9Mundane;
        private List<BlueprintItemShield> s9Enhanced;
        private int s9Enhancement;
        private readonly Sprint6ShieldSetupModifiers s9Setup = new Sprint6ShieldSetupModifiers();
        private readonly List<KeyValuePair<UnitEntityData, Buff>> s9Buffs = new List<KeyValuePair<UnitEntityData, Buff>>();
        private int s9DrainAttempts;
        private const int Sprint6ShieldDrainAttempts = 30;
        private int s9BandAttempts, s9BandRulesBefore;

        private void BeginSprint6Shield()
        {
            s9Measure = new JObject { ["contract"] = "native-mounted-shield-v1", ["featGuid"] = HorseCompanionBlueprintService.MountedShieldFeatureGuid,
                ["policy"] = "Amounts are read from the mount's native AC stat, native RuleCalculateAC and the production MountedShieldService.Amount; checks are the production Mounted Combat check with a planned final native total.",
                ["setupDisclosure"] = "Experience; stock shield items created for the run; Greater Shield Focus and the second pair's Mounted Combat and Mounted Shield granted as facts; native Shield-descriptor modifiers as another shield source; a hostile attack-bonus adjustment for the AC band." };
            s6Measure["mountedShield"] = s9Measure;
        }

        // Every setup obligation is attempted on each call and kept until verified released (supplemental review PN5-F3); the
        // Sprint 6 drain calls this again each frame while it returns false, within its own bounded cleanup. After the bound the
        // remaining obligations are recorded as failed residue (the reader refuses the run) and cleanup goes on.
        private bool DrainSprint6ShieldFixture()
        {
            if (!Sprint6Shield || s9Measure == null) return true;
            s9Plan.Clear(); s9PlanStep = -1;
            s9DrainAttempts++;
            var restore = new JObject { ["attempts"] = s9DrainAttempts }; s9Measure["drain"] = restore;
            restore["setup"] = s9Setup.Release();
            var buffsLeft = new JArray();
            foreach (var pair in s9Buffs.ToArray())
            {
                try { if (pair.Key.Buffs.Enumerable.Any(buff => ReferenceEquals(buff, pair.Value))) pair.Value.Remove(); } catch (Exception exception) { buffsLeft.Add("remove: " + exception.Message); }
                if (!pair.Key.Buffs.Enumerable.Any(buff => ReferenceEquals(buff, pair.Value))) s9Buffs.Remove(pair); else buffsLeft.Add(pair.Value.Blueprint.AssetGuid);
            }
            restore["buffsLeft"] = buffsLeft;
            if (s9TemporaryGreater && !ShieldReleaseGreater()) restore["greaterLeft"] = true;
            if (MountedShieldService.Current != null && !MountedShieldService.Current.Enabled) MountedShieldService.Current.Enable();
            if (s9Equipment != null) restore["rider"] = s9Equipment.Restore();
            if (s9OwnerEquipment != null) restore["owner"] = s9OwnerEquipment.Restore();
            var clean = s9Setup.Outstanding == 0 && s9Setup.Faults.Count == 0 && s9Buffs.Count == 0 && !s9TemporaryGreater && (s9Equipment == null || s9Equipment.Clean) && (s9OwnerEquipment == null || s9OwnerEquipment.Clean);
            restore["clean"] = clean;
            if (clean || s9DrainAttempts >= Sprint6ShieldDrainAttempts) { restore["final"] = true; return true; }
            return false;
        }

        private bool s9TemporaryGreater;

        // One planned attempt at a time (supplemental review PN10-F2): armed together with its step's record boundary, for the
        // measured rider, mount and the row's hostile, consumed by the first qualifying attempt, cleared on step exit and drain.
        private readonly PlannedAttempt s9Plan = new PlannedAttempt();
        private int s9PlanStep = -1;
        private bool Sprint6ShieldPlannedStep => s9PlanStep >= 0 && s9PlanStep == s6Step;
        private int? Sprint6ShieldPlanConsumedBy => s9Plan.ConsumedBy;

        private void ShieldArm(string plan, int step)
        {
            s6NextPlan = plan;
            s9Plan.Arm(plan, plan == "success" ? 1 : plan == "equal" ? 0 : -1, rider.UniqueId, horse.UniqueId, target.UniqueId);
            s9PlanStep = step;
            s6Step = step; s6BoundStep = step; s6BoundAt = Time.time; s6StrikesThisStep = 0; s6AttemptsAtStep = s6Records.Count;
        }

        private void ClearSprint6ShieldPlan() { s9Plan.Clear(); s9PlanStep = -1; }

        private void ShieldDisarm()
        {
            s9Plan.Clear(); s9PlanStep = -1;
            s9PlanStep = -1;
            s6NextPlan = "natural";
        }

        private int? Sprint6ShieldPlannedTotal(MountedCombatDefenseRecord record)
        {
            var total = s9Plan.Consume(record.Rider, record.Mount, record.Attacker, record.AttackResult, System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(record.Rule));
            if (total != null)
            {
                // The attempt that completed the planned step, with its hostile, for the reader (review PN10-F3).
                var planned = s6Facts["plannedAttempts"] as JObject ?? (JObject)(s6Facts["plannedAttempts"] = new JObject());
                planned[s6Step.ToString(System.Globalization.CultureInfo.InvariantCulture)] = new JObject { ["identity"] = s9Plan.ConsumedBy, ["attacker"] = record.Attacker,
                    ["rider"] = record.Rider, ["mount"] = record.Mount, ["plan"] = s9Plan.Plan };
            }
            return total;
        }

        private bool ShieldReleaseGreater()
        {
            if (!s9TemporaryGreater) return true;
            try { rider.Descriptor.Progression.Features.RemoveFact(horseService.GreaterShieldFocusFeature); } catch (Exception) { }
            if (!Has(rider.Descriptor, horseService.GreaterShieldFocusFeature)) s9TemporaryGreater = false;
            return !s9TemporaryGreater;
        }

        private Buff ShieldAddBuff(UnitEntityData unit, BlueprintBuff blueprint)
        {
            var buff = unit.Buffs.AddBuff(blueprint, unit, TimeSpan.FromMinutes(10));
            if (buff != null) s9Buffs.Add(new KeyValuePair<UnitEntityData, Buff>(unit, buff));
            return buff;
        }

        private void ShieldRemoveBuffs()
        {
            foreach (var pair in s9Buffs.ToArray())
            {
                if (pair.Key.Buffs.Enumerable.Any(buff => ReferenceEquals(buff, pair.Value))) pair.Value.Remove();
                if (!pair.Key.Buffs.Enumerable.Any(buff => ReferenceEquals(buff, pair.Value))) s9Buffs.Remove(pair);
            }
        }

        // ---- observations -----------------------------------------------------------------------------------------------
        // One native picture of what Mounted Shield does for this exact pair now.
        private JObject ShieldSnapshot(UnitEntityData shieldRider, UnitEntityData mount)
        {
            var service = MountedShieldService.Current;
            var ac = mount.Stats.AC;
            var owned = MountedShieldService.IdentityModifiers(ac, HorseCompanionBlueprintService.MountedShieldFeatureGuid);
            var facts = service?.ReadFacts(shieldRider, mount);
            UnitEntityData ledgerMount = null; int ledgerAmount;
            var riderFact = MountedShieldFeat == null ? null : shieldRider.Descriptor.Progression.Features.GetFact(MountedShieldFeat);
            var hasLedger = service != null && service.TryGetContribution(shieldRider, out ledgerMount, out ledgerAmount);
            var shield = MountedShieldService.EffectiveShield(shieldRider);
            // The native AC rules decide flat-footedness themselves (outside combat CanActInCombat is false), so each probe
            // records the choice it made and the reader compares with the matching stat value (review PN8-F3).
            var meleeRule = Rulebook.Trigger(new RuleCalculateAC(shieldRider, mount, AttackType.Melee));
            var touchRule = Rulebook.Trigger(new RuleCalculateAC(shieldRider, mount, AttackType.Touch));
            var row = new JObject {
                ["rider"] = shieldRider.UniqueId, ["mount"] = mount.UniqueId, ["frame"] = Time.frameCount,
                ["amount"] = service?.Amount(shieldRider, mount), ["serviceEnabled"] = service?.Enabled,
                ["facts"] = facts == null ? null : new JObject { ["available"] = facts.Available, ["pair"] = facts.MountedPair, ["ownsFeat"] = facts.OwnsFeat,
                    ["effectiveShield"] = facts.EffectiveShield, ["base"] = facts.ShieldBaseBonus, ["focus"] = facts.ShieldFocus, ["greater"] = facts.GreaterShieldFocus },
                ["shield"] = shield == null ? null : new JObject { ["blueprint"] = shield.Blueprint.AssetGuid, ["name"] = shield.Blueprint.name,
                    ["armorType"] = shield.ArmorComponent?.Blueprint?.Type?.AssetGuid, ["enchantmentValue"] = shield.EnchantmentValue,
                    ["armorEnhancement"] = Sprint6ShieldEquipment.ArmorEnhancement(shield), ["base"] = shield.ArmorComponent?.Blueprint?.ArmorBonus },
                ["ac"] = ac.ModifiedValue, ["touch"] = ac.Touch, ["flatFooted"] = ac.FlatFooted, ["flatFootedTouch"] = ac.FlatFootedTouch,
                ["ruleMelee"] = meleeRule.TargetAC, ["ruleMeleeFlatFooted"] = meleeRule.IsTargetFlatFooted,
                ["ruleTouch"] = touchRule.TargetAC, ["ruleTouchFlatFooted"] = touchRule.IsTargetFlatFooted,
                ["owned"] = new JArray(owned.Select(modifier => new JObject { ["value"] = modifier.ModValue, ["descriptor"] = modifier.ModDescriptor.ToString(),
                    ["sourceGuid"] = modifier.Source?.Blueprint?.AssetGuid, ["sourceIsRiderFact"] = riderFact != null && ReferenceEquals(modifier.Source, riderFact),
                    ["sourceName"] = (modifier.Source?.Blueprint as Kingmaker.Blueprints.Facts.BlueprintUnitFact)?.Name })),
                ["shieldModifiers"] = new JArray(ac.Modifiers.Where(modifier => modifier.ModDescriptor == ModifierDescriptor.Shield).Select(modifier => new JObject {
                    ["value"] = modifier.ModValue, ["kmc"] = owned.Contains(modifier), ["item"] = modifier.ItemSource != null, ["sourceGuid"] = modifier.Source?.Blueprint?.AssetGuid })),
                ["ledger"] = hasLedger ? new JObject { ["mount"] = ledgerMount?.UniqueId } : null,
                ["riderMobility"] = shieldRider.Stats.SkillMobility.ModifiedValue,
                ["riderMobilityKmc"] = shieldRider.Stats.SkillMobility.Modifiers.Count(modifier => string.Equals(modifier.SourceComponent, MountedShieldService.CheckSourceComponent, StringComparison.Ordinal) ||
                    string.Equals(modifier.SourceComponent, MountedShieldService.MountSourceComponent, StringComparison.Ordinal) || riderFact != null && ReferenceEquals(modifier.Source, riderFact)),
                ["riderShieldModifiers"] = new JArray(shieldRider.Stats.AC.Modifiers.Where(modifier => modifier.ModDescriptor == ModifierDescriptor.Shield).Select(modifier => new JObject {
                    ["value"] = modifier.ModValue, ["item"] = modifier.ItemSource != null, ["sourceGuid"] = modifier.Source?.Blueprint?.AssetGuid })),
                ["riderSet"] = shieldRider.Body.CurrentHandEquipmentSetIndex, ["mounted"] = relationship.World.Registry.FindActive(shieldRider)?.Boundary?.State == RelationshipState.Mounted };
            return row;
        }

        private JObject ShieldStep(string name, UnitEntityData shieldRider = null, UnitEntityData mount = null)
        {
            var record = ShieldSnapshot(shieldRider ?? rider, mount ?? horse);
            record["state"] = Sprint6State(shieldRider ?? rider);
            ((JObject)s6Facts["steps"])[name] = record;
            return record;
        }

        // Stock shields from the library: one mundane item per stock armor type, and the stock heavy shields whose only
        // difference is an enhancement (by value, from the native item entity).
        private void FindShieldBlueprints()
        {
            // Verified stock items only (supplemental review PN6-F4); a missing or changed stock item stops the row.
            var identities = new JObject();
            s9Measure["shieldBlueprints"] = identities;
            s9Mundane = new Dictionary<string, BlueprintItemShield>(StringComparer.Ordinal);
            foreach (var key in Sprint6ShieldTypeNames) s9Mundane[key] = Sprint6ShieldEquipment.ResolveStock(key, identities);
            s9Enhanced = new[] { "heavyPlus1", "heavyPlus3", "heavyPlus5" }.Select(key => Sprint6ShieldEquipment.ResolveStock(key, identities)).ToList();
        }

        // The stock Shield spell's buff (supplemental review PN6-F3): MageShieldBuff, GUID read from the installed data, resolved
        // by GUID and accepted only with its AC Shield-descriptor bonus among its components. Null, recorded, when unavailable.
        private const string MageShieldBuffGuid = "9c0fa9b438ada3f43864be8dd8b3e741";
        private BlueprintBuff ShieldSpellBuff(out int value)
        {
            value = 0;
            var buff = ResourcesLibrary.TryGetBlueprint<BlueprintBuff>(MageShieldBuffGuid);
            var bonus = buff?.ComponentsArray?.OfType<AddStatBonus>().FirstOrDefault(component => component.Stat == StatType.AC && component.Descriptor == ModifierDescriptor.Shield);
            if (buff == null || buff.name != "MageShieldBuff" || bonus == null) return null;
            value = bonus.Value;
            return buff;
        }

        // Applies the stock Shield spell to the unit for one observation; an existing one is never added to or removed by the
        // driver (the observation is then recorded unavailable).
        private JObject ShieldApplySpell(UnitEntityData unit)
        {
            int value;
            var buff = ShieldSpellBuff(out value);
            var record = new JObject { ["guid"] = MageShieldBuffGuid, ["unit"] = unit.UniqueId, ["value"] = value };
            if (buff == null) { record["unavailable"] = "The stock MageShieldBuff with its AC Shield bonus is not loaded."; return record; }
            if (unit.Buffs.Enumerable.Any(existing => existing.Blueprint == buff)) { record["unavailable"] = "The unit already holds the Shield spell."; return record; }
            record["applied"] = ShieldAddBuff(unit, buff) != null;
            return record;
        }

        // ---- acquisition ------------------------------------------------------------------------------------------------
        private BlueprintFeature ShieldFocus => horseService.ShieldFocusFeature;
        private BlueprintFeature MountedShieldFeat => horseService.MountedShieldFeature;
        private static bool Has(UnitDescriptor unit, BlueprintFeature feature) => feature != null && unit.Progression.Features.HasFact(feature);

        private void TickSprint6ShieldAcquire()
        {
            switch (s6Step)
            {
                case 0:
                    if (!rider.Commands.Empty || rider.AreHandsBusyWithAnimation) return;
                    Sprint6Inventory();
                    ShieldInventory();
                    if (MountedShieldFeat == null || horseService.MountedShieldFailure != null) { FailSprint6Row("Mounted Shield did not register: " + horseService.MountedShieldFailure); return; }
                    if (Has(rider.Descriptor, horseService.MountedCombatFeature) || Has(rider.Descriptor, ShieldFocus) || Has(rider.Descriptor, MountedShieldFeat))
                    { FailSprint6Row("The fixture rider already owns Mounted Combat, Shield Focus or Mounted Shield; the native choice must start without them."); return; }
                    s6Facts["levels"] = new JArray(); s6Step = 1; return;
                case 1:
                    // Mounted Combat and Shield Focus at one level with two feat choices (fighter bonus and ordinary), then
                    // Mounted Shield at a later level. Levels without both choices still advance with their own choices.
                    var levels = (JArray)s6Facts["levels"];
                    if (levels.Count >= 12) { FailSprint6Row("Mounted Shield was not reached within twelve native levels."); return; }
                    Sprint6EnsureLevels(rider, 1);
                    var level = ShieldCommitLevel(rider, true);
                    levels.Add(level);
                    if (level.Value<bool>("committed") != true) { FailSprint6Row("A native level did not commit: " + level.Value<string>("refused")); return; }
                    if (!Has(rider.Descriptor, MountedShieldFeat)) return;
                    s6Facts["riderFacts"] = ShieldFactCounts(rider);
                    s6Frames = 0; s6Step = Sprint6Case == "MS-selection" ? 2 : 5; return;
                case 2:
                    // The second pair's rider: Shield Focus through its own native level, where Mounted Shield must stay
                    // unavailable without Mounted Combat, then the native Ranger companion (Sprint 6's acquisition).
                    s6Owner = Game.Instance.Player.PartyCharacters.Select(reference => reference.Value).Where(member =>
                        member != null && member != rider && !member.Descriptor.IsPet && member.Descriptor.Pet == null && member.IsInGame && member.Descriptor.State.IsConscious &&
                        member.Descriptor.State.Size == Size.Medium && member.Descriptor.Progression.GetClassLevel(ResourcesLibrary.TryGetBlueprint<BlueprintCharacterClass>(Chunk7OrdinaryBuildDriver.RangerClassGuid)) == 0 &&
                        !Has(member.Descriptor, horseService.MountedCombatFeature) && !Has(member.Descriptor, MountedShieldFeat) && !Has(member.Descriptor, ShieldFocus))
                        .OrderBy(member => member.UniqueId, StringComparer.Ordinal).FirstOrDefault();
                    if (s6Owner == null) { FailSprint6Row("No Medium party member without a companion can take the second pair."); return; }
                    s6Rules.Watch(s6Owner);
                    var ownerLevels = new JArray(); s6Facts["ownerLevels"] = ownerLevels;
                    for (var pass = 0; pass < 4 && !Has(s6Owner.Descriptor, ShieldFocus); pass++)
                    {
                        Sprint6EnsureLevels(s6Owner, 1);
                        var ownerLevel = ShieldCommitLevel(s6Owner, false, ShieldFocus);
                        ownerLevels.Add(ownerLevel);
                        if (ownerLevel.Value<bool>("committed") != true) { FailSprint6Row("The second rider's native level did not commit: " + ownerLevel.Value<string>("refused")); return; }
                    }
                    if (!Has(s6Owner.Descriptor, ShieldFocus)) { FailSprint6Row("The second rider did not take Shield Focus natively."); return; }
                    s6Step = 3; return;
                case 3:
                    var acquisition = new JObject(); s6Facts["ownerCompanion"] = acquisition;
                    // Disclosed setup (supplemental review PN4-F6): native experience for the four Ranger levels the companion
                    // needs; every level still passes the native CanLevelUp entitlement check inside the acquisition.
                    acquisition["experienceBefore"] = s6Owner.Descriptor.Progression.Experience;
                    Sprint6EnsureLevels(s6Owner, 4);
                    acquisition["experienceSetup"] = s6Owner.Descriptor.Progression.Experience;
                    var acquired = NativeHorseFixtureAcquisition.Create(s6Owner, horseService, logger, acquisition);
                    acquisition["acquired"] = acquired;
                    if (!acquired) { FailSprint6Row("The second pair's native companion did not commit."); return; }
                    // The second pair's identities, for the reader's two-pair binding (controller review R1-F4).
                    acquisition["owner"] = s6Owner.UniqueId; acquisition["pet"] = s6Owner.Descriptor.Pet?.UniqueId;
                    // Disclosed setup: the second rider's Mounted Combat and Mounted Shield as facts (the native choice is the
                    // first rider's), and a light shield, so the two pairs differ (+2 against +3).
                    s6Owner.Descriptor.Progression.Features.AddFeature(horseService.MountedCombatFeature);
                    s6Owner.Descriptor.Progression.Features.AddFeature(MountedShieldFeat);
                    s6Facts["ownerSetupFacts"] = ShieldFactCounts(s6Owner);
                    s6Frames = 0; s6Step = 5; return;
                default:
                    if (++s6Frames < Sprint6SettleFrames) return;
                    FinishSprint6Row(true, "Mounted Shield was chosen through the native controller.");
                    return;
            }
        }

        private JObject ShieldFactCounts(UnitEntityData unit) => new JObject { ["unit"] = unit.UniqueId,
            ["mountedCombat"] = unit.Descriptor.Progression.Features.Enumerable.Count(feature => feature.Blueprint == horseService.MountedCombatFeature),
            ["shieldFocus"] = unit.Descriptor.Progression.Features.Enumerable.Count(feature => feature.Blueprint == ShieldFocus),
            ["mountedShield"] = unit.Descriptor.Progression.Features.Enumerable.Count(feature => feature.Blueprint == MountedShieldFeat),
            ["mountedShieldRank"] = unit.Descriptor.Progression.Features.GetRank(MountedShieldFeat) };

        private void ShieldInventory()
        {
            var feat = MountedShieldFeat;
            var sprite = horseService.MountedShieldIcon;
            s6Facts["shieldRegistration"] = new JObject { ["failure"] = horseService.MountedShieldFailure, ["withdrawn"] = horseService.MountedShieldWithdrawn,
                ["guid"] = feat?.AssetGuid, ["groups"] = new JArray(feat?.Groups.Select(group => group.ToString()) ?? new string[0]), ["ranks"] = feat?.Ranks, ["isClassFeature"] = feat?.IsClassFeature,
                ["hideInUi"] = feat?.HideInUI, ["prerequisites"] = new JArray((feat?.ComponentsArray ?? new BlueprintComponent[0]).OfType<Prerequisite>().Select(prerequisite => new JObject {
                    ["type"] = prerequisite.GetType().Name, ["group"] = prerequisite.Group.ToString(), ["hidden"] = prerequisite.HideInUI,
                    ["feature"] = (prerequisite as PrerequisiteFeature)?.Feature?.AssetGuid })),
                ["iconIsShieldIcon"] = feat != null && sprite != null && ReferenceEquals(feat.Icon, sprite), ["iconSprite"] = sprite?.name,
                ["iconWidth"] = sprite?.texture?.width, ["iconHeight"] = sprite?.texture?.height,
                ["name"] = feat?.Name, ["description"] = feat?.Description,
                ["shieldFocus"] = ShieldFocus?.AssetGuid, ["greaterShieldFocus"] = horseService.GreaterShieldFocusFeature?.AssetGuid };
            s6Facts["threeFeatSelections"] = new JArray(horseService.MountedCombatSelections.Select(selection => new JObject { ["guid"] = selection.AssetGuid, ["name"] = selection.name,
                ["combat"] = selection.AllFeatures.Count(feature => ReferenceEquals(feature, horseService.MountedCombatFeature)),
                ["archery"] = selection.AllFeatures.Count(feature => ReferenceEquals(feature, horseService.MountedArcheryFeature)),
                ["shield"] = selection.AllFeatures.Count(feature => ReferenceEquals(feature, MountedShieldFeat)),
                ["kmcTailExact"] = Sprint6KmcTailExact(selection.AllFeatures),
                // The last three entries as published (review PN8-F5), so the positive three-feat observation names each feat.
                ["kmcTail"] = new JArray(selection.AllFeatures.Skip(Math.Max(0, selection.AllFeatures.Length - 3)).Select(feature => feature?.AssetGuid)) }));
        }

        // One native level as Fighter, through the controller the player uses. Feat choices go to the ordinary and fighter
        // lists in the order Mounted Combat, Shield Focus, Mounted Shield (or only `only`), each when the native CanSelect
        // allows it; Mounted Shield's own native eligibility is read in every offered list before and after each choice.
        // With observe, the whole level is first chosen and cancelled (preview rollback), and a level holding two
        // prerequisite choices is backed out of once (UnselectFeature) and chosen again before the commit.
        private JObject ShieldCommitLevel(UnitEntityData unit, bool observe, BlueprintFeature only = null)
        {
            var record = new JObject { ["unit"] = unit.UniqueId, ["characterLevelBefore"] = unit.Descriptor.Progression.CharacterLevel, ["committed"] = false,
                ["canLevelUp"] = LevelUpController.CanLevelUp(unit.Descriptor), ["factsBefore"] = ShieldFactCounts(unit) };
            if (record.Value<bool>("canLevelUp") != true) { record["refused"] = "The native controller cannot level this unit."; return record; }
            if (observe)
            {
                var probe = ShieldLevelPass(unit, false, only);
                record["cancelled"] = probe;
                record["factsAfterCancel"] = ShieldFactCounts(unit);
                record["characterLevelAfterCancel"] = unit.Descriptor.Progression.CharacterLevel;
            }
            var pass = ShieldLevelPass(unit, true, only, observe);
            record["pass"] = pass;
            record["committed"] = pass.Value<bool>("committed");
            record["refused"] = pass["refused"];
            record["characterLevelAfter"] = unit.Descriptor.Progression.CharacterLevel;
            record["factsAfter"] = ShieldFactCounts(unit);
            return record;
        }

        private JObject ShieldLevelPass(UnitEntityData unit, bool commit, BlueprintFeature only, bool backOut = false)
        {
            var fighter = ResourcesLibrary.TryGetBlueprint<BlueprintCharacterClass>(Sprint6FighterClassGuid);
            var record = new JObject { ["commit"] = commit, ["committed"] = false };
            var choices = new JArray(); record["choices"] = choices;
            var eligibility = new JArray(); record["eligibility"] = eligibility;
            var originalAutoLevelup = SettingsRoot.Instance.AutoLevelup.CurrentValue;
            LevelUpController controller = null;
            var committed = false;
            try
            {
                SettingsRoot.Instance.AutoLevelup.CurrentValue = AutolevelupState.Off;
                controller = LevelUpController.StartWithoutAssigningStaticInstance(unit.Descriptor);
                if (!controller.SelectClass(fighter)) { record["refused"] = "The native controller refused the class."; return record; }
                while (controller.State.AttributePoints > 0) { if (!controller.SpendAttributePoint(StatType.Strength)) break; choices.Add("attribute:Strength"); }
                // Mounted Combat's own prerequisite (one trained Mobility rank), as Sprint 6 takes it.
                if (only == null && !Has(unit.Descriptor, horseService.MountedCombatFeature) && controller.Preview.Stats.SkillMobility.BaseValue == 0)
                { if (!controller.SpendSkillPoint(StatType.SkillMobility)) { record["refused"] = "The native controller refused the Mobility rank."; return record; } choices.Add("skill:SkillMobility"); }
                var offered = OfferedFeatStates(controller, false);
                record["offered"] = new JArray(offered.Select(state => ((BlueprintFeatureSelection)state.Selection).name));
                ShieldEligibility(eligibility, "start", controller, offered);
                // Each choice resolves its selection state from the controller's current state: a native selection rebuilds the
                // preview and may rebuild the states with it.
                var picked = new List<KeyValuePair<BlueprintFeatureSelection, BlueprintFeature>>();
                foreach (var selection in offered.Select(value => (BlueprintFeatureSelection)value.Selection).ToArray())
                {
                    var state = controller.State.Selections.FirstOrDefault(value => !value.Selected && ReferenceEquals(value.Selection, selection));
                    if (state == null) continue;
                    var wanted = only != null ? (Has(controller.Preview, only) ? null : only) : NextShieldWanted(controller.Preview, unit.Descriptor, offered.Length);
                    if (wanted == null) continue;
                    var item = selection.Items.FirstOrDefault(candidate => ReferenceEquals(candidate.Feature, wanted));
                    if (item == null || !selection.CanSelect(controller.Preview, controller.State, state, item)) { choices.Add("unavailable:" + selection.name + "=" + wanted.name); continue; }
                    if (!controller.SelectFeature(state, item)) { record["refused"] = "The native controller refused " + wanted.name + "."; return record; }
                    choices.Add("selection:" + selection.name + "=" + wanted.name);
                    picked.Add(new KeyValuePair<BlueprintFeatureSelection, BlueprintFeature>(selection, wanted));
                    ShieldEligibility(eligibility, "after-" + wanted.name, controller, OfferedFeatStates(controller, true));
                }
                // Back: the first of two choices made at this level (a prerequisite) is withdrawn; Mounted Shield's eligibility
                // and any Mounted Shield choice follow the preview, and the same choices are made again in order.
                if (backOut && picked.Count == 2)
                {
                    // Native UnselectFeature rebuilds the preview, its LevelUpState and its selection states (supplemental review
                    // PN4-F5), so everything after it is read from the controller's current state, never the states held above.
                    var first = picked[0];
                    var picks = picked.ToArray();
                    var firstState = controller.State.Selections.First(value => value.Selected && ReferenceEquals(value.Selection, first.Key) && ReferenceEquals(value.SelectedItem?.Feature, first.Value));
                    controller.UnselectFeature(firstState);
                    record["backOut"] = new JObject { ["withdrawn"] = first.Value.name,
                        ["stillSelected"] = picks.Skip(1).All(pick => CurrentChoice(controller, pick.Key, pick.Value)),
                        ["previewHasWithdrawn"] = Has(controller.Preview, first.Value), ["previewHasOther"] = Has(controller.Preview, picks[1].Value) };
                    ShieldEligibility(eligibility, "back-" + first.Value.name, controller, OfferedFeatStates(controller, true));
                    foreach (var pick in picks)
                    {
                        if (CurrentChoice(controller, pick.Key, pick.Value)) continue;
                        var state = controller.State.Selections.FirstOrDefault(value => !value.Selected && ReferenceEquals(value.Selection, pick.Key));
                        var item = state == null ? null : pick.Key.Items.FirstOrDefault(candidate => ReferenceEquals(candidate.Feature, pick.Value));
                        if (item == null || !controller.SelectFeature(state, item)) { record["refused"] = "The native controller refused " + pick.Value.name + " again after Back."; return record; }
                        choices.Add("reselection:" + pick.Key.name + "=" + pick.Value.name);
                    }
                    record["afterBack"] = new JObject { ["mountedCombat"] = Has(controller.Preview, horseService.MountedCombatFeature), ["shieldFocus"] = Has(controller.Preview, ShieldFocus),
                        ["bothChosen"] = picks.All(pick => CurrentChoice(controller, pick.Key, pick.Value)) };
                    ShieldEligibility(eligibility, "reselected", controller, OfferedFeatStates(controller, true));
                }
                NativeHorseFixtureAcquisition.SpendPetSkills(controller, fighter, choices);
                for (var index = 0; index < 12 && ShieldFillChoice(controller, choices); index++) { }
                for (var index = 0; index < 12 && NativeHorseFixtureAcquisition.MakeNextPetSelection(controller, choices); index++) { }
                NativeHorseFixtureAcquisition.SpendPetSkills(controller, fighter, choices);
                record["complete"] = controller.State.IsComplete();
                record["previewFacts"] = new JObject { ["mountedCombat"] = Has(controller.Preview, horseService.MountedCombatFeature), ["shieldFocus"] = Has(controller.Preview, ShieldFocus),
                    ["mountedShield"] = Has(controller.Preview, MountedShieldFeat) };
                if (!commit) return record;
                if (controller.State.IsComplete() != true) { record["refused"] = "The native level was not complete."; return record; }
                controller.Commit();
                committed = true; record["committed"] = true;
            }
            finally
            {
                if (controller != null && !committed) controller.Cancel();
                SettingsRoot.Instance.AutoLevelup.CurrentValue = originalAutoLevelup;
            }
            return record;
        }

        // The ordinary and fighter feat choices of the controller's current state (fighter first); with includeSelected, the ones
        // already chosen at this level too.
        private static FeatureSelectionState[] OfferedFeatStates(LevelUpController controller, bool includeSelected) =>
            controller.State.Selections.Where(state => (includeSelected || !state.Selected) && state.Selection is BlueprintFeatureSelection selection &&
                (selection.name == "FighterFeatSelection" || selection.name == "BasicFeatSelection"))
                .OrderBy(state => ((BlueprintFeatureSelection)state.Selection).name == "FighterFeatSelection" ? 0 : 1).ToArray();

        // Whether the current state holds this exact choice in this selection.
        private static bool CurrentChoice(LevelUpController controller, BlueprintFeatureSelection selection, BlueprintFeature feature) =>
            controller.State.Selections.Any(state => state.Selected && ReferenceEquals(state.Selection, selection) && ReferenceEquals(state.SelectedItem?.Feature, feature));

        // Mounted Combat and Shield Focus together at the first level that offers two feat choices (Mounted Shield is read
        // between them and must refuse without Shield Focus), then Mounted Shield at a later level with a feat choice.
        private BlueprintFeature NextShieldWanted(UnitDescriptor preview, UnitDescriptor unit, int offeredCount)
        {
            var prerequisitesOwned = Has(unit, horseService.MountedCombatFeature) && Has(unit, ShieldFocus);
            // Both prerequisites, and later Mounted Shield itself, are taken at a level that offers both the ordinary and the
            // fighter choice, so the run observes Mounted Shield's native eligibility in each list (supplemental review PN9-F5).
            if (offeredCount < 2) return null;
            if (!Has(preview, horseService.MountedCombatFeature)) return horseService.MountedCombatFeature;
            if (!Has(preview, ShieldFocus)) return ShieldFocus;
            if (prerequisitesOwned && !Has(preview, MountedShieldFeat)) return MountedShieldFeat;
            return null;
        }

        // A remaining choice filled with its first native candidate that is none of the feats this row measures, so a
        // filler never takes Mounted Combat, Shield Focus, Greater Shield Focus or Mounted Shield out of order.
        private bool ShieldFillChoice(LevelUpController controller, JArray choices)
        {
            var measured = new BlueprintFeature[] { horseService.MountedCombatFeature, ShieldFocus, horseService.GreaterShieldFocusFeature, MountedShieldFeat, horseService.MountedArcheryFeature };
            foreach (var state in controller.State.Selections.Where(value => !value.Selected && value.Selection is BlueprintFeatureSelection).ToArray())
            {
                var selection = (BlueprintFeatureSelection)state.Selection;
                var item = selection.Items.FirstOrDefault(candidate => !measured.Contains(candidate.Feature) && selection.CanSelect(controller.Preview, controller.State, state, candidate));
                if (item == null || !controller.SelectFeature(state, item)) continue;
                choices.Add("filler:" + selection.name + "=" + item.Feature?.name);
                return true;
            }
            return false;
        }

        private void ShieldEligibility(JArray rows, string moment, LevelUpController controller, FeatureSelectionState[] offered)
        {
            foreach (var state in offered)
            {
                var selection = (BlueprintFeatureSelection)state.Selection;
                var item = selection.Items.FirstOrDefault(candidate => ReferenceEquals(candidate.Feature, MountedShieldFeat));
                rows.Add(new JObject { ["moment"] = moment, ["selection"] = selection.name, ["offered"] = item != null, ["selected"] = state.Selected,
                    ["mountedCombat"] = Has(controller.Preview, horseService.MountedCombatFeature), ["shieldFocus"] = Has(controller.Preview, ShieldFocus),
                    ["eligible"] = item != null && selection.CanSelect(controller.Preview, controller.State, state, item),
                    ["iconIsShieldIcon"] = item != null && ReferenceEquals(item.Feature.Icon, horseService.MountedShieldIcon) });
            }
        }

        // ---- equipment and AC (exploration, mounted) ------------------------------------------------------------------
        private void TickSprint6ShieldEquipment()
        {
            if (s6Step % 2 == 1)
            {
                if (++s6Frames < Sprint6ShieldSettleFrames) return;
                s6Frames = 0; s6Step++; return;
            }
            switch (s6Step)
            {
                case 0:
                    FindShieldBlueprints();
                    if (s9Mundane.Count != 4) { FailSprint6Row("The library lacks a mundane stock shield of every type."); return; }
                    s9Equipment = new Sprint6ShieldEquipment(rider);
                    s9Equipment.Prepare(s9Mundane["heavy"]);
                    s6Facts["equipment"] = s9Equipment.Evidence;
                    s6Step = 1; return;
                case 2: ShieldStep("none"); s9Equipment.Equip(s9Mundane["buckler"]); s6Step = 3; return;
                case 4: ShieldStep("buckler"); s9Equipment.Equip(s9Mundane["light"]); s6Step = 5; return;
                case 6: ShieldStep("light"); s9Equipment.Equip(s9Mundane["heavy"]); s6Step = 7; return;
                case 8:
                    ShieldStep("heavy");
                    // A tower shield the native slot genuinely refuses (CanInsertItem) is recorded, not forced, and that observation
                    // stays partial; any other setup or identity failure propagates (supplemental review PN5-F9).
                    string towerRefusal;
                    if (s9Equipment.TryEquip(s9Mundane["tower"], out towerRefusal) == null) s6Facts["towerRefused"] = towerRefusal;
                    s6Step = 9; return;
                case 10:
                    if (s6Facts["towerRefused"] == null) ShieldStep("tower");
                    s9Enhancement = 0;
                    s6Step = 11; return;
                // Every enhanced stock heavy shield in turn: the base is the same, so the amount must be too.
                case 12:
                    if (s9Enhancement > 0) ShieldStep("heavyEnhanced" + (s9Enhancement - 1))["requestedEnhancement"] = new[] { 1, 3, 5 }[s9Enhancement - 1];
                    if (s9Enhancement >= s9Enhanced.Count) { s9Equipment.Equip(s9Mundane["heavy"]); s6Step = 13; return; }
                    s9Equipment.Equip(s9Enhanced[s9Enhancement++]);
                    s6Step = 11; return;
                case 14:
                    ShieldStep("heavyAgain");
                    // Disclosed setup: Greater Shield Focus as a fact, then removed; owned by the drain in between (PN6-F2).
                    if (Has(rider.Descriptor, horseService.GreaterShieldFocusFeature)) { FailSprint6Row("The rider already owns Greater Shield Focus."); return; }
                    s9TemporaryGreater = true;
                    rider.Descriptor.Progression.Features.AddFeature(horseService.GreaterShieldFocusFeature);
                    s6Step = 15; return;
                case 16:
                    ShieldStep("greater");
                    if (!ShieldReleaseGreater()) throw new InvalidOperationException("The temporary Greater Shield Focus could not be removed.");
                    s6Step = 17; return;
                case 18: ShieldStep("greaterRemoved"); s9Setup.Add(horse.Stats.AC, 2, "competing", ModifierDescriptor.Shield); s6Step = 19; return;
                case 20: ShieldStep("competing2"); ShieldRequireReleased("competing"); s9Setup.Add(horse.Stats.AC, 3, "competing", ModifierDescriptor.Shield); s6Step = 21; return;
                case 22: ShieldStep("competing3"); ShieldRequireReleased("competing"); s9Setup.Add(horse.Stats.AC, 4, "competing", ModifierDescriptor.Shield); s6Step = 23; return;
                case 24:
                    ShieldStep("competing4"); ShieldRequireReleased("competing");
                    s6Facts["mountSpell"] = ShieldApplySpell(horse);
                    s6Step = 25; return;
                case 26:
                    ShieldStep("mountSpell");
                    ShieldRemoveBuffs();
                    var other = (s9Equipment.ShieldSet + 1) % rider.Body.HandsEquipmentSets.Count;
                    s6Facts["otherSet"] = other;
                    rider.Body.CurrentHandEquipmentSetIndex = other;
                    s6Step = 27; return;
                case 28: ShieldStep("otherSet"); rider.Body.CurrentHandEquipmentSetIndex = s9Equipment.ShieldSet; s6Step = 29; return;
                case 30: ShieldStep("setRestored"); s9Equipment.Unequip(); s6Step = 31; return;
                case 32:
                    var unequipped = ShieldStep("unequipped");
                    unequipped["shieldInInventory"] = rider.Inventory.Items.Any(item => item is ItemEntityShield && item.HoldingSlot == null);
                    s6Facts["riderSpell"] = ShieldApplySpell(rider);
                    s6Step = 33; return;
                case 34:
                    ShieldStep("riderSpellOnly");
                    ShieldRemoveBuffs();
                    s9Equipment.Equip(s9Mundane["heavy"]);
                    s6Step = 35; return;
                case 36:
                    ShieldStep("final");
                    FinishSprint6Row(true, "Equipment and AC observations recorded.");
                    return;
            }
        }

        // ---- two pairs and lifecycle (exploration) ----------------------------------------------------------------------
        private void TickSprint6ShieldPairs()
        {
            switch (s6Step)
            {
                case 0:
                    if (s9Equipment == null) { FailSprint6Row("The rider's shield set was not prepared."); return; }
                    s9OwnerEquipment = new Sprint6ShieldEquipment(s6Owner);
                    s9OwnerEquipment.Prepare(s9Mundane["light"]);
                    s9OwnerEquipment.Equip(s9Mundane["light"]);
                    s6Facts["ownerEquipment"] = s9OwnerEquipment.Evidence;
                    s6Frames = 0; s6Step = 1; return;
                case 1:
                    s6OwnerPet = s6Owner.Descriptor.Pet;
                    if (s6OwnerPet == null || !s6OwnerPet.IsInState || s6OwnerPet.View?.AgentASP == null || !s6Owner.Commands.Empty || ++s6Frames < Sprint6SettleFrames) return;
                    s6Rules.Watch(s6OwnerPet);
                    if (Sprint5Fixture.Click(nativeControls, s6Owner, nativeControls.MountAbility, s6OwnerPet)) { s6Frames = 0; s6Step = 2; ResetLeafClock(); }
                    return;
                case 2:
                    if (relationship.World.Registry.FindActive(s6Owner)?.Boundary?.State != RelationshipState.Mounted || ++s6Frames < Sprint6SettleFrames) return;
                    // The UI selection switch is observed, not assumed (supplemental review PN9-F6): the first rider is the single
                    // native selection before it, the second rider after it, and the first rider again afterwards.
                    if (SelectionManager.Instance.SingleSelectedUnit != rider) { SelectionManager.Instance.SelectUnit(rider.View, true, true, false); return; }
                    ShieldStep("bothFirst"); ShieldStep("bothSecond", s6Owner, s6OwnerPet);
                    s6Facts["crossFirstToSecondMount"] = ShieldSnapshot(rider, s6OwnerPet); s6Facts["crossSecondToFirstMount"] = ShieldSnapshot(s6Owner, horse);
                    s6Facts["selectedBeforeSwitch"] = SelectionManager.Instance.SingleSelectedUnit?.UniqueId;
                    SelectionManager.Instance.SelectUnit(s6Owner.View, true, true, false);
                    s6Frames = 0; s6Step = 3; return;
                case 3:
                    if (++s6Frames < Sprint6ShieldSettleFrames) return;
                    s6Facts["selectedAtSecondSnapshots"] = SelectionManager.Instance.SingleSelectedUnit?.UniqueId;
                    ShieldStep("uiSelectedSecondFirst"); ShieldStep("uiSelectedSecondSecond", s6Owner, s6OwnerPet);
                    SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                    s6Step = 4; return;
                case 4:
                    if (s6Facts["selectedAfterReturn"] == null) s6Facts["selectedAfterReturn"] = SelectionManager.Instance.SingleSelectedUnit?.UniqueId;
                    if (!rider.Commands.Empty) return;
                    if (Sprint5Fixture.Click(nativeControls, rider, nativeControls.DismountAbility, rider)) { s6Frames = 0; s6Step = 5; ResetLeafClock(); }
                    return;
                case 5:
                    if (relationship.State == RelationshipState.Mounted || ++s6Frames < Sprint6ShieldSettleFrames) return;
                    ShieldStep("dismountedFirst"); ShieldStep("dismountedSecond", s6Owner, s6OwnerPet);
                    s6Step = 6; return;
                case 6:
                    if (!rider.Commands.Empty || rider.CombatState.HasCooldownForCommand(Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Move)) return;
                    if (Sprint5Fixture.Click(nativeControls, rider, nativeControls.MountAbility, horse)) { s6Frames = 0; s6Step = 7; ResetLeafClock(); }
                    return;
                case 7:
                    if (relationship.State != RelationshipState.Mounted || ++s6Frames < Sprint6ShieldSettleFrames) return;
                    ShieldStep("remounted");
                    rider.Descriptor.Progression.Features.RemoveFact(MountedShieldFeat);
                    s6Frames = 0; s6Step = 8; return;
                case 8:
                    if (++s6Frames < Sprint6ShieldSettleFrames) return;
                    ShieldStep("featRemoved"); ShieldStep("featRemovedSecond", s6Owner, s6OwnerPet);
                    rider.Descriptor.Progression.Features.AddFeature(MountedShieldFeat);
                    s6Frames = 0; s6Step = 9; return;
                case 9:
                    if (++s6Frames < Sprint6ShieldSettleFrames) return;
                    ShieldStep("featReacquired");
                    s6Facts["serviceDisableAccepted"] = MountedShieldService.Current.Disable("Mounted Shield scenario");
                    s6Frames = 0; s6Step = 10; return;
                case 10:
                    if (++s6Frames < Sprint6ShieldSettleFrames) return;
                    ShieldStep("serviceDisabledFirst"); ShieldStep("serviceDisabledSecond", s6Owner, s6OwnerPet);
                    MountedShieldService.Current.Enable();
                    s6Frames = 0; s6Step = 11; return;
                case 11:
                    if (++s6Frames < Sprint6ShieldSettleFrames) return;
                    ShieldStep("serviceEnabledFirst"); ShieldStep("serviceEnabledSecond", s6Owner, s6OwnerPet);
                    s6Step = 12; return;
                default:
                    FinishSprint6Row(true, "Two-pair and lifecycle observations recorded.");
                    return;
            }
        }

        // ---- the Mounted Combat check with Mounted Shield (real time) --------------------------------------------------
        private void TickSprint6ShieldCheckRealTime()
        {
            switch (s6Step)
            {
                case 0:
                    if (s9Equipment == null) { FailSprint6Row("The rider's shield set was not prepared."); return; }
                    s9Equipment.Equip(s9Mundane["heavy"]);
                    Sprint6SpawnTarget("ms-rt"); s6Facts["hostile"] = target.UniqueId; s6Step = 1; return;
                case 1:
                    if (!rider.IsInCombat || !horse.IsInCombat || !rider.CombatState.CanActInCombat) return;
                    ShieldStep("combatStart"); ShieldCalibrate("equal"); ShieldArm("equal", 2); return;
                // Equality of the shield-modified total fails and the hit stands.
                case 2:
                    if (Sprint6RecordSinceStep() == null) { Sprint6Command(horse); return; }
                    ShieldStep("equal"); ShieldDisarm(); s6Step = 3; return;
                case 3: if (!Sprint6RealTimeUsageFree(rider)) return; ShieldCalibrate("success"); ShieldArm("success", 4); return;
                case 4:
                    if (Sprint6RecordSinceStep() == null) { Sprint6Command(horse); return; }
                    ShieldStep("success");
                    // Equipment changes during the running use neither refund nor restart it.
                    var before = Sprint6State(rider); s9Equipment.Unequip(); var unequipped = Sprint6State(rider); s9Equipment.Equip(s9Mundane["heavy"]);
                    s6Facts["equipmentDuringUse"] = new JObject { ["before"] = before, ["unequipped"] = unequipped, ["reequipped"] = Sprint6State(rider) };
                    ShieldDisarm(); s6Frames = 0; s6Step = 5; return;
                case 5:
                    if (++s6Frames < Sprint6ShieldSettleFrames) return;
                    if (Sprint6RecordSinceStep() == null) { Sprint6RuleStrike(horse); return; }
                    ShieldStep("spentRefused"); s6Step = 6; return;
                case 6: if (!Sprint6RealTimeUsageFree(rider)) return; Sprint6SetPreference(false, "off"); ShieldDisarm(); s6Step = 7; return;
                case 7:
                    if (Sprint6RecordSinceStep() == null) { Sprint6RuleStrike(horse); return; }
                    ShieldStep("preferenceOffRefused"); Sprint6SetPreference(true, "on");
                    // An unrelated Mobility check by the rider: no Mounted Shield addition outside the hit-negation rule.
                    var unrelated = Rulebook.Trigger(new RuleSkillCheck(rider, StatType.SkillMobility, 10));
                    s6Facts["unrelatedMobility"] = new JObject { ["stat"] = unrelated.StatValue, ["bonus"] = (int)unrelated.Bonus, ["displayed"] = rider.Stats.SkillMobility.ModifiedValue,
                        ["dc"] = unrelated.DC, ["result"] = unrelated.RollResult, ["frame"] = Time.frameCount, ["contributions"] = MountedShieldService.Current.CheckContributionCount };
                    s6Step = 8; return;
                // A stronger shield source on the mount (+4) covers its AC; the one production check made under it still gets
                // the rider's full +3 (supplemental review PN4-F9).
                case 8:
                    if (!Sprint6RealTimeUsageFree(rider)) return;
                    s9Setup.Add(horse.Stats.AC, 4, "competing", ModifierDescriptor.Shield);
                    ShieldStep("competingBeforeCheck"); ShieldCalibrate("success"); ShieldArm("success", 9); return;
                case 9:
                    if (Sprint6RecordSinceStep() == null) { Sprint6RuleStrike(horse); return; }
                    ShieldStep("competingCheck"); ShieldRequireReleased("competing"); ShieldCalibrate(null);
                    ShieldDisarm(); s6Step = 10; return;
                // A hit that Mounted Shield's AC turns into a miss: no check, no use. The hostile's bonus is aimed so a total
                // inside the shield's band (AC - 3 .. AC - 1) is likely; only actual strikes count against the bound, and the
                // qualifying attack itself is recorded (supplemental review PN4-F10).
                case 10:
                    if (!Sprint6RealTimeUsageFree(rider)) return;
                    var amount = MountedShieldService.Current.Amount(rider, horse);
                    var probe = Rulebook.Trigger(new RuleCalculateAC(target, horse, AttackType.Melee)).TargetAC;
                    // The AC change Mounted Shield makes now (supplemental review PN5-F7): Shield bonuses do not stack, so it is
                    // the amount above the strongest other Shield modifier on the mount, read from the native modifiers.
                    var owned = MountedShieldService.IdentityModifiers(horse.Stats.AC, HorseCompanionBlueprintService.MountedShieldFeatureGuid);
                    var otherShield = horse.Stats.AC.Modifiers.Where(modifier => modifier.ModDescriptor == ModifierDescriptor.Shield && !owned.Contains(modifier)).Select(modifier => modifier.ModValue).DefaultIfEmpty(0).Max();
                    var effective = Math.Max(0, amount - Math.Max(0, otherShield));
                    var current = ShieldHostileBonus();
                    var adjustment = probe - 12 - current;
                    s9Setup.Add(target.Stats.AdditionalAttackBonus, adjustment, "band", ModifierDescriptor.UntypedStackable);
                    s6Facts["band"] = new JObject { ["amount"] = amount, ["ac"] = probe, ["otherShield"] = otherShield, ["effectiveDelta"] = effective,
                        ["acWithoutShield"] = probe - effective, ["attackBonusBefore"] = current, ["adjustment"] = adjustment };
                    if (effective < 1) { s6Facts["bandUnavailable"] = "Mounted Shield changes no AC on this mount now; the band cannot be measured."; s6Step = 12; return; }
                    s9BandAttempts = 0; s9BandRulesBefore = s6Rules.Attacks.Count; s6Step = 11; return;
                case 11:
                    var miss = ShieldBandMiss();
                    if (miss != null || s9BandAttempts >= Sprint6ShieldBandBound)
                    {
                        s6Facts["bandMiss"] = miss?.DeepClone(); s6Facts["bandAttempts"] = s9BandAttempts;
                        ShieldRequireReleased("band");
                        ShieldStep("bandDone"); s6Frames = 0; s6Step = 12; return;
                    }
                    if (!Sprint6RealTimeUsageFree(rider) || !target.Commands.Empty) return;
                    s9BandAttempts++; Sprint6RuleStrike(horse); return;
                default:
                    if (!target.Commands.Empty || ++s6Frames < Sprint6SettleFrames) return;
                    FinishSprint6Row(true, "Mounted Shield check observations recorded.");
                    return;
            }
        }

        // The first miss on the mount, since the band step began, whose total reached the AC without Mounted Shield's own
        // contribution.
        private JObject ShieldBandMiss()
        {
            for (var index = s9BandRulesBefore; index < s6Rules.Attacks.Count; index++)
            {
                var row = (JObject)s6Rules.Attacks[index];
                var context = row["acContext"] as JObject;
                // An ordinary native AC miss only (controller review R1-F5): not Mirror Image, concealment or another outcome.
                if (context == null || (string)row["target"] != horse.UniqueId || (string)row["attacker"] != target.UniqueId || row.Value<bool>("isHitAtEvent") ||
                    (string)row["resultAtEvent"] != "Miss") continue;
                var ac = row.Value<int>("ac"); var effective = context.Value<int>("effective");
                var total = row.Value<int>("d20") + row.Value<int>("bonus");
                if (effective >= 1 && context.Value<int>("acResult") == ac && row.Value<int>("d20") > 1 && total < ac && total >= ac - effective) return row;
            }
            return null;
        }

        // A setup released mid-row must be gone before the next observation; otherwise the row fails (the drain retries it).
        private void ShieldRequireReleased(string name)
        {
            var result = s9Setup.Release(name);
            if (((JArray)result["remaining"]).Any(value => (string)value == Sprint6ShieldSetupModifiers.ComponentPrefix + name) || ((JArray)result["faults"]).Count != 0)
                throw new InvalidOperationException("Setup " + name + " was not removed without a fault: " + result.ToString(Newtonsoft.Json.Formatting.None));
        }

        // Mounted Shield's effect on the mount's AC at the moment of one attack (supplemental review PN6-F6): its own modifier,
        // the strongest other Shield modifier, and the AC change that leaves (Shield bonuses do not stack).
        private JObject ShieldAcContext(UnitEntityData unit)
        {
            if (unit != horse) return null;
            var owned = MountedShieldService.IdentityModifiers(unit.Stats.AC, HorseCompanionBlueprintService.MountedShieldFeatureGuid);
            var own = owned.Select(modifier => modifier.ModValue).DefaultIfEmpty(0).Max();
            // Competing means non-stacking (controller review R1-F2): a stacking Shield modifier adds on top.
            var other = unit.Stats.AC.Modifiers.Where(modifier => modifier.ModDescriptor == ModifierDescriptor.Shield && !modifier.Stacks && !owned.Contains(modifier)).Select(modifier => modifier.ModValue).DefaultIfEmpty(0).Max();
            return new JObject { ["owned"] = own, ["ownedCount"] = owned.Length, ["otherShield"] = other, ["effective"] = Math.Max(0, own - Math.Max(0, other)) };
        }

        // The hostile's native attack bonus against the mount now (every setup modifier included).
        private int ShieldHostileBonus()
        {
            var weapon = target.GetThreatHand()?.Weapon ?? target.Body.EmptyHandWeapon;
            return Rulebook.Trigger(new RuleCalculateAttackBonus(target, horse, weapon, 0)).Result;
        }

        // Aims the hostile's native attack bonus at the rider's expected check base (Mobility plus the Mounted Shield amount),
        // so a planned total is reachable with a natural die (supplemental review PN4-F9): for "equal" the bonus is that base,
        // so the die is the attack's own d20 (1..20); for "success" it is one less, so the die is again the attack's d20.
        // null removes the calibration.
        private void ShieldCalibrate(string plan)
        {
            ShieldRequireReleased("calibration");
            if (plan == null) return;
            var checkBase = rider.Stats.SkillMobility.ModifiedValue + MountedShieldService.Current.Amount(rider, horse);
            var wanted = plan == "equal" ? checkBase : checkBase - 1;
            var current = ShieldHostileBonus();
            s9Setup.Add(target.Stats.AdditionalAttackBonus, wanted - current, "calibration", ModifierDescriptor.UntypedStackable);
            var calibrations = s6Facts["calibrations"] as JArray ?? new JArray();
            s6Facts["calibrations"] = calibrations;
            calibrations.Add(new JObject { ["plan"] = plan, ["checkBase"] = checkBase, ["attackBonusBefore"] = current, ["attackBonusAfter"] = ShieldHostileBonus(),
                ["mountAc"] = Rulebook.Trigger(new RuleCalculateAC(target, horse, AttackType.Melee)).TargetAC, ["step"] = s6Step });
        }

        // ---- turn-based ---------------------------------------------------------------------------------------------
        private void TickSprint6ShieldCheckTurnBased(TurnController turn)
        {
            switch (s6Step)
            {
                case 0:
                    if (turnBasedModeProbe == null) turnBasedModeProbe = new NativeModeTransitionProbe(true);
                    if (!turnBasedModeProbe.TemporaryValueIsCurrent) { turnBasedModeProbe.DispatchTemporaryValueIfRequired(); return; }
                    if (pairedAutomaticEndProbe == null) pairedAutomaticEndProbe = new NativeAutomaticEndProbe(false);
                    FindShieldBlueprints();
                    if (!s9Mundane.ContainsKey("heavy")) { FailSprint6Row("The library lacks a mundane heavy shield."); return; }
                    s9Equipment = new Sprint6ShieldEquipment(rider);
                    s9Equipment.Prepare(s9Mundane["heavy"]); s9Equipment.Equip(s9Mundane["heavy"]);
                    s6Facts["equipment"] = s9Equipment.Evidence;
                    Sprint6SpawnTarget("ms-tb"); s6Facts["hostile"] = target.UniqueId; s6Step = 1; return;
                // Own turn: success with the shield's addition, paid by this turn's Swift action.
                case 1:
                    if (!rider.IsInCombat || !horse.IsInCombat) return;
                    if (WaitForNativeCastingPrincipal(true, turn?.Unit == rider, rider.CombatState.CanActInCombat, () => EndCastingNativeTurn(turn))) return;
                    if (!rider.HasSwiftAction() || !Sprint6RiderTurn(turn)) return;
                    s6Turn = turn; ShieldStep("ownTurnBefore"); ShieldCalibrate("success"); ShieldArm("success", 2); return;
                case 2:
                    if (Sprint6RecordSinceStep() == null) { Sprint6OpportunityStrike(horse); return; }
                    ShieldStep("ownTurnNegated"); ShieldDisarm(); s6Step = 3; return;
                case 3:
                    if (Sprint6RecordSinceStep() == null) { Sprint6OpportunityStrike(horse); return; }
                    ShieldStep("ownTurnSecondRefused"); s6Step = 4; return;
                // The next own turn renews the use; off turn, equality of the shield-modified total borrows and stands.
                case 4:
                    if (turn?.Unit == rider && ReferenceEquals(turn, s6Turn)) { EndCastingNativeTurn(turn); return; }
                    if (turn?.Unit != rider) { Sprint6AdvanceTo(turn, rider); return; }
                    if (!Sprint6RiderTurn(turn)) return;
                    s6Turn = turn; ShieldStep("renewed"); s6Step = 5; return;
                case 5:
                    if (!Sprint6AdvanceTo(turn, target)) return;
                    ShieldCalibrate("equal"); ShieldArm("equal", 6); return;
                case 6:
                    if (Sprint6RecordSinceStep() != null) { ShieldStep("offTurnEqual"); ShieldCalibrate(null); ShieldDisarm(); s6Frames = 0; s6Step = 7; return; }
                    Sprint6HostileTurnStrike(turn);
                    return;
                default:
                    if (!target.Commands.Empty || ++s6Frames < Sprint6SettleFrames) return;
                    FinishSprint6Row(true, "Turn-based Mounted Shield check observations recorded.");
                    return;
            }
        }
    }
}
