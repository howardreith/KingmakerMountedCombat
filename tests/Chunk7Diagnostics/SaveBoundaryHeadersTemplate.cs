using System;using System.Linq;using System.Collections.Generic;using Newtonsoft.Json.Linq;
namespace KingmakerMountedCombat.Diagnostics {
class UnitEntityData {internal string UniqueId=Guid.NewGuid().ToString();}class MountedPersistenceService{}static class Time{internal static int frameCount;}
class Chunk7SaveBoundaryProbe {
private readonly MountedPersistenceService persistence;private readonly UnitEntityData[] actors;private readonly Func<string,JObject> capture;private readonly JArray events=new JArray(),hooks=new JArray();private readonly List<string> errors=new List<string>();internal bool installed=true;private int inside,starts,stages;private readonly int expectedSnapshots;
__METHODS__
internal static Chunk7SaveBoundaryProbe New(int count=2)=>new Chunk7SaveBoundaryProbe(new MountedPersistenceService(),Enumerable.Range(0,4).Select(i=>new UnitEntityData()).ToArray(),kind=>new JObject{["kind"]=kind},count);
internal void Start()=>Starting();internal void Stage()=>Staged();internal void Entry(string kind,bool own)=>Native(kind,own?actors[0]:new UnitEntityData());
}
class HeaderTests {static int pass,fail;static void Check(string name,Action act){try{act();pass++;Console.WriteLine("PASS "+name);}catch(Exception e){fail++;Console.WriteLine("FAIL "+name+" "+e.Message);}}static void Need(bool ok){if(!ok)throw new Exception("Actual header contract failed.");}static void Pair(Chunk7SaveBoundaryProbe p){p.Start();p.Stage();}static void Refuse(Action a){try{a();}catch(InvalidOperationException){return;}throw new Exception("Expected refusal.");}
static int Main(){
Check("one ordinary header remains complete",()=>{var p=Chunk7SaveBoundaryProbe.New(1);Pair(p);Need(p.Complete&&p.HeadersComplete(1)&&(int)p.Capture()["expectedSnapshots"]==1);});
Check("seed alone cannot complete the two-operation case",()=>{var p=Chunk7SaveBoundaryProbe.New();Pair(p);Need(!p.Complete&&p.HeadersComplete(1));});
Check("two ordered headers complete without reinstall/reset",()=>{var p=Chunk7SaveBoundaryProbe.New();Pair(p);Pair(p);var x=p.Capture();Need(p.Complete&&!p.HeadersComplete(1)&&p.HeadersComplete(2)&&(int)x["starts"]==2&&(int)x["stages"]==2&&((JArray)x["events"]).Count==4);});
Check("second header before its snapshot remains incomplete",()=>{var p=Chunk7SaveBoundaryProbe.New();Pair(p);p.Start();Need(!p.Complete&&!p.HeadersComplete(1)&&!p.HeadersComplete(2));});
foreach(var order in new[]{"SS","T","STT","STSTS","STSTST"}){var value=order;Check("refuses unordered or extra header "+value,()=>{var p=Chunk7SaveBoundaryProbe.New();foreach(var c in value){if(c=='S')p.Start();else p.Stage();}Need(!p.Complete&&((JArray)p.Capture()["errors"]).Count>0);});}
foreach(var kind in new[]{"prepare-before","round-before"}){var value=kind;Check("forbidden native entry invalidates both completed headers "+value,()=>{var p=Chunk7SaveBoundaryProbe.New();Pair(p);p.Entry(value,true);Pair(p);Need(!p.Complete&&!p.HeadersComplete(2));});}
Check("foreign actor callback is not borrowed into selected proof",()=>{var p=Chunk7SaveBoundaryProbe.New();Pair(p);p.Entry("prepare-before",false);Pair(p);Need(p.Complete&&((JArray)p.Capture()["events"]).Count==4);});
Check("observer must remain installed",()=>{var p=Chunk7SaveBoundaryProbe.New();Pair(p);Pair(p);p.installed=false;Need(!p.Complete);});
Check("no arbitrary zero or third-operation allowance",()=>{Refuse(()=>Chunk7SaveBoundaryProbe.New(0));Refuse(()=>Chunk7SaveBoundaryProbe.New(3));var p=Chunk7SaveBoundaryProbe.New();Need(!p.HeadersComplete(0)&&!p.HeadersComplete(3));});
Check("capture failure is retained and never called complete",()=>{var p=new Chunk7SaveBoundaryProbe(new MountedPersistenceService(),Enumerable.Range(0,4).Select(i=>new UnitEntityData()).ToArray(),kind=>{throw new Exception("capture failed");},2);Pair(p);Pair(p);Need(!p.Complete&&((JArray)p.Capture()["errors"]).Count>=1);});
Console.WriteLine("SAVE BOUNDARY HEADER METHODS PASS="+pass+" FAIL="+fail+"; native observer installation/actors substituted");return fail==0?0:1;}}
}
