using System;
using System.Collections.Generic;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    internal static class MountedPairOwnershipTests
    {
        internal static void Register(TestRunner runner)
        {
            runner.Run("pair ownership admits two Medium relationships and atomically refuses duplicate roles", Admission);
            runner.Run("pair request retains exact owner across another selection and rejects replacement generation", Routing);
            runner.Run("pair-local rollback retains failed cleanup and shared actor expenditure", CleanupAndAllocations);
            runner.Run("save and teardown readiness include every retained owner without changing grants", Readiness);
            runner.Run("native capacity refuses before attachment and does not truncate registry records", NativeCapacity);
            runner.Run("ownership session replacement refuses until prior custody settles", Sessions);
            runner.Run("legacy pending-split survivor remains addressable without active membership", PendingSurvivor);
            runner.Run("persisted custody imports exact IDs and generations without native attachment or grants", StableImport);
            runner.Run("failed retirement retains its owner and cannot skip independent settled custody", RetirementIsolation);
        }

        private sealed class Actor { internal string Id = Guid.NewGuid().ToString("N"); }
        // Substitutes only the unavailable Unity movement/view lease boundary.
        private sealed class Runtime : IMountedPairRuntime
        {
            internal int Acquisitions;
            internal bool FailAttach, FailCleanup, Residue;
            public void AcquireMovementAuthority(MountedPair pair) { Acquisitions++; Residue = true; }
            public void AttachPresentation(MountedPair pair) { if (FailAttach) throw new InvalidOperationException("view unavailable"); }
            public void RestorePresentation(MountedPair pair) { if (FailCleanup) throw new InvalidOperationException("view still owned"); }
            public void RestoreMovementAuthority(MountedPair pair, CleanupTrigger trigger)
            { if (FailCleanup) throw new InvalidOperationException("movement still owned"); Residue = false; }
        }
        private sealed class Boundary
        {
            internal readonly Actor Rider = new Actor(), Mount = new Actor();
            internal readonly Runtime Runtime = new Runtime();
            internal readonly MountedRelationshipCoordinator Relationship;
            internal bool Participation, Preparing, Transition, Fence, ChargeCleanup, CommandCleanup;
            internal Boundary() { Relationship = new MountedRelationshipCoordinator(Runtime); }
            internal bool Retained => Runtime.Residue || Participation || ChargeCleanup || CommandCleanup;
        }
        private sealed class World
        {
            internal readonly object Session = new object();
            internal readonly MountedPairRegistry<Actor, Boundary> Registry = new MountedPairRegistry<Actor, Boundary>(a => a.Id);
            internal readonly Dictionary<Actor, MountedMovementState> Allocations = new Dictionary<Actor, MountedMovementState>();
            internal readonly ActorAllocationLifetime<Actor, MountedMovementState> Lifetime;
            internal World() { Lifetime = new ActorAllocationLifetime<Actor, MountedMovementState>(Allocations); Lifetime.SynchronizeSession(Session, "campaign"); }
            internal MountedPairOwner<Actor, Boundary> Reserve(Boundary pair, long generation = 1, int capacity = 6)
            {
                TestRunner.True(Registry.TryReserve(Session, "campaign", pair.Rider, pair.Mount, generation, pair,
                    () => pair.Retained, () => !pair.Preparing && !pair.Transition && !pair.ChargeCleanup, () => !pair.Fence,
                    out var owner, out var refusal, capacity), refusal);
                return owner;
            }
            internal MountedPairOwner<Actor, Boundary> Mount(Boundary pair, long generation = 1, int capacity = 6)
            {
                var owner = Reserve(pair, generation, capacity);
                var result = pair.Relationship.Mount(Candidate(pair));
                if (result.Succeeded) Registry.Commit(owner); else Registry.Detach(owner);
                Registry.RetireSettled(); return owner;
            }
        }
        private static MountedPairCandidate Candidate(Boundary p) => new MountedPairCandidate(p.Rider.Id, p.Mount.Id)
        {
            RiderIsDirectlyControllable = true, MountIsDirectlyControllable = true,
            RiderIsAliveAndConscious = true, MountIsAliveAndConscious = true,
            ExactReciprocalCompanionRelationship = true, RiderSizeOrdinal = 4, MountSizeOrdinal = 5,
            RiderViewAndStockAgentAvailable = true, MountViewAndStockAgentAvailable = true,
            RiderStockAgentEnabled = true, MountStockAgentEnabled = true,
            RiderAgentOverrideAvailable = true, MountAgentOverrideAvailable = true,
            RiderIsExactlyMedium = true, SafeMovementMode = true
        };
        private static void Admission()
        {
            var world = new World(); var a = new Boundary(); var b = new Boundary();
            var first = world.Mount(a); var second = world.Mount(b);
            TestRunner.True(ReferenceEquals(first, world.Registry.FindActive(a.Rider)) &&
                ReferenceEquals(first, world.Registry.FindActive(a.Mount)) &&
                ReferenceEquals(second, world.Registry.FindActive(b.Rider)) &&
                ReferenceEquals(second, world.Registry.FindActive(b.Mount)), "Both roles must resolve their exact owner.");
            foreach (var actors in new[] { new[] { a.Rider, b.Mount }, new[] { b.Mount, a.Rider }, new[] { a.Rider, a.Rider },
                new[] { new Actor { Id = Guid.Parse(a.Rider.Id).ToString("D") }, new Actor() } })
                TestRunner.True(!world.Registry.TryReserve(world.Session, "campaign", actors[0], actors[1], 2, b,
                    () => false, () => true, () => true, out _, out _), "Duplicate/cross-role alias entered the registry.");
            TestRunner.Equal(2, world.Registry.ActiveCount, "Refusal changed existing membership.");
            TestRunner.Equal(1, a.Runtime.Acquisitions, "Unrelated actor acquired a second lease.");
            TestRunner.Equal(null, world.Registry.FindOwner(new Actor { Id = a.Rider.Id }), "Stable ID cannot substitute a native object.");
        }
        private static void Routing()
        {
            var world = new World(); var a = new Boundary(); var b = new Boundary();
            var first = world.Mount(a); world.Mount(b);
            var command = new object(); var context = new object();
            var bindings = new System.Runtime.CompilerServices.ConditionalWeakTable<object, MountedPairRequest<Actor, Boundary>>();
            var request = world.Registry.CaptureRequest(world.Session, "campaign", a.Rider, a.Mount, 1, a, true);
            bindings.Add(command, request); bindings.Add(context, request);
            TestRunner.True(world.Registry.IsActiveOwner(first, a.Rider, a.Mount) &&
                !world.Registry.IsActiveOwner(first, a.Mount, a.Rider) &&
                world.Registry.CaptureRequest(world.Session, "campaign", a.Mount, a.Mount, 1, a, true) == null,
                "Request admitted the mount as its own rider.");
            var c = new Boundary();
            var next = world.Registry.CaptureRequest(world.Session, "campaign", c.Rider, c.Mount, 0, c, false);
            TestRunner.True(world.Registry.TryResolve(next, world.Session, "campaign", 0, out var nextBoundary) &&
                ReferenceEquals(nextBoundary, c) &&
                world.Registry.CaptureRequest(world.Session, "campaign", c.Rider, c.Mount, 0, c, false, 1) == null,
                "Routing must respect the adapter capacity without limiting unrelated domain ownership.");
            var selected = b; // request context may change, admitted command ownership may not
            TestRunner.True(bindings.TryGetValue(context, out var delivered) &&
                world.Registry.TryResolve(delivered, world.Session, "campaign", 1, out var boundary) &&
                ReferenceEquals(boundary, a) && !ReferenceEquals(boundary, selected), "Delayed A delivery routed into B.");
            a.Relationship.Dismount(CleanupTrigger.Manual); world.Registry.Detach(first); world.Registry.RetireSettled();
            var replacement = world.Mount(a, 2);
            TestRunner.True(!world.Registry.TryResolve(delivered, world.Session, "campaign", 2, out _) &&
                !world.Registry.TryResolve(delivered, world.Session, "campaign", 1, out _) &&
                !ReferenceEquals(first, replacement) && !world.Registry.IsActiveOwner(first, a.Rider, a.Mount) &&
                world.Registry.IsActiveOwner(replacement, a.Rider, a.Mount), "Old callback entered a replacement owner.");
        }
        private static void CleanupAndAllocations()
        {
            var world = new World(); var a = new Boundary(); var b = new Boundary(); var first = world.Mount(a);
            world.Allocations[b.Mount] = new MountedMovementState { TimeMoved = 3.125f, TimeForced = 1f };
            var history = world.Allocations[b.Mount];
            b.Runtime.FailAttach = true; b.Runtime.FailCleanup = true;
            var failed = world.Mount(b);
            TestRunner.Equal(MountedPairOwnershipState.Detached, failed.State, "Fault discarded cleanup custody.");
            TestRunner.Equal(null, world.Registry.FindActive(b.Mount), "Fault remains an active relationship.");
            TestRunner.True(ReferenceEquals(failed, world.Registry.FindOwner(b.Mount)) &&
                ReferenceEquals(first, world.Registry.FindActive(a.Rider)), "B failure damaged A or lost B.");
            TestRunner.True(world.Registry.AdmissionRefusal(world.Session, "campaign", b.Rider, b.Mount) != null,
                "Conflicting actors reused before cleanup.");
            var c = new Boundary(); world.Mount(c); // retained B cannot block unrelated domain admission
            b.Runtime.FailAttach = false; b.Runtime.FailCleanup = false;
            TestRunner.True(b.Relationship.Dismount(CleanupTrigger.Exception).Succeeded, "Cleanup retry failed.");
            b.Participation = true; world.Registry.RetireSettled();
            TestRunner.True(world.Registry.Owns(failed), "Split participation released with the view lease.");
            b.Participation = false; b.CommandCleanup = true; world.Registry.RetireSettled();
            TestRunner.True(world.Registry.Owns(failed), "Finished command pending sweep lost custody.");
            b.CommandCleanup = false; world.Registry.RetireSettled(); world.Mount(b, 2);
            world.Lifetime.SynchronizeSession(world.Session, "campaign");
            TestRunner.True(ReferenceEquals(history, world.Allocations[b.Mount]) && history.TimeMoved == 3.125f,
                "Dismount/re-pair reset expenditure.");
        }
        private static void Readiness()
        {
            var world = new World(); var a = new Boundary(); var b = new Boundary();
            var first = world.Mount(a); var second = world.Mount(b);
            a.Preparing = true; b.Transition = true; b.Fence = true;
            TestRunner.True(!world.Registry.AllSaveReady() && !world.Registry.AllTeardownReady(), "Aggregate lost an existing refusal.");
            a.Preparing = false;
            TestRunner.True(!world.Registry.AllSaveReady(), "Settling A erased B transition.");
            b.Relationship.Dismount(CleanupTrigger.Manual); b.Participation = true; world.Registry.Detach(second);
            TestRunner.True(!world.Registry.AllSaveReady(), "Detached owner disappeared from readiness.");
            b.Transition = false; b.Fence = false; b.ChargeCleanup = true;
            TestRunner.True(!world.Registry.AllSaveReady(), "Retained Charge cleanup lost its save barrier.");
            b.ChargeCleanup = false;
            TestRunner.True(world.Registry.AllSaveReady() && world.Registry.AllTeardownReady(),
                "Settled combat/participation gained a blanket refusal.");
            var visited = 0;
            var errors = world.Registry.VisitOwners(owner => { visited++; if (ReferenceEquals(owner, first)) throw new Exception("release failed"); });
            TestRunner.True(visited == 2 && errors.Length == 1 && world.Registry.Owns(first) && world.Registry.Owns(second),
                "One failed release skipped or retired another owner.");
        }
        private static void NativeCapacity()
        {
            var world = new World(); var a = new Boundary(); var b = new Boundary(); world.Mount(a, 1, 1);
            TestRunner.True(!world.Registry.TryReserve(world.Session, "campaign", b.Rider, b.Mount, 1, b,
                () => b.Retained, () => true, () => true, out _, out _, 1), "Second native pair admitted.");
            TestRunner.Equal(0, b.Runtime.Acquisitions, "Capacity refusal happened after attachment.");
            TestRunner.Equal(1, world.Registry.Owners.Length, "Capacity refusal mutated custody.");
        }
        private static void Sessions()
        {
            var world = new World(); var a = new Boundary(); var owner = world.Mount(a);
            var request = world.Registry.CaptureRequest(world.Session, "campaign", a.Rider, a.Mount, 1, a, true);
            TestRunner.True(!world.Registry.TryResolve(request, new object(), "campaign", 1, out _) &&
                !world.Registry.TryResolve(request, world.Session, "another-campaign", 1, out _), "Session/campaign alias delivered a stale command.");
            TestRunner.True(world.Registry.AdmissionRefusal(new object(), "campaign", new Actor(), new Actor()) != null,
                "New world silently discarded old obligations.");
            a.Relationship.Dismount(CleanupTrigger.Manual); world.Registry.Detach(owner); world.Registry.RetireSettled();
            TestRunner.Equal(null, world.Registry.AdmissionRefusal(new object(), "new-campaign", new Actor(), new Actor()),
                "Settled previous world prevented new ownership.");
        }
        private static void PendingSurvivor()
        {
            var world = new World(); var b = new Boundary(); b.Participation = true;
            TestRunner.True(world.Registry.TryRetainDetached(world.Session, "campaign", b.Rider, null, 1, b,
                () => b.Participation, () => true, () => true, out var owner), "Pending survivor was dropped.");
            TestRunner.True(ReferenceEquals(owner, world.Registry.FindOwner(b.Rider)) && world.Registry.FindActive(b.Rider) == null,
                "Pending survivor became active membership.");
            b.Participation = false; world.Registry.RetireSettled();
            TestRunner.True(!world.Registry.TryRetainDetached(world.Session, "campaign", new Actor { Id = " " }, null, 1, b,
                () => true, () => true, () => true, out _), "Detached custody accepted an empty actor identity.");
            TestRunner.True(world.Registry.FindOwner(b.Rider) == null, "Settled survivor remains reserved.");
        }
        private static void RetirementIsolation()
        {
            var world = new World(); var a = new Boundary(); var b = new Boundary();
            var fail = true; var aQueries = 0; var bQueries = 0;
            TestRunner.True(world.Registry.TryReserve(world.Session, "campaign", a.Rider, a.Mount, 9, a,
                () => { aQueries++; if (fail) throw new InvalidOperationException("retained A unavailable"); return false; },
                () => true, () => true, out var first, out _), "First owner was not reserved.");
            TestRunner.True(world.Registry.TryReserve(world.Session, "campaign", b.Rider, b.Mount, 17, b,
                () => { bQueries++; return false; }, () => true, () => true, out var second, out _), "Second owner was not reserved.");
            world.Registry.Detach(first); world.Registry.Detach(second);
            Exception failure = null;
            try { world.Registry.RetireSettled(); } catch (Exception error) { failure = error; }
            TestRunner.True(failure is AggregateException && ((AggregateException)failure).InnerExceptions.Count == 1 &&
                aQueries == 1 && bQueries == 1 && first.State == MountedPairOwnershipState.Detached &&
                ReferenceEquals(first, world.Registry.FindOwner(a.Rider)) && ReferenceEquals(first, world.Registry.FindOwner(a.Mount)) &&
                second.State == MountedPairOwnershipState.Retired && world.Registry.FindOwner(b.Rider) == null &&
                world.Registry.FindOwner(b.Mount) == null && world.Registry.Owners.Length == 1,
                "A failing custody predicate skipped B retirement, lost A, or hid its error.");
            fail = false; world.Registry.RetireSettled();
            TestRunner.True(aQueries == 2 && bQueries == 1 && first.State == MountedPairOwnershipState.Retired &&
                world.Registry.Owners.Length == 0, "A retry replayed independently completed B retirement or retained settled A.");
        }
        private static void StableImport()
        {
            var world = new World(); var a = new Boundary(); var b = new Boundary();
            var id = Guid.NewGuid();
            TestRunner.True(world.Registry.TryImport(world.Session, "campaign", id, 73, a.Rider, a.Mount, a, true,
                () => a.Retained, () => true, () => true, out var imported, out var refusal), refusal);
            TestRunner.True(imported.Id == id && imported.Generation == 73 && imported.State == MountedPairOwnershipState.Reserved,
                "Import did not preserve the exact reserved custody identity.");
            TestRunner.Equal(0, a.Runtime.Acquisitions, "Custody import must not attach or acquire a movement lease.");
            TestRunner.True(world.Registry.FindActive(a.Rider) == null, "Import activated before saved presentation committed.");
            TestRunner.True(!world.Registry.TryImport(world.Session, "campaign", id, 74, b.Rider, b.Mount, b, true,
                () => false, () => true, () => true, out _, out _), "Duplicate persistent owner identity admitted.");
            TestRunner.True(!world.Registry.TryImport(world.Session, "campaign", Guid.NewGuid(), 0, b.Rider, b.Mount, b, true,
                () => false, () => true, () => true, out _, out _), "Invalid persisted generation admitted.");
            TestRunner.True(!world.Registry.TryImport(new object(), "campaign", Guid.NewGuid(), 1, b.Rider, b.Mount, b, true,
                () => false, () => true, () => true, out _, out _), "Foreign world import bypassed retained session custody.");
            TestRunner.True(!world.Registry.TryImport(world.Session, "campaign", Guid.NewGuid(), 1, a.Mount, b.Mount, b, false,
                () => true, () => true, () => true, out _, out _), "Cross-role detached import bypassed active reservation.");
            TestRunner.Equal(1, world.Registry.Owners.Length, "Rejected imports partially mutated registry membership.");
            world.Registry.Commit(imported);
            TestRunner.True(world.Registry.IsActiveOwner(imported, a.Rider, a.Mount), "Saved attachment could not commit its imported owner.");
            b.Participation = true; var detachedId = Guid.NewGuid();
            TestRunner.True(world.Registry.TryImport(world.Session, "campaign", detachedId, 19, null, b.Mount, b, false,
                () => b.Participation, () => true, () => true, out var detached, out refusal), refusal);
            world.Registry.RetireSettled();
            TestRunner.True(detached.Id == detachedId && detached.Generation == 19 &&
                detached.State == MountedPairOwnershipState.Detached && ReferenceEquals(world.Registry.FindOwner(b.Mount), detached),
                "Pending-only imported participant lost custody or became active.");
            b.Participation = false; world.Registry.RetireSettled();
            TestRunner.True(detached.State == MountedPairOwnershipState.Retired && world.Registry.IsActiveOwner(imported, a.Rider, a.Mount),
                "Settling an imported pending survivor disturbed an unrelated mounted owner.");
        }
    }
}
