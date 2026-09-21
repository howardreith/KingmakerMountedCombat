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
        private static bool nativeSlotRotation;
        internal static void EnableNativeSlotRotation()
        {
            if (authority == null) throw new InvalidOperationException("Native test slots require active isolated authority.");
            nativeSlotRotation = true;
        }
        internal static void DisableNativeSlotRotation() => nativeSlotRotation = false;
        internal static bool HasPendingWrites { get { lock (writeGate) return writes.Count != 0; } }
        private static readonly object writeGate = new object();
        private sealed class WriteTransaction
        {
            internal readonly SaveInfo Save;
            internal readonly string StagingPath;
            internal readonly PersistenceSaveAuthorization.WriteLease Lease;
            internal bool Committed;
            internal WriteTransaction(SaveInfo save, string path, PersistenceSaveAuthorization.WriteLease lease)
            { Save = save; StagingPath = path; Lease = lease; }
        }
        private static readonly Dictionary<ISaver, WriteTransaction> writes =
            new Dictionary<ISaver, WriteTransaction>();
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
            var settingsRefresh = typeof(Kingmaker.UI.SettingsUI.SettingsRoot).GetMethod("HandleSettingsUpdated",
                BindingFlags.Public | BindingFlags.Static);
            if (settingsRefresh == null || settingsRefresh.MetadataToken != 0x0600346B)
                throw new InvalidOperationException("Native settings cache refresh contract changed.");
            harmony.Patch(settingsRefresh, null, new HarmonyMethod(typeof(NativePersistenceIsolation).GetMethod(
                "SettingsRefreshPostfix", BindingFlags.NonPublic | BindingFlags.Static)), null);
            Patch(harmony, typeof(Kingmaker.UI.SettingsUI.SettingsEntitySlider), "get_CurrentValue", 0x060033EE,
                Type.EmptyTypes, "NativeSlotCountPrefix", null);
            Patch(harmony, typeof(Kingmaker.UI.SettingsUI.SettingsEntityBool), "get_CurrentValue", 0x06003364,
                Type.EmptyTypes, "NativeAutosaveEnabledPrefix", null);
            Patch(harmony, typeof(SaveManager), "get_SavePath", 0x0600800C, Type.EmptyTypes, "SavePathPrefix", null);
            Patch(harmony, typeof(SaveManager), "UpdateSaveListAsync", 0x0600800E, Type.EmptyTypes, null, "SaveRootTranspiler");
            Patch(harmony, typeof(SaveManager), "PrepareSave", 0x06008025, new[] { typeof(SaveInfo) }, null, "SaveRootTranspiler");
            Patch(harmony, NativeZipSaver, "SaveJson", 0x06008063, new[] { typeof(string), typeof(string) }, "LoadHeaderJsonPrefix", null);
            Patch(harmony, NativeZipSaver, "Clear", 0x06008067, Type.EmptyTypes, "ClearPrefix", null);
            Patch(harmony, NativeZipSaver, "RenameFile", 0x0600806D, new[] { typeof(string) }, "RenamePrefix", null);
            Patch(harmony, NativeZipSaver, "Save", 0x06008068, Type.EmptyTypes, "LoadHeaderCommitPrefix", null);
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
                name == "Save" && type == NativeZipSaver ? new HarmonyMethod(typeof(NativePersistenceIsolation).GetMethod("CommitPostfix", flags)) : null,
                transpiler == null ? null : new HarmonyMethod(typeof(NativePersistenceIsolation).GetMethod(transpiler, flags)));
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("Persistence patch construction failed: " + type.FullName + "." + name, exception);
            }
        }

        private static bool NativeSlotCountPrefix(Kingmaker.UI.SettingsUI.SettingsEntitySlider __instance, ref float __result)
        {
            if (authority == null || !nativeSlotRotation) return true;
            var settings = Kingmaker.UI.SettingsUI.SettingsRoot.Instance;
            if (!ReferenceEquals(__instance, settings.QuicksaveSlots) && !ReferenceEquals(__instance, settings.AutosaveSlots)) return true;
            __result = 1f;
            return false;
        }

        private static bool NativeAutosaveEnabledPrefix(Kingmaker.UI.SettingsUI.SettingsEntityBool __instance, ref bool __result)
        {
            if (authority == null || !nativeSlotRotation ||
                !ReferenceEquals(__instance, Kingmaker.UI.SettingsUI.SettingsRoot.Instance.AutosaveEnabled)) return true;
            __result = true;
            return false;
        }

        private static void SettingsRefreshPostfix()
        {
            if (authority != null) RuntimeAutomationHost.ReapplyDeclaredPersistenceMode();
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

        private sealed class ReadOnlyLoadScope
        {
            internal readonly string Path;
            internal readonly HashSet<ISaver> HeaderUpdates = new HashSet<ISaver>();
            internal ReadOnlyLoadScope(string path) { Path = path; }
        }

        private static ReadOnlyLoadScope readOnlyLoad;
        private static readonly Type NativeZipSaver = typeof(SaveManager).Assembly.GetType(
            "Kingmaker.EntitySystem.Persistence.ZipSaver", true);

        internal static IEnumerator<object> WrapReadOnlyLoad(IEnumerator<object> routine, string archivePath)
        {
            if (authority == null || routine == null) return routine;
            var scope = new ReadOnlyLoadScope(archivePath);
            var acquired = false;
            return new KingmakerMountedCombat.Domain.ScopedEnumerator<object>(routine, () =>
            {
                if (readOnlyLoad != null) throw new InvalidOperationException("Overlapping isolated native loads.");
                authority.AssertReadableArchive(archivePath);
                readOnlyLoad = scope;
                acquired = true;
            }, () =>
            {
                if (!acquired) return;
                scope.HeaderUpdates.Clear();
                if (ReferenceEquals(readOnlyLoad, scope)) readOnlyLoad = null;
            });
        }

        private static bool IsSelectedReadOnlyArchive(ISaver saver)
        {
            if (authority == null || readOnlyLoad == null) return false;
            if (saver == null || saver.GetType() != NativeZipSaver)
                throw new InvalidOperationException("Isolated loading requires the exact native archive saver.");
            var path = (string)NativeZipSaver.GetProperty("FolderName").GetValue(saver, null);
            return string.Equals(path, readOnlyLoad.Path, StringComparison.Ordinal);
        }

        private static bool LoadHeaderJsonPrefix(ISaver __instance, string name)
        {
            if (!IsSelectedReadOnlyArchive(__instance)) return true;
            if (name != "header") throw new InvalidOperationException("Unexpected mutation of the selected read-only archive.");
            readOnlyLoad.HeaderUpdates.Add(__instance);
            return false;
        }

        private static bool LoadHeaderCommitPrefix(ISaver __instance)
        {
            if (!IsSelectedReadOnlyArchive(__instance))
            {
                if (authority != null)
                {
                    lock (writeGate) if (!writes.ContainsKey(__instance)) throw new InvalidOperationException("Native commit has no authorized run-owned write lease.");
                    NativeMountedSaveStorage.ConstrainIsolatedCommit(__instance, authority.Root);
                }
                return true;
            }
            if (!readOnlyLoad.HeaderUpdates.Remove(__instance))
                throw new InvalidOperationException("Read-only load attempted an unrecognized native archive commit.");
            // Preserve the actual source archive while native LoadedTimes updates
            // remain in the descriptor. This is never reported as a save write.
            return false;
        }

        internal static RuntimeSaveTarget Project(SaveInfo save) => new RuntimeSaveTarget
        {
            InternalName = save.Name, FileName = save.FileName, FullPath = save.FolderName,
            SaveType = save.Type.ToString(), GameId = save.GameId, GameName = save.GameName,
            Area = save.Area?.AssetGuidThreadSafe
        };

        internal static RuntimeSaveTarget ProjectNewRequest(SaveManager manager, SaveInfo save)
        {
            if (authority == null || manager.SavePath != authority.Root)
                throw new InvalidOperationException("Native write projection requires exact active isolation.");
            var suffix = save.Type == SaveInfo.SaveType.Manual ? "_" +
                System.Text.RegularExpressions.Regex.Replace(save.Name, "[^a-zA-Z0-9]", "_") : string.Empty;
            var leaf = save.Type + "_" + manager.FindUnusedSaveNumber(save.Type) + suffix + ".zks";
            return authority.ProjectNewRequest(new RuntimeSaveTarget
            {
                InternalName = save.Name, FileName = leaf, FullPath = Path.Combine(authority.Root, leaf),
                SaveType = save.Type.ToString(), GameId = Kingmaker.Game.Instance.Player.GameId,
                GameName = Kingmaker.Game.Instance.Player.MainCharacter.Value.CharacterName,
                Area = Kingmaker.Game.Instance.CurrentlyLoadedArea.AssetGuidThreadSafe
            }, authority.Root);
        }

        internal static void ObservePreparedWrite(SaveInfo save)
        {
            if (authority == null) return;
            lock (writeGate)
            {
                if (save?.Saver == null || writes.ContainsKey(save.Saver))
                    throw new InvalidOperationException("Native writer was allocated ambiguously.");
                writes.Add(save.Saver, new WriteTransaction(save, save.FolderName,
                    authority.BeginWrite(Project(save), authority.Root)));
            }
        }

        internal static PersistenceSaveAuthorization.ReplacementLease BeginArchiveReplacement(string source, string destination) =>
            authority?.BeginReplacement(source, destination);

        private static void CommitPostfix(ISaver __instance)
        {
            if (authority == null || IsSelectedReadOnlyArchive(__instance)) return;
            lock (writeGate)
            {
                if (!writes.TryGetValue(__instance, out var transaction) || transaction.Committed)
                    throw new InvalidOperationException("Native commit lost its unique active transaction.");
                transaction.Lease.Complete();
                transaction.Committed = true;
            }
        }

        internal static void ObserveWorkerComplete(SaveInfo saveInfo)
        {
            if (authority == null) return;
            lock (writeGate)
            {
                foreach (var item in writes.Where(w => ReferenceEquals(w.Value.Save, saveInfo)).ToArray())
                {
                    try { item.Value.Lease.Dispose(); }
                    finally { writes.Remove(item.Key); }
                }
            }
        }

        private static bool ClearPrefix(ISaver __instance)
        {
            if (authority == null) return true;
            lock (writeGate)
            {
                if (!writes.TryGetValue(__instance, out var transaction) ||
                    __instance.GetType() != NativeZipSaver ||
                    (string)NativeZipSaver.GetProperty("FolderName").GetValue(__instance, null) != transaction.StagingPath)
                    throw new InvalidOperationException("Native deletion is outside its exact active staging transaction.");
                // Clear's native file deletion swallows IO failures. Close its
                // cached handle first, then require exact owned cleanup success.
                __instance.Dispose();
                if (transaction.Committed) authority.DeleteCompletedStaging(transaction.StagingPath);
                else transaction.Lease.ClearStaging();
                return false;
            }
        }

        private static void RenamePrefix()
        {
            if (authority != null)
                throw new InvalidOperationException("Native rename is not authorized by the single-write bootstrap contract.");
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