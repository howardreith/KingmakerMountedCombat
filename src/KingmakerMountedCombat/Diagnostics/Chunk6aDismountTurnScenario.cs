using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker.View;
using Kingmaker.Controllers.Combat;
using Kingmaker;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using KingmakerMountedCombat.Domain;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        private JObject chunk6aDismountTurn;
        private NativeTurnCompletionProbe chunk6aDismountCompletion;
        private TurnController chunk6aDismountNextTurn;
        private UnitMoveTo chunk6aDismountGround;
        private int chunk6aDismountTurnStage;
        private bool chunk6aDismountPriorReactionObservation;

        private Vector3 FindChunk6aDismountTargetPosition()
        {
            if (AstarPath.active == null || rider.IsInCombat || horse.IsInCombat || Game.Instance.Player.IsInCombat)
                throw new InvalidOperationException("Full TB target placement requires fresh exploration geometry.");
            // Frozen131 default rider-centred spawn overlapped the Horse (1.056m).
            // A 2.2m ring leaves the native 0.9m Horse and 0.7m stock target clear,
            // and admits a 0.5-0.7m ground proposal inside the Horse's melee envelope.
            // This is a diagnostic spawn proposal, verified against the actual target after Spawn.
            const float targetCorpulence = 0.7f, separation = 2.2f;
            var direction = horse.Position - rider.Position; direction.y = 0;
            if (direction.sqrMagnitude < 0.01f) throw new InvalidOperationException("Full TB target direction is degenerate.");
            direction.Normalize();
            var candidates = new JArray();
            observations["chunk6aDismountTargetPlacement"] = new JObject {
                ["contract"] = "pre-combat-clear-target-and-bounded-ground-proposal",
                ["horseOrigin"] = CapturePosition(horse.Position), ["riderOrigin"] = CapturePosition(rider.Position),
                ["horseCorpulence"] = horse.View.Corpulence, ["proposedTargetCorpulence"] = targetCorpulence,
                ["separation"] = separation, ["candidates"] = candidates
            };
            for (var index = 0; index < 24; index++)
            {
                var angle = index == 0 ? 0 : (index % 2 == 0 ? index : -index) * 15;
                var requested = horse.Position + Quaternion.Euler(0, angle, 0) * direction * separation;
                var nearest = AstarPath.active.GetNearest(requested); var point = nearest.clampedPosition;
                var distance = HorizontalDistance(point, horse.Position);
                var blockers = Game.Instance.State.Units.Where(unit => unit.IsInState && unit.View != null &&
                    HorizontalDistance(point, unit.Position) < targetCorpulence + unit.View.Corpulence + 0.05f)
                    .Select(unit => unit.UniqueId).ToArray();
                var clear = nearest.node != null && nearest.node.Walkable && blockers.Length == 0 &&
                    Math.Abs(distance - separation) <= MountedCombatSpatialPolicy.DiagnosticPlacementTolerance;
                candidates.Add(new JObject { ["requested"] = CapturePosition(requested), ["point"] = CapturePosition(point),
                    ["distance"] = distance, ["blockers"] = new JArray(blockers), ["clear"] = clear });
                if (clear) return point;
            }
            throw new InvalidOperationException("Full TB has no bounded clear native target placement before combat.");
        }

        private void VerifyChunk6aDismountTargetPlacement()
        {
            var placement = (JObject)observations["chunk6aDismountTargetPlacement"];
            placement["actualTargetId"] = target.UniqueId;
            placement["actualTargetPosition"] = CapturePosition(target.Position);
            placement["actualTargetCorpulence"] = target.View.Corpulence;
            placement["actualHorseSeparation"] = HorizontalDistance(horse.Position, target.Position);
            if (Math.Abs(target.View.Corpulence - (float)placement["proposedTargetCorpulence"]) > 0.0001f ||
                (float)placement["actualHorseSeparation"] < horse.View.Corpulence + target.View.Corpulence + 0.05f)
                throw new InvalidOperationException("Full TB spawned target violates its actual native clearance precondition.");
            placement["preCombatGroundProposal"] = CapturePosition(FindChunk6aDismountGroundDestination());
        }

        private JObject CaptureChunk6aDismountEngagement()
        {
            return new JObject {
                ["targetId"] = target.UniqueId, ["targetObject"] = RuntimeHelpers.GetHashCode(target),
                ["targetPosition"] = CapturePosition(target.Position), ["targetMoving"] = target.HasMotionThisTick,
                ["targetCorpulence"] = target.View.Corpulence, ["riderCorpulence"] = rider.View.Corpulence,
                ["mountCorpulence"] = horse.View.Corpulence,
                ["riderEngagesTarget"] = rider.IsEngage(target), ["mountEngagesTarget"] = horse.IsEngage(target),
                ["targetEngagesRider"] = target.IsEngage(rider), ["targetEngagesMount"] = target.IsEngage(horse),
                ["riderTrackedTarget"] = rider.CombatState.EngagedUnits.Contains(target),
                ["mountTrackedTarget"] = horse.CombatState.EngagedUnits.Contains(target),
                ["riderMotion"] = rider.HasMotionThisTick, ["mountMotion"] = horse.HasMotionThisTick,
                ["frame"] = Time.frameCount
            };
        }

        private Vector3 FindChunk6aDismountGroundDestination()
        {
            var origin = horse.Position;
            var radius = float.PositiveInfinity;
            foreach (var actor in new[] { rider, horse })
            {
                var native = new UnitAttack(target); native.Init(actor);
                var ranges = native.CreateFullAttack().Select(attack => attack.WeaponRange).ToArray();
                if (ranges.Length == 0) throw new InvalidOperationException("Dismount setup has no native attack adjacency plan.");
                radius = Math.Min(radius, horse.View.Corpulence + target.View.Corpulence + ranges.Min());
            }
            // The existing selector subtracts 0.4m. Cap its endpoint ring at current
            // separation: this short native move enters Acting without retreating.
            radius = Math.Min(radius, HorizontalDistance(origin, target.Position) + 0.4f);
            var routes = new JArray();
            observations["chunk6aDismountGroundRoutes"] = routes;
            return FindNativeAttackFixturePoint(horse, true, origin, 0.5f, radius,
                "chunk6aDismountGroundCandidates", 0.7f, point => {
                    var end = ObstacleAnalyzer.TraceAlongNavmesh(origin, point);
                    var footprint = NativeGroundMovementObservation.CaptureFootprint(horse, point);
                    var residuals = ((JArray)footprint["probes"]).Select(p => (float)p["residual"]).ToArray();
                    var routeResidual = HorizontalDistance(end, point);
                    var clear = routeResidual <= MountedCombatSpatialPolicy.DiagnosticPlacementTolerance &&
                        residuals.Length > 0 && residuals.All(r => !float.IsNaN(r) && !float.IsInfinity(r) &&
                            r <= MountedCombatSpatialPolicy.DiagnosticPlacementTolerance);
                    routes.Add(new JObject { ["point"] = CapturePosition(point), ["routeEnd"] = CapturePosition(end),
                        ["routeResidual"] = routeResidual, ["footprint"] = footprint, ["clear"] = clear });
                    return clear;
                });
        }

        private void ObserveChunk6aDismountGroundEngagement(JObject ground)
        {
            var before = ground["engagementBefore"];
            var sample = CaptureChunk6aDismountEngagement();
            ((JArray)ground["engagementSamples"]).Add(sample);
            foreach (var field in new[] { "targetId", "targetObject", "targetPosition" })
                if (!JToken.DeepEquals(before[field], sample[field]))
                    throw new InvalidOperationException("Dismount ground setup changed the stationary exact target: " + field);
            if ((bool)sample["targetMoving"])
                throw new InvalidOperationException("Dismount ground setup target began a native movement.");
            foreach (var field in new[] { "riderEngagesTarget", "mountEngagesTarget", "targetEngagesRider", "targetEngagesMount",
                "riderTrackedTarget", "mountTrackedTarget" })
                if ((bool)before[field] && !(bool)sample[field])
                    throw new InvalidOperationException("Dismount ground setup lost native engagement: " + field);
        }

        private JObject CaptureChunk6aDismountBoundary()
        {
            var b = CaptureChunk6aOrderBoundary();
            b["riderCommands"] = CaptureOrdinaryActor(rider);
            b["mountCommands"] = CaptureOrdinaryActor(horse);
            return b;
        }

        private static JObject Chunk6aTurnResources(JToken b) => new JObject {
            ["frame"] = b["frame"].DeepClone(), ["gameTicks"] = b["gameTicks"].DeepClone(),
            ["allocationSequence"] = b["allocationSequence"].DeepClone(), ["round"] = b["round"].DeepClone(),
            ["rider"] = b["riderResources"].DeepClone(), ["mount"] = b["mountResources"].DeepClone()
        };

        private JObject Chunk6aDismountPassive(JObject before, JObject after)
        {
            var p = new JObject { ["contract"] = "same-allocation-no-command-cost-with-observed-native-time-only",
                ["riderId"] = rider.UniqueId, ["mountId"] = horse.UniqueId, ["turnBased"] = true,
                ["traceComplete"] = allocationTrace.Complete, ["observerHooks"] = allocationTrace.ObserverHooks,
                ["before"] = before, ["after"] = after,
                ["events"] = new JArray(allocationTrace.EventsSince((int)before["allocationSequence"])
                    .Take((int)after["allocationSequence"] - (int)before["allocationSequence"])) };
            NativePassiveResourceEvidence.AssertComplete(p); return p;
        }
        private void CaptureChunk6aDismountReady()
        {
            if (chunk6aDismountTurn == null) return;
            var ready = CaptureChunk6aDismountBoundary();
            chunk6aDismountTurn["ready"] = ready;
            var expenditureAfter = chunk6aDismountTurn["groundSetup"]?["after"] ?? chunk6aDismountTurn["riderAttack"]?["after"];
            chunk6aDismountTurn["readyBridge"] = Chunk6aDismountPassive(Chunk6aTurnResources(expenditureAfter), Chunk6aTurnResources(ready));
        }

        // CM05-tb permits a later native turn. The immediate-after-Mount claim is separate.
        // An idle Acting turn cannot regain Move merely by polling availability.
        private bool PrepareChunk6aDismountTurn(TurnController turn)
        {
            if (!Chunk6aLaterTurnDismount) return true;
            var controller = Game.Instance.TurnBasedCombatController;
            if (relationship.State != RelationshipState.Mounted || !CombatController.IsInTurnBasedCombat() ||
                !rider.IsInCombat || !horse.IsInCombat || !Game.Instance.Player.IsInCombat)
                throw new InvalidOperationException("Full TB Dismount setup lost the live mounted encounter.");
            if (turn?.Unit == horse)
                throw new InvalidOperationException("Full TB Dismount setup observed an independent mounted partner turn.");
            if (chunk6aDismountTurnStage == 0)
            {
                var availability = nativeControls.Evaluate(NativeMountedControlKind.Dismount, rider);
                observations["chunk6aDismountWait"] = new JObject {
                    ["expectedEvent"] = !Chunk6aIdle ? "current-native-command-terminal" : "one-player-End-Turn-input",
                    ["availabilityReason"] = availability.Reason, ["enabled"] = availability.IsEnabled,
                    ["boundary"] = CaptureChunk6aDismountBoundary()
                };
                if (!Chunk6aIdle) return false;
                if (!ReferenceEquals(turn, chunk6aMountTurn) || turn?.Unit != rider || !turn.IsActing)
                    throw new InvalidOperationException("Full TB Dismount setup lost the exact adopted Acting rider turn.");
                if (availability.IsEnabled) return true;
                if (availability.Reason != "The rider has no Move action available to dismount.")
                    throw new InvalidOperationException("Idle full TB Dismount has no resolving native operation: " + availability.Reason);
                if (controller.WaitingForUI || GetPendingNextUnit(controller) != null || !turn.CanEndTurnAndNoActing())
                    throw new InvalidOperationException("Spent idle rider cannot accept ordinary End Turn; exact state retained in chunk6aDismountWait.");
                if (!EnsureChunk6aRiderSelection("CM05-combat-dismount-accepted")) return false;
                chunk6aDismountPriorReactionObservation = allocationTrace.ObserveReactionResources;
                allocationTrace.ObserveReactionResources = true;
                chunk6aDismountCompletion = new NativeTurnCompletionProbe(allocationTrace, rider, horse, combat);
                var before = CaptureChunk6aDismountBoundary();
                chunk6aDismountTurn = new JObject {
                    ["contract"] = "full-tb-later-native-turn-dismount",
                    ["riderId"] = rider.UniqueId, ["mountId"] = horse.UniqueId,
                    ["reason"] = availability.Reason, ["beforeEndInput"] = before,
                    ["endInput"] = new JObject { ["method"] = "Kingmaker.Game.PauseBind", ["token"] = "06000CB7",
                        ["moduleMvid"] = typeof(Game).Assembly.ManifestModule.ModuleVersionId.ToString(), ["count"] = 1 }
                };
                var mountProof = chunk6aCommandProofs.OfType<JObject>().Single(p => (string)p["window"] == "positive-mount");
                var terminal = mountProof["samples"].Single(s => (string)s["boundary"] == "terminal");
                var terminalResources = new JObject { ["frame"] = terminal["frame"].DeepClone(), ["gameTicks"] = terminal["gameTicks"].DeepClone(),
                    ["allocationSequence"] = terminal["allocationSequence"].DeepClone(),
                    ["rider"] = terminal["nativeAllocation"]["rider"].DeepClone(), ["mount"] = terminal["nativeAllocation"]["mount"].DeepClone() };
                chunk6aDismountTurn["mountTerminalBridge"] = Chunk6aDismountPassive(terminalResources, Chunk6aTurnResources(before));

                observations["chunk6aDismountTurn"] = chunk6aDismountTurn;
                chunk6aDismountTurnStage = 1; // Set before input: never repeat this rider's End.
                Game.Instance.PauseBind();
                chunk6aDismountTurn["afterEndInput"] = CaptureChunk6aDismountBoundary();
                ResetLeafClock(); return false;
            }
            if (chunk6aDismountTurnStage == 1)
            {
                var passing = CombatController.IsPassing();
                var pending = GetPendingNextUnit(controller) != null;
                var nextBoundary = CaptureChunk6aDismountBoundary();
                observations["chunk6aDismountWait"] = new JObject {
                    ["expectedEvent"] = controller.WaitingForUI ? "native-turn-UI-completion" :
                        passing || pending ? "native-next-unit-installation" :
                        !Chunk6aIdle ? "current-native-command-terminal" :
                        turn?.Unit != rider ? "ordinary-fixture-actor-turn-completion" : "native-rider-turn-completion",
                    ["nativePassing"] = passing, ["pendingNextUnit"] = pending,
                    ["waitingForUI"] = (bool)controller.WaitingForUI, ["boundary"] = nextBoundary
                };
                if (controller.RoundNumber > chunk6aMountRound + 1 || combat.PairedActivationSequence > 2)
                    throw new InvalidOperationException("Full TB Dismount skipped its exact next native allocation.");
                if (controller.RoundNumber != chunk6aMountRound + 1 || turn?.Unit != rider)
                {
                    if (turn?.Unit != rider) TryEndPhase3gFixtureTurn(turn);
                    else if (Chunk6aIdle && turn.IsActing && !passing && !pending && !controller.WaitingForUI)
                        throw new InvalidOperationException("The one ordinary End input left the exact idle rider Acting; no pending native event can advance Dismount.");
                    return false;
                }
                if (!Chunk6aIdle || passing || pending || controller.WaitingForUI) return false;
                // Pair preparation is synchronous. An idle installed next rider cannot repair absent pairing by waiting.
                NativeDismountGroundEvidence.AssertNextPair(nextBoundary, rider.UniqueId, horse.UniqueId);
                if (ReferenceEquals(turn, chunk6aMountTurn))
                    throw new InvalidOperationException("Next-round Dismount reused the ended rider context.");
                chunk6aDismountNextTurn = turn;
                var next = CaptureChunk6aDismountBoundary();
                chunk6aDismountTurn["nextRound"] = next;
                var before = chunk6aDismountTurn["beforeEndInput"];
                var continuation = new JObject {
                    ["contract"] = "ordinary-paired-end-through-one-native-next-preparation",
                    ["traceComplete"] = allocationTrace.Complete, ["riderId"] = rider.UniqueId, ["mountId"] = horse.UniqueId,
                    ["before"] = Chunk6aTurnResources(before), ["after"] = Chunk6aTurnResources(next),
                    ["completion"] = chunk6aDismountCompletion.Capture(), ["observerHooks"] = allocationTrace.ObserverHooks,
                    ["events"] = new JArray(allocationTrace.EventsSince((int)before["allocationSequence"])
                        .Take((int)next["allocationSequence"] - (int)before["allocationSequence"]))
                };
                chunk6aDismountTurn["continuation"] = continuation;
                NativeAllocationContinuationEvidence.AssertComplete(continuation);
                chunk6aDismountCompletion.Dispose(); chunk6aDismountCompletion = null;
                chunk6aDismountTurnStage = 2; ResetLeafClock();
            }
            if (!ReferenceEquals(turn, chunk6aDismountNextTurn))
                throw new InvalidOperationException("Full TB Dismount setup changed its next rider context.");
            if (chunk6aDismountTurnStage == 2)
            {
                if (!Chunk6aIdle) return false;
                if (!EnsureChunk6aRiderSelection("CM05-combat-dismount-accepted")) return false;
                if (turn.Status != TurnController.TurnStatus.Preparing)
                    throw new InvalidOperationException("Expected the observed next native Preparing allocation before mounted ground input.");
                if (Chunk6aLaterTurnRiderAttack)
                {
                    // CM05-after-rider-expenditure: the mounted rider's single ranged attack is
                    // the next allocation's Acting entry and its Standard expenditure.
                    var attack = new JObject { ["kind"] = "single-attack", ["turnObject"] = RuntimeHelpers.GetHashCode(turn),
                        ["round"] = controller.RoundNumber, ["before"] = CaptureChunk6aEconomyBoundary() };
                    chunk6aDismountTurn["riderAttack"] = attack;
                    chunk6aDismountTurn["attackStartBridge"] = Chunk6aDismountPassive(Chunk6aTurnResources(chunk6aDismountTurn["nextRound"]), Chunk6aTurnResources(attack["before"]));
                    chunk6aEconomyLaterAttack = IssueChunk6aNativeAttack(rider, false, attack);
                    chunk6aDismountTurnStage = 3; ResetLeafClock(); return false;
                }
                var engagement = CaptureChunk6aDismountEngagement();
                if ((bool)engagement["targetMoving"] || !target.Commands.Empty)
                    throw new InvalidOperationException("Dismount ground setup requires the exact stationary diagnostic target.");
                var point = FindChunk6aDismountGroundDestination();
                var ground = new JObject { ["before"] = CaptureChunk6aDismountBoundary(),
                    ["destination"] = CapturePosition(point), ["origin"] = CapturePosition(horse.Position),
                    ["engagementBefore"] = engagement, ["engagementSamples"] = new JArray() };
                chunk6aDismountTurn["groundSetup"] = ground;
                chunk6aDismountTurn["groundStartBridge"] = Chunk6aDismountPassive(Chunk6aTurnResources(chunk6aDismountTurn["nextRound"]), Chunk6aTurnResources(ground["before"]));
                using (var input = new NativeOrdinaryAttackInput(point))
                {
                    input.Predict(); var cycles = 0;
                    while ((turn.EnabledFiveFootStep || turn.EnabledSingleActionMove) && cycles++ < 8)
                    { input.Click(button: 1); input.Predict(); }
                    if (turn.EnabledFiveFootStep || turn.EnabledSingleActionMove || !input.Click())
                        throw new InvalidOperationException("Normal native ground input could not establish ordinary mounted movement.");
                    ground["modeCycles"] = cycles;
                }
                chunk6aDismountGround = horse.Commands.Move as UnitMoveTo;
                ground["admittedCommand"] = CaptureOrdinaryCommand(chunk6aDismountGround);
                if (chunk6aDismountGround?.Executor != horse || !chunk6aDismountGround.CreatedByPlayer)
                    throw new InvalidOperationException("Full TB next-turn ground input lost exact mount movement authority.");
                chunk6aDismountTurnStage = 3; ResetLeafClock(); return false;
            }
            if (chunk6aDismountTurnStage == 3 && Chunk6aLaterTurnRiderAttack)
            {
                if (!Chunk6aEconomyCommandSettled(chunk6aEconomyLaterAttack, rider)) return false;
                var attack = (JObject)chunk6aDismountTurn["riderAttack"];
                FinishChunk6aEconomyCommand(attack, rider, chunk6aEconomyLaterAttack);
                if (chunk6aEconomyLaterAttack.Result != UnitCommand.ResultType.Success || !turn.IsActing)
                    throw new InvalidOperationException("The mounted rider's native attack did not settle on the same native Acting rider turn: " + attack.ToString(Newtonsoft.Json.Formatting.None));
                chunk6aDismountTurnStage = 4;
            }
            if (chunk6aDismountTurnStage == 3)
            {
                ObserveChunk6aDismountGroundEngagement((JObject)chunk6aDismountTurn["groundSetup"]);
                observations["chunk6aDismountWait"] = new JObject {
                    ["expectedEvent"] = "exact-mounted-ground-command-terminal-and-native-Acting",
                    ["command"] = CaptureOrdinaryCommand(chunk6aDismountGround), ["boundary"] = CaptureChunk6aDismountBoundary()
                };
                if (!chunk6aDismountGround.IsFinished || !Chunk6aIdle || horse.View.AgentASP.IsReallyMoving) return false;
                var ground = (JObject)chunk6aDismountTurn["groundSetup"];
                ground["after"] = CaptureChunk6aDismountBoundary();
                ground["terminalCommand"] = CaptureOrdinaryCommand(chunk6aDismountGround);
                ground["traceComplete"] = allocationTrace.Complete;
                ground["events"] = allocationTrace.EventsSince((int)ground["before"]["allocationSequence"]);
                ground["observerHooks"] = allocationTrace.ObserverHooks;
                if (chunk6aDismountGround.Result != UnitCommand.ResultType.Success || !turn.IsActing)
                    throw new InvalidOperationException("The exact mounted ground command did not settle on the same native Acting rider turn.");
                NativeDismountGroundEvidence.AssertComplete(ground, rider.UniqueId, horse.UniqueId);
                chunk6aDismountTurnStage = 4;
            }
            if (!nativeControls.Evaluate(NativeMountedControlKind.Dismount, rider).IsEnabled)
                throw new InvalidOperationException("Completed next-turn setup still cannot legally Dismount: " +
                    nativeControls.Evaluate(NativeMountedControlKind.Dismount, rider).Reason);
            return true;
        }
        private void RestoreChunk6aDismountTurn()
        {
            chunk6aDismountCompletion?.Dispose(); chunk6aDismountCompletion = null;
            if (chunk6aDismountTurn != null && allocationTrace != null) allocationTrace.ObserveReactionResources = chunk6aDismountPriorReactionObservation;
        }
    }
}
