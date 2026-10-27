// Actual World Move/selection/settlement bodies are inserted by the focused runner.
// Native command publication, Unity state and optional telemetry are external facades.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Linq;

internal struct Vector3 { public float x,y,z; public Vector3(float a,float b,float c){x=a;y=b;z=c;} }
internal static class Time { internal static int frameCount=1061; }
internal static class CombatController { internal static bool IsInTurnBasedCombat()=>false; }
internal static class PointerController { internal static bool SimulatingClick; }
internal sealed class Descriptor { internal readonly State State=new State(); }
internal sealed class State { internal bool ConsciousReadThrows; internal bool IsConscious {get{if(ConsciousReadThrows)throw new Exception("conscious metadata read failed");return true;}} }
internal sealed class UnitView { internal UnitEntityData Actor; }
internal sealed class UnitEntityData
{
    internal readonly string UniqueId; internal readonly UnitView View; internal readonly Descriptor Descriptor=new Descriptor();
    internal readonly UnitCommands Commands; internal bool IsInState=true,IsInGame=true; internal Vector3 Position;
    internal UnitEntityData(string id){UniqueId=id;View=new UnitView{Actor=this};Commands=new UnitCommands(this);}
}
internal class UnitCommand
{
    internal enum CommandType { Free,Standard,Swift,Move }
    internal enum ResultType { None,Success,Interrupted }
    internal UnitEntityData Executor; internal bool CreatedByPlayer=true,IsFinished; internal bool StartedReadThrows;
    internal bool IsStarted {get{if(StartedReadThrows)throw new InvalidOperationException("started observation failed");return false;}}
    internal CommandType Type=CommandType.Move; internal ResultType Result; internal int Interrupts;
    internal virtual void Interrupt(){Interrupts++;IsFinished=true;Result=ResultType.Interrupted;}
}
internal sealed class UnitMoveTo:UnitCommand { internal UnitMoveTo(UnitEntityData actor){Executor=actor;} }
internal sealed class Execution { internal bool IsEnded; }
internal sealed class UnitUseAbility:UnitCommand { internal Execution ExecutionProcess; }
internal sealed class UnitCommands
{
    internal UnitCommand[] Storage=new UnitCommand[4]; internal bool RawReadThrows;
    internal UnitCommand[] Raw {get{if(RawReadThrows)throw new InvalidOperationException("raw observation failed");return Storage;}}
    internal readonly LinkedList<UnitCommand> Queue=new LinkedList<UnitCommand>(); internal UnitCommand PreviousCommand;
    internal UnitCommands(UnitEntityData actor){}
    internal UnitMoveTo Move {get{var c=Storage[3];return c==null||c.IsFinished?null:c as UnitMoveTo;}}
    internal UnitCommand GetCommand(UnitCommand.CommandType t)=>Storage[(int)t];
    internal bool Empty=>Storage.All(x=>x==null);
}
internal sealed class SelectionManager
{
    internal static SelectionManager Instance=new SelectionManager();
    internal readonly List<UnitEntityData> SelectedUnits=new List<UnitEntityData>();
    internal void SelectUnit(UnitView view,bool a,bool b,bool c){SelectedUnits.Clear();SelectedUnits.Add(view.Actor);}
}
internal sealed class Ui { internal SelectionManager SelectionManager; }
internal enum Mode { Default,Dialog }
internal sealed class NativeTime { internal bool Throw; internal TimeSpan GameTime {get{if(Throw)throw new Exception("clock observation failed");return TimeSpan.FromSeconds(1);}} }
internal sealed class Game
{
    internal static Game Instance; internal bool IsPaused,IsControllerGamepad; internal Mode CurrentMode;
    internal readonly NativeTime TimeController=new NativeTime(); internal readonly Ui UI=new Ui();
}
internal sealed class Combat { internal string LastFeedback="idle"; internal int[] LastRejectionCodes=new int[0]; }
internal sealed class Pair { internal readonly Combat Combat=new Combat(); }
internal sealed class World
{
    internal bool SerializationBusy; internal readonly Pair Pair=new Pair(); internal Pair ForActor(UnitEntityData actor)=>Pair;
}
internal sealed class Persistence { internal bool ActiveSaveWorkerRunning,CombatRestorationPending; }
internal static class ClickGroundHandler
{
    internal static Action<Vector3> OnMove; internal static int Calls; internal static bool ObservedPause; internal static bool Marker;
    internal static void MoveSelectedUnitsToPoint(Vector3 point,bool marker){Calls++;ObservedPause=Game.Instance.IsPaused;Marker=marker;OnMove(point);}
}
internal sealed class MovementTelemetryWriter { }
internal sealed class MountedOptionalDiagnosticsWindow:IDisposable
{
    internal static int Installs; internal MountedOptionalDiagnosticsWindow(MovementTelemetryWriter writer,string label){}
    internal void Install(int frame,double elapsed){Installs++;} public void Dispose(){}
}
internal sealed class Engine
{
    private UnitEntityData firstRider,firstMount,secondRider,secondMount;
    private readonly JArray groundInputs=new JArray(),movements=new JArray();
    private readonly List<UnitCommand> issuedCommands=new List<UnitCommand>(),groundCommandsAwaited=new List<UnitCommand>();
    private UnitEntityData[] incompleteGroundInputActors; private bool groundInputCustodyUncertain;
    private UnitCommand issuedMove; private UnitEntityData movingActor; private Vector3 moveStart,moveGoal;
    private JObject moveRecord,quietBefore; private MountedOptionalDiagnosticsWindow quietWindow; private object measurement;
    private readonly Stopwatch clock=Stopwatch.StartNew(); private readonly World world=new World(); private readonly Persistence persistence=new Persistence();
    private readonly Func<MovementTelemetryWriter> optionalTelemetry=()=>new MovementTelemetryWriter();
    private bool SixPairStress;
    private sealed class Inherited { internal UnitEntityData[] Actors; }
    private Inherited inherited;
    internal Engine(bool six=false){firstRider=new UnitEntityData("rider-a");firstMount=new UnitEntityData("pet-a");secondRider=new UnitEntityData("rider-b");secondMount=new UnitEntityData("pet-b");
        SixPairStress=six;if(six)inherited=new Inherited{Actors=new[]{firstRider,firstMount,secondRider,secondMount}.Concat(Enumerable.Range(4,8).Select(i=>new UnitEntityData("actor-"+i))).ToArray()};}
    internal void SetActorCount(int count){inherited.Actors=inherited.Actors.Take(count).Concat(Enumerable.Range(inherited.Actors.Length,Math.Max(0,count-inherited.Actors.Length)).Select(i=>new UnitEntityData("extra-"+i))).ToArray();}
    internal UnitEntityData A(int i)=>Actors()[i]; internal JArray Attempts=>groundInputs; internal JArray Movements=>movements;
    internal List<UnitCommand> Owned=>issuedCommands; internal List<UnitCommand> Awaited=>groundCommandsAwaited;
    internal bool Settled()=>NativeCommandsSettled();
    internal void Run(string label="mammoth",int rider=0,int mover=1,int other=2){Move(A(rider),A(mover),new Vector3(2,0,0),A(other),label);}
    internal void Drain(){ /* CLEANUP_INTERRUPT */ }
    private bool TryCleanup(string part,Action action){try{action();return true;}catch{return false;}}
    private JObject CaptureWorld(string label)=>new JObject{{"label",label}};
    /* MOVE */
    /* SELECT */
    /* ACTORS */
    /* POINT */
    /* ID */
    /* REQUIRE */
    /* SETTLED */
    /* SNAPSHOT */
    /* RECORD_ERROR */
    /* COMMAND */
    /* CAPTURE */
    /* RETAIN */
    /* GROUND_SETTLED */
}
internal static class Program
{
    private static int passed,failed;
    private static void Check(bool ok,string name){Console.WriteLine((ok?"PASS ":"FAIL ")+name);if(ok)passed++;else failed++;}
    private static Exception Run(Engine e,string label="mammoth",int rider=0,int mover=1,int other=2){try{e.Run(label,rider,mover,other);return null;}catch(Exception x){return x;}}
    private static Engine Fresh(bool six=false)
    {
        SelectionManager.Instance=new SelectionManager();Game.Instance=new Game();Game.Instance.UI.SelectionManager=SelectionManager.Instance;
        ClickGroundHandler.Calls=0;ClickGroundHandler.OnMove=_=>{};MountedOptionalDiagnosticsWindow.Installs=0;return new Engine(six);
    }
    private static int Obj(object x)=>x==null?0:RuntimeHelpers.GetHashCode(x);
    private static JObject Attempt(Engine e)=>e.Attempts.Count==0?null:(JObject)e.Attempts[0];
    private static bool ReadBool(JToken t,string key)=>t?[key]?.Type==JTokenType.Boolean&&(bool)t[key];
    private static bool HasNew(Engine e,UnitCommand cmd)=>e.Owned.Any(x=>ReferenceEquals(x,cmd));
    private static JObject Actor(JObject row,string phase,int i)=>row?[phase]?["actors"]?[i] as JObject;
    private static void MainCases()
    {
        foreach(var route in new[]{new[]{0,1,2},new[]{2,3,0},new[]{0,0,2}})
        {
            var e=Fresh();var cmd=new UnitMoveTo(e.A(route[1]));ClickGroundHandler.OnMove=_=>e.A(route[1]).Commands.Storage[3]=cmd;
            var error=Run(e,route[0]==2?"horse":route[1]==0?"unmounted-control":"mammoth",route[0],route[1],route[2]);
            Check(error==null&&ClickGroundHandler.Calls==1&&ClickGroundHandler.ObservedPause&&!ClickGroundHandler.Marker,"exact existing native input succeeds "+route[1]);
            Check(e.Movements.Count==1&&HasNew(e,cmd)&&!Game.Instance.IsPaused&&SelectionManager.Instance.SelectedUnits.Single()==e.A(route[2]),"success preserves movement record and switched selection "+route[1]);
            var row=Attempt(e);Check(row!=null&&ReadBool(row,"nativeCallEntered")&&ReadBool(row,"nativeCallReturned")&&ReadBool(row,"commandValidationPassed"),"success has bounded actual attempt "+route[1]);
            Check(Actor(row,"after",route[1])?["rawMoveObject"]?.Value<int>()==Obj(cmd)&&Actor(row,"after",route[1])?["publicMoveObject"]?.Value<int>()==Obj(cmd),"success captures raw and public command "+route[1]);
        }
        {
            var e=Fresh();var x=Run(e);var row=Attempt(e);
            Check(x?.Message=="Native ground click failed to issue the intended command."&&ClickGroundHandler.Calls==1&&e.Movements.Count==0,"refused call preserves exact assertion and no success row");
            Check(row!=null&&ReadBool(row,"nativeCallReturned")&&!ReadBool(row,"commandValidationPassed")&&row["before"]!=null&&row["after"]!=null,"refused call retains both snapshots");
            Check(e.Owned.Count==0&&MountedOptionalDiagnosticsWindow.Installs==0,"refusal acquires no command or quiet window");
        }
        {
            var e=Fresh();var cmd=new UnitMoveTo(e.A(1));ClickGroundHandler.OnMove=_=>e.A(1).Commands.Queue.AddLast(cmd);var x=Run(e);var row=Attempt(e);
            Check(x?.Message=="Native ground click failed to issue the intended command.","queued-only command is not fabricated as public Move");
            Check(HasNew(e,cmd)&&!e.Settled(),"queued new native command retains cleanup custody");
            Check(Actor(row,"after",1)?["queueCount"]?.Value<int>()==1&&Actor(row,"after",1)?["publicMoveObject"]?.Value<int>()==0,"queued-only snapshot distinguishes slot getter");
            e.Drain();Check(cmd.Interrupts==1&&e.A(1).Commands.Queue.Count==1&&ClickGroundHandler.Calls==1,"cleanup interrupts owned command without queue clear or retry");
        }
        {
            var e=Fresh();var cmd=new UnitMoveTo(e.A(1)){IsFinished=true,Result=UnitCommand.ResultType.Interrupted};ClickGroundHandler.OnMove=_=>e.A(1).Commands.Storage[3]=cmd;var x=Run(e);var row=Attempt(e);
            Check(x!=null&&HasNew(e,cmd)&&e.Settled(),"already finished raw command remains exact retained evidence");
            Check(Actor(row,"after",1)?["rawMoveObject"]?.Value<int>()==Obj(cmd)&&Actor(row,"after",1)?["publicMoveObject"]?.Value<int>()==0,"finished raw and hidden public Move are distinct");
            e.Drain();Check(cmd.Interrupts==0,"finished command is never interrupted again");
        }
        {
            var e=Fresh();var cmd=new UnitMoveTo(e.A(1));e.A(1).Commands.Storage[3]=cmd;var x=Run(e);e.Drain();
            Check(x!=null&&!HasNew(e,cmd)&&cmd.Interrupts==0,"same pre-existing Move is not borrowed");
        }
        {
            var e=Fresh();var cmd=new UnitMoveTo(e.A(2));ClickGroundHandler.OnMove=_=>e.A(2).Commands.Storage[3]=cmd;var x=Run(e);
            Check(x?.Message=="Native ground click failed to issue the intended command."&&HasNew(e,cmd),"misrouted new move is retained before intended-executor refusal");
            e.Drain();Check(cmd.Interrupts==1,"misrouted exact actor command receives existing cleanup");
        }
        {
            var e=Fresh();var one=new UnitMoveTo(e.A(1));var two=new UnitMoveTo(e.A(3));ClickGroundHandler.OnMove=_=>{e.A(1).Commands.Storage[3]=one;e.A(3).Commands.Storage[3]=two;};var x=Run(e);
            Check(x?.Message=="Native ground click changed another actor's movement command."&&e.Movements.Count==0,"nonselected command change still refuses");
            Check(HasNew(e,one)&&HasNew(e,two)&&e.Owned.Count==2,"both new commands retained before second assertion");
            e.Drain();Check(one.Interrupts==1&&two.Interrupts==1,"both retained commands drain independently");
        }
        {
            var e=Fresh();var cmd=new UnitMoveTo(e.A(1));var original=new InvalidOperationException("native throw after publication");ClickGroundHandler.OnMove=_=>{e.A(1).Commands.Storage[3]=cmd;throw original;};var x=Run(e);var row=Attempt(e);
            Check(ReferenceEquals(x,original)&&ClickGroundHandler.Calls==1,"native exception object is not masked or retried");
            Check(row!=null&&ReadBool(row,"nativeCallEntered")&&!ReadBool(row,"nativeCallReturned")&&row["nativeError"]!=null&&row["after"]!=null,"native throw still records finally snapshot");
            Check(HasNew(e,cmd),"native throw retains published command");e.Drain();Check(cmd.Interrupts==1,"published-before-throw command drains");
        }
        {
            var e=Fresh();var old=new UnitMoveTo(e.A(1));e.A(1).Commands.PreviousCommand=old;e.A(2).Commands.Queue.AddLast(old);
            ClickGroundHandler.OnMove=_=>e.A(1).Commands.Storage[3]=old;var x=Run(e);e.Drain();Check(x?.Message=="Native ground click failed to issue the intended command."&&!HasNew(e,old)&&old.Interrupts==0,"pre-existing queue and previous references are excluded");
        }
        {
            var e=Fresh();var cmd=new UnitMoveTo(e.A(1));ClickGroundHandler.OnMove=_=>{e.A(1).Commands.Storage[3]=cmd;e.A(1).Commands.Queue.AddLast(cmd);};Run(e);e.Drain();
            Check(e.Owned.Count==1&&cmd.Interrupts==1,"duplicate native references confer one cleanup owner");
        }
        foreach(var foreign in new[]{true,false})
        {
            var e=Fresh();var cmd=new UnitMoveTo(foreign?new UnitEntityData("pet-a"):e.A(1)){CreatedByPlayer=!(!foreign)};
            ClickGroundHandler.OnMove=_=>e.A(1).Commands.Queue.AddLast(cmd);Run(e);e.Drain();
            Check(!HasNew(e,cmd)&&cmd.Interrupts==0,"foreign or non-player command has no interruption authority "+foreign);
            Check(e.Awaited.Any(x=>ReferenceEquals(x,cmd))&&!e.Settled(),"foreign or non-player reference is only awaited "+foreign);
            cmd.IsFinished=true;Check(e.Settled(),"observed foreign or non-player terminal state releases wait "+foreign);
        }
        foreach(var foreign in new[]{true,false})
        {
            var e=Fresh();var cmd=new UnitMoveTo(foreign?e.A(3):e.A(1)){CreatedByPlayer=foreign};
            ClickGroundHandler.OnMove=_=>e.A(1).Commands.Storage[3]=cmd;var x=Run(e);
            Check(x?.Message=="Native ground click failed to issue the intended command."&&e.Movements.Count==0,"public Move must bind exact executor and player origin "+foreign);
            if(!foreign){e.Drain();Check(!HasNew(e,cmd)&&cmd.Interrupts==0,"non-player public Move is not borrowed by success path");}
        }
        {
            var e=Fresh();var cmd=new UnitUseAbility{Executor=e.A(1),Type=UnitCommand.CommandType.Standard,ExecutionProcess=new Execution()};ClickGroundHandler.OnMove=_=>e.A(1).Commands.Storage[1]=cmd;Run(e);e.Drain();
            Check(cmd.Interrupts==0&&!e.Settled(),"another command kind is not interrupted by Move custody");
            cmd.IsFinished=true;Check(!e.Settled(),"observed ability process still fences cleanup");cmd.ExecutionProcess.IsEnded=true;Check(e.Settled(),"native process termination releases passive wait");
        }
        {
            var e=Fresh();e.A(0).Commands.RawReadThrows=true;var x=Run(e);var row=Attempt(e);
            Check(x!=null&&ClickGroundHandler.Calls==0&&row?["before"]?["commandsComplete"]?.Value<bool>()==false,"failed baseline custody refuses before dispatch");
        }
        {
            var e=Fresh();e.A(0).Commands.Storage=new UnitCommand[9];var x=Run(e);var row=Attempt(e);
            Check(x!=null&&ClickGroundHandler.Calls==0&&Actor(row,"before",0)?["raw"]?.Count()==8&&ReadBool(Actor(row,"before",0),"rawTruncated"),"raw-slot bound refuses without unbounded observation");
        }
        {
            var e=Fresh();for(var i=0;i<17;i++)e.A(0).Commands.Queue.AddLast(new UnitMoveTo(e.A(0)));var x=Run(e);var row=Attempt(e);
            Check(x!=null&&ClickGroundHandler.Calls==0&&Actor(row,"before",0)?["queue"]?.Count()==16&&ReadBool(Actor(row,"before",0),"queueTruncated"),"queue bound refuses before any new input");
        }
        {
            var e=Fresh();var cmd=new UnitMoveTo(e.A(1));ClickGroundHandler.OnMove=_=>{e.A(1).Commands.Storage[3]=cmd;e.A(0).Commands.RawReadThrows=true;};var x=Run(e);var row=Attempt(e);
            Check(x!=null&&HasNew(e,cmd)&&row?["after"]?["actors"]?[0]?["readError"]!=null,"partial reference capture refuses while retaining other command custody");
            e.Drain();bool settled=false,threw=false;try{settled=e.Settled();}catch{threw=true;}Check(!threw&&!settled&&ReadBool(row,"custodyUncertain"),"partial observation retains explicit cleanup uncertainty without throwing");
            e.A(0).Commands.RawReadThrows=false;Check(!e.Settled(),"later readable actor slots cannot recover omitted command lifetime");
        }
        {
            var e=Fresh();ClickGroundHandler.OnMove=_=>{for(var i=0;i<17;i++)e.A(1).Commands.Queue.AddLast(new UnitMoveTo(e.A(1)));};Run(e);var row=Attempt(e);e.Drain();
            Check(e.Owned.Count==16&&ReadBool(row,"custodyUncertain")&&!e.Settled(),"post-input truncation cannot hide remaining native queue custody");
            foreach(var cmd in e.A(1).Commands.Queue)cmd.IsFinished=true;e.A(1).Commands.Queue.Clear();Check(!e.Settled(),"later empty queue cannot erase incomplete reference observation");
        }
        {
            var e=Fresh();var cmd=new UnitUseAbility{Executor=e.A(2),Type=UnitCommand.CommandType.Standard,ExecutionProcess=new Execution()};
            ClickGroundHandler.OnMove=_=>{e.A(2).Commands.Storage[1]=cmd;e.A(2).Commands.RawReadThrows=true;};Run(e);
            // External native maintenance removes an already finished shell; it does not end this process.
            cmd.IsFinished=true;e.A(2).Commands.RawReadThrows=false;e.A(2).Commands.Storage[1]=null;
            Check(!e.Awaited.Any(x=>ReferenceEquals(x,cmd))&&!cmd.ExecutionProcess.IsEnded&&cmd.Interrupts==0&&!e.Settled(),"omitted finished shell with live process cannot be inferred from empty slots");
            cmd.ExecutionProcess.IsEnded=true;Check(!e.Settled(),"unobserved external process completion cannot clear retained uncertainty");
        }
        {
            var e=Fresh();var cmd=new UnitMoveTo(e.A(1));ClickGroundHandler.OnMove=_=>{e.A(1).Commands.Storage[3]=cmd;e.A(0).Descriptor.State.ConsciousReadThrows=true;};var x=Run(e);var row=Attempt(e);
            Check(x==null&&HasNew(e,cmd)&&ReadBool(row?["after"],"commandsComplete")&&!ReadBool(row,"custodyUncertain")&&Actor(row,"after",0)?["readError"]!=null,"complete reference capture distinguishes optional actor metadata failure");
            e.Drain();Check(e.Settled(),"complete captured references remain normally recoverable after metadata error");
        }
        {
            var e=Fresh();var cmd=new UnitMoveTo(e.A(1)){StartedReadThrows=true};Game.Instance.TimeController.Throw=true;ClickGroundHandler.OnMove=_=>e.A(1).Commands.Storage[3]=cmd;var x=Run(e);var row=Attempt(e);
            Check(x==null&&HasNew(e,cmd)&&row?["before"]?["environmentError"]!=null&&row?["after"]?["actors"]?[1]?["raw"]?[3]?["readError"]!=null,"passive environment and command metadata errors remain explicit");
        }
        {
            var e=Fresh();var original=new Exception("original native error");ClickGroundHandler.OnMove=_=>{e.A(0).Commands.RawReadThrows=true;throw original;};var x=Run(e);
            Check(ReferenceEquals(x,original)&&Attempt(e)?["after"]!=null,"finally observation failure preserves native exception identity");
        }
        {
            var e=Fresh();for(var i=0;i<4;i++){ClickGroundHandler.OnMove=_=>e.A(1).Commands.Storage[3]=new UnitMoveTo(e.A(1));Run(e);}
            Check(e.Attempts.Count==3&&ClickGroundHandler.Calls==3,"closed case records and dispatches at most its three inputs");
        }
        {
            var e=Fresh();Check(e.Attempts.Count==0&&ClickGroundHandler.Calls==0&&e.Owned.Count==0&&e.Awaited.Count==0,"observer is inactive outside actual selected Move");
        }
        {
            var e=Fresh(true);var cmd=new UnitMoveTo(e.A(11));ClickGroundHandler.OnMove=_=>e.A(11).Commands.Storage[3]=cmd;
            var x=Run(e,"six-ground-5",10,11,0);var row=Attempt(e);
            Check(x==null&&ClickGroundHandler.Calls==1&&HasNew(e,cmd)&&row?["before"]?["actors"]?.Count()==12&&row?["after"]?["actors"]?.Count()==12,
                "actual six-pair Move inspects all twelve actors before and after native dispatch");
            Check(Actor(row,"after",11)?["publicMoveObject"]?.Value<int>()==Obj(cmd)&&e.Movements.Count==1&&SelectionManager.Instance.SelectedUnits.Single()==e.A(0),
                "sixth Horse receives its exact new player command while selection changes to a neighbor");
        }
        foreach(var count in new[]{11,13})
        {
            var e=Fresh(true);e.SetActorCount(count);var x=Run(e,"six-bound");
            Check(x!=null&&ClickGroundHandler.Calls==0&&!ReadBool(Attempt(e)?["before"],"commandsComplete"),
                "six-pair command custody refuses wrong actor cardinality before native input "+count);
        }
        {
            var e=Fresh(true);var cmd=new UnitMoveTo(e.A(11));var unexpected=new UnitMoveTo(e.A(8));
            ClickGroundHandler.OnMove=_=>{e.A(11).Commands.Storage[3]=cmd;e.A(8).Commands.Storage[3]=unexpected;};
            var x=Run(e,"six-neighbor",10,11,0);e.Drain();
            Check(x!=null&&HasNew(e,cmd)&&HasNew(e,unexpected)&&cmd.Interrupts==1&&unexpected.Interrupts==1,
                "unexpected fifth-pair movement preserves FAIL and exact cleanup custody beyond the legacy four actors");
        }
        {
            var e=Fresh(true);var cmd=new UnitMoveTo(e.A(11));var ability=new UnitUseAbility{Executor=e.A(8),Type=UnitCommand.CommandType.Standard,IsFinished=true,ExecutionProcess=new Execution()};
            ClickGroundHandler.OnMove=_=>{e.A(11).Commands.Storage[3]=cmd;e.A(8).Commands.Storage[1]=ability;};
            Run(e,"six-process",10,11,0);e.Drain();
            Check(e.Awaited.Contains(ability)&&ability.Interrupts==0&&!e.Settled(),"live native process on fifth pair is awaited without borrowing cancellation authority");
            ability.ExecutionProcess.IsEnded=true;Check(e.Settled(),"six-pair cleanup settles only after the exact neighboring native process ends");
        }
        {
            var e=Fresh(true);for(var i=0;i<7;i++){ClickGroundHandler.OnMove=_=>e.A(11).Commands.Storage[3]=new UnitMoveTo(e.A(11));Run(e,"six-limit",10,11,0);}
            Check(e.Attempts.Count==6&&ClickGroundHandler.Calls==6,"six-pair case dispatches at most six declared native ground inputs");
        }
    }
    internal static int Main(){try{MainCases();}catch(Exception e){Check(false,"unexpected test exception: "+e);}Console.WriteLine("RESULT "+passed+" PASS / "+failed+" FAIL");return failed==0?0:1;}
}
