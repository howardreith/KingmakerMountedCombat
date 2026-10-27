using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Diagnostics;
using System.Threading;
using System.Runtime.Remoting.Proxies;
using System.Runtime.Remoting.Messaging;
internal static class QuietWindowContracts {
    const BindingFlags F=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance;
    static int passed;
    static Type window,writer,measurement,world;
    static object Blank(Type t){return FormatterServices.GetUninitializedObject(t);}
    static FieldInfo Field(Type t,string n){var f=t.GetField(n,F);if(f==null)throw new MissingFieldException(t.FullName,n);return f;}
    static void Set(object o,string n,object v){Field(o.GetType(),n).SetValue(o,v);}
    static object Get(object o,string n){return Field(o.GetType(),n).GetValue(o);}
    static object Call(object o,string n,params object[] a){return o.GetType().GetMethod(n,F).Invoke(o,a);}
    static bool Active(){return (bool)window.GetProperty("IsActive",F).GetValue(null,null);}
    static Exception Error(Action action){try{action();return null;}catch(TargetInvocationException e){return e.InnerException;}catch(Exception e){return e;}}
    static void Check(bool ok,string name){if(!ok)throw new Exception(name);passed++;Console.WriteLine("PASS "+name);}
    static object Writer(bool clock=true){var v=Blank(writer);Set(v,"intervalSeconds",0.1d);Set(v,"lastSampleSeconds",0d);Set(v,"sequence",17L);Set(v,"created",true);if(clock)Set(v,"clock",Stopwatch.StartNew());return v;}
    static object Window(object w,string label="mammoth"){return Activator.CreateInstance(window,F,null,new[]{w,(object)label},null);}
    sealed class Logger:RealProxy {internal Logger(Type t):base(t){}public override IMessage Invoke(IMessage m){return new ReturnMessage(null,null,0,null,(IMethodCallMessage)m);}}
    public static int Main(string[] args){
        try {
            var managed=args[0];AppDomain.CurrentDomain.AssemblyResolve+=(s,e)=>{var leaf=new AssemblyName(e.Name).Name+".dll";foreach(var dir in new[]{managed,Path.Combine(managed,"UnityModManager")}){var path=Path.Combine(dir,leaf);if(File.Exists(path))return Assembly.LoadFrom(path);}return null;};
            var mod=Assembly.LoadFrom(args[1]);Func<string,Type> type=n=>mod.GetType("KingmakerMountedCombat.Diagnostics."+n,true);
            window=type("MountedOptionalDiagnosticsWindow");writer=type("MovementTelemetryWriter");measurement=type("MountedRuntimeMeasurement");world=mod.GetType("KingmakerMountedCombat.Integration.MountedWorldService",true);
            var priorWindow=Field(window,"current").GetValue(null);var priorMeasurement=Field(measurement,"current").GetValue(null);
            Check(priorWindow==null&&priorMeasurement==null,"ordinary assembly load has no quiet gate or measurement observer");
            try {
                var settings=Activator.CreateInstance(type("DiagnosticSettings"));Check(!(bool)settings.GetType().GetProperty("EnableDiagnosticOverlay").GetValue(settings,null),"ordinary settings leave diagnostic overlay disabled");
                var host=type("RuntimeAutomationHost");var factory=host.GetMethod("CreateFromCommandLine",F);var a=new object[factory.GetParameters().Length];foreach(var p in factory.GetParameters()){if(p.Name=="logger")a[p.Position]=new Logger(mod.GetType("KingmakerMountedCombat.Logging.IModLogger",true)).GetTransparentProxy();else if(p.Name=="loadedModId")a[p.Position]="KingmakerMountedCombat";else if(p.Name=="relationshipStateProvider")a[p.Position]=new Func<string>(()=>"Unmounted");else if(p.Name=="movementExperimentProvider")a[p.Position]=new Func<bool>(()=>true);}
                Check(factory.Invoke(null,a)==null,"actual no-request factory creates no runtime automation host");
                var broken=Writer(false);var failed=Window(broken);Check(Error(()=>Call(failed,"Install",1,1d))!=null&&!Active(),"fallible entry timer capture publishes no optional gate");Call(failed,"Dispose");
                var w=Writer();var q=Window(w);Check(!Active(),"capture-only quiet constructor leaves telemetry unchanged");Call(q,"Install",10,1d);Check(Active(),"caller-owned install publishes its exact quiet gate");
                Check((bool)window.GetMethod("Suppresses",F).Invoke(null,new[]{w}),"gate suppresses only its exact writer");var other=Writer();Check(!(bool)window.GetMethod("Suppresses",F).Invoke(null,new[]{other}),"foreign optional writer is not suppressed");
                var duplicate=Window(w);Check(Error(()=>Call(duplicate,"Install",10,1d)) is InvalidOperationException,"nested quiet admission cannot replace existing ownership");Call(duplicate,"Dispose");Check(Active(),"disposing an unpublished nested attempt retains original gate");
                Check(!(bool)Call(q,"Ready",21,2d)&&!(bool)Call(q,"Ready",30,1.1d),"native quiet observation requires both frame and wall-clock bounds");
                Check(Error(()=>Activator.CreateInstance(measurement,F,null,new[]{(object)"forbidden",Blank(world)},null)) is InvalidOperationException,"actual measurement constructor refuses before acquiring observations during quiet interval");
                Thread.Sleep(300);Call(w,"Update",0.016f);Check((double)Get(w,"lastSampleSeconds")==0d&&(long)Get(w,"sequence")==17L&&(bool)Get(w,"created"),"actual telemetry Update gates before Stopwatch sampling even after cadence elapsed");
                var receipt=Call(q,"Complete",30,2d);Check(receipt!=null&&!Active(),"complete multi-frame window restores absent prior gate and emits raw receipt");Check(Error(()=>Call(q,"Complete",31,2.1d)) is InvalidOperationException,"completed window cannot emit duplicate success");Call(q,"Dispose");Check(!Active(),"quiet disposal remains idempotent");
                var nativeEntry=Error(()=>Call(w,"Update",0.016f));Check((nativeEntry is NullReferenceException || nativeEntry is System.Security.SecurityException)&&(double)Get(w,"lastSampleSeconds")>0d,"released writer resumes actual sampling prelude before unavailable offline native capture");
                var measured=Blank(measurement);Field(measurement,"current").SetValue(null,measured);var blocked=Window(Writer());Check(Error(()=>Call(blocked,"Install",1,1d)) is InvalidOperationException&&!Active(),"existing measurement prevents quiet admission without clearing it");Check(Object.ReferenceEquals(Field(measurement,"current").GetValue(null),measured),"prior measurement custody remains exact after refusal");Field(measurement,"current").SetValue(null,null);Call(blocked,"Dispose");
                var timeout=Window(Writer());Call(timeout,"Install",10,1d);Check(Error(()=>Call(timeout,"Ready",1811,2d)) is InvalidOperationException&&Active(),"frame timeout preserves gate for explicit cleanup");Check(Error(()=>Call(timeout,"Ready",20,17d)) is InvalidOperationException&&Active(),"wall-clock timeout preserves gate for explicit cleanup");Call(timeout,"Dispose");
                var driftWriter=Writer();var drift=Window(driftWriter);Call(drift,"Install",1,1d);Set(driftWriter,"lastSampleSeconds",1d);Check(Error(()=>Call(drift,"Complete",20,2d)) is InvalidOperationException&&Active(),"changed sampling state refuses evidence and retains cleanup custody");Call(drift,"Dispose");
                var captured=Window(Writer());Call(captured,"Install",1,1d);var foreign=Window(Writer());Field(window,"current").SetValue(null,foreign);Check(Error(()=>Call(captured,"Dispose")) is InvalidOperationException&&Object.ReferenceEquals(Field(window,"current").GetValue(null),foreign),"cleanup cannot clear a foreign window token");Field(window,"current").SetValue(null,captured);Call(captured,"Dispose");Check(!Active(),"same retained token can retry exact gate restoration");
                foreach(var bad in new[]{Double.NaN,Double.PositiveInfinity,-1d}){var invalid=Window(Writer());Check(Error(()=>Call(invalid,"Install",1,bad)) is InvalidOperationException&&!Active(),"invalid clock refuses gate publication: "+bad);Call(invalid,"Dispose");}
                var largeInterval=Writer();Set(largeInterval,"intervalSeconds",1d);var longer=Window(largeInterval);Call(longer,"Install",1,1d);Check(!(bool)Call(longer,"Ready",20,1.5d),"quiet interval spans at least two exact prior telemetry cadences");Call(longer,"Dispose");
            } finally {Field(window,"current").SetValue(null,priorWindow);Field(measurement,"current").SetValue(null,priorMeasurement);}
            Console.WriteLine("QUIET CONTRACTS PASS="+passed);return 0;
        } catch(Exception e){Console.WriteLine(e);return 1;}
    }
}
