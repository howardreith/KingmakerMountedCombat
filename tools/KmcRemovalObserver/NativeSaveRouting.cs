using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Harmony12;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.EntitySystem.Persistence.SavesStorage;

namespace KmcRemovalObserver
{
    // The observer's own copy of the run-scoped save routing: every native
    // save-root, area-stash and cloud-replication resolution is redirected into
    // the owned profile for the whole process, exactly the seams KMC's own
    // isolation patches (same methods, same metadata tokens, same installed
    // Kingmaker assembly). Nothing here restores, admits or snapshots anything;
    // the engine is left to load the archive entirely on its own.
    internal static class NativeSaveRouting
    {
        internal const string HarmonyId = "KmcRemovalObserver.SaveRouting";
        private static readonly Guid ExpectedMvid = new Guid("07fa1e4d-8618-41b3-9b8d-faa17d3b26f7");
        private const BindingFlags Flags = BindingFlags.Static | BindingFlags.NonPublic;
        private static string dataParent;
        private static string savesRoot;
        private static string stashFolder;

        internal static bool Installed => savesRoot != null;
        internal static string SavesRoot => savesRoot;
        internal static string StashFolder => stashFolder;

        internal static void Install(string profileRoot)
        {
            if (savesRoot != null) throw new InvalidOperationException("Save routing is already installed.");
            if (typeof(SaveManager).Assembly.ManifestModule.ModuleVersionId != ExpectedMvid)
                throw new InvalidOperationException("Save routing requires the exact installed Kingmaker assembly.");
            var full = Path.GetFullPath(profileRoot).TrimEnd(Path.DirectorySeparatorChar);
            var saves = Path.Combine(full, "Saved Games");
            var areas = Path.Combine(full, "Areas");
            if (!Directory.Exists(saves) || (File.GetAttributes(saves) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidOperationException("The owned profile has no plain Saved Games directory.");
            if (!Directory.Exists(areas) || Directory.EnumerateFileSystemEntries(areas).Any() || (File.GetAttributes(areas) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidOperationException("The owned area stash must be an empty plain directory.");
            var harmony = HarmonyInstance.Create(HarmonyId);
            dataParent = full; savesRoot = saves; stashFolder = areas;
            try
            {
                Patch(harmony, typeof(SaveManager), "get_SavePath", 0x0600800C, Type.EmptyTypes, nameof(SavePathPrefix), null);
                Patch(harmony, typeof(SaveManager), "UpdateSaveListAsync", 0x0600800E, Type.EmptyTypes, null, nameof(SaveRootTranspiler));
                Patch(harmony, typeof(SaveManager), "PrepareSave", 0x06008025, new[] { typeof(SaveInfo) }, null, nameof(SaveRootTranspiler));
                var stash = typeof(SaveManager).Assembly.GetType("Kingmaker.EntitySystem.Persistence.AreaDataStash", true);
                var folder = stash.GetProperty("Folder", BindingFlags.Static | BindingFlags.Public).GetGetMethod();
                if (folder == null || folder.MetadataToken != 0x06007F83) throw new InvalidOperationException("Native area stash getter is missing.");
                harmony.Patch(folder, new HarmonyMethod(typeof(NativeSaveRouting).GetMethod(nameof(StashPrefix), Flags)), null, null);
                Patch(harmony, typeof(SteamSavesReplicator), "Initialize", 0x0600804B, Type.EmptyTypes, nameof(CloudPrefix), null);
                Patch(harmony, typeof(SteamSavesReplicator), "PullUpdates", 0x06008043, Type.EmptyTypes, nameof(CloudPrefix), null);
                Patch(harmony, typeof(SteamSavesReplicator), "RegisterSave", 0x06008042, new[] { typeof(SaveInfo) }, nameof(CloudPrefix), null);
                Patch(harmony, typeof(SteamSavesReplicator), "DeleteSave", 0x06008044, new[] { typeof(SaveInfo) }, nameof(CloudPrefix), null);
                Patch(harmony, typeof(SavesStorageAccess), "Upload", 0x06008250, new[] { typeof(SaveInfo), typeof(SaveCreateDTO) }, nameof(CloudPrefix), null);
            }
            catch
            {
                dataParent = null; savesRoot = null; stashFolder = null;
                try { harmony.UnpatchAll(HarmonyId); } catch { }
                throw;
            }
        }

        private static void Patch(HarmonyInstance harmony, Type type, string name, int token, Type[] parameters, string prefix, string transpiler)
        {
            var method = type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance, null, parameters, null);
            if (method == null || method.MetadataToken != token)
                throw new InvalidOperationException("Save routing contract changed: " + type.FullName + "." + name);
            harmony.Patch(method,
                prefix == null ? null : new HarmonyMethod(typeof(NativeSaveRouting).GetMethod(prefix, Flags)),
                null,
                transpiler == null ? null : new HarmonyMethod(typeof(NativeSaveRouting).GetMethod(transpiler, Flags)));
        }

        internal static string ObservedStashFolder => (string)typeof(SaveManager).Assembly
            .GetType("Kingmaker.EntitySystem.Persistence.AreaDataStash", true).GetProperty("Folder").GetValue(null, null);

        // Every Harmony owner with a patch anywhere in this process; KMC's own
        // ids would appear here if its assembly were loaded and patching.
        internal static string[] PatchOwners()
        {
            var owners = new HashSet<string>(StringComparer.Ordinal);
            var harmony = HarmonyInstance.Create(HarmonyId + ".Inspection");
            foreach (var method in harmony.GetPatchedMethods().ToArray())
            {
                var info = harmony.GetPatchInfo(method);
                if (info == null) continue;
                foreach (var owner in info.Owners) owners.Add(owner);
            }
            return owners.OrderBy(o => o, StringComparer.Ordinal).ToArray();
        }

        private static bool SavePathPrefix(ref string __result)
        {
            if (savesRoot == null) return true;
            __result = savesRoot;
            return false;
        }

        private static bool StashPrefix(ref string __result)
        {
            if (stashFolder == null) return true;
            __result = stashFolder;
            return false;
        }

        private static bool CloudPrefix() => savesRoot == null;

        private static string IsolatedDataParent() => dataParent ?? Kingmaker.Utility.ApplicationPaths.persistentDataPath;

        private static IEnumerable<CodeInstruction> SaveRootTranspiler(IEnumerable<CodeInstruction> source, MethodBase __originalMethod)
        {
            var code = source.ToList();
            var expected = __originalMethod.MetadataToken == 0x0600800E ? 1 : __originalMethod.MetadataToken == 0x06008025 ? 2 : -1;
            var sites = code.Where(c => c.opcode == OpCodes.Call && c.operand is MethodInfo &&
                ((MethodInfo)c.operand).Module == typeof(SaveManager).Module &&
                ((MethodInfo)c.operand).MetadataToken == 0x06001BC7).ToArray();
            if (sites.Length != expected)
                throw new InvalidOperationException("Native save-root call sites changed; routing cannot be established.");
            var replacement = typeof(NativeSaveRouting).GetMethod(nameof(IsolatedDataParent), Flags);
            foreach (var site in sites) site.operand = replacement;
            return code;
        }
    }
}
