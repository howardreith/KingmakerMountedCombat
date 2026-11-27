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
        MountedFirstTickVerdict Decide(float allowedMetres, float deltaTime, out string detail);
    }
}
