using System;
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

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class RuntimePersistenceScenario
    {
        private UnitEntityData combatTarget;
        private TurnController savedBoundary;
        private TurnController endedBoundary;
        private long savedSequence;
        private int laterActivations;
        private float moveBeforeContinuation;

        private bool PairIdle => rider.Commands.Empty && mount.Commands.Empty &&
            !rider.AreHandsBusyWithAnimation && !mount.AreHandsBusyWithAnimation &&
            !combat.HasActiveCommand && !combat.HasActiveGroundMovement;

        private JObject CombatObservation() => new JObject
        {
            ["round"] = Game.Instance.TurnBasedCombatController.RoundNumber,
            ["current"] = Game.Instance.TurnBasedCombatController.CurrentTurn?.Unit.UniqueId,
            ["turn"] = Game.Instance.TurnBasedCombatController.CurrentTurn == null ? null :
                JObject.FromObject(NativeTurnPersistence.Capture(Game.Instance.TurnBasedCombatController.CurrentTurn),
                MountedSaveCodec.CreateSerializer()),
            ["activation"] = combat.PairedActivationIdentity,
            ["sequence"] = combat.PairedActivationSequence,
            ["partner"] = combat.PairedPartnerContext == null ? null :
                JObject.FromObject(NativeTurnPersistence.Capture(combat.PairedPartnerContext), MountedSaveCodec.CreateSerializer())
        };

        private void AdvanceCombat()
        {
            if (clock.Elapsed.TotalSeconds > 150)
                throw new InvalidOperationException("P02 partial-movement stage timed out: " + stage + "; " + persistence.Feedback);
            var game = Game.Instance;
            if (LoadingProcess.Instance.IsLoadingInProcess || persistence.CombatRestorationPending) return;
            if (targetService != null && stage > 0 && !targetService.RefreshBidirectionalCombatMemoryLease())
                throw new InvalidOperationException("Owned native combat memory lease was lost.");
            if (game.IsPaused) { game.IsPaused = false; return; }
            var turn = game.TurnBasedCombatController.CurrentTurn;
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
                    Check(LegitimateContinuation(data.Rider, MountedPersistenceService.CaptureActor(rider), 0) &&
                        LegitimateContinuation(data.Mount, MountedPersistenceService.CaptureActor(mount), 0),
                        "P02-exact-saved-native-remainder");
                    Check(turn?.Unit == rider && game.TurnBasedCombatController.RoundNumber == data.Combat.Round &&
                        combat.PairedActivationIdentity == data.Combat.Paired.Activation.EncounterId + ":" +
                            data.Combat.Paired.Activation.Sequence, "P02-same-round-boundary-and-grant");
                    var movement = data.Combat.Allocations.Single(a => a.ActorId == mount.UniqueId).Movement;
                    Check(combat.PairedPartnerContext != null &&
                        Math.Abs(combat.PairedPartnerContext.TimeMoved - movement.TimeMoved) < 0.001f &&
                        rider.CombatState.Cooldown.StandardAction == 0 && mount.CombatState.Cooldown.MoveAction > 0,
                        "P02-partial-transport-commitment-and-rider-standard-retained");
                    combatTarget = data.Combat.Actors.Select(a => game.State.Units.Single(u => u.UniqueId == a.Native.Id))
                        .Single(u => u.IsEnemy(rider) && u.IsInCombat);
                    Check(combatTarget.Faction != null && MountedSaveData.HexId(combatTarget.Faction.AssetGuid),
                        "P02-loaded-enemy-has-native-faction");
                    savedBoundary = turn; savedSequence = combat.PairedActivationSequence;
                    controls.Update(); beforeControls = controls.CaptureSnapshot();
                    Write("initial", CombatObservation());
                    stage = 6; return;
                }
                UnitEntityData selectedRider; UnitEntityData selectedMount; string error;
                if (!relationship.TryResolveAutomationPair(out selectedRider, out selectedMount, out error))
                    throw new InvalidOperationException(error);
                rider = selectedRider; mount = selectedMount;
                Check(!game.Player.IsInCombat && relationship.MountRiderOn(rider, mount).Succeeded,
                    "P02-mounted-before-native-combat");
                controls.Update(); BindOwnedControlSlots(); beforeControls = controls.CaptureSnapshot();
                targetService = new DiagnosticCombatTargetService(logger);
                combatTarget = targetService.Spawn(rider, mount, FindDestination(7f), request.RunId, true, false, true);
                Check(targetService.PrepareForPlayerClick(combatTarget) &&
                    targetService.QueueBidirectionalCombatMemory(rider, combatTarget), "P02-native-fixture-combat-request");
                Write("initial"); stage = 1; return;
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
                BeginCombatMovement(0.75f, "partial-movement-dispatched");
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
                Write("partial-movement-completed", CombatObservation());
                stage = 3; return;
            }
            if (stage == 3)
            {
                if (!game.SaveManager.IsSaveAllowed()) return;
                var descriptor = game.SaveManager.CreateNewSave("KMC_P01");
                Check(descriptor.Type == SaveInfo.SaveType.Manual && descriptor.Name == "KMC_P01",
                    "P02-real-native-manual-descriptor");
                game.SaveGame(descriptor, () => callback = true);
                stage = 4; return;
            }
            if (stage == 4)
            {
                if (!callback) return;
                var save = game.SaveManager.SingleOrDefault(s => s.Name == "KMC_P01");
                if (save == null || save.OperationState != SaveInfo.StateType.None || !save.HasFileOnDisk) return;
                var read = NativeMountedSaveStorage.Read(save.Saver);
                Check(read.Kind == MountedSaveReadKind.Current && read.Data.Combat != null &&
                    read.Data.Combat.Paired.Activation.Sequence == savedSequence &&
                    read.Data.Combat.Current.ActorId == rider.UniqueId, "P02-actual-archive-contains-combat-grant");
                Check(ReferenceEquals(savedBoundary, turn) && combat.PairedActivationSequence == savedSequence &&
                    relationship.State == RelationshipState.Mounted &&
                    LegitimateContinuation(read.Data.Rider, MountedPersistenceService.CaptureActor(rider), 0) &&
                    LegitimateContinuation(read.Data.Mount, MountedPersistenceService.CaptureActor(mount), 0),
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
                stage = 6; return;
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
                ruleProbe = new MountedCombatRuleProbe();
                ruleProbe.Arm(rider, mount, rider, combatTarget);
                if (targetService != null)
                    Check(targetService.PrepareForPlayerClick(combatTarget) &&
                        targetService.BeginExpectedAttackDispatch(combatTarget), "P02-native-target-attack-admission");
                SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                using (var input = new NativeOrdinaryAttackInput(combatTarget))
                    Check(input.Click(), "P02-ordinary-rider-attack-input");
                Write("attack-dispatched", CombatObservation()); stage = 8; return;
            }
            if (stage == 8)
            {
                if (ruleProbe.AttackRuleCount < 1 || ruleProbe.AttackRollCount < 1 || !PairIdle) return;
                Check(ruleProbe.LastInitiatorId == rider.UniqueId && ruleProbe.LastTargetId == combatTarget.UniqueId &&
                    ruleProbe.UnexpectedPairAttackCount == 0 && rider.CombatState.Cooldown.StandardAction > 0,
                    "P02-ordinary-attack-spends-native-standard");
                Write("attack-delivered", new JObject { ["rules"] = ruleProbe.AttackRuleCount,
                    ["rolls"] = ruleProbe.AttackRollCount, ["combat"] = CombatObservation() });
                stage = 9; return;
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
                laterActivations++; savedBoundary = turn;
                Write("next-paired-activation", CombatObservation());
                if (laterActivations < 2) { EndFixtureTurn(turn); return; }
                Write("usable-continuation-complete", CombatObservation());
                Dispose();
                Result = new RuntimeSubscenarioResult { Name = request.Scenario, Status = "PASS",
                    AssertionPassCount = passed, AssertionFailCount = 0, Errors = new string[0] };
                Completed = true;
            }
        }

        private void BeginCombatMovement(float distance, string kind)
        {
            var turn = Game.Instance.TurnBasedCombatController.CurrentTurn;
            SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
            Game.Instance.DefaultPointerController.ClearPointerMode();
            origin = mount.Position; destination = FindDestination(distance);
            using (var input = new NativeOrdinaryAttackInput(destination))
            {
                input.Predict();
                for (var cycle = 0; (turn.EnabledFiveFootStep || turn.EnabledSingleActionMove) && cycle < 4; cycle++)
                { input.Click(button: 1); input.Predict(); }
                Check(!turn.EnabledFiveFootStep && input.Click(), "P02-ordinary-ground-input");
            }
            move = mount.Commands.Move as UnitMoveTo;
            Check(move != null && move.Executor == mount, "P02-native-transport-owner");
            Write(kind, CombatObservation());
        }

        private void EndFixtureTurn(TurnController turn)
        {
            if (ReferenceEquals(turn, endedBoundary) || !turn.Unit.IsDirectlyControllable ||
                turn.Unit.Group != rider.Group || !turn.Unit.Commands.Empty ||
                turn.Unit.AreHandsBusyWithAnimation || !turn.CanEndTurnAndNoActing() ||
                Game.Instance.TurnBasedCombatController.WaitingForUI) return;
            Check(turn.Unit != mount, "P02-partner-never-owns-an-independent-native-turn");
            Game.Instance.PauseBind();
            endedBoundary = turn;
        }
    }
}
