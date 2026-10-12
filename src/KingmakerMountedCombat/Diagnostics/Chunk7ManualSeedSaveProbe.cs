using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Harmony12;
using Kingmaker;
using Kingmaker.EntitySystem.Persistence;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // Read-only receipts for the already-owned native write. No worker, iterator,
    // save descriptor, slot, snapshot or resource is created by this observer.
    internal sealed class Chunk7ManualSeedSaveProbe : IDisposable
    {
        private const string HarmonyId = "KingmakerMountedCombat.Diagnostics.ManualSeedSave";
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        private static Chunk7ManualSeedSaveProbe active;
        private readonly Game game;
        private readonly SaveManager manager;
        private readonly MountedPersistenceService persistence;
        private readonly Func<object, int> identity;
        private readonly Action assertWorld;
        private readonly string leaf;
        private readonly JArray events = new JArray(), hooks = new JArray();
        private readonly List<string> errors = new List<string>();
        private readonly Dictionary<string, FieldInfo> fields = new Dictionary<string, FieldInfo>();
        private HarmonyInstance harmony;
        private bool attempted, installed, disposed, startingOwned, stagedOwned;
        private int inside, requests, prepares, starts, stages, completions, callbacks;
        private SaveInfo expected, prepared;
        internal bool InCallback => inside != 0;
        internal bool Complete => installed && requests == 1 && prepares == 1 && starts == 1 && stages == 1 && completions == 1 && callbacks == 1 && errors.Count == 0;
        internal SaveInfo Written => prepared;
        internal Chunk7ManualSeedSaveProbe(Game game, MountedPersistenceService persistence, Func<object, int> identity, Action assertWorld, string leaf)
        {
            this.game = game ?? throw new ArgumentNullException(nameof(game)); manager = game.SaveManager;
            this.persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
            this.identity = identity ?? throw new ArgumentNullException(nameof(identity));
            this.assertWorld = assertWorld ?? throw new ArgumentNullException(nameof(assertWorld)); this.leaf = leaf;
        }
        internal void Install()
        {
            if (disposed) throw new ObjectDisposedException(nameof(Chunk7ManualSeedSaveProbe));
            if (installed) return;
            Require(!attempted && active == null, "Manual seed observation already owns or attempted installation."); attempted = true;
            Require(typeof(Game).Module.ModuleVersionId == new Guid("07fa1e4d-8618-41b3-9b8d-faa17d3b26f7"), "Installed native save module differs.");
            var scope = typeof(MountedPersistenceService).GetNestedType("SaveScope", BindingFlags.NonPublic);
            Require(scope != null, "Production save scope contract is unavailable.");
            foreach (var pair in new[] { new KeyValuePair<string, Type>("Requested", typeof(SaveInfo)), new KeyValuePair<string, Type>("Prepared", typeof(SaveInfo)),
                new KeyValuePair<string, Type>("Worker", typeof(Task)), new KeyValuePair<string, Type>("Routine", typeof(IEnumerator<object>)),
                new KeyValuePair<string, Type>("World", typeof(Player)), new KeyValuePair<string, Type>("Completed", typeof(bool)) })
            {
                var field = scope.GetField(pair.Key, Flags); Require(field != null && field.FieldType == pair.Value, "Production save scope field differs: " + pair.Key); fields.Add(pair.Key, field);
            }
            harmony = HarmonyInstance.Create(HarmonyId); active = this;
            Patch(typeof(SaveManager).GetMethod("SaveRoutine", Flags, null, new[] { typeof(SaveInfo), typeof(bool) }, null), 0x06008029, "SaveBefore", null);
            Patch(typeof(SaveManager).GetMethod("PrepareSave", Flags, null, new[] { typeof(SaveInfo) }, null), 0x06008025, null, "PreparedAfter");
            Patch(typeof(MountedPersistenceService).GetMethod("RecordCompletedSave", Flags, null, new[] { scope }, null), null, "CompletedBefore", null);
            startingOwned = true; persistence.SaveSnapshotStarting += Starting;
            stagedOwned = true; persistence.SaveSnapshotStaged += Staged;
            installed = true;
        }
        private void Patch(MethodInfo method, int? token, string before, string after)
        {
            Require(method != null && (!token.HasValue || method.MetadataToken == token.Value), "Manual seed observation method differs.");
            harmony.Patch(method, before == null ? null : new HarmonyMethod(typeof(Hooks).GetMethod(before, Flags)) { prioritiy = Priority.First },
                after == null ? null : new HarmonyMethod(typeof(Hooks).GetMethod(after, Flags)) { prioritiy = Priority.Last });
            hooks.Add(new JObject { ["type"] = method.DeclaringType.FullName, ["method"] = method.Name, ["token"] = method.MetadataToken.ToString("X8"), ["moduleMvid"] = method.Module.ModuleVersionId.ToString("D") });
        }
        internal void Expect(SaveInfo value)
        {
            Require(installed && expected == null && value != null && !value.IsActuallySaved && value.Type == SaveInfo.SaveType.Manual &&
                value.Name == Chunk7ManualSeedSelection.OutputName, "Manual seed observer requires one new exact native descriptor."); expected = value;
        }
        private void Observe(Action action)
        {
            inside++;
            try { Require(events.Count < 16, "Manual seed observation exceeded its finite event bound."); Require(ReferenceEquals(Game.Instance, game) && ReferenceEquals(game.SaveManager, manager), "Save observer world/manager changed."); action(); }
            catch (Exception error) { if (!errors.Contains(error.ToString())) errors.Add(error.ToString()); }
            finally { inside--; }
        }
        private JObject Record(string kind, SaveInfo value = null)
        {
            var item = new JObject { ["sequence"] = events.Count + 1, ["kind"] = kind, ["frame"] = Time.frameCount,
                ["game"] = identity(game), ["saveManager"] = identity(manager), ["player"] = identity(game.Player), ["requested"] = identity(expected),
                ["descriptor"] = identity(value), ["fileName"] = value?.FileName, ["internalName"] = value?.Name,
                ["type"] = value?.Type.ToString(), ["gameId"] = value?.GameId, ["gameName"] = value?.GameName, ["area"] = value?.Area?.AssetGuidThreadSafe,
                ["path"] = value?.FolderName, ["snapshotCount"] = persistence.SnapshotCount, ["completedSaveCount"] = persistence.CompletedSaveCount };
            events.Add(item); return item;
        }
        private void Starting() => Observe(() => { Require(++starts == 1 && prepared != null, "Manual seed header is duplicate or unprepared."); Record("snapshot-starting", prepared); });
        private void Staged() => Observe(() => { Require(++stages == 1 && prepared != null, "Manual seed snapshot is duplicate or unprepared."); Record("snapshot-staged", prepared); });
        private void CompleteScope(object scope)
        {
            Require(scope != null && ReferenceEquals(fields["Requested"].GetValue(scope), expected) &&
                ReferenceEquals(fields["Prepared"].GetValue(scope), prepared) && ReferenceEquals(fields["World"].GetValue(scope), game.Player) &&
                (bool)fields["Completed"].GetValue(scope), "Completed scope is not the exact admitted native save.");
            var worker = fields["Worker"].GetValue(scope) as Task; var routine = fields["Routine"].GetValue(scope) as IEnumerator<object>; Task nativeWorker;
            Require(worker != null && routine != null && NativeSaveWorkerBoundary.TryReadWorker(routine, out nativeWorker) && ReferenceEquals(worker, nativeWorker) &&
                worker.IsCompleted && !worker.IsFaulted && !worker.IsCanceled && prepared.OperationState == SaveInfo.StateType.None && prepared.HasFileOnDisk,
                "Exact native iterator/worker did not settle successfully.");
            var item = Record("native-worker-completed", prepared); item["scope"] = identity(scope); item["iterator"] = identity(routine);
            item["iteratorType"] = routine.GetType().FullName; item["worker"] = identity(worker); item["workerId"] = worker.Id;
            item["nativeWorkerMatches"] = true; item["workerStatus"] = worker.Status.ToString(); item["workerCompleted"] = true;
            item["workerFaulted"] = worker.IsFaulted; item["workerCanceled"] = worker.IsCanceled;
        }
        internal void ObserveCallback() => Observe(() => { assertWorld(); Require(++callbacks == 1 && completions == 1, "Save callback is duplicate or precedes exact worker completion."); Record("native-save-callback", prepared); });
        internal JObject Capture() => new JObject { ["contract"] = "manual-seed-native-save-v1", ["installed"] = installed, ["installationAttempted"] = attempted,
            ["complete"] = Complete, ["requests"] = requests, ["prepares"] = prepares, ["starts"] = starts, ["stages"] = stages,
            ["completions"] = completions, ["callbacks"] = callbacks, ["events"] = events.DeepClone(), ["hooks"] = hooks.DeepClone(), ["errors"] = new JArray(errors) };
        private static class Hooks
        {
            internal static void SaveBefore(SaveManager __instance, SaveInfo saveInfo, bool forceAuto)
            {
                var p = active; if (p == null) return;
                p.Observe(() => { p.assertWorld(); Require(++p.requests == 1 && ReferenceEquals(__instance, p.manager) && ReferenceEquals(saveInfo, p.expected) && !forceAuto,
                    "Manual seed native request substituted its manager, descriptor, count or category."); p.Record("native-save-request", saveInfo); });
            }
            internal static void PreparedAfter(SaveManager __instance, SaveInfo save)
            {
                var p = active; if (p == null) return;
                p.Observe(() => { Require(++p.prepares == 1 && p.requests == 1 && ReferenceEquals(__instance, p.manager) && save != null &&
                    save.Type == SaveInfo.SaveType.Manual && save.Name == Chunk7ManualSeedSelection.OutputName && save.FileName == p.leaf &&
                    save.GameId == p.game.Player.GameId && save.Area?.AssetGuidThreadSafe == p.game.CurrentlyLoadedArea?.AssetGuidThreadSafe,
                    "Native prepared Manual seed differs from its declared leaf or world."); p.prepared = save; p.Record("native-save-prepared", save); });
            }
            internal static void CompletedBefore(MountedPersistenceService __instance, object scope)
            {
                var p = active; if (p == null) return;
                p.Observe(() => { Require(ReferenceEquals(__instance, p.persistence) && ++p.completions == 1, "Foreign/duplicate save completion."); p.CompleteScope(scope); });
            }
        }
        public void Dispose()
        {
            if (disposed) return; Require(inside == 0, "Native save observer callback retains custody."); var failures = new List<Exception>();
            try { if (startingOwned) { persistence.SaveSnapshotStarting -= Starting; startingOwned = false; } } catch (Exception error) { failures.Add(error); }
            try { if (stagedOwned) { persistence.SaveSnapshotStaged -= Staged; stagedOwned = false; } } catch (Exception error) { failures.Add(error); }
            try { harmony?.UnpatchAll(HarmonyId); } catch (Exception error) { failures.Add(error); }
            if (failures.Count != 0) throw new AggregateException("Manual seed observer retains failed releases.", failures);
            if (ReferenceEquals(active, this)) active = null; disposed = true;
        }
        private static void Require(bool value, string error) { if (!value) throw new InvalidOperationException(error); }
    }
}
