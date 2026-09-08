using System;
using System.Linq;
using System.Reflection;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.Selection;
using Kingmaker.UI.SettingsUI;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        private bool pairedControlsStarted;
        private int pairedControlStage;
        private int pairedControlFrame;
        private TurnController pairedControlTurn;
        private UnitAttack pairedControlAttack;
        private JObject pairedControlBefore;
        private JObject pairedControlOperation;
        private JObject pairedControlEvidence;
        private NativeAutomaticEndProbe pairedAutomaticEndProbe;
        private float pairedNativeSetupRadius;

        private Vector3 FindPairedControlPoint(float minimumDisplacement, string evidenceKey)
        {
            var plans = new JArray();
            pairedNativeSetupRadius = float.PositiveInfinity;
            foreach (var actor in new[] { rider, horse })
            {
                var native = new UnitAttack(target); native.Init(actor);
                var ranges = native.CreateFullAttack().Select(attack => attack.WeaponRange).ToArray();
                RequirePaired(ranges.Length > 0, "Paired stationary fixture has no native attack plan.");
                var radius = horse.View.Corpulence + target.View.Corpulence + ranges.Min();
                pairedNativeSetupRadius = Math.Min(pairedNativeSetupRadius, radius);
                plans.Add(new JObject { ["actor"] = actor.UniqueId, ["weaponRanges"] = new JArray(ranges),
                    ["minimumRadius"] = radius });
            }
            observations[evidenceKey + "-plans"] = plans;
            // These probes require actual travel. A point inside the native
            // arrival radius can produce same-frame Success without any movement.
            return FindNativeAttackFixturePoint(horse, true, horse.Position, Math.Max(1f, minimumDisplacement),
                pairedNativeSetupRadius, evidenceKey);
        }

        private void BeginPairedControlProbe()
        {
            pairedControlsStarted = true;
            pairedControlStage = -3;
            pairedControlTurn = Game.Instance.TurnBasedCombatController.CurrentTurn;
            pairedControlBefore = RecordPairedTransition("ordinary-controls-setup-begin");
            pairedControlEvidence = new JObject { ["level"] = "NATIVE INTEGRATION",
                ["inputKind"] = "scripted-native-control-integration", ["operations"] = new JArray(),
                ["before"] = pairedControlBefore.DeepClone(), ["automaticEndInputCount"] = 0 };
            observations["pairedOrdinaryControls"] = pairedControlEvidence;
            ordinaryAttackTrace = new NativeOrdinaryAttackTrace(rider, horse, combat);
            ordinaryAttackTrace.BeginCase("paired-ordinary-stationary-setup");
            pairedAutomaticEndProbe = new NativeAutomaticEndProbe();
            SelectPairedControlActor(horse);
            ResetLeafClock();
        }

        private void SelectPairedControlActor(UnitEntityData actor)
        {
            SelectionManager.Instance.SelectUnit(actor.View, true, true, false);
            pairedControlFrame = Time.frameCount;
        }

        private bool PairedSelectionReady(UnitEntityData actor)
        {
            if (Time.frameCount < pairedControlFrame + 3) return false;
            RequirePaired(SelectionManager.Instance.SingleSelectedUnit == actor &&
                ReferenceEquals(Game.Instance.TurnBasedCombatController.CurrentTurn, pairedControlTurn),
                "Ordinary paired selection changed activation ownership or was overwritten by native Tick.");
            return PairedTransitionActorsIdle();
        }

        private void BeginPairedOrdinaryAttack(UnitEntityData actor, bool full)
        {
            ordinaryAttackTrace.BeginCase(actor == horse ? (full ? "paired-mount-full" : "paired-mount-single") : "paired-rider-full");
            ruleProbe.Arm(target, false);
            var context = actor == horse ? combat.PairedPartnerContext : pairedControlTurn;
            var stationary = new UnitAttack(target);
            stationary.Init(actor);
            var nativeRanges = stationary.CreateFullAttack().Select(attack => attack.WeaponRange).ToArray();
            var minimumRadius = horse.View.Corpulence + target.View.Corpulence + nativeRanges.Min();
            var distance = HorizontalDistance(horse.Position, target.Position);
            RequirePaired(stationary.IsUnitEnoughClose && (!full || distance <= minimumRadius),
                "Ordinary Full/Single fixture is outside an actual planned weapon's native reach.");
            pairedControlOperation = new JObject { ["actor"] = actor.UniqueId, ["full"] = full,
                ["before"] = RecordPairedTransition("ordinary-paired-attack-before"),
                ["selectedActor"] = SelectionManager.Instance.SingleSelectedUnit?.UniqueId,
                ["contextActor"] = context?.Unit.UniqueId, ["nativeWeaponRanges"] = new JArray(nativeRanges),
                ["nativeOriginCorpulence"] = horse.View.Corpulence, ["targetCorpulence"] = target.View.Corpulence,
                ["minimumNativeRadius"] = minimumRadius, ["nativeOriginDistance"] = distance };
            ((JArray)pairedControlEvidence["operations"]).Add(pairedControlOperation);
            using (var input = new NativeOrdinaryAttackInput(target))
            {
                var before = CaptureOrdinaryLiveState();
                for (var i = 0; i < 3; i++) input.Predict(context);
                pairedControlOperation["hoverPure"] = JToken.DeepEquals(before, CaptureOrdinaryLiveState());
                var cycles = 0;
                while (context.EnabledFullAttack != full && cycles++ < 8) { input.Click(button: 1); input.Predict(context); }
                pairedControlOperation["cursorCycles"] = cycles;
                pairedControlOperation["fullEnabled"] = context.EnabledFullAttack;
                pairedControlOperation["principalFullEnabled"] = pairedControlTurn.EnabledFullAttack;
                RequirePaired((bool)pairedControlOperation["hoverPure"] && context.EnabledFullAttack == full,
                    "Selected actor native prediction is impure or cannot select Full/Single.");
                RequirePaired(targetService.BeginExpectedAttackDispatch(target), "Ordinary paired target dispatch refused.");
                pairedControlOperation["clicked"] = input.Click();
                RequirePaired((bool)pairedControlOperation["clicked"], "Selected actor ordinary native attack input refused.");
            }
            pairedControlAttack = null;
            ResetLeafClock();
        }

        private bool FinishPairedOrdinaryAttack(UnitEntityData actor, bool full)
        {
            // Observe terminal cost before waiting for presentation to become
            // idle: native automatic completion may advance time in that wait.
            if (pairedControlOperation["after"] != null) return PairedTransitionActorsIdle();
            if (pairedControlAttack == null) pairedControlAttack = actor == horse
                ? ordinaryAttackTrace.LastStartedMountAttack : ordinaryAttackTrace.LastStartedRiderAttack;
            if (pairedControlAttack == null || !pairedControlAttack.IsFinished) return false;
            var after = RecordPairedTransition("ordinary-paired-attack-after");
            pairedControlOperation["after"] = after;
            pairedControlOperation["command"] = CaptureOrdinaryCommand(pairedControlAttack);
            pairedControlOperation["nativePlan"] = pairedControlAttack.AllAttacks.Count;
            pairedControlOperation["completed"] = pairedControlAttack.GetAttackIndex();
            pairedControlOperation["nativeFull"] = pairedControlAttack.IsFullAttack;
            pairedControlOperation["nativeSinglePrimary"] = pairedControlAttack.IsSingleAttack;
            pairedControlOperation["nativeRules"] = actor == horse ? ruleProbe.MountNonOpportunityAttackRuleCount : ruleProbe.RiderResolvedCount;
            var actorKey = actor == horse ? "mount" : "rider";
            var otherKey = actor == horse ? "rider" : "mount";
            RequirePaired(pairedControlAttack.Executor == actor && pairedControlAttack.Result == UnitCommand.ResultType.Success &&
                pairedControlAttack.IsFullAttack == full && !pairedControlAttack.IsSingleAttack &&
                pairedControlAttack.GetAttackIndex() == pairedControlAttack.AllAttacks.Count &&
                (int)pairedControlOperation["nativeRules"] == pairedControlAttack.GetAttackIndex() &&
                pairedControlAttack.AllAttacks.Count >= 1 && (full || pairedControlAttack.AllAttacks.Count == 1) &&
                (float)after[actorKey]["standard"] == 6f && (float)after[actorKey]["move"] == (full ? 3f : 0f) &&
                (float)after[otherKey]["standard"] == (float)pairedControlOperation["before"][otherKey]["standard"] &&
                (float)after[otherKey]["move"] == (float)pairedControlOperation["before"][otherKey]["move"],
                "Ordinary paired attack did not preserve native mode, complete sequence, rules or exact actor costs.");
            RequireNoPairedRefresh((JObject)pairedControlOperation["before"], after);
            return PairedTransitionActorsIdle();
        }

        private void TickPairedControls()
        {
            var game = Game.Instance;
            if (game.IsPaused) { game.IsPaused = false; return; }
            var turn = game.TurnBasedCombatController.CurrentTurn;
            pairedControlEvidence["stage"] = pairedControlStage;
            if (turn?.Unit == horse) throw new InvalidOperationException("Ordinary controls produced a duplicate independent mount turn.");
            if (pairedControlStage == -3)
            {
                if (!PairedSelectionReady(horse)) return;
                pairedTransitionTurn = pairedControlTurn;
                var destination = FindPairedControlPoint(0.25f, "paired-stationary-endpoints");
                BeginPairedTransitionMove(0f, false, "ordinary-stationary-setup", true, false, destination);
                pairedControlEvidence["setupMovement"] = pairedTransitionMove;
                RequirePaired((bool)pairedTransitionMove["clicked"] && (bool)pairedTransitionMove["admitted"],
                    "Mount-selected native terrain click did not admit the stationary setup command.");
                pairedControlStage = -2; return;
            }
            if (pairedControlStage == -2)
            {
                if (movementCommand != null && !movementCommand.IsFinished || !PairedTransitionActorsIdle()) return;
                FinishPairedTransitionMove(false, false);
                RequirePaired(HorizontalDistance(horse.Position, target.Position) <= pairedNativeSetupRadius,
                    "Native positioning did not reach adjacency for every planned weapon.");
                pairedControlEvidence["setupMovement"] = pairedTransitionMove.DeepClone();
                pairedTransitionMoves.Remove(pairedTransitionMove);
                EndAllocationNativeTurn(turn);
                pairedControlStage = -1; ResetLeafClock(); return;
            }
            if (pairedControlStage == -1)
            {
                if (ReferenceEquals(turn, pairedControlTurn)) { EndAllocationNativeTurn(turn); return; }
                if (turn == null || turn.Status != TurnController.TurnStatus.Preparing && !turn.IsActing) return;
                if (turn.Unit != rider) { EndAllocationNativeTurn(turn); return; }
                pairedControlTurn = turn;
                pairedControlBefore = RecordPairedTransition("ordinary-controls-begin");
                foreach (var actor in new[] { "rider", "mount" })
                    RequirePaired((float)pairedControlBefore[actor]["standard"] == 0f &&
                        (float)pairedControlBefore[actor]["move"] == 0f, "Stationary fixture did not wait for native refresh.");
                pairedControlEvidence["before"] = pairedControlBefore.DeepClone();
                SelectPairedControlActor(horse); pairedControlStage = 0; return;
            }
            if (pairedControlStage == 0)
            {
                if (!PairedSelectionReady(horse)) return;
                BeginPairedOrdinaryAttack(horse, true); pairedControlStage = 1; return;
            }
            if (pairedControlStage == 1)
            {
                if (!FinishPairedOrdinaryAttack(horse, true)) return;
                SelectPairedControlActor(rider); pairedControlStage = 2; return;
            }
            if (pairedControlStage == 2)
            {
                if (!PairedSelectionReady(rider)) return;
                BeginPairedOrdinaryAttack(rider, true); pairedControlStage = 3; return;
            }
            if (pairedControlStage == 3)
            {
                if (!FinishPairedOrdinaryAttack(rider, true)) return;
                pairedControlStage = 4; ResetLeafClock(); return;
            }
            if (pairedControlStage == 4 || pairedControlStage == 9)
            {
                if (ReferenceEquals(turn, pairedControlTurn)) return; // No End input in automatic stage4.
                if (turn == null || turn.Status != TurnController.TurnStatus.Preparing && !turn.IsActing) return;
                if (turn.Unit != rider) { EndAllocationNativeTurn(turn); return; }
                var refreshed = RecordPairedTransition(pairedControlStage == 4 ? "ordinary-auto-end-next-grant" : "ordinary-explicit-end-next-grant");
                var expected = pairedControlStage == 4 ? 1 : 2;
                foreach (var name in new[] { "rider", "mount" })
                    RequirePaired((float)refreshed[name]["standard"] == 0f && (float)refreshed[name]["move"] == 0f &&
                        (int)refreshed[name + "Clears"] == (int)pairedControlBefore[name + "Clears"] + expected &&
                        (int)refreshed[name + "Effects"] == (int)pairedControlBefore[name + "Effects"] + expected,
                        "Ordinary controls repeated or omitted preparation/effects at the next paired boundary.");
                pairedControlTurn = turn;
                if (pairedControlStage == 4)
                {
                    pairedControlEvidence["automaticRefresh"] = refreshed;
                    SelectPairedControlActor(horse); pairedControlStage = 5; return;
                }
                pairedControlEvidence["explicitRefresh"] = refreshed;
                pairedAutomaticEndProbe.Dispose();
                pairedControlEvidence["automaticEndSettingRestored"] = pairedAutomaticEndProbe.Restored;
                pairedAutomaticEndProbe = null;
                pairedControlEvidence["passed"] = true;
                AddRow("P03-paired-ordinary-controls", true,
                    "Native actor selection, Full/Single costs, mount movement, automatic and explicit completion.",
                    (JObject)pairedControlEvidence.DeepClone());
                pairedControlsStarted = false;
                BeginPairedRestrictionProbe(); return;
            }
            if (pairedControlStage == 5)
            {
                if (!PairedSelectionReady(horse)) return;
                BeginPairedOrdinaryAttack(horse, false); pairedControlStage = 6; return;
            }
            if (pairedControlStage == 6)
            {
                if (!FinishPairedOrdinaryAttack(horse, false)) return;
                pairedTransitionTurn = pairedControlTurn;
                var destination = FindPairedControlPoint(0.75f, "paired-residual-endpoints");
                BeginPairedTransitionMove(0f, false, "mount-selected-residual", true, false, destination);
                pairedControlEvidence["residualMovement"] = pairedTransitionMove;
                pairedControlStage = 7; return;
            }
            if (pairedControlStage == 7)
            {
                if (movementCommand != null && !movementCommand.IsFinished || !PairedTransitionActorsIdle()) return;
                FinishPairedTransitionMove(false, false);
                pairedControlEvidence["movement"] = pairedTransitionMove.DeepClone();
                pairedTransitionMoves.Remove(pairedTransitionMove);
                var before = CaptureOrdinaryLiveState();
                using (var input = new NativeOrdinaryAttackInput(target))
                { input.Predict(combat.PairedPartnerContext); pairedControlEvidence["excessAttackClicked"] = input.Click(); }
                pairedControlEvidence["excessBefore"] = before;
                pairedControlFrame = Time.frameCount;
                pairedControlStage = 8; return;
            }
            if (pairedControlStage == 8)
            {
                if (Time.frameCount < pairedControlFrame + 4 || !PairedTransitionActorsIdle()) return;
                var unchanged = ReferenceEquals(ordinaryAttackTrace.LastStartedMountAttack, pairedControlAttack) &&
                    horse.CombatState.Cooldown.StandardAction == 6f && rider.CombatState.Cooldown.StandardAction == 0f &&
                    horse.CombatState.Cooldown.MoveAction == (float)pairedControlEvidence["excessBefore"]["mount"]["move"];
                RequirePaired(unchanged, "Mount ordinary repeated input bypassed spent Standard or changed current debt.");
                pairedControlEvidence["excessAttackRejected"] = true;
                pairedControlEvidence["explicitEndBefore"] = RecordPairedTransition("ordinary-explicit-end-input");
                EndAllocationNativeTurn(turn);
                pairedControlStage = 9; ResetLeafClock();
            }
        }

        // Exact native AutoEndTurn is an input preference, not an actor resource.
        // Reuse the mode probe's verified nullable cache contract without touching
        // its persisted setting. Native HasExtraAction reads this preference.
        private sealed class NativeAutomaticEndProbe : IDisposable
        {
            private readonly SettingsEntityBool setting = SettingsRoot.Instance.AutoEndTurn;
            private readonly FieldInfo cache = typeof(SettingsEntityBool).GetField("m_Cached", BindingFlags.Instance | BindingFlags.NonPublic);
            private readonly object original;
            private readonly string persisted;
            internal bool Restored { get; private set; }
            internal NativeAutomaticEndProbe()
            {
                if (cache?.MetadataToken != 0x04002275 || cache.FieldType != typeof(bool?) ||
                    cache.Module.ModuleVersionId != new Guid("07fa1e4d-8618-41b3-9b8d-faa17d3b26f7"))
                    throw new MissingMemberException("Native automatic-End setting contract changed.");
                original = cache.GetValue(setting); persisted = setting.GetSavedValueString();
                cache.SetValue(setting, (bool?)true);
            }
            public void Dispose()
            {
                cache.SetValue(setting, original);
                Restored = Equals(cache.GetValue(setting), original) && setting.GetSavedValueString() == persisted;
                if (!Restored) throw new InvalidOperationException("Native automatic-End input preference was not restored exactly.");
            }
        }
    }
}
