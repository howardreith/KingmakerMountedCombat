using System;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.Utility;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class RuntimePersistenceScenario
    {
        private readonly JArray commitmentMoves = new JArray();
        private SavedNativeActor commitmentDebt;
        private SavedMovementValues commitmentBefore;
        private int commitmentFrame;
        private bool CommitmentCase => request.Scenario == "persistence-p03-save" ||
            request.Scenario == "persistence-p03-load";

        private SavedMovementValues CurrentCommitment =>
            NativeTurnPersistence.Capture(combat.PairedPartnerContext).Movement;

        private void BeginCommitmentFixture()
        {
            commitmentBefore = CurrentCommitment;
            commitmentDebt = MountedPersistenceService.CaptureActor(mount);
            BeginCombatMovement(Checkpoint == "step" ? 1.2f : 6f,
                "commitment-dispatched", null, Checkpoint == "step");
            stage = 70;
        }

        private bool AdvanceCommitmentProbe(TurnController turn)
        {
            if (stage < 70 || stage > 75) return false;
            if (stage == 70)
            {
                if (move == null || !move.IsFinished || !PairIdle) return true;
                var after = CurrentCommitment;
                var debt = MountedPersistenceService.CaptureActor(mount);
                var distance = GeometryUtils.MechanicsDistance(origin, mount.Position);
                Check(distance > 0.25f && rider.CombatState.Cooldown.StandardAction == 0 &&
                    rider.CombatState.Cooldown.MoveAction == 0 &&
                    ReferenceEquals(savedBoundary, turn), "P03-native-transport-has-no-rider-tax-or-new-grant");
                commitmentMoves.Add(new JObject { ["distance"] = distance, ["result"] = move.Result.ToString(),
                    ["before"] = JObject.FromObject(commitmentDebt, MountedSaveCodec.CreateSerializer()),
                    ["after"] = JObject.FromObject(debt, MountedSaveCodec.CreateSerializer()),
                    ["movement"] = JObject.FromObject(after, MountedSaveCodec.CreateSerializer()) });
                if (Checkpoint == "step")
                    Check(move.Result == UnitCommand.ResultType.Success && debt.Move == 0 && debt.Standard == 0 &&
                        after.MetresStepped > 0 && after.MetresStepped < TurnController.MetersOfFiveFootStep &&
                        after.TimeStepped > 0, "P03-native-partial-step-commitment");
                else
                {
                    Check(debt.Move > commitmentDebt.Move && after.MetresStepped == 0,
                        "P03-native-conversion-setup-consumes-current-mount-work");
                    if (debt.Standard == 0)
                    {
                        commitmentDebt = debt;
                        BeginCombatMovement(6f, null);
                        return true;
                    }
                    Check(debt.Standard == 6 && debt.Move > 3 && debt.Move < 6,
                        "P03-native-Standard-conversion-leaves-measured-Move-remainder");
                }
                var detail = CombatObservation();
                detail["moves"] = commitmentMoves;
                detail["roundEffectCandidates"] = RoundEffectCandidates();
                Write("commitment-created", detail);
                stage = 3; return true;
            }
            if (Time.frameCount < commitmentFrame + 8 || !PairIdle || move != null && !move.IsFinished) return true;
            var current = CurrentCommitment;
            var actual = GeometryUtils.MechanicsDistance(origin, mount.Position);
            if (stage == 71)
            {
                Check(actual < 0.02f && LegitimateContinuation(commitmentDebt,
                    MountedPersistenceService.CaptureActor(mount), 0) &&
                    Math.Abs(current.MetresStepped - commitmentBefore.MetresStepped) < 0.001f,
                    "P03-saved-step-still-rejects-ordinary-movement");
                Write("ordinary-movement-rejected", CombatObservation());
                BeginCommitmentAttempt(2f, true, 72, "step-remainder-dispatched");
                return true;
            }
            if (stage == 72)
            {
                var remainder = TurnController.MetersOfFiveFootStep - commitmentBefore.MetresStepped;
                Check(actual > 0.02f && actual <= remainder + 0.02f &&
                    current.MetresStepped <= TurnController.MetersOfFiveFootStep + 0.01f &&
                    current.MetresStepped > commitmentBefore.MetresStepped &&
                    mount.CombatState.Cooldown.MoveAction == 0 &&
                    mount.CombatState.Cooldown.StandardAction == 0 &&
                    rider.CombatState.Cooldown.MoveAction == 0,
                    "P03-step-uses-only-saved-remainder-without-action-tax");
                Write("step-remainder-completed", CombatObservation());
                BeginCommitmentAttempt(2f, true, 73, "spent-work-input-before");
                return true;
            }
            if (stage == 73)
            {
                Check(actual < 0.02f && Math.Abs(current.MetresStepped - commitmentBefore.MetresStepped) < 0.001f &&
                    LegitimateContinuation(commitmentDebt, MountedPersistenceService.CaptureActor(mount), 0),
                    "P03-exhausted-step-delivers-no-renewed-distance");
                Write("spent-work-rejected", CombatObservation());
                stage = 9; return true;
            }
            if (stage == 74)
            {
                var debt = MountedPersistenceService.CaptureActor(mount);
                Check(actual > 0.02f && debt.Move > commitmentDebt.Move && debt.Move <= 6.02f &&
                    debt.Standard == commitmentDebt.Standard && rider.CombatState.Cooldown.MoveAction == 0 &&
                    rider.CombatState.Cooldown.StandardAction == 0 &&
                    ReferenceEquals(savedBoundary, turn), "P03-converted-movement-retains-only-legitimate-remainder");
                commitmentMoves.Add(new JObject { ["distance"] = actual, ["moveBefore"] = commitmentDebt.Move,
                    ["moveAfter"] = debt.Move, ["standard"] = debt.Standard });
                if (debt.Move < 5.99f)
                {
                    BeginCommitmentAttempt(6f, false, 74, null);
                    return true;
                }
                var detail = CombatObservation(); detail["moves"] = commitmentMoves;
                Write("conversion-remainder-completed", detail);
                BeginCommitmentAttempt(0.75f, false, 75, "spent-work-input-before");
                return true;
            }
            Check(actual < 0.02f && LegitimateContinuation(commitmentDebt,
                MountedPersistenceService.CaptureActor(mount), 0),
                "P03-converted-exhausted-movement-delivers-no-distance-or-refund");
            Write("spent-work-rejected", CombatObservation());
            stage = 9; return true;
        }

        private void ContinueCommitment()
        {
            Check(ReferenceEquals(savedBoundary, Game.Instance.TurnBasedCombatController.CurrentTurn) &&
                rider.CombatState.Cooldown.StandardAction == 0 && rider.CombatState.Cooldown.MoveAction == 0,
                "P03-saved-grant-keeps-rider-actions");
            if (Checkpoint == "step")
            {
                BeginCommitmentAttempt(0.75f, false, 71, "ordinary-movement-input-before");
                return;
            }
            var accepted = controls.CaptureSnapshot().DispatchAcceptedCount;
            Check(!TryPrimaryInput(true) && controls.CaptureSnapshot().DispatchAcceptedCount == accepted && PairIdle,
                "P03-converted-Standard-cannot-attack-again");
            Write("converted-standard-rejected", CombatObservation());
            BeginCommitmentAttempt(6f, false, 74, "conversion-remainder-dispatched");
        }

        private void BeginCommitmentAttempt(float distance, bool step, int nextStage, string kind)
        {
            commitmentDebt = MountedPersistenceService.CaptureActor(mount);
            commitmentBefore = CurrentCommitment;
            var beforeInput = kind == "spent-work-input-before" || kind == "ordinary-movement-input-before";
            if (beforeInput) Write(kind, CombatObservation());
            BeginCombatMovement(distance, beforeInput ? null : kind, null, step, false);
            commitmentFrame = Time.frameCount;
            stage = nextStage;
        }

        private static JArray RoundEffectCandidates()
        {
            var result = new JArray();
            foreach (var buff in ResourcesLibrary.LibraryObject.BlueprintsByAssetId.Values.OfType<BlueprintBuff>()
                .Where(b => b.ComponentsArray.OfType<AddEffectFastHealing>().Any(c =>
                    c.GetType() == typeof(AddEffectFastHealing) && c.Heal == 1))
                .OrderBy(b => b.AssetGuid, StringComparer.Ordinal).Take(8))
                result.Add(new JObject { ["id"] = buff.AssetGuid, ["name"] = buff.name,
                    ["components"] = new JArray(buff.ComponentsArray.Select(c => c.GetType().FullName)) });
            return result;
        }
    }
}
