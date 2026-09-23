using System;
using System.IO;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.Utility;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class RuntimePersistenceScenario
    {
        // Campaign B is a genuine second native game, started through the
        // engine's own Game.LoadNewGame from an authored preset, under isolated
        // save routing established before it starts. Its only role: prove that
        // KMC neither leaks A's mounted state into an unrelated campaign nor
        // loses A's state on the way back. A: mounted, real expenditure, save.
        // B: new game, engine-minted identity frozen on its first write, clean
        // save. A again: exact pair, bindings, positions and debt restored, and
        // every archive byte-identical to when it was written.
        private bool CampaignBCase => request.Scenario == "persistence-p07-save" && request.PersistenceCase == "campaign-b";
        private int campaignStage;
        private int campaignFrames;
        private string campaignRiderId;
        private string campaignMountId;
        private string aFirstPath;
        private string aFirstHash;
        private string aSecondPath;
        private string aSecondHash;
        private Vector3 campaignOrigin;
        private Vector3 campaignRiderAtSave;
        private Vector3 campaignMountAtSave;
        private float campaignMoved;
        private SavedNativeActor campaignRiderDebt;
        private SavedNativeActor campaignMountDebt;
        private long campaignSaveTicks;
        private SavedMountedSlot[] campaignBindings;
        private int campaignSemanticsAtDeparture;
        private int campaignPresentationAtDeparture;
        private int campaignDisposalsAtDeparture;
        private Player campaignWorldA;
        private NativeCampaignBootstrapPreset campaignPreset;
        private bool campaignWindowOpened;
        private string campaignBGameId;
        private string campaignBGameName;
        private string campaignBArea;
        private string bAutoPath;
        private string bAutoHash;
        private string bManualPath;
        private string bManualHash;
        private bool campaignManualAllowed;
        private int campaignPartyAtB;
        private string campaignModeAtB;
        private bool campaignSlotRotationForced;

        private void AdvanceCampaignB()
        {
            if (clock.Elapsed.TotalSeconds > 400)
                throw new InvalidOperationException("P07 campaign B timed out at " + campaignStage +
                    " frames=" + campaignFrames + ": " + persistence.Feedback);
            var game = Game.Instance;
            if (game == null) return;
            stage = 900 + campaignStage;
            if (campaignStage == 0)
            {
                // A's opening write from the shared stage 0 has completed.
                if (LoadingProcess.Instance.IsLoadingInProcess || NativePersistenceIsolation.HasPendingWrites ||
                    game.CurrentlyLoadedArea == null || game.CurrentMode != Kingmaker.GameModes.GameModeType.Default || !callback) return;
                if (relationship.Rider?.Commands.Move != null || relationship.Mount?.Commands.Move != null) return;
                Check(settings.EnablePairedActivation && !settings.EnableUnifiedMountedTurn &&
                    !settings.EnablePairedCommandScheduler && !settings.EnableDiagnosticOverlay, "P07-required-policy");
                Check(relationship.State == RelationshipState.Mounted && persistence.SnapshotCount == 1,
                    "P07-campaign-B-opens-with-one-real-mounted-save");
                Check(NativePersistenceIsolation.DeclaresBootstrapCampaign && !NativePersistenceIsolation.BootstrapWindowOpen &&
                    NativePersistenceIsolation.BootstrapGameId == null && NativePersistenceIsolation.BootstrapFreezeCount == 0,
                    "P07-bootstrap-campaign-is-declared-and-not-yet-minted");
                rider = relationship.Rider; mount = relationship.Mount;
                campaignRiderId = rider.UniqueId; campaignMountId = mount.UniqueId;
                var first = game.SaveManager.Single(s => s.Name == "KMC_P01");
                Check(first.FileName == "Manual_300_KMC_P01.zks" && first.HasFileOnDisk &&
                    first.OperationState == SaveInfo.StateType.None && first.GameId == request.Fixture.Working.GameId,
                    "P07-first-A-archive-is-the-exact-declared-leaf");
                var read = NativeMountedSaveStorage.Read(first.Saver);
                Check(read.Kind == MountedSaveReadKind.Current && read.Data.Mounted &&
                    read.Data.Rider.Id == campaignRiderId && read.Data.Mount.Id == campaignMountId &&
                    read.Data.CampaignId == request.Fixture.Working.GameId, "P07-first-A-archive-carries-the-pair");
                aFirstPath = first.FolderName; aFirstHash = Hash(aFirstPath);
                Write("native-write-complete", new JObject {
                    ["ordinal"] = 1, ["path"] = aFirstPath, ["sha256"] = aFirstHash,
                    ["length"] = new FileInfo(aFirstPath).Length, ["nativeType"] = first.Type.ToString(),
                    ["nativeCallback"] = callback, ["operation"] = first.OperationState.ToString(),
                    ["snapshot"] = JObject.FromObject(read.Data, MountedSaveCodec.CreateSerializer()) });
                // Real expenditure before the second save: an ordinary player
                // ground click that moves the mounted pair.
                SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                game.DefaultPointerController.ClearPointerMode();
                Check(game.DefaultPointerController.Mode == Kingmaker.Controllers.Clicks.PointerMode.Default &&
                    game.SelectedAbilityHandler.Ability == null, "P07-campaign-B-pointer-cancel-before-ground-input");
                campaignOrigin = mount.Position;
                var destination = FindDestination(3f);
                using (var input = new NativeOrdinaryAttackInput(destination))
                    Check(input.Click(), "P07-campaign-B-expenditure-ground-input");
                move = mount.Commands.Move as UnitMoveTo;
                Check(move != null && move.Executor == mount && move.CreatedByPlayer, "P07-campaign-B-expenditure-owner");
                campaignFrames = 0; campaignStage = 1;
                return;
            }
            if (campaignStage == 1)
            {
                if (!move.IsFinished) { if (++campaignFrames > 1800) throw new InvalidOperationException("P07 campaign B expenditure never finished."); return; }
                campaignMoved = GeometryUtils.MechanicsDistance(campaignOrigin, mount.Position);
                Check(move.Result == Kingmaker.UnitLogic.Commands.Base.UnitCommand.ResultType.Success && campaignMoved > 1f,
                    "P07-campaign-B-real-expenditure-moved-the-pair");
                Check(relationship.State == RelationshipState.Mounted && relationship.Rider == rider && relationship.Mount == mount,
                    "P07-campaign-B-expenditure-retains-the-pair");
                campaignRiderAtSave = rider.Position; campaignMountAtSave = mount.Position;
                campaignRiderDebt = MountedPersistenceService.CaptureActor(rider);
                campaignMountDebt = MountedPersistenceService.CaptureActor(mount);
                campaignSaveTicks = game.TimeController.GameTime.Ticks;
                campaignBindings = controls.CapturePersistentSlots();
                Check(campaignBindings.Length > 0, "P07-campaign-B-owned-bindings-exist-before-departure");
                // A NEW archive for the post-expenditure state: CreateNewSave
                // allocates the next declared leaf rather than replacing 300.
                callback = false;
                var descriptor = game.SaveManager.CreateNewSave("KMC_P01");
                Check(descriptor.Type == SaveInfo.SaveType.Manual && game.SaveManager.IsSaveAllowed(),
                    "P07-campaign-B-second-A-save-admission");
                game.SaveGame(descriptor, () => callback = true);
                campaignFrames = 0; campaignStage = 2;
                return;
            }
            if (campaignStage == 2)
            {
                if (!callback || NativePersistenceIsolation.HasPendingWrites || LoadingProcess.Instance.IsLoadingInProcess)
                { if (++campaignFrames > 2400) throw new InvalidOperationException("P07 campaign B second A save never completed: " + persistence.Feedback); return; }
                var second = game.SaveManager.Where(s => s.Name == "KMC_P01" && s.HasFileOnDisk && s.FileName == "Manual_301_KMC_P01.zks").ToArray();
                Check(second.Length == 1 && second[0].OperationState == SaveInfo.StateType.None,
                    "P07-second-A-archive-is-the-exact-declared-leaf");
                aSecondPath = second[0].FolderName; aSecondHash = Hash(aSecondPath);
                var read = NativeMountedSaveStorage.Read(second[0].Saver);
                Check(read.Kind == MountedSaveReadKind.Current && read.Data.Mounted &&
                    read.Data.Rider.Id == campaignRiderId && read.Data.Mount.Id == campaignMountId &&
                    read.Data.CampaignId == request.Fixture.Working.GameId && persistence.SnapshotCount == 2 &&
                    aSecondHash != aFirstHash && Hash(aFirstPath) == aFirstHash,
                    "P07-second-A-archive-records-the-expended-pair-without-touching-the-first");
                Write("campaign-b-expenditure", CampaignDetail(new JObject {
                    ["moved"] = campaignMoved, ["origin"] = new JArray(campaignOrigin.x, campaignOrigin.y, campaignOrigin.z),
                    ["riderAtSave"] = new JArray(campaignRiderAtSave.x, campaignRiderAtSave.y, campaignRiderAtSave.z),
                    ["mountAtSave"] = new JArray(campaignMountAtSave.x, campaignMountAtSave.y, campaignMountAtSave.z),
                    ["bindings"] = campaignBindings.Length,
                    ["secondArchive"] = ArchiveDetail(aSecondPath, aSecondHash, second[0], read) }));
                // Leave A exactly as the Esc menu does. No worker is running, so
                // this is not deferred: the engine disposes A's world.
                campaignWorldA = game.Player;
                campaignSemanticsAtDeparture = persistence.SemanticRestoreCount;
                campaignPresentationAtDeparture = persistence.PresentationRestoreCount;
                campaignDisposalsAtDeparture = persistence.NativeWorldDisposalCount;
                Check(!persistence.ActiveSaveWorkerRunning && !persistence.SaveDraining, "P07-campaign-B-departs-with-no-owned-worker");
                game.ResetToMainMenu(null, null);
                Check(persistence.ResetToMainMenuDeferredCount == 0 && !persistence.ResetToMainMenuPending,
                    "P07-campaign-B-departure-is-not-deferred-without-a-worker");
                campaignFrames = 0; campaignStage = 3;
                return;
            }
            if (campaignStage == 3)
            {
                campaignFrames++;
                if (LoadingProcess.Instance.IsLoadingInProcess || game.CurrentlyLoadedArea != null ||
                    NativePersistenceIsolation.HasPendingWrites || campaignFrames < 10)
                { if (campaignFrames > 3600) throw new InvalidOperationException("P07 campaign B never reached the main menu."); return; }
                // A is gone: no pair, no leases, no pending KMC transfer state.
                Check(relationship.State != RelationshipState.Mounted && !persistence.SaveSuspended &&
                    !persistence.HasActiveSaveScope && !persistence.SaveDraining,
                    "P07-leaving-A-releases-the-pair-and-every-lease");
                Check(Hash(aFirstPath) == aFirstHash && Hash(aSecondPath) == aSecondHash,
                    "P07-A-archives-are-untouched-by-leaving-A");
                rider = null; mount = null; move = null;
                Write("campaign-b-departed", CampaignDetail(null));
                // The engine's own authored start, resolved the same way the
                // isolated authority declared B's leaves. The autosave setting
                // is forced on for this process, as the slot cases do, so the
                // engine's own first write is deterministic.
                campaignPreset = NativeCampaignBootstrap.Resolve();
                NativePersistenceIsolation.EnableNativeSlotRotation();
                campaignSlotRotationForced = true;
                Check(Game.NewGameUnit == null, "P07-no-character-generation-product-precedes-the-native-new-game");
                NativePersistenceIsolation.OpenBootstrapWindow();
                campaignWindowOpened = NativePersistenceIsolation.BootstrapWindowOpen;
                Check(campaignWindowOpened && NativePersistenceIsolation.BootstrapGameId == null,
                    "P07-bootstrap-window-opens-immediately-before-the-native-new-game");
                Write("campaign-b-started", CampaignDetail(new JObject {
                    ["presetSource"] = campaignPreset.Source, ["dlcEnabled"] = campaignPreset.DlcEnabled,
                    ["presetArea"] = campaignPreset.Area, ["enterPointArea"] = campaignPreset.EnterPointArea,
                    ["makeAutosave"] = campaignPreset.MakeAutosave, ["charGen"] = campaignPreset.CharGen,
                    ["presetPlayerCharacter"] = campaignPreset.HasPlayerCharacter,
                    ["presetCompanions"] = campaignPreset.CompanionCount,
                    ["autosaveEnabled"] = Kingmaker.UI.SettingsUI.SettingsRoot.Instance.AutosaveEnabled.CurrentValue,
                    ["windowOpened"] = campaignWindowOpened,
                    ["frozenBefore"] = NativePersistenceIsolation.BootstrapGameId != null }));
                game.LoadNewGame(campaignPreset.Preset, null);
                campaignFrames = 0; campaignStage = 4;
                return;
            }
            if (campaignStage == 4)
            {
                campaignFrames++;
                if (LoadingProcess.Instance.IsLoadingInProcess || game.CurrentlyLoadedArea == null ||
                    NativePersistenceIsolation.HasPendingWrites || game.Player == null ||
                    game.Player.MainCharacter.Value == null || campaignFrames < 30)
                { if (campaignFrames > 7200) throw new InvalidOperationException("P07 campaign B never loaded: " + persistence.Feedback); return; }
                campaignModeAtB = game.CurrentMode.ToString();
                campaignPartyAtB = game.Player.Party.Count;
                campaignBGameId = game.Player.GameId;
                campaignBGameName = game.Player.MainCharacter.Value.CharacterName;
                campaignBArea = game.CurrentlyLoadedArea.AssetGuidThreadSafe;
                Guid minted;
                Check(!ReferenceEquals(game.Player, campaignWorldA) && Guid.TryParse(campaignBGameId, out minted) &&
                    minted != Guid.Empty && campaignBGameId != request.Fixture.Working.GameId,
                    "P07-the-engine-minted-a-fresh-campaign-identity");
                Check(campaignBArea == campaignPreset.Area, "P07-campaign-B-opened-in-its-authored-preset-area");
                // The engine's own first write already froze the identity.
                Check(NativePersistenceIsolation.BootstrapFreezeCount == 1 && !NativePersistenceIsolation.BootstrapWindowOpen &&
                    NativePersistenceIsolation.BootstrapGameId == campaignBGameId &&
                    NativePersistenceIsolation.BootstrapGameName == campaignBGameName,
                    "P07-first-native-write-froze-the-exact-minted-identity");
                var auto = game.SaveManager.Where(s => s.Type == SaveInfo.SaveType.Auto && s.HasFileOnDisk).ToArray();
                Check(auto.Length == 1 && auto[0].FileName == NativeCampaignBootstrap.AutosaveLeaf &&
                    auto[0].GameId == campaignBGameId && auto[0].OperationState == SaveInfo.StateType.None,
                    "P07-campaign-B-autosave-is-the-exact-declared-leaf-with-the-minted-identity");
                bAutoPath = auto[0].FolderName; bAutoHash = Hash(bAutoPath);
                VerifyCleanCampaignArchive(auto[0], "autosave");
                // KMC holds nothing of A in B: no pair, no bindings, no restoration
                // and no Mount cast. The last selected archive's metadata is only
                // recorded; it cannot bind to B's world and restores nothing.
                Check(relationship.State != RelationshipState.Mounted &&
                    persistence.SemanticRestoreCount == campaignSemanticsAtDeparture &&
                    persistence.PresentationRestoreCount == campaignPresentationAtDeparture &&
                    controls.CapturePersistentSlots().Length == 0 && controls.NativeCastRequestCount == 0,
                    "P07-campaign-B-carries-no-mounted-state-bindings-or-restoration");
                Check(game.Player.Party.All(u => u.UniqueId != campaignRiderId && u.UniqueId != campaignMountId),
                    "P07-campaign-B-party-shares-no-actor-with-A");
                Check(Hash(aFirstPath) == aFirstHash && Hash(aSecondPath) == aSecondHash,
                    "P07-A-archives-are-untouched-by-starting-B");
                // A user-visible manual save in B, when the authored start allows
                // one at all; the autosave above is B's guaranteed archive.
                campaignManualAllowed = game.SaveManager.IsSaveAllowed() && game.CurrentMode == Kingmaker.GameModes.GameModeType.Default;
                if (campaignManualAllowed)
                {
                    callback = false;
                    var descriptor = game.SaveManager.CreateNewSave(NativeCampaignBootstrap.ManualName);
                    game.SaveGame(descriptor, () => callback = true);
                    campaignFrames = 0; campaignStage = 5;
                    return;
                }
                campaignFrames = 0; campaignStage = 6;
                return;
            }
            if (campaignStage == 5)
            {
                if (!callback || NativePersistenceIsolation.HasPendingWrites || LoadingProcess.Instance.IsLoadingInProcess)
                { if (++campaignFrames > 2400) throw new InvalidOperationException("P07 campaign B manual save never completed: " + persistence.Feedback); return; }
                var manual = game.SaveManager.Where(s => s.Name == NativeCampaignBootstrap.ManualName && s.HasFileOnDisk).ToArray();
                Check(manual.Length == 1 && manual[0].FileName == NativeCampaignBootstrap.ManualLeaf &&
                    manual[0].GameId == campaignBGameId && manual[0].GameName == campaignBGameName &&
                    manual[0].OperationState == SaveInfo.StateType.None,
                    "P07-campaign-B-manual-save-is-the-exact-declared-leaf-with-the-frozen-identity");
                bManualPath = manual[0].FolderName; bManualHash = Hash(bManualPath);
                VerifyCleanCampaignArchive(manual[0], "manual");
                Check(NativePersistenceIsolation.BootstrapFreezeCount == 1 && Hash(bAutoPath) == bAutoHash,
                    "P07-a-later-B-write-freezes-nothing-new-and-keeps-the-autosave");
                campaignFrames = 0; campaignStage = 6;
                return;
            }
            if (campaignStage == 6)
            {
                Check(Hash(aFirstPath) == aFirstHash && Hash(aSecondPath) == aSecondHash,
                    "P07-A-archives-are-untouched-by-B-writes");
                Write("campaign-b-frozen", CampaignDetail(new JObject {
                    ["gameId"] = campaignBGameId, ["gameName"] = campaignBGameName, ["area"] = campaignBArea,
                    ["loadedArea"] = game.CurrentlyLoadedArea?.AssetGuidThreadSafe, ["presetArea"] = campaignPreset.Area,
                    ["freezeCount"] = NativePersistenceIsolation.BootstrapFreezeCount,
                    ["windowOpen"] = NativePersistenceIsolation.BootstrapWindowOpen,
                    ["modeAtB"] = campaignModeAtB, ["partyAtB"] = campaignPartyAtB,
                    ["manualAllowed"] = campaignManualAllowed, ["manualSaved"] = bManualPath != null,
                    ["loadedDataMounted"] = persistence.LoadedData?.Mounted,
                    ["autosave"] = BArchiveDetail(bAutoPath, bAutoHash),
                    ["manual"] = bManualPath == null ? null : BArchiveDetail(bManualPath, bManualHash),
                    ["bindings"] = controls.CapturePersistentSlots().Length,
                    ["aFirstSha256"] = Hash(aFirstPath), ["aSecondSha256"] = Hash(aSecondPath) }));
                // Back to A through the ordinary main-menu load path, from
                // inside B's live world: exactly what a player does.
                var target = game.SaveManager.Single(s => s.FileName == "Manual_301_KMC_P01.zks");
                Check(target.GameId == request.Fixture.Working.GameId && target.HasFileOnDisk, "P07-A-return-target-is-the-expended-archive");
                callback = false; campaignFrames = 0;
                game.SaveManager.AddCallbackAfterLoad(() => callback = true);
                game.LoadGameFromMainMenu(target);
                campaignStage = 7;
                return;
            }
            if (campaignStage == 7)
            {
                campaignFrames++;
                if (LoadingProcess.Instance.IsLoadingInProcess || NativePersistenceIsolation.HasPendingWrites ||
                    game.CurrentlyLoadedArea == null || game.CurrentMode != Kingmaker.GameModes.GameModeType.Default ||
                    !callback || campaignFrames < 10)
                { if (campaignFrames > 7200) throw new InvalidOperationException("P07 return to A never completed: " + persistence.Feedback); return; }
                Check(game.Player.GameId == request.Fixture.Working.GameId && !ReferenceEquals(game.Player, campaignWorldA),
                    "P07-A-is-back-under-its-own-identity-in-a-new-native-world");
                Check(relationship.State == RelationshipState.Mounted &&
                    relationship.Rider.UniqueId == campaignRiderId && relationship.Mount.UniqueId == campaignMountId,
                    "P07-A-restores-the-exact-pair-after-B");
                rider = relationship.Rider; mount = relationship.Mount;
                Check(persistence.LoadedData != null && persistence.LoadedData.Mounted &&
                    persistence.LoadedData.CampaignId == request.Fixture.Working.GameId &&
                    persistence.SemanticRestoreCount == campaignSemanticsAtDeparture + 2 &&
                    persistence.PresentationRestoreCount == campaignPresentationAtDeparture + 1 &&
                    controls.NativeCastRequestCount == 0,
                    "P07-A-restores-once-from-its-own-archive-with-no-Mount-cast");
                var bindings = controls.CapturePersistentSlots();
                Check(bindings.Length == campaignBindings.Length && campaignBindings.All(
                    s => bindings.Any(x => x.ActorId == s.ActorId && x.Index == s.Index && x.Kind == s.Kind)),
                    "P07-A-restores-the-owned-bindings-saved-before-B");
                var riderDelta = GeometryUtils.MechanicsDistance(rider.Position, campaignRiderAtSave);
                var mountDelta = GeometryUtils.MechanicsDistance(mount.Position, campaignMountAtSave);
                Check(mountDelta < 0.5f && riderDelta < 1.5f, "P07-A-restores-the-expended-position");
                var elapsed = Math.Max(0, (game.TimeController.GameTime.Ticks - campaignSaveTicks) / (double)TimeSpan.TicksPerSecond);
                Check(LegitimateContinuation(campaignRiderDebt, MountedPersistenceService.CaptureActor(rider), elapsed) &&
                    LegitimateContinuation(campaignMountDebt, MountedPersistenceService.CaptureActor(mount), elapsed),
                    "P07-A-restores-legitimate-native-debt-after-B");
                Check(Hash(aFirstPath) == aFirstHash && Hash(aSecondPath) == aSecondHash && Hash(bAutoPath) == bAutoHash &&
                    (bManualPath == null || Hash(bManualPath) == bManualHash),
                    "P07-every-archive-is-byte-identical-after-the-round-trip");
                Check(persistence.NativeWorldDisposalCount == campaignDisposalsAtDeparture + 1,
                    "P07-returning-to-A-disposed-exactly-Bs-world");
                beforeControls = controls.CaptureSnapshot();
                Check(beforeControls.ExactFactCount > 0 && beforeControls.DuplicateFactCount == 0 && !beforeControls.SerializationSuspended,
                    "P07-A-controls-present-once-after-B");
                Write("campaign-b-returned", CampaignDetail(new JObject {
                    ["riderDelta"] = riderDelta, ["mountDelta"] = mountDelta, ["elapsedSeconds"] = elapsed,
                    ["bindingsRestored"] = bindings.Length, ["bindingsSaved"] = campaignBindings.Length,
                    ["aFirstSha256"] = Hash(aFirstPath), ["aSecondSha256"] = Hash(aSecondPath),
                    ["bAutoSha256"] = Hash(bAutoPath), ["bManualSha256"] = bManualPath == null ? null : Hash(bManualPath),
                    ["disposals"] = persistence.NativeWorldDisposalCount,
                    ["bootstrapGameId"] = NativePersistenceIsolation.BootstrapGameId }));
                if (campaignSlotRotationForced) { NativePersistenceIsolation.DisableNativeSlotRotation(); campaignSlotRotationForced = false; }
                // The ordinary continuation now proves A is usable after B.
                recoveryContinuation = true; stage = 2;
            }
        }

        // A campaign-B archive must be a real native save of B alone: header
        // identity is B's, and whatever KMC wrote records no pair, no bindings
        // and B's own campaign/area, never A's.
        private void VerifyCleanCampaignArchive(SaveInfo save, string role)
        {
            Check(save.GameId == campaignBGameId && save.GameName == campaignBGameName &&
                save.Area?.AssetGuidThreadSafe == campaignBArea, "P07-campaign-B-" + role + "-header-is-Bs-own");
            var read = NativeMountedSaveStorage.Read(save.Saver);
            Check(read.Kind == MountedSaveReadKind.Missing || (read.Kind == MountedSaveReadKind.Current &&
                !read.Data.Mounted && read.Data.Rider == null && read.Data.Mount == null &&
                (read.Data.Slots == null || read.Data.Slots.Length == 0) &&
                read.Data.CampaignId == campaignBGameId && read.Data.AreaId == campaignBArea),
                "P07-campaign-B-" + role + "-records-no-pair-and-only-Bs-own-identity");
        }

        private JObject BArchiveDetail(string path, string hash)
        {
            var save = Game.Instance.SaveManager.Single(s => s.FolderName == path);
            var read = NativeMountedSaveStorage.Read(save.Saver);
            return new JObject {
                ["path"] = path, ["leaf"] = save.FileName, ["sha256"] = hash, ["length"] = new FileInfo(path).Length,
                ["nativeType"] = save.Type.ToString(), ["internalName"] = save.Name,
                ["gameId"] = save.GameId, ["gameName"] = save.GameName, ["area"] = save.Area?.AssetGuidThreadSafe,
                ["operation"] = save.OperationState.ToString(), ["kmcMember"] = read.Kind.ToString(),
                ["snapshot"] = read.Data == null ? null : JObject.FromObject(read.Data, MountedSaveCodec.CreateSerializer()) };
        }

        private static JObject ArchiveDetail(string path, string hash, SaveInfo save, MountedSaveReadResult read) => new JObject {
            ["path"] = path, ["leaf"] = save.FileName, ["sha256"] = hash, ["length"] = new FileInfo(path).Length,
            ["nativeType"] = save.Type.ToString(), ["gameId"] = save.GameId,
            ["snapshot"] = JObject.FromObject(read.Data, MountedSaveCodec.CreateSerializer()) };

        private JObject CampaignDetail(JObject extra)
        {
            var world = Game.Instance;
            var detail = new JObject {
                ["case"] = request.PersistenceCase, ["stage"] = campaignStage, ["frames"] = campaignFrames,
                ["riderId"] = campaignRiderId, ["mountId"] = campaignMountId,
                ["gameId"] = world?.Player?.GameId, ["loadedArea"] = world?.CurrentlyLoadedArea?.AssetGuidThreadSafe,
                ["worldIsA"] = world?.Player != null && ReferenceEquals(world.Player, campaignWorldA),
                ["snapshots"] = persistence.SnapshotCount, ["failedSaves"] = persistence.FailedSaveCount,
                ["semantics"] = persistence.SemanticRestoreCount, ["presentation"] = persistence.PresentationRestoreCount,
                ["disposals"] = persistence.NativeWorldDisposalCount, ["rejections"] = persistence.RejectedLoadCount,
                ["saveSuspended"] = persistence.SaveSuspended, ["activeScope"] = persistence.HasActiveSaveScope,
                ["draining"] = persistence.SaveDraining, ["resetDeferrals"] = persistence.ResetToMainMenuDeferredCount,
                ["bootstrapDeclared"] = NativePersistenceIsolation.DeclaresBootstrapCampaign,
                ["bootstrapWindowOpen"] = NativePersistenceIsolation.BootstrapWindowOpen,
                ["bootstrapGameId"] = NativePersistenceIsolation.BootstrapGameId,
                ["bootstrapGameName"] = NativePersistenceIsolation.BootstrapGameName,
                ["bootstrapFreezes"] = NativePersistenceIsolation.BootstrapFreezeCount,
                ["aFirstPath"] = aFirstPath, ["aFirstHash"] = aFirstHash,
                ["aSecondPath"] = aSecondPath, ["aSecondHash"] = aSecondHash,
                ["bAutoPath"] = bAutoPath, ["bAutoHash"] = bAutoHash,
                ["bManualPath"] = bManualPath, ["bManualHash"] = bManualHash,
                ["nativePaused"] = world?.IsPaused, ["nativeMode"] = world?.CurrentMode.ToString(),
                ["feedback"] = persistence.Feedback };
            if (extra != null) foreach (var property in extra.Properties()) detail[property.Name] = property.Value;
            return detail;
        }
    }
}
