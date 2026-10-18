using System;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Root;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.GameModes;
using Kingmaker.UI;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Chunk7AssistedFixtureScenario
    {
        private const string RangerClassGuid="cda0615668a6df14eb36ba19ee881af6";
        private UnitEntityData[] originalActors,originalCharacters,originalRemote,originalSelection;
        private object[] originalDescriptors,originalViews;
        private Chunk7OrdinaryBuildDriver driver;
        private NativeOrdinaryAcquisitionProbe ordinaryProbe;
        private Chunk7NativeBuilderCancellation cancellation;
        private MovementCaptureOverlayLease ummLease;
        private UnityModManagerNet.UnityModManager.UI ummInstance;
        private bool originalUmmOpened,originalPause,presentationCaptured,buildComplete,levelAutoComplete;
        private int experienceReads,moneyReads;private string preservationPurpose;
        private void CaptureInitialRoster()
        {
            Require(originalActors==null,"Assisted original roster may be captured once.");
            var actors=FieldActors();var chars=player.PartyCharacters.Take(7).Select(x=>x.Value).ToArray();var remote=player.RemoteCompanions.Take(33).Select(x=>x.Value).ToArray();
            Require(chars.Length>0&&chars.Length<=6&&remote.Length<=32&&chars.All(x=>x!=null)&&remote.All(x=>x!=null)&&
                remote.Distinct().Count()==remote.Length&&remote.All(x=>!actors.Contains(x)),"Native character/remote roster is duplicate, unbounded or overlaps field.");
            originalActors=actors;originalCharacters=chars;originalRemote=remote;
            originalDescriptors=actors.Select(x=>(object)x.Descriptor).ToArray();originalViews=actors.Select(x=>(object)x.View).ToArray();
            evidence["beforeSetup"]=Snapshot(true);
        }
        private UnitEntityData[] FieldActors()
        {
            var actors=Chunk7NativeFieldRoster.Capture(player);
            Require(actors.Length>0&&actors.Length<=12&&actors.All(x=>x!=null)&&actors.Select(x=>x.UniqueId).Distinct(StringComparer.Ordinal).Count()==actors.Length,
                "Assisted field actor scope is null, duplicate or exceeds twelve.");return actors;
        }
        private void RequireRoster(bool ready)
        {
            var current=FieldActors();if(originalActors!=null)
            {
                Require(player.PartyCharacters.Select(x=>x.Value).SequenceEqual(originalCharacters)&&player.RemoteCompanions.Select(x=>x.Value).SequenceEqual(originalRemote),
                    "Assisted setup changed native character/remote membership or order.");
                for(var i=0;i<originalActors.Length;i++)Require(current.Count(x=>ReferenceEquals(x,originalActors[i]))==1&&
                    ReferenceEquals(originalActors[i].Descriptor,originalDescriptors[i])&&ReferenceEquals(originalActors[i].View,originalViews[i]),
                    "Assisted original field actor/descriptor/view was replaced.");
                Require(current.All(x=>originalActors.Contains(x)||ReferenceEquals(x,main.Descriptor.Pet)),"An unselected field actor entered assisted setup.");
            }
            if(ready)Require(current.All(x=>x.IsInState&&x.IsInGame&&x.View?.AgentASP!=null&&x.Descriptor.State.IsConscious&&!x.Descriptor.State.IsDead&&
                !x.IsInCombat&&x.Commands!=null&&x.Commands.Empty&&x.Commands.Queue!=null&&x.Commands.Queue.Count==0),"Assisted full native field is not living, idle and settled.");
        }
        private JArray CaptureActors(bool includeExperience,bool includeRemote=false)
        {
            var actors=FieldActors();
            if(includeRemote){var remote=player.RemoteCompanions.Take(33).Select(x=>x.Value).ToArray();Require(remote.Length<=32&&remote.All(x=>x!=null),"Remote preservation roster exceeds its bound.");actors=actors.Concat(remote).ToArray();Require(actors.Distinct().Count()==actors.Length&&actors.Select(x=>x.UniqueId).Distinct(StringComparer.Ordinal).Count()==actors.Length,"Remote/field preservation identities overlap.");}
            var result=new JArray();var totalFacts=0;
            foreach(var actor in actors.OrderBy(x=>x.UniqueId,StringComparer.Ordinal))
            {
                var classes=actor.Descriptor.Progression.Classes.Take(21).ToArray();
                var facts=actor.Descriptor.Progression.Features.Enumerable.Take(129).ToArray();
                var buffs=actor.Buffs.Enumerable.Take(65).ToArray();totalFacts+=facts.Length+buffs.Length;
                Require(classes.Length<=20&&facts.Length<=128&&buffs.Length<=64&&totalFacts<=1024&&classes.All(x=>x?.CharacterClass!=null)&&
                    facts.All(x=>x?.Blueprint!=null)&&buffs.All(x=>x?.Blueprint!=null),"Assisted progression/fact inventory is malformed or over bound.");
                var row=new JObject{["id"]=actor.UniqueId,["object"]=identities.Get(actor),["descriptor"]=identities.Get(actor.Descriptor),["view"]=identities.Get(actor.View),
                    ["blueprint"]=actor.Blueprint.AssetGuid,["masterId"]=actor.Descriptor.Master.Value?.UniqueId,["petId"]=actor.Descriptor.Pet?.UniqueId,
                    ["inState"]=actor.IsInState,["inGame"]=actor.IsInGame,["conscious"]=actor.Descriptor.State.IsConscious,["dead"]=actor.Descriptor.State.IsDead,
                    ["inCombat"]=actor.IsInCombat,["commandsEmpty"]=actor.Commands.Empty,["queuedCommands"]=actor.Commands.Queue.Count,
                    ["characterLevel"]=actor.Descriptor.Progression.CharacterLevel,
                    ["classes"]=new JArray(classes.OrderBy(x=>x.CharacterClass.AssetGuid,StringComparer.Ordinal).Select(x=>new JObject{["guid"]=x.CharacterClass.AssetGuid,["level"]=x.Level})),
                    ["features"]=new JArray(facts.OrderBy(x=>x.Blueprint.AssetGuid,StringComparer.Ordinal).ThenBy(x=>identities.Get(x)).Select(x=>new JObject{
                        ["object"]=identities.Get(x),["guid"]=x.Blueprint.AssetGuid,["source"]=x.Source?.AssetGuid,["rank"]=x.GetRank()})),
                    ["buffs"]=new JArray(buffs.OrderBy(x=>x.Blueprint.AssetGuid,StringComparer.Ordinal).ThenBy(x=>identities.Get(x)).Select(x=>new JObject{["object"]=identities.Get(x),["guid"]=x.Blueprint.AssetGuid})),
                    ["actions"]=new JObject{["move"]=actor.CombatState.Cooldown.MoveAction,["standard"]=actor.CombatState.Cooldown.StandardAction,
                        ["swift"]=actor.CombatState.Cooldown.SwiftAction,["prepared"]=actor.CombatState.Prepared,["reactions"]=actor.CombatState.AttackOfOpportunityCount,
                        ["reactionAllowance"]=actor.CombatState.AttackOfOpportunityPerRound,["initiative"]=actor.CombatState.Initiative}};
                if(includeExperience||!ReferenceEquals(actor,main))row["experience"]=actor.Descriptor.Progression.Experience;result.Add(row);
            }
            return result;
        }
        private string CapturePreservedStateSha256()
        {
            Require(preservationPurpose=="experience"||preservationPurpose=="hire-funds","No assistance preservation read is selected.");
            RequireAuthority(false);RequireNoOwners();RequireWorld();RequireRoster(true);
            Require(preservationRows.Count<12&&!Busy(),"Assisted preservation is busy or exceeded its bounded snapshot count.");
            var frame=UnityEngine.Time.frameCount;var state=new JObject{["game"]=identities.Get(game),["player"]=identities.Get(player),["saveManager"]=identities.Get(saveManager),
                ["state"]=identities.Get(game.State),["areaState"]=identities.Get(game.State.LoadedAreaState),["mainState"]=identities.Get(game.State.LoadedAreaState.MainState),
                ["mainActor"]=identities.Get(main),["mainActorId"]=mainId,["gameId"]=campaignId,["gameName"]=campaignName,["area"]=preset.Area.AssetGuidThreadSafe,
                ["mode"]=game.CurrentMode.ToString(),["gameTimeTicks"]=game.TimeController.GameTime.Ticks,["experienceRatePercent"]=player.ExperienceRatePercent,
                ["nativeCasts"]=controls.NativeCastRequestCount,["snapshots"]=persistence.SnapshotCount,["completedSaves"]=persistence.CompletedSaveCount,
                ["semanticRestores"]=persistence.SemanticRestoreCount,["presentationRestores"]=persistence.PresentationRestoreCount,["nativePostLoads"]=persistence.ArchivePostLoadObservedCount,
                ["mountedOwners"]=world.Registry.Owners.Length,["mountedSlots"]=controls.CapturePersistentSlots().Length,
                ["partyCharacters"]=new JArray(player.PartyCharacters.Select(x=>new JObject{["id"]=x.Value.UniqueId,["object"]=identities.Get(x.Value)})),
                ["remoteCompanions"]=new JArray(player.RemoteCompanions.Select(x=>new JObject{["id"]=x.Value.UniqueId,["object"]=identities.Get(x.Value)})),["actors"]=CaptureActors(false,true)};
            Require(frame==UnityEngine.Time.frameCount,"Assisted preservation frame changed during capture.");
            var text=state.ToString(Formatting.None);Require(Encoding.UTF8.GetByteCount(text)<=262144,"Assisted preservation snapshot exceeds 256 KiB.");
            var hash=HashText(text);var index=preservationPurpose=="experience"?++experienceReads:++moneyReads;
            Require(index<=3,"Assisted effect made an unselected preservation read.");
            preservationRows.Add(new JObject{["sequence"]=preservationRows.Count+1,["frame"]=frame,
                ["purpose"]=preservationPurpose+"-"+(index==1?"before":index==2?"entry":"after"),["state"]=state,["canonicalJson"]=text,["sha256"]=hash});
            return hash;
        }
        private static string HashText(string text){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(new UTF8Encoding(false,true).GetBytes(text))).Replace("-","").ToLowerInvariant();}
        private void StartBuild()
        {
            RequireDefaultReady();RequireRoster(true);
            Require(driver==null&&game.UI?.CharacterBuildController!=null&&game.UI.CharacterBuildController.LevelUpController==null&&game.UI.CharacterBuildController.Unit==null&&
                main.Descriptor.Pet==null&&saveManager.IsSaveAllowed()&&slotOwned&&NativePersistenceIsolation.NativeSlotRotationEnabled,
                "Assisted native main builder is not exclusively available.");
            var table=BlueprintRoot.Instance.Progression.XPTable;var xp=main.Descriptor.Progression.Experience;
            Require(table?.Bonuses!=null&&table.Bonuses.Length>=21&&Enumerable.Range(targetLevel+1,20-targetLevel).All(level=>xp<table.GetBonus(level)),
                "Observed XP exceeds the bounded selected attained level; no extra native levels are authorized.");
            authority.ReserveAssistedLevelup(request,game,saveManager,Target(initialAuto));
            originalSelection=SelectionManager.Instance.SelectedUnits.ToArray();
            Require(originalSelection.Distinct().Count()==originalSelection.Length&&originalSelection.All(x=>originalActors.Contains(x)),"Initial selection is foreign or duplicate.");
            originalPause=game.IsPaused;ummInstance=UnityModManagerNet.UnityModManager.UI.Instance;
            Require(ummInstance!=null&&ummLease==null,"Exact unborrowed UMM instance is unavailable.");originalUmmOpened=ummInstance.Opened;
            presentationCaptured=true;MovementScreenshotCaptureCoordinator.AcquireClosedUmmLeaseRetained(out ummLease);
            var pins=new Chunk7OrdinaryBuildPins(request.RunId,HashText(evidence["derivedTarget"].ToString(Formatting.None)),Chunk7OrdinaryBuildMode.ExistingRangerLevels,
                null,null,null,Gender.Male,false,4,Size.Medium);
            driver=new Chunk7OrdinaryBuildDriver(pins,main,horses.HorseFeature,horses.HorseUnit.AssetGuid,AssertChoiceAuthority,ObserveChoice);
            ordinaryProbe=NativeOrdinaryAcquisitionProbe.ForAssistedFixture(()=>driver.Actor);ordinaryProbe.Install();
            game.IsPaused=false;
        }
        private void AssertChoiceAuthority()
        {
            Require(!cleanup&&stage==2,"Assisted builder is outside its selected phase.");RequireAuthority(false);RequireNoOwners();RequireRoster(false);
            Require(!Busy()&&!player.IsInCombat&&!game.CutsceneLock&&!game.IsModeActive(GameModeType.Dialog)&&
                (DialogMessageBox.Instance==null||!DialogMessageBox.Instance.IsShown)&&slotOwned&&NativePersistenceIsolation.NativeSlotRotationEnabled,
                "Native write/story/popup or lost projection blocks assisted builder input.");
        }
        private void ObserveChoice(JObject observation){Require(choices.Count<1400,"Assisted native choice evidence exceeded its bound.");choices.Add(observation.DeepClone());}
        private void TickBuild()
        {
            AssertChoiceAuthority();
            if(!buildComplete)
            {
                if(driver.Tick()!=Chunk7OrdinaryBuildStep.Complete)return;
                RequireAcquiredHorse();evidence["builderComplete"]=driver.CaptureState();buildComplete=true;Pass("C7-assisted-native-choices");return;
            }
            if(!levelAutoComplete)
            {
                // NOT READY - stale level-up autosave wait (docs/CHUNK7-STALE-LEVELUP-AUTOSAVE-FAMILIES.md).
                // The installed game never registers LevelUpAutoSaveController (GameModesFactory builds its
                // controllers explicitly without it), so an ordinary level-up never requests this native Auto
                // and the wait cannot complete. scripts/runtime/Invoke-KingmakerRuntimeScenario.ps1 refuses this
                // family before any lock, transaction, Mods swap or launch. Kept intact until its completion
                // contract is repaired; never manufacture a level-up callback or relabel a Manual as Auto.
                if(!ObserveLevelupAutosave())return;levelAutoComplete=true;
                evidence["ordinaryProbe"]=ordinaryProbe.Capture();ordinaryProbe.Dispose();stage=3;
            }
        }
        private void RequireAcquiredHorse()
        {
            var pet=main.Descriptor.Pet;var ranger=ResourcesLibrary.TryGetBlueprint<BlueprintCharacterClass>(RangerClassGuid);
            Require(ranger!=null&&main.Descriptor.Progression.GetClassLevel(ranger)>=4&&main.Descriptor.Progression.CharacterLevel==targetLevel&&
                !LevelUpController.CanLevelUp(main.Descriptor)&&pet!=null&&ReferenceEquals(pet.Blueprint,horses.HorseUnit)&&ReferenceEquals(pet.Descriptor.Master.Value,main)&&
                main.Descriptor.GetFact(horses.HorseFeature) is Feature feature&&feature.Source!=null&&
                (main.Descriptor.GetFact(horses.LevelRank)?.GetRank()??0)>0,"Assisted native choices did not produce the exact reciprocal sourced Horse and attained Ranger level.");
        }
        private void FinishSetup()
        {
            RequireDefaultReady();RequireRoster(true);RequireAcquiredHorse();
            if(shortfall.MoneyEffect==null)
            {
                preservationPurpose="hire-funds";
                try{shortfall.EnsureOneNativeHireCost();}finally{preservationPurpose=null;evidence["setup"]=Chunk7AssistedFixtureEvidence.Capture(binding,story,shortfall);}
                Require(!shortfall.Failed,"Native hire funds shortfall failed.");
                Pass("C7-assisted-setup");if(driver==null)Pass("C7-assisted-native-choices");
            }
            RestorePresentation();
            if(slotOwned){Require(NativePersistenceIsolation.NativeSlotRotationEnabled,"Owned projection was lost.");NativePersistenceIsolation.DisableNativeSlotRotation();slotOwned=false;}
            RequireDefaultReady();RequireRoster(true);
            evidence["nativeProfileInventory"]=Chunk7NativeProfileInventory.Capture(FieldActors());
            Require((string)evidence["nativeProfileInventory"]["captureStatus"]=="PASS","Native final profile inventory did not complete.");
            evidence["fixtureReadiness"]=CaptureFixtureReadiness();
            evidence["fieldWorld"]=Snapshot(true);evidence["afterSetupActors"]=CaptureActors(true);
            BeginManualSave();
        }
        private JObject CaptureFixtureReadiness()
        {
            RequireWorld();
            var settings = Kingmaker.UI.SettingsUI.SettingsRoot.Instance;
            Require(settings != null && settings.OnlyOneSave != null && settings.AutosaveEnabled != null && settings.AutosaveSlots != null,
                "Native save settings are unavailable for read-only inventory.");
            var onlyOne = settings.OnlyOneSave; var enabled = settings.AutosaveEnabled; var slots = settings.AutosaveSlots;
            var actors=FieldActors();
            var result = new JObject { ["schemaVersion"] = 1, ["contract"] = "installed-native-fixture-readiness-v1",
                ["captureStatus"] = "IN PROGRESS", ["frame"] = UnityEngine.Time.frameCount,
                ["gameplayAssemblyMvid"] = typeof(Game).Module.ModuleVersionId.ToString("D"), ["observationOnly"] = true };
            evidence["fixtureReadiness"] = result;
            var projection = NativePersistenceIsolation.NativeSlotRotationEnabled;
            var saveSettings = new JObject { ["observationKind"] = "native-effective-getters", ["projectionEnabled"] = projection,
                ["autosaveSlotsNativeType"] = "System.Single", ["autosaveCapacityInferred"] = false,
                ["sliderFallbackCallbackPresent"] = slots.OverrideDefaultValue != null, ["settingsWritten"] = false };
            result["saveSettings"] = saveSettings;
            // This observation phase holds no slot projection. The native slider getter
            // may invoke a default-value callback; refuse that path rather than
            // executing an unknown action or reproducing its parsing semantics.
            Require(!projection && slots.OverrideDefaultValue == null,
                "Read-only inventory refuses an active slot projection or native slider fallback callback.");
            saveSettings["onlyOneSave"] = onlyOne.CurrentValue;
            saveSettings["autosaveEnabled"] = enabled.CurrentValue;
            var slotValue = slots.CurrentValue;
            Require(!float.IsNaN(slotValue) && !float.IsInfinity(slotValue), "Native autosave slot setting is non-finite.");
            saveSettings["autosaveSlots"] = slotValue; // Preserve Single; do not round or infer slot capacity.
            result["experienceRatePercent"] = game.Player.ExperienceRatePercent;

            var main = game.Player.MainCharacter.Value; var progression = main.Descriptor.Progression;
            var classes = progression.Classes.Take(21).ToArray();
            Require(classes.Length > 0 && classes.Length <= 20 && classes.All(value => value != null && value.CharacterClass != null &&
                value.Level > 0 && value.Level <= 20) && classes.Select(value => value.CharacterClass.AssetGuid).Distinct(StringComparer.Ordinal).Count() == classes.Length &&
                classes.Sum(value => value.Level) == progression.CharacterLevel, "Native main class progression is missing, duplicate or unbounded.");
            var classRows = new JArray();
            foreach (var value in classes)
            {
                var archetypes = value.Archetypes == null ? new string[0] : value.Archetypes.Take(17).Select(item => item == null ? null : item.AssetGuid).ToArray();
                Require(archetypes.Length <= 16 && archetypes.All(item => item != null) && archetypes.Distinct(StringComparer.Ordinal).Count() == archetypes.Length,
                    "Native main archetype inventory is missing, duplicate or unbounded.");
                classRows.Add(new JObject { ["classGuid"] = value.CharacterClass.AssetGuid, ["level"] = value.Level, ["archetypeGuids"] = new JArray(archetypes) });
            }
            result["mainProgression"] = new JObject { ["actorId"] = main.UniqueId, ["characterLevel"] = progression.CharacterLevel,
                ["experience"] = progression.Experience, ["classes"] = classRows };
            var characters = game.Player.PartyCharacters.Take(7).Select(value => value.Value).ToArray();
            Require(characters.Length > 0 && characters.Length <= 6 && characters.All(value => value != null && actors.Any(actor => ReferenceEquals(actor, value))) &&
                characters.Select(value => value.UniqueId).Distinct(StringComparer.Ordinal).Count() == characters.Length,
                "Native party-character inventory is foreign, duplicate or unbounded.");
            result["staticPartyFacts"] = new JObject { ["selectorOpened"] = false, ["liveSelectorRequiredAuthorityAvailable"] = false,
                ["membershipOrRemovalQualified"] = false, ["characters"] = new JArray(characters.Select(value => new JObject {
                    ["actorId"] = value.UniqueId, ["blueprintGuid"] = value.Blueprint.AssetGuid, ["isMain"] = ReferenceEquals(value, main),
                    ["lockedCompanionComponent"] = value.Blueprint.GetComponent<Kingmaker.Blueprints.LockedCompanionComponent>() != null })) };

            var saves = saveManager.Take(33).ToArray();
            Require(saves.Length <= 32 && saves.All(value => value != null), "Isolated native descriptor inventory is null or exceeds its bound.");
            var saveRows = new JArray(saves.OrderBy(value => value.FileName, StringComparer.Ordinal).Select(value => new JObject {
                ["fileName"] = value.FileName, ["internalName"] = value.Name, ["gameId"] = value.GameId, ["gameName"] = value.GameName,
                ["area"] = value.Area == null ? null : value.Area.AssetGuidThreadSafe, ["type"] = value.Type.ToString(),
                ["isAutoLevelupSave"] = value.IsAutoLevelupSave, ["isActuallySaved"] = value.IsActuallySaved,
                ["hasFileOnDisk"] = value.HasFileOnDisk, ["operationState"] = value.OperationState.ToString(),
                ["systemSaveTimeTicks"] = value.SystemSaveTime.Ticks, ["systemSaveTimeKind"] = value.SystemSaveTime.Kind.ToString() }));
            result["isolatedSaveDescriptors"] = new JObject { ["root"] = saveManager.SavePath, ["metadataOnly"] = true,
                ["maximumObservedDescriptors"] = 32, ["descriptors"] = saveRows };
            RequireDescriptorInventory(initialAuto,false);
            Require(ReferenceEquals(settings, Kingmaker.UI.SettingsUI.SettingsRoot.Instance) && ReferenceEquals(onlyOne, settings.OnlyOneSave) &&
                ReferenceEquals(enabled, settings.AutosaveEnabled) && ReferenceEquals(slots, settings.AutosaveSlots) &&
                !NativePersistenceIsolation.NativeSlotRotationEnabled && slots.OverrideDefaultValue == null,
                "Native settings identity or read-only getter admission changed during observation.");
            RequireWorld(); result["captureStatus"] = "PASS"; return result;
        }
        private void AssertCleanupInputAuthority()
        {
            RequireAuthority(true);RequireNoOwners();
            Require(!Busy()&&!player.IsInCombat&&!game.CutsceneLock&&!game.IsModeActive(GameModeType.Dialog),"Native cleanup retains busy/combat/story custody.");
        }
        private bool DrainBuildCustody()
        {
            if(driver!=null&&cancellation==null&&driver.TryGetOwnedBuildSession(out var builder))cancellation=new Chunk7NativeBuilderCancellation(builder,AssertCleanupInputAuthority,Busy);
            return cancellation==null||cancellation.TryDrain();
        }
        private void RestorePresentation()
        {
            if(!presentationCaptured)return;AssertCleanupInputAuthority();
            Require(DialogMessageBox.Instance==null||!DialogMessageBox.Instance.IsShown,"Foreign popup retains final presentation custody.");
            Require(ReferenceEquals(UnityModManagerNet.UnityModManager.UI.Instance,ummInstance)&&ummInstance!=null,"Captured UMM instance changed.");
            SelectionManager.Instance.MultiSelect(originalSelection.Select(x=>x.View),false);
            Require(SelectionManager.Instance.SelectedUnits.SequenceEqual(originalSelection),"Original exact selection did not restore.");
            ummLease?.Dispose();Require(ummInstance.Opened==originalUmmOpened,"Original UMM state did not restore.");ummLease=null;game.IsPaused=originalPause;
            presentationCaptured=false;
        }
    }
}
