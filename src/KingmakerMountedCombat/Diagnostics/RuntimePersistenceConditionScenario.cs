using System;
using System.IO;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Parts;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class RuntimePersistenceScenario
    {
        private bool ConditionCase => CombatCase && Checkpoint == "condition";
        private PairedNativeConditionLease conditionLease;
        private NativeAllocationRoundFactLease conditionFact;
        private NativeActorAllocationTrace conditionTrace;
        private UnitCommand conditionCommand;
        private int conditionOriginalDamage;
        private int conditionSavedDamage;
        private int conditionInitialRiderGrants;
        private int conditionInitialMountGrants;
        private int conditionFutureVisits;
        private readonly System.Collections.Generic.HashSet<TurnController> conditionRefreshed =
            new System.Collections.Generic.HashSet<TurnController>();
        private readonly System.Collections.Generic.HashSet<TurnController> conditionTurns =
            new System.Collections.Generic.HashSet<TurnController>();

        private JObject ConditionObservation() => new JObject {
            ["combat"] = CombatObservation(), ["damage"] = mount.Damage,
            ["originalDamage"] = conditionOriginalDamage,
            ["nativeRiderPreparations"] = conditionTrace.GrantCount(rider),
            ["nativeMountPreparations"] = conditionTrace.GrantCount(mount),
            ["riderEnded"] = combat.PairedActorEnded(rider), ["mountEnded"] = combat.PairedActorEnded(mount),
            ["conditionActive"] = mount.Descriptor.State.HasCondition(UnitCondition.Confusion),
            ["nativePartPresent"] = mount.Get<UnitPartConfusion>() != null,
            ["stimulus"] = conditionLease?.Evidence.DeepClone(),
            ["fact"] = conditionFact?.Capture(),
            ["command"] = conditionCommand == null ? null : new JObject {
                ["type"] = conditionCommand.GetType().FullName, ["actor"] = conditionCommand.Executor?.UniqueId,
                ["started"] = conditionCommand.IsStarted, ["finished"] = conditionCommand.IsFinished,
                ["ignoreCooldown"] = conditionCommand.IsIgnoreCooldown, ["result"] = conditionCommand.Result.ToString()
            }
        };

        private void AdvanceCondition()
        {
            if (clock.Elapsed.TotalSeconds > 150)
                throw new InvalidOperationException("P03 condition stage timed out: " + stage + "; " + persistence.Feedback);
            var game = Game.Instance;
            if (LoadingProcess.Instance.IsLoadingInProcess || persistence.CombatRestorationPending) return;
            if (targetService != null && !targetService.RefreshBidirectionalCombatMemoryLease())
                throw new InvalidOperationException("P03 condition fixture lost native combat memory.");
            if (game.IsPaused) { game.IsPaused = false; return; }
            var controller = game.TurnBasedCombatController;
            var turn = controller.CurrentTurn;
            if (stage == 0)
            {
                Check(settings.EnablePairedActivation && !settings.EnableUnifiedMountedTurn &&
                    !settings.EnablePairedCommandScheduler && !settings.EnableDiagnosticOverlay, "P03-condition-required-policy");
                if (Cold)
                {
                    var data = persistence.LoadedData;
                    Check(data?.Combat?.TurnBased == true && !data.Mounted && data.Combat.Paired?.Activation?.Split == true,
                        "P03-cold-native-split-data-present");
                    rider = game.State.Units.Single(u => u.UniqueId == data.Combat.Paired.RiderId);
                    mount = game.State.Units.Single(u => u.UniqueId == data.Combat.Paired.MountId);
                    combatTarget = data.Combat.Actors.Select(a => game.State.Units.Single(u => u.UniqueId == a.Native.Id))
                        .Single(u => u.IsEnemy(rider) && u.IsInCombat);
                    Check(persistence.SemanticRestoreCount == data.Combat.Actors.Length &&
                        persistence.PresentationRestoreCount == 0 && controls.NativeCastRequestCount == 0 &&
                        relationship.State == RelationshipState.Unmounted, "P03-cold-split-rebound-without-Mount-or-acquisition");
                    Check(data.Combat.Actors.All(a => LegitimateContinuation(a.Native,
                        MountedPersistenceService.CaptureActor(game.State.Units.Single(u => u.UniqueId == a.Native.Id)), 0)),
                        "P03-cold-exact-native-expenditure");
                    ValidateConditionSnapshot(data);
                    conditionTrace = new NativeActorAllocationTrace(rider, mount, combat);
                    conditionTrace.BeginEncounter(request.RunId);
                    savedBoundary = turn; savedSequence = combat.PairedActivationSequence; savedRound = data.Combat.Round;
                    Check(!mount.Descriptor.State.HasCondition(UnitCondition.Confusion) && mount.Get<UnitPartConfusion>() == null &&
                        mount.IsDirectlyControllable && mount.Descriptor.State.IsConscious && mount.Damage > 0,
                        "P03-native-self-harm-persists-with-no-cold-condition-or-health-repair");
                    conditionSavedDamage = mount.Damage;
                    Write("initial", ConditionObservation());
                    BeginConditionContinuation(); return;
                }
                Kingmaker.EntitySystem.Entities.UnitEntityData selectedRider, selectedMount; string error;
                if (!relationship.TryResolveAutomationPair(out selectedRider, out selectedMount, out error))
                    throw new InvalidOperationException(error);
                rider = selectedRider; mount = selectedMount;
                Check(!game.Player.IsInCombat && relationship.MountRiderOn(rider, mount).Succeeded,
                    "P03-condition-mounted-before-native-combat");
                controls.Update(); BindOwnedControlSlots();
                conditionOriginalDamage = mount.Damage;
                conditionTrace = new NativeActorAllocationTrace(rider, mount, combat);
                conditionTrace.BeginEncounter(request.RunId);
                // This mode adds no wound/healing and never restores Damage.
                // Its temporary fact is removed before any save is requested.
                conditionFact = new NativeAllocationRoundFactLease(mount, false);
                conditionLease = new PairedNativeConditionLease(mount, combat, conditionTrace, conditionFact, 60);
                targetService = new DiagnosticCombatTargetService(logger);
                combatTarget = targetService.Spawn(rider, mount, FindDestination(7f), request.RunId, true, false, true);
                Check(targetService.PrepareForPlayerClick(combatTarget) &&
                    targetService.QueueBidirectionalCombatMemory(rider, combatTarget), "P03-condition-native-encounter-request");
                Write("initial", ConditionObservation()); stage = 1; return;
            }
            if (turn != null && conditionTurns.Add(turn))
            {
                turnVisits.Add(controller.RoundNumber + "|" + turn.Unit.UniqueId);
                if (savedBoundary != null && turn.Unit == mount)
                    Check(controller.RoundNumber > savedRound, "P03-forfeited-mount-no-second-turn-in-saved-round");
                if (stage == 6 && !ReferenceEquals(turn, savedBoundary) && (turn.Unit == rider || turn.Unit == mount) &&
                    conditionFutureVisits++ < 4)
                    Write("condition-next-turn-observed", ConditionObservation());
            }
            if (stage == 1)
            {
                if (turn == null) return;
                if (turn.Unit != rider) { EndConditionTurn(turn); return; }
                conditionCommand = mount.Get<UnitPartConfusion>()?.Cmd;
                if (conditionCommand == null) return;
                savedBoundary = turn; savedSequence = combat.PairedActivationSequence; savedRound = controller.RoundNumber;
                Check(conditionCommand.Executor == mount && !conditionCommand.IsIgnoreCooldown,
                    "P03-native-preparation-command-exact-actor-and-cost-path");
                Write("condition-command-observed", ConditionObservation()); stage = 2; return;
            }
            if (stage == 2)
            {
                if (!conditionCommand.IsFinished || !PairIdle || relationship.State != RelationshipState.Unmounted) return;
                Check(conditionCommand.Result == UnitCommand.ResultType.Success &&
                    (int)conditionLease.Evidence["nativeSelfDamageRules"] == 1 &&
                    (int)conditionLease.Evidence["choiceOverrides"] == 1 &&
                    mount.Damage > conditionOriginalDamage && combat.PairedActorEnded(mount) && !combat.PairedActorEnded(rider) &&
                    ReferenceEquals(turn, savedBoundary) && rider.CombatState.Cooldown.StandardAction == 0 &&
                    rider.CombatState.Cooldown.MoveAction == 0, "P03-native-condition-forfeits-only-mount-and-retains-principal");
                conditionSavedDamage = mount.Damage;
                conditionLease.Dispose(); conditionFact.Dispose();
                Check(mount.Damage == conditionSavedDamage &&
                    (bool)conditionLease.Evidence["cleanupResourcesUnchanged"] &&
                    (bool)conditionLease.Evidence["directControlRestored"] && (bool)conditionFact.Capture()["restored"],
                    "P03-test-stimuli-removed-before-save-with-native-harm-and-costs-retained");
                controls.Update(); beforeControls = controls.CaptureSnapshot();
                conditionInitialRiderGrants = conditionTrace.GrantCount(rider);
                conditionInitialMountGrants = conditionTrace.GrantCount(mount);
                Write("condition-forfeit-retained", ConditionObservation()); stage = 3; return;
            }
            if (stage == 3) { RequestCombatSave(); return; }
            if (stage == 4)
            {
                if (!callback || NativePersistenceIsolation.HasPendingWrites) return;
                var save = game.SaveManager.SingleOrDefault(s => s.Name == "KMC_P01");
                if (save == null || save.OperationState != SaveInfo.StateType.None || !save.HasFileOnDisk) return;
                var read = NativeMountedSaveStorage.Read(save.Saver);
                Check(read.Kind == MountedSaveReadKind.Current, "P03-condition-real-native-current-archive");
                ValidateConditionSnapshot(read.Data);
                Check(ReferenceEquals(turn, savedBoundary) && mount.Damage == conditionSavedDamage &&
                    conditionTrace.GrantCount(rider) == conditionInitialRiderGrants &&
                    conditionTrace.GrantCount(mount) == conditionInitialMountGrants &&
                    read.Data.Combat.Actors.All(a => LegitimateContinuation(a.Native,
                        MountedPersistenceService.CaptureActor(game.State.Units.Single(u => u.UniqueId == a.Native.Id)), 0)),
                    "P03-saving-does-not-repeat-condition-or-refresh-forfeited-work");
                var control = controls.CaptureSnapshot();
                Check(!control.SerializationSuspended && control.ExactFactCount == beforeControls.ExactFactCount &&
                    control.DuplicateFactCount == 0 && control.ManagedHotbarSlotCount == 0,
                    "P03-split-native-controls-usable-after-save");
                Write("native-write-complete", new JObject {
                    ["path"] = save.FolderName, ["sha256"] = Hash(save.FolderName), ["length"] = new FileInfo(save.FolderName).Length,
                    ["nativeType"] = save.Type.ToString(), ["nativeCallback"] = callback,
                    ["operation"] = save.OperationState.ToString(),
                    ["snapshot"] = JObject.FromObject(read.Data, MountedSaveCodec.CreateSerializer()),
                    ["condition"] = ConditionObservation()
                });
                BeginConditionContinuation(); return;
            }
            if (stage == 5)
            {
                if (!FinishCombatAttack()) return;
                Check(mount.Damage == conditionSavedDamage && combat.PairedActorEnded(mount),
                    "P03-unused-principal-attack-does-not-refund-forfeited-partner");
                stage = 6; return;
            }
            if (stage == 6)
            {
                if (turn == null) return;
                if (ReferenceEquals(turn, savedBoundary)) { EndConditionTurn(turn); return; }
                // Native Prepare ends in Preparing for controllable actors.
                // Tick enters Acting only after input, costs or lost control.
                // Observe the completed ready phase before issuing native End.
                var ready = turn.Status == TurnController.TurnStatus.Preparing || turn.IsActing;
                if ((turn.Unit == rider || turn.Unit == mount) && (!ready || controller.WaitingForUI)) return;
                var riderGrants = conditionTrace.GrantCount(rider) - conditionInitialRiderGrants;
                var mountGrants = conditionTrace.GrantCount(mount) - conditionInitialMountGrants;
                if ((turn.Unit == rider || turn.Unit == mount) && ready &&
                    !ReferenceEquals(turn, endedBoundary) && turn.Unit.Commands.Empty && conditionRefreshed.Add(turn))
                {
                    var count = turn.Unit == rider ? riderGrants : mountGrants;
                    Check(count >= 1 && count <= 2 && controller.RoundNumber > savedRound &&
                        turn.Unit.CombatState.Cooldown.StandardAction == 0 && turn.Unit.CombatState.Cooldown.MoveAction == 0,
                        "P03-real-next-independent-preparation-refreshes-once");
                    Write("next-independent-activation", new JObject { ["actor"] = turn.Unit.UniqueId, ["count"] = count,
                        ["round"] = controller.RoundNumber, ["status"] = turn.Status.ToString(),
                        ["prepared"] = turn.Unit.CombatState.Prepared,
                        ["riderPreparations"] = riderGrants, ["mountPreparations"] = mountGrants });
                }
                if (riderGrants == 2 && mountGrants == 2 && conditionRefreshed.Count == 4)
                {
                    Check(turnVisits.Count >= 4 && relationship.State == RelationshipState.Unmounted,
                        "P03-split-participation-continues-through-unrelated-actors");
                    Write("usable-continuation-complete", new JObject { ["condition"] = ConditionObservation(),
                        ["turnVisits"] = new JArray(turnVisits), ["riderPreparations"] = riderGrants, ["mountPreparations"] = mountGrants });
                    Dispose();
                    Result = new RuntimeSubscenarioResult { Name = request.Scenario, Status = "PASS",
                        AssertionPassCount = passed, AssertionFailCount = 0, Errors = new string[0] };
                    Completed = true; return;
                }
                EndConditionTurn(turn);
            }
        }

        private void ValidateConditionSnapshot(MountedSaveData data)
        {
            var pair = data.Combat?.Paired;
            Check(!data.Mounted && pair?.Activation?.Split == true && pair.Activation.Mount.Ended &&
                !pair.Activation.Rider.Ended && pair.Activation.Mount.ForfeitRecorded &&
                pair.Activation.Mount.ForfeitAdded > 0 && pair.RiderId == rider.UniqueId && pair.MountId == mount.UniqueId &&
                data.Combat.Current?.ActorId == rider.UniqueId && pair.BoundaryIsCurrent &&
                combat.PairedActivationIdentity == pair.Activation.EncounterId + ":" + pair.Activation.Sequence &&
                data.Combat.Round == Game.Instance.TurnBasedCombatController.RoundNumber,
                "P03-exact-condition-forfeit-split-participation-in-archive");
            Check(combat.PairedActorEnded(mount) && !combat.PairedActorEnded(rider) &&
                rider.CombatState.Cooldown.StandardAction == 0 && !mount.HasStandardAction(),
                "P03-forfeited-partner-has-no-new-work-and-principal-remains-ready");
        }

        private void BeginConditionContinuation()
        {
            conditionInitialRiderGrants = conditionTrace.GrantCount(rider);
            conditionInitialMountGrants = conditionTrace.GrantCount(mount);
            Write("condition-remainder-before-input", ConditionObservation());
            BeginCombatAttack(rider, "attack"); stage = 5;
        }

        private void EndConditionTurn(TurnController turn)
        {
            if (turn == null || ReferenceEquals(turn, endedBoundary) || !turn.Unit.IsDirectlyControllable ||
                turn.Unit.Group != rider.Group || !turn.Unit.Commands.Empty || turn.Unit.AreHandsBusyWithAnimation ||
                !turn.CanEndTurnAndNoActing() || Game.Instance.TurnBasedCombatController.WaitingForUI) return;
            Game.Instance.PauseBind(); endedBoundary = turn;
        }
    }
}
