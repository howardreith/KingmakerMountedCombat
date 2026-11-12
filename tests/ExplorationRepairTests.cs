using System;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // Exploration repair sprint (2026-11-12): retained mounted intent across scripted events, camp and world-map
    // travel, and the mounted item-familiar follow rule. Domain decisions only; native acceptance is separate.
    internal static class ExplorationRepairTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("exploration E1/C1 only native scripted-event modes suspend with retained intent; other boundaries keep the clean dismount", EventModesSuspend);
            runner.Run("exploration issue 7 the world map is recognised only by its native global-map UI scene", WorldMapRecognition);
            runner.Run("exploration E3 restoration waits for settled ordinary play and cancels for a changed world or combat", RestorationGate);
            runner.Run("exploration E3 a retained pair restores once eligible, retries while transient and drops when invalidated or separated", PairDecision);
            runner.Run("exploration issue 7 a carried pair the destination refuses stays dismounted on its own", CarriedArrival);
            runner.Run("exploration P1 a mounted rider's familiar follows the mount's path end or ground position under the native leash", FamiliarFollow);
            runner.Run("exploration event suspension feedback says the pair remounts automatically", SuspensionFeedback);
        }

        private static void EventModesSuspend()
        {
            foreach (var mode in new[] { "Dialog", "Cutscene", "Rest" })
            {
                TestRunner.True(MountedRetainedIntentPolicy.SuspendsForEvent(mode), mode + " did not retain the pair's mounted intent.");
                // The relationship itself still may not stay attached through the event: the attachment is suspended.
                TestRunner.Equal(MountedGameModeDisposition.CleanDismount, MountedGameModePolicy.Classify(mode),
                    mode + " must not keep the physical attachment through a native event that can move or hide units.");
            }
            foreach (var mode in new[] { "Default", "Pause", "FullScreenUi", "EscMode", "None", "GlobalMap", "Kingdom", "KingdomSettlement",
                "CutsceneGlobalMap", "GameOver", "BugReport", "dialog", "" })
                TestRunner.True(!MountedRetainedIntentPolicy.SuspendsForEvent(mode), mode + " was treated as a retained scripted event.");
        }

        private static void WorldMapRecognition()
        {
            TestRunner.True(MountedRetainedIntentPolicy.IsWorldMapArea(true, "UI_GlobalMap", "UI_GlobalMap"), "The global map area was not recognised.");
            TestRunner.True(!MountedRetainedIntentPolicy.IsWorldMapArea(false, "UI_GlobalMap", "UI_GlobalMap"), "An area without a custom UI is a local area.");
            TestRunner.True(!MountedRetainedIntentPolicy.IsWorldMapArea(true, "UI_Kingdom", "UI_GlobalMap"), "Another custom UI scene is not the world map.");
            TestRunner.True(!MountedRetainedIntentPolicy.IsWorldMapArea(true, null, "UI_GlobalMap"), "A missing scene name is not the world map.");
        }

        private static void RestorationGate()
        {
            MountedRetainedIntentDecision Decide(bool sameWorld = true, bool combat = false, bool worldMap = false, string mode = "Default",
                bool eventMode = false, bool loading = false, bool boundary = false, bool camp = false) =>
                MountedRetainedIntentPolicy.Decide(sameWorld, combat, worldMap, mode, eventMode, loading, boundary, camp);
            TestRunner.Equal(MountedRetainedIntentDecision.Restore, Decide(), "Settled ordinary play did not restore.");
            TestRunner.Equal(MountedRetainedIntentDecision.Cancel, Decide(sameWorld: false), "A different world or save kept stale intent.");
            TestRunner.Equal(MountedRetainedIntentDecision.Cancel, Decide(combat: true), "Restoration adopted a native encounter (ambush or hostile dialog).");
            TestRunner.Equal(MountedRetainedIntentDecision.Hold, Decide(worldMap: true), "The world map restored a physical pair.");
            foreach (var mode in new[] { "Dialog", "Cutscene", "Rest", "Pause", "FullScreenUi", "EscMode", "None", "GlobalMap" })
                TestRunner.Equal(MountedRetainedIntentDecision.Hold, Decide(mode: mode), mode + " restored before ordinary play resumed.");
            TestRunner.Equal(MountedRetainedIntentDecision.Hold, Decide(eventMode: true), "A dialog nested under a cutscene (still on the mode stack) was treated as finished.");
            TestRunner.Equal(MountedRetainedIntentDecision.Hold, Decide(loading: true), "Restoration ran during native loading.");
            TestRunner.Equal(MountedRetainedIntentDecision.Hold, Decide(boundary: true), "Restoration ran over an owned save or area boundary.");
            TestRunner.Equal(MountedRetainedIntentDecision.Hold, Decide(camp: true), "Restoration ran while the camp assembly was still gathering the party.");
            TestRunner.Equal(MountedRetainedIntentDecision.Cancel, Decide(sameWorld: false, worldMap: true, mode: "Dialog"), "A changed world must cancel before anything holds.");
        }

        private static void PairDecision()
        {
            MountedRetainedPairDecision Pair(bool resolved = true, bool profile = true, bool already = false, bool permanent = false,
                string refusal = null, float separation = 1f, MountedRetainedIntentKind kind = MountedRetainedIntentKind.ScriptedEvent,
                bool sameArea = true, bool expired = false) =>
                MountedRetainedIntentPolicy.DecidePair(resolved, profile, already, permanent, refusal, separation,
                    MountedRetainedIntentPolicy.MaximumSeparationMetres(kind), sameArea, expired);
            TestRunner.Equal(MountedRetainedPairDecision.Restore, Pair(), "An eligible retained pair did not restore.");
            TestRunner.Equal(MountedRetainedPairDecision.Drop, Pair(resolved: false), "A removed companion was restored.");
            TestRunner.Equal(MountedRetainedPairDecision.Drop, Pair(profile: false), "A changed pairing or profile was restored.");
            TestRunner.Equal(MountedRetainedPairDecision.Drop, Pair(already: true), "A pair the player already remounted (or re-paired) was attached twice.");
            TestRunner.Equal(MountedRetainedPairDecision.Drop, Pair(permanent: true, refusal: "Both units must be alive and conscious."), "A dead or unconscious member was restored.");
            TestRunner.Equal(MountedRetainedPairDecision.Retry, Pair(refusal: "Both stock movement agents must be enabled before mounting."), "A transiently unattachable pair was dropped too early.");
            TestRunner.Equal(MountedRetainedPairDecision.Drop, Pair(refusal: "Both stock movement agents must be enabled before mounting.", expired: true), "A pair never became attachable but intent was kept forever.");
            TestRunner.Equal(MountedRetainedPairDecision.Drop, Pair(separation: 6.5f), "A scripted relocation that separated the pair snapped the rider across the scene.");
            TestRunner.Equal(MountedRetainedPairDecision.Restore, Pair(separation: 6.5f, kind: MountedRetainedIntentKind.Camp), "Separate native campsite slots refused the camp restoration.");
            TestRunner.Equal(MountedRetainedPairDecision.Drop, Pair(separation: 30f, kind: MountedRetainedIntentKind.Camp), "A pair outside one campsite was snapped together.");
            TestRunner.Equal(MountedRetainedPairDecision.Drop, Pair(sameArea: false), "A rider was seated across a navmesh boundary.");
            TestRunner.Equal(MountedRetainedPairDecision.Drop, Pair(separation: float.NaN), "A non-finite separation was accepted.");
        }

        private static void CarriedArrival()
        {
            TestRunner.True(MountedRetainedIntentPolicy.CarriedArrivalRestores(false, true, null), "An eligible carried pair did not restore on arrival.");
            TestRunner.True(!MountedRetainedIntentPolicy.CarriedArrivalRestores(true, true, null), "A carried pair was restored into an arrival encounter.");
            TestRunner.True(!MountedRetainedIntentPolicy.CarriedArrivalRestores(false, false, null), "A carried pair with a missing actor was restored.");
            TestRunner.True(!MountedRetainedIntentPolicy.CarriedArrivalRestores(false, true, "Mounting is available only in Default or Pause game mode."),
                "A carried pair the destination refuses was restored.");
        }

        private static void FamiliarFollow()
        {
            TestRunner.Equal(MountedFamiliarFollowSource.NativeMaster, MountedFamiliarFollowPolicy.Source(false, true), "An unmounted master's familiar left the native rule.");
            TestRunner.Equal(MountedFamiliarFollowSource.MountPathEnd, MountedFamiliarFollowPolicy.Source(true, true), "A moving mount's fixed path end was not followed.");
            TestRunner.Equal(MountedFamiliarFollowSource.MountGround, MountedFamiliarFollowPolicy.Source(true, false), "An idle mount's ground position was not followed.");
            TestRunner.True(!MountedFamiliarFollowPolicy.ShouldFollow(false, 9.9f), "The familiar started following inside the native leash.");
            TestRunner.True(MountedFamiliarFollowPolicy.ShouldFollow(false, 10.1f), "The familiar did not follow past the native leash.");
            TestRunner.True(MountedFamiliarFollowPolicy.ShouldFollow(true, 0f), "A following familiar stopped following before arriving.");
            TestRunner.True(MountedFamiliarFollowPolicy.MustLeaveOwnedAnchor(true, true), "A mounted rider's familiar stayed under the owned attachment anchor.");
            TestRunner.True(!MountedFamiliarFollowPolicy.MustLeaveOwnedAnchor(false, true), "An unmounted master's familiar parent was changed.");
            TestRunner.True(!MountedFamiliarFollowPolicy.MustLeaveOwnedAnchor(true, false), "A familiar with its own native parent was moved.");
        }

        private static void SuspensionFeedback()
        {
            var text = MountedCleanupFeedbackPolicy.Describe(CleanupTrigger.EventSuspension);
            TestRunner.True(text.IndexOf("remount", StringComparison.OrdinalIgnoreCase) >= 0, "Event suspension feedback did not say the pair remounts.");
            TestRunner.True((int)CleanupTrigger.EventSuspension > (int)CleanupTrigger.GameModeBoundary &&
                (int)CleanupTrigger.EventSuspension < (int)CleanupTrigger.ViewDetached, "The event suspension trigger changed cleanup priority order.");
        }
    }
}
