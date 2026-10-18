using System;
using System.Collections.Generic;
using System.Reflection;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Groups;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Diagnostics
{
    /// <summary>
    /// Diagnostic-only adapter that suppresses autonomous commands from the exact
    /// non-pair members of the rider's player-party group while a transient hostile
    /// target exists. It never interrupts, clears, replaces, or owns commands.
    /// </summary>
    internal sealed class DiagnosticNonPairPartyAiLease : IDisposable
    {
        private static readonly FieldInfo AiBackingField = typeof(UnitEntityData).GetField(
            "m_AiEnabled",
            BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly List<UnitEntityData> expectedMembers = new List<UnitEntityData>();
        private ScopedDiagnosticAiLease<UnitEntityData> lease;
        private UnitEntityData rider;
        private UnitEntityData mount;
        // Actors of the scenario's other mounted pairs. They are pair actors, not leased
        // non-pair members: the two-pair scenarios command them directly (observed natively in
        // 20261018-c7b-world27-ownerseed-b4s2b-rt, where pair B was leased and refused).
        private readonly List<UnitEntityData> otherPairActors = new List<UnitEntityData>();
        private UnitGroup group;
        private bool disposed;

        public string GroupId { get; private set; }

        public bool GroupIsPlayerParty { get; private set; }

        public bool RiderSharesGroup { get; private set; }

        public bool MountSharesGroup { get; private set; }

        public bool Acquired { get; private set; }

        public bool IsActive => lease != null && lease.IsAcquired;

        public bool ActiveValidationPassed => lease != null && lease.LastActiveValidationPassed;

        public bool Restored => lease == null || lease.LastRestoreVerified;

        public string LastError { get; private set; }

        public IReadOnlyList<ScopedDiagnosticAiLease<UnitEntityData>.State> Members =>
            lease == null
                ? (IReadOnlyList<ScopedDiagnosticAiLease<UnitEntityData>.State>)Array.Empty<ScopedDiagnosticAiLease<UnitEntityData>.State>()
                : lease.States;

        public void Acquire(UnitEntityData expectedRider, UnitEntityData expectedMount, UnitEntityData[] otherPairs = null)
        {
            ThrowIfDisposed();
            if (Acquired || lease != null)
            {
                throw new InvalidOperationException("A non-pair party AI lease has already been attempted.");
            }
            if (expectedRider == null || expectedMount == null ||
                !expectedRider.IsInState || !expectedMount.IsInState ||
                expectedRider.Group == null || expectedRider.Group != expectedMount.Group ||
                !expectedRider.Group.IsPlayerParty || AiBackingField == null ||
                AiBackingField.FieldType != typeof(bool))
            {
                throw new InvalidOperationException("The exact live owner/animal player-party AI scope is unavailable.");
            }
            var others = otherPairs ?? new UnitEntityData[0];
            var seen = new HashSet<UnitEntityData> { expectedRider, expectedMount };
            foreach (var actor in others)
            {
                if (actor == null || !actor.IsInState || actor.Group != expectedRider.Group || !seen.Add(actor))
                {
                    throw new InvalidOperationException("Another mounted pair's actor is missing, foreign or duplicated.");
                }
            }

            rider = expectedRider;
            mount = expectedMount;
            otherPairActors.Clear();
            otherPairActors.AddRange(others);
            group = rider.Group;
            GroupId = group.Id;
            GroupIsPlayerParty = group.IsPlayerParty;
            RiderSharesGroup = rider.Group == group;
            MountSharesGroup = mount.Group == group;
            expectedMembers.Clear();
            expectedMembers.AddRange(CaptureCurrentMembers());

            lease = new ScopedDiagnosticAiLease<UnitEntityData>(
                unit => unit.UniqueId,
                unit => unit != null && unit != rider && unit != mount && !otherPairActors.Contains(unit) && unit.IsInState &&
                    unit.Group == group && group.IsPlayerParty && unit.IsDirectlyControllable,
                unit => unit.Commands != null && unit.Commands.Empty,
                unit => (bool)AiBackingField.GetValue(unit),
                unit => unit.IsAIEnabled,
                (unit, value) => unit.IsAIEnabled = value);
            lease.Acquire(expectedMembers);
            Acquired = lease.IsAcquired && lease.LastActiveValidationPassed;
            LastError = null;
            if (!Acquired)
            {
                throw new InvalidOperationException("The exact non-pair party AI lease did not acquire.");
            }
        }

        public bool ValidateActive()
        {
            ThrowIfDisposed();
            if (!Acquired || lease == null || !lease.IsAcquired)
            {
                LastError = "The non-pair party AI lease is not active.";
                return false;
            }
            try
            {
                lease.ValidateActive(CaptureCurrentMembers(), declaredCommandMember);
                LastError = null;
                return true;
            }
            catch (Exception exception)
            {
                LastError = exception.Message;
                return false;
            }
        }

        // One declared ordinary command by one exact leased member (owner-seed scenario 3):
        // its AI stays disabled; only its own empty-command invariant is waived until the
        // command settles. At most one declaration per lease, never a pair actor.
        private UnitEntityData declaredCommandMember;
        public int DeclaredMemberCommandCount { get; private set; }
        public bool DeclaredMemberCommandOpen => declaredCommandMember != null;

        public bool BeginDeclaredMemberCommand(UnitEntityData unit)
        {
            ThrowIfDisposed();
            if (declaredCommandMember != null || DeclaredMemberCommandCount != 0 || !OwnsExactMember(unit) ||
                unit.Commands == null || !unit.Commands.Empty || !ValidateActive())
            {
                return false;
            }
            declaredCommandMember = unit;
            DeclaredMemberCommandCount++;
            return true;
        }

        public bool EndDeclaredMemberCommand(UnitEntityData unit)
        {
            ThrowIfDisposed();
            if (declaredCommandMember == null || !ReferenceEquals(unit, declaredCommandMember) ||
                unit.Commands == null || !unit.Commands.Empty)
            {
                return false;
            }
            declaredCommandMember = null;
            return ValidateActive();
        }

        public bool OwnsExactMember(UnitEntityData unit)
        {
            ThrowIfDisposed();
            if (!Acquired || lease == null || !lease.IsAcquired || unit == null ||
                ReferenceEquals(unit, rider) || ReferenceEquals(unit, mount) ||
                !ReferenceEquals(unit.Group, group))
            {
                return false;
            }

            var referenceCount = 0;
            for (var index = 0; index < expectedMembers.Count; index++)
            {
                if (ReferenceEquals(expectedMembers[index], unit))
                {
                    referenceCount++;
                }
            }
            return referenceCount == 1;
        }

        internal void ReassertAfterNativeModeExit()
        {
            ThrowIfDisposed();
            if (!Acquired || lease == null || !lease.IsAcquired ||
                Kingmaker.Game.Instance.TurnBasedCombatController.Initialized ||
                TurnBased.Controllers.CombatController.IsInTurnBasedCombat())
                throw new InvalidOperationException("Diagnostic AI reassertion requires a completed native TB shutdown.");
            lease.ReassertAfterNativeReset(CaptureCurrentMembers());
            LastError = null;
        }

        public bool RestoreAndVerify()
        {
            if (lease == null || !lease.IsAcquired)
            {
                return Restored;
            }
            try
            {
                lease.Restore(CaptureCurrentMembers());
                LastError = null;
                return lease.LastRestoreVerified;
            }
            catch (Exception exception)
            {
                LastError = exception.Message;
                return false;
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }
            var restored = RestoreAndVerify();
            disposed = true;
            if (!restored)
            {
                throw new InvalidOperationException(
                    "Non-pair party AI lease disposal retained residue: " + (LastError ?? "unknown error") + ".");
            }
        }

        private List<UnitEntityData> CaptureCurrentMembers()
        {
            if (group == null || group != rider?.Group || group != mount?.Group || !group.IsPlayerParty)
            {
                throw new InvalidOperationException("The exact owner/animal player-party group changed.");
            }

            var current = new List<UnitEntityData>();
            for (var index = 0; index < group.Count; index++)
            {
                var unit = group[index];
                if (unit != rider && unit != mount && !otherPairActors.Contains(unit))
                {
                    current.Add(unit);
                }
            }
            return current;
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(DiagnosticNonPairPartyAiLease));
            }
        }
    }
}
