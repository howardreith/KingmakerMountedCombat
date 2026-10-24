using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.UI.Selection;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Chunk7CombatScenario
    {
        private bool cleanupProven, cleanupPendingReported;
        private readonly Dictionary<string, int> cleanupFailureAttempts = new Dictionary<string, int>();
        private void BeginCleanup()
        {
            if (!cleanupStarted)
            {
                cleanupStarted = true; stepStarted = clock.Elapsed.TotalSeconds;
                if (trace != null) evidence["allocationTrace"] = trace.Capture();
                if (completion != null) evidence["nativeCompletion"] = completion.Capture();
                if (rounds != null) evidence["nativeRounds"] = rounds.Capture();
                if (realtimeEffects != null) evidence["nativeEffects"] = realtimeEffects.Capture();
                if (nativeLife != null && lifeDelivery != null) nativeLife["deliveryProbe"] = lifeDelivery.Capture();
                if (nativeLife != null && lifeEffects != null) nativeLife["nativeEffects"] = lifeEffects.Capture();
            }
            if (rounds != null) rounds.AfterNativeRound = null;
            if (cleanupFault != null && b != null) { b.Relationship.Dismounting -= cleanupFault; cleanupFault = null; }
        }
        private bool TryCleanup(string part, Action operation)
        {
            try { operation(); return true; }
            catch (Exception error)
            {
                var key = part + "|" + error.GetType().FullName + "|" + error.Message;
                int attempts; cleanupFailureAttempts.TryGetValue(key, out attempts); cleanupFailureAttempts[key] = attempts + 1;
                if (attempts == 0) Fail("C7-combat-cleanup", part + ": " + error);
                evidence["cleanupFailureAttempts"] = JObject.FromObject(cleanupFailureAttempts);
                return false;
            }
        }
        private bool ReleaseCleanupObservers()
        {
            var passed = TryCleanup("registered disable observer and re-enable custody", EndRegisteredDisableObservation);
            passed &= TryCleanup("native life delivery observer", () => { lifeDelivery?.Dispose(); lifeDelivery = null; });
            passed &= TryCleanup("native life effects observer", () => { lifeEffects?.Dispose(); lifeEffects = null; });
            passed &= TryCleanup("attack modes rule observer", () => { attackModesRules?.Dispose(); attackModesRules = null; });
            passed &= TryCleanup("reaction rule observer", () => { reactionRules?.Dispose(); reactionRules = null; });
            passed &= TryCleanup("staged rule observer", () => { stagedRules?.Dispose(); stagedRules = null; });
            passed &= TryCleanup("RT effects observer", () => { realtimeEffects?.Dispose(); realtimeEffects = null; });
            passed &= TryCleanup("completion observer", () => { completion?.Dispose(); completion = null; });
            passed &= TryCleanup("round observer", () => { rounds?.Dispose(); rounds = null; });
            passed &= TryCleanup("allocation observer", () => { trace?.Dispose(); trace = null; });
            return passed;
        }
        private void AwaitCleanup(bool settled, string message)
        {
            if (settled || clock.Elapsed.TotalSeconds - stepStarted <= 45 || cleanupPendingReported) return;
            Fail("C7-combat-cleanup", message + " Cleanup custody remains retained at the bounded host boundary.");
            evidence["cleanupPending"] = new JObject { ["reason"] = message, ["serializationBusy"] = world.SerializationBusy,
                ["loading"] = LoadingProcess.Instance.IsLoadingInProcess, ["queuedLoads"] = new JArray(LoadingProcess.Instance.QueuedNames),
                ["pendingNativeWrites"] = NativePersistenceIsolation.HasPendingWrites,
                ["preparationPending"] = world.UnifiedTurn.HasUnsettledPreparation,
                ["relationshipTransitionPending"] = controls.HasUnsettledRelationshipTransition, ["settled"] = false };
            if (!checkpointOnly) RuntimeLoaderInventory.WriteArtifact(request, "chunk7-combat-cleanup-pending.json",
                "chunk7-combat-cleanup-pending", "FAIL", evidence, errors);
            cleanupPendingReported = true;
        }
        private void PollCleanup()
        {
            if (cleanupProven) { CompleteEvidence(); return; }
            if (LoadingProcess.Instance.IsLoadingInProcess || LoadingProcess.Instance.QueuedNames.Any() ||
                NativePersistenceIsolation.HasPendingWrites || world.SerializationBusy)
            { AwaitCleanup(false, "A native load, queued load or write worker still owns fixture state."); return; }
            if (captured && !TryCleanup("captured native world", () => {
                Require(nativeScope != null, "Captured native world authority is absent."); nativeScope.RequireCurrent();
            })) { AwaitCleanup(false, "Captured native world, area or actor identity changed."); return; }
            if (!ReleaseCleanupObservers()) { AwaitCleanup(false, "Native observer teardown has not settled."); return; }
            if (!captured) { if (!checkpointOnly) inherited?.Release(); cleanupProven = true; CompleteEvidence(); return; }
            Game.Instance.IsPaused = false;
            var commandsSettled = true;
            foreach (var command in issuedCommands.Where(c => c != null && !c.IsFinished))
                commandsSettled &= TryCleanup("owned command", () => command.Interrupt());
            foreach (var pair in world.Pairs)
                commandsSettled &= TryCleanup("pair command", () => pair.Combat.Cancel("Chunk 7 combat fixture cleanup"));
            commandsSettled &= issuedCommands.Where(c => c != null).All(c => c.IsFinished &&
                (!(c is Kingmaker.UnitLogic.Commands.UnitUseAbility) || ((Kingmaker.UnitLogic.Commands.UnitUseAbility)c).ExecutionProcess == null ||
                 ((Kingmaker.UnitLogic.Commands.UnitUseAbility)c).ExecutionProcess.IsEnded)) &&
                world.Pairs.All(p => !p.Combat.HasActiveCommand && !p.Combat.HasActiveGroundMovement && !p.Combat.HasChargeOwnership &&
                    !controls.HasUnsettledShellFor(p.Relationship));
            if (!commandsSettled || world.UnifiedTurn.HasUnsettledPreparation || controls.HasUnsettledRelationshipTransition)
            { AwaitCleanup(false, "Native command, process, preparation or relationship transition has not settled."); return; }
            // The staged scroll stack leaves only after every owned command and cast process has settled.
            if (!TryCleanup("staged scroll stack and cast observer", ReleaseStagedFixture))
            { AwaitCleanup(false, "The staged scroll stack or its original quick slots have not settled."); return; }
            if (!cleanupServicesSettled)
            {
                var targetGone = false;
                TryCleanup("target", () => { targetGone = targetService == null || targetService.DestroyAndVerify(); });
                foreach (var actor in inherited?.Party ?? actors ?? originalParty ?? new Kingmaker.EntitySystem.Entities.UnitEntityData[0])
                    if (actor.IsInState && actor.IsInCombat)
                        TryCleanup("captured native combat actor", () => actor.LeaveCombat());
                TryCleanup("native entity drain", () => Game.Instance.EntityDestroyer.Tick());
                if (!targetGone || Game.Instance.Player.IsInCombat || Game.Instance.TurnBasedCombatController.Initialized)
                { AwaitCleanup(false, "Native encounter cleanup has not settled."); return; }
                var leasesRestored = TryCleanup("target observer", () => { targetService?.Dispose(); targetService = null; });
                leasesRestored &= TryCleanup("initiative inputs", () => { foreach (var entry in initiativeBefore) entry.Key.Stats.Initiative.BaseValue = entry.Value; });
                leasesRestored &= TryCleanup("original mode", () => { mode?.Dispose(); mode = null; });
                leasesRestored &= TryCleanup("first pair AI", () => {
                    if (firstPairAi == null || firstPairAi.LastRestoreVerified) return;
                    firstPairAi.ReassertAfterNativeReset(new[] { ar, am, br }); firstPairAi.Restore(new[] { ar, am, br });
                });
                if (!leasesRestored) { AwaitCleanup(false, "A source fixture lease has not restored."); return; }
                cleanupServicesSettled = true;
            }
            if (!checkpointOnly)
            {
                foreach (var pair in world.Pairs)
                    TryCleanup("relationship", () => Require(pair.Relationship.Dismount(CleanupTrigger.Exception).Succeeded, "Pair cleanup retained obligations."));
            }
            var pairClean = checkpointOnly || world.Pairs.All(p => p.Relationship.State == RelationshipState.Unmounted &&
                p.Relationship.Runtime.NoPreparedPairResidue && p.Relationship.PairOwner == null && !controls.HasUnsettledShellFor(p.Relationship));
            if (!pairClean) { AwaitCleanup(false, "A pair still owns cleanup before native progression removal."); return; }
            if (!checkpointOnly && inherited == null)
            {
                if (horseFeature != null)
                    TryCleanup("owned Horse fact", () => {
                        Require(br.Descriptor.Pet == null || ReferenceEquals(br.Descriptor.Pet, bm), "Cleanup refuses a replaced native pet.");
                        br.Descriptor.Progression.Features.RemoveFact(horseFeature); horseFeature = null;
                    });
                if (rankFeature != null)
                    TryCleanup("owned rank fact", () => { br.Descriptor.Progression.Features.RemoveFact(rankFeature); rankFeature = null; });
            }
            TryCleanup("native entity drain", () => Game.Instance.EntityDestroyer.Tick());
            var inheritedClean = inherited != null && TryCleanup("inherited native facts", inherited.RequirePreserved);
            var horseClean = (checkpointOnly || inherited != null) ? checkpointOnly || inheritedClean : horseFeature == null && rankFeature == null &&
                (bm == null || !bm.IsInState && !Game.Instance.State.Units.Contains(bm) && br.Descriptor.Pet == null);
            if (!horseClean || world.UnifiedTurn.HasUnsettledPreparation || controls.HasUnsettledRelationshipTransition ||
                world.Pairs.Any(p => controls.HasUnsettledShellFor(p.Relationship)))
            { AwaitCleanup(false, "Native cleanup retains a progression, shell or preparation obligation."); return; }
            if (!checkpointOnly)
            {
                var settingsRestored = TryCleanup("settings", () => {
                    Require(world.Primary.Combat.TryConfigurePairedActivation(pairedBefore), "Original paired configuration cannot restore yet.");
                    settings.EnableUnsafeMovementExperiment = movementBefore; settings.EnableMountedCharge = chargeBefore;
                });
                settingsRestored &= TryCleanup("selection", () => SelectionManager.Instance.MultiSelect(selectionBefore.Where(x => x.IsInState && x.View != null).Select(x => x.View), false));
                settingsRestored &= TryCleanup("UMM", () => { umm?.Dispose(); umm = null; });
                if (!settingsRestored) { AwaitCleanup(false, "Fixture settings have not restored."); return; }
                Game.Instance.IsPaused = pauseBefore;
                if (LifeDeath && nativeLife != null) nativeLife["deathCleanup"] = CaptureDeathCleanup();
            }
            var cleanupReady = initiativeBefore.All(x => x.Key.Stats.Initiative.BaseValue == x.Value) &&
                (firstPairAi == null || firstPairAi.LastRestoreVerified) && mode == null && targetService == null;
            Require(cleanupReady, "Cleanup postcondition does not match the captured native fixture state.");
            if (inherited != null && !checkpointOnly) evidence["inheritedCleanup"] = inherited.Capture("cleanup");
            evidence["cleanup"] = new JObject { ["pairResidueAbsent"] = pairClean, ["ownedHorseRemoved"] = inherited == null && horseClean,
                ["inheritedPetsPreserved"] = inherited != null && inheritedClean,
                ["nativeCombatEnded"] = !Game.Instance.Player.IsInCombat && !Game.Instance.TurnBasedCombatController.Initialized,
                ["initiativeInputsRestored"] = true, ["firstPairAiRestored"] = true, ["controlsSettled"] = true,
                ["nativeProgressionRequiresExternalWorkingRestoration"] = !checkpointOnly && inherited == null,
                ["nativeSaveWrittenByThisDriver"] = false, ["delegatedPersistenceCleanup"] = checkpointOnly };
            if (inherited != null && !checkpointOnly) inherited.Release();
            cleanupProven = true; Pass("C7-combat-cleanup"); CompleteEvidence();
        }
        private void CompleteEvidence()
        {
            if (completed) return;
            evidence["elapsedSeconds"] = clock.Elapsed.TotalSeconds;
            evidence["nativeShellLifecycle"] = controls.DescribeNativeShellLifecycle();
            evidence["fullRealtimeCombatProof"] = false; evidence["naturalUnconsciousStimulusProven"] = SelectedScenario == NativeLifeCleanup && !LifeDeath && (bool?)nativeLife?["complete"] == true;
            if (SelectedScenario == NativeLifeCleanup && LifeDeath) evidence["naturalDeathStimulusProven"] = (bool?)nativeLife?["complete"] == true;
            if (!checkpointOnly) RuntimeLoaderInventory.WriteArtifact(request, SelectedScenario == NativeLifeCleanup ? "chunk7-native-life-evidence.json" : RealtimeScenario ? "chunk7-realtime-evidence.json" : EvidenceFileName,
                SelectedScenario == NativeLifeCleanup ? "chunk7-native-life-evidence" : RealtimeScenario ? "chunk7-realtime-evidence" : EvidenceKind, errors.Count == 0 ? "PASS" : "FAIL", evidence, errors);
            completed = true;
        }
        internal bool DrainForHost()
        {
            if (disposed) return true;
            if (!cleanupStarted)
            {
                if (!checkpointOnly) Fail("C7-combat-interrupted", "Host requested cleanup before the selected combat case completed at step " + step + ", action " + actionStage + ".");
                BeginCleanup();
            }
            if (!completed) PollCleanup();
            return completed && cleanupProven;
        }
        public void Dispose()
        {
            if (disposed) return;
            if (!DrainForHost()) throw new InvalidOperationException("Combat fixture cleanup is retained and requires bounded owner polling.");
            disposed = true;
        }
    }
}
