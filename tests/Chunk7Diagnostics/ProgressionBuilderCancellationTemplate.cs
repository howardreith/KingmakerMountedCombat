using System;
class DialogMessageBox {internal static DialogMessageBox Instance;internal bool IsShown;}
class CharacterBuildController {internal object LevelUpController=new object();internal int Escs;internal bool EscFails;internal void OnHotKeyEscPressed(){Escs++;DialogMessageBox.Instance.IsShown=true;if(EscFails)throw new InvalidOperationException("native Esc failed");}}
class Ui {internal CharacterBuildController CharacterBuildController=new CharacterBuildController();}
class Game {internal static Game Instance=new Game();internal Ui UI=new Ui();}
class ActualCancellation {
    readonly CharacterBuildController builder;readonly Action assertScope;readonly Func<bool> nativeBusy;
    DialogMessageBox dialog;bool ownsDialog,requested,confirmed,settled;
    bool wrongScope,busy,wrongCallback,buttonReady=true,clickFails,sticky;int yesClicks,contractReads;
    ActualCancellation(){Game.Instance=new Game();DialogMessageBox.Instance=new DialogMessageBox();builder=Game.Instance.UI.CharacterBuildController;assertScope=()=>{if(wrongScope)throw new InvalidOperationException("foreign scope");};nativeBusy=()=>busy;}
    object ReadOwnedButton(){contractReads++;if(wrongCallback||!dialog.IsShown)throw new InvalidOperationException("foreign expected-dialog callback");return new object();}
    bool ReadyButton(object button)=>buttonReady;
    void InvokeClick(object button){yesClicks++;if(clickFails)throw new InvalidOperationException("native event failed");if(!sticky){builder.LevelUpController=null;dialog.IsShown=false;}}
    static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
/*ACTUAL_BUILDER_DRAIN*/
    static int passed,failed;
    static void Assert(bool value){if(!value)throw new InvalidOperationException("assertion failed");}
    static void Refused(Func<bool> call){bool refused=false;try{call();}catch(InvalidOperationException){refused=true;}Assert(refused);}
    static void Check(string name,Action<ActualCancellation> test){var a=new ActualCancellation();try{test(a);passed++;Console.WriteLine("PASS "+name);}catch(Exception error){failed++;Console.WriteLine("FAIL "+name+": "+error);}}
    internal static int Main(){
        Check("exact owned builder uses one native Esc and Yes",a=>{Assert(a.TryDrain()&&a.settled&&!a.ownsDialog&&a.builder.Escs==1&&a.yesClicks==1);Assert(a.TryDrain()&&a.builder.Escs==1&&a.yesClicks==1);});
        Check("already closed builder needs no native UI",a=>{a.builder.LevelUpController=null;Assert(a.TryDrain()&&a.settled&&a.builder.Escs==0&&a.yesClicks==0);});
        Check("busy native worker does not open dialog",a=>{a.busy=true;Assert(!a.TryDrain()&&!a.requested&&a.builder.Escs==0);});
        Check("foreign world prevents native UI",a=>{a.wrongScope=true;Refused(a.TryDrain);Assert(a.builder.Escs==0&&a.yesClicks==0);});
        Check("replaced builder cannot be cancelled",a=>{Game.Instance.UI.CharacterBuildController=new CharacterBuildController();Refused(a.TryDrain);Assert(a.builder.Escs==0&&a.yesClicks==0);});
        Check("pre-existing unexpected dialog stays untouched",a=>{DialogMessageBox.Instance.IsShown=true;Refused(a.TryDrain);Assert(!a.ownsDialog&&a.builder.Escs==0&&a.yesClicks==0);});
        Check("unready expected button waits without repeating Esc",a=>{a.buttonReady=false;Assert(!a.TryDrain()&&a.ownsDialog&&a.builder.Escs==1&&a.yesClicks==0);Assert(!a.TryDrain()&&a.builder.Escs==1);a.buttonReady=true;Assert(a.TryDrain()&&a.yesClicks==1);});
        Check("replacement dialog after opening retains builder custody",a=>{a.buttonReady=false;Assert(!a.TryDrain());DialogMessageBox.Instance=new DialogMessageBox{IsShown=true};Refused(a.TryDrain);Assert(a.ownsDialog&&!a.settled&&a.yesClicks==0);});
        Check("foreign expected-dialog callback never receives Yes",a=>{a.wrongCallback=true;Refused(a.TryDrain);Assert(a.ownsDialog&&a.yesClicks==0);});
        Check("native Esc exception remains owned without replay",a=>{a.builder.EscFails=true;Refused(a.TryDrain);Assert(a.requested&&a.ownsDialog&&a.builder.Escs==1);a.builder.EscFails=false;Assert(a.TryDrain()&&a.builder.Escs==1&&a.yesClicks==1);});
        Check("native Yes exception is never replayed",a=>{a.clickFails=true;Refused(a.TryDrain);Assert(a.confirmed&&a.ownsDialog&&a.yesClicks==1);a.clickFails=false;Assert(!a.TryDrain()&&a.yesClicks==1&&!a.settled);});
        Check("native asynchronous closure remains owned",a=>{a.sticky=true;Assert(!a.TryDrain()&&a.confirmed&&a.ownsDialog);Assert(!a.TryDrain()&&a.yesClicks==1);a.builder.LevelUpController=null;a.dialog.IsShown=false;Assert(a.TryDrain()&&!a.ownsDialog);});
        Check("scope is rechecked even after native closure",a=>{Assert(a.TryDrain());a.wrongScope=true;Refused(a.TryDrain);Assert(a.yesClicks==1);});
        Console.WriteLine("PROGRESSION ACTUAL BUILDER CANCELLATION PASS="+passed+" FAIL="+failed);return failed==0?0:1;
    }
}
