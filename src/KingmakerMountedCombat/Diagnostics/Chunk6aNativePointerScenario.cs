using System;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UI.Selection;
using KingmakerMountedCombat.Domain;
using Newtonsoft.Json;
namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        private NativeMountPointerInput chunk6aPointerInput;
        private void BeginChunk6aNativePointer()
        {
            nativeControls.Update();
            var data = rider.Descriptor.Abilities.GetAbility(nativeControls.MountAbility)?.Data;
            chunk6aPointerInput = new NativeMountPointerInput(rider, horse, data);
            observations["chunk6aNativePointerInput"] = chunk6aPointerInput.Capture();
            chunk6aStage = 26;
        }
        private void TickChunk6aNativePointer()
        {
            var ready = chunk6aPointerInput.PollReady();
            observations["chunk6aNativePointerInput"] = chunk6aPointerInput.Capture();
            if (!ready) return;
            try
            {
                chunk6aApproachPath.CaptureBeforeClick();
                chunk6aMountClicked = chunk6aPointerInput.Click();
                var command = rider.Commands.GetCommand(UnitCommand.CommandType.Move) as UnitUseAbility;
                lastNativeAbilityShell = command?.Executor == rider && command.Target?.Unit == horse &&
                    command.Spell?.Blueprint == nativeControls.MountAbility ? command : null;
                if (lastNativeAbilityShell == null) chunk6aMountClicked = false;
                CompleteChunk6aPositiveClick();
            }
            finally { CleanupChunk6aNativePointer(); }
        }
        private void CleanupChunk6aNativePointer()
        {
            if (chunk6aPointerInput == null) return;
            try { chunk6aPointerInput.Dispose(); }
            finally { observations["chunk6aNativePointerInput"] = chunk6aPointerInput.Capture(); chunk6aPointerInput = null; }
        }
        // Both the unchanged RT input and the TB pointer gate close the same one-command window.
        private void CompleteChunk6aPositiveClick()
        {
            chunk6aCommandWindow.ClickCompleted(chunk6aMountClicked);
            if (chunk6aMountClicked && chunk6aApproachPath != null) chunk6aApproachPath.Bind(lastNativeAbilityShell);
            if (!chunk6aMountClicked)
            {
                // Name the exact obstacle. A refusal here reports whichever condition the
                // availability and target contracts actually rejected, plus the live
                // selection and measured geometry, instead of a generic message that
                // leaves the next reader to guess.
                var refusedAvailability = nativeControls.Evaluate(
                    NativeMountedControlKind.MountCompanion, rider);
                var refusedSelection = SelectionManager.Instance?.SelectedUnits;
                FailCurrent("CM01-combat-mount-accepted",
                    "Exact native combat Mount target click was not admitted. " +
                    "availabilityEnabled=" + refusedAvailability.IsEnabled +
                    "; transitionReady=" + refusedAvailability.IsTransitionReady +
                    "; availabilityReason=\"" + refusedAvailability.Reason + "\"" +
                    "; targetRejection=\"" +
                    playerAction.DescribeNativeMountTargetRejection(rider, horse) + "\"" +
                    "; canTarget=" + nativeControls.CanTarget(
                        NativeMountedControlKind.MountCompanion, rider, horse) +
                    "; selectedCount=" + (refusedSelection?.Count ?? -1) +
                    "; selectedIsRider=" + (refusedSelection != null && refusedSelection.Count == 1 &&
                        refusedSelection[0] == rider) +
                    "; geometry=" + CaptureChunk6aGeometry("mount-click-refused").ToString(Formatting.None));
                BeginCleanup();
                return;
            }
            BeginChunk6aPausedHold();
            chunk6aStage = 2;
            ResetLeafClock();
            return;
        }
    }
}
