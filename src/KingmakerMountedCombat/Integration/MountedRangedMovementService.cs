using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints.Facts;
using TurnBased.Controllers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Logging;

namespace KingmakerMountedCombat.Integration
{
    // One attack roll's mounted ranged-movement reading, kept for diagnostics and native evidence.
    internal sealed class MountedRangedMovementRecord
    {
        internal string Rider, Mount, Weapon, Allocation;
        internal MountedRangedMovementOutcome Outcome;
        internal int Contribution, Round;
        internal float NormalTravelSeconds;
        internal bool OwnsMountedArchery;
        internal RuleCalculateAttackBonus Rule;
    }

    // The mounted ranged-motion rule (Domain MountedRangedMovementPolicy). It applies to every active mounted rider, with
    // or without Mounted Combat or Mounted Archery: -4 after the mount's second Move of normal travel in its current
    // allocation, -2 if the rider owns Mounted Archery, in turn-based combat only.
    //
    // The seam: the native RuleAttackRoll.OnTrigger triggers RuleCalculateAC, then exactly one RuleCalculateAttackBonus for
    // the roll, whose OnTrigger triggers RuleCalculateAttackBonusWithoutTarget; a critical confirmation reuses that
    // AttackBonus and an extra Manyshot projectile reuses the whole AttackRoll. A global about-to-trigger handler on
    // RuleCalculateAttackBonus whose enclosing event is that roll (AC computed, attack-bonus rule not yet stored) adds one
    // BonusSource through the native AddBonus. It is part of that rule only: no stat modifier, no aggregate
    // AttackBonusPenalty, nothing to clean up, and UI previews (the target-free rule and Power Attack's AI estimate,
    // which runs before the roll's AC) never see it.
    internal sealed class MountedRangedMovementService : IGlobalRulebookHandler<RuleCalculateAttackBonus>, IDisposable
    {
        private readonly HorseCompanionBlueprintService blueprints;
        private readonly MountedWorldService world;
        private readonly IModLogger logger;
        private readonly HashSet<string> reportedFailures = new HashSet<string>(StringComparer.Ordinal);
        private bool disposed, withdrawalReported;

        internal MountedRangedMovementService(HorseCompanionBlueprintService blueprints, MountedWorldService world, IModLogger logger)
        {
            this.blueprints = blueprints ?? throw new ArgumentNullException(nameof(blueprints));
            this.world = world ?? throw new ArgumentNullException(nameof(world));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            EventBus.Subscribe(this);
            Current = this;
        }

        internal static MountedRangedMovementService Current { get; private set; }
        internal bool Enabled { get; set; }
        internal int AppliedCount { get; private set; }
        internal MountedRangedMovementRecord LastRecord { get; private set; }
        internal event Action<MountedRangedMovementRecord> Observed;

        public void OnEventAboutToTrigger(RuleCalculateAttackBonus evt)
        {
            if (disposed || !Enabled || evt == null) return;
            try { Apply(evt); }
            catch (Exception exception)
            {
                // The rulebook would swallow this; report each distinct fault once and leave the roll native.
                if (reportedFailures.Add(exception.GetType().FullName + ":" + exception.Message))
                    logger.Exception("Mounted ranged movement attack bonus", exception);
            }
        }

        public void OnEventDidTrigger(RuleCalculateAttackBonus evt) { }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (ReferenceEquals(Current, this)) Current = null;
            EventBus.Unsubscribe(this);
        }

        private void Apply(RuleCalculateAttackBonus evt)
        {
            var roll = Rulebook.CurrentContext?.PreviousEvent as RuleAttackRoll;
            // Only the roll's own calculation: same attacker, target and weapon, after its AC and before its stored result.
            if (roll == null || roll.ACRule == null || roll.AttackBonusRule != null || !ReferenceEquals(roll.Initiator, evt.Initiator) ||
                !ReferenceEquals(roll.Target, evt.Target) || !ReferenceEquals(roll.Weapon, evt.Weapon)) return;
            var rider = evt.Initiator;
            var pair = world.ForActor(rider)?.Relationship;
            var activeRider = pair != null && pair.State == RelationshipState.Mounted && ReferenceEquals(pair.Rider, rider) && pair.Mount != null;
            if (!activeRider) return;
            var weapon = evt.Weapon?.Blueprint;
            var qualifying = weapon != null && MountedRangedMovementPolicy.QualifyingRangedWeaponAttack(
                roll.RuleAttackWithWeapon != null, weapon.IsRanged, weapon.IsNatural, IsSpellProxyCategory(weapon.Category));
            if (!qualifying) return;

            var mount = pair.Mount;
            var turnBased = CombatController.IsInTurnBasedCombat();
            float travel = 0f;
            string allocation = null;
            var current = turnBased && world.UnifiedTurn.TryReadCurrentMountTravel(mount, rider, out travel, out allocation);
            var owns = blueprints.MountedArcheryFeature != null && rider.Descriptor.Progression.Features.HasFact(blueprints.MountedArcheryFeature);
            var facts = new MountedRangedMovementFacts
            {
                Available = blueprints.MountedArcheryReady, TurnBased = turnBased, ActiveRider = true, QualifyingAttack = true,
                CurrentAllocation = current, NormalTravelSeconds = travel, OwnsMountedArchery = owns
            };
            if (!facts.Available && blueprints.MountedArcheryFeature != null && !withdrawalReported)
            {
                // Fail-safe, but never silent: a leased selection drifted outside KMC, or the label sources are missing.
                withdrawalReported = true;
                logger.Warning("Mounted ranged movement rule withdrawn: Mounted Archery is registered but not offered by every " +
                    "KMC combat-feat selection (or its log sources are missing); no mounted ranged penalty or reduction applies. " +
                    "failure=" + (blueprints.MountedArcheryFailure ?? "none") + ".");
            }
            int contribution;
            var outcome = MountedRangedMovementPolicy.Evaluate(facts, out contribution);
            // Real time and an unavailable rule are unchanged native attacks: no record, no line.
            if (outcome == MountedRangedMovementOutcome.NotTurnBased || outcome == MountedRangedMovementOutcome.Unavailable) return;
            Fact source = null;
            if (contribution != 0)
            {
                source = owns ? blueprints.MountedRangedMovementReducedSource : blueprints.MountedRangedMovementSource;
                // A rule is triggered once; this refuses a second contribution should any path deliver it twice.
                if (evt.BonusSources != null && evt.BonusSources.Any(entry => ReferenceEquals(entry.Source, blueprints.MountedRangedMovementSource) ||
                    ReferenceEquals(entry.Source, blueprints.MountedRangedMovementReducedSource))) return;
                evt.AddBonus(contribution, source);
                AppliedCount++;
            }
            var record = new MountedRangedMovementRecord
            {
                Rider = rider.UniqueId, Mount = mount.UniqueId, Weapon = weapon.name, Allocation = allocation, Outcome = outcome,
                Contribution = contribution, NormalTravelSeconds = travel, OwnsMountedArchery = owns, Rule = evt,
                Round = Game.Instance.TurnBasedCombatController?.RoundNumber ?? 0
            };
            LastRecord = record;
            logger.Info("Mounted ranged movement: rider=" + record.Rider + ";mount=" + record.Mount + ";weapon=" + record.Weapon +
                ";mode=" + (turnBased ? "TB" : "RT") + ";allocation=" + (allocation ?? "none") + ";normalTravel=" + travel.ToString("R") +
                ";threshold=" + MountedRangedMovementPolicy.SecondMoveThresholdSeconds.ToString("R") + ";mountedArchery=" + owns +
                ";outcome=" + outcome + ";contribution=" + contribution + ";round=" + record.Round + ".");
            Observed?.Invoke(record);
        }

        // Proxy weapons that abilities create for rays, touch spells, kinetic blasts and alchemist bombs.
        private static bool IsSpellProxyCategory(WeaponCategory category) =>
            category == WeaponCategory.Ray || category == WeaponCategory.Touch || category == WeaponCategory.KineticBlast || category == WeaponCategory.Bomb;
    }
}
