using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    // Chunk 6A action-economy rows (CM03/CM05). One isolated fresh native TB transaction per
    // row, built on the allocation-order fixture. This validator binds the variant-specific
    // native evidence (mount-slot expenditure, debt retention, rider refusal, rider
    // expenditure, five-foot Acting entry, unrelated candidate, Dismount economy and the
    // split-release observation); the shared order/continuation structure is validated by
    // NativeMountOrderEvidence.AssertCore and the exact command windows by their own proofs.
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
            new Variant { Scenario = "chunk6a-mount-spent-move-tb", Row = "CM03-mount-spent-move", RiderFirst = false, MountSlot = "ground", RiderEntry = "ground", Dismount = "none", TargetPlacement = "default" },
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

        private static void Require(bool ok, string why) { if (!ok) throw new InvalidOperationException("Action economy: " + why); }
        private static string Text(JToken x) => x?.Type == JTokenType.String ? (string)x : null;
        private static long Int(JToken x) { Require(x?.Type == JTokenType.Integer, "integer missing"); return (long)x; }
        private static bool Bool(JToken x) { Require(x?.Type == JTokenType.Boolean, "boolean missing"); return (bool)x; }
        private static double Num(JToken x) { Require(x != null && (x.Type == JTokenType.Integer || x.Type == JTokenType.Float), "number missing"); var n = (double)x; Require(!double.IsNaN(n) && !double.IsInfinity(n), "nonfinite number"); return n; }
        private static bool Near(double a, double b) => Math.Abs(a - b) <= 0.0001;

        // The observed native action debt of one actor, replayed across a trace slice.
        // Standard/Move/Swift may change only through native cooldown ticks (the same model
        // NativePassiveResourceEvidence uses); every other boundary must leave them exact.
        private sealed class Debt
        {
            internal double Standard, Move, Swift; internal long Grants; internal string Actor; internal long Object; internal bool InCombat;
            internal double InitiativeCooldown;
            internal static Debt Read(JToken s) => new Debt {
                Actor = Text(s?["actor"]), Object = Int(s?["actorObject"]), InCombat = Bool(s?["inCombat"]), Grants = Int(s?["grantSequence"]),
                Standard = Num(s?["standard"]), Move = Num(s?["move"]), Swift = Num(s?["swift"]), InitiativeCooldown = Num(s?["initiativeCooldown"]) };
            internal bool Matches(Debt x) => x != null && Actor == x.Actor && Object == x.Object && Object != 0 && Grants == x.Grants &&
                Near(Standard, x.Standard) && Near(Move, x.Move) && Near(Swift, x.Swift);
            internal Debt Tick(JToken e)
            {
                var delta = Num(e["gameDeltaTime"]); Require(delta >= 0, "negative native delta");
                var tb = Bool(e["nativeTurnBased"]); var passing = Bool(e["nativePassing"]); var surprised = Bool(e["nativeSurprised"]);
                var waiting = Bool(e["state"]?["waitingInitiative"]);
                var decay = delta;
                if (tb && InCombat) { if (!passing) decay = 0; else if (InitiativeCooldown > 0 && !surprised) decay = Math.Max(0, decay - InitiativeCooldown); }
                else if ((tb && !passing) || waiting) decay = 0;
                return new Debt { Actor = Actor, Object = Object, InCombat = InCombat, Grants = Grants,
                    InitiativeCooldown = Math.Max(0, InitiativeCooldown - delta),
                    Standard = decay > 0 ? Math.Max(0, Standard - decay) : Standard, Move = decay > 0 ? Math.Max(0, Move - decay) : Move,
                    Swift = decay > 0 ? Math.Max(0, Swift - decay) : Swift };
            }
        }

        // Replays one actor's debt from a boundary snapshot through a contiguous trace slice.
        // allowedCostCommand: the one native command whose cost callbacks may change the debt
        // inside this slice (null when no command may charge the actor). Returns the final debt.
        private static Debt ReplayActorDebt(JToken before, JArray events, long startSequence, long endSequence, string actor, long? allowedCostCommand, string label)
        {
            var current = Debt.Read(before); Require(current.Actor == actor && current.Object != 0, label + ": baseline actor differs");
            Debt pending = null; var sequence = startSequence; var ticks = 0;
            foreach (var e in events)
            {
                Require(Int(e["sequence"]) == ++sequence, label + ": trace sequence gap");
                var who = Text(e["state"]?["actor"]); if (who != actor) continue;
                var boundary = Text(e["boundary"]) ?? ""; var actual = Debt.Read(e["state"]);
                Require(!boundary.Contains("prepare") && !boundary.Contains("clear") && !boundary.Contains("remove-unit"), label + ": native preparation, clear or removal occurred for " + actor + " at " + boundary);
                var command = e["command"]?.Type == JTokenType.Integer ? (long)e["command"] : 0L;
                var owned = allowedCostCommand.HasValue && command == allowedCostCommand.Value && command != 0;
                if (boundary.Contains("cost") || boundary.Contains("admission"))
                {
                    var action = Text(e["actionType"]);
                    Require(owned || action == "Free" || action == null, label + ": undeclared native " + action + " command callback for " + actor + " at " + boundary);
                    if (owned) { current = actual; pending = null; continue; }
                }
                if (boundary == "cooldown-tick-after") { Require(pending != null && pending.Matches(actual), label + ": native cooldown effect differs"); current = actual; pending = null; ticks++; continue; }
                if (pending != null) Require(pending.Matches(actual), label + ": nested native callback changed debt");
                else Require(current.Matches(actual) || owned, label + ": " + actor + " debt changed without a native tick at " + boundary);
                if (owned) current = actual;
                if (boundary == "cooldown-tick-before") { Require(pending == null, label + ": nested cooldown tick"); pending = actual.Tick(e); }
            }
            Require(sequence == endSequence && pending == null, label + ": trace did not close");
            return current;
        }

        private static JArray Events(JToken p) { Require(p?["events"] is JArray, "events missing"); return (JArray)p["events"]; }

        private static void AssertBoundaryPair(JToken before, JToken after, string actor, string label)
        {
            Require(Int(after["frame"]) >= Int(before["frame"]) && Int(after["gameTicks"]) >= Int(before["gameTicks"]) &&
                Int(after["allocationSequence"]) >= Int(before["allocationSequence"]), label + ": boundary order differs");
            Require(Int(before["turnObject"]) != 0 && Int(after["turnObject"]) == Int(before["turnObject"]) && Int(after["round"]) == Int(before["round"]) &&
                Text(before["currentActor"]) == actor && Text(after["currentActor"]) == actor, label + ": native turn or actor differs");
            Require(Bool(before["turnBased"]) && Bool(after["turnBased"]), label + ": native mode differs");
            Require(Int(before["controllerObject"]) == Int(after["controllerObject"]) && Int(before["sessionObject"]) == Int(after["sessionObject"]), label + ": encounter identity changed");
        }

        private static JToken Resources(JToken boundary, string role) => boundary[role + "Resources"];

        // One ordinary native command issued by an actor on its own turn, with the exact native
        // cost callbacks it produced. kind: ground | single-attack | full-attack | five-foot-step.
        private static void AssertActorCommand(JObject c, string actor, string other, string kind, string label)
        {
            var before = c["before"]; var after = c["after"]; var admitted = c["admitted"]; var terminal = c["terminal"];
            AssertBoundaryPair(before, after, actor, label);
            var id = Int(admitted?["id"]);
            Require(id != 0 && Int(terminal?["id"]) == id && Text(admitted["executor"]) == actor && Text(terminal["executor"]) == actor &&
                Bool(terminal["finished"]) && Bool(terminal["acted"]) && Text(terminal["result"]) == "Success", label + ": exact native command did not succeed");
            var expectedType = kind == "single-attack" || kind == "full-attack" ? UnitAttackType : UnitMoveToType;
            Require(Text(admitted["type"]) == expectedType && Text(terminal["type"]) == expectedType, label + ": command type differs");
            Require(Bool(c["traceComplete"]), label + ": allocation trace incomplete");
            var input = c["input"]; Require(input != null && Bool(input["clicked"]), label + ": native input was not admitted");
            var events = Events(c); var start = Int(before["allocationSequence"]); var end = Int(after["allocationSequence"]);
            var costs = new List<JToken>(); var admissions = 0; var otherActorCosts = 0;
            long sequence = start;
            foreach (var e in events)
            {
                Require(Int(e["sequence"]) == ++sequence, label + ": event sequence gap");
                var who = Text(e["state"]?["actor"]); var boundary = Text(e["boundary"]) ?? "";
                if (who != actor && who != other) continue;
                Require(!boundary.Contains("prepare") && !boundary.Contains("clear") && !boundary.Contains("turn-end") && !boundary.Contains("remove-unit"), label + ": undeclared native allocation event " + boundary);
                if (boundary.Contains("cost") || boundary.Contains("admission"))
                {
                    Require(who == actor && Int(e["command"]) == id && Text(e["commandActor"]) == actor && !Bool(e["simulatingClick"]), label + ": another actor or command owns a native callback at " + boundary);
                    if (who == other) otherActorCosts++;
                    if (boundary.Contains("admission")) admissions++;
                    if (boundary.Contains("cost")) costs.Add(e);
                }
            }
            Require(sequence == end && admissions == 2 && otherActorCosts == 0, label + ": admission or trace terminal differs");
            var boundaries = new[] { "cost-before", "actor-cost-before", "actor-cost-after", "cost-after" };
            Require(costs.Count == boundaries.Length, label + ": native TB cost callback count differs");
            for (var i = 0; i < costs.Count; i++) Require(Text(costs[i]["boundary"]) == boundaries[i] && Bool(costs[i]["acted"]), label + ": native cost boundary order differs");
            var b = costs[0]["state"]; var a = costs[costs.Count - 1]["state"];
            var beforeRes = Resources(before, actor == Text(before["riderResources"]?["actor"]) ? "rider" : "mount");
            var afterRes = Resources(after, actor == Text(after["riderResources"]?["actor"]) ? "rider" : "mount");
            Require(Text(beforeRes?["actor"]) == actor && Text(afterRes?["actor"]) == actor, label + ": actor resources missing");
            Require(Int(afterRes["grantSequence"]) == Int(beforeRes["grantSequence"]), label + ": native preparation repeated");
            if (kind == "ground" || kind == "five-foot-step")
            {
                foreach (var e in costs) Require(Bool(e["ignoreCooldown"]) && Text(e["actionType"]) == "Move", label + ": native TB ground command charged at acted");
                foreach (var field in new[] { "standard", "move", "swift" }) Require(Near(Num(b[field]), Num(a[field])), label + ": ground acted callback changed debt");
                Require(Near(Num(afterRes["standard"]), Num(beforeRes["standard"])) && Near(Num(afterRes["swift"]), Num(beforeRes["swift"])), label + ": ground command changed another action");
                var delta = Num(afterRes["move"]) - Num(beforeRes["move"]);
                var allowed = Num(afterRes["measuredAllowedTime"]) - Num(beforeRes["measuredAllowedTime"]);
                if (kind == "ground") Require(delta > 0 && allowed > 0 && Near(delta, allowed), label + ": Move debt differs from the observed native allowed movement time");
                else Require(Near(delta, 0) && Num(c["stepMetres"]) > 0 && Num(c["stepMetres"]) <= Num(c["stepLimit"]) + 0.01 && Bool(input["fiveFootStep"]), label + ": five-foot step charged Move or exceeded the native step");
            }
            else
            {
                foreach (var e in costs) Require(!Bool(e["ignoreCooldown"]) && Text(e["actionType"]) == "Standard", label + ": attack cost is not one native Standard command");
                Require(Near(Num(a["standard"]), Num(b["standard"]) + 6), label + ": native Standard cost differs");
                Require(Near(Num(a["swift"]), Num(b["swift"])), label + ": attack wrote Swift");
                var full = kind == "full-attack";
                Require(Bool(c["nativeFull"]) == full && Bool(c["nativeSingle"]) != full && Bool(input["fullEnabled"]) == full, label + ": native attack mode differs");
                Require(Near(Num(a["move"]), Num(b["move"]) + (full ? 3 : 0)), label + ": native attack Move component differs");
                Require(Near(Num(afterRes["standard"]), Num(beforeRes["standard"]) + 6) && Near(Num(afterRes["move"]), Num(beforeRes["move"]) + (full ? 3 : 0)), label + ": attack terminal debt differs");
            }
        }

        private static void AssertSpent(JToken s, bool usedOneMove, bool usedStandard, bool hasMove, bool hasStandard, string label)
        {
            Require(Bool(s?["usedOneMoveAction"]) == usedOneMove && Bool(s["usedStandardAction"]) == usedStandard &&
                Bool(s["hasMoveAction"]) == hasMove && Bool(s["hasStandardAction"]) == hasStandard, label + ": native action availability differs");
        }

        private static JObject MountSlotTurn(JObject e, Variant v, JObject order)
        {
            var slot = (JObject)e["mountSlot"]; Require(slot != null, "mount slot evidence missing");
            var rider = Text(e["riderId"]); var mount = Text(e["mountId"]);
            Require(Text(slot["kind"]) == v.MountSlot, "mount slot kind differs");
            AssertActorCommand(slot, mount, rider, v.MountSlot, "mount slot");
            var before = slot["before"]; var after = slot["after"]; var slotEnd = slot["slotEnd"];
            Require(Text(before["status"]) == "Preparing" && Text(after["status"]) == "Acting", "mount slot did not enter Acting through its own command");
            Require(Int(Resources(before, "mount")["grantSequence"]) == 1 && Int(Resources(before, "rider")["grantSequence"]) == 0, "mount slot preparation or rider order differs");
            Require(Text(before["state"]?["relationshipState"]) == "Unmounted" && Text(after["state"]?["relationshipState"]) == "Unmounted", "mount slot relationship differs");
            var spent = slot["spent"];
            if (v.MountSlot == "ground") AssertSpent(spent, true, false, true, true, "mount slot");
            else if (v.MountSlot == "single-attack") AssertSpent(spent, false, true, true, false, "mount slot");
            else AssertSpent(spent, true, true, false, false, "mount slot");
            // The slot end: the native End of the mount's own turn keeps its debt (ForceToEnd(false)).
            Require(Int(slotEnd["round"]) == Int(before["round"]) && Int(slotEnd["allocationSequence"]) >= Int(after["allocationSequence"]) &&
                Text(slotEnd["currentActor"]) != mount, "mount slot end boundary differs");
            var endEvents = (JArray)slot["endEvents"]; Require(endEvents != null, "slot end events missing");
            var ends = endEvents.Count(x => Text(x["boundary"]) == "turn-end-after" && Text(x["state"]?["actor"]) == mount);
            Require(ends == 1 && !endEvents.Any(x => Text(x["state"]?["actor"]) == mount && (Text(x["boundary"]) ?? "").Contains("cost")), "mount slot did not end exactly once without another cost");
            var debt = ReplayActorDebt(Resources(after, "mount"), endEvents, Int(after["allocationSequence"]), Int(slotEnd["allocationSequence"]), mount, null, "mount slot end");
            Require(debt.Matches(Debt.Read(Resources(slotEnd, "mount"))), "mount debt changed at its own native End");
            Require(Int(order["mountBefore"]["mount"]["nativePrepareCount"]) == 1 && Text(order["disposition"]) == "RetainPartnerParticipation" &&
                Int(order["mounted"]["partnerContextObject"]) == 0, "spent mount was not retained without preparation");
            return slot;
        }

        private static void Retention(JObject e, JObject slot, JObject mountProof)
        {
            var r = (JObject)e["retention"]; Require(r != null, "retention evidence missing");
            var mount = Text(e["mountId"]);
            var before = r["before"]; var after = r["after"];
            Require(JToken.DeepEquals(before, slot["slotEnd"]), "retention does not start at the mount slot end");
            Require(Bool(r["traceComplete"]) && Int(r["mountTurnsBetween"]) == 0, "retention trace incomplete or mount acted again");
            var debt = ReplayActorDebt(Resources(before, "mount"), Events(r), Int(before["allocationSequence"]), Int(after["allocationSequence"]), mount, null, "retention");
            Require(debt.Matches(Debt.Read(Resources(after, "mount"))), "mount debt was refreshed before the Mount");
            var pre = mountProof["preClick"];
            Require(Int(pre["allocationSequence"]) == Int(after["allocationSequence"]) && Int(pre["frame"]) == Int(after["frame"]) && Int(pre["gameTicks"]) == Int(after["gameTicks"]), "retention end is not the exact Mount pre-click");
            var terminal = mountProof["samples"].OfType<JObject>().Single(s => Text(s["boundary"]) == "terminal");
            foreach (var field in new[] { "standard", "move", "swift" })
            {
                Require(Near(Num(pre["state"]["mount"][field]), Num(Resources(after, "mount")[field])), "pre-click mount debt differs: " + field);
                Require(Near(Num(terminal["state"]["mount"][field]), Num(pre["state"]["mount"][field])), "Mount changed the retained mount debt: " + field);
                Require(Near(Num(terminal["nativeAllocation"]["mount"][field]), Num(terminal["state"]["mount"][field])), "terminal mount allocation differs: " + field);
            }
            Require(Int(terminal["state"]["mount"]["nativePrepareCount"]) == Int(Resources(after, "mount")["grantSequence"]) &&
                Int(mountProof["resourceWindow"]["mountPrepareDelta"]) == 0 && Int(mountProof["resourceWindow"]["clearCount"]) == 0 &&
                Int(mountProof["resourceWindow"]["expectedMountPrepareDelta"]) == 0, "Mount prepared or cleared the spent mount");
        }

        private static void RiderEntry(JObject e, Variant v, JObject mountProof)
        {
            var entry = (JObject)e["riderEntry"]; Require(entry != null && Text(entry["kind"]) == v.RiderEntry, "rider Acting entry differs from the variant");
            var rider = Text(e["riderId"]); var mount = Text(e["mountId"]);
            if (v.RiderEntry == "ground")
            {
                Require(Text(entry["setupContract"]) == "native-rider-ground-order-before-mount-baseline" && Int(entry["turnObject"]) != 0, "ground Acting entry binding missing");
                return;
            }
            AssertActorCommand(entry, rider, mount, v.RiderEntry == "attack" ? "single-attack" : "five-foot-step", "rider entry");
            Require(Text(entry["before"]["status"]) == "Preparing" && Text(entry["after"]["status"]) == "Acting", "rider entry did not take the turn from Preparing to Acting");
            var after = Resources(entry["after"], "rider");
            if (v.RiderEntry == "attack")
            {
                Require(Bool(entry["weapon"]?["ranged"]) && Bool(entry["spent"]?["usedStandardAction"]) && Bool(entry["spent"]["hasMoveAction"]) && !Bool(entry["spent"]["usedOneMoveAction"]), "rider other action did not leave exactly one lawful Move");
            }
            else
            {
                Require(Bool(entry["spent"]?["hasMoveAction"]) && !Bool(entry["spent"]["usedOneMoveAction"]) && !Bool(entry["spent"]["usedStandardAction"]), "five-foot step spent an action");
                Require(Bool(entry["after"]["state"]["geometry"]["isAdjacent"]), "five-foot step did not end inside the Mount envelope");
            }
            if (mountProof == null) return;
            var pre = mountProof["preClick"];
            Require(Int(pre["state"]["rider"]["nativeTurnObject"]) == Int(entry["after"]["turnObject"]), "Mount belongs to another rider turn");
            foreach (var field in new[] { "standard", "move", "swift" })
                Require(Near(Num(pre["state"]["rider"][field]), Num(after[field])), "entry debt did not carry into the Mount baseline: " + field);
            if (v.RiderEntry == "attack")
            {
                var terminal = mountProof["samples"].OfType<JObject>().Single(s => Text(s["boundary"]) == "terminal");
                Require(Near(Num(terminal["state"]["rider"]["standard"]), Num(after["standard"])) && Near(Num(after["standard"]), 6), "Mount changed the rider's prior Standard debt");
            }
        }

        private static void Refusal(JObject e)
        {
            var rider = Text(e["riderId"]); var mount = Text(e["mountId"]);
            var exhaustion = (JObject)e["exhaustion"]; Require(exhaustion != null, "exhaustion evidence missing");
            AssertActorCommand(exhaustion, rider, mount, "single-attack", "exhaustion");
            Require(Text(exhaustion["before"]["status"]) == "Acting" && Bool(exhaustion["weapon"]?["ranged"]), "exhaustion is not a Standard action on the Acting setup turn");
            AssertSpent(exhaustion["spent"], true, true, false, false, "exhaustion");
            var r = (JObject)e["refusal"]; Require(r != null, "refusal evidence missing");
            var before = r["before"]; var after = r["after"];
            AssertBoundaryPair(before, after, rider, "refusal");
            Require(Int(before["turnObject"]) == Int(exhaustion["after"]["turnObject"]) && Text(before["status"]) == "Acting", "refusal is not on the exhausted Acting turn");
            var availability = r["availability"];
            Require(Bool(availability["visible"]) && !Bool(availability["enabled"]) && !Bool(availability["transitionReady"]) &&
                (Text(availability["reason"]) ?? "").Contains(NoMoveToMountReason), "availability did not refuse for the missing Move");
            Require(!Bool(r["abilityAvailableForCast"]) && !Bool(r["canTarget"]), "native targeting admitted a rider without Move");
            Require(!Bool(before["state"]["rider"]["hasMove"]) && !Bool(after["state"]["rider"]["hasMove"]), "rider held a Move at refusal");
            var click = r["click"];
            Require(!Bool(click["clicked"]) && !Bool(click["nativeShell"]["present"]) && Int(click["dispatchAcceptedDelta"]) == 0 && Int(click["dispatchRejectedDelta"]) == 0 &&
                Int(click["nativePrimaryShellPrepareDelta"]) == 0 && Int(click["nativeCastRequestDelta"]) == Int(click["nativeRefusalDelta"]) && Int(click["nativeCastRequestDelta"]) <= 1 &&
                Int(click["targetSelectionStartDelta"]) == Int(click["targetSelectionEndDelta"]) && Int(click["targetSelectionStartDelta"]) <= 1, "native click created a command, shell or dispatch");
            var controls = r["controls"];
            foreach (var field in new[] { "shellCount", "processBindings", "dispatchAccepted", "dispatchRejected" })
                Require(Int(controls["before"][field]) == Int(controls["after"][field]), "refusal changed native control state: " + field);
            Require(JToken.DeepEquals(r["ledgerBefore"], r["ledgerAfter"]), "refusal changed the transition ledger");
            Require(Text(before["state"]["relationshipState"]) == "Unmounted" && Text(after["state"]["relationshipState"]) == "Unmounted" &&
                Int(before["state"]["generation"]) == Int(after["state"]["generation"]), "refusal changed the relationship");
            Require(Bool(r["riderCommandsEmptyAfter"]) && Bool(r["mountCommandsEmptyAfter"]) && Bool(r["traceComplete"]), "a command survived the refusal");
            foreach (var pair in new[] { new[] { "rider", rider }, new[] { "mount", mount } })
            {
                var debt = ReplayActorDebt(Resources(before, pair[0]), Events(r), Int(before["allocationSequence"]), Int(after["allocationSequence"]), pair[1], null, "refusal " + pair[0]);
                Require(debt.Matches(Debt.Read(Resources(after, pair[0]))), "refusal changed " + pair[0] + " debt");
            }
            var end = r["end"]; Require(Text(end?["method"]) == "Kingmaker.Game.PauseBind" && Text(end["token"]) == "06000CB7" && Int(end["count"]) == 1 &&
                Int(end["beforeEndInput"]["turnObject"]) == Int(before["turnObject"]) && Int(end["afterEnd"]["turnObject"]) != Int(before["turnObject"]), "exhausted turn did not end through one native End input");
        }

        private static void Unrelated(JObject e, JObject order)
        {
            var u = (JObject)e["unrelated"]; Require(u != null, "unrelated candidate evidence missing");
            var rider = Text(e["riderId"]); var mount = Text(e["mountId"]); var actor = Text(u["actorId"]);
            Require(!string.IsNullOrEmpty(actor) && actor != rider && actor != mount && Int(u["actorObject"]) != 0, "unrelated candidate identity missing");
            Require(Int(u["inputBase"]) == UnrelatedInitiativeInput && Bool(u["outsideCombat"]) && JToken.DeepEquals(u["beforeResources"], u["afterResources"]), "unrelated initiative input differs or changed resources");
            var roster = order["mounted"]["roster"].Select(x => Text(x["actor"])).ToArray();
            var ri = Array.IndexOf(roster, rider); var ui = Array.IndexOf(roster, actor); var mi = Array.IndexOf(roster, mount);
            Require(ri >= 0 && ui >= 0 && mi >= 0 && ri < ui && ui < mi && Int(u["rosterIndexAtMount"]) == ui, "unrelated candidate is not between rider and mount in the native roster");
            var round = Int(order["mounted"]["round"]); var mountedSequence = Int(order["mounted"]["allocationSequence"]); var nextSequence = Int(order["nextRound"]["allocationSequence"]);
            var turns = order["turns"].Where(t => Text(t["currentActor"]) == actor && Int(t["round"]) == round && Int(t["allocationSequence"]) > mountedSequence).Select(t => Int(t["turnObject"])).Distinct().ToArray();
            Require(turns.Length == 1, "unrelated candidate did not take exactly one native turn after the Mount");
            var events = order["allocationTrace"]["events"].Where(x => Text(x["state"]?["actor"]) == actor && Int(x["sequence"]) > mountedSequence && Int(x["sequence"]) <= nextSequence).ToArray();
            Require(events.Count(x => Text(x["boundary"]) == "prepare-before" && Int(x["round"]) == round) == 1 &&
                events.Count(x => Text(x["boundary"]) == "prepare-after" && Int(x["round"]) == round) == 1 &&
                events.Count(x => Text(x["boundary"]) == "turn-end-after" && Int(x["round"]) == round) == 1, "unrelated candidate was skipped or duplicated");
            Require(!order["turns"].Any(t => Text(t["currentActor"]) == actor && Int(t["round"]) == round && Int(t["allocationSequence"]) < mountedSequence), "unrelated candidate acted before the rider");
        }

        private static void Dismount(JObject e, Variant v, JObject order, JObject mountProof, JObject dismountProof)
        {
            var d = (JObject)e["dismount"]; Require(d != null && Text(d["kind"]) == v.Dismount, "dismount evidence or kind differs");
            var rider = Text(e["riderId"]); var mount = Text(e["mountId"]);
            Require(dismountProof != null && Bool(dismountProof["pass"]) && Text(dismountProof["identity"]["abilityGuid"]) == DismountAbilityGuid &&
                Text(dismountProof["identity"]["casterId"]) == rider && Text(dismountProof["identity"]["targetId"]) == rider && Text(dismountProof["window"]) == "combat-dismount", "exact Dismount proof missing");
            var pre = d["preDismount"]; var afterDismount = d["afterDismount"]; var dpre = dismountProof["preClick"];
            Require(Int(dpre["allocationSequence"]) == Int(pre["allocationSequence"]) && Int(dpre["frame"]) == Int(pre["frame"]) && Int(dpre["gameTicks"]) == Int(pre["gameTicks"]), "pre-Dismount boundary is not the exact pre-click");
            foreach (var role in new[] { "rider", "mount" }) foreach (var field in new[] { "standard", "move", "swift" })
                Require(Near(Num(dpre["state"][role][field]), Num(Resources(pre, role)[field])), "pre-Dismount " + role + " debt differs: " + field);
            var terminal = dismountProof["samples"].OfType<JObject>().Single(s => Text(s["boundary"]) == "terminal");
            foreach (var field in new[] { "standard", "move", "swift" })
                Require(Near(Num(terminal["state"]["mount"][field]), Num(dpre["state"]["mount"][field])), "Dismount changed mount debt: " + field);
            Require(Near(Num(terminal["state"]["rider"]["standard"]), Num(dpre["state"]["rider"]["standard"])) && Near(Num(terminal["state"]["rider"]["swift"]), Num(dpre["state"]["rider"]["swift"])) &&
                Near(Num(terminal["state"]["rider"]["move"]), Num(dpre["state"]["rider"]["move"]) + 3), "Dismount charged other than exactly one rider Move");
            Require(Int(terminal["state"]["rider"]["nativePrepareCount"]) == Int(dpre["state"]["rider"]["nativePrepareCount"]) &&
                Int(terminal["state"]["mount"]["nativePrepareCount"]) == Int(dpre["state"]["mount"]["nativePrepareCount"]), "Dismount repeated a native preparation");
            Require(Text(pre["currentActor"]) == rider && Text(pre["status"]) == "Acting" && Bool(pre["pairIdle"]) && Bool(pre["state"]?["rider"]?["hasMove"]), "Dismount was not requested on the idle acting rider with a lawful Move");
            Require(Text(pre["state"]["relationshipState"]) == "Mounted" && Text(afterDismount["state"]["relationshipState"]) == "Unmounted" &&
                Bool(afterDismount["pairedSplit"]) && Int(afterDismount["state"]["generation"]) == Int(pre["state"]["generation"]) &&
                Int(afterDismount["turnObject"]) == Int(pre["turnObject"]) && Int(afterDismount["round"]) == Int(pre["round"]), "Dismount did not split the activation on the same allocation");
            Require(Int(Resources(afterDismount, "mount")["grantSequence"]) == Int(Resources(pre, "mount")["grantSequence"]) &&
                Int(Resources(afterDismount, "rider")["grantSequence"]) == Int(Resources(pre, "rider")["grantSequence"]), "Dismount granted a preparation");
            var mounted = order["mounted"];
            if (v.Dismount == "immediate")
            {
                Require(Int(pre["turnObject"]) == Int(mounted["turnObject"]) && Int(pre["round"]) == Int(mounted["round"]) && Int(pre["pairedSequence"]) == 1, "immediate Dismount is not on the Mount's own allocation");
                Require(Near(Num(Resources(pre, "rider")["move"]), 3), "immediate Dismount baseline is not exactly the Mount's one Move");
                var mountTerminal = mountProof["samples"].OfType<JObject>().Single(s => Text(s["boundary"]) == "terminal");
                var bridge = (JObject)d["mountTerminalBridge"]; NativePassiveResourceEvidence.AssertComplete(bridge);
                foreach (var key in new[] { "frame", "gameTicks", "allocationSequence" })
                    Require(Int(bridge["before"][key]) == Int(mountTerminal[key]) && Int(bridge["after"][key]) == Int(pre[key]), "immediate bridge clock differs");
                foreach (var role in new[] { "rider", "mount" })
                    Require(JToken.DeepEquals(bridge["before"][role], mountTerminal["nativeAllocation"][role]) && JToken.DeepEquals(bridge["after"][role], Resources(pre, role)), "immediate bridge resources differ");
            }
            else
            {
                var later = (JObject)d["laterTurn"]; Require(later != null && Text(later["contract"]) == "full-tb-later-native-turn-dismount" && Text(later["riderId"]) == rider && Text(later["mountId"]) == mount, "later-turn evidence missing");
                var next = later["nextRound"]; var b = later["beforeEndInput"];
                Require(Int(b["turnObject"]) == Int(mounted["turnObject"]) && Int(next["round"]) == Int(mounted["round"]) + 1 && Int(next["turnObject"]) != Int(mounted["turnObject"]) &&
                    Text(next["currentActor"]) == rider && Int(next["pairedSequence"]) == 2 && Text(next["partnerActor"]) == mount && Int(next["partnerContextObject"]) != 0 && !Bool(next["pairedSplit"]), "later Dismount is not on the exact next paired allocation");
                var continuation = (JObject)later["continuation"]; NativeAllocationContinuationEvidence.AssertComplete(continuation);
                Require(Int(continuation["before"]["allocationSequence"]) == Int(b["allocationSequence"]) && Int(continuation["after"]["allocationSequence"]) == Int(next["allocationSequence"]), "continuation interval differs");
                Require(Int(pre["turnObject"]) == Int(next["turnObject"]) && Int(pre["round"]) == Int(next["round"]), "Dismount is not on the next rider allocation");
                JToken expenditureBefore, expenditureAfter; JObject startBridge;
                if (v.Dismount == "later-after-mount")
                {
                    var ground = (JObject)later["groundSetup"]; NativeDismountGroundEvidence.AssertComplete(ground, rider, mount);
                    Require(Int(ground["before"]["turnObject"]) == Int(next["turnObject"]), "mount expenditure is not on the next rider allocation");
                    Require(Num(Resources(ground["after"], "mount")["move"]) > Num(Resources(ground["before"], "mount")["move"]), "mount spent no movement");
                    expenditureBefore = ground["before"]; expenditureAfter = ground["after"]; startBridge = (JObject)later["groundStartBridge"];
                }
                else
                {
                    var attack = (JObject)later["riderAttack"]; Require(attack != null, "rider expenditure missing");
                    AssertActorCommand(attack, rider, mount, "single-attack", "rider expenditure");
                    Require(Int(attack["before"]["turnObject"]) == Int(next["turnObject"]) && Text(attack["before"]["status"]) == "Preparing" && Text(attack["after"]["status"]) == "Acting" && Bool(attack["weapon"]?["ranged"]), "rider expenditure is not the next-turn Standard action");
                    Require(Bool(attack["spent"]["usedStandardAction"]) && Bool(attack["spent"]["hasMoveAction"]), "rider expenditure left no lawful Move");
                    expenditureBefore = attack["before"]; expenditureAfter = attack["after"]; startBridge = (JObject)later["attackStartBridge"];
                }
                // The next paired allocation's resources are passive from its boundary to the expenditure's own pre-command boundary.
                Require(startBridge != null, "expenditure start bridge missing"); NativePassiveResourceEvidence.AssertComplete(startBridge);
                foreach (var key in new[] { "frame", "gameTicks", "allocationSequence" })
                    Require(Int(startBridge["before"][key]) == Int(next[key]) && Int(startBridge["after"][key]) == Int(expenditureBefore[key]), "start bridge clock differs");
                foreach (var role in new[] { "rider", "mount" })
                    Require(JToken.DeepEquals(startBridge["before"][role], Resources(next, role)) && JToken.DeepEquals(startBridge["after"][role], Resources(expenditureBefore, role)), "start bridge resources differ");
                var ready = (JObject)later["readyBridge"]; NativePassiveResourceEvidence.AssertComplete(ready);
                foreach (var key in new[] { "frame", "gameTicks", "allocationSequence" })
                    Require(Int(ready["before"][key]) == Int(expenditureAfter[key]) && Int(ready["after"][key]) == Int(pre[key]), "ready bridge clock differs");
                foreach (var role in new[] { "rider", "mount" })
                    Require(JToken.DeepEquals(ready["before"][role], Resources(expenditureAfter, role)) && JToken.DeepEquals(ready["after"][role], Resources(pre, role)), "ready bridge resources differ");
                foreach (var field in new[] { "standard", "move", "swift" })
                    Require(Near(Num(Resources(pre, "rider")[field]), Num(Resources(expenditureAfter, "rider")[field])) && Near(Num(Resources(pre, "mount")[field]), Num(Resources(expenditureAfter, "mount")[field])), "expenditure debt changed before the Dismount: " + field);
                Require(Bool(pre["state"]?["rider"]?["hasMove"]), "rider had no Move for the later Dismount");
            }
            Release(e, rider, mount, afterDismount);
        }

        private static void Release(JObject e, string rider, string mount, JToken afterDismount)
        {
            var r = (JObject)e["release"]; Require(r != null, "release observation missing");
            var round = Int(afterDismount["round"]); Require(Int(r["dismountRound"]) == round, "release round differs");
            var end = r["endInput"];
            Require(Text(end?["method"]) == "Kingmaker.Game.PauseBind" && Text(end["token"]) == "06000CB7" && Int(end["count"]) == 1 &&
                Int(end["beforeEndInput"]["turnObject"]) == Int(afterDismount["turnObject"]) && Text(end["beforeEndInput"]["currentActor"]) == rider, "dismounted rider did not end through one native End input");
            var turns = (JArray)r["turns"]; Require(turns != null && turns.Count > 0 && turns.Count <= 128, "release turn observations missing");
            Require(!turns.Any(t => Text(t["currentActor"]) == mount && Int(t["round"]) == round), "the mount received a duplicate native turn in the release round");
            var mountTurn = (JObject)r["mountTurn"];
            Require(mountTurn != null && Text(mountTurn["currentActor"]) == mount && Int(mountTurn["round"]) == round + 1 && Bool(mountTurn["turnBased"]) &&
                Text(mountTurn["state"]["relationshipState"]) == "Unmounted" && Int(mountTurn["state"]["generation"]) == Int(afterDismount["state"]["generation"]), "the mount's separate participation did not resume in the following round");
            Require(Int(Resources(mountTurn, "mount")["grantSequence"]) == Int(Resources(afterDismount, "mount")["grantSequence"]) + 1 &&
                Int(Resources(mountTurn, "rider")["grantSequence"]) == Int(Resources(afterDismount, "rider")["grantSequence"]) + 1, "next-round preparations differ from exactly once each");
            Require(Bool(r["traceComplete"]), "release trace incomplete");
            var events = Events(r); long sequence = Int(afterDismount["allocationSequence"]);
            var mountPrepares = 0; var riderPrepares = 0;
            foreach (var x in events)
            {
                Require(Int(x["sequence"]) == ++sequence, "release trace gap");
                var who = Text(x["state"]?["actor"]); var boundary = Text(x["boundary"]) ?? "";
                if (who == mount)
                {
                    Require(Int(x["round"]) != round || !(boundary.Contains("cost") || boundary.Contains("prepare") || boundary.Contains("turn-end")), "the mount acted, prepared or ended in the release round after the Dismount");
                    if (boundary == "prepare-before") { Require(Int(x["round"]) == round + 1, "mount prepared outside the following round"); mountPrepares++; }
                }
                if (who == rider && boundary == "prepare-before") { Require(Int(x["round"]) == round + 1, "rider prepared outside the following round"); riderPrepares++; }
            }
            Require(sequence == Int(mountTurn["allocationSequence"]) && mountPrepares == 1 && riderPrepares == 1, "release observation did not close on one separate preparation each");
        }

        internal static void AssertComplete(JObject e, JObject order, JObject mountProof, JObject dismountProof)
        {
            Require(Text(e?["contract"]) == Contract, "contract differs");
            var v = VariantOf(Text(e["variant"])); Require(v != null && Text(e["scenario"]) == v.Scenario && Text(e["row"]) == v.Row, "variant, scenario or row differs");
            var rider = Text(e["riderId"]); var mount = Text(e["mountId"]);
            Require(!string.IsNullOrEmpty(rider) && !string.IsNullOrEmpty(mount) && rider != mount, "pair identity missing");
            var auto = e["automaticEnd"]; Require(Bool(auto?["leased"]) && !Bool(auto["temporaryValue"]), "automatic End preference was not leased false");
            Require(order != null && Text(order["riderId"]) == rider && Text(order["mountId"]) == mount && Bool(order["riderFirst"]) == v.RiderFirst && Text(order["scenario"]) == v.Scenario, "order fixture binding differs");
            NativeMountOrderEvidence.AssertFixture(order, v.RiderFirst);
            if (v.Mounts)
            {
                Require(mountProof != null && Bool(mountProof["pass"]) && Text(mountProof["identity"]["abilityGuid"]) == MountAbilityGuid && Text(mountProof["identity"]["casterId"]) == rider &&
                    Text(mountProof["identity"]["targetId"]) == mount && Text(mountProof["window"]) == "positive-mount", "exact Mount proof missing");
                Require(Text(order["disposition"]) == (v.RiderFirst ? "PreparePartnerThisRound" : "RetainPartnerParticipation"), "adoption disposition differs from the declared order");
            }
            if (v.OrderCompletion) NativeMountOrderEvidence.AssertCore(order, v.Scenario, v.RiderFirst);
            RiderEntry(e, v, v.Mounts ? mountProof : null);
            if (v.MountSlot != "none") Retention(e, MountSlotTurn(e, v, order), mountProof);
            if (v.RiderExhaust) Refusal(e);
            if (v.Unrelated) Unrelated(e, order);
            if (v.Dismounts) Dismount(e, v, order, mountProof, dismountProof);
            if (v.AdjacentMount)
            {
                Require(Bool(mountProof["preClick"]["state"]["geometry"]["isAdjacent"]) && !mountProof["resourceWindow"]["events"].Any(x => (Text(x["boundary"]) ?? "").StartsWith("approach-movement", StringComparison.Ordinal)), "adjacent Mount moved");
                var terminal = mountProof["samples"].OfType<JObject>().Single(s => Text(s["boundary"]) == "terminal");
                Require(Near(Num(terminal["state"]["rider"]["move"]), Num(mountProof["preClick"]["state"]["rider"]["move"]) + 3), "adjacent Mount charged other than exactly one Move");
            }
        }

        internal static void AssertRestoration(JObject e)
        {
            var auto = e?["automaticEnd"]; Require(Bool(auto?["leased"]) && Bool(auto["restored"]), "automatic End preference was not restored exactly");
            if (e["unrelated"] is JObject u) Require(Bool(u["restoration"]?["exact"]) && Bool(u["restoration"]["outsideCombat"]) && Int(u["restoration"]["base"]) == Int(u["originalBase"]), "unrelated initiative input not restored exactly");
        }
    }
}
