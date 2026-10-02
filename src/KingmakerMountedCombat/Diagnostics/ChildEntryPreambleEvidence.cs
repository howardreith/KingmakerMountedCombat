using System;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    // The parent-to-child handoff persisted as structured evidence in the child artifact:
    // parent and child scenario and session, the exact pair, the relationship state and
    // generation, the exact admission mode of the last native relationship dispatch, zero
    // in-flight Mount/Dismount command, shell, process, dispatch and cost, both actors'
    // resource and preparation state, and the frame and native game time. A bare string or
    // Boolean is not final qualification; this snapshot is validated at child entry and
    // again by the external reader.
    internal static class ChildEntryPreambleEvidence
    {
        internal const string Contract = "structured-child-entry-preamble-snapshot";
        internal const string ParentScenario = "horse-companion-unmounted-suite";
        internal const string ExplorationAdmission = "Exploration";
        internal const string VoluntaryCombatAdmission = "VoluntaryCombat";

        private static void Require(bool ok, string why) { if (!ok) throw new InvalidOperationException("Child entry preamble: " + why); }
        private static string Text(JToken x) => x?.Type == JTokenType.String ? (string)x : null;
        private static long Int(JToken x) { Require(x?.Type == JTokenType.Integer, "integer missing"); return (long)x; }
        private static bool Bool(JToken x) { Require(x?.Type == JTokenType.Boolean, "boolean missing"); return (bool)x; }
        private static double Num(JToken x) { Require(x != null && (x.Type == JTokenType.Integer || x.Type == JTokenType.Float), "number missing"); var n = (double)x; Require(!double.IsNaN(n) && !double.IsInfinity(n), "nonfinite number"); return n; }

        internal static void AssertComplete(JObject p, string childScenario, bool pairAlreadyMounted, bool requiresIdleParty)
        {
            Require(Text(p?["contract"]) == Contract, "contract differs");
            Require(Text(p["parentScenario"]) == ParentScenario && !string.IsNullOrEmpty(Text(p["parentEngine"])), "parent identity missing");
            Require(Text(p["childScenario"]) == childScenario && !string.IsNullOrEmpty(childScenario), "child scenario differs");
            Require(!string.IsNullOrEmpty(Text(p["runId"])) && Int(p["sessionObject"]) != 0 && !string.IsNullOrEmpty(Text(p["areaGuid"])), "session binding missing");
            // A JSON re-reader may parse the ISO timestamp as a date token; either form is the same clock evidence.
            Require(Int(p["frame"]) >= 0 && Int(p["gameTicks"]) >= 0 && (!string.IsNullOrEmpty(Text(p["capturedAtUtc"])) || p["capturedAtUtc"]?.Type == JTokenType.Date), "clock missing");
            var rider = Text(p["riderId"]); var mount = Text(p["mountId"]);
            Require(!string.IsNullOrEmpty(rider) && !string.IsNullOrEmpty(mount) && rider != mount && Int(p["riderObject"]) != 0 && Int(p["mountObject"]) != 0 && Int(p["riderObject"]) != Int(p["mountObject"]), "pair identity missing");
            Require(Bool(p["pairAlreadyMounted"]) == pairAlreadyMounted, "handoff mounted flag differs");
            Require(Text(p["relationshipState"]) == (pairAlreadyMounted ? "Mounted" : "Unmounted") && Int(p["relationshipGeneration"]) >= 0, "relationship state contradicts the handoff");
            var admission = Text(p["admissionMode"]); Require(!string.IsNullOrEmpty(admission), "admission mode missing");
            if (pairAlreadyMounted) Require(admission == ExplorationAdmission, "mounted handoff was not an exploration preamble Mount");
            else Require(admission != VoluntaryCombatAdmission, "unmounted handoff follows a voluntary combat dispatch");
            Require(Bool(p["admissionModeExplicit"]) == (admission != "<none>"), "admission explicitness differs");
            var commands = p["commands"];
            Require(Bool(commands?["riderCommandsEmpty"]) && Bool(commands["mountCommandsEmpty"]) && Int(commands["riderRelationshipCommands"]) == 0 && Int(commands["mountRelationshipCommands"]) == 0, "a Mount/Dismount command is in flight at child entry");
            var control = p["control"];
            Require(!Bool(control?["transitionInFlight"]) && !Bool(control["riderOwnsUnsettledShell"]) && Int(control["shellCount"]) >= 0 && Int(control["processBindings"]) >= 0 &&
                Int(control["dispatchAccepted"]) >= 0 && Int(control["dispatchRejected"]) >= 0 && !string.IsNullOrEmpty(Text(control["transitionLedger"])), "a relationship shell, process or dispatch is unsettled at child entry");
            foreach (var role in new[] { "rider", "mount" })
            {
                var actor = p["actors"]?[role]; Require(actor != null && Text(actor["id"]) == (role == "rider" ? rider : mount), role + " resources missing");
                foreach (var field in new[] { "standard", "move", "swift", "initiative", "reactionCooldown" }) Require(Num(actor[field]) >= 0, role + " " + field + " invalid");
                Require(actor["prepared"]?.Type == JTokenType.Boolean && actor["inCombat"]?.Type == JTokenType.Boolean && actor["canAct"]?.Type == JTokenType.Boolean &&
                    actor["hasMove"]?.Type == JTokenType.Boolean && actor["hasStandard"]?.Type == JTokenType.Boolean && Int(actor["reactions"]) >= 0, role + " preparation state missing");
                Require(!Bool(actor["commandRunning"]), role + " has a running native command at child entry");
            }
            var party = p["party"];
            Require(party?["playerInCombat"]?.Type == JTokenType.Boolean && Int(party["membersInCombat"]) >= 0 && Int(party["members"]) >= 2, "party state missing");
            if (requiresIdleParty) Require(!Bool(party["playerInCombat"]) && Int(party["membersInCombat"]) == 0 && Bool(party["idle"]), "the disposable party was not idle at child entry");
            Require(p["turnBased"]?.Type == JTokenType.Boolean && p["paused"]?.Type == JTokenType.Boolean, "mode state missing");
        }
    }
}
