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

        private static int Main()
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
            Expect("companion-only rows", new[] { Row("admission-before", Companion, companionAttack, Companion), Row("admission-after", Companion, companionAttack, Companion) },
                companionAttack, Slots(), 0, 0);
            Console.WriteLine("TOTAL failures=" + failures);
            return failures == 0 ? 0 : 1;
        }
    }
}
