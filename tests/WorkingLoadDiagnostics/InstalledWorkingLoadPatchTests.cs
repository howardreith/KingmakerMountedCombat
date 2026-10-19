using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Harmony12;
using KingmakerMountedCombat.Diagnostics;

// Runs in a disposable process. Only wrappers are installed; no game method is invoked.
internal static class InstalledWorkingLoadPatchTests
{
    private const string Owner = "KingmakerMountedCombat.Tests.WorkingLoadPatch";
    private static NativeWorkingLoadContract contract;
    private static string runMode;
    private static readonly bool[] rewritten = new bool[5];
    private static IEnumerable<CodeInstruction> Rewrite(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
    {
        var index = Array.FindIndex(contract.Owners, x => Equals(x, __originalMethod));
        if (index < 1 || rewritten[index]) throw new InvalidOperationException("Unexpected native rewrite owner");
        if (runMode == "partial" && index == 2) throw new InvalidOperationException("simulated fallible rewrite after acquisition");
        var result = NativeWorkingLoadRewrite.Rewrite(instructions, __originalMethod, contract,
            typeof(InstalledWorkingLoadPatchTests).GetMethod(nameof(Returned), BindingFlags.Static | BindingFlags.NonPublic),
            typeof(InstalledWorkingLoadPatchTests).GetMethod(nameof(Receiver), BindingFlags.Static | BindingFlags.NonPublic));
        rewritten[index] = true;
        return result;
    }
    private static IEnumerable<CodeInstruction> PassThrough(IEnumerable<CodeInstruction> instructions) => instructions;
    private static void IteratorReturned(IEnumerator<object> __result) { }
    private static void StartReturned(Task __result, object __instance) { }
    private static void Returned(object value, object receiver, int kind) { }
    private static void Receiver(object receiver, int kind) { }
    private static HarmonyMethod Hook(string name) => new HarmonyMethod(typeof(InstalledWorkingLoadPatchTests).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic));
    private static MethodBase Target(int index) => index == 0 ? (MethodBase)contract.LoadRoutineFactory :
        index == 1 ? contract.Start : contract.Owners[index - 1];
    private static HarmonyPatchType Kind(int index) => index < 2 ? HarmonyPatchType.Postfix : HarmonyPatchType.Transpiler;
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Run(string mode)
    {
        runMode = mode;
        contract = new NativeWorkingLoadContract();
        var harmony = HarmonyInstance.Create(Owner);
        if (mode == "bool-refusal")
        {
            // The old first target fails even with an identity transpiler. Exit this
            // disposable process without attempting the broken wrapper's unpatch.
            try { harmony.Patch(contract.Owners[0], null, null, Hook(nameof(PassThrough))); }
            catch (InvalidProgramException error)
            {
                Console.WriteLine("BOOL ITERATOR REFUSAL PASS " + error.GetType().Name);
                return 0;
            }
            throw new InvalidOperationException("Pinned bool iterator unexpectedly accepted Harmony12 patch");
        }
        var attempted = new bool[6];
        try
        {
            for (int i = 0; i < attempted.Length; i++)
            {
                attempted[i] = true;
                Console.WriteLine("PATCH TARGET " + i + " " + Target(i));
                var method = i == 0 ? Hook(nameof(IteratorReturned)) : i == 1 ? Hook(nameof(StartReturned)) : Hook(nameof(Rewrite));
                harmony.Patch(Target(i), null, i < 2 ? method : null, i < 2 ? null : method);
                if (i >= 2 && !rewritten[i - 1]) throw new InvalidOperationException("Native rewrite did not execute");
            }
            if (mode != "complete") throw new InvalidOperationException("Partial mode did not interrupt installation");
        }
        catch (Exception error)
        {
            if (mode != "partial" || !error.ToString().Contains("simulated fallible rewrite after acquisition")) throw;
        }
        finally
        {
            for (int i = attempted.Length - 1; i >= 0; i--)
            {
                if (!attempted[i]) continue;
                harmony.Unpatch(Target(i), Kind(i), Owner);
                var info = harmony.GetPatchInfo(Target(i));
                if (info != null && (i < 2 ? info.Postfixes : info.Transpilers).Any(p => p.owner == Owner))
                    throw new InvalidOperationException("Owned patch survived removal " + i);
            }
        }
        if (mode == "complete" && rewritten.Skip(1).Any(x => !x)) throw new InvalidOperationException("Missing native rewrite");
        Console.WriteLine("WORKING LOAD REAL PATCH " + mode.ToUpperInvariant() + " PASS");
        return 0;
    }
    private static int Main(string[] args)
    {
        var managed = args[0];
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) => {
            var leaf = new AssemblyName(e.Name).Name + ".dll";
            foreach (var dir in new[] { managed, Path.Combine(managed, "UnityModManager") })
            { var path = Path.Combine(dir, leaf); if (File.Exists(path)) return Assembly.LoadFrom(path); }
            return null;
        };
        try { return Run(args[1]); }
        catch (Exception error) { Console.WriteLine("WORKING LOAD REAL PATCH FAIL " + error); return 1; }
    }
}
