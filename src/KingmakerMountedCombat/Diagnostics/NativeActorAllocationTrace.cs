using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Harmony12;
using Kingmaker;
using Kingmaker.Controllers.Combat;
using Kingmaker.Controllers.Units;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Parts;
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
    internal sealed partial class NativeActorAllocationTrace : IDisposable
    {
        private const string HarmonyId = "KingmakerMountedCombat.Diagnostics.ActorAllocation";
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        private static NativeActorAllocationTrace active;
        private static readonly JsonSerializer TraceSerializer = new JsonSerializer { ContractResolver = new DefaultContractResolver() };
        private HarmonyInstance harmony;
        private bool disposed, installed, installationStarted;
        private readonly UnitEntityData rider;
        private readonly UnitEntityData mount;
        private readonly MountedCombatController combat;
        private readonly MountedWorldService world;
        private readonly JArray events = new JArray();
        private readonly JArray installedHooks = new JArray();
        // Diagnostic-only ForceToEnd observations for every traced unit, kept beside the event
        // stream so existing boundary sequences are unchanged.
        private readonly JArray forceToEnd = new JArray();
        private readonly RetainedObjectIdentity identities = new RetainedObjectIdentity(16000);
        internal JArray ObserverHooks => (JArray)installedHooks.DeepClone();
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
        private int disablePrepareDepth;
        internal bool ObserveReactionResources { get; set; }
        internal bool ObserveGroundCooldownEligibility { get; set; }
        internal event Action<string, UnitEntityData, UnitCommand> BoundaryObserved;
        internal int EventCount => events.Count;
        internal JArray EventsSince(int offset) => new JArray(events.Skip(offset).Select(item => item.DeepClone()));
        internal bool Complete => installed && dropped == 0 && observationErrors == 0 && identities.FaultCount == 0 && disablePrepareDepth == 0;

        internal NativeActorAllocationTrace(UnitEntityData rider, UnitEntityData mount, MountedCombatController combat, bool deferInstallation = false)
        {
            this.rider = rider; this.mount = mount; this.combat = combat;
            world = combat?.TurnParticipant?.Relationship.World;
            // Historical diagnostics keep their existing eager acquisition. New
            // Chunk 7 owners retain the field before invoking explicit Install.
            if (!deferInstallation)
            {
                try { Install(); }
                catch { Dispose(); throw; }
            }
        }
        internal void Install()
        {
            if (disposed) throw new ObjectDisposedException(nameof(NativeActorAllocationTrace));
            if (installed) return;
            if (installationStarted) throw new InvalidOperationException("A failed observer installation retains cleanup custody and cannot be replayed.");
            if (active != null) throw new InvalidOperationException("Another observer already owns this native boundary.");
            installationStarted = true;
            // Native contract validation; no patch ownership exists before the Harmony boundary.
            if (typeof(UnitEntityData).Assembly.ManifestModule.ModuleVersionId != new Guid("07fa1e4d-8618-41b3-9b8d-faa17d3b26f7"))
                throw new InvalidOperationException("Allocation trace requires the exact installed Kingmaker MVID.");
            harmony = HarmonyInstance.Create(HarmonyId); active = this;
            Patch(typeof(TurnController), 0x06000C37, "ApproachMovementBefore", "ApproachMovementAfter");
            Patch(typeof(TurnController), 0x06000C3C, "PrepareBefore", "PrepareAfter");
            Patch(typeof(UnitCombatState.Cooldowns), 0x0600C3BE, "ClearBefore", "ClearAfter");
            Patch(typeof(UnitEntityData), 0x06008355, "DisableJoinBefore", "DisableJoinAfter");
            Patch(typeof(UnitCombatPrepareController), 0x0600936F, "DisablePrepareTickBefore", "DisablePrepareTickAfter");
            Patch(typeof(UnitCombatCooldownsController), 0x0600934A, "CooldownTickBefore", "CooldownTickAfter");
            Patch(typeof(BaseUnitCombatController), 0x06009343, "CooldownEligibilityBefore", "CooldownEligibilityAfter");
            Patch(typeof(UnitCombatState), 0x060093A1, "OpportunityBefore", "OpportunityAfter");
            Patch(typeof(UnitCombatState), 0x060093A4, "CombatClearBefore", "CombatClearAfter");
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
            Patch(typeof(TurnController), 0x06000C47, "ForceToEndBefore", "ForceToEndAfter");
            // UnitCommand.Interrupt(bool): the pinned token the production interrupt bridge also uses.
            // Recorded with the managed caller chain so an unexplained Interrupt names its source.
            Patch(typeof(UnitCommand), 0x060027AC, "InterruptBefore", "InterruptAfter");
            Patch(typeof(CombatController), 0x06000BE6, "RemoveUnitBefore", "RemoveUnitAfter");
            Patch(typeof(UnitConfusionController), 0x06009131, "ConfusionBefore", "ConfusionAfter");
            Patch(typeof(PairedConfusionPreparation), typeof(PairedConfusionPreparation).GetMethod("Prepare", Flags).MetadataToken,
                "PairedConfusionBefore", "PairedConfusionAfter");
            Patch(typeof(UnitMovementAgent), 0x060018A9, "MovementBefore", "MovementAfter");
            Patch(typeof(UnitMovementAgent), 0x060018AA, "PhysicalTickBefore", "PhysicalTickAfter");
            Patch(typeof(UnitMovementAgentBase), 0x060018DB, "PhysicalMoveBefore", "PhysicalMoveAfter");
            installed = true;
        }

        internal void BeginEncounter(string id) { encounter = id; Record("encounter-setup", rider); }
        internal JObject Capture() => new JObject { ["events"] = events.DeepClone(), ["forceToEnd"] = forceToEnd.DeepClone(), ["dropped"] = dropped, ["observationErrors"] = observationErrors + (installed ? 0 : 1), ["observerHooks"] = installedHooks.DeepClone(),
            ["identityRegistry"] = new JObject { ["contract"] = "retained-native-object-identity-v1", ["capacity"] = identities.Capacity,
                ["ids"] = new JArray(identities.Ids), ["retainedCount"] = identities.RetainedCount,
                ["released"] = identities.Released, ["faults"] = identities.FaultCount } };
        internal int GrantCount(UnitEntityData actor) => grants.ContainsKey(actor) ? grants[actor] : 0;
        internal JObject Snapshot(UnitEntityData actor)
        {
            var turn = Game.Instance?.TurnBasedCombatController?.CurrentTurn;
            var cooldown = actor.CombatState.Cooldown;
            var actorCombat = ActorCombat(actor);
            var confusion = actor.Get<UnitPartConfusion>();
            var nativeTurn = turn?.Unit == actor ? turn : actorCombat?.PairedPartnerContext?.Unit == actor ? actorCombat.PairedPartnerContext : null;
            return new JObject {
                ["actor"] = actor.UniqueId, ["actorObject"] = Id(actor), ["grantSequence"] = GrantCount(actor),
                ["inCombat"] = actor.IsInCombat,
                ["grantSequenceKind"] = "observed-native-Prepare-entries",
                ["pairedGrantIdentity"] = actorCombat?.PairedActivationIdentity,
                ["standard"] = cooldown.StandardAction, ["move"] = cooldown.MoveAction, ["swift"] = cooldown.SwiftAction,
                ["initiativeCooldown"] = cooldown.Initiative, ["initiative"] = actor.CombatState.Initiative,
                ["initiativeOrder"] = actor.CombatState.Initiative,
                ["reactionCooldown"] = cooldown.AttackOfOpportunity, ["reactions"] = actor.CombatState.AttackOfOpportunityCount,
                ["reactionsPerRound"] = actor.CombatState.AttackOfOpportunityPerRound,
                ["waitingInitiative"] = actor.CombatState.IsWaitingInitiative,
                ["disengageTargets"] = actor.CombatState.DisengageAttackTargets.Count,
                ["prepared"] = actor.CombatState.Prepared, ["canAct"] = actor.CombatState.CanActInCombat,
                ["confused"] = actor.Descriptor.State.HasCondition(UnitCondition.Confusion),
                ["confusionImmune"] = actor.Descriptor.State.HasConditionImmunity(UnitCondition.Confusion),
                ["confusionPart"] = confusion != null,
                ["confusionState"] = confusion?.State.ToString(),
                ["confusionRoundTicks"] = confusion?.RoundStartTime.Ticks,
                ["confusionCommand"] = Id(confusion?.Cmd),
                ["preparingPairedActor"] = actorCombat?.IsPreparingPairedActor(actor) == true,
                ["prone"] = actor.Descriptor.State.Prone.Active,
                ["proneRequested"] = actor.Descriptor.State.Prone.ShouldBeActive,
                ["maneuverImmune"] = actor.Descriptor.State.HasCondition(Kingmaker.UnitLogic.UnitCondition.ImmuneToCombatManeuvers),
                ["timeToNextNativeTurn"] = actor.GetTimeToNextTurn(), ["damage"] = actor.Damage,
                ["hasSwift"] = actor.HasSwiftAction(), ["hasStandard"] = actor.HasStandardAction(), ["usedStandard"] = actor.UsedStandardAction(),
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

        private void RecordForceToEnd(UnitEntityData actor)
        {
            if (actor == null) return;
            if (forceToEnd.Count >= 4096) { dropped++; return; }
            try
            {
                forceToEnd.Add(new JObject { ["eventSequence"] = events.Count, ["actor"] = actor.UniqueId, ["frame"] = Time.frameCount,
                    ["round"] = Game.Instance.TurnBasedCombatController.RoundNumber });
            }
            catch (Exception) { observationErrors++; }
        }
        internal void Record(string boundary, UnitEntityData actor, UnitCommand command = null, string detail = null, object callback = null,
            UnitEntityData reactionTarget = null)
        {
            if (actor == null || actor.Group != rider.Group && (!combat.PairedActivationEnabled || !actor.IsInCombat)) return;
            if (events.Count >= 16000) { dropped++; return; }
            try
            {
                var controller = Game.Instance.TurnBasedCombatController;
                var turn = controller.CurrentTurn;
                events.Add(new JObject {
                    ["sequence"] = events.Count + 1, ["encounter"] = encounter, ["session"] = Id(Game.Instance.Player),
                    ["activationIdentity"] = ActorCombat(actor)?.PairedActivationIdentity,
                    ["boundary"] = boundary, ["frame"] = Time.frameCount, ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks,
                    ["simulatingClick"] = Kingmaker.Controllers.Clicks.PointerController.SimulatingClick,
                    ["gameDeltaTime"] = Game.Instance.TimeController.GameDeltaTime,
                    ["nativeTurnBased"] = CombatController.IsInTurnBasedCombat(),
                    ["nativePassing"] = CombatController.IsPassing(),
                    ["nativeSurprised"] = controller.IsSurprised(actor),
                    ["controller"] = Id(controller), ["round"] = controller.RoundNumber, ["roundStartTicks"] = controller.RoundStartTime.Ticks,
                    ["turn"] = Id(turn), ["preparingTurn"] = Id(preparing), ["currentActor"] = turn?.Unit?.UniqueId,
                    ["turnStatus"] = turn?.Status.ToString(), ["state"] = Snapshot(actor), ["detail"] = detail,
                    ["callbackObject"] = Id(callback),
                    ["reactionObservation"] = reactionTarget == null ? null : new JObject {
                        ["targetId"] = reactionTarget.UniqueId, ["targetObject"] = Id(reactionTarget),
                        ["attackerId"] = actor.UniqueId, ["attackerObject"] = Id(actor),
                        ["attackerMotion"] = actor.HasMotionThisTick, ["targetMotion"] = reactionTarget.HasMotionThisTick,
                        ["disengageTargetIds"] = new JArray(actor.CombatState.DisengageAttackTargets.Select(u => u.UniqueId)) },
                    ["command"] = Id(command), ["commandType"] = command?.GetType().FullName,
                    ["actionType"] = command?.Type.ToString(), ["timeSinceStart"] = command?.TimeSinceStart,
                    ["commandActor"] = command?.Executor?.UniqueId, ["started"] = command?.IsStarted,
                    // An attack of opportunity's target is its provoker; its motion shows what provoked it.
                    ["commandTarget"] = command?.Target?.Unit?.UniqueId, ["commandTargetMotion"] = command?.Target?.Unit?.HasMotionThisTick,
                    ["acted"] = command?.IsActed, ["finished"] = command?.IsFinished, ["result"] = command?.Result.ToString(),
                    ["ignoreCooldown"] = command?.IsIgnoreCooldown,
                    ["commandInitialized"] = command?.Executor != null,
                    ["shouldApproach"] = command?.Executor?.View != null && command.Target != null
                        ? (bool?)command.ShouldUnitApproach : null,
                    ["feedback"] = ActorCombat(actor)?.LastFeedback
                });
                BoundaryObserved?.Invoke(boundary, actor, command);
            }
            catch (Exception exception) { observationErrors++; events.Add(new JObject { ["boundary"] = boundary, ["observationError"] = exception.ToString() }); }
        }

        public void Dispose()
        {
            if (disposed) return;
            harmony?.UnpatchAll(HarmonyId);
            identities.Dispose();
            if (ReferenceEquals(active, this)) active = null;
            disposed = true;
        }
        internal int ObjectIdentity(object value) => identities.Get(value);
        private int Id(object value) => ObjectIdentity(value);
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
            installedHooks.Add(new JObject { ["method"] = method.DeclaringType.FullName + "." + method.Name, ["token"] = token.ToString("X8"),
                ["moduleMvid"] = method.Module.ModuleVersionId.ToString(), ["prefix"] = prefix, ["postfix"] = postfix });
        }
        private MountedCombatController ActorCombat(UnitEntityData actor)
        {
            var participant = world?.UnifiedTurn.Find(actor);
            return participant != null ? world.Find(participant.Relationship)?.Combat :
                actor == rider || actor == mount ? combat : null;
        }
        private IEnumerable<UnitEntityData> ObservedPairActors => world == null ? new[] { rider, mount } :
            world.UnifiedTurn.SnapshotParticipants.SelectMany(p => p.ParticipationActors).Distinct();
        // One scenario-declared non-pair actor (the owner-seed RT companion) whose own command
        // admissions and interrupts are recorded too. Never the whole party: every other campaign's
        // event stream is unchanged.
        private UnitEntityData declaredCommandOwner;
        // The registered-disable case keeps this exact native actor observable
        // after its pair leaves the active participation inventory. Observation
        // never changes native JoinCombat, preparation or cooldown behavior.
        private UnitEntityData disableNativeActor;
        internal void ObserveDisableNativeActor(UnitEntityData actor)
        {
            if (actor == null || world?.Registry.FindOwner(actor) == null || disableNativeActor != null)
                throw new InvalidOperationException("Disable observation requires one current exact pair actor, once.");
            disableNativeActor = actor;
        }
        internal void EndDisableNativeObservation()
        {
            if (disablePrepareDepth != 0) throw new InvalidOperationException("Native disable preparation observer has an open callback.");
            disableNativeActor = null;
        }
        internal UnitEntityData DeclaredCommandOwner => declaredCommandOwner;
        internal void DeclareCommandOwner(UnitEntityData actor)
        {
            if (actor == null || ObservedPairActors.Contains(actor)) throw new InvalidOperationException("A declared command owner must be one exact non-pair actor.");
            if (declaredCommandOwner != null && !ReferenceEquals(declaredCommandOwner, actor)) throw new InvalidOperationException("Only one extra command owner may be declared.");
            declaredCommandOwner = actor;
        }
        private bool ObservesCommandActor(UnitEntityData actor) => actor != null && (ObservedPairActors.Contains(actor) || ReferenceEquals(actor, declaredCommandOwner));
        // One exact native command whose every UnitCommand.Interrupt(bool) entry is kept apart from
        // the event stream (so existing sequences are unchanged), with the command's terminal state
        // before the call and the managed caller chain. 7E C03 attack modes: an unpaired rider's
        // command is otherwise unobserved, and native Interrupt overwrites a Success that OnAction
        // already assigned while the last attack's animation runs. Observation only.
        private UnitCommand watchedCommand;
        private JArray watchedInterrupts;
        internal void WatchCommandInterrupts(UnitCommand command)
        {
            if (command == null || watchedCommand != null) throw new InvalidOperationException("Exactly one native command interrupt watch may be open.");
            watchedCommand = command; watchedInterrupts = new JArray();
        }
        internal JArray TakeWatchedInterrupts(UnitCommand command)
        {
            if (command == null || !ReferenceEquals(command, watchedCommand)) throw new InvalidOperationException("The native command interrupt watch names another command.");
            var rows = watchedInterrupts; watchedCommand = null; watchedInterrupts = null; return rows;
        }
        private void RecordWatchedInterrupt(UnitCommand command, string callers, bool callersTruncated)
        {
            try
            {
                var target = command.TargetUnit; var executor = command.Executor;
                watchedInterrupts.Add(new JObject { ["frame"] = Time.frameCount,
                    ["round"] = Game.Instance.TurnBasedCombatController?.RoundNumber, ["command"] = Id(command),
                    ["resultBefore"] = command.Result.ToString(), ["actedBefore"] = command.IsActed, ["finishedBefore"] = command.IsFinished,
                    ["interruptAsSoonAsPossible"] = command.InterruptAsSoonAsPossible, ["timeSinceStart"] = command.TimeSinceStart,
                    ["attackIndex"] = (command as UnitAttack)?.GetAttackIndex(), ["plannedAttacks"] = (command as UnitAttack)?.AllAttacks.Count,
                    ["executorCanAct"] = executor?.Descriptor.State.CanAct, ["targetInState"] = target?.IsInState,
                    ["targetDead"] = target?.Descriptor.State.IsDead, ["callers"] = callers, ["callersTruncated"] = callersTruncated });
            }
            catch (Exception) { observationErrors++; }
        }
        private UnitEntityData Owner(UnitCommands commands) =>
            ObservedPairActors.SingleOrDefault(actor => ReferenceEquals(actor.Commands, commands)) ??
            (declaredCommandOwner != null && ReferenceEquals(declaredCommandOwner.Commands, commands) ? declaredCommandOwner : null);
        private UnitEntityData CooldownOwner(UnitCombatState.Cooldowns value) =>
            ObservedPairActors.SingleOrDefault(actor => ReferenceEquals(actor.CombatState.Cooldown, value)) ??
            (disableNativeActor != null && ReferenceEquals(disableNativeActor.CombatState.Cooldown, value) ? disableNativeActor : null) ??
            (preparing?.Unit.CombatState.Cooldown == value ? preparing.Unit : null);
        private bool ObservesReactionActor(UnitEntityData actor) => ObserveReactionResources && ActorCombat(actor) != null;
        private static partial class Hooks
        {
            internal static void CooldownEligibilityBefore(BaseUnitCombatController __instance, UnitEntityData unit, out bool __state)
            {
                var p = active;
                __state = p != null && p.ObserveGroundCooldownEligibility && p.ObservesReactionActor(unit) &&
                    __instance != null && __instance.GetType() == typeof(UnitCombatCooldownsController);
                if (__state) p.Record("cooldown-eligibility-before", unit, detail: __instance.GetType().FullName, callback: __instance);
            }
            internal static void CooldownEligibilityAfter(BaseUnitCombatController __instance, UnitEntityData unit, bool __state, bool __result)
            {
                if (__state) active?.Record("cooldown-eligibility-after", unit, detail: "eligible=" + __result, callback: __instance);
            }
            internal static void CooldownTickBefore(UnitEntityData unit)
            { if (active?.ObservesReactionActor(unit) == true) active.Record("cooldown-tick-before", unit); }
            internal static void CooldownTickAfter(UnitEntityData unit)
            { if (active?.ObservesReactionActor(unit) == true) active.Record("cooldown-tick-after", unit); }
            internal static void OpportunityBefore(UnitCombatState __instance, UnitEntityData target, bool simulate)
            { if (active?.ObservesReactionActor(__instance.Unit) == true) active.Record("opportunity-before", __instance.Unit, detail: "simulate=" + simulate, reactionTarget: target); }
            internal static void OpportunityAfter(UnitCombatState __instance, UnitEntityData target, bool simulate, bool __result)
            { if (active?.ObservesReactionActor(__instance.Unit) == true) active.Record("opportunity-after", __instance.Unit, detail: "simulate=" + simulate + ";result=" + __result, reactionTarget: target); }

            internal static void PhysicalTickBefore(UnitMovementAgent __instance, out UnitMovementAgent __state)
            {
                __state = active?.physicalMovement;
                if (active == null) return;
                var actor = __instance.Unit?.EntityData;
                active.physicalMovement = CombatController.IsInTurnBasedCombat() &&
                    active.ObservedPairActors.Contains(actor) ? __instance : null;
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
            internal static void ClearBefore(UnitCombatState.Cooldowns __instance)
            { if (active != null) { var actor = active.CooldownOwner(__instance); active.Record("clear-before", actor,
                detail: ReferenceEquals(actor, active.disableNativeActor) ? "disablePrepareDepth=" + active.disablePrepareDepth : null); } }
            internal static void ClearAfter(UnitCombatState.Cooldowns __instance)
            { if (active != null) { var actor = active.CooldownOwner(__instance); active.Record("clear-after", actor,
                detail: ReferenceEquals(actor, active.disableNativeActor) ? "disablePrepareDepth=" + active.disablePrepareDepth : null); } }
            internal static void DisableJoinBefore(UnitEntityData __instance)
            { if (ReferenceEquals(__instance, active?.disableNativeActor)) active.Record("disable-join-before", __instance); }
            internal static void DisableJoinAfter(UnitEntityData __instance)
            { if (ReferenceEquals(__instance, active?.disableNativeActor)) active.Record("disable-join-after", __instance); }
            internal static void DisablePrepareTickBefore()
            { if (active?.disableNativeActor != null) { active.disablePrepareDepth++; active.Record("disable-prepare-tick-before", active.disableNativeActor); } }
            internal static void DisablePrepareTickAfter()
            { if (active?.disableNativeActor != null) { active.Record("disable-prepare-tick-after", active.disableNativeActor); active.disablePrepareDepth--; } }
            internal static void CombatClearBefore(UnitCombatState __instance) { active?.Record("combat-clear-before", __instance.Unit); }
            internal static void CombatClearAfter(UnitCombatState __instance) { active?.Record("combat-clear-after", __instance.Unit); }
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
            // An attack of opportunity's admission carries its caller chain, naming the provoking path
            // (disengage tick, provoke fan-out such as a native stand-up, or a forced feat).
            internal static void CommandBefore(UnitCommands __instance, UnitCommand cmd) { active?.Record("admission-before", active.Owner(__instance), cmd,
                detail: cmd is UnitAttackOfOpportunity ? InterruptCallers() : null); }
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
            internal static void InterruptBefore(UnitCommand __instance)
            {
                var p = active;
                if (p == null || __instance == null) return;
                if (ReferenceEquals(__instance, p.watchedCommand)) { bool truncated; var chain = InterruptCallers(256, out truncated); p.RecordWatchedInterrupt(__instance, chain, truncated); }
                var actor = __instance.Executor;
                if (!p.ObservesCommandActor(actor)) return;
                p.Record("command-interrupt-before", actor, __instance, detail: InterruptCallers());
            }
            internal static void InterruptAfter(UnitCommand __instance)
            {
                var p = active;
                if (p == null || __instance == null) return;
                var actor = __instance.Executor;
                if (!p.ObservesCommandActor(actor)) return;
                p.Record("command-interrupt-after", actor, __instance);
            }
            // The managed frames above the interrupt hook, outermost last. Harmony wrapper frames and
            // this trace are skipped; the summary is observation only and never changes the call.
            private static string InterruptCallers() { bool truncated; return InterruptCallers(14, out truncated); }
            // The watched command keeps its whole chain (up to 256 named frames) and says whether any was cut,
            // so a missing KingmakerMountedCombat frame is never inferred from a truncated chain.
            private static string InterruptCallers(int maximum, out bool truncated)
            {
                truncated = true;
                try
                {
                    var stack = new System.Diagnostics.StackTrace(2, false);
                    var names = new List<string>(); truncated = false;
                    for (var index = 0; index < stack.FrameCount; index++)
                    {
                        var method = stack.GetFrame(index)?.GetMethod();
                        if (method == null) continue;
                        var name = (method.DeclaringType == null ? "" : method.DeclaringType.FullName + ".") + method.Name;
                        if (name.StartsWith("Harmony", StringComparison.Ordinal) ||
                            name.StartsWith("KingmakerMountedCombat.Diagnostics.NativeActorAllocationTrace", StringComparison.Ordinal)) continue;
                        if (names.Count == maximum) { truncated = true; break; }
                        names.Add(name);
                    }
                    return "callers=" + string.Join(" < ", names.ToArray());
                }
                catch (Exception exception) { truncated = true; return "callers=unavailable:" + exception.GetType().Name; }
            }
            internal static void TurnEndBefore(TurnController __instance) { active?.Record("turn-end-before", __instance.Unit); }
            internal static void TurnEndAfter(TurnController __instance) { active?.Record("turn-end-after", __instance.Unit); }
            internal static void ForceToEndBefore(TurnController __instance) { active?.RecordForceToEnd(__instance.Unit); }
            internal static void ForceToEndAfter() { }
            internal static void RemoveUnitBefore(UnitEntityData unit) { active?.Record("remove-unit-before", unit); }
            internal static void ConfusionBefore(UnitEntityData unit)
            { if (active?.preparing != null) active.Record("native-confusion-before", unit); }
            internal static void ConfusionAfter(UnitEntityData unit)
            { if (active?.preparing != null) active.Record("native-confusion-after", unit); }
            internal static void PairedConfusionBefore(UnitEntityData actor)
            { active?.Record("paired-confusion-adapter-before", actor); }
            internal static void PairedConfusionAfter(UnitEntityData actor)
            { active?.Record("paired-confusion-adapter-after", actor); }
            internal static void RemoveUnitAfter(UnitEntityData unit) { active?.Record("remove-unit-after", unit); }
            internal static void MovementBefore(float deltaTime, out float __state) { __state = deltaTime; }
            internal static void MovementAfter(UnitMovementAgent __instance, float deltaTime, bool __result, float __state)
            {
                var actor = __instance.Unit?.EntityData;
                if (active == null || actor == null || !active.ObservedPairActors.Contains(actor) || !CombatController.IsInTurnBasedCombat()) return;
                active.requestedTime[actor] = (active.requestedTime.ContainsKey(actor) ? active.requestedTime[actor] : 0f) + __state;
                active.movedTime[actor] = (active.movedTime.ContainsKey(actor) ? active.movedTime[actor] : 0f) + (__result ? deltaTime : 0f);
                if (Time.frameCount % 15 == 0) active.Record("movement-tick", actor, detail: "requested=" + __state.ToString("R") + ";allowed=" + deltaTime.ToString("R") + ";result=" + __result);
            }
        }
    }
}
