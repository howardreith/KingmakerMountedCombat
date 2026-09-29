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
    // Read-only proof of native replacement admission surrounding this exact Mount's OnEnded.
    // A caller supplies the input. The observer never interrupts a command or changes selection.
    internal sealed class NativeMountReplacementInputProbe : IDisposable
    {
        private const string HarmonyId = "KingmakerMountedCombat.Diagnostics.MountReplacementInput";
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        private static NativeMountReplacementInputProbe active;
        private readonly HarmonyInstance harmony;
        private readonly UnitEntityData rider;
        private readonly UnitUseAbility command;
        private readonly UnitMoveTo replacement;
        private readonly Func<JObject> state;
        private readonly NativeActorAllocationTrace trace;
        private readonly int traceStart;
        private readonly JArray events = new JArray(), hooks = new JArray(), errors = new JArray();
        private int runDepth;
        private bool disposed;

        internal NativeMountReplacementInputProbe(UnitEntityData rider, UnitUseAbility command, UnitMoveTo replacement, Func<JObject> state, NativeActorAllocationTrace trace)
        {
            if (active != null) throw new InvalidOperationException("An exact Mount Replacement input observer is already active.");
            if (rider == null || command == null || state == null || trace == null || command.Executor != rider ||
                command.IsStarted || command.IsActed || command.IsFinished || command.ExecutionProcess != null ||
                !ReferenceEquals(rider.Commands.GetCommand(UnitCommand.CommandType.Move), command))
                throw new InvalidOperationException("Replacement input observation requires the exact pending unacted Mount in the rider Move slot.");
            if (command.Spell?.Blueprint?.AssetGuid != "f053faad986631688defa003cd7bda0e")
                throw new InvalidOperationException("Replacement input observation requires the native Mount ability.");
            if (replacement == null || replacement.Executor != null || !replacement.CreatedByPlayer || replacement.IsStarted || replacement.IsActed || replacement.IsFinished)
                throw new InvalidOperationException("Replacement requires one new native player ground command.");
            this.replacement = replacement;
            this.rider = rider; this.command = command; this.state = state; this.trace = trace; traceStart = trace.EventCount;
            if (typeof(UnitCommand).Assembly.ManifestModule.ModuleVersionId != new Guid("07fa1e4d-8618-41b3-9b8d-faa17d3b26f7"))
                throw new InvalidOperationException("Replacement input observer requires the pinned native assembly.");
            harmony = HarmonyInstance.Create(HarmonyId); active = this;
            try
            {
                Patch(typeof(UnitCommands), 0x060026B2, "RunBefore", "RunAfter");
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
            if (events.Count >= 16) { if (errors.Count == 0) errors.Add("Replacement input callback bound exceeded."); return; }
            events.Add(new JObject { ["sequence"] = events.Count + 1, ["boundary"] = boundary,
                ["frame"] = Time.frameCount, ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks,
                ["runDepth"] = runDepth, ["commandObject"] = Id(command), ["casterId"] = rider.UniqueId,
                ["moveSlotObject"] = Id(rider.Commands.GetCommand(UnitCommand.CommandType.Move)),
                ["processObject"] = Id(command.ExecutionProcess), ["contextObject"] = Id(command.ExecutionProcess?.Context),
                ["started"] = command.IsStarted, ["acted"] = command.IsActed, ["finished"] = command.IsFinished,
                ["result"] = command.Result.ToString(), ["replacement"] = CaptureReplacement(), ["state"] = state() });
        }
        private JObject CaptureReplacement() => new JObject { ["commandObject"] = Id(replacement),
            ["class"] = replacement.GetType().FullName, ["executorId"] = replacement.Executor?.UniqueId,
            ["type"] = replacement.Type.ToString(), ["createdByPlayer"] = replacement.CreatedByPlayer,
            ["ignoreCooldown"] = replacement.IsIgnoreCooldown, ["started"] = replacement.IsStarted,
            ["acted"] = replacement.IsActed, ["finished"] = replacement.IsFinished,
            ["result"] = replacement.Result.ToString(),
            ["destination"] = new JObject { ["x"] = replacement.Target.x, ["y"] = replacement.Target.y, ["z"] = replacement.Target.z } };
        private void Safe(Action observe)
        {
            if (disposed) return;
            try { observe(); }
            catch (Exception exception) { if (errors.Count == 0) errors.Add(exception.ToString()); }
        }
        internal JObject Capture() => new JObject { ["contract"] = "native-replacement-surrounds-exact-unacted-mount-terminal",
            ["commandObject"] = Id(command), ["replacementObject"] = Id(replacement), ["casterId"] = rider.UniqueId, ["events"] = events.DeepClone(),
            ["observerHooks"] = hooks.DeepClone(), ["complete"] = !disposed && runDepth == 0 && errors.Count == 0,
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

            internal static void RunBefore(UnitCommands __instance, UnitCommand cmd)
            {
                var probe = active;
                if (probe == null || !ReferenceEquals(__instance, probe.rider.Commands) || !ReferenceEquals(cmd, probe.replacement)) return;
                probe.Safe(() => { probe.runDepth++; probe.Record("replacement-before"); });
            }
            internal static void EndedAfter(UnitCommand __instance)
            {
                var probe = active;
                if (probe == null || !ReferenceEquals(__instance, probe.command)) return;
                probe.Safe(() => probe.Record("command-ended"));
            }
            internal static void RunAfter(UnitCommands __instance, UnitCommand cmd)
            {
                var probe = active;
                if (probe == null || !ReferenceEquals(__instance, probe.rider.Commands) || !ReferenceEquals(cmd, probe.replacement)) return;
                probe.Safe(() => { probe.Record("replacement-after"); probe.runDepth--; });
            }
        }
    }
}
