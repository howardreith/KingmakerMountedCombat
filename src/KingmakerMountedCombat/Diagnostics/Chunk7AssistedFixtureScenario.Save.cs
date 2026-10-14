using System;
using System.IO;
using System.Linq;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.GameModes;
using Kingmaker.UI;
using KingmakerMountedCombat.Integration;
using KingmakerMountedCombat.Domain;
using Newtonsoft.Json.Linq;
namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Chunk7AssistedFixtureScenario
    {
        private Chunk7ManualSeedSaveProbe manualProbe;
        private SaveInfo requestedManual;
        private bool manualDispatching,manualRequested,manualCallback,manualProven;
        private int manualCallbacks;private JObject beforeManualArrangement;
        private static RuntimeSaveTarget Target(SaveInfo save)=>new RuntimeSaveTarget{InternalName=save.Name,FileName=save.FileName,FullPath=save.FolderName,
            SaveType=save.Type.ToString(),GameId=save.GameId,GameName=save.GameName,Area=save.Area?.AssetGuidThreadSafe};
        private JArray DescriptorInventory()
        {
            var saves=saveManager.Take(5).ToArray();Require(saves.Length<=4&&saves.All(x=>x!=null),"Assisted native descriptor inventory is null or unbounded.");
            return new JArray(saves.OrderBy(x=>x.FileName,StringComparer.Ordinal).Select(x=>new JObject{["descriptor"]=identities.Get(x),["fileName"]=x.FileName,["internalName"]=x.Name,
                ["gameId"]=x.GameId,["gameName"]=x.GameName,["area"]=x.Area?.AssetGuidThreadSafe,["type"]=x.Type.ToString(),["autoLevelup"]=x.IsAutoLevelupSave,
                ["isActuallySaved"]=x.IsActuallySaved,["hasFileOnDisk"]=x.HasFileOnDisk,["operation"]=x.OperationState.ToString()}));
        }
        private void RequireDescriptorInventory(SaveInfo auto,bool manual)
        {
            var saves=saveManager.Take(5).ToArray();
            Require(saves.Length==(manual?3:2)&&saves.Count(x=>ReferenceEquals(x,auto))==1&&
                saves.Count(x=>x!=null&&x.FileName==request.Fixture.Working.FileName&&x.Name==request.Fixture.Working.InternalName&&x.GameId==request.Fixture.Working.GameId&&
                    x.GameName==request.Fixture.Working.GameName&&x.Area?.AssetGuidThreadSafe==request.Fixture.Working.Area&&x.Type==SaveInfo.SaveType.Manual)==1&&
                (!manual||saves.Count(x=>ReferenceEquals(x,manualProbe?.Written))==1),"Native isolated inventory differs from exact Working anchor, current Auto and selected Manual.");
            foreach(var save in saves)Require(save.IsActuallySaved&&save.HasFileOnDisk&&save.OperationState==SaveInfo.StateType.None&&
                authority.ValidateArchiveIdentity(Target(save),authority.Root)==null,"Native descriptor/file identity differs from isolated authority.");
        }
        private JObject CaptureWrite(string phase,SaveInfo requested,SaveInfo written,int sequence)
        {
            var file=new FileInfo(written.FolderName);
            Require(file.Exists&&file.Length>0&&(file.Attributes&FileAttributes.ReparsePoint)==0&&file.FullName==Path.Combine(authority.Root,written.FileName),
                "Native assisted output did not materialize at its exact owned leaf.");
            return new JObject{["phase"]=phase,["frame"]=UnityEngine.Time.frameCount,["path"]=file.FullName,["fileName"]=written.FileName,["internalName"]=written.Name,
                ["gameId"]=written.GameId,["gameName"]=written.GameName,["area"]=written.Area?.AssetGuidThreadSafe,["type"]=written.Type.ToString(),
                ["operation"]=written.OperationState.ToString(),["autoLevelup"]=written.IsAutoLevelupSave,["length"]=file.Length,["sha256"]=Hash(file.FullName),
                ["lastWriteTimeUtcTicks"]=file.LastWriteTimeUtc.Ticks,["requestedDescriptor"]=identities.Get(requested),["preparedDescriptor"]=identities.Get(written),
                ["completionSequence"]=sequence,["snapshots"]=persistence.SnapshotCount,["completedSaves"]=persistence.CompletedSaveCount,
                ["authority"]=AuthoritySnapshot(),["contentInspected"]=false,["coldLoadQualified"]=false};
        }
        private bool ObserveLevelupAutosave()
        {
            if(Busy()||ordinaryProbe?.ObservedSave==null||ordinaryProbe.ObservedSave.OperationState!=SaveInfo.StateType.None)return false;
            var save=ordinaryProbe.ObservedSave;var completion=persistence.LastCompletedSave;
            Require(ordinaryProbe.Complete&&ReferenceEquals(ordinaryProbe.ObservedRequestedSave,initialAuto)&&completion!=null&&completion.Sequence==2&&
                ReferenceEquals(completion.Requested,initialAuto)&&ReferenceEquals(completion.Written,save)&&!ReferenceEquals(save,initialAuto)&&
                save.Type==SaveInfo.SaveType.Auto&&save.IsAutoLevelupSave&&save.FileName==Chunk7AssistedFixtureSelection.InitialAutoLeaf&&
                save.FolderName==Path.Combine(authority.Root,save.FileName)&&completion.Path==save.FolderName&&save.IsActuallySaved&&save.HasFileOnDisk&&
                ReferenceEquals(save.Area,preset.Area)&&save.GameId==campaignId&&save.GameName==campaignName&&
                authority.AdmittedNativeWriteRequests==2&&authorization.AuthorizedWriteCount==2&&persistence.SnapshotCount==2&&persistence.CompletedSaveCount==2,
                "Assisted terminal Auto lacks exact original request, prepared replacement, native completion or cumulative quota.");
            NativePersistenceIsolation.RequireOwnedWrite(save);RequireDescriptorInventory(save,false);
            nativeWrites.Add(CaptureWrite("levelup-auto",initialAuto,save,2));
            evidence["ordinaryIdentityBridge"]=new JObject{["contract"]="exact-object-label-domains-v1",["hostDomain"]="retained-host-2048",["observerDomain"]="runtime-object-hash",
                ["roles"]=new JObject{["actor"]=OrdinaryIdentityBridge(main),["descriptor"]=OrdinaryIdentityBridge(main.Descriptor),["requested"]=OrdinaryIdentityBridge(initialAuto),["prepared"]=OrdinaryIdentityBridge(save),["saveManager"]=OrdinaryIdentityBridge(saveManager)}};
            evidence["afterLevelupDescriptors"]=DescriptorInventory();
            authority.ConfirmAssistedLevelup(request,game,saveManager,Target(save));initialAuto=save;return true;
        }
        private JObject OrdinaryIdentityBridge(object value)=>new JObject{["host"]=identities.Get(value),["observer"]=NativeOrdinaryAcquisitionProbe.Id(value)};
        private JObject Arrangement()=>new JObject{["money"]=player.Money,["experienceRatePercent"]=player.ExperienceRatePercent,["actors"]=new JArray(FieldActors().OrderBy(x=>x.UniqueId,StringComparer.Ordinal).Select(x=>new JObject{
            ["id"]=x.UniqueId,["object"]=identities.Get(x),["descriptor"]=identities.Get(x.Descriptor),["view"]=identities.Get(x.View),["blueprint"]=x.Blueprint.AssetGuid,
            ["masterId"]=x.Descriptor.Master.Value?.UniqueId,["petId"]=x.Descriptor.Pet?.UniqueId,["experience"]=x.Descriptor.Progression.Experience,["level"]=x.Descriptor.Progression.CharacterLevel,
            ["classes"]=new JArray(x.Descriptor.Progression.Classes.OrderBy(c=>c.CharacterClass.AssetGuid,StringComparer.Ordinal).Select(c=>new JObject{["guid"]=c.CharacterClass.AssetGuid,["level"]=c.Level}))}))};
        private void BeginManualSave()
        {
            RequireDefaultReady();RequireRoster(true);RequireAcquiredHorse();
            Require(!manualRequested&&manualProbe==null&&!NativePersistenceIsolation.NativeSlotRotationEnabled&&saveManager.IsSaveAllowed()&&player.SavedInArea==game.CurrentlyLoadedArea,
                "Final assisted Manual is not an eligible fresh native write.");
            RequireDescriptorInventory(initialAuto,false);var before=DescriptorInventory();
            var number=saveManager.FindUnusedSaveNumber(SaveInfo.SaveType.Manual);var leaf=Chunk7AssistedFixtureSelection.ManualLeaf(number);
            Require(!File.Exists(Path.Combine(authority.Root,leaf)),"Selected native Manual leaf already exists.");
            authority.ReserveAssistedManual(request,game,saveManager,number);
            Require(saveManager.FindUnusedSaveNumber(SaveInfo.SaveType.Manual)==number&&!File.Exists(Path.Combine(authority.Root,leaf)),"Manual inventory/number changed after atomic reservation.");
            evidence["nativeNumbering"]=new JObject{["method"]="FindUnusedSaveNumber",["token"]="06008024",["moduleMvid"]=typeof(SaveManager).Module.ModuleVersionId.ToString("D"),
                ["frame"]=UnityEngine.Time.frameCount,["number"]=number,["fileName"]=leaf,["sourceDescriptors"]=before,["outputInitiallyAbsent"]=true,["slotProjectionEnabled"]=false};
            beforeManualArrangement=Arrangement();evidence["beforeManualArrangement"]=beforeManualArrangement.DeepClone();
            manualProbe=new Chunk7ManualSeedSaveProbe(game,persistence,identities.Get,RequireWorld,leaf,Chunk7AssistedFixtureSelection.OutputName);manualProbe.Install();
            requestedManual=saveManager.CreateNewSave(Chunk7AssistedFixtureSelection.OutputName);
            Require(requestedManual!=null&&requestedManual.Type==SaveInfo.SaveType.Manual&&requestedManual.Name==Chunk7AssistedFixtureSelection.OutputName&&
                !requestedManual.IsActuallySaved&&!requestedManual.HasFileOnDisk&&!requestedManual.IsAutoLevelupSave&&ReferenceEquals(requestedManual.Area,preset.Area)&&saveManager.IsSaveAllowed(),
                "Native requested Manual is not the exact fresh descriptor.");
            manualProbe.Expect(requestedManual);manualRequested=true;stage=4;manualDispatching=true;
            try{game.SaveGame(requestedManual,()=>{manualCallbacks++;manualCallback=true;manualProbe.ObserveCallback();});}
            finally{manualDispatching=false;}
        }
        private void PollManualSave()
        {
            if(!manualCallback||Busy())return;RequireAuthority(false);RequireNoOwners();RequireWorld();RequireRoster(true);RequireAcquiredHorse();
            var expected=2+authority.AssistedLevelupWrites;var completion=persistence.LastCompletedSave;
            Require(manualCallbacks==1&&manualProbe!=null&&manualProbe.Complete&&completion!=null&&completion.Sequence==expected&&
                ReferenceEquals(completion.Requested,requestedManual)&&ReferenceEquals(completion.Written,manualProbe.Written)&&
                completion.Path==Path.Combine(authority.Root,authority.AssistedManualLeaf)&&persistence.SnapshotCount==expected&&persistence.CompletedSaveCount==expected&&
                authority.AdmittedNativeWriteRequests==expected&&authorization.AuthorizedWriteCount==expected,
                "Final assisted Manual lacks exact callback, worker, descriptor or cumulative completion.");
            var written=completion.Written;
            Require(written.Type==SaveInfo.SaveType.Manual&&!written.IsAutoLevelupSave&&written.Name==Chunk7AssistedFixtureSelection.OutputName&&
                written.FileName==authority.AssistedManualLeaf&&written.GameId==campaignId&&written.GameName==campaignName&&ReferenceEquals(written.Area,preset.Area)&&
                written.OperationState==SaveInfo.StateType.None&&written.IsActuallySaved&&written.HasFileOnDisk,"Final native Manual descriptor differs.");
            RequireDescriptorInventory(initialAuto,true);var after=Arrangement();
            Require(JToken.DeepEquals(beforeManualArrangement,after),"Native save-only boundary changed admitted XP/money/actor/class state.");
            evidence["afterManualArrangement"]=after;evidence["settledDescriptors"]=DescriptorInventory();
            var row=CaptureWrite("final-manual",requestedManual,written,expected);nativeWrites.Add(row);
            evidence["finalManual"]=row.DeepClone();authority.ConfirmAssistedManual(request,game,saveManager,Target(written));
            evidence["savedWorld"]=Snapshot(true);manualProven=true;Pass("C7-assisted-manual");BeginCleanup();
        }
        private void BeginCleanup(){if(cleanup)return;cleanup=true;cleanupStart=clock.Elapsed.TotalSeconds;}
        private void PollCleanup()
        {
            try
            {
                if(completed)return;RequireAuthority(true);
                if(dispatched&&!callbackReturned||probe?.InSynchronousCallback==true||ordinaryProbe?.InSynchronousCallback==true||manualProbe?.InCallback==true||manualDispatching||
                    Busy()||persistence.DrainForTeardown(0)==OwnedWorkerTeardownVerdict.Refused){Retain("Native callback/loading/worker retains assisted custody.");return;}
                RequireNoOwners();if(!DrainBuildCustody()){Retain("Owned builder cancellation retains assisted custody.");return;}
                if(errors.Count==0)Require(manualProven,"Assisted setup cannot complete before its final native Manual proof.");
                if(binding!=null&&evidence["setupAtCleanup"]==null)evidence["setupAtCleanup"]=Chunk7AssistedFixtureEvidence.Capture(binding,story,shortfall);
                if(manualProbe!=null&&evidence["manualProbe"]==null)evidence["manualProbe"]=manualProbe.Capture();
                if(ordinaryProbe!=null&&evidence["ordinaryProbe"]==null)evidence["ordinaryProbe"]=ordinaryProbe.Capture();
                if(probe!=null&&evidence["bootstrapProbe"]==null)evidence["bootstrapProbe"]=probe.Capture();
                if(main!=null&&evidence["settledWorld"]==null)evidence["settledWorld"]=Snapshot(true);
                Exception failure=null;
                try{manualProbe?.Dispose();}catch(Exception e){failure=e;Fail("C7-assisted-cleanup",e.ToString());}
                try{ordinaryProbe?.Dispose();}catch(Exception e){failure=failure??e;Fail("C7-assisted-cleanup",e.ToString());}
                try{probe?.Dispose();}catch(Exception e){failure=failure??e;Fail("C7-assisted-cleanup",e.ToString());}
                if(failure!=null)throw new InvalidOperationException("Assisted observers retain exact cleanup/identity custody.",failure);
                RestorePresentation();
                if(slotOwned){Require(NativePersistenceIsolation.NativeSlotRotationEnabled,"Owned slot projection changed externally.");if(!priorSlot)NativePersistenceIsolation.DisableNativeSlotRotation();slotOwned=false;}
                authority?.CloseUnusedBootstrapWindow();
                if(nativeGateOwned){authority.CloseAssistedNativeGate(this);nativeGateOwned=false;}
                if(!identities.Released){evidence["identityRegistry"]=new JObject{["capacity"]=identities.Capacity,["faultCount"]=identities.FaultCount,["retainedIds"]=new JArray(identities.Ids)};identities.Dispose();}
                story?.Dispose();
                evidence["cleanup"]=new JObject{["custodySettled"]=true,["callbackEntered"]=callbackEntered,["callbackReturned"]=callbackReturned,["dispatched"]=dispatched,
                    ["nativeWorkerSettled"]=!Busy(),["observersReleased"]=true,["identityRegistryReleased"]=identities.Released,["nativeGateReleased"]=!nativeGateOwned,
                    ["slotProjectionBefore"]=priorSlot,["slotProjectionAfter"]=NativePersistenceIsolation.NativeSlotRotationEnabled,["presentationRestored"]=!presentationCaptured,
                    ["manualRefunds"]=0,["manualFactRemovals"]=0,["manualCompanionRemovals"]=0,["authority"]=AuthoritySnapshot()};
                binding=null;nativeBoundary=null;story=null;shortfall=null;
                settled=true;Pass("C7-assisted-cleanup");evidence["elapsedSeconds"]=clock.Elapsed.TotalSeconds;
                RuntimeLoaderInventory.WriteArtifact(request,EvidenceFileName,EvidenceKind,errors.Count==0?"PASS":"FAIL",evidence,errors);completed=true;
            }
            catch(Exception error){Fail("C7-assisted-cleanup",error.ToString());Retain(error.Message);}
        }
        private void Retain(string reason)
        {
            if(pending||clock.Elapsed.TotalSeconds-cleanupStart<=45)return;Fail("C7-assisted-cleanup",reason+" The deadline does not relinquish custody.");
            RuntimeLoaderInventory.WriteArtifact(request,PendingFileName,PendingKind,"FAIL",new JObject{["cleanupCustodyRetained"]=true,["cleanupPendingAtHostBoundary"]=true,
                ["reason"]=reason,["authority"]=AuthoritySnapshot()},errors);pending=true;
        }
        internal bool DrainForHost(){if(disposed)return true;if(!cleanup){Fail("C7-assisted-interrupted","Assisted fixture interrupted before selected terminal proof.");BeginCleanup();}if(!completed)PollCleanup();return completed&&settled;}
        public void Dispose(){if(disposed)return;if(!DrainForHost())throw new InvalidOperationException("Assisted native custody remains retained.");disposed=true;}
    }
}
