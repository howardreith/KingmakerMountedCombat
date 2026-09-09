using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // Read-only native incoming effects, independent defenses and outgoing
    // projectile completion. No rule result, roll, target or damage is changed.
    internal sealed class Chunk4IncomingRuleObserver : IDisposable,
        IGlobalRulebookHandler<RuleAttackWithWeapon>, IGlobalRulebookHandler<RuleAttackWithWeaponResolve>, IGlobalRulebookHandler<RuleAttackRoll>,
        IGlobalRulebookHandler<RuleDealDamage>, IGlobalRulebookHandler<RuleHealDamage>,
        IGlobalRulebookHandler<RuleSavingThrow>
    {
        private readonly UnitEntityData rider;
        private readonly UnitEntityData mount;
        private readonly IDisposable subscription;
        private readonly List<RuleAttackWithWeapon> attacks = new List<RuleAttackWithWeapon>();
        private readonly HashSet<RuleAttackWithWeapon> resolvedAttacks = new HashSet<RuleAttackWithWeapon>();
        private readonly JArray events = new JArray();
        private string caseId;
        private int dropped;
        internal Chunk4IncomingRuleObserver(UnitEntityData rider, UnitEntityData mount)
        {
            this.rider = rider; this.mount = mount;
            subscription = EventBus.Subscribe(this);
        }
        internal void BeginCase(string value) { caseId = value; }
        internal JObject Capture() => new JObject { ["dropped"] = dropped, ["events"] = events.DeepClone(),
            ["attacks"] = new JArray(attacks.Select(attack => new JObject {
                ["identity"] = RuntimeHelpers.GetHashCode(attack), ["actor"] = attack.Initiator.UniqueId,
                ["target"] = attack.Target.UniqueId, ["projectile"] = attack.Projectile != null,
                ["resolved"] = resolvedAttacks.Contains(attack), ["result"] = attack.AttackRoll?.Result.ToString(),
                ["opportunity"] = attack.IsAttackOfOpportunity, ["weapon"] = attack.Weapon.Blueprint.AssetGuid
            })) };
        internal bool AllAttacksResolved => attacks.All(attack => resolvedAttacks.Contains(attack));
        private bool Pair(UnitEntityData actor) => actor == rider || actor == mount;
        private JObject Record(string kind, object rule, UnitEntityData actor, UnitEntityData target)
        {
            var item = new JObject { ["caseId"] = caseId, ["kind"] = kind,
                ["identity"] = RuntimeHelpers.GetHashCode(rule), ["frame"] = Time.frameCount,
                ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks,
                ["actor"] = actor?.UniqueId, ["target"] = target?.UniqueId,
                ["actorPosition"] = Position(actor), ["targetPosition"] = Position(target),
                ["targetDamage"] = target?.Damage, ["targetACStat"] = target?.Stats.AC.ModifiedValue };
            if (events.Count < 1024) events.Add(item); else dropped++;
            return item;
        }
        private static JToken Position(UnitEntityData actor) => actor == null ? JValue.CreateNull() :
            (JToken)new JArray(actor.Position.x, actor.Position.y, actor.Position.z);
        public void OnEventAboutToTrigger(RuleAttackWithWeapon evt)
        {
            if (!Pair(evt.Initiator) && !Pair(evt.Target)) return;
            if (!attacks.Contains(evt)) attacks.Add(evt);
            var item = Record("attack-before", evt, evt.Initiator, evt.Target);
            item["weapon"] = evt.Weapon.Blueprint.AssetGuid; item["opportunity"] = evt.IsAttackOfOpportunity;
            item["nativeRange"] = evt.Weapon.Blueprint.AttackRange.Meters;
            item["actorCorpulence"] = evt.Initiator.View?.Corpulence;
            item["targetCorpulence"] = evt.Target.View?.Corpulence;
        }
        public void OnEventDidTrigger(RuleAttackWithWeapon evt) { }
        public void OnEventAboutToTrigger(RuleAttackWithWeaponResolve evt) { }
        public void OnEventDidTrigger(RuleAttackWithWeaponResolve evt)
        {
            if (!Pair(evt.Initiator) && !Pair(evt.Target)) return;
            var item = Record("weapon-resolved", evt, evt.Initiator, evt.Target);
            item["attack"] = RuntimeHelpers.GetHashCode(evt.AttackWithWeapon);
            item["firstResolution"] = resolvedAttacks.Add(evt.AttackWithWeapon);
        }
        public void OnEventAboutToTrigger(RuleAttackRoll evt) { }
        public void OnEventDidTrigger(RuleAttackRoll evt)
        {
            if (!Pair(evt.Initiator) && !Pair(evt.Target)) return;
            var item = Record("attack-roll", evt, evt.Initiator, evt.Target);
            item["attack"] = evt.RuleAttackWithWeapon == null ? 0 : RuntimeHelpers.GetHashCode(evt.RuleAttackWithWeapon);
            item["nativeAC"] = evt.TargetAC; item["flatFooted"] = evt.IsTargetFlatFooted;
            item["attackType"] = evt.AttackType.ToString(); item["attackBonus"] = evt.AttackBonus;
            item["result"] = evt.Result.ToString(); item["autoHit"] = evt.AutoHit; item["autoMiss"] = evt.AutoMiss;
        }
        public void OnEventAboutToTrigger(RuleDealDamage evt)
        { if (Pair(evt.Target)) Record("damage-before", evt, evt.Initiator, evt.Target); }
        public void OnEventDidTrigger(RuleDealDamage evt)
        {
            if (!Pair(evt.Target)) return;
            var item = Record("damage-after", evt, evt.Initiator, evt.Target);
            item["damage"] = evt.Damage; item["beforeDifficulty"] = evt.DamageBeforeDifficulty;
            item["sourceAbility"] = evt.SourceAbility?.AssetGuid; item["sourceArea"] = evt.SourceArea?.AssetGuid;
            item["halfBecauseSavingThrow"] = evt.HalfBecauseSavingThrow;
        }
        public void OnEventAboutToTrigger(RuleHealDamage evt)
        { if (Pair(evt.Target)) Record("heal-before", evt, evt.Initiator, evt.Target); }
        public void OnEventDidTrigger(RuleHealDamage evt)
        {
            if (!Pair(evt.Target)) return;
            var item = Record("heal-after", evt, evt.Initiator, evt.Target); item["value"] = evt.Value;
        }
        public void OnEventAboutToTrigger(RuleSavingThrow evt) { }
        public void OnEventDidTrigger(RuleSavingThrow evt)
        {
            if (!Pair(evt.Initiator)) return;
            var item = Record("saving-throw", evt, evt.Initiator, evt.Initiator);
            item["nativeSource"] = Chunk4NativeAreaObservation.CaptureSaveSource(evt); item["type"] = evt.Type.ToString(); item["dc"] = evt.DifficultyClass; item["stat"] = evt.StatValue;
            item["roll"] = evt.RollResult; item["passed"] = evt.IsPassed; item["autoPass"] = evt.AutoPass;
            item["evasion"] = evt.Evasion; item["improvedEvasion"] = evt.ImprovedEvasion;
        }
        public void Dispose() { subscription.Dispose(); }
    }
}
