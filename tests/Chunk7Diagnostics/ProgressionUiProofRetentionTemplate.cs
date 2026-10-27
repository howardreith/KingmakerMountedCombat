using System;
class NativeProbe {internal bool Complete=true;internal int Disposes,Failures;internal bool Released;internal void Dispose(){Disposes++;if(Failures>0){Failures--;throw new InvalidOperationException("native unpatch failed");}Released=true;}}
class CharacterBuildController{internal object LevelUpController;}
class Ui{internal CharacterBuildController CharacterBuildController=new CharacterBuildController();}
class Game{internal static Game Instance=new Game();internal Ui UI=new Ui();}
class ActualUiDispose {
    bool disposed,respecSelected,pending;NativeProbe dismissal,respec;int drains;
    bool DrainUi(){drains++;return !pending;}static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
/*ACTUAL_UI_DISPOSE*/
    static int passed,failed;
    static void Assert(bool value){if(!value)throw new InvalidOperationException("assertion failed");}
    static void Check(string name,Action<ActualUiDispose> test){Game.Instance=new Game();var a=new ActualUiDispose();try{test(a);passed++;Console.WriteLine("PASS "+name);}catch(Exception error){failed++;Console.WriteLine("FAIL "+name+": "+error);}}
    static void Refused(Action call){bool refused=false;try{call();}catch(InvalidOperationException){refused=true;}Assert(refused);}
    internal static int Main(){
        Check("released respec proof survives independent caller cleanup retry",a=>{a.respecSelected=true;a.respec=new NativeProbe();var exact=a.respec;a.Dispose();Assert(a.disposed&&exact.Released&&ReferenceEquals(a.respec,exact)&&a.respec.Complete);});
        Check("released dismissal proof survives independent caller cleanup retry",a=>{a.dismissal=new NativeProbe();var exact=a.dismissal;a.Dispose();Assert(a.disposed&&exact.Released&&ReferenceEquals(a.dismissal,exact)&&a.dismissal.Complete);});
        Check("failed observer release keeps its exact proof and ownership",a=>{a.respecSelected=true;a.respec=new NativeProbe{Failures=1};var exact=a.respec;Refused(a.Dispose);Assert(!a.disposed&&ReferenceEquals(a.respec,exact)&&!exact.Released);a.Dispose();Assert(a.disposed&&exact.Released);});
        Check("pending UI prevents observer release",a=>{a.respec=new NativeProbe();a.pending=true;Refused(a.Dispose);Assert(!a.disposed&&a.respec.Disposes==0);});
        Check("owned builder prevents observer release",a=>{a.respecSelected=true;a.respec=new NativeProbe();Game.Instance.UI.CharacterBuildController.LevelUpController=new object();Refused(a.Dispose);Assert(!a.disposed&&a.respec.Disposes==0);});
        Check("released observer is not disposed again",a=>{a.respecSelected=true;a.respec=new NativeProbe();var exact=a.respec;a.Dispose();a.Dispose();Assert(a.disposed&&exact.Disposes==1);});
        Console.WriteLine("PROGRESSION ACTUAL UI PROOF RETENTION PASS="+passed+" FAIL="+failed);return failed==0?0:1;
    }
}
