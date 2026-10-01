using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker;
using Kingmaker.Controllers.Combat;
using Kingmaker.UI.Selection;
using KingmakerMountedCombat.Domain;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        private const string Chunk6aRepeatedDismountRow = "CM05-repeated-input";
        private JObject chunk6aRepeatedDismountEvidence;
        private bool chunk6aRepeatedDismountCancellationComplete;

        private void BeginChunk6aRepeatedDismountObservation()
        {
            if (!IsUnmountedAttackControls) return;
            if (allocationTrace != null)
                throw new InvalidOperationException("Repeated Dismount requires one fresh allocation observer.");
            allocationTrace = new NativeActorAllocationTrace(rider, horse, combat)
            {
                ObserveReactionResources = true
            };
            allocationTrace.BeginEncounter(request.Scenario);
            chunk6aRepeatedDismountEvidence = new JObject
            {
                ["contract"] = "cancel-once-one-native-dismount-then-repeat-refused",
                ["scenario"] = request.Scenario,
                ["riderId"] = rider.UniqueId,
                ["mountId"] = horse.UniqueId,
                ["abilityGuid"] = nativeControls.DismountAbility.AssetGuid
            };
            observations["chunk6aRepeatedDismount"] = chunk6aRepeatedDismountEvidence;
        }

        private JObject CaptureChunk6aRepeatedDismountControls()
        {
            var controls = nativeControls.CaptureSnapshot();
            return new JObject
            {
                ["targetSelectionStart"] = controls.TargetSelectionStartCount,
                ["targetSelectionEnd"] = controls.TargetSelectionEndCount,
                ["nativeCastRequest"] = controls.NativeCastRequestCount,
                ["nativeRefusal"] = controls.NativeRefusalCount,
                ["dispatchAccepted"] = controls.DispatchAcceptedCount,
                ["dispatchRejected"] = controls.DispatchRejectedCount,
                ["activationCount"] = controls.ActivationRecordCount
            };
        }

        private JObject CaptureChunk6aRepeatedDismountBoundary(string name)
        {
            var selected = SelectionManager.Instance?.SelectedUnits;
            var fact = rider.Descriptor.Abilities.GetAbility(nativeControls.DismountAbility);
            var availability = nativeControls.Evaluate(NativeMountedControlKind.Dismount, rider);
            return new JObject
            {
                ["name"] = name,
                ["frame"] = Time.frameCount,
                ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks,
                ["allocationSequence"] = allocationTrace.EventCount,
                ["traceComplete"] = allocationTrace.Complete,
                ["turnBased"] = CombatController.IsInTurnBasedCombat(),
                ["inCombat"] = rider.IsInCombat && horse.IsInCombat && Game.Instance.Player.IsInCombat,
                ["pairIdle"] = rider.Commands.Empty && horse.Commands.Empty && !combat.HasActiveCommand,
                ["riderId"] = rider.UniqueId,
                ["mountId"] = horse.UniqueId,
                ["selectedIds"] = selected == null ? null : new JArray(selected.Select(unit => unit?.UniqueId)),
                ["exactSingleRider"] = selected != null && selected.Count == 1 && ReferenceEquals(selected[0], rider),
                ["selectedAbilityObject"] = Game.Instance.SelectedAbilityHandler?.Ability == null
                    ? 0
                    : RuntimeHelpers.GetHashCode(Game.Instance.SelectedAbilityHandler.Ability),
                ["riderCommandsEmpty"] = rider.Commands.Empty,
                ["mountCommandsEmpty"] = horse.Commands.Empty,
                ["shellCount"] = nativeControls.NativeRelationshipShellCount,
                ["processBindings"] = nativeControls.NativeRelationshipProcessBindingCount,
                ["controls"] = CaptureChunk6aRepeatedDismountControls(),
                ["state"] = CaptureChunk6aCausalState(),
                ["riderResources"] = allocationTrace.Snapshot(rider),
                ["mountResources"] = allocationTrace.Snapshot(horse),
                ["riderPosition"] = new JArray(rider.Position.x, rider.Position.y, rider.Position.z),
                ["mountPosition"] = new JArray(horse.Position.x, horse.Position.y, horse.Position.z),
                ["abilityPresent"] = fact?.Active == true && fact.Data != null,
                ["abilityObject"] = fact == null ? 0 : RuntimeHelpers.GetHashCode(fact),
                ["abilityDataObject"] = fact?.Data == null ? 0 : RuntimeHelpers.GetHashCode(fact.Data),
                ["availabilityVisible"] = availability.IsVisible,
                ["availabilityEnabled"] = availability.IsEnabled,
                ["availabilityReason"] = availability.Reason
            };
        }

        private JArray CaptureChunk6aRepeatedDismountEvents(int start, int end)
        {
            return new JArray(allocationTrace.EventsSince(start).Take(Math.Max(0, end - start)));
        }

        private static long LastChunk6aActivationSequence(
            IReadOnlyList<NativeMountedAbilityActivationRecord> records)
        {
            return records == null || records.Count == 0 ? 0L : records[records.Count - 1].Sequence;
        }

        // The native control service appends one activation ledger record per
        // observed control phase (target selection started/ended, cast requested or
        // refused, dispatch, terminal). A synchronous window proves it stayed passive
        // by exposing exactly the records it appended, never by hiding them behind a
        // flat count: the preview.144 cancellation lawfully appended the two
        // target-selection phases and nothing else.
        private JArray CaptureChunk6aRepeatedDismountActivationRecords(long lastSequenceBefore)
        {
            var appended = new JArray();
            foreach (var record in nativeControls.SnapshotAbilityActivations())
            {
                if (record.Sequence > lastSequenceBefore)
                {
                    appended.Add(ProjectChunk6aActivationRecord(record));
                }
            }
            return appended;
        }

        private static JObject ProjectChunk6aActivationRecord(NativeMountedAbilityActivationRecord record)
        {
            return new JObject
            {
                ["sequence"] = record.Sequence,
                ["activationId"] = record.ActivationId,
                ["phase"] = record.Phase.ToString(),
                ["kind"] = record.Kind.ToString(),
                ["abilityGuid"] = record.AbilityGuid,
                ["frame"] = record.Frame,
                ["casterId"] = record.CasterId,
                ["activeSelectedUnitIds"] = record.ActiveSelectedUnitIds,
                ["targetId"] = record.TargetId,
                ["targetSelectionMode"] = record.TargetSelectionMode,
                ["relationshipStateAtStart"] = record.RelationshipStateAtStart.ToString(),
                ["relationshipStateObserved"] = record.RelationshipStateObserved.ToString(),
                ["riderIdAtStart"] = record.RiderIdAtStart,
                ["mountIdAtStart"] = record.MountIdAtStart,
                ["riderViewChanged"] = record.RiderViewChanged,
                ["mountViewChanged"] = record.MountViewChanged,
                ["inCombat"] = record.InCombat,
                ["turnBased"] = record.TurnBased,
                ["gameMode"] = record.GameMode,
                ["currentTurnUnitId"] = record.CurrentTurnUnitId,
                ["lifecycleSequenceAtStart"] = record.LifecycleSequenceAtStart,
                ["lifecycleSequenceObserved"] = record.LifecycleSequenceObserved,
                ["lifecycleDeliveries"] = record.LifecycleDeliveries,
                ["cleanupTrigger"] = record.CleanupTrigger.HasValue
                    ? (JToken)record.CleanupTrigger.Value.ToString()
                    : JValue.CreateNull(),
                ["dispatchAccepted"] = record.DispatchAccepted.HasValue
                    ? (JToken)record.DispatchAccepted.Value
                    : JValue.CreateNull(),
                ["relationshipEnded"] = record.RelationshipEnded,
                ["relationshipTransitionChanged"] = record.RelationshipTransitionChanged,
                ["relationshipTransitionResult"] = record.RelationshipTransitionResult,
                ["terminalResult"] = record.TerminalResult
            };
        }

        private bool ObserveChunk6aRepeatedDismountCancellation()
        {
            if (!IsUnmountedAttackControls) return true;
            if (chunk6aRepeatedDismountCancellationComplete)
                throw new InvalidOperationException("Repeated Dismount cancellation was already measured.");
            if (!EnsureChunk6aRiderSelection(Chunk6aRepeatedDismountRow)) return false;

            var handler = Game.Instance.SelectedAbilityHandler;
            var ability = rider.Descriptor.Abilities.GetAbility(nativeControls.DismountAbility)?.Data;
            if (handler == null || ability == null || handler.Ability != null)
            {
                FailCurrent(Chunk6aRepeatedDismountRow,
                    "Exact native Dismount fact, empty selected-ability handler, or rider selection was unavailable before cancellation.");
                BeginCleanup();
                return false;
            }

            var start = allocationTrace.EventCount;
            var activationsBefore = LastChunk6aActivationSequence(nativeControls.SnapshotAbilityActivations());
            var before = CaptureChunk6aRepeatedDismountBoundary("cancel-before");
            handler.SetAbility(ability);
            var selected = new JObject
            {
                ["handlerObject"] = RuntimeHelpers.GetHashCode(handler),
                ["abilityDataObject"] = RuntimeHelpers.GetHashCode(ability),
                ["selectedAbilityObject"] = handler.Ability == null ? 0 : RuntimeHelpers.GetHashCode(handler.Ability),
                ["abilityGuid"] = handler.Ability?.Blueprint?.AssetGuid,
                ["casterId"] = handler.Ability?.Caster?.Unit?.UniqueId
            };
            handler.DropAbility();
            var after = CaptureChunk6aRepeatedDismountBoundary("cancel-after");
            var cancellation = new JObject
            {
                ["setAbilityInvoked"] = true,
                ["onClickInvoked"] = false,
                ["dropAbilityInvoked"] = true,
                ["before"] = before,
                ["selected"] = selected,
                ["after"] = after,
                ["activationRecords"] = CaptureChunk6aRepeatedDismountActivationRecords(activationsBefore),
                ["allocationEvents"] = CaptureChunk6aRepeatedDismountEvents(start, (int)after["allocationSequence"])
            };
            chunk6aRepeatedDismountEvidence["cancellation"] = cancellation;
            try
            {
                NativeRepeatedDismountEvidence.AssertCancellation(chunk6aRepeatedDismountEvidence);
            }
            catch (Exception exception)
            {
                AddRow(Chunk6aRepeatedDismountRow, false, exception.Message, chunk6aRepeatedDismountEvidence);
                BeginCleanup();
                return false;
            }
            chunk6aRepeatedDismountCancellationComplete = true;
            return true;
        }

        private void BeginChunk6aRepeatedDismountCommand()
        {
            if (!IsUnmountedAttackControls) return;
            if (!chunk6aRepeatedDismountCancellationComplete)
                throw new InvalidOperationException("Positive Dismount began before its exact cancellation control.");
            BeginChunk6aCommandWindow(nativeControls.DismountAbility.AssetGuid);
        }

        private void ObserveChunk6aRepeatedDismountClick(bool clicked)
        {
            if (!IsUnmountedAttackControls) return;
            chunk6aRepeatedDismountEvidence["positiveClicked"] = clicked;
            chunk6aRepeatedDismountEvidence["positiveInput"] =
                observations["rt-combat-dismount"]?.DeepClone();
            chunk6aCommandWindow.ClickCompleted(clicked);
        }

        private bool FinishChunk6aRepeatedDismountObservation()
        {
            if (!IsUnmountedAttackControls) return true;
            if (chunk6aCommandWindow?.Terminal != true) return false;

            var proof = FinishChunk6aCommandWindow("focused-combat-dismount", true, 0, false);
            chunk6aRepeatedDismountEvidence["positiveProof"] = proof;
            var afterPositive = CaptureChunk6aRepeatedDismountBoundary("positive-terminal");
            var repeatActivationsBefore = LastChunk6aActivationSequence(nativeControls.SnapshotAbilityActivations());
            chunk6aRepeatedDismountEvidence["afterPositive"] = afterPositive;
            chunk6aRepeatedDismountEvidence["terminalBridge"] =
                NativeRelationshipTerminalBridge.Capture(allocationTrace, proof, afterPositive, false);

            var start = allocationTrace.EventCount;
            var repeatedClicked = TryNativeAbilityTargetClick(
                nativeControls.DismountAbility, rider, "chunk6a-repeated-dismount-click");
            var afterRepeat = CaptureChunk6aRepeatedDismountBoundary("repeat-after");
            chunk6aRepeatedDismountEvidence["repeat"] = new JObject
            {
                ["before"] = afterPositive.DeepClone(),
                ["clicked"] = repeatedClicked,
                ["input"] = observations["chunk6a-repeated-dismount-click"]?.DeepClone(),
                ["inputBaseline"] = observations["chunk6a-repeated-dismount-click-before-input"]?.DeepClone(),
                ["after"] = afterRepeat,
                ["activationRecords"] = CaptureChunk6aRepeatedDismountActivationRecords(repeatActivationsBefore),
                ["allocationEvents"] = CaptureChunk6aRepeatedDismountEvents(start, (int)afterRepeat["allocationSequence"])
            };

            string failure = null;
            try { NativeRepeatedDismountEvidence.AssertComplete(chunk6aRepeatedDismountEvidence); }
            catch (Exception exception) { failure = exception.Message; }
            AddRow(Chunk6aRepeatedDismountRow, failure == null,
                failure ?? "Cancelling exact native Dismount targeting created no command, shell, movement, resource event or transition; one normal Dismount then paid one native rider Move, and the post-terminal repeated input was refused without a second transition or cost.",
                chunk6aRepeatedDismountEvidence);
            return failure == null;
        }
    }

    internal static class NativeRepeatedDismountEvidence
    {
        private const string Scenario = "unmounted-attack-controls-rt";
        private const string Dismount = "3af2b81f4d72bbb30501fa730fcdf36e";
        private static readonly string[] LedgerFields =
        {
            "admittedMount", "acceptedMount", "admittedDismount", "acceptedDismount",
            "refusedVoluntary", "forcedDetach", "duplicateSuppressed", "concurrentSuppressed"
        };
        private static readonly string[] ResourceFields =
        {
            "standard", "move", "swift", "reactionCooldown", "initiativeCooldown",
            "reactions", "reactionsPerRound", "initiativeOrder", "grantSequence"
        };

        private static void Require(bool condition, string reason)
        {
            if (!condition) throw new InvalidOperationException("Repeated Dismount: " + reason);
        }

        private static string Text(JToken value) =>
            value?.Type == JTokenType.String ? (string)value : null;

        private static long Int(JToken value)
        {
            Require(value?.Type == JTokenType.Integer, "integer missing");
            return (long)value;
        }

        private static bool Yes(JToken value) =>
            value?.Type == JTokenType.Boolean && (bool)value;

        private static bool No(JToken value) =>
            value?.Type == JTokenType.Boolean && !(bool)value;

        private static double Number(JToken value)
        {
            Require(value != null && (value.Type == JTokenType.Float || value.Type == JTokenType.Integer),
                "number missing");
            var result = (double)value;
            Require(!double.IsNaN(result) && !double.IsInfinity(result), "nonfinite number");
            return result;
        }

        private static void Same(JToken before, JToken after, string reason)
        {
            Require(before != null && after != null && JToken.DeepEquals(before, after), reason);
        }

        private static void SameCausalState(JToken before, JToken after)
        {
            Require(before is JObject && after is JObject, "causal state missing");
            var left = before.DeepClone();
            var right = after.DeepClone();
            (left["geometry"] as JObject)?.Remove("seconds");
            (right["geometry"] as JObject)?.Remove("seconds");
            Same(left, right, "causal state changed");
        }

        private static void SameResources(JToken before, JToken after, string role)
        {
            Require(Text(before?["actor"]) == Text(after?["actor"]) &&
                Int(before?["actorObject"]) == Int(after?["actorObject"]) &&
                Int(before["actorObject"]) != 0 && Yes(before["inCombat"]) && Yes(after["inCombat"]),
                role + " resource identity changed");
            foreach (var field in ResourceFields)
                Require(Math.Abs(Number(before[field]) - Number(after[field])) <= 0.0001,
                    role + " resource changed: " + field);
        }

        private static void BoundResources(JToken boundary, string role)
        {
            var native = boundary[role + "Resources"];
            var causal = boundary["state"]?[role];
            Require(Text(native?["actor"]) == Text(causal?["actor"]) &&
                Int(native?["grantSequence"]) == Int(causal?["nativePrepareCount"]),
                role + " native/causal allocation binding changed");
            foreach (var field in new[]
            {
                "standard", "move", "swift", "reactionCooldown", "initiativeCooldown",
                "reactions", "reactionsPerRound", "initiativeOrder"
            })
                Require(Math.Abs(Number(native[field]) - Number(causal[field])) <= 0.0001,
                    role + " native/causal resource differs: " + field);
        }

        private static void Boundary(JToken boundary, string rider, string mount, long generation, string state)
        {
            Require(boundary is JObject && No(boundary["turnBased"]) && Yes(boundary["inCombat"]) &&
                Yes(boundary["pairIdle"]) && Yes(boundary["traceComplete"]), "requires complete idle RT combat");
            Require(Text(boundary["riderId"]) == rider && Text(boundary["mountId"]) == mount &&
                Yes(boundary["exactSingleRider"]) && boundary["selectedIds"] is JArray &&
                boundary["selectedIds"].Count() == 1 && Text(boundary["selectedIds"][0]) == rider,
                "exact rider selection or pair identity changed");
            Require(Yes(boundary["riderCommandsEmpty"]) &&
                Yes(boundary["mountCommandsEmpty"]) && Int(boundary["selectedAbilityObject"]) == 0,
                "boundary retained a command or selected ability");
            Require(Text(boundary["state"]?["relationshipState"]) == state &&
                Int(boundary["state"]?["generation"]) == generation,
                "relationship state or generation changed");
            Require(Text(boundary["state"]?["rider"]?["actor"]) == rider &&
                Text(boundary["state"]?["mount"]?["actor"]) == mount,
                "causal resource actors changed");
            SameResources(boundary["riderResources"], boundary["riderResources"], "rider");
            SameResources(boundary["mountResources"], boundary["mountResources"], "mount");
            BoundResources(boundary, "rider");
            BoundResources(boundary, "mount");
            if (state == "Mounted")
            {
                Require(Yes(boundary["abilityPresent"]) && Int(boundary["abilityObject"]) != 0 &&
                    Int(boundary["abilityDataObject"]) != 0 && Yes(boundary["availabilityVisible"]) &&
                    Yes(boundary["availabilityEnabled"]), "mounted Dismount surface unavailable");
            }
            else
            {
                Require(No(boundary["abilityPresent"]) && No(boundary["availabilityVisible"]) &&
                    No(boundary["availabilityEnabled"]) &&
                    Text(boundary["availabilityReason"]) == "Dismount is available only to the exact mounted rider.",
                    "post-terminal Dismount surface was not exactly unavailable");
            }
        }

        private static void SameWindow(JToken window, string rider, string mount, bool controlsMaySelect)
        {
            var before = window?["before"];
            var after = window?["after"];
            Require(before is JObject && after is JObject, "synchronous window boundaries missing");
            foreach (var field in new[] { "frame", "gameTicks", "allocationSequence", "shellCount", "processBindings" })
                Require(Int(before[field]) == Int(after[field]), "synchronous window changed " + field);
            Same(before["riderPosition"], after["riderPosition"], "rider moved");
            Same(before["mountPosition"], after["mountPosition"], "mount moved");
            Same(before["state"]["ledger"], after["state"]["ledger"], "transition ledger changed");
            SameCausalState(before["state"], after["state"]);
            SameResources(before["riderResources"], after["riderResources"], "rider");
            SameResources(before["mountResources"], after["mountResources"], "mount");
            foreach (var field in new[] { "nativeCastRequest", "nativeRefusal", "dispatchAccepted", "dispatchRejected" })
                Require(Int(before["controls"][field]) == Int(after["controls"][field]),
                    "control count changed: " + field);
            var selectionDelta = controlsMaySelect ? 1L : 0L;
            Require(Int(after["controls"]["targetSelectionStart"]) - Int(before["controls"]["targetSelectionStart"]) == selectionDelta &&
                Int(after["controls"]["targetSelectionEnd"]) - Int(before["controls"]["targetSelectionEnd"]) == selectionDelta,
                "target selection callback count changed");
            PassiveSelectionRecords(window, before, after, rider, mount, controlsMaySelect);
        }

        // The native control service appends one activation ledger record per
        // observed control phase. A SetAbility/DropAbility cancellation therefore
        // lawfully appends exactly TargetSelectionStarted and TargetSelectionEnded
        // for one activation of the exact Dismount kind with no cast, dispatch,
        // relationship, view or lifecycle change, and a refused post-terminal input
        // appends none. The window must expose those records; a flat count that
        // contradicts the selection callbacks it also requires is not evidence.
        private static void PassiveSelectionRecords(
            JToken window, JToken before, JToken after, string rider, string mount, bool controlsMaySelect)
        {
            var records = window["activationRecords"] as JArray;
            Require(records != null, "activation records missing");
            Require(records.Count == (controlsMaySelect ? 2 : 0), "activation record count differs");
            Require(Int(after["controls"]["activationCount"]) - Int(before["controls"]["activationCount"]) == records.Count,
                "activation count differs from the appended records");
            if (!controlsMaySelect) return;
            var started = records[0];
            var ended = records[1];
            Require(Text(started["phase"]) == "TargetSelectionStarted" && Text(ended["phase"]) == "TargetSelectionEnded",
                "activation phases are not one passive target selection");
            Require(Text(started["terminalResult"]) == "target-selection-started" &&
                Text(ended["terminalResult"]) == "target-selection-cancelled",
                "activation terminal results are not one cancelled target selection");
            Require(Int(started["activationId"]) > 0 && Int(ended["activationId"]) == Int(started["activationId"]) &&
                Int(ended["sequence"]) == Int(started["sequence"]) + 1,
                "activation records are not one consecutive activation");
            foreach (var record in new[] { started, ended })
            {
                Require(Text(record["kind"]) == "Dismount" && Text(record["abilityGuid"]) == Dismount &&
                    Text(record["casterId"]) == rider && Text(record["activeSelectedUnitIds"]) == rider &&
                    Text(record["targetId"]) == "<none>" && Yes(record["targetSelectionMode"]),
                    "activation record identity differs");
                Require(Int(record["frame"]) == Int(before["frame"]), "activation record left the synchronous frame");
                Require(Text(record["relationshipStateAtStart"]) == "Mounted" &&
                    Text(record["relationshipStateObserved"]) == "Mounted" &&
                    Text(record["riderIdAtStart"]) == rider && Text(record["mountIdAtStart"]) == mount &&
                    No(record["riderViewChanged"]) && No(record["mountViewChanged"]) &&
                    No(record["relationshipEnded"]) && No(record["relationshipTransitionChanged"]),
                    "activation record observed a relationship or view change");
                Require(Yes(record["inCombat"]) && No(record["turnBased"]), "activation record is not RT combat");
                Require(record["dispatchAccepted"]?.Type == JTokenType.Null &&
                    record["cleanupTrigger"]?.Type == JTokenType.Null &&
                    Text(record["lifecycleDeliveries"]) == "<none>" &&
                    Int(record["lifecycleSequenceObserved"]) == Int(record["lifecycleSequenceAtStart"]),
                    "activation record observed a dispatch, cleanup or lifecycle delivery");
            }
        }

        private static void PositiveProof(JToken proof, string rider, string mount, long generation)
        {
            foreach (var field in new[] { "pass", "identityComplete", "sameCommandAtEveryBoundary",
                "exactActedObserved", "nativeTerminal", "traceComplete" })
                Require(Yes(proof?[field]), "positive command flag missing: " + field);
            Require(Int(proof["initCount"]) == 1 && proof["errors"] is JArray && !proof["errors"].Any() &&
                Text(proof["nativeResult"]) == "Success" && Yes(proof["resourceWindow"]?["pass"]) &&
                Yes(proof["resourceWindow"]?["reactionResources"]?["pass"]),
                "positive command/resource proof incomplete");
            var identity = proof["identity"];
            Require(Text(identity?["abilityGuid"]) == Dismount && Text(identity?["casterId"]) == rider &&
                Text(identity?["targetId"]) == rider && Text(identity?["commandType"]) == "Move" &&
                Int(identity?["generationAtInit"]) == generation &&
                !string.IsNullOrEmpty(Text(identity?["controlIdentity"])),
                "positive command identity differs");
            foreach (var field in new[] { "commandObject", "processObject", "contextObject" })
                Require(Int(identity[field]) != 0, "positive command identity incomplete: " + field);
            Require(Text(proof["mountId"]) == mount, "positive command mount differs");
            var terminals = proof["samples"].Where(sample => Text(sample["boundary"]) == "terminal").ToArray();
            Require(terminals.Length == 1 && Text(terminals[0]["state"]?["relationshipState"]) == "Unmounted",
                "positive terminal relationship differs");
            foreach (var field in LedgerFields)
            {
                var expected = field == "admittedDismount" || field == "acceptedDismount" ? 1L : 0L;
                Require(Int(proof["ledgerDelta"]?[field]) == expected, "positive ledger delta differs: " + field);
            }
        }

        internal static void AssertCancellation(JObject evidence)
        {
            Require(Text(evidence?["contract"]) == "cancel-once-one-native-dismount-then-repeat-refused" &&
                Text(evidence?["scenario"]) == Scenario && Text(evidence?["abilityGuid"]) == Dismount,
                "case identity differs");
            var rider = Text(evidence["riderId"]);
            var mount = Text(evidence["mountId"]);
            Require(!string.IsNullOrEmpty(rider) && !string.IsNullOrEmpty(mount) && rider != mount,
                "pair identity missing");
            var cancel = evidence["cancellation"];
            Require(Yes(cancel?["setAbilityInvoked"]) && No(cancel?["onClickInvoked"]) &&
                Yes(cancel?["dropAbilityInvoked"]) && cancel?["allocationEvents"] is JArray &&
                !cancel["allocationEvents"].Any(), "cancellation invoked a click or native allocation event");
            var before = cancel["before"];
            var after = cancel["after"];
            var generation = Int(before?["state"]?["generation"]);
            Boundary(before, rider, mount, generation, "Mounted");
            Boundary(after, rider, mount, generation, "Mounted");
            SameWindow(cancel, rider, mount, true);
            var selected = cancel["selected"];
            Require(Int(selected?["handlerObject"]) != 0 &&
                Int(selected?["abilityDataObject"]) == Int(before["abilityDataObject"]) &&
                Int(selected?["selectedAbilityObject"]) == Int(before["abilityDataObject"]) &&
                Text(selected?["abilityGuid"]) == Dismount && Text(selected?["casterId"]) == rider,
                "cancellation did not select the exact leased Dismount ability");
        }

        internal static void AssertComplete(JObject evidence)
        {
            AssertCancellation(evidence);
            var rider = Text(evidence["riderId"]);
            var mount = Text(evidence["mountId"]);
            var cancellationAfter = evidence["cancellation"]["after"];
            var generation = Int(cancellationAfter["state"]["generation"]);
            var proof = evidence["positiveProof"];
            PositiveProof(proof, rider, mount, generation);
            Require(Yes(evidence["positiveClicked"]) && Yes(evidence["positiveInput"]?["clicked"]) &&
                Text(evidence["positiveInput"]?["abilityGuid"]) == Dismount &&
                Text(evidence["positiveInput"]?["clickedTargetId"]) == rider &&
                Text(evidence["positiveInput"]?["resolvedTargetId"]) == rider,
                "positive native selected-ability click differs");
            foreach (var field in new[] { "frame", "gameTicks", "allocationSequence" })
                Require(Int(cancellationAfter[field]) == Int(proof["preClick"]?[field]),
                    "positive pre-click baseline does not immediately follow cancellation: " + field);
            SameCausalState(cancellationAfter["state"], proof["preClick"]["state"]);

            var afterPositive = evidence["afterPositive"];
            Boundary(afterPositive, rider, mount, generation, "Unmounted");
            NativeTerminalBridgeEvidence.AssertComplete(
                (JObject)proof, (JObject)afterPositive, (JObject)evidence["terminalBridge"], false, "Unmounted");

            var repeat = evidence["repeat"];
            Require(No(repeat?["clicked"]) && No(repeat?["input"]?["clicked"]) &&
                No(repeat?["input"]?["abilityPresent"]) && Yes(repeat?["input"]?["handlerPresent"]) &&
                Yes(repeat?["input"]?["targetViewPresent"]) &&
                repeat?["allocationEvents"] is JArray && !repeat["allocationEvents"].Any(),
                "post-terminal repeat was not refused before command/allocation creation");
            Boundary(repeat["before"], rider, mount, generation, "Unmounted");
            Boundary(repeat["after"], rider, mount, generation, "Unmounted");
            Same(afterPositive, repeat["before"], "repeat did not begin at the positive terminal boundary");
            SameWindow(repeat, rider, mount, false);
            var repeatedAvailability = repeat["inputBaseline"]?["abilityAvailableForCast"];
            Require(Text(repeat["inputBaseline"]?["availabilityReason"]) ==
                "Dismount is available only to the exact mounted rider." &&
                (repeatedAvailability == null || repeatedAvailability.Type == JTokenType.Null),
                "repeat baseline did not retain the exact unavailable Dismount reason");
        }
    }
}
