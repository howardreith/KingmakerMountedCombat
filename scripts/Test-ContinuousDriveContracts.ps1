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
                    if(byDismount) ownerType.GetMethod("Release").Invoke(scripted.Owner,new object[]{"dismount"});
                    else ownerType.GetMethod("ObserveRiderOrder").Invoke(scripted.Owner,new object[]{true,true});
                    var how=byDismount?"dismount":"rider order";
                    Console.WriteLine("INFO reproduction A ("+how+"): ["+scripted.Log+"]; B survives="+ReferenceEquals(scripted.OverrideOn(viewTwo),overrideB)+"; A removed="+(scripted.OverrideOn(viewOne)==null)+"; command terminal="+scripted.Finished.Contains(pushed));
                    if(!ReferenceEquals(scripted.OverrideOn(viewTwo),overrideB)) throw new InvalidOperationException("the unrelated override B on the mount's current view was cleared by the "+how+" release");
                    if(scripted.OverrideOn(viewOne)!=null || !scripted.Finished.Contains(pushed)) throw new InvalidOperationException("the owned command or its agent was not retired by the "+how+" release");
                    // Later cleanup, settlement and a repeated native termination.
                    ownerType.GetMethod("Settle").Invoke(scripted.Owner,new object[]{false});
                    ownerType.GetMethod("Release").Invoke(scripted.Owner,new object[]{"again"});
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
                ownerType.GetMethod("Release").Invoke(scripted.Owner,new object[]{"dismount"});
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
                    ownerType.GetMethod("Release").Invoke(scripted.Owner,new object[]{"dismount"});
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
                var release=Body(Required(ownerType,"Release",1));
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
                    Count(Body(Required(relationship,"ReleaseContinuousDrive",1)),"MountedContinuousDriveOwner","Release")==1 &&
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
        Console.WriteLine("CONTINUOUS DRIVE CONTRACT PASS="+passes+" FAIL="+failures+"; installed-assembly and compiled-product contracts, not native gameplay evidence");
        return failures==0?0:1;
    }
}
'@
try { $kmcCode=[KmcContinuousDriveProbe]::Run($kmcManaged,$kmcDll) }
catch { Write-Output $_.Exception.ToString(); exit 1 }
exit $kmcCode
