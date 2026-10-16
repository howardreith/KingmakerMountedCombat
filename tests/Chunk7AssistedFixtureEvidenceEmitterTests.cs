using System;using System.Linq;using System.Reflection;using System.Collections.Generic;using Newtonsoft.Json.Linq;using KingmakerMountedCombat.Diagnostics;
static class AssistedCoreTests
{
 static int passed,failed;static string G(int n){return n.ToString("x32");}
 static void Check(string name,Func<bool> body){try{if(!body())throw new Exception("predicate false");passed++;Console.WriteLine("PASS "+name);}catch(Exception e){failed++;Console.WriteLine("FAIL "+name+": "+e.GetType().Name+" "+e.Message);}}
 static bool Throws(Action a){try{a();return false;}catch{return true;}}
 static T Copy<T>(T a)where T:class{return a==null?null:(T)typeof(object).GetMethod("MemberwiseClone",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(a,null);}
 static Chunk7AssistedStorySnapshot Clone(Chunk7AssistedStorySnapshot s){var v=Copy(s);v.Identity=Copy(s.Identity);v.Offers=s.Offers==null?null:s.Offers.Select(Copy).ToArray();v.Rows=s.Rows==null?null:s.Rows.Select(Copy).ToArray();return v;}
 sealed class Rig:IChunk7AssistedStoryBoundary,IChunk7AssistedShortfallBoundary
 {
  internal Chunk7AssistedFixtureBinding Binding;internal Chunk7AssistedStorySnapshot State;internal Chunk7AssistedShortfallSnapshot Short;
  internal int Clicks,XpCalls,MoneyCalls,ReadCount;internal Action<Rig> BeforeClick,OnClick,OnXp,OnMoney;internal bool FailReadAfterClick,EntryMissing;
  internal Rig(){var game=new object();var player=new object();var mgr=new object();var main=new object();var desc=new object();var auth=new object();Binding=new Chunk7AssistedFixtureBinding("assisted-emitter-generated","11111111-2222-3333-4444-555555555555","KMC_C7_ASSISTED_NATIVE","aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",game,player,mgr,main,desc,auth,1);
   var id=new Chunk7AssistedIdentity{Game=game,Player=player,SaveManager=mgr,Main=main,Descriptor=desc,Authority=auth,RunId=Binding.RunId,CampaignId=Binding.CampaignId,CampaignName=Binding.CampaignName,MainId=Binding.MainId,Generation=1,ExactIsolatedFreshCampaign=true,NoForeignOrMountedCustody=true};
   State=new Chunk7AssistedStorySnapshot{Identity=id,Frame=10,ElapsedMilliseconds=0,Mode="Dialog",UiStatus="pc-cached-rows",MouseController=true,DialogActive=true,MainViewReady=true,
    Controller=new object(),Dialog=new object(),Cue=new object(),Ui=new object(),Common=new object(),Manager=new object(),PcDialog=new object(),CurrentPart=new object(),OffersList=new object(),RowsList=new object(),ManagerKind=0,ManagerShown=true,PcActive=true,DialogGuid=G(1),CueGuid=G(2),
    Offers=new[]{new Chunk7AssistedStoryOffer{Answer=new object(),Selection=new object(),Guid=G(3),SelectionType=3},new Chunk7AssistedStoryOffer{Answer=new object(),Selection=new object(),Guid=G(4),SelectionType=0}},Rows=new Chunk7AssistedStoryRow[2]};
   for(int i=0;i<2;i++)State.Rows[i]=new Chunk7AssistedStoryRow{View=new object(),Answer=State.Offers[i].Answer,Guid=State.Offers[i].Guid,NativeIndex=i+1,Initialized=true,ActiveAndEnabled=true,ActiveInHierarchy=true};
   Short=new Chunk7AssistedShortfallSnapshot{Identity=Copy(id),Frame=10,Experience=100,Level=1,ExperienceRate=100,TargetLevel=4,TargetThreshold=9000,NativeHireCost=8000,Money=100,ReadyDefault=true,LivingIdleField=true,PreservedStateSha256=new string('a',64)};
  }
  Chunk7AssistedStorySnapshot IChunk7AssistedStoryBoundary.Read(){ReadCount++;if(FailReadAfterClick&&Clicks>0)throw new Exception("after read unavailable");return Clone(State);}
  public void Click(Chunk7AssistedStorySnapshot expected,int oi,int ri,Action enter){if(BeforeClick!=null)BeforeClick(this);var actual=Clone(State);Binding.Require(actual.Identity);Chunk7AssistedStoryDriver.RequireSameClickState(expected,actual);if(!EntryMissing)enter();Clicks++;if(OnClick!=null)OnClick(this);}
  public void ClickSystemButton(Chunk7AssistedStorySnapshot expected,Action enter){if(BeforeClick!=null)BeforeClick(this);var actual=Clone(State);Binding.Require(actual.Identity);Chunk7AssistedStoryDriver.RequireSameClickState(expected,actual);if(!EntryMissing)enter();Clicks++;if(OnClick!=null)OnClick(this);}
  Chunk7AssistedShortfallSnapshot IChunk7AssistedShortfallBoundary.Read(){var v=Copy(Short);v.Identity=Copy(Short.Identity);return v;}
  public void GainExperience(Chunk7AssistedShortfallSnapshot expected,int delta,Action enter){Chunk7AssistedShortfall.RequireSameInput(expected,Short);enter();XpCalls++;Short.Experience+=delta;if(OnXp!=null)OnXp(this);}
  public void GainMoney(Chunk7AssistedShortfallSnapshot expected,long delta,Action enter){Chunk7AssistedShortfall.RequireSameInput(expected,Short);enter();MoneyCalls++;Short.Money+=delta;if(OnMoney!=null)OnMoney(this);}
  internal Chunk7AssistedStoryDriver Driver(){return new Chunk7AssistedStoryDriver(Binding,this);}internal Chunk7AssistedShortfall Assistance(){return new Chunk7AssistedShortfall(Binding,this);}
 }
 static bool BadStory(Action<Rig> mutate){var r=new Rig();mutate(r);using(var d=r.Driver())return Throws(()=>d.Tick())&&r.Clicks==0;}
 static bool Stale(Action<Rig> mutate){var r=new Rig();r.BeforeClick=mutate;using(var d=r.Driver())return Throws(()=>d.Tick())&&r.Clicks==0&&d.AttemptCount==1&&!d.Attempts[0].NativeEntered;}
 static bool BadXp(Action<Rig> mutate){var r=new Rig();mutate(r);var a=r.Assistance();return Throws(()=>a.EnsureExperience())&&r.XpCalls==0;}
 static bool BadMoney(Action<Rig> mutate){var r=new Rig();r.Short.Level=4;mutate(r);var a=r.Assistance();return Throws(()=>a.EnsureOneNativeHireCost())&&r.MoneyCalls==0;}

 static void Default(Rig r){r.State.Mode="Default";r.State.UiStatus="not-dialog";r.State.DialogActive=false;r.State.Dialog=null;r.State.Cue=null;r.State.DialogGuid=null;r.State.CueGuid=null;r.State.Ui=null;r.State.Common=null;r.State.Manager=null;r.State.PcDialog=null;r.State.CurrentPart=null;r.State.OffersList=null;r.State.RowsList=null;r.State.ManagerKind=null;r.State.ManagerShown=null;r.State.PcActive=null;r.State.Offers=new Chunk7AssistedStoryOffer[0];r.State.Rows=new Chunk7AssistedStoryRow[0];}
 static void SystemOnly(Rig r){var answer=new object();r.State.Offers=new[]{new Chunk7AssistedStoryOffer{Answer=answer,Selection=new object(),Guid=G(5),SelectionType=0,IsSystemAnswer=true}};r.State.Rows=new Chunk7AssistedStoryRow[0];r.State.SystemButton=new object();r.State.SystemButtonEnabled=true;r.State.SystemButtonAnswer=answer;r.State.ContinueCue=new object();}
 static JObject Emit(string leaf,Rig r,Chunk7AssistedStoryDriver d,Chunk7AssistedShortfall a){var j=Chunk7AssistedFixtureEvidence.Capture(r.Binding,d,a);System.IO.File.WriteAllText(System.IO.Path.Combine(Environment.CurrentDirectory,leaf),j.ToString(),new System.Text.UTF8Encoding(false));return j;}
 static JObject Success(string leaf,bool click,bool skip,bool noXp,bool enoughMoney,bool system=false){var r=new Rig();if(system)SystemOnly(r);if(skip)r.State.Rows[0].Disabled=true;using(var d=r.Driver()){
  if(click){r.OnClick=Default;if(d.Tick()!=Chunk7AssistedStoryStatus.Clicked)throw new Exception("click absent");if(d.Tick()!=Chunk7AssistedStoryStatus.Waiting)throw new Exception("same-frame readiness escaped");r.State.Frame=11;r.State.ElapsedMilliseconds=16;}else{Default(r);}
  if(d.Tick()!=Chunk7AssistedStoryStatus.ReadyAtObservedFrame)throw new Exception("native later-ready observation absent");
  r.Short.Frame=r.State.Frame+1;r.Short.Experience=noXp?9000:100;r.Short.Level=noXp?4:1;r.Short.NativeHireCost=noXp?8000:500;r.Short.Money=enoughMoney?10000:100;
  var a=r.Assistance();a.EnsureExperience();
  // External native level-up boundary is represented only between setup effects.
  // The XP effect itself never changes level/facts/cost/preservation digest.
  r.Short.Frame++;r.Short.Level=4;r.Short.NativeHireCost=8000;r.Short.PreservedStateSha256=new string('b',64);
  a.EnsureOneNativeHireCost();var j=Emit(leaf,r,d,a);if(r.XpCalls!=(noXp?0:1)||r.MoneyCalls!=(enoughMoney?0:1)||r.Clicks!=(click?1:0))throw new Exception("native substitute count changed");return j;
 }}
 // A story hub that consumes an owned click and re-enters the IDENTICAL still-active dialog, cue and
 // answer objects on a later frame. The driver resolves the click as consumed and clicks the next offer
 // whose (controller, dialog, cue, answer) tuple is unused, so the run records two clicks at one cue.
 static JObject SameCueReentry(string leaf){var r=new Rig();using(var d=r.Driver()){
  if(d.Tick()!=Chunk7AssistedStoryStatus.Clicked)throw new Exception("first click absent");
  r.State.Frame=11;r.State.ElapsedMilliseconds=16;
  if(d.Tick()!=Chunk7AssistedStoryStatus.Clicked)throw new Exception("second click at the re-entered cue absent");
  Default(r);r.State.Frame=12;r.State.ElapsedMilliseconds=32;
  if(d.Tick()!=Chunk7AssistedStoryStatus.ReadyAtObservedFrame)throw new Exception("native later-ready observation absent");
  r.Short.Frame=r.State.Frame+1;r.Short.Experience=100;r.Short.Level=1;r.Short.NativeHireCost=500;r.Short.Money=100;
  var a=r.Assistance();a.EnsureExperience();
  // External native level-up boundary is represented only between setup effects.
  r.Short.Frame++;r.Short.Level=4;r.Short.NativeHireCost=8000;r.Short.PreservedStateSha256=new string('b',64);
  a.EnsureOneNativeHireCost();var j=Emit(leaf,r,d,a);
  if(r.Clicks!=2||r.XpCalls!=1||r.MoneyCalls!=1)throw new Exception("native substitute count changed");return j;}}
 static int Main(){
 Check("actual zero-click ready and two no-op effects",()=>{var j=Success("ready-noop.json",false,false,true,true);return (int)j["story"]["nativeClickCount"]==0&&(bool)j["experience"]["noOp"]&&(bool)j["hireFunds"]["noOp"]&&(string)j["mainActorId"]=="aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";});
 Check("actual one click then positive XP and funds",()=>{var j=Success("click-xp-funds.json",true,false,false,false);return (int)j["story"]["nativeClickCount"]==1&&(long)j["experience"]["delta"]==8900&&(long)j["hireFunds"]["delta"]==7900&&(int)j["experience"]["before"]["frame"]>(int)j["story"]["lastObserved"]["frame"]&&(int)j["hireFunds"]["before"]["level"]==4&&!(bool)j["story"]["attempts"][0]["systemButton"];});
 Check("actual positive XP with funds already enough",()=>{var j=Success("xp-funds-sufficient.json",true,false,false,true);return (long)j["experience"]["delta"]==8900&&(bool)j["hireFunds"]["noOp"]&&(long)j["hireFunds"]["before"]["money"]==10000;});
 Check("actual first disabled offer is skipped",()=>{var j=Success("skip-disabled-first.json",true,true,false,false);return (int)j["story"]["attempts"][0]["offerIndex"]==1&&(bool)j["story"]["attempts"][0]["before"]["rowDisabled"][0];});
 Check("actual system-button click records no row",()=>{var j=Success("system-button.json",true,false,false,false,true);var a=j["story"]["attempts"][0];return (int)j["story"]["nativeClickCount"]==1&&(bool)a["systemButton"]&&(int)a["offerIndex"]==0&&(int)a["rowIndex"]==-1&&(int)a["nativeIndex"]==-1&&(int)a["row"]==0&&((JArray)a["before"]["rowObjects"]).Count==0&&(int)a["answer"]==(int)a["before"]["offerObjects"][0]&&(long)j["experience"]["delta"]==8900;});
 Check("actual same-cue re-entry clicks the next unused offer",()=>{var j=SameCueReentry("same-cue-reentry.json");var first=j["story"]["attempts"][0];var second=j["story"]["attempts"][1];
  return (int)j["story"]["nativeClickCount"]==2&&(int)j["story"]["attemptCount"]==2&&(int)first["offerIndex"]==0&&(int)second["offerIndex"]==1&&
   (int)second["before"]["cue"]==(int)first["before"]["cue"]&&(int)second["before"]["dialog"]==(int)first["before"]["dialog"]&&(int)second["before"]["controller"]==(int)first["before"]["controller"]&&
   (bool)second["before"]["dialogActive"]&&(int)second["before"]["frame"]>(int)first["after"]["frame"]&&(int)second["answer"]!=(int)first["answer"]&&
   !(bool)second["systemButton"]&&!(bool)second["isExitAnswer"]&&!(bool)second["isContinueAnswer"]&&!(bool)second["continueCueDisplayed"];});
 Check("actual partial native XP failure is retained negative",()=>{var r=new Rig();Default(r);using(var d=r.Driver()){d.Tick();r.Short.Frame=r.State.Frame+1;r.OnXp=x=>{throw new InvalidOperationException("generated native XP event failed after mutation");};var a=r.Assistance();if(!Throws(()=>a.EnsureExperience()))return false;var j=Emit("partial-native-failure.json",r,d,a);return (bool)j["shortfallFailed"]&&(bool)j["experience"]["nativeEntered"]&&!(bool)j["experience"]["nativeReturned"]&&j["experience"]["after"].Type==JTokenType.Null&&(string)j["experience"]["failure"]!=null&&r.Short.Experience==9000&&r.XpCalls==1;}});
 Console.WriteLine("ASSISTED ACTUAL EMITTER: "+passed+" PASS / "+failed+" FAIL");return failed==0?0:1;
 }}
