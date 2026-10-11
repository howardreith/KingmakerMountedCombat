using System;
using System.IO;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.UI.Selection;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class RuntimePersistenceScenario
    {
        private bool ChargeLifecycleCase => request.Scenario == "persistence-p07-save" &&
            RuntimeRequest.IsChargeLifecycleCase(Checkpoint);
        private Player chargeLifecycleWorld;
        private string chargeLifecycleRider, chargeLifecycleMount, chargeLifecycleArea;
        private int chargeLifecycleStage, chargeLifecycleFrames;
        private bool chargeLifecycleLoading, chargeLifecycleFixtureReleased;
        private JObject chargeLifecycleRequest;

        private JObject ChargeLifecycleObservation() => new JObject
        {
            ["case"] = Checkpoint, ["riderId"] = chargeLifecycleRider, ["mountId"] = chargeLifecycleMount,
            ["sameWorld"] = ReferenceEquals(chargeLifecycleWorld, Game.Instance?.Player),
            ["sourceArea"] = chargeLifecycleArea, ["area"] = Game.Instance?.CurrentlyLoadedArea?.AssetGuidThreadSafe,
            ["loading"] = LoadingProcess.Instance.IsLoadingInProcess, ["loadingObserved"] = chargeLifecycleLoading,
            ["relationship"] = relationship.State.ToString(), ["charge"] = ChargePersistenceObservation(),
            ["rules"] = realtimeProbe?.CapturePairEvidence(), ["request"] = chargeLifecycleRequest?.DeepClone(),
            ["riderAttacks"] = realtimeProbe?.RiderNonOpportunityAttackRuleCount,
            ["riderResolved"] = realtimeProbe?.RiderResolvedCount,
            ["mountAttacks"] = realtimeProbe?.MountNonOpportunityAttackRuleCount,
            ["enabled"] = persistence.Enabled, ["patchesInstalled"] = MountedPatchController.BridgeInstalled,
            ["resetPending"] = persistence.ResetToMainMenuPending,
            ["resetDeferred"] = persistence.ResetToMainMenuDeferredCount,
            ["areaRefused"] = persistence.RefusedAreaTransferCount,
            ["areaSuspensions"] = persistence.AreaSuspensionCount, ["areaResumes"] = persistence.AreaResumeCount,
            ["snapshotCount"] = persistence.SnapshotCount,
            ["fixtureReleased"] = chargeLifecycleFixtureReleased,
            ["removalState"] = removal.State.ToString(), ["cleanupSaves"] = removal.CleanupSaveCount,
            ["cleanupPath"] = removal.CleanupSavePath, ["cleanupSha256"] = removal.CleanupSaveSha256,
            ["cleanupLeaf"] = removal.CleanupSaveLeaf, ["cleanupBinding"] = removal.CleanupBinding,
            ["cleanupReferences"] = new JArray(removal.CleanupReferenceHits),
            ["cleanupScannedMembers"] = removal.CleanupScannedMembers
        };

        private void BeginChargeLifecycleBoundary()
        {
            chargeLifecycleWorld = Game.Instance.Player;
            chargeLifecycleRider = rider.UniqueId; chargeLifecycleMount = mount.UniqueId;
            chargeLifecycleArea = Game.Instance.CurrentlyLoadedArea.AssetGuidThreadSafe;
            chargeLifecycleRequest = new JObject();
            combat.ChargeOwnershipDrained += ObserveChargeLifecycleDrain;
            MountedChargeAdmissionFault.BeforeCleanupStep = stepName =>
            {
                if (stepName != "mount-speed-override") return;
                chargeFaultFired = true;
                throw new InvalidOperationException("Owned P07 held charge speed cleanup fault");
            };
            Write("charge-lifecycle-before", ChargeLifecycleObservation());
            switch (Checkpoint)
            {
                case "mounted-charge-area":
                    Game.Instance.ReloadArea();
                    chargeLifecycleRequest["method"] = "Game.ReloadArea:06000CD6";
                    break;
                case "mounted-charge-session":
                    Game.Instance.ResetToMainMenu(null, null);
                    chargeLifecycleRequest["method"] = "Game.ResetToMainMenu:06000CDD";
                    break;
                case "mounted-charge-disable":
                    chargeLifecycleRequest["disableAccepted"] = Main.InvokeRegisteredToggleForAutomation(false);
                    chargeLifecycleRequest["unloadAccepted"] = Main.InvokeRegisteredUnloadForAutomation();
                    break;
                case "mounted-charge-removal":
                    var assessment = removal.Assess();
                    chargeLifecycleRequest["removalReasons"] = new JArray(assessment.Reasons);
                    chargeLifecycleRequest["removalAccepted"] = removal.Begin();
                    SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                    chargeLifecycleRequest["nativeStopSent"] = true;
                    try { SelectionManager.Instance.Stop(); }
                    catch (InvalidOperationException exception) when (chargeFaultFired && combat.HasChargeOwnership &&
                        exception.Message.StartsWith("Mounted charge cleanup remains owned:", StringComparison.Ordinal))
                    {
                        chargeLifecycleRequest["nativeStopRefused"] = true;
                        chargeLifecycleRequest["nativeStopError"] = exception.Message;
                    }
                    break;
            }
            Write("charge-lifecycle-held", ChargeLifecycleObservation());
            // Keep the fault through a complete ordinary update. An attempted boundary
            // must leave the exact owner retained, with no native snapshot or disposal.
            stage = 70; chargeLifecycleStage = 0;
        }

        private void ReleaseChargeLifecycleFixture()
        {
            if (chargeLifecycleFixtureReleased) return;
            // This releases diagnostic world references only AFTER the refused boundary
            // has been recorded. The exact charge owner remains production-owned.
            targetService?.Dispose(); targetService = null; combatTarget = null;
            restoreRealtimeAi?.Invoke(); restoreRealtimeAi = null;
            chargeLifecycleFixtureReleased = true;
        }

        private void ObserveChargeLifecycleDrain()
        {
            if (!ChargeLifecycleCase || !chargeFaultObserved) return;
            // This is inside the successful barrier, before a pending reset can
            // replace the old world. Record raw debt without changing it.
            Write("charge-lifecycle-cleanup-complete", ChargeLifecycleObservation());
            combat.ChargeOwnershipDrained -= ObserveChargeLifecycleDrain;
        }

        private void AdvanceChargeLifecycle()
        {
            if (clock.Elapsed.TotalSeconds > 240)
                throw new InvalidOperationException("Charge lifecycle timed out at " + chargeLifecycleStage);
            chargeLifecycleLoading |= LoadingProcess.Instance.IsLoadingInProcess;
            if (chargeLifecycleStage == 0)
            {
                if (++chargeLifecycleFrames < 2) return;
                if (Checkpoint == "mounted-charge-removal")
                {
                    var assessment = removal.Assess();
                    chargeLifecycleRequest["retainedDebtAssessment"] = new JObject {
                        ["safe"] = assessment.Safe, ["activeCommand"] = combat.HasActiveCommand,
                        ["ownedCharge"] = combat.HasChargeOwnership, ["worldActiveCommand"] = assessment.World.ActiveMountedCommand,
                        ["reasons"] = new JArray(assessment.Reasons) };
                }
                Write("charge-lifecycle-debt-retained", ChargeLifecycleObservation());
                if (!chargeFaultFired || !combat.HasChargeOwnership || persistence.SnapshotCount != 0 ||
                    !ReferenceEquals(chargeLifecycleWorld, Game.Instance.Player) || Game.Instance.CurrentlyLoadedArea == null)
                    throw new InvalidOperationException("Held charge debt did not retain its exact world and owner.");
                chargeFaultObserved = true;
                // A deferred main-menu request replays before the observer's next update.
                // Release the diagnostic target/AI leases while this world still exists.
                if (Checkpoint == "mounted-charge-session" || Checkpoint == "mounted-charge-area") ReleaseChargeLifecycleFixture();
                Write("charge-lifecycle-retry-ready", ChargeLifecycleObservation());
                MountedChargeAdmissionFault.BeforeCleanupStep = null;
                chargeLifecycleStage = 1; return;
            }
            if (combat.HasChargeOwnership) return;
            if (chargeLifecycleStage == 1)
            {
                if (Checkpoint == "mounted-charge-session" && Game.Instance.CurrentlyLoadedArea == null)
                { rider = null; mount = null; }
                Write("charge-lifecycle-drained", ChargeLifecycleObservation());
                if (Checkpoint == "mounted-charge-area")
                {
                    chargeLifecycleRequest["carryEligibleAtRetry"] = !Game.Instance.Player.IsInCombat && !rider.IsInCombat && !mount.IsInCombat &&
                        relationship.State == RelationshipState.Mounted;
                    Write("charge-lifecycle-area-retry", ChargeLifecycleObservation());
                    Game.Instance.ReloadArea();
                }
                else if (Checkpoint == "mounted-charge-disable")
                {
                    chargeLifecycleRequest["settledDisableAccepted"] = Main.InvokeRegisteredToggleForAutomation(false);
                    Write("charge-lifecycle-disabled", ChargeLifecycleObservation());
                    chargeLifecycleRequest["reenableAccepted"] = Main.InvokeRegisteredToggleForAutomation(true);
                }
                else if (Checkpoint == "mounted-charge-removal") ReleaseChargeLifecycleFixture();
                chargeLifecycleStage = 2; chargeLifecycleFrames = 0; return;
            }
            if (LoadingProcess.Instance.IsLoadingInProcess || NativePersistenceIsolation.HasPendingWrites) return;
            if (Checkpoint == "mounted-charge-session")
            {
                if (Game.Instance.CurrentlyLoadedArea != null || persistence.ResetToMainMenuPending) return;
                rider = null; mount = null;
            }
            else if (Checkpoint == "mounted-charge-area")
            {
                if (!chargeLifecycleLoading || persistence.AreaTransitionPending ||
                    Game.Instance.CurrentlyLoadedArea?.AssetGuidThreadSafe != chargeLifecycleArea) return;
            }
            else if (Checkpoint == "mounted-charge-removal")
            {
                if (Game.Instance.IsPaused) { Game.Instance.IsPaused = false; return; }
                if (Game.Instance.Player.IsInCombat || rider.IsInCombat || mount.IsInCombat) return;
                if (chargeLifecycleStage == 2)
                {
                    if (!Game.Instance.SaveManager.IsSaveAllowed()) return;
                    callback = false;
                    Game.Instance.SaveGame(Game.Instance.SaveManager.CreateNewSave("KMC_P01"), () => callback = true);
                    chargeLifecycleStage = 3; return;
                }
                if (chargeLifecycleStage == 3)
                {
                    if (!callback) return;
                    var first = Game.Instance.SaveManager.Single(s => s.FileName == "Manual_300_KMC_P01.zks");
                    Write("charge-removal-opening-write", new JObject { ["path"] = first.FolderName,
                        ["sha256"] = Hash(first.FolderName), ["length"] = new FileInfo(first.FolderName).Length });
                    if (!removal.Begin()) throw new InvalidOperationException("Settled charge removal refused: " + removal.Status);
                    chargeLifecycleStage = 4; return;
                }
                if (removal.State == RemovalPreparationState.Saving) return;
                if (removal.State != RemovalPreparationState.Ready)
                    throw new InvalidOperationException("Charge removal did not reach readiness: " + removal.Status);
            }
            if (Checkpoint != "mounted-charge-session" && Game.Instance.CurrentMode != Kingmaker.GameModes.GameModeType.Default)
            {
                if (Game.Instance.IsPaused) Game.Instance.IsPaused = false;
                return;
            }
            if (++chargeLifecycleFrames < 3) return;
            Write("charge-lifecycle-complete", ChargeLifecycleObservation());
            Dispose();
            Result = new RuntimeSubscenarioResult { Name = request.Scenario, Status = "PASS",
                AssertionPassCount = passed, AssertionFailCount = 0, Errors = new string[0] };
            Completed = true;
        }
    }
}
