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
    //   UnitMovementAgent.FollowPrecomputedPath(Path p, float) 060018B6: its only installed caller is UnitCommand.TickApproaching
    //     (060027A6), which passes a FRESH ForcedPath copy of the ticking command's ForcedPath (or of the visualized path) with no command
    //     argument; FollowPrecomputedPath sets m_RequestedPath to it and calls OnPathComplete synchronously. Round 22 (R19-F5): the product's
    //     transpiler on TickApproaching (ForcedPathCallerTranspiler) replaces that single call with FollowPrecomputedPathFrom(agent, p,
    //     radius, this), which carries the exact executing command across the call; the FollowPrecomputedPath prefix records the forced
    //     request for that command only, and the OnPathComplete hooks then record the delivery under the same rule as a PathTo request.
    // ForcePath (UnitMoveAlongPath and ability movement) is not called through the transpiled site and is never attributed to a carrier.
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

        // PR #70 R19-F5 (round 22): the command whose TickApproaching is making the transpiled FollowPrecomputedPath call, for exactly the duration
        // of that call (null otherwise). Written only by FollowPrecomputedPathFrom (and cleared by ReleaseForcedIssuer at teardown).
        private static Kingmaker.UnitLogic.Commands.Base.UnitCommand forcedIssuer;

        internal static Kingmaker.UnitLogic.Commands.Base.UnitCommand CurrentForcedIssuer => forcedIssuer;

        // The transpiled TickApproaching call site (ForcedPathCallerTranspiler): the native call is made exactly once with its original agent,
        // path and radius; the caller is `this` of the executing TickApproaching. The issuer is set in this method's own try/finally (exception-safe
        // without a Harmony finalizer): a native exception propagates unchanged and the previous issuer is restored, so nested calls stay exact
        // and nothing is retained after an exception.
        internal static void FollowPrecomputedPathFrom(Kingmaker.View.UnitMovementAgent agent, Pathfinding.Path p, float radius,
            Kingmaker.UnitLogic.Commands.Base.UnitCommand caller)
        {
            var previous = forcedIssuer;
            forcedIssuer = caller;
            try { agent.FollowPrecomputedPath(p, radius); }
            finally { forcedIssuer = previous; }
        }

        // Teardown (MountedServiceTeardownSequence.ModDisable on success): no issuer survives disable or unload.
        internal static void ReleaseForcedIssuer() => forcedIssuer = null;

        // FollowPrecomputedPath prefix: the forced request of the exact executing command. It is recorded only for a carrier admitted on this
        // agent (MountedCarrierApproachLedger.RecordRequest records nothing for an unregistered issuer and only releases the replaced
        // outstanding request); no other command, whatever its route, receives credit.
        internal static void BeforeForcedRequest(Kingmaker.View.UnitMovementAgent agent, Pathfinding.Path p)
        {
            var issuer = forcedIssuer;
            if (p == null || issuer == null || !Tracks(agent)) return;
            KingmakerMountedCombat.Domain.MountedCarrierApproachLedger.RecordRequest(agent,
                KingmakerMountedCombat.Domain.MountedCarrierApproachLedger.IsAdmittedOn(issuer, agent) ? issuer : null, p);
        }

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
