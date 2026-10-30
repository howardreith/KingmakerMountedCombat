using System;
using System.Collections.Generic;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed class MountedCombatRuleProbe :
        IGlobalRulebookHandler<RuleAttackWithWeapon>,
        IGlobalRulebookHandler<RuleAttackRoll>,
        IGlobalRulebookHandler<RuleRollDice>,
        IGlobalRulebookHandler<RuleDealDamage>,
        IDisposable
    {
        private readonly IDisposable subscription;
        private UnitEntityData rider;
        private UnitEntityData mount;
        private UnitEntityData expectedActor;
        private UnitEntityData expectedTarget;
        private bool disposed;

        public MountedCombatRuleProbe()
        {
            subscription = EventBus.Subscribe(this);
        }

        public int AttackRuleCount { get; private set; }

        public int AttackRollCount { get; private set; }

        public int DamageRuleCount { get; private set; }

        public int UnexpectedPairAttackCount { get; private set; }

        public int ForcedD20Count { get; private set; }

        public int? ForcedD20 { get; private set; }

        public int TotalDamage { get; private set; }

        public string LastInitiatorId { get; private set; }

        public string LastTargetId { get; private set; }

        public string LastAttackResult { get; private set; }

        public bool? LastAttackHit { get; private set; }

        public void Arm(
            UnitEntityData exactRider,
            UnitEntityData exactMount,
            UnitEntityData actor,
            UnitEntityData target,
            int? forcedD20 = null)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(MountedCombatRuleProbe));
            }
            rider = exactRider ?? throw new ArgumentNullException(nameof(exactRider));
            mount = exactMount ?? throw new ArgumentNullException(nameof(exactMount));
            expectedActor = actor ?? throw new ArgumentNullException(nameof(actor));
            expectedTarget = target ?? throw new ArgumentNullException(nameof(target));
            if (forcedD20.HasValue && forcedD20.Value != 1 && forcedD20.Value != 20)
            {
                throw new ArgumentOutOfRangeException(nameof(forcedD20), "Only exact diagnostic natural 1 or 20 is permitted.");
            }
            ForcedD20 = forcedD20;
            AttackRuleCount = 0;
            AttackRollCount = 0;
            DamageRuleCount = 0;
            UnexpectedPairAttackCount = 0;
            ForcedD20Count = 0;
            TotalDamage = 0;
            LastInitiatorId = null;
            LastTargetId = null;
            LastAttackResult = null;
            LastAttackHit = null;
            events.Clear(); FirstExpectedAttack = null; FirstUnexpectedPairAttack = null; ObservedPairRuleCount = 0; pending = null;
        }

        public void OnEventAboutToTrigger(RuleAttackWithWeapon evt)
        {
            BeginEvent(evt);
        }

        public void OnEventDidTrigger(RuleAttackWithWeapon evt)
        {
            EndEvent(evt);
            if (!IsObservedTarget(evt?.Target))
            {
                return;
            }
            if (evt.Initiator == expectedActor)
            {
                AttackRuleCount++;
                LastInitiatorId = evt.Initiator.UniqueId;
                LastTargetId = evt.Target.UniqueId;
            }
            else if (evt.Initiator == rider || evt.Initiator == mount)
            {
                UnexpectedPairAttackCount++;
            }
        }

        public void OnEventAboutToTrigger(RuleAttackRoll evt)
        {
        }

        public void OnEventDidTrigger(RuleAttackRoll evt)
        {
            if (!IsExact(evt?.Initiator, evt?.Target))
            {
                return;
            }
            AttackRollCount++;
            LastInitiatorId = evt.Initiator.UniqueId;
            LastTargetId = evt.Target.UniqueId;
            LastAttackResult = evt.Result.ToString();
            LastAttackHit = evt.IsHit;
        }

        public void OnEventAboutToTrigger(RuleDealDamage evt)
        {
        }

        public void OnEventAboutToTrigger(RuleRollDice evt)
        {
            if (ForcedD20.HasValue &&
                evt != null &&
                evt.Initiator == expectedActor &&
                evt.DiceFormula.Rolls == 1 &&
                evt.DiceFormula.Dice == Kingmaker.RuleSystem.DiceType.D20)
            {
                evt.Override(ForcedD20.Value);
                ForcedD20Count++;
            }
        }

        public void OnEventDidTrigger(RuleRollDice evt)
        {
        }

        public void OnEventDidTrigger(RuleDealDamage evt)
        {
            if (!IsExact(evt?.Initiator, evt?.Target))
            {
                return;
            }
            DamageRuleCount++;
            TotalDamage += evt.Damage;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }
            subscription.Dispose();
            disposed = true;
        }

        private bool IsObservedTarget(UnitEntityData target)
        {
            return expectedTarget != null && target == expectedTarget;
        }

        private bool IsExact(UnitEntityData initiator, UnitEntityData target)
        {
            if (!IsObservedTarget(target))
            {
                return false;
            }
            if (initiator == expectedActor)
            {
                return true;
            }
            if (initiator == rider || initiator == mount)
            {
                UnexpectedPairAttackCount++;
            }
            return false;
        }
        // Sprint 1 UPG-01 (iteration 7): bounded, passive provenance of the pair's weapon-attack rules against
        // the observed target. One record per rule (at most MaximumEvents), the first expected (rider) and the
        // first unexpected (other pair member) kept separately; pre/post Standard cooldown of the initiator and
        // its current standard command identity are read, never written. Nothing here changes any counter above.
        internal const int MaximumEvents = 8;
        private readonly List<MountedCombatRuleProbeEvent> events = new List<MountedCombatRuleProbeEvent>();
        public IReadOnlyList<MountedCombatRuleProbeEvent> Events => events;
        public MountedCombatRuleProbeEvent FirstExpectedAttack { get; private set; }
        public MountedCombatRuleProbeEvent FirstUnexpectedPairAttack { get; private set; }
        public int ObservedPairRuleCount { get; private set; }
        private MountedCombatRuleProbeEvent pending;

        private void BeginEvent(RuleAttackWithWeapon evt)
        {
            pending = null;
            if (evt?.Initiator == null || !IsObservedTarget(evt.Target) || (evt.Initiator != rider && evt.Initiator != mount)) return;
            pending = MountedCombatRuleProbeEvent.Capture(evt, evt.Initiator == expectedActor, ObservedPairRuleCount + 1);
        }

        private void EndEvent(RuleAttackWithWeapon evt)
        {
            var record = pending; pending = null;
            if (record == null || evt?.Initiator == null) return;
            record.Complete(evt);
            ObservedPairRuleCount++;
            if (record.Expected) { if (FirstExpectedAttack == null) FirstExpectedAttack = record; }
            else if (FirstUnexpectedPairAttack == null) FirstUnexpectedPairAttack = record;
            if (events.Count < MaximumEvents) events.Add(record);
        }
    }

    internal sealed class MountedCombatRuleProbeEvent
    {
        public int Ordinal { get; private set; }
        public bool Expected { get; private set; }
        public string InitiatorId { get; private set; }
        public string TargetId { get; private set; }
        public long GameTicks { get; private set; }
        public bool IsAttackOfOpportunity { get; private set; }
        public bool IsFullAttack { get; private set; }
        public bool IsCharge { get; private set; }
        public bool IsFirstAttack { get; private set; }
        public int AttackNumber { get; private set; }
        public int AttacksCount { get; private set; }
        public string WeaponBlueprintId { get; private set; }
        public bool InitiatorAiEnabled { get; private set; }
        public string CommandType { get; private set; }
        public bool? CommandCreatedByPlayer { get; private set; }
        public string CommandAiActionBlueprintId { get; private set; }
        public bool? CommandIsStarted { get; private set; }
        public bool? CommandIsActed { get; private set; }
        public string CommandTargetId { get; private set; }
        public float StandardCooldownBefore { get; private set; }
        public float StandardCooldownAfter { get; private set; }
        public string Result { get; private set; }
        public bool? Hit { get; private set; }

        internal static MountedCombatRuleProbeEvent Capture(RuleAttackWithWeapon evt, bool expected, int ordinal)
        {
            var initiator = evt.Initiator;
            var command = initiator.Commands?.Standard;
            return new MountedCombatRuleProbeEvent
            {
                Ordinal = ordinal, Expected = expected, InitiatorId = initiator.UniqueId, TargetId = evt.Target?.UniqueId,
                GameTicks = Game.Instance?.TimeController?.GameTime.Ticks ?? 0,
                IsAttackOfOpportunity = evt.IsAttackOfOpportunity, IsFullAttack = evt.IsFullAttack, IsCharge = evt.IsCharge,
                IsFirstAttack = evt.IsFirstAttack, AttackNumber = evt.AttackNumber, AttacksCount = evt.AttacksCount,
                WeaponBlueprintId = evt.Weapon?.Blueprint?.AssetGuid, InitiatorAiEnabled = initiator.IsAIEnabled,
                CommandType = command?.GetType().Name, CommandCreatedByPlayer = command?.CreatedByPlayer,
                CommandAiActionBlueprintId = command?.AiAction?.AssetGuid, CommandIsStarted = command?.IsStarted,
                CommandIsActed = command?.IsActed, CommandTargetId = command?.TargetUnit?.UniqueId,
                StandardCooldownBefore = initiator.CombatState?.Cooldown?.StandardAction ?? -1f,
                StandardCooldownAfter = -1f
            };
        }

        internal void Complete(RuleAttackWithWeapon evt)
        {
            StandardCooldownAfter = evt.Initiator.CombatState?.Cooldown?.StandardAction ?? -1f;
            Result = evt.AttackRoll?.Result.ToString();
            Hit = evt.AttackRoll?.IsHit;
        }
    }
}
