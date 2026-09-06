using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Harmony12;
using Kingmaker;
using Kingmaker.Controllers.Clicks;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.Controllers.Units;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // Installed only by the guarded disposable fixture. These hooks observe native
    // calls; they never alter arguments, results, commands, modes or actor state.
    internal sealed class NativeOrdinaryAttackTrace : IDisposable
    {
        private const string HarmonyId = "KingmakerMountedCombat.Diagnostics.OrdinaryAttackTrace";
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        private static NativeOrdinaryAttackTrace active;
        private readonly HarmonyInstance harmony;
        private readonly UnitEntityData rider;
        private readonly UnitEntityData mount;
        private readonly MountedCombatController combat;
        private readonly JArray events = new JArray();
        private readonly Dictionary<UnitAttack, JObject> nativeRecoveryInterrupts = new Dictionary<UnitAttack, JObject>();
        private string caseId;
        private int dropped;
        internal UnitAttack LastStartedRiderAttack { get; private set; }

        internal NativeOrdinaryAttackTrace(UnitEntityData rider, UnitEntityData mount, MountedCombatController combat)
        {
            if (active != null) throw new InvalidOperationException("An ordinary attack trace is already active.");
            this.rider = rider;
            this.mount = mount;
            this.combat = combat;
            harmony = HarmonyInstance.Create(HarmonyId);
            active = this;
            try
            {
                Patch(typeof(TurnController), "UpdateActionPredictions", 0x06000C6E, "PredictionBefore", "PredictionAfter");
                Patch(typeof(TurnController), "SetAttackMode", 0x06000C3F, "ModeBefore", "ModeAfter");
                Patch(typeof(PointerController), "SimulateClick", 0x060093C7, "SimulationBefore", "SimulationAfter");
                Patch(typeof(ClickUnitHandler), "OnClick", 0x060093ED, "ClickBefore", "ClickAfter");
                Patch(typeof(UnitCommands), "Run", 0x060026B2, "RunBefore", "RunAfter");
                Patch(typeof(UnitAttack), "InitAttacks", 0x0600267C, "PlanBefore", "PlanAfter");
                Patch(typeof(UnitAttack), "OnStart", 0x0600267E, "StartBefore", "StartAfter");
                Patch(typeof(UnitAttack), "OnAction", 0x06002681, "DeliveryBefore", "DeliveryAfter");
                Patch(typeof(UnitAttack), "UpdateTarget", 0x06002683, null, "TargetAfter");
                Patch(typeof(UnitCommand), "Interrupt", 0x060027AC, "InterruptBefore", null);
                Patch(typeof(UnitCommand), "OnEnded", 0x060027B2, null, "Ended");
                Patch(typeof(UnitActionController), "UpdateCooldowns", 0x06009120, "CostBefore", "CostAfter");
            }
            catch { Dispose(); throw; }
        }

        internal void BeginCase(string value) { caseId = value; LastStartedRiderAttack = null; Record("fixture-case", rider); }
        internal JObject Capture() => new JObject { ["events"] = events.DeepClone(), ["dropped"] = dropped };
        internal JObject NativeRecoveryInterrupt(UnitAttack command) => command != null && nativeRecoveryInterrupts.ContainsKey(command)
            ? (JObject)nativeRecoveryInterrupts[command].DeepClone() : null;
        internal JObject NativeRangeRejection(UnitAttack command)
        {
            var observation = events.OfType<JObject>().LastOrDefault(item => (string)item["boundary"] == "target-invalid" &&
                (int?)item["command"] == Identity(command));
            return observation == null ? null : (JObject)observation.DeepClone();
        }

        public void Dispose()
        {
            harmony.UnpatchAll(HarmonyId);
            if (ReferenceEquals(active, this)) active = null;
        }

        private void Patch(Type type, string name, int token, string prefix, string postfix)
        {
            var method = type.GetMethods(Flags).SingleOrDefault(item => item.Name == name && item.MetadataToken == token);
            if (method == null) throw new MissingMethodException(type.FullName, name + " exact token " + token.ToString("X8"));
            var before = prefix == null ? null : new HarmonyMethod(typeof(Hooks).GetMethod(prefix, Flags));
            if (before != null) before.prioritiy = Priority.First; // Exact Harmony12 spelling.
            harmony.Patch(method, before, postfix == null ? null : new HarmonyMethod(typeof(Hooks).GetMethod(postfix, Flags)));
        }

        private bool Owns(UnitEntityData actor) => actor != null && (actor == rider || actor == mount);
        private UnitEntityData Owner(UnitCommands commands) => commands == rider.Commands ? rider : commands == mount.Commands ? mount : null;
        private static int Identity(object value) => value == null ? 0 : RuntimeHelpers.GetHashCode(value);
        private static object Field(object value, string name) => value?.GetType().GetField(name, Flags)?.GetValue(value);

        private void Record(string boundary, UnitEntityData actor, UnitCommand command = null, string detail = null)
        {
            if (caseId == null || !Owns(actor)) return;
            if (events.Count >= 8192) { dropped++; return; }
            try
            {
                var turn = Game.Instance?.TurnBasedCombatController?.CurrentTurn;
                var pointer = Game.Instance?.DefaultPointerController;
                var attack = command as UnitAttack;
                if (boundary == "start-after" && actor == rider) LastStartedRiderAttack = attack;
                var row = new JObject {
                    ["index"] = events.Count, ["caseId"] = caseId, ["boundary"] = boundary,
                    ["frame"] = Time.frameCount, ["gameTime"] = Game.Instance.TimeController.GameTime.Ticks,
                    ["simulation"] = PointerController.SimulatingClick, ["inputKind"] = "NATIVE INTEGRATION",
                    ["actor"] = actor.UniqueId, ["turn"] = Identity(turn), ["turnActor"] = turn?.Unit?.UniqueId,
                    ["turnStatus"] = turn?.Status.ToString(), ["fullEnabled"] = turn?.EnabledFullAttack,
                    ["attackMode"] = Field(turn, "m_AttackMode")?.ToString(),
                    ["manualMode"] = Field(turn, "m_SmartCursorChangedManually")?.ToString(),
                    ["needPrediction"] = Field(turn, "m_NeedNewPredictions")?.ToString(),
                    ["highlighted"] = (Field(turn, "m_HighlightedUnit") as UnitEntityData)?.UniqueId,
                    ["pointerOn"] = pointer?.PointerOn?.name,
                    ["simulationHandler"] = Field(pointer, "m_SimulateClickHandler")?.GetType().FullName,
                    ["standard"] = actor.CombatState.Cooldown.StandardAction,
                    ["move"] = actor.CombatState.Cooldown.MoveAction,
                    ["restrictedByMove"] = actor.CombatState.IsFullAttackRestrictedBecauseOfMoveAction,
                    ["command"] = Identity(command), ["commandType"] = command?.GetType().FullName,
                    ["executor"] = command?.Executor?.UniqueId, ["target"] = attack?.Target?.UniqueId,
                    ["createdByPlayer"] = command?.CreatedByPlayer, ["started"] = command?.IsStarted,
                    ["acted"] = command?.IsActed, ["finished"] = command?.IsFinished, ["result"] = command?.Result.ToString(),
                    ["timeSinceStart"] = command?.TimeSinceStart,
                    ["actorPosition"] = new JArray(actor.Position.x, actor.Position.y, actor.Position.z),
                    ["targetPosition"] = attack?.Target == null ? null : new JArray(attack.Target.Position.x, attack.Target.Position.y, attack.Target.Position.z),
                    ["targetDead"] = attack?.Target?.Descriptor.State.IsDead,
                    ["targetUnconscious"] = attack?.Target?.Descriptor.State.IsUnconscious,
                    ["targetHp"] = attack?.Target?.Stats.HitPoints.ModifiedValue,
                    ["targetDamage"] = attack?.Target?.Damage,
                    ["targetTemporaryHp"] = attack?.Target?.Stats.TemporaryHitPoints.ModifiedValue,
                    ["actorCanAct"] = actor.Descriptor.State.CanAct,
                    ["interruptPending"] = command?.InterruptAsSoonAsPossible,
                    ["commands"] = Identity(actor.Commands),
                    ["raw"] = new JArray(actor.Commands.Raw.Select(Identity)),
                    ["queue"] = new JArray(actor.Commands.Queue.Select(Identity)),
                    ["intentStarts"] = combat.StockAttackIntentStartCount,
                    ["duplicateDispatches"] = combat.StockAttackDuplicateDispatchCount,
                    ["single"] = attack?.IsSingleAttack, ["full"] = attack?.IsFullAttack,
                    ["planned"] = attack?.AllAttacks.Count, ["completed"] = attack?.GetAttackIndex(),
                    ["plannedWeapon"] = attack?.PlannedAttack?.Weapon?.Blueprint.AssetGuid,
                    ["plannedWeaponRange"] = attack?.PlannedAttack?.WeaponRange,
                    ["pairApproachRadius"] = (attack as MountedPairSingleAttack)?.PairApproachRadius,
                    ["detail"] = detail
                };
                if (command?.Executor != null)
                {
                    row["shouldApproach"] = command.ShouldUnitApproach;
                    row["approachRadius"] = command.ApproachRadius;
                    row["enoughClose"] = command.IsUnitEnoughClose;
                    row["shouldInterrupt"] = command.ShouldBeInterrupted;
                    if (attack?.Target != null)
                    {
                        var origin = attack is MountedPairSingleAttack && actor == rider ? mount : actor;
                        row["rangeOriginDistance"] = Kingmaker.Utility.GeometryUtils.MechanicsDistance(origin.Position, attack.Target.Position);
                        row["targetInState"] = attack.Target.IsInState;
                        row["targetUntargetable"] = UnitCommand.CommandTargetUntargetable(actor, attack.Target);
                        row["targetStealth"] = attack.Target.InStealthFor(actor.Group);
                    }
                }
                if (attack != null)
                    row["plan"] = new JArray(attack.AllAttacks.Select(item => new JObject {
                        ["weapon"] = item.Weapon?.Blueprint.AssetGuid, ["penalty"] = item.AttackBonusPenalty,
                        ["ranged"] = item.Weapon?.Blueprint.IsRanged, ["natural"] = item.Weapon?.Blueprint.IsNatural
                    }));
                events.Add(row);
                if (boundary == "native-recovery-interrupt" && attack != null) nativeRecoveryInterrupts[attack] = row;
            }
            catch (Exception exception)
            {
                events.Add(new JObject { ["caseId"] = caseId, ["boundary"] = boundary, ["observationError"] = exception.ToString() });
            }
        }

        private static class Hooks
        {
            internal static void PredictionBefore(TurnController __instance) { active?.Record("prediction-before", __instance.Unit); }
            internal static void PredictionAfter(TurnController __instance) { active?.Record("prediction-after", __instance.Unit); }
            internal static void ModeBefore(TurnController __instance, int mode, bool force)
            { active?.Record("mode-before", __instance.Unit, null, "requested=" + mode + ";force=" + force + ";caller=" + new StackTrace(1, false)); }
            internal static void ModeAfter(TurnController __instance) { active?.Record("mode-after", __instance.Unit); }
            internal static void SimulationBefore() { active?.Record("simulation-before", Game.Instance.TurnBasedCombatController.CurrentTurn?.Unit); }
            internal static void SimulationAfter() { active?.Record("simulation-after", Game.Instance.TurnBasedCombatController.CurrentTurn?.Unit); }
            internal static void ClickBefore(bool simulate, bool muteEvents)
            { active?.Record("hostile-handler-before", Game.Instance.TurnBasedCombatController.CurrentTurn?.Unit ?? active.rider, null, "simulate=" + simulate + ";muteEvents=" + muteEvents); }
            internal static void ClickAfter() { active?.Record("hostile-handler-after", Game.Instance.TurnBasedCombatController.CurrentTurn?.Unit ?? active.rider); }
            internal static void RunBefore(UnitCommands __instance, UnitCommand cmd) { active?.Record("run-before", active.Owner(__instance), cmd); }
            internal static void RunAfter(UnitCommands __instance, UnitCommand cmd) { active?.Record("run-after", active.Owner(__instance), cmd); }
            internal static void PlanBefore(UnitAttack __instance) { active?.Record("plan-before", __instance.Executor, __instance); }
            internal static void PlanAfter(UnitAttack __instance) { active?.Record("plan-after", __instance.Executor, __instance); }
            internal static void StartBefore(UnitAttack __instance) { active?.Record("start-before", __instance.Executor, __instance); }
            internal static void StartAfter(UnitAttack __instance) { active?.Record("start-after", __instance.Executor, __instance); }
            internal static void DeliveryBefore(UnitAttack __instance) { active?.Record("delivery-before", __instance.Executor, __instance); }
            internal static void DeliveryAfter(UnitAttack __instance) { active?.Record("delivery-after", __instance.Executor, __instance); }
            internal static void TargetAfter(UnitAttack __instance, bool __result)
            { if (!__result) active?.Record("target-invalid", __instance.Executor, __instance); }
            internal static void InterruptBefore(UnitCommand __instance)
            {
                if (!__instance.IsStarted || __instance.IsFinished) return;
                var stack = new StackTrace(1, false);
                active?.Record("interrupt-request", __instance.Executor, __instance, stack.ToString());
                var attack = __instance as UnitAttack;
                if (__instance.GetType() == typeof(UnitAttack) && __instance.Result == UnitCommand.ResultType.Success &&
                    attack.PlannedAttack == null && attack.AllAttacks.Count > 0 && attack.GetAttackIndex() == attack.AllAttacks.Count &&
                    (stack.GetFrames() ?? new StackFrame[0]).Any(frame => frame.GetMethod().DeclaringType == typeof(UnitAttack) && frame.GetMethod().Name == "OnTick"))
                    active?.Record("native-recovery-interrupt", __instance.Executor, __instance, stack.ToString());
            }
            internal static void Ended(UnitCommand __instance) { active?.Record("ended", __instance.Executor, __instance); }
            internal static void CostBefore(UnitCommand command) { active?.Record("cost-before", command.Executor, command); }
            internal static void CostAfter(UnitCommand command) { active?.Record("cost-after", command.Executor, command); }
        }
    }
}
