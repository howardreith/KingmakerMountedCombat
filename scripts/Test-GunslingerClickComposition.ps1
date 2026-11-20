[CmdletBinding()]
param(
    [ValidateSet('Debug','Release')][string]$Configuration='Release',
    # Another build of the product to judge with the same probe. The released 0.2.12 DLL (MVID e64656cd-6fe3-4227-80bf-6a7cb87c3477)
    # must FAIL the combined compositions with the incident's structural-contract exception.
    [string]$ProductDll='',
    # The released Tabletop Rules Expansion (mod id KingmakerGunslinger) 0.0.191 DLL. Never redistributed by this repository: take
    # it from that project's v0.0.191 release package and keep it outside the repository; its SHA-256 is pinned below.
    [string]$GunslingerDll=''
)
# Tabletop Rules Expansion click-approach compatibility (startup repair after 0.2.12).
#   COMPOSITION  the real production transpilers of both mods are registered on ClickUnitHandler.OnClick through the installed
#                Harmony12 shim (HarmonyLib PatchProcessor, UpdateWrapper, patch sorting, MethodPatcher.CreateReplacement and the
#                shared patch state that rebuilds the combined chain on each registration): Mounted Combat alone, the other mod
#                alone, the other mod first, Mounted Combat first. Mounted Combat registers through its own PatchUnitClick; the
#                other mod's Desktop transpiler is registered exactly as its NativeFirearmAttackOrderPatch.Install does (default
#                HarmonyMethod, its mod id as owner). The one step a detached process cannot take is replaced: writing the jump
#                into the live original (HarmonyLib.PatchTools.DetourMethod) for OnClick only, because .NET Framework cannot JIT
#                the original (it calls the Unity InternalCall Component.get_transform). Every transpiler, the ordering and the
#                replacement build run for real. The composed instructions are read back with HarmonyLib's GetCurrentInstructions.
#   EXECUTED     the click command's submission slice of the actual composed instructions (from the router guard to its join)
#                is emitted as a dynamic method (the command local becomes argument 1, the other mod's invocation local
#                argument 2, the container on the stack argument 0) and run with native command objects: the real router,
#                the real routing body and the other mod's real Submit. Substituted engine boundaries: native
#                UnitCommands.Run(UnitCommand) records and does not run the native body; the ground-order admission
#                (PatchMethods.GroundCommandPrefix/Postfix, unchanged by this repair) is replaced by one that sends the rider's
#                order to that pair's mount; units, containers and the relationship world are uninitialised native and product
#                objects with only the fields the path reads. Not native gameplay evidence.
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$lab=[IO.Path]::GetFullPath((Join-Path $repo '../..'))
$layout=(Get-Content -Raw (Join-Path $lab 'environment-intake.json') | ConvertFrom-Json).requestedLayout
$managed=Join-Path $layout.kingmakerInstallDir 'Kingmaker_Data/Managed'
$dll=if([string]::IsNullOrWhiteSpace($ProductDll)){Join-Path $repo "bin/$Configuration/KingmakerMountedCombat.dll"}else{[IO.Path]::GetFullPath($ProductDll)}
$gun=if([string]::IsNullOrWhiteSpace($GunslingerDll)){Join-Path $lab 'compatibility-binaries/KingmakerGunslinger-0.0.191/KingmakerGunslinger.dll'}else{[IO.Path]::GetFullPath($GunslingerDll)}
if(-not(Test-Path -LiteralPath $dll -PathType Leaf)){Write-Output "The product build is missing: $dll";exit 1}
if(-not(Test-Path -LiteralPath $gun -PathType Leaf)){Write-Output "The Tabletop Rules Expansion 0.0.191 DLL is missing: $gun (download the v0.0.191 release package from howardreith/KingmakerGunslingerAndOtherTabletopRules, verify it, and extract KingmakerGunslinger/KingmakerGunslinger.dll there)";exit 1}
$pins=[ordered]@{
    'Assembly-CSharp'=@((Join-Path $managed 'Assembly-CSharp.dll'),'3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb')
    'KingmakerGunslinger 0.0.191'=@($gun,'7c3b0324f40e32b0a476c24e9e66a63a2ecefd8908dc3902bed8ec81a5d87287')
}
foreach($name in $pins.Keys){
    $actual=(Get-FileHash -Algorithm SHA256 -LiteralPath $pins[$name][0]).Hash.ToLowerInvariant()
    if($actual -cne $pins[$name][1]){Write-Output "Identity mismatch for ${name}: $actual (expected $($pins[$name][1]))";exit 1}
    Write-Output "INFO $name SHA-256 $actual"
}
foreach($loader in 'UnityModManager.dll','0Harmony12.dll','0Harmony.dll'){
    $path=Join-Path $managed "UnityModManager/$loader"; $item=Get-Item -LiteralPath $path
    Write-Output ("INFO {0} {1} SHA-256 {2}" -f $loader,$item.VersionInfo.ProductVersion,(Get-FileHash -Algorithm SHA256 -LiteralPath $path).Hash.ToLowerInvariant())
}
Write-Output ("INFO product {0} SHA-256 {1}" -f $dll,(Get-FileHash -Algorithm SHA256 -LiteralPath $dll).Hash.ToLowerInvariant())
$refs=@((Join-Path $managed 'Assembly-CSharp.dll'),(Join-Path $managed 'UnityEngine.CoreModule.dll'),(Join-Path $managed 'UnityEngine.UI.dll'),(Join-Path $managed 'UnityEngine.dll'),(Join-Path $managed 'UnityModManager/0Harmony12.dll'),
    (Join-Path $managed 'UnityModManager/0Harmony.dll'),'System.Core')
Add-Type -TypeDefinition @'
using System; using System.IO; using System.Reflection;
public static class KmcGunslingerResolve
{
    public static void Init(string[] directories)
    {
        AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
        {
            var name = new AssemblyName(args.Name).Name;
            foreach (var directory in directories) { var path = Path.Combine(directory, name + ".dll"); if (File.Exists(path)) return Assembly.LoadFrom(path); }
            return null;
        };
    }
}
'@
[KmcGunslingerResolve]::Init(@($managed,(Join-Path $managed 'UnityModManager'),(Split-Path $dll),(Split-Path $gun)))
Add-Type -ReferencedAssemblies $refs -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;

// Engine boundaries a detached process cannot run (see the header of Test-GunslingerClickComposition.ps1).
public static class KmcGunslingerBoundary
{
    public static MethodBase DetourTarget;
    public static int DetoursSkipped;
    public static bool SkipDetour(MethodBase method) { if (DetourTarget == null || method != DetourTarget) return true; DetoursSkipped++; return false; }

    public sealed class Submission { public UnitCommands Container; public UnitCommand Command; public object Invocation; public string Via; }
    public static readonly List<Submission> Log = new List<Submission>();
    public static UnitCommands ThrowFor;
    public static int Postfixes;
    public static bool AdmissionThrows;
    public static Func<UnitEntityData, UnitEntityData> MountOf;

    // Native UnitCommands.Run(UnitCommand): records; native Init's executor assignment is kept so a hand-off is observable.
    public static bool NativeRun(UnitCommands __instance, UnitCommand cmd)
    {
        Log.Add(new Submission { Container = __instance, Command = cmd, Via = "native Run" });
        if (cmd != null && cmd.Executor == null)
            typeof(UnitCommand).GetField("<Executor>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(cmd, typeof(UnitCommands).GetField("m_Owner", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance));
        if (ThrowFor != null && ReferenceEquals(__instance, ThrowFor)) throw new InvalidOperationException("probe: the native run failed");
        return false;
    }
    // The other mod's Submit runs for real; this only records each call before it.
    public static void SubmitSeen(UnitCommands commands, UnitCommand command, object invocation)
    { Log.Add(new Submission { Container = commands, Command = command, Invocation = invocation, Via = "Submit" }); }
    // Ground-order admission substitute: the rider's order goes to that pair's mount, as admitted.
    public static bool Admission(ref UnitEntityData unit, ref bool __result)
    {
        if (AdmissionThrows) throw new InvalidOperationException("probe: the admission failed");
        var mount = MountOf == null ? null : MountOf(unit);
        if (mount != null) unit = mount;
        __result = true;
        return false;
    }
    public static bool AdmissionCompleted() { Postfixes++; return false; }
    // No live game in a detached process: Game.Instance is null (its type initializer builds the whole game).
    public static bool NoLiveGame(ref Kingmaker.Game __result) { __result = null; return false; }

    // An unknown rewrite of the click command's submission (diagnostic check).
    public static void ForeignSubmit(UnitCommands commands, UnitCommand command) { commands.Run(command); }
    public static IEnumerable<Harmony12.CodeInstruction> ForeignTranspiler(IEnumerable<Harmony12.CodeInstruction> source)
    {
        var code = source.ToList();
        var run = typeof(UnitCommands).GetMethod("Run", new[] { typeof(UnitCommand) });
        var move = typeof(UnitMoveTo).GetConstructor(new[] { typeof(UnityEngine.Vector3), typeof(float) });
        var construction = code.FindIndex(i => i.opcode == OpCodes.Newobj && Equals(i.operand, move));
        var site = code.FindIndex(construction, i => i.opcode == OpCodes.Callvirt && Equals(i.operand, run));
        code[site].opcode = OpCodes.Call; code[site].operand = typeof(KmcGunslingerBoundary).GetMethod("ForeignSubmit");
        return code;
    }
}

// The product's logger interface, recorded (a fault's text is printed when a check fails).
public sealed class KmcGunslingerLog : System.Runtime.Remoting.Proxies.RealProxy
{
    public readonly List<string> Lines = new List<string>();
    public KmcGunslingerLog(Type contract) : base(contract) { }
    public override System.Runtime.Remoting.Messaging.IMessage Invoke(System.Runtime.Remoting.Messaging.IMessage message)
    {
        var call = (System.Runtime.Remoting.Messaging.IMethodCallMessage)message;
        Lines.Add(call.MethodName + ": " + string.Join(" | ", call.InArgs.Select(a => a == null ? "null" : a.ToString()).ToArray()));
        return new System.Runtime.Remoting.Messaging.ReturnMessage(null, null, 0, call.LogicalCallContext, call);
    }
}

public static class KmcGunslingerProbe
{
    static KmcGunslingerLog productLog;
    const BindingFlags All = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    const string MountedOwner = "KingmakerMountedCombat.Feasibility", OtherOwner = "KingmakerGunslinger";
    static int passes, failures;
    static Assembly product, other;
    static MethodBase click;
    static Type controller, methods, bridge;

    static void Check(string label, Func<bool> body)
    {
        string detail = null; var ok = false;
        if (productLog != null) productLog.Lines.Clear();
        try { ok = body(); }
        catch (Exception error) { var inner = Root(error); detail = inner.GetType().Name + ": " + inner.Message; }
        if (ok) { passes++; Console.WriteLine("PASS " + label); }
        else
        {
            failures++; Console.WriteLine("FAIL " + label + (detail != null ? " [" + detail + "]" : ""));
            if (productLog != null) foreach (var line in productLog.Lines) Console.WriteLine("INFO product log: " + line.Substring(0, Math.Min(line.Length, 900)).Replace((char)13, ' ').Replace((char)10, ' '));
        }
    }
    static Exception Root(Exception error) { while (error is TargetInvocationException && error.InnerException != null) error = error.InnerException; return error; }
    static bool Every(string tag, params bool[] parts)
    {
        var failed = new List<string>(); for (var i = 0; i < parts.Length; i++) if (!parts[i]) failed.Add((i + 1).ToString());
        if (failed.Count > 0) Console.WriteLine("INFO " + tag + " failing parts: " + string.Join(",", failed.ToArray()));
        return failed.Count == 0;
    }
    static object Blank(Type type) { return FormatterServices.GetUninitializedObject(type); }
    static FieldInfo Field(Type type, string name) { for (var t = type; t != null; t = t.BaseType) { var f = t.GetField(name, All); if (f != null) return f; } throw new MissingFieldException(type.Name, name); }
    static void Set(object target, string name, object value) { Field(target.GetType(), name).SetValue(target, value); }

    // ---- registration through the installed loader ----------------------------------------------------------------
    sealed class Outcome { public string Mounted, Other; public List<Harmony12.Patch> Transpilers = new List<Harmony12.Patch>(); public List<string> Owners = new List<string>();
        public List<HarmonyLib.CodeInstruction> Composed; public Exception ComposeError; }

    static string RegisterMounted(Harmony12.HarmonyInstance harmony)
    {
        try
        {
            var instance = Blank(controller); Field(controller, "harmony").SetValue(instance, harmony);
            var own = controller.GetMethod("PatchUnitClick", All);
            if (own != null) own.Invoke(instance, null);
            else controller.GetMethod("PatchExact", All).Invoke(instance, new object[] { click.DeclaringType, "OnClick", 0x060093ED,
                new[] { typeof(UnityEngine.GameObject), typeof(UnityEngine.Vector3), typeof(int), typeof(bool), typeof(bool) }, "UnitClickPrefix", null, "CannotAttackApproachTranspiler" });
            return null;
        }
        catch (Exception error) { var inner = Root(error); return inner.GetType().Name + ": " + inner.Message; }
    }
    static string RegisterOther(Harmony12.HarmonyInstance harmony)
    {
        try { harmony.Patch(click, null, null, new Harmony12.HarmonyMethod(other.GetType("KingmakerGunslinger.Firing.NativeFirearmAttackOrderPatch", true).GetMethod("Desktop", All))); return null; }
        catch (Exception error) { var inner = Root(error); return inner.GetType().Name + ": " + inner.Message; }
    }
    static Outcome Compose(string order, Action<Harmony12.HarmonyInstance> before = null, string name = null)
    {
        var mounted = Harmony12.HarmonyInstance.Create(MountedOwner);
        var gun = Harmony12.HarmonyInstance.Create(OtherOwner);
        var foreign = Harmony12.HarmonyInstance.Create("probe.foreign");
        var outcome = new Outcome();
        try
        {
            if (before != null) before(foreign);
            foreach (var step in order)
            {
                if (step == 'M') outcome.Mounted = RegisterMounted(mounted) ?? "registered";
                else outcome.Other = RegisterOther(gun) ?? "registered";
            }
            var info = mounted.GetPatchInfo(click);
            if (info != null) { outcome.Transpilers.AddRange(info.Transpilers); outcome.Owners.AddRange(info.Owners); }
            try { outcome.Composed = HarmonyLib.PatchProcessor.GetCurrentInstructions(click, int.MaxValue, null); }
            catch (Exception error) { outcome.ComposeError = Root(error); }
            Console.WriteLine("INFO " + (name ?? order) + ": Mounted Combat " + (outcome.Mounted ?? "-") + "; other mod " + (outcome.Other ?? "-") + "; chain " +
                string.Join(" -> ", outcome.Transpilers.OrderByDescending(p => p.priority).ThenBy(p => p.index).Select(p => p.owner + "(" + p.priority + "," + p.index + ")").ToArray()));
            return outcome;
        }
        finally { mounted.UnpatchAll(MountedOwner); gun.UnpatchAll(OtherOwner); foreign.UnpatchAll("probe.foreign"); }
    }

    static string Show(HarmonyLib.CodeInstruction i)
    {
        var method = i.operand as MethodBase; var local = i.operand as LocalVariableInfo;
        if (method != null) return i.opcode + " " + method.DeclaringType.FullName + "::" + method.Name;
        if (local != null) return i.opcode + " local:" + local.LocalType.FullName + (local.LocalType == typeof(UnitCommand) ? "#" + local.LocalIndex : "");
        if (i.operand is Label) return i.opcode + " label";
        return i.opcode + (i.operand == null ? "" : " " + i.operand);
    }
    // A listing that also carries branch wiring and exception blocks: every label is numbered by first appearance (as an
    // instruction's label or as an operand), so two listings are equal only when every branch reaches the same place.
    static List<string> Canonical(IEnumerable<HarmonyLib.CodeInstruction> code)
    {
        var numbers = new Dictionary<Label, int>();
        Func<Label, string> number = l => { int n; if (!numbers.TryGetValue(l, out n)) numbers[l] = n = numbers.Count; return "L" + n; };
        var lines = new List<string>();
        foreach (var i in code)
        {
            var name = i.opcode.Name; if (name.EndsWith(".s")) name = name.Substring(0, name.Length - 2);
            var text = string.Join("", i.labels.Select(l => number(l) + ": ").ToArray()) +
                string.Join("", i.blocks.Select(b => "[" + b.blockType + "] ").ToArray()) + name;
            if (i.operand is Label) text += " " + number((Label)i.operand);
            else if (i.operand is Label[]) text += " " + string.Join(",", ((Label[])i.operand).Select(number).ToArray());
            else { var shown = Show(i); text += shown.Substring(i.opcode.Name.Length); }
            lines.Add(text);
        }
        return lines;
    }
    static bool IsCall(HarmonyLib.CodeInstruction i, string type, string name)
    { var m = i.operand as MethodBase; return m != null && m.Name == name && m.DeclaringType.FullName == type; }
    static int Count(List<HarmonyLib.CodeInstruction> code, string type, string name) { return code.Count(i => IsCall(i, type, name)); }
    const string NativeCommands = "Kingmaker.UnitLogic.Commands.UnitCommands", OtherOrder = "KingmakerGunslinger.Firing.NativeFirearmAttackOrder";
    const string Methods = "KingmakerMountedCombat.Integration.MountedPatchController+PatchMethods";

    // The click command's guarded submission in the composed instructions: dup; ldloc command; call router; brtrue h;
    // ldloc command; <tail>; br d; h: pop; d: nop.
    sealed class Site { public int Start, Router, TailStart, TailEnd, Join; public List<HarmonyLib.CodeInstruction> Code; }
    static Site FindSite(List<HarmonyLib.CodeInstruction> code)
    {
        var routers = Enumerable.Range(0, code.Count).Where(i => IsCall(code[i], Methods, "ClickApproachRouted")).ToList();
        if (routers.Count != 1) return null;
        var r = routers[0];
        var s = new Site { Code = code, Router = r, Start = r - 2, TailStart = r + 2 };
        var skip = (Label)code[r + 1].operand;
        var brIndex = code.FindIndex(r + 2, i => i.opcode == OpCodes.Br);
        var done = (Label)code[brIndex].operand;
        s.TailEnd = brIndex - 1;
        s.Join = code.FindIndex(brIndex, i => i.labels.Contains(done));
        if (!(code[s.Start].opcode == OpCodes.Dup && code[r + 1].opcode == OpCodes.Brtrue && code[brIndex + 1].opcode == OpCodes.Pop &&
              code[brIndex + 1].labels.Contains(skip) && s.Join == brIndex + 2 && code[s.Join].opcode == OpCodes.Nop)) return null;
        return s;
    }

    // ---- executed slice ---------------------------------------------------------------------------------------------
    static Delegate EmitSlice(Site site)
    {
        var code = site.Code;
        var commandLocal = (LocalVariableInfo)code[site.Start + 1].operand;
        Type invocationType = typeof(object);
        for (var i = site.Start; i <= site.Join; i++) { var l = code[i].operand as LocalVariableInfo; if (l != null && l.LocalType.FullName == OtherOrder + "+InputInvocation") invocationType = l.LocalType; }
        var method = new DynamicMethod("ComposedClickSubmission", typeof(void), new[] { typeof(UnitCommands), typeof(UnitCommand), invocationType }, typeof(KmcGunslingerProbe).Module, true);
        var il = method.GetILGenerator();
        var labels = new Dictionary<Label, Label>();
        Func<Label, Label> map = l => { Label m; if (!labels.TryGetValue(l, out m)) labels[l] = m = il.DefineLabel(); return m; };
        il.Emit(OpCodes.Ldarg_0);
        for (var i = site.Start; i <= site.Join; i++)
        {
            var ins = code[i];
            foreach (var l in ins.labels) il.MarkLabel(map(l));
            var local = ins.operand as LocalVariableInfo;
            if (local != null)
            {
                if (local.LocalIndex == commandLocal.LocalIndex && local.LocalType == commandLocal.LocalType) il.Emit(OpCodes.Ldarg_1);
                else if (local.LocalType == invocationType) il.Emit(OpCodes.Ldarg_2);
                else throw new InvalidOperationException("an unexpected local in the submission slice: " + Show(ins));
            }
            else if (ins.operand is Label) il.Emit(ins.opcode, map((Label)ins.operand));
            else if (ins.operand is MethodInfo) il.Emit(ins.opcode, (MethodInfo)ins.operand);
            else if (ins.operand == null) il.Emit(ins.opcode);
            else throw new InvalidOperationException("an unexpected operand in the submission slice: " + Show(ins));
        }
        il.Emit(OpCodes.Ret);
        var delegateType = System.Linq.Expressions.Expression.GetActionType(typeof(UnitCommands), typeof(UnitCommand), invocationType);
        return method.CreateDelegate(delegateType);
    }

    sealed class Pair { public UnitEntityData Rider, Mount; public object Relationship; }
    static UnitEntityData Unit()
    {
        var unit = (UnitEntityData)Blank(typeof(UnitEntityData));
        var commands = (UnitCommands)Blank(typeof(UnitCommands));
        Set(commands, "m_Owner", unit); Set(unit, "<Commands>k__BackingField", commands);
        return unit;
    }
    static Pair NewPair(string state, long generation)
    {
        var p = new Pair { Rider = Unit(), Mount = Unit() };
        var relationshipType = product.GetType("KingmakerMountedCombat.Integration.GameMountedRelationshipService", true);
        var relationship = Blank(relationshipType);
        var runtime = Blank(Field(relationshipType, "runtime").FieldType); Set(runtime, "rider", p.Rider); Set(runtime, "mount", p.Mount);
        var coordinator = Blank(Field(relationshipType, "coordinator").FieldType);
        Set(coordinator, "<State>k__BackingField", Enum.Parse(product.GetType("KingmakerMountedCombat.Domain.RelationshipState", true), state));
        Set(relationship, "runtime", runtime); Set(relationship, "coordinator", coordinator); Set(relationship, "mountedPairGeneration", generation);
        var ownerType = Field(relationshipType, "<PairOwner>k__BackingField").FieldType;
        var owner = Blank(ownerType); Set(owner, "Rider", p.Rider); Set(owner, "Mount", p.Mount);
        Set(relationship, "<PairOwner>k__BackingField", owner);
        p.Relationship = relationship;
        return p;
    }
    static void InstallWorld(params Pair[] pairs)
    {
        var worldType = product.GetType("KingmakerMountedCombat.Integration.MountedWorldService", true);
        var servicesType = product.GetType("KingmakerMountedCombat.Integration.MountedPairServices", true);
        var world = Blank(worldType);
        var list = (System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(servicesType));
        foreach (var p in pairs)
        {
            var s = Blank(servicesType); Set(s, "Relationship", p.Relationship); Set(s, "AdmissionRider", p.Rider); Set(s, "AdmissionMount", p.Mount);
            list.Add(s); Set(p.Relationship, "<World>k__BackingField", world);
        }
        Set(world, "pairs", list);
        // A real registry whose actor index holds each pair's owner (its boundary is that pair's relationship), so
        // ForActor resolves through it, as for an admitted pair, and never reads the live game.
        var registryType = Field(worldType, "Registry").FieldType;
        Func<UnitEntityData, string> id = unit => "probe-" + System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(unit);
        var registry = registryType.GetConstructors(All)[0].Invoke(new object[] { id });
        var actors = (System.Collections.IDictionary)Field(registryType, "actors").GetValue(registry);
        foreach (var p in pairs)
        {
            var owner = Field(p.Relationship.GetType(), "<PairOwner>k__BackingField").GetValue(p.Relationship);
            Set(owner, "Boundary", p.Relationship);
            actors[id(p.Rider)] = owner; actors[id(p.Mount)] = owner;
        }
        Set(world, "Registry", registry);
        bridge.GetField("Service", All).SetValue(null, pairs.Length == 0 ? null : pairs[0].Relationship);
        KmcGunslingerBoundary.MountOf = unit => { foreach (var p in pairs) if (ReferenceEquals(p.Rider, unit)) return p.Mount; return null; };
    }
    static UnitMoveTo Move()
    {
        return (UnitMoveTo)typeof(UnitMoveTo).GetConstructor(new[] { typeof(UnityEngine.Vector3), typeof(float) }).Invoke(new object[] { new UnityEngine.Vector3(1f, 0f, 2f), 1.1f });
    }
    static object NewInvocation(bool simulated)
    {
        var order = other.GetType(OtherOrder, true);
        var invocation = order.GetMethod("BeginInput", All).Invoke(null, null);
        order.GetMethod("SetInputSimulation", All).Invoke(null, new object[] { invocation, simulated });
        return invocation;
    }
    static int SubmittedProposals(object invocation)
    { return ((System.Collections.ICollection)invocation.GetType().GetField("Submitted", All).GetValue(invocation)).Count; }
    static object OtherBinding(UnitCommand command)
    { return other.GetType(OtherOrder, true).GetMethod("Get", All).Invoke(null, new object[] { command }); }
    static long Routed() { return (long)methods.GetField("RiderClickApproachRoutedCount", All).GetValue(null); }

    // The routing hook's bounded fault report: a check states whether the routing faulted, so no outcome passes through the
    // fault fallback by accident.
    static object FaultReport() { return methods.GetField("markerFault", All).GetValue(null); }
    static bool Faulted() { var report = FaultReport(); return (bool)report.GetType().GetProperty("Reported").GetValue(report, null); }
    static void Reset() { var report = FaultReport(); Field(report.GetType(), "<Reported>k__BackingField").SetValue(report, false); KmcGunslingerBoundary.Log.Clear(); KmcGunslingerBoundary.ThrowFor = null; KmcGunslingerBoundary.Postfixes = 0; KmcGunslingerBoundary.AdmissionThrows = false;
        typeof(Kingmaker.Controllers.Clicks.PointerController).GetField("SimulatingClick", All).SetValue(null, false); }
    static string Trace() { return string.Join(", ", KmcGunslingerBoundary.Log.Select(s => s.Via + "(" + (s.Command == null ? "null" : s.Command.GetType().Name) + ")").ToArray()); }

    public static int Run(string managed, string productPath, string otherPath)
    {
        var native = typeof(UnitEntityData).Assembly;
        product = Assembly.LoadFrom(productPath); other = Assembly.LoadFrom(otherPath);
        Console.WriteLine("INFO product MVID " + product.ManifestModule.ModuleVersionId + "; other mod MVID " + other.ManifestModule.ModuleVersionId +
            " (" + other.GetName().Version + "); Harmony12 " + typeof(Harmony12.HarmonyInstance).Assembly.GetName().Version + " over HarmonyLib " + typeof(HarmonyLib.CodeInstruction).Assembly.GetName().Version);
        controller = product.GetType("KingmakerMountedCombat.Integration.MountedPatchController", true);
        methods = controller.GetNestedType("PatchMethods", All);
        bridge = controller.GetNestedType("PatchBridge", All);
        click = native.GetType("Kingmaker.Controllers.Clicks.Handlers.ClickUnitHandler", true).GetMethods(All).First(m => m.MetadataToken == 0x060093ED);
        var boundary = Harmony12.HarmonyInstance.Create("probe.boundary");
        boundary.Patch(typeof(HarmonyLib.CodeInstruction).Assembly.GetType("HarmonyLib.PatchTools", true).GetMethod("DetourMethod", All),
            new Harmony12.HarmonyMethod(typeof(KmcGunslingerBoundary).GetMethod("SkipDetour")), null, null);
        KmcGunslingerBoundary.DetourTarget = click;
        try
        {
            var original = HarmonyLib.PatchProcessor.GetOriginalInstructions(click, null);
            var M = Compose("M"); var G = Compose("G"); var GM = Compose("GM"); var MG = Compose("MG");
            Func<Outcome, bool> composed = o => o.Composed != null;

            Check("COMPOSITION Mounted Combat alone: registers through PatchUnitClick; the click command's one native Run is guarded by the router and kept; the loot and interact Runs are unchanged; nothing else changes",
                () => {
                    var site = composed(M) ? FindSite(M.Composed) : null;
                    // Remove the seven guard instructions, give the guard's labels back to the load, and the rest must equal the
                    // original exactly, branch wiring and exception blocks included (Harmony writes branches in long form,
                    // so forms are compared, not encodings).
                    var unchanged = false;
                    if (site != null)
                    {
                        var stripped = new List<HarmonyLib.CodeInstruction>();
                        for (var i = 0; i < M.Composed.Count; i++)
                        {
                            if (i == site.Start || i == site.Start + 1 || i == site.Router || i == site.Router + 1 || i > site.TailEnd && i <= site.Join) continue;
                            var copy = new HarmonyLib.CodeInstruction(M.Composed[i].opcode, M.Composed[i].operand); // Clone() drops labels and blocks
                            copy.labels.AddRange(M.Composed[i].labels); copy.blocks.AddRange(M.Composed[i].blocks);
                            if (i == site.TailStart) copy.labels.AddRange(M.Composed[site.Start].labels);
                            stripped.Add(copy);
                        }
                        var a1 = Canonical(original); var a2 = Canonical(stripped);
                        unchanged = a1.SequenceEqual(a2);
                        if (!unchanged) for (var i = 0; i < Math.Min(a1.Count, a2.Count); i++) if (a1[i] != a2[i]) { Console.WriteLine("INFO first difference at " + i + ": [" + a1[i] + "] (" + a1[i].Length + ", " + original[i].operand.GetType().Name + ") / [" + a2[i] + "] (" + a2[i].Length + ", " + stripped[i].operand.GetType().Name + ")"); break; }
                    }
                    return Every("M", M.Mounted == "registered", M.Owners.SequenceEqual(new[] { MountedOwner }), site != null,
                        site != null && site.TailEnd == site.TailStart + 1 && IsCall(M.Composed[site.TailEnd], NativeCommands, "Run"),
                        Count(M.Composed, NativeCommands, "Run") == 3, unchanged, M.Composed.Count == original.Count + 7);
                });
            Check("COMPOSITION the other mod alone: its Desktop transpiler registers and rewrites all three native Runs to its Submit with its input invocation",
                () => Every("G", G.Other == "registered", G.Owners.SequenceEqual(new[] { OtherOwner }), composed(G),
                    composed(G) && Count(G.Composed, OtherOrder, "Submit") == 3 && Count(G.Composed, NativeCommands, "Run") == 0 && Count(G.Composed, Methods, "ClickApproachRouted") == 0));
            Check("COMPOSITION the other mod registered first (the incident's load order): Mounted Combat registers; Harmony rebuilds the chain other-mod -> Mounted Combat; the router guards the other mod's Submit of the click command, which keeps its invocation argument; its three Submits remain",
                () => {
                    var site = composed(GM) ? FindSite(GM.Composed) : null;
                    var chain = GM.Transpilers.OrderByDescending(p => p.priority).ThenBy(p => p.index).Select(p => p.owner).ToArray();
                    return Every("GM", GM.Other == "registered", GM.Mounted == "registered", chain.SequenceEqual(new[] { OtherOwner, MountedOwner }), site != null,
                        site != null && site.TailEnd == site.TailStart + 2 && IsCall(GM.Composed[site.TailEnd], OtherOrder, "Submit") &&
                            ((LocalVariableInfo)GM.Composed[site.TailStart + 1].operand).LocalType.FullName == OtherOrder + "+InputInvocation",
                        Count(GM.Composed, OtherOrder, "Submit") == 3, Count(GM.Composed, NativeCommands, "Run") == 0, GM.Composed.Count == G.Composed.Count + 7);
                });
            Check("COMPOSITION Mounted Combat registered first: the other mod still finds its three native Runs (Mounted Combat no longer consumes one); Harmony's rebuilt chain is Mounted Combat -> other mod; the composed method equals the other order's instruction for instruction",
                () => {
                    var chain = MG.Transpilers.OrderByDescending(p => p.priority).ThenBy(p => p.index).Select(p => p.owner).ToArray();
                    return Every("MG", MG.Mounted == "registered", MG.Other == "registered", chain.SequenceEqual(new[] { MountedOwner, OtherOwner }),
                        composed(MG) && FindSite(MG.Composed) != null, composed(MG) && composed(GM) && Canonical(MG.Composed).SequenceEqual(Canonical(GM.Composed)));
                });
            Check("COMPOSITION each registration builds the replacement through HarmonyLib (the detour into the live original is the only step skipped, once per build)",
                () => KmcGunslingerBoundary.DetoursSkipped >= 6);
            Check("DIAGNOSTIC an unknown rewrite of the click command's submission (owner probe.foreign) makes Mounted Combat's registration fail, naming the failed expectation, what now uses the command local, the method and the owner that changed it first; Harmony records no Mounted Combat patch",
                () => {
                    var X = Compose("M", foreign => foreign.Patch(click, null, null, new Harmony12.HarmonyMethod(typeof(KmcGunslingerBoundary).GetMethod("ForeignTranspiler"))), "foreign rewrite, then M");
                    Console.WriteLine("INFO diagnostic: " + X.Mounted);
                    return Every("X", X.Mounted != null && X.Mounted.Contains("Harmony could not patch Kingmaker.Controllers.Clicks.Handlers.ClickUnitHandler.OnClick"),
                        X.Mounted != null && X.Mounted.Contains("structural contract mismatch: one submission of the click command's local after the construction"),
                        X.Mounted != null && X.Mounted.Contains("found 0"), X.Mounted != null && X.Mounted.Contains("KmcGunslingerBoundary.ForeignSubmit"),
                        X.Mounted != null && X.Mounted.Contains("owners probe.foreign"), X.Owners.SequenceEqual(new[] { "probe.foreign" }));
                });

            // ---- executed slices of the composed methods ----
            var siteGM = composed(GM) ? FindSite(GM.Composed) : null;
            var siteM = composed(M) ? FindSite(M.Composed) : null;
            if (siteGM == null || siteM == null) { Console.WriteLine("FAIL EXECUTED the composed submission slices cannot be located; the executed checks were not run"); failures++; }
            else
            {
                var probes = Harmony12.HarmonyInstance.Create("probe.engine");
                try
                {
                    probes.Patch(typeof(Kingmaker.Game).GetProperty("Instance", All).GetGetMethod(true), new Harmony12.HarmonyMethod(typeof(KmcGunslingerBoundary).GetMethod("NoLiveGame")), null, null);
                    probes.Patch(typeof(UnitCommands).GetMethod("Run", new[] { typeof(UnitCommand) }), new Harmony12.HarmonyMethod(typeof(KmcGunslingerBoundary).GetMethod("NativeRun")), null, null);
                    probes.Patch(other.GetType(OtherOrder, true).GetMethod("Submit", All), new Harmony12.HarmonyMethod(typeof(KmcGunslingerBoundary).GetMethod("SubmitSeen")), null, null);
                    probes.Patch(methods.GetMethod("GroundCommandPrefix", All), new Harmony12.HarmonyMethod(typeof(KmcGunslingerBoundary).GetMethod("Admission")), null, null);
                    probes.Patch(methods.GetMethod("GroundCommandPostfix", All), new Harmony12.HarmonyMethod(typeof(KmcGunslingerBoundary).GetMethod("AdmissionCompleted")), null, null);
                    productLog = new KmcGunslingerLog(product.GetType("KingmakerMountedCombat.Logging.IModLogger", true));
                    bridge.GetField("Logger", All).SetValue(null, productLog.GetTransparentProxy());
                    var both = EmitSlice(siteGM); var alone = EmitSlice(siteM); var reverse = EmitSlice(FindSite(MG.Composed));
                    Action<UnitEntityData, UnitCommand, object> combined = (unit, command, invocation) => both.DynamicInvoke(unit.Commands, command, invocation);
                    Action<UnitEntityData, UnitCommand> mountedOnly = (unit, command) => alone.DynamicInvoke(unit.Commands, command, null);
                    Action<UnitEntityData, UnitCommand, object> reverseOrder = (unit, command, invocation) => reverse.DynamicInvoke(unit.Commands, command, invocation);
                    var a = NewPair("Mounted", 7); var b = NewPair("Mounted", 9); var resting = NewPair("Unmounted", 3); var stranger = Unit();

                    Check("EXECUTED both mods, mounted cannot-attack approach: the rider's original UnitMoveTo is run once by its own pair's mount; the other mod's Submit is skipped; no second submission; the admission completes once; nothing is recorded on the invocation",
                        () => { Reset(); InstallWorld(a, b); var invocation = NewInvocation(false); var move = Move(); var routed = Routed(); combined(a.Rider, move, invocation);
                            var log = KmcGunslingerBoundary.Log; Console.WriteLine("INFO trace: " + Trace());
                            return Every("E1", log.Count == 1, log.Count == 1 && log[0].Via == "native Run" && ReferenceEquals(log[0].Container, a.Mount.Commands) && ReferenceEquals(log[0].Command, move),
                                ReferenceEquals(move.Executor, a.Mount), KmcGunslingerBoundary.Postfixes == 1, Routed() == routed + 1, SubmittedProposals(invocation) == 0, OtherBinding(move) == null, !Faulted()); });
                    Check("EXECUTED both mods, independent pairs: the second pair's rider is routed to the second mount only; the first pair's mount receives nothing",
                        () => { Reset(); InstallWorld(a, b); var move = Move(); combined(b.Rider, move, NewInvocation(false)); var log = KmcGunslingerBoundary.Log;
                            return Every("E2", log.Count == 1, log.Count == 1 && ReferenceEquals(log[0].Container, b.Mount.Commands), ReferenceEquals(move.Executor, b.Mount), !Faulted()); });
                    Check("EXECUTED Mounted Combat alone, mounted cannot-attack approach: the same command is run once by the mount and the native Run is skipped",
                        () => { Reset(); InstallWorld(a, b); var move = Move(); mountedOnly(a.Rider, move); var log = KmcGunslingerBoundary.Log;
                            return Every("E3", log.Count == 1, log.Count == 1 && ReferenceEquals(log[0].Container, a.Mount.Commands) && ReferenceEquals(log[0].Command, move), !Faulted()); });
                    Check("EXECUTED Mounted Combat registered first (the reverse chain's own composed slice): the mounted approach goes to the mount once with no Submit; an unmounted unit's UnitMoveTo reaches the other mod's Submit and one native run",
                        () => { Reset(); InstallWorld(a, resting); var move = Move(); reverseOrder(a.Rider, move, NewInvocation(false)); var routedLog = KmcGunslingerBoundary.Log.ToList();
                            var routedOk = routedLog.Count == 1 && ReferenceEquals(routedLog[0].Container, a.Mount.Commands) && ReferenceEquals(routedLog[0].Command, move) && !Faulted();
                            Reset(); InstallWorld(a, resting); var invocation = NewInvocation(false); var restingMove = Move(); reverseOrder(resting.Rider, restingMove, invocation); var log = KmcGunslingerBoundary.Log;
                            return Every("E3r", routedOk, log.Count == 2 && log[0].Via == "Submit" && ReferenceEquals(log[0].Invocation, invocation) && ReferenceEquals(log[1].Container, resting.Rider.Commands), !Faulted()); });
                    foreach (var unmounted in new[] { "unmounted pair", "unit outside every pair", "no mounted world" })
                    {
                        var label = unmounted;
                        Check("EXECUTED both mods, " + label + ": the UnitMoveTo reaches the other mod's Submit on the clicking unit's own container with the same command and invocation, which runs it natively once there",
                            () => { Reset(); var unit = label == "unmounted pair" ? resting.Rider : stranger;
                                if (label == "no mounted world") InstallWorld(); else InstallWorld(a, resting);
                                var invocation = NewInvocation(false); var move = Move(); var routed = Routed(); combined(unit, move, invocation); var log = KmcGunslingerBoundary.Log;
                                Console.WriteLine("INFO trace (" + label + "): " + Trace());
                                return Every("E4", log.Count == 2, log.Count == 2 && log[0].Via == "Submit" && ReferenceEquals(log[0].Container, unit.Commands) && ReferenceEquals(log[0].Command, move) && ReferenceEquals(log[0].Invocation, invocation),
                                    log.Count == 2 && log[1].Via == "native Run" && ReferenceEquals(log[1].Container, unit.Commands) && ReferenceEquals(log[1].Command, move),
                                    Routed() == routed, KmcGunslingerBoundary.Postfixes == 0, !Faulted()); });
                    }
                    foreach (var kind in new[] { typeof(UnitAttack), typeof(UnitUseAbility) })
                    {
                        var type = kind;
                        Check("EXECUTED both mods, a mounted rider's " + type.Name + " (firearm attack / auto-use cast path): not routed; the other mod's Submit receives the same container, command and invocation",
                            () => { Reset(); InstallWorld(a, b); var invocation = NewInvocation(false); var command = (UnitCommand)Blank(type); combined(a.Rider, command, invocation); var log = KmcGunslingerBoundary.Log;
                                return Every("E5", log.Count == 2, log.Count == 2 && log[0].Via == "Submit" && ReferenceEquals(log[0].Container, a.Rider.Commands) && ReferenceEquals(log[0].Command, command) && ReferenceEquals(log[0].Invocation, invocation),
                                    KmcGunslingerBoundary.Postfixes == 0, !Faulted()); });
                    }
                    Check("EXECUTED both mods, simulated input (native SimulateClick: SimulatingClick and the simulate argument together): not routed; the other mod's Submit marks its preview and runs it on the rider's container",
                        () => { Reset(); InstallWorld(a, b); typeof(Kingmaker.Controllers.Clicks.PointerController).GetField("SimulatingClick", All).SetValue(null, true);
                            var invocation = NewInvocation(true); var move = Move(); combined(a.Rider, move, invocation); var log = KmcGunslingerBoundary.Log; var binding = OtherBinding(move);
                            var ok = Every("E6", log.Count == 2, log.Count == 2 && log[0].Via == "Submit" && ReferenceEquals(log[1].Container, a.Rider.Commands),
                                binding != null && (bool)binding.GetType().GetField("Preview", All).GetValue(binding), KmcGunslingerBoundary.Postfixes == 0, !Faulted());
                            Reset(); return ok; });
                    Check("EXECUTED both mods, null command (the other mod's auto-use creation can return none): not routed; the other mod's Submit receives it and submits nothing",
                        () => { Reset(); InstallWorld(a, b); combined(a.Rider, null, NewInvocation(false)); var log = KmcGunslingerBoundary.Log;
                            return Every("E7", log.Count == 1, log.Count == 1 && log[0].Via == "Submit" && log[0].Command == null, !Faulted()); });
                    Check("EXECUTED both mods, a fault in the mount's run after the command was handed to the mount: the router contains it, the admission still completes, and the rider's submission is skipped (no second submission of an initialised command)",
                        () => { Reset(); InstallWorld(a, b); KmcGunslingerBoundary.ThrowFor = a.Mount.Commands; var move = Move(); combined(a.Rider, move, NewInvocation(false)); var log = KmcGunslingerBoundary.Log;
                            var ok = Every("E8", log.Count == 1, log.Count == 1 && ReferenceEquals(log[0].Container, a.Mount.Commands), ReferenceEquals(move.Executor, a.Mount), KmcGunslingerBoundary.Postfixes == 1, Faulted());
                            Reset(); return ok; });
                    Check("EXECUTED both mods, a fault before any hand-off (the admission throws): the router contains it and the original submission runs once, through the other mod's Submit on the rider's container",
                        () => { Reset(); InstallWorld(a, b); KmcGunslingerBoundary.AdmissionThrows = true; var move = Move(); combined(a.Rider, move, NewInvocation(false)); var log = KmcGunslingerBoundary.Log;
                            var ok = Every("E9", log.Count == 2, log.Count == 2 && log[0].Via == "Submit" && ReferenceEquals(log[1].Container, a.Rider.Commands) && ReferenceEquals(log[1].Command, move), Faulted());
                            Reset(); return ok; });
                    Check("EXECUTED both mods, a command already given to another unit: the rider's submission is skipped",
                        () => { Reset(); InstallWorld(); var move = Move(); typeof(UnitCommand).GetField("<Executor>k__BackingField", All).SetValue(move, stranger);
                            combined(a.Rider, move, NewInvocation(false)); return KmcGunslingerBoundary.Log.Count == 0 && !Faulted(); });
                    Check("EXECUTED both mods, an unrouted submission that throws: the exception reaches the caller (the other mod's own input guard), exactly one native run was attempted, and the router left nothing behind",
                        () => { Reset(); InstallWorld(a, resting); KmcGunslingerBoundary.ThrowFor = resting.Rider.Commands; var routed = Routed(); Exception seen = null;
                            try { combined(resting.Rider, Move(), NewInvocation(false)); } catch (Exception error) { seen = Root(error); }
                            var ok = Every("E10", seen is InvalidOperationException, KmcGunslingerBoundary.Log.Count(s => s.Via == "native Run") == 1, Routed() == routed, KmcGunslingerBoundary.Postfixes == 0, !Faulted());
                            Reset(); return ok; });
                }
                finally { bridge.GetField("Logger", All).SetValue(null, null); probes.UnpatchAll("probe.engine"); InstallWorld(); bridge.GetField("Service", All).SetValue(null, null); Reset(); }
            }
        }
        finally { KmcGunslingerBoundary.DetourTarget = null; boundary.UnpatchAll("probe.boundary"); }
        Console.WriteLine("GUNSLINGER CLICK COMPOSITION PASS=" + passes + " FAIL=" + failures + "; real transpilers through the installed loader and executed composed slices, not native gameplay evidence");
        return failures == 0 ? 0 : 1;
    }
}
'@
try { $code=[KmcGunslingerProbe]::Run($managed,$dll,$gun) }
catch { Write-Output $_.Exception.ToString(); exit 1 }
exit $code
