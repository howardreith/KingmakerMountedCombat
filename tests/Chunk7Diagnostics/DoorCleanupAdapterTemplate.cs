using System;
using System.Collections.Generic;
using System.Linq;
internal sealed class JObject { internal readonly Dictionary<string,object> values=new Dictionary<string,object>(); public object this[string key] { get {object v;values.TryGetValue(key,out v);return v;} set {values[key]=value;} } }
internal sealed class Scope { public bool valid=true; public void RequireCurrent(){if(!valid)throw new InvalidOperationException("foreign-world");} }
internal sealed class Loader {public bool IsLoadingInProcess;public List<string> QueuedNames=new List<string>();}
internal static class LoadingProcess {public static Loader Instance=new Loader();}
internal static class NativePersistenceIsolation {public static bool HasPendingWrites;}
internal sealed class World {public bool SerializationBusy;}
internal sealed class Command {public bool IsFinished;public int interrupts;public bool throwOnInterrupt,remainPending;public void Interrupt(){interrupts++;if(throwOnInterrupt)throw new InvalidOperationException("interrupt");if(!remainPending)IsFinished=true;}}
internal sealed class DoorData {public bool AlreadyUnlocked;}
// StandardDoor.Open toggles State; refuse models a native toggle that leaves State unchanged.
internal sealed class Door {public bool open,Enabled,throwOnOpen,refuseOpen,dataMissing;public int opens;public DoorData data=new DoorData();public DoorData Data=>dataMissing?null:data;public bool GetState()=>open;public void Open(){opens++;if(throwOnOpen)throw new InvalidOperationException("open");if(!refuseOpen)open=!open;}}
internal sealed class Cut {public bool enabled;}
internal sealed class Adapter {
 public Scope scope=new Scope();public World world=new World();public List<Command> commands=new List<Command>();public bool captured=true,restored,originalOpen=true,originalEnabled=false,originalCut=false,originalAlreadyUnlocked=false,doorValid=true,settled=true;
 public int settledFrame,doorChecks;public Door door=new Door{open=true,Enabled=true};public Cut cut=new Cut();public JObject evidence=new JObject();
 private void RequireDoor(){doorChecks++;if(!doorValid)throw new InvalidOperationException("foreign-door");}
 private bool Settled(bool open)=>settled&&door.open==open&&cut.enabled!=open;
 private static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
__METHOD__
}
internal static class Program {
 private static int passed;private static void Check(bool value,string text){if(!value)throw new Exception(text);passed++;Console.WriteLine("PASS "+text);}
 private static Adapter New(){LoadingProcess.Instance=new Loader();NativePersistenceIsolation.HasPendingWrites=false;return new Adapter();}
 private static bool Throws(Action a){try{a();return false;}catch(InvalidOperationException){return true;}}
 static int Main(){try{
 var a=New();a.scope.valid=false;var c=new Command();a.commands.Add(c);Check(Throws(()=>a.Drain())&&c.interrupts==0&&a.door.opens==0&&!a.restored,"foreign world refuses command and door mutations");
 a=New();c=new Command();a.commands.Add(c);LoadingProcess.Instance.IsLoadingInProcess=true;Check(!a.Drain()&&c.interrupts==0&&!a.restored,"active load retains all custody");
 a=New();c=new Command();a.commands.Add(c);LoadingProcess.Instance.QueuedNames.Add("load");Check(!a.Drain()&&c.interrupts==0,"queued load refuses cleanup");
 a=New();c=new Command();a.commands.Add(c);NativePersistenceIsolation.HasPendingWrites=true;Check(!a.Drain()&&c.interrupts==0,"archive worker refuses cleanup");
 a=New();c=new Command();a.commands.Add(c);a.world.SerializationBusy=true;Check(!a.Drain()&&c.interrupts==0,"serialization lease refuses cleanup");
 a=New();c=new Command{remainPending=true};a.commands.Add(c);a.door.open=false;Check(!a.Drain()&&c.interrupts==1&&a.door.opens==0&&!a.restored,"unfinished native command prevents door restoration");
 c.remainPending=false;Check(a.Drain()&&c.IsFinished&&a.door.opens==1&&a.restored&&!a.door.Enabled,"retry settles same command then restores door");
 a=New();c=new Command{throwOnInterrupt=true};a.commands.Add(c);Check(Throws(()=>a.Drain())&&!a.restored&&a.door.opens==0,"failed native interruption retains door custody");
 c.throwOnInterrupt=false;Check(a.Drain()&&a.restored,"failed native interruption can retry");
 a=New();a.doorValid=false;Check(Throws(()=>a.Drain())&&a.door.opens==0&&!a.restored,"replaced door refuses fixture mutation");
 a=New();a.door.open=false;a.settled=false;Check(!a.Drain()&&a.door.opens==1&&!a.restored&&a.door.Enabled,"native opening must settle before disabling or releasing lease");
 Check(!a.Drain()&&a.door.opens==1,"repeated cleanup does not restart native door opening");
 a.settled=true;Check(a.Drain()&&a.restored&&!a.door.Enabled,"settled opening restores captured disabled flag");
 var opens=a.door.opens;Check(a.Drain()&&a.door.opens==opens,"completed cleanup is idempotent");
 a=New();a.originalEnabled=true;Check(a.Drain()&&a.door.Enabled,"original enabled door remains enabled");
 a=New();a.captured=false;c=new Command();a.commands.Add(c);Check(a.Drain()&&c.IsFinished&&a.doorChecks==0&&!a.restored,"partial admission drains published command without inventing door lease");
 a=New();a.cut.enabled=true;Check(!a.Drain()&&!a.restored,"unsettled native cut retains lease");
 a=New();a.commands.Add(new Command{IsFinished=true});Check(a.Drain()&&a.commands[0].interrupts==0,"finished command is not replayed");
 a=New();a.originalOpen=false;a.originalCut=true;a.originalEnabled=true;a.door.open=true;a.cut.enabled=false;a.settled=false;
 Check(!a.Drain()&&a.door.opens==1&&!a.door.open&&!a.restored,"door captured closed is closed by the native Open toggle");
 Check(!a.Drain()&&a.door.opens==1,"repeated cleanup does not toggle the closing door again");
 a.cut.enabled=true;a.settled=true;Check(a.Drain()&&a.restored&&!a.door.open&&a.door.Enabled&&a.door.opens==1,"settled closing restores the captured closed state");
 Check(a.Drain()&&a.door.opens==1,"completed closed restoration is idempotent");
 a=New();a.originalOpen=false;a.originalCut=true;a.door.open=false;a.cut.enabled=true;Check(a.Drain()&&a.door.opens==0&&a.restored,"door still closed needs no toggle");
 a=New();a.originalOpen=false;a.originalCut=true;a.door.open=true;a.doorValid=false;Check(Throws(()=>a.Drain())&&a.door.opens==0&&!a.restored,"replaced door refuses closed restoration");
 a=New();a.originalOpen=false;a.originalCut=true;a.door.open=true;a.door.throwOnOpen=true;Check(Throws(()=>a.Drain())&&a.door.open&&!a.restored&&a.evidence["restoration"]==null,"throwing native toggle retains custody without claiming restoration");
 a.door.throwOnOpen=false;a.cut.enabled=true;Check(a.Drain()&&a.restored&&!a.door.open,"closed restoration retries after a throwing toggle");
 a=New();a.originalOpen=false;a.originalCut=true;a.door.open=true;a.door.refuseOpen=true;Check(!a.Drain()&&!a.Drain()&&a.door.open&&!a.restored&&a.evidence["restoration"]==null,"refused native toggle never claims restoration");
 a=New();a.originalAlreadyUnlocked=false;a.door.data.AlreadyUnlocked=true;Check(a.Drain()&&a.restored&&!a.door.data.AlreadyUnlocked,"interaction unlock residue returns to its captured value");
 a=New();a.originalAlreadyUnlocked=true;a.door.data.AlreadyUnlocked=true;Check(a.Drain()&&a.door.data.AlreadyUnlocked,"captured unlocked door stays unlocked");
 a=New();a.door.dataMissing=true;Check(Throws(()=>a.Drain())&&!a.restored&&a.evidence["restoration"]==null,"missing persistent data refuses an unchecked restoration claim");
 Console.WriteLine("TOTAL PASS="+passed+" FAIL=0");return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
