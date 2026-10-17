using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Harmony12;
using Kingmaker;
using Kingmaker.Controllers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // Observes installed ordinary UI completion and the save it actually emits.
    // No callback, save, return value or native argument is supplied by this probe.
    internal sealed class NativeOrdinaryAcquisitionProbe : IDisposable
    {
        private const string HarmonyId = "KingmakerMountedCombat.Diagnostics.Chunk7Ordinary";
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
        private static NativeOrdinaryAcquisitionProbe active;
        private HarmonyInstance harmony;
        private readonly Func<UnitEntityData> actor;
        private readonly string preparedLeaf;
        private readonly List<string> errors = new List<string>();
        private readonly JArray events = new JArray(), hooks = new JArray();
        private UnitEntityData completionActor;
        private bool completionChargen, disposed, installationStarted, installed;
        private int completionFrame = -1, completionEntries, completionExits, saveRequests, preparedWrites;
        private SaveInfo requestedSave, observedSave;
        private string requestedName;
        private Game game;
        private SaveManager saveManager;
        internal NativeOrdinaryAcquisitionProbe(Func<UnitEntityData> actor) : this(actor, false) { }
        internal static NativeOrdinaryAcquisitionProbe ForAssistedFixture(Func<UnitEntityData> actor) => new NativeOrdinaryAcquisitionProbe(actor, true);
        // Owner-seed setup: the exact prepared leaf of its reserved level-up Auto (Auto_1, then the Auto_2 staging copy).
        internal static NativeOrdinaryAcquisitionProbe ForOwnerSeedSetup(Func<UnitEntityData> actor, string preparedLeaf) =>
            preparedLeaf == PersistenceSaveAuthorization.OwnerSeedAutoLeaf || preparedLeaf == PersistenceSaveAuthorization.OwnerSeedStagingLeaf
                ? new NativeOrdinaryAcquisitionProbe(actor, preparedLeaf) : throw new ArgumentException("Owner-seed prepared leaf is not a reserved setup leaf.");
        private NativeOrdinaryAcquisitionProbe(Func<UnitEntityData> actor, bool assistedFixture)
            : this(actor, assistedFixture ? Chunk7AssistedFixtureSelection.LevelupStagingLeaf : Chunk7OrdinaryAcquisitionSelection.AutoLeaf) { }
        private NativeOrdinaryAcquisitionProbe(Func<UnitEntityData> actor, string preparedLeaf)
        {
            this.actor = actor ?? throw new ArgumentNullException(nameof(actor));
            this.preparedLeaf = preparedLeaf;
        }
        internal int SaveRequests => saveRequests;
        internal int PreparedWrites => preparedWrites;
        // The scenario assigns its field before this fallible operation. A
        // partially patched observer therefore remains owned until unpatch succeeds.
        internal void Install()
        {
            if (disposed) throw new ObjectDisposedException(nameof(NativeOrdinaryAcquisitionProbe));
            if (installed) return;
            if (installationStarted || active != null)
                throw new InvalidOperationException("An ordinary acquisition probe already owns or attempted installation.");
            installationStarted = true;
            // Native contract validation; no patch ownership exists before the Harmony boundary.
            if (typeof(Game).Module.ModuleVersionId != new Guid("07fa1e4d-8618-41b3-9b8d-faa17d3b26f7"))
                throw new InvalidOperationException("Ordinary acquisition observer native MVID differs.");
            game = Game.Instance; saveManager = game?.SaveManager;
            Require(game != null && saveManager != null, "Ordinary native save world is unavailable.");
            harmony = HarmonyInstance.Create(HarmonyId); active = this;
            Patch(typeof(LevelUpAutoSaveController), "HandleLevelUpComplete", 0x0600906B,
                new[] { typeof(UnitEntityData), typeof(bool) }, "CompletionBefore", "CompletionAfter");
            Patch(typeof(Game), "SaveGame", 0x06000CE4, new[] { typeof(SaveInfo), typeof(Action) }, "SaveBefore", null);
            Patch(typeof(SaveManager), "PrepareSave", 0x06008025, new[] { typeof(SaveInfo) }, null, "PreparedAfter");
            installed = true;
        }
        internal SaveInfo ObservedRequestedSave => requestedSave;
        internal SaveInfo ObservedSave => observedSave;
        internal bool InSynchronousCallback => completionActor != null && completionFrame == Time.frameCount;
        internal bool Complete => installed && errors.Count == 0 && completionEntries == completionExits && saveRequests == 1 && preparedWrites == 1;
        internal JObject Capture() => new JObject { ["hooks"] = hooks.DeepClone(), ["events"] = events.DeepClone(),
            ["errors"] = new JArray(errors), ["completionEntries"] = completionEntries, ["completionExits"] = completionExits,
            ["saveRequests"] = saveRequests, ["preparedWrites"] = preparedWrites, ["installationAttempted"] = installationStarted,
            ["installed"] = installed, ["complete"] = Complete };
        private void Patch(Type type, string name, int token, Type[] parameters, string prefix, string postfix)
        {
            var method = type.GetMethod(name, Flags, null, parameters, null);
            if (method == null || method.MetadataToken != token) throw new MissingMethodException("Ordinary native observer contract differs: " + type.FullName + "." + name);
            harmony.Patch(method, prefix == null ? null : new HarmonyMethod(typeof(Hooks).GetMethod(prefix, Flags)) { prioritiy = Priority.First },
                postfix == null ? null : new HarmonyMethod(typeof(Hooks).GetMethod(postfix, Flags)) { prioritiy = Priority.Last });
            hooks.Add(new JObject { ["type"] = type.FullName, ["method"] = name, ["token"] = token.ToString("X8"),
                ["moduleMvid"] = method.Module.ModuleVersionId.ToString("D") });
        }
        private void Safe(Action operation)
        {
            try { operation(); }
            catch (Exception error) { var value = error.ToString(); if (!errors.Contains(value)) errors.Add(value); }
        }
        private JObject Record(string kind, UnitEntityData unit = null, SaveInfo save = null)
        {
            if (events.Count >= 128) throw new InvalidOperationException("Ordinary native observer exceeded its event bound.");
            var row = new JObject { ["kind"] = kind, ["frame"] = Time.frameCount,
                ["actorId"] = unit?.UniqueId, ["actorObject"] = Id(unit), ["characterLevel"] = unit?.Descriptor.Progression.CharacterLevel,
                ["inState"] = unit?.IsInState, ["isChargen"] = completionChargen,
                ["saveObject"] = Id(save), ["saveType"] = save?.Type.ToString(), ["saveName"] = save?.Name,
                ["saveLeaf"] = save?.FileName, ["autoLevelup"] = save?.IsAutoLevelupSave,
                ["savePath"] = save?.FolderName, ["completionActorId"] = completionActor?.UniqueId,
                ["insideObservedCompletion"] = InSynchronousCallback };
            events.Add(row); return row;
        }
        internal static int Id(object value) => value == null ? 0 : RuntimeHelpers.GetHashCode(value);
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        private static class Hooks
        {
            internal static void CompletionBefore(UnitEntityData unit, bool isChargen)
            {
                var p = active; if (p == null) return;
                p.Safe(() => {
                    Require(!p.InSynchronousCallback && ReferenceEquals(unit, p.actor()), "Native completion overlapped or belongs to another actor.");
                    p.completionActor = unit; p.completionChargen = isChargen; p.completionFrame = Time.frameCount;
                    p.completionEntries++; p.Record("native-level-completion-enter", unit);
                });
            }
            internal static void CompletionAfter(UnitEntityData unit, bool isChargen)
            {
                var p = active; if (p == null) return;
                p.Safe(() => {
                    Require(p.InSynchronousCallback && ReferenceEquals(unit, p.completionActor) && isChargen == p.completionChargen,
                        "Native completion exit lost its exact actor/scope.");
                    p.Record("native-level-completion-exit", unit); p.completionExits++; p.completionActor = null;
                });
            }
            internal static void SaveBefore(Game __instance, SaveInfo saveInfo, Action callback)
            {
                var p = active; if (p == null) return;
                p.Safe(() => {
                    p.saveRequests++;
                    Require(ReferenceEquals(__instance, p.game) && ReferenceEquals(Game.Instance, p.game) &&
                        ReferenceEquals(p.game.SaveManager, p.saveManager) && p.saveRequests == 1 && p.InSynchronousCallback && !p.completionChargen && p.completionActor.IsInState &&
                        ReferenceEquals(p.completionActor, p.actor()) && saveInfo != null && saveInfo.Type == SaveInfo.SaveType.Auto &&
                        saveInfo.IsAutoLevelupSave && !string.IsNullOrEmpty(saveInfo.Name) && callback == null,
                        "Native save was not the one actual ordinary terminal-level autosave.");
                    p.requestedSave = saveInfo; p.requestedName = saveInfo.Name;
                    p.Record("native-level-autosave-request", p.completionActor, saveInfo);
                });
            }
            internal static void PreparedAfter(SaveManager __instance, SaveInfo save)
            {
                var p = active; if (p == null) return;
                p.Safe(() => {
                    p.preparedWrites++;
                    // Native SaveRoutine retains the request and prepares a new
                    // Type/Name copy after the synchronous level callback returns.
                    var row = p.Record("native-level-autosave-prepared", p.actor(), save);
                    row["requestedSaveObject"] = Id(p.requestedSave); row["accepted"] = false;
                    Require(p.preparedWrites == 1 && p.saveRequests == 1 && p.completionEntries == p.completionExits &&
                        !p.InSynchronousCallback && ReferenceEquals(Game.Instance, p.game) &&
                        ReferenceEquals(__instance, p.saveManager) && ReferenceEquals(p.game.SaveManager, p.saveManager) &&
                        p.requestedSave != null && p.requestedSave.Type == SaveInfo.SaveType.Auto &&
                        p.requestedSave.IsAutoLevelupSave && p.requestedSave.Name == p.requestedName &&
                        save != null && !ReferenceEquals(save, p.requestedSave) && save.Name == p.requestedName &&
                        save.Type == SaveInfo.SaveType.Auto && save.FileName == p.preparedLeaf && save.IsAutoLevelupSave,
                        "Native prepared autosave changed request/copy, world, order, category or leaf.");
                    p.observedSave = save; row["accepted"] = true;
                });
            }
        }
        public void Dispose()
        {
            if (disposed) return;
            if (InSynchronousCallback) throw new InvalidOperationException("Ordinary native callback still owns its observer.");
            harmony?.UnpatchAll(HarmonyId); disposed = true;
            if (ReferenceEquals(active, this)) active = null;
        }
    }
}
