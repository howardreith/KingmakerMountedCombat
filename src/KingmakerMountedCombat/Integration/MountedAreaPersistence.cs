using System;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints.Area;
using Kingmaker.EntitySystem.Persistence;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Integration
{
    internal sealed partial class MountedPersistenceService
    {
        // An ordinary area transfer keeps the native Player. Only bounded semantic
        // IDs/slots cross its view boundary; a save load always invalidates this.
        private AreaTransfer areaTransfer;
        internal bool AreaTransitionPending => areaTransfer != null;
        internal int AreaSuspensionCount { get; private set; }
        internal int AreaResumeCount { get; private set; }
        internal int RefusedAreaTransferCount { get; private set; }

        internal void BeginAreaTransition(BlueprintArea area, SaveInfo saveInfo)
        {
            var world = Game.Instance?.Player;
            if (saveInfo != null) { CancelAreaTransition(); return; }
            if (areaTransfer != null && areaTransfer.World.Target == world &&
                areaTransfer.AreaId == area?.AssetGuidThreadSafe) return;
            CancelAreaTransition();
            if (restoreLoad != null && (!restoreLoad.World.NativeCompleted || presentationPending))
            {
                FenceAbandonedCombat();
                restoreLoad.World.Close();
                restoreLoad = null;
                presentationPending = false;
                restoredActors.Clear();
                loaded = null;
                loadSequence++;
            }
            if (!Enabled || world == null || area == null || relationship.State != RelationshipState.Mounted ||
                world.IsInCombat || relationship.Rider.IsInCombat || relationship.Mount.IsInCombat)
                return;
            // The archive worker serializes the LIVE Player, cross-scene state and
            // LoadedAreaState, so a world replacement overlapping it would change
            // the very graphs being written. KMC refuses to carry a pair across
            // that boundary; the transfer is simply not armed, and the ordinary
            // unarmed area path still runs.
            if (SaveDraining || SaveSuspended)
            {
                RefusedAreaTransferCount++;
                Report("A mounted save is still being written; the pair is not carried across this area change.");
                return;
            }
            areaTransfer = new AreaTransfer {
                World = new WeakReference(world), CampaignId = world.GameId, AreaId = area.AssetGuidThreadSafe,
                RiderId = relationship.Rider.UniqueId, MountId = relationship.Mount.UniqueId,
                ProfileId = relationship.Runtime.MountProfileId, Slots = controls.CapturePersistentSlots()
            };
        }

        internal bool SuspendAreaPair()
        {
            var transfer = areaTransfer;
            if (transfer == null || !Enabled || transfer.World.Target != Game.Instance?.Player) return false;
            if (transfer.Suspended) return true;
            try
            {
                // No End, preparation or resource writes: native area departure
                // owns encounter shutdown; this only releases process-local leases.
                unifiedTurn.DiscardPersistenceWorld();
                controls.SuspendAreaControls();
                if (!relationship.GuardBoundary(CleanupTrigger.AreaSuspension))
                    throw new InvalidOperationException("Area suspension retained mounted attachment/movement references.");
                transfer.Suspended = true;
                AreaSuspensionCount++;
                Report("Mounted relationship suspended for native area placement.");
                return true;
            }
            catch
            {
                CancelAreaTransition();
                throw;
            }
        }

        internal void RestoreAreaPair()
        {
            var transfer = areaTransfer;
            if (transfer == null || !transfer.Suspended || transfer.ResumeAttempted) return;
            transfer.ResumeAttempted = true;
            var game = Game.Instance;
            try
            {
                if (!Enabled || transfer.World.Target != game?.Player || game.Player.GameId != transfer.CampaignId ||
                    game.CurrentlyLoadedArea?.AssetGuidThreadSafe != transfer.AreaId)
                {
                    Report("Area changed before mounted restoration; relationship remains unmounted.");
                    return;
                }
                var riders = game.State.Units.Where(u => u.UniqueId == transfer.RiderId).ToArray();
                var mounts = game.State.Units.Where(u => u.UniqueId == transfer.MountId).ToArray();
                if (riders.Length != 1 || mounts.Length != 1)
                {
                    Report("Area pair did not resolve uniquely; relationship remains unmounted without refund.");
                    return;
                }
                // Native OnAreaLoaded has finished entity activation and party
                // navmesh placement. This is BEFORE after-entry autosaving.
                var result = relationship.RestoreSavedPair(riders[0], mounts[0], transfer.ProfileId);
                if (!result.Succeeded)
                {
                    Report("Area relationship was not eligible: " + string.Join("; ", result.Errors));
                    return;
                }
                AreaResumeCount++;
                Report("Mounted relationship restored after native area placement without Mount or preparation.");
            }
            finally
            {
                controls.ResumeAreaControls();
                if (relationship.State == RelationshipState.Mounted) controls.RestorePersistentSlots(transfer.Slots);
            }
        }

        private void CompleteAreaTransitionIfReady()
        {
            if (areaTransfer == null) return;
            if (areaTransfer.World.Target != Game.Instance?.Player) { CancelAreaTransition(); return; }
            if (LoadingProcess.Instance.IsLoadingInProcess || LoadingProcess.Instance.QueuedNames.Any() ||
                NativeDeferredSave.Waiting(LoadingProcess.Instance)) return;
            // The exact native post-placement hook is required, never a late
            // automatic remount after a missed save barrier.
            if (!areaTransfer.ResumeAttempted)
                Report("Native area placement did not complete; relationship remains unmounted.");
            CancelAreaTransition();
        }

        private void CancelAreaTransition()
        {
            if (areaTransfer == null) return;
            areaTransfer = null;
            controls.ResumeAreaControls();
        }

        private sealed class AreaTransfer
        {
            internal WeakReference World;
            internal string CampaignId, AreaId, RiderId, MountId, ProfileId;
            internal SavedMountedSlot[] Slots;
            internal bool Suspended, ResumeAttempted;
        }
    }
}
