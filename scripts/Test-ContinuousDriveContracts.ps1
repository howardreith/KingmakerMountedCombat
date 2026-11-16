[CmdletBinding()]
param([ValidateSet('Debug','Release')][string]$Configuration='Release',[string]$ProductDll)
# Known issues sprint 3 continuation: M05 (gamepad stick movement of a mounted pair) and the group-pace fault report,
# with the corrections of the review of 5e27c9bd (R1: retiring the owned command without clearing a foreign
# override; R2: rider-order admission through queue promotion).
#
# Four kinds of evidence, none of which launches anything. Each check's label names its kind:
#   NATIVE-IL         the installed Assembly-CSharp is READ: which method creates the continuous command, which unit it
#                     is initialised and run on, where its agent override is installed and removed, who reads that
#                     override, and how a unit is moved.
#   NATIVE-EXECUTED   installed native code is RUN on genuine engine objects (UnitCommands, UnitMoveContiniously,
#                     UnitEntityData) built without the game: native UnitCommands.Run is executed with a command whose
#                     executor is another unit. Only what can run without the Unity player is executed; the label says
#                     where execution stops.
#   PRODUCT-EXECUTED  the compiled KingmakerMountedCombat.dll is RUN: its transpilers on the installed methods' real
#                     instruction streams, its hooks, its ownership record and decisions, and its fault report with a
#                     logger that throws during a real operational failure.
#   PRODUCT-IL        the compiled product is READ for what its integration methods call and in which order.
# -ProductDll reads another build (for example the reviewed head 1223eb2f, which must FAIL the PRODUCT groups). Every
# check runs; the script exits 1 if any failed. This is not native gameplay evidence.
$ErrorActionPreference='Stop'
$kmcRepo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$kmcLab=[IO.Path]::GetFullPath((Join-Path $kmcRepo '../..'))
$kmcLayout=(Get-Content -Raw (Join-Path $kmcLab 'environment-intake.json')|ConvertFrom-Json).requestedLayout
$kmcManaged=Join-Path $kmcLayout.kingmakerInstallDir 'Kingmaker_Data/Managed'
$kmcDll=if($ProductDll){[IO.Path]::GetFullPath($ProductDll)}else{Join-Path $kmcRepo "bin/$Configuration/KingmakerMountedCombat.dll"}
if(-not(Test-Path -LiteralPath $kmcDll -PathType Leaf)){Write-Output "The product build is missing: $kmcDll";exit 1}
Add-Type -TypeDefinition @'
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using System.Runtime.Serialization;

// A logger that throws on every call: a transparent proxy for the product's own (internal) logger interface.
public sealed class KmcThrowingLogger : RealProxy
{
    public static int Calls;
    public KmcThrowingLogger(Type type) : base(type) { }
    public override IMessage Invoke(IMessage message)
    {
        Calls++;
        return new ReturnMessage(new InvalidOperationException("the logger failed"), (IMethodCallMessage)message);
    }
}

// A logger that records what the product logs.
public sealed class KmcRecordingLogger : RealProxy
{
    public readonly List<string> Lines=new List<string>();
    public KmcRecordingLogger(Type type) : base(type) { }
    public override IMessage Invoke(IMessage message)
    {
        var call=(IMethodCallMessage)message;
        if(call.MethodName=="GetType") return new ReturnMessage(GetProxiedType(),null,0,call.LogicalCallContext,call);
        Lines.Add(call.MethodName+": "+(call.ArgCount>0?Convert.ToString(call.GetArg(0)):""));
        return new ReturnMessage(null,null,0,call.LogicalCallContext,call);
    }
}

// A scripted stand-in for the game adapter of the product's stick-movement owner: a transparent proxy for the
// product's own host interface. The mount unit and its current view are separate identities, each view keeps its
// own override, and Interrupt behaves as the native call does: the command becomes terminal first, and then its
// end removes the override of the mount's CURRENT view, unless the compiled owner's own guard forbids it (a build
// without that guard gets the plain native rule). It answers both shapes of the interface (the reviewed head's
// MountView, and ViewOf/IsHeld).
public sealed class KmcScriptedDriveHost : RealProxy
{
    public bool PairMounted=true, Gated, AgentIsContinuous=true, EndCallbackFails;
    public long Generation=1;
    public int Frame=1, FailInterrupts, FailClears, Forgotten;
    public object Mount=new object(), CurrentView=new object(), SlotCommand, Executor, Owner;
    public readonly Dictionary<object,object> Overrides=new Dictionary<object,object>();
    public readonly List<object> Finished=new List<object>();
    public readonly List<string> Calls=new List<string>();
    public KmcScriptedDriveHost(Type type) : base(type) { Executor=Mount; }

    public object OverrideOn(object view) { object found; return view!=null && Overrides.TryGetValue(view,out found)?found:null; }

    // The native end of a continuous command, as the installed OnEnded with the product's guard inside it.
    public void NativeEnd(object command)
    {
        if(Finished.Contains(command)) return;
        Finished.Add(command);
        if(ReferenceEquals(SlotCommand,command)) SlotCommand=null;
        if(EndCallbackFails){ EndCallbackFails=false; throw new InvalidOperationException("the end callback failed after the command became terminal"); }
        var current=OverrideOn(CurrentView);
        if(current==null) return;
        var guard=Owner==null?null:Owner.GetType().GetMethod("NativeEndMayClearOverride");
        if(guard==null || (bool)guard.Invoke(Owner,new object[]{command,CurrentView,current})) Overrides[CurrentView]=null;
    }

    public override IMessage Invoke(IMessage message)
    {
        var call=(IMethodCallMessage)message;
        object result=null;
        try
        {
            switch(call.MethodName)
            {
                case "GetType": result=GetProxiedType(); break;
                case "get_PairMounted": result=PairMounted; break;
                case "get_PairGeneration": result=Generation; break;
                case "get_Frame": result=Frame; break;
                case "get_StickInputGated": result=Gated; break;
                case "get_Mount": result=Mount; break;
                case "get_MountView": result=CurrentView; break;
                case "ViewOf": result=ReferenceEquals(call.GetArg(0),Mount)?CurrentView:null; break;
                case "ExecutorOf": result=Executor; break;
                case "IsFinished": result=Finished.Contains(call.GetArg(0)); break;
                case "InMountMoveSlot": result=SlotCommand!=null && ReferenceEquals(SlotCommand,call.GetArg(0)); break;
                case "IsHeld": result=SlotCommand!=null && ReferenceEquals(SlotCommand,call.GetArg(0)); break;
                case "OverrideOf": result=OverrideOn(call.GetArg(0)); break;
                case "IsContinuousAgent": result=AgentIsContinuous && call.GetArg(0)!=null; break;
                case "Interrupt":
                    Calls.Add("Interrupt("+call.GetArg(1)+")");
                    if(FailInterrupts>0){ FailInterrupts--; throw new InvalidOperationException("the interrupt failed before the native call"); }
                    NativeEnd(call.GetArg(0));
                    break;
                case "ClearOverride":
                    Calls.Add("ClearOverride");
                    if(FailClears>0){ FailClears--; throw new InvalidOperationException("the override could not be removed"); }
                    Overrides[call.GetArg(0)]=null;
                    break;
                case "ReleaseRiderHold": Calls.Add("ReleaseRiderHold"); break;
                case "Forget": Forgotten++; break;
                case "Report": Calls.Add("Report("+call.GetArg(0)+","+call.GetArg(2)+")"); break;
                default: throw new InvalidOperationException("unexpected host call "+call.MethodName);
            }
        }
        catch(Exception error)
        {
            var inner=error;
            while(inner is TargetInvocationException && inner.InnerException!=null) inner=inner.InnerException;
            return new ReturnMessage(inner,call);
        }
        return new ReturnMessage(result,null,0,call.LogicalCallContext,call);
    }
    public string Log { get { return string.Join(" ",Calls.ToArray()); } }
}

public static class KmcContinuousDriveProbe
{
    private static int passes, failures;
    private static MethodInfo read;
    private const BindingFlags All=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly;
    private const BindingFlags Any=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;

    private static Exception Innermost(Exception error)
    {
        var inner=error;
        while((inner is TargetInvocationException || inner is TypeInitializationException) && inner.InnerException!=null) inner=inner.InnerException;
        return inner;
    }

    private static void Check(string label, Func<bool> body)
    {
        string detail=null; var ok=false;
        try { ok=body(); }
        catch(Exception error) { var inner=Innermost(error); detail=inner.GetType().Name+": "+inner.Message; }
        if(ok){ passes++; Console.WriteLine("PASS "+label); }
        else { failures++; Console.WriteLine("FAIL "+label+(detail!=null?" ["+detail+"]":"")); }
    }

    private sealed class Ins { public OpCode Op; public object Operand; public MethodBase Call; public FieldInfo Field; public List<Label> Labels=new List<Label>(); }

    private static Ins Read(object raw)
    {
        var ins=new Ins();
        ins.Op=(OpCode)raw.GetType().GetField("opcode").GetValue(raw);
        ins.Operand=raw.GetType().GetField("operand").GetValue(raw);
        ins.Call=ins.Operand as MethodBase; ins.Field=ins.Operand as FieldInfo;
        var labels=raw.GetType().GetField("labels").GetValue(raw) as IEnumerable;
        if(labels!=null) foreach(var label in labels) ins.Labels.Add((Label)label);
        return ins;
    }

    private static List<Ins> Body(MethodBase method)
    {
        if(method==null) throw new InvalidOperationException("the method does not exist in this build");
        var found=new List<Ins>();
        foreach(var raw in (IEnumerable)read.Invoke(null,new object[]{method,null})) found.Add(Read(raw));
        return found;
    }

    private static bool Is(Ins ins, string type, string name)
    {
        return ins.Call!=null && ins.Call.Name==name && (type==null || ins.Call.DeclaringType.Name==type || ins.Call.DeclaringType.FullName==type);
    }

    private static List<int> Find(List<Ins> body, string type, string name)
    {
        var found=new List<int>();
        for(var i=0;i<body.Count;i++) if(Is(body[i],type,name)) found.Add(i);
        return found;
    }

    private static int Count(List<Ins> body, string type, string name) { return Find(body,type,name).Count; }

    private static int One(List<Ins> body, string type, string name)
    {
        var found=Find(body,type,name);
        if(found.Count!=1) throw new InvalidOperationException(name+" is referenced "+found.Count+" times, expected once");
        return found[0];
    }

    private static int First(List<Ins> body, string type, string name)
    {
        var found=Find(body,type,name);
        if(found.Count==0) throw new InvalidOperationException(name+" is not referenced");
        return found[0];
    }

    private static MethodBase Method(Type type, string name, int parameters)
    {
        if(type==null) throw new InvalidOperationException("the type does not exist in this build");
        foreach(var member in type.GetMember(name,MemberTypes.Method|MemberTypes.Constructor,All))
        {
            var method=(MethodBase)member;
            if(parameters<0 || method.GetParameters().Length==parameters) return method;
        }
        return null;
    }

    // The compiler-generated methods of the lambdas written inside one method.
    private static List<MethodBase> Lambdas(Type type, string enclosing)
    {
        var found=new List<MethodBase>();
        var types=new List<Type>{type};
        types.AddRange(type.GetNestedTypes(Any));
        foreach(var candidate in types)
            foreach(var method in candidate.GetMethods(All))
                if(method.Name.StartsWith("<"+enclosing+">",StringComparison.Ordinal) && method.GetMethodBody()!=null) found.Add(method);
        return found;
    }

    private static MethodBase Required(Type type, string name, int parameters)
    {
        var method=Method(type,name,parameters);
        if(method==null) throw new InvalidOperationException(name+" does not exist in this build");
        return method;
    }

    private static MethodBase Token(Type type, string name, int token)
    {
        foreach(var member in type.GetMember(name,MemberTypes.Method|MemberTypes.Constructor,All))
            if(((MethodBase)member).MetadataToken==token) return (MethodBase)member;
        throw new InvalidOperationException(type.Name+"."+name+" with token "+token.ToString("X8")+" does not exist");
    }

    private static bool HasString(List<Ins> body, string value)
    {
        foreach(var ins in body) if(ins.Op==OpCodes.Ldstr && (string)ins.Operand==value) return true;
        return false;
    }

    private static bool LoadsInt(Ins ins, int value)
    {
        if(ins.Op==OpCodes.Ldc_I4 && ins.Operand is int) return (int)ins.Operand==value;
        if(ins.Op==OpCodes.Ldc_I4_S) return Convert.ToInt32(ins.Operand)==value;
        return false;
    }

    // Every method of the assembly whose IL calls (or constructs through) the given method definition.
    private static List<int> Callers(Assembly assembly, MethodBase target)
    {
        var token=target.MetadataToken;
        var found=new List<int>();
        Type[] types;
        try { types=assembly.GetTypes(); }
        catch(ReflectionTypeLoadException partial) { types=partial.Types; }
        foreach(var type in types)
        {
            if(type==null) continue;
            MemberInfo[] members;
            try { members=type.GetMembers(All); } catch(Exception) { continue; }
            foreach(var member in members)
            {
                var method=member as MethodBase;
                if(method==null) continue;
                byte[] il;
                try { var body=method.GetMethodBody(); if(body==null) continue; il=body.GetILAsByteArray(); } catch(Exception) { continue; }
                if(il==null) continue;
                for(var i=0;i+4<il.Length;i++)
                {
                    if(il[i]!=0x28 && il[i]!=0x6F && il[i]!=0x73) continue;
                    if(BitConverter.ToInt32(il,i+1)!=token) continue;
                    if(!found.Contains(method.MetadataToken)) found.Add(method.MetadataToken);
                    break;
                }
            }
        }
        found.Sort();
        return found;
    }

    private static string Hex(List<int> tokens)
    {
        var parts=new List<string>();
        foreach(var token in tokens) parts.Add(token.ToString("X8"));
        return string.Join(",",parts.ToArray());
    }

    private static object ToHarmony12(Type instructionType, List<Ins> body)
    {
        var list=(IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(instructionType));
        foreach(var ins in body)
        {
            // Branch targets travel with the instruction, as they do in a real patch.
            var made=Activator.CreateInstance(instructionType,new object[]{ins.Op,ins.Operand});
            var labels=(IList)instructionType.GetField("labels").GetValue(made);
            foreach(var label in ins.Labels) labels.Add(label);
            list.Add(made);
        }
        return list;
    }

    private static List<Ins> FromHarmony12(object instructions)
    {
        var found=new List<Ins>();
        foreach(var raw in (IEnumerable)instructions) found.Add(Read(raw));
        return found;
    }

    private static bool SameLabels(Ins a, Ins b)
    {
        if(a.Labels.Count!=b.Labels.Count) return false;
        for(var i=0;i<a.Labels.Count;i++) if(!a.Labels[i].Equals(b.Labels[i])) return false;
        return true;
    }

    private static bool Same(Ins a, Ins b) { return a.Op==b.Op && Equals(a.Operand,b.Operand) && SameLabels(a,b); }

    // The native instruction stream with the listed insertions (index in the native stream -> the call inserted before
    // it) and replacements (index -> the call that replaces it) must equal the transpiled stream exactly.
    private static void RequireOnly(List<Ins> before, List<Ins> after, Dictionary<int,List<MethodBase>> insertedBefore, Dictionary<int,MethodBase> replaced)
    {
        var cursor=0;
        for(var i=0;i<before.Count;i++)
        {
            if(insertedBefore.ContainsKey(i))
                foreach(var hook in insertedBefore[i])
                {
                    if(cursor>=after.Count || after[cursor].Op!=OpCodes.Call || !ReferenceEquals(after[cursor].Call,hook))
                        throw new InvalidOperationException("the call to "+hook.Name+" is not inserted before native instruction "+i);
                    if(after[cursor].Labels.Count!=0) throw new InvalidOperationException("the inserted call to "+hook.Name+" carries a branch target");
                    cursor++;
                }
            if(cursor>=after.Count) throw new InvalidOperationException("the transpiled stream ends at native instruction "+i);
            if(replaced.ContainsKey(i))
            {
                if(after[cursor].Op!=OpCodes.Call || !ReferenceEquals(after[cursor].Call,replaced[i]))
                    throw new InvalidOperationException("native instruction "+i+" is not replaced by "+replaced[i].Name);
                if(!SameLabels(after[cursor],before[i])) throw new InvalidOperationException("the replacement of native instruction "+i+" lost or gained a branch target");
            }
            else if(!Same(after[cursor],before[i]))
                throw new InvalidOperationException("unexpected change at native instruction "+i+": "+before[i].Op+" "+before[i].Operand+" -> "+after[cursor].Op+" "+after[cursor].Operand);
            cursor++;
        }
        if(cursor!=after.Count) throw new InvalidOperationException("the transpiled stream has "+(after.Count-cursor)+" trailing instructions");
    }

    private static void SetStatic(Type type, string name, object value)
    {
        var field=type.GetField(name,Any);
        if(field!=null){ field.SetValue(null,value); return; }
        var property=type.GetProperty(name,Any);
        if(property==null) throw new InvalidOperationException(type.Name+"."+name+" does not exist");
        property.SetValue(null,value,null);
    }

    // The once-only state of the product's group-pace fault report, whichever build is read.
    private static void ResetFaultReport(Type patchMethods)
    {
        var flag=patchMethods.GetField("groupPaceFaultReported",Any);
        if(flag!=null) flag.SetValue(null,false);
        var report=patchMethods.GetField("groupPaceFault",Any);
        if(report!=null) report.SetValue(null,Activator.CreateInstance(report.FieldType));
    }

    public static float SpeedOne<T>(T unit) { return 4.5f; }

    // The compiled owner on a scripted host; the host can ask that owner's guard, as the native end does.
    private static KmcScriptedDriveHost NewScriptedHost(Assembly product)
    {
        var hostType=product.GetType("KingmakerMountedCombat.Domain.IMountedContinuousDriveHost",true);
        var ownerType=product.GetType("KingmakerMountedCombat.Domain.MountedContinuousDriveOwner",true);
        var scripted=new KmcScriptedDriveHost(hostType);
        scripted.Owner=ownerType.GetConstructors()[0].Invoke(new object[]{scripted.GetTransparentProxy()});
        return scripted;
    }

    // A release as the lifecycle makes it deliberately, on either build's owner.
    private static void ReleaseOwner(object owner, string reason)
    {
        var release=owner.GetType().GetMethod("Release");
        release.Invoke(owner,release.GetParameters().Length==1?new object[]{reason}:new object[]{reason,true});
    }

    public static bool Refuse<T>(T trigger) { return false; }
    public static bool AlwaysRetained<T>(T rider, T mount) { return true; }
    public static void FailingSubscriber<T>(T trigger) { throw new InvalidOperationException("a dismount subscriber failed"); }

    // The product's real GameMountedRelationshipService, built without the game and unmounted. Its stick-movement owner
    // is replaced by the same compiled owner on the scripted host, because binding through the real adapter needs a
    // live pair. Everything the service then does about that owner's cleanup is the product's own code.
    private sealed class Lifecycle
    {
        public Assembly Product; public Type ServiceType, Trigger, Policy; public object Service; public KmcScriptedDriveHost Host; public KmcRecordingLogger Logger;
        public object Command=new object(), AgentA=new object(), ViewOne;

        public Lifecycle(Assembly product)
        {
            Product=product;
            ServiceType=product.GetType("KingmakerMountedCombat.Integration.GameMountedRelationshipService",true);
            Trigger=product.GetType("KingmakerMountedCombat.Domain.CleanupTrigger",true);
            Policy=product.GetType("KingmakerMountedCombat.Domain.MountedCleanupCompletionPolicy",false);
            var loggerType=product.GetType("KingmakerMountedCombat.Logging.IModLogger",true);
            Logger=new KmcRecordingLogger(loggerType);
            var settings=Activator.CreateInstance(product.GetType("KingmakerMountedCombat.Diagnostics.DiagnosticSettings",true));
            foreach(var ctor in ServiceType.GetConstructors(Any))
                if(ctor.GetParameters().Length==2) Service=ctor.Invoke(new object[]{Logger.GetTransparentProxy(),settings});
            if(Service==null) throw new InvalidOperationException("the relationship service could not be constructed");
            var field=ServiceType.GetField("continuousDrive",Any);
            if(field==null) throw new InvalidOperationException("the relationship service has no stick-movement owner in this build");
            Host=NewScriptedHost(product);
            field.SetValue(Service,Host.Owner);
            ViewOne=Host.CurrentView;
        }

        // The pair's command is bound on view V1 with agent A and admitted to the mount's Move slot.
        public void BindAdmitted()
        {
            Host.Overrides[ViewOne]=AgentA;
            Host.Owner.GetType().GetMethod("Bind").Invoke(Host.Owner,new object[]{Command});
            Host.SlotCommand=Command;
            Host.Owner.GetType().GetMethod("ObserveRun").Invoke(Host.Owner,new object[]{Command});
            Host.Calls.Clear();
        }

        public object Call(string name, params object[] arguments)
        {
            var method=ServiceType.GetMethod(name,Any);
            if(method==null) throw new InvalidOperationException(name+" does not exist in this build");
            return method.Invoke(Service,arguments);
        }
        public object Dismount(string trigger) { return Call("Dismount",Enum.Parse(Trigger,trigger)); }
        public string State { get { return Convert.ToString(ServiceType.GetProperty("State").GetValue(Service,null)); } }
        public int Retained { get { return (int)Host.Owner.GetType().GetProperty("ResidueCount").GetValue(Host.Owner,null); } }
        public bool Finished { get { return Host.Finished.Contains(Command); } }
        public bool AgentInstalled { get { return ReferenceEquals(Host.OverrideOn(ViewOne),AgentA); } }
        public int Count(string prefix) { var count=0; foreach(var call in Host.Calls) if(call.StartsWith(prefix)) count++; return count; }
        public int Warnings { get { var count=0; foreach(var call in Host.Calls) if(call.StartsWith("Report(") && call.EndsWith(",True)")) count++; return count; } }

        private static bool Flag(object result, string name) { return (bool)result.GetType().GetProperty(name).GetValue(result,null); }
        public static string Errors(object result) { var parts=new List<string>(); foreach(var error in (IEnumerable)result.GetType().GetProperty("Errors").GetValue(result,null)) parts.Add(Convert.ToString(error)); return string.Join(" | ",parts.ToArray()); }

        // The decision every lifecycle gate makes on a cleanup result. The corrected build's gates call these two
        // policy methods (pinned below); the reviewed build's gates carried the same expression inline.
        public bool WithoutResidue(object result)
        {
            if(Policy!=null) return (bool)Policy.GetMethod("IsWithoutResidue").Invoke(null,new object[]{result});
            return Flag(result,"Succeeded") && !Flag(result,"MovementAuthorityResidual") && !Flag(result,"PresentationResidual");
        }
        public bool Complete(object result)
        {
            if(Policy!=null) return (bool)Policy.GetMethod("IsComplete").Invoke(null,new object[]{result,ServiceType.GetProperty("State").GetValue(Service,null)});
            return WithoutResidue(result) && State=="Unmounted";
        }

        // The retry the composition root makes before its enabled gate.
        public void Recover() { Call("RecoverContinuousDriveCleanup"); }
        public bool Has(string name) { return ServiceType.GetMethod(name,Any)!=null; }
        // Whether the pair's owner retains cleanup; a build without that reading retains nothing for stick movement.
        public bool OwnerRetains { get { return Has("get_RetainsCleanupObligations") && (bool)Call("get_RetainsCleanupObligations"); } }

        // The gate MountedPatchController.Dispose asks before it removes the Harmony guards.
        public bool GuardsMayBeRemoved()
        {
            var controller=Product.GetType("KingmakerMountedCombat.Integration.MountedPatchController",true);
            var bridge=controller.GetNestedType("PatchBridge",Any).GetField("Service",Any);
            var guard=controller.GetNestedType("PatchMethods",Any).GetMethod("GuardNativeBoundary",Any);
            var boundary=Product.GetType("KingmakerMountedCombat.Diagnostics.NativeLifecycleBoundary",true);
            var before=bridge.GetValue(null);
            bridge.SetValue(null,Service);
            try { return (bool)guard.Invoke(null,new object[]{Enum.Parse(boundary,"ModDisable"),Enum.Parse(Trigger,"ModDisabled"),"patch-disposal"}); }
            finally { bridge.SetValue(null,before); }
        }
    }

    // ---- world boundaries (review of fe22563a) ----
    // The product's real MountedWorldService, built without the game as Test-WorldOwnershipContracts.ps1 builds it: its
    // real registry, real pair boundaries (GameMountedRelationshipService) and real admission discard. Actors are genuine
    // UnitEntityData objects made without the game; their stable ids are read through the registry's own id-reader seam.
    // A boundary's stick-movement owner runs on the scripted host, as in the lifecycle checks.
    private static readonly Dictionary<object,string> worldActorIds=new Dictionary<object,string>();
    public static string ReadWorldActorId<T>(T unit) { string id; return worldActorIds.TryGetValue(unit,out id)?id:null; }
    private static Assembly nativeAssembly;

    private sealed class WorldFixture
    {
        public Assembly Product; public object World, Registry; public Type UnitType, ServiceType, Trigger, RegistryType;
        public object Session=new object(); public string Campaign=Guid.NewGuid().ToString("N");
        private PropertyInfo gate; private object gateBefore;
        public readonly Dictionary<object,KmcScriptedDriveHost> Hosts=new Dictionary<object,KmcScriptedDriveHost>();
        public bool Seam;

        public WorldFixture(Assembly product)
        {
            Product=product;
            ServiceType=product.GetType("KingmakerMountedCombat.Integration.GameMountedRelationshipService",true);
            Trigger=product.GetType("KingmakerMountedCombat.Domain.CleanupTrigger",true);
            UnitType=nativeAssembly.GetType("Kingmaker.EntitySystem.Entities.UnitEntityData",true);
            gate=product.GetType("KingmakerMountedCombat.Integration.NativeMountedSaveFormat",true).GetProperty("ArchiveNativeSupportEnabled",Any);
            gateBefore=gate.GetValue(null,null); gate.SetValue(null,true,null);
            var settings=Activator.CreateInstance(product.GetType("KingmakerMountedCombat.Diagnostics.DiagnosticSettings",true));
            var logger=new KmcRecordingLogger(product.GetType("KingmakerMountedCombat.Logging.IModLogger",true)).GetTransparentProxy();
            foreach(var constructor in product.GetType("KingmakerMountedCombat.Integration.MountedWorldService",true).GetConstructors(Any))
                if(constructor.GetParameters().Length==2) World=constructor.Invoke(new[]{logger,settings});
            if(World==null) throw new InvalidOperationException("the world service could not be constructed");
            Registry=World.GetType().GetField("Registry",Any).GetValue(World);
            RegistryType=Registry.GetType();
            var reader=typeof(KmcContinuousDriveProbe).GetMethod("ReadWorldActorId").MakeGenericMethod(UnitType);
            RegistryType.GetField("actorId",Any).SetValue(Registry,Delegate.CreateDelegate(typeof(Func<,>).MakeGenericType(UnitType,typeof(string)),reader));
            Seam=ServiceType.GetMethod("ImportSavedOwnershipForSession",Any)!=null;
        }
        public void Close() { gate.SetValue(null,gateBefore,null); }

        public object Actor(string id) { var unit=FormatterServices.GetUninitializedObject(UnitType); worldActorIds[unit]=id; return unit; }
        // A pair boundary of the world. Its combat collaborators subscribe to its dismount (the Dismounting event and the
        // charge barrier) and need the running game, so they are detached from this boundary's dismount; every ownership,
        // discard and import decision below is the product's own. Charge, participation and command refusals are
        // exercised through the boundary's own readiness predicates (check D) and the drains are pinned from IL.
        public object Boundary()
        {
            var pair=World.GetType().GetMethod("CreateRestoredBoundary",Any).Invoke(World,null);
            var boundary=pair.GetType().GetField("Relationship",Any).GetValue(pair);
            ServiceType.GetProperty("ChargeCleanupBarrier",Any).SetValue(boundary,null,null);
            ServiceType.GetField("Dismounting",Any).SetValue(boundary,null);
            return boundary;
        }
        public object Call(object target,string name,params object[] arguments)
        {
            var method=target.GetType().GetMethod(name,Any);
            if(method==null) throw new InvalidOperationException(name+" does not exist in this build");
            return method.Invoke(target,arguments);
        }
        public object Owners { get { return RegistryType.GetProperty("Owners",Any).GetValue(Registry,null); } }
        public int OwnerCount { get { return ((Array)Owners).Length; } }
        public int EarlierCount { get { var p=RegistryType.GetProperty("EarlierWorldCleanupOwners",Any); return p==null?0:((Array)p.GetValue(Registry,null)).Length; } }
        public object PairOwner(object boundary) { return ServiceType.GetProperty("PairOwner",Any).GetValue(boundary,null); }
        public object OwnerOf(object actor) { return RegistryType.GetMethod("FindOwner",Any).Invoke(Registry,new[]{actor}); }
        public static object Read(object owner,string field) { return owner.GetType().GetField(field,Any).GetValue(owner); }
        public static string State(object owner) { return owner==null?"none":Convert.ToString(owner.GetType().GetProperty("State",Any).GetValue(owner,null)); }

        // The owner is registered as production registers it: ImportSavedOwnership's session seam, with the boundary's own
        // retained-obligation predicate and its declared world predicate. The reviewed build has no seam; there the
        // registry is asked directly with the reserve lambda's own expression, read from the boundary at each call:
        // its cleanup obligations, its retained commands and its retained participation.
        public object Register(object boundary,object rider,object mount,Guid id,long generation,object session,string campaign)
        {
            if(Seam) Call(boundary,"ImportSavedOwnershipForSession",session,campaign,rider,mount,id.ToString("N"),generation,true);
            else
            {
                var cleanup=ServiceType.GetProperty("RetainsCleanupObligations",Any).GetGetMethod(true);
                var commandsField=ServiceType.GetField("retainedCommands",Any); var participationField=ServiceType.GetField("retainedParticipation",Any);
                Func<bool> retained=()=>(bool)cleanup.Invoke(boundary,null) || ((Func<bool>)commandsField.GetValue(boundary))() ||
                    (bool)((Delegate)participationField.GetValue(boundary)).DynamicInvoke(rider,mount);
                var arguments=new object[]{session,campaign,rider,mount,generation,boundary,retained,
                    ServiceType.GetField("pairSaveReady",Any).GetValue(boundary),ServiceType.GetField("pairTeardownReady",Any).GetValue(boundary),null,null,6,(Guid?)id};
                if(!(bool)RegistryType.GetMethod("TryReserve",Any).Invoke(Registry,arguments)) throw new InvalidOperationException("registration refused: "+arguments[10]);
                ServiceType.GetProperty("PairOwner",Any).GetSetMethod(true).Invoke(boundary,new[]{arguments[9]});
            }
            var owner=PairOwner(boundary);
            RegistryType.GetMethod("Commit",Any).Invoke(Registry,new[]{owner});
            return owner;
        }

        // The boundary's stick-movement owner on the scripted host, with an admitted drive: command C on view V1, agent A.
        public KmcScriptedDriveHost Drive(object boundary,Assembly product,object command,object view,object agent)
        {
            var host=NewScriptedHost(product);
            ServiceType.GetField("continuousDrive",Any).SetValue(boundary,host.Owner);
            host.CurrentView=view; host.Overrides[view]=agent;
            host.Owner.GetType().GetMethod("Bind").Invoke(host.Owner,new object[]{command});
            host.SlotCommand=command;
            host.Owner.GetType().GetMethod("ObserveRun").Invoke(host.Owner,new object[]{command});
            host.Calls.Clear();
            Hosts[boundary]=host;
            return host;
        }
        public object Dismount(object boundary,string trigger) { return Call(boundary,"Dismount",Enum.Parse(Trigger,trigger)); }
        public bool WorldGuard(object boundary,string trigger) { return (bool)Call(boundary,"GuardWorldBoundary",Enum.Parse(Trigger,trigger)); }
        public bool StrictGuard(object boundary,string trigger) { return (bool)Call(boundary,"GuardBoundary",Enum.Parse(Trigger,trigger)); }
        public int Retained(object boundary) { var host=Hosts[boundary]; return (int)host.Owner.GetType().GetProperty("ResidueCount").GetValue(host.Owner,null); }
        public void Recover(object boundary) { Call(boundary,"RecoverContinuousDriveCleanup"); }
        // The discard each boundary calls in this build: area suspension the admission discard; load housekeeping the
        // world-replacement discard (the reviewed build's load housekeeping called the admission discard itself).
        public string Discard(bool worldReplacement)
        {
            var name=worldReplacement && World.GetType().GetMethod("DiscardAdmissionBindingsForWorldReplacement",Any)!=null
                ? "DiscardAdmissionBindingsForWorldReplacement" : "DiscardAdmissionBindings";
            try { Call(World,name); return null; }
            catch(Exception error) { return Innermost(error).Message; }
        }
        public bool Held(object boundary,object rider,object mount,Guid id) { return (bool)Call(boundary,"PairHeldByDetachedCleanup",rider,mount,id.ToString("N")); }
        public string TryRegister(object boundary,object rider,object mount,Guid id,long generation,object session,string campaign)
        {
            try { Register(boundary,rider,mount,id,generation,session,campaign); return null; }
            catch(Exception error) { return Innermost(error).Message; }
        }
        public void Settle() { Call(World,"ReleaseSettledAdmissions"); }
        public string AdmissionRefusal(object session,string campaign,object rider,object mount)
        {
            return (string)RegistryType.GetMethod("AdmissionRefusal",Any).Invoke(Registry,new object[]{session,campaign,rider,mount,6});
        }
    }

    // UnitCommand.Init called non-virtually: UnitMoveContiniously.Init itself needs Unity components.
    private static DynamicMethod baseInitCall;

    public static int Run(string managed, string productPath)
    {
        ResolveEventHandler resolver=(sender,args)=>{
            var leaf=new AssemblyName(args.Name).Name+".dll";
            foreach(var folder in new[]{managed,Path.Combine(managed,"UnityModManager")})
            {
                var dependency=Path.Combine(folder,leaf);
                if(File.Exists(dependency)) return Assembly.LoadFrom(dependency);
            }
            return null;
        };
        AppDomain.CurrentDomain.AssemblyResolve+=resolver;
        var native=Assembly.LoadFrom(Path.Combine(managed,"Assembly-CSharp.dll"));
        nativeAssembly=native;
        var product=Assembly.LoadFrom(productPath);
        Console.WriteLine("INFO product "+productPath+" MVID "+product.ManifestModule.ModuleVersionId);
        var modern=Assembly.LoadFrom(Path.Combine(managed,"UnityModManager/0Harmony.dll"));
        foreach(var method in modern.GetType("HarmonyLib.PatchProcessor",true).GetMethods(BindingFlags.Public|BindingFlags.Static))
            if(method.Name=="GetOriginalInstructions" && method.GetParameters().Length==2 && !method.GetParameters()[1].ParameterType.IsByRef) read=method;
        if(read==null) throw new InvalidOperationException("The installed Harmony instruction reader is unavailable.");
        var harmony12=Assembly.LoadFrom(Path.Combine(managed,"UnityModManager/0Harmony12.dll"));
        var instruction12=harmony12.GetType("Harmony12.CodeInstruction",true);

        var unit=native.GetType("Kingmaker.EntitySystem.Entities.UnitEntityData",true);
        var command=native.GetType("Kingmaker.UnitLogic.Commands.Base.UnitCommand",true);
        var commands=native.GetType("Kingmaker.UnitLogic.Commands.UnitCommands",true);
        var continuous=native.GetType("Kingmaker.UnitLogic.Commands.UnitMoveContiniously",true);
        var continuousAgent=native.GetType("Kingmaker.View.UnitMovementAgentContinious",true);
        var agentBase=native.GetType("Kingmaker.View.UnitMovementAgentBase",true);
        var ground=native.GetType("Kingmaker.Controllers.Clicks.Handlers.ClickGroundHandler",true);
        var gamepad=native.GetType("Kingmaker.UI._ConsoleUI.InputLayers.InGameLayer.InGameInputLayer",true);
        var view=native.GetType("Kingmaker.View.UnitEntityView",true);
        var mover=native.GetType("Kingmaker.Controllers.Units.UnitMoveController",true);
        var descriptor=native.GetType("Kingmaker.UnitLogic.UnitDescriptor",true);
        var state=native.GetType("Kingmaker.UnitLogic.UnitState",true);
        var groupMove=Token(ground,"MoveSelectedUnitsToPoint",0x060093DB);
        var stick=Token(gamepad,"UpdateMovement",0x06005D0C);
        var init=Token(continuous,"Init",0x060026F0);
        var overrideRead=Required(view,"get_AgentOverride",0);
        var overrideWrite=Required(view,"set_AgentOverride",1);

        // ---- NATIVE-IL ----
        Check("NATIVE-IL exact Kingmaker Assembly-CSharp MVID",
            ()=>native.ManifestModule.ModuleVersionId==new Guid("07fa1e4d-8618-41b3-9b8d-faa17d3b26f7"));
        Check("NATIVE-IL the stick layer (06005D0C) is the only native code that creates a UnitMoveContiniously, and the only caller of UnitCommands.MoveContiniously besides the speed reader",
            ()=>{
                var creators=Callers(native,Required(continuous,".ctor",0));
                var lookups=Callers(native,Required(commands,"get_MoveContiniously",0));
                Console.WriteLine("INFO creators of UnitMoveContiniously: "+Hex(creators)+"; readers of Commands.MoveContiniously: "+Hex(lookups));
                return Hex(creators)=="06005D0C" && Hex(lookups)=="06005D0C,0600830B";
            });
        Check("NATIVE-IL the stick layer reads the driven unit once (CurrentSelectUnit.Value stored into one local) and uses that one unit for the override reads, Commands.MoveContiniously, InterruptMove, Init, Commands.Run, Accelerate and Deaccelerate",
            ()=>{
                var body=Body(stick);
                var stores=new List<int>(); var loads=0;
                for(var i=0;i<body.Count;i++)
                {
                    if(body[i].Field==null || body[i].Field.Name!="unit" || body[i].Field.FieldType!=unit) continue;
                    if(body[i].Op==OpCodes.Stfld) stores.Add(i);
                    if(body[i].Op==OpCodes.Ldfld) loads++;
                }
                if(stores.Count!=1) throw new InvalidOperationException("stores of the driven unit: "+stores.Count);
                var source=body[stores[0]-1];
                var fromSelection=source.Call!=null && source.Call.Name=="get_Value" && body[stores[0]-2].Field!=null && body[stores[0]-2].Field.Name=="CurrentSelectUnit";
                var initCall=One(body,"UnitCommand","Init"); var run=One(body,"UnitCommands","Run"); var interrupt=One(body,"UnitCommands","InterruptMove");
                // Each of those calls takes its receiver or argument from the driven-unit local, a few instructions before.
                Func<int,bool> fromUnit=index=>{ for(var i=index-1;i>=0 && i>=index-4;i--) if(body[i].Op==OpCodes.Ldfld && body[i].Field!=null && body[i].Field.Name=="unit") return true; return false; };
                var lookups=Find(body,"UnitCommands","get_MoveContiniously");
                var reads=Find(body,"UnitEntityView","get_AgentOverride");
                foreach(var index in lookups) if(!fromUnit(index)) throw new InvalidOperationException("a MoveContiniously lookup is not on the driven unit");
                foreach(var index in reads) if(!fromUnit(index)) throw new InvalidOperationException("an override read is not on the driven unit");
                return fromSelection && loads>=16 && interrupt<initCall && initCall<run && fromUnit(interrupt) && fromUnit(initCall) && fromUnit(run) &&
                    lookups.Count==4 && reads.Count==6 && Count(body,"InGameInputLayer","MakeCompanionsFollow")==2 &&
                    Count(body,"UnitCommand","Accelerate")==2 && Count(body,"UnitCommand","Deaccelerate")==2;
            });
        Check("NATIVE-IL UnitMoveContiniously.Init (060026F0) installs a UnitMovementAgentContinious as its executor view's AgentOverride; OnTick (060026F1) ends without that override; OnEnded (060026F2) removes whatever override the executor's view holds",
            ()=>{
                var initBody=Body(init);
                var tick=Body(Token(continuous,"OnTick",0x060026F1));
                var ended=Body(Token(continuous,"OnEnded",0x060026F2));
                var setter=Body(overrideWrite);
                var endedWrite=One(ended,"UnitEntityView","set_AgentOverride");
                return First(initBody,"UnitCommand","Init")<First(initBody,"UnitEntityView","set_AgentOverride") &&
                    Count(initBody,"UnitEntityView","set_AgentOverride")==2 && Count(initBody,"GameObject","GetComponent")==1 && Count(initBody,"GameObject","AddComponent")==1 &&
                    Count(tick,"UnitEntityView","get_AgentOverride")==1 && Count(tick,null,"OnEnded")==2 &&
                    ended[endedWrite-1].Op==OpCodes.Ldnull && Count(ended,"UnitCommand","OnEnded")==1 && Count(ended,"UnitCommand","get_Executor")>=2 &&
                    Count(setter,"Object","Destroy")==1;
            });
        Check("NATIVE-IL UnitCommands.Run (060026B3) refuses a command whose executor is not the container's owner, before it touches any slot",
            ()=>{
                var run=Body(Token(commands,"Run",0x060026B3));
                var refusal=-1;
                for(var i=0;i<run.Count;i++) if(run[i].Op==OpCodes.Ldstr && (string)run[i].Operand=="Invalid command, can't run (unit: {0}, command: {1})") refusal=i;
                var firstSlotWrite=-1;
                for(var i=0;i<run.Count && firstSlotWrite<0;i++) if(run[i].Op==OpCodes.Stelem_Ref) firstSlotWrite=i;
                return refusal>=0 && First(run,"UnitCommand","get_Executor")<refusal && firstSlotWrite>refusal && Count(run,"UnitCommand","Init")==1;
            });
        Check("NATIVE-IL a unit is moved through UnitEntityView.MovementAgent (the override when one is installed), never through the stock agent directly",
            ()=>{
                var tick=Body(Token(mover,"Tick",0x06009183));
                return Count(tick,"UnitEntityView","get_MovementAgent")>=2 && Count(tick,"UnitEntityView","get_AgentASP")==0 &&
                    Count(tick,"UnitMovementAgentBase","TickMovement")==1;
            });
        Check("NATIVE-IL the native readers of UnitEntityView.AgentOverride are exactly UnitEntityView.MovementAgent itself, the stick layer, the continuous command, the ground-order handler, the speed reader and the stock agent's slow-down; its only writers are the continuous command's Init and OnEnded",
            ()=>{
                var readers=Callers(native,overrideRead);
                var writers=Callers(native,overrideWrite);
                Console.WriteLine("INFO readers of AgentOverride: "+Hex(readers)+"; writers: "+Hex(writers));
                return Hex(readers)=="06001825,060018B4,060026F0,060026F1,060026F2,06005D0C,0600830B,060093DB" && Hex(writers)=="060026F0,060026F2";
            });
        Check("NATIVE-IL the ground-order handler (060093DB) takes a non-null override on the current unit to mean that unit is being stick-driven: one read, on CurrentSelectUnit's view",
            ()=>{
                var body=Body(groupMove);
                var site=One(body,"UnitEntityView","get_AgentOverride");
                var current=-1;
                for(var i=0;i<site;i++) if(body[i].Field!=null && body[i].Field.Name=="CurrentSelectUnit") current=i;
                return current>=0 && Count(body,"UnitEntityData","get_View")>=1 && Count(body,"Game","get_IsControllerGamepad")>=2;
            });

        Check("NATIVE-IL the stick layer returns before it reads the driven unit while the party is in combat, and in turn-based combat a command is ticked only for the unit whose turn it is (so a continuous command the stick no longer feeds would keep its slot)",
            ()=>{
                var body=Body(stick);
                var combat=One(body,"Player","get_IsInCombat");
                var store=-1;
                for(var i=0;i<body.Count;i++) if(body[i].Op==OpCodes.Stfld && body[i].Field!=null && body[i].Field.Name=="unit" && body[i].Field.FieldType==unit) store=i;
                var actions=native.GetType("Kingmaker.Controllers.Units.UnitActionController",true);
                var turnBased=Body(Token(actions,"TickCommandTurnBased",0x0600911D));
                var tick=Body(Required(actions,"TickCommand",2));
                return combat<store && Count(turnBased,null,"IsCurrentUnit")>=1 && Count(turnBased,"UnitCommand","get_Executor")>=1 &&
                    First(tick,"UnitActionController","TickCommandTurnBased")<First(tick,"UnitCommand","TickApproaching") &&
                    First(tick,"UnitActionController","TickCommandTurnBased")<First(tick,"UnitCommand","Tick");
            });

        Check("NATIVE-IL UnitCommand.Interrupt (060027AC) stamps the command's result before it calls OnEnded and does nothing on a command already stamped or finished; UnitMoveContiniously keeps no view or agent of its own, so its OnEnded reads the executor's view at that moment",
            ()=>{
                var body=Body(Token(command,"Interrupt",0x060027AC));
                var stamp=-1; var readResult=-1;
                for(var i=0;i<body.Count;i++)
                {
                    var writes=Is(body[i],"UnitCommand","set_Result") || (body[i].Op==OpCodes.Stfld && body[i].Field!=null && body[i].Field.Name.Contains("Result"));
                    var reads=Is(body[i],"UnitCommand","get_Result") || (body[i].Op==OpCodes.Ldfld && body[i].Field!=null && body[i].Field.Name.Contains("Result"));
                    if(writes && stamp<0) stamp=i;
                    if(reads && readResult<0) readResult=i;
                }
                var ended=One(body,null,"OnEnded");
                var endedBody=Body(Token(continuous,"OnEnded",0x060026F2));
                return readResult>=0 && readResult<stamp && One(body,"UnitCommand","get_IsFinished")<stamp && stamp<ended &&
                    continuous.GetFields(All).Length==0 && Count(endedBody,"UnitCommand","get_Executor")>=2 && Count(endedBody,"UnitEntityData","get_View")>=2;
            });
        Check("NATIVE-IL only the internal Run (060026B3) puts a command into a slot: the public Run (060026B2) calls it, and queue promotion (RemoveFinishedAndUpdateQueue, 060026BD) calls it directly, never the public Run; enqueueing (AddToQueueInternal, 060026B8) calls neither",
            ()=>{
                var internalRun=Token(commands,"Run",0x060026B3);
                var publicRun=Token(commands,"Run",0x060026B2);
                var callersOfInternal=Callers(native,internalRun);
                var callersOfPublic=Callers(native,publicRun);
                Console.WriteLine("INFO callers of the internal Run 060026B3: "+Hex(callersOfInternal));
                var promote=Body(Token(commands,"RemoveFinishedAndUpdateQueue",0x060026BD));
                var promoted=0;
                foreach(var ins in promote) if(Is(ins,"UnitCommands","Run")){ if(ins.Call.MetadataToken!=0x060026B3) throw new InvalidOperationException("queue promotion calls another Run overload"); promoted++; }
                var enqueue=Body(Token(commands,"AddToQueueInternal",0x060026B8));
                var slotWrites=0;
                foreach(var ins in Body(internalRun)) if(ins.Op==OpCodes.Stelem_Ref) slotWrites++;
                var publicSlotWrites=0;
                foreach(var ins in Body(publicRun)) if(ins.Op==OpCodes.Stelem_Ref) publicSlotWrites++;
                // The public Run, the custom-handler path inside the internal Run's own special handling (060026B7), and queue promotion.
                return promoted==1 && Hex(callersOfInternal)=="060026B2,060026B7,060026BD" && !callersOfPublic.Contains(0x060026BD) &&
                    Count(enqueue,"UnitCommands","Run")==0 && slotWrites>=2 && publicSlotWrites==0;
            });
        Check("NATIVE-IL the native slot rules: InterruptMove (060026C2) ends only commands that have approached (IsMoveUnit, set only by TickApproaching 060027A6); a new Move or Standard order ends the commands of both slots (InterruptAndRemoveCommand 060026BF calls itself for the paired type); with an uninterruptible command present the new order is queued instead",
            ()=>{
                var interruptMove=Body(Token(commands,"InterruptMove",0x060026C2));
                var setters=Callers(native,Required(command,"set_IsMoveUnit",1));
                var paired=Body(Token(commands,"InterruptAndRemoveCommand",0x060026BF));
                var recursive=0;
                foreach(var ins in paired) if(ins.Call!=null && ins.Call.MetadataToken==0x060026BF) recursive++;
                var queue=Body(Required(commands,"TryAddToQueueInsteadOfRunImmediately",1));
                var interruptible=Body(Required(command,"get_IsInterruptible",0));
                Console.WriteLine("INFO setters of IsMoveUnit: "+Hex(setters));
                return One(interruptMove,"UnitCommand","get_IsMoveUnit")<One(interruptMove,"UnitCommand","Interrupt") && Hex(setters)=="060027A6" &&
                    recursive==2 && Count(paired,"UnitCommand","Interrupt")==1 &&
                    One(queue,"UnitCommands","HasUniterrapitble")<First(queue,"UnitCommands","AddToQueueInternal") &&
                    Count(interruptible,"UnitCommand","get_IsStarted")==1 && Count(interruptible,"UnitCommand","get_IsActed")==1;
            });

        // ---- NATIVE-EXECUTED ----
        object riderUnit=null, mountUnit=null, riderCommands=null, mountCommands=null, stickCommand=null;
        Check("NATIVE-EXECUTED genuine UnitCommands containers and a genuine UnitMoveContiniously are built without the game, and the command's executor is set to the mount by native UnitCommand.Init",
            ()=>{
                riderUnit=FormatterServices.GetUninitializedObject(unit);
                mountUnit=FormatterServices.GetUninitializedObject(unit);
                foreach(var one in new[]{riderUnit,mountUnit})
                {
                    var unitDescriptor=FormatterServices.GetUninitializedObject(descriptor);
                    var unitState=FormatterServices.GetUninitializedObject(state);
                    foreach(var field in descriptor.GetFields(Any)) if(field.FieldType==state) field.SetValue(unitDescriptor,unitState);
                    foreach(var field in unit.GetFields(Any)) if(field.FieldType==descriptor) field.SetValue(one,unitDescriptor);
                }
                riderCommands=Activator.CreateInstance(commands,new object[]{riderUnit});
                mountCommands=Activator.CreateInstance(commands,new object[]{mountUnit});
                stickCommand=Activator.CreateInstance(continuous);
                // The base method, called non-virtually: UnitMoveContiniously.Init itself needs Unity components.
                var baseInit=(MethodInfo)Required(command,"Init",1);
                baseInitCall=new DynamicMethod("KmcBaseInit",typeof(void),new[]{command,unit},command,true);
                var il=baseInitCall.GetILGenerator(); il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldarg_1); il.Emit(OpCodes.Call,baseInit); il.Emit(OpCodes.Ret);
                baseInitCall.Invoke(null,new object[]{stickCommand,mountUnit});
                return ReferenceEquals(command.GetProperty("Executor",Any).GetValue(stickCommand,null),mountUnit) &&
                    command.GetProperty("Type",Any).GetValue(stickCommand,null).ToString()=="Move" &&
                    (bool)commands.GetProperty("Empty").GetValue(riderCommands,null) && (bool)commands.GetProperty("Empty").GetValue(mountCommands,null);
            });
        Check("NATIVE-EXECUTED native UnitCommands.Run, run on the rider's container with the mount's command, takes the refusal branch (execution stops in the refusal's own log call, which needs the Unity player) and admits nothing: the Move slot stays empty and MoveContiniously stays null",
            ()=>{
                Exception thrown=null;
                try { commands.GetMethod("Run",new[]{command}).Invoke(riderCommands,new object[]{stickCommand}); }
                catch(TargetInvocationException error) { thrown=error.InnerException; }
                var trace=thrown==null?"<returned>":thrown.ToString();
                var inRefusalLog=thrown is TypeInitializationException && ((TypeInitializationException)thrown).TypeName=="UberLogger.Logger" &&
                    trace.Contains("UberDebug.LogError") && trace.Contains("UnitCommands.Run");
                Console.WriteLine("INFO rider container: "+(thrown==null?"Run returned":"stopped in "+(thrown is TypeInitializationException?"the type initializer of "+((TypeInitializationException)thrown).TypeName:thrown.GetType().Name)+(trace.Contains("UberDebug.LogError")?" under UberDebug.LogError":"")));
                return (thrown==null || inRefusalLog) && (bool)commands.GetProperty("Empty").GetValue(riderCommands,null) &&
                    commands.GetProperty("MoveContiniously").GetValue(riderCommands,null)==null &&
                    !(bool)commands.GetMethod("Contains",new[]{command}).Invoke(riderCommands,new object[]{stickCommand});
            });
        Check("NATIVE-EXECUTED the same command on the mount's own container passes the executor test: native Run goes past the refusal into code that needs the running game (it does not stop in the refusal's log call)",
            ()=>{
                Exception thrown=null;
                try { commands.GetMethod("Run",new[]{command}).Invoke(mountCommands,new object[]{stickCommand}); }
                catch(TargetInvocationException error) { thrown=error.InnerException; }
                var trace=thrown==null?"<returned>":thrown.ToString();
                Console.WriteLine("INFO mount container: "+(thrown==null?"Run returned":"stopped in "+(thrown is TypeInitializationException?"the type initializer of "+((TypeInitializationException)thrown).TypeName:thrown.GetType().Name)));
                return thrown==null || (!trace.Contains("UberDebug.LogError") && trace.Contains("UnitCommands.Run"));
            });

        Check("NATIVE-EXECUTED on a genuine UnitCommands container: enqueueing a command behind an occupied slot admits nothing; when the occupant is finished, native RemoveFinishedAndUpdateQueue takes the queued command and enters the internal Run directly (no public Run frame), where execution stops for lack of the running game",
            ()=>{
                var container=Activator.CreateInstance(commands,new object[]{riderUnit});
                var occupant=Activator.CreateInstance(continuous);
                var queued=Activator.CreateInstance(continuous);
                baseInitCall.Invoke(null,new object[]{occupant,riderUnit});
                baseInitCall.Invoke(null,new object[]{queued,riderUnit});
                ((Array)commands.GetField("m_Commands",Any).GetValue(container)).SetValue(occupant,3);
                commands.GetMethod("AddToQueue",new[]{command}).Invoke(container,new object[]{queued});
                var queue=(ICollection)commands.GetProperty("Queue").GetValue(container,null);
                var enqueuedOnly=queue.Count==1 && !(bool)commands.GetMethod("Contains",new[]{command}).Invoke(container,new object[]{queued}) &&
                    ReferenceEquals(commands.GetProperty("MoveContiniously").GetValue(container,null),occupant);
                command.GetField("<IsFinished>k__BackingField",Any).SetValue(occupant,true);
                Exception thrown=null;
                try { commands.GetMethod("RemoveFinishedAndUpdateQueue").Invoke(container,null); }
                catch(TargetInvocationException error) { thrown=error.InnerException; }
                var trace=thrown==null?"":thrown.ToString();
                Console.WriteLine("INFO queue promotion: "+(thrown==null?"returned":"stopped in "+(thrown is TypeInitializationException?"the type initializer of "+((TypeInitializationException)thrown).TypeName:thrown.GetType().Name))+
                    "; internal Run frame="+trace.Contains("Run(UnitCommand cmd, Boolean fromQueue, Boolean doNotClearQueue)")+"; public Run frame="+trace.Contains("Run(UnitCommand cmd)"));
                return enqueuedOnly && queue.Count==0 && commands.GetProperty("MoveContiniously").GetValue(container,null)!=occupant &&
                    (thrown==null || (trace.Contains("RemoveFinishedAndUpdateQueue") && trace.Contains("Run(UnitCommand cmd, Boolean fromQueue, Boolean doNotClearQueue)") && !trace.Contains("Run(UnitCommand cmd)")));
            });

        // ---- PRODUCT-EXECUTED ----
        var patchMethods=product.GetType("KingmakerMountedCombat.Integration.MountedPatchController+PatchMethods",true);
        var bridge=product.GetType("KingmakerMountedCombat.Integration.MountedPatchController+PatchBridge",true);
        var relationship=product.GetType("KingmakerMountedCombat.Integration.GameMountedRelationshipService",true);
        var runtime=product.GetType("KingmakerMountedCombat.Integration.KingmakerMountedPairRuntime",true);
        var riderAgent=product.GetType("KingmakerMountedCombat.Integration.RiderMovementAgent",true);
        var logger=product.GetType("KingmakerMountedCombat.Logging.IModLogger",true);
        var unitList=typeof(List<>).MakeGenericType(unit);

        Check("PRODUCT-EXECUTED the gamepad transpiler, run on the installed UpdateMovement, inserts the driven-unit hook before the one store of the driven unit, the formation hook before each MakeCompanionsFollow call and the follower hook after the SelectedUnits read, and changes nothing else, branch targets included",
            ()=>{
                var before=Body(stick);
                var transpiler=(MethodInfo)Required(patchMethods,"GamepadMovementTranspiler",1);
                var after=FromHarmony12(transpiler.Invoke(null,new object[]{ToHarmony12(instruction12,before)}));
                var driven=Required(patchMethods,"GamepadDrivenUnit",1);
                var formation=Required(patchMethods,"GamepadFormationUnit",1);
                var follower=Required(patchMethods,"GamepadFollowerOfferUnits",1);
                var inserted=new Dictionary<int,List<MethodBase>>();
                for(var i=0;i<before.Count;i++)
                {
                    if(before[i].Op==OpCodes.Stfld && before[i].Field!=null && before[i].Field.Name=="unit" && before[i].Field.FieldType==unit) inserted[i]=new List<MethodBase>{driven};
                    if(Is(before[i],"InGameInputLayer","MakeCompanionsFollow")) inserted[i]=new List<MethodBase>{formation};
                }
                inserted[One(before,"SelectionManagerBase","get_SelectedUnits")+1]=new List<MethodBase>{follower};
                RequireOnly(before,after,inserted,new Dictionary<int,MethodBase>());
                var targets=0;
                foreach(var ins in before) targets+=ins.Labels.Count;
                if(targets==0) throw new InvalidOperationException("the probe read no branch targets from the native method");
                return after.Count==before.Count+4 && inserted.Count==4 &&
                    ((MethodInfo)driven).ReturnType==unit && driven.GetParameters()[0].ParameterType==unit &&
                    ((MethodInfo)formation).ReturnType==unit && formation.GetParameters()[0].ParameterType==unit;
            });
        Check("PRODUCT-EXECUTED the ground-order transpiler, run on the installed MoveSelectedUnitsToPoint, replaces the one AgentOverride read with the product hook (beside the speed selection and the paired-input call of the earlier sprints), keeps that instruction's branch target, and changes nothing else",
            ()=>{
                var before=Body(groupMove);
                var transpiler=(MethodInfo)Required(patchMethods,"GroundOrderTranspiler",1);
                var earlier=FromHarmony12(((MethodInfo)Required(patchMethods,"GroundGroupMoveTranspiler",1)).Invoke(null,new object[]{ToHarmony12(instruction12,before)}));
                var after=FromHarmony12(transpiler.Invoke(null,new object[]{ToHarmony12(instruction12,before)}));
                var hook=Required(patchMethods,"GamepadDrivenOverride",1);
                var replaced=new Dictionary<int,MethodBase>();
                replaced[One(earlier,"UnitEntityView","get_AgentOverride")]=hook;
                RequireOnly(earlier,after,new Dictionary<int,List<MethodBase>>(),replaced);
                // The replaced read is itself a branch target in the installed method (the null-conditional chain).
                var site=One(earlier,"UnitEntityView","get_AgentOverride");
                if(earlier[site].Labels.Count==0) throw new InvalidOperationException("the override read carries no branch target in this build");
                return after.Count==earlier.Count && Count(after,"UnitEntityView","get_AgentOverride")==0 &&
                    ((MethodInfo)hook).ReturnType==agentBase && hook.GetParameters()[0].ParameterType==view;
            });
        Check("PRODUCT-EXECUTED a continuous-drive transpiler given a method that does not hold its recorded site fails instead of patching",
            ()=>{
                var transpilers=product.GetType("KingmakerMountedCombat.Integration.ContinuousDriveTranspilers",true);
                var refused=0;
                try
                {
                    var result=Required(transpilers,"DrivenUnit",3).Invoke(null,new object[]{ToHarmony12(instruction12,Body(groupMove)),
                        Required(patchMethods,"GamepadDrivenUnit",1),Required(patchMethods,"GamepadFormationUnit",1)});
                    foreach(var ignored in (IEnumerable)result) { }
                }
                catch(TargetInvocationException error) { if(error.InnerException is InvalidOperationException) refused++; }
                try
                {
                    // The stick layer reads the override six times: not the one read this transpiler is for.
                    var result=Required(transpilers,"DrivenOverrideRead",2).Invoke(null,new object[]{ToHarmony12(instruction12,Body(stick)),Required(patchMethods,"GamepadDrivenOverride",1)});
                    foreach(var ignored in (IEnumerable)result) { }
                }
                catch(TargetInvocationException error) { if(error.InnerException is InvalidOperationException) refused++; }
                return refused==2;
            });
        Check("PRODUCT-EXECUTED the compiled hooks, with no mounted pair, give native its own answer: the selected unit drives and anchors its own formation, a missing view has no override, and the Init prefix lets native Init run for the same executor",
            ()=>{
                SetStatic(bridge,"Service",null); SetStatic(bridge,"Logger",null);
                var one=FormatterServices.GetUninitializedObject(unit);
                var driven=Required(patchMethods,"GamepadDrivenUnit",1); var formation=Required(patchMethods,"GamepadFormationUnit",1);
                var overrideHook=Required(patchMethods,"GamepadDrivenOverride",1);
                var prefix=(MethodInfo)Required(patchMethods,"ContinuousMovePrefix",1);
                var postfix=(MethodInfo)Required(patchMethods,"ContinuousMovePostfix",1);
                var arguments=new object[]{one};
                var runsNative=(bool)prefix.Invoke(null,arguments);
                postfix.Invoke(null,new object[]{null});
                postfix.Invoke(null,new object[]{stickCommand});
                return ReferenceEquals(driven.Invoke(null,new object[]{one}),one) && driven.Invoke(null,new object[]{null})==null &&
                    ReferenceEquals(formation.Invoke(null,new object[]{one}),one) && formation.Invoke(null,new object[]{null})==null &&
                    overrideHook.Invoke(null,new object[]{null})==null && runsNative && ReferenceEquals(arguments[0],one);
            });
        Check("PRODUCT-EXECUTED the compiled decisions: the mount is driven only for a mounted pair's rider that has a mount; the formation is anchored on the rider only while the stick drives the mount of the selected rider; a mount override is admissible only as the owned live drive",
            ()=>{
                var policy=product.GetType("KingmakerMountedCombat.Domain.MountedContinuousDrivePolicy",true);
                Func<string,bool[],bool> of=(name,flags)=>{ var args=new object[flags.Length]; for(var i=0;i<flags.Length;i++) args[i]=flags[i]; return (bool)policy.GetMethod(name).Invoke(null,args); };
                return of("DrivesPhysicalMover",new[]{true,true,true}) && !of("DrivesPhysicalMover",new[]{false,true,true}) &&
                    !of("DrivesPhysicalMover",new[]{true,false,true}) && !of("DrivesPhysicalMover",new[]{true,true,false}) &&
                    of("FormationAnchorIsRider",new[]{true,true,true}) && !of("FormationAnchorIsRider",new[]{false,true,true}) &&
                    !of("FormationAnchorIsRider",new[]{true,false,true}) && !of("FormationAnchorIsRider",new[]{true,true,false}) &&
                    of("MountOverrideAdmissible",new[]{false,false}) && of("MountOverrideAdmissible",new[]{true,true}) && !of("MountOverrideAdmissible",new[]{true,false});
            });
        Check("PRODUCT-EXECUTED the compiled ownership record: the exact agent of the exact command is owned while that command runs in the mount's Move slot for the same pair generation, within the binding frame before Run is decided, and never otherwise",
            ()=>{
                var watchType=product.GetType("KingmakerMountedCombat.Domain.MountedContinuousDriveWatch",true);
                var watch=Activator.CreateInstance(watchType);
                object boundCommand=new object(), boundAgent=new object(), boundView=new object();
                Func<object,long,bool,bool,bool,int,bool> owns=(agent,generation,mounted,finished,inSlot,frame)=>
                    (bool)watchType.GetMethod("Owns").Invoke(watch,new object[]{agent,generation,mounted,finished,inSlot,frame});
                Func<long,bool,bool,bool,bool,bool,int,string> settle=(generation,mounted,finished,inSlot,attempted,gated,frame)=>
                    watchType.GetMethod("Settle").Invoke(watch,new object[]{generation,mounted,finished,inSlot,attempted,gated,frame}).ToString();
                Action<int> bind=frame=>watchType.GetMethod("Bind").Invoke(watch,new object[]{boundCommand,boundAgent,boundView,7L,frame});
                bind(100);
                var awaiting=owns(boundAgent,7L,true,false,false,100) && settle(7L,true,false,false,false,false,100)=="Keep" && !owns(boundAgent,7L,true,false,false,101);
                var orphanNextFrame=settle(7L,true,false,false,false,false,101)=="Orphaned";
                bind(110);
                var refused=settle(7L,true,false,false,true,false,110)=="Orphaned" && !owns(boundAgent,7L,true,false,false,110);
                bind(120);
                var admitted=settle(7L,true,false,true,true,false,120)=="Keep" && owns(boundAgent,7L,true,false,true,500);
                var strict=!owns(new object(),7L,true,false,true,500) && !owns(null,7L,true,false,true,500) && !owns(boundAgent,8L,true,false,true,500) &&
                    !owns(boundAgent,7L,false,false,true,500) && !owns(boundAgent,7L,true,true,true,500) && !owns(boundAgent,7L,true,false,false,500);
                var settlements=settle(8L,true,false,true,false,false,500)=="Unowned" && settle(7L,false,false,true,false,false,500)=="Unowned" &&
                    settle(7L,true,true,false,false,false,500)=="Ended" && settle(7L,true,false,false,false,false,500)=="Orphaned" &&
                    settle(7L,true,false,true,false,true,500)=="InputGated" && settle(7L,true,false,false,false,true,500)=="Orphaned";
                watchType.GetMethod("Clear").Invoke(watch,null);
                return awaiting && orphanNextFrame && refused && admitted && strict && settlements && !owns(boundAgent,7L,true,false,true,500) &&
                    settle(7L,true,false,true,false,false,500)=="None";
            });
        Check("PRODUCT-EXECUTED the compiled retirement decisions: an unfinished command is ended whether or not its agent is installed (without an end event when it never held its slot); the captured agent is cleared only while installed; a pair-bound command's native end may clear only the bound agent on the bound view; a terminal command with its agent still installed is not an established retirement",
            ()=>{
                var policy=product.GetType("KingmakerMountedCombat.Domain.MountedContinuousDrivePolicy",true);
                var settlement=product.GetType("KingmakerMountedCombat.Domain.MountedContinuousDriveSettlement",true);
                var plan=policy.GetMethod("PlanRetirement");
                Func<string,bool,bool,string> of=(name,finished,installed)=>{
                    var result=plan.Invoke(null,new object[]{Enum.Parse(settlement,name),finished,installed});
                    return ((bool)result.GetType().GetProperty("InterruptCommand").GetValue(result,null)?"I":"-")+
                        ((bool)result.GetType().GetProperty("RaiseEndEvent").GetValue(result,null)?"E":"-")+
                        ((bool)result.GetType().GetProperty("ClearCapturedAgent").GetValue(result,null)?"C":"-");
                };
                Func<string,bool[],bool> decide=(name,flags)=>{ var args=new object[flags.Length]; for(var i=0;i<flags.Length;i++) args[i]=flags[i]; return (bool)policy.GetMethod(name).Invoke(null,args); };
                return of("Keep",false,true)=="---" && of("None",false,true)=="---" &&
                    of("Orphaned",false,true)=="I-C" && of("Orphaned",false,false)=="I--" &&
                    of("Unowned",false,true)=="IEC" && of("Unowned",false,false)=="IE-" && of("InputGated",false,true)=="IEC" && of("Replaced",false,false)=="IE-" &&
                    of("Ended",true,true)=="--C" && of("Ended",true,false)=="---" && of("Unowned",true,true)=="--C" && of("Orphaned",true,false)=="---" &&
                    decide("NativeEndMayClearOverride",new[]{false,false,false}) && decide("NativeEndMayClearOverride",new[]{true,true,true}) &&
                    !decide("NativeEndMayClearOverride",new[]{true,false,true}) && !decide("NativeEndMayClearOverride",new[]{true,true,false}) &&
                    decide("RetirementEstablished",new[]{true,true,false}) && decide("RetirementEstablished",new[]{false,false,false}) &&
                    !decide("RetirementEstablished",new[]{true,true,true}) && !decide("RetirementEstablished",new[]{false,true,false}) &&
                    decide("RiderOrderReplacesDrive",new[]{true,true,true}) && !decide("RiderOrderReplacesDrive",new[]{false,true,true}) &&
                    !decide("RiderOrderReplacesDrive",new[]{true,false,true}) && !decide("RiderOrderReplacesDrive",new[]{true,true,false});
            });
        Check("PRODUCT-EXECUTED the compiled owner, on a scripted host: a command on the mount with its continuous agent is bound, admitted (the rider's Hold released), owned by the invariant's question, and ended natively when the party enters combat",
            ()=>{
                var scripted=NewScriptedHost(product);
                var ownerType=scripted.Owner.GetType();
                object pushed=new object(), installedAgent=new object();
                scripted.Overrides[scripted.CurrentView]=installedAgent;
                ownerType.GetMethod("Bind").Invoke(scripted.Owner,new object[]{pushed});
                var boundNow=(bool)ownerType.GetProperty("IsBound").GetValue(scripted.Owner,null);
                var ownedBeforeRun=(bool)ownerType.GetMethod("OwnsMountOverride").Invoke(scripted.Owner,new object[]{installedAgent});
                scripted.SlotCommand=pushed;
                ownerType.GetMethod("ObserveRun").Invoke(scripted.Owner,new object[]{pushed});
                scripted.Frame=50;
                var ownedLater=(bool)ownerType.GetMethod("OwnsMountOverride").Invoke(scripted.Owner,new object[]{installedAgent});
                var foreignOwned=(bool)ownerType.GetMethod("OwnsMountOverride").Invoke(scripted.Owner,new object[]{new object()});
                var settledKept=ownerType.GetMethod("Settle").Invoke(scripted.Owner,new object[]{false}).ToString();
                var logBeforeCombat=scripted.Log;
                scripted.Gated=true;
                var settledInCombat=ownerType.GetMethod("Settle").Invoke(scripted.Owner,new object[]{false}).ToString();
                Console.WriteLine("INFO scripted host, admitted drive: ["+logBeforeCombat+"] then in combat ["+scripted.Log+"]");
                return boundNow && ownedBeforeRun && ownedLater && !foreignOwned && settledKept=="Keep" && logBeforeCombat=="ReleaseRiderHold" && settledInCombat=="InputGated" &&
                    scripted.Log=="ReleaseRiderHold Interrupt(True) Report(InputGated,False)" && !(bool)ownerType.GetProperty("IsBound").GetValue(scripted.Owner,null) &&
                    scripted.OverrideOn(scripted.CurrentView)==null;
            });
        Check("PRODUCT-EXECUTED the compiled owner, on a scripted host: a command native Run did not admit is ended without an end event, its own native end removes its agent, and repeated refusals are reported once; another unit's command and a non-continuous agent bind nothing",
            ()=>{
                var scripted=NewScriptedHost(product);
                var ownerType=scripted.Owner.GetType();
                for(var push=0;push<3;push++)
                {
                    object pushed=new object();
                    scripted.Frame++; scripted.Overrides[scripted.CurrentView]=new object();
                    ownerType.GetMethod("Bind").Invoke(scripted.Owner,new object[]{pushed});
                    ownerType.GetMethod("ObserveRun").Invoke(scripted.Owner,new object[]{pushed});
                    if(scripted.OverrideOn(scripted.CurrentView)!=null) throw new InvalidOperationException("a refused command left its agent installed");
                }
                var refusals=scripted.Log;
                Console.WriteLine("INFO scripted host, three refused admissions: ["+refusals+"]");
                var orphanCount=(long)ownerType.GetProperty("OrphanCount").GetValue(scripted.Owner,null);
                scripted.Calls.Clear();
                scripted.Executor=new object(); scripted.Overrides[scripted.CurrentView]=new object();
                ownerType.GetMethod("Bind").Invoke(scripted.Owner,new object[]{new object()});
                scripted.Executor=scripted.Mount; scripted.AgentIsContinuous=false;
                ownerType.GetMethod("Bind").Invoke(scripted.Owner,new object[]{new object()});
                return refusals=="Interrupt(False) Report(Orphaned,True) Interrupt(False) Interrupt(False)" && orphanCount==3 &&
                    !(bool)ownerType.GetProperty("IsBound").GetValue(scripted.Owner,null) && scripted.Log=="";
            });
        Check("PRODUCT-EXECUTED R1 reproduction A on the compiled owner: the command is bound to view V1 with agent A, the mount then receives view V2 carrying an unrelated override B, and V1 and A stay alive; the dismount release and the rider-order release each retire the command and A, and B survives the release and every later cleanup and termination",
            ()=>{
                foreach(var byDismount in new[]{true,false})
                {
                    var scripted=NewScriptedHost(product);
                    var ownerType=scripted.Owner.GetType();
                    object pushed=new object(), agentA=new object(), viewOne=scripted.CurrentView, viewTwo=new object(), overrideB=new object();
                    scripted.Overrides[viewOne]=agentA;
                    ownerType.GetMethod("Bind").Invoke(scripted.Owner,new object[]{pushed});
                    scripted.SlotCommand=pushed;
                    ownerType.GetMethod("ObserveRun").Invoke(scripted.Owner,new object[]{pushed});
                    scripted.Calls.Clear();
                    scripted.CurrentView=viewTwo; scripted.Overrides[viewTwo]=overrideB;
                    if(byDismount) ReleaseOwner(scripted.Owner,"dismount");
                    else ownerType.GetMethod("ObserveRiderOrder").Invoke(scripted.Owner,new object[]{true,true});
                    var how=byDismount?"dismount":"rider order";
                    Console.WriteLine("INFO reproduction A ("+how+"): ["+scripted.Log+"]; B survives="+ReferenceEquals(scripted.OverrideOn(viewTwo),overrideB)+"; A removed="+(scripted.OverrideOn(viewOne)==null)+"; command terminal="+scripted.Finished.Contains(pushed));
                    if(!ReferenceEquals(scripted.OverrideOn(viewTwo),overrideB)) throw new InvalidOperationException("the unrelated override B on the mount's current view was cleared by the "+how+" release");
                    if(scripted.OverrideOn(viewOne)!=null || !scripted.Finished.Contains(pushed)) throw new InvalidOperationException("the owned command or its agent was not retired by the "+how+" release");
                    // Later cleanup, settlement and a repeated native termination.
                    ownerType.GetMethod("Settle").Invoke(scripted.Owner,new object[]{false});
                    ReleaseOwner(scripted.Owner,"again");
                    scripted.NativeEnd(pushed);
                    if(!ReferenceEquals(scripted.OverrideOn(viewTwo),overrideB)) throw new InvalidOperationException("a later cleanup or termination cleared the unrelated override B");
                    if((int)ownerType.GetProperty("ResidueCount").GetValue(scripted.Owner,null)!=0) return false;
                    if(scripted.Log!=(byDismount?"Interrupt(True) ClearOverride Report(Unowned,False)":"Interrupt(True) ClearOverride Report(Replaced,False)")) return false;
                }
                return true;
            });
        Check("PRODUCT-EXECUTED R1 reproduction B on the compiled owner: an admitted drive's agent is replaced by a foreign override on the same view; relationship cleanup retires the owned command and leaves the foreign override, and the original command's later native termination cannot remove it either",
            ()=>{
                var scripted=NewScriptedHost(product);
                var ownerType=scripted.Owner.GetType();
                object pushed=new object(), agentA=new object(), foreignOverride=new object();
                scripted.Overrides[scripted.CurrentView]=agentA;
                ownerType.GetMethod("Bind").Invoke(scripted.Owner,new object[]{pushed});
                scripted.SlotCommand=pushed;
                ownerType.GetMethod("ObserveRun").Invoke(scripted.Owner,new object[]{pushed});
                scripted.Calls.Clear();
                scripted.Overrides[scripted.CurrentView]=foreignOverride;
                var ownsForeign=(bool)ownerType.GetMethod("OwnsMountOverride").Invoke(scripted.Owner,new object[]{foreignOverride});
                ReleaseOwner(scripted.Owner,"dismount");
                var afterCleanup=ReferenceEquals(scripted.OverrideOn(scripted.CurrentView),foreignOverride);
                var retiredByCleanup=scripted.Finished.Contains(pushed);
                // The original command's later life: native ticking ends a continuous command whose own agent is gone.
                scripted.NativeEnd(pushed);
                ownerType.GetMethod("Settle").Invoke(scripted.Owner,new object[]{false});
                var afterTermination=ReferenceEquals(scripted.OverrideOn(scripted.CurrentView),foreignOverride);
                Console.WriteLine("INFO reproduction B: ["+scripted.Log+"]; foreign override after cleanup="+afterCleanup+", after the command's later termination="+afterTermination+"; command retired by cleanup="+retiredByCleanup);
                if(!afterCleanup) throw new InvalidOperationException("relationship cleanup cleared the foreign override");
                if(!afterTermination) throw new InvalidOperationException("the abandoned owned command later ended natively and cleared the foreign override");
                return !ownsForeign && retiredByCleanup && scripted.Log=="Interrupt(True) Report(Unowned,False)" &&
                    !(bool)ownerType.GetProperty("IsBound").GetValue(scripted.Owner,null);
            });
        Check("PRODUCT-EXECUTED R1 retries on the compiled owner: the command and the captured agent are retired independently; a failed interrupt, a native end that fails after the command became terminal, and a failed override removal are each contained; a record is kept exactly while its command is unfinished and held or its agent is still installed, and the next settlement completes it without a second interrupt of a terminal command",
            ()=>{
                // failure, residue after the failing release, agent still installed then, native calls in order
                var cases=new[]{
                    new object[]{"interrupt",1,false,"Interrupt(True) ClearOverride Report(Unowned,True) Interrupt(True)"},
                    new object[]{"end-callback",0,false,"Interrupt(True) ClearOverride Report(Unowned,True) Report(Unowned,False)"},
                    new object[]{"clear",1,true,"Interrupt(True) ClearOverride Report(Unowned,True) ClearOverride"},
                    new object[]{"end-callback+clear",1,true,"Interrupt(True) ClearOverride Report(Unowned,True) ClearOverride"}};
                foreach(var row in cases)
                {
                    var failure=(string)row[0];
                    var scripted=NewScriptedHost(product);
                    var ownerType=scripted.Owner.GetType();
                    object pushed=new object(), agentA=new object(), viewOne=scripted.CurrentView;
                    scripted.Overrides[viewOne]=agentA;
                    ownerType.GetMethod("Bind").Invoke(scripted.Owner,new object[]{pushed});
                    scripted.SlotCommand=pushed;
                    ownerType.GetMethod("ObserveRun").Invoke(scripted.Owner,new object[]{pushed});
                    scripted.Calls.Clear();
                    if(failure=="interrupt") scripted.FailInterrupts=1;
                    if(failure.StartsWith("end-callback")) scripted.EndCallbackFails=true;
                    if(failure.EndsWith("clear")) scripted.FailClears=1;
                    // A replaced view: the command's native end cannot remove the agent, so only the owner's own removal can.
                    if(failure=="clear") scripted.CurrentView=new object();
                    ReleaseOwner(scripted.Owner,"dismount");
                    var residueAfterFailure=(int)ownerType.GetProperty("ResidueCount").GetValue(scripted.Owner,null);
                    var agentStillInstalled=ReferenceEquals(scripted.OverrideOn(viewOne),agentA);
                    var ownedWhileResidue=(bool)ownerType.GetMethod("OwnsMountOverride").Invoke(scripted.Owner,new object[]{agentA});
                    var terminalAfterFailure=scripted.Finished.Contains(pushed);
                    var failedSteps=(long)ownerType.GetProperty("RetirementFailureCount").GetValue(scripted.Owner,null);
                    ownerType.GetMethod("Settle").Invoke(scripted.Owner,new object[]{false});
                    var residueAfterRetry=(int)ownerType.GetProperty("ResidueCount").GetValue(scripted.Owner,null);
                    Console.WriteLine("INFO retry after a failed "+failure+": ["+scripted.Log+"]; after the failure: residue="+residueAfterFailure+", command terminal="+terminalAfterFailure+", agent installed="+agentStillInstalled);
                    if(residueAfterFailure!=(int)row[1] || agentStillInstalled!=(bool)row[2] || ownedWhileResidue || failedSteps!=1)
                        throw new InvalidOperationException(failure+": the record was not kept exactly while something of it remained");
                    if(residueAfterRetry!=0 || scripted.OverrideOn(viewOne)!=null || !scripted.Finished.Contains(pushed)) throw new InvalidOperationException(failure+": the retry did not establish the retirement");
                    if(scripted.Log!=(string)row[3]) throw new InvalidOperationException(failure+": unexpected native calls ["+scripted.Log+"]");
                }
                return true;
            });
        Check("PRODUCT-EXECUTED the native-end transpiler, run on the installed UnitMoveContiniously.OnEnded, routes its one AgentOverride write through the product guard with the ending command, keeps every branch target and changes nothing else; the guard gives a command no pair bound the native rule",
            ()=>{
                var before=Body(Token(continuous,"OnEnded",0x060026F2));
                var transpiler=(MethodInfo)Required(patchMethods,"ContinuousMoveEndTranspiler",1);
                var after=FromHarmony12(transpiler.Invoke(null,new object[]{ToHarmony12(instruction12,before)}));
                var hook=(MethodInfo)Required(patchMethods,"EndedContinuousCommandOverride",3);
                var site=One(before,"UnitEntityView","set_AgentOverride");
                if(after.Count!=before.Count+1) throw new InvalidOperationException("instruction count "+before.Count+" -> "+after.Count);
                for(var i=0;i<before.Count;i++)
                {
                    if(i==site) continue;
                    if(!Same(after[i<site?i:i+1],before[i])) throw new InvalidOperationException("unexpected change at native instruction "+i);
                }
                var parameters=hook.GetParameters();
                var guard=relationship.GetMethod("ContinuousDriveEndMayClearOverride",Any);
                var unbound=Activator.CreateInstance(continuous);
                var someView=FormatterServices.GetUninitializedObject(view);
                return after[site].Op==OpCodes.Ldarg_0 && after[site].Labels.Count==0 && before[site-1].Op==OpCodes.Ldnull &&
                    after[site+1].Op==OpCodes.Call && ReferenceEquals(after[site+1].Call,hook) && SameLabels(after[site+1],before[site]) &&
                    hook.ReturnType==typeof(void) && parameters[0].ParameterType==view && parameters[1].ParameterType==agentBase && parameters[2].ParameterType==continuous &&
                    Count(Body(Token(continuous,"OnEnded",0x060026F2)),"UnitCommand","OnEnded")==1 && Count(after,"UnitCommand","OnEnded")==1 &&
                    (bool)guard.Invoke(null,new object[]{unbound,someView}) && (bool)guard.Invoke(null,new object[]{null,someView}) && (bool)guard.Invoke(null,new object[]{unbound,null});
            });
        Check("PRODUCT-EXECUTED an operational failure inside a hook, with a logger that throws: the gamepad follower hook still returns the selection itself, the speed hook still yields the native selector's values, and the logger is called once",
            ()=>{
                // A relationship service with no state: resolving a unit's owner through it fails inside production code.
                SetStatic(bridge,"Service",FormatterServices.GetUninitializedObject(relationship));
                SetStatic(bridge,"Logger",new KmcThrowingLogger(logger).GetTransparentProxy());
                ResetFaultReport(patchMethods);
                KmcThrowingLogger.Calls=0;
                try
                {
                    var selected=(IList)Activator.CreateInstance(unitList);
                    selected.Add(FormatterServices.GetUninitializedObject(unit)); selected.Add(FormatterServices.GetUninitializedObject(unit));
                    var answered=Required(patchMethods,"GamepadFollowerOfferUnits",1).Invoke(null,new object[]{selected});
                    var afterFirst=KmcThrowingLogger.Calls;
                    var selector=Delegate.CreateDelegate(typeof(Func<,>).MakeGenericType(unit,typeof(float)),typeof(KmcContinuousDriveProbe).GetMethod("SpeedOne").MakeGenericMethod(unit));
                    var values=new List<float>();
                    foreach(var value in (IEnumerable)Required(patchMethods,"GroupPaceSpeedInputs",2).Invoke(null,new object[]{selected,selector})) values.Add((float)value);
                    Console.WriteLine("INFO operational failure reached the logger "+afterFirst+" time(s) on the first hook, "+KmcThrowingLogger.Calls+" in all");
                    return ReferenceEquals(answered,selected) && selected.Count==2 && afterFirst==1 && KmcThrowingLogger.Calls==1 &&
                        values.Count==2 && values[0]==4.5f && values[1]==4.5f;
                }
                finally { SetStatic(bridge,"Service",null); SetStatic(bridge,"Logger",null); ResetFaultReport(patchMethods); }
            });
        Check("PRODUCT-EXECUTED the same operational failure with a throwing logger leaves the native driven unit: the driven-unit hook and the formation hook each answer with the unit native gave them",
            ()=>{
                SetStatic(bridge,"Service",FormatterServices.GetUninitializedObject(relationship));
                SetStatic(bridge,"Logger",new KmcThrowingLogger(logger).GetTransparentProxy());
                ResetFaultReport(patchMethods);
                KmcThrowingLogger.Calls=0;
                try
                {
                    var one=FormatterServices.GetUninitializedObject(unit);
                    var driven=Required(patchMethods,"GamepadDrivenUnit",1).Invoke(null,new object[]{one});
                    var reached=KmcThrowingLogger.Calls;
                    ResetFaultReport(patchMethods);
                    var anchor=Required(patchMethods,"GamepadFormationUnit",1).Invoke(null,new object[]{one});
                    Console.WriteLine("INFO operational failure reached the logger "+reached+" time(s) on the driven-unit hook, "+KmcThrowingLogger.Calls+" in all");
                    return ReferenceEquals(driven,one) && ReferenceEquals(anchor,one) && reached==1 && KmcThrowingLogger.Calls==2;
                }
                finally { SetStatic(bridge,"Service",null); SetStatic(bridge,"Logger",null); ResetFaultReport(patchMethods); }
            });

        // ---- PRODUCT-IL ----
        Check("PRODUCT-IL the mounted invariant asks the policy whether the mount's override is admissible, with the pair's owner answer, before it reports the mount-agent fault",
            ()=>{
                var validate=Body(Required(runtime,"ValidateMountedInvariants",1));
                var admissible=One(validate,"MountedContinuousDrivePolicy","MountOverrideAdmissible");
                var asked=Find(validate,"KingmakerMountedPairRuntime","get_MountOverrideOwner");
                var answered=Find(validate,null,"Invoke");
                return asked.Count>=1 && asked[0]<admissible && answered.Count>=1 && answered[answered.Count-1]<admissible &&
                    HasString(validate,"The authoritative mount movement agent changed.") && Count(validate,"UnitEntityView","get_AgentASP")>=2;
            });
        Check("PRODUCT-IL the game adapter answers from the pair's exact units: the pair generation field, the mount's Move slot, a unit's own current view, the command's executor's slots and queue, a UnitMovementAgentContinious test, the party's combat state; its fact answers write nothing",
            ()=>{
                var host=product.GetType("KingmakerMountedCombat.Integration.GameMountedRelationshipService+ContinuousDriveHost",true);
                var generation=false;
                foreach(var ins in Body(Required(host,"get_PairGeneration",0))) if(ins.Field!=null && ins.Field.Name=="mountedPairGeneration") generation=true;
                var isContinuous=false;
                foreach(var ins in Body(Required(host,"IsContinuousAgent",1))) if(ins.Op==OpCodes.Isinst && (Type)ins.Operand==continuousAgent) isContinuous=true;
                var slot=Body(Required(host,"InMountMoveSlot",1));
                foreach(var name in new[]{"get_PairMounted","get_PairGeneration","get_Frame","get_StickInputGated","get_Mount","ViewOf","ExecutorOf","IsFinished","InMountMoveSlot","IsHeld","OverrideOf","IsContinuousAgent"})
                    foreach(var ins in Body(Required(host,name,-1)))
                        if(Is(ins,null,"set_AgentOverride") || Is(ins,null,"Interrupt") || Is(ins,null,"Run") || Is(ins,null,"InterruptMove") || Is(ins,null,"set_HoldState") || ins.Op==OpCodes.Stfld)
                            throw new InvalidOperationException(name+" writes or calls "+(ins.Call!=null?ins.Call.Name:ins.Field.Name));
                return generation && isContinuous && Count(slot,"UnitCommands","GetCommand")==1 && Count(slot,null,"get_Mount")==1 &&
                    Count(Body(Required(host,"get_StickInputGated",0)),"Player","get_IsInCombat")==1 &&
                    Count(Body(Required(host,"ViewOf",1)),"UnitEntityData","get_View")==1 && Count(Body(Required(host,"ViewOf",1)),null,"get_Mount")==0 &&
                    Count(Body(Required(host,"get_Mount",0)),null,"get_Mount")==1 &&
                    Count(Body(Required(host,"IsHeld",1)),"UnitCommands","Contains")==1 && Count(Body(Required(host,"IsHeld",1)),"UnitCommands","get_Queue")==1 &&
                    Count(Body(Required(host,"IsHeld",1)),"UnitCommands","get_PreviousCommand")==1 &&
                    Count(Body(Required(host,"IsHeld",1)),"UnitCommand","get_Executor")==1 &&
                    Count(Body(Required(host,"ExecutorOf",1)),"UnitCommand","get_Executor")==1 && Count(Body(Required(host,"IsFinished",1)),"UnitCommand","get_IsFinished")==1 &&
                    Count(Body(Required(host,"OverrideOf",1)),"UnitEntityView","get_AgentOverride")==1;
            });
        Check("PRODUCT-IL the adapter's three effects are native calls on what it was handed: UnitCommand.Interrupt, UnitEntityView.AgentOverride = null, and the rider's Hold; binding and the invariant's question go straight to the owner",
            ()=>{
                var host=product.GetType("KingmakerMountedCombat.Integration.GameMountedRelationshipService+ContinuousDriveHost",true);
                var interrupt=Body(Required(host,"Interrupt",2));
                var clear=Body(Required(host,"ClearOverride",1));
                var hold=Body(Required(host,"ReleaseRiderHold",0));
                var write=One(clear,"UnitEntityView","set_AgentOverride");
                var bind=Body(Required(relationship,"BindContinuousDrive",1));
                var postfix=Body(Required(patchMethods,"ContinuousMovePostfix",1));
                var holdWrite=-1;
                for(var i=0;i<hold.Count;i++) if(hold[i].Field!=null && hold[i].Field.Name.Contains("HoldState") && hold[i].Op==OpCodes.Stfld) holdWrite=i;
                if(holdWrite<0) holdWrite=One(hold,"UnitEntityData","set_HoldState");
                return One(interrupt,"UnitCommand","Interrupt")>=0 && clear[write-1].Op==OpCodes.Ldnull &&
                    holdWrite>One(hold,null,"get_Rider") && Count(hold,null,"get_Mount")==0 &&
                    Count(bind,"MountedContinuousDriveOwner","Bind")==1 &&
                    One(postfix,"GameMountedRelationshipService","BindContinuousDrive")>One(postfix,"UnitCommand","get_Executor");
            });
        Check("PRODUCT-IL retirement transfers the record to residue before the active record is cleared and keeps it in the list for the attempt; one attempt plans and interrupts, then re-reads the captured view, plans again and clears the captured agent on its own, then asks whether the retirement is established; release and settlement recover residue",
            ()=>{
                var ownerType=product.GetType("KingmakerMountedCombat.Domain.MountedContinuousDriveOwner",true);
                var residueType=ownerType.GetNestedType("Residue",Any);
                if(residueType==null) throw new InvalidOperationException("the owner has no residue record in this build");
                var retire=Body(Required(ownerType,"Retire",3));
                var complete=Body(Required(ownerType,"Attempt",1));
                var installed=Body(Required(ownerType,"CapturedAgentInstalled",1));
                var release=Body(Required(ownerType,"Release",-1));
                var settle=Body(Required(ownerType,"Settle",1));
                var made=-1; var added=-1;
                for(var i=0;i<retire.Count;i++)
                {
                    if(made<0 && retire[i].Op==OpCodes.Newobj && retire[i].Call!=null && retire[i].Call.DeclaringType==residueType) made=i;
                    if(added<0 && Is(retire[i],null,"Add")) added=i;
                }
                var clear=One(retire,"MountedContinuousDriveWatch","Clear");
                var attempt=One(retire,"MountedContinuousDriveOwner","Complete");
                var plans=Find(complete,"MountedContinuousDrivePolicy","PlanRetirement");
                if(plans.Count!=2) throw new InvalidOperationException("the attempt plans "+plans.Count+" times, expected once before each native effect");
                var plan=plans[0];
                var interrupt=One(complete,"IMountedContinuousDriveHost","Interrupt");
                var write=One(complete,"IMountedContinuousDriveHost","ClearOverride");
                var established=One(complete,"MountedContinuousDrivePolicy","RetirementEstablished");
                var reads=Find(complete,"MountedContinuousDriveOwner","CapturedAgentInstalled");
                return made>=0 && made<clear && clear<attempt && added>=0 && added<attempt &&
                    plan<interrupt && interrupt<plans[1] && plans[1]<write && write<established && reads.Count==3 && reads[0]<plan && reads[1]>interrupt && reads[1]<plans[1] && reads[2]>write &&
                    One(complete,"IMountedContinuousDriveHost","IsHeld")<established && Count(installed,"IMountedContinuousDriveHost","OverrideOf")==1 &&
                    Count(complete,"MountedContinuousDriveWatch","Clear")==0 &&
                    First(release,"MountedContinuousDriveOwner","RecoverResidue")<One(release,"MountedContinuousDriveOwner","Retire") &&
                    Count(settle,"MountedContinuousDriveOwner","RecoverResidue")==1;
            });
        Check("PRODUCT-EXECUTED the compiled native-end guard with its bound-command table: for a command the compiled owner bound, it allows the clearing only of the bound agent on the bound view, refuses a foreign override on that view and any override of another view, and returns to the native rule once the command is no longer in the table",
            ()=>{
                var guard=relationship.GetMethod("ContinuousDriveEndMayClearOverride",Any);
                var table=relationship.GetField("continuousDriveCommands",Any).GetValue(null);
                var overrideField=view.GetField("m_AgentOverride",Any);
                var scripted=NewScriptedHost(product);
                var ownerType=scripted.Owner.GetType();
                object boundView=FormatterServices.GetUninitializedObject(view), otherView=FormatterServices.GetUninitializedObject(view);
                object boundAgent=FormatterServices.GetUninitializedObject(continuousAgent), foreignAgent=FormatterServices.GetUninitializedObject(continuousAgent);
                var boundCommand=Activator.CreateInstance(continuous);
                overrideField.SetValue(boundView,boundAgent);
                scripted.CurrentView=boundView; scripted.Overrides[boundView]=boundAgent;
                ownerType.GetMethod("Bind").Invoke(scripted.Owner,new object[]{boundCommand});
                if(!(bool)ownerType.GetProperty("IsBound").GetValue(scripted.Owner,null)) throw new InvalidOperationException("the owner did not bind the command");
                table.GetType().GetMethod("Add").Invoke(table,new object[]{boundCommand,scripted.Owner});
                try
                {
                    var own=(bool)guard.Invoke(null,new object[]{boundCommand,boundView});
                    overrideField.SetValue(boundView,foreignAgent);
                    var foreignOnBoundView=(bool)guard.Invoke(null,new object[]{boundCommand,boundView});
                    overrideField.SetValue(otherView,boundAgent);
                    var anotherView=(bool)guard.Invoke(null,new object[]{boundCommand,otherView});
                    table.GetType().GetMethod("Remove").Invoke(table,new object[]{boundCommand});
                    var afterForgetting=(bool)guard.Invoke(null,new object[]{boundCommand,boundView});
                    Console.WriteLine("INFO compiled guard for a bound command: own agent on bound view="+own+", foreign override on bound view="+foreignOnBoundView+", another view="+anotherView+", after forgetting="+afterForgetting);
                    return own && !foreignOnBoundView && !anotherView && afterForgetting;
                }
                finally { table.GetType().GetMethod("Remove").Invoke(table,new object[]{boundCommand}); }
            });
        Check("PRODUCT-EXECUTED the compiled game adapter's own answers that need no live pair, on genuine engine objects: a command is held while its executor has it in a slot, in its queue or parked as the previous command, and not otherwise; the executor, the finished flag and a view's override are read from the exact objects handed in",
            ()=>{
                var hostType=product.GetType("KingmakerMountedCombat.Integration.GameMountedRelationshipService+ContinuousDriveHost",true);
                var adapter=FormatterServices.GetUninitializedObject(hostType);
                var executor=FormatterServices.GetUninitializedObject(unit);
                var container=Activator.CreateInstance(commands,new object[]{executor});
                var set=false;
                foreach(var field in unit.GetFields(Any)) if(field.FieldType==commands){ field.SetValue(executor,container); set=true; }
                if(!set) throw new InvalidOperationException("the unit's command container field was not found");
                var held=Activator.CreateInstance(continuous);
                baseInitCall.Invoke(null,new object[]{held,executor});
                Func<bool> isHeld=()=>(bool)hostType.GetMethod("IsHeld").Invoke(adapter,new object[]{held});
                var slots=(Array)commands.GetField("m_Commands",Any).GetValue(container);
                var nowhere=isHeld();
                slots.SetValue(held,3);
                var inSlot=isHeld();
                slots.SetValue(null,3);
                var queue=commands.GetProperty("Queue").GetValue(container,null);
                queue.GetType().GetMethod("AddLast",new[]{command}).Invoke(queue,new object[]{held});
                var inQueue=isHeld();
                queue.GetType().GetMethod("Clear").Invoke(queue,null);
                commands.GetProperty("PreviousCommand").SetValue(container,held,null);
                var parked=isHeld();
                commands.GetProperty("PreviousCommand").SetValue(container,null,null);
                var afterwards=isHeld();
                var unheldExecutorless=(bool)hostType.GetMethod("IsHeld").Invoke(adapter,new object[]{Activator.CreateInstance(continuous)});
                var someView=FormatterServices.GetUninitializedObject(view);
                var someAgent=FormatterServices.GetUninitializedObject(continuousAgent);
                view.GetField("m_AgentOverride",Any).SetValue(someView,someAgent);
                var finishedBefore=(bool)hostType.GetMethod("IsFinished").Invoke(adapter,new object[]{held});
                command.GetField("<IsFinished>k__BackingField",Any).SetValue(held,true);
                Console.WriteLine("INFO compiled adapter IsHeld: nowhere="+nowhere+", in a slot="+inSlot+", queued="+inQueue+", parked as previous="+parked+", afterwards="+afterwards);
                return !nowhere && inSlot && inQueue && parked && !afterwards && !unheldExecutorless &&
                    ReferenceEquals(hostType.GetMethod("ExecutorOf").Invoke(adapter,new object[]{held}),executor) &&
                    !finishedBefore && (bool)hostType.GetMethod("IsFinished").Invoke(adapter,new object[]{held}) &&
                    ReferenceEquals(hostType.GetMethod("OverrideOf").Invoke(adapter,new object[]{someView}),someAgent) &&
                    hostType.GetMethod("OverrideOf").Invoke(adapter,new object[]{null})==null && hostType.GetMethod("OverrideOf").Invoke(adapter,new object[]{new object()})==null;
            });
        Check("PRODUCT-IL the native end's guard finds a command's owner in the bound-command table and asks it with the view's current override; the hook writes only after the guard; the adapter's ClearOverride and that guarded native write are the only product code outside the rider's own agent lease that write UnitEntityView.AgentOverride",
            ()=>{
                var guard=Body(Required(relationship,"ContinuousDriveEndMayClearOverride",2));
                var hook=Body(Required(patchMethods,"EndedContinuousCommandOverride",3));
                var bind=Body(Required(relationship,"BindContinuousDrive",1));
                var writers=new List<string>();
                foreach(var type in product.GetTypes())
                {
                    if(type.Namespace!=null && type.Namespace.EndsWith(".Diagnostics")) continue;
                    foreach(var member in type.GetMembers(All))
                    {
                        var method=member as MethodBase;
                        if(method==null || method.IsAbstract || method.GetMethodBody()==null) continue;
                        List<Ins> body;
                        try { body=Body(method); } catch(Exception) { continue; }
                        if(Count(body,"UnitEntityView","set_AgentOverride")>0) writers.Add(type.Name+"."+method.Name);
                    }
                }
                writers.Sort();
                Console.WriteLine("INFO product writers of AgentOverride: "+string.Join(", ",writers.ToArray()));
                foreach(var writer in writers)
                    if(!writer.StartsWith("KingmakerMountedPairRuntime.") && writer!="ContinuousDriveHost.ClearOverride" && writer!="PatchMethods.EndedContinuousCommandOverride")
                        throw new InvalidOperationException("an unexpected writer of AgentOverride: "+writer);
                return One(guard,null,"TryGetValue")<One(guard,"MountedContinuousDriveOwner","NativeEndMayClearOverride") &&
                    One(guard,"UnitEntityView","get_AgentOverride")<One(guard,"MountedContinuousDriveOwner","NativeEndMayClearOverride") &&
                    One(hook,"GameMountedRelationshipService","ContinuousDriveEndMayClearOverride")<One(hook,"UnitEntityView","set_AgentOverride") &&
                    One(bind,"MountedContinuousDriveOwner","Bind")<One(bind,null,"Add") &&
                    writers.Contains("ContinuousDriveHost.ClearOverride") && writers.Contains("PatchMethods.EndedContinuousCommandOverride");
            });
        Check("PRODUCT-IL the drive is settled before the invariant is read and when native Run was called with it; an order is observed after the internal Run that admits commands (the route queue promotion takes), not after the public Run, and counts only when it is in a slot of the rider's own container; the drive is released before the pair's dismount",
            ()=>{
                var validate=Body(Required(relationship,"ValidateActivePair",0));
                var dismount=Body(Required(relationship,"Dismount",1));
                var observe=Body(Required(relationship,"ObserveContinuousDriveRun",1));
                var order=Body(Required(relationship,"ObserveOrderDuringContinuousDrive",2));
                var runPostfix=Body(Required(patchMethods,"UnitCommandRunPostfix",3));
                var internalPostfix=Body(Required(patchMethods,"InternalRunPostfix",2));
                return One(validate,"GameMountedRelationshipService","SettleContinuousDrive")<One(validate,"KingmakerMountedPairRuntime","ValidateMountedInvariants") &&
                    Count(Body(Required(relationship,"SettleContinuousDrive",0)),"MountedContinuousDriveOwner","Settle")==1 &&
                    One(dismount,"GameMountedRelationshipService","ReleaseContinuousDrive")<One(dismount,"MountedRelationshipCoordinator","Dismount") &&
                    Count(Body(Required(relationship,"ReleaseContinuousDrive",-1)),"MountedContinuousDriveOwner","Release")==1 &&
                    Count(observe,"MountedContinuousDriveOwner","ObserveRun")==1 &&
                    One(order,null,"get_Rider")<One(order,"MountedContinuousDriveOwner","ObserveRiderOrder") && Count(order,"UnitCommands","Contains")==1 &&
                    Count(order,"UnitCommand","get_Type")==2 && Count(order,null,"get_Mount")==0 &&
                    Count(runPostfix,"GameMountedRelationshipService","ObserveContinuousDriveRun")==1 &&
                    Count(runPostfix,"GameMountedRelationshipService","ObserveOrderDuringContinuousDrive")==0 &&
                    Count(internalPostfix,"GameMountedRelationshipService","ObserveOrderDuringContinuousDrive")==1 &&
                    Count(internalPostfix,"GameMountedRelationshipService","ObserveContinuousDriveRun")==0;
            });
        Check("PRODUCT-IL the Init prefix ends the rider's own moving orders and the pair's intent before routing, and the driven-unit decisions only decide",
            ()=>{
                var prefix=Body(Required(patchMethods,"ContinuousMovePrefix",1));
                var end=Body(Required(relationship,"EndRiderOrdersForContinuousDrive",0));
                foreach(var name in new[]{"ContinuousDriveMover","ContinuousDriveFormationUnit"})
                    foreach(var ins in Body(Required(relationship,name,name=="ContinuousDriveMover"?1:2)))
                        if(Is(ins,null,"Run") || Is(ins,null,"Interrupt") || Is(ins,null,"InterruptMove") || Is(ins,null,"set_AgentOverride") || Is(ins,null,"Bind"))
                            throw new InvalidOperationException(name+" calls "+ins.Call.Name);
                return One(prefix,"GameMountedRelationshipService","EndRiderOrdersForContinuousDrive")<One(prefix,"MountedCombatController","Cancel") &&
                    One(prefix,"MountedCombatController","Cancel")<One(prefix,"GameMountedRelationshipService","RouteContinuousMove") &&
                    Count(end,"MountedCombatController","InterruptRiderApproachingOrders")==1 && Count(end,"UnitCommands","InterruptMove")==1;
            });
        Check("PRODUCT-IL the rider's mounted agent mirrors the agent that moves the mount (UnitEntityView.MovementAgent) for its moving state and velocity",
            ()=>{
                var moverRead=Body(Required(riderAgent,"get_MountMover",0));
                var uses=0;
                foreach(var name in new[]{"get_WantsToMove","get_IsReallyMoving","UpdateVelocity"})
                {
                    var body=Body(Required(riderAgent,name,0));
                    if(Count(body,"UnitEntityView","get_AgentASP")!=0) throw new InvalidOperationException(name+" reads the stock agent");
                    if(Count(body,"RiderMovementAgent","get_MountMover")>=1) uses++;
                }
                return uses==3 && Count(moverRead,"UnitEntityView","get_MovementAgent")==1 && Count(moverRead,"UnitEntityView","get_AgentASP")==0;
            });
        Check("PRODUCT-IL the group-pace fault report goes through the bounded report, which contains a failing sink",
            ()=>{
                var report=Body(Required(patchMethods,"ReportGroupPaceFault",2));
                var bounded=product.GetType("KingmakerMountedCombat.Domain.MountedBoundedFaultReport",true);
                var calls=0;
                var instance=Activator.CreateInstance(bounded);
                Action<string,Exception> throwing=(message,error)=>{ calls++; throw new InvalidOperationException("sink"); };
                bounded.GetMethod("Report").Invoke(instance,new object[]{throwing,"a",new Exception("x")});
                bounded.GetMethod("Report").Invoke(instance,new object[]{throwing,"b",new Exception("y")});
                return One(report,"MountedBoundedFaultReport","Report")>=0 && calls==1 &&
                    (bool)bounded.GetProperty("Reported").GetValue(instance,null) && (bool)bounded.GetProperty("SinkFailed").GetValue(instance,null);
            });
        Check("PRODUCT-IL patch installation names the sites by exact token: UnitMoveContiniously.Init (060026F0) with prefix and postfix, its OnEnded (060026F2) with the guard transpiler, the internal UnitCommands.Run (060026B3) with its prefix, the admission postfix and its transpiler, UpdateMovement (06005D0C) and MoveSelectedUnitsToPoint (060093DB) with the composed transpilers",
            ()=>{
                var controller=product.GetType("KingmakerMountedCombat.Integration.MountedPatchController",true);
                var wantedByToken=new Dictionary<int,string[]>();
                wantedByToken[0x06005D0C]=new[]{"GamepadMovementTranspiler"};
                wantedByToken[0x060093DB]=new[]{"GroundOrderTranspiler"};
                wantedByToken[0x060026F0]=new[]{"ContinuousMovePrefix","ContinuousMovePostfix"};
                wantedByToken[0x060026F2]=new[]{"ContinuousMoveEndTranspiler"};
                wantedByToken[0x060026B3]=new[]{"InternalRunPrefix","InternalRunPostfix","NativeRunHoldTranspiler"};
                var names=new List<string>{"UpdateMovement","MoveSelectedUnitsToPoint","Init","OnEnded","Run"};
                var found=new List<int>();
                foreach(var member in controller.GetMembers(All))
                {
                    var method=member as MethodBase;
                    if(method==null || method.GetMethodBody()==null) continue;
                    var body=Body(method);
                    for(var i=0;i<body.Count;i++)
                    {
                        if(body[i].Op!=OpCodes.Ldstr || !names.Contains((string)body[i].Operand)) continue;
                        var token=0; var strings=new List<string>();
                        for(var j=i+1;j<body.Count && j<i+30;j++)
                        {
                            if(Is(body[j],"MountedPatchController","PatchExact")) break;
                            if(token==0) foreach(var candidate in wantedByToken.Keys) if(LoadsInt(body[j],candidate)) token=candidate;
                            if(body[j].Op==OpCodes.Ldstr) strings.Add((string)body[j].Operand);
                        }
                        if(token==0 || found.Contains(token)) continue;
                        var wired=0;
                        foreach(var wanted in wantedByToken[token]) if(strings.Contains(wanted)) wired++;
                        if(wired==wantedByToken[token].Length) found.Add(token);
                    }
                }
                return found.Count==5;
            });
        Check("PRODUCT-EXECUTED lifecycle A on the real relationship service (disable, one failed agent removal): the command is bound to V1/A, the mount receives V2 carrying a foreign override B, and the disable cleanup's removal of A fails once; the cleanup result is not complete and names the retained obligation; the retry the root makes before its enabled gate removes A and leaves B; the next disable cleanup is complete; the terminal command is ended once",
            ()=>{
                var life=new Lifecycle(product);
                life.BindAdmitted();
                object viewTwo=new object(), overrideB=new object();
                life.Host.CurrentView=viewTwo; life.Host.Overrides[viewTwo]=overrideB;
                life.Host.FailClears=1;
                var result=life.Dismount("ModDisabled");
                Console.WriteLine("INFO lifecycle A, disable cleanup: ["+life.Host.Log+"]; retained="+life.Retained+"; A installed="+life.AgentInstalled+"; command terminal="+life.Finished+"; result complete="+life.Complete(result)+"; errors=["+Lifecycle.Errors(result)+"]");
                if(!life.Finished || !life.AgentInstalled || life.Retained!=1) throw new InvalidOperationException("setup: the command should be terminal with its agent still installed and retained");
                if(!ReferenceEquals(life.Host.OverrideOn(viewTwo),overrideB)) throw new InvalidOperationException("the failed removal touched the foreign override B");
                if(life.Complete(result)) throw new InvalidOperationException("the disable cleanup reported complete while the captured agent A is still installed on V1 and its removal is retained: the disable would stop ordinary updates over it");
                if(Lifecycle.Errors(result).IndexOf("retained obligation",StringComparison.Ordinal)<0) return false;
                if(!(bool)life.Call("get_ContinuousDriveCleanupPending") || !(bool)life.Call("get_RetainsCleanupObligations")) return false;
                life.Recover();
                Console.WriteLine("INFO lifecycle A, after the pre-gate retry: ["+life.Host.Log+"]; retained="+life.Retained+"; A installed="+life.AgentInstalled);
                if(life.AgentInstalled || life.Retained!=0) throw new InvalidOperationException("the retry outside the ordinary pair update did not remove the captured agent");
                if(!ReferenceEquals(life.Host.OverrideOn(viewTwo),overrideB)) throw new InvalidOperationException("the recovery touched the foreign override B");
                var again=life.Dismount("ModDisabled");
                return life.Complete(again) && Lifecycle.Errors(again)=="" && life.Count("Interrupt")==1 && life.Count("ClearOverride")==2 && life.Warnings==1 &&
                    !(bool)life.Call("get_RetainsCleanupObligations");
            });
        Check("PRODUCT-EXECUTED lifecycle B on the real relationship service (runtime-error cleanup, unfinished command): the cleanup's interrupt fails while the command stays held; the agent is removed on its own; the result does not read as without residue, so the root's failure cleanup cannot stop ordinary updates; the command stays owned and is retired by the next retry, by the root's pre-gate retry and by the ordinary pair update alike",
            ()=>{
                foreach(var byOrdinaryUpdate in new[]{false,true})
                {
                    var life=new Lifecycle(product);
                    life.BindAdmitted();
                    life.Host.FailInterrupts=1;
                    var result=life.Dismount("Exception");
                    Console.WriteLine("INFO lifecycle B, runtime-error cleanup: ["+life.Host.Log+"]; retained="+life.Retained+"; command terminal="+life.Finished+"; A installed="+life.AgentInstalled+"; result without residue="+life.WithoutResidue(result)+"; errors=["+Lifecycle.Errors(result)+"]");
                    if(life.Finished || !ReferenceEquals(life.Host.SlotCommand,life.Command) || life.Retained!=1) throw new InvalidOperationException("setup: the command should still be held and retained");
                    if(life.AgentInstalled) throw new InvalidOperationException("the agent was not removed on its own when the interrupt failed");
                    if(life.WithoutResidue(result)) throw new InvalidOperationException("the runtime-error cleanup read as without residue while the owned command is still held: the root would stop ordinary updates, the command's only recovery");
                    if(byOrdinaryUpdate) life.Call("ValidateActivePair"); else life.Recover();
                    if(!life.Finished || life.Retained!=0) throw new InvalidOperationException("the retained command was not retired by the "+(byOrdinaryUpdate?"ordinary pair update":"pre-gate retry"));
                    var again=life.Dismount("Exception");
                    if(!life.WithoutResidue(again) || life.Count("Interrupt")!=2 || life.Count("ClearOverride")!=1) return false;
                }
                return true;
            });
        Check("PRODUCT-EXECUTED lifecycle C on the real relationship service (persistent failure and teardown): the first attempt and the automatic retries (pre-gate retry, ordinary update, runtime-error cleanup, and the per-frame retry of a failed cleanup) stop at the automatic limit with one report; owner retention, boundary reuse, the gate before guard removal and shutdown stay refused; when the failure ends, a deliberate cleanup retires the command and every refusal clears",
            ()=>{
                var life=new Lifecycle(product);
                var limit=(int)product.GetType("KingmakerMountedCombat.Domain.MountedContinuousDriveOwner",true).GetField("AutomaticRetryLimit").GetValue(null);
                life.BindAdmitted();
                life.Host.FailInterrupts=1000000;
                if(life.Complete(life.Dismount("ModDisabled"))) throw new InvalidOperationException("the disable cleanup reported complete over a command that could not be retired");
                var retryRequired=life.ServiceType.GetField("cleanupRetryRequired",Any);
                var retryTrigger=life.ServiceType.GetField("cleanupRetryTrigger",Any);
                var refusedRetries=0;
                for(var frame=0;frame<8;frame++)
                {
                    life.Host.Frame++;
                    life.Recover();
                    life.Call("ValidateActivePair");
                    if(life.WithoutResidue(life.Dismount("Exception"))) throw new InvalidOperationException("a runtime-error cleanup read as without residue over the retained command");
                    // The per-frame retry of a failed cleanup: a dismount with the disable's own trigger, every frame.
                    retryRequired.SetValue(life.Service,true); retryTrigger.SetValue(life.Service,Enum.Parse(life.Trigger,"ModDisabled"));
                    try { life.Call("ValidateActivePair"); }
                    catch(Exception error) { if(Innermost(error).Message.IndexOf("retained",StringComparison.Ordinal)>=0) refusedRetries++; else throw; }
                }
                Console.WriteLine("INFO lifecycle C, eight frames of automatic recovery: interrupts="+life.Count("Interrupt")+" (automatic limit "+limit+"); warnings="+life.Warnings+"; per-frame cleanup retries refused="+refusedRetries+"; retained="+life.Retained);
                if(life.Count("Interrupt")!=limit) throw new InvalidOperationException("automatic recovery made "+life.Count("Interrupt")+" attempts, expected the automatic limit of "+limit);
                if(life.Warnings!=1 || refusedRetries!=8 || life.Retained!=1 || life.Finished) return false;
                // Teardown stays refused. Each of these gates is a deliberate cleanup and makes one more attempt.
                if(!(bool)life.Call("get_RetainsCleanupObligations")) throw new InvalidOperationException("the pair's owner does not retain its unfinished cleanup: it would be settled and its actors reused");
                var recycled=true;
                try { life.Call("AdvanceIdleBoundaryGeneration"); } catch(Exception error) { if(Innermost(error).Message.IndexOf("unfinished owner",StringComparison.Ordinal)>=0) recycled=false; else throw; }
                if(recycled) throw new InvalidOperationException("the boundary was recycled over retained cleanup");
                if(life.GuardsMayBeRemoved()) throw new InvalidOperationException("the gate before guard removal passed while an owned command can still end natively");
                var shutdown=life.Call("Shutdown");
                if(life.WithoutResidue(shutdown) || life.State=="Disposed") throw new InvalidOperationException("shutdown completed over retained cleanup");
                if(life.Count("Interrupt")!=limit+2 || life.Retained!=1 || life.Warnings!=1) return false;
                // The failure ends; a deliberate cleanup recovers.
                life.Host.FailInterrupts=0;
                var recovered=life.Dismount("ModDisabled");
                Console.WriteLine("INFO lifecycle C, deliberate recovery: complete="+life.Complete(recovered)+"; command terminal="+life.Finished+"; retained="+life.Retained+"; interrupts="+life.Count("Interrupt"));
                if(!life.Complete(recovered) || !life.Finished || life.Retained!=0) return false;
                if((bool)life.Call("get_RetainsCleanupObligations") || !life.GuardsMayBeRemoved()) return false;
                life.Call("AdvanceIdleBoundaryGeneration");
                return life.WithoutResidue(life.Call("Shutdown")) && life.State=="Disposed" && life.Count("Interrupt")==limit+3;
            });
        Check("PRODUCT-EXECUTED a readiness refusal on the real relationship service does not stop recovery: with the charge barrier refusing the cleanup, the dismount still retries the retained retirement, and reports only the refusal once nothing is retained",
            ()=>{
                var life=new Lifecycle(product);
                life.BindAdmitted();
                life.Host.FailInterrupts=1;
                if(life.Complete(life.Dismount("ModDisabled")) || life.Retained!=1 || life.Finished) throw new InvalidOperationException("setup: the command's retirement should be retained");
                var barrier=life.ServiceType.GetProperty("ChargeCleanupBarrier",Any);
                barrier.SetValue(life.Service,Delegate.CreateDelegate(barrier.PropertyType,typeof(KmcContinuousDriveProbe).GetMethod("Refuse").MakeGenericMethod(life.Trigger)),null);
                var refused=life.Dismount("ModDisabled");
                Console.WriteLine("INFO readiness refusal: retained="+life.Retained+"; command terminal="+life.Finished+"; errors=["+Lifecycle.Errors(refused)+"]");
                if(life.Retained!=0 || !life.Finished) throw new InvalidOperationException("the readiness refusal returned before the retained cleanup was retried");
                if(life.Complete(refused) || Lifecycle.Errors(refused).IndexOf("charge cleanup",StringComparison.Ordinal)<0 ||
                    Lifecycle.Errors(refused).IndexOf("retained obligation",StringComparison.Ordinal)>=0) return false;
                // Past the automatic limit, a refused deliberate cleanup still makes its attempt; a refused cleanup from a
                // path that can run every frame makes none.
                var limit=(int)product.GetType("KingmakerMountedCombat.Domain.MountedContinuousDriveOwner",true).GetField("AutomaticRetryLimit").GetValue(null);
                var persistent=new Lifecycle(product);
                persistent.BindAdmitted();
                persistent.Host.FailInterrupts=1000000;
                persistent.Dismount("ModDisabled");
                for(var frame=0;frame<4;frame++) persistent.Recover();
                if(persistent.Count("Interrupt")!=limit) throw new InvalidOperationException("setup: the automatic limit was not reached");
                barrier.SetValue(persistent.Service,Delegate.CreateDelegate(barrier.PropertyType,typeof(KmcContinuousDriveProbe).GetMethod("Refuse").MakeGenericMethod(persistent.Trigger)),null);
                for(var frame=0;frame<4;frame++) persistent.Dismount("Exception");
                if(persistent.Count("Interrupt")!=limit) throw new InvalidOperationException("a refused cleanup from a per-frame path retried beyond the automatic limit");
                var deliberate=persistent.Dismount("ModDisabled");
                Console.WriteLine("INFO readiness refusal past the limit: interrupts after four per-frame cleanups="+limit+", after one deliberate cleanup="+persistent.Count("Interrupt")+"; errors=["+Lifecycle.Errors(deliberate)+"]");
                return persistent.Count("Interrupt")==limit+1 && persistent.Retained==1 &&
                    Lifecycle.Errors(deliberate).IndexOf("charge cleanup",StringComparison.Ordinal)>=0 && Lifecycle.Errors(deliberate).IndexOf("retained obligation",StringComparison.Ordinal)>=0 &&
                    !(bool)persistent.Call("get_LastCleanupHeldOnlyByStickMovement");
            });
        Check("PRODUCT-EXECUTED world boundaries on the real relationship service: with stick-movement cleanup retained past the automatic limit, the area, load and event guard lets the boundary proceed, makes its deliberate attempt, and leaves the obligation retained and the owner unsettled, while the strict guard and the gate before guard removal stay refused; a boundary the attachment itself refuses is refused by both; a cleanup held back by stick movement alone is told from one held back by anything else",
            ()=>{
                var limit=(int)product.GetType("KingmakerMountedCombat.Domain.MountedContinuousDriveOwner",true).GetField("AutomaticRetryLimit").GetValue(null);
                var life=new Lifecycle(product);
                life.BindAdmitted();
                life.Host.FailInterrupts=1000000;
                var first=life.Dismount("Exception");
                if(life.WithoutResidue(first) || !(bool)life.Call("get_LastCleanupHeldOnlyByStickMovement")) throw new InvalidOperationException("a cleanup held back by stick movement alone is not recognised as such");
                for(var frame=0;frame<4;frame++) life.Recover();
                if(life.Count("Interrupt")!=limit) throw new InvalidOperationException("setup: the automatic limit was not reached");
                var area=(bool)life.Call("GuardWorldBoundary",Enum.Parse(life.Trigger,"AreaSuspension"));
                var load=(bool)life.Call("GuardWorldBoundary",Enum.Parse(life.Trigger,"LoadRequested"));
                var scene=(bool)life.Call("GuardWorldBoundary",Enum.Parse(life.Trigger,"EventSuspension"));
                var strict=(bool)life.Call("GuardBoundary",Enum.Parse(life.Trigger,"AreaSuspension"));
                Console.WriteLine("INFO world boundaries with a retained obligation: area="+area+" load="+load+" event="+scene+"; strict guard="+strict+"; interrupts="+life.Count("Interrupt")+"; retained="+life.Retained);
                if(!area || !load || !scene) throw new InvalidOperationException("a world boundary was failed over retained stick-movement cleanup alone");
                if(strict || life.GuardsMayBeRemoved()) throw new InvalidOperationException("the strict guard passed over retained stick-movement cleanup");
                if(life.Count("Interrupt")!=limit+5 || life.Retained!=1 || !(bool)life.Call("get_RetainsCleanupObligations")) return false;
                var barrier=life.ServiceType.GetProperty("ChargeCleanupBarrier",Any);
                barrier.SetValue(life.Service,Delegate.CreateDelegate(barrier.PropertyType,typeof(KmcContinuousDriveProbe).GetMethod("Refuse").MakeGenericMethod(life.Trigger)),null);
                if((bool)life.Call("GuardWorldBoundary",Enum.Parse(life.Trigger,"AreaSuspension")) || (bool)life.Call("get_LastCleanupHeldOnlyByStickMovement"))
                    throw new InvalidOperationException("a boundary the attachment itself refused was let through as if only stick-movement cleanup remained");
                barrier.SetValue(life.Service,null,null);
                var healthy=new Lifecycle(product);
                healthy.BindAdmitted();
                return (bool)healthy.Call("GuardWorldBoundary",Enum.Parse(healthy.Trigger,"AreaSuspension")) && !(bool)healthy.Call("get_LastCleanupHeldOnlyByStickMovement") && healthy.Retained==0;
            });
        Check("PRODUCT-EXECUTED lifecycle D on the real relationship service (healthy controls, which hold on the reviewed build too): a release with nothing failing gives a complete result with no error; a service with no stick movement cleans up, retries nothing, recycles its boundary and passes the gate before guard removal; one pair's retained cleanup leaves another pair's service complete and untouched",
            ()=>{
                var healthy=new Lifecycle(product);
                healthy.BindAdmitted();
                var result=healthy.Dismount("ModDisabled");
                if(!healthy.Complete(result) || Lifecycle.Errors(result)!="" || healthy.Host.Log!="Interrupt(True) Report(Unowned,False)" || healthy.Retained!=0) return false;
                var idle=new Lifecycle(product);
                var idleResult=idle.Dismount("Exception");
                if(idle.Has("RecoverContinuousDriveCleanup")) idle.Recover();
                idle.Call("ValidateActivePair"); idle.Call("AdvanceIdleBoundaryGeneration");
                if(!idle.WithoutResidue(idleResult) || idle.Host.Calls.Count!=0 || idle.OwnerRetains || !idle.GuardsMayBeRemoved()) return false;
                var failing=new Lifecycle(product); var other=new Lifecycle(product);
                failing.BindAdmitted(); failing.Host.FailInterrupts=1;
                failing.Dismount("ModDisabled");
                var otherResult=other.Dismount("ModDisabled");
                return other.Complete(otherResult) && other.Host.Calls.Count==0 && !other.OwnerRetains && other.GuardsMayBeRemoved() && failing.Retained==1;
            });
        Check("PRODUCT-IL the lifecycle gates read the cleanup-completion policy: the dismount adds retained stick-movement cleanup to its result after the coordinator and before ownership completion, and decides deliberate or automatic recovery by the policy; the disable's per-pair cleanup, the boundary guard, shutdown, the failed-cleanup retry and the root's failure cleanup decide on the result through it; the failed-cleanup retry marks its dismount automatic",
            ()=>{
                var policy="MountedCleanupCompletionPolicy";
                var dismount=Body(Required(relationship,"Dismount",1));
                var retry=Body(Required(relationship,"RetryFailedCleanupOrThrow",0));
                var subscriber=product.GetType("KingmakerMountedCombat.Integration.MountedLifecycleSubscriber",true);
                var root=product.GetType("KingmakerMountedCombat.CompositionRoot",true);
                var validate=Body(Required(relationship,"ValidateActivePair",0));
                // Every dismount the per-frame update makes is bracketed by the automatic mark: set before the call,
                // cleared after it.
                var flagWrites=0;
                foreach(var body in new[]{validate,retry})
                {
                    var marks=new List<int>();
                    for(var i=0;i<body.Count;i++) if(body[i].Op==OpCodes.Stfld && body[i].Field!=null && body[i].Field.Name=="automaticCleanupRetry") marks.Add(i);
                    var call=One(body,"GameMountedRelationshipService","Dismount");
                    if(marks.Count!=2 || marks[0]>call || marks[1]<call || body[marks[0]-1].Op!=OpCodes.Ldc_I4_1 || body[marks[1]-1].Op!=OpCodes.Ldc_I4_0)
                        throw new InvalidOperationException("a dismount made from the pair's per-frame update is not marked automatic");
                    flagWrites+=marks.Count;
                }
                var failureCleanup=false;
                foreach(var lambda in Lambdas(root,"PerformFailureCleanup"))
                {
                    var body=Body(lambda);
                    if(Count(body,"GameMountedRelationshipService","Dismount")==1 && One(body,"GameMountedRelationshipService","Dismount")<One(body,policy,"IsWithoutResidue") &&
                        Count(body,"GameMountedRelationshipService","get_LastCleanupHeldOnlyByStickMovement")==1) failureCleanup=true;
                }
                // The stop held back by stick-movement cleanup alone is latched, and taken again by Update once no pair
                // owes any.
                var failure=Body(Required(root,"PerformFailureCleanup",0));
                var latched=0; var disabled=-1;
                for(var i=0;i<failure.Count;i++)
                {
                    if(failure[i].Op==OpCodes.Stfld && failure[i].Field!=null && failure[i].Field.Name=="failureCleanupPending") latched++;
                    if(Is(failure[i],"CompositionRoot","set_IsEnabled")) disabled=i;
                }
                var rootUpdate=Body(Required(root,"UpdateCore",-1));
                if(latched!=1 || disabled<0 || First(failure,null,"Visit")>disabled ||
                    One(rootUpdate,"MountedWorldService","get_AnyContinuousDriveCleanupPending")>One(rootUpdate,"CompositionRoot","PerformFailureCleanup"))
                    throw new InvalidOperationException("the runtime-error stop is not deferred until retained stick-movement cleanup is established");
                var with=Find(dismount,policy,"WithRetainedStickMovement");
                return with.Count==2 && One(dismount,"MountedRelationshipCoordinator","Dismount")<with[1] && with[1]<One(dismount,"GameMountedRelationshipService","CompleteOwnershipAttempt") &&
                    One(dismount,policy,"ReleaseRecoversExplicitly")<One(dismount,"GameMountedRelationshipService","ReleaseContinuousDrive") &&
                    Count(dismount,"MountedContinuousDriveOwner","RecoverRetained")==1 && Count(dismount,"MountedContinuousDriveOwner","RecoverRetainedDeliberately")==1 &&
                    Count(Body(Required(subscriber,"CleanupOwner",5)),policy,"IsComplete")==1 &&
                    Count(Body(Required(relationship,"GuardBoundary",1)),policy,"IsComplete")==1 &&
                    Count(Body(Required(relationship,"Shutdown",0)),policy,"IsWithoutResidue")==1 &&
                    Count(retry,policy,"IsComplete")==1 && flagWrites==4 && failureCleanup;
            });
        Check("PRODUCT-IL the area, load and event boundaries read the world-boundary guard, which decides on the attachment's own result; the gate before guard removal and every other caller read the strict guard",
            ()=>{
                var world=new List<string>(); var strict=new List<string>(); var unread=new List<string>();
                foreach(var type in product.GetTypes())
                    foreach(var member in type.GetMembers(All))
                    {
                        var method=member as MethodBase;
                        if(method==null || method.GetMethodBody()==null || type.Namespace=="KingmakerMountedCombat.Diagnostics" || (type.DeclaringType!=null && type.DeclaringType.Namespace=="KingmakerMountedCombat.Diagnostics")) continue;
                        var owner0=(type.DeclaringType??type).Name;
                        List<Ins> body;
                        try { body=Body(method); } catch(Exception) { unread.Add(owner0+"."+method.Name); continue; }
                        var owner=(type.DeclaringType??type).Name;
                        for(var i=0;i<Count(body,"GameMountedRelationshipService","GuardWorldBoundary");i++) world.Add(owner+"."+method.Name);
                        for(var i=0;i<Count(body,"GameMountedRelationshipService","GuardBoundary");i++) strict.Add(owner);
                    }
                world.Sort(); strict.Sort();
                Console.WriteLine("INFO callers of the world-boundary guard: "+string.Join(",",world.ToArray())+"; of the strict guard: "+string.Join(",",strict.ToArray())+"; methods the instruction reader could not read: "+unread.Count);
                var guard=Body(Required(relationship,"GuardWorldBoundary",1));
                // The three callers are the persistence service's area suspension, load housekeeping and event suspension.
                return world.Count==3 && world[0]=="MountedPersistenceService.BeginLoadHousekeeping" && world[1]=="MountedPersistenceService.SuspendAreaPair" && world[2]=="MountedPersistenceService.SuspendPairs" &&
                    string.Join(",",strict.ToArray())=="MountedPatchController" &&
                    One(guard,"GameMountedRelationshipService","Dismount")<One(guard,"MountedCleanupCompletionPolicy","IsComplete") &&
                    Count(Body(Required(relationship,"get_LastCleanupHeldOnlyByStickMovement",0)),"MountedCleanupCompletionPolicy","IsWithoutResidue")==1;
            });
        Check("PRODUCT-IL recovery and teardown around the gates: the root retries retained stick-movement cleanup before it reads its enabled gate, through the owner's automatic recovery; owner retention (reservation and import), boundary recycling and the world's boundary reuse read the retained cleanup; the patch controller asks the boundary guard before it removes the Harmony guards",
            ()=>{
                var root=product.GetType("KingmakerMountedCombat.CompositionRoot",true);
                var world=product.GetType("KingmakerMountedCombat.Integration.MountedWorldService",true);
                var controller=product.GetType("KingmakerMountedCombat.Integration.MountedPatchController",true);
                var update=Body(Required(root,"UpdateCore",-1));
                MethodBase recoverLambda=null;
                foreach(var lambda in Lambdas(root,"UpdateCore")) if(Count(Body(lambda),"GameMountedRelationshipService","RecoverContinuousDriveCleanup")==1) recoverLambda=lambda;
                if(recoverLambda==null) throw new InvalidOperationException("the root's update does not retry retained stick-movement cleanup");
                var taken=-1;
                for(var i=0;i<update.Count;i++) if(update[i].Op==OpCodes.Ldftn && ReferenceEquals(update[i].Call,recoverLambda) || update[i].Op==OpCodes.Ldftn && update[i].Call!=null && update[i].Call.MetadataToken==recoverLambda.MetadataToken) taken=i;
                var recover=Body(Required(relationship,"RecoverContinuousDriveCleanup",0));
                var retained=0;
                var importSite=Method(relationship,"ImportSavedOwnershipForSession",-1)!=null?"ImportSavedOwnershipForSession":"ImportSavedOwnership";
                foreach(var name in new[]{"TryReserveOwnership",importSite})
                    foreach(var lambda in Lambdas(relationship,name)) if(Count(Body(lambda),"GameMountedRelationshipService","get_RetainsCleanupObligations")==1) retained++;
                var reuse=0;
                foreach(var name in new[]{"AcquireEmptyBoundary","ReleaseSettledAdmissions"})
                {
                    var found=Count(Body(Required(world,name,0)),"GameMountedRelationshipService","get_ContinuousDriveCleanupPending");
                    foreach(var lambda in Lambdas(world,name)) found+=Count(Body(lambda),"GameMountedRelationshipService","get_ContinuousDriveCleanupPending");
                    if(found==1) reuse++;
                }
                var dispose=Body(Required(controller,"Dispose",0));
                return taken>=0 && taken<First(update,"CompositionRoot","get_IsEnabled") &&
                    Count(recover,"MountedContinuousDriveOwner","RecoverRetained")==1 && Count(recover,"MountedContinuousDriveOwner","Release")==0 &&
                    Count(Body(Required(relationship,"get_RetainsCleanupObligations",0)),"MountedCleanupCompletionPolicy","OwnerRetainsCleanup")==1 &&
                    retained==2 && reuse==2 &&
                    Count(Body(Required(relationship,"AdvanceIdleBoundaryGeneration",0)),"GameMountedRelationshipService","get_RetainsCleanupObligations")==1 &&
                    One(dispose,null,"GuardNativeBoundary")<One(dispose,null,"UnpatchAll");
            });
        Check("PRODUCT-EXECUTED world boundary A, area transition, on the real world service with a registered owner: the pair's owner is imported and committed, its drive's agent removal fails once at the area suspension guard while a foreign override B sits on the mount's new view; the guard passes and the admission discard that follows passes with the owner still registered (detached, its actors and identity held); the arrival defers the pair instead of importing beside that owner; after recovery the owner retires, B is untouched, and the pair is imported with its exact identity, generation, session and campaign",
            ()=>{
                var w=new WorldFixture(product);
                try
                {
                    var rider=w.Actor("rider-a"); var mount=w.Actor("mount-a"); var id=Guid.NewGuid();
                    var old=w.Boundary();
                    var owner=w.Register(old,rider,mount,id,7L,w.Session,w.Campaign);
                    object driveCommand=new object(), viewOne=new object(), agent=new object(), viewTwo=new object(), foreign=new object();
                    var host=w.Drive(old,product,driveCommand,viewOne,agent);
                    host.CurrentView=viewTwo; host.Overrides[viewTwo]=foreign;
                    host.FailClears=1;
                    var guard=w.WorldGuard(old,"AreaSuspension");
                    var discard=w.Discard(false);
                    Console.WriteLine("INFO world A, area suspension: guard="+guard+"; owner "+WorldFixture.State(owner)+"; retained="+w.Retained(old)+"; admission discard: "+(discard??"passed"));
                    if(!guard) throw new InvalidOperationException("setup: the world-boundary guard refused");
                    if(w.Retained(old)!=1 || WorldFixture.State(owner)!="Detached" || !ReferenceEquals(w.OwnerOf(rider),owner)) throw new InvalidOperationException("setup: the owner should be detached and still registered");
                    if(discard!=null) throw new InvalidOperationException("the admission discard after the guard failed over stick-movement cleanup alone ("+discard+"): area suspension would latch a failed transfer");
                    if(!ReferenceEquals(w.OwnerOf(rider),owner) || !ReferenceEquals(w.OwnerOf(mount),owner) || w.OwnerCount!=1) throw new InvalidOperationException("the discard erased the unfinished owner");
                    if(!ReferenceEquals(host.OverrideOn(viewTwo),foreign) || !ReferenceEquals(host.OverrideOn(viewOne),agent)) throw new InvalidOperationException("the failed removal touched the foreign override or lost the captured agent");
                    // The arrival: the pair is held, so it is deferred; an import beside the owner is refused cleanly.
                    if(!w.Held(old,rider,mount,id)) throw new InvalidOperationException("the arrival does not see the pair held by its earlier owner");
                    var restored=w.Boundary();
                    var early=w.TryRegister(restored,rider,mount,id,7L,w.Session,w.Campaign);
                    if(early==null || early.IndexOf("stick-movement cleanup is still outstanding",StringComparison.Ordinal)<0) throw new InvalidOperationException("an import beside the earlier owner was not refused with the truthful reason: "+(early??"admitted"));
                    if(w.OwnerCount!=1) throw new InvalidOperationException("a refused import left a duplicate owner");
                    // Recovery before the root's gate; the owner retires; the deferred pair restores with its identity.
                    w.Recover(old);
                    w.Settle();
                    Console.WriteLine("INFO world A, after recovery: retained="+w.Retained(old)+"; owner "+WorldFixture.State(owner)+"; held="+w.Held(old,rider,mount,id)+"; B intact="+ReferenceEquals(host.OverrideOn(viewTwo),foreign));
                    if(w.Retained(old)!=0 || WorldFixture.State(owner)!="Retired" || w.Held(old,rider,mount,id)) return false;
                    if(!ReferenceEquals(host.OverrideOn(viewTwo),foreign) || host.OverrideOn(viewOne)!=null || host.Finished.Count!=1) return false;
                    var late=w.TryRegister(restored,rider,mount,id,7L,w.Session,w.Campaign);
                    var again=w.PairOwner(restored);
                    if(late!=null) throw new InvalidOperationException("the deferred pair was not restored after the cleanup: "+late);
                    return w.OwnerCount==1 && (Guid)WorldFixture.Read(again,"Id")==id && (long)WorldFixture.Read(again,"Generation")==7L &&
                        ReferenceEquals(WorldFixture.Read(again,"Session"),w.Session) && (string)WorldFixture.Read(again,"CampaignId")==w.Campaign &&
                        WorldFixture.State(again)=="Active" && w.PairOwner(old)==null;
                }
                finally { w.Close(); }
            });
        Check("PRODUCT-EXECUTED world boundary B, load, on the real world service with a registered owner: the old world's owner still owes stick-movement cleanup through load housekeeping; the world-replacement discard passes and sets that owner aside with its obligation; the new world's pair (the same characters from the save, same owner identity) is imported under the new session without an old-session, actor-index or duplicate-identity refusal; the old cleanup, recovered later, touches only its own view and leaves the new pair, its owner and its view alone",
            ()=>{
                var w=new WorldFixture(product);
                try
                {
                    var rider=w.Actor("rider-b"); var mount=w.Actor("mount-b"); var id=Guid.NewGuid();
                    var old=w.Boundary();
                    var owner=w.Register(old,rider,mount,id,4L,w.Session,w.Campaign);
                    object driveCommand=new object(), viewOne=new object(), agent=new object();
                    var host=w.Drive(old,product,driveCommand,viewOne,agent);
                    host.FailInterrupts=1000000;
                    var guard=w.WorldGuard(old,"LoadRequested");
                    var discard=w.Discard(true);
                    Console.WriteLine("INFO world B, load housekeeping: guard="+guard+"; retained="+w.Retained(old)+"; world-replacement discard: "+(discard??"passed")+"; live owners="+w.OwnerCount+"; set aside="+w.EarlierCount);
                    if(!guard || w.Retained(old)!=1) throw new InvalidOperationException("setup: the obligation should be retained through the load guard");
                    if(discard!=null) throw new InvalidOperationException("load housekeeping failed over stick-movement cleanup alone ("+discard+")");
                    if(w.OwnerCount!=0 || w.EarlierCount!=1 || WorldFixture.State(owner)!="Detached") throw new InvalidOperationException("the old owner was not set aside with its obligation");
                    // The new world: new objects for the same characters, the save's owner identity.
                    var session=new object(); var campaign=w.Campaign;
                    var newRider=w.Actor("rider-b"); var newMount=w.Actor("mount-b");
                    var fresh=w.Boundary();
                    if(ReferenceEquals(fresh,old)) throw new InvalidOperationException("the boundary that still owes cleanup was reused");
                    var refusal=w.AdmissionRefusal(session,campaign,newRider,newMount);
                    var import=w.TryRegister(fresh,newRider,newMount,id,4L,session,campaign);
                    var newOwner=w.PairOwner(fresh);
                    Console.WriteLine("INFO world B, new world: admission refusal="+(refusal??"none")+"; import="+(import??"imported")+"; new owner "+WorldFixture.State(newOwner));
                    if(refusal!=null || import!=null) throw new InvalidOperationException("the new world was refused over the old world's cleanup: "+(refusal??import));
                    if(!ReferenceEquals(WorldFixture.Read(newOwner,"Session"),session) || (Guid)WorldFixture.Read(newOwner,"Id")!=id || !ReferenceEquals(w.OwnerOf(newRider),newOwner)) return false;
                    // The new pair's mount view, with its own override, as the scripted host sees views.
                    object newView=new object(), newOverride=new object();
                    host.Overrides[newView]=newOverride; host.CurrentView=newView;
                    host.FailInterrupts=0;
                    w.Recover(old);
                    for(var frame=0;frame<3;frame++) { host.Frame++; w.Dismount(old,"ModDisabled"); }
                    w.Settle();
                    Console.WriteLine("INFO world B, old cleanup after the load: retained="+w.Retained(old)+"; old owner "+WorldFixture.State(owner)+"; set aside="+w.EarlierCount+"; new override intact="+ReferenceEquals(host.OverrideOn(newView),newOverride)+"; new owner "+WorldFixture.State(newOwner));
                    return w.Retained(old)==0 && host.Finished.Contains(driveCommand) && ReferenceEquals(host.OverrideOn(newView),newOverride) &&
                        WorldFixture.State(owner)=="Retired" && w.EarlierCount==0 && w.PairOwner(old)==null &&
                        WorldFixture.State(newOwner)=="Active" && ReferenceEquals(w.OwnerOf(newRider),newOwner) && w.OwnerCount==1;
                }
                finally { w.Close(); }
            });
        Check("PRODUCT-EXECUTED world boundary C, persistent cleanup, on the real world service: the automatic limit is reached with one report; the obligation and the owner stay; the pair stays held, an import or a new admission of it is refused with a truthful reason, and nothing reports it restored; the world boundaries still pass; disable, shutdown and guard removal stay strict on the held boundary; a later deliberate recovery clears the obligation, the owner retires and the pair imports with its identity",
            ()=>{
                var w=new WorldFixture(product);
                try
                {
                    var limit=(int)product.GetType("KingmakerMountedCombat.Domain.MountedContinuousDriveOwner",true).GetField("AutomaticRetryLimit").GetValue(null);
                    var rider=w.Actor("rider-c"); var mount=w.Actor("mount-c"); var id=Guid.NewGuid();
                    var old=w.Boundary();
                    var owner=w.Register(old,rider,mount,id,3L,w.Session,w.Campaign);
                    object driveCommand=new object(), viewOne=new object(), agent=new object();
                    var host=w.Drive(old,product,driveCommand,viewOne,agent);
                    host.FailInterrupts=1000000;
                    w.Dismount(old,"Exception");
                    for(var frame=0;frame<8;frame++) { host.Frame++; w.Recover(old); }
                    var interrupts=0; foreach(var call in host.Calls) if(call.StartsWith("Interrupt")) interrupts++;
                    var warnings=0; foreach(var call in host.Calls) if(call.StartsWith("Report(") && call.EndsWith(",True)")) warnings++;
                    if(interrupts!=limit || warnings!=1 || w.Retained(old)!=1) throw new InvalidOperationException("setup: the automatic limit was not reached with one report");
                    var discard=w.Discard(false);
                    if(discard!=null) throw new InvalidOperationException("the admission discard failed over the held pair: "+discard);
                    var held=w.Held(old,rider,mount,id);
                    var restored=w.Boundary();
                    var import=w.TryRegister(restored,rider,mount,id,3L,w.Session,w.Campaign);
                    var admission=w.AdmissionRefusal(w.Session,w.Campaign,rider,mount);
                    Console.WriteLine("INFO world C, held past the limit: discard="+(discard??"passed")+"; held="+held+"; import refusal="+import+"; admission refusal="+admission);
                    if(discard!=null || !held || import==null || import.IndexOf("stick-movement cleanup is still outstanding",StringComparison.Ordinal)<0 || admission==null) return false;
                    if(w.PairOwner(restored)!=null || w.OwnerCount!=1 || WorldFixture.State(owner)!="Detached") throw new InvalidOperationException("a held pair was reported restored, or its owner was discarded");
                    // Strict gates on the held boundary itself; each is also one deliberate attempt.
                    if(w.StrictGuard(old,"ModDisabled")) throw new InvalidOperationException("the strict guard passed over the retained cleanup");
                    var shutdown=w.Call(old,"Shutdown");
                    if((bool)shutdown.GetType().GetProperty("Succeeded").GetValue(shutdown,null)) throw new InvalidOperationException("shutdown completed over the retained cleanup");
                    if(!w.WorldGuard(old,"EventSuspension")) throw new InvalidOperationException("an event boundary was failed over stick-movement cleanup alone");
                    host.FailInterrupts=0;
                    w.Dismount(old,"ModDisabled");
                    w.Settle();
                    var late=w.TryRegister(restored,rider,mount,id,3L,w.Session,w.Campaign);
                    Console.WriteLine("INFO world C, deliberate recovery: retained="+w.Retained(old)+"; owner "+WorldFixture.State(owner)+"; held="+w.Held(old,rider,mount,id)+"; import="+(late??"imported"));
                    return w.Retained(old)==0 && WorldFixture.State(owner)=="Retired" && !w.Held(old,rider,mount,id) && late==null &&
                        (Guid)WorldFixture.Read(w.PairOwner(restored),"Id")==id && w.OwnerCount==1;
                }
                finally { w.Close(); }
            });
        Check("PRODUCT-EXECUTED world boundary D, controls on the real world service: a healthy release retires its owner at once and the pair imports again; an unrelated registered pair is never held; a detached owner that still holds retained commands, native participation or a failed attachment cleanup keeps the admission discard and the world replacement refused, with or without stick-movement cleanup beside it; a save in flight keeps the discard refused",
            ()=>{
                var w=new WorldFixture(product);
                try
                {
                    var rider=w.Actor("rider-d"); var mount=w.Actor("mount-d"); var id=Guid.NewGuid();
                    var other=w.Actor("rider-e"); var otherMount=w.Actor("mount-e"); var otherId=Guid.NewGuid();
                    var healthy=w.Boundary();
                    var owner=w.Register(healthy,rider,mount,id,2L,w.Session,w.Campaign);
                    var unrelated=w.Boundary();
                    var unrelatedOwner=w.Register(unrelated,other,otherMount,otherId,9L,w.Session,w.Campaign);
                    var host=w.Drive(healthy,product,new object(),new object(),new object());
                    var healthyGuard=w.WorldGuard(healthy,"AreaSuspension"); var unrelatedGuard=w.WorldGuard(unrelated,"AreaSuspension");
                    w.Settle();
                    var healthyDiscard=w.Discard(false);
                    Console.WriteLine("INFO world D, healthy: guards="+healthyGuard+"/"+unrelatedGuard+"; retained="+w.Retained(healthy)+"; owners "+WorldFixture.State(owner)+"/"+WorldFixture.State(unrelatedOwner)+"; discard="+(healthyDiscard??"passed"));
                    if(!healthyGuard || !unrelatedGuard || w.Retained(healthy)!=0 || WorldFixture.State(owner)!="Retired" || WorldFixture.State(unrelatedOwner)!="Retired" || healthyDiscard!=null) return false;
                    if(w.Seam && (w.Held(healthy,rider,mount,id) || w.Held(healthy,other,otherMount,otherId))) return false;
                    var restored=w.Boundary();
                    var restoredImport=w.TryRegister(restored,rider,mount,id,2L,w.Session,w.Campaign);
                    var unrelatedImport=w.TryRegister(w.Boundary(),other,otherMount,otherId,9L,w.Session,w.Campaign);
                    Console.WriteLine("INFO world D, healthy arrival: imports="+(restoredImport??"imported")+"/"+(unrelatedImport??"imported"));
                    if(restoredImport!=null || unrelatedImport!=null) return false;
                    // Genuine world obligations keep their refusals.
                    var labels=new[]{"retained commands","native participation","a failed attachment cleanup"};
                    for(var kind=0;kind<3;kind++)
                        foreach(var withStick in new[]{false,true})
                        {
                            var v=new WorldFixture(product);
                            try
                            {
                                var r=v.Actor("rider-f"); var m=v.Actor("mount-f");
                                var b=v.Boundary();
                                var o=v.Register(b,r,m,Guid.NewGuid(),1L,v.Session,v.Campaign);
                                var h=v.Drive(b,product,new object(),new object(),new object());
                                if(withStick) { h.FailInterrupts=1000000; h.CurrentView=new object(); }
                                // The obligation is in place before the boundary: the pair's own predicates read it.
                                if(kind==0) v.ServiceType.GetField("retainedCommands",Any).SetValue(b,new Func<bool>(()=>true));
                                if(kind==1) v.ServiceType.GetField("retainedParticipation",Any).SetValue(b,Delegate.CreateDelegate(
                                    typeof(Func<,,>).MakeGenericType(v.UnitType,v.UnitType,typeof(bool)),typeof(KmcContinuousDriveProbe).GetMethod("AlwaysRetained").MakeGenericMethod(v.UnitType)));
                                // A failed attachment cleanup: a Dismounting subscriber fails, as the product records any.
                                if(kind==2) v.ServiceType.GetField("Dismounting",Any).SetValue(b,Delegate.CreateDelegate(
                                    typeof(Action<>).MakeGenericType(v.Trigger),typeof(KmcContinuousDriveProbe).GetMethod("FailingSubscriber").MakeGenericMethod(v.Trigger)));
                                var guarded=v.WorldGuard(b,"AreaSuspension");
                                var discard=v.Discard(false); var replacement=v.Discard(true);
                                if(kind==2 && guarded) throw new InvalidOperationException("the world-boundary guard passed over a failed attachment cleanup");
                                if(withStick && v.Retained(b)!=1) throw new InvalidOperationException("setup: the stick-movement cleanup should be retained beside the other obligation");
                                Console.WriteLine("INFO world D, "+labels[kind]+(withStick?" beside stick cleanup":"")+": discard="+(discard??"passed")+"; world replacement="+(replacement??"passed")+"; owner "+WorldFixture.State(o));
                                if(discard==null || replacement==null || v.OwnerCount!=1 || v.EarlierCount!=0) return false;
                                if(v.Seam && v.Held(b,r,m,(Guid)WorldFixture.Read(o,"Id"))) return false;
                                if(withStick) { h.FailInterrupts=0; v.Recover(b); }
                            }
                            finally { v.Close(); }
                        }
                    w.World.GetType().GetProperty("ArchiveWorkerRunning",Any).SetValue(w.World,new Func<bool>(()=>true),null);
                    return w.Discard(false)!=null;
                }
                finally { w.Close(); }
            });
        Check("PRODUCT-EXECUTED the compiled arrival decisions for a held pair: departing Restore or Defer, or a refusal caused only by its own installed agent, become Defer, while the capital retirement and any other refusal keep their outcome; carried Restore, or Retain only for its own agent, become Retain, while Drop and the capital retirement keep theirs; the candidate counts only the mount's installed override as the pair's own agent and is left unchanged",
            ()=>{
                var intent=product.GetType("KingmakerMountedCombat.Domain.MountedRetainedIntentPolicy",true);
                var departingType=product.GetType("KingmakerMountedCombat.Domain.MountedDepartingArrivalDecision",true);
                var carriedType=product.GetType("KingmakerMountedCombat.Domain.MountedCarriedArrivalDecision",true);
                var departing=intent.GetMethod("DeferHeldArrival"); var carried=intent.GetMethod("DeferHeldCarriedArrival");
                foreach(var decision in Enum.GetValues(departingType))
                    foreach(var agentOnly in new[]{false,true})
                    {
                        var name=decision.ToString();
                        var expected=name=="Restore"||name=="Defer"||agentOnly?"Defer":name;
                        if(departing.Invoke(null,new object[]{true,agentOnly,decision}).ToString()!=expected || departing.Invoke(null,new object[]{false,agentOnly,decision}).ToString()!=name)
                            throw new InvalidOperationException("departing "+name+(agentOnly?" (own agent only)":""));
                    }
                foreach(var decision in Enum.GetValues(carriedType))
                    foreach(var agentOnly in new[]{false,true})
                    {
                        var name=decision.ToString();
                        var expected=name=="Restore"||name=="Retain"&&agentOnly?"Retain":name;
                        if(carried.Invoke(null,new object[]{true,agentOnly,decision}).ToString()!=expected || carried.Invoke(null,new object[]{false,agentOnly,decision}).ToString()!=name)
                            throw new InvalidOperationException("carried "+name+(agentOnly?" (own agent only)":""));
                    }
                var candidateType=product.GetType("KingmakerMountedCombat.Domain.MountedPairCandidate",true);
                Func<object> saved=()=>{
                    var value=Activator.CreateInstance(candidateType,new object[]{"rider","mount"});
                    foreach(var flag in new[]{"RiderIsDirectlyControllable","MountIsDirectlyControllable","RiderIsAliveAndConscious","MountIsAliveAndConscious",
                        "ExactReciprocalCompanionRelationship","RiderViewAndStockAgentAvailable","MountViewAndStockAgentAvailable","RiderStockAgentEnabled",
                        "MountStockAgentEnabled","RiderAgentOverrideAvailable","MountAgentOverrideAvailable","RiderIsExactlyMedium","SafeMovementMode"})
                        candidateType.GetProperty(flag).SetValue(value,true,null);
                    candidateType.GetProperty("RiderSizeOrdinal").SetValue(value,4,null); candidateType.GetProperty("MountSizeOrdinal").SetValue(value,5,null);
                    return value;
                };
                var only=candidateType.GetMethod("RefusedOnlyForMountAgentOverride",Any);
                var agent=saved(); candidateType.GetProperty("MountAgentOverrideAvailable").SetValue(agent,false,null);
                var dead=saved(); candidateType.GetProperty("MountAgentOverrideAvailable").SetValue(dead,false,null); candidateType.GetProperty("MountIsAliveAndConscious").SetValue(dead,false,null);
                return !(bool)only.Invoke(saved(),null) && (bool)only.Invoke(agent,null) && !(bool)candidateType.GetProperty("MountAgentOverrideAvailable").GetValue(agent,null) &&
                    !(bool)only.Invoke(dead,null);
            });
        Check("PRODUCT-IL the world boundaries around the guard: area suspension asks the world-boundary guard, then the admission discard; load housekeeping asks it, then the world-replacement discard (never the plain one); the admission discard decides through the registry, and the world replacement sets detached cleanup aside after it; the arrival asks, for carried and departing pairs alike, whether the pair is held and whether only its own agent refuses it, before any import or attachment, and decides through the held-pair decisions; the in-area retry refuses a held pair before it acquires a boundary; the import refuses a held pair before the registry import; a boundary that still owes cleanup takes no new owner; both ownership sites declare the world predicate without the stick-movement cleanup",
            ()=>{
                var persistence=product.GetType("KingmakerMountedCombat.Integration.MountedPersistenceService",true);
                var world=product.GetType("KingmakerMountedCombat.Integration.MountedWorldService",true);
                var suspend=Body(Required(persistence,"SuspendAreaPair",0));
                var load=Body(Required(persistence,"BeginLoadHousekeeping",0));
                var arrival=Body(Required(persistence,"RestoreAreaPair",0));
                var discard=Body(Required(world,"DiscardAdmissionBindings",0));
                var replacement=Body(Required(world,"DiscardAdmissionBindingsForWorldReplacement",0));
                var import=Body(Required(relationship,"ImportSavedOwnershipForSession",7));
                var reserve=Body(Required(relationship,"TryReserveOwnership",3));
                var helds=Find(arrival,"GameMountedRelationshipService","PairHeldByDetachedCleanup");
                var ownAgent=Find(arrival,"MountedPairCandidate","RefusedOnlyForMountAgentOverride");
                if(helds.Count!=2 || ownAgent.Count!=2) throw new InvalidOperationException("the arrival asks "+helds.Count+" held-pair and "+ownAgent.Count+" own-agent questions, expected one each for carried and departing pairs");
                var held=helds[1];
                foreach(var name in new[]{"ImportSavedOwnership","RestoreSavedPair","RestoreRetainedPair","RestoreParticipation"})
                    foreach(var index in Find(arrival,null,name))
                        if(index<held) throw new InvalidOperationException(name+" precedes the held-pair question");
                if(!(helds[0]<One(arrival,"MountedRetainedIntentPolicy","DecideCarriedArrival") && One(arrival,"MountedRetainedIntentPolicy","DecideCarriedArrival")<One(arrival,"MountedRetainedIntentPolicy","DeferHeldCarriedArrival") &&
                    ownAgent[0]<One(arrival,"MountedRetainedIntentPolicy","DecideCarriedArrival") && held<ownAgent[1] && ownAgent[1]<One(arrival,"MountedRetainedIntentPolicy","DeferHeldArrival")))
                    throw new InvalidOperationException("the held-pair decisions are not made from the held-pair questions");
                var retained=Body(Required(persistence,"RestoreRetainedPair",6));
                var reserveSite=Body(Required(relationship,"TryReserveOwnership",3));
                if(One(retained,"GameMountedRelationshipService","PairHeldByDetachedCleanup")>First(retained,"MountedWorldService","CreateRestoredBoundary") ||
                    One(reserveSite,"GameMountedRelationshipService","get_ContinuousDriveCleanupPending")>One(reserveSite,null,"TryReserve"))
                    throw new InvalidOperationException("the retry or the reservation does not refuse a held pair or a boundary that owes cleanup first");
                var worldLambdas=0;
                foreach(var name in new[]{"TryReserveOwnership","ImportSavedOwnershipForSession"})
                    foreach(var lambda in Lambdas(relationship,name))
                    {
                        var body=Body(lambda);
                        var commandsRead=false; var participationRead=false;
                        foreach(var ins in body) if(ins.Field!=null) { if(ins.Field.Name=="retainedCommands") commandsRead=true; if(ins.Field.Name=="retainedParticipation") participationRead=true; }
                        // The world predicate is the retained predicate without the stick-movement cleanup: commands and
                        // participation still hold the world.
                        if(Count(body,"GameMountedRelationshipService","get_RetainsWorldCleanupObligations")==1 && Count(body,"GameMountedRelationshipService","get_RetainsCleanupObligations")==0 &&
                            commandsRead && participationRead) worldLambdas++;
                    }
                return One(suspend,"GameMountedRelationshipService","GuardWorldBoundary")<One(suspend,"MountedWorldService","DiscardAdmissionBindings") &&
                    Count(suspend,"MountedWorldService","DiscardAdmissionBindingsForWorldReplacement")==0 &&
                    One(load,"GameMountedRelationshipService","GuardWorldBoundary")<One(load,"MountedWorldService","DiscardAdmissionBindingsForWorldReplacement") &&
                    Count(load,"MountedWorldService","DiscardAdmissionBindings")==0 &&
                    Count(discard,null,"SettledForAdmissionDiscard")==1 && Count(discard,null,"get_Owners")==0 &&
                    One(replacement,"MountedWorldService","DiscardAdmissionBindings")<One(replacement,null,"SetAsideEarlierWorldCleanup") &&
                    held<One(arrival,"MountedRetainedIntentPolicy","DecideDepartingArrival") &&
                    One(arrival,"MountedRetainedIntentPolicy","DecideDepartingArrival")<One(arrival,"MountedRetainedIntentPolicy","DeferHeldArrival") &&
                    One(import,null,"HeldForDetachedCleanup")<One(import,null,"TryImport") &&
                    Count(import,null,"DeclareDetachedCleanup")==1 && Count(reserve,null,"DeclareDetachedCleanup")==1 && worldLambdas==2 &&
                    Count(Body(Required(relationship,"get_RetainsWorldCleanupObligations",0)),"MountedCleanupCompletionPolicy","OwnerHoldsWorld")==1;
            });
        Console.WriteLine("CONTINUOUS DRIVE CONTRACT PASS="+passes+" FAIL="+failures+"; installed-assembly and compiled-product contracts, not native gameplay evidence");
        return failures==0?0:1;
    }
}
'@
try { $kmcCode=[KmcContinuousDriveProbe]::Run($kmcManaged,$kmcDll) }
catch { Write-Output $_.Exception.ToString(); exit 1 }
exit $kmcCode
