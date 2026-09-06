using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker;
using Kingmaker.Enums;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using KingmakerMountedCombat.Domain;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // Parameterized controls in the existing guarded Horse fixture and result
    // protocol. Native input/planning/expenditure remains the subject of the test.
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        internal const string OrdinaryAttackControlsScenario = "ordinary-attack-controls-tb";
        private bool IsOrdinaryAttackControls => request.Scenario == OrdinaryAttackControlsScenario;
        private sealed class OrdinaryCase
        {
            internal readonly string Id;
            internal readonly bool Mounted;
            internal readonly bool Primary;
            internal readonly bool Rapid;
            internal readonly int? Bab;
            internal readonly bool Haste;
            internal readonly string Preparation;
            internal OrdinaryCase(string id, bool mounted, bool primary = false, bool rapid = true,
                int? bab = null, bool haste = false, string preparation = "fresh")
            { Id = id; Mounted = mounted; Primary = primary; Rapid = rapid; Bab = bab; Haste = haste; Preparation = preparation; }
            internal bool ExpectedFull => !Primary && (Preparation == "fresh" || Preparation == "carried-move" || Preparation == "mixed-range");
        }
        private static readonly OrdinaryCase[] OrdinaryCases = {
            new OrdinaryCase("C01-B", false), new OrdinaryCase("C01-C", true), new OrdinaryCase("C01-D", true, true),
            new OrdinaryCase("C03-rapid-off-B", false, rapid: false), new OrdinaryCase("C03-rapid-off-C", true, rapid: false),
            new OrdinaryCase("C03-bab-B", false, rapid: false, bab: 6), new OrdinaryCase("C03-bab-C", true, rapid: false, bab: 6),
            new OrdinaryCase("C03-haste-B", false, rapid: false, bab: 6, haste: true), new OrdinaryCase("C03-haste-C", true, rapid: false, bab: 6, haste: true),
            new OrdinaryCase("C02-restricted-B", false, preparation: "stale-staggered"), new OrdinaryCase("C02-restricted-C", true, preparation: "stale-staggered"),
            new OrdinaryCase("C03-single-B", false, preparation: "native-single"), new OrdinaryCase("C03-single-C", true, preparation: "native-single"),
            new OrdinaryCase("C03-spent-standard-B", false, preparation: "spent-standard"), new OrdinaryCase("C03-spent-standard-C", true, preparation: "spent-standard"),
            new OrdinaryCase("C03-rider-move-B", false, preparation: "rider-move"), new OrdinaryCase("C03-carried-move-C", true, preparation: "carried-move"),
            new OrdinaryCase("C03-mixed-range-B", false, preparation: "mixed-range"), new OrdinaryCase("C03-mixed-range-C", true, preparation: "mixed-range")
        };
        private static readonly string[] OrdinaryCaseIds = OrdinaryCases.Select(item => item.Id).ToArray();
        internal static double OrdinaryScenarioDeadlineSeconds => OrdinaryCaseIds.Length * LeafDeadlineSeconds + 60.0d;
        private OrdinaryCase OrdinaryCurrent => OrdinaryCases[ordinaryCase];
        private int ordinaryCase;
        private int ordinaryStage;
        private bool ordinaryControlSent;
        private JObject ordinaryBefore;
        private JObject ordinaryPrediction;
        private UnitAttack ordinaryMeasured;
        private TurnController ordinaryTurn;
        private int ordinaryRepeats;
        private bool ordinaryContinuity;
        private long ordinaryIntentBefore;
        private NativeAttackFixtureVariation ordinaryVariation;
        private JObject ordinaryVariationAtDispatch;
        private JObject ordinarySpent;
        private double ordinarySpentAt;
        private JObject ordinaryMovement;
        private bool ordinaryMovementDone;
        private UnitMoveTo ordinaryMove;
        private bool ordinarySetupComplete;
        private float ordinarySetupRadius;
        private bool OrdinaryMounted => OrdinaryCurrent.Mounted;
        private bool OrdinaryPrimary => OrdinaryCurrent.Primary;

        private void BeginOrdinaryAttackControls()
        {
            if (settings.EnableUnifiedMountedTurn || settings.EnablePairedCommandScheduler ||
                settings.EnableDiagnosticOverlay || playerAction.OverlayPresent)
                throw new InvalidOperationException("Ordinary attack controls require exact separate-turn C0 settings.");
            rangedWeaponLease = new Phase3dRangedWeaponLease(rider);
            rangedWeaponLease.Acquire(WeaponCategory.Longbow);
            AcquirePhase3hRapidShot();
            ordinaryAttackTrace = new NativeOrdinaryAttackTrace(rider, horse, combat);
            step = Phase3dHorseStep.Phase3gControls;
            BeginOrdinaryCase();
        }

        private void BeginOrdinaryCase()
        {
            if (ordinaryCase >= OrdinaryCaseIds.Length) { BeginCleanup(); return; }
            ordinaryAttackTrace.BeginCase(OrdinaryCaseIds[ordinaryCase]);
            ordinaryStage = 0;
            ordinaryControlSent = false;
            ordinaryBefore = null;
            ordinaryMeasured = null;
            ordinaryPrediction = null;
            ordinaryTurn = null;
            ordinaryRepeats = 0;
            ordinaryContinuity = true;
            ordinaryVariationAtDispatch = null;
            ordinarySpent = null;
            ordinaryMovement = null;
            ordinaryMovementDone = false;
            ordinaryMove = null;
            ordinarySetupComplete = OrdinaryCurrent.Preparation == "mixed-range";
            ResetLeafClock();
        }

        private void TickOrdinaryAttackControls()
        {
            var game = Game.Instance;
            observations["ordinaryAttackProgress"] = new JObject {
                ["caseId"] = ordinaryCase < OrdinaryCaseIds.Length ? OrdinaryCaseIds[ordinaryCase] : "cleanup",
                ["stage"] = ordinaryStage, ["state"] = relationship.State.ToString(),
                ["turnActor"] = game.TurnBasedCombatController.CurrentTurn?.Unit?.UniqueId,
                ["before"] = ordinaryBefore, ["prediction"] = ordinaryPrediction
            };
            if (game.IsPaused) { game.IsPaused = false; return; }
            if (ordinaryStage == 0)
            {
                if (!rider.Commands.Empty || !horse.Commands.Empty || rider.AreHandsBusyWithAnimation) return;
                SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                if ((relationship.State == RelationshipState.Mounted) != OrdinaryMounted)
                {
                    if (!ordinaryControlSent)
                    {
                        ordinaryControlSent = TryNativeAbilityTargetClick(OrdinaryMounted ? nativeControls.MountAbility :
                            nativeControls.DismountAbility, OrdinaryMounted ? horse : rider, "ordinary-fixture-pair-control");
                        if (!ordinaryControlSent) throw new InvalidOperationException("Native fixture Mount/Dismount input refused.");
                    }
                    return;
                }
                if (!OrdinaryMounted && (!PrepareUnmountedHorseAiIsolation() || !PrepareCombatMountRiderAiIsolation())) return;
                ordinaryVariation = new NativeAttackFixtureVariation(rider, phase3hRapidToggle,
                    OrdinaryCurrent.Rapid, OrdinaryCurrent.Bab, OrdinaryCurrent.Haste);
                BeginTarget(OrdinaryCurrent.Preparation == "mixed-range" ? 6f : 3.5f, OrdinaryCaseIds[ordinaryCase]);
                var nativePlan = new UnitAttack(target);
                nativePlan.Init(rider);
                var origin = OrdinaryMounted ? horse : rider;
                // Read native weapon inventory for placement only. Do not let a
                // previous case's recovery cooldown choose the fixture distance;
                // the later measured command still plans on its own native start.
                ordinarySetupRadius = origin.View.Corpulence + target.View.Corpulence +
                    nativePlan.CreateFullAttack().Min(attack => attack.WeaponRange);
                ruleProbe.Arm(target, false);
                ordinaryStage = 1;
                return;
            }
            if (ordinaryStage == 1)
            {
                if (!IsCombatReady(OrdinaryMounted)) return;
                if (!CombatController.IsInTurnBasedCombat())
                {
                    if (!ordinarySetupComplete)
                    {
                        var mover = OrdinaryMounted ? horse : rider;
                        var destination = FindOrdinaryControlPoint(mover.Position, 0.25f);
                        observations["setup-" + OrdinaryCurrent.Id] = new JObject {
                            ["radius"] = ordinarySetupRadius, ["before"] = CaptureOrdinaryLiveState(),
                            ["targetPoint"] = new JArray(target.Position.x, target.Position.y, target.Position.z),
                            ["destination"] = new JArray(destination.x, destination.y, destination.z) };
                        SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                        ClickGroundHandler.MoveSelectedUnitsToPoint(destination, false);
                        ordinaryMove = mover.Commands.Move as UnitMoveTo;
                        if (ordinaryMove?.Executor != mover) throw new InvalidOperationException("Native fixture positioning did not acquire the exact mover.");
                        ordinaryStage = 7; ResetLeafClock(); return;
                    }
                    if (turnBasedModeProbe == null) turnBasedModeProbe = new NativeModeTransitionProbe(true);
                    if (!turnBasedModeProbe.TemporaryDeliveryAttempted) turnBasedModeProbe.DispatchTemporaryValueIfRequired();
                    return;
                }
                var turn = game.TurnBasedCombatController.CurrentTurn;
                if (turn?.Unit != rider || turn.Status != TurnController.TurnStatus.Preparing && !turn.IsActing)
                { TryEndPhase3gFixtureTurn(turn); return; }
                if (!rider.Commands.Empty || !horse.Commands.Empty || rider.AreHandsBusyWithAnimation ||
                    !rider.HasStandardAction() || game.HandsEquipmentController.IsUpdateScheduledFor(rider)) return;
                if (rider.IsMoveActionRestricted()) { TryEndPhase3gFixtureTurn(turn); return; }
                SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                ordinaryTurn = turn;
                if (!ordinaryMovementDone && (OrdinaryCurrent.Preparation == "rider-move" || OrdinaryCurrent.Preparation == "carried-move"))
                {
                    var mover = OrdinaryMounted ? horse : rider;
                    var direction = (mover.Position - target.Position).normalized;
                    var destination = FindOrdinaryControlPoint(target.Position - direction * ordinarySetupRadius, 2.1f);
                    ordinaryMovement = new JObject { ["before"] = CaptureOrdinaryLiveState(),
                        ["requestedPoint"] = new JArray(destination.x, destination.y, destination.z), ["mover"] = mover.UniqueId };
                    using (var input = new NativeOrdinaryAttackInput(destination))
                    {
                        input.Predict();
                        var cycles = 0;
                        while (turn.EnabledFiveFootStep && cycles++ < 4) { input.Click(button: 1); input.Predict(); }
                        ordinaryMovement["nativeCursorCycles"] = cycles;
                        if (!input.Click()) return;
                    }
                    ordinaryMove = mover.Commands.Move as UnitMoveTo;
                    if (ordinaryMove?.Executor != mover) throw new InvalidOperationException("Native ground input did not acquire the exact movement owner.");
                    ordinaryStage = 6; ResetLeafClock(); return;
                }
                ordinaryBefore = CaptureOrdinaryLiveState();
                ordinaryIntentBefore = combat.StockAttackIntentStartCount;
                using (var input = new NativeOrdinaryAttackInput(target))
                {
                    var beforePrediction = CaptureOrdinaryLiveState();
                    for (var index = 0; index < 3; index++) input.Predict();
                    var afterPrediction = CaptureOrdinaryLiveState();
                    ordinaryPrediction = new JObject {
                        ["count"] = 3, ["pure"] = JToken.DeepEquals(beforePrediction, afterPrediction),
                        ["before"] = beforePrediction, ["after"] = afterPrediction,
                        ["fullEnabled"] = turn.EnabledFullAttack, ["nativeEstimate"] = UnitAttack.EstimateFullAttacks(rider)
                    };
                    if (OrdinaryCurrent.Preparation == "native-single" || OrdinaryCurrent.Preparation == "spent-standard")
                    {
                        var cycles = 0;
                        while (turn.EnabledFullAttack && cycles++ < 8) { input.Click(button: 1); input.Predict(); }
                        if (turn.EnabledFullAttack || cycles == 0)
                            throw new InvalidOperationException("Native right-click did not choose a genuine Single action.");
                        ordinaryPrediction["nativeCursorCycles"] = cycles;
                        ordinaryPrediction["fullAfterChoice"] = turn.EnabledFullAttack;
                    }
                    if (OrdinaryCurrent.Preparation == "stale-staggered") ordinaryVariation.ApplyStaleRestriction();
                    ordinaryVariationAtDispatch = ordinaryVariation.Capture();
                    if (!targetService.BeginExpectedAttackDispatch(target))
                        throw new InvalidOperationException("Exact ordinary control target admission failed.");
                    var clicked = OrdinaryPrimary
                        ? TryNativeAbilityTargetClick(nativeControls.RiderPrimaryAbility, target, "ordinary-primary-input")
                        : input.Click();
                    if (!clicked && OrdinaryCurrent.Preparation == "stale-staggered")
                    {
                        ordinaryPrediction["staleClickRefused"] = true;
                        ordinaryStage = 4;
                        return;
                    }
                    if (!clicked) { CompleteOrdinaryCase(false, "Native input admission refused."); return; }
                }
                ordinaryStage = 2;
                ResetLeafClock();
                return;
            }
            if (ordinaryStage == 4)
            {
                // A changed native condition can invalidate cached prediction. Let
                // the normal prediction/admission callbacks reject and refresh it.
                using (var input = new NativeOrdinaryAttackInput(target))
                {
                    input.Predict();
                    if (!input.Click()) return;
                }
                ordinaryStage = 2; ResetLeafClock(); return;
            }
            if (ordinaryStage == 2)
            {
                if (ordinaryMeasured == null) ordinaryMeasured = ordinaryAttackTrace.LastStartedRiderAttack;
                if (ordinaryMeasured == null) return;
                if (OrdinaryMounted && !OrdinaryPrimary && !ordinaryMeasured.IsFinished && ordinaryRepeats == 0)
                {
                    var before = CaptureOrdinaryLiveState();
                    var intent = combat.StockAttackIntentStartCount;
                    using (var input = new NativeOrdinaryAttackInput(target))
                    {
                        for (var index = 0; index < 3; index++)
                        {
                            input.Predict();
                            // The native pointer admission gate may consume a repeated
                            // busy click; the observed handler additionally exercises
                            // the real mounted same-target routing service.
                            input.Click();
                            input.Click(false);
                            ordinaryRepeats++;
                        }
                    }
                    ordinaryContinuity = JToken.DeepEquals(before, CaptureOrdinaryLiveState()) &&
                        combat.StockAttackIntentStartCount == intent && rider.Commands.Contains(ordinaryMeasured);
                }
                if (!ordinaryMeasured.IsFinished || rider.AreHandsBusyWithAnimation ||
                    ruleProbe.RiderResolvedCount < ordinaryMeasured.GetAttackIndex()) return;
                var nativePlan = ordinaryMeasured.AllAttacks.Count;
                var completed = ordinaryMeasured.GetAttackIndex();
                var recovery = !OrdinaryMounted && ordinaryAttackTrace.NativeRecoveryInterrupt(ordinaryMeasured) != null;
                var rangeRejection = ordinaryAttackTrace.NativeRangeRejection(ordinaryMeasured);
                var rangeStop = OrdinaryCurrent.Preparation == "mixed-range" && ordinaryMeasured.Result == UnitCommand.ResultType.Interrupt &&
                    completed > 0 && completed < nativePlan && ordinaryMeasured.PlannedAttack?.Weapon.Blueprint.IsNatural == true &&
                    rangeRejection != null && (float)rangeRejection["rangeOriginDistance"] >
                        (float)(OrdinaryMounted ? rangeRejection["pairApproachRadius"] : rangeRejection["approachRadius"]);
                var completion = rangeStop || completed == nativePlan &&
                    (ordinaryMeasured.Result == UnitCommand.ResultType.Success || recovery);
                var success = completion && ordinaryMeasured.Executor == rider &&
                    ruleProbe.RiderResolvedCount == completed && ruleProbe.MountNonOpportunityAttackRuleCount == 0 &&
                    ruleProbe.PairForcedD20Count == 0 && ReferenceEquals(game.TurnBasedCombatController.CurrentTurn, ordinaryTurn) &&
                    (bool)ordinaryPrediction["pure"] && ordinaryContinuity &&
                    (OrdinaryCurrent.ExpectedFull ? ordinaryMeasured.IsFullAttack && nativePlan >= 2 :
                        nativePlan == 1 && !ordinaryMeasured.IsFullAttack && ordinaryMeasured.IsSingleAttack == OrdinaryPrimary) &&
                    Math.Abs(rider.CombatState.Cooldown.StandardAction - 6f) < 0.001f &&
                    Math.Abs(rider.CombatState.Cooldown.MoveAction - (OrdinaryCurrent.ExpectedFull || OrdinaryCurrent.Preparation == "rider-move" ? 3f : 0f)) < 0.001f &&
                    Math.Abs(horse.CombatState.Cooldown.StandardAction - (float)ordinaryBefore["mount"]["standard"]) < 0.001f &&
                    Math.Abs(horse.CombatState.Cooldown.MoveAction - (float)ordinaryBefore["mount"]["move"]) < 0.001f;
                if (success && OrdinaryCurrent.Preparation == "spent-standard")
                {
                    ordinarySpent = new JObject { ["before"] = CaptureOrdinaryLiveState(),
                        ["rulesBefore"] = ruleProbe.CapturePairEvidence() };
                    using (var input = new NativeOrdinaryAttackInput(target))
                    {
                        input.Predict();
                        ordinarySpent["clickAccepted"] = input.Click();
                    }
                    ordinarySpentAt = clock.Elapsed.TotalSeconds;
                    ordinaryStage = 5;
                    return;
                }
                CompleteOrdinaryCase(success, "Native stationary plan, effects, costs and prediction continuity.");
                return;
            }
            if (ordinaryStage == 5)
            {
                if (clock.Elapsed.TotalSeconds - ordinarySpentAt < 1.0d) return;
                ordinarySpent["after"] = CaptureOrdinaryLiveState();
                ordinarySpent["rulesAfter"] = ruleProbe.CapturePairEvidence();
                ordinarySpent["sameNativeTurn"] = ReferenceEquals(game.TurnBasedCombatController.CurrentTurn, ordinaryTurn);
                ordinarySpent["secondAttackStarted"] = !ReferenceEquals(ordinaryAttackTrace.LastStartedRiderAttack, ordinaryMeasured);
                var success = !rider.HasStandardAction() && (bool)ordinarySpent["sameNativeTurn"] &&
                    !(bool)ordinarySpent["secondAttackStarted"] && ruleProbe.RiderResolvedCount == 1 &&
                    Math.Abs(rider.CombatState.Cooldown.StandardAction - 6f) < 0.001f &&
                    Math.Abs(rider.CombatState.Cooldown.MoveAction) < 0.001f;
                CompleteOrdinaryCase(success, "Native Standard expenditure prevents another attack in the same activation.");
                return;
            }
            if (ordinaryStage == 6)
            {
                var mover = OrdinaryMounted ? horse : rider;
                if (!ordinaryMove.IsFinished || mover.View.AgentASP.IsReallyMoving) return;
                ordinaryMovement["after"] = CaptureOrdinaryLiveState();
                ordinaryMovement["command"] = CaptureOrdinaryCommand(ordinaryMove);
                var before = ordinaryMovement["before"];
                var initial = before[OrdinaryMounted ? "mount" : "rider"]["position"];
                var displacement = HorizontalDistance(new Vector3((float)initial[0], (float)initial[1], (float)initial[2]), mover.Position);
                ordinaryMovement["displacement"] = displacement;
                ordinaryMovement["sameNativeTurn"] = ReferenceEquals(game.TurnBasedCombatController.CurrentTurn, ordinaryTurn);
                var success = ordinaryMove.Result == UnitCommand.ResultType.Success && displacement > 2f &&
                    (bool)ordinaryMovement["sameNativeTurn"] && mover.CombatState.Cooldown.MoveAction > 0f &&
                    Math.Abs(rider.CombatState.Cooldown.StandardAction - (float)before["rider"]["standard"]) < 0.001f &&
                    (!OrdinaryMounted || Math.Abs(rider.CombatState.Cooldown.MoveAction - (float)before["rider"]["move"]) < 0.001f) &&
                    ruleProbe.PairNonOpportunityAttackRuleCount == 0;
                ordinaryMovement["passed"] = success;
                if (!success) { CompleteOrdinaryCase(false, "Native movement allocation did not preserve its expected actor owner."); return; }
                ordinaryMovementDone = true; ordinaryStage = 1; ResetLeafClock(); return;
            }
            if (ordinaryStage == 7)
            {
                var mover = OrdinaryMounted ? horse : rider;
                if (!ordinaryMove.IsFinished || mover.View.AgentASP.IsReallyMoving) return;
                var setup = observations["setup-" + OrdinaryCurrent.Id];
                setup["after"] = CaptureOrdinaryLiveState();
                setup["command"] = CaptureOrdinaryCommand(ordinaryMove);
                setup["distance"] = mover.DistanceTo(target);
                if (ordinaryMove.Result != UnitCommand.ResultType.Success || mover.DistanceTo(target) > ordinarySetupRadius)
                    throw new InvalidOperationException("Native setup movement did not reach legal mixed-weapon adjacency.");
                ordinarySetupComplete = true; ordinaryMove = null; ordinaryStage = 1; ResetLeafClock(); return;
            }
            if (ordinaryStage == 3)
            {
                if (combat.HasActiveCommand || ruleProbe.RiderResolvedCount < ruleProbe.RiderNonOpportunityAttackRuleCount) return;
                TryLeaveCombat(target); TryLeaveCombat(rider); TryLeaveCombat(horse);
                if (targetService != null)
                {
                    if (!targetService.DestroyAndVerify()) return;
                    targetService.Dispose(); targetService = null; target = null;
                }
                if (turnBasedModeProbe != null) { turnBasedModeProbe.Dispose(); turnBasedModeProbe = null; }
                if (CombatController.IsInTurnBasedCombat() || game.TurnBasedCombatController.Initialized ||
                    game.Player.IsInCombat || !rider.Commands.Empty || !horse.Commands.Empty) return;
                ordinaryVariation.Dispose(); ordinaryVariation = null;
                if (!OrdinaryMounted && (!RestoreCombatMountRiderAiIsolation() || !RestoreUnmountedHorseAiIsolation()))
                    throw new InvalidOperationException("Unmounted control AI isolation did not restore exactly.");
                if (!OrdinaryMounted)
                {
                    observations["ai-" + OrdinaryCaseIds[ordinaryCase]] = new JObject {
                        ["rider"] = CaptureCombatMountRiderAiIsolation(), ["mount"] = CaptureUnmountedHorseAiIsolation()
                    };
                    combatMountRiderAiLease = null;
                    unmountedHorseAiLease = null;
                    unmountedHorseAiSettleRequested = false;
                }
                ordinaryCase++;
                BeginOrdinaryCase();
            }
        }

        private JObject CaptureOrdinaryLiveState()
        {
            return new JObject { ["rider"] = CaptureOrdinaryActor(rider), ["mount"] = CaptureOrdinaryActor(horse),
                ["relationship"] = relationship.State.ToString(), ["intentStarts"] = combat.StockAttackIntentStartCount,
                ["duplicateDispatches"] = combat.StockAttackDuplicateDispatchCount };
        }

        private Vector3 FindOrdinaryControlPoint(Vector3 preferredOrigin, float minimumDisplacement)
        {
            var mover = OrdinaryMounted ? horse : rider;
            var direction = preferredOrigin - target.Position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f) direction = Vector3.forward;
            direction.Normalize();
            // The native ground command admits an endpoint within 0.3 m. Leave
            // 0.4 m inside weapon reach and reject occupied endpoints before input.
            var radius = ordinarySetupRadius - 0.4f;
            for (var index = 0; index < 24; index++)
            {
                var angle = index == 0 ? 0f : (index % 2 == 0 ? index : -index) * 15f;
                var nearest = global::AstarPath.active.GetNearest(target.Position +
                    Quaternion.Euler(0f, angle, 0f) * direction * radius);
                var point = nearest.clampedPosition;
                if (nearest.node == null || !nearest.node.Walkable ||
                    HorizontalDistance(point, target.Position) > radius + 0.05f ||
                    HorizontalDistance(point, mover.Position) < minimumDisplacement) continue;
                var occupied = Game.Instance.State.Units.Any(unit => unit != mover && unit.IsInState && unit.View != null &&
                    !(OrdinaryMounted && unit == rider) && HorizontalDistance(point, unit.Position) <
                        mover.View.Corpulence + unit.View.Corpulence + 0.05f);
                if (!occupied) return point;
            }
            throw new InvalidOperationException("No clear native fixture endpoint exists inside the actual weapon range.");
        }

        private static JObject CaptureOrdinaryActor(Kingmaker.EntitySystem.Entities.UnitEntityData actor)
        {
            return new JObject {
                ["id"] = actor.UniqueId, ["standard"] = actor.CombatState.Cooldown.StandardAction,
                ["move"] = actor.CombatState.Cooldown.MoveAction, ["swift"] = actor.CombatState.Cooldown.SwiftAction,
                ["position"] = new JArray(actor.Position.x, actor.Position.y, actor.Position.z),
                ["commands"] = RuntimeHelpers.GetHashCode(actor.Commands),
                ["raw"] = new JArray(actor.Commands.Raw.Select(CaptureOrdinaryCommand)),
                ["queue"] = new JArray(actor.Commands.Queue.Select(CaptureOrdinaryCommand)),
                ["previous"] = CaptureOrdinaryCommand(actor.Commands.PreviousCommand),
                ["group"] = actor.Commands.GroupCommand == null ? 0 : RuntimeHelpers.GetHashCode(actor.Commands.GroupCommand)
            };
        }

        private static JToken CaptureOrdinaryCommand(UnitCommand command)
        {
            return command == null ? JValue.CreateNull() : (JToken)new JObject {
                ["id"] = RuntimeHelpers.GetHashCode(command), ["type"] = command.GetType().FullName,
                ["executor"] = command.Executor?.UniqueId,
                ["started"] = command.IsStarted, ["acted"] = command.IsActed,
                ["finished"] = command.IsFinished, ["result"] = command.Result.ToString()
            };
        }

        private void CompleteOrdinaryCase(bool success, string detail)
        {
            AddRow(OrdinaryCaseIds[ordinaryCase], success, detail, new JObject {
                ["level"] = "NATIVE INTEGRATION", ["parameters"] = new JObject { ["mode"] = "TB", ["mounted"] = OrdinaryMounted,
                    ["primary"] = OrdinaryPrimary, ["weapon"] = "Longbow", ["rapidShot"] = phase3hRapidToggle.IsOn,
                    ["bab"] = OrdinaryCurrent.Bab, ["haste"] = OrdinaryCurrent.Haste, ["preparation"] = OrdinaryCurrent.Preparation },
                ["variation"] = ordinaryVariationAtDispatch, ["spentStandard"] = ordinarySpent,
                ["movement"] = ordinaryMovement,
                ["before"] = ordinaryBefore, ["after"] = CaptureOrdinaryLiveState(), ["prediction"] = ordinaryPrediction,
                ["repeatedRequests"] = ordinaryRepeats, ["continuity"] = ordinaryContinuity,
                ["intentStarts"] = combat.StockAttackIntentStartCount - ordinaryIntentBefore,
                ["nativeCommand"] = CaptureOrdinaryCommand(ordinaryMeasured),
                ["nativeFull"] = ordinaryMeasured?.IsFullAttack, ["nativeSingle"] = ordinaryMeasured?.IsSingleAttack,
                ["nativeRecovery"] = ordinaryAttackTrace.NativeRecoveryInterrupt(ordinaryMeasured),
                ["nativeRangeRejection"] = ordinaryAttackTrace.NativeRangeRejection(ordinaryMeasured),
                ["target"] = new JObject { ["id"] = target?.UniqueId, ["dead"] = target?.Descriptor.State.IsDead,
                    ["unconscious"] = target?.Descriptor.State.IsUnconscious, ["damage"] = target?.Damage,
                    ["hp"] = target?.Stats.HitPoints.ModifiedValue, ["temporaryHp"] = target?.Stats.TemporaryHitPoints.ModifiedValue },
                ["nativePlan"] = ordinaryMeasured == null ? null : new JArray(ordinaryMeasured.AllAttacks.Select(item => new JObject {
                    ["weapon"] = item.Weapon?.Blueprint.AssetGuid, ["penalty"] = item.AttackBonusPenalty,
                    ["range"] = item.WeaponRange, ["ranged"] = item.Weapon?.Blueprint.IsRanged, ["natural"] = item.Weapon?.Blueprint.IsNatural })),
                ["planned"] = ordinaryMeasured?.AllAttacks.Count, ["completed"] = ordinaryMeasured?.GetAttackIndex(),
                ["rules"] = ruleProbe.CapturePairEvidence()
            });
            SelectionManager.Instance.Stop();
            ordinaryStage = 3;
            ResetLeafClock();
        }
    }
}
