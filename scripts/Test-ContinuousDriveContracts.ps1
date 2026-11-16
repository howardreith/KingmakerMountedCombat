[CmdletBinding()]
param([ValidateSet('Debug','Release')][string]$Configuration='Release',[string]$ProductDll)
# Known issues sprint 3 continuation: M05 (gamepad stick movement of a mounted pair) and the group-pace fault report.
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
// product's own host interface, answering from the fields below and recording the native effects it is asked for.
public sealed class KmcScriptedDriveHost : RealProxy
{
    public bool PairMounted=true, Gated, AgentIsContinuous=true, InterruptRemovesOverride=true;
    public long Generation=1;
    public int Frame=1;
    public object Mount=new object(), MountView=new object(), Override, SlotCommand, Executor;
    public readonly List<object> Finished=new List<object>();
    public readonly List<string> Calls=new List<string>();
    public KmcScriptedDriveHost(Type type) : base(type) { Executor=Mount; }
    public override IMessage Invoke(IMessage message)
    {
        var call=(IMethodCallMessage)message;
        object result=null;
        switch(call.MethodName)
        {
            case "GetType": result=GetProxiedType(); break;
            case "get_PairMounted": result=PairMounted; break;
            case "get_PairGeneration": result=Generation; break;
            case "get_Frame": result=Frame; break;
            case "get_StickInputGated": result=Gated; break;
            case "get_Mount": result=Mount; break;
            case "get_MountView": result=MountView; break;
            case "ExecutorOf": result=Executor; break;
            case "IsFinished": result=Finished.Contains(call.GetArg(0)); break;
            case "InMountMoveSlot": result=SlotCommand!=null && ReferenceEquals(SlotCommand,call.GetArg(0)); break;
            case "OverrideOf": result=ReferenceEquals(call.GetArg(0),MountView)?Override:null; break;
            case "IsContinuousAgent": result=AgentIsContinuous && call.GetArg(0)!=null; break;
            case "Interrupt":
                Calls.Add("Interrupt("+call.GetArg(1)+")"); Finished.Add(call.GetArg(0));
                if(ReferenceEquals(SlotCommand,call.GetArg(0))) SlotCommand=null;
                if(InterruptRemovesOverride) Override=null;
                break;
            case "ClearOverride": Calls.Add("ClearOverride"); if(ReferenceEquals(call.GetArg(0),MountView)) Override=null; break;
            case "ReleaseRiderHold": Calls.Add("ReleaseRiderHold"); break;
            case "Report": Calls.Add("Report("+call.GetArg(0)+","+call.GetArg(2)+")"); break;
            default: return new ReturnMessage(new InvalidOperationException("unexpected host call "+call.MethodName),call);
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
                var call=new DynamicMethod("KmcBaseInit",typeof(void),new[]{command,unit},command,true);
                var il=call.GetILGenerator(); il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldarg_1); il.Emit(OpCodes.Call,baseInit); il.Emit(OpCodes.Ret);
                call.Invoke(null,new object[]{stickCommand,mountUnit});
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
        Check("PRODUCT-EXECUTED the compiled retirement plan: nothing is ended or cleared unless the exact bound agent is still installed; a command outside the slot is ended without an end event, a running drive that lost its pair, its input or its place is ended natively, an ended command is not interrupted again",
            ()=>{
                var policy=product.GetType("KingmakerMountedCombat.Domain.MountedContinuousDrivePolicy",true);
                var settlement=product.GetType("KingmakerMountedCombat.Domain.MountedContinuousDriveSettlement",true);
                var plan=policy.GetMethod("PlanRetirement");
                Func<string,bool,bool,string> of=(name,installed,finished)=>{
                    var result=plan.Invoke(null,new object[]{Enum.Parse(settlement,name),installed,finished});
                    return ((bool)result.GetType().GetProperty("InterruptCommand").GetValue(result,null)?"I":"-")+
                        ((bool)result.GetType().GetProperty("RaiseEndEvent").GetValue(result,null)?"E":"-")+
                        ((bool)result.GetType().GetProperty("ClearOverride").GetValue(result,null)?"C":"-");
                };
                foreach(var name in Enum.GetNames(settlement)) if(of(name,false,false)!="---" || of(name,false,true)!="---") return false;
                var replaces=policy.GetMethod("RiderOrderReplacesDrive");
                return of("Keep",true,false)=="---" && of("None",true,false)=="---" && of("Orphaned",true,false)=="I-C" && of("Unowned",true,false)=="IEC" &&
                    of("InputGated",true,false)=="IEC" && of("Replaced",true,false)=="IEC" &&
                    of("Ended",true,true)=="--C" && of("Unowned",true,true)=="--C" && of("Orphaned",true,true)=="--C" &&
                    (bool)replaces.Invoke(null,new object[]{true,true,true}) && !(bool)replaces.Invoke(null,new object[]{false,true,true}) &&
                    !(bool)replaces.Invoke(null,new object[]{true,false,true}) && !(bool)replaces.Invoke(null,new object[]{true,true,false});
            });
        Check("PRODUCT-EXECUTED the compiled owner, on a scripted host: a command on the mount with its continuous agent is bound, admitted (the rider's Hold released), owned by the invariant's question, and ended natively when the party enters combat",
            ()=>{
                var hostType=product.GetType("KingmakerMountedCombat.Domain.IMountedContinuousDriveHost",true);
                var ownerType=product.GetType("KingmakerMountedCombat.Domain.MountedContinuousDriveOwner",true);
                var scripted=new KmcScriptedDriveHost(hostType);
                var driveOwner=ownerType.GetConstructors()[0].Invoke(new object[]{scripted.GetTransparentProxy()});
                object pushed=new object(), installedAgent=new object();
                scripted.Override=installedAgent;
                ownerType.GetMethod("Bind").Invoke(driveOwner,new object[]{pushed});
                var boundNow=(bool)ownerType.GetProperty("IsBound").GetValue(driveOwner,null);
                var ownedBeforeRun=(bool)ownerType.GetMethod("OwnsMountOverride").Invoke(driveOwner,new object[]{installedAgent});
                scripted.SlotCommand=pushed;
                ownerType.GetMethod("ObserveRun").Invoke(driveOwner,new object[]{pushed});
                scripted.Frame=50;
                var ownedLater=(bool)ownerType.GetMethod("OwnsMountOverride").Invoke(driveOwner,new object[]{installedAgent});
                var foreignOwned=(bool)ownerType.GetMethod("OwnsMountOverride").Invoke(driveOwner,new object[]{new object()});
                var settledKept=ownerType.GetMethod("Settle").Invoke(driveOwner,new object[]{false}).ToString();
                var logBeforeCombat=scripted.Log;
                scripted.Gated=true;
                var settledInCombat=ownerType.GetMethod("Settle").Invoke(driveOwner,new object[]{false}).ToString();
                Console.WriteLine("INFO scripted host, admitted drive: ["+logBeforeCombat+"] then in combat ["+scripted.Log+"]");
                return boundNow && ownedBeforeRun && ownedLater && !foreignOwned && settledKept=="Keep" && logBeforeCombat=="ReleaseRiderHold" && settledInCombat=="InputGated" &&
                    scripted.Log=="ReleaseRiderHold Interrupt(True) Report(InputGated,False)" && !(bool)ownerType.GetProperty("IsBound").GetValue(driveOwner,null) &&
                    scripted.Override==null;
            });
        Check("PRODUCT-EXECUTED the compiled owner, on a scripted host: a command native Run did not admit is ended without an end event and its agent cleared, reported once across repeated refusals; another unit's command, a non-continuous agent and a foreign override bind or retire nothing",
            ()=>{
                var hostType=product.GetType("KingmakerMountedCombat.Domain.IMountedContinuousDriveHost",true);
                var ownerType=product.GetType("KingmakerMountedCombat.Domain.MountedContinuousDriveOwner",true);
                var scripted=new KmcScriptedDriveHost(hostType);
                scripted.InterruptRemovesOverride=false;
                var driveOwner=ownerType.GetConstructors()[0].Invoke(new object[]{scripted.GetTransparentProxy()});
                for(var push=0;push<3;push++)
                {
                    object pushed=new object();
                    scripted.Frame++; scripted.Override=new object();
                    ownerType.GetMethod("Bind").Invoke(driveOwner,new object[]{pushed});
                    ownerType.GetMethod("ObserveRun").Invoke(driveOwner,new object[]{pushed});
                }
                var refusals=scripted.Log;
                Console.WriteLine("INFO scripted host, three refused admissions: ["+refusals+"]");
                var orphanCount=(long)ownerType.GetProperty("OrphanCount").GetValue(driveOwner,null);
                // Nothing binds for another executor or for an installedAgent that is not a liveCommand continuous installedAgent.
                scripted.Calls.Clear();
                scripted.Executor=new object(); scripted.Override=new object();
                ownerType.GetMethod("Bind").Invoke(driveOwner,new object[]{new object()});
                scripted.Executor=scripted.Mount; scripted.AgentIsContinuous=false;
                ownerType.GetMethod("Bind").Invoke(driveOwner,new object[]{new object()});
                var boundWrongly=(bool)ownerType.GetProperty("IsBound").GetValue(driveOwner,null);
                // A bound, admitted drive whose override was replaced by a foreignOwned one: released, and nothing touched.
                scripted.AgentIsContinuous=true;
                object liveCommand=new object(), ownAgent=new object(), foreignOverride=new object();
                scripted.Override=ownAgent; scripted.Frame++;
                ownerType.GetMethod("Bind").Invoke(driveOwner,new object[]{liveCommand});
                scripted.SlotCommand=liveCommand;
                ownerType.GetMethod("ObserveRun").Invoke(driveOwner,new object[]{liveCommand});
                scripted.Calls.Clear();
                scripted.Override=foreignOverride;
                var ownsForeign=(bool)ownerType.GetMethod("OwnsMountOverride").Invoke(driveOwner,new object[]{foreignOverride});
                ownerType.GetMethod("Release").Invoke(driveOwner,new object[]{"dismount"});
                return refusals=="Interrupt(False) ClearOverride Report(Orphaned,True) Interrupt(False) ClearOverride Interrupt(False) ClearOverride" &&
                    orphanCount==3 && !boundWrongly && !ownsForeign && scripted.Log=="" && ReferenceEquals(scripted.Override,foreignOverride) &&
                    !(bool)ownerType.GetProperty("IsBound").GetValue(driveOwner,null);
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
        Check("PRODUCT-IL the game adapter answers from the pair's exact units: the pair generation field, the mount's Move slot, a UnitMovementAgentContinious test, the party's combat state; its fact answers write nothing",
            ()=>{
                var host=product.GetType("KingmakerMountedCombat.Integration.GameMountedRelationshipService+ContinuousDriveHost",true);
                var generation=false;
                foreach(var ins in Body(Required(host,"get_PairGeneration",0))) if(ins.Field!=null && ins.Field.Name=="mountedPairGeneration") generation=true;
                var isContinuous=false;
                foreach(var ins in Body(Required(host,"IsContinuousAgent",1))) if(ins.Op==OpCodes.Isinst && (Type)ins.Operand==continuousAgent) isContinuous=true;
                var slot=Body(Required(host,"InMountMoveSlot",1));
                foreach(var name in new[]{"get_PairMounted","get_PairGeneration","get_Frame","get_StickInputGated","get_Mount","get_MountView","ExecutorOf","IsFinished","InMountMoveSlot","OverrideOf","IsContinuousAgent"})
                    foreach(var ins in Body(Required(host,name,-1)))
                        if(Is(ins,null,"set_AgentOverride") || Is(ins,null,"Interrupt") || Is(ins,null,"Run") || Is(ins,null,"InterruptMove") || Is(ins,null,"set_HoldState") || ins.Op==OpCodes.Stfld)
                            throw new InvalidOperationException(name+" writes or calls "+(ins.Call!=null?ins.Call.Name:ins.Field.Name));
                return generation && isContinuous && Count(slot,"UnitCommands","GetCommand")==1 && Count(slot,null,"get_Mount")==1 &&
                    Count(Body(Required(host,"get_StickInputGated",0)),"Player","get_IsInCombat")==1 &&
                    Count(Body(Required(host,"get_MountView",0)),null,"get_Mount")==1 && Count(Body(Required(host,"get_Mount",0)),null,"get_Mount")==1 &&
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
        Check("PRODUCT-IL retirement clears the record first, follows the policy's plan, and clears an override only after comparing it again with the bound agent; the adapter's ClearOverride is the only product code outside the rider's own agent lease that writes UnitEntityView.AgentOverride",
            ()=>{
                var ownerType=product.GetType("KingmakerMountedCombat.Domain.MountedContinuousDriveOwner",true);
                var retire=Body(Required(ownerType,"Retire",2));
                var clear=One(retire,"MountedContinuousDriveWatch","Clear");
                var plan=One(retire,"MountedContinuousDrivePolicy","PlanRetirement");
                var interrupt=One(retire,"IMountedContinuousDriveHost","Interrupt");
                var write=One(retire,"IMountedContinuousDriveHost","ClearOverride");
                var reads=Find(retire,"IMountedContinuousDriveHost","OverrideOf");
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
                    if(!writer.StartsWith("KingmakerMountedPairRuntime.") && writer!="ContinuousDriveHost.ClearOverride")
                        throw new InvalidOperationException("an unexpected writer of AgentOverride: "+writer);
                return clear<plan && plan<interrupt && interrupt<write && reads.Count==2 && reads[0]<plan && reads[1]>interrupt && reads[1]<write &&
                    writers.Contains("ContinuousDriveHost.ClearOverride");
            });
        Check("PRODUCT-IL the drive is settled before the invariant is read and when native Run was called with it, a rider order admitted on the rider's own container is reported to it, and it is released before the pair's dismount",
            ()=>{
                var validate=Body(Required(relationship,"ValidateActivePair",0));
                var dismount=Body(Required(relationship,"Dismount",1));
                var observe=Body(Required(relationship,"ObserveContinuousDriveRun",1));
                var order=Body(Required(relationship,"ObserveOrderDuringContinuousDrive",2));
                var runPostfix=Body(Required(patchMethods,"UnitCommandRunPostfix",3));
                return One(validate,"GameMountedRelationshipService","SettleContinuousDrive")<One(validate,"KingmakerMountedPairRuntime","ValidateMountedInvariants") &&
                    Count(Body(Required(relationship,"SettleContinuousDrive",0)),"MountedContinuousDriveOwner","Settle")==1 &&
                    One(dismount,"GameMountedRelationshipService","ReleaseContinuousDrive")<One(dismount,"MountedRelationshipCoordinator","Dismount") &&
                    Count(Body(Required(relationship,"ReleaseContinuousDrive",1)),"MountedContinuousDriveOwner","Release")==1 &&
                    Count(observe,"MountedContinuousDriveOwner","ObserveRun")==1 &&
                    One(order,null,"get_Rider")<One(order,"MountedContinuousDriveOwner","ObserveRiderOrder") && Count(order,"UnitCommands","Contains")==1 &&
                    Count(order,"UnitCommand","get_Type")==2 && Count(order,null,"get_Mount")==0 &&
                    Count(runPostfix,"GameMountedRelationshipService","ObserveContinuousDriveRun")==1 &&
                    Count(runPostfix,"GameMountedRelationshipService","ObserveOrderDuringContinuousDrive")==1;
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
        Check("PRODUCT-IL patch installation names the sites by exact token: UnitMoveContiniously.Init (060026F0) with prefix and postfix, UpdateMovement (06005D0C) and MoveSelectedUnitsToPoint (060093DB) with the composed transpilers",
            ()=>{
                var controller=product.GetType("KingmakerMountedCombat.Integration.MountedPatchController",true);
                var found=0;
                foreach(var member in controller.GetMembers(All))
                {
                    var method=member as MethodBase;
                    if(method==null || method.GetMethodBody()==null) continue;
                    var body=Body(method);
                    for(var i=0;i<body.Count;i++)
                    {
                        if(body[i].Op!=OpCodes.Ldstr) continue;
                        var name=(string)body[i].Operand;
                        var token=name=="UpdateMovement"?0x06005D0C:(name=="MoveSelectedUnitsToPoint"?0x060093DB:(name=="Init"?0x060026F0:0));
                        if(token==0) continue;
                        var wanted=name=="UpdateMovement"?new[]{"GamepadMovementTranspiler"}:(name=="MoveSelectedUnitsToPoint"?new[]{"GroundOrderTranspiler"}:new[]{"ContinuousMovePrefix","ContinuousMovePostfix"});
                        var pinned=false; var wired=0;
                        for(var j=i+1;j<body.Count && j<i+26;j++)
                        {
                            if(Is(body[j],"MountedPatchController","PatchExact")) break;
                            if(LoadsInt(body[j],token)) pinned=true;
                            if(body[j].Op==OpCodes.Ldstr && Array.IndexOf(wanted,(string)body[j].Operand)>=0) wired++;
                        }
                        if(pinned && wired==wanted.Length) found++;
                    }
                }
                return found==3;
            });
        Console.WriteLine("CONTINUOUS DRIVE CONTRACT PASS="+passes+" FAIL="+failures+"; installed-assembly and compiled-product contracts, not native gameplay evidence");
        return failures==0?0:1;
    }
}
'@
try { $kmcCode=[KmcContinuousDriveProbe]::Run($kmcManaged,$kmcDll) }
catch { Write-Output $_.Exception.ToString(); exit 1 }
exit $kmcCode
