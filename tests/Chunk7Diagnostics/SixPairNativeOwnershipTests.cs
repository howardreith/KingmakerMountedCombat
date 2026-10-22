using System;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;

// Only native objects/view acquisition are facades. The complete production
// ownership partial, registry and selected native format compile unchanged.
namespace Kingmaker.EntitySystem.Entities
{
    internal sealed class UnitEntityData
    {
        internal string UniqueId = Guid.NewGuid().ToString("N");
        internal int Standard = 6, Move = 3, Swift = 6;
    }
}
namespace Kingmaker
{
    internal sealed class Player { internal string GameId = Guid.NewGuid().ToString("N"); }
    internal sealed class Game { internal static Game Instance; internal Player Player = new Player(); }
}
namespace KingmakerMountedCombat.Integration
{
    internal sealed class MountedWorldService { }
    internal sealed class MountedPlayerActionController { }
    internal sealed class MountedCombatController { }
    internal sealed class NativeRuntime { internal bool NoPreparedPairResidue = true; }
    internal sealed class Coordinator { internal RelationshipState State = RelationshipState.Unmounted; }
    internal sealed partial class GameMountedRelationshipService
    {
        private long mountedPairGeneration;
        private bool cleanupRetryRequired;
        private readonly NativeRuntime runtime = new NativeRuntime();
        private readonly Coordinator coordinator = new Coordinator();
        internal long MountedPairGeneration => mountedPairGeneration;
        internal UnitEntityData Rider, Mount;
        internal int Attachments;
        internal bool Retained, SaveReady = true, TeardownReady = true;
        internal GameMountedRelationshipService(MountedPairRegistry<UnitEntityData, GameMountedRelationshipService> registry)
        {
            Ownership = registry;
            BindOwnershipReadiness((r, m) => Retained, () => false, () => SaveReady, () => TeardownReady);
        }
        internal bool Begin(UnitEntityData rider, UnitEntityData mount)
        {
            if (!TryReserveOwnership(rider, mount, out _)) return false;
            Rider = rider; Mount = mount; mountedPairGeneration = PairOwner.Generation;
            // Represents the external native movement/view acquisition boundary.
            Attachments++; runtime.NoPreparedPairResidue = false;
            coordinator.State = RelationshipState.Mounted; CommitOwnership(); return true;
        }
        internal void FinishImport(UnitEntityData rider, UnitEntityData mount)
        {
            if (!TryReserveSavedOwnership(rider, mount, out var error)) throw new Exception(error);
            Rider = rider; Mount = mount; coordinator.State = RelationshipState.Mounted;
            CommitSavedOwnership(); CompleteSavedParticipationImport();
        }
        internal void Detach(bool retain)
        {
            Retained = retain; runtime.NoPreparedPairResidue = true;
            coordinator.State = RelationshipState.Unmounted; CompleteOwnershipAttempt();
        }
    }
}
internal static class SixPairNativeOwnershipTests
{
    private static int count;
    private static void Check(bool ok, string text) { if (!ok) throw new Exception(text); count++; Console.WriteLine("PASS " + text); }
    private static MountedPairRegistry<UnitEntityData, GameMountedRelationshipService> Registry() =>
        new MountedPairRegistry<UnitEntityData, GameMountedRelationshipService>(actor => actor.UniqueId);
    private static GameMountedRelationshipService[] Six(MountedPairRegistry<UnitEntityData, GameMountedRelationshipService> registry) =>
        Enumerable.Range(0, 6).Select(_ => new GameMountedRelationshipService(registry)).ToArray();
    public static void Main()
    {
        Game.Instance = new Game(); var registry = Registry(); var pairs = Six(registry);
        var riders = Enumerable.Range(0, 6).Select(_ => new UnitEntityData()).ToArray();
        var mounts = Enumerable.Range(0, 6).Select(_ => new UnitEntityData()).ToArray();
        Check(GameMountedRelationshipService.NativePairCapacity == 6 && NativeMountedSaveFormat.NativePairCapacity == 6,
            "actual native adapter and persistence select six active pairs");
        for (var i = 0; i < 6; i++)
        {
            Check(pairs[i].NativeAdmissionRefusal(riders[i], mounts[i]) == null && pairs[i].Begin(riders[i], mounts[i]), "native reserve/commit pair " + i);
            Check(pairs[i].Attachments == 1 && pairs[i].IsCurrentPairOwner(pairs[i].PairOwner), "exact active custody pair " + i);
        }
        var requests = pairs.Select((p, i) => p.CaptureNativeRequest(riders[i], mounts[i], true)).ToArray();
        for (var i = 0; i < 6; i++)
            Check(requests[i] != null && pairs[(i + 1) % 6].ResolveNativeRequest(requests[i], out var resolved) &&
                ReferenceEquals(resolved, pairs[i]) && ReferenceEquals(pairs[0].ResolveActorOwner(mounts[i]), pairs[i]),
                "selection-independent native command and actor routing pair " + i);
        var seventh = new GameMountedRelationshipService(registry); var extraRider = new UnitEntityData(); var extraMount = new UnitEntityData();
        Check(seventh.NativeAdmissionRefusal(extraRider, extraMount) != null &&
            seventh.CaptureNativeRequest(extraRider, extraMount, false) == null && !seventh.Begin(extraRider, extraMount) &&
            seventh.Attachments == 0 && seventh.PairOwner == null && registry.Owners.Length == 6,
            "seventh pair refused before acquisition with no provisional owner or neighbor mutation");
        pairs[4].SaveReady = false; pairs[5].TeardownReady = false;
        Check(!pairs[0].AllSaveOwnersReady && !pairs[0].AllTeardownOwnersReady, "aggregate barriers include fifth and sixth owners");
        pairs[4].SaveReady = true; pairs[5].TeardownReady = true;
        foreach (var p in pairs) p.Detach(true);
        Check(registry.ActiveCount == 0 && registry.Owners.Length == 6, "six detached obligations retain custody without active membership");
        var replacements = Six(registry);
        for (var i = 0; i < 6; i++) Check(replacements[i].Begin(new UnitEntityData(), new UnitEntityData()), "replacement admission beside retained pair " + i);
        Check(registry.ActiveCount == 6 && registry.Owners.Length == 12, "all twelve independent active/retained owners remain indexed");
        replacements[0].Detach(true);
        Check(registry.ActiveCount == 5 && !seventh.Begin(extraRider, extraMount) && registry.Owners.Length == 12 && seventh.Attachments == 0,
            "thirteenth retained owner refused independently of available active slot");
        pairs[2].TeardownReady = false;
        var visited = 0; var errors = registry.VisitOwners(owner => { visited++; if (ReferenceEquals(owner.Boundary, pairs[2])) throw new Exception("retained teardown"); });
        Check(visited == 12 && errors.Length == 1 && registry.Owners.Length == 12, "one failing cleanup visits all twelve owners and retains failed custody");
        foreach (var p in pairs.Concat(replacements)) { p.Retained = false; p.Detach(false); }
        Check(registry.Owners.Length == 0, "all twelve owners retire after obligations settle");
        Check(riders.Concat(mounts).All(a => a.Standard == 6 && a.Move == 3 && a.Swift == 6), "admission/routing/teardown preserve all original native budgets");

        Game.Instance = new Game(); registry = Registry(); pairs = Six(registry);
        var ids = Enumerable.Range(0, 6).Select(_ => Guid.NewGuid().ToString("N")).ToArray();
        for (var i = 0; i < 6; i++)
        {
            pairs[i].ImportSavedOwnership(riders[i], mounts[i], ids[i], i + 11, true);
            Check(pairs[i].PairOwner.Id.ToString("N") == ids[i] && pairs[i].PairOwner.Generation == i + 11 && pairs[i].Attachments == 0,
                "saved import preserves exact owner/generation without native acquisition pair " + i);
            pairs[i].FinishImport(riders[i], mounts[i]);
        }
        Check(registry.ActiveCount == 6 && pairs.All(p => p.IsCurrentPairOwner(p.PairOwner)), "six saved pairs commit exact imported ownership once");
        var prior = pairs[5].PairOwner; var rejected = false;
        try { pairs[5].ImportSavedOwnership(riders[5], mounts[5], ids[5], 16, true); } catch (InvalidOperationException) { rejected = true; }
        Check(rejected && ReferenceEquals(prior, pairs[5].PairOwner) && registry.Owners.Length == 6, "repeat import cannot replace a saved owner");
        foreach (var p in pairs) p.Detach(false);
        Check(registry.Owners.Length == 0 && riders.Concat(mounts).All(a => a.Standard == 6 && a.Move == 3 && a.Swift == 6),
            "saved ownership release changes no native resource or residual custody");
        Console.WriteLine("SIX-PAIR NATIVE OWNERSHIP PASS=" + count + " FAIL=0; actual adapter/registry with external native objects only");
    }
}
