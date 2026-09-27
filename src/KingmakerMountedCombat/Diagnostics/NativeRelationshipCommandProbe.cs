using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Harmony12;
using Kingmaker;
using Kingmaker.Controllers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // One disposable observer per measured window. Strong references live only for the
    // window, so a completed command remains observable after leaving its native slot.
    // Every patch is observational and is removed in Dispose, including constructor failure.
    internal sealed class NativeRelationshipCommandProbe : IDisposable
    {
        private const string HarmonyId = "KingmakerMountedCombat.Diagnostics.RelationshipCommand";
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        private static NativeRelationshipCommandProbe active;
        private readonly HarmonyInstance harmony;
        private readonly NativeMountedControlService controls;
        private readonly NativeActorAllocationTrace trace;
        private readonly UnitEntityData rider, mount;
        private readonly string ability;
        private readonly Func<JObject> state;
        private readonly int traceStart;
        private readonly JObject preClick;
        private readonly List<Sample> samples = new List<Sample>();
        private readonly JArray errors = new JArray();
        private RelationshipCommandIdentity identity;
        private int initCount;
        private int traceEnd = -1;
        private bool disposed;
        private JObject completed;
        internal UnitUseAbility Command { get; private set; }
        internal bool Terminal => Command != null && Command.IsFinished && Command.ExecutionProcess?.IsEnded == true;

        private sealed class Sample
        {
            internal string Boundary;
            internal RelationshipCommandIdentity Identity;
            internal JObject Value;
        }

        internal NativeRelationshipCommandProbe(NativeMountedControlService controls,
            NativeActorAllocationTrace trace, UnitEntityData rider, UnitEntityData mount,
            string ability, Func<JObject> state)
        {
            if (active != null) throw new InvalidOperationException("A relationship command window is already active.");
            this.controls = controls; this.trace = trace; this.rider = rider; this.mount = mount;
            this.ability = ability; this.state = state; traceStart = trace.EventCount;
            preClick = new JObject { ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks,
                ["frame"] = Time.frameCount, ["state"] = state() };
            harmony = HarmonyInstance.Create(HarmonyId); active = this;
            trace.BoundaryObserved += OnAllocationBoundary;
            try
            {
                Patch(typeof(NativeMountedControlService).GetMethod("PrepareNativeMountApproach", Flags), null, "InitAfter");
                Patch(typeof(NativeMountedControlService).GetMethod("BindNativeRelationshipProcess", Flags), null, "BindAfter");
                Patch(typeof(NativeMountedControlService).GetMethods(Flags).Single(m => m.Name == "TryDispatch" && m.GetParameters().Length == 4), "DeliverBefore", "DeliverAfter");
                Patch(typeof(UnitCommand).GetMethods(Flags).Single(m => m.MetadataToken == 0x060027A6), "ApproachBefore", null);
                Patch(typeof(UnitCommand).GetMethods(Flags).Single(m => m.MetadataToken == 0x060027A7), "TickBefore", "TickAfter");
                Patch(typeof(AbilityExecutionProcess).GetMethod("Tick", Flags), null, "ProcessTickAfter");
            }
            catch { Dispose(); throw; }
        }

        private void Patch(MethodInfo method, string before, string after)
        {
            if (method == null) throw new MissingMethodException("Exact relationship observation boundary missing.");
            harmony.Patch(method,
                before == null ? null : new HarmonyMethod(typeof(Hooks).GetMethod(before, Flags)) { prioritiy = Priority.First },
                after == null ? null : new HarmonyMethod(typeof(Hooks).GetMethod(after, Flags)) { prioritiy = Priority.Last });
        }

        internal void ClickCompleted(bool admitted)
        {
            if (!admitted || Command == null) errors.Add("Click admitted no exact initialized relationship command.");
            Observe("click-admission", Command);
            if (Command == null || !ReferenceEquals(rider.Commands.GetCommand(UnitCommand.CommandType.Move), Command))
                errors.Add("The clicked command was not installed in the rider Move slot.");
            Observe("move-slot-installation", Command);
        }

        private void OnAllocationBoundary(string boundary, UnitEntityData actor, UnitCommand command)
        {
            if ((boundary == "cost-before" || boundary == "cost-after" || boundary == "actor-cost-before" || boundary == "actor-cost-after") &&
                ReferenceEquals(command, Command))
            {
                Observe(boundary, Command);
                if (boundary == "cost-after") ObserveTerminal();
            }
        }

        private void ObserveTerminal()
        {
            if (!Terminal || traceEnd >= 0 || !samples.Any(s => s.Boundary == "cost-after")) return;
            Observe("terminal", Command);
            traceEnd = trace.EventCount;
        }

        private void Initialized(UnitUseAbility command)
        {
            if (command?.Executor != rider || command.Spell?.Blueprint?.AssetGuid != ability) return;
            initCount++;
            if (Command == null) Command = command;
            else if (!ReferenceEquals(Command, command)) errors.Add("More than one command entered the measured window.");
            Observe("init", command);
        }

        private void Observe(string boundary, UnitUseAbility command, AbilityExecutionContext deliveredContext = null)
        {
            if (disposed || traceEnd >= 0) return;
            try
            {
                if (samples.Count >= 128) { errors.Add("Causal observation bound exceeded."); return; }
                var observed = controls.CaptureRelationshipCommandIdentity(command);
                if (identity == null && observed?.Complete == true) identity = observed;
                if (deliveredContext != null && !ReferenceEquals(command?.ExecutionProcess?.Context, deliveredContext))
                    errors.Add("Deliver used a different execution context.");
                var value = new JObject
                {
                    ["boundary"] = boundary, ["frame"] = Time.frameCount,
                    ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks,
                    ["identity"] = Describe(observed), ["acted"] = command?.IsActed,
                    ["finished"] = command?.IsFinished, ["result"] = command?.Result.ToString(),
                    ["processEnded"] = command?.ExecutionProcess?.IsEnded,
                    ["nativeProcessBinding"] = controls.HasExactRelationshipProcessBinding(command),
                    ["deliveryContext"] = Id(deliveredContext), ["state"] = state()
                };
                samples.Add(new Sample { Boundary = boundary, Identity = observed, Value = value });
            }
            catch (Exception exception) { errors.Add(boundary + ": " + exception); }
        }

        private static int Id(object value) => value == null ? 0 : RuntimeHelpers.GetHashCode(value);
        private static JObject Describe(RelationshipCommandIdentity value) => value == null ? null : new JObject
        {
            ["commandObject"] = Id(value.Command), ["controlIdentity"] = value.Control,
            ["processObject"] = Id(value.Process), ["contextObject"] = Id(value.Context),
            ["casterId"] = value.Caster, ["targetId"] = value.Target,
            ["generationAtInit"] = value.Generation, ["commandType"] = value.Action,
            ["abilityGuid"] = value.Ability
        };

        internal JObject Finish(bool inCombat, bool turnBased, int expectedPartnerPreparations, bool requireApproach)
        {
            if (completed != null) return (JObject)completed.DeepClone();
            if (traceEnd < 0) errors.Add("The exact command/process terminal callback was not observed.");
            var causal = identity != null && identity.Complete && initCount == 1 && Terminal &&
                Command.Result == UnitCommand.ResultType.Success &&
                ((JArray)preClick["state"]["selectedIds"]).Count == 1 &&
                (string)preClick["state"]["selectedIds"][0] == rider.UniqueId &&
                (long)preClick["state"]["generation"] == identity.Generation;
            foreach (var name in new[] { "init", "click-admission", "move-slot-installation", "acted", "cost-before", "cost-after", "process-binding", "deliver", "relationship-transition", "terminal" })
                causal &= samples.Count(s => s.Boundary == name) == 1;
            if (requireApproach) causal &= samples.Count(s => s.Boundary == "approach-start") == 1;
            foreach (var sample in samples)
            {
                var early = sample.Boundary == "init" || sample.Boundary == "click-admission" ||
                    sample.Boundary == "move-slot-installation" || sample.Boundary == "approach-start";
                causal &= identity != null && identity.Matches(sample.Identity, early);
                if (sample.Boundary == "acted" || sample.Boundary == "cost-before" || sample.Boundary == "cost-after")
                    causal &= (bool?)sample.Value["acted"] == true;
                if (sample.Boundary == "process-binding") causal &= (bool)sample.Value["nativeProcessBinding"];
                if (sample.Boundary == "deliver") causal &= (int)sample.Value["deliveryContext"] == Id(identity?.Context);
            }
            var acted = samples.FindIndex(s => s.Boundary == "acted");
            var cost = samples.FindIndex(s => s.Boundary == "cost-before");
            var delivery = samples.FindIndex(s => s.Boundary == "deliver");
            causal &= acted >= 0 && cost > acted && delivery > cost;
            var resource = EvaluateResources(inCombat, turnBased, expectedPartnerPreparations);
            var ledgerDelta = new JObject();
            foreach (var item in ((JObject)preClick["state"]["ledger"]).Properties())
                ledgerDelta[item.Name] = (long)samples.Last().Value["state"]["ledger"][item.Name] - (long)item.Value;
            completed = new JObject
            {
                ["preClick"] = preClick.DeepClone(), ["ledgerDelta"] = ledgerDelta,
                ["mountId"] = mount.UniqueId, ["identity"] = Describe(identity), ["identityComplete"] = identity?.Complete == true,
                ["initCount"] = initCount, ["sameCommandAtEveryBoundary"] = causal,
                ["exactActedObserved"] = acted >= 0 && (bool?)samples[acted].Value["acted"] == true,
                ["nativeTerminal"] = Terminal, ["nativeResult"] = Command?.Result.ToString(),
                ["resourceWindow"] = resource,
                ["pass"] = causal && (bool)resource["pass"] && errors.Count == 0 && trace.Complete,
                ["samples"] = new JArray(samples.Select(s => s.Value.DeepClone())),
                ["errors"] = errors.DeepClone(), ["traceComplete"] = trace.Complete
            };
            return (JObject)completed.DeepClone();
        }

        private JObject EvaluateResources(bool inCombat, bool turnBased, int expectedPartnerPreparations)
        {
            var events = new JArray(trace.EventsSince(traceStart).Take(Math.Max(0, traceEnd - traceStart)));
            var pairEvents = events.OfType<JObject>().Where(e => (string)e["state"]?["actor"] == rider.UniqueId ||
                (string)e["state"]?["actor"] == mount.UniqueId).ToArray();
            var before = pairEvents.Where(e => (string)e["boundary"] == "cost-before").ToArray();
            var after = pairEvents.Where(e => (string)e["boundary"] == "cost-after").ToArray();
            var nestedBefore = pairEvents.Where(e => (string)e["boundary"] == "actor-cost-before").ToArray();
            var nestedAfter = pairEvents.Where(e => (string)e["boundary"] == "actor-cost-after").ToArray();
            Func<JObject, bool> exact = e => (int)e["command"] == Id(Command) &&
                (string)e["commandActor"] == rider.UniqueId && (string)e["state"]["actor"] == rider.UniqueId &&
                (string)e["actionType"] == "Move" &&
                (bool?)e["acted"] == true;
            var oneSequence = before.Length == 1 && after.Length == 1 && before.All(exact) && after.All(exact) &&
                nestedBefore.Length == (inCombat && turnBased ? 1 : 0) &&
                nestedAfter.Length == nestedBefore.Length && nestedBefore.All(exact) && nestedAfter.All(exact);
            var exactCost = false;
            if (oneSequence)
            {
                var b = before[0]; var a = after[0];
                exactCost = (bool)b["state"]["inCombat"] == inCombat && (bool)a["state"]["inCombat"] == inCombat &&
                    NativeResourceWindowPolicy.ExactMoveCallback((bool)b["acted"], inCombat, turnBased,
                        (double)b["timeSinceStart"], (double)b["state"]["move"], (double)a["state"]["move"],
                        (double)b["state"]["standard"], (double)a["state"]["standard"],
                        (double)b["state"]["swift"], (double)a["state"]["swift"]);
            }
            var riderPrepares = pairEvents.Count(e => (string)e["boundary"] == "prepare-before" && (string)e["state"]["actor"] == rider.UniqueId);
            var mountPrepares = pairEvents.Count(e => (string)e["boundary"] == "prepare-before" && (string)e["state"]["actor"] == mount.UniqueId);
            var clears = pairEvents.Where(e => (string)e["boundary"] == "clear-before").ToArray();
            var clearAfter = pairEvents.Where(e => (string)e["boundary"] == "clear-after").ToArray();
            var prepareAfter = pairEvents.Where(e => (string)e["boundary"] == "prepare-after").ToArray();
            var lawfulPreparation = riderPrepares == 0 && mountPrepares == expectedPartnerPreparations &&
                clears.Length == expectedPartnerPreparations && clearAfter.Length == expectedPartnerPreparations &&
                prepareAfter.Length == expectedPartnerPreparations && clears.Concat(clearAfter).Concat(prepareAfter).All(e =>
                    (string)e["state"]["actor"] == mount.UniqueId && (int)e["preparingTurn"] != 0) &&
                !pairEvents.Any(e => ((string)e["boundary"]).StartsWith("combat-clear", StringComparison.Ordinal));
            // Absence of another native callback establishes the negative cost claim.
            // Endpoint comparisons additionally catch a clear/refund between callbacks.
            var endpoints = samples.Any(s => s.Boundary == "init") && samples.Last().Boundary == "terminal";
            if (endpoints)
            {
                var first = preClick;
                var last = samples.Last().Value;
                var elapsed = ((long)last["gameTicks"] - (long)first["gameTicks"]) / (double)TimeSpan.TicksPerSecond;
                foreach (var actor in new[] { "rider", "mount" })
                    foreach (var field in new[] { "standard", "move", "swift" })
                    {
                        var was = (double)first["state"][actor][field];
                        var age = elapsed;
                        if (actor == "rider" && field == "move" && after.Length == 1)
                        {
                            was = (double)after[0]["state"][field];
                            age = ((long)last["gameTicks"] - (long)after[0]["gameTicks"]) / (double)TimeSpan.TicksPerSecond;
                        }
                        if (actor == "mount" && expectedPartnerPreparations == 1 && clearAfter.Length == 1)
                        {
                            was = (double)clearAfter[0]["state"][field];
                            age = ((long)last["gameTicks"] - (long)clearAfter[0]["gameTicks"]) / (double)TimeSpan.TicksPerSecond;
                            endpoints &= Math.Abs(was) <= 0.0001;
                        }
                        endpoints &= NativeResourceWindowPolicy.EndpointConserved(was,
                            (double)last["state"][actor][field], age, turnBased || !inCombat,
                            turnBased || !inCombat ? 0.0001 : 0.05);
                    }
            }
            if (endpoints && before.Length == 1)
            {
                var age = ((long)before[0]["gameTicks"] - (long)preClick["gameTicks"]) / (double)TimeSpan.TicksPerSecond;
                endpoints &= NativeResourceWindowPolicy.EndpointConserved((double)preClick["state"]["rider"]["move"],
                    (double)before[0]["state"]["move"], age, turnBased || !inCombat,
                    turnBased || !inCombat ? 0.0001 : 0.05);
            }
            return new JObject
            {
                ["pass"] = oneSequence && exactCost && lawfulPreparation && endpoints && trace.Complete,
                ["oneExactRiderMoveSequence"] = oneSequence, ["exactNativeCost"] = exactCost,
                ["noOtherActorOrActionCostCallbacks"] = oneSequence,
                ["riderPrepareDelta"] = riderPrepares, ["mountPrepareDelta"] = mountPrepares,
                ["expectedMountPrepareDelta"] = expectedPartnerPreparations,
                ["clearCount"] = clears.Length, ["lawfulPreparationOnly"] = lawfulPreparation,
                ["noRefundOrExtraDebtAtEndpoints"] = endpoints, ["inCombat"] = inCombat,
                ["turnBased"] = turnBased, ["events"] = events
            };
        }

        internal JObject Capture() => new JObject
        {
            ["identity"] = Describe(identity), ["initCount"] = initCount,
            ["terminal"] = Terminal, ["errors"] = errors.DeepClone(),
            ["samples"] = new JArray(samples.Select(s => s.Value.DeepClone()))
        };

        public void Dispose()
        {
            if (disposed) return;
            disposed = true; trace.BoundaryObserved -= OnAllocationBoundary;
            harmony.UnpatchAll(HarmonyId); if (ReferenceEquals(active, this)) active = null;
        }

        private static class Hooks
        {
            internal static void InitAfter(UnitUseAbility command) { active?.Initialized(command); }
            internal static void BindAfter(UnitUseAbility command)
            { if (ReferenceEquals(active?.Command, command)) active.Observe("process-binding", command); }
            internal static void ApproachBefore(UnitCommand __instance)
            {
                if (active != null && ReferenceEquals(active.Command, __instance) &&
                    !active.samples.Any(s => s.Boundary == "approach-start"))
                    active.Observe("approach-start", active.Command);
            }
            internal static void TickBefore(UnitCommand __instance, out bool __state) { __state = __instance.IsActed; }
            internal static void TickAfter(UnitCommand __instance, bool __state)
            {
                if (active != null && ReferenceEquals(active.Command, __instance) && !__state && __instance.IsActed)
                    active.Observe("acted", active.Command);
            }
            internal static void ProcessTickAfter(AbilityExecutionProcess __instance)
            {
                if (active != null && ReferenceEquals(active.Command?.ExecutionProcess, __instance)) active.ObserveTerminal();
            }
            internal static void DeliverBefore(NativeMountedControlKind kind, UnitEntityData caster,
                UnitEntityData target, AbilityExecutionContext context)
            {
                if (active != null && caster == active.rider &&
                    (kind == NativeMountedControlKind.MountCompanion || kind == NativeMountedControlKind.Dismount))
                    active.Observe("deliver", active.Command, context);
            }
            internal static void DeliverAfter(NativeMountedControlKind kind, UnitEntityData caster,
                UnitEntityData target, AbilityExecutionContext context)
            {
                if (active != null && caster == active.rider &&
                    (kind == NativeMountedControlKind.MountCompanion || kind == NativeMountedControlKind.Dismount))
                    active.Observe("relationship-transition", active.Command, context);
            }
        }
    }
}
