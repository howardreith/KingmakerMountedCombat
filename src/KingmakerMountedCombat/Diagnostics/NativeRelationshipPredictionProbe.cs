using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.UnitLogic.Commands;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class NativeRelationshipCommandProbe
    {
        private readonly List<UnitUseAbility> predictionCommands = new List<UnitUseAbility>();
        private readonly JArray predictionInits = new JArray();
        private JObject PredictionSample(UnitUseAbility command) => new JObject {
            ["frame"] = Time.frameCount, ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks,
            ["allocationSequence"] = trace.EventCount,
            ["simulatingClick"] = Kingmaker.Controllers.Clicks.PointerController.SimulatingClick,
            ["commandObject"] = Id(command), ["casterId"] = command.Executor?.UniqueId,
            ["targetId"] = command.Target?.Unit?.UniqueId, ["commandType"] = command.Type.ToString(),
            ["abilityGuid"] = command.Spell?.Blueprint?.AssetGuid,
            ["started"] = command.IsStarted, ["acted"] = command.IsActed,
            ["processObject"] = Id(command.ExecutionProcess), ["contextObject"] = Id(command.ExecutionProcess?.Context) };
        private void RecordPredictionCommand(UnitUseAbility command)
        {
            if (predictionCommands.Count >= 128 || predictionCommands.Any(c => ReferenceEquals(c, command)))
            { errors.Add("Repeated or excessive native speculative Init."); return; }
            var sample = PredictionSample(command);
            sample["identity"] = Describe(controls.CaptureRelationshipCommandIdentity(command));
            predictionCommands.Add(command); predictionInits.Add(sample);
        }
        private JObject CapturePredictionCommands() => new JObject {
            ["contract"] = "native-speculative-init-separated-from-one-committed-request",
            ["commands"] = new JArray(predictionCommands.Select((command, index) => new JObject {
                ["init"] = predictionInits[index].DeepClone(), ["close"] = PredictionSample(command) })) };
        private void CompletePredictionEvidence(JObject proof)
        {
            var prediction = CapturePredictionCommands(); proof["predictionCommands"] = prediction;
            try { NativePredictionCommandEvidence.AssertComplete(prediction, proof); prediction["pass"] = true; }
            catch (Exception exception) {
                prediction["pass"] = false; prediction["error"] = exception.Message;
                proof["pass"] = false; ((JArray)proof["errors"]).Add("Prediction observation: " + exception.Message);
            }
        }
    }
}
