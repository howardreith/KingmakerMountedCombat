using System;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // Known issues C02: rider-only scene placements of three metres or less.
    //
    // Production code executed here: MountedSceneContinuityPolicy.AttributesRiderPlacement (a native scene placement
    // operation wrote the rider and not its mount), HoldsUnattributedPlacement (the synchronizer's distance rule for any
    // other write), MountedSceneHandOffSequence (suspend, then reapply the scene's placement) and the real
    // MountedRelationshipCoordinator for the suspension. Modelled: the rider's synchronizer frame loop as RiderMovementAgent
    // runs it (hold a pending placement for its decision frames, otherwise write the rider to the saddle), the owner's
    // polling that hands the pair off or releases the hold, and the native operation's direct Position writes.
    internal static class ScenePlacementAttributionTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("known issues C02: the attribution decision takes only a scene operation's write of the rider without its mount; the unattributed rule is the unchanged distance rule", PolicyDecisions);
            runner.Run("known issues C02 (synchronizer and native write modelled; decisions and handoff production): out of combat, an attributed rider placement at 0.5 m, 2 m, 3 m and 6 m keeps its endpoint; without attribution only the 6 m placement did", AttributedPlacementsKeepTheirEndpoint);
            runner.Run("known issues C02 (modelled synchronizer): in combat the handoff is refused once, the rider returns to the saddle once and nothing oscillates afterwards", RefusedHandOffSettlesOnce);
            runner.Run("known issues C02 (modelled synchronizer): ordinary small movement, a mount-only placement, a placement of both, one outside a scene and an unrelated unit never hand the rider off", NoAttributionControls);
        }

        private static void PolicyDecisions()
        {
            TestRunner.True(MountedSceneContinuityPolicy.AttributesRiderPlacement(true, true, true, false), "A scene operation's rider-only write is not attributed.");
            TestRunner.True(!MountedSceneContinuityPolicy.AttributesRiderPlacement(true, true, true, true), "A write of both rider and mount is attributed to the rider.");
            TestRunner.True(!MountedSceneContinuityPolicy.AttributesRiderPlacement(true, true, false, true), "A mount-only write is attributed to the rider.");
            TestRunner.True(!MountedSceneContinuityPolicy.AttributesRiderPlacement(true, false, true, false), "A write outside a scene is attributed.");
            TestRunner.True(!MountedSceneContinuityPolicy.AttributesRiderPlacement(false, true, true, false), "An unmounted rider is attributed.");
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
                    scene.RiderOnlyPlacement(distance, true);
                    scene.Frames(40, true);
                    var kept = Math.Abs(scene.Rider.X - scene.TargetX) < 0.1f && Math.Abs(scene.Rider.Z - scene.TargetZ) < 0.1f;
                    if (attribution || distance > MountedSceneContinuityPolicy.ScenePlacementMinimumMetres)
                    {
                        TestRunner.True(kept && scene.HandedOff && scene.Outcome == MountedSceneHandOffOutcome.HandedOffPlacementPreserved,
                            "The scene's placement at " + distance + " m was not kept" + (attribution ? "" : " (the unattributed rule should keep a far one)") + ".");
                    }
                    else
                    {
                        // The Sprint 3 base: an unattributed near placement cannot be told from lag; the rider returns to the saddle.
                        TestRunner.True(!kept && !scene.HandedOff && scene.AtSaddle,
                            "Setup (base without attribution): the near placement at " + distance + " m should have been swallowed.");
                    }
                }
        }

        private static void RefusedHandOffSettlesOnce()
        {
            var scene = new Scene { Attribution = true, Combat = true };
            scene.Frames(3, true);
            scene.RiderOnlyPlacement(2f, true);
            scene.Frames(60, true);
            TestRunner.True(!scene.HandedOff && scene.AtSaddle && scene.Refusals == 1, "A refused handoff did not end once, at the saddle.");
            TestRunner.True(scene.Holds == 1 && scene.SaddleCorrections == 1, "The rider oscillated between the placement and the saddle.");
        }

        private static void NoAttributionControls()
        {
            // Ordinary synchronization lag: the rider is a little off the saddle, and no operation wrote it.
            var lag = new Scene { Attribution = true };
            lag.Frames(3, true);
            lag.Rider.X += 0.2f;
            lag.Frames(10, true);
            TestRunner.True(lag.Holds == 0 && !lag.HandedOff && lag.AtSaddle, "Ordinary movement was taken for a placement.");
            // A mount-only placement moves the saddle; the rider follows.
            var mountOnly = new Scene { Attribution = true };
            mountOnly.Frames(3, true);
            mountOnly.Operation(() => { mountOnly.Mount.X += 2f; }, true);
            mountOnly.Frames(10, true);
            TestRunner.True(mountOnly.Holds == 0 && !mountOnly.HandedOff && mountOnly.AtSaddle && Math.Abs(mountOnly.Rider.X - mountOnly.Mount.X) < 0.01f,
                "A mount-only placement handed the rider off.");
            // Both written: the pair stays together at the mount's placement, as before.
            var both = new Scene { Attribution = true };
            both.Frames(3, true);
            both.Operation(() => { both.Mount.X += 2f; both.Rider.X += 2.5f; }, true);
            both.Frames(10, true);
            TestRunner.True(both.Holds == 0 && !both.HandedOff && both.AtSaddle, "A placement of both handed the rider off.");
            // Outside a scene nothing is attributed.
            var outside = new Scene { Attribution = true };
            outside.Frames(3, false);
            outside.RiderOnlyPlacement(2f, false);
            outside.Frames(10, false);
            TestRunner.True(outside.Holds == 0 && !outside.HandedOff && outside.AtSaddle, "A placement outside a scene was attributed.");
            // An unrelated unit written by the operation: the pair's rider was not written.
            var unrelated = new Scene { Attribution = true };
            unrelated.Frames(3, true);
            var other = new Body { X = 10f };
            unrelated.Operation(() => { other.X += 1f; }, true);
            unrelated.Frames(10, true);
            TestRunner.True(unrelated.Holds == 0 && !unrelated.HandedOff && unrelated.AtSaddle, "An unrelated unit's placement handed the rider off.");
        }

        // ---- the model ----
        private sealed class Body { internal float X, Z; }

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
            public bool Attribution, Combat;
            public readonly Body Rider = new Body(), Mount = new Body();
            private readonly MountedRelationshipCoordinator coordinator;
            private readonly ReleasingRuntime runtime;
            public float TargetX, TargetZ;
            public bool HandedOff;
            public MountedSceneHandOffOutcome Outcome;
            public int Refusals, Holds, SaddleCorrections;
            private float? pendingX, pendingZ;
            private int pendingAge;
            private bool correctNext, hasLastWrite;
            private float lastX, lastZ;

            public Scene()
            {
                runtime = new ReleasingRuntime { Rider = Rider, Mount = Mount };
                coordinator = new MountedRelationshipCoordinator(runtime);
                var candidate = new MountedPairCandidate("rider-a", "horse-a")
                {
                    RiderIsDirectlyControllable = true, MountIsDirectlyControllable = true, RiderIsAliveAndConscious = true, MountIsAliveAndConscious = true,
                    ExactReciprocalCompanionRelationship = true, RiderSizeOrdinal = 4, MountSizeOrdinal = 5, RiderViewAndStockAgentAvailable = true,
                    MountViewAndStockAgentAvailable = true, RiderStockAgentEnabled = true, MountStockAgentEnabled = true, RiderAgentOverrideAvailable = true,
                    MountAgentOverrideAvailable = true, RiderIsExactlyMedium = true, SafeMovementMode = true
                };
                TestRunner.True(coordinator.Mount(candidate).Succeeded, "Setup: the pair did not mount.");
            }

            public bool AtSaddle => Math.Abs(Rider.X - Mount.X) < 0.001f && Math.Abs(Rider.Z - Mount.Z) < 0.001f;

            public void RiderOnlyPlacement(float distance, bool sceneActive)
            {
                TargetX = Mount.X + distance * 0.6f; TargetZ = Mount.Z + distance * 0.8f;
                Operation(() => { Rider.X = TargetX; Rider.Z = TargetZ; }, sceneActive);
            }

            // A native scene placement operation, with the product's capture before and attribution after it.
            public void Operation(Action writes, bool sceneActive)
            {
                float rx = Rider.X, rz = Rider.Z, mx = Mount.X, mz = Mount.Z;
                writes();
                if (!Attribution || HandedOff) return;
                if (MountedSceneContinuityPolicy.AttributesRiderPlacement(coordinator.State == RelationshipState.Mounted, sceneActive,
                        MountedSceneContinuityPolicy.PositionWritten(Rider.X - rx, 0, Rider.Z - rz), MountedSceneContinuityPolicy.PositionWritten(Mount.X - mx, 0, Mount.Z - mz)) &&
                    !pendingX.HasValue)
                {
                    pendingX = Rider.X; pendingZ = Rider.Z; pendingAge = 0; correctNext = false; Holds++;
                }
            }

            public void Frames(int count, bool sceneActive) { for (var i = 0; i < count; i++) Frame(sceneActive); }

            private void Frame(bool sceneActive)
            {
                if (HandedOff) return;
                // The owner's poll (ObserveScenePlacements): hand the pair off at the placement, or release the hold.
                if (pendingX.HasValue)
                {
                    float x = pendingX.Value, z = pendingZ.Value;
                    if (Combat)
                    {
                        Refusals++;
                        pendingX = null; correctNext = true;
                    }
                    else
                    {
                        var captured = Rider;
                        Outcome = MountedSceneHandOffSequence.Run(captured,
                            () => coordinator.Dismount(CleanupTrigger.EventSuspension).Succeeded && coordinator.State == RelationshipState.Unmounted,
                            true, r => { r.X = x; r.Z = z; },
                            r => Math.Abs(r.X - x) < 0.1f && Math.Abs(r.Z - z) < 0.1f, out _);
                        HandedOff = MountedSceneHandOffSequence.IsHandedOff(Outcome);
                        if (HandedOff) return;
                        pendingX = null; correctNext = true;
                    }
                }
                // The synchronizer: a held placement waits its decision frames; otherwise the rider is written to the saddle,
                // unless the unattributed distance rule takes the write for the scene's placement.
                if (pendingX.HasValue) { if (++pendingAge <= 30) return; pendingX = null; }
                if (correctNext) { correctNext = false; WriteSaddle(); return; }
                var deviation = (float)Math.Sqrt((Rider.X - Mount.X) * (Rider.X - Mount.X) + (Rider.Z - Mount.Z) * (Rider.Z - Mount.Z));
                var moved = hasLastWrite ? (float)Math.Sqrt((Rider.X - lastX) * (Rider.X - lastX) + (Rider.Z - lastZ) * (Rider.Z - lastZ)) : 0f;
                if (MountedSceneContinuityPolicy.HoldsUnattributedPlacement(sceneActive, hasLastWrite, deviation, moved))
                {
                    pendingX = Rider.X; pendingZ = Rider.Z; pendingAge = 0; Holds++;
                    return;
                }
                WriteSaddle();
            }

            private void WriteSaddle()
            {
                if (!AtSaddle) SaddleCorrections++;
                Rider.X = Mount.X; Rider.Z = Mount.Z;
                hasLastWrite = true; lastX = Rider.X; lastZ = Rider.Z;
            }
        }
    }
}
