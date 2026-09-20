using System;

namespace KingmakerMountedCombat.Domain
{
    public enum MountedCombatTransactionState
    {
        Idle,
        Armed,
        Approaching,
        Attacking,
        Completed,
        Cancelled,
        Faulted
    }

    public sealed class MountedCombatTransaction
    {
        public const int MaximumRepaths = 4;
        public const string TargetInvalidatedBeforeChildAttackReason =
            "target invalidated before child attack";

        public MountedCombatTransactionState State { get; private set; }

        public MountedCombatActionKind Action { get; private set; }

        public string TargetId { get; private set; }

        public int RepathCount { get; private set; }

        public int ChildAttackStartCount { get; private set; }

        public string TerminalReason { get; private set; }

        public bool Arm(MountedCombatActionKind action)
        {
            if (action == MountedCombatActionKind.None || !CanReplaceOrStart())
            {
                return false;
            }

            Reset(action);
            State = MountedCombatTransactionState.Armed;
            return true;
        }

        public bool AcceptTarget(string targetId, bool requiresApproach)
        {
            if (State != MountedCombatTransactionState.Armed || string.IsNullOrWhiteSpace(targetId))
            {
                return false;
            }

            TargetId = targetId;
            State = requiresApproach
                ? MountedCombatTransactionState.Approaching
                : MountedCombatTransactionState.Attacking;
            return true;
        }

        public bool TryRepath(string exactTargetId)
        {
            if (State != MountedCombatTransactionState.Approaching ||
                !string.Equals(TargetId, exactTargetId, StringComparison.Ordinal) ||
                RepathCount >= MaximumRepaths)
            {
                return false;
            }

            RepathCount++;
            return true;
        }

        public bool Arrive(string exactTargetId)
        {
            if (State != MountedCombatTransactionState.Approaching ||
                !string.Equals(TargetId, exactTargetId, StringComparison.Ordinal))
            {
                return false;
            }

            State = MountedCombatTransactionState.Attacking;
            return true;
        }

        public bool TryStartSingleAttack(string exactTargetId)
        {
            if (State != MountedCombatTransactionState.Attacking ||
                ChildAttackStartCount != 0 ||
                !string.Equals(TargetId, exactTargetId, StringComparison.Ordinal))
            {
                return false;
            }

            ChildAttackStartCount = 1;
            return true;
        }

        public bool Complete(string exactTargetId)
        {
            if (State != MountedCombatTransactionState.Attacking ||
                ChildAttackStartCount != 1 ||
                !string.Equals(TargetId, exactTargetId, StringComparison.Ordinal))
            {
                return false;
            }

            State = MountedCombatTransactionState.Completed;
            TerminalReason = "completed";
            return true;
        }

        public bool Cancel(string reason)
        {
            if (IsTerminal || State == MountedCombatTransactionState.Idle)
            {
                return false;
            }

            State = MountedCombatTransactionState.Cancelled;
            TerminalReason = string.IsNullOrWhiteSpace(reason) ? "cancelled" : reason;
            return true;
        }

        public bool CancelTargetInvalidationBeforeChildAttack(string exactTargetId)
        {
            if (ChildAttackStartCount != 0 ||
                string.IsNullOrWhiteSpace(exactTargetId) ||
                !string.Equals(TargetId, exactTargetId, StringComparison.Ordinal))
            {
                return false;
            }

            return Cancel(TargetInvalidatedBeforeChildAttackReason);
        }

        public bool Fault(string reason)
        {
            if (IsTerminal || State == MountedCombatTransactionState.Idle)
            {
                return false;
            }

            State = MountedCombatTransactionState.Faulted;
            TerminalReason = string.IsNullOrWhiteSpace(reason) ? "faulted" : reason;
            return true;
        }

        public bool IsTerminal =>
            State == MountedCombatTransactionState.Completed ||
            State == MountedCombatTransactionState.Cancelled ||
            State == MountedCombatTransactionState.Faulted;

        private bool CanReplaceOrStart()
        {
            return State == MountedCombatTransactionState.Idle ||
                State == MountedCombatTransactionState.Armed ||
                IsTerminal;
        }

        private void Reset(MountedCombatActionKind action)
        {
            Action = action;
            TargetId = null;
            RepathCount = 0;
            ChildAttackStartCount = 0;
            TerminalReason = null;
        }
    }
}
