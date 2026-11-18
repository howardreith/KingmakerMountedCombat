[CmdletBinding()]
param([ValidateSet('Debug','Release')][string]$Configuration='Release',[string]$ProductDll)
# Known issues M04 closeout: destination-marker ownership.
#   NATIVE-IL         the installed Assembly-CSharp is READ (whole-assembly scans where stated): the pointer table, its only
#                     placement and visibility rules, every native creator of a marker-capable command, the unit-click
#                     cannot-attack approach and the scene's own stop of its move.
#   PRODUCT-IL        the compiled product is READ: every product site that builds a marker-capable command and what it
#                     asks for, the click-approach routing, the placement record and the diagnostic cleanup wiring.
#   PRODUCT-EXECUTED  the compiled routing decision, marker constants and ownership classifier are RUN.
# -ProductDll reads another build (the accepted Sprint 4 build 14f1b474 must FAIL the PRODUCT groups). Not native gameplay evidence.
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
        Check("NATIVE-IL a scene's own move ends with the scene: CommandMoveUnit.OnStop interrupts the unit command it ran",
            ()=>Find(Body(moveUnit.GetMethod("OnStop",Any)),null,"Interrupt").Count>=1);

        var controller=product.GetType("KingmakerMountedCombat.Integration.MountedPatchController",true);
        var methods=controller.GetNestedType("PatchMethods",Any);
        Check("PRODUCT-IL the complete product inventory of marker-capable command sites, each with what it asks for, is the reviewed one: the door, recovery, attack, carried-interaction and regroup moves ask for no marker; the entrance carrier is a UnitAreaTransition (one marker for the pair, by the skip and retirement); the click-approach reroute copies the native move's request. Every other site is in the Diagnostics namespace, reached only by the runtime automation host, which exists only when the game is launched with its command line",
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
                    "KingmakerMountedCombat.Integration.MountedPatchController/PatchMethods.RerouteRiderClickApproach:UnitMoveTo:copied",
                    "KingmakerMountedCombat.Integration.MountedPersistenceService.NewRegroupMove:UnitMoveTo:false"};
                var unclassified=sites.Where(s=>!expected.Any(e=>s.StartsWith(e,StringComparison.Ordinal))).ToList();
                var asks=sites.Where(s=>s.EndsWith(":true")||s.EndsWith(":default")||s.EndsWith(":computed")).ToList();
                Console.WriteLine("INFO unclassified="+string.Join(", ",unclassified.ToArray())+"; asking for a marker="+string.Join(", ",asks.ToArray()));
                return sites.Count==7 && unclassified.Count==0 && asks.Count==0 && automationOnly &&
                    sites.Contains("KingmakerMountedCombat.Integration.MountedDoorInteractionCommand.BeginDelegatedMove:MountedDoorApproachMove:false") &&
                    sites.Exists(s=>s.StartsWith("KingmakerMountedCombat.Integration.MountedCombatController.")&&s.EndsWith(":UnitAreaTransition:transition")) &&
                    sites.Exists(s=>s.StartsWith("KingmakerMountedCombat.Integration.MountedCarriedApproach.")&&s.EndsWith(":false")) &&
                    sites.Exists(s=>s.StartsWith("KingmakerMountedCombat.Integration.MountedDoorOccupancySettlement.")&&s.EndsWith(":false")) &&
                    sites.Exists(s=>s.StartsWith("KingmakerMountedCombat.Integration.MountedPairAttackCommand.")&&s.EndsWith(":false"));
            });
        Check("PRODUCT-IL the click-approach routing: the unit-click prefix opens the native click's scope and its registered postfix closes it; the command-run prefix asks the reroute after the scene check and before the pair's command gates; the reroute decides through the production policy and goes through the ground-order lifecycle (GroundCommandPrefix, a player-created UnitMoveTo with the native move's approach radius, GroundCommandPostfix in a finally)",
            ()=>{
                var prefix=Body(methods.GetMethod("UnitClickPrefix",Any)); var postfix=Body(methods.GetMethod("UnitClickPostfix",Any));
                var run=Body(methods.GetMethod("UnitCommandRunPrefix",Any)); var reroute=Body(methods.GetMethod("RerouteRiderClickApproach",Any));
                var scene=Find(run,null,"ObserveSceneCommand"); var asks=Find(run,null,"RerouteRiderClickApproach"); var gates=Find(run,null,"AdmitPairCommandRun");
                var createdByPlayer=false;
                for(var i=1;i<reroute.Count;i++) if(reroute[i].Op==OpCodes.Stfld && reroute[i].Operand is FieldInfo && ((FieldInfo)reroute[i].Operand).Name=="CreatedByPlayer" && reroute[i-1].Op==OpCodes.Ldc_I4_1) createdByPlayer=true;
                var routeDecision=Find(reroute,"MountedDestinationMarkerPolicy","RoutesRiderClickApproach");
                var admission=Find(reroute,null,"GroundCommandPrefix"); var completion=Find(reroute,null,"GroundCommandPostfix");
                return Find(prefix,"MountedNativeUnitClickScope","Open").Count==1 && Find(postfix,"MountedNativeUnitClickScope","Close").Count==1 &&
                    Registers(controller,"Kingmaker.Controllers.Clicks.Handlers.ClickUnitHandler",0x060093ED,"UnitClickPrefix","UnitClickPostfix") &&
                    scene.Count==1 && asks.Count==1 && gates.Count==1 && scene[0]<asks[0] && asks[0]<gates[0] &&
                    routeDecision.Count==1 && admission.Count==1 && completion.Count==1 && routeDecision[0]<admission[0] && admission[0]<completion[0] &&
                    createdByPlayer && Find(reroute,null,"get_ApproachRadius").Count>=1 && Find(reroute,"MountedNativeUnitClickScope","get_IsOpen").Count==1;
            });
        Check("PRODUCT-IL the placement record and entrance wiring: OnMovementStarted (0600184E) is patched with the carried-rider skip prefix and the placement-record postfix; the prefix still asks SkipCarriedRiderPointer; the postfix records only a placement that ran; the record holds its command weakly; the entrance carry still retires only the rider's own pointer",
            ()=>{
                var markers=product.GetType("KingmakerMountedCombat.Integration.MountedDestinationMarkers",true);
                var record=Body(markers.GetMethod("RecordPlacement",Any));
                var weak=false; foreach(var i in record) if(i.Op==OpCodes.Newobj && i.Call!=null && i.Call.DeclaringType==typeof(WeakReference)) weak=true;
                var post=Body(methods.GetMethod("MarkerPlacementPostfix",Any));
                var entrance=product.GetType("KingmakerMountedCombat.Integration.MountedCombatController",true);
                var carries=false; foreach(var m in entrance.GetMethods(All)) if(m.GetMethodBody()!=null && Find(Body(m),"MountedDestinationMarkers","RetireRiderPointer").Count==1) carries=true;
                return Registers(controller,"Kingmaker.View.UnitEntityView",0x0600184E,"CarriedRiderPointerPrefix","MarkerPlacementPostfix") &&
                    Find(Body(methods.GetMethod("CarriedRiderPointerPrefix",Any)),null,"SkipCarriedRiderPointer").Count==1 &&
                    Find(post,"MountedDestinationMarkers","RecordPlacement").Count==1 && weak && carries;
            });
        Check("PRODUCT-IL diagnostic cleanup: the observer's Reset clears its marker state, the recorded placements and the click scope, and the composition root calls it when disabled and when disposed; a boundary (mode, load) clears the marker state",
            ()=>{
                var observer=product.GetType("KingmakerMountedCombat.Integration.MountedExplorationObserver",true);
                var reset=Body(observer.GetMethod("Reset",Any)); var observe=Body(observer.GetMethod("Observe",Any));
                var root=product.GetType("KingmakerMountedCombat.CompositionRoot",true);
                var disable=Body(root.GetMethod("SetEnabled")); var dispose=Body(root.GetMethod("Dispose"));
                return Find(reset,null,"ClearMarkers").Count==1 && Find(reset,"MountedDestinationMarkers","ResetPlacements").Count==1 &&
                    Find(reset,"MountedNativeUnitClickScope","Close").Count==1 && Find(observe,null,"ClearMarkers").Count>=1 &&
                    Find(disable,"MountedExplorationObserver","Reset").Count+FindAny(root,"MountedExplorationObserver","Reset")>=2 && Find(dispose,"MountedExplorationObserver","Reset").Count==1;
            });
        Check("PRODUCT-IL failure containment: each pair's marker observation runs inside its own catch in the observer's loop, its log line is written inside a catch, the reroute's call site and the placement-record postfix are inside a catch, so no diagnostic or routing fault, and no throwing logger, stops another pair or reaches native movement",
            ()=>{
                var observer=product.GetType("KingmakerMountedCombat.Integration.MountedExplorationObserver",true);
                var observe=observer.GetMethod("Observe",Any); var markers=observer.GetMethod("ObserveMarkers",Any);
                var info=product.GetType("KingmakerMountedCombat.Logging.IModLogger",true).GetMethod("Info");
                var run=methods.GetMethod("UnitCommandRunPrefix",Any); var reroute=methods.GetMethod("RerouteRiderClickApproach",Any);
                var post=methods.GetMethod("MarkerPlacementPostfix",Any);
                var record=product.GetType("KingmakerMountedCombat.Integration.MountedDestinationMarkers",true).GetMethod("RecordPlacement",Any);
                return Caught(observe,markers.MetadataToken) && Caught(markers,info.MetadataToken) && Caught(run,reroute.MetadataToken) && Caught(post,record.MetadataToken);
            });
        Check("PRODUCT-EXECUTED the compiled routing decision, the marker constants and the ownership classifier: only the exact native click approach of a mounted rider is routed; delegated interaction and regroup moves ask for no marker; hidden is never stale, a live blocked move and a pending placement are native, and the five anomaly classes are anomalies",
            ()=>{
                var policy=product.GetType("KingmakerMountedCombat.Domain.MountedDestinationMarkerPolicy",true);
                var routes=policy.GetMethod("RoutesRiderClickApproach");
                Func<bool,bool,bool,bool,bool,bool,bool> r=(a,b,c,d,e,f)=>(bool)routes.Invoke(null,new object[]{a,b,c,d,e,f});
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
                return r(true,true,true,false,true,false) && !r(false,true,true,false,true,false) && !r(true,false,true,false,true,false) && !r(true,true,false,false,true,false) &&
                    !r(true,true,true,true,true,false) && !r(true,true,true,false,false,false) && !r(true,true,true,false,true,true) && !door && !regroup &&
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
