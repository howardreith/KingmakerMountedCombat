using System;

namespace KingmakerMountedCombat.Domain
{
    // Ratnook door repair (2026-11-12): the native route of one mounted area transfer.
    //
    // Exact installed contracts (Kingmaker SHA-256 3b6450ff..., MVID 07fa1e4d-...):
    // AreaTransitionGroupCommand.ExecuteTransition (0600263E) is the only raiser of IPartyLeaveAreaHandler and calls
    // Game.LoadArea(BlueprintAreaEnterPoint, AutoSaveMode) (06000CC9) synchronously right after it. That overload sends
    // an enter point of the currently loaded area to Game.Teleport (06000CCB) and every other enter point to the full
    // Game.LoadArea (06000CD5). The full load queues OnAreaLoaded (06000CD7); Teleport queues only the TeleportParty
    // loading process, which places the party (Player.MoveCharacters 06000DCC) and raises ITeleportHandler.
    // HandlePartyTeleport (06007512). A local door therefore never emits the full-load arrival callback.
    public enum MountedAreaTransferRoute
    {
        // The party left through a door; no native dispatch has been observed yet.
        Requested,
        // The full Game.LoadArea was admitted for this destination; its OnAreaLoaded is the placement.
        FullLoad,
        // Game.Teleport was dispatched to an enter point of the current area; HandlePartyTeleport is the placement.
        LocalTeleport
    }

    // What the transfer's guard does when the native loading queue is idle.
    public enum MountedAreaTransferIdleDecision
    {
        // Restoration was attempted (it latches its own failure); release the transfer.
        Finish,
        // The party left but no native dispatch happened (the synchronous dispatch was refused or never ran): the
        // party was not moved, so the suspended pairs restore in place through the all-pair restoration.
        RestoreInPlace,
        // A dispatched route (or an unsuspended transfer) reached idle without its own placement and restoration:
        // native state is unknown, so the protective failure latch keeps commands and saving blocked.
        Fail
    }

    // One transfer's phase record. Pure state; the integration owns every native call and delegates each decision here.
    public sealed class MountedAreaTransferLifecycle
    {
        public MountedAreaTransferLifecycle(MountedAreaTransferRoute initialRoute)
        {
            if (initialRoute == MountedAreaTransferRoute.LocalTeleport)
                throw new ArgumentException("A local teleport is observed at its native dispatch, never assumed.", nameof(initialRoute));
            Route = initialRoute;
        }

        public MountedAreaTransferRoute Route { get; private set; }
        public bool Suspended { get; private set; }
        public bool Placed { get; private set; }
        public bool ResumeAttempted { get; private set; }
        public bool RefusedBeforeDispatch { get; private set; }

        public void ObserveSuspended() => Suspended = true;

        // Harmony prefix of the full LoadArea admitted for this transfer's destination. A newer full load that replaces a
        // pending local teleport supersedes it: the queue is FIFO, so the full load's OnAreaLoaded is the final placement
        // and the teleport's earlier HandlePartyTeleport is no longer this transfer's completion.
        public bool ObserveFullLoadDispatch()
        {
            if (ResumeAttempted || Placed || RefusedBeforeDispatch) return false;
            Route = MountedAreaTransferRoute.FullLoad;
            return true;
        }

        // Harmony prefix of Game.Teleport for an enter point of the transfer's destination, which is the loaded area.
        // A second local dispatch for the same unfinished transfer (a second door before the first teleport ran) keeps
        // the route; the placement rule below waits for the last queued teleport.
        public bool ObserveLocalTeleportDispatch()
        {
            if (Route != MountedAreaTransferRoute.Requested || ResumeAttempted || Placed || RefusedBeforeDispatch) return false;
            Route = MountedAreaTransferRoute.LocalTeleport;
            return true;
        }

        // Native Game.OnAreaLoaded. True once: this transfer's full load placed the party after suspension.
        public bool ObserveFullLoadPlacement() => ObservePlacement(MountedAreaTransferRoute.FullLoad);

        // Native ITeleportHandler.HandlePartyTeleport, raised inside the current TeleportParty process. The loading queue
        // is FIFO and every TeleportParty dispatched before or with the door's own is either the current process (this
        // callback) or still queued; so while another TeleportParty is queued this callback is not the final placement
        // (a foreign teleport that was current when the door dispatched, or an earlier of two doors). True once.
        public bool ObserveLocalTeleportPlacement(bool anotherTeleportQueued)
        {
            if (Route != MountedAreaTransferRoute.LocalTeleport || Placed || ResumeAttempted || anotherTeleportQueued) return false;
            return ObservePlacement(MountedAreaTransferRoute.LocalTeleport);
        }

        private bool ObservePlacement(MountedAreaTransferRoute route)
        {
            // Placement before suspension is not this transfer's completion: nothing was suspended to restore.
            if (Route != route || !Suspended || Placed || ResumeAttempted) return false;
            Placed = true;
            return true;
        }

        // The single restoration attempt (successful or latched by the restorer itself).
        public bool BeginRestore()
        {
            if (!Suspended || ResumeAttempted) return false;
            ResumeAttempted = true;
            return true;
        }

        public MountedAreaTransferIdleDecision DecideAtIdleLoader()
        {
            if (ResumeAttempted) return MountedAreaTransferIdleDecision.Finish;
            if (Suspended && Route == MountedAreaTransferRoute.Requested && !Placed && !RefusedBeforeDispatch)
                return MountedAreaTransferIdleDecision.RestoreInPlace;
            return MountedAreaTransferIdleDecision.Fail;
        }

        // The integration confirmed the party is still in the departure area of the same world: restore in place once.
        public bool ObserveRefusedBeforeDispatch()
        {
            if (DecideAtIdleLoader() != MountedAreaTransferIdleDecision.RestoreInPlace) return false;
            RefusedBeforeDispatch = true;
            Placed = true;
            return true;
        }

        public string Describe() =>
            "route=" + Route + "; suspended=" + Suspended + "; placed=" + Placed + "; resumeAttempted=" + ResumeAttempted +
            (RefusedBeforeDispatch ? "; refusedBeforeDispatch=True" : "");
    }
}
