using System;
using System.IO;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Dungeon.Actions;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.GameModes;
using Kingmaker.UI.Group;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // F4 (WORLD-01): the real Beneath the Stolen Lands entrance on the Working fixture's own Endless campaign. A disclosed
    // setup transfer (public LoadArea, AutoSaveMode.None, no write) takes the mounted pair from the floor to the roguelike
    // hub; there the native entrance action (ActionEnterToDungeon) opens the native party selection, its native Go
    // re-initialises the party (the boundary that used to dismount every rider) and dispatches the native AfterEntry
    // LoadArea of the current stage. The run's one declared output is that after-entry Auto, admitted in the hub and
    // committed in the floor; its KMC record is read back in this process. Nothing restores, teleports or saves directly.
    internal sealed partial class RuntimePersistenceScenario
    {
        private bool DungeonEntryCase => request.Scenario == "persistence-p07-save" && RuntimeRequest.IsDungeonEntryCase(request.PersistenceCase);
        // One bound for the entry and its continuation, which share the scenario clock (review MINOR).
        private const double DungeonEntrySeconds = 240;
        private int dungeonStage, dungeonFrames;
        private GroupManager dungeonGroup;
        private JObject dungeonRoute;
        private string dungeonPartyBefore;
        private long dungeonGenerationBefore;

        // UnityEngine.Object equality: a destroyed GroupManager compares equal to null.
        private bool SelectorOpen() => dungeonGroup != null && dungeonGroup.gameObject.activeSelf;

        private void AdvanceDungeonEntry()
        {
            if (clock.Elapsed.TotalSeconds > DungeonEntrySeconds) throw new InvalidOperationException("P07 dungeon entry stage timed out: " + dungeonStage);
            var game = Game.Instance;
            if (LoadingProcess.Instance.IsLoadingInProcess) { areaFrames++; return; }
            switch (dungeonStage)
            {
                case 0:
                {
                    var target = request.PersistenceAreaTarget;
                    areaSourceArea = game.CurrentlyLoadedArea.AssetGuidThreadSafe;
                    Check(persistence.SnapshotCount == 0 && persistence.AreaSuspensionCount == 0 && persistence.AreaResumeCount == 0 &&
                        !persistence.AreaTransitionPending && areaSourceArea == request.Fixture.Working.Area &&
                        areaSourceArea == RuntimeRequest.DungeonEntryFloorArea, "P07-dungeon-entry-idle-on-the-floor");
                    var hub = ResourcesLibrary.TryGetBlueprint<BlueprintAreaEnterPoint>(target.EnterPoint);
                    Check(hub != null && hub.Area != null && hub.Area.AssetGuidThreadSafe == RuntimeRequest.DungeonEntryHubArea,
                        "P07-dungeon-entry-exact-hub-enter-point");
                    NativePersistenceIsolation.EnableNativeSlotRotation();
                    Check(Kingmaker.UI.SettingsUI.SettingsRoot.Instance.AutosaveEnabled.CurrentValue && game.SaveManager.IsSaveAllowed() &&
                        !game.SaveManager.Any(s => s.Type == SaveInfo.SaveType.Auto && s.IsActuallySaved),
                        "P07-dungeon-entry-native-autosave-admitted-without-existing-slot");
                    CaptureAreaBaseline();
                    dungeonRoute = new JObject { ["setupTransfer"] = new JObject { ["enterPoint"] = target.EnterPoint, ["area"] = target.Area,
                        ["autoSaveMode"] = "None", ["disclosure"] = "controlled setup transfer to the hub (public LoadArea, no write)" } };
                    Write("dungeon-entry-setup-transfer-requested", AreaDetail());
                    game.LoadArea(hub, AutoSaveMode.None);
                    dungeonStage = 1; return;
                }
                case 1:
                {
                    if (persistence.AreaResumeCount < 1) return;
                    rider = relationship.Rider; mount = relationship.Mount;
                    var hubArrival = AreaDetail(); Write("dungeon-entry-hub-arrival", hubArrival);
                    Check(game.CurrentlyLoadedArea.AssetGuidThreadSafe == RuntimeRequest.DungeonEntryHubArea && persistence.SnapshotCount == 0 &&
                        persistence.AreaSuspensionCount == 1 && persistence.AreaResumeCount == 1 && relationship.State == RelationshipState.Mounted &&
                        rider?.UniqueId == areaRiderDebt.Id && mount?.UniqueId == areaMountDebt.Id && game.CurrentMode == GameModeType.Default,
                        "P07-dungeon-entry-same-pair-mounted-in-the-hub-without-a-write");
                    dungeonGroup = game.UI.Canvas.GroupManager;
                    Check(dungeonGroup != null && !dungeonGroup.gameObject.activeSelf, "P07-dungeon-entry-no-borrowed-party-selector");
                    dungeonGenerationBefore = relationship.MountedPairGeneration;
                    dungeonPartyBefore = string.Join("|", game.Player.PartyCharacters.Select(u => u.Value?.UniqueId).OrderBy(x => x, StringComparer.Ordinal).ToArray());
                    // The native entrance action itself (what the hub's entrance runs); it sets the first stage and opens the selector.
                    new ActionEnterToDungeon().RunAction();
                    var enter = game.Player.DungeonState.GetCurrentStageEnterPoint();
                    dungeonRoute["entrance"] = new JObject { ["action"] = "native ActionEnterToDungeon.RunAction",
                        ["resolvedEnterPoint"] = enter?.AssetGuidThreadSafe, ["resolvedArea"] = enter?.Area?.AssetGuidThreadSafe,
                        ["stage"] = game.Player.DungeonState.Stage, ["selectorOpened"] = dungeonGroup.gameObject.activeSelf };
                    Check(enter?.Area?.AssetGuidThreadSafe == RuntimeRequest.DungeonEntryFloorArea && ReferenceEquals(game.UI.Canvas.GroupManager, dungeonGroup) &&
                        dungeonGroup.gameObject.activeSelf, "P07-dungeon-entry-native-entrance-resolves-the-declared-floor-and-opens-the-selector");
                    dungeonFrames = 0; dungeonStage = 2; return;
                }
                case 2:
                    Check(dungeonGroup != null, "P07-dungeon-entry-party-selector-alive-before-go");
                    if (!dungeonGroup.gameObject.activeInHierarchy || ++dungeonFrames < 10) return;
                    Write("dungeon-entry-party-selection-go", new JObject { ["partyBefore"] = dungeonPartyBefore, ["generationBefore"] = dungeonGenerationBefore,
                        ["relationship"] = relationship.State.ToString() });
                    // The native Go button: SetGroup (Player.ReInitPartyCharacters) and the entrance's AfterEntry LoadArea.
                    dungeonGroup.Go();
                    // Go is synchronous: it closes the selector before dispatching the native load, or returns with it open.
                    // r06: the dispatched AfterEntry load destroys the hub's UI in the same call, so the selector is read
                    // through Unity's lifetime check (a destroyed selector is a closed one).
                    Check(!SelectorOpen(), "P07-dungeon-entry-native-go-accepted-the-party");
                    dungeonStage = 3; return;
                case 3:
                {
                    if (SelectorOpen() || persistence.AreaResumeCount < 2 || NativePersistenceIsolation.HasPendingWrites || NativeAutosaveArchive() == null) return;
                    rider = relationship.Rider; mount = relationship.Mount;
                    var arrival = AreaDetail(); arrival["route"] = dungeonRoute; Write("dungeon-entry-floor-arrival", arrival);
                    var partyAfter = string.Join("|", game.Player.PartyCharacters.Select(u => u.Value?.UniqueId).OrderBy(x => x, StringComparer.Ordinal).ToArray());
                    Check(ReferenceEquals(areaWorld, game.Player) && game.CurrentlyLoadedArea.AssetGuidThreadSafe == RuntimeRequest.DungeonEntryFloorArea &&
                        game.CurrentMode == GameModeType.Default && partyAfter == dungeonPartyBefore, "P07-dungeon-entry-native-arrival-same-world-and-party");
                    Check(relationship.State == RelationshipState.Mounted && rider != null && mount != null && rider.UniqueId == areaRiderDebt.Id &&
                        mount.UniqueId == areaMountDebt.Id && relationship.Runtime.ValidateMountedInvariants() == null,
                        "P07-dungeon-entry-same-pair-mounted-after-party-selection-and-load");
                    Check(relationship.MountedPairGeneration == dungeonGenerationBefore, "P07-dungeon-entry-party-re-initialisation-kept-the-pair-generation");
                    Check(game.State.Units.Count(u => u.UniqueId == rider.UniqueId) == 1 && game.State.Units.Count(u => u.UniqueId == mount.UniqueId) == 1 &&
                        IsBoundNativeView(rider) && IsBoundNativeView(mount) && relationship.IsExactCapturedView(rider) && relationship.IsExactCapturedView(mount),
                        "P07-dungeon-entry-bound-native-views-no-duplicates");
                    Check(persistence.AreaSuspensionCount == 2 && persistence.AreaResumeCount == 2 && !persistence.AreaTransitionPending &&
                        persistence.SemanticRestoreCount == 0 && persistence.PresentationRestoreCount == 0 && controls.NativeCastRequestCount == 0,
                        "P07-dungeon-entry-two-transfers-no-load-replay-or-mount-cast");
                    var archive = NativeAutosaveArchive();
                    Check(archive != null && archive.FileName == "Auto_1.zks" && persistence.SnapshotCount == 1 && persistence.FailedSaveCount == 0,
                        "P07-dungeon-entry-exactly-one-native-after-entry-autosave");
                    areaAutosaveHash = Hash(archive.FolderName);
                    // r07: the product writes the current schema-3 archive member (pairs), which the legacy single-pair reader
                    // does not present as Current; read the member the product actually wrote (as the UPG-01 resave check does).
                    var read = NativeMountedSaveStorage.ReadArchive(archive.Saver);
                    var pair = NativeMountedSaveFormat.CanRestore(read) && read.Data.Pairs != null && read.Data.Pairs.Length == 1 ? read.Data.Pairs[0] : null;
                    Check(pair != null && read.Data.SchemaVersion == MountedArchiveData.Schema && pair.Rider?.Id == areaRiderDebt.Id &&
                        pair.Mount?.Id == areaMountDebt.Id && pair.Generation == dungeonGenerationBefore &&
                        read.Data.AreaId == RuntimeRequest.DungeonEntryFloorArea && read.Data.CampaignId == areaWorld.GameId,
                        "P07-dungeon-entry-autosave-carries-the-exact-mounted-pair-in-the-floor");
                    var barrier = areaAutosaveBarrier;
                    Check(barrier != null && (string)barrier["relationship"] == "Mounted" && (string)barrier["area"] == RuntimeRequest.DungeonEntryFloorArea &&
                        (int)barrier["snapshots"] == 1 && (int)barrier["suspensions"] == 2 && (int)barrier["resumes"] == 2 &&
                        (string)barrier["riderId"] == areaRiderDebt.Id && (string)barrier["mountId"] == areaMountDebt.Id,
                        "P07-dungeon-entry-autosave-after-native-restoration");
                    Write("native-write-complete", new JObject { ["ordinal"] = 1, ["path"] = archive.FolderName, ["sha256"] = areaAutosaveHash,
                        ["length"] = new FileInfo(archive.FolderName).Length, ["nativeType"] = archive.Type.ToString(), ["mode"] = "AfterEntry",
                        ["operation"] = archive.OperationState.ToString(), ["nativeCallback"] = false,
                        ["admissionArea"] = RuntimeRequest.DungeonEntryHubArea, ["area"] = read.Data.AreaId, ["barrier"] = barrier,
                        ["partySelection"] = new JObject { ["partyBefore"] = dungeonPartyBefore, ["partyAfter"] = partyAfter,
                            ["generationBefore"] = dungeonGenerationBefore, ["generationAfter"] = relationship.MountedPairGeneration },
                        // The pair view the readers check, taken field by field from the schema-3 member, and the member itself.
                        ["snapshot"] = new JObject { ["Mounted"] = true, ["SchemaVersion"] = read.Data.SchemaVersion, ["AreaId"] = read.Data.AreaId,
                            ["CampaignId"] = read.Data.CampaignId, ["Generation"] = pair.Generation,
                            ["Rider"] = new JObject { ["Id"] = pair.Rider.Id }, ["Mount"] = new JObject { ["Id"] = pair.Mount.Id } },
                        ["archiveMember"] = JObject.FromObject(read.Data, MountedSaveCodec.CreateSerializer()) });
                    // The ordinary continuation (movement and an attack on the floor) makes no write.
                    areaContinuation = true; stage = 2; return;
                }
            }
        }
    }
}
