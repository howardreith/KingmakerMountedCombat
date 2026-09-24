using System;
using System.IO;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class RuntimePersistenceScenario
    {
        // The user-visible Prepare-to-Disable/removal contract: refused, with
        // the exact reason, while a permanent KMC Horse reference exists in the
        // loaded world; refused again while the party is in real combat with a
        // native enemy even though the engine itself would admit a save under
        // the qualified paired policy; otherwise the pair is dismounted through
        // the registered disable's own cleanup and a NEW native save is written,
        // bound to the persistence service's own completed-write record for
        // that request and verified from its bytes: no pair,
        // no combat supplement, no control binding, and no KMC-registered
        // blueprint identity in any member. Then the real registered disable and
        // re-enable, and the shared continuation.
        private bool RemovalCase => request.Scenario == "persistence-p07-save" && request.PersistenceCase == "prepare-removal";
        private int removalStage;
        private int removalFrames;
        private UnitEntityData removalSpawned;
        private string removalSpawnedId;
        private string removalFirstPath;
        private string removalFirstHash;
        private RemovalAssessment removalRefusedAssessment;
        private RemovalAssessment removalCombatAssessment;
        private RemovalAssessment removalSafeAssessment;
        private bool removalBeganRefused;
        private bool removalCombatRefused;
        private bool removalCombatSaveAllowed;
        private string removalCombatTargetId;
        private bool removalBegan;
        private int removalFactsMounted;
        private int removalFactsDisabled;
        private int removalFactsReEnabled;
        private bool removalDisabled;
        private bool removalReEnabled;
        private bool removalRemounted;
        private string removalRiderId;
        private string removalMountId;

        private void AdvanceRemoval()
        {
            if (clock.Elapsed.TotalSeconds > 280)
                throw new InvalidOperationException("P07 removal preparation timed out at " + removalStage +
                    " frames=" + removalFrames + ": " + removal.Status);
            var game = Game.Instance;
            if (game == null) return;
            stage = 1000 + removalStage;
            if (LoadingProcess.Instance.IsLoadingInProcess || game.CurrentlyLoadedArea == null) return;
            // Real combat pauses the game (Pause is a game mode): inside the combat
            // stages the walk unpauses first, exactly as the death walk does, and
            // gates on Default mode only outside them. Run final99-p07-removal
            // measured the earlier all-stage Default gate spinning to the deadline.
            if (removalStage == 2 || removalStage == 3)
            {
                // The memory lease is refreshed only while the encounter is meant
                // to continue; stage 3 is its teardown (final100-p07-removal).
                if (removalStage == 2 && !targetService.RefreshBidirectionalCombatMemoryLease())
                    throw new InvalidOperationException("P07 removal native combat memory fixture lease was lost.");
                if (game.IsPaused) { Write("fixture-native-unpause"); game.IsPaused = false; return; }
            }
            else if (game.CurrentMode != Kingmaker.GameModes.GameModeType.Default) return;
            if (removalStage == 0)
            {
                if (!callback || NativePersistenceIsolation.HasPendingWrites) return;
                Check(relationship.State == RelationshipState.Mounted && persistence.SnapshotCount == 1,
                    "P07-removal-opens-with-one-real-mounted-save");
                rider = relationship.Rider; mount = relationship.Mount;
                removalRiderId = rider.UniqueId; removalMountId = mount.UniqueId;
                beforeControls = controls.CaptureSnapshot();
                removalFactsMounted = beforeControls.ExactFactCount;
                var first = RecoveryArchive();
                removalFirstPath = first.FolderName; removalFirstHash = Hash(removalFirstPath);
                var read = NativeMountedSaveStorage.Read(first.Saver);
                Check(read.Kind == MountedSaveReadKind.Current && read.Data.Mounted &&
                    read.Data.Rider.Id == removalRiderId && read.Data.Mount.Id == removalMountId,
                    "P07-removal-first-archive-carries-the-pair");
                Write("native-write-complete", new JObject {
                    ["ordinal"] = 1, ["path"] = removalFirstPath, ["sha256"] = removalFirstHash,
                    ["length"] = new FileInfo(removalFirstPath).Length, ["nativeType"] = first.Type.ToString(),
                    ["nativeCallback"] = callback, ["operation"] = first.OperationState.ToString(),
                    ["snapshot"] = JObject.FromObject(read.Data, MountedSaveCodec.CreateSerializer()) });
                // A real permanent reference: the KMC Horse blueprint itself,
                // spawned into the loaded world through the engine's own creator.
                Check(horseCompanion.State == HorseCompanionBlueprintState.Registered && horseCompanion.HorseUnit != null &&
                    horseCompanion.HorseUnit.AssetGuid == HorseCompanionBlueprintService.UnitGuid,
                    "P07-removal-has-the-registered-KMC-Horse-blueprint");
                var state = game.State.LoadedAreaState.MainState;
                removalSpawned = game.EntityCreator.SpawnUnit(horseCompanion.HorseUnit, FindDestination(6f), Quaternion.identity, state);
                game.EntityCreator.Tick();
                Check(removalSpawned != null && removalSpawned.IsInState &&
                    removalSpawned.Blueprint.AssetGuid == HorseCompanionBlueprintService.UnitGuid &&
                    game.State.Units.Contains(removalSpawned), "P07-removal-KMC-Horse-unit-is-in-the-loaded-world");
                removalSpawnedId = removalSpawned.UniqueId;
                removalRefusedAssessment = removal.Assess();
                removalBeganRefused = !removal.Begin();
                Check(!removalRefusedAssessment.Safe && removalRefusedAssessment.Mounted &&
                    removalRefusedAssessment.PermanentReferences.Any(r => r.IndexOf(removalSpawnedId, StringComparison.Ordinal) >= 0),
                    "P07-removal-assessment-names-the-exact-permanent-KMC-Horse-reference");
                Check(removalBeganRefused && removal.State == RemovalPreparationState.Refused && removal.RefusalCount == 1 &&
                    removal.CleanupSaveCount == 0 && relationship.State == RelationshipState.Mounted &&
                    persistence.SnapshotCount == 1 && !persistence.SaveSuspended && !persistence.HasActiveSaveScope,
                    "P07-unsafe-removal-is-refused-without-dismounting-or-saving");
                Write("removal-refused", RemovalDetail(new JObject {
                    ["spawnedId"] = removalSpawnedId, ["spawnedBlueprint"] = removalSpawned.Blueprint.AssetGuid,
                    ["references"] = new JArray(removalRefusedAssessment.PermanentReferences),
                    ["reasons"] = new JArray(removalRefusedAssessment.Reasons) }));
                removalSpawned.Destroy();
                game.EntityDestroyer?.Tick();
                removalFrames = 0; removalStage = 1;
                return;
            }
            if (removalStage == 1)
            {
                if (game.State.Units.Any(u => u.UniqueId == removalSpawnedId))
                {
                    if (++removalFrames > 600) throw new InvalidOperationException("P07 removal probe unit was never destroyed.");
                    return;
                }
                // Real combat with a native enemy through the same target fixture
                // every combat case uses. The engine admits a manual save under
                // the qualified paired policy; the removal contract must not.
                targetService = new DiagnosticCombatTargetService(logger);
                var target = targetService.Spawn(rider, mount, FindDestination(8f), request.RunId + "-removal-combat", true, true);
                removalCombatTargetId = target.UniqueId;
                Check(targetService.PrepareForPlayerClick(target), "P07-removal-combat-enemy-prepared");
                Check(targetService.QueueBidirectionalCombatMemory(rider, target), "P07-removal-native-combat-requested");
                removalFrames = 0; removalStage = 2;
                return;
            }
            if (removalStage == 2)
            {
                if (!game.Player.IsInCombat || !rider.IsInCombat || !rider.CombatState.CanActInCombat)
                { if (++removalFrames > 3600) throw new InvalidOperationException("P07 removal fixture never entered native combat."); return; }
                removalCombatSaveAllowed = game.SaveManager.IsSaveAllowed();
                removalCombatAssessment = removal.Assess();
                removalCombatRefused = !removal.Begin();
                Check(!removalCombatAssessment.Safe && removalCombatAssessment.Mounted && removalCombatAssessment.World != null &&
                    removalCombatAssessment.World.PartyInCombat && removalCombatAssessment.PermanentReferences.Count == 0 &&
                    removalCombatAssessment.Reasons.Any(r => r.IndexOf("in combat", StringComparison.Ordinal) >= 0),
                    "P07-removal-assessment-refuses-real-combat-by-its-own-rule");
                Check(removalCombatRefused && removal.State == RemovalPreparationState.Refused && removal.RefusalCount == 2 &&
                    removal.CleanupSaveCount == 0 && relationship.State == RelationshipState.Mounted &&
                    persistence.SnapshotCount == 1 && !persistence.SaveSuspended && !persistence.HasActiveSaveScope,
                    "P07-removal-in-combat-is-refused-without-dismounting-or-saving");
                Write("removal-refused-combat", RemovalDetail(new JObject {
                    ["targetId"] = removalCombatTargetId, ["engineSaveAllowed"] = removalCombatSaveAllowed,
                    ["reasons"] = new JArray(removalCombatAssessment.Reasons) }));
                removalFrames = 0; removalStage = 3;
                return;
            }
            if (removalStage == 3)
            {
                // End the encounter through the target fixture's own teardown and
                // wait for the world to settle by the removal contract's own rule.
                if (targetService != null)
                {
                    if (!targetService.DestroyAndVerify()) { if (++removalFrames > 3600) throw new InvalidOperationException("P07 removal enemy never left the world."); return; }
                    targetService.Dispose(); targetService = null; removalFrames = 0;
                }
                if (game.Player.IsInCombat || rider.IsInCombat || mount.IsInCombat) return;
                if (game.CurrentMode != Kingmaker.GameModes.GameModeType.Default)
                { if (++removalFrames > 3600) throw new InvalidOperationException("P07 removal world never returned to Default mode after combat: " + game.CurrentMode); return; }
                removalSafeAssessment = removal.Assess();
                if (!removalSafeAssessment.Safe)
                {
                    if (++removalFrames < 600) return;
                    throw new InvalidOperationException("P07 removal assessment never settled after combat: " + removalSafeAssessment.Summary);
                }
                Check(removalSafeAssessment.Safe && removalSafeAssessment.Mounted && removalSafeAssessment.PermanentReferences.Count == 0 &&
                    !removalSafeAssessment.World.InspectionFailed,
                    "P07-removal-assessment-is-safe-once-the-reference-and-the-combat-are-gone");
                removalBegan = removal.Begin();
                Check(removalBegan && removal.State == RemovalPreparationState.Saving &&
                    relationship.State == RelationshipState.Unmounted,
                    "P07-prepare-to-disable-dismounts-through-cleanup-before-saving");
                Write("removal-requested", RemovalDetail(null));
                removalFrames = 0; removalStage = 4;
                return;
            }
            if (removalStage == 4)
            {
                if (removal.State == RemovalPreparationState.Saving)
                {
                    if (++removalFrames % 600 == 0)
                        Write("removal-saving-probe", new JObject { ["frames"] = removalFrames, ["status"] = removal.Status,
                            ["pending"] = removal.PendingDiagnostics, ["callback"] = callback, ["pendingWrites"] = NativePersistenceIsolation.HasPendingWrites,
                            ["saveSuspended"] = persistence.SaveSuspended, ["activeScope"] = persistence.HasActiveSaveScope,
                            ["completed"] = persistence.CompletedSaveCount, ["lastCompletedPath"] = persistence.LastCompletedSave?.Path,
                            ["commits"] = NativeMountedArchiveCommit.CommitCount, ["lastCommitted"] = NativeMountedArchiveCommit.LastCommittedDestination,
                            ["seconds"] = clock.Elapsed.TotalSeconds });
                    if (removalFrames > 7200) throw new InvalidOperationException("P07 cleanup save never settled: " + removal.Status + " [" + removal.PendingDiagnostics + "]");
                    return;
                }
                if (NativePersistenceIsolation.HasPendingWrites) return;
                Check(removal.State == RemovalPreparationState.Ready && removal.CleanupSaveCount == 1 && removal.UnconfirmedCount == 0 &&
                    removal.CleanupSaveLeaf == "Manual_301_KMC_CLEANUP.zks" && !string.IsNullOrEmpty(removal.CleanupSavePath) &&
                    File.Exists(removal.CleanupSavePath) && Hash(removal.CleanupSavePath) == removal.CleanupSaveSha256,
                    "P07-cleanup-save-is-a-new-exact-declared-archive");
                // Both archives this walk writes are first-ever saves: the engine
                // writes them in place and KMC's replacement-commit record stays
                // at zero (SerializeAndSaveThread reaches the replacement site
                // only for an original archive). Readiness is bound to the
                // persistence service's completed-write record for this request.
                var completion = persistence.LastCompletedSave;
                Check(removal.CleanupBinding == "bound" && removal.CleanupCampaignId == request.Fixture.Working.GameId &&
                    persistence.CompletedSaveCount == 2 && completion != null && completion.Path == removal.CleanupSavePath &&
                    completion.Written != null && completion.Written.FileName == removal.CleanupSaveLeaf &&
                    NativeMountedArchiveCommit.CommitCount == 0 && NativeMountedArchiveCommit.LastCommittedDestination == null,
                    "P07-cleanup-save-is-bound-to-this-operation-own-completed-write");
                Check(removal.CleanupScannedMembers > 0 && removal.CleanupReferenceHits.Count == 0,
                    "P07-cleanup-archive-members-carry-no-KMC-blueprint-identity");
                var cleanup = game.SaveManager.Single(s => s.FolderName == removal.CleanupSavePath);
                var read = NativeMountedSaveStorage.Read(cleanup.Saver);
                Check(ReferenceEquals(cleanup, completion.Written) && cleanup.Name == MountedRemovalPreparation.CleanupSaveName && cleanup.OperationState == SaveInfo.StateType.None &&
                    cleanup.GameId == request.Fixture.Working.GameId &&
                    read.Kind == MountedSaveReadKind.Current && !read.Data.Mounted && read.Data.Rider == null && read.Data.Mount == null &&
                    read.Data.Combat == null && read.Data.Slots != null && read.Data.Slots.Length == 0 &&
                    read.Data.CampaignId == request.Fixture.Working.GameId,
                    "P07-cleanup-archive-records-no-pair-combat-or-binding-in-the-fixture-campaign");
                Check(Hash(removalFirstPath) == removalFirstHash, "P07-cleanup-save-left-the-first-archive-untouched");
                Check(relationship.State == RelationshipState.Unmounted && !persistence.SaveSuspended &&
                    !persistence.HasActiveSaveScope && controls.CaptureSnapshot().DuplicateFactCount == 0 &&
                    persistence.SnapshotCount == 2 && persistence.FailedSaveCount == 0,
                    "P07-prepared-state-holds-no-pair-lease-or-duplicate-control");
                Write("removal-prepared", RemovalDetail(new JObject {
                    ["cleanup"] = new JObject {
                        ["path"] = cleanup.FolderName, ["leaf"] = cleanup.FileName, ["sha256"] = Hash(cleanup.FolderName),
                        ["length"] = new FileInfo(cleanup.FolderName).Length, ["nativeType"] = cleanup.Type.ToString(),
                        ["internalName"] = cleanup.Name, ["gameId"] = cleanup.GameId, ["area"] = cleanup.Area?.AssetGuidThreadSafe,
                        ["operation"] = cleanup.OperationState.ToString(), ["kmcMember"] = read.Kind.ToString(),
                        ["snapshot"] = JObject.FromObject(read.Data, MountedSaveCodec.CreateSerializer()) },
                    ["firstSha256"] = Hash(removalFirstPath) }));
                // What the user does next: the exact registered disable, then
                // re-enable and remount, so the shared continuation proves play.
                removalDisabled = Main.InvokeRegisteredToggleForAutomation(false);
                removalFactsDisabled = controls.CaptureSnapshot().ExactFactCount;
                removalReEnabled = Main.InvokeRegisteredToggleForAutomation(true);
                removalRemounted = relationship.MountAutomationPair().Succeeded && relationship.State == RelationshipState.Mounted;
                rider = relationship.Rider; mount = relationship.Mount;
                var after = controls.CaptureSnapshot();
                removalFactsReEnabled = after.ExactFactCount;
                Write("removal-disabled-reenabled", RemovalDetail(null));
                Check(removalDisabled && removalFactsDisabled < removalFactsMounted,
                    "P07-registered-disable-succeeds-from-the-prepared-state");
                Check(removalReEnabled && removalRemounted && rider != null && mount != null &&
                    rider.UniqueId == removalRiderId && mount.UniqueId == removalMountId,
                    "P07-re-enable-and-remount-reuse-the-same-actors");
                Check(after.DuplicateFactCount == 0 && removalFactsReEnabled <= removalFactsMounted &&
                    removalFactsReEnabled > removalFactsDisabled && controls.NativeCastRequestCount == 0,
                    "P07-re-enable-grants-nothing-extra-and-casts-no-Mount");
                beforeControls = after;
                recoveryContinuation = true; stage = 2;
            }
        }

        private JObject RemovalDetail(JObject extra)
        {
            var detail = new JObject {
                ["case"] = request.PersistenceCase, ["stage"] = removalStage, ["frames"] = removalFrames,
                ["riderId"] = removalRiderId, ["mountId"] = removalMountId,
                ["state"] = removal.State.ToString(), ["status"] = removal.Status,
                ["assessments"] = removal.AssessmentCount, ["refusals"] = removal.RefusalCount,
                ["unconfirmed"] = removal.UnconfirmedCount,
                ["cleanupSaves"] = removal.CleanupSaveCount, ["cleanupLeaf"] = removal.CleanupSaveLeaf,
                ["cleanupSha256"] = removal.CleanupSaveSha256, ["cleanupPath"] = removal.CleanupSavePath,
                ["cleanupCampaign"] = removal.CleanupCampaignId, ["binding"] = removal.CleanupBinding,
                ["scannedMembers"] = removal.CleanupScannedMembers, ["scannedBytes"] = removal.CleanupScannedBytes,
                ["referenceHits"] = new JArray(removal.CleanupReferenceHits),
                ["beganRefused"] = removalBeganRefused, ["combatRefused"] = removalCombatRefused, ["began"] = removalBegan,
                ["partyCombat"] = Game.Instance?.Player?.IsInCombat,
                ["factsMounted"] = removalFactsMounted, ["factsDisabled"] = removalFactsDisabled,
                ["factsReEnabled"] = removalFactsReEnabled,
                ["disabled"] = removalDisabled, ["reEnabled"] = removalReEnabled, ["remounted"] = removalRemounted,
                ["snapshots"] = persistence.SnapshotCount, ["failedSaves"] = persistence.FailedSaveCount,
                ["completedSaves"] = persistence.CompletedSaveCount, ["commits"] = NativeMountedArchiveCommit.CommitCount,
                ["saveSuspended"] = persistence.SaveSuspended, ["activeScope"] = persistence.HasActiveSaveScope,
                ["enabled"] = persistence.Enabled, ["nativeCastRequests"] = controls.NativeCastRequestCount,
                ["firstPath"] = removalFirstPath, ["firstHash"] = removalFirstHash,
                ["feedback"] = persistence.Feedback };
            if (extra != null) foreach (var property in extra.Properties()) detail[property.Name] = property.Value;
            return detail;
        }
    }
}
