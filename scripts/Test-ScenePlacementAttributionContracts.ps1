[CmdletBinding()]
param([ValidateSet('Debug','Release')][string]$Configuration='Release',[string]$ProductDll)
# Known issues C02: rider-only scene placements of three metres or less. Also pins the C01 fallback (no staging
# handoff in an encounter) and the C03 combat-hold notice: its wiring, and its delivery, executed through the installed
# native EventBus to a subscribed IWarningNotificationUIHandler sink (the interface the game's warning display implements).
# Review of 263ac3ea: R4 (a notice is spent only once a receiver took it, never during a load), R5 (the installed
# operations' orientation writes, and the scene's final orientation at the same endpoint) and R6 (C02 and C04 have
# independent bounded fault reports), each run against this build and, with -ProductDll, the reviewed one.
# Review of e1c0c7ad: R7 (an exact standalone rider turn is held and handed off), R8 (CommandTeleportParty's proven
# target set makes a whole-pair write even when one actor was already at its authored transform) and R9 (a throwing
# success logger never truncates the pairs of one operation).
# Review of 813d00a1: R10 (scene ownership of the exact operation, captured at entry: a live background player's
# CommandMoveUnit and CommandTeleportParty, and TranslocateUnit inside a cutscene player's parameter scope), with the
# installed cutscene ownership contract pinned and the compiled ownership helper run against represented installed
# player state.
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

// A success-report sink for the compiled attribution core: counts calls and throws when asked (review R9).
public static class KmcReportRelay
{
    public static int Calls;
    public static bool Throw;
    public static void Report<TObservation, TOperation, TChange>(TObservation observation, TOperation operation, TChange change)
    {
        Calls++;
        if (Throw) throw new InvalidOperationException("the logger failed");
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

    // Every method of the installed assembly that calls the given method (call or callvirt), as "Type.Method".
    private static Type[] nativeTypes;
    private static List<string> Callers(Assembly native, MethodBase target)
    {
        if (nativeTypes == null) { try { nativeTypes = native.GetTypes(); } catch (ReflectionTypeLoadException partial) { nativeTypes = partial.Types; } }
        var found = new List<string>();
        foreach (var type in nativeTypes)
        {
            if (type == null) continue;
            foreach (var member in type.GetMembers(All))
            {
                var method = member as MethodBase; if (method == null) continue;
                byte[] il; try { var body = method.GetMethodBody(); if (body == null) continue; il = body.GetILAsByteArray(); } catch { continue; }
                for (var i = 0; i + 4 < il.Length; i++)
                    if ((il[i] == 0x28 || il[i] == 0x6F) && BitConverter.ToInt32(il, i + 1) == target.MetadataToken) { found.Add(type.FullName + "." + method.Name); break; }
            }
        }
        return found;
    }

    // Represented installed cutscene state (review R10): a CutscenePlayerData whose one activated gate has one started track
    // whose current command is the given one; built from the installed types with no constructor run.
    private static object RepresentedPlayer(Assembly native, object command, bool finished, int commandIndex)
    {
        var playerType = native.GetType("Kingmaker.AreaLogic.Cutscenes.CutscenePlayerData", true);
        var gateDataType = playerType.GetNestedType("GateData"); var trackDataType = playerType.GetNestedType("TrackData");
        var gateType = native.GetType("Kingmaker.AreaLogic.Cutscenes.Gate", true); var trackType = native.GetType("Kingmaker.AreaLogic.Cutscenes.Track", true);
        var commandBase = native.GetType("Kingmaker.AreaLogic.Cutscenes.CommandBase", true);
        var track = FormatterServices.GetUninitializedObject(trackType);
        var commands = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(commandBase)); commands.Add(command);
        trackType.GetField("m_Commands", Any).SetValue(track, commands);
        var gate = FormatterServices.GetUninitializedObject(gateType);
        var tracks = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(trackType)); tracks.Add(track);
        gateType.GetField("m_Tracks", Any).SetValue(gate, tracks);
        var gateData = FormatterServices.GetUninitializedObject(gateDataType);
        gateDataType.GetField("Gate").SetValue(gateData, gate);
        var trackData = FormatterServices.GetUninitializedObject(trackDataType);
        trackDataType.GetField("StartGate").SetValue(trackData, gateData);
        trackDataType.GetField("TrackIndex").SetValue(trackData, 0);
        trackDataType.GetField("CommandIndex").SetValue(trackData, commandIndex);
        var started = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(trackDataType)); started.Add(trackData);
        gateDataType.GetField("StartedTracks").SetValue(gateData, started);
        var player = FormatterServices.GetUninitializedObject(playerType);
        var gates = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(gateDataType)); gates.Add(gateData);
        playerType.GetField("m_ActivatedGates", Any).SetValue(player, gates);
        playerType.GetField("<IsFinished>k__BackingField", Any).SetValue(player, finished);
        return player;
    }

    // A compiled MountedScenePlacementObservation (review R7-R9 checks).
    private static object NewObservation(Assembly product, object hold, float rx0, float rz0, float ro0, float rx, float rz, float ro,
        float mx0, float mz0, float mo0, float mx, float mz, float mo, bool riderTargeted, bool mountTargeted)
    {
        var type = product.GetType("KingmakerMountedCombat.Domain.MountedScenePlacementObservation", true);
        var o = Activator.CreateInstance(type);
        Action<string, object> set = (name, value) => type.GetField(name).SetValue(o, value);
        set("Hold", hold); set("PairMounted", true);
        set("RiderX0", rx0); set("RiderZ0", rz0); set("RiderOrientation0", ro0); set("RiderX", rx); set("RiderZ", rz); set("RiderOrientation", ro);
        set("MountX0", mx0); set("MountZ0", mz0); set("MountOrientation0", mo0); set("MountX", mx); set("MountZ", mz); set("MountOrientation", mo);
        set("RiderTargeted", riderTargeted); set("MountTargeted", mountTargeted);
        return o;
    }

    private static object ListOf(Assembly product, object first)
    {
        var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(product.GetType("KingmakerMountedCombat.Domain.MountedScenePlacementObservation", true)));
        list.Add(first);
        return list;
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
        Check("PRODUCT-IL the three operations are patched by their exact tokens, each with its own capture prefix (naming the operation; review R8) and its attribution postfix",
            ()=>Registers(controller,translocate.FullName,0x06008FA2,"TranslocateUnitCapturePrefix","TranslocateUnitPostfix") &&
                Registers(controller,move.FullName,0x06009E4E,"CommandMoveUnitCapturePrefix","CommandMoveUnitPostfix") &&
                Registers(controller,party.FullName,0x06009E90,"CommandTeleportPartyCapturePrefix","CommandTeleportPartyPostfix"));
        Check("PRODUCT-IL each prefix captures the mounted pairs with the exact operation (CommandTeleportParty's also its proven target set); the postfix keeps only the captured mounting instance and synchronizer and hands every pair to the attribution core with no logging of its own (review R9); the synchronizer's samples and the owner's poll both go through the hold (review R2); faults are contained in C02's own bounded report (review R6)",
            ()=>{
                var attribution=product.GetType("KingmakerMountedCombat.Integration.MountedScenePlacementAttribution",true);
                var attribute=Body(attribution.GetMethod("Attribute",Any));
                var agent=product.GetType("KingmakerMountedCombat.Integration.RiderMovementAgent",true);
                var hold=Body(agent.GetMethod("HoldForScenePlacement",Any));
                var current=Body(agent.GetMethod("CurrentScenePlacement",Any));
                var intent=product.GetType("KingmakerMountedCombat.Integration.MountedPersistenceService",true);
                var poll=Body(intent.GetMethod("ObserveScenePlacements",Any));
                var apply=Find(attribute,"MountedScenePlacementAttributionCore","Apply");
                var share=Body(methods.GetMethod("AttributeScenePlacement",Any));
                var prefix=Body(methods.GetMethod("CaptureScenePlacement",Any));
                var partyPrefix=Body(methods.GetMethod("CommandTeleportPartyCapturePrefix",Any));
                var single=Body(methods.GetMethod("TranslocateUnitCapturePrefix",Any));
                return Find(prefix,"MountedScenePlacementAttribution","Take").Count==1 && Find(prefix,"MountedScenePlacementAttribution","TeleportPartyTargets").Count==1 &&
                    Find(partyPrefix,null,"CaptureScenePlacement").Count==1 && Find(single,null,"CaptureScenePlacement").Count==1 &&
                    Find(share,"MountedScenePlacementAttribution","Attribute").Count==1 &&
                    Find(share,null,"ReportScenePlacementFault").Count==1 && Find(prefix,null,"ReportScenePlacementFault").Count==1 &&
                    Find(share,null,"ReportScriptZoneFault").Count==0 && Find(prefix,null,"ReportScriptZoneFault").Count==0 &&
                    apply.Count==1 && Find(attribute,"IModLogger","Info").Count==0 &&
                    Find(attribute,"GameMountedRelationshipService","get_MountingInstance").Count>=1 &&
                    Find(attribute,"GameMountedRelationshipService","get_MountingInstance")[0]<apply[0] &&
                    Find(attribute,"RiderMovementAgent","get_ConfiguredSceneHold").Count==1 &&
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
        Check("PRODUCT-IL (review R5 and R8) the capture records both units' orientations and their membership of the proven target set; the core classifies with both moves, both turns and both target flags, and a rider-only turn goes to the hold's Reorient",
            ()=>{
                var attribution=product.GetType("KingmakerMountedCombat.Integration.MountedScenePlacementAttribution",true);
                var core=product.GetType("KingmakerMountedCombat.Domain.MountedScenePlacementAttributionCore",true);
                var capture=Body(attribution.GetMethod("Take",Any)); var classify=Body(core.GetMethod("Classify"));
                var apply=Body(core.GetMethod("Apply"));
                var eight=false; foreach(var ins in classify) if(ins.Call!=null && ins.Call.Name=="ClassifyOperationWrite" && ins.Call.GetParameters().Length==8) eight=true;
                return Find(capture,"UnitEntityData","get_Orientation").Count==2 && Find(capture,null,"Contains").Count==2 &&
                    Find(classify,"MountedSceneContinuityPolicy","OrientationWritten").Count==2 && eight &&
                    Find(apply,"MountedScenePlacementHold","Reorient").Count==1 && Find(apply,"MountedScenePlacementHold","ClearForWholePair").Count==1;
            });
        Check("NATIVE-IL (review R8) CommandTeleportParty.OnRun (06009E90) reads its m_UnitsList, asks Player.GetCharactersList once and writes Position and Orientation for every unit it yields, with no filter of its own; GetCharactersList reads the live lists (ControllableCharacters, AllCharacters, the cross-scene units, PartyCharacters, DetachedPartyCharacters)",
            ()=>{
                var c=Body(Token(party,"OnRun",0x06009E90));
                var player=native.GetType("Kingmaker.Player",true);
                var list=Body(player.GetMethod("GetCharactersList"));
                var field=-1; for(var i=0;i<c.Count;i++) if(c[i].Op==OpCodes.Ldfld && c[i].Operand is FieldInfo && ((FieldInfo)c[i].Operand).Name=="m_UnitsList") field=i;
                var get=Find(c,"Player","GetCharactersList");
                var calls=new List<string>(); foreach(var ins in c) if(ins.Call!=null) calls.Add(ins.Call.DeclaringType.Name+"."+ins.Call.Name);
                var filters=calls.FindAll(n=>n.Contains("IsPet")||n.Contains("IsDetached")||n.Contains("IsInGame")||n.Contains("Where"));
                Console.WriteLine("INFO R8: OnRun calls "+string.Join(", ",calls.ToArray()));
                return field>=0 && get.Count==1 && field<get[0] && Find(c,null,"MoveNext").Count>=1 && filters.Count==0 &&
                    Find(list,"Player","get_ControllableCharacters").Count==1 && Find(list,"Player","get_AllCharacters").Count==1 &&
                    Find(list,"Player","get_PartyCharacters").Count==1 && Find(list,"Player","get_DetachedPartyCharacters").Count==1 &&
                    Find(list,"Player","get_AllCrossSceneUnits").Count==2;
            });
        Check("PRODUCT-IL (review R8) the CommandTeleportParty prefix passes the operation instance; its target set is the native Player.GetCharactersList of the instance's m_UnitsList field",
            ()=>{
                var attribution=product.GetType("KingmakerMountedCombat.Integration.MountedScenePlacementAttribution",true);
                var targets=Body(attribution.GetMethod("TeleportPartyTargets",Any));
                var prefix=methods.GetMethod("CommandTeleportPartyCapturePrefix",Any);
                var named=false; foreach(var member in attribution.GetMembers(All)){ var m=member as MethodBase; if(m==null||m.GetMethodBody()==null) continue; foreach(var ins in Body(m)) if(ins.Op==OpCodes.Ldstr && (string)ins.Operand=="m_UnitsList") named=true; }
                return prefix.GetParameters()[0].ParameterType==party && prefix.GetParameters()[0].Name=="__instance" &&
                    Find(targets,"Player","GetCharactersList").Count==1 && Find(targets,"FieldInfo","GetValue").Count==1 && named;
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
                var reorient=holdType.GetMethod("Reorient");
                if(kind=="RiderTurn") reorient.Invoke(h,reorient.GetParameters().Length==5 ? new object[]{4f,0f,7f,120f,2} : new object[]{120f,2});
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
                var zone=hooks.GetMethod("ScriptZoneTickPostfix",Any);
                var prefix=hooks.GetMethod("TranslocateUnitCapturePrefix",Any) ?? hooks.GetMethod("ScenePlacementCapturePrefix",Any);
                var postfix=hooks.GetMethod("TranslocateUnitPostfix",Any);
                var voids=zone.ReturnType==typeof(void) && prefix.ReturnType==typeof(void) && postfix.ReturnType==typeof(void) &&
                    hooks.GetMethod("CommandMoveUnitPostfix",Any).ReturnType==typeof(void) && hooks.GetMethod("CommandTeleportPartyPostfix",Any).ReturnType==typeof(void);
                Action reset=()=>{ foreach(var f in hooks.GetFields(Any)) if(f.IsStatic && f.FieldType==faultType) f.SetValue(null,Activator.CreateInstance(faultType)); };
                Action<string,object> setStatic=(name,value)=>bridge.GetField(name,Any).SetValue(null,value);
                var broken=(IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(snapshotType));
                broken.Add(Activator.CreateInstance(snapshotType,true));   // a captured pair with no owner: attribution fails inside production code
                var captureType=product.GetType("KingmakerMountedCombat.Integration.MountedScenePlacementAttribution",true).GetNestedType("Capture",Any);
                object brokenState=broken;
                if(captureType!=null){ brokenState=Activator.CreateInstance(captureType,true); captureType.GetField("Snapshots",Any).SetValue(brokenState,broken); }
                var escaped=false;
                Action failBoth=()=>{
                    try
                    {
                        zone.Invoke(null,null);
                        prefix.Invoke(null,new object[]{null});
                        postfix.Invoke(null,new object[]{brokenState});
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
        Check("PRODUCT-EXECUTED (review R7) nothing pending; an exact operation turns the seated rider to 75 degrees at its own position, the mount unchanged, in a scene: through this build's compiled attribution (as its Attribute composes it) and hold, the rider's position at 75 degrees is pending for the handoff",
            ()=>{
                var holdType=product.GetType("KingmakerMountedCombat.Domain.MountedScenePlacementHold",true);
                var h=Activator.CreateInstance(holdType);
                var core=product.GetType("KingmakerMountedCombat.Domain.MountedScenePlacementAttributionCore");
                string how;
                if(core!=null)
                {
                    var o=NewObservation(product,h,4f,7f,0f,4f,7f,75f,4f,7f,0f,4f,7f,0f,false,false);
                    core.GetMethod("Apply").Invoke(null,new object[]{ListOf(product,o),true,1,null});
                    how="core";
                }
                else
                {
                    // e1c0c7ad: classification of position and orientation changes, and Reorient for a rider-only turn.
                    var policy=product.GetType("KingmakerMountedCombat.Domain.MountedSceneContinuityPolicy",true);
                    MethodInfo six=null; foreach(var m in policy.GetMethods()) if(m.Name=="ClassifyOperationWrite" && m.GetParameters().Length==6) six=m;
                    var kind=six.Invoke(null,new object[]{true,true,false,true,false,false}).ToString();
                    if(kind=="RiderTurn") holdType.GetMethod("Reorient").Invoke(h,new object[]{75f,1});
                    how="reviewed classification "+kind+" and Reorient";
                }
                var pending=(bool)holdType.GetProperty("Pending").GetValue(h,null);
                Console.WriteLine("INFO R7: "+how+"; pending="+pending+(pending?" at ("+holdType.GetProperty("X").GetValue(h,null)+", "+holdType.GetProperty("Z").GetValue(h,null)+") "+holdType.GetProperty("Orientation").GetValue(h,null):""));
                return pending && (float)holdType.GetProperty("X").GetValue(h,null)==4f && (float)holdType.GetProperty("Z").GetValue(h,null)==7f &&
                    (float)holdType.GetProperty("Orientation").GetValue(h,null)==75f;
            });
        Check("PRODUCT-EXECUTED (review R8) with a rider-only placement A pending, CommandTeleportParty targeting both actors: rider already at its authored transform, mount already at its, and both already at theirs each clear the obsolete hold without a new one; a rider-only target set and a single-unit mount operation do not",
            ()=>{
                var holdType=product.GetType("KingmakerMountedCombat.Domain.MountedScenePlacementHold",true);
                var core=product.GetType("KingmakerMountedCombat.Domain.MountedScenePlacementAttributionCore");
                var policy=product.GetType("KingmakerMountedCombat.Domain.MountedSceneContinuityPolicy",true);
                MethodInfo six=null; foreach(var m in policy.GetMethods()) if(m.Name=="ClassifyOperationWrite" && m.GetParameters().Length==6) six=m;
                // A (2, 3) at 30 is pending; the mount stands at (0, 0) at 0. Returns the hold after one operation.
                Func<float,float,float,float,float,float,bool,bool,object> run=(rx,rz,ro,mx,mz,mo,riderTargeted,mountTargeted)=>{
                    var h=Activator.CreateInstance(holdType);
                    holdType.GetMethod("Attribute").Invoke(h,new object[]{2f,0f,3f,30f,1});
                    if(core!=null)
                    {
                        var o=NewObservation(product,h,2f,3f,30f,rx,rz,ro,0f,0f,0f,mx,mz,mo,riderTargeted,mountTargeted);
                        core.GetMethod("Apply").Invoke(null,new object[]{ListOf(product,o),true,2,null});
                    }
                    else
                    {
                        // e1c0c7ad's attribution: changes only, no target evidence.
                        var kind=six.Invoke(null,new object[]{true,true,rx!=2f||rz!=3f,ro!=30f,mx!=0f||mz!=0f,mo!=0f}).ToString();
                        if(kind=="WholePair") holdType.GetMethod("ClearForWholePair").Invoke(h,null);
                        else if(kind=="RiderPlacement") holdType.GetMethod("Attribute").Invoke(h,new object[]{rx,0f,rz,ro,2});
                    }
                    return h;
                };
                Func<object,string> state=h=>(bool)holdType.GetProperty("Pending").GetValue(h,null)
                    ? "pending ("+holdType.GetProperty("X").GetValue(h,null)+", "+holdType.GetProperty("Z").GetValue(h,null)+")" : "cleared";
                var riderAtTarget=state(run(2f,3f,30f,6f,0f,90f,true,true));
                var mountAtTarget=state(run(-1.5f,0f,10f,0f,0f,0f,true,true));
                var bothAtTarget=state(run(2f,3f,30f,0f,0f,0f,true,true));
                var bothChange=state(run(9f,0f,0f,8f,0f,0f,true,true));
                var riderOnlyTargets=state(run(-2f,1.5f,20f,0f,0f,0f,true,false));
                var mountSingle=state(run(2f,3f,30f,2f,0f,0f,false,false));
                Console.WriteLine("INFO R8: rider at target -> "+riderAtTarget+"; mount at target -> "+mountAtTarget+"; both at target -> "+bothAtTarget+
                    "; both change -> "+bothChange+"; rider-only target set -> "+riderOnlyTargets+"; single-unit mount operation -> "+mountSingle);
                return riderAtTarget=="cleared" && mountAtTarget=="cleared" && bothAtTarget=="cleared" && bothChange=="cleared" &&
                    riderOnlyTargets=="pending (-2, 1.5)" && mountSingle=="pending (2, 3)";
            });
        Check("PRODUCT-EXECUTED (review R9) the compiled attribution core over two pairs of one operation with a success logger that throws on the first: both holds are updated, nothing escapes; a healthy logger receives both",
            ()=>{
                var holdType=product.GetType("KingmakerMountedCombat.Domain.MountedScenePlacementHold",true);
                var core=product.GetType("KingmakerMountedCombat.Domain.MountedScenePlacementAttributionCore",true);
                var observationType=product.GetType("KingmakerMountedCombat.Domain.MountedScenePlacementObservation",true);
                var operation=product.GetType("KingmakerMountedCombat.Domain.MountedScenePlacementOperation",true);
                var change=product.GetType("KingmakerMountedCombat.Domain.MountedScenePlacementChange",true);
                var apply=core.GetMethod("Apply");
                var reportType=apply.GetParameters()[3].ParameterType;
                var a=Activator.CreateInstance(holdType); var b=Activator.CreateInstance(holdType);
                var list=ListOf(product,NewObservation(product,a,0f,0f,0f,2f,1f,45f,0f,0f,0f,0f,0f,0f,false,false));
                ((IList)list).Add(NewObservation(product,b,0f,0f,0f,3f,1f,45f,0f,0f,0f,0f,0f,0f,false,false));
                KmcReportRelay.Calls=0; KmcReportRelay.Throw=true;
                var throwing=Delegate.CreateDelegate(reportType,typeof(KmcReportRelay).GetMethod("Report").MakeGenericMethod(observationType,operation,change));
                var escaped=false;
                try { apply.Invoke(null,new object[]{list,true,1,throwing}); } catch { escaped=true; }
                var bothHeld=(bool)holdType.GetProperty("Pending").GetValue(a,null) && (bool)holdType.GetProperty("Pending").GetValue(b,null);
                var throwingCalls=KmcReportRelay.Calls;
                KmcReportRelay.Calls=0; KmcReportRelay.Throw=false;
                var healthyList=ListOf(product,NewObservation(product,Activator.CreateInstance(holdType),0f,0f,0f,2f,1f,45f,0f,0f,0f,0f,0f,0f,false,false));
                ((IList)healthyList).Add(NewObservation(product,Activator.CreateInstance(holdType),0f,0f,0f,3f,1f,45f,0f,0f,0f,0f,0f,0f,false,false));
                apply.Invoke(null,new object[]{healthyList,true,1,throwing});
                Console.WriteLine("INFO R9: throwing logger called "+throwingCalls+" time(s); both holds updated="+bothHeld+"; escaped="+escaped+"; healthy logger calls="+KmcReportRelay.Calls);
                return !escaped && bothHeld && throwingCalls==2 && KmcReportRelay.Calls==2;
            });
        Check("NATIVE-IL (review R10) cutscene command ownership: CommandBase.Run (which runs OnRun) is called only by TrackData.StartCommand and CutscenePlayerData.Restore; CommandBase.IsFinished only by TrackData.Tick, StartCommand and ForceStop; nothing calls CommandMoveUnit.IsFinished or CommandTeleportParty.OnRun directly; StartCommand records the command in m_CommandData before running it; both commands' player parameter is named player; the player's activated gates (m_ActivatedGates) hold GateData whose StartedTracks are TrackData with public Command and IsFinished",
            ()=>{
                var commandBase=native.GetType("Kingmaker.AreaLogic.Cutscenes.CommandBase",true);
                var playerType=native.GetType("Kingmaker.AreaLogic.Cutscenes.CutscenePlayerData",true);
                var trackData=playerType.GetNestedType("TrackData"); var gateData=playerType.GetNestedType("GateData");
                var run=commandBase.GetMethod("Run"); var finished=commandBase.GetMethod("IsFinished");
                var runCallers=Callers(native,run); var finishedCallers=Callers(native,finished);
                var direct=Callers(native,Token(move,"IsFinished",0x06009E4E)).Count+Callers(native,Token(party,"OnRun",0x06009E90)).Count;
                var start=Body(trackData.GetMethod("StartCommand",Any));
                var store=-1; for(var i=0;i<start.Count;i++) if(start[i].Op==OpCodes.Ldfld && start[i].Operand is FieldInfo && ((FieldInfo)start[i].Operand).Name=="m_CommandData") store=i;
                var runAt=Find(start,"CommandBase","Run");
                var gatesField=playerType.GetField("m_ActivatedGates",Any);
                Console.WriteLine("INFO R10: Run callers "+string.Join(", ",runCallers.ToArray())+"; IsFinished callers "+string.Join(", ",finishedCallers.ToArray())+"; direct calls "+direct);
                var expectedRun=new HashSet<string>(StringComparer.Ordinal){"Kingmaker.AreaLogic.Cutscenes.CutscenePlayerData+TrackData.StartCommand","Kingmaker.AreaLogic.Cutscenes.CutscenePlayerData.Restore"};
                var expectedFinished=new HashSet<string>(StringComparer.Ordinal){"Kingmaker.AreaLogic.Cutscenes.CutscenePlayerData+TrackData.ForceStop","Kingmaker.AreaLogic.Cutscenes.CutscenePlayerData+TrackData.StartCommand","Kingmaker.AreaLogic.Cutscenes.CutscenePlayerData+TrackData.Tick"};
                return runCallers.Count==2 && expectedRun.SetEquals(runCallers) && finishedCallers.Count==3 && expectedFinished.SetEquals(finishedCallers) &&
                    direct==0 && store>=0 && runAt.Count==1 && store<runAt[0] &&
                    Token(move,"IsFinished",0x06009E4E).GetParameters()[0].Name=="player" && Token(party,"OnRun",0x06009E90).GetParameters()[0].Name=="player" &&
                    gatesField!=null && gatesField.FieldType==typeof(List<>).MakeGenericType(gateData) &&
                    gateData.GetField("StartedTracks").FieldType==typeof(List<>).MakeGenericType(trackData) &&
                    trackData.GetProperty("Command").GetGetMethod()!=null && trackData.GetProperty("IsFinished").GetGetMethod()!=null;
            });
        Check("NATIVE-IL (review R10) action ownership: the CutsceneParametersContext scope (its Data) is opened only by CutscenePlayerData (Start, TickScene, the Paused setter, Restore, Stop, Dispose); only TickScene names the player in it (set_Cutscene), before opening it and after its paused return; the scope is the native ElementsContext data that actions read",
            ()=>{
                var context=native.GetType("Kingmaker.AreaLogic.Cutscenes.CutsceneParametersContext",true);
                var playerType=native.GetType("Kingmaker.AreaLogic.Cutscenes.CutscenePlayerData",true);
                var opens=Callers(native,context.GetProperty("Data").GetGetMethod()); var names=Callers(native,context.GetProperty("Cutscene").GetSetMethod());
                var tick=Body(playerType.GetMethod("TickScene"));
                var paused=-1; for(var i=0;i<tick.Count;i++) if(tick[i].Op==OpCodes.Ldfld && tick[i].Operand is FieldInfo && ((FieldInfo)tick[i].Operand).Name=="m_Paused") paused=i;
                var setAt=Find(tick,"CutsceneParametersContext","set_Cutscene"); var openAt=Find(tick,"CutsceneParametersContext","get_Data");
                var dataType=context.GetNestedType("ContextData");
                Console.WriteLine("INFO R10: scope opened by "+string.Join(", ",opens.ToArray())+"; named by "+string.Join(", ",names.ToArray()));
                return opens.Count==6 && opens.TrueForAll(n=>n.StartsWith("Kingmaker.AreaLogic.Cutscenes.CutscenePlayerData.")) &&
                    names.Count==1 && names[0]=="Kingmaker.AreaLogic.Cutscenes.CutscenePlayerData.TickScene" &&
                    paused>=0 && setAt.Count==1 && openAt.Count==1 && paused<setAt[0] && setAt[0]<openAt[0] &&
                    dataType.BaseType.FullName=="Kingmaker.ElementsSystem.ElementsContextData";
            });
        Check("INSTALLED-NATIVE AND PRODUCT EXECUTED (review R10) the compiled ownership helper against represented installed player state: a live player whose started track has the command current owns it; a null, finished or unrelated player, a replaced command and a finished track do not; inside an installed CutsceneParametersContext scope (opened through the native ElementsContext) an action is owned, unless the scope names a finished player; outside one it is not",
            ()=>{
                var helper=product.GetType("KingmakerMountedCombat.Integration.NativeSceneOperationOwnership",true);
                var owned=helper.GetMethod("CommandOwnedByLivePlayer",Any); var scoped=helper.GetMethod("ActionInCutsceneScope",Any);
                var command=FormatterServices.GetUninitializedObject(move); var other=FormatterServices.GetUninitializedObject(move);
                Func<object,object,bool> ask=(c,p)=>(bool)owned.Invoke(null,new object[]{c,p});
                var live=RepresentedPlayer(native,command,false,0);
                var results=new Dictionary<string,bool>{
                    {"live",ask(command,live)}, {"null",ask(command,null)}, {"finished",ask(command,RepresentedPlayer(native,command,true,0))},
                    {"replaced",ask(other,live)}, {"unrelated",ask(command,RepresentedPlayer(native,other,false,0))},
                    {"finished track",ask(command,RepresentedPlayer(native,command,false,1))}};
                var context=native.GetType("Kingmaker.AreaLogic.Cutscenes.CutsceneParametersContext",true);
                var ctx=Activator.CreateInstance(context);
                var outside=(bool)scoped.Invoke(null,null);
                bool inStarting, inLive, inFinished;
                using((IDisposable)context.GetProperty("Data").GetValue(ctx,null)) { inStarting=(bool)scoped.Invoke(null,null); }
                context.GetProperty("Cutscene").SetValue(ctx,live,null);
                using((IDisposable)context.GetProperty("Data").GetValue(ctx,null)) { inLive=(bool)scoped.Invoke(null,null); }
                context.GetProperty("Cutscene").SetValue(ctx,RepresentedPlayer(native,command,true,0),null);
                using((IDisposable)context.GetProperty("Data").GetValue(ctx,null)) { inFinished=(bool)scoped.Invoke(null,null); }
                var after=(bool)scoped.Invoke(null,null);
                var text=new List<string>(); foreach(var r in results) text.Add(r.Key+"="+r.Value);
                Console.WriteLine("INFO R10: command ownership "+string.Join(", ",text.ToArray())+"; action scope outside="+outside+", starting="+inStarting+", live="+inLive+", finished="+inFinished+", after="+after);
                return results["live"] && !results["null"] && !results["finished"] && !results["replaced"] && !results["unrelated"] && !results["finished track"] &&
                    !outside && inStarting && inLive && !inFinished && !after;
            });
        Check("PRODUCT-IL (review R10) the commands' prefixes take the original instance and CutscenePlayerData player; the capture decides ownership through the production helper (with the global foreground rule) and stores it in the typed capture before the operation runs; the postfix passes the captured ownership to the core and never reads the global mode itself",
            ()=>{
                var moveP=methods.GetMethod("CommandMoveUnitCapturePrefix",Any); var partyP=methods.GetMethod("CommandTeleportPartyCapturePrefix",Any);
                var playerType=native.GetType("Kingmaker.AreaLogic.Cutscenes.CutscenePlayerData",true);
                Func<MethodInfo,Type,bool> shape=(m,t)=>{ var ps=m.GetParameters(); return ps.Length==3 && ps[0].ParameterType==t && ps[0].Name=="__instance" && ps[1].ParameterType==playerType && ps[1].Name=="player" && ps[2].IsOut; };
                var closureDecides=false;
                foreach(var nested in methods.GetNestedTypes(Any)) foreach(var m in nested.GetMethods(All))
                    if(m.GetMethodBody()!=null && Find(Body(m),"NativeSceneOperationOwnership","Decide").Count==1 && Find(Body(m),"MountedSceneContinuityPolicy","SceneActive").Count==1) closureDecides=true;
                var attribution=product.GetType("KingmakerMountedCombat.Integration.MountedScenePlacementAttribution",true);
                var take=Body(attribution.GetMethod("Take",Any)); var attribute=Body(attribution.GetMethod("Attribute",Any));
                var stored=false; foreach(var ins in take) if(ins.Op==OpCodes.Stfld && ins.Operand is FieldInfo && ((FieldInfo)ins.Operand).Name=="Ownership") stored=true;
                var read=false; foreach(var ins in attribute) if(ins.Op==OpCodes.Ldfld && ins.Operand is FieldInfo && ((FieldInfo)ins.Operand).Name=="Ownership") read=true;
                var owned=Find(attribute,"MountedSceneOperationOwnership","SceneOwned"); var apply=Find(attribute,"MountedScenePlacementAttributionCore","Apply");
                return shape(moveP,move) && shape(partyP,party) && closureDecides && stored && read && owned.Count==1 && apply.Count==1 && owned[0]<apply[0] &&
                    Find(attribute,"MountedSceneContinuityPolicy","SceneActive").Count==0 && Find(attribute,"Game","get_CutsceneLock").Count==0 && Find(attribute,"Game","IsModeActive").Count==0;
            });
        Check("PRODUCT-EXECUTED (review R10) in Default play (no foreground scene), a live background player's operations through this build's compiled ownership and core as its attribution composes them: A a timed-out CommandMoveUnit moving the rider 1.5 m, B a rider-only CommandTeleportParty, C a standalone turn to 75 degrees are each held; a null, finished or replaced player is not; ownership captured at entry survives the player finishing inside the operation",
            ()=>{
                var holdType=product.GetType("KingmakerMountedCombat.Domain.MountedScenePlacementHold",true);
                var core=product.GetType("KingmakerMountedCombat.Domain.MountedScenePlacementAttributionCore",true);
                var helper=product.GetType("KingmakerMountedCombat.Integration.NativeSceneOperationOwnership");
                var sourceType=product.GetType("KingmakerMountedCombat.Domain.MountedScenePlacementSource",true);
                var command=FormatterServices.GetUninitializedObject(move);
                // The ownership this build's attribution uses: the captured decision, or (813d00a1) the global mode at postfix.
                Func<string,object,bool> owned=(source,player)=>{
                    if(helper==null) return false;   // Default play: no lock, no Dialog, no Cutscene mode
                    var ownership=helper.GetMethod("Decide",Any).Invoke(null,new object[]{Enum.Parse(sourceType,source),false,command,player});
                    return (bool)product.GetType("KingmakerMountedCombat.Domain.MountedSceneOperationOwnership",true).GetMethod("SceneOwned").Invoke(null,new object[]{ownership});
                };
                Func<string,object,float,float,float,bool,string> run=(source,player,rx,rz,ro,riderTargeted)=>{
                    var h=Activator.CreateInstance(holdType);
                    var sceneOwned=owned(source,player);
                    var o=NewObservation(product,h,0f,0f,0f,rx,rz,ro,0f,0f,0f,0f,0f,0f,riderTargeted,false);
                    core.GetMethod("Apply").Invoke(null,new object[]{ListOf(product,o),sceneOwned,1,null});
                    return (bool)holdType.GetProperty("Pending").GetValue(h,null)
                        ? "held ("+holdType.GetProperty("X").GetValue(h,null)+", "+holdType.GetProperty("Z").GetValue(h,null)+") "+holdType.GetProperty("Orientation").GetValue(h,null) : "none";
                };
                var live=RepresentedPlayer(native,command,false,0);
                var a=run("CommandMoveUnitTimedOut",live,0.9f,1.2f,0f,false);
                var b=run("CommandTeleportParty",live,-2f,0.5f,0f,true);
                var c=run("CommandTeleportParty",live,0f,0f,75f,true);
                var nul=run("CommandMoveUnitTimedOut",null,0.9f,1.2f,0f,false);
                var fin=run("CommandMoveUnitTimedOut",RepresentedPlayer(native,command,true,0),0.9f,1.2f,0f,false);
                var rep=run("CommandMoveUnitTimedOut",RepresentedPlayer(native,FormatterServices.GetUninitializedObject(move),false,0),0.9f,1.2f,0f,false);
                // Captured at entry, then the player finishes inside the operation.
                var entry=owned("CommandMoveUnitTimedOut",live);
                live.GetType().GetField("<IsFinished>k__BackingField",Any).SetValue(live,true);
                var h2=Activator.CreateInstance(holdType);
                core.GetMethod("Apply").Invoke(null,new object[]{ListOf(product,NewObservation(product,h2,0f,0f,0f,0.9f,1.2f,0f,0f,0f,0f,0f,0f,0f,false,false)),entry,1,null});
                var kept=(bool)holdType.GetProperty("Pending").GetValue(h2,null);
                Console.WriteLine("INFO R10: A move -> "+a+"; B rider-only teleport -> "+b+"; C turn -> "+c+"; null -> "+nul+"; finished -> "+fin+"; replaced -> "+rep+"; captured at entry kept="+kept);
                return a=="held (0.9, 1.2) 0" && b=="held (-2, 0.5) 0" && c=="held (0, 0) 75" && nul=="none" && fin=="none" && rep=="none" && kept;
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
