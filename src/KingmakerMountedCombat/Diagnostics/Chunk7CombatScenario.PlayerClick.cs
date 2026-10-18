using System;
using System.Linq;
using Kingmaker;
using Kingmaker.PubSubSystem;
using Kingmaker.UI.Selection;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // Owner-seed-derived RT, Directive 4 R1: one real ordinary player hostile click by rider A while the
    // native party brains still propose UnitAttacks on retained ManualTargets (rider A and Horse B after
    // the concurrent primaries). The click must start exactly one pair-A stock intent and one rider-A
    // native attack on its target; refused proposals must neither borrow nor erase it, earn no warning,
    // and leave pair B untouched. The window ends with the player's own native Stop on rider A. The
    // observer only records: it clears no target and suppresses no proposal.
    internal sealed partial class Chunk7CombatScenario
    {
        private const string PairAttackType = "KingmakerMountedCombat.Integration.MountedPairAttackCommand";
        private JObject playerClick;
        private int playerClickTraceOffset;
        private PlayerWarningObserver playerWarnings;

        private sealed class PlayerWarningObserver : IWarningNotificationUIHandler, IDisposable
        {
            private readonly IDisposable subscription;
            internal readonly JArray Events = new JArray();
            internal PlayerWarningObserver() { subscription = EventBus.Subscribe(this); }
            public void HandleWarning(Kingmaker.UI.WarningNotificationType type, bool addToLog) { }
            public void HandleWarning(string text, bool addToLog)
            {
                if (Events.Count < 64) Events.Add(new JObject { ["text"] = text, ["frame"] = Time.frameCount });
            }
            public void Dispose() { subscription?.Dispose(); }
        }

        private bool PlayerClickReady() =>
            PairIdle(a) && PairIdle(b) && NativeActorReady(ar) && TargetSettled() && !a.Combat.HasStockAttackIntent;

        private JObject PlayerClickWindow(string label)
        {
            var snapshot = CompanionWindow(label);
            snapshot["intent"] = new JObject {
                ["aStarts"] = a.Combat.StockAttackIntentStartCount, ["aCancels"] = a.Combat.StockAttackIntentCancelCount,
                ["aRequests"] = a.Combat.StockAttackNativeRequestCount, ["aActive"] = a.Combat.HasStockAttackIntent,
                ["aObservation"] = a.Combat.LastStockAttackObservation,
                ["bStarts"] = b.Combat.StockAttackIntentStartCount, ["bRequests"] = b.Combat.StockAttackNativeRequestCount,
                ["bActive"] = b.Combat.HasStockAttackIntent, ["bObservation"] = b.Combat.LastStockAttackObservation };
            snapshot["manualTargets"] = new JObject { ["riderA"] = ar.CombatState.ManualTarget?.UniqueId, ["mountB"] = bm.CombatState.ManualTarget?.UniqueId };
            return snapshot;
        }

        private void BeginPlayerClick()
        {
            Game.Instance.IsPaused = true;
            playerWarnings = new PlayerWarningObserver();
            playerClickTraceOffset = trace.EventCount;
            playerClick = new JObject { ["contract"] = "owner-seed-rt-player-click-integrity-v1", ["actorId"] = ar.UniqueId,
                ["targetId"] = target.UniqueId, ["traceSequenceBefore"] = playerClickTraceOffset, ["before"] = PlayerClickWindow("player-click-before") };
            evidence["playerClick"] = playerClick;
            Require(targetService.BeginExpectedAttackDispatch(target), "Player-click native target authorization refused.");
            Chunk7NativeFixture.Select(ar); Game.Instance.DefaultPointerController.ClearPointerMode();
            using (var input = new NativeOrdinaryAttackInput(target)) playerClick["clicked"] = input.Click();
            playerClick["admitted"] = PlayerClickWindow("player-click-admitted");
            Require((bool)playerClick["clicked"] && a.Combat.HasStockAttackIntent &&
                a.Combat.StockAttackIntentStartCount == (long)playerClick["before"]["intent"]["aStarts"] + 1 &&
                b.Combat.StockAttackIntentStartCount == (long)playerClick["before"]["intent"]["bStarts"],
                "The ordinary player click did not start exactly one pair A stock intent: " + a.Combat.LastStockAttackObservation);
            Game.Instance.IsPaused = false;
        }

        private bool FinishPlayerClick()
        {
            var events = trace.EventsSince(playerClickTraceOffset).Cast<JObject>().ToArray();
            var riderCosts = events.Where(e => (string)e["boundary"] == "cost-after" && (string)e["state"]?["actor"] == ar.UniqueId &&
                (string)e["commandType"] == PairAttackType).ToArray();
            if (playerClick["stop"] == null)
            {
                // Wait for the click's first native attack to be charged, then stop it with the player's own Stop.
                if (riderCosts.Length == 0) return false;
                Game.Instance.IsPaused = true;
                var stopBefore = trace.EventCount;
                Chunk7NativeFixture.Select(ar); SelectionManager.Instance.Stop();
                playerClick["stop"] = new JObject { ["selectedId"] = ar.UniqueId, ["traceSequenceBefore"] = stopBefore,
                    ["intentAfterStop"] = a.Combat.HasStockAttackIntent, ["manualTargetAfterStop"] = ar.CombatState.ManualTarget?.UniqueId };
                Game.Instance.IsPaused = false;
                return false;
            }
            if (!PairIdle(a) || !PairIdle(b) || ar.AreHandsBusyWithAnimation) return false;
            playerClick["after"] = PlayerClickWindow("player-click-after"); playerClick["traceSequenceAfter"] = trace.EventCount;
            playerWarnings.Dispose();
            playerClick["warnings"] = playerWarnings.Events;
            var pairIds = actors.Select(x => x.UniqueId).ToArray();
            var stopSequence = (int)playerClick["stop"]["traceSequenceBefore"];
            var clickCosts = riderCosts.Where(e => (int)e["sequence"] <= stopSequence).ToArray();
            playerClick["riderAttackCosts"] = new JArray(clickCosts.Select(e => new JObject { ["sequence"] = e["sequence"], ["command"] = e["command"],
                ["commandTarget"] = e["commandTarget"] }));
            var pairBKmc = events.Where(e => (string)e["commandType"] == PairAttackType &&
                ((string)e["state"]?["actor"] == br.UniqueId || (string)e["state"]?["actor"] == bm.UniqueId)).ToArray();
            var accounted = events.Where(e => CompanionAccountedBoundaries.Contains((string)e["boundary"])).ToArray();
            var refused = RefusedPairProposals(accounted, new[] { ar.UniqueId, am.UniqueId, br.UniqueId, bm.UniqueId }, 0,
                new[] { playerClick["before"]["pairCommands"], playerClick["admitted"]["pairCommands"], playerClick["after"]["pairCommands"] }
                    .Select(x => (JToken)new JArray(((JObject)x).Properties().Select(prop => prop.Value))).ToArray(), out var unusedTouched);
            playerClick["pairRefusedProposals"] = refused; playerClick["pairBKmcCommandRows"] = pairBKmc.Length;
            Require(clickCosts.Length == 1 && (string)clickCosts[0]["commandTarget"] == target.UniqueId,
                "The player click did not cause exactly one rider-A native attack on its own target before Stop.");
            Require(pairBKmc.Length == 0, "Pair B received a KMC attack command during the player click.");
            Require(!(bool)playerClick["stop"]["intentAfterStop"], "The player's native Stop did not end pair A's stock intent.");
            Require(!playerWarnings.Events.Any(), "A player-facing warning appeared while only native proposals were refused.");
            foreach (var actor in new[] { br, bm })
            {
                var before = Actor((JObject)playerClick["before"], actor); var after = Actor((JObject)playerClick["after"], actor);
                Require(new[] { "standard", "move", "swift" }.All(f => (double)after[f] <= (double)before[f] + .001) && JToken.DeepEquals(before["reactions"], after["reactions"]),
                    "Pair B's native expenditure rose during the player click.");
            }
            Require(RestoredOrLiveOwnersUnchanged(), "Pair custody changed during the player click.");
            playerClick["terminal"] = true;
            return true;
        }
    }
}
