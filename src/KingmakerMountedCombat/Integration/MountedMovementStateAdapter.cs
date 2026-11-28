using System;
using System.Collections.Generic;
using System.Reflection;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Controllers.Combat;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UI.SettingsUI;
using Kingmaker.View;
using KingmakerMountedCombat.Domain;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Integration
{
    // Pair-scoped actor records survive dismount/reselection. No live synthetic
    // TurnController, rider cooldown projection, selection mutation or turn reset.
    internal sealed partial class MountedMovementStateAdapter
    {
        private sealed class Allocation
        {
            internal object Controller;
            internal int Round;
            internal long RoundStart;
            internal string GrantIdentity;
            internal bool Prepared;
            internal float MoveUsed;
            internal bool StandardUsed;
            internal MountedMovementState Movement = new MountedMovementState();
            // PR #70 R9-F3 (round 11): the last step tick's start position and its reserved physical bound, replaced by the steered displacement
            // the installed mover actually executed once it is observed (the next tick or allowance read).
            internal bool StepTickPending;
            internal Vector3 StepTickStart;
            internal float StepTickReserved;
            internal void SettleStepTravel(Vector3 position)
            {
                if (!StepTickPending) return;
                StepTickPending = false;
                var dx = position.x - StepTickStart.x; var dz = position.z - StepTickStart.z;
                Movement.MetresStepTravelled = Math.Max(0f, Movement.MetresStepTravelled - StepTickReserved + (float)Math.Sqrt(dx * dx + dz * dz));
            }
        }
        // R11-F9 (round 12): the speed facts read once at the start of a movement tick (TickDelegated); every allowance read of that tick's
        // decision and debit uses them, never a later read that could succeed or fail differently.
        private UnitEntityData speedScopeActor;
        private float speedScopePhysical, speedScopeNominal;
        private void ReadSpeeds(UnitEntityData actor, out float physical, out float nominal)
        {
            if (actor != null && ReferenceEquals(actor, speedScopeActor)) { physical = speedScopePhysical; nominal = speedScopeNominal; return; }
            physical = PhysicalSpeedOf(actor);
            nominal = NominalSpeedOf(actor);
        }
        // The speeds a mode needs: the physical speed (MaxSpeed) always; the nominal speed (CurrentSpeedMps) only for a step's native counter.
        private static bool SpeedsReadable(float physical, float nominal, bool step) => physical > 0f && (!step || nominal > 0f);
        // The nominal speed handed to the step arithmetic: an ordinary Move never uses it, a step without it gets no allowance.
        private static float NominalForMode(float physical, float nominal, bool step) => nominal > 0f ? nominal : step ? 0f : physical;
        private readonly Dictionary<UnitEntityData, Allocation> allocations = new Dictionary<UnitEntityData, Allocation>();
        // PR #70 R1-F6 (round 6): the pair's pending provisional replacement request (owned by that request, never by an allocation). It
        // applies only to a tick of the exact carrier the request installed; the request binds and releases it.
        internal IMountedFirstTickGate FirstTickGate;
        private readonly ActorAllocationLifetime<UnitEntityData, Allocation> lifetime;
        private TurnController preparingTurn;

        internal MountedMovementStateAdapter()
        {
            lifetime = new ActorAllocationLifetime<UnitEntityData, Allocation>(allocations);
        }

        internal int TrackedActorCount => allocations.Count;
        internal bool HasUnsettledPreparation => preparingTurn != null;

        internal void MaintainLifetimes()
        {
            var player = Game.Instance?.Player;
            if (player == null) return;
            if (lifetime.SynchronizeSession(player, player.GameId) != 0) preparingTurn = null;
            if (player.IsInCombat || allocations.Count == 0) return;
            var settled = new List<UnitEntityData>();
            foreach (var entry in allocations)
            {
                var actor = entry.Key;
                var cooldown = actor.CombatState?.Cooldown;
                if (cooldown == null || actor.Commands == null) continue;
                // An explicit paired grant ends with the encounter, even if
                // actor removal already disposed its activation. Native debt
                // remains on the actor and continues its normal RT recovery.
                // Dismount, selection and mode conversion are not this boundary.
                if (!actor.IsInCombat && actor.Commands.Empty && (entry.Value.GrantIdentity != null || cooldown.StandardAction <= 0f &&
                    cooldown.MoveAction <= 0f && cooldown.SwiftAction <= 0f &&
                    (entry.Value.Prepared || entry.Value.MoveUsed <= 0f)))
                    settled.Add(actor);
            }
            lifetime.RetireSettledActors(settled);
        }

        internal void RetireDestroyedActor(UnitEntityData actor)
        {
            if (actor == null) return;
            lifetime.RetireDestroyedActor(actor);
            if (preparingTurn?.Unit == actor) preparingTurn = null;
        }

        internal void RetireCompletedEncounterActor(UnitEntityData actor)
        {
            if (actor != null && !(Game.Instance?.Player?.IsInCombat ?? true) &&
                !actor.IsInCombat && actor.Commands.Empty) lifetime.RetireDestroyedActor(actor);
        }

        internal void Clear()
        {
            preparingTurn = null;
            lifetime.Clear();
        }

        internal void BeginPreparation(TurnController turn, UnitEntityData activeMount)
        {
            MaintainLifetimes();
            preparingTurn = turn != null && CombatController.IsInTurnBasedCombat() &&
                (turn.Unit == activeMount || Owns(turn.Unit)) ? turn : null;
        }

        internal void BeginGrantedPreparation(TurnController turn, string identity)
        {
            if (turn == null) return;
            MaintainLifetimes();
            var controller = Game.Instance.TurnBasedCombatController;
            allocations[turn.Unit] = new Allocation { Controller = controller, Round = controller.RoundNumber,
                RoundStart = controller.RoundStartTime.Ticks, GrantIdentity = identity };
            preparingTurn = turn;
        }

        internal void BeforeNativeRoundState(UnitCombatState state)
        {
            var turn = preparingTurn;
            if (turn == null || state?.Unit != turn.Unit ||
                Game.Instance?.TurnBasedCombatController?.CurrentTurn != turn && Get(turn.Unit).GrantIdentity == null ||
                !CombatController.IsInTurnBasedCombat()) return;
            // Exact native order: Clear -> reapply acting-command costs ->
            // reaction fields -> OnNewRound -> round/AI/fact/readiness callbacks.
            // Reconcile here, before the callbacks, without replaying any of them.
            Prepared(turn, turn.Unit);
        }

        internal void EndPreparation(TurnController turn)
        {
            if (ReferenceEquals(preparingTurn, turn)) preparingTurn = null;
        }
        private static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly PropertyInfo[] TurnMovementProperties = {
            Property("TimeMoved"), Property("TimeMovedInForceMode"), Property("TimeMovedByFiveFootStep"),
            Property("MetersMovedByFiveFootStep"), Property("ImmuneAttackOfOpportunityOnDisengage") };
        private static readonly FieldInfo AiStepField = Field("m_AIUsedFiveFootStep");
        private static readonly FieldInfo AutoStopField = Field("m_AutoStopAfterFirstMoveAction");
        private static readonly FieldInfo ForceModeField = AgentField("m_IsInForceMode", 0x040011AE, typeof(bool));
        private static readonly FieldInfo SavedMinSpeedField = AgentField("m_SavedMinSpeed", 0x040011B6, typeof(float?));
        private static readonly FieldInfo MinSpeedField = AgentField("m_MinSpeed", 0x040011CB, typeof(float));
        private static readonly FieldInfo WarmupField = AgentField("m_WarmupTime", 0x04001191, typeof(float));
        private static readonly FieldInfo SlowdownField = AgentField("m_SlowDownTime", 0x040011AC, typeof(float));

        internal bool Owns(UnitEntityData actor) => actor != null && allocations.ContainsKey(actor);

        // Diagnostic read without lifetime maintenance: a copy, never the live record.
        internal MountedMovementState PeekMovement(UnitEntityData actor)
        {
            Allocation allocation;
            if (actor == null || !allocations.TryGetValue(actor, out allocation)) return null;
            var movement = allocation.Movement;
            return new MountedMovementState { TimeMoved = movement.TimeMoved, TimeForced = movement.TimeForced,
                TimeStepped = movement.TimeStepped, MetresStepped = movement.MetresStepped, StepImmune = movement.StepImmune,
                AiStep = movement.AiStep, AutoStopPending = movement.AutoStopPending };
        }

        internal void ResetTransientStepImmunity(TurnController context)
        {
            if (context == null) return;
            Allocation allocation;
            if (allocations.TryGetValue(context.Unit, out allocation)) allocation.Movement.StepImmune = false;
            TurnMovementProperties[4].SetValue(context, false, null);
        }

        private Allocation Get(UnitEntityData mount)
        {
            MaintainLifetimes();
            var controller = Game.Instance.TurnBasedCombatController;
            Allocation allocation;
            if (!allocations.TryGetValue(mount, out allocation) || allocation.Controller != controller ||
                allocation.GrantIdentity == null &&
                (allocation.Round != controller.RoundNumber || allocation.RoundStart != controller.RoundStartTime.Ticks))
            {
                allocation = new Allocation { Controller = controller, Round = controller.RoundNumber,
                    RoundStart = controller.RoundStartTime.Ticks };
                allocations[mount] = allocation;
            }
            return allocation;
        }

        internal void ObserveNativeMovement(TurnController turn, UnitEntityData mount)
        {
            if (turn == null || turn.Unit != mount || mount == null || !CombatController.IsInTurnBasedCombat()) return;
            var allocation = Get(mount);
            allocation.MoveUsed = Math.Max(allocation.MoveUsed, mount.CombatState.Cooldown.MoveAction);
            allocation.StandardUsed |= mount.CombatState.Cooldown.StandardAction > 0f;
            var state = allocation.Movement;
            state.TimeMoved = turn.TimeMoved;
            state.TimeForced = turn.TimeMovedInForceMode;
            state.TimeStepped = turn.TimeMovedByFiveFootStep;
            state.MetresStepped = turn.MetersMovedByFiveFootStep;
            state.StepImmune = turn.ImmuneAttackOfOpportunityOnDisengage;
            state.AiStep = (bool)AiStepField.GetValue(turn);
            state.AutoStopPending = (bool)AutoStopField.GetValue(turn);
        }

        internal void ObserveAction(UnitCommand command)
        {
            if (command?.Executor == null || !Owns(command.Executor) || !CombatController.IsInTurnBasedCombat()) return;
            var allocation = Get(command.Executor);
            allocation.StandardUsed |= command.Executor.CombatState.Cooldown.StandardAction > 0f;
            allocation.MoveUsed = Math.Max(allocation.MoveUsed, command.Executor.CombatState.Cooldown.MoveAction);
        }

        internal void Prepared(TurnController turn, UnitEntityData activeMount)
        {
            if (turn == null || !CombatController.IsInTurnBasedCombat() ||
                turn.Unit != activeMount && !Owns(turn.Unit)) return;
            var mount = turn.Unit;
            var allocation = Get(mount);
            if (allocation.GrantIdentity != null)
            {
                // Native Clear and acting-command reapplication have run once at
                // this explicit grant. Previous grant floors must not be restored.
                allocation.MoveUsed = mount.CombatState.Cooldown.MoveAction;
                allocation.StandardUsed = mount.CombatState.Cooldown.StandardAction > 0f;
            }
            // A native allocation remains authoritative. Only expenditure made
            // within this native epoch survives its later Prepare; no early refresh.
            mount.CombatState.Cooldown.MoveAction = Math.Max(mount.CombatState.Cooldown.MoveAction, allocation.MoveUsed);
            if (allocation.StandardUsed) mount.CombatState.Cooldown.StandardAction = Math.Max(6f, mount.CombatState.Cooldown.StandardAction);
            allocation.Prepared = true;
            allocation.MoveUsed = mount.CombatState.Cooldown.MoveAction;
            var state = allocation.Movement;
            TurnMovementProperties[0].SetValue(turn, state.TimeMoved, null);
            TurnMovementProperties[1].SetValue(turn, state.TimeForced, null);
            TurnMovementProperties[2].SetValue(turn, state.TimeStepped, null);
            TurnMovementProperties[3].SetValue(turn, state.MetresStepped, null);
            TurnMovementProperties[4].SetValue(turn, state.StepImmune, null);
            AiStepField.SetValue(turn, state.AiStep);
            AutoStopField.SetValue(turn, state.AutoStopPending);
        }

        internal string TickDelegated(TurnController inputTurn, UnitEntityData mount, ref float deltaTime)
        {
            var allocation = Get(mount);
            // PR #70 R1-F6 (round 6): the carrier of a provisional replacement request reaches its first tick here, with this frame's actual
            // delta (UnitMoveController.Tick passes TimeController.DeltaTime). Its allowance is what Advance would grant for it under the
            // selected limit (MountedMovementState.Remaining, clamped by the tick); the request decides BEFORE any debit. A refusal (or a
            // commit in place) zeroes the tick, so nothing moves and nothing is debited.
            // Round 10 (R9-F3/R9-F4/R9-F5): the agent's own speed (MaxSpeed) converts time and distance for the allowance, the step ledger and the
            // debit, as native's shift does; a speed that cannot be read (a throwing getter included) is contained here: a gated carrier is then
            // refused by its decision and any other tick gets no movement (never an assumed speed).
            var physicalSpeed = PhysicalSpeedOf(mount);
            // R9-F3 (round 11): the installed step counter's unit (CurrentSpeedMps); the previous step tick's steered displacement settles first.
            var nominalSpeed = NominalSpeedOf(mount);
            // R11-F9 (round 12): the speeds this tick's mode requires are validated here, before any decision, and the same facts price the
            // decision's allowance reads and the debit (an ordinary Move does not need CurrentSpeedMps).
            var stepMode = inputTurn.EnabledFiveFootStep;
            var speedsReadable = SpeedsReadable(physicalSpeed, nominalSpeed, stepMode);
            var nominalUsed = NominalForMode(physicalSpeed, nominalSpeed, stepMode);
            allocation.SettleStepTravel(mount.Position);
            speedScopeActor = mount; speedScopePhysical = physicalSpeed; speedScopeNominal = nominalSpeed;
            try
            {
            var gatedTick = false;
            var gate = FirstTickGate;
            if (gate != null && gate.Carrier != null && ReferenceEquals(mount.Commands?.GetCommand(UnitCommand.CommandType.Move), gate.Carrier))
            {
                var gateRemaining = allocation.Movement.Remaining(physicalSpeed > 0f ? physicalSpeed : 0f, nominalUsed, TurnController.MetersOfFiveFootStep,
                    Math.Max(mount.CombatState.Cooldown.MoveAction, allocation.MoveUsed), allocation.StandardUsed || mount.UsedStandardAction(),
                    mount.IsMoveActionRestricted(), inputTurn.EnabledFiveFootStep, inputTurn.EnabledSingleActionMove);
                // Round 9 (R8-F6): the installed shift is MaxSpeed x the delta this hook returns (MountedAgentSteering), so distance is measured
                // with the agent's own speed; unreadable speed gives NaN and the decision refuses any moving commit.
                var allowedMetres = speedsReadable ? Math.Max(0f, Math.Min(deltaTime, gateRemaining)) * physicalSpeed : float.NaN;
                string gateDetail;
                float grantedMetres;
                var verdict = gate.Decide(inputTurn, allowedMetres, deltaTime, out grantedMetres, out gateDetail);
                // PR #70 R6-F2 (round 8): a commit on the restricted tick grants only the travel to its predicted stop; the tick is shortened to
                // it (the agent moves and is debited exactly that), so the stop never depends on how the agent would round a waypoint.
                // R11-F5 (round 12): the duration the decision approved is the duration returned and debited, also inside native's NearTheEnd
                // endpoint correction (which may not lengthen this approved tick; later ticks keep the native tolerance).
                if (verdict == MountedFirstTickVerdict.Commit && physicalSpeed > 0f)
                {
                    deltaTime = Math.Min(deltaTime, Math.Max(0f, grantedMetres) / physicalSpeed);
                    gatedTick = true;
                }
                if (verdict != MountedFirstTickVerdict.Commit)
                {
                    var tick = deltaTime;
                    deltaTime = 0f;
                    return "mountMove=first-tick-" + (verdict == MountedFirstTickVerdict.Refuse ? "refused" : "committed-in-place") + ";tick=" + tick.ToString("R") +
                        ";allowedMetres=" + allowedMetres.ToString("R") + ";" + gateDetail;
                }
            }
            if (!speedsReadable)
            {
                var tick = deltaTime;
                deltaTime = 0f;
                return "mountMove=speed-unreadable;tick=" + tick.ToString("R");
            }
            var cooldown = mount.CombatState.Cooldown;
            var before = cooldown.MoveAction;
            // Within a prepared allocation native time passing must not turn an
            // already-spent Standard into a second mounted movement conversion.
            var standardUsed = allocation.StandardUsed || mount.UsedStandardAction();
            var used = Math.Max(before, allocation.MoveUsed);
            float debit;
            var requested = deltaTime;
            var agent = mount.View.AgentASP;
            var forced = (bool)ForceModeField.GetValue(agent);
            var steppedBefore = allocation.Movement.MetresStepTravelled;
            // PR #70 R13-F5 (round 15): installed TickMovement completes the movement after its shift on the last segment (a passed endpoint
            // plane or within 0.05 m of the path's end moves the agent ONTO the end). A step tick is therefore bounded by that endpoint move as
            // well: when the endpoint lies farther than the physical step remainder, the tick is shortened so the shift can neither come within
            // the snap distance nor cross the endpoint plane (a zero tick when no such shift remains: the step ends there, native's denial
            // branch then completes the movement). An endpoint within the remainder keeps the lawful completion. Ordinary movement (and its
            // NearTheEnd borrowed time) is unchanged.
            var endpointDetail = "";
            if (inputTurn.EnabledFiveFootStep && !forced && deltaTime > 0f && physicalSpeed > 0f)
            {
                float endpointCap;
                if (StepEndpointCap(agent, mount.Position, Math.Max(0f, TurnController.MetersOfFiveFootStep - allocation.Movement.MetresStepTravelled), physicalSpeed,
                    out endpointCap, out endpointDetail) && endpointCap < deltaTime)
                    deltaTime = endpointCap;
            }
            deltaTime = allocation.Movement.Advance(deltaTime, physicalSpeed, nominalUsed,
                TurnController.MetersOfFiveFootStep, used, standardUsed, mount.IsMoveActionRestricted(),
                inputTurn.EnabledFiveFootStep, inputTurn.EnabledSingleActionMove,
                forced, agent.NearTheEnd && !gatedTick,
                SettingsRoot.Instance.AutoStopAfterFirstMoveAction.CurrentValue, out debit);
            // A forced line reaches here only as a charge, whose native shell paid for its caster and not for this mover.
            if (forced) debit = deltaTime;
            if (allocation.Movement.MetresStepTravelled > steppedBefore)
            {
                allocation.StepTickPending = true;
                allocation.StepTickStart = mount.Position;
                allocation.StepTickReserved = allocation.Movement.MetresStepTravelled - steppedBefore;
            }
            cooldown.MoveAction = used + debit;
            allocation.MoveUsed = allocation.Prepared ? cooldown.MoveAction : allocation.MoveUsed + debit;
            // Before Prepare only the new debit belongs to this allocation; an
            // old native Standard is not converted into a guessed fresh action.
            if (allocation.Prepared) allocation.StandardUsed |= mount.CombatState.Cooldown.StandardAction > 0f;
            if (deltaTime > 0f && !forced)
            {
                // Match the native allowed-movement pacing boundary. The next
                // native TickMovement restores the saved minimum speed. Skipping
                // this adds warm-up/slowdown action time to delegated short paths.
                SavedMinSpeedField.SetValue(agent, MinSpeedField.GetValue(agent));
                MinSpeedField.SetValue(agent, 1f);
                WarmupField.SetValue(agent, 0f);
                SlowdownField.SetValue(agent, 0f);
            }
            return "mountMove=" + before.ToString("R") + "->" + cooldown.MoveAction.ToString("R") +
                ";mountTime=" + allocation.Movement.TimeMoved.ToString("R") +
                ";mountStepMetres=" + allocation.Movement.MetresStepped.ToString("R") +
                ";mountStepTravelled=" + allocation.Movement.MetresStepTravelled.ToString("R") +
                ";nativePrepared=" + allocation.Prepared + ";round=" + allocation.Round +
                ";physicalDelta=" + requested.ToString("R") + "->" + deltaTime.ToString("R") + (gatedTick ? ";approvedTick=True" : "") + endpointDetail;
            }
            finally { speedScopeActor = null; }
        }

        // R13-F5 (round 15): the longest step tick whose shift (speed x tick) cannot trigger installed TickMovement's endpoint completion beyond
        // the physical step remainder. The shift's length bounds the stop's distance change, so a shift no longer than (distance to the end -
        // snap distance - margin) cannot end within the snap distance, and one shorter than the distance to the last segment's endpoint plane
        // cannot pass it. False (no bound) when the agent has no readable path or its end lies within the remainder (a lawful completion).
        internal const float StepEndpointMarginMetres = 0.0001f;
        internal static bool StepEndpointCap(Kingmaker.View.UnitMovementAgent agent, Vector3 position, float physicalLeft, float speed, out float cap, out string detail)
        {
            cap = float.MaxValue; detail = "";
            List<Vector3> path; int cursor; Vector2 waypoint;
            if (agent == null || !(speed > 0f) || !MountedAgentSteering.TryRead(agent, out path, out cursor, out waypoint) || path == null || path.Count == 0) return false;
            var end = path[path.Count - 1];
            var toEnd = Kingmaker.Utility.GeometryUtils.MechanicsDistance(position, end);
            if (toEnd <= physicalLeft) return false;
            var room = toEnd - PairedAttackClickProbe.NativeLastSegmentArrivalMetres - StepEndpointMarginMetres;
            var plane = float.MaxValue;
            if (cursor >= path.Count - 2 && path.Count >= 2)
            {
                var previous = path[path.Count - 2];
                double sx = end.x - previous.x, sz = end.z - previous.z, length = Math.Sqrt(sx * sx + sz * sz);
                if (length > 1e-6d)
                {
                    plane = (float)(((end.x - position.x) * sx + (end.z - position.z) * sz) / length);
                    room = Math.Min(room, plane - StepEndpointMarginMetres);
                }
            }
            cap = Math.Max(0f, room) / speed;
            detail = ";stepEndpoint(toEnd=" + toEnd.ToString("R") + ";physicalLeft=" + physicalLeft.ToString("R") + ";plane=" + plane.ToString("R") + ";cap=" + cap.ToString("R") + ")";
            return true;
        }

        // The agent's own speed (MaxSpeed), contained; NaN when unreadable.
        internal static float PhysicalSpeedOf(UnitEntityData actor)
        {
            try { return MountedAgentSteering.PhysicalSpeed(actor?.View?.AgentASP); }
            catch (Exception) { return float.NaN; }
        }

        // R9-F3 (round 11): the unit's CurrentSpeedMps, the installed step counter's unit, contained; NaN when unreadable.
        internal static float NominalSpeedOf(UnitEntityData actor)
        {
            try { return actor == null ? float.NaN : actor.CurrentSpeedMps; }
            catch (Exception) { return float.NaN; }
        }

        internal void CopyGrantedMovementToContext(TurnController turn)
        {
            if (turn == null || !allocations.ContainsKey(turn.Unit)) return;
            var state = allocations[turn.Unit].Movement;
            TurnMovementProperties[0].SetValue(turn, state.TimeMoved, null);
            TurnMovementProperties[1].SetValue(turn, state.TimeForced, null);
            TurnMovementProperties[2].SetValue(turn, state.TimeStepped, null);
            TurnMovementProperties[3].SetValue(turn, state.MetresStepped, null);
            TurnMovementProperties[4].SetValue(turn, state.StepImmune, null);
            AiStepField.SetValue(turn, state.AiStep);
            AutoStopField.SetValue(turn, state.AutoStopPending);
        }

        // The same allocation's remaining granted movement as distance (Remaining is time; native distance = time x speed).
        internal float RemainingGrantedMetres(UnitEntityData actor, bool step, bool singleMove)
        {
            Allocation allocation;
            if (actor == null || !allocations.TryGetValue(actor, out allocation) || !allocation.Prepared) return 0f;
            float speed, nominal;
            ReadSpeeds(actor, out speed, out nominal);
            if (!SpeedsReadable(speed, nominal, step)) return float.NaN;
            allocation.SettleStepTravel(actor.Position);
            return allocation.Movement.Remaining(speed, NominalForMode(speed, nominal, step), TurnController.MetersOfFiveFootStep,
                Math.Max(allocation.MoveUsed, actor.CombatState.Cooldown.MoveAction),
                allocation.StandardUsed || actor.UsedStandardAction(), actor.IsMoveActionRestricted(), step, singleMove) * speed;
        }

        // Review R6-F3: the distance the same allocation grants across one order (into the Standard conversion when the
        // native limits allow it), with the native auto-stop setting Advance itself honors.
        // PR #70 R14-F2 (round 15): stepCounterBinds is the step-counter classification computed from the SAME speed observations that priced
        // this grant (MountedMovementState.StepCounterFinal with an unbounded tick: the native counter binds the step, so a debit of it can be
        // final), carried with the priced grant so the click never rereads the speeds to classify it. Unreadable speeds price NaN (no grant)
        // and classify a step conservatively as binding (the provisional request, which changes nothing before its debit).
        internal float RemainingOrderMetres(UnitEntityData actor, bool step, bool singleMove, out bool stepCounterBinds)
        {
            stepCounterBinds = false;
            Allocation allocation;
            if (actor == null || !allocations.TryGetValue(actor, out allocation) || !allocation.Prepared) return 0f;
            // Round 10 (R9-F3/R9-F4): physical metres for an ordinary Move (time x the agent's own speed). Round 11 (R9-F3): a step's remaining
            // time is bounded by the installed counter (nominal metres / CurrentSpeedMps) and by the physical travel left (metres / MaxSpeed), so
            // the product is the physical distance the mover may still travel within both limits.
            float speed, nominal;
            ReadSpeeds(actor, out speed, out nominal);
            if (!SpeedsReadable(speed, nominal, step)) { stepCounterBinds = step; return float.NaN; }
            allocation.SettleStepTravel(actor.Position);
            stepCounterBinds = step && allocation.Movement.StepCounterFinal(speed, nominal, TurnController.MetersOfFiveFootStep, float.MaxValue);
            return allocation.Movement.RemainingAcrossOrder(speed, NominalForMode(speed, nominal, step), TurnController.MetersOfFiveFootStep,
                Math.Max(allocation.MoveUsed, actor.CombatState.Cooldown.MoveAction),
                allocation.StandardUsed || actor.UsedStandardAction(), actor.IsMoveActionRestricted(), step, singleMove,
                SettingsRoot.Instance.AutoStopAfterFirstMoveAction.CurrentValue) * speed;
        }

        // PR #70 R1-F6: the allocation is in the state where native grants exactly one frame's tick (MountedMovementState.FirstTickOnly).
        internal bool FirstTickOnly(UnitEntityData actor, bool step, bool singleMove)
        {
            Allocation allocation;
            if (actor == null || !allocations.TryGetValue(actor, out allocation) || !allocation.Prepared) return false;
            return allocation.Movement.FirstTickOnly(Math.Max(allocation.MoveUsed, actor.CombatState.Cooldown.MoveAction),
                allocation.StandardUsed || actor.UsedStandardAction(), actor.IsMoveActionRestricted(), step, singleMove);
        }

        // PR #70 R12-F1 (round 13): whether a step tick of this length makes the native step counter final (MountedMovementState.StepCounterFinal),
        // with this tick's speed facts and the completed step travel settled first.
        internal bool StepCounterFinal(UnitEntityData actor, float tick)
        {
            Allocation allocation;
            if (actor == null || !allocations.TryGetValue(actor, out allocation) || !allocation.Prepared) return false;
            float speed, nominal;
            ReadSpeeds(actor, out speed, out nominal);
            if (!SpeedsReadable(speed, nominal, true)) return false;
            allocation.SettleStepTravel(actor.Position);
            return allocation.Movement.StepCounterFinal(speed, nominal, TurnController.MetersOfFiveFootStep, tick);
        }

        internal bool HasGrantedMovement(UnitEntityData actor, bool step, bool singleMove)
        {
            Allocation allocation;
            if (actor == null || !allocations.TryGetValue(actor, out allocation) || !allocation.Prepared) return false;
            // R11-F2 / R11-F9 (round 12): completed step travel is settled to its steered displacement before this availability check, and the
            // speeds are the contained reads the mode needs (a step without a readable CurrentSpeedMps has no allowance; an ordinary Move is time).
            float speed, nominal;
            ReadSpeeds(actor, out speed, out nominal);
            if (step && !SpeedsReadable(speed, nominal, true)) return false;
            allocation.SettleStepTravel(actor.Position);
            return allocation.Movement.Remaining(speed > 0f ? speed : 0f, NominalForMode(speed, nominal, step), TurnController.MetersOfFiveFootStep,
                Math.Max(allocation.MoveUsed, actor.CombatState.Cooldown.MoveAction),
                allocation.StandardUsed || actor.UsedStandardAction(), actor.IsMoveActionRestricted(), step, singleMove) > 0f;
        }

        private static PropertyInfo Property(string name) => typeof(TurnController).GetProperty(name, Flags) ??
            throw new MissingMemberException(typeof(TurnController).FullName, name);
        private static FieldInfo Field(string name) => typeof(TurnController).GetField(name, Flags) ??
            throw new MissingFieldException(typeof(TurnController).FullName, name);
        private static FieldInfo AgentField(string name, int token, Type fieldType)
        {
            var field = typeof(UnitMovementAgent).GetField(name, Flags);
            if (field == null || field.MetadataToken != token || field.FieldType != fieldType)
                throw new MissingFieldException(typeof(UnitMovementAgent).FullName, name);
            return field;
        }
    }
}
