using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Kingmaker;
using UnityEngine;
namespace Kingmaker { sealed class Game { public KingmakerMountedCombat.Diagnostics.DialogController DialogController;public KingmakerMountedCombat.Diagnostics.GameModeType CurrentMode;public bool CutsceneLock;public bool IsModeActive(KingmakerMountedCombat.Diagnostics.GameModeType mode){return CurrentMode==mode;} } }
namespace UnityEngine { static class Time { internal static int frameCount; } class Object { internal bool Destroyed;public static bool operator==(Object a,Object b){bool an=ReferenceEquals(a,null)||a.Destroyed,bn=ReferenceEquals(b,null)||b.Destroyed;return an&&bn||!an&&!bn&&ReferenceEquals(a,b);}public static bool operator!=(Object a,Object b){return !(a==b);}public override bool Equals(object value){return ReferenceEquals(this,value);}public override int GetHashCode(){return base.GetHashCode();} } }
namespace KingmakerMountedCombat.Diagnostics {
enum GameModeType { Default,Dialog }
class BlueprintScriptableObject:UnityEngine.Object { public string AssetGuidThreadSafe; }
class BlueprintDialog:BlueprintScriptableObject {}
class BlueprintCueBase:BlueprintScriptableObject {}
class BlueprintCue:BlueprintCueBase {}
class DialogController { public BlueprintDialog Dialog;public BlueprintCue m_CurrentCue;public BlueprintCueBase m_CueToPlay,m_ContinueCue;public bool m_CuePlayScheduled,m_DialogStopScheduled; }
class DialogMessageBoxBase { public enum BoxType { TestValue=3 } }
class DialogMessageBox:UnityEngine.Object { public static DialogMessageBox Instance;public DialogMessageBoxBase.BoxType m_BoxType;internal bool Shown,Throw;internal int Reads;public bool IsShown { get { Reads++;if(Throw)throw new InvalidOperationException("popup read failed");return Shown; } } }
sealed class RuntimeFixtureIdentity {}
sealed class RuntimeSubscenarioResult {}
internal sealed class Chunk7BootstrapDialogueObservation:IDisposable {
__ACTUAL_RECORDER__
 private static RawState ReadNative(Game game){return NativeRead.UseActual?ReadActualNative(game):NativeRead.Read(game);}
 // Actual native read body below; module/field access and Unity semantics are
 // explicit external facades. This does not execute an installed native getter.
__ACTUAL_NATIVE_READ__
__ACTUAL_GUID_READ__
 private static void RequireModule(Type type){}
 private static T ReadField<T>(Type type,object instance,string name,int token){return (T)type.GetField(name,BindingFlags.Public|BindingFlags.Instance|BindingFlags.Static|BindingFlags.DeclaredOnly).GetValue(instance);}
__ACTUAL_RAW_STATE__
}
static class NativeRead {
 internal static bool UseActual;internal static Chunk7BootstrapDialogueObservation.RawState State;internal static int Reads;internal static Exception Error;
 internal static Chunk7BootstrapDialogueObservation.RawState Read(Game game){Reads++;if(Error!=null)throw Error;return State;}
}
static class DtoSerialization {
__ACTUAL_JSON_SETTINGS__
__ACTUAL_RESULT_DTO__
 internal static JObject Capture(object value){return JObject.Parse(JsonConvert.SerializeObject(new RuntimeGameResultV2{Scenario="chunk7-native-main-bootstrap",Status="FAIL",CurrentGameMode="Dialog",BootstrapDialogue=value},JsonSettings));}
}
static class Tests {
 static int passed,failed;
 static void Check(string label,bool valid){Console.WriteLine((valid?"PASS ":"FAIL ")+label);if(valid)passed++;else failed++;}
 static Chunk7BootstrapDialogueObservation.RawState State(Game game=null){return new Chunk7BootstrapDialogueObservation.RawState{
  Game=game??new Game(),Controller=new object(),TopMode="Default",DialogActive=false,CutsceneLock=false,
  Dialog=new object(),DialogGuid=new string('a',32),CurrentCue=new object(),CurrentCueGuid=new string('b',32),
  CuePlayScheduled=false,DialogStopScheduled=false,MessageBox=new object(),MessageBoxShown=false,MessageBoxType=0};}
 static Chunk7BootstrapDialogueObservation Recorder(){var refs=new Dictionary<object,int>();return new Chunk7BootstrapDialogueObservation(o=>{int label;if(!refs.TryGetValue(o,out label)){label=-1001-refs.Count;refs.Add(o,label);}return label;});}
 static int Held(Chunk7BootstrapDialogueObservation value){return ((RetainedObjectIdentity)typeof(Chunk7BootstrapDialogueObservation).GetField("identities",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(value)).RetainedCount;}
 static JObject Row(Chunk7BootstrapDialogueObservation value,int index=0){return (JObject)value.Capture()["snapshots"][index];}
 static void Save(string folder,string name,JObject value){File.WriteAllText(Path.Combine(folder,name),value.ToString(Formatting.Indented));}
 internal static int Main(string[] args){
  if(args.Length!=1)throw new InvalidOperationException("Exact private output folder required");var folder=args[0];
  var recorder=Recorder();var game=new Game();var state=State(game);state.QueuedCue=state.CurrentCue;state.QueuedCueGuid=state.CurrentCueGuid;
  recorder.Observe("bootstrap-readiness",10,()=>state);var initial=recorder.Capture();
  Check("readiness records raw default and independent hidden popup",(bool)Row(recorder)["readSucceeded"]&&(string)Row(recorder)["topMode"]=="Default"&&!(bool)Row(recorder)["messageBoxShown"]);
  Check("negative object labels are retained exact references",(int)Row(recorder)["game"]<0&&Held(recorder)==5);
  Check("same cue reference keeps one label",(int)Row(recorder)["queuedCue"]["object"]==(int)Row(recorder)["currentCue"]["object"]);
  state.TopMode="Dialog";state.DialogActive=true;state.CuePlayScheduled=true;recorder.Observe("cleanup-entry",11,()=>state);
  Check("authored dialog and system popup remain separate",(bool)Row(recorder,1)["dialogActive"]&&!(bool)Row(recorder,1)["messageBoxShown"]);
  Check("prior snapshot is immutable after source changes",(string)initial["snapshots"][0]["topMode"]=="Default");
  state.MessageBoxShown=true;state.DialogStopScheduled=true;recorder.Observe("cleanup-settlement",12,()=>state);
  Save(folder,"artifact.json",recorder.Capture());Check("artifact retains live recorder for terminal sample",!recorder.Released&&Held(recorder)>0);
  NativeRead.State=state;NativeRead.Error=null;NativeRead.Reads=0;UnityEngine.Time.frameCount=13;
  var terminal=recorder.CaptureTerminal(game);Save(folder,"terminal.json",terminal);
  Check("exact four ordered phases are recorded",((JArray)terminal["snapshots"]).Count==4&&(string)terminal["snapshots"][3]["phase"]=="host-terminal");
  Check("terminal reads native facade once",NativeRead.Reads==1);
  Check("terminal releases every retained native reference",recorder.Released&&Held(recorder)==0&&(bool)terminal["identityReleased"]);
  Check("terminal keeps raw Dialog and shown popup",(string)terminal["snapshots"][3]["topMode"]=="Dialog"&&(bool)terminal["snapshots"][3]["messageBoxShown"]);
  var untouched=recorder.CaptureTerminal(game);terminal["snapshots"][0]["frame"]=-1;
  Check("terminal is idempotent and deep cloned",NativeRead.Reads==1&&(int)recorder.CaptureTerminal(game)["snapshots"][0]["frame"]==10&&(int)untouched["snapshots"][0]["frame"]==10);
  var oldDto=DtoSerialization.Capture(null);var newDto=DtoSerialization.Capture(untouched);Save(folder,"game-absent.json",oldDto);Save(folder,"game-present.json",newDto);
  Check("actual old DTO shape omits optional field",oldDto.Property("bootstrapDialogue")==null);
  Check("actual new DTO shape contains capped raw object",newDto["bootstrapDialogue"] is JObject&&JToken.DeepEquals(newDto["bootstrapDialogue"],JObject.Parse(untouched.ToString(Formatting.None))));
  newDto.Remove("bootstrapDialogue");Check("optional field leaves every old DTO value unchanged",JToken.DeepEquals(oldDto,newDto));
  Check("serialization does not activate native read",NativeRead.Reads==1);
  recorder=Recorder();int reads=0;recorder.Observe("cleanup-entry",20,()=>{reads++;return State();});recorder.Observe("cleanup-entry",21,()=>{reads++;throw new Exception();});
  Check("duplicate phase is read once",reads==1&&((JArray)recorder.Capture()["snapshots"]).Count==1);
  recorder.Observe("bootstrap-readiness",22,()=>{reads++;return State();});Check("phase reversal is explicit without native read",reads==1&&((JArray)recorder.Capture()["errors"]).Count==1);
  recorder.Observe("unknown",22,()=>State());recorder.Observe("cleanup-settlement",-1,()=>State());for(int n=0;n<10;n++)recorder.Observe("unknown",1,()=>State());Check("observer errors stay bounded",((JArray)recorder.Capture()["errors"]).Count==4);
  recorder.Dispose();recorder.Observe("host-terminal",23,()=>{reads++;return State();});Check("released recorder never reacquires references",Held(recorder)==0&&reads==1);
  recorder=Recorder();recorder.Observe("cleanup-entry",30,()=>{throw new InvalidOperationException(new string('x',400));});
  Check("read failure is explicit and capped",!(bool)Row(recorder)["readSucceeded"]&&((string)Row(recorder)["readError"]).Length==256);
  Check("failed read records no partial native state",Row(recorder).Properties().Where(x=>x.Name!="phase"&&x.Name!="frame"&&x.Name!="readSucceeded"&&x.Name!="readError").All(x=>x.Value.Type==JTokenType.Null));
  NativeRead.Error=new IOException("native field read failed");UnityEngine.Time.frameCount=31;var failedTerminal=recorder.CaptureTerminal(game);Save(folder,"read-error.json",failedTerminal);
  Check("failed terminal read still releases registry",recorder.Released&&Held(recorder)==0&&!(bool)failedTerminal["snapshots"][1]["readSucceeded"]);NativeRead.Error=null;
  recorder=Recorder();recorder.Observe("bootstrap-readiness",1,()=>null);Check("null read state is explicit failure",!(bool)Row(recorder)["readSucceeded"]);
  recorder=Recorder();var empty=new Chunk7BootstrapDialogueObservation.RawState();recorder.Observe("cleanup-entry",1,()=>empty);NativeRead.State=empty;UnityEngine.Time.frameCount=2;var nullTerminal=recorder.CaptureTerminal(null);Save(folder,"null-state.json",nullTerminal);
  Check("absent game/controller/popup remain raw nulls",(int)nullTerminal["snapshots"][0]["game"]==0&&(string)nullTerminal["snapshots"][0]["topMode"]==null&&(int)nullTerminal["snapshots"][0]["dialog"]["object"]==0);
  recorder=new Chunk7BootstrapDialogueObservation(o=>17);recorder.Observe("cleanup-entry",1,()=>State());Check("hash collision is a failed observation",!(bool)Row(recorder)["readSucceeded"]&&(int)recorder.Capture()["identityFaultCount"]==1);recorder.Dispose();Check("partial identity collision releases acquired reference",Held(recorder)==0);
  recorder=new Chunk7BootstrapDialogueObservation(o=>0);recorder.Observe("cleanup-entry",1,()=>State());Check("zero label refuses instead of aliasing null",!(bool)Row(recorder)["readSucceeded"]&&(int)recorder.Capture()["identityFaultCount"]==1);
  recorder=Recorder();state=State();state.TopMode=new string('x',65);recorder.Observe("cleanup-entry",1,()=>state);Check("oversized mode becomes bounded read error",!(bool)Row(recorder)["readSucceeded"]&&Held(recorder)==0);
  recorder=Recorder();state=State();state.DialogGuid=new string('x',65);recorder.Observe("cleanup-entry",1,()=>state);Check("oversized blueprint label cannot escape observation bound",!(bool)Row(recorder)["readSucceeded"]);recorder.Dispose();Check("partially labelled oversized read releases references",Held(recorder)==0);
  recorder=Recorder();state=State();state.Dialog=null;recorder.Observe("cleanup-entry",1,()=>state);Check("blueprint string without object refuses",!(bool)Row(recorder)["readSucceeded"]);
  recorder=Recorder();for(int index=0;index<4;index++){state=State();state.QueuedCue=new object();state.QueuedCueGuid="queued";state.ContinueCue=new object();state.ContinueCueGuid="continued";recorder.Observe(new[]{"bootstrap-readiness","cleanup-entry","cleanup-settlement","host-terminal"}[index],index,()=>state);}Check("four maximum distinct rows stay under identity capacity",Held(recorder)==28&&((JArray)recorder.Capture()["snapshots"]).Count==4);recorder.Dispose();Check("maximum observation releases all references",Held(recorder)==0);
  recorder=Recorder();recorder.Dispose();NativeRead.Reads=0;UnityEngine.Time.frameCount=40;var preReleased=recorder.CaptureTerminal(game);Save(folder,"released-error.json",preReleased);Check("premature release is explicit and does not read native",NativeRead.Reads==0&&((JArray)preReleased["errors"]).Count==1&&(bool)preReleased["identityReleased"]);
  NativeRead.UseActual=true;var nativeGame=new Game{CurrentMode=GameModeType.Dialog,DialogController=new DialogController{m_CuePlayScheduled=true,m_DialogStopScheduled=false,Dialog=new BlueprintDialog{AssetGuidThreadSafe="native-dialog"},m_CurrentCue=new BlueprintCue{AssetGuidThreadSafe="native-cue"}}};
  var popup=new DialogMessageBox{Destroyed=true,Shown=true,m_BoxType=DialogMessageBoxBase.BoxType.TestValue};DialogMessageBox.Instance=popup;recorder=Recorder();UnityEngine.Time.frameCount=50;var unityNull=recorder.CaptureTerminal(nativeGame);Save(folder,"unity-null-popup.json",unityNull);
  Check("actual read preserves destroyed popup CLR identity",(int)unityNull["snapshots"][0]["messageBox"]!=0&&(bool?)unityNull["snapshots"][0]["messageBoxShown"]==null);
  Check("actual Unity-null branch avoids popup getter",popup.Reads==0&&(int?)unityNull["snapshots"][0]["messageBoxType"]==null);
  Check("actual native read keeps story flags independent",(bool)unityNull["snapshots"][0]["dialogActive"]&&(bool)unityNull["snapshots"][0]["cuePlayScheduled"]&&(string)unityNull["snapshots"][0]["currentCue"]["guid"]=="native-cue");
  DialogMessageBox.Instance=null;recorder=Recorder();var absentPopup=recorder.CaptureTerminal(nativeGame);Save(folder,"absent-popup.json",absentPopup);Check("actual absent popup remains zero identity",(int)absentPopup["snapshots"][0]["messageBox"]==0&&(bool?)absentPopup["snapshots"][0]["messageBoxShown"]==null);
  DialogMessageBox.Instance=new DialogMessageBox{Shown=true,m_BoxType=DialogMessageBoxBase.BoxType.TestValue};recorder=Recorder();var livePopup=recorder.CaptureTerminal(nativeGame);Save(folder,"live-popup.json",livePopup);Check("actual live popup reads shown and enum exactly",(bool)livePopup["snapshots"][0]["messageBoxShown"]&&(int)livePopup["snapshots"][0]["messageBoxType"]==3&&DialogMessageBox.Instance.Reads==1);
  DialogMessageBox.Instance.Throw=true;recorder=Recorder();var popupError=recorder.CaptureTerminal(nativeGame);Check("actual getter exception stays explicit and releases recorder",!(bool)popupError["snapshots"][0]["readSucceeded"]&&recorder.Released&&Held(recorder)==0);
  NativeRead.UseActual=false;DialogMessageBox.Instance=null;
  Console.WriteLine("TOTAL PASS="+passed+" FAIL="+failed);return failed==0?0:1;
 }
}
}
