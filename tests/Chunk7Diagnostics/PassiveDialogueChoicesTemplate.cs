using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Newtonsoft.Json.Linq;
using KingmakerMountedCombat.Diagnostics;
using Kingmaker.UI;
/*ACTUAL_SOURCES*/
static class Boundary
{
 public static int ForbiddenCalls; public static Action<string> AfterRead;
 public static void Forbidden(){ForbiddenCalls++;throw new Exception("Native evaluation or construction is forbidden in passive observation.");}
 public static T Blank<T>() where T:class {return (T)FormatterServices.GetUninitializedObject(typeof(T));}
 public static T Read<T>(Type type,object instance,string name,int token){
  if(name=="<Scheduled>k__BackingField")name="Scheduled"; if(name=="<IsScheduled>k__BackingField")name="IsScheduled";
  var f=type.GetField(name,BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly);
  if(f==null||f.FieldType!=typeof(T))throw new Exception("External native field declaration changed: "+type.Name+"."+name);
  var value=(T)f.GetValue(instance); var action=AfterRead;if(action!=null)action(name);return value;
 }
}
namespace UnityEngine {public class Object {public bool Destroyed; public static bool operator ==(Object a,Object b){return (ReferenceEquals(a,null)||a.Destroyed)==(ReferenceEquals(b,null)||b.Destroyed)&&(ReferenceEquals(a,b)||(ReferenceEquals(a,null)||a.Destroyed));} public static bool operator !=(Object a,Object b){return !(a==b);}public override bool Equals(object o){return ReferenceEquals(this,o);}public override int GetHashCode(){return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);}}public class MonoBehaviour:Object{}public static class Time{public static int frameCount=1;}}
namespace Kingmaker.GameModes {public enum GameModeType{None,Default,Dialog}}
namespace Kingmaker.Blueprints {public class BlueprintScriptableObject:UnityEngine.Object {public string Guid;public string AssetGuidThreadSafe {get{return Guid;}}}}
namespace Kingmaker.Blueprints.Root {public class BlueprintRoot {public static BlueprintRoot Instance;public DialogRoot Dialog;}public class DialogRoot {public Kingmaker.DialogSystem.Blueprints.BlueprintAnswer ContinueAnswer,ExitAnswer,InterchapterContinueAnswer,InterchapterExitAnswer;}}
namespace Kingmaker.ElementsSystem {public class Condition{public Condition(){Boundary.Forbidden();}public bool Check(){Boundary.Forbidden();return false;}}public class GameAction{public GameAction(){Boundary.Forbidden();}public void Run(){Boundary.Forbidden();}}public class ConditionsChecker{public Condition[] Conditions;public bool Check(){Boundary.Forbidden();return false;}}public class ActionList{public GameAction[] Actions;public void Run(){Boundary.Forbidden();}}}
namespace Kingmaker.DialogSystem {public class CharacterSelection {public enum Type{Clear,Keep,Random,Player,Manual,Companion} public Type SelectionType;}}
namespace Kingmaker.DialogSystem.Blueprints {public enum DialogType{Common,Book,Interchapter} public class BlueprintDialog:Kingmaker.Blueprints.BlueprintScriptableObject{} public class BlueprintCueBase:Kingmaker.Blueprints.BlueprintScriptableObject{}public class BlueprintCue:BlueprintCueBase{}public class BlueprintAnswer:Kingmaker.Blueprints.BlueprintScriptableObject{public CharacterSelection CharacterSelection;public bool RequireValidCue;public Kingmaker.ElementsSystem.ConditionsChecker ShowConditions,SelectConditions;public Kingmaker.ElementsSystem.ActionList OnSelect;public BlueprintAnswer(){Boundary.Forbidden();}public bool CanShow(){Boundary.Forbidden();return false;}public bool CanSelect(){Boundary.Forbidden();return false;}}}
namespace Kingmaker.DialogSystem.State {public class DialogState {public StartDialogData Scheduled;} public class StartDialogData {public Kingmaker.DialogSystem.Blueprints.BlueprintDialog Dialog;public bool IsScheduled;}}
namespace Kingmaker.Controllers.Dialog {public class DialogController {public List<Kingmaker.DialogSystem.Blueprints.BlueprintAnswer> m_Answers;public Kingmaker.DialogSystem.Blueprints.BlueprintDialog Dialog;public Kingmaker.DialogSystem.Blueprints.BlueprintCue m_CurrentCue;public Kingmaker.DialogSystem.Blueprints.BlueprintCueBase m_CueToPlay,m_ContinueCue;public bool m_CuePlayScheduled,m_DialogStopScheduled;public void SelectAnswer(){Boundary.Forbidden();}}}
namespace Kingmaker.UI {public class UIAccess {public UICommon Common;} public class UICommon:UnityEngine.MonoBehaviour {public Kingmaker.UI.Dialog.DialogBookEventUIManager DialogUIManager;}public class DialogMessageBoxBase {public enum BoxType{One,Two}}public class DialogMessageBox:UnityEngine.Object {public static DialogMessageBox Instance;public bool IsShown;public DialogMessageBoxBase.BoxType m_BoxType;}}
namespace Kingmaker.UI.Dialog {public class DialogBookEventUIManager:UnityEngine.MonoBehaviour {public DialogController Dialog;public Kingmaker.DialogSystem.Blueprints.DialogType m_State;public bool m_IsShow;}public class DialogController:UnityEngine.MonoBehaviour {public DialogCurrentPart CurrentPart;public bool m_IsActive;public DialogueSystemButton SystemButton;}public class DialogueSystemButton:UnityEngine.MonoBehaviour {public bool m_IsEnable;public Kingmaker.DialogSystem.Blueprints.BlueprintAnswer m_Answer;}public class DialogCurrentPart:UnityEngine.MonoBehaviour {public List<TempOptionUI> m_AnswersList;}public class TempOptionUI:UnityEngine.MonoBehaviour {public Kingmaker.DialogSystem.Blueprints.BlueprintAnswer m_Answer;public int m_Index;public bool m_ChooseCharacterInit,m_IsInit,m_Disabled,m_Interchapter;public TempOptionUI(){Boundary.Forbidden();}public void Initialize(){Boundary.Forbidden();}public void OnClick(){Boundary.Forbidden();}}public class AnswerVM{public AnswerVM(){Boundary.Forbidden();}}}
namespace Kingmaker {public class Player{public Kingmaker.DialogSystem.State.DialogState Dialog;}public class Game{public Kingmaker.Controllers.Dialog.DialogController DialogController;public UI.UIAccess UI;public Player Player;public GameModes.GameModeType CurrentMode;public bool DialogActive,CutsceneLock;public bool IsModeActive(GameModes.GameModeType mode){return DialogActive;}}}
static class Tests
{
 static void Write(string directory,string name,JObject value){if(directory!=null)System.IO.File.WriteAllText(System.IO.Path.Combine(directory,name),value.ToString());}
 static int passed,failed;
 static void Check(bool value,string name){if(value){passed++;Console.WriteLine("PASS "+name);}else{failed++;Console.WriteLine("FAIL "+name);}}
 static T B<T>()where T:class{return Boundary.Blank<T>();}
 static Kingmaker.DialogSystem.Blueprints.BlueprintAnswer Answer(char c){var a=B<Kingmaker.DialogSystem.Blueprints.BlueprintAnswer>();a.Guid=new string(c,32);a.CharacterSelection=B<Kingmaker.DialogSystem.CharacterSelection>();a.ShowConditions=B<Kingmaker.ElementsSystem.ConditionsChecker>();a.ShowConditions.Conditions=new[]{B<Kingmaker.ElementsSystem.Condition>()};a.SelectConditions=B<Kingmaker.ElementsSystem.ConditionsChecker>();a.SelectConditions.Conditions=new Kingmaker.ElementsSystem.Condition[0];a.OnSelect=B<Kingmaker.ElementsSystem.ActionList>();a.OnSelect.Actions=new[]{B<Kingmaker.ElementsSystem.GameAction>()};return a;}
 static Kingmaker.UI.Dialog.TempOptionUI Row(Kingmaker.DialogSystem.Blueprints.BlueprintAnswer a,int index=0){var r=B<Kingmaker.UI.Dialog.TempOptionUI>();r.m_Answer=a;r.m_Index=index;r.m_IsInit=true;return r;}
 static Kingmaker.Game Game(){var g=B<Kingmaker.Game>();g.CurrentMode=Kingmaker.GameModes.GameModeType.Dialog;g.DialogActive=true;g.Player=B<Kingmaker.Player>();g.Player.Dialog=B<Kingmaker.DialogSystem.State.DialogState>();g.DialogController=B<Kingmaker.Controllers.Dialog.DialogController>();g.DialogController.Dialog=B<Kingmaker.DialogSystem.Blueprints.BlueprintDialog>();g.DialogController.Dialog.Guid=new string('a',32);g.DialogController.m_CurrentCue=B<Kingmaker.DialogSystem.Blueprints.BlueprintCue>();g.DialogController.m_CurrentCue.Guid=new string('b',32);g.DialogController.m_Answers=new List<Kingmaker.DialogSystem.Blueprints.BlueprintAnswer>{Answer('c')};g.UI=B<Kingmaker.UI.UIAccess>();g.UI.Common=B<Kingmaker.UI.UICommon>();g.UI.Common.DialogUIManager=B<Kingmaker.UI.Dialog.DialogBookEventUIManager>();var m=g.UI.Common.DialogUIManager;m.m_IsShow=true;m.Dialog=B<Kingmaker.UI.Dialog.DialogController>();m.Dialog.m_IsActive=true;m.Dialog.CurrentPart=B<Kingmaker.UI.Dialog.DialogCurrentPart>();m.Dialog.CurrentPart.m_AnswersList=new List<Kingmaker.UI.Dialog.TempOptionUI>{Row(g.DialogController.m_Answers[0])};return g;}
 static List<Kingmaker.UI.Dialog.TempOptionUI> Rows(Kingmaker.Game g){return g.UI.Common.DialogUIManager.Dialog.CurrentPart.m_AnswersList;}
 static Chunk7DialogueChoiceObservation.RawState Native(Kingmaker.Game g){return Chunk7DialogueChoiceObservation.ReadNative(g,g==null?null:g.DialogController,g==null||g.DialogController==null?null:g.DialogController.Dialog,g==null||g.DialogController==null?null:g.DialogController.m_CurrentCue);}
 static JObject Capture(Kingmaker.Game g){using(var ids=new RetainedObjectIdentity(444))return Chunk7DialogueChoiceObservation.Capture(Native(g),ids.Get);}
 static bool Good(JObject o){return (bool)o["readSucceeded"];}
 static void Refused(Action<Kingmaker.Game> mutate,string label,string contains=null){var g=Game();mutate(g);var o=Capture(g);Check(!Good(o)&&(contains==null||((string)o["readError"]).Contains(contains)),label);Boundary.AfterRead=null;}
 static void Status(Action<Kingmaker.Game> mutate,string expected,string name){var g=Game();mutate(g);var o=Capture(g);Check(Good(o)&&(string)o["uiStatus"]==expected,name);}
 static Chunk7BootstrapDialogueObservation.RawState BaseState(int n){return new Chunk7BootstrapDialogueObservation.RawState{Game=new object(),Controller=new object(),Dialog=new object(),DialogGuid=new string('a',32),CurrentCue=new object(),CurrentCueGuid=new string('b',32),QueuedCue=new object(),QueuedCueGuid=new string('c',32),ContinueCue=new object(),ContinueCueGuid=new string('d',32),DialogState=new object(),ScheduledStart=new object(),ScheduledStartIsScheduled=false,ScheduledDialog=new object(),ScheduledDialogGuid=new string('e',32),MessageBox=new object(),MessageBoxShown=false,MessageBoxType=0,TopMode="Dialog",DialogActive=true,CutsceneLock=false,CuePlayScheduled=false,DialogStopScheduled=false};}
 static Chunk7DialogueChoiceObservation.RawState MaxChoices(Chunk7BootstrapDialogueObservation.RawState b){return new Chunk7DialogueChoiceObservation.RawState{Game=b.Game,Controller=b.Controller,Dialog=b.Dialog,CurrentCue=b.CurrentCue,Ui=new object(),Common=new object(),Manager=new object(),PcDialog=new object(),CurrentPart=new object(),UiStatus="pc-cached-rows",ManagerKind=0,ManagerShown=true,PcDialogActive=true,Offered=Enumerable.Range(0,32).Select(i=>new Chunk7DialogueChoiceObservation.Offered{Answer=new object(),Guid=new string('f',32),ShowTypes=new string[0],SelectTypes=new string[0],ActionTypes=new string[0]}).ToArray(),Rows=Enumerable.Range(0,32).Select(i=>new Chunk7DialogueChoiceObservation.Row{View=new object(),Answer=new object(),Guid=new string('f',32),Initialized=true}).ToArray()};}
 internal static int Run(string outputDirectory){
  var g=Game();var o=Capture(g);Check(Good(o)&&(string)o["uiStatus"]=="pc-cached-rows","active PC raw snapshot");
  Check(o["offered"].Count()==1&&(string)o["offered"][0]["answer"]["guid"]==new string('c',32),"offered native answer identity");
  Check(o["rows"][0]["matchingOfferIndices"].Count()==1&&(int)o["rows"][0]["matchingOfferIndices"][0]==0,"exact offered and row reference match");
  Check(!(bool)o["rows"][0]["cachedDisabled"]&&!(bool)o["cachedStateIsAuthorization"],"cached enabled metadata grants no authority");
  Check((int)o["directTypeCount"]==2&&(string)o["offered"][0]["onSelectActionTypes"][0]=="Kingmaker.ElementsSystem.GameAction","direct type names only");
  Rows(g)[0].m_Disabled=true;o=Capture(g);Check((bool)o["rows"][0]["cachedDisabled"],"stored disabled flag preserved");
  Rows(g)[0].m_ChooseCharacterInit=true;Rows(g)[0].m_Interchapter=true;Rows(g)[0].m_IsInit=false;o=Capture(g);Check((bool)o["rows"][0]["choosingCharacter"]&&(bool)o["rows"][0]["interchapter"]&&!(bool)o["rows"][0]["initialized"],"raw row lifecycle flags retained");
  g=Game();Rows(g)[0].m_Answer=Answer('c');o=Capture(g);Check(Good(o)&&o["rows"][0]["matchingOfferIndices"].Count()==0,"same GUID foreign object does not match offered object");
  Rows(g)[0].m_Answer=null;o=Capture(g);Check(Good(o)&&(int)o["rows"][0]["answer"]["object"]==0&&o["rows"][0]["matchingOfferIndices"].Count()==0,"unbound UI row stays unbound");
  g=Game();g.DialogController.m_Answers.Add(g.DialogController.m_Answers[0]);o=Capture(g);Check(Good(o)&&o["rows"][0]["matchingOfferIndices"].Count()==2,"duplicate offered references visible");
  g=Game();Rows(g).Add(Rows(g)[0]);o=Capture(g);Check((int)o["rows"][0]["sameRowReferences"]==2&&(int)o["rows"][0]["sameAnswerReferences"]==2,"duplicate UI row references visible");
  g=Game();Rows(g).Add(Row(g.DialogController.m_Answers[0],1));o=Capture(g);Check((int)o["rows"][0]["sameRowReferences"]==1&&(int)o["rows"][0]["sameAnswerReferences"]==2,"distinct duplicate-answer rows visible");
  Check((string)Capture(null)["uiStatus"]=="missing-game","missing game observed");
  Status(x=>x.DialogController=null,"missing-controller","missing native controller observed");
  Status(x=>x.UI=null,"missing-ui","missing UI observed");Status(x=>x.UI.Common=null,"missing-common","missing common UI observed");
  Status(x=>x.UI.Common.DialogUIManager=null,"missing-manager","missing manager observed");
  Status(x=>x.UI.Common.DialogUIManager.m_State=Kingmaker.DialogSystem.Blueprints.DialogType.Book,"unsupported-dialog-kind","book dialogue is not guessed as PC rows");
  Status(x=>x.UI.Common.DialogUIManager.m_State=Kingmaker.DialogSystem.Blueprints.DialogType.Interchapter,"unsupported-dialog-kind","interchapter explicitly unsupported");
  Status(x=>x.UI.Common.DialogUIManager.Dialog=null,"missing-pc-dialog","missing PC view observed");
  Status(x=>x.UI.Common.DialogUIManager.Dialog.CurrentPart=null,"missing-current-part","missing current part observed");
  Status(x=>x.CurrentMode=Kingmaker.GameModes.GameModeType.Default,"inactive-dialog","stale rows outside top Dialog labelled inactive");
  Status(x=>x.DialogActive=false,"inactive-dialog","inactive Dialog mode labelled");Status(x=>x.UI.Common.DialogUIManager.m_IsShow=false,"inactive-dialog","hidden manager cache labelled");
  Status(x=>x.UI.Common.DialogUIManager.Dialog.m_IsActive=false,"inactive-dialog","inactive PC view cache labelled");
  Status(x=>x.UI.Common.Destroyed=true,"missing-common","destroyed Unity common not dereferenced");
  Refused(x=>x.DialogController.m_Answers=null,"null offered list refuses");Refused(x=>x.DialogController.m_Answers.Add(null),"null offered entry refuses");
  Refused(x=>x.DialogController.m_Answers[0].Destroyed=true,"destroyed offered blueprint refuses");
  Refused(x=>x.DialogController.m_Answers=Enumerable.Range(0,33).Select(i=>Answer('c')).ToList(),"offered cap refuses before traversal","over bound");
  Refused(x=>x.UI.Common.DialogUIManager.Dialog.CurrentPart.m_AnswersList=null,"null UI row list refuses");Refused(x=>Rows(x).Add(null),"null PC row refuses");
  Refused(x=>Rows(x)[0].Destroyed=true,"destroyed row refuses");Refused(x=>Rows(x).AddRange(Enumerable.Range(0,32).Select(i=>Row(x.DialogController.m_Answers[0]))),"PC row cap refuses","over bound");
  Refused(x=>x.DialogController.m_Answers[0].ShowConditions.Conditions=Enumerable.Range(0,33).Select(i=>B<Kingmaker.ElementsSystem.Condition>()).ToArray(),"direct condition-list cap refuses");
  Refused(x=>{x.DialogController.m_Answers=Enumerable.Range(0,9).Select(i=>Answer('c')).ToList();foreach(var a in x.DialogController.m_Answers)a.OnSelect.Actions=Enumerable.Range(0,32).Select(i=>B<Kingmaker.ElementsSystem.GameAction>()).ToArray();},"aggregate direct-type cap refuses");
  g=Game();g.DialogController.m_Answers[0].ShowConditions=null;g.DialogController.m_Answers[0].SelectConditions.Conditions=null;g.DialogController.m_Answers[0].OnSelect.Actions=new Kingmaker.ElementsSystem.GameAction[]{null};o=Capture(g);Check(Good(o)&&o["offered"][0]["showConditionTypes"].Type==JTokenType.Null&&o["offered"][0]["selectConditionTypes"].Type==JTokenType.Null&&o["offered"][0]["onSelectActionTypes"][0].Type==JTokenType.Null,"null metadata containers and elements remain raw nulls");
  g=Game();g.DialogController.m_Answers[0].CharacterSelection=null;o=Capture(g);Check(Good(o)&&o["offered"][0]["selectionType"].Type==JTokenType.Null,"null character-selection metadata retained");
  Refused(x=>x.DialogController.m_Answers[0].Guid="malformed","malformed answer GUID refuses");
  g=Game();var n=Native(g);n.Offered[0].ShowTypes=new[]{new string('x',257)};using(var ids=new RetainedObjectIdentity(444))Check(!Good(Chunk7DialogueChoiceObservation.Capture(n,ids.Get)),"overlong metadata name refuses");
  g=Game();bool changed=false;Boundary.AfterRead=name=>{if(name=="m_Disabled"&&!changed){changed=true;g.DialogController.m_Answers.Clear();}};o=Capture(g);Boundary.AfterRead=null;Check(!Good(o)&&((string)o["readError"]).Contains("changed during"),"offered-list mutation detected");
  g=Game();changed=false;Boundary.AfterRead=name=>{if(name=="m_Disabled"&&!changed){changed=true;g.DialogController.m_CurrentCue=B<Kingmaker.DialogSystem.Blueprints.BlueprintCue>();}};o=Capture(g);Boundary.AfterRead=null;Check(!Good(o)&&((string)o["readError"]).Contains("context changed"),"cue replacement during read refuses");
  g=Game();changed=false;Boundary.AfterRead=name=>{if(name=="m_Disabled"&&!changed){changed=true;Rows(g).Add(Row(g.DialogController.m_Answers[0]));}};o=Capture(g);Boundary.AfterRead=null;Check(!Good(o)&&((string)o["readError"]).Contains("PC rows"),"UI list mutation detected");
  Check(Boundary.ForbiddenCalls==0,"no native constructor predicate callback condition action evaluated");
  var recorder=new Chunk7BootstrapDialogueObservation();var phases=new[]{"bootstrap-readiness","cleanup-entry","cleanup-settlement","host-terminal"};
  for(int i=0;i<4;i++){var state=BaseState(i);state.Choices=MaxChoices(state);recorder.Observe(phases[i],i,()=>state);}var evidence=recorder.Capture();
  Check((int)evidence["identityCapacity"]==444&&evidence["identityIds"].Count()==444&&(int)evidence["identityFaultCount"]==0,"four fully distinct snapshots fit exact 444 identity bound");
  Check(evidence["snapshots"].All(x=>(bool)x["readSucceeded"]&&(bool)x["choices"]["readSucceeded"]),"maximum snapshots preserve complete base and choice data");
  recorder.Dispose();Check((bool)recorder.Capture()["identityReleased"],"choice references share released parent registry");
  recorder=new Chunk7BootstrapDialogueObservation();g=Game();recorder.Observe("cleanup-entry",g);evidence=recorder.Capture();Check(evidence["snapshots"].Count()==1&&(string)evidence["snapshots"][0]["phase"]=="cleanup-entry"&&(bool)evidence["snapshots"][0]["choices"]["readSucceeded"],"failure entry captures choices without readiness snapshot");
  var terminal=recorder.CaptureTerminal(g);Check((bool)terminal["identityReleased"]&&(string)terminal["snapshots"].Last["phase"]=="host-terminal","terminal capture releases choices");
  recorder=new Chunk7BootstrapDialogueObservation();g=Game();g.DialogController.m_Answers=null;recorder.Observe("cleanup-entry",g);evidence=recorder.Capture();Check((bool)evidence["snapshots"][0]["readSucceeded"]&&!(bool)evidence["snapshots"][0]["choices"]["readSucceeded"],"choice failure preserves independent base observation");
  recorder.CaptureTerminal(g);Check(recorder.Released,"terminal native choice error still releases registry");
  FailureEntryTests.Run(Check,Game);
  g=Game();recorder=new Chunk7BootstrapDialogueObservation();recorder.Observe("cleanup-entry",g);Write(outputDirectory,"choice-valid.json",recorder.CaptureTerminal(g));
  g=Game();g.DialogController.m_Answers=null;recorder=new Chunk7BootstrapDialogueObservation();recorder.Observe("cleanup-entry",g);Write(outputDirectory,"choice-error.json",recorder.CaptureTerminal(g));
  g=Game();Rows(g)[0].m_Answer=Answer('c');recorder=new Chunk7BootstrapDialogueObservation();recorder.Observe("cleanup-entry",g);Write(outputDirectory,"choice-foreign.json",recorder.CaptureTerminal(g));
  g=Game();g.DialogController.m_Answers.Add(g.DialogController.m_Answers[0]);Rows(g).Add(Rows(g)[0]);recorder=new Chunk7BootstrapDialogueObservation();recorder.Observe("cleanup-entry",g);Write(outputDirectory,"choice-duplicates.json",recorder.CaptureTerminal(g));
  recorder=new Chunk7BootstrapDialogueObservation();recorder.Observe("cleanup-entry",(Kingmaker.Game)null);Write(outputDirectory,"choice-absent.json",recorder.CaptureTerminal(null));
  g=Game();g.UI.Common.DialogUIManager.m_State=Kingmaker.DialogSystem.Blueprints.DialogType.Book;recorder=new Chunk7BootstrapDialogueObservation();recorder.Observe("cleanup-entry",g);Write(outputDirectory,"choice-unsupported.json",recorder.CaptureTerminal(g));
  g=Game();g.DialogController.m_Answers[0].ShowConditions=null;g.DialogController.m_Answers[0].OnSelect.Actions=new Kingmaker.ElementsSystem.GameAction[]{null};recorder=new Chunk7BootstrapDialogueObservation();recorder.Observe("cleanup-entry",g);Write(outputDirectory,"choice-null-metadata.json",recorder.CaptureTerminal(g));
  recorder=new Chunk7BootstrapDialogueObservation();for(int i=0;i<4;i++){var state=BaseState(i);state.Choices=MaxChoices(state);recorder.Observe(phases[i],i,()=>state);}recorder.Dispose();Write(outputDirectory,"choice-max.json",recorder.Capture());
  recorder=new Chunk7BootstrapDialogueObservation();for(int i=0;i<4;i++){var state=BaseState(i);state.Choices=MaxChoices(state);for(int j=0;j<8;j++)state.Choices.Offered[j].ShowTypes=Enumerable.Repeat(new string('\u0001',256),32).ToArray();recorder.Observe(phases[i],i,()=>state);}recorder.Dispose();var maximumText=recorder.Capture();
  var compactBytes=System.Text.Encoding.UTF8.GetByteCount(maximumText.ToString(Newtonsoft.Json.Formatting.None));var indentedBytes=System.Text.Encoding.UTF8.GetByteCount(maximumText.ToString(Newtonsoft.Json.Formatting.Indented));
  Check(maximumText["snapshots"].All(x=>(bool)x["choices"]["readSucceeded"]&&(int)x["choices"]["directTypeCount"]==256)&&maximumText["identityIds"].Count()==444,"full text bound retains all four phases and 444 identities");
  Check(compactBytes<2*1024*1024&&indentedBytes<2*1024*1024,"maximum escaped type text stays below two MiB within existing sixteen MiB artifact cap");
  Write(outputDirectory,"choice-max-text.json",maximumText);Write(outputDirectory,"maximum-output-bytes.json",new JObject{["compactUtf8Bytes"]=compactBytes,["indentedUtf8Bytes"]=indentedBytes,["existingBootstrapSourceArtifactCap"]=16*1024*1024,["diagnosticBudgetChecked"]=2*1024*1024});
  Check(Boundary.ForbiddenCalls==0,"all boundary cases remain passive");
  Console.WriteLine("PASSIVE CHOICE ACTUAL PASS="+passed+" FAIL="+failed);return failed==0?0:1;
 }
}

namespace KingmakerMountedCombat.Diagnostics {
 sealed class ChoiceFailureEntryScenario {
  internal bool completed,disposed,cleanup,callbackReturned;
  internal double cleanupStart;
  internal int Failures,CleanupPolls,Continued;
  internal string LastError;
  internal Kingmaker.Game game;
  internal ChoiceClock clock=new ChoiceClock();
  internal JObject evidence=new JObject();
  internal Chunk7BootstrapDialogueObservation dialogue=new Chunk7BootstrapDialogueObservation();
  private void RequireAuthority(bool cleanupOnly){}
  private bool Busy(){return false;}
  private void PollCleanup(){CleanupPolls++;}
  private void Fail(string kind,string error){Failures++;LastError=kind+": "+error;}
  private static void Require(bool value,string reason){if(!value)throw new InvalidOperationException(reason);}
  /*ACTUAL_UPDATE_FAILURE_PREFIX*/
  /*ACTUAL_BEGIN_CLEANUP*/
 }
 sealed class ChoiceClock {internal double Seconds;internal TimeSpan Elapsed {get{return TimeSpan.FromSeconds(Seconds);}}}
 static class FailureEntryTests {
  internal static void Run(Action<bool,string> check,Func<Kingmaker.Game> game){
   var g=game();var s=new ChoiceFailureEntryScenario{game=g};s.clock.Seconds=301;s.Update();
   check(s.Failures==1&&s.cleanup&&s.Continued==0&&s.LastError.Contains("settlement window"),"actual Update timeout retains original failure and starts cleanup");
   var rows=s.evidence["dialogueObservation"]["snapshots"];
   check(rows.Count()==1&&(string)rows[0]["phase"]=="cleanup-entry"&&(bool)rows[0]["choices"]["readSucceeded"]&&rows[0]["choices"]["rows"].Count()==1,"actual failure BeginCleanup captures offered PC rows without readiness");
   check(g.CurrentMode==Kingmaker.GameModes.GameModeType.Dialog&&g.DialogActive&&s.cleanupStart==301,"failure observation preserves native Dialog and cleanup timestamp");
   s.Update();check(s.CleanupPolls==1&&s.evidence["dialogueObservation"]["snapshots"].Count()==1,"cleanup retry polls without duplicating failure entry snapshot");
   var t=s.dialogue.CaptureTerminal(g);check((bool)t["identityReleased"],"timeout observer references release at terminal boundary");
   g=game();s=new ChoiceFailureEntryScenario{game=g};s.clock.Seconds=301;g.DialogController.m_Answers=null;s.Update();
   check(s.Failures==1&&s.LastError.Contains("settlement window")&&!(bool)s.evidence["dialogueObservation"]["snapshots"][0]["choices"]["readSucceeded"],"choice observation failure cannot replace original timeout");s.dialogue.Dispose();
  }
 }
}
