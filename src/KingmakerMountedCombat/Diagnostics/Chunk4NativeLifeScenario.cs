using System;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using KingmakerMountedCombat.Domain;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        internal static bool IsChunk4NativeLifeScenario(string scenario) => scenario == "chunk4-rider-incapacitation-tb" ||
            scenario == "chunk4-rider-death-tb" || scenario == "chunk4-mount-death-tb";
        private bool IsChunk4NativeLife => IsChunk4NativeLifeScenario(request.Scenario);
        private bool Chunk4LifeIncapacitation => request.Scenario == "chunk4-rider-incapacitation-tb";
        private bool Chunk4LifeMount => request.Scenario == "chunk4-mount-death-tb";
        private UnitEntityData Chunk4LifeSubject => Chunk4LifeMount ? horse : rider;
        private UnitEntityData Chunk4LifeSurvivor => Chunk4LifeMount ? rider : horse;
        private string Chunk4LifeId => Chunk4LifeIncapacitation ? "C4-LIFE-rider-incapacitation" :
            Chunk4LifeMount ? "C4-LIFE-mount-death-live-command" : "C4-LIFE-rider-death-live-command";
        private int chunk4LifeStage;
        private bool chunk4LifeMountInput;
        private UnitMoveTo chunk4LifeMove;
        private UnitAttack chunk4LifeFirst;
        private UnitAttack chunk4LifeLive;
        private TurnController chunk4LifeTurn;
        private TurnController chunk4LifeUnrelatedTurn;
        private int chunk4LifeUnrelatedCount;
        private int chunk4LifeRiderGrants;
        private int chunk4LifeMountGrants;
        private JObject chunk4LifeEvidence;
        private PairedConditionObserver chunk4LifeObserver;
        private Chunk4IncomingRuleObserver chunk4IncomingObserver;

        private void BeginChunk4NativeLife()
        {
            if (!settings.EnablePairedActivation || settings.EnableUnifiedMountedTurn || settings.EnablePairedCommandScheduler ||
                settings.EnableDiagnosticOverlay || playerAction.OverlayPresent)
                throw new InvalidOperationException("Native life fixture requires the accepted paired authority.");
            CaptureIdleFixturePartyForCleanup();
            chunk4LifeEvidence = new JObject { ["level"] = "NATIVE INTEGRATION", ["caseId"] = Chunk4LifeId, ["mode"] = "TB",
                ["inputKind"] = "native-primary-controls-and-labelled-native-damage-effect", ["damageDispatches"] = 0,
                ["incapacitation"] = Chunk4LifeIncapacitation, ["subject"] = Chunk4LifeSubject.UniqueId,
                ["survivor"] = Chunk4LifeSurvivor.UniqueId, ["unrelatedTurns"] = new JArray() };
            observations["chunk4NativeLife"] = chunk4LifeEvidence;
            observations["chunk4NativeEffectInventory"] = Chunk4NativeEffectInventory.Capture(rider, horse);
            if (!Chunk4LifeSubject.Descriptor.State.IsConscious || Chunk4LifeSubject.Descriptor.IsEssentialForGame ||
                Chunk4LifeSubject == Game.Instance.Player.MainCharacter.Value ||
                Chunk4LifeSubject.Descriptor.State.Immortality || Chunk4LifeIncapacitation && !Chunk4LifeSubject.Descriptor.State.AllowDyingCondition)
                throw new InvalidOperationException("Disposable actor's real life policy does not permit the requested native life stimulus.");
            ordinaryAttackTrace = new NativeOrdinaryAttackTrace(rider, horse, combat, () => relationship.State.ToString());
            allocationTrace = new NativeActorAllocationTrace(rider, horse, combat);
            allocationTrace.BeginEncounter(Chunk4LifeId);
            chunk4LifeObserver = new PairedConditionObserver(rider, horse);
            chunk4IncomingObserver = new Chunk4IncomingRuleObserver(rider, horse);
            chunk4IncomingObserver.BeginCase(Chunk4LifeId);
            pairedAutomaticEndProbe = new NativeAutomaticEndProbe(false);
            step = Phase3dHorseStep.Phase3gControls; ResetLeafClock();
        }

        private JObject CaptureChunk4LifeState() => new JObject {
            ["live"] = CaptureOrdinaryLiveState(), ["identity"] = combat.PairedActivationIdentity,
            ["split"] = combat.PairedActivationSplit, ["finalized"] = combat.PairedActivationFinalized,
            ["riderEnded"] = combat.PairedActorEnded(rider), ["mountEnded"] = combat.PairedActorEnded(horse),
            ["playerCombat"] = Game.Instance.Player.IsInCombat, ["riderCombat"] = rider.IsInCombat, ["mountCombat"] = horse.IsInCombat,
            ["tbActive"] = CombatController.IsInTurnBasedCombat(), ["tbInitialized"] = Game.Instance.TurnBasedCombatController.Initialized,
            ["actorRecords"] = combat.TrackedActorAllocations, ["round"] = Game.Instance.TurnBasedCombatController.RoundNumber,
            ["privatePartner"] = combat.PairedPartnerContext?.Unit.UniqueId,
            ["pairCommand"] = combat.HasActiveCommand, ["pairIntent"] = combat.HasStockAttackIntent,
            ["pairMovement"] = combat.HasActiveGroundMovement,
            ["attachmentResidue"] = relationship.Runtime.HasPresentationAttachmentResidue,
            ["attachmentRestoreVerified"] = relationship.Runtime.PresentationAttachmentRestoreVerified,
            ["poseRestoreVerified"] = relationship.Runtime.PoseBaselineRestoreVerified,
            ["rider"] = CaptureChunk4LifeActor(rider), ["mount"] = CaptureChunk4LifeActor(horse),
            ["riderGrants"] = allocationTrace.GrantCount(rider), ["mountGrants"] = allocationTrace.GrantCount(horse),
            ["currentActor"] = Game.Instance.TurnBasedCombatController.CurrentTurn?.Unit.UniqueId,
            ["frame"] = Time.frameCount
        };
        private static JObject CaptureChunk4LifeActor(UnitEntityData actor) => new JObject {
            ["id"] = actor.UniqueId, ["inState"] = actor.IsInState,
            ["lifeState"] = actor.Descriptor.State.LifeState.ToString(), ["conscious"] = actor.Descriptor.State.IsConscious,
            ["dead"] = actor.Descriptor.State.IsDead, ["finallyDead"] = actor.Descriptor.State.IsFinallyDead,
            ["damage"] = actor.Damage, ["hp"] = actor.Stats.HitPoints.ModifiedValue,
            ["temporaryHp"] = actor.Stats.TemporaryHitPoints.ModifiedValue, ["constitution"] = actor.Stats.Constitution.ModifiedValue,
            ["activeView"] = actor.View != null && actor.View.gameObject.activeInHierarchy,
            ["enabledRenderers"] = actor.View == null ? 0 : actor.View.GetComponentsInChildren<Renderer>(true)
                .Count(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy),
            ["parent"] = actor.View?.transform.parent?.name
        };

        private void TickChunk4NativeLife()
        {
            var game = Game.Instance; var controller = game.TurnBasedCombatController; var turn = controller.CurrentTurn;
            chunk4LifeEvidence["stage"] = chunk4LifeStage;
            chunk4LifeEvidence["progress"] = CaptureChunk4LifeState();
            if (game.IsPaused) { game.IsPaused = false; return; }
            if (chunk4LifeStage == 0)
            {
                if (!Chunk4PairedPlayIdle) return;
                SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                if (relationship.State != RelationshipState.Mounted)
                {
                    if (!chunk4LifeMountInput) chunk4LifeMountInput = TryNativeAbilityTargetClick(nativeControls.MountAbility, horse, "life-pre-combat-mount");
                    return;
                }
                if (rider.IsInCombat || horse.IsInCombat || !PrepareUnmountedHorseAiIsolation() || !PrepareCombatMountRiderAiIsolation()) return;
                if (turnBasedModeProbe == null) turnBasedModeProbe = new NativeModeTransitionProbe(true);
                if (!turnBasedModeProbe.TemporaryValueIsCurrent) { turnBasedModeProbe.DispatchTemporaryValueIfRequired(); return; }
                chunk4LifeEvidence["mountedBeforeCombat"] = !game.Player.IsInCombat;
                BeginTarget(6f, Chunk4LifeId); ruleProbe.Arm(target, false);
                chunk4LifeStage = 1; ResetLeafClock(); return;
            }
            if (chunk4LifeStage == 1)
            {
                if (!IsCombatReady(true) || !CombatController.IsInTurnBasedCombat()) return;
                if (turn?.Unit != rider || turn.Status != TurnController.TurnStatus.Preparing && !turn.IsActing)
                { TryEndPhase3gFixtureTurn(turn); return; }
                if (!Chunk4PairedPlayIdle) return;
                chunk4LifeTurn = turn;
                using (var input = new NativeOrdinaryAttackInput(FindPairedControlPoint(0.75f, "life-native-adjacency")))
                {
                    input.Predict(); var cycles = 0;
                    while ((turn.EnabledFiveFootStep || turn.EnabledSingleActionMove) && cycles++ < 8) { input.Click(button: 1); input.Predict(); }
                    if (!input.Click()) return;
                }
                chunk4LifeMove = horse.Commands.Move as UnitMoveTo;
                if (chunk4LifeMove?.Executor != horse) throw new InvalidOperationException("Native life setup lost the real mover.");
                chunk4LifeStage = 2; ResetLeafClock(); return;
            }
            if (chunk4LifeStage == 2)
            {
                if (!chunk4LifeMove.IsFinished || !Chunk4PairedPlayIdle) return;
                if (chunk4LifeMove.Result != UnitCommand.ResultType.Success || rider.CombatState.Cooldown.MoveAction != 0)
                    throw new InvalidOperationException("Life fixture native positioning failed or taxed rider Move.");
                TryEndPhase3gFixtureTurn(turn);
                if (!ReferenceEquals(phase3gEndedTurn, chunk4LifeTurn)) return;
                chunk4LifeStage = 3; ResetLeafClock(); return;
            }
            if (chunk4LifeStage == 3)
            {
                if (turn == null || ReferenceEquals(turn, chunk4LifeTurn)) return;
                if (turn.Unit != rider) { TryEndPhase3gFixtureTurn(turn); return; }
                if (!Chunk4PairedPlayIdle || turn.Status != TurnController.TurnStatus.Preparing && !turn.IsActing) return;
                if (combat.PairedPartnerContext?.Unit != horse || rider.CombatState.Cooldown.StandardAction != 0 || horse.CombatState.Cooldown.StandardAction != 0)
                    throw new InvalidOperationException("Life fixture lacks fresh independent native preparation.");
                chunk4LifeTurn = turn;
                chunk4LifeEvidence["beforeActions"] = CaptureChunk4LifeState();
                ordinaryAttackTrace.BeginCase(Chunk4LifeId + "-first-primary");
                if (!targetService.BeginExpectedAttackDispatch(target)) throw new InvalidOperationException("Life target cannot accept the legal setup Primary.");
                if (!TryNativeAbilityTargetClick(Chunk4LifeMount || Chunk4LifeIncapacitation ? nativeControls.MountPrimaryAbility : nativeControls.RiderPrimaryAbility,
                    target, "life-first-primary")) throw new InvalidOperationException("Life fixture first native Primary was rejected.");
                chunk4LifeStage = 4; ResetLeafClock(); return;
            }
            if (chunk4LifeStage == 4)
            {
                var firstMount = Chunk4LifeMount || Chunk4LifeIncapacitation;
                if (chunk4LifeFirst == null) chunk4LifeFirst = firstMount ? ordinaryAttackTrace.LastStartedMountAttack : ordinaryAttackTrace.LastStartedRiderAttack;
                if (chunk4LifeFirst == null || !chunk4LifeFirst.IsFinished || !Chunk4PairedPlayIdle || !chunk4IncomingObserver.AllAttacksResolved) return;
                if (chunk4LifeFirst.Result != UnitCommand.ResultType.Success || chunk4LifeFirst.GetAttackIndex() != 1 ||
                    chunk4LifeFirst.Executor.CombatState.Cooldown.StandardAction != 6)
                    throw new InvalidOperationException("Life fixture requires a real completed and spent Primary before live interruption.");
                chunk4LifeEvidence["firstPrimary"] = CaptureOrdinaryCommand(chunk4LifeFirst);
                ordinaryAttackTrace.BeginCase(Chunk4LifeId + "-live-primary");
                if (!TryNativeAbilityTargetClick(firstMount ? nativeControls.RiderPrimaryAbility : nativeControls.MountPrimaryAbility,
                    target, "life-second-primary")) throw new InvalidOperationException("Life fixture second native Primary was rejected.");
                chunk4LifeStage = 5; ResetLeafClock(); return;
            }
            if (chunk4LifeStage == 5)
            {
                var liveRider = Chunk4LifeMount || Chunk4LifeIncapacitation;
                if (chunk4LifeLive == null) chunk4LifeLive = liveRider ? ordinaryAttackTrace.LastStartedRiderAttack : ordinaryAttackTrace.LastStartedMountAttack;
                if (chunk4LifeLive == null || !chunk4LifeLive.IsStarted || chunk4LifeLive.TimeSinceStart <= 0) return;
                if (chunk4LifeLive.IsFinished || !combat.HasActiveCommand || !ReferenceEquals(turn, chunk4LifeTurn) || relationship.State != RelationshipState.Mounted)
                    throw new InvalidOperationException("Native life stimulus missed the live paired command window.");
                var subject = Chunk4LifeSubject;
                var difficulty = game.Player.Difficulty.DamageToParty;
                var deathThreshold = subject.Stats.HitPoints.ModifiedValue + subject.Stats.Constitution.ModifiedValue;
                var desiredDamage = Chunk4LifeIncapacitation ? subject.Stats.HitPoints.ModifiedValue + 1 : deathThreshold + 1;
                var needed = desiredDamage - subject.Damage + subject.Stats.TemporaryHitPoints.ModifiedValue;
                if (!target.IsPlayersEnemy || target.IsPlayerFaction || difficulty <= 0 || float.IsNaN(difficulty) || float.IsInfinity(difficulty) || needed <= 0)
                    throw new InvalidOperationException("Life stimulus lacks its exact native enemy, finite difficulty or positive damage window.");
                var requested = checked((int)Math.Ceiling((needed + 1d) / difficulty));
                if (Chunk4LifeIncapacitation && subject.Damage + requested * difficulty - subject.Stats.TemporaryHitPoints.ModifiedValue >= deathThreshold)
                    throw new InvalidOperationException("Native difficulty leaves no safe incapacitation window below death.");
                chunk4LifeEvidence["beforeDamage"] = CaptureChunk4LifeState();
                chunk4LifeEvidence["liveCommandBefore"] = CaptureOrdinaryCommand(chunk4LifeLive);
                chunk4LifeEvidence["unrelatedOrderBefore"] = new JArray(controller.SortedUnits.Where(unit => unit != rider && unit != horse && unit.IsInState &&
                    unit.IsInCombat && !unit.Descriptor.State.IsDead).Select(unit => unit.UniqueId));
                if (((JArray)chunk4LifeEvidence["unrelatedOrderBefore"]).Count < 2)
                    throw new InvalidOperationException("Life fixture lacks two unrelated native successor actors.");
                chunk4LifeRiderGrants = allocationTrace.GrantCount(rider); chunk4LifeMountGrants = allocationTrace.GrantCount(horse);
                chunk4LifeEvidence["source"] = target.UniqueId; chunk4LifeEvidence["requestedDamage"] = requested;
                chunk4LifeEvidence["damageToParty"] = difficulty; chunk4LifeEvidence["deathThreshold"] = deathThreshold;
                chunk4LifeEvidence["damageDispatches"] = 1;
                var damage = Rulebook.Trigger(new RuleDealDamage(target, subject,
                    new DamageBundle(new DirectDamage(new DiceFormula(0, DiceType.Zero), requested))));
                chunk4LifeEvidence["nativeDamage"] = damage.Damage;
                chunk4LifeEvidence["nativeDamageBeforeDifficulty"] = damage.DamageBeforeDifficulty;
                chunk4LifeEvidence["afterDamage"] = CaptureChunk4LifeState();
                if (game.Player.Difficulty.DamageToParty != difficulty || damage.DamageBeforeDifficulty != requested ||
                    subject.Damage < desiredDamage || Chunk4LifeIncapacitation && subject.Damage >= deathThreshold)
                    throw new InvalidOperationException("Native damage did not reach its requested life window under unchanged difficulty.");
                chunk4LifeStage = 6; ResetLeafClock(); return;
            }
            if (chunk4LifeStage == 6)
            {
                var subject = Chunk4LifeSubject;
                var reached = Chunk4LifeIncapacitation ? !subject.Descriptor.State.IsConscious && !subject.Descriptor.State.IsDead : subject.Descriptor.State.IsDead;
                if (!reached || relationship.State != RelationshipState.Unmounted || combat.PairedPartnerContext != null ||
                    combat.PairedActivationIdentity != null && !combat.PairedActivationSplit || combat.HasActiveCommand || combat.HasStockAttackIntent || combat.HasActiveGroundMovement ||
                    !rider.Commands.Empty || !horse.Commands.Empty || !chunk4IncomingObserver.AllAttacksResolved) return;
                if (turn == null) return;
                if (ReferenceEquals(turn, chunk4LifeTurn))
                {
                    // A conscious surviving principal may still need the ordinary
                    // player End input. Cleanup must already have settled first.
                    if (turn.Unit.Descriptor.State.IsConscious)
                    {
                        if (chunk4LifeEvidence["survivorEndBefore"] == null) chunk4LifeEvidence["survivorEndBefore"] = CaptureChunk4LifeState();
                        TryEndPhase3gFixtureTurn(turn);
                        if (ReferenceEquals(phase3gEndedTurn, turn)) chunk4LifeEvidence["survivorEndInput"] = true;
                    }
                    return;
                }
                if (turn.Unit == rider || turn.Unit == horse || relationship.Runtime.HasPresentationAttachmentResidue ||
                    !relationship.Runtime.PresentationAttachmentRestoreVerified || !Chunk4LifeSurvivor.Descriptor.State.IsConscious ||
                    !Chunk4LifeSurvivor.IsInState || (int)CaptureChunk4LifeActor(Chunk4LifeSurvivor)["enabledRenderers"] < 1)
                    throw new InvalidOperationException("Native life cleanup retained presentation, participation or an invisible surviving actor.");
                var survivorKey = Chunk4LifeMount ? "rider" : "mount";
                if (Chunk4LifeSurvivor.Damage != (int)chunk4LifeEvidence["beforeDamage"][survivorKey]["damage"])
                    throw new InvalidOperationException("Targeted native life stimulus changed the independent partner's health.");
                chunk4LifeEvidence["afterCleanup"] = CaptureChunk4LifeState();
                chunk4LifeEvidence["liveCommandAfter"] = CaptureOrdinaryCommand(chunk4LifeLive);
                chunk4LifeStage = 7; ResetLeafClock();
            }
            if (chunk4LifeStage == 7)
            {
                if (allocationTrace.GrantCount(rider) != chunk4LifeRiderGrants || allocationTrace.GrantCount(horse) != chunk4LifeMountGrants ||
                    combat.PairedPartnerContext != null || combat.PairedActivationIdentity != null &&
                        (!combat.PairedActivationSplit || combat.PairedActivationIdentity != (string)chunk4LifeEvidence["beforeDamage"]["identity"]))
                    throw new InvalidOperationException("Native life cleanup issued a duplicate pair grant or private context.");
                if (turn == null) return;
                if (!ReferenceEquals(turn, chunk4LifeUnrelatedTurn))
                {
                    if (turn.Unit == rider || turn.Unit == horse ||
                        (string)chunk4LifeEvidence["unrelatedOrderBefore"][chunk4LifeUnrelatedCount] != turn.Unit.UniqueId)
                        throw new InvalidOperationException("Native life cleanup did not proceed through unrelated actors from its actual order.");
                    chunk4LifeUnrelatedTurn = turn;
                    ((JArray)chunk4LifeEvidence["unrelatedTurns"]).Add(new JObject { ["actor"] = turn.Unit.UniqueId,
                        ["round"] = controller.RoundNumber, ["frame"] = Time.frameCount });
                    chunk4LifeUnrelatedCount++; ResetLeafClock();
                }
                if (chunk4LifeUnrelatedCount < 2) { TryEndPhase3gFixtureTurn(turn); return; }
                if (Chunk4LifeIncapacitation ? Chunk4LifeSubject.Descriptor.State.IsConscious || Chunk4LifeSubject.Descriptor.State.IsDead :
                    !Chunk4LifeSubject.Descriptor.State.IsDead)
                    throw new InvalidOperationException("The observed native life result did not persist through unrelated turns.");
                chunk4LifeEvidence["finalLife"] = CaptureChunk4LifeState();
                chunk4LifeStage = 8; ResetLeafClock(); return;
            }
            if (chunk4LifeStage == 8)
            {
                var source = game.Player.MainCharacter.Value;
                if (source == null || !source.Descriptor.State.IsConscious || !target.IsPlayersEnemy ||
                    target.Descriptor.IsEssentialForGame || target.Descriptor.State.Immortality || target == source)
                    throw new InvalidOperationException("Life encounter completion requires the exact disposable enemy and a conscious native source.");
                var requested = checked(target.Stats.HitPoints.ModifiedValue + target.Stats.Constitution.ModifiedValue +
                    target.Stats.TemporaryHitPoints.ModifiedValue - target.Damage + 16);
                chunk4LifeEvidence["enemyBeforeDamage"] = CaptureChunk4LifeActor(target);
                var damage = Rulebook.Trigger(new RuleDealDamage(source, target,
                    new DamageBundle(new DirectDamage(new DiceFormula(0, DiceType.Zero), requested))));
                chunk4LifeEvidence["enemyDamageDispatches"] = 1;
                chunk4LifeEvidence["enemyNativeDamage"] = damage.Damage;
                chunk4LifeEvidence["enemyDamageSource"] = source.UniqueId;
                if (target.Damage < target.Stats.HitPoints.ModifiedValue + target.Stats.Constitution.ModifiedValue)
                    throw new InvalidOperationException("Native effect did not defeat the disposable encounter target.");
                chunk4LifeStage = 9; ResetLeafClock(); return;
            }
            if (chunk4LifeStage == 9)
            {
                // The identity is allowed to retain accounting while split. It
                // must retire after actual native combat exit, before harness cleanup.
                if (!target.Descriptor.State.IsDead || targetService.LifeTransitionCount < 1 || game.Player.IsInCombat ||
                    rider.IsInCombat || horse.IsInCombat || CombatController.IsInTurnBasedCombat() || controller.Initialized ||
                    combat.PairedActivationIdentity != null || combat.PairedPartnerContext != null || combat.TrackedActorAllocations != 0 ||
                    !rider.Commands.Empty || !horse.Commands.Empty || !chunk4IncomingObserver.AllAttacksResolved) return;
                chunk4LifeEvidence["nativeEncounterExit"] = CaptureChunk4LifeState();
                chunk4LifeEvidence["enemyAfterDeath"] = CaptureChunk4LifeActor(target);
                chunk4LifeEvidence["enemyLifeTransitions"] = targetService.LifeTransitionCount;
                if (Chunk4LifeIncapacitation ? Chunk4LifeSubject.Descriptor.State.IsConscious || Chunk4LifeSubject.Descriptor.State.IsDead :
                    !Chunk4LifeSubject.Descriptor.State.IsDead)
                    throw new InvalidOperationException("Native encounter completion changed the real subject life result.");
                chunk4LifeEvidence["nativeLifeEvents"] = chunk4LifeObserver.Capture();
                chunk4LifeEvidence["nativeRules"] = chunk4IncomingObserver.Capture();
                chunk4LifeEvidence["allocationTrace"] = allocationTrace.Capture();
                observations["ordinaryTrace"] = ordinaryAttackTrace.Capture();
                AddRow(Chunk4LifeId, true, "Native damage interrupted a live pair command; life state, independent costs and unrelated turn progression were retained.", chunk4LifeEvidence);
                BeginCleanup();
            }
        }

        private void CleanupChunk4NativeLife()
        {
            if (chunk4LifeObserver != null) { chunk4LifeEvidence["nativeLifeEvents"] = chunk4LifeObserver.Capture(); chunk4LifeObserver.Dispose(); chunk4LifeObserver = null; }
            if (chunk4IncomingObserver != null) { observations["chunk4IncomingRules"] = chunk4IncomingObserver.Capture(); chunk4IncomingObserver.Dispose(); chunk4IncomingObserver = null; }
        }
    }
}
