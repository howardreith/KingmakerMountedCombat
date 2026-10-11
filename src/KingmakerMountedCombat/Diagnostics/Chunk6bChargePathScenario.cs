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
    // diagnostic leases. No ability, no product change and no cost owned by this measurement: the mount agent
    // receives exactly the calls the stock charge makes on its caster's agent (the charging flag, a doubled
    // speed override and a forced straight path to the target) while the rider agent stays under the mount's
    // movement authority, and every leased value is restored exactly.
    //
    // Preview.156 measured (and the decompiled engine confirmed, read-only) that UnitActionController stops every
    // unit whose command container is empty on every tick, so a forced path on the mount lives only while the
    // mount holds a live command; the stock charge's caster holds its running engage-unit ability for the whole
    // path. The measurement therefore runs the forced path under the pair's own admitted delegated ground move
    // (a native UnitMoveTo created by a native ground click, executed by the mount: the pathway qualified in
    // Chunk 6A) as its carrier, and re-applies the forced path whenever the agent leaves force mode, exactly as
    // the stock runtime routine re-forces it. UnitMovementAgent.Stop() leaves m_IsInForceMode latched until the
    // next OnPathComplete (the stock charge leaves the same latch), so the latch is recorded after the stop and
    // then proven cleared by the next lawful pair path (a residue probe). The compiled side records facts and
    // checks its own structure; scripts/runtime/Chunk6bChargePathEvidence.ps1 is the acceptance authority.
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
        private const float Chunk6bForcedApproachRadius = 1000000f;

        private int chunk6bCase, chunk6bStage, chunk6bReforces;
        private bool chunk6bControlSent;
        private JObject chunk6bGeometry, chunk6bBefore, chunk6bLease, chunk6bCarrier, chunk6bProbe;
        private readonly JArray chunk6bSamples = new JArray();
        private bool chunk6bChargingBefore, chunk6bChargingApplied;
        private float? chunk6bSpeedBefore;
        private Vector3 chunk6bOrigin, chunk6bLineDirection, chunk6bLastPosition;
        private double chunk6bStarted, chunk6bLastSample, chunk6bLastTime, chunk6bStoppedAt;
        private int chunk6bLastSampleFrame;
        private float chunk6bMoved, chunk6bLateral, chunk6bPeakSpeed;
        private string chunk6bStopReason;
        private bool chunk6bRiderCommandsEmpty = true, chunk6bMountOnlyCarrier = true, chunk6bChargingThroughout = true;
        private int chunk6bAttackRulesBefore, chunk6bOpportunityAttackRulesBefore, chunk6bNonOpportunityAttackRulesBefore;
        private UnitMoveTo chunk6bCarrierMove, chunk6bProbeMove;
        private JObject chunk6bAfter, chunk6bRestoration, chunk6bCosts;
        private float chunk6bProbeOriginDistance;
        private bool chunk6bProbeMoved;
        private JObject chunk6bTurnAdvance;
        private TurnController chunk6bTurnBefore;
        private bool chunk6bEndTurnClicked;
        private double chunk6bTurnAdvanceStarted;
        private TurnController chunk6bLastSeenTurn;

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
                ["riderAvoidanceDisabled"] = rider.View.MovementAgent.AvoidanceDisabled,
                ["mountAvoidanceDisabled"] = horse.View.MovementAgent.AvoidanceDisabled
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

        private JObject CaptureChunk6bCosts()
        {
            return new JObject
            {
                ["riderStandardDelta"] = rider.CombatState.Cooldown.StandardAction - (float)chunk6bBefore["rider"]["standard"],
                ["riderMoveDelta"] = rider.CombatState.Cooldown.MoveAction - (float)chunk6bBefore["rider"]["move"],
                ["mountStandardDelta"] = horse.CombatState.Cooldown.StandardAction - (float)chunk6bBefore["mount"]["standard"],
                ["mountMoveDelta"] = horse.CombatState.Cooldown.MoveAction - (float)chunk6bBefore["mount"]["move"]
            };
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

        private Vector3 Chunk6bLineDirectionFromMount()
        {
            var direction = target.Position - horse.Position; direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f) throw new InvalidOperationException("The charge line is degenerate.");
            direction.Normalize();
            return direction;
        }

        // The carrier's own ground destination: on the charge line, a pair reach short of the target, so the
        // native ground click never lands on the target unit. The forced path itself ends at the target position.
        private Vector3 FindChunk6bCarrierDestination()
        {
            var direction = Chunk6bLineDirectionFromMount();
            var wanted = target.Position - direction * Math.Max(Chunk6bReach, 1.0f);
            var actual = ObstacleAnalyzer.TraceAlongNavmesh(horse.Position, wanted);
            if (GeometryUtils.MechanicsDistance(actual, wanted) > 0.25f)
                throw new InvalidOperationException("The carrier destination on the charge line is not natively reachable.");
            return actual;
        }

        // The residue probe: a short lawful pair path away from the target, so the next OnPathComplete can be
        // observed clearing the latched force mode.
        private Vector3 FindChunk6bProbeDestination()
        {
            var back = -Chunk6bLineDirectionFromMount();
            for (var i = 0; i < 16; i++)
            {
                var wanted = horse.Position + Quaternion.Euler(0f, i * 22.5f, 0f) * back * 1.5f;
                var actual = ObstacleAnalyzer.TraceAlongNavmesh(horse.Position, wanted);
                if (GeometryUtils.MechanicsDistance(actual, wanted) <= 0.25f && GeometryUtils.MechanicsDistance(actual, horse.Position) > 0.75f) return actual;
            }
            throw new InvalidOperationException("No native walkable residue-probe destination exists.");
        }

        // One native ground order for the mounted pair: the stock pointer input (cursor mode cycled natively in
        // turn-based combat) and the command the pair admitted, read from the mount first (the mounted pair's
        // rider-turn ground movement is a mount-executed UnitMoveTo) and otherwise from the rider.
        private UnitMoveTo IssueChunk6bGroundOrder(Vector3 point, bool fiveFootStep, JObject record)
        {
            var turn = Game.Instance.TurnBasedCombatController.CurrentTurn;
            using (var input = new NativeOrdinaryAttackInput(point))
            {
                input.Predict(); var cycles = 0;
                if (Chunk6bChargePathTb && turn != null)
                {
                    if (fiveFootStep) { while (!turn.EnabledFiveFootStep && cycles++ < 8) { input.Click(button: 1); input.Predict(); } }
                    else { while ((turn.EnabledFiveFootStep || turn.EnabledSingleActionMove) && cycles++ < 8) { input.Click(button: 1); input.Predict(); } }
                    if (turn.EnabledFiveFootStep != fiveFootStep || (!fiveFootStep && turn.EnabledSingleActionMove))
                        throw new InvalidOperationException("Native right-click did not choose the " + (fiveFootStep ? "five-foot step" : "ordinary movement") + " mode.");
                }
                var clicked = input.Click();
                var command = horse.Commands.Move as UnitMoveTo ?? rider.Commands.Move as UnitMoveTo;
                record["input"] = new JObject
                {
                    ["kind"] = fiveFootStep ? "five-foot-step" : "ground", ["clicked"] = clicked, ["cursorCycles"] = cycles, ["frame"] = Time.frameCount,
                    ["fiveFootStep"] = turn?.EnabledFiveFootStep, ["singleActionMove"] = turn?.EnabledSingleActionMove,
                    ["point"] = CapturePosition(point), ["feedback"] = combat.LastFeedback,
                    ["rejectionCodes"] = new JArray((combat.LastRejectionCodes ?? new MountedCombatRejectionCode[0]).Select(code => code.ToString()))
                };
                record["admitted"] = CaptureOrdinaryCommand(command);
                record["executor"] = command == null ? null : command.Executor == horse ? "mount" : command.Executor == rider ? "rider" : "other";
                if (!clicked || command == null || !command.CreatedByPlayer || (command.Executor != horse && command.Executor != rider))
                    throw new InvalidOperationException("Native ground input admitted no exact player command for the mounted pair: " + record.ToString(Newtonsoft.Json.Formatting.None));
                return command;
            }
        }

        // The residue probe: the next lawful pair path, which the engine completes through OnPathComplete,
        // clearing the force mode that the stock Stop() leaves latched. A refusal is recorded, never thrown,
        // so the external reader sees the engine exact words.
        private void IssueChunk6bResidueProbe()
        {
            chunk6bProbe = new JObject { ["kind"] = "residue-probe", ["forceModeBefore"] = Chunk6bForceMode, ["before"] = CaptureChunk6bState("probe-before") };
            if (chunk6bTurnAdvance != null) chunk6bProbe["turnAdvance"] = chunk6bTurnAdvance;
            try
            {
                var probePoint = FindChunk6bProbeDestination();
                chunk6bProbe["destination"] = CapturePosition(probePoint);
                SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                chunk6bProbeMove = IssueChunk6bGroundOrder(probePoint, false, chunk6bProbe);
                chunk6bProbe["admitted"] = true;
                chunk6bProbeOriginDistance = 0f; chunk6bLastPosition = horse.Position; chunk6bProbeMoved = false;
                chunk6bStoppedAt = Chunk6bNow;
            }
            catch (InvalidOperationException exception)
            {
                chunk6bProbe["admitted"] = false; chunk6bProbe["refusal"] = exception.Message; chunk6bProbeMove = null;
            }
            chunk6bStage = chunk6bProbeMove == null ? 5 : 4; ResetLeafClock();
        }

        // The stock charge's forced path on the mover's agent: the straight line from the mount to the target
        // with the stock approach radius. Re-applied whenever the agent leaves force mode, as the stock
        // runtime routine re-forces its path.
        private void ApplyChunk6bForcedPath(string reason)
        {
            horse.View.AgentASP.ForcePath(new ForcedPath(new List<Vector3> { horse.Position, target.Position }), Chunk6bForcedApproachRadius);
            chunk6bReforces++;
            ((JArray)chunk6bLease["forcePaths"]).Add(new JObject
            {
                ["reason"] = reason, ["frame"] = Time.frameCount, ["nativeSeconds"] = Chunk6bNow, ["origin"] = CapturePosition(horse.Position),
                ["forceModeAfterApply"] = Chunk6bForceMode, ["mountMoving"] = horse.View.AgentASP.IsReallyMoving
            });
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
                ["cases"] = new JArray(Chunk6bChargePathCases), ["forceModeField"] = Chunk6bForceModeField.MetadataToken.ToString("X8"),
                ["carrier"] = "delegated-ground-move"
            };
            step = Phase3dHorseStep.Phase3gControls;
            ResetLeafClock();
        }

        private void ResetChunk6bCase()
        {
            chunk6bGeometry = null; chunk6bBefore = null; chunk6bLease = null; chunk6bCarrier = null; chunk6bProbe = null;
            chunk6bAfter = null; chunk6bRestoration = null; chunk6bCosts = null;
            chunk6bSamples.Clear(); chunk6bChargingApplied = false; chunk6bSpeedBefore = null; chunk6bChargingBefore = false;
            chunk6bMoved = chunk6bLateral = chunk6bPeakSpeed = 0f; chunk6bStopReason = null; chunk6bReforces = 0;
            chunk6bRiderCommandsEmpty = true; chunk6bMountOnlyCarrier = true; chunk6bChargingThroughout = true;
            chunk6bCarrierMove = null; chunk6bProbeMove = null;
            chunk6bControlSent = false; chunk6bProbeMoved = false; chunk6bProbeOriginDistance = 0f;
            chunk6bTurnAdvance = null; chunk6bTurnBefore = null; chunk6bEndTurnClicked = false; chunk6bTurnAdvanceStarted = 0.0;
            chunk6bLastSeenTurn = null;
        }

        private bool Chunk6bPairIdle => rider.Commands.Empty && horse.Commands.Empty && !rider.AreHandsBusyWithAnimation && !horse.View.AgentASP.IsReallyMoving;

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
                    if (!Chunk6bPairIdle || !rider.CombatState.Prepared ||
                        rider.CombatState.Cooldown.StandardAction > 0.001f || rider.CombatState.Cooldown.MoveAction > 0.001f || controller.WaitingForUI) return;
                    // Preview.157 measured that a rider five-foot-step entry consumes the pair's one granted
                    // movement for the activation, after which the delegated carrier is refused with the product's
                    // own exact reason ("The mount has no movement available in this paired activation.").
                    // The carrier is therefore the rider turn's first movement, issued from Preparing, which is
                    // also the stock charge precondition: the stock CanTarget requires CurrentTurn.TimeMoved == 0.
                    if (turn.TimeMoved > 0.0001f)
                        throw new InvalidOperationException("The rider turn had already moved before the charge path was measured.");
                }
                else if (!Chunk6bPairIdle) return;
                SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                chunk6bGeometry = CaptureChunk6bGeometry();
                chunk6bBefore = CaptureChunk6bState("before");
                    chunk6bAttackRulesBefore = ruleProbe.PairAttackRuleCount;
                chunk6bOpportunityAttackRulesBefore = ruleProbe.PairOpportunityAttackRuleCount;
                chunk6bNonOpportunityAttackRulesBefore = ruleProbe.PairNonOpportunityAttackRuleCount;
                // The carrier: the pair's own admitted delegated ground move toward the charge line.
                var carrierDestination = FindChunk6bCarrierDestination();
                chunk6bCarrier = new JObject { ["kind"] = "delegated-ground-move", ["destination"] = CapturePosition(carrierDestination) };
                chunk6bCarrierMove = IssueChunk6bGroundOrder(carrierDestination, false, chunk6bCarrier);
                chunk6bCarrier["admitted"] = true;
                chunk6bCarrier["startedMoving"] = false;
                var agent = horse.View.AgentASP;
                chunk6bChargingBefore = agent.IsCharging;
                chunk6bSpeedBefore = agent.MaxSpeedOverride;
                var speedApplied = Math.Max(chunk6bSpeedBefore ?? 0f, horse.CombatSpeedMps * 2f);
                var riderAgentBefore = new { rider.View.AgentASP.IsCharging, rider.View.AgentASP.MaxSpeedOverride, rider.View.AgentASP.enabled };
                chunk6bLease = new JObject
                {
                    ["chargingBefore"] = chunk6bChargingBefore, ["speedOverrideBefore"] = chunk6bSpeedBefore,
                    ["chargingApplied"] = true, ["speedOverrideApplied"] = speedApplied, ["forcePathApplied"] = true,
                    ["forcePaths"] = new JArray(), ["startedFrame"] = Time.frameCount, ["startedGameSeconds"] = Chunk6bNow,
                    ["destination"] = CapturePosition(target.Position), ["approachRadius"] = Chunk6bForcedApproachRadius
                };
                // The three stock calls, on the mount agent only; nothing on the rider agent, no cost of its own.
                agent.IsCharging = true; chunk6bChargingApplied = true;
                agent.MaxSpeedOverride = speedApplied;
                ApplyChunk6bForcedPath("initial");
                chunk6bLease["forceModeAfterApply"] = Chunk6bForceMode;
                chunk6bLease["riderAgentTouched"] = rider.View.AgentASP.IsCharging != riderAgentBefore.IsCharging ||
                    rider.View.AgentASP.MaxSpeedOverride != riderAgentBefore.MaxSpeedOverride || rider.View.AgentASP.enabled != riderAgentBefore.enabled;
                chunk6bOrigin = horse.Position; chunk6bLastPosition = horse.Position;
                chunk6bLineDirection = Chunk6bLineDirectionFromMount();
                chunk6bStarted = chunk6bLastTime = Chunk6bNow; chunk6bLastSample = -1; chunk6bLastSampleFrame = 0;
                chunk6bStage = 2; ResetLeafClock(); return;
            }
            if (chunk6bStage == 2)
            {
                var agent = horse.View.AgentASP;
                var carrierRunning = chunk6bCarrierMove != null && !chunk6bCarrierMove.IsFinished;
                var now = Chunk6bNow;
                var elapsed = now - chunk6bStarted;
                // Stock interplay: the carrier's own native approach replaces the path with pathfinding; the
                // measurement re-forces the straight line exactly as the stock runtime routine does.
                if (carrierRunning && !Chunk6bForceMode) ApplyChunk6bForcedPath("re-force");
                var step = HorizontalDistance(horse.Position, chunk6bLastPosition);
                var dt = now - chunk6bLastTime;
                // Preview.158 measured that in turn-based mode the game clock barely advances across the whole
                // path, so a step-over-time estimate reads zero. The agent's own Speed is the engine's value
                // and is taken as well; the peak is the larger of the two.
                if (dt > 0 && step > 0) chunk6bPeakSpeed = Math.Max(chunk6bPeakSpeed, (float)(step / dt));
                chunk6bPeakSpeed = Math.Max(chunk6bPeakSpeed, agent.Speed);
                chunk6bMoved += step;
                if (step > 0.001f) chunk6bCarrier["startedMoving"] = true;
                var offset = horse.Position - chunk6bOrigin; offset.y = 0f;
                var along = Vector3.Dot(offset, chunk6bLineDirection);
                chunk6bLateral = Math.Max(chunk6bLateral, (offset - chunk6bLineDirection * along).magnitude);
                chunk6bLastPosition = horse.Position; chunk6bLastTime = now;
                chunk6bRiderCommandsEmpty &= rider.Commands.Empty;
                chunk6bMountOnlyCarrier &= horse.Commands.Raw.All(command => command == null || ReferenceEquals(command, chunk6bCarrierMove)) && horse.Commands.Queue.Count == 0;
                chunk6bChargingThroughout &= agent.IsCharging;
                var distanceToTarget = horse.DistanceTo(target);
                // Sample on either clock: game time in real time, and frame advance in turn-based mode, where
                // the game clock advances far more slowly than the path does.
                if (chunk6bSamples.Count < 120 &&
                    (elapsed - chunk6bLastSample >= 0.1 || Time.frameCount - chunk6bLastSampleFrame >= 3))
                {
                    chunk6bLastSample = elapsed;
                    chunk6bLastSampleFrame = Time.frameCount;
                    chunk6bSamples.Add(new JObject
                    {
                        ["nativeSeconds"] = elapsed, ["frame"] = Time.frameCount, ["mountPosition"] = CapturePosition(horse.Position),
                        ["riderPosition"] = CapturePosition(rider.Position), ["distanceToTarget"] = distanceToTarget,
                        ["mountMoving"] = agent.IsReallyMoving, ["forceMode"] = Chunk6bForceMode, ["speedMps"] = dt > 0 ? step / dt : 0,
                        ["carrierRunning"] = carrierRunning, ["carrierStarted"] = chunk6bCarrierMove?.IsStarted ?? false, ["reforces"] = chunk6bReforces,
                        ["riderCommandsEmpty"] = rider.Commands.Empty, ["mountOnlyCarrier"] = chunk6bMountOnlyCarrier,
                        ["riderMove"] = rider.CombatState.Cooldown.MoveAction, ["riderStandard"] = rider.CombatState.Cooldown.StandardAction,
                        ["mountMove"] = horse.CombatState.Cooldown.MoveAction, ["mountStandard"] = horse.CombatState.Cooldown.StandardAction,
                        ["turnTimeMoved"] = turn?.TimeMoved, ["turnTimeForced"] = turn?.TimeMovedInForceMode,
                        ["pairMovement"] = combat.LastPairedMovementObservation, ["charging"] = agent.IsCharging,
                        ["descriptorCharging"] = horse.Descriptor.State.IsCharging
                    });
                }
                var arrived = distanceToTarget <= Chunk6bReach;
                var interrupt = chunk6bCase == 1 && (chunk6bMoved >= Chunk6bInterruptAfterMetres || elapsed >= Chunk6bInterruptAfterSeconds && chunk6bMoved > 0.1f);
                var carrierEnded = !carrierRunning;
                var stalled = elapsed > 1.0 && !agent.IsReallyMoving && chunk6bMoved < 0.5f;
                var tooFar = chunk6bMoved > (float)chunk6bGeometry["maximumRange"];
                var timedOut = elapsed > (Chunk6bChargePathTb ? 6.0 : 12.0);
                if (!(arrived || interrupt || carrierEnded || stalled || tooFar || timedOut)) return;
                chunk6bStopReason = arrived ? "arrival" : interrupt ? "interrupt" : carrierEnded ? "carrier-ended" : stalled ? "stalled" : tooFar ? "distance" : "timeout";
                // The measurement ends the carrier natively (the stock charge would queue its attack here).
                if (carrierRunning) chunk6bCarrierMove.Interrupt();
                chunk6bCarrier["interruptedByMeasurement"] = carrierRunning;
                chunk6bCarrier["terminal"] = CaptureOrdinaryCommand(chunk6bCarrierMove);
                chunk6bStoppedAt = now;
                chunk6bStage = 3; ResetLeafClock(); return;
            }
            if (chunk6bStage == 3)
            {
                var agent = horse.View.AgentASP;
                if ((agent.IsReallyMoving || !horse.Commands.Empty) && Chunk6bNow - chunk6bStoppedAt < 3.0) return;
                var settleSeconds = Chunk6bNow - chunk6bStoppedAt;
                // Restore the lease exactly: the charging flag back as found, the override as found.
                if (chunk6bChargingApplied) { agent.IsCharging = false; chunk6bChargingApplied = false; }
                agent.MaxSpeedOverride = chunk6bSpeedBefore;
                chunk6bAfter = CaptureChunk6bState("after");
                chunk6bRestoration = new JObject
                {
                    ["chargingRestored"] = agent.IsCharging == chunk6bChargingBefore,
                    ["speedOverrideRestored"] = agent.MaxSpeedOverride == chunk6bSpeedBefore,
                    ["forceModeAfterStop"] = Chunk6bForceMode,
                    ["carrierFinished"] = chunk6bCarrierMove != null && chunk6bCarrierMove.IsFinished,
                    ["settleSeconds"] = settleSeconds, ["mountCommandsEmpty"] = horse.Commands.Empty
                };
                chunk6bCosts = CaptureChunk6bCosts();
                // Preview.159 measured the engine own answer to a second pair path in the same turn:
                // "The mount has no movement available in this paired activation." Kingmaker remaining-
                // movement rule (MountedMovementState.Remaining, the exact GetRemainingMovementTime formula)
                // returns zero for the rest of a turn in which the actor moved in force mode, so in
                // turn-based mode no lawful pair path exists in this activation and the latch can only be
                // observed clearing in the rider next turn, which is where a player would meet it. Real
                // time has no turn budget and probes at once.
                if (Chunk6bChargePathTb) { chunk6bTurnAdvanceStarted = Chunk6bNow; chunk6bStage = 7; ResetLeafClock(); return; }
                IssueChunk6bResidueProbe();
                return;
            }
            if (chunk6bStage == 7)
            {
                // One native End Turn input, then the rider next turn. The engine allocates the new turn
                // and its movement; this measurement writes nothing and only observes the latch across the
                // boundary. The pair stays idle throughout: the rider has no queued action and the mount is
                // under the pair movement authority.
                // Every turn boundary is progress, so the harness leaf deadline measures a stall here
                // rather than the length of a lawful round.
                if (!ReferenceEquals(turn, chunk6bLastSeenTurn)) { chunk6bLastSeenTurn = turn; ResetLeafClock(); }
                if (!Chunk6bPairIdle || combat.HasActiveCommand || combat.HasActiveGroundMovement) return;
                if (Chunk6bNow - chunk6bTurnAdvanceStarted > 180.0)
                    throw new InvalidOperationException("The rider next turn did not arrive for the turn-based residue probe.");
                if (!rider.IsInCombat || !horse.IsInCombat || !CombatController.IsInTurnBasedCombat())
                    throw new InvalidOperationException("The turn-based residue probe lost its encounter before the rider next turn.");
                if (!chunk6bEndTurnClicked)
                {
                    if (turn == null || turn.Unit != rider || !turn.CanEndTurnAndNoActing() ||
                        controller.WaitingForUI || GetPendingNextUnit(controller) != null) return;
                    SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                    chunk6bTurnBefore = turn;
                    chunk6bTurnAdvance = new JObject
                    {
                        ["kind"] = "turn-advance",
                        ["reason"] = "forced movement leaves no remaining movement in the same turn",
                        ["endedTurnUnit"] = turn.Unit?.UniqueId,
                        ["endedTurnStatus"] = turn.Status.ToString(),
                        ["forceModeAtEnd"] = Chunk6bForceMode,
                        ["before"] = CaptureChunk6bState("turn-advance-before"),
                        ["input"] = new JObject { ["method"] = "Kingmaker.Game.PauseBind", ["count"] = 1 }
                    };
                    chunk6bEndTurnClicked = true;
                    Game.Instance.PauseBind();
                    return;
                }
                // Preview.160 measured the round stalling on an idle fixture party member in Preparing, so
                // the wait ends the turns it is allowed to end, exactly as the Chunk 6A turn scenarios do:
                // an exact idle pair or leased fixture actor only, never a foreign native turn.
                if (turn != null && turn.Unit != rider && !ReferenceEquals(turn, chunk6bTurnBefore))
                {
                    TryEndPhase3gFixtureTurn(turn);
                    return;
                }
                if (turn == null || ReferenceEquals(turn, chunk6bTurnBefore) || turn.Unit != rider ||
                    controller.WaitingForUI || GetPendingNextUnit(controller) != null) return;
                if (turn.Status != TurnController.TurnStatus.Preparing && !turn.IsActing) return;
                chunk6bTurnAdvance["nextTurnUnit"] = turn.Unit?.UniqueId;
                chunk6bTurnAdvance["nextTurnStatus"] = turn.Status.ToString();
                chunk6bTurnAdvance["forceModeAtNextTurn"] = Chunk6bForceMode;
                chunk6bTurnAdvance["waitedSeconds"] = Chunk6bNow - chunk6bTurnAdvanceStarted;
                chunk6bTurnAdvance["after"] = CaptureChunk6bState("turn-advance-after");
                IssueChunk6bResidueProbe();
                return;
            }
            if (chunk6bStage == 4)
            {
                var agent = horse.View.AgentASP;
                var step = HorizontalDistance(horse.Position, chunk6bLastPosition);
                chunk6bLastPosition = horse.Position;
                chunk6bProbeOriginDistance += step;
                if (!chunk6bProbeMoved && agent.IsReallyMoving && step > 0.001f)
                {
                    chunk6bProbeMoved = true;
                    chunk6bProbe["firstMove"] = new JObject { ["frame"] = Time.frameCount, ["forceMode"] = Chunk6bForceMode, ["nativeSeconds"] = Chunk6bNow - chunk6bStoppedAt };
                }
                var probeDone = chunk6bProbeMove.IsFinished && !agent.IsReallyMoving;
                var probeTimedOut = Chunk6bNow - chunk6bStoppedAt > 6.0;
                if (!probeDone && !probeTimedOut) return;
                if (!chunk6bProbeMove.IsFinished) chunk6bProbeMove.Interrupt();
                chunk6bProbe["terminal"] = CaptureOrdinaryCommand(chunk6bProbeMove);
                chunk6bProbe["movedDistance"] = chunk6bProbeOriginDistance;
                chunk6bProbe["timedOut"] = probeTimedOut;
                chunk6bProbe["forceModeAfter"] = Chunk6bForceMode;
                chunk6bProbe["after"] = CaptureChunk6bState("probe-after");
                chunk6bStage = 5; ResetLeafClock(); return;
            }
            if (chunk6bStage == 5)
            {
                var agent = horse.View.AgentASP;
                if (agent.IsReallyMoving) return;
                var distanceToTarget = horse.DistanceTo(target);
                var afterAgents = (JObject)chunk6bAfter["agents"];
                var evidence = new JObject
                {
                    ["level"] = "NATIVE MEASUREMENT", ["mode"] = Chunk6bChargePathTb ? "TB" : "RT", ["case"] = Chunk6bCaseId,
                    ["mounted"] = relationship.State == RelationshipState.Mounted && relationship.Rider == rider && relationship.Mount == horse,
                    ["geometry"] = chunk6bGeometry, ["carrier"] = chunk6bCarrier, ["lease"] = chunk6bLease, ["before"] = chunk6bBefore,
                    ["samples"] = chunk6bSamples.DeepClone(),
                    ["stop"] = new JObject
                    {
                        ["reason"] = chunk6bStopReason, ["elapsedSeconds"] = chunk6bStoppedAt > chunk6bStarted ? chunk6bStoppedAt - chunk6bStarted : 0.0,
                        ["settleSeconds"] = chunk6bRestoration["settleSeconds"], ["distanceToTarget"] = (float)chunk6bAfter["distanceToTarget"],
                        ["movedDistance"] = chunk6bMoved, ["maximumLateralDeviation"] = chunk6bLateral, ["peakSpeedMps"] = chunk6bPeakSpeed, ["reforces"] = chunk6bReforces
                    },
                    ["after"] = new JObject
                    {
                        ["state"] = chunk6bAfter, ["forceMode"] = (bool)afterAgents["forceMode"], ["mountMoving"] = (bool)afterAgents["mountMoving"],
                        ["charging"] = (bool)afterAgents["charging"], ["descriptorCharging"] = (bool)afterAgents["descriptorCharging"],
                        ["speedOverride"] = afterAgents["speedOverride"], ["turn"] = chunk6bAfter["turn"]
                    },
                    ["restoration"] = chunk6bRestoration, ["costs"] = chunk6bCosts, ["costsAfterProbe"] = CaptureChunk6bCosts(),
                    ["residueProbe"] = chunk6bProbe,
                    ["reach"] = new JObject { ["radius"] = Chunk6bReach, ["arrivalWithinReach"] = (float)chunk6bAfter["distanceToTarget"] <= Chunk6bReach, ["distanceNow"] = distanceToTarget },
                    ["riderCommandsEmptyThroughout"] = chunk6bRiderCommandsEmpty, ["mountOnlyCarrierThroughout"] = chunk6bMountOnlyCarrier,
                    ["chargingObservedThroughout"] = chunk6bChargingThroughout,
                    // The probe counts only rules the rider or the mount initiated against the armed target.
                    // A native attack of opportunity is the engine's own reflex and is recorded, never suppressed;
                    // a non-opportunity pair attack would mean the measurement delivered an attack, which it must
                    // never do. The external reader decides on the split, not on the total.
                    ["attackRules"] = ruleProbe.PairAttackRuleCount - chunk6bAttackRulesBefore,
                    ["attackRulesOpportunity"] = ruleProbe.PairOpportunityAttackRuleCount - chunk6bOpportunityAttackRulesBefore,
                    ["attackRulesNonOpportunity"] = ruleProbe.PairNonOpportunityAttackRuleCount - chunk6bNonOpportunityAttackRulesBefore,
                    ["attackRuleEvidence"] = ruleProbe.CapturePairEvidence()
                };
                // Structural integrity only: the carrier was admitted, the lease was applied and restored and the
                // samples exist. The external reader decides whether the measured behaviour is the lawful one.
                var structural = chunk6bLease != null && chunk6bCarrier != null && chunk6bSamples.Count >= 1 &&
                    (bool)chunk6bRestoration["chargingRestored"] && (bool)chunk6bRestoration["speedOverrideRestored"];
                AddRow(Chunk6bCaseId, structural,
                    chunk6bCase == 0 ? "The mount agent followed a forced straight path at charge speed to the pair reach under the pair's own admitted carrier, with no cost."
                        : "An interrupted forced path stopped at once and left no speed or charging residue; the latched force mode cleared on the next lawful path.", evidence);
                SelectionManager.Instance.Stop();
                chunk6bStage = 6; ResetLeafClock(); return;
            }
            if (chunk6bStage == 6)
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
