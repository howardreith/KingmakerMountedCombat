using System;
using System.IO;
using System.Linq;
using Kingmaker;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.UI.Selection;
using Kingmaker.Utility;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class RuntimePersistenceScenario
    {
        // Campaign B's own manual archive, written by the engine in the
        // campaign-b run under isolated routing, opened in a fresh process as a
        // world of its own: B's identity, B's area, no pair, nothing restored or
        // invented, then ordinary movement of B's main character and a NEW
        // native save in B, with B's source archive byte-identical afterwards.
        private bool CampaignBColdCase => request.Scenario == "persistence-p07-load" && request.PersistenceCase == "campaign-b";
        private const string CampaignBSecondSaveName = "KMC_B2";
        private const string CampaignBSecondSaveLeaf = "Manual_303_KMC_B2.zks";
        private int campaignColdStage;
        private int campaignColdFrames;
        private UnitEntityData campaignColdMover;
        private Vector3 campaignColdOrigin;
        private Vector3 campaignColdDestination;
        private string campaignColdSaveHash;

        private static Vector3 FindWalkableNear(Vector3 origin, float distance)
        {
            for (var i = 0; i < 16; i++)
            {
                var wanted = origin + Quaternion.Euler(0, i * 22.5f, 0) * Vector3.forward * distance;
                var actual = Kingmaker.View.ObstacleAnalyzer.TraceAlongNavmesh(origin, wanted);
                if (GeometryUtils.MechanicsDistance(actual, wanted) <= 0.25f && GeometryUtils.MechanicsDistance(actual, origin) > distance - 0.5f) return actual;
            }
            throw new InvalidOperationException("No native walkable destination exists near the main character.");
        }

        private void AdvanceCampaignBCold()
        {
            if (clock.Elapsed.TotalSeconds > 200) throw new InvalidOperationException("P07 campaign B cold load timed out at " + campaignColdStage + ".");
            var game = Game.Instance;
            if (game == null || LoadingProcess.Instance.IsLoadingInProcess || game.CurrentlyLoadedArea == null ||
                game.CurrentMode != Kingmaker.GameModes.GameModeType.Default) return;
            stage = 1400 + campaignColdStage;
            if (campaignColdStage == 0)
            {
                if (++campaignColdFrames < 10 || game.Player?.MainCharacter.Value == null) return;
                var archive = Path.Combine(game.SaveManager.SavePath, request.PersistenceLoad.FileName);
                var party = game.Player.Party.ToArray();
                var mounts = game.State.Units.Where(u => SupportedMountedProfiles.IsSupported(u)).ToArray();
                campaignColdMover = game.Player.MainCharacter.Value;
                Write("initial");
                Write("campaign-b-cold-loaded", new JObject {
                    ["case"] = request.PersistenceCase, ["gameId"] = game.Player.GameId, ["gameName"] = game.Player.MainCharacter.Value.CharacterName,
                    ["expectedGameId"] = request.PersistenceLoad.GameId, ["fixtureGameId"] = request.Fixture.Working.GameId,
                    ["area"] = game.CurrentlyLoadedArea.AssetGuidThreadSafe, ["expectedArea"] = request.PersistenceLoad.Area,
                    ["party"] = party.Length, ["partyIds"] = new JArray(party.Select(u => u.UniqueId)), ["mainCharacter"] = campaignColdMover.UniqueId,
                    ["supportedMounts"] = mounts.Length, ["units"] = game.State.Units.Count(),
                    ["loadedDataPresent"] = persistence.LoadedData != null, ["loadedDataMounted"] = persistence.LoadedData?.Mounted,
                    ["loadedDataCampaign"] = persistence.LoadedData?.CampaignId, ["bindings"] = controls.CapturePersistentSlots().Length,
                    ["semantics"] = persistence.SemanticRestoreCount, ["presentation"] = persistence.PresentationRestoreCount,
                    ["nativeCastRequests"] = controls.NativeCastRequestCount, ["feedback"] = persistence.Feedback,
                    ["archivePath"] = archive, ["archiveSha256"] = Hash(archive), ["expectedSha256"] = request.PersistenceLoad.Sha256 });
                Check(game.Player.GameId == request.PersistenceLoad.GameId && game.Player.GameId != request.Fixture.Working.GameId &&
                    game.CurrentlyLoadedArea.AssetGuidThreadSafe == request.PersistenceLoad.Area,
                    "P07-B-archive-opened-Bs-own-campaign-and-area-not-As");
                Check(relationship.State == RelationshipState.Unmounted && persistence.LoadedData != null && !persistence.LoadedData.Mounted &&
                    persistence.LoadedData.CampaignId == request.PersistenceLoad.GameId && controls.CapturePersistentSlots().Length == 0 &&
                    persistence.SemanticRestoreCount == 0 && persistence.PresentationRestoreCount == 0 && controls.NativeCastRequestCount == 0,
                    "P07-B-cold-load-restores-nothing-and-invents-no-pair-or-binding");
                Check(party.Length >= 1 && mounts.Length == 0 && Hash(archive) == request.PersistenceLoad.Sha256,
                    "P07-B-world-has-no-supported-mount-and-its-archive-is-byte-identical");
                // Ordinary play in B: a real ground click for B's main character.
                campaignColdOrigin = campaignColdMover.Position;
                campaignColdDestination = FindWalkableNear(campaignColdOrigin, 3f);
                SelectionManager.Instance.SelectUnit(campaignColdMover.View, true, true, false);
                ClickGroundHandler.MoveSelectedUnitsToPoint(campaignColdDestination, false);
                campaignColdFrames = 0; campaignColdStage = 1;
                return;
            }
            if (campaignColdStage == 1)
            {
                var moved = GeometryUtils.MechanicsDistance(campaignColdMover.Position, campaignColdOrigin);
                if (moved < 1.5f || !campaignColdMover.Commands.Empty)
                { if (++campaignColdFrames > 1200) throw new InvalidOperationException("P07 B main character never moved: " + moved + " m."); return; }
                Write("campaign-b-cold-moved", new JObject { ["mover"] = campaignColdMover.UniqueId, ["displacement"] = moved,
                    ["origin"] = new JArray(campaignColdOrigin.x, campaignColdOrigin.y, campaignColdOrigin.z),
                    ["destination"] = new JArray(campaignColdDestination.x, campaignColdDestination.y, campaignColdDestination.z),
                    ["relationship"] = relationship.State.ToString(), ["partyCombat"] = game.Player.IsInCombat });
                Check(moved >= 1.5f && relationship.State == RelationshipState.Unmounted && !game.Player.IsInCombat,
                    "P07-Bs-main-character-moved-through-ordinary-native-input");
                Check(game.SaveManager.IsSaveAllowed(), "P07-B-admits-its-own-manual-save");
                callback = false;
                game.SaveGame(game.SaveManager.CreateNewSave(CampaignBSecondSaveName), () => callback = true);
                campaignColdFrames = 0; campaignColdStage = 2;
                return;
            }
            if (campaignColdStage == 2)
            {
                if (!callback || NativePersistenceIsolation.HasPendingWrites)
                { if (++campaignColdFrames > 2400) throw new InvalidOperationException("P07 B second save never completed: " + persistence.Feedback); return; }
                var saved = game.SaveManager.Single(s => s.Name == CampaignBSecondSaveName && s.HasFileOnDisk);
                var read = NativeMountedSaveStorage.Read(saved.Saver);
                var source = Path.Combine(game.SaveManager.SavePath, request.PersistenceLoad.FileName);
                campaignColdSaveHash = Hash(saved.FolderName);
                Write("campaign-b-cold-saved", new JObject { ["archive"] = new JObject {
                        ["path"] = saved.FolderName, ["leaf"] = saved.FileName, ["sha256"] = campaignColdSaveHash,
                        ["length"] = new FileInfo(saved.FolderName).Length, ["nativeType"] = saved.Type.ToString(),
                        ["internalName"] = saved.Name, ["gameId"] = saved.GameId, ["area"] = saved.Area?.AssetGuidThreadSafe,
                        ["operation"] = saved.OperationState.ToString(), ["kmcMember"] = read.Kind.ToString(),
                        ["snapshot"] = read.Data == null ? null : JObject.FromObject(read.Data, MountedSaveCodec.CreateSerializer()) },
                    ["sourceSha256"] = Hash(source), ["expectedSourceSha256"] = request.PersistenceLoad.Sha256,
                    ["snapshots"] = persistence.SnapshotCount, ["failedSaves"] = persistence.FailedSaveCount });
                Check(saved.FileName == CampaignBSecondSaveLeaf && saved.OperationState == SaveInfo.StateType.None &&
                    saved.GameId == request.PersistenceLoad.GameId && read.Kind == MountedSaveReadKind.Current && !read.Data.Mounted &&
                    read.Data.Rider == null && read.Data.Mount == null && read.Data.CampaignId == request.PersistenceLoad.GameId &&
                    read.Data.AreaId == request.PersistenceLoad.Area && persistence.SnapshotCount == 1 && persistence.FailedSaveCount == 0,
                    "P07-Bs-second-save-is-Bs-own-clean-archive");
                Check(Hash(source) == request.PersistenceLoad.Sha256, "P07-Bs-source-archive-is-byte-identical-after-its-own-write");
                Dispose();
                Result = new RuntimeSubscenarioResult { Name = request.Scenario, Status = "PASS",
                    AssertionPassCount = passed, AssertionFailCount = 0, Errors = new string[0] };
                Completed = true;
            }
        }
    }
}
