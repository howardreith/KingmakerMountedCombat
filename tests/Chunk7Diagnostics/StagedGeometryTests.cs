using System;
using System.Linq;
using KingmakerMountedCombat.Diagnostics;

namespace KingmakerMountedCombat.Tests
{
    // Focused regression for the 7E F04 staged leg geometry terms, on the real b202 st exhaustion
    // leg (run 20261024-c7e-b202-st-owner-seed-staged, step 6, leg 0, all 48 recorded proposals).
    internal static class StagedGeometryTests
    {
        private struct P
        {
            internal readonly float Angle, Length, Residual, X, Z; internal readonly bool Walkable;
            internal P(float angle, float length, bool walkable, float residual, float x, float z) { Angle = angle; Length = length; Walkable = walkable; Residual = residual; X = x; Z = z; }
        }
        private const float Envelope = 16.84f, TargetX = -19.916172f, TargetZ = -5.26272964f, StartX = -22.9889965f, StartZ = -24.2515965f;
        private const double BeyondEnd = 19.2358856;
        private static readonly P[] B202ExhaustLeg = {
            new P(0f, 7f, false, 6.999994f, -29.8991f, -23.1333866f),
            new P(22.5f, 7f, false, 6.99999666f, -28.94518f, -20.5741215f),
            new P(-22.5f, 7f, false, 6.99998856f, -29.8010159f, -25.8628788f),
            new P(45f, 7f, false, 6.999995f, -27.0844784f, -18.5747166f),
            new P(-45f, 7f, false, 6.999994f, -28.6658688f, -28.347086f),
            new P(67.5f, 7f, false, 6.99999571f, -24.6002789f, -17.43957f),
            new P(-67.5f, 7f, false, 6.999999f, -26.6664639f, -30.2077885f),
            new P(90f, 7f, true, 0f, -21.870779f, -17.341486f),
            new P(-90f, 7f, false, 7.00000048f, -24.1071987f, -31.1617069f),
            new P(112.5f, 7f, true, 0f, -19.3115139f, -18.2954044f),
            new P(-112.5f, 7f, false, 6.99999952f, -21.3776989f, -31.0636234f),
            new P(135f, 7f, true, 0f, -17.312109f, -20.1561069f),
            new P(-135f, 7f, false, 7.00000429f, -18.8935f, -29.9284763f),
            new P(157.5f, 7f, true, 0f, -16.1769619f, -22.6402988f),
            new P(-157.5f, 7f, false, 7.00000334f, -17.0328045f, -27.9290714f),
            new P(180f, 7f, false, 7.000009f, -16.0788784f, -25.3698063f),
            new P(0f, 5f, false, 4.99999142f, -27.92478f, -23.4528751f),
            new P(22.5f, 5f, false, 4.99999237f, -27.2434063f, -21.6248264f),
            new P(-22.5f, 5f, false, 4.99999571f, -27.8547268f, -25.4025211f),
            new P(45f, 5f, false, 5f, -25.9143429f, -20.19668f),
            new P(-45f, 5f, false, 4.999998f, -27.0439053f, -27.17695f),
            new P(67.5f, 5f, true, 0f, -23.9901752f, -19.3139839f),
            new P(-67.5f, 5f, false, 4.99998856f, -25.6157589f, -28.5060062f),
            new P(90f, 5f, true, 0f, -22.1222477f, -19.28315f),
            new P(-90f, 5f, false, 4.99999762f, -23.78771f, -29.1873875f),
            new P(112.5f, 5f, true, 0f, -20.3622189f, -19.9971714f),
            new P(-112.5f, 5f, false, 5.00000525f, -21.8380642f, -29.1173344f),
            new P(135f, 5f, true, 0f, -18.93408f, -21.3262424f),
            new P(-135f, 5f, false, 5.00000429f, -20.0636425f, -28.3065128f),
            new P(157.5f, 5f, true, 0f, -18.1232586f, -23.1006718f),
            new P(-157.5f, 5f, false, 5.00000525f, -18.7345715f, -26.8783665f),
            new P(180f, 5f, false, 5.00000668f, -18.0531979f, -25.0503178f),
            new P(0f, 3.5f, false, 3.49999452f, -26.4440441f, -23.6924839f),
            new P(22.5f, 3.5f, false, 3.49999928f, -25.9670849f, -22.4128513f),
            new P(-22.5f, 3.5f, false, 3.49999213f, -26.3950024f, -25.0572453f),
            new P(45f, 3.5f, false, 3.49999547f, -25.0367336f, -21.4131565f),
            new P(-45f, 3.5f, false, 3.4999938f, -25.8274288f, -26.2993412f),
            new P(67.5f, 3.5f, true, 0f, -23.7946377f, -20.8455753f),
            new P(-67.5f, 3.5f, false, 3.49999118f, -24.8277264f, -27.2296848f),
            new P(90f, 3.5f, true, 0f, -22.429884f, -20.7965336f),
            new P(-90f, 3.5f, false, 3.499992f, -23.5480938f, -27.7066441f),
            new P(112.5f, 3.5f, true, 0f, -21.1502514f, -21.2734928f),
            new P(-112.5f, 3.5f, false, 3.499994f, -22.18334f, -27.6576023f),
            new P(135f, 3.5f, true, 0f, -20.1693039f, -22.22338f),
            new P(-135f, 3.5f, false, 3.50000453f, -20.9412441f, -27.0900364f),
            new P(157.5f, 3.5f, true, 0f, -19.5829754f, -23.4459476f),
            new P(-157.5f, 3.5f, false, 3.500004f, -20.0108929f, -26.0903263f),
            new P(180f, 3.5f, false, 3.50000715f, -19.5339336f, -24.8106937f),
        };
        private static int failures, passes;
        private static void Check(bool condition, string name) { if (condition) passes++; else { failures++; Console.WriteLine("FAIL " + name); } }
        private static double Flat(float x1, float z1, float x2, float z2) => Math.Sqrt((x1 - x2) * (x1 - x2) + (z1 - z2) * (z1 - z2));
        private static int Main()
        {
            Check(Chunk7StagedGeometry.Band(18.83, 18.84, 22.84) == Chunk7StagedGeometry.BelowBand, "band floor refuses below");
            Check(Chunk7StagedGeometry.Band(18.84, 18.84, 22.84) == null && Chunk7StagedGeometry.Band(22.84, 18.84, 22.84) == null, "band edges are inside");
            Check(Chunk7StagedGeometry.Band(22.85, 18.84, 22.84) == Chunk7StagedGeometry.AboveBand, "band ceiling refuses above");
            Check(Chunk7StagedGeometry.Refusal(false, 0, 7, null) == Chunk7StagedGeometry.NotWalkable, "an unwalkable node refuses first");
            Check(Chunk7StagedGeometry.Refusal(true, .31, 7, null) == Chunk7StagedGeometry.TraceInexact, "an inexact native trace refuses");
            Check(Chunk7StagedGeometry.Refusal(true, .3, .5, null) == Chunk7StagedGeometry.TooShort, "a leg of at most 0.5 m refuses");
            Check(Chunk7StagedGeometry.Refusal(true, .3, .51, Chunk7StagedGeometry.BelowBand) == Chunk7StagedGeometry.BelowBand, "the declared geometry refuses last");
            Check(Chunk7StagedGeometry.Refusal(true, 0, 7, null) == null, "an eligible proposal has no refusal");
            // b202 st exhaustion leg: every term recomputed from the recorded proposals.
            var rows = B202ExhaustLeg.Select(p => new { p, distance = Flat(p.X, p.Z, TargetX, TargetZ),
                refusal = Chunk7StagedGeometry.Refusal(p.Walkable, p.Residual, Flat(p.X, p.Z, StartX, StartZ),
                    p.Walkable ? Chunk7StagedGeometry.ExhaustBand(Flat(p.X, p.Z, TargetX, TargetZ), Envelope) : null) }).ToArray();
            Check(rows.Length == 48, "b202 records 48 proposals (16 angles at 7, 5 and 3.5 m)");
            Check(rows.Count(r => r.refusal == Chunk7StagedGeometry.NotWalkable) == 34, "b202: 34 proposals are not walkable");
            Check(rows.Count(r => r.refusal == Chunk7StagedGeometry.BelowBand) == 14, "b202: all 14 walkable exact-trace proposals are below the band floor");
            Check(rows.All(r => r.refusal != null), "b202: no proposal is eligible");
            var walkableMax = rows.Where(r => r.p.Walkable).Max(r => r.distance);
            Check(walkableMax < Envelope + Chunk7StagedGeometry.ExhaustBandFloor && Math.Abs(walkableMax - 18.186) < .01,
                "b202: the farthest walkable point (18.19 m) is under the floor (18.84 m): the band is unreachable from there");
            Check(rows.Where(r => r.p.Walkable).All(r => r.distance < BeyondEnd), "b202: every walkable point lies inward of the start");
            // The beyond-range placement: the mover ended 19.24 m out, under the reader's envelope + 2.5 m and
            // under the corrected goal band (from the aim, envelope + 3 m, upward).
            Check(BeyondEnd < Envelope + 2.5, "b202: the beyond-range leg ended under the reader's envelope + 2.5 m");
            Check(Chunk7StagedGeometry.BeyondBand(BeyondEnd, Envelope) == Chunk7StagedGeometry.BelowBand, "the beyond-range goal band refuses the b202 end point");
            Check(Chunk7StagedGeometry.BeyondBand(Envelope + 3, Envelope) == null && Chunk7StagedGeometry.BeyondBand(Envelope + 4.49, Envelope) == null &&
                Chunk7StagedGeometry.BeyondBand(Envelope + 4.51, Envelope) == Chunk7StagedGeometry.AboveBand, "the beyond-range goal band is the aim to aim + 1.5 m");
            Check(Chunk7StagedGeometry.ExhaustBand(Envelope + 2.01, Envelope) == null && Chunk7StagedGeometry.ExhaustBand(Envelope + 5.99, Envelope) == null && Chunk7StagedGeometry.ExhaustBand(Envelope + 1.99, Envelope) == Chunk7StagedGeometry.BelowBand &&
                Chunk7StagedGeometry.ExhaustBand(Envelope + 6.01, Envelope) == Chunk7StagedGeometry.AboveBand, "the exhaustion band is unchanged (envelope + 2 m to + 6 m)");
            Console.WriteLine("Staged geometry: " + passes + " PASS / " + failures + " FAIL");
            return failures == 0 ? 0 : 1;
        }
    }
}