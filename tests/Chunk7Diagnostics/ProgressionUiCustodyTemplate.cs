using System;
using System.Collections.Generic;
using System.Linq;

class Surface { public bool activeSelf=true; }
class Group { public Surface gameObject=new Surface(); public int Closes; public bool Sticky; public void Close(){Closes++;if(!Sticky)gameObject.activeSelf=false;} }
class Selector { public Surface gameObject=new Surface(); public bool Shown=true; public int Closes; public void OnButtonClose(){Closes++;Shown=false;} }
class Canvas { public Group GroupManager; }
class Ui { public Canvas Canvas=new Canvas(); }
class Game { public static Game Instance=new Game(); public Ui UI=new Ui(); }
class DialogMessageBox { public static DialogMessageBox Instance; public bool IsShown; public bool Expected=true,ButtonReady=true,Sticky; public int NoClicks; }
class Probe { public bool InSynchronousCallback; }
class ActualUiCleanup {
    private bool disposed,confirmationInProgress,dialogOwned,groupOwned,selectorOwned,confirmationSent,busy,wrongScope;
    private Action assertScope; private Func<bool> nativeBusy; private Probe dismissal,respec;
    private DialogMessageBox dialog; private Group group; private Selector selector,currentSelector;
    private int readinessChecks;
    private ActualUiCleanup(){Game.Instance=new Game();DialogMessageBox.Instance=null;assertScope=()=>{if(wrongScope)throw new InvalidOperationException("foreign scope");};nativeBusy=()=>busy;}
    private void RequireExpectedDialog(){if(!dialog.Expected)throw new InvalidOperationException("foreign callback");}
    private object ReadReference(object value,Type type,string name,int token,string expectedType){return value;}
    private bool ReadyButton(object button){readinessChecks++;return ((DialogMessageBox)button).ButtonReady;}
    private void InvokeClick(object button){var value=(DialogMessageBox)button;value.NoClicks++;if(!value.Sticky)value.IsShown=false;}
    private void BeforeMutation(){assertScope();if(nativeBusy())throw new InvalidOperationException("busy");}
    private Selector ExactSelector(){return currentSelector;}
    private bool SelectorShown(){return selector.Shown;}
    private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
/*ACTUAL_UI_DRAIN*/
    private void OwnGroup(){groupOwned=true;group=new Group();Game.Instance.UI.Canvas.GroupManager=group;}
    private void OwnSelector(){selectorOwned=true;selector=currentSelector=new Selector();}
    private void OwnDialog(){dialogOwned=true;dialog=DialogMessageBox.Instance=new DialogMessageBox{IsShown=true};}
    private static int passed,failed;
    private static void Check(string name,Action<ActualUiCleanup> test){var f=new ActualUiCleanup();try{test(f);passed++;Console.WriteLine("PASS "+name);}catch(Exception error){failed++;Console.WriteLine("FAIL "+name+": "+error);}}
    private static void Assert(bool value){if(!value)throw new InvalidOperationException("assertion failed");}
    private static void Refused(Func<bool> action){bool refused=false;try{action();}catch(InvalidOperationException){refused=true;}Assert(refused);}
    internal static int Verify(){
        Check("ordinary owned group closes exactly once",f=>{f.OwnGroup();Assert(f.DrainUi()&&!f.groupOwned&&f.group.Closes==1);Assert(f.DrainUi()&&f.group.Closes==1);});
        Check("foreign scope blocks group mutation",f=>{f.OwnGroup();f.wrongScope=true;Refused(f.DrainUi);Assert(f.groupOwned&&f.group.Closes==0);});
        Check("native worker preserves owned group",f=>{f.OwnGroup();f.busy=true;Assert(!f.DrainUi()&&f.groupOwned&&f.group.Closes==0);});
        Check("synchronous dismissal callback preserves UI",f=>{f.OwnGroup();f.dismissal=new Probe{InSynchronousCallback=true};Assert(!f.DrainUi()&&f.group.Closes==0);});
        Check("synchronous respec callback preserves UI",f=>{f.OwnSelector();f.respec=new Probe{InSynchronousCallback=true};Assert(!f.DrainUi()&&f.selector.Closes==0);});
        Check("unexpected unowned dialog blocks all UI cleanup",f=>{f.OwnGroup();DialogMessageBox.Instance=new DialogMessageBox{IsShown=true};Refused(f.DrainUi);Assert(f.groupOwned&&f.group.Closes==0);});
        Check("same dialog with lost ownership blocks cleanup",f=>{f.OwnGroup();f.OwnDialog();f.dialogOwned=false;Refused(f.DrainUi);Assert(f.group.Closes==0&&f.dialog.NoClicks==0);});
        Check("replacement shown dialog blocks captured group",f=>{f.OwnGroup();f.OwnDialog();DialogMessageBox.Instance=new DialogMessageBox{IsShown=true};Refused(f.DrainUi);Assert(f.group.Closes==0&&f.dialog.NoClicks==0);});
        Check("replacement hidden dialog still retains ownership",f=>{f.OwnDialog();DialogMessageBox.Instance=new DialogMessageBox();Refused(f.DrainUi);Assert(f.dialogOwned&&f.dialog.NoClicks==0);});
        Check("exact expected dismissal No is native single click",f=>{f.OwnDialog();Assert(f.DrainUi()&&!f.dialogOwned&&f.dialog.NoClicks==1);Assert(f.DrainUi()&&f.dialog.NoClicks==1);});
        Check("foreign native confirmation callback remains untouched",f=>{f.OwnDialog();f.dialog.Expected=false;Refused(f.DrainUi);Assert(f.dialogOwned&&f.dialog.NoClicks==0);});
        Check("unready native No preserves dialog",f=>{f.OwnDialog();f.dialog.ButtonReady=false;Assert(!f.DrainUi()&&f.dialogOwned&&f.dialog.NoClicks==0);});
        Check("already confirmed dismissal never clicks No",f=>{f.OwnDialog();f.confirmationSent=true;Assert(!f.DrainUi()&&f.dialogOwned&&f.dialog.NoClicks==0);});
        Check("replacement group remains untouched",f=>{f.OwnGroup();Game.Instance.UI.Canvas.GroupManager=new Group();Refused(f.DrainUi);Assert(f.groupOwned&&f.group.Closes==0);});
        Check("native group pending deactivation retains UI",f=>{f.OwnGroup();f.group.Sticky=true;Assert(!f.DrainUi()&&f.groupOwned);});
        Check("respec native disappearance remains owned without repeat Close",f=>{f.OwnSelector();Assert(!f.DrainUi()&&f.selectorOwned&&f.selector.Closes==1);Assert(!f.DrainUi()&&f.selector.Closes==1);f.selector.gameObject.activeSelf=false;Assert(f.DrainUi()&&!f.selectorOwned);});
        Check("replacement respec selector remains untouched",f=>{f.OwnSelector();f.currentSelector=new Selector();Refused(f.DrainUi);Assert(f.selectorOwned&&f.selector.Closes==0);});
        Console.WriteLine("PROGRESSION ACTUAL UI CLEANUP PASS="+passed+" FAIL="+failed);return failed;
    }
}
class Actor { internal string UniqueId="actor"; internal Descriptor Descriptor=new Descriptor(); }
class Descriptor { internal Actor Unit; }
class Builder { internal Descriptor Unit; internal Controller LevelUpController; }
class Controller { internal Descriptor Unit; }
class RespecProof { internal bool Ready,Fault;internal int Calls;internal Actor Original;internal bool TryRequireNativeRebind(Builder builder){Calls++;if(Fault)throw new InvalidOperationException("incomplete native effects");return Ready;} }
class Pins { internal bool IsRespec; }
class ActualRebind {
    private Pins pins=new Pins{IsRespec=true};private bool respecRebound;private RespecProof nativeRespec=new RespecProof();private Builder builder=new Builder();private Actor actor;private Descriptor descriptor;private int observations;
    private ActualRebind(){actor=new Actor();descriptor=actor.Descriptor;nativeRespec.Original=new Actor();}
    private object Snapshot(){return null;}private void Emit(string name,object row){observations++;}
/*ACTUAL_RESPEC_ADOPT*/
    private static int passed,failed;
    private static void Check(string name,Action<ActualRebind> test){var f=new ActualRebind();try{test(f);passed++;Console.WriteLine("PASS "+name);}catch(Exception error){failed++;Console.WriteLine("FAIL "+name+": "+error);}}
    private static void Assert(bool value){if(!value)throw new InvalidOperationException("assertion failed");}
    internal static int Verify(){
        Check("pending native first commit cannot adopt original",f=>{var old=f.actor;f.TryAdoptNativeRespecRebind();Assert(ReferenceEquals(old,f.actor)&&!f.respecRebound&&f.observations==0);});
        Check("complete observed native first commit permits exact rebind",f=>{f.nativeRespec.Ready=true;f.TryAdoptNativeRespecRebind();Assert(f.respecRebound&&ReferenceEquals(f.actor,f.nativeRespec.Original)&&ReferenceEquals(f.descriptor,f.actor.Descriptor)&&f.observations==1);});
        Check("rebind is observed once across Tick and cleanup",f=>{f.nativeRespec.Ready=true;f.TryAdoptNativeRespecRebind();f.TryAdoptNativeRespecRebind();Assert(f.nativeRespec.Calls==1&&f.observations==1);});
        Check("native effects refusal preserves preview custody",f=>{var old=f.actor;f.nativeRespec.Fault=true;bool refused=false;try{f.TryAdoptNativeRespecRebind();}catch(InvalidOperationException){refused=true;}Assert(refused&&ReferenceEquals(f.actor,old)&&!f.respecRebound&&f.observations==0);});
        Check("ordinary mode never borrows respec rebind",f=>{f.pins.IsRespec=false;f.nativeRespec.Ready=true;f.TryAdoptNativeRespecRebind();Assert(!f.respecRebound&&f.nativeRespec.Calls==0);});
        Console.WriteLine("PROGRESSION ACTUAL RESPEC ADOPTION PASS="+passed+" FAIL="+failed);return failed;
    }
}
class Program { static int Main(){return ActualUiCleanup.Verify()+ActualRebind.Verify()==0?0:1;} }
