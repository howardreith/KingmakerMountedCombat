using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;
namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        internal static bool IsChunk6aDismountEscapeScenario(string scenario) =>
            scenario == "chunk6a-dismount-feature-disabled-rt" || scenario == "chunk6a-dismount-policy-disabled-rt";
        private bool Chunk6aDismountEscapeOnly => IsChunk6aDismountEscapeScenario(request.Scenario);
        private const string Chunk6aEscapeRow = "CM05-dismount-survives-feature-policy-disable";
        private NativePassiveResourceProbe chunk6aEscapeWait;
        private JObject chunk6aEscapeEvidence;
        private bool chunk6aEscapeSettingsOwned, chunk6aEscapeOriginalMovement, chunk6aEscapeOriginalPaired;
        private int chunk6aEscapeDisabledFrame;
        private bool Chunk6aEscapeMovement => request.Scenario == "chunk6a-dismount-feature-disabled-rt";
        private JObject CaptureChunk6aEscapeBoundary()
        {
            var ability = rider.Descriptor.Abilities.GetAbility(nativeControls.DismountAbility);
            var availability = nativeControls.Evaluate(NativeMountedControlKind.Dismount, rider);
            return new JObject {
                ["frame"] = Time.frameCount, ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks,
                ["allocationSequence"] = allocationTrace.EventCount, ["state"] = CaptureChunk6aCausalState(),
                ["riderResources"] = allocationTrace.Snapshot(rider), ["mountResources"] = allocationTrace.Snapshot(horse),
                ["settings"] = new JObject { ["movement"] = settings.EnableUnsafeMovementExperiment, ["paired"] = settings.EnablePairedActivation,
                    ["legacyUnified"] = settings.EnableUnifiedMountedTurn, ["legacyScheduler"] = settings.EnablePairedCommandScheduler, ["overlay"] = settings.EnableDiagnosticOverlay },
                ["abilityPresent"] = ability?.Active == true && ability.Data != null,
                ["abilityObject"] = ability == null ? 0 : RuntimeHelpers.GetHashCode(ability),
                ["abilityGuid"] = ability?.Blueprint?.AssetGuid, ["abilityCasterId"] = ability?.Data?.Caster?.Unit?.UniqueId,
                ["visible"] = availability.IsVisible, ["enabled"] = availability.IsEnabled, ["reason"] = availability.Reason,
                ["pairIdle"] = Chunk6aIdle, ["inCombat"] = rider.IsInCombat && horse.IsInCombat && Game.Instance.Player.IsInCombat,
                ["turnBased"] = CombatController.IsInTurnBasedCombat()
            };
        }
        private void BeginChunk6aDismountEscape(JObject mountProof)
        {
            if (!Chunk6aDismountEscapeOnly || Chunk6aTurnBased || !Chunk6aIdle || chunk6aCommandWindow != null ||
                relationship.State != RelationshipState.Mounted || (bool?)mountProof?["pass"] != true)
                throw new InvalidOperationException("Dismount escape requires its own settled positive RT Mount.");
            chunk6aEscapeOriginalMovement = settings.EnableUnsafeMovementExperiment;
            chunk6aEscapeOriginalPaired = settings.EnablePairedActivation;
            if (!chunk6aEscapeOriginalMovement || !chunk6aEscapeOriginalPaired || settings.EnableUnifiedMountedTurn ||
                settings.EnablePairedCommandScheduler || settings.EnableDiagnosticOverlay)
                throw new InvalidOperationException("Dismount escape baseline lacks accepted paired preset.");
            chunk6aEscapeEvidence = new JObject { ["contract"] = "settled-positive-rt-mount-disabled-setting-native-dismount",
                ["scenario"] = request.Scenario, ["case"] = Chunk6aEscapeMovement ? "feature" : "policy",
                ["mountProof"] = mountProof.DeepClone(), ["beforeDisable"] = CaptureChunk6aEscapeBoundary() };
            observations["chunk6aDismountEscape"] = chunk6aEscapeEvidence;
            var mountBridge = NativeRelationshipTerminalBridge.Capture(allocationTrace, mountProof, (JObject)chunk6aEscapeEvidence["beforeDisable"], false);
            chunk6aEscapeEvidence["mountTerminalBridge"] = mountBridge;
            NativeTerminalBridgeEvidence.AssertComplete(mountProof, (JObject)chunk6aEscapeEvidence["beforeDisable"], mountBridge, false, "Mounted");
            chunk6aEscapeWait = new NativePassiveResourceProbe(allocationTrace, rider, horse);
            chunk6aEscapeSettingsOwned = true;
            if (Chunk6aEscapeMovement) settings.EnableUnsafeMovementExperiment = false;
            else settings.EnablePairedActivation = false;
            nativeControls.Update();
            chunk6aEscapeDisabledFrame = Time.frameCount;
            chunk6aEscapeEvidence["afterDisable"] = CaptureChunk6aEscapeBoundary();
            chunk6aStage = 27; ResetLeafClock();
        }
        private void RequireChunk6aEscapeSetting()
        {
            if (!chunk6aEscapeSettingsOwned || settings.EnableUnsafeMovementExperiment != !Chunk6aEscapeMovement ||
                settings.EnablePairedActivation != Chunk6aEscapeMovement || settings.EnableUnifiedMountedTurn ||
                settings.EnablePairedCommandScheduler || settings.EnableDiagnosticOverlay)
                throw new InvalidOperationException("Dismount escape settings changed outside the owned lease.");
        }
        private void TickChunk6aDismountEscape()
        {
            RequireChunk6aEscapeSetting();
            if (chunk6aStage == 27)
            {
                var observed = CaptureChunk6aEscapeBoundary(); chunk6aEscapeEvidence["waiting"] = observed;
                if (!(bool)observed["inCombat"] || (bool)observed["turnBased"] || relationship.State != RelationshipState.Mounted ||
                    !(bool)observed["abilityPresent"] || !(bool)observed["visible"])
                    throw new InvalidOperationException("Disabled setting stranded or forcibly detached the exact mounted pair: " + observed);
                if (!Chunk6aIdle || Time.frameCount < chunk6aEscapeDisabledFrame + 10 || !(bool)observed["enabled"]) return;
                if (!EnsureChunk6aRiderSelection(Chunk6aEscapeRow)) return;
                var passive = chunk6aEscapeWait.Finish();
                chunk6aEscapeEvidence["waitResources"] = passive;
                chunk6aEscapeWait.Dispose(); chunk6aEscapeWait = null;
                if ((bool?)passive["pass"] != true) throw new InvalidOperationException("Dismount escape wait resource proof failed: " + passive["failure"]);
                chunk6aEscapeEvidence["beforeClick"] = CaptureChunk6aEscapeBoundary();
                NativeDismountEscapeEvidence.AssertStimulus(chunk6aEscapeEvidence);
                BeginChunk6aCommandWindow(nativeControls.DismountAbility.AssetGuid);
                chunk6aDismountClicked = TryNativeAbilityTargetClick(nativeControls.DismountAbility, rider, "chunk6a-disabled-dismount-click");
                chunk6aCommandWindow.ClickCompleted(chunk6aDismountClicked);
                chunk6aEscapeEvidence["clicked"] = chunk6aDismountClicked;
                chunk6aEscapeEvidence["input"] = observations["chunk6a-disabled-dismount-click"]?.DeepClone();
                if (!chunk6aDismountClicked) throw new InvalidOperationException("Disabled-setting Dismount native click refused.");
                chunk6aStage = 28; ResetLeafClock(); return;
            }
            if (chunk6aStage != 28) throw new InvalidOperationException("Dismount escape stage differs.");
            if (relationship.State != RelationshipState.Unmounted || !Chunk6aIdle || chunk6aCommandWindow?.Terminal != true) return;
            var proof = FinishChunk6aCommandWindow("disabled-setting-dismount", true, 0, false);
            chunk6aEscapeEvidence["dismountProof"] = proof;
            chunk6aEscapeEvidence["afterDismount"] = CaptureChunk6aEscapeBoundary();
            chunk6aEscapeEvidence["dismountTerminalBridge"] = NativeRelationshipTerminalBridge.Capture(allocationTrace, proof, (JObject)chunk6aEscapeEvidence["afterDismount"], false);
            RestoreChunk6aEscapeSettings();
            string failure = null;
            try { NativeDismountEscapeEvidence.AssertComplete(chunk6aEscapeEvidence); }
            catch (Exception exception) { failure = exception.Message; }
            AddRow(Chunk6aEscapeRow, failure == null,
                failure ?? "The exact rider retained its leased Dismount under the disabled setting and one native command released the pair with its exact action/reaction proof; settings restored.", chunk6aEscapeEvidence);
            chunk6aStage = 99; BeginCleanup();
        }
        private void RestoreChunk6aEscapeSettings()
        {
            if (!chunk6aEscapeSettingsOwned) return;
            RequireChunk6aEscapeSetting();
            settings.EnableUnsafeMovementExperiment = chunk6aEscapeOriginalMovement;
            settings.EnablePairedActivation = chunk6aEscapeOriginalPaired;
            chunk6aEscapeSettingsOwned = false;
            nativeControls.Update();
            chunk6aEscapeEvidence["restored"] = CaptureChunk6aEscapeBoundary();
        }
        private void CleanupChunk6aDismountEscape()
        {
            if (chunk6aEscapeWait != null) chunk6aEscapeEvidence["waitAtCleanup"] = chunk6aEscapeWait.Capture();
            chunk6aEscapeWait?.Dispose(); chunk6aEscapeWait = null;
            RestoreChunk6aEscapeSettings();
        }
    }
}
