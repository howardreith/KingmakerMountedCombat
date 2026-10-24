namespace KingmakerMountedCombat.Diagnostics
{
    // Pure geometry terms of the 7E F04 staged legs and the navmesh proposal search. Each proposal
    // records the first term that refused it, so a refused search names its refusing condition.
    // No native or Unity dependency (compiled alone by its focused regression).
    internal static class Chunk7StagedGeometry
    {
        // Exhaustion legs stay beyond weapon range: envelope + 2 m to envelope + 6 m from the target.
        internal const float ExhaustBandFloor = 2f, ExhaustBandCeiling = 6f;
        // The beyond-range leg aims at envelope + 3 m. Its goal is accepted from that aim upward, not
        // 0.5 m below it: the native ground mover stops short of its goal (b202 st: planned 6.48 m,
        // displaced 6.21 m, ending 19.24 m from the target, under the reader's envelope + 2.5 m).
        internal const float BeyondMargin = 3f, BeyondBandBelow = 0f, BeyondBandAbove = 1.5f;
        internal const float MaximumTraceResidual = .3f, MinimumLegLength = .5f;
        internal const string NotWalkable = "not-walkable", TraceInexact = "trace-inexact", TooShort = "too-short",
            BelowBand = "below-band", AboveBand = "above-band", NoExhaustContinuation = "no-exhaust-continuation";

        internal static string Band(double distance, double floor, double ceiling) =>
            distance < floor ? BelowBand : distance > ceiling ? AboveBand : null;
        internal static string ExhaustBand(double targetDistance, double envelope) =>
            Band(targetDistance, envelope + ExhaustBandFloor, envelope + ExhaustBandCeiling);
        internal static string BeyondBand(double targetDistance, double envelope) =>
            Band(targetDistance, envelope + BeyondMargin - BeyondBandBelow, envelope + BeyondMargin + BeyondBandAbove);
        // The first refusing term in the order eligibility applies them (walkable node, exact native
        // trace, a real leg, then the declared geometry); null is an eligible proposal.
        internal static string Refusal(bool walkable, double traceResidual, double legLength, string geometry) =>
            !walkable ? NotWalkable : traceResidual > MaximumTraceResidual ? TraceInexact : legLength <= MinimumLegLength ? TooShort : geometry;
    }
}
