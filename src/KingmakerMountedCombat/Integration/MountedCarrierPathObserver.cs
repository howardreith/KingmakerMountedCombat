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
    //   UnitMovementAgent.FollowPrecomputedPath(Path p, float) 060018B6 (round 19 follow-up): its only installed caller is
    //     UnitCommand.TickApproaching (060027A6), which passes a FRESH ForcedPath copy of the ticking command's ForcedPath (or of the visualized
    //     path) with no command argument; FollowPrecomputedPath sets m_RequestedPath to it and calls OnPathComplete synchronously. The
    //     product's TickApproaching prefix and postfix scope the exact ticking command (EnterApproach/ExitApproach, a stack), and the
    //     FollowPrecomputedPath prefix records the forced request for that command; the OnPathComplete hooks then record the delivery under the
    //     same rule as a PathTo request. (Reference identity with the carrier's own ForcedPath cannot bind it: native passes a copy.)
    // ForcePath (UnitMoveAlongPath and ability movement) has no command scope and is never attributed to a carrier.
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

        // PR #70 R19-F5 (round 20): the commands whose native TickApproaching is running, innermost last. Harmony 1.2 (the installed Harmony12)
        // has no finalizer, so a body that throws skips its postfix; the scope is therefore exception-safe by construction: entries hold the
        // command WEAKLY (a failed scope never roots its command or actors), and an entry from an earlier engine frame can only be a scope whose
        // body threw (TickApproaching never spans frames), so it is discarded when the next scope opens; a collected command is discarded too.
        // An active outer scope is always from the current frame and is never discarded (no count-based clearing). A postfix ends its own scope
        // and any nested scope whose body threw inside it. ReleaseScopes (MountedServiceTeardownSequence.ModDisable, both disable and unload)
        // drops every entry.
        private struct ApproachScope
        {
            internal System.WeakReference Command;
            internal int Frame;
        }
        private static readonly System.Collections.Generic.List<ApproachScope> approaching = new System.Collections.Generic.List<ApproachScope>();

        private static void DiscardStaleScopes()
        {
            var frame = UnityEngine.Time.frameCount;
            for (var i = approaching.Count - 1; i >= 0; i--)
                if (approaching[i].Frame != frame || !approaching[i].Command.IsAlive) approaching.RemoveAt(i);
        }

        // TickApproaching prefix (first statement of MountedPatchController.PatchMethods.MountedAttackApproachPrefix).
        internal static void EnterApproach(Kingmaker.UnitLogic.Commands.Base.UnitCommand command)
        {
            if (command == null) return;
            DiscardStaleScopes();
            approaching.Add(new ApproachScope { Command = new System.WeakReference(command), Frame = UnityEngine.Time.frameCount });
        }

        // TickApproaching postfix (Harmony runs it whether or not the prefix skipped the native body): this command's innermost scope ends, with
        // every scope nested inside it (their bodies threw).
        internal static void ExitApproach(Kingmaker.UnitLogic.Commands.Base.UnitCommand command)
        {
            for (var i = approaching.Count - 1; i >= 0; i--)
                if (ReferenceEquals(approaching[i].Command.Target, command)) { approaching.RemoveRange(i, approaching.Count - i); return; }
        }

        // The innermost live scope of this frame (the command whose TickApproaching is running).
        internal static Kingmaker.UnitLogic.Commands.Base.UnitCommand CurrentIssuer
        {
            get
            {
                var frame = UnityEngine.Time.frameCount;
                for (var i = approaching.Count - 1; i >= 0; i--)
                {
                    var command = approaching[i].Command.Target as Kingmaker.UnitLogic.Commands.Base.UnitCommand;
                    if (approaching[i].Frame == frame && command != null) return command;
                }
                return null;
            }
        }

        internal static int RetainedScopeCount => approaching.Count;

        // Teardown (MountedServiceTeardownSequence.ModDisable on success): no scope survives disable or unload.
        internal static void ReleaseScopes() => approaching.Clear();

        // FollowPrecomputedPath prefix: the forced request the innermost ticking command issues (before set_RequestedPath and OnPathComplete).
        internal static void BeforeForcedRequest(Kingmaker.View.UnitMovementAgent agent, Pathfinding.Path p)
        {
            if (p == null || !Tracks(agent)) return;
            var issuer = CurrentIssuer;
            if (issuer != null) KingmakerMountedCombat.Domain.MountedCarrierApproachLedger.RecordRequest(agent, issuer, p);
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
