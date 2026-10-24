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
    // 7E X1 C11/F04: read-only native weapon attacks whose initiator or target is one of the declared
    // units (both pairs and the diagnostic target). Each record carries the native attack-of-opportunity
    // flag, the weapon and both actors' positions at the rule (the shot origin for a ranged attack).
    // No rule input, roll, target or result is changed.
    internal sealed class Chunk7PairRuleObserver : IDisposable, IGlobalRulebookHandler<RuleAttackWithWeapon>
    {
        private readonly UnitEntityData[] units;
        private readonly IDisposable subscription;
        private readonly JArray events = new JArray(), errors = new JArray();
        private int dropped;
        private bool disposed;
        internal Chunk7PairRuleObserver(params UnitEntityData[] units)
        {
            if (units == null || units.Length == 0 || units.Any(unit => unit == null)) throw new ArgumentNullException(nameof(units));
            this.units = units.ToArray();
            subscription = EventBus.Subscribe(this);
        }
        internal int EventCount => events.Count;
        internal JArray Since(int count) => new JArray(events.Skip(count).Select(item => item.DeepClone()));
        internal JObject Capture() => new JObject { ["dropped"] = dropped, ["errors"] = errors.DeepClone(), ["events"] = events.DeepClone() };
        private bool Declared(UnitEntityData unit) => unit != null && units.Any(item => ReferenceEquals(item, unit));
        private static JArray Position(UnitEntityData unit) => unit == null ? null : new JArray(unit.Position.x, unit.Position.y, unit.Position.z);
        public void OnEventAboutToTrigger(RuleAttackWithWeapon evt) { }
        public void OnEventDidTrigger(RuleAttackWithWeapon evt)
        {
            try
            {
                if (disposed || evt == null || !Declared(evt.Initiator) && !Declared(evt.Target)) return;
                if (events.Count >= 256) { dropped++; return; }
                events.Add(new JObject { ["sequence"] = events.Count + 1, ["kind"] = "attack", ["identity"] = RuntimeHelpers.GetHashCode(evt),
                    ["frame"] = Time.frameCount, ["round"] = Game.Instance.TurnBasedCombatController.RoundNumber,
                    ["actor"] = evt.Initiator?.UniqueId, ["target"] = evt.Target?.UniqueId,
                    ["opportunity"] = evt.IsAttackOfOpportunity, ["fullAttack"] = evt.IsFullAttack,
                    ["weapon"] = evt.Weapon?.Blueprint.AssetGuid, ["ranged"] = evt.Weapon?.Blueprint.IsRanged == true,
                    ["natural"] = evt.Weapon?.Blueprint.IsNatural == true,
                    ["actorPosition"] = Position(evt.Initiator), ["targetPosition"] = Position(evt.Target) });
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
