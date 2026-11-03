using System;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints.Root;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.Utility;
using Kingmaker.View;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // CHARGE-01 rows. Every charge here is the rider's ordinary ability click on the native target handler. The target's place is
    // disclosed setup; the one forced d20 (a natural 20 at the rule boundary, for the critical branch) is disclosed too and disarmed
    // after its row. The rows record native observations; the reader owns PASS/FAIL.
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        internal const string Sprint7ChargeRealTime = "sprint7-charge-rt", Sprint7ChargeTurnBased = "sprint7-charge-tb", Sprint7ChargeMountFirst = "sprint7-charge-tb-mount-first";
        internal static bool IsSprint7ChargeScenario(string scenario) => scenario == Sprint7ChargeRealTime || scenario == Sprint7ChargeTurnBased || scenario == Sprint7ChargeMountFirst;
        private bool IsSprint7Charge => IsSprint7ChargeScenario(request.Scenario);
        private bool Sprint7Tb => request.Scenario != Sprint7ChargeRealTime;
        private bool Sprint7RiderFirst => request.Scenario == Sprint7ChargeTurnBased;
        internal const int Sprint7SchemaVersion = 51;
        internal const double Sprint7ScenarioDeadlineSeconds = 300.0d, Sprint7HostDeadlineSeconds = 460.0d;
        private static readonly string[] Sprint7RealTimeCases = { "S7-rt-charge", "S7-rt-cancel" };
        private static readonly string[] Sprint7TurnBasedCases = { "S7-tb-charge" };
        private string[] Sprint7Cases => Sprint7Tb ? Sprint7TurnBasedCases : Sprint7RealTimeCases;
        private string Sprint7Case => Sprint7Cases[s7CaseIndex];
        private const float Sprint7StepBoundSeconds = 45f;
        private const int Sprint7SettleFrames = 20;

        private int s7CaseIndex, s7Stage = 1, s7Step, s7Frames, s7BoundStep = -1, s7RulesAtClick, s7InitiativeRider, s7InitiativeMount;
        private bool s7InitiativeSet;
        private float s7BoundAt, s7ClosestDistance = float.MaxValue, s7PeakSpeedOverride;
        private bool s7MountClick;
        private JObject s7Facts, s7Measure;
        private Sprint6RuleObserver s7Rules;
        private MountedCombatRuleProbe s7Die;
        private TurnController s7Turn;
        private MountedPairAttackCommand s7Command;

        private void BeginSprint7Charge()
        {
            if (!settings.EnablePairedActivation || settings.EnableUnifiedMountedTurn ||
                settings.EnablePairedCommandScheduler || settings.EnableDiagnosticOverlay || playerAction.OverlayPresent)
                throw new InvalidOperationException("Sprint 7 rows require the accepted single paired authority.");
            CaptureIdleFixturePartyForCleanup();
            castingTrace = new NativeCastingItemTrace(rider, horse);
            castingCosts = new NativeActorAllocationTrace(rider, horse, combat);
            s3Rules = new Chunk4IncomingRuleObserver(rider, horse);
            s7Rules = new Sprint6RuleObserver(rider, horse);
            var stock = Game.Instance.Player.PartyCharacters.Select(reference => reference.Value).FirstOrDefault(member => member == rider);
            s7Measure = new JObject { ["contract"] = "native-mounted-charge-v1", ["mode"] = Sprint7Tb ? "TB" : "RT", ["order"] = Sprint7Tb ? (Sprint7RiderFirst ? "rider-first" : "mount-first") : null,
                ["rider"] = rider.UniqueId, ["mount"] = horse.UniqueId, ["mountBlueprint"] = horse.Blueprint.AssetGuid, ["cases"] = new JArray(Sprint7Cases),
                ["attackRules"] = s7Rules.Attacks, ["attackD20IsForced"] = false, ["defaultFeatureEnabled"] = settings.EnableMountedCharge, ["riderFound"] = stock != null,
                ["policy"] = "The rider commits a native full-round action and one attack; the mount carries; each actor keeps its own remaining actions." };
            observations["sprint7Charge"] = s7Measure;
            castingOriginalSlots = new NativeCastingOriginalSlots(rider, castingTrace);
            s7Facts = new JObject { ["case"] = Sprint7Case, ["steps"] = new JObject() };
            step = Phase3dHorseStep.Phase3gControls;
            ResetLeafClock();
        }

        private JObject CaptureSprint7DeadlineProgress() => new JObject { ["stage"] = s7Stage, ["step"] = s7Step, ["caseIndex"] = s7CaseIndex,
            ["case"] = s7CaseIndex < Sprint7Cases.Length ? Sprint7Case : null, ["facts"] = s7Facts?.DeepClone(), ["state"] = Sprint7State() };

        private bool DrainSprint7Fixture()
        {
            if (s7Rules != null) { s7Rules.Dispose(); s7Rules = null; }
            if (s7Die != null) { s7Die.Dispose(); s7Die = null; }
            if (s7InitiativeSet)
            {
                rider.Stats.Initiative.BaseValue = s7InitiativeRider; horse.Stats.Initiative.BaseValue = s7InitiativeMount;
                s7InitiativeSet = false;
            }
            return DrainSprint4Fixture();
        }

        private Buff Sprint7Buff() => rider.Buffs.GetFact(BlueprintRoot.Instance.SystemMechanics.ChargeBuff) as Buff;

        private JObject Sprint7State()
        {
            var turn = Game.Instance.TurnBasedCombatController.CurrentTurn;
            var buff = rider == null ? null : Sprint7Buff();
            var agent = horse?.View?.AgentASP;
            var partner = combat.TurnParticipant?.PartnerContext;
            return new JObject { ["frame"] = Time.frameCount, ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks,
                ["riderStandard"] = rider.CombatState.Cooldown.StandardAction, ["riderMove"] = rider.CombatState.Cooldown.MoveAction, ["riderSwift"] = rider.CombatState.Cooldown.SwiftAction,
                ["mountStandard"] = horse.CombatState.Cooldown.StandardAction, ["mountMove"] = horse.CombatState.Cooldown.MoveAction, ["mountSwift"] = horse.CombatState.Cooldown.SwiftAction,
                ["partnerTimeMoved"] = partner?.TimeMoved, ["partnerTimeForced"] = partner?.TimeMovedInForceMode, ["partnerCanMove"] = partner == null ? (bool?)null : partner.HasMovement(),
                ["turnUnit"] = turn?.Unit?.UniqueId, ["turnStatus"] = turn?.Status.ToString(), ["round"] = Game.Instance.TurnBasedCombatController.RoundNumber,
                ["activation"] = combat.TurnParticipant?.ActivationIdentity, ["activationSequence"] = combat.TurnParticipant?.ActivationSequence, ["riderAc"] = rider.Stats.AC.ModifiedValue, ["mountAc"] = horse.Stats.AC.ModifiedValue,
                ["buff"] = buff != null, ["buffSecondsLeft"] = buff == null ? (double?)null : buff.TimeLeft.TotalSeconds, ["buffCount"] = rider.Buffs.Enumerable.Count(fact => fact.Blueprint == BlueprintRoot.Instance.SystemMechanics.ChargeBuff),
                ["riderCharging"] = rider.Descriptor.State.IsCharging, ["mountCharging"] = agent?.IsCharging, ["speedOverride"] = agent?.MaxSpeedOverride, ["mountMoving"] = agent?.IsReallyMoving,
                ["mountPosition"] = CapturePosition(horse.Position), ["targetPosition"] = target == null ? null : CapturePosition(target.Position),
                ["distance"] = target == null ? (float?)null : HorizontalDistance(horse.Position, target.Position), ["riderCommandsEmpty"] = rider.Commands.Empty, ["mountCommandsEmpty"] = horse.Commands.Empty,
                ["chargeOwned"] = combat.HasChargeOwnership, ["activeCommand"] = combat.HasActiveCommand, ["mounted"] = relationship.State == RelationshipState.Mounted,
                ["feedback"] = combat.LastFeedback, ["refusal"] = combat.LastMountedChargeRefusal, ["admitted"] = combat.MountedChargeAdmittedCount, ["refused"] = combat.MountedChargeRefusedCount };
        }

        private JObject Sprint7Step(string name)
        {
            var record = Sprint7State();
            ((JObject)s7Facts["steps"])[name] = record;
            return record;
        }

        private void FinishSprint7Row(bool passed, string detail)
        {
            s7Facts["after"] = Sprint7State();
            AddRow(Sprint7Case, passed, detail, s7Facts);
            s7CaseIndex++; s7Step = 0;
            if (s7CaseIndex == Sprint7Cases.Length || !passed) { BeginCleanup(); return; }
            s7Facts = new JObject { ["case"] = Sprint7Case, ["steps"] = new JObject() };
            ResetLeafClock();
        }

        private void FailSprint7Row(string why) { s7Facts["failure"] = why; FinishSprint7Row(false, why); }

        private void TickSprint7Charge()
        {
            var game = Game.Instance; var turn = game.TurnBasedCombatController.CurrentTurn;
            if (game.IsPaused) { game.IsPaused = false; return; }
            CastingMeasurement["progress"] = new JObject { ["stage"] = s7Stage, ["caseIndex"] = s7CaseIndex, ["step"] = s7Step };
            if (s7Stage == 1)
            {
                if (!rider.Commands.Empty || !horse.Commands.Empty || rider.AreHandsBusyWithAnimation) return;
                SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                var isolationReady = PrepareUnmountedHorseAiIsolation();
                var pairMounted = relationship.State == RelationshipState.Mounted;
                if (CastingEntryMayRequestMount(true, pairMounted, isolationReady, s7MountClick))
                    s7MountClick = TryNativeAbilityTargetClick(nativeControls.MountAbility, horse, "sprint7-exploration-mount");
                if (!CastingEntryReady(true, pairMounted, isolationReady)) return;
                s7Step = 0; s7BoundStep = -1; s7Stage = 2; ResetLeafClock(); return;
            }
            if (s7Step != s7BoundStep) { s7BoundStep = s7Step; s7BoundAt = Time.time; s7Frames = 0; ResetLeafClock(); }
            else if (Time.time - s7BoundAt > Sprint7StepBoundSeconds) { FailSprint7Row("Step " + s7Step + " exceeded its bound."); return; }
            targetService?.ObserveTargetLifeState(); targetService?.RefreshBidirectionalCombatMemoryLease();
            switch (Sprint7Case)
            {
                case "S7-rt-charge": TickSprint7RealTime(); return;
                case "S7-rt-cancel": TickSprint7Cancel(); return;
                case "S7-tb-charge": TickSprint7TurnBased(turn); return;
                default: throw new InvalidOperationException("Unknown Sprint 7 case " + Sprint7Case);
            }
        }

        // ---- input and placement ----------------------------------------------------------------------------------------
        private JObject Sprint7Click(string name)
        {
            var refusedBefore = nativeControls.NativeRefusalCount; var castsBefore = nativeControls.NativeCastRequestCount;
            var availability = combat.ObserveMountedChargeTarget(rider, target);
            var before = Sprint7State();
            var ability = rider.Descriptor.Abilities.GetAbility(nativeControls.MountedChargeAbility)?.Data;
            var record = new JObject { ["abilityPresent"] = ability != null, ["available"] = ability?.IsAvailableForCast, ["canTarget"] = ability?.CanTarget(target),
                ["unavailableReason"] = ability?.GetUnavailableReason(), ["policyAllowed"] = availability.IsAllowed, ["policyReason"] = availability.Reason, ["policyCode"] = availability.RejectionCode?.ToString(), ["before"] = before };
            var handler = Game.Instance.SelectedAbilityHandler; bool clicked;
            try { clicked = Sprint5Fixture.Click(nativeControls, rider, nativeControls.MountedChargeAbility, target); }
            finally { handler?.DropAbility(); }
            record["clicked"] = clicked; record["castDelta"] = nativeControls.NativeCastRequestCount - castsBefore; record["refusalDelta"] = nativeControls.NativeRefusalCount - refusedBefore;
            record["shell"] = combat.OwnedChargeShell != null; record["shellTarget"] = combat.OwnedChargeShell?.Target?.Unit?.UniqueId;
            record["afterClick"] = Sprint7State();
            ((JObject)s7Facts["steps"])[name] = record;
            return record;
        }

        // Disclosed setup: the hostile is placed on the nearest clear lane. Nothing about the lane is changed.
        private bool Sprint7PlaceOnLane(float[] radii)
        {
            var origin = horse.Position;
            foreach (var radius in radii)
                for (var angle = 0; angle < 360; angle += 15)
                {
                    var wanted = origin + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * radius;
                    var point = ObstacleAnalyzer.GetNearestNode(wanted).clampedPosition;
                    if ((point.To2D() - wanted.To2D()).sqrMagnitude > 0.09f || ObstacleAnalyzer.TraceAlongNavmesh(origin, point) != point) continue;
                    target.Translocate(point, null);
                    if (MountedChargeGeometry.FootprintClear(horse, rider, target) && !MountedChargeGeometry.LandingBlocked(horse, rider, target) && target.IsVisibleForPlayer)
                    {
                        s7Facts["lane"] = new JObject { ["point"] = CapturePosition(point), ["radius"] = radius, ["angle"] = angle };
                        return true;
                    }
                }
            return false;
        }

        // A point the navmesh can reach but the straight line cannot, when this fixture has one. The refusal is real only if it is the route's.
        private bool Sprint7PlaceBehindObstruction()
        {
            var origin = horse.Position;
            foreach (var radius in new[] { 10f, 13f, 16f, 20f })
                for (var angle = 0; angle < 360; angle += 15)
                {
                    var wanted = origin + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * radius;
                    var point = ObstacleAnalyzer.GetNearestNode(wanted).clampedPosition;
                    if ((point.To2D() - wanted.To2D()).sqrMagnitude > 0.09f || ObstacleAnalyzer.TraceAlongNavmesh(origin, point) == point) continue;
                    target.Translocate(point, null);
                    s7Facts["obstruction"] = new JObject { ["none"] = false, ["point"] = CapturePosition(point), ["radius"] = radius, ["angle"] = angle };
                    return true;
                }
            s7Facts["obstruction"] = new JObject { ["none"] = true };
            return false;
        }

        private void Sprint7ObserveTravel()
        {
            var distance = HorizontalDistance(horse.Position, target.Position);
            if (distance < s7ClosestDistance) s7ClosestDistance = distance;
            var speed = horse.View.AgentASP.MaxSpeedOverride ?? 0f;
            if (speed > s7PeakSpeedOverride) s7PeakSpeedOverride = speed;
            s7Command = combat.OwnedChargeCommand ?? s7Command;
            if (s7Rules.Attacks.Count > s7RulesAtClick && s7Facts["atAttack"] == null) s7Facts["atAttack"] = Sprint7State();
            if (s7Command != null && s7Facts["whileCharging"] == null && horse.View.AgentASP.IsCharging) s7Facts["whileCharging"] = Sprint7State();
        }

        private bool Sprint7ChargeTerminal() => s7Command != null && s7Command.IsFinished && !combat.HasChargeOwnership && rider.Commands.Empty && horse.Commands.Empty && !rider.AreHandsBusyWithAnimation;

        private void Sprint7RecordTerminal(string name)
        {
            s7Facts[name] = new JObject { ["state"] = Sprint7State(), ["sequence"] = s7Command?.ChargeSequence, ["sequenceLawful"] = s7Command?.ChargeSequenceLawful, ["result"] = s7Command?.Result.ToString(),
                ["lease"] = s7Command?.CaptureChargeLeaseEvidence(), ["ownership"] = combat.LastDrainedChargeOwnership?.DeepClone(), ["revalidationFailed"] = s7Command?.ChargeRevalidationFailed,
                ["revalidationReason"] = s7Command?.ChargeRevalidationFailureReason, ["outcomeRejection"] = combat.LastFeedback, ["rules"] = s7Rules.Attacks.Count - s7RulesAtClick,
                ["closestDistance"] = s7ClosestDistance, ["peakSpeedOverride"] = s7PeakSpeedOverride, ["mountCombatSpeed"] = horse.CombatSpeedMps,
                ["riderWeaponRange"] = rider.GetFirstWeapon()?.AttackRange.Meters, ["mountCorpulence"] = horse.View.Corpulence, ["targetCorpulence"] = target.View.Corpulence };
        }

        private void Sprint7ResetTravel() { s7ClosestDistance = float.MaxValue; s7PeakSpeedOverride = 0f; s7RulesAtClick = s7Rules.Attacks.Count; s7Command = null; }

        // The ordinary attack: the native unit click handler, armed by nothing.
        private bool Sprint7OrdinaryClick(UnitEntityData actor)
        {
            SelectionManager.Instance.SelectUnit(actor.View, true, true, false);
            return new ClickUnitHandler().OnClick(target.View.gameObject, target.Position, 0, false, false);
        }

        // ---- real time --------------------------------------------------------------------------------------------------
        private void TickSprint7RealTime()
        {
            switch (s7Step)
            {
                case 0:
                    BeginTarget(10f, "s7-rt"); ruleProbe.Arm(target, false); s7Rules.Watch(target);
                    s7Measure["hostileAttackSetup"] = 0; s7Step = 1; return;
                case 1: if (!rider.IsInCombat || !horse.IsInCombat || !rider.CombatState.CanActInCombat || !target.IsVisibleForPlayer) return; s7Step = 2; return;
                // Too near: refused by the shared decision, nothing spent.
                case 2:
                    target.Translocate(FindWalkablePoint(horse.Position, 3f, .75f), null); s7Step = 3; return;
                case 3:
                    if (++s7Frames < Sprint7SettleFrames) return;
                    Sprint7Click("tooNear"); s7Step = 4; return;
                // A blocked straight line where the fixture has one.
                case 4:
                    s7Step = Sprint7PlaceBehindObstruction() ? 5 : 6; return;
                case 5:
                    if (++s7Frames < Sprint7SettleFrames) return;
                    Sprint7Click("obstructed"); s7Step = 6; return;
                // The ordinary positive charge, with the natural 20 on this attack alone.
                case 6:
                    if (!Sprint7PlaceOnLane(new[] { 12f, 10f, 14f, 9f, 16f, 8f })) { FailSprint7Row("No clear charge lane exists at this fixture."); return; }
                    s7Step = 7; return;
                case 7:
                    if (++s7Frames < Sprint7SettleFrames || !rider.Commands.Empty) return;
                    if (rider.CombatState.HasCooldownForCommand(UnitCommand.CommandType.Standard)) return;
                    s7Die = new MountedCombatRuleProbe(); s7Die.Arm(rider, horse, rider, target, 20);
                    s7Measure["attackD20IsForced"] = true;
                    Sprint7ResetTravel();
                    if (Sprint7Click("charge").Value<bool>("clicked") != true) { FailSprint7Row("The ordinary charge click was refused: " + combat.LastMountedChargeRefusal); return; }
                    s7Step = 8; return;
                case 8:
                    Sprint7ObserveTravel();
                    if (!Sprint7ChargeTerminal()) return;
                    Sprint7RecordTerminal("terminal"); s7Facts["forcedDice"] = s7Die.ForcedD20Count; s7Die.Dispose(); s7Die = null; s7Step = 9; return;
                // While spent the same click is refused; the native penalty then ends with its native round.
                case 9:
                    Sprint7Click("spentRefused"); s7Step = 10; return;
                case 10:
                    if (Sprint7Buff() != null || rider.CombatState.HasCooldownForCommand(UnitCommand.CommandType.Standard)) return;
                    Sprint7Step("buffExpired"); s7Step = 11; return;
                // The ordinary attack neighbour: no charge flag and none of its bonus.
                case 11:
                    if (!rider.Commands.Empty || rider.AreHandsBusyWithAnimation) return;
                    s7RulesAtClick = s7Rules.Attacks.Count;
                    if (!Sprint7OrdinaryClick(rider)) { FailSprint7Row("The ordinary attack neighbour was refused."); return; }
                    s7Step = 12; return;
                case 12:
                    if (s7Rules.Attacks.Count <= s7RulesAtClick || !rider.Commands.Empty) return;
                    Sprint7Step("ordinaryAttack"); s7Facts["ordinaryRule"] = s7Rules.Attacks[s7RulesAtClick].DeepClone();
                    if (combat.HasActiveCommand) SelectionManager.Instance.Stop();
                    FinishSprint7Row(true, "Real-time charge observations recorded."); return;
            }
        }

        // Stop after commitment, then the target's death mid-charge. Each keeps the cost, delivers nothing and leaves no effect.
        private void TickSprint7Cancel()
        {
            switch (s7Step)
            {
                case 0:
                    if (!rider.Commands.Empty || combat.HasActiveCommand || rider.CombatState.HasCooldownForCommand(UnitCommand.CommandType.Standard)) return;
                    SelectionManager.Instance.Stop();
                    if (!Sprint7PlaceOnLane(new[] { 14f, 12f, 16f, 10f })) { FailSprint7Row("No clear lane for the Stop control."); return; }
                    s7Step = 1; return;
                case 1:
                    if (++s7Frames < Sprint7SettleFrames) return;
                    Sprint7ResetTravel();
                    if (Sprint7Click("stopCharge").Value<bool>("clicked") != true) { FailSprint7Row("The Stop control's charge was refused: " + combat.LastMountedChargeRefusal); return; }
                    s7Step = 2; return;
                case 2:
                    Sprint7ObserveTravel();
                    if (s7Command == null || !horse.View.AgentASP.IsCharging || !horse.View.AgentASP.IsReallyMoving) return;
                    if (++s7Frames < 3) return;
                    Sprint7Step("beforeStop"); SelectionManager.Instance.SelectUnit(rider.View, true, true, false); SelectionManager.Instance.Stop();
                    s7Frames = 0; s7Step = 3; return;
                case 3:
                    Sprint7ObserveTravel();
                    if (!Sprint7ChargeTerminal() || ++s7Frames < Sprint7SettleFrames) return;
                    Sprint7RecordTerminal("stopTerminal"); s7Step = 4; return;
                case 4:
                    if (!rider.Commands.Empty || rider.CombatState.HasCooldownForCommand(UnitCommand.CommandType.Standard)) return;
                    if (!Sprint7PlaceOnLane(new[] { 14f, 12f, 16f, 10f })) { FailSprint7Row("No clear lane for the target-loss control."); return; }
                    s7Step = 5; return;
                case 5:
                    if (++s7Frames < Sprint7SettleFrames) return;
                    Sprint7ResetTravel();
                    if (Sprint7Click("deathCharge").Value<bool>("clicked") != true) { FailSprint7Row("The target-loss control's charge was refused: " + combat.LastMountedChargeRefusal); return; }
                    s7Step = 6; return;
                case 6:
                    Sprint7ObserveTravel();
                    if (s7Command == null || !horse.View.AgentASP.IsCharging || !horse.View.AgentASP.IsReallyMoving) return;
                    if (++s7Frames < 3) return;
                    Sprint7Step("beforeDeath");
                    target.Descriptor.State.AllowDyingCondition.ReleaseAll();
                    Rulebook.Trigger(new RuleDealDamage(rider, target, new DamageBundle(new DirectDamage(new DiceFormula(0, DiceType.Zero), target.MaxHP + target.Stats.TemporaryHitPoints.ModifiedValue + 50))));
                    s7Facts["targetDeathState"] = new JObject { ["dead"] = target.Descriptor.State.IsDead, ["conscious"] = target.Descriptor.State.IsConscious };
                    s7Frames = 0; s7Step = 7; return;
                case 7:
                    Sprint7ObserveTravel();
                    if (!Sprint7ChargeTerminal() || ++s7Frames < Sprint7SettleFrames) return;
                    Sprint7RecordTerminal("deathTerminal");
                    FinishSprint7Row(true, "Real-time cancellation observations recorded."); return;
            }
        }

        // ---- turn-based -------------------------------------------------------------------------------------------------
        private bool Sprint7RiderInput(TurnController turn) => turn?.Unit == rider && Chunk7NativeFixture.ReadyForTurnInput(turn) && rider.Commands.Empty && horse.Commands.Empty && !rider.AreHandsBusyWithAnimation;

        private void TickSprint7TurnBased(TurnController turn)
        {
            switch (s7Step)
            {
                case 0:
                    if (turnBasedModeProbe == null) turnBasedModeProbe = new NativeModeTransitionProbe(true);
                    if (!turnBasedModeProbe.TemporaryValueIsCurrent) { turnBasedModeProbe.DispatchTemporaryValueIfRequired(); return; }
                    if (pairedAutomaticEndProbe == null) pairedAutomaticEndProbe = new NativeAutomaticEndProbe(false);
                    // Disclosed setup: the two actors' own native initiative, before combat, as the allocation rows set it.
                    s7InitiativeRider = rider.Stats.Initiative.BaseValue; s7InitiativeMount = horse.Stats.Initiative.BaseValue; s7InitiativeSet = true;
                    rider.Stats.Initiative.BaseValue = Sprint7RiderFirst ? 40 : -40; horse.Stats.Initiative.BaseValue = Sprint7RiderFirst ? -40 : 40;
                    BeginTarget(10f, "s7-tb"); ruleProbe.Arm(target, false); s7Rules.Watch(target);
                    castingCosts.BeginEncounter(request.RunId + ":s7-tb");
                    s7Step = 1; return;
                case 1:
                    if (!rider.IsInCombat || !horse.IsInCombat) return;
                    if (WaitForNativeCastingPrincipal(true, turn?.Unit == rider, rider.CombatState.CanActInCombat, () => EndCastingNativeTurn(turn))) return;
                    if (!Sprint7RiderInput(turn)) return;
                    s7Turn = turn; s7Facts["order"] = new JObject { ["riderInitiative"] = rider.CombatState.Initiative, ["mountInitiative"] = horse.CombatState.Initiative };
                    target.Translocate(FindWalkablePoint(horse.Position, 3f, .75f), null); s7Step = 2; return;
                // Too near, on the rider's own turn: refused, and the turn's actions are untouched.
                case 2:
                    if (++s7Frames < Sprint7SettleFrames || !Sprint7RiderInput(turn)) return;
                    Sprint7Click("tooNear"); s7Step = 3; return;
                case 3:
                    if (!Sprint7PlaceOnLane(new[] { 12f, 10f, 14f, 9f, 16f, 8f })) { FailSprint7Row("No clear charge lane exists at this fixture."); return; }
                    s7Step = 4; return;
                // The ordinary charge on the paired rider turn, with a natural die.
                case 4:
                    if (++s7Frames < Sprint7SettleFrames || !Sprint7RiderInput(turn)) return;
                    Sprint7ResetTravel();
                    if (Sprint7Click("charge").Value<bool>("clicked") != true) { FailSprint7Row("The ordinary charge click was refused: " + combat.LastMountedChargeRefusal); return; }
                    s7Step = 5; return;
                case 5:
                    Sprint7ObserveTravel();
                    if (!Sprint7ChargeTerminal()) return;
                    Sprint7RecordTerminal("terminal"); s7Facts["sameTurn"] = ReferenceEquals(turn, s7Turn); s7Step = 6; return;
                // Spent on the same turn: refused. The mount keeps its own Standard action and its own ordinary attack.
                case 6:
                    Sprint7Click("spentRefused");
                    s7Facts["partnerMovementAfter"] = new JObject { ["canMovePairedMount"] = combat.TurnParticipant?.CanMovePairedMount(turn) }; s7Step = 7; return;
                case 7:
                    s7RulesAtClick = s7Rules.Attacks.Count;
                    s7Facts["mountOwnAttack"] = new JObject { ["clicked"] = Sprint7OrdinaryClick(horse), ["before"] = Sprint7State() }; s7Frames = 0; s7Step = 8; return;
                case 8:
                    if (s7Rules.Attacks.Count > s7RulesAtClick || ++s7Frames > 180)
                    {
                        if (!horse.Commands.Empty && s7Frames <= 180) return;
                        Sprint7Step("afterMountOwnAttack");
                        s7Facts["mountOwnAttackRules"] = new JArray(s7Rules.Attacks.Skip(s7RulesAtClick).Select(rule => rule.DeepClone()));
                        SelectionManager.Instance.SelectUnit(rider.View, true, true, false); s7Step = 9;
                    }
                    return;
                // No workaround: the pair's next native activation, its preparation counted, then the already-spent partner.
                case 9:
                    if (turn?.Unit == rider && ReferenceEquals(turn, s7Turn)) { if (rider.Commands.Empty && horse.Commands.Empty) EndCastingNativeTurn(turn); return; }
                    if (turn?.Unit != rider) { EndCastingNativeTurn(turn); return; }
                    if (!Sprint7RiderInput(turn)) return;
                    s7Turn = turn; Sprint7Step("nextTurn"); s7Step = 10; return;
                case 10:
                    s7RulesAtClick = s7Rules.Attacks.Count;
                    if (!Sprint7OrdinaryClick(horse)) { FailSprint7Row("The mount's attack for the spent-partner control was refused."); return; }
                    s7Frames = 0; s7Step = 11; return;
                case 11:
                    if (s7Rules.Attacks.Count <= s7RulesAtClick || !horse.Commands.Empty || horse.AreHandsBusyWithAnimation) return;
                    SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                    if (!Sprint7PlaceOnLane(new[] { 12f, 10f, 14f, 9f, 16f, 8f })) { FailSprint7Row("No clear lane for the spent-partner control."); return; }
                    s7Step = 12; return;
                case 12:
                    if (++s7Frames < Sprint7SettleFrames) return;
                    Sprint7Click("partnerSpentRefused"); s7Step = 13; return;
                // Charging round after round: the next renewed turn charges again, whatever the native round left.
                case 13:
                    if (ReferenceEquals(turn, s7Turn)) { if (rider.Commands.Empty && horse.Commands.Empty && !horse.AreHandsBusyWithAnimation) EndCastingNativeTurn(turn); return; }
                    if (turn?.Unit != rider) { EndCastingNativeTurn(turn); return; }
                    if (!Sprint7RiderInput(turn)) return;
                    s7Turn = turn; s7Step = 14; return;
                case 14:
                    if (!Sprint7PlaceOnLane(new[] { 12f, 10f, 14f, 9f, 16f, 8f })) { FailSprint7Row("No clear lane for the second charge."); return; }
                    s7Step = 15; return;
                case 15:
                    if (++s7Frames < Sprint7SettleFrames || !Sprint7RiderInput(turn)) return;
                    Sprint7ResetTravel(); s7Facts["secondWhileCharging"] = null;
                    if (Sprint7Click("secondCharge").Value<bool>("clicked") != true) { FailSprint7Row("The next round's charge was refused: " + combat.LastMountedChargeRefusal); return; }
                    s7Step = 16; return;
                case 16:
                    Sprint7ObserveTravel();
                    if (!Sprint7ChargeTerminal()) return;
                    Sprint7RecordTerminal("secondTerminal");
                    FinishSprint7Row(true, "Turn-based charge observations recorded."); return;
            }
        }
    }
}
