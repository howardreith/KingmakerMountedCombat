using System;
using System.Collections.Generic;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // Cutscene continuity repair (owner order 2026-11-12). The production decisions the integration composes (mode
    // disposition, command/in-game handoff, native staging ownership, combat hold, per-pair restore and bounded regroup)
    // driven against the real MountedRelationshipCoordinator with a counting runtime, so attachment transitions are
    // observed rather than assumed. Native acceptance is separate (NOT RUN on this machine).
    internal static class CutsceneContinuityTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("cutscene continuity: ordinary dialogue and a camera/input-lock scene keep the attachment and admit no input", OrdinaryScenesPreserve);
            runner.Run("cutscene continuity: a scripted mount move carries its rider; a scripted rider operation hands only that pair off", ScriptedCommandsHandOff);
            runner.Run("cutscene continuity: a handed-off pair restores without Mount after native staging ends, regrouping within bounds", StagingRestoresAfterRelease);
            runner.Run("cutscene continuity: dialogue that starts combat keeps the attached pair; a still-staged pair holds through the fight", DialogueIntoCombat);
            runner.Run("cutscene continuity: a scene during an encounter never hands off, dismounts or spends", SceneDuringEncounter);
            runner.Run("cutscene continuity: nested, skipped and aborted scenes read readiness from native state, never twice", NestedSkipAbort);
            runner.Run("cutscene continuity: unmounted, invalidated and unaffected pairs keep their own decisions", UnmountedInvalidatedMultiplePairs);
            runner.Run("cutscene continuity: the bounded regroup walks once or twice within its deadline and never teleports", RegroupBounds);
        }

        // Counts every native attachment step the real coordinator asks for.
        private sealed class CountingRuntime : IMountedPairRuntime
        {
            internal int Acquire, Attach, ReleasePresentation, ReleaseAuthority;
            internal readonly List<CleanupTrigger> Triggers = new List<CleanupTrigger>();
            public void AcquireMovementAuthority(MountedPair pair) { Acquire++; }
            public void AttachPresentation(MountedPair pair) { Attach++; }
            public void RestorePresentation(MountedPair pair) { ReleasePresentation++; }
            public void RestoreMovementAuthority(MountedPair pair, CleanupTrigger trigger) { ReleaseAuthority++; Triggers.Add(trigger); }
        }

        // The candidate the integration builds (KingmakerMountedPairRuntime.CreateCandidate): SafeMovementMode is
        // MountedGameModePolicy.CanAdmitMountedAction of the exact current mode.
        private static MountedPairCandidate Candidate(string mode, bool combat = false, string rider = "rider-a", string mount = "horse-a") =>
            new MountedPairCandidate(rider, mount)
            {
                RiderIsDirectlyControllable = true, MountIsDirectlyControllable = true, RiderIsAliveAndConscious = true, MountIsAliveAndConscious = true,
                ExactReciprocalCompanionRelationship = true, RiderSizeOrdinal = 4, MountSizeOrdinal = 5, RiderViewAndStockAgentAvailable = true,
                MountViewAndStockAgentAvailable = true, RiderStockAgentEnabled = true, MountStockAgentEnabled = true, RiderAgentOverrideAvailable = true,
                MountAgentOverrideAvailable = true, RiderIsExactlyMedium = true, PartyIsInCombat = combat, RiderIsInCombat = combat, MountIsInCombat = combat,
                SafeMovementMode = MountedGameModePolicy.CanAdmitMountedAction(mode)
            };

        private static MountedRelationshipCoordinator MountedPair(CountingRuntime runtime)
        {
            var coordinator = new MountedRelationshipCoordinator(runtime);
            TestRunner.True(coordinator.Mount(Candidate("Default")).Succeeded, "The fixture pair did not mount in Default.");
            runtime.Acquire = runtime.Attach = 0;
            return coordinator;
        }

        // The integration's mode boundary (MountedLifecycleSubscriber.ObserveOrCleanupGameMode): a retained mode is only
        // observed; Rest suspends; anything else cleans up.
        private static void ApplyModeBoundary(MountedRelationshipCoordinator coordinator, string mode)
        {
            if (MountedGameModePolicy.CanRetainMountedRelationship(mode) || coordinator.State != RelationshipState.Mounted) return;
            coordinator.Dismount(MountedRetainedIntentPolicy.SuspendsForEvent(mode) ? CleanupTrigger.EventSuspension : CleanupTrigger.GameModeBoundary);
        }

        private static void OrdinaryScenesPreserve()
        {
            var runtime = new CountingRuntime();
            var pair = MountedPair(runtime);
            // Hostile or friendly dialogue, then a cutscene that only locks input and moves the camera, nested and unwound.
            foreach (var mode in new[] { "Dialog", "Cutscene", "Dialog", "Cutscene", "Default" })
            {
                ApplyModeBoundary(pair, mode);
                TestRunner.Equal(RelationshipState.Mounted, pair.State, mode + " dismounted an already-mounted pair.");
                TestRunner.True(MountedViewActivityPolicy.IsAdmissible(MountedGameModePolicy.Classify(mode), true, true, true, true),
                    mode + " rejected the active mounted views.");
            }
            TestRunner.Equal(0, runtime.ReleaseAuthority + runtime.ReleasePresentation, "An ordinary scene released the attachment.");
            TestRunner.Equal(0, runtime.Acquire + runtime.Attach, "An ordinary scene re-attached (a dismount/remount cycle).");
            foreach (var mode in new[] { "Dialog", "Cutscene" })
            {
                TestRunner.Equal(MountedGameModeDisposition.PreserveScriptedScene, MountedGameModePolicy.Classify(mode), mode + " was not a preserved scene.");
                TestRunner.True(MountedGameModePolicy.CanRetainMountedRelationship(mode), mode + " cannot retain the relationship.");
                // Retaining the attachment never admits gameplay input.
                TestRunner.True(!MountedGameModePolicy.CanAdmitMountedAction(mode), mode + " admitted a mounted action.");
                TestRunner.True(!MountedGameModePolicy.CanQueueMountedAction(mode), mode + " queued a mounted action.");
                TestRunner.True(!MountedRetainedIntentPolicy.SuspendsForEvent(mode), mode + " still suspends at its mode boundary.");
                // A scene that hides a captured view while it holds the camera is not an invalidation.
                TestRunner.True(MountedViewActivityPolicy.IsAdmissible(MountedGameModeDisposition.PreserveScriptedScene, false, true, false, false),
                    mode + " invalidated a temporarily hidden view.");
            }
            // Ordinary returns to world play keep their exact view rule, and the world-map cutscene keeps its boundary.
            TestRunner.True(!MountedViewActivityPolicy.IsAdmissible(MountedGameModeDisposition.PreserveWorldInteraction, false, true, false, true),
                "Default accepted a hidden rider view after the scene.");
            TestRunner.Equal(MountedGameModeDisposition.CleanDismount, MountedGameModePolicy.Classify("CutsceneGlobalMap"), "The world-map cutscene escaped its boundary.");
            // Rest keeps its own established suspension.
            var rest = MountedPair(new CountingRuntime());
            ApplyModeBoundary(rest, "Rest");
            TestRunner.Equal(RelationshipState.Unmounted, rest.State, "Rest kept the attachment through camp translocation.");
        }

        private static MountedSceneCommandDisposition Command(bool rider = true, bool cutsceneFlag = true, bool sceneActive = true,
            bool controlled = false, bool player = false, bool ai = false, bool mod = false, bool combat = false, bool mounted = true, bool simulating = false) =>
            MountedSceneContinuityPolicy.DecideCommand(mounted, rider, !rider,
                MountedSceneContinuityPolicy.IsScriptedCommand(cutsceneFlag, sceneActive, controlled, player, ai, mod), combat, simulating);

        private static void ScriptedCommandsHandOff()
        {
            // CommandMoveParty: the rider's UnitMoveTo runs first (party characters precede pets), then the horse's.
            TestRunner.Equal(MountedSceneCommandDisposition.HandOffRider, Command(), "A cutscene UnitMoveTo on the rider was not handed to staging.");
            TestRunner.Equal(MountedSceneCommandDisposition.KeepAttached, Command(rider: false), "A cutscene move of the mount dismounted its rider.");
            // Dialog/script actions (RunCommand, AttackCommand) carry no cutscene flag but run inside a scene.
            TestRunner.Equal(MountedSceneCommandDisposition.HandOffRider, Command(cutsceneFlag: false), "A dialog action's rider command was not handed off.");
            // A background cutscene in Default mode is recognised by its native flag or by its control of the rider.
            TestRunner.Equal(MountedSceneCommandDisposition.HandOffRider, Command(sceneActive: false), "A background cutscene command was missed.");
            TestRunner.Equal(MountedSceneCommandDisposition.HandOffRider, Command(cutsceneFlag: false, sceneActive: false, controlled: true),
                "A command on a rider controlled by a background cutscene was missed.");
            // Ordinary play, the player's own orders, AI and this mod's own commands are never scene staging.
            TestRunner.Equal(MountedSceneCommandDisposition.NotScripted, Command(cutsceneFlag: false, sceneActive: false), "An ordinary command was treated as staging.");
            TestRunner.Equal(MountedSceneCommandDisposition.NotScripted, Command(player: true), "A player order was treated as staging.");
            TestRunner.Equal(MountedSceneCommandDisposition.NotScripted, Command(ai: true), "An AI order was treated as staging.");
            TestRunner.Equal(MountedSceneCommandDisposition.NotScripted, Command(mod: true), "A pair-owned command was treated as staging.");
            TestRunner.Equal(MountedSceneCommandDisposition.NotScripted, Command(simulating: true), "A native cursor prediction was treated as staging.");

            // The handoff is the established suspension: the real coordinator releases authority once, for EventSuspension.
            var runtime = new CountingRuntime();
            var pair = MountedPair(runtime);
            TestRunner.True(pair.Dismount(CleanupTrigger.EventSuspension).Succeeded, "The staging handoff did not suspend cleanly.");
            TestRunner.Equal(1, runtime.ReleaseAuthority, "The handoff did not release movement authority exactly once.");
            TestRunner.Equal(CleanupTrigger.EventSuspension, runtime.Triggers[0], "The handoff was not an event suspension.");
            TestRunner.True(MountedCleanupFeedbackPolicy.Describe(CleanupTrigger.EventSuspension).Contains("remounts automatically"),
                "The handoff feedback does not say the pair remounts automatically.");
        }

        private static MountedRetainedPairDecision Pair(bool staged = false, float separation = 1f, bool sameArea = true, string refusal = null,
            bool expired = false, bool permanent = false, bool already = false, bool resolved = true,
            MountedRegroupState regroup = MountedRegroupState.Unavailable) =>
            MountedRetainedIntentPolicy.DecidePair(resolved, true, already, permanent, refusal, separation,
                MountedRetainedIntentPolicy.MaximumSeparationMetres(MountedRetainedIntentKind.ScriptedEvent), sameArea, expired, staged, regroup);

        private static MountedRetainedIntentDecision Gate(string mode = "Default", bool combat = false, bool sceneOnStack = false,
            MountedRetainedIntentKind kind = MountedRetainedIntentKind.ScriptedEvent) =>
            MountedRetainedIntentPolicy.Decide(kind, true, combat, false, mode, sceneOnStack, false, false, false);

        private static void StagingRestoresAfterRelease()
        {
            var runtime = new CountingRuntime();
            var pair = MountedPair(runtime);
            pair.Dismount(CleanupTrigger.EventSuspension);
            // While the scene plays: held at the custody gate, and the pair itself waits while staging is owned.
            TestRunner.Equal(MountedRetainedIntentDecision.Hold, Gate("Cutscene", sceneOnStack: true), "Restoration ran inside the scene.");
            TestRunner.Equal(MountedRetainedPairDecision.Retry, Pair(staged: true, separation: 15f), "A pair still owned by the scene was judged on its separation.");
            // Restoration needs Default: a candidate built inside the scene refuses cleanly, so nothing attaches mid-scene.
            var mid = pair.RestoreSaved(Candidate("Cutscene"));
            TestRunner.True(!mid.Succeeded && pair.State == RelationshipState.Unmounted && runtime.Acquire == 0, "A pair re-attached while the scene owned the camera.");
            // Released in ordinary play, close together: restored through saved restoration, never the Mount admission.
            TestRunner.Equal(MountedRetainedIntentDecision.Restore, Gate(), "Ordinary play after the scene did not restore.");
            TestRunner.Equal(MountedRetainedPairDecision.Restore, Pair(separation: 2f), "A released pair beside its mount did not restore.");
            TestRunner.True(pair.RestoreSaved(Candidate("Default")).Succeeded && pair.State == RelationshipState.Mounted, "The released pair did not restore.");
            TestRunner.Equal(1, runtime.Acquire, "Restoration acquired movement authority other than exactly once.");
            TestRunner.True(pair.Mount(Candidate("Default"), MountedRelationshipAdmission.SavedRestore).Errors.Count != 0,
                "Saved restoration was reachable through the public Mount admission.");
            // Respecting the final placement: a mount left behind walks to its rider; a rider is never snapped back.
            TestRunner.Equal(MountedRetainedPairDecision.Regroup, Pair(separation: 12f, regroup: MountedRegroupState.Available), "A reachable separation did not regroup.");
            TestRunner.Equal(MountedRetainedPairDecision.Retry, Pair(separation: 9f, regroup: MountedRegroupState.InProgress), "A regroup in progress was abandoned.");
            TestRunner.Equal(MountedRetainedPairDecision.Restore, Pair(separation: 1.4f, regroup: MountedRegroupState.InProgress), "An arrived regroup did not restore.");
            TestRunner.Equal(MountedRetainedPairDecision.Drop, Pair(separation: 12f, regroup: MountedRegroupState.Exhausted), "An exhausted regroup snapped the pair together.");
            TestRunner.Equal(MountedRetainedPairDecision.Drop, Pair(separation: 45f, regroup: MountedRegroupState.Available), "A distant rider was regrouped beyond the bound.");
            TestRunner.Equal(MountedRetainedPairDecision.Drop, Pair(separation: 12f, sameArea: false, regroup: MountedRegroupState.Available), "A regroup was attempted across a navigation boundary.");
        }

        private static void DialogueIntoCombat()
        {
            // Mounted exploration -> hostile dialog -> the encounter begins in Default: the pair was never released.
            var runtime = new CountingRuntime();
            var pair = MountedPair(runtime);
            foreach (var mode in new[] { "Dialog", "Default" }) ApplyModeBoundary(pair, mode);
            TestRunner.Equal(RelationshipState.Mounted, pair.State, "The pair did not enter the encounter mounted.");
            TestRunner.Equal(0, runtime.Acquire + runtime.Attach + runtime.ReleaseAuthority, "The dialog-to-combat transition re-ran an attachment step.");
            // A pair a scene had to stage and that is still staged when the fight begins keeps its intent (never adopted
            // into the encounter, never a Mount), stops any regroup walk, and restores once the fight is over.
            TestRunner.Equal(MountedRetainedIntentDecision.Hold, Gate(combat: true), "Combat cancelled a staged pair's mounted intent.");
            TestRunner.Equal(MountedRegroupState.Unavailable,
                MountedSceneContinuityPolicy.RegroupAvailability(MountedRetainedIntentKind.ScriptedEvent, true, 0, false), "A regroup walk was offered during an encounter.");
            TestRunner.Equal(MountedRetainedIntentDecision.Restore, Gate(), "A staged pair did not restore after the encounter.");
            // The hold is not a scene-mode artefact: it applies in Default with the party fighting.
            TestRunner.Equal(MountedRetainedIntentDecision.Hold, Gate("Default", combat: true), "A staged pair was restored into a running encounter.");
        }

        private static void SceneDuringEncounter()
        {
            var runtime = new CountingRuntime();
            var pair = MountedPair(runtime);
            // Turn-based combat continues in Cutscene mode (native TurnBasedCombatController is registered for it).
            foreach (var mode in new[] { "Cutscene", "Dialog", "Default" }) ApplyModeBoundary(pair, mode);
            TestRunner.Equal(RelationshipState.Mounted, pair.State, "A scene during the encounter dismounted the pair.");
            TestRunner.Equal(0, runtime.Acquire + runtime.Attach + runtime.ReleaseAuthority + runtime.ReleasePresentation,
                "A scene during the encounter touched the attachment (and with it paired participation).");
            TestRunner.Equal(MountedSceneCommandDisposition.KeepAttachedInCombat, Command(combat: true),
                "A scripted rider command during an encounter handed the pair off (restoration would need a native participation decision).");
            TestRunner.Equal(MountedSceneInGameDisposition.Validate,
                MountedSceneContinuityPolicy.DecideInGameChange(true, true, true, true, true, true), "An in-combat removal was retained as staging.");
            TestRunner.True(!MountedGameModePolicy.CanAdmitMountedAction("Cutscene") && !MountedGameModePolicy.CanQueueMountedAction("Dialog"),
                "A scene during the encounter admitted mounted actions.");
        }

        private static bool Owned(bool controlled = false, bool cutsceneCommand = false, bool inGame = true, bool sceneActive = false) =>
            MountedSceneContinuityPolicy.ActorStagingOwned(controlled, cutsceneCommand, inGame, sceneActive);

        private static void NestedSkipAbort()
        {
            // Nested: an outer scene and an inner one both control the rider; readiness is the native control itself.
            TestRunner.True(Owned(controlled: true), "A rider controlled by a playing cutscene was ready.");
            // A brief intermediate Default while a cutscene-made order is still running keeps the pair waiting.
            TestRunner.True(Owned(cutsceneCommand: true), "An intermediate Default restored while a cutscene order still ran.");
            TestRunner.Equal(MountedRetainedPairDecision.Retry, Pair(staged: true), "An intermediate Default restored a still-staged pair.");
            // Skip or abort: native Stop/ForceStop released control and interrupted the order; no paired end callback.
            TestRunner.True(!Owned(), "A skipped scene left the pair owned without any native owner.");
            TestRunner.Equal(MountedRetainedPairDecision.Restore, Pair(), "A skipped scene stranded the pair.");
            // A scene that took the rider out of the game owns it only while a scene is active.
            TestRunner.True(Owned(inGame: false, sceneActive: true), "A rider hidden by a playing scene was ready.");
            TestRunner.True(!Owned(inGame: false, sceneActive: false), "A unit removed outside any scene was held as staging.");
            TestRunner.True(MountedSceneContinuityPolicy.SceneActive(true, false, false) && MountedSceneContinuityPolicy.SceneActive(false, true, false) &&
                MountedSceneContinuityPolicy.SceneActive(false, false, true) && !MountedSceneContinuityPolicy.SceneActive(false, false, false),
                "The scene predicate does not follow the native lock and mode stack.");
            // Never twice: once restored the pair is in a relationship, so a later pass drops the duplicate.
            var runtime = new CountingRuntime();
            var pair = MountedPair(runtime);
            pair.Dismount(CleanupTrigger.EventSuspension);
            TestRunner.True(pair.RestoreSaved(Candidate("Default")).Succeeded, "The first restoration failed.");
            TestRunner.Equal(MountedRetainedPairDecision.Drop, Pair(already: true), "A second restoration of the same pair was admitted.");
            TestRunner.True(!pair.RestoreSaved(Candidate("Default")).Succeeded && runtime.Acquire == 1, "A duplicate restoration attached twice.");
        }

        private static void UnmountedInvalidatedMultiplePairs()
        {
            // An unmounted pair is never touched by a scene: no handoff, no retained intent, no unsolicited mounting.
            TestRunner.Equal(MountedSceneCommandDisposition.NotScripted, Command(mounted: false), "A scene command on an unmounted rider was handed off.");
            TestRunner.Equal(MountedSceneInGameDisposition.Validate,
                MountedSceneContinuityPolicy.DecideInGameChange(false, true, true, true, true, false), "An unmounted pair was captured as staging.");
            // Invalidation is honoured before staging: death, unconsciousness or a changed companion relationship.
            TestRunner.Equal(MountedSceneInGameDisposition.Validate,
                MountedSceneContinuityPolicy.DecideInGameChange(true, true, true, false, true, false), "A dead or unconscious member was retained as staging.");
            TestRunner.Equal(MountedSceneInGameDisposition.Validate,
                MountedSceneContinuityPolicy.DecideInGameChange(true, true, true, true, false, false), "A changed pairing was retained as staging.");
            TestRunner.Equal(MountedSceneInGameDisposition.HandOff,
                MountedSceneContinuityPolicy.DecideInGameChange(true, true, true, true, true, false), "A scene's temporary removal was treated as invalidation.");
            TestRunner.Equal(MountedRetainedPairDecision.Drop, Pair(staged: true, permanent: true), "A dead pair waiting on a scene was kept (never resurrect).");
            TestRunner.Equal(MountedRetainedPairDecision.Drop, Pair(staged: true, resolved: false), "A removed companion waiting on a scene was kept.");
            // Two pairs: A is still staged, B was never handed off. Each keeps its own state and decision.
            var runtimeA = new CountingRuntime(); var a = MountedPair(runtimeA);
            var runtimeB = new CountingRuntime(); var b = MountedPair(runtimeB);
            a.Dismount(CleanupTrigger.EventSuspension);
            TestRunner.Equal(RelationshipState.Mounted, b.State, "Handing pair A off dismounted pair B.");
            TestRunner.Equal(0, runtimeB.ReleaseAuthority, "Pair B's attachment was released by pair A's staging.");
            TestRunner.Equal(MountedRetainedPairDecision.Retry, Pair(staged: true), "Staged pair A did not wait.");
            TestRunner.Equal(MountedRetainedPairDecision.Restore, Pair(separation: 2f), "A released pair was blocked by another pair's staging.");
        }

        private static void RegroupBounds()
        {
            const float max = MountedRetainedIntentPolicy.MaximumRestoreSeparationMetres;
            TestRunner.Equal(MountedRegroupState.Available, MountedSceneContinuityPolicy.RegroupAvailability(MountedRetainedIntentKind.ScriptedEvent, false, 0, false), "A first regroup was refused.");
            TestRunner.Equal(MountedRegroupState.Available, MountedSceneContinuityPolicy.RegroupAvailability(MountedRetainedIntentKind.ScriptedEvent, false, 1, false), "A replaced first walk could not retry once.");
            TestRunner.Equal(MountedRegroupState.Exhausted, MountedSceneContinuityPolicy.RegroupAvailability(MountedRetainedIntentKind.ScriptedEvent, false,
                MountedSceneContinuityPolicy.RegroupMaximumAttempts, false), "Regrouping was unbounded.");
            TestRunner.Equal(MountedRegroupState.InProgress, MountedSceneContinuityPolicy.RegroupAvailability(MountedRetainedIntentKind.ScriptedEvent, false, 1, true), "A live walk was not in progress.");
            foreach (var kind in new[] { MountedRetainedIntentKind.Camp, MountedRetainedIntentKind.WorldMapTravel })
                TestRunner.Equal(MountedRegroupState.Unavailable, MountedSceneContinuityPolicy.RegroupAvailability(kind, false, 0, false), kind + " custody regrouped.");
            TestRunner.Equal(MountedRegroupStep.Wait, MountedSceneContinuityPolicy.StepRegroup(true, 3f, 10f, max), "A walking mount was abandoned early.");
            TestRunner.Equal(MountedRegroupStep.Arrived, MountedSceneContinuityPolicy.StepRegroup(true, 3f, 4f, max), "An arrived mount was not recognised.");
            TestRunner.Equal(MountedRegroupStep.Abandon, MountedSceneContinuityPolicy.StepRegroup(true, MountedSceneContinuityPolicy.RegroupDeadlineSeconds + 0.1f, 10f, max),
                "A walk past its deadline continued.");
            TestRunner.Equal(MountedRegroupStep.Abandon, MountedSceneContinuityPolicy.StepRegroup(false, 3f, 10f, max), "A replaced or failed walk was waited on.");
            TestRunner.Equal(MountedRegroupStep.Abandon, MountedSceneContinuityPolicy.StepRegroup(true, 3f, float.NaN, max), "A non-finite separation was waited on.");
            TestRunner.True(MountedSceneContinuityPolicy.RegroupMaximumMetres > max && MountedSceneContinuityPolicy.RegroupApproachRadiusMetres < max,
                "The regroup bound does not lead back inside the restore separation.");
            TestRunner.Equal(6f, max, "The restore separation itself was widened instead of regrouping.");
        }
    }
}
