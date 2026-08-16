using System;
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
        }

        public void OnEventAboutToTrigger(RuleAttackWithWeapon evt)
        {
        }

        public void OnEventDidTrigger(RuleAttackWithWeapon evt)
        {
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
    }
}
