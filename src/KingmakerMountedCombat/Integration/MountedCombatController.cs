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
        // Reviews R3-F3/R1-F5/R2-F5/R2-F1: the retained ordinary order (kind at acceptance, exact own commands, accepted work).
        private readonly MountedRetainedOrderLedger retainedOrder = new MountedRetainedOrderLedger();
        // Review R1-F8: whether the mount's Hold is the player's.
        private readonly MountedHoldProvenance mountHold = new MountedHoldProvenance();
        // Review R1-F11: the first automatic order of a real-time combat, attributed by generation.
        private readonly MountedEngagementRecord engagement = new MountedEngagementRecord();
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
            riderTurnGroundMoveAdmissionPending || HasStockAttackIntent || HasLiveCarriedApproach || HasOutstandingCleanup;

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
        internal string LastStandardRefusalTrace { get; private set; } = "not-observed";

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
        internal void ReportMountedSmiteRefusal(UnitCommand residentCommand, bool outOfReach)
        {
            if (residentCommand != null && !residentCommand.IsFinished) residentCommand.Interrupt();
            if (!outOfReach) return; // native's own reason, such as a target that fell or a spent use: nothing here to explain or move toward
            MountedSmiteRefusalCount++;
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
                command.GetType() == typeof(UnitMoveTo) && IsPairPlayerOrder(command) &&
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
            bool allowApproach = true,
            bool preStartApproach = false,
            bool nativeStartGatesReadiness = false,
            FirstTickProposal provisional = null)
        {
            NativeSingleAttackWeaponSelection mountPrimary;
            var automaticOrigin = provisional == null && inputSurface == AutomaticInputSurface && stockIntent.Automatic;
            // PR #70 R1-F6 (round 7): a provisional order is evaluated as the fresh explicit click it is (its own target-life fact), and is
            // wired to no intent or generation until its first tick commits it.
            var ordinaryIntent = provisional == null && (inputSurface == "stock-hostile-click" || automaticOrigin) && target == stockIntentTarget;
            var context = CaptureContext(action, target, out mountPrimary, false, ordinaryIntent || provisional != null, abilityCaster != null,
                ordinaryIntent && nativeStartGatesReadiness, provisional != null);
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
                    // PR #70 R1-F6 (round 7): a provisional order is the ordinary order it may become (the plan its click proved).
                    !ordinaryIntent && provisional == null);
                command.NativePartnerMovement = settings.EnablePairedActivation;
                if (ordinaryIntent)
                {
                    // Review R3-F3: the order's own wrapper and every exact move it issues (recorded before native Run sees it).
                    var orderGeneration = stockIntent.Generation;
                    retainedOrder.RecordOwnIssued(orderGeneration, command);
                    command.OwnCommandIssuing = issued => retainedOrder.RecordOwnIssued(orderGeneration, issued);
                    // PR-1 review R2-F1: supersession stops the installed wrapper before its first delivery.
                    command.RetainedOrderSupersededCheck = () => retainedOrder.Superseded(orderGeneration);
                    // Review R4-F1, PR #70 R1-F7: a turn-based rider melee order admitted by the click probe follows the proof bound to it.
                    AttachClickProof(command, action, target);
                }
                if (provisional != null)
                {
                    // PR #70 R1-F6 (round 7): the provisional order follows its click's proof (route, plan, revalidator from the current origin)
                    // exactly as an accepted route-bound order does; its manual-target write waits for its commit; its carrier's first tick
                    // decides (FirstTickCarrierIssued), and a strike reached without a decided tick commits only in place (ProvisionalStrikeGate).
                    provisional.Command = command;
                    command.MarkProvisionalOrder();
                    var proof = provisional.Proof;
                    command.ValidatedApproachRoute = proof.Route;
                    command.ClickPlan = proof.Plan;
                    var established = proof.Route.Count >= 2 ? proof.Route[proof.Route.Count - 1] : (Vector3?)null;
                    var plan = proof.Plan;
                    command.RouteRevalidator = dispatchPlan => RevalidateAdmittedClickRoute(target, established, plan, dispatchPlan);
                    command.FirstTickCarrierIssued = carrier => BindFirstTickCarrier(command, carrier);
                    command.FirstTickCarrierPermitted = () => FirstTickCarrierPermitted(command);
                    command.CarrierReleasedForStrike = () => CarrierReleasedForStrike(command);
                    command.ProvisionalStrikeGate = () => AdmitProvisionalStrike(command);
                    if (provisional.TargetUnconsciousAtClick) command.AdmitTargetUnconsciousWhenOrdered();
                }
                if (automaticOrigin)
                {
                    // Review R4-F2, PR-1 round 11: the automatic order's one current admission decision, asked by its own command at every
                    // approach and pre-delivery boundary, before and after native start.
                    command.AutomaticOrderAdmissionLost = RetireAutomaticOrderIfNotAdmitted;
                }
                if (ordinaryIntent && preStartApproach)
                {
                    // MMR-05 (review R3-F1): while native has not started the order, its own approach moves the mount.
                    command.PreStartMovementForbidden = () => relationship.Mount == null || !relationship.Mount.Descriptor.State.CanMove ||
                        stockIntent.Automatic && AutomaticMovementForbidden(relationship.Rider);
                    command.AllowPreStartApproach();
                }
                // The player's order on an already unconscious enemy keeps that target as native UnitAttack does.
                if (ordinaryIntent && !automaticOrigin && !stockIntent.Automatic && stockIntent.TargetUnconsciousWhenOrdered)
                    command.AdmitTargetUnconsciousWhenOrdered();
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
                if (!automaticOrigin && provisional == null) OwnManualTarget(actionActor, target, ordinaryIntent ? stockIntent.Generation : -1);
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
            mountHold.Observe(relationship.Mount != null && relationship.Mount.HoldState);
            retainedOrder.ReleaseFinished(command => command is UnitCommand unit && unit.IsFinished && !ReferenceEquals(command, stockIntentCommand));
            ReleaseEntranceCarrier();
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

            ObserveEngagementProgress();
            TryDriveStockAttackIntent();
            // R12-F4 (round 13): outstanding owned cleanup keeps being retried after a dismount or a refused teardown.
            if (HasOutstandingCleanup) TrySettleOutstandingCleanup();
        }

        private Vector3? engagementLastMountPosition;
        private object engagementLastCarrier;
        private bool engagementLastPlayerOperation;
        internal string LastEngagementFirstStop { get; private set; }
        internal MountedEngagementRecord Engagement => engagement;

        // MMR-04 (reviews R1-F11, R2, R3): the first automatic order of a real-time combat (MountedEngagementRecord). The window
        // opens at the first combat update or the first automatic proposal, whichever comes first; each update's mount movement
        // is attributed to that order only while its own pair command or delegated move is live and no player operation is.
        private void ObserveEngagementProgress()
        {
            var game = Game.Instance;
            var mount = relationship.Mount;
            if (game?.TimeController == null || mount == null || relationship.State != RelationshipState.Mounted || !IsPairInCombat() ||
                CombatController.IsInTurnBasedCombat())
            {
                engagement.Reset();
                engagementLastMountPosition = null;
                engagementLastCarrier = null;
                engagementLastPlayerOperation = false;
                return;
            }
            // PR-1 review R1-F13: the one-shot report is complete; nothing more is sampled until the next combat opens a new record.
            if (engagement.Reported) return;
            var now = game.TimeController.GameTime.TotalSeconds;
            engagement.Open(now);
            var rider = relationship.Rider;
            var playerOperation = HasRiderPlayerOperation(rider, mount) || HasStockAttackIntent && !stockIntent.Automatic;
            if (playerOperation) engagement.ObservePlayerOperation();
            var position = mount.Position;
            // Review R1-F11: an interval counts as the automatic order's own travel only when the SAME exact carrier owned the
            // mount's movement at both of its ends; any change of owner starts a fresh baseline.
            var carrier = HasStockAttackIntent && stockIntent.Automatic && stockIntent.Generation == engagement.OrderGeneration
                ? stockIntentCommand?.CurrentMovementOwner : null;
            if (engagementLastMountPosition.HasValue)
            {
                var delta = position - engagementLastMountPosition.Value; delta.y = 0f;
                engagement.ObserveInterval(delta.magnitude, engagementLastCarrier, carrier, engagementLastPlayerOperation, playerOperation);
            }
            engagementLastMountPosition = position;
            engagementLastCarrier = carrier;
            engagementLastPlayerOperation = playerOperation;
            if (!engagement.Due(now)) return;
            var report = engagement.Report();
            LastEngagementFirstStop = engagement.FirstStop();
            TryLog(() => "Mounted real-time engagement: " + report +
                "; aiEnabled=" + (rider != null && rider.IsAIEnabled) + "; brainActive=" + (rider != null && rider.IsBrainActive) +
                "; riderHold=" + (rider != null && rider.HoldState) + "; mountHold=" + mount.HoldState +
                "; mountHoldRestricts=" + mountHold.MountHoldRestrictsAutomatic(mount.HoldState) + "; playerMoveHold=" + automaticGate.PlayerMoveHold +
                "; intent=" + (HasStockAttackIntent ? (stockIntent.Automatic ? "automatic" : "player") : "none") +
                "; lastObservation=" + LastStockAttackObservation + ".");
        }

        private bool StockIntentOwnMotionLive => stockIntentCommand != null &&
            (!stockIntentCommand.IsFinished || stockIntentCommand.DelegatedMove != null && !stockIntentCommand.DelegatedMove.IsFinished);

        // MMR-03/R1-F6: turn-based participant selection uses the dispatcher's own admission (CaptureContext and the action
        // evaluator: Standard, native capability block, paired addressability, selection and transaction), so an
        // individually ended or prohibited actor is skipped exactly as dispatch would refuse it.
        // MMR-02 (review R3-F2): the dispatcher's own admission of the rider's melee attack on a clicked target (Standard,
        // native capability block, paired addressability, selection and transaction), read before native accepts the click.
        // PR #70 R1-F8: the click is a fresh explicit order, so its own target-life fact applies (native UnitAttack admits an enemy that
        // is already unconscious when ordered); an earlier intent's permission is never borrowed.
        internal bool RiderMeleeClickAdmitted(UnitEntityData target) =>
            !disposed && relationship.State == RelationshipState.Mounted && target != null &&
            EvaluateStockActor(MountedCombatActionKind.RiderMelee, target, true).IsAllowed;

        // PR #70 R1-F8: a fresh explicit order on this target, decided by the target's own state now (living; conscious, or already
        // unconscious when ordered).
        private static bool FreshExplicitClickTargetLifeAdmits(UnitEntityData target)
        {
            var state = target?.Descriptor?.State;
            return state != null && UnifiedMountedStockAttackPolicy.TargetLifeAdmits(state.IsConscious, state.IsDead, state.IsFinallyDead, state.IsUnconscious);
        }

        private MountedCombatActionAvailability EvaluateStockActor(MountedCombatActionKind action, UnitEntityData target, bool freshExplicitClick = false)
        {
            NativeSingleAttackWeaponSelection ignored;
            return MountedCombatActionEvaluator.Evaluate(CaptureContext(action, target, out ignored, false, true, false, false, freshExplicitClick));
        }

        // R9-F1 (round 10), R11-F8 (rounds 12-13): an installed command's actual manual-target write on either actor supersedes each committed
        // request's outstanding operation on that exact actor (the same target included), also after the pair was unbound. Round 13: the
        // accepted-work routing (MountedAcceptedWorkRouting) also delivers it to a controller that is no longer the actor's pair owner while
        // that controller still owes an outstanding operation on the actor (ObservesOutstandingActor), until the operation settles.
        internal void ObserveOutstandingAssignment(UnitCommands container, UnitCommand command, MountedAcceptedWorkRoute route)
        {
            if (disposed || command == null || container == null) return;
            if (settlingProposals.Count > 0 && route != MountedAcceptedWorkRoute.Queue && !(command is MountedPairSingleAttack) && container.Contains(command) &&
                command.MarkManualTarget && command.AiAction == null && command.TargetUnit != null)
                foreach (var settling in settlingProposals)
                {
                    var settlingRider = settling.RiderToWrite ?? relationship.Rider;
                    if (container == settlingRider?.Commands && ReferenceEquals(settlingRider.CombatState?.ManualTarget, command.TargetUnit)) settling.RiderAssignedAfterCommit = true;
                    if (settling.MountToClear != null && container == settling.MountToClear.Commands && ReferenceEquals(settling.MountToClear.CombatState?.ManualTarget, command.TargetUnit))
                        settling.MountAssignedAfterCommit = true;
                }
        }

        // R11-F8 (round 13): this controller still owes an outstanding deferred operation on the actor owning this command container.
        internal bool ObservesOutstandingActor(UnitCommands container)
        {
            if (disposed || container == null) return false;
            foreach (var settling in settlingProposals)
                // R11-F8 (round 14): the retained request itself is the outstanding operation (ApplyAcceptanceEffects may have emptied its list
                // before a read that threw; the sweep keeps it until a retry completes), so observation follows retention, not the list.
                if ((settling.RiderToWrite != null && container == settling.RiderToWrite.Commands ||
                    settling.MountToClear != null && container == settling.MountToClear.Commands)) return true;
            return false;
        }

        // Reviews R1-F5, R3-F3, R2-F5. Called by the native acceptance seams only for a command native now holds: the private
        // UnitCommands.Run overload's postfix (slot; the public Run, queue promotion and a custom ExecuteBefore all reach it) and
        // AddToQueueInternal's postfix (queue; AddToQueue/AddToQueueFirst, Run's queue-instead path, custom handlers). A refused
        // submission or a merge into a running command is never observed. The retained order's own wrapper and every exact
        // delegated move it issued (repath replacements included) are recognized by identity, so they never count as newer work.
        internal void ObserveAcceptedWork(UnitCommands container, UnitCommand command, MountedAcceptedWorkRoute route)
        {
            if (disposed || command == null || container == null) return;
            ObserveOutstandingAssignment(container, command, route);
            if (relationship.State != RelationshipState.Mounted) return;
            // PR #70 R1-F6 (rounds 6-7): a pending provisional order's own command and carrier are not newer work against any order.
            if (IsPendingReplacementWork(command)) return;
            if (PendingReplacement) ObserveWorkAgainstProvisional(container, command, route);
            if (!HasStockAttackIntent || !retainedOrder.IsCurrent(stockIntent.Generation)) return;
            var onRider = container == relationship.Rider?.Commands;
            if (!onRider && container != relationship.Mount?.Commands) return;
            var facts = new MountedAcceptedWorkFacts
            {
                PairCommand = command is MountedPairSingleAttack,
                Reaction = command is UnitAttackOfOpportunity,
                AiOrigin = command.AiAction != null,
                PlayerOrder = IsPairPlayerOrder(command),
                FreeOrSwift = command.Type == UnitCommand.CommandType.Free || command.Type == UnitCommand.CommandType.Swift,
                NeedsApproach = CommandNeedsApproach(command)
            };
            // PR-1 review R1-F11 (round 4): the actor's latest ACTUAL assignment decides ownership, whatever the command's competing
            // class (a coexisting Free/Swift write counts). Native UpdateCombatTarget, inside the Run that installed the command (slot
            // or verified promotion), writes the manual target only for MarkManualTarget with no AiAction; an installed such command
            // whose target is now the actor's selection wrote it, the same target included. A queue insertion or a refused promotion
            // is never installed and writes nothing. The pair's own commands record their writes in OwnManualTarget.
            if (route != MountedAcceptedWorkRoute.Queue && !(command is MountedPairSingleAttack) && !IsOwnDelegatedMove(command) &&
                container.Contains(command) && command.MarkManualTarget && command.AiAction == null)
            {
                var actor = onRider ? relationship.Rider : relationship.Mount;
                var written = command.TargetUnit;
                if (written != null && ReferenceEquals(actor?.CombatState?.ManualTarget, written))
                {
                    SelectionOf(actor)?.ObserveForeignAssignment();
                    ForeignSelectionWriteCount++;
                }
            }
            if (MountedRetainedOrderLedger.Classify(retainedOrder.IsRetainedOwn(command), facts) != MountedAcceptedWorkClass.Competing) return;
            retainedOrder.ObserveAccepted(command, facts, route, (onRider ? "rider:" : "mount:") + DescribeCommand(command));
            LastCompetingWorkDetail = retainedOrder.OverrideDetail;
        }

        internal string LastCompetingWorkDetail { get; private set; }


        internal long ForeignSelectionWriteCount { get; private set; }

        // PR-1 review R1-F11: ClickUnitHandler.OnClick assigns the clicked unit as the manual target right after its Run. The click
        // is bracketed (prefix/postfix) so a write this click made for the pair's own order stays that order's, while the click's
        // own assignment to any other order's selection is the player's newest selection.
        // PR-1 review R1-F11 (round 5): the handler assigns the clicked unit to the units the click commands (the selection), so only
        // an actor that was selected at the click can have been assigned by it.
        // PR-1 review R1-F11 (round 6): a simulated click (prediction or pointer simulation) runs no player assignment and is not bracketed;
        // an unhandled click assigned nothing.
        // PR-1 review R1-F11 (round 8): installed ClickUnitHandler.OnClick (0x060093ED) assigns unit.CombatState.ManualTarget = clicked at one
        // call site (IL_0363) for each selected unit its loop does not skip. That call site itself is observed: the OnClick transpiler
        // (PatchMethods.ClickAssignmentTranspiler) replaces it with PatchMethods.ClickManualTargetAssignment, which performs the same
        // assignment and reports the assigned combat state and target (ObserveClickTargetAssignment). Selection, a handled result, an
        // unchanged value or any other Run on the actor prove nothing; only this assignment to this actor transfers ownership.
        // PR #70 R1-F7 (rounds 4-5): native PointerController.Tick (0x060093BA, the only caller of TurnController.IgnoreClick) is the input
        // dispatch: it evaluates IgnoreClick, whose postfix may produce a proof, and then runs that click's handler. Its body is wrapped in
        // try/finally (PatchMethods.PointerTickTranspiler), so the dispatch also ends, and its unclaimed proof is discarded, when it throws. A proof records the
        // dispatch it was produced in; only a click bracket that opens inside that SAME dispatch may claim it, and the dispatch's end
        // discards a proof no handler claimed. A proof produced outside any dispatch (a standalone IgnoreClick read) is never claimable,
        // and a later request (a new dispatch, or a handler reached outside PointerController.Tick) cannot adopt an earlier proof.
        private long pointerDispatch;
        private bool inPointerDispatch;
        internal bool InPointerDispatch => inPointerDispatch;
        internal void BeginPointerDispatch()
        {
            if (pendingClickProof != null) DiscardClickProof("a new pointer dispatch began");
            pointerDispatch++;
            inPointerDispatch = true;
            dispatchTargetBound = false; dispatchTarget = null; dispatchTurn = null;
        }

        internal void EndPointerDispatch()
        {
            inPointerDispatch = false;
            dispatchTargetBound = false; dispatchTarget = null; dispatchTurn = null;
            if (pendingClickProof != null) DiscardClickProof("its pointer dispatch ended without its click taking it");
        }

        // PR #70 R5-F2 (round 6): a scoped direct-input dispatch names the target its handler will receive and the turn it was opened in
        // (MountedPatchController.BeginScopedInputDispatch). An override may be applied in it only for that exact target and turn; native
        // PointerController.Tick binds none (its handler receives the hovered object native IgnoreClick read).
        private bool dispatchTargetBound;
        private UnitEntityData dispatchTarget;
        private object dispatchTurn;
        internal void BindDispatchTarget(UnitEntityData target, object turn)
        {
            if (!inPointerDispatch) return;
            dispatchTargetBound = true; dispatchTarget = target; dispatchTurn = turn;
        }

        internal bool DispatchCarriesOverrideFor(UnitEntityData target, object turn) => inPointerDispatch &&
            (!dispatchTargetBound || target != null && ReferenceEquals(dispatchTarget, target) && turn != null && ReferenceEquals(dispatchTurn, turn));

        internal void BeginClickObservation(IEnumerable<UnitEntityData> selected, bool simulated)
        {
            if (disposed || simulated) { activeClick = -1; return; }
            activeClick = ++clickSerial;
            riderCommandedByClick = false;
            mountCommandedByClick = false;
            if (pendingClickProof != null)
            {
                if (pendingClickProof.ClickSerial < 0 && pendingClickProof.Dispatch >= 0 && inPointerDispatch && pendingClickProof.Dispatch == pointerDispatch)
                    pendingClickProof.ClickSerial = activeClick;
                else DiscardClickProof("not its own dispatch's click");
            }
        }

        // The click's own ManualTarget assignment (from the transpiled OnClick call site) inside the open bracket.
        internal void ObserveClickTargetAssignment(Kingmaker.Controllers.Combat.UnitCombatState combatState, UnitEntityData target)
        {
            if (disposed || activeClick < 0 || combatState == null || target == null) return;
            if (relationship.Rider != null && ReferenceEquals(relationship.Rider.CombatState, combatState)) riderCommandedByClick = true;
            // R9-F1 (round 10): and of a committed request's outstanding operation on that actor.
            foreach (var settlingClick in settlingProposals)
            {
                var settlingRider = settlingClick.RiderToWrite ?? relationship.Rider;
                if (settlingRider != null && ReferenceEquals(settlingRider.CombatState, combatState)) settlingClick.RiderAssignedAfterCommit = true;
                if (settlingClick.MountToClear != null && ReferenceEquals(settlingClick.MountToClear.CombatState, combatState)) settlingClick.MountAssignedAfterCommit = true;
            }
            // PR #70 R6-F1 (round 8): a later click's actual assignment of the rider's target is the player's newer assignment.
            var pending = firstTickProposal;
            if (PendingReplacement && pending.ClickSerial != activeClick && relationship.Rider != null && ReferenceEquals(relationship.Rider.CombatState, combatState))
                pending.NewerTargetObserved = true;
            if (relationship.Mount != null && ReferenceEquals(relationship.Mount.CombatState, combatState)) mountCommandedByClick = true;
        }

        internal void EndClickObservation(UnitEntityData clicked, bool handled)
        {
            var serial = activeClick;
            activeClick = -1;
            // PR #70 R1-F7: a proof its own click handling did not take is abandoned with the click.
            if (pendingClickProof != null && pendingClickProof.ClickSerial == serial) DiscardClickProof("click handling ended without taking it");
            if (disposed || serial < 0 || clicked == null || !handled) return;
            riderSelection.ObserveClickAssignment(serial, relationship.Rider?.CombatState?.ManualTarget, clicked, riderCommandedByClick);
            mountSelection.ObserveClickAssignment(serial, relationship.Mount?.CombatState?.ManualTarget, clicked, mountCommandedByClick);
        }

        private bool riderCommandedByClick, mountCommandedByClick;

        internal MountedRetainedOrderLedger RetainedOrder => retainedOrder;

        // Native would move its actor for it: not started and not yet close enough (UnitCommand.IsUnitEnoughClose).
        private static bool CommandNeedsApproach(UnitCommand command)
        {
            if (command.IsStarted) return false;
            try { return !command.IsUnitEnoughClose; }
            catch (Exception) { return true; }
        }

        // PR #70 R12-F4 (round 16): every mount command whose motion this pair owns: its exact paired native commands and carriers, the
        // retained order's and active command's own carriers, and a carrier whose cleanup is retained. Read by the relationship's mod-disable
        // restoration (KingmakerMountedPairRuntime.PreservesMountSuccessor).
        // The player's admitted ground order is native's own command, never the pair's (ReleaseRiderTurnGroundMovementForModDisable).
        // Round 17 (R16-F1): the pair's own command types on the mount (its pair wrapper and child attack) are always its own.
        internal bool OwnsMountMotionCommand(UnitCommand command) => command != null && !ReferenceEquals(command, activeRiderTurnGroundMove) &&
            (command is MountedPairAttackCommand || command is MountedPairSingleAttack || OwnsExactPairedNativeCommand(command) || IsOwnDelegatedMove(command) || retainedCarrierCleanups.Any(record => ReferenceEquals(record.Carrier, command)));

        // The pair's own exact carrier moves: the retained order's, and the active pair command's (a charge included).
        internal bool IsOwnDelegatedMove(UnitCommand command) => command is UnitMoveTo &&
            (retainedOrder.IsRetainedOwn(command) || activeCommand != null && activeCommand.IsOwnDelegatedMove(command));

        // Review R1-F8: UnitMoveTo.OnAction sets a player-faction executor's Hold in real-time combat. For the pair's own
        // delegated move that write is undone (an unmounted attacker's approach sets none); every other move keeps it.
        internal void RestoreHoldAfterMoveAction(UnitMoveTo move, bool holdBefore)
        {
            var executor = move?.Executor;
            if (disposed || executor == null || executor != relationship.Mount) return;
            if (!MountedHoldProvenance.RestoreHoldAfterMoveAction(IsOwnDelegatedMove(move), holdBefore, executor.HoldState)) return;
            executor.HoldState = holdBefore;
            if (executor.HoldState != holdBefore) mountHold.ObserveUnrestoredCarrierHold();
            else OwnMoveHoldRestoredCount++;
        }

        internal int OwnMoveHoldRestoredCount { get; private set; }

        // MMR-02 (review R4-F1; PR #70 R1-F1, R1-F7, R1-F9): the proof of an overridden turn-based click: the executable route to its
        // verified destination (empty when the strike was admissible where the pair stood) and the plan identity it was proven for.
        // It is bound to the exact click (target, turn context, frame and a sequence number); the click's own handling takes it once
        // (TakeValidatedClickProof) on every disposition, and it is bound to the order generation that click starts or continues
        // (BindClickProof). Only that order's wrapper receives it (AttachClickProof). A native-accepted click on the rider's context
        // discards any pending proof, so no proof passes from one click to the next.
        internal sealed class ClickProof
        {
            public UnitEntityData Target;
            public object Turn;
            public int Frame;
            public long Sequence;
            // PR #70 R1-F7 (round 3): the serial of the exact click bracket (BeginClickObservation) whose own handling may take it.
            public long ClickSerial = -1;
            // PR #70 R1-F7 (round 4): the pointer dispatch that produced it (-1: produced outside any dispatch, never claimable).
            public long Dispatch = -1;
            public List<Vector3> Route;
            public PairedAttackClickProbe.PlanIdentity Plan;
            // PR #70 R1-F6 (round 6): the proven native arrival radius of the route's destination, and whether the route's movement is the
            // restricted single move's one tick (the click then becomes a provisional request decided at its carrier's first tick).
            public float ArrivalRadius = float.NaN;
            public bool FirstTick;
            // PR #70 R14-F3 (round 15): the provisional request is a step whose native counter binds (not the restricted single tick): compatible
            // older mount work does not refuse it; its carrier waits behind that work (the existing accepted-work classification and waiting path).
            public bool StepCounterBinds;
        }

        // PR #70 R14-F2 (round 15): the grant carries its step-counter classification, computed from the same speed observations that priced it.
        internal delegate float MountGrantReader(out bool step, out bool singleMove, out string reason, out bool stepCounterBinds);

        private ClickProof pendingClickProof;
        private long clickProofSequence;
        private ClickProof intentClickProof;
        private long intentClickProofGeneration = -1;
        internal int ClickProofDiscardCount { get; private set; }
        internal string LastClickProofDisposition { get; private set; } = "<none>";
        internal PairedAttackClickProbe.Result LastAttackClickProbe { get; private set; }
        internal ClickProof PendingClickProofForDiagnostics => pendingClickProof;
        internal ClickProof IntentClickProofForDiagnostics => intentClickProof;

        // MMR-02 (reviews R1-F3, R3-F2; PR #70 R1-F1, R1-F8, R1-F9): the admission of a turn-based rider melee click that native
        // dropped, from the native input facts PairedActivationControls read. The rider's attack is admitted by the dispatcher's own
        // rule with the fresh click's target-life fact, the mount's grant is read first and bounds the probe, and only a proven
        // executable route (or a strike admissible where the pair stands) overrides native's refusal. The click's proof is exactly
        // this decision's: a route, an in-range marker or nothing.
        internal PairedAttackClickAdmission AdmitTurnBasedAttackClick(UnitEntityData target, object turn, bool inputCandidate, bool riderAddressable,
            bool riderSelected, bool riderCommandsEmpty, bool predictionsCurrent, bool overTerrain, bool abilitySelected, bool meleeOnTarget,
            MountGrantReader readGrant, PairedAttackClickProbe.PlanIdentity predictedPlan, out string reason, out string trace)
        {
            var rider = relationship.Rider;
            var mount = relationship.Mount;
            var riderAttack = inputCandidate && RiderMeleeClickAdmitted(target);
            bool stepMode = false, singleMove = false, pricedStepCounterBinds = false;
            var grantReason = "<not-read>";
            var granted = riderAttack && readGrant != null ? readGrant(out stepMode, out singleMove, out grantReason, out pricedStepCounterBinds) : 0f;
            PairedAttackClickProbe.Result probe = null;
            if (riderAttack)
            {
                try { probe = PairedAttackClickProbe.Evaluate(rider, mount, target, granted); }
                catch (Exception error) { TryLogException("Turn-based rider attack click probe failed; native decision kept", error); }
            }
            var decision = PairedAttackClickAdmissionPolicy.Decide(true, true, riderAddressable, riderSelected, riderCommandsEmpty,
                predictionsCurrent, overTerrain, abilitySelected, meleeOnTarget, riderAttack,
                probe != null && probe.AdmissibleNow, probe != null && probe.RouteComplete, probe != null && probe.RouteProven,
                probe?.RouteMetres ?? float.NaN, granted, out reason);
            // PR #70 R1-F9: the click's own native prediction (the UnitAttack native IgnoreClick evaluated) must plan the strike the probe
            // proved: same executor, mode, first weapon and reach.
            if (decision != PairedAttackClickAdmission.NativeDecision && (probe == null || !probe.Plan.Matches(predictedPlan)))
            {
                decision = PairedAttackClickAdmission.NativeDecision;
                reason = "prediction-plan-mismatch";
            }
            LastAttackClickProbe = probe;
            // PR #70 round 13 (owner amendment 2026-11-27g section 3-4): an ordinary click whose route the grant covers is admitted like native's
            // own (IgnoreClick's movement-limited visualized path; no forecast of later frames): it is an accepted order. The restricted
            // single-action tick stays a provisional request decided before its debit, and (R12-F1, round 14) so does a step whose native counter
            // binds the step (its debit can be final): its replacement effects wait for that debit's own decision. R14-F2 (round 15): that
            // classification is the one the grant was priced with (the same speed observations), never a second read.
            var finalStepPossible = decision == PairedAttackClickAdmission.AdmitRoute && stepMode && grantReason == "granted" && pricedStepCounterBinds;
            RememberValidatedClickProof(target, turn,
                decision == PairedAttackClickAdmission.AdmitRoute ? probe.Route :
                decision == PairedAttackClickAdmission.AdmitInRange ? new List<Vector3>() : null,
                probe?.Plan ?? default(PairedAttackClickProbe.PlanIdentity), probe?.ArrivalRadius ?? float.NaN,
                decision == PairedAttackClickAdmission.AdmitRoute && (grantReason == "single-move-first-tick" || finalStepPossible), finalStepPossible);
            trace = "mountStepMode=" + stepMode + "; mountSingleMove=" + singleMove + "; mountGrant=" + grantReason + "; finalStepPossible=" + finalStepPossible +
                "; mountGrantedMetres=" + granted.ToString("R") + "; routeMetres=" + (probe?.RouteMetres ?? float.NaN).ToString("R") +
                "; diagnosticReachMetres=" + (probe?.DiagnosticReachMetres ?? float.NaN).ToString("R") + "; path=" + (probe?.Path.ToString() ?? "<not-run>") +
                "; probe=" + (probe?.Detail ?? "<not-run>") + "; pairRadius=" + (probe?.PairRadius ?? float.NaN).ToString("R") +
                "; plan={" + (probe?.Plan.ToString() ?? "<not-run>") + "}; predicted={" + predictedPlan + "}; proof=" + LastClickProofDisposition;
            return decision;
        }

        internal void RememberValidatedClickProof(UnitEntityData target, object turn, List<Vector3> route, PairedAttackClickProbe.PlanIdentity plan,
            float arrivalRadius = float.NaN, bool firstTick = false, bool stepCounterBinds = false)
        {
            if (pendingClickProof != null) DiscardClickProof("replaced by a later click decision");
            if (route == null) return;
            pendingClickProof = new ClickProof
            {
                Target = target, Turn = turn, Frame = Time.frameCount, Sequence = ++clickProofSequence, Route = new List<Vector3>(route), Plan = plan,
                Dispatch = inPointerDispatch ? pointerDispatch : -1, ArrivalRadius = arrivalRadius, FirstTick = firstTick, StepCounterBinds = firstTick && stepCounterBinds
            };
            LastClickProofDisposition = "pending:#" + pendingClickProof.Sequence;
        }

        // A native-accepted click on the rider's context: native's own path rule applies to it, so no override proof belongs to it.
        internal void DiscardPendingClickProof(string why)
        {
            if (pendingClickProof != null) DiscardClickProof(why);
        }

        private void DiscardClickProof(string why)
        {
            pendingClickProof = null;
            ClickProofDiscardCount++;
            LastClickProofDisposition = "discarded:" + why;
        }

        // The exact click's own handling: always clears the pending proof; returns it only for this click.
        private ClickProof TakeValidatedClickProof(UnitEntityData target, object turn)
        {
            var proof = pendingClickProof;
            if (proof == null) return null;
            var exact = ReferenceEquals(proof.Target, target) && turn != null && ReferenceEquals(proof.Turn, turn) && Time.frameCount - proof.Frame <= 1 &&
                proof.ClickSerial >= 0 && proof.ClickSerial == activeClick;
            if (!exact) { DiscardClickProof("another click"); return null; }
            pendingClickProof = null;
            LastClickProofDisposition = "taken:#" + proof.Sequence;
            return proof;
        }

        // Binds the click's proof, or its absence, to the order generation the click starts or continues (undispatched).
        private void BindClickProof(ClickProof proof, string disposition)
        {
            if (intentClickProof != null && !ReferenceEquals(intentClickProof, proof)) ClickProofDiscardCount++;
            intentClickProof = proof;
            intentClickProofGeneration = proof == null ? -1 : stockIntent.Generation;
            LastClickProofDisposition = disposition + (proof == null ? ":none" : ":#" + proof.Sequence);
        }

        // TryExecuteAction: the order's own wrapper receives the proof bound to its generation and target, once.
        private void AttachClickProof(MountedPairAttackCommand command, MountedCombatActionKind action, UnitEntityData target)
        {
            var proof = intentClickProof;
            intentClickProof = null;
            if (proof == null) return;
            if (action != MountedCombatActionKind.RiderMelee || intentClickProofGeneration != stockIntent.Generation || !ReferenceEquals(proof.Target, target))
            {
                ClickProofDiscardCount++;
                LastClickProofDisposition = "discarded:not-this-order";
                return;
            }
            command.ValidatedApproachRoute = proof.Route;
            command.ClickPlan = proof.Plan;
            // Review R7-F1: the destination established at planning is re-examined by every revalidation.
            var established = proof.Route.Count >= 2 ? proof.Route[proof.Route.Count - 1] : (Vector3?)null;
            var plan = proof.Plan;
            command.RouteRevalidator = dispatchPlan => RevalidateAdmittedClickRoute(target, established, plan, dispatchPlan);
            // PR #70 R1-F6 (round 7, R6-F4): a carrier revalidated in the first-tick state is decided at its first tick.
            command.FirstTickCarrierIssued = carrier => BindFirstTickCarrier(command, carrier);
            command.FirstTickCarrierPermitted = () => FirstTickCarrierPermitted(command);
            command.CarrierReleasedForStrike = () => CarrierReleasedForStrike(command);
            command.ProvisionalStrikeGate = () => AdmitProvisionalStrike(command);
            LastClickProofDisposition = "attached:#" + proof.Sequence;
        }

        // PR #70 R1-F6 (rounds 6-7): the first-tick boundary (docs/missions/mmr-split/PR-2-first-tick-boundary.md). The restricted single-action
        // move grants ONE movement tick whose length is known only at that tick. A turn-based rider click admitted in that state (proof.FirstTick)
        // is a PROVISIONAL ORDER (round 7): an ordinary pair attack command run on the RIDER for the clicked target through the real action
        // evaluation (TryExecuteAction), carrying the click's proof (route, plan, revalidator) but no intent, generation, manual-target or Hold
        // effect. Being the rider's own command it fills the rider turn's command container, so native TurnController.Tick moves a Preparing
        // turn to Acting (Status 2 -> 3 when !m_Commands.Empty, m_Commands = Unit.Commands); it is the active pair command, so native command
        // eligibility admits its carrier (OwnsExactPairedNativeCommand: activeCommand.DelegatedMove) and TryOverrideMountTurnMovement drives
        // that carrier on the Acting turn. Its carrier is created by its own BeginDelegatedMove from a route revalidated from the CURRENT
        // origin; the gate is bound to that exact carrier only after it holds the mount's Move slot (FirstTickCarrierIssued). The carrier's
        // first TickDelegated decides from live state, before any debit: commit (the previous intent is retired, intent B starts with the
        // click's facts, the provisional command becomes B's own wrapper and keeps its exact carrier, and the deferred manual target and Hold
        // are applied unless the player assigned newer ones), commit in place, or refuse (the tick is zeroed; the provisional command and its
        // exact carrier are retired at the next sweep and the previous order resumes). The same gate binds the carrier of an ACCEPTED
        // route-bound order whose revalidation ran in the first-tick state (R6-F4): its search ceiling never becomes an executable grant.
        internal sealed class FirstTickProposal : IMountedFirstTickGate
        {
            public MountedCombatController Owner;
            // true: a provisional replacement (the click is not yet an order); false: the carrier of an accepted order.
            public bool Replacement;
            public UnitAttack StockAttack;
            public UnitEntityData Principal, Target;
            public TurnController Turn;
            public ClickProof Proof;
            // The click's immutable target-life fact (R6-F3): an enemy already unconscious when clicked stays a valid explicit target.
            public bool TargetUnconsciousAtClick;
            public long ClickSerial = -1;
            // The provisional order (Replacement) or the accepted order's wrapper; its exact carrier once its BeginDelegatedMove issued one.
            public MountedPairAttackCommand Command;
            public UnitMoveTo CarrierMove;
            public MountedFirstTickProposalState State;
            public bool Retired;
            public string Detail = "<pending>";
            public float AllowedMetres = float.NaN;
            // R6-F1: the player's state the provisional order must not change before it commits.
            public UnitEntityData TargetBeforeClick;
            public bool TargetWriteDeferred, RiderHoldAtClick, RiderHoldDeferred, MountHoldDeferred;
            public long CommittedGeneration = -1;
            public object Carrier => CarrierMove;
            // R6-F1 (round 8): an actual later write of the rider's target observed at the installed-command or click hooks; R7-F3: the newer
            // competing work that superseded this request.
            public bool NewerTargetObserved;
            public string Superseded;
            // R6-F7 (round 9): the accepted order's deferred engine writes still to apply, and those that failed (retried by the sweep).
            public bool EffectRiderTarget, EffectMountTargetClear, EffectRiderHold, EffectMountHold;
            // R10-F4 (round 11): the retired generation's mount target the outstanding clear belongs to (A's owned selection); R11-F8 (round 12):
            // and the exact mount and rider the deferred operations address, captured at the commit (never re-read from the relationship).
            public UnitEntityData MountTargetToClear, MountToClear, RiderToWrite;
            // R9-F1 (round 10): an actual assignment of that actor's manual target observed after the commit supersedes its outstanding operation.
            public bool RiderAssignedAfterCommit, MountAssignedAfterCommit;
            // R6-F7 (round 10): the exact cleanup obligations of a refused request, tracked independently of native Result/IsFinished.
            public bool CleanupCommandDone, CleanupCarrierDone, CleanupSlotDone, CleanupMotionDone, CleanupFaulted;
            public readonly List<string> OutstandingEffects = new List<string>();
            // PR #70 R14-F4 (round 15): ownership of the deferred work is its effect flags (each cleared only when its write succeeded or an actual
            // newer assignment superseded it); OutstandingEffects is the diagnostic list of the writes that failed in the latest application and
            // can be empty while work is owed (an application that threw before reaching a write handler).
            public bool EffectsOutstanding => EffectRiderTarget || EffectMountTargetClear || EffectRiderHold || EffectMountHold;
            public string DescribeEffects()
            {
                if (!EffectsOutstanding) return "complete";
                var owed = new List<string>();
                if (EffectMountTargetClear) owed.Add("mount-target");
                if (EffectRiderTarget) owed.Add("rider-target");
                if (EffectRiderHold) owed.Add("rider-hold");
                if (EffectMountHold) owed.Add("mount-hold");
                return "outstanding(" + string.Join(",", owed.ToArray()) + ")";
            }
            public MountedFirstTickVerdict Decide(object movementContext, float allowedTick, float deltaTime, float speed, float stepTravelLeft, out float approvedTick, out string detail) =>
                Owner.DecideFirstTickProposal(this, movementContext, allowedTick, deltaTime, speed, stepTravelLeft, false, out approvedTick, out detail);
        }

        private FirstTickProposal firstTickProposal;
        // R10-F3 (round 11): the committed request whose order has not yet started its first child (checked once, immediately before it starts).
        private FirstTickProposal firstChildEntitlement;
        internal int FirstChildEntitlementRefusalCount { get; private set; }
        // R6-F7 (round 9), R11-F8 (round 12): committed requests whose deferred effects are still outstanding; the sweep retries each (B's own
        // effects while B is current, the retired generation's mount-target clear on its exact mount until it completes or is superseded).
        private void SettleOutstandingEffects()
        {
            for (var i = 0; i < settlingProposals.Count; i++)
            {
                var settling = settlingProposals[i];
                try { ApplyAcceptanceEffects(settling); }
                catch (Exception error) { TryLogException("Deferred acceptance effects retry failed; retained", error); continue; }
                // R14-F4 (round 15): retention follows the effect flags, never the diagnostic list.
                if (!settling.EffectsOutstanding) { settlingProposals.RemoveAt(i); i--; }
            }
        }

        // R11-F7 (round 12): an accepted command whose exact carrier's cleanup was left unfinished at its terminal (a fault in OnEnded after native
        // completion) keeps that obligation here until SettleCarrierCleanup reports every step settled (same slot and successor checks; the
        // terminal itself was reported once by the command).
        // The record (exact carrier and mount, the unfinished steps) is retained, never the finished wrapper.
        private readonly List<MountedCarrierCleanup> retainedCarrierCleanups = new List<MountedCarrierCleanup>();
        internal int RetainedCarrierCleanupCount => retainedCarrierCleanups.Count;
        private void RetainCarrierCleanup(MountedPairAttackCommand command)
        {
            var record = command?.CarrierCleanupRecord;
            if (record != null && record.Pending && !retainedCarrierCleanups.Contains(record)) retainedCarrierCleanups.Add(record);
        }
        private void SettleRetainedCarrierCleanups()
        {
            for (var i = 0; i < retainedCarrierCleanups.Count; i++)
            {
                var record = retainedCarrierCleanups[i];
                bool settled;
                try { settled = !record.Pending || record.Settle(null); }
                catch (Exception error) { FirstTickCleanupFailureCount++; TryLogException("Accepted command's carrier cleanup retry failed; retained", error); continue; }
                if (settled) { retainedCarrierCleanups.RemoveAt(i); i--; }
            }
        }
        // PR #70 R12-F4 (round 13): owned work that must settle before teardown is reported successful: outstanding acceptance effects (a
        // deferred target write or clear, a Hold write), a retained carrier cleanup record, and a refused request whose exact cleanup has not
        // finished. It keeps the pair's registry owner (HasRetainedPairCommands) and makes the pair's teardown readiness false, so disable and
        // unload refuse BEFORE any patch or observer is removed; the controller's per-frame drive (every relationship state) and the
        // lifecycle's disable/unload drain retry it (one attempt per call, never a loop).
        internal bool HasOutstandingCleanup => settlingProposals.Count > 0 || retainedCarrierCleanups.Count > 0 ||
            firstTickProposal != null && firstTickProposal.State == MountedFirstTickProposalState.Refused && !firstTickProposal.Retired;

        // One retry of the outstanding cleanup (no pending request is decided here); true when nothing remains.
        internal bool TrySettleOutstandingCleanup()
        {
            if (!HasOutstandingCleanup) return true;
            try
            {
                SettleOutstandingEffects();
                SettleRetainedCarrierCleanups();
                var p = firstTickProposal;
                if (p != null && p.State == MountedFirstTickProposalState.Refused && !p.Retired) SweepFirstTickProposal();
            }
            catch (Exception error) { TryLogException("Outstanding mounted cleanup retry failed; retained for the next drain", error); }
            return !HasOutstandingCleanup;
        }

        // R6-F7 (round 9): a committed request whose deferred effects are still outstanding; the sweep retries them while its order is current.
        // R11-F8 (round 12): every committed request with outstanding deferred effects (not one reference a later commit could overwrite).
        private readonly List<FirstTickProposal> settlingProposals = new List<FirstTickProposal>();
        internal FirstTickProposal SettlingProposalForDiagnostics => settlingProposals.Count == 0 ? null : settlingProposals[settlingProposals.Count - 1];
        internal int SettlingProposalCount => settlingProposals.Count;
        internal int FirstTickCleanupFailureCount { get; private set; }
        private MountedPairAttackCommand evaluatingFirstTickCommand;
        internal FirstTickProposal FirstTickProposalForDiagnostics => firstTickProposal;
        internal FirstTickProposal LastFirstTickProposal { get; private set; }
        internal int FirstTickCommitCount { get; private set; }
        internal int FirstTickRefusalCount { get; private set; }
        internal int FirstTickRetiredCommandCount { get; private set; }
        internal int DeferredClickTargetWriteCount { get; private set; }
        internal int DeferredHoldWriteCount { get; private set; }
        internal string LastFirstTickDecision { get; private set; } = "<none>";
        private bool PendingReplacement => firstTickProposal != null && firstTickProposal.Replacement && firstTickProposal.State == MountedFirstTickProposalState.Pending;
        private bool IsPendingReplacementWork(UnitCommand command) => command != null && PendingReplacement && firstTickProposal.Command != null &&
            (ReferenceEquals(command, firstTickProposal.Command) || firstTickProposal.Command.IsOwnDelegatedMove(command));

        // Work on the mount other than the given carrier (finished commands are gone for this purpose).
        private bool MountHoldsOtherWork(UnitCommand except)
        {
            var commands = relationship.Mount?.Commands;
            if (commands == null) return true;
            if (commands.Queue.Count > 0) return true;
            foreach (var command in commands.Raw)
                if (command != null && !command.IsFinished && !ReferenceEquals(command, except)) return true;
            return false;
        }

        // The provisional order's installation (TryRouteMountedStockAttack). Refused before anything is installed when any rider or mount work
        // is live. The proposal is published before the native Run so the Run's Hold seam and the click's manual-target seam recognize it; any
        // failure settles it completely (no gate is bound before a carrier holds the mount's Move slot).
        private void BeginProvisionalOrder(UnitAttack stockAttack, UnitEntityData principal, UnitEntityData target, TurnController turn, ClickProof proof)
        {
            RetirePendingReplacement("a newer click");
            var rider = relationship.Rider;
            var mount = relationship.Mount;
            var state = target?.Descriptor?.State;
            var proposal = new FirstTickProposal
            {
                Owner = this, Replacement = true, StockAttack = stockAttack, Principal = principal, Target = target, Turn = turn, Proof = proof,
                TargetUnconsciousAtClick = state != null && state.IsUnconscious, ClickSerial = activeClick,
                TargetBeforeClick = rider?.CombatState?.ManualTarget, RiderHoldAtClick = rider != null && rider.HoldState
            };
            LastFirstTickProposal = proposal;
            string refusal = null;
            if (rider == null || mount == null) refusal = "pair-unavailable";
            else if (HasActiveCommand || activeDoorInteraction != null && !activeDoorInteraction.IsFinished || chargeOwner != null) refusal = "pair-work-live";
            // R7-F5 (round 8): a pending door settlement owns the pair; installing the provisional order would cancel it before acceptance.
            else if (pendingDoorSettlement != null) refusal = "door-settlement-pending";
            else if (!rider.Commands.Empty) refusal = "rider-holds-live-work";
            // R14-F3 (round 15): a step request (its native counter binds; its route may need several frames) is classified with the existing
            // accepted-work rule (CompetingMountWork): compatible older mount work (an in-range Free/Swift action, a reaction) coexists and the
            // request's carrier waits behind it on the existing deferred-carrier path, so finishing that work releases the request; competing
            // work still refuses it. The restricted single tick keeps its idle-mount precondition. The before-debit decision of the step's
            // carrier (final-step-counter) still protects the prior order where the actual debit is final.
            else if (proof.StepCounterBinds ? CompetingMountWork(null) != null : MountHoldsOtherWork(null)) refusal = "mount-holds-live-work";
            else if (proof.Route == null || proof.Route.Count < 2 || !(proof.ArrivalRadius > 0f)) refusal = "no-route";
            else if (ResolveRiderPrimaryAction() != MountedCombatActionKind.RiderMelee) refusal = "not-a-melee-order";
            if (refusal != null)
            {
                proposal.State = MountedFirstTickProposalState.Refused;
                proposal.Retired = true;
                proposal.Detail = refusal;
                FirstTickRefusalCount++;
                LastFirstTickDecision = "refused:" + refusal;
                LastStockAttackObservation = "first-tick-proposal-refused;" + refusal;
                return;
            }
            firstTickProposal = proposal;
            var result = MountedCombatClickResult.HandledRejected;
            try { result = TryExecuteAction(MountedCombatActionKind.RiderMelee, target, "stock-hostile-click", rider, true, false, false, proposal); }
            catch (Exception error) { TryLogException("Turn-based provisional order installation failed; it is settled", error); }
            if (result != MountedCombatClickResult.HandledAccepted || proposal.Command == null || proposal.Command.IsFinished ||
                !ReferenceEquals(activeCommand, proposal.Command) || !ReferenceEquals(rider.Commands.Standard, proposal.Command))
            {
                RefuseFirstTickProposal("provisional-order-not-installed(" + result + ")");
                SweepFirstTickProposal();
                return;
            }
            LastFirstTickDecision = "pending:#" + proof.Sequence;
            LastStockAttackObservation = "first-tick-proposal;target=" + target.UniqueId;
            TryLog(() => "Turn-based rider attack on the restricted single move's one tick: provisional order #" + proof.Sequence +
                " runs on the rider; the previous order, manual target and Hold are unchanged until its carrier's first tick decides.");
        }

        // BeginDelegatedMove of a route-bound turn-based wrapper (round 8, R7-F1: in every movement mode), after its carrier holds the Move slot. A
        // request of the same command keeps deciding every carrier that command issues (a replan included): a pending one decides its first
        // tick, a refused one refuses it until the sweep settles the command, so no carrier of a first-tick route ever moves ungated.
        private void BindFirstTickCarrier(MountedPairAttackCommand command, UnitMoveTo carrier)
        {
            if (disposed || command == null || carrier == null) return;
            var p = firstTickProposal;
            if (p != null && ReferenceEquals(p.Command, command) && !p.Retired)
            {
                p.CarrierMove = carrier;
                unifiedTurn.BindMountFirstTickGate(p, true);
                return;
            }
            // Another command's carrier now holds the Move slot: a pending provisional order lost its carrier and is settled; an earlier gate is released.
            if (p != null && p.Replacement) RetireFirstTickProposal("another carrier took the mount's Move slot");
            else if (p != null) unifiedTurn.BindMountFirstTickGate(p, false);
            var ordinary = new FirstTickProposal
            {
                Owner = this, Replacement = false, Command = command, CarrierMove = carrier, Target = stockIntentTarget,
                Turn = Game.Instance?.TurnBasedCombatController?.CurrentTurn,
                TargetUnconsciousAtClick = stockIntent.TargetUnconsciousWhenOrdered, CommittedGeneration = stockIntent.Generation
            };
            firstTickProposal = ordinary;
            LastFirstTickProposal = ordinary;
            unifiedTurn.BindMountFirstTickGate(ordinary, true);
        }

        // The target's validity under the click's own target-life fact (R6-F3).
        private bool FirstTickTargetValid(FirstTickProposal p)
        {
            var target = p.Target;
            var state = target?.Descriptor?.State;
            return target != null && target.IsInState && state != null && relationship.Rider != null && relationship.Rider.IsEnemy(target) &&
                UnifiedMountedStockAttackPolicy.TargetLifeAdmits(state.IsConscious, state.IsDead, state.IsFinallyDead, p.TargetUnconsciousAtClick);
        }

        // The rider's current entitlement to the strike (Standard, action eligibility, attack capability): the dispatcher's own admission,
        // with the proposal's command counted as the pair's own transaction.
        private bool FirstTickRiderEntitled(FirstTickProposal p)
        {
            evaluatingFirstTickCommand = p.Command;
            try { return RiderMeleeClickAdmitted(p.Target); }
            finally { evaluatingFirstTickCommand = null; }
        }

        // The decision. inPlaceOnly: the provisional command reached its strike without a decided tick (its own carrier release, or
        // StartChildAttack); only a strike admissible where the pair stands commits there. movementContext: the input context the executing
        // movement adapter debits with (round 9, R8-F5). Every failure inside the decision settles the request as refused (R6-F7); the live
        // wrapper's three plans must agree before any commit (R7-F2); a stationary strike commits in place on the rider's own entitlement, any
        // movement needs the mount's current authority read on that same context (R7-F4, R8-F5), the agent's own live route (R6-F2, R8-F1),
        // and, for the restricted single tick, the installed shift contract (R8-F6).
        internal MountedFirstTickVerdict DecideFirstTickProposal(FirstTickProposal p, object movementContext, float allowedTick, float deltaTime, float speed, float stepTravelLeft,
            bool inPlaceOnly, out float approvedTick, out string detail)
        {
            approvedTick = 0f;
            // Reporting only (round 17, R16-F4): the decision works in durations; metres are speed x duration for the record.
            var allowedMetres = allowedTick * speed;
            if (p == null || !ReferenceEquals(p, firstTickProposal) || p.State != MountedFirstTickProposalState.Pending)
            {
                detail = "settled:" + (p == null ? "none" : p.State + ":" + p.Detail);
                var settledCommit = p != null && p.State == MountedFirstTickProposalState.Committed;
                if (settledCommit) approvedTick = allowedTick;
                return settledCommit ? MountedFirstTickVerdict.Commit : MountedFirstTickVerdict.Refuse;
            }
            p.AllowedMetres = allowedMetres;
            var tick = ";allowedTick=" + allowedTick.ToString("R") + ";allowedMetres=" + allowedMetres.ToString("R") + ";tick=" + deltaTime.ToString("R");
            string refusal;
            bool inPlace = false;
            float granted = 0f;
            try { refusal = EvaluateFirstTickDecision(p, movementContext, allowedTick, deltaTime, speed, stepTravelLeft, inPlaceOnly, out inPlace, out granted); }
            catch (Exception error)
            {
                TryLogException("Turn-based first-tick decision failed; the request is refused", error);
                refusal = "decision-failed(" + error.GetType().Name + ")";
            }
            if (refusal == null)
            {
                string failure = null;
                bool committed;
                try { committed = !p.Replacement || CommitProvisionalOrder(p, out failure); }
                catch (Exception error) { committed = false; failure = "commit-failed(" + error.GetType().Name + ")"; }
                if (committed)
                {
                    p.State = MountedFirstTickProposalState.Committed;
                    p.Retired = true;
                    unifiedTurn.BindMountFirstTickGate(p, false);
                    if (ReferenceEquals(firstTickProposal, p)) firstTickProposal = null;
                    // R14-F4 (round 15): registered from the effect flags (an initial application that threw keeps its work owned).
                    if (p.EffectsOutstanding && !settlingProposals.Contains(p)) settlingProposals.Add(p);
                    // R10-F3 (round 11): the first child of this order still needs the rider's current entitlement when it starts.
                    firstChildEntitlement = p;
                    FirstTickCommitCount++;
                    approvedTick = inPlace ? 0f : granted;
                    detail = LastFirstTickDecision = "committed:" + (p.Replacement ? "replacement:" : "order:") + p.Detail + ";approvedTick=" + approvedTick.ToString("R") +
                        ";grantedMetres=" + (approvedTick == 0f ? 0f : approvedTick * speed).ToString("R") +
                        ";effects=" + p.DescribeEffects() + tick;
                    TryLog(() => "Turn-based first-tick decision committed: " + LastFirstTickDecision + ".");
                    return inPlace ? MountedFirstTickVerdict.CommitInPlace : MountedFirstTickVerdict.Commit;
                }
                refusal = failure ?? "commit-not-accepted";
            }
            p.State = MountedFirstTickProposalState.Refused;
            p.Detail = refusal;
            FirstTickRefusalCount++;
            detail = LastFirstTickDecision = "refused:" + refusal + tick;
            LastStockAttackObservation = "first-tick-proposal-refused;" + refusal;
            LastFeedback = "Mounted attack not started: the mount cannot reach a legal strike position with its current movement.";
            TryLog(() => "Turn-based first-tick decision refused before any debit: " + LastFirstTickDecision +
                (p.Replacement ? "; the previous order is unchanged." : "; the order ends without movement or expenditure."));
            return MountedFirstTickVerdict.Refuse;
        }

        private static bool SamePath(IList<Vector3> a, IList<Vector3> b)
        {
            if (a == null || b == null || a.Count != b.Count) return false;
            for (var i = 0; i < a.Count; i++) if (Kingmaker.Utility.GeometryUtils.MechanicsDistance(a[i], b[i]) > PairedAttackClickProbe.WaypointPassedMetres) return false;
            return true;
        }

        // R8-F2: live work on the mount other than the carrier that the existing accepted-work rule classifies as competing (compatible Free/Swift
        // work, reactions and the order's own work coexist).
        private string CompetingMountWork(UnitCommand except)
        {
            var commands = relationship.Mount?.Commands;
            if (commands == null) return "mount-commands-unavailable";
            var live = new List<UnitCommand>();
            foreach (var command in commands.Raw) if (command != null && !command.IsFinished && !ReferenceEquals(command, except)) live.Add(command);
            foreach (var command in commands.Queue) if (command != null && !command.IsFinished && !ReferenceEquals(command, except)) live.Add(command);
            foreach (var command in live)
            {
                var facts = new MountedAcceptedWorkFacts
                {
                    PairCommand = command is MountedPairSingleAttack,
                    Reaction = command is UnitAttackOfOpportunity,
                    AiOrigin = command.AiAction != null,
                    PlayerOrder = IsPairPlayerOrder(command),
                    FreeOrSwift = command.Type == UnitCommand.CommandType.Free || command.Type == UnitCommand.CommandType.Swift,
                    NeedsApproach = CommandNeedsApproach(command)
                };
                var kind = MountedRetainedOrderLedger.Classify(retainedOrder.IsRetainedOwn(command), facts);
                if (kind == MountedAcceptedWorkClass.Competing || kind == MountedAcceptedWorkClass.PairCommand) return DescribeCommand(command);
            }
            return null;
        }

        // The decision's predicates and its geometry, in order; null admits (p.Detail then describes the commit).
        // Round 17 (R16-F4): allowedTick is the permitted duration and speed the captured MaxSpeed; granted is the approved DURATION.
        private string EvaluateFirstTickDecision(FirstTickProposal p, object movementContext, float allowedTick, float deltaTime, float speed, float stepTravelLeft, bool inPlaceOnly,
            out bool inPlace, out float granted)
        {
            inPlace = false; granted = 0f;
            var rider = relationship.Rider;
            var mount = relationship.Mount;
            var turn = Game.Instance?.TurnBasedCombatController?.CurrentTurn;
            if (disposed || rider == null || mount == null || relationship.State != RelationshipState.Mounted) return "pair-unavailable";
            if (!ReferenceEquals(turn, p.Turn)) return "turn-changed";
            if (!unifiedTurn.CanAddressActor(mount, turn)) return "mount-not-addressable";
            if (p.Command == null || p.Command.IsFinished || !ReferenceEquals(activeCommand, p.Command) || !ReferenceEquals(rider.Commands.Standard, p.Command))
                return "order-not-active";
            if (p.Superseded != null) return "superseded-by-newer-work(" + p.Superseded + ")";
            if (!p.Replacement && retainedOrder.Superseded(p.CommittedGeneration)) return "superseded-by-newer-work(order generation " + p.CommittedGeneration + ")";
            var carrier = p.CarrierMove;
            if (!inPlaceOnly && (carrier == null || carrier.IsFinished || !ReferenceEquals(mount.Commands?.GetCommand(UnitCommand.CommandType.Move), carrier)))
                return "carrier-not-held";
            var competing = CompetingMountWork(inPlaceOnly ? null : carrier);
            if (competing != null) return "newer-mount-work(" + competing + ")";
            if (!FirstTickTargetValid(p)) return "target-invalid-under-click-facts";
            var livePlan = p.Command.LivePlanDisagreement();
            if (livePlan != null) return "plan-changed(" + livePlan + ")";
            if (!FirstTickRiderEntitled(p)) return "rider-not-entitled";
            var here = PairedAttackClickProbe.EvaluateFirstTick(rider, mount, p.Target, null, -1, 0f, 0f);
            if (p.Command.ClickPlan.Valid && !here.Plan.Matches(p.Command.ClickPlan)) return "plan-changed(forecast)";
            if (here.AdmissibleNow) { inPlace = true; p.Detail = "in-range"; return null; }
            if (inPlaceOnly) return "no-strike-in-place";
            var input = movementContext as TurnController;
            if (input == null) return "movement-context-unavailable";
            bool step, single;
            string mode;
            var grant = unifiedTurn.PairedMountGrantedMetres(turn, out step, out single, out mode, input);
            if (mode != "single-move-first-tick" && mode != "granted") return "mount-cannot-move(" + mode + ")";
            var agent = mount.View?.AgentASP;
            List<Vector3> agentPath; int cursor; Vector2 waypoint;
            if (!MountedAgentSteering.TryRead(agent, out agentPath, out cursor, out waypoint)) return "agent-not-steering";
            if (!SamePath(agentPath, carrier.ForcedPath?.vectorPath)) return "agent-path-not-carrier";
            if (cursor < 0 || cursor >= agentPath.Count ||
                Kingmaker.Utility.GeometryUtils.MechanicsDistance(new Vector3(waypoint.x, agentPath[cursor].y, waypoint.y), agentPath[cursor]) > PairedAttackClickProbe.WaypointPassedMetres)
                return "agent-cursor-inconsistent(" + cursor + ")";
            if (!(speed > 0f) || !(allowedTick >= 0f)) return "agent-speed-unreadable";
            // Round 13 (owner amendment 2026-11-27g, sections 3-5): only a genuinely single-tick allowance is decided on its executable stop before
            // the debit: the restricted single tick, an ORDINARY (time-limited) grant this tick exhausts, and a STEP whose native counter this tick
            // makes final (R12-F1: (1.524 - MetresStepped) / CurrentSpeedMps binds and fits this tick, so no later debit can follow whatever the
            // steered displacement). A step whose physical reservation binds instead keeps its route checks: the reservation is settled to the
            // executed displacement and another lawful debit remains. Any longer allowance is a lawful attempt under the native rules (installed
            // TickMovement clamps every later tick to the remaining time; an approach that exhausts it waits or is force-finished by native,
            // spending no Standard): the common remaining-route and arrival checks below apply whatever the grant size, with no forecast of
            // later frame durations.
            var ordinaryTime = mode == "granted" && !step;
            var finalTick = ordinaryTime && grant <= deltaTime * speed;
            var finalStep = mode == "granted" && step && unifiedTurn.PairedMountStepCounterFinal(deltaTime);
            // PR #70 round 16 (R15-F3): the common live route and arrival check (EvaluateRemainingRoute: the carrier's revalidated native route,
            // the live leg and every route position inside the full arrival radius) applies to every ordinary grant and every step, whatever the
            // grant size or counter state: the final ordinary tick and the step whose native counter this tick makes final run it before their
            // executable stop is accepted, exactly as the larger ordinary grant and the non-final step do below, so the same blind arrival path is
            // refused on either side of those branches. Their executable-stop decision (a lawful shortened stop included) follows unchanged. The
            // restricted single-move first tick is different: its only movement is this tick's stop under the single-action restriction (no
            // later tick follows to complete the route), so that executable stop alone is its admission.
            PairedAttackClickProbe.Result route = null;
            var approach = p.Command.ApproachOf(carrier);
            if (ordinaryTime || finalStep)
            {
                route = PairedAttackClickProbe.EvaluateRemainingRoute(rider, mount, p.Target, agentPath, cursor, carrier.ApproachRadius,
                    approach?.NativeRoute, approach?.DestinationMetres ?? float.NaN);
                if (p.Command.ClickPlan.Valid && !route.Plan.Matches(p.Command.ClickPlan)) return "plan-changed(forecast)";
                if (!route.RouteProven) return (finalTick ? "final-ordinary-tick-" : finalStep ? "final-step-counter-" : "") + "route-arrival-not-admitted(" + route.Detail + ")";
            }
            if (mode == "single-move-first-tick" || finalTick || finalStep)
            {
                // R8-F6 (round 10): the installed combat steering rule at the delta that will be returned (PairedAttackClickProbe.EvaluateFirstTickNative).
                Vector2 heading; float combatAngular, agentSpeed; bool followPrecomputed, forceMode, inCombat;
                if (!MountedAgentSteering.TryReadSteering(agent, out heading, out combatAngular, out agentSpeed, out followPrecomputed, out forceMode, out inCombat))
                    return "agent-steering-unreadable";
                if (forceMode || !inCombat || !(combatAngular > 0f)) return "agent-steering-unsupported(force=" + forceMode + ";combat=" + inCombat + ")";
                var probe = PairedAttackClickProbe.EvaluateFirstTickNative(rider, mount, p.Target, agentPath, cursor, allowedTick, speed, heading, combatAngular,
                    agentSpeed, followPrecomputed, stepTravelLeft);
                if (p.Command.ClickPlan.Valid && !probe.Plan.Matches(p.Command.ClickPlan)) return "plan-changed(forecast)";
                var label = finalTick ? "final-ordinary-tick" : finalStep ? "final-step-counter" : null;
                if (!probe.RouteProven) return (label != null ? label + "-" : "") + "no-affordable-strike-position(" + probe.Detail + ";grant=" + grant.ToString("R") + ")";
                granted = probe.Duration;
                p.Detail = (label != null ? label + ":" : "") + probe.Detail + ";speed=" + speed.ToString("R") + (label != null ? ";grant=" + grant.ToString("R") : "") +
                    (route != null ? ";route=" + route.Detail : "");
                return null;
            }
            var physicalGrant = grant;
            // Round 11 (R8-F1): the carrier's own revalidated native route and destination arc length, evaluated with the full arrival radius
            // (already evaluated above for an ordinary grant; a step whose native counter is not final is evaluated here).
            if (route == null)
            {
                route = PairedAttackClickProbe.EvaluateRemainingRoute(rider, mount, p.Target, agentPath, cursor, carrier.ApproachRadius,
                    approach?.NativeRoute, approach?.DestinationMetres ?? float.NaN);
                if (p.Command.ClickPlan.Valid && !route.Plan.Matches(p.Command.ClickPlan)) return "plan-changed(forecast)";
                if (!route.RouteProven) return "route-arrival-not-admitted(" + route.Detail + ")";
            }
            // R12-F2 (round 14): when a shortened stop within THIS tick (the installed steering at the returned delta, as for the single tick)
            // already admits the strike, that executable stop is carried through the debit (the tick is shortened to it); otherwise the lawful
            // ordinary route proceeds with the full tick under the amended admission rule. Round 17 (R16-F2): this executable immediate strike
            // is tested once the live route and arrival geometry above has passed and BEFORE the cost of the full destination is compared with
            // the grant: an affordable strike within the executable tick is not refused because the carrier's destination costs more.
            Vector2 headingS; float angularS, agentSpeedS; bool followS, forceS, combatS;
            if (allowedTick > 0f && MountedAgentSteering.TryReadSteering(agent, out headingS, out angularS, out agentSpeedS, out followS, out forceS, out combatS) && !forceS && combatS &&
                angularS > 0f)
            {
                var shortened = PairedAttackClickProbe.EvaluateFirstTickNative(rider, mount, p.Target, agentPath, cursor, allowedTick, speed, headingS, angularS, agentSpeedS, followS,
                    stepTravelLeft);
                if (shortened.RouteProven && (!p.Command.ClickPlan.Valid || shortened.Plan.Matches(p.Command.ClickPlan)))
                {
                    granted = shortened.Duration;
                    p.Detail = "ordinary-movement-shortened-strike(" + shortened.Detail + ";route=" + route.Detail + ";grant=" + physicalGrant.ToString("R") + ")";
                    return null;
                }
            }
            if (!PairedAttackClickAdmissionPolicy.RouteWithinGrant(true, route.RouteMetres, physicalGrant))
                return "route-beyond-current-grant(routeMetres=" + route.RouteMetres.ToString("R") + ";grant=" + physicalGrant.ToString("R") + ")";
            // Round 16: the executable current tick must move (a step whose every positive duration would complete the path beyond its physical
            // remainder executes nothing); a moving order is never committed on a zero tick.
            if (!(allowedTick > 0f))
                return "no-executable-movement(allowedTick=" + allowedTick.ToString("R") + (float.IsPositiveInfinity(stepTravelLeft) ? "" : ";stepTravelLeft=" + stepTravelLeft.ToString("R")) + ")";
            granted = allowedTick;
            p.Detail = "ordinary-movement(" + route.Detail + ";grant=" + physicalGrant.ToString("R") + ")";
            return null;
        }

        // The commit of a provisional order: success is read from the actual accepted generation and owner, never from a routing result.
        // A failure before the new order owns its command and carrier restores the previous order exactly (CommitAcceptedProvisional).
        private bool CommitProvisionalOrder(FirstTickProposal p, out string failure)
        {
            failure = null;
            var priorGeneration = stockIntent.Generation;
            var priorIntent = HasStockAttackIntent;
            try
            {
                AcceptStockRequest(p.StockAttack, p.Principal, p.Target, p.Turn, false, relationship.Rider, relationship.Mount, null, p);
            }
            catch (Exception error)
            {
                TryLogException("Turn-based provisional order commit failed", error);
                failure = "commit-failed(" + error.GetType().Name + ")";
            }
            var accepted = HasStockAttackIntent && stockIntent.Generation != priorGeneration && stockIntent.Owns(p.Target, stockIntent.Generation) &&
                ReferenceEquals(stockIntentCommand, p.Command) && ReferenceEquals(activeCommand, p.Command) && retainedOrder.IsCurrent(stockIntent.Generation) &&
                !p.Command.IsFinished && !p.Command.ProvisionalOrder;
            if (accepted) { p.CommittedGeneration = stockIntent.Generation; failure = null; return true; }
            if (failure == null) failure = "commit-not-accepted";
            failure += ";previousIntent=" + (priorIntent ? (stockIntent.Generation == priorGeneration && HasStockAttackIntent ? "kept" : "lost") : "none") +
                ";generation=" + priorGeneration + "->" + stockIntent.Generation;
            return false;
        }

        // A provisional order reached its strike start without a decided tick (no carrier was needed, or the target came into reach).
        // R9-F2 (round 10): every first-tick request of this command (provisional or accepted) is decided by the current in-place admission
        // (the rider's entitlement included) before the initial child attack starts; a refused one blocks it.
        private bool AdmitProvisionalStrike(MountedPairAttackCommand command)
        {
            var p = firstTickProposal;
            if (p == null || !ReferenceEquals(p.Command, command))
            {
                // R10-F3 (round 11), R11-F6 (round 12): every accepted click-bound rider order (this gate is attached from its dispatch) checks the
                // rider's current entitlement immediately before its first child starts, whether a movement gate committed, a carrier was deferred
                // behind older work, or no carrier was ever needed; StartChildAttack asks only before the first child, so an already released
                // strike keeps its native handling.
                if (command.ProvisionalOrder) return false;
                var committed = firstChildEntitlement;
                if (committed != null && ReferenceEquals(committed.Command, command)) firstChildEntitlement = null;
                bool entitled;
                evaluatingFirstTickCommand = command;
                try { entitled = RiderMeleeClickAdmitted(committed != null && ReferenceEquals(committed.Command, command) ? committed.Target : command.TargetUnit); }
                catch (Exception error) { TryLogException("Turn-based first child entitlement check failed; the strike is refused", error); entitled = false; }
                finally { evaluatingFirstTickCommand = null; }
                if (!entitled)
                {
                    FirstChildEntitlementRefusalCount++;
                    LastFirstTickDecision = "refused-at-first-child:rider-not-entitled;generation=" + (committed != null && ReferenceEquals(committed.Command, command) ? committed.CommittedGeneration : stockIntent.Generation);
                    LastStockAttackObservation = "first-child-refused;rider-not-entitled";
                    return false;
                }
                return true;
            }
            if (p.State == MountedFirstTickProposalState.Committed) return true;
            if (p.State != MountedFirstTickProposalState.Pending) return false;
            string detail;
            float granted;
            return DecideFirstTickProposal(p, null, 0f, 0f, float.NaN, float.PositiveInfinity, true, out granted, out detail) != MountedFirstTickVerdict.Refuse;
        }

        // R8-F4 (round 9): the command released its own carrier because its strike became admissible where the pair stands (TickApproach). A
        // pending provisional order is decided in place there; an accepted order's gate is settled as a stationary strike (nothing moved), so
        // the sweep never treats that release as carrier loss.
        private void CarrierReleasedForStrike(MountedPairAttackCommand command)
        {
            var p = firstTickProposal;
            if (p == null || !ReferenceEquals(p.Command, command) || p.State != MountedFirstTickProposalState.Pending) return;
            AdmitProvisionalStrike(command);
        }

        // R8-F3 (round 9): a command whose request was refused issues no further carrier (no Run, so no Hold write and no accepted work).
        private bool FirstTickCarrierPermitted(MountedPairAttackCommand command)
        {
            var p = firstTickProposal;
            return p == null || !ReferenceEquals(p.Command, command) || p.State != MountedFirstTickProposalState.Refused;
        }

        private void RefuseFirstTickProposal(string why)
        {
            var p = firstTickProposal;
            if (p == null || p.State != MountedFirstTickProposalState.Pending) return;
            p.State = MountedFirstTickProposalState.Refused;
            p.Detail = why;
            FirstTickRefusalCount++;
            LastFirstTickDecision = "refused:" + why;
        }

        // A newer click retires only a pending provisional order (an accepted order's gate stays with its carrier: a same-target continuation
        // keeps that carrier, and a replacement retires the order through CancelCore, which releases the gate).
        private void RetirePendingReplacement(string why)
        {
            if (firstTickProposal != null && firstTickProposal.Replacement) RetireFirstTickProposal(why);
        }

        // A cancellation retires a pending provisional order and its exact carrier; an accepted order's gate is only released (the order's own
        // retirement handles its wrapper).
        private void RetireFirstTickProposal(string why)
        {
            var p = firstTickProposal;
            if (p == null) return;
            if (!p.Replacement)
            {
                unifiedTurn.BindMountFirstTickGate(p, false);
                if (p.State == MountedFirstTickProposalState.Pending) { p.State = MountedFirstTickProposalState.Refused; p.Detail = why; }
                p.Retired = true;
                firstTickProposal = null;
                return;
            }
            RefuseFirstTickProposal(why);
            SweepFirstTickProposal();
        }

        // Settles and retires the request outside the movement tick (the controller's per-frame drive). A pending request whose command, carrier,
        // turn or target ended is refused; a refused request's exact command and carrier are ended (only if they are still the request's own and
        // still held); a committed request needs nothing (its order owns command and carrier). Idempotent: a retired request is never revived.
        internal void SweepFirstTickProposal()
        {
            SettleOutstandingEffects();
            SettleRetainedCarrierCleanups();
            var p = firstTickProposal;
            if (p == null) return;
            if (p.State == MountedFirstTickProposalState.Pending)
            {
                var mount = relationship.Mount;
                string why = null;
                if (p.Command == null || p.Command.IsFinished) why = "order-ended-before-first-tick";
                else if (p.Replacement && !ReferenceEquals(activeCommand, p.Command)) why = "order-replaced";
                else if (!ReferenceEquals(Game.Instance?.TurnBasedCombatController?.CurrentTurn, p.Turn)) why = "turn-changed";
                else if (!FirstTickTargetValid(p)) why = "target-invalid-under-click-facts";
                else if (p.CarrierMove != null && (p.CarrierMove.IsFinished || mount == null ||
                    !ReferenceEquals(mount.Commands?.GetCommand(UnitCommand.CommandType.Move), p.CarrierMove))) why = "carrier-lost-before-first-tick";
                if (why == null) return;
                RefuseFirstTickProposal(why);
            }
            if (p.State == MountedFirstTickProposalState.Refused && !p.Retired)
            {
                // R6-F7 (rounds 9-11): exact cleanup obligations, tracked independently of native Result/IsFinished, each done once: the command's
                // terminal interruption (never replayed once native stamped it), then the exact carrier's cleanup record (interruption, removal
                // from the mount's Move slot, motion stop unless a successor steers the mount). A failure keeps the request owned (its gate keeps
                // refusing) and the next sweep retries only what is unfinished.
                var command = p.Command;
                var carrier = p.CarrierMove;
                var commands = relationship.Mount?.Commands;
                try
                {
                    if (!p.CleanupCommandDone)
                    {
                        if (command != null && command.Result == UnitCommand.ResultType.None && !command.IsFinished &&
                            (p.Replacement ? !ReferenceEquals(stockIntentCommand, command) : ReferenceEquals(stockIntentCommand, command)))
                        {
                            try
                            {
                                command.MarkOrderRetired();
                                InterruptOwnedCommand(command, MountedAttackEndCause.Cancelled, "first-tick decision refused: " + p.Detail);
                            }
                            finally { if (command.Result != UnitCommand.ResultType.None || command.IsFinished) { p.CleanupCommandDone = true; FirstTickRetiredCommandCount++; } }
                        }
                        p.CleanupCommandDone = true;
                    }
                    // R6-F7 (round 11): the exact carrier's interruption, slot removal and motion are the command's own cleanup record
                    // (MountedPairAttackCommand.SettleCarrierCleanup), begun by whichever caller ended it first (native interruption's OnEnded,
                    // the order's release, this sweep); only its unfinished steps run here, with the same Move-slot and other-slot successor checks
                    // as the normal release (a successor promoted from the queue by the slot removal included).
                    if (!p.CleanupCarrierDone)
                    {
                        if (carrier == null || command == null) p.CleanupCarrierDone = true;
                        else p.CleanupCarrierDone = command.SettleCarrierCleanup(carrier);
                    }
                    p.CleanupSlotDone = p.CleanupMotionDone = p.CleanupCarrierDone;
                    if (!p.CleanupCarrierDone) throw new InvalidOperationException("carrier cleanup unsettled: " + (command?.CarrierCleanupState ?? "none"));
                }
                catch (Exception error)
                {
                    p.CleanupFaulted = true;
                    FirstTickCleanupFailureCount++;
                    TryLogException("Refused first-tick request cleanup failed; its unfinished obligations stay owned and are retried", error);
                    return;
                }
                p.Retired = true;
                if (command != null && command.IsFinished && ReferenceEquals(activeCommand, command)) activeCommand = null;
            }
            unifiedTurn.BindMountFirstTickGate(p, false);
            if (ReferenceEquals(firstTickProposal, p)) firstTickProposal = null;
        }

        // PR #70 R6-F1, R7-F3 (round 8): work native accepted (slot, queue or promotion) on either participant while a provisional order is pending.
        // An installed player command that wrote the rider's manual target is the player's newer assignment (kept at the commit, the same target
        // included). Competing work by the existing classification supersedes the unreleased provisional order: it is refused now (its gate refuses
        // any tick) and the next sweep ends only its own command and carrier; coexisting work and successors keep their normal treatment.
        private void ObserveWorkAgainstProvisional(UnitCommands container, UnitCommand command, MountedAcceptedWorkRoute route)
        {
            var p = firstTickProposal;
            var onRider = container == relationship.Rider?.Commands;
            if (p == null || !onRider && container != relationship.Mount?.Commands) return;
            if (onRider && route != MountedAcceptedWorkRoute.Queue && !(command is MountedPairSingleAttack) && container.Contains(command) &&
                command.MarkManualTarget && command.AiAction == null && command.TargetUnit != null &&
                ReferenceEquals(relationship.Rider.CombatState?.ManualTarget, command.TargetUnit)) p.NewerTargetObserved = true;
            var facts = new MountedAcceptedWorkFacts
            {
                PairCommand = command is MountedPairSingleAttack,
                Reaction = command is UnitAttackOfOpportunity,
                AiOrigin = command.AiAction != null,
                PlayerOrder = IsPairPlayerOrder(command),
                FreeOrSwift = command.Type == UnitCommand.CommandType.Free || command.Type == UnitCommand.CommandType.Swift,
                NeedsApproach = CommandNeedsApproach(command)
            };
            if (route == MountedAcceptedWorkRoute.Promotion || MountedRetainedOrderLedger.Classify(false, facts) != MountedAcceptedWorkClass.Competing) return;
            p.Superseded = (onRider ? "rider:" : "mount:") + DescribeCommand(command);
            RefuseFirstTickProposal("superseded-by-newer-work(" + p.Superseded + ")");
        }

        // R6-F1: the click's own post-Run manual-target assignment for a pending provisional order of this exact click is deferred to the
        // order's acceptance (MountedPatchController.ClickManualTargetAssignment).
        // A provisional request this same click refused (at its installation) leaves no target either: the click is not an accepted order.
        internal bool DefersClickTargetAssignment(Kingmaker.Controllers.Combat.UnitCombatState combatState, UnitEntityData target)
        {
            var p = LastFirstTickProposal;
            if (disposed || p == null || !p.Replacement || activeClick < 0 || p.ClickSerial != activeClick || relationship.Rider == null ||
                !ReferenceEquals(relationship.Rider.CombatState, combatState) || !ReferenceEquals(target, p.Target) ||
                p.State == MountedFirstTickProposalState.Committed) return false;
            if (!p.TargetWriteDeferred) { p.TargetWriteDeferred = true; DeferredClickTargetWriteCount++; }
            return true;
        }

        // Review R4-F1: the proof for a carrier of a click-admitted turn-based order, recomputed from the CURRENT origin with the
        // CURRENT grant by the same production walk the click used; null when the proof cannot be established. Reviews R6-F1,
        // R7-F1: the proof carries the verified destination and its proven native arrival radius; the destination established
        // at planning is re-examined on the current route.
        // PR #70 R1-F9: prediction, revalidation and dispatch must agree on the plan (executor, ordinary mode, first planned weapon,
        // reach); any difference ends the proof.
        private PairedAttackClickProbe.Approach RevalidateAdmittedClickRoute(UnitEntityData target, Vector3? established,
            PairedAttackClickProbe.PlanIdentity clickPlan, PairedAttackClickProbe.PlanIdentity dispatchPlan)
        {
            var rider = relationship.Rider;
            var mount = relationship.Mount;
            var turn = Game.Instance?.TurnBasedCombatController?.CurrentTurn;
            if (disposed || rider == null || mount == null || target == null || turn == null) return null;
            bool step, single;
            string reason;
            var grant = unifiedTurn.PairedMountGrantedMetres(turn, out step, out single, out reason);
            var probe = PairedAttackClickProbe.Evaluate(rider, mount, target, grant, established);
            LastRoutePlans = "click{" + clickPlan + "};revalidation{" + probe.Plan + "};dispatch{" + dispatchPlan + "}";
            if (!probe.Plan.Matches(clickPlan) || !dispatchPlan.Matches(clickPlan))
            {
                LastRouteRevalidation = "proven=False;plan-changed;" + LastRoutePlans;
                TryLog(() => "Turn-based admitted approach refused at revalidation: " + LastRouteRevalidation + ".");
                return null;
            }
            if (probe.AdmissibleNow)
            {
                LastRouteRevalidation = "proven=True;in-range;" + LastRoutePlans;
                return new PairedAttackClickProbe.Approach { Route = new List<Vector3> { mount.Position, mount.Position }, ArrivalRadius = PairedAttackClickProbe.ArrivalRadii[0], Plan = probe.Plan };
            }
            // PR #70 R6-F4 (round 7): in the first-tick state the grant is only the route-search ceiling; the route it proves is executable only
            // through the first-tick decision of the carrier made from it (Approach.FirstTick -> BindFirstTickCarrier), never as a grant.
            var firstTick = reason == "single-move-first-tick";
            var proven = PairedAttackClickAdmissionPolicy.RouteWithinGrant(probe.RouteProven, probe.RouteMetres, grant);
            LastRouteRevalidation = "proven=" + proven + ";routeMetres=" + probe.RouteMetres.ToString("R") + ";grant=" + grant.ToString("R") +
                ";mountGrant=" + reason + ";firstTick=" + firstTick + ";probe=" + probe.Detail + ";" + LastRoutePlans;
            TryLog(() => "Turn-based admitted approach revalidated from the current origin: " + LastRouteRevalidation + ".");
            // PR #70 R8-F1 (round 11): the untruncated native route and the destination's arc length on it go with the carrier, for its
            // first-debit revalidation on these same points.
            return proven ? new PairedAttackClickProbe.Approach { Route = probe.Route, ArrivalRadius = probe.ArrivalRadius, Plan = probe.Plan, FirstTick = firstTick,
                NativeRoute = probe.NativeRoute, DestinationMetres = probe.DestinationMetres } : null;
        }

        internal string LastRouteRevalidation { get; private set; }
        internal string LastRoutePlans { get; private set; }

        internal void ObserveHoldInput()
        {
            if (!IsPairSelected()) return;
            var selected = SelectionManager.Instance?.SelectedUnits;
            mountHold.ObservePlayerHoldInput(UnifiedMountedStockAttackPolicy.ContainsExactPrincipal(selected, relationship.Mount));
        }

        public void Cancel(string reason) => CancelCore(reason, false);

        // PR-1 review R4-F1: ordinaryRetarget is a fresh player order replacing the current one (not a hard lifecycle boundary): a
        // command whose strike was already delivered stays native's until it completes; every other boundary keeps its hard cleanup.
        private void CancelCore(string reason, bool ordinaryRetarget, bool modDisableYieldsSuccessor = false)
        {
            if (disposed)
            {
                return;
            }
            if (chargeOwner != null && !TryDrainChargeOwnership(reason))
                throw new InvalidOperationException("Mounted charge cleanup remains owned: " + ChargeCleanupStatus);
            // PR #70 R1-F6 (rounds 6-9): a pending provisional order ends refused with its exact command and carrier (a commit never runs
            // CancelCore since round 9).
            RetireFirstTickProposal("cancelled: " + reason);
            RetireOrderForCancel(reason, ordinaryRetarget);
            ClearObservedStockRequest();
            ArmedAction = MountedCombatActionKind.None;
            ClearProjectedNativeMountTurnGroundMove();
            overlayWorldInputGuard.Clear();
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
            // PR #70 R12-F4 (round 16): at mod disable the player's admitted ground order (native's own command) is released from this pair's
            // accounting without being interrupted.
            if (modDisableYieldsSuccessor) ReleaseRiderTurnGroundMovementForModDisable();
            else CancelRiderTurnGroundMovement();
            SweepFinishedCommand();
            // PR-1 review R4-F1: CancelMountMovement interrupts the mount's IsMoveUnit commands, and native TickApproaching sets that
            // flag on a horse Standard attack that approached. On ordinary retargeting a delivered mount command stays native's (the
            // retired order's own movement was released with its wrappers); every hard boundary keeps the mount-wide interrupt.
            if (!(ordinaryRetarget && MountCommandLeftToNative()))
            {
                if (modDisableYieldsSuccessor) relationship.Runtime.CancelMountMovement(CleanupTrigger.ModDisabled);
                else relationship.Runtime.CancelMountMovement();
            }
            LastFeedback = "Mounted combat cancelled: " + (string.IsNullOrWhiteSpace(reason) ? "boundary" : reason) + ".";
        }

        private bool MountCommandLeftToNative() => activeCommand != null && !activeCommand.IsFinished && activeCommand.StrikeReleased &&
            ReferenceEquals(activeCommand.ActionActor, relationship.Mount);

        // PR-1 reviews R3-F1, R4-F1: the order part of a cancellation. The exact wrappers (the active one and the order's own, which may
        // be queue-only) are retained before the intent is cleared and retired through RetireOrderWrapper: a queue-only wrapper is
        // removed and marked so no late promotion installs it; at a hard boundary every unfinished wrapper is ended; on ordinary
        // retargeting a delivered strike is left to native completion (the active reference is kept until its terminal).
        private void RetireOrderForCancel(string reason, bool ordinaryRetarget)
        {
            var orderCommand = stockIntentCommand;
            ClearStockAttackIntent(reason, true);
            var command = activeCommand;
            if (command != null && !command.ChargeMode)
            {
                RetireOrderWrapper(command, reason, !ordinaryRetarget);
                if (command.IsFinished && ReferenceEquals(activeCommand, command)) activeCommand = null;
            }
            if (orderCommand != null && !ReferenceEquals(orderCommand, command)) RetireOrderWrapper(orderCommand, reason, !ordinaryRetarget);
        }

        // PR-1 reviews R3-F1, R5-F1: an observed target invalidation retires the generation and its exact wrapper, installed or queued,
        // even if the target's eligibility later returns; a strike already delivered keeps its native terminal handling.
        // PR-1 review R6-F1: an unreleased wrapper stopped here records the same cause (TargetInvalid) and bounded target reason it records
        // when it observes the loss first (MountedPairAttackCommand.RecordTargetInvalidation; the first recorded cause stands), so
        // controller-first and wrapper-first loss end alike. A delivered strike is left to its native terminal handling.
        private void RetireOrderOnTargetInvalidation()
        {
            const string reason = "target invalidation; native child owns terminal outcome";
            var command = stockIntentCommand;
            if (command != null && !command.IsFinished && !command.StrikeReleased) command.RecordTargetInvalidation();
            ClearStockAttackIntent(reason, true);
            RetireOrderWrapper(command, reason);
        }

        // PR-1 review R5-F2: invalidation is scoped to the order's own generation, also while an earlier generation's delivered command
        // still occupies the active slot (that command completes against its own target).
        private bool RetireOrderIfTargetInvalid(bool targetValid)
        {
            if (targetValid || !HasActiveCommand && (stockIntentCommand == null || stockIntentCommand.IsFinished)) return false;
            RetireOrderOnTargetInvalidation();
            return true;
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
            // PR #70 R1-F7: the click's own handling takes the click's proof once, whatever its disposition.
            var clickProof = playerCommand ? TakeValidatedClickProof(target, turn) : null;
            if (!turnBased || ownerIsMount) clickProof = null;
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

            // PR #70 R1-F6 (rounds 6-7): a click proven on the restricted single move's one tick that would replace the current state is a
            // provisional order on the rider; nothing of the previous order, manual target or Hold is touched here (BeginProvisionalOrder; its
            // carrier's first tick commits through AcceptStockRequest). A click continuing the retained order on the same target is the
            // normal acceptance; its carrier is decided at its first tick as well (BindFirstTickCarrier).
            if (turnBased && !ownerIsMount && clickProof != null && clickProof.FirstTick && !(HasStockAttackIntent && ReferenceEquals(target, stockIntentTarget)))
            {
                BeginProvisionalOrder(stockAttack, principal, target, turn, clickProof);
                return false;
            }
            return AcceptStockRequest(stockAttack, principal, target, turn, ownerIsMount, rider, mount, clickProof);
        }

        // The accepted player request's normal path (the replacement of the previous order included); PR #70 R1-F6 (rounds 6-7): also the
        // commit of a provisional first-tick order (committing).
        private bool AcceptStockRequest(UnitAttack stockAttack, UnitEntityData principal, UnitEntityData target, TurnController turn, bool ownerIsMount,
            UnitEntityData rider, UnitEntityData mount, ClickProof clickProof, FirstTickProposal committing = null)
        {
            // PR #70 R6-F5 (round 7): every newer accepted click (the same-target continuation included) retires a pending provisional order and
            // its exact carrier before it is classified; the proposal's own commit is the only acceptance that keeps it.
            if (committing != null) return CommitAcceptedProvisional(principal, target, turn, rider, mount, committing);
            RetirePendingReplacement("a newer accepted click");
            // The native effect of a new order on the unit it was given to, which a routed click never reaches.
            ObserveDeliberateOrder();
            principal.HoldState = false;
            var actionContext = ownerIsMount ? MountedCombatActionKind.MountPrimaryNatural : ResolveRiderPrimaryAction();
            var weaponContext = principal.GetFirstWeapon();
            var mountWeaponContext = mount.GetFirstWeapon();
            // Native explicit-order rule: a click on an enemy that is already unconscious (downed, lying prone) is a valid
            // order on that enemy. A retained intent continues only if it was itself ordered on the downed enemy; a click
            // on a target that fell unconscious after an earlier order is a fresh order, as a new native UnitAttack is.
            var clickTargetState = target?.Descriptor?.State;
            var orderedWhileUnconscious = clickTargetState != null && clickTargetState.IsUnconscious;
            var click = target != null && target.IsInState && clickTargetState != null &&
                UnifiedMountedStockAttackPolicy.TargetLifeAdmits(clickTargetState.IsConscious, clickTargetState.IsDead,
                    clickTargetState.IsFinallyDead, orderedWhileUnconscious) &&
                (!orderedWhileUnconscious || stockIntent.TargetUnconsciousWhenOrdered) && principal.IsEnemy(target)
                ? ObserveStockClick(target, turn, ownerIsMount, rider, mount, weaponContext, mountWeaponContext, (int)actionContext)
                : MountedStockClick.NewRequest;
            if (click == MountedStockClick.Continue)
            {
                // Consume this exact pair request before UnitCommands.Run can replace its windup/approach.
                // The stock pointer loop still dispatches every unrelated selected party member.
                stockIntent.PromoteToPlayer();
                retainedOrder.PromoteToPlayer();
                stockIntentCommand?.PromoteToPlayerOrigin();
                automaticRuns.Clear();
                OwnManualTarget(principal, target, stockIntent.Generation);
                // PR #70 R1-F7: an undispatched order takes this click's proof (or none, for a native-accepted click); a live carrier
                // is kept and the click's proof is discarded.
                if (stockIntentCommand == null || stockIntentCommand.IsFinished) BindClickProof(clickProof, "continued-undispatched");
                else if (clickProof != null) { ClickProofDiscardCount++; LastClickProofDisposition = "discarded:live-carrier-kept"; }
                LastStockAttackObservation = "intent-continued;target=" + target.UniqueId;
                return false;
            }
            // Known issues Sprint 6 (A01): a same-target click after the dispatched command ended is a fresh request as well.
            CancelCore("new stock hostile attack", true);
            stockIntent.Begin(target, turn, ownerIsMount, IsPairInCombat(),
                rider, mount, weaponContext, mountWeaponContext, (int)actionContext,
                targetUnconsciousWhenOrdered: orderedWhileUnconscious);
            // Review R2-F1: the order's attack kind is fixed here, at acceptance, for its whole lifetime.
            retainedOrder.Begin(stockIntent.Generation, false,
                ownerIsMount || actionContext != MountedCombatActionKind.RiderRanged);
            BindClickProof(clickProof, "new-order");
            StockAttackIntentStartCount++;
            OwnManualTarget(principal, target, stockIntent.Generation);
            LastRejectionCodes = new MountedCombatRejectionCode[0];
            LastFeedback = "Mounted stock attack intent accepted: rider principal, " +
                MountDisplayName + " movement authority.";
            LastStockAttackObservation = "intent-start;target=" + target.UniqueId +
                ";ranged=" + (ResolveRiderPrimaryAction() == MountedCombatActionKind.RiderRanged) +
                ";createdByPlayer=" + stockAttack.CreatedByPlayer + ";principal=" + principal.UniqueId +
                ";turnBased=" + CombatController.IsInTurnBasedCombat() +
                ";targetUnconsciousWhenOrdered=" + stockIntent.TargetUnconsciousWhenOrdered;
            TryDriveStockAttackIntent();
            return false;
        }

        // PR #70 R1-F6 (round 7), R6-F7 (rounds 8-9): the provisional order's commit, in three phases.
        // 1. Staging: every fallible read and the native order effect, and the idle preconditions. A failure here changes nothing.
        // 2. Transfer (KMC-owned state only, no engine write): the previous intent, ledger, click proof and selection ownership are retired and
        //    intent B, its ledger, its exact command and carrier ownership are established. The previous state is captured first
        //    (MountedStateSnapshot); a failure before the new order owns its command and carrier restores it exactly (same generation, intent,
        //    ledger and ownership records) and the request is refused.
        // 3. Deferred effects (engine writes): the manual targets and both Holds. Each is applied once and contained; one that fails stays
        //    outstanding on the request (settlingProposals) and is retried by the sweep while B is still the current order, so success is never
        //    reported as complete before those writes finish.
        private bool CommitAcceptedProvisional(UnitEntityData principal, UnitEntityData target, TurnController turn, UnitEntityData rider, UnitEntityData mount,
            FirstTickProposal p)
        {
            var command = p.Command;
            if (command == null || command.IsFinished || !ReferenceEquals(activeCommand, command)) return false;
            // 1. Staging.
            if (chargeOwner != null || activeDoorInteraction != null && !activeDoorInteraction.IsFinished || pendingDoorSettlement != null ||
                HasLiveCarriedApproach || activeRiderTurnGroundMove != null && !activeRiderTurnGroundMove.IsFinished)
                throw new InvalidOperationException("other pair work is live at the commit");
            // PR #70 R14-F4 (round 15): the exact actors and their current manual targets are staged here with the other pre-transfer facts; nothing
            // after the transfer rereads them to fill the deferred-effect record.
            var riderTargetAtCommit = rider.CombatState?.ManualTarget;
            var mountTargetAtCommit = mount.CombatState?.ManualTarget;
            var newerTarget = p.NewerTargetObserved || !ReferenceEquals(riderTargetAtCommit, p.TargetBeforeClick);
            var actionContext = ResolveRiderPrimaryAction();
            var weaponContext = principal.GetFirstWeapon();
            var mountWeaponContext = mount.GetFirstWeapon();
            var inCombat = IsPairInCombat();
            var feedback = "Mounted stock attack intent accepted: rider principal, " + MountDisplayName + " movement authority.";
            var clearRiderHold = rider.HoldState && rider.HoldState == p.RiderHoldAtClick;
            var clearMountHold = p.MountHoldDeferred && mount.HoldState;
            var hadIntent = HasStockAttackIntent;
            var retiringGeneration = stockIntent.Generation;
            var riderTargetOwnedByRetiring = riderSelection.ClearsOnRetirement(retiringGeneration, riderTargetAtCommit);
            var mountTargetOwnedByRetiring = mountSelection.ClearsOnRetirement(retiringGeneration, mountTargetAtCommit);
            var carrier = p.CarrierMove ?? command.DelegatedMove;
            ObserveDeliberateOrder();
            // 2. Transfer.
            var snapshot = MountedStateSnapshot.Capture(stockIntent, retainedOrder, automaticRuns, riderSelection, mountSelection);
            var savedIntentCommand = stockIntentCommand; var savedProof = intentClickProof; var savedProofGeneration = intentClickProofGeneration;
            var savedPendingProof = pendingClickProof; var savedObservation = LastStockAttackObservation; var savedDisposition = LastClickProofDisposition;
            var savedCancelCount = StockAttackIntentCancelCount; var savedStartCount = StockAttackIntentStartCount; var savedDispatchCount = StockAttackRiderDispatchCount;
            var savedDiscards = ClickProofDiscardCount;
            long generation;
            try
            {
                riderSelection.Release();
                mountSelection.Release();
                if (hadIntent) StockAttackIntentCancelCount++;
                stockIntent.Cancel();
                retainedOrder.Clear();
                ClearObservedStockRequest();
                intentClickProof = null;
                intentClickProofGeneration = -1;
                pendingClickProof = null;
                stockIntentCommand = null;
                automaticRuns.Clear();
                stockIntent.Begin(target, turn, false, inCombat, rider, mount, weaponContext, mountWeaponContext, (int)actionContext,
                    targetUnconsciousWhenOrdered: p.TargetUnconsciousAtClick);
                generation = stockIntent.Generation;
                retainedOrder.Begin(generation, false, actionContext != MountedCombatActionKind.RiderRanged);
                BindClickProof(null, "first-tick-commit");
                StockAttackIntentStartCount++;
                retainedOrder.RecordOwnIssued(generation, command);
                if (carrier != null) retainedOrder.RecordOwnIssued(generation, carrier);
                stockIntentCommand = command;
                stockIntent.ObserveDispatch(false);
                StockAttackRiderDispatchCount++;
                var ownGeneration = generation;
                command.OwnCommandIssuing = issued => retainedOrder.RecordOwnIssued(ownGeneration, issued);
                command.RetainedOrderSupersededCheck = () => retainedOrder.Superseded(ownGeneration);
                command.CommitProvisionalOrder();
            }
            catch (Exception)
            {
                snapshot.Restore();
                stockIntentCommand = savedIntentCommand; intentClickProof = savedProof; intentClickProofGeneration = savedProofGeneration;
                pendingClickProof = savedPendingProof; LastStockAttackObservation = savedObservation; LastClickProofDisposition = savedDisposition;
                StockAttackIntentCancelCount = savedCancelCount; StockAttackIntentStartCount = savedStartCount; StockAttackRiderDispatchCount = savedDispatchCount;
                ClickProofDiscardCount = savedDiscards;
                command.OwnCommandIssuing = null;
                command.RetainedOrderSupersededCheck = null;
                command.MarkProvisionalOrder();
                activeCommand = command;
                throw;
            }
            // 3. Deferred effects, each once; a failure stays outstanding for the sweep.
            p.CommittedGeneration = generation;
            p.EffectRiderTarget = !newerTarget;
            p.EffectMountTargetClear = mountTargetOwnedByRetiring;
            p.MountTargetToClear = mountTargetOwnedByRetiring ? mountTargetAtCommit : null;
            p.MountToClear = mount;
            p.RiderToWrite = rider;
            p.EffectRiderHold = clearRiderHold;
            p.EffectMountHold = clearMountHold;
            if (newerTarget) riderSelection.Release();
            // R14-F4 (round 15): the initial application is contained like every retry; whatever it did not finish stays owned by its flags (the
            // first-tick decision registers the request from them), so a fault here never reports the effects complete or leaves them unowned.
            try { ApplyAcceptanceEffects(p); }
            catch (Exception error) { TryLogException("Accepted order's deferred effects failed on their first application; retained", error); }
            LastRejectionCodes = new MountedCombatRejectionCode[0];
            LastFeedback = feedback;
            LastStockAttackObservation = "intent-start;target=" + target.UniqueId + ";firstTickCommit=True;generation=" + generation +
                ";previousOwnedTarget=" + riderTargetOwnedByRetiring + ";newerTargetKept=" + newerTarget +
                ";targetUnconsciousWhenOrdered=" + stockIntent.TargetUnconsciousWhenOrdered +
                ";effects=" + p.DescribeEffects();
            return false;
        }

        // The accepted order's deferred engine writes, each applied once and contained; only while that order is still current.
        private void ApplyAcceptanceEffects(FirstTickProposal p)
        {
            p.OutstandingEffects.Clear();
            var current = HasStockAttackIntent && stockIntent.Generation == p.CommittedGeneration && stockIntent.Owns(p.Target, p.CommittedGeneration);
            // R10-F4 (round 11): B's own effects (its target write, the Hold clears) end with B; the clear of the RETIRED generation's mount target
            // (A's owned selection, released at the transfer) stays owned until it succeeds or an actual newer assignment on the mount supersedes it,
            // also after B retires; it clears only that target.
            if (!current) p.EffectRiderTarget = p.EffectRiderHold = p.EffectMountHold = false;
            var mountToClear = p.MountToClear ?? relationship.Mount;
            // R14-F4 (round 15): a fault in this supersession read ends the application with the list possibly empty; the flag keeps the clear
            // owned and the callers (the first application's containment, the sweep) retain the request from the flags.
            if (p.EffectMountTargetClear && (p.MountAssignedAfterCommit || mountToClear?.CombatState == null || !ReferenceEquals(mountToClear.CombatState.ManualTarget, p.MountTargetToClear)))
                p.EffectMountTargetClear = false;
            if (p.EffectMountTargetClear)
            {
                try { mountToClear.CombatState.ManualTarget = null; p.EffectMountTargetClear = false; }
                catch (Exception error) { TryLogException("Retired order's mount target clear failed; retried", error); p.OutstandingEffects.Add("mount-target"); }
            }
            if (!current) return;
            if (p.EffectRiderTarget)
            {
                if (p.NewerTargetObserved || p.RiderAssignedAfterCommit) p.EffectRiderTarget = false;
                else try { OwnManualTarget(p.Principal, p.Target, p.CommittedGeneration); p.EffectRiderTarget = false; }
                    catch (Exception error) { TryLogException("Accepted order's manual target write failed; retried", error); p.OutstandingEffects.Add("rider-target"); }
            }
            if (p.EffectRiderHold)
            {
                // R14-F4 (round 15): the exact actors staged at the commit (never re-read from the relationship).
                var riderToWrite = p.RiderToWrite ?? relationship.Rider;
                try { if (riderToWrite != null && riderToWrite.HoldState) riderToWrite.HoldState = false; p.EffectRiderHold = false; }
                catch (Exception error) { TryLogException("Accepted order's rider Hold write failed; retried", error); p.OutstandingEffects.Add("rider-hold"); }
            }
            if (p.EffectMountHold)
            {
                var mountToWrite = p.MountToClear ?? relationship.Mount;
                try { if (mountToWrite != null && mountToWrite.HoldState) mountToWrite.HoldState = false; p.EffectMountHold = false; }
                catch (Exception error) { TryLogException("Accepted order's mount Hold write failed; retried", error); p.OutstandingEffects.Add("mount-hold"); }
            }
        }

        internal long StockAttackFreshAfterTerminalCount { get; private set; }

        private bool StockIntentCommandLive => stockIntentCommand != null && !stockIntentCommand.IsFinished;

        // Known issues Sprint 6 (A01): an exact same-target click continues the retained intent only while its request has
        // not dispatched or its dispatched command is still live; otherwise it is a fresh request through the replacement path.
        // Review of 2ae4cc8b (finding 5): only that same request after its command ended is counted as fresh.
        private MountedStockClick ObserveStockClick(UnitEntityData target, TurnController turn, bool ownerIsMount, UnitEntityData rider,
            UnitEntityData mount, object weapon, object mountWeapon, int action)
        {
            var click = stockIntent.ClassifyClick(target, turn, ownerIsMount, rider, mount, weapon, mountWeapon, action, StockIntentCommandLive);
            if (click == MountedStockClick.FreshAfterTerminal) StockAttackFreshAfterTerminalCount++;
            return click;
        }

        // AI-01: the first mounted/unmounted divergence was this boundary. An unmounted unit's brain proposal runs
        // natively; here it was refused as an unobserved command, so a rider idled after its target died.
        private void ObserveAutomaticProposal(UnitAttack proposal, bool ownerIsRider, bool turnBased)
        {
            // PR-1 reviews R1-F9, R10-F4: a retained automatic order that is no longer admitted (target invalid, unperceived, or outside
            // native admission with the action's current range) is retired by the same decision before it can refuse this proposal.
            if (relationship.State == RelationshipState.Mounted) RetireAutomaticOrderIfNotAdmitted(null);
            AutomaticProposalCount++;
            // A move that arrived this frame holds the pair before this proposal is read, whichever controller ran first.
            ObservePlayerMoveOutcome();
            var rider = relationship.Rider;
            var mount = relationship.Mount;
            var target = proposal.Target;
            var targetState = target?.Descriptor?.State;
            var nativeProposed = target;
            // Review R1-F11: native's sort leaves equally ranked candidates (in reach, weight 0 or the last target's +1) in an
            // arbitrary order. Among the candidates native itself ranked equal to its proposal and that native would score
            // equally (conscious, not disliked, attackable, valid under Hold/unreachable), the pair takes the nearest; a live
            // automatic order already on one of them keeps it (no oscillation).
            // Review R5-F7: only the supported rider MELEE proposal; a mounted archer keeps the brain's choice.
            if (!turnBased && rider != null && target != null)
            {
                var tieBroken = ResolveEqualRankTie(rider, target, proposal);
                if (tieBroken != null && !ReferenceEquals(tieBroken, target))
                {
                    AutomaticTieBreakCount++;
                    LastAutomaticTieBreak = "proposed=" + target.UniqueId + ";nearestEqual=" + tieBroken.UniqueId;
                    target = tieBroken;
                    targetState = target.Descriptor?.State;
                }
            }
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
                TargetInRiderReach = !movementForbidden || StationaryStrikeAdmissible(target),
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
                NewerPlayerOperation = HasRiderPlayerOperation(rider, mount),
                RiderCannotAttack = NativeActorCapability.Read(rider).CanNotAttack
            }, out refusal);
            // Review R1-F11: native's own ranking for this exact proposal (the brain's live decision context), read only.
            // Review R5-F8: native's ranking is annotated with native's own proposal; the selected target is recorded separately.
            var ranking = turnBased ? null : NativeBrainRankingReader.Describe(rider, nativeProposed);
            var engagementNow = Game.Instance?.TimeController?.GameTime.TotalSeconds;
            if (ranking != null) LastAutomaticProposalRanking = ranking;
            if (decision == MountedAutomaticProposalDecision.Refuse)
            {
                AutomaticRefusedCount++;
                LastAutomaticRefusal = refusal + ";target=" + (target?.UniqueId ?? "<none>") + ";frame=" + Time.frameCount;
                if (!turnBased && engagementNow.HasValue && IsPairInCombat())
                    engagement.ObserveProposal(engagementNow.Value, nativeProposed?.UniqueId, target?.UniqueId, ranking, false, -1, refusal);
                return;
            }
            if (decision == MountedAutomaticProposalDecision.ContinueOwnIntent)
            {
                AutomaticContinuedCount++;
                if (!turnBased && engagementNow.HasValue && IsPairInCombat())
                    engagement.ObserveProposal(engagementNow.Value, nativeProposed?.UniqueId, target?.UniqueId, ranking, true, stockIntent.Generation, null);
                return;
            }
            AutomaticAdmittedCount++;
            // PR-1 review R1-F11: an automatic order inherits no ownership of an existing selection.
            riderSelection.Release();
            mountSelection.Release();
            stockIntent.Begin(target, null, false, IsPairInCombat(), rider, mount, rider.GetFirstWeapon(), mount.GetFirstWeapon(),
                (int)ResolveRiderPrimaryAction(), true);
            // PR-1 round 11 (R10-F3): the order keeps the proposing native action (its identity, never a number), bound to this generation.
            automaticOrderAction = proposal.AiAction;
            automaticOrderActionGeneration = stockIntent.Generation;
            retainedOrder.Begin(stockIntent.Generation, true, ResolveRiderPrimaryAction() != MountedCombatActionKind.RiderRanged);
            if (!turnBased && engagementNow.HasValue && IsPairInCombat())
                engagement.ObserveProposal(engagementNow.Value, nativeProposed?.UniqueId, target.UniqueId, ranking, true, stockIntent.Generation, null);
            LastStockAttackObservation = "automatic-intent-start;target=" + target.UniqueId +
                ";ranged=" + (ResolveRiderPrimaryAction() == MountedCombatActionKind.RiderRanged) +
                ";brainAction=" + proposal.AiAction.AssetGuid + ";movementForbidden=" + movementForbidden;
            logger.Info("Mounted automatic attack intent admitted: riderId=" + rider.UniqueId + "; targetId=" + target.UniqueId +
                "; brainAction=" + proposal.AiAction.AssetGuid + "; movementForbidden=" + movementForbidden + ".");
            TryDriveStockAttackIntent();
        }

        internal string LastAutomaticProposalRanking { get; private set; }
        internal long AutomaticTieBreakCount { get; private set; }
        internal string LastAutomaticTieBreak { get; private set; }

        // PR-1 round 11 (R10-F3): the native action that proposed the current automatic order, and the generation it belongs to.
        private Kingmaker.Controllers.Brain.Blueprints.BlueprintAiAction automaticOrderAction;
        private long automaticOrderActionGeneration = -1;

        // ---- PR-1 round 11 (R10-F1..F4): one current admission decision for the unreleased automatic order ----
        // Every boundary that may advance, keep or let the automatic order refuse other work asks this one decision for the exact order
        // (its generation, target and owned command): the proposal boundary (before the retained order may refuse a replacement), the
        // controller drive (before dispatch), queued promotion (AdmitQueuedAutomaticRun through TryRetireStockIntent), and the order's own
        // command at pre-start movement, native start, approach continuation and reissue, carrier creation, its own target-invalidation
        // observation and the windup before first delivery (EndIfAutomaticOrderNotAdmitted). The answer is typed, in one precedence: actual
        // target invalidation first (the command's own invalidation test), then perception loss, then native ineligibility of a valid target
        // (BlueprintAiAction.IsTargetValid's restricted rule with the action's current range), then (round 12, R11-F1/F2) the automatic order's
        // own boundaries, the rule TryRetireStockIntent applied only at the drive and queued promotion (AI off, Stop, newer player work, the
        // stationary mounted-strike check whenever pair movement is forbidden, a mount-only Hold included); so the observer that happens to
        // run first never changes the answer, the cause or the reason.
        private MountedAutomaticOrderAdmission DecideAutomaticOrderAdmission(MountedPairAttackCommand command, out string boundaryReason)
        {
            boundaryReason = null;
            var rider = relationship.Rider;
            var mount = relationship.Mount;
            var target = stockIntentTarget;
            if (AutomaticOrderTargetInvalid(command, rider, target)) return MountedAutomaticOrderAdmission.TargetInvalid;
            // PR-1 round 12 (R11-F3): an unavailable rider group or memory cannot show the target perceived.
            var memory = rider?.Group?.Memory;
            if (memory == null || !memory.Contains(target)) return MountedAutomaticOrderAdmission.TargetUnperceived;
            var turnBased = CombatController.IsInTurnBasedCombat();
            if (!turnBased && AutomaticTargetOutsideNativeAdmission(rider, target))
                return MountedAutomaticOrderAdmission.OutsideNativeAdmission;
            var movementForbidden = AutomaticMovementForbidden(rider);
            boundaryReason = MountedAutomaticAttackPolicy.RetireReason(
                !turnBased,
                true,
                rider != null && rider.IsDirectlyControllable,
                rider != null && rider.IsAIEnabled,
                rider != null && rider.IsBrainActive,
                automaticGate.IsSuppressed(relationship.MountedPairGeneration),
                movementForbidden,
                // PR-1 review R4-F3: the order's actual strike admission from the current position decides, not whether a carrier is live.
                !movementForbidden || StockIntentStrikeAdmissibleNow(),
                MountedRetainedOrderLedger.NewerPlayerWork(true, HasRiderPlayerOperation(rider, mount), retainedOrder.Superseded(stockIntent.Generation)));
            return boundaryReason != null ? MountedAutomaticOrderAdmission.OrderBoundary : MountedAutomaticOrderAdmission.Admitted;
        }

        // The order's own command's invalidation test while it exists (the test its own observation applies); before dispatch, the same
        // facts for the order's principal (the rider), with the order's own unconscious-target admission.
        private bool AutomaticOrderTargetInvalid(MountedPairAttackCommand command, UnitEntityData rider, UnitEntityData target)
        {
            if (command != null && !command.IsFinished) return command.TargetInvalidNow();
            var state = target?.Descriptor?.State;
            return !UnifiedMountedStockAttackPolicy.IsValidTarget(
                target != null,
                target != null && target.IsInState,
                state != null && UnifiedMountedStockAttackPolicy.TargetLifeAdmits(state.IsConscious, state.IsDead, state.IsFinallyDead,
                    stockIntent.TargetUnconsciousWhenOrdered),
                rider != null && rider.IsEnemy(target),
                rider != null && rider.CanAttack(target));
        }

        // Native BlueprintAiAction.IsTargetValid (0x0600942A) for the order's target now: restricted (the rider's AiMovementForbidden, or the
        // target in AIData.UnreachableUnits), it is valid only within the proposing action's CURRENT range (read from native for the rider:
        // its weapons or reach as they are now) plus both bodies (DecisionContext.ActionRangeWithCorpulence; TargetInfo.Distance is
        // Unit.DistanceTo) and in native line of sight (range first, as native tests it). It never depends on the optional ranking. A
        // restricted target whose action range cannot be read is not assumed admissible.
        private bool AutomaticTargetOutsideNativeAdmission(UnitEntityData rider, UnitEntityData target)
        {
            if (rider == null || target == null) return false;
            var unreachable = rider.CombatState?.AIData?.UnreachableUnits;
            if (!rider.AiMovementForbidden && (unreachable == null || !unreachable.Contains(target))) return false;
            var range = automaticOrderActionGeneration == stockIntent.Generation
                ? NativeBrainRankingReader.CurrentActionRange(rider, automaticOrderAction) : float.NaN;
            if (float.IsNaN(range)) return true;
            var reach = range + (rider.View != null ? rider.View.Corpulence : 0f) + (target.View != null ? target.View.Corpulence : 0f);
            return !(rider.DistanceTo(target) <= reach && NativeBrainRankingReader.NativeLos(rider, target));
        }

        internal long AutomaticAdmissionRetiredCount { get; private set; }

        // Retires the current automatic order when the decision says it is no longer admitted; returns true then. observer: the command
        // asking (null for the controller's own boundaries). Bound to the exact order: a command that is not the current automatic order's
        // own never inspects, retires or cleans up anything (a successor's work is untouched). Retirement takes the one order path for each
        // cause, so the order's own command and carrier are ended once, by the controller.
        // PR-1 round 13 (R12-F1): once the order's strike was released (delivered), the controller's boundaries still retire the generation, so
        // the repeating order ends; RetireOrderWrapper leaves the live wrapper to native completion (its remaining routine acts and terminal),
        // and no admission-loss cause is written onto it. A released command never asks itself.
        private bool RetireAutomaticOrderIfNotAdmitted(MountedPairAttackCommand observer)
        {
            if (!HasStockAttackIntent || !stockIntent.Automatic) return false;
            var command = stockIntentCommand;
            if (observer != null && (!ReferenceEquals(observer, command) || !ReferenceEquals(observer.TargetUnit, stockIntentTarget))) return false;
            if (observer != null && observer.StrikeReleased) return false;
            string boundaryReason;
            var admission = DecideAutomaticOrderAdmission(command, out boundaryReason);
            if (admission == MountedAutomaticOrderAdmission.Admitted) return false;
            AutomaticRetiredCount++;
            AutomaticAdmissionRetiredCount++;
            if (admission == MountedAutomaticOrderAdmission.TargetInvalid)
            {
                LastAutomaticRetirement = "automatic target invalid";
                RetireOrderOnTargetInvalidation();
                return true;
            }
            if (admission == MountedAutomaticOrderAdmission.OrderBoundary)
            {
                // The automatic order's own boundary retires it as the shared retirement always did: its exact wrapper is retired with
                // the boundary reason (a released strike was excluded above).
                LastAutomaticRetirement = boundaryReason;
                ClearStockAttackIntent(boundaryReason, true);
                RetireOrderWrapper(command, boundaryReason);
                return true;
            }
            var unperceived = admission == MountedAutomaticOrderAdmission.TargetUnperceived;
            var reason = unperceived ? "automatic target left perception" : "automatic target outside native admission";
            LastAutomaticRetirement = reason;
            if (command != null && !command.IsFinished && !command.StrikeReleased)
                command.RecordAutomaticAdmissionLoss(reason, unperceived ? "Automatic target left the rider group's perception."
                    : "Automatic target left native admission (outside the action's current range or sight).");
            ClearStockAttackIntent(reason, true);
            RetireOrderWrapper(command, reason);
            return true;
        }
        // ---- end of the automatic-order admission decision ----

        private UnitEntityData ResolveEqualRankTie(UnitEntityData rider, UnitEntityData proposed, UnitAttack proposal)
        {
            var candidates = NativeBrainRankingReader.Candidates(rider, proposal.AiAction);
            if (candidates == null) return null;
            var forbidden = AutomaticMovementForbidden(rider);
            // PR-1 review R6-F2: under restricted travel a melee candidate counts only if the pair can perform the stationary mounted
            // strike against it (StationaryStrikeAdmissible: mount origin, planned weapon, command sight); cached sight is not enough.
            var melee = ResolveRiderPrimaryAction() == MountedCombatActionKind.RiderMelee;
            var ranked = new List<NativeRankedCandidate>();
            foreach (var candidate in candidates)
            {
                var unit = candidate.Unit;
                var state = unit?.Descriptor?.State;
                // PR-1 reviews R7-F2, R8-F1: native's own admission for the proposal's action (BlueprintAiAction.IsTargetValid, 0x0600942A):
                // where the rider's movement is forbidden or the candidate is marked unreachable, only a candidate within the action's range
                // (with both bodies) AND in native line of sight is valid; otherwise native admits it and ordinary approach follows. Restricted
                // pair travel adds the stationary mounted-strike admission for a melee candidate (R6-F2). Dislike is not an admission rule
                // (native scores a disliked target 0.5, not 0): among the admitted equal-ranked candidates the tie-break passes over a disliked
                // one only while an admitted candidate that is not disliked exists; otherwise the disliked candidate is a legal choice (R9-F2).
                var nativeAdmissionRestricted = rider.AiMovementForbidden || rider.CombatState.AIData.UnreachableUnits.Contains(unit);
                var nativeValid = !nativeAdmissionRestricted || candidate.Distance <= candidate.ActionRangeWithCorpulence && candidate.InLos;
                var eligible = unit != null && state != null && state.IsConscious && !state.IsDead && rider.CanAttack(unit) &&
                    nativeValid &&
                    (!(forbidden && melee) || StationaryStrikeAdmissible(unit));
                // PR-1 review R9-F2: dislike is a preference applied by the tie-break after admission, not an admission rule.
                ranked.Add(new NativeRankedCandidate { Unit = unit, Weight = candidate.Weight, Distance = candidate.Distance, Eligible = eligible,
                    Disliked = unit != null && ReferenceEquals(rider.CombatState?.AIData?.DislikedTarget, unit) });
            }
            return NativeAttackTieBreak.Choose(ranked, proposed,
                HasStockAttackIntent && stockIntent.Automatic ? stockIntentTarget : null,
                ResolveRiderPrimaryAction() == MountedCombatActionKind.RiderMelee) as UnitEntityData;
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

        // Review R1-F8: the movement owner is the mount. Its Hold restricts automatic movement only when the player put it there
        // (MountedHoldProvenance); a mount that cannot move restricts it as the rider's own conditions do.
        private bool AutomaticMovementForbidden(UnitEntityData rider)
        {
            var mount = relationship.Mount;
            return rider != null && MountedHoldProvenance.AutomaticMovementForbidden(rider.AiMovementForbidden, automaticGate.PlayerMoveHold,
                mount == null || mount.Descriptor.State.CanMove, mount != null && mountHold.MountHoldRestrictsAutomatic(mount.HoldState));
        }

        // Review R3-F1: whether the planned strike is admissible from where the pair stands, by the strike's own production
        // admission on an unrun MountedPairSingleAttack built exactly as the pair command builds its child (rider melee:
        // mount-origin reach; horse natural attack: the horse executes), never a separate reach formula.
        internal long PlannedStrikeProbeCount { get; private set; }

        // PR-1 review R1-F1: a turn-based horse follow-through is offered only while the planned horse strike's own production
        // admission passes from where the pair stands (exact native distance, planned weapon, command sight).
        private bool FollowThroughStrikeAdmissibleNow(UnitEntityData target) =>
            PlannedStrikeAdmissibleNow(MountedCombatActionKind.MountPrimaryNatural, target);

        private bool PlannedStrikeAdmissibleNow(MountedCombatActionKind action, UnitEntityData target)
        {
            PlannedStrikeProbeCount++;
            var rider = relationship.Rider;
            var mount = relationship.Mount;
            if (rider?.View == null || mount?.View == null || target?.View == null) return false;
            try
            {
                var mountStrike = action == MountedCombatActionKind.MountPrimaryNatural;
                var probe = new MountedPairSingleAttack(target, rider, mount, !mountStrike);
                probe.Init(mountStrike ? mount : rider);
                return probe.EvaluateCurrentNativeAdmission() == MountedPairNativeAdmissionState.Admitted;
            }
            catch (Exception exception)
            {
                TryLogException("Planned strike admission probe", exception);
                return false;
            }
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
            // Review R2-F5: the player's own work in the mount's slots counts too (a newer mount order while the old attack
            // waits), except the pair's own commands and carrier moves, reactions and coexisting Free/Swift actions.
            var mountRaw = mount?.Commands?.Raw;
            if (mountRaw != null)
            {
                foreach (var command in mountRaw)
                {
                    if (command == null || !IsPairPlayerOrder(command)) continue;
                    slots.Add(new MountedAutomaticSlotFacts
                    {
                        Finished = command.IsFinished,
                        AiOrigin = command.AiAction != null,
                        PairOwnedOrReaction = command is MountedPairSingleAttack || command is UnitAttackOfOpportunity || IsOwnDelegatedMove(command),
                        CoexistsWithAttack = (command.Type == UnitCommand.CommandType.Free || command.Type == UnitCommand.CommandType.Swift) &&
                            !CommandNeedsApproach(command)
                    });
                }
            }
            var live = MountedAutomaticAttackPolicy.HasPlayerOperation(slots, CountForeignQueued(rider) + CountForeignQueued(mount));
            // F4 decision snapshot (r03 Hold stall): the exact operations that made this predicate true, overwritten only
            // when it is true, so a refusal can always be attributed to a named command.
            if (live)
            {
                try { LastPlayerOperationDetail = DescribePlayerOperation(rider, mount); }
                catch (Exception exception) { LastPlayerOperationDetail = "frame=" + Time.frameCount + "; <" + exception.GetType().Name + ">"; }
            }
            return live;
        }

        internal string LastPlayerOperationDetail { get; private set; }

        private static string DescribePlayerOperation(UnitEntityData rider, UnitEntityData mount)
        {
            var parts = new List<string> { "frame=" + Time.frameCount };
            var raw = rider?.Commands?.Raw;
            if (raw != null)
                foreach (var command in raw)
                    if (command != null && !command.IsFinished && command.AiAction == null && !(command is MountedPairSingleAttack) &&
                        !(command is UnitAttackOfOpportunity))
                        parts.Add("riderSlot=" + DescribeCommand(command));
            var mountRaw = mount?.Commands?.Raw;
            if (mountRaw != null)
                foreach (var command in mountRaw)
                    if (command != null && !command.IsFinished && command.AiAction == null && !(command is MountedPairSingleAttack) &&
                        !(command is UnitAttackOfOpportunity) && command.CreatedByPlayer)
                        parts.Add("mountSlot=" + DescribeCommand(command));
            foreach (var unit in new[] { rider, mount })
            {
                var queue = unit?.Commands?.Queue;
                if (queue == null) continue;
                foreach (var command in queue)
                    if (!(command is MountedPairSingleAttack))
                        parts.Add((unit == rider ? "riderQueue=" : "mountQueue=") + DescribeCommand(command));
            }
            return string.Join("; ", parts.ToArray());
        }

        private static string DescribeCommand(UnitCommand command) =>
            command.GetType().Name + "{type=" + command.Type + ",started=" + command.IsStarted + ",finished=" + command.IsFinished +
            ",player=" + command.CreatedByPlayer + ",ai=" + (command.AiAction != null) + ",executor=" + command.Executor?.UniqueId +
            ",target=" + (command.TargetUnit?.UniqueId ?? "<point>") + ",enoughClose=" + DescribeEnoughClose(command) +
            ",ability=" + ((command as UnitUseAbility)?.Spell?.Blueprint?.AssetGuid ?? "-") + "}";

        // Only the Free/Swift coexistence predicate reads the native distance; other commands are not asked.
        private static string DescribeEnoughClose(UnitCommand command)
        {
            if (command.IsStarted) return "started";
            if (command.Type != UnitCommand.CommandType.Free && command.Type != UnitCommand.CommandType.Swift) return "-";
            try { return command.IsUnitEnoughClose.ToString(); }
            catch (Exception exception) { return "<" + exception.GetType().Name + ">"; }
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
            if (!stockIntent.Automatic && turnBased)
            {
                // PR-1 review R1-F2: newer competing player work accepted into either participant supersedes the older order's
                // unreleased work on BOTH participants (a pending rider dispatch or queued wrapper included); a strike already
                // released keeps native's handling.
                if (!RetainedOrderSuperseded()) return false;
                return RetireSupersededPlayerOrder();
            }
            // PR-1 rounds 11-12 (R10-F2, R11-F1/F2): an automatic order is decided entirely by its one typed admission decision (target
            // invalidation first, then perception, native eligibility and its own boundaries); what follows is the explicit order's rule.
            if (stockIntent.Automatic) return RetireAutomaticOrderIfNotAdmitted(null);
            var movementForbidden = stockIntent.Automatic && AutomaticMovementForbidden(rider);
            var reason = MountedAutomaticAttackPolicy.RetireReason(
                !turnBased,
                stockIntent.Automatic,
                rider != null && rider.IsDirectlyControllable,
                rider != null && rider.IsAIEnabled,
                rider != null && rider.IsBrainActive,
                automaticGate.IsSuppressed(relationship.MountedPairGeneration),
                movementForbidden,
                // PR-1 review R4-F3: the order's actual strike admission from the current position decides, not whether a carrier is live.
                !movementForbidden || StockIntentStrikeAdmissibleNow(),
                MountedRetainedOrderLedger.NewerPlayerWork(stockIntent.Automatic,
                    stockIntent.Automatic && HasRiderPlayerOperation(rider, mount), retainedOrder.Superseded(stockIntent.Generation)));
            if (reason == null) return false;
            var automatic = stockIntent.Automatic;
            var command = stockIntentCommand;
            ClearStockAttackIntent(reason, true);
            if (automatic)
            {
                AutomaticRetiredCount++;
                LastAutomaticRetirement = reason;
            }
            RetireOrderWrapper(command, reason);
            return true;
        }

        // PR-1 reviews R2-F4, R4-F5: the turn-based order ends with neither actor able to act; the rider's evidenced refusal for this
        // generation, if any, is the answer, once (shared by the drive and the attack-path harness).
        private void FinishTurnBasedIntent(string feedback, string trace)
        {
            var pendingRefusal = retainedOrder.TakeRefusalOnRetirement(stockIntent.Generation);
            if (pendingRefusal != null) { feedback = pendingRefusal; PendingRefusalPresentedCount++; }
            if (feedback != null)
            {
                LastFeedback = feedback;
                LastStandardRefusalTrace = trace;
                logger.Info("Mounted stock attack refused for its spent actor: " + LastStandardRefusalTrace);
                ShowStockFeedback();
            }
            ClearStockAttackIntent("native actor resources exhausted", false);
            LastStockAttackObservation = "tb-intent-complete-resources";
        }

        // PR-1 round 14 (R13-F1): a released wrapper whose attack plan is exhausted (its final act delivered, its native animation still
        // finishing, IsFinished false) has no next attack to judge, so its own admission would read Unavailable; the retained target's
        // current stationary eligibility for future dispatch is then assessed with the production planned-strike probe
        // (StationaryStrikeAdmissible) instead, never inferred from the empty plan.
        private bool StockIntentStrikeAdmissibleNow() =>
            stockIntentCommand != null && !stockIntentCommand.IsFinished && !stockIntentCommand.AttackPlanExhausted
                ? stockIntentCommand.StrikeAdmissibleNow() : StationaryStrikeAdmissible(stockIntentTarget);

        // PR-1 review R5-F3: before a wrapper exists, a melee order's stationary admission is the planned mounted melee strike (mount origin,
        // planned weapon, command sight), not the rider-only reach shortcut; ranged orders keep their rider reach rule.
        private bool StationaryStrikeAdmissible(UnitEntityData target) =>
            ResolveRiderPrimaryAction() == MountedCombatActionKind.RiderRanged ? IsTargetInRiderReach(target)
                : PlannedStrikeAdmissibleNow(MountedCombatActionKind.RiderMelee, target);

        private bool RetainedOrderSuperseded() => HasStockAttackIntent && retainedOrder.IsCurrent(stockIntent.Generation) &&
            retainedOrder.Superseded(stockIntent.Generation) && (stockIntentCommand == null || !stockIntentCommand.StrikeReleased);

        private bool RetireSupersededPlayerOrder()
        {
            const string reason = "newer accepted player work supersedes the order";
            var command = stockIntentCommand;
            ClearStockAttackIntent(reason, true);
            SupersededPlayerOrderCount++;
            RetireOrderWrapper(command, reason);
            return true;
        }

        // PR-1 round-4 lifecycle (reviews R2-F1, R3-F1, R3-F2): the one path every retirement of an order takes for its exact
        // wrapper (turn-based supersession, real-time retirement, perception loss). A delivered strike is left to native completion
        // (the order is already retired, so it cannot repeat); an unreleased wrapper is marked retired on itself, taken out of its
        // native queue if queued, and ended. Unrelated queue entries and other actors' orders are untouched.
        // hardBoundary (a lifecycle boundary such as dismount, area change or disposal): even a delivered strike is ended.
        private void RetireOrderWrapper(MountedPairAttackCommand command, string reason, bool hardBoundary = false)
        {
            if (command == null) return;
            var commands = command.Executor?.Commands;
            var holdsSlot = command.IsStarted || commands != null && commands.Contains(command);
            switch (MountedRetainedOrderLedger.RetireWrapper(true, command.IsFinished, command.StrikeReleased && !hardBoundary, holdsSlot))
            {
                case MountedWrapperRetirement.LeaveToNative:
                    ReleasedStrikeLeftToNativeCount++;
                    return;
                case MountedWrapperRetirement.RefuseQueued:
                    command.MarkOrderRetired();
                    if (commands != null && commands.Queue.Remove(command)) RetiredQueueEntryRemovedCount++;
                    if (ReferenceEquals(activeCommand, command)) activeCommand = null;
                    if (!command.IsFinished) InterruptOwnedCommand(command, MountedAttackEndCause.Cancelled, reason);
                    return;
                case MountedWrapperRetirement.InterruptInSlot:
                    command.MarkOrderRetired();
                    if (ReferenceEquals(activeCommand, command)) activeCommand = null;
                    InterruptOwnedCommand(command, MountedAttackEndCause.Cancelled, reason);
                    SweepFinishedCommand();
                    return;
            }
        }

        internal long SupersededPlayerOrderCount { get; private set; }
        internal long RetiredQueueEntryRemovedCount { get; private set; }
        internal long ReleasedStrikeLeftToNativeCount { get; private set; }

        // PR-1 review R1-F2/R3-F1: native promotion of this pair's explicit queued wrapper; a retired wrapper (marked on itself)
        // never promotes, whichever of controller retirement and promotion runs first.
        internal bool AdmitQueuedExplicitRun(MountedPairSingleAttack command)
        {
            if (command != null && command.OrderRetired)
            {
                if (!command.IsFinished) EndQueuedCommand(command, "retired pair command");
                return false;
            }
            if (disposed || command == null || !ReferenceEquals(command, stockIntentCommand) || stockIntent.Automatic) return true;
            if (!RetainedOrderSuperseded()) return true;
            RetireSupersededPlayerOrder();
            if (!command.IsFinished) EndQueuedCommand(command, "superseded queued pair command");
            return false;
        }

        private void TryDriveStockAttackIntent()
        {
            // PR #70 R1-F6 (rounds 6-7): while a provisional order is pending (its command and carrier live, its turn current), the previous
            // order is neither retired nor advanced; every end of that state settles it here first.
            SweepFirstTickProposal();
            if (PendingReplacement) return;
            if (chargeOwner != null) return; // A charge from admission to its end owns the pair's attack.
            var target = stockIntentTarget;
            if (disposed || target == null)
            {
                return;
            }
            // PR-1 reviews R1-F9, R10-F1..F4: the automatic order's one admission decision, before any readiness or geometry probe.
            if (relationship.State == RelationshipState.Mounted && RetireAutomaticOrderIfNotAdmitted(null)) return;

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
            // PR-1 round 13 (R12-F4): an automatic order was decided at the top of this drive (RetireAutomaticOrderIfNotAdmitted); only an
            // explicit order's retirement rule remains to apply here, so the stationary strike probe is built once per drive.
            if (relationship.State == RelationshipState.Mounted && !stockIntent.Automatic && TryRetireStockIntent(rider, mount, turnBased))
            {
                return;
            }
            var targetState = target.Descriptor?.State;
            var principal = stockIntent.MountActor ? mount : rider;
            var targetValid = UnifiedMountedStockAttackPolicy.IsValidTarget(
                target != null,
                target != null && target.IsInState,
                targetState != null && UnifiedMountedStockAttackPolicy.TargetLifeAdmits(targetState.IsConscious,
                    targetState.IsDead, targetState.IsFinallyDead, stockIntent.TargetUnconsciousWhenOrdered),
                principal != null && principal.IsEnemy(target),
                principal != null && principal.CanAttack(target));
            // Retire repetition now; the owned child's actual terminal lifecycle decides success versus expected invalidation. Released
            // projectiles and native AoOs survive.
            if (RetireOrderIfTargetInvalid(targetValid)) return;
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
            // MMR-03 (reviews R1-F4/F5/F6, R2-F1, R3-F3). Turn-based: each actor is offered only when the dispatcher's own
            // admission allows it, so an individually ended or prohibited rider leaves the eligible horse its attack and vice
            // versa. The horse joins a rider's ordinary order only for the attack kind the order was ACCEPTED with
            // (MountedRetainedOrderLedger, fixed at Begin) and only while no newer player work was accepted into either actor's
            // container for this order; the order's own wrapper and exact delegated moves (repaths included) never count.
            MountedCombatActionKind earlyAction;
            bool dispatchBeforeReadiness;
            var plan = PlanStockDispatch(rider, mount, target, turnBased, targetValid, riderAction, ranged, stationary, out earlyAction, out dispatchBeforeReadiness);
            var decision = plan.Decision;

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
                var spent = stockIntent.MountActor ? mount : rider;
                var partner = stockIntent.MountActor ? rider : mount;
                var ownRefusal = turnBased && spent != null
                    ? EvaluateStockActor(stockIntent.MountActor ? MountedCombatActionKind.MountPrimaryNatural : riderAction, target) : null;
                var feedback = stockIntent.GetRejectionFeedback(ownRefusal != null && !ownRefusal.IsAllowed && spent.HasStandardAction() &&
                    !string.IsNullOrEmpty(ownRefusal.Feedback)
                    ? ownRefusal.Feedback
                    : UnifiedMountedStockAttackPolicy.NoStandardFeedback(
                        spent?.CharacterName, partner?.CharacterName, partner != null && partner.HasStandardAction()));
                FinishTurnBasedIntent(feedback, "actor=" + spent?.UniqueId +
                    ";riderStandard=" + rider?.CombatState.Cooldown.StandardAction.ToString("R") +
                    ";riderMove=" + rider?.CombatState.Cooldown.MoveAction.ToString("R") +
                    ";mountStandard=" + mount?.CombatState.Cooldown.StandardAction.ToString("R") +
                    ";mountMove=" + mount?.CombatState.Cooldown.MoveAction.ToString("R"));
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
            // PR-1 review R1-F1: a turn-based horse follow-through on a rider's order has no movement authority of its own; a target
            // that left the horse's reach before start settles the opportunity without a carrier (OnStart).
            var allowApproach = plan.AllowApproach;
            var result = TryExecuteAction(
                action,
                target,
                automaticOrigin ? AutomaticInputSurface : "stock-hostile-click",
                rider,
                allowApproach,
                !turnBased && allowApproach && action != MountedCombatActionKind.RiderRanged,
                dispatchBeforeReadiness && action == earlyAction);
            if (result != MountedCombatClickResult.HandledAccepted || activeCommand == null)
            {
                // Read before clearing: whether the request already dispatched decides its answer. Every rejection
                // is logged, since several admission faults have no other diagnostic.
                var codes = LastRejectionCodes ?? new MountedCombatRejectionCode[0];
                var feedback = stockIntent.GetDispatchRejectionFeedback(LastFeedback, codes);
                logger.Info("Mounted stock attack dispatch rejected: action=" + action +
                    "; origin=" + (automaticOrigin ? "automatic" : "player") +
                    "; generation=" + generation +
                    "; actorId=" + ((action == MountedCombatActionKind.MountPrimaryNatural ? mount : rider)?.UniqueId ?? "<none>") +
                    "; targetId=" + target.UniqueId +
                    "; codes=" + string.Join(",", codes.Select(code => code.ToString()).ToArray()) +
                    "; presented=" + (feedback != null) +
                    "; feedback=" + LastFeedback);
                if (automaticOrigin)
                {
                    // No player click to answer: the brain's next proposal re-decides, without a player-facing warning.
                    ClearStockAttackIntent("automatic dispatch rejected", true);
                    LastStockAttackObservation = "automatic-intent-cancel-dispatch-rejected;action=" + action;
                    return;
                }
                ClearStockAttackIntent("stock dispatch rejected", true);
                LastStockAttackObservation = "intent-cancel-dispatch-rejected;action=" + action;
                if (feedback != null) ShowStockFeedback();
                return;
            }
            if (!stockIntent.Owns(target, generation))
            {
                Cancel("stock intent replaced during native admission");
                return;
            }

            stockIntentCommand = activeCommand;
            if (automaticOrigin) { AutomaticDispatchCount++; engagement.ObserveDispatch(generation); }
            if (dispatchBeforeReadiness) StockAttackDispatchBeforeReadinessCount++;
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

        // PR-1 review R4-F5: the dispatch part of TryDriveStockAttackIntent, one production operation shared with the attack-path harness:
        // the current generation, each actor's eligibility (a rider attempt that ended for its own capability is never redispatched, even
        // with its Standard unspent and its capability recovered), the real-time early striker, and the production plan.
        private MountedDispatchPlan PlanStockDispatch(UnitEntityData rider, UnitEntityData mount, UnitEntityData target, bool turnBased, bool targetValid,
            MountedCombatActionKind riderAction, bool ranged, bool stationary, out MountedCombatActionKind earlyAction, out bool dispatchBeforeReadiness)
        {
            earlyAction = MountedCombatActionKind.RiderMelee;
            dispatchBeforeReadiness = false;
            if (!HasStockAttackIntent || target == null) return new MountedDispatchPlan { Decision = MountedStockAttackDecision.Wait };
            var orderGeneration = stockIntent.Generation;
            var meleeOrder = retainedOrder.IsCurrent(orderGeneration) ? retainedOrder.Melee : !ranged;
            var dispatchTurn = turnBased ? Game.Instance?.TurnBasedCombatController?.CurrentTurn : null;
            var riderEligible = turnBased
                ? !retainedOrder.RiderEnded(orderGeneration) &&
                    EvaluateStockActor(riderAction == MountedCombatActionKind.RiderRanged ? MountedCombatActionKind.RiderRanged : MountedCombatActionKind.RiderMelee, target).IsAllowed
                : rider != null && rider.HasStandardAction();
            var mountEligible = turnBased
                ? EvaluateStockActor(MountedCombatActionKind.MountPrimaryNatural, target).IsAllowed
                : mount != null && mount.HasStandardAction();
            // MMR-05 (review R3-F1). Real time, neither actor strike-ready, an out-of-reach melee order whose movement is lawful:
            // the rider's order is dispatched now and waits in its native slot exactly as an unmounted UnitAttack does (native
            // ShouldStartCommand keeps preparation, the initiative wait, cooldown and busy hands); its own pre-start approach
            // moves the mount meanwhile. Nothing readies, refunds or spends an action.
            // Reviews R5-F6, R5-F9: the early striker is the order's own actor under the existing order rules (a rider order is the
            // rider's, an explicit horse order the horse's); temporary start readiness never converts it into the other actor's
            // attack. The planned-strike probe runs only for an actual early-dispatch decision (no active command, nobody ready).
            var earlyStriker = MountedEarlyStriker.None;
            if (!turnBased && rider != null && mount != null && meleeOrder && !HasActiveCommand && !riderEligible && !mountEligible &&
                !stationary && mount.Descriptor.State.CanMove)
            {
                earlyStriker = MountedPreStartApproachPolicy.SelectStriker(stockIntent.MountActor,
                    riderAction == MountedCombatActionKind.RiderMelee && !rider.IsStandardActionRestricted() && rider.Descriptor.State.CanAct &&
                        !NativeActorCapability.Read(rider).CanNotAttack,
                    !mount.IsStandardActionRestricted() && mount.Descriptor.State.CanAct && !NativeActorCapability.Read(mount).CanNotAttack);
            }
            earlyAction = earlyStriker == MountedEarlyStriker.Mount ? MountedCombatActionKind.MountPrimaryNatural : MountedCombatActionKind.RiderMelee;
            dispatchBeforeReadiness = earlyStriker != MountedEarlyStriker.None &&
                MountedPreStartApproachPolicy.DispatchBeforeStrikeReadiness(true, true, false, PlannedStrikeAdmissibleNow(earlyAction, target),
                    riderEligible, mountEligible);
            if (dispatchBeforeReadiness)
            {
                if (earlyStriker == MountedEarlyStriker.Mount) mountEligible = true; else riderEligible = true;
            }
            // PR-1 reviews R3-F4, R3-F5: one production plan decides the next dispatch, the acting actor and its approach authority;
            // the horse strike-plan probe runs only when a horse dispatch can follow from it.
            var plan = MountedDispatchPlanner.Plan(new MountedDispatchFacts
            {
                ExactPair = relationship.State == RelationshipState.Mounted && rider != null && mount != null,
                TargetValid = targetValid,
                PairCommandActive = HasActiveCommand,
                TurnBased = turnBased,
                RiderEligible = riderEligible,
                MountEligible = mountEligible,
                Ranged = ranged,
                Stationary = stationary,
                MountAlreadyInRange = IsMountAlreadyInPrimaryRange(target),
                ExplicitMountOrder = stockIntent.MountActor,
                LegacyUnifiedTurn = settings.UseLegacyUnifiedTurn,
                PairedActivation = settings.EnablePairedActivation,
                Automatic = stockIntent.Automatic,
                MountAddressable = turnBased && mount != null && unifiedTurn.CanAddressActor(mount, dispatchTurn),
                MeleeOrder = meleeOrder,
                Superseded = retainedOrder.Superseded(orderGeneration),
                PreferMount = stockIntent.PreferMount,
                MountInReach = () => FollowThroughStrikeAdmissibleNow(target)
            });
            return plan;
        }

        internal long StockAttackDispatchBeforeReadinessCount { get; private set; }

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

        // PR-1 review R1-F11 (round 4): ownership of each actor's manual target by the generation that wrote it
        // (MountedManualTargetOwnership): any actual foreign assignment, the same target included, takes the selection over.
        private readonly MountedManualTargetOwnership riderSelection = new MountedManualTargetOwnership();
        private readonly MountedManualTargetOwnership mountSelection = new MountedManualTargetOwnership();
        private long clickSerial;
        private long activeClick = -1;

        private MountedManualTargetOwnership SelectionOf(UnitEntityData actor) =>
            actor == null ? null : actor == relationship.Rider ? riderSelection : actor == relationship.Mount ? mountSelection : null;

        // The pair's own write. An actor-specific action outside any stock generation owns nothing and ends earlier ownership.
        private void OwnManualTarget(UnitEntityData actor, UnitEntityData target, long generation)
        {
            if (actor == null) return;
            actor.CombatState.ManualTarget = target;
            SelectionOf(actor)?.RecordOwnWrite(generation, target, activeClick);
        }

        private void ClearStockAttackIntent(string reason, bool countCancellation)
        {
            var target = stockIntentTarget;
            if (target != null && countCancellation)
            {
                StockAttackIntentCancelCount++;
            }
            // PR-1 review R1-F11: only a manual-target assignment this generation made and still owns is released; a pre-existing
            // player selection or any newer assignment (the same target included) survives.
            var retiringGeneration = stockIntent.Generation;
            if (riderSelection.ClearsOnRetirement(retiringGeneration, relationship.Rider?.CombatState?.ManualTarget))
            {
                relationship.Rider.CombatState.ManualTarget = null;
            }
            if (mountSelection.ClearsOnRetirement(retiringGeneration, relationship.Mount?.CombatState?.ManualTarget))
            {
                relationship.Mount.CombatState.ManualTarget = null;
            }
            riderSelection.Release();
            mountSelection.Release();
            stockIntent.Cancel();
            retainedOrder.Clear();
            intentClickProof = null;
            if (pendingClickProof != null) DiscardClickProof("order cleared");
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
            groundOrderClickApproach = null;
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

        // Known issues M04 closeout (review of b1a88b4e, R2): the open ground order is running the exact native
        // cannot-attack click approach, as the original command object, on the mount. Native never sets its
        // CreatedByPlayer, so the order's own checks recognise it by identity instead. Held only while the order is
        // open: CompleteGroundCommandAdmission (always reached, from the hook's finally) clears it.
        internal void BindGroundOrderClickApproach(UnitCommand command)
        {
            if (groundOrderOpen && !disposed) groundOrderClickApproach = command;
        }

        private UnitCommand groundOrderClickApproach;

        // A player order of this pair: CreatedByPlayer, or the exact native cannot-attack click approach the pair's ground
        // order ran (native never marks it CreatedByPlayer): by identity while that order is open, afterwards as the
        // watched explicit move. The watch was bound only for CreatedByPlayer moves before, so no other command changes.
        internal bool IsPairPlayerOrder(UnitCommand command) =>
            command != null && (command.CreatedByPlayer || ReferenceEquals(command, groundOrderClickApproach) ||
                playerMove.IsCurrent(command, relationship.MountedPairGeneration));

        internal void ObserveGroundOrderRun(UnitCommands commands, UnitCommand command)
        {
            if (!groundOrderOpen || disposed || relationship.State != RelationshipState.Mounted) return;
            if (!MountedPlayerMoveWatch.IsExplicitGroundMove(groundOrderOpen, command != null && command.GetType() == typeof(UnitMoveTo),
                IsPairPlayerOrder(command), command?.AiAction != null,
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
            // PR #70 R6-F1 (round 7): the Run of a pending provisional order's command or carrier keeps the actor's Hold; the order applies it once
            // when it commits (CommitAcceptedProvisional), and a refusal leaves it as it was.
            if (IsPendingReplacementWork(command))
            {
                if (owner.HoldState)
                {
                    if (owner == relationship.Rider) firstTickProposal.RiderHoldDeferred = true; else firstTickProposal.MountHoldDeferred = true;
                    DeferredHoldWriteCount++;
                }
                return true;
            }
            // Review R1-F8: an automatic order's exact delegated move does not clear the mount's Hold.
            var keeps = owner == relationship.Mount &&
                MountedHoldProvenance.NativeRunKeepsMountHold(retainedOrder.IsRetainedOwn(command), retainedOrder.Automatic) ||
                automaticRuns.NativeRunKeepsHold(command, owner.Commands?.Queue.First?.Value, doNotClearQueue,
                relationship.MountedPairGeneration);
            if (keeps && owner.HoldState) AutomaticHoldKeptCount++;
            if (keeps && owner.HoldState && fromQueue) AutomaticQueuedHoldKeptCount++;
            return keeps;
        }

        // The Run that queue advancement makes. A refused command is left finished, never in a slot.
        internal bool AdmitQueuedAutomaticRun(MountedPairSingleAttack command)
        {
            // PR-1 review R3-F1: a wrapper retired with its order never promotes.
            if (command != null && command.OrderRetired)
            {
                AutomaticQueuedRefusedCount++;
                if (!command.IsFinished) EndQueuedCommand(command, "retired pair command");
                return false;
            }
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
                EndQueuedCommand(command, "stale queued automatic command");
                return false;
            }
            var rider = relationship.Rider;
            if (TryRetireStockIntent(rider, relationship.Mount, CombatController.IsInTurnBasedCombat()))
            {
                AutomaticQueuedRefusedCount++;
                if (!command.IsFinished) EndQueuedCommand(command, "queued automatic command retired");
                return false;
            }
            if (AutomaticMovementForbidden(rider)) (command as MountedPairAttackCommand)?.ForbidApproach();
            AutomaticQueuedRunCount++;
            return true;
        }

        // A queued pair command the controller refuses records its cause like every other owned end.
        private void EndQueuedCommand(MountedPairSingleAttack command, string reason)
        {
            var pair = command as MountedPairAttackCommand;
            if (pair != null) InterruptOwnedCommand(pair, MountedAttackEndCause.Cancelled, reason);
            else command.Interrupt();
        }

        internal long ExplicitOrphanReleasedCount { get; private set; }

        private void SweepOrphanedAutomaticCommand()
        {
            var command = activeCommand;
            var commands = command?.ActionActor?.Commands;
            if (commands == null) return;
            var inSlot = commands.Contains(command);
            var inQueue = commands.Queue.Contains(command);
            // PR-1 review R1-F5: an explicit (or promoted-to-player) unstarted wrapper that native dropped from both its slot and the
            // queue (a fresh Run cleared the queue) no longer holds the pair: its claim ends; the command now in the container is
            // not touched.
            var explicitOrphan = !command.IsAutomaticOrigin && !command.IsStarted && !command.IsFinished && !inSlot && !inQueue;
            if (explicitOrphan)
            {
                activeCommand = null;
                ExplicitOrphanReleasedCount++;
                if (ReferenceEquals(stockIntentCommand, command)) ClearStockAttackIntent("explicit command dropped from the native queue", true);
                InterruptOwnedCommand(command, MountedAttackEndCause.Cancelled, "explicit command dropped from the native queue");
                return;
            }
            if (!MountedAutomaticRunLedger.IsOrphaned(command.IsAutomaticOrigin, command.IsStarted, command.IsFinished, inSlot, inQueue)) return;
            activeCommand = null;
            AutomaticQueuedRefusedCount++;
            // The intent ends first: nothing failed to reach this target, so its terminal must not record it unreachable.
            if (ReferenceEquals(stockIntentCommand, command)) ClearStockAttackIntent("automatic command dropped from the native queue", true);
            InterruptOwnedCommand(command, MountedAttackEndCause.Cancelled, "automatic command dropped from the native queue");
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
            // PR #70 R1-F6 (round 7): a provisional order is the active pair command, so its carrier is admitted as the attack approach is.
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
            var clickApproach = groundOrderClickApproach;
            groundOrderClickApproach = null;
            if (!riderTurnGroundMoveAdmissionPending)
            {
                return;
            }

            riderTurnGroundMoveAdmissionPending = false;
            if (!relationship.IsCurrentPairOwner(groundMoveOwner)) { groundMoveOwner = null; return; }
            var command = groundMoveOwner.Mount?.Commands?.GetCommand(UnitCommand.CommandType.Move) as UnitMoveTo;
            if (routedUnit != relationship.Mount || command == null || command.Executor != relationship.Mount ||
                !(command.CreatedByPlayer || ReferenceEquals(command, clickApproach)) || relationship.Mount.Commands.Queue.Contains(command))
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
                        InterruptOwnedCommand(dispatched, MountedAttackEndCause.Replaced, "door interaction replaced stock attack");
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
            entranceCarrier.Clear();
            if (ChargeAdmissionFenced) throw new InvalidOperationException("Controller teardown retained a native boundary or serialization fence.");
            // R12-F4 (round 13): owned cleanup (including work the cancellation created) is settled or keeps this controller alive.
            if (!TrySettleOutstandingCleanup()) throw new InvalidOperationException("Controller teardown retains outstanding mounted cleanup; retry once it settles.");
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
            bool nativeActorIntent = false,
            bool nativeStartGatesReadiness = false,
            bool freshExplicitClick = false)
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
                // Native explicit-order rule: only the player's own retained order on an enemy that was already
                // unconscious admits that downed enemy; every other request still needs a conscious target.
                TargetAliveAndConscious = assumeTargetAvailable || freshExplicitClick && FreshExplicitClickTargetLifeAdmits(target) ||
                    !freshExplicitClick && targetState != null && UnifiedMountedStockAttackPolicy.TargetLifeAdmits(targetState.IsConscious,
                        targetState.IsDead, targetState.IsFinallyDead,
                        ordinaryIntent && !stockIntent.Automatic && ReferenceEquals(target, stockIntentTarget) &&
                        stockIntent.TargetUnconsciousWhenOrdered),
                TargetVisible = assumeTargetAvailable || targetValid && target.IsVisibleForPlayer,
                TargetHostile = assumeTargetAvailable || targetValid && actionActor != null && actionActor.IsEnemy(target),
                TargetAttackable = assumeTargetAvailable || targetValid && actionActor != null && actionActor.CanAttack(target),
                TargetIsVisibleEnemy = assumeTargetAvailable || targetValid && target.IsVisibleForPlayer && actionActor != null &&
                    actionActor.IsEnemy(target) && actionActor.CanAttack(target),
                ActionActorOwnsCurrentTurnOrRealTime = actionActorTurn,
                // MMR-05 (review R3-F1): a real-time rider order dispatched before strike readiness waits in its native slot;
                // native ShouldStartCommand alone decides when it may start (cooldown, initiative). A restriction still refuses.
                ActionActorHasStandardAction = actionActor != null && (actionActor.HasStandardAction() ||
                    nativeStartGatesReadiness && !turnBasedCombat && !actionActor.IsStandardActionRestricted()),
                // A remaining Standard is not permission to act: the acting actor's own conditions are read separately.
                ActionActorCapabilityBlock = MountedActorCapabilityPolicy.AdmissionBlock(
                    NativeActorCapability.Read(actionActor), turnBasedCombat),
                ActionActorDisplayName = actionActor?.CharacterName,
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
                TransactionIdle = !HasFaultedChargeCleanupOwner && (!HasActiveCommand || HasStockAttackIntent && ReferenceEquals(activeCommand, stockIntentCommand) ||
                    evaluatingFirstTickCommand != null && ReferenceEquals(activeCommand, evaluatingFirstTickCommand)) &&
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
            // PR #70 R12-F4 (round 16): the mod-disable/unload relationship cleanup yields the mount's newer player Move (its command, path and
            // cursor) while it ends the pair's own work; every other cleanup keeps the hard cancellation.
            if (trigger == CleanupTrigger.ModDisabled) CancelCore("relationship cleanup " + trigger, false, true);
            else Cancel("relationship cleanup " + trigger);
        }

        internal long RiderSpecificTerminalCount { get; private set; }
        internal long PendingRefusalPresentedCount { get; private set; }

        private void HandleCommandTerminal(
            MountedPairAttackCommand command,
            MountedPairAttackOutcome outcome)
        {
            // Review of fa533e97 (R20): every step that can fail is contained, so ownership, the finished-command sweep,
            // the outcome and the order's retirement always settle, and the handler is applied once and completely.
            if (ReferenceEquals(chargeOwner?.Command, command)) TryContain("Charge ownership retirement at the terminal", () => chargeOwner.Ownership.Retire());
            // R11-F7 (round 12): an exact carrier cleanup the command left unfinished at its end stays owned until it settles.
            TryContain("Retained carrier cleanup at the terminal", () => RetainCarrierCleanup(command));
            TryContain("Paired scheduler terminal observation", () => pairedCommandScheduler.ObserveTerminal(
                command,
                outcome?.Result,
                outcome?.TerminalReason));
            var stockIntentOwned = ReferenceEquals(stockIntentCommand, command);
            if (stockIntentOwned)
            {
                stockIntentCommand = null;
            }
            LastOutcome = outcome;
            finishedCommandPendingSweep = command;
            if (activeCommand == command && !command.ChargeMode)
            {
                activeCommand = null;
            }
            // The native result is one fact among the command's own: a command native force-finished before it started
            // also reports Success, and must neither read as a completed attack nor keep its order alive.
            var terminal = MountedAttackTerminalPolicy.Classify(new MountedAttackTerminalFacts
            {
                NativeSuccess = string.Equals(outcome.Result, UnitCommand.ResultType.Success.ToString(), StringComparison.Ordinal),
                TransactionCompleted = outcome.TransactionCompleted,
                NativeRangedTailTermination = outcome.NativeRangedTailTermination,
                NativeAttackRuleObserved = outcome.NativeAttackRuleObserved,
                CompletedAttackCount = outcome.NativeCompletedAttackCount
            });
            // Known issues Sprint 6 (A02): the recorded cause, or a replacement by the native Run it ended inside, decides
            // whether the capability snapshot explains this end. The snapshot itself stays in the outcome for diagnostics.
            var cause = MountedAttackTerminalCausePolicy.Resolve(outcome.EndCause,
                MountedNativeRunScope.EndedInsideReplacingRun(command.Executor?.Commands, command));
            outcome.EndCause = cause;
            // Review of fa533e97 (R18): the recorded causal block, or the block of a native capability branch captured where
            // native took it. The terminal snapshot is never causal; it stays in the outcome for diagnostics.
            var causalBlock = MountedAttackTerminalCausePolicy.CausalCapability(cause, outcome.EndCauseCapabilityBlock,
                outcome.NativeTerminalBranch, outcome.NativeTerminalCapabilityBlock);
            var capabilityIsCause = causalBlock != MountedActorCapabilityBlock.None;
            // A05: only a typed, evidenced failed automatic approach records its target unreachable for the rider's brain.
            if (stockIntentOwned && stockIntent.Automatic && MountedAttackTerminalCausePolicy.RecordsAutomaticApproachFailure(true,
                    outcome.ChildAttackStartCount, outcome.NativeRangedTailTermination,
                    string.Equals(outcome.Result, UnitCommand.ResultType.Success.ToString(), StringComparison.Ordinal), cause,
                    outcome.ApproachRequiredAtStart, outcome.DelegatedMoveStartCount))
            {
                automaticApproachFailedTarget = stockIntentTarget;
            }
            LastFeedback = terminal == MountedAttackTerminalKind.Completed ||
                    terminal == MountedAttackTerminalKind.DeliveredWithoutCompletion
                ? "Mounted pair attack completed."
                : terminal == MountedAttackTerminalKind.RangedTailDelivered
                    ? "Ranged attacks completed; remaining melee attacks are out of reach."
                : terminal == MountedAttackTerminalKind.EndedWithoutExecution
                    ? DescribeUnexecutedTerminal(outcome, cause, causalBlock)
                : DescribeTerminalFailure(outcome, cause, causalBlock);
            // Native Run writes a successor's manual target right after this interruption, so the charge's own is released now.
            if (command.ChargeMode && ReferenceEquals(chargeOwner?.Command, command))
            {
                TryContain("Charge manual target release retained", () => ReleaseChargeManualTarget(chargeOwner));
            }
            TryLog(() => "Mounted combat command terminal: action=" + command.Action +
                "; result=" + outcome.Result +
                "; cause=" + cause + (outcome.EndCauseDetail != null ? "(" + outcome.EndCauseDetail + ")" : string.Empty) +
                "; causalBlock=" + causalBlock + "; nativeBranch=" + outcome.NativeTerminalBranch + "(" + outcome.NativeTerminalCapabilityBlock + ")" +
                "; terminalBlock=" + outcome.ActorCapabilityBlockAtTerminal + "; capabilityIsCause=" + capabilityIsCause +
                "; reason=" + (outcome.TerminalReason ?? "<none>") +
                "; childAttacks=" + outcome.ChildAttackStartCount +
                "; repaths=" + outcome.RepathCount +
                "; repathObservations=" + (string.IsNullOrEmpty(outcome.RepathObservations) ? "<none>" : outcome.RepathObservations) +
                "; rejectionCodes=" + string.Join(",", LastRejectionCodes.Select(code => code.ToString()).ToArray()) +
                "; transaction=" + (outcome.TransactionState ?? "<none>") +
                "; terminal=" + terminal +
                "; feedback=" + LastFeedback);
            SettleOrderTerminal(command, stockIntentOwned, terminal, cause, capabilityIsCause, outcome.NativeAttackRuleObserved,
                outcome.NativeCompletedAttackCount, outcome.Result, causalBlock.ToString(), outcome.ActorCapabilityBlockAtTerminal.ToString(),
                () => "; actorId=" + (outcome.ActorId ?? "<none>") +
                    "; targetId=" + (outcome.TargetId ?? "<none>") +
                    "; nativeResult=" + outcome.Result +
                    "; transaction=" + (outcome.TransactionState ?? "<none>") +
                    "; reason=" + (outcome.TerminalReason ?? "<none>") +
                    "; started=" + outcome.NativeCommandStarted +
                    "; sequenceStarted=" + (outcome.ChildAttackStartCount != 0) +
                    "; attackRule=" + outcome.NativeAttackRuleObserved +
                    "; attacks=" + outcome.NativeCompletedAttackCount + "/" + outcome.NativePlannedAttackCount +
                    "; cause=" + cause + "; capabilityIsCause=" + capabilityIsCause +
                    "; capability={" + (outcome.ActorCapabilityAtTerminal ?? "<none>") + "}.");
        }

        // PR-1 round-4/5 lifecycle (reviews R1-F3, R2-F4, R4-F5): the order transition of one actor's terminal, shared by
        // HandleCommandTerminal and the attack-path harness. The disposition comes from this wrapper's recorded native terminal branch
        // and its delivery facts (its own OnAction, an attack rule or completed attacks; a turn-based force-finish marks IsActed without
        // any of them), so an unstarted or interrupted rider attempt with zero delivery ends only the rider attempt and leaves the
        // generation for one horse recheck. The rider's pending refusal is settled by the horse's delivery or by why the generation
        // ended: newer work (supersession or a replacing Run) drops it silently; a horse that lost its opportunity on the still-current
        // order presents it once.
        private void SettleOrderTerminal(MountedPairAttackCommand command, bool stockIntentOwned, MountedAttackTerminalKind terminal,
            MountedAttackEndCause cause, bool capabilityIsCause, bool attackRuleObserved, int completedAttacks, string result,
            string causalBlock, string blockAtTerminal, Func<string> unexecutedDetail)
        {
            var generation = stockIntent.Generation;
            var disposition = MountedRetainedOrderLedger.DecideTerminal(new MountedOrderTerminalFacts
            {
                OwnedByOrder = stockIntentOwned,
                OrderCurrent = retainedOrder.IsCurrent(generation),
                RetainsIntent = MountedAttackTerminalPolicy.RetainsIntent(terminal),
                EndedWithoutExecution = terminal == MountedAttackTerminalKind.EndedWithoutExecution,
                TurnBased = CombatController.IsInTurnBasedCombat(),
                AutomaticOrder = stockIntent.Automatic,
                MountActorOrder = stockIntent.MountActor,
                MeleeOrder = retainedOrder.Melee,
                RiderMeleeCommand = command.Action == MountedCombatActionKind.RiderMelee,
                CapabilityIsCause = capabilityIsCause,
                TargetInvalid = cause == MountedAttackEndCause.TargetInvalid,
                Delivered = command.StrikeReleased,
                AttackRuleObserved = attackRuleObserved,
                CompletedAttacks = completedAttacks
            });
            // The horse's actual delivery settles the rider's pending refusal silently (admission alone never does).
            if (stockIntentOwned && command.Action == MountedCombatActionKind.MountPrimaryNatural && command.StrikeReleased)
                retainedOrder.SettleRefusalDelivered(generation);
            if (disposition == MountedOrderTerminalDisposition.NotOrdersCommand || disposition == MountedOrderTerminalDisposition.Keep)
            {
                return;
            }
            if (disposition == MountedOrderTerminalDisposition.RiderEndedForHorse)
            {
                retainedOrder.MarkRiderEnded(generation);
                RiderSpecificTerminalCount++;
                // Bound to this generation; presented once if the order then retires with neither actor delivering.
                retainedOrder.HoldRiderRefusal(generation, LastFeedback);
                LastStockAttackObservation = "rider-attempt-ended-actor-specific;block=" + causalBlock + ";order-retained-for-the-horse";
                return;
            }
            var endedByNewerWork = cause == MountedAttackEndCause.Replaced || retainedOrder.Superseded(generation);
            var pendingRefusal = retainedOrder.TakeRefusalOnRetirement(generation, !endedByNewerWork);
            if (pendingRefusal != null) LastFeedback = pendingRefusal;
            if (disposition == MountedOrderTerminalDisposition.Retire)
            {
                ClearStockAttackIntent("stock command terminal " + result, true);
                LastStockAttackObservation = "intent-cancel-terminal;action=" + command.Action + ";result=" + result;
                if (pendingRefusal != null)
                {
                    PendingRefusalPresentedCount++;
                    try { ShowStockFeedback(); }
                    catch (Exception exception) { TryLog(() => "Mounted attack refusal feedback was not delivered: " + exception.GetType().Name); }
                }
                return;
            }
            if (pendingRefusal != null) PendingRefusalPresentedCount++;
            // Only this intent generation and its own finished command end here: no action, cooldown, condition or turn
            // is written, the partner keeps its own orders, and nothing is remembered against a later click.
            var playerOrigin = !stockIntent.Automatic;
            StockAttackUnexecutedRetirementCount++;
            TryLog(() => "Mounted attack intent retired without execution: origin=" + (playerOrigin ? "player" : "automatic") +
                "; generation=" + generation + "; action=" + command.Action + (unexecutedDetail == null ? string.Empty : unexecutedDetail()));
            ClearStockAttackIntent("stock command ended without execution", true);
            LastStockAttackObservation = "intent-retired-unexecuted;action=" + command.Action + ";result=" + result + ";block=" + blockAtTerminal;
            // A player's order that was dropped is answered once, whatever made native drop it; an order that newer player work
            // superseded or replaced is not answered (controller-first retirement is silent the same way).
            if (playerOrigin && (!endedByNewerWork || pendingRefusal != null))
            {
                try { ShowStockFeedback(); }
                catch (Exception exception) { TryLog(() => "Mounted attack terminal feedback was not delivered: " + exception.GetType().Name); }
            }
        }

        // Known issues Sprint 6 (A02): every end of an owned pair command records why, before native Interrupt reports it.
        private void InterruptOwnedCommand(MountedPairAttackCommand command, MountedAttackEndCause cause, string reason)
        {
            command.RecordEndCause(cause, reason);
            pairedCommandScheduler.ObserveExternalInterrupt(command, reason);
            // PR #70 R12-F4 (round 15): work created by a cancellation itself. When the interruption's exact carrier stop fails inside
            // OnEnded (the fault still propagates to the caller), the command's unfinished carrier cleanup record stays owned here, so the
            // teardown drain after the lifecycle's cleanup sees it (teardown not ready, retryable) instead of losing it with the throw.
            try { command.Interrupt(); }
            finally { RetainCarrierCleanup(command); }
        }


        // Diagnostics never decide a terminal: a failing logger leaves retirement and ownership as they are.
        private void TryLog(Func<string> line)
        {
            try { logger.Info(line()); }
            catch (Exception) { }
        }

        private void TryLogException(string phase, Exception exception)
        {
            try { logger.Exception(phase, exception); }
            catch (Exception) { }
        }

        // Review of fa533e97 (R20): a recovery step that may fail is attempted, and its failure is reported best-effort.
        private void TryContain(string phase, Action step)
        {
            try { step(); }
            catch (Exception exception) { TryLogException(phase, exception); }
        }

        internal long StockAttackUnexecutedRetirementCount { get; private set; }

        internal long DoorInteractionReplacedMountMoveCount { get; private set; }

        internal long DoorInteractionRetainedClickCount { get; private set; }

        // Sprint 3 (TRAP-01, SPELL-01): the last released carried approach and its counters.
        internal MountedCarriedApproachOutcome LastCarriedApproachOutcome { get; private set; }

        internal long CarriedApproachBindCount { get; private set; }

        internal long CarriedApproachReleaseCount { get; private set; }

        internal long CarriedApproachExhaustedCount { get; private set; }

        internal string LastCarriedApproachObservation { get; private set; } = "not-observed";

        // F4 review MINOR-2: native UnitCommands.InterruptMove interrupts every command still approaching (IsMoveUnit).
        // The rider's own approaching orders never run native TickApproaching (the mount carries them), so the trap-order
        // group stop ends them here: an unstarted carried command, and a pair attack still approaching before any strike.
        internal int InterruptRiderApproachingOrders(string reason)
        {
            if (disposed) return 0;
            var count = 0;
            var carried = carriedApproach?.Command;
            if (carried != null && !carried.IsStarted && !carried.IsFinished) { carried.Interrupt(); count++; }
            var active = activeCommand;
            if (active != null && !active.ChargeMode && active.IsApproachingBeforeAttack)
            {
                if (HasStockAttackIntent && ReferenceEquals(stockIntentCommand, active)) ClearStockAttackIntent(reason, true);
                activeCommand = null;
                InterruptOwnedCommand(active, MountedAttackEndCause.Cancelled, reason);
                count++;
            }
            if (count > 0) logger.Info("Rider approaching orders interrupted by the native group stop: count=" + count + "; reason=" + reason + ".");
            return count;
        }

        internal long CarriedApproachReplacedTravelCount { get; private set; }

        // Exact native component types only: a derived chest, loot or door type is "Other" and stays native.
        // A corpse click is the exact native UnitLootUnit (a Standard command on the dead unit, ApproachRadius 1 m,
        // opened by its own OnAction); a derived command type stays native.
        internal static MountedCarriedInteractionClass ClassifyCarriedInteraction(UnitCommand command, UnitInteractWithObject interaction)
        {
            if (command != null && command.GetType() == typeof(UnitLootUnit))
                return command.TargetUnit != null ? MountedCarriedInteractionClass.UnitLoot : MountedCarriedInteractionClass.NotInteraction;
            var component = interaction?.Interaction;
            if (component == null || command.GetType() != typeof(UnitInteractWithObject)) return MountedCarriedInteractionClass.NotInteraction;
            if (component is DisableTrapInteractionComponent) return MountedCarriedInteractionClass.TrapDisarm;
            if (component is StandardDoor) return MountedCarriedInteractionClass.Door;
            var type = component.GetType();
            return component.Type == InteractionType.Approach && (type == typeof(StandardChest) || type == typeof(LootComponent))
                ? MountedCarriedInteractionClass.Container
                : MountedCarriedInteractionClass.Other;
        }

        // Called from the TickApproaching prefix for every unstarted native command. Returns true when the mount
        // approaches for the rider's own command this tick and the native rider approach must be skipped; false
        // leaves the native code untouched (not a carried command, the command is in range, or another pair
        // operation owns the mount, in which case the command waits natively exactly as before).
        internal bool TryTickCarriedApproach(UnitCommand command, bool relationshipControlShell)
        {
            if (disposed || command == null) return false;
            var ability = command as UnitUseAbility;
            var interaction = command as UnitInteractWithObject;
            var kind = MountedCarriedApproachPolicy.Classify(ability != null, ClassifyCarriedInteraction(command, interaction));
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
                // F4 review MAJOR-1: a new ordinary chest/loot order replaces the pair's own real-time travel, as native Run
                // makes an unmounted unit's own move yield to its new Standard command (and as a door click does). Only the
                // exact travel the mount held when this order ran (ObserveContainerOrderRun), once, so a later player move,
                // a pair operation's delegated move or a charge carrier is never cancelled for an older waiting order.
                var boundTravel = ReferenceEquals(containerOrderCommand, command) ? containerOrderTravel : null;
                var firstTick = boundTravel != null;
                containerOrderTravel = null;
                var mountCommands = relationship.Mount?.Commands;
                var mountMove = mountCommands?.GetCommand(UnitCommand.CommandType.Move);
                if (MountedCarriedApproachPolicy.NewOrderReplacesOwnTravel(kind, firstTick, CombatController.IsInTurnBasedCombat(), command.CreatedByPlayer,
                        mountMove != null && ReferenceEquals(mountMove, boundTravel) && !mountMove.IsFinished,
                        mountCommands != null && (mountCommands.Standard != null || mountCommands.GetCommand(UnitCommand.CommandType.Free) != null ||
                            mountCommands.GetCommand(UnitCommand.CommandType.Swift) != null),
                        mountCommands != null && mountCommands.Queue.Count == 0))
                {
                    mountMove.Interrupt();
                    if (mountCommands.Queue.Count == 0) mountCommands.RemoveFinishedAndUpdateQueue();
                    relationship.Mount.View?.StopMoving();
                    CarriedApproachReplacedTravelCount++;
                    logger.Info("Mounted container order replaced the " + MountDisplayName + " travel: command=" + command.GetType().Name + ".");
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
            if (admitted)
            {
                ObserveContainerOrderRun(container, routed);
            }
            return admitted;
        }

        // F4 re-review MAJOR: the exact mount travel a new chest/loot order may replace is the one the mount held when the
        // rider's order ran (native Run makes a unit's own earlier move yield to its new Standard command). A move issued
        // later, a pair operation's own delegated move or a charge carrier is never this instance, so it is never cancelled.
        private void ObserveContainerOrderRun(UnitCommands container, UnitCommand command)
        {
            var interaction = command as UnitInteractWithObject;
            if (disposed || command == null || container == null || container != relationship.Rider?.Commands ||
                relationship.State != RelationshipState.Mounted || !command.CreatedByPlayer ||
                MountedCarriedApproachPolicy.Classify(false, ClassifyCarriedInteraction(command, interaction)) != MountedCarriedCommandKind.ContainerInteraction)
                return;
            var mountMove = relationship.Mount?.Commands?.GetCommand(UnitCommand.CommandType.Move);
            var ownTravel = mountMove is UnitMoveTo && IsPairPlayerOrder(mountMove) && !mountMove.IsFinished && !(mountMove is MountedDoorApproachMove) &&
                !HasActiveCommand && !HasActiveDoorInteraction && chargeOwner == null && !OwnsExactPairedNativeCommand(mountMove);
            containerOrderCommand = command;
            containerOrderTravel = ownTravel ? mountMove : null;
        }

        private UnitCommand containerOrderCommand, containerOrderTravel;

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
            public bool IsPlayerRequest(object command) => command is UnitCommand unit && owner.IsPairPlayerOrder(unit) && unit.AiAction == null;

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

        internal long CarriedApproachTerminalCount { get; private set; }

        private void HandleCarriedApproachTerminal(MountedCarriedApproach approach, MountedCarriedApproachOutcome outcome)
        {
            LastCarriedApproachOutcome = outcome;
            CarriedApproachTerminalCount++;
            if (ReferenceEquals(carriedApproach, approach)) carriedApproach = null;
            if (outcome.TerminalReason != null && !outcome.Satisfied)
            {
                CarriedApproachExhaustedCount++;
                LastFeedback = MountedCarriedApproachPolicy.ExhaustedFeedback(
                    MountedCarriedApproachPolicy.ParseKind(outcome.Kind), MountDisplayName, outcome.TerminalReason);
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
            var mountMoveIsPlayerMove = mountMove is UnitMoveTo && IsPairPlayerOrder(mountMove) &&
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

        private string DescribeTerminalFailure(MountedPairAttackOutcome outcome, MountedAttackEndCause cause, MountedActorCapabilityBlock causalBlock)
        {
            // Known issues Sprint 6 (A02): an explicit cancellation or replacement is named as such, whatever the actor's
            // state was when it happened; the capability text needs the capability to be the cause.
            var cancelled = MountedAttackTerminalCausePolicy.CancellationFeedback(cause);
            if (cancelled != null)
            {
                LastRejectionCodes = new[] { MountedCombatRejectionCode.CommandCancelled };
                return cancelled;
            }
            if (causalBlock != MountedActorCapabilityBlock.None)
            {
                LastRejectionCodes = new[] { MountedCombatRejectionCode.ActorCannotAct };
                return MountedActorCapabilityPolicy.Feedback(outcome.ActionActorName, causalBlock) +
                    (outcome.NativeCompletedAttackCount > 0 ? " The remaining attacks were not performed." : " The attack was not performed.");
            }
            // Review of fa533e97 (R18): a target branch native took (a dead target, or one out of state, untargetable or
            // stealthed) is named as such, whatever the actor's condition.
            if (cause == MountedAttackEndCause.Unrecorded && MountedNativeTerminalBranchPolicy.IsTarget(outcome.NativeTerminalBranch))
            {
                LastRejectionCodes = new[] { MountedCombatRejectionCode.TargetInvalid };
                return "Mounted rider melee ended: the exact target became invalid before completion.";
            }
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

        // Native reported Success for a command that executed nothing.
        private string DescribeUnexecutedTerminal(MountedPairAttackOutcome outcome, MountedAttackEndCause cause, MountedActorCapabilityBlock causalBlock)
        {
            var cancelled = MountedAttackTerminalCausePolicy.CancellationFeedback(cause);
            if (cancelled != null)
            {
                LastRejectionCodes = new[] { MountedCombatRejectionCode.CommandCancelled };
                return cancelled;
            }
            LastRejectionCodes = new[]
            {
                causalBlock != MountedActorCapabilityBlock.None ? MountedCombatRejectionCode.ActorCannotAct : MountedCombatRejectionCode.WrongActionState
            };
            return MountedAttackTerminalPolicy.UnexecutedFeedback(outcome.ActionActorName, causalBlock);
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

        // PR #70 R12-F4 (round 16): the player's turn-based ground order on the mount is native's own command (ClickGroundHandler's
        // CreatedByPlayer UnitMoveTo, or native's cannot-attack click approach, run on the mount after RouteGroundCommand rewrote the rider);
        // the pair only admitted and accounted for it (and, without paired activation, ticked it). Mod disable ends that involvement: the
        // admission, owner and accounting references are released and the command keeps running.
        internal int ModDisableGroundMoveReleases { get; private set; }
        private void ReleaseRiderTurnGroundMovementForModDisable()
        {
            riderTurnGroundMoveAdmissionPending = false;
            groundMoveOwner = null;
            if (activeRiderTurnGroundMove != null) ModDisableGroundMoveReleases++;
            activeRiderTurnGroundMove = null;
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
