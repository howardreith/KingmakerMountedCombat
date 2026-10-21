using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

// Main.OnUpdate, CompositionRoot.Update/UpdateCore, trace forwarding methods,
// host entry prefix and result DTOs/settings are extracted from current sources.
// Native services, Unity values, the remaining host/scenario and logger are facades.
namespace KingmakerMountedCombat.Diagnostics
{
    static class Values
    {
        internal static long Ticks; internal static int Frame=100,ClockReads;
        internal static bool Focus=true,Background=false,ThrowClock,ThrowFrame,ThrowFocus,ThrowBackground;
        internal static long Clock(){ClockReads++;if(ThrowClock)throw new InvalidOperationException("clock");return Ticks;}
        internal static int ReadFrame(){if(ThrowFrame)throw new InvalidOperationException("frame");return Frame;}
        internal static bool ReadFocus(){if(ThrowFocus)throw new InvalidOperationException("focus");return Focus;}
        internal static bool ReadBackground(){if(ThrowBackground)throw new InvalidOperationException("background");return Background;}
        internal static RuntimeStartupTrace New(){return new RuntimeStartupTrace(1000,Clock,ReadFrame,ReadFocus,ReadBackground);}
        internal static void Reset(){Ticks=0;Frame=100;ClockReads=0;Focus=true;Background=false;ThrowClock=ThrowFrame=ThrowFocus=ThrowBackground=false;Calls.Clear();Fault=null;LogFault=null;MeasurementEndFault=false;MountedRuntimeMeasurement.Current=null;}
        internal static readonly List<string> Calls=new List<string>();
        internal static string Fault; internal static Exception LogFault; internal static bool MeasurementEndFault; internal static readonly Exception NativeError=new InvalidOperationException("native boundary error");
        internal static void Effect(string name){Calls.Add(name);Ticks+=7;if(Fault==name)throw NativeError;}
    }
    sealed class Unit { internal Combat Combat=new Combat(); }
    sealed class Combat { internal void UpdateChargeCleanup(){Values.Effect("charge");} }
    sealed class World { internal void Visit(Action<Unit> action){action(new Unit());}internal void UpdatePairs(){Values.Effect("pairs");} }
    sealed class Service { private string name;internal Service(string n){name=n;}internal void Update(){Values.Effect(name);} }
    sealed class Persistence { internal bool ActiveSaveWorkerRunning,SaveSuspended,AreaTransitionPending,CombatRestorationPending;internal void Update(){Values.Effect("persistence");} }
    sealed class Telemetry { internal void Update(float x){Values.Effect("telemetry");} }
    sealed class MountedRuntimeMeasurement { internal static MountedRuntimeMeasurement Current;internal long BeginFrame(){Values.Effect("measure-start");return 0;}internal void EndNativeAllocationWindow(){Values.Effect("measure-native-end");if(Values.MeasurementEndFault)throw new InvalidOperationException("observer close");}internal void EndFrame(long x){Values.Effect("measure-end");}internal long BeginPhase(){return 0;}internal void EndPairUpdate(long x){}internal void EndTurnUpdate(long x){} }
    sealed class RuntimeAutomationHost
    {
        internal readonly RuntimeStartupTrace startupTrace; private bool disposed,completed;
        internal bool IsSaveBackedFailurePending,IsCompleted; private int frameCount; private double elapsedSeconds;
        private readonly Stopwatch runtimeClock=Stopwatch.StartNew(); internal RuntimeStartupTraceSnapshot DuringHost;
        internal RuntimeAutomationHost(){startupTrace=Values.New();}
__HOST_TRACE__
__HOST_UPDATE_PREFIX__
            DuringHost=startupTrace.Capture();
            Values.Effect("host");
        }
    }
    sealed class CompositionRoot
    {
        internal RuntimeAutomationHost runtimeAutomation;
        private Service horseCompanion=new Service("horse"),removal=new Service("removal"),nativeControls=new Service("controls"),unifiedTurn=new Service("unified");
        private Persistence persistence=new Persistence();private World world=new World();private Telemetry movementTelemetry=new Telemetry();
        private bool failureCleanupPending;internal bool IsEnabled=true;internal int Failures;internal Exception Failure;
        internal CompositionRoot(bool accepted){if(accepted)runtimeAutomation=new RuntimeAutomationHost();}
        private void ThrowIfDisposed(){Values.Effect("disposed-check");}
        private void PerformFailureCleanup(){Values.Effect("cleanup");}
        internal void HandleUpdateFailure(Exception e){Failures++;Failure=e;Values.Effect("handle-failure");}
__COMP_TRACE__
__COMP_UPDATE__
__COMP_CORE__
    }
    sealed class Log { internal readonly List<Exception> Errors=new List<Exception>();internal void LogException(string label,Exception e){Errors.Add(e);if(Values.LogFault!=null)throw Values.LogFault;} }
    sealed class UnityModManager { internal sealed class ModEntry { internal Log Logger=new Log(); } }
    static class Main
    {
        internal static CompositionRoot root;
__MAIN_METHOD__
        internal static UnityModManager.ModEntry Invoke(){var m=new UnityModManager.ModEntry();OnUpdate(m,0.016f);return m;}
    }
    sealed class RuntimeFixtureIdentity { }
    sealed class RuntimeSubscenarioResult { }
    static class ResultFormatter
    {
__JSON_SETTINGS__
__RESULT1__
__RESULT2__
        internal static string Format(RuntimeStartupTraceSnapshot s,bool v2){return JsonConvert.SerializeObject(v2?(object)new RuntimeGameResultV2{StartupTrace=s}:new RuntimeGameResult{StartupTrace=s},JsonSettings);}
        internal static string Trace(RuntimeStartupTraceSnapshot s){return JsonConvert.SerializeObject(s,JsonSettings);}
    }
    static class Tests
    {
        private static int passed,failed;
        private static void Check(string name,bool ok){if(ok){passed++;Console.WriteLine("PASS "+name);}else{failed++;Console.WriteLine("FAIL "+name);}}
        private static void Normal(RuntimeStartupTrace t){t.BeginCallback();t.PreHostStart();t.PreHostEnd();t.HostEntry();t.EndCallback();}
        private static void Emit(string root,string name,string value){using(var f=new FileStream(Path.Combine(root,name),FileMode.CreateNew,FileAccess.Write))using(var w=new StreamWriter(f,new System.Text.UTF8Encoding(false)))w.Write(value);}
        public static int Run(string output)
        {
            Directory.CreateDirectory(output);Values.Reset();var t=Values.New();var s=t.Capture();
            Check("accepted recorder is initially empty",s.CallbacksObserved==0&&s.Samples.Count==0&&!s.ReadFailure&&!s.OrderingFailure);
            Emit(output,"empty.json",ResultFormatter.Trace(s));
            Normal(t);s=t.Capture();Check("one callback has exact five phases",s.Samples.Select(x=>x.Phase).SequenceEqual(new[]{"on-update-entry","pre-host-start","pre-host-end","host-entry","on-update-exit"}));
            Check("Unity frame is native sample not callback number",s.Samples.All(x=>x.UnityFrame==100&&x.Callback==1));
            Check("flags are sampled without mutation",s.Samples.All(x=>x.IsFocused==true&&x.RunInBackground==false)&&Values.Focus&&!Values.Background);
            var prior=s;for(int i=1;i<10;i++){Values.Frame++;Values.Ticks+=20;Normal(t);}s=t.Capture();
            Check("ten callbacks have50 samples",s.CallbacksObserved==10&&s.ClosedCallbacks==10&&s.Samples.Count==50);
            var reads=Values.ClockReads;for(int i=0;i<50;i++)Normal(t);
            Check("after cap no getter is read",Values.ClockReads==reads&&t.Capture().Samples.Count==50);
            Check("earlier snapshot remains unchanged",prior.Samples.Count==5&&prior.CallbacksObserved==1);
            Emit(output,"full.json",ResultFormatter.Trace(s));
            var a=(RuntimeStartupTraceSample[])s.Samples;a[0]=default(RuntimeStartupTraceSample);
            Check("snapshot array does not mutate recorder",t.Capture().Samples[0].Phase=="on-update-entry");
            Values.Reset();t=Values.New();t.BeginCallback();t.PreHostStart();t.EndCallback();s=t.Capture();
            Check("native interruption retains exact partial callback",s.Samples.Count==3&&s.ClosedCallbacks==1&&!s.OrderingFailure&&s.Samples[2].Phase=="on-update-exit");
            Emit(output,"partial.json",ResultFormatter.Trace(s));
            t=Values.New();t.PreHostStart();Check("phase without callback stops as uncertain",t.Capture().OrderingFailure&&t.Capture().Samples.Count==0);
            t=Values.New();t.BeginCallback();t.PreHostStart();t.PreHostStart();Check("duplicate phase stops without invented row",t.Capture().OrderingFailure&&t.Capture().Samples.Count==2);
            t=Values.New();t.BeginCallback();t.BeginCallback();Check("nested callback refuses ordering",t.Capture().OrderingFailure&&t.Capture().CallbacksObserved==1);
            t=Values.New();for(int i=0;i<9;i++)Normal(t);t.BeginCallback();t.BeginCallback();Check("tenth nested callback does not bypass cap gate",t.Capture().OrderingFailure&&t.Capture().Samples.Count==46);
            Values.Reset();Values.ThrowClock=Values.ThrowFocus=true;t=Values.New();Normal(t);s=t.Capture();
            Check("observer read errors do not throw",s.ReadFailure&&s.Samples.Count==5&&s.Samples.All(x=>x.ElapsedTicks==null&&x.IsFocused==null));
            Check("independent successful getters remain observed",s.Samples.All(x=>x.UnityFrame==100&&x.RunInBackground==false));
            Emit(output,"unavailable.json",ResultFormatter.Trace(s));
            Values.Reset();t=Values.New();Values.Ticks=10;t.BeginCallback();Values.Ticks=9;t.PreHostStart();Check("regressing clock becomes unavailable",t.Capture().ReadFailure&&t.Capture().Samples[1].ElapsedTicks==null);
            Values.Reset();Values.Frame=-1;t=Values.New();t.BeginCallback();Check("invalid Unity frame is unavailable",t.Capture().Samples[0].UnityFrame==null&&t.Capture().ReadFailure);
            Values.Reset();Values.ThrowFrame=Values.ThrowBackground=true;t=Values.New();t.BeginCallback();Check("other getter failures stay bounded",t.Capture().ReadFailure&&t.Capture().Samples[0].RunInBackground==null&&t.Capture().Samples[0].UnityFrame==null);
            Values.Reset();t=Values.New();t.BeginCallback();Values.Focus=false;Values.Background=true;t.PreHostStart();Check("focus transition is raw and no constant inferred",t.Capture().Samples[0].IsFocused==true&&t.Capture().Samples[1].IsFocused==false&&t.Capture().Samples[1].RunInBackground==true);
            Values.Reset();Main.root=new CompositionRoot(true);Main.Invoke();s=Main.root.runtimeAutomation.startupTrace.Capture();
            Check("actual native callback traverses five trace markers",s.Samples.Count==5&&!s.OrderingFailure&&s.ClosedCallbacks==1);
            Check("original service calls remain exactly once",Values.Calls.SequenceEqual(new[]{"disposed-check","horse","persistence","charge","removal","controls","host","telemetry","pairs","unified"}));
            Check("pre-host span excludes later mod work",s.Samples[2].ElapsedTicks-s.Samples[1].ElapsedTicks==42&&s.Samples[4].ElapsedTicks-s.Samples[3].ElapsedTicks==28);
            Values.Ticks+=1000;Main.Invoke();s=Main.root.runtimeAutomation.startupTrace.Capture();
            Check("exit-to-next-entry isolates callback gap",s.Samples[5].ElapsedTicks-s.Samples[4].ElapsedTicks==1000);
            Check("saved host prefix precedes callback exit",Main.root.runtimeAutomation.DuringHost.ClosedCallbacks==1&&Main.root.runtimeAutomation.DuringHost.Samples.Count==9);
            Values.Reset();Main.root=new CompositionRoot(false);Main.Invoke();Check("ordinary play creates no recorder or clock reads",Main.root.runtimeAutomation==null&&Values.ClockReads==0);
            Check("ordinary play still executes service sequence",Values.Calls.Count==9&&!Values.Calls.Contains("host"));
            Values.Reset();Main.root=null;Main.Invoke();Check("absent root stays inert",Values.ClockReads==0&&Values.Calls.Count==0);
            foreach(var stage in new[]{"disposed-check","horse","persistence","charge","removal","controls","host","telemetry","pairs","unified"}){
                Values.Reset();Main.root=new CompositionRoot(true);Values.Fault=stage;var log=Main.Invoke();s=Main.root.runtimeAutomation.startupTrace.Capture();
                Check("original native exception path "+stage,log.Logger.Errors.Count==1&&ReferenceEquals(log.Logger.Errors[0],Values.NativeError)&&ReferenceEquals(Main.root.Failure,Values.NativeError)&&Main.root.Failures==1&&s.ClosedCallbacks==1&&!s.OrderingFailure&&Values.Calls.Count(x=>x==stage)==1);
            }
            Values.Reset();Main.root=new CompositionRoot(true);MountedRuntimeMeasurement.Current=new MountedRuntimeMeasurement();Values.Fault="horse";Values.MeasurementEndFault=true;var measuredFault=Main.Invoke();
            Check("native Update error survives a failing F11 observer close and completes frame cleanup",ReferenceEquals(Main.root.Failure,Values.NativeError)&&ReferenceEquals(measuredFault.Logger.Errors.Single(),Values.NativeError)&&Values.Calls.Count(x=>x=="measure-native-end")==1&&Values.Calls.Count(x=>x=="measure-end")==1);
            Values.Reset();Main.root=new CompositionRoot(true);Values.Fault="horse";Values.LogFault=new IOException("logger boundary");Exception escaped=null;try{Main.Invoke();}catch(Exception e){escaped=e;}
            Check("trace finally does not replace logger exception",ReferenceEquals(escaped,Values.LogFault)&&Main.root.runtimeAutomation.startupTrace.Capture().ClosedCallbacks==1);
            Values.Reset();Main.root=new CompositionRoot(true);Values.ThrowClock=Values.ThrowFrame=Values.ThrowFocus=Values.ThrowBackground=true;Main.Invoke();
            Check("all passive reads may fail without changing service flow",Values.Calls.Count==10&&Main.root.Failures==0&&Main.root.runtimeAutomation.startupTrace.Capture().ReadFailure);
            Values.Reset();Main.root=new CompositionRoot(true);for(int i=0;i<10;i++){Values.Ticks+=50;Main.Invoke();}s=Main.root.runtimeAutomation.DuringHost;
            Check("tenth host result is truthful49-sample prefix",s.CallbacksObserved==10&&s.ClosedCallbacks==9&&s.Samples.Count==49);
            Emit(output,"terminal-prefix.json",ResultFormatter.Trace(s));
            Emit(output,"result-v1.json",ResultFormatter.Format(s,false));Emit(output,"result-v2.json",ResultFormatter.Format(s,true));
            Check("actual v1 DTO emits startup subtree",ResultFormatter.Format(s,false).Contains("\"startupTrace\""));
            Check("actual v2 DTO emits startup subtree",ResultFormatter.Format(s,true).Contains("\"startupTrace\""));
            Check("bootstrap construction failure omits absent v1 trace",!ResultFormatter.Format(null,false).Contains("\"startupTrace\""));
            Check("bootstrap construction failure omits absent v2 trace",!ResultFormatter.Format(null,true).Contains("\"startupTrace\""));
            Console.WriteLine("STARTUP TRACE PASS="+passed+" FAIL="+failed);return failed==0?0:1;
        }
    }
}
class Program { static int Main(string[] args){return KingmakerMountedCombat.Diagnostics.Tests.Run(args[0]);} }
