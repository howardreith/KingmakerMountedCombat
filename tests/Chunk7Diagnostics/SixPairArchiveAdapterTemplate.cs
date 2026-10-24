using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;

namespace Kingmaker.EntitySystem.Entities
{
    internal sealed class UnitEntityData { internal string UniqueId; internal SavedNativeActor Native; }
}
namespace Kingmaker.GameModes { internal enum GameModeType { Default } }
namespace Kingmaker
{
    internal sealed class Player { internal string GameId = Guid.NewGuid().ToString("N"); internal bool IsInCombat; internal UnitEntityData[] AllCharacters; }
    internal sealed class State { internal UnitEntityData[] Units; }
    internal sealed class Area { internal string AssetGuidThreadSafe = new string('a', 32); }
    internal sealed class Clock { internal TimeSpan GameTime; }
    internal sealed class Game
    {
        internal static Game Instance; internal Player Player = new Player(); internal State State = new State();
        internal Area CurrentlyLoadedArea = new Area(); internal Clock TimeController = new Clock();
        internal GameModes.GameModeType CurrentMode; internal object TurnBasedCombatController;
    }
    internal sealed class LoadingProcess { internal static LoadingProcess Instance = new LoadingProcess(); internal bool IsLoadingInProcess; }
}
namespace Kingmaker.PubSubSystem
{
    internal interface IWarningNotificationUIHandler { void HandleWarning(string message, bool persistent); }
    internal static class EventBus { internal static void RaiseEvent<T>(Action<T> action) { } }
}
namespace KingmakerMountedCombat.Integration
{
    internal sealed class Owner { internal Guid Id; internal long Generation; internal UnitEntityData Rider, Mount; internal MountedPairOwnershipState State; }
    internal sealed class Runtime { internal string MountProfileId = "medium-humanoid-horse-v1"; }
    internal sealed class Result { internal bool Succeeded = true; internal string[] Errors = new string[0]; }
    internal sealed class Relationship : IDisposable
    {
        internal static int Imports, Restores, Completes, FailImportAt, FailRestoreAt;
        internal int Disposals; internal bool FailDispose; internal Owner PairOwner;
        internal RelationshipState State; internal Runtime Runtime = new Runtime();
        internal void ImportSavedOwnership(UnitEntityData rider, UnitEntityData mount, string id, long generation, bool mounted)
        {
            if (++Imports == FailImportAt) throw new InvalidDataException("injected exact native import failure");
            PairOwner = new Owner { Id = Guid.Parse(id), Generation = generation, Rider = rider, Mount = mount, State = MountedPairOwnershipState.Reserved };
        }
        internal Result RestoreSavedPair(UnitEntityData rider, UnitEntityData mount, string profile)
        {
            if (++Restores == FailRestoreAt) throw new InvalidDataException("injected exact native attachment failure");
            if (!ReferenceEquals(PairOwner.Rider, rider) || !ReferenceEquals(PairOwner.Mount, mount)) throw new Exception("actor substitution");
            PairOwner.State = MountedPairOwnershipState.Active; State = RelationshipState.Mounted; return new Result();
        }
        internal void CompleteSavedParticipationImport() { Completes++; }
        internal bool IsCurrentPairOwner(Owner owner) => ReferenceEquals(PairOwner, owner) && owner?.State == MountedPairOwnershipState.Active;
        public void Dispose() { Disposals++; if (FailDispose) throw new Exception("retained native cleanup"); PairOwner = null; State = RelationshipState.Unmounted; }
    }
    internal sealed class Participant { internal bool TrySettleExplorationParticipation() => true; internal SavedPairedState CaptureParticipation() => null; }
    internal sealed class MountedPairServices
    {
        internal Relationship Relationship = new Relationship(); internal Participant Participant = new Participant();
        internal IDisposable PlayerActions, Combat, Scheduler;
    }
    internal sealed class Registry
    {
        internal bool AllTeardownReady() => true;
        internal bool Owns(Owner owner) => owner != null;
    }
    internal sealed class Turn
    {
        internal List<Participant> SnapshotParticipants = new List<Participant>();
        internal void EndPairedServiceParticipation() { } internal void Dispose() { }
        internal bool HasUnsettledPreparation;
    }
    internal sealed class MountedWorldService
    {
        internal readonly List<MountedPairServices> Published = new List<MountedPairServices>();
        internal MountedPairServices[] Pairs => Published.ToArray(); private MountedPairServices[] PairSnapshot => Pairs;
        internal Registry Registry = new Registry(); internal Turn UnifiedTurn = new Turn();
        private bool disposed, constructingPair; private readonly List<object> failedConstructions = new List<object>();
        internal bool AnyChargeFenced, SerializationBusy; internal int CleanupFailures;
        internal bool Disposed => disposed;
        internal MountedPairServices CreateRestoredBoundary()
        {
            var pair = new MountedPairServices(); Published.Add(pair); UnifiedTurn.SnapshotParticipants.Add(pair.Participant); return pair;
        }
        private void RetryFailedConstructionCleanup() { }
        private void HandlePairFailure(MountedPairServices pair, Exception error) { CleanupFailures++; }
        /*ACTUAL_WORLD_VISIT*/
        /*ACTUAL_WORLD_DISPOSE*/
    }
    internal sealed class Settings { internal bool EnablePairedActivation = true, EnableUnifiedMountedTurn, EnablePairedCommandScheduler; }
    internal sealed class Controls
    {
        internal int Restores; internal SavedMountedSlot[] Slots;
        internal SavedMountedSlot[] CapturePersistentSlots() => Slots;
        internal void RestorePersistentSlots(SavedMountedSlot[] slots) { Restores++; Slots = slots; }
    }
    internal sealed class WorldScope { internal int Closes; internal bool CanPresent(Player world) => true; internal void Close() { Closes++; } }
    internal sealed class LoadScope { internal WorldScope World = new WorldScope(); }
    internal sealed class FailureFence { internal bool Blocks(Player world) => false; }
    internal sealed class Logger { internal void Exception(string message, Exception error) { } }
    internal static class RetainedNativeReentryPolicy { internal static string[] SpentPendingRiders(MountedArchiveData data) => new string[0]; }
    internal sealed class RetainedPendingReentry { internal Player World; internal object Controller; internal SavedCombatData Combat; internal long LoadSequence; internal RetainedPendingRider[] Riders; }
    internal sealed class RetainedPendingRider { internal UnitEntityData Rider, Mount; internal MountedPairServices Pair; internal Guid OwnerId; internal long Generation; }
    internal sealed class MountedPersistenceService
    {
        internal MountedWorldService mountedWorld = new MountedWorldService();
        internal MountedArchiveData RestorableArchive; private SavedCombatData SelectedCombat => RestorableArchive?.Combat;
        internal bool Enabled = true, ArchiveNativeEnabled = true, presentationPending = true, SaveSuspended;
        internal LoadScope restoreLoad = new LoadScope(); internal string selectedCampaign, areaRestoreFailure, combatRestoreFailure;
        private readonly FailureFence abandonedCombat = new FailureFence(); private bool combatRestored, archiveGraphReady, archiveOwnersReady;
        private readonly Dictionary<string, UnitEntityData> archiveActors = new Dictionary<string, UnitEntityData>();
        private readonly Dictionary<string, UnitEntityData> restoredActors = new Dictionary<string, UnitEntityData>();
        private readonly Dictionary<string, MountedPairServices> archiveOwners = new Dictionary<string, MountedPairServices>();
        private readonly Settings settings = new Settings(); internal Controls controls = new Controls(); private Turn unifiedTurn => mountedWorld.UnifiedTurn;
        internal int PresentationRestoreCount; private long loadSequence; private RetainedPendingReentry retainedPendingReentry;
        internal string ArchiveRestorePhase; internal readonly List<string> Reports = new List<string>(); private Logger logger = new Logger();
        private MountedSaveReadResult loaded; private Relationship relationship;
        internal int Owners => archiveOwners.Count;
        internal void CreditPostLoad(UnitEntityData actor) { restoredActors.Add(actor.UniqueId, actor); }
        private bool PreflightArchivePresentation() => true; private void ClearArchivePresentationReadiness() { }
        private void RetrySaveRelease() { } private void DrainAbandonedSave() { } private void ReplayDeferredResetIfSettled() { }
        private void ContinueWorldRelease() { } private void CompleteAreaTransitionIfReady() { } private void TryRestoreCombat() { }
        private void Report(string message) { Reports.Add(message); }
        private SavedNativeActor CaptureActor(UnitEntityData actor) => actor.Native;
        private SavedCombatData CaptureCombat(bool archive) => throw new Exception("combat not selected by this fixture");
        private static string OwnerKey(string id) => Guid.Parse(id).ToString("N");
        /*ACTUAL_PENDING*/
        /*ACTUAL_CAPTURE*/
        /*ACTUAL_RESOLVE*/
        /*ACTUAL_IDENTITY*/
        /*ACTUAL_PREPARE*/
        /*ACTUAL_PRESENTATION*/
        /*ACTUAL_BLOCK*/
        /*ACTUAL_UPDATE*/
        internal MountedArchiveData Capture() => CaptureArchive();
    }
}
internal static class Program
{
    private static int count;
    private static void Check(bool ok, string text) { if (!ok) throw new Exception(text); count++; Console.WriteLine("PASS " + text); }
    private static MountedPersistenceService Setup()
    {
        Relationship.Imports = Relationship.Restores = Relationship.Completes = Relationship.FailImportAt = Relationship.FailRestoreAt = 0;
        var game = Game.Instance = new Game(); var service = new MountedPersistenceService();
        var actors = Enumerable.Range(0, 12).Select(_ => new UnitEntityData { UniqueId = Guid.NewGuid().ToString("N") }).ToArray();
        foreach (var a in actors) a.Native = new SavedNativeActor { Id = a.UniqueId, Standard = 6, Move = 3, Swift = 6 };
        game.Player.AllCharacters = actors; game.State.Units = actors; service.selectedCampaign = game.Player.GameId;
        service.RestorableArchive = new MountedArchiveData { SchemaVersion = 3, CampaignId = game.Player.GameId, AreaId = game.CurrentlyLoadedArea.AssetGuidThreadSafe,
            Policy = MountedSaveData.PairedPolicy, RulesId = MountedSaveData.Rules, Participations = new SavedPairParticipation[0],
            Pairs = Enumerable.Range(0, 6).Select(i => new SavedMountedPair { PairId = Guid.NewGuid().ToString("N"), Generation = i + 7,
                ProfileId = "medium-humanoid-horse-v1", Rider = actors[i * 2].Native, Mount = actors[i * 2 + 1].Native }).ToArray(),
            Slots = actors.Select(a => new SavedMountedSlot { ActorId = a.UniqueId, Index = 5, Kind = 1 }).ToArray() };
        foreach (var a in actors) service.CreditPostLoad(a);
        return service;
    }
    private static bool DebtPreserved() => Game.Instance.State.Units.All(a => a.Native.Standard == 6 && a.Native.Move == 3 && a.Native.Swift == 6);
    public static void Main(string[] args)
    {
        var managed = Path.GetFullPath(Path.GetDirectoryName(args[0]));
        AppDomain.CurrentDomain.AssemblyResolve += (sender, request) =>
        {
            var name = new System.Reflection.AssemblyName(request.Name).Name;
            if (name != "UnityEngine" && !name.StartsWith("UnityEngine.", StringComparison.Ordinal)) return null;
            var path = Path.GetFullPath(Path.Combine(managed, name + ".dll"));
            if (!path.StartsWith(managed + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) return null;
            return File.Exists(path) ? System.Reflection.Assembly.LoadFrom(path) : null;
        };
        var s = Setup(); var original = MountedArchiveCodec.Encode(s.RestorableArchive); s.Update();
        Check(s.Owners == 6 && s.mountedWorld.Pairs.Length == 6 && Relationship.Imports == 6 && Relationship.Restores == 6 && Relationship.Completes == 6,
            "actual archive loop imports and presents all six exact owners once");
        Check(s.PresentationRestoreCount == 6 && s.controls.Restores == 1 && s.controls.Slots.Length == 12 && s.restoreLoad.World.Closes == 1,
            "actual Update restores twelve actor slots once and closes the admitted load");
        var captured = s.Capture();
        Check(MountedArchiveCodec.Encode(captured) == original && captured.Pairs.SelectMany(p => new[] { p.Rider, p.Mount }).Count() == 12,
            "actual capture preserves all six owner IDs/generations/profiles and twelve saved budgets without truncation");
        for (var i = 0; i < 10; i++) s.Update();
        Check(Relationship.Imports == 6 && Relationship.Restores == 6 && s.controls.Restores == 1 && DebtPreserved(), "later updates neither replay restoration nor change resources");
        s.mountedWorld.Pairs[5].Relationship.PairOwner.State = MountedPairOwnershipState.Reserved;
        var rejected = false; try { s.Capture(); } catch (InvalidOperationException) { rejected = true; }
        Check(rejected, "capture refuses a sixth owner without exact active custody");
        s.mountedWorld.Dispose(); Check(s.mountedWorld.Disposed && s.mountedWorld.Pairs.All(p => p.Relationship.Disposals == 1), "actual world Dispose visits every restored boundary");
        foreach (var failure in new[] { 1, 4, 6 })
        {
            s = Setup(); Relationship.FailImportAt = failure; s.Update();
            Check(Relationship.Imports == failure && s.Owners == failure - 1 && s.mountedWorld.Pairs.Length == failure && s.combatRestoreFailure != null,
                "failed import retains all imported and constructed boundaries at position " + failure);
            for (var i = 0; i < 10; i++) s.Update();
            Check(Relationship.Imports == failure && Relationship.Restores == 0 && s.controls.Restores == 0 && s.restoreLoad.World.Closes == 0 && DebtPreserved(),
                "actual failure fence prevents retry, partial presentation and grants at position " + failure);
            s.mountedWorld.Pairs[0].Relationship.FailDispose = true; rejected = false;
            try { s.mountedWorld.Dispose(); } catch (AggregateException) { rejected = true; }
            Check(rejected && !s.mountedWorld.Disposed && s.mountedWorld.Pairs.All(p => p.Relationship.Disposals == 1),
                "actual failed teardown visits independent boundaries and retains world at position " + failure);
            s.mountedWorld.Pairs[0].Relationship.FailDispose = false; s.mountedWorld.Dispose();
            Check(s.mountedWorld.Disposed && s.mountedWorld.Pairs.All(p => p.Relationship.PairOwner == null) && DebtPreserved(),
                "settled retry releases every exact retained boundary without resources at position " + failure);
        }
        s = Setup(); Relationship.FailRestoreAt = 5; s.Update();
        for (var i = 0; i < 10; i++) s.Update();
        Check(Relationship.Imports == 6 && Relationship.Restores == 5 && s.PresentationRestoreCount == 4 && s.controls.Restores == 0 && s.combatRestoreFailure != null && DebtPreserved(),
            "attachment failure after six imports is latched without reimport or later grants");
        s.mountedWorld.Dispose();
        Check(s.mountedWorld.Pairs.All(p => p.Relationship.Disposals == 1) && s.mountedWorld.Disposed, "partial presentation teardown includes all six constructed owners");
        Console.WriteLine("SIX-PAIR ARCHIVE ADAPTER PASS=" + count + " FAIL=0; actual source methods with native/service boundaries substituted");
    }
}
