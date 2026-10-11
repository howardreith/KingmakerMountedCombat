using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker;
using Kingmaker.UnitLogic.Commands.Base;
using KingmakerMountedCombat.Domain;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        private JObject chunk6aGeometryChangeEvidence;

        private void TickChunk6aGeometryChange()
        {
            if (chunk6aStage == 16)
            {
                if (!Chunk6aIdle || !nativeControls.Evaluate(NativeMountedControlKind.MountCompanion, rider).IsEnabled) return;
                if (!EnsureChunk6aRiderSelection("CM02-geometry-change")) return;
                chunk6aGeometryChangeBefore = CaptureChunk6aState("geometry-change-before");
                chunk6aGeometryChangeStart = CaptureChunk6aGeometry("geometry-change-pre-click");
                if ((bool)chunk6aGeometryChangeStart["isAdjacent"])
                {
                    FailCurrent("CM02-geometry-change", "The geometry-change command requires its own non-adjacent pre-click baseline.");
                    BeginCleanup(); return;
                }
                chunk6aGeometryChangeLedgerBefore = Chunk6aLedgerCounters();
                chunk6aGeometryChangeEvidence = new JObject
                {
                    ["contract"] = "native-target-motion-during-exact-mount-approach",
                    ["start"] = chunk6aGeometryChangeStart, ["before"] = chunk6aGeometryChangeBefore
                };
                observations["chunk6aGeometryChange"] = chunk6aGeometryChangeEvidence;
                BeginChunk6aCommandWindow(nativeControls.MountAbility.AssetGuid);
                var clicked = TryNativeAbilityTargetClick(nativeControls.MountAbility, horse, "chunk6a-geometry-change-click");
                chunk6aCommandWindow.ClickCompleted(clicked);
                if (!clicked)
                {
                    FailCurrent("CM02-geometry-change", "The selected exact rider's native geometry-change Mount click was refused: " + playerAction.LastFeedback);
                    BeginCleanup(); return;
                }
                chunk6aStage = 17; ResetLeafClock(); return;
            }
            var command = chunk6aCommandWindow?.Command;
            if (!chunk6aGeometryChanged)
            {
                if (command == null || command.IsActed || command.IsFinished)
                {
                    FailCurrent("CM02-geometry-change", "The exact Mount left pre-acted approach before the target ground order was issued.");
                    BeginCleanup(); return;
                }
                var riderDisplacement = Chunk6aPlanarDistance((JObject)chunk6aGeometryChangeStart["riderPosition"], CapturePosition(rider.Position));
                if (!chunk6aCommandWindow.ApproachObserved || !rider.View.AgentASP.IsReallyMoving || riderDisplacement <= 0.25f) return;
                chunk6aGeometryChangeAtChange = CaptureChunk6aGeometry("geometry-change-trigger");
                // Commands.Move returns UnitMoveTo only; Mount is a UnitUseAbility in the Move slot.
                var moveSlot = rider.Commands.GetCommand(UnitCommand.CommandType.Move);
                chunk6aGeometryChangeEvidence["trigger"] = new JObject
                {
                    ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks,
                    ["approachObserved"] = chunk6aCommandWindow.ApproachObserved,
                    ["riderReallyMoving"] = rider.View.AgentASP.IsReallyMoving,
                    ["command"] = CaptureOrdinaryCommand(command),
                    ["moveSlot"] = CaptureOrdinaryCommand(moveSlot),
                    ["geometry"] = chunk6aGeometryChangeAtChange, ["riderDisplacement"] = riderDisplacement
                };
                if ((bool)chunk6aGeometryChangeAtChange["isAdjacent"] || !ReferenceEquals(moveSlot, command))
                {
                    FailCurrent("CM02-geometry-change", "The ground order missed the exact uncommitted non-adjacent Mount approach.");
                    BeginCleanup(); return;
                }
                string refusal;
                chunk6aGeometryChangeCommand = Chunk6aSendHorseAway(3f, out chunk6aGeometryChangeDestination, out refusal);
                if (chunk6aGeometryChangeCommand == null || !EnsureChunk6aRiderSelection("CM02-geometry-change"))
                {
                    FailCurrent("CM02-geometry-change", "The native Horse ground order was refused: " + refusal);
                    BeginCleanup(); return;
                }
                chunk6aCommandWindow.DeclareGeometryGroundOrder(chunk6aGeometryChangeCommand);
                chunk6aGeometryChangeEvidence["auxiliaryCommandId"] = RuntimeHelpers.GetHashCode(chunk6aGeometryChangeCommand);
                chunk6aGeometryChangeEvidence["auxiliaryAdmission"] = CaptureOrdinaryCommand(chunk6aGeometryChangeCommand);
                chunk6aGeometryChangeEvidence["auxiliaryDestination"] = CapturePosition(chunk6aGeometryChangeDestination);
                chunk6aGeometryChanged = true;
                return;
            }
            if (chunk6aCommandWindow?.Terminal != true || !Chunk6aIdle || !chunk6aGeometryChangeCommand.IsFinished) return;
            var proof = FinishChunk6aCommandWindow("geometry-change-mount", true, 0, true);
            chunk6aGeometryChangeEvidence["commandProof"] = proof;
            chunk6aGeometryChangeEvidence["auxiliaryTerminal"] = CaptureOrdinaryCommand(chunk6aGeometryChangeCommand);
            chunk6aGeometryChangeEvidence["auxiliaryMovement"] = NativeGroundMovementObservation.Capture(horse, chunk6aGeometryChangeCommand);
            chunk6aGeometryChangeEvidence["after"] = CaptureChunk6aState("geometry-change-after");
            chunk6aGeometryChangeEvidence["noTransitionInFlight"] = !playerAction.HasVoluntaryTransitionInFlight;
            chunk6aGeometryChangeEvidence["feedback"] = playerAction.LastFeedback;
            if (!(bool)proof["pass"])
            {
                AddRow("CM02-geometry-change", false, "The geometry-change command failed its exact native causal or resource proof.", chunk6aGeometryChangeEvidence);
                BeginCleanup(); return;
            }
            // This is the delivery PREFIX, before attachment can change either view.
            var arrival = (JObject)proof["samples"].OfType<JObject>().Single(sample =>
                (string)sample["boundary"] == "deliver")["state"]["geometry"];
            var terminal = proof["samples"].OfType<JObject>().Single(sample => (string)sample["boundary"] == "terminal")["state"];
            var horseDisplacement = Chunk6aPlanarDistance((JObject)chunk6aGeometryChangeStart["horsePosition"], (JObject)arrival["horsePosition"]);
            var delta = (JObject)proof["ledgerDelta"];
            var accepted = (string)terminal["relationshipState"] == "Mounted" && (bool)arrival["isAdjacent"] &&
                (long)terminal["generation"] == (long)proof["identity"]["generationAtInit"] + 1 &&
                (long)delta["acceptedMount"] == 1 && (long)delta["refusedVoluntary"] == 0;
            var refused = (string)terminal["relationshipState"] == "Unmounted" &&
                (long)terminal["generation"] == (long)proof["identity"]["generationAtInit"] &&
                (long)delta["acceptedMount"] == 0 && (long)delta["refusedVoluntary"] == 1;
            var oneRequest = (long)delta["admittedMount"] == 1 && (long)delta["admittedDismount"] == 0 &&
                (long)delta["acceptedDismount"] == 0 && (long)delta["forcedDetach"] == 0 && (long)delta["duplicateSuppressed"] == 0 && (long)delta["concurrentSuppressed"] == 0;
            var auxiliaryTerminal = chunk6aGeometryChangeCommand.Result == UnitCommand.ResultType.Success ||
                chunk6aGeometryChangeCommand.Result == UnitCommand.ResultType.Interrupt;
            chunk6aGeometryChangeEvidence["arrival"] = arrival;
            chunk6aGeometryChangeEvidence["horseDisplacement"] = horseDisplacement;
            chunk6aGeometryChangeEvidence["acceptedLawfully"] = accepted;
            chunk6aGeometryChangeEvidence["refusedWithCommittedCost"] = refused;
            AddRow("CM02-geometry-change", horseDisplacement > Chunk6aStationaryToleranceMeters && auxiliaryTerminal &&
                oneRequest && (accepted || refused) && !playerAction.HasVoluntaryTransitionInFlight,
                "One exact native Mount approached before a separately identified Horse ground order changed target geometry; pre-attachment delivery revalidated arrival, and its exact acted Move cost remained native-owned. The declared RT ground command wrote no resource at its observed cost callback.",
                chunk6aGeometryChangeEvidence);
            chunk6aStage = 99; BeginCleanup();
        }
    }
}
