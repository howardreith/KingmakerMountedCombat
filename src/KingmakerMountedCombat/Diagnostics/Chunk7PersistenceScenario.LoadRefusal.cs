using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Kingmaker;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.UI.Selection;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Chunk7PersistenceScenario
    {
        private bool OwnerSeedLoadRefusalCase => request.OwnedPairs?.IsOwnerSeedLoadRefusal == true;
        private Chunk7OwnerSeedLoadProbe loadRefusalObserver;
        private RuntimeSaveAuthorization.OwnerSeedLoadRefusalProbeLease loadRefusalLease;
        private readonly RetainedObjectIdentity loadRefusalIdentities = new RetainedObjectIdentity(128);
        private SaveInfo loadRefusalPrimary, loadRefusalAlternate;
        private JObject loadRefusal, loadRefusalBaseline;
        private int loadRefusalRefreshFrame, loadRefusalRejectedBefore;
        private string loadRefusalReceiptPath;
        private long loadRefusalReceiptLength;

        private void RequireLoadRefusalAuthority()
        {
            Require(OwnerSeedLoadRefusalCase && Cold && inherited?.Bound == true && loadRefusalObserver?.Installed == true &&
                ReferenceEquals(beforeLoadGame,Game.Instance) && nativeScope != null && replayAuthorization != null &&
                replayAuthorization.AuthorityGeneration == replayAuthorityGeneration &&
                replayAuthorization.IsExactPersistenceScope(replaySaveAuthority) && NativePersistenceIsolation.IsExactAuthority(replaySaveAuthority) &&
                replaySaveAuthority.MatchesOwnerSeedLoadRefusalRequest(request) &&
                replayAuthorization.AuthorizedLoadCount == 1 && replayAuthorization.AuthorizedWriteCount == 0 &&
                replayAuthorization.UnauthorizedLoadCount == 0 && replayAuthorization.UnauthorizedWriteCount == 0 &&
                replayAuthorization.FatalViolationCount == 0 && !LoadingProcess.Instance.IsLoadingInProcess &&
                !LoadingProcess.Instance.QueuedNames.Any() && !persistence.LoadInFlight && !NativePersistenceIsolation.HasPendingWrites &&
                !persistence.HasActiveSaveScope && !persistence.HasSaveAdmissionFence && !world.SerializationBusy &&
                !controls.HasUnsettledRelationshipTransition && !world.UnifiedTurn.HasUnsettledPreparation &&
                inherited.Party.All(Chunk7NativeFixture.Idle),
                "Load-refusal lost its exact settled original world and exhausted immutable authority.");
            RequireNativeWorld();inherited.RequirePreserved();AssertOwners();AssertSlots();
        }
        private void BeginLoadRefusal()
        {
            RequireLoadRefusalAuthority();
            Require(loadRefusal == null && saved?.SchemaVersion == 3 && saved.Pairs.Length == 2 && saved.Participations.Length == 0 && saved.Combat == null &&
                restoration != null && persistence.NativeWorldDisposalCount == 0 && persistence.SnapshotCount == 0,
                "Load-refusal requires the one accepted native restored exploration graph, once.");
            loadRefusalPrimary=Game.Instance.SaveManager.Single(value=>value.FileName==request.PersistenceLoad.FileName);
            loadRefusalAlternate=Game.Instance.SaveManager.Single(value=>value.FileName==request.PersistenceAlternate.FileName);
            loadRefusalReceiptPath=Path.Combine(Path.GetDirectoryName(replaySaveAuthority.Root),"owner-seed-load-refusal-derivation.json");
            var receiptFile=new FileInfo(loadRefusalReceiptPath);
            Require(receiptFile.Exists && receiptFile.Length>0 && receiptFile.Length<=16*1024*1024 &&
                (receiptFile.Attributes & FileAttributes.ReparsePoint)==0 && Hash(loadRefusalReceiptPath)==request.LoadRefusalDerivationSha256,
                "Load-refusal derivation receipt is absent, unsafe or changed.");
            loadRefusalReceiptLength=receiptFile.Length;
            var receipt=JObject.Parse(File.ReadAllText(loadRefusalReceiptPath));
            MountedArchiveReadResult primary, alternate;
            using(var reader=loadRefusalPrimary.Saver.Clone())primary=NativeMountedSaveStorage.ReadArchive(reader);
            using(var reader=loadRefusalAlternate.Saver.Clone())alternate=NativeMountedSaveStorage.ReadArchive(reader);
            Require(primary.Kind==MountedSaveReadKind.Current && primary.SourceSchema==3 && primary.HistoricalData==null &&
                JToken.DeepEquals(JObject.FromObject(primary.Data,MountedSaveCodec.CreateSerializer()),JObject.FromObject(saved,MountedSaveCodec.CreateSerializer())),
                "Load-refusal primary source differs from the actual loaded current-format graph.");
            ValidateLoadRefusalDerivation(receipt,request,saved,primary.OriginalJson,alternate.OriginalJson);
            Require(alternate.Kind==(request.PersistenceCase=="multi-malformed-refusal-manual"?MountedSaveReadKind.Invalid:MountedSaveReadKind.Future),
                "The immutable alternate does not reach the selected actual metadata decoder verdict.");
            VerifyLoadRefusalInputs();
            var pauseBefore=Game.Instance.IsPaused;Game.Instance.IsPaused=true;
            loadRefusalRejectedBefore=persistence.RejectedLoadCount;
            loadRefusalBaseline=CaptureLoadRefusalState("refusal-baseline");
            loadRefusal=new JObject { ["contract"]="owner-seed-real-entry-load-refusal-v1",["derivationSha256"]=request.LoadRefusalDerivationSha256,
                ["derivation"]=receipt,["fixturePauseBefore"]=pauseBefore,["baseline"]=loadRefusalBaseline,["entries"]=new JArray(),
                ["alternateReadKind"]=alternate.Kind.ToString(),["complete"]=false };
            evidence["loadRefusal"]=loadRefusal;
            loadRefusalLease=replayAuthorization.BeginOwnerSeedLoadRefusalProbe(request,replaySaveAuthority);
        }
        private bool UpdateLoadRefusal()
        {
            if(!OwnerSeedLoadRefusalCase || stage!=80 && stage!=81)return false;
            RequireLoadRefusalAuthority();VerifyLoadRefusalInputs();
            if(stage==80)
            {
                var names=new[]{"Game.LoadGameFromMainMenu","Game.LoadGame","Game.LoadGameForSmokeTest"};
                for(var index=0;index<names.Length;index++)
                {
                    var before=CaptureLoadRefusalState("entry-before");
                    using(loadRefusalLease.Arm(names[index]))
                    {
                        if(index==0)Game.Instance.LoadGameFromMainMenu(loadRefusalAlternate);
                        else if(index==1)Game.Instance.LoadGame(loadRefusalAlternate);
                        else Game.Instance.LoadGameForSmokeTest(loadRefusalAlternate);
                    }
                    var after=CaptureLoadRefusalState("entry-after");
                    ((JArray)loadRefusal["entries"]).Add(new JObject { ["entry"]=names[index],["before"]=before,["after"]=after,
                        ["rejectedDelta"]=persistence.RejectedLoadCount-loadRefusalRejectedBefore,
                        ["earlyIdentityCount"]=loadRefusalLease.EarlyIdentityCount,["realRejectedDecisionCount"]=loadRefusalLease.RejectedDecisionCount });
                    Require(loadRefusalLease.EarlyIdentityCount==index+1 && loadRefusalLease.RejectedDecisionCount==index+1 &&
                        loadRefusalLease.UnexpectedAcceptedCount==0 && loadRefusalLease.IncompleteEntryCount==0 &&
                        persistence.RejectedLoadCount==loadRefusalRejectedBefore+index+1,
                        "A real early native entry did not deliver one actual mounted metadata refusal before disposal.");
                    RequireLoadRefusalAuthority();ValidateLoadRefusalPreservedState(loadRefusalBaseline,after);VerifyLoadRefusalInputs();
                }
                loadRefusalLease.Dispose();loadRefusalLease=null;
                loadRefusalRefreshFrame=Time.frameCount;
                loadRefusal["menuRefresh"]=new JObject { ["before"]=CaptureLoadRefusalState("menu-refresh-before"),["dispatchFrame"]=loadRefusalRefreshFrame };
                Game.Instance.SaveManager.UpdateSaveListAsync();Next(81);return true;
            }
            if(!Game.Instance.SaveManager.AreSavesUpToDate || Time.frameCount<loadRefusalRefreshFrame+3)return true;
            var final=CaptureLoadRefusalState("menu-refresh-complete");ValidateLoadRefusalPreservedState(loadRefusalBaseline,final);
            Require(persistence.RejectedLoadCount==loadRefusalRejectedBefore+3 && Game.Instance.SaveManager.Count()==2 &&
                Game.Instance.SaveManager.Any(save=>save.FileName==request.PersistenceLoad.FileName) &&
                Game.Instance.SaveManager.Any(save=>save.FileName==request.PersistenceAlternate.FileName),
                "Native save-list refresh lost an immutable entry or retried a rejected load.");
            var refresh=(JObject)loadRefusal["menuRefresh"];refresh["after"]=final;refresh["completeFrame"]=Time.frameCount;refresh["areSavesUpToDate"]=true;
            loadRefusal["complete"]=true;Pass("C7-persistence-real-entry-load-refusal");Next(10);return true;
        }
        private JObject CaptureLoadRefusalState(string label)
        {
            var game=Game.Instance;var serializer=MountedSaveCodec.CreateSerializer();
            return new JObject { ["world"]=Capture(label),["paused"]=game.IsPaused,["selection"]=new JArray(SelectionManager.Instance.SelectedUnits.Select(value=>value.UniqueId)),
                ["objects"]=new JObject { ["player"]=loadRefusalIdentities.Get(game.Player),["state"]=loadRefusalIdentities.Get(game.State),
                    ["areaState"]=loadRefusalIdentities.Get(game.State.LoadedAreaState),["area"]=loadRefusalIdentities.Get(game.CurrentlyLoadedArea),
                    ["loadedArchive"]=loadRefusalIdentities.Get(persistence.LoadedArchive),["actors"]=new JArray(actors.Select(value=>new JObject{["id"]=value.UniqueId,["object"]=loadRefusalIdentities.Get(value)})) },
                ["loadedArchive"]=JObject.FromObject(persistence.LoadedArchive,serializer),["slots"]=JArray.FromObject(controls.CapturePersistentSlots(),serializer),
                ["party"]=new JArray(inherited.Party.OrderBy(value=>value.UniqueId,StringComparer.Ordinal).Select(value=>new JObject{["native"]=JObject.FromObject(MountedPersistenceService.CaptureActor(value),serializer),["prepared"]=value.CombatState.Prepared,["inCombat"]=value.IsInCombat})),
                ["nativePrepares"]=new JArray(actors.Select(value=>new JObject { ["actorId"]=value.UniqueId,["count"]=restoration.Count("prepare-before",new[]{value.UniqueId}) })),
                ["rejectedLoads"]=persistence.RejectedLoadCount,["worldDisposals"]=persistence.NativeWorldDisposalCount,
                ["nativeLoads"]=replaySaveAuthority.AdmittedNativeLoadRequests,["nativeWrites"]=replaySaveAuthority.AdmittedNativeWriteRequests,
                ["authorizedLoads"]=replayAuthorization.AuthorizedLoadCount,["authorizedWrites"]=replayAuthorization.AuthorizedWriteCount,
                ["unauthorizedLoads"]=replayAuthorization.UnauthorizedLoadCount,["unauthorizedWrites"]=replayAuthorization.UnauthorizedWriteCount,
                ["fatalViolations"]=replayAuthorization.FatalViolationCount,["loadProbe"]=loadRefusalObserver.Capture() };
        }
        internal static void ValidateLoadRefusalPreservedState(JObject before,JObject after)
        {
            Require(before!=null && after!=null,"Load-refusal state observation is missing.");
            foreach(var value in new[]{before,after})
            {
                Require(value["paused"]?.Type==JTokenType.Boolean && (bool)value["paused"] && value["world"] is JObject &&
                    value["objects"] is JObject && value["loadedArchive"] is JObject && value["selection"] is JArray &&
                    value["slots"] is JArray && value["party"] is JArray party && party.Count>=4 && party.Count<=12 &&
                    value["nativePrepares"] is JArray prepares && prepares.Count==4 && value["loadProbe"] is JObject,
                    "Load-refusal conservation observation has an incomplete native state shape.");
                foreach(var name in new[]{"nativeLoads","authorizedLoads"})Require(value[name]?.Type==JTokenType.Integer && (int)value[name]==1,"Load-refusal lost its sole native load.");
                foreach(var name in new[]{"worldDisposals","nativeWrites","authorizedWrites","unauthorizedLoads","unauthorizedWrites","fatalViolations"})
                    Require(value[name]?.Type==JTokenType.Integer && (int)value[name]==0,"Load-refusal admitted a forbidden native disposal/write/request.");
            }
            foreach(var name in new[]{"paused","selection","objects","loadedArchive","slots","party","nativePrepares","worldDisposals","nativeLoads","nativeWrites","authorizedLoads","authorizedWrites","unauthorizedLoads","unauthorizedWrites","fatalViolations"})
                Require(JToken.DeepEquals(before[name],after[name]),"Load refusal or native menu refresh changed "+name+".");
            var a=(JObject)before["world"].DeepClone();var b=(JObject)after["world"].DeepClone();a.Remove("label");a.Remove("frame");b.Remove("label");b.Remove("frame");
            Require(JToken.DeepEquals(a,b),"Load refusal or native menu refresh changed the actual mounted native world.");
            foreach(var name in new[]{"blueprintFailureCount","dropped","failedTypeBindCount","loggedExceptionCount","observerFaultCount"})
                Require(before["loadProbe"]?[name]?.Type==JTokenType.Integer && JToken.DeepEquals(before["loadProbe"][name],after["loadProbe"]?[name]),
                    "Load refusal/menu refresh produced a new native resolution/exception/observer failure.");
        }
        private void VerifyLoadRefusalInputs()
        {
            foreach(var descriptor in new[]{request.PersistenceLoad,request.PersistenceAlternate})
            {
                var file=new FileInfo(Path.Combine(replaySaveAuthority.Root,descriptor.FileName));
                Require(file.Exists && (file.Attributes & FileAttributes.ReparsePoint)==0 && file.Length==descriptor.Length &&
                    file.LastWriteTimeUtc.Ticks==descriptor.LastWriteTimeUtcTicks && Hash(file.FullName)==descriptor.Sha256,
                    "Load-refusal immutable source or alternate bytes/timestamp changed.");
            }
            var receipt=new FileInfo(loadRefusalReceiptPath);
            Require(receipt.Exists && receipt.Length==loadRefusalReceiptLength && (receipt.Attributes & FileAttributes.ReparsePoint)==0 &&
                Hash(receipt.FullName)==request.LoadRefusalDerivationSha256,"Load-refusal derivation receipt changed.");
        }
        private void EndLoadRefusalObservation()
        {
            loadRefusalLease?.Dispose();loadRefusalLease=null;
            if(loadRefusalObserver!=null){if(loadRefusal!=null)loadRefusal["finalProbe"]=loadRefusalObserver.Capture();loadRefusalObserver.Dispose();loadRefusalObserver=null;}
            if(loadRefusalReceiptPath!=null)VerifyLoadRefusalInputs();
            loadRefusalIdentities.Dispose();
        }

        internal static void ValidateLoadRefusalDerivation(JObject receipt,RuntimeRequest request,MountedArchiveData loaded,string sourceMetadata,string alternateMetadata)
        {
            Require(receipt!=null && request?.OwnedPairs?.IsOwnerSeedLoadRefusal==true && request.Validate().Count==0 && loaded!=null &&
                (string)receipt["contract"]=="owner-seed-load-refusal-metadata-copy-v1" && (string)receipt["checkpoint"]==request.PersistenceCase &&
                (string)receipt["sourceRunId"]==request.OwnedPairs.SourceRunId && (string)receipt["sourceArtifactSha256"]==request.OwnedPairs.SourceArtifactSha256 &&
                receipt["fixtureSetupOnly"]?.Type==JTokenType.Boolean && (bool)receipt["fixtureSetupOnly"] &&
                receipt["nativeRestorationProven"]?.Type==JTokenType.Boolean && !(bool)receipt["nativeRestorationProven"] &&
                JToken.DeepEquals(receipt["sourceDescriptor"],LoadRefusalDescriptor(request.PersistenceLoad)) &&
                JToken.DeepEquals(receipt["descriptor"],LoadRefusalDescriptor(request.PersistenceAlternate)),
                "Load-refusal derivation lacks its exact disclosed immutable source/alternate lineage.");
            loaded.Validate();
            var source=MountedArchiveCodec.Decode(sourceMetadata);
            Require(source.Kind==MountedSaveReadKind.Current && source.SourceSchema==3 && source.HistoricalData==null &&
                loaded.Pairs.Length==2 && loaded.Participations.Length==0 && loaded.Combat==null &&
                loaded.CampaignId==request.PersistenceLoad.GameId && loaded.AreaId==request.PersistenceLoad.Area &&
                loaded.Pairs.Count(pair=>pair.Rider.Id==request.OwnedPairs.RiderAId && pair.Mount.Id==request.OwnedPairs.MountAId && pair.ProfileId=="medium-humanoid-horse-v1")==1 &&
                loaded.Pairs.Count(pair=>pair.Rider.Id==request.OwnedPairs.RiderBId && pair.Mount.Id==request.OwnedPairs.MountBId && pair.ProfileId=="medium-humanoid-horse-v1")==1 &&
                JToken.DeepEquals(JObject.FromObject(source.Data,MountedSaveCodec.CreateSerializer()),JObject.FromObject(loaded,MountedSaveCodec.CreateSerializer())),
                "Load-refusal derivation source is not the complete actual restored exploration graph.");
            var malformed=request.PersistenceCase=="multi-malformed-refusal-manual";
            Require((string)receipt["mutation"]==(malformed?"truncated-json":"future-schema-99") && alternateMetadata!=null &&
                receipt["metadataLength"]?.Type==JTokenType.Integer && (long)receipt["metadataLength"]==Encoding.UTF8.GetByteCount(alternateMetadata) &&
                (string)receipt["metadataSha256"]==LoadRefusalTextHash(alternateMetadata),"Load-refusal metadata bytes are not the exact declared mutation.");
            if(malformed)Require(alternateMetadata=="{\"SchemaVersion\":3,","Malformed refusal admits only the exact disclosed truncated metadata text.");
            else {var future=JObject.Parse(sourceMetadata);future["SchemaVersion"]=99;Require(JToken.DeepEquals(future,JObject.Parse(alternateMetadata)),"Future refusal changed more than SchemaVersion.");}
            var sourceMembers=receipt["sourceMembers"] as JArray;var members=receipt["members"] as JArray;
            Require(sourceMembers!=null && members!=null && sourceMembers.Count>=2 && sourceMembers.Count<=1024 && members.Count==sourceMembers.Count &&
                receipt["nativeMembersVerified"]?.Type==JTokenType.Integer && (int)receipt["nativeMembersVerified"]==members.Count-1,
                "Load-refusal native member inventory is missing or unbounded.");
            foreach(var rows in new[]{sourceMembers,members})
                Require(rows.All(row=>row is JObject && ((JObject)row).Properties().Count()==3 && row["name"]?.Type==JTokenType.String &&
                    !string.IsNullOrEmpty((string)row["name"]) && ((string)row["name"]).Length<=256 && row["length"]?.Type==JTokenType.Integer &&
                    (long)row["length"]>=0 && (long)row["length"]<=268435456 && row["sha256"]?.Type==JTokenType.String && RuntimeRequest.IsLowerHex((string)row["sha256"],64)) &&
                    rows.Select(row=>(string)row["name"]).Distinct(StringComparer.OrdinalIgnoreCase).Count()==rows.Count &&
                    rows.Count(row=>(string)row["name"]=="header.json")==1 && rows.Count(row=>(string)row["name"]==MountedSaveData.ArchiveMember)==1,
                    "Load-refusal member row is malformed, duplicated or omits its native header/metadata.");
            foreach(var row in sourceMembers)
            {
                var other=members.SingleOrDefault(value=>(string)value["name"]==(string)row["name"]);
                Require(other!=null && ((string)row["name"]==MountedSaveData.ArchiveMember || JToken.DeepEquals(row,other)),"Load-refusal derivative changed a native member.");
            }
            var sourceMember=sourceMembers.Single(row=>(string)row["name"]==MountedSaveData.ArchiveMember);
            var member=members.Single(row=>(string)row["name"]==MountedSaveData.ArchiveMember);
            Require((string)sourceMember["sha256"]==LoadRefusalTextHash(sourceMetadata) && (long)sourceMember["length"]==Encoding.UTF8.GetByteCount(sourceMetadata) &&
                (string)member["sha256"]==LoadRefusalTextHash(alternateMetadata) && (long)member["length"]==Encoding.UTF8.GetByteCount(alternateMetadata),
                "Load-refusal member receipt does not bind the actual source/alternate metadata bytes.");
        }
        private static JObject LoadRefusalDescriptor(RuntimeSaveDescriptor value) => value==null?null:new JObject {
            ["internalName"]=value.InternalName,["fileName"]=value.FileName,["sha256"]=value.Sha256,["length"]=value.Length,
            ["lastWriteTimeUtcTicks"]=value.LastWriteTimeUtcTicks,["gameId"]=value.GameId,["gameName"]=value.GameName,["area"]=value.Area };
        private static string LoadRefusalTextHash(string value)
        {using(var algorithm=SHA256.Create())return BitConverter.ToString(algorithm.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-","").ToLowerInvariant();}
    }
}
