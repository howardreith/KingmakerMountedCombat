using System;
using System.Linq;
using Kingmaker;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Commands;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // Native condition/cost continuation on the designated disposable pair.
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        private bool pairedRestrictionsStarted;
        private int pairedRestrictionStage;
        private int pairedRestrictionNext;
        private bool pairedRestrictionAwaiting;
        private bool pairedRestrictionAutomatic;
        private UnitCondition? pairedRestrictionCondition;
        private JObject pairedRestrictionEvidence;
        private JObject pairedRestrictionBoundary;
        private PairedConditionObserver pairedConditionObserver;
        private UnitAttack pairedRestrictionPreviousAttack;
        private int pairedRestrictionPreviousRules;

        private void CleanupPairedRestrictions()
        {
            SetPairedRestriction(null);
            if (pairedRestrictionEvidence != null)
                pairedRestrictionEvidence["ownedConditionRestored"] = !pairedRestrictionCondition.HasValue;
            if (pairedConditionObserver != null)
            {
                pairedRestrictionEvidence["nativeConditionEvents"] = pairedConditionObserver.Capture();
                pairedConditionObserver.Dispose(); pairedConditionObserver = null;
            }
        }

        private void BeginPairedRestrictionProbe()
        {
            pairedRestrictionsStarted = true;
            pairedRestrictionStage = 0;
            pairedControlTurn = Game.Instance.TurnBasedCombatController.CurrentTurn;
            pairedRestrictionEvidence = new JObject { ["level"] = "NATIVE INTEGRATION",
                ["inputKind"] = "scripted-native-control-integration", ["operations"] = new JArray(),
                ["movements"] = new JArray(), ["events"] = new JArray(), ["automaticEndInputCount"] = 0 };
            observations["pairedRestrictions"] = pairedRestrictionEvidence;
            // Reuse the established native attack/control helper with a separate
            // evidence object; the completed P03 object and row stay immutable.
            pairedControlEvidence = pairedRestrictionEvidence;
            pairedAutomaticEndProbe = new NativeAutomaticEndProbe();
            pairedConditionObserver = new PairedConditionObserver(rider, horse);
            RecordPairedRestriction("restrictions-begin");
            SelectPairedControlActor(horse);
            ResetLeafClock();
        }

        private JObject RecordPairedRestriction(string kind)
        {
            var sample = (JObject)RecordPairedTransition(kind).DeepClone();
            sample["condition"] = pairedRestrictionCondition?.ToString();
            sample["mountAble"] = horse.IsAbleToAct();
            sample["mountCanGetUp"] = combat.PairedPartnerCanGetUp;
            sample["mountCanStandUp"] = horse.Descriptor.State.CanStandUp;
            sample["mountCanMove"] = horse.Descriptor.State.CanMove;
            sample["mountConditions"] = new JArray(new[] { UnitCondition.Staggered, UnitCondition.Prone, UnitCondition.Stunned }
                .Where(condition => horse.Descriptor.State.HasCondition(condition)).Select(condition => condition.ToString()));
            ((JArray)pairedRestrictionEvidence["events"]).Add(sample);
            return sample;
        }

        private void SetPairedRestriction(UnitCondition? condition)
        {
            if (pairedRestrictionCondition.HasValue)
                horse.Descriptor.State.RemoveCondition(pairedRestrictionCondition.Value);
            pairedRestrictionCondition = null;
            if (!condition.HasValue) return;
            RequirePaired(!horse.Descriptor.State.HasCondition(condition.Value), "Restriction fixture would modify an existing condition.");
            horse.Descriptor.State.AddCondition(condition.Value);
            pairedRestrictionCondition = condition;
        }

        private void AwaitPairedRestrictionGrant(int next, bool automatic = false)
        {
            pairedRestrictionBoundary = RecordPairedRestriction(automatic ? "disabled-auto-end-wait" : "restriction-native-end-input");
            pairedRestrictionNext = next;
            pairedRestrictionAutomatic = automatic;
            pairedRestrictionAwaiting = true;
            if (!automatic) EndAllocationNativeTurn(pairedControlTurn);
            ResetLeafClock();
        }

        private bool WaitPairedRestrictionGrant(TurnController turn)
        {
            if (ReferenceEquals(turn, pairedControlTurn))
            {
                if (!pairedRestrictionAutomatic) EndAllocationNativeTurn(turn);
                return false;
            }
            if (turn == null || turn.Status != TurnController.TurnStatus.Preparing && !turn.IsActing) return false;
            RequirePaired(turn.Unit != horse, "Restriction probe produced a duplicate mount turn.");
            if (turn.Unit != rider) { EndAllocationNativeTurn(turn); return false; }
            var after = RecordPairedRestriction("restriction-native-next-grant");
            RequirePaired((string)after["identity"] != (string)pairedRestrictionBoundary["identity"], "Restriction refreshed the old grant.");
            foreach (var actor in new[] { "rider", "mount" })
                RequirePaired((float)after[actor]["standard"] == 0f && (float)after[actor]["move"] == 0f &&
                    (int)after[actor + "Clears"] == (int)pairedRestrictionBoundary[actor + "Clears"] + 1 &&
                    (int)after[actor + "Effects"] == (int)pairedRestrictionBoundary[actor + "Effects"] + 1,
                    "Restriction boundary omitted/replayed preparation or retained old debt.");
            pairedControlTurn = turn;
            pairedRestrictionStage = pairedRestrictionNext;
            pairedRestrictionAwaiting = false;
            SelectPairedControlActor(pairedRestrictionStage == 10 ? rider : horse);
            ResetLeafClock();
            return true;
        }

        private void SavePairedRestrictionMovement()
        {
            ((JArray)pairedRestrictionEvidence["movements"]).Add(pairedTransitionMove.DeepClone());
            pairedTransitionMoves.Remove(pairedTransitionMove);
        }

        private void TickPairedRestrictions()
        {
            var turn = Game.Instance.TurnBasedCombatController.CurrentTurn;
            pairedRestrictionEvidence["stage"] = pairedRestrictionStage;
            if (pairedRestrictionAwaiting) { WaitPairedRestrictionGrant(turn); return; }
            RequirePaired(turn?.Unit != horse, "Restriction probe exposed an independent mount activation.");
            if (pairedRestrictionStage == 0)
            {
                if (!PairedSelectionReady(horse)) return;
                SetPairedRestriction(UnitCondition.Staggered);
                RecordPairedRestriction("staggered-before-move");
                pairedTransitionTurn = turn;
                var destination = FindPairedControlPoint(0.25f, "paired-staggered-endpoints");
                BeginPairedTransitionMove(0f, false, "staggered-partial", true, false, destination);
                pairedRestrictionStage = 1; return;
            }
            if (pairedRestrictionStage == 1)
            {
                if (movementCommand != null && !movementCommand.IsFinished || !PairedTransitionActorsIdle()) return;
                FinishPairedTransitionMove(false, false); SavePairedRestrictionMovement();
                var range = new UnitAttack(target); range.Init(horse);
                RequirePaired(range.IsUnitEnoughClose && !horse.HasStandardAction(), "Staggered attack rejection lacks stationary native resource provenance.");
                pairedRestrictionPreviousAttack = ordinaryAttackTrace.LastStartedMountAttack;
                pairedRestrictionPreviousRules = ruleProbe.MountNonOpportunityAttackRuleCount;
                using (var input = new NativeOrdinaryAttackInput(target))
                { input.Predict(combat.PairedPartnerContext); pairedRestrictionEvidence["staggeredAttackClicked"] = input.Click(); }
                pairedControlFrame = Time.frameCount; pairedRestrictionStage = 2; return;
            }
            if (pairedRestrictionStage == 2)
            {
                if (Time.frameCount < pairedControlFrame + 4 || !PairedTransitionActorsIdle()) return;
                var sample = RecordPairedRestriction("staggered-move-rejects-standard");
                RequirePaired((float)sample["mount"]["standard"] == 0f && (float)sample["mount"]["move"] > 0f &&
                    !horse.HasStandardAction() && !combat.HasStockAttackIntent &&
                    ReferenceEquals(pairedRestrictionPreviousAttack, ordinaryAttackTrace.LastStartedMountAttack) &&
                    pairedRestrictionPreviousRules == ruleProbe.MountNonOpportunityAttackRuleCount,
                    "Staggered partial movement gained or retained an unaffordable Standard action.");
                sample["noNewAttackOrPendingIntent"] = true;
                sample["nativeRulesBefore"] = pairedRestrictionPreviousRules;
                sample["nativeRulesAfter"] = ruleProbe.MountNonOpportunityAttackRuleCount;
                AwaitPairedRestrictionGrant(3); return;
            }
            if (pairedRestrictionStage == 3)
            {
                if (!PairedSelectionReady(horse)) return;
                BeginPairedOrdinaryAttack(horse, false); pairedRestrictionStage = 4; return;
            }
            if (pairedRestrictionStage == 4)
            {
                if (!FinishPairedOrdinaryAttack(horse, false)) return;
                pairedTransitionTurn = turn;
                BeginPairedTransitionMove(0.5f, false, "staggered-standard-rejects-move", true);
                pairedControlFrame = Time.frameCount; pairedRestrictionStage = 5; return;
            }
            if (pairedRestrictionStage == 5)
            {
                if (Time.frameCount < pairedControlFrame + 4 || !PairedTransitionActorsIdle()) return;
                FinishPairedTransitionMove(false, true); SavePairedRestrictionMovement();
                RecordPairedRestriction("staggered-standard-rejects-move");
                SetPairedRestriction(null); AwaitPairedRestrictionGrant(6); return;
            }
            if (pairedRestrictionStage == 6)
            {
                if (!PairedSelectionReady(horse)) return;
                SetPairedRestriction(UnitCondition.Prone);
                pairedRestrictionStage = 7; ResetLeafClock(); return;
            }
            if (pairedRestrictionStage == 7)
            {
                if (!horse.Descriptor.State.Prone.Active) return;
                SetPairedRestriction(null);
                if (!combat.PairedPartnerCanGetUp) return;
                RecordPairedRestriction("native-get-up-input-before");
                pairedTransitionTurn = turn;
                // A nearby projected click can finish natively without needing
                // movement, leaving the actor prone. Require a real approach.
                var destination = FindPairedControlPoint(1f, "paired-get-up-endpoints");
                BeginPairedTransitionMove(0f, false, "native-get-up-input", true, false, destination);
                pairedTransitionMove["nativeEnoughCloseAtAdmission"] = movementCommand?.IsUnitEnoughClose;
                pairedTransitionMove["nativeApproachRadius"] = movementCommand?.ApproachRadius;
                pairedTransitionMove["nativeApproachDistance"] = movementCommand == null ? (float?)null :
                    HorizontalDistance(horse.Position, movementCommand.ApproachPoint);
                pairedRestrictionEvidence["getUpInput"] = pairedTransitionMove.DeepClone();
                pairedRestrictionEvidence["getUpFeedback"] = combat.LastFeedback;
                RequirePaired((bool)pairedTransitionMove["admitted"], "Native get-up terrain input was refused.");
                RequirePaired(!movementCommand.IsUnitEnoughClose,
                    "Get-up stimulus already satisfies the native movement destination.");
                pairedRestrictionStage = 8; ResetLeafClock(); return;
            }
            if (pairedRestrictionStage == 8)
            {
                if (horse.Descriptor.State.Prone.Active || !PairedTransitionActorsIdle()) return;
                var sample = RecordPairedRestriction("native-get-up-finished");
                var events = (JArray)pairedConditionObserver.Capture()["events"];
                var allowed = (float)sample["mount"]["measuredAllowedTime"] - (float)pairedTransitionMove["before"]["measuredAllowedTime"];
                pairedTransitionMove["after"] = sample["mount"].DeepClone();
                pairedTransitionMove["nativeMoveCost"] = (float)sample["mount"]["move"] - (float)pairedTransitionMove["before"]["move"];
                pairedTransitionMove["nativeAllowedTime"] = allowed;
                pairedTransitionMove["distance"] = HorizontalDistance(pairedTransitionOrigin, horse.Position);
                pairedTransitionMove["travelledDistance"] = (float)sample["mount"]["measuredTravelDistance"] - (float)pairedTransitionMove["before"]["measuredTravelDistance"];
                pairedTransitionMove["nativeShiftDistance"] = (float)sample["mount"]["measuredNativeShiftDistance"] - (float)pairedTransitionMove["before"]["measuredNativeShiftDistance"];
                pairedTransitionMove["result"] = movementCommand?.Result.ToString();
                RequirePaired(events.OfType<JObject>().Count(e => (string)e["kind"] == "native-get-up" &&
                    (string)e["actor"] == horse.UniqueId) == 1 &&
                    Math.Abs((float)pairedTransitionMove["nativeMoveCost"] - 3f - allowed) < 0.001f &&
                    (float)sample["mount"]["standard"] == 0f && (float)sample["rider"]["move"] == 0f,
                    "Native get-up did not charge one mount Move action through one callback.");
                SavePairedRestrictionMovement(); AwaitPairedRestrictionGrant(9); return;
            }
            if (pairedRestrictionStage == 9)
            {
                SetPairedRestriction(UnitCondition.Stunned);
                RequirePaired(!horse.IsAbleToAct(), "Disabled mount condition did not reach native action eligibility.");
                RecordPairedRestriction("disabled-mount-before-rider-full");
                SelectPairedControlActor(rider); pairedRestrictionStage = 10; return;
            }
            if (pairedRestrictionStage == 10)
            {
                if (!PairedSelectionReady(rider)) return;
                BeginPairedOrdinaryAttack(rider, true); pairedRestrictionStage = 11; return;
            }
            if (pairedRestrictionStage == 11)
            {
                if (!FinishPairedOrdinaryAttack(rider, true)) return;
                AwaitPairedRestrictionGrant(12, true); return;
            }
            if (pairedRestrictionStage == 12)
            {
                SetPairedRestriction(null);
                var restored = RecordPairedRestriction("native-restrictions-restored");
                RequirePaired(((JArray)restored["mountConditions"]).Count == 0 && !horse.Descriptor.State.Prone.Active,
                    "Owned native restriction fixture did not restore its condition state.");
                pairedRestrictionEvidence["ownedConditionRestored"] = true;
                pairedAutomaticEndProbe.Dispose();
                pairedRestrictionEvidence["automaticEndSettingRestored"] = pairedAutomaticEndProbe.Restored;
                pairedAutomaticEndProbe = null;
                pairedRestrictionEvidence["nativeConditionEvents"] = pairedConditionObserver.Capture();
                pairedConditionObserver.Dispose(); pairedConditionObserver = null;
                pairedRestrictionEvidence["passed"] = true;
                AddRow("P04-paired-native-restrictions", true,
                    "Native staggered costs/rejections, get-up callback cost, disabled mount completion and fresh grants.",
                    (JObject)pairedRestrictionEvidence.DeepClone());
                pairedRestrictionsStarted = false;
                BeginPairedTransitionProbe();
            }
        }
    }
}
