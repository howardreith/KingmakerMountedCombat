using System;
using System.Globalization;
using Kingmaker;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.GameModes;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.View;
using KingmakerMountedCombat.Diagnostics;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Logging;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Integration
{
    internal enum MountedCombatClickResult
    {
        NotHandled,
        HandledAccepted,
        HandledRejected
    }

    internal sealed class MountedCombatController : IDisposable
    {
        private readonly GameMountedRelationshipService relationship;
        private readonly DiagnosticSettings settings;
        private readonly IModLogger logger;
        private MountedPairAttackCommand activeCommand;
        private MountedPairAttackCommand finishedCommandPendingSweep;
        private bool disposed;

        public MountedCombatController(
            GameMountedRelationshipService relationship,
            DiagnosticSettings settings,
            IModLogger logger)
        {
            this.relationship = relationship ?? throw new ArgumentNullException(nameof(relationship));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            relationship.Dismounting += HandleDismounting;
        }

        public MountedCombatActionKind ArmedAction { get; private set; }

        public string LastFeedback { get; private set; } = "Mounted combat is idle.";

        public MountedPairAttackOutcome LastOutcome { get; private set; }

        public bool HasActiveCommand => activeCommand != null && !activeCommand.IsFinished;

        internal string DescribeActiveCommandReadiness()
        {
            var command = activeCommand;
            var actionActor = command?.ActionActor;
            if (command == null)
            {
                return "active=false";
            }

            var commands = actionActor?.Commands;
            var game = Game.Instance;
            var handsEquipment = game?.HandsEquipmentController;
            var inStandardSlot = commands != null && commands.Standard == command;
            var queued = commands != null && commands.Queue.Contains(command);
            var handsBusy = actionActor != null && actionActor.AreHandsBusyWithAnimation;
            var equipmentUpdateScheduled = actionActor != null && handsEquipment != null &&
                handsEquipment.IsUpdateScheduledFor(actionActor);
            var canAct = actionActor?.Descriptor?.State != null && actionActor.Descriptor.State.CanAct;
            var canActInCombat = actionActor?.CombatState != null && actionActor.CombatState.CanActInCombat;
            var hasCooldown = actionActor?.CombatState != null && actionActor.CombatState.HasCooldownForCommand(command);
            var gamePaused = game != null && game.IsPaused;
            var initiativeCooldown = actionActor?.CombatState == null
                ? "unavailable"
                : actionActor.CombatState.Cooldown.Initiative.ToString("R", CultureInfo.InvariantCulture);
            return "active=true" +
                ";inStandardSlot=" + inStandardSlot +
                ";queued=" + queued +
                ";started=" + command.IsStarted +
                ";running=" + command.IsRunning +
                ";finished=" + command.IsFinished +
                ";result=" + command.Result +
                ";unitEnoughClose=" + command.IsUnitEnoughClose +
                ";handsBusy=" + handsBusy +
                ";dontWaitForHands=" + command.DontWaitForHands +
                ";equipmentUpdateScheduled=" + equipmentUpdateScheduled +
                ";gamePaused=" + gamePaused +
                ";initiativeCooldown=" + initiativeCooldown +
                ";canAct=" + canAct +
                ";canActInCombat=" + canActInCombat +
                ";ignoreCooldown=" + command.IsIgnoreCooldown +
                ";hasCooldown=" + hasCooldown;
        }

        public bool CanShowCombatActions =>
            !disposed &&
            settings.EnableUnsafeMovementExperiment &&
            relationship.State == RelationshipState.Mounted &&
            IsPairInCombat();

        public bool Arm(MountedCombatActionKind action)
        {
            ThrowIfDisposed();
            if (action != MountedCombatActionKind.RiderMelee &&
                action != MountedCombatActionKind.MountPrimaryNatural)
            {
                LastFeedback = "Choose Rider melee or Mammoth primary.";
                return false;
            }
            if (!CanShowCombatActions)
            {
                LastFeedback = "Mounted combat actions require the exact active pair in combat.";
                return false;
            }
            if (HasActiveCommand)
            {
                LastFeedback = "A mounted pair command is already active.";
                return false;
            }

            ArmedAction = action;
            LastFeedback = action == MountedCombatActionKind.RiderMelee
                ? "Rider melee armed: select one visible enemy."
                : "Mammoth primary armed: select one visible enemy.";
            return true;
        }

        public MountedCombatClickResult TryHandleUnitClick(
            GameObject gameObject,
            int button,
            bool simulate)
        {
            if (disposed || ArmedAction == MountedCombatActionKind.None || simulate || button != 0)
            {
                return MountedCombatClickResult.NotHandled;
            }

            var action = ArmedAction;
            ArmedAction = MountedCombatActionKind.None;
            var targetView = gameObject == null ? null : gameObject.GetComponent<UnitEntityView>();
            var target = targetView?.EntityData;
            NativeSingleAttackWeaponSelection mountPrimary;
            var context = CaptureContext(action, target, out mountPrimary);
            var availability = MountedCombatActionEvaluator.Evaluate(context);
            if (!availability.IsAllowed)
            {
                LastFeedback = availability.Feedback;
                return MountedCombatClickResult.HandledRejected;
            }

            try
            {
                var command = new MountedPairAttackCommand(
                    relationship,
                    relationship.Rider,
                    relationship.Mount,
                    target,
                    action,
                    mountPrimary,
                    logger,
                    HandleCommandTerminal);
                activeCommand = command;
                LastOutcome = null;
                var actionActor = command.ActionActor;
                actionActor.Commands.Run(command);
                if (command.Executor != actionActor || command.IsFinished ||
                    (!actionActor.Commands.Contains(command) &&
                     !actionActor.Commands.Queue.Contains(command)))
                {
                    activeCommand = null;
                    LastFeedback = "Mounted pair command failed to enter the action actor Standard slot.";
                    return MountedCombatClickResult.HandledRejected;
                }
                actionActor.CombatState.ManualTarget = target;
                LastFeedback = "Mounted pair command accepted: " + action + ".";
                return MountedCombatClickResult.HandledAccepted;
            }
            catch (Exception exception)
            {
                activeCommand = null;
                LastFeedback = "Mounted pair command failed closed: " + exception.GetType().Name + ".";
                logger.Exception("Mounted combat click", exception);
                return MountedCombatClickResult.HandledRejected;
            }
        }

        public void Update()
        {
            if (disposed)
            {
                return;
            }
            SweepFinishedCommand();
            if (activeCommand != null && activeCommand.IsFinished)
            {
                activeCommand = null;
            }
            if (relationship.State != RelationshipState.Mounted)
            {
                ArmedAction = MountedCombatActionKind.None;
            }
            else if (!IsPairInCombat() && (ArmedAction != MountedCombatActionKind.None || HasActiveCommand))
            {
                Cancel("combat ended");
            }
        }

        public void Cancel(string reason)
        {
            if (disposed)
            {
                return;
            }
            var endingExplicitMountAction = ArmedAction == MountedCombatActionKind.MountPrimaryNatural ||
                activeCommand?.Action == MountedCombatActionKind.MountPrimaryNatural;
            ArmedAction = MountedCombatActionKind.None;
            var command = activeCommand;
            activeCommand = null;
            if (command != null && !command.IsFinished)
            {
                command.Interrupt();
            }
            SweepFinishedCommand();
            relationship.Runtime.CancelMountMovement();
            if (endingExplicitMountAction && CombatController.IsInTurnBasedCombat())
            {
                var turn = Game.Instance?.TurnBasedCombatController?.CurrentTurn;
                if (turn != null && turn.Unit == relationship.Mount)
                {
                    turn.ForceToEnd(false);
                }
            }
            LastFeedback = "Mounted combat cancelled: " + (string.IsNullOrWhiteSpace(reason) ? "boundary" : reason) + ".";
        }

        public bool ShouldSuppressStockOpportunityAttack(
            UnitEntityData attacker,
            UnitEntityData target)
        {
            return !disposed && MountedOpportunityIsolationPolicy.ShouldSuppressStockOpportunityAttack(
                relationship.State == RelationshipState.Mounted,
                HasActiveCommand,
                attacker != null && attacker == relationship.Rider,
                attacker != null && attacker == relationship.Mount,
                target != null);
        }

        public bool TryOverrideMountTurnMovement(
            UnitMovementAgent agent,
            ref float deltaTime,
            out bool result)
        {
            result = false;
            if (disposed || activeCommand == null || activeCommand.IsFinished ||
                agent == null || agent.Unit?.EntityData != relationship.Mount)
            {
                return false;
            }
            var game = Game.Instance;
            var turn = game?.TurnBasedCombatController?.CurrentTurn;
            if (!MountedPairTurnPolicy.CanDelegateMountMovement(
                relationship.State == RelationshipState.Mounted,
                CombatController.IsInTurnBasedCombat(),
                turn?.Unit == relationship.Rider,
                turn != null && turn.IsActing,
                agent.Unit.EntityData == relationship.Mount))
            {
                return false;
            }

            if (!agent.IsReallyMoving || agent.Unit.IsCommandsPreventMovement ||
                (agent.Unit.AnimationManager != null && agent.Unit.AnimationManager.IsPreventingMovement))
            {
                return true;
            }
            ExactTurnMovementAdapter.Tick(turn, ref deltaTime);
            result = deltaTime > 0f;
            return true;
        }

        public void EndExactMountTurn(UnitEntityData startedUnit)
        {
            if (disposed || !MountedPairTurnPolicy.ShouldEndMountTurn(
                relationship.State == RelationshipState.Mounted,
                CombatController.IsInTurnBasedCombat(),
                startedUnit == relationship.Mount,
                ArmedAction == MountedCombatActionKind.MountPrimaryNatural ||
                    activeCommand?.Action == MountedCombatActionKind.MountPrimaryNatural))
            {
                return;
            }
            var turn = Game.Instance?.TurnBasedCombatController?.CurrentTurn;
            if (turn != null && turn.Unit == relationship.Mount)
            {
                Cancel("suppressed Mammoth native turn");
                turn.ForceToEnd();
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }
            Cancel("controller disposal");
            relationship.Dismounting -= HandleDismounting;
            disposed = true;
        }

        private MountedCombatActionContext CaptureContext(
            MountedCombatActionKind action,
            UnitEntityData target,
            out NativeSingleAttackWeaponSelection mountPrimary)
        {
            var rider = relationship.Rider;
            var mount = relationship.Mount;
            var selection = SelectionManager.Instance?.SelectedUnits;
            var exactSelection = selection != null && selection.Count == 1 && selection[0] == rider;
            var targetState = target?.Descriptor?.State;
            var riderState = rider?.Descriptor?.State;
            var mountState = mount?.Descriptor?.State;
            var turn = Game.Instance?.TurnBasedCombatController?.CurrentTurn;
            var actionActor = action == MountedCombatActionKind.RiderMelee ? rider : mount;
            var actionActorTurn = MountedPairTurnPolicy.CanIssueAction(
                CombatController.IsInTurnBasedCombat(),
                turn?.Unit == actionActor,
                turn != null && turn.Status == TurnBased.Controllers.TurnController.TurnStatus.Preparing,
                turn != null && turn.IsActing);
            var riderWeapon = rider?.GetFirstWeapon();
            mountPrimary = action == MountedCombatActionKind.MountPrimaryNatural
                ? NativeSingleAttackWeaponResolver.Resolve(mount)
                : null;
            var targetValid = target != null && target.IsInState && target.View != null;
            return new MountedCombatActionContext
            {
                Action = action,
                FeatureEnabled = settings.EnableUnsafeMovementExperiment,
                ExactMountedPair = exactSelection && relationship.State == RelationshipState.Mounted &&
                    rider != null && mount != null && rider.Descriptor?.Pet == mount,
                SupportedMammothProfile = mount?.Blueprint != null &&
                    string.Equals(mount.Blueprint.AssetGuid, KingmakerMountedPairRuntime.MammothBlueprintGuid, StringComparison.Ordinal),
                InCombat = IsPairInCombat(),
                RiderAliveAndConscious = riderState != null && riderState.IsConscious && !riderState.IsFinallyDead,
                MountAliveAndConscious = mountState != null && mountState.IsConscious && !mountState.IsFinallyDead,
                TargetExists = targetValid,
                TargetAliveAndConscious = targetState != null && targetState.IsConscious && !targetState.IsFinallyDead,
                TargetIsVisibleEnemy = targetValid && target.IsVisibleForPlayer && actionActor != null &&
                    actionActor.IsEnemy(target) && actionActor.CanAttack(target),
                ActionActorOwnsCurrentTurnOrRealTime = actionActorTurn,
                ActionActorHasStandardAction = actionActor != null && actionActor.HasStandardAction(),
                RiderWeaponIsSupportedMelee = riderWeapon?.Blueprint != null && !riderWeapon.Blueprint.IsRanged,
                MountPrimaryNaturalAttackIsExact = mountPrimary?.Kind == NativeSingleAttackSlotKind.PrimaryHand &&
                    mountPrimary.Weapon?.Blueprint != null &&
                    mountPrimary.Weapon.Blueprint.IsNatural && !mountPrimary.Weapon.Blueprint.IsRanged,
                TransactionIdle = !HasActiveCommand,
                LoadingOrLifecycleBoundary = Game.Instance == null ||
                    Game.Instance.CurrentMode != GameModeType.Default && Game.Instance.CurrentMode != GameModeType.Pause
            };
        }

        private bool IsPairInCombat()
        {
            var rider = relationship.Rider;
            var mount = relationship.Mount;
            return rider != null && mount != null &&
                (rider.IsInCombat || mount.IsInCombat || (Game.Instance?.Player?.IsInCombat ?? false));
        }

        private void HandleDismounting(CleanupTrigger trigger)
        {
            Cancel("relationship cleanup " + trigger);
        }

        private void HandleCommandTerminal(
            MountedPairAttackCommand command,
            MountedPairAttackOutcome outcome)
        {
            LastOutcome = outcome;
            finishedCommandPendingSweep = command;
            if (activeCommand == command)
            {
                activeCommand = null;
            }
            LastFeedback = outcome.Result == UnitCommand.ResultType.Success.ToString()
                ? "Mounted pair attack completed."
                : "Mounted pair attack ended: " + outcome.Result + ".";
            if (command.Action == MountedCombatActionKind.MountPrimaryNatural &&
                CombatController.IsInTurnBasedCombat())
            {
                var turn = Game.Instance?.TurnBasedCombatController?.CurrentTurn;
                if (turn != null && turn.Unit == command.ActionActor)
                {
                    turn.ForceToEnd(false);
                }
            }
        }

        private void SweepFinishedCommand()
        {
            var command = finishedCommandPendingSweep;
            if (command == null || !command.IsFinished)
            {
                return;
            }

            var commands = command.ActionActor?.Commands;
            if (commands == null || !commands.Contains(command))
            {
                finishedCommandPendingSweep = null;
                return;
            }

            // InterruptAll deliberately leaves an already-finished raw slot behind.
            // Drain only this exact finished KMC command and only when no unrelated
            // queued command could be advanced by the stock sweep.
            if (commands.Queue.Count != 0)
            {
                return;
            }

            commands.RemoveFinishedAndUpdateQueue();
            if (!commands.Contains(command))
            {
                finishedCommandPendingSweep = null;
            }
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(MountedCombatController));
            }
        }
    }
}
