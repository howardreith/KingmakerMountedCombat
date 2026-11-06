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
            return TryRepath(exactTargetId, false);
        }

        // F4 (AI-02). A repath that follows a moving target while the pair is still closing in is the native chase
        // (UnitAttack re-approaches a moving target every tick without any count) and does not consume the bounded
        // allowance; it is still bounded by MaximumProgressingRepaths and the command's elapsed bound. Every other
        // repath (no progress, unadmitted after a finished move, every Charge repath) counts as before.
        public const int MaximumProgressingRepaths = 40;

        public int ProgressingRepathCount { get; private set; }

        public bool TryRepath(string exactTargetId, bool progressingChase)
        {
            if (State != MountedCombatTransactionState.Approaching ||
                !string.Equals(TargetId, exactTargetId, StringComparison.Ordinal))
            {
                return false;
            }

            if (progressingChase && ProgressingRepathCount < MaximumProgressingRepaths)
            {
                ProgressingRepathCount++;
                return true;
            }

            if (RepathCount >= MaximumRepaths)
            {
                return false;
            }

            RepathCount++;
            return true;
        }

        // F4 (AI-01). Native UnitAttack.UpdateTarget continues a started routine on another target once its target
        // is down; the routine's remaining planned attacks are the upper bound and nothing new is spent. Only an
        // attacking transaction whose single child attack already started may follow it.
        public int ContinuationTargetCount { get; private set; }

        public bool TryContinueOnTarget(string currentTargetId, string nextTargetId)
        {
            if (State != MountedCombatTransactionState.Attacking || ChildAttackStartCount != 1 ||
                !string.Equals(TargetId, currentTargetId, StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(nextTargetId) || string.Equals(nextTargetId, currentTargetId, StringComparison.Ordinal))
            {
                return false;
            }

            TargetId = nextTargetId;
            ContinuationTargetCount++;
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
            ProgressingRepathCount = 0;
            ContinuationTargetCount = 0;
            ChildAttackStartCount = 0;
            TerminalReason = null;
        }
    }

    // F4 (AI-02). Whether a target-drift repath is the native chase of a target the pair is closing on.
    public static class MountedChaseProgressPolicy
    {
        // The pair-to-target distance must have shrunk by at least this much since the previous repath (or the start).
        public const float ProgressEpsilonMetres = 0.25f;
        // The elapsed bound grows by the travel time to the initial gap at the mount's own speed, capped.
        public const float MaximumApproachAllowanceSeconds = 8f;

        public static bool IsProgressingChase(bool chargeMode, bool targetDrift, float previousDistance, float currentDistance)
        {
            if (chargeMode || !targetDrift || !IsFinite(previousDistance) || !IsFinite(currentDistance)) return false;
            return currentDistance <= previousDistance - ProgressEpsilonMetres;
        }

        public static float ApproachAllowanceSeconds(bool chargeMode, bool requiresApproach, float initialGapMetres, float mountSpeedMps)
        {
            if (chargeMode || !requiresApproach || !IsFinite(initialGapMetres) || !IsFinite(mountSpeedMps) || initialGapMetres <= 0f) return 0f;
            var seconds = initialGapMetres / Math.Max(1f, mountSpeedMps);
            return Math.Min(MaximumApproachAllowanceSeconds, seconds);
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }

    // F4 (AI-01). Native UnitAttack.UpdateTarget (2.1.7b): once a started routine's target is dead (or newly
    // unconscious), the next planned attack goes to the nearest conscious, targetable remembered enemy within five feet
    // plus the attack's approach radius. The pair keeps its own stricter admission (no approach, pair range, line of
    // sight); a target still standing but out of reach keeps the existing termination rules.
    public static class MountedRoutineContinuationPolicy
    {
        public const float NativeFiveFeetMetres = 1.524f;

        public static bool MayContinue(bool chargeMode, bool attacking, bool attackReleased, bool plannedAttackRemains, bool targetDown)
        {
            return !chargeMode && attacking && attackReleased && plannedAttackRemains && targetDown;
        }

        public static bool WithinNativeContinuationRange(float distance, float executorCorpulence, float targetCorpulence, float weaponRange)
        {
            return !float.IsNaN(distance) && distance <= NativeFiveFeetMetres + executorCorpulence + targetCorpulence + weaponRange;
        }
    }
}
