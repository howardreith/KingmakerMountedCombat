using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.Utility;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class RuntimePersistenceScenario
    {
        private UnitEntityData combatTarget;
        private TurnController savedBoundary;
        private TurnController endedBoundary;
        private TurnController observedTurn;
        private readonly List<string> turnVisits = new List<string>();
        private long savedSequence;
        private int savedRound;
        private int laterActivations;
        private float moveBeforeContinuation;
        private UnitEntityData attackActor;
        private string attackKind;
        private SavedNativeActor rejectedRider;
        private SavedNativeActor rejectedMount;
        private int rejectedFrame;
        private string Checkpoint => request.PersistenceCase ?? "partial-movement";

        private bool PairIdle => rider.Commands.Empty && mount.Commands.Empty &&
            !rider.AreHandsBusyWithAnimation && !mount.AreHandsBusyWithAnimation &&
            !combat.HasActiveCommand && !combat.HasActiveGroundMovement;

        private JObject CombatObservation() => new JObject
        {
            ["checkpoint"] = Checkpoint,
            ["round"] = Game.Instance.TurnBasedCombatController.RoundNumber,
            ["current"] = Game.Instance.TurnBasedCombatController.CurrentTurn?.Unit.UniqueId,
            ["turn"] = Game.Instance.TurnBasedCombatController.CurrentTurn == null ? null :
                JObject.FromObject(NativeTurnPersistence.Capture(Game.Instance.TurnBasedCombatController.CurrentTurn),
                MountedSaveCodec.CreateSerializer()),
            ["activation"] = combat.PairedActivationIdentity,
            ["sequence"] = combat.PairedActivationSequence,
            ["nativeRiderStandardAvailable"] = rider?.HasStandardAction(),
            ["nativeMountStandardAvailable"] = mount?.HasStandardAction(),
            ["roundEffects"] = RoundEffectCase && riderRoundEffect != null ? RoundEffects() : null,
            ["partner"] = combat.PairedPartnerContext == null ? null :
                JObject.FromObject(NativeTurnPersistence.Capture(combat.PairedPartnerContext), MountedSaveCodec.CreateSerializer())
        };

        private void AdvanceCombat()
        {
            if (clock.Elapsed.TotalSeconds > 150)
                throw new InvalidOperationException("P02 " + Checkpoint + " stage timed out: " + stage + "; " + persistence.Feedback);
            var game = Game.Instance;
            if (LoadingProcess.Instance.IsLoadingInProcess || persistence.CombatRestorationPending) return;
            if (targetService != null && stage > 0 && !targetService.RefreshBidirectionalCombatMemoryLease())
                throw new InvalidOperationException("Owned native combat memory lease was lost.");
            if (game.IsPaused) { game.IsPaused = false; return; }
            var turn = game.TurnBasedCombatController.CurrentTurn;
            if (rider != null && turn != null && !ReferenceEquals(turn, observedTurn))
            {
                observedTurn = turn;
                turnVisits.Add(game.TurnBasedCombatController.RoundNumber + "|" + turn.Unit.UniqueId);
            }
            if (CommitmentCase && AdvanceCommitmentProbe(turn)) return;
            if (RoundEffectCase && AdvanceRoundEffectFixture(turn)) return;
            if (stage == 0)
            {
                Check(settings.EnablePairedActivation && !settings.EnableUnifiedMountedTurn &&
                    !settings.EnablePairedCommandScheduler && !settings.EnableDiagnosticOverlay, "P02-required-policy");
                if (Cold)
                {
                    var data = persistence.LoadedData;
                    Check(data?.Combat?.TurnBased == true && data.Mounted &&
                        relationship.State == RelationshipState.Mounted, "P02-cold-combat-and-pair-restored");
                    rider = relationship.Rider; mount = relationship.Mount;
                    Check(rider.UniqueId == data.Rider.Id && mount.UniqueId == data.Mount.Id &&
                        game.State.Units.Count(u => u.UniqueId == rider.UniqueId) == 1 &&
                        game.State.Units.Count(u => u.UniqueId == mount.UniqueId) == 1, "P02-same-unique-native-actors");
                    Check(persistence.SemanticRestoreCount == data.Combat.Actors.Length &&
                        persistence.PresentationRestoreCount == 1 && controls.NativeCastRequestCount == 0,
                        "P02-no-replayed-mount-or-missing-early-actors");
                    var elapsed = Checkpoint == "explicit-end" ?
                        (game.TimeController.GameTime.Ticks - data.GameTimeTicks) / (double)TimeSpan.TicksPerSecond : 0;
                    Check(data.Combat.Actors.All(a => LegitimateContinuation(a.Native,
                        MountedPersistenceService.CaptureActor(game.State.Units.Single(u => u.UniqueId == a.Native.Id)), elapsed)),
                        "P02-exact-saved-native-remainder-for-every-actor");
                    Check((turn?.Unit == rider || Checkpoint == "explicit-end") &&
                        game.TurnBasedCombatController.RoundNumber == data.Combat.Round &&
                        combat.PairedActivationIdentity == data.Combat.Paired.Activation.EncounterId + ":" +
                            data.Combat.Paired.Activation.Sequence, "P02-same-round-boundary-and-grant");
                    ValidateCheckpoint(data);
                    if (Checkpoint != "explicit-end")
                    {
                        var movement = data.Combat.Allocations.Single(a => a.ActorId == mount.UniqueId).Movement;
                        Check(combat.PairedPartnerContext != null &&
                            Math.Abs(combat.PairedPartnerContext.TimeMoved - movement.TimeMoved) < 0.001f,
                            "P02-saved-transport-commitment-retained");
                    }
                    combatTarget = data.Combat.Actors.Select(a => game.State.Units.Single(u => u.UniqueId == a.Native.Id))
                        .Single(u => u.IsEnemy(rider) && u.IsInCombat);
                    Check(combatTarget.Faction != null && MountedSaveData.HexId(combatTarget.Faction.AssetGuid),
                        "P02-loaded-enemy-has-native-faction");
                    Check(combatTarget.Stats.HitPoints.BaseValue == 256 && combatTarget.Descriptor.State.IsConscious,
                        "P02-native-target-health-survived-without-cold-provision");
                    savedBoundary = turn; savedSequence = combat.PairedActivationSequence; savedRound = data.Combat.Round;
                    controls.Update(); beforeControls = controls.CaptureSnapshot();
                    Check(beforeControls.ExactFactCount == 3 && beforeControls.DuplicateFactCount == 0 &&
                        beforeControls.ManagedHotbarSlotCount == data.Slots.Length, "P02-cold-controls-once");
                    Write("initial", CombatObservation());
                    if (RoundEffectCase) ObserveColdRoundEffects();
                    ContinueSavedCheckpoint(); return;
                }
                UnitEntityData selectedRider; UnitEntityData selectedMount; string error;
                if (!relationship.TryResolveAutomationPair(out selectedRider, out selectedMount, out error))
                    throw new InvalidOperationException(error);
                rider = selectedRider; mount = selectedMount;
                Check(!game.Player.IsInCombat && relationship.MountRiderOn(rider, mount).Succeeded,
                    "P02-mounted-before-native-combat");
                controls.Update(); BindOwnedControlSlots(); beforeControls = controls.CaptureSnapshot();
                if (RoundEffectCase) InstallRoundEffects();
                targetService = new DiagnosticCombatTargetService(logger);
                combatTarget = targetService.Spawn(rider, mount, FindDestination(7f), request.RunId, true, false, true);
                Check(targetService.PrepareForPlayerClick(combatTarget) &&
                    targetService.QueueBidirectionalCombatMemory(rider, combatTarget), "P02-native-fixture-combat-request");
                Write("initial", CombatObservation()); stage = 1; return;
            }
            if (stage == 1)
            {
                if (turn == null || turn.Status != TurnController.TurnStatus.Preparing && !turn.IsActing) return;
                if (turn.Unit != rider) { EndFixtureTurn(turn); return; }
                if (!PairIdle) return;
                Check(CombatController.IsInTurnBasedCombat() && combat.PairedPartnerContext != null &&
                    rider.CombatState.Cooldown.StandardAction == 0 && mount.CombatState.Cooldown.MoveAction == 0,
                    "P02-fresh-native-paired-boundary");
                savedBoundary = turn; savedSequence = combat.PairedActivationSequence;
                savedRound = game.TurnBasedCombatController.RoundNumber;
                if (CommitmentCase) { BeginCommitmentFixture(); return; }
                if (RoundEffectCase) { BeginRoundEffectFixture(); return; }
                BeginCombatMovement(0.75f, "partial-movement-dispatched",
                    Checkpoint == "between-partner-orders" ? (Vector3?)RiderReachDestination() : null);
                stage = 2; return;
            }
            if (stage == 2)
            {
                if (move == null || !move.IsFinished || !PairIdle) return;
                Check(move.Result == UnitCommand.ResultType.Success &&
                    GeometryUtils.MechanicsDistance(origin, mount.Position) > 0.25f &&
                    mount.CombatState.Cooldown.MoveAction > 0 && mount.CombatState.Cooldown.MoveAction < 3f &&
                    rider.CombatState.Cooldown.StandardAction == 0 && rider.CombatState.Cooldown.MoveAction == 0 &&
                    ReferenceEquals(savedBoundary, turn), "P02-partial-mount-work-leaves-rider-standard-and-mount-remainder");
                if (Checkpoint == "between-partner-orders")
                    Check(mount.DistanceTo(combatTarget) < RiderAttackRadius() && mount.HasLOS(combatTarget),
                        "P02-rider-in-native-weapon-reach-before-mount-spends-movement");
                Write("partial-movement-completed", CombatObservation());
                if (Checkpoint == "partial-movement") stage = 3;
                else if (Checkpoint == "explicit-end") stage = 22;
                else if (Checkpoint == "between-partner-orders")
                { BeginCombatAttack(mount, "setup-mount-attack"); stage = 21; }
                else { BeginCombatAttack(rider, "setup-rider-attack"); stage = 20; }
                return;
            }
            if (stage == 20 || stage == 21)
            {
                if (!FinishCombatAttack()) return;
                if (stage == 20 && Checkpoint == "exhausted")
                { BeginCombatAttack(mount, "setup-mount-attack"); stage = 21; return; }
                stage = 3; return;
            }
            if (stage == 22)
            {
                EndFixtureTurn(turn);
                if (!ReferenceEquals(endedBoundary, turn)) return;
                Check(turn.IsEnding && rider.CombatState.Cooldown.StandardAction == 6 &&
                    mount.CombatState.Cooldown.StandardAction == 6, "P02-native-explicit-End-forfeited-both-actors");
                Write("explicit-end-requested", CombatObservation());
                // The native queue/iterator selects the actual snapshot phase.
                // Do not freeze or assign a synthetic Ending state.
                RequestCombatSave(); return;
            }
            if (stage == 3) { RequestCombatSave(); return; }
            if (stage == 4)
            {
                if (!callback) return;
                var save = game.SaveManager.SingleOrDefault(s => s.Name == "KMC_P01");
                if (save == null || save.OperationState != SaveInfo.StateType.None || !save.HasFileOnDisk) return;
                var read = NativeMountedSaveStorage.Read(save.Saver);
                Check(read.Kind == MountedSaveReadKind.Current && read.Data.Combat != null &&
                    read.Data.Combat.Paired.Activation.Sequence == savedSequence,
                    "P02-actual-archive-contains-combat-grant");
                ValidateCheckpoint(read.Data);
                var elapsed = (game.TimeController.GameTime.Ticks - read.Data.GameTimeTicks) / (double)TimeSpan.TicksPerSecond;
                Check((ReferenceEquals(savedBoundary, turn) || Checkpoint == "explicit-end") &&
                    combat.PairedActivationSequence == savedSequence && relationship.State == RelationshipState.Mounted &&
                    read.Data.Combat.Actors.All(a => LegitimateContinuation(a.Native,
                        MountedPersistenceService.CaptureActor(game.State.Units.Single(u => u.UniqueId == a.Native.Id)), elapsed)),
                    "P02-save-does-not-end-refresh-or-tax-live-actors");
                var control = controls.CaptureSnapshot();
                Check(control.ExactFactCount == beforeControls.ExactFactCount && control.DuplicateFactCount == 0 &&
                    control.ManagedHotbarSlotCount == beforeControls.ManagedHotbarSlotCount && !control.SerializationSuspended,
                    "P02-owned-controls-restored-once");
                Write("native-write-complete", new JObject {
                    ["path"] = save.FolderName, ["sha256"] = Hash(save.FolderName), ["length"] = new FileInfo(save.FolderName).Length,
                    ["nativeType"] = save.Type.ToString(), ["nativeCallback"] = callback,
                    ["operation"] = save.OperationState.ToString(),
                    ["snapshot"] = JObject.FromObject(read.Data, MountedSaveCodec.CreateSerializer()) });
                ContinueSavedCheckpoint(); return;
            }
            if (stage == 6)
            {
                if (!PairIdle) return;
                Check(ReferenceEquals(savedBoundary, turn) && !combat.PairedActorEnded(rider) &&
                    !combat.PairedActorEnded(mount), "P02-both-actors-still-have-the-saved-grant");
                moveBeforeContinuation = mount.CombatState.Cooldown.MoveAction;
                BeginCombatMovement(0.75f, "movement-dispatched");
                stage = 7; return;
            }
            if (stage == 7)
            {
                if (!move.IsFinished || !PairIdle) return;
                Check(move.Result == UnitCommand.ResultType.Success &&
                    mount.CombatState.Cooldown.MoveAction > moveBeforeContinuation &&
                    rider.CombatState.Cooldown.MoveAction == 0 && ReferenceEquals(savedBoundary, turn),
                    "P02-continuation-consumes-more-mount-work-without-rider-tax");
                Write("movement-completed", CombatObservation());
                BeginCombatAttack(Checkpoint == "rider-spent" ? mount : rider, "attack");
                stage = 8; return;
            }
            if (stage == 8)
            {
                if (!FinishCombatAttack()) return;
                if (Checkpoint == "rider-spent") { BeginRejectedWork(); return; }
                stage = 9; return;
            }
            if (stage == 32)
            {
                if (turn == null) return;
                if (ReferenceEquals(turn, savedBoundary))
                {
                    Check(turn.IsEnding || turn.Status == TurnController.TurnStatus.Ended,
                        "P02-pending-saved-End-does-not-become-Acting");
                    return;
                }
                Check(combat.PairedActivationSequence == savedSequence && turn.Unit != rider && turn.Unit != mount,
                    "P02-ended-pair-has-no-new-grant-during-next-native-turn");
                BeginRejectedWork(); return;
            }
            if (stage == 31)
            {
                if (Time.frameCount < rejectedFrame + 8 || !PairIdle || move != null && !move.IsFinished) return;
                Check(GeometryUtils.MechanicsDistance(origin, mount.Position) < 0.02f &&
                    LegitimateContinuation(rejectedRider, MountedPersistenceService.CaptureActor(rider), 0) &&
                    LegitimateContinuation(rejectedMount, MountedPersistenceService.CaptureActor(mount), 0),
                    "P02-exhausted-movement-delivers-no-distance-or-refund");
                Write("spent-work-rejected", CombatObservation());
                if (Checkpoint == "between-partner-orders")
                { BeginCombatAttack(rider, "attack"); stage = 8; }
                else stage = 9;
                return;
            }
            if (stage == 9)
            {
                if (turn == null || turn.Status != TurnController.TurnStatus.Preparing && !turn.IsActing) return;
                if (turn.Unit == mount) throw new InvalidOperationException("P02 partner received a duplicate independent activation.");
                if (turn.Unit != rider) { EndFixtureTurn(turn); return; }
                if (ReferenceEquals(turn, savedBoundary)) { EndFixtureTurn(turn); return; }
                if (!PairIdle) return;
                Check(combat.PairedActivationSequence == savedSequence + laterActivations + 1 &&
                    combat.PairedPartnerContext != null && rider.CombatState.Cooldown.StandardAction == 0 &&
                    rider.CombatState.Cooldown.MoveAction == 0 && mount.CombatState.Cooldown.StandardAction == 0 &&
                    mount.CombatState.Cooldown.MoveAction == 0 && combat.PairedPartnerContext.TimeMoved == 0,
                    "P02-next-true-activation-refreshes-once-without-old-commitments");
                if (RoundEffectCase)
                {
                    if (!RoundEffectsReady(turn, laterActivations + 2)) return;
                    CheckRoundEffects(laterActivations + 2, "P03-next-real-round-applies-native-effect-once");
                }
                if (CommitmentCase)
                    Check(CurrentCommitment.MetresStepped == 0 && CurrentCommitment.TimeStepped == 0,
                        "P03-next-true-activation-refreshes-step-commitment-once");
                laterActivations++; savedBoundary = turn;
                Write("next-paired-activation", CombatObservation());
                if (laterActivations < 2) { EndFixtureTurn(turn); return; }
                var expected = game.TurnBasedCombatController.SortedUnits.Where(u => u != mount).Select(u => u.UniqueId).ToArray();
                var prefix = (savedRound + 1) + "|";
                Check(turnVisits.Where(v => v.StartsWith(prefix, StringComparison.Ordinal)).Select(v => v.Substring(prefix.Length))
                    .SequenceEqual(expected), "P02-unrelated-turn-order-and-exactly-once-participation");
                var final = CombatObservation(); final["turnVisits"] = new JArray(turnVisits);
                Write("usable-continuation-complete", final);
                Dispose();
                Result = new RuntimeSubscenarioResult { Name = request.Scenario, Status = "PASS",
                    AssertionPassCount = passed, AssertionFailCount = 0, Errors = new string[0] };
                Completed = true;
            }
        }

        private void ValidateCheckpoint(MountedSaveData data)
        {
            var saved = data.Combat;
            Check(saved?.Paired?.Activation != null && saved.Round >= 1 && saved.Paired.Activation.Sequence >= 1,
                "P02-snapshot-has-native-round-and-participation");
            var riderSpent = data.Rider.Standard > 0;
            var mountSpent = data.Mount.Standard > 0;
            if (RoundEffectCase)
            {
                Check(!riderSpent && !mountSpent && data.Rider.Move == 0 && data.Mount.Move == 0 &&
                    saved.Current?.ActorId == data.Rider.Id, "P03-round-effect-snapshot-has-unused-native-actions");
                return;
            }
            if (CommitmentCase)
            {
                var allocation = saved.Allocations.Single(a => a.ActorId == data.Mount.Id);
                var movement = allocation.Movement;
                Check(!riderSpent && data.Rider.Move == 0 && saved.Current?.ActorId == data.Rider.Id &&
                    (Checkpoint == "step" ? !mountSpent && data.Mount.Move == 0 &&
                        movement.MetresStepped > 0 && movement.MetresStepped < TurnController.MetersOfFiveFootStep &&
                        movement.TimeStepped > 0 : !mountSpent && data.Mount.Standard == 0 &&
                        data.Mount.Move > 3 && data.Mount.Move < 6 && movement.TimeMoved > 3),
                    "P03-actual-snapshot-matches-native-" + Checkpoint);
                return;
            }
            var valid = Checkpoint == "partial-movement" ? !riderSpent && !mountSpent && data.Mount.Move > 0 && data.Mount.Move < 3 :
                Checkpoint == "rider-spent" ? riderSpent && !mountSpent && data.Mount.Move > 0 && data.Mount.Move < 3 :
                Checkpoint == "between-partner-orders" ? !riderSpent && mountSpent && data.Mount.Move >= 3 :
                Checkpoint == "exhausted" ? riderSpent && mountSpent && data.Mount.Move >= 3 :
                saved.Paired.Activation.Ending && riderSpent && mountSpent;
            Check(valid && (Checkpoint == "explicit-end" || saved.Current?.ActorId == data.Rider.Id),
                "P02-actual-snapshot-matches-" + Checkpoint);
        }

        private void RequestCombatSave()
        {
            var game = Game.Instance;
            if (!game.SaveManager.IsSaveAllowed()) return;
            var descriptor = game.SaveManager.CreateNewSave("KMC_P01");
            Check(descriptor.Type == SaveInfo.SaveType.Manual && descriptor.Name == "KMC_P01",
                "P02-real-native-manual-descriptor");
            game.SaveGame(descriptor, () => callback = true);
            stage = 4;
        }

        private void ContinueSavedCheckpoint()
        {
            if (CommitmentCase) { ContinueCommitment(); return; }
            if (RoundEffectCase) { ContinueRoundEffects(); return; }
            if (Checkpoint == "explicit-end") { stage = 32; return; }
            if (Checkpoint == "partial-movement" || Checkpoint == "rider-spent") { stage = 6; return; }
            BeginRejectedWork();
        }

        private void BeginCombatAttack(UnitEntityData actor, string kind)
        {
            attackActor = actor; attackKind = kind;
            if (ruleProbe == null) ruleProbe = new MountedCombatRuleProbe();
            ruleProbe.Arm(rider, mount, actor, combatTarget);
            if (targetService != null)
                Check(targetService.PrepareForPlayerClick(combatTarget) &&
                    targetService.BeginExpectedAttackDispatch(combatTarget), "P02-native-target-attack-admission");
            if (actor == mount) Check(TryPrimaryInput(true), "P02-native-mount-primary-input");
            else
            {
                SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                Game.Instance.DefaultPointerController.ClearPointerMode();
                using (var input = new NativeOrdinaryAttackInput(combatTarget))
                    Check(input.Click(), "P02-ordinary-rider-attack-input");
            }
            Write(kind + "-dispatched", CombatObservation());
        }

        private bool FinishCombatAttack()
        {
            if (ruleProbe.AttackRuleCount < 1 || ruleProbe.AttackRollCount < 1 || !PairIdle) return false;
            Check(ruleProbe.LastInitiatorId == attackActor.UniqueId && ruleProbe.LastTargetId == combatTarget.UniqueId &&
                ruleProbe.UnexpectedPairAttackCount == 0 && attackActor.CombatState.Cooldown.StandardAction > 0,
                "P02-native-attack-spends-only-its-actor-standard");
            Write(attackKind + "-delivered", new JObject { ["rules"] = ruleProbe.AttackRuleCount,
                ["rolls"] = ruleProbe.AttackRollCount, ["actor"] = attackActor.UniqueId,
                ["damage"] = ruleProbe.TotalDamage, ["hit"] = ruleProbe.LastAttackHit, ["combat"] = CombatObservation() });
            return true;
        }

        private bool TryPrimaryInput(bool mountActor)
        {
            controls.Update();
            SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
            var blueprint = mountActor ? controls.MountPrimaryAbility : controls.RiderPrimaryAbility;
            var ability = rider.Descriptor.Abilities.GetAbility(blueprint)?.Data;
            var handler = Game.Instance.SelectedAbilityHandler;
            if (ability == null || handler == null || combatTarget?.View == null)
                throw new InvalidOperationException("P02 native Primary input lacks a saved actor/control/target.");
            try
            {
                handler.SetAbility(ability);
                handler.GetPriority(combatTarget.View.gameObject, combatTarget.Position);
                return handler.OnClick(combatTarget.View.gameObject, combatTarget.Position, 0, false, false);
            }
            finally { handler.DropAbility(); }
        }

        private void BeginRejectedWork()
        {
            rejectedRider = MountedPersistenceService.CaptureActor(rider);
            rejectedMount = MountedPersistenceService.CaptureActor(mount);
            Write("spent-work-input-before", CombatObservation());
            var before = controls.CaptureSnapshot().DispatchAcceptedCount;
            if (rejectedRider.Standard > 0 || Checkpoint == "explicit-end") Check(!TryPrimaryInput(false), "P02-spent-rider-standard-rejected");
            if (rejectedMount.Standard > 0 || Checkpoint == "explicit-end") Check(!TryPrimaryInput(true), "P02-spent-mount-standard-rejected");
            Check(controls.CaptureSnapshot().DispatchAcceptedCount == before && PairIdle,
                "P02-spent-attack-admission-created-no-native-work");
            if (Checkpoint == "explicit-end")
            {
                Check(combat.PairedActivationSequence == savedSequence &&
                    LegitimateContinuation(rejectedRider, MountedPersistenceService.CaptureActor(rider), 0) &&
                    LegitimateContinuation(rejectedMount, MountedPersistenceService.CaptureActor(mount), 0),
                    "P02-ended-actors-do-not-regain-work");
                Write("spent-work-rejected", CombatObservation()); stage = 9; return;
            }
            SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
            Game.Instance.DefaultPointerController.ClearPointerMode();
            origin = mount.Position;
            using (var input = new NativeOrdinaryAttackInput(FindDestination(0.75f)))
            { input.Predict(); input.Click(); }
            move = mount.Commands.Move as UnitMoveTo;
            rejectedFrame = Time.frameCount; stage = 31;
        }

        private float RiderAttackRadius()
        {
            var selection = NativeSingleAttackWeaponResolver.Resolve(rider);
            if (selection?.Weapon == null || mount?.View == null || combatTarget?.View == null)
                throw new InvalidOperationException("P02 fixture has no native rider weapon/range context.");
            return mount.View.Corpulence + combatTarget.View.Corpulence + selection.Weapon.AttackRange.Meters;
        }

        private Vector3 RiderReachDestination()
        {
            var radius = RiderAttackRadius();
            var offset = mount.Position - combatTarget.Position;
            offset.y = 0;
            var wanted = combatTarget.Position + offset.normalized * (radius - 0.35f);
            var traced = Kingmaker.View.ObstacleAnalyzer.TraceAlongNavmesh(mount.Position, wanted);
            Check(GeometryUtils.MechanicsDistance(wanted, traced) < 0.1f &&
                GeometryUtils.MechanicsDistance(traced, mount.Position) > 0.25f &&
                GeometryUtils.MechanicsDistance(traced, combatTarget.Position) < radius,
                "P02-walkable-position-within-native-rider-reach");
            return traced;
        }

        private void BeginCombatMovement(float distance, string kind, Vector3? wantedDestination = null,
            bool step = false, bool requireCommand = true)
        {
            var turn = Game.Instance.TurnBasedCombatController.CurrentTurn;
            SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
            Game.Instance.DefaultPointerController.ClearPointerMode();
            origin = mount.Position; destination = wantedDestination ?? FindDestination(distance);
            using (var input = new NativeOrdinaryAttackInput(destination))
            {
                input.Predict();
                for (var cycle = 0; (turn.EnabledFiveFootStep != step || !step && turn.EnabledSingleActionMove) && cycle < 4; cycle++)
                { input.Click(button: 1); input.Predict(); }
                Check(turn.EnabledFiveFootStep == step, "P02-native-ground-cursor-policy");
                var accepted = input.Click();
                if (requireCommand) Check(accepted, "P02-ordinary-ground-input");
            }
            move = mount.Commands.Move as UnitMoveTo;
            if (requireCommand) Check(move != null && move.Executor == mount, "P02-native-transport-owner");
            if (kind != null) Write(kind, CombatObservation());
        }

        private void EndFixtureTurn(TurnController turn)
        {
            if (turn == null || ReferenceEquals(turn, endedBoundary) || !turn.Unit.IsDirectlyControllable ||
                turn.Unit.Group != rider.Group || !turn.Unit.Commands.Empty ||
                turn.Unit.AreHandsBusyWithAnimation || !turn.CanEndTurnAndNoActing() ||
                Game.Instance.TurnBasedCombatController.WaitingForUI) return;
            Check(turn.Unit != mount, "P02-partner-never-owns-an-independent-native-turn");
            Game.Instance.PauseBind();
            endedBoundary = turn;
        }
    }
}
