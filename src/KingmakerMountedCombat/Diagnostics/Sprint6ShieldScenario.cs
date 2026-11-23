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
    // Custody of the hands of one disposable-fixture actor (supplemental review PN4-F4). Every hand slot of every weapon set
    // is captured before anything changes, so an original the native slot moves (an off-hand shield makes HandSlot remove a
    // two-handed primary) is restored as surely as one this class moved itself. Each created shield is released and checked
    // out of every slot and collection; each original is checked back in its own slot; a failure stays recorded.
    internal sealed class Sprint6ShieldEquipment
    {
        private readonly UnitEntityData owner;
        private readonly List<ItemEntity> created = new List<ItemEntity>();
        private readonly List<KeyValuePair<HandSlot, ItemEntity>> original = new List<KeyValuePair<HandSlot, ItemEntity>>();
        internal readonly JObject Evidence = new JObject();
        internal int ShieldSet { get; private set; } = -1;
        private int originalSet = -1;
        internal Sprint6ShieldEquipment(UnitEntityData owner) { this.owner = owner; }

        // One shield set whose off-hand takes a shield (a primary the native slot will not keep beside it is moved out first),
        // and every other set's off-hand cleared of shields, so a set switch has a well-defined shieldless side.
        internal void Prepare(BlueprintItemShield probe)
        {
            var sets = owner.Body.HandsEquipmentSets;
            originalSet = owner.Body.CurrentHandEquipmentSetIndex;
            foreach (var set in sets) { original.Add(new KeyValuePair<HandSlot, ItemEntity>(set.PrimaryHand, set.PrimaryHand.MaybeItem)); original.Add(new KeyValuePair<HandSlot, ItemEntity>(set.SecondaryHand, set.SecondaryHand.MaybeItem)); }
            Evidence["sets"] = sets.Count; Evidence["originalSet"] = originalSet;
            Evidence["originalHands"] = new JArray(original.Select(pair => pair.Value?.Blueprint.AssetGuid));
            var test = Create(probe);
            for (var index = 0; index < sets.Count && ShieldSet < 0; index++)
            {
                var slot = sets[index].SecondaryHand;
                if (slot.HasItem) slot.RemoveItem(true);
                if (!slot.CanInsertItem(test) && sets[index].PrimaryHand.HasItem) sets[index].PrimaryHand.RemoveItem(true);
                if (slot.CanInsertItem(test)) ShieldSet = index;
            }
            if (ShieldSet < 0) throw new InvalidOperationException("No hand set of the rider takes a shield in its off-hand.");
            for (var index = 0; index < sets.Count; index++)
                if (index != ShieldSet && sets[index].SecondaryHand.MaybeItem is ItemEntityShield) sets[index].SecondaryHand.RemoveItem(true);
            Release(test);
            owner.Body.CurrentHandEquipmentSetIndex = ShieldSet;
            Evidence["shieldSet"] = ShieldSet;
        }

        internal ItemEntityShield Equip(BlueprintItemShield blueprint)
        {
            Unequip();
            var shield = Create(blueprint);
            var slot = owner.Body.HandsEquipmentSets[ShieldSet].SecondaryHand;
            if (!slot.CanInsertItem(shield)) throw new InvalidOperationException("The shield set refused " + blueprint.name + ".");
            slot.InsertItem(shield);
            if (!ReferenceEquals(slot.MaybeItem, shield)) throw new InvalidOperationException("Native equip did not keep " + blueprint.name + ".");
            return shield;
        }

        // The shield leaves the slot and stays in the inventory (MS-06).
        internal ItemEntity Unequip()
        {
            var slot = owner.Body.HandsEquipmentSets[ShieldSet].SecondaryHand;
            var item = slot.MaybeItem;
            if (item != null && created.Contains(item)) slot.RemoveItem(true);
            return item;
        }

        private ItemEntityShield Create(BlueprintItemShield blueprint)
        {
            var item = blueprint.CreateEntity<ItemEntityShield>();
            if (item == null) throw new InvalidOperationException("The native item factory created no shield for " + blueprint.name + ".");
            item.Identify();
            var inserted = owner.Inventory.Add(item, true);
            if (!ReferenceEquals(inserted, item)) throw new InvalidOperationException("The inventory changed the created shield's identity.");
            created.Add(item);
            return item;
        }

        // Out of every slot and collection, verified; a shield still held stays tracked.
        private bool Release(ItemEntity item)
        {
            if (item.HoldingSlot != null) item.HoldingSlot.RemoveItem(true);
            if (item.Collection != null) item.Collection.Remove(item);
            if (item.HoldingSlot != null || item.Collection != null) return false;
            created.Remove(item);
            return true;
        }

        internal JObject Restore()
        {
            var errors = new JArray();
            foreach (var item in created.ToArray())
            {
                try { if (!Release(item)) errors.Add("created shield still held: " + item.Blueprint.AssetGuid); }
                catch (Exception exception) { errors.Add("release: " + exception.Message); }
            }
            // Originals back into their own slots: off-hands first, so a two-handed primary is inserted into a free pair.
            foreach (var pair in original.Where(value => !value.Key.IsPrimaryHand).Concat(original.Where(value => value.Key.IsPrimaryHand)))
            {
                if (pair.Value == null || ReferenceEquals(pair.Key.MaybeItem, pair.Value)) continue;
                try { if (pair.Value.HoldingSlot != null) pair.Value.HoldingSlot.RemoveItem(true); if (!pair.Key.HasItem && pair.Key.CanInsertItem(pair.Value)) pair.Key.InsertItem(pair.Value); }
                catch (Exception exception) { errors.Add("restore: " + exception.Message); }
            }
            try { if (originalSet >= 0 && owner.Body.CurrentHandEquipmentSetIndex != originalSet) owner.Body.CurrentHandEquipmentSetIndex = originalSet; }
            catch (Exception exception) { errors.Add("set: " + exception.Message); }
            foreach (var pair in original)
                if (!ReferenceEquals(pair.Key.MaybeItem, pair.Value)) errors.Add("hand not restored: expected " + (pair.Value?.Blueprint.AssetGuid ?? "empty") + ", found " + (pair.Key.MaybeItem?.Blueprint.AssetGuid ?? "empty"));
            if (owner.Body.CurrentHandEquipmentSetIndex != originalSet) errors.Add("weapon set not restored");
            var restored = new JObject { ["errors"] = errors, ["createdLeft"] = created.Count, ["handsChecked"] = original.Count,
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
        private static readonly string[] Sprint6ShieldTypeGuids = { "26fcc43f7d20374498d2e1643381d345", "d38e8ea23ce653c4582eb3e002555483", "d1b05b901bab9524388ebfa0435902a6", "5f0f4b6e480e7054b8592b5a8b55854a" };
        private static readonly string[] Sprint6ShieldTypeNames = { "buckler", "light", "heavy", "tower" };
        private const int Sprint6ShieldSettleFrames = 6, Sprint6ShieldBandBound = 120;

        private JObject s9Measure;
        private Sprint6ShieldEquipment s9Equipment, s9OwnerEquipment;
        private Dictionary<string, BlueprintItemShield> s9Mundane;
        private List<BlueprintItemShield> s9Enhanced;
        private int s9Enhancement;
        private ModifiableValue.Modifier s9Competing;
        private Buff s9MountSpell, s9RiderSpell;
        private ModifiableValue.Modifier s9RiderSetupShield;
        private int s9BandAttempts, s9BandRulesBefore;
        private ModifiableValue.Modifier s9HostileSetup;

        private void BeginSprint6Shield()
        {
            s9Measure = new JObject { ["contract"] = "native-mounted-shield-v1", ["featGuid"] = HorseCompanionBlueprintService.MountedShieldFeatureGuid,
                ["policy"] = "Amounts are read from the mount's native AC stat, native RuleCalculateAC and the production MountedShieldService.Amount; checks are the production Mounted Combat check with a planned final native total.",
                ["setupDisclosure"] = "Experience; stock shield items created for the run; Greater Shield Focus and the second pair's Mounted Combat and Mounted Shield granted as facts; native Shield-descriptor modifiers as another shield source; a hostile attack-bonus adjustment for the AC band." };
            s6Measure["mountedShield"] = s9Measure;
        }

        private bool s9Drained;

        // Runs once: the Sprint 6 drain is re-entered every frame until its pairs dismount.
        private bool DrainSprint6ShieldFixture()
        {
            if (!Sprint6Shield || s9Drained || s9Measure == null) return true;
            s9Drained = true;
            var restore = new JObject(); s9Measure["drain"] = restore;
            try { s9Competing?.Remove(); s9Competing = null; } catch (Exception exception) { restore["competingError"] = exception.Message; }
            try { s9RiderSetupShield?.Remove(); s9RiderSetupShield = null; } catch (Exception exception) { restore["riderShieldError"] = exception.Message; }
            try { s9HostileSetup?.Remove(); s9HostileSetup = null; } catch (Exception exception) { restore["hostileError"] = exception.Message; }
            try { s9Calibration?.Remove(); s9Calibration = null; } catch (Exception exception) { restore["calibrationError"] = exception.Message; }
            try { s9MountSpell?.Remove(); s9MountSpell = null; } catch (Exception exception) { restore["mountSpellError"] = exception.Message; }
            try { s9RiderSpell?.Remove(); s9RiderSpell = null; } catch (Exception exception) { restore["riderSpellError"] = exception.Message; }
            if (MountedShieldService.Current != null && !MountedShieldService.Current.Enabled) MountedShieldService.Current.Enable();
            if (s9Equipment != null) { restore["rider"] = s9Equipment.Restore(); s9Equipment = null; }
            if (s9OwnerEquipment != null) { restore["owner"] = s9OwnerEquipment.Restore(); s9OwnerEquipment = null; }
            return true;
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
            var row = new JObject {
                ["rider"] = shieldRider.UniqueId, ["mount"] = mount.UniqueId, ["frame"] = Time.frameCount,
                ["amount"] = service?.Amount(shieldRider, mount), ["serviceEnabled"] = service?.Enabled,
                ["facts"] = facts == null ? null : new JObject { ["available"] = facts.Available, ["pair"] = facts.MountedPair, ["ownsFeat"] = facts.OwnsFeat,
                    ["effectiveShield"] = facts.EffectiveShield, ["base"] = facts.ShieldBaseBonus, ["focus"] = facts.ShieldFocus, ["greater"] = facts.GreaterShieldFocus },
                ["shield"] = shield == null ? null : new JObject { ["blueprint"] = shield.Blueprint.AssetGuid, ["name"] = shield.Blueprint.name,
                    ["armorType"] = shield.ArmorComponent?.Blueprint?.Type?.AssetGuid, ["enhancement"] = shield.EnchantmentValue, ["base"] = shield.ArmorComponent?.Blueprint?.ArmorBonus },
                ["ac"] = ac.ModifiedValue, ["touch"] = ac.Touch, ["flatFooted"] = ac.FlatFooted, ["flatFootedTouch"] = ac.FlatFootedTouch,
                ["ruleMelee"] = Rulebook.Trigger(new RuleCalculateAC(shieldRider, mount, AttackType.Melee)).TargetAC,
                ["ruleTouch"] = Rulebook.Trigger(new RuleCalculateAC(shieldRider, mount, AttackType.Touch)).TargetAC,
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
                    ["value"] = modifier.ModValue, ["item"] = modifier.ItemSource != null })),
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
            var shields = ResourcesLibrary.LibraryObject.GetAllBlueprints().OfType<BlueprintItemShield>()
                .Where(shield => shield.ArmorComponent?.Type != null).OrderBy(shield => shield.name, StringComparer.Ordinal).ToArray();
            s9Mundane = new Dictionary<string, BlueprintItemShield>(StringComparer.Ordinal);
            for (var index = 0; index < Sprint6ShieldTypeGuids.Length; index++)
            {
                var guid = Sprint6ShieldTypeGuids[index];
                var mundane = shields.FirstOrDefault(shield => shield.ArmorComponent.Type.AssetGuid == guid && !shield.IsNotable &&
                    shield.ArmorComponent.Enchantments.Count == 0 && (shield.WeaponComponent == null || shield.WeaponComponent.Enchantments.Count == 0));
                if (mundane != null) s9Mundane[Sprint6ShieldTypeNames[index]] = mundane;
            }
            s9Enhanced = shields.Where(shield => shield.ArmorComponent.Type.AssetGuid == Sprint6ShieldTypeGuids[2] && !shield.IsNotable &&
                shield.ArmorComponent.Enchantments.Count == 1 && (shield.WeaponComponent == null || shield.WeaponComponent.Enchantments.Count == 0)).Take(12).ToList();
            s9Measure["shieldBlueprints"] = new JObject { ["mundane"] = new JObject(s9Mundane.Select(pair => new JProperty(pair.Key, pair.Value.AssetGuid + "/" + pair.Value.name))),
                ["enhancedCandidates"] = new JArray(s9Enhanced.Select(shield => shield.AssetGuid + "/" + shield.name)) };
        }

        private BlueprintBuff FindShieldSpellBuff()
        {
            var buffs = ResourcesLibrary.LibraryObject.GetAllBlueprints().OfType<BlueprintBuff>().Where(buff => buff.ComponentsArray != null &&
                buff.ComponentsArray.Length == 1 && buff.ComponentsArray[0] is AddStatBonus bonus && bonus.Stat == StatType.AC &&
                bonus.Descriptor == ModifierDescriptor.Shield && bonus.Value == 4).OrderBy(buff => buff.name, StringComparer.Ordinal).ToArray();
            return buffs.FirstOrDefault(buff => buff.name == "MageShieldBuff") ?? buffs.FirstOrDefault();
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
                    if (levels.Count >= 8) { FailSprint6Row("Mounted Shield was not reached within eight native levels."); return; }
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
            s6Facts["threeFeatSelections"] = new JArray(horseService.MountedCombatSelections.Select(selection => new JObject { ["guid"] = selection.AssetGuid,
                ["combat"] = selection.AllFeatures.Count(feature => ReferenceEquals(feature, horseService.MountedCombatFeature)),
                ["archery"] = selection.AllFeatures.Count(feature => ReferenceEquals(feature, horseService.MountedArcheryFeature)),
                ["shield"] = selection.AllFeatures.Count(feature => ReferenceEquals(feature, MountedShieldFeat)),
                ["kmcTailExact"] = Sprint6KmcTailExact(selection.AllFeatures) }));
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
            if (!prerequisitesOwned && offeredCount < 2) return null;
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
                    // A tower shield the native slot refuses is recorded, not forced (non-proficient riders).
                    try { s9Equipment.Equip(s9Mundane["tower"]); } catch (InvalidOperationException exception) { s6Facts["towerRefused"] = exception.Message; }
                    s6Step = 9; return;
                case 10:
                    if (s6Facts["towerRefused"] == null) ShieldStep("tower");
                    s9Enhancement = 0;
                    s6Step = 11; return;
                // Every enhanced stock heavy shield in turn: the base is the same, so the amount must be too.
                case 12:
                    if (s9Enhancement > 0) ShieldStep("heavyEnhanced" + (s9Enhancement - 1));
                    if (s9Enhancement >= s9Enhanced.Count) { s9Equipment.Equip(s9Mundane["heavy"]); s6Step = 13; return; }
                    s9Equipment.Equip(s9Enhanced[s9Enhancement++]);
                    s6Step = 11; return;
                case 14:
                    ShieldStep("heavyAgain");
                    // Disclosed setup: Greater Shield Focus as a fact, then removed.
                    rider.Descriptor.Progression.Features.AddFeature(horseService.GreaterShieldFocusFeature);
                    s6Step = 15; return;
                case 16: ShieldStep("greater"); rider.Descriptor.Progression.Features.RemoveFact(horseService.GreaterShieldFocusFeature); s6Step = 17; return;
                case 18: ShieldStep("greaterRemoved"); s9Competing = horse.Stats.AC.AddModifier(2, (Kingmaker.Blueprints.GameLogicComponent)null, ModifierDescriptor.Shield); s6Step = 19; return;
                case 20: ShieldStep("competing2"); s9Competing.Remove(); s9Competing = horse.Stats.AC.AddModifier(3, (Kingmaker.Blueprints.GameLogicComponent)null, ModifierDescriptor.Shield); s6Step = 21; return;
                case 22: ShieldStep("competing3"); s9Competing.Remove(); s9Competing = horse.Stats.AC.AddModifier(4, (Kingmaker.Blueprints.GameLogicComponent)null, ModifierDescriptor.Shield); s6Step = 23; return;
                case 24:
                    ShieldStep("competing4"); s9Competing.Remove(); s9Competing = null;
                    var spell = FindShieldSpellBuff();
                    s6Facts["shieldSpell"] = spell == null ? null : spell.AssetGuid + "/" + spell.name;
                    if (spell != null) s9MountSpell = horse.Buffs.AddBuff(spell, horse, TimeSpan.FromMinutes(10));
                    s6Step = 25; return;
                case 26:
                    ShieldStep("mountSpell");
                    s9MountSpell?.Remove(); s9MountSpell = null;
                    var other = (s9Equipment.ShieldSet + 1) % rider.Body.HandsEquipmentSets.Count;
                    s6Facts["otherSet"] = other;
                    rider.Body.CurrentHandEquipmentSetIndex = other;
                    s6Step = 27; return;
                case 28: ShieldStep("otherSet"); rider.Body.CurrentHandEquipmentSetIndex = s9Equipment.ShieldSet; s6Step = 29; return;
                case 30: ShieldStep("setRestored"); s9Equipment.Unequip(); s6Step = 31; return;
                case 32:
                    var unequipped = ShieldStep("unequipped");
                    unequipped["shieldInInventory"] = rider.Inventory.Items.Any(item => item is ItemEntityShield && item.HoldingSlot == null);
                    var riderSpell = FindShieldSpellBuff();
                    if (riderSpell != null) s9RiderSpell = rider.Buffs.AddBuff(riderSpell, rider, TimeSpan.FromMinutes(10));
                    else s9RiderSetupShield = rider.Stats.AC.AddModifier(4, (Kingmaker.Blueprints.GameLogicComponent)null, ModifierDescriptor.Shield);
                    s6Step = 33; return;
                case 34:
                    ShieldStep("riderSpellOnly");
                    s9RiderSpell?.Remove(); s9RiderSpell = null;
                    s9RiderSetupShield?.Remove(); s9RiderSetupShield = null;
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
                    ShieldStep("bothFirst"); ShieldStep("bothSecond", s6Owner, s6OwnerPet);
                    s6Facts["crossFirstToSecondMount"] = ShieldSnapshot(rider, s6OwnerPet); s6Facts["crossSecondToFirstMount"] = ShieldSnapshot(s6Owner, horse);
                    SelectionManager.Instance.SelectUnit(s6Owner.View, true, true, false);
                    s6Frames = 0; s6Step = 3; return;
                case 3:
                    if (++s6Frames < Sprint6ShieldSettleFrames) return;
                    ShieldStep("uiSelectedSecondFirst"); ShieldStep("uiSelectedSecondSecond", s6Owner, s6OwnerPet);
                    SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                    s6Step = 4; return;
                case 4:
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
                    Sprint6SpawnTarget("ms-rt"); s6Step = 1; return;
                case 1:
                    if (!rider.IsInCombat || !horse.IsInCombat || !rider.CombatState.CanActInCombat) return;
                    ShieldStep("combatStart"); ShieldCalibrate("equal"); s6NextPlan = "equal"; s6Step = 2; return;
                // Equality of the shield-modified total fails and the hit stands.
                case 2:
                    if (Sprint6RecordSinceStep() == null) { Sprint6Command(horse); return; }
                    ShieldStep("equal"); s6Step = 3; return;
                case 3: if (!Sprint6RealTimeUsageFree(rider)) return; ShieldCalibrate("success"); s6NextPlan = "success"; s6Step = 4; return;
                case 4:
                    if (Sprint6RecordSinceStep() == null) { Sprint6Command(horse); return; }
                    ShieldStep("success");
                    // Equipment changes during the running use neither refund nor restart it.
                    var before = Sprint6State(rider); s9Equipment.Unequip(); var unequipped = Sprint6State(rider); s9Equipment.Equip(s9Mundane["heavy"]);
                    s6Facts["equipmentDuringUse"] = new JObject { ["before"] = before, ["unequipped"] = unequipped, ["reequipped"] = Sprint6State(rider) };
                    s6NextPlan = "natural"; s6Frames = 0; s6Step = 5; return;
                case 5:
                    if (++s6Frames < Sprint6ShieldSettleFrames) return;
                    if (Sprint6RecordSinceStep() == null) { Sprint6RuleStrike(horse); return; }
                    ShieldStep("spentRefused"); s6Step = 6; return;
                case 6: if (!Sprint6RealTimeUsageFree(rider)) return; Sprint6SetPreference(false, "off"); s6NextPlan = "natural"; s6Step = 7; return;
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
                    s9Competing = horse.Stats.AC.AddModifier(4, (Kingmaker.Blueprints.GameLogicComponent)null, ModifierDescriptor.Shield);
                    ShieldStep("competingBeforeCheck"); ShieldCalibrate("success"); s6NextPlan = "success"; s6Step = 9; return;
                case 9:
                    if (Sprint6RecordSinceStep() == null) { Sprint6RuleStrike(horse); return; }
                    ShieldStep("competingCheck"); s9Competing.Remove(); s9Competing = null; ShieldCalibrate(null);
                    s6NextPlan = "natural"; s6Step = 10; return;
                // A hit that Mounted Shield's AC turns into a miss: no check, no use. The hostile's bonus is aimed so a total
                // inside the shield's band (AC - 3 .. AC - 1) is likely; only actual strikes count against the bound, and the
                // qualifying attack itself is recorded (supplemental review PN4-F10).
                case 10:
                    if (!Sprint6RealTimeUsageFree(rider)) return;
                    var amount = MountedShieldService.Current.Amount(rider, horse);
                    var probe = Rulebook.Trigger(new RuleCalculateAC(target, horse, AttackType.Melee)).TargetAC;
                    var current = ShieldHostileBonus();
                    var adjustment = probe - 12 - current;
                    s9HostileSetup = target.Stats.AdditionalAttackBonus.AddModifier(adjustment, (Kingmaker.Blueprints.GameLogicComponent)null, ModifierDescriptor.UntypedStackable);
                    s6Facts["band"] = new JObject { ["amount"] = amount, ["ac"] = probe, ["attackBonusBefore"] = current, ["adjustment"] = adjustment };
                    s9BandAttempts = 0; s9BandRulesBefore = s6Rules.Attacks.Count; s6Step = 11; return;
                case 11:
                    var miss = ShieldBandMiss();
                    if (miss != null || s9BandAttempts >= Sprint6ShieldBandBound)
                    {
                        s6Facts["bandMiss"] = miss?.DeepClone(); s6Facts["bandAttempts"] = s9BandAttempts;
                        s9HostileSetup?.Remove(); s9HostileSetup = null;
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
            var amount = s6Facts["band"]?.Value<int>("amount") ?? 0;
            for (var index = s9BandRulesBefore; index < s6Rules.Attacks.Count; index++)
            {
                var row = (JObject)s6Rules.Attacks[index];
                if ((string)row["target"] != horse.UniqueId || (string)row["attacker"] != target.UniqueId || row.Value<bool>("isHitAtEvent")) continue;
                var total = row.Value<int>("d20") + row.Value<int>("bonus");
                if (row.Value<int>("d20") > 1 && total < row.Value<int>("ac") && total >= row.Value<int>("ac") - amount) return row;
            }
            return null;
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
        private ModifiableValue.Modifier s9Calibration;
        private void ShieldCalibrate(string plan)
        {
            s9Calibration?.Remove(); s9Calibration = null;
            if (plan == null) return;
            var checkBase = rider.Stats.SkillMobility.ModifiedValue + MountedShieldService.Current.Amount(rider, horse);
            var wanted = plan == "equal" ? checkBase : checkBase - 1;
            var current = ShieldHostileBonus();
            s9Calibration = target.Stats.AdditionalAttackBonus.AddModifier(wanted - current, (Kingmaker.Blueprints.GameLogicComponent)null, ModifierDescriptor.UntypedStackable);
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
                    Sprint6SpawnTarget("ms-tb"); s6Step = 1; return;
                // Own turn: success with the shield's addition, paid by this turn's Swift action.
                case 1:
                    if (!rider.IsInCombat || !horse.IsInCombat) return;
                    if (WaitForNativeCastingPrincipal(true, turn?.Unit == rider, rider.CombatState.CanActInCombat, () => EndCastingNativeTurn(turn))) return;
                    if (!rider.HasSwiftAction() || !Sprint6RiderTurn(turn)) return;
                    s6Turn = turn; ShieldStep("ownTurnBefore"); ShieldCalibrate("success"); s6NextPlan = "success"; s6Step = 2; return;
                case 2:
                    if (Sprint6RecordSinceStep() == null) { Sprint6OpportunityStrike(horse); return; }
                    ShieldStep("ownTurnNegated"); s6NextPlan = "natural"; s6Step = 3; return;
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
                    ShieldCalibrate("equal"); s6NextPlan = "equal"; s6Step = 6; return;
                case 6:
                    if (Sprint6RecordSinceStep() != null) { ShieldStep("offTurnEqual"); ShieldCalibrate(null); s6Frames = 0; s6Step = 7; return; }
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
