using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Logging;

namespace KingmakerMountedCombat.Integration
{
    // Mounted Shield's two benefits, from one calculation (MountedShieldPolicy.Amount over ReadFacts):
    //
    // 1. The current mount's AC. A native ModifiableValue modifier on the mount's AC stat with the Shield descriptor, sourced
    //    by the rider's own Mounted Shield fact. The native stat does the rest: Shield is not a stacking descriptor, so the
    //    highest shield bonus counts and an untyped stack never occurs; ModifiableValueArmorClass excludes a positive shield
    //    modifier from touch AC and keeps it in flat-footed AC; the character sheet and AC tooltip read the same stat and
    //    name the source fact. Ownership: MountedShieldContributionLedger records the exact modifier per rider and writes
    //    only on change (no per-frame churn). The rider's pair is also reconciled at the moment native AC is consumed (the
    //    mount's RuleCalculateAC, before it reads the stat), so no attack sees the previous equipment's value; the sheet
    //    follows within a frame. (Only that rulebook handler is subscribed: a plain EventBus subscriber would change the
    //    reviewed global-subscriber inventory, Diagnostics/Chunk7ReplaySubscriberContract.) Release paths: the pair's dismount, the service's
    //    disable/dispose/failure cleanup, the rider fact's own turn-off (MountedShieldFeatureLogic: feat removal, respec,
    //    unit teardown, and the native save path, which turns the party and area off before serializing them), and the next
    //    reconcile when the shield, the feat or the pair changed. Every removal goes through the owning stat by reference
    //    and is verified, so a write the engine left half-done (its stat-change callback threw before the handle came back)
    //    is still found and removed. A modifier that still reached a save is dropped by the native load cleanup
    //    (CharacterStats.CleanupModifiers removes a modifier whose source fact the mount does not own).
    // 2. The Mounted Combat check. MountedCombatDefenseService adds the same amount to its own RuleSkillCheck's native Bonus
    //    before Rulebook.Trigger (ContributeToCheck). It is part of that one rule: no rider stat changes.
    internal sealed class MountedShieldService : IGlobalRulebookHandler<RuleCalculateAC>, IDisposable
    {
        // The identity of the modifiers this service writes (with the source fact's blueprint).
        internal const string MountSourceComponent = "KMC_MountedShield_MountShieldBonus";
        internal const string CheckSourceComponent = "KMC_MountedShield_MountedCombatCheck";

        private readonly HorseCompanionBlueprintService blueprints;
        private readonly MountedWorldService world;
        private readonly Func<bool> writesAllowed;
        private readonly Func<bool> removalsAllowed;
        private readonly IModLogger logger;
        private readonly MountedShieldContributionLedger<UnitEntityData, ModifiableValue.Modifier> ledger;
        private readonly HashSet<string> reportedFailures = new HashSet<string>(StringComparer.Ordinal);
        private bool enabled, disposed;

        // writesAllowed: the ordinary update gates are open (services enabled, no save, area transition, restoration, archive
        // worker or owned load). removalsAllowed: no archive worker serializes the live graphs and no owned load replaces the
        // world. Both gate the event-time reconciles; the per-frame passes are placed behind the same gates by the root.
        internal MountedShieldService(HorseCompanionBlueprintService blueprints, MountedWorldService world, Func<bool> writesAllowed,
            Func<bool> removalsAllowed, IModLogger logger)
        {
            this.blueprints = blueprints ?? throw new ArgumentNullException(nameof(blueprints));
            this.world = world ?? throw new ArgumentNullException(nameof(world));
            this.writesAllowed = writesAllowed ?? throw new ArgumentNullException(nameof(writesAllowed));
            this.removalsAllowed = removalsAllowed ?? throw new ArgumentNullException(nameof(removalsAllowed));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            ledger = new MountedShieldContributionLedger<UnitEntityData, ModifiableValue.Modifier>(ApplyToMount, IsAppliedToMount, RemoveFromMount);
            world.PairDismounting += HandlePairDismounting;
            EventBus.Subscribe(this);
            Current = this;
        }

        internal static MountedShieldService Current { get; private set; }
        internal int OwnedCount => ledger.Count;
        internal int AppliedCount { get; private set; }
        internal int RemovedCount { get; private set; }
        internal int CheckContributionCount { get; private set; }

        internal bool Enabled => enabled;

        // Enabling writes nothing until the next ordinary reconcile.
        internal void Enable()
        {
            if (!disposed) enabled = true;
        }

        // Stops new writes and releases every contribution (recorded and swept). False, with the service enabled again so its
        // record keeps ownership and the next pass retries, when anything with this feature's identity remains on a loaded
        // unit; the caller then refuses its disable, as it does for other retained mounted residue.
        internal bool Disable(string reason)
        {
            if (disposed) return true;
            enabled = false;
            if (ReleaseAll(reason)) return true;
            enabled = true;
            return false;
        }

        internal bool TryGetContribution(UnitEntityData rider, out UnitEntityData mount, out int amount) => ledger.TryGet(rider, out mount, out amount);

        // The single source of truth for both benefits: what this exact rider supplies to this exact mount now.
        internal int Amount(UnitEntityData rider, UnitEntityData mount) => MountedShieldPolicy.Amount(ReadFacts(rider, mount));

        // Every term read from the live native rider. Availability (the publication's full comparison) is read last and only
        // when every other term holds, so ordinary unmounted or featless frames never pay for it.
        internal MountedShieldFacts ReadFacts(UnitEntityData rider, UnitEntityData mount)
        {
            var facts = new MountedShieldFacts();
            if (!enabled || disposed || rider?.Descriptor == null || mount?.Descriptor == null || blueprints.MountedShieldFeature == null) return facts;
            var pair = world.ForActor(rider)?.Relationship;
            facts.MountedPair = pair != null && pair.State == RelationshipState.Mounted && ReferenceEquals(pair.Rider, rider) &&
                ReferenceEquals(pair.Mount, mount) && Live(rider) && Live(mount);
            var fact = OwnedFeature(rider);
            facts.OwnsFeat = fact != null && fact.Active && fact.IsTurnedOn;
            var shield = EffectiveShield(rider);
            facts.EffectiveShield = shield != null;
            facts.ShieldBaseBonus = shield == null ? 0 : ShieldBaseBonus(shield);
            facts.ShieldFocus = HasFeature(rider, blueprints.ShieldFocusFeature);
            facts.GreaterShieldFocus = HasFeature(rider, blueprints.GreaterShieldFocusFeature);
            facts.Available = facts.MountedPair && facts.OwnsFeat && facts.EffectiveShield && blueprints.IsMountedShieldAvailable();
            return facts;
        }

        // The physical shield whose own native shield AC applies now: an ItemEntityShield in the off-hand of the current
        // weapon set, in an active slot, whose armor component's native Shield modifier is on the rider's AC. That is exactly
        // the engine's effective availability: an inactive set or a disabled slot unequips the item natively
        // (ItemSlot.OnActiveChanged -> OnWillUnequip removes the armor component's modifiers), and re-activation restores it.
        // A shield in the inventory, a Shield spell or any other Shield-descriptor effect is not a physical shield here.
        internal static ItemEntityShield EffectiveShield(UnitEntityData rider)
        {
            var slot = rider?.Body?.SecondaryHand;
            var shield = slot?.MaybeShield;
            var armor = shield?.ArmorComponent;
            if (armor == null || !slot.Active) return null;
            foreach (var modifier in rider.Stats.AC.Modifiers)
                if (modifier.ModDescriptor == ModifierDescriptor.Shield && ReferenceEquals(modifier.ItemSource, armor)) return shield;
            return null;
        }

        // Native base data: the armor type's own bonus (BlueprintItemArmor.ArmorBonus => its BlueprintArmorType.ArmorBonus),
        // the same value ItemEntityArmor.RecalculateStats adds before it adds GameHelper.GetItemEnhancementBonus. Item and
        // temporary enhancement, the shield weapon's enhancement and every other item power are therefore never read.
        internal static int ShieldBaseBonus(ItemEntityShield shield) => shield.ArmorComponent.Blueprint.ArmorBonus;

        // Added to the Mounted Combat check's own rule before it is triggered; returns the amount added (0 when none).
        internal int ContributeToCheck(RuleSkillCheck check, UnitEntityData rider, UnitEntityData mount)
        {
            if (check == null) throw new ArgumentNullException(nameof(check));
            var amount = Amount(rider, mount);
            if (amount <= 0) return 0;
            AddCheckBonus(check, amount, OwnedFeature(rider));
            CheckContributionCount++;
            return amount;
        }

        // The native writes and removal of each benefit. scripts/Test-MountedShieldNativeContract.ps1 runs them on genuine
        // engine objects (a ModifiableValue and a RuleSkillCheck) to observe the native stacking, roll arithmetic and the
        // recovery of a half-done write.
        //
        // Mount AC: the Shield descriptor, named by the rider's own fact, so the native stat applies shield semantics.
        internal static ModifiableValue.Modifier AddMountShieldBonus(ModifiableValue ac, int amount, Fact source) =>
            ac.AddModifier(amount, source, MountSourceComponent, ModifierDescriptor.Shield);

        // The check: untyped and stacking, so the full amount always reaches this one check whatever else the rule carries.
        internal static ModifiableValue.Modifier AddCheckBonus(RuleSkillCheck check, int amount, Fact source) =>
            check.Bonus.AddModifier(amount, source, CheckSourceComponent, ModifierDescriptor.UntypedStackable);

        // Whether the stat holds this exact modifier object. Membership, not Modifier.AppliedTo: the engine adds the modifier
        // to the stat before it sets AppliedTo, so a write whose stat-change callback threw is in the stat without it.
        internal static bool Holds(ModifiableValue stat, ModifiableValue.Modifier modifier) =>
            stat != null && modifier != null && stat.Modifiers.Any(value => ReferenceEquals(value, modifier));

        // Removes this exact modifier through the stat that holds it and verifies it is gone. A stat-change callback that
        // throws after the removal is reported through onCallbackFault and does not leave the modifier behind; anything that
        // does leave it is an error.
        internal static void RemoveExact(ModifiableValue stat, ModifiableValue.Modifier modifier, Action<Exception> onCallbackFault)
        {
            if (!Holds(stat, modifier)) return;
            try { stat.RemoveModifier(modifier); }
            catch (Exception exception)
            {
                if (Holds(stat, modifier)) throw;
                onCallbackFault?.Invoke(exception);
            }
            if (Holds(stat, modifier)) throw new InvalidOperationException("The native stat kept a Mounted Shield modifier after its removal.");
        }

        // Every modifier with this feature's identity on the stat: the mount source component, and a source fact that is
        // absent (an unresolved reference) or of the Mounted Shield blueprint.
        internal static ModifiableValue.Modifier[] IdentityModifiers(ModifiableValue stat, string featureGuid) =>
            stat == null ? new ModifiableValue.Modifier[0] : stat.Modifiers.Where(modifier =>
                string.Equals(modifier.SourceComponent, MountSourceComponent, StringComparison.Ordinal) &&
                (modifier.Source == null || string.Equals(modifier.Source.Blueprint?.AssetGuid, featureGuid, StringComparison.Ordinal))).ToArray();

        // Ordinary update. ordinary false: only stale contributions are removed (the update gates are closed); true: the
        // wanted contribution of every mounted pair is also written. Each pair costs a few native reads per frame; a native
        // write happens only when something changed.
        internal void Update(bool ordinary)
        {
            if (disposed || !enabled) return;
            try
            {
                ledger.Reconcile(Desires(), ordinary);
            }
            catch (Exception exception) { ReportOnce("Mounted Shield reconcile", exception); }
        }

        private List<MountedShieldDesire<UnitEntityData>> Desires()
        {
            var desires = new List<MountedShieldDesire<UnitEntityData>>();
            if (blueprints.MountedShieldFeature == null) return desires;
            foreach (var pair in world.Pairs)
            {
                var relationship = pair.Relationship;
                if (relationship == null || relationship.State != RelationshipState.Mounted) continue;
                var rider = relationship.Rider;
                var mount = relationship.Mount;
                if (rider == null || mount == null) continue;
                var amount = Amount(rider, mount);
                if (amount > 0) desires.Add(new MountedShieldDesire<UnitEntityData>(rider, mount, amount));
            }
            return desires;
        }

        // One rider's pair, now: its record is kept, replaced or removed, and nothing else is touched.
        private void ReconcileRider(UnitEntityData rider, string reason)
        {
            if (disposed || !enabled || rider == null || blueprints.MountedShieldFeature == null || !removalsAllowed()) return;
            try
            {
                MountedShieldDesire<UnitEntityData> desire = null;
                var relationship = world.ForActor(rider)?.Relationship;
                if (relationship != null && relationship.State == RelationshipState.Mounted && ReferenceEquals(relationship.Rider, rider) && relationship.Mount != null)
                {
                    var amount = Amount(rider, relationship.Mount);
                    if (amount > 0) desire = new MountedShieldDesire<UnitEntityData>(rider, relationship.Mount, amount);
                }
                if (desire == null && !ledger.TryGet(rider, out _, out _)) return;
                if (ledger.ReconcileRider(rider, desire, writesAllowed()) > 0) Log("reconciled for " + rider.UniqueId + " (" + reason + ")");
            }
            catch (Exception exception) { ReportOnce("Mounted Shield " + reason + " reconcile", exception); }
        }

        // The mount's AC is about to be read by an attack: its rider's contribution is made current first.
        public void OnEventAboutToTrigger(RuleCalculateAC evt)
        {
            var target = evt?.Target;
            if (disposed || !enabled || target == null) return;
            var pair = world.Pairs.FirstOrDefault(value => value.Relationship?.State == RelationshipState.Mounted && ReferenceEquals(value.Relationship.Mount, target));
            if (pair != null) ReconcileRider(pair.Relationship.Rider, "attack AC");
        }

        public void OnEventDidTrigger(RuleCalculateAC evt) { }

        // The rider fact turned off (or the rider left): its contribution goes now, outside every update gate.
        internal void ReleaseRider(UnitEntityData rider, string reason)
        {
            if (disposed || rider == null) return;
            try { if (ledger.Release(rider)) Log("released for " + rider.UniqueId + " (" + reason + ")"); }
            catch (Exception exception) { ReportOnce("Mounted Shield release", exception); }
        }

        private void HandlePairDismounting(GameMountedRelationshipService relationship, CleanupTrigger trigger)
        {
            if (disposed || relationship == null) return;
            try
            {
                var released = relationship.Rider != null && ledger.Release(relationship.Rider);
                if (!released && relationship.Mount != null) released = ledger.ReleaseMount(relationship.Mount);
                if (released) Log("released at dismount (" + trigger + ")");
            }
            catch (Exception exception) { ReportOnce("Mounted Shield dismount release", exception); }
        }

        // Every recorded contribution, then an exhaustive sweep of every loaded unit for a modifier with this feature's
        // identity that the record does not hold (for example one a half-done write left). Only this feature's modifiers go.
        // True only when the record is empty and no loaded unit still carries such a modifier.
        internal bool ReleaseAll(string reason)
        {
            var count = 0;
            try { count = ledger.ReleaseAll(); }
            catch (Exception exception) { ReportOnce("Mounted Shield release-all", exception); }
            var swept = 0;
            var clean = false;
            try
            {
                swept = LoadedUnits().Sum(RemoveUnrecorded);
                clean = ledger.Count == 0 && !LoadedUnits().Any(unit => IdentityModifiers(unit.Stats?.AC, HorseCompanionBlueprintService.MountedShieldFeatureGuid).Length != 0);
            }
            catch (Exception exception) { ReportOnce("Mounted Shield sweep", exception); }
            if (count + swept > 0) Log("released " + count + " recorded and " + swept + " unrecorded contribution(s) (" + reason + ")");
            if (!clean) logger.Error("Mounted Shield: a mount still carries a Mounted Shield contribution after " + reason + "; it is retained for a retry.");
            return clean;
        }

        public void Dispose()
        {
            if (disposed) return;
            enabled = false;
            if (!ReleaseAll("disposed"))
            {
                enabled = true;
                throw new InvalidOperationException("Mounted Shield cannot dispose while a mount still carries its contribution.");
            }
            disposed = true;
            EventBus.Unsubscribe(this);
            world.PairDismounting -= HandlePairDismounting;
            if (ReferenceEquals(Current, this)) Current = null;
        }

        private ModifiableValue.Modifier ApplyToMount(UnitEntityData rider, UnitEntityData mount, int amount)
        {
            var fact = OwnedFeature(rider);
            if (fact == null) return null;
            // At most one Mounted Shield bonus per mount: the ledger holds no record for this mount here, so any modifier
            // with this identity on it is unrecorded residue.
            RemoveUnrecorded(mount);
            ModifiableValue.Modifier modifier;
            try { modifier = AddMountShieldBonus(mount.Stats.AC, amount, fact); }
            catch (Exception exception)
            {
                // The engine adds the modifier before its stat-change callbacks run; if one threw, the modifier is in the stat
                // with no handle returned. Recover it by identity now, so no unrecorded value stays on the mount.
                ReportOnce("Mounted Shield mount write", exception);
                RemoveUnrecorded(mount);
                return null;
            }
            AppliedCount++;
            Log("+" + amount + " shield bonus from " + rider.UniqueId + " on mount " + mount.UniqueId + " AC (now " + mount.Stats.AC.ModifiedValue + ")");
            return modifier;
        }

        private static bool IsAppliedToMount(UnitEntityData mount, ModifiableValue.Modifier modifier) => Holds(mount?.Stats?.AC, modifier);

        private void RemoveFromMount(UnitEntityData mount, ModifiableValue.Modifier modifier)
        {
            RemoveExact(mount.Stats.AC, modifier, exception => ReportOnce("Mounted Shield stat callback", exception));
            RemovedCount++;
            Log("removed +" + modifier.ModValue + " from mount " + mount.UniqueId + " AC (now " + mount.Stats.AC.ModifiedValue + ")");
        }

        // Removes every modifier with this feature's identity on the unit that the ledger does not record; verified.
        private int RemoveUnrecorded(UnitEntityData unit)
        {
            var ac = unit?.Stats?.AC;
            var stray = IdentityModifiers(ac, HorseCompanionBlueprintService.MountedShieldFeatureGuid).Where(modifier => !ledger.Owns(modifier)).ToArray();
            foreach (var modifier in stray) RemoveExact(ac, modifier, exception => ReportOnce("Mounted Shield stat callback", exception));
            return stray.Length;
        }

        private static IEnumerable<UnitEntityData> LoadedUnits()
        {
            var game = Game.Instance;
            if (game?.State?.Units == null) return Enumerable.Empty<UnitEntityData>();
            return game.State.Units.Concat(game.Player?.AllCharacters ?? Enumerable.Empty<UnitEntityData>()).Where(unit => unit != null).Distinct().ToArray();
        }

        private Fact OwnedFeature(UnitEntityData rider)
        {
            var feature = blueprints.MountedShieldFeature;
            return feature == null ? null : rider?.Descriptor?.Progression?.Features?.GetFact(feature);
        }

        private static bool HasFeature(UnitEntityData unit, Kingmaker.Blueprints.Classes.BlueprintFeature feature) =>
            feature != null && unit.Descriptor.Progression.Features.HasFact(feature);

        private static bool Live(UnitEntityData unit) => unit.IsInGame && !unit.Descriptor.State.IsDead;

        private void Log(string text) => logger.Info("Mounted Shield: " + text + ".");

        private void ReportOnce(string context, Exception exception)
        {
            if (reportedFailures.Add(context + ":" + exception.GetType().FullName + ":" + exception.Message)) logger.Exception(context, exception);
        }
    }

    // The Mounted Shield feature's own logic. Turning the fact off releases the mount contribution its rider owns: feat
    // removal and respec, the unit leaving the game, and the native save path, which turns the party and the loaded area
    // off before it serializes them, so the contribution is never written into a save. The next ordinary reconcile after
    // the fact turns on again restores exactly one contribution when the rider is still eligible.
    internal sealed class MountedShieldFeatureLogic : OwnedGameLogicComponent<UnitDescriptor>
    {
        public override void OnTurnOff()
        {
            base.OnTurnOff();
            MountedShieldService.Current?.ReleaseRider(Owner?.Unit, "Mounted Shield fact turned off");
        }
    }
}
