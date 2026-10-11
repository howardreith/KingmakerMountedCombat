using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Harmony12;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // Lab draft: read-only proof of the real Stop callback surrounding this exact Mount's OnEnded.
    // A caller supplies the input. The observer never interrupts a command or changes selection.
    internal sealed class NativeMountStopInputProbe : IDisposable
    {
        private const string HarmonyId = "KingmakerMountedCombat.Diagnostics.MountStopInput";
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        private static NativeMountStopInputProbe active;
        private readonly HarmonyInstance harmony;
        private readonly UnitEntityData rider;
        private readonly UnitUseAbility command;
        private readonly Func<JObject> state;
        private readonly NativeActorAllocationTrace trace;
        private readonly int traceStart;
        private readonly JArray events = new JArray(), hooks = new JArray(), errors = new JArray();
        private int stopDepth;
        private bool disposed;

        internal NativeMountStopInputProbe(UnitEntityData rider, UnitUseAbility command, Func<JObject> state, NativeActorAllocationTrace trace)
        {
            if (active != null) throw new InvalidOperationException("An exact Mount Stop input observer is already active.");
            if (rider == null || command == null || state == null || trace == null || command.Executor != rider ||
                command.IsStarted || command.IsActed || command.IsFinished || command.ExecutionProcess != null ||
                !ReferenceEquals(rider.Commands.GetCommand(UnitCommand.CommandType.Move), command))
                throw new InvalidOperationException("Stop input observation requires the exact pending unacted Mount in the rider Move slot.");
            if (command.Spell?.Blueprint?.AssetGuid != "f053faad986631688defa003cd7bda0e")
                throw new InvalidOperationException("Stop input observation requires the native Mount ability.");
            this.rider = rider; this.command = command; this.state = state; this.trace = trace; traceStart = trace.EventCount;
            if (typeof(UnitCommand).Assembly.ManifestModule.ModuleVersionId != new Guid("07fa1e4d-8618-41b3-9b8d-faa17d3b26f7"))
                throw new InvalidOperationException("Stop input observer requires the pinned native assembly.");
            harmony = HarmonyInstance.Create(HarmonyId); active = this;
            try
            {
                Patch(typeof(SelectionManagerBase), 0x060000B9, "StopBefore", "StopAfter");
                Patch(typeof(UnitCommand), 0x060027B2, null, "EndedAfter");
            }
            catch { Dispose(); throw; }
        }
        private void Patch(Type type, int token, string prefix, string postfix)
        {
            var method = type.GetMethods(Flags).Single(m => m.MetadataToken == token);
            harmony.Patch(method,
                prefix == null ? null : new HarmonyMethod(typeof(Hooks).GetMethod(prefix, Flags)) { prioritiy = Priority.First },
                postfix == null ? null : new HarmonyMethod(typeof(Hooks).GetMethod(postfix, Flags)) { prioritiy = Priority.Last });
            hooks.Add(new JObject { ["method"] = method.DeclaringType.FullName + "." + method.Name,
                ["token"] = token.ToString("X8"), ["moduleMvid"] = method.Module.ModuleVersionId.ToString(),
                ["prefix"] = prefix, ["postfix"] = postfix });
        }
        private static int Id(object value) => value == null ? 0 : RuntimeHelpers.GetHashCode(value);
        private void Record(string boundary)
        {
            if (disposed) return;
            if (events.Count >= 16) { if (errors.Count == 0) errors.Add("Stop input callback bound exceeded."); return; }
            events.Add(new JObject { ["sequence"] = events.Count + 1, ["boundary"] = boundary,
                ["frame"] = Time.frameCount, ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks,
                ["stopDepth"] = stopDepth, ["commandObject"] = Id(command), ["casterId"] = rider.UniqueId,
                ["moveSlotObject"] = Id(rider.Commands.GetCommand(UnitCommand.CommandType.Move)),
                ["processObject"] = Id(command.ExecutionProcess), ["contextObject"] = Id(command.ExecutionProcess?.Context),
                ["started"] = command.IsStarted, ["acted"] = command.IsActed, ["finished"] = command.IsFinished,
                ["result"] = command.Result.ToString(), ["state"] = state() });
        }
        private void Safe(Action observe)
        {
            if (disposed) return;
            try { observe(); }
            catch (Exception exception) { if (errors.Count == 0) errors.Add(exception.ToString()); }
        }
        internal JObject Capture() => new JObject { ["contract"] = "native-stop-surrounds-exact-unacted-mount-terminal",
            ["commandObject"] = Id(command), ["casterId"] = rider.UniqueId, ["events"] = events.DeepClone(),
            ["observerHooks"] = hooks.DeepClone(), ["complete"] = !disposed && stopDepth == 0 && errors.Count == 0,
            ["errors"] = errors.DeepClone(), ["allocationTraceComplete"] = trace.Complete,
            ["allocationEvents"] = trace.EventsSince(traceStart) };
        public void Dispose()
        {
            if (disposed) return;
            disposed = true; harmony.UnpatchAll(HarmonyId);
            if (ReferenceEquals(active, this)) active = null;
        }
        private static class Hooks
        {
            internal static void StopBefore(SelectionManagerBase __instance)
            {
                var probe = active;
                if (probe == null) return;
                probe.Safe(() => {
                    if (!ReferenceEquals(__instance, SelectionManager.Instance)) probe.errors.Add("Stop used another selection manager.");
                    probe.stopDepth++;
                    probe.Record("stop-before");
                });
            }
            internal static void EndedAfter(UnitCommand __instance)
            {
                var probe = active;
                if (probe == null || !ReferenceEquals(__instance, probe.command)) return;
                probe.Safe(() => probe.Record("command-ended"));
            }
            internal static void StopAfter(SelectionManagerBase __instance)
            {
                var probe = active;
                if (probe == null) return;
                probe.Safe(() => { probe.Record("stop-after"); probe.stopDepth--; });
            }
        }
    }
}
