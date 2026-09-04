using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker;
using Kingmaker.Controllers.Combat;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using KingmakerMountedCombat.Diagnostics;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Logging;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Integration
{
    internal sealed class MountedPairCommandSchedulerSnapshot
    {
        public bool Enabled { get; set; }
        public bool HasActiveLease { get; set; }
        public string State { get; set; }
        public string RiderId { get; set; }
        public string MountId { get; set; }
        public long RelationshipGeneration { get; set; }
        public string TurnIdentity { get; set; }
        public int TurnRound { get; set; }
        public string CommandIdentity { get; set; }
        public string CommandType { get; set; }
        public string ActionOrigin { get; set; }
        public string TargetId { get; set; }
        public string WeaponBlueprintId { get; set; }
        public string ExpectedResourceOwnerId { get; set; }
        public string ExpectedRuleInitiatorId { get; set; }
        public int CreationFrame { get; set; }
        public int AdmissionFrame { get; set; }
        public int FirstGrantFrame { get; set; }
        public int LastDrivenFrame { get; set; }
        public int StartObservedFrame { get; set; }
        public int DriveCount { get; set; }
        public int StartObservationCount { get; set; }
        public int TerminalObservationCount { get; set; }
        public int InterruptCount { get; set; }
        public int ResourceChargeObservationCount { get; set; }
        public int DuplicateFrameDriveCount { get; set; }
        public int CleanupCount { get; set; }
        public int ForeignCommandAdoptionCount { get; set; }
        public bool RiderRemainedCurrent { get; set; }
        public bool ExactExecutorRetained { get; set; }
        public bool ExactSlotRetained { get; set; }
        public bool MountStandardAvailableBefore { get; set; }
        public bool MountStandardAvailableAfter { get; set; }
        public bool RiderStandardAvailableBefore { get; set; }
        public bool RiderStandardAvailableAfter { get; set; }
        public float MountStandardCooldownBefore { get; set; }
        public float MountStandardCooldownAfter { get; set; }
        public float RiderStandardCooldownBefore { get; set; }
        public float RiderStandardCooldownAfter { get; set; }
        public string TerminalResult { get; set; }
        public string LastRejection { get; set; }
        public string CleanupReason { get; set; }
        public string FaultReason { get; set; }
        public string FirstObservedTurnStatus { get; set; }
        public string LastObservedTurnStatus { get; set; }
        public bool PreparingObserved { get; set; }
        public bool ActingObserved { get; set; }
        public bool EndingObserved { get; set; }
    }

    internal sealed class MountedPairCommandScheduler : IDisposable
    {
        private const float CooldownEpsilon = 0.0001f;

        private readonly GameMountedRelationshipService relationship;
        private readonly DiagnosticSettings settings;
        private readonly IModLogger logger;
        private SchedulerLease lease;
        private MountedPairCommandSchedulerSnapshot lastSnapshot;
        private bool interruptingExactCommand;
        private bool disposed;

        public MountedPairCommandScheduler(
            GameMountedRelationshipService relationship,
            DiagnosticSettings settings,
            IModLogger logger)
        {
            this.relationship = relationship ?? throw new ArgumentNullException(nameof(relationship));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            relationship.Dismounting += HandleDismounting;
        }

        internal int DriveCount => lease?.StateMachine.DriveCount ?? lastSnapshot?.DriveCount ?? 0;

        internal bool RequiresLease(MountedCombatActionKind action)
        {
            return action == MountedCombatActionKind.MountPrimaryNatural &&
                settings.EnableUnifiedMountedTurn &&
                CombatController.IsInTurnBasedCombat();
        }

        internal bool CanRegisterCurrentPair(out string reason)
        {
            reason = null;
            if (disposed)
            {
                reason = "scheduler disposed";
                return false;
            }
            if (!settings.EnablePairedCommandScheduler)
            {
                reason = "experimental scheduler disabled";
                return false;
            }
            if (!settings.EnableUnifiedMountedTurn)
            {
                reason = "unified turn disabled";
                return false;
            }
            if (!CombatController.IsInTurnBasedCombat())
            {
                reason = "not turn based";
                return false;
            }
            if (relationship.State != RelationshipState.Mounted ||
                relationship.Rider == null || relationship.Mount == null ||
                relationship.MountedPairGeneration <= 0)
            {
                reason = "exact mounted pair unavailable";
                return false;
            }
            if (lease != null)
            {
                reason = "another scheduler lease is active";
                return false;
            }

            var game = Game.Instance;
            var turn = game?.TurnBasedCombatController?.CurrentTurn;
            if (turn == null || turn.Unit != relationship.Rider)
            {
                reason = "rider is not CurrentTurn.Unit";
                return false;
            }
            if (turn.Status != TurnController.TurnStatus.Preparing && !turn.IsActing)
            {
                reason = "rider turn is not Preparing or Acting";
                return false;
            }

            var mount = relationship.Mount;
            if (!mount.IsAwake || game?.State?.AwakeUnits == null ||
                !game.State.AwakeUnits.Contains(mount))
            {
                reason = "mount is outside AwakeUnits";
                return false;
            }
            if (mount.Commands == null || mount.Commands.Standard != null ||
                mount.Commands.Queue.Count != 0)
            {
                reason = "mount Standard slot or queue is occupied";
                return false;
            }

            return true;
        }

        internal bool TryRegister(
            MountedPairAttackCommand command,
            out string reason)
        {
            reason = null;
            if (command == null || command.Action != MountedCombatActionKind.MountPrimaryNatural ||
                command.ActionActor != relationship.Mount || command.Rider != relationship.Rider ||
                command.Mount != relationship.Mount || command.Type != UnitCommand.CommandType.Standard ||
                !command.CreatedByPlayer || command.AiAction != null || command.IsStarted || command.IsFinished)
            {
                reason = "command is not one exact unstarted KMC mount-primary Standard command";
                return false;
            }
            if (!CanRegisterCurrentPair(out reason))
            {
                return false;
            }

            var rider = relationship.Rider;
            var mount = relationship.Mount;
            var turnController = Game.Instance.TurnBasedCombatController;
            var turn = turnController.CurrentTurn;
            var stateMachine = new PairedCommandSchedulerLeaseStateMachine();
            var commandIdentity = DescribeReference("command", command);
            var turnIdentity = DescribeReference("turn", turn);
            if (!stateMachine.Register(
                    rider.UniqueId,
                    mount.UniqueId,
                    relationship.MountedPairGeneration,
                    turnIdentity,
                    commandIdentity))
            {
                reason = "scheduler state rejected registration";
                return false;
            }

            lease = new SchedulerLease
            {
                StateMachine = stateMachine,
                Rider = rider,
                Mount = mount,
                Turn = turn,
                Command = command,
                RelationshipGeneration = relationship.MountedPairGeneration,
                TurnRound = turnController.RoundNumber,
                CommandIdentity = commandIdentity,
                TurnIdentity = turnIdentity,
                TargetId = command.TargetUnit?.UniqueId,
                WeaponBlueprintId = command.ExpectedMountPrimary?.Weapon?.Blueprint?.AssetGuid,
                CreationFrame = Time.frameCount,
                FirstGrantFrame = -1,
                MountStandardAvailableBefore = mount.HasStandardAction(),
                RiderStandardAvailableBefore = rider.HasStandardAction(),
                MountStandardCooldownBefore = mount.CombatState.Cooldown.StandardAction,
                RiderStandardCooldownBefore = rider.CombatState.Cooldown.StandardAction,
                RiderRemainedCurrent = true,
                ExactExecutorRetained = true,
                ExactSlotRetained = true,
                LastRejection = PairedCommandSchedulerRejection.None
            };
            lastSnapshot = null;
            logger.Info("Paired scheduler registered exact command lease: riderId=" + rider.UniqueId +
                "; mountId=" + mount.UniqueId +
                "; generation=" + lease.RelationshipGeneration +
                "; turn=" + turnIdentity +
                "; command=" + commandIdentity + ".");
            return true;
        }

        internal bool ConfirmAdmission(
            MountedPairAttackCommand command,
            out string reason)
        {
            reason = null;
            if (!OwnsExactLease(command))
            {
                reason = "scheduler lease does not own the admitted command";
                return false;
            }

            var exactExecutor = command.Executor == lease.Mount;
            var exactSlot = lease.Mount.Commands != null &&
                ReferenceEquals(lease.Mount.Commands.Standard, command);
            var inQueue = lease.Mount.Commands != null &&
                lease.Mount.Commands.Queue.Contains(command);
            if (!lease.StateMachine.ConfirmAdmission(exactExecutor, exactSlot, inQueue))
            {
                lease.ExactExecutorRetained &= exactExecutor;
                lease.ExactSlotRetained &= exactSlot && !inQueue;
                reason = "command did not enter the exact mount Standard slot";
                FailInterruptAndDispose(reason, null);
                return false;
            }

            lease.AdmissionFrame = Time.frameCount;
            ObserveTurnStatus(Game.Instance?.TurnBasedCombatController?.CurrentTurn);
            RefreshLifecycleAndResources();
            return true;
        }

        internal bool TryExtendNativeEligibility(
            UnitCommand command,
            bool stockResult,
            ref bool result)
        {
            if (disposed || command == null || !OwnsExactLease(command))
            {
                return false;
            }

            try
            {
                RefreshLifecycleAndResources();
                if (lease == null)
                {
                    result = false;
                    return false;
                }
                if (command.IsFinished || command.Result != UnitCommand.ResultType.None ||
                    lease.StateMachine.State == PairedCommandSchedulerState.Completed)
                {
                    result = false;
                    return false;
                }
                if (stockResult || result)
                {
                    result = false;
                    FailInterruptAndDispose(
                        "stock eligibility unexpectedly admitted a scheduler-leased cross-actor command",
                        null);
                    return false;
                }

                var context = CaptureEligibilityContext(command);
                ObserveTurnStatus(Game.Instance?.TurnBasedCombatController?.CurrentTurn);
                PairedCommandSchedulerRejection rejection;
                if (!lease.StateMachine.TryAuthorizeDrive(
                        Time.frameCount,
                        context,
                        out rejection))
                {
                    lease.LastRejection = rejection;
                    if (rejection != PairedCommandSchedulerRejection.WaitingForUi &&
                        rejection != PairedCommandSchedulerRejection.CommandTerminal &&
                        PairedCommandSchedulerPolicy.IsInvariantFailure(rejection))
                    {
                        FailInterruptAndDispose("eligibility invariant " + rejection, null);
                    }
                    return false;
                }

                if (lease.FirstGrantFrame < 0)
                {
                    lease.FirstGrantFrame = Time.frameCount;
                }
                lease.LastRejection = PairedCommandSchedulerRejection.None;
                result = true;
                return true;
            }
            catch (Exception exception)
            {
                result = false;
                FailInterruptAndDispose("eligibility exception", exception);
                return false;
            }
        }

        internal void ObserveTerminal(
            MountedPairAttackCommand command,
            string result,
            string reason)
        {
            if (!OwnsExactLease(command))
            {
                return;
            }

            try
            {
                RefreshLifecycleAndResources();
                lease.StateMachine.Complete(result);
                lease.TerminalReason = reason;
                lastSnapshot = CaptureLeaseSnapshot(lease, true);
            }
            catch (Exception exception)
            {
                FailAndInterrupt("terminal observation exception", exception);
            }
        }

        internal void ObserveExternalInterrupt(
            MountedPairAttackCommand command,
            string reason)
        {
            if (!OwnsExactLease(command))
            {
                return;
            }

            lease.StateMachine.Interrupt(reason);
            lease.TerminalReason = reason;
        }

        internal void AbandonRegistration(
            MountedPairAttackCommand command,
            string reason)
        {
            if (!OwnsExactLease(command))
            {
                return;
            }

            FailInterruptAndDispose(reason, null);
        }

        internal void Update()
        {
            if (disposed || lease == null)
            {
                return;
            }

            try
            {
                RefreshLifecycleAndResources();
                if (lease == null)
                {
                    return;
                }
                var command = lease.Command;
                var commands = lease.Mount?.Commands;
                if (command.IsFinished)
                {
                    if (commands == null || !commands.Contains(command))
                    {
                        ArchiveAndDispose("native terminal slot removal");
                    }
                    return;
                }

                var currentTurn = Game.Instance?.TurnBasedCombatController?.CurrentTurn;
                ObserveTurnStatus(currentTurn);
                var decision = PairedCommandSchedulerPolicy.Evaluate(
                    CaptureEligibilityContext(command));
                if (!decision.Eligible &&
                    decision.Rejection != PairedCommandSchedulerRejection.WaitingForUi)
                {
                    FailInterruptAndDispose(
                        "active lease invariant " + decision.Rejection,
                        null);
                }
            }
            catch (Exception exception)
            {
                FailInterruptAndDispose("scheduler update exception", exception);
            }
        }

        internal MountedPairCommandSchedulerSnapshot CaptureSnapshot()
        {
            if (lease != null)
            {
                return CaptureLeaseSnapshot(lease, true);
            }
            if (lastSnapshot != null)
            {
                return Clone(lastSnapshot);
            }

            return new MountedPairCommandSchedulerSnapshot
            {
                Enabled = settings.EnablePairedCommandScheduler,
                HasActiveLease = false,
                State = PairedCommandSchedulerState.Idle.ToString(),
                FirstGrantFrame = -1,
                LastDrivenFrame = -1,
                StartObservedFrame = -1,
                RiderRemainedCurrent = true,
                ExactExecutorRetained = true,
                ExactSlotRetained = true,
                LastRejection = PairedCommandSchedulerRejection.None.ToString()
            };
        }

        internal string Describe()
        {
            var snapshot = CaptureSnapshot();
            return "enabled=" + snapshot.Enabled +
                ",activeLease=" + snapshot.HasActiveLease +
                ",state=" + snapshot.State +
                ",riderId=" + (snapshot.RiderId ?? "<none>") +
                ",mountId=" + (snapshot.MountId ?? "<none>") +
                ",generation=" + snapshot.RelationshipGeneration +
                ",turn=" + (snapshot.TurnIdentity ?? "<none>") +
                ",command=" + (snapshot.CommandIdentity ?? "<none>") +
                ",creationFrame=" + snapshot.CreationFrame +
                ",admissionFrame=" + snapshot.AdmissionFrame +
                ",firstGrantFrame=" + snapshot.FirstGrantFrame +
                ",lastDrivenFrame=" + snapshot.LastDrivenFrame +
                ",startFrame=" + snapshot.StartObservedFrame +
                ",driveCount=" + snapshot.DriveCount +
                ",startCount=" + snapshot.StartObservationCount +
                ",terminalCount=" + snapshot.TerminalObservationCount +
                ",resourceChargeCount=" + snapshot.ResourceChargeObservationCount +
                ",duplicateFrameCount=" + snapshot.DuplicateFrameDriveCount +
                ",riderRemainedCurrent=" + snapshot.RiderRemainedCurrent +
                ",executorRetained=" + snapshot.ExactExecutorRetained +
                ",slotRetained=" + snapshot.ExactSlotRetained +
                ",firstTurnStatus=" + (snapshot.FirstObservedTurnStatus ?? "<none>") +
                ",lastTurnStatus=" + (snapshot.LastObservedTurnStatus ?? "<none>") +
                ",lastRejection=" + (snapshot.LastRejection ?? "<none>") +
                ",fault=" + (snapshot.FaultReason ?? "<none>");
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            relationship.Dismounting -= HandleDismounting;
            if (lease != null)
            {
                InterruptExactCommand("scheduler disposal");
                ArchiveAndDispose("scheduler disposal");
            }
            disposed = true;
        }

        private PairedCommandEligibilityContext CaptureEligibilityContext(UnitCommand command)
        {
            var game = Game.Instance;
            var controller = game?.TurnBasedCombatController;
            var currentTurn = controller?.CurrentTurn;
            var currentRider = currentTurn?.Unit == lease.Rider;
            var exactExecutor = command.Executor == lease.Mount;
            var exactSlot = lease.Mount?.Commands != null &&
                ReferenceEquals(lease.Mount.Commands.Standard, command);
            lease.RiderRemainedCurrent &= currentRider;
            lease.ExactExecutorRetained &= exactExecutor;
            lease.ExactSlotRetained &= exactSlot && !lease.Mount.Commands.Queue.Contains(command);

            return new PairedCommandEligibilityContext
            {
                SchedulerEnabled = settings.EnablePairedCommandScheduler,
                UnifiedTurnEnabled = settings.EnableUnifiedMountedTurn,
                TurnBased = CombatController.IsInTurnBasedCombat(),
                RelationshipMounted = relationship.State == RelationshipState.Mounted,
                PairReferencesMatch = relationship.Rider == lease.Rider &&
                    relationship.Mount == lease.Mount,
                RelationshipGenerationMatches =
                    relationship.MountedPairGeneration == lease.RelationshipGeneration,
                TurnReferenceMatches = ReferenceEquals(currentTurn, lease.Turn),
                CurrentTurnIsRider = currentRider,
                TurnPreparing = currentTurn != null &&
                    currentTurn.Status == TurnController.TurnStatus.Preparing,
                TurnActing = currentTurn != null && currentTurn.IsActing,
                TurnEnding = currentTurn != null && currentTurn.IsEnding,
                CommandReferenceMatches = ReferenceEquals(command, lease.Command),
                ExecutorIsMount = exactExecutor,
                CommandTypeEligible = command is MountedPairAttackCommand &&
                    command.Type == UnitCommand.CommandType.Standard,
                ExplicitKmcOrigin = ReferenceEquals(command, lease.Command),
                CreatedByPlayer = command.CreatedByPlayer,
                ExactExpectedSlot = exactSlot,
                InQueue = lease.Mount?.Commands != null &&
                    lease.Mount.Commands.Queue.Contains(command),
                IsAttackOfOpportunity = command is UnitAttackOfOpportunity,
                IsFreeOrOutOfTurn = command.Type == UnitCommand.CommandType.Free,
                IsAiCommand = command.AiAction != null || !command.CreatedByPlayer,
                WaitingForUi = controller != null && (bool)controller.WaitingForUI,
                MountAwake = lease.Mount != null && lease.Mount.IsAwake,
                MountInAwakeUnits = lease.Mount != null && game?.State?.AwakeUnits != null &&
                    game.State.AwakeUnits.Contains(lease.Mount),
                CommandStarted = command.IsStarted,
                CommandTerminal = command.IsFinished || command.Result != UnitCommand.ResultType.None
            };
        }

        private void RefreshLifecycleAndResources()
        {
            if (lease == null)
            {
                return;
            }

            var frame = Time.frameCount;
            var command = lease.Command;
            lease.StateMachine.ObserveLifecycle(
                frame,
                command.IsStarted,
                command.IsRunning,
                command.IsFinished,
                command.Result.ToString());

            var mount = lease.Mount;
            var rider = lease.Rider;
            lease.MountStandardAvailableAfter = mount != null && mount.HasStandardAction();
            lease.RiderStandardAvailableAfter = rider != null && rider.HasStandardAction();
            lease.MountStandardCooldownAfter = mount?.CombatState?.Cooldown.StandardAction ?? float.NaN;
            lease.RiderStandardCooldownAfter = rider?.CombatState?.Cooldown.StandardAction ?? float.NaN;

            var mountCharged = lease.MountStandardAvailableBefore &&
                !lease.MountStandardAvailableAfter;
            if (mountCharged && lease.StateMachine.ResourceChargeObservationCount == 0)
            {
                lease.StateMachine.ObserveResourceCharge();
                lease.MountCooldownAtObservedCharge = lease.MountStandardCooldownAfter;
            }
            else if (lease.StateMachine.ResourceChargeObservationCount != 0 &&
                lease.MountStandardCooldownAfter >
                    lease.MountCooldownAtObservedCharge + CooldownEpsilon)
            {
                FailAndInterrupt("mount Standard cooldown increased after its one charge", null);
                return;
            }

            if (lease.RiderStandardAvailableBefore != lease.RiderStandardAvailableAfter ||
                Math.Abs(lease.RiderStandardCooldownAfter -
                    lease.RiderStandardCooldownBefore) > CooldownEpsilon)
            {
                FailAndInterrupt("rider Standard changed during a mount-owned lease", null);
            }
        }

        private bool OwnsExactLease(UnitCommand command)
        {
            return lease != null && command != null && ReferenceEquals(lease.Command, command);
        }

        private void FailAndInterrupt(string reason, Exception exception)
        {
            if (lease == null)
            {
                return;
            }

            lease.TerminalReason = reason;
            if (exception == null)
            {
                logger.Error("Paired scheduler faulted closed: " + reason + ".");
            }
            else
            {
                logger.Exception("Paired scheduler faulted closed: " + reason, exception);
            }
            InterruptExactCommand(reason);
            lease?.StateMachine.Fault(reason);
        }

        private void FailInterruptAndDispose(string reason, Exception exception)
        {
            if (lease == null)
            {
                return;
            }

            FailAndInterrupt(reason, exception);
            ArchiveAndDispose(reason);
        }

        private void InterruptExactCommand(string reason)
        {
            if (lease == null || interruptingExactCommand)
            {
                return;
            }

            lease.StateMachine.Interrupt(reason);
            var command = lease.Command;
            if (command == null || command.IsFinished)
            {
                return;
            }

            try
            {
                interruptingExactCommand = true;
                command.Interrupt();
            }
            catch (Exception exception)
            {
                logger.Exception("Paired scheduler exact-command interrupt", exception);
            }
            finally
            {
                interruptingExactCommand = false;
            }
        }

        private void ArchiveAndDispose(string reason)
        {
            if (lease == null)
            {
                return;
            }

            RefreshLifecycleAndResources();
            lease.StateMachine.Dispose(reason);
            lastSnapshot = CaptureLeaseSnapshot(lease, false);
            lease = null;
        }

        private MountedPairCommandSchedulerSnapshot CaptureLeaseSnapshot(
            SchedulerLease value,
            bool active)
        {
            var state = value.StateMachine;
            return new MountedPairCommandSchedulerSnapshot
            {
                Enabled = settings.EnablePairedCommandScheduler,
                HasActiveLease = active,
                State = state.State.ToString(),
                RiderId = value.Rider?.UniqueId,
                MountId = value.Mount?.UniqueId,
                RelationshipGeneration = value.RelationshipGeneration,
                TurnIdentity = value.TurnIdentity,
                TurnRound = value.TurnRound,
                CommandIdentity = value.CommandIdentity,
                CommandType = value.Command?.GetType().FullName,
                ActionOrigin = MountedCombatActionKind.MountPrimaryNatural.ToString(),
                TargetId = value.TargetId,
                WeaponBlueprintId = value.WeaponBlueprintId,
                ExpectedResourceOwnerId = value.Mount?.UniqueId,
                ExpectedRuleInitiatorId = value.Mount?.UniqueId,
                CreationFrame = value.CreationFrame,
                AdmissionFrame = value.AdmissionFrame,
                FirstGrantFrame = value.FirstGrantFrame,
                LastDrivenFrame = state.LastDrivenFrame,
                StartObservedFrame = state.StartObservedFrame,
                DriveCount = state.DriveCount,
                StartObservationCount = state.StartObservationCount,
                TerminalObservationCount = state.TerminalObservationCount,
                InterruptCount = state.InterruptCount,
                ResourceChargeObservationCount = state.ResourceChargeObservationCount,
                DuplicateFrameDriveCount = state.DuplicateFrameDriveCount,
                CleanupCount = state.CleanupCount,
                ForeignCommandAdoptionCount = 0,
                RiderRemainedCurrent = value.RiderRemainedCurrent,
                ExactExecutorRetained = value.ExactExecutorRetained,
                ExactSlotRetained = value.ExactSlotRetained,
                MountStandardAvailableBefore = value.MountStandardAvailableBefore,
                MountStandardAvailableAfter = value.MountStandardAvailableAfter,
                RiderStandardAvailableBefore = value.RiderStandardAvailableBefore,
                RiderStandardAvailableAfter = value.RiderStandardAvailableAfter,
                MountStandardCooldownBefore = value.MountStandardCooldownBefore,
                MountStandardCooldownAfter = value.MountStandardCooldownAfter,
                RiderStandardCooldownBefore = value.RiderStandardCooldownBefore,
                RiderStandardCooldownAfter = value.RiderStandardCooldownAfter,
                TerminalResult = state.TerminalResult,
                LastRejection = value.LastRejection.ToString(),
                CleanupReason = state.CleanupReason ?? value.TerminalReason,
                FaultReason = state.FaultReason,
                FirstObservedTurnStatus = value.FirstObservedTurnStatus,
                LastObservedTurnStatus = value.LastObservedTurnStatus,
                PreparingObserved = value.PreparingObserved,
                ActingObserved = value.ActingObserved,
                EndingObserved = value.EndingObserved
            };
        }

        private static MountedPairCommandSchedulerSnapshot Clone(
            MountedPairCommandSchedulerSnapshot value)
        {
            return new MountedPairCommandSchedulerSnapshot
            {
                Enabled = value.Enabled,
                HasActiveLease = value.HasActiveLease,
                State = value.State,
                RiderId = value.RiderId,
                MountId = value.MountId,
                RelationshipGeneration = value.RelationshipGeneration,
                TurnIdentity = value.TurnIdentity,
                TurnRound = value.TurnRound,
                CommandIdentity = value.CommandIdentity,
                CommandType = value.CommandType,
                ActionOrigin = value.ActionOrigin,
                TargetId = value.TargetId,
                WeaponBlueprintId = value.WeaponBlueprintId,
                ExpectedResourceOwnerId = value.ExpectedResourceOwnerId,
                ExpectedRuleInitiatorId = value.ExpectedRuleInitiatorId,
                CreationFrame = value.CreationFrame,
                AdmissionFrame = value.AdmissionFrame,
                FirstGrantFrame = value.FirstGrantFrame,
                LastDrivenFrame = value.LastDrivenFrame,
                StartObservedFrame = value.StartObservedFrame,
                DriveCount = value.DriveCount,
                StartObservationCount = value.StartObservationCount,
                TerminalObservationCount = value.TerminalObservationCount,
                InterruptCount = value.InterruptCount,
                ResourceChargeObservationCount = value.ResourceChargeObservationCount,
                DuplicateFrameDriveCount = value.DuplicateFrameDriveCount,
                CleanupCount = value.CleanupCount,
                ForeignCommandAdoptionCount = value.ForeignCommandAdoptionCount,
                RiderRemainedCurrent = value.RiderRemainedCurrent,
                ExactExecutorRetained = value.ExactExecutorRetained,
                ExactSlotRetained = value.ExactSlotRetained,
                MountStandardAvailableBefore = value.MountStandardAvailableBefore,
                MountStandardAvailableAfter = value.MountStandardAvailableAfter,
                RiderStandardAvailableBefore = value.RiderStandardAvailableBefore,
                RiderStandardAvailableAfter = value.RiderStandardAvailableAfter,
                MountStandardCooldownBefore = value.MountStandardCooldownBefore,
                MountStandardCooldownAfter = value.MountStandardCooldownAfter,
                RiderStandardCooldownBefore = value.RiderStandardCooldownBefore,
                RiderStandardCooldownAfter = value.RiderStandardCooldownAfter,
                TerminalResult = value.TerminalResult,
                LastRejection = value.LastRejection,
                CleanupReason = value.CleanupReason,
                FaultReason = value.FaultReason,
                FirstObservedTurnStatus = value.FirstObservedTurnStatus,
                LastObservedTurnStatus = value.LastObservedTurnStatus,
                PreparingObserved = value.PreparingObserved,
                ActingObserved = value.ActingObserved,
                EndingObserved = value.EndingObserved
            };
        }

        private void ObserveTurnStatus(TurnController turn)
        {
            if (lease == null || turn == null)
            {
                return;
            }

            var status = turn.Status.ToString();
            if (lease.FirstObservedTurnStatus == null)
            {
                lease.FirstObservedTurnStatus = status;
            }
            lease.LastObservedTurnStatus = status;
            lease.PreparingObserved |= turn.Status == TurnController.TurnStatus.Preparing;
            lease.ActingObserved |= turn.IsActing;
            lease.EndingObserved |= turn.IsEnding;
        }

        private void HandleDismounting(CleanupTrigger trigger)
        {
            if (lease == null)
            {
                return;
            }

            InterruptExactCommand("relationship cleanup " + trigger);
            ArchiveAndDispose("relationship cleanup " + trigger);
        }

        private static string DescribeReference(string kind, object value)
        {
            return kind + "@" + RuntimeHelpers.GetHashCode(value).ToString("x8");
        }

        private sealed class SchedulerLease
        {
            public PairedCommandSchedulerLeaseStateMachine StateMachine;
            public UnitEntityData Rider;
            public UnitEntityData Mount;
            public TurnController Turn;
            public MountedPairAttackCommand Command;
            public long RelationshipGeneration;
            public int TurnRound;
            public string TurnIdentity;
            public string CommandIdentity;
            public string TargetId;
            public string WeaponBlueprintId;
            public int CreationFrame;
            public int AdmissionFrame;
            public int FirstGrantFrame;
            public bool RiderRemainedCurrent;
            public bool ExactExecutorRetained;
            public bool ExactSlotRetained;
            public bool MountStandardAvailableBefore;
            public bool MountStandardAvailableAfter;
            public bool RiderStandardAvailableBefore;
            public bool RiderStandardAvailableAfter;
            public float MountStandardCooldownBefore;
            public float MountStandardCooldownAfter;
            public float MountCooldownAtObservedCharge;
            public float RiderStandardCooldownBefore;
            public float RiderStandardCooldownAfter;
            public string TerminalReason;
            public PairedCommandSchedulerRejection LastRejection;
            public string FirstObservedTurnStatus;
            public string LastObservedTurnStatus;
            public bool PreparingObserved;
            public bool ActingObserved;
            public bool EndingObserved;
        }
    }
}
