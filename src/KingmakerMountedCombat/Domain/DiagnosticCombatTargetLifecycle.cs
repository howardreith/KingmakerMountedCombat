using System;

namespace KingmakerMountedCombat.Domain
{
    public enum DiagnosticCombatTargetState
    {
        Absent,
        Created,
        Active,
        DestroyRequested,
        Removed,
        Faulted
    }

    public sealed class DiagnosticCombatTargetLifecycle
    {
        public DiagnosticCombatTargetState State { get; private set; }

        public string TargetId { get; private set; }

        public bool BeginCreate(string targetId, bool exactWorkingAuthorized)
        {
            if (!exactWorkingAuthorized ||
                string.IsNullOrWhiteSpace(targetId) ||
                (State != DiagnosticCombatTargetState.Absent && State != DiagnosticCombatTargetState.Removed))
            {
                return false;
            }

            TargetId = targetId;
            State = DiagnosticCombatTargetState.Created;
            return true;
        }

        public bool Activate(string exactTargetId, bool allSafetyGatesPassed)
        {
            if (State != DiagnosticCombatTargetState.Created ||
                !allSafetyGatesPassed ||
                !string.Equals(TargetId, exactTargetId, StringComparison.Ordinal))
            {
                return false;
            }

            State = DiagnosticCombatTargetState.Active;
            return true;
        }

        public bool RequestDestroy(string reason)
        {
            if (State == DiagnosticCombatTargetState.Absent ||
                State == DiagnosticCombatTargetState.Removed ||
                State == DiagnosticCombatTargetState.DestroyRequested)
            {
                return false;
            }

            State = DiagnosticCombatTargetState.DestroyRequested;
            return true;
        }

        public bool ConfirmRemoved(string exactTargetId, bool zeroResidue)
        {
            if (State != DiagnosticCombatTargetState.DestroyRequested ||
                !zeroResidue ||
                !string.Equals(TargetId, exactTargetId, StringComparison.Ordinal))
            {
                return false;
            }

            State = DiagnosticCombatTargetState.Removed;
            return true;
        }

        public void Fault()
        {
            State = DiagnosticCombatTargetState.Faulted;
        }
    }
}
