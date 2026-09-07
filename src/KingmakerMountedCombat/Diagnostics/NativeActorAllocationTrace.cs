using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Harmony12;
using Kingmaker;
using Kingmaker.Controllers.Combat;
using Kingmaker.Controllers.Units;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.View;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // Scoped observation only. Installed by the guarded disposable fixture, with
    // rollback on a missing exact hook. Never changes a native argument or result.
    internal sealed class NativeActorAllocationTrace : IDisposable
    {
        private const string HarmonyId = "KingmakerMountedCombat.Diagnostics.ActorAllocation";
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        private static NativeActorAllocationTrace active;
        private static readonly JsonSerializer TraceSerializer = new JsonSerializer { ContractResolver = new DefaultContractResolver() };
        private readonly HarmonyInstance harmony;
        private readonly UnitEntityData rider;
        private readonly UnitEntityData mount;
        private readonly MountedCombatController combat;
        private readonly JArray events = new JArray();
        private readonly Dictionary<UnitEntityData, int> grants = new Dictionary<UnitEntityData, int>();
        private readonly Dictionary<UnitEntityData, float> movedTime = new Dictionary<UnitEntityData, float>();
        private readonly Dictionary<UnitEntityData, float> requestedTime = new Dictionary<UnitEntityData, float>();
        private readonly Dictionary<UnitEntityData, float> travelledDistance = new Dictionary<UnitEntityData, float>();
        private readonly Dictionary<UnitEntityData, float> nativeShiftDistance = new Dictionary<UnitEntityData, float>();
        private UnitMovementAgent physicalMovement;
        private TurnController preparing;
        private string encounter;
        private int dropped;
        private int observationErrors;

        internal NativeActorAllocationTrace(UnitEntityData rider, UnitEntityData mount, MountedCombatController combat)
        {
            if (active != null) throw new InvalidOperationException("An allocation trace is already active.");
            this.rider = rider; this.mount = mount; this.combat = combat;
            if (typeof(UnitEntityData).Assembly.ManifestModule.ModuleVersionId != new Guid("07fa1e4d-8618-41b3-9b8d-faa17d3b26f7"))
                throw new InvalidOperationException("Allocation trace requires the exact installed Kingmaker MVID.");
            harmony = HarmonyInstance.Create(HarmonyId); active = this;
            try
            {
                Patch(typeof(TurnController), 0x06000C3C, "PrepareBefore", "PrepareAfter");
                Patch(typeof(UnitCombatState.Cooldowns), 0x0600C3BE, "ClearBefore", "ClearAfter");
                Patch(typeof(UnitCombatState), 0x0600939D, "RoundBefore", "RoundAfter");
                Patch(typeof(TurnController), 0x06000C7F, "RoundHandlerBefore", "RoundHandlerAfter");
                Patch(typeof(TurnController), 0x06000C80, "ReadyHandlerBefore", "ReadyHandlerAfter");
                Patch(typeof(CombatAiData), 0x06001E0B, "AiRoundBefore", "AiRoundAfter");
                Patch(typeof(TurnController).GetNestedType("<>c", Flags), 0x0600A2D2, "FactBefore", "FactAfter");
                Patch(typeof(UnitCommands), 0x060026B2, "CommandBefore", "CommandAfter");
                Patch(typeof(UnitActionController), 0x06009120, "CostBefore", "CostAfter");
                Patch(typeof(UnitActionController), 0x0600911D, "EligibilityBefore", "EligibilityAfter");
                Patch(typeof(UnitEntityData), 0x0600838F, "ActorCostBefore", "ActorCostAfter");
                Patch(typeof(TurnController), 0x06000C5E, "CommandEndBefore", "CommandEndAfter");
                Patch(typeof(TurnController), 0x06000C46, "TurnEndBefore", "TurnEndAfter");
                Patch(typeof(UnitMovementAgent), 0x060018A9, "MovementBefore", "MovementAfter");
                Patch(typeof(UnitMovementAgent), 0x060018AA, "PhysicalTickBefore", "PhysicalTickAfter");
                Patch(typeof(UnitMovementAgentBase), 0x060018DB, "PhysicalMoveBefore", "PhysicalMoveAfter");
            }
            catch { Dispose(); throw; }
        }

        internal void BeginEncounter(string id) { encounter = id; Record("encounter-setup", rider); }
        internal JObject Capture() => new JObject { ["events"] = events.DeepClone(), ["dropped"] = dropped, ["observationErrors"] = observationErrors };
        internal int GrantCount(UnitEntityData actor) => grants.ContainsKey(actor) ? grants[actor] : 0;
        internal JObject Snapshot(UnitEntityData actor)
        {
            var turn = Game.Instance?.TurnBasedCombatController?.CurrentTurn;
            var cooldown = actor.CombatState.Cooldown;
            var nativeTurn = turn?.Unit == actor ? turn : combat.PairedPartnerContext?.Unit == actor ? combat.PairedPartnerContext : null;
            return new JObject {
                ["actor"] = actor.UniqueId, ["actorObject"] = Id(actor), ["grantSequence"] = GrantCount(actor),
                ["grantSequenceKind"] = "observed-native-Prepare-entries",
                ["pairedGrantIdentity"] = actor == rider || actor == mount ? combat.PairedActivationIdentity : null,
                ["standard"] = cooldown.StandardAction, ["move"] = cooldown.MoveAction, ["swift"] = cooldown.SwiftAction,
                ["initiativeCooldown"] = cooldown.Initiative, ["initiative"] = actor.CombatState.Initiative,
                ["reactionCooldown"] = cooldown.AttackOfOpportunity, ["reactions"] = actor.CombatState.AttackOfOpportunityCount,
                ["disengageTargets"] = actor.CombatState.DisengageAttackTargets.Count,
                ["prepared"] = actor.CombatState.Prepared, ["canAct"] = actor.CombatState.CanActInCombat,
                ["prone"] = actor.Descriptor.State.Prone.Active,
                ["proneRequested"] = actor.Descriptor.State.Prone.ShouldBeActive,
                ["maneuverImmune"] = actor.Descriptor.State.HasCondition(Kingmaker.UnitLogic.UnitCondition.ImmuneToCombatManeuvers),
                ["timeToNextNativeTurn"] = actor.GetTimeToNextTurn(), ["damage"] = actor.Damage,
                ["hasStandard"] = actor.HasStandardAction(), ["usedStandard"] = actor.UsedStandardAction(),
                ["moveRestricted"] = actor.IsMoveActionRestricted(), ["speedMps"] = actor.CurrentSpeedMps,
                ["stepRangeMetres"] = TurnController.MetersOfFiveFootStep,
                ["agentPacing"] = AgentPacing(actor),
                ["timeMoved"] = nativeTurn?.TimeMoved, ["timeForced"] = nativeTurn?.TimeMovedInForceMode,
                ["timeStepped"] = nativeTurn?.TimeMovedByFiveFootStep, ["metresStepped"] = nativeTurn?.MetersMovedByFiveFootStep,
                ["stepImmune"] = nativeTurn?.ImmuneAttackOfOpportunityOnDisengage,
                ["movementLimit"] = nativeTurn?.CurrentMovementLimit.ToString(),
                ["actorContextStatus"] = nativeTurn?.Status.ToString(),
                ["remainingNativeTime"] = nativeTurn?.GetRemainingTime(),
                ["measuredAllowedTime"] = movedTime.ContainsKey(actor) ? movedTime[actor] : 0f,
                ["measuredRequestedTime"] = requestedTime.ContainsKey(actor) ? requestedTime[actor] : 0f,
                ["measuredTravelDistance"] = travelledDistance.ContainsKey(actor) ? travelledDistance[actor] : 0f,
                ["measuredNativeShiftDistance"] = nativeShiftDistance.ContainsKey(actor) ? nativeShiftDistance[actor] : 0f,
                ["position"] = new JArray(actor.Position.x, actor.Position.y, actor.Position.z),
                ["retained"] = Retained(actor), ["nativeRoundHit"] = actor.CombatState.HitThisRound,
                ["nativeRoundAttacks"] = actor.CombatState.ExecutedAttackNumber
            };
        }

        private static JObject AgentPacing(UnitEntityData actor)
        {
            var agent = actor.View?.AgentASP;
            if (agent == null) return null;
            var state = new JObject();
            foreach (var name in new[] { "m_MinSpeed", "m_SavedMinSpeed", "m_WarmupTime", "m_SlowDownTime", "m_IsInForceMode" })
            {
                var value = typeof(UnitMovementAgent).GetField(name, Flags).GetValue(agent);
                state[name] = value == null ? JValue.CreateNull() : JToken.FromObject(value, TraceSerializer);
            }
            return state;
        }

        // Reflection is confined to this observer; looking up a record must not
        // call the production Get method, which can create a new allocation.
        private JObject Retained(UnitEntityData actor)
        {
            var coordinator = typeof(MountedCombatController).GetField("unifiedTurn", Flags).GetValue(combat);
            var adapter = coordinator.GetType().GetField("movementState", Flags).GetValue(coordinator);
            var records = (IDictionary)adapter.GetType().GetField("allocations", Flags).GetValue(adapter);
            var record = records[actor];
            if (record == null) return null;
            var result = new JObject();
            foreach (var field in record.GetType().GetFields(Flags))
            {
                var value = field.GetValue(record);
                result[field.Name] = field.Name == "Controller" ? new JValue(Id(value)) : value == null ? JValue.CreateNull() : JToken.FromObject(value, TraceSerializer);
            }
            return result;
        }

        internal void Record(string boundary, UnitEntityData actor, UnitCommand command = null, string detail = null, object callback = null)
        {
            if (actor == null || actor.Group != rider.Group && (!combat.PairedActivationEnabled || !actor.IsInCombat)) return;
            if (events.Count >= 16000) { dropped++; return; }
            try
            {
                var controller = Game.Instance.TurnBasedCombatController;
                var turn = controller.CurrentTurn;
                events.Add(new JObject {
                    ["sequence"] = events.Count + 1, ["encounter"] = encounter, ["session"] = Id(Game.Instance.Player),
                    ["activationIdentity"] = combat.PairedActivationIdentity,
                    ["boundary"] = boundary, ["frame"] = Time.frameCount, ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks,
                    ["simulatingClick"] = Kingmaker.Controllers.Clicks.PointerController.SimulatingClick,
                    ["controller"] = Id(controller), ["round"] = controller.RoundNumber, ["roundStartTicks"] = controller.RoundStartTime.Ticks,
                    ["turn"] = Id(turn), ["preparingTurn"] = Id(preparing), ["currentActor"] = turn?.Unit?.UniqueId,
                    ["turnStatus"] = turn?.Status.ToString(), ["state"] = Snapshot(actor), ["detail"] = detail,
                    ["callbackObject"] = Id(callback),
                    ["command"] = Id(command), ["commandType"] = command?.GetType().FullName,
                    ["commandActor"] = command?.Executor?.UniqueId, ["started"] = command?.IsStarted,
                    ["acted"] = command?.IsActed, ["finished"] = command?.IsFinished, ["result"] = command?.Result.ToString(),
                    ["ignoreCooldown"] = command?.IsIgnoreCooldown,
                    ["commandInitialized"] = command?.Executor != null,
                    ["shouldApproach"] = command?.Executor?.View != null && command.Target != null
                        ? (bool?)command.ShouldUnitApproach : null,
                    ["feedback"] = combat.LastFeedback
                });
            }
            catch (Exception exception) { observationErrors++; events.Add(new JObject { ["boundary"] = boundary, ["observationError"] = exception.ToString() }); }
        }

        public void Dispose() { harmony.UnpatchAll(HarmonyId); if (ReferenceEquals(active, this)) active = null; }
        private static int Id(object value) => value == null ? 0 : RuntimeHelpers.GetHashCode(value);
        private void Patch(Type type, int token, string prefix, string postfix)
        {
            var method = type?.GetMethods(Flags).SingleOrDefault(item => item.MetadataToken == token);
            if (method == null) throw new MissingMethodException(type?.FullName, token.ToString("X8"));
            // Observe actual native entry after the production reconciliation
            // prefix; subsequent native round/fact/readiness callbacks are also
            // recorded independently, with the real resources and effects.
            var before = new HarmonyMethod(typeof(Hooks).GetMethod(prefix, Flags)) { prioritiy = prefix == "RoundBefore" ? Priority.Last : Priority.First };
            var after = new HarmonyMethod(typeof(Hooks).GetMethod(postfix, Flags)) { prioritiy = Priority.Last };
            harmony.Patch(method, before, after);
        }
        private UnitEntityData Owner(UnitCommands commands) => commands == rider.Commands ? rider : commands == mount.Commands ? mount : null;
        private static class Hooks
        {
            internal static void PhysicalTickBefore(UnitMovementAgent __instance, out UnitMovementAgent __state)
            {
                __state = active?.physicalMovement;
                if (active == null) return;
                var actor = __instance.Unit?.EntityData;
                active.physicalMovement = CombatController.IsInTurnBasedCombat() &&
                    (actor == active.rider || actor == active.mount) ? __instance : null;
            }
            internal static void PhysicalTickAfter(UnitMovementAgent __state)
            { if (active != null) active.physicalMovement = __state; }
            internal static void PhysicalMoveBefore(UnitMovementAgentBase __instance, out Vector3 __state)
            { __state = active?.physicalMovement == __instance ? __instance.transform.position : Vector3.zero; }
            internal static void PhysicalMoveAfter(UnitMovementAgentBase __instance, Vector3 shift, Vector3 __state)
            {
                if (active == null || active.physicalMovement != __instance) return;
                var actor = __instance.Unit.EntityData;
                var delivered = __instance.transform.position - __state;
                var distance = new Vector2(delivered.x, delivered.z).magnitude;
                var requested = new Vector2(shift.x, shift.z).magnitude;
                active.travelledDistance[actor] = (active.travelledDistance.ContainsKey(actor) ? active.travelledDistance[actor] : 0f) + distance;
                active.nativeShiftDistance[actor] = (active.nativeShiftDistance.ContainsKey(actor) ? active.nativeShiftDistance[actor] : 0f) + requested;
                // Native Move includes both the velocity shift and its final
                // endpoint correction. Sum actual segments, never a straight
                // line between input and destination or a guessed speed.
                active.Record("native-movement-displacement", actor,
                    detail: "requestedMetres=" + requested.ToString("R") + ";deliveredMetres=" + distance.ToString("R"));
            }
            internal static void PrepareBefore(TurnController __instance) { if (active == null) return; active.preparing = __instance; active.grants[__instance.Unit] = active.GrantCount(__instance.Unit) + 1; active.Record("prepare-before", __instance.Unit); }
            internal static void PrepareAfter(TurnController __instance) { active?.Record("prepare-after", __instance.Unit); if (active != null) active.preparing = null; }
            internal static void ClearBefore(UnitCombatState.Cooldowns __instance) { if (active?.preparing?.Unit.CombatState.Cooldown == __instance) active.Record("clear-before", active.preparing.Unit); }
            internal static void ClearAfter(UnitCombatState.Cooldowns __instance) { if (active?.preparing?.Unit.CombatState.Cooldown == __instance) active.Record("clear-after", active.preparing.Unit); }
            internal static void RoundBefore(UnitCombatState __instance) { active?.Record("round-state-before", __instance.Unit); }
            internal static void RoundAfter(UnitCombatState __instance) { active?.Record("round-state-after", __instance.Unit); }
            internal static void RoundHandlerBefore(TurnController __instance, IUnitNewCombatRoundHandler handler) { active?.Record("round-handler-before", __instance.Unit, detail: handler.GetType().FullName, callback: handler); }
            internal static void RoundHandlerAfter(TurnController __instance, IUnitNewCombatRoundHandler handler) { active?.Record("round-handler-after", __instance.Unit, detail: handler.GetType().FullName, callback: handler); }
            internal static void ReadyHandlerBefore(TurnController __instance, ITurnBasedModeHandler h) { active?.Record("ready-handler-before", __instance.Unit, detail: h.GetType().FullName, callback: h); }
            internal static void ReadyHandlerAfter(TurnController __instance, ITurnBasedModeHandler h) { active?.Record("ready-handler-after", __instance.Unit, detail: h.GetType().FullName, callback: h); }
            internal static void FactBefore(ITickEachRound logic) { active?.Record("fact-before", active.preparing?.Unit, detail: logic.GetType().FullName, callback: logic); }
            internal static void FactAfter(ITickEachRound logic) { active?.Record("fact-after", active.preparing?.Unit, detail: logic.GetType().FullName, callback: logic); }
            internal static void AiRoundBefore(CombatAiData __instance) { if (active?.preparing?.Unit.CombatState.AIData == __instance) active.Record("ai-round-before", active.preparing.Unit); }
            internal static void AiRoundAfter(CombatAiData __instance) { if (active?.preparing?.Unit.CombatState.AIData == __instance) active.Record("ai-round-after", active.preparing.Unit); }
            internal static void CommandBefore(UnitCommands __instance, UnitCommand cmd) { active?.Record("admission-before", active.Owner(__instance), cmd); }
            internal static void CommandAfter(UnitCommands __instance, UnitCommand cmd) { active?.Record("admission-after", active.Owner(__instance), cmd); }
            internal static void CostBefore(UnitCommand command) { active?.Record("cost-before", command?.Executor, command); }
            internal static void EligibilityBefore() { }
            internal static void EligibilityAfter(UnitCommand command, bool __result)
            {
                if (active != null && command?.Executor == active.mount && Time.frameCount % 30 == 0)
                    active.Record("command-eligibility", command.Executor, command, "nativeResult=" + __result);
            }
            internal static void CostAfter(UnitCommand command) { active?.Record("cost-after", command?.Executor, command); }
            internal static void ActorCostBefore(UnitEntityData __instance, UnitCommand command) { active?.Record("actor-cost-before", __instance, command); }
            internal static void ActorCostAfter(UnitEntityData __instance, UnitCommand command) { active?.Record("actor-cost-after", __instance, command); }
            internal static void CommandEndBefore(TurnController __instance, UnitCommand command) { active?.Record("command-end-before", command?.Executor, command, __instance.Unit.UniqueId); }
            internal static void CommandEndAfter(TurnController __instance, UnitCommand command) { active?.Record("command-end-after", command?.Executor, command, __instance.Unit.UniqueId); }
            internal static void TurnEndBefore(TurnController __instance) { active?.Record("turn-end-before", __instance.Unit); }
            internal static void TurnEndAfter(TurnController __instance) { active?.Record("turn-end-after", __instance.Unit); }
            internal static void MovementBefore(float deltaTime, out float __state) { __state = deltaTime; }
            internal static void MovementAfter(UnitMovementAgent __instance, float deltaTime, bool __result, float __state)
            {
                var actor = __instance.Unit?.EntityData;
                if (active == null || actor == null || actor != active.rider && actor != active.mount || !CombatController.IsInTurnBasedCombat()) return;
                active.requestedTime[actor] = (active.requestedTime.ContainsKey(actor) ? active.requestedTime[actor] : 0f) + __state;
                active.movedTime[actor] = (active.movedTime.ContainsKey(actor) ? active.movedTime[actor] : 0f) + (__result ? deltaTime : 0f);
                if (Time.frameCount % 15 == 0) active.Record("movement-tick", actor, detail: "requested=" + __state.ToString("R") + ";allowed=" + deltaTime.ToString("R") + ";result=" + __result);
            }
        }
    }
}
