using System;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic.Abilities.Blueprints;
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
    // Chunk 6A foundation checkpoints on the unchanged Chunk 5 persistence machinery: a
    // settled real-time combat Mount or Dismount (P04 checkpoints combat-mount-rt and
    // combat-dismount-rt) and a settled turn-based combat Mount on the rider's own Acting turn
    // (P02 checkpoint combat-mount-tb), each saved through the same owned manual archive and
    // reopened cold in a fresh process. The scenario performs the native transition through the
    // registered selected-ability click, records what the engine did and saves at the settled
    // boundary; the external validator decides the acceptance. Nothing here writes a cooldown,
    // a relationship or a native turn.
    internal sealed partial class RuntimePersistenceScenario
    {
        private const int FoundationSettleFrames = 10;
        // The Chunk 6 closeout checkpoints ride the same combat Mount click flow (RuntimePersistenceClosureScenario).
        private bool RealtimeCombatMount => RealtimeCase && (Checkpoint == "combat-mount-rt" || RealtimeClosure);
        private bool RealtimeCombatDismount => RealtimeCase && Checkpoint == "combat-dismount-rt";
        private bool RealtimeFoundation => RealtimeCombatMount || RealtimeCombatDismount;
        private bool FoundationCombatMount => CombatCase && Checkpoint == "combat-mount-tb";
        private int foundationSettled;
        private int foundationClickFrame = -1;
        private int foundationAvailabilityWaits;
        private JObject foundationBefore;
        private UnitMoveTo foundationEntryMove;
        private TurnController foundationTurn;
        private Vector3 foundationEntryOrigin;

        private JObject FoundationObservation()
        {
            var ledger = controls.RelationshipTransitionLedger;
            return new JObject
            {
                ["relationship"] = relationship.State.ToString(),
                ["generation"] = relationship.MountedPairGeneration,
                ["transitionInFlight"] = controls.HasUnsettledRelationshipTransition,
                ["transitionSettlement"] = controls.DescribeRelationshipTransitionSettlement(),
                ["relationshipShells"] = controls.NativeRelationshipShellCount,
                ["castRequests"] = controls.NativeCastRequestCount,
                ["dispatchAccepted"] = controls.DispatchAcceptedCount,
                ["dispatchRejected"] = controls.DispatchRejectedCount,
                ["transitionCounters"] = new JObject
                {
                    ["admittedMount"] = ledger.AdmittedMountCount, ["acceptedMount"] = ledger.AcceptedMountCount,
                    ["admittedDismount"] = ledger.AdmittedDismountCount, ["acceptedDismount"] = ledger.AcceptedDismountCount,
                    ["refusedVoluntary"] = ledger.RefusedVoluntaryCount, ["forcedDetach"] = ledger.ForcedDetachCount,
                    ["duplicateSuppressed"] = ledger.DuplicateControlSuppressedCount,
                    ["concurrentSuppressed"] = ledger.ConcurrentControlSuppressedCount
                },
                ["pairedIdentity"] = combat.PairedActivationIdentity,
                ["pairedSequence"] = combat.PairedActivationSequence,
                ["pairedSplit"] = combat.PairedActivationSplit,
                ["pairedFinalized"] = combat.PairedActivationFinalized,
                ["partnerContextActor"] = combat.PairedPartnerContext?.Unit?.UniqueId,
                ["adoptionCount"] = combat.MidEncounterAdoptionCount,
                ["adoptionObservation"] = combat.LastPairedAdoptionObservation,
                ["initiativeObservation"] = combat.PairedInitiativeObservation,
                ["persistenceWorldDiscards"] = combat.PersistenceWorldDiscardCount,
                ["lastPersistenceWorldDiscard"] = combat.LastPersistenceWorldDiscardObservation,
                ["partyInCombat"] = Game.Instance.Player?.IsInCombat,
                ["lifetimeRetirements"] = combat.PairedLifetimeRetirementCount,
                ["lifetimeRetirementsDeferred"] = combat.PairedLifetimeRetirementDeferredCount,
                ["lastLifetimeRetirement"] = combat.LastPairedLifetimeRetirement,
                ["riderInCombat"] = rider?.IsInCombat,
                ["mountInCombat"] = mount?.IsInCombat,
                ["riderHasMove"] = rider?.HasMoveAction(),
                ["riderHasStandard"] = rider?.HasStandardAction(),
                ["riderCommandsEmpty"] = rider?.Commands.Empty,
                ["mountCommandsEmpty"] = mount?.Commands.Empty,
                ["riderReallyMoving"] = rider?.View?.AgentASP?.IsReallyMoving,
                ["mountReallyMoving"] = mount?.View?.AgentASP?.IsReallyMoving,
                ["riderPosition"] = RealtimePoint(rider),
                ["mountPosition"] = RealtimePoint(mount),
                ["pairDistance"] = rider == null || mount == null ? (float?)null : rider.DistanceTo(mount),
                ["riderMoveSlot"] = DescribeFoundationCommand(rider?.Commands?.GetCommand(UnitCommand.CommandType.Move))
            };
        }

        private static JObject DescribeFoundationCommand(UnitCommand command) => command == null ? null : new JObject
        {
            ["type"] = command.GetType().Name,
            ["abilityGuid"] = (command as UnitUseAbility)?.Spell?.Blueprint?.AssetGuid,
            ["executor"] = command.Executor?.UniqueId,
            ["target"] = command.Target?.Unit?.UniqueId,
            ["started"] = command.IsStarted,
            ["acted"] = command.IsActed,
            ["finished"] = command.IsFinished,
            ["result"] = command.Result.ToString(),
            ["createdByPlayer"] = command.CreatedByPlayer
        };

        private JObject FoundationActorDebt() => new JObject
        {
            ["rider"] = JObject.FromObject(MountedPersistenceService.CaptureActor(rider), MountedSaveCodec.CreateSerializer()),
            ["mount"] = JObject.FromObject(MountedPersistenceService.CaptureActor(mount), MountedSaveCodec.CreateSerializer())
        };

        // The registered selected-ability click, exactly as a player issues it: select the rider,
        // cancel any pointer mode, set the ability, click the target, drop the ability. Every
        // control counter delta is recorded; nothing is retried.
        private JObject TryFoundationAbilityClick(BlueprintAbility blueprint, UnitEntityData clickedTarget)
        {
            controls.Update();
            var fact = rider.Descriptor.Abilities.GetAbility(blueprint);
            var data = fact?.Data;
            var handler = Game.Instance.SelectedAbilityHandler;
            var targetObject = clickedTarget?.View?.gameObject;
            var record = new JObject
            {
                ["abilityGuid"] = blueprint?.AssetGuid,
                ["abilityPresent"] = data != null,
                ["availableForCast"] = data?.IsAvailableForCast,
                ["handlerPresent"] = handler != null,
                ["targetViewPresent"] = targetObject != null,
                ["clickedTargetId"] = clickedTarget?.UniqueId,
                ["frame"] = Time.frameCount,
                ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks
            };
            if (data == null || handler == null || targetObject == null) { record["clicked"] = false; return record; }
            SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
            Game.Instance.DefaultPointerController.ClearPointerMode();
            var selected = SelectionManager.Instance.SelectedUnits;
            record["selectedIds"] = new JArray(selected.Select(u => u.UniqueId));
            var before = controls.CaptureSnapshot();
            bool clicked;
            try
            {
                handler.SetAbility(data);
                handler.GetPriority(targetObject, clickedTarget.Position);
                clicked = handler.OnClick(targetObject, clickedTarget.Position, 0, false, false);
            }
            finally { handler.DropAbility(); }
            var after = controls.CaptureSnapshot();
            var shell = rider.Commands?.Raw?.OfType<UnitUseAbility>().FirstOrDefault(c => ReferenceEquals(c.Spell?.Blueprint, blueprint));
            record["clicked"] = clicked;
            record["targetSelectionStartDelta"] = after.TargetSelectionStartCount - before.TargetSelectionStartCount;
            record["targetSelectionEndDelta"] = after.TargetSelectionEndCount - before.TargetSelectionEndCount;
            record["nativeCastRequestDelta"] = after.NativeCastRequestCount - before.NativeCastRequestCount;
            record["nativeRefusalDelta"] = after.NativeRefusalCount - before.NativeRefusalCount;
            record["dispatchAcceptedDelta"] = after.DispatchAcceptedCount - before.DispatchAcceptedCount;
            record["dispatchRejectedDelta"] = after.DispatchRejectedCount - before.DispatchRejectedCount;
            record["shell"] = DescribeFoundationCommand(shell);
            record["shellInMoveSlot"] = shell != null && ReferenceEquals(rider.Commands.GetCommand(UnitCommand.CommandType.Move), shell);
            return record;
        }

        private static JObject DescribeAvailability(NativeMountedControlAvailability availability) => new JObject
        {
            ["visible"] = availability.IsVisible,
            ["enabled"] = availability.IsEnabled,
            ["transitionReady"] = availability.IsTransitionReady,
            ["reason"] = availability.Reason
        };

        // ---- Real-time (P04) ---------------------------------------------------------------

        private void BeginFoundationTransition()
        {
            foundationSettled = 0;
            Write("rt-foundation-combat-ready", RealtimeObservation());
            // The two closure cases that observe the pending approach first move the rider outside the approach radius.
            stage = RealtimeClosure && (ClosureUnsettled || ClosurePendingArea) ? 38 : 40;
        }

        // The native Mount/Dismount availability lawfully requires the exact single rider selection (the
        // product's exactCasterSelected); preview.152 waited on it without selecting (CM07-mount-save-rt).
        private JObject EnsureFoundationRiderSelection()
        {
            var manager = SelectionManager.Instance;
            if (manager != null && rider?.View != null) manager.SelectUnit(rider.View, true, true, false);
            var selected = manager?.SelectedUnits;
            return new JObject
            {
                ["riderId"] = rider?.UniqueId,
                ["selectedCount"] = selected?.Count ?? -1,
                ["selectedIds"] = selected == null ? null : new JArray(selected.Select(u => u?.UniqueId)),
                ["exactSingleRider"] = selected != null && selected.Count == 1 && selected[0] == rider,
                ["frame"] = Time.frameCount
            };
        }

        private void AdvanceFoundationTransitionRequest()
        {
            var game = Game.Instance;
            var kind = RealtimeCombatMount ? NativeMountedControlKind.MountCompanion : NativeMountedControlKind.Dismount;
            var expectedBefore = RealtimeCombatMount ? RelationshipState.Unmounted : RelationshipState.Mounted;
            if (relationship.State != expectedBefore)
                throw new InvalidOperationException("Foundation transition starts from " + relationship.State + " instead of " + expectedBefore + ".");
            if (!rider.Commands.Empty || !mount.Commands.Empty || rider.AreHandsBusyWithAnimation ||
                game.HandsEquipmentController.IsUpdateScheduledFor(rider)) return;
            var selection = EnsureFoundationRiderSelection();
            controls.Update();
            var availability = controls.Evaluate(kind, rider);
            if (!availability.IsEnabled)
            {
                if (foundationAvailabilityWaits++ == 0)
                    Write("rt-foundation-availability-wait", new JObject { ["kind"] = kind.ToString(), ["selection"] = selection,
                        ["availability"] = DescribeAvailability(availability), ["actual"] = RealtimeObservation() });
                return;
            }
            foundationBefore = FoundationObservation();
            foundationBefore["debt"] = FoundationActorDebt();
            var click = TryFoundationAbilityClick(RealtimeCombatMount ? controls.MountAbility : controls.DismountAbility,
                RealtimeCombatMount ? mount : rider);
            foundationClickFrame = Time.frameCount;
            Write(RealtimeCombatMount ? "rt-combat-mount-click" : "rt-combat-dismount-click", new JObject
            {
                ["availability"] = DescribeAvailability(availability),
                ["before"] = foundationBefore,
                ["click"] = click,
                ["actual"] = RealtimeObservation()
            });
            Check((bool)click["clicked"], "RT-foundation-native-click-admitted");
            if (ClosureUnsettled) { BeginClosureUnsettledSave(); return; }
            if (ClosurePendingArea) { stage = 83; return; }
            stage = 41;
        }

        private void AdvanceFoundationTransitionSettle()
        {
            var expectedAfter = RealtimeCombatMount ? RelationshipState.Mounted : RelationshipState.Unmounted;
            var settled = relationship.State == expectedAfter && rider.Commands.Empty && mount.Commands.Empty &&
                !controls.HasUnsettledRelationshipTransition &&
                !(rider.View?.AgentASP?.IsReallyMoving ?? false) && !(mount.View?.AgentASP?.IsReallyMoving ?? false);
            if (!settled) { foundationSettled = 0; return; }
            if (++foundationSettled < FoundationSettleFrames) return;
            controls.Update();
            if (RealtimeCombatMount) BindOwnedControlSlots();
            beforeControls = controls.CaptureSnapshot();
            var observation = RealtimeObservation();
            observation["settledFrames"] = foundationSettled;
            observation["framesSinceClick"] = Time.frameCount - foundationClickFrame;
            observation["before"] = foundationBefore;
            observation["debt"] = FoundationActorDebt();
            Write(RealtimeCombatMount ? "rt-combat-mount-settled" : "rt-combat-dismount-settled", observation);
            if (ClosureRoutes) { stage = 82; return; }
            RequestRealtimeSave();
        }

        private void ValidateFoundationRemainder(MountedSaveData data, double elapsed)
        {
            var game = Game.Instance;
            Check(data.Combat.Current == null && data.Combat.Roster.Length == 0 && !game.TurnBasedCombatController.Initialized,
                "RT-foundation-no-turn-based-activation");
            Check(data.Combat.Actors.All(a =>
            {
                var actor = game.State.Units.Single(u => u.UniqueId == a.Native.Id);
                return actor.CombatState.Prepared == a.Prepared && actor.IsInCombat == a.InCombat &&
                    LegitimateContinuation(a.Native, MountedPersistenceService.CaptureActor(actor), elapsed);
            }), "RT-current-native-debt-follows-restored-game-clock");
            Check(relationship.State == (RealtimeMounted ? RelationshipState.Mounted : RelationshipState.Unmounted),
                "RT-foundation-relationship-as-saved");
            var bindings = controls.CapturePersistentSlots();
            Check(bindings.Length == data.Slots.Length && data.Slots.All(s =>
                bindings.Any(a => a.ActorId == s.ActorId && a.Index == s.Index && a.Kind == s.Kind)) &&
                controls.CaptureSnapshot().DuplicateFactCount == 0, "RT-exact-owned-controls-once");
        }

        // One ordinary native attack after the settled transition (warm after the save, cold after
        // the load): its first delivery is observed without a fresh round, then the shared
        // real-time continuation waits for the real cooldown exactly as the approach cases do.
        private void BeginFoundationContinuation()
        {
            Check(realtimeProbe.RiderResolvedCount == 0 && realtimeProbe.RiderNonOpportunityAttackRuleCount == 0,
                "RT-foundation-save-has-no-delivered-or-replayed-attack");
            approachRoundBaseline = realtimeRounds.Count;
            QueueRealtimeAttack();
            Write("rt-foundation-continuation", RealtimeObservation());
            stage = 22;
        }

        // ---- Turn-based (P02) --------------------------------------------------------------

        private Vector3 FindFoundationDestination(UnitEntityData actor, float distance)
        {
            for (var i = 0; i < 16; i++)
            {
                var wanted = actor.Position + Quaternion.Euler(0, i * 22.5f, 0) * Vector3.forward * distance;
                var actual = Kingmaker.View.ObstacleAnalyzer.TraceAlongNavmesh(actor.Position, wanted);
                if (GeometryUtils.MechanicsDistance(actual, wanted) <= 0.25f &&
                    GeometryUtils.MechanicsDistance(actual, actor.Position) > distance - 0.5f) return actual;
            }
            throw new InvalidOperationException("No native walkable destination exists for the foundation acting entry.");
        }

        // Native Preparing persists while an able player unit is idle. A real, short rider ground
        // order enters Acting; its own Move time is carried into the Mount baseline, never reset.
        private void BeginFoundationActingEntry(TurnController turn)
        {
            if (turn.IsActing) { foundationTurn = turn; stage = 51; return; }
            if (turn.Status != TurnController.TurnStatus.Preparing) return;
            SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
            Game.Instance.DefaultPointerController.ClearPointerMode();
            foundationTurn = turn;
            foundationEntryOrigin = rider.Position;
            var destination = FindFoundationDestination(rider, 0.75f);
            using (var input = new NativeOrdinaryAttackInput(destination))
            {
                input.Predict();
                // A native five-foot step is free. A ground order charged about 0.24 s of Move before the
                // Mount charged its 3 s, which crossed one Move action (UsedTwoMoveAction) and lawfully
                // restricted the Standard the continuation attack needs (preview.153 CM07-mount-save-tb).
                for (var cycle = 0; !turn.EnabledFiveFootStep && cycle < 8; cycle++)
                { input.Click(button: 1); input.Predict(); }
                Check(turn.EnabledFiveFootStep, "P02-combat-mount-tb-native-step-cursor-policy");
                Check(input.Click(), "P02-combat-mount-tb-acting-entry-input");
            }
            foundationEntryMove = rider.Commands.Move as UnitMoveTo;
            Check(foundationEntryMove != null && foundationEntryMove.Executor == rider, "P02-combat-mount-tb-acting-entry-owner");
            var observation = CombatObservation();
            observation["entry"] = new JObject
            {
                ["origin"] = RealtimePoint(rider),
                ["destination"] = new JArray(destination.x, destination.y, destination.z),
                ["fiveFootStep"] = turn.EnabledFiveFootStep,
                ["riderMoveBefore"] = rider.CombatState.Cooldown.MoveAction,
                ["command"] = DescribeFoundationCommand(foundationEntryMove)
            };
            Write("combat-mount-acting-entry-dispatched", observation);
            stage = 50;
        }

        private void AdvanceFoundationTurn(TurnController turn)
        {
            if (turn == null) return;
            if (!ReferenceEquals(turn, foundationTurn))
                throw new InvalidOperationException("The rider's native turn changed before the combat Mount settled (stage " + stage + ").");
            if (stage == 50)
            {
                if (foundationEntryMove == null || !foundationEntryMove.IsFinished || !PairIdle ||
                    (rider.View?.AgentASP?.IsReallyMoving ?? false)) return;
                var observation = CombatObservation();
                observation["entry"] = new JObject
                {
                    ["command"] = DescribeFoundationCommand(foundationEntryMove),
                    ["displacement"] = GeometryUtils.MechanicsDistance(foundationEntryOrigin, rider.Position),
                    ["acting"] = turn.IsActing,
                    ["status"] = turn.Status.ToString(),
                    ["debt"] = FoundationActorDebt()
                };
                Write("combat-mount-acting-entry-completed", observation);
                Check(foundationEntryMove.Result == UnitCommand.ResultType.Success && turn.IsActing,
                    "P02-combat-mount-tb-acting-entry-settled");
                stage = 51;
                return;
            }
            if (stage == 51)
            {
                if (!PairIdle || !turn.IsActing) return;
                controls.Update();
                var selection = EnsureFoundationRiderSelection();
                var availability = controls.Evaluate(NativeMountedControlKind.MountCompanion, rider);
                if (!availability.IsEnabled)
                {
                    if (foundationAvailabilityWaits++ == 0)
                        Write("combat-mount-availability-wait", new JObject { ["selection"] = selection, ["availability"] = DescribeAvailability(availability),
                            ["combat"] = CombatObservation() });
                    return;
                }
                foundationBefore = FoundationObservation();
                foundationBefore["debt"] = FoundationActorDebt();
                var click = TryFoundationAbilityClick(controls.MountAbility, mount);
                foundationClickFrame = Time.frameCount;
                var observation = CombatObservation();
                observation["availability"] = DescribeAvailability(availability);
                observation["before"] = foundationBefore;
                observation["click"] = click;
                Write("combat-mount-click", observation);
                Check((bool)click["clicked"], "P02-combat-mount-tb-native-click-admitted");
                stage = 52;
                return;
            }
            if (stage == 52)
            {
                var settled = relationship.State == RelationshipState.Mounted && PairIdle &&
                    !controls.HasUnsettledRelationshipTransition && combat.PairedActivationIdentity != null &&
                    !(rider.View?.AgentASP?.IsReallyMoving ?? false);
                if (!settled) { foundationSettled = 0; return; }
                if (++foundationSettled < FoundationSettleFrames) return;
                controls.Update();
                BindOwnedControlSlots();
                beforeControls = controls.CaptureSnapshot();
                savedBoundary = turn;
                savedSequence = combat.PairedActivationSequence;
                savedRound = Game.Instance.TurnBasedCombatController.RoundNumber;
                var observation = CombatObservation();
                observation["settledFrames"] = foundationSettled;
                observation["framesSinceClick"] = Time.frameCount - foundationClickFrame;
                observation["before"] = foundationBefore;
                observation["debt"] = FoundationActorDebt();
                Write("combat-mount-settled", observation);
                // Stage 3 requests the native manual save every frame until Kingmaker admits it.
                stage = 3;
            }
        }
    }
}
