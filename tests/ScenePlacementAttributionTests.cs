using System;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // Known issues C02: rider-only scene placements of three metres or less; review R2 of b30c24f8 (the scene's final
    // placement stands).
    //
    // Production code executed here:
    // - MountedScenePlacementHold: the rider synchronizer's whole pending-placement state (hold, supersede, clear for a
    //   whole-pair placement, release, the per-sample decision with the unattributed distance rule and the frame budget);
    // - MountedSceneContinuityPolicy.ClassifyOperationWrite and PositionWritten (what an operation did to the pair);
    // - MountedSceneHandOffSequence (suspend, then reapply the held placement) and the real MountedRelationshipCoordinator.
    // Represented (cannot run offline): the native operations' direct Position writes and the capture around them
    // (MountedScenePlacementAttribution restated in Operation), the synchronizer writing the rider to the saddle when the
    // hold lets it (RiderMovementAgent), and the owner's poll (MountedRetainedIntent.ObserveScenePlacements: hand off at the
    // held placement, or release it when refused). Combat stands for the staging handoff's encounter refusal.
    internal static class ScenePlacementAttributionTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("known issues C02: the attribution decision takes only a scene operation's write of the rider without its mount; the unattributed rule is the unchanged distance rule", PolicyDecisions);
            runner.Run("known issues C02 (hold production; native write and poll represented): out of combat, an attributed rider placement at 0.5 m, 2 m, 3 m and 6 m keeps its endpoint; without attribution only the 6 m placement did", AttributedPlacementsKeepTheirEndpoint);
            runner.Run("known issues C02 (hold production): in combat the handoff is refused once, the rider returns to the saddle once and nothing oscillates afterwards", RefusedHandOffSettlesOnce);
            runner.Run("known issues C02 (hold production): ordinary small movement, a mount-only placement, a placement of both, one outside a scene and an unrelated unit never hand the rider off", NoAttributionControls);
            runner.Run("C02 review R2 (hold production): two rider placements before the poll hand off once, at the final endpoint and orientation; the reviewed first-wins hold went back to the first", TwoPlacementsBeforePoll);
            runner.Run("C02 review R2 (hold production): 6 m then 7 m keeps 7 m, attributed or not, before or during the hold, whether the poll or the synchronizer comes first", SixThenSevenMetres);
            runner.Run("C02 review R2 (hold production): a later whole-pair placement leaves no rider-only target to reapply; the pair stays together at the mount's placement", WholePairSupersedes);
            runner.Run("C02 review R2 (hold production): nested operations attribute the rider's position at the end of the outermost operation", NestedOperations);
            runner.Run("C02 review R2 (hold production): in combat two placements still end in one refusal and one return to the saddle", CombatRefusalAfterSupersession);
            runner.Run("C02 review R5: orientation comparison wraps (0 and 360, -90 and 270 are the same facing) and classification sees turns", OrientationDecisions);
            runner.Run("C02 review R5 (hold production): a second operation at the same endpoint with another orientation leaves P/120 pending and the handoff applies it once; the reviewed position-only attribution kept 30", SameEndpointFinalOrientation);
            runner.Run("C02 review R5 (hold production): a rotation-only whole-pair operation clears an obsolete rider-only hold; a mount-only turn keeps it; a rider-only turn alone starts nothing", TurnsAndWholePair);
            runner.Run("C02 review R5 (hold production): nested turns, a replaced mounting, combat, outside a scene and ordinary turning between operations", TurnControls);
        }

        private static void PolicyDecisions()
        {
            TestRunner.True(MountedSceneContinuityPolicy.AttributesRiderPlacement(true, true, true, false), "A scene operation's rider-only write is not attributed.");
            TestRunner.True(!MountedSceneContinuityPolicy.AttributesRiderPlacement(true, true, true, true), "A write of both rider and mount is attributed to the rider.");
            TestRunner.True(!MountedSceneContinuityPolicy.AttributesRiderPlacement(true, true, false, true), "A mount-only write is attributed to the rider.");
            TestRunner.True(!MountedSceneContinuityPolicy.AttributesRiderPlacement(true, false, true, false), "A write outside a scene is attributed.");
            TestRunner.True(!MountedSceneContinuityPolicy.AttributesRiderPlacement(false, true, true, false), "An unmounted rider is attributed.");
            TestRunner.Equal(MountedScenePlacementOperation.WholePair, MountedSceneContinuityPolicy.ClassifyOperationWrite(true, true, true, true), "A whole-pair write was not recognised.");
            TestRunner.Equal(MountedScenePlacementOperation.WholePair, MountedSceneContinuityPolicy.ClassifyOperationWrite(true, false, true, true), "A whole-pair write outside a scene was not recognised.");
            TestRunner.Equal(MountedScenePlacementOperation.None, MountedSceneContinuityPolicy.ClassifyOperationWrite(false, true, true, true), "An unmounted pair's write was classified.");
            TestRunner.True(MountedSceneContinuityPolicy.HoldsUnattributedPlacement(true, true, 3.5f, 3.5f) &&
                !MountedSceneContinuityPolicy.HoldsUnattributedPlacement(true, true, 3f, 3.5f) &&
                !MountedSceneContinuityPolicy.HoldsUnattributedPlacement(true, true, 3.5f, 2f) &&
                !MountedSceneContinuityPolicy.HoldsUnattributedPlacement(false, true, 6f, 6f) &&
                !MountedSceneContinuityPolicy.HoldsUnattributedPlacement(true, false, 6f, 6f), "The unattributed distance rule changed.");
            TestRunner.True(!MountedSceneContinuityPolicy.PositionWritten(0.000001f, 0, 0) && MountedSceneContinuityPolicy.PositionWritten(0.01f, 0, 0),
                "Position equality does not follow Unity's Vector3 equality.");
        }

        private static void AttributedPlacementsKeepTheirEndpoint()
        {
            foreach (var attribution in new[] { true, false })
                foreach (var distance in new[] { 0.5f, 2f, 3f, 6f })
                {
                    var scene = new Scene { Attribution = attribution };
                    scene.Frames(3, true);
                    scene.RiderOnlyPlacement(distance, 0.6f, 0.8f, 45f, true);
                    scene.Frames(40, true);
                    if (attribution || distance > MountedSceneContinuityPolicy.ScenePlacementMinimumMetres)
                    {
                        TestRunner.True(scene.KeptLastTarget && scene.HandOffs == 1 && scene.Outcome == MountedSceneHandOffOutcome.HandedOffPlacementPreserved,
                            "The scene's placement at " + distance + " m was not kept" + (attribution ? "" : " (the unattributed rule should keep a far one)") + ".");
                    }
                    else
                    {
                        // Before C02: an unattributed near placement cannot be told from lag; the rider returns to the saddle.
                        TestRunner.True(!scene.KeptLastTarget && scene.HandOffs == 0 && scene.AtSaddle,
                            "Setup (no attribution): the near placement at " + distance + " m should have been swallowed.");
                    }
                }
        }

        private static void RefusedHandOffSettlesOnce()
        {
            var scene = new Scene { Attribution = true, Combat = true };
            scene.Frames(3, true);
            scene.RiderOnlyPlacement(2f, 0.6f, 0.8f, 10f, true);
            scene.Frames(60, true);
            TestRunner.True(scene.HandOffs == 0 && scene.AtSaddle && scene.Refusals == 1, "A refused handoff did not end once, at the saddle.");
            TestRunner.True(scene.Hold.ObservedCount == 1 && scene.SaddleCorrections == 1, "The rider oscillated between the placement and the saddle.");
        }

        private static void NoAttributionControls()
        {
            // Ordinary synchronization lag: the rider is a little off the saddle, and no operation wrote it.
            var lag = new Scene { Attribution = true };
            lag.Frames(3, true);
            lag.Rider.X += 0.2f;
            lag.Frames(10, true);
            TestRunner.True(lag.Hold.ObservedCount == 0 && lag.HandOffs == 0 && lag.AtSaddle, "Ordinary movement was taken for a placement.");
            // A mount-only placement moves the saddle; the rider follows.
            var mountOnly = new Scene { Attribution = true };
            mountOnly.Frames(3, true);
            mountOnly.Operation(() => { mountOnly.Mount.X += 2f; }, true);
            mountOnly.Frames(10, true);
            TestRunner.True(mountOnly.Hold.ObservedCount == 0 && mountOnly.HandOffs == 0 && mountOnly.AtSaddle && Math.Abs(mountOnly.Rider.X - mountOnly.Mount.X) < 0.01f,
                "A mount-only placement handed the rider off.");
            // Both written: the pair stays together at the mount's placement, as before.
            var both = new Scene { Attribution = true };
            both.Frames(3, true);
            both.Operation(() => { both.Mount.X += 2f; both.Rider.X += 2.5f; }, true);
            both.Frames(10, true);
            TestRunner.True(both.Hold.ObservedCount == 0 && both.HandOffs == 0 && both.AtSaddle, "A placement of both handed the rider off.");
            // Outside a scene nothing is attributed.
            var outside = new Scene { Attribution = true };
            outside.Frames(3, false);
            outside.RiderOnlyPlacement(2f, 0.6f, 0.8f, 0f, false);
            outside.Frames(10, false);
            TestRunner.True(outside.Hold.ObservedCount == 0 && outside.HandOffs == 0 && outside.AtSaddle, "A placement outside a scene was attributed.");
            // An unrelated unit written by the operation: the pair's rider was not written.
            var unrelated = new Scene { Attribution = true };
            unrelated.Frames(3, true);
            var other = new Body { X = 10f };
            unrelated.Operation(() => { other.X += 1f; }, true);
            unrelated.Frames(10, true);
            TestRunner.True(unrelated.Hold.ObservedCount == 0 && unrelated.HandOffs == 0 && unrelated.AtSaddle, "An unrelated unit's placement handed the rider off.");
        }

        private static void TwoPlacementsBeforePoll()
        {
            foreach (var reviewed in new[] { false, true })
                foreach (var distances in new[] { new[] { 2f, 2.5f }, new[] { 0.5f, 3f } })
                {
                    var scene = new Scene { Attribution = true, ReviewedFirstWins = reviewed };
                    scene.Frames(3, true);
                    scene.RiderOnlyPlacement(distances[0], 0.6f, 0.8f, 30f, true);
                    var firstX = scene.TargetX; var firstZ = scene.TargetZ;
                    scene.RiderOnlyPlacement(distances[1], -0.8f, 0.6f, 120f, true);
                    scene.Frames(40, true);
                    if (!reviewed)
                        TestRunner.True(scene.HandOffs == 1 && scene.KeptLastTarget && Math.Abs(scene.Rider.O - 120f) < 0.01f &&
                            scene.Hold.SupersededCount == 1, "The final placement (" + distances[1] + " m, orientation 120) did not stand, or was handed off more than once.");
                    else
                        TestRunner.True(scene.HandOffs == 1 && Math.Abs(scene.Rider.X - firstX) < 0.1f && Math.Abs(scene.Rider.Z - firstZ) < 0.1f &&
                            Math.Abs(scene.Rider.O - 30f) < 0.01f, "Setup (reviewed first-wins hold): the rider should have been sent back to the first placement.");
                }
        }

        private static void SixThenSevenMetres()
        {
            // Attributed: both operations before the poll.
            var attributed = new Scene { Attribution = true };
            attributed.Frames(3, true);
            attributed.RiderOnlyPlacement(6f, 0.6f, 0.8f, 0f, true);
            attributed.RiderOnlyPlacement(7f, 0.6f, 0.8f, 0f, true);
            attributed.Frames(40, true);
            TestRunner.True(attributed.HandOffs == 1 && attributed.KeptLastTarget, "Attributed 6 m then 7 m did not keep 7 m.");
            // Unattributed, both before the synchronizer's next sample (the behaviour before C02): 7 m.
            var before = new Scene { Attribution = false };
            before.Frames(3, true);
            before.RiderOnlyPlacement(6f, 0.6f, 0.8f, 0f, true);
            before.RiderOnlyPlacement(7f, 0.6f, 0.8f, 0f, true);
            before.Frames(40, true);
            TestRunner.True(before.HandOffs == 1 && before.KeptLastTarget, "Unattributed 6 m then 7 m before a sample did not keep 7 m.");
            // Unattributed, the second write while the first is held (sampled, not yet polled), far from the first: 7 m.
            var during = new Scene { Attribution = false };
            during.Frames(3, true);
            during.RiderOnlyPlacement(6f, 0.6f, 0.8f, 0f, true);
            during.SynchronizerSample(true);
            TestRunner.True(during.Hold.Pending, "Setup: the far write was not held.");
            during.RiderOnlyPlacement(7f, -0.8f, 0.6f, 0f, true);
            during.Frames(40, true);
            TestRunner.True(during.HandOffs == 1 && during.KeptLastTarget, "An unattributed far write during the hold did not supersede the held one.");
            // A near unattributed write during the hold cannot be told from lag: the held placement stands (the 3 m rule).
            var near = new Scene { Attribution = false };
            near.Frames(3, true);
            near.RiderOnlyPlacement(6f, 0.6f, 0.8f, 0f, true);
            near.SynchronizerSample(true);
            var heldX = near.Rider.X; var heldZ = near.Rider.Z;
            near.RiderOnlyPlacement(7f, 0.6f, 0.8f, 0f, true);
            near.Frames(40, true);
            TestRunner.True(near.HandOffs == 1 && Math.Abs(near.Rider.X - heldX) < 0.1f && Math.Abs(near.Rider.Z - heldZ) < 0.1f,
                "An unattributed write 1 m from the held placement replaced it.");
        }

        private static void WholePairSupersedes()
        {
            foreach (var reviewed in new[] { false, true })
            {
                var scene = new Scene { Attribution = true, ReviewedFirstWins = reviewed };
                scene.Frames(3, true);
                scene.RiderOnlyPlacement(2f, 0.6f, 0.8f, 30f, true);
                var obsoleteX = scene.TargetX; var obsoleteZ = scene.TargetZ;
                // The whole pair placed together, 10 m away (the rider written beside the mount's new place).
                scene.Operation(() => { scene.Mount.X = 10f; scene.Mount.Z = 0f; scene.Rider.X = 10.4f; scene.Rider.Z = 0f; }, true);
                scene.Frames(40, true);
                var sentBack = Math.Abs(scene.Rider.X - obsoleteX) < 0.1f && Math.Abs(scene.Rider.Z - obsoleteZ) < 0.1f;
                if (!reviewed)
                    TestRunner.True(scene.HandOffs == 0 && !sentBack && scene.AtSaddle && Math.Abs(scene.Mount.X - 10f) < 0.001f && scene.Hold.ClearedCount == 1,
                        "A whole-pair placement left the obsolete rider-only target to be reapplied.");
                else
                    TestRunner.True(scene.HandOffs == 1 && sentBack, "Setup (reviewed hold): the obsolete rider-only target should have been reapplied.");
            }
        }

        private static void NestedOperations()
        {
            // The outer operation writes B after an inner one wrote A: B, the position at the end of the outermost operation.
            var later = new Scene { Attribution = true };
            later.Frames(3, true);
            later.Operation(() =>
            {
                later.RiderOnlyPlacement(2f, 0.6f, 0.8f, 10f, true);
                later.Rider.X = later.Mount.X - 1.5f; later.Rider.Z = later.Mount.Z; later.Rider.O = 200f;
                later.TargetX = later.Rider.X; later.TargetZ = later.Rider.Z;
            }, true);
            later.Frames(40, true);
            TestRunner.True(later.HandOffs == 1 && later.KeptLastTarget && Math.Abs(later.Rider.O - 200f) < 0.01f, "The outer operation's final write did not stand.");
            // The outer operation writes B before an inner one writes A: A is where the rider stands at the end, and stands.
            var earlier = new Scene { Attribution = true };
            earlier.Frames(3, true);
            earlier.Operation(() =>
            {
                earlier.Rider.X = earlier.Mount.X - 1.5f; earlier.Rider.Z = earlier.Mount.Z; earlier.Rider.O = 200f;
                earlier.RiderOnlyPlacement(2f, 0.6f, 0.8f, 10f, true);
            }, true);
            earlier.Frames(40, true);
            TestRunner.True(earlier.HandOffs == 1 && earlier.KeptLastTarget && Math.Abs(earlier.Rider.O - 10f) < 0.01f, "The inner operation's final write did not stand.");
        }

        private static void CombatRefusalAfterSupersession()
        {
            var scene = new Scene { Attribution = true, Combat = true };
            scene.Frames(3, true);
            scene.RiderOnlyPlacement(2f, 0.6f, 0.8f, 10f, true);
            scene.RiderOnlyPlacement(2.5f, -0.8f, 0.6f, 20f, true);
            scene.Frames(60, true);
            TestRunner.True(scene.HandOffs == 0 && scene.Refusals == 1 && scene.SaddleCorrections == 1 && scene.AtSaddle,
                "Two placements in combat did not end in one refusal and one return to the saddle.");
        }

        private static void OrientationDecisions()
        {
            TestRunner.True(!MountedSceneContinuityPolicy.OrientationWritten(0f, 360f) && !MountedSceneContinuityPolicy.OrientationWritten(-90f, 270f) &&
                !MountedSceneContinuityPolicy.OrientationWritten(30f, 30.0005f), "The same facing was taken for a turn.");
            TestRunner.True(MountedSceneContinuityPolicy.OrientationWritten(350f, 10f) && MountedSceneContinuityPolicy.OrientationWritten(30f, 120f) &&
                MountedSceneContinuityPolicy.OrientationWritten(0.1f, 0.7f), "A turn was missed.");
            TestRunner.True(Math.Abs(MountedSceneContinuityPolicy.OrientationDeltaDegrees(350f, 10f) - 20f) < 0.001f &&
                Math.Abs(MountedSceneContinuityPolicy.OrientationDeltaDegrees(10f, 350f) + 20f) < 0.001f, "The wrapped difference is not the shortest one.");
            TestRunner.Equal(MountedScenePlacementOperation.RiderTurn, MountedSceneContinuityPolicy.ClassifyOperationWrite(true, true, false, true, false, false), "A rider-only turn was not recognised.");
            TestRunner.Equal(MountedScenePlacementOperation.WholePair, MountedSceneContinuityPolicy.ClassifyOperationWrite(true, true, false, true, false, true), "A turn of both was not a whole-pair operation.");
            TestRunner.Equal(MountedScenePlacementOperation.None, MountedSceneContinuityPolicy.ClassifyOperationWrite(true, true, false, false, false, true), "A mount-only turn touched the rider.");
            TestRunner.Equal(MountedScenePlacementOperation.None, MountedSceneContinuityPolicy.ClassifyOperationWrite(true, false, false, true, false, false), "A turn outside a scene was classified.");
            TestRunner.Equal(MountedScenePlacementOperation.RiderPlacement, MountedSceneContinuityPolicy.ClassifyOperationWrite(true, true, true, true, false, false), "A move and turn of the rider was not a placement.");
        }

        private static void SameEndpointFinalOrientation()
        {
            foreach (var reviewed in new[] { false, true })
            {
                var scene = new Scene { Attribution = true, ReviewedPositionOnly = reviewed };
                scene.Frames(3, true);
                scene.RiderOnlyPlacement(2f, 0.6f, 0.8f, 30f, true);
                // Placement B: the same endpoint P (TranslocateUnit to another locator at P, copy rotation), orientation 120.
                scene.Operation(() => { scene.Rider.X = scene.TargetX; scene.Rider.Z = scene.TargetZ; scene.Rider.O = 120f; }, true);
                scene.Frames(40, true);
                if (!reviewed)
                    TestRunner.True(scene.HandOffs == 1 && scene.KeptLastTarget && Math.Abs(scene.Rider.O - 120f) < 0.01f,
                        "The same-endpoint placement's final orientation (120) was not the one applied once.");
                else
                    TestRunner.True(scene.HandOffs == 1 && scene.KeptLastTarget && Math.Abs(scene.Rider.O - 30f) < 0.01f,
                        "Setup (reviewed position-only attribution): the handoff should have reapplied orientation 30.");
            }
        }

        private static void TurnsAndWholePair()
        {
            // A whole-pair operation that only turns both units (CommandTeleportParty to locators where they already stand).
            var whole = new Scene { Attribution = true };
            whole.Frames(3, true);
            whole.RiderOnlyPlacement(2f, 0.6f, 0.8f, 30f, true);
            whole.Operation(() => { whole.Rider.O = 200f; whole.Mount.O = 200f; }, true);
            whole.Frames(40, true);
            TestRunner.True(whole.HandOffs == 0 && whole.AtSaddle && whole.Hold.ClearedCount == 1, "A rotation-only whole-pair operation left the rider-only target to reapply.");
            // A mount-only turn keeps the pending rider placement as it was.
            var mountTurn = new Scene { Attribution = true };
            mountTurn.Frames(3, true);
            mountTurn.RiderOnlyPlacement(2f, 0.6f, 0.8f, 30f, true);
            mountTurn.Operation(() => { mountTurn.Mount.O = 90f; }, true);
            mountTurn.Frames(40, true);
            TestRunner.True(mountTurn.HandOffs == 1 && mountTurn.KeptLastTarget && Math.Abs(mountTurn.Rider.O - 30f) < 0.01f, "A mount-only turn changed the rider's pending placement.");
            // A rider-only turn with nothing pending starts nothing: the seated rider's facing follows the saddle.
            var riderTurn = new Scene { Attribution = true };
            riderTurn.Frames(3, true);
            riderTurn.Operation(() => { riderTurn.Rider.O = 75f; }, true);
            riderTurn.Frames(40, true);
            TestRunner.True(riderTurn.HandOffs == 0 && riderTurn.AtSaddle && riderTurn.Hold.ObservedCount == 0, "A rider-only turn on its own started a handoff.");
        }

        private static void TurnControls()
        {
            // Nested: the inner operation places P/30; the outer one then turns the rider in place to 120.
            var nested = new Scene { Attribution = true };
            nested.Frames(3, true);
            nested.Operation(() =>
            {
                nested.RiderOnlyPlacement(2f, 0.6f, 0.8f, 30f, true);
                nested.Rider.O = 120f;
            }, true);
            nested.Frames(40, true);
            TestRunner.True(nested.HandOffs == 1 && nested.KeptLastTarget && Math.Abs(nested.Rider.O - 120f) < 0.01f, "The outer operation's final turn did not stand.");
            // A mounting replaced inside the operation (dismounted and mounted again): nothing is attributed to the new one.
            var replaced = new Scene { Attribution = true };
            replaced.Frames(3, true);
            replaced.Operation(() => { replaced.Remount(); replaced.Rider.X = replaced.Mount.X + 2f; replaced.Rider.O = 50f; }, true);
            replaced.Frames(40, true);
            TestRunner.True(replaced.Hold.ObservedCount == 0 && replaced.HandOffs == 0, "A placement was attributed to a mounting made inside the operation.");
            // Combat: a placement then a turn, one refusal and one return to the saddle.
            var combat = new Scene { Attribution = true, Combat = true };
            combat.Frames(3, true);
            combat.RiderOnlyPlacement(2f, 0.6f, 0.8f, 30f, true);
            combat.Operation(() => { combat.Rider.O = 120f; }, true);
            combat.Frames(60, true);
            TestRunner.True(combat.HandOffs == 0 && combat.Refusals == 1 && combat.SaddleCorrections == 1 && combat.AtSaddle, "A turn in combat did not keep the bounded refusal.");
            // Outside a scene a turn is not attributed.
            var outside = new Scene { Attribution = true };
            outside.Frames(3, false);
            outside.Operation(() => { outside.Rider.O = 120f; }, false);
            outside.Frames(10, false);
            TestRunner.True(outside.Hold.ObservedCount == 0 && outside.HandOffs == 0, "A turn outside a scene was attributed.");
            // Ordinary turning (animation, the saddle's yaw, a targeting turn) between operations is never an operation.
            var ordinary = new Scene { Attribution = true };
            ordinary.Frames(3, true);
            ordinary.RiderOnlyPlacement(2f, 0.6f, 0.8f, 30f, true);
            ordinary.Rider.O = 310f; ordinary.Mount.O = 45f;
            ordinary.Frames(40, true);
            TestRunner.True(ordinary.HandOffs == 1 && ordinary.KeptLastTarget && Math.Abs(ordinary.Rider.O - 30f) < 0.01f,
                "An ordinary turn between operations changed the scene's placement.");
        }

        // ---- represented engine and orchestration around the production hold ----
        private sealed class Body { internal float X, Z, O; }

        private sealed class ReleasingRuntime : IMountedPairRuntime
        {
            internal Body Rider, Mount;
            public void AcquireMovementAuthority(MountedPair pair) { }
            public void AttachPresentation(MountedPair pair) { }
            public void RestorePresentation(MountedPair pair) { }
            public void RestoreMovementAuthority(MountedPair pair, CleanupTrigger trigger)
            {
                // Like the real cleanup: an EventSuspension places the rider beside the mount and clears the references.
                if (trigger == CleanupTrigger.EventSuspension) { Rider.X = Mount.X + 1.2f; Rider.Z = Mount.Z; }
                Rider = null; Mount = null;
            }
        }

        private sealed class Scene
        {
            // Attribution false: no native operation is attributed (the behaviour before C02). ReviewedFirstWins: the
            // reviewed b30c24f8 attribution, transcribed (not production) only to show what R2 changes: a pending placement
            // rejects a later one, and a whole-pair write clears nothing.
            public bool Attribution, Combat, ReviewedFirstWins, ReviewedPositionOnly;
            public readonly Body Rider = new Body(), Mount = new Body();
            public readonly MountedScenePlacementHold Hold = new MountedScenePlacementHold();
            private readonly MountedRelationshipCoordinator coordinator;
            public float TargetX, TargetZ;
            public int HandOffs, Refusals, SaddleCorrections;
            public MountedSceneHandOffOutcome Outcome;
            private int frame, depth;
            private bool hasLastWrite;
            private float lastX, lastZ;

            public Scene()
            {
                runtime = new ReleasingRuntime { Rider = Rider, Mount = Mount };
                coordinator = new MountedRelationshipCoordinator(runtime);
                TestRunner.True(coordinator.Mount(Candidate()).Succeeded, "Setup: the pair did not mount.");
            }

            private readonly ReleasingRuntime runtime;

            private static MountedPairCandidate Candidate() => new MountedPairCandidate("rider-a", "horse-a")
            {
                RiderIsDirectlyControllable = true, MountIsDirectlyControllable = true, RiderIsAliveAndConscious = true, MountIsAliveAndConscious = true,
                ExactReciprocalCompanionRelationship = true, RiderSizeOrdinal = 4, MountSizeOrdinal = 5, RiderViewAndStockAgentAvailable = true,
                MountViewAndStockAgentAvailable = true, RiderStockAgentEnabled = true, MountStockAgentEnabled = true, RiderAgentOverrideAvailable = true,
                MountAgentOverrideAvailable = true, RiderIsExactlyMedium = true, SafeMovementMode = true
            };

            public bool AtSaddle => Math.Abs(Rider.X - Mount.X) < 0.001f && Math.Abs(Rider.Z - Mount.Z) < 0.001f;

            // A dismount and a new mounting of the same pair (a new mounting instance).
            public void Remount()
            {
                TestRunner.True(coordinator.Dismount(CleanupTrigger.Manual).Succeeded, "Setup: the dismount did not complete.");
                TestRunner.True(coordinator.Mount(Candidate()).Succeeded, "Setup: the remount did not complete.");
            }
            public bool KeptLastTarget => Math.Abs(Rider.X - TargetX) < 0.1f && Math.Abs(Rider.Z - TargetZ) < 0.1f;

            public void RiderOnlyPlacement(float distance, float dirX, float dirZ, float orientation, bool sceneActive)
            {
                var x = Mount.X + distance * dirX; var z = Mount.Z + distance * dirZ;
                Operation(() => { Rider.X = x; Rider.Z = z; Rider.O = orientation; }, sceneActive);
                TargetX = Rider.X; TargetZ = Rider.Z;
            }

            // A native scene placement operation: the product's capture before it and its attribution after it
            // (MountedScenePlacementAttribution), with the production classification and hold.
            public void Operation(Action writes, bool sceneActive)
            {
                float rx = Rider.X, rz = Rider.Z, ro = Rider.O, mx = Mount.X, mz = Mount.Z, mo = Mount.O;
                var instance = coordinator.ActivePair;
                depth++;
                try { writes(); } finally { depth--; }
                if (!Attribution) return;
                // MountedScenePlacementAttribution acts only on the mounting instance it captured.
                if (instance == null || !ReferenceEquals(coordinator.ActivePair, instance)) return;
                var mounted = coordinator.State == RelationshipState.Mounted;
                bool riderMoved = MountedSceneContinuityPolicy.PositionWritten(Rider.X - rx, 0, Rider.Z - rz),
                    mountMoved = MountedSceneContinuityPolicy.PositionWritten(Mount.X - mx, 0, Mount.Z - mz);
                // The reviewed 263ac3ea attribution compared positions only (transcribed, not production).
                var write = ReviewedPositionOnly
                    ? MountedSceneContinuityPolicy.ClassifyOperationWrite(mounted, sceneActive, riderMoved, mountMoved)
                    : MountedSceneContinuityPolicy.ClassifyOperationWrite(mounted, sceneActive, riderMoved,
                        MountedSceneContinuityPolicy.OrientationWritten(ro, Rider.O), mountMoved, MountedSceneContinuityPolicy.OrientationWritten(mo, Mount.O));
                if (write == MountedScenePlacementOperation.RiderTurn) { Hold.Reorient(Rider.O, frame); return; }
                if (ReviewedFirstWins)
                {
                    if (write == MountedScenePlacementOperation.RiderPlacement && !Hold.Pending) Hold.Attribute(Rider.X, 0, Rider.Z, Rider.O, frame);
                    return;
                }
                if (write == MountedScenePlacementOperation.WholePair) Hold.ClearForWholePair();
                else if (write == MountedScenePlacementOperation.RiderPlacement) Hold.Attribute(Rider.X, 0, Rider.Z, Rider.O, frame);
            }

            public void Frames(int count, bool sceneActive) { for (var i = 0; i < count; i++) Frame(sceneActive); }

            private void Frame(bool sceneActive)
            {
                frame++;
                if (coordinator.State != RelationshipState.Mounted) return;
                // The owner's poll (ObserveScenePlacements): hand the pair off at the held placement, or release it.
                if (Hold.Refresh(frame, sceneActive, Rider.X, 0, Rider.Z, Rider.O, Mount.X, Mount.Z))
                {
                    float x = Hold.X, z = Hold.Z, o = Hold.Orientation;
                    if (Combat)
                    {
                        Refusals++;
                        Hold.Release();
                    }
                    else
                    {
                        Outcome = MountedSceneHandOffSequence.Run(Rider,
                            () => coordinator.Dismount(CleanupTrigger.EventSuspension).Succeeded && coordinator.State == RelationshipState.Unmounted,
                            true, r => { r.X = x; r.Z = z; r.O = o; },
                            r => Math.Abs(r.X - x) < 0.1f && Math.Abs(r.Z - z) < 0.1f, out _);
                        if (MountedSceneHandOffSequence.IsHandedOff(Outcome)) { HandOffs++; return; }
                        Hold.Release();
                    }
                }
                SynchronizerSample(sceneActive);
            }

            // RiderMovementAgent's sample: the production hold decides; when it lets go, the rider is written to the saddle.
            public void SynchronizerSample(bool sceneActive)
            {
                if (Hold.Sample(frame, sceneActive, Rider.X, 0, Rider.Z, Rider.O, Mount.X, Mount.Z, hasLastWrite, lastX, lastZ)) return;
                if (!AtSaddle) SaddleCorrections++;
                Rider.X = Mount.X; Rider.Z = Mount.Z;
                hasLastWrite = true; lastX = Rider.X; lastZ = Rider.Z;
            }
        }
    }
}
