using System;
using Kingmaker;
using Kingmaker.UnitLogic.Commands;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        private TurnController pairedReactionTurn;
        private UnitMoveTo pairedReactionMove;
        private Vector3 pairedReactionHome;
        private Vector3 pairedReactionOrigin;
        private Vector3 pairedReactionAway;
        private int pairedReactionLeg;
        private int pairedReactionFrame;
        private int pairedReactionRulesBefore;
        private bool pairedReactionRefreshed;
        private readonly JObject pairedReactionEvidence = new JObject();
        private JObject pairedReactionOperation;

        private bool TickPairedReactionProbe(TurnController turn)
        {
            if (turn.Unit != target || pairedActivationNumber != 1 || pairedReactionLeg == 4) return false;
            if (pairedReactionTurn == null)
            {
                pairedReactionTurn = turn;
                pairedReactionHome = target.Position;
                pairedReactionAway = pairedReactionHome + (target.Position - horse.Position).normalized * 4f;
                pairedReactionRulesBefore = ruleProbe.MountOpportunityAttackRuleCount;
                pairedReactionEvidence["inputKind"] = "scripted-native-AI-command";
                pairedReactionEvidence["nativeTurnActor"] = target.UniqueId;
                pairedReactionEvidence["activationIdentity"] = combat.PairedActivationIdentity;
                pairedReactionEvidence["mountBefore"] = allocationTrace.Snapshot(horse);
                pairedReactionEvidence["operations"] = new JArray();
                observations["pairedReactionProbe"] = pairedReactionEvidence;
                RequirePaired(horse.CombatState.AttackOfOpportunityPerRound == 1 && horse.CombatState.AttackOfOpportunityCount == 1,
                    "Reaction fixture requires one real available native mount reaction.");
                BeginPairedReactionMove();
                return true;
            }
            RequirePaired(ReferenceEquals(turn, pairedReactionTurn), "Enemy reaction probe crossed a native turn boundary.");
            if (!pairedReactionMove.IsFinished || !target.Commands.Empty || !horse.Commands.Empty || horse.AreHandsBusyWithAnimation)
            {
                RequirePaired(Time.frameCount <= pairedReactionFrame + 600, "Native enemy reaction movement did not finish.");
                return true;
            }
            var after = allocationTrace.Snapshot(target);
            var before = pairedReactionOperation["before"];
            var distance = HorizontalDistance(pairedReactionOrigin, target.Position);
            var time = (float)after["timeMoved"] - (float)before["timeMoved"];
            var cost = (float)after["move"] - (float)before["move"];
            pairedReactionOperation["after"] = after;
            pairedReactionOperation["mountAfter"] = allocationTrace.Snapshot(horse);
            pairedReactionOperation["distance"] = distance;
            pairedReactionOperation["nativeTime"] = time;
            pairedReactionOperation["nativeCost"] = cost;
            pairedReactionOperation["result"] = pairedReactionMove.Result.ToString();
            pairedReactionOperation["mountOpportunityRules"] = ruleProbe.MountOpportunityAttackRuleCount - pairedReactionRulesBefore;
            pairedReactionOperation["rules"] = ruleProbe.CapturePairEvidence();
            allocationTrace.Record("reaction-enemy-move-completed", target, pairedReactionMove);
            RequirePaired(distance > 3.8f && time > 0f && Math.Abs(time - cost) < 0.02f &&
                pairedReactionMove.Result == Kingmaker.UnitLogic.Commands.Base.UnitCommand.ResultType.Success && !turn.EnabledFiveFootStep,
                "Reaction stimulus lacks completed native movement, time and cost.");
            RequirePaired(ruleProbe.MountOpportunityAttackRuleCount - pairedReactionRulesBefore == 1 &&
                horse.CombatState.AttackOfOpportunityCount == 0,
                "Native mount reaction was missing or duplicated after a second disengagement.");
            var mountBefore = pairedReactionOperation["mountBefore"];
            var mountAfter = pairedReactionOperation["mountAfter"];
            RequirePaired((float)mountBefore["standard"] == (float)mountAfter["standard"] &&
                (float)mountBefore["move"] == (float)mountAfter["move"],
                "Native opportunity attack changed the mount's ordinary action debt.");
            pairedReactionLeg++;
            if (pairedReactionLeg < 4) BeginPairedReactionMove();
            else pairedReactionEvidence["mountAfterConsumption"] = allocationTrace.Snapshot(horse);
            return true;
        }

        private void BeginPairedReactionMove()
        {
            pairedReactionOrigin = target.Position;
            pairedReactionOperation = new JObject { ["leg"] = pairedReactionLeg + 1,
                ["before"] = allocationTrace.Snapshot(target), ["mountBefore"] = allocationTrace.Snapshot(horse) };
            ((JArray)pairedReactionEvidence["operations"]).Add(pairedReactionOperation);
            // The target's AI choice is scripted. Native command admission,
            // pathing, turn ownership, movement debit and disengagement run normally.
            pairedReactionMove = new UnitMoveTo(pairedReactionLeg % 2 == 0 ? pairedReactionAway : pairedReactionHome);
            allocationTrace.Record("reaction-enemy-move-input", target, pairedReactionMove, "scripted-native-AI-command");
            target.Commands.Run(pairedReactionMove);
            pairedReactionFrame = Time.frameCount;
        }

        private void VerifyPairedReactionRefresh(JObject mount)
        {
            RequirePaired(pairedReactionLeg == 4 && pairedReactionEvidence["mountAfterConsumption"] != null,
                "The unrelated enemy turn did not exercise actual mount reaction consumption.");
            RequirePaired((int)mount["reactions"] == horse.CombatState.AttackOfOpportunityPerRound &&
                (float)mount["reactionCooldown"] == 0f && (int)mount["disengageTargets"] == 0,
                "Complete paired preparation did not renew the consumed native reaction state.");
            pairedReactionEvidence["mountAfterRefresh"] = mount.DeepClone();
            pairedReactionEvidence["refreshIdentity"] = combat.PairedActivationIdentity;
            pairedReactionEvidence["passed"] = true;
            pairedReactionRefreshed = true;
        }
    }
}
