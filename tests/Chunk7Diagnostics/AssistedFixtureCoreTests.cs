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
  internal Rig(){var game=new object();var player=new object();var mgr=new object();var main=new object();var desc=new object();var auth=new object();Binding=new Chunk7AssistedFixtureBinding("test-run",Guid.NewGuid().ToString(),"KMC owned",G(100),game,player,mgr,main,desc,auth,1);
   var id=new Chunk7AssistedIdentity{Game=game,Player=player,SaveManager=mgr,Main=main,Descriptor=desc,Authority=auth,RunId=Binding.RunId,CampaignId=Binding.CampaignId,CampaignName=Binding.CampaignName,MainId=Binding.MainId,Generation=1,ExactIsolatedFreshCampaign=true,NoForeignOrMountedCustody=true};
   State=new Chunk7AssistedStorySnapshot{Identity=id,Frame=10,ElapsedMilliseconds=0,Mode="Dialog",UiStatus="pc-cached-rows",MouseController=true,DialogActive=true,MainViewReady=true,
    Controller=new object(),Dialog=new object(),Cue=new object(),Ui=new object(),Common=new object(),Manager=new object(),PcDialog=new object(),CurrentPart=new object(),OffersList=new object(),RowsList=new object(),ManagerKind=0,ManagerShown=true,PcActive=true,DialogGuid=G(1),CueGuid=G(2),
    Offers=new[]{new Chunk7AssistedStoryOffer{Answer=new object(),Selection=new object(),Guid=G(3),SelectionType=3},new Chunk7AssistedStoryOffer{Answer=new object(),Selection=new object(),Guid=G(4),SelectionType=0}},Rows=new Chunk7AssistedStoryRow[2]};
   for(int i=0;i<2;i++)State.Rows[i]=new Chunk7AssistedStoryRow{View=new object(),Answer=State.Offers[i].Answer,Guid=State.Offers[i].Guid,NativeIndex=i+1,Initialized=true,ActiveAndEnabled=true,ActiveInHierarchy=true};
   Short=new Chunk7AssistedShortfallSnapshot{Identity=Copy(id),Frame=10,Experience=100,Level=1,ExperienceRate=100,TargetLevel=4,TargetThreshold=9000,NativeHireCost=8000,Money=100,ReadyDefault=true,LivingIdleField=true,PreservedStateSha256=new string('a',64)};
  }
  Chunk7AssistedStorySnapshot IChunk7AssistedStoryBoundary.Read(){ReadCount++;if(FailReadAfterClick&&Clicks>0)throw new Exception("after read unavailable");return Clone(State);}
  public void Click(Chunk7AssistedStorySnapshot expected,int oi,int ri,Action enter){if(BeforeClick!=null)BeforeClick(this);var actual=Clone(State);Binding.Require(actual.Identity);Chunk7AssistedStoryDriver.RequireSameClickState(expected,actual);if(!EntryMissing)enter();Clicks++;if(OnClick!=null)OnClick(this);}
  Chunk7AssistedShortfallSnapshot IChunk7AssistedShortfallBoundary.Read(){var v=Copy(Short);v.Identity=Copy(Short.Identity);return v;}
  public void GainExperience(Chunk7AssistedShortfallSnapshot expected,int delta,Action enter){Chunk7AssistedShortfall.RequireSameInput(expected,Short);enter();XpCalls++;Short.Experience+=delta;if(OnXp!=null)OnXp(this);}
  public void GainMoney(Chunk7AssistedShortfallSnapshot expected,long delta,Action enter){Chunk7AssistedShortfall.RequireSameInput(expected,Short);enter();MoneyCalls++;Short.Money+=delta;if(OnMoney!=null)OnMoney(this);}
  internal Chunk7AssistedStoryDriver Driver(){return new Chunk7AssistedStoryDriver(Binding,this);}internal Chunk7AssistedShortfall Assistance(){return new Chunk7AssistedShortfall(Binding,this);}
 }
 static bool BadStory(Action<Rig> mutate){var r=new Rig();mutate(r);using(var d=r.Driver())return Throws(()=>d.Tick())&&r.Clicks==0;}
 static bool WaitsStory(Action<Rig> mutate){var r=new Rig();mutate(r);using(var d=r.Driver())return d.Tick()==Chunk7AssistedStoryStatus.Waiting&&r.Clicks==0;}
 static void Interchapter(Rig r){r.State.ManagerKind=2;r.State.UiStatus="interchapter-rows";r.State.Offers=new[]{r.State.Offers[0]};r.State.Rows=new[]{r.State.Rows[0]};r.State.Rows[0].Interchapter=true;}
 static bool Stale(Action<Rig> mutate){var r=new Rig();r.BeforeClick=mutate;using(var d=r.Driver())return Throws(()=>d.Tick())&&r.Clicks==0&&d.AttemptCount==1&&!d.Attempts[0].NativeEntered;}
 static bool BadXp(Action<Rig> mutate){var r=new Rig();mutate(r);var a=r.Assistance();return Throws(()=>a.EnsureExperience())&&r.XpCalls==0;}
 static bool BadMoney(Action<Rig> mutate){var r=new Rig();r.Short.Level=4;mutate(r);var a=r.Assistance();return Throws(()=>a.EnsureOneNativeHireCost())&&r.MoneyCalls==0;}
 static int Main()
 {
  Check("first eligible uses offered order not row order",()=>{var r=new Rig();Array.Reverse(r.State.Rows);using(var d=r.Driver()){return d.Tick()==Chunk7AssistedStoryStatus.Clicked&&d.Attempts[0].OfferIndex==0&&d.Attempts[0].RowIndex==1&&d.Attempts[0].NativeIndex==1&&d.NativeClickCount==1;}});
  Check("disabled first selects next owned cached-enabled",()=>{var r=new Rig();r.State.Rows[0].Disabled=true;using(var d=r.Driver())return d.Tick()==Chunk7AssistedStoryStatus.Clicked&&d.Attempts[0].OfferIndex==1;});
  Check("same frame and unchanged next frame never repeat",()=>{var r=new Rig();using(var d=r.Driver()){d.Tick();d.Tick();r.State.Frame++;d.Tick();return r.Clicks==1&&d.NativeClickCount==1;}});
  Check("later cue progress permits next distinct click",()=>{var r=new Rig();using(var d=r.Driver()){d.Tick();r.State.Frame++;r.State.Cue=new object();r.State.CueGuid=G(8);d.Tick();return r.Clicks==2;}});
  Check("return to prior exact context refuses replay",()=>{var r=new Rig();var cue=r.State.Cue;using(var d=r.Driver()){d.Tick();r.State.Frame++;r.State.Cue=new object();d.Tick();r.State.Frame++;r.State.Cue=cue;return Throws(()=>d.Tick())&&r.Clicks==2;}});
  Check("scheduled native transition waits without click",()=>{var r=new Rig();r.State.TransitionPending=true;using(var d=r.Driver())return d.Tick()==Chunk7AssistedStoryStatus.Waiting&&r.Clicks==0;});
  Check("native busy waits without click",()=>{var r=new Rig();r.State.NativeBusy=true;using(var d=r.Driver())return d.Tick()==Chunk7AssistedStoryStatus.Waiting&&r.Clicks==0;});
  Check("native cutscene waits without click",()=>{var r=new Rig();r.State.Mode="Cutscene";r.State.Cutscene=true;using(var d=r.Driver())return d.Tick()==Chunk7AssistedStoryStatus.Waiting&&r.Clicks==0;});
  Check("Default current frame is observational ready only",()=>{var r=new Rig();r.State.Mode="Default";r.State.DialogActive=false;using(var d=r.Driver()){var s=d.Tick();r.State.Mode="Dialog";r.State.DialogActive=true;r.State.Frame++;return s==Chunk7AssistedStoryStatus.ReadyAtObservedFrame&&d.Tick()==Chunk7AssistedStoryStatus.Clicked;}});
  Check("pending Default is not ready",()=>{var r=new Rig();r.State.Mode="Default";r.State.DialogActive=false;r.State.TransitionPending=true;using(var d=r.Driver())return d.Tick()==Chunk7AssistedStoryStatus.Waiting;});
  Check("missing PC readiness waits finitely",()=>{var r=new Rig();r.State.UiStatus="missing-current-part";using(var d=r.Driver()){d.Tick();r.State.ElapsedMilliseconds=30001;return Throws(()=>d.Tick())&&r.Clicks==0;}});
  Check("total deadline before any native click",()=>BadStory(r=>r.State.ElapsedMilliseconds=300001));
  Check("no native progress deadline retains one click",()=>{var r=new Rig();using(var d=r.Driver()){d.Tick();r.State.ElapsedMilliseconds=30001;r.State.Frame++;return Throws(()=>d.Tick())&&r.Clicks==1;}});
  Check("choice cap16 before seventeenth callback",()=>{var r=new Rig();using(var d=r.Driver()){for(int i=0;i<16;i++){r.State.Frame++;r.State.Cue=new object();d.Tick();}r.State.Frame++;r.State.Cue=new object();return Throws(()=>d.Tick())&&r.Clicks==16;}});
  Check("native exception after mutation is retained without retry",()=>{var r=new Rig();r.OnClick=x=>{throw new Exception("native after mutation");};using(var d=r.Driver()){var e=Throws(()=>d.Tick());var n=Throws(()=>d.Tick());return e&&n&&r.Clicks==1&&d.Attempts[0].NativeEntered&&!d.Attempts[0].NativeReturned&&d.Attempts[0].Failure!=null;}});
  Check("native return then missing after snapshot remains FAIL",()=>{var r=new Rig();r.FailReadAfterClick=true;using(var d=r.Driver())return Throws(()=>d.Tick())&&d.Attempts[0].NativeReturned&&!d.Attempts[0].AfterReadSucceeded&&r.Clicks==1;});
  Check("entry witness missing cannot claim native success",()=>{var r=new Rig();r.EntryMissing=true;using(var d=r.Driver())return Throws(()=>d.Tick())&&!d.Attempts[0].NativeEntered&&!d.Attempts[0].NativeReturned;});
  Check("reentrant callback refuses without second dispatch",()=>{var r=new Rig();using(var d=r.Driver()){r.OnClick=x=>d.Tick();return Throws(()=>d.Tick())&&r.Clicks==1;}});
  Check("dispose during native callback retains custody",()=>{var r=new Rig();using(var d=r.Driver()){r.OnClick=x=>d.Dispose();return Throws(()=>d.Tick())&&r.Clicks==1&&d.Status==Chunk7AssistedStoryStatus.Failed;}});
  Check("disposed driver refuses input",()=>{var r=new Rig();var d=r.Driver();d.Dispose();return Throws(()=>d.Tick())&&r.Clicks==0;});
  Check("manual chooser refuses",()=>BadStory(r=>r.State.Offers[0].SelectionType=4));
  Check("unknown selection enum refuses",()=>BadStory(r=>r.State.Offers[0].SelectionType=6));
  Check("missing character selection refuses",()=>BadStory(r=>r.State.Offers[0].Selection=null));
  Check("nullable selection refuses",()=>BadStory(r=>r.State.Offers[0].SelectionType=null));
  Check("interchapter page clicks its single option",()=>{var r=new Rig();Interchapter(r);using(var d=r.Driver())return d.Tick()==Chunk7AssistedStoryStatus.Clicked&&r.Clicks==1&&d.LastObserved.ManagerKind==2&&d.LastObserved.UiStatus=="interchapter-rows"&&d.LastObserved.RowInterchapter[0];});
  Check("book page clicks first cached-enabled answer",()=>{var r=new Rig();r.State.ManagerKind=1;r.State.UiStatus="book-rows";using(var d=r.Driver())return d.Tick()==Chunk7AssistedStoryStatus.Clicked&&d.Attempts[0].OfferIndex==0;});
  Check("unsupported dialog kind refuses",()=>BadStory(r=>{r.State.ManagerKind=3;r.State.UiStatus="unsupported-dialog-kind";}));
  Check("kind without its selected status waits",()=>WaitsStory(r=>r.State.ManagerKind=2));
  Check("common status under book kind waits",()=>WaitsStory(r=>r.State.ManagerKind=1));
  Check("missing kind waits",()=>WaitsStory(r=>r.State.ManagerKind=null));
  Check("book page refuses interchapter rows",()=>BadStory(r=>{r.State.ManagerKind=1;r.State.UiStatus="book-rows";r.State.Rows[0].Interchapter=true;}));
  Check("kind change at dispatch refuses",()=>Stale(r=>{r.State.ManagerKind=1;r.State.UiStatus="book-rows";}));
  Check("interchapter progress bound still retains one click",()=>{var r=new Rig();Interchapter(r);using(var d=r.Driver()){d.Tick();r.State.ElapsedMilliseconds=30001;r.State.Frame++;return Throws(()=>d.Tick())&&r.Clicks==1;}});
  Check("foreign nonstory popup refuses",()=>BadStory(r=>r.State.UnexpectedPopup=true));
  Check("console input refuses",()=>BadStory(r=>r.State.MouseController=false));
  Check("unknown mode refuses",()=>BadStory(r=>r.State.Mode="GlobalMap"));
  Check("uninitialized row refuses",()=>BadStory(r=>r.State.Rows[0].Initialized=false));
  Check("row character chooser refuses",()=>BadStory(r=>r.State.Rows[0].ChoosingCharacter=true));
  Check("row interchapter refuses",()=>BadStory(r=>r.State.Rows[0].Interchapter=true));
  Check("foreign row answer refuses",()=>BadStory(r=>r.State.Rows[0].Answer=new object()));
  Check("duplicate row reference refuses",()=>BadStory(r=>r.State.Rows[1].View=r.State.Rows[0].View));
  Check("duplicate offered reference refuses",()=>BadStory(r=>r.State.Offers[1].Answer=r.State.Offers[0].Answer));
  Check("duplicate answer GUID refuses",()=>BadStory(r=>r.State.Offers[1].Guid=r.State.Offers[0].Guid));
  Check("duplicate row index refuses",()=>BadStory(r=>r.State.Rows[1].NativeIndex=r.State.Rows[0].NativeIndex));
  Check("malformed GUID refuses",()=>BadStory(r=>r.State.Offers[0].Guid="bad"));
  Check("inactive first row skips to eligible active row",()=>{var r=new Rig();r.State.Rows[0].ActiveInHierarchy=false;using(var d=r.Driver())return d.Tick()==Chunk7AssistedStoryStatus.Clicked&&d.Attempts[0].OfferIndex==1;});
  Check("all disabled waits",()=>{var r=new Rig();foreach(var x in r.State.Rows)x.Disabled=true;using(var d=r.Driver())return d.Tick()==Chunk7AssistedStoryStatus.Waiting&&r.Clicks==0;});
  Check("overbound offer list refuses",()=>BadStory(r=>r.State.Offers=new Chunk7AssistedStoryOffer[33]));
  Check("overbound rows refuse",()=>BadStory(r=>r.State.Rows=new Chunk7AssistedStoryRow[33]));
  Check("foreign game refuses",()=>BadStory(r=>r.State.Identity.Game=new object()));
  Check("foreign player refuses",()=>BadStory(r=>r.State.Identity.Player=new object()));
  Check("foreign main refuses",()=>BadStory(r=>r.State.Identity.Main=new object()));
  Check("foreign descriptor refuses",()=>BadStory(r=>r.State.Identity.Descriptor=new object()));
  Check("foreign manager refuses",()=>BadStory(r=>r.State.Identity.SaveManager=new object()));
  Check("foreign campaign refuses",()=>BadStory(r=>r.State.Identity.CampaignId=Guid.NewGuid().ToString()));
  Check("foreign authority refuses",()=>BadStory(r=>r.State.Identity.Authority=new object()));
  Check("wrong generation refuses",()=>BadStory(r=>r.State.Identity.Generation++));
  Check("unqualified setup refuses",()=>BadStory(r=>r.State.Identity.ExactIsolatedFreshCampaign=false));
  Check("mounted custody refuses",()=>BadStory(r=>r.State.Identity.NoForeignOrMountedCustody=false));
  Check("stale frame refuses at dispatch",()=>Stale(r=>r.State.Frame++));
  Check("stale controller refuses at dispatch",()=>Stale(r=>r.State.Controller=new object()));
  Check("stale dialog refuses at dispatch",()=>Stale(r=>r.State.Dialog=new object()));
  Check("stale cue refuses at dispatch",()=>Stale(r=>r.State.Cue=new object()));
  Check("stale current part refuses at dispatch",()=>Stale(r=>r.State.CurrentPart=new object()));
  Check("same contents replacement offer list refuses",()=>Stale(r=>r.State.OffersList=new object()));
  Check("same contents replacement row list refuses",()=>Stale(r=>r.State.RowsList=new object()));
  Check("same enum replacement selection owner refuses",()=>Stale(r=>r.State.Offers[0].Selection=new object()));
  Check("newly disabled row refuses",()=>Stale(r=>r.State.Rows[0].Disabled=true));
  Check("new chooser state refuses",()=>Stale(r=>r.State.Rows[0].ChoosingCharacter=true));
  Check("new pending transition refuses",()=>Stale(r=>r.State.TransitionPending=true));
  Check("changed global main refuses",()=>Stale(r=>r.State.Identity.Main=new object()));
  Check("exact XP shortfall only",()=>{var r=new Rig();var a=r.Assistance();var e=a.EnsureExperience();return r.XpCalls==1&&e.Delta==8900&&r.Short.Experience==9000&&e.NativeEntered&&e.NativeReturned&&e.AfterReadSucceeded;});
  Check("XP already sufficient makes no native call",()=>{var r=new Rig();r.Short.Experience=9000;var e=r.Assistance().EnsureExperience();return e.NoOp&&!e.Attempted&&r.XpCalls==0;});
  Check("XP step cannot be repeated",()=>{var r=new Rig();var a=r.Assistance();a.EnsureExperience();return Throws(()=>a.EnsureExperience())&&r.XpCalls==1;});
  Check("money exact current hire shortfall only",()=>{var r=new Rig();r.Short.Level=4;var e=r.Assistance().EnsureOneNativeHireCost();return e.Delta==7900&&r.Short.Money==8000&&r.MoneyCalls==1;});
  Check("money already sufficient makes no call",()=>{var r=new Rig();r.Short.Level=4;r.Short.Money=8001;var e=r.Assistance().EnsureOneNativeHireCost();return e.NoOp&&r.MoneyCalls==0&&r.Short.Money==8001;});
  Check("money step cannot repeat",()=>{var r=new Rig();r.Short.Level=4;var a=r.Assistance();a.EnsureOneNativeHireCost();return Throws(()=>a.EnsureOneNativeHireCost())&&r.MoneyCalls==1;});
  Check("wrong XP rate refuses before call",()=>BadXp(r=>r.Short.ExperienceRate=150));
  Check("pet main XP refuses",()=>BadXp(r=>r.Short.IsPet=true));
  Check("owned pet XP propagation refuses",()=>BadXp(r=>r.Short.HasPet=true));
  Check("dual companion XP propagation refuses",()=>BadXp(r=>r.Short.HasDualCompanion=true));
  Check("XP grant ceiling refuses",()=>BadXp(r=>r.Short.TargetThreshold=200101));
  Check("negative XP refuses",()=>BadXp(r=>r.Short.Experience=-1));
  Check("unbounded target level refuses",()=>BadXp(r=>r.Short.TargetLevel=9));
  Check("target below required level4 refuses",()=>BadXp(r=>r.Short.TargetLevel=3));
  Check("nonDefault assistance refuses",()=>BadXp(r=>r.Short.ReadyDefault=false));
  Check("foreign builder refuses",()=>BadXp(r=>r.Short.ForeignBuilder=true));
  Check("nonidle main refuses",()=>BadXp(r=>r.Short.LivingIdleField=false));
  Check("preservation observation missing refuses",()=>BadXp(r=>r.Short.PreservedStateSha256=null));
  Check("funds cannot replace attained level",()=>BadMoney(r=>r.Short.Level=3));
  Check("nonpositive native hire cost refuses",()=>BadMoney(r=>r.Short.NativeHireCost=0));
  Check("funds grant ceiling refuses",()=>BadMoney(r=>r.Short.NativeHireCost=100101));
  Check("negative current funds refuses",()=>BadMoney(r=>r.Short.Money=-1));
  Check("XP event unexpected level change fails after mutation",()=>{var r=new Rig();r.OnXp=x=>x.Short.Level++;var a=r.Assistance();return Throws(()=>a.EnsureExperience())&&r.XpCalls==1&&a.ExperienceEffect.NativeReturned&&Throws(()=>a.EnsureExperience());});
  Check("XP event budget state change fails after mutation",()=>{var r=new Rig();r.OnXp=x=>x.Short.PreservedStateSha256=new string('b',64);var a=r.Assistance();return Throws(()=>a.EnsureExperience())&&r.XpCalls==1;});
  Check("XP event exception no retry or refund",()=>{var r=new Rig();r.OnXp=x=>{throw new Exception("partial native effect");};var a=r.Assistance();return Throws(()=>a.EnsureExperience())&&Throws(()=>a.EnsureExperience())&&r.XpCalls==1&&r.Short.Experience==9000&&!a.ExperienceEffect.NativeReturned;});
  Check("money event exception no retry",()=>{var r=new Rig();r.Short.Level=4;r.OnMoney=x=>{throw new Exception("partial money");};var a=r.Assistance();return Throws(()=>a.EnsureOneNativeHireCost())&&Throws(()=>a.EnsureOneNativeHireCost())&&r.MoneyCalls==1&&r.Short.Money==8000;});
  Check("funds event changes unrelated XP is failure",()=>{var r=new Rig();r.Short.Level=4;r.OnMoney=x=>x.Short.Experience++;var a=r.Assistance();return Throws(()=>a.EnsureOneNativeHireCost())&&r.MoneyCalls==1;});
  Check("reentrant XP step cannot duplicate",()=>{var r=new Rig();var a=r.Assistance();r.OnXp=x=>a.EnsureExperience();return Throws(()=>a.EnsureExperience())&&r.XpCalls==1;});
  Check("explicit evidence retains setup limits and internal fields",()=>{var r=new Rig();using(var d=r.Driver()){d.Tick();var a=r.Assistance();a.EnsureExperience();var j=Chunk7AssistedFixtureEvidence.Capture(r.Binding,d,a);return (bool)j["fixtureSetupOnly"]&&!(bool)j["ordinaryAcquisitionProven"]&&!(bool)j["ordinaryProgressionProven"]&&!(bool)j["smallPoseQualified"]&&(long)j["experience"]["delta"]==8900&&(bool)j["story"]["attempts"][0]["nativeEntered"]&&j.ToString().Length<65536;}});
  Console.WriteLine("ASSISTED FIXTURE CORE: "+passed+" PASS / "+failed+" FAIL");return failed==0?0:1;
 }
}
