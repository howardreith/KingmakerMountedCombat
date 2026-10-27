using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // 7E X1 C03: read-only native attack rules of rider A and mount A against the diagnostic
    // target. Records each weapon attack (plan position, full attack, weapon) and each attack
    // roll (total bonus, the native target-independent calculation and its bonus sources). No
    // rule input, roll, target or result is changed.
    internal sealed class Chunk7AttackModesRuleObserver : IDisposable,
        IGlobalRulebookHandler<RuleAttackWithWeapon>, IGlobalRulebookHandler<RuleAttackRoll>
    {
        private readonly UnitEntityData rider, mount, target;
        private readonly IDisposable subscription;
        private readonly JArray events = new JArray(), errors = new JArray();
        private int dropped;
        private bool disposed;
        internal Chunk7AttackModesRuleObserver(UnitEntityData rider, UnitEntityData mount, UnitEntityData target)
        {
            this.rider = rider ?? throw new ArgumentNullException(nameof(rider));
            this.mount = mount ?? throw new ArgumentNullException(nameof(mount));
            this.target = target ?? throw new ArgumentNullException(nameof(target));
            subscription = EventBus.Subscribe(this);
        }
        internal int EventCount => events.Count;
        internal JArray Since(int count) => new JArray(events.Skip(count).Select(item => item.DeepClone()));
        internal JObject Capture() => new JObject { ["dropped"] = dropped, ["errors"] = errors.DeepClone(), ["events"] = events.DeepClone() };
        private bool Observed(UnitEntityData initiator, UnitEntityData victim) =>
            !disposed && (ReferenceEquals(initiator, rider) || ReferenceEquals(initiator, mount)) && ReferenceEquals(victim, target);
        private JObject Record(string kind, object rule, UnitEntityData initiator)
        {
            var item = new JObject { ["sequence"] = events.Count + 1, ["kind"] = kind, ["identity"] = RuntimeHelpers.GetHashCode(rule),
                ["frame"] = Time.frameCount, ["round"] = Game.Instance.TurnBasedCombatController.RoundNumber,
                ["actor"] = initiator.UniqueId, ["target"] = target.UniqueId };
            if (events.Count >= 256) { dropped++; return null; }
            events.Add(item); return item;
        }
        public void OnEventAboutToTrigger(RuleAttackWithWeapon evt) { }
        public void OnEventDidTrigger(RuleAttackWithWeapon evt)
        {
            try
            {
                if (evt == null || !Observed(evt.Initiator, evt.Target)) return;
                var item = Record("attack", evt, evt.Initiator); if (item == null) return;
                item["opportunity"] = evt.IsAttackOfOpportunity; item["fullAttack"] = evt.IsFullAttack; item["firstAttack"] = evt.IsFirstAttack;
                item["attackNumber"] = evt.AttackNumber; item["attacksCount"] = evt.AttacksCount;
                item["weapon"] = evt.Weapon?.Blueprint.AssetGuid; item["ranged"] = evt.Weapon?.Blueprint.IsRanged == true;
                item["natural"] = evt.Weapon?.Blueprint.IsNatural == true;
            }
            catch (Exception error) { if (!errors.Any(e => (string)e == error.ToString())) errors.Add(error.ToString()); }
        }
        public void OnEventAboutToTrigger(RuleAttackRoll evt) { }
        public void OnEventDidTrigger(RuleAttackRoll evt)
        {
            try
            {
                if (evt == null || !Observed(evt.Initiator, evt.Target)) return;
                var item = Record("roll", evt, evt.Initiator); if (item == null) return;
                item["attack"] = evt.RuleAttackWithWeapon == null ? 0 : RuntimeHelpers.GetHashCode(evt.RuleAttackWithWeapon);
                item["opportunity"] = evt.RuleAttackWithWeapon?.IsAttackOfOpportunity == true;
                item["attackBonus"] = evt.AttackBonus; item["iterativePenalty"] = evt.AttackBonusPenalty;
                item["weapon"] = evt.Weapon?.Blueprint.AssetGuid; item["ranged"] = evt.Weapon?.Blueprint.IsRanged == true;
                item["nativeCalculation"] = NativeAttackBonusEvidence.Capture(evt);
                item["bonuses"] = evt.AttackBonusRule == null ? null : new JArray(evt.AttackBonusRule.BonusSources.Select(source => new JObject {
                    ["bonus"] = source.Bonus, ["source"] = source.Source?.Blueprint.AssetGuid, ["sourceName"] = source.Source?.Blueprint.name }));
            }
            catch (Exception error) { if (!errors.Any(e => (string)e == error.ToString())) errors.Add(error.ToString()); }
        }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true; subscription?.Dispose();
        }
    }
}
