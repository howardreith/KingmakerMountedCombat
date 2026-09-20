using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Harmony12;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.EntitySystem.Persistence.SavesStorage;
using Kingmaker.Utility;
using KingmakerMountedCombat.Diagnostics;

namespace KingmakerMountedCombat.Integration
{
    // Installed only by a persistence-specific owned runtime process. This does
    // not activate save permission, copy fixtures, or modify global user settings.
    // The runtime harness must retain this isolation for the whole owned process,
    // including mod-disable scenarios; never return a live process to human roots.
    internal static class NativePersistenceIsolation
    {
        private static PersistenceSaveAuthorization authority;
        private static readonly Guid ExpectedMvid = new Guid("07fa1e4d-8618-41b3-9b8d-faa17d3b26f7");

        internal static void Bind(PersistenceSaveAuthorization authorizedScope)
        {
            if (authorizedScope == null) throw new ArgumentNullException(nameof(authorizedScope));
            if (authority != null) throw new InvalidOperationException("Save isolation is already bound for this process.");
            authority = authorizedScope;
        }

        internal static void Install(HarmonyInstance harmony)
        {
            if (typeof(SaveManager).Assembly.ManifestModule.ModuleVersionId != ExpectedMvid)
                throw new InvalidOperationException("Persistence isolation requires the exact installed Kingmaker assembly.");
            Patch(harmony, typeof(SaveManager), "get_SavePath", 0x0600800C, Type.EmptyTypes, "SavePathPrefix", null);
            Patch(harmony, typeof(SaveManager), "UpdateSaveListAsync", 0x0600800E, Type.EmptyTypes, null, "SaveRootTranspiler");
            Patch(harmony, typeof(SaveManager), "PrepareSave", 0x06008025, new[] { typeof(SaveInfo) }, null, "SaveRootTranspiler");
            var loadIterator = typeof(SaveManager).Assembly.GetType("Kingmaker.EntitySystem.Persistence.SaveManager+<LoadRoutine>d__50", true);
            Patch(harmony, loadIterator, "MoveNext", 0x0600BF00, Type.EmptyTypes, null, "LoadHeaderTranspiler");
            // Every native stash read/write/clear resolves this property.
            var stash = typeof(SaveManager).Assembly.GetType("Kingmaker.EntitySystem.Persistence.AreaDataStash", true);
            var folder = stash.GetProperty("Folder", BindingFlags.Static | BindingFlags.Public).GetGetMethod();
            if (folder == null || folder.MetadataToken != 0x06007F83) throw new InvalidOperationException("Native area stash getter is missing.");
            harmony.Patch(folder, new HarmonyMethod(typeof(NativePersistenceIsolation).GetMethod(
                "StashPrefix", BindingFlags.Static | BindingFlags.NonPublic)), null, null);
            Patch(harmony, typeof(SteamSavesReplicator), "Initialize", 0x0600804B, Type.EmptyTypes, "CloudPrefix", null);
            Patch(harmony, typeof(SteamSavesReplicator), "PullUpdates", 0x06008043, Type.EmptyTypes, "CloudPrefix", null);
            Patch(harmony, typeof(SteamSavesReplicator), "RegisterSave", 0x06008042, new[] { typeof(SaveInfo) }, "CloudPrefix", null);
            Patch(harmony, typeof(SteamSavesReplicator), "DeleteSave", 0x06008044, new[] { typeof(SaveInfo) }, "CloudPrefix", null);
            Patch(harmony, typeof(SavesStorageAccess), "Upload", 0x06008250,
                new[] { typeof(SaveInfo), typeof(SaveCreateDTO) }, "CloudPrefix", null);
        }

        private static void Patch(HarmonyInstance harmony, Type type, string name, int token,
            Type[] parameters, string prefix, string transpiler)
        {
            var method = type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Static | BindingFlags.Instance, null, parameters, null);
            if (method == null || method.MetadataToken != token)
                throw new InvalidOperationException("Persistence isolation contract changed: " + type.FullName + "." + name);
            var flags = BindingFlags.Static | BindingFlags.NonPublic;
            try
            {
            harmony.Patch(method, prefix == null ? null : new HarmonyMethod(typeof(NativePersistenceIsolation).GetMethod(prefix, flags)),
                null, transpiler == null ? null : new HarmonyMethod(typeof(NativePersistenceIsolation).GetMethod(transpiler, flags)));
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("Persistence patch construction failed: " + type.FullName + "." + name, exception);
            }
        }

        private static bool SavePathPrefix(ref string __result)
        {
            if (authority == null) return true;
            __result = authority.Root;
            return false;
        }

        private static bool StashPrefix(ref string __result)
        {
            if (authority == null) return true;
            __result = Path.Combine(Path.GetDirectoryName(authority.Root), "Areas");
            return false;
        }

        internal static string ObservedStashFolder => (string)typeof(SaveManager).Assembly
            .GetType("Kingmaker.EntitySystem.Persistence.AreaDataStash", true).GetProperty("Folder").GetValue(null, null);

        private static bool CloudPrefix() => authority == null;

        private static string IsolatedDataParent() => authority == null ?
            ApplicationPaths.persistentDataPath : Path.GetDirectoryName(authority.Root);

        private static IEnumerable<CodeInstruction> LoadHeaderTranspiler(IEnumerable<CodeInstruction> source)
        {
            var code = source.ToList();
            var jsonSites = code.Where(c => c.opcode == OpCodes.Callvirt && c.operand is MethodInfo &&
                ((MethodInfo)c.operand).Module == typeof(SaveManager).Module &&
                ((MethodInfo)c.operand).MetadataToken == 0x06007FAE).ToArray();
            var commitSites = code.Where(c => c.opcode == OpCodes.Callvirt && c.operand is MethodInfo &&
                ((MethodInfo)c.operand).Module == typeof(SaveManager).Module &&
                ((MethodInfo)c.operand).MetadataToken == 0x06007FB3).ToArray();
            if (jsonSites.Length != 1 || commitSites.Length != 1)
                throw new InvalidOperationException("Native load-header write contract changed.");
            jsonSites[0].opcode = OpCodes.Call;
            jsonSites[0].operand = typeof(NativePersistenceIsolation).GetMethod("LoadHeaderJson", BindingFlags.Static | BindingFlags.NonPublic);
            commitSites[0].opcode = OpCodes.Call;
            commitSites[0].operand = typeof(NativePersistenceIsolation).GetMethod("LoadHeaderCommit", BindingFlags.Static | BindingFlags.NonPublic);
            return code;
        }

        private static void LoadHeaderJson(ISaver saver, string name, string json)
        {
            if (authority == null) { saver.SaveJson(name, json); return; }
            if (name != "header") throw new InvalidOperationException("Unexpected write while loading an isolated source archive.");
            // Native LoadedTimes changes only in memory during the test process.
            // The exact loaded archive stays byte-identical; no KMC data is injected.
        }

        private static void LoadHeaderCommit(ISaver saver)
        {
            if (authority == null) saver.Save();
        }

        private static IEnumerable<CodeInstruction> SaveRootTranspiler(IEnumerable<CodeInstruction> source, MethodBase __originalMethod)
        {
            var code = source.ToList();
            var expected = __originalMethod.MetadataToken == 0x0600800E ? 1 :
                __originalMethod.MetadataToken == 0x06008025 ? 2 : -1;
            var sites = code.Where(c => c.opcode == OpCodes.Call && c.operand is MethodInfo &&
                ((MethodInfo)c.operand).Module == typeof(SaveManager).Module &&
                ((MethodInfo)c.operand).MetadataToken == 0x06001BC7).ToArray();
            if (sites.Length != expected)
                throw new InvalidOperationException("Native save-root call sites changed; isolation cannot be established.");
            var replacement = typeof(NativePersistenceIsolation).GetMethod("IsolatedDataParent", BindingFlags.Static | BindingFlags.NonPublic);
            foreach (var site in sites) site.operand = replacement;
            return code;
        }
    }
}