[CmdletBinding()]
param([ValidateSet('Debug','Release')][string]$Configuration='Release',[string]$ProductDll)
# Known issues sprint 3: M01 (entrance acceleration), M02 (gamepad follower acceleration), M03 (group speed inputs)
# and M04 (destination markers).
#
# Two groups of contracts, read without launching anything:
#   NATIVE   the installed Assembly-CSharp contracts the changes rest on: how an entrance order is made and offered
#            acceleration, how the group speed limit is computed, what the gamepad stick layer does to selected units,
#            and how destination pointers are placed and shown (including that a unit's stock agent plans its
#            commands whether or not it is enabled). Also the two native facts behind M05, the stick-DRIVEN mounted
#            pair: native Run refuses a command whose executor is another unit, and the continuous command installs
#            the executor's AgentOverride. M05's own contracts are scripts/Test-ContinuousDriveContracts.ps1.
#   PRODUCT  the compiled KingmakerMountedCombat.dll: the production transpilers are run on the installed methods'
#            real instruction streams and must change exactly their one site; the compiled hooks and decisions are
#            executed; the compiled carry, entrance and marker code is read for what it calls and in which order.
# -ProductDll reads another build (for example the reviewed base, which must FAIL the PRODUCT group). Every check
# runs; the script exits 1 if any failed. This is not native gameplay evidence.
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
using System.Runtime.Serialization;
public static class KmcPhysicalMoverPaceProbe
{
    private static int passes, failures;
    private static MethodInfo read;
    private const BindingFlags All=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly;

    private static void Check(string label, Func<bool> body)
    {
        string detail=null; var ok=false;
        try { ok=body(); }
        catch(Exception error)
        {
            var inner=error;
            while((inner is TargetInvocationException || inner is TypeInitializationException) && inner.InnerException!=null) inner=inner.InnerException;
            detail=inner.GetType().Name+": "+inner.Message;
        }
        if(ok){ passes++; Console.WriteLine("PASS "+label); }
        else { failures++; Console.WriteLine("FAIL "+label+(detail!=null?" ["+detail+"]":"")); }
    }

    private sealed class Ins { public OpCode Op; public object Operand; public MethodBase Call; public FieldInfo Field; }

    private static List<Ins> Body(MethodBase method)
    {
        if(method==null) throw new InvalidOperationException("the method does not exist in this build");
        var found=new List<Ins>();
        foreach(var raw in (IEnumerable)read.Invoke(null,new object[]{method,null}))
        {
            var ins=new Ins();
            ins.Op=(OpCode)raw.GetType().GetField("opcode").GetValue(raw);
            ins.Operand=raw.GetType().GetField("operand").GetValue(raw);
            ins.Call=ins.Operand as MethodBase; ins.Field=ins.Operand as FieldInfo;
            found.Add(ins);
        }
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

    private static MethodBase Token(Type type, string name, int token)
    {
        foreach(var member in type.GetMember(name,MemberTypes.Method|MemberTypes.Constructor,All))
            if(((MethodBase)member).MetadataToken==token) return (MethodBase)member;
        throw new InvalidOperationException(type.Name+"."+name+" with token "+token.ToString("X8")+" does not exist");
    }

    private static bool HasFloat(List<Ins> body, float value)
    {
        foreach(var ins in body) if(ins.Op==OpCodes.Ldc_R4 && ins.Operand is float && (float)ins.Operand==value) return true;
        return false;
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
        if(value==0) return ins.Op==OpCodes.Ldc_I4_0;
        if(value==1) return ins.Op==OpCodes.Ldc_I4_1;
        return false;
    }

    private static bool IsSpeedSelect(Ins ins, Type unit)
    {
        var method=ins.Call as MethodInfo;
        if(method==null || method.DeclaringType!=typeof(System.Linq.Enumerable) || method.Name!="Select" || !method.IsGenericMethod) return false;
        var arguments=method.GetGenericArguments();
        return arguments.Length==2 && arguments[0]==unit && arguments[1]==typeof(float);
    }

    // The production transpilers take Harmony 1.2 instructions; the installed reader yields the modern type.
    private static object ToHarmony12(Type instructionType, List<Ins> body)
    {
        var list=(IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(instructionType));
        foreach(var ins in body) list.Add(Activator.CreateInstance(instructionType,new object[]{ins.Op,ins.Operand}));
        return list;
    }

    private static List<Ins> FromHarmony12(object instructions)
    {
        var found=new List<Ins>();
        foreach(var raw in (IEnumerable)instructions)
        {
            var ins=new Ins();
            ins.Op=(OpCode)raw.GetType().GetField("opcode").GetValue(raw);
            ins.Operand=raw.GetType().GetField("operand").GetValue(raw);
            ins.Call=ins.Operand as MethodBase; ins.Field=ins.Operand as FieldInfo;
            found.Add(ins);
        }
        return found;
    }

    private static readonly List<object> speedKeys=new List<object>();
    private static readonly List<float> speedValues=new List<float>();
    public static float SpeedOf<T>(T unit)
    {
        for(var i=0;i<speedKeys.Count;i++) if(ReferenceEquals(speedKeys[i],unit)) return speedValues[i];
        throw new InvalidOperationException("the selector was asked about a unit that was not selected");
    }

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
        var moveTo=native.GetType("Kingmaker.UnitLogic.Commands.UnitMoveTo",true);
        var areaTransition=native.GetType("Kingmaker.UnitLogic.Commands.UnitAreaTransition",true);
        var continuous=native.GetType("Kingmaker.UnitLogic.Commands.UnitMoveContiniously",true);
        var ground=native.GetType("Kingmaker.Controllers.Clicks.Handlers.ClickGroundHandler",true);
        var gamepad=native.GetType("Kingmaker.UI._ConsoleUI.InputLayers.InGameLayer.InGameInputLayer",true);
        var pointers=native.GetType("Kingmaker.UI.ClickPointerManager",true);
        var view=native.GetType("Kingmaker.View.UnitEntityView",true);
        var agent=native.GetType("Kingmaker.View.UnitMovementAgent",true);
        var transitionController=native.GetType("Kingmaker.UI.MapObjectOvertip.AreaTransitionController",true);
        var groupMove=Token(ground,"MoveSelectedUnitsToPoint",0x060093DB);
        var stick=Token(gamepad,"UpdateMovement",0x06005D0C);

        // ---- NATIVE ----
        Check("NATIVE exact Kingmaker Assembly-CSharp MVID",
            ()=>native.ManifestModule.ModuleVersionId==new Guid("07fa1e4d-8618-41b3-9b8d-faa17d3b26f7"));
        Check("NATIVE M01 an entrance order is a player-made UnitAreaTransition that takes the group speed limit and runs on the unit it was made for",
            ()=>{
                var body=Body(Method(transitionController,"RunUnitTransitionCommand",4));
                var made=-1;
                for(var i=0;i<body.Count;i++) if(body[i].Op==OpCodes.Newobj && body[i].Call!=null && body[i].Call.DeclaringType==areaTransition) made=i;
                var player=-1;
                for(var i=1;i<body.Count;i++) if(body[i].Op==OpCodes.Stfld && body[i].Field!=null && body[i].Field.Name=="CreatedByPlayer" && LoadsInt(body[i-1],1)) player=i;
                return made>=0 && player>made && One(body,"UnitCommand","set_SpeedLimit")>made && One(body,"UnitCommands","Run")>player;
            });
        Check("NATIVE M01 a completed new path offers AccelerateIfPathIsLong (060027B4) to every command of that unit, whatever its type",
            ()=>{
                var body=Body(Method(agent,"OnPathComplete",1));
                var offer=One(body,"UnitCommand","AccelerateIfPathIsLong");
                return body[offer].Call.MetadataToken==0x060027B4 && One(body,"UnitCommands","GetEnumerator")<offer && Count(body,null,"OfType")==0;
            });
        Check("NATIVE M01 AccelerateIfPathIsLong accelerates its own command at 12.7 m and then offers Accelerate to the commands of the selected units only",
            ()=>{
                var body=Body(Token(command,"AccelerateIfPathIsLong",0x060027B4));
                return HasFloat(body,12.7f) && Count(body,"UnitCommand","CanAccelerate")==1 && Count(body,"UnitCommand","Accelerate")==1 &&
                    One(body,"SelectionManagerBase","get_SelectedUnits")>One(body,"UnitCommand","Accelerate");
            });
        Check("NATIVE M03 the group speed limit is the minimum of the selected units' ModifiedSpeedMps, taken by one Select and one Min, floored from the constant 30",
            ()=>{
                var body=Body(groupMove);
                var selects=new List<int>();
                for(var i=0;i<body.Count;i++) if(IsSpeedSelect(body[i],unit)) selects.Add(i);
                if(selects.Count!=1) throw new InvalidOperationException("speed selections: "+selects.Count);
                var select=selects[0];
                if(!Is(body[select+1],"Enumerable","Min")) throw new InvalidOperationException("the selection is not followed by Min");
                MethodBase selector=null;
                for(var i=select-1;i>=0 && i>select-12;i--) if(body[i].Op==OpCodes.Ldftn){ selector=body[i].Call; break; }
                if(selector==null) throw new InvalidOperationException("the speed selector was not found");
                var reads=Body(selector);
                var floor=false;
                for(var i=select;i<body.Count && i<select+20;i++) if(LoadsInt(body[i],30)) floor=true;
                return Count(reads,"UnitEntityData","get_ModifiedSpeedMps")==1 && floor && One(body,"SelectionManagerBase","get_SelectedUnits")<select;
            });
        Check("NATIVE M02 the stick layer (06005D0C) accelerates the driven unit's continuous command at 0.95 and walks the selected units' Commands.Move once with Accelerate and once with Deaccelerate",
            ()=>{
                var body=Body(stick);
                var selected=One(body,"SelectionManagerBase","get_SelectedUnits");
                var accelerate=0; var deaccelerate=0; var driver=0;
                for(var i=1;i<body.Count;i++)
                {
                    if(Is(body[i],"UnitCommand","Accelerate") && Is(body[i-1],"UnitCommands","get_Move")) accelerate++;
                    if(Is(body[i],"UnitCommand","Deaccelerate") && Is(body[i-1],"UnitCommands","get_Move")) deaccelerate++;
                    if(Is(body[i],"UnitCommand","Accelerate") && Is(body[i-1],"UnitCommands","get_MoveContiniously")) driver++;
                }
                return HasFloat(body,0.95f) && Is(body[selected+1],null,"GetEnumerator") && accelerate==1 && deaccelerate==1 && driver==1 &&
                    Count(body,"InGameInputLayer","MakeCompanionsFollow")==2;
            });
        Check("NATIVE M05 the stick-driven unit is the console selection's current unit: its continuous command is initialised for it, run on its own command slots, and dereferenced at once",
            ()=>{
                var body=Body(stick);
                var current=-1;
                for(var i=0;i<body.Count;i++) if(body[i].Field!=null && body[i].Field.Name=="CurrentSelectUnit") current=i;
                var init=One(body,"UnitCommand","Init");
                var run=One(body,"UnitCommands","Run");
                return current>=0 && current<init && init<run && Count(body,"UnitCommands","get_MoveContiniously")==4;
            });
        Check("NATIVE M05 UnitCommands.Run refuses a command whose executor is another unit, and UnitMoveContiniously.Init installs the executor's AgentOverride",
            ()=>{
                var run=Body(Method(commands,"Run",3));
                var init=Body(Token(continuous,"Init",0x060026F0));
                return HasString(run,"Invalid command, can't run (unit: {0}, command: {1})") && Count(run,"UnitCommand","get_Executor")>=2 &&
                    Count(init,"UnitEntityView","set_AgentOverride")>=1;
            });
        Check("NATIVE M04 the pointer pass only shows or hides: a unit's pointer is visible while it holds a live marker move or a live UnitAreaTransition, and nothing is placed or removed there",
            ()=>{
                var body=Body(Token(pointers,"Update",0x06002D64));
                return Count(body,"ClickPointerPrefab","SetVisible")==3 && Count(body,"ClickPointerManager","AddPointer")==0 &&
                    Count(body,null,"Remove")==0 && Count(body,"UnitCommands","get_Move")==1 && Count(body,"UnitMoveTo","get_ShowTargetMarker")==1 &&
                    Count(body,"UnitCommands","GetCommand")==1;
            });
        Check("NATIVE M04 a pointer is placed by the unit's own view when its movement starts, and UnitMoveTo asks for a marker unless told otherwise",
            ()=>{
                var started=Body(Method(view,"OnMovementStarted",2));
                var show=Body(Method(ground,"ShowDestination",3));
                var agentTest=false;
                foreach(var ins in show) if(ins.Op==OpCodes.Isinst && (Type)ins.Operand==agent) agentTest=true;
                var asks=0;
                foreach(var member in moveTo.GetMember(".ctor",MemberTypes.Constructor,All))
                {
                    var body=Body((MethodBase)member);
                    for(var i=1;i<body.Count;i++)
                        if(body[i].Op==OpCodes.Stfld && body[i].Field!=null && body[i].Field.Name.Contains("ShowTargetMarker") && LoadsInt(body[i-1],1)) asks++;
                }
                return Count(started,"ClickPointerManager","AddPointer")==1 && Count(started,"UnitEntityData","get_IsDirectlyControllable")==1 &&
                    agentTest && asks==2 && One(Body(Method(agent,"OnPathComplete",1)),"UnitEntityView","OnMovementStarted")>=0;
            });
        Check("NATIVE M04 a command's approach is planned by the unit's stock agent whatever override is installed and whether or not that agent is enabled, and OnMovementStarted (0600184E) does nothing but place a pointer",
            ()=>{
                var move=Body(Method(view,"MoveTo",5));
                var plan=Body(Method(agent,"PathTo",5));
                var started=Body(Token(view,"OnMovementStarted",0x0600184E));
                foreach(var ins in plan) if(Is(ins,null,"get_enabled") || Is(ins,null,"get_isActiveAndEnabled")) throw new InvalidOperationException("PathTo tests whether the agent is enabled");
                foreach(var ins in started)
                {
                    if(ins.Op==OpCodes.Stfld) throw new InvalidOperationException("OnMovementStarted writes the field "+ins.Field.Name);
                    if(ins.Call!=null && (ins.Call.DeclaringType==commands || ins.Call.DeclaringType==command) && !ins.Call.Name.StartsWith("get_") && ins.Call.Name!="GetCommand")
                        throw new InvalidOperationException("OnMovementStarted calls "+ins.Call.Name);
                }
                return One(move,"UnitEntityView","get_AgentASP")<One(move,"UnitMovementAgent","PathTo") && Count(plan,null,"StartPath")>=2 &&
                    Count(started,"ClickPointerManager","AddPointer")==1 && Count(started,"ClickPointerManager","AddPreviewPointer")==1 &&
                    One(Body(Method(command,"TickApproaching",0)),"UnitEntityView","MoveTo")>=0;
            });

        // ---- PRODUCT ----
        var patchMethods=product.GetType("KingmakerMountedCombat.Integration.MountedPatchController+PatchMethods",true);
        var policy=product.GetType("KingmakerMountedCombat.Domain.MountedGroupPacePolicy",true);
        var combat=product.GetType("KingmakerMountedCombat.Integration.MountedCombatController",true);
        var relationship=product.GetType("KingmakerMountedCombat.Integration.GameMountedRelationshipService",true);
        var unitList=typeof(List<>).MakeGenericType(unit);
        var unitSequence=typeof(IEnumerable<>).MakeGenericType(unit);
        var speedSelector=typeof(Func<,>).MakeGenericType(unit,typeof(float));

        Check("PRODUCT M03 the group-move transpiler, run on the installed MoveSelectedUnitsToPoint, replaces exactly the speed Select with the product hook and leaves Min and everything else native",
            ()=>{
                var before=Body(groupMove);
                var transpiler=(MethodInfo)Method(patchMethods,"GroundGroupMoveTranspiler",1);
                if(transpiler==null) throw new InvalidOperationException("GroundGroupMoveTranspiler does not exist in this build");
                var after=FromHarmony12(transpiler.Invoke(null,new object[]{ToHarmony12(instruction12,before)}));
                var hook=(MethodInfo)Method(patchMethods,"GroupPaceSpeedInputs",2);
                if(after.Count!=before.Count+1) throw new InvalidOperationException("instruction count "+before.Count+" -> "+after.Count);
                var offset=0; var replaced=0; var inserted=0;
                for(var i=0;i<before.Count;i++)
                {
                    var now=after[i+offset];
                    if(now.Op==before[i].Op && Equals(now.Operand,before[i].Operand)) continue;
                    if(IsSpeedSelect(before[i],unit) && now.Op==OpCodes.Call && ReferenceEquals(now.Call,hook) && Is(after[i+offset+1],"Enumerable","Min")) { replaced++; continue; }
                    // The paired-input context call the earlier transpiler of this method inserts.
                    if(offset==0 && now.Op==OpCodes.Call && now.Call!=null && now.Call.Name=="PairedInputContext" &&
                        after[i+1].Op==before[i].Op && Equals(after[i+1].Operand,before[i].Operand)) { offset=1; inserted++; continue; }
                    throw new InvalidOperationException("unexpected change at native instruction "+i+": "+before[i].Op+" -> "+now.Op);
                }
                var parameters=hook.GetParameters();
                return replaced==1 && inserted==1 && hook.ReturnType==typeof(IEnumerable<float>) && parameters[0].ParameterType==unitSequence &&
                    parameters[1].ParameterType==speedSelector;
            });
        Check("PRODUCT M02 the gamepad transpiler, run on the installed UpdateMovement, inserts the product hook between the SelectedUnits read and the follower loop and changes nothing else",
            ()=>{
                var before=Body(stick);
                var transpiler=(MethodInfo)Method(patchMethods,"GamepadFollowerOfferTranspiler",1);
                if(transpiler==null) throw new InvalidOperationException("GamepadFollowerOfferTranspiler does not exist in this build");
                var after=FromHarmony12(transpiler.Invoke(null,new object[]{ToHarmony12(instruction12,before)}));
                var hook=(MethodInfo)Method(patchMethods,"GamepadFollowerOfferUnits",1);
                var site=One(before,"SelectionManagerBase","get_SelectedUnits");
                if(after.Count!=before.Count+1) throw new InvalidOperationException("instruction count "+before.Count+" -> "+after.Count);
                for(var i=0;i<before.Count;i++)
                {
                    var now=after[i<=site?i:i+1];
                    if(now.Op!=before[i].Op || !Equals(now.Operand,before[i].Operand)) throw new InvalidOperationException("unexpected change at native instruction "+i);
                }
                return after[site+1].Op==OpCodes.Call && ReferenceEquals(after[site+1].Call,hook) && Is(after[site+2],null,"GetEnumerator") &&
                    hook.ReturnType==unitList && hook.GetParameters()[0].ParameterType==unitList;
            });
        Check("PRODUCT a transpiler given a method that does not hold its recorded site fails instead of patching",
            ()=>{
                var transpilers=product.GetType("KingmakerMountedCombat.Integration.GroupPaceTranspilers",true);
                var refused=0;
                foreach(var attempt in new[]{new object[]{"SpeedInputs",stick,"GroupPaceSpeedInputs",2},new object[]{"GamepadFollowerOffer",groupMove,"GamepadFollowerOfferUnits",1}})
                {
                    try
                    {
                        var result=Method(transpilers,(string)attempt[0],2).Invoke(null,new object[]{ToHarmony12(instruction12,Body((MethodBase)attempt[1])),
                            Method(patchMethods,(string)attempt[2],(int)attempt[3])});
                        foreach(var ignored in (IEnumerable)result) { }
                    }
                    catch(TargetInvocationException error) { if(error.InnerException is InvalidOperationException) refused++; }
                }
                return refused==2;
            });
        Check("PRODUCT M03 the compiled speed hook, with no mounted pair, yields the native selector's value for each selected unit in the selection's order",
            ()=>{
                var selected=(IList)Activator.CreateInstance(unitList);
                speedKeys.Clear(); speedValues.Clear();
                for(var i=0;i<3;i++){ var one=FormatterServices.GetUninitializedObject(unit); selected.Add(one); speedKeys.Add(one); speedValues.Add(3f+i); }
                var selector=Delegate.CreateDelegate(speedSelector,typeof(KmcPhysicalMoverPaceProbe).GetMethod("SpeedOf").MakeGenericMethod(unit));
                var values=new List<float>();
                foreach(var value in (IEnumerable)Method(patchMethods,"GroupPaceSpeedInputs",2).Invoke(null,new object[]{selected,selector})) values.Add((float)value);
                return values.Count==3 && values[0]==3f && values[1]==4f && values[2]==5f && selected.Count==3;
            });
        Check("PRODUCT M02 the compiled follower hook, with no mounted pair, answers with the selection's own units in order and leaves the selection as it was",
            ()=>{
                var selected=(IList)Activator.CreateInstance(unitList);
                for(var i=0;i<3;i++) selected.Add(FormatterServices.GetUninitializedObject(unit));
                selected.Insert(1,null);
                var answered=(IList)Method(patchMethods,"GamepadFollowerOfferUnits",1).Invoke(null,new object[]{selected});
                if(answered.Count!=4 || selected.Count!=4) return false;
                for(var i=0;i<4;i++) if(!ReferenceEquals(answered[i],selected[i])) return false;
                return true;
            });
        Check("PRODUCT the compiled actor projection replaces a unit by its mover, lists each actor once in order and keeps a null entry",
            ()=>{
                var project=product.GetType("KingmakerMountedCombat.Domain.MountedPaceActors",true).GetMethod("Project").MakeGenericMethod(typeof(string));
                Func<string,string> mover=name=>name=="rider"?"mount":(name=="rider2"?"mount2":null);
                var actors=(List<string>)project.Invoke(null,new object[]{new List<string>{"a","rider",null,"mount","rider2","b"},mover});
                return string.Join(",",actors.ConvertAll(a=>a??"<null>").ToArray())=="a,mount,<null>,mount2,b";
            });
        Check("PRODUCT M01 the compiled classification knows the routed ground order and the carried entrance order, and nothing else",
            ()=>{
                var classify=policy.GetMethod("ClassifyOrder");
                if(classify==null) throw new InvalidOperationException("ClassifyOrder does not exist in this build");
                Func<bool[],string> of=flags=>{ var args=new object[flags.Length]; for(var i=0;i<flags.Length;i++) args[i]=flags[i]; return classify.Invoke(null,args).ToString(); };
                // executorIsMount, createdByPlayer, aiOrigin, exactGroundMove, boundRoutedGroundOrder, exactAreaTransition, boundEntranceCarrier, riderHoldsMemberCommandOfSameGroup
                return of(new[]{true,true,false,true,true,false,false,false})=="RoutedGroundOrder" &&
                    of(new[]{true,true,false,false,false,true,true,true})=="CarriedEntranceOrder" &&
                    of(new[]{true,true,false,true,false,false,false,false})=="None" &&
                    of(new[]{true,true,false,false,true,false,false,false})=="None" &&
                    of(new[]{true,true,false,false,false,true,false,true})=="None" &&
                    of(new[]{true,true,false,false,false,true,true,false})=="None" &&
                    of(new[]{true,true,false,false,false,false,true,true})=="None" &&
                    of(new[]{true,true,true,false,false,true,true,true})=="None" &&
                    of(new[]{true,false,false,false,false,true,true,true})=="None" &&
                    of(new[]{false,true,false,false,false,true,true,true})=="None";
            });
        Check("PRODUCT M02 and M03 the compiled follower and speed decisions answer with the mount only for a mounted pair's rider outside turn-based combat",
            ()=>{
                var follower=policy.GetMethod("DecideGamepadFollower");
                var speed=policy.GetMethod("SpeedInputIsMount");
                if(follower==null || speed==null) throw new InvalidOperationException("the decisions do not exist in this build");
                Func<bool[],string> f=flags=>{ var args=new object[flags.Length]; for(var i=0;i<flags.Length;i++) args[i]=flags[i]; return follower.Invoke(null,args).ToString(); };
                Func<bool[],bool> s=flags=>{ var args=new object[flags.Length]; for(var i=0;i<flags.Length;i++) args[i]=flags[i]; return (bool)speed.Invoke(null,args); };
                // pairMounted, turnBasedCombat, unitIsRider, mountSelected, mountExecutesRoutedGroundOrder, orderFinished
                return f(new[]{true,false,true,false,true,false})=="OfferToMount" && f(new[]{false,false,true,false,true,false})=="KeepSelectedUnit" &&
                    f(new[]{true,true,true,false,true,false})=="KeepSelectedUnit" && f(new[]{true,false,false,false,true,false})=="KeepSelectedUnit" &&
                    f(new[]{true,false,true,true,true,false})=="KeepSelectedUnit" && f(new[]{true,false,true,false,false,false})=="KeepSelectedUnit" &&
                    f(new[]{true,false,true,false,true,true})=="KeepSelectedUnit" &&
                    // pairMounted, turnBasedCombat, unitIsRider, mountAvailable
                    s(new[]{true,false,true,true}) && !s(new[]{false,false,true,true}) && !s(new[]{true,true,true,true}) &&
                    !s(new[]{true,false,false,true}) && !s(new[]{true,false,true,false});
            });
        Check("PRODUCT M01 the carry classifies the mount's Move-slot command and applies native Accelerate once: no limit, gait, speed or agent write",
            ()=>{
                var carry=Body(Method(combat,"CarryNativeGroupAcceleration",0));
                var classify=Body(Method(combat,"ClassifyMountGroupOrder",3));
                foreach(var ins in carry)
                    if(Is(ins,null,"set_SpeedLimit") || Is(ins,null,"set_MovementType") || Is(ins,null,"Deaccelerate") || Is(ins,null,"set_OverrideSpeed") ||
                        Is(ins,null,"set_MaxSpeedOverride") || Is(ins,"UnitCommands","Run") || Is(ins,null,"Interrupt")) throw new InvalidOperationException("the carry calls "+ins.Call.Name);
                // Both identities are asked with the pair generation, and the carrier with the group of the command
                // in the mount's slot (the group is read before the generation that precedes the question).
                var carrierAsked=One(classify,"MountedEntranceCarrierWatch","IsCurrent");
                var groupRead=-1;
                for(var i=0;i<carrierAsked;i++) if(classify[i].Op==OpCodes.Ldfld && classify[i].Field!=null && classify[i].Field.Name=="GroupCommand") groupRead=i;
                return One(carry,"MountedCombatController","ClassifyMountGroupOrder")<One(carry,"MountedGroupPacePolicy","Decide") &&
                    One(carry,"MountedGroupPacePolicy","Decide")<One(carry,"UnitCommand","Accelerate") &&
                    Count(classify,"MountedGroupPacePolicy","ClassifyOrder")==1 && Count(classify,"MountedPlayerMoveWatch","IsCurrent")==1 &&
                    Count(classify,"UnitCommands","GetCommand")==2 && Count(classify,"GameMountedRelationshipService","get_MountedPairGeneration")==2 &&
                    Is(classify[carrierAsked-1],"GameMountedRelationshipService","get_MountedPairGeneration") && groupRead>=0 &&
                    Is(classify[One(classify,"MountedPlayerMoveWatch","IsCurrent")-1],"GameMountedRelationshipService","get_MountedPairGeneration");
            });
        Check("PRODUCT M02 and M03 the follower answer and the speed mover only decide: neither runs, interrupts, accelerates or writes a command",
            ()=>{
                var follower=Body(Method(combat,"GamepadFollowerOfferMover",2));
                var speed=Body(Method(relationship,"GroupPaceSpeedMover",1));
                foreach(var body in new[]{follower,speed})
                    foreach(var ins in body)
                        if(Is(ins,null,"Accelerate") || Is(ins,null,"Deaccelerate") || Is(ins,null,"Run") || Is(ins,null,"Interrupt") || Is(ins,null,"set_SpeedLimit") ||
                            Is(ins,null,"set_MovementType")) throw new InvalidOperationException("a decision calls "+ins.Call.Name);
                // The follower pass answers for the routed ground order only (the classification compared for equality
                // with RoutedGroundOrder = 1, never merely "some order"), and the speed mover tests the unit against the
                // pair's rider and answers with the pair's mount.
                var decide=One(follower,"MountedGroupPacePolicy","DecideGamepadFollower");
                var routedOnly=false;
                for(var i=1;i<decide;i++) if(follower[i].Op==OpCodes.Ceq && LoadsInt(follower[i-1],1)) routedOnly=true;
                for(var i=1;i<decide;i++) if(follower[i].Op==OpCodes.Cgt_Un && LoadsInt(follower[i-1],0) && !Is(follower[i-2],null,"get_AiAction"))
                {
                    // "kind != None" compiles to this shape; object null tests load ldnull, not an integer.
                    throw new InvalidOperationException("the follower answer accepts any classified order");
                }
                var riderRead=One(speed,null,"get_Rider");
                return routedOnly && Count(follower,"MountedCombatController","ClassifyMountGroupOrder")==1 &&
                    Count(speed,"MountedGroupPacePolicy","SpeedInputIsMount")==1 && speed[riderRead+1].Op==OpCodes.Ceq &&
                    riderRead<One(speed,"MountedGroupPacePolicy","SpeedInputIsMount") && Count(speed,null,"get_Mount")==2;
            });
        Check("PRODUCT M01 the entrance carrier is issued as before (one command, one Run, after the carry decision), only then bound for the offer, and let go by the controller's update",
            ()=>{
                var body=Body(Method(combat,"CarryRiderEntranceTransition",2));
                var made=0;
                foreach(var ins in body) if(ins.Op==OpCodes.Newobj && ins.Call!=null && ins.Call.DeclaringType==areaTransition) made++;
                var run=One(body,"UnitCommands","Run");
                var update=Body(Method(combat,"Update",0));
                var release=Body(Method(combat,"ReleaseEntranceCarrier",0));
                return made==1 && One(body,"MountedEntranceCarryPolicy","Decide")<run && One(body,"MountedEntranceCarrierWatch","Bind")>run &&
                    Count(body,null,"Accelerate")==0 && Count(body,null,"get_IsAccelerated")==0 &&
                    Count(update,"MountedCombatController","ReleaseEntranceCarrier")==1 && Count(release,"MountedEntranceCarrierWatch","ReleaseIfEnded")==1;
            });
        Check("PRODUCT M04 the rider's old pointer is retired after the carrier ran, by the policy, entry first and object second, and nothing else in the product changes the native pointer table",
            ()=>{
                var entrance=Body(Method(combat,"CarryRiderEntranceTransition",2));
                var markers=product.GetType("KingmakerMountedCombat.Integration.MountedDestinationMarkers",true);
                var retire=Body(Method(markers,"RetireRiderPointer",2));
                var writers=0;
                foreach(var type in product.GetTypes())
                    foreach(var member in type.GetMembers(All))
                    {
                        var method=member as MethodBase;
                        if(method==null || method.IsAbstract || method.GetMethodBody()==null) continue;
                        List<Ins> body;
                        try { body=Body(method); } catch(Exception) { continue; }
                        // A diagnostic scenario reads the table; only a method that removes, adds, sets or places counts here.
                        var tableWrite=false;
                        foreach(var ins in body)
                            if(ins.Call!=null && ins.Call.DeclaringType.IsGenericType && ins.Call.DeclaringType.GetGenericTypeDefinition()==typeof(Dictionary<,>) &&
                                ins.Call.DeclaringType.GetGenericArguments()[0]==unit &&
                                (ins.Call.Name=="Remove" || ins.Call.Name=="Clear" || ins.Call.Name=="set_Item" || ins.Call.Name=="Add")) tableWrite=true;
                        if(Count(body,"ClickPointerManager","AddPointer")>0 || Count(body,"ClickPointerManager","AddPreviewPointer")>0 ||
                            (Count(body,"ClickPointerManager","get_UnitMarks")>0 && tableWrite)) writers++;
                    }
                if(writers!=1) throw new InvalidOperationException("methods that change the native pointer table: "+writers);
                return One(entrance,"MountedDestinationMarkers","RetireRiderPointer")>One(entrance,"UnitCommands","Run") &&
                    One(retire,"MountedDestinationMarkerPolicy","RetireRiderPointer")<One(retire,null,"Remove") &&
                    One(retire,null,"Remove")<One(retire,"Object","Destroy");
            });
        Check("PRODUCT M04 the compiled marker decisions: retire needs an admitted carrier and an existing pointer; the rider's placement is skipped only for a mounted pair's rider whose command the mount carries",
            ()=>{
                var markerPolicy=product.GetType("KingmakerMountedCombat.Domain.MountedDestinationMarkerPolicy",true);
                var decide=markerPolicy.GetMethod("RetireRiderPointer");
                var skip=markerPolicy.GetMethod("CarriedRiderPlacesNoPointer");
                if(skip==null) throw new InvalidOperationException("CarriedRiderPlacesNoPointer does not exist in this build");
                return (bool)decide.Invoke(null,new object[]{true,true}) && !(bool)decide.Invoke(null,new object[]{false,true}) &&
                    !(bool)decide.Invoke(null,new object[]{true,false}) && !(bool)decide.Invoke(null,new object[]{false,false}) &&
                    (bool)skip.Invoke(null,new object[]{true,true,true}) && !(bool)skip.Invoke(null,new object[]{false,true,true}) &&
                    !(bool)skip.Invoke(null,new object[]{true,false,true}) && !(bool)skip.Invoke(null,new object[]{true,true,false});
            });
        Check("PRODUCT M04 the placement prefix decides through the carry classification and the policy, writes nothing, and with no pair lets native place the pointer",
            ()=>{
                var skip=Body(Method(combat,"SkipCarriedRiderPointer",1));
                foreach(var ins in skip)
                    if(Is(ins,null,"Accelerate") || Is(ins,null,"Run") || Is(ins,null,"Interrupt") || Is(ins,null,"Remove") || Is(ins,null,"Destroy") ||
                        Is(ins,null,"AddPointer") || Is(ins,null,"SetVisible")) throw new InvalidOperationException("the decision calls "+ins.Call.Name);
                var prefix=(MethodInfo)Method(patchMethods,"CarriedRiderPointerPrefix",1);
                var uninitialised=FormatterServices.GetUninitializedObject(view);
                return Count(skip,"MountedDestinationMarkerPolicy","CarriedRiderPlacesNoPointer")==1 && Count(skip,"MountedCombatController","ClassifyMountGroupOrder")==1 &&
                    prefix.ReturnType==typeof(bool) && (bool)prefix.Invoke(null,new object[]{null}) && (bool)prefix.Invoke(null,new object[]{uninitialised});
            });
        Check("PRODUCT M04 the scene regroup walk is made by one factory that asks for no destination marker",
            ()=>{
                var persistence=product.GetType("KingmakerMountedCombat.Integration.MountedPersistenceService",true);
                var factory=Method(persistence,"NewRegroupMove",1);
                if(factory==null) throw new InvalidOperationException("NewRegroupMove does not exist in this build");
                var body=Body(factory);
                var marker=One(body,"UnitMoveTo","set_ShowTargetMarker");
                var regroup=Body(Method(persistence,"StartRegroup",5));
                var direct=0;
                foreach(var ins in regroup) if(ins.Op==OpCodes.Newobj && ins.Call!=null && ins.Call.DeclaringType==moveTo) direct++;
                return LoadsInt(body[marker-1],0) && direct==0 && Count(regroup,"MountedPersistenceService","NewRegroupMove")==1;
            });
        Check("PRODUCT patch installation names its sites by exact token: MoveSelectedUnitsToPoint (060093DB) and InGameInputLayer.UpdateMovement (06005D0C), each through the transpiler that composes this sprint's with the continuation's, and UnitEntityView.OnMovementStarted (0600184E)",
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
                        var token=name=="UpdateMovement"?0x06005D0C:(name=="MoveSelectedUnitsToPoint"?0x060093DB:(name=="OnMovementStarted"?0x0600184E:0));
                        var transpiler=name=="UpdateMovement"?"GamepadMovementTranspiler":(name=="OnMovementStarted"?"CarriedRiderPointerPrefix":"GroundOrderTranspiler");
                        if(token==0) continue;
                        var pinned=false; var wired=false;
                        // Up to the next patch registration: the parameter-type array of a registration lies between.
                        for(var j=i+1;j<body.Count && j<i+26;j++)
                        {
                            if(Is(body[j],"MountedPatchController","PatchExact")) break;
                            if(LoadsInt(body[j],token)) pinned=true;
                            if(body[j].Op==OpCodes.Ldstr && (string)body[j].Operand==transpiler) wired=true;
                        }
                        if(pinned && wired) found++;
                    }
                }
                // The composed transpilers contain this sprint's: the follower hook and the speed hook.
                var composedStick=Body(Method(patchMethods,"GamepadMovementTranspiler",1));
                var composedGround=Body(Method(patchMethods,"GroundOrderTranspiler",1));
                return found==3 && Count(composedStick,null,"GamepadFollowerOfferTranspiler")==1 && Count(composedGround,null,"GroundGroupMoveTranspiler")==1;
            });
        Console.WriteLine("PHYSICAL MOVER PACE CONTRACT PASS="+passes+" FAIL="+failures+"; installed-assembly and compiled-product contracts, not native gameplay evidence");
        return failures==0?0:1;
    }
}
'@
try { $kmcCode=[KmcPhysicalMoverPaceProbe]::Run($kmcManaged,$kmcDll) }
catch { Write-Output $_.Exception.ToString(); exit 1 }
exit $kmcCode
