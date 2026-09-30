using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        internal const string Chunk6aRepeatedRequestScenario = "chunk6a-repeated-mount-request";
        private bool Chunk6aRepeatedRequestOnly => request.Scenario == Chunk6aRepeatedRequestScenario;
        private JObject chunk6aRepeatedRequestEvidence;
        private bool chunk6aRepeatedRequestIssued;
        private long chunk6aRepeatedRequestShellsBefore;
        private long chunk6aRepeatedRequestBindingsBefore;
        private NativeMountedControlSnapshot chunk6aRepeatedRequestControlsBefore;
        private JObject chunk6aRepeatedRequestLedgerBefore;
        private long chunk6aRepeatedRequestGenerationBefore;

        private void TickChunk6aRepeatedRequest()
        {
            if (!Chunk6aRepeatedRequestOnly || Chunk6aTurnBased)
                throw new InvalidOperationException("Repeated Mount request requires its isolated RT transaction.");

            if (chunk6aStage == 45)
            {
                if (!Chunk6aIdle || !nativeControls.Evaluate(NativeMountedControlKind.MountCompanion, rider).IsEnabled)
                    return;
                if (!EnsureChunk6aRiderSelection("CM06-repeated-request"))
                    return;

                var controller = Game.Instance.TurnBasedCombatController;
                var order = controller == null ? null : controller.SortedUnits.ToList();
                observations["chunk6aAdoptionDisposition"] = new JObject
                {
                    ["disposition"] = chunk6aDisposition.ToString(),
                    ["refusal"] = chunk6aDispositionRefusal,
                    ["riderRosterIndex"] = order == null ? -1 : order.IndexOf(rider),
                    ["mountRosterIndex"] = order == null ? -1 : order.IndexOf(horse)
                };
                var start = CaptureChunk6aGeometry("repeated-request-pre-click");
                if ((bool)start["isAdjacent"])
                    throw new InvalidOperationException("Repeated-request Mount must start outside transition reach.");

                chunk6aRepeatedRequestShellsBefore = nativeControls.NativeRelationshipShellCount;
                chunk6aRepeatedRequestBindingsBefore = nativeControls.NativeRelationshipProcessBindingCount;
                chunk6aRepeatedRequestControlsBefore = nativeControls.CaptureSnapshot();
                chunk6aRepeatedRequestLedgerBefore = Chunk6aLedgerCounters();
                chunk6aRepeatedRequestGenerationBefore = relationship.MountedPairGeneration;
                chunk6aRepeatedRequestEvidence = new JObject
                {
                    ["contract"] = "one-owned-native-mount-when-request-repeated-during-approach",
                    ["start"] = start,
                    ["initial"] = new JObject
                    {
                        ["relationshipState"] = relationship.State.ToString(),
                        ["generation"] = chunk6aRepeatedRequestGenerationBefore,
                        ["shellCount"] = chunk6aRepeatedRequestShellsBefore,
                        ["processBindingCount"] = chunk6aRepeatedRequestBindingsBefore,
                        ["controls"] = JObject.FromObject(chunk6aRepeatedRequestControlsBefore),
                        ["ledger"] = chunk6aRepeatedRequestLedgerBefore.DeepClone()
                    }
                };
                observations["chunk6aRepeatedRequest"] = chunk6aRepeatedRequestEvidence;

                BeginChunk6aCommandWindow(nativeControls.MountAbility.AssetGuid);
                var clicked = TryNativeAbilityTargetClick(
                    nativeControls.MountAbility,
                    horse,
                    "chunk6a-repeated-request-first-click");
                chunk6aCommandWindow.ClickCompleted(clicked);
                chunk6aRepeatedRequestEvidence["firstInput"] =
                    observations["chunk6a-repeated-request-first-click"]?.DeepClone();
                if (!clicked)
                {
                    FailCurrent("CM06-repeated-request",
                        "The first exact rider native Mount click was refused: " + playerAction.LastFeedback);
                    BeginCleanup();
                    return;
                }

                chunk6aStage = 46;
                ResetLeafClock();
                return;
            }

            var command = chunk6aCommandWindow?.Command;
            if (!chunk6aRepeatedRequestIssued)
            {
                if (command == null || command.IsStarted || command.IsActed || command.IsFinished ||
                    command.ExecutionProcess != null)
                {
                    FailCurrent("CM06-repeated-request",
                        "The first exact Mount left pending approach before the repeated input boundary.");
                    BeginCleanup();
                    return;
                }

                var moved = Chunk6aPlanarDistance(
                    (JObject)chunk6aRepeatedRequestEvidence["start"]["riderPosition"],
                    CapturePosition(rider.Position));
                if (!chunk6aCommandWindow.ApproachObserved || !rider.View.AgentASP.IsReallyMoving || moved <= 0.25f)
                    return;

                var geometry = CaptureChunk6aGeometry("repeated-request-trigger");
                var slotBefore = rider.Commands.GetCommand(UnitCommand.CommandType.Move);
                var selectedBefore = SelectionManager.Instance.SelectedUnits;
                if ((bool)geometry["isAdjacent"] || !ReferenceEquals(slotBefore, command) ||
                    selectedBefore.Count != 1 || selectedBefore[0] != rider ||
                    !nativeControls.OwnsUnsettledRelationshipShell(command))
                {
                    FailCurrent("CM06-repeated-request",
                        "The repeated input missed the exact selected rider's owned non-adjacent pending Mount.");
                    BeginCleanup();
                    return;
                }

                var availabilityBeforeSelection = nativeControls.Evaluate(
                    NativeMountedControlKind.MountCompanion,
                    rider);
                var before = CaptureChunk6aCausalState();
                var controlsBefore = nativeControls.CaptureSnapshot();
                var shellsBefore = nativeControls.NativeRelationshipShellCount;
                var bindingsBefore = nativeControls.NativeRelationshipProcessBindingCount;
                var traceBefore = allocationTrace.EventCount;
                var trigger = new JObject
                {
                    ["frame"] = Time.frameCount,
                    ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks,
                    ["approachObserved"] = true,
                    ["riderReallyMoving"] = rider.View.AgentASP.IsReallyMoving,
                    ["riderDisplacement"] = moved,
                    ["geometry"] = geometry,
                    ["commandObject"] = RuntimeHelpers.GetHashCode(command),
                    ["moveSlotObject"] = RuntimeHelpers.GetHashCode(slotBefore),
                    ["started"] = command.IsStarted,
                    ["acted"] = command.IsActed,
                    ["finished"] = command.IsFinished,
                    ["processPresent"] = command.ExecutionProcess != null,
                    ["shellOwned"] = nativeControls.OwnsUnsettledRelationshipShell(command),
                    ["exactSingleRider"] = selectedBefore.Count == 1 && selectedBefore[0] == rider
                };

                chunk6aRepeatedRequestIssued = true;
                NativeMountedControlAvailability availabilityDuringSelection = null;
                var repeatedClicked = TryNativeAbilityTargetClick(
                    nativeControls.MountAbility,
                    horse,
                    "chunk6a-repeated-request-second-click",
                    () =>
                    {
                        availabilityDuringSelection = nativeControls.Evaluate(
                            NativeMountedControlKind.MountCompanion,
                            rider);
                        return new JObject
                        {
                            ["visible"] = availabilityDuringSelection.IsVisible,
                            ["enabled"] = availabilityDuringSelection.IsEnabled,
                            ["transitionReady"] = availabilityDuringSelection.IsTransitionReady,
                            ["reason"] = availabilityDuringSelection.Reason
                        };
                    });
                var after = CaptureChunk6aCausalState();
                var controlsAfter = nativeControls.CaptureSnapshot();
                var slotAfter = rider.Commands.GetCommand(UnitCommand.CommandType.Move);
                var selectedAfter = SelectionManager.Instance.SelectedUnits;
                var repeat = new JObject
                {
                    ["preSelectionAvailability"] = new JObject
                    {
                        ["visible"] = availabilityBeforeSelection.IsVisible,
                        ["enabled"] = availabilityBeforeSelection.IsEnabled,
                        ["transitionReady"] = availabilityBeforeSelection.IsTransitionReady,
                        ["reason"] = availabilityBeforeSelection.Reason
                    },
                    ["availability"] = new JObject
                    {
                        ["visible"] = availabilityDuringSelection?.IsVisible,
                        ["enabled"] = availabilityDuringSelection?.IsEnabled,
                        ["transitionReady"] = availabilityDuringSelection?.IsTransitionReady,
                        ["reason"] = availabilityDuringSelection?.Reason
                    },
                    ["clicked"] = repeatedClicked,
                    ["feedback"] = playerAction.LastFeedback,
                    ["input"] = observations["chunk6a-repeated-request-second-click"]?.DeepClone(),
                    ["before"] = before,
                    ["after"] = after,
                    ["controlsBefore"] = JObject.FromObject(controlsBefore),
                    ["controlsAfter"] = JObject.FromObject(controlsAfter),
                    ["shellCountBefore"] = shellsBefore,
                    ["shellCountAfter"] = nativeControls.NativeRelationshipShellCount,
                    ["processBindingCountBefore"] = bindingsBefore,
                    ["processBindingCountAfter"] = nativeControls.NativeRelationshipProcessBindingCount,
                    ["allocationEventCountBefore"] = traceBefore,
                    ["allocationEventCountAfter"] = allocationTrace.EventCount,
                    ["commandObjectBefore"] = RuntimeHelpers.GetHashCode(command),
                    ["moveSlotObjectBefore"] = RuntimeHelpers.GetHashCode(slotBefore),
                    ["moveSlotObjectAfter"] = slotAfter == null ? 0 : RuntimeHelpers.GetHashCode(slotAfter),
                    ["sameMoveSlot"] = ReferenceEquals(slotAfter, command),
                    ["firstCommandStillOwned"] = nativeControls.OwnsUnsettledRelationshipShell(command),
                    ["firstCommandStarted"] = command.IsStarted,
                    ["firstCommandActed"] = command.IsActed,
                    ["firstCommandFinished"] = command.IsFinished,
                    ["firstCommandProcessPresent"] = command.ExecutionProcess != null,
                    ["selectedCountAfter"] = selectedAfter.Count,
                    ["selectedIdsAfter"] = new JArray(selectedAfter.Select(unit => unit.UniqueId)),
                    ["exactSingleRiderAfter"] = selectedAfter.Count == 1 && selectedAfter[0] == rider,
                    ["probeAfter"] = chunk6aCommandWindow.Capture()
                };
                chunk6aRepeatedRequestEvidence["trigger"] = trigger;
                chunk6aRepeatedRequestEvidence["repeat"] = repeat;

                var rejectedBeforeSecondCommand = availabilityBeforeSelection.IsEnabled &&
                    availabilityDuringSelection != null && availabilityDuringSelection.IsVisible &&
                    !availabilityDuringSelection.IsEnabled && !repeatedClicked &&
                    ReferenceEquals(slotAfter, command) &&
                    nativeControls.OwnsUnsettledRelationshipShell(command) &&
                    nativeControls.NativeRelationshipShellCount == shellsBefore &&
                    nativeControls.NativeRelationshipProcessBindingCount == bindingsBefore &&
                    allocationTrace.EventCount == traceBefore &&
                    relationship.State == RelationshipState.Unmounted &&
                    relationship.MountedPairGeneration == chunk6aRepeatedRequestGenerationBefore &&
                    selectedAfter.Count == 1 && selectedAfter[0] == rider &&
                    !command.IsStarted && !command.IsActed && !command.IsFinished && command.ExecutionProcess == null;
                repeat["rejectedBeforeSecondCommand"] = rejectedBeforeSecondCommand;
                if (!rejectedBeforeSecondCommand)
                {
                    AddRow("CM06-repeated-request", false,
                        "The repeated native Mount input was not refused before creating, replacing, starting, acting or charging another relationship command.",
                        chunk6aRepeatedRequestEvidence);
                    chunk6aStage = 99;
                    BeginCleanup();
                    return;
                }
            }

            if (relationship.State != RelationshipState.Mounted || !Chunk6aIdle ||
                chunk6aCommandWindow?.Terminal != true)
                return;

            var expectedMountPrepare = chunk6aDisposition == MidEncounterAdoption.PreparePartnerThisRound ? 1 : 0;
            var proof = FinishChunk6aCommandWindow("positive-mount", true, expectedMountPrepare, true);
            var terminal = CaptureChunk6aCausalState();
            var controlsTerminal = nativeControls.CaptureSnapshot();
            chunk6aRepeatedRequestEvidence["commandProof"] = proof;
            chunk6aRepeatedRequestEvidence["terminal"] = terminal;
            chunk6aRepeatedRequestEvidence["terminalControls"] = JObject.FromObject(controlsTerminal);
            chunk6aRepeatedRequestEvidence["terminalShellCount"] = nativeControls.NativeRelationshipShellCount;
            chunk6aRepeatedRequestEvidence["terminalProcessBindingCount"] = nativeControls.NativeRelationshipProcessBindingCount;

            var oneRequest = nativeControls.NativeRelationshipShellCount == chunk6aRepeatedRequestShellsBefore + 1 &&
                nativeControls.NativeRelationshipProcessBindingCount == chunk6aRepeatedRequestBindingsBefore + 1 &&
                controlsTerminal.NativeCastRequestCount == chunk6aRepeatedRequestControlsBefore.NativeCastRequestCount + 1 &&
                controlsTerminal.DispatchAcceptedCount == chunk6aRepeatedRequestControlsBefore.DispatchAcceptedCount + 1;
            var oneTransition = relationship.MountedPairGeneration == chunk6aRepeatedRequestGenerationBefore + 1 &&
                Chunk6aLedgerDelta(chunk6aRepeatedRequestLedgerBefore, "admittedMount", 1) &&
                Chunk6aLedgerDelta(chunk6aRepeatedRequestLedgerBefore, "acceptedMount", 1) &&
                Chunk6aLedgerDelta(chunk6aRepeatedRequestLedgerBefore, "duplicateSuppressed", 0) &&
                Chunk6aLedgerDelta(chunk6aRepeatedRequestLedgerBefore, "concurrentSuppressed", 0) &&
                Chunk6aLedgerDelta(chunk6aRepeatedRequestLedgerBefore, "forcedDetach", 0);
            chunk6aRepeatedRequestEvidence["oneRequest"] = oneRequest;
            chunk6aRepeatedRequestEvidence["oneTransition"] = oneTransition;

            var pass = (bool)proof["pass"] &&
                (bool)chunk6aRepeatedRequestEvidence["repeat"]["rejectedBeforeSecondCommand"] &&
                oneRequest && oneTransition;
            AddRow("CM06-repeated-request", pass,
                pass
                    ? "A second normal native Mount input during the exact first command's measured approach was refused before another UnitUseAbility or shell existed; the original command alone approached, acted, paid once and transitioned once."
                    : "The repeated-request case did not retain one exact owned native Mount through one acted cost and one relationship transition.",
                chunk6aRepeatedRequestEvidence);
            chunk6aStage = 99;
            BeginCleanup();
        }

        private bool CaptureChunk6aRepeatedRequestDeadline()
        {
            if (!Chunk6aRepeatedRequestOnly || chunk6aStage < 45 || chunk6aStage > 46)
                return false;
            observations["chunk6aRepeatedRequestDeadline"] = new JObject
            {
                ["case"] = chunk6aRepeatedRequestEvidence?.DeepClone(),
                ["probe"] = chunk6aCommandWindow?.Capture()
            };
            FailCurrent("CM06-repeated-request",
                "The exact first approach, repeated input refusal or original command terminal did not settle at the unchanged 30-second deadline.");
            return true;
        }
    }
}