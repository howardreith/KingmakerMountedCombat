using System;
using System.IO;
using System.Linq;
using System.Text;
using KingmakerMountedCombat.Diagnostics;

namespace KingmakerMountedCombat.Tests
{
    // Focused regression for the 7E F04 staged leg geometry terms, on the real b202 st exhaustion
    // leg (run 20261024-c7e-b202-st-owner-seed-staged, step 6, leg 0, all 48 recorded proposals), and for
    // the up-front whole-plan selection on the recorded b203 points and synthetic native floors.
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
        // ---- The up-front whole-plan selection (b203 st, run 20261024-c7e-b203-st-owner-seed-staged) ----
        // Every recorded proposal of the b203 run as (leg start x, z, requested x, z, walkable, goal x, y, z,
        // trace residual): steps 0, 2 and 3 (the five legs the native run completed) and the 21 proposals of
        // the refused round-3 move-beyond-range leg. b201 and b202 issued the same first five legs identically.
        private struct Rec
        {
            internal readonly double StartX, StartZ, X, Z, GoalX, GoalY, GoalZ, Residual; internal readonly bool Walkable;
            internal Rec(float sx, float sz, float x, float z, bool walkable, float gx, float gy, float gz, float residual)
            { StartX = sx; StartZ = sz; X = x; Z = z; Walkable = walkable; GoalX = gx; GoalY = gy; GoalZ = gz; Residual = residual; }
        }
        private static Rec R(float sx, float sz, float x, float z, bool walkable, float gx, float gy, float gz, float residual) => new Rec(sx, sz, x, z, walkable, gx, gy, gz, residual);
        private static readonly Rec[] B203 = {
            // The placement the old code used (8 m along +Z): ValidatePlacement found it walkable; its clamped
            // point was not recorded, so the requested point stands for it (mount A's level).
            R(-19.916172f, -13.26273f, -19.916172f, -5.26272964f, true, -19.916172f, 0.3618099f, -5.26272964f, 0f),
            // step 0 move-short leg 0 from (-19.916172, -13.26273)
            R(-19.916172f, -13.26273f, -21.916172f, -13.26273f, true, -21.9161663f, 0.7919998f, -13.2627211f, 0f),
            // step 2 move-short leg 0 from (-21.916151, -13.2627211)
            R(-21.916151f, -13.2627211f, -19.9758644f, -13.7477884f, true, -19.9758587f, 0.7399998f, -13.7515974f, 0f),
            // step 3 move-two-moves leg 0 from (-19.9758434, -13.7515974)
            R(-19.9758434f, -13.7515974f, -26.97567f, -13.7023935f, false, -26.97566f, 0.6850004f, -13.7023878f, 6.99999f),
            R(-19.9758434f, -13.7515974f, -26.4240112f, -11.027421f, false, -26.4240017f, 1.18500042f, -11.0274153f, 6.999994f),
            R(-19.9758434f, -13.7515974f, -26.46167f, -16.3848572f, false, -26.46166f, 0.6850004f, -16.3848515f, 6.999989f),
            R(-19.9758434f, -13.7515974f, -24.8906746f, -8.76717949f, false, -24.8906689f, 1.28499985f, -8.767177f, 6.999997f),
            R(-19.9758434f, -13.7515974f, -24.96026f, -18.6664314f, false, -24.9602566f, 0.292999268f, -18.6664219f, 6.999991f),
            R(-19.9758434f, -13.7515974f, -22.6091022f, -7.26577139f, true, -22.6090984f, 1.53899956f, -7.265773f, 0f),
            R(-19.9758434f, -13.7515974f, -22.70002f, -20.1997643f, true, -22.70001f, 0.522999763f, -20.1997623f, 0f),
            // step 3 move-two-moves leg 1 from (-22.699995, -20.1997623)
            R(-22.699995f, -20.1997623f, -15.8184862f, -21.4822731f, true, -15.8184862f, 0.685000062f, -21.4822731f, 0f),
            R(-22.699995f, -20.1997623f, -16.8331051f, -24.0180874f, true, -16.8331032f, 0.685000062f, -24.0180874f, 0f),
            R(-22.699995f, -20.1997623f, -15.8515148f, -18.75121f, false, -15.8515081f, 0.6850004f, -18.7512f, 7.00000763f),
            R(-22.699995f, -20.1997623f, -18.7409058f, -25.9725952f, false, -18.7408962f, 1.03100014f, -25.97259f, 6.99999952f),
            R(-22.699995f, -20.1997623f, -16.9271622f, -16.2406731f, true, -16.9271622f, 0.685000062f, -16.2406731f, 0f),
            // step 3 move-two-moves leg 2 from (-16.927145, -16.2406712)
            R(-16.927145f, -16.2406712f, -23.6812649f, -18.0796547f, true, -23.6812572f, 0.4260006f, -18.0796452f, 0f),
            // step 5 move-beyond-range leg 0 from (-23.681242, -18.0796452)
            R(-23.681242f, -18.0796452f, -25.508049f, -24.2983932f, false, -25.50804f, 1.18500042f, -24.2983952f, 6.48151541f),
            R(-23.681242f, -18.0796452f, -27.7488022f, -23.12593f, false, -27.7487926f, 1.18500042f, -23.1259251f, 6.48150539f),
            R(-23.681242f, -18.0796452f, -22.98918f, -24.5241089f, true, -22.989172f, 1.17599964f, -24.5241032f, 0.122774825f),
            R(-23.681242f, -18.0796452f, -29.3703079f, -21.1852169f, false, -29.3702984f, 1.14299965f, -21.18522f, 6.481509f),
            R(-23.681242f, -18.0796452f, -20.5756683f, -23.7687111f, true, -20.5756588f, 0.9790001f, -23.7515965f, 0f),
            R(-23.681242f, -18.0796452f, -30.1257057f, -18.7717075f, false, -30.1257f, 0.6850004f, -18.7717075f, 6.48151159f),
            R(-23.681242f, -18.0796452f, -18.6349564f, -22.1472054f, true, -18.6349564f, 0.685000062f, -22.1472054f, 0f),
            R(-23.681242f, -18.0796452f, -26.0717449f, -26.2173119f, false, -26.0717373f, 1.18500042f, -26.21731f, 8.481513f),
            R(-23.681242f, -18.0796452f, -29.00393f, -24.6830635f, false, -29.00392f, 1.18500042f, -24.683054f, 8.48150349f),
            R(-23.681242f, -18.0796452f, -22.775629f, -26.5126762f, false, -22.7756252f, 1.18500042f, -26.5126743f, 8.481516f),
            R(-23.681242f, -18.0796452f, -31.125782f, -22.1435032f, false, -31.1257763f, 1.18500042f, -22.1435032f, 8.481511f),
            R(-23.681242f, -18.0796452f, -19.617384f, -25.5241852f, false, -19.6173763f, 0.9659996f, -25.2515965f, 8.243294f),
            R(-23.681242f, -18.0796452f, -32.1142731f, -18.9852581f, false, -32.11427f, 0.6850004f, -18.9852543f, 8.481514f),
            R(-23.681242f, -18.0796452f, -17.0778236f, -23.4023323f, true, -17.0778236f, 0.685000062f, -23.4023323f, 0f),
            R(-23.681242f, -18.0796452f, -26.6354427f, -28.13623f, false, -26.6354351f, 1.18500042f, -28.1362247f, 10.48151f),
            R(-23.681242f, -18.0796452f, -30.2590561f, -26.2401943f, false, -30.2590466f, 1.18500042f, -26.2401981f, 10.481514f),
            R(-23.681242f, -18.0796452f, -22.56208f, -28.5012417f, false, -22.56207f, 1.18500042f, -28.5012455f, 10.4815216f),
            R(-23.681242f, -18.0796452f, -32.8812561f, -23.1017876f, false, -32.8812523f, 1.18500042f, -23.1017857f, 10.4815111f),
            R(-23.681242f, -18.0796452f, -18.6590977f, -27.27966f, false, -18.6590939f, 1.03100014f, -27.2796574f, 10.4815168f),
            R(-23.681242f, -18.0796452f, -34.1028366f, -19.1988068f, false, -34.1028328f, 0.6850004f, -19.198801f, 10.4815111f),
            R(-23.681242f, -18.0796452f, -15.5206928f, -24.65746f, true, -15.5206823f, 0.6850004f, -24.65745f, 0.406585455f),
        };
        // A navmesh that answers only what b203 recorded: a query within 0.01 m of a recorded request returns
        // the recorded node, its trace from the recorded leg start the recorded residual; anything unrecorded is
        // not walkable (and its trace reaches nothing). Conservative: it can only refuse more than the game.
        private sealed class RecordedNavmesh : IStagedNavmesh
        {
            private readonly Rec[] rows; internal int Unrecorded;
            internal RecordedNavmesh(Rec[] rows) { this.rows = rows; }
            public bool Nearest(StagedPoint requested, out StagedPoint goal)
            {
                foreach (var r in rows)
                    if (Math.Abs(r.X - requested.X) <= .01 && Math.Abs(r.Z - requested.Z) <= .01) { goal = new StagedPoint(r.GoalX, r.GoalY, r.GoalZ); return r.Walkable; }
                Unrecorded++; goal = requested; return false;
            }
            public StagedPoint Trace(StagedPoint from, StagedPoint to)
            {
                foreach (var r in rows)
                    if (Math.Abs(r.GoalX - to.X) <= 1e-4 && Math.Abs(r.GoalZ - to.Z) <= 1e-4 && Math.Abs(r.StartX - from.X) <= .01 && Math.Abs(r.StartZ - from.Z) <= .01)
                    {
                        var length = Chunk7StagedGeometry.Flat(from, to); if (length <= 0) return to;
                        return new StagedPoint(to.X + (from.X - to.X) / length * r.Residual, to.Y, to.Z + (from.Z - to.Z) / length * r.Residual);
                    }
                return from;
            }
        }
        // Synthetic native worlds: walkable inside a predicate, the clamped point is the request, and a straight
        // trace stops at the last walkable point along the segment (sampled every 0.05 m).
        private sealed class WorldNavmesh : IStagedNavmesh
        {
            private readonly Func<double, double, bool> walkable;
            internal WorldNavmesh(Func<double, double, bool> walkable) { this.walkable = walkable; }
            public bool Nearest(StagedPoint requested, out StagedPoint goal) { goal = requested; return walkable(requested.X, requested.Z); }
            public StagedPoint Trace(StagedPoint from, StagedPoint to)
            {
                var length = Chunk7StagedGeometry.Flat(from, to); var steps = (int)Math.Ceiling(length / .05); var last = from;
                for (var i = 1; i <= steps; i++)
                {
                    var t = Math.Min(1, i * .05 / length); var p = new StagedPoint(from.X + (to.X - from.X) * t, from.Y, from.Z + (to.Z - from.Z) * t);
                    if (!walkable(p.X, p.Z)) return last; last = p;
                }
                return to;
            }
        }
        internal static readonly StagedPlanStep[] Steps = {
            new StagedPlanStep("move-short", 0), new StagedPlanStep("cast-standard-scroll", 0), new StagedPlanStep("move-short", 0),
            new StagedPlanStep("move-two-moves", 1), new StagedPlanStep("attack-ranged", 1),
            new StagedPlanStep("move-beyond-range", 2), new StagedPlanStep("move-exhaust", 2), new StagedPlanStep("move-probe", 2),
            new StagedPlanStep("cast-standard-scroll", 2), new StagedPlanStep("attack-ranged-approach", 3) };
        private static readonly StagedPoint MountStart = new StagedPoint(-19.916172f, 0.3618099f, -13.26273f), RiderStart = new StagedPoint(-19.4512444f, 1.7840395f, -13.2158108f);
        private static readonly double Speed = 5.08f, PlanEnvelope = .9f + Chunk7StagedGeometry.PlannedTargetCorpulence + 15.24f;
        private static StagedLeg LegOf(StagedSimulation sim, int step, int leg) => sim.Legs.FirstOrDefault(l => l.Step == step && l.Leg == leg);
        private static bool Near(StagedPoint p, double x, double z, double tolerance = .001) => Math.Abs(p.X - x) <= tolerance && Math.Abs(p.Z - z) <= tolerance;
        // Every simulated plan invariant a feasible selection must hold, recomputed independently of the planner.
        private static void CheckFeasiblePlan(StagedSimulation sim, StagedPoint target, double envelope, string name)
        {
            Check(sim.Feasible, name + ": the selected plan is feasible");
            foreach (var leg in sim.Legs.Where(l => Chunk7StagedGeometry.Moves(l.Kind)))
            {
                Check(leg.Chosen == leg.Proposals.Count - 1 && leg.Proposals.Take(leg.Chosen).All(q => q.Refusal != null), name + ": every leg takes its first eligible proposal");
                var p = leg.Proposals[leg.Chosen]; var d = Chunk7StagedGeometry.Flat(p.Goal, target);
                Check(Chunk7StagedGeometry.Flat(p.Trace, p.Goal) <= Chunk7StagedGeometry.MaximumTraceResidual && p.Walkable, name + ": every chosen goal is walkable with an exact trace");
                if (leg.Kind == "move-beyond-range") Check(d >= envelope + 3 && d <= envelope + 4.5, name + ": the beyond-range goal is in its band");
                if (leg.Kind == "move-exhaust") Check(d >= envelope + 2 && d <= envelope + 6, name + ": every exhaustion goal is in its band");
                if (Chunk7StagedGeometry.Tangential(leg.Kind)) Check(d >= 6.5 && d <= envelope - 1.5, name + ": every in-range leg stays in its band");
                Check(leg.Leg < Chunk7StagedGeometry.MaxLegs, name + ": no step exceeds eight legs");
            }
            var two = sim.Legs.Where(l => l.Kind == "move-two-moves").ToArray(); var exhaust = sim.Legs.Where(l => l.Kind == "move-exhaust").ToArray();
            Check(two.Length >= 1 && two.Last().TimeAfter > Chunk7StagedGeometry.OneMove && two.Take(two.Length - 1).All(l => l.TimeAfter <= Chunk7StagedGeometry.OneMove), name + ": the double move ends at the second native Move");
            Check(exhaust.Length >= 1 && exhaust.Last().TimeAfter >= Chunk7StagedGeometry.ExhaustedMove, name + ": the exhaustion legs spend the native move budget");
            var approach = sim.Legs.Single(l => l.Kind == "attack-ranged-approach");
            Check(Chunk7StagedGeometry.Flat(approach.Start, target) > envelope + .5 && approach.Chosen >= 0, name + ": the approach starts beyond range and reaches range on a straight trace");
        }
        private const double DiscRadius = 12;
        private static void UpFrontSelection(string output)
        {
            // Rotation reproduces Unity's Quaternion.Euler(0, a, 0) on the recorded b203 two-moves leg 0 (start, target, side +1).
            var target = new StagedPoint(-19.916172f, 0.3618099f, -5.26272964f);
            var plan = Chunk7StagedGeometry.Inputs(target, PlanEnvelope, Speed, Steps);
            var cursor = new StagedCursor { Step = 3, Leg = 0, Time = 0, Side = 1, Position = new StagedPoint(-19.9758434f, 0.295891285f, -13.7515974f) };
            var leg0 = Chunk7StagedGeometry.SearchLeg(plan, new WorldNavmesh((x, z) => false), cursor, null);
            Check(Near(leg0.Proposals[0].Requested, -26.97567, -13.7023935) && Near(leg0.Proposals[1].Requested, -26.4240112, -11.027421) &&
                Near(leg0.Proposals[6].Requested, -22.70002, -20.1997643), "the pure rotation reproduces the recorded native requests");
            Check(leg0.Proposals.Count == 16 && leg0.Proposals.All(p => p.Refusal == Chunk7StagedGeometry.NotWalkable), "every leg searches the full circle (16 angles)");

            // b203 replayed on its own recorded points.
            var nav = new RecordedNavmesh(B203);
            var selection = Chunk7StagedGeometry.Select(MountStart, RiderStart, PlanEnvelope, Speed, Steps, nav);
            var first = selection.Candidates[0];
            Check(first.Angle == 0 && first.Distance == 8 && Near(first.Requested, -19.916172, -5.26272964, 1e-5), "b203: candidate 0 is the old fixed placement (8 m along +Z)");
            var sim = first.Simulation;
            Check(sim != null && Near(LegOf(sim, 0, 0).End, -21.9161663, -13.2627211) && Near(LegOf(sim, 2, 0).End, -19.9758587, -13.7515974) &&
                Near(LegOf(sim, 3, 0).End, -22.70001, -20.1997623) && Near(LegOf(sim, 3, 1).End, -16.9271622, -16.2406731) && Near(LegOf(sim, 3, 2).End, -23.6812572, -18.0796452),
                "b203: the simulation reproduces the five native legs the old run completed");
            Check(LegOf(sim, 3, 0).Chosen == 6 && LegOf(sim, 3, 1).Chosen == 4 && LegOf(sim, 3, 2).Chosen == 0 && LegOf(sim, 3, 2).TimeAfter > 3 && LegOf(sim, 3, 1).TimeAfter <= 3,
                "b203: the double move takes the native choices and ends at the second Move after three legs");
            Check(sim.Legs.Single(l => l.Kind == "attack-ranged").Check == null, "b203: the in-range shot is in range (13.36 m)");
            Check(!sim.Feasible && sim.RefusalStep == 5 && sim.RefusalKind == "move-beyond-range" && sim.RefusalLeg == 0 && sim.RefusalTerm == Chunk7StagedGeometry.NoDestination &&
                first.Refusal == Chunk7StagedGeometry.PlanInfeasible, "b203: the old placement is refused up front, at the round-3 beyond-range leg (old: five native steps first)");
            var beyond = LegOf(sim, 5, 0); var recorded = beyond.Proposals.Where(p => B203.Any(r => Math.Abs(r.X - p.Requested.X) <= .01 && Math.Abs(r.Z - p.Requested.Z) <= .01 && r.StartX < -23)).ToArray();
            Check(beyond.Proposals.Count == 48 && recorded.Length == 21, "b203: the beyond-range leg holds all 21 recorded proposals among 48 (full circle, 3 lengths)");
            Check(recorded.Count(p => p.Refusal == Chunk7StagedGeometry.NotWalkable) == 16 && recorded.Count(p => p.Refusal == Chunk7StagedGeometry.BelowBand) == 4 &&
                recorded.Count(p => p.Refusal == Chunk7StagedGeometry.TraceInexact) == 1, "b203: the recorded refusals recompute (16 not walkable, 4 below the band, 1 trace-inexact, as recorded)");
            Check(Math.Abs(beyond.Proposals[0].Length - 6.481517) < 1e-4, "b203: the beyond-range base length is the aim minus the 13.36 m start (6.48 m)");
            Check(selection.Selected == -1 && selection.Candidates.Count == 64 && selection.Candidates.All(c => c.Refusal != null) &&
                selection.Candidates.Skip(1).All(c => c.Refusal == Chunk7StagedGeometry.NotWalkable), "b203: no recorded placement admits the plan; the selection refuses explicitly with all 64 candidates");
            // b202's beyond-range goal (the old band's choice, 19.50 m) is below the aim band the plan applies.
            Check(Chunk7StagedGeometry.Geometry("move-beyond-range", new StagedPoint(-23.681242f, 0, -18.0796452f), new StagedPoint(-22.989172f, 0, -24.5241032f), target, PlanEnvelope) ==
                Chunk7StagedGeometry.BelowBand, "b202: the old beyond-range choice is below the aim band");
            File.WriteAllText(Path.Combine(output, "b203-plan.json"), "{\"plan\":" + Chunk7StagedGeometry.PlanJson(selection, 15.24f, .9f) + "}", new UTF8Encoding(false));

            // A bounded floor (a disc of 12 m around the start): the far side of an 8 m placement holds no band
            // strip for the whole plan, so it refuses; the plan selects the first placement that holds.
            var disc = new WorldNavmesh((x, z) => (x - MountStart.X) * (x - MountStart.X) + (z - MountStart.Z) * (z - MountStart.Z) <= DiscRadius * DiscRadius);
            var bounded = Chunk7StagedGeometry.Select(MountStart, RiderStart, PlanEnvelope, Speed, Steps, disc);
            Check(bounded.Candidates[0].Refusal == Chunk7StagedGeometry.PlanInfeasible && bounded.Candidates[0].Simulation.RefusalStep >= 5,
                "disc: the old fixed placement is refused, in round 3, before any input");
            Check(bounded.Selected > 0 && bounded.Candidates.Take(bounded.Selected).All(c => c.Refusal != null), "disc: the first placement whose whole plan holds is selected");
            if (bounded.Chosen != null) CheckFeasiblePlan(bounded.Chosen.Simulation, bounded.Chosen.Goal, PlanEnvelope, "disc");
            Check(bounded.Chosen != null && bounded.Chosen.Distance == 9, "disc: a 9 m placement is the first that holds");
            Console.WriteLine("disc: selected " + bounded.Selected + " (" + (bounded.Chosen == null ? "none" : bounded.Chosen.Distance + " m, " + bounded.Chosen.Angle + " deg") + "); candidate 0 " + bounded.Candidates[0].Simulation?.Describe());
            File.WriteAllText(Path.Combine(output, "disc-plan.json"), "{\"plan\":" + Chunk7StagedGeometry.PlanJson(bounded, 15.24f, .9f) + "}", new UTF8Encoding(false));

            // Open floor: the old placement holds; the native execution then follows the plan (each leg's first
            // eligible proposal also keeps the remainder eligible).
            var open = new WorldNavmesh((x, z) => true);
            var free = Chunk7StagedGeometry.Select(MountStart, RiderStart, PlanEnvelope, Speed, Steps, open);
            Check(free.Selected == 0, "open floor: the first placement holds");
            CheckFeasiblePlan(free.Chosen.Simulation, free.Chosen.Goal, PlanEnvelope, "open floor");
            var openPlan = Chunk7StagedGeometry.Inputs(free.Chosen.Goal, PlanEnvelope, Speed, Steps);
            var followed = Chunk7StagedGeometry.SearchLeg(openPlan, open, Chunk7StagedGeometry.Start(MountStart), (c, p) => Chunk7StagedGeometry.Continuation(openPlan, open, c, p));
            Check(followed.Chosen == LegOf(free.Chosen.Simulation, 0, 0).Chosen && followed.Proposals[followed.Chosen].Continuation == null, "execution with the continuation term takes the planned first leg");

            // The stop-short margin: an exhaustion goal just inside the floor whose point 0.5 m back along an
            // outward leg falls below it is refused by the margin term, not the band.
            var t0 = new StagedPoint(0, 0, 0);
            Check(Chunk7StagedGeometry.Geometry("move-exhaust", new StagedPoint(0, 0, -16), new StagedPoint(0, 0, -18.9), t0, 16.84) == Chunk7StagedGeometry.StopShortPrefix + Chunk7StagedGeometry.BelowBand,
                "the stop-short margin refuses a goal whose shortfall leaves the exhaustion band");
            Check(Chunk7StagedGeometry.Geometry("move-exhaust", new StagedPoint(0, 0, -22), new StagedPoint(0, 0, -19), t0, 16.84) == null, "an inward-approached goal keeps its margin");
            Check(Chunk7StagedGeometry.Geometry("move-beyond-range", new StagedPoint(0, 0, -13), new StagedPoint(0, 0, -19.84), t0, 16.84) == null,
                "a radial beyond-range goal at the aim keeps the reader floor with its margin (19.34 m)");
            Check(Chunk7StagedGeometry.Geometry("move-beyond-range", new StagedPoint(0, 0, -13), new StagedPoint(0, 0, -19.83), t0, 16.84) == Chunk7StagedGeometry.BelowBand, "the aim band is unchanged");
            // The 8-leg limit: at 100 m/s the double move never reaches its second native Move within eight legs.
            var fast = Chunk7StagedGeometry.Simulate(Chunk7StagedGeometry.Inputs(free.Chosen.Goal, PlanEnvelope, 100, Steps), open, Chunk7StagedGeometry.Start(MountStart));
            Check(!fast.Feasible && fast.RefusalTerm == Chunk7StagedGeometry.LegLimit && fast.RefusalStep == 3 && fast.RefusalLeg == 8, "the 8-leg limit refuses a step that cannot finish");
            // No floor at all: every placement is refused at its own node.
            var none = Chunk7StagedGeometry.Select(MountStart, RiderStart, PlanEnvelope, Speed, Steps, new WorldNavmesh((x, z) => false));
            Check(none.Selected == -1 && none.Candidates.Count == 64 && none.Candidates.All(c => c.Refusal == Chunk7StagedGeometry.NotWalkable && c.Simulation == null), "no floor: an explicit refusal with every candidate recorded");

            // The reader fixture: mount A 8 m from the origin on +X, an open floor except 0.5 m holes at the first
            // eight placement requests, so the ninth (-90 degrees, the origin) is selected; plus its confirmation.
            var fixtureStart = new StagedPoint(8, 0, 0); var fixtureRider = new StagedPoint(8, 1.4, 0);
            var holes = Chunk7StagedGeometry.Fan.Take(8).Select(a => { double ux, uz; Chunk7StagedGeometry.Rotate(0, 1, a, out ux, out uz); return new StagedPoint(8 + ux * 8, 0, uz * 8); }).ToArray();
            var fixtureNav = new WorldNavmesh((x, z) => holes.All(h => (h.X - x) * (h.X - x) + (h.Z - z) * (h.Z - z) > .25));
            var fixture = Chunk7StagedGeometry.Select(fixtureStart, fixtureRider, PlanEnvelope, Speed, Steps, fixtureNav);
            Check(fixture.Selected == 8 && Math.Abs(fixture.Chosen.Goal.X) < 1e-9 && Math.Abs(fixture.Chosen.Goal.Z) < 1e-9, "reader fixture: the ninth placement (the origin) is selected");
            var confirmed = Chunk7StagedGeometry.Inputs(fixture.Chosen.Goal, PlanEnvelope, Speed, Steps);
            var confirmation = Chunk7StagedGeometry.Simulate(confirmed, fixtureNav, Chunk7StagedGeometry.Start(fixtureStart));
            CheckFeasiblePlan(confirmation, fixture.Chosen.Goal, PlanEnvelope, "reader fixture");
            File.WriteAllText(Path.Combine(output, "fixture-plan.json"), "{\"plan\":" + Chunk7StagedGeometry.PlanJson(fixture, 15.24f, .9f) + ",\"confirmation\":" +
                Chunk7StagedGeometry.ConfirmationJson(fixtureStart, confirmed, confirmation) + "}", new UTF8Encoding(false));
        }
        private static int failures, passes;
        private static void Check(bool condition, string name) { if (condition) passes++; else { failures++; Console.WriteLine("FAIL " + name); } }
        private static double Flat(float x1, float z1, float x2, float z2) => Math.Sqrt((x1 - x2) * (x1 - x2) + (z1 - z2) * (z1 - z2));
        private static int Main(string[] args)
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
            UpFrontSelection(args.Length > 0 ? args[0] : Path.GetTempPath());
            Console.WriteLine("Staged geometry: " + passes + " PASS / " + failures + " FAIL");
            return failures == 0 ? 0 : 1;
        }
    }
}