using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace KingmakerMountedCombat.Diagnostics
{
    // A point in native world coordinates (doubles; native floats convert exactly).
    internal struct StagedPoint
    {
        internal readonly double X, Y, Z;
        internal StagedPoint(double x, double y, double z) { X = x; Y = y; Z = z; }
    }
    // The two native navigation queries every staged proposal uses: the nearest node to a requested point
    // (walkable flag and its clamped position) and the native straight navmesh trace's end point.
    internal interface IStagedNavmesh
    {
        bool Nearest(StagedPoint requested, out StagedPoint goal);
        StagedPoint Trace(StagedPoint from, StagedPoint to);
    }
    internal struct StagedPlanStep
    {
        internal readonly string Kind; internal readonly int Round;
        internal StagedPlanStep(string kind, int round) { Kind = kind; Round = round; }
    }
    internal sealed class StagedPlanInputs
    {
        internal StagedPoint Target; internal double Envelope, Speed; internal StagedPlanStep[] Steps;
    }
    // Where a simulation (or a native execution) stands: the plan step, the leg inside it, the mount's
    // native movement time in the current round, the tangent side of the next leg and the mount position.
    internal sealed class StagedCursor
    {
        internal int Step, Leg, Side = 1; internal double Time; internal StagedPoint Position;
        internal StagedCursor Copy() => (StagedCursor)MemberwiseClone();
    }
    internal sealed class StagedProposal
    {
        internal double Angle, Length; internal StagedPoint Requested, Goal, Trace; internal bool Walkable;
        internal string Refusal; internal bool BaseAccepted; internal string Continuation;
    }
    internal sealed class StagedLeg
    {
        internal int Step, Leg, Side, Chosen = -1; internal string Kind, Check; internal double Time, TimeAfter;
        internal StagedPoint Start, End; internal readonly List<StagedProposal> Proposals = new List<StagedProposal>();
    }
    internal sealed class StagedSimulation
    {
        internal readonly List<StagedLeg> Legs = new List<StagedLeg>();
        internal int RefusalStep = -1, RefusalLeg = -1; internal string RefusalKind, RefusalTerm;
        internal bool Feasible => RefusalTerm == null;
        internal string Describe() => Feasible ? null : "step " + RefusalStep + " " + RefusalKind + " leg " + RefusalLeg + ": " + RefusalTerm;
    }
    internal sealed class StagedCandidate
    {
        internal double Angle, Distance; internal StagedPoint Requested, Goal; internal bool Walkable;
        internal string Refusal; internal StagedSimulation Simulation;
    }
    internal sealed class StagedPlanSelection
    {
        internal StagedPoint MountStart, RiderStart; internal double Envelope, Speed;
        internal readonly List<StagedCandidate> Candidates = new List<StagedCandidate>();
        internal int Selected = -1;
        internal StagedCandidate Chosen => Selected < 0 ? null : Candidates[Selected];
    }

    // Pure geometry of the 7E F04 staged legs, the whole-plan simulation and the up-front target placement.
    // Each proposal records the first term that refused it, so a refused search names its refusing condition.
    // No native or Unity dependency (compiled alone by its focused regression; the scenario supplies the
    // native navigation queries through IStagedNavmesh).
    internal static class Chunk7StagedGeometry
    {
        // Exhaustion legs stay beyond weapon range: envelope + 2 m to envelope + 6 m from the target.
        internal const float ExhaustBandFloor = 2f, ExhaustBandCeiling = 6f;
        // The beyond-range leg aims at envelope + 3 m. Its goal is accepted from that aim upward, not
        // 0.5 m below it: the native ground mover stops short of its goal (b202 st: planned 6.48 m,
        // displaced 6.21 m, ending 19.24 m from the target, under the reader's envelope + 2.5 m).
        internal const float BeyondMargin = 3f, BeyondBandBelow = 0f, BeyondBandAbove = 1.5f;
        internal const float MaximumTraceResidual = .3f, MinimumLegLength = .5f;
        // In-range tangential legs (rounds N and N+1): inside weapon range and outside the target's reach.
        internal const double TangentialFloor = 6.5, TangentialBelowEnvelope = 1.5;
        // The reader's beyond-range floor (envelope + 2.5 m), applied to a goal's stop-short point.
        internal const double BeyondReaderFloor = 2.5;
        // The native mover may stop short of its goal (b201-b203 st: 0.27 m once, otherwise exact); every
        // banded leg's goal must keep its band with this margin back along the leg as well.
        internal const double StopShortMargin = .5;
        // Native TB movement: a 6 s move budget per round; the second Move starts beyond 3 s
        // (UsedTwoMoveAction is MoveAction > 3); exhaustion is read at 5.95 s (frozen 203 TB).
        internal const double MoveBudget = 6, OneMove = 3, ExhaustedMove = 5.95;
        internal const int MaxLegs = 8;
        internal const double ShortLeg = 2, LongLeg = 7;
        internal static readonly double[] ExhaustLengths = { 7, 5, 3.5 };
        // The RiderRanged approach: the start must lie beyond the envelope plus 0.5 m (the scenario's own
        // requirement) with the stop-short margin; a straight exact trace toward the target must reach a
        // walkable point inside the envelope (1, 3 or 5 m inside it).
        internal const double ApproachStartBeyond = .5;
        internal static readonly double[] ApproachInside = { 1, 3, 5 };
        // The forward fan first, then the rest of the circle (as Chunk7CombatScenario.FullCircleFan).
        internal static readonly double[] Fan = { 0, 22.5, -22.5, 45, -45, 67.5, -67.5, 90, -90, 112.5, -112.5, 135, -135, 157.5, -157.5, 180 };
        // Target placement candidates around mount A's start, in this fixed order: the 8 m point along world
        // +Z first (the b201-b203 placement), then the rest of the circle, then 9, 7 and 10 m. A placement is
        // a walkable native point within 0.3 m of the request, at the mount's level (|rise| <= 1 m), inside
        // the bounded rider-relative spawn range (3-20 m, MountedCombatSpatialPolicy).
        internal static readonly double[] PlacementDistances = { 8, 9, 7, 10 };
        internal const double PlacementMaxOffset = .3, PlacementMaxRise = 1, SpawnMinimum = 3, SpawnMaximum = 20;
        // The diagnostic target's native corpulence, recorded 0.7 in b201-b203; the target is spawned only
        // after the plan, so the plan uses this and the scenario refuses before input if the spawned
        // target's envelope differs.
        internal const double PlannedTargetCorpulence = .7;
        internal const string PlanContract = "owner-seed-staged-geometry-plan-v1";

        internal const string NotWalkable = "not-walkable", TraceInexact = "trace-inexact", TooShort = "too-short",
            BelowBand = "below-band", AboveBand = "above-band", NoExhaustContinuation = "no-exhaust-continuation",
            StopShortPrefix = "stop-short-", NoPlanContinuation = "no-plan-continuation", NoDestination = "no-destination",
            LegLimit = "leg-limit", NoDirection = "no-direction", ShotOutOfRange = "shot-out-of-range",
            ApproachStartInRange = "approach-start-in-range", ApproachOutsideRange = "approach-outside-range",
            PlacementOffset = "placement-offset", PlacementRise = "placement-rise", PlacementSpawnRange = "placement-spawn-range",
            PlanInfeasible = "plan-infeasible";

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

        internal static double Flat(StagedPoint a, StagedPoint b) { var dx = a.X - b.X; var dz = a.Z - b.Z; return Math.Sqrt(dx * dx + dz * dz); }
        // Unity's Quaternion.Euler(0, degrees, 0) applied to a horizontal vector.
        internal static void Rotate(double x, double z, double degrees, out double rx, out double rz)
        {
            var r = degrees * Math.PI / 180.0; var c = Math.Cos(r); var s = Math.Sin(r);
            rx = x * c + z * s; rz = -x * s + z * c;
        }
        internal static bool Moves(string kind) => kind.StartsWith("move-", StringComparison.Ordinal);
        internal static bool Tangential(string kind) => kind == "move-short" || kind == "move-two-moves";
        internal static double[] Lengths(string kind, double startDistance, double envelope)
        {
            switch (kind)
            {
                case "move-short": case "move-probe": return new[] { ShortLeg };
                case "move-two-moves": return new[] { LongLeg };
                case "move-exhaust": return ExhaustLengths;
                case "move-beyond-range":
                    var length = Math.Max(.6, envelope + BeyondMargin - startDistance);
                    return new[] { length, length + 2, length + 4 };
                default: throw new InvalidOperationException("Unknown staged movement kind " + kind + ".");
            }
        }
        // The declared band of a leg's goal and of its stop-short point (null: none, the probe).
        internal static string Geometry(string kind, StagedPoint start, StagedPoint goal, StagedPoint target, double envelope)
        {
            var distance = Flat(goal, target); var length = Flat(start, goal);
            var stop = new StagedPoint(goal.X - (goal.X - start.X) / length * StopShortMargin, goal.Y, goal.Z - (goal.Z - start.Z) / length * StopShortMargin);
            var stopDistance = Flat(stop, target); string band, margin;
            switch (kind)
            {
                case "move-short": case "move-two-moves":
                    band = Band(distance, TangentialFloor, envelope - TangentialBelowEnvelope);
                    margin = Band(stopDistance, TangentialFloor, envelope - TangentialBelowEnvelope); break;
                case "move-beyond-range":
                    band = BeyondBand(distance, envelope);
                    margin = Band(stopDistance, envelope + BeyondReaderFloor, envelope + BeyondMargin + BeyondBandAbove); break;
                case "move-exhaust":
                    band = ExhaustBand(distance, envelope); margin = ExhaustBand(stopDistance, envelope); break;
                default: return null;
            }
            return band ?? (margin == null ? null : StopShortPrefix + margin);
        }
        // Whether the step wants another leg after one (the native rules the scenario reads, simulated on
        // the accumulated movement time): the double move until the second Move, exhaustion until 5.95 s.
        internal static bool WantsAnotherLeg(string kind, double time) =>
            kind == "move-two-moves" ? time <= OneMove : kind == "move-exhaust" && time < ExhaustedMove;

        // One leg's ordered proposals from the cursor: every requested length in order, each over the full
        // fan around the leg's base direction (radial for the beyond-range leg, otherwise the tangent on the
        // cursor's side). The first proposal every term admits is chosen; continuation (execution only) is
        // the last term. Every proposal up to the chosen one is recorded.
        internal static StagedLeg SearchLeg(StagedPlanInputs plan, IStagedNavmesh nav, StagedCursor cursor,
            Func<StagedCursor, StagedProposal, string> continuation)
        {
            var kind = plan.Steps[cursor.Step].Kind; var start = cursor.Position;
            var leg = new StagedLeg { Step = cursor.Step, Kind = kind, Leg = cursor.Leg, Side = cursor.Side, Start = start, Time = cursor.Time, End = start, TimeAfter = cursor.Time };
            double rx = start.X - plan.Target.X, rz = start.Z - plan.Target.Z;
            if (rx * rx + rz * rz <= 1e-8) { leg.Check = NoDirection; return leg; }
            double dx = rx, dz = rz;
            if (kind != "move-beyond-range") Rotate(rx, rz, 90.0 * cursor.Side, out dx, out dz);
            var norm = Math.Sqrt(dx * dx + dz * dz); dx /= norm; dz /= norm;
            foreach (var length in Lengths(kind, Flat(start, plan.Target), plan.Envelope))
                foreach (var angle in Fan)
                {
                    double ux, uz; Rotate(dx, dz, angle, out ux, out uz);
                    var p = new StagedProposal { Angle = angle, Length = length, Requested = new StagedPoint(start.X + ux * length, start.Y, start.Z + uz * length) };
                    p.Walkable = nav.Nearest(p.Requested, out p.Goal);
                    p.Trace = p.Walkable ? nav.Trace(start, p.Goal) : start;
                    // The declared geometry and any continuation run only once the cheaper terms passed.
                    p.Refusal = Refusal(p.Walkable, Flat(p.Trace, p.Goal), Flat(start, p.Goal), null);
                    p.BaseAccepted = p.Refusal == null;
                    if (p.Refusal == null) p.Refusal = Geometry(kind, start, p.Goal, plan.Target, plan.Envelope);
                    if (p.Refusal == null && continuation != null) { p.Continuation = continuation(cursor, p); if (p.Continuation != null) p.Refusal = NoPlanContinuation; }
                    leg.Proposals.Add(p);
                    if (p.Refusal == null) { leg.Chosen = leg.Proposals.Count - 1; Advance(plan, cursor.Copy(), leg); return leg; }
                }
            return leg;
        }
        // The cursor after a leg's chosen proposal: movement time grows by the leg at the mount's native speed,
        // capped by the 6 s budget (the mount stops there, part way along); the probe never moves (its native
        // refusal is the observation). The side flips after every leg, as the scenario does.
        internal static StagedCursor Advance(StagedPlanInputs plan, StagedCursor cursor, StagedLeg leg)
        {
            var goal = leg.Proposals[leg.Chosen].Goal; var start = cursor.Position;
            if (leg.Kind != "move-probe")
            {
                var legTime = Flat(start, goal) / plan.Speed;
                if (cursor.Time + legTime > MoveBudget)
                {
                    var fraction = (MoveBudget - cursor.Time) / legTime;
                    cursor.Position = new StagedPoint(start.X + (goal.X - start.X) * fraction, start.Y + (goal.Y - start.Y) * fraction, start.Z + (goal.Z - start.Z) * fraction);
                    cursor.Time = MoveBudget;
                }
                else { cursor.Position = goal; cursor.Time += legTime; }
            }
            cursor.Side = -cursor.Side; cursor.Leg++;
            leg.End = cursor.Position; leg.TimeAfter = cursor.Time;
            return cursor;
        }
        // The cursor after a step that is done: the next step; a new native round starts its movement time at 0.
        internal static void NextStep(StagedPlanInputs plan, StagedCursor cursor)
        {
            var round = plan.Steps[cursor.Step].Round; cursor.Step++; cursor.Leg = 0;
            if (cursor.Step < plan.Steps.Length && plan.Steps[cursor.Step].Round != round) cursor.Time = 0;
        }
        // The plan continuation term of a native leg's proposal: the remainder of the declared plan, simulated
        // from that proposal's goal with the cursor it leaves (null when the whole remainder stays eligible).
        internal static string Continuation(StagedPlanInputs plan, IStagedNavmesh nav, StagedCursor cursor, StagedProposal proposal)
        {
            var leg = new StagedLeg { Kind = plan.Steps[cursor.Step].Kind, Chosen = 0 }; leg.Proposals.Add(proposal);
            var next = Advance(plan, cursor.Copy(), leg);
            if (!WantsAnotherLeg(leg.Kind, next.Time)) NextStep(plan, next);
            else if (next.Leg >= MaxLegs) return "step " + cursor.Step + " " + leg.Kind + " leg " + next.Leg + ": " + LegLimit;
            return Simulate(plan, nav, next).Describe();
        }
        // The whole declared plan from a cursor, greedily: each leg takes its first eligible proposal (the same
        // order the native execution searches). The first leg without one, or a step past its leg limit,
        // refuses with its term. Casts move nothing; shots check their range; the approach checks its trace.
        internal static StagedSimulation Simulate(StagedPlanInputs plan, IStagedNavmesh nav, StagedCursor from)
        {
            var sim = new StagedSimulation(); var cursor = from.Copy();
            while (cursor.Step < plan.Steps.Length)
            {
                var kind = plan.Steps[cursor.Step].Kind;
                if (!Moves(kind))
                {
                    if (kind == "attack-ranged" || kind == "attack-ranged-approach")
                    {
                        var check = kind == "attack-ranged" ? CheckShot(plan, cursor) : Approach(plan, nav, cursor);
                        sim.Legs.Add(check);
                        if (check.Check != null) return Refuse(sim, cursor, kind, check.Check);
                    }
                    NextStep(plan, cursor); continue;
                }
                var leg = SearchLeg(plan, nav, cursor, null); sim.Legs.Add(leg);
                if (leg.Chosen < 0) return Refuse(sim, cursor, kind, leg.Check ?? NoDestination);
                cursor.Position = leg.End; cursor.Time = leg.TimeAfter; cursor.Side = -cursor.Side; cursor.Leg++;
                if (!WantsAnotherLeg(kind, cursor.Time)) NextStep(plan, cursor);
                else if (cursor.Leg >= MaxLegs) return Refuse(sim, cursor, kind, LegLimit);
            }
            return sim;
        }
        private static StagedSimulation Refuse(StagedSimulation sim, StagedCursor cursor, string kind, string term)
        {
            sim.RefusalStep = cursor.Step; sim.RefusalLeg = cursor.Leg; sim.RefusalKind = kind; sim.RefusalTerm = term; return sim;
        }
        private static StagedLeg CheckShot(StagedPlanInputs plan, StagedCursor cursor) => new StagedLeg { Step = cursor.Step, Kind = "attack-ranged", Leg = 0,
            Side = cursor.Side, Start = cursor.Position, End = cursor.Position, Time = cursor.Time, TimeAfter = cursor.Time,
            Check = Flat(cursor.Position, plan.Target) > plan.Envelope ? ShotOutOfRange : null };
        // The approach shot's mount approach, as a straight exact native trace toward the target.
        internal static StagedLeg Approach(StagedPlanInputs plan, IStagedNavmesh nav, StagedCursor cursor)
        {
            var start = cursor.Position; var distance = Flat(start, plan.Target);
            var leg = new StagedLeg { Step = cursor.Step, Kind = "attack-ranged-approach", Leg = 0, Side = cursor.Side, Start = start, End = start, Time = cursor.Time, TimeAfter = cursor.Time };
            if (distance <= plan.Envelope + ApproachStartBeyond + StopShortMargin) { leg.Check = ApproachStartInRange; return leg; }
            double ux = (start.X - plan.Target.X) / distance, uz = (start.Z - plan.Target.Z) / distance;
            foreach (var inside in ApproachInside)
            {
                var along = plan.Envelope - inside;
                var p = new StagedProposal { Angle = 0, Length = inside, Requested = new StagedPoint(plan.Target.X + ux * along, start.Y, plan.Target.Z + uz * along) };
                p.Walkable = nav.Nearest(p.Requested, out p.Goal);
                p.Trace = p.Walkable ? nav.Trace(start, p.Goal) : start;
                p.Refusal = Refusal(p.Walkable, Flat(p.Trace, p.Goal), Flat(start, p.Goal), null);
                p.BaseAccepted = p.Refusal == null;
                if (p.Refusal == null && Flat(p.Goal, plan.Target) > plan.Envelope) p.Refusal = ApproachOutsideRange;
                leg.Proposals.Add(p);
                if (p.Refusal == null) { leg.Chosen = leg.Proposals.Count - 1; leg.End = p.Goal; return leg; }
            }
            leg.Check = NoDestination; return leg;
        }
        internal static StagedPlanInputs Inputs(StagedPoint target, double envelope, double speed, StagedPlanStep[] steps) =>
            new StagedPlanInputs { Target = target, Envelope = envelope, Speed = speed, Steps = steps };
        internal static StagedCursor Start(StagedPoint mountStart) => new StagedCursor { Step = 0, Leg = 0, Time = 0, Side = 1, Position = mountStart };
        // The up-front selection: every placement candidate in the fixed order, each admitted only when the whole
        // declared plan simulates from mount A's start with every leg eligible. The first such candidate is
        // selected; the record keeps every candidate tried and the first refusing leg and term of each.
        internal static StagedPlanSelection Select(StagedPoint mountStart, StagedPoint riderStart, double envelope, double speed,
            StagedPlanStep[] steps, IStagedNavmesh nav)
        {
            var selection = new StagedPlanSelection { MountStart = mountStart, RiderStart = riderStart, Envelope = envelope, Speed = speed };
            foreach (var distance in PlacementDistances)
                foreach (var angle in Fan)
                {
                    double ux, uz; Rotate(0, 1, angle, out ux, out uz);
                    var candidate = new StagedCandidate { Angle = angle, Distance = distance, Requested = new StagedPoint(mountStart.X + ux * distance, mountStart.Y, mountStart.Z + uz * distance) };
                    candidate.Walkable = nav.Nearest(candidate.Requested, out candidate.Goal);
                    var rider = Flat(riderStart, candidate.Goal);
                    candidate.Refusal = !candidate.Walkable ? NotWalkable : Flat(candidate.Goal, candidate.Requested) > PlacementMaxOffset ? PlacementOffset :
                        Math.Abs(candidate.Goal.Y - mountStart.Y) > PlacementMaxRise ? PlacementRise :
                        rider < SpawnMinimum || rider > SpawnMaximum ? PlacementSpawnRange : null;
                    if (candidate.Refusal == null)
                    {
                        candidate.Simulation = Simulate(Inputs(candidate.Goal, envelope, speed, steps), nav, Start(mountStart));
                        if (!candidate.Simulation.Feasible) candidate.Refusal = PlanInfeasible;
                    }
                    selection.Candidates.Add(candidate);
                    if (candidate.Refusal == null) { selection.Selected = selection.Candidates.Count - 1; return selection; }
                }
            return selection;
        }

        // ---- Record (one serializer, used by the scenario and the offline regression alike) ----
        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
        private static string S(string value) => value == null ? "null" : "\"" + value + "\"";
        private static void P(StringBuilder b, StagedPoint p) => b.Append('[').Append(N(p.X)).Append(',').Append(N(p.Y)).Append(',').Append(N(p.Z)).Append(']');
        private static void Array(StringBuilder b, double[] values) { b.Append('['); for (var i = 0; i < values.Length; i++) { if (i > 0) b.Append(','); b.Append(N(values[i])); } b.Append(']'); }
        // A proposal: [angle, length, requested x, y, z, walkable, goal x, y, z, trace x, y, z, refusal].
        private static void Proposal(StringBuilder b, StagedProposal p)
        {
            b.Append('[').Append(N(p.Angle)).Append(',').Append(N(p.Length)).Append(',').Append(N(p.Requested.X)).Append(',').Append(N(p.Requested.Y)).Append(',')
                .Append(N(p.Requested.Z)).Append(',').Append(p.Walkable ? "true" : "false").Append(',').Append(N(p.Goal.X)).Append(',').Append(N(p.Goal.Y)).Append(',')
                .Append(N(p.Goal.Z)).Append(',').Append(N(p.Trace.X)).Append(',').Append(N(p.Trace.Y)).Append(',').Append(N(p.Trace.Z)).Append(',').Append(S(p.Refusal)).Append(']');
        }
        internal static void Leg(StringBuilder b, StagedLeg leg)
        {
            b.Append("{\"step\":").Append(leg.Step).Append(",\"kind\":").Append(S(leg.Kind)).Append(",\"leg\":").Append(leg.Leg).Append(",\"side\":").Append(leg.Side)
                .Append(",\"time\":").Append(N(leg.Time)).Append(",\"start\":"); P(b, leg.Start);
            b.Append(",\"proposals\":[");
            for (var i = 0; i < leg.Proposals.Count; i++) { if (i > 0) b.Append(','); Proposal(b, leg.Proposals[i]); }
            b.Append("],\"chosen\":").Append(leg.Chosen < 0 ? "null" : leg.Chosen.ToString(CultureInfo.InvariantCulture)).Append(",\"check\":").Append(S(leg.Check))
                .Append(",\"end\":"); P(b, leg.End); b.Append(",\"timeAfter\":").Append(N(leg.TimeAfter)).Append('}');
        }
        internal static void Simulation(StringBuilder b, StagedSimulation sim, StagedPoint target)
        {
            b.Append("{\"target\":"); P(b, target); b.Append(",\"legs\":[");
            for (var i = 0; i < sim.Legs.Count; i++) { if (i > 0) b.Append(','); Leg(b, sim.Legs[i]); }
            b.Append("],\"refusal\":");
            if (sim.Feasible) b.Append("null");
            else b.Append("{\"step\":").Append(sim.RefusalStep).Append(",\"kind\":").Append(S(sim.RefusalKind)).Append(",\"leg\":").Append(sim.RefusalLeg)
                .Append(",\"term\":").Append(S(sim.RefusalTerm)).Append('}');
            b.Append('}');
        }
        internal static string PlanJson(StagedPlanSelection selection, double weaponRange, double mountCorpulence)
        {
            var b = new StringBuilder();
            b.Append("{\"contract\":").Append(S(PlanContract)).Append(",\"mountStart\":"); P(b, selection.MountStart);
            b.Append(",\"riderStart\":"); P(b, selection.RiderStart);
            b.Append(",\"weaponRange\":").Append(N(weaponRange)).Append(",\"mountCorpulence\":").Append(N(mountCorpulence))
                .Append(",\"plannedTargetCorpulence\":").Append(N(PlannedTargetCorpulence)).Append(",\"envelope\":").Append(N(selection.Envelope))
                .Append(",\"speed\":").Append(N(selection.Speed)).Append(",\"placementDistances\":"); Array(b, PlacementDistances);
            b.Append(",\"angles\":"); Array(b, Fan);
            b.Append(",\"stopShortMargin\":").Append(N(StopShortMargin)).Append(",\"maxLegs\":").Append(MaxLegs).Append(",\"candidates\":[");
            for (var i = 0; i < selection.Candidates.Count; i++)
            {
                var c = selection.Candidates[i]; if (i > 0) b.Append(',');
                b.Append("{\"angle\":").Append(N(c.Angle)).Append(",\"distance\":").Append(N(c.Distance)).Append(",\"requested\":"); P(b, c.Requested);
                b.Append(",\"walkable\":").Append(c.Walkable ? "true" : "false").Append(",\"goal\":"); P(b, c.Goal);
                b.Append(",\"refusal\":").Append(S(c.Refusal)).Append(",\"simulation\":");
                if (c.Simulation == null) b.Append("null"); else Simulation(b, c.Simulation, c.Goal);
                b.Append('}');
            }
            b.Append("],\"selected\":").Append(selection.Selected < 0 ? "null" : selection.Selected.ToString(CultureInfo.InvariantCulture)).Append('}');
            return b.ToString();
        }
        // The in-combat confirmation: the selected plan simulated again from the actual spawned target, the
        // actual envelope, the mount's in-combat native speed and its position, before any input.
        internal static string ConfirmationJson(StagedPoint mountStart, StagedPlanInputs plan, StagedSimulation sim)
        {
            var b = new StringBuilder();
            b.Append("{\"mountStart\":"); P(b, mountStart); b.Append(",\"target\":"); P(b, plan.Target);
            b.Append(",\"envelope\":").Append(N(plan.Envelope)).Append(",\"speed\":").Append(N(plan.Speed)).Append(",\"simulation\":");
            Simulation(b, sim, plan.Target); b.Append('}');
            return b.ToString();
        }
    }
}
