using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums.Damage;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UI.Log;
using Kingmaker.UnitLogic;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Logging;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Integration
{
    // One fall's distinguishing values, captured at the native boundary. Exact unit identities, never display names.
    internal sealed class MountedRiderFallRecord
    {
        internal long Sequence, Generation;
        internal string Cause, RiderId, MountId, RiderName, MountName, Mode, Inability, Outcome, LogText, AbandonedReason, ProneRefusal;
        internal int Round, CaptureFrame, ResolveFrame;
        internal bool Resolved, Attempted, Forced, Passed, ThresholdAgrees, DamageDealt, ProneImposed, RiderProneBefore, DamageImmune;
        internal int CheckD20, CheckStat, CheckBonus, CheckResult, CheckDc, CheckDcAdjustment;
        internal int DamageRolled, DamageBeforeDifficulty, DamageApplied, RiderDamageBefore, RiderDamageAfter;
        internal float StandardBefore, StandardAfter, MoveBefore, MoveAfter, SwiftBefore, SwiftAfter;
    }

    // Resolves D5 through native rules: RuleSkillCheck (Mobility, the native difficulty DC adjustment included),
    // RuleDealDamage (1d6 bludgeoning, so native reduction, immunity and temporary hit points apply) and the native
    // knock-off rule for prone. It writes no action, cooldown, reaction, feat or initiative state.
    internal sealed class MountedRiderFallService
    {
        private const int RecordCapacity = 32;
        private readonly MountedRiderFallLedger<GameMountedRelationshipService> ledger = new MountedRiderFallLedger<GameMountedRelationshipService>();
        private readonly Dictionary<long, Captured> captured = new Dictionary<long, Captured>();
        private readonly List<MountedRiderFallRecord> records = new List<MountedRiderFallRecord>();
        private readonly Func<bool> restorationPending;
        private readonly IModLogger logger;
        private bool resolving;

        internal MountedRiderFallService(Func<bool> restorationPending, IModLogger logger)
        {
            this.restorationPending = restorationPending ?? throw new ArgumentNullException(nameof(restorationPending));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        internal long BegunCount => ledger.BegunCount;
        internal long ResolvedCount { get; private set; }
        internal long AbandonedCount { get; private set; }
        internal int PendingCount => ledger.PendingCount;
        internal MountedRiderFallRefusal LastRefusal { get; private set; }
        internal IReadOnlyList<MountedRiderFallRecord> Records => records.ToArray();
        // Test environment only: replaces the check's d20 for one labelled success or failure row. Null in ordinary play.
        internal Func<MountedRiderFallRecord, int?> ScenarioD20 { get; set; }
        internal event Action<MountedRiderFallRecord> Observed;

        private sealed class Captured
        {
            internal MountedRiderFallEvent<GameMountedRelationshipService> Event;
            internal UnitEntityData Rider, Mount;
            internal MountedRiderFallRecord Record;
        }

        // Called before the relationship cleanup for the same native callback: the rider and mount are still the pair's.
        internal void Capture(GameMountedRelationshipService owner, UnitEntityData actor, MountedRiderFallCause cause)
        {
            if (owner == null || actor == null) return;
            var rider = owner.Rider;
            var mount = owner.Mount;
            var facts = new MountedRiderFallFacts
            {
                Cause = cause,
                ExactMountOfMountedPair = owner.State == RelationshipState.Mounted && mount != null && ReferenceEquals(mount, actor) && rider != null,
                RiderAlive = rider?.Descriptor != null && !rider.Descriptor.State.IsDead,
                Gameplay = !restorationPending() && LoadingProcess.Instance?.IsLoadingInProcess != true
            };
            MountedRiderFallEvent<GameMountedRelationshipService> fall;
            LastRefusal = ledger.Begin(owner, owner.MountedPairGeneration, facts, rider?.UniqueId, mount?.UniqueId, out fall);
            if (fall == null) return;
            var record = new MountedRiderFallRecord
            {
                Sequence = fall.Sequence, Generation = fall.Generation, Cause = cause.ToString(), RiderId = rider.UniqueId, MountId = mount.UniqueId,
                RiderName = rider.CharacterName, MountName = mount.CharacterName, Mode = CombatController.IsInTurnBasedCombat() ? "TB" : "RT",
                Round = Game.Instance?.TurnBasedCombatController?.RoundNumber ?? 0, CaptureFrame = Time.frameCount
            };
            captured[fall.Sequence] = new Captured { Event = fall, Rider = rider, Mount = mount, Record = record };
            logger.Info("Mounted rider fall captured: sequence=" + fall.Sequence + ";cause=" + cause + ";riderId=" + record.RiderId +
                ";mountId=" + record.MountId + ";generation=" + fall.Generation + ".");
        }

        // Called after the relationship cleanup: every pending fall of this pair whose rider is now unseated resolves once.
        internal void Complete(GameMountedRelationshipService owner)
        {
            if (owner == null || resolving) return;
            foreach (var fall in ledger.TakeUnseated(owner, Unseated)) Resolve(fall);
        }

        // A fall whose detachment completed through a later retry, or whose seat moved on without unseating its rider.
        internal void Update()
        {
            if (resolving || ledger.PendingCount == 0) return;
            foreach (var fall in ledger.TakeUnseated(null, Unseated)) Resolve(fall);
            foreach (var fall in ledger.TakeAbandoned(Abandoned)) Abandon(fall);
        }

        private static bool Unseated(MountedRiderFallEvent<GameMountedRelationshipService> fall) =>
            fall.Pair.State == RelationshipState.Unmounted && fall.Pair.MountedPairGeneration == fall.Generation;

        private static string Abandoned(MountedRiderFallEvent<GameMountedRelationshipService> fall)
        {
            if (fall.Pair.State == RelationshipState.Disposed) return "relationship disposed before the rider was unseated";
            return fall.Pair.MountedPairGeneration != fall.Generation ? "relationship generation advanced before the rider was unseated" : null;
        }

        private void Abandon(MountedRiderFallEvent<GameMountedRelationshipService> fall)
        {
            Captured entry;
            if (!captured.TryGetValue(fall.Sequence, out entry)) return;
            captured.Remove(fall.Sequence);
            entry.Record.AbandonedReason = fall.AbandonedReason;
            AbandonedCount++;
            Keep(entry.Record);
            logger.Warning("Mounted rider fall abandoned: sequence=" + fall.Sequence + ";reason=" + fall.AbandonedReason + ".");
        }

        private void Resolve(MountedRiderFallEvent<GameMountedRelationshipService> fall)
        {
            Captured entry;
            if (!captured.TryGetValue(fall.Sequence, out entry)) return;
            captured.Remove(fall.Sequence);
            // Native damage and condition callbacks raised below never re-enter a resolution.
            resolving = true;
            try { ResolveNative(entry); }
            catch (Exception exception) { logger.Exception("Mounted rider fall", exception); }
            finally
            {
                resolving = false;
                ResolvedCount++;
                Keep(entry.Record);
            }
            try { Observed?.Invoke(entry.Record); }
            catch (Exception exception) { logger.Exception("Mounted rider fall observer", exception); }
        }

        private void ResolveNative(Captured entry)
        {
            var rider = entry.Rider;
            var record = entry.Record;
            record.Resolved = true;
            record.ResolveFrame = Time.frameCount;
            var state = rider.Descriptor.State;
            if (state.IsDead)
            {
                // The rider died before landing: no soft-fall operation for a dead rider.
                record.AbandonedReason = "rider died before landing";
                record.Outcome = MountedRiderFallOutcome.None.ToString();
                return;
            }
            var cooldown = rider.CombatState.Cooldown;
            record.StandardBefore = cooldown.StandardAction; record.MoveBefore = cooldown.MoveAction; record.SwiftBefore = cooldown.SwiftAction;
            record.RiderDamageBefore = rider.Damage;
            record.RiderProneBefore = state.Prone.Active;
            var inability = MountedRiderFallPolicy.Inability(new MountedRiderConditionFacts
            {
                Conscious = state.IsConscious, Helpless = state.IsHelpless, Stunned = state.HasCondition(UnitCondition.Stunned),
                Dazed = state.HasCondition(UnitCondition.Dazed), CantAct = state.HasCondition(UnitCondition.CantAct)
            });
            record.Inability = inability.ToString();
            var passed = false;
            if (inability == MountedRiderFallInability.None)
            {
                var forced = ScenarioD20?.Invoke(record);
                record.Forced = forced.HasValue;
                var check = forced.HasValue
                    ? new ControlledFallCheck(rider, MountedRiderFallPolicy.DifficultyClass, forced.Value)
                    : new RuleSkillCheck(rider, StatType.SkillMobility, MountedRiderFallPolicy.DifficultyClass);
                Rulebook.Trigger<RuleSkillCheck>(check);
                record.Attempted = true;
                record.CheckD20 = check.D20; record.CheckStat = check.StatValue; record.CheckBonus = (int)check.Bonus;
                record.CheckResult = check.RollResult; record.CheckDc = check.DC; record.CheckDcAdjustment = check.DifficultyDCAdjustment;
                passed = check.IsPassed;
                record.Passed = passed;
                record.ThresholdAgrees = MountedRiderFallPolicy.Passes(check.RollResult, check.DC) == passed;
            }
            var outcome = MountedRiderFallPolicy.Outcome(inability, passed);
            record.Outcome = outcome.ToString();
            int? damage = null;
            if (MountedRiderFallPolicy.DealsDamage(outcome))
            {
                var fallDamage = new PhysicalDamage(new DiceFormula(MountedRiderFallPolicy.DamageDice, DiceType.D6), PhysicalDamageForm.Bludgeoning);
                var rule = Rulebook.Trigger(new RuleDealDamage(rider, rider, new DamageBundle(fallDamage)));
                var value = rule.ResultDamage?.FirstOrDefault() ?? default(DamageValue);
                record.DamageDealt = rule.ResultDamage != null;
                record.DamageRolled = value.RolledValue;
                record.DamageImmune = value.Source != null && value.Source.Immune;
                record.DamageBeforeDifficulty = rule.DamageBeforeDifficulty;
                record.DamageApplied = rule.Damage;
                damage = rule.Damage;
            }
            var canBeKnockedOff = rider.CanBeKnockedOff();
            if (MountedRiderFallPolicy.ImposesProne(outcome, canBeKnockedOff))
            {
                // The native knock-off writer: the prone controller applies it on its next tick, interrupting commands.
                state.Prone.ShouldBeActive = true;
                record.ProneImposed = true;
            }
            else if (outcome == MountedRiderFallOutcome.HardLanding)
            {
                record.ProneRefusal = state.Prone.Active ? "already prone" : state.HasConditionImmunity(UnitCondition.Prone) ? "immune to prone" : "getting up";
            }
            record.RiderDamageAfter = rider.Damage;
            record.StandardAfter = cooldown.StandardAction; record.MoveAfter = cooldown.MoveAction; record.SwiftAfter = cooldown.SwiftAction;
            record.LogText = MountedRiderFallPolicy.LogText(record.MountName, record.RiderName, entry.Event.Cause,
                inability, record.CheckResult, record.CheckDc, outcome, damage, record.ProneImposed, record.ProneRefusal);
            logger.Info("Mounted rider fall: " + record.LogText + " (riderId=" + record.RiderId + ";mountId=" + record.MountId + ";generation=" + record.Generation + ")");
            try { Game.Instance.UI?.BattleLogManager?.LogView?.AddLogEntry(record.LogText, new Color32(52, 40, 28, 255), LogChannel.Combat); }
            catch (Exception exception) { logger.Exception("Mounted rider fall log entry", exception); }
        }

        private void Keep(MountedRiderFallRecord record)
        {
            records.Add(record);
            if (records.Count > RecordCapacity) records.RemoveAt(0);
        }

        private sealed class ControlledFallCheck : RuleSkillCheck
        {
            private readonly int d20;
            internal ControlledFallCheck(UnitEntityData unit, int dc, int d20) : base(unit, StatType.SkillMobility, dc) { this.d20 = d20; }
            protected override RollEntry RollD20() => d20;
        }
    }
}
