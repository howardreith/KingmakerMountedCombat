using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    // Chunk 6A action-economy rows (CM03/CM05). One isolated fresh native TB transaction per
    // row, built on the allocation-order fixture. The compiled producer records raw structured
    // native evidence and checks only its internal integrity here: required identities and
    // fields exist, references claimed identical are identical, sequences and clocks do not
    // reverse, numbers are finite, the artifact is bounded, and the fixture leases it acquired
    // were restored. It never decides the mandatory behavior: legal command types, costs, debt
    // conservation, preparation and allocation, terminal behavior and fixture semantics are
    // judged only by the external validator scripts/runtime/NativeActionEconomyEvidence.ps1.
    // Observation only: no game resource is written by anything in this file.
    internal static class NativeActionEconomyEvidence
    {
        internal const string Contract = "chunk6a-action-economy-isolated-native-variant";
        internal const string MountAbilityGuid = "f053faad986631688defa003cd7bda0e";
        internal const string DismountAbilityGuid = "3af2b81f4d72bbb30501fa730fcdf36e";
        internal const string NoMoveToMountReason = "The rider has no Move action available to mount.";
        internal const string UnitMoveToType = "Kingmaker.UnitLogic.Commands.UnitMoveTo";
        internal const string UnitAttackType = "Kingmaker.UnitLogic.Commands.UnitAttack";
        internal const int UnrelatedInitiativeInput = 10;
        internal const int MaximumWindowEvents = 100000;

        internal sealed class Variant
        {
            public string Scenario, Row, MountSlot, RiderEntry, Dismount, TargetPlacement;
            public bool RiderFirst, Unrelated, AdjacentMount, Longbow, RiderExhaust;
            public bool Mounts => !RiderExhaust;
            public bool Dismounts => Dismount != "none";
            public bool OrderCompletion => Mounts && !Dismounts;
        }

        internal static readonly Variant[] Variants =
        {
            new Variant { Scenario = "chunk6a-mount-spent-move-tb", Row = "CM03-mount-spent-move", RiderFirst = false, MountSlot = "ground", RiderEntry = "ground", Dismount = "none", TargetPlacement = "mount-step-clear" },
            new Variant { Scenario = "chunk6a-mount-spent-standard-tb", Row = "CM03-mount-spent-standard", RiderFirst = false, MountSlot = "single-attack", RiderEntry = "ground", Dismount = "none", TargetPlacement = "near-mount" },
            new Variant { Scenario = "chunk6a-mount-spent-all-tb", Row = "CM03-mount-spent-all", RiderFirst = false, MountSlot = "full-attack", RiderEntry = "ground", Dismount = "none", TargetPlacement = "near-mount" },
            new Variant { Scenario = "chunk6a-rider-without-move-tb", Row = "CM03-rider-without-move", RiderFirst = true, MountSlot = "none", RiderEntry = "ground", RiderExhaust = true, Longbow = true, Dismount = "none", TargetPlacement = "default" },
            new Variant { Scenario = "chunk6a-rider-other-action-tb", Row = "CM03-rider-other-action", RiderFirst = true, MountSlot = "none", RiderEntry = "attack", Longbow = true, Dismount = "none", TargetPlacement = "default" },
            new Variant { Scenario = "chunk6a-unrelated-candidate-between-tb", Row = "CM03-unrelated-candidate-between", RiderFirst = true, Unrelated = true, MountSlot = "none", RiderEntry = "ground", Dismount = "none", TargetPlacement = "default" },
            new Variant { Scenario = "chunk6a-dismount-after-rider-expenditure-tb", Row = "CM05-after-rider-expenditure", RiderFirst = true, MountSlot = "none", RiderEntry = "ground", Longbow = true, Dismount = "later-after-rider", TargetPlacement = "default" },
            new Variant { Scenario = "chunk6a-dismount-after-mount-expenditure-tb", Row = "CM05-after-mount-expenditure", RiderFirst = true, MountSlot = "none", RiderEntry = "ground", Dismount = "later-after-mount", TargetPlacement = "dismount-ring" },
            new Variant { Scenario = "chunk6a-dismount-immediately-after-mount-tb", Row = "CM05-immediately-after-mount", RiderFirst = true, MountSlot = "none", RiderEntry = "five-foot-step", AdjacentMount = true, Dismount = "immediate", TargetPlacement = "default" }
        };

        internal static Variant VariantOf(string scenario) => Variants.FirstOrDefault(v => string.Equals(v.Scenario, scenario, StringComparison.Ordinal));
        internal static string[] Scenarios => Variants.Select(v => v.Scenario).ToArray();
        internal static string[] Rows => Variants.Select(v => v.Row).ToArray();

        private static void Require(bool ok, string why) { if (!ok) throw new InvalidOperationException("Action economy structure: " + why); }
        private static string Text(JToken x) => x?.Type == JTokenType.String ? (string)x : null;
        private static bool IsInt(JToken x) => x?.Type == JTokenType.Integer;
        private static bool IsBool(JToken x) => x?.Type == JTokenType.Boolean;
        private static bool IsNullOrText(JToken x) => x == null || x.Type == JTokenType.Null || x.Type == JTokenType.String;
        private static bool IsFinite(JToken x)
        {
            if (x == null) return false;
            if (x.Type == JTokenType.Integer) return true;
            if (x.Type != JTokenType.Float) return false;
            var n = (double)x; return !double.IsNaN(n) && !double.IsInfinity(n);
        }
        private static long Int(JToken x, string label) { Require(IsInt(x), label + " integer missing"); return (long)x; }
        private static JObject Obj(JToken x, string label) { Require(x is JObject, label + " missing"); return (JObject)x; }

        // Every number recorded under a token must be finite (NaN or infinity would not survive
        // serialization exactly) and every nested array must stay bounded.
        private static void AssertFinite(JToken token, string label)
        {
            if (token == null) return;
            switch (token.Type)
            {
                case JTokenType.Float: Require(IsFinite(token), label + " carries a non-finite number"); break;
                case JTokenType.Object: foreach (var property in ((JObject)token).Properties()) AssertFinite(property.Value, label + "." + property.Name); break;
                case JTokenType.Array:
                    var array = (JArray)token; Require(array.Count <= MaximumWindowEvents, label + " exceeds the bounded size");
                    foreach (var item in array) AssertFinite(item, label + "[]");
                    break;
            }
        }

        private static JObject Resources(JObject boundary, string role, string label)
        {
            var resources = Obj(boundary[role + "Resources"], label + " " + role + " resources");
            Require(!string.IsNullOrEmpty(Text(resources["actor"])) && IsInt(resources["actorObject"]) && IsInt(resources["grantSequence"]), label + " " + role + " resource identity missing");
            foreach (var field in new[] { "standard", "move", "swift" }) Require(IsFinite(resources[field]), label + " " + role + " " + field + " missing");
            return resources;
        }

        private static JObject Boundary(JToken token, string rider, string mount, string label)
        {
            var b = Obj(token, label);
            foreach (var field in new[] { "frame", "gameTicks", "allocationSequence", "turnObject", "round", "controllerObject", "sessionObject" }) Int(b[field], label + " " + field);
            // Between two native turns (a slot end captured after the mount's End) there is no
            // current actor, status or turn object; a present value must be a string.
            Require(IsNullOrText(b["currentActor"]) && IsNullOrText(b["status"]) && IsBool(b["turnBased"]), label + " turn identity missing");
            Require(Text(Resources(b, "rider", label)["actor"]) == rider && Text(Resources(b, "mount", label)["actor"]) == mount, label + " resources name another actor");
            var state = Obj(b["state"], label + " state");
            Require(!string.IsNullOrEmpty(Text(state["relationshipState"])) && IsInt(state["generation"]), label + " relationship state missing");
            return b;
        }

        private static void Ordered(JObject before, JObject after, string label)
        {
            foreach (var field in new[] { "frame", "gameTicks", "allocationSequence" })
                Require((long)after[field] >= (long)before[field], label + " " + field + " reverses");
        }

        // A recorded trace slice: contiguous ascending sequences from the window's own start,
        // each event with its boundary name, actor snapshot and a non-reversing clock.
        private static void Events(JToken token, long start, long end, string label)
        {
            var events = token as JArray; Require(events != null, label + " events missing");
            Require(events.Count <= MaximumWindowEvents, label + " events exceed the bounded size");
            var sequence = start; long ticks = -1;
            foreach (var e in events)
            {
                Require(e is JObject, label + " event is not an object");
                Require(Int(e["sequence"], label + " event sequence") == ++sequence, label + " trace sequence is not contiguous");
                Require(!string.IsNullOrEmpty(Text(e["boundary"])) && !string.IsNullOrEmpty(Text(e["state"]?["actor"])), label + " event identity missing");
                var gameTicks = Int(e["gameTicks"], label + " event clock"); Require(gameTicks >= ticks, label + " event clock reverses"); ticks = gameTicks;
            }
            Require(sequence == end, label + " trace does not close on its own after boundary");
        }

        private static void Window(JToken token, string rider, string mount, string label)
        {
            var w = Obj(token, label);
            var before = Boundary(w["before"], rider, mount, label + " before"); var after = Boundary(w["after"], rider, mount, label + " after");
            Ordered(before, after, label);
            Require(IsBool(w["traceComplete"]), label + " trace completeness missing");
            Events(w["events"], (long)before["allocationSequence"], (long)after["allocationSequence"], label);
        }

        // One recorded native command block: boundaries, the admitted and terminal command
        // snapshots naming the same command id and executor, the input record, the trace
        // slice and the native action availability snapshot.
        private static void CommandBlock(JToken token, string rider, string mount, string label)
        {
            var c = Obj(token, label);
            Require(!string.IsNullOrEmpty(Text(c["kind"])) && IsInt(c["turnObject"]) && IsInt(c["round"]), label + " identity missing");
            Window(c, rider, mount, label);
            var admitted = Obj(c["admitted"], label + " admitted"); var terminal = Obj(c["terminal"], label + " terminal");
            var id = Int(admitted["id"], label + " admitted id");
            Require(id != 0 && Int(terminal["id"], label + " terminal id") == id, label + " terminal names another command");
            Require(!string.IsNullOrEmpty(Text(admitted["type"])) && !string.IsNullOrEmpty(Text(terminal["type"])), label + " command type missing");
            Require(!string.IsNullOrEmpty(Text(admitted["executor"])) && Text(terminal["executor"]) == Text(admitted["executor"]), label + " executor differs between admission and terminal");
            foreach (var field in new[] { "started", "acted", "finished" }) Require(IsBool(terminal[field]), label + " terminal " + field + " missing");
            Require(!string.IsNullOrEmpty(Text(terminal["result"])), label + " terminal result missing");
            var input = Obj(c["input"], label + " input"); Require(IsBool(input["clicked"]), label + " input admission missing");
            var spent = Obj(c["spent"], label + " spent");
            foreach (var field in new[] { "usedOneMoveAction", "usedTwoMoveAction", "usedStandardAction", "hasMoveAction", "hasStandardAction" }) Require(IsBool(spent[field]), label + " spent " + field + " missing");
            Require(Text(spent["actor"]) == Text(admitted["executor"]), label + " spent snapshot names another actor");
        }

        internal static void AssertStructure(JObject e)
        {
            Require(e != null && Text(e["contract"]) == Contract, "contract missing");
            var v = VariantOf(Text(e["variant"])); Require(v != null, "variant is not registered");
            Require(Text(e["scenario"]) == v.Scenario && Text(e["row"]) == v.Row, "variant identity differs from its table entry");
            var rider = Text(e["riderId"]); var mount = Text(e["mountId"]);
            Require(!string.IsNullOrEmpty(rider) && !string.IsNullOrEmpty(mount) && rider != mount, "pair identity missing");
            var auto = Obj(e["automaticEnd"], "automatic End lease"); Require(IsBool(auto["leased"]) && IsBool(auto["temporaryValue"]), "automatic End lease record missing");
            if (v.MountSlot != "none")
            {
                CommandBlock(e["mountSlot"], rider, mount, "mountSlot");
                var slot = (JObject)e["mountSlot"]; var slotEnd = Boundary(slot["slotEnd"], rider, mount, "mountSlot slotEnd");
                Ordered((JObject)slot["after"], slotEnd, "mountSlot end"); Events(slot["endEvents"], (long)slot["after"]["allocationSequence"], (long)slotEnd["allocationSequence"], "mountSlot end");
                Window(e["retention"], rider, mount, "retention"); Int(e["retention"]["mountTurnsBetween"], "retention mountTurnsBetween");
            }
            var entry = Obj(e["riderEntry"], "riderEntry"); Require(!string.IsNullOrEmpty(Text(entry["kind"])), "riderEntry kind missing");
            if (v.RiderEntry != "ground") CommandBlock(entry, rider, mount, "riderEntry");
            if (v.RiderExhaust)
            {
                CommandBlock(e["exhaustion"], rider, mount, "exhaustion");
                var refusal = Obj(e["refusal"], "refusal"); Window(refusal, rider, mount, "refusal");
                Require(refusal["availability"] is JObject && refusal["click"] is JObject && refusal["controls"] is JObject && refusal["end"] is JObject, "refusal records missing");
            }
            if (v.Unrelated)
            {
                var u = Obj(e["unrelated"], "unrelated");
                Require(!string.IsNullOrEmpty(Text(u["actorId"])) && Text(u["actorId"]) != rider && Text(u["actorId"]) != mount && IsInt(u["actorObject"]), "unrelated identity missing");
                Require(IsInt(u["originalBase"]) && IsInt(u["inputBase"]) && IsBool(u["outsideCombat"]), "unrelated lease record missing");
            }
            if (v.Dismounts)
            {
                var d = Obj(e["dismount"], "dismount"); Require(Text(d["kind"]) == v.Dismount, "dismount kind differs from its table entry");
                var pre = Boundary(d["preDismount"], rider, mount, "preDismount"); var afterDismount = Boundary(d["afterDismount"], rider, mount, "afterDismount");
                Ordered(pre, afterDismount, "dismount");
                Require(v.Dismount == "immediate" ? d["mountTerminalBridge"] is JObject : d["laterTurn"] is JObject, "dismount allocation record missing");
                // The later-turn rider expenditure is this producer's own command block; the mount
                // expenditure block belongs to the Dismount-turn machinery and its own structural check.
                if (v.Dismount == "later-after-rider") CommandBlock(d["laterTurn"]?["riderAttack"], rider, mount, "laterTurn riderAttack");
                var release = Obj(e["release"], "release"); Int(release["dismountRound"], "release dismountRound");
                Require(release["turns"] is JArray && ((JArray)release["turns"]).Count <= 128, "release turn observations missing or unbounded");
                var mountTurn = Boundary(release["mountTurn"], rider, mount, "release mountTurn");
                Events(release["events"], (long)afterDismount["allocationSequence"], (long)mountTurn["allocationSequence"], "release");
                Require(IsBool(release["traceComplete"]), "release trace completeness missing");
            }
            AssertFinite(e, "actionEconomy");
        }

        // The fixture's own leases were released exactly (the producer's observation hooks
        // clean up); the external validator judges everything else.
        internal static void AssertRestoration(JObject e)
        {
            var auto = Obj(e?["automaticEnd"], "automatic End lease");
            Require(IsBool(auto["leased"]) && (bool)auto["leased"] && IsBool(auto["restored"]) && (bool)auto["restored"], "automatic End preference was not restored exactly");
            if (e["unrelated"] is JObject u)
            {
                var r = Obj(u["restoration"], "unrelated restoration");
                Require(IsBool(r["exact"]) && (bool)r["exact"] && IsBool(r["outsideCombat"]) && (bool)r["outsideCombat"] && IsInt(r["base"]) && IsInt(u["originalBase"]) && (long)r["base"] == (long)u["originalBase"],
                    "unrelated initiative input not restored exactly");
            }
        }
    }
}
