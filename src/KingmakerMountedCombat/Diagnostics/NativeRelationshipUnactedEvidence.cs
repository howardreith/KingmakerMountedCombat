using System;
using System.Linq;
using Kingmaker.UnitLogic.Commands.Base;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class NativeRelationshipCommandProbe
    {
        private readonly bool unactedFailure;
        private RelationshipCommandIdentity unactedIdentity;
        internal bool UnactedTerminal => unactedFailure && traceEnd >= 0 && Command?.IsFinished == true &&
            !Command.IsActed && Command.ExecutionProcess == null &&
            (Command.Result == UnitCommand.ResultType.Fail || Command.Result == UnitCommand.ResultType.Interrupt);
        private void UnactedEnded()
        {
            if (!unactedFailure || traceEnd >= 0) return;
            Observe("unacted-terminal", Command);
            traceEnd = trace.EventCount;
        }
        internal JObject FinishUnacted()
        {
            if (!unactedFailure) throw new InvalidOperationException("A positive window cannot use the unacted obstruction contract.");
            if (completed != null) return (JObject)completed.DeepClone();
            if (traceEnd < 0)
            {
                errors.Add("Exact unacted native terminal callback was not observed.");
                Observe("deadline", Command); traceEnd = trace.EventCount;
            }
            var causal = UnactedTerminal && unactedIdentity?.UnactedRequestComplete == true && initCount == 1 &&
                ((JArray)preClick["state"]["selectedIds"]).Count == 1 && (string)preClick["state"]["selectedIds"][0] == rider.UniqueId;
            foreach (var name in new[] { "init", "click-admission", "move-slot-installation", "approach-start", "unacted-terminal" })
            {
                var count = samples.Count(s => s.Boundary == name); causal &= count == 1;
                if (count != 1) errors.Add("Instrumentation boundary " + name + " expected once; observed=" + count + ".");
            }
            var expected = new[] { "init", "click-admission", "move-slot-installation", "approach-start", "unacted-terminal" };
            causal &= samples.Select(s => s.Boundary).SequenceEqual(expected) && samples.All(s => unactedIdentity != null &&
                unactedIdentity.MatchesUnacted(s.Identity) && (bool?)s.Value["acted"] == false &&
                (string)s.Value["state"]["relationshipState"] == "Unmounted" && (long)s.Value["state"]["generation"] == unactedIdentity.Generation &&
                JToken.DeepEquals(s.Value["state"]["ledger"], preClick["state"]["ledger"]));
            var last = samples.Last().Value;
            var ledger = new JObject();
            foreach (var item in ((JObject)preClick["state"]["ledger"]).Properties())
                ledger[item.Name] = (long)last["state"]["ledger"][item.Name] - (long)item.Value;
            causal &= ledger.Properties().All(p => (long)p.Value == 0) && (string)last["state"]["relationshipState"] == "Unmounted" &&
                (long)preClick["state"]["generation"] == unactedIdentity?.Generation && (long)last["state"]["generation"] == unactedIdentity?.Generation;
            var events = new JArray(trace.EventsSince(traceStart).Take(Math.Max(0, traceEnd - traceStart)));
            var pair = events.OfType<JObject>().Where(e => (string)e["state"]?["actor"] == rider.UniqueId || (string)e["state"]?["actor"] == mount.UniqueId);
            var noCost = !pair.Any(e => {
                var name = (string)e["boundary"];
                return name.StartsWith("cost-", StringComparison.Ordinal) || name.StartsWith("actor-cost-", StringComparison.Ordinal) ||
                    name.StartsWith("prepare-", StringComparison.Ordinal) || name.StartsWith("clear-", StringComparison.Ordinal) || name.StartsWith("combat-clear", StringComparison.Ordinal);
            });
            var elapsed = ((long)last["gameTicks"] - (long)preClick["gameTicks"]) / (double)TimeSpan.TicksPerSecond;
            var endpoints = true;
            foreach (var actor in new[] { "rider", "mount" })
                foreach (var field in new[] { "standard", "move", "swift" })
                    endpoints &= NativeResourceWindowPolicy.EndpointConserved((double)preClick["state"][actor][field],
                        (double)last["state"][actor][field], elapsed, false, 0.05);
            var reactions = EvaluateReactionResources(events, 0);
            completed = new JObject { ["contract"] = "unacted-native-obstruction-no-cost-or-transition",
                ["observerHooks"] = installedHooks.DeepClone(), ["preClick"] = preClick.DeepClone(),
                ["identity"] = Describe(unactedIdentity), ["mountId"] = mount.UniqueId, ["initCount"] = initCount,
                ["sameCommandAtEveryBoundary"] = causal, ["nativeTerminal"] = UnactedTerminal,
                ["nativeResult"] = Command?.Result.ToString(), ["ledgerDelta"] = ledger,
                ["resourceWindow"] = new JObject { ["events"] = events, ["reactionResources"] = reactions,
                    ["noCostOrPreparationCallbacks"] = noCost, ["endpointsConserved"] = endpoints,
                    ["pass"] = noCost && endpoints && (bool)reactions["pass"] && trace.Complete },
                ["samples"] = new JArray(samples.Select(s => s.Value.DeepClone())), ["errors"] = errors.DeepClone(),
                ["traceComplete"] = trace.Complete, ["pass"] = causal && noCost && endpoints && (bool)reactions["pass"] && errors.Count == 0 && trace.Complete };
            return (JObject)completed.DeepClone();
        }
        private static partial class Hooks
        {
            internal static void UnactedEndedAfter(UnitCommand __instance)
            { if (active != null && ReferenceEquals(active.Command, __instance)) active.UnactedEnded(); }
        }
    }
}
