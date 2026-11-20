using System;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // Known issues C02: rider-only scene placements of three metres or less; review R2 of b30c24f8 (the scene's final
    // placement stands).
    //
    // Production code executed here:
    // - MountedScenePlacementAttributionCore.Apply: the attribution of one operation to every captured pair (since review
    //   R8/R9 of e1c0c7ad the model's operations go through it);
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
            runner.Run("C02 review R7 (core and hold production): with nothing pending, an exact rider-only turn to 75 degrees at the rider's position is handed off once at that position and orientation; the reviewed e1c0c7ad attribution discarded it", StandaloneRiderTurn);
            runner.Run("C02 review R7 (core and hold production): a standalone turn outside a scene, a mount-only turn, ordinary turning, a replaced mounting, combat, repeated and nested turns and wrapped orientations", StandaloneTurnControls);
            runner.Run("C02 review R8 (core production): a CommandTeleportParty proven to target both actors is whole-pair even when one actor was already at its authored transform; only-rider and only-mount targets are not; the reviewed attribution reapplied or replaced the obsolete target", WholePairByProvenTargets);
            runner.Run("C02 review R9 (core production): one operation over two pairs with a logger that throws on the first success still updates both pairs' holds; a healthy logger receives both", ThrowingSuccessLogger);
            runner.Run("C02 review R10 (ownership policy, core and hold production; native cutscene state represented): in Default play a live background player's timed-out CommandMoveUnit, rider-only CommandTeleportParty and standalone turn are each held and handed off once; the reviewed global-mode attribution swallowed all three", BackgroundSceneOwnership);
            runner.Run("C02 review R10: a null, finished, replaced or unrelated player, a direct invocation and a TranslocateUnit outside any cutscene scope are not owned; a TranslocateUnit inside a live (or starting) cutscene scope is", NonSceneControls);
            runner.Run("C02 review R10: ownership is captured at the operation's entry; a mode or player change inside the operation does not change it either way", OwnershipCapturedAtEntry);
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
            // A rider-only turn with nothing pending: review R7 (StandaloneRiderTurn) holds and hands it off.
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

        private static void StandaloneRiderTurn()
        {
            foreach (var reviewed in new[] { false, true })
            {
                var scene = new Scene { Attribution = true, ReviewedE1c0 = reviewed };
                scene.Frames(3, true);
                float x = scene.Rider.X, z = scene.Rider.Z;
                // An exact TranslocateUnit (copy rotation) to a locator at the rider's own position, orientation 75; mount unchanged.
                scene.Operation(() => { scene.Rider.X = x; scene.Rider.Z = z; scene.Rider.O = 75f; }, true);
                scene.TargetX = x; scene.TargetZ = z;
                scene.Frames(40, true);
                if (!reviewed)
                    TestRunner.True(scene.HandOffs == 1 && scene.KeptLastTarget && Math.Abs(scene.Rider.O - 75f) < 0.01f && scene.Hold.ObservedCount == 1,
                        "The exact rider-only turn was not handed off once at the rider's position and 75 degrees.");
                else
                    TestRunner.True(scene.HandOffs == 0 && !scene.Hold.Pending && scene.Hold.ObservedCount == 0,
                        "Setup (reviewed e1c0c7ad attribution): the standalone turn should have been discarded.");
            }
        }

        private static void StandaloneTurnControls()
        {
            Func<Scene> fresh = () => { var s = new Scene { Attribution = true }; s.Frames(3, true); return s; };
            // Outside a scene.
            var outside = new Scene { Attribution = true };
            outside.Frames(3, false);
            outside.Operation(() => { outside.Rider.O = 75f; }, false);
            outside.Frames(10, false);
            TestRunner.True(outside.HandOffs == 0 && outside.Hold.ObservedCount == 0, "A turn outside a scene was held.");
            // A mount-only turn.
            var mountTurn = fresh();
            mountTurn.Operation(() => { mountTurn.Mount.O = 75f; }, true);
            mountTurn.Frames(10, true);
            TestRunner.True(mountTurn.HandOffs == 0 && mountTurn.Hold.ObservedCount == 0, "A mount-only turn was held for the rider.");
            // Ordinary turning (animation, targeting, the saddle's yaw) outside any operation.
            var ordinary = fresh();
            ordinary.Rider.O = 75f; ordinary.Mount.O = 30f;
            ordinary.Frames(10, true);
            TestRunner.True(ordinary.HandOffs == 0 && ordinary.Hold.ObservedCount == 0, "Ordinary turning was taken for a scene placement.");
            // A mounting replaced inside the operation.
            var replaced = fresh();
            replaced.Operation(() => { replaced.Remount(); replaced.Rider.O = 75f; }, true);
            replaced.Frames(10, true);
            TestRunner.True(replaced.HandOffs == 0 && replaced.Hold.ObservedCount == 0, "A turn was attributed to a mounting made inside the operation.");
            // Combat: held, refused once, released once, never again.
            var combat = new Scene { Attribution = true, Combat = true };
            combat.Frames(3, true);
            combat.Operation(() => { combat.Rider.O = 75f; }, true);
            combat.Frames(60, true);
            TestRunner.True(combat.HandOffs == 0 && combat.Refusals == 1 && combat.Hold.ObservedCount == 1 && combat.AtSaddle && !combat.Hold.Pending,
                "A standalone turn in combat was not refused exactly once.");
            // Repeated: 75 then 200 before the poll, one handoff at 200.
            var repeated = fresh();
            float x = repeated.Rider.X, z = repeated.Rider.Z;
            repeated.Operation(() => { repeated.Rider.O = 75f; }, true);
            repeated.Operation(() => { repeated.Rider.O = 200f; }, true);
            repeated.TargetX = x; repeated.TargetZ = z;
            repeated.Frames(40, true);
            TestRunner.True(repeated.HandOffs == 1 && repeated.KeptLastTarget && Math.Abs(repeated.Rider.O - 200f) < 0.01f, "Repeated turns did not keep the final one, once.");
            // Nested: the outer operation turns again after the inner one.
            var nested = fresh();
            nested.Operation(() => { nested.Operation(() => { nested.Rider.O = 75f; }, true); nested.Rider.O = 120f; }, true);
            nested.Frames(40, true);
            TestRunner.True(nested.HandOffs == 1 && Math.Abs(nested.Rider.O - 120f) < 0.01f, "A nested turn did not keep the outermost final orientation.");
            // Wrapped: 10 to 370 is the same facing (nothing); 350 to 10 is a turn.
            var same = fresh();
            same.Rider.O = 10f;
            same.Operation(() => { same.Rider.O = 370f; }, true);
            same.Frames(10, true);
            TestRunner.True(same.HandOffs == 0 && same.Hold.ObservedCount == 0, "The same facing written as 370 was taken for a turn.");
            var wrapped = fresh();
            wrapped.Rider.O = 350f;
            wrapped.Operation(() => { wrapped.Rider.O = 10f; }, true);
            wrapped.Frames(40, true);
            TestRunner.True(wrapped.HandOffs == 1 && Math.Abs(wrapped.Rider.O - 10f) < 0.01f, "A turn across 0 degrees was missed.");
        }

        private static void WholePairByProvenTargets()
        {
            // Each case: a pending rider-only placement A first; then a CommandTeleportParty (or a single-unit operation).
            Func<bool, Scene> pending = reviewed =>
            {
                var s = new Scene { Attribution = true, ReviewedE1c0 = reviewed };
                s.Frames(3, true);
                s.RiderOnlyPlacement(2f, 0.6f, 0.8f, 30f, true);
                return s;
            };
            foreach (var reviewed in new[] { false, true })
            {
                // 1. The rider already stands at its authored transform (A, 30); only the mount changes.
                var riderAtTarget = pending(reviewed);
                float ax = riderAtTarget.TargetX, az = riderAtTarget.TargetZ;
                riderAtTarget.Operation(() => { riderAtTarget.Rider.X = ax; riderAtTarget.Rider.Z = az; riderAtTarget.Rider.O = 30f; riderAtTarget.Mount.X = 6f; riderAtTarget.Mount.O = 90f; }, true, true, true);
                riderAtTarget.Frames(40, true);
                // 2. The mount already stands at its authored transform; only the rider changes.
                var mountAtTarget = pending(reviewed);
                mountAtTarget.Operation(() => { mountAtTarget.Rider.X = mountAtTarget.Mount.X - 1.5f; mountAtTarget.Rider.O = 10f; }, true, true, true);
                mountAtTarget.Frames(40, true);
                // 4. Both setters run with the values already there.
                var bothEqual = pending(reviewed);
                bothEqual.Operation(() => { }, true, true, true);
                bothEqual.Frames(40, true);
                if (!reviewed)
                {
                    TestRunner.True(riderAtTarget.HandOffs == 0 && riderAtTarget.Hold.ClearedCount == 1 && riderAtTarget.AtSaddle,
                        "Rider already at target: the obsolete rider-only hold was not cleared.");
                    TestRunner.True(mountAtTarget.HandOffs == 0 && mountAtTarget.Hold.ClearedCount == 1 && mountAtTarget.Hold.SupersededCount == 0 && mountAtTarget.AtSaddle,
                        "Mount already at target: the hold was replaced by another rider-only hold instead of cleared.");
                    TestRunner.True(bothEqual.HandOffs == 0 && bothEqual.Hold.ClearedCount == 1 && Math.Abs(bothEqual.Mount.X) < 0.001f,
                        "Both already at target: the proven whole-pair write was not applied, or movement was manufactured.");
                }
                else
                {
                    TestRunner.True(riderAtTarget.HandOffs == 1 && riderAtTarget.Hold.ClearedCount == 0,
                        "Setup (reviewed e1c0c7ad): rider at target should have left the obsolete hold to be reapplied.");
                    TestRunner.True(mountAtTarget.HandOffs == 1 && mountAtTarget.Hold.SupersededCount == 1,
                        "Setup (reviewed e1c0c7ad): mount at target should have replaced the hold with a rider-only one.");
                    TestRunner.True(bothEqual.HandOffs == 1 && bothEqual.Hold.ClearedCount == 0,
                        "Setup (reviewed e1c0c7ad): both at target should have left the obsolete hold.");
                }
            }
            // 3. Both change: whole pair, as before.
            var both = pending(false);
            both.Operation(() => { both.Rider.X = 9f; both.Mount.X = 8f; }, true, true, true);
            both.Frames(40, true);
            TestRunner.True(both.HandOffs == 0 && both.Hold.ClearedCount == 1 && both.AtSaddle, "A whole-pair placement of both changed values did not clear the hold.");
            // 5. Only the rider is in the target set (PartyCharacters excludes pets): a rider placement, superseding A.
            var riderOnly = pending(false);
            riderOnly.RiderOnlyPlacementTargeted(2.5f, -0.8f, 0.6f, 20f);
            riderOnly.Frames(40, true);
            TestRunner.True(riderOnly.HandOffs == 1 && riderOnly.KeptLastTarget && riderOnly.Hold.ClearedCount == 0, "A rider-only target set was taken for a whole pair.");
            // 6. A single-unit operation on the mount (TranslocateUnit): not whole-pair; A stays.
            var mountOnly = pending(false);
            float px = mountOnly.TargetX, pz = mountOnly.TargetZ;
            mountOnly.Operation(() => { mountOnly.Mount.X += 2f; }, true);
            mountOnly.Frames(40, true);
            TestRunner.True(mountOnly.HandOffs == 1 && mountOnly.Hold.ClearedCount == 0 && Math.Abs(mountOnly.Rider.X - px) < 0.1f && Math.Abs(mountOnly.Rider.Z - pz) < 0.1f,
                "A single-unit operation on the mount was taken for a whole pair.");
        }

        private static void ThrowingSuccessLogger()
        {
            var first = new MountedScenePlacementHold(); var second = new MountedScenePlacementHold();
            Func<MountedScenePlacementHold, float, MountedScenePlacementObservation> placed = (hold, x) => new MountedScenePlacementObservation
            {
                Hold = hold, PairMounted = true, RiderX0 = 0f, RiderX = x, RiderZ = 1f, RiderOrientation = 45f, MountX0 = 0f, MountX = 0f
            };
            var calls = 0;
            var changed = MountedScenePlacementAttributionCore.Apply(new[] { placed(first, 2f), placed(second, 3f) }, true, 1,
                (o, write, change) => { calls++; throw new InvalidOperationException("the logger failed"); });
            TestRunner.True(changed == 2 && calls == 2 && first.Pending && second.Pending && first.X == 2f && second.X == 3f,
                "A throwing success logger stopped the second pair's attribution.");
            // A healthy logger receives both.
            var lines = new System.Collections.Generic.List<string>();
            MountedScenePlacementAttributionCore.Apply(new[] { placed(new MountedScenePlacementHold(), 2f), placed(new MountedScenePlacementHold(), 3f) }, true, 1,
                (o, write, change) => lines.Add(write + "->" + change + "@" + o.RiderX));
            TestRunner.True(lines.Count == 2 && lines[0] == "RiderPlacement->Held@2" && lines[1] == "RiderPlacement->Held@3", "A healthy logger did not receive both diagnostics.");
            // The reviewed e1c0c7ad attribution logged inside its loop without containment (transcribed): the second pair is lost.
            var a = new MountedScenePlacementHold(); var b = new MountedScenePlacementHold();
            try
            {
                foreach (var hold in new[] { a, b })
                {
                    hold.Attribute(2f, 0f, 1f, 45f, 1);
                    throw new InvalidOperationException("the logger failed");
                }
            }
            catch (InvalidOperationException) { }
            TestRunner.True(a.Pending && !b.Pending, "Setup (reviewed loop): the second pair should have been skipped.");
        }

        private static Provenance LivePlayer(MountedScenePlacementSource source) =>
            new Provenance { Source = source, PlayerPresent = true, CommandCurrent = true };

        private static void BackgroundSceneOwnership()
        {
            foreach (var reviewed in new[] { false, true })
            {
                // A. A live background player's CommandMoveUnit times out and places the seated rider 1.5 m away; mount unchanged.
                var move = new Scene { Attribution = true, ReviewedGlobalAtPostfix = reviewed };
                move.Frames(3, false);
                float tx = move.Mount.X + 0.9f, tz = move.Mount.Z + 1.2f;
                move.Operation(() => { move.Rider.X = tx; move.Rider.Z = tz; }, LivePlayer(MountedScenePlacementSource.CommandMoveUnitTimedOut));
                move.TargetX = tx; move.TargetZ = tz;
                move.Frames(40, false);
                // B. A live background CommandTeleportParty whose target set holds the rider and not the pet.
                var party = new Scene { Attribution = true, ReviewedGlobalAtPostfix = reviewed };
                party.Frames(3, false);
                float px = party.Mount.X - 2f, pz = party.Mount.Z + 0.5f;
                party.Operation(() => { party.Rider.X = px; party.Rider.Z = pz; party.Rider.O = 0.3f; }, LivePlayer(MountedScenePlacementSource.CommandTeleportParty), true, false);
                party.TargetX = px; party.TargetZ = pz;
                party.Frames(40, false);
                // C. The same ownership; the rider stays where it is and is turned to 75 degrees.
                var turn = new Scene { Attribution = true, ReviewedGlobalAtPostfix = reviewed };
                turn.Frames(3, false);
                float sx = turn.Rider.X, sz = turn.Rider.Z;
                turn.Operation(() => { turn.Rider.O = 75f; }, LivePlayer(MountedScenePlacementSource.CommandTeleportParty), true, false);
                turn.TargetX = sx; turn.TargetZ = sz;
                turn.Frames(40, false);
                if (!reviewed)
                {
                    TestRunner.True(move.HandOffs == 1 && move.KeptLastTarget, "A. The background timed-out move was not handed off once at its endpoint.");
                    TestRunner.True(party.HandOffs == 1 && party.KeptLastTarget && party.Hold.ClearedCount == 0, "B. The background rider-only party teleport was not a rider placement.");
                    TestRunner.True(turn.HandOffs == 1 && turn.KeptLastTarget && Math.Abs(turn.Rider.O - 75f) < 0.01f, "C. The background standalone turn was not handed off once at its position and 75 degrees.");
                }
                else
                {
                    TestRunner.True(move.HandOffs == 0 && move.AtSaddle && party.HandOffs == 0 && party.AtSaddle && turn.HandOffs == 0 && turn.Hold.ObservedCount == 0,
                        "Setup (reviewed global-mode attribution): the background operations should have been swallowed.");
                }
            }
            // B, classified directly: RiderPlacement, neither None nor WholePair.
            var observation = new MountedScenePlacementObservation { Hold = new MountedScenePlacementHold(), PairMounted = true, RiderX = 2f, RiderTargeted = true };
            TestRunner.Equal(MountedScenePlacementOperation.RiderPlacement, MountedScenePlacementAttributionCore.Classify(observation,
                MountedSceneOperationOwnership.SceneOwned(LivePlayer(MountedScenePlacementSource.CommandTeleportParty).Decide())), "B was not classified as a rider placement.");
            // Combat: a background placement is still refused once.
            var combat = new Scene { Attribution = true, Combat = true };
            combat.Frames(3, false);
            combat.Operation(() => { combat.Rider.X += 1f; }, LivePlayer(MountedScenePlacementSource.CommandMoveUnitTimedOut));
            combat.Frames(60, false);
            TestRunner.True(combat.HandOffs == 0 && combat.Refusals == 1 && combat.AtSaddle, "A background placement in combat was not refused exactly once.");
        }

        private static void NonSceneControls()
        {
            var cases = new[]
            {
                Tuple.Create("null player", new Provenance { Source = MountedScenePlacementSource.CommandMoveUnitTimedOut }),
                Tuple.Create("finished player", new Provenance { Source = MountedScenePlacementSource.CommandMoveUnitTimedOut, PlayerPresent = true, PlayerFinished = true, CommandCurrent = true }),
                Tuple.Create("replaced or unrelated player (the command is not its current one)", new Provenance { Source = MountedScenePlacementSource.CommandTeleportParty, PlayerPresent = true }),
                Tuple.Create("direct TranslocateUnit outside any cutscene scope", new Provenance { Source = MountedScenePlacementSource.TranslocateUnit }),
                Tuple.Create("TranslocateUnit in the scope of a finished player", new Provenance { Source = MountedScenePlacementSource.TranslocateUnit, ScopePresent = true, ScopePlayerKnown = true, ScopePlayerFinished = true }),
                Tuple.Create("a command claiming an action scope", new Provenance { Source = MountedScenePlacementSource.CommandMoveUnitTimedOut, ScopePresent = true }),
            };
            foreach (var c in cases)
            {
                var scene = new Scene { Attribution = true };
                scene.Frames(3, false);
                scene.Operation(() => { scene.Rider.X += 1.5f; }, c.Item2);
                scene.Frames(20, false);
                TestRunner.True(scene.HandOffs == 0 && scene.Hold.ObservedCount == 0 && scene.AtSaddle, "Attributed with " + c.Item1 + ".");
            }
            foreach (var known in new[] { true, false })
            {
                var scoped = new Scene { Attribution = true };
                scoped.Frames(3, false);
                scoped.Operation(() => { scoped.Rider.X += 1.5f; }, new Provenance { Source = MountedScenePlacementSource.TranslocateUnit, ScopePresent = true, ScopePlayerKnown = known });
                scoped.TargetX = scoped.Rider.X; scoped.TargetZ = scoped.Rider.Z;
                scoped.Frames(40, false);
                TestRunner.True(scoped.HandOffs == 1 && scoped.KeptLastTarget, "A TranslocateUnit inside a " + (known ? "live" : "starting") + " cutscene scope was not owned.");
            }
            // Ordinary position and orientation updates outside the operations.
            var ordinary = new Scene { Attribution = true };
            ordinary.Frames(3, false);
            ordinary.Rider.X += 1.5f; ordinary.Rider.O = 80f;
            ordinary.Frames(10, false);
            TestRunner.True(ordinary.HandOffs == 0 && ordinary.Hold.ObservedCount == 0 && ordinary.AtSaddle, "An ordinary update outside the operations was attributed.");
        }

        private static void OwnershipCapturedAtEntry()
        {
            foreach (var reviewed in new[] { false, true })
            {
                // Owned at entry by a live background player; the player finishes inside the operation.
                var kept = new Scene { Attribution = true, ReviewedGlobalAtPostfix = reviewed };
                kept.Frames(3, false);
                var live = LivePlayer(MountedScenePlacementSource.CommandMoveUnitTimedOut);
                kept.Operation(() => { kept.Rider.X += 1.5f; live.PlayerFinished = true; live.CommandCurrent = false; }, live);
                kept.Frames(40, false);
                // Not owned at entry; a foreground mode begins inside the operation.
                var late = new Scene { Attribution = true, ReviewedGlobalAtPostfix = reviewed };
                late.Frames(3, false);
                var none = new Provenance { Source = MountedScenePlacementSource.TranslocateUnit };
                late.Operation(() => { late.Rider.X += 1.5f; none.Foreground = true; }, none);
                late.Frames(40, false);
                if (!reviewed)
                    TestRunner.True(kept.HandOffs == 1 && late.HandOffs == 0 && late.AtSaddle, "Ownership was not the one captured at the operation's entry.");
                else
                    TestRunner.True(kept.HandOffs == 0 && late.HandOffs == 1, "Setup (reviewed attribution): ownership should have been read at the postfix.");
            }
        }

        // Review R10: the native facts the prefix reads for one operation (represented). Decide is the production policy.
        private sealed class Provenance
        {
            public MountedScenePlacementSource Source;
            public bool Foreground, PlayerPresent, PlayerFinished, CommandCurrent, ScopePresent, ScopePlayerKnown, ScopePlayerFinished;

            public MountedSceneOwnership Decide() => MountedSceneOperationOwnership.Decide(Source, Foreground,
                Source != MountedScenePlacementSource.TranslocateUnit && MountedSceneOperationOwnership.CommandOwnedByLivePlayer(PlayerPresent, PlayerFinished, CommandCurrent),
                Source == MountedScenePlacementSource.TranslocateUnit && MountedSceneOperationOwnership.ActionInCutsceneScope(ScopePresent, ScopePlayerKnown, ScopePlayerFinished));
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
            public bool Attribution, Combat, ReviewedFirstWins, ReviewedPositionOnly, ReviewedE1c0, ReviewedGlobalAtPostfix;
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

            // A CommandTeleportParty whose target set holds the rider only (the mount, a pet, is not listed).
            public void RiderOnlyPlacementTargeted(float distance, float dirX, float dirZ, float orientation)
            {
                var x = Mount.X + distance * dirX; var z = Mount.Z + distance * dirZ;
                Operation(() => { Rider.X = x; Rider.Z = z; Rider.O = orientation; }, true, true, false);
                TargetX = Rider.X; TargetZ = Rider.Z;
            }

            public void RiderOnlyPlacement(float distance, float dirX, float dirZ, float orientation, bool sceneActive)
            {
                var x = Mount.X + distance * dirX; var z = Mount.Z + distance * dirZ;
                Operation(() => { Rider.X = x; Rider.Z = z; Rider.O = orientation; }, sceneActive);
                TargetX = Rider.X; TargetZ = Rider.Z;
            }

            // A native scene placement operation: the product's capture before it and its attribution after it
            // (MountedScenePlacementAttribution), with the production classification and hold.
            // riderTargeted/mountTargeted: the operation's proven target set (CommandTeleportParty); false for single-unit ones.
            // A foreground scene (the global rule) owns the operation when sceneActive.
            public void Operation(Action writes, bool sceneActive, bool riderTargeted = false, bool mountTargeted = false) =>
                Operation(writes, new Provenance { Source = MountedScenePlacementSource.TranslocateUnit, Foreground = sceneActive }, riderTargeted, mountTargeted);

            // Review R10: the operation's own provenance (represented native cutscene state). The production policy decides who
            // owns it at entry, as the prefix does; the reviewed 813d00a1 attribution (transcribed) read only the global mode
            // at the postfix.
            public void Operation(Action writes, Provenance provenance, bool riderTargeted = false, bool mountTargeted = false)
            {
                var ownership = provenance.Decide();
                float rx = Rider.X, rz = Rider.Z, ro = Rider.O, mx = Mount.X, mz = Mount.Z, mo = Mount.O;
                var instance = coordinator.ActivePair;
                depth++;
                try { writes(); } finally { depth--; }
                if (!Attribution) return;
                // MountedScenePlacementAttribution acts only on the mounting instance it captured.
                if (instance == null || !ReferenceEquals(coordinator.ActivePair, instance)) return;
                var mounted = coordinator.State == RelationshipState.Mounted;
                bool riderMoved = MountedSceneContinuityPolicy.PositionWritten(Rider.X - rx, 0, Rider.Z - rz),
                    mountMoved = MountedSceneContinuityPolicy.PositionWritten(Mount.X - mx, 0, Mount.Z - mz),
                    riderTurned = MountedSceneContinuityPolicy.OrientationWritten(ro, Rider.O),
                    mountTurned = MountedSceneContinuityPolicy.OrientationWritten(mo, Mount.O);
                if (ReviewedFirstWins || ReviewedPositionOnly || ReviewedE1c0)
                {
                    // Earlier reviewed attributions, transcribed (not production) only to show what each review changed:
                    // b30c24f8 first wins; 263ac3ea positions only; e1c0c7ad turns without target evidence, and a turn
                    // only reorients a pending placement.
                    var sceneActive = provenance.Foreground;
                    var reviewed = ReviewedPositionOnly ? MountedSceneContinuityPolicy.ClassifyOperationWrite(mounted, sceneActive, riderMoved, mountMoved)
                        : MountedSceneContinuityPolicy.ClassifyOperationWrite(mounted, sceneActive, riderMoved, ReviewedE1c0 && riderTurned, mountMoved, ReviewedE1c0 && mountTurned);
                    if (reviewed == MountedScenePlacementOperation.RiderTurn) { if (Hold.Pending) Hold.Reorient(Hold.X, Hold.Y, Hold.Z, Rider.O, frame); return; }
                    if (reviewed == MountedScenePlacementOperation.RiderPlacement && !(ReviewedFirstWins && Hold.Pending)) Hold.Attribute(Rider.X, 0, Rider.Z, Rider.O, frame);
                    else if (reviewed == MountedScenePlacementOperation.WholePair && !ReviewedFirstWins) Hold.ClearForWholePair();
                    return;
                }
                MountedScenePlacementAttributionCore.Apply(new[]
                {
                    new MountedScenePlacementObservation
                    {
                        Pair = this, Hold = Hold, PairMounted = mounted,
                        RiderX0 = rx, RiderZ0 = rz, RiderOrientation0 = ro, MountX0 = mx, MountZ0 = mz, MountOrientation0 = mo,
                        RiderX = Rider.X, RiderZ = Rider.Z, RiderOrientation = Rider.O, MountX = Mount.X, MountZ = Mount.Z, MountOrientation = Mount.O,
                        RiderTargeted = riderTargeted, MountTargeted = mountTargeted
                    }
                }, ReviewedGlobalAtPostfix ? provenance.Foreground : MountedSceneOperationOwnership.SceneOwned(ownership), frame, null);
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
