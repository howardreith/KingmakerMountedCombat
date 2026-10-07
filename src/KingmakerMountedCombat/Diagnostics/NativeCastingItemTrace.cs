using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Harmony12;
using Kingmaker;
using Kingmaker.Controllers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using KingmakerMountedCombat.Domain;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // Disposable-fixture observation only. Native input, resources and processes are
    // never changed by these hooks. Substantive acceptance belongs to the reader.
    internal sealed class NativeCastingItemTrace : IDisposable,
        IGlobalRulebookHandler<RuleCastSpell>, IGlobalRulebookHandler<RuleDealDamage>,
        IGlobalRulebookHandler<RuleHealDamage>, IGlobalRulebookHandler<RuleCheckConcentration>,
        IGlobalRulebookHandler<RuleCheckCastingDefensively>, IGlobalRulebookHandler<RuleSummonUnit>
    {
        private const string HarmonyId = "KingmakerMountedCombat.Diagnostics.CastingItems";
        private static NativeCastingItemTrace active;
        private readonly HarmonyInstance harmony;
        private readonly IDisposable subscription;
        private readonly UnitEntityData rider, mount;
        private readonly RetainedObjectIdentity identities = new RetainedObjectIdentity(4096);
        private readonly JArray events = new JArray(), hooks = new JArray();
        private readonly List<UnitUseAbility> shells = new List<UnitUseAbility>();
        private readonly List<UnitEntityData> summons = new List<UnitEntityData>();
        private readonly List<AbilityExecutionProcess> processes = new List<AbilityExecutionProcess>();
        private string caseId;
        private int dropped, faults;
        private bool disposed;
        private JObject closed;
        internal NativeCastingItemTrace(UnitEntityData rider, UnitEntityData mount)
        {
            if (active != null) throw new InvalidOperationException("Casting observer already active.");
            if (typeof(UnitUseAbility).Assembly.ManifestModule.ModuleVersionId != new Guid("07fa1e4d-8618-41b3-9b8d-faa17d3b26f7"))
                throw new InvalidOperationException("Casting observer requires the pinned native MVID.");
            this.rider = rider; this.mount = mount;
            harmony = HarmonyInstance.Create(HarmonyId); active = this;
            try
            {
                Patch(typeof(UnitUseAbility), 0x0600272D, "ShellStartBefore", "ShellStartAfter");
                Patch(typeof(UnitUseAbility), 0x06002737, "ShellActionBefore", "ShellActionAfter");
                Patch(typeof(AbilityData), 0x06002B60, "SpellSpendBefore", "SpellSpendAfter");
                Patch(typeof(ItemEntity), 0x06007B74, "ItemSpendBefore", "ItemSpendAfter");
                Patch(typeof(UnitUseAbility), 0x0600273A, "ConcentrationBefore", "ConcentrationAfter");
                Patch(typeof(UnitUseAbility), 0x0600273B, "DefensiveBefore", "DefensiveAfter");
                subscription = EventBus.Subscribe(this);
            }
            catch { Dispose(); throw; }
        }
        internal int EventCount => events.Count;
        internal bool Complete => dropped == 0 && faults == 0 && identities.FaultCount == 0;
        internal bool ProcessesSettled => processes.All(p => p.IsEnded);
        internal IReadOnlyList<UnitUseAbility> Shells => shells;
        internal IReadOnlyList<UnitEntityData> Summons => summons;
        internal void BeginCase(string id) { caseId = id; }
        internal int Identity(object value) => identities.Get(value);
        internal JArray EventsSince(int offset) => new JArray(events.Skip(offset).Select(e => e.DeepClone()));
        internal JObject Capture() => disposed ? (JObject)closed.DeepClone() : new JObject {
            ["contract"] = "native-casting-item-observation-v1", ["dropped"] = dropped,
            ["faults"] = faults, ["hooks"] = hooks.DeepClone(), ["events"] = events.DeepClone(),
            ["identityRegistry"] = new JObject { ["retainedCount"] = identities.RetainedCount,
                ["released"] = identities.Released, ["faults"] = identities.FaultCount },
            ["shells"] = new JArray(shells.Select(Shell)),
            ["processes"] = new JArray(processes.Select(p => new JObject {
                ["identity"] = Identity(p), ["context"] = Identity(p.Context), ["ended"] = p.IsEnded }))
        };
        internal JObject Ability(AbilityData spell) => spell == null ? null : new JObject {
            ["identity"] = Identity(spell), ["blueprint"] = spell.Blueprint.AssetGuid,
            ["caster"] = spell.Caster?.Unit?.UniqueId, ["spellbook"] = Identity(spell.Spellbook),
            ["slot"] = Identity(spell.ParamSpellSlot), ["convertedFrom"] = Identity(spell.ConvertedFrom),
            ["sourceItem"] = Identity(spell.SourceItem), ["sourceItemBlueprint"] = spell.SourceItem?.Blueprint.AssetGuid,
            ["sourceItemCharges"] = spell.SourceItem?.Charges, ["spellLevel"] = spell.SpellLevel,
            ["actionType"] = spell.ActionType.ToString(), ["runtimeActionType"] = spell.RuntimeActionType.ToString(),
            ["fullRound"] = spell.RequireFullRoundAction, ["available"] = spell.IsAvailable,
            ["availableForCast"] = spell.IsAvailableForCast
        };
        internal JObject Shell(UnitUseAbility command) => new JObject {
            ["identity"] = Identity(command), ["executor"] = command.Executor?.UniqueId,
            ["spell"] = Ability(command.Spell), ["started"] = command.IsStarted,
            ["acted"] = command.IsActed, ["finished"] = command.IsFinished, ["result"] = command.Result.ToString(),
            ["commandActionType"] = command.Type.ToString(), ["ignoreCooldown"] = command.IsIgnoreCooldown,
            ["timeSinceStart"] = command.TimeSinceStart,
            ["process"] = Identity(command.ExecutionProcess), ["processEnded"] = command.ExecutionProcess?.IsEnded,
            ["riderContainer"] = rider.Commands.Raw.Concat(rider.Commands.Queue).Any(c => ReferenceEquals(c, command)),
            ["mountContainer"] = mount.Commands.Raw.Concat(mount.Commands.Queue).Any(c => ReferenceEquals(c, command))
        };
        private bool Pair(UnitEntityData unit) => unit == rider || unit == mount;
        private JObject Record(string kind, object value, UnitEntityData actor)
        {
            var entry = new JObject { ["kind"] = kind, ["caseId"] = caseId, ["identity"] = Identity(value),
                ["actor"] = actor?.UniqueId, ["frame"] = Time.frameCount,
                ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks };
            if (events.Count < 2048) events.Add(entry); else dropped++;
            return entry;
        }
        private void ObserveShell(string kind, UnitUseAbility shell)
        {
            if (!Pair(shell.Executor)) return;
            if (!shells.Contains(shell)) shells.Add(shell);
            var process = shell.ExecutionProcess;
            if (process != null && !processes.Contains(process)) processes.Add(process);
            Record(kind, shell, shell.Executor)["shell"] = Shell(shell);
        }
        private static void Guard(Action<NativeCastingItemTrace> body)
        {
            var owner = active;
            if (owner == null || owner.disposed) return;
            try { body(owner); } catch { owner.faults++; }
        }
        private static void ShellStartBefore(UnitUseAbility __instance) => Guard(t => t.ObserveShell("start-before", __instance));
        private static void ShellStartAfter(UnitUseAbility __instance) => Guard(t => t.ObserveShell("start-after", __instance));
        private static void ShellActionBefore(UnitUseAbility __instance) => Guard(t => t.ObserveShell("action-before", __instance));
        private static void ShellActionAfter(UnitUseAbility __instance) => Guard(t => t.ObserveShell("action-after", __instance));
        private static void SpellSpendBefore(AbilityData __instance) => SpellSpend("spell-spend-before", __instance);
        private static void SpellSpendAfter(AbilityData __instance) => SpellSpend("spell-spend-after", __instance);
        private static void SpellSpend(string kind, AbilityData spell) => Guard(t => {
            if (t.Pair(spell.Caster?.Unit)) t.Record(kind, spell, spell.Caster.Unit)["ability"] = t.Ability(spell);
        });
        private static void ItemSpendBefore(ItemEntity __instance, UnitDescriptor user) => ItemSpend("item-spend-before", __instance, user);
        private static void ItemSpendAfter(ItemEntity __instance, UnitDescriptor user, bool __result) => ItemSpend("item-spend-after", __instance, user, __result);
        private static void ItemSpend(string kind, ItemEntity item, UnitDescriptor user, bool? result = null) => Guard(t => {
            if (!t.Pair(user?.Unit)) return;
            var entry = t.Record(kind, item, user.Unit);
            entry["blueprint"] = item.Blueprint.AssetGuid; entry["charges"] = item.Charges;
            entry["count"] = item.Count; entry["result"] = result;
        });
        private static void ConcentrationBefore(UnitUseAbility __instance) => Guard(t => t.ObserveShell("concentration-before", __instance));
        private static void ConcentrationAfter(UnitUseAbility __instance) => Guard(t => t.ObserveShell("concentration-after", __instance));
        private static void DefensiveBefore(UnitUseAbility __instance) => Guard(t => t.ObserveShell("defensive-before", __instance));
        private static void DefensiveAfter(UnitUseAbility __instance, bool __result) => Guard(t => {
            if (t.Pair(__instance.Executor)) t.Record("defensive-after", __instance, __instance.Executor)["nativeResult"] = __result;
        });
        public void OnEventAboutToTrigger(RuleCastSpell evt)
        {
            Guard(t => {
            if (!Pair(evt.Spell.Caster.Unit)) return;
            var entry = Record("cast-before", evt, evt.Spell.Caster.Unit);
            entry["ability"] = Ability(evt.Spell); entry["target"] = evt.SpellTarget?.Unit?.UniqueId;
                    });
        }
        public void OnEventDidTrigger(RuleCastSpell evt)
        {
            Guard(t => {
            if (!Pair(evt.Spell.Caster.Unit)) return;
            var process = evt.ExecutionProcess;
            if (process != null && !processes.Contains(process)) processes.Add(process);
            var entry = Record("cast-after", evt, evt.Spell.Caster.Unit);
            entry["ability"] = Ability(evt.Spell); entry["target"] = evt.SpellTarget?.Unit?.UniqueId;
            entry["process"] = Identity(process); entry["context"] = Identity(process?.Context);
            entry["spellFailed"] = evt.IsSpellFailed; entry["arcaneFailed"] = evt.IsArcaneSpellFailed;
                    });
        }
        public void OnEventAboutToTrigger(RuleDealDamage evt) { }
        public void OnEventDidTrigger(RuleDealDamage evt)
        {
            Guard(t => {
            if (!Pair(evt.Initiator) && !Pair(evt.Target)) return;
            var entry = Record("damage", evt, evt.Initiator);
            entry["target"] = evt.Target.UniqueId; entry["damage"] = evt.Damage;
            entry["sourceAbility"] = evt.SourceAbility?.AssetGuid;
                    });
        }
        public void OnEventAboutToTrigger(RuleHealDamage evt) { }
        public void OnEventDidTrigger(RuleHealDamage evt)
        {
            Guard(t => {
            if (!Pair(evt.Initiator) && !Pair(evt.Target)) return;
            var entry = Record("heal", evt, evt.Initiator);
            entry["target"] = evt.Target.UniqueId; entry["value"] = evt.Value;
                    });
        }
        public void OnEventAboutToTrigger(RuleCheckConcentration evt) => Guard(t => {
            if (t.Pair(evt.Initiator)) t.Record("concentration-rule-before", evt, evt.Initiator)["dc"] = evt.DC;
        });
        public void OnEventDidTrigger(RuleCheckConcentration evt) => Guard(t => {
            if (!t.Pair(evt.Initiator)) return;
            var entry = t.Record("concentration-rule-after", evt, evt.Initiator);
            entry["dc"] = evt.DC; entry["roll"] = evt.ResultRoll; entry["success"] = evt.Success;
        });
        public void OnEventAboutToTrigger(RuleCheckCastingDefensively evt) => Guard(t => {
            if (t.Pair(evt.Initiator)) t.Record("defensive-rule-before", evt, evt.Initiator)["dc"] = evt.DC;
        });
        public void OnEventDidTrigger(RuleCheckCastingDefensively evt) => Guard(t => {
            if (!t.Pair(evt.Initiator)) return;
            var entry = t.Record("defensive-rule-after", evt, evt.Initiator);
            entry["dc"] = evt.DC; entry["roll"] = evt.ResultRoll; entry["success"] = evt.Success;
        });
        public void OnEventAboutToTrigger(RuleSummonUnit evt) { }
        public void OnEventDidTrigger(RuleSummonUnit evt) => Guard(t => {
            if (!t.Pair(evt.Initiator)) return;
            var unit = evt.SummonedUnit;
            if (unit != null && !t.summons.Contains(unit)) t.summons.Add(unit);
            var entry = t.Record("summon", evt, evt.Initiator);
            entry["unit"] = unit?.UniqueId; entry["unitObject"] = t.Identity(unit);
            entry["blueprint"] = unit?.Blueprint.AssetGuid; entry["context"] = t.Identity(evt.Context);
        });
        private void Patch(Type type, int token, string prefix, string postfix)
        {
            var target = type.Module.ResolveMethod(token) as MethodInfo;
            if (target == null || target.DeclaringType != type) throw new InvalidOperationException("Native casting hook differs.");
            var flags = BindingFlags.NonPublic | BindingFlags.Static;
            harmony.Patch(target, new HarmonyMethod(typeof(NativeCastingItemTrace).GetMethod(prefix, flags)),
                new HarmonyMethod(typeof(NativeCastingItemTrace).GetMethod(postfix, flags)));
            hooks.Add(new JObject { ["type"] = type.FullName, ["method"] = target.Name,
                ["token"] = token, ["mvid"] = type.Assembly.ManifestModule.ModuleVersionId.ToString() });
        }
        public void Dispose()
        {
            if (disposed) return;
            closed = Capture();
            harmony.UnpatchAll(HarmonyId); subscription?.Dispose();
            active = null; disposed = true; identities.Dispose();
            closed["identityRegistry"]["retainedCount"] = 0;
            closed["identityRegistry"]["released"] = true;
        }
    }
}