using System;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.GameModes;
using KingmakerMountedCombat.Domain;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class RuntimeMovementScenarioEngine
    {
        private const string SlopeWorkingArea = "9d1278a2f599b2a4daab53abdfe88d2e";
        private const string SlopeHubArea = "fd1b6fa9f788ca24e86bd922a10da080";
        private const string SlopeHubEntry = "104849f5f7ea36748aeeb036551047a9";
        private readonly BoundaryFailureDrain slopeLocationFailure = new BoundaryFailureDrain();
        private readonly JObject slopeLocation = new JObject {
            ["method"] = "Game.LoadArea/06000CC9", ["entry"] = SlopeHubEntry, ["autoSave"] = "None",
            ["dispatches"] = 0, ["loadingFrames"] = 0, ["stableFrames"] = 0,
            ["status"] = "pending", ["failure"] = null, ["before"] = null, ["after"] = null };

        private static bool SlopeLocationLoading => LoadingProcess.Instance != null && LoadingProcess.Instance.IsLoadingInProcess;

        private JObject CaptureSlopeLocationIdentity()
        {
            var game = Game.Instance;
            if (game?.Player?.MainCharacter.Value == null || game.CurrentlyLoadedArea == null) return null;
            if (!relationship.TryResolveAutomationPair(out var exactRider, out var exactMount, out var error)) return null;
            return new JObject {
                ["game"] = game.Player.GameId, ["main"] = game.Player.MainCharacter.Value.UniqueId,
                ["rider"] = exactRider.UniqueId, ["mount"] = exactMount.UniqueId,
                ["area"] = game.CurrentlyLoadedArea.AssetGuidThreadSafe, ["mode"] = game.CurrentMode.ToString(),
                ["combat"] = game.Player.IsInCombat || exactRider.IsInCombat || exactMount.IsInCombat,
                ["relationship"] = relationship.State.ToString(),
                ["viewsReady"] = exactRider.View != null && exactMount.View != null &&
                    exactRider.View.gameObject.activeInHierarchy && exactMount.View.gameObject.activeInHierarchy };
        }

        private void FailSlopeLocation(string reason)
        {
            if (slopeLocationFailure.IsLatched) return;
            slopeLocationFailure.Request(reason, SlopeLocationLoading);
            slopeLocation["status"] = "failed"; slopeLocation["failure"] = reason;
            FailCurrent("Native slope location preparation: " + reason);
        }

        private void AdvanceSlopeLocation()
        {
            // Preparation shares the unchanged row/suite clocks. A failure
            // drains native loading before touching selection or final cleanup.
            if (rowClock.Elapsed.TotalSeconds > RowTimeoutSeconds)
                FailSlopeLocation("The native area preparation exceeded the existing row deadline.");
            if (slopeLocationFailure.IsLatched)
            {
                if (slopeLocationFailure.Observe(SlopeLocationLoading) == BoundaryFailureDrainState.DrainingActiveLoad) return;
                BeginCleanup(CleanupTrigger.Exception); return;
            }
            try
            {
                if ((int)slopeLocation["dispatches"] == 0)
                {
                    var before = CaptureSlopeLocationIdentity();
                    if (request.Scenario != "chunk4-traversal-slope" || request.Fixture.Working.Area != SlopeWorkingArea ||
                        before == null || (string)before["game"] != request.Fixture.Working.GameId ||
                        (string)before["area"] != SlopeWorkingArea || (string)before["mode"] != "Default" ||
                        (bool)before["combat"] || !(bool)before["viewsReady"] || relationship.State != RelationshipState.Unmounted || SlopeLocationLoading)
                        throw new InvalidOperationException("Only the verified idle Working pair may enter the native slope location.");
                    var entry = ResourcesLibrary.TryGetBlueprint<BlueprintAreaEnterPoint>(SlopeHubEntry);
                    if (entry == null || entry.AssetGuidThreadSafe != SlopeHubEntry || entry.Area == null ||
                        entry.Area.AssetGuidThreadSafe != SlopeHubArea || entry.Area == Game.Instance.CurrentlyLoadedArea)
                        throw new InvalidOperationException("Exact native hub entry/area identity was not available; no alternate entry is allowed.");
                    slopeLocation["before"] = before;
                    slopeLocation["dispatches"] = 1; slopeLocation["status"] = "loading";
                    logger.Info("Native slope fixture location admitted: " + slopeLocation.ToString(Newtonsoft.Json.Formatting.None));
                    // This overload performs a real cross-area load. None keeps
                    // the native before-exit/after-entry autosave branches off.
                    Game.Instance.LoadArea(entry, AutoSaveMode.None);
                    return;
                }
                if (SlopeLocationLoading)
                {
                    slopeLocation["loadingFrames"] = (int)slopeLocation["loadingFrames"] + 1;
                    slopeLocation["stableFrames"] = 0;
                    return;
                }
                var game = Game.Instance;
                if (game?.CurrentlyLoadedArea == null || game.CurrentlyLoadedArea.AssetGuidThreadSafe != SlopeHubArea)
                {
                    if ((int)slopeLocation["loadingFrames"] > 0)
                        throw new InvalidOperationException("Native loading ended without the exact hub destination.");
                    return; // LoadArea queues its work; it need not start in this frame.
                }
                if (game.CurrentMode != GameModeType.Default)
                    throw new InvalidOperationException("Unexpected mode after native hub loading: " + game.CurrentMode + ". No UI is dismissed.");
                var after = CaptureSlopeLocationIdentity();
                slopeLocation["after"] = after;
                if (after == null || !(bool)after["viewsReady"])
                {
                    slopeLocation["stableFrames"] = 0; return;
                }
                foreach (var field in new[] { "game", "main", "rider", "mount" })
                    if (!JToken.DeepEquals(slopeLocation["before"][field], after[field]))
                        throw new InvalidOperationException("Native location changed the verified " + field + " identity.");
                if ((bool)after["combat"] || relationship.State != RelationshipState.Unmounted)
                    throw new InvalidOperationException("Native slope preparation did not remain idle and unmounted.");
                if ((int)slopeLocation["loadingFrames"] == 0)
                    throw new InvalidOperationException("No actual native loading interval was observed.");
                slopeLocation["stableFrames"] = (int)slopeLocation["stableFrames"] + 1;
                if ((int)slopeLocation["stableFrames"] < 10) return;
                slopeLocation["status"] = "ready";
                logger.Info("Native slope fixture location ready: " + slopeLocation.ToString(Newtonsoft.Json.Formatting.None));
                BeginPreparedMovementRow();
            }
            catch (Exception exception)
            {
                FailSlopeLocation(exception.GetType().Name + ": " + exception.Message);
                if (slopeLocationFailure.Observe(SlopeLocationLoading) != BoundaryFailureDrainState.DrainingActiveLoad)
                    BeginCleanup(CleanupTrigger.Exception);
            }
        }
    }
}
