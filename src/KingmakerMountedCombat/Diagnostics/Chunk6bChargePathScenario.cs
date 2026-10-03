using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.Utility;
using Kingmaker.View;
using KingmakerMountedCombat.Domain;
using Newtonsoft.Json.Linq;
using Pathfinding;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // Chunk 6B increment 6B.1: a diagnostics-only measurement of the pair forced path under the existing
    // diagnostic leases. No ability, no product change and no cost: the mount agent receives exactly the
    // calls the stock charge makes on its caster's agent (the charging flag, a doubled speed override and a
    // forced straight path to the target) while the rider agent stays under the mount's movement authority,
    // and every leased value is restored exactly. The compiled side records facts and checks its own
    // structure; scripts/runtime/Chunk6bChargePathEvidence.ps1 is the acceptance authority for the rows.
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        internal const string Chunk6bChargePathRtScenario = "chunk6b-charge-path-rt";
        internal const string Chunk6bChargePathTbScenario = "chunk6b-charge-path-tb";
        internal static bool IsChunk6bChargePathScenario(string scenario) =>
            string.Equals(scenario, Chunk6bChargePathRtScenario, StringComparison.Ordinal) ||
            string.Equals(scenario, Chunk6bChargePathTbScenario, StringComparison.Ordinal);
        private bool IsChunk6bChargePath => IsChunk6bChargePathScenario(request.Scenario);
        private bool Chunk6bChargePathTb => string.Equals(request.Scenario, Chunk6bChargePathTbScenario, StringComparison.Ordinal);
        private static readonly string[] Chunk6bChargePathCases = { "C6B-PATH-straight-arrival", "C6B-PATH-interrupt-stop" };
        private static readonly FieldInfo Chunk6bForceModeField = ResolveChunk6bAgentField("m_IsInForceMode", 0x040011AE, typeof(bool));
        private const float Chunk6bInterruptAfterMetres = 1.5f;
        private const double Chunk6bInterruptAfterSeconds = 0.4;

        private int chunk6bCase, chunk6bStage;
        private bool chunk6bControlSent;
        private JObject chunk6bGeometry, chunk6bBefore, chunk6bLease, chunk6bEntry;
        private readonly JArray chunk6bSamples = new JArray();
        private bool chunk6bChargingBefore, chunk6bChargingApplied;
        private float? chunk6bSpeedBefore;
        private Vector3 chunk6bOrigin, chunk6bLineDirection, chunk6bLastPosition;
        private double chunk6bStarted, chunk6bLastSample, chunk6bLastTime, chunk6bStoppedAt;
        private float chunk6bMoved, chunk6bLateral, chunk6bPeakSpeed;
        private string chunk6bStopReason;
        private bool chunk6bCommandsEmpty = true, chunk6bChargingThroughout = true;
        private int chunk6bAttackRulesBefore;
        private UnitMoveTo chunk6bEntryMove;
        private TurnController chunk6bEntryTurn;

        private static FieldInfo ResolveChunk6bAgentField(string name, int token, Type fieldType)
        {
            var field = typeof(UnitMovementAgent).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null || field.MetadataToken != token || field.FieldType != fieldType)
                throw new MissingFieldException(typeof(UnitMovementAgent).FullName, name);
            return field;
        }

        private string Chunk6bCaseId => Chunk6bChargePathCases[chunk6bCase];
        private JObject Chunk6bMeasurement => (JObject)observations["chunk6bChargePath"];
        private bool Chunk6bForceMode => (bool)Chunk6bForceModeField.GetValue(horse.View.AgentASP);
        private float Chunk6bReach => horse.View.Corpulence + target.View.Corpulence + (rider.GetFirstWeapon()?.AttackRange.Meters ?? 0f);
        private double Chunk6bNow => Game.Instance.TimeController.GameTime.TotalSeconds;

        private float Chunk6bMinimumRange()
        {
            var targetCorpulence = target?.View.Corpulence ?? 0.5f;
            return Chunk6bChargePathTb
                ? TurnController.MetersOfFiveFootStep + GameConsts.MinWeaponRange.Meters + horse.View.Corpulence + targetCorpulence
                : 10.Feet().Meters + horse.View.Corpulence + targetCorpulence;
        }

        // The stock clearance check read from the mount (the mover): every other awake unit with avoidance
        // must stay farther than 0.8 of the summed corpulences from the landing point a weapon reach short
        // of the target. The carried rider is not an obstacle to its own mount.
        private bool Chunk6bLandingBlocked(Vector3 point, float targetCorpulence)
        {
            var separation = rider.GetFirstWeapon() == null ? 0f : horse.View.Corpulence + targetCorpulence + rider.GetFirstWeapon().AttackRange.Meters;
            var landing = point.To2D() - (point - horse.Position).To2D().normalized * separation;
            return Game.Instance.State.AwakeUnits.Any(actor => actor != horse && actor != rider && actor != target &&
                actor.View && !actor.View.MovementAgent.AvoidanceDisabled &&
                (landing - actor.Position.To2D()).magnitude < (horse.View.Corpulence + actor.View.Corpulence) * 0.8f);
        }

        private JObject CaptureChunk6bGeometry()
        {
            var endpoint = ObstacleAnalyzer.TraceAlongNavmesh(horse.Position, target.Position);
            var distance = (target.Position - horse.Position).magnitude;
            var minimum = Chunk6bMinimumRange();
            var maximum = horse.CombatSpeedMps * 6f;
            var straight = endpoint == target.Position;
            var blocked = Chunk6bLandingBlocked(target.Position, target.View.Corpulence);
            var turn = Game.Instance.TurnBasedCombatController.CurrentTurn;
            return new JObject
            {
                ["originMount"] = CapturePosition(horse.Position), ["originRider"] = CapturePosition(rider.Position),
                ["targetPosition"] = CapturePosition(target.Position), ["traceEndpoint"] = CapturePosition(endpoint),
                ["distance3D"] = distance, ["minimumRange"] = minimum, ["maximumRange"] = maximum,
                ["mountCombatSpeedMps"] = horse.CombatSpeedMps, ["mountCorpulence"] = horse.View.Corpulence,
                ["targetCorpulence"] = target.View.Corpulence, ["straightRoute"] = straight, ["landingBlocked"] = blocked,
                ["mountAvoidanceDisabled"] = horse.View.MovementAgent.AvoidanceDisabled,
                ["riderAvoidanceDisabled"] = rider.View.MovementAgent.AvoidanceDisabled,
                ["nativeTimeMoved"] = turn?.TimeMoved,
                ["customCanTarget"] = distance <= maximum && distance >= minimum && straight &&
                    (horse.View.MovementAgent.AvoidanceDisabled || !blocked)
            };
        }

        private JObject CaptureChunk6bTurn()
        {
            var turn = Game.Instance.TurnBasedCombatController?.CurrentTurn;
            if (turn == null) return null;
            return new JObject
            {
                ["unit"] = turn.Unit?.UniqueId, ["isRider"] = turn.Unit == rider, ["status"] = turn.Status.ToString(),
                ["acting"] = turn.IsActing, ["timeMoved"] = turn.TimeMoved, ["timeMovedInForceMode"] = turn.TimeMovedInForceMode,
                ["timeMovedByFiveFootStep"] = turn.TimeMovedByFiveFootStep, ["metersMovedByFiveFootStep"] = turn.MetersMovedByFiveFootStep,
                ["enabledFiveFootStep"] = turn.EnabledFiveFootStep, ["enabledSingleActionMove"] = turn.EnabledSingleActionMove
            };
        }

        private JObject CaptureChunk6bAgents()
        {
            var agent = horse.View.AgentASP;
            return new JObject
            {
                ["charging"] = agent.IsCharging, ["speedOverride"] = agent.MaxSpeedOverride, ["forceMode"] = Chunk6bForceMode,
                ["mountMoving"] = agent.IsReallyMoving, ["descriptorCharging"] = horse.Descriptor.State.IsCharging,
                ["riderDescriptorCharging"] = rider.Descriptor.State.IsCharging, ["riderAgentCharging"] = rider.View.AgentASP.IsCharging,
                ["riderAgentEnabled"] = rider.View.AgentASP.enabled, ["riderAgentMoving"] = rider.View.AgentASP.IsReallyMoving,
                ["riderAvoidanceDisabled"] = rider.View.MovementAgent.AvoidanceDisabled
            };
        }

        private JObject CaptureChunk6bState(string kind)
        {
            var state = new JObject
            {
                ["kind"] = kind, ["frame"] = Time.frameCount, ["nativeSeconds"] = Chunk6bNow,
                ["rider"] = CaptureOrdinaryActor(rider), ["mount"] = CaptureOrdinaryActor(horse),
                ["agents"] = CaptureChunk6bAgents(), ["turn"] = CaptureChunk6bTurn(),
                ["pairMovement"] = combat.LastPairedMovementObservation, ["relationship"] = relationship.State.ToString(),
                ["distanceToTarget"] = target == null ? (float?)null : horse.DistanceTo(target)
            };
            return state;
        }

        private Vector3 FindChunk6bTargetPoint()
        {
            var attempts = new JArray();
            Chunk6bMeasurement["placement-" + Chunk6bCaseId] = new JObject { ["origin"] = CapturePosition(horse.Position), ["attempts"] = attempts };
            return FindWalkablePoint(horse.Position, 9f, 0.5f, point =>
            {
                var endpoint = ObstacleAnalyzer.TraceAlongNavmesh(horse.Position, point);
                var blocked = Chunk6bLandingBlocked(point, 0.5f);
                var riderDistance = HorizontalDistance(rider.Position, point);
                var within = MountedCombatSpatialPolicy.IsWithinDiagnosticSpawnBounds(riderDistance);
                attempts.Add(new JObject { ["point"] = CapturePosition(point), ["nativeTrace"] = CapturePosition(endpoint),
                    ["landingBlockedEstimate"] = blocked, ["riderDistance"] = riderDistance, ["withinFixtureBounds"] = within });
                return within && endpoint == point && !blocked;
            });
        }

        // A short native five-foot step along the charge line enters the rider's Acting turn without
        // spending a Move action (preview.155 CM07-mount-save-tb); the forced path is measured on top of it.
        private Vector3 FindChunk6bEntryDestination()
        {
            var direction = target.Position - horse.Position; direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f) throw new InvalidOperationException("The charge line is degenerate.");
            direction.Normalize();
            for (var i = 0; i < 16; i++)
            {
                var wanted = horse.Position + Quaternion.Euler(0f, i * 22.5f, 0f) * direction * 0.75f;
                var actual = ObstacleAnalyzer.TraceAlongNavmesh(horse.Position, wanted);
                if (GeometryUtils.MechanicsDistance(actual, wanted) <= 0.25f && GeometryUtils.MechanicsDistance(actual, horse.Position) > 0.25f) return actual;
            }
            throw new InvalidOperationException("No native walkable five-foot-step destination exists for the charge-path entry.");
        }

        private void BeginChunk6bChargePath()
        {
            if (!settings.EnablePairedActivation || settings.EnableUnifiedMountedTurn ||
                settings.EnablePairedCommandScheduler || settings.EnableDiagnosticOverlay || playerAction.OverlayPresent)
                throw new InvalidOperationException("Chunk 6B requires the accepted paired configuration.");
            CaptureIdleFixturePartyForCleanup();
            if (Chunk6bChargePathTb) pairedAutomaticEndProbe = new NativeAutomaticEndProbe(false);
            observations["chunk6bChargePath"] = new JObject
            {
                ["contract"] = "chunk6b-pair-forced-path-measurement", ["mode"] = Chunk6bChargePathTb ? "TB" : "RT",
                ["cases"] = new JArray(Chunk6bChargePathCases), ["forceModeField"] = Chunk6bForceModeField.MetadataToken.ToString("X8")
            };
            step = Phase3dHorseStep.Phase3gControls;
            ResetLeafClock();
        }

        private void ResetChunk6bCase()
        {
            chunk6bGeometry = null; chunk6bBefore = null; chunk6bLease = null; chunk6bEntry = null;
            chunk6bSamples.Clear(); chunk6bChargingApplied = false; chunk6bSpeedBefore = null; chunk6bChargingBefore = false;
            chunk6bMoved = chunk6bLateral = chunk6bPeakSpeed = 0f; chunk6bStopReason = null;
            chunk6bCommandsEmpty = true; chunk6bChargingThroughout = true; chunk6bEntryMove = null; chunk6bEntryTurn = null;
            chunk6bControlSent = false;
        }

        private void TickChunk6bChargePath()
        {
            var game = Game.Instance;
            var controller = game.TurnBasedCombatController;
            var turn = controller.CurrentTurn;
            if (game.IsPaused) { game.IsPaused = false; return; }
            Chunk6bMeasurement["progress"] = new JObject
            {
                ["case"] = Chunk6bCaseId, ["stage"] = chunk6bStage, ["frame"] = Time.frameCount,
                ["turn"] = turn?.Unit?.UniqueId, ["status"] = turn?.Status.ToString(), ["relationship"] = relationship.State.ToString(),
                ["samples"] = chunk6bSamples.Count
            };
            if (chunk6bStage == 0)
            {
                if (!rider.Commands.Empty || !horse.Commands.Empty || rider.AreHandsBusyWithAnimation) return;
                SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                if (relationship.State != RelationshipState.Mounted)
                {
                    if (!chunk6bControlSent)
                        chunk6bControlSent = TryNativeAbilityTargetClick(nativeControls.MountAbility, horse, "chunk6b-charge-path-mount-" + Chunk6bCaseId);
                    return;
                }
                if (rider.IsInCombat || horse.IsInCombat || !PrepareUnmountedHorseAiIsolation() || !PrepareCombatMountRiderAiIsolation()) return;
                if (turnBasedModeProbe == null) turnBasedModeProbe = new NativeModeTransitionProbe(Chunk6bChargePathTb);
                if (!turnBasedModeProbe.TemporaryValueIsCurrent) { turnBasedModeProbe.DispatchTemporaryValueIfRequired(); return; }
                BeginTarget(9f, Chunk6bCaseId, FindChunk6bTargetPoint());
                ruleProbe.Arm(target, false);
                chunk6bStage = 1; ResetLeafClock(); return;
            }
            if (chunk6bStage == 1)
            {
                if (!IsCombatReady(true)) return;
                if (Chunk6bChargePathTb)
                {
                    if (turn?.Unit == horse) throw new InvalidOperationException("Independent mount turn in the paired charge-path fixture.");
                    if (turn?.Unit != rider || turn.Status != TurnController.TurnStatus.Preparing && !turn.IsActing) { TryEndPhase3gFixtureTurn(turn); return; }
                    if (!rider.Commands.Empty || !horse.Commands.Empty || rider.AreHandsBusyWithAnimation || !rider.CombatState.Prepared ||
                        rider.CombatState.Cooldown.StandardAction > 0.001f || rider.CombatState.Cooldown.MoveAction > 0.001f || controller.WaitingForUI) return;
                    if (!turn.IsActing)
                    {
                        if (chunk6bEntryMove == null)
                        {
                            chunk6bEntryTurn = turn;
                            chunk6bEntry = new JObject { ["kind"] = "five-foot-step", ["before"] = CaptureChunk6bState("entry-before") };
                            chunk6bEntryMove = IssueChunk6aNativeGroundOrder(rider, FindChunk6bEntryDestination(), true, chunk6bEntry);
                            ResetLeafClock(); return;
                        }
                        if (!ReferenceEquals(turn, chunk6bEntryTurn)) throw new InvalidOperationException("The rider's native turn changed before its Acting entry settled.");
                        if (!chunk6bEntryMove.IsFinished || !rider.Commands.Empty || !horse.Commands.Empty || horse.View.AgentASP.IsReallyMoving) return;
                        chunk6bEntry["after"] = CaptureChunk6bState("entry-after");
                        chunk6bEntry["terminal"] = CaptureOrdinaryCommand(chunk6bEntryMove);
                        if (!turn.IsActing)
                        {
                            AddRow(Chunk6bCaseId, false, "The native five-foot-step entry settled without the rider turn entering Acting; no forced path was measured.", new JObject { ["level"] = "NATIVE MEASUREMENT", ["entry"] = chunk6bEntry });
                            chunk6bStage = 4; ResetLeafClock(); return;
                        }
                    }
                    else if (chunk6bEntryMove != null && (!chunk6bEntryMove.IsFinished || horse.View.AgentASP.IsReallyMoving)) return;
                }
                else if (!rider.Commands.Empty || !horse.Commands.Empty || rider.AreHandsBusyWithAnimation || horse.View.AgentASP.IsReallyMoving) return;
                chunk6bGeometry = CaptureChunk6bGeometry();
                chunk6bBefore = CaptureChunk6bState("before");
                chunk6bAttackRulesBefore = ruleProbe.PairAttackRuleCount;
                var agent = horse.View.AgentASP;
                chunk6bChargingBefore = agent.IsCharging;
                chunk6bSpeedBefore = agent.MaxSpeedOverride;
                var speedApplied = Math.Max(chunk6bSpeedBefore ?? 0f, horse.CombatSpeedMps * 2f);
                var riderAgentBefore = new { rider.View.AgentASP.IsCharging, rider.View.AgentASP.MaxSpeedOverride, rider.View.AgentASP.enabled };
                // The three stock calls, on the mount agent only; nothing on the rider agent, no command, no cost.
                agent.IsCharging = true; chunk6bChargingApplied = true;
                agent.MaxSpeedOverride = speedApplied;
                agent.ForcePath(new ForcedPath(new List<Vector3> { horse.Position, target.Position }), 1000000f);
                var riderAgentTouched = rider.View.AgentASP.IsCharging != riderAgentBefore.IsCharging ||
                    rider.View.AgentASP.MaxSpeedOverride != riderAgentBefore.MaxSpeedOverride || rider.View.AgentASP.enabled != riderAgentBefore.enabled;
                chunk6bLease = new JObject
                {
                    ["chargingBefore"] = chunk6bChargingBefore, ["speedOverrideBefore"] = chunk6bSpeedBefore,
                    ["chargingApplied"] = true, ["speedOverrideApplied"] = speedApplied, ["forcePathApplied"] = true,
                    ["forceModeAfterApply"] = Chunk6bForceMode, ["riderAgentTouched"] = riderAgentTouched,
                    ["startedFrame"] = Time.frameCount, ["startedGameSeconds"] = Chunk6bNow, ["destination"] = CapturePosition(target.Position)
                };
                chunk6bOrigin = horse.Position; chunk6bLastPosition = horse.Position;
                chunk6bLineDirection = (target.Position - horse.Position); chunk6bLineDirection.y = 0f; chunk6bLineDirection.Normalize();
                chunk6bStarted = chunk6bLastTime = Chunk6bNow; chunk6bLastSample = -1;
                chunk6bStage = 2; ResetLeafClock(); return;
            }
            if (chunk6bStage == 2)
            {
                var agent = horse.View.AgentASP;
                var now = Chunk6bNow;
                var elapsed = now - chunk6bStarted;
                var step = HorizontalDistance(horse.Position, chunk6bLastPosition);
                var dt = now - chunk6bLastTime;
                if (dt > 0 && step > 0) chunk6bPeakSpeed = Math.Max(chunk6bPeakSpeed, (float)(step / dt));
                chunk6bMoved += step;
                var offset = horse.Position - chunk6bOrigin; offset.y = 0f;
                var along = Vector3.Dot(offset, chunk6bLineDirection);
                chunk6bLateral = Math.Max(chunk6bLateral, (offset - chunk6bLineDirection * along).magnitude);
                chunk6bLastPosition = horse.Position; chunk6bLastTime = now;
                chunk6bCommandsEmpty &= rider.Commands.Empty && horse.Commands.Empty;
                chunk6bChargingThroughout &= agent.IsCharging;
                var distanceToTarget = horse.DistanceTo(target);
                if (elapsed - chunk6bLastSample >= 0.1)
                {
                    chunk6bLastSample = elapsed;
                    chunk6bSamples.Add(new JObject
                    {
                        ["nativeSeconds"] = elapsed, ["frame"] = Time.frameCount, ["mountPosition"] = CapturePosition(horse.Position),
                        ["riderPosition"] = CapturePosition(rider.Position), ["distanceToTarget"] = distanceToTarget,
                        ["mountMoving"] = agent.IsReallyMoving, ["forceMode"] = Chunk6bForceMode, ["speedMps"] = dt > 0 ? step / dt : 0,
                        ["riderCommandsEmpty"] = rider.Commands.Empty, ["mountCommandsEmpty"] = horse.Commands.Empty,
                        ["riderMove"] = rider.CombatState.Cooldown.MoveAction, ["riderStandard"] = rider.CombatState.Cooldown.StandardAction,
                        ["mountMove"] = horse.CombatState.Cooldown.MoveAction, ["mountStandard"] = horse.CombatState.Cooldown.StandardAction,
                        ["turnTimeMoved"] = turn?.TimeMoved, ["turnTimeForced"] = turn?.TimeMovedInForceMode,
                        ["pairMovement"] = combat.LastPairedMovementObservation, ["charging"] = agent.IsCharging,
                        ["descriptorCharging"] = horse.Descriptor.State.IsCharging
                    });
                }
                var arrived = distanceToTarget <= Chunk6bReach;
                var interrupt = chunk6bCase == 1 && (chunk6bMoved >= Chunk6bInterruptAfterMetres || elapsed >= Chunk6bInterruptAfterSeconds && chunk6bMoved > 0.1f);
                var stalled = elapsed > 1.0 && !agent.IsReallyMoving;
                var tooFar = chunk6bMoved > (float)chunk6bGeometry["maximumRange"];
                var timedOut = elapsed > (Chunk6bChargePathTb ? 6.0 : 12.0);
                if (!(arrived || interrupt || stalled || tooFar || timedOut)) return;
                chunk6bStopReason = arrived ? "arrival" : interrupt ? "interrupt" : stalled ? "stalled" : tooFar ? "distance" : "timeout";
                horse.View.StopMoving();
                chunk6bStoppedAt = now;
                chunk6bStage = 3; ResetLeafClock(); return;
            }
            if (chunk6bStage == 3)
            {
                var agent = horse.View.AgentASP;
                if (agent.IsReallyMoving && Chunk6bNow - chunk6bStoppedAt < 3.0) return;
                var settleSeconds = Chunk6bNow - chunk6bStoppedAt;
                // Restore the lease exactly: the charging counter back by the one increment, the override as found.
                if (chunk6bChargingApplied) { agent.IsCharging = false; chunk6bChargingApplied = false; }
                agent.MaxSpeedOverride = chunk6bSpeedBefore;
                var after = CaptureChunk6bState("after");
                var afterAgents = (JObject)after["agents"];
                var restoration = new JObject
                {
                    ["chargingRestored"] = agent.IsCharging == chunk6bChargingBefore,
                    ["speedOverrideRestored"] = agent.MaxSpeedOverride == chunk6bSpeedBefore,
                    ["forceModeCleared"] = !Chunk6bForceMode
                };
                var costs = new JObject
                {
                    ["riderStandardDelta"] = rider.CombatState.Cooldown.StandardAction - (float)chunk6bBefore["rider"]["standard"],
                    ["riderMoveDelta"] = rider.CombatState.Cooldown.MoveAction - (float)chunk6bBefore["rider"]["move"],
                    ["mountStandardDelta"] = horse.CombatState.Cooldown.StandardAction - (float)chunk6bBefore["mount"]["standard"],
                    ["mountMoveDelta"] = horse.CombatState.Cooldown.MoveAction - (float)chunk6bBefore["mount"]["move"]
                };
                var distanceToTarget = horse.DistanceTo(target);
                var evidence = new JObject
                {
                    ["level"] = "NATIVE MEASUREMENT", ["mode"] = Chunk6bChargePathTb ? "TB" : "RT", ["case"] = Chunk6bCaseId,
                    ["mounted"] = relationship.State == RelationshipState.Mounted && relationship.Rider == rider && relationship.Mount == horse,
                    ["geometry"] = chunk6bGeometry, ["entry"] = chunk6bEntry, ["lease"] = chunk6bLease, ["before"] = chunk6bBefore,
                    ["samples"] = chunk6bSamples.DeepClone(),
                    ["stop"] = new JObject
                    {
                        ["reason"] = chunk6bStopReason, ["frame"] = Time.frameCount, ["elapsedSeconds"] = chunk6bStoppedAt - chunk6bStarted,
                        ["settleSeconds"] = settleSeconds, ["distanceToTarget"] = distanceToTarget, ["movedDistance"] = chunk6bMoved,
                        ["maximumLateralDeviation"] = chunk6bLateral, ["peakSpeedMps"] = chunk6bPeakSpeed
                    },
                    ["after"] = new JObject
                    {
                        ["state"] = after, ["forceMode"] = (bool)afterAgents["forceMode"], ["mountMoving"] = (bool)afterAgents["mountMoving"],
                        ["charging"] = (bool)afterAgents["charging"], ["descriptorCharging"] = (bool)afterAgents["descriptorCharging"],
                        ["speedOverride"] = afterAgents["speedOverride"], ["turn"] = after["turn"]
                    },
                    ["restoration"] = restoration, ["costs"] = costs,
                    ["reach"] = new JObject { ["radius"] = Chunk6bReach, ["arrivalWithinReach"] = distanceToTarget <= Chunk6bReach },
                    ["commandsObservedEmptyThroughout"] = chunk6bCommandsEmpty, ["chargingObservedThroughout"] = chunk6bChargingThroughout,
                    ["attackRules"] = ruleProbe.PairAttackRuleCount - chunk6bAttackRulesBefore
                };
                // Structural integrity only: the lease was applied and restored and the samples exist. The
                // external reader decides whether the measured behaviour is the lawful one.
                var structural = chunk6bLease != null && chunk6bSamples.Count >= 1 && (bool)restoration["chargingRestored"] &&
                    (bool)restoration["speedOverrideRestored"];
                AddRow(Chunk6bCaseId, structural,
                    chunk6bCase == 0 ? "The mount agent followed a forced straight path at charge speed to the pair reach with no command and no cost."
                        : "An interrupted forced path stopped at once and left no force-mode, speed or charging residue.", evidence);
                SelectionManager.Instance.Stop();
                chunk6bStage = 4; ResetLeafClock(); return;
            }
            if (chunk6bStage == 4)
            {
                if (combat.HasActiveCommand || horse.View.AgentASP.IsReallyMoving) return;
                TryLeaveCombat(target); TryLeaveCombat(rider); TryLeaveCombat(horse);
                if (targetService != null)
                {
                    if (!targetService.DestroyAndVerify()) return;
                    targetService.Dispose(); targetService = null; target = null;
                }
                if (turnBasedModeProbe != null) { turnBasedModeProbe.Dispose(); turnBasedModeProbe = null; }
                if (CombatController.IsInTurnBasedCombat() || controller.Initialized || game.Player.IsInCombat ||
                    !rider.Commands.Empty || !horse.Commands.Empty) return;
                if (!RestoreCombatMountRiderAiIsolation() || !RestoreUnmountedHorseAiIsolation())
                    throw new InvalidOperationException("Charge-path fixture AI restoration failed.");
                combatMountRiderAiLease = null; unmountedHorseAiLease = null; unmountedHorseAiSettleRequested = false;
                chunk6bCase++;
                ResetChunk6bCase();
                chunk6bStage = 0;
                ResetLeafClock();
                if (chunk6bCase >= Chunk6bChargePathCases.Length) BeginCleanup();
            }
        }
    }
}
