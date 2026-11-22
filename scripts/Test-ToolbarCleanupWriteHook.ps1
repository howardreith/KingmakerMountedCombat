[CmdletBinding()]
param(
    [ValidateSet('Debug','Release')][string]$Configuration='Release',
    # Another build of the product to judge with the same probe.
    [string]$ProductDll=''
)
# Toolbar persistence: the production CleanupBadSlots write hook and the SetSlot prefix, executed offline (second Pro review
# of PR #57, item 3). Reuses the isolation of Test-GunslingerClickComposition.ps1: the built product, the installed
# Assembly-CSharp and the installed loader's Harmony12 shim (over HarmonyLib) in a detached process.
#   COMPOSITION  the product's BadSlotCleanupWritesTranspiler is registered on the installed UnitUISettings.CleanupBadSlots
#                through the Harmony12 shim; the composed instructions (HarmonyLib GetCurrentInstructions) are compared with
#                the original stream: exactly the three SetSlot call sites become calls of PatchMethods.NativeCleanupSetSlot,
#                and every other instruction, branch label and exception block is unchanged. The one step a detached
#                process does not take is writing the jump into the live original (HarmonyLib.PatchTools.DetourMethod) for
#                CleanupBadSlots only. The transpiler is also invoked directly on streams with an unexpected number of SetSlot
#                calls, which it must refuse.
#   EXECUTED     the product's NativeCleanupSetSlot and SlotWritePrefix run for real on a native UnitUISettings. Substituted
#                boundaries: native SetSlot is wrapped by a counting probe (it can make the native write fail); the service
#                methods the hook and the prefix call (NativeMountedControlService.RecordNativeCleanupWrite and
#                BeforeNativeSlotWrite) are replaced by recording probes on an uninitialised service object (their bodies are
#                covered by the component tests and the wiring fixture). A runtime mutation of the production hook that
#                removes its recording call must make the record check fail. Not native gameplay evidence.
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$lab=[IO.Path]::GetFullPath((Join-Path $repo '../..'))
$layout=(Get-Content -Raw (Join-Path $lab 'environment-intake.json') | ConvertFrom-Json).requestedLayout
$managed=Join-Path $layout.kingmakerInstallDir 'Kingmaker_Data/Managed'
$dll=if([string]::IsNullOrWhiteSpace($ProductDll)){Join-Path $repo "bin/$Configuration/KingmakerMountedCombat.dll"}else{[IO.Path]::GetFullPath($ProductDll)}
if(-not(Test-Path -LiteralPath $dll -PathType Leaf)){Write-Output "The product build is missing: $dll";exit 1}
$assemblyCsharp=Join-Path $managed 'Assembly-CSharp.dll'
$pinned='3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb'
$actual=(Get-FileHash -Algorithm SHA256 -LiteralPath $assemblyCsharp).Hash.ToLowerInvariant()
if($actual -cne $pinned){Write-Output "Identity mismatch for Assembly-CSharp: $actual (expected $pinned)";exit 1}
Write-Output "INFO Assembly-CSharp SHA-256 $actual"
foreach($loader in '0Harmony12.dll','0Harmony.dll'){
    $path=Join-Path $managed "UnityModManager/$loader"; $item=Get-Item -LiteralPath $path
    Write-Output ("INFO {0} {1} SHA-256 {2}" -f $loader,$item.VersionInfo.ProductVersion,(Get-FileHash -Algorithm SHA256 -LiteralPath $path).Hash.ToLowerInvariant())
}
Write-Output ("INFO product {0} SHA-256 {1}" -f $dll,(Get-FileHash -Algorithm SHA256 -LiteralPath $dll).Hash.ToLowerInvariant())
$refs=@($assemblyCsharp,(Join-Path $managed 'UnityEngine.CoreModule.dll'),(Join-Path $managed 'UnityEngine.UI.dll'),(Join-Path $managed 'UnityEngine.dll'),(Join-Path $managed 'UnityModManager/0Harmony12.dll'),
    (Join-Path $managed 'UnityModManager/0Harmony.dll'),'System.Core')
Add-Type -TypeDefinition @'
using System; using System.IO; using System.Reflection;
public static class KmcToolbarHookResolve
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
[KmcToolbarHookResolve]::Init(@($managed,(Join-Path $managed 'UnityModManager'),(Split-Path $dll)))
Add-Type -ReferencedAssemblies $refs -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using Kingmaker.UI.UnitSettings;

// Substituted boundaries (see the header of Test-ToolbarCleanupWriteHook.ps1).
public static class KmcToolbarHookBoundary
{
    public static MethodBase DetourTarget;
    public static int DetoursSkipped;
    public static bool SkipDetour(MethodBase method) { if (DetourTarget == null || method != DetourTarget) return true; DetoursSkipped++; return false; }

    // Native SetSlot: every write attempt is counted; the native write can be made to fail.
    public static int SetSlotCalls;
    public static bool SetSlotFails;
    public static void SetSlotProbe() { SetSlotCalls++; if (SetSlotFails) throw new InvalidOperationException("probe: the native write failed"); }

    // The service methods the production hook and prefix call.
    public sealed class Call { public string Via; public UnitUISettings Settings; public int Index; public MechanicActionBarSlot SlotThen; public MechanicActionBarSlot[] BarThen; }
    public static readonly List<Call> Calls = new List<Call>();
    public static bool ServiceThrows;
    public static bool RecordProbe(UnitUISettings settings, int index)
    {
        Calls.Add(new Call { Via = "record", Settings = settings, Index = index, SlotThen = settings.Slots[index] });
        if (ServiceThrows) throw new InvalidOperationException("probe: the record failed");
        return false;
    }
    public static bool BeforeWriteProbe(UnitUISettings settings)
    {
        Calls.Add(new Call { Via = "before write", Settings = settings, Index = -1, BarThen = (MechanicActionBarSlot[])settings.Slots.Clone() });
        if (ServiceThrows) throw new InvalidOperationException("probe: the observation failed");
        return false;
    }

    // The disconnected recording route: the production hook with its one recording call removed (its three stack operands
    // popped instead), a runtime mutation of the production method.
    public static int Disconnected;
    public static IEnumerable<Harmony12.CodeInstruction> DisconnectRecording(IEnumerable<Harmony12.CodeInstruction> source)
    {
        var code = source.ToList();
        var site = code.FindIndex(i => { var m = i.operand as MethodInfo; return m != null && m.Name == "RecordNativeCleanupWrite"; });
        if (site < 0 || code.FindLastIndex(i => { var m = i.operand as MethodInfo; return m != null && m.Name == "RecordNativeCleanupWrite"; }) != site)
            throw new InvalidOperationException("the production hook does not call RecordNativeCleanupWrite exactly once");
        code[site].opcode = OpCodes.Pop; code[site].operand = null;
        code.Insert(site + 1, new Harmony12.CodeInstruction(OpCodes.Pop, null));
        code.Insert(site + 1, new Harmony12.CodeInstruction(OpCodes.Pop, null));
        Disconnected++;
        return code;
    }
}

// The product's logger interface, recorded.
public sealed class KmcToolbarHookLog : System.Runtime.Remoting.Proxies.RealProxy
{
    public readonly List<string> Lines = new List<string>();
    public KmcToolbarHookLog(Type contract) : base(contract) { }
    public override System.Runtime.Remoting.Messaging.IMessage Invoke(System.Runtime.Remoting.Messaging.IMessage message)
    {
        var call = (System.Runtime.Remoting.Messaging.IMethodCallMessage)message;
        Lines.Add(call.MethodName + ": " + string.Join(" | ", call.InArgs.Select(a => a == null ? "null" : a.ToString()).ToArray()));
        return new System.Runtime.Remoting.Messaging.ReturnMessage(null, null, 0, call.LogicalCallContext, call);
    }
}

public static class KmcToolbarHookProbe
{
    const BindingFlags All = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    const string MountedOwner = "KingmakerMountedCombat.Feasibility";
    static int passes, failures;

    static void Check(string label, Func<bool> body)
    {
        string detail = null; var ok = false;
        try { ok = body(); }
        catch (Exception error) { var inner = Root(error); detail = inner.GetType().Name + ": " + inner.Message; }
        if (ok) { passes++; Console.WriteLine("PASS " + label); }
        else { failures++; Console.WriteLine("FAIL " + label + (detail != null ? " [" + detail + "]" : "")); }
    }
    static Exception Root(Exception error) { while (error is TargetInvocationException && error.InnerException != null) error = error.InnerException; return error; }
    static bool Every(string tag, params bool[] parts)
    {
        var failed = new List<string>(); for (var i = 0; i < parts.Length; i++) if (!parts[i]) failed.Add((i + 1).ToString());
        if (failed.Count > 0) Console.WriteLine("INFO " + tag + " failing parts: " + string.Join(",", failed.ToArray()));
        return failed.Count == 0;
    }
    static object Blank(Type type) { return FormatterServices.GetUninitializedObject(type); }

    static string Show(HarmonyLib.CodeInstruction i)
    {
        var method = i.operand as MethodBase; var local = i.operand as LocalVariableInfo; var field = i.operand as FieldInfo; var type = i.operand as Type;
        if (method != null) return i.opcode + " " + method.DeclaringType.FullName + "::" + method.Name;
        if (field != null) return i.opcode + " " + field.DeclaringType.FullName + "::" + field.Name;
        if (type != null) return i.opcode + " " + type.FullName;
        if (local != null) return i.opcode + " local:" + local.LocalType.FullName + "#" + local.LocalIndex;
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

    // Every ldstr string and ldc.i4 constant in a method body (a byte scan; false positives only widen the search).
    static void Constants(MethodBase method, Module module, HashSet<string> strings, HashSet<int> numbers)
    {
        var body = method.GetMethodBody(); if (body == null) return;
        var il = body.GetILAsByteArray();
        for (var at = 0; at + 4 < il.Length; at++)
        {
            var value = BitConverter.ToInt32(il, at + 1);
            if (il[at] == 0x72 && (value >> 24) == 0x70) { try { strings.Add(module.ResolveString(value)); } catch (ArgumentException) { } }
            else if (il[at] == 0x20) numbers.Add(value);
        }
    }

    public static int Run(string managed, string productPath)
    {
        var product = Assembly.LoadFrom(productPath);
        var controller = product.GetType("KingmakerMountedCombat.Integration.MountedPatchController", true);
        var methods = controller.GetNestedType("PatchMethods", All);
        var bridge = controller.GetNestedType("PatchBridge", All);
        var serviceType = product.GetType("KingmakerMountedCombat.Integration.NativeMountedControlService", true);
        var loggerType = product.GetType("KingmakerMountedCombat.Logging.IModLogger", true);
        var transpiler = methods.GetMethod("BadSlotCleanupWritesTranspiler", All);
        var hook = methods.GetMethod("NativeCleanupSetSlot", All);
        var prefix = methods.GetMethod("SlotWritePrefix", All);
        var cleanup = typeof(UnitUISettings).GetMethod("CleanupBadSlots", All);
        var setSlot = typeof(UnitUISettings).GetMethod("SetSlot", new[] { typeof(MechanicActionBarSlot), typeof(int) });
        var expected = (int)controller.GetField("NativeCleanupSetSlotCalls", All).GetValue(null);
        var cleanupToken = (int)controller.GetField("NativeCleanupBadSlotsToken", All).GetValue(null);
        var setSlotToken = (int)controller.GetField("NativeSetSlotToken", All).GetValue(null);
        Console.WriteLine("INFO product transpiler " + (transpiler != null) + ", hook " + (hook != null) + ", SetSlot prefix " + (prefix != null) + "; expected SetSlot call sites " + expected);

        Check("REGISTRATION the product registers the write transpiler with CleanupBadSlots's exact token and the write prefix with SetSlot's exact token", () =>
        {
            var registered = false;
            foreach (var method in controller.GetMethods(All).Cast<MethodBase>().Concat(controller.GetConstructors(All)))
            {
                var strings = new HashSet<string>(); var numbers = new HashSet<int>();
                Constants(method, product.ManifestModule, strings, numbers);
                if (strings.Contains("BadSlotCleanupWritesTranspiler") && strings.Contains("BadSlotCleanupPrefix") && numbers.Contains(cleanupToken) &&
                    strings.Contains("SlotWritePrefix") && numbers.Contains(setSlotToken)) registered = true;
            }
            return Every("R1", cleanupToken == cleanup.MetadataToken, setSlotToken == setSlot.MetadataToken, expected == 3, registered);
        });

        var boundary = Harmony12.HarmonyInstance.Create("probe.toolbar.boundary");
        boundary.Patch(typeof(HarmonyLib.CodeInstruction).Assembly.GetType("HarmonyLib.PatchTools", true).GetMethod("DetourMethod", All),
            new Harmony12.HarmonyMethod(typeof(KmcToolbarHookBoundary).GetMethod("SkipDetour")), null, null);
        var log = new KmcToolbarHookLog(loggerType);
        try
        {
            // ---- composition on the installed CleanupBadSlots --------------------------------------------------------------
            KmcToolbarHookBoundary.DetourTarget = cleanup;
            var original = HarmonyLib.PatchProcessor.GetCurrentInstructions(cleanup, int.MaxValue, null);
            List<HarmonyLib.CodeInstruction> composed;
            var mounted = Harmony12.HarmonyInstance.Create(MountedOwner);
            try
            {
                mounted.Patch(cleanup, null, null, new Harmony12.HarmonyMethod(transpiler));
                composed = HarmonyLib.PatchProcessor.GetCurrentInstructions(cleanup, int.MaxValue, null);
            }
            finally { mounted.UnpatchAll(MountedOwner); KmcToolbarHookBoundary.DetourTarget = null; }
            Check("COMPOSITION the installed CleanupBadSlots composed with the production transpiler: exactly its three SetSlot call sites call NativeCleanupSetSlot; every other instruction, label and exception block is unchanged; the live original was not written", () =>
            {
                var before = Canonical(original); var after = Canonical(composed);
                var nativeText = "call Kingmaker.UI.UnitSettings.UnitUISettings::SetSlot";
                var hookText = "call KingmakerMountedCombat.Integration.MountedPatchController+PatchMethods::NativeCleanupSetSlot";
                var changed = 0; var other = 0;
                for (var i = 0; i < Math.Min(before.Count, after.Count); i++)
                {
                    if (before[i] == after[i]) continue;
                    if (before[i].Replace(nativeText, hookText) == after[i] && before[i].Contains(nativeText)) changed++; else other++;
                }
                Console.WriteLine("INFO composed CleanupBadSlots: " + before.Count + " instructions, " + changed + " rewritten, " + other + " other differences, " +
                    KmcToolbarHookBoundary.DetoursSkipped + " live detour(s) skipped");
                return Every("C1", before.Count == after.Count, changed == 3, other == 0,
                    original.Count(i => Equals(i.operand, setSlot)) == 3, composed.Count(i => Equals(i.operand, setSlot)) == 0,
                    composed.Count(i => Equals(i.operand, hook) && i.opcode == OpCodes.Call) == 3, KmcToolbarHookBoundary.DetoursSkipped >= 1);
            });
            Check("COMPOSITION the production transpiler refuses a stream with 0, 2 or 4 SetSlot calls, and for 3 rewrites only those, keeping every instruction object", () =>
            {
                Func<int, List<Harmony12.CodeInstruction>> stream = n =>
                {
                    var list = new List<Harmony12.CodeInstruction> { new Harmony12.CodeInstruction(OpCodes.Nop, null) };
                    for (var i = 0; i < n; i++) { list.Add(new Harmony12.CodeInstruction(OpCodes.Call, setSlot)); list.Add(new Harmony12.CodeInstruction(OpCodes.Nop, null)); }
                    list.Add(new Harmony12.CodeInstruction(OpCodes.Ret, null));
                    return list;
                };
                Func<int, bool> refuses = n =>
                {
                    try { transpiler.Invoke(null, new object[] { stream(n) }); return false; }
                    catch (Exception error) { return Root(error) is InvalidOperationException; }
                };
                var input = stream(3);
                var output = ((IEnumerable<Harmony12.CodeInstruction>)transpiler.Invoke(null, new object[] { input })).ToList();
                var kept = output.Count == input.Count && output.Zip(input, (a, b) => ReferenceEquals(a, b)).All(same => same);
                return Every("C2", refuses(0), refuses(2), refuses(4), kept, output.Count(i => Equals(i.operand, hook) && i.opcode == OpCodes.Call) == 3,
                    output.Count(i => i.opcode == OpCodes.Nop) == 4 && output.Last().opcode == OpCodes.Ret);
            });

            // ---- the production hook and prefix, executed --------------------------------------------------------------
            boundary.Patch(setSlot, new Harmony12.HarmonyMethod(typeof(KmcToolbarHookBoundary).GetMethod("SetSlotProbe")), null, null);
            boundary.Patch(serviceType.GetMethod("RecordNativeCleanupWrite", All), new Harmony12.HarmonyMethod(typeof(KmcToolbarHookBoundary).GetMethod("RecordProbe")), null, null);
            boundary.Patch(serviceType.GetMethod("BeforeNativeSlotWrite", All), new Harmony12.HarmonyMethod(typeof(KmcToolbarHookBoundary).GetMethod("BeforeWriteProbe")), null, null);
            bridge.GetField("NativeControls", All).SetValue(null, Blank(serviceType));
            bridge.GetField("Logger", All).SetValue(null, log.GetTransparentProxy());
            Func<UnitUISettings> newBar = () => { var bar = (UnitUISettings)Blank(typeof(UnitUISettings)); bar.Slots = new MechanicActionBarSlot[140]; return bar; };
            Func<MechanicActionBarSlot> newSlot = () => (MechanicActionBarSlot)Blank(typeof(MechanicActionBarSlotEmpty));
            Action reset = () => { KmcToolbarHookBoundary.SetSlotCalls = 0; KmcToolbarHookBoundary.SetSlotFails = false; KmcToolbarHookBoundary.ServiceThrows = false; KmcToolbarHookBoundary.Calls.Clear(); log.Lines.Clear(); };
            // One native write, then one record of exactly that write (the record sees the slot already written).
            Func<string, bool> writesThenRecords = tag =>
            {
                reset();
                var bar = newBar(); var slot = newSlot();
                hook.Invoke(null, new object[] { bar, slot, 9 });
                var calls = KmcToolbarHookBoundary.Calls;
                return Every(tag, KmcToolbarHookBoundary.SetSlotCalls == 1, ReferenceEquals(bar.Slots[9], slot), calls.Count == 1,
                    calls.Count == 1 && calls[0].Via == "record" && ReferenceEquals(calls[0].Settings, bar) && calls[0].Index == 9 && ReferenceEquals(calls[0].SlotThen, slot));
            };
            Check("EXECUTED the production NativeCleanupSetSlot: one native write, then one record of that write, with the slot already written", () => writesThenRecords("X1"));
            Check("EXECUTED the production NativeCleanupSetSlot: a native write that throws propagates its exception unchanged, with no retry and no record", () =>
            {
                reset(); KmcToolbarHookBoundary.SetSlotFails = true;
                var bar = newBar(); Exception seen = null;
                try { hook.Invoke(null, new object[] { bar, newSlot(), 10 }); } catch (Exception error) { seen = Root(error); }
                return Every("X2", seen is InvalidOperationException && seen.Message == "probe: the native write failed", KmcToolbarHookBoundary.SetSlotCalls == 1,
                    KmcToolbarHookBoundary.Calls.Count == 0, bar.Slots[10] == null);
            });
            Check("EXECUTED the production NativeCleanupSetSlot: a failing record never undoes or repeats the native write and is logged, not thrown", () =>
            {
                reset(); KmcToolbarHookBoundary.ServiceThrows = true;
                var bar = newBar(); var slot = newSlot(); Exception seen = null;
                try { hook.Invoke(null, new object[] { bar, slot, 11 }); } catch (Exception error) { seen = Root(error); }
                return Every("X3", seen == null, ReferenceEquals(bar.Slots[11], slot), KmcToolbarHookBoundary.SetSlotCalls == 1, KmcToolbarHookBoundary.Calls.Count == 1,
                    log.Lines.Count(line => line.StartsWith("Exception:", StringComparison.Ordinal)) == 1);
            });
            Check("NEGATIVE CONTROL the record check fails when the production hook's recording call is removed (runtime mutation), and passes again without it", () =>
            {
                var mutation = Harmony12.HarmonyInstance.Create("probe.toolbar.mutation");
                bool disconnected;
                try
                {
                    mutation.Patch(hook, null, null, new Harmony12.HarmonyMethod(typeof(KmcToolbarHookBoundary).GetMethod("DisconnectRecording")));
                    Console.WriteLine("INFO the next X1 check runs against the disconnected hook and is expected to fail:");
                    disconnected = writesThenRecords("X1 (disconnected)");
                }
                finally { mutation.UnpatchAll("probe.toolbar.mutation"); }
                return Every("N1", KmcToolbarHookBoundary.Disconnected == 1, !disconnected, KmcToolbarHookBoundary.SetSlotCalls == 1, writesThenRecords("X1 (restored)"));
            });
            boundary.Patch(setSlot, new Harmony12.HarmonyMethod(prefix), null, null);
            Check("EXECUTED the production SlotWritePrefix on native SetSlot: the service observes the bar before the write lands, once per write", () =>
            {
                reset();
                var bar = newBar(); var slot = newSlot();
                bar.SetSlot(slot, 12);
                var calls = KmcToolbarHookBoundary.Calls;
                return Every("X4", calls.Count == 1, calls.Count == 1 && calls[0].Via == "before write" && ReferenceEquals(calls[0].Settings, bar) && calls[0].BarThen[12] == null,
                    ReferenceEquals(bar.Slots[12], slot), KmcToolbarHookBoundary.SetSlotCalls == 1);
            });
            Check("EXECUTED the production SlotWritePrefix: a failing observation is logged and the native write still happens once", () =>
            {
                reset(); KmcToolbarHookBoundary.ServiceThrows = true;
                var bar = newBar(); var slot = newSlot(); Exception seen = null;
                try { bar.SetSlot(slot, 13); } catch (Exception error) { seen = Root(error); }
                return Every("X5", seen == null, ReferenceEquals(bar.Slots[13], slot), KmcToolbarHookBoundary.SetSlotCalls == 1,
                    log.Lines.Count(line => line.StartsWith("Exception:", StringComparison.Ordinal)) == 1);
            });
            Check("EXECUTED the production NativeCleanupSetSlot with the production SetSlot prefix registered: the bar is observed before the write and the write is recorded after it", () =>
            {
                reset();
                var bar = newBar(); var slot = newSlot();
                hook.Invoke(null, new object[] { bar, slot, 14 });
                var calls = KmcToolbarHookBoundary.Calls;
                return Every("X6", calls.Count == 2, calls.Count == 2 && calls[0].Via == "before write" && calls[0].BarThen[14] == null,
                    calls.Count == 2 && calls[1].Via == "record" && ReferenceEquals(calls[1].SlotThen, slot), KmcToolbarHookBoundary.SetSlotCalls == 1);
            });
        }
        finally
        {
            KmcToolbarHookBoundary.DetourTarget = null;
            bridge.GetField("NativeControls", All).SetValue(null, null);
            bridge.GetField("Logger", All).SetValue(null, null);
            boundary.UnpatchAll("probe.toolbar.boundary");
        }
        Console.WriteLine("TOOLBAR CLEANUP WRITE HOOK PASS=" + passes + " FAIL=" + failures + "; the production transpiler through the installed loader and the production hook and prefix executed, not native gameplay evidence");
        return failures == 0 ? 0 : 1;
    }
}
'@
try { $code=[KmcToolbarHookProbe]::Run($managed,$dll) }
catch { Write-Output $_.Exception.ToString(); exit 1 }
exit $code
