using System;
using Kingmaker;
using KingmakerMountedCombat.Domain;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        private bool chunk6aRefusalPolicyOwned;
        private NativePassiveResourceProbe chunk6aRefusalPolicyResources;
        private JObject chunk6aRefusalPolicyEvidence;
        private JObject CaptureChunk6aRefusalPolicy() => new JObject {
            ["frame"] = Time.frameCount, ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks,
            ["allocationSequence"] = allocationTrace.EventCount, ["state"] = CaptureChunk6aCausalState(),
            ["settings"] = new JObject { ["movement"] = settings.EnableUnsafeMovementExperiment,
                ["paired"] = settings.EnablePairedActivation, ["unified"] = settings.EnableUnifiedMountedTurn,
                ["scheduler"] = settings.EnablePairedCommandScheduler, ["overlay"] = settings.EnableDiagnosticOverlay }
        };
        private void BeginChunk6aRefusalPolicy()
        {
            if (chunk6aRefusalPolicyOwned || !settings.EnableUnsafeMovementExperiment || !settings.EnablePairedActivation ||
                settings.EnableUnifiedMountedTurn || settings.EnablePairedCommandScheduler || settings.EnableDiagnosticOverlay ||
                relationship.State != RelationshipState.Unmounted)
                throw new InvalidOperationException("Policy refusal requires the accepted unmounted baseline.");
            chunk6aRefusalPolicyResources = new NativePassiveResourceProbe(allocationTrace, rider, horse);
            chunk6aRefusalPolicyEvidence = new JObject { ["contract"] = "synchronous-paired-policy-disabled-refusal-restored",
                ["before"] = CaptureChunk6aRefusalPolicy() };
            observations["chunk6aRefusalPolicy"] = chunk6aRefusalPolicyEvidence;
            chunk6aRefusalPolicyOwned = true;
            settings.EnablePairedActivation = false;
            nativeControls.Update();
            chunk6aRefusalPolicyEvidence["disabled"] = CaptureChunk6aRefusalPolicy();
        }
        private void RestoreChunk6aRefusalPolicy()
        {
            if (!chunk6aRefusalPolicyOwned) return;
            if (settings.EnablePairedActivation || !settings.EnableUnsafeMovementExperiment ||
                settings.EnableUnifiedMountedTurn || settings.EnablePairedCommandScheduler || settings.EnableDiagnosticOverlay)
                throw new InvalidOperationException("Policy refusal setting changed outside its owned window.");
            settings.EnablePairedActivation = true;
            chunk6aRefusalPolicyOwned = false;
            nativeControls.Update();
            chunk6aRefusalPolicyEvidence["restored"] = CaptureChunk6aRefusalPolicy();
        }
        private void CompleteChunk6aRefusalPolicy(JObject evidence)
        {
            RestoreChunk6aRefusalPolicy();
            chunk6aRefusalPolicyEvidence["resources"] = chunk6aRefusalPolicyResources.Finish();
            chunk6aRefusalPolicyResources.Dispose(); chunk6aRefusalPolicyResources = null;
            evidence["policy"] = chunk6aRefusalPolicyEvidence.DeepClone();
        }
        private void CleanupChunk6aRefusalPolicy()
        {
            try { RestoreChunk6aRefusalPolicy(); }
            finally { chunk6aRefusalPolicyResources?.Dispose(); chunk6aRefusalPolicyResources = null; }
        }
    }
}
