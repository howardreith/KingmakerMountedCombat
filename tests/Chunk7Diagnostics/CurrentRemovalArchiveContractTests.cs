using System;
using System.IO;
using System.Linq;
using System.Reflection;
using KingmakerMountedCombat.Diagnostics;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;

internal static class CurrentRemovalArchiveContractTests
{
    private const string Campaign="aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",Area="bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
    private const string Rider="11111111-1111-4111-8111-111111111111",Mount="22222222-2222-4222-8222-222222222222";
    private const string OtherRider="33333333-3333-4333-8333-333333333333",OtherMount="44444444-4444-4444-8444-444444444444";
    private static int passed,failed;
    private static SavedNativeActor Actor(string id) => new SavedNativeActor { Id=id,Standard=1.25f,Move=2.125f,Swift=3.25f,Initiative=0.5f,Reaction=4.5f,ReactionsRemaining=2,LastSurpriseTicks=17 };
    private static MountedArchiveData Opening() => new MountedArchiveData { SchemaVersion=3,CampaignId=Campaign,AreaId=Area,GameTimeTicks=10000000,Policy=MountedSaveData.PairedPolicy,RulesId=MountedSaveData.Rules,
        Pairs=new[]{new SavedMountedPair { PairId="cccccccccccc4ccc8ccccccccccccccc",Generation=7,ProfileId="medium-humanoid-mammoth-v1",Rider=Actor(Rider),Mount=Actor(Mount) }},
        Participations=new SavedPairParticipation[0],Slots=new[]{new SavedMountedSlot{ActorId=Rider,Index=11,Kind=2},new SavedMountedSlot{ActorId=Rider,Index=12,Kind=3},new SavedMountedSlot{ActorId=Rider,Index=13,Kind=4}} };
    private static MountedArchiveData Cleanup() { var value=Opening();value.Pairs=new SavedMountedPair[0];value.Slots=new SavedMountedSlot[0];return value; }
    private static MountedArchiveReadResult Read(MountedArchiveData value) => MountedArchiveCodec.Decode(MountedArchiveCodec.Encode(value));
    private static MountedArchiveReadResult Unchecked(MountedArchiveData value) => new MountedArchiveReadResult { Kind=MountedSaveReadKind.Current,SourceSchema=3,Data=value };
    private static RuntimePersistenceScenario.RemovalArchiveObservation Validate(MountedArchiveReadResult read,bool mounted=true) => RuntimePersistenceScenario.ValidateCurrentRemovalArchive(read,mounted,Campaign,Area,Rider,Mount);
    private static JObject Json(object value) => JObject.FromObject(value,MountedSaveCodec.CreateSerializer());
    private static void True(bool value,string message) { if(!value)throw new Exception(message); }
    private static void Check(string name,Action body) { try {body();passed++;Console.WriteLine("PASS "+name);}catch(Exception error){failed++;Console.WriteLine("FAIL "+name+": "+error);} }
    private static void Refuse(Action action) { var refused=false;try{action();}catch(InvalidOperationException){refused=true;}catch(InvalidDataException){refused=true;}if(!refused)throw new Exception("Actual validator admitted an invalid current removal graph."); }
    private static void AddCombat(MountedArchiveData archive,bool pending)
    {
        var pair=archive.Pairs.Single();
        archive.Combat=new SavedCombatData { Actors=new[]{pair.Rider,pair.Mount}.Select(actor=>new SavedCombatActor { Native=actor,InCombat=true,Prepared=true,AiActions=new SavedAiAction[0],DisengageTargets=new string[0] }).ToArray(),Roster=new SavedRosterActor[0],Engagements=new SavedEngagement[0],Allocations=new SavedMovementAllocation[0] };
        if(pending)archive.Participations=new[]{new SavedPairParticipation { OwnerId=pair.PairId,Generation=pair.Generation,MountedPairId=pair.PairId,State=new SavedPairedState { PendingSplitId=Mount,PendingSplitRound=3,SplitReleaseRound=2 } }};
        archive.Validate();
    }
    public static int Main(string[] arguments)
    {
        if(arguments.Length!=1)throw new ArgumentException("Installed Unity dependency path is required.");
        AppDomain.CurrentDomain.AssemblyResolve+=(sender,request)=>new AssemblyName(request.Name).Name=="UnityEngine"?Assembly.LoadFrom(arguments[0]):null;
        Check("actual current opening preserves native owner identity and every exact projected value without modifying input",()=>{
            var read=Read(Opening());var before=MountedArchiveCodec.Encode(read.Data);var result=Validate(read);var native=read.Data;var pair=native.Pairs.Single();
            var expected=new MountedSaveData {SchemaVersion=2,CampaignId=native.CampaignId,AreaId=native.AreaId,GameTimeTicks=native.GameTimeTicks,Policy=native.Policy,RulesId=native.RulesId,Mounted=true,ProfileId=pair.ProfileId,Rider=pair.Rider,Mount=pair.Mount,Slots=native.Slots,Combat=null};
            True(result.Kind==MountedSaveReadKind.Current&&result.SourceSchema==3&&result.Data.SchemaVersion==2,"Observation was labelled as a historical native input.");
            True(RuntimePersistenceScenario.RemovalArchiveObservation.ProjectionFormat=="schema3-single-pair-observation-v1","Projection lacks its explicit diagnostic label.");
            True(ReferenceEquals(result.NativeArchive,native)&&JToken.DeepEquals(Json(result.Data),Json(expected))&&Json(result.Data).Properties().Count()==12,"Projection discarded data or changed its exact twelve-property contract.");
            True(MountedArchiveCodec.Encode(native)==before&&native.Pairs.Single().PairId=="cccccccccccc4ccc8ccccccccccccccc"&&native.Pairs.Single().Generation==7,"Read-only observation changed the actual schema3 graph.");
        });
        Check("actual cleanup has zero pair participation and slot arrays plus null combat with complete current graph",()=>{var read=Read(Cleanup());var before=MountedArchiveCodec.Encode(read.Data);var result=Validate(read,false);True(!result.Data.Mounted&&result.Data.Rider==null&&result.Data.Mount==null&&result.Data.ProfileId==null&&result.Data.Combat==null&&result.Data.Slots.Length==0&&ReferenceEquals(result.NativeArchive,read.Data)&&MountedArchiveCodec.Encode(read.Data)==before,"Cleanup projection or native archive changed.");});
        Check("representable unrelated slot is preserved in full and projected observations rather than silently dropped",()=>{var archive=Opening();archive.Slots=archive.Slots.Concat(new[]{new SavedMountedSlot{ActorId=OtherRider,Index=19,Kind=1}}).ToArray();var read=Read(archive);var result=Validate(read);True(JToken.DeepEquals(JArray.FromObject(read.Data.Slots),JArray.FromObject(result.Data.Slots))&&result.Data.Slots.Length==4,"Projection dropped a native control reference.");});
        foreach(var kind in new[]{1,2,3,4}) {var selected=kind;Check("actual schema2 observation preserves representable current slot kind "+selected,()=>{var a=Opening();a.Slots[0].Kind=selected;True(Validate(Read(a)).Data.Slots[0].Kind==selected,"Current control reference changed.");});}
        Check("schema3 slot kind5 remains valid current data but refuses unrepresentable legacy observation",()=>{var a=Opening();a.Slots[0].Kind=5;var read=Read(a);True(read.Kind==MountedSaveReadKind.Current&&read.SourceSchema==3,"Fixture was not actual valid schema3 data.");Refuse(()=>Validate(read));True(a.Slots[0].Kind==5,"Refusal stripped the unrepresentable reference.");});
        Check("full valid second owner cannot hide behind a one-pair observation projection",()=>{var a=Opening();a.Pairs=a.Pairs.Concat(new[]{new SavedMountedPair{PairId="dddddddddddd4ddd8ddddddddddddddd",Generation=2,ProfileId="medium-humanoid-horse-v1",Rider=Actor(OtherRider),Mount=Actor(OtherMount)}}).ToArray();var read=Read(a);Refuse(()=>Validate(read));True(read.Data.Pairs.Length==2,"Refusal normalized away a foreign owner.");});
        Check("full valid retained participation cannot be dropped into a null-combat observation",()=>{var a=Opening();AddCombat(a,true);var read=Read(a);Refuse(()=>Validate(read));True(read.Data.Participations.Length==1&&read.Data.Combat!=null,"Refusal erased participation or combat.");});
        Check("valid shared combat without participation is also outside selected exploration opening",()=>{var a=Opening();AddCombat(a,false);Refuse(()=>Validate(Read(a)));});
        foreach(var fault in new[]{"null-read","missing","invalid","future","source1","source2","historical-data","null-data","schema2","schema4","foreign-campaign","foreign-area","foreign-rider","foreign-mount","swapped-roles","horse-profile","unknown-profile","null-pairs","null-participations","null-slots","null-pair","duplicate-slot","slots129","nan-debt","negative-generation"}) {var selected=fault;Check("actual current removal validator refuses "+selected,()=>{
            var a=Opening();var read=Unchecked(a);
            switch(selected){case "null-read":read=null;break;case "missing":read.Kind=MountedSaveReadKind.Missing;break;case "invalid":read.Kind=MountedSaveReadKind.Invalid;break;case "future":read.Kind=MountedSaveReadKind.Future;break;case "source1":read.SourceSchema=1;break;case "source2":read.SourceSchema=2;break;case "historical-data":read.HistoricalData=new MountedSaveData();break;case "null-data":read.Data=null;break;case "schema2":a.SchemaVersion=2;break;case "schema4":a.SchemaVersion=4;break;case "foreign-campaign":a.CampaignId="eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee";break;case "foreign-area":a.AreaId=new string('f',32);break;case "foreign-rider":a.Pairs[0].Rider.Id=OtherRider;break;case "foreign-mount":a.Pairs[0].Mount.Id=OtherMount;break;case "swapped-roles":a.Pairs[0].Rider.Id=Mount;a.Pairs[0].Mount.Id=Rider;break;case "horse-profile":a.Pairs[0].ProfileId="medium-humanoid-horse-v1";break;case "unknown-profile":a.Pairs[0].ProfileId="unknown";break;case "null-pairs":a.Pairs=null;break;case "null-participations":a.Participations=null;break;case "null-slots":a.Slots=null;break;case "null-pair":a.Pairs[0]=null;break;case "duplicate-slot":a.Slots=new[]{a.Slots[0],a.Slots[0]};break;case "slots129":a.Slots=Enumerable.Range(0,129).Select(index=>new SavedMountedSlot{ActorId=Rider,Index=index,Kind=2}).ToArray();break;case "nan-debt":a.Pairs[0].Rider.Move=float.NaN;break;case "negative-generation":a.Pairs[0].Generation=-1;break;}
            Refuse(()=>Validate(read));
        });}
        foreach(var fault in new[]{"pair","slot","combat","participation"}) {var selected=fault;Check("actual cleanup refuses leaked "+selected+" even if a flattened projection could omit it",()=>{var a=Cleanup();switch(selected){case "pair":a.Pairs=Opening().Pairs;break;case "slot":a.Slots=Opening().Slots;break;case "combat":var c=Opening();AddCombat(c,false);a.Combat=c.Combat;break;case "participation":var p=Opening();AddCombat(p,true);a.Combat=p.Combat;a.Participations=p.Participations;a.Participations[0].MountedPairId=null;break;}Refuse(()=>Validate(Read(a),false));});}
        Check("actual historical schema2 decode migrates graph but is refused as current-only source",()=>{var a=Opening();var pair=a.Pairs.Single();var old=new MountedSaveData{SchemaVersion=2,CampaignId=Campaign,AreaId=Area,GameTimeTicks=a.GameTimeTicks,Policy=a.Policy,RulesId=a.RulesId,Mounted=true,ProfileId=pair.ProfileId,Rider=pair.Rider,Mount=pair.Mount,Slots=a.Slots};var read=MountedArchiveCodec.Decode(MountedSaveCodec.Encode(old));True(read.Kind==MountedSaveReadKind.Current&&read.SourceSchema==2&&read.HistoricalData!=null&&read.Data.SchemaVersion==3,"Historical fixture did not migrate through actual loader.");Refuse(()=>Validate(read));});
        Check("actual historical schema1 decode is also refused without relabelling its native source",()=>{var a=Opening();var pair=a.Pairs.Single();var old=new MountedSaveData{SchemaVersion=2,CampaignId=Campaign,AreaId=Area,GameTimeTicks=a.GameTimeTicks,Policy=a.Policy,RulesId=a.RulesId,Mounted=true,ProfileId=pair.ProfileId,Rider=pair.Rider,Mount=pair.Mount,Slots=a.Slots};var wire=Json(old);wire["SchemaVersion"]=1;wire.Remove("Combat");var read=MountedArchiveCodec.Decode(wire.ToString());True(read.Kind==MountedSaveReadKind.Current&&read.SourceSchema==1&&read.HistoricalData!=null&&read.Data.SchemaVersion==3,"Historical schema1 fixture did not migrate through actual loader.");Refuse(()=>Validate(read));});
        Check("actual malformed current wire stays invalid and cannot enter diagnostic projection",()=>{var wire=Json(Opening());wire["UnknownOwnershipReference"]="must-not-drop";var read=MountedArchiveCodec.Decode(wire.ToString());True(read.Kind==MountedSaveReadKind.Invalid&&read.Data==null,"Malformed current wire was normalized.");Refuse(()=>Validate(read));});
        Check("actual future wire stays preserved future data and cannot enter diagnostic projection",()=>{var wire=Json(Opening());wire["SchemaVersion"]=4;var json=wire.ToString();var read=MountedArchiveCodec.Decode(json);True(read.Kind==MountedSaveReadKind.Future&&read.OriginalJson==json&&read.Data==null,"Future data was modified or normalized.");Refuse(()=>Validate(read));});
        Check("observation never changes selected writer schema or native graph serialization",()=>{var read=Read(Opening());var before=MountedArchiveCodec.Encode(read.Data);Validate(read);True(NativeMountedSaveFormat.SelectedWriterSchema==3&&NativeMountedSaveFormat.CaptureOrPreserve(read,()=>read.Data)==before,"Readback changed writer selection or native format.");});
        Console.WriteLine("TOTAL PASS="+passed+" FAIL="+failed);return failed==0?0:1;
    }
}
