[CmdletBinding()]
param([ValidateSet('Debug','Release')][string]$Configuration='Release',[string]$ProductDll)
# Known issues C02: rider-only scene placements of three metres or less. Also pins the C01 fallback (no staging
# handoff in an encounter) and the C03 combat-hold notice: its wiring, and its delivery, executed through the installed
# native EventBus to a subscribed IWarningNotificationUIHandler sink (the interface the game's warning display implements).
# Review of 263ac3ea: R4 (a notice is spent only once a receiver took it, never during a load), R5 (the installed
# operations' orientation writes, and the scene's final orientation at the same endpoint) and R6 (C02 and C04 have
# independent bounded fault reports), each run against this build and, with -ProductDll, the reviewed one.
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
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using System.Runtime.Serialization;

// Loggers for the product's own (internal) logger interface: one records, one throws on every call.
public sealed class KmcRecordingLogger : RealProxy
{
    public readonly List<string> Lines = new List<string>();
    public KmcRecordingLogger(Type type) : base(type) { }
    public override IMessage Invoke(IMessage message)
    {
        var call = (IMethodCallMessage)message;
        if (call.MethodName == "GetType") return new ReturnMessage(GetProxiedType(), null, 0, call.LogicalCallContext, call);
        Lines.Add(call.MethodName + ": " + (call.ArgCount > 0 ? Convert.ToString(call.GetArg(0)) : ""));
        return new ReturnMessage(null, null, 0, call.LogicalCallContext, call);
    }
}

public sealed class KmcThrowingLogger : RealProxy
{
    public int Calls;
    public KmcThrowingLogger(Type type) : base(type) { }
    public override IMessage Invoke(IMessage message)
    {
        Calls++;
        return new ReturnMessage(new InvalidOperationException("the logger failed"), (IMethodCallMessage)message);
    }
}

// What the emitted warning sinks received (the sinks are real classes implementing the installed native interface,
// emitted once the installed assembly is loaded; see WarningSinkType).
public static class KmcWarningRelay
{
    public static readonly Dictionary<object, List<string>> Received = new Dictionary<object, List<string>>();
    public static readonly HashSet<object> Refusing = new HashSet<object>();
    public static void Text(object sink, string text, bool addToLog)
    {
        List<string> list; if(!Received.TryGetValue(sink, out list)) Received[sink] = list = new List<string>();
        list.Add(text + "|addToLog=" + addToLog);
        if(Refusing.Contains(sink)) throw new InvalidOperationException("the warning display refused");
    }
    public static List<string> Of(object sink) { List<string> list; return Received.TryGetValue(sink, out list) ? list : new List<string>(); }
}

public static class KmcScenePlacementProbe
{
    // A class implementing the installed Kingmaker.PubSubSystem.IWarningNotificationUIHandler: its text overload hands
    // each warning to KmcWarningRelay; its enum overload does nothing.
    private static Type WarningSinkType(Assembly native)
    {
        var handler = native.GetType("Kingmaker.PubSubSystem.IWarningNotificationUIHandler", true);
        var module = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("KmcWarningSink"), AssemblyBuilderAccess.Run).DefineDynamicModule("KmcWarningSink");
        var type = module.DefineType("KmcWarningSink", TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class, typeof(object), new[] { handler });
        type.DefineDefaultConstructor(MethodAttributes.Public);
        foreach (var method in handler.GetMethods())
        {
            var parameters = Array.ConvertAll(method.GetParameters(), p => p.ParameterType);
            var body = type.DefineMethod(method.Name, MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
                method.ReturnType, parameters);
            var il = body.GetILGenerator();
            if (parameters[0] == typeof(string))
            {
                il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldarg_1); il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Call, typeof(KmcWarningRelay).GetMethod("Text"));
            }
            il.Emit(OpCodes.Ret);
            type.DefineMethodOverride(body, method);
        }
        return type.CreateType();
    }

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

    // The notifier's Evaluate, as each build declares it (the reviewed 263ac3ea build takes no loading flag).
    private static void Evaluate(MethodInfo evaluate, object notifier, object custody, bool held, bool loading, string[] pairs)
    {
        Func<string[]> names = () => pairs; Func<string> diagnostic = () => "probe";
        if (evaluate.GetParameters().Length == 5) evaluate.Invoke(notifier, new object[] { custody, held, loading, names, diagnostic });
        else evaluate.Invoke(notifier, new object[] { custody, held, names, diagnostic });
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
        Check("PRODUCT-IL the prefix captures the mounted pairs; each postfix, only for the captured mounting instance, classifies the write and hands a rider placement to the rider's synchronizer or clears it for a whole-pair placement; the synchronizer's samples and the owner's poll both go through the hold (review R2); faults are contained in C02's own bounded report (review R6)",
            ()=>{
                var attribution=product.GetType("KingmakerMountedCombat.Integration.MountedScenePlacementAttribution",true);
                var attribute=Body(attribution.GetMethod("Attribute",Any));
                var agent=product.GetType("KingmakerMountedCombat.Integration.RiderMovementAgent",true);
                var hold=Body(agent.GetMethod("HoldForScenePlacement",Any));
                var current=Body(agent.GetMethod("CurrentScenePlacement",Any));
                var intent=product.GetType("KingmakerMountedCombat.Integration.MountedPersistenceService",true);
                var poll=Body(intent.GetMethod("ObserveScenePlacements",Any));
                var classify=Find(attribute,"MountedSceneContinuityPolicy","ClassifyOperationWrite");
                var share=Body(methods.GetMethod("AttributeScenePlacement",Any));
                var prefix=Body(methods.GetMethod("ScenePlacementCapturePrefix",Any));
                return Find(prefix,"MountedScenePlacementAttribution","Capture").Count==1 && Find(share,"MountedScenePlacementAttribution","Attribute").Count==1 &&
                    Find(share,null,"ReportScenePlacementFault").Count==1 && Find(prefix,null,"ReportScenePlacementFault").Count==1 &&
                    Find(share,null,"ReportScriptZoneFault").Count==0 && Find(prefix,null,"ReportScriptZoneFault").Count==0 &&
                    classify.Count==1 && Find(attribute,"GameMountedRelationshipService","get_MountingInstance").Count>=1 &&
                    Find(attribute,"GameMountedRelationshipService","get_MountingInstance")[0]<classify[0] &&
                    classify[0]<Find(attribute,"RiderMovementAgent","AttributeScenePlacement")[0] &&
                    classify[0]<Find(attribute,"RiderMovementAgent","SupersedeScenePlacementWithWholePair")[0] &&
                    Find(hold,"MountedScenePlacementHold","Sample").Count==1 && Find(current,"MountedScenePlacementHold","Refresh").Count==1 &&
                    Find(poll,"RiderMovementAgent","CurrentScenePlacement").Count==1 && Find(poll,"RiderMovementAgent","get_PendingScenePlacement").Count==0;
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
        Check("PRODUCT-EXECUTED (C03, review R4) with no warning receiver subscribed the notice is not spent: held custody A evaluated with nobody subscribed reaches nobody; once a sink subscribes, the same still-held custody is delivered once; scene-staging custody held in combat during a load (the compiled Decide holds it) is not attempted even with a receiver present, then delivered once after the load",
            ()=>{
                var sinkType=WarningSinkType(native);
                var sink=Activator.CreateInstance(sinkType);
                var bus=native.GetType("Kingmaker.PubSubSystem.EventBus",true);
                var subscribe=bus.GetMethod("Subscribe",new[]{typeof(object)}); var unsubscribe=bus.GetMethod("Unsubscribe",new[]{typeof(object)});
                var notifierType=product.GetType("KingmakerMountedCombat.Integration.MountedCombatHoldNotifier",true);
                var evaluate=notifierType.GetMethod("Evaluate",Any); var compose=notifierType.GetMethod("Compose",Any);
                Func<object> fresh=()=>notifierType.GetConstructors(BindingFlags.Instance|BindingFlags.NonPublic)[0].Invoke(new object[]{null});
                var namesA=new[]{"Linzi and Pony"}; var namesL=new[]{"Octavia and Horse"};
                var expectedA=(string)compose.Invoke(null,new object[]{namesA})+"|addToLog=True";
                var expectedL=(string)compose.Invoke(null,new object[]{namesL})+"|addToLog=True";
                Func<string,int> seen=text=>{ var n=0; foreach(var m in KmcWarningRelay.Of(sink)) if(m==text) n++; return n; };
                var policy=product.GetType("KingmakerMountedCombat.Domain.MountedRetainedIntentPolicy",true);
                var kind=Enum.Parse(product.GetType("KingmakerMountedCombat.Domain.MountedRetainedIntentKind",true),"ScriptedEvent");
                var held=policy.GetMethod("Decide").Invoke(null,new object[]{kind,true,true,false,"Default",false,true,false,false}).ToString();
                var loadingParameter=evaluate.GetParameters().Length==5;
                try
                {
                    // 1-2: nobody subscribed; nothing anywhere receives the notice.
                    var n=fresh(); var a=new object();
                    for(var i=0;i<3;i++) Evaluate(evaluate,n,a,true,false,namesA);
                    var nobody=KmcWarningRelay.Received.Count==0;
                    // 3-4: a sink subscribes; the same custody is still held.
                    subscribe.Invoke(null,new object[]{sink});
                    for(var i=0;i<3;i++) Evaluate(evaluate,n,a,true,false,namesA);
                    var afterSubscribe=seen(expectedA);
                    // The load: scene-staging custody restored from a save, combat, loading in progress, a receiver present.
                    var l=fresh(); var custody=new object();
                    for(var i=0;i<5;i++) Evaluate(evaluate,l,custody,true,true,namesL);
                    var duringLoad=seen(expectedL);
                    for(var i=0;i<5;i++) Evaluate(evaluate,l,custody,true,false,namesL);
                    var afterLoad=seen(expectedL);
                    Console.WriteLine("INFO R4: Decide(scene staging, combat, loading)="+held+"; evaluate takes loading="+loadingParameter+"; nobody subscribed received="+
                        (nobody?0:KmcWarningRelay.Received.Count)+"; after the sink subscribed the same custody was delivered "+afterSubscribe+" time(s); during the load "+
                        duringLoad+", after it "+afterLoad);
                    return held=="Hold" && nobody && afterSubscribe==1 && duringLoad==0 && afterLoad==1;
                }
                finally { unsubscribe.Invoke(null,new object[]{sink}); KmcWarningRelay.Received.Clear(); }
            });
        Check("PRODUCT-IL (C03, review R3 and R4) the restore loop asks the combat-hold notifier on its hold and on its other evaluations, passing whether a load is in progress; the notifier raises the native IWarningNotificationUIHandler warning and spends the notice only after the dispatch",
            ()=>{
                var intent=product.GetType("KingmakerMountedCombat.Integration.MountedPersistenceService",true);
                var core=Body(intent.GetMethod("RestoreRetainedIntentCore",Any));
                var notifier=product.GetType("KingmakerMountedCombat.Integration.MountedCombatHoldNotifier",true);
                var evaluate=Body(notifier.GetMethod("Evaluate",Any));
                var raises=false;
                foreach(var ins in evaluate) if(ins.Call!=null && ins.Call.Name=="RaiseEvent" && ins.Call.DeclaringType.Name=="EventBus" && ins.Call.IsGenericMethod &&
                    ins.Call.GetGenericArguments()[0].Name=="IWarningNotificationUIHandler") raises=true;
                var raise=-1; for(var i=0;i<evaluate.Count;i++) if(evaluate[i].Call!=null && evaluate[i].Call.Name=="RaiseEvent") raise=i;
                var spends=Find(evaluate,"CombatHoldNotice","Spend");
                return Find(core,"MountedCombatHoldNotifier","Evaluate").Count==2 && Find(core,"CombatHoldNotice","ShouldNotify").Count==0 &&
                    Find(core,"LoadingProcess","get_IsLoadingInProcess").Count>=2 &&
                    Find(evaluate,"CombatHoldNotice","ShouldAttempt").Count==1 && spends.Count>=1 && raise>=0 && spends[0]>raise && raises;
            });
        Check("PRODUCT-EXECUTED (C03, review R3) the compiled notifier delivers through the installed native EventBus to a subscribed warning sink: the actual text once across repeated held evaluations, again for a later encounter, again for a new custody after the previous one disappeared in combat; a refusing display neither throws out of it nor repeats",
            ()=>{
                var sinkType=WarningSinkType(native);
                var sink=Activator.CreateInstance(sinkType); var refusingSink=Activator.CreateInstance(sinkType);
                KmcWarningRelay.Refusing.Add(refusingSink);
                var bus=native.GetType("Kingmaker.PubSubSystem.EventBus",true);
                var subscribe=bus.GetMethod("Subscribe",new[]{typeof(object)}); var unsubscribe=bus.GetMethod("Unsubscribe",new[]{typeof(object)});
                Func<List<string>> messagesNow=()=>KmcWarningRelay.Of(sink);
                var notifierType=product.GetType("KingmakerMountedCombat.Integration.MountedCombatHoldNotifier",true);
                var notifier=notifierType.GetConstructors(BindingFlags.Instance|BindingFlags.NonPublic)[0].Invoke(new object[]{null});
                var evaluate=notifierType.GetMethod("Evaluate",Any);
                var compose=notifierType.GetMethod("Compose",Any);
                Action<object,bool,string[]> ask=(custody,held,pairs)=>Evaluate(evaluate,notifier,custody,held,false,pairs);
                var a=new object(); var b=new object(); var c=new object();
                var amiri=new[]{"Amiri and Horse"}; var two=new[]{"Valerie and Horse","Harrim and Pony"};
                var expectedA=(string)compose.Invoke(null,new object[]{amiri})+"|addToLog=True";
                var expectedB=(string)compose.Invoke(null,new object[]{two})+"|addToLog=True";
                subscribe.Invoke(null,new object[]{sink});
                try
                {
                    for(var i=0;i<5;i++) ask(a,true,amiri);
                    var messages=messagesNow();
                    var once=messages.Count==1 && messages[0]==expectedA;
                    ask(a,false,null); for(var i=0;i<3;i++) ask(a,true,amiri);
                    messages=messagesNow();
                    var again=messages.Count==2 && messages[1]==expectedA;
                    // Custody A disappears during the encounter (no later evaluation of it); custody B is held in combat at once.
                    for(var i=0;i<3;i++) ask(b,true,two);
                    messages=messagesNow();
                    var newCustody=messages.Count==3 && messages[2]==expectedB;
                    unsubscribe.Invoke(null,new object[]{sink});
                    subscribe.Invoke(null,new object[]{refusingSink});
                    var threw=false;
                    try { for(var i=0;i<3;i++) ask(c,true,amiri); } catch { threw=true; }
                    unsubscribe.Invoke(null,new object[]{refusingSink});
                    var refused=KmcWarningRelay.Of(refusingSink);
                    messages=messagesNow();
                    var bounded=!threw && refused.Count==1 && messages.Count==3;
                    Console.WriteLine("INFO delivered: "+string.Join(" || ",messages.ToArray()));
                    Console.WriteLine("INFO refusing display received "+refused.Count+" notice(s); notifier threw="+threw+"; notifier delivered="+
                        notifierType.GetProperty("DeliveredCount",Any).GetValue(notifier,null)+", failed="+notifierType.GetProperty("FailedCount",Any).GetValue(notifier,null));
                    return once && again && newCustody && bounded &&
                        expectedA.StartsWith("Amiri and Horse stay dismounted for this fight") && expectedB.StartsWith("Valerie and Horse; Harrim and Pony stay dismounted");
                }
                finally { unsubscribe.Invoke(null,new object[]{sink}); unsubscribe.Invoke(null,new object[]{refusingSink}); }
            });
        Check("NATIVE-IL (review R5) the installed operations' orientation writes: TranslocateUnit.RunAction (06008FA2) writes UnitEntityData.Orientation once, after Position, only under its m_CopyRotation field, never conditioned on the position changing; CommandTeleportParty.OnRun (06009E90) writes Orientation once per listed character after Position, unconditionally; CommandMoveUnit.IsFinished (06009E4E) never writes Orientation; the Orientation setter stores the value as given (and the desired orientation)",
            ()=>{
                var unitType=native.GetType("Kingmaker.EntitySystem.Entities.UnitEntityData",true);
                var a=Body(Token(translocate,"RunAction",0x06008FA2)); var b=Body(Token(move,"IsFinished",0x06009E4E)); var c=Body(Token(party,"OnRun",0x06009E90));
                var setter=Body(unitType.GetProperty("Orientation").GetSetMethod());
                var copy=new List<int>(); for(var i=0;i<a.Count;i++) if(a[i].Op==OpCodes.Ldfld && a[i].Operand is FieldInfo && ((FieldInfo)a[i].Operand).Name=="m_CopyRotation") copy.Add(i);
                var aO=Find(a,"UnitEntityData","set_Orientation"); var aP=Find(a,"UnitEntityData","set_Position");
                var cO=Find(c,"UnitEntityData","set_Orientation"); var cP=Find(c,"UnitEntityData","set_Position");
                Func<List<Ins>,bool> comparesPositions=body=>Find(body,"Vector3","op_Equality").Count+Find(body,"Vector3","op_Inequality").Count+Find(body,"Vector3","Equals").Count>0;
                var setterCalls=0; foreach(var ins in setter) if(ins.Call!=null) setterCalls++;
                var branchAfterCopy=copy.Count==1 && (a[copy[0]+1].Op==OpCodes.Brfalse || a[copy[0]+1].Op==OpCodes.Brfalse_S);
                return aO.Count==1 && copy.Count==1 && branchAfterCopy && aP.Count==2 && aP[0]<copy[0] && copy[0]<aO[0] && !comparesPositions(a) &&
                    Find(b,"UnitEntityData","set_Orientation").Count==0 &&
                    cO.Count==1 && cP.Count==1 && cP[0]<cO[0] && !comparesPositions(c) &&
                    setterCalls==1 && Find(setter,"UnitEntityData","set_DesiredOrientation").Count==1;
            });
        Check("PRODUCT-IL (review R5) the capture records both units' orientations and the attribution classifies with both turns, reorienting a pending placement for a rider-only turn",
            ()=>{
                var attribution=product.GetType("KingmakerMountedCombat.Integration.MountedScenePlacementAttribution",true);
                var capture=Body(attribution.GetMethod("Capture",Any)); var attribute=Body(attribution.GetMethod("Attribute",Any));
                var classify=-1; foreach(var ins in attribute) if(ins.Call!=null && ins.Call.Name=="ClassifyOperationWrite" && ins.Call.GetParameters().Length==6) classify=1;
                return Find(capture,"UnitEntityData","get_Orientation").Count==2 && Find(attribute,"MountedSceneContinuityPolicy","OrientationWritten").Count==2 &&
                    classify==1 && Find(attribute,"RiderMovementAgent","ReorientScenePlacement").Count==1;
            });
        Check("PRODUCT-EXECUTED (review R5) the same endpoint with a final orientation: placement A writes P at 30 degrees; placement B writes the same P at 120 before the poll; through this build's compiled classification (as its attribution composes it) and hold, P/120 is pending",
            ()=>{
                var policy=product.GetType("KingmakerMountedCombat.Domain.MountedSceneContinuityPolicy",true);
                var holdType=product.GetType("KingmakerMountedCombat.Domain.MountedScenePlacementHold",true);
                MethodInfo six=null, four=null;
                foreach(var m in policy.GetMethods()) if(m.Name=="ClassifyOperationWrite"){ if(m.GetParameters().Length==6) six=m; else if(m.GetParameters().Length==4) four=m; }
                var h=Activator.CreateInstance(holdType);
                holdType.GetMethod("Attribute").Invoke(h,new object[]{4f,0f,7f,30f,1});
                string kind;
                if(six!=null)
                {
                    var turned=(bool)policy.GetMethod("OrientationWritten").Invoke(null,new object[]{30f,120f});
                    kind=six.Invoke(null,new object[]{true,true,false,turned,false,false}).ToString();
                }
                else kind=four.Invoke(null,new object[]{true,true,false,false}).ToString();   // positions only, as that attribution reads them
                if(kind=="RiderTurn") holdType.GetMethod("Reorient").Invoke(h,new object[]{120f,2});
                else if(kind=="RiderPlacement") holdType.GetMethod("Attribute").Invoke(h,new object[]{4f,0f,7f,120f,2});
                var orientation=(float)holdType.GetProperty("Orientation").GetValue(h,null);
                var x=(float)holdType.GetProperty("X").GetValue(h,null);
                Console.WriteLine("INFO R5: placement B classified "+kind+"; pending orientation "+orientation);
                return x==4f && orientation==120f;
            });
        Check("PRODUCT-EXECUTED (review R6) real operational failures in the compiled hooks: C04's script-zone hook and C02's scene-placement hooks each report their first fault once and never again, independently; with a logger that throws, each is still attempted once and nothing escapes; every hook is void (native always runs)",
            ()=>{
                var hooks=controller.GetNestedType("PatchMethods",Any); var bridge=controller.GetNestedType("PatchBridge",Any);
                var loggerType=product.GetType("KingmakerMountedCombat.Logging.IModLogger",true);
                var faultType=product.GetType("KingmakerMountedCombat.Domain.MountedBoundedFaultReport",true);
                var relationship=product.GetType("KingmakerMountedCombat.Integration.GameMountedRelationshipService",true);
                var snapshotType=product.GetType("KingmakerMountedCombat.Integration.MountedScenePlacementAttribution",true).GetNestedType("Snapshot",Any);
                var zone=hooks.GetMethod("ScriptZoneTickPostfix",Any); var prefix=hooks.GetMethod("ScenePlacementCapturePrefix",Any);
                var postfix=hooks.GetMethod("TranslocateUnitPostfix",Any);
                var voids=zone.ReturnType==typeof(void) && prefix.ReturnType==typeof(void) && postfix.ReturnType==typeof(void) &&
                    hooks.GetMethod("CommandMoveUnitPostfix",Any).ReturnType==typeof(void) && hooks.GetMethod("CommandTeleportPartyPostfix",Any).ReturnType==typeof(void);
                Action reset=()=>{ foreach(var f in hooks.GetFields(Any)) if(f.IsStatic && f.FieldType==faultType) f.SetValue(null,Activator.CreateInstance(faultType)); };
                Action<string,object> setStatic=(name,value)=>bridge.GetField(name,Any).SetValue(null,value);
                var broken=(IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(snapshotType));
                broken.Add(Activator.CreateInstance(snapshotType,true));   // a captured pair with no owner: attribution fails inside production code
                var escaped=false;
                Action failBoth=()=>{
                    try
                    {
                        zone.Invoke(null,null);
                        prefix.Invoke(null,new object[]{null});
                        postfix.Invoke(null,new object[]{broken});
                    }
                    catch { escaped=true; }
                };
                try
                {
                    // A relationship service with no state: the hooks' own work fails inside production code.
                    setStatic("Service",FormatterServices.GetUninitializedObject(relationship));
                    reset();
                    var recording=new KmcRecordingLogger(loggerType);
                    setStatic("Logger",recording.GetTransparentProxy());
                    for(var i=0;i<4;i++) failBoth();
                    int zoneLines=0, placementLines=0;
                    foreach(var line in recording.Lines){ if(line.Contains("Mounted script zones")) zoneLines++; if(line.Contains("Mounted scene placement")) placementLines++; }
                    reset();
                    var throwing=new KmcThrowingLogger(loggerType);
                    setStatic("Logger",throwing.GetTransparentProxy());
                    for(var i=0;i<4;i++) failBoth();
                    Console.WriteLine("INFO R6: recording logger lines="+recording.Lines.Count+" (script zones "+zoneLines+", scene placement "+placementLines+"); throwing logger calls="+throwing.Calls+"; escaped="+escaped);
                    return voids && !escaped && zoneLines==1 && placementLines==1 && recording.Lines.Count==2 && throwing.Calls==2;
                }
                finally { setStatic("Service",null); setStatic("Logger",null); reset(); }
            });
        Check("PRODUCT-EXECUTED (review R2) the compiled hold keeps the scene's latest placement: a second attribution supersedes the first (endpoint and orientation), a whole-pair placement clears it, a far unattributed write since the hold supersedes it and a near one does not, a release corrects once",
            ()=>{
                var holdType=product.GetType("KingmakerMountedCombat.Domain.MountedScenePlacementHold",true);
                var h=Activator.CreateInstance(holdType);
                Func<string,object> get=n=>holdType.GetProperty(n).GetValue(h,null);
                var attribute=holdType.GetMethod("Attribute"); var clear=holdType.GetMethod("ClearForWholePair");
                var refresh=holdType.GetMethod("Refresh"); var release=holdType.GetMethod("Release"); var sample=holdType.GetMethod("Sample");
                attribute.Invoke(h,new object[]{1.2f,0f,1.6f,30f,1}); attribute.Invoke(h,new object[]{-2f,0f,1.5f,120f,2});
                var latest=(bool)get("Pending") && (float)get("X")==-2f && (float)get("Z")==1.5f && (float)get("Orientation")==120f;
                var cleared=clear.Invoke(h,null).ToString()=="Cleared" && !(bool)get("Pending");
                attribute.Invoke(h,new object[]{6f,0f,0f,0f,3});
                var nearKept=(bool)refresh.Invoke(h,new object[]{4,true,7f,0f,0f,5f,0f,0f}) && (float)get("X")==6f;
                var farTaken=(bool)refresh.Invoke(h,new object[]{5,true,0f,0f,7f,5f,0f,0f}) && (float)get("Z")==7f && (float)get("X")==0f;
                release.Invoke(h,null);
                var correctsOnce=!(bool)sample.Invoke(h,new object[]{6,true,9f,0f,9f,0f,0f,0f,true,9f,9f}) && !(bool)get("Pending");
                return latest && cleared && nearKept && farTaken && correctsOnce;
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
