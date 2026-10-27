using System;
using System.Collections.Generic;
using System.IO;
class JObject{}
class CharacterBuildController { internal object LevelUpController; }
class Ui { internal CharacterBuildController CharacterBuildController=new CharacterBuildController(); }
class Game { internal static Game Instance=new Game(); internal Ui UI=new Ui(); }
class Source {internal string FileName="source.zks",Sha256="hash";}
class Driver {internal bool Owns;internal bool TryGetOwnedBuildSession(out CharacterBuildController builder){builder=Game.Instance.UI.CharacterBuildController;return Owns;} }
class RespecProbe {internal bool Refuse;internal void RequireExactPreview(CharacterBuildController builder){if(Refuse)throw new InvalidOperationException("no native preview proof");}}
class NativeUi {internal bool RefuseDrain,RefuseDispose;internal int Drains,Disposes,Captures;internal RespecProbe RespecProbe=new RespecProbe();internal bool DrainUi(){Drains++;return !RefuseDrain;}internal void Dispose(){Disposes++;if(RefuseDispose)throw new InvalidOperationException("UI observer unpatch");}internal JObject Capture(){Captures++;return new JObject();}}
class Completion {internal bool InSynchronousCallback,RefuseDispose;internal int Disposes,Captures;internal JObject Capture(){Captures++;return new JObject();}internal void Dispose(){Disposes++;if(RefuseDispose)throw new InvalidOperationException("completion unpatch");}}
// The owned Manual completion probe: a native callback keeps it alive; drain disposes it once.
class ManualProbe {internal bool InCallback,RefuseDispose;internal int Disposes;internal void Dispose(){Disposes++;if(RefuseDispose)throw new InvalidOperationException("manual probe unpatch");}}
class Custody {internal bool Refuse;internal int Drains;internal bool TryDrainOriginalPair(){Drains++;return !Refuse;}}
class Chunk7NativeBuilderCancellation {internal static bool Refuse;internal static int Calls;internal Chunk7NativeBuilderCancellation(CharacterBuildController b,Action a,Func<bool> n){}internal bool TryDrain(){Calls++;if(Refuse)return false;Game.Instance.UI.CharacterBuildController.LevelUpController=null;return true;}}
static class NativePersistenceIsolation {internal static int Disables;internal static void DisableNativeSlotRotation(){Disables++;}}
class ActualSession {
    bool disposed,cleanup,mutationStarted=true,settled,rotationOwned=true,wrongWorld,wrongTarget,busy,IsRespec;int scopes,hashes,errors;
    Driver driver;NativeUi nativeUi=new NativeUi();Completion completion=new Completion();ManualProbe manualProbe=new ManualProbe();Custody pairCustody=new Custody();Chunk7NativeBuilderCancellation cancellation;
    JObject nativeUiEvidence,completionEvidence;string expectedRoot="root";Source source=new Source();
    ActualSession(){Game.Instance=new Game();NativePersistenceIsolation.Disables=0;Chunk7NativeBuilderCancellation.Calls=0;Chunk7NativeBuilderCancellation.Refuse=false;}
    void AssertWorld(){scopes++;if(wrongWorld)throw new InvalidOperationException("foreign world/authority");}void AssertTargetBinding(){if(wrongTarget)throw new InvalidOperationException("unproved target rebind");}bool NativeBusy()=>busy;void Remember(Exception error){errors++;}string Hash(string path){hashes++;return "hash";}static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
/*ACTUAL_SESSION_DRAIN*/
/*ACTUAL_SESSION_DISPOSE*/
    static int passed,failed;
    static void Check(string name,Action<ActualSession> test){var a=new ActualSession();try{test(a);passed++;Console.WriteLine("PASS "+name);}catch(Exception error){failed++;Console.WriteLine("FAIL "+name+": "+error);}}
    static void Assert(bool value){if(!value)throw new InvalidOperationException("assertion failed");}
    static int Mutations(ActualSession a)=>a.nativeUi.Drains+a.nativeUi.Disposes+a.completion.Disposes+a.pairCustody.Drains+Chunk7NativeBuilderCancellation.Calls+NativePersistenceIsolation.Disables;
    internal static int Main(){
        Check("never-mutated admission refusal settles without native cleanup",a=>{a.mutationStarted=false;a.wrongWorld=true;Assert(a.TryDrain()&&a.settled&&Mutations(a)==0);});
        Check("same-world settled cleanup releases every exact owner",a=>{Assert(a.TryDrain()&&a.settled&&a.nativeUi.Disposes==1&&a.completion.Disposes==1&&NativePersistenceIsolation.Disables==1);});
        Check("changed world/authority retains all custody before mutation",a=>{a.wrongWorld=true;Assert(!a.TryDrain()&&!a.settled&&Mutations(a)==0);});
        Check("native worker retains all custody before mutation",a=>{a.busy=true;Assert(!a.TryDrain()&&!a.settled&&Mutations(a)==0);});
        Check("native Manual callback retains every observer",a=>{a.manualProbe.InCallback=true;Assert(!a.TryDrain()&&!a.settled&&Mutations(a)==0&&a.manualProbe.Disposes==0);});
        Check("settled cleanup releases the Manual probe once",a=>{Assert(a.TryDrain()&&a.settled&&a.manualProbe.Disposes==1);});
        Check("Manual probe unpatch failure retains rotation",a=>{a.manualProbe.RefuseDispose=true;Assert(!a.TryDrain()&&!a.settled&&NativePersistenceIsolation.Disables==0&&a.completion.Disposes==1);});
        Check("native callback retains every observer",a=>{a.completion.InSynchronousCallback=true;Assert(!a.TryDrain()&&!a.settled&&Mutations(a)==0);});
        Check("unproved target rebind retains all custody",a=>{a.wrongTarget=true;Assert(!a.TryDrain()&&!a.settled&&Mutations(a)==0);});
        Check("builder cancellation retained until exact native UI closes",a=>{a.driver=new Driver{Owns=true};Game.Instance.UI.CharacterBuildController.LevelUpController=new object();Chunk7NativeBuilderCancellation.Refuse=true;Assert(!a.TryDrain()&&a.cancellation!=null&&!a.settled&&a.nativeUi.Drains==0);Chunk7NativeBuilderCancellation.Refuse=false;Assert(a.TryDrain());});
        Check("observed respec preview can be owned before driver assignment",a=>{a.IsRespec=true;Game.Instance.UI.CharacterBuildController.LevelUpController=new object();Assert(a.TryDrain()&&Chunk7NativeBuilderCancellation.Calls==1);});
        Check("constructed unopened driver cannot hide exact observed respec preview",a=>{a.IsRespec=true;a.driver=new Driver{Owns=false};Game.Instance.UI.CharacterBuildController.LevelUpController=new object();Assert(a.TryDrain()&&a.cancellation!=null&&Chunk7NativeBuilderCancellation.Calls==1);});
        Check("unopened ordinary driver creates no cancellation or native builder",a=>{a.driver=new Driver{Owns=false};Assert(a.TryDrain()&&a.cancellation==null&&Chunk7NativeBuilderCancellation.Calls==0);});
        Check("unobserved foreign preview is never cancelled",a=>{a.IsRespec=true;Game.Instance.UI.CharacterBuildController.LevelUpController=new object();a.nativeUi.RespecProbe.Refuse=true;Assert(!a.TryDrain()&&a.cancellation==null&&Mutations(a)==0);});
        Check("pending selector keeps pair and observers alive",a=>{a.nativeUi.RefuseDrain=true;Assert(!a.TryDrain()&&!a.settled&&a.pairCustody.Drains==0&&a.nativeUi.Disposes==0&&a.completion.Disposes==0);});
        Check("pending command/process keeps observers and slot lease alive",a=>{a.pairCustody.Refuse=true;Assert(!a.TryDrain()&&!a.settled&&a.nativeUi.Disposes==0&&a.completion.Disposes==0&&NativePersistenceIsolation.Disables==0);});
        Check("one observer failure still attempts other observer independently",a=>{a.nativeUi.RefuseDispose=true;Assert(!a.TryDrain()&&!a.settled&&a.nativeUi.Disposes==1&&a.completion.Disposes==1&&NativePersistenceIsolation.Disables==0);a.nativeUi.RefuseDispose=false;Assert(a.TryDrain());});
        Check("both observer failures are retained separately",a=>{a.nativeUi.RefuseDispose=true;a.completion.RefuseDispose=true;Assert(!a.TryDrain()&&!a.settled&&a.errors==2&&a.nativeUi.Disposes==1&&a.completion.Disposes==1);});
        Check("completion unpatch failure retains rotation",a=>{a.completion.RefuseDispose=true;Assert(!a.TryDrain()&&!a.settled&&NativePersistenceIsolation.Disables==0);});
        Check("observer evidence is captured before disposal and retained on retry",a=>{a.nativeUi.RefuseDispose=true;Assert(!a.TryDrain());var first=a.nativeUiEvidence;a.nativeUi.RefuseDispose=false;Assert(a.TryDrain()&&ReferenceEquals(first,a.nativeUiEvidence)&&a.nativeUi.Captures==1&&a.completion.Captures==1);});
        Check("Dispose refusal never drops its owner",a=>{a.busy=true;bool refused=false;try{a.Dispose();}catch(InvalidOperationException){refused=true;}Assert(refused&&!a.disposed&&!a.settled);a.busy=false;a.Dispose();Assert(a.disposed&&a.settled);});
        Console.WriteLine("PROGRESSION ACTUAL SESSION DRAIN PASS="+passed+" FAIL="+failed);return failed==0?0:1;
    }
}
