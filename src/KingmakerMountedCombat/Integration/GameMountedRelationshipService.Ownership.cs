using System;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Integration
{
    internal sealed partial class GameMountedRelationshipService
    {
        // Admission and archive restoration select the same all-pair capacity.
        internal const int NativePairCapacity = NativeMountedSaveFormat.NativePairCapacity;
        internal MountedWorldService World { get; set; }
        private bool importedMountedOwnership;
        private bool importedParticipationOwnership;
        internal MountedPlayerActionController PlayerActions { get; set; }
        internal MountedCombatController CombatOwner { get; set; }
        internal MountedPairRegistry<UnitEntityData, GameMountedRelationshipService> Ownership { get; }
        internal MountedPairOwner<UnitEntityData, GameMountedRelationshipService> PairOwner { get; private set; }
        private Func<UnitEntityData, UnitEntityData, bool> retainedParticipation = (rider, mount) => false;
        private Func<bool> retainedCommands = () => false;
        private Func<bool> pairSaveReady = () => true;
        private Func<bool> pairTeardownReady = () => true;

        internal void BindOwnershipReadiness(Func<UnitEntityData, UnitEntityData, bool> participation,
            Func<bool> commands, Func<bool> saveReady, Func<bool> teardownReady)
        {
            retainedParticipation = participation ?? throw new ArgumentNullException(nameof(participation));
            retainedCommands = commands ?? throw new ArgumentNullException(nameof(commands));
            pairSaveReady = saveReady ?? throw new ArgumentNullException(nameof(saveReady));
            pairTeardownReady = teardownReady ?? throw new ArgumentNullException(nameof(teardownReady));
        }

        internal string NativeAdmissionRefusal(UnitEntityData rider, UnitEntityData mount)
        {
            RefreshOwnership();
            return Ownership.AdmissionRefusal(Game.Instance?.Player, Game.Instance?.Player?.GameId,
                rider, mount, NativePairCapacity);
        }

        private bool TryReserveOwnership(UnitEntityData rider, UnitEntityData mount, out string refusal)
        {
            RefreshOwnership();
            MountedPairOwner<UnitEntityData, GameMountedRelationshipService> owner;
            if (!Ownership.TryReserve(Game.Instance?.Player, Game.Instance?.Player?.GameId,
                rider, mount, checked(mountedPairGeneration + 1), this,
                () => RetainsCleanupObligations || retainedCommands() || retainedParticipation(rider, mount),
                () => pairSaveReady(), () => pairTeardownReady(), out owner, out refusal, NativePairCapacity)) return false;
            // Known issues sprint 3 (review of fe22563a): the same obligations without the stick-movement cleanup the
            // boundary keeps on its own. Only world boundaries read this; the owner itself retires on the full set.
            Ownership.DeclareDetachedCleanup(owner,
                () => RetainsWorldCleanupObligations || retainedCommands() || retainedParticipation(rider, mount));
            PairOwner = owner;
            return true;
        }

        internal void ImportSavedOwnership(UnitEntityData rider, UnitEntityData mount, string ownerId,
            long generation, bool mounted)
        {
            var player = Game.Instance?.Player;
            ImportSavedOwnershipForSession(player, player?.GameId, rider, mount, ownerId, generation, mounted);
        }

        // The session is observed once at the native boundary; detached contract probes supply that observation.
        internal void ImportSavedOwnershipForSession(object session, string campaign, UnitEntityData rider, UnitEntityData mount,
            string ownerId, long generation, bool mounted)
        {
            RefreshOwnership();
            if (PairOwner != null || !Guid.TryParse(ownerId, out var id))
                throw new InvalidOperationException("Saved custody cannot replace an existing owner.");
            // A pair whose earlier owner still owes its stick-movement cleanup is not imported beside that owner.
            if (Ownership.HeldForDetachedCleanup(rider, mount, id))
                throw new InvalidOperationException(MountedCleanupCompletionPolicy.HeldByDetachedCleanupRefusal);
            if (!Ownership.TryImport(session, campaign, id, generation,
                rider, mount, this, mounted,
                () => importedParticipationOwnership || RetainsCleanupObligations ||
                    retainedCommands() || retainedParticipation(rider, mount),
                () => pairSaveReady(), () => pairTeardownReady(), out var owner, out var refusal))
                throw new InvalidOperationException(refusal);
            Ownership.DeclareDetachedCleanup(owner, () => importedParticipationOwnership || RetainsWorldCleanupObligations ||
                retainedCommands() || retainedParticipation(rider, mount));
            PairOwner = owner;
            mountedPairGeneration = generation;
            importedMountedOwnership = mounted;
            importedParticipationOwnership = true;
        }

        internal void CompleteSavedParticipationImport() { importedParticipationOwnership = false; }
        internal void CancelSavedOwnershipImport() { importedParticipationOwnership = false; importedMountedOwnership = false; }

        private bool TryReserveSavedOwnership(UnitEntityData rider, UnitEntityData mount, out string refusal)
        {
            if (!importedMountedOwnership) return TryReserveOwnership(rider, mount, out refusal);
            refusal = null;
            if (PairOwner != null && PairOwner.State == MountedPairOwnershipState.Reserved &&
                Ownership.Owns(PairOwner) && ReferenceEquals(PairOwner.Rider, rider) && ReferenceEquals(PairOwner.Mount, mount))
                return true;
            refusal = "Saved attachment no longer has its exact imported owner.";
            return false;
        }

        private void CommitSavedOwnership()
        {
            mountedPairGeneration = PairOwner.Generation;
            CommitOwnership();
            importedMountedOwnership = false;
        }

        private void CommitOwnership() => Ownership.Commit(PairOwner);

        private void CompleteOwnershipAttempt()
        {
            var owner = PairOwner;
            if (owner != null && Ownership.Owns(owner) &&
                (owner.State == MountedPairOwnershipState.Reserved ||
                 coordinator.State == RelationshipState.Unmounted || coordinator.State == RelationshipState.Faulted))
                Ownership.Detach(owner);
            RefreshOwnership();
        }

        internal void RetainRestoredParticipation(UnitEntityData rider, UnitEntityData mount)
        {
            RefreshOwnership();
            if (rider == null && mount == null) return;
            // Called only for the schema-1/2 UNMOUNTED native adapter, after one
            // shared restore. Mounted archives reserve at RestoreSavedPair instead.
            if (Ownership.Owners.Length != 0 || !Ownership.TryRetainDetached(Game.Instance?.Player,
                Game.Instance?.Player?.GameId, rider, mount, checked(mountedPairGeneration + 1), this,
                () => retainedParticipation(rider, mount), () => pairSaveReady(), () => pairTeardownReady(), out var owner))
                throw new InvalidOperationException("Restored participation conflicts with native pair capacity.");
            PairOwner = owner;
        }

        internal void RefreshOwnership()
        {
            try { Ownership.RetireSettled(); }
            finally { RefreshRetiredOwnership(); }
        }

        internal void RefreshRetiredOwnership()
        {
            if (PairOwner?.State != MountedPairOwnershipState.Retired) return;
            PairOwner = null;
            importedMountedOwnership = false;
            importedParticipationOwnership = false;
        }

        // Known issues sprint 3 (review of fe22563a): an area arrival defers a pair still held by its earlier owner's
        // detached cleanup instead of importing it beside that owner.
        internal bool PairHeldByDetachedCleanup(UnitEntityData rider, UnitEntityData mount, string ownerId)
        {
            RefreshOwnership();
            return Ownership.HeldForDetachedCleanup(rider, mount, Guid.TryParse(ownerId, out var id) ? id : (Guid?)null);
        }

        internal GameMountedRelationshipService ResolveActorOwner(UnitEntityData actor, bool activeOnly = false) =>
            (activeOnly ? Ownership.FindActive(actor) : Ownership.FindOwner(actor))?.Boundary;

        internal MountedPairRequest<UnitEntityData, GameMountedRelationshipService> CaptureNativeRequest(
            UnitEntityData rider, UnitEntityData mount, bool mounted)
        {
            RefreshOwnership();
            return Ownership.CaptureRequest(Game.Instance?.Player, Game.Instance?.Player?.GameId,
                rider, mount, mountedPairGeneration, this, mounted, NativePairCapacity);
        }

        internal bool ResolveNativeRequest(MountedPairRequest<UnitEntityData, GameMountedRelationshipService> request,
            out GameMountedRelationshipService owner) => Ownership.TryResolve(request,
                Game.Instance?.Player, Game.Instance?.Player?.GameId,
                request?.Boundary.MountedPairGeneration ?? -1, out owner);

        internal bool IsCurrentPairOwner(MountedPairOwner<UnitEntityData, GameMountedRelationshipService> owner) =>
            ReferenceEquals(PairOwner, owner) && owner != null && owner.Generation == mountedPairGeneration &&
            Ownership.IsActiveOwner(owner, Rider, Mount);

        internal bool AllSaveOwnersReady => Ownership.AllSaveReady();
        internal bool AllTeardownOwnersReady => Ownership.AllTeardownReady();
    }
}
