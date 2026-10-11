using System;
using Kingmaker.UnitLogic.Commands.Base;
using KingmakerMountedCombat.Domain;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        private bool Chunk6aObstructionActive => IsChunk6aCombatMount && Chunk6aNeedsDoor &&
            (chunk6aStage == 0 && chunk6aExplorationStage >= 4 || chunk6aStage >= 18 && chunk6aStage <= 21);
        private JObject chunk6aObstructionEvidence;
        private UnitCommand chunk6aBlockedCommand;
        private int chunk6aClosedDoorObservations;
        private void TickChunk6aObstruction()
        {
            if (!Chunk6aObstructionOnly) throw new InvalidOperationException("Obstruction requires its own fresh native transaction.");
            if (chunk6aStage == 18)
            {
                chunk6aStage = 19; ResetLeafClock(); return;
            }
            if (chunk6aStage == 19)
            {
                if (!Chunk6aDoorSettled(false) || !Chunk6aIdle || !nativeControls.Evaluate(NativeMountedControlKind.MountCompanion, rider).IsEnabled) return;
                if (!EnsureChunk6aRiderSelection("CM02-obstruction")) return;
                var geometry = CaptureChunk6aGeometry("obstruction-pre-click");
                if ((bool)geometry["isAdjacent"]) throw new InvalidOperationException("Closed-door Mount must start outside the transition envelope.");
                chunk6aObstructionEvidence = new JObject { ["contract"] = "closed-native-door-unacted-mount",
                    ["start"] = geometry, ["doorAtClick"] = CaptureChunk6aDoor(), ["diagnosticStopIssued"] = false };
                observations["chunk6aObstruction"] = chunk6aObstructionEvidence;
                chunk6aCommandWindow = new NativeRelationshipCommandProbe(nativeControls, allocationTrace, rider, horse,
                    nativeControls.MountAbility.AssetGuid, CaptureChunk6aCausalState, true);
                chunk6aPath = new NativeCommandPathProbe(rider);
                var clicked = TryNativeAbilityTargetClick(nativeControls.MountAbility, horse, "chunk6a-obstruction-click");
                chunk6aCommandWindow.ClickCompleted(clicked);
                if (!clicked) { FailCurrent("CM02-obstruction", "Exact rider native Mount target click was refused: " + playerAction.LastFeedback); BeginCleanup(); return; }
                chunk6aBlockedCommand = chunk6aCommandWindow.Command; chunk6aPath.Bind(chunk6aBlockedCommand);
                chunk6aStage = 20; ResetLeafClock(); return;
            }
            if (chunk6aStage == 20)
            {
                chunk6aObstructionEvidence["path"] = chunk6aPath.Capture();
                chunk6aObstructionEvidence["liveCommand"] = CaptureOrdinaryCommand(chunk6aBlockedCommand);
                var door = CaptureChunk6aDoor();
                chunk6aObstructionEvidence["doorLive"] = door;
                chunk6aObstructionEvidence["closedDoorObservations"] = ++chunk6aClosedDoorObservations;
                if ((bool)door["open"] || !(bool)door["cutEnabled"] || (double)door["clipTime"] > 0 ||
                    (bool)door["cutNeedsUpdate"] || (bool)door["graphUpdatesQueued"])
                { FinishChunk6aObstruction("The native door/cut ceased to be a settled obstruction during the exact command."); return; }
                if (chunk6aBlockedCommand.IsActed || relationship.State != RelationshipState.Unmounted)
                { FinishChunk6aObstruction("The closed-door Mount acted or transitioned before a native obstruction terminal."); return; }
                if (!chunk6aBlockedCommand.IsFinished || !Chunk6aIdle) return;
                FinishChunk6aObstruction(null); return;
            }
            if (chunk6aStage == 21)
            {
                if (!RestoreChunk6aDoor()) return;
                chunk6aStage = 99; BeginCleanup(); return;
            }
        }
        private void FinishChunk6aObstruction(string failure)
        {
            var proof = chunk6aCommandWindow.FinishUnacted();
            var path = chunk6aPath.Capture(); var observedFailure = chunk6aPath.FailureObserved;
            chunk6aCommandWindow.Dispose(); chunk6aCommandWindow = null; chunk6aPath.Dispose(); chunk6aPath = null;
            chunk6aObstructionEvidence["commandProof"] = proof; chunk6aObstructionEvidence["path"] = path;
            chunk6aObstructionEvidence["terminal"] = CaptureOrdinaryCommand(chunk6aBlockedCommand);
            chunk6aObstructionEvidence["doorAtTerminal"] = CaptureChunk6aDoor();
            chunk6aObstructionEvidence["after"] = CaptureChunk6aCausalState();
            var noResidue = !playerAction.HasVoluntaryTransitionInFlight && rider.Commands.Empty && horse.Commands.Empty;
            chunk6aObstructionEvidence["noResidue"] = noResidue;
            var pass = failure == null && observedFailure && (bool)path["complete"] && (bool)proof["pass"] && noResidue;
            AddRow("CM02-obstruction", pass, failure ?? (pass ?
                "The exact native Mount observed a real closed-door path failure and terminated unacted without a process, cost, transition or residue; no diagnostic Stop was issued." :
                "The closed-door case did not prove the exact native path-failure terminal and conserved action/reaction window."), chunk6aObstructionEvidence);
            if (!pass) { BeginCleanup(); return; }
            chunk6aStage = 21; ResetLeafClock();
        }
        private bool CaptureChunk6aObstructionDeadline()
        {
            if (!IsChunk6aCombatMount || !Chunk6aNeedsDoor) return false;
            if (chunk6aStage == 20 && chunk6aCommandWindow != null && chunk6aPath != null)
            {
                var failure = chunk6aPath.FailureObserved ?
                    "Observed native path failure left the exact unacted Mount pending at the unchanged 30-second deadline." :
                    "Instrumentation did not observe an exact native obstruction boundary before the unchanged 30-second deadline.";
                FinishChunk6aObstruction(failure); return true;
            }
            if (chunk6aPath != null) observations["chunk6aDoorSetupLivePath"] = chunk6aPath.Capture();
            if (Chunk6aObstructionActive)
            { FailCurrent("CM02-obstruction", "Native door setup or restoration did not settle at the unchanged 30-second deadline."); return true; }
            return false;
        }
        private void CleanupChunk6aObstruction()
        {
            // Evidence is already sealed. Cleanup interruption can never qualify a terminal.
            chunk6aPath?.Dispose(); chunk6aPath = null;
            chunk6aBlockedCommand?.Interrupt(); chunk6aDoorGround?.Interrupt();
            RestoreChunk6aDoor();
        }
    }
}
