using System;
using System.Linq;
using Kingmaker;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.Utility;
using Kingmaker.View;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class RuntimePersistenceScenario
    {
        private bool ReactionCase => Checkpoint == "reaction";
        private Phase3dCombatRuleProbe reactionProbe;
        private UnitMoveTo reactionMove;
        private Vector3 reactionOrigin;
        private Vector3 reactionHome;
        private Vector3 reactionAway;
        private int reactionRulesBefore;
        private SavedNativeActor reactionDebtBefore;
        private long reactionMoveTicks;

        private void InstallReactionTargetCondition()
        {
            Check(!combatTarget.Descriptor.State.HasCondition(UnitCondition.ImmuneToCombatManeuvers),
                "P03-owned-reaction-target-condition-slot-empty");
            // This existing native condition is itself serialized by UnitState.
            // It isolates repeated disengagement from the mammoth's trip.
            combatTarget.Descriptor.State.AddCondition(UnitCondition.ImmuneToCombatManeuvers);
            BindReactionProbe();
        }

        private void BindReactionProbe()
        {
            Check(combatTarget.Descriptor.State.HasCondition(UnitCondition.ImmuneToCombatManeuvers),
                "P03-native-target-condition-present-without-cold-reapplication");
            reactionProbe = new Phase3dCombatRuleProbe(rider, mount);
            reactionProbe.Arm(combatTarget, false);
        }

        private JObject ReactionObservation() => new JObject
        {
            ["combat"] = CombatObservation(),
            ["rules"] = reactionProbe.CapturePairEvidence(),
            ["nativeTargetMove"] = combatTarget.CombatState.Cooldown.MoveAction,
            ["targetProne"] = combatTarget.Descriptor.State.Prone.Active,
            ["condition"] = combatTarget.Descriptor.State.HasCondition(UnitCondition.ImmuneToCombatManeuvers),
            ["target"] = combatTarget.UniqueId
        };

        private void BeginReactionFixture()
        {
            BeginCombatMovement(0.75f, "reaction-approach-dispatched", RiderReachDestination());
            stage = 90;
        }

        private bool AdvanceReactionFixture(TurnController turn)
        {
            if (stage < 90 || stage > 95) return false;
            if (stage == 90)
            {
                if (move == null || !move.IsFinished || !PairIdle) return true;
                Check(move.Result == UnitCommand.ResultType.Success &&
                    mount.CombatState.AttackOfOpportunityPerRound == 1 &&
                    mount.CombatState.AttackOfOpportunityCount == 1,
                    "P03-native-mount-has-one-unspent-reaction-before-stimulus");
                Write("reaction-approach-completed", ReactionObservation());
                stage = 91;
            }
            if (stage == 91)
            {
                if (turn == null || !turn.IsActing && turn.Status != TurnController.TurnStatus.Preparing) return true;
                if (turn.Unit != combatTarget) { EndFixtureTurn(turn); return true; }
                if (!PairIdle || !combatTarget.Commands.Empty) return true;
                savedBoundary = turn; savedSequence = combat.PairedActivationSequence;
                savedRound = Game.Instance.TurnBasedCombatController.RoundNumber;
                Check(combat.PairedActorEnded(mount) && combat.PairedActorEnded(rider) &&
                    mount.CombatState.AttackOfOpportunityCount == 1,
                    "P03-reaction-occurs-on-unrelated-native-turn-after-pair-ended");
                reactionHome = combatTarget.Position;
                reactionAway = ReactionDestination(reactionHome, true);
                BeginReactionMove(reactionAway, "reaction-dispatched"); stage = 92; return true;
            }
            if (stage == 92)
            {
                if (!FinishReactionMove(turn)) return true;
                Check(reactionProbe.MountOpportunityAttackRuleCount == 1 &&
                    reactionProbe.PairOpportunityAttackRollCount >= 1 &&
                    mount.CombatState.AttackOfOpportunityCount == 0 &&
                    mount.CombatState.DisengageAttackTargets.Contains(combatTarget),
                    "P03-native-disengagement-delivered-and-consumed-one-mount-reaction");
                Write("reaction-consumed", ReactionObservation()); stage = 3; return true;
            }
            if (stage == 94 || stage == 95)
            {
                if (!FinishReactionMove(turn)) return true;
                Check(mount.CombatState.AttackOfOpportunityCount == 0 &&
                    reactionProbe.MountOpportunityAttackRuleCount == reactionRulesBefore,
                    "P03-loaded-consumed-reaction-cannot-trigger-again");
                if (stage == 94)
                {
                    BeginReactionMove(reactionAway, "reaction-repeat-dispatched");
                    stage = 95; return true;
                }
                Write("reaction-repeat-rejected", ReactionObservation()); stage = 9; return true;
            }
            return true;
        }

        private Vector3 ReactionDestination(Vector3 from, bool away)
        {
            var direction = from - mount.Position; direction.y = 0;
            if (direction.sqrMagnitude < 0.01f) throw new InvalidOperationException("Reaction fixture has no stable native direction.");
            var wanted = away ? from + direction.normalized * 4f :
                mount.Position + direction.normalized * (RiderAttackRadius() - 0.35f);
            var point = ObstacleAnalyzer.TraceAlongNavmesh(from, wanted);
            Check(GeometryUtils.MechanicsDistance(point, wanted) < 0.1f &&
                GeometryUtils.MechanicsDistance(from, point) > 3.5f,
                "P03-disengagement-stimulus-has-a-real-bounded-native-path");
            return point;
        }

        private void BeginReactionMove(Vector3 destination, string kind)
        {
            reactionOrigin = combatTarget.Position;
            reactionDebtBefore = MountedPersistenceService.CaptureActor(mount);
            reactionMoveTicks = Game.Instance.TimeController.GameTime.Ticks;
            reactionMove = new UnitMoveTo(destination);
            combatTarget.Commands.Run(reactionMove);
            Check(reactionMove.Executor == combatTarget, "P03-scripted-enemy-choice-uses-native-command-owner");
            Write(kind, ReactionObservation());
        }

        private bool FinishReactionMove(TurnController turn)
        {
            if (!reactionMove.IsFinished || !PairIdle || !combatTarget.Commands.Empty ||
                combatTarget.AreHandsBusyWithAnimation) return false;
            Check(ReferenceEquals(turn, savedBoundary) && turn.Unit == combatTarget &&
                combat.PairedActivationSequence == savedSequence &&
                reactionMove.Result == UnitCommand.ResultType.Success &&
                GeometryUtils.MechanicsDistance(reactionOrigin, combatTarget.Position) > 3.5f &&
                !turn.EnabledFiveFootStep, "P03-real-enemy-movement-stays-in-its-native-turn");
            var elapsed = (Game.Instance.TimeController.GameTime.Ticks - reactionMoveTicks) /
                (double)TimeSpan.TicksPerSecond;
            var after = MountedPersistenceService.CaptureActor(mount);
            Check(after.Standard <= reactionDebtBefore.Standard + 0.001f &&
                after.Standard + elapsed + 0.001 >= reactionDebtBefore.Standard &&
                after.Move <= reactionDebtBefore.Move + 0.001f &&
                after.Move + elapsed + 0.001 >= reactionDebtBefore.Move,
                "P03-reaction-does-not-spend-or-refund-ordinary-mount-work");
            return true;
        }

        private void ObserveColdReaction()
        {
            BindReactionProbe();
            Check(mount.CombatState.AttackOfOpportunityCount == 0 &&
                mount.CombatState.DisengageAttackTargets.Contains(combatTarget),
                "P03-cold-reaction-count-and-disengagement-obligation-retained");
            Write("reaction-loaded", ReactionObservation());
        }

        private void ContinueReaction()
        {
            Check(Game.Instance.TurnBasedCombatController.CurrentTurn?.Unit == combatTarget &&
                mount.CombatState.AttackOfOpportunityCount == 0,
                "P03-saving-or-loading-does-not-renew-the-consumed-reaction");
            reactionRulesBefore = reactionProbe.MountOpportunityAttackRuleCount;
            // New test movement is derived from the loaded native positions.
            // No former process path, command or evidence record supplies state.
            reactionAway = combatTarget.Position;
            reactionHome = ReactionDestination(reactionAway, false);
            BeginReactionMove(reactionHome, "reaction-return-dispatched"); stage = 94;
        }

        private void CheckReactionRefresh()
        {
            Check(mount.CombatState.AttackOfOpportunityCount == mount.CombatState.AttackOfOpportunityPerRound &&
                mount.CombatState.Cooldown.AttackOfOpportunity == 0 &&
                mount.CombatState.DisengageAttackTargets.Count == 0,
                "P03-real-next-preparation-refreshes-reaction-once");
        }
    }
}
