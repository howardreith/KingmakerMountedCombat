using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Harmony12;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.View;
using Newtonsoft.Json.Linq;
using Pathfinding;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // Observes one exact actor/command. Never claims, releases, replaces or edits a path.
    // Pooled path contents are copied at the callback, not inspected after reuse.
    internal sealed class NativeCommandPathProbe : IDisposable
    {
        private const string HarmonyId = "KingmakerMountedCombat.Diagnostics.CommandPath";
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        private static NativeCommandPathProbe active;
        private static readonly FieldInfo Requested = typeof(UnitMovementAgent).GetField("m_RequestedPath", Flags);
        private readonly HarmonyInstance harmony;
        private readonly UnitEntityData actor;
        private readonly JArray events = new JArray(), hooks = new JArray(), errors = new JArray();
        private readonly List<Request> requests = new List<Request>();
        private Request completing;
        private UnitCommand command;
        private bool disposed;
        private sealed class Request
        {
            internal Path Path;
            internal int Sequence;
            internal Vector3 Destination;
        }
        internal NativeCommandPathProbe(UnitEntityData actor)
        {
            if (active != null) throw new InvalidOperationException("A native path observation is already active.");
            if (Requested == null || Requested.MetadataToken != 0x0400118E || Requested.FieldType != typeof(Path))
                throw new InvalidOperationException("Pinned native requested-path field changed.");
            this.actor = actor; harmony = HarmonyInstance.Create(HarmonyId); active = this;
            try
            {
                Patch(typeof(UnitMovementAgent), 0x060018A3, null, "PathAfter");
                Patch(typeof(UnitMovementAgent), 0x060018B9, "CompleteBefore", "CompleteAfter");
                Patch(typeof(UnitEntityView), 0x0600184F, "InterruptedBefore", null);
                Patch(typeof(UnitEntityView), 0x06001850, "NotFoundBefore", null);
                Patch(typeof(UnitCommand), 0x060027B2, null, "EndedAfter");
            }
            catch { Dispose(); throw; }
        }
        private void Patch(Type type, int token, string prefix, string postfix)
        {
            var method = type.GetMethods(Flags).Single(m => m.MetadataToken == token);
            try
            {
                harmony.Patch(method,
                    prefix == null ? null : new HarmonyMethod(typeof(Hooks).GetMethod(prefix, Flags)) { prioritiy = Priority.First },
                    postfix == null ? null : new HarmonyMethod(typeof(Hooks).GetMethod(postfix, Flags)) { prioritiy = Priority.Last });
                hooks.Add(new JObject { ["method"] = type.FullName + "." + method.Name, ["token"] = token.ToString("X8"),
                    ["moduleMvid"] = method.Module.ModuleVersionId.ToString(), ["prefix"] = prefix, ["postfix"] = postfix });
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("Native path instrumentation wrapper failed at " +
                    type.FullName + "." + method.Name + " token=" + token.ToString("X8") +
                    "; installed=" + hooks, exception);
            }
        }
        internal void Bind(UnitCommand value)
        {
            if (command != null || value == null || value.Executor != actor) throw new InvalidOperationException("Path observation requires one exact command.");
            command = value;
            Record("command-bound", null);
        }
        private static int Id(object value) => value == null ? 0 : RuntimeHelpers.GetHashCode(value);
        private static JObject Point(Vector3 value) => new JObject { ["x"] = value.x, ["y"] = value.y, ["z"] = value.z };
        private void Record(string boundary, Request request)
        {
            if (disposed) return;
            if (events.Count >= 256) { if (errors.Count == 0) errors.Add("Native path observation bound exceeded."); return; }
            var path = request?.Path;
            events.Add(new JObject {
                ["boundary"] = boundary, ["sequence"] = events.Count + 1, ["frame"] = Time.frameCount,
                ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks, ["actorId"] = actor.UniqueId,
                ["commandObject"] = Id(command), ["acted"] = command?.IsActed, ["finished"] = command?.IsFinished,
                ["result"] = command?.Result.ToString(), ["requestSequence"] = request?.Sequence,
                ["pathObject"] = Id(path), ["destination"] = request == null ? null : Point(request.Destination),
                ["position"] = Point(actor.Position), ["reallyMoving"] = actor.View.AgentASP.IsReallyMoving,
                ["pathError"] = path?.error, ["pathState"] = path?.CompleteState.ToString(),
                ["points"] = path?.vectorPath == null ? null : new JArray(path.vectorPath.Select(p => Point(p))) });
        }
        private void PathRequested(UnitMovementAgent agent, UnitCommand value, Vector3 destination)
        {
            if (agent != actor.View.AgentASP || !ReferenceEquals(command, value)) return;
            var path = (Path)Requested.GetValue(agent);
            if (path == null) return;
            // One in-flight path can be encountered by several ticks without a new request.
            if (requests.Count > 0 && ReferenceEquals(requests.Last().Path, path) && completing == null &&
                !events.OfType<JObject>().Any(e => (string)e["boundary"] == "path-complete-after" && (int?)e["requestSequence"] == requests.Count)) return;
            var request = new Request { Path = path, Sequence = requests.Count + 1, Destination = destination };
            requests.Add(request); Record("path-request", request);
        }
        private Request Current(Path path) => requests.LastOrDefault(r => ReferenceEquals(r.Path, path));
        private void Safe(Action action) { try { action(); } catch (Exception exception) { errors.Add(exception.ToString()); } }
        internal JObject Capture() => new JObject { ["observerHooks"] = hooks.DeepClone(), ["events"] = events.DeepClone(),
            ["errors"] = errors.DeepClone(), ["complete"] = errors.Count == 0, ["commandObject"] = Id(command), ["actorId"] = actor.UniqueId };
        internal bool FailureObserved => events.OfType<JObject>().Any(e =>
            (string)e["boundary"] == "path-not-found" || (string)e["boundary"] == "movement-interrupted");
        public void Dispose() { if (disposed) return; disposed = true; harmony.UnpatchAll(HarmonyId); if (ReferenceEquals(active, this)) active = null; }
        private static class Hooks
        {
            internal static void PathAfter(UnitMovementAgent __instance, UnitCommand command, Vector3 destination)
            { active?.Safe(() => active.PathRequested(__instance, command, destination)); }
            internal static void CompleteBefore(UnitMovementAgent __instance, Path p)
            {
                if (active == null || __instance != active.actor.View.AgentASP) return;
                active.Safe(() => {
                    active.completing = ReferenceEquals(Requested.GetValue(__instance), p) ? active.Current(p) : null;
                    if (active.completing != null) active.Record("path-complete-before", active.completing);
                });
            }
            internal static void CompleteAfter(UnitMovementAgent __instance, Path p)
            {
                if (active == null || __instance != active.actor.View.AgentASP) return;
                active.Safe(() => { if (active.completing != null) active.Record("path-complete-after", active.completing); active.completing = null; });
            }
            internal static void NotFoundBefore(UnitEntityView __instance)
            {
                if (active != null && __instance == active.actor.View && active.completing != null)
                    active.Safe(() => active.Record("path-not-found", active.completing));
            }
            internal static void InterruptedBefore(UnitEntityView __instance)
            {
                if (active == null || __instance != active.actor.View) return;
                active.Safe(() => { var request = active.Current(__instance.AgentASP.Path); if (request != null) active.Record("movement-interrupted", request); });
            }
            internal static void EndedAfter(UnitCommand __instance)
            {
                if (active != null && ReferenceEquals(active.command, __instance)) active.Safe(() => active.Record("command-ended", null));
            }
        }
    }
}
