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
        private readonly List<string> errors = new List<string>();
        private readonly JArray events = new JArray(), hooks = new JArray();
        private UnitEntityData completionActor;
        private bool completionChargen, disposed, installationStarted, installed;
        private int completionFrame = -1, completionEntries, completionExits, saveRequests, preparedWrites;
        private SaveInfo observedSave;
        internal NativeOrdinaryAcquisitionProbe(Func<UnitEntityData> actor)
        {
            this.actor = actor ?? throw new ArgumentNullException(nameof(actor));
        }
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
            harmony = HarmonyInstance.Create(HarmonyId); active = this;
            Patch(typeof(LevelUpAutoSaveController), "HandleLevelUpComplete", 0x0600906B,
                new[] { typeof(UnitEntityData), typeof(bool) }, "CompletionBefore", "CompletionAfter");
            Patch(typeof(Game), "SaveGame", 0x06000CE4, new[] { typeof(SaveInfo), typeof(Action) }, "SaveBefore", null);
            Patch(typeof(SaveManager), "PrepareSave", 0x06008025, new[] { typeof(SaveInfo) }, null, "PreparedAfter");
            installed = true;
        }
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
        private void Record(string kind, UnitEntityData unit = null, SaveInfo save = null)
        {
            if (events.Count >= 128) throw new InvalidOperationException("Ordinary native observer exceeded its event bound.");
            events.Add(new JObject { ["kind"] = kind, ["frame"] = Time.frameCount,
                ["actorId"] = unit?.UniqueId, ["actorObject"] = Id(unit), ["characterLevel"] = unit?.Descriptor.Progression.CharacterLevel,
                ["inState"] = unit?.IsInState, ["isChargen"] = completionChargen,
                ["saveObject"] = Id(save), ["saveType"] = save?.Type.ToString(), ["saveName"] = save?.Name,
                ["saveLeaf"] = save?.FileName, ["autoLevelup"] = save?.IsAutoLevelupSave,
                ["savePath"] = save?.FolderName, ["completionActorId"] = completionActor?.UniqueId,
                ["insideObservedCompletion"] = InSynchronousCallback });
        }
        private static int Id(object value) => value == null ? 0 : RuntimeHelpers.GetHashCode(value);
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
            internal static void SaveBefore(SaveInfo saveInfo, Action callback)
            {
                var p = active; if (p == null) return;
                p.Safe(() => {
                    p.saveRequests++;
                    Require(p.saveRequests == 1 && p.InSynchronousCallback && !p.completionChargen && p.completionActor.IsInState &&
                        ReferenceEquals(p.completionActor, p.actor()) && saveInfo != null && saveInfo.Type == SaveInfo.SaveType.Auto &&
                        saveInfo.IsAutoLevelupSave && callback == null,
                        "Native save was not the one actual ordinary terminal-level autosave.");
                    p.observedSave = saveInfo; p.Record("native-level-autosave-request", p.completionActor, saveInfo);
                });
            }
            internal static void PreparedAfter(SaveInfo save)
            {
                var p = active; if (p == null) return;
                p.Safe(() => {
                    p.preparedWrites++;
                    Require(p.preparedWrites == 1 && ReferenceEquals(save, p.observedSave) && save.Type == SaveInfo.SaveType.Auto &&
                        save.FileName == Chunk7OrdinaryAcquisitionSelection.AutoLeaf && save.IsAutoLevelupSave,
                        "Native prepared autosave changed descriptor/category/leaf or repeated its write.");
                    p.Record("native-level-autosave-prepared", p.actor(), save);
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
