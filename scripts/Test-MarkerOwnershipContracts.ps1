[CmdletBinding()]
param([ValidateSet('Debug','Release')][string]$Configuration='Release',[string]$ProductDll)
# Known issues M04 closeout: destination-marker ownership.
#   NATIVE-IL         the installed Assembly-CSharp is READ (whole-assembly scans where stated): the pointer table, its only
#                     placement and visibility rules, every native creator of a marker-capable command, the unit-click
#                     cannot-attack approach and its complete setup up to its Run, and the scene's own stop of its move.
#   NATIVE-EXECUTED   the installed UnitMoveTo(Vector3, float) constructor is RUN detached, and the state it leaves is read.
#   PRODUCT-IL        the compiled product is READ: every product site that builds a marker-capable command and what it
#                     asks for, the click-approach routing, the ground order's identity of that command, the AddPointer
#                     placement record and the diagnostic cleanup wiring.
#   PRODUCT-EXECUTED  the compiled routing decision, marker constants, placement ledger and ownership classifier are RUN, and
#                     the product's cannot-attack transpiler is RUN against the installed ClickUnitHandler.OnClick IL.
# -ProductDll reads another build (the accepted Sprint 4 build 14f1b474 and the reviewed b1a88b4e build must FAIL the
# PRODUCT groups they predate). Not native gameplay evidence.
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
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

public static class KmcMarkerProbe
{
    private static int passes, failures;
    private static MethodInfo read;
    private const BindingFlags All=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly;
    private const BindingFlags Any=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;

    private static void Check(string label, Func<bool> body)
    {
        string detail=null; var ok=false;
        try { ok=body(); }
        catch(Exception error) { var inner=error; while(inner is TargetInvocationException && inner.InnerException!=null) inner=inner.InnerException; detail=inner.GetType().Name+": "+inner.Message; }
        if(ok){ passes++; Console.WriteLine("PASS "+label); }
        else { failures++; Console.WriteLine("FAIL "+label+(detail!=null?" ["+detail+"]":"")); }
    }
    private sealed class Ins { public OpCode Op; public object Operand; public MethodBase Call; }
    private static List<Ins> Body(MethodBase method)
    {
        if(method==null) throw new InvalidOperationException("the method does not exist in this build");
        var found=new List<Ins>();
        foreach(var raw in (IEnumerable)read.Invoke(null,new object[]{method,null}))
        {
            var ins=new Ins(); ins.Op=(OpCode)raw.GetType().GetField("opcode").GetValue(raw); ins.Operand=raw.GetType().GetField("operand").GetValue(raw);
            ins.Call=ins.Operand as MethodBase; found.Add(ins);
        }
        return found;
    }
    private static List<int> Find(List<Ins> body, string type, string name)
    {
        var found=new List<int>();
        for(var i=0;i<body.Count;i++) if(body[i].Call!=null && body[i].Call.Name==name && (type==null || body[i].Call.DeclaringType.Name==type)) found.Add(i);
        return found;
    }
    private static MethodBase Token(Type type, string name, int token)
    {
        foreach(var member in type.GetMember(name,MemberTypes.Method,All)) if(((MethodBase)member).MetadataToken==token) return (MethodBase)member;
        throw new InvalidOperationException(type.Name+"."+name+" with token "+token.ToString("X8")+" does not exist");
    }
    private static bool Registers(Type controller, string typeName, int token, string prefix, string postfix)
    {
        foreach(var member in controller.GetMembers(All))
        {
            var method=member as MethodBase; if(method==null || method.GetMethodBody()==null) continue;
            var body=Body(method);
            for(var j=0;j<body.Count;j++)
            {
                if(!(body[j].Op==OpCodes.Ldc_I4 && (int)body[j].Operand==token)) continue;
                var strings=new List<string>();
                // A registration with a long parameter-type array puts the hook names further from the token.
                for(var k=j;k<body.Count && k<j+80;k++) if(body[k].Op==OpCodes.Ldstr) strings.Add((string)body[k].Operand);
                var typed=false;
                for(var k=Math.Max(0,j-8);k<j;k++) if(body[k].Op==OpCodes.Ldtoken && body[k].Operand is Type && ((Type)body[k].Operand).FullName==typeName) typed=true;
                if(typed && strings.Contains(prefix) && (postfix==null || strings.Contains(postfix))) return true;
            }
        }
        return false;
    }

    // The hook names one exact registration passes (the strings after its token up to its PatchExact call).
    private static List<string> Hooks(Type controller, string typeName, int token)
    {
        foreach(var member in controller.GetMembers(All))
        {
            var method=member as MethodBase; if(method==null || method.GetMethodBody()==null) continue;
            var body=Body(method);
            for(var j=0;j<body.Count;j++)
            {
                if(!(body[j].Op==OpCodes.Ldc_I4 && (int)body[j].Operand==token)) continue;
                var typed=false;
                for(var k=Math.Max(0,j-8);k<j;k++) if(body[k].Op==OpCodes.Ldtoken && body[k].Operand is Type && ((Type)body[k].Operand).FullName==typeName) typed=true;
                if(!typed) continue;
                var names=new List<string>();
                for(var k=j+1;k<body.Count;k++) { if(body[k].Call!=null && body[k].Call.Name=="PatchExact") return names; if(body[k].Op==OpCodes.Ldstr) names.Add((string)body[k].Operand); }
            }
        }
        return null;
    }

    private static Type[] TypesOf(Assembly assembly) { try { return assembly.GetTypes(); } catch(ReflectionTypeLoadException partial) { return partial.Types.Where(t=>t!=null).ToArray(); } }

    // Every method of an assembly whose IL uses one of the given metadata tokens with one of the given opcodes, as "Type.Method".
    private static List<string> Uses(Assembly assembly, IEnumerable<int> tokens, params byte[] opcodes)
    {
        var wanted=new HashSet<int>(tokens); var found=new List<string>();
        foreach(var type in TypesOf(assembly))
            foreach(var member in type.GetMembers(All))
            {
                var method=member as MethodBase; if(method==null) continue;
                byte[] il; try { var body=method.GetMethodBody(); if(body==null) continue; il=body.GetILAsByteArray(); } catch { continue; }
                for(var i=0;i+4<il.Length;i++)
                    if(Array.IndexOf(opcodes,il[i])>=0 && wanted.Contains(BitConverter.ToInt32(il,i+1))) { found.Add(type.FullName+"."+method.Name); break; }
            }
        return found;
    }

    // Every construction of a marker-capable command (UnitMoveTo and its subclasses, UnitAreaTransition) in an assembly,
    // with what it asks for: "Type.Method:Command:marker".
    private static List<string> MarkerSites(Assembly assembly, Type move, Type transition)
    {
        var found=new List<string>();
        foreach(var type in TypesOf(assembly))
            foreach(var member in type.GetMembers(All))
            {
                var method=member as MethodBase; if(method==null || method.GetMethodBody()==null) continue;
                List<Ins> body; try { body=Body(method); } catch { continue; }
                for(var i=0;i<body.Count;i++)
                {
                    if(body[i].Op!=OpCodes.Newobj || body[i].Call==null) continue;
                    var built=body[i].Call.DeclaringType;
                    if(!(move.IsAssignableFrom(built) || transition.IsAssignableFrom(built))) continue;
                    var marker=transition.IsAssignableFrom(built) ? "transition" : "default";
                    for(var k=i+1;k<body.Count && k<i+40;k++)
                    {
                        if(body[k].Op==OpCodes.Newobj && body[k].Call!=null && (move.IsAssignableFrom(body[k].Call.DeclaringType) || transition.IsAssignableFrom(body[k].Call.DeclaringType))) break;
                        if(body[k].Call!=null && body[k].Call.Name=="set_ShowTargetMarker")
                        {
                            var value=body[k-1];
                            marker=value.Op==OpCodes.Ldc_I4_0 ? "false" : value.Op==OpCodes.Ldc_I4_1 ? "true" :
                                value.Call!=null && value.Call.Name=="get_ShowTargetMarker" ? "copied" : value.Op.Name.StartsWith("ldarg") ? "parameter" : "computed";
                            break;
                        }
                    }
                    found.Add(type.FullName.Replace('+','/')+"."+method.Name+":"+built.Name+":"+marker);
                }
            }
        return found;
    }

    public static int Run(string managed, string productPath)
    {
        AppDomain.CurrentDomain.AssemblyResolve+=(s,a)=>{
            var leaf=new AssemblyName(a.Name).Name+".dll";
            foreach(var folder in new[]{managed,Path.Combine(managed,"UnityModManager")}){ var p=Path.Combine(folder,leaf); if(File.Exists(p)) return Assembly.LoadFrom(p); }
            return null;
        };
        var native=Assembly.LoadFrom(Path.Combine(managed,"Assembly-CSharp.dll"));
        var product=Assembly.LoadFrom(productPath);
        Console.WriteLine("INFO product "+productPath+" MVID "+product.ManifestModule.ModuleVersionId);
        var modern=Assembly.LoadFrom(Path.Combine(managed,"UnityModManager/0Harmony.dll"));
        foreach(var method in modern.GetType("HarmonyLib.PatchProcessor",true).GetMethods(BindingFlags.Public|BindingFlags.Static))
            if(method.Name=="GetOriginalInstructions" && method.GetParameters().Length==2 && !method.GetParameters()[1].ParameterType.IsByRef) read=method;

        var manager=native.GetType("Kingmaker.UI.ClickPointerManager",true);
        var view=native.GetType("Kingmaker.View.UnitEntityView",true);
        var agent=native.GetType("Kingmaker.View.UnitMovementAgent",true);
        var move=native.GetType("Kingmaker.UnitLogic.Commands.UnitMoveTo",true);
        var transition=native.GetType("Kingmaker.UnitLogic.Commands.UnitAreaTransition",true);
        var unitClick=native.GetType("Kingmaker.Controllers.Clicks.Handlers.ClickUnitHandler",true);
        var moveUnit=native.GetType("Kingmaker.AreaLogic.Cutscenes.Commands.CommandMoveUnit",true);
        var marksField=manager.GetField("m_UnitMarks",Any);
        var addPointer=manager.GetMethod("AddPointer"); var started=Token(view,"OnMovementStarted",0x0600184E);

        Check("NATIVE-IL the pointer table: m_UnitMarks (one pointer per unit) is touched only by its initializer, AddPointer, Update and the UnitMarks getter, whole assembly; no ClickPointerManager method removes or clears an entry; the getter's only callers are the three local-map readers",
            ()=>{
                var touched=Uses(native,new[]{marksField.MetadataToken},0x7B,0x7C,0x7D);
                var getters=Uses(native,new[]{manager.GetProperty("UnitMarks").GetGetMethod().MetadataToken},0x28,0x6F);
                var removes=0; foreach(var m in manager.GetMethods(All)) if(m.GetMethodBody()!=null) removes+=Find(Body(m),null,"Remove").Count+Find(Body(m),null,"Clear").Count;
                Console.WriteLine("INFO table touched by "+string.Join(", ",touched.ToArray())+"; getter read by "+string.Join(", ",getters.ToArray()));
                var expected=new HashSet<string>(StringComparer.Ordinal){"Kingmaker.UI.ClickPointerManager..ctor","Kingmaker.UI.ClickPointerManager.AddPointer","Kingmaker.UI.ClickPointerManager.Update","Kingmaker.UI.ClickPointerManager.get_UnitMarks"};
                return touched.Count==4 && expected.SetEquals(touched) && removes==0 && getters.Count==3 && getters.TrueForAll(g=>g.Contains("LocalMap"));
            });
        Check("NATIVE-IL placement: AddPointer's only caller is UnitEntityView.OnMovementStarted (0600184E), whose callers are UnitMovementAgent.OnPathComplete and ClickGroundHandler.ShowDestination's path callback; FollowPrecomputedPath completes through OnPathComplete; OnMovementStarted places only for a directly controllable unit whose move asks for a marker or that holds a UnitAreaTransition, outside Cutscene mode",
            ()=>{
                var adds=Uses(native,new[]{addPointer.MetadataToken},0x28,0x6F);
                var starts=Uses(native,new[]{started.MetadataToken},0x28,0x6F);
                var follow=Body(agent.GetMethod("FollowPrecomputedPath")); var complete=Body(agent.GetMethod("OnPathComplete",Any)); var s=Body(started);
                Console.WriteLine("INFO placement callers "+string.Join(", ",starts.ToArray()));
                return adds.Count==1 && adds[0]=="Kingmaker.View.UnitEntityView.OnMovementStarted" && starts.Count==2 &&
                    starts.Exists(n=>n=="Kingmaker.View.UnitMovementAgent.OnPathComplete") && starts.Exists(n=>n.Contains("ClickGroundHandler") && n.Contains("ShowDestination")) &&
                    Find(follow,null,"OnPathComplete").Count==1 && Find(complete,"UnitEntityView","OnMovementStarted").Count==1 &&
                    Find(s,null,"get_IsDirectlyControllable").Count==1 && Find(s,null,"get_ShowTargetMarker").Count==1 && Find(s,null,"GetCommand").Count==1 &&
                    Find(s,null,"get_CurrentMode").Count==1 && Find(s,"ClickPointerManager","AddPointer").Count==1 && Find(s,"ClickPointerManager","AddPreviewPointer").Count==1;
            });
        Check("NATIVE-IL visibility: ClickPointerManager.Update shows a pointer only while its unit holds a live UnitMoveTo with ShowTargetMarker (Move slot, or the first queued) or a live UnitAreaTransition; full when the unit is selected, half otherwise; nothing ties the pointer to the command it was placed for",
            ()=>{
                var u=Body(manager.GetMethod("Update",Any));
                return Find(u,null,"get_Move").Count==1 && Find(u,null,"OfType").Count==1 && Find(u,null,"GetCommand").Count==1 && Find(u,null,"get_IsFinished").Count==2 &&
                    Find(u,null,"get_ShowTargetMarker").Count==1 && Find(u,null,"Contains").Count==1 && Find(u,"ClickPointerPrefab","SetVisible").Count==3 &&
                    Find(u,null,"get_position").Count==0 && Find(u,null,"get_localPosition").Count==0;
            });
        Check("NATIVE-IL every UnitMoveTo constructor sets ShowTargetMarker to true (a UnitMoveTo asks for a marker unless told otherwise)",
            ()=>{
                var backing=move.GetField("<ShowTargetMarker>k__BackingField",Any);
                var ok=true; var n=0;
                foreach(var c in move.GetConstructors(All)){ n++; var b=Body(c); var set=false; for(var i=1;i<b.Count;i++) if(b[i].Op==OpCodes.Stfld && b[i].Operand is FieldInfo && ((FieldInfo)b[i].Operand).MetadataToken==backing.MetadataToken && b[i-1].Op==OpCodes.Ldc_I4_1) set=true; ok&=set; }
                return n>=2 && ok;
            });
        Check("NATIVE-IL the complete set of native creators of marker-capable commands (newobj of UnitMoveTo, its subclasses or UnitAreaTransition, whole assembly) is the reviewed nineteen",
            ()=>{
                var sites=MarkerSites(native,move,transition).Select(s=>s.Substring(0,s.IndexOf(':'))).Distinct().ToList();
                var expected=new HashSet<string>(StringComparer.Ordinal){
                    "Kingmaker.UI.MapObjectOvertip.AreaTransitionController.RunUnitTransitionCommand","Kingmaker.UI._ConsoleUI.Overtips.EntityOvertipVM.RunUnitTransitionCommand",
                    "ImpactsOnSpawn.MoveToPlayer","Kingmaker.AreaLogic.Cutscenes.Commands.CommandMoveParty.OnRun","Kingmaker.AreaLogic.Cutscenes.Commands.CommandMoveUnit.OnRun",
                    "Kingmaker.Controllers.Brain.Blueprints.BlueprintAiCastSpell.CreateCommand","Kingmaker.Controllers.Brain.Blueprints.BlueprintAiSwitchWeapon.CreateCommand",
                    "Kingmaker.Controllers.Brain.Blueprints.BlueprintAiTouch.CreateCommand","Kingmaker.Controllers.Clicks.Handlers.ClickGroundHandler.RunCommand",
                    "Kingmaker.Controllers.Clicks.Handlers.ClickUnitHandler.OnClick","Kingmaker.Controllers.Combat.UnitCombatState.ReturnToStartingPositionIfNeeded",
                    "Kingmaker.Controllers.Units.SummonedUnitsController.TickUnitFollowMaster","Kingmaker.Controllers.Units.UnitFearController.TickOnUnit",
                    "Kingmaker.Controllers.Units.UnitRoamingController.TickOnUnit","Kingmaker.Designers.EventConditionActionSystem.Actions.RunCommand.RunAction",
                    "Kingmaker.Test.WaypointMovement.Update","Kingmaker.View.MapObjects.CampPlaceView.MoveToRandomBuildingPosition",
                    "Kingmaker.View.MapObjects.CampPositionsSelector.MoveParty","WolfTrigger.WolfGoTo1"};
                var extra=sites.Where(s=>!expected.Contains(s)).ToList(); var missing=expected.Where(s=>!sites.Contains(s)).ToList();
                Console.WriteLine("INFO native creators "+sites.Count+"; unexpected="+string.Join(", ",extra.ToArray())+"; missing="+string.Join(", ",missing.ToArray()));
                return extra.Count==0 && missing.Count==0;
            });
        Check("NATIVE-IL ClickUnitHandler.OnClick (060093ED) builds exactly one UnitMoveTo, in its branch for a selected unit with UnitCondition.CanNotAttack, and runs it on that unit's own commands outside the ground-click handler",
            ()=>{
                var b=Body(Token(unitClick,"OnClick",0x060093ED));
                var news=new List<int>(); for(var i=0;i<b.Count;i++) if(b[i].Op==OpCodes.Newobj && b[i].Call!=null && b[i].Call.DeclaringType==move) news.Add(i);
                var condition=(int)Enum.Parse(native.GetType("Kingmaker.UnitLogic.UnitCondition",true),"CanNotAttack");
                var has=Find(b,null,"HasCondition"); var guarded=false;
                foreach(var h in has) for(var k=Math.Max(0,h-3);k<h;k++) if((b[k].Op==OpCodes.Ldc_I4_S || b[k].Op==OpCodes.Ldc_I4) && Convert.ToInt32(b[k].Operand)==condition && news.Count==1 && h<news[0]) guarded=true;
                return news.Count==1 && guarded && Find(b,"UnitCommands","Run").Count>=1 && Find(b,"ClickGroundHandler","RunCommand").Count==0;
            });
        var onClick=Token(unitClick,"OnClick",0x060093ED);
        Check("NATIVE-IL the cannot-attack command's complete setup (review of b1a88b4e, R2): new UnitMoveTo(target unit's Position, target Corpulence * 1.1) is stored in the click command's local, which nothing reads or configures until the one UnitCommands.Run of that local (no field, property or call on it in between); the request event in between (OnClick b__1) passes only the clicking unit and the target to IClickActionHandler.OnAttackRequested from a closure holding no command; the local is otherwise filled only by UnitUseAbility.CreateCastCommand and UnitAttack.CreateAttackCommand, which build only UnitUseAbility and UnitAttack; native marks the loot and interact commands it runs elsewhere in OnClick CreatedByPlayer, never this one; ManualTarget follows the Run",
            ()=>{
                var b=Body(onClick);
                var ctor=-1; for(var i=0;i<b.Count;i++) if(b[i].Op==OpCodes.Newobj && b[i].Call!=null && b[i].Call.DeclaringType==move) ctor=i;
                var local=LocalIndex(b[ctor+1]);
                var stores=new List<int>(); var loads=new List<int>();
                for(var i=0;i<b.Count;i++){ if(IsStore(b[i]) && LocalIndex(b[i])==local) stores.Add(i); if(IsLoad(b[i]) && LocalIndex(b[i])==local) loads.Add(i); }
                var runs=Find(b,"UnitCommands","Run"); var run=runs.Where(r=>IsLoad(b[r-1]) && LocalIndex(b[r-1])==local).ToList();
                var between=loads.Concat(stores).Where(i=>i>ctor+1 && i<run[0]-1).Count();
                var sources=stores.Select(i=>b[i-1].Call==null?"?":b[i-1].Call.DeclaringType.Name+"::"+b[i-1].Call.Name).OrderBy(x=>x,StringComparer.Ordinal).ToArray();
                var ctorArgs=b[ctor].Call.GetParameters().Select(x=>x.ParameterType.Name).ToArray();
                var sized=b[ctor-1].Op==OpCodes.Mul && b[ctor-2].Op==OpCodes.Ldc_R4 && Math.Abs((float)b[ctor-2].Operand-1.1f)<1e-6 && b[ctor-3].Call!=null && b[ctor-3].Call.Name=="get_Corpulence";
                var display=unitClick.GetNestedType("<>c__DisplayClass2_1",Any);
                var holdsCommand=display.GetFields(Any).Any(f=>typeof(object).IsAssignableFrom(f.FieldType) && native.GetType("Kingmaker.UnitLogic.Commands.Base.UnitCommand",true).IsAssignableFrom(f.FieldType));
                var request=Body(display.GetMethod("<OnClick>b__1",Any));
                var factories=new[]{ native.GetType("Kingmaker.UnitLogic.Commands.UnitUseAbility",true).GetMethod("CreateCastCommand",Any), native.GetType("Kingmaker.UnitLogic.Commands.UnitAttack",true).GetMethod("CreateAttackCommand",Any) };
                var built=factories.SelectMany(f=>Body(f).Where(i=>i.Op==OpCodes.Newobj && i.Call!=null).Select(i=>i.Call.DeclaringType)).Distinct().ToList();
                var markedElsewhere=runs.Count(r=>r>=1 && b[r-1].Op==OpCodes.Stfld && ((FieldInfo)b[r-1].Operand).Name=="CreatedByPlayer");
                var manual=Find(b,null,"set_ManualTarget").Where(i=>i>run[0] && i<run[0]+10).Count();
                Console.WriteLine("INFO cannot-attack setup: ctor UnitMoveTo("+string.Join(",",ctorArgs)+") at "+ctor+"; local "+local+"; its Run at "+run[0]+"; uses between="+between+"; local sources="+string.Join(", ",sources)+
                    "; factory products="+string.Join(",",built.Select(t=>t.Name).ToArray())+"; other Runs marked CreatedByPlayer="+markedElsewhere+" of "+(runs.Count-1));
                return Every("C1",ctor>0,
                    local>=0,
                    run.Count==1,
                    runs.Count==3,
                    between==0,
                    sized,
                    ctorArgs.Length==2,
                    ctorArgs[0]=="Vector3",
                    ctorArgs[1]=="Single",
                    Enumerable.Range(ctor-8,8).Any(k=>b[k].Call!=null && b[k].Call.Name=="get_Position"),
                    sources.Length==3,
                    sources.Contains("UnitUseAbility::CreateCastCommand"),
                    sources.Contains("UnitAttack::CreateAttackCommand"),
                    sources.Contains("UnitMoveTo::.ctor"),
                    !holdsCommand,
                    Find(request,"IClickActionHandler","OnAttackRequested").Count==1,
                    request.Count(i=>i.Op==OpCodes.Ldfld)==3,
                    built.All(t=>!move.IsAssignableFrom(t)),
                    built.Count==2,
                    markedElsewhere==2,
                    b[run[0]-2].Op!=OpCodes.Stfld,
                    manual==1);
            });
        Check("NATIVE-EXECUTED the installed UnitMoveTo(Vector3, float) constructor, run detached, leaves the complete configured state of the cannot-attack command: the point target, ApproachRadius, MaxApproachRadius 1000000, cooldown ignored, ShowTargetMarker true, CreatedByPlayer false and every other field at its default (Orientation, MovementDelay, SpeedLimit, MovementType Normal, Roaming, AiAction, Executor, ForcedPath)",
            ()=>{
                var two=move.GetConstructors().Single(c=>c.GetParameters().Length==2);
                var vector=Activator.CreateInstance(two.GetParameters()[0].ParameterType,new object[]{1f,2f,3f});
                var made=two.Invoke(new object[]{vector,1.65f});
                var state=new List<string>();
                for(var t=move;t!=null && t!=typeof(object);t=t.BaseType)
                    foreach(var f in t.GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly))
                    { object v; try { v=f.GetValue(made); } catch(Exception e) { v="<"+e.GetType().Name+">"; } state.Add(t.Name+"."+f.Name+"="+(v==null?"null":v.ToString())); }
                Console.WriteLine("INFO native cannot-attack command state: "+string.Join("; ",state.ToArray()));
                Func<string,object> prop=n=>move.GetProperty(n,Any).GetValue(made,null);
                Func<string,object> field=n=>{ for(var t=move;t!=null;t=t.BaseType){ var f=t.GetField(n,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly); if(f!=null) return f.GetValue(made); } throw new MissingFieldException(n); };
                return Every("C2",(float)prop("ApproachRadius")==1.65f,
                    (float)prop("MaxApproachRadius")==1000000f,
                    (bool)field("m_IgnoreCooldown"),
                    (bool)prop("ShowTargetMarker"),
                    !(bool)field("CreatedByPlayer"),
                    prop("Orientation")==null,
                    (float)prop("MovementDelay")==0f,
                    prop("SpeedLimit")==null,
                    prop("MovementType").ToString()=="Normal",
                    !(bool)field("Roaming"),
                    prop("AiAction")==null,
                    prop("Executor")==null,
                    field("m_ForcedPath")==null,
                    field("<Target>k__BackingField").ToString().Contains("point '"+vector.ToString()+"'"));
            });
        Check("NATIVE-IL a preview is not a placement: OnMovementStarted calls AddPointer (06002D62, (Vector3 clickEventWorldPosition, UnitEntityData unit)) once for a placement and AddPreviewPointer for a preview",
            ()=>{
                var b=Body(started); var parameters=addPointer.GetParameters();
                return addPointer.MetadataToken==0x06002D62 && parameters.Length==2 && parameters[1].Name=="unit" && Find(b,"ClickPointerManager","AddPointer").Count==1 && Find(b,"ClickPointerManager","AddPreviewPointer").Count>=1;
            });
        Check("NATIVE-IL a scene's own move ends with the scene: CommandMoveUnit.OnStop interrupts the unit command it ran",
            ()=>Find(Body(moveUnit.GetMethod("OnStop",Any)),null,"Interrupt").Count>=1);

        var controller=product.GetType("KingmakerMountedCombat.Integration.MountedPatchController",true);
        var methods=controller.GetNestedType("PatchMethods",Any);
        Check("PRODUCT-IL the complete product inventory of marker-capable command sites, each with what it asks for, is the reviewed one: the door, recovery, attack, carried-interaction and regroup moves ask for no marker; the entrance carrier is a UnitAreaTransition (one marker for the pair, by the skip and retirement); the click-approach routing builds no command (it runs the native one). Every other site is in the Diagnostics namespace, reached only by the runtime automation host, which exists only when the game is launched with its command line",
            ()=>{
                var all=MarkerSites(product,move,transition);
                var diagnostic=all.Where(s=>s.StartsWith("KingmakerMountedCombat.Diagnostics.",StringComparison.Ordinal)).ToList();
                var sites=all.Where(s=>!s.StartsWith("KingmakerMountedCombat.Diagnostics.",StringComparison.Ordinal)).ToList();
                sites.Sort(StringComparer.Ordinal);
                var root=product.GetType("KingmakerMountedCombat.CompositionRoot",true);
                var automationOnly=false; foreach(var c in root.GetConstructors(All)) if(Find(Body(c),"RuntimeAutomationHost","CreateFromCommandLine").Count==1) automationOnly=true;
                Console.WriteLine("INFO product play sites: "+string.Join(" | ",sites.ToArray())+"; diagnostic automation sites="+diagnostic.Count+"; automation from the command line only="+automationOnly);
                var expected=new[]{
                    "KingmakerMountedCombat.Integration.MountedCarriedApproach.",
                    "KingmakerMountedCombat.Integration.MountedCombatController.",
                    "KingmakerMountedCombat.Integration.MountedDoorInteractionCommand.BeginDelegatedMove:MountedDoorApproachMove:false",
                    "KingmakerMountedCombat.Integration.MountedDoorOccupancySettlement.",
                    "KingmakerMountedCombat.Integration.MountedPairAttackCommand.",
                    "KingmakerMountedCombat.Integration.MountedPersistenceService.NewRegroupMove:UnitMoveTo:false"};
                var unclassified=sites.Where(s=>!expected.Any(e=>s.StartsWith(e,StringComparison.Ordinal))).ToList();
                var asks=sites.Where(s=>s.EndsWith(":true")||s.EndsWith(":default")||s.EndsWith(":computed")).ToList();
                Console.WriteLine("INFO unclassified="+string.Join(", ",unclassified.ToArray())+"; asking for a marker="+string.Join(", ",asks.ToArray()));
                return sites.Count==6 && unclassified.Count==0 && asks.Count==0 && automationOnly &&
                    sites.Contains("KingmakerMountedCombat.Integration.MountedDoorInteractionCommand.BeginDelegatedMove:MountedDoorApproachMove:false") &&
                    sites.Exists(s=>s.StartsWith("KingmakerMountedCombat.Integration.MountedCombatController.")&&s.EndsWith(":UnitAreaTransition:transition")) &&
                    sites.Exists(s=>s.StartsWith("KingmakerMountedCombat.Integration.MountedCarriedApproach.")&&s.EndsWith(":false")) &&
                    sites.Exists(s=>s.StartsWith("KingmakerMountedCombat.Integration.MountedDoorOccupancySettlement.")&&s.EndsWith(":false")) &&
                    sites.Exists(s=>s.StartsWith("KingmakerMountedCombat.Integration.MountedPairAttackCommand.")&&s.EndsWith(":false"));
            });
        Check("PRODUCT-EXECUTED the click-approach authority is the exact native call site (review of b1a88b4e, R1; Tabletop Rules Expansion startup repair): the product's transpiler, run against the installed ClickUnitHandler.OnClick IL, keeps the UnitCommands.Run of the local the UnitMoveTo construction is stored in and guards it: dup; the same local; a call of ClickApproachRouted (static bool, same container and command); brtrue to a pop that skips the Run; br to a join after it. The loot and interact Runs and every other instruction are unchanged; OnClick is registered with the unit-click prefix and this transpiler, and no frame scope or click postfix exists. Test-GunslingerClickComposition.ps1 composes it with the other mod through the loader",
            ()=>{
                var harmony12=Assembly.LoadFrom(Path.Combine(managed,"UnityModManager/0Harmony12.dll"));
                var instruction=harmony12.GetType("Harmony12.CodeInstruction",true);
                var before=(IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(instruction));
                foreach(var raw in (IEnumerable)read.Invoke(null,new object[]{onClick,null}))
                {
                    var made=Activator.CreateInstance(instruction,new object[]{raw.GetType().GetField("opcode").GetValue(raw),raw.GetType().GetField("operand").GetValue(raw)});
                    var labels=(IList)instruction.GetField("labels").GetValue(made); foreach(var l in (IEnumerable)raw.GetType().GetField("labels").GetValue(raw)) labels.Add(l);
                    before.Add(made);
                }
                var snapshot=new List<object[]>(); foreach(var i in before) snapshot.Add(new[]{instruction.GetField("opcode").GetValue(i),instruction.GetField("operand").GetValue(i)});
                var generator=new DynamicMethod("KmcMarkerProbeLabels",typeof(void),Type.EmptyTypes).GetILGenerator();
                var after=((IEnumerable)methods.GetMethod("CannotAttackApproachTranspiler",Any).Invoke(null,new object[]{before,generator})).Cast<object>().ToList();
                Func<int,object> op=i=>instruction.GetField("opcode").GetValue(after[i]); Func<int,object> arg=i=>instruction.GetField("operand").GetValue(after[i]);
                Func<int,IList> lab=i=>(IList)instruction.GetField("labels").GetValue(after[i]);
                var b=Body(onClick); var hook=methods.GetMethod("ClickApproachRouted",Any);
                var ctor=-1; for(var i=0;i<b.Count;i++) if(b[i].Op==OpCodes.Newobj && b[i].Call!=null && b[i].Call.DeclaringType==move) ctor=i;
                var local=LocalIndex(b[ctor+1]);
                var run=hook.GetParameters()[0].ParameterType.GetMethod("Run",new[]{hook.GetParameters()[1].ParameterType});
                var r=-1; for(var i=ctor+2;i<b.Count;i++) if(Equals(snapshot[i][1],run) && IsLoad(b[i-1]) && LocalIndex(b[i-1])==local) r=i;
                Func<int,int,bool> same=(a,s)=>Equals(op(a),snapshot[s][0]) && Equals(arg(a),snapshot[s][1]);
                var prefix=r>0 && Enumerable.Range(0,r-1).All(i=>same(i,i)); var suffix=r>0 && Enumerable.Range(r+1,snapshot.Count-r-1).All(i=>same(i+7,i));
                var hookParameters=hook.GetParameters().Select(x=>x.ParameterType.Name).ToArray();
                Console.WriteLine("INFO transpiler: "+snapshot.Count+" -> "+after.Count+" instructions; guarded Run at "+r+"; hook "+hook.ReturnType.Name+" ("+string.Join(",",hookParameters)+")");
                return Every("C3",after.Count==snapshot.Count+7,
                    r>0,
                    prefix,
                    Equals(op(r-1),OpCodes.Dup),
                    Equals(op(r),snapshot[r-1][0]) && Equals(arg(r),snapshot[r-1][1]),
                    Equals(op(r+1),OpCodes.Call) && Equals(arg(r+1),hook),
                    Equals(op(r+2),OpCodes.Brtrue),
                    same(r+3,r-1) && same(r+4,r),
                    Equals(op(r+5),OpCodes.Br),
                    Equals(op(r+6),OpCodes.Pop) && lab(r+6).Count==1 && lab(r+6)[0].Equals(arg(r+2)),
                    Equals(op(r+7),OpCodes.Nop) && lab(r+7).Count==1 && lab(r+7)[0].Equals(arg(r+5)),
                    suffix,
                    hook.IsStatic && hook.ReturnType==typeof(bool),
                    hookParameters.Length==2 && hookParameters[0]=="UnitCommands" && hookParameters[1]=="UnitCommand",
                    string.Join(",",(Hooks(controller,"Kingmaker.Controllers.Clicks.Handlers.ClickUnitHandler",0x060093ED)??new List<string>()).ToArray())=="UnitClickPrefix,CannotAttackApproachTranspiler",
                    product.GetType("KingmakerMountedCombat.Integration.MountedNativeUnitClickScope")==null,
                    methods.GetMethod("UnitClickPostfix",Any)==null);
            });
        Check("PRODUCT-IL the click-approach routing (review of b1a88b4e, R1, R2): only the call-site hook reaches it (the command-run prefix does not); it decides through the production policy, scene precedence included, binds the hostile-click observation, then the ground-order lifecycle (GroundCommandPrefix; the open order's identity of this command; the mount's Run; GroundCommandPostfix in a finally); every Run it makes runs the original command parameter; it builds no command and sets no CreatedByPlayer; a fault leaves the call site's own submission to run only while the command is not given to another unit; the call-site hook submits nothing itself (the method keeps its submission, Tabletop Rules Expansion startup repair)",
            ()=>{
                var route=methods.GetMethod("RouteRiderClickApproach",Any); var hook=methods.GetMethod("ClickApproachRouted",Any);
                var r=Body(route); var h=Body(hook); var run=Body(methods.GetMethod("UnitCommandRunPrefix",Any));
                var callers=Uses(product,new[]{route.MetadataToken},0x28,0x6F).Count;
                var decision=Find(r,"MountedDestinationMarkerPolicy","RoutesRiderClickApproach"); var observe=Find(r,null,"BindObservedStockRequest");
                var admission=Find(r,null,"GroundCommandPrefix"); var identity=Find(r,null,"BindGroundOrderClickApproach"); var completion=Find(r,null,"GroundCommandPostfix");
                var runs=Find(r,"UnitCommands","Run");
                var builds=r.Any(i=>i.Op==OpCodes.Newobj && i.Call!=null && native.GetType("Kingmaker.UnitLogic.Commands.Base.UnitCommand",true).IsAssignableFrom(i.Call.DeclaringType));
                var marks=r.Any(i=>i.Op==OpCodes.Stfld && ((FieldInfo)i.Operand).Name=="CreatedByPlayer");
                var original=runs.All(i=>r[i-1].Op==OpCodes.Ldarg_1);
                var postfix=product.GetType("KingmakerMountedCombat.Integration.MountedPatchController",true).GetNestedType("PatchMethods",Any).GetMethod("GroundCommandPostfix",Any);
                return Every("C4",callers==1,
                    Find(h,null,"RouteRiderClickApproach").Count==1,
                    Find(run,null,"RouteRiderClickApproach").Count==0,
                    Find(run,null,"ClickApproachRouted").Count==0,
                    decision.Count==1,
                    observe.Count==1,
                    admission.Count==1,
                    identity.Count==1,
                    completion.Count==1,
                    runs.Count==2,
                    decision[0]<observe[0],
                    observe[0]<admission[0],
                    admission[0]<identity[0],
                    identity[0]<runs.Max(),
                    Find(r,null,"IsUnderLiveSceneControl").Count==1,
                    Find(r,"MountedSceneContinuityPolicy","SceneActive").Count==1,
                    !builds,
                    !marks,
                    original,
                    InFinally(route,postfix.MetadataToken),
                    Find(h,"UnitCommands","Run").Count==0,
                    Find(h,null,"get_Executor").Count>=1);
            });
        Check("PRODUCT-IL the ground order recognises the native command by identity (review of b1a88b4e, R2): BindGroundOrderClickApproach holds it only while the order is open; CompleteGroundCommandAdmission (the turn-based admission) accepts it by reference and clears it, and the controller's boundary clears it; IsPairPlayerOrder is CreatedByPlayer, that identity, or the watched explicit move; the watch binding, the turn-based path-end bridge, door admission, the container-order travel, the carried-request player test and the marker observer ask it; the group-pace order classification keeps CreatedByPlayer (native accelerates no cannot-attack approach)",
            ()=>{
                var combat=product.GetType("KingmakerMountedCombat.Integration.MountedCombatController",true);
                var field=combat.GetField("groundOrderClickApproach",Any);
                Func<string,List<Ins>> body=n=>Body(combat.GetMethod(n,Any));
                var bind=body("BindGroundOrderClickApproach"); var complete=body("CompleteGroundCommandAdmission"); var player=body("IsPairPlayerOrder");
                Func<List<Ins>,bool> clears=b2=>{ for(var i=1;i<b2.Count;i++) if(b2[i].Op==OpCodes.Stfld && Equals(b2[i].Operand,field) && b2[i-1].Op==OpCodes.Ldnull) return true; return false; };
                var clearing=combat.GetMethods(All).Count(m=>m.GetMethodBody()!=null && clears(Body(m)));
                var askers=new[]{"ObserveGroundOrderRun","TryCompleteNativeMountTurnMoveAtReachedPathEnd","DecideDoorAdmission","ObserveContainerOrderRun"}.All(n=>Find(body(n),null,"IsPairPlayerOrder").Count==1);
                var host=combat.GetNestedType("MountedCarriedRequestHost",Any);
                var observer=product.GetType("KingmakerMountedCombat.Integration.MountedExplorationObserver",true);
                var pace=combat.GetMethod("ClassifyMountGroupOrder",Any);
                return Every("C5",field!=null,
                    bind.Any(i=>i.Op==OpCodes.Stfld && Equals(i.Operand,field)),
                    bind.Any(i=>i.Op==OpCodes.Ldfld && ((FieldInfo)i.Operand).Name=="groundOrderOpen"),
                    clears(complete),
                    clearing>=2,
                    complete.Any(i=>i.Op==OpCodes.Ldfld && Equals(i.Operand,field)),
                    player.Any(i=>i.Op==OpCodes.Ldfld && ((FieldInfo)i.Operand).Name=="CreatedByPlayer"),
                    player.Any(i=>i.Op==OpCodes.Ldfld && Equals(i.Operand,field)),
                    Find(player,"MountedPlayerMoveWatch","IsCurrent").Count==1,
                    askers,
                    Find(Body(host.GetMethod("IsPlayerRequest",Any)),null,"IsPairPlayerOrder").Count==1,
                    Find(Body(observer.GetMethod("Facts",Any)),null,"IsPairPlayerOrder").Count==1,
                    Find(Body(pace),null,"IsPairPlayerOrder").Count==0);
            });
        Check("PRODUCT-IL the placement record is the actual AddPointer call (review of b1a88b4e, R3): ClickPointerManager.AddPointer (06002D62) is patched with a postfix taking its 'unit' that records; OnMovementStarted keeps only the carried-rider skip prefix (no placement postfix); RecordPlacement records the command that qualifies at placement without reading any pointer or its visibility; the ledger holds commands weakly in a ConditionalWeakTable keyed by unit; the entrance carry still retires only the rider's own pointer",
            ()=>{
                var markers=product.GetType("KingmakerMountedCombat.Integration.MountedDestinationMarkers",true);
                var record=Body(markers.GetMethod("RecordPlacement",Any));
                var ledger=product.GetType("KingmakerMountedCombat.Domain.MountedPointerPlacementLedger`2",true);
                var weak=News(ledger.GetMethod("Record")).Contains(typeof(WeakReference));
                var table=ledger.GetFields(Any).Any(f=>f.FieldType.IsGenericType && f.FieldType.GetGenericTypeDefinition()==typeof(System.Runtime.CompilerServices.ConditionalWeakTable<,>));
                var post=methods.GetMethod("PointerPlacedPostfix",Any); var parameters=post.GetParameters();
                var entrance=product.GetType("KingmakerMountedCombat.Integration.MountedCombatController",true);
                var carries=false; foreach(var m in entrance.GetMethods(All)) if(m.GetMethodBody()!=null && Find(Body(m),"MountedDestinationMarkers","RetireRiderPointer").Count==1) carries=true;
                var addHooks=Hooks(controller,"Kingmaker.UI.ClickPointerManager",0x06002D62); var startHooks=Hooks(controller,"Kingmaker.View.UnitEntityView",0x0600184E);
                Console.WriteLine("INFO registrations: AddPointer="+(addHooks==null?"none":string.Join(",",addHooks.ToArray()))+"; OnMovementStarted="+(startHooks==null?"none":string.Join(",",startHooks.ToArray())));
                return Every("C6",addHooks!=null && addHooks.Count==1 && addHooks[0]=="PointerPlacedPostfix",
                    startHooks!=null && startHooks.Count==1 && startHooks[0]=="CarriedRiderPointerPrefix",
                    Registers(controller,"Kingmaker.View.UnitEntityView",0x0600184E,"CarriedRiderPointerPrefix",null),
                    methods.GetMethod("MarkerPlacementPostfix",Any)==null,
                    parameters.Length==1,
                    parameters[0].Name=="unit",
                    parameters[0].ParameterType.Name=="UnitEntityData",
                    Find(Body(post),"MountedDestinationMarkers","RecordPlacement").Count==1,
                    Find(record,null,"Record").Count==1,
                    Find(record,null,"QualifyingCommand").Count==1,
                    Find(record,null,"get_UnitMarks").Count==0,
                    !record.Any(i=>i.Operand is FieldInfo && ((FieldInfo)i.Operand).Name=="IsVisible"),
                    Find(Body(methods.GetMethod("CarriedRiderPointerPrefix",Any)),null,"SkipCarriedRiderPointer").Count==1,
                    weak,
                    table,
                    carries);
            });
        Check("PRODUCT-IL diagnostic cleanup: the observer's Reset clears its marker state and the recorded placements (the ledger's Reset), and the composition root calls it when disabled and when disposed; a boundary (mode, load) clears the marker state; the click-approach routing stores nothing to clean",
            ()=>{
                var observer=product.GetType("KingmakerMountedCombat.Integration.MountedExplorationObserver",true);
                var reset=Body(observer.GetMethod("Reset",Any)); var observe=Body(observer.GetMethod("Observe",Any));
                var root=product.GetType("KingmakerMountedCombat.CompositionRoot",true);
                var disable=Body(root.GetMethod("SetEnabled")); var dispose=Body(root.GetMethod("Dispose"));
                var markers=product.GetType("KingmakerMountedCombat.Integration.MountedDestinationMarkers",true);
                return Find(reset,null,"ClearMarkers").Count==1 && Find(reset,"MountedDestinationMarkers","ResetPlacements").Count==1 &&
                    Find(Body(markers.GetMethod("ResetPlacements",Any)),null,"Reset").Count==1 && Find(observe,null,"ClearMarkers").Count>=1 &&
                    Find(disable,"MountedExplorationObserver","Reset").Count+FindAny(root,"MountedExplorationObserver","Reset")>=2 && Find(dispose,"MountedExplorationObserver","Reset").Count==1;
            });
        Check("PRODUCT-IL failure containment: each pair's marker observation runs inside its own catch in the observer's loop, its log line is written inside a catch, the click-approach routing (in its call-site hook) and the AddPointer placement-record postfix are inside a catch, so no diagnostic or routing fault, and no throwing logger, stops another pair or reaches native movement",
            ()=>{
                var observer=product.GetType("KingmakerMountedCombat.Integration.MountedExplorationObserver",true);
                var observe=observer.GetMethod("Observe",Any); var markers=observer.GetMethod("ObserveMarkers",Any);
                var info=product.GetType("KingmakerMountedCombat.Logging.IModLogger",true).GetMethod("Info");
                var run=methods.GetMethod("ClickApproachRouted",Any); var reroute=methods.GetMethod("RouteRiderClickApproach",Any);
                var post=methods.GetMethod("PointerPlacedPostfix",Any);
                var record=product.GetType("KingmakerMountedCombat.Integration.MountedDestinationMarkers",true).GetMethod("RecordPlacement",Any);
                return Caught(observe,markers.MetadataToken) && Caught(markers,info.MetadataToken) && Caught(run,reroute.MetadataToken) && Caught(post,record.MetadataToken);
            });
        Check("PRODUCT-EXECUTED the compiled routing decision, the marker constants, the placement ledger and the ownership classifier: only the exact native click approach of a mounted rider, at the native call site, outside simulation and scene ownership, is routed; a placement is recorded only when recorded (a declined one leaves the earlier record) and the ledger's Reset forgets it; delegated interaction and regroup moves ask for no marker; hidden is never stale, a live blocked move and a pending placement are native, and the five anomaly classes are anomalies",
            ()=>{
                var policy=product.GetType("KingmakerMountedCombat.Domain.MountedDestinationMarkerPolicy",true);
                var routes=policy.GetMethod("RoutesRiderClickApproach");
                Func<bool,bool,bool,bool,bool,bool,bool,bool> r=(a,b,c,d,e,f,g)=>(bool)routes.Invoke(null,new object[]{a,b,c,d,e,f,g});
                var ledgerType=product.GetType("KingmakerMountedCombat.Domain.MountedPointerPlacementLedger`2",true).MakeGenericType(typeof(object),typeof(object));
                var ledger=Activator.CreateInstance(ledgerType); var who=new object(); var first=new object(); var second=new object();
                Func<object,bool> placedFor=c=>{ var args=new object[]{who,c,0}; return (bool)ledgerType.GetMethod("PlacedFor").Invoke(ledger,args); };
                ledgerType.GetMethod("Record").Invoke(ledger,new object[]{who,first,5});
                var keptEarlier=placedFor(first) && !placedFor(second);
                ledgerType.GetMethod("Record").Invoke(ledger,new object[]{who,second,6});
                var moved=placedFor(second) && !placedFor(first);
                ledgerType.GetMethod("Reset").Invoke(ledger,null);
                var forgot=!placedFor(second);
                var door=(bool)policy.GetField("DelegatedInteractionApproachShowsMarker").GetRawConstantValue();
                var regroup=(bool)policy.GetField("UnorderedMountMoveShowsMarker").GetRawConstantValue();
                var classifier=product.GetType("KingmakerMountedCombat.Domain.MountedMarkerOwnershipPolicy",true);
                var factsType=product.GetType("KingmakerMountedCombat.Domain.MountedMarkerFacts",true);
                Func<bool,bool,bool,bool,bool,float,bool,object> facts=(exists,visible,live,unordered,placed,seen,moving)=>{
                    var f=Activator.CreateInstance(factsType);
                    factsType.GetField("PointerExists").SetValue(f,exists); factsType.GetField("PointerVisible").SetValue(f,visible); factsType.GetField("QualifyingCommandLive").SetValue(f,live);
                    factsType.GetField("QualifyingCommandUnordered").SetValue(f,unordered); factsType.GetField("PlacedForCurrentCommand").SetValue(f,placed);
                    factsType.GetField("SecondsSinceCommandSeen").SetValue(f,seen); factsType.GetField("Moving").SetValue(f,moving); return f; };
                Func<object,string> unit=f=>classifier.GetMethod("ClassifyUnit").Invoke(null,new[]{f}).ToString();
                Func<object,object,string> pair=(a,b)=>classifier.GetMethod("ClassifyPair").Invoke(null,new object[]{true,a,b}).ToString();
                var none=facts(false,false,false,false,false,0f,false);
                return r(true,true,true,false,true,false,false) && !r(false,true,true,false,true,false,false) && !r(true,false,true,false,true,false,false) && !r(true,true,false,false,true,false,false) &&
                    !r(true,true,true,true,true,false,false) && !r(true,true,true,false,false,false,false) && !r(true,true,true,false,true,true,false) && !r(true,true,true,false,true,false,true) &&
                    keptEarlier && moved && forgot && !door && !regroup &&
                    unit(facts(true,false,true,false,false,99f,false))=="HiddenPointer" && unit(facts(true,true,true,false,true,99f,false))=="ExpectedUnfinishedMove" &&
                    unit(facts(true,true,true,false,false,1f,true))=="PlacementPending" && unit(facts(true,true,true,false,false,4f,true))=="PlacedForEarlierCommand" &&
                    pair(facts(true,true,true,false,true,1f,true),facts(true,true,true,false,true,1f,true))=="DuplicatePairMarkers" &&
                    pair(facts(true,true,true,false,true,1f,true),none)=="RiderHeldWhileMountMoves" &&
                    pair(none,facts(true,true,false,false,true,1f,true))=="VisibleWithoutQualifyingCommand" &&
                    pair(none,facts(true,true,true,true,true,1f,true))=="InternalMoveRequestsMarker" &&
                    pair(none,facts(true,true,true,false,true,1f,true))=="ExpectedPhysicalMover";
            });

        Console.WriteLine("MARKER OWNERSHIP CONTRACT PASS="+passes+" FAIL="+failures+"; installed-assembly and compiled-product contracts, not native gameplay evidence");
        return failures==0?0:1;
    }

    // Every call of the given token in the method lies inside the try block of a catch clause.
    private static bool Caught(MethodBase method, int token)
    {
        var body=method.GetMethodBody(); var il=body.GetILAsByteArray(); var seen=0;
        for(var i=0;i+4<il.Length;i++)
        {
            if(!((il[i]==0x28 || il[i]==0x6F) && BitConverter.ToInt32(il,i+1)==token)) continue;
            seen++;
            var inside=false;
            foreach(var clause in body.ExceptionHandlingClauses)
                if(clause.Flags==ExceptionHandlingClauseOptions.Clause && i>=clause.TryOffset && i<clause.TryOffset+clause.TryLength) inside=true;
            if(!inside) return false;
        }
        return seen>0;
    }

    // The types a method constructs, read from its raw IL (works for methods of generic types, which Harmony cannot read).
    private static List<Type> News(MethodBase method)
    {
        var found=new List<Type>(); var il=method.GetMethodBody().GetILAsByteArray();
        var typeArguments=method.DeclaringType.IsGenericType ? method.DeclaringType.GetGenericArguments() : null;
        for(var i=0;i+4<il.Length;i++)
        {
            if(il[i]!=0x73) continue;
            try { var made=method.Module.ResolveMethod(BitConverter.ToInt32(il,i+1),typeArguments,null); if(made!=null && made.IsConstructor) found.Add(made.DeclaringType); } catch { }
        }
        return found;
    }

    // All parts hold; the failing parts are named, so a FAIL says which condition broke.
    private static bool Every(string tag, params bool[] parts)
    {
        var failed=new List<string>(); for(var i=0;i<parts.Length;i++) if(!parts[i]) failed.Add((i+1).ToString());
        if(failed.Count>0) Console.WriteLine("INFO "+tag+" failing parts: "+string.Join(",",failed.ToArray()));
        return failed.Count==0;
    }

    // Every call of the given token in the method lies inside the handler of a finally clause.
    private static bool InFinally(MethodBase method, int token)
    {
        var body=method.GetMethodBody(); var il=body.GetILAsByteArray(); var seen=0;
        for(var i=0;i+4<il.Length;i++)
        {
            if(!((il[i]==0x28 || il[i]==0x6F) && BitConverter.ToInt32(il,i+1)==token)) continue;
            seen++;
            var inside=false;
            foreach(var clause in body.ExceptionHandlingClauses)
                if(clause.Flags==ExceptionHandlingClauseOptions.Finally && i>=clause.HandlerOffset && i<clause.HandlerOffset+clause.HandlerLength) inside=true;
            if(!inside) return false;
        }
        return seen>0;
    }

    private static bool IsStore(Ins i) { return i.Op==OpCodes.Stloc || i.Op==OpCodes.Stloc_S || i.Op==OpCodes.Stloc_0 || i.Op==OpCodes.Stloc_1 || i.Op==OpCodes.Stloc_2 || i.Op==OpCodes.Stloc_3; }
    private static bool IsLoad(Ins i) { return i.Op==OpCodes.Ldloc || i.Op==OpCodes.Ldloc_S || i.Op==OpCodes.Ldloca || i.Op==OpCodes.Ldloca_S || i.Op==OpCodes.Ldloc_0 || i.Op==OpCodes.Ldloc_1 || i.Op==OpCodes.Ldloc_2 || i.Op==OpCodes.Ldloc_3; }
    private static int LocalIndex(Ins i)
    {
        if(i.Op==OpCodes.Stloc_0 || i.Op==OpCodes.Ldloc_0) return 0;
        if(i.Op==OpCodes.Stloc_1 || i.Op==OpCodes.Ldloc_1) return 1;
        if(i.Op==OpCodes.Stloc_2 || i.Op==OpCodes.Ldloc_2) return 2;
        if(i.Op==OpCodes.Stloc_3 || i.Op==OpCodes.Ldloc_3) return 3;
        var v=i.Operand as LocalVariableInfo; if(v!=null) return v.LocalIndex;
        if(i.Operand is byte) return (byte)i.Operand; if(i.Operand is short) return (short)i.Operand; if(i.Operand is int) return (int)i.Operand;
        return -1;
    }

    // How many methods of a type call the given method.
    private static int FindAny(Type type, string declaring, string name)
    {
        var n=0; foreach(var m in type.GetMethods(All)) if(m.GetMethodBody()!=null && Find(Body(m),declaring,name).Count>0) n++;
        return n;
    }
}
'@
try { $kmcCode=[KmcMarkerProbe]::Run($kmcManaged,$kmcDll) }
catch { Write-Output $_.Exception.ToString(); exit 1 }
exit $kmcCode
