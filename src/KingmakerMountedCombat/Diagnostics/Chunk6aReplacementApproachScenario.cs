using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using KingmakerMountedCombat.Domain;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        internal const string Chunk6aReplacementApproachScenario = "chunk6a-command-replacement";
        private bool Chunk6aReplacementOnly => request.Scenario == Chunk6aReplacementApproachScenario;
        private NativeMountReplacementInputProbe chunk6aReplacementInput;
        private JObject chunk6aReplacementEvidence;
        private bool chunk6aReplacementIssued;
        private UnitMoveTo chunk6aReplacementCommand;

        private void TickChunk6aReplacementApproach()
        {
            if (!Chunk6aReplacementOnly || Chunk6aTurnBased) throw new InvalidOperationException("Replacement approach requires its isolated RT transaction.");
            if (chunk6aStage == 34)
            {
                if (!Chunk6aIdle || !nativeControls.Evaluate(NativeMountedControlKind.MountCompanion, rider).IsEnabled) return;
                if (!EnsureChunk6aRiderSelection("CM04-command-replacement")) return;
                var start = CaptureChunk6aGeometry("replacement-pre-click");
                if ((bool)start["isAdjacent"]) throw new InvalidOperationException("Replacement Mount must start outside transition reach.");
                chunk6aReplacementEvidence = new JObject { ["contract"] = "native-replacement-during-exact-mount-approach", ["start"] = start };
                observations["chunk6aReplacementApproach"] = chunk6aReplacementEvidence;
                chunk6aCommandWindow = new NativeRelationshipCommandProbe(nativeControls, allocationTrace, rider, horse,
                    nativeControls.MountAbility.AssetGuid, CaptureChunk6aCausalState, true);
                var clicked = TryNativeAbilityTargetClick(nativeControls.MountAbility, horse, "chunk6a-command-replacement-click");
                chunk6aCommandWindow.ClickCompleted(clicked);
                if (!clicked) { FailCurrent("CM04-command-replacement", "Exact rider native Mount click was refused: " + playerAction.LastFeedback); BeginCleanup(); return; }
                chunk6aStage = 35; ResetLeafClock(); return;
            }
            var command = chunk6aCommandWindow?.Command;
            if (!chunk6aReplacementIssued)
            {
                if (command == null || command.IsStarted || command.IsActed || command.IsFinished || command.ExecutionProcess != null)
                { FailCurrent("CM04-command-replacement", "The exact Mount left pending approach before native replacement."); BeginCleanup(); return; }
                var moved = Chunk6aPlanarDistance((JObject)chunk6aReplacementEvidence["start"]["riderPosition"], CapturePosition(rider.Position));
                if (!chunk6aCommandWindow.ApproachObserved || !rider.View.AgentASP.IsReallyMoving || moved <= 0.25f) return;
                var geometry = CaptureChunk6aGeometry("replacement-trigger");
                var slot = rider.Commands.GetCommand(UnitCommand.CommandType.Move);
                var selected = SelectionManager.Instance.SelectedUnits;
                if ((bool)geometry["isAdjacent"] || !ReferenceEquals(slot, command) || selected.Count != 1 || selected[0] != rider)
                { FailCurrent("CM04-command-replacement", "Replacement missed the exact selected rider's non-adjacent pending Mount."); BeginCleanup(); return; }
                chunk6aReplacementEvidence["trigger"] = new JObject { ["frame"] = Time.frameCount,
                    ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks, ["approachObserved"] = true,
                    ["riderReallyMoving"] = rider.View.AgentASP.IsReallyMoving, ["riderDisplacement"] = moved,
                    ["commandObject"] = RuntimeHelpers.GetHashCode(command), ["moveSlotObject"] = RuntimeHelpers.GetHashCode(slot),
                    ["started"] = command.IsStarted, ["acted"] = command.IsActed, ["finished"] = command.IsFinished, ["geometry"] = geometry };
                // Use the measured pre-click origin as the replacement destination.
                // Admission cancels the old command synchronously; replacement ticks are outside its resource window.
                var destination = chunk6aReplacementEvidence["start"]["riderPosition"];
                chunk6aReplacementCommand = new UnitMoveTo(new Vector3((float)destination["x"], (float)destination["y"], (float)destination["z"])) { CreatedByPlayer = true };
                chunk6aReplacementEvidence["destination"] = destination.DeepClone();
                chunk6aReplacementInput = new NativeMountReplacementInputProbe(rider, command, chunk6aReplacementCommand, CaptureChunk6aCausalState, allocationTrace);
                chunk6aReplacementIssued = true;
                rider.Commands.Run(chunk6aReplacementCommand);
                chunk6aReplacementEvidence["input"] = chunk6aReplacementInput.Capture();
                chunk6aReplacementInput.Dispose(); chunk6aReplacementInput = null;
            }
            // Finish the original Mount window before any replacement movement or cost.
            if (!command.IsFinished) { FailCurrent("CM04-command-replacement", "Native replacement did not synchronously terminate the exact pending Mount."); BeginCleanup(); return; }
            var proof = chunk6aCommandWindow.FinishUnacted("unacted-native-replacement-no-cost-or-transition");
            chunk6aCommandWindow.Dispose(); chunk6aCommandWindow = null;
            chunk6aReplacementEvidence["commandProof"] = proof;
            chunk6aReplacementEvidence["terminal"] = CaptureOrdinaryCommand(command);
            chunk6aReplacementEvidence["after"] = CaptureChunk6aCausalState();
            var input = (JObject)chunk6aReplacementEvidence["input"];
            var noCost = !((JArray)input["allocationEvents"]).OfType<JObject>().Any(e =>
                ((string)e["state"]?["actor"] == rider.UniqueId || (string)e["state"]?["actor"] == horse.UniqueId) &&
                new[] { "cost-", "actor-cost-", "prepare-", "clear-", "combat-clear" }.Any(prefix => ((string)e["boundary"]).StartsWith(prefix, StringComparison.Ordinal)));
            var noResidue = !playerAction.HasVoluntaryTransitionInFlight && horse.Commands.Empty &&
                !rider.Commands.Raw.Concat(rider.Commands.Queue).Any(c => ReferenceEquals(c, command)) &&
                ReferenceEquals(rider.Commands.GetCommand(UnitCommand.CommandType.Move), chunk6aReplacementCommand);
            chunk6aReplacementEvidence["noResidue"] = noResidue;
            var pass = (bool)proof["pass"] && command.Result == UnitCommand.ResultType.Interrupt && noResidue && noCost &&
                (bool)input["complete"] && (bool)input["allocationTraceComplete"] && ((JArray)input["events"]).Count == 3 &&
                ((JArray)input["events"]).OfType<JObject>().Select(e => (string)e["boundary"]).SequenceEqual(new[] { "replacement-before", "command-ended", "replacement-after" });
            AddRow("CM04-command-replacement", pass,
                pass ? "One exact native ground replacement surrounded the pending Mount's native Interrupt/OnEnded after measured approach; the Mount added no acted cost, process, transition or residue." :
                "The exact native replacement or conserved unacted Mount proof failed.", chunk6aReplacementEvidence);
            chunk6aStage = 99; BeginCleanup();
        }
        private bool CaptureChunk6aReplacementDeadline()
        {
            if (!Chunk6aReplacementOnly || chunk6aStage < 34 || chunk6aStage > 35) return false;
            observations["chunk6aReplacementDeadline"] = chunk6aCommandWindow?.Capture();
            if (chunk6aReplacementInput != null) observations["chunk6aReplacementInputDeadline"] = chunk6aReplacementInput.Capture();
            FailCurrent("CM04-command-replacement", "Exact native approach, replacement or terminal did not settle at the unchanged 30-second deadline.");
            return true;
        }
        private void CleanupChunk6aReplacementInput()
        { chunk6aReplacementInput?.Dispose(); chunk6aReplacementInput = null; }
    }
}