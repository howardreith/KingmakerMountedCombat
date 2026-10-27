using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    internal static class MountedPresentationReadinessTests
    {
        private sealed class Fixture
        {
            internal object World = new object(), Archive = new object();
            internal object RiderA = new object(), MountA = new object(), RiderB = new object(), MountB = new object();
            internal object OwnerA = new object(), OwnerB = new object(), BoundaryA = new object(), BoundaryB = new object();
            internal long Sequence = 7, GenerationA = 12, GenerationB = 31;
            internal string Source = "saved-schema3-two-pair-graph", Allocation = "four-exact-native-allocations";
            internal string DebtA = "standard=4.25;move=1;swift=0;prepared=true", DebtB = "standard=6;move=0;swift=0;prepared=true";
            internal string StateA = "Reserved", StateB = "Detached", ParticipationA = "active=12;armed=13", ParticipationB = "pending-only;release-round=8";
            internal Guid IdA = Guid.Parse("11111111-1111-4111-8111-111111111111"), IdB = Guid.Parse("22222222-2222-4222-8222-222222222222");
            internal PresentationRestoreCustody Capture(bool omitActor = false, bool omitOwner = false, bool reverse = false)
            {
                var actors = new[] { new PresentationActorCustody("rider-a", RiderA, DebtA), new PresentationActorCustody("mount-a", MountA, "standard=2;prepared=true"),
                    new PresentationActorCustody("rider-b", RiderB, DebtB), new PresentationActorCustody("mount-b", MountB, "standard=3;prepared=false") };
                var owners = new[] { new PresentationOwnerCustody(IdA, GenerationA, OwnerA, BoundaryA, RiderA, MountA, StateA, ParticipationA),
                    new PresentationOwnerCustody(IdB, GenerationB, OwnerB, BoundaryB, null, MountB, StateB, ParticipationB) };
                return new PresentationRestoreCustody(World, Sequence, Archive, Source, Allocation, new[] { "pair-a", "pair-b" },
                    omitActor ? actors.Take(3) : reverse ? actors.Reverse() : actors,
                    omitOwner ? owners.Take(1) : reverse ? owners.Reverse() : owners);
            }
            internal Dictionary<string, MountedPresentationReadinessResult> Rigs(bool a, bool b) =>
                new Dictionary<string, MountedPresentationReadinessResult> {
                    ["pair-a"] = a ? MountedPresentationReadinessResult.Ready() : MountedPresentationReadinessResult.Waiting("native avatar incomplete"),
                    ["pair-b"] = b ? MountedPresentationReadinessResult.Ready() : MountedPresentationReadinessResult.Waiting("native equipment dirty") };
        }
        internal static void Register(TestRunner t)
        {
            t.Run("mixed world waits without changing saved source or detached custody", () => {
                var f = new Fixture(); var initial = f.Capture(); var gate = new MountedPresentationReadiness(initial, 0);
                Kind(MountedPresentationReadinessKind.Waiting, gate.Check(1, f.Capture(), f.Rigs(true, false)));
                TestRunner.True(initial.Refusal(f.Capture()) == null, "Waiting changed native state or ownership.");
                Kind(MountedPresentationReadinessKind.Ready, gate.Check(2, f.Capture(), f.Rigs(true, true)));
            });
            t.Run("ready second pair cannot cause partial attachment while first native rig waits", () => {
                var f = new Fixture(); var gate = new MountedPresentationReadiness(f.Capture(), 0);
                Kind(MountedPresentationReadinessKind.Waiting, gate.Check(2, f.Capture(), f.Rigs(false, true)));
            });
            t.Run("complete incompatible rig refuses even while another pair is still loading", () => {
                var f = new Fixture(); var gate = new MountedPresentationReadiness(f.Capture(), 0); var rigs = f.Rigs(false, true);
                rigs["pair-b"] = MountedPresentationReadinessResult.Incompatible("wrong finished skeleton");
                Kind(MountedPresentationReadinessKind.Incompatible, gate.Check(1, f.Capture(), rigs));
                Kind(MountedPresentationReadinessKind.Incompatible, gate.Check(2, f.Capture(), f.Rigs(true, true)));
            });
            t.Run("world time is not assumed frozen by wall-clock waiting", () => {
                var f = new Fixture(); var gate = new MountedPresentationReadiness(f.Capture(), 0);
                Kind(MountedPresentationReadinessKind.Waiting, gate.Check(29, f.Capture(), f.Rigs(false, true)));
            });
            t.Run("current native debt at fence entry can already reflect earlier legitimate decay", () => {
                var f = new Fixture { DebtA = "standard=0.25;move=0;swift=0;prepared=true" };
                var gate = new MountedPresentationReadiness(f.Capture(), 0);
                Kind(MountedPresentationReadinessKind.Ready, gate.Check(1, f.Capture(), f.Rigs(true, true)));
            });
            t.Run("bounded wait failure retains an incompatible result after eventual rig completion", () => {
                var f = new Fixture(); var gate = new MountedPresentationReadiness(f.Capture(), 0);
                Kind(MountedPresentationReadinessKind.Incompatible, gate.Check(30.001, f.Capture(), f.Rigs(false, false)));
                Kind(MountedPresentationReadinessKind.Incompatible, gate.Check(31, f.Capture(), f.Rigs(true, true)));
            });
            t.Run("exact actor and owner ordering does not create a new custody identity", () => {
                var f = new Fixture(); var gate = new MountedPresentationReadiness(f.Capture(), 0);
                Kind(MountedPresentationReadinessKind.Ready, gate.Check(1, f.Capture(reverse: true), f.Rigs(true, true)));
            });
            Reject(t, "replacement native world", f => f.World = new object());
            Reject(t, "new load sequence", f => f.Sequence++);
            Reject(t, "same-data replacement archive object", f => f.Archive = new object());
            Reject(t, "in-place saved source edit", f => f.Source += "changed");
            Reject(t, "actor object replaced under same stable ID", f => f.RiderA = new object());
            Reject(t, "saved mount reference replaced", f => f.MountB = new object());
            Reject(t, "refund while existing native cooldown entry is fenced", f => f.DebtA = "standard=0;move=0;swift=0;prepared=true");
            Reject(t, "new native preparation while presentation is fenced", f => f.DebtB = "standard=6;move=0;swift=0;prepared=false");
            Reject(t, "additional spend while native commands are fenced", f => f.DebtA = "standard=6;move=6;swift=0;prepared=true");
            Reject(t, "owner generation changes", f => f.GenerationA++);
            Reject(t, "detached owner restarted release round", f => f.ParticipationB = "pending-only;release-round=9");
            Reject(t, "actor allocation grant replay", f => f.Allocation += "new-grant");
            Reject(t, "replacement owner under same saved ID", f => f.OwnerA = new object());
            Reject(t, "replacement service boundary", f => f.BoundaryB = new object());
            Reject(t, "reserved owner becomes active before all-pair readiness", f => f.StateA = "Active");
            Reject(t, "unfinished detached owner is retired", f => f.StateB = "Retired");
            Reject(t, "pending-only state gains an invented activation", f => f.ParticipationB = "activation=99;pending-only;release-round=8");
            foreach (var omit in new[] { "actor", "owner", "rig", "foreign-rig", "null-rig" })
                t.Run("missing or conflicting " + omit + " refuses entire preflight", () => {
                    var f = new Fixture(); var gate = new MountedPresentationReadiness(f.Capture(), 0); var rigs = f.Rigs(true, true);
                    if (omit == "rig") rigs.Remove("pair-a");
                    if (omit == "foreign-rig") { rigs.Remove("pair-a"); rigs.Add("pair-foreign", MountedPresentationReadinessResult.Ready()); }
                    if (omit == "null-rig") rigs["pair-a"] = null;
                    Kind(MountedPresentationReadinessKind.Incompatible, gate.Check(1, f.Capture(omit == "actor", omit == "owner"), rigs));
                });
            foreach (var now in new[] { double.NaN, double.PositiveInfinity, -1d })
                t.Run("unbounded or reversed native observation clock refuses " + now, () => {
                    var f = new Fixture(); var gate = new MountedPresentationReadiness(f.Capture(), 0);
                    Kind(MountedPresentationReadinessKind.Incompatible, gate.Check(now, f.Capture(), f.Rigs(true, true)));
                });
        }
        private static void Reject(TestRunner runner, string name, Action<Fixture> mutation)
        {
            runner.Run(name, () => { var f = new Fixture(); var gate = new MountedPresentationReadiness(f.Capture(), 0);
                Kind(MountedPresentationReadinessKind.Waiting, gate.Check(0, f.Capture(), f.Rigs(false, false)));
                mutation(f); Kind(MountedPresentationReadinessKind.Incompatible, gate.Check(1, f.Capture(), f.Rigs(true, true))); });
        }
        private static void Kind(MountedPresentationReadinessKind expected, MountedPresentationReadinessResult actual) =>
            TestRunner.Equal(expected, actual.Kind, actual.Reason ?? "Unexpected preflight result.");
    }
}
