[CmdletBinding()]
param([ValidateSet('Debug','Release')][string]$Configuration='Release',[string]$ProductDll)
# Known issues C02: rider-only scene placements of three metres or less. Also pins the C01 fallback (no staging
# handoff in an encounter) and the C03 combat-hold notice wiring.
#   NATIVE-IL         the installed Assembly-CSharp is READ: the scene placement operations write a unit's Position
#                     directly (TranslocateUnit.RunAction, CommandMoveUnit.IsFinished when the move took too long,
#                     CommandTeleportParty.OnRun), not through UnitEntityData.Translocate.
#   PRODUCT-IL        the compiled product is READ for its patch registrations and what its hooks call.
#   PRODUCT-EXECUTED  the compiled product's attribution and hold decisions are RUN.
# -ProductDll reads another build (the Sprint 3 base e5b92474 must FAIL the PRODUCT groups). Not native gameplay evidence.
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

public static class KmcScenePlacementProbe
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
                for(var k=j;k<body.Count && k<j+24;k++) if(body[k].Op==OpCodes.Ldstr) strings.Add((string)body[k].Operand);
                var typed=false;
                for(var k=Math.Max(0,j-8);k<j;k++) if(body[k].Op==OpCodes.Ldtoken && body[k].Operand is Type && ((Type)body[k].Operand).FullName==typeName) typed=true;
                if(typed && strings.Contains(prefix) && strings.Contains(postfix)) return true;
            }
        }
        return false;
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

        var translocate=native.GetType("Kingmaker.Designers.EventConditionActionSystem.Actions.TranslocateUnit",true);
        var move=native.GetType("Kingmaker.AreaLogic.Cutscenes.Commands.CommandMoveUnit",true);
        var party=native.GetType("Kingmaker.AreaLogic.Cutscenes.Commands.CommandTeleportParty",true);
        Check("NATIVE-IL the scene placement operations write Position directly, never through UnitEntityData.Translocate: TranslocateUnit.RunAction (06008FA2), CommandMoveUnit.IsFinished (06009E4E) on its taking-too-long branch, CommandTeleportParty.OnRun (06009E90)",
            ()=>{
                var a=Body(Token(translocate,"RunAction",0x06008FA2)); var b=Body(Token(move,"IsFinished",0x06009E4E)); var c=Body(Token(party,"OnRun",0x06009E90));
                var tooLong=new List<int>();
                for(var i=0;i<b.Count;i++) if(b[i].Op==OpCodes.Ldfld && b[i].Operand is FieldInfo && ((FieldInfo)b[i].Operand).Name=="TakingTooLong") tooLong.Add(i);
                return Find(a,"UnitEntityData","set_Position").Count>=1 && Find(a,"UnitEntityData","Translocate").Count==0 &&
                    tooLong.Count==1 && Find(b,"UnitEntityData","set_Position").Count==1 && Find(b,"UnitEntityData","set_Position")[0]>tooLong[0] &&
                    Find(c,"UnitEntityData","set_Position").Count==1 && Find(c,"UnitEntityData","Translocate").Count==0;
            });

        var controller=product.GetType("KingmakerMountedCombat.Integration.MountedPatchController",true);
        var methods=controller.GetNestedType("PatchMethods",Any);
        Check("PRODUCT-IL the three operations are patched by their exact tokens with the capture prefix and their attribution postfix",
            ()=>Registers(controller,translocate.FullName,0x06008FA2,"ScenePlacementCapturePrefix","TranslocateUnitPostfix") &&
                Registers(controller,move.FullName,0x06009E4E,"ScenePlacementCapturePrefix","CommandMoveUnitPostfix") &&
                Registers(controller,party.FullName,0x06009E90,"ScenePlacementCapturePrefix","CommandTeleportPartyPostfix"));
        Check("PRODUCT-IL the prefix captures the mounted pairs; each postfix attributes through the decision and hands a rider-only write to the rider's synchronizer; the synchronizer's own detection asks the unchanged distance rule; faults are contained",
            ()=>{
                var attribution=product.GetType("KingmakerMountedCombat.Integration.MountedScenePlacementAttribution",true);
                var attribute=Body(attribution.GetMethod("Attribute",Any));
                var agent=product.GetType("KingmakerMountedCombat.Integration.RiderMovementAgent",true);
                var hold=Body(agent.GetMethod("HoldForScenePlacement",Any));
                var share=Body(methods.GetMethod("AttributeScenePlacement",Any));
                var prefix=Body(methods.GetMethod("ScenePlacementCapturePrefix",Any));
                return Find(prefix,"MountedScenePlacementAttribution","Capture").Count==1 && Find(share,"MountedScenePlacementAttribution","Attribute").Count==1 &&
                    Find(share,null,"ReportScriptZoneFault").Count==1 && Find(prefix,null,"ReportScriptZoneFault").Count==1 &&
                    Find(attribute,"MountedSceneContinuityPolicy","AttributesRiderPlacement")[0]<Find(attribute,"RiderMovementAgent","AttributeScenePlacement")[0] &&
                    Find(hold,"MountedSceneContinuityPolicy","HoldsUnattributedPlacement").Count==1 && Find(hold,"MountedSceneContinuityPolicy","IsScenePlacement").Count==0;
            });
        Check("NATIVE-IL (C01) no installed method pauses a cutscene for combat: of the methods that set Paused, only CutsceneController.Activate reads IsInCombat, and only to interrupt AI commands before it pauses background cutscenes by IsBackground; and CommandMoveUnit.OnRun (06009E4D) runs a UnitMoveTo on the unit, which a seated rider cannot walk",
            ()=>{
                var player=native.GetType("Kingmaker.AreaLogic.Cutscenes.CutscenePlayerData",true);
                var setPaused=player.GetProperty("Paused",Any).GetSetMethod(true).MetadataToken;
                var combatTokens=new HashSet<int>();
                Type[] types; try { types=native.GetTypes(); } catch(ReflectionTypeLoadException partial) { types=partial.Types; }
                foreach(var type in types) if(type!=null) foreach(var member in type.GetMember("get_IsInCombat",MemberTypes.Method,All)) combatTokens.Add(member.MetadataToken);
                var callers=0; var combatCallers=0;
                foreach(var type in types)
                {
                    if(type==null) continue;
                    foreach(var member in type.GetMembers(All))
                    {
                        var method=member as MethodBase; if(method==null) continue;
                        byte[] il; try { var body=method.GetMethodBody(); if(body==null) continue; il=body.GetILAsByteArray(); } catch { continue; }
                        var calls=new HashSet<int>();
                        for(var i=0;i+4<il.Length;i++) if(il[i]==0x28 || il[i]==0x6F) calls.Add(BitConverter.ToInt32(il,i+1));
                        if(!calls.Contains(setPaused)) continue;
                        callers++;
                        foreach(var token in combatTokens) if(calls.Contains(token))
                        {
                            Console.WriteLine("INFO Paused setter reads IsInCombat: "+type.FullName+"."+method.Name);
                            var activate=type.FullName=="Kingmaker.Controllers.CutsceneController" && method.Name=="Activate";
                            if(activate)
                            {
                                var a=Body(method); var combat=Find(a,null,"get_IsInCombat"); var interrupt=Find(a,"UnitCommands","InterruptAll");
                                var background=new List<int>(); var pause=Find(a,"CutscenePlayerData","set_Paused");
                                for(var k=0;k<a.Count;k++) if(a[k].Op==OpCodes.Ldfld && a[k].Operand is FieldInfo && ((FieldInfo)a[k].Operand).Name=="IsBackground") background.Add(k);
                                var last=0; foreach(var c in combat) last=Math.Max(last,c);
                                activate=interrupt.Count==1 && background.Count==1 && pause.Count==1 && last<interrupt[0] && interrupt[0]<background[0] && background[0]<pause[0];
                            }
                            if(!activate) combatCallers++;
                            break;
                        }
                    }
                }
                Console.WriteLine("INFO Paused setters="+callers+"; pausing on combat="+combatCallers);
                var run=Body(Token(move,"OnRun",0x06009E4D));
                var builds=false; foreach(var ins in run) if(ins.Op==OpCodes.Newobj && ins.Call!=null && ins.Call.DeclaringType.Name=="UnitMoveTo") builds=true;
                return callers>=1 && combatCallers==0 && builds && Find(run,"UnitCommands","Run").Count>=1;
            });
        Check("PRODUCT-IL (C01 fallback, unchanged) the staging handoff refuses during an encounter before any suspension, and the suspension guard still requires settled participation",
            ()=>{
                var intent=product.GetType("KingmakerMountedCombat.Integration.MountedPersistenceService",true);
                var handoff=Body(intent.GetMethod("TryHandOffForStaging",Any)); var refusal=Body(intent.GetMethod("SuspensionRefusal",Any));
                var combat=Find(handoff,null,"get_IsInCombat"); var suspend=Find(handoff,null,"Run");
                return combat.Count>=1 && Find(refusal,null,"HasAreaParticipationObligations").Count==1 &&
                    Find(handoff,"MountedSceneHandOffSequence","Run").Count==1 && combat[0]<Find(handoff,"MountedSceneHandOffSequence","Run")[0];
            });
        Check("PRODUCT-IL (C03) the restore loop asks the combat-hold notice on its hold and on its other evaluations, and reports the hold to the player through it",
            ()=>{
                var intent=product.GetType("KingmakerMountedCombat.Integration.MountedPersistenceService",true);
                var core=Body(intent.GetMethod("RestoreRetainedIntentCore",Any));
                var asks=Find(core,"CombatHoldNotice","ShouldNotify");
                return asks.Count==2 && Find(core,null,"Report").Count>=2;
            });
        Check("PRODUCT-EXECUTED the compiled decisions: only a scene operation's write of the rider without its mount is attributed; the unattributed rule still needs more than 3 m from both the saddle and the last write, during a scene",
            ()=>{
                var policy=product.GetType("KingmakerMountedCombat.Domain.MountedSceneContinuityPolicy",true);
                var attr=policy.GetMethod("AttributesRiderPlacement"); var holds=policy.GetMethod("HoldsUnattributedPlacement");
                Func<bool,bool,bool,bool,bool> a=(m,s,r,mo)=>(bool)attr.Invoke(null,new object[]{m,s,r,mo});
                Func<bool,bool,float,float,bool> h=(s,l,d,x)=>(bool)holds.Invoke(null,new object[]{s,l,d,x});
                return a(true,true,true,false) && !a(true,true,true,true) && !a(true,true,false,true) && !a(true,false,true,false) && !a(false,true,true,false) &&
                    h(true,true,3.5f,3.5f) && !h(true,true,3f,3.5f) && !h(true,true,3.5f,2f) && !h(false,true,6f,6f) && !h(true,false,6f,6f);
            });

        Console.WriteLine("SCENE PLACEMENT CONTRACT PASS="+passes+" FAIL="+failures+"; installed-assembly and compiled-product contracts, not native gameplay evidence");
        return failures==0?0:1;
    }
}
'@
try { $kmcCode=[KmcScenePlacementProbe]::Run($kmcManaged,$kmcDll) }
catch { Write-Output $_.Exception.ToString(); exit 1 }
exit $kmcCode
