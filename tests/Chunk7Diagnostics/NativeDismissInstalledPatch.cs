using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Harmony12;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;

internal static class NativeDismissInstalledPatch
{
    private const string Owner = "KingmakerMountedCombat.Tests.NativeDismissInventory";

    private static int Run(string productPath)
    {
        var assembly = Assembly.LoadFrom(productPath);
        var patchMethods = assembly.GetType("KingmakerMountedCombat.Integration.MountedPatchController+PatchMethods", true);
        var rewrite = patchMethods.GetMethod("MountedDismissInventoryTranspiler",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        var target = typeof(Player).GetMethod("DismissCompanion", new[] { typeof(UnitEntityData) });
        if (rewrite == null || target == null || target.MetadataToken != 0x06000DD7 ||
            target.Module.ModuleVersionId != new Guid("07fa1e4d-8618-41b3-9b8d-faa17d3b26f7"))
            throw new InvalidOperationException("Pinned native dismissal contract changed.");
        var harmony = HarmonyInstance.Create(Owner);
        try
        {
            harmony.Patch(target, null, null, new HarmonyMethod(rewrite));
            var patch = harmony.GetPatchInfo(target);
            if (patch == null || patch.Transpilers.Count(item => item.owner == Owner) != 1)
                throw new InvalidOperationException("Mounted native dismissal rewrite did not install once.");
        }
        finally
        {
            harmony.Unpatch(target, HarmonyPatchType.Transpiler, Owner);
            var patch = harmony.GetPatchInfo(target);
            if (patch != null && patch.Transpilers.Any(item => item.owner == Owner))
                throw new InvalidOperationException("Mounted native dismissal rewrite survived removal.");
        }
        Console.WriteLine("NATIVE DISMISS INSTALLED PATCH PASS=1 FAIL=0");
        return 0;
    }

    private static int Main(string[] args)
    {
        var managed = args[0];
        AppDomain.CurrentDomain.AssemblyResolve += (sender, requested) =>
        {
            var leaf = new AssemblyName(requested.Name).Name + ".dll";
            foreach (var directory in new[] { managed, Path.Combine(managed, "UnityModManager") })
            {
                var path = Path.Combine(directory, leaf);
                if (File.Exists(path)) return Assembly.LoadFrom(path);
            }
            return null;
        };
        try { return Run(args[1]); }
        catch (Exception error) { Console.WriteLine("NATIVE DISMISS INSTALLED PATCH FAIL " + error); return 1; }
    }
}
