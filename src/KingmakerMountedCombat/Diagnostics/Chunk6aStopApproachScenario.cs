using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic.Commands.Base;
using KingmakerMountedCombat.Domain;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        internal const string Chunk6aStopApproachScenario = "chunk6a-stop-approach";
        private bool Chunk6aStopOnly => request.Scenario == Chunk6aStopApproachScenario;
        private NativeMountStopInputProbe chunk6aStopInput;
        private JObject chunk6aStopEvidence;
        private bool chunk6aStopIssued;

        private void TickChunk6aStopApproach()
        {
            if (!Chunk6aStopOnly || Chunk6aTurnBased) throw new InvalidOperationException("Stop approach requires its isolated RT transaction.");
            if (chunk6aStage == 22)
            {
                if (!Chunk6aIdle || !nativeControls.Evaluate(NativeMountedControlKind.MountCompanion, rider).IsEnabled) return;
                if (!EnsureChunk6aRiderSelection("CM04-stop-during-approach")) return;
                var start = CaptureChunk6aGeometry("stop-pre-click");
                if ((bool)start["isAdjacent"]) throw new InvalidOperationException("Stop Mount must start outside transition reach.");
                chunk6aStopEvidence = new JObject { ["contract"] = "native-stop-during-exact-mount-approach", ["start"] = start };
                observations["chunk6aStopApproach"] = chunk6aStopEvidence;
                chunk6aCommandWindow = new NativeRelationshipCommandProbe(nativeControls, allocationTrace, rider, horse,
                    nativeControls.MountAbility.AssetGuid, CaptureChunk6aCausalState, true);
                var clicked = TryNativeAbilityTargetClick(nativeControls.MountAbility, horse, "chunk6a-stop-approach-click");
                chunk6aCommandWindow.ClickCompleted(clicked);
                if (!clicked) { FailCurrent("CM04-stop-during-approach", "Exact rider native Mount click was refused: " + playerAction.LastFeedback); BeginCleanup(); return; }
                chunk6aStage = 23; ResetLeafClock(); return;
            }
            var command = chunk6aCommandWindow?.Command;
            if (!chunk6aStopIssued)
            {
                if (command == null || command.IsStarted || command.IsActed || command.IsFinished || command.ExecutionProcess != null)
                { FailCurrent("CM04-stop-during-approach", "The exact Mount left pending approach before native Stop."); BeginCleanup(); return; }
                var moved = Chunk6aPlanarDistance((JObject)chunk6aStopEvidence["start"]["riderPosition"], CapturePosition(rider.Position));
                if (!chunk6aCommandWindow.ApproachObserved || !rider.View.AgentASP.IsReallyMoving || moved <= 0.25f) return;
                var geometry = CaptureChunk6aGeometry("stop-trigger");
                var slot = rider.Commands.GetCommand(UnitCommand.CommandType.Move);
                var selected = SelectionManager.Instance.SelectedUnits;
                if ((bool)geometry["isAdjacent"] || !ReferenceEquals(slot, command) || selected.Count != 1 || selected[0] != rider)
                { FailCurrent("CM04-stop-during-approach", "Stop missed the exact selected rider's non-adjacent pending Mount."); BeginCleanup(); return; }
                chunk6aStopEvidence["trigger"] = new JObject { ["frame"] = Time.frameCount,
                    ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks, ["approachObserved"] = true,
                    ["riderReallyMoving"] = rider.View.AgentASP.IsReallyMoving, ["riderDisplacement"] = moved,
                    ["commandObject"] = RuntimeHelpers.GetHashCode(command), ["moveSlotObject"] = RuntimeHelpers.GetHashCode(slot),
                    ["started"] = command.IsStarted, ["acted"] = command.IsActed, ["finished"] = command.IsFinished, ["geometry"] = geometry };
                chunk6aStopInput = new NativeMountStopInputProbe(rider, command, CaptureChunk6aCausalState, allocationTrace);
                chunk6aStopIssued = true;
                SelectionManager.Instance.Stop();
                chunk6aStopEvidence["input"] = chunk6aStopInput.Capture();
                chunk6aStopInput.Dispose(); chunk6aStopInput = null;
                return;
            }
            if (!command.IsFinished || !Chunk6aIdle) return;
            var proof = chunk6aCommandWindow.FinishUnacted("unacted-native-stop-no-cost-or-transition");
            chunk6aCommandWindow.Dispose(); chunk6aCommandWindow = null;
            chunk6aStopEvidence["commandProof"] = proof;
            chunk6aStopEvidence["terminal"] = CaptureOrdinaryCommand(command);
            chunk6aStopEvidence["after"] = CaptureChunk6aCausalState();
            var input = (JObject)chunk6aStopEvidence["input"];
            var noCost = !((JArray)input["allocationEvents"]).OfType<JObject>().Any(e =>
                ((string)e["state"]?["actor"] == rider.UniqueId || (string)e["state"]?["actor"] == horse.UniqueId) &&
                new[] { "cost-", "actor-cost-", "prepare-", "clear-", "combat-clear", "admission-" }.Any(prefix => ((string)e["boundary"]).StartsWith(prefix, StringComparison.Ordinal)));
            var noResidue = !playerAction.HasVoluntaryTransitionInFlight && rider.Commands.Empty && horse.Commands.Empty;
            chunk6aStopEvidence["noResidue"] = noResidue;
            var pass = (bool)proof["pass"] && command.Result == UnitCommand.ResultType.Interrupt && noResidue && noCost &&
                (bool)input["complete"] && (bool)input["allocationTraceComplete"] && ((JArray)input["events"]).Count == 3 &&
                ((JArray)input["events"]).OfType<JObject>().Select(e => (string)e["boundary"]).SequenceEqual(new[] { "stop-before", "command-ended", "stop-after" });
            AddRow("CM04-stop-during-approach", pass,
                pass ? "One native Stop surrounded the exact pending Mount's native Interrupt/OnEnded after measured approach; no acted cost, process, transition or residue." :
                "The exact native Stop or conserved unacted command proof failed.", chunk6aStopEvidence);
            chunk6aStage = 99; BeginCleanup();
        }
        private bool CaptureChunk6aStopDeadline()
        {
            if (!Chunk6aStopOnly || chunk6aStage < 22 || chunk6aStage > 23) return false;
            observations["chunk6aStopDeadline"] = chunk6aCommandWindow?.Capture();
            if (chunk6aStopInput != null) observations["chunk6aStopInputDeadline"] = chunk6aStopInput.Capture();
            FailCurrent("CM04-stop-during-approach", "Exact native approach, Stop or terminal did not settle at the unchanged 30-second deadline.");
            return true;
        }
        private void CleanupChunk6aStopInput()
        { chunk6aStopInput?.Dispose(); chunk6aStopInput = null; }
    }
}