using System;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // Ratnook door repair (2026-11-12). Each case drives the production transfer lifecycle through one native callback
    // order taken from the exact installed contracts (party-leave in AreaTransitionGroupCommand.ExecuteTransition
    // 0600263E, its synchronous Game.LoadArea 06000CC9 dispatch to Game.Teleport 06000CCB or the full LoadArea 06000CD5,
    // ITeleportHandler.HandlePartyTeleport inside TeleportParty, Game.OnAreaLoaded 06000CD7, and the idle loading queue).
    // OFFLINE decision evidence only: no native route was exercised by these tests.
    internal static class RatnookDoorRepairTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("ratnook door: a same-area door restores at its native teleport placement and never latches the idle failure", LocalDoorRestoresAtTeleportPlacement);
            runner.Run("ratnook door: the 0.2.3 incident order (local dispatch, no full-load callback) is never a permanent failure once placed", IncidentOrderWithoutFullLoadCallback);
            runner.Run("ratnook door: a dispatched route waits for its own placement and fails closed only if the queue idles without it", DispatchedRouteWaitsForOwnPlacement);
            runner.Run("ratnook door: a party-leave with no native dispatch restores in place once and accepts no later completion", RefusedBeforeDispatchRestoresInPlaceOnce);
            runner.Run("ratnook door: full area loads and same-area forced reloads complete only at OnAreaLoaded", FullLoadAndForcedReloadUseOnAreaLoaded);
            runner.Run("ratnook door: placement observed before suspension restores nothing and keeps the protective idle outcome", PlacementBeforeSuspension);
            runner.Run("ratnook door: duplicate placement callbacks and duplicate restores admit exactly one attempt", DuplicateCallbacksRestoreOnce);
            runner.Run("ratnook door: a foreign teleport that was current at the door's dispatch does not complete the door's transfer", ForeignCurrentTeleportIsStale);
            runner.Run("ratnook door: two local doors before the first teleport ran restore once, at the last placement", TwoDoorsRestoreAtLastPlacement);
            runner.Run("ratnook door: a newer full load supersedes a pending local teleport; the stale teleport callback is ignored", NewerFullLoadSupersedesTeleport);
            runner.Run("ratnook door: an attempted restoration finishes the transfer; its own failure latch is not the lifecycle's to clear", AttemptedRestoreFinishes);
            runner.Run("ratnook door: a local teleport is observed at dispatch, never assumed at construction", LocalRouteIsNeverAssumed);
        }

        private static MountedAreaTransferLifecycle PartyLeave()
        {
            var transfer = new MountedAreaTransferLifecycle(MountedAreaTransferRoute.Requested);
            transfer.ObserveSuspended();
            return transfer;
        }

        // ExecuteTransition: party-leave (Begin + SuspendAreaPair), then Game.LoadArea 06000CC9 -> Game.Teleport for an
        // enter point of the loaded area; TeleportParty later places the party and raises HandlePartyTeleport.
        private static void LocalDoorRestoresAtTeleportPlacement()
        {
            var transfer = PartyLeave();
            TestRunner.True(transfer.ObserveLocalTeleportDispatch(), "The door's own teleport dispatch was not bound to the transfer.");
            TestRunner.Equal(MountedAreaTransferRoute.LocalTeleport, transfer.Route, "The local route was not recorded.");
            TestRunner.True(!transfer.ObserveFullLoadPlacement(), "A full-load callback completed a local teleport transfer.");
            TestRunner.True(transfer.ObserveLocalTeleportPlacement(false), "The native teleport placement did not complete the door transfer.");
            TestRunner.True(transfer.BeginRestore(), "Restoration was not admitted after native placement.");
            TestRunner.Equal(MountedAreaTransferIdleDecision.Finish, transfer.DecideAtIdleLoader(), "A restored local door latched the idle failure.");
        }

        // Log lines 11460-11475 of the supplied 0.2.3 log: suspension, no full-load callbacks, then the latch at idle.
        // The 0.2.3 guard only knew OnAreaLoaded; with the route recorded the same order completes at HandlePartyTeleport.
        private static void IncidentOrderWithoutFullLoadCallback()
        {
            var transfer = PartyLeave();
            transfer.ObserveLocalTeleportDispatch();
            // Busy queue frames: nothing decided. The TeleportParty process then places the party and the queue idles.
            TestRunner.True(transfer.ObserveLocalTeleportPlacement(false) && transfer.BeginRestore(), "The incident order did not restore at its own placement.");
            TestRunner.True(transfer.DecideAtIdleLoader() != MountedAreaTransferIdleDecision.Fail, "The incident order still latched the whole-party command block.");
        }

        private static void DispatchedRouteWaitsForOwnPlacement()
        {
            foreach (var local in new[] { true, false })
            {
                var transfer = PartyLeave();
                if (local) transfer.ObserveLocalTeleportDispatch(); else transfer.ObserveFullLoadDispatch();
                // A dispatched route is never mistaken for a refused one: no in-place remount before native placement.
                TestRunner.True(!transfer.ObserveRefusedBeforeDispatch(), "A dispatched transfer was restored in place before native placement.");
                // The native chain dropped without its placement (StopAll, an exception): unknown placement stays protective.
                TestRunner.Equal(MountedAreaTransferIdleDecision.Fail, transfer.DecideAtIdleLoader(),
                    (local ? "Local" : "Full") + " dispatch without placement was treated as a clean finish.");
                TestRunner.True(!transfer.ResumeAttempted, "The protective outcome attempted restoration.");
            }
        }

        // The full LoadArea prefix refused the request (or the dispatch never ran): the dispatch is synchronous with the
        // party-leave event, so an idle queue without it means the party was not moved.
        private static void RefusedBeforeDispatchRestoresInPlaceOnce()
        {
            var transfer = PartyLeave();
            TestRunner.Equal(MountedAreaTransferIdleDecision.RestoreInPlace, transfer.DecideAtIdleLoader(), "A refused transition became a permanent failure.");
            TestRunner.True(transfer.ObserveRefusedBeforeDispatch(), "The in-place restoration was not admitted.");
            TestRunner.True(!transfer.ObserveRefusedBeforeDispatch(), "The in-place restoration was admitted twice.");
            TestRunner.True(transfer.BeginRestore(), "The in-place restoration attempt was refused.");
            TestRunner.True(!transfer.ObserveLocalTeleportDispatch() && !transfer.ObserveFullLoadDispatch(), "A late dispatch rebound a restored transfer.");
            TestRunner.True(!transfer.ObserveLocalTeleportPlacement(false) && !transfer.ObserveFullLoadPlacement(), "A late completion restored the pair a second time.");
            TestRunner.Equal(MountedAreaTransferIdleDecision.Finish, transfer.DecideAtIdleLoader(), "The restored transfer was not released.");

            var unsuspended = new MountedAreaTransferLifecycle(MountedAreaTransferRoute.Requested);
            TestRunner.Equal(MountedAreaTransferIdleDecision.Fail, unsuspended.DecideAtIdleLoader(), "An unsuspended transfer was treated as a clean in-place pair.");
        }

        private static void FullLoadAndForcedReloadUseOnAreaLoaded()
        {
            // A door to another area: party-leave first, then the admitted full LoadArea; OnAreaBeginUnloading suspends again (no-op).
            var door = PartyLeave();
            TestRunner.True(door.ObserveFullLoadDispatch(), "The full load was not recorded for a cross-area door.");
            TestRunner.True(!door.ObserveLocalTeleportDispatch(), "A full-load transfer was reclassified as a local teleport.");
            TestRunner.True(!door.ObserveLocalTeleportPlacement(false), "A teleport callback completed a full area load.");
            TestRunner.True(door.ObserveFullLoadPlacement() && door.BeginRestore(), "OnAreaLoaded did not restore the cross-area door.");

            // A same-area forced reload or a world-map arrival reaches only the full LoadArea prefix (no party-leave).
            var reload = new MountedAreaTransferLifecycle(MountedAreaTransferRoute.FullLoad);
            TestRunner.True(!reload.ObserveLocalTeleportDispatch(), "A forced reload of the current area was misread as a local shortcut.");
            reload.ObserveSuspended();
            TestRunner.True(!reload.ObserveLocalTeleportPlacement(false), "A teleport callback completed a forced reload.");
            TestRunner.True(reload.ObserveFullLoadPlacement() && reload.BeginRestore(), "OnAreaLoaded did not restore the forced reload.");
            TestRunner.Equal(MountedAreaTransferIdleDecision.Finish, reload.DecideAtIdleLoader(), "A restored forced reload latched the failure.");
        }

        private static void PlacementBeforeSuspension()
        {
            var full = new MountedAreaTransferLifecycle(MountedAreaTransferRoute.FullLoad);
            TestRunner.True(!full.ObserveFullLoadPlacement(), "OnAreaLoaded before suspension restored a pair that was never suspended.");
            full.ObserveSuspended();
            TestRunner.True(full.ObserveFullLoadPlacement(), "The first placement after suspension was refused.");

            var local = new MountedAreaTransferLifecycle(MountedAreaTransferRoute.Requested);
            local.ObserveLocalTeleportDispatch();
            TestRunner.True(!local.ObserveLocalTeleportPlacement(false) && !local.BeginRestore(), "A teleport placement before suspension restored a pair.");
            TestRunner.Equal(MountedAreaTransferIdleDecision.Fail, local.DecideAtIdleLoader(), "An unknown unsuspended placement released its guard as clean.");
        }

        private static void DuplicateCallbacksRestoreOnce()
        {
            var transfer = PartyLeave();
            transfer.ObserveLocalTeleportDispatch();
            TestRunner.True(!transfer.ObserveLocalTeleportDispatch(), "A duplicate teleport dispatch rebound the transfer.");
            TestRunner.True(transfer.ObserveLocalTeleportPlacement(false), "The first placement was refused.");
            TestRunner.True(!transfer.ObserveLocalTeleportPlacement(false), "A duplicate HandlePartyTeleport completed the transfer twice.");
            TestRunner.True(transfer.BeginRestore(), "The single restore was refused.");
            TestRunner.True(!transfer.BeginRestore(), "A duplicate restore was admitted.");
        }

        // Review P2 sequence A: a cutscene/capital teleport is the current process (not yet run) when the door dispatches;
        // the door's TeleportParty is then queued behind it while the foreign one raises HandlePartyTeleport.
        private static void ForeignCurrentTeleportIsStale()
        {
            var transfer = PartyLeave();
            TestRunner.True(transfer.ObserveLocalTeleportDispatch(), "The door's dispatch behind a running teleport was not bound.");
            TestRunner.True(!transfer.ObserveLocalTeleportPlacement(true), "The foreign teleport's callback restored the door's pair before its placement.");
            TestRunner.True(!transfer.ResumeAttempted && !transfer.Placed, "A deferred placement consumed the transfer.");
            TestRunner.True(transfer.ObserveLocalTeleportPlacement(false) && transfer.BeginRestore(), "The door's own placement was not recognised.");
        }

        // Review P2 sequence B: a second door's party-leave finds the unfinished transfer (suspension is a no-op) and its
        // dispatch keeps the route; the first door's callback runs while the second TeleportParty is still queued.
        private static void TwoDoorsRestoreAtLastPlacement()
        {
            var transfer = PartyLeave();
            transfer.ObserveLocalTeleportDispatch();
            TestRunner.True(!transfer.ObserveLocalTeleportDispatch(), "The second door rebound the route.");
            TestRunner.True(!transfer.ObserveLocalTeleportPlacement(true), "The first door's placement restored before the second teleport moved the party.");
            TestRunner.True(transfer.ObserveLocalTeleportPlacement(false), "The last placement did not complete the transfer.");
            TestRunner.True(transfer.BeginRestore() && !transfer.ObserveLocalTeleportPlacement(false), "The pair was restored more than once.");
        }

        private static void NewerFullLoadSupersedesTeleport()
        {
            var transfer = PartyLeave();
            transfer.ObserveLocalTeleportDispatch();
            TestRunner.True(transfer.ObserveFullLoadDispatch(), "A newer full load did not supersede the pending local teleport.");
            TestRunner.True(!transfer.ObserveLocalTeleportPlacement(false), "The superseded teleport's callback restored before the final full-load placement.");
            TestRunner.True(transfer.ObserveFullLoadPlacement() && transfer.BeginRestore(), "The superseding load did not restore at its own placement.");
        }

        // A restore that left owned state latches areaRestoreFailure inside RestoreAreaPair itself; the idle boundary
        // releases only the transfer record and never clears that latch (load housekeeping does, after cleanup).
        private static void AttemptedRestoreFinishes()
        {
            var transfer = PartyLeave();
            transfer.ObserveLocalTeleportDispatch();
            transfer.ObserveLocalTeleportPlacement(false);
            transfer.BeginRestore();
            TestRunner.Equal(MountedAreaTransferIdleDecision.Finish, transfer.DecideAtIdleLoader(), "An attempted restore was retried or reclassified.");
            TestRunner.True(!transfer.ObserveRefusedBeforeDispatch(), "An attempted restore was offered a second in-place attempt.");
        }

        private static void LocalRouteIsNeverAssumed()
        {
            var refused = false;
            try { new MountedAreaTransferLifecycle(MountedAreaTransferRoute.LocalTeleport); }
            catch (ArgumentException) { refused = true; }
            TestRunner.True(refused, "A transfer was constructed as local without observing the native teleport dispatch.");
        }
    }
}
