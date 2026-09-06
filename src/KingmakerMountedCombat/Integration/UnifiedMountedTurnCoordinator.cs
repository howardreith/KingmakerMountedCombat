using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker;
using Kingmaker.Controllers.Combat;
using Kingmaker.Controllers.Units;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Commands.Base;
using KingmakerMountedCombat.Diagnostics;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Logging;
using TurnBased.Controllers;

namespace KingmakerMountedCombat.Integration
{
    internal sealed class MountedActionLedgerSnapshot
    {
        public string UnitId { get; set; }
        public float Initiative { get; set; }
        public float Standard { get; set; }
        public float Move { get; set; }
        public float Swift { get; set; }
        public float AttackOfOpportunity { get; set; }
        public bool HasStandard { get; set; }
        public bool HasMove { get; set; }
        public bool HasSwift { get; set; }
    }

    internal sealed class UnifiedMountedTurnSnapshot
    {
        public bool Enabled { get; set; }
        public string RelationshipState { get; set; }
        public bool TurnBased { get; set; }
        public int Round { get; set; }
        public string CurrentTurnUnitId { get; set; }
        public string SharedInitiativeOwnerId { get; set; }
        public int SharedInitiativeValue { get; set; }
        public int SharedInitiativeBonus { get; set; }
        public MountedActionLedgerSnapshot Rider { get; set; }
        public MountedActionLedgerSnapshot Mount { get; set; }
        public bool NativeFiveFootStepEnabled { get; set; }
        public float NativeFiveFootStepMeters { get; set; }
        public bool PendingSplit { get; set; }
        public int PendingSplitRound { get; set; }
        public long RedundantMountTurnSkipCount { get; set; }
        public long DeferredMountTurnSkipCount { get; set; }
        public long PostTickMountTurnSkipCount { get; set; }
        public long MountLedgerPrepareCount { get; set; }
        public long MirroredInitiativeCount { get; set; }
        public long MountInitiativeOverrideCount { get; set; }
        public long TrackerMountFilterCount { get; set; }
        public long SharedTurnRetentionCount { get; set; }
        public long StepOpportunityCandidateCount { get; set; }
        public long StepOpportunitySuppressionCount { get; set; }
        public long OrdinaryMovementOpportunityPassThroughCount { get; set; }
        public long MountCommandAdmissionCount { get; set; }
        public long ArchitectureFallbackCount { get; set; }
        public string LastInitiativeObservation { get; set; }
        public string LastSplitObservation { get; set; }
        public string LastMovementObservation { get; set; }
        public string LastStepOpportunityObservation { get; set; }
        public string LastTurnCandidateObservation { get; set; }
    }

    internal sealed class UnifiedMountedTurnCoordinator : IDisposable
    {
        private const int NextUnitFieldToken = 0x04000652;
        private const int ChooseNextUnitToken = 0x06000BD2;
        private const int SortUnitsToken = 0x06000BDD;
        private const int InitiativeOverrideResultFieldToken = 0x04004B5B;
        private static readonly FieldInfo NextUnitField = ResolveField(
            typeof(CombatController), "m_NextUnit", NextUnitFieldToken);
        private static readonly MethodInfo ChooseNextUnitMethod = ResolveMethod(
            typeof(CombatController), "ChooseNextUnit", ChooseNextUnitToken, Type.EmptyTypes);
        private static readonly MethodInfo SortUnitsMethod = ResolveMethod(
            typeof(CombatController), "SortUnits", SortUnitsToken, new[] { typeof(string) });
        private static readonly FieldInfo InitiativeOverrideResultField = ResolveField(
            typeof(RuleInitiativeRoll), "m_OverrideResult", InitiativeOverrideResultFieldToken);

        [ThreadStatic]
        private static int trackerProjectionDepth;

        private readonly GameMountedRelationshipService relationship;
        private readonly MountedMovePrepayment movePrepayment = new MountedMovePrepayment();
        private readonly DiagnosticSettings settings;
        private readonly MountedPairCommandScheduler pairedCommandScheduler;
        private readonly IModLogger logger;
        private MountedCombatController combat;
        private TurnController preparedRiderTurn;
        private UnitEntityData pendingSplitMount;
        private int pendingSplitRound = -1;
        private bool chooseNextReentry;
        private bool lastTurnBased;
        private bool disposed;
        private long mountCommandEligibilityEncounterCount;
        private long mountCommandEligibilityStockTrueCount;
        private long mountCommandEligibilityStockFalseCount;
        private int mountCommandEligibilityFirstFrame = -1;
        private int mountCommandEligibilityLastFrame = -1;
        private string lastMountCommandEligibilityObservation = "not-observed";

        public UnifiedMountedTurnCoordinator(
            GameMountedRelationshipService relationship,
            DiagnosticSettings settings,
            MountedPairCommandScheduler pairedCommandScheduler,
            IModLogger logger)
        {
            this.relationship = relationship ?? throw new ArgumentNullException(nameof(relationship));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.pairedCommandScheduler = pairedCommandScheduler ??
                throw new ArgumentNullException(nameof(pairedCommandScheduler));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            relationship.MountedPairActivated += HandleMountedPairActivated;
            relationship.Dismounting += HandleDismounting;
        }

        internal bool Enabled => settings.EnableUnifiedMountedTurn;

        internal long RedundantMountTurnSkipCount { get; private set; }

        internal long DeferredMountTurnSkipCount { get; private set; }

        internal long PostTickMountTurnSkipCount { get; private set; }

        internal long MountLedgerPrepareCount { get; private set; }

        internal long MirroredInitiativeCount { get; private set; }

        internal long MountInitiativeOverrideCount { get; private set; }

        internal long TrackerMountFilterCount { get; private set; }

        internal long SharedTurnRetentionCount { get; private set; }

        internal long StepOpportunitySuppressionCount { get; private set; }

        internal long StepOpportunityCandidateCount { get; private set; }

        internal long OrdinaryMovementOpportunityPassThroughCount { get; private set; }

        internal long MountCommandAdmissionCount { get; private set; }

        internal long ArchitectureFallbackCount { get; private set; }

        internal string LastInitiativeObservation { get; private set; } = "not-observed";

        internal string LastSplitObservation { get; private set; } = "not-observed";

        internal string LastMovementObservation { get; private set; } = "not-observed";

        internal string LastStepOpportunityObservation { get; private set; } = "not-observed";

        internal string LastTurnCandidateObservation { get; private set; } = "not-observed";

        internal void BindCombat(MountedCombatController controller)
        {
            if (combat != null || controller == null)
            {
                throw new InvalidOperationException("Unified mounted turn combat binding must occur exactly once.");
            }
            combat = controller;
        }

        internal void Update()
        {
            if (disposed)
            {
                return;
            }

            var turnBased = CombatController.IsInTurnBasedCombat();
            var controller = Game.Instance?.TurnBasedCombatController;
            if (UnifiedMountedTurnPolicy.ShouldRestoreSplitParticipation(
                    pendingSplitMount != null,
                    turnBased,
                    controller?.RoundNumber ?? 0,
                    pendingSplitRound))
            {
                LastSplitObservation = "restored;mount=" + (pendingSplitMount?.UniqueId ?? "<none>") +
                    ";round=" + (controller?.RoundNumber.ToString() ?? "<none>");
                pendingSplitMount = null;
                pendingSplitRound = -1;
            }

            if (Enabled && relationship.State == RelationshipState.Mounted &&
                relationship.Rider != null && relationship.Mount != null)
            {
                if (turnBased)
                {
                    MirrorExactInitiative(relationship.Mount, "update");
                }
                else if (lastTurnBased)
                {
                    relationship.Mount.CombatState.Cooldown.Initiative =
                        relationship.Rider.CombatState.Cooldown.Initiative;
                    LastInitiativeObservation = "tb-to-rt;owner=" + relationship.Rider.UniqueId +
                        ";mount=" + relationship.Mount.UniqueId +
                        ";cooldown=" + relationship.Rider.CombatState.Cooldown.Initiative.ToString("R");
                }
            }

            lastTurnBased = turnBased;
        }

        internal void HandleChooseNextUnit(CombatController controller)
        {
            if (disposed || chooseNextReentry || controller == null || !Enabled ||
                !CombatController.IsInTurnBasedCombat())
            {
                return;
            }

            TryAdvancePastExactMount(controller, "choose-next-postfix");
        }

        internal void HandleCombatControllerTickCompleted(CombatController controller)
        {
            if (disposed || chooseNextReentry || controller == null || !Enabled ||
                !CombatController.IsInTurnBasedCombat())
            {
                return;
            }

            TryAdvancePastExactMount(controller, "combat-tick-postfix");
        }

        private void TryAdvancePastExactMount(CombatController controller, string source)
        {
            var candidate = NextUnitField.GetValue(controller) as UnitEntityData;
            var mustSkip = ShouldSuppressMount(candidate, controller.RoundNumber);
            if (!UnifiedMountedTurnPolicy.ShouldAdvancePastSkippedCandidate(
                    mustSkip,
                    controller.CurrentTurn == null))
            {
                if (mustSkip && controller.CurrentTurn != null)
                {
                    DeferredMountTurnSkipCount++;
                    LastTurnCandidateObservation = "deferred;source=" + source +
                        ";mount=" + candidate.UniqueId +
                        ";current=" + (controller.CurrentTurn.Unit?.UniqueId ?? "<none>") +
                        ";status=" + controller.CurrentTurn.Status +
                        ";round=" + controller.RoundNumber;
                }
                return;
            }

            var skipped = candidate;
            try
            {
                chooseNextReentry = true;
                ChooseNextUnitMethod.Invoke(controller, null);
            }
            catch (Exception exception)
            {
                EnterFallback("ChooseNextUnit invocation failed", exception);
                return;
            }
            finally
            {
                chooseNextReentry = false;
            }

            var replacement = NextUnitField.GetValue(controller) as UnitEntityData;
            if (replacement == skipped)
            {
                EnterFallback("ChooseNextUnit retained the exact redundant mount", null);
                return;
            }

            RedundantMountTurnSkipCount++;
            if (string.Equals(source, "combat-tick-postfix", StringComparison.Ordinal))
            {
                PostTickMountTurnSkipCount++;
            }
            LastTurnCandidateObservation = "skipped;source=" + source +
                ";mount=" + skipped.UniqueId +
                ";replacement=" + (replacement?.UniqueId ?? "<none>") +
                ";round=" + controller.RoundNumber;
            logger.Info("Unified mounted turn skipped exact redundant mount candidate: mountId=" +
                skipped.UniqueId + "; replacementId=" + (replacement?.UniqueId ?? "<none>") +
                "; round=" + controller.RoundNumber + "; source=" + source + ".");
        }

        internal void HandleTurnPrepared(TurnController turn)
        {
            if (disposed || turn == null)
            {
                return;
            }

            var mount = relationship.Mount;
            var controller = Game.Instance?.TurnBasedCombatController;
            if (!settings.EnableUnifiedMountedTurn && controller != null && CombatController.IsInTurnBasedCombat() &&
                (turn.Unit == mount && relationship.State == RelationshipState.Mounted || movePrepayment.Owns(turn.Unit)))
            {
                movePrepayment.ObserveEpoch(turn.Unit, controller, controller.RoundNumber, controller.RoundStartTime.Ticks);
                var cooldown = turn.Unit.CombatState.Cooldown;
                cooldown.MoveAction = movePrepayment.ReconcileNativePreparation(cooldown.MoveAction);
            }
            var mountState = mount?.Descriptor?.State;
            if (!UnifiedMountedTurnPolicy.ShouldPrepareMountLedger(
                    Enabled,
                    relationship.State == RelationshipState.Mounted,
                    turn.Unit == relationship.Rider,
                    mount != null && mount.IsInCombat && mountState != null &&
                        mountState.IsConscious && !mountState.IsFinallyDead,
                    ReferenceEquals(preparedRiderTurn, turn)))
            {
                return;
            }

            preparedRiderTurn = turn;
            PrepareExactMountLedger(mount);
        }

        internal void ExtendTurnIfMountActionable(TurnController turn, ref bool result)
        {
            if (disposed || result || turn == null)
            {
                return;
            }

            var mount = relationship.Mount;
            var state = mount?.Descriptor?.State;
            var mountAliveAndAble = mount != null && state != null && state.IsConscious &&
                !state.IsFinallyDead && mount.IsAbleToAct();
            var mountMovementAvailable = mountAliveAndAble && state.CanMove &&
                mount.CombatState.Cooldown.MoveAction < 6f && !mount.UsedTwoMoveAction();
            if (!UnifiedMountedTurnPolicy.ShouldKeepRiderTurnOpen(
                    Enabled,
                    relationship.State == RelationshipState.Mounted,
                    turn.Unit == relationship.Rider,
                    turn.IsActing,
                    mountAliveAndAble,
                    mountAliveAndAble && mount.HasStandardAction(),
                    mountMovementAvailable,
                    mountAliveAndAble && mount.HasSwiftAction(),
                    combat != null && combat.HasActiveCommand))
            {
                return;
            }

            result = true;
            SharedTurnRetentionCount++;
        }

        internal void MirrorInitiativeEvent(RuleInitiativeRoll rule)
        {
            if (disposed || rule == null)
            {
                return;
            }

            var unit = rule.Initiator;
            if (!UnifiedMountedTurnPolicy.ShouldMirrorInitiative(
                    Enabled,
                    relationship.State == RelationshipState.Mounted,
                    relationship.Rider?.CombatState != null && relationship.Rider.CombatState.Prepared,
                    unit != null && unit == relationship.Mount))
            {
                return;
            }

            var rider = relationship.Rider;
            var nativeMountResult = rule.Result;
            try
            {
                // UnitCombatPrepareController has already populated both combat states when
                // this native event is delivered. Override the hidden mount rule before the
                // controller caches it, then mirror the rider tie-break and cooldown values.
                // Future mode transitions therefore cannot resurrect the discarded result.
                InitiativeOverrideResultField.SetValue(rule, rider.CombatState.Initiative);
                MirrorExactInitiative(unit, "native-initiative-event");
                MountInitiativeOverrideCount++;
                LastInitiativeObservation = "native-initiative-event;owner=" + rider.UniqueId +
                    ";mount=" + unit.UniqueId + ";nativeMountResult=" + nativeMountResult +
                    ";sharedResult=" + rider.CombatState.Initiative +
                    ";riderBonus=" + rider.Stats.Initiative.ModifiedValue +
                    ";ruleOverridden=" + rule.IsOverriden;
            }
            catch (Exception exception)
            {
                EnterFallback("RuleInitiativeRoll result override failed", exception);
            }
        }

        internal bool BeginTrackerProjection()
        {
            if (disposed || !Enabled || !CombatController.IsInTurnBasedCombat())
            {
                return false;
            }

            trackerProjectionDepth++;
            return true;
        }

        internal void EndTrackerProjection(bool entered)
        {
            if (!entered)
            {
                return;
            }

            trackerProjectionDepth = Math.Max(0, trackerProjectionDepth - 1);
        }

        internal void FilterTrackerSortedUnits(ref IEnumerable<UnitEntityData> units)
        {
            if (disposed || trackerProjectionDepth <= 0 || units == null)
            {
                return;
            }

            var controller = Game.Instance?.TurnBasedCombatController;
            var original = units.ToArray();
            var filtered = original.Where(unit => !ShouldSuppressMount(
                unit,
                controller?.RoundNumber ?? 0)).ToArray();
            if (filtered.Length == original.Length)
            {
                return;
            }

            units = filtered;
            TrackerMountFilterCount += original.Length - filtered.Length;
        }

        internal void AdmitExactMountCommand(UnitCommand command, ref bool result)
        {
            if (disposed || command == null)
            {
                return;
            }

            var turn = Game.Instance?.TurnBasedCombatController?.CurrentTurn;
            var exactOwnedPairCommand = combat != null && combat.OwnsExactUnifiedMountCommand(command);
            if (exactOwnedPairCommand)
            {
                ObserveExactMountCommandEligibility(command, result, turn);
            }
            if (!exactOwnedPairCommand ||
                !pairedCommandScheduler.TryExtendNativeEligibility(
                    command,
                    result,
                    ref result))
            {
                return;
            }

            MountCommandAdmissionCount++;
        }

        internal string DescribeMountCommandEligibilityObservation()
        {
            var game = Game.Instance;
            var turn = game?.TurnBasedCombatController?.CurrentTurn;
            var mount = relationship.Mount;
            var mountCommands = mount?.Commands;
            return "pairedSchedulerEnabled=" + settings.EnablePairedCommandScheduler +
                ";encounterCount=" + mountCommandEligibilityEncounterCount +
                ";stockTrueCount=" + mountCommandEligibilityStockTrueCount +
                ";stockFalseCount=" + mountCommandEligibilityStockFalseCount +
                ";firstFrame=" + mountCommandEligibilityFirstFrame +
                ";lastFrame=" + mountCommandEligibilityLastFrame +
                ";mountIsAwakeNow=" + (mount != null && mount.IsAwake) +
                ";mountInAwakeUnitsNow=" + (mount != null && game?.State?.AwakeUnits != null &&
                    game.State.AwakeUnits.Contains(mount)) +
                ";currentTurnUnitNow=" + (turn?.Unit?.UniqueId ?? "<none>") +
                ";currentTurnStatusNow=" + (turn == null ? "<none>" : turn.Status.ToString()) +
                ";currentTurnActingNow=" + (turn != null && turn.IsActing) +
                ";currentTurnEndingNow=" + (turn != null && turn.IsEnding) +
                ";mountStandardTypeNow=" + (mountCommands?.Standard == null
                    ? "<none>"
                    : mountCommands.Standard.GetType().FullName) +
                ";mountQueueCountNow=" + (mountCommands?.Queue.Count ?? -1) +
                ";admissionOverrideCount=" + MountCommandAdmissionCount +
                ";schedulerDriveCount=" + pairedCommandScheduler.DriveCount +
                ";scheduler={" + pairedCommandScheduler.Describe() + "}" +
                ";last={" + lastMountCommandEligibilityObservation + "}";
        }

        private void ObserveExactMountCommandEligibility(
            UnitCommand command,
            bool stockResult,
            TurnController turn)
        {
            var frame = UnityEngine.Time.frameCount;
            if (mountCommandEligibilityEncounterCount == 0)
            {
                mountCommandEligibilityFirstFrame = frame;
            }
            mountCommandEligibilityEncounterCount++;
            mountCommandEligibilityLastFrame = frame;
            if (stockResult)
            {
                mountCommandEligibilityStockTrueCount++;
            }
            else
            {
                mountCommandEligibilityStockFalseCount++;
            }

            var game = Game.Instance;
            var mount = relationship.Mount;
            var commands = mount?.Commands;
            lastMountCommandEligibilityObservation =
                "frame=" + frame +
                ",stockResult=" + stockResult +
                ",commandType=" + command.GetType().FullName +
                ",executorId=" + (command.Executor?.UniqueId ?? "<none>") +
                ",mountId=" + (mount?.UniqueId ?? "<none>") +
                ",executorIsMount=" + (command.Executor == mount) +
                ",inStandardSlot=" + (commands != null && ReferenceEquals(commands.Standard, command)) +
                ",inQueue=" + (commands != null && commands.Queue.Contains(command)) +
                ",mountIsAwake=" + (mount != null && mount.IsAwake) +
                ",mountInAwakeUnits=" + (mount != null && game?.State?.AwakeUnits != null &&
                    game.State.AwakeUnits.Contains(mount)) +
                ",currentTurnUnit=" + (turn?.Unit?.UniqueId ?? "<none>") +
                ",currentTurnIsRider=" + (turn?.Unit == relationship.Rider) +
                ",turnStatus=" + (turn == null ? "<none>" : turn.Status.ToString()) +
                ",turnActing=" + (turn != null && turn.IsActing) +
                ",turnEnding=" + (turn != null && turn.IsEnding) +
                ",started=" + command.IsStarted +
                ",running=" + command.IsRunning +
                ",acted=" + command.IsActed +
                ",finished=" + command.IsFinished +
                ",result=" + command.Result;
        }

        internal void TickMountMovement(TurnController turn, ref float deltaTime)
        {
            if (turn == null || relationship.Rider?.CombatState == null ||
                relationship.Mount?.CombatState == null)
            {
                throw new InvalidOperationException("Exact rider and mount cooldown ledgers are required.");
            }

            var riderCooldown = relationship.Rider.CombatState.Cooldown;
            var mountCooldown = relationship.Mount.CombatState.Cooldown;
            var riderBefore = riderCooldown.MoveAction;
            var riderStandardBefore = riderCooldown.StandardAction;
            var mountBefore = mountCooldown.MoveAction;
            var controller = Game.Instance?.TurnBasedCombatController;
            if (!settings.EnableUnifiedMountedTurn && controller != null)
                movePrepayment.ObserveEpoch(relationship.Mount, controller, controller.RoundNumber, controller.RoundStartTime.Ticks);
            var physicalDeltaRequested = deltaTime;
            var nativeFiveFootStep = turn.EnabledFiveFootStep;
            var riderSpeed = relationship.Rider.CurrentSpeedMps;
            var mountSpeed = relationship.Mount.CurrentSpeedMps;
            var riderLedgerDelta = MountedFiveFootStepPolicy.ToRiderLedgerDelta(
                physicalDeltaRequested,
                riderSpeed,
                mountSpeed,
                nativeFiveFootStep);
            var temporaryAfter = mountBefore;
            try
            {
                riderCooldown.MoveAction = mountBefore;
                // Native Standard-to-movement conversion must consult the mount's Standard.
                // This scoped projection never lends the rider's unused Standard to the mount.
                riderCooldown.StandardAction = mountCooldown.StandardAction;
                ExactTurnMovementAdapter.Tick(turn, ref riderLedgerDelta);
                temporaryAfter = riderCooldown.MoveAction;
            }
            finally
            {
                riderCooldown.MoveAction = riderBefore;
                riderCooldown.StandardAction = riderStandardBefore;
            }

            mountCooldown.MoveAction = MountedFiveFootStepPolicy.TransferMoveCooldown(
                riderBefore,
                temporaryAfter,
                mountBefore);
            if (!settings.EnableUnifiedMountedTurn && controller != null)
                movePrepayment.RecordPhysicalMove(mountBefore, mountCooldown.MoveAction);
            deltaTime = Math.Min(
                physicalDeltaRequested,
                MountedFiveFootStepPolicy.ToMountPhysicalDelta(
                    riderLedgerDelta,
                    riderSpeed,
                    mountSpeed,
                    nativeFiveFootStep));
            LastMovementObservation = "riderMove=" + riderBefore.ToString("R") + "->" +
                riderCooldown.MoveAction.ToString("R") + ";mountMove=" + mountBefore.ToString("R") +
                "->" + mountCooldown.MoveAction.ToString("R") + ";fiveFoot=" +
                nativeFiveFootStep + ";distance=" + turn.MetersMovedByFiveFootStep.ToString("R") +
                ";physicalDelta=" + physicalDeltaRequested.ToString("R") + "->" + deltaTime.ToString("R") +
                ";ledgerDelta=" + riderLedgerDelta.ToString("R") +
                ";speed=" + riderSpeed.ToString("R") + "->" + mountSpeed.ToString("R");
        }

        internal bool ShouldSuppressStepOpportunity(UnitEntityData target)
        {
            var turn = Game.Instance?.TurnBasedCombatController?.CurrentTurn;
            var ordinaryMovementAlreadyUsed = turn != null &&
                turn.TimeMoved > turn.TimeMovedByFiveFootStep + 0.001f;
            var exactMountedMovementCandidate = Enabled &&
                relationship.State == RelationshipState.Mounted &&
                CombatController.IsInTurnBasedCombat() &&
                turn?.Unit == relationship.Rider &&
                target != null && target == relationship.Mount &&
                combat != null && combat.HasExactMountMovement;
            if (exactMountedMovementCandidate)
            {
                StepOpportunityCandidateCount++;
            }
            var suppress = MountedFiveFootStepPolicy.ShouldSuppressDisengageOpportunity(
                    Enabled,
                    relationship.State == RelationshipState.Mounted,
                    CombatController.IsInTurnBasedCombat(),
                    turn?.Unit == relationship.Rider,
                    target != null && target == relationship.Mount,
                    combat != null && combat.HasExactMountMovement,
                    turn != null && turn.EnabledFiveFootStep,
                    ordinaryMovementAlreadyUsed,
                    turn?.MetersMovedByFiveFootStep ?? float.PositiveInfinity,
                    TurnController.MetersOfFiveFootStep);
            LastStepOpportunityObservation = "candidate=" + exactMountedMovementCandidate +
                ";suppressed=" + suppress +
                ";fiveFoot=" + (turn != null && turn.EnabledFiveFootStep) +
                ";ordinaryUsed=" + ordinaryMovementAlreadyUsed +
                ";meters=" + (turn?.MetersMovedByFiveFootStep.ToString("R") ?? "<none>") +
                ";target=" + (target?.UniqueId ?? "<none>");
            if (!suppress)
            {
                if (exactMountedMovementCandidate && (turn == null || !turn.EnabledFiveFootStep))
                {
                    OrdinaryMovementOpportunityPassThroughCount++;
                }
                return false;
            }

            StepOpportunitySuppressionCount++;
            return true;
        }

        internal UnifiedMountedTurnSnapshot CaptureSnapshot()
        {
            var controller = Game.Instance?.TurnBasedCombatController;
            var turn = controller?.CurrentTurn;
            var rider = relationship.Rider;
            return new UnifiedMountedTurnSnapshot
            {
                Enabled = Enabled,
                RelationshipState = relationship.State.ToString(),
                TurnBased = CombatController.IsInTurnBasedCombat(),
                Round = controller?.RoundNumber ?? 0,
                CurrentTurnUnitId = turn?.Unit?.UniqueId,
                SharedInitiativeOwnerId = rider?.UniqueId,
                SharedInitiativeValue = rider?.CombatState?.Initiative ?? 0,
                SharedInitiativeBonus = rider?.Stats?.Initiative?.ModifiedValue ?? 0,
                Rider = CaptureLedger(rider),
                Mount = CaptureLedger(relationship.Mount),
                NativeFiveFootStepEnabled = turn != null && turn.EnabledFiveFootStep,
                NativeFiveFootStepMeters = turn?.MetersMovedByFiveFootStep ?? 0f,
                PendingSplit = pendingSplitMount != null,
                PendingSplitRound = pendingSplitRound,
                RedundantMountTurnSkipCount = RedundantMountTurnSkipCount,
                DeferredMountTurnSkipCount = DeferredMountTurnSkipCount,
                PostTickMountTurnSkipCount = PostTickMountTurnSkipCount,
                MountLedgerPrepareCount = MountLedgerPrepareCount,
                MirroredInitiativeCount = MirroredInitiativeCount,
                MountInitiativeOverrideCount = MountInitiativeOverrideCount,
                TrackerMountFilterCount = TrackerMountFilterCount,
                SharedTurnRetentionCount = SharedTurnRetentionCount,
                StepOpportunityCandidateCount = StepOpportunityCandidateCount,
                StepOpportunitySuppressionCount = StepOpportunitySuppressionCount,
                OrdinaryMovementOpportunityPassThroughCount = OrdinaryMovementOpportunityPassThroughCount,
                MountCommandAdmissionCount = MountCommandAdmissionCount,
                ArchitectureFallbackCount = ArchitectureFallbackCount,
                LastInitiativeObservation = LastInitiativeObservation,
                LastSplitObservation = LastSplitObservation,
                LastMovementObservation = LastMovementObservation,
                LastStepOpportunityObservation = LastStepOpportunityObservation,
                LastTurnCandidateObservation = LastTurnCandidateObservation
            };
        }

        internal void ObserveModeChanged(bool enabled)
        {
            lastTurnBased = enabled;
            if (!enabled)
            {
                preparedRiderTurn = null;
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            relationship.MountedPairActivated -= HandleMountedPairActivated;
            relationship.Dismounting -= HandleDismounting;
            combat = null;
            preparedRiderTurn = null;
            pendingSplitMount = null;
            disposed = true;
        }

        private void HandleMountedPairActivated(UnitEntityData rider, UnitEntityData mount)
        {
            preparedRiderTurn = null;
            pendingSplitMount = null;
            pendingSplitRound = -1;
            if (!Enabled || !CombatController.IsInTurnBasedCombat() || rider == null || mount == null)
            {
                return;
            }

            MirrorExactInitiative(mount, "combat-mount-merge");
            TrySortUnits("KMC unified mounted merge");
        }

        private void HandleDismounting(CleanupTrigger trigger)
        {
            preparedRiderTurn = null;
            if (!Enabled || !CombatController.IsInTurnBasedCombat() || relationship.Mount == null)
            {
                return;
            }

            pendingSplitMount = relationship.Mount;
            pendingSplitRound = Game.Instance?.TurnBasedCombatController?.RoundNumber ?? -1;
            LastSplitObservation = "pending;mount=" + pendingSplitMount.UniqueId +
                ";round=" + pendingSplitRound + ";trigger=" + trigger;
        }

        private bool ShouldSuppressMount(UnitEntityData candidate, int currentRound)
        {
            var exactActiveMount = relationship.Mount;
            var candidateIsActiveMount = candidate != null && candidate == exactActiveMount;
            var candidateIsPending = candidate != null && candidate == pendingSplitMount;
            return UnifiedMountedTurnPolicy.ShouldSkipTurnCandidate(
                Enabled,
                CombatController.IsInTurnBasedCombat(),
                relationship.State == RelationshipState.Mounted && candidateIsActiveMount,
                candidateIsActiveMount || candidateIsPending,
                candidateIsPending,
                currentRound,
                pendingSplitRound);
        }

        private void MirrorExactInitiative(UnitEntityData mount, string source)
        {
            var rider = relationship.Rider;
            if (rider?.CombatState == null || mount?.CombatState == null ||
                !rider.CombatState.Prepared)
            {
                return;
            }

            var changed = mount.CombatState.Initiative != rider.CombatState.Initiative ||
                mount.CombatState.InitiativeRandom != rider.CombatState.InitiativeRandom ||
                Math.Abs(mount.CombatState.Cooldown.Initiative -
                    rider.CombatState.Cooldown.Initiative) > 0.0001f;
            mount.CombatState.Initiative = rider.CombatState.Initiative;
            mount.CombatState.InitiativeRandom = rider.CombatState.InitiativeRandom;
            mount.CombatState.Cooldown.Initiative = rider.CombatState.Cooldown.Initiative;
            if (changed)
            {
                MirroredInitiativeCount++;
                LastInitiativeObservation = source + ";owner=" + rider.UniqueId +
                    ";mount=" + mount.UniqueId + ";value=" + rider.CombatState.Initiative +
                    ";bonus=" + rider.Stats.Initiative.ModifiedValue;
            }
        }

        private void PrepareExactMountLedger(UnitEntityData mount)
        {
            mount.Commands.InterruptAll();
            var cooldown = mount.CombatState.Cooldown;
            cooldown.Initiative = 0f;
            cooldown.StandardAction = 0f;
            cooldown.MoveAction = 0f;
            cooldown.SwiftAction = 0f;
            cooldown.AttackOfOpportunity = 0f;
            if (mount.CombatState.AttackOfOpportunityPerRound > 0 &&
                mount.CombatState.AttackOfOpportunityCount <= mount.CombatState.AttackOfOpportunityPerRound)
            {
                mount.CombatState.AttackOfOpportunityCount = mount.CombatState.AttackOfOpportunityPerRound;
                mount.CombatState.DisengageAttackTargets.Clear();
            }
            mount.CombatState.OnNewRound();
            EventBus.RaiseEvent<IUnitNewCombatRoundHandler>(handler => handler.HandleNewCombatRound(mount));
            mount.CombatState.AIData.TickRound();
            mount.Logic.CallFactComponents<ITickEachRound>(logic => logic.OnNewRound());
            MountLedgerPrepareCount++;
            logger.Info("Unified mounted turn prepared exact mount ledger once: riderId=" +
                relationship.Rider.UniqueId + "; mountId=" + mount.UniqueId +
                "; round=" + (Game.Instance?.TurnBasedCombatController?.RoundNumber.ToString() ?? "<none>") + ".");
        }

        private static MountedActionLedgerSnapshot CaptureLedger(UnitEntityData unit)
        {
            var cooldown = unit?.CombatState?.Cooldown;
            if (unit == null || cooldown == null)
            {
                return null;
            }
            return new MountedActionLedgerSnapshot
            {
                UnitId = unit.UniqueId,
                Initiative = cooldown.Initiative,
                Standard = cooldown.StandardAction,
                Move = cooldown.MoveAction,
                Swift = cooldown.SwiftAction,
                AttackOfOpportunity = cooldown.AttackOfOpportunity,
                HasStandard = unit.HasStandardAction(),
                HasMove = unit.Descriptor?.State != null && unit.Descriptor.State.CanMove &&
                    cooldown.MoveAction < 6f && !unit.UsedTwoMoveAction(),
                HasSwift = unit.HasSwiftAction()
            };
        }

        private void TrySortUnits(string reason)
        {
            var controller = Game.Instance?.TurnBasedCombatController;
            if (controller == null || !controller.Initialized)
            {
                return;
            }

            try
            {
                SortUnitsMethod.Invoke(controller, new object[] { reason });
            }
            catch (Exception exception)
            {
                EnterFallback("SortUnits invocation failed", exception);
            }
        }

        private void EnterFallback(string reason, Exception exception)
        {
            ArchitectureFallbackCount++;
            settings.EnableUnifiedMountedTurn = false;
            if (exception == null)
            {
                logger.Error("Unified mounted turn entered separate-turn fallback: " + reason + ".");
            }
            else
            {
                logger.Exception("Unified mounted turn entered separate-turn fallback: " + reason, exception);
            }
        }

        private static FieldInfo ResolveField(Type type, string name, int token)
        {
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null || field.MetadataToken != token)
            {
                throw new MissingFieldException(type.FullName, name + " exact token " + token.ToString("X8"));
            }
            return field;
        }

        private static MethodInfo ResolveMethod(Type type, string name, int token, Type[] parameters)
        {
            var method = type.GetMethod(
                name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                parameters,
                null);
            if (method == null || method.MetadataToken != token)
            {
                throw new MissingMethodException(type.FullName, name + " exact token " + token.ToString("X8"));
            }
            return method;
        }
    }
}
