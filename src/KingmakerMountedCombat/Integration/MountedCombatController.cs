using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Kingmaker.Blueprints;
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

    internal sealed partial class MountedCombatController : IDisposable
    {
        private readonly GameMountedRelationshipService relationship;
        private readonly DiagnosticSettings settings;
        private readonly HorsePrimaryAttackAnimationAdapter horsePrimaryAttackAnimation;
        private readonly MountedPairTurnParticipant unifiedTurn;
        internal MountedPairTurnParticipant TurnParticipant => unifiedTurn;
        internal int TrackedActorAllocations => unifiedTurn.TrackedAllocationActorCount;
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
        private readonly ObservedPlayerAttackRequest<UnitEntityData, UnitCommand> observedStockRequest =
            new ObservedPlayerAttackRequest<UnitEntityData, UnitCommand>();
        private readonly MountedAutomaticAttackGate automaticGate = new MountedAutomaticAttackGate();
        private readonly MountedAutomaticRunLedger automaticRuns = new MountedAutomaticRunLedger();
        private readonly MountedPlayerMoveWatch playerMove = new MountedPlayerMoveWatch();
        private bool groundOrderOpen;
        private const string AutomaticInputSurface = "automatic-native-target";
        private UnitEntityData automaticApproachFailedTarget;
        private MountedDoorInteractionCommand activeDoorInteraction;
        // Sprint 2 (SPACE-01): the owned post-interaction occupancy settlement of the last door click.
        private MountedDoorOccupancySettlement pendingDoorSettlement;
        // Sprint 3 (TRAP-01, SPELL-01): the mount's delegated approach for the rider's own unstarted native
        // command (spell/item/touch delivery, discovered-trap interaction). One per pair; released when that
        // command ends or is replaced, or when the pair's ownership ends.
        private MountedCarriedApproach carriedApproach;
        // The last frame on which the native turn movement override admitted the mount with budget left.
        private int mountMovementOpportunityFrame = -1;
        private UnitMoveTo activeRiderTurnGroundMove;
        private MountedPairOwner<UnitEntityData, GameMountedRelationshipService> groundMoveOwner;
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
            MountedPairTurnParticipant unifiedTurn,
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
            relationship.CombatOwner = this;
            relationship.Dismounting += HandleDismounting;
            relationship.ChargeCleanupBarrier = trigger => TryDrainChargeOwnership("relationship cleanup " + trigger);
        }

        public MountedCombatActionKind ArmedAction { get; private set; }

        public string LastFeedback { get; private set; } = "Mounted combat is idle.";

        public MountedPairAttackOutcome LastOutcome { get; private set; }

        public MountedCombatRejectionCode[] LastRejectionCodes { get; private set; } = new MountedCombatRejectionCode[0];

        public bool HasActiveCommand => activeCommand != null && !activeCommand.IsFinished;

        public bool HasPendingDoorSettlement => pendingDoorSettlement != null && !pendingDoorSettlement.IsFinished;

        internal bool MountMovementOpportunityRecently => mountMovementOpportunityFrame >= 0 && Time.frameCount - mountMovementOpportunityFrame <= 2;

        internal bool HasRetainedPairCommands => HasChargeOwnership || activeCommand != null ||
            finishedCommandPendingSweep != null || activeDoorInteraction != null || pendingDoorSettlement != null || activeRiderTurnGroundMove != null ||
            riderTurnGroundMoveAdmissionPending || HasStockAttackIntent || HasLiveCarriedApproach;

        public bool HasLiveCarriedApproach => carriedApproach != null && carriedApproach.IsLive;

        internal bool HasLiveCarriedApproachMove => carriedApproach != null && carriedApproach.HasLiveDelegatedMove;

        internal bool PreservesApproachParent(UnitCommands commands, UnitCommand.CommandType type, bool interruptPaired)
        {
            return !disposed && relationship.State == RelationshipState.Mounted &&
                activeCommand != null && activeCommand.PreservesApproachParent(commands, type, interruptPaired);
        }

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

        internal bool StockIntentIsAutomatic => stockIntent.Automatic;

        internal MountedPairAttackCommand CurrentPairCommand => activeCommand;

        internal UnitEntityData CurrentStockIntentTarget => stockIntentTarget;

        internal bool AutomaticSuppressedByStop => automaticGate.IsSuppressed(relationship.MountedPairGeneration);

        internal long AutomaticProposalCount { get; private set; }

        internal long AutomaticAdmittedCount { get; private set; }

        internal long AutomaticContinuedCount { get; private set; }

        internal long AutomaticRefusedCount { get; private set; }

        internal long AutomaticDispatchCount { get; private set; }

        internal long AutomaticRetiredCount { get; private set; }

        internal long AutomaticHoldKeptCount { get; private set; }

        internal long AutomaticQueuedHoldKeptCount { get; private set; }

        internal long AutomaticQueuedRunCount { get; private set; }

        internal long AutomaticQueuedRefusedCount { get; private set; }

        internal bool PlayerMoveHoldArmed => automaticGate.PlayerMoveHold;

        internal object WatchedPlayerMove => playerMove.Move;

        internal MountedPlayerMoveOutcome LastPlayerMoveOutcome { get; private set; }

        internal string LastAutomaticRefusal { get; private set; } = "not-observed";

        internal string LastAutomaticRetirement { get; private set; } = "not-observed";

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
            activeDoorInteraction?.DelegatedMove != null && !activeDoorInteraction.DelegatedMove.IsFinished ||
            pendingDoorSettlement?.RecoveryMove != null || HasLiveCarriedApproachMove;

        internal bool OwnsExactUnifiedMountCommand(UnitCommand command)
        {
            return command != null && ReferenceEquals(activeCommand, command) &&
                activeCommand.Action == MountedCombatActionKind.MountPrimaryNatural &&
                activeCommand.ActionActor == relationship.Mount;
        }

        internal bool OwnsExactPairedNativeCommand(UnitCommand command)
        {
            if (command == null || command.Executor != relationship.Mount || command.IsFinished) return false;
            var commands = relationship.Mount.Commands;
            return ReferenceEquals(commands.GetCommand(command.Type), command) &&
                (OwnsExactUnifiedMountCommand(command) || ReferenceEquals(command, activeRiderTurnGroundMove) ||
                 ReferenceEquals(command, activeCommand?.DelegatedMove) || ReferenceEquals(command, activeDoorInteraction?.DelegatedMove) ||
                 ReferenceEquals(command, pendingDoorSettlement?.RecoveryMove) || ReferenceEquals(command, carriedApproach?.DelegatedMove) ||
                IsMountOwnSwiftAbility(command));
        }

        // Celestial Smite, an instant Swift ability, on the paired turn. The mount has no separate turn while mounted, so without this the command
        // never starts (Sprint 9 a02). Native still checks availability and charges the mount's own Swift action. Other mount abilities are not admitted:
        // a command with a real duration would need ticks a preparing turn does not give it.
        internal bool IsMountOwnSwiftAbility(UnitCommand command) =>
            command is UnitUseAbility ability && command.Type == UnitCommand.CommandType.Swift && ability.Spell?.Caster?.Unit == relationship.Mount &&
            relationship.State == RelationshipState.Mounted && ability.Spell.Blueprint.GetComponent<DivineMountSmiteLogic>() != null;

        internal int MountedSmiteRefusalCount { get; private set; }

        // Nothing is spent and this request's own command is all that ends: the pair's accepted work carries on.
        internal void ReportMountedSmiteRefusal(UnitCommand residentCommand)
        {
            MountedSmiteRefusalCount++;
            if (residentCommand != null && !residentCommand.IsFinished) residentCommand.Interrupt();
            LastFeedback = DivineMountSmiteLogic.NeedsApproachMessage;
            ShowStockFeedback();
        }

        internal bool PairedActivationEnabled => settings.EnablePairedActivation;
        internal bool TryConfigurePairedActivation(bool enabled) => unifiedTurn.TryConfigurePairedActivation(enabled);
        internal string PairedActivationIdentity => unifiedTurn.ActivationIdentity;
        internal bool PairedActivationSplit => unifiedTurn.ActivationSplit;
        internal bool PairedActivationFinalized => unifiedTurn.ActivationFinalized;
        internal long PairedActivationSequence => unifiedTurn.ActivationSequence;
        internal string PairedInitiativeObservation => unifiedTurn.LastInitiativeObservation;
        internal long PersistenceWorldDiscardCount => unifiedTurn.PersistenceWorldDiscardCount;
        internal string LastPersistenceWorldDiscardObservation => unifiedTurn.LastPersistenceWorldDiscardObservation;
        internal long PairedLifetimeRetirementCount => unifiedTurn.PairedLifetimeRetirementCount;
        internal long PairedLifetimeRetirementDeferredCount => unifiedTurn.PairedLifetimeRetirementDeferredCount;
        internal string LastPairedLifetimeRetirement => unifiedTurn.LastPairedLifetimeRetirement;
        internal string LastPairedMovementObservation => unifiedTurn.LastMovementObservation;
        internal bool IsPreparingPairedActor(UnitEntityData actor) => unifiedTurn.IsPreparingPairedActor(actor);
        internal bool PairedActorEnded(UnitEntityData actor) => unifiedTurn.PairedActorEnded(actor);
        internal bool MayStartNativePreparationDuringSave(UnitCommand command) => unifiedTurn.MayStartNativePreparationDuringSave(command);
        internal MidEncounterAdoption ResolveMidEncounterAdoption(
            UnitEntityData rider, UnitEntityData mount, out string refusal) =>
            unifiedTurn.ResolveMidEncounterAdoption(rider, mount, out refusal);
        internal long MidEncounterAdoptionCount => unifiedTurn.MidEncounterAdoptionCount;
        internal string LastPairedAdoptionObservation => unifiedTurn.LastAdoptionObservation;
        internal long AdoptionPlanCount => unifiedTurn.AdoptionPlanCount;
        internal long AdoptionRevalidationFailureCount => unifiedTurn.AdoptionRevalidationFailureCount;
        internal long AdoptionRollbackCount => unifiedTurn.AdoptionRollbackCount;
        internal long AdoptionFaultConsumedCount => unifiedTurn.AdoptionFaultConsumedCount;
        internal string LastAdoptionPlanObservation => unifiedTurn.LastAdoptionPlanObservation;
        internal IDisposable ArmMidEncounterAdoptionFault(UnitEntityData rider, UnitEntityData mount) =>
            unifiedTurn.ArmMidEncounterAdoptionFault(rider, mount);
        internal TurnController PairedPartnerContext => unifiedTurn.PartnerContext;
        internal bool PairedPartnerCanGetUp => unifiedTurn.PartnerCanGetUp;

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
            else if (pendingDoorSettlement?.RecoveryMove != null)
            {
                move = pendingDoorSettlement.RecoveryMove;
                source = "door-recovery";
            }
            else if (carriedApproach?.DelegatedMove != null && !carriedApproach.DelegatedMove.IsFinished)
            {
                move = carriedApproach.DelegatedMove;
                source = "carried-approach";
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
            // The carried command ends through its own native API (Stop, a replacement order, a lost target, the
            // approach-exhausted interrupt): the mount's delegated move is released with it, nothing else is touched.
            if (carriedApproach != null && ReferenceEquals(command, carriedApproach.Command))
            {
                ReleaseCarriedApproach("carried command interrupted");
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
                activeCommand == null && activeDoorInteraction == null && activeRiderTurnGroundMove == null && !HasLiveCarriedApproach;
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
            if (HasActiveCommand || HasFaultedChargeCleanupOwner || HasActiveGroundMovement || riderTurnGroundMoveAdmissionPending)
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
            // Either pair actor's native click is recorded as player provenance; admission below still
            // requires the exact principal, selection and turn, so a hidden-mount click only earns
            // its bounded feedback.
            if (disposed || relationship.State != RelationshipState.Mounted ||
                unit == null || target == null ||
                unit != relationship.Rider && unit != relationship.Mount)
            {
                return;
            }

            observedStockRequest.Observe(unit, target, Time.frameCount);
            StockAttackNativeRequestCount++;
            LastStockAttackObservation = "native-request;frame=" + observedStockRequest.Frame +
                ";unit=" + unit.UniqueId + ";target=" + target.UniqueId;
        }

        // First step of every native UnitCommands.Run on either pair actor, before any refusal: binds a
        // same-frame click to the exact command its own synchronous Run carries.
        internal void BindObservedStockRequest(UnitCommands commands, UnitCommand command)
        {
            var owner = commands == null ? null : commands == relationship.Rider?.Commands ? relationship.Rider :
                commands == relationship.Mount?.Commands ? relationship.Mount : null;
            if (owner != null)
            {
                observedStockRequest.Bind(owner, command, Time.frameCount);
            }
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
                settings.UsePairedTurnControls,
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
                        settings.UsePairedTurnControls));
            }

            NativeSingleAttackWeaponSelection ignored;
            var context = CaptureContext(action, null, out ignored, true, false, true);
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
                CaptureContext(action, target, out ignored, false, false, true)).IsAllowed;
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
                CaptureContext(action, target, out ignored, false, false, true)).Feedback;
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
            var automaticOrigin = inputSurface == AutomaticInputSurface && stockIntent.Automatic;
            var ordinaryIntent = (inputSurface == "stock-hostile-click" || automaticOrigin) && target == stockIntentTarget;
            var context = CaptureContext(action, target, out mountPrimary, false, ordinaryIntent, abilityCaster != null);
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
                    allowApproach,
                    !ordinaryIntent);
                command.NativePartnerMovement = settings.EnablePairedActivation;
                // Never presented as a click: an automatic pair command is not a player order, marks no manual
                // target and leaves a native Hold in place at every native Run (NativeRunKeepsHold).
                if (automaticOrigin)
                {
                    command.MarkAutomaticOrigin();
                    automaticRuns.Bind(command, relationship.MountedPairGeneration);
                }
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
                CancelPendingDoorSettlement("pair attack admitted");
                activeCommand = command;
                unifiedTurn.RememberPairedMovementInput();
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
                if (!automaticOrigin) actionActor.CombatState.ManualTarget = target;
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
            // The ground click is synchronous; one that threw never reached its postfix.
            groundOrderOpen = false;
            automaticGate.Tick(relationship.MountedPairGeneration, relationship.State == RelationshipState.Mounted,
                IsPairInCombat(), CombatController.IsInTurnBasedCombat(), relationship.Rider?.IsAIEnabled == true);
            ObservePlayerMoveOutcome();
            SweepOrphanedAutomaticCommand();
            ObserveAutomaticApproachFailure();
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
            // A carried command that ended natively without an interrupt (acted and finished) releases its driver here.
            if (carriedApproach != null && !carriedApproach.IsLive)
            {
                ReleaseCarriedApproach("carried command ended");
            }
            var settlement = pendingDoorSettlement;
            if (settlement != null)
            {
                settlement.Tick();
                if (settlement.IsFinished)
                {
                    var settled = settlement.Outcome;
                    logger.Info("Mounted door occupancy settled: mountId=" + settled.MountId + "; settled=" + settled.OccupancySettled +
                        " (" + settled.OccupancySettleGameSeconds.ToString("F2") + " s); footprintClear=" + settled.FootprintClear +
                        "; recovery=" + settled.RecoveryDisposition + "/" + settled.RecoveryTerminal +
                        "; footprintClearAfter=" + settled.FootprintClearAfterRecovery + "; slotRestored=" + settled.MountMoveSlotRestored);
                    if (ReferenceEquals(pendingDoorSettlement, settlement)) pendingDoorSettlement = null;
                }
            }
            DriveRiderTurnGroundMovement();
            SweepRiderTurnGroundMovement();
            if (activeCommand != null && activeCommand.IsFinished)
            {
                if (!activeCommand.ChargeMode) activeCommand = null;
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
            if (chargeOwner != null && !TryDrainChargeOwnership(reason))
                throw new InvalidOperationException("Mounted charge cleanup remains owned: " + ChargeCleanupStatus);
            ClearStockAttackIntent(reason, true);
            ClearObservedStockRequest();
            ArmedAction = MountedCombatActionKind.None;
            ClearProjectedNativeMountTurnGroundMove();
            overlayWorldInputGuard.Clear();
            var command = activeCommand;
            if (command == null || !command.ChargeMode) activeCommand = null;
            if (command != null && !command.ChargeMode && !command.IsFinished)
            {
                pairedCommandScheduler.ObserveExternalInterrupt(command, reason);
                command.Interrupt();
            }
            ReleaseCarriedApproach(reason);
            var doorInteraction = activeDoorInteraction;
            activeDoorInteraction = null;
            if (doorInteraction != null && !doorInteraction.IsFinished)
            {
                doorInteraction.Interrupt();
            }
            var settlement = pendingDoorSettlement;
            pendingDoorSettlement = null;
            settlement?.Cancel(reason);
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
            var pairedActorTurn = settings.EnablePairedActivation && unifiedTurn.CanAddressActor(principal, turn);
            if (ownerIsMount && (!turnBased || turn?.Unit != mount && !pairedActorTurn))
            {
                // The native party click visits each owner separately. Consume only this pair's
                // redundant mount request; never touch another party member's command or selection.
                var hiddenMountClick = observedStockRequest.Consume(command);
                if (!turnBased && selection != null && selection.Contains(rider))
                {
                    return false;
                }
                LastFeedback = "Select the rider to attack, or use the mount on its own turn.";
                LastRejectionCodes = new[] { MountedCombatRejectionCode.WrongActorOrSelection };
                if (!hiddenMountClick)
                {
                    // A native party-brain proposal (retained ManualTarget), not player input: refuse silently.
                    LastStockAttackObservation = "rejected-hidden-mount-proposal";
                    return false;
                }
                LastStockAttackObservation = "rejected-hidden-mount-surface";
                ShowStockFeedback();
                return false;
            }

            var exactRiderSelected = UnifiedMountedStockAttackPolicy.ContainsExactPrincipal(selection, principal);
            var target = stockAttack.Target;
            var playerCommand = observedStockRequest.IsPlayerCommand(command);
            var exactObservedRequest = playerCommand && UnifiedMountedStockAttackPolicy.IsExactObservedPlayerRequest(
                true,
                ownerIsRider || ownerIsMount && turnBased && (turn?.Unit == mount || pairedActorTurn),
                exactRiderSelected,
                exactStockAttack,
                observedStockRequest.Unit == principal,
                observedStockRequest.Target == target,
                Time.frameCount,
                observedStockRequest.Frame);
            // Only the click's own command consumes the observation; any other command leaves it intact.
            observedStockRequest.Consume(command);
            if (!exactObservedRequest && !playerCommand && stockAttack.AiAction != null)
            {
                // A native brain proposal, never a click: it goes through its own admission. Native Run is refused
                // either way, because the pair controller is the one dispatcher for both actors.
                ObserveAutomaticProposal(stockAttack, ownerIsRider, turnBased);
                return false;
            }
            if (!exactObservedRequest)
            {
                LastFeedback = "Mounted stock attack rejected: no exact current native hostile-click request was observed.";
                LastRejectionCodes = new[] { MountedCombatRejectionCode.CommandAdmissionFailure };
                if (!playerCommand)
                {
                    // AI/auto-use commands carry no native pointer provenance: refuse silently.
                    LastStockAttackObservation = "rejected-unobserved-command";
                    return false;
                }
                // The player's own click that fails exact admission (e.g. the rider is not the selected
                // principal) keeps its bounded, once-per-click feedback.
                LastFeedback = "Mounted stock attack rejected: select the rider of this pair to attack.";
                LastStockAttackObservation = "rejected-inexact-player-click";
                ShowStockFeedback();
                return false;
            }

            if (!UnifiedMountedStockAttackPolicy.AllowsOrdinaryInput(
                turnBased, settings.UsePairedTurnControls, turn?.Unit == principal || pairedActorTurn))
            {
                LastFeedback = "Mounted attacks use separate turns. Select the actor whose turn is active.";
                LastRejectionCodes = new[] { MountedCombatRejectionCode.WrongTurn };
                LastStockAttackObservation = "rejected-separate-turn-fallback";
                ShowStockFeedback();
                return false;
            }

            // The native effect of a new order on the unit it was given to, which a routed click never reaches.
            ObserveDeliberateOrder();
            principal.HoldState = false;
            var actionContext = ownerIsMount ? MountedCombatActionKind.MountPrimaryNatural : ResolveRiderPrimaryAction();
            var weaponContext = principal.GetFirstWeapon();
            var mountWeaponContext = mount.GetFirstWeapon();
            if (target != null && target.IsInState && target.Descriptor?.State != null &&
                target.Descriptor.State.IsConscious && !target.Descriptor.State.IsFinallyDead && principal.IsEnemy(target) &&
                stockIntent.CanContinue(target, turn, ownerIsMount, rider, mount, weaponContext, mountWeaponContext, (int)actionContext))
            {
                // Consume this exact pair request before UnitCommands.Run can replace its windup/approach.
                // The stock pointer loop still dispatches every unrelated selected party member.
                stockIntent.PromoteToPlayer();
                stockIntentCommand?.PromoteToPlayerOrigin();
                automaticRuns.Clear();
                principal.CombatState.ManualTarget = target;
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

        // AI-01: the first mounted/unmounted divergence was this boundary. An unmounted unit's brain proposal runs
        // natively; here it was refused as an unobserved command, so a rider idled after its target died.
        private void ObserveAutomaticProposal(UnitAttack proposal, bool ownerIsRider, bool turnBased)
        {
            AutomaticProposalCount++;
            // A move that arrived this frame holds the pair before this proposal is read, whichever controller ran first.
            ObservePlayerMoveOutcome();
            var rider = relationship.Rider;
            var mount = relationship.Mount;
            var target = proposal.Target;
            var targetState = target?.Descriptor?.State;
            var movementForbidden = AutomaticMovementForbidden(rider);
            string refusal;
            var decision = MountedAutomaticAttackPolicy.Decide(new MountedAutomaticProposalFacts
            {
                AiOrigin = proposal.AiAction != null && !proposal.CreatedByPlayer,
                ExactMountedPair = relationship.State == RelationshipState.Mounted && rider != null && mount != null,
                TurnBased = turnBased,
                OwnerIsRider = ownerIsRider,
                RiderDirectlyControllable = rider != null && rider.IsDirectlyControllable,
                RiderAiEnabled = rider != null && rider.IsAIEnabled,
                RiderBrainActive = rider != null && rider.IsBrainActive,
                StopSuppressed = automaticGate.IsSuppressed(relationship.MountedPairGeneration),
                MovementForbidden = movementForbidden,
                TargetInRiderReach = !movementForbidden || IsTargetInRiderReach(target),
                TargetValid = UnifiedMountedStockAttackPolicy.IsValidTarget(
                    target != null,
                    target != null && target.IsInState,
                    targetState != null && targetState.IsConscious && !targetState.IsFinallyDead,
                    rider != null && rider.IsEnemy(target),
                    rider != null && rider.CanAttack(target)),
                ArmedAction = ArmedAction != MountedCombatActionKind.None,
                PlayerIntentLive = HasStockAttackIntent && !stockIntent.Automatic,
                AutomaticIntentLive = HasStockAttackIntent && stockIntent.Automatic,
                AutomaticIntentOnThisTarget = stockIntent.Automatic && ReferenceEquals(stockIntentTarget, target),
                PairCommandActive = HasActiveCommand || HasFaultedChargeCleanupOwner,
                MountOperationLive = HasMountOperationBesidesAttack(mount),
                NewerPlayerOperation = HasRiderPlayerOperation(rider, mount)
            }, out refusal);
            if (decision == MountedAutomaticProposalDecision.Refuse)
            {
                AutomaticRefusedCount++;
                LastAutomaticRefusal = refusal + ";target=" + (target?.UniqueId ?? "<none>");
                return;
            }
            if (decision == MountedAutomaticProposalDecision.ContinueOwnIntent)
            {
                AutomaticContinuedCount++;
                return;
            }
            AutomaticAdmittedCount++;
            stockIntent.Begin(target, null, false, IsPairInCombat(), rider, mount, rider.GetFirstWeapon(), mount.GetFirstWeapon(),
                (int)ResolveRiderPrimaryAction(), true);
            LastStockAttackObservation = "automatic-intent-start;target=" + target.UniqueId +
                ";ranged=" + (ResolveRiderPrimaryAction() == MountedCombatActionKind.RiderRanged) +
                ";brainAction=" + proposal.AiAction.AssetGuid + ";movementForbidden=" + movementForbidden;
            logger.Info("Mounted automatic attack intent admitted: riderId=" + rider.UniqueId + "; targetId=" + target.UniqueId +
                "; brainAction=" + proposal.AiAction.AssetGuid + "; movementForbidden=" + movementForbidden + ".");
            TryDriveStockAttackIntent();
        }

        // The mount, not the rider, approached and failed, so the native UnitMovementAgent marked the target on the wrong
        // actor. Recording it on the rider gives the rider's brain the native reach-only rule for this target
        // (BlueprintAiAction.IsTargetValid), cleared by native combat state, instead of a proposal loop. A terminal runs
        // inside a replacing native Run, so only this later update can tell a lost approach from a newer order.
        private void ObserveAutomaticApproachFailure()
        {
            var failed = automaticApproachFailedTarget;
            if (failed == null) return;
            automaticApproachFailedTarget = null;
            var rider = relationship.Rider;
            if (relationship.State == RelationshipState.Mounted && !HasRiderPlayerOperation(rider, relationship.Mount))
                rider?.CombatState?.AIData.UnreachableUnits.Add(failed);
        }

        private bool AutomaticMovementForbidden(UnitEntityData rider)
        {
            return rider != null && (rider.AiMovementForbidden || automaticGate.PlayerMoveHold);
        }

        private bool IsTargetInRiderReach(UnitEntityData target)
        {
            var rider = relationship.Rider;
            var weapon = rider?.GetFirstWeapon();
            if (rider?.View == null || target?.View == null || weapon?.Blueprint == null) return false;
            return rider.DistanceTo(target) <= rider.View.Corpulence + target.View.Corpulence + weapon.AttackRange.Meters +
                MountedCombatSpatialPolicy.RangeTolerance && rider.HasLOS(target);
        }

        private bool HasMountOperationBesidesAttack(UnitEntityData mount)
        {
            return HasActiveDoorInteraction || HasPendingDoorSettlement || HasLiveCarriedApproach ||
                HasActiveGroundMovement || riderTurnGroundMoveAdmissionPending ||
                MountedCarriedApproach.MountMoveSlotHost.HasOtherLive(mount?.Commands, stockIntentCommand?.DelegatedMove);
        }

        private bool HasRiderPlayerOperation(UnitEntityData rider, UnitEntityData mount)
        {
            var slots = new List<MountedAutomaticSlotFacts>();
            var raw = rider?.Commands?.Raw;
            if (raw != null)
            {
                foreach (var command in raw)
                {
                    if (command == null) continue;
                    slots.Add(new MountedAutomaticSlotFacts
                    {
                        Finished = command.IsFinished,
                        AiOrigin = command.AiAction != null,
                        PairOwnedOrReaction = command is MountedPairSingleAttack || command is UnitAttackOfOpportunity,
                        CoexistsWithAttack = (command.Type == UnitCommand.CommandType.Free || command.Type == UnitCommand.CommandType.Swift) &&
                            (command.IsStarted || command.IsUnitEnoughClose)
                    });
                }
            }
            return MountedAutomaticAttackPolicy.HasPlayerOperation(slots, CountForeignQueued(rider) + CountForeignQueued(mount));
        }

        // Native Run can queue the pair's own command behind a running one: that is the intent itself, not a newer order.
        private static int CountForeignQueued(UnitEntityData unit)
        {
            var queue = unit?.Commands?.Queue;
            return queue == null ? 0 : queue.Count(command => !(command is MountedPairSingleAttack));
        }

        // A click-owned intent yields only to a newer player operation; the automatic one also honors its own boundaries.
        // Only the intent's own pair command ends here, so the operation that retired it is never touched.
        private bool TryRetireStockIntent(UnitEntityData rider, UnitEntityData mount, bool turnBased)
        {
            if (!stockIntent.Automatic && turnBased) return false;
            var movementForbidden = stockIntent.Automatic && AutomaticMovementForbidden(rider);
            // A delegated approach already under way is a chase even with the target in the rider's own reach.
            var approaching = stockIntentCommand?.DelegatedMove != null && !stockIntentCommand.DelegatedMove.IsFinished;
            var reason = MountedAutomaticAttackPolicy.RetireReason(
                !turnBased,
                stockIntent.Automatic,
                rider != null && rider.IsDirectlyControllable,
                rider != null && rider.IsAIEnabled,
                rider != null && rider.IsBrainActive,
                automaticGate.IsSuppressed(relationship.MountedPairGeneration),
                movementForbidden,
                !movementForbidden || !approaching && IsTargetInRiderReach(stockIntentTarget),
                HasRiderPlayerOperation(rider, mount));
            if (reason == null) return false;
            var automatic = stockIntent.Automatic;
            var command = stockIntentCommand;
            ClearStockAttackIntent(reason, true);
            if (automatic)
            {
                AutomaticRetiredCount++;
                LastAutomaticRetirement = reason;
            }
            if (command != null && ReferenceEquals(activeCommand, command) && !command.IsFinished)
            {
                activeCommand = null;
                pairedCommandScheduler.ObserveExternalInterrupt(command, reason);
                command.Interrupt();
                SweepFinishedCommand();
            }
            return true;
        }

        private void TryDriveStockAttackIntent()
        {
            if (chargeOwner != null) return; // A charge from admission to its end owns the pair's attack.
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
                if (currentTurn.Unit != expectedActor && !unifiedTurn.CanAddressActor(expectedActor, currentTurn) ||
                    currentTurn.Status != TurnController.TurnStatus.Preparing && !currentTurn.IsActing)
                {
                    return;
                }
            }

            var rider = relationship.Rider;
            var mount = relationship.Mount;
            if (relationship.State == RelationshipState.Mounted && TryRetireStockIntent(rider, mount, turnBased))
            {
                return;
            }
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
            // Under a native Hold or a completed player move neither actor approaches, so the mount joins only from
            // where it stands: exactly the ranged rule.
            var automaticOrigin = stockIntent.Automatic;
            var stationary = automaticOrigin && AutomaticMovementForbidden(rider);
            var decision = UnifiedMountedStockAttackPolicy.DecideNext(
                relationship.State == RelationshipState.Mounted && rider != null && mount != null,
                targetValid,
                HasActiveCommand,
                turnBased,
                rider != null && rider.HasStandardAction(),
                mount != null && mount.HasStandardAction(),
                ranged || stationary,
                IsMountAlreadyInPrimaryRange(target),
                !turnBased || !stockIntent.MountActor,
                !turnBased || stockIntent.MountActor || settings.UseLegacyUnifiedTurn,
                settings.EnablePairedActivation && stockIntent.PreferMount);

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
                automaticOrigin ? AutomaticInputSurface : "stock-hostile-click",
                rider,
                !stationary && (!ranged || action != MountedCombatActionKind.MountPrimaryNatural));
            if (result != MountedCombatClickResult.HandledAccepted || activeCommand == null)
            {
                if (automaticOrigin)
                {
                    // No player click to answer: the brain's next proposal re-decides, without a player-facing warning.
                    ClearStockAttackIntent("automatic dispatch rejected", true);
                    LastStockAttackObservation = "automatic-intent-cancel-dispatch-rejected;action=" + action;
                    return;
                }
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
            if (automaticOrigin) AutomaticDispatchCount++;
            stockIntent.ObserveDispatch(action == MountedCombatActionKind.MountPrimaryNatural);
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
            automaticRuns.Clear();
            if (target != null)
            {
                LastStockAttackObservation = "intent-cleared;reason=" +
                    (string.IsNullOrWhiteSpace(reason) ? "boundary" : reason) +
                    ";target=" + target.UniqueId;
            }
        }

        private void ClearObservedStockRequest()
        {
            observedStockRequest.Clear();
        }

        private void ShowStockFeedback()
        {
            var message = LastFeedback;
            EventBus.RaiseEvent<IWarningNotificationUIHandler>(handler => handler.HandleWarning(message, true));
        }

        internal void RejectPairedControl(string reason)
        {
            LastFeedback = reason;
            ShowStockFeedback();
        }

        internal void CancelSelectedInput(string reason)
        {
            if (IsPairSelected())
            {
                Cancel(reason);
            }
        }

        private bool IsPairSelected()
        {
            var selected = SelectionManager.Instance?.SelectedUnits;
            return UnifiedMountedStockAttackPolicy.ContainsExactPrincipal(selected, relationship.Rider) ||
                UnifiedMountedStockAttackPolicy.ContainsExactPrincipal(selected, relationship.Mount);
        }

        // Native Stop, from its own patch: the selected pair's automatic path stays quiet until a deliberate order.
        internal void ObserveStopInput()
        {
            if (!IsPairSelected()) return;
            ObservePlayerMoveOutcome();
            automaticGate.ObserveStop(relationship.MountedPairGeneration);
        }

        internal void ObserveSelectedPlayerOrder()
        {
            if (IsPairSelected()) ObserveDeliberateOrder();
        }

        internal void ObservePlayerOrderFor(UnitEntityData unit)
        {
            if (unit != null && (unit == relationship.Rider || unit == relationship.Mount)) ObserveDeliberateOrder();
        }

        // A move that arrived earlier this frame is settled first, so its result can never overwrite the newer order.
        private void ObserveDeliberateOrder()
        {
            ObservePlayerMoveOutcome();
            automaticGate.ObservePlayerOrder();
        }

        // Opened by the native ground click's own prefix and closed by its postfix: only the command that click runs on
        // the pair's mover can become the watched move.
        internal void OpenGroundOrder()
        {
            groundOrderOpen = true;
        }

        internal void ObserveGroundOrderRun(UnitCommands commands, UnitCommand command)
        {
            if (!groundOrderOpen || disposed || relationship.State != RelationshipState.Mounted) return;
            if (!MountedPlayerMoveWatch.IsExplicitGroundMove(groundOrderOpen, command != null && command.GetType() == typeof(UnitMoveTo),
                command != null && command.CreatedByPlayer, command?.AiAction != null,
                commands != null && commands == relationship.Mount?.Commands,
                commands != null && (commands.Contains(command) || commands.Queue.Contains(command)))) return;
            playerMove.Bind(command, relationship.MountedPairGeneration);
            LastPlayerMoveOutcome = MountedPlayerMoveOutcome.Pending;
            // The native effect of a new order on the unit it was given to, which a routed click never reaches.
            relationship.Rider.HoldState = false;
        }

        private void ObservePlayerMoveOutcome()
        {
            var move = playerMove.Move as UnitMoveTo;
            if (move == null) return;
            var commands = relationship.Mount?.Commands;
            var outcome = playerMove.Observe(relationship.MountedPairGeneration, relationship.State == RelationshipState.Mounted,
                move.IsFinished, move.IsActed && move.Result == UnitCommand.ResultType.Success,
                move.Executor == relationship.Mount && relationship.Mount.HoldState,
                commands != null && (commands.Contains(move) || commands.Queue.Contains(move)));
            if (outcome == MountedPlayerMoveOutcome.Pending) return;
            LastPlayerMoveOutcome = outcome;
            if (outcome == MountedPlayerMoveOutcome.CompletedHeld) automaticGate.ObservePlayerMoveCompleted();
        }

        // The one native write of Hold in UnitCommands.Run, for either pair actor.
        internal bool NativeRunKeepsHold(UnitEntityData owner, UnitCommand command, bool fromQueue, bool doNotClearQueue)
        {
            if (disposed || owner == null || owner != relationship.Rider && owner != relationship.Mount) return false;
            var keeps = automaticRuns.NativeRunKeepsHold(command, owner.Commands?.Queue.First?.Value, doNotClearQueue,
                relationship.MountedPairGeneration);
            if (keeps && owner.HoldState) AutomaticHoldKeptCount++;
            if (keeps && owner.HoldState && fromQueue) AutomaticQueuedHoldKeptCount++;
            return keeps;
        }

        // The Run that queue advancement makes. A refused command is left finished, never in a slot.
        internal bool AdmitQueuedAutomaticRun(MountedPairSingleAttack command)
        {
            var decision = automaticRuns.DecideQueuedRun(command, command.IsAutomaticOrigin, command.IsFinished,
                relationship.State == RelationshipState.Mounted && !disposed, relationship.MountedPairGeneration);
            if (decision == MountedQueuedAutomaticRun.NotAutomatic) return true;
            if (decision == MountedQueuedAutomaticRun.RefuseFinished)
            {
                AutomaticQueuedRefusedCount++;
                return false;
            }
            if (decision == MountedQueuedAutomaticRun.RefuseStale)
            {
                AutomaticQueuedRefusedCount++;
                command.Interrupt();
                return false;
            }
            var rider = relationship.Rider;
            if (TryRetireStockIntent(rider, relationship.Mount, CombatController.IsInTurnBasedCombat()))
            {
                AutomaticQueuedRefusedCount++;
                if (!command.IsFinished) command.Interrupt();
                return false;
            }
            if (AutomaticMovementForbidden(rider)) (command as MountedPairAttackCommand)?.ForbidApproach();
            AutomaticQueuedRunCount++;
            return true;
        }

        private void SweepOrphanedAutomaticCommand()
        {
            var command = activeCommand;
            var commands = command?.ActionActor?.Commands;
            if (commands == null || !MountedAutomaticRunLedger.IsOrphaned(command.IsAutomaticOrigin, command.IsStarted, command.IsFinished,
                commands.Contains(command), commands.Queue.Contains(command))) return;
            activeCommand = null;
            AutomaticQueuedRefusedCount++;
            // The intent ends first: nothing failed to reach this target, so its terminal must not record it unreachable.
            if (ReferenceEquals(stockIntentCommand, command)) ClearStockAttackIntent("automatic command dropped from the native queue", true);
            pairedCommandScheduler.ObserveExternalInterrupt(command, "automatic command dropped from the native queue");
            command.Interrupt();
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
                settings.UseLegacyUnifiedTurn);
        }

        public bool TryOverrideMountTurnMovement(
            UnitMovementAgent agent,
            ref float deltaTime,
            out bool result)
        {
            result = false;
            var attackApproachActive = activeCommand != null && !activeCommand.IsFinished;
            // The door's occupancy recovery move is driven exactly like its approach: on the rider's acting turn.
            // The carried approach's delegated move is admitted exactly like the door approach: on the rider's acting turn.
            var doorApproachActive = activeDoorInteraction != null && !activeDoorInteraction.IsFinished ||
                pendingDoorSettlement?.RecoveryMove != null || HasLiveCarriedApproachMove;
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
                    agent.Unit.EntityData == relationship.Mount,
                    turn != null && turn.Status == TurnController.TurnStatus.Preparing,
                    // The exact owned charge transaction, live, is the only delegator a preparing
                    // rider turn admits - and the command proves that itself. A door approach or a
                    // finished charge still requires an acting turn.
                    attackApproachActive && activeCommand.ChargeTransactionDelegating);
            if (!movementAdmitted)
            {
                return false;
            }

            if (!agent.IsReallyMoving || agent.Unit.IsCommandsPreventMovement ||
                (agent.Unit.AnimationManager != null && agent.Unit.AnimationManager.IsPreventingMovement))
            {
                return !settings.EnablePairedActivation;
            }
            unifiedTurn.TickMountMovement(turn, ref deltaTime);
            result = deltaTime > 0f;
            // The mount's legitimate movement opportunity this frame: admitted on the rider's acting turn with
            // movement budget left. The door approach's stall clock counts stillness only on such frames.
            if (result) mountMovementOpportunityFrame = Time.frameCount;
            // At zero remaining time use the native denial branch. Because the
            // mount is not CurrentTurn, it cannot be charged twice there; native
            // CanMoveInTurnBased completes/interrupts its path with the installed
            // forced/cutscene/uninterruptible-command rules. Merely returning
            // false movement while skipping that branch leaves a live path and
            // an unfinished command after the actor exhausts its allocation.
            return result || !settings.EnablePairedActivation;
        }

        public bool TryAdmitGroundCommand(UnitEntityData requestedUnit)
        {
            var pairedMountInput = unifiedTurn.MaySelectPairedPartner(requestedUnit) &&
                SelectionManager.Instance?.SingleSelectedUnit == requestedUnit;
            // The decision is MountedGroundOrderAdmissionPolicy: an order for any unit that is not
            // this pair's rider (or its selectable paired mount) passes through untouched; the save
            // serialization refusal precedes the pair-local ground-command cancel; paused real-time orders
            // queue natively while turn-based orders keep the rider-turn gates.
            var disposition = MountedGroundOrderAdmissionPolicy.Decide(
                disposed,
                relationship.State == RelationshipState.Mounted,
                requestedUnit == relationship.Rider,
                pairedMountInput,
                new GroundOrderFacts(this));
            if (disposition == MountedGroundOrderDisposition.AdmitRiderTurnMovement)
            {
                groundMoveOwner = relationship.PairOwner;
                if (!relationship.IsCurrentPairOwner(groundMoveOwner)) return false;
                riderTurnGroundMoveAdmissionPending = true;
                unifiedTurn.RememberPairedMovementInput();
            }
            var feedback = MountedGroundOrderAdmissionPolicy.Feedback(disposition, MountDisplayName);
            var rejectionCodes = MountedGroundOrderAdmissionPolicy.RejectionCodes(disposition);
            if (feedback != null) LastFeedback = feedback;
            if (rejectionCodes != null) LastRejectionCodes = rejectionCodes;
            return MountedGroundOrderAdmissionPolicy.LetsNativeOrderRun(disposition);
        }

        // The live native facts for one ground-order decision. The current turn is read once,
        // on the first turn-based fact the decision reaches.
        private sealed class GroundOrderFacts : IMountedGroundOrderFacts
        {
            private readonly MountedCombatController controller;
            private bool turnRead;
            private TurnController turn;

            internal GroundOrderFacts(MountedCombatController controller) { this.controller = controller; }

            private TurnController Turn
            {
                get
                {
                    if (!turnRead)
                    {
                        turnRead = true;
                        turn = Game.Instance?.TurnBasedCombatController?.CurrentTurn;
                    }
                    return turn;
                }
            }

            public bool TryConsumePropagatedOverlayWorldClick() => controller.TrySuppressPropagatedOverlayWorldClick();
            public bool SaveSerializationSuspended => controller.relationship.SaveSerializationSuspended;
            public void CancelPairCommands() => controller.Cancel("ground command");
            public string CurrentModeName => Game.Instance?.CurrentMode.ToString();
            public bool InTurnBasedCombat => CombatController.IsInTurnBasedCombat();
            public bool PairedActivationEnabled => controller.settings.EnablePairedActivation;
            public bool CanMovePairedMount() => controller.unifiedTurn.CanMovePairedMount(Turn);
            public bool CurrentTurnUnitIsRider => Turn?.Unit == controller.relationship.Rider;
            public bool RiderTurnPreparing => Turn != null && Turn.Status == TurnController.TurnStatus.Preparing;
            public bool RiderTurnActing => Turn != null && Turn.IsActing;
            public bool MountMoveSlotIdle => !(controller.relationship.Mount?.Commands == null ||
                controller.relationship.Mount.Commands.GetCommand(UnitCommand.CommandType.Move) != null ||
                controller.relationship.Mount.Commands.Queue.Count != 0);
        }

        public void CompleteGroundCommandAdmission(UnitEntityData routedUnit)
        {
            groundOrderOpen = false;
            if (!riderTurnGroundMoveAdmissionPending)
            {
                return;
            }

            riderTurnGroundMoveAdmissionPending = false;
            if (!relationship.IsCurrentPairOwner(groundMoveOwner)) { groundMoveOwner = null; return; }
            var command = groundMoveOwner.Mount?.Commands?.GetCommand(UnitCommand.CommandType.Move) as UnitMoveTo;
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
            LastFeedback = "Mounted ground movement active: " + MountDisplayName + " owns movement.";
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

            // Sprint 2 (DOOR-01): native replacement semantics. A click on the door already being approached keeps
            // the accepted command; a click on another door replaces it; the mount's ordinary real-time player move
            // yields to the door approach exactly as a native unit's own move yields to its new Standard command.
            // Review correction B: two phases. Phase one evaluates every gate (turn/mode, admission disposition,
            // whether the busy Move slot is only the pair's own recovery) WITHOUT touching the existing operation;
            // only an admissible plan mutates, in phase two, and installs the new command. A forbidden click cancels
            // nothing: no stock intent, no active door, no recovery, no mount move.
            var mountCommands = relationship.Mount?.Commands;
            var turn = Game.Instance?.TurnBasedCombatController?.CurrentTurn;
            var turnAdmissible = MountedInteractionRoutingPolicy.CanAdmitInCurrentTurn(
                CombatController.IsInTurnBasedCombat(),
                turn?.Unit == relationship.Rider,
                turn != null && turn.Status == TurnController.TurnStatus.Preparing,
                turn != null && turn.IsActing);
            var disposition = DecideDoorAdmission(mountCommands, exactDoor);
            // The pair's own recovery move or its carried approach move is the only kind of busy Move slot a door click
            // may replace (native: the rider's new Standard replaces its pending cast; the pair's movement follows it).
            var ownMountMove = pendingDoorSettlement?.RecoveryMove != null ? (UnitCommand)pendingDoorSettlement.RecoveryMove : carriedApproach?.DelegatedMove;
            var busyOnlyWithOwnRecovery = ownMountMove != null && mountCommands != null &&
                ReferenceEquals(mountCommands.GetCommand(UnitCommand.CommandType.Move), ownMountMove) &&
                mountCommands.Standard == null && mountCommands.GetCommand(UnitCommand.CommandType.Free) == null &&
                mountCommands.GetCommand(UnitCommand.CommandType.Swift) == null && mountCommands.Queue.Count == 0;
            var plan = MountedDoorApproachPolicy.PlanAdmission(disposition, busyOnlyWithOwnRecovery, turnAdmissible);
            if (plan == MountedDoorAdmissionPlan.RejectWrongTurn)
            {
                LastFeedback = "Open an approached door during the rider's turn.";
                LastRejectionCodes = new[] { MountedCombatRejectionCode.WrongTurn };
                logger.Info("Rejected mounted door interaction (nothing replaced): " + LastFeedback);
                return false;
            }
            if (!MountedDoorApproachPolicy.PlanMutates(plan))
            {
                LastFeedback = MountedDoorApproachPolicy.Feedback(disposition, MountDisplayName);
                LastRejectionCodes = new[] { MountedCombatRejectionCode.AlreadyActiveCommand };
                if (plan == MountedDoorAdmissionPlan.RetainSameDoor) DoorInteractionRetainedClickCount++;
                logger.Info(LastFeedback);
                return false;
            }

            // Phase two: the admissible plan mutates exactly what it names, then the admission is re-decided. An armed
            // stock-attack intent yields to the door click (native: a new Standard replaces the attack); only the intent
            // is cleared here, never the controller-wide cancel, so the recovery and mount-move branches below name
            // their own replacements.
            if (HasStockAttackIntent)
            {
                var dispatched = stockIntentCommand != null && ReferenceEquals(activeCommand, stockIntentCommand) ? activeCommand : null;
                ClearStockAttackIntent("door interaction replaced stock attack intent", true);
                ClearObservedStockRequest();
                if (dispatched != null && !dispatched.ChargeMode)
                {
                    activeCommand = null;
                    if (!dispatched.IsFinished)
                    {
                        pairedCommandScheduler.ObserveExternalInterrupt(dispatched, "door interaction replaced stock attack");
                        dispatched.Interrupt();
                    }
                }
                disposition = DecideDoorAdmission(mountCommands, exactDoor);
            }
            if (plan == MountedDoorAdmissionPlan.ReplaceRecovery)
            {
                DoorInteractionReplacedRecoveryCount++;
                CancelPendingDoorSettlement("replaced by a new door click");
                ReleaseCarriedApproach("replaced by a door click");
                if (mountCommands != null && mountCommands.Queue.Count == 0) mountCommands.RemoveFinishedAndUpdateQueue();
                disposition = DecideDoorAdmission(mountCommands, exactDoor);
            }
            if (plan == MountedDoorAdmissionPlan.ReplaceActiveDoor || disposition == MountedDoorAdmissionDisposition.ReplaceActiveDoor)
            {
                var replaced = activeDoorInteraction;
                activeDoorInteraction = null;
                replaced?.Interrupt();
                // The earlier approach's finished delegated move may still sit in the Move slot; sweep it before re-deciding.
                if (mountCommands != null && mountCommands.Queue.Count == 0) mountCommands.RemoveFinishedAndUpdateQueue();
                logger.Info("Mounted door interaction replaced an earlier door approach: door=" + exactDoor.name);
                disposition = DecideDoorAdmission(mountCommands, exactDoor);
            }
            if (disposition == MountedDoorAdmissionDisposition.ReplaceMountMovement)
            {
                var mountMove = mountCommands.GetCommand(UnitCommand.CommandType.Move);
                mountMove?.Interrupt();
                if (mountCommands.Queue.Count == 0) mountCommands.RemoveFinishedAndUpdateQueue();
                relationship.Mount.View?.StopMoving();
                DoorInteractionReplacedMountMoveCount++;
                logger.Info("Mounted door interaction replaced the " + MountDisplayName + " player move: door=" + exactDoor.name +
                    "; slotIdle=" + (mountCommands.Empty && mountCommands.Queue.Count == 0));
                disposition = DecideDoorAdmission(mountCommands, exactDoor);
            }
            if (disposition != MountedDoorAdmissionDisposition.Admit)
            {
                // Fail closed after a replacement that did not free the pair: reported, nothing further replaced.
                LastFeedback = MountedDoorApproachPolicy.Feedback(disposition, MountDisplayName);
                LastRejectionCodes = new[] { MountedCombatRejectionCode.AlreadyActiveCommand };
                logger.Warning("Mounted door interaction replacement did not free the pair: plan=" + plan + "; " + LastFeedback);
                return false;
            }

            // Admitted: a settlement still waiting on the previous click (no move yet) is superseded by this one.
            CancelPendingDoorSettlement("superseded by a new door click");
            ObserveDeliberateOrder();
            var routed = new MountedDoorInteractionCommand(
                relationship,
                relationship.Rider,
                relationship.Mount,
                exactDoor,
                logger,
                HandleDoorInteractionTerminal);
            routed.NativePartnerMovement = settings.EnablePairedActivation;
            routed.MovementOpportunity = () => MountMovementOpportunityRecently;
            activeDoorInteraction = routed;
            unifiedTurn.RememberPairedMovementInput();
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
            if (ChargeAdmissionFenced) throw new InvalidOperationException("Controller teardown retained a native boundary or serialization fence.");
            relationship.Dismounting -= HandleDismounting;
            relationship.ChargeCleanupBarrier = null;
            disposed = true;
        }

        private MountedCombatActionContext CaptureContext(
            MountedCombatActionKind action,
            UnitEntityData target,
            out NativeSingleAttackWeaponSelection mountPrimary,
            bool assumeTargetAvailable = false,
            bool ordinaryIntent = false,
            bool nativeActorIntent = false)
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
                settings.UsePairedTurnControls,
                turn?.Unit == rider,
                turn?.Unit == actionActor,
                turn != null && turn.Status == TurnBased.Controllers.TurnController.TurnStatus.Preparing,
                turn != null && turn.IsActing);
            if (settings.EnablePairedActivation && turnBasedCombat)
                actionActorTurn = unifiedTurn.CanAddressActor(actionActor, turn);
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
                ExactRiderSelection = ordinaryIntent || nativeActorIntent || MountedTurnSelectionPolicy.IsExpectedActionSelection(
                    turnBasedCombat,
                    action == MountedCombatActionKind.MountPrimaryNatural,
                    settings.UsePairedTurnControls,
                    exactRiderSelection,
                    exactMountSelection),
                SupportedMountProfile = SupportedMountedProfiles.Resolve(rider, mount) != null &&
                    string.Equals(relationship.Runtime.MountProfileId,
                        SupportedMountedProfiles.Resolve(rider, mount)?.Id,
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
                TransactionIdle = !HasFaultedChargeCleanupOwner && (!HasActiveCommand || HasStockAttackIntent && ReferenceEquals(activeCommand, stockIntentCommand)) &&
                    !HasActiveGroundMovement && !riderTurnGroundMoveAdmissionPending,
                LoadingOrLifecycleBoundary = Game.Instance == null ||
                    !MountedGameModePolicy.CanQueueMountedAction(Game.Instance.CurrentMode.ToString()),
                PathKnownUnavailable = false,
                WithinSupportedRangeEnvelope = true,
                RangeOriginConsistent = true,
                CommandAdmissionReady = action != MountedCombatActionKind.MountPrimaryNatural ||
                    !turnBasedCombat || !settings.UseLegacyUnifiedTurn ||
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
            if (ReferenceEquals(chargeOwner?.Command, command)) chargeOwner.Ownership.Retire();
            pairedCommandScheduler.ObserveTerminal(
                command,
                outcome?.Result,
                outcome?.TerminalReason);
            var stockIntentOwned = ReferenceEquals(stockIntentCommand, command);
            if (stockIntentOwned)
            {
                stockIntentCommand = null;
            }
            if (stockIntentOwned && stockIntent.Automatic && outcome != null && outcome.ChildAttackStartCount == 0 &&
                !outcome.NativeRangedTailTermination &&
                !string.Equals(outcome.Result, UnitCommand.ResultType.Success.ToString(), StringComparison.Ordinal))
            {
                automaticApproachFailedTarget = stockIntentTarget;
            }
            LastOutcome = outcome;
            finishedCommandPendingSweep = command;
            if (activeCommand == command && !command.ChargeMode)
            {
                activeCommand = null;
            }
            LastFeedback = outcome.Result == UnitCommand.ResultType.Success.ToString()
                ? "Mounted pair attack completed."
                : outcome.NativeRangedTailTermination ? "Ranged attacks completed; remaining melee attacks are out of reach."
                : DescribeTerminalFailure(outcome);
            // Native Run writes a successor's manual target right after this interruption, so the charge's own is released now.
            if (command.ChargeMode && ReferenceEquals(chargeOwner?.Command, command))
            {
                try { ReleaseChargeManualTarget(chargeOwner); }
                catch (Exception exception) { logger.Exception("Charge manual target release retained", exception); }
            }
            logger.Info("Mounted combat command terminal: action=" + command.Action +
                "; result=" + outcome.Result +
                "; reason=" + (outcome.TerminalReason ?? "<none>") +
                "; childAttacks=" + outcome.ChildAttackStartCount +
                "; repaths=" + outcome.RepathCount +
                "; repathObservations=" + (string.IsNullOrEmpty(outcome.RepathObservations) ? "<none>" : outcome.RepathObservations) +
                "; rejectionCodes=" + string.Join(",", LastRejectionCodes.Select(code => code.ToString()).ToArray()) +
                "; feedback=" + LastFeedback);
            if (stockIntentOwned && !outcome.NativeRangedTailTermination &&
                !string.Equals(outcome.Result, UnitCommand.ResultType.Success.ToString(), StringComparison.Ordinal))
            {
                ClearStockAttackIntent("stock command terminal " + outcome.Result, true);
                LastStockAttackObservation = "intent-cancel-terminal;action=" + command.Action +
                    ";result=" + outcome.Result;
            }
        }

        internal long DoorInteractionReplacedMountMoveCount { get; private set; }

        internal long DoorInteractionRetainedClickCount { get; private set; }

        // Sprint 3 (TRAP-01, SPELL-01): the last released carried approach and its counters.
        internal MountedCarriedApproachOutcome LastCarriedApproachOutcome { get; private set; }

        internal long CarriedApproachBindCount { get; private set; }

        internal long CarriedApproachReleaseCount { get; private set; }

        internal long CarriedApproachExhaustedCount { get; private set; }

        internal string LastCarriedApproachObservation { get; private set; } = "not-observed";

        // Called from the TickApproaching prefix for every unstarted native command. Returns true when the mount
        // approaches for the rider's own command this tick and the native rider approach must be skipped; false
        // leaves the native code untouched (not a carried command, the command is in range, or another pair
        // operation owns the mount, in which case the command waits natively exactly as before).
        internal bool TryTickCarriedApproach(UnitCommand command, bool relationshipControlShell)
        {
            if (disposed || command == null) return false;
            var ability = command as UnitUseAbility;
            var interaction = command as UnitInteractWithObject;
            var kind = ability != null
                ? MountedCarriedCommandKind.Ability
                : interaction != null && command.GetType() == typeof(UnitInteractWithObject) && interaction.Interaction is DisableTrapInteractionComponent
                    ? MountedCarriedCommandKind.TrapInteraction
                    : MountedCarriedCommandKind.None;
            var chargeShell = ability != null &&
                (string.Equals(ability.Spell?.Blueprint?.AssetGuid, NativeMountedControlService.MountedChargeAbilityGuid, StringComparison.Ordinal) ||
                 OwnsChargeNativeAction(ability));
            var disposition = MountedCarriedApproachPolicy.Decide(
                relationship.State == RelationshipState.Mounted,
                command.Executor != null && command.Executor == relationship.Rider,
                command.IsStarted,
                command.IsFinished,
                kind,
                relationshipControlShell,
                chargeShell,
                command.AiAction != null,
                Kingmaker.Controllers.Clicks.PointerController.SimulatingClick);
            if (disposition != MountedCarriedApproachDisposition.Carry) return false;
            if (carriedApproach != null && !ReferenceEquals(carriedApproach.Command, command))
            {
                ReleaseCarriedApproach("replaced by another rider command");
            }
            if (carriedApproach == null)
            {
                // Another pair operation owns the mount: the native command waits unstarted, as it did before this
                // sprint, until that operation ends; nothing is cancelled on its behalf.
                if (HasActiveCommand || HasActiveGroundMovement || riderTurnGroundMoveAdmissionPending || HasActiveDoorInteraction ||
                    pendingDoorSettlement?.RecoveryMove != null)
                {
                    LastCarriedApproachObservation = "deferred: another pair operation owns the mount";
                    return false;
                }
                // Review correction B: any other live or queued command of the mount owns its movement; it is never
                // cancelled for a carried approach, which binds once the mount is free.
                if (MountedCarriedApproach.MountMoveSlotHost.HasOtherLive(relationship.Mount?.Commands, null))
                {
                    LastCarriedApproachObservation = "deferred: another command owns the mount's movement";
                    return false;
                }
                // A settlement still sampling the last door click yields to the pair's new movement intent.
                CancelPendingDoorSettlement("superseded by a carried approach");
                carriedApproach = new MountedCarriedApproach(relationship, relationship.Rider, relationship.Mount, command, kind, logger, HandleCarriedApproachTerminal)
                {
                    NativePartnerMovement = settings.EnablePairedActivation,
                    MovementOpportunity = () => MountMovementOpportunityRecently
                };
                unifiedTurn.RememberPairedMovementInput();
                CarriedApproachBindCount++;
                ObserveDeliberateOrder();
                LastCarriedApproachOutcome = null;
                LastFeedback = MountedCarriedApproachPolicy.Feedback(kind, MountDisplayName);
                LastCarriedApproachObservation = "bound: " + command.GetType().Name;
                logger.Info("Mounted carried approach bound: kind=" + kind + "; command=" + command.GetType().Name +
                    "; riderId=" + relationship.Rider.UniqueId + "; mountId=" + relationship.Mount.UniqueId +
                    "; target=" + (command.TargetUnit?.UniqueId ?? interaction?.Interaction?.name ?? "<point>") +
                    "; approachRadius=" + command.ApproachRadius.ToString("F2", CultureInfo.InvariantCulture) + ".");
            }
            return carriedApproach.Tick();
        }

        // PR #8 review correction A. The UnitCommands.Run prefix runs the pair's routing gates through this admission
        // in their existing order; nothing is released while they decide. Only a request every gate admitted gets a
        // ticket, which the Run postfix completes from what native Run actually did (CompletePairCommandRun).
        internal bool AdmitPairCommandRun(UnitCommands container, ref UnitCommand command, out MountedCarriedRequestTicket ticket)
        {
            var routed = command;
            var admitted = MountedCarriedRequestFlow.Admit(carriedRequestHost, container, routed, new Func<bool>[]
            {
                () => TryRouteMountedDoorInteraction(container, ref routed),
                () => TryRouteMountedStockAttack(container, routed),
                () => ShouldAllowStockCommand(container, routed)
            }, out ticket);
            command = routed;
            if (disposed) ticket = null;
            return admitted;
        }

        internal void CompletePairCommandRun(MountedCarriedRequestTicket ticket, UnitCommand executed)
        {
            if (disposed || ticket == null) return;
            var action = MountedCarriedRequestFlow.Complete(carriedRequestHost, ticket, executed);
            if (action != MountedCarriedRequestAction.Keep) CarriedApproachAdmittedReleaseCount++;
        }

        internal int CarriedApproachAdmittedReleaseCount { get; private set; }

        private MountedCarriedRequestHost carriedRequestHostValue;
        private MountedCarriedRequestHost carriedRequestHost => carriedRequestHostValue ?? (carriedRequestHostValue = new MountedCarriedRequestHost(this));

        private sealed class MountedCarriedRequestHost : IMountedCarriedRequestHost
        {
            private readonly MountedCombatController owner;
            internal MountedCarriedRequestHost(MountedCombatController owner) { this.owner = owner; }

            // The bound carried approach. One whose command native Run just ended directly (an out-of-range unstarted
            // command it interrupted without the patched slot removal) is still bound here, so the admitted request
            // releases its delegated move at once instead of at the next update sweep.
            public object LiveApproach => owner.carriedApproach;
            public bool IsRiderContainer(object container) => container != null && ReferenceEquals(container, owner.relationship.Rider?.Commands);
            public bool IsMountContainer(object container) => container != null && ReferenceEquals(container, owner.relationship.Mount?.Commands);
            public bool IsCarriedCommand(object approach, object command) => command != null && ReferenceEquals((approach as MountedCarriedApproach)?.Command, command);
            public bool IsDelegatedMove(object approach, object command) => command != null && ReferenceEquals((approach as MountedCarriedApproach)?.DelegatedMove, command);
            public bool IsPlayerRequest(object command) => command is UnitCommand unit && unit.CreatedByPlayer && unit.AiAction == null;

            public bool CarriedCommandDisplaced(object approach)
            {
                var carried = (approach as MountedCarriedApproach)?.Command;
                var commands = owner.relationship.Rider?.Commands;
                if (carried == null || carried.IsFinished || commands == null) return true;
                return !commands.Contains(carried) && !commands.Queue.Contains(carried);
            }

            public bool OwnMoveHeld(object approach) => MountedMovementDisplacementPolicy.OwnMoveHeld(MountSlotFacts(approach));

            // A queued command is not displacement: native Run left the carrier's move in its slot (see the policy).
            // The conservative queue-aware HasOtherLive stays with cleanup and re-binding, where it protects a successor.
            public bool MountMovementDisplaced(object approach, bool ownMoveHeldAtAdmission) =>
                MountedMovementDisplacementPolicy.Displaced(ownMoveHeldAtAdmission, MountSlotFacts(approach));

            private System.Collections.Generic.List<MountedSlotCommandFacts> MountSlotFacts(object approach)
            {
                var carrier = approach as MountedCarriedApproach;
                var commands = owner.relationship.Mount?.Commands;
                var facts = new System.Collections.Generic.List<MountedSlotCommandFacts>();
                if (carrier == null || commands == null) return facts;
                var own = carrier.DelegatedMove;
                var raw = commands.Raw;
                for (var index = 0; index < raw.Length; index++)
                {
                    var command = raw[index];
                    if (command == null) continue;
                    var unstarted = !command.IsStarted && !command.IsFinished;
                    facts.Add(new MountedSlotCommandFacts
                    {
                        IsCarrierMove = own != null && ReferenceEquals(command, own),
                        Finished = command.IsFinished,
                        InMoveSlot = index == (int)UnitCommand.CommandType.Move,
                        Started = command.IsStarted,
                        EnoughClose = !unstarted || command.IsUnitEnoughClose
                    });
                }
                return facts;
            }

            public void Release(object approach, string reason)
            {
                if (ReferenceEquals(owner.carriedApproach, approach)) owner.ReleaseCarriedApproach(reason);
            }

            public void InterruptCarriedCommand(object approach)
            {
                var carried = (approach as MountedCarriedApproach)?.Command;
                if (carried != null && !carried.IsFinished && !carried.IsStarted) carried.Interrupt();
            }
        }

        private void ReleaseCarriedApproach(string reason)
        {
            var releasing = carriedApproach;
            if (releasing == null) return;
            carriedApproach = null;
            CarriedApproachReleaseCount++;
            releasing.Release(reason);
        }

        private void HandleCarriedApproachTerminal(MountedCarriedApproach approach, MountedCarriedApproachOutcome outcome)
        {
            LastCarriedApproachOutcome = outcome;
            if (ReferenceEquals(carriedApproach, approach)) carriedApproach = null;
            if (outcome.TerminalReason != null && !outcome.Satisfied)
            {
                CarriedApproachExhaustedCount++;
                LastFeedback = MountedCarriedApproachPolicy.ExhaustedFeedback(
                    outcome.Kind == MountedCarriedCommandKind.TrapInteraction.ToString() ? MountedCarriedCommandKind.TrapInteraction : MountedCarriedCommandKind.Ability,
                    MountDisplayName, outcome.TerminalReason);
                LastRejectionCodes = new[] { MountedCombatRejectionCode.NoPath };
            }
            LastCarriedApproachObservation = "released: " + outcome.Result + "; satisfied=" + outcome.Satisfied;
            logger.Info("Mounted carried approach terminal: kind=" + outcome.Kind + "; command=" + outcome.CommandType +
                "; result=" + outcome.Result + "; satisfied=" + outcome.Satisfied +
                "; moveStarts=" + outcome.DelegatedMoveStartCount + "; replans=" + outcome.ReplanCount +
                "; withoutProgress=" + outcome.ApproachesWithoutProgress +
                "; nativeApproachEndSignalled=" + outcome.NativeApproachEndSignalled +
                "; trapSafePoint=" + outcome.TrapSafePointApplied +
                "; distance=" + outcome.DistanceAtSatisfaction.ToString("F2", CultureInfo.InvariantCulture) +
                "; los=" + outcome.LineOfSightAtSatisfaction +
                "; operationalSeconds=" + outcome.ApproachElapsedOperationalSeconds.ToString("F2", CultureInfo.InvariantCulture) +
                "; slotRestored=" + outcome.MountMoveSlotRestored +
                "; reason=" + (outcome.TerminalReason ?? "<none>") + ".");
        }

        internal long DoorInteractionReplacedRecoveryCount { get; private set; }

        // The settlement yields to any admitted pair command (door click, pair attack, charge): one owner of the mount.
        private void CancelPendingDoorSettlement(string reason)
        {
            var yielding = pendingDoorSettlement;
            pendingDoorSettlement = null;
            if (yielding != null && !yielding.IsFinished) yielding.Cancel(reason);
        }

        internal long DoorOccupancyRecoveryCount { get; private set; }

        private MountedDoorAdmissionDisposition DecideDoorAdmission(UnitCommands mountCommands, StandardDoor exactDoor)
        {
            if (mountCommands == null)
            {
                return MountedDoorAdmissionDisposition.RejectMountBusy;
            }
            var mountMove = mountCommands.GetCommand(UnitCommand.CommandType.Move);
            var mountMoveIsPlayerMove = mountMove is UnitMoveTo && mountMove.CreatedByPlayer &&
                !(mountMove is MountedDoorApproachMove) && !mountMove.IsFinished;
            var otherSlotOccupied = mountCommands.Standard != null ||
                mountCommands.GetCommand(UnitCommand.CommandType.Free) != null ||
                mountCommands.GetCommand(UnitCommand.CommandType.Swift) != null;
            return MountedDoorApproachPolicy.DecideAdmission(
                // A dispatched stock-attack intent is replaceable by a door click (native: a new Standard replaces the
                // attack); it is interrupted in phase two of TryRouteMountedDoorInteraction, never here.
                HasActiveCommand && !(HasStockAttackIntent && ReferenceEquals(activeCommand, stockIntentCommand)) ||
                HasActiveGroundMovement || riderTurnGroundMoveAdmissionPending,
                HasActiveDoorInteraction,
                HasActiveDoorInteraction && ReferenceEquals(activeDoorInteraction.Door, exactDoor),
                CombatController.IsInTurnBasedCombat(),
                mountMove != null,
                mountMoveIsPlayerMove,
                otherSlotOccupied,
                mountCommands.Queue.Count == 0);
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
            if (outcome.Settlement != null && !outcome.Settlement.IsFinished)
            {
                // The native action ended; the controller now owns the settlement (and any recovery move) until it
                // reports a terminal result on this same outcome, or Stop/retarget/dismount/area change cancels it.
                CancelPendingDoorSettlement("superseded by a later door interaction");
                pendingDoorSettlement = outcome.Settlement;
                DoorOccupancyRecoveryCount++;
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
                "; preventionRefused=" + outcome.PreventionRefused +
                "; settlementPending=" + (outcome.Settlement != null && !outcome.Settlement.IsFinished) +
                "; reason=" + (outcome.TerminalReason ?? "<none>") +
                "; feedback=" + LastFeedback);
        }

        private void SweepFinishedCommand()
        {
            var command = finishedCommandPendingSweep;
            if (command != null && command.ChargeMode)
            {
                if (command.IsFinished) TryDrainChargeOwnership("terminal sweep");
                return;
            }
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
            if (!relationship.IsCurrentPairOwner(groundMoveOwner) || !exactTurn || rawMove != command && !command.IsFinished)
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
            groundMoveOwner = null;
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
            if (settings.EnablePairedActivation) return;
            var command = activeRiderTurnGroundMove;
            if (command == null || command.IsFinished || !relationship.IsCurrentPairOwner(groundMoveOwner) || !CombatController.IsInTurnBasedCombat())
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
            groundMoveOwner = null;
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
