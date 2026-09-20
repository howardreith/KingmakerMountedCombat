using System;

namespace KingmakerMountedCombat.Domain
{
    public enum PairedCommandSchedulerState
    {
        Idle,
        Registered,
        AwaitingStart,
        Running,
        Finishing,
        Interrupting,
        Completed,
        Faulted,
        Disposed
    }

    public enum PairedCommandSchedulerRejection
    {
        None,
        SchedulerDisabled,
        UnifiedTurnDisabled,
        NotTurnBased,
        RelationshipNotMounted,
        PairReferenceChanged,
        RelationshipGenerationChanged,
        TurnReferenceChanged,
        RiderNotCurrent,
        TurnStateIneligible,
        CommandReferenceChanged,
        ExecutorNotMount,
        CommandTypeIneligible,
        CommandNotKmcCreated,
        CommandNotPlayerCreated,
        CommandSlotChanged,
        CommandQueued,
        AttackOfOpportunityExcluded,
        FreeOrOutOfTurnExcluded,
        AiCommandExcluded,
        WaitingForUi,
        MountNotAwake,
        MountNotInAwakeUnits,
        CommandTerminal,
        LeaseNotAwaitingDrive,
        DuplicateFrameDrive
    }

    public sealed class PairedCommandEligibilityContext
    {
        public bool SchedulerEnabled { get; set; }

        public bool UnifiedTurnEnabled { get; set; }

        public bool TurnBased { get; set; }

        public bool RelationshipMounted { get; set; }

        public bool PairReferencesMatch { get; set; }

        public bool RelationshipGenerationMatches { get; set; }

        public bool TurnReferenceMatches { get; set; }

        public bool CurrentTurnIsRider { get; set; }

        public bool TurnPreparing { get; set; }

        public bool TurnActing { get; set; }

        public bool TurnEnding { get; set; }

        public bool CommandReferenceMatches { get; set; }

        public bool ExecutorIsMount { get; set; }

        public bool CommandTypeEligible { get; set; }

        public bool ExplicitKmcOrigin { get; set; }

        public bool CreatedByPlayer { get; set; }

        public bool ExactExpectedSlot { get; set; }

        public bool InQueue { get; set; }

        public bool IsAttackOfOpportunity { get; set; }

        public bool IsFreeOrOutOfTurn { get; set; }

        public bool IsAiCommand { get; set; }

        public bool WaitingForUi { get; set; }

        public bool MountAwake { get; set; }

        public bool MountInAwakeUnits { get; set; }

        public bool CommandStarted { get; set; }

        public bool CommandTerminal { get; set; }
    }

    public sealed class PairedCommandEligibilityDecision
    {
        public PairedCommandEligibilityDecision(
            bool eligible,
            PairedCommandSchedulerRejection rejection)
        {
            Eligible = eligible;
            Rejection = rejection;
        }

        public bool Eligible { get; }

        public PairedCommandSchedulerRejection Rejection { get; }
    }

    public static class PairedCommandSchedulerPolicy
    {
        public static PairedCommandEligibilityDecision Evaluate(
            PairedCommandEligibilityContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!context.SchedulerEnabled)
            {
                return Reject(PairedCommandSchedulerRejection.SchedulerDisabled);
            }
            if (!context.UnifiedTurnEnabled)
            {
                return Reject(PairedCommandSchedulerRejection.UnifiedTurnDisabled);
            }
            if (!context.TurnBased)
            {
                return Reject(PairedCommandSchedulerRejection.NotTurnBased);
            }
            if (!context.RelationshipMounted)
            {
                return Reject(PairedCommandSchedulerRejection.RelationshipNotMounted);
            }
            if (!context.PairReferencesMatch)
            {
                return Reject(PairedCommandSchedulerRejection.PairReferenceChanged);
            }
            if (!context.RelationshipGenerationMatches)
            {
                return Reject(PairedCommandSchedulerRejection.RelationshipGenerationChanged);
            }
            if (!context.TurnReferenceMatches)
            {
                return Reject(PairedCommandSchedulerRejection.TurnReferenceChanged);
            }
            if (!context.CurrentTurnIsRider)
            {
                return Reject(PairedCommandSchedulerRejection.RiderNotCurrent);
            }
            if (!context.TurnPreparing && !context.TurnActing &&
                !(context.TurnEnding && context.CommandStarted))
            {
                return Reject(PairedCommandSchedulerRejection.TurnStateIneligible);
            }
            if (!context.CommandReferenceMatches)
            {
                return Reject(PairedCommandSchedulerRejection.CommandReferenceChanged);
            }
            if (!context.ExecutorIsMount)
            {
                return Reject(PairedCommandSchedulerRejection.ExecutorNotMount);
            }
            if (!context.CommandTypeEligible)
            {
                return Reject(PairedCommandSchedulerRejection.CommandTypeIneligible);
            }
            if (!context.ExplicitKmcOrigin)
            {
                return Reject(PairedCommandSchedulerRejection.CommandNotKmcCreated);
            }
            if (!context.CreatedByPlayer)
            {
                return Reject(PairedCommandSchedulerRejection.CommandNotPlayerCreated);
            }
            if (!context.ExactExpectedSlot)
            {
                return Reject(PairedCommandSchedulerRejection.CommandSlotChanged);
            }
            if (context.InQueue)
            {
                return Reject(PairedCommandSchedulerRejection.CommandQueued);
            }
            if (context.IsAttackOfOpportunity)
            {
                return Reject(PairedCommandSchedulerRejection.AttackOfOpportunityExcluded);
            }
            if (context.IsFreeOrOutOfTurn)
            {
                return Reject(PairedCommandSchedulerRejection.FreeOrOutOfTurnExcluded);
            }
            if (context.IsAiCommand)
            {
                return Reject(PairedCommandSchedulerRejection.AiCommandExcluded);
            }
            if (context.WaitingForUi)
            {
                return Reject(PairedCommandSchedulerRejection.WaitingForUi);
            }
            if (!context.MountAwake)
            {
                return Reject(PairedCommandSchedulerRejection.MountNotAwake);
            }
            if (!context.MountInAwakeUnits)
            {
                return Reject(PairedCommandSchedulerRejection.MountNotInAwakeUnits);
            }
            if (context.CommandTerminal)
            {
                return Reject(PairedCommandSchedulerRejection.CommandTerminal);
            }

            return new PairedCommandEligibilityDecision(
                true,
                PairedCommandSchedulerRejection.None);
        }

        public static bool IsInvariantFailure(PairedCommandSchedulerRejection rejection)
        {
            switch (rejection)
            {
                case PairedCommandSchedulerRejection.None:
                case PairedCommandSchedulerRejection.WaitingForUi:
                    return false;
                default:
                    return true;
            }
        }

        private static PairedCommandEligibilityDecision Reject(
            PairedCommandSchedulerRejection rejection)
        {
            return new PairedCommandEligibilityDecision(false, rejection);
        }
    }

    public sealed class PairedCommandSchedulerLeaseStateMachine
    {
        public PairedCommandSchedulerState State { get; private set; }

        public string RiderId { get; private set; }

        public string MountId { get; private set; }

        public long RelationshipGeneration { get; private set; }

        public string TurnIdentity { get; private set; }

        public string CommandIdentity { get; private set; }

        public int DriveCount { get; private set; }

        public int StartObservationCount { get; private set; }

        public int TerminalObservationCount { get; private set; }

        public int InterruptCount { get; private set; }

        public int ResourceChargeObservationCount { get; private set; }

        public int DuplicateFrameDriveCount { get; private set; }

        public int CleanupCount { get; private set; }

        public int LastDrivenFrame { get; private set; } = -1;

        public int StartObservedFrame { get; private set; } = -1;

        public string TerminalResult { get; private set; }

        public string CleanupReason { get; private set; }

        public string FaultReason { get; private set; }

        public bool Register(
            string riderId,
            string mountId,
            long relationshipGeneration,
            string turnIdentity,
            string commandIdentity)
        {
            if (State != PairedCommandSchedulerState.Idle ||
                string.IsNullOrWhiteSpace(riderId) ||
                string.IsNullOrWhiteSpace(mountId) ||
                relationshipGeneration <= 0 ||
                string.IsNullOrWhiteSpace(turnIdentity) ||
                string.IsNullOrWhiteSpace(commandIdentity))
            {
                return false;
            }

            RiderId = riderId;
            MountId = mountId;
            RelationshipGeneration = relationshipGeneration;
            TurnIdentity = turnIdentity;
            CommandIdentity = commandIdentity;
            State = PairedCommandSchedulerState.Registered;
            return true;
        }

        public bool ConfirmAdmission(
            bool exactExecutor,
            bool exactExpectedSlot,
            bool inQueue)
        {
            if (State != PairedCommandSchedulerState.Registered ||
                !exactExecutor || !exactExpectedSlot || inQueue)
            {
                Fault("command admission identity changed");
                return false;
            }

            State = PairedCommandSchedulerState.AwaitingStart;
            return true;
        }

        public bool TryAuthorizeDrive(
            int frame,
            PairedCommandEligibilityContext context,
            out PairedCommandSchedulerRejection rejection)
        {
            if (State != PairedCommandSchedulerState.AwaitingStart &&
                State != PairedCommandSchedulerState.Running)
            {
                rejection = PairedCommandSchedulerRejection.LeaseNotAwaitingDrive;
                return false;
            }

            var decision = PairedCommandSchedulerPolicy.Evaluate(context);
            if (!decision.Eligible)
            {
                rejection = decision.Rejection;
                return false;
            }

            if (frame == LastDrivenFrame)
            {
                DuplicateFrameDriveCount++;
                Fault("duplicate scheduler drive in frame " + frame);
                rejection = PairedCommandSchedulerRejection.DuplicateFrameDrive;
                return false;
            }

            LastDrivenFrame = frame;
            DriveCount++;
            if (context.CommandStarted)
            {
                ObserveStarted(frame);
            }
            rejection = PairedCommandSchedulerRejection.None;
            return true;
        }

        public bool ObserveLifecycle(
            int frame,
            bool started,
            bool running,
            bool finished,
            string terminalResult)
        {
            if (State == PairedCommandSchedulerState.Disposed)
            {
                return false;
            }

            if (started)
            {
                ObserveStarted(frame);
            }
            else if (StartObservationCount != 0 && !finished)
            {
                Fault("command start state regressed");
                return false;
            }

            if (running && State == PairedCommandSchedulerState.AwaitingStart)
            {
                State = PairedCommandSchedulerState.Running;
            }

            if (!finished)
            {
                return true;
            }

            return Complete(terminalResult);
        }

        public bool ObserveResourceCharge()
        {
            if (State == PairedCommandSchedulerState.Disposed ||
                ResourceChargeObservationCount != 0)
            {
                Fault("mount Standard resource charged more than once");
                return false;
            }

            ResourceChargeObservationCount = 1;
            return true;
        }

        public bool Complete(string terminalResult)
        {
            if (State == PairedCommandSchedulerState.Disposed ||
                State == PairedCommandSchedulerState.Completed ||
                State == PairedCommandSchedulerState.Faulted ||
                TerminalObservationCount != 0)
            {
                return false;
            }

            State = PairedCommandSchedulerState.Finishing;
            TerminalObservationCount = 1;
            TerminalResult = string.IsNullOrWhiteSpace(terminalResult)
                ? "<unspecified>"
                : terminalResult;
            State = PairedCommandSchedulerState.Completed;
            return true;
        }

        public bool Interrupt(string reason)
        {
            if (State == PairedCommandSchedulerState.Disposed ||
                State == PairedCommandSchedulerState.Completed ||
                InterruptCount != 0)
            {
                return false;
            }

            InterruptCount = 1;
            CleanupReason = Normalize(reason, "interrupted");
            if (State != PairedCommandSchedulerState.Faulted)
            {
                State = PairedCommandSchedulerState.Interrupting;
            }
            return true;
        }

        public bool Fault(string reason)
        {
            if (State == PairedCommandSchedulerState.Disposed ||
                State == PairedCommandSchedulerState.Faulted)
            {
                return false;
            }

            FaultReason = Normalize(reason, "faulted");
            State = PairedCommandSchedulerState.Faulted;
            return true;
        }

        public bool Dispose(string reason)
        {
            if (State == PairedCommandSchedulerState.Disposed)
            {
                return false;
            }

            CleanupCount = 1;
            CleanupReason = Normalize(reason, CleanupReason ?? "disposed");
            State = PairedCommandSchedulerState.Disposed;
            return true;
        }

        private void ObserveStarted(int frame)
        {
            if (StartObservationCount == 0)
            {
                StartObservationCount = 1;
                StartObservedFrame = frame;
            }
            if (State == PairedCommandSchedulerState.AwaitingStart)
            {
                State = PairedCommandSchedulerState.Running;
            }
        }

        private static string Normalize(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }
    }
}
