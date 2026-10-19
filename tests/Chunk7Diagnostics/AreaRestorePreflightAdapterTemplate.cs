using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerMountedCombat.Domain;

namespace Kingmaker.EntitySystem.Entities
{
    internal sealed class UnitEntityData
    {
        internal string UniqueId, ProfileId;
        internal bool IsInGame = true, IsDirectlyControllable = true;
        internal double NativeDebt = 2.5;
    }
}
namespace Kingmaker
{
    internal sealed class Player
    {
        internal string GameId = "campaign";
        internal List<Kingmaker.EntitySystem.Entities.UnitEntityData> AllCharacters = new List<Kingmaker.EntitySystem.Entities.UnitEntityData>();
    }
    internal sealed class State
    {
        internal List<Kingmaker.EntitySystem.Entities.UnitEntityData> Units = new List<Kingmaker.EntitySystem.Entities.UnitEntityData>();
    }
    internal sealed class Area { internal string AssetGuidThreadSafe = "destination"; }
    internal sealed class Game
    {
        internal static Game Instance;
        internal Player Player = new Player();
        internal State State = new State();
        internal Area CurrentlyLoadedArea = new Area();
    }
}
namespace KingmakerMountedCombat.Integration
{
    using Kingmaker;
    using Kingmaker.EntitySystem.Entities;
    internal sealed class Profile { internal string Id; }
    internal static class SupportedMountedProfiles
    {
        internal static Profile Resolve(UnitEntityData rider, UnitEntityData mount) =>
            rider == null || mount == null ? null : new Profile { Id = mount.ProfileId };
    }
    internal static class KingmakerMountedPairRuntime
    {
        internal static MountedPairCandidate CreateCandidate(UnitEntityData rider, UnitEntityData mount) =>
            new MountedPairCandidate(rider.UniqueId, mount.UniqueId) {
                RiderIsDirectlyControllable = rider.IsInGame && rider.IsDirectlyControllable,
                MountIsDirectlyControllable = mount.IsInGame && mount.IsDirectlyControllable,
                RiderIsAliveAndConscious = true, MountIsAliveAndConscious = true,
                ExactReciprocalCompanionRelationship = true,
                RiderSizeOrdinal = 4, MountSizeOrdinal = 5,
                RiderViewAndStockAgentAvailable = true, MountViewAndStockAgentAvailable = true,
                RiderStockAgentEnabled = true, MountStockAgentEnabled = true,
                RiderAgentOverrideAvailable = true, MountAgentOverrideAvailable = true,
                RiderIsExactlyMedium = true, SafeMovementMode = true
            };
    }
    internal sealed class TransitionResult
    {
        internal bool Succeeded = true;
        internal string[] Errors = new string[0];
    }
    internal sealed class Relationship
    {
        internal static int Imports, Restores, Completes;
        internal void ImportSavedOwnership(UnitEntityData rider, UnitEntityData mount, string owner, long generation, bool fromArea) { Imports++; }
        internal TransitionResult RestoreSavedPair(UnitEntityData rider, UnitEntityData mount, string profile) { Restores++; return new TransitionResult(); }
        internal void CompleteSavedParticipationImport() { Completes++; }
    }
    internal sealed class Boundary { internal Relationship Relationship = new Relationship(); }
    internal sealed class MountedWorld
    {
        internal int Creates;
        internal UnifiedTurn Turn;
        internal Boundary CreateRestoredBoundary()
        {
            Creates++;
            var boundary = new Boundary();
            Turn.SnapshotParticipants.Add(new Participant { Relationship = boundary.Relationship });
            return boundary;
        }
    }
    internal sealed class Participant
    {
        internal Relationship Relationship;
        internal static int Restores;
        internal void RestoreParticipation(object state, Dictionary<string, UnitEntityData> resolved, bool turnBased) { Restores++; }
    }
    internal sealed class UnifiedTurn
    {
        internal List<Participant> SnapshotParticipants = new List<Participant>();
    }
    internal sealed class Controls
    {
        internal int ResumeCalls, SlotRestores;
        internal void ResumeAreaControls() { ResumeCalls++; }
        internal void RestorePersistentSlots(object slots) { SlotRestores++; }
    }
    internal sealed partial class MountedPersistenceService
    {
        internal sealed class Pair
        {
            internal string RiderId, MountId, ProfileId, OwnerId;
            internal long Generation;
            internal object Participation;
        }
        internal sealed class Transfer
        {
            internal WeakReference World;
            internal string CampaignId, AreaId;
            internal Pair[] Pairs;
            internal Slot[] Slots;
            internal bool Suspended = true, ResumeAttempted, TurnBased;
        }
        internal sealed class Slot { internal string ActorId; }
        internal Transfer areaTransfer, failedAreaTransfer;
        internal string areaRestoreFailure;
        internal int AreaResumeCount;
        internal bool Enabled = true, ArchiveNativeEnabled = true;
        internal MountedWorld mountedWorld = new MountedWorld();
        internal Relationship relationship = new Relationship();
        internal UnifiedTurn unifiedTurn = new UnifiedTurn();
        internal Controls controls = new Controls();
        internal readonly List<string> Reports = new List<string>();
        internal MountedPersistenceService() { mountedWorld.Turn = unifiedTurn; }
        internal void Report(string value) { Reports.Add(value); }
        __METHOD__
    }
}
internal static class Program
{
    private static int passed;
    private static void Check(bool value, string name)
    {
        if (!value) throw new Exception("FAIL " + name);
        passed++; Console.WriteLine("PASS " + name);
    }
    private static KingmakerMountedCombat.Integration.MountedPersistenceService Setup(out Kingmaker.EntitySystem.Entities.UnitEntityData[] actors)
    {
        usingDummyReset();
        var game = Kingmaker.Game.Instance = new Kingmaker.Game();
        actors = Enumerable.Range(0, 4).Select(i => new Kingmaker.EntitySystem.Entities.UnitEntityData {
            UniqueId = "actor" + i, ProfileId = "medium-humanoid-horse-v1" }).ToArray();
        game.State.Units.AddRange(actors);
        game.Player.AllCharacters.AddRange(actors);
        var service = new KingmakerMountedCombat.Integration.MountedPersistenceService();
        service.areaTransfer = new KingmakerMountedCombat.Integration.MountedPersistenceService.Transfer {
            World = new WeakReference(game.Player), CampaignId = game.Player.GameId,
            AreaId = game.CurrentlyLoadedArea.AssetGuidThreadSafe,
            Pairs = new[] {
                new KingmakerMountedCombat.Integration.MountedPersistenceService.Pair { RiderId = actors[0].UniqueId, MountId = actors[1].UniqueId, ProfileId = actors[1].ProfileId, OwnerId = "ownerA", Generation = 1 },
                new KingmakerMountedCombat.Integration.MountedPersistenceService.Pair { RiderId = actors[2].UniqueId, MountId = actors[3].UniqueId, ProfileId = actors[3].ProfileId, OwnerId = "ownerB", Generation = 4 }
            },
            Slots = new[] { new KingmakerMountedCombat.Integration.MountedPersistenceService.Slot { ActorId = actors[0].UniqueId },
                new KingmakerMountedCombat.Integration.MountedPersistenceService.Slot { ActorId = actors[2].UniqueId } }
        };
        return service;
    }
    private static void usingDummyReset()
    {
        KingmakerMountedCombat.Integration.Relationship.Imports = 0;
        KingmakerMountedCombat.Integration.Relationship.Restores = 0;
        KingmakerMountedCombat.Integration.Relationship.Completes = 0;
        KingmakerMountedCombat.Integration.Participant.Restores = 0;
    }
    private static void Run()
    {
        Kingmaker.EntitySystem.Entities.UnitEntityData[] actors;
        var denied = Setup(out actors);
        actors[3].IsDirectlyControllable = false;
        denied.RestoreAreaPair();
        Check(denied.areaTransfer.ResumeAttempted && ReferenceEquals(denied.failedAreaTransfer, denied.areaTransfer) &&
            denied.areaRestoreFailure.Contains("directly controllable"), "second-pair native control refusal retains exact transfer custody");
        Check(denied.AreaResumeCount == 0 && denied.mountedWorld.Creates == 0 &&
            KingmakerMountedCombat.Integration.Relationship.Imports == 0 && KingmakerMountedCombat.Integration.Relationship.Restores == 0 &&
            KingmakerMountedCombat.Integration.Relationship.Completes == 0 && KingmakerMountedCombat.Integration.Participant.Restores == 0 &&
            denied.controls.SlotRestores == 0, "all-pair preflight refuses before first ownership or attachment mutation");
        Check(denied.controls.ResumeCalls == 1 && actors.All(a => a.NativeDebt == 2.5),
            "failure releases control suspension without editing native debt");
        denied.RestoreAreaPair();
        Check(denied.controls.ResumeCalls == 1 && denied.mountedWorld.Creates == 0, "failed restore is idempotent");
        var accepted = Setup(out actors);
        accepted.RestoreAreaPair();
        Check(accepted.failedAreaTransfer == null && accepted.AreaResumeCount == 2 && accepted.mountedWorld.Creates == 2 &&
            KingmakerMountedCombat.Integration.Relationship.Imports == 2 && KingmakerMountedCombat.Integration.Relationship.Restores == 2 &&
            KingmakerMountedCombat.Integration.Relationship.Completes == 2 && KingmakerMountedCombat.Integration.Participant.Restores == 2 &&
            accepted.controls.SlotRestores == 1 && accepted.controls.ResumeCalls == 2 && actors.All(a => a.NativeDebt == 2.5),
            "two eligible pairs restore exactly once without native budget mutation");
        accepted.RestoreAreaPair();
        Check(accepted.AreaResumeCount == 2 && accepted.mountedWorld.Creates == 2, "successful restore is idempotent");
    }
    private static int Main()
    {
        try { Run(); Console.WriteLine("AREA RESTORE PREFLIGHT ADAPTER PASS=" + passed + " FAIL=0"); return 0; }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
