namespace KingmakerMountedCombat.Integration
{
    // PR #70 R17-F1/R18-F2 (round 19): the production observer behind the two installed approach boundaries, the only writer of an exact
    // carrier's issuance and delivery facts (KingmakerMountedCombat.Domain.MountedCarrierApproachLedger). Its hooks
    // (MountedPatchController.PatchMethods.CarrierPathRequest* and CarrierPathComplete*) are installed with PatchExact on
    //   UnitMovementAgent.PathTo(UnitCommand command, Vector3 destination, float, float, UnitMovementAgentBase) 060018A3: native
    //     UnitCommand.TickApproaching -> UnitEntityView.MoveTo -> PathTo carries the exact command whose approach is ticking; a NEW object in
    //     m_RequestedPath (0400118E) after the call is the request that command issued (an outstanding request toward the same destination is
    //     kept by native without a new request, and is not attributed to the caller);
    //   UnitMovementAgent.OnPathComplete(Path p) 060018B9: native acts only when p is m_RequestedPath (otherwise it returns at once), clears
    //     it, and either makes p the agent's Path (set_Path, SetWaypoint(1), OnMovementStarted: the delivery) or returns without delivering
    //     (no destination, or a failed/not-found path: OnPathNotFound). set_Path and set_RequestedPath claim their path (ClaimPath), so A*
    //     cannot hand back the object the agent currently holds as Path: a pooled object reused for a delivery was released earlier.
    // Forced paths (UnitCommand.TickApproaching -> FollowPrecomputedPath, and ForcePath) set m_RequestedPath and complete synchronously without
    // PathTo: they are deliveries no admitted carrier requested (the route-bound override carrier keeps its own displacement criterion).
    // Both hooks are filtered to agents an admitted carrier was issued to (one weak-table lookup) and never change native state.
    internal static class MountedCarrierPathObserver
    {
        internal const int CompletionIgnored = 0, CompletionOfRequest = 1, CompletionOfHeldPath = 2;

        private static readonly System.Reflection.FieldInfo RequestedPathField = typeof(Kingmaker.View.UnitMovementAgent).GetField("m_RequestedPath",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

        // Checked when the hooks are installed: the pinned field (token, type) the observer reads.
        internal static bool RequestedPathContractHolds(int expectedToken) =>
            RequestedPathField != null && RequestedPathField.FieldType == typeof(Pathfinding.Path) && RequestedPathField.MetadataToken == expectedToken;

        internal static Pathfinding.Path RequestedPathOf(Kingmaker.View.UnitMovementAgent agent) =>
            agent == null || RequestedPathField == null ? null : RequestedPathField.GetValue(agent) as Pathfinding.Path;

        private static bool Tracks(Kingmaker.View.UnitMovementAgent agent) => KingmakerMountedCombat.Domain.MountedCarrierApproachLedger.Tracks(agent);

        // PathTo prefix: the outstanding request before native decides.
        internal static Pathfinding.Path BeforeRequest(Kingmaker.View.UnitMovementAgent agent) => Tracks(agent) ? RequestedPathOf(agent) : null;

        // PathTo postfix: a new request object is the request the carried command issued.
        internal static void AfterRequest(Kingmaker.View.UnitMovementAgent agent, Kingmaker.UnitLogic.Commands.Base.UnitCommand command, Pathfinding.Path before)
        {
            if (!Tracks(agent)) return;
            var requested = RequestedPathOf(agent);
            if (requested != null && !ReferenceEquals(requested, before))
                KingmakerMountedCombat.Domain.MountedCarrierApproachLedger.RecordRequest(agent, command, requested);
        }

        // OnPathComplete prefix: whether native will act on this completion (p is the outstanding request). A completion of a path the agent
        // already holds is not natively reachable (the held path is claimed) and is never counted as a delivery (it is indistinguishable from
        // not-found, which leaves Path unchanged).
        internal static int BeforeCompletion(Kingmaker.View.UnitMovementAgent agent, Pathfinding.Path p)
        {
            if (p == null || !Tracks(agent) || !ReferenceEquals(RequestedPathOf(agent), p)) return CompletionIgnored;
            return ReferenceEquals(agent.Path, p) ? CompletionOfHeldPath : CompletionOfRequest;
        }

        // OnPathComplete postfix: a delivery is native having made p the agent's Path.
        internal static void AfterCompletion(Kingmaker.View.UnitMovementAgent agent, Pathfinding.Path p, int before)
        {
            if (before == CompletionIgnored) return;
            KingmakerMountedCombat.Domain.MountedCarrierApproachLedger.RecordCompletion(agent, p, before == CompletionOfRequest && ReferenceEquals(agent.Path, p));
        }

        internal static KingmakerMountedCombat.Domain.MountedCarrierApproachPhase PhaseOf(Kingmaker.UnitLogic.Commands.Base.UnitCommand carrier,
            Kingmaker.View.UnitMovementAgent agent) =>
            KingmakerMountedCombat.Domain.MountedCarrierApproachLedger.PhaseOf(carrier, agent?.Path, RequestedPathOf(agent));
    }
}
