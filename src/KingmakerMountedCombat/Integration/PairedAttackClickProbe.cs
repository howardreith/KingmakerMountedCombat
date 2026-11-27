using System;
using System.Collections.Generic;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.Utility;
using KingmakerMountedCombat.Domain;
using UnityEngine;

namespace KingmakerMountedCombat.Integration
{
    // MMR-02. The facts KMC needs to prove a dropped turn-based rider melee click legal by its own rule, without changing
    // anything (reviews R1-F3, R3-F2, R4-F1 of PR #64; PR #70 round 1):
    //  - the strike's PRODUCTION admission, evaluated on an unrun MountedPairSingleAttack for the exact rider, mount and target, in
    //    the ORDINARY planning mode the dispatched order uses (PR #70 R1-F9: not a forced single attack), so the first planned
    //    weapon and its reach are the order's; the plan identity (executor, mode, first planned weapon, reach) travels with the
    //    proof and is compared at revalidation and dispatch;
    //  - a native route of the MOUNT's own agent (UnitMovementAgent.FindPath, the call native's path preview makes) whose native
    //    outcome is Complete (PR #70 R1-F5: a Partial or Error result never authorizes movement, whatever its last point admits);
    //  - the bounded walk along that route (MountedRouteWalker: 1 cm arc-length grid, the grant point and the established
    //    position; reviews R5-F2, R6-F2, R7-F1) to an opening where the strike's production admission passes, evaluated with the
    //    rider and eye offsets rotated to the mount's heading on that segment. PR #70 R1-F3: when an opening cannot hold an
    //    executable arrival, discovery continues past it (at most MaxOpenings openings), and the arrival centre is chosen among the
    //    opening's earliest position, the established position, the grant point, its middle and its positions at half-radius
    //    spacing;
    //  - PR #70 R1-F4: when the mount-centred route supplies no usable approach (for example it ends where the HORSE sees the
    //    target but the rider does not), a bounded set of native path queries (MaxLateralQueries) to rider-admissible positions
    //    around the target, each validated under the same outcome, cost, walk and arrival rules;
    //  - the carrier's destination and arrival radius (review R6-F1). Native completes a point command when the executor is within
    //    ApproachRadius of ApproachPoint (UnitCommand.IsUnitEnoughClose). The destination is a point inside the opening, and the
    //    radius is the largest candidate for which the strike's production admission passes at the centre and at 32 sampled points
    //    on rings at the radius and half of it (the heading of the final segment) AND, PR #70 R1-F2, at every route position inside
    //    that radius before the destination, sampled every millimetre with the heading of its own segment: native proximity can
    //    complete the carrier on an earlier segment of a bend that enters the disc, so that segment must already admit the strike.
    //    These are finite samples of the disc, not a proof for every point in it; the strike's own native admission still gates
    //    the attack at the actual position.
    internal static class PairedAttackClickProbe
    {
        // Arrival radii tried, largest first (review R6-F1).
        internal static readonly float[] ArrivalRadii = { 0.05f, 0.025f, 0.0125f, 0.00625f, 0.003125f };
        // A position established at planning is re-examined on a later route when it lies within this of a segment.
        internal const float EstablishedToleranceMetres = 0.01f;
        // Bounded discovery (PR #70 R1-F3, R1-F4).
        internal const int MaxOpenings = 16;
        internal const int MaxLateralQueries = 6;
        internal const int LateralDirections = 16;
        internal const float LateralInsetMetres = 0.1f;
        internal const float LateralArrivalMetres = 0.05f;
        // Route positions inside the arrival disc are sampled at this arc-length spacing (PR #70 R1-F2).
        internal const float InDiscSampleMetres = 0.001f;
        // Kept clear of an opening's measured boundary when placing a disc in it.
        internal const float BoundaryMarginMetres = 0.0002f;
        internal const int MaxCentreSamples = 400;

        internal enum PathOutcome { None, Complete, Partial, Error }

        // PR #70 R1-F9: the identity of the order's planned strike.
        internal struct PlanIdentity
        {
            public bool Valid;
            public UnitEntityData Executor;
            public bool SingleAttack;
            public object Weapon;
            public float Reach;

            internal static PlanIdentity Of(UnitAttack attack)
            {
                var planned = attack == null || attack.AllAttacks.Count == 0 ? null : attack.PlannedAttack;
                return planned == null ? default(PlanIdentity) : new PlanIdentity
                {
                    Valid = true, Executor = attack.Executor, SingleAttack = attack.IsSingleAttack, Weapon = planned.Weapon, Reach = planned.WeaponRange
                };
            }

            internal bool Matches(PlanIdentity other) => Valid && other.Valid && ReferenceEquals(Executor, other.Executor) &&
                SingleAttack == other.SingleAttack && ReferenceEquals(Weapon, other.Weapon) && Math.Abs(Reach - other.Reach) <= 1e-4f;

            public override string ToString() => !Valid ? "<no plan>" : "executor=" + (Executor?.UniqueId ?? "<none>") + ",mode=" +
                (SingleAttack ? "single" : "ordinary") + ",weapon=" + (Weapon == null ? "<none>" : Weapon.GetHashCode().ToString("x")) + ",reach=" + Reach.ToString("R");
        }

        // A carrier proof: the route to the verified destination, the native arrival radius proven around it and the plan it was
        // proven for.
        internal sealed class Approach
        {
            public List<Vector3> Route;
            public float ArrivalRadius;
            public PlanIdentity Plan;
        }

        internal sealed class Result
        {
            public bool AdmissibleNow;
            public float PairRadius = float.NaN;
            public PathOutcome Path = PathOutcome.None;
            // The cost along the proven route to its destination; NaN unless a route is proven.
            public float RouteMetres = float.NaN;
            // Reporting only (PR #70 R1-F1): the unbounded walk's first admissible point; never authorizes anything.
            public float DiagnosticReachMetres = float.NaN;
            public List<Vector3> Route;
            public float ArrivalRadius = float.NaN;
            public PlanIdentity Plan;
            public bool Lateral;
            public int Openings;
            public int LateralQueries;
            public string Detail = "<none>";
            public bool RouteComplete => Path == PathOutcome.Complete;
            // A usable executable route: native Complete, a destination within the grant and a proven arrival radius.
            public bool RouteProven => Path == PathOutcome.Complete && Route != null && Route.Count >= 2 && ArrivalRadius > 0f && !float.IsNaN(RouteMetres);
        }

        // budgetMetres: the mount's remaining grant (no destination beyond it). established: the destination verified when the click
        // was planned, re-examined on this route (review R7-F1). singleAttack: the planning mode of the order (ordinary: false).
        internal static Result Evaluate(UnitEntityData rider, UnitEntityData mount, UnitEntityData target, float budgetMetres, Vector3? established = null,
            bool singleAttack = false)
        {
            var result = new Result();
            if (rider?.View == null || mount?.View == null || target?.View == null) { result.Detail = "unavailable"; return result; }
            var probe = new MountedPairSingleAttack(target, rider, mount, true, singleAttack);
            probe.Init(rider);
            result.Plan = PlanIdentity.Of(probe);
            result.AdmissibleNow = probe.EvaluateCurrentNativeAdmission() == MountedPairNativeAdmissionState.Admitted;
            result.PairRadius = probe.PairApproachRadius;
            if (result.AdmissibleNow) { result.Detail = "in-range"; return result; }
            if (float.IsNaN(result.PairRadius) || result.PairRadius <= 0f) { result.Detail = "no-radius"; return result; }
            var agent = mount.View.AgentASP;
            if (agent == null) { result.Detail = "no-agent"; return result; }
            var budget = Math.Max(0f, budgetMetres);
            Func<Vector3, Vector3, Vector3, bool> admits = (point, from, to) =>
                probe.EvaluateNativeAdmissionAtMountPosition(point, to - from) == MountedPairNativeAdmissionState.Admitted;
            List<Vector3> points;
            var outcome = Query(agent, target.Position, result.PairRadius, true, probe.GetTargetLOSObjectId(), out points);
            result.Path = outcome;
            if (outcome == PathOutcome.None) { result.Detail = "agent-busy"; return result; }
            var why = "route-" + outcome.ToString().ToLowerInvariant();
            if (outcome == PathOutcome.Complete)
            {
                if (TryApproach(points, budget, admits, established, result, out why)) return result;
                result.DiagnosticReachMetres = DiagnosticReach(points, admits);
            }
            // PR #70 R1-F4: the mount-centred route gives no usable approach; bounded native queries to rider-admissible positions.
            foreach (var candidate in LateralCandidates(mount.Position, target.Position, result.PairRadius, admits))
            {
                if (result.LateralQueries >= MaxLateralQueries) break;
                result.LateralQueries++;
                List<Vector3> lateral;
                if (Query(agent, candidate, LateralArrivalMetres, false, 0, out lateral) != PathOutcome.Complete) continue;
                string lateralWhy;
                if (!TryApproach(lateral, budget, admits, established, result, out lateralWhy)) continue;
                result.Path = PathOutcome.Complete;
                result.Lateral = true;
                result.Detail = "lateral-" + result.Detail + ";lateralQueries=" + result.LateralQueries;
                return result;
            }
            result.Path = outcome;
            result.Detail = why + (float.IsNaN(result.DiagnosticReachMetres) ? "" : "(diagnosticReach=" + result.DiagnosticReachMetres.ToString("R") + ")") +
                ";lateralQueries=" + result.LateralQueries;
            return result;
        }

        // One native query of the mount's agent, waited for, classified by its native completion state.
        private static PathOutcome Query(Kingmaker.View.UnitMovementAgent agent, Vector3 destination, float radius, bool needLos, int losId, out List<Vector3> points)
        {
            points = null;
            var path = agent.FindPath(destination, ignored => { }, radius, needLos, losId);
            if (path == null) return PathOutcome.None;
            AstarPath.WaitForPath(path);
            if (path.error || !path.IsDone() || path.CompleteState == global::PathCompleteState.Error) return PathOutcome.Error;
            points = path.vectorPath;
            if (path.CompleteState == global::PathCompleteState.Partial) return PathOutcome.Partial;
            if (path.CompleteState != global::PathCompleteState.Complete || points == null || points.Count == 0) return PathOutcome.Error;
            return PathOutcome.Complete;
        }

        // Candidate rider-admissible positions on a ring just inside pair reach around the target, nearest to the mount first.
        private static IEnumerable<Vector3> LateralCandidates(Vector3 mount, Vector3 target, float pairRadius, Func<Vector3, Vector3, Vector3, bool> admits)
        {
            var ring = pairRadius - LateralInsetMetres;
            if (!(ring > 0f)) yield break;
            var candidates = new List<KeyValuePair<float, Vector3>>();
            for (var k = 0; k < LateralDirections; k++)
            {
                var angle = k * 2d * Math.PI / LateralDirections;
                var point = new Vector3(target.x + ring * (float)Math.Cos(angle), target.y, target.z + ring * (float)Math.Sin(angle));
                if (admits(point, mount, point)) candidates.Add(new KeyValuePair<float, Vector3>(GeometryUtils.MechanicsDistance(mount, point), point));
            }
            candidates.Sort((a, b) => a.Key.CompareTo(b.Key));
            foreach (var candidate in candidates) yield return candidate.Value;
        }

        // The bounded walk over openings on one complete route (PR #70 R1-F3); fills the result on success.
        private static bool TryApproach(IList<Vector3> points, float budget, Func<Vector3, Vector3, Vector3, bool> admits, Vector3? established, Result result, out string why)
        {
            why = "route-never-admits";
            Func<Vector3, Vector3, float> distance = (from, to) => GeometryUtils.MechanicsDistance(from, to);
            Func<Vector3, Vector3, IEnumerable<float>> extra = established.HasValue ? (from, to) => OnSegment(established.Value, from, to) : (Func<Vector3, Vector3, IEnumerable<float>>)null;
            var start = 0f;
            for (var opening = 0; opening < MaxOpenings; opening++)
            {
                List<Vector3> walked;
                float openingStart;
                if (!MountedRouteWalker.Walk(points, distance, Vector3.Lerp, admits, budget, extra, start, out walked, out openingStart))
                {
                    if (opening > 0) why = "no-executable-arrival(openings=" + opening + "): " + why;
                    return false;
                }
                result.Openings = opening + 1;
                var openingEnd = OpeningEnd(points, openingStart, admits);
                List<Vector3> route;
                float destination, radius;
                if (TryChooseArrival(points, openingStart, openingEnd, budget, admits, established, out route, out destination, out radius))
                {
                    result.Route = route;
                    result.RouteMetres = destination;
                    result.ArrivalRadius = radius;
                    result.Detail = "route(opening=" + openingStart.ToString("R") + ".." + openingEnd.ToString("R") + ";openings=" + result.Openings +
                        ";destination=" + destination.ToString("R") + ";radius=" + radius.ToString("R") + ")";
                    return true;
                }
                why = "arrival-unverifiable(opening=" + openingStart.ToString("R") + ".." + openingEnd.ToString("R") + ")";
                start = openingEnd + BoundaryMarginMetres;
            }
            return false;
        }

        // The unbounded walk's first admissible arc length, for the log only.
        private static float DiagnosticReach(IList<Vector3> points, Func<Vector3, Vector3, Vector3, bool> admits)
        {
            List<Vector3> ignored;
            float reach;
            return MountedRouteWalker.Walk(points, (a, b) => GeometryUtils.MechanicsDistance(a, b), Vector3.Lerp, admits, float.PositiveInfinity, out ignored, out reach)
                ? reach : float.NaN;
        }

        // The last admissible arc length of the opening that begins at openingStart (1 cm march, then bisection).
        internal static float OpeningEnd(IList<Vector3> points, float openingStart, Func<Vector3, Vector3, Vector3, bool> admits)
        {
            var length = RouteLength(points);
            var last = openingStart;
            for (var s = openingStart + MountedRouteWalker.SampleStepMetres; ; s += MountedRouteWalker.SampleStepMetres)
            {
                if (s >= length) s = length;
                if (!AdmitsAt(points, s, admits))
                {
                    float low = last, high = s;
                    for (var iteration = 0; iteration < MountedRouteWalker.RefineIterations; iteration++)
                    {
                        var middle = (low + high) * 0.5f;
                        if (AdmitsAt(points, middle, admits)) low = middle; else high = middle;
                    }
                    return low;
                }
                last = s;
                if (s >= length) return length;
            }
        }

        private static bool AdmitsAt(IList<Vector3> points, float s, Func<Vector3, Vector3, Vector3, bool> admits)
        {
            Vector3 from, to;
            List<Vector3> ignored;
            var point = PointAt(points, s, out from, out to, out ignored);
            return admits(point, from, to);
        }

        internal static float RouteLength(IList<Vector3> points)
        {
            var length = 0f;
            for (var index = 1; index < points.Count; index++) length += GeometryUtils.MechanicsDistance(points[index - 1], points[index]);
            return length;
        }

        // The parameter t of a point lying on the segment (within EstablishedToleranceMetres of it), horizontally.
        private static IEnumerable<float> OnSegment(Vector3 point, Vector3 from, Vector3 to)
        {
            var dx = to.x - from.x; var dz = to.z - from.z;
            var lengthSqr = dx * dx + dz * dz;
            if (lengthSqr <= 0f) yield break;
            var t = ((point.x - from.x) * dx + (point.z - from.z) * dz) / lengthSqr;
            if (t <= 0f || t > 1f) yield break;
            var px = from.x + dx * t - point.x; var pz = from.z + dz * t - point.z;
            if (px * px + pz * pz <= EstablishedToleranceMetres * EstablishedToleranceMetres) yield return t;
        }

        // Review R6-F1, PR #70 R1-F2/R1-F3: the destination (inside the opening [openingStart, openingEnd], within the grant) and the
        // largest proven arrival radius.
        internal static bool TryChooseArrival(IList<Vector3> points, float openingStart, float openingEnd, float budget, Func<Vector3, Vector3, Vector3, bool> admits,
            Vector3? established, out List<Vector3> route, out float destinationMetres, out float radius)
        {
            route = null; destinationMetres = float.NaN; radius = float.NaN;
            if (openingStart <= 0f && points.Count > 0)
            {
                route = new List<Vector3> { points[0], points[0] };
                destinationMetres = 0f; radius = ArrivalRadii[0];
                return true;
            }
            var establishedMetres = established.HasValue ? ArcLengthOf(points, established.Value) : float.NaN;
            foreach (var candidate in ArrivalRadii)
            {
                var low = openingStart + candidate + BoundaryMarginMetres;
                var high = Math.Min(openingEnd - candidate - BoundaryMarginMetres, budget);
                if (high < low) continue;
                foreach (var s in Centres(low, high, candidate, budget, establishedMetres))
                {
                    Vector3 from, to;
                    List<Vector3> truncated;
                    var destination = PointAt(points, s, out from, out to, out truncated);
                    if (truncated == null || !DiscAdmits(destination, candidate, from, to, admits) || !RouteInsideDiscAdmits(points, s, destination, candidate, admits)) continue;
                    route = truncated; destinationMetres = s; radius = candidate;
                    return true;
                }
            }
            return false;
        }

        // Arrival centres tried for one radius: the earliest, the established position, the grant point, the middle, then the
        // opening's positions at half-radius spacing (bounded), then the latest.
        private static IEnumerable<float> Centres(float low, float high, float radius, float budget, float established)
        {
            yield return low;
            if (!float.IsNaN(established) && established >= low && established <= high) yield return established;
            if (budget >= low && budget <= high) yield return budget;
            yield return (low + high) * 0.5f;
            var step = Math.Max(radius * 0.5f, InDiscSampleMetres);
            var count = 0;
            for (var s = low + step; s < high && count < MaxCentreSamples; s += step, count++) yield return s;
            yield return high;
        }

        private static float ArcLengthOf(IList<Vector3> points, Vector3 point)
        {
            var travelled = 0f;
            for (var index = 1; index < points.Count; index++)
            {
                var segment = GeometryUtils.MechanicsDistance(points[index - 1], points[index]);
                foreach (var t in OnSegment(point, points[index - 1], points[index])) return travelled + segment * t;
                travelled += segment;
            }
            return float.NaN;
        }

        // Native arrival disc samples: the centre and 16 directions on rings at the radius and half of it, at the segment's heading.
        internal static bool DiscAdmits(Vector3 centre, float radius, Vector3 from, Vector3 to, Func<Vector3, Vector3, Vector3, bool> admits)
        {
            if (!admits(centre, from, to)) return false;
            foreach (var ring in new[] { radius, radius * 0.5f })
                for (var k = 0; k < 16; k++)
                {
                    var angle = k * Math.PI / 8d;
                    var point = new Vector3(centre.x + ring * (float)Math.Cos(angle), centre.y, centre.z + ring * (float)Math.Sin(angle));
                    if (!admits(point, from, to)) return false;
                }
            return true;
        }

        // PR #70 R1-F2: every position of the route before the destination that lies inside the arrival radius (where native
        // proximity could complete the carrier), sampled every InDiscSampleMetres with its own segment's heading, admits the strike.
        internal static bool RouteInsideDiscAdmits(IList<Vector3> points, float destinationMetres, Vector3 centre, float radius,
            Func<Vector3, Vector3, Vector3, bool> admits)
        {
            var travelled = 0f;
            for (var index = 1; index < points.Count && travelled < destinationMetres; index++)
            {
                var from = points[index - 1]; var to = points[index];
                var segment = GeometryUtils.MechanicsDistance(from, to);
                if (segment <= 0f) continue;
                var limit = Math.Min(1f, (destinationMetres - travelled) / segment);
                var dx = to.x - from.x; var dz = to.z - from.z; var fx = from.x - centre.x; var fz = from.z - centre.z;
                var a = dx * dx + dz * dz; var b = 2f * (fx * dx + fz * dz); var c = fx * fx + fz * fz - radius * radius;
                var discriminant = b * b - 4f * a * c;
                if (discriminant >= 0f)
                {
                    var root = (float)Math.Sqrt(discriminant);
                    var t0 = Math.Max(0f, (-b - root) / (2f * a));
                    var t1 = Math.Min(limit, (-b + root) / (2f * a));
                    if (t1 >= t0)
                    {
                        var step = InDiscSampleMetres / segment;
                        for (var t = t0; t < t1; t += step)
                            if (!admits(Vector3.Lerp(from, to, t), from, to)) return false;
                        if (!admits(Vector3.Lerp(from, to, t1), from, to)) return false;
                    }
                }
                travelled += segment;
            }
            return true;
        }

        // The point at arc length s along the route (horizontal mechanics distance), its segment and the route truncated there.
        internal static Vector3 PointAt(IList<Vector3> points, float s, out Vector3 from, out Vector3 to, out List<Vector3> truncated)
        {
            truncated = null; from = to = points[0];
            var walked = new List<Vector3> { points[0] };
            var travelled = 0f;
            for (var index = 1; index < points.Count; index++)
            {
                from = points[index - 1]; to = points[index];
                var segment = GeometryUtils.MechanicsDistance(from, to);
                if (segment <= 0f) continue;
                if (travelled + segment >= s)
                {
                    var point = Vector3.Lerp(from, to, Math.Max(0f, (s - travelled) / segment));
                    walked.Add(point);
                    truncated = walked;
                    return point;
                }
                walked.Add(to);
                travelled += segment;
            }
            return points[points.Count - 1];
        }
    }
}
