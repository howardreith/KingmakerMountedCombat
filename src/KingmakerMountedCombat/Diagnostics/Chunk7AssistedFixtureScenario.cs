using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Root;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.GameModes;
using Kingmaker.UI;
using Kingmaker.UI.LoadingScreen;
using UnityEngine.SceneManagement;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    // Separately selected disposable setup. Assistance never qualifies ordinary
    // acquisition/progression or a mounted action. Native story/build/save calls
    // remain owned by their original controllers and the exact isolated scope.
    internal sealed partial class Chunk7AssistedFixtureScenario : IDisposable
    {
        internal const string EvidenceFileName="chunk7-assisted-fixture-evidence.json";
        internal const string EvidenceKind="chunk7-assisted-fixture-evidence";
        internal const string PendingFileName="chunk7-assisted-fixture-cleanup-pending.json";
        internal const string PendingKind="chunk7-assisted-fixture-cleanup-pending";
        private readonly RuntimeRequest request;
        private readonly RuntimeSaveAuthorization authorization;
        private readonly MountedWorldService world;
        private readonly MountedPersistenceService persistence;
        private readonly NativeMountedControlService controls;
        private readonly HorseCompanionBlueprintService horses;
        private readonly Game game; private readonly Player player; private readonly SaveManager saveManager;
        private readonly Stopwatch clock=Stopwatch.StartNew();
        private readonly JObject evidence=new JObject();
        private readonly List<string> errors=new List<string>();
        private readonly List<RuntimeSubscenarioResult> results=new List<RuntimeSubscenarioResult>();
        private readonly RetainedObjectIdentity identities=new RetainedObjectIdentity(2048);
        private readonly JArray nativeWrites=new JArray(), preservationRows=new JArray(), choices=new JArray();
        private PersistenceSaveAuthorization authority; private int generation, stage, readyFrame=-1, targetLevel;
        private BlueprintRoot blueprintRoot; private BlueprintAreaPreset preset;
        private Chunk7NativeBootstrapProbe probe;
        private object nativeState,nativeAreaState,nativeMainState,nativeDescriptor,nativeView;
        private UnitEntityData main; private string campaignId,campaignName,mainId;
        private SaveInfo initialAuto;
        private Chunk7AssistedFixtureBinding binding;
        private Chunk7AssistedFixtureNativeBoundary nativeBoundary;
        private Chunk7AssistedStoryDriver story;
        private Chunk7AssistedShortfall shortfall;
        private bool preflightReady,dispatched,callbackEntered,callbackReturned,slotOwned,priorSlot,nativeGateOwned;
        private bool cleanup,settled,completed,disposed,pending;private double cleanupStart;
        internal bool PreflightReady=>preflightReady;
        internal bool IsBound=>authority!=null;
        internal bool IsCompleted=>completed;
        internal IReadOnlyList<string> Errors=>errors;
        internal IReadOnlyList<RuntimeSubscenarioResult> Results=>results;
        internal static bool SupportsScenario(string value)=>value==Chunk7AssistedFixtureSelection.Scenario;
        internal Chunk7AssistedFixtureScenario(RuntimeRequest request,RuntimeSaveAuthorization authorization,
            GameMountedRelationshipService relationship,NativeMountedControlService controls,
            MountedPersistenceService persistence,HorseCompanionBlueprintService horses)
        {
            if(request?.AssistedFixture==null||request.Validate().Count!=0)throw new ArgumentException("Exact assisted fixture request required.");
            this.request=request;this.authorization=authorization??throw new ArgumentNullException(nameof(authorization));
            world=relationship?.World??throw new ArgumentNullException(nameof(relationship));
            this.controls=controls??throw new ArgumentNullException(nameof(controls));this.persistence=persistence??throw new ArgumentNullException(nameof(persistence));
            this.horses=horses??throw new ArgumentNullException(nameof(horses));
            game=Game.Instance??throw new InvalidOperationException("Native menu Game unavailable.");player=game.Player;saveManager=game.SaveManager;
            evidence["selection"]=Chunk7EvidenceDto.Request(request.AssistedFixture);evidence["processId"]=Process.GetCurrentProcess().Id;
            evidence["assistanceOnly"]=true;evidence["ordinaryAcquisitionProven"]=false;evidence["ordinaryProgressionProven"]=false;
            evidence["mountedCombatProven"]=false;evidence["smallPoseQualified"]=false;evidence["workingDeserialized"]=false;
            evidence["diagnosticArchiveContentRead"]=false;evidence["nativeWrites"]=nativeWrites;evidence["choices"]=choices;
            evidence["preservation"]=new JObject{["contract"]="assisted-main-shortfall-preservation-v1",
                ["excluded"]=new JArray("mainExperience","partyMoney"),["maximumSnapshots"]=12,["rows"]=preservationRows};
        }
        internal static BlueprintAreaPreset ResolveMainPreset()
        {
            var root = BlueprintRoot.Instance; var value = root?.NewGamePreset;
            Require(value != null && value.Area != null && value.EnterPoint?.Area != null &&
                value.Area.AssetGuidThreadSafe == value.EnterPoint.Area.AssetGuidThreadSafe && value.MakeAutosave &&
                (value.PlayerCharacter ?? root.DefaultPlayerCharacter) != null,
                "The exact authored main-campaign preset, player blueprint or first autosave is unavailable.");
            return value;
        }
        internal void Preflight()
        {
            if (completed || disposed || preflightReady) return;
            try
            {
                if (cleanup) { PollCleanup(); return; }
                evidence["preflight"] = CapturePreflight();
                RequireNativeMenuAdmission();
                Require(horses.State != HorseCompanionBlueprintState.Faulted,
                    "Assisted fixture refused before new game: Horse/Ranger registration faulted. " + horses.Failure);
                Require(clock.Elapsed.TotalSeconds <= 90, "Assisted fixture Horse/blueprint readiness timed out before any new game or write.");
                if (horses.State != HorseCompanionBlueprintState.Registered) return;
                RequireNativeRangerSelection();
                preset = ResolveMainPreset(); blueprintRoot = BlueprintRoot.Instance;
                evidence["preset"] = new JObject { ["source"] = "BlueprintRoot.NewGamePreset", ["guid"] = preset.AssetGuid,
                    ["area"] = preset.Area.AssetGuidThreadSafe, ["enterPoint"] = preset.EnterPoint.AssetGuid,
                    ["enterPointArea"] = preset.EnterPoint.Area.AssetGuidThreadSafe, ["makeAutosave"] = preset.MakeAutosave,
                    ["charGen"] = preset.CharGen, ["playerBlueprint"] = (preset.PlayerCharacter ?? blueprintRoot.DefaultPlayerCharacter).AssetGuid,
                    ["usesPresetPlayer"] = preset.PlayerCharacter != null, ["companionCount"] = preset.Companions?.Count ?? 0 };
                preflightReady = true; Pass("C7-assisted-preflight");
            }
            catch (Exception error) { Fail("C7-assisted-preflight", error.ToString()); BeginCleanup(); PollCleanup(); }
        }
        private JObject CapturePreflight()
        {
            var library = ResourcesLibrary.LibraryObject; BlueprintScriptableObject value = null;
            library?.BlueprintsByAssetId.TryGetValue(HorseCompanionBlueprintService.RangerSelectionGuid, out value);
            var ranger = value as BlueprintFeatureSelection; var features = ranger?.AllFeatures;
            return new JObject { ["horseState"] = horses.State.ToString(), ["horseFailure"] = horses.Failure,
                ["libraryReady"] = library != null && library.BlueprintsByAssetId.Count > 0,
                ["rangerSelectionGuid"] = HorseCompanionBlueprintService.RangerSelectionGuid,
                ["rangerSelectionPresent"] = ranger != null, ["rangerAllFeaturesCount"] = features == null ? -1 : features.Length,
                ["rangerFirst32Guids"] = features == null ? new JArray() : new JArray(features.Take(32).Select(feature => feature?.AssetGuid)),
                ["horseFeatureGuid"] = horses.HorseFeature?.AssetGuid, ["horseUnitGuid"] = horses.HorseUnit?.AssetGuid,
                ["loadedAreaPresent"] = game.CurrentlyLoadedArea != null, ["mainActorPresent"] = player?.MainCharacter.Value != null,
                ["authorizedLoads"] = authorization.AuthorizedLoadCount, ["authorizedWrites"] = authorization.AuthorizedWriteCount,
                ["nativeNewGameDispatched"] = dispatched };
        }
        private void RequireNativeRangerSelection()
        {
            Require(horses.State == HorseCompanionBlueprintState.Registered,
                "Assisted fixture Horse registration is no longer ready.");
            BlueprintScriptableObject value = null;
            ResourcesLibrary.LibraryObject?.BlueprintsByAssetId.TryGetValue(
                HorseCompanionBlueprintService.RangerSelectionGuid, out value);
            HorseCompanionBlueprintService.RequireNativeRangerOptions(
                (value as BlueprintFeatureSelection)?.AllFeatures, horses.HorseFeature);
        }
        private static bool NativeMenuAlreadyEntering(MainMenu menu)
        {
            var field = typeof(MainMenu).GetField("m_EnterGameStarted", BindingFlags.Instance | BindingFlags.NonPublic);
            Require(field != null && field.MetadataToken == 0x04000720 && field.FieldType == typeof(bool), "Native MainMenu EnterGame ownership contract changed.");
            return (bool)field.GetValue(menu);
        }
        private void RequireNativeMenuAdmission()
        {
            RequireOriginalMenu();
            Require(game.IsControllerMouse && SceneManager.GetSceneByName(SceneName.MainMenu).isLoaded &&
                game.UI?.MainMenu != null && LoadingScreen.Instance != null && !Busy() &&
                !game.IsModeActive(GameModeType.Dialog) && (DialogMessageBox.Instance == null || !DialogMessageBox.Instance.IsShown) &&
                !NativeMenuAlreadyEntering(game.UI.MainMenu),
                "Assisted fixture requires an idle exact PC main menu, loading screen, and no queued work or unexpected dialog.");
        }
        private void RequireOriginalMenu()
        {
            Require(ReferenceEquals(Game.Instance, game) && ReferenceEquals(game.Player, player) && ReferenceEquals(game.SaveManager, saveManager) &&
                player != null && player.MainCharacter.Value == null && game.CurrentlyLoadedArea == null && Game.NewGameUnit == null,
                "Assisted fixture requires its exact empty menu world and no generated character product.");
            RequireNoOwners();
        }
        private void Pass(string row) { if (results.All(value => value.Name != row)) results.Add(new RuntimeSubscenarioResult { Name = row, Status = "PASS", AssertionPassCount = 1, Errors = Array.Empty<string>() }); }
        private void Fail(string row, string error)
        {
            if (errors.Contains(error)) return; errors.Add(error); var value = results.SingleOrDefault(result => result.Name == row);
            if (value == null) results.Add(new RuntimeSubscenarioResult { Name = row, Status = "FAIL", AssertionFailCount = 1, Errors = new[] { error } });
            else { value.Status = "FAIL"; value.AssertionFailCount++; value.Errors = value.Errors.Concat(new[] { error }).ToArray(); }
        }
        private static string Hash(string path) { using (var sha = SHA256.Create()) using (var stream = File.OpenRead(path)) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant(); }
        private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
        internal void BindAndStart(PersistenceSaveAuthorization scope)
        {
            Require(preflightReady&&authority==null&&!cleanup,"Assisted start requires its sole unused binding.");
            authority=scope??throw new ArgumentNullException(nameof(scope));generation=authorization.AuthorityGeneration;
            try
            {
                RequireNativeMenuAdmission();RequireNativeRangerSelection();RequireAuthority(false);
                Require(authority.MatchesAssistedFixturePlan(request,game,saveManager)&&authority.AssistedPhase=="initial-auto"&&
                    authority.BootstrapFreezeCount==0&&!authority.BootstrapWindowOpen&&authority.AdmittedNativeWriteRequests==0,
                    "Assisted authority is not its exact unused initial Auto plan.");
                Require(persistence.SnapshotCount==0&&persistence.CompletedSaveCount==0&&controls.NativeCastRequestCount==0,
                    "Native work preceded assisted start.");
                authority.BindAssistedNativeGate(request,game,saveManager,this,RequireNativeWriteOrigin);nativeGateOwned=true;
                probe=new Chunk7NativeBootstrapProbe(game,preset);probe.Install();
                evidence["before"]=Snapshot(false);priorSlot=NativePersistenceIsolation.NativeSlotRotationEnabled;
                Require(!priorSlot,"A pre-existing slot projection cannot be borrowed.");slotOwned=true;NativePersistenceIsolation.EnableNativeSlotRotation();
                var menu=game.UI.MainMenu;RequireNativeMenuAdmission();dispatched=true;
                menu.EnterGame(()=>{
                    callbackEntered=true;
                    try
                    {
                        Require(!cleanup&&(DialogMessageBox.Instance==null||!DialogMessageBox.Instance.IsShown),"Unexpected native popup/cancellation before owned start.");
                        RequireOriginalMenu();RequireAuthority(false);RequireNativeRangerSelection();
                        Require(ReferenceEquals(BlueprintRoot.Instance,blueprintRoot)&&ReferenceEquals(ResolveMainPreset(),preset),"Authored preset changed.");
                        authority.OpenBootstrapWindow();game.LoadNewGame(preset,null);
                    }
                    catch(Exception error){Fail("C7-assisted-native-start",error.ToString());BeginCleanup();}
                    finally{callbackReturned=true;}
                });
            }
            catch(Exception error){Fail("C7-assisted-native-start",error.ToString());BeginCleanup();}
        }
        internal void Update()
        {
            if(completed||disposed)return;
            try
            {
                if(cleanup){PollCleanup();return;}
                RequireAuthority(false);
                Require(DialogMessageBox.Instance==null||!DialogMessageBox.Instance.IsShown,"Unexpected system popup blocks assisted input.");
                Require(clock.Elapsed.TotalSeconds<=540,"Assisted fixture exceeded its finite setup window.");
                if(!callbackReturned||Busy())return;
                if(stage==0){BindInitialWorld();return;}
                if(stage==1){TickStory();return;}
                if(stage==2){TickBuild();return;}
                if(stage==3){FinishSetup();return;}
                if(stage==4){PollManualSave();return;}
                throw new InvalidOperationException("Unknown assisted stage.");
            }
            catch(Exception error){Fail("C7-assisted-runtime",error.ToString());BeginCleanup();}
        }
        private void BindInitialWorld()
        {
            Require(probe!=null&&probe.Complete&&ReferenceEquals(probe.NativePlayer,player)&&player.GameId==probe.NativeGameId,
                "Exact native new-game/identity/initial Auto proof incomplete.");
            var actor=player.MainCharacter.Value;var save=probe.NativeSave;var completion=persistence.LastCompletedSave;
            Require(actor!=null&&ReferenceEquals(game.CurrentlyLoadedArea,preset.Area)&&ReferenceEquals(actor.Blueprint,preset.PlayerCharacter??blueprintRoot.DefaultPlayerCharacter)&&
                game.State?.LoadedAreaState?.MainState!=null&&actor.View?.AgentASP!=null,"Native authored main/world has not materialized.");
            Require(save!=null&&completion!=null&&completion.Sequence==1&&ReferenceEquals(completion.Requested,probe.NativeRequestedSave)&&
                ReferenceEquals(completion.Written,save)&&!ReferenceEquals(save,probe.NativeRequestedSave)&&save.Type==SaveInfo.SaveType.Auto&&!save.IsAutoLevelupSave&&
                save.FileName==Chunk7AssistedFixtureSelection.InitialAutoLeaf&&save.FolderName==Path.Combine(authority.Root,save.FileName)&&
                completion.Path==save.FolderName&&save.OperationState==SaveInfo.StateType.None&&save.HasFileOnDisk&&save.IsActuallySaved&&
                ReferenceEquals(save.Area,preset.Area)&&save.GameId==player.GameId&&save.GameName==actor.CharacterName&&
                authority.BootstrapFreezeCount==1&&!authority.BootstrapWindowOpen&&authority.BootstrapGameId==player.GameId&&authority.BootstrapGameName==save.GameName&&
                authority.AdmittedNativeWriteRequests==1&&authorization.AuthorizedWriteCount==1&&persistence.SnapshotCount==1&&persistence.CompletedSaveCount==1,
                "Assisted initial native Auto lacks exact completion/identity/authority.");
            main=actor;mainId=main.UniqueId;campaignId=player.GameId;campaignName=main.CharacterName;
            nativeState=game.State;nativeAreaState=game.State.LoadedAreaState;nativeMainState=game.State.LoadedAreaState.MainState;
            nativeDescriptor=main.Descriptor;nativeView=main.View;initialAuto=save;
            RequireNoOwners();RequireWorld();RequireDescriptorInventory(save,false);
            nativeWrites.Add(CaptureWrite("initial-auto",probe.NativeRequestedSave,save,completion.Sequence));
            evidence["initialWorld"]=Snapshot(true);
            evidence["bootstrapIdentityBridge"]=new JObject{["contract"]="exact-object-label-domains-v1",["hostDomain"]="retained-host-2048",["observerDomain"]="retained-bootstrap-128",
                ["roles"]=new JObject{["game"]=BootstrapIdentityBridge(game),["player"]=BootstrapIdentityBridge(player),["saveManager"]=BootstrapIdentityBridge(saveManager),
                    ["main"]=BootstrapIdentityBridge(main),["requested"]=BootstrapIdentityBridge(probe.NativeRequestedSave),["prepared"]=BootstrapIdentityBridge(save),["preset"]=BootstrapIdentityBridge(preset)}};
            evidence["bootstrapProbe"]=probe.Capture();
            authority.ConfirmAssistedInitialAuto(request,game,saveManager,Target(save));
            probe.Dispose();Require(probe.IdentityReleased,"Initial bootstrap probe retained identities.");
            // Only the exact observed main is eligible. Target is derived from its
            // actual native classes; no nominal level or arbitrary XP is selected.
            var ranger=Kingmaker.Blueprints.ResourcesLibrary.TryGetBlueprint<Kingmaker.Blueprints.Classes.BlueprintCharacterClass>(RangerClassGuid);
            Require(ranger!=null,"Native Ranger class unavailable.");
            var rank=main.Descriptor.Progression.GetClassLevel(ranger);var level=main.Descriptor.Progression.CharacterLevel;
            targetLevel=checked(level+Math.Max(0,4-rank));
            Require(targetLevel>=4&&targetLevel<=8&&rank>=0&&level>=1,"Observed main requires an unsupported bounded level target.");
            stage=1;binding=new Chunk7AssistedFixtureBinding(request.RunId,campaignId,campaignName,mainId,game,player,saveManager,main,main.Descriptor,authority,generation);
            nativeBoundary=new Chunk7AssistedFixtureNativeBoundary(binding,AssertOwnedSetupAuthority,Busy,NoMountedCustody,CapturePreservedStateSha256,targetLevel);
            story=new Chunk7AssistedStoryDriver(binding,nativeBoundary);shortfall=new Chunk7AssistedShortfall(binding,nativeBoundary);
            evidence["derivedTarget"]=new JObject{["mainActorId"]=mainId,["characterLevel"]=level,["rangerLevel"]=rank,["targetLevel"]=targetLevel};
            stage=1;Pass("C7-assisted-native-start");
        }
        private JObject BootstrapIdentityBridge(object value)=>new JObject{["host"]=identities.Get(value),["observer"]=probe.Identity(value)};
        private void TickStory()
        {
            var status=story.Tick();evidence["setup"]=Chunk7AssistedFixtureEvidence.Capture(binding,story,shortfall);
            Require(status!=Chunk7AssistedStoryStatus.Failed&&status!=Chunk7AssistedStoryStatus.Disposed,"Assisted native story driver failed: "+story.Failure);
            if(status!=Chunk7AssistedStoryStatus.ReadyAtObservedFrame){readyFrame=-1;return;}
            if(readyFrame<0){readyFrame=UnityEngine.Time.frameCount;return;}
            if(UnityEngine.Time.frameCount<=readyFrame)return;
            RequireDefaultReady();Pass("C7-assisted-story");CaptureInitialRoster();
            preservationPurpose="experience";try{shortfall.EnsureExperience();}finally{preservationPurpose=null;evidence["setup"]=Chunk7AssistedFixtureEvidence.Capture(binding,story,shortfall);}
            Require(!shortfall.Failed,"Native XP shortfall failed; no retry is permitted.");
            if(Kingmaker.UnitLogic.Class.LevelUp.LevelUpController.CanLevelUp(main.Descriptor)){StartBuild();stage=2;}
            else{RequireAcquiredHorse();stage=3;}
        }
        private void AssertOwnedSetupAuthority()
        {
            Require(!cleanup&&stage>=1&&stage<=3&&binding!=null,"Assisted setup authority is outside its live selected phase.");
            RequireAuthority(false);RequireNoOwners();RequireWorld();
            Require(authority.AssistedPhase=="setup","Assisted setup storage phase changed.");
        }
        private string RequireNativeWriteOrigin(RuntimeSaveTarget target)
        {
            if(cleanup||!nativeGateOwned||!ReferenceEquals(Game.Instance,game)||!ReferenceEquals(game.Player,player)||!ReferenceEquals(game.SaveManager,saveManager))
                return "Assisted native write lost its exact caller/world.";
            if(authority.AssistedPhase=="initial-auto")
                return stage==0&&callbackEntered&&!callbackReturned&&probe!=null&&probe.InSynchronousCallback&&
                    probe.NativeGameId!=null&&probe.NativeGameId==player.GameId?null:"Initial Auto was not issued inside the exact authored new-game callback.";
            if(authority.AssistedPhase=="levelup-auto")
                return stage==2&&driver!=null&&ReferenceEquals(driver.Actor,main)&&ordinaryProbe!=null&&ordinaryProbe.InSynchronousCallback?
                    null:"Assisted Auto was not issued inside the exact ordinary level-up completion.";
            if(authority.AssistedPhase=="manual")
                return stage==4&&manualDispatching&&requestedManual!=null&&manualProbe!=null?null:"Assisted Manual was not issued by its exact retained descriptor call.";
            return "No assisted native write is selected in this phase.";
        }
        private void RequireAuthority(bool cleanupOnly)
        {
            Require(ReferenceEquals(Game.Instance,game)&&ReferenceEquals(game.Player,player)&&ReferenceEquals(game.SaveManager,saveManager),"Assisted world/manager identity changed.");
            if(authority==null)return;
            Require(authorization.IsActive&&authorization.AuthorityGeneration==generation&&authorization.IsExactPersistenceScope(authority)&&
                NativePersistenceIsolation.IsExactAuthority(authority)&&saveManager.SavePath==authority.Root&&
                NativePersistenceIsolation.ObservedStashFolder==Path.Combine(Path.GetDirectoryName(authority.Root),"Areas")&&
                authority.MatchesAssistedFixturePlan(request,game,saveManager)&&authority.AdmittedNativeLoadRequests==0&&authorization.AuthorizedLoadCount==0,
                "Assisted exact zero-load request/isolation authority changed.");
            if(!cleanupOnly)Require(authorization.UnauthorizedLoadCount==0&&authorization.UnauthorizedWriteCount==0&&authorization.FatalViolationCount==0&&
                authorization.SuppressedWorkingWriteCount==0&&authorization.BaselineLoadRequestCount==0,"Assisted native boundary was refused.");
            Require(persistence.SemanticRestoreCount==0&&persistence.PresentationRestoreCount==0&&persistence.ArchivePostLoadObservedCount==0&&
                controls.NativeCastRequestCount==0&&controls.CapturePersistentSlots().Length==0&&persistence.SnapshotCount<=3&&persistence.CompletedSaveCount<=3,
                "Assisted setup observed unselected mounted/load/save effects.");
            if(main!=null)RequireWorld();
        }
        private void RequireWorld()
        {
            Require(ReferenceEquals(game.State,nativeState)&&ReferenceEquals(game.State?.LoadedAreaState,nativeAreaState)&&
                ReferenceEquals(game.State?.LoadedAreaState?.MainState,nativeMainState)&&ReferenceEquals(game.CurrentlyLoadedArea,preset.Area)&&
                ReferenceEquals(player.MainCharacter.Value,main)&&ReferenceEquals(main.Descriptor,nativeDescriptor)&&ReferenceEquals(main.View,nativeView)&&
                main.UniqueId==mainId&&main.CharacterName==campaignName&&player.GameId==campaignId,"Assisted native world/main identity changed.");
        }
        private bool Busy()=>LoadingProcess.Instance!=null&&(LoadingProcess.Instance.IsLoadingInProcess||LoadingProcess.Instance.QueuedNames.Any())||
            NativePersistenceIsolation.HasPendingWrites||persistence.AreaTransitionPending||persistence.HasActiveSaveScope||persistence.HasSaveAdmissionFence||world.SerializationBusy;
        private bool NoMountedCustody()=>world.Registry.Owners.Length==0&&!controls.HasUnsettledRelationshipTransition&&!world.UnifiedTurn.HasUnsettledPreparation&&
            world.Pairs.All(pair=>pair.Relationship.State==RelationshipState.Unmounted&&pair.Relationship.PairOwner==null&&pair.Relationship.Runtime.NoPreparedPairResidue&&
                !pair.Combat.HasActiveCommand&&!pair.Combat.HasActiveGroundMovement&&!pair.Combat.HasChargeOwnership);
        private void RequireNoOwners()=>Require(NoMountedCustody(),"Assisted setup found mounted command/preparation custody.");
        private void RequireDefaultReady()
        {
            RequireAuthority(false);RequireNoOwners();RequireWorld();var snapshot=nativeBoundary.ReadStory();
            Require(snapshot.Mode=="Default"&&!snapshot.NativeBusy&&!snapshot.Cutscene&&!snapshot.DialogActive&&!snapshot.TransitionPending&&!snapshot.UnexpectedPopup&&snapshot.MainViewReady&&
                !player.IsInCombat,"Assisted world still has native story/loading/combat custody.");
        }
        private JObject AuthoritySnapshot()=>new JObject{["root"]=authority?.Root,["generation"]=generation,["phase"]=authority?.AssistedPhase,
            ["maximumNativeLoads"]=authority?.MaximumNativeLoadRequests,["maximumNativeWrites"]=authority?.MaximumNativeWriteRequests,
            ["admittedNativeLoads"]=authority?.AdmittedNativeLoadRequests,["admittedNativeWrites"]=authority?.AdmittedNativeWriteRequests,
            ["authorizedLoads"]=authorization.AuthorizedLoadCount,["authorizedWrites"]=authorization.AuthorizedWriteCount,
            ["unauthorizedLoads"]=authorization.UnauthorizedLoadCount,["unauthorizedWrites"]=authorization.UnauthorizedWriteCount,
            ["fatalViolations"]=authorization.FatalViolationCount,["suppressedWrites"]=authorization.SuppressedWorkingWriteCount,
            ["freezeCount"]=authority?.BootstrapFreezeCount,["windowOpen"]=authority?.BootstrapWindowOpen,["frozenGameId"]=authority?.BootstrapGameId,
            ["frozenGameName"]=authority?.BootstrapGameName,["manualNumber"]=authority?.AssistedManualNumber,["manualLeaf"]=authority?.AssistedManualLeaf,
            ["reservationCount"]=authority?.AssistedReservationCount,["levelupWrites"]=authority?.AssistedLevelupWrites};
        private JObject Snapshot(bool field)=>new JObject{["frame"]=UnityEngine.Time.frameCount,["game"]=identities.Get(game),["player"]=identities.Get(player),
            ["saveManager"]=identities.Get(saveManager),["mainActor"]=identities.Get(main),["mainActorId"]=main?.UniqueId,["gameId"]=player?.GameId,
            ["gameName"]=main?.CharacterName,["area"]=game.CurrentlyLoadedArea?.AssetGuidThreadSafe,["mode"]=game.CurrentMode.ToString(),
            ["nativeLoading"]=Busy(),["authority"]=AuthoritySnapshot(),["party"]=field?CaptureActors(false):new JArray()};
    }
}
