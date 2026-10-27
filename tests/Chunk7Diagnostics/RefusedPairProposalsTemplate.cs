using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    internal static class ActualRefusedPairProposals
    {
        private const string StockAttackType = "Kingmaker.UnitLogic.Commands.UnitAttack";
/*ACTUAL_METHOD*/
/*ACTUAL_OPPORTUNITY*/
    }

    internal static class RefusedPairProposalsRegression
    {
        private const string RiderA = "73681ef2-0000-0000-0000-000000000001", HorseA = "e3a3be8b-0000-0000-0000-000000000002";
        private const string RiderB = "fd213210-0000-0000-0000-000000000003", Companion = "5dd7bd1d-0000-0000-0000-000000000004";
        private static int sequence, failures;

        // Built field by field as NativeActorAllocationTrace builds it: a refused-before-Run UnitAttack
        // has no executor, so commandActor is assigned a null string (a String-typed JValue).
        private static JObject Row(string boundary, string actor, int command, string executor, bool started = false, string result = "None",
            string type = "Kingmaker.UnitLogic.Commands.UnitAttack")
        {
            string commandType = type;
            return new JObject {
                ["sequence"] = ++sequence, ["boundary"] = boundary, ["state"] = new JObject { ["actor"] = actor },
                ["command"] = command, ["commandType"] = commandType, ["commandActor"] = executor,
                ["started"] = (bool?)started, ["acted"] = (bool?)false, ["finished"] = (bool?)false, ["result"] = result };
        }

        // Shaped as CompanionWindow's pairCommands (actor id -> slot array) and transformed exactly as the call site does.
        private static JToken[] Slots(params int[] identities)
        {
            var pairCommands = new JObject { [RiderA] = new JArray(identities.Select(i => new JObject { ["identity"] = i })), [HorseA] = new JArray(), [RiderB] = new JArray() };
            return new JToken[] { pairCommands, pairCommands, pairCommands }
                .Select(x => (JToken)new JArray(((JObject)x).Properties().Select(prop => prop.Value))).ToArray();
        }

        private static void Expect(string name, JObject[] rows, int companionCommand, JToken[] slots, int refused, int touched)
        {
            var actual = ActualRefusedPairProposals.RefusedPairProposals(rows, new[] { RiderA, HorseA, RiderB }, companionCommand, slots, out var pairTouched);
            var ok = actual.Count == refused && pairTouched == touched;
            Console.WriteLine((ok ? "PASS " : "FAIL ") + name + " refused=" + actual.Count + " pairTouched=" + pairTouched);
            if (!ok) failures++;
        }

        private static int Main(string[] args)
        {
            var nullExecutor = Row("admission-before", RiderA, 1, null)["commandActor"];
            Console.WriteLine((nullExecutor.Type == JTokenType.String ? "PASS " : "FAIL ") + "trace null executor is a String-typed JValue: " + nullExecutor.Type);
            if (nullExecutor.Type != JTokenType.String) failures++;
            const int companionAttack = 900;
            Expect("closed refused proposal", new[] { Row("admission-before", RiderA, 10, null), Row("admission-after", RiderA, 10, null) },
                companionAttack, Slots(), 1, 0);
            Expect("two refused proposals by two pair actors", new[] { Row("admission-before", RiderA, 11, null), Row("admission-after", RiderA, 11, null),
                Row("admission-before", RiderB, 12, null), Row("admission-after", RiderB, 12, null) }, companionAttack, Slots(), 2, 0);
            Expect("admitted pair command (has an executor)", new[] { Row("admission-before", RiderA, 13, RiderA), Row("admission-after", RiderA, 13, RiderA) },
                companionAttack, Slots(), 0, 2);
            Expect("started pair command", new[] { Row("admission-before", RiderA, 14, null, true), Row("admission-after", RiderA, 14, null, true) },
                companionAttack, Slots(), 0, 2);
            Expect("pair command in a slot", new[] { Row("admission-before", RiderA, 15, null), Row("admission-after", RiderA, 15, null) },
                companionAttack, Slots(15), 0, 2);
            Expect("third row of the same identity", new[] { Row("admission-before", RiderA, 16, null), Row("admission-after", RiderA, 16, null),
                Row("cost-before", RiderA, 16, null) }, companionAttack, Slots(), 0, 3);
            Expect("identity shared with a companion row", new[] { Row("admission-before", RiderA, 17, null), Row("admission-after", RiderA, 17, null),
                Row("cost-before", Companion, 17, Companion) }, companionAttack, Slots(), 0, 2);
            Expect("the companion's own command identity", new[] { Row("admission-before", RiderA, companionAttack, null), Row("admission-after", RiderA, companionAttack, null) },
                companionAttack, Slots(), 0, 2);
            Expect("not a stock attack", new[] { Row("admission-before", RiderA, 18, null, type: "Kingmaker.UnitLogic.Commands.UnitMoveTo"),
                Row("admission-after", RiderA, 18, null, type: "Kingmaker.UnitLogic.Commands.UnitMoveTo") }, companionAttack, Slots(), 0, 2);
            Expect("non-None result", new[] { Row("admission-before", RiderA, 19, null), Row("admission-after", RiderA, 19, null, result: "Success") },
                companionAttack, Slots(), 0, 2);
            Expect("after before before", new[] { Row("admission-after", RiderA, 20, null), Row("admission-before", RiderA, 20, null) },
                companionAttack, Slots(), 0, 2);
            var acted = Row("admission-after", RiderA, 21, null); acted["acted"] = (bool?)true;
            Expect("acted pair command", new[] { Row("admission-before", RiderA, 21, null), acted }, companionAttack, Slots(), 0, 2);
            var finished = Row("admission-after", RiderA, 22, null); finished["finished"] = (bool?)true;
            Expect("finished pair command", new[] { Row("admission-before", RiderA, 22, null), finished }, companionAttack, Slots(), 0, 2);
            var noExecutorProperty = Row("admission-after", RiderA, 23, null); noExecutorProperty.Remove("commandActor");
            Expect("missing commandActor property", new[] { Row("admission-before", RiderA, 23, null), noExecutorProperty }, companionAttack, Slots(), 0, 2);
            Expect("two rows naming different pair actors", new[] { Row("admission-before", RiderA, 24, null), Row("admission-after", RiderB, 24, null) },
                companionAttack, Slots(), 0, 2);
            Expect("companion-only rows", new[] { Row("admission-before", Companion, companionAttack, Companion), Row("admission-after", Companion, companionAttack, Companion) },
                companionAttack, Slots(), 0, 0);
            // F2 r09 native engagement-controller attack of opportunity by an engaged pair actor (shape of the r09 trace rows).
            const string Target = "5b50d370-0000-0000-0000-000000000005";
            const string Callers = "callers=MonoMod.Utils.DynamicMethodDefinition.Kingmaker.UnitLogic.Commands.UnitCommands.Run_Patch3 < MonoMod.Utils.DynamicMethodDefinition.Kingmaker.Controllers.Combat.UnitCombatState.AttackOfOpportunity_Patch3 < Kingmaker.Controllers.Combat.UnitCombatEngagementController+<>c__DisplayClass4_0.<Tick>b__0 < Kingmaker.Utility.LinqExtensions.ForEach < Kingmaker.Controllers.Combat.UnitCombatEngagementController.Tick < Kingmaker.GameModes.GameMode.Tick < Kingmaker.Game.Tick < Kingmaker.Runner.Update";
            Func<string, int, string, int, int, double, JObject> aooRow = (boundary, command, executor, reactions, unused, standard) => {
                var row = Row(boundary, HorseA, command, executor, boundary.StartsWith("cost"), boundary.StartsWith("cost") ? "Success" : "None", ActualRefusedPairProposals.OpportunityAttackType);
                row["acted"] = (bool?)boundary.StartsWith("cost"); row["finished"] = (bool?)boundary.StartsWith("cost"); row["actionType"] = "Free"; row["commandTarget"] = Target;
                row["detail"] = boundary == "admission-before" ? Callers : null;
                row["state"] = new JObject { ["actor"] = HorseA, ["reactions"] = reactions, ["standard"] = standard, ["move"] = 0.0, ["swift"] = 0.0, ["reactionCooldown"] = boundary.StartsWith("cost") ? 5.4 : 0.0 };
                row["gameTicks"] = 1000000000L; return row; };
            Action<string, bool> check = (name, ok) => { Console.WriteLine((ok ? "PASS " : "FAIL ") + name); if (!ok) failures++; };
            Func<int, JObject[]> aoo = command => new[] { aooRow("admission-before", command, null, 1, 0, 2.88), aooRow("admission-after", command, HorseA, 1, 0, 2.88),
                aooRow("cost-before", command, HorseA, 0, 0, 2.88), aooRow("cost-after", command, HorseA, 0, 0, 2.88) };
            Action<string, JObject[], string, int, int> expectAoo = (name, rows, provoker, accounted, touched) => {
                var found = ActualRefusedPairProposals.NativeOpportunityAttacks(rows, new[] { RiderA, HorseA, RiderB }, provoker, Slots(), new[] { HorseA }, out var seen);
                ActualRefusedPairProposals.RefusedPairProposals(rows.Where(e => !seen.Contains((int)e["sequence"])).ToArray(), new[] { RiderA, HorseA, RiderB }, companionAttack, Slots(), out var pairTouched);
                var ok = found.Count == accounted && pairTouched == touched;
                Console.WriteLine((ok ? "PASS " : "FAIL ") + name + " opportunity=" + found.Count + " pairTouched=" + pairTouched); if (!ok) failures++; };
            expectAoo("native engagement attack of opportunity on the provoking target is accounted", aoo(30), Target, 1, 0);
            expectAoo("an attack of opportunity on another unit stays a pair touch", aoo(31), RiderB, 0, 4);
            var kmcCaller = aoo(32); kmcCaller[0]["detail"] = Callers.Replace("Kingmaker.Runner.Update", "KingmakerMountedCombat.Integration.X.Y");
            expectAoo("a KingmakerMountedCombat frame in the caller chain stays a pair touch", kmcCaller, Target, 0, 4);
            var otherPath = aoo(33); otherPath[0]["detail"] = "callers=Kingmaker.UnitLogic.Commands.UnitCommands.Run < Kingmaker.UI.Selection.ClickUnitHandler.OnClick";
            expectAoo("a stock attack-of-opportunity type outside the native path stays a pair touch", otherPath, Target, 0, 4);
            var noReaction = aoo(34); foreach (var r in noReaction) r["state"]["reactions"] = 0;
            expectAoo("an attack of opportunity without a reaction to spend stays a pair touch", noReaction, Target, 0, 4);
            var notSpent = aoo(35); notSpent[3]["state"]["reactions"] = 1;
            expectAoo("an attack of opportunity that spent no reaction stays a pair touch", notSpent, Target, 0, 4);
            var standardRise = aoo(36); standardRise[3]["state"]["standard"] = 6.0;
            expectAoo("an attack of opportunity that raised Standard stays a pair touch", standardRise, Target, 0, 4);
            expectAoo("a fifth row of the same identity stays a pair touch", aoo(37).Concat(new[] { aooRow("command-interrupt-before", 37, HorseA, 0, 0, 2.88) }).ToArray(), Target, 0, 5);
            var unfinished = aoo(38); unfinished[3]["finished"] = (bool?)false;
            expectAoo("an unfinished attack of opportunity stays a pair touch", unfinished, Target, 0, 4);
            expectAoo("two native attacks of opportunity are both accounted", aoo(39).Concat(aoo(40)).ToArray(), Target, 2, 0);
            var truncated = aoo(41); truncated[0]["detail"] = Callers.Replace(" < Kingmaker.Runner.Update", "");
            expectAoo("a caller chain that does not reach the frame loop (possibly truncated) stays a pair touch", truncated, Target, 0, 4);
            var notFree = aoo(42); foreach (var r in notFree) r["actionType"] = "Standard";
            expectAoo("a non-Free attack of opportunity stays a pair touch", notFree, Target, 0, 4);
            var mismatched = aoo(43); mismatched[1]["commandActor"] = RiderA;
            expectAoo("a mismatched command actor stays a pair touch", mismatched, Target, 0, 4);
            var notEngine = aoo(44); notEngine[0]["detail"] = Callers.Replace("Kingmaker.Controllers.Combat.UnitCombatEngagementController+<>c__DisplayClass4_0.<Tick>b__0", "Kingmaker.Controllers.Combat.UnitCombatState.ShouldAttackOnDisengage");
            expectAoo("a chain whose third frame is not the engagement controller stays a pair touch", notEngine, Target, 0, 4);
            var failed = aoo(45); failed[3]["result"] = "Fail";
            expectAoo("a cost row without native Success stays a pair touch", failed, Target, 0, 4);
            {   var found = ActualRefusedPairProposals.NativeOpportunityAttacks(aoo(46), new[] { RiderA, HorseA, RiderB }, Target, Slots(46), new[] { HorseA }, out var seen);
                check("an attack of opportunity held in a pair command slot stays unaccounted", found.Count == 0); }
            {   var found = ActualRefusedPairProposals.NativeOpportunityAttacks(aoo(47), new[] { RiderA, HorseA, RiderB }, Target, Slots(), new string[0], out var seen);
                check("an attack of opportunity by a pair actor not engaging the target before the window stays unaccounted", found.Count == 0); }
            {   var found = ActualRefusedPairProposals.NativeOpportunityAttacks(aoo(48), new[] { RiderA, RiderB }, Target, Slots(), new[] { HorseA }, out var seen);
                check("an attack of opportunity by a non-pair actor is not accounted", found.Count == 0); }
            var before = new JObject { ["reactions"] = 1, ["reactionCooldown"] = 0.0 };
            Func<int, double, JObject> after = (r, c) => new JObject { ["reactions"] = r, ["reactionCooldown"] = c };
            var last = new JObject { ["costGameTicks"] = 1000000000L, ["costReactionCooldown"] = 5.4 };
            check("one attack of opportunity spends exactly one reaction", ActualRefusedPairProposals.ReactionsAccounted(before, after(0, 0.49), 1, 1000000000L, 1049000000L, last, false));
            check("a reaction natively restored after its full cooldown elapsed is admitted", ActualRefusedPairProposals.ReactionsAccounted(before, after(1, 0.0), 1, 1000000000L, 1054000000L, last, false));
            check("a reaction restored before its cooldown elapsed is refused", !ActualRefusedPairProposals.ReactionsAccounted(before, after(1, 0.0), 1, 1000000000L, 1030000000L, last, false));
            check("a reaction restored next to a prepare or clear of the actor is refused", !ActualRefusedPairProposals.ReactionsAccounted(before, after(1, 0.0), 1, 1000000000L, 1054000000L, last, true));
            check("a reaction spent without an attack of opportunity is refused", !ActualRefusedPairProposals.ReactionsAccounted(before, after(0, 0.49), 0, 1000000000L, 1049000000L, null, false));
            check("an unchanged reaction count with cooldown left is refused", !ActualRefusedPairProposals.ReactionsAccounted(before, after(1, 3.0), 1, 1000000000L, 1054000000L, last, false));

            // F6 r09: exact numeric shape of the pre-window AoO cooldown crossing zero during the player click.
            // Feed the same cases to the compiled observer predicate and the external reader predicate.
            var renewalCases = new JArray();
            Func<int, double, JObject> snapshot = (count, cooldown) => new JObject {
                ["reactions"] = count, ["reactionCooldown"] = cooldown,
                ["native"] = new JObject { ["reactions"] = count, ["reactionCooldown"] = cooldown,
                    ["reactionsPerRound"] = 1, ["grantSequence"] = 0L } };
            Action<string, bool, Action<JObject>> renewal = (name, expected, mutate) => {
                var test = new JObject { ["name"] = name, ["expected"] = expected,
                    ["before"] = snapshot(0, .82590276), ["after"] = snapshot(1, 0),
                    ["beforeTicks"] = 2245250220000L, ["afterTicks"] = 2245266430000L, ["resetSeen"] = false };
                mutate?.Invoke(test); renewalCases.Add(test);
                check(name, ActualRefusedPairProposals.ReactionsAccounted((JObject)test["before"], (JObject)test["after"], 0,
                    (long)test["beforeTicks"], (long)test["afterTicks"], null, (bool)test["resetSeen"]) == expected);
            };
            renewal("carried native cooldown expires during F6 r09 click", true, null);
            renewal("unchanged positive capacity greater than one", true, t => {
                t["before"]["reactions"] = t["before"]["native"]["reactions"] = 1;
                t["after"]["reactions"] = t["after"]["native"]["reactions"] = 2;
                t["before"]["native"]["reactionsPerRound"] = t["after"]["native"]["reactionsPerRound"] = 2; });
            renewal("carried reaction renews too early", false, t => t["afterTicks"] = 2245255220000L);
            renewal("existing tenth-second timing slack remains bounded", true, t => t["afterTicks"] = 2245257720000L);
            renewal("backward native game time", false, t => t["afterTicks"] = 2245250219999L);
            renewal("negative native game time", false, t => t["beforeTicks"] = -1L);
            renewal("capacity changes during renewal", false, t => t["after"]["native"]["reactionsPerRound"] = 2);
            renewal("zero native reaction capacity", false, t => t["before"]["native"]["reactionsPerRound"] = 0);
            renewal("out-of-range native reaction capacity", false, t => t["before"]["native"]["reactionsPerRound"] = 2147483648L);
            renewal("renewal exceeds native capacity", false, t => t["after"]["reactions"] = t["after"]["native"]["reactions"] = 2);
            renewal("negative starting reaction count", false, t => t["before"]["reactions"] = t["before"]["native"]["reactions"] = -1);
            renewal("native count disagrees before renewal", false, t => t["before"]["native"]["reactions"] = 1);
            renewal("native count disagrees after renewal", false, t => t["after"]["native"]["reactions"] = 0);
            renewal("a grant intervenes", false, t => t["after"]["native"]["grantSequence"] = 1L);
            renewal("negative grant sequence", false, t => t["before"]["native"]["grantSequence"] = t["after"]["native"]["grantSequence"] = -1L);
            renewal("missing native snapshot", false, t => ((JObject)t["before"]).Remove("native"));
            renewal("missing grant observation", false, t => ((JObject)t["after"]["native"]).Remove("grantSequence"));
            renewal("capacity is not an integer observation", false, t => t["before"]["native"]["reactionsPerRound"] = "1");
            renewal("no positive carried cooldown", false, t => t["before"]["reactionCooldown"] = t["before"]["native"]["reactionCooldown"] = 0.0);
            renewal("cooldown remains after renewal", false, t => t["after"]["reactionCooldown"] = t["after"]["native"]["reactionCooldown"] = .1);
            renewal("native cooldown disagrees before renewal", false, t => t["before"]["native"]["reactionCooldown"] = .5);
            renewal("native cooldown disagrees after renewal", false, t => t["after"]["native"]["reactionCooldown"] = .0005);
            renewal("negative cooldown", false, t => t["after"]["reactionCooldown"] = t["after"]["native"]["reactionCooldown"] = -.1);
            renewal("nonfinite cooldown", false, t => t["before"]["reactionCooldown"] = double.NaN);
            renewal("infinite cooldown", false, t => t["before"]["native"]["reactionCooldown"] = double.PositiveInfinity);
            foreach (var boundary in ActualRefusedPairProposals.ReactionResetBoundaries)
                renewal("carried renewal next to " + boundary, false, t => t["resetSeen"] = ActualRefusedPairProposals.ReactionResetSeen(
                    new[] { new JObject { ["boundary"] = boundary, ["state"] = new JObject { ["actor"] = HorseA } } }, HorseA));
            if (args.Length == 1) System.IO.File.WriteAllText(args[0], renewalCases.ToString());
            Console.WriteLine("TOTAL failures=" + failures);
            return failures == 0 ? 0 : 1;
        }
    }
}
