using System;
using System.Linq;
using Kingmaker.UnitLogic.Commands.Base;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class NativeRelationshipCommandProbe
    {
        private const string RiderIncapacitatedContract = "unacted-native-rider-incapacitated-no-cost-or-transition";
        private const string MountIncapacitatedContract = "unacted-native-mount-incapacitated-no-cost-or-transition";
        private const string IncapacityClearContract = "native-incapacity-combat-removal-clears-initiative-only";
        private const string LifeAssemblyMvid = "07fa1e4d-8618-41b3-9b8d-faa17d3b26f7";
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
        internal JObject FinishUnacted(string contract = "unacted-native-obstruction-no-cost-or-transition",
            JObject nativeLifeEvents = null)
        {
            if (!unactedFailure) throw new InvalidOperationException("A positive window cannot use an unacted contract.");
            if (contract != "unacted-native-obstruction-no-cost-or-transition" && contract != "unacted-native-stop-no-cost-or-transition" && contract != "unacted-native-replacement-no-cost-or-transition" && contract != "unacted-native-ownership-loss-no-cost-or-transition" && contract != "unacted-native-size-form-change-no-cost-or-transition" && contract != "unacted-native-lost-direct-control-no-cost-or-transition" && contract != RiderIncapacitatedContract && contract != MountIncapacitatedContract) throw new InvalidOperationException("Undeclared unacted native contract.");
            var incapacityContract = contract == RiderIncapacitatedContract || contract == MountIncapacitatedContract;
            if (!incapacityContract && nativeLifeEvents != null)
                throw new InvalidOperationException("Native life evidence is allowed only for a declared incapacity contract.");
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
            var pair = events.OfType<JObject>().Where(e => (string)e["state"]?["actor"] == rider.UniqueId || (string)e["state"]?["actor"] == mount.UniqueId).ToArray();
            var incapacityClear = incapacityContract
                ? EvaluateIncapacityClearDisposition(events, contract, nativeLifeEvents)
                : null;
            var incapacityClearPass = !incapacityContract || (bool)incapacityClear["pass"];
            var noNativeCostOrPreparation = !pair.Any(e => {
                var name = (string)e["boundary"];
                return name.StartsWith("cost-", StringComparison.Ordinal) ||
                    name.StartsWith("actor-cost-", StringComparison.Ordinal) ||
                    name.StartsWith("prepare-", StringComparison.Ordinal);
            });
            var hasClear = pair.Any(e => {
                var name = (string)e["boundary"];
                return name.StartsWith("clear-", StringComparison.Ordinal) ||
                    name.StartsWith("combat-clear", StringComparison.Ordinal);
            });
            var noUndeclaredResourceCallbacks = noNativeCostOrPreparation &&
                (!hasClear || incapacityContract && incapacityClearPass);
            var elapsed = ((long)last["gameTicks"] - (long)preClick["gameTicks"]) / (double)TimeSpan.TicksPerSecond;
            var endpoints = true;
            foreach (var actor in new[] { "rider", "mount" })
                foreach (var field in new[] { "standard", "move", "swift" })
                    endpoints &= NativeResourceWindowPolicy.EndpointConserved((double)preClick["state"][actor][field],
                        (double)last["state"][actor][field], elapsed, false, 0.05);
            var incapacityActor = incapacityContract && incapacityClearPass ? (string)incapacityClear["subjectId"] : null;
            var reactions = EvaluateReactionResources(events, 0, incapacityActor);
            completed = new JObject {
                ["contract"] = contract,
                ["observerHooks"] = installedHooks.DeepClone(),
                ["preClick"] = preClick.DeepClone(),
                ["identity"] = Describe(unactedIdentity),
                ["mountId"] = mount.UniqueId,
                ["initCount"] = initCount,
                ["sameCommandAtEveryBoundary"] = causal,
                ["nativeTerminal"] = UnactedTerminal,
                ["nativeResult"] = Command?.Result.ToString(),
                ["ledgerDelta"] = ledger,
                ["resourceWindow"] = new JObject {
                    ["events"] = events,
                    ["reactionResources"] = reactions,
                    ["nativeIncapacityClearDisposition"] = incapacityClear == null ? (JToken)JValue.CreateNull() : incapacityClear,
                    ["noNativeCostOrPreparationCallbacks"] = noNativeCostOrPreparation,
                    ["noCostOrPreparationCallbacks"] = noUndeclaredResourceCallbacks,
                    ["endpointsConserved"] = endpoints,
                    ["pass"] = noUndeclaredResourceCallbacks && endpoints && (bool)reactions["pass"] &&
                        incapacityClearPass && trace.Complete
                },
                ["samples"] = new JArray(samples.Select(s => s.Value.DeepClone())),
                ["errors"] = errors.DeepClone(),
                ["traceComplete"] = trace.Complete,
                ["pass"] = causal && noUndeclaredResourceCallbacks && endpoints && (bool)reactions["pass"] &&
                    incapacityClearPass && errors.Count == 0 && trace.Complete
            };
            CompletePredictionEvidence(completed);
            return (JObject)completed.DeepClone();
        }

        private JObject EvaluateIncapacityClearDisposition(JArray events, string contract, JObject nativeLifeEvents)
        {
            var failures = new JArray();
            var expectedBoundaries = new[] { "combat-clear-before", "clear-before", "clear-after", "combat-clear-after" };
            var subjectId = contract == RiderIncapacitatedContract ? rider.UniqueId :
                contract == MountIncapacitatedContract ? mount.UniqueId : null;
            var sequences = new JArray();
            var actionUnchanged = false;
            var actionBridged = false;
            int? actionBridgeBeforeSequence = null;
            int? actionBridgeAfterSequence = null;
            var reactionUnchanged = false;
            var initiativeCooldownCleared = false;
            var initiativeOrderCleared = false;
            var preparedCleared = false;
            JObject life = null;
            try
            {
                life = ExactNativeIncapacityLifeEvent(nativeLifeEvents, subjectId);
                if (life == null)
                    throw new InvalidOperationException("Exact native incapacity life event is missing or differs.");
                var clearEvents = events.OfType<JObject>().Where(item => {
                    var actor = (string)item["state"]?["actor"];
                    var boundary = (string)item["boundary"];
                    return (actor == rider.UniqueId || actor == mount.UniqueId) &&
                        (boundary.StartsWith("clear-", StringComparison.Ordinal) ||
                         boundary.StartsWith("combat-clear", StringComparison.Ordinal));
                }).ToArray();
                if (clearEvents.Length != expectedBoundaries.Length)
                    throw new InvalidOperationException("Native incapacity clear boundary count differs.");
                for (var index = 0; index < clearEvents.Length; index++)
                {
                    var item = clearEvents[index];
                    if ((string)item["boundary"] != expectedBoundaries[index] ||
                        (string)item["state"]["actor"] != subjectId ||
                        index > 0 && (int)item["sequence"] != (int)clearEvents[index - 1]["sequence"] + 1)
                        throw new InvalidOperationException("Native incapacity clear actor, order, or sequence differs.");
                    if ((int)item["frame"] != (int)life["frame"] ||
                        (long)item["gameTicks"] != (long)life["gameTicks"] ||
                        (bool?)item["state"]["inCombat"] != false)
                        throw new InvalidOperationException("Native incapacity clear is not bound to the exact life-state boundary.");
                    sequences.Add((int)item["sequence"]);
                }
                var states = clearEvents.Select(item => item["state"]).ToArray();
                var actionFields = new[] { "standard", "move", "swift" };
                var quartetActionUnchanged = actionFields.All(field =>
                    states.Skip(1).All(state => SameResource(states[0][field], state[field])));
                if (!quartetActionUnchanged)
                    throw new InvalidOperationException("Native incapacity clear changed Standard, Move, or Swift.");
                var subjectKind = contract == RiderIncapacitatedContract ? "rider" : "mount";
                var firstClearSequence = (int)clearEvents[0]["sequence"];
                var preceding = events.OfType<JObject>()
                    .Where(item => (string)item["state"]?["actor"] == subjectId &&
                        (int)item["sequence"] < firstClearSequence)
                    .Select(item => new {
                        Sequence = (int)item["sequence"], Rank = 1,
                        GameTicks = (long)item["gameTicks"], State = item["state"]
                    })
                    .Concat(samples.Where(sample => (int)sample.Value["allocationSequence"] < firstClearSequence)
                        .Select(sample => new {
                            Sequence = (int)sample.Value["allocationSequence"], Rank = 2,
                            GameTicks = (long)sample.Value["gameTicks"], State = sample.Value["state"][subjectKind]
                        }))
                    .Concat(new[] { new {
                        Sequence = (int)preClick["allocationSequence"], Rank = 0,
                        GameTicks = (long)preClick["gameTicks"], State = preClick["state"][subjectKind]
                    } })
                    .OrderBy(item => item.Sequence).ThenBy(item => item.Rank).LastOrDefault();
                var terminal = samples.LastOrDefault()?.Value;
                if (preceding != null) actionBridgeBeforeSequence = preceding.Sequence;
                if (terminal != null) actionBridgeAfterSequence = (int)terminal["allocationSequence"];
                actionBridged = preceding != null && terminal != null &&
                    (int)terminal["allocationSequence"] == (int)clearEvents[3]["sequence"] &&
                    actionFields.All(field => NativeResourceWindowPolicy.IncapacityActionBridge(
                        (double)preceding.State[field], (double)states[0][field],
                        (double)states[3][field], (double)terminal["state"][subjectKind][field],
                        preceding.GameTicks, (long)clearEvents[0]["gameTicks"],
                        (long)terminal["gameTicks"], 0.05));
                actionUnchanged = quartetActionUnchanged && actionBridged;
                if (!actionBridged)
                    throw new InvalidOperationException(
                        "Native incapacity clear action resources are not reconciled with surrounding command observations.");
                reactionUnchanged = states.Skip(1).All(state =>
                    (int)state["reactions"] == (int)states[0]["reactions"] &&
                    (int)state["reactionsPerRound"] == (int)states[0]["reactionsPerRound"] &&
                    SameResource(state["reactionCooldown"], states[0]["reactionCooldown"]));
                if (!reactionUnchanged)
                    throw new InvalidOperationException("Native incapacity clear changed reaction allowance or cooldown.");
                var beforeCooldown = (double)states[0]["initiativeCooldown"];
                var beforeOrder = (int)states[0]["initiativeOrder"];
                initiativeCooldownCleared = beforeCooldown > 0 &&
                    SameResource(states[1]["initiativeCooldown"], states[0]["initiativeCooldown"]) &&
                    SameResource(states[2]["initiativeCooldown"], new JValue(0d)) &&
                    SameResource(states[3]["initiativeCooldown"], new JValue(0d));
                if (!initiativeCooldownCleared)
                    throw new InvalidOperationException("Native incapacity clear did not perform the exact initiative-cooldown clear.");
                initiativeOrderCleared = beforeOrder != 0 &&
                    (int)states[1]["initiativeOrder"] == beforeOrder &&
                    (int)states[2]["initiativeOrder"] == beforeOrder &&
                    (int)states[3]["initiativeOrder"] == 0;
                if (!initiativeOrderCleared)
                    throw new InvalidOperationException("Native incapacity combat clear did not perform the exact initiative-order clear.");
                preparedCleared = (bool)states[1]["prepared"] == (bool)states[0]["prepared"] &&
                    (bool)states[2]["prepared"] == (bool)states[0]["prepared"] &&
                    (bool)states[3]["prepared"] == false;
                if (!preparedCleared)
                    throw new InvalidOperationException("Native incapacity combat clear changed preparation outside its terminal removal boundary.");
                if ((bool?)life["commandRunning"] != false ||
                    !SameResource(life["standard"], states[0]["standard"]) ||
                    !SameResource(life["move"], states[0]["move"]))
                    throw new InvalidOperationException("Native incapacity life event is not resource-identical to combat removal entry.");
            }
            catch (Exception exception) { failures.Add(exception.Message); }
            return new JObject {
                ["contract"] = IncapacityClearContract,
                ["declaredCommandContract"] = contract,
                ["subjectId"] = subjectId,
                ["lifeEventFrame"] = life == null ? (JToken)JValue.CreateNull() : life["frame"].DeepClone(),
                ["lifeEventGameTicks"] = life == null ? (JToken)JValue.CreateNull() : life["gameTicks"].DeepClone(),
                ["boundaries"] = new JArray(expectedBoundaries),
                ["sequences"] = sequences,
                ["actionResourcesUnchanged"] = actionUnchanged,
                ["actionResourcesBridged"] = actionBridged,
                ["actionBridgeBeforeSequence"] = actionBridgeBeforeSequence.HasValue
                    ? new JValue(actionBridgeBeforeSequence.Value) : JValue.CreateNull(),
                ["actionBridgeAfterSequence"] = actionBridgeAfterSequence.HasValue
                    ? new JValue(actionBridgeAfterSequence.Value) : JValue.CreateNull(),
                ["reactionResourcesUnchanged"] = reactionUnchanged,
                ["initiativeCooldownCleared"] = initiativeCooldownCleared,
                ["initiativeOrderCleared"] = initiativeOrderCleared,
                ["preparedCleared"] = preparedCleared,
                ["pass"] = failures.Count == 0,
                ["errors"] = failures
            };
        }

        private static JObject ExactNativeIncapacityLifeEvent(JObject trace, string subjectId)
        {
            var events = (trace?["events"] as JArray)?.OfType<JObject>().ToArray() ?? new JObject[0];
            if (events.Length != 1) return null;
            var item = events[0];
            var source = (item["nativeSource"] as JArray)?.OfType<JObject>().ToArray() ?? new JObject[0];
            return (string)item["kind"] == "native-life-state" && (string)item["actor"] == subjectId &&
                (string)item["detail"] == "Conscious" && (string)item["lifeState"] == "Unconscious" &&
                source.Length == 2 && (string)source[0]["type"] == "Kingmaker.Controllers.Units.UnitLifeController" &&
                (string)source[0]["method"] == "SetLifeState" && (string)source[0]["token"] == "06009164" &&
                (string)source[0]["assemblyMvid"] == LifeAssemblyMvid &&
                (string)source[1]["type"] == "Kingmaker.Controllers.Units.UnitLifeController" &&
                (string)source[1]["method"] == "TickOnUnit" && (string)source[1]["token"] == "06009162" &&
                (string)source[1]["assemblyMvid"] == LifeAssemblyMvid ? item : null;
        }

        private static bool SameResource(JToken left, JToken right)
        {
            if (left == null || right == null) return false;
            var first = (double)left;
            var second = (double)right;
            return !double.IsNaN(first) && !double.IsNaN(second) &&
                !double.IsInfinity(first) && !double.IsInfinity(second) &&
                Math.Abs(first - second) <= 0.0001;
        }

        private static partial class Hooks
        {
            internal static void UnactedEndedAfter(UnitCommand __instance)
            { if (active != null && ReferenceEquals(active.Command, __instance)) active.UnactedEnded(); }
        }
    }
}
