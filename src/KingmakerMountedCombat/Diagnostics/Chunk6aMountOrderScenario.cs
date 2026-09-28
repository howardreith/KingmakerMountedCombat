using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker;
using KingmakerMountedCombat.Domain;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;
namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        internal static bool IsChunk6aMountOrderScenario(string scenario) => scenario == "chunk6a-allocation-rider-first-tb" || scenario == "chunk6a-allocation-mount-first-tb";
        private bool Chunk6aMountOrderOnly => IsChunk6aMountOrderScenario(request.Scenario);
        private bool Chunk6aOrderRiderFirst => request.Scenario == "chunk6a-allocation-rider-first-tb";
        private JObject chunk6aOrderEvidence;
        private readonly JArray chunk6aOrderTurns = new JArray();
        private readonly HashSet<string> chunk6aOrderSeen = new HashSet<string>(StringComparer.Ordinal);
        private bool chunk6aOrderInputOwned, chunk6aOrderEndClicked;
        private int chunk6aOrderRiderBase, chunk6aOrderMountBase, chunk6aOrderStartSequence;
        private NativeTurnCompletionProbe chunk6aOrderCompletion;
        private JObject CaptureChunk6aOrderBoundary()
        {
            var controller = Game.Instance.TurnBasedCombatController; var turn = controller.CurrentTurn;
            return new JObject {
                ["frame"] = Time.frameCount, ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks,
                ["allocationSequence"] = allocationTrace.EventCount, ["turnObject"] = turn == null ? 0 : RuntimeHelpers.GetHashCode(turn),
                ["currentActor"] = turn?.Unit?.UniqueId, ["status"] = turn?.Status.ToString(), ["round"] = controller.RoundNumber,
                ["controllerObject"] = RuntimeHelpers.GetHashCode(controller), ["sessionObject"] = RuntimeHelpers.GetHashCode(Game.Instance.Player),
                ["turnBased"] = CombatController.IsInTurnBasedCombat(), ["state"] = CaptureChunk6aCausalState(),
                ["riderResources"] = allocationTrace.Snapshot(rider), ["mountResources"] = allocationTrace.Snapshot(horse),
                ["pairedSequence"] = combat.PairedActivationSequence, ["adoptionCount"] = combat.MidEncounterAdoptionCount,
                ["partnerContextObject"] = combat.PairedPartnerContext == null ? 0 : RuntimeHelpers.GetHashCode(combat.PairedPartnerContext),
                ["partnerActor"] = combat.PairedPartnerContext?.Unit?.UniqueId,
                ["pairedFinalized"] = combat.PairedActivationFinalized, ["pairedSplit"] = combat.PairedActivationSplit,
                ["roster"] = new JArray(controller.SortedUnits.Select(u => new JObject { ["actor"] = u.UniqueId,
                    ["initiativeOrder"] = u.CombatState.Initiative, ["visible"] = u.IsVisibleForPlayer, ["surprised"] = controller.IsSurprised(u) }))
            };
        }
        private void PrepareChunk6aMountOrderFixture()
        {
            if (!Chunk6aMountOrderOnly) return;
            if (!NativeMountOrderEvidence.CanPrepareFixture(chunk6aOrderInputOwned, rider.IsInCombat, horse.IsInCombat,
                Game.Instance.Player.IsInCombat, relationship.State == RelationshipState.Unmounted, Chunk6aIdle,
                turnBasedModeProbe?.TemporaryValue == true, turnBasedModeProbe?.TemporaryValueIsCurrent == true))
                throw new InvalidOperationException("Mount-order inputs require a fresh unmounted pre-encounter allocation.");
            chunk6aOrderRiderBase = rider.Stats.Initiative.BaseValue; chunk6aOrderMountBase = horse.Stats.Initiative.BaseValue;
            var before = new JObject { ["rider"] = allocationTrace.Snapshot(rider), ["mount"] = allocationTrace.Snapshot(horse) };
            chunk6aOrderEvidence = new JObject { ["contract"] = "fresh-native-allocation-order-through-next-paired-round", ["scenario"] = request.Scenario,
                ["riderFirst"] = Chunk6aOrderRiderFirst, ["riderId"] = rider.UniqueId, ["mountId"] = horse.UniqueId,
                ["fixture"] = new JObject { ["outsideCombat"] = true, ["targetTurnBased"] = turnBasedModeProbe.TemporaryValue, ["modeLeaseCurrent"] = turnBasedModeProbe.TemporaryValueIsCurrent, ["relationshipState"] = relationship.State.ToString(),
                    ["riderOriginalBase"] = chunk6aOrderRiderBase, ["mountOriginalBase"] = chunk6aOrderMountBase, ["beforeResources"] = before },
                ["turns"] = chunk6aOrderTurns };
            observations["chunk6aMountOrder"] = chunk6aOrderEvidence;
            chunk6aOrderInputOwned = true;
            rider.Stats.Initiative.BaseValue = Chunk6aOrderRiderFirst ? 40 : -40;
            horse.Stats.Initiative.BaseValue = Chunk6aOrderRiderFirst ? -40 : 40;
            var after = new JObject { ["rider"] = allocationTrace.Snapshot(rider), ["mount"] = allocationTrace.Snapshot(horse) };
            var fixture = (JObject)chunk6aOrderEvidence["fixture"];
            fixture["afterResources"] = after; fixture["riderInputBase"] = rider.Stats.Initiative.BaseValue; fixture["mountInputBase"] = horse.Stats.Initiative.BaseValue;
            fixture["riderModifiedInput"] = rider.Stats.Initiative.ModifiedValue; fixture["mountModifiedInput"] = horse.Stats.Initiative.ModifiedValue;
            if (!JToken.DeepEquals(before, after)) throw new InvalidOperationException("Pre-encounter initiative input changed an allocation resource.");
            chunk6aOrderStartSequence = allocationTrace.EventCount;
            fixture["allocationSequence"] = chunk6aOrderStartSequence;
        }
        private void ObserveChunk6aMountOrderTurn()
        {
            if (!Chunk6aMountOrderOnly || chunk6aOrderEvidence == null) return;
            var controller = Game.Instance.TurnBasedCombatController; var turn = controller?.CurrentTurn;
            if (turn == null || !Game.Instance.Player.IsInCombat) return;
            var key = controller.RoundNumber + ":" + RuntimeHelpers.GetHashCode(turn) + ":" + turn.Status;
            if (!chunk6aOrderSeen.Add(key)) return;
            if (chunk6aOrderTurns.Count >= 128) throw new InvalidOperationException("Mount-order observation bound exceeded.");
            chunk6aOrderTurns.Add(CaptureChunk6aOrderBoundary());
        }
        private void FinishChunk6aMountOrder(JObject mountProof)
        {
            if (!Chunk6aMountOrderOnly || chunk6aOrderEvidence == null || (bool?)mountProof?["pass"] != true || chunk6aCommandWindow != null)
                throw new InvalidOperationException("Allocation order requires one complete positive native Mount.");
            chunk6aOrderEvidence["positiveProof"] = mountProof.DeepClone();
            chunk6aOrderEvidence["mountBefore"] = chunk6aPreMount.DeepClone();
            chunk6aOrderEvidence["disposition"] = chunk6aDisposition.ToString();
            var mounted = CaptureChunk6aOrderBoundary();
            chunk6aOrderEvidence["mounted"] = mounted;
            var terminal = mountProof["samples"].OfType<JObject>().Single(s => (string)s["boundary"] == "terminal");
            var bridge = new JObject { ["contract"] = "same-allocation-no-command-cost-with-observed-native-time-only",
                ["riderId"] = rider.UniqueId, ["mountId"] = horse.UniqueId, ["turnBased"] = true,
                ["traceComplete"] = allocationTrace.Complete, ["observerHooks"] = allocationTrace.ObserverHooks,
                ["before"] = new JObject { ["frame"] = terminal["frame"].DeepClone(), ["gameTicks"] = terminal["gameTicks"].DeepClone(),
                    ["allocationSequence"] = terminal["allocationSequence"].DeepClone(), ["rider"] = terminal["nativeAllocation"]["rider"].DeepClone(),
                    ["mount"] = terminal["nativeAllocation"]["mount"].DeepClone() },
                ["after"] = new JObject { ["frame"] = mounted["frame"].DeepClone(), ["gameTicks"] = mounted["gameTicks"].DeepClone(),
                    ["allocationSequence"] = mounted["allocationSequence"].DeepClone(), ["rider"] = mounted["riderResources"].DeepClone(),
                    ["mount"] = mounted["mountResources"].DeepClone() },
                ["events"] = new JArray(allocationTrace.EventsSince((int)terminal["allocationSequence"]).Take((int)mounted["allocationSequence"] - (int)terminal["allocationSequence"])) };
            chunk6aOrderEvidence["terminalBridge"] = bridge;
            NativePassiveResourceEvidence.AssertComplete(bridge);
            allocationTrace.ObserveReactionResources = true;
            chunk6aOrderCompletion = new NativeTurnCompletionProbe(allocationTrace, rider, horse, combat);
            chunk6aStage = 29; ResetLeafClock();
        }
        private void TickChunk6aMountOrderCompletion()
        {
            var controller = Game.Instance.TurnBasedCombatController; var turn = controller.CurrentTurn;
            if (!Chunk6aMountOrderOnly || relationship.State != RelationshipState.Mounted || !rider.IsInCombat || !horse.IsInCombat ||
                !Game.Instance.Player.IsInCombat || !CombatController.IsInTurnBasedCombat())
                throw new InvalidOperationException("Allocation-order encounter or exact pair ended before observation completed.");
            if (chunk6aStage == 29)
            {
                if (!ReferenceEquals(turn, chunk6aMountTurn) || turn.Unit != rider || !turn.IsActing)
                    throw new InvalidOperationException("Mount-order case lost its adopted rider turn before native End Turn input.");
                if (!Chunk6aIdle || controller.WaitingForUI || GetPendingNextUnit(controller) != null || !turn.CanEndTurnAndNoActing()) return;
                if (!EnsureChunk6aRiderSelection("CM03-allocation-order")) return;
                if (chunk6aOrderEndClicked) throw new InvalidOperationException("Duplicate native End Turn input.");
                chunk6aOrderEvidence["beforeEndInput"] = CaptureChunk6aOrderBoundary();
                chunk6aOrderEndClicked = true;
                Game.Instance.PauseBind();
                chunk6aOrderEvidence["afterEndInput"] = CaptureChunk6aOrderBoundary();
                chunk6aOrderEvidence["input"] = new JObject { ["method"] = "Kingmaker.Game.PauseBind", ["token"] = "06000CB7",
                    ["moduleMvid"] = typeof(Game).Assembly.ManifestModule.ModuleVersionId.ToString(), ["count"] = 1 };
                chunk6aStage = 30; ResetLeafClock(); return;
            }
            if (chunk6aStage != 30) throw new InvalidOperationException("Mount-order completion stage differs.");
            if (controller.RoundNumber > chunk6aMountRound + 1 || combat.PairedActivationSequence > 2)
                throw new InvalidOperationException("Skipped the exact next paired activation.");
            if (turn?.Unit == horse) throw new InvalidOperationException("The mounted partner received an independent native turn.");
            if (controller.RoundNumber == chunk6aMountRound + 1 && turn?.Unit == rider && combat.PairedActivationSequence == 2 &&
                combat.PairedPartnerContext?.Unit == horse && Chunk6aIdle)
            {
                chunk6aOrderEvidence["nextRound"] = CaptureChunk6aOrderBoundary();
                chunk6aOrderEvidence["completion"] = chunk6aOrderCompletion.Capture();
                chunk6aOrderEvidence["allocationTrace"] = allocationTrace.Capture();
                var mounted = chunk6aOrderEvidence["mounted"]; var next = chunk6aOrderEvidence["nextRound"];
                Func<JToken, JObject> resourceBoundary = b => new JObject { ["frame"] = b["frame"].DeepClone(), ["gameTicks"] = b["gameTicks"].DeepClone(),
                    ["allocationSequence"] = b["allocationSequence"].DeepClone(), ["round"] = b["round"].DeepClone(),
                    ["rider"] = b["riderResources"].DeepClone(), ["mount"] = b["mountResources"].DeepClone() };
                chunk6aOrderEvidence["continuation"] = new JObject { ["contract"] = "ordinary-paired-end-through-one-native-next-preparation", ["traceComplete"] = allocationTrace.Complete,
                    ["riderId"] = rider.UniqueId, ["mountId"] = horse.UniqueId, ["before"] = resourceBoundary(mounted), ["after"] = resourceBoundary(next),
                    ["completion"] = chunk6aOrderEvidence["completion"].DeepClone(), ["observerHooks"] = allocationTrace.ObserverHooks,
                    ["events"] = new JArray(allocationTrace.EventsSince((int)mounted["allocationSequence"]).Take((int)next["allocationSequence"] - (int)mounted["allocationSequence"])) };
                string failure = null;
                try { NativeMountOrderEvidence.AssertComplete(chunk6aOrderEvidence); }
                catch (Exception exception) { failure = exception.Message; }
                var row = Chunk6aOrderRiderFirst ? "CM03-rider-before-mount-slot" : "CM03-mount-slot-before-rider";
                AddRow(row, failure == null, failure ?? "The exact native order yielded one partner participation in the transition round and no independent mounted partner turn before the next paired activation.", chunk6aOrderEvidence);
                AddRow("CM03-next-round-activation", failure == null, failure ?? "The next native round prepared both actors once under paired sequence two without repeating mid-encounter adoption.", chunk6aOrderEvidence);
                chunk6aStage = 99; BeginCleanup(); return;
            }
            if (turn?.Unit != rider) TryEndPhase3gFixtureTurn(turn);
        }
        private void RestoreChunk6aMountOrderFixture()
        {
            chunk6aOrderCompletion?.Dispose(); chunk6aOrderCompletion = null;
            if (!chunk6aOrderInputOwned) return;
            if (rider.IsInCombat || horse.IsInCombat || rider.Stats.Initiative.BaseValue != (Chunk6aOrderRiderFirst ? 40 : -40) ||
                horse.Stats.Initiative.BaseValue != (Chunk6aOrderRiderFirst ? -40 : 40))
                throw new InvalidOperationException("Mount-order initiative restoration found live combat or changed lease inputs.");
            rider.Stats.Initiative.BaseValue = chunk6aOrderRiderBase; horse.Stats.Initiative.BaseValue = chunk6aOrderMountBase;
            chunk6aOrderInputOwned = false;
            chunk6aOrderEvidence["restoration"] = new JObject { ["outsideCombat"] = !rider.IsInCombat && !horse.IsInCombat,
                ["riderBase"] = rider.Stats.Initiative.BaseValue, ["mountBase"] = horse.Stats.Initiative.BaseValue,
                ["exact"] = rider.Stats.Initiative.BaseValue == chunk6aOrderRiderBase && horse.Stats.Initiative.BaseValue == chunk6aOrderMountBase };
        }
    }
}
