using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // Shared-parent sibling slots for the rider attachment lease. Batch187 showed
    // six riders of one native parent restored first-in-first-out: a captured
    // ordinal taken while other riders were absent pointed past the end of the
    // parent once they returned, and Unity clamped it.
    internal static class TransformAttachmentSlotTests
    {
        // First-acquire ordinals observed natively in batch187 rows 1,3,5,7,9,11
        // under the prior capture-only lease, and the ten other parent children.
        private static readonly int[] Batch187CaptureOrdinals = { 0, 7, 5, 4, 8, 9 };
        private const int Batch187OtherChildren = 10;

        public static void Register(TestRunner runner)
        {
            runner.Run("attachment slot fixture reproduces the batch187 native capture ordinals", Batch187FixtureMatchesNativeObservation);
            runner.Run("attachment slot restores the batch187 six-rider layout exactly for three cycles", Batch187LayoutRestoresExactlyForThreeCycles);
            runner.Run("attachment slot restores six riders exactly for every dismount order", EveryDismountOrderRestoresExactly);
            runner.Run("attachment slot restores interleaved mount and dismount sequences exactly", InterleavedSequencesRestoreExactly);
            runner.Run("attachment slot keeps one-pair and two-pair controls exact", OneAndTwoPairControlsRemainExact);
            runner.Run("attachment slot keeps the exact slot when an unrelated earlier sibling leaves", UnrelatedEarlierRemovalKeepsExactSlot);
            runner.Run("attachment slot ignores unrelated siblings appended while mounted", UnrelatedAppendedSiblingIsIgnored);
            runner.Run("attachment slot returns a scene-root rider to its exact scene and root slot", SceneRootReturnsToExactSceneSlot);
            runner.Run("attachment slot rolls back a failed reservation creation without moving the rider", FailedReservationCreationRollsBack);
            runner.Run("attachment slot withdraws a misplaced reservation before any detachment", MisplacedReservationIsWithdrawn);
            runner.Run("attachment slot restores exactly after a failed detachment", FailedDetachmentRestoresExactly);
            runner.Run("attachment slot refuses a detachment that did not vacate the slot", UnvacatedSlotIsRefused);
            runner.Run("attachment slot retains reservation custody through a failed restore", FailedRestoreRetainsReservation);
            runner.Run("attachment slot retains custody when the reservation release fails", FailedReservationReleaseRetainsCustody);
            runner.Run("attachment slot never repositions a verified rider after a release that left the slot then failed", VerifiedRiderStaysAfterPartialRelease);
            runner.Run("attachment slot never repositions a verified rider after a failed post-release verification", VerifiedRiderStaysAfterFailedPostReleaseRead);
            runner.Run("attachment slot rollback before detachment tolerates unrelated siblings and native movement", PreDetachRollbackToleratesUnrelatedChanges);
            runner.Run("attachment slot falls back to the exact captured ordinal after third-party reservation loss", ThirdPartyReservationLossUsesExactOrdinal);
            runner.Run("attachment slot refuses an unrepresentable ordinal after third-party reservation loss", ThirdPartyReservationLossRefusesClampedOrdinal);
            runner.Run("attachment slot releases a native replacement last without moving the reserved slot", ReplacementTakesNativeLastPlacement);
        }

        private static void Batch187FixtureMatchesNativeObservation()
        {
            // Sanity anchor: the reconstructed layout, detached in order without
            // reservations, yields exactly the batch187 native capture ordinals.
            var fixture = Batch187Layout();
            var observed = new List<int>();
            foreach (var rider in fixture.Riders)
            {
                observed.Add(rider.SiblingIndex);
                rider.SetParent(fixture.Hierarchy.Root("carrier"));
            }
            TestRunner.True(observed.SequenceEqual(Batch187CaptureOrdinals),
                "Reconstructed layout differs from batch187: " + string.Join(",", observed));
        }

        private static void Batch187LayoutRestoresExactlyForThreeCycles()
        {
            var fixture = Batch187Layout();
            var leases = fixture.Riders.Select(rider => fixture.Hierarchy.CreateLease()).ToArray();
            for (var cycle = 1; cycle <= 3; cycle++)
            {
                for (var index = 0; index < 6; index++)
                {
                    leases[index].Acquire(fixture.Riders[index], fixture.Anchors[index]);
                    TestRunner.Equal(16, fixture.Parent.Children.Count, "A reservation did not keep the shared parent's child count, cycle " + cycle + ".");
                }
                for (var index = 0; index < 6; index++)
                {
                    var captured = leases[index].OriginalSiblingIndex;
                    leases[index].Restore();
                    TestRunner.True(leases[index].LastRestoreVerified && leases[index].LastRestoreUsedReservation,
                        "Rider " + index + " was not restored through its reservation, cycle " + cycle + ".");
                    TestRunner.Equal(captured, fixture.Riders[index].SiblingIndex, "Rider " + index + " left its captured slot, cycle " + cycle + ".");
                }
                fixture.Hierarchy.EndFrame();
                fixture.RequireOriginalLayout("cycle " + cycle);
            }
        }

        private static void EveryDismountOrderRestoresExactly()
        {
            var fixture = Batch187Layout();
            var leases = fixture.Riders.Select(rider => fixture.Hierarchy.CreateLease()).ToArray();
            var orders = 0;
            foreach (var order in Permutations(Enumerable.Range(0, 6).ToArray()))
            {
                for (var index = 0; index < 6; index++)
                {
                    leases[index].Acquire(fixture.Riders[index], fixture.Anchors[index]);
                }
                foreach (var index in order)
                {
                    leases[index].Restore();
                }
                fixture.Hierarchy.EndFrame();
                fixture.RequireOriginalLayout("restore order " + string.Join("", order));
                orders++;
            }
            TestRunner.Equal(720, orders, "Not every six-rider dismount order was exercised.");
        }

        private static void InterleavedSequencesRestoreExactly()
        {
            var random = new Random(187);
            for (var sequence = 0; sequence < 200; sequence++)
            {
                var fixture = Batch187Layout();
                var leases = fixture.Riders.Select(rider => fixture.Hierarchy.CreateLease()).ToArray();
                for (var step = 0; step < 40; step++)
                {
                    var index = random.Next(6);
                    if (leases[index].IsAcquired)
                    {
                        var captured = leases[index].OriginalSiblingIndex;
                        leases[index].Restore();
                        TestRunner.Equal(captured, fixture.Riders[index].SiblingIndex,
                            "Interleaved sequence " + sequence + " step " + step + " left rider " + index + " out of its slot.");
                    }
                    else
                    {
                        leases[index].Acquire(fixture.Riders[index], fixture.Anchors[index]);
                    }
                    if (random.Next(4) == 0) fixture.Hierarchy.EndFrame();
                }
                foreach (var lease in leases.Where(value => value.IsAcquired)) lease.Restore();
                fixture.Hierarchy.EndFrame();
                fixture.RequireOriginalLayout("interleaved sequence " + sequence);
            }
        }

        private static void OneAndTwoPairControlsRemainExact()
        {
            foreach (var count in new[] { 1, 2 })
            {
                var hierarchy = new FakeSiblingHierarchy();
                var parent = hierarchy.Root("dynamic-root");
                var riders = new List<FakeSiblingNode>();
                for (var index = 0; index < 4; index++) hierarchy.Child("other-" + index, parent);
                for (var index = 0; index < count; index++) riders.Add(hierarchy.Child("rider-" + index, parent));
                hierarchy.Child("other-tail", parent);
                var original = parent.Children.ToArray();
                var leases = riders.Select(rider => hierarchy.CreateLease()).ToArray();
                for (var cycle = 0; cycle < 3; cycle++)
                {
                    for (var index = 0; index < count; index++) leases[index].Acquire(riders[index], hierarchy.Root("anchor-" + index));
                    for (var index = 0; index < count; index++) leases[index].Restore();
                    hierarchy.EndFrame();
                    TestRunner.True(parent.Children.SequenceEqual(original), count + "-pair control changed the parent order.");
                    TestRunner.Equal(0, hierarchy.LiveReservations, count + "-pair control left a reservation.");
                }
            }
        }

        private static void UnrelatedEarlierRemovalKeepsExactSlot()
        {
            var hierarchy = new FakeSiblingHierarchy();
            var parent = hierarchy.Root("dynamic-root");
            var early = hierarchy.Child("early", parent);
            var before = hierarchy.Child("before", parent);
            var rider = hierarchy.Child("rider", parent);
            var after = hierarchy.Child("after", parent);
            var lease = hierarchy.CreateLease();
            lease.Acquire(rider, hierarchy.Root("anchor"));
            early.Leave();
            lease.Restore();
            hierarchy.EndFrame();
            TestRunner.True(parent.Children.SequenceEqual(new[] { before, rider, after }),
                "An unrelated earlier removal moved the rider out of its exact slot.");
            TestRunner.True(lease.LastRestoreVerified && lease.LastRestoreUsedReservation,
                "Exact slot restoration was not attributed to the reservation.");
            TestRunner.Equal(lease.OriginalSiblingIndex - 1, rider.SiblingIndex,
                "The absolute ordinal should reflect the unrelated earlier removal.");
        }

        private static void UnrelatedAppendedSiblingIsIgnored()
        {
            var hierarchy = new FakeSiblingHierarchy();
            var parent = hierarchy.Root("dynamic-root");
            hierarchy.Child("before", parent);
            var rider = hierarchy.Child("rider", parent);
            hierarchy.Child("after", parent);
            var lease = hierarchy.CreateLease();
            lease.Acquire(rider, hierarchy.Root("anchor"));
            var spawned = hierarchy.Child("spawned", parent);
            lease.Restore();
            hierarchy.EndFrame();
            TestRunner.Equal(1, rider.SiblingIndex, "An appended unrelated sibling moved the rider.");
            TestRunner.Equal(3, spawned.SiblingIndex, "The appended unrelated sibling was reordered.");
        }

        private static void SceneRootReturnsToExactSceneSlot()
        {
            var hierarchy = new FakeSiblingHierarchy();
            var areaScene = new FakeSiblingScene("area");
            var mountScene = new FakeSiblingScene("mounts");
            var first = hierarchy.Root("root-first", areaScene);
            var rider = hierarchy.Root("rider", areaScene);
            var last = hierarchy.Root("root-last", areaScene);
            var anchor = hierarchy.Root("anchor", mountScene);
            var lease = hierarchy.CreateLease();
            lease.Acquire(rider, anchor);
            TestRunner.Equal(3, areaScene.Roots.Count, "The root reservation did not hold the vacated scene slot.");
            TestRunner.Equal(1, areaScene.Roots.IndexOf(hierarchy.Reservations[0]), "The root reservation is not in the exact scene slot.");
            lease.Restore();
            TestRunner.True(ReferenceEquals(rider.RootScene, areaScene) && rider.Parent == null,
                "A scene-root rider was not returned to its exact original scene.");
            TestRunner.True(areaScene.Roots.SequenceEqual(new[] { first, rider, last }), "The scene root slot was not restored exactly.");
            TestRunner.True(hierarchy.DontDestroyOnLoad.Roots.Contains(hierarchy.Reservations[0]),
                "The released root reservation did not leave the original scene within the call.");
            hierarchy.EndFrame();
            TestRunner.Equal(0, hierarchy.LiveReservations, "The released root reservation was not destroyed.");
        }

        private static void FailedReservationCreationRollsBack()
        {
            var hierarchy = new FakeSiblingHierarchy { FailCreations = 1 };
            var parent = hierarchy.Root("dynamic-root");
            hierarchy.Child("before", parent);
            var rider = hierarchy.Child("rider", parent);
            var original = parent.Children.ToArray();
            var lease = hierarchy.CreateLease();
            var failed = false;
            try { lease.Acquire(rider, hierarchy.Root("anchor")); }
            catch (InvalidOperationException) { failed = true; }
            TestRunner.True(failed && lease.IsAcquired && !lease.HoldsSiblingReservation,
                "A failed reservation creation did not leave an owned, reservation-free rollback.");
            TestRunner.True(ReferenceEquals(rider.Parent, parent), "A failed reservation creation detached the rider.");
            lease.Restore();
            TestRunner.True(!lease.IsAcquired && lease.LastRestoreVerified && parent.Children.SequenceEqual(original),
                "Rollback after a failed reservation creation was not exact.");
        }

        private static void MisplacedReservationIsWithdrawn()
        {
            var hierarchy = new FakeSiblingHierarchy { MisplaceReservations = true };
            var parent = hierarchy.Root("dynamic-root");
            hierarchy.Child("first", parent);
            hierarchy.Child("second", parent);
            var rider = hierarchy.Child("rider", parent);
            var original = parent.Children.ToArray();
            var lease = hierarchy.CreateLease();
            var failed = false;
            try { lease.Acquire(rider, hierarchy.Root("anchor")); }
            catch (InvalidOperationException) { failed = true; }
            TestRunner.True(failed && ReferenceEquals(rider.Parent, parent),
                "A misplaced reservation did not stop acquisition before detachment.");
            hierarchy.MisplaceReservations = false;
            lease.Restore();
            hierarchy.EndFrame();
            TestRunner.True(!lease.IsAcquired && parent.Children.SequenceEqual(original) && hierarchy.LiveReservations == 0,
                "A misplaced reservation was not withdrawn exactly.");
        }

        private static void FailedDetachmentRestoresExactly()
        {
            var hierarchy = new FakeSiblingHierarchy { FailAttachments = 1 };
            var parent = hierarchy.Root("dynamic-root");
            hierarchy.Child("first", parent);
            var rider = hierarchy.Child("rider", parent);
            hierarchy.Child("last", parent);
            var original = parent.Children.ToArray();
            var lease = hierarchy.CreateLease();
            var failed = false;
            try { lease.Acquire(rider, hierarchy.Root("anchor")); }
            catch (InvalidOperationException) { failed = true; }
            TestRunner.True(failed && lease.IsAcquired && lease.HoldsSiblingReservation,
                "A failed detachment did not retain the owned reservation.");
            lease.Restore();
            hierarchy.EndFrame();
            TestRunner.True(!lease.IsAcquired && parent.Children.SequenceEqual(original) && hierarchy.LiveReservations == 0,
                "Rollback after a failed detachment was not exact.");
        }

        private static void UnvacatedSlotIsRefused()
        {
            var hierarchy = new FakeSiblingHierarchy { IgnoreAttachments = 1 };
            var parent = hierarchy.Root("dynamic-root");
            hierarchy.Child("first", parent);
            var rider = hierarchy.Child("rider", parent);
            hierarchy.Child("last", parent);
            var original = parent.Children.ToArray();
            var lease = hierarchy.CreateLease();
            var failed = false;
            try { lease.Acquire(rider, hierarchy.Root("anchor")); }
            catch (InvalidOperationException) { failed = true; }
            TestRunner.True(failed && lease.IsAcquired && lease.HoldsSiblingReservation,
                "A detachment that left the rider in its slot was accepted.");
            lease.Restore();
            hierarchy.EndFrame();
            TestRunner.True(!lease.IsAcquired && parent.Children.SequenceEqual(original) && hierarchy.LiveReservations == 0,
                "Rollback after an unvacated slot was not exact.");
        }

        private static void FailedRestoreRetainsReservation()
        {
            var fixture = Batch187Layout();
            var leases = fixture.Riders.Select(rider => fixture.Hierarchy.CreateLease()).ToArray();
            for (var index = 0; index < 6; index++) leases[index].Acquire(fixture.Riders[index], fixture.Anchors[index]);
            fixture.Riders[3].FailNextRotationWrite = true;
            var failed = false;
            for (var index = 0; index < 6; index++)
            {
                try { leases[index].Restore(); }
                catch (AggregateException) { failed = index == 3; }
            }
            TestRunner.True(failed && leases[3].IsAcquired && leases[3].HoldsSiblingReservation && leases[3].SiblingReservationIntact,
                "A failed restoration released its reservation or lease.");
            fixture.Hierarchy.EndFrame();
            TestRunner.Equal(1, fixture.Hierarchy.LiveReservations, "Only the failed rider's reservation should remain.");
            leases[3].Restore();
            fixture.Hierarchy.EndFrame();
            fixture.RequireOriginalLayout("retry after a failed restoration");
        }

        private static void FailedReservationReleaseRetainsCustody()
        {
            var hierarchy = new FakeSiblingHierarchy();
            var parent = hierarchy.Root("dynamic-root");
            hierarchy.Child("first", parent);
            var rider = hierarchy.Child("rider", parent);
            hierarchy.Child("last", parent);
            var original = parent.Children.ToArray();
            var lease = hierarchy.CreateLease();
            lease.Acquire(rider, hierarchy.Root("anchor"));
            hierarchy.FailDestroys = 1;
            var failed = false;
            try { lease.Restore(); }
            catch (AggregateException) { failed = true; }
            TestRunner.True(failed && lease.IsAcquired && lease.HoldsSiblingReservation && !lease.LastRestoreVerified,
                "A failed reservation release discarded custody.");
            TestRunner.Equal(1, rider.SiblingIndex, "The verified rider moved while its reservation was retained.");
            lease.Restore();
            hierarchy.EndFrame();
            TestRunner.True(!lease.IsAcquired && !lease.HoldsSiblingReservation && parent.Children.SequenceEqual(original) &&
                hierarchy.LiveReservations == 0, "A retried reservation release was not exact.");
        }

        private static void VerifiedRiderStaysAfterPartialRelease()
        {
            var hierarchy = new FakeSiblingHierarchy();
            var parent = hierarchy.Root("dynamic-root");
            var early = hierarchy.Child("early", parent);
            var before = hierarchy.Child("before", parent);
            var rider = hierarchy.Child("rider", parent);
            var after = hierarchy.Child("after", parent);
            var lease = hierarchy.CreateLease();
            lease.Acquire(rider, hierarchy.Root("anchor"));
            hierarchy.FailDestroysAfterRemoval = 1;
            var failed = false;
            try { lease.Restore(); }
            catch (AggregateException) { failed = true; }
            TestRunner.True(failed && lease.IsAcquired && lease.HoldsSiblingReservation, "A release that left the slot then failed discarded custody.");
            early.Leave();
            lease.Restore();
            hierarchy.EndFrame();
            TestRunner.True(!lease.IsAcquired && parent.Children.SequenceEqual(new[] { before, rider, after }) && hierarchy.LiveReservations == 0,
                "A verified rider was repositioned by the stale captured ordinal on retry: " + string.Join(",", parent.Children.Select(node => node.Name)));
        }

        private static void VerifiedRiderStaysAfterFailedPostReleaseRead()
        {
            var hierarchy = new FakeSiblingHierarchy();
            var parent = hierarchy.Root("dynamic-root");
            var early = hierarchy.Child("early", parent);
            var before = hierarchy.Child("before", parent);
            var rider = hierarchy.Child("rider", parent);
            var after = hierarchy.Child("after", parent);
            var lease = hierarchy.CreateLease();
            lease.Acquire(rider, hierarchy.Root("anchor"));
            hierarchy.ArmIndexFaultOnRelease = true;
            var failed = false;
            try { lease.Restore(); }
            catch (AggregateException) { failed = true; }
            TestRunner.True(failed && lease.IsAcquired && !lease.HoldsSiblingReservation, "A failed post-release verification lost its retained lease.");
            early.Leave();
            lease.Restore();
            hierarchy.EndFrame();
            TestRunner.True(!lease.IsAcquired && parent.Children.SequenceEqual(new[] { before, rider, after }),
                "A verified rider was repositioned by the stale captured ordinal on retry: " + string.Join(",", parent.Children.Select(node => node.Name)));
        }

        private static void PreDetachRollbackToleratesUnrelatedChanges()
        {
            var hierarchy = new FakeSiblingHierarchy { MisplaceReservations = true };
            var parent = hierarchy.Root("dynamic-root");
            var early = hierarchy.Child("early", parent);
            var before = hierarchy.Child("before", parent);
            var rider = hierarchy.Child("rider", parent);
            var after = hierarchy.Child("after", parent);
            var lease = hierarchy.CreateLease();
            var refused = false;
            try { lease.Acquire(rider, hierarchy.Root("anchor")); }
            catch (InvalidOperationException) { refused = true; }
            hierarchy.MisplaceReservations = false;
            hierarchy.FailDestroys = 1;
            var compensationFailed = false;
            try { lease.Restore(); }
            catch (AggregateException) { compensationFailed = true; }
            // The game keeps running: an unrelated sibling leaves and the never-moved rider walks.
            early.Leave();
            rider.WorldPosition = "moved-by-native-movement";
            lease.Restore();
            hierarchy.EndFrame();
            TestRunner.True(refused && compensationFailed && !lease.IsAcquired && !lease.HoldsSiblingReservation &&
                parent.Children.SequenceEqual(new[] { before, rider, after }) && hierarchy.LiveReservations == 0 &&
                rider.WorldPosition == "moved-by-native-movement",
                "A pre-detachment rollback retry depended on the stale ordinal or pose it never wrote.");
        }

        private static void ThirdPartyReservationLossUsesExactOrdinal()
        {
            var hierarchy = new FakeSiblingHierarchy();
            var parent = hierarchy.Root("dynamic-root");
            hierarchy.Child("first", parent);
            hierarchy.Child("second", parent);
            var rider = hierarchy.Child("rider", parent);
            hierarchy.Child("last", parent);
            var original = parent.Children.ToArray();
            var lease = hierarchy.CreateLease();
            lease.Acquire(rider, hierarchy.Root("anchor"));
            hierarchy.DestroyByThirdParty(hierarchy.Reservations[0]);
            TestRunner.True(!lease.SiblingReservationIntact, "A destroyed reservation was still reported intact.");
            lease.Restore();
            TestRunner.True(lease.LastRestoreVerified && !lease.LastRestoreUsedReservation && lease.LastRestoreUsedCapturedOrdinal && !lease.HoldsSiblingReservation &&
                parent.Children.SequenceEqual(original), "Captured-ordinal fallback was not exact or not reported.");
        }

        private static void ThirdPartyReservationLossRefusesClampedOrdinal()
        {
            var hierarchy = new FakeSiblingHierarchy();
            var parent = hierarchy.Root("dynamic-root");
            var first = hierarchy.Child("first", parent);
            var second = hierarchy.Child("second", parent);
            var rider = hierarchy.Child("rider", parent);
            var lease = hierarchy.CreateLease();
            lease.Acquire(rider, hierarchy.Root("anchor"));
            hierarchy.DestroyByThirdParty(hierarchy.Reservations[0]);
            first.Leave();
            second.Leave();
            for (var attempt = 0; attempt < 2; attempt++)
            {
                var failed = false;
                try { lease.Restore(); }
                catch (AggregateException) { failed = true; }
                TestRunner.True(failed && lease.IsAcquired && !lease.LastRestoreVerified,
                    "A clamped captured ordinal was accepted after reservation loss, attempt " + attempt + ".");
            }
        }

        private static void ReplacementTakesNativeLastPlacement()
        {
            var hierarchy = new FakeSiblingHierarchy();
            var parent = hierarchy.Root("dynamic-root");
            var before = hierarchy.Child("before", parent);
            var rider = hierarchy.Child("old-rider", parent);
            var neighbor = hierarchy.Child("neighbor-rider", parent);
            var after = hierarchy.Child("after", parent);
            var anchor = hierarchy.Root("anchor");
            var neighborAnchor = hierarchy.Root("neighbor-anchor");
            var lease = hierarchy.CreateLease();
            var neighborLease = hierarchy.CreateLease();
            lease.Acquire(rider, anchor);
            neighborLease.Acquire(neighbor, neighborAnchor);
            var replacement = hierarchy.Child("stock-polymorph-replacement", anchor);
            replacement.WorldPosition = "replacement-position";
            replacement.WorldRotation = "replacement-rotation";
            var reservedSlot = hierarchy.Reservations[0].SiblingIndex;

            TestRunner.True(lease.ReleaseInheritedReplacement(replacement), "Inherited replacement was not released.");
            TestRunner.True(ReferenceEquals(parent, replacement.Parent) && parent.Children.Last() == replacement,
                "Replacement did not take the native last-sibling placement in the captured parent.");
            TestRunner.Equal(reservedSlot, hierarchy.Reservations[0].SiblingIndex, "Replacement moved the reserved slot.");
            TestRunner.True(replacement.WorldPosition == "replacement-position" && replacement.WorldRotation == "replacement-rotation",
                "Replacement world pose changed.");
            var foreign = hierarchy.Child("foreign", parent);
            TestRunner.True(!lease.ReleaseInheritedReplacement(foreign), "A view outside the owned anchor was reparented.");

            neighborLease.Restore();
            lease.Restore();
            hierarchy.EndFrame();
            TestRunner.True(parent.Children.SequenceEqual(new[] { before, rider, neighbor, after, replacement, foreign }),
                "Old-view and neighbor slots were not restored exactly around the released replacement.");
        }

        private static Batch187Fixture Batch187Layout()
        {
            // Rebuild the original sixteen-child order: ten anonymous children,
            // then each rider placed where batch187 captured it with the earlier
            // riders absent (reverse insertion reproduces those ordinals).
            var hierarchy = new FakeSiblingHierarchy();
            var parent = hierarchy.Root("native-shared-parent");
            for (var index = 0; index < Batch187OtherChildren; index++) hierarchy.Child("native-other-" + index, parent);
            var riders = Enumerable.Range(0, 6).Select(index => new FakeSiblingNode("rider-" + index)).ToArray();
            for (var index = 5; index >= 0; index--)
            {
                riders[index].SetParent(parent);
                riders[index].SetSiblingIndex(Batch187CaptureOrdinals[index]);
            }
            var anchors = Enumerable.Range(0, 6).Select(index => hierarchy.Root("anchor-" + index)).ToArray();
            return new Batch187Fixture(hierarchy, parent, riders, anchors);
        }

        private static IEnumerable<int[]> Permutations(int[] values)
        {
            if (values.Length <= 1)
            {
                yield return values;
                yield break;
            }
            for (var index = 0; index < values.Length; index++)
            {
                var rest = values.Where((value, position) => position != index).ToArray();
                foreach (var tail in Permutations(rest))
                {
                    yield return new[] { values[index] }.Concat(tail).ToArray();
                }
            }
        }

        private sealed class Batch187Fixture
        {
            private readonly FakeSiblingNode[] original;

            public Batch187Fixture(FakeSiblingHierarchy hierarchy, FakeSiblingNode parent, FakeSiblingNode[] riders, FakeSiblingNode[] anchors)
            {
                Hierarchy = hierarchy;
                Parent = parent;
                Riders = riders;
                Anchors = anchors;
                original = parent.Children.ToArray();
            }

            public FakeSiblingHierarchy Hierarchy { get; }
            public FakeSiblingNode Parent { get; }
            public FakeSiblingNode[] Riders { get; }
            public FakeSiblingNode[] Anchors { get; }

            public void RequireOriginalLayout(string context)
            {
                TestRunner.True(Parent.Children.SequenceEqual(original),
                    "Shared parent order differs after " + context + ": " + string.Join(",", Parent.Children.Select(node => node.Name)));
                TestRunner.Equal(0, Hierarchy.LiveReservations, "Reservations remained after " + context + ".");
            }
        }
    }

    // List-backed model of the Unity sibling semantics the runtime adapter uses:
    // SetParent appends (and is a no-op for the current parent), a parentless
    // node joins the end of its hierarchy's scene roots, a sibling index is the
    // node's resulting clamped position, and Destroy completes at frame end.
    internal sealed class FakeSiblingScene
    {
        internal FakeSiblingScene(string name) { Name = name; }
        internal string Name { get; }
        internal readonly List<FakeSiblingNode> Roots = new List<FakeSiblingNode>();
        public override string ToString() => Name;
    }

    internal sealed class FakeSiblingNode
    {
        internal FakeSiblingNode(string name) { Name = name; }
        internal string Name { get; }
        internal FakeSiblingNode Parent { get; private set; }
        internal FakeSiblingScene RootScene { get; private set; }
        internal readonly List<FakeSiblingNode> Children = new List<FakeSiblingNode>();
        internal bool Destroyed { get; set; }
        internal string WorldPosition { get; set; } = "position";
        internal string WorldRotation { get; set; } = "rotation";
        internal string LocalScale { get; set; } = "scale";
        internal bool FailNextRotationWrite { get; set; }
        internal double PositionWriteBias { get; set; }
        internal double RotationWriteBias { get; set; }
        internal double ScaleWriteBias { get; set; }
        internal FakeSiblingScene Scene => Parent != null ? Parent.Scene : RootScene;
        internal object Container => Parent != null ? (object)Parent : RootScene;
        internal List<FakeSiblingNode> Siblings => Parent != null ? Parent.Children : RootScene?.Roots;
        internal int SiblingIndex => Siblings?.IndexOf(this) ?? -1;

        internal void JoinScene(FakeSiblingScene scene)
        {
            Leave();
            RootScene = scene;
            scene.Roots.Add(this);
        }

        internal void SetParent(FakeSiblingNode parent)
        {
            if (Destroyed) throw new InvalidOperationException("Destroyed node " + Name + ".");
            if (parent != null && ReferenceEquals(parent, Parent) || parent == null && Parent == null && RootScene != null) return;
            var scene = Scene;
            Leave();
            if (parent != null)
            {
                Parent = parent;
                parent.Children.Add(this);
            }
            else
            {
                RootScene = scene;
                scene.Roots.Add(this);
            }
        }

        internal void SetSiblingIndex(int index)
        {
            var list = Siblings;
            list.Remove(this);
            list.Insert(Math.Max(0, Math.Min(index, list.Count)), this);
        }

        internal void MoveToScene(FakeSiblingScene scene)
        {
            if (Parent != null) throw new InvalidOperationException("Only a scene root can move between scenes.");
            if (ReferenceEquals(RootScene, scene)) return;
            JoinScene(scene);
        }

        internal void Leave()
        {
            if (Parent != null)
            {
                Parent.Children.Remove(this);
                Parent = null;
            }
            else if (RootScene != null)
            {
                RootScene.Roots.Remove(this);
                RootScene = null;
            }
        }
    }

    internal sealed class FakeSiblingHierarchy
    {
        internal readonly FakeSiblingScene Active = new FakeSiblingScene("active");
        internal readonly FakeSiblingScene DontDestroyOnLoad = new FakeSiblingScene("DontDestroyOnLoad");
        internal readonly List<FakeSiblingNode> Reservations = new List<FakeSiblingNode>();
        private readonly List<FakeSiblingNode> pendingDestroy = new List<FakeSiblingNode>();
        internal int FailCreations { get; set; }
        internal int FailDestroys { get; set; }
        internal int FailAttachments { get; set; }
        internal int FailDestroysAfterRemoval { get; set; }
        internal bool ArmIndexFaultOnRelease { get; set; }
        private bool failNextIndexRead;
        internal int IgnoreAttachments { get; set; }
        internal bool MisplaceReservations { get; set; }
        internal int LiveReservations => Reservations.Count(node => !node.Destroyed);

        internal FakeSiblingNode Root(string name, FakeSiblingScene scene = null)
        {
            var node = new FakeSiblingNode(name);
            node.JoinScene(scene ?? Active);
            return node;
        }

        internal FakeSiblingNode Child(string name, FakeSiblingNode parent)
        {
            var node = new FakeSiblingNode(name);
            node.SetParent(parent);
            return node;
        }

        internal void DestroyByThirdParty(FakeSiblingNode node)
        {
            node.Leave();
            node.Destroyed = true;
        }

        internal void EndFrame()
        {
            foreach (var node in pendingDestroy)
            {
                node.Leave();
                node.Destroyed = true;
            }
            pendingDestroy.Clear();
        }

        internal ScopedTransformAttachmentLease<FakeSiblingNode, string, string, string> CreateLease()
        {
            return new ScopedTransformAttachmentLease<FakeSiblingNode, string, string, string>(
                node => node.Parent,
                ReadSiblingIndex,
                node => node.WorldPosition,
                node => node.WorldRotation,
                node => node.LocalScale,
                Attach,
                SetSiblingIndex,
                (node, position) => node.WorldPosition = position,
                (node, rotation) =>
                {
                    if (node.FailNextRotationWrite)
                    {
                        node.FailNextRotationWrite = false;
                        throw new InvalidOperationException("injected rotation failure");
                    }
                    node.WorldRotation = rotation;
                },
                (node, scale) => node.LocalScale = scale,
                node => node.Container,
                MoveToContainer,
                CreateReservation,
                node => !node.Destroyed,
                DestroyReservation);
        }

        internal ScopedTransformAttachmentLease<FakeSiblingNode, double, double, double> CreateNumericLease(
            IEqualityComparer<double> positionComparer,
            IEqualityComparer<double> rotationComparer,
            IEqualityComparer<double> scaleComparer)
        {
            var culture = System.Globalization.CultureInfo.InvariantCulture;
            return new ScopedTransformAttachmentLease<FakeSiblingNode, double, double, double>(
                node => node.Parent,
                node => node.SiblingIndex,
                node => double.Parse(node.WorldPosition, culture),
                node => double.Parse(node.WorldRotation, culture),
                node => double.Parse(node.LocalScale, culture),
                Attach,
                SetSiblingIndex,
                (node, position) => node.WorldPosition = (position + node.PositionWriteBias).ToString("R", culture),
                (node, rotation) => node.WorldRotation = (rotation + node.RotationWriteBias).ToString("R", culture),
                (node, scale) => node.LocalScale = (scale + node.ScaleWriteBias).ToString("R", culture),
                node => node.Container,
                MoveToContainer,
                CreateReservation,
                node => !node.Destroyed,
                DestroyReservation,
                null,
                positionComparer,
                rotationComparer,
                scaleComparer);
        }

        private void Attach(FakeSiblingNode node, FakeSiblingNode parent, bool worldPositionStays)
        {
            if (FailAttachments > 0)
            {
                FailAttachments--;
                throw new InvalidOperationException("injected attachment failure");
            }
            if (IgnoreAttachments > 0)
            {
                IgnoreAttachments--;
                return;
            }
            node.SetParent(parent);
        }

        private int ReadSiblingIndex(FakeSiblingNode node)
        {
            if (failNextIndexRead)
            {
                failNextIndexRead = false;
                throw new InvalidOperationException("injected sibling index read failure");
            }
            return node.SiblingIndex;
        }

        private void SetSiblingIndex(FakeSiblingNode node, int index)
        {
            node.SetSiblingIndex(MisplaceReservations && Reservations.Contains(node) ? 0 : index);
        }

        private static void MoveToContainer(FakeSiblingNode node, object container)
        {
            var parent = container as FakeSiblingNode;
            if (parent != null)
            {
                node.SetParent(parent);
                return;
            }
            node.SetParent(null);
            node.MoveToScene((FakeSiblingScene)container);
        }

        private FakeSiblingNode CreateReservation()
        {
            if (FailCreations > 0)
            {
                FailCreations--;
                throw new InvalidOperationException("injected reservation creation failure");
            }
            var reservation = Root("reservation-" + Reservations.Count);
            Reservations.Add(reservation);
            return reservation;
        }

        private void DestroyReservation(FakeSiblingNode reservation)
        {
            if (FailDestroys > 0)
            {
                FailDestroys--;
                throw new InvalidOperationException("injected reservation destruction failure");
            }
            if (reservation.Parent != null) reservation.SetParent(null);
            else reservation.MoveToScene(DontDestroyOnLoad);
            if (FailDestroysAfterRemoval > 0)
            {
                FailDestroysAfterRemoval--;
                throw new InvalidOperationException("injected destruction failure after leaving the slot");
            }
            pendingDestroy.Add(reservation);
            if (ArmIndexFaultOnRelease)
            {
                ArmIndexFaultOnRelease = false;
                failNextIndexRead = true;
            }
        }
    }
}
