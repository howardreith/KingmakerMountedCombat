using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    // Producer-side validator for the CM05-forced-detach row. A native death or
    // incapacitation ends the mounted pair through lifecycle cleanup exactly once,
    // under a cleanup trigger, booking no voluntary Mount or Dismount admission, no
    // KMC control activity (selection, cast, dispatch, shell, process binding) and no
    // native action cost or preparation callback for either actor between the labelled
    // native damage and the settled cleanup. Repeated deliveries for the same detach
    // are suppressed by the transition ledger, never booked again. The activation
    // ledger records appended inside the window are exposed and bound exactly: when
    // the rider primary is the live paired command (mount death, rider incapacitation)
    // the control service appends one passive RelationshipEnded observation of that
    // activation; when the mount primary is live (rider death) it appends nothing.
    // Mirrored exactly by scripts/runtime/NativeForcedDetachEvidence.ps1; the synthetic
    // fixture test runs both readers over the same evidence and mutations.
    internal static class NativeForcedDetachEvidence
    {
        internal const string Row = "CM05-forced-detach";
        internal const string Contract = "forced-detach-records-once-pays-no-voluntary-move";
        internal const string Stimulus = "labelled-native-damage-effect";
        internal const string RelationshipEndedTerminal = "relationship-ended-before-primary-terminal";
        private const string Incapacitation = "chunk4-rider-incapacitation-tb";
        private const string RiderDeath = "chunk4-rider-death-tb";
        private static readonly string[] Scenarios = { RiderDeath, "chunk4-mount-death-tb", Incapacitation };
        private static readonly string[] LedgerFields =
        {
            "admittedMount", "acceptedMount", "admittedDismount", "acceptedDismount",
            "refusedVoluntary", "forcedDetach", "duplicateSuppressed", "concurrentSuppressed"
        };
        // Counters that must not move at all; activationCount moves by exactly the bound records.
        private static readonly string[] PassiveControlFields =
        {
            "targetSelectionStart", "targetSelectionEnd", "nativeCastRequest", "nativeRefusal",
            "dispatchAccepted", "dispatchRejected"
        };
        // The native lifecycle deliveries (boundary, cleanup trigger, handler source) that
        // may end a mounted pair when a pair actor dies or falls unconscious. The first is
        // the only one a non-lethal incapacitation can deliver.
        private static readonly string[][] LifeDeliveries =
        {
            new[] { "UnitIncapacitated", "Incapacitated", "IUnitLifeStateChanged.HandleUnitLifeStateChanged" },
            new[] { "UnitDeath", "Death", "IUnitHandler.HandleUnitDeath" },
            new[] { "UnitFinallyDead", "Death", "IUnitFinallyDeadHandler.HandleUnitBecameFinallyDead" }
        };

        private static void Require(bool condition, string reason)
        {
            if (!condition) throw new InvalidOperationException("Forced detach: " + reason);
        }

        private static string Text(JToken value) => value?.Type == JTokenType.String ? (string)value : null;

        private static long Int(JToken value)
        {
            Require(value?.Type == JTokenType.Integer, "integer missing");
            return (long)value;
        }

        private static bool Yes(JToken value) => value?.Type == JTokenType.Boolean && (bool)value;

        private static bool No(JToken value) => value?.Type == JTokenType.Boolean && !(bool)value;

        private static void Life(JToken life, string actor, bool conscious, bool dead)
        {
            Require(life is JObject && Text(life["actor"]) == actor && !string.IsNullOrEmpty(Text(life["lifeState"])) &&
                life["conscious"]?.Type == JTokenType.Boolean && (bool)life["conscious"] == conscious &&
                life["dead"]?.Type == JTokenType.Boolean && (bool)life["dead"] == dead &&
                life["finallyDead"]?.Type == JTokenType.Boolean, "native life state of " + actor + " differs");
        }

        private static void Boundary(JToken boundary, string name, string state, string rider, string mount)
        {
            Require(boundary is JObject && Text(boundary["name"]) == name, "boundary " + name + " missing");
            Require(Yes(boundary["traceComplete"]), "boundary lacks a complete native trace");
            Require(Yes(boundary["turnBased"]), "boundary is not native turn-based combat");
            Require(Text(boundary["relationshipState"]) == state, "boundary relationship state is not " + state);
            Require(Text(boundary["rider"]?["actor"]) == rider && Text(boundary["mount"]?["actor"]) == mount,
                "boundary resource snapshots name other actors");
            Require(boundary["ledger"] is JObject && boundary["controls"] is JObject, "boundary ledger or controls missing");
            foreach (var field in LedgerFields) Int(boundary["ledger"][field]);
            foreach (var field in PassiveControlFields) Int(boundary["controls"][field]);
            Int(boundary["controls"]["activationCount"]);
            foreach (var field in new[] { "frame", "gameTicks", "allocationSequence", "generation", "shellCount", "processBindings", "lifecycleSequence", "activationSequence" })
                Int(boundary[field]);
            Require(boundary["pairCommand"]?.Type == JTokenType.Boolean, "boundary pair command flag missing");
        }

        internal static void AssertComplete(JObject evidence)
        {
            Require(Text(evidence?["level"]) == "NATIVE INTEGRATION" && Text(evidence?["caseId"]) == Row &&
                Text(evidence?["contract"]) == Contract && Text(evidence?["mode"]) == "TB" &&
                Text(evidence?["nativeStimulus"]) == Stimulus, "case identity differs");
            var scenario = Text(evidence["scenario"]);
            Require(Array.IndexOf(Scenarios, scenario) >= 0, "scenario is not a native life scenario");
            var incapacitation = scenario == Incapacitation;
            var rider = Text(evidence["riderId"]);
            var mount = Text(evidence["mountId"]);
            Require(!string.IsNullOrEmpty(rider) && !string.IsNullOrEmpty(mount) && rider != mount, "pair identity missing");
            var subject = scenario == "chunk4-mount-death-tb" ? mount : rider;
            var survivor = subject == rider ? mount : rider;
            Require(Text(evidence["subject"]) == subject && Text(evidence["survivor"]) == survivor, "life subject differs from the scenario");

            var before = evidence["before"];
            var after = evidence["after"];
            Boundary(before, "before-damage", "Mounted", rider, mount);
            Boundary(after, "after-cleanup", "Unmounted", rider, mount);
            Require(Yes(before["inCombat"]) && Yes(before["pairCommand"]), "the stimulus did not interrupt a live paired command in native combat");
            Life(before["subjectLife"], subject, true, false);
            Life(before["survivorLife"], survivor, true, false);
            Require(No(after["pairCommand"]) && Yes(after["riderCommandsEmpty"]) && Yes(after["mountCommandsEmpty"]), "cleanup left a pair command");
            Life(after["subjectLife"], subject, false, !incapacitation);
            Life(after["survivorLife"], survivor, true, false);
            Require(Int(after["frame"]) >= Int(before["frame"]) && Int(after["gameTicks"]) >= Int(before["gameTicks"]) &&
                Int(after["allocationSequence"]) >= Int(before["allocationSequence"]) &&
                Int(after["lifecycleSequence"]) >= Int(before["lifecycleSequence"]) &&
                Int(after["activationSequence"]) >= Int(before["activationSequence"]), "window runs backwards");
            Require(Int(after["generation"]) == Int(before["generation"]), "a forced detach changed the pair generation");
            foreach (var field in PassiveControlFields)
                Require(Int(after["controls"][field]) == Int(before["controls"][field]), "native control count changed: " + field);
            foreach (var field in new[] { "shellCount", "processBindings" })
                Require(Int(after[field]) == Int(before[field]), "native relationship shell state changed: " + field);

            var deliveries = evidence["deliveries"] as JArray;
            Require(deliveries != null, "lifecycle deliveries missing");
            Require(deliveries.Count == Int(after["lifecycleSequence"]) - Int(before["lifecycleSequence"]), "lifecycle delivery window count differs");
            var expectedSequence = Int(before["lifecycleSequence"]);
            foreach (var item in deliveries)
            {
                expectedSequence++;
                Require(item is JObject && Int(item["sequence"]) == expectedSequence, "lifecycle deliveries are not consecutive");
                Require(item["cleanupAttempted"]?.Type == JTokenType.Boolean && item["cleanupSucceeded"]?.Type == JTokenType.Boolean &&
                    item["cleanupErrors"] is JArray && !string.IsNullOrEmpty(Text(item["boundary"])) &&
                    !string.IsNullOrEmpty(Text(item["source"])) && !string.IsNullOrEmpty(Text(item["stateBefore"])) &&
                    !string.IsNullOrEmpty(Text(item["stateAfter"])), "lifecycle delivery shape differs");
            }
            var cleanups = deliveries.Where(item => Yes(item["cleanupAttempted"]) && Text(item["stateBefore"]) == "Mounted").ToArray();
            Require(cleanups.Length == 1, "the forced detach was not attempted exactly once from the mounted state");
            var cleanup = cleanups[0];
            Require(Yes(cleanup["cleanupSucceeded"]) && Text(cleanup["stateAfter"]) == "Unmounted" && !cleanup["cleanupErrors"].Any(),
                "the one cleanup did not succeed into the unmounted state");
            var trigger = Text(cleanup["cleanupTrigger"]);
            var allowed = incapacitation ? LifeDeliveries.Take(1) : LifeDeliveries;
            Require(allowed.Any(delivery => Text(cleanup["boundary"]) == delivery[0] && trigger == delivery[1] && Text(cleanup["source"]) == delivery[2]),
                "the one cleanup was not delivered by a native life boundary with its cleanup trigger");
            var attempted = 0;
            foreach (var item in deliveries)
            {
                if (Yes(item["cleanupAttempted"])) attempted++;
                if (ReferenceEquals(item, cleanup)) continue;
                Require(Text(item["stateBefore"]) == Text(item["stateAfter"]), "another delivery changed the relationship state");
                Require(!Yes(item["cleanupAttempted"]) || (Text(item["stateBefore"]) == "Unmounted" && Yes(item["cleanupSucceeded"])),
                    "a repeated cleanup delivery was not an idempotent success from the unmounted state");
            }
            foreach (var field in LedgerFields)
            {
                var delta = Int(after["ledger"][field]) - Int(before["ledger"][field]);
                var expected = field == "forcedDetach" ? 1L : field == "duplicateSuppressed" ? attempted - 1L : 0L;
                Require(delta == expected, "transition ledger delta differs: " + field);
            }

            // Activation ledger records appended inside the window. The live paired
            // command is the mount primary for a rider death (nothing is appended) and
            // the rider primary otherwise (the service appends exactly one passive
            // RelationshipEnded observation of that activation when the pair leaves
            // Mounted). Any selection, cast, dispatch or terminal record is control activity.
            var records = evidence["activationRecords"] as JArray;
            Require(records != null, "activation records missing");
            var recordDelta = Int(after["activationSequence"]) - Int(before["activationSequence"]);
            Require(records.Count == recordDelta && recordDelta == Int(after["controls"]["activationCount"]) - Int(before["controls"]["activationCount"]),
                "activation record count differs from the activation ledger delta");
            Require(records.Count == (scenario == RiderDeath ? 0 : 1), "activation record count differs from the live primary of the scenario");
            var recordSequence = Int(before["activationSequence"]);
            foreach (var record in records)
            {
                recordSequence++;
                Require(record is JObject && Int(record["sequence"]) == recordSequence, "activation records are not the consecutive appended window");
                Require(Text(record["phase"]) == "RelationshipEnded" && Text(record["kind"]) == "RiderPrimary" &&
                    Text(record["terminalResult"]) == RelationshipEndedTerminal && Yes(record["relationshipEnded"]),
                    "an appended activation record is not the passive relationship-ended observation of the live rider primary");
                Require(Text(record["casterId"]) == rider && Text(record["riderIdAtStart"]) == rider && Text(record["mountIdAtStart"]) == mount &&
                    Text(record["targetId"]) == "<none>" && No(record["targetSelectionMode"]) && record["dispatchAccepted"]?.Type == JTokenType.Null,
                    "an appended activation record names another actor, a target, a selection or a dispatch");
                Require(Text(record["relationshipStateAtStart"]) == "Mounted" && Text(record["relationshipStateObserved"]) == "Unmounted" &&
                    Text(record["cleanupTrigger"]) == trigger, "an appended activation record does not observe this forced detach");
                Require(Int(record["frame"]) >= Int(before["frame"]) && Int(record["frame"]) <= Int(after["frame"]) &&
                    Int(record["lifecycleSequenceObserved"]) >= Int(cleanup["sequence"]) && Int(record["lifecycleSequenceObserved"]) <= Int(after["lifecycleSequence"]),
                    "an appended activation record lies outside the forced-detach window");
            }

            var transition = after["lastTransition"];
            Require(transition is JObject && Text(transition["kind"]) == "ForcedDetach" && Text(transition["riderId"]) == rider &&
                Text(transition["mountId"]) == mount && Int(transition["generationBefore"]) == Int(before["generation"]) &&
                Yes(transition["settled"]) && Yes(transition["accepted"]) && Text(transition["trigger"]) == trigger,
                "the last transition record is not the settled forced detach of this pair");
            var result = after["lastTransitionResult"];
            Require(result is JObject && Yes(result["succeeded"]) && Text(result["state"]) == "Unmounted" && Text(result["trigger"]) == trigger &&
                result["errors"] is JArray && !result["errors"].Any() && No(result["movementAuthorityResidual"]) && No(result["presentationResidual"]),
                "the last relationship result is not a residue-free cleanup under the delivered trigger");

            var events = evidence["allocationEvents"] as JArray;
            Require(events != null, "allocation events missing");
            Require(events.Count == Int(after["allocationSequence"]) - Int(before["allocationSequence"]), "allocation window count differs");
            var sequence = Int(before["allocationSequence"]);
            foreach (var item in events)
            {
                sequence++;
                Require(item is JObject && Int(item["sequence"]) == sequence, "allocation events are not the consecutive native window");
                var actor = Text(item["state"]?["actor"]);
                if (actor != rider && actor != mount) continue;
                var boundary = Text(item["boundary"]) ?? string.Empty;
                Require(boundary.IndexOf("cost", StringComparison.Ordinal) < 0, "a native cost callback fired for a pair actor during the forced detach");
                Require(!boundary.StartsWith("prepare-", StringComparison.Ordinal), "a native preparation fired for a pair actor during the forced detach");
            }
        }
    }
}
