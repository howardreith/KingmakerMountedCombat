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
        // A supported clean save (the cleanup archive a prepare-removal run
        // wrote) opened in a fresh process with KMC's gameplay and persistence
        // integration DETACHED before the load: every Harmony guard removed,
        // services off, so the engine deserializes the archive with no KMC
        // restoration, admission or snapshot at all. Then ordinary play without
        // KMC: a real ground click for the main character, and a NEW native
        // save written by the engine alone, which carries no KMC member. The
        // DLL is still loaded (it hosts this automation) and the run-scoped
        // save isolation stays: this is "integration absent", never "mod
        // absent", and is reported so; the true no-DLL load is its own case.
        internal static bool IsIntegrationAbsentCase(RuntimeRequest request) =>
            request != null && request.Scenario == "persistence-p07-load" && request.PersistenceCase == "absent-kmc";
        private bool IntegrationAbsentCase => IsIntegrationAbsentCase(request);
        private const string AbsentSecondSaveName = "KMC_ABSENT2";
        private const string AbsentSecondSaveLeaf = "Manual_302_KMC_ABSENT2.zks";
        private int absentStage;
        private int absentFrames;
        private UnitEntityData absentMover;
        private Vector3 absentOrigin;
        private Vector3 absentDestination;

        private void AdvanceAbsentLoad()
        {
            if (clock.Elapsed.TotalSeconds > 200) throw new InvalidOperationException("P07 integration-absent load timed out at " + absentStage + ".");
            var game = Game.Instance;
            if (game == null || LoadingProcess.Instance.IsLoadingInProcess || game.CurrentlyLoadedArea == null ||
                game.CurrentMode != Kingmaker.GameModes.GameModeType.Default || game.Player?.MainCharacter.Value == null) return;
            stage = 1500 + absentStage;
            if (absentStage == 0)
            {
                if (++absentFrames < 10) return;
                var archive = Path.Combine(game.SaveManager.SavePath, request.PersistenceLoad.FileName);
                var mammoth = game.State.Units.Count(u => u.Blueprint?.AssetGuid == SupportedMountedProfiles.MammothBlueprintGuid);
                var kmcHorses = game.State.Units.Count(u => u.Blueprint?.AssetGuid == HorseCompanionBlueprintService.UnitGuid);
                var detail = new JObject {
                    ["integrationDetached"] = integrationDetached, ["bridgeInstalled"] = MountedPatchController.BridgeInstalled,
                    ["enabled"] = persistence.Enabled, ["loadedData"] = persistence.LoadedData != null,
                    ["semantics"] = persistence.SemanticRestoreCount, ["presentation"] = persistence.PresentationRestoreCount,
                    ["nativeCastRequests"] = controls.NativeCastRequestCount, ["activeScope"] = persistence.HasActiveSaveScope,
                    ["gameId"] = game.Player.GameId, ["area"] = game.CurrentlyLoadedArea.AssetGuidThreadSafe,
                    ["expectedGameId"] = request.PersistenceLoad.GameId, ["expectedArea"] = request.PersistenceLoad.Area,
                    ["party"] = game.Player.Party.Count, ["units"] = game.State.Units.Count(),
                    ["mammothUnits"] = mammoth, ["kmcHorseUnits"] = kmcHorses,
                    ["archivePath"] = archive, ["archiveSha256"] = Hash(archive), ["expectedSha256"] = request.PersistenceLoad.Sha256,
                    ["sourceFileName"] = request.PersistenceLoad.FileName, ["mode"] = game.CurrentMode.ToString() };
                Write("initial");
                Write("absent-load-complete", detail);
                Check(integrationDetached && !MountedPatchController.BridgeInstalled && !persistence.Enabled,
                    "P07-KMC-integration-was-detached-before-the-native-load");
                Check(relationship.State == RelationshipState.Unmounted && persistence.LoadedData == null &&
                    persistence.SemanticRestoreCount == 0 && persistence.PresentationRestoreCount == 0 &&
                    controls.NativeCastRequestCount == 0 && !persistence.HasActiveSaveScope && !persistence.SaveSuspended,
                    "P07-the-engine-restored-the-save-with-no-KMC-restoration-admission-or-snapshot");
                Check(game.Player.GameId == request.PersistenceLoad.GameId &&
                    game.CurrentlyLoadedArea.AssetGuidThreadSafe == request.PersistenceLoad.Area &&
                    game.Player.Party.Count > 0 && game.CurrentMode == Kingmaker.GameModes.GameModeType.Default,
                    "P07-the-clean-save-opened-its-own-campaign-area-and-party");
                Check(kmcHorses == 0, "P07-the-clean-save-holds-no-KMC-blueprint-unit");
                Check(Hash(archive) == request.PersistenceLoad.Sha256, "P07-the-loaded-archive-is-byte-identical");
                // Ordinary play with the integration gone: a real ground click.
                absentMover = game.Player.MainCharacter.Value;
                absentOrigin = absentMover.Position;
                absentDestination = FindWalkableNear(absentOrigin, 3f);
                SelectionManager.Instance.SelectUnit(absentMover.View, true, true, false);
                ClickGroundHandler.MoveSelectedUnitsToPoint(absentDestination, false);
                absentFrames = 0; absentStage = 1;
                return;
            }
            if (absentStage == 1)
            {
                var moved = GeometryUtils.MechanicsDistance(absentMover.Position, absentOrigin);
                if (moved < 1.5f || !absentMover.Commands.Empty)
                { if (++absentFrames > 1200) throw new InvalidOperationException("P07 main character never moved without KMC: " + moved + " m."); return; }
                Write("absent-moved", new JObject { ["mover"] = absentMover.UniqueId, ["displacement"] = moved,
                    ["origin"] = new JArray(absentOrigin.x, absentOrigin.y, absentOrigin.z),
                    ["destination"] = new JArray(absentDestination.x, absentDestination.y, absentDestination.z),
                    ["relationship"] = relationship.State.ToString(), ["partyCombat"] = game.Player.IsInCombat,
                    ["bridgeInstalled"] = MountedPatchController.BridgeInstalled, ["enabled"] = persistence.Enabled });
                Check(moved >= 1.5f && relationship.State == RelationshipState.Unmounted && !game.Player.IsInCombat &&
                    !MountedPatchController.BridgeInstalled, "P07-the-main-character-moved-through-ordinary-native-input-without-KMC");
                Check(game.SaveManager.IsSaveAllowed(), "P07-the-engine-admits-a-manual-save-without-KMC");
                callback = false;
                game.SaveGame(game.SaveManager.CreateNewSave(AbsentSecondSaveName), () => callback = true);
                absentFrames = 0; absentStage = 2;
                return;
            }
            if (absentStage == 2)
            {
                if (!callback || NativePersistenceIsolation.HasPendingWrites)
                { if (++absentFrames > 2400) throw new InvalidOperationException("P07 save without KMC never completed."); return; }
                var saved = game.SaveManager.Single(s => s.Name == AbsentSecondSaveName && s.HasFileOnDisk);
                var read = NativeMountedSaveStorage.Read(saved.Saver);
                var source = Path.Combine(game.SaveManager.SavePath, request.PersistenceLoad.FileName);
                Write("absent-saved", new JObject { ["archive"] = new JObject {
                        ["path"] = saved.FolderName, ["leaf"] = saved.FileName, ["sha256"] = Hash(saved.FolderName),
                        ["length"] = new FileInfo(saved.FolderName).Length, ["nativeType"] = saved.Type.ToString(),
                        ["internalName"] = saved.Name, ["gameId"] = saved.GameId, ["area"] = saved.Area?.AssetGuidThreadSafe,
                        ["operation"] = saved.OperationState.ToString(), ["kmcMember"] = read.Kind.ToString() },
                    ["sourceSha256"] = Hash(source), ["expectedSourceSha256"] = request.PersistenceLoad.Sha256,
                    ["snapshots"] = persistence.SnapshotCount, ["bridgeInstalled"] = MountedPatchController.BridgeInstalled });
                Check(saved.FileName == AbsentSecondSaveLeaf && saved.OperationState == SaveInfo.StateType.None &&
                    saved.GameId == request.PersistenceLoad.GameId && read.Kind == MountedSaveReadKind.Missing &&
                    persistence.SnapshotCount == 0 && !MountedPatchController.BridgeInstalled,
                    "P07-the-engine-wrote-a-new-archive-alone-with-no-KMC-member");
                Check(Hash(source) == request.PersistenceLoad.Sha256, "P07-the-cleanup-archive-is-byte-identical-after-the-write");
                Dispose();
                Result = new RuntimeSubscenarioResult { Name = request.Scenario, Status = "PASS",
                    AssertionPassCount = passed, AssertionFailCount = 0, Errors = new string[0] };
                Completed = true;
            }
        }
    }
}
