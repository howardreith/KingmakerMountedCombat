using System;
using System.Globalization;
using System.Linq;
using Kingmaker;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.GameModes;
using Kingmaker.PubSubSystem;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.View;
using Kingmaker.View.MapObjects;
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
        private readonly HorsePrimaryAttackAnimationAdapter horsePrimaryAttackAnimation;
        private readonly UnifiedMountedTurnCoordinator unifiedTurn;
        private readonly MountedPairCommandScheduler pairedCommandScheduler;
        private readonly IModLogger logger;
        private readonly MountedOverlayWorldInputGuard overlayWorldInputGuard = new MountedOverlayWorldInputGuard();
        private MountedPairAttackCommand activeCommand;
        private MountedPairAttackCommand finishedCommandPendingSweep;
        private MountedPairAttackCommand stockIntentCommand;
        private readonly MountedAttackIntent<UnitEntityData, TurnController> stockIntent =
            new MountedAttackIntent<UnitEntityData, TurnController>();
        private UnitEntityData stockIntentTarget => stockIntent.Target;
        private TurnController stockIntentTurn => stockIntent.Turn;
        private UnitEntityData observedStockRequestUnit;
        private UnitEntityData observedStockRequestTarget;
        private int observedStockRequestFrame = -1;
        private MountedDoorInteractionCommand activeDoorInteraction;
        private UnitMoveTo activeRiderTurnGroundMove;
        private UnitMoveTo observedNativeMountTurnMove;
        private UnitMoveTo projectedNativeMountTurnGroundMove;
        private int projectedNativeMountTurnGroundMoveFrame = -1;
        private bool riderTurnGroundMoveAdmissionPending;
        private int activeGroundMoveDriveCount;
        private float groundMoveRiderMoveBefore;
        private float groundMoveMountMoveBefore;
        private bool disposed;

        public MountedCombatController(
            GameMountedRelationshipService relationship,
            DiagnosticSettings settings,
            HorsePrimaryAttackAnimationAdapter horsePrimaryAttackAnimation,
            UnifiedMountedTurnCoordinator unifiedTurn,
            MountedPairCommandScheduler pairedCommandScheduler,
            IModLogger logger)
        {
            this.relationship = relationship ?? throw new ArgumentNullException(nameof(relationship));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.horsePrimaryAttackAnimation = horsePrimaryAttackAnimation ??
                throw new ArgumentNullException(nameof(horsePrimaryAttackAnimation));
            this.unifiedTurn = unifiedTurn ?? throw new ArgumentNullException(nameof(unifiedTurn));
            this.pairedCommandScheduler = pairedCommandScheduler ??
                throw new ArgumentNullException(nameof(pairedCommandScheduler));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            relationship.Dismounting += HandleDismounting;
        }

        public MountedCombatActionKind ArmedAction { get; private set; }

        public string LastFeedback { get; private set; } = "Mounted combat is idle.";

        public MountedPairAttackOutcome LastOutcome { get; private set; }

        public MountedCombatRejectionCode[] LastRejectionCodes { get; private set; } = new MountedCombatRejectionCode[0];

        public bool HasActiveCommand => activeCommand != null && !activeCommand.IsFinished;

        public bool HasActiveGroundMovement => activeRiderTurnGroundMove != null && !activeRiderTurnGroundMove.IsFinished;

        public bool HasActiveDoorInteraction => activeDoorInteraction != null && !activeDoorInteraction.IsFinished;

        internal bool HasStockAttackIntent => stockIntentTarget != null;

        internal long StockAttackNativeRequestCount { get; private set; }

        internal long StockAttackIntentStartCount { get; private set; }

        internal long StockAttackIntentCancelCount { get; private set; }

        internal long StockAttackRiderDispatchCount { get; private set; }

        internal long StockAttackMountDispatchCount { get; private set; }

        internal long StockAttackDuplicateDispatchCount { get; private set; }

        internal string LastStockAttackObservation { get; private set; } = "not-observed";

        internal UnifiedMountedTurnSnapshot CaptureUnifiedTurnSnapshot()
        {
            return unifiedTurn.CaptureSnapshot();
        }

        internal MountedPairCommandSchedulerSnapshot CapturePairedCommandSchedulerSnapshot()
        {
            return pairedCommandScheduler.CaptureSnapshot();
        }

        internal bool HasExactMountMovement =>
            activeRiderTurnGroundMove != null && !activeRiderTurnGroundMove.IsFinished ||
            activeCommand?.DelegatedMove != null && !activeCommand.DelegatedMove.IsFinished ||
            activeDoorInteraction?.DelegatedMove != null && !activeDoorInteraction.DelegatedMove.IsFinished;

        internal bool OwnsExactUnifiedMountCommand(UnitCommand command)
        {
            return command != null && ReferenceEquals(activeCommand, command) &&
                activeCommand.Action == MountedCombatActionKind.MountPrimaryNatural &&
                activeCommand.ActionActor == relationship.Mount;
        }

        internal MountedDoorInteractionOutcome LastDoorInteractionOutcome { get; private set; }

        internal int LastGroundMoveDriveCount { get; private set; }

        internal string LastGroundMoveResult { get; private set; }

        internal string LastGroundMoveExecutorId { get; private set; }

        internal bool LastGroundMoveUsedRiderTurnAdapter { get; private set; }

        internal bool LastGroundMoveSlotRestored { get; private set; }

        internal float LastGroundMoveRiderMoveBefore { get; private set; }

        internal float LastGroundMoveRiderMoveAfter { get; private set; }

        internal float LastGroundMoveMountMoveBefore { get; private set; }

        internal float LastGroundMoveMountMoveAfter { get; private set; }

        internal string LastNativeMountTurnMoveInterruptSource { get; private set; } = "<not-observed>";

        internal bool TryGetExactRiderTurnDelegatedMoveForAnimation(
            UnitEntityData mountCandidate,
            out UnitMoveTo move,
            out string source)
        {
            move = null;
            source = null;
            var turn = Game.Instance?.TurnBasedCombatController?.CurrentTurn;
            if (disposed || relationship.State != RelationshipState.Mounted ||
                !CombatController.IsInTurnBasedCombat() || mountCandidate == null ||
                mountCandidate != relationship.Mount || turn?.Unit != relationship.Rider ||
                turn.Status != TurnController.TurnStatus.Preparing && !turn.IsActing)
            {
                return false;
            }

            if (activeRiderTurnGroundMove != null && !activeRiderTurnGroundMove.IsFinished)
            {
                move = activeRiderTurnGroundMove;
                source = "rider-turn-ground";
            }
            else if (activeCommand?.DelegatedMove != null && !activeCommand.DelegatedMove.IsFinished)
            {
                move = activeCommand.DelegatedMove;
                source = "attack-approach";
            }
            else if (activeDoorInteraction?.DelegatedMove != null && !activeDoorInteraction.DelegatedMove.IsFinished)
            {
                move = activeDoorInteraction.DelegatedMove;
                source = "door-approach";
            }

            var commands = relationship.Mount?.Commands;
            return move != null && move.Executor == relationship.Mount && commands != null &&
                ReferenceEquals(commands.GetCommand(UnitCommand.CommandType.Move), move) &&
                !commands.Queue.Contains(move);
        }

        internal bool TryGetExactHorsePrimaryAnimationContext(
            AttackHandInfo attack,
            out MountedPairAttackCommand command,
            out UnitEntityData horse)
        {
            command = activeCommand;
            horse = relationship.Mount;
            return !disposed && relationship.State == RelationshipState.Mounted &&
                command != null && !command.IsFinished &&
                command.Action == MountedCombatActionKind.MountPrimaryNatural &&
                horse != null && string.Equals(horse.Blueprint?.AssetGuid,
                    HorseCompanionBlueprintService.UnitGuid, StringComparison.Ordinal) &&
                command.ActionActor == horse && command.ChildAttack != null &&
                ReferenceEquals(command.ChildAttack.PlannedAttack, attack) &&
                attack?.Hand?.Owner?.Unit == horse &&
                ReferenceEquals(attack.Weapon, command.ChildAttack.PlannedAttack.Weapon);
        }

        internal void BeginNativeMountTurnMoveObservation(UnitMoveTo command)
        {
            observedNativeMountTurnMove = command != null &&
                relationship.State == RelationshipState.Mounted &&
                command.Executor == relationship.Mount
                    ? command
                    : null;
            LastNativeMountTurnMoveInterruptSource = observedNativeMountTurnMove == null
                ? "<invalid-observation-command>"
                : "<not-interrupted>";
        }

        internal void ObserveCommandInterrupt(UnitCommand command)
        {
            if (ReferenceEquals(command, activeCommand) && !command.IsStarted)
            {
                activeCommand.ObservePreStartInterrupt();
            }
            if (!ReferenceEquals(command, observedNativeMountTurnMove) ||
                !string.Equals(LastNativeMountTurnMoveInterruptSource, "<not-interrupted>", StringComparison.Ordinal))
            {
                if (ReferenceEquals(command, projectedNativeMountTurnGroundMove))
                {
                    ClearProjectedNativeMountTurnGroundMove();
                }
                return;
            }

            var frames = new System.Diagnostics.StackTrace(1, false).GetFrames();
            var source = frames == null
                ? "<stack-unavailable>"
                : string.Join(" <- ", frames.Take(12).Select(frame =>
                {
                    var method = frame.GetMethod();
                    return method == null
                        ? "<unknown>"
                        : (method.DeclaringType?.FullName ?? "<global>") + "." + method.Name;
                }).ToArray());
            LastNativeMountTurnMoveInterruptSource = source +
                "; terminalState=" + DescribeNativeMountTurnMoveInterruptState(command as UnitMoveTo);
            if (ReferenceEquals(command, projectedNativeMountTurnGroundMove))
            {
                ClearProjectedNativeMountTurnGroundMove();
            }
        }

        internal bool TryCompleteNativeMountTurnMoveAtReachedPathEnd(UnitMovementAgent agent)
        {
            var mount = relationship.Mount;
            var rider = relationship.Rider;
            var command = mount?.Commands?.GetCommand(UnitCommand.CommandType.Move) as UnitMoveTo;
            var turn = Game.Instance?.TurnBasedCombatController?.CurrentTurn;
            var turnIsOpen = turn != null &&
                (turn.Status == TurnController.TurnStatus.Preparing || turn.IsActing);
            var independentMountTurn = turnIsOpen && turn.Unit == mount &&
                activeCommand == null && activeDoorInteraction == null && activeRiderTurnGroundMove == null;
            var delegatedRiderTurn = turnIsOpen && turn.Unit == rider &&
                ReferenceEquals(command, activeRiderTurnGroundMove);
            var pathPoints = agent?.Path?.vectorPath;
            var pathHasAtLeastTwoPoints = pathPoints != null && pathPoints.Count >= 2;
            var pathEnd = pathHasAtLeastTwoPoints
                ? pathPoints[pathPoints.Count - 1]
                : Vector3.zero;
            var viewPosition = agent == null ? Vector3.zero : agent.transform.position;
            var pathEndpointDistance = pathHasAtLeastTwoPoints
                ? HorizontalDistance(viewPosition, pathEnd)
                : float.PositiveInfinity;
            var mechanicsDistanceToTarget = command == null
                ? float.PositiveInfinity
                : Kingmaker.Utility.GeometryUtils.MechanicsDistance(viewPosition, command.Target);
            var exactPlayerCreatedMountMove = command != null &&
                command.GetType() == typeof(UnitMoveTo) && command.CreatedByPlayer &&
                command.Executor == mount && !command.IsFinished &&
                mount != null && !mount.Commands.Queue.Contains(command);
            var exactMoveSlot = command != null &&
                ReferenceEquals(mount?.Commands?.GetCommand(UnitCommand.CommandType.Move), command);
            var movementAgentActive = agent != null && agent.Unit?.EntityData == mount &&
                agent.IsReallyMoving && agent.WantsToMove;

            if (!MountedTurnGroundCompletionPolicy.CanBridgeReachedPathEnd(
                    relationship.State == RelationshipState.Mounted,
                    CombatController.IsInTurnBasedCombat(),
                    independentMountTurn || delegatedRiderTurn,
                    exactPlayerCreatedMountMove,
                    exactMoveSlot,
                    pathHasAtLeastTwoPoints,
                    movementAgentActive,
                    pathEndpointDistance,
                    command == null ? float.PositiveInfinity : command.ApproachRadius,
                    mechanicsDistanceToTarget,
                    mount?.View == null ? float.PositiveInfinity : mount.View.Corpulence))
            {
                return false;
            }

            projectedNativeMountTurnGroundMove = command;
            projectedNativeMountTurnGroundMoveFrame = Time.frameCount;
            agent.Stop();
            logger.Info("Accepted exact mounted " + MountDisplayName + " TB path endpoint: commandTargetDistance=" +
                mechanicsDistanceToTarget.ToString("R", CultureInfo.InvariantCulture) +
                "; pathEndpointDistance=" + pathEndpointDistance.ToString("R", CultureInfo.InvariantCulture) +
                "; stockApproachRadius=" + command.ApproachRadius.ToString("R", CultureInfo.InvariantCulture) +
                "; mountCorpulence=" + mount.View.Corpulence.ToString("R", CultureInfo.InvariantCulture) +
                "; independentMountTurn=" + independentMountTurn +
                "; delegatedRiderTurn=" + delegatedRiderTurn + ".");
            return true;
        }

        internal bool ShouldTreatNativeMountTurnMoveAsEnoughClose(UnitCommand command)
        {
            if (!ReferenceEquals(command, projectedNativeMountTurnGroundMove))
            {
                return false;
            }

            if (disposed || relationship.State != RelationshipState.Mounted ||
                command == null || command.Executor != relationship.Mount)
            {
                ClearProjectedNativeMountTurnGroundMove();
                return false;
            }

            return !command.IsFinished ||
                command.Result == UnitCommand.ResultType.Success &&
                Time.frameCount <= projectedNativeMountTurnGroundMoveFrame + 2;
        }

        private string DescribeNativeMountTurnMoveInterruptState(UnitMoveTo command)
        {
            try
            {
                var mount = relationship.Mount;
                var view = mount?.View;
                var agent = view?.AgentASP;
                var path = agent?.Path;
                var pathPoints = path?.vectorPath;
                var hasPathEnd = pathPoints != null && pathPoints.Count != 0;
                var target = command == null ? Vector3.zero : command.Target;
                var viewPosition = view == null ? Vector3.zero : view.transform.position;
                var entityPosition = mount == null ? Vector3.zero : mount.Position;
                var pathEnd = hasPathEnd ? pathPoints[pathPoints.Count - 1] : Vector3.zero;
                var turn = Game.Instance?.TurnBasedCombatController?.CurrentTurn;
                var rawMove = mount?.Commands?.GetCommand(UnitCommand.CommandType.Move);
                return "createdByPlayer=" + (command?.CreatedByPlayer ?? false) +
                    ",executorExact=" + (command != null && command.Executor == mount) +
                    ",rawMoveExact=" + ReferenceEquals(rawMove, command) +
                    ",turnUnitExact=" + (turn?.Unit == mount) +
                    ",turnStatus=" + (turn == null ? "<none>" : turn.Status.ToString()) +
                    ",turnActing=" + (turn?.IsActing ?? false) +
                    ",turnCanMove=" + (turn?.CanMove ?? false) +
                    ",commandApproachRadius=" + FormatFloat(command?.ApproachRadius ?? -1f) +
                    ",agentApproachRadius=" + FormatFloat(agent == null ? -1f : agent.ApproachRadius) +
                    ",agentMaxApproachRadius=" + FormatFloat(agent == null ? -1f : agent.MaxApproachRadius) +
                    ",entityToTarget=" + FormatFloat(command == null ? -1f : HorizontalDistance(entityPosition, target)) +
                    ",viewToTarget=" + FormatFloat(command == null ? -1f : HorizontalDistance(viewPosition, target)) +
                    ",mechanicsToTarget=" + FormatFloat(command == null
                        ? -1f
                        : Kingmaker.Utility.GeometryUtils.MechanicsDistance(viewPosition, target)) +
                    ",pathPointCount=" + (pathPoints?.Count ?? 0) +
                    ",entityToPathEnd=" + FormatFloat(hasPathEnd ? HorizontalDistance(entityPosition, pathEnd) : -1f) +
                    ",targetToPathEnd=" + FormatFloat(command == null || !hasPathEnd
                        ? -1f
                        : HorizontalDistance(target, pathEnd)) +
                    ",agentReallyMoving=" + (agent?.IsReallyMoving ?? false) +
                    ",agentWantsToMove=" + (agent?.WantsToMove ?? false);
            }
            catch (Exception exception)
            {
                return "observation-error:" + exception.GetType().FullName + ":" + exception.Message;
            }
        }

        private static string FormatFloat(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        private static float HorizontalDistance(Vector3 left, Vector3 right)
        {
            var delta = left - right;
            delta.y = 0f;
            return delta.magnitude;
        }

        internal void EndNativeMountTurnMoveObservation(UnitMoveTo command)
        {
            if (ReferenceEquals(command, observedNativeMountTurnMove))
            {
                observedNativeMountTurnMove = null;
            }
            if (ReferenceEquals(command, projectedNativeMountTurnGroundMove))
            {
                ClearProjectedNativeMountTurnGroundMove();
            }
        }

        internal bool HasActivePreChildCommandForTarget(UnitEntityData exactTarget)
        {
            return HasActiveCommand &&
                activeCommand.HasAcceptedTargetBeforeChildAttack(exactTarget);
        }

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
                ";hasCooldown=" + hasCooldown +
                ";commandLifecycle={" + unifiedTurn.DescribeMountCommandEligibilityObservation() + "}";
        }

        public bool CanShowCombatActions =>
            !disposed &&
            settings.EnableUnsafeMovementExperiment &&
            relationship.State == RelationshipState.Mounted &&
            IsPairInCombat();

        private string MountDisplayName => relationship.Runtime.MountDisplayName ?? "Mount";

        public bool Arm(MountedCombatActionKind action)
        {
            ThrowIfDisposed();
            if (action != MountedCombatActionKind.RiderMelee &&
                action != MountedCombatActionKind.RiderRanged &&
                action != MountedCombatActionKind.MountPrimaryNatural)
            {
                LastFeedback = "Choose Rider Primary or " + MountDisplayName + " Primary.";
                return false;
            }
            if (!CanShowCombatActions)
            {
                LastFeedback = "Mounted combat actions require the exact active pair in combat.";
                logger.Info("Rejected mounted overlay action activation: action=" + action + "; feedback=" + LastFeedback);
                return false;
            }
            if (HasActiveCommand || HasActiveGroundMovement || riderTurnGroundMoveAdmissionPending)
            {
                LastFeedback = "A mounted pair command is already active.";
                logger.Info("Rejected mounted overlay action activation: action=" + action + "; feedback=" + LastFeedback);
                return false;
            }

            ArmedAction = action;
            ClearStockAttackIntent("explicit mounted action", true);
            LastRejectionCodes = new MountedCombatRejectionCode[0];
            LastFeedback = action == MountedCombatActionKind.RiderMelee
                ? "Rider melee armed: select one visible enemy."
                : action == MountedCombatActionKind.RiderRanged
                    ? "Rider ranged armed: select one visible enemy."
                    : MountDisplayName + " primary armed: select one visible enemy.";
            logger.Info("Mounted overlay action armed: action=" + action + "; frame=" + Time.frameCount + "; feedback=" + LastFeedback);
            return true;
        }

        internal MountedCombatActionKind ResolveRiderPrimaryAction()
        {
            var weapon = relationship.Rider?.GetFirstWeapon();
            return weapon?.Blueprint != null && weapon.Blueprint.IsRanged
                ? MountedCombatActionKind.RiderRanged
                : MountedCombatActionKind.RiderMelee;
        }

        internal bool ArmRiderPrimary()
        {
            return Arm(ResolveRiderPrimaryAction());
        }

        internal void ObserveStockAttackRequested(UnitEntityData unit, UnitEntityData target)
        {
            if (disposed || relationship.State != RelationshipState.Mounted ||
                unit == null || target == null ||
                unit != relationship.Rider && !(unit == relationship.Mount &&
                    CombatController.IsInTurnBasedCombat() &&
                    Game.Instance?.TurnBasedCombatController?.CurrentTurn?.Unit == unit))
            {
                return;
            }

            observedStockRequestUnit = unit;
            observedStockRequestTarget = target;
            observedStockRequestFrame = Time.frameCount;
            StockAttackNativeRequestCount++;
            LastStockAttackObservation = "native-request;frame=" + observedStockRequestFrame +
                ";unit=" + unit.UniqueId + ";target=" + target.UniqueId;
        }

        internal void MarkPlayerFacingOverlayActivation(int frame)
        {
            overlayWorldInputGuard.MarkActivation(frame);
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
            if (TrySuppressPropagatedOverlayWorldClick())
            {
                return MountedCombatClickResult.HandledRejected;
            }

            var action = ArmedAction;
            ArmedAction = MountedCombatActionKind.None;
            var targetView = gameObject == null ? null : gameObject.GetComponent<UnitEntityView>();
            var target = targetView?.EntityData;
            logger.Info("Mounted combat click observed: action=" + action +
                "; button=" + button +
                "; directUnitView=" + (targetView != null) +
                "; targetId=" + (target?.UniqueId ?? "<none>") +
                "; mode=" + (Game.Instance == null ? "<none>" : Game.Instance.CurrentMode.ToString()) +
                "; turnBased=" + CombatController.IsInTurnBasedCombat() + ".");
            return TryExecuteAction(action, target, "overlay", null);
        }

        internal NativeMountedControlAvailability GetNativeAbilityAvailability(
            MountedCombatActionKind action,
            UnitEntityData caster)
        {
            if (disposed || action != MountedCombatActionKind.RiderMelee &&
                action != MountedCombatActionKind.RiderRanged &&
                action != MountedCombatActionKind.MountPrimaryNatural)
            {
                return new NativeMountedControlAvailability(false, false, "Mounted primary control is unavailable.");
            }

            var kind = action == MountedCombatActionKind.MountPrimaryNatural
                ? NativeMountedControlKind.MountPrimary
                : NativeMountedControlKind.RiderPrimary;
            var turnBased = CombatController.IsInTurnBasedCombat();
            var casterExpected = NativeMountedControlPolicy.IsExpectedPrimaryCaster(
                kind,
                turnBased,
                settings.EnableUnifiedMountedTurn,
                caster != null && caster == relationship.Rider,
                caster != null && caster == relationship.Mount);
            if (!casterExpected)
            {
                return new NativeMountedControlAvailability(
                    true,
                    false,
                    NativeMountedControlPolicy.WrongTurnReason(
                        kind,
                        MountDisplayName,
                        settings.EnableUnifiedMountedTurn));
            }

            NativeSingleAttackWeaponSelection ignored;
            var context = CaptureContext(action, null, out ignored, true);
            var availability = MountedCombatActionEvaluator.Evaluate(context);
            return new NativeMountedControlAvailability(true, availability.IsAllowed, availability.Feedback);
        }

        internal bool CanNativeAbilityTarget(
            MountedCombatActionKind action,
            UnitEntityData caster,
            UnitEntityData target)
        {
            if (!GetNativeAbilityAvailability(action, caster).IsEnabled)
            {
                return false;
            }
            NativeSingleAttackWeaponSelection ignored;
            return MountedCombatActionEvaluator.Evaluate(
                CaptureContext(action, target, out ignored)).IsAllowed;
        }

        internal string DescribeNativeAbilityTargetRejection(
            MountedCombatActionKind action,
            UnitEntityData caster,
            UnitEntityData target)
        {
            var activation = GetNativeAbilityAvailability(action, caster);
            if (!activation.IsEnabled)
            {
                return activation.Reason;
            }
            NativeSingleAttackWeaponSelection ignored;
            return MountedCombatActionEvaluator.Evaluate(
                CaptureContext(action, target, out ignored)).Feedback;
        }

        internal MountedCombatClickResult TryExecuteNativeAbility(
            MountedCombatActionKind action,
            UnitEntityData caster,
            UnitEntityData target)
        {
            var availability = GetNativeAbilityAvailability(action, caster);
            logger.Info("Native mounted primary delivery observed: action=" + action +
                "; casterId=" + (caster?.UniqueId ?? "<none>") +
                "; targetId=" + (target?.UniqueId ?? "<none>") +
                "; mode=" + (Game.Instance == null ? "<none>" : Game.Instance.CurrentMode.ToString()) +
                "; turnBased=" + CombatController.IsInTurnBasedCombat() +
                "; activationEnabled=" + availability.IsEnabled + ".");
            if (!availability.IsEnabled)
            {
                LastFeedback = availability.Reason;
                LastRejectionCodes = new[]
                {
                    availability.Reason.IndexOf("turn", StringComparison.OrdinalIgnoreCase) >= 0
                        ? MountedCombatRejectionCode.WrongTurn
                        : MountedCombatRejectionCode.WrongActionState
                };
                return MountedCombatClickResult.HandledRejected;
            }
            if (HasStockAttackIntent)
            {
                Cancel("explicit native mounted primary replaced stock attack intent");
            }
            return TryExecuteAction(action, target, "native-ability", caster);
        }

        private MountedCombatClickResult TryExecuteAction(
            MountedCombatActionKind action,
            UnitEntityData target,
            string inputSurface,
            UnitEntityData abilityCaster,
            bool allowApproach = true)
        {
            NativeSingleAttackWeaponSelection mountPrimary;
            var ordinaryIntent = inputSurface == "stock-hostile-click" && target == stockIntentTarget;
            var context = CaptureContext(action, target, out mountPrimary, false, ordinaryIntent);
            var availability = MountedCombatActionEvaluator.Evaluate(context);
            if (!availability.IsAllowed)
            {
                LastFeedback = availability.Feedback;
                LastRejectionCodes = availability.RejectionCodes.ToArray();
                logger.Info("Rejected mounted combat request: inputSurface=" + inputSurface +
                    "; abilityCasterId=" + (abilityCaster?.UniqueId ?? "<none>") +
                    "; codes=" + string.Join(",", LastRejectionCodes.Select(code => code.ToString()).ToArray()) +
                    "; feedback=" + LastFeedback);
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
                    horsePrimaryAttackAnimation,
                    logger,
                    HandleCommandTerminal,
                    allowApproach);
                var schedulerRequired = pairedCommandScheduler.RequiresLease(action);
                string schedulerReason;
                if (schedulerRequired &&
                    !pairedCommandScheduler.TryRegister(command, out schedulerReason))
                {
                    LastFeedback = "Mounted pair scheduler rejected command registration: " +
                        schedulerReason + ".";
                    LastRejectionCodes = new[] { MountedCombatRejectionCode.CommandAdmissionFailure };
                    return MountedCombatClickResult.HandledRejected;
                }
                activeCommand = command;
                LastOutcome = null;
                LastRejectionCodes = new MountedCombatRejectionCode[0];
                var actionActor = command.ActionActor;
                actionActor.Commands.Run(command);
                if (command.Executor != actionActor || command.IsFinished ||
                    (!actionActor.Commands.Contains(command) &&
                     !actionActor.Commands.Queue.Contains(command)))
                {
                    pairedCommandScheduler.AbandonRegistration(
                        command,
                        "native command-container admission failed");
                    activeCommand = null;
                    LastFeedback = "Mounted pair command failed to enter the action actor Standard slot.";
                    LastRejectionCodes = new[] { MountedCombatRejectionCode.CommandAdmissionFailure };
                    return MountedCombatClickResult.HandledRejected;
                }
                if (schedulerRequired &&
                    !pairedCommandScheduler.ConfirmAdmission(command, out schedulerReason))
                {
                    activeCommand = null;
                    LastFeedback = "Mounted pair scheduler rejected native admission: " +
                        schedulerReason + ".";
                    LastRejectionCodes = new[] { MountedCombatRejectionCode.CommandAdmissionFailure };
                    return MountedCombatClickResult.HandledRejected;
                }
                actionActor.CombatState.ManualTarget = target;
                LastFeedback = "Mounted pair command accepted: " + action + ".";
                logger.Info("Mounted combat command accepted: action=" + action +
                    "; inputSurface=" + inputSurface +
                    "; abilityCasterId=" + (abilityCaster?.UniqueId ?? "<none>") +
                    "; actorId=" + actionActor.UniqueId +
                    "; targetId=" + target.UniqueId +
                    "; commandOwner=" + command.Executor.UniqueId +
                    "; feedback=" + LastFeedback);
                return MountedCombatClickResult.HandledAccepted;
            }
            catch (Exception exception)
            {
                pairedCommandScheduler.AbandonRegistration(
                    activeCommand,
                    "command admission exception " + exception.GetType().Name);
                activeCommand = null;
                LastFeedback = "Mounted pair command failed closed: " + exception.GetType().Name + ".";
                LastRejectionCodes = new[] { MountedCombatRejectionCode.CommandAdmissionFailure };
                logger.Exception("Mounted combat request from " + inputSurface, exception);
                return MountedCombatClickResult.HandledRejected;
            }
        }

        public void Update()
        {
            if (disposed)
            {
                return;
            }
            pairedCommandScheduler.Update();
            SweepFinishedCommand();
            pairedCommandScheduler.Update();
            if (projectedNativeMountTurnGroundMove != null &&
                projectedNativeMountTurnGroundMove.IsFinished &&
                Time.frameCount > projectedNativeMountTurnGroundMoveFrame + 2)
            {
                ClearProjectedNativeMountTurnGroundMove();
            }
            if (activeDoorInteraction != null && activeDoorInteraction.IsFinished)
            {
                activeDoorInteraction = null;
            }
            DriveRiderTurnGroundMovement();
            SweepRiderTurnGroundMovement();
            if (activeCommand != null && activeCommand.IsFinished)
            {
                activeCommand = null;
            }
            if (relationship.State != RelationshipState.Mounted)
            {
                ArmedAction = MountedCombatActionKind.None;
                ClearProjectedNativeMountTurnGroundMove();
            }
            else if (stockIntent.ObserveCombatEnded(IsPairInCombat()) ||
                !HasStockAttackIntent && !IsPairInCombat() &&
                (ArmedAction != MountedCombatActionKind.None || HasActiveCommand))
            {
                Cancel("combat ended");
            }

            TryDriveStockAttackIntent();
        }

        public void Cancel(string reason)
        {
            if (disposed)
            {
                return;
            }
            ClearStockAttackIntent(reason, true);
            ClearObservedStockRequest();
            ArmedAction = MountedCombatActionKind.None;
            ClearProjectedNativeMountTurnGroundMove();
            overlayWorldInputGuard.Clear();
            var command = activeCommand;
            activeCommand = null;
            if (command != null && !command.IsFinished)
            {
                pairedCommandScheduler.ObserveExternalInterrupt(command, reason);
                command.Interrupt();
            }
            var doorInteraction = activeDoorInteraction;
            activeDoorInteraction = null;
            if (doorInteraction != null && !doorInteraction.IsFinished)
            {
                doorInteraction.Interrupt();
            }
            CancelRiderTurnGroundMovement();
            SweepFinishedCommand();
            relationship.Runtime.CancelMountMovement();
            LastFeedback = "Mounted combat cancelled: " + (string.IsNullOrWhiteSpace(reason) ? "boundary" : reason) + ".";
        }

        public bool TryRouteMountedStockAttack(UnitCommands commands, UnitCommand command)
        {
            var stockAttack = command as UnitAttack;
            var exactStockAttack = stockAttack != null && command.GetType() == typeof(UnitAttack);
            var rider = relationship.Rider;
            var mount = relationship.Mount;
            var ownerIsRider = rider != null && commands == rider.Commands;
            var ownerIsMount = mount != null && commands == mount.Commands;
            if (disposed || relationship.State != RelationshipState.Mounted ||
                !exactStockAttack || !ownerIsRider && !ownerIsMount)
            {
                return true;
            }

            var turnBased = CombatController.IsInTurnBasedCombat();
            var turn = turnBased ? Game.Instance?.TurnBasedCombatController?.CurrentTurn : null;
            var selection = SelectionManager.Instance?.SelectedUnits;
            var principal = ownerIsMount ? mount : rider;
            if (ownerIsMount && (!turnBased || turn?.Unit != mount))
            {
                // The native party click visits each owner separately. Consume only this pair's
                // redundant mount request; never touch another party member's command or selection.
                if (!turnBased && selection != null && selection.Contains(rider))
                {
                    return false;
                }
                LastFeedback = "Select the rider to attack, or use the mount on its own turn.";
                LastRejectionCodes = new[] { MountedCombatRejectionCode.WrongActorOrSelection };
                LastStockAttackObservation = "rejected-hidden-mount-surface";
                ShowStockFeedback();
                return false;
            }

            var exactRiderSelected = UnifiedMountedStockAttackPolicy.ContainsExactPrincipal(selection, principal);
            var target = stockAttack.Target;
            var exactObservedRequest = UnifiedMountedStockAttackPolicy.IsExactObservedPlayerRequest(
                true,
                ownerIsRider || ownerIsMount && turnBased && turn?.Unit == mount,
                exactRiderSelected,
                exactStockAttack,
                observedStockRequestUnit == principal,
                observedStockRequestTarget == target,
                Time.frameCount,
                observedStockRequestFrame);
            ClearObservedStockRequest();
            if (!exactObservedRequest)
            {
                LastFeedback = "Mounted stock attack rejected: no exact current native hostile-click request was observed.";
                LastRejectionCodes = new[] { MountedCombatRejectionCode.CommandAdmissionFailure };
                LastStockAttackObservation = "rejected-unobserved-command";
                // AI/auto-use commands carry no native pointer provenance: do not spam the player.
                return false;
            }

            if (!UnifiedMountedStockAttackPolicy.AllowsOrdinaryInput(
                turnBased, settings.EnableUnifiedMountedTurn, turn?.Unit == principal))
            {
                LastFeedback = "Mounted attacks use separate turns. Select the actor whose turn is active.";
                LastRejectionCodes = new[] { MountedCombatRejectionCode.WrongTurn };
                LastStockAttackObservation = "rejected-separate-turn-fallback";
                ShowStockFeedback();
                return false;
            }

            var actionContext = ownerIsMount ? MountedCombatActionKind.MountPrimaryNatural : ResolveRiderPrimaryAction();
            var weaponContext = principal.GetFirstWeapon();
            var mountWeaponContext = mount.GetFirstWeapon();
            if (target != null && target.IsInState && target.Descriptor?.State != null &&
                target.Descriptor.State.IsConscious && !target.Descriptor.State.IsFinallyDead && principal.IsEnemy(target) &&
                stockIntent.CanContinue(target, turn, ownerIsMount, rider, mount, weaponContext, mountWeaponContext, (int)actionContext))
            {
                // Consume this exact pair request before UnitCommands.Run can replace its windup/approach.
                // The stock pointer loop still dispatches every unrelated selected party member.
                LastStockAttackObservation = "intent-continued;target=" + target.UniqueId;
                return false;
            }
            Cancel("new stock hostile attack");
            stockIntent.Begin(target, turn, ownerIsMount, IsPairInCombat(),
                rider, mount, weaponContext, mountWeaponContext, (int)actionContext);
            StockAttackIntentStartCount++;
            principal.CombatState.ManualTarget = target;
            LastRejectionCodes = new MountedCombatRejectionCode[0];
            LastFeedback = "Mounted stock attack intent accepted: rider principal, " +
                MountDisplayName + " movement authority.";
            LastStockAttackObservation = "intent-start;target=" + target.UniqueId +
                ";ranged=" + (ResolveRiderPrimaryAction() == MountedCombatActionKind.RiderRanged) +
                ";createdByPlayer=" + stockAttack.CreatedByPlayer + ";principal=" + principal.UniqueId +
                ";turnBased=" + CombatController.IsInTurnBasedCombat();
            TryDriveStockAttackIntent();
            return false;
        }

        private void TryDriveStockAttackIntent()
        {
            var target = stockIntentTarget;
            if (disposed || target == null)
            {
                return;
            }

            var turnBased = CombatController.IsInTurnBasedCombat();
            if (turnBased)
            {
                var currentTurn = Game.Instance?.TurnBasedCombatController?.CurrentTurn;
                if (stockIntentTurn == null || currentTurn != stockIntentTurn)
                {
                    Cancel("native actor turn ended");
                    LastStockAttackObservation = "tb-intent-complete-turn-boundary";
                    return;
                }
                var expectedActor = stockIntent.MountActor ? relationship.Mount : relationship.Rider;
                if (currentTurn.Unit != expectedActor ||
                    currentTurn.Status != TurnController.TurnStatus.Preparing && !currentTurn.IsActing)
                {
                    return;
                }
            }

            var rider = relationship.Rider;
            var mount = relationship.Mount;
            var targetState = target.Descriptor?.State;
            var principal = stockIntent.MountActor ? mount : rider;
            var targetValid = UnifiedMountedStockAttackPolicy.IsValidTarget(
                target != null,
                target != null && target.IsInState,
                targetState != null && targetState.IsConscious && !targetState.IsFinallyDead,
                principal != null && principal.IsEnemy(target),
                principal != null && principal.CanAttack(target));
            if (!targetValid && HasActiveCommand && ReferenceEquals(activeCommand, stockIntentCommand))
            {
                // Retire repetition now; the owned child's actual terminal lifecycle decides
                // success versus expected invalidation. Released projectiles and native AoOs survive.
                ClearStockAttackIntent("target invalidation; native child owns terminal outcome", true);
                return;
            }
            // A paused click records intent without dispatching, spending or changing selection.
            // Admission is not a gameplay tick. Native commands accept orders in ordinary Pause;
            // only the native controller may start/tick the current actor after it can advance.
            if (Game.Instance == null || !MountedGameModePolicy.CanQueueMountedAction(Game.Instance.CurrentMode.ToString()))
            {
                return;
            }
            var riderAction = ResolveRiderPrimaryAction();
            var ranged = !stockIntent.MountActor && riderAction == MountedCombatActionKind.RiderRanged;
            var decision = UnifiedMountedStockAttackPolicy.DecideNext(
                relationship.State == RelationshipState.Mounted && rider != null && mount != null,
                targetValid,
                HasActiveCommand,
                turnBased,
                rider != null && rider.HasStandardAction(),
                mount != null && mount.HasStandardAction(),
                ranged,
                IsMountAlreadyInPrimaryRange(target),
                !turnBased || !stockIntent.MountActor,
                !turnBased || stockIntent.MountActor || settings.EnableUnifiedMountedTurn);

            if (decision == MountedStockAttackDecision.Wait)
            {
                return;
            }
            if (decision == MountedStockAttackDecision.CancelInvalidIntent)
            {
                Cancel("stock target invalidation");
                LastStockAttackObservation = "intent-cancel-target-invalid";
                return;
            }
            if (decision == MountedStockAttackDecision.CompleteTurnBasedIntent)
            {
                if (stockIntentCommand == null)
                {
                    LastFeedback = "No Standard action remains for this actor's attack.";
                    ShowStockFeedback();
                }
                ClearStockAttackIntent("native actor resources exhausted", false);
                LastStockAttackObservation = "tb-intent-complete-resources";
                return;
            }

            if (stockIntentCommand != null && !stockIntentCommand.IsFinished)
            {
                StockAttackDuplicateDispatchCount++;
                Cancel("duplicate stock dispatch guard");
                return;
            }

            var action = decision == MountedStockAttackDecision.DispatchRider
                ? riderAction
                : MountedCombatActionKind.MountPrimaryNatural;
            var generation = stockIntent.Generation;
            var result = TryExecuteAction(
                action,
                target,
                "stock-hostile-click",
                rider,
                !ranged || action != MountedCombatActionKind.MountPrimaryNatural);
            if (result != MountedCombatClickResult.HandledAccepted || activeCommand == null)
            {
                ClearStockAttackIntent("stock dispatch rejected", true);
                LastStockAttackObservation = "intent-cancel-dispatch-rejected;action=" + action;
                ShowStockFeedback();
                return;
            }
            if (!stockIntent.Owns(target, generation))
            {
                Cancel("stock intent replaced during native admission");
                return;
            }

            stockIntentCommand = activeCommand;
            if (action == MountedCombatActionKind.MountPrimaryNatural)
            {
                StockAttackMountDispatchCount++;
            }
            else
            {
                StockAttackRiderDispatchCount++;
            }
            LastStockAttackObservation = "dispatch;action=" + action +
                ";target=" + target.UniqueId +
                ";riderStandard=" + rider.CombatState.Cooldown.StandardAction.ToString("R") +
                ";mountStandard=" + mount.CombatState.Cooldown.StandardAction.ToString("R");
        }

        private bool IsMountAlreadyInPrimaryRange(UnitEntityData target)
        {
            var mount = relationship.Mount;
            var selection = NativeSingleAttackWeaponResolver.Resolve(mount);
            if (mount?.View == null || target?.View == null || selection?.Weapon?.Blueprint == null ||
                !NativePrimaryNaturalAttackPolicy.IsExact(
                    selection.Kind,
                    selection.AdditionalLimbIndex,
                    selection.Weapon.Blueprint.IsNatural,
                    selection.Weapon.Blueprint.IsRanged))
            {
                return false;
            }

            var radius = mount.View.Corpulence + target.View.Corpulence +
                selection.Weapon.AttackRange.Meters;
            return mount.DistanceTo(target) <= radius + MountedCombatSpatialPolicy.RangeTolerance &&
                mount.HasLOS(target);
        }

        private void ClearStockAttackIntent(string reason, bool countCancellation)
        {
            var target = stockIntentTarget;
            if (target != null && countCancellation)
            {
                StockAttackIntentCancelCount++;
            }
            if (target != null && relationship.Rider?.CombatState?.ManualTarget == target)
            {
                relationship.Rider.CombatState.ManualTarget = null;
            }
            if (target != null && relationship.Mount?.CombatState?.ManualTarget == target)
            {
                relationship.Mount.CombatState.ManualTarget = null;
            }
            stockIntent.Cancel();
            stockIntentCommand = null;
            if (target != null)
            {
                LastStockAttackObservation = "intent-cleared;reason=" +
                    (string.IsNullOrWhiteSpace(reason) ? "boundary" : reason) +
                    ";target=" + target.UniqueId;
            }
        }

        private void ClearObservedStockRequest()
        {
            observedStockRequestUnit = null;
            observedStockRequestTarget = null;
            observedStockRequestFrame = -1;
        }

        private void ShowStockFeedback()
        {
            var message = LastFeedback;
            EventBus.RaiseEvent<IWarningNotificationUIHandler>(handler => handler.HandleWarning(message, true));
        }

        internal void CancelSelectedInput(string reason)
        {
            var selected = SelectionManager.Instance?.SelectedUnits;
            if (UnifiedMountedStockAttackPolicy.ContainsExactPrincipal(selected, relationship.Rider) ||
                UnifiedMountedStockAttackPolicy.ContainsExactPrincipal(selected, relationship.Mount))
            {
                Cancel(reason);
            }
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
                target != null,
                settings.EnableUnifiedMountedTurn);
        }

        public bool TryOverrideMountTurnMovement(
            UnitMovementAgent agent,
            ref float deltaTime,
            out bool result)
        {
            result = false;
            var attackApproachActive = activeCommand != null && !activeCommand.IsFinished;
            var doorApproachActive = activeDoorInteraction != null && !activeDoorInteraction.IsFinished;
            var riderGroundMoveActive = activeRiderTurnGroundMove != null && !activeRiderTurnGroundMove.IsFinished;
            if (disposed || !attackApproachActive && !doorApproachActive && !riderGroundMoveActive ||
                agent == null || agent.Unit?.EntityData != relationship.Mount)
            {
                return false;
            }
            var game = Game.Instance;
            var turn = game?.TurnBasedCombatController?.CurrentTurn;
            var movementAdmitted = riderGroundMoveActive
                ? MountedPairTurnPolicy.CanDriveRiderGroundMovement(
                    relationship.State == RelationshipState.Mounted,
                    CombatController.IsInTurnBasedCombat(),
                    turn?.Unit == relationship.Rider,
                    turn != null && turn.Status == TurnController.TurnStatus.Preparing,
                    turn != null && turn.IsActing,
                    agent.Unit.EntityData == relationship.Mount)
                : MountedPairTurnPolicy.CanDelegateMountMovement(
                    relationship.State == RelationshipState.Mounted,
                    CombatController.IsInTurnBasedCombat(),
                    turn?.Unit == relationship.Rider,
                    turn != null && turn.IsActing,
                    agent.Unit.EntityData == relationship.Mount);
            if (!movementAdmitted)
            {
                return false;
            }

            if (!agent.IsReallyMoving || agent.Unit.IsCommandsPreventMovement ||
                (agent.Unit.AnimationManager != null && agent.Unit.AnimationManager.IsPreventingMovement))
            {
                return true;
            }
            unifiedTurn.TickMountMovement(turn, ref deltaTime);
            result = deltaTime > 0f;
            return true;
        }

        public bool TryAdmitGroundCommand(UnitEntityData requestedUnit)
        {
            if (disposed || relationship.State != RelationshipState.Mounted || requestedUnit != relationship.Rider)
            {
                return true;
            }
            if (TrySuppressPropagatedOverlayWorldClick())
            {
                return false;
            }

            Cancel("ground command");

            var game = Game.Instance;
            if (game == null || !MountedGameModePolicy.CanAdmitMountedAction(game.CurrentMode.ToString()))
            {
                LastFeedback = "Mounted ground movement is available only in the active world view.";
                LastRejectionCodes = new[] { MountedCombatRejectionCode.WrongActionState };
                return false;
            }

            if (!CombatController.IsInTurnBasedCombat())
            {
                LastRejectionCodes = new MountedCombatRejectionCode[0];
                return true;
            }

            var turn = game.TurnBasedCombatController?.CurrentTurn;
            if (!MountedPairTurnPolicy.CanAdmitRiderGroundMovement(
                true,
                true,
                true,
                turn?.Unit == relationship.Rider,
                turn != null && turn.Status == TurnController.TurnStatus.Preparing,
                turn != null && turn.IsActing))
            {
                LastFeedback = "Move the mounted pair during the rider's turn.";
                LastRejectionCodes = new[] { MountedCombatRejectionCode.WrongTurn };
                return false;
            }

            if (relationship.Mount?.Commands == null ||
                relationship.Mount.Commands.GetCommand(UnitCommand.CommandType.Move) != null ||
                relationship.Mount.Commands.Queue.Count != 0)
            {
                LastFeedback = "Mounted ground movement rejected: the " + MountDisplayName + " Move slot is not idle.";
                LastRejectionCodes = new[] { MountedCombatRejectionCode.AlreadyActiveCommand };
                return false;
            }

            riderTurnGroundMoveAdmissionPending = true;
            LastRejectionCodes = new MountedCombatRejectionCode[0];
            LastFeedback = "Mounted rider-turn ground movement admitted; " + MountDisplayName + " owns pathfinding.";
            return true;
        }

        public void CompleteGroundCommandAdmission(UnitEntityData routedUnit)
        {
            if (!riderTurnGroundMoveAdmissionPending)
            {
                return;
            }

            riderTurnGroundMoveAdmissionPending = false;
            var command = relationship.Mount?.Commands?.GetCommand(UnitCommand.CommandType.Move) as UnitMoveTo;
            if (routedUnit != relationship.Mount || command == null || command.Executor != relationship.Mount ||
                !command.CreatedByPlayer || relationship.Mount.Commands.Queue.Contains(command))
            {
                if (command != null && !command.IsFinished)
                {
                    command.Interrupt(false);
                }
                relationship.Runtime.CancelMountMovement();
                LastFeedback = "Mounted ground movement rejected: exact " + MountDisplayName + " command admission failed.";
                LastRejectionCodes = new[] { MountedCombatRejectionCode.CommandAdmissionFailure };
                return;
            }

            activeRiderTurnGroundMove = command;
            activeGroundMoveDriveCount = 0;
            groundMoveRiderMoveBefore = relationship.Rider.CombatState.Cooldown.MoveAction;
            groundMoveMountMoveBefore = relationship.Mount.CombatState.Cooldown.MoveAction;
            LastGroundMoveResult = null;
            LastGroundMoveExecutorId = command.Executor?.UniqueId;
            LastGroundMoveUsedRiderTurnAdapter = false;
            LastGroundMoveSlotRestored = false;
            LastFeedback = "Mounted ground movement active: " + MountDisplayName + " movement ledger; separate-turn costs remain unqualified.";
            logger.Info("Mounted ground movement accepted: riderId=" + relationship.Rider.UniqueId +
                "; executorId=" + LastGroundMoveExecutorId +
                "; turnStatus=" + (Game.Instance?.TurnBasedCombatController?.CurrentTurn?.Status.ToString() ?? "<none>") +
                "; feedback=" + LastFeedback);
        }

        public bool ShouldAllowStockCommand(UnitCommands commands, UnitCommand command)
        {
            var rider = relationship.Rider;
            if (!MountedStockAttackPolicy.ShouldReject(
                relationship.State == RelationshipState.Mounted,
                rider != null && commands == rider.Commands,
                relationship.Mount != null && commands == relationship.Mount.Commands,
                command != null && command.GetType() == typeof(UnitAttack)))
            {
                return true;
            }

            var ownerIsMount = relationship.Mount != null && commands == relationship.Mount.Commands;
            var selectedWeapon = ownerIsMount ? relationship.Mount.GetFirstWeapon() : rider?.GetFirstWeapon();
            var ranged = selectedWeapon?.Blueprint != null && selectedWeapon.Blueprint.IsRanged;
            LastFeedback = MountedStockAttackPolicy.RejectionFeedback(ownerIsMount, ranged, MountDisplayName);
            LastRejectionCodes = new[]
            {
                ranged
                    ? MountedCombatRejectionCode.MountedRangedUnsupported
                    : MountedCombatRejectionCode.WrongActionState
            };
            logger.Info("Rejected exact mounted pair stock UnitAttack: owner=" +
                (ownerIsMount ? MountDisplayName : "rider") + "; feedback=" + LastFeedback);
            return false;
        }

        public bool TryRouteMountedDoorInteraction(UnitCommands commands, ref UnitCommand command)
        {
            var stockInteraction = command as UnitInteractWithObject;
            var exactDoor = stockInteraction?.Interaction as StandardDoor;
            if (disposed || !MountedInteractionRoutingPolicy.ShouldRouteExactDoor(
                relationship.State == RelationshipState.Mounted,
                commands != null && commands == relationship.Rider?.Commands,
                stockInteraction != null && command.GetType() == typeof(UnitInteractWithObject),
                exactDoor != null && stockInteraction.Interaction.GetType() == typeof(StandardDoor)))
            {
                return true;
            }

            if (HasStockAttackIntent)
            {
                Cancel("door interaction replaced stock attack intent");
            }

            if (HasActiveCommand || HasActiveGroundMovement || HasActiveDoorInteraction ||
                riderTurnGroundMoveAdmissionPending)
            {
                LastFeedback = "Mounted door interaction rejected: another pair command is active.";
                LastRejectionCodes = new[] { MountedCombatRejectionCode.AlreadyActiveCommand };
                logger.Info(LastFeedback);
                return false;
            }

            var mountCommands = relationship.Mount?.Commands;
            if (mountCommands == null || !mountCommands.Empty || mountCommands.Queue.Count != 0)
            {
                LastFeedback = "Mounted door interaction rejected: " + MountDisplayName +
                    " is busy; wait for its current command to finish.";
                LastRejectionCodes = new[] { MountedCombatRejectionCode.AlreadyActiveCommand };
                logger.Info(LastFeedback);
                return false;
            }

            var turn = Game.Instance?.TurnBasedCombatController?.CurrentTurn;
            if (!MountedInteractionRoutingPolicy.CanAdmitInCurrentTurn(
                CombatController.IsInTurnBasedCombat(),
                turn?.Unit == relationship.Rider,
                turn != null && turn.Status == TurnController.TurnStatus.Preparing,
                turn != null && turn.IsActing))
            {
                LastFeedback = "Open an approached door during the rider's turn.";
                LastRejectionCodes = new[] { MountedCombatRejectionCode.WrongTurn };
                logger.Info("Rejected mounted door interaction: " + LastFeedback);
                return false;
            }

            var routed = new MountedDoorInteractionCommand(
                relationship,
                relationship.Rider,
                relationship.Mount,
                exactDoor,
                logger,
                HandleDoorInteractionTerminal);
            activeDoorInteraction = routed;
            LastDoorInteractionOutcome = null;
            LastRejectionCodes = new MountedCombatRejectionCode[0];
            LastFeedback = "Mounted door interaction accepted: " + MountDisplayName + " approach, rider interaction.";
            command = routed;
            logger.Info("Mounted door interaction routed: riderId=" + relationship.Rider.UniqueId +
                "; mountId=" + relationship.Mount.UniqueId +
                "; door=" + exactDoor.name + "; feedback=" + LastFeedback);
            return true;
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
            out NativeSingleAttackWeaponSelection mountPrimary,
            bool assumeTargetAvailable = false,
            bool ordinaryIntent = false)
        {
            var rider = relationship.Rider;
            var mount = relationship.Mount;
            var selection = SelectionManager.Instance?.SelectedUnits;
            var exactRiderSelection = selection != null && selection.Count == 1 && selection[0] == rider;
            var exactMountSelection = selection != null && selection.Count == 1 && selection[0] == mount;
            var targetState = target?.Descriptor?.State;
            var riderState = rider?.Descriptor?.State;
            var mountState = mount?.Descriptor?.State;
            var turn = Game.Instance?.TurnBasedCombatController?.CurrentTurn;
            var actionActor = action == MountedCombatActionKind.MountPrimaryNatural ? mount : rider;
            var turnBasedCombat = CombatController.IsInTurnBasedCombat();
            var actionActorTurn = MountedPairTurnPolicy.CanIssueSharedAction(
                turnBasedCombat,
                settings.EnableUnifiedMountedTurn,
                turn?.Unit == rider,
                turn?.Unit == actionActor,
                turn != null && turn.Status == TurnBased.Controllers.TurnController.TurnStatus.Preparing,
                turn != null && turn.IsActing);
            var riderWeapon = rider?.GetFirstWeapon();
            mountPrimary = action == MountedCombatActionKind.MountPrimaryNatural
                ? NativeSingleAttackWeaponResolver.Resolve(mount)
                : null;
            var targetValid = assumeTargetAvailable || target != null && target.IsInState && target.View != null;
            return new MountedCombatActionContext
            {
                Action = action,
                FeatureEnabled = settings.EnableUnsafeMovementExperiment,
                ExactMountedPair = relationship.State == RelationshipState.Mounted &&
                    rider != null && mount != null && rider.Descriptor?.Pet == mount &&
                    mount.Descriptor?.Master.Value == rider,
                ExactRiderSelection = ordinaryIntent || MountedTurnSelectionPolicy.IsExpectedActionSelection(
                    turnBasedCombat,
                    action == MountedCombatActionKind.MountPrimaryNatural,
                    settings.EnableUnifiedMountedTurn,
                    exactRiderSelection,
                    exactMountSelection),
                SupportedMountProfile = SupportedMountedProfiles.Resolve(mount) != null &&
                    string.Equals(relationship.Runtime.MountProfileId,
                        SupportedMountedProfiles.Resolve(mount)?.Id,
                        StringComparison.Ordinal),
                MountDisplayName = relationship.Runtime.MountDisplayName,
                SupportedRiderBodyProfile = rider != null && rider.GetActivePolymorph() == null &&
                    relationship.Runtime.PoseHealthy && relationship.IsExactCapturedView(rider),
                InCombat = IsPairInCombat(),
                NativeHostileInitiation = ordinaryIntent && !turnBasedCombat,
                RiderAliveAndConscious = riderState != null && riderState.IsConscious && !riderState.IsFinallyDead,
                MountAliveAndConscious = mountState != null && mountState.IsConscious && !mountState.IsFinallyDead,
                TargetExists = targetValid,
                TargetAliveAndConscious = assumeTargetAvailable ||
                    targetState != null && targetState.IsConscious && !targetState.IsFinallyDead,
                TargetVisible = assumeTargetAvailable || targetValid && target.IsVisibleForPlayer,
                TargetHostile = assumeTargetAvailable || targetValid && actionActor != null && actionActor.IsEnemy(target),
                TargetAttackable = assumeTargetAvailable || targetValid && actionActor != null && actionActor.CanAttack(target),
                TargetIsVisibleEnemy = assumeTargetAvailable || targetValid && target.IsVisibleForPlayer && actionActor != null &&
                    actionActor.IsEnemy(target) && actionActor.CanAttack(target),
                ActionActorOwnsCurrentTurnOrRealTime = actionActorTurn,
                ActionActorHasStandardAction = actionActor != null && actionActor.HasStandardAction(),
                RiderHasEligibleWeapon = riderWeapon?.Blueprint != null,
                RiderWeaponIsRanged = riderWeapon?.Blueprint != null && riderWeapon.Blueprint.IsRanged,
                RiderWeaponCategorySupported = riderWeapon?.Blueprint != null,
                RiderWeaponIsSupportedMelee = riderWeapon?.Blueprint != null && !riderWeapon.Blueprint.IsRanged &&
                    !riderWeapon.Blueprint.IsNatural,
                MountPrimaryNaturalAttackIsExact = mountPrimary?.Weapon?.Blueprint != null &&
                    NativePrimaryNaturalAttackPolicy.IsExact(
                        mountPrimary.Kind,
                        mountPrimary.AdditionalLimbIndex,
                        mountPrimary.Weapon.Blueprint.IsNatural,
                        mountPrimary.Weapon.Blueprint.IsRanged),
                TransactionIdle = (!HasActiveCommand || HasStockAttackIntent && ReferenceEquals(activeCommand, stockIntentCommand)) &&
                    !HasActiveGroundMovement && !riderTurnGroundMoveAdmissionPending,
                LoadingOrLifecycleBoundary = Game.Instance == null ||
                    !MountedGameModePolicy.CanQueueMountedAction(Game.Instance.CurrentMode.ToString()),
                PathKnownUnavailable = false,
                WithinSupportedRangeEnvelope = true,
                RangeOriginConsistent = true,
                CommandAdmissionReady = action != MountedCombatActionKind.MountPrimaryNatural ||
                    !turnBasedCombat || !settings.EnableUnifiedMountedTurn ||
                    pairedCommandScheduler.CanRegisterCurrentPair(out _)
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
            pairedCommandScheduler.ObserveTerminal(
                command,
                outcome?.Result,
                outcome?.TerminalReason);
            var stockIntentOwned = ReferenceEquals(stockIntentCommand, command);
            if (stockIntentOwned)
            {
                stockIntentCommand = null;
            }
            LastOutcome = outcome;
            finishedCommandPendingSweep = command;
            if (activeCommand == command)
            {
                activeCommand = null;
            }
            LastFeedback = outcome.Result == UnitCommand.ResultType.Success.ToString()
                ? "Mounted pair attack completed."
                : DescribeTerminalFailure(outcome);
            logger.Info("Mounted combat command terminal: action=" + command.Action +
                "; result=" + outcome.Result +
                "; reason=" + (outcome.TerminalReason ?? "<none>") +
                "; childAttacks=" + outcome.ChildAttackStartCount +
                "; repaths=" + outcome.RepathCount +
                "; rejectionCodes=" + string.Join(",", LastRejectionCodes.Select(code => code.ToString()).ToArray()) +
                "; feedback=" + LastFeedback);
            if (stockIntentOwned &&
                !string.Equals(outcome.Result, UnitCommand.ResultType.Success.ToString(), StringComparison.Ordinal))
            {
                ClearStockAttackIntent("stock command terminal " + outcome.Result, true);
                LastStockAttackObservation = "intent-cancel-terminal;action=" + command.Action +
                    ";result=" + outcome.Result;
            }
        }

        private void HandleDoorInteractionTerminal(
            MountedDoorInteractionCommand command,
            MountedDoorInteractionOutcome outcome)
        {
            LastDoorInteractionOutcome = outcome;
            if (activeDoorInteraction == command)
            {
                activeDoorInteraction = null;
            }
            LastFeedback = outcome.Result == UnitCommand.ResultType.Success.ToString() &&
                outcome.InteractionCount == 1 && outcome.DoorStateChanged
                ? "Mounted door interaction completed."
                : "Mounted door interaction ended: " + outcome.Result + ".";
            logger.Info("Mounted door interaction terminal: result=" + outcome.Result +
                "; interactions=" + outcome.InteractionCount +
                "; moveStarts=" + outcome.DelegatedMoveStartCount +
                "; moveTicks=" + outcome.DelegatedMoveTickCount +
                "; doorStateChanged=" + outcome.DoorStateChanged +
                "; riderPathSuppressed=" + outcome.RiderPathSuppressed +
                "; mountMoveSlotRestored=" + outcome.MountMoveSlotRestored +
                "; reason=" + (outcome.TerminalReason ?? "<none>") +
                "; feedback=" + LastFeedback);
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

        private string DescribeTerminalFailure(MountedPairAttackOutcome outcome)
        {
            var reason = outcome?.TerminalReason ?? string.Empty;
            if (reason.IndexOf("repath", StringComparison.OrdinalIgnoreCase) >= 0 ||
                reason.IndexOf("Move", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                LastRejectionCodes = new[] { MountedCombatRejectionCode.NoPath };
                return "Mounted rider melee ended: the Mammoth could not complete a supported path.";
            }
            if (reason.IndexOf("range", StringComparison.OrdinalIgnoreCase) >= 0 ||
                reason.IndexOf("admission bridge", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                LastRejectionCodes = new[] { MountedCombatRejectionCode.RangeOriginMismatch };
                return "Mounted rider melee ended: the Mammoth-origin and native rider range gates did not agree.";
            }
            if (reason.IndexOf("target", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                LastRejectionCodes = new[] { MountedCombatRejectionCode.TargetInvalid };
                return "Mounted rider melee ended: the exact target became invalid before completion.";
            }
            if (reason.IndexOf("bounded execution time", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                LastRejectionCodes = new[] { MountedCombatRejectionCode.OutsideSupportedRange };
                return "Mounted rider melee ended: the target exceeded the bounded approach window.";
            }

            LastRejectionCodes = new[] { MountedCombatRejectionCode.CommandAdmissionFailure };
            return "Mounted pair attack ended: " + (outcome == null ? "unknown" : outcome.Result) + ".";
        }

        private bool TrySuppressPropagatedOverlayWorldClick()
        {
            if (!overlayWorldInputGuard.TryConsumePropagatedWorldClick(Time.frameCount))
            {
                return false;
            }

            logger.Info("Suppressed one frame-bounded world click propagated from the mounted overlay; armed action retained=" + ArmedAction + ".");
            return true;
        }

        private void SweepRiderTurnGroundMovement()
        {
            var command = activeRiderTurnGroundMove;
            if (command == null)
            {
                return;
            }

            var turn = Game.Instance?.TurnBasedCombatController?.CurrentTurn;
            var exactTurn = CombatController.IsInTurnBasedCombat() && turn?.Unit == relationship.Rider &&
                turn != null && (turn.Status == TurnController.TurnStatus.Preparing || turn.IsActing);
            var commands = relationship.Mount?.Commands;
            var rawMove = commands?.GetCommand(UnitCommand.CommandType.Move);
            if (!exactTurn || rawMove != command && !command.IsFinished)
            {
                CancelRiderTurnGroundMovement();
                LastFeedback = "Mounted ground movement cancelled: rider turn or exact " + MountDisplayName + " Move-slot ownership changed.";
                return;
            }

            if (!command.IsFinished)
            {
                return;
            }

            if (commands != null && commands.Contains(command) && commands.Queue.Count == 0)
            {
                commands.RemoveFinishedAndUpdateQueue();
            }
            activeRiderTurnGroundMove = null;
            relationship.Runtime.CancelMountMovement();
            LastGroundMoveDriveCount = activeGroundMoveDriveCount;
            LastGroundMoveResult = command.Result.ToString();
            LastGroundMoveSlotRestored = commands != null &&
                commands.GetCommand(UnitCommand.CommandType.Move) == null &&
                !commands.Contains(command) && commands.Queue.Count == 0;
            LastGroundMoveRiderMoveBefore = groundMoveRiderMoveBefore;
            LastGroundMoveRiderMoveAfter = relationship.Rider.CombatState.Cooldown.MoveAction;
            LastGroundMoveMountMoveBefore = groundMoveMountMoveBefore;
            LastGroundMoveMountMoveAfter = relationship.Mount.CombatState.Cooldown.MoveAction;
            LastFeedback = command.Result == UnitCommand.ResultType.Success
                ? "Mounted ground movement completed."
                : "Mounted ground movement ended: " + command.Result + ".";
            logger.Info("Mounted ground movement terminal: result=" + LastGroundMoveResult +
                "; executorId=" + LastGroundMoveExecutorId +
                "; driveCount=" + LastGroundMoveDriveCount +
                "; riderAdapter=" + LastGroundMoveUsedRiderTurnAdapter +
                "; slotRestored=" + LastGroundMoveSlotRestored +
                "; riderMove=" + LastGroundMoveRiderMoveBefore.ToString("R", CultureInfo.InvariantCulture) +
                "->" + LastGroundMoveRiderMoveAfter.ToString("R", CultureInfo.InvariantCulture) +
                "; mountMove=" + LastGroundMoveMountMoveBefore.ToString("R", CultureInfo.InvariantCulture) +
                "->" + LastGroundMoveMountMoveAfter.ToString("R", CultureInfo.InvariantCulture) + ".");
        }

        private void DriveRiderTurnGroundMovement()
        {
            var command = activeRiderTurnGroundMove;
            if (command == null || command.IsFinished || !CombatController.IsInTurnBasedCombat())
            {
                return;
            }

            var turn = Game.Instance?.TurnBasedCombatController?.CurrentTurn;
            if (!MountedPairTurnPolicy.CanDriveRiderGroundMovement(
                    relationship.State == RelationshipState.Mounted,
                    true,
                    turn?.Unit == relationship.Rider,
                    turn != null && turn.Status == TurnController.TurnStatus.Preparing,
                    turn != null && turn.IsActing,
                    relationship.Mount?.Commands?.GetCommand(UnitCommand.CommandType.Move) == command))
            {
                return;
            }

            LastGroundMoveUsedRiderTurnAdapter = true;
            if (!command.IsStarted)
            {
                activeGroundMoveDriveCount++;
                command.TickApproaching();
                if (command.IsUnitEnoughClose && !relationship.Mount.View.MovementAgent.IsReallyMoving)
                {
                    command.Start();
                }
            }
            if (command.IsRunning)
            {
                activeGroundMoveDriveCount++;
                command.Tick();
            }
        }

        private void CancelRiderTurnGroundMovement()
        {
            riderTurnGroundMoveAdmissionPending = false;
            var command = activeRiderTurnGroundMove;
            activeRiderTurnGroundMove = null;
            if (command != null && !command.IsFinished)
            {
                command.Interrupt(false);
            }
        }

        private void ClearProjectedNativeMountTurnGroundMove()
        {
            projectedNativeMountTurnGroundMove = null;
            projectedNativeMountTurnGroundMoveFrame = -1;
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
