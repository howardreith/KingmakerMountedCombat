using System;
using System.Linq;
using Kingmaker;
using Kingmaker.TurnBasedMode;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.Utility;
using KingmakerMountedCombat.Domain;
using Newtonsoft.Json.Linq;
using Pathfinding;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // Owner-seed-derived TB, Directive 4 R3: after rider A's native first-action Dismount, Horse A takes
    // one ordinary native ground move on its own independent turn (genuine prior mount spending), then
    // rider A uses native combat Mount as the first action of its next prepared turn while pair B stays
    // mounted and owned. The roster order decides the adoption: mount-first leaves the Horse's slot behind
    // the rider (RetainPartnerParticipation: no preparation, the spent move stands); rider-first leaves it
    // ahead (PreparePartnerThisRound: exactly one partner preparation). No status, resource or activation
    // is written by the diagnostic.
    internal sealed partial class Chunk7CombatScenario
    {
        private JObject firstActionMount;
        private UnitMoveTo horseIndependentMove;
        private TurnController horseIndependentTurn, firstActionTurn;
        private Vector3 horseMoveStart;
        private int horseGrantsBeforeMount;

        private void BeginHorseIndependentMove(TurnController turn)
        {
            horseIndependentTurn = turn; actingIndex = 0; Game.Instance.IsPaused = true;
            Chunk7NativeFixture.Select(am); horseMoveStart = am.Position;
            var delta = target.Position - horseMoveStart; delta.y = 0f;
            Require(delta.sqrMagnitude > .01f, "Native target direction is unavailable for the independent Horse.");
            var toward = delta.normalized; var candidates = new JArray();
            // v2: the destination search is required; a reader recomputes each preview's residual and displacement from the
            // Horse's position in the paused "before" snapshot (horseMoveStart, the same frame).
            var move = new JObject { ["round"] = Game.Instance.TurnBasedCombatController.RoundNumber, ["before"] = WorldSnapshot("horse-independent-move-before"),
                ["destinationSearch"] = new JObject { ["contract"] = "native-tb-ground-step-destination-v1",
                    ["approachRadius"] = NativeGroundStepDestinationPolicy.NativeGroundApproachRadius,
                    ["minimumNativeApproach"] = NativeGroundStepDestinationPolicy.MinimumNativeApproach, ["candidates"] = candidates } };
            firstActionMount = new JObject { ["contract"] = "owner-seed-tb-first-action-mount-v2", ["riderId"] = ar.UniqueId, ["mountId"] = am.UniqueId,
                ["neighbourOwnerId"] = b.Relationship.PairOwner?.Id.ToString(), ["neighbourGeneration"] = b.Relationship.MountedPairGeneration,
                ["horseMove"] = move };
            evidence["firstActionMount"] = firstActionMount;
            // F3 r07/r08: the fixed 0.5 m point toward the target admitted a native UnitMoveTo whose consumed preview ended
            // where the Horse stood (shouldApproach=false), so it succeeded without moving. The same native ground click is
            // issued only for a step destination whose own fresh native preview reaches it outside the click's approach
            // radius (NativeGroundStepDestinationPolicy); the target bearing is tried first. None refuses as uncovered.
            NativeOrdinaryAttackInput chosen = null;
            try
            {
                foreach (var offset in NativeGroundStepDestinationPolicy.BearingOffsets())
                {
                    var destination = horseMoveStart + Quaternion.Euler(0f, offset, 0f) * toward * NativeGroundStepDestinationPolicy.StepDistance;
                    var row = new JObject { ["bearingOffset"] = offset, ["destination"] = Point(destination) }; candidates.Add(row);
                    var held = PathVisualizer.Instance?.CurrentPathForUnit(am.View);
                    NativeOrdinaryAttackInput input;
                    try { input = new NativeOrdinaryAttackInput(destination); }
                    catch (InvalidOperationException error) { row["reason"] = "native-handler-not-ground"; row["detail"] = error.Message; continue; }
                    var admitted = false;
                    try
                    {
                        input.Predict(); var attempts = 0;
                        while ((turn.EnabledFiveFootStep || turn.EnabledSingleActionMove) && attempts++ < 4) { input.Click(button: 1); input.Predict(); }
                        var path = PathVisualizer.Instance?.CurrentPathForUnit(am.View); var points = path?.vectorPath;
                        var fresh = path != null && !ReferenceEquals(path, held);
                        var complete = path != null && path.CompleteState == PathCompleteState.Complete && points != null && points.Count > 0;
                        var end = complete ? points[points.Count - 1] : horseMoveStart;
                        var residual = complete ? (double)GeometryUtils.MechanicsDistance(end, destination) : double.NaN;
                        var displacement = complete ? (double)GeometryUtils.MechanicsDistance(end, horseMoveStart) : double.NaN;
                        admitted = NativeGroundStepDestinationPolicy.Admits(fresh, complete, residual, displacement);
                        row["freshPreview"] = fresh; row["previewState"] = path?.CompleteState.ToString(); row["previewPoints"] = points?.Count;
                        row["previewEnd"] = complete ? Point(end) : null; row["endResidual"] = complete ? new JValue(residual) : null;
                        row["displacement"] = complete ? new JValue(displacement) : null;
                        row["reason"] = NativeGroundStepDestinationPolicy.Reason(fresh, complete, residual, displacement);
                    }
                    finally { if (!admitted) input.Dispose(); }
                    if (!admitted) continue;
                    chosen = input; move["destination"] = Point(destination); move["bearingOffset"] = offset;
                    move["requestedDistance"] = Vector3.Distance(horseMoveStart, destination);
                    break;
                }
                Require(chosen != null, "No native ground step destination's own native preview leaves the independent Horse's approach radius; " +
                    "the independent Horse move is uncovered on this fixture: " + candidates.ToString(Newtonsoft.Json.Formatting.None));
                move["clicked"] = chosen.Click();
            }
            finally { chosen?.Dispose(); }
            horseIndependentMove = am.Commands.Move as UnitMoveTo;
            Require((bool)firstActionMount["horseMove"]["clicked"] && horseIndependentMove?.Executor == am,
                "The independent Horse's ordinary ground click did not admit its own native move.");
            issuedCommands.Add(horseIndependentMove); firstActionMount["horseMove"]["command"] = trace.ObjectIdentity(horseIndependentMove);
            Game.Instance.IsPaused = false;
        }

        private bool FinishHorseIndependentMove()
        {
            if (!horseIndependentMove.IsFinished || !am.Commands.Empty || am.AreHandsBusyWithAnimation) return false;
            var move = (JObject)firstActionMount["horseMove"]; var after = WorldSnapshot("horse-independent-move-after"); move["after"] = after;
            var distance = Vector3.Distance(horseMoveStart, am.Position); move["distance"] = distance;
            Require(ReferenceEquals(horseIndependentTurn, Game.Instance.TurnBasedCombatController.CurrentTurn) && distance > .02f &&
                (float)Actor(after, am)["move"] > (float)Actor((JObject)move["before"], am)["move"] && NeighbourBudgetsUnchanged((JObject)move["before"], after),
                "The independent Horse's native move did not spend its own movement alone.");
            move["terminal"] = true;
            return true;
        }

        // The rider's next own turn, prepared and idle; every other turn ends natively.
        private bool FirstActionMountReady(TurnController turn) =>
            turn?.Unit == ar && Chunk7NativeFixture.ReadyForTurnInput(turn) && ar.Commands.Empty && am.Commands.Empty &&
            !ar.AreHandsBusyWithAnimation && !am.AreHandsBusyWithAnimation && a.Relationship.State == RelationshipState.Unmounted;

        private void BeginFirstActionMount(TurnController turn)
        {
            firstActionTurn = turn; actingIndex = 0; horseGrantsBeforeMount = trace.GrantCount(am);
            firstActionMount["mountTurn"] = new JObject { ["round"] = Game.Instance.TurnBasedCombatController.RoundNumber,
                ["turnStatus"] = turn.Status.ToString(), ["expectedDisposition"] = SelectedScenario == MountFirst ?
                    MidEncounterAdoption.RetainPartnerParticipation.ToString() : MidEncounterAdoption.PreparePartnerThisRound.ToString(),
                ["horseGrantsBefore"] = horseGrantsBeforeMount, ["before"] = WorldSnapshot("first-action-mount-before") };
            BeginRelationship(ar, am, controls.MountAbility, br, "first-action-mount-a");
        }

        private bool FinishFirstActionMount()
        {
            if (a.Relationship.State != RelationshipState.Mounted || !RelationshipSettled()) return false;
            var mountTurn = (JObject)firstActionMount["mountTurn"]; var after = WorldSnapshot("first-action-mount-after"); mountTurn["after"] = after;
            var observation = a.Combat.LastPairedAdoptionObservation; mountTurn["adoptionObservation"] = observation;
            mountTurn["transactionObservation"] = a.Relationship.LastAdoptionTransactionObservation;
            var grants = trace.GrantCount(am) - horseGrantsBeforeMount; mountTurn["horseGrantsDuringMount"] = grants;
            var retain = SelectedScenario == MountFirst;
            Require(observation != null && observation.Contains("disposition=" + (string)mountTurn["expectedDisposition"]) &&
                observation.Contains(";partnerPrepareCalls=" + (retain ? 0 : 1)),
                "First-action combat Mount did not take the roster's adoption disposition: " + observation);
            Require(grants == (retain ? 0 : 1), "First-action combat Mount prepared the partner " + grants + " times.");
            var beforeMount = Actor((JObject)mountTurn["before"], am); var afterMount = Actor(after, am);
            if (retain)
                Require(new[] { "standard", "move", "swift" }.All(f => (double)afterMount[f] >= (double)beforeMount[f] - .001) &&
                    (double)afterMount["move"] > .001, "Retained participation replenished the Horse's prior native spending.");
            Require(b.Relationship.State == RelationshipState.Mounted && b.Relationship.PairOwner?.Id.ToString() == (string)firstActionMount["neighbourOwnerId"] &&
                b.Relationship.MountedPairGeneration == (long)firstActionMount["neighbourGeneration"] && NeighbourBudgetsUnchanged((JObject)mountTurn["before"], after),
                "Pair B's ownership or expenditure changed during pair A's first-action Mount.");
            mountTurn["terminal"] = true;
            return true;
        }
    }
}
