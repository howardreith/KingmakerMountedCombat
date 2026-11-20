using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerMountedCombat.Diagnostics;

namespace KingmakerMountedCombat.Tests
{
    // T02: the producer's repeated-world subscriber check follows one reviewed revision. These rows are synthetic;
    // scripts/chunk7/Test-Chunk7ReplaySubscriberContract.ps1 compares the revision with the compiled subscribers.
    internal static class Chunk7ReplaySubscriberContractTests
    {
        private const string Lifecycle = "KingmakerMountedCombat.Integration.MountedLifecycleSubscriber";
        private const string Controls = "KingmakerMountedCombat.Integration.NativeMountedControlService";

        // A valid inventory: process-owned subscriber n is instance n + 1, Horse facts follow.
        private static List<Chunk7ReplaySubscriberRow> Valid(int horseFacts)
        {
            var rows = new List<Chunk7ReplaySubscriberRow>();
            for (var owner = 0; owner < Chunk7ReplaySubscriberContract.ProcessOwned.Count; owner++)
                foreach (var name in Chunk7ReplaySubscriberContract.ProcessOwned[owner].Value)
                    rows.Add(new Chunk7ReplaySubscriberRow(name, Chunk7ReplaySubscriberContract.ProcessOwned[owner].Key, owner + 1));
            for (var fact = 0; fact < horseFacts; fact++)
                rows.Add(new Chunk7ReplaySubscriberRow(Chunk7ReplaySubscriberContract.HorseFactInterface,
                    Chunk7ReplaySubscriberContract.HorseFactType, 100 + fact));
            return rows;
        }

        private static void Refused(string name, int horseFacts, Action<List<Chunk7ReplaySubscriberRow>> change, string expected)
        {
            var rows = Valid(horseFacts); change(rows);
            var violation = Chunk7ReplaySubscriberContract.Violation(rows, horseFacts);
            TestRunner.True(violation != null && violation.IndexOf(expected, StringComparison.Ordinal) >= 0,
                name + " was not refused for its own reason: " + (violation ?? "<accepted>"));
        }

        private static int IndexOf(List<Chunk7ReplaySubscriberRow> rows, string type, string interfaceSuffix) =>
            rows.FindIndex(row => row.Type == type && row.Interface.EndsWith("." + interfaceSuffix, StringComparison.Ordinal));

        internal static void Register(TestRunner runner)
        {
            runner.Run("replay subscriber contract: the reviewed revision is r2 with four process-owned subscribers and 21 entries", () =>
            {
                TestRunner.True(Chunk7ReplaySubscriberContract.Revision == "kmc-global-subscribers-r2", "Revision name changed without review.");
                TestRunner.True(Chunk7ReplaySubscriberContract.ProcessOwned.Count == 4 && Chunk7ReplaySubscriberContract.ProcessOwnedEntries == 21,
                    "Reviewed process-owned membership changed without a new revision.");
                var lifecycle = Chunk7ReplaySubscriberContract.ProcessOwned.Single(owner => owner.Key == Lifecycle).Value;
                TestRunner.True(lifecycle.Length == 15 && lifecycle.Distinct(StringComparer.Ordinal).Count() == 15 &&
                    lifecycle.Contains("Kingmaker.PubSubSystem.IUnitFallProneHandler") && lifecycle.Contains("Kingmaker.PubSubSystem.ITeleportHandler"),
                    "Lifecycle membership lost the F6 fall or the same-area teleport interface.");
                TestRunner.True(Chunk7ReplaySubscriberContract.ProcessOwned.All(owner => owner.Value.Distinct(StringComparer.Ordinal).Count() == owner.Value.Length) &&
                    Chunk7ReplaySubscriberContract.ProcessOwned.Select(owner => owner.Key).Distinct(StringComparer.Ordinal).Count() == 4,
                    "A reviewed subscriber or interface is declared twice.");
            });
            runner.Run("replay subscriber contract: exact inventories beside one and two Horse facts are accepted", () =>
            {
                TestRunner.True(Chunk7ReplaySubscriberContract.Violation(Valid(1), 1) == null, "One Horse owner refused.");
                TestRunner.True(Chunk7ReplaySubscriberContract.Violation(Valid(2), 2) == null, "Two Horse owners refused.");
                var shuffled = Valid(2); shuffled.Reverse();
                TestRunner.True(Chunk7ReplaySubscriberContract.Violation(shuffled, 2) == null, "Row order changed the verdict.");
            });
            runner.Run("replay subscriber contract: missing, duplicate and foreign handlers are each refused", () =>
            {
                Refused("missing lifecycle interface", 1, rows => rows.RemoveAt(IndexOf(rows, Lifecycle, "ITeleportHandler")), "missing [Kingmaker.PubSubSystem.ITeleportHandler]");
                Refused("the historical 13-interface lifecycle", 1, rows => { rows.RemoveAt(IndexOf(rows, Lifecycle, "ITeleportHandler")); rows.RemoveAt(IndexOf(rows, Lifecycle, "IUnitFallProneHandler")); }, "lists 13 of 15");
                Refused("duplicate lifecycle interface", 1, rows => rows.Add(rows[IndexOf(rows, Lifecycle, "ISceneHandler")]), "lists 16 of 15");
                Refused("foreign interface on a reviewed subscriber", 1, rows => rows.Add(new Chunk7ReplaySubscriberRow("Kingmaker.PubSubSystem.IAttackHandler", Lifecycle, 1)), "unexpected [Kingmaker.PubSubSystem.IAttackHandler]");
                Refused("a foreign interface replacing a reviewed one at the same count", 1, rows =>
                {
                    var index = IndexOf(rows, Controls, "IClickActionHandler");
                    rows[index] = new Chunk7ReplaySubscriberRow("Kingmaker.PubSubSystem.IAttackHandler", Controls, rows[index].Subject);
                }, "missing [Kingmaker.PubSubSystem.IClickActionHandler]");
                Refused("unreviewed subscriber type", 1, rows => rows.Add(new Chunk7ReplaySubscriberRow("Kingmaker.PubSubSystem.IUnitGainLevelHandler", "KingmakerMountedCombat.Integration.DivineMountSpellResistance", 300)), "outside the reviewed revision");
                Refused("absent process-owned subscriber", 1, rows => rows.RemoveAll(row => row.Type.EndsWith(".RidingDogTackService", StringComparison.Ordinal)), "RidingDogTackService lists 0 of 2");
            });
            runner.Run("replay subscriber contract: process ownership is one distinct instance per subscriber", () =>
            {
                Refused("two lifecycle instances", 1, rows =>
                {
                    var index = IndexOf(rows, Lifecycle, "IPartyHandler");
                    rows[index] = new Chunk7ReplaySubscriberRow(rows[index].Interface, Lifecycle, 77);
                }, "held by 2 instances");
                Refused("two process-owned subscribers under one instance", 1, rows =>
                {
                    for (var index = 0; index < rows.Count; index++)
                        if (rows[index].Type == Controls) rows[index] = new Chunk7ReplaySubscriberRow(rows[index].Interface, Controls, 1);
                }, "shares its instance with another process-owned subscriber");
                Refused("a Horse fact labelled as a process-owned instance", 1, rows =>
                    rows[rows.Count - 1] = new Chunk7ReplaySubscriberRow(Chunk7ReplaySubscriberContract.HorseFactInterface, Chunk7ReplaySubscriberContract.HorseFactType, 1),
                    "shares a process-owned instance");
            });
            runner.Run("replay subscriber contract: Horse facts follow the Horse owners exactly", () =>
            {
                TestRunner.True(Chunk7ReplaySubscriberContract.Violation(Valid(2), 1) != null && Chunk7ReplaySubscriberContract.Violation(Valid(1), 2) != null,
                    "A Horse fact count that differs from the Horse owners was accepted.");
                Refused("one Horse instance listed twice", 2, rows => rows[rows.Count - 1] = rows[rows.Count - 2], "two Horse fact rows name one subscriber instance");
                Refused("a Horse fact under another interface", 1, rows =>
                    rows[rows.Count - 1] = new Chunk7ReplaySubscriberRow("Kingmaker.PubSubSystem.IUnitHandler", Chunk7ReplaySubscriberContract.HorseFactType, 100),
                    "interface other than");
                TestRunner.True(Chunk7ReplaySubscriberContract.Violation(Valid(1), 0) != null && Chunk7ReplaySubscriberContract.Violation(Valid(2), 3) != null,
                    "A Horse owner count outside one or two was accepted.");
                var threw = false;
                try { Chunk7ReplaySubscriberContract.Violation(null, 1); } catch (ArgumentNullException) { threw = true; }
                TestRunner.True(threw, "A missing inventory was not refused.");
            });
        }
    }
}
