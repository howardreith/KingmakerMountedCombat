using System;using System.Collections.Generic;using System.Threading;using KingmakerMountedCombat.Diagnostics;
static class Tests {
 static int pass,fail;static void Check(string n,Func<bool> f){try{if(!f())throw new Exception("predicate false");pass++;Console.WriteLine("PASS "+n);}catch(Exception e){fail++;Console.WriteLine("FAIL "+n+" "+e);}}
 static bool Throws(Action a){try{a();return false;}catch{return true;}}
 static bool Stale(Action<FacadeRig> change){var r=new FacadeRig();var s=r.Boundary.ReadStory();change(r);return Throws(()=>r.Boundary.Click(s,0,0,r.Enter))&&r.ClickCount==0&&r.EntryCount==0;}
 static bool Refuses(Action<FacadeRig> change){var r=new FacadeRig();change(r);using(var d=r.Driver())return Throws(()=>d.Tick())&&r.ClickCount==0;}
 static bool Waits(Action<FacadeRig> change){var r=new FacadeRig();change(r);using(var d=r.Driver())return d.Tick()==Chunk7AssistedStoryStatus.Waiting&&r.ClickCount==0;}
 static Kingmaker.UI.Dialog.DialogBookEventUIManager Manager(FacadeRig r){return (Kingmaker.UI.Dialog.DialogBookEventUIManager)r.Manager;}
 static void Interchapter(FacadeRig r){r.Game.DialogController.Dialog.Type=Kingmaker.DialogSystem.DialogType.Interchapter;r.Kind=2;r.Game.DialogController.m_Answers.RemoveAt(1);var option=new Kingmaker.UI.Dialog.TempOptionUI{m_Answer=r.Game.DialogController.m_Answers[0],m_Index=1,m_Interchapter=true};r.Manager=new Kingmaker.UI.Dialog.DialogBookEventUIManager{m_State=Kingmaker.DialogSystem.DialogType.Interchapter,Interchapter=new Kingmaker.UI.BookEvent.BookEventInterchapterController{m_Answer=option}};}
 static void Book(FacadeRig r){r.Game.DialogController.Dialog.Type=Kingmaker.DialogSystem.DialogType.Book;r.Kind=1;var book=new Kingmaker.UI.BookEvent.BookEventController();for(int i=0;i<2;i++)book.Answers.Children.Add(new Kingmaker.UI.Dialog.TempOptionUI{m_Answer=r.Game.DialogController.m_Answers[i],m_Index=i+1});book.Answers.Children.Add(new Kingmaker.UI.Dialog.TempOptionUI{m_IsInit=false});r.Manager=new Kingmaker.UI.Dialog.DialogBookEventUIManager{m_State=Kingmaker.DialogSystem.DialogType.Book,BookEvent=book};}
 static int Main(){
 Check("actual adapter capture binds stored native objects",()=>{var r=new FacadeRig();var s=r.Boundary.ReadStory();return ReferenceEquals(s.Controller,r.Game.DialogController)&&ReferenceEquals(s.CurrentPart,r.Part)&&ReferenceEquals(s.OffersList,r.Game.DialogController.m_Answers)&&ReferenceEquals(s.RowsList,r.Part.m_AnswersList)&&s.Offers.Length==2&&s.Rows.Length==2&&s.Rows[0].NativeIndex==1&&r.MethodChecks==3&&Fixture.Predicates==0;});
 Check("native row call once after explicit witness",()=>{var r=new FacadeRig();r.RequireExplicitEntry=true;var s=r.Boundary.ReadStory();r.Boundary.Click(s,0,0,r.Enter);return r.ClickCount==1&&r.EntryCount==1&&Fixture.Predicates==0;});
 Check("actual driver adapter marks attempt before native callback",()=>{var r=new FacadeRig();using(var d=r.Driver()){r.ClickAction=()=>{if(!d.Attempts[0].NativeEntered||d.NativeClickCount!=1)throw new Exception("not marked");};return d.Tick()==Chunk7AssistedStoryStatus.Clicked&&r.ClickCount==1;}});
 Check("row native exception remains terminal no retry",()=>{var r=new FacadeRig();r.ClickAction=()=>{throw new Exception("native after mutation");};using(var d=r.Driver())return Throws(()=>d.Tick())&&Throws(()=>d.Tick())&&r.ClickCount==1&&d.Attempts[0].NativeEntered&&!d.Attempts[0].NativeReturned;});
 Check("native synchronous Default still waits a later frame",()=>{var r=new FacadeRig();r.ClickAction=r.Default;using(var d=r.Driver()){d.Tick();if(d.Tick()!=Chunk7AssistedStoryStatus.Waiting)return false;UnityEngine.Time.frameCount++;return d.Tick()==Chunk7AssistedStoryStatus.ReadyAtObservedFrame&&r.ClickCount==1;}});
 Check("foreign authority callback refuses before traversal",()=>{var r=new FacadeRig();r.AuthorityFail=true;return Throws(()=>r.Boundary.ReadStory())&&r.PassiveReads==0;});
 Check("foreign mounted custody refuses",()=>Refuses(r=>r.Mounted=true));
 Check("foreign global Game refuses",()=>Stale(r=>Kingmaker.Game.Instance=new Kingmaker.Game()));
 Check("foreign Player refuses",()=>Stale(r=>r.Game.Player=new Kingmaker.Player()));
 Check("foreign main refuses",()=>Stale(r=>r.Game.Player.MainCharacter.Value=new Kingmaker.EntitySystem.Entities.UnitEntityData()));
 Check("foreign descriptor refuses",()=>Stale(r=>r.Main.Descriptor=new Kingmaker.UnitLogic.UnitDescriptor()));
 Check("foreign save manager refuses",()=>Stale(r=>r.Game.SaveManager=new object()));
 Check("changed campaign refuses",()=>Stale(r=>r.Game.Player.GameId=Guid.NewGuid().ToString()));
 Check("changed native main ID refuses",()=>Stale(r=>r.Main.UniqueId=FacadeRig.G(70)));
 Check("changed native main name refuses",()=>Stale(r=>r.Main.CharacterName="foreign"));
 Check("wrong host thread refuses before passive reads",()=>{var r=new FacadeRig();bool refused=false;var t=new Thread(()=>refused=Throws(()=>r.Boundary.ReadStory()));t.Start();t.Join();return refused&&r.PassiveReads==0;});
 Check("changed frame refuses dispatch",()=>Stale(r=>UnityEngine.Time.frameCount++));
 Check("changed controller refuses dispatch",()=>Stale(r=>r.Game.DialogController=new Kingmaker.Controllers.Dialog.DialogController()));
 Check("changed dialog reference refuses",()=>Stale(r=>r.Game.DialogController.Dialog=new Kingmaker.DialogSystem.Blueprints.BlueprintDialog{AssetGuidThreadSafe=FacadeRig.G(2)}));
 Check("changed cue reference refuses",()=>Stale(r=>r.Game.DialogController.m_CurrentCue=new Kingmaker.DialogSystem.Blueprints.BlueprintCue{AssetGuidThreadSafe=FacadeRig.G(3)}));
 Check("same contents replacement offered List refuses",()=>Stale(r=>r.Game.DialogController.m_Answers=new List<Kingmaker.DialogSystem.Blueprints.BlueprintAnswer>(r.Game.DialogController.m_Answers)));
 Check("same contents replacement row List refuses",()=>Stale(r=>r.Part.m_AnswersList=new List<Kingmaker.UI.Dialog.TempOptionUI>(r.Part.m_AnswersList)));
 Check("same selection enum replacement object refuses",()=>Stale(r=>r.Game.DialogController.m_Answers[0].CharacterSelection=new Kingmaker.DialogSystem.CharacterSelection()));
 Check("current part replacement refuses",()=>Stale(r=>{var p=new Kingmaker.UI.Dialog.DialogCurrentPart();p.m_AnswersList=r.Part.m_AnswersList;r.Part=p;}));
 Check("UI replacement refuses",()=>Stale(r=>r.Game.UI=new Kingmaker.UI.UI()));
 Check("Common replacement refuses",()=>Stale(r=>r.Game.UI.Common=new object()));
 Check("manager replacement refuses",()=>Stale(r=>r.Manager=new object()));
 Check("PC dialog replacement refuses",()=>Stale(r=>r.Pc=new Kingmaker.UI.Dialog.DialogController()));
 Check("new cached disabled refuses",()=>Stale(r=>r.Part.m_AnswersList[0].m_Disabled=true));
 Check("new uninitialized row refuses",()=>Stale(r=>r.Part.m_AnswersList[0].m_IsInit=false));
 Check("new chooser refuses",()=>Stale(r=>r.Part.m_AnswersList[0].m_ChooseCharacterInit=true));
 Check("new inactive component refuses",()=>Stale(r=>r.Part.m_AnswersList[0].isActiveAndEnabled=false));
 Check("new inactive hierarchy refuses",()=>Stale(r=>r.Part.m_AnswersList[0].gameObject.activeInHierarchy=false));
 Check("null row gameObject refuses",()=>Stale(r=>r.Part.m_AnswersList[0].gameObject=null));
 Check("new popup refuses",()=>Stale(r=>Kingmaker.UI.DialogMessageBox.Instance=new Kingmaker.UI.DialogMessageBox{IsShown=true}));
 Check("console input refuses",()=>Stale(r=>r.Game.IsControllerMouse=false));
 Check("Manual chooser refuses",()=>Refuses(r=>r.Game.DialogController.m_Answers[0].CharacterSelection.SelectionType=Kingmaker.DialogSystem.CharacterSelection.Type.Manual));
 Check("null CharacterSelection refuses",()=>Refuses(r=>r.Game.DialogController.m_Answers[0].CharacterSelection=null));
 Check("interchapter page clicks its single option",()=>{var r=new FacadeRig();Interchapter(r);var s=r.Boundary.ReadStory();using(var d=r.Driver())return s.UiStatus=="interchapter-rows"&&s.ManagerKind==2&&s.Rows.Length==1&&s.Rows[0].Interchapter&&ReferenceEquals(s.PcDialog,Manager(r).Interchapter)&&ReferenceEquals(s.CurrentPart,Manager(r).Interchapter.gameObject)&&ReferenceEquals(s.RowsList,Manager(r).Interchapter.transform)&&d.Tick()==Chunk7AssistedStoryStatus.Clicked&&r.ClickCount==1&&d.Attempts[0].RowIndex==0;});
 Check("book page clicks first initialized enabled answer",()=>{var r=new FacadeRig();Book(r);var s=r.Boundary.ReadStory();using(var d=r.Driver())return s.UiStatus=="book-rows"&&s.ManagerKind==1&&s.Rows.Length==2&&ReferenceEquals(s.RowsList,Manager(r).BookEvent.Answers)&&d.Tick()==Chunk7AssistedStoryStatus.Clicked&&r.ClickCount==1&&d.Attempts[0].OfferIndex==0;});
 Check("book chooser row refuses",()=>Refuses(r=>{Book(r);Manager(r).BookEvent.Answers.Children[0].m_ChooseCharacterInit=true;}));
 Check("book disabled first answer selects the next enabled answer",()=>{var r=new FacadeRig();Book(r);Manager(r).BookEvent.Answers.Children[0].m_Disabled=true;using(var d=r.Driver())return d.Tick()==Chunk7AssistedStoryStatus.Clicked&&d.Attempts[0].OfferIndex==1;});
 Check("unsupported native kind refuses",()=>Refuses(r=>{r.Game.DialogController.Dialog.Type=(Kingmaker.DialogSystem.DialogType)3;r.Kind=3;}));
 Check("blueprint and manager kinds disagree refuses",()=>Refuses(r=>{Interchapter(r);r.Kind=0;}));
 Check("hidden interchapter manager waits",()=>Waits(r=>{Interchapter(r);r.Shown=false;}));
 Check("inactive interchapter view waits",()=>Waits(r=>{Interchapter(r);Manager(r).Interchapter.isActiveAndEnabled=false;}));
 Check("missing interchapter view waits",()=>Waits(r=>{Interchapter(r);Manager(r).Interchapter=null;}));
 Check("interchapter option replacement refuses dispatch",()=>{var r=new FacadeRig();Interchapter(r);var s=r.Boundary.ReadStory();Manager(r).Interchapter.m_Answer=new Kingmaker.UI.Dialog.TempOptionUI{m_Answer=r.Game.DialogController.m_Answers[0],m_Index=1,m_Interchapter=true};return Throws(()=>r.Boundary.Click(s,0,0,r.Enter))&&r.ClickCount==0&&r.EntryCount==0;});
 Check("interchapter view replacement refuses dispatch",()=>{var r=new FacadeRig();Interchapter(r);var s=r.Boundary.ReadStory();var option=Manager(r).Interchapter.m_Answer;Manager(r).Interchapter=new Kingmaker.UI.BookEvent.BookEventInterchapterController{m_Answer=option};return Throws(()=>r.Boundary.Click(s,0,0,r.Enter))&&r.ClickCount==0;});
 Check("common page still refuses interchapter rows",()=>Refuses(r=>r.Part.m_AnswersList[0].m_Interchapter=true));
 Check("kind change between read and dispatch refuses",()=>{var r=new FacadeRig();var s=r.Boundary.ReadStory();Interchapter(r);return Throws(()=>r.Boundary.Click(s,0,0,r.Enter))&&r.ClickCount==0;});
 Check("native busy waits",()=>Waits(r=>r.Busy=true));
 Check("scheduled StartDialog waits",()=>Waits(r=>r.Game.Player.Dialog.Scheduled=new Kingmaker.DialogSystem.StartDialogData()));
 Check("queued cue waits",()=>Waits(r=>r.Game.DialogController.m_CueToPlay=new Kingmaker.DialogSystem.Blueprints.BlueprintCue()));
 Check("continue cue waits",()=>Waits(r=>r.Game.DialogController.m_ContinueCue=new Kingmaker.DialogSystem.Blueprints.BlueprintCue()));
 Check("cue-play schedule waits",()=>Waits(r=>r.Game.DialogController.m_CuePlayScheduled=true));
 Check("dialog-stop schedule waits",()=>Waits(r=>r.Game.DialogController.m_DialogStopScheduled=true));
 Check("new scheduled transition refuses immediate dispatch",()=>Stale(r=>r.Game.Player.Dialog.Scheduled=new Kingmaker.DialogSystem.StartDialogData()));
 Check("native offered order change during passive capture refuses",()=>Refuses(r=>r.AfterPassive=()=>r.Game.DialogController.m_Answers.Reverse()));
 Check("native row cache changes during passive capture refuses",()=>Refuses(r=>r.AfterPassive=()=>r.Part.m_AnswersList[0].m_Disabled=true));
 Check("frame changes during passive capture refuses",()=>Refuses(r=>r.AfterPassive=()=>UnityEngine.Time.frameCount++));
 Check("global controller changes during passive capture refuses",()=>Refuses(r=>r.AfterPassive=()=>r.Game.DialogController=new Kingmaker.Controllers.Dialog.DialogController()));
 Check("passive read error refuses instead of supplying authority",()=>Refuses(r=>r.RawError="read failure"));
 Check("row and offer count bounds refuse",()=>Refuses(r=>{while(r.Game.DialogController.m_Answers.Count<33)r.Game.DialogController.m_Answers.Add(r.Game.DialogController.m_Answers[0]);}));
 Check("native XP boundary exact delta and false log argument",()=>{var r=new FacadeRig();r.Default();r.RequireExplicitEntry=true;var s=r.Boundary.ReadShortfall();r.Boundary.GainExperience(s,8900,r.Enter);return r.XpCount==1&&r.XpDelta==8900&&!r.XpLog&&r.Main.Descriptor.Progression.Experience==9000&&r.TableLevel==4;});
 Check("native money boundary exact observed hire-cost delta",()=>{var r=new FacadeRig();r.Default();r.Main.Descriptor.Progression.CharacterLevel=4;r.RequireExplicitEntry=true;var s=r.Boundary.ReadShortfall();r.Boundary.GainMoney(s,7900,r.Enter);return r.MoneyCount==1&&r.MoneyDelta==7900&&r.Game.Player.Money==8000;});
 Check("XP native event exception retained once with service",()=>{var r=new FacadeRig();r.Default();r.XpAction=()=>{throw new Exception("native effect exception");};var a=r.Shortfall();return Throws(()=>a.EnsureExperience())&&Throws(()=>a.EnsureExperience())&&r.XpCount==1&&a.ExperienceEffect.NativeEntered&&!a.ExperienceEffect.NativeReturned;});
 Check("money native exception retained once with service",()=>{var r=new FacadeRig();r.Default();r.Main.Descriptor.Progression.CharacterLevel=4;r.MoneyAction=()=>{throw new Exception("native effect exception");};var a=r.Shortfall();return Throws(()=>a.EnsureOneNativeHireCost())&&Throws(()=>a.EnsureOneNativeHireCost())&&r.MoneyCount==1;});
 Check("money changed at pre-call recheck refuses",()=>{var r=new FacadeRig();r.Default();r.Main.Descriptor.Progression.CharacterLevel=4;var s=r.Boundary.ReadShortfall();r.Game.Player.Money++;return Throws(()=>r.Boundary.GainMoney(s,7900,r.Enter))&&r.MoneyCount==0&&r.EntryCount==0;});
 Check("XP changed at pre-call recheck refuses",()=>{var r=new FacadeRig();r.Default();var s=r.Boundary.ReadShortfall();r.Main.Descriptor.Progression.Experience++;return Throws(()=>r.Boundary.GainExperience(s,8900,r.Enter))&&r.XpCount==0;});
 Check("native XP no battlelog or actions fabricated",()=>{var r=new FacadeRig();r.Default();var a=r.Shortfall();a.EnsureExperience();return r.XpCount==1&&!r.XpLog&&Fixture.Predicates==0&&r.ClickCount==0&&r.MoneyCount==0;});
 Check("changed native preservation refuses before money entry",()=>{var r=new FacadeRig();r.Default();r.Main.Descriptor.Progression.CharacterLevel=4;var s=r.Boundary.ReadShortfall();r.Preserved=new string('b',64);return Throws(()=>r.Boundary.GainMoney(s,7900,r.Enter))&&r.MoneyCount==0;});
 Check("actual native pet main read refuses XP",()=>{var r=new FacadeRig();r.Default();r.Main.Descriptor.Master.Value=new Kingmaker.EntitySystem.Entities.UnitEntityData();return Throws(()=>r.Shortfall().EnsureExperience())&&r.XpCount==0;});
 Check("actual native dual companion read refuses XP",()=>{var r=new FacadeRig();r.Default();r.Main.Dual=new Kingmaker.UnitLogic.Parts.UnitPartDualCompanion();return Throws(()=>r.Shortfall().EnsureExperience())&&r.XpCount==0;});
 Check("actual builder occupancy refuses assistance",()=>{var r=new FacadeRig();r.Default();r.Game.UI.CharacterBuildController.Unit=new object();return Throws(()=>r.Shortfall().EnsureExperience())&&r.XpCount==0;});
 Check("actual missing main view refuses assistance",()=>{var r=new FacadeRig();r.Default();r.Main.View=null;return Throws(()=>r.Shortfall().EnsureExperience())&&r.XpCount==0;});
 Console.WriteLine("ASSISTED NATIVE FACADE: "+pass+" PASS / "+fail+" FAIL");return fail==0?0:1;
 }}
