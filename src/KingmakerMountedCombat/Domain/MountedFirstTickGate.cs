namespace KingmakerMountedCombat.Domain
{
    // PR #70 R1-F6 (round 6): the first-tick replacement boundary (docs/missions/mmr-split/PR-2-first-tick-boundary.md). A provisional
    // replacement request owns one carrier. That carrier's FIRST delegated movement tick is the only point at which the real allowance of
    // the restricted single-action move (one engine tick) is known, before any debit. The adapter asks the request's gate there; the gate
    // commits the request (the previous order is retired once, through the normal path, and the tick moves and is debited normally),
    // commits it in place (the strike is already admissible: the tick does not move), or refuses it (the tick is zeroed before any debit).
    public enum MountedFirstTickVerdict { Refuse, Commit, CommitInPlace }

    public enum MountedFirstTickProposalState { Pending, Committed, Refused }

    public interface IMountedFirstTickGate
    {
        // The exact carrier command the request installed; the gate applies only to a tick of this command.
        object Carrier { get; }
        // PR #70 R6-F2 (round 8): a commit also states the metres it grants this tick (the adapter shortens the tick to them, never lengthens it).
        // Round 9 (R8-F5): movementContext is the input context the executing movement adapter debits with (its mode and grant decide).
        // PR #70 round 16 (R13-F5, R15-F1, R15-F2): the allowance is the EXECUTABLE current tick (a step's already bounded by its installed
        // endpoint completion) and stepTravelLeft the physical step remainder every candidate's settled travel must fit (+Infinity: not a step).
        // Round 17 (R16-F4): the allowance and the approval are DURATIONS (allowedTick: the permitted duration exactly as the adapter would
        // return it; approvedTick: the selected duration the adapter returns and debits unchanged) and speed is the MaxSpeed the adapter
        // captured for this tick: no duration is reconstructed from metres, so the decision and the engine select waypoints with the same float.
        MountedFirstTickVerdict Decide(object movementContext, float allowedTick, float deltaTime, float speed, float stepTravelLeft, out float approvedTick, out string detail);
    }
}
