using System;
using System.IO;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Persistence;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class RuntimePersistenceScenario
    {
        // A supported clean save (the cleanup archive a prepare-removal run
        // wrote) opened in a fresh process with KMC's gameplay and persistence
        // integration DETACHED before the load: every Harmony guard removed,
        // services off, so the engine deserializes the archive with no KMC
        // restoration, admission or snapshot at all. The DLL is still loaded
        // (it hosts this automation) and the run-scoped save isolation stays:
        // this is "integration absent", never "mod absent", and is reported so.
        internal static bool IsIntegrationAbsentCase(RuntimeRequest request) =>
            request != null && request.Scenario == "persistence-p07-load" && request.PersistenceCase == "absent-kmc";
        private bool IntegrationAbsentCase => IsIntegrationAbsentCase(request);
        private int absentFrames;

        private void AdvanceAbsentLoad()
        {
            if (clock.Elapsed.TotalSeconds > 150) throw new InvalidOperationException("P07 integration-absent load timed out.");
            var game = Game.Instance;
            if (game == null || LoadingProcess.Instance.IsLoadingInProcess || game.CurrentlyLoadedArea == null ||
                game.CurrentMode != Kingmaker.GameModes.GameModeType.Default || game.Player?.MainCharacter.Value == null) return;
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
            Dispose();
            Result = new RuntimeSubscenarioResult { Name = request.Scenario, Status = "PASS",
                AssertionPassCount = passed, AssertionFailCount = 0, Errors = new string[0] };
            Completed = true;
        }
    }
}
