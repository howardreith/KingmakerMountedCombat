using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Harmony12;
using Kingmaker;
using Kingmaker.Controllers.Combat;
using Kingmaker.EntitySystem.Entities;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // Read-only native entry/header observation for one selected save. Install is
    // explicit so a partial Harmony/event installation remains owned for retry.
    internal sealed class Chunk7SaveBoundaryProbe : IDisposable
    {
        private const string HarmonyId = "KingmakerMountedCombat.Diagnostics.Chunk7SaveBoundary";
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
        private static Chunk7SaveBoundaryProbe active;
        private readonly MountedPersistenceService persistence;
        private readonly UnitEntityData[] actors;
        private readonly Func<string, JObject> capture;
        private readonly JArray events = new JArray(), hooks = new JArray();
        private readonly List<string> errors = new List<string>();
        private HarmonyInstance harmony;
        private bool started, installed, disposed, startingOwned, stagedOwned;
        private int inside, starts, stages;
        private readonly int expectedSnapshots;
        internal Chunk7SaveBoundaryProbe(MountedPersistenceService persistence, UnitEntityData[] actors, Func<string, JObject> capture, int expectedSnapshots = 1)
        {
            this.persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
            this.actors = actors?.ToArray() ?? throw new ArgumentNullException(nameof(actors));
            this.capture = capture ?? throw new ArgumentNullException(nameof(capture));
            Require(expectedSnapshots == 1 || expectedSnapshots == 2, "Only one save or the selected seed/replacement pair is observable.");
            this.expectedSnapshots = expectedSnapshots;
            Require(this.actors.Length == 4 && this.actors.All(a => a != null) && this.actors.Distinct().Count() == 4,
                "Save boundary requires its four exact actors.");
        }
        internal void Install()
        {
            if (disposed) throw new ObjectDisposedException(nameof(Chunk7SaveBoundaryProbe));
            if (installed) return;
            Require(!started && active == null, "Save observer installation already owns or attempted this boundary.");
            started = true;
            ValidateNativeContract();
            harmony = HarmonyInstance.Create(HarmonyId); active = this;
            Patch(typeof(TurnController), 0x06000C3C, "PrepareBefore");
            Patch(typeof(UnitCombatState), 0x0600939D, "RoundBefore");
            startingOwned = true; persistence.SaveSnapshotStarting += Starting;
            stagedOwned = true; persistence.SaveSnapshotStaged += Staged;
            installed = true;
        }
        private static void ValidateNativeContract()
        {
            Require(typeof(Game).Module.ModuleVersionId == new Guid("07fa1e4d-8618-41b3-9b8d-faa17d3b26f7"), "Native save observation contract differs.");
        }
        private void Patch(Type type, int token, string before)
        {
            var method = type.GetMethods(Flags).SingleOrDefault(m => m.MetadataToken == token);
            Require(method != null, "Native save entry observation is unavailable.");
            harmony.Patch(method, new HarmonyMethod(typeof(Hooks).GetMethod(before, Flags)) { prioritiy = Priority.First });
            hooks.Add(new JObject { ["type"] = type.FullName, ["method"] = method.Name, ["token"] = token.ToString("X8"),
                ["moduleMvid"] = method.Module.ModuleVersionId.ToString("D") });
        }
        private void Observe(Action action)
        {
            inside++;
            try { Require(events.Count < 64, "Save boundary observation exceeded its finite bound."); action(); }
            catch (Exception error) { var text = error.ToString(); if (!errors.Contains(text)) errors.Add(text); }
            finally { inside--; }
        }
        private void Starting() => Observe(() => { Require(++starts <= expectedSnapshots && starts == stages + 1, "An extra or unordered native header was observed."); events.Add(capture("header-starting")); });
        private void Staged() => Observe(() => { Require(++stages <= expectedSnapshots && starts == stages, "An extra or unordered mounted snapshot was staged."); events.Add(capture("header-staged")); });
        private void Native(string kind, UnitEntityData actor)
        {
            // Count entry attempts even when the production prefix refuses them.
            if (!actors.Any(a => ReferenceEquals(a, actor))) return;
            Observe(() => events.Add(new JObject { ["kind"] = kind, ["frame"] = Time.frameCount, ["actorId"] = actor.UniqueId }));
        }
        internal bool Complete => HeadersComplete(expectedSnapshots);
        internal bool HeadersComplete(int count) => installed && count > 0 && count <= expectedSnapshots && starts == count && stages == count && errors.Count == 0 &&
            events.All(e => (string)e["kind"] != "prepare-before" && (string)e["kind"] != "round-before");
        internal JObject Capture() => new JObject { ["contract"] = "chunk7-selected-save-boundary-v1", ["installed"] = installed,
            ["expectedSnapshots"] = expectedSnapshots, ["starts"] = starts, ["stages"] = stages, ["complete"] = Complete, ["events"] = events.DeepClone(),
            ["hooks"] = hooks.DeepClone(), ["errors"] = new JArray(errors), ["maximumEvents"] = 64,
            ["prepareMetric"] = "native-method-entries-including-production-refused-attempts" };
        private static class Hooks
        {
            internal static void PrepareBefore(TurnController __instance) => active?.Native("prepare-before", __instance.Unit);
            internal static void RoundBefore(UnitCombatState __instance) => active?.Native("round-before", __instance.Unit);
        }
        public void Dispose()
        {
            if (disposed) return;
            Require(inside == 0, "Save observer callback retains its installation.");
            var failures = new List<Exception>();
            try { if (startingOwned) { persistence.SaveSnapshotStarting -= Starting; startingOwned = false; } }
            catch (Exception error) { failures.Add(error); }
            try { if (stagedOwned) { persistence.SaveSnapshotStaged -= Staged; stagedOwned = false; } }
            catch (Exception error) { failures.Add(error); }
            try { harmony?.UnpatchAll(HarmonyId); }
            catch (Exception error) { failures.Add(error); }
            if (failures.Count != 0) throw new AggregateException("Save observation retains failed releases.", failures);
            if (ReferenceEquals(active, this)) active = null;
            disposed = true;
        }
        private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    }
}
