using System;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    // The structured parent-to-child handoff snapshot persisted in every tranche artifact from
    // preview.150. The compiled producer checks only the snapshot's internal integrity here:
    // identities and fields exist, references claimed identical are identical, numbers are
    // finite. Which handoffs are lawful for which child (the admission mode a mounted child
    // requires, the idle-party requirement) is judged only by the external validator
    // scripts/runtime/ChildEntryPreambleEvidence.ps1. Observation only.
    internal static class ChildEntryPreambleEvidence
    {
        internal const string Contract = "structured-child-entry-preamble-snapshot";
        internal const string ParentScenario = HorseCompanionUnmountedScenarioEngine.ScenarioName;
        internal const string ExplorationAdmission = "Exploration";
        internal const string VoluntaryCombatAdmission = "VoluntaryCombat";

        private static void Require(bool ok, string why) { if (!ok) throw new InvalidOperationException("Child entry preamble structure: " + why); }
        private static string Text(JToken x) => x?.Type == JTokenType.String ? (string)x : null;
        private static bool IsInt(JToken x) => x?.Type == JTokenType.Integer;
        private static bool IsBool(JToken x) => x?.Type == JTokenType.Boolean;
        private static bool IsFinite(JToken x)
        {
            if (x == null) return false;
            if (x.Type == JTokenType.Integer) return true;
            if (x.Type != JTokenType.Float) return false;
            var n = (double)x; return !double.IsNaN(n) && !double.IsInfinity(n);
        }
        private static long Int(JToken x, string label) { Require(IsInt(x), label + " integer missing"); return (long)x; }

        internal static void AssertStructure(JObject p, string childScenario, bool pairAlreadyMounted)
        {
            Require(p != null && Text(p["contract"]) == Contract, "contract differs");
            Require(Text(p["parentScenario"]) == ((childScenario == "chunk6c-casting-rt" || childScenario == "chunk6c-casting-tb" || childScenario == "chunk6c-casting-unmounted-rt" || childScenario == "chunk6c-casting-unmounted-tb" || childScenario == "chunk6d-staged-rt" || childScenario == "chunk6d-staged-tb" || childScenario == "chunk6e-reaction-rt" || childScenario == "chunk6e-reaction-tb") ? childScenario : ParentScenario) && !string.IsNullOrEmpty(Text(p["parentEngine"])), "parent identity missing");
            Require(!string.IsNullOrEmpty(childScenario) && Text(p["childScenario"]) == childScenario, "child scenario differs");
            Require(!string.IsNullOrEmpty(Text(p["runId"])) && Int(p["sessionObject"], "sessionObject") != 0 && !string.IsNullOrEmpty(Text(p["areaGuid"])), "session binding missing");
            // A JSON re-reader may parse the ISO timestamp as a date token; either form is the same clock evidence.
            Require(Int(p["frame"], "frame") >= 0 && Int(p["gameTicks"], "gameTicks") >= 0 && (!string.IsNullOrEmpty(Text(p["capturedAtUtc"])) || p["capturedAtUtc"]?.Type == JTokenType.Date), "clock missing");
            var rider = Text(p["riderId"]); var mount = Text(p["mountId"]);
            Require(!string.IsNullOrEmpty(rider) && !string.IsNullOrEmpty(mount) && rider != mount && Int(p["riderObject"], "riderObject") != 0 && Int(p["mountObject"], "mountObject") != 0 &&
                Int(p["riderObject"], "riderObject") != Int(p["mountObject"], "mountObject"), "pair identity missing");
            // Internally consistent references: the handoff flag, the relationship state it
            // claims and the explicitness of the recorded admission mode.
            Require(IsBool(p["pairAlreadyMounted"]) && (bool)p["pairAlreadyMounted"] == pairAlreadyMounted, "handoff mounted flag differs");
            Require(Text(p["relationshipState"]) == (pairAlreadyMounted ? "Mounted" : "Unmounted") && Int(p["relationshipGeneration"], "relationshipGeneration") >= 0, "relationship state contradicts the handoff flag");
            var admission = Text(p["admissionMode"]); Require(!string.IsNullOrEmpty(admission), "admission mode missing");
            Require(IsBool(p["admissionModeExplicit"]) && (bool)p["admissionModeExplicit"] == (admission != "<none>"), "admission explicitness differs");
            var commands = p["commands"] as JObject; Require(commands != null, "command snapshot missing");
            Require(IsBool(commands["riderCommandsEmpty"]) && IsBool(commands["mountCommandsEmpty"]) && IsInt(commands["riderRelationshipCommands"]) && IsInt(commands["mountRelationshipCommands"]), "command snapshot fields missing");
            var control = p["control"] as JObject; Require(control != null, "control snapshot missing");
            Require(IsBool(control["transitionInFlight"]) && IsBool(control["riderOwnsUnsettledShell"]) && Int(control["shellCount"], "shellCount") >= 0 && Int(control["processBindings"], "processBindings") >= 0 &&
                Int(control["dispatchAccepted"], "dispatchAccepted") >= 0 && Int(control["dispatchRejected"], "dispatchRejected") >= 0 && !string.IsNullOrEmpty(Text(control["transitionLedger"])), "control snapshot fields missing");
            foreach (var role in new[] { "rider", "mount" })
            {
                var actor = p["actors"]?[role] as JObject; Require(actor != null && Text(actor["id"]) == (role == "rider" ? rider : mount), role + " resources missing");
                foreach (var field in new[] { "standard", "move", "swift", "initiative", "reactionCooldown" }) Require(IsFinite(actor[field]) && (double)actor[field] >= 0, role + " " + field + " invalid");
                foreach (var field in new[] { "prepared", "inCombat", "canAct", "hasMove", "hasStandard", "commandRunning" }) Require(IsBool(actor[field]), role + " " + field + " missing");
                Require(Int(actor["reactions"], role + " reactions") >= 0, role + " reactions invalid");
            }
            var party = p["party"] as JObject; Require(party != null, "party snapshot missing");
            Require(IsBool(party["playerInCombat"]) && IsBool(party["idle"]) && Int(party["membersInCombat"], "membersInCombat") >= 0 && Int(party["members"], "members") >= 2, "party snapshot fields missing");
            Require(IsBool(p["turnBased"]) && IsBool(p["paused"]), "mode state missing");
        }
    }
}
