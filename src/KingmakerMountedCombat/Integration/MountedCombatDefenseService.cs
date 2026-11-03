using System;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UI.Log;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Logging;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Integration
{
    // One attempt's distinguishing values, captured at the native boundary before any later rule can change them.
    internal sealed class MountedCombatDefenseRecord
    {
        internal string Rider, Mount, Attacker, Weapon, ResultBefore, ResultAfter, Mode;
        internal MountedCombatDefenseRefusal Refusal;
        internal MountedCombatDefenseCommitment Commitment;
        internal int AttackD20, AttackBonus, AttackResult, TargetAc, CheckD20, CheckStat, CheckBonus, CheckResult, CheckDc, Round, Frame;
        internal bool Attempted, Negated, CriticalBefore, CriticalAfter, AttackOfOpportunity, RiderTurn, Forced;
        internal float SwiftBefore, SwiftAfter;
        internal MountedCombatDefenseState StateBefore, StateAfter;
        internal RuleAttackRoll Rule;
    }

    // The native charge for a borrowed or current Swift action. It is never run: an immediate action has no native
    // out-of-turn command (6E), so this only carries the Swift type through the unit's own UpdateCooldowns.
    internal sealed class MountedCombatReactionCommand : UnitCommand
    {
        internal MountedCombatReactionCommand() : base(CommandType.Swift) { }
        protected override ResultType OnAction() => ResultType.Success;
    }

    // Policy lives in MountedCombatDefensePolicy; this reads native facts and carries out what it decides. The transpiled
    // hook runs inside RuleAttackRoll.OnTrigger after the result and any native parry, before the sneak-attack mask and
    // the IAttackHandler event, so no damage, on-hit effect or log entry has seen the hit.
    internal sealed class MountedCombatDefenseService : IUnitNewCombatRoundHandler, IUnitCombatHandler, IDisposable
    {
        private static readonly System.Reflection.MethodInfo CriticalConfirmed = NativeSetter("IsCriticalConfirmed", 0x0600713C);
        private static readonly System.Reflection.MethodInfo CriticalRoll = NativeSetter("IsCriticalRoll", 0x0600713A);
        private const int SwiftRoundSeconds = 6;
        private const int RealTimeObservationFrames = 30;

        private readonly HorseCompanionBlueprintService blueprints;
        private readonly MountedWorldService world;
        private readonly Func<bool> restorationPending;
        private readonly IModLogger logger;
        private bool resolving, disposed;
        private int nextObservationFrame;

        internal static MountedCombatDefenseService Current { get; private set; }
        internal bool Enabled { get; set; }
        internal int AttemptCount { get; private set; }
        internal int NegatedCount { get; private set; }
        internal int SwiftChargeCount { get; private set; }
        internal MountedCombatDefenseRecord LastRecord { get; private set; }
        // Test environment only: replaces the check's d20 for one exact equality or failure row. Null in ordinary play.
        internal Func<MountedCombatDefenseRecord, int?> ScenarioD20 { get; set; }
        internal event Action<MountedCombatDefenseRecord> Observed;

        internal MountedCombatDefenseService(HorseCompanionBlueprintService blueprints, MountedWorldService world, Func<bool> restorationPending, IModLogger logger)
        {
            this.blueprints = blueprints ?? throw new ArgumentNullException(nameof(blueprints));
            this.world = world ?? throw new ArgumentNullException(nameof(world));
            this.restorationPending = restorationPending ?? throw new ArgumentNullException(nameof(restorationPending));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            EventBus.Subscribe(this);
            Current = this;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (ReferenceEquals(Current, this)) Current = null;
            EventBus.Unsubscribe(this);
        }

        private static System.Reflection.MethodInfo NativeSetter(string property, int token)
        {
            var setter = typeof(RuleAttackRoll).GetProperty(property)?.GetSetMethod(true);
            if (setter == null || setter.MetadataToken != token)
                throw new MissingMethodException(typeof(RuleAttackRoll).FullName, property);
            return setter;
        }

        private bool Ready => Enabled && !disposed && blueprints.MountedCombatFeature != null;

        internal void ResolveAttack(RuleAttackRoll rule)
        {
            if (resolving || !Ready || !rule.IsHit) return;
            var pair = world.Pairs.FirstOrDefault(value => value.Relationship.State == RelationshipState.Mounted && ReferenceEquals(value.Relationship.Mount, rule.Target));
            var rider = pair?.Relationship.Rider;
            if (rider == null) return;
            // Exactly one attempt: this site runs once per attack rule, and a rule cannot be re-entered from its own check.
            resolving = true;
            try { Resolve(rule, rider, pair.Relationship.Mount); }
            catch (Exception exception) { logger.Exception("Mounted Combat reaction", exception); }
            finally { resolving = false; }
        }

        private void Resolve(RuleAttackRoll rule, UnitEntityData rider, UnitEntityData mount)
        {
            ObserveRealTime(rider);
            var turnBased = CombatController.IsInTurnBasedCombat();
            var turn = Game.Instance.TurnBasedCombatController?.CurrentTurn;
            var riderTurn = turnBased && turn?.Unit == rider &&
                (turn.Status == TurnController.TurnStatus.Preparing || turn.IsActing || turn.IsEnding);
            var attackResult = (int)rule.Roll + rule.AttackBonus;
            var before = ReadState(rider);
            var record = new MountedCombatDefenseRecord {
                Rider = rider.UniqueId, Mount = mount.UniqueId, Attacker = rule.Initiator.UniqueId, Weapon = rule.Weapon?.Blueprint?.name,
                Rule = rule, ResultBefore = rule.Result.ToString(), Mode = turnBased ? "TB" : "RT", AttackD20 = rule.Roll, AttackBonus = rule.AttackBonus,
                AttackResult = attackResult, TargetAc = rule.TargetAC, CriticalBefore = rule.IsCriticalConfirmed, RiderTurn = riderTurn,
                AttackOfOpportunity = rule.RuleAttackWithWeapon?.IsAttackOfOpportunity == true, SwiftBefore = rider.CombatState.Cooldown.SwiftAction,
                Round = Game.Instance.TurnBasedCombatController?.RoundNumber ?? 0, Frame = Time.frameCount, StateBefore = before };
            var state = rider.Descriptor.State;
            var facts = new MountedCombatDefenseFacts {
                WouldHit = true, HasNumericAttackResult = !rule.AutoHit && (int)rule.Roll > 0, OwnsFeat = rider.Descriptor.Progression.Features.HasFact(blueprints.MountedCombatFeature),
                PreferenceOn = PreferenceOn(rider),
                RiderCanAct = state.IsConscious && state.CanAct && !state.IsHelpless && rider.CombatState.CanActInCombat,
                RiderFlatFooted = Rulebook.Trigger(new RuleCheckTargetFlatFooted(rule.Initiator, rider)).IsFlatFooted,
                TurnBased = turnBased, RiderTurn = riderTurn, SwiftAvailable = rider.HasSwiftAction(), State = before };
            MountedCombatDefenseCommitment commitment;
            record.Refusal = MountedCombatDefensePolicy.Evaluate(facts, out commitment);
            record.Commitment = commitment;
            if (record.Refusal == MountedCombatDefenseRefusal.None)
            {
                // Committed before the roll: a failed check still spends the use and the action.
                if (Apply(rider, before, MountedCombatDefensePolicy.Commit(before, commitment))) RollCheck(rule, rider, record);
                else record.Refusal = MountedCombatDefenseRefusal.RiderCannotAct;
            }
            record.ResultAfter = rule.Result.ToString();
            record.CriticalAfter = rule.IsCriticalConfirmed;
            record.SwiftAfter = rider.CombatState.Cooldown.SwiftAction;
            record.StateAfter = ReadState(rider);
            LastRecord = record;
            Observed?.Invoke(record);
        }

        private void RollCheck(RuleAttackRoll rule, UnitEntityData rider, MountedCombatDefenseRecord record)
        {
            record.Attempted = true;
            AttemptCount++;
            var forced = ScenarioD20?.Invoke(record);
            record.Forced = forced.HasValue;
            // The DC is the first passing result, so the native pass/fail and its log line agree with the strict rule.
            // The game's difficulty setting adjusts other DCs, not an attack result.
            var dc = record.AttackResult + 1;
            var check = forced.HasValue ? new ControlledSkillCheck(rider, dc, forced.Value) : new RuleSkillCheck(rider, StatType.SkillMobility, dc);
            check.IgnoreDifficultyBonusToDC = true;
            Rulebook.Trigger<RuleSkillCheck>(check);
            record.CheckD20 = check.D20; record.CheckStat = check.StatValue; record.CheckBonus = (int)check.Bonus; record.CheckResult = check.RollResult; record.CheckDc = check.DC;
            record.Negated = MountedCombatDefensePolicy.Negates(check.RollResult, record.AttackResult);
            if (record.Negated)
            {
                // AttackResult.Parried is the engine's own negated hit: IsHit is false, so a melee resolve deals no damage
                // and a projectile takes its miss path. A negated critical also stops threatening its critical-only triggers.
                rule.SetFake(AttackResult.Parried);
                var no = new object[] { false };
                CriticalConfirmed.Invoke(rule, no);
                CriticalRoll.Invoke(rule, no);
                NegatedCount++;
            }
            ReportToLog(rider, record);
        }

        private sealed class ControlledSkillCheck : RuleSkillCheck
        {
            private readonly int d20;
            internal ControlledSkillCheck(UnitEntityData unit, int dc, int d20) : base(unit, StatType.SkillMobility, dc) { this.d20 = d20; }
            protected override RollEntry RollD20() => d20;
        }

        private void ReportToLog(UnitEntityData rider, MountedCombatDefenseRecord record)
        {
            var text = "Mounted Combat: " + rider.CharacterName + " rolls Mobility " + record.CheckResult + " against attack " + record.AttackResult +
                (record.Negated ? " and negates the hit on the mount." : "; the hit stands.");
            logger.Info(text);
            try { Game.Instance.UI?.BattleLogManager?.LogView?.AddLogEntry(text, new Color32(52, 40, 28, 255), LogChannel.Combat); }
            catch (Exception exception) { logger.Exception("Mounted Combat log entry", exception); }
        }

        private bool PreferenceOn(UnitEntityData rider)
        {
            var toggle = rider.Descriptor.ActivatableAbilities.GetFact(blueprints.MountedCombatToggle) as ActivatableAbility;
            return toggle == null || toggle.IsOn;
        }

        internal MountedCombatDefenseState ReadState(UnitEntityData rider)
        {
            var buffs = rider.Descriptor.Buffs;
            return new MountedCombatDefenseState { Spent = buffs.HasFact(blueprints.MountedCombatSpentBuff), Owed = buffs.HasFact(blueprints.MountedCombatOwedBuff), Window = buffs.HasFact(blueprints.MountedCombatWindowBuff) };
        }

        private bool Apply(UnitEntityData rider, MountedCombatDefenseState before, MountedCombatDefenseTransition transition)
        {
            var after = transition.State;
            if (!SetBuff(rider, blueprints.MountedCombatSpentBuff, before.Spent, after.Spent, null) ||
                !SetBuff(rider, blueprints.MountedCombatOwedBuff, before.Owed, after.Owed, null) ||
                !SetBuff(rider, blueprints.MountedCombatWindowBuff, before.Window, after.Window, TimeSpan.FromSeconds(SwiftRoundSeconds)))
                return false;
            if (transition.ChargeSwift)
            {
                // The unit's own native writer, as a Swift command's start would call it.
                rider.UpdateCooldowns(new MountedCombatReactionCommand());
                SwiftChargeCount++;
            }
            return true;
        }

        private bool SetBuff(UnitEntityData rider, BlueprintBuff blueprint, bool present, bool wanted, TimeSpan? duration)
        {
            if (present == wanted) return true;
            if (!wanted) { rider.Descriptor.Buffs.RemoveFact(blueprint); return true; }
            var applied = rider.Descriptor.Buffs.AddBuff(blueprint, rider, duration);
            if (applied != null) return true;
            logger.Warning("The native rule refused Mounted Combat state " + blueprint.name + " on " + rider.UniqueId + ".");
            return false;
        }

        private void ObserveRealTime(UnitEntityData rider)
        {
            if (CombatController.IsInTurnBasedCombat() || restorationPending()) return;
            var state = ReadState(rider);
            if (state.Spent || state.Owed) Apply(rider, state, MountedCombatDefensePolicy.RealTimeObserved(state));
        }

        // Turn-based state carried into real time needs its window started; nothing else depends on this cadence.
        internal void Update()
        {
            if (!Ready || Time.frameCount < nextObservationFrame) return;
            nextObservationFrame = Time.frameCount + RealTimeObservationFrames;
            var party = Game.Instance?.Player?.PartyCharacters;
            if (party == null || CombatController.IsInTurnBasedCombat() || restorationPending()) return;
            foreach (var reference in party)
            {
                var rider = reference.Value;
                if (rider?.Descriptor != null && rider.IsInGame) ObserveRealTime(rider);
            }
        }

        public void HandleNewCombatRound(UnitEntityData unit)
        {
            // In real time this event only follows a Standard cooldown. The turn-based event is the unit's native
            // preparation, after its cooldowns were cleared.
            if (!Ready || !CombatController.IsInTurnBasedCombat() || unit?.Descriptor == null) return;
            var state = ReadState(unit);
            if (state.Any) Apply(unit, state, MountedCombatDefensePolicy.RiderTurnPrepared(state));
        }

        public void HandleUnitJoinCombat(UnitEntityData unit) { }

        // A round ends with its combat; a reload does not pass through here.
        public void HandleUnitLeaveCombat(UnitEntityData unit)
        {
            if (!Ready || unit?.Descriptor == null || restorationPending()) return;
            var state = ReadState(unit);
            if (state.Any) Apply(unit, state, new MountedCombatDefenseTransition());
        }
    }
}
