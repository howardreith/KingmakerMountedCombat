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

        public void Acquire(UnitEntityData expectedRider, UnitEntityData expectedMount)
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

            rider = expectedRider;
            mount = expectedMount;
            group = rider.Group;
            GroupId = group.Id;
            GroupIsPlayerParty = group.IsPlayerParty;
            RiderSharesGroup = rider.Group == group;
            MountSharesGroup = mount.Group == group;
            expectedMembers.Clear();
            expectedMembers.AddRange(CaptureCurrentMembers());

            lease = new ScopedDiagnosticAiLease<UnitEntityData>(
                unit => unit.UniqueId,
                unit => unit != null && unit != rider && unit != mount && unit.IsInState &&
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
                lease.ValidateActive(CaptureCurrentMembers());
                LastError = null;
                return true;
            }
            catch (Exception exception)
            {
                LastError = exception.Message;
                return false;
            }
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
                if (unit != rider && unit != mount)
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
