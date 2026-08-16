using System;
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
                relationship.Rider.Commands.Run(command);
                if (command.Executor != relationship.Rider || command.IsFinished)
                {
                    activeCommand = null;
                    LastFeedback = "Mounted pair command failed to enter the rider Standard slot.";
                    return MountedCombatClickResult.HandledRejected;
                }
                relationship.Rider.CombatState.ManualTarget = target;
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
            ArmedAction = MountedCombatActionKind.None;
            var command = activeCommand;
            activeCommand = null;
            if (command != null && !command.IsFinished)
            {
                command.Interrupt();
            }
            relationship.Runtime.CancelMountMovement();
            LastFeedback = "Mounted combat cancelled: " + (string.IsNullOrWhiteSpace(reason) ? "boundary" : reason) + ".";
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
                startedUnit == relationship.Mount))
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
            var riderTurn = !CombatController.IsInTurnBasedCombat() ||
                (turn != null && turn.Unit == rider && turn.IsActing);
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
                TargetIsVisibleEnemy = targetValid && target.IsVisibleForPlayer && rider != null &&
                    rider.IsEnemy(target) && rider.CanAttack(target),
                RiderOwnsCurrentTurnOrRealTime = riderTurn,
                RiderHasStandardAction = rider != null && rider.HasStandardAction(),
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
            if (activeCommand == command)
            {
                activeCommand = null;
            }
            LastFeedback = outcome.Result == UnitCommand.ResultType.Success.ToString()
                ? "Mounted pair attack completed."
                : "Mounted pair attack ended: " + outcome.Result + ".";
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
