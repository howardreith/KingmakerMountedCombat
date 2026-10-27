using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using KingmakerMountedCombat.Diagnostics;

namespace Kingmaker.EntitySystem.Entities {
    class UnitEntityData { internal string UniqueId; internal NativeCommands Commands = new NativeCommands(); }
    class NativeCommands { internal List<UnitCommand> Raw = new List<UnitCommand>(); }
}
namespace Kingmaker.UnitLogic.Commands.Base { class UnitCommand { internal bool IsFinished = true; } }
namespace Kingmaker.UnitLogic.Commands {
    class UnitUseAbility : UnitCommand { internal NativeProcess ExecutionProcess; }
    class NativeProcess { internal bool IsEnded = true; }
}
namespace KingmakerMountedCombat.Domain {
    enum MountedPairOwnershipState { Reserved, Active, Detached, Retired }
    enum RelationshipState { Unmounted, Mounted }
    enum CleanupTrigger { Exception }
    class MountedPairOwner<TActor, TBoundary> {
        internal Guid Id = Guid.NewGuid(); internal long Generation = 7;
        internal TActor Rider, Mount; internal TBoundary Boundary;
        internal string RiderId = "rider", MountId = "mount";
        internal MountedPairOwnershipState State = MountedPairOwnershipState.Active;
        internal Func<bool> RetainsObligations = () => false, SaveReady = () => true, TeardownReady = () => true;
    }
}
namespace KingmakerMountedCombat.Integration {
    class NativeMountedControlService { internal bool HasUnsettledRelationshipTransition, Shell; internal bool HasUnsettledShellFor(GameMountedRelationshipService relationship) => Shell; }
    class Runtime { internal bool NoPreparedPairResidue = true; }
    class Outcome { internal bool Succeeded = true; }
    class GameMountedRelationshipService {
        internal RelationshipState State = RelationshipState.Mounted;
        internal MountedPairOwner<UnitEntityData, GameMountedRelationshipService> PairOwner;
        internal Runtime Runtime = new Runtime(); internal long MountedPairGeneration = 7;
        internal int DismountCalls; internal bool FailDismount, RefuseDismount, LeaveResidue;
        internal Action OnDismount = () => {};
        internal Outcome Dismount(CleanupTrigger trigger) {
            DismountCalls++; if (FailDismount) throw new InvalidOperationException("native cleanup fault");
            if (RefuseDismount) return new Outcome { Succeeded = false };
            State = RelationshipState.Unmounted; PairOwner = null; Runtime.NoPreparedPairResidue = !LeaveResidue;
            OnDismount(); return new Outcome();
        }
    }
    class Combat {
        internal bool HasActiveCommand, HasActiveGroundMovement, HasChargeOwnership, FailCancel;
        internal int CancelCalls;
        internal void Cancel(string reason) { CancelCalls++; if (FailCancel) throw new InvalidOperationException("cancel fault"); }
    }
    class MountedPairServices { internal GameMountedRelationshipService Relationship; internal Combat Combat = new Combat(); }
    class Registry {
        internal MountedPairOwner<UnitEntityData, GameMountedRelationshipService>[] Owners;
        internal MountedPairOwner<UnitEntityData, GameMountedRelationshipService> FindActive(UnitEntityData actor) =>
            Owners.SingleOrDefault(value => value.State == MountedPairOwnershipState.Active && ReferenceEquals(value.Rider, actor));
        internal MountedPairOwner<UnitEntityData, GameMountedRelationshipService> FindOwner(UnitEntityData actor) =>
            Owners.SingleOrDefault(value => ReferenceEquals(value.Rider, actor) || ReferenceEquals(value.Mount, actor));
        internal bool Owns(MountedPairOwner<UnitEntityData, GameMountedRelationshipService> owner) => Owners.Contains(owner);
        internal bool IsActiveOwner(MountedPairOwner<UnitEntityData, GameMountedRelationshipService> owner, UnitEntityData rider, UnitEntityData mount) =>
            Owners.Contains(owner) && owner.State == MountedPairOwnershipState.Active && ReferenceEquals(owner.Rider, rider) && ReferenceEquals(owner.Mount, mount);
    }
    class Unified { internal bool HasUnsettledPreparation; }
    class MountedWorldService {
        internal NativeMountedControlService NativeControls = new NativeMountedControlService();
        internal Registry Registry = new Registry(); internal MountedPairServices[] Pairs;
        internal Unified UnifiedTurn = new Unified(); internal bool SerializationBusy, Worker;
        internal bool ArchiveWorkerRunning() => Worker;
    }
}
class PairCustodyChecks {
    private sealed class Fixture {
        internal MountedWorldService World = new MountedWorldService();
        internal MountedPairOwner<UnitEntityData, GameMountedRelationshipService> Owner, Other;
        internal MountedPairServices Pair; internal Chunk7ProgressionPairCustody Custody;
        internal bool Busy, WrongWorld; internal int ScopeChecks;
        internal Fixture() {
            Owner = Create("target"); Other = Create("control");
            World.Registry.Owners = new[] { Owner, Other };
            Pair = new MountedPairServices { Relationship = Owner.Boundary };
            World.Pairs = new[] { Pair, new MountedPairServices { Relationship = Other.Boundary } };
            Owner.Boundary.OnDismount = () => { Owner.State = MountedPairOwnershipState.Retired; World.Registry.Owners = new[] { Other }; };
        }
        internal void Capture() { Custody = new Chunk7ProgressionPairCustody(World, Owner.Rider, Owner.Mount,
            () => { ScopeChecks++; if (WrongWorld) throw new InvalidOperationException("world replaced"); }, () => Busy); }
        private static MountedPairOwner<UnitEntityData, GameMountedRelationshipService> Create(string label) {
            var owner = new MountedPairOwner<UnitEntityData, GameMountedRelationshipService> {
                Rider = new UnitEntityData { UniqueId = label + "rider" }, Mount = new UnitEntityData { UniqueId = label + "mount" },
                Boundary = new GameMountedRelationshipService() };
            owner.Boundary.PairOwner = owner; return owner;
        }
    }
    static int passed, failed;
    static void Check(string name, Action test) { try { test(); passed++; Console.WriteLine("PASS " + name); } catch (Exception error) { failed++; Console.WriteLine("FAIL " + name + ": " + error); } }
    static void Require(bool value) { if (!value) throw new InvalidOperationException("assertion failed"); }
    static void Refused(Action action) { bool refused = false; try { action(); } catch (InvalidOperationException) { refused = true; } Require(refused); }
    static void HoldsBeforeMutation(Action<Fixture> change) {
        var f = new Fixture(); f.Capture(); change(f); bool result = false;
        try { result = f.Custody.TryDrainOriginalPair(); } catch (InvalidOperationException) {}
        Require(!result && !f.Custody.TargetSettled && f.Pair.Combat.CancelCalls == 0 && f.Owner.Boundary.DismountCalls == 0);
    }
    static int Main() {
        Check("settled native commands allow targeted cleanup and preserve control owner", () => {
            var f = new Fixture(); f.Capture(); Require(f.Custody.TryDrainOriginalPair());
            Require(f.Custody.TargetSettled && f.Pair.Combat.CancelCalls == 1 && f.Owner.Boundary.DismountCalls == 1 && f.Other.State == MountedPairOwnershipState.Active);
        });
        Check("native busy preserves original custody", () => HoldsBeforeMutation(f => f.Busy = true));
        Check("serialization fence preserves original custody", () => HoldsBeforeMutation(f => f.World.SerializationBusy = true));
        Check("archive worker preserves original custody", () => HoldsBeforeMutation(f => f.World.Worker = true));
        Check("changed world preserves original custody", () => HoldsBeforeMutation(f => f.WrongWorld = true));
        Check("unfinished native command blocks cancellation", () => HoldsBeforeMutation(f => f.Owner.Rider.Commands.Raw.Add(new UnitCommand { IsFinished = false })));
        Check("finished ability with live process blocks cancellation", () => HoldsBeforeMutation(f => f.Owner.Rider.Commands.Raw.Add(new UnitUseAbility { ExecutionProcess = new NativeProcess { IsEnded = false } })));
        Check("removed native shell remains owned until process ends", () => {
            var f = new Fixture(); var shell = new UnitUseAbility { ExecutionProcess = new NativeProcess() }; f.Owner.Rider.Commands.Raw.Add(shell); f.Capture();
            shell.ExecutionProcess.IsEnded = false; f.Owner.Rider.Commands.Raw.Clear();
            Require(!f.Custody.TryDrainOriginalPair() && f.Pair.Combat.CancelCalls == 0);
            shell.ExecutionProcess.IsEnded = true; Require(f.Custody.TryDrainOriginalPair());
        });
        Check("passive settlement observation retains removed live process without mutation", () => {
            var f = new Fixture(); var shell = new UnitUseAbility { ExecutionProcess = new NativeProcess() }; f.Owner.Rider.Commands.Raw.Add(shell); f.Capture();
            shell.ExecutionProcess.IsEnded = false; f.Owner.Rider.Commands.Raw.Clear();
            Require(!f.Custody.NativeCommandsSettled && f.Pair.Combat.CancelCalls == 0 && f.Owner.Boundary.DismountCalls == 0);
            shell.ExecutionProcess.IsEnded = true;
            Require(f.Custody.NativeCommandsSettled && !f.Custody.TargetSettled && f.Pair.Combat.CancelCalls == 0 && f.Owner.Boundary.DismountCalls == 0);
        });
        Check("unrelated owner retirement blocks all mutations", () => HoldsBeforeMutation(f => f.Other.State = MountedPairOwnershipState.Detached));
        Check("unrelated generation change blocks all mutations", () => HoldsBeforeMutation(f => f.Other.Boundary.MountedPairGeneration++));
        Check("replacement target registration blocks all mutations", () => HoldsBeforeMutation(f => {
            var replacement = new MountedPairOwner<UnitEntityData, GameMountedRelationshipService> { Rider = f.Owner.Rider, Mount = f.Owner.Mount, Boundary = f.Owner.Boundary };
            f.World.Registry.Owners = new[] { replacement, f.Other };
        }));
        Check("cancel failure retained while independent dismount still attempted", () => {
            var f = new Fixture(); f.Capture(); f.Pair.Combat.FailCancel = true;
            Require(!f.Custody.TryDrainOriginalPair() && f.Owner.Boundary.DismountCalls == 1 && !f.Custody.TargetSettled);
            f.Pair.Combat.FailCancel = false; Require(f.Custody.TryDrainOriginalPair());
        });
        Check("active pair ground command retains custody after cancellation", () => {
            var f = new Fixture(); f.Capture(); f.Pair.Combat.HasActiveGroundMovement = true;
            Require(!f.Custody.TryDrainOriginalPair() && f.Owner.Boundary.DismountCalls == 0);
        });
        Check("dismount exception remains retryable", () => {
            var f = new Fixture(); f.Capture(); f.Owner.Boundary.FailDismount = true;
            Require(!f.Custody.TryDrainOriginalPair() && !f.Custody.TargetSettled);
            f.Owner.Boundary.FailDismount = false; Require(f.Custody.TryDrainOriginalPair());
        });
        Check("prepared residue cannot be reported settled", () => {
            var f = new Fixture(); f.Capture(); f.Owner.Boundary.LeaveResidue = true; Require(!f.Custody.TryDrainOriginalPair());
        });
        Check("retained detached obligation cannot be reported settled", () => {
            var f = new Fixture(); f.Capture(); f.Owner.RetainsObligations = () => true; Require(!f.Custody.TryDrainOriginalPair());
        });
        Check("unready teardown cannot be reported settled", () => {
            var f = new Fixture(); f.Capture(); f.Owner.TeardownReady = () => false; Require(!f.Custody.TryDrainOriginalPair());
        });
        Check("shared preparation cannot be reported settled", () => {
            var f = new Fixture(); f.Capture(); f.World.UnifiedTurn.HasUnsettledPreparation = true; Require(!f.Custody.TryDrainOriginalPair());
        });
        Check("repeated settlement rechecks world without repeated cleanup", () => {
            var f = new Fixture(); f.Capture(); Require(f.Custody.TryDrainOriginalPair()); var checks = f.ScopeChecks;
            Require(f.Custody.TryDrainOriginalPair() && f.ScopeChecks > checks && f.Owner.Boundary.DismountCalls == 1);
            f.WrongWorld = true; Refused(() => f.Custody.TryDrainOriginalPair());
        });
        Check("unsettled pair at initial admission is refused", () => {
            var f = new Fixture(); f.Owner.Rider.Commands.Raw.Add(new UnitUseAbility { ExecutionProcess = new NativeProcess { IsEnded = false } }); Refused(f.Capture);
        });
        Console.WriteLine("PROGRESSION ACTUAL PAIR CUSTODY PASS=" + passed + " FAIL=" + failed); return failed == 0 ? 0 : 1;
    }
}
