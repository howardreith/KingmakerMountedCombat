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
    //     radius, this), which carries the exact (agent, path, issuer) of every active forced call. Round 23 (R22-F1): the forced request is
    //     recorded where native actually installs it: the OnPathComplete prefix of that call (p is then m_RequestedPath and matches the active
    //     forced call's own agent and path), so an outstanding request stays outstanding until native replaces it, and the OnPathComplete
    //     postfix records the delivery under the same rule as a PathTo request.
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

        // PR #70 R19-F5 (round 22), R22-F1 (round 23): the active transpiled FollowPrecomputedPath calls, innermost first, each with its exact agent,
        // path and issuer, for exactly the duration of the call. Written only by FollowPrecomputedPathFrom (and cleared by ReleaseForcedIssuer at
        // teardown); the entries are immutable.
        private sealed class ForcedCall
        {
            internal Kingmaker.View.UnitMovementAgent Agent;
            internal Pathfinding.Path Path;
            internal Kingmaker.UnitLogic.Commands.Base.UnitCommand Issuer;
            internal ForcedCall Outer;
        }
        private static ForcedCall activeForcedCall;

        internal static Kingmaker.UnitLogic.Commands.Base.UnitCommand CurrentForcedIssuer => activeForcedCall?.Issuer;

        // The transpiled TickApproaching call site (ForcedPathCallerTranspiler): the native call is made exactly once with its original agent,
        // path and radius; the caller is `this` of the executing TickApproaching. The issuer is set in this method's own try/finally (exception-safe
        // without a Harmony finalizer): a native exception propagates unchanged and the previous issuer is restored, so nested calls stay exact
        // and nothing is retained after an exception.
        internal static void FollowPrecomputedPathFrom(Kingmaker.View.UnitMovementAgent agent, Pathfinding.Path p, float radius,
            Kingmaker.UnitLogic.Commands.Base.UnitCommand caller)
        {
            var call = new ForcedCall { Agent = agent, Path = p, Issuer = caller, Outer = activeForcedCall };
            activeForcedCall = call;
            try { agent.FollowPrecomputedPath(p, radius); }
            finally { activeForcedCall = call.Outer; }
        }

        // Teardown (MountedServiceTeardownSequence.ModDisable on success): no forced call survives disable or unload.
        internal static void ReleaseForcedIssuer() => activeForcedCall = null;

        // Called from the OnPathComplete prefix once native has installed p as m_RequestedPath: an active forced call with this exact agent and
        // path made that request, and it is recorded now (the outstanding request it replaces was outstanding until this assignment). It is
        // recorded only for an issuer admitted on this agent (RecordRequest records nothing for an unregistered issuer and only releases the
        // replaced outstanding request); no other command, whatever its route, receives credit.
        private static void RecordForcedRequestAtAssignment(Kingmaker.View.UnitMovementAgent agent, Pathfinding.Path p)
        {
            for (var call = activeForcedCall; call != null; call = call.Outer)
            {
                if (!ReferenceEquals(call.Agent, agent) || !ReferenceEquals(call.Path, p)) continue;
                KingmakerMountedCombat.Domain.MountedCarrierApproachLedger.RecordRequest(agent,
                    KingmakerMountedCombat.Domain.MountedCarrierApproachLedger.IsAdmittedOn(call.Issuer, agent) ? call.Issuer : null, p);
                return;
            }
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
            RecordForcedRequestAtAssignment(agent, p);
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
