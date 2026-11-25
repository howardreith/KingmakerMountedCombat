using System;
using System.Collections.Generic;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Utility;
using KingmakerMountedCombat.Domain;
using UnityEngine;

namespace KingmakerMountedCombat.Integration
{
    // MMR-02 (reviews R1-F3, R3-F2, R4-F1). The facts KMC needs to prove a dropped turn-based rider melee click legal by its own
    // rule, without changing anything:
    //  - the strike's PRODUCTION admission, evaluated on an unrun MountedPairSingleAttack for the exact rider, mount and target
    //    (mount origin, planned weapon range, supported rider offset, native command line of sight); the object never enters
    //    a command container;
    //  - when movement is needed, a native route of the MOUNT's own agent (UnitMovementAgent.FindPath, the call native's path
    //    preview makes), walked from the mount until the first point at which that same production admission passes
    //    (MountedPairSingleAttack.EvaluateNativeAdmissionAtMountPosition). The route is truncated there and its cost measured
    //    along it. The admitted order's carrier then FOLLOWS exactly this route (UnitCommand.ForcedPath), so admission and
    //    execution agree on the path, the stopping point, its line of sight and the rider offset. Reviews R5-F2, R6-F2: the
    //    walk is bounded by the mount's grant and each point is evaluated with the rider and eye offsets rotated to the mount's
    //    heading on that segment. Review R7-F1: the walk's discovery limits are those of MountedRouteWalker (1 cm grid from the
    //    route start, the grant point and the established position); a successful walk's measured cost and endpoint are used.
    //  - Review R6-F1: the carrier's destination and arrival radius. Native completes a point command when the executor is
    //    within ApproachRadius of ApproachPoint by GeometryUtils.SqrMechanicsDistance (UnitCommand.IsUnitEnoughClose), wherever
    //    it is in that horizontal disc. The destination is therefore a point INSIDE the admissible opening, and the radius the
    //    largest candidate for which the strike's production admission passes at the centre and on rings at the radius and half
    //    of it (32 points, the heading of the segment there): every position native can accept as arrival is a position from
    //    which the strike is admitted, also near a corner. The route is truncated at that destination, within the grant.
    internal static class PairedAttackClickProbe
    {
        // Arrival radii tried, largest first (review R6-F1); the destination is searched up to this far into the opening.
        internal static readonly float[] ArrivalRadii = { 0.05f, 0.025f, 0.0125f, 0.00625f, 0.003125f };
        internal const float ArrivalSearchMetres = 0.3f;
        // A position established at planning is re-examined on a later route when it lies within this of a segment.
        internal const float EstablishedToleranceMetres = 0.01f;

        // A carrier proof: the route to the verified destination and the native arrival radius proven around it.
        internal sealed class Approach
        {
            public List<Vector3> Route;
            public float ArrivalRadius;
        }

        internal sealed class Result
        {
            public bool AdmissibleNow;
            public float PairRadius = float.NaN;
            public bool RouteComplete;
            public float RouteMetres = float.NaN;
            public bool EndpointAdmitsStrike;
            public List<Vector3> Route;
            public float ArrivalRadius = float.NaN;
            public string Detail = "<none>";
        }

        // budgetMetres: the mount's remaining grant (no endpoint beyond it). established: the destination verified when the click
        // was planned, re-examined on this route (review R7-F1).
        internal static Result Evaluate(UnitEntityData rider, UnitEntityData mount, UnitEntityData target, float budgetMetres, Vector3? established = null)
        {
            var result = new Result();
            if (rider?.View == null || mount?.View == null || target?.View == null) { result.Detail = "unavailable"; return result; }
            var probe = new MountedPairSingleAttack(target, rider, mount, true);
            probe.Init(rider);
            result.AdmissibleNow = probe.EvaluateCurrentNativeAdmission() == MountedPairNativeAdmissionState.Admitted;
            result.PairRadius = probe.PairApproachRadius;
            if (result.AdmissibleNow) { result.Detail = "in-range"; return result; }
            if (float.IsNaN(result.PairRadius) || result.PairRadius <= 0f) { result.Detail = "no-radius"; return result; }
            var agent = mount.View.AgentASP;
            if (agent == null) { result.Detail = "no-agent"; return result; }
            var path = agent.FindPath(target.Position, ignored => { }, result.PairRadius, true, probe.GetTargetLOSObjectId());
            if (path == null) { result.Detail = "agent-busy"; return result; }
            AstarPath.WaitForPath(path);
            var points = path.vectorPath;
            if (path.error || !path.IsDone() || points == null || points.Count == 0) { result.Detail = "route-failed"; return result; }
            result.RouteComplete = true;
            var budget = Math.Max(0f, budgetMetres);
            List<Vector3> route;
            float cost;
            Func<Vector3, Vector3, Vector3, bool> admits = (point, from, to) =>
                probe.EvaluateNativeAdmissionAtMountPosition(point, to - from) == MountedPairNativeAdmissionState.Admitted;
            Func<Vector3, Vector3, float> distance = (from, to) => GeometryUtils.MechanicsDistance(from, to);
            Func<Vector3, Vector3, IEnumerable<float>> extra = established.HasValue ? (from, to) => OnSegment(established.Value, from, to) : (Func<Vector3, Vector3, IEnumerable<float>>)null;
            var found = MountedRouteWalker.Walk(points, distance, Vector3.Lerp, admits, budget, extra, out route, out cost);
            if (!found)
            {
                // The unbounded walk (diagnostic, and review R7-F1: any successful walk's measured cost is used as measured).
                List<Vector3> unbounded;
                float reach;
                if (MountedRouteWalker.Walk(points, distance, Vector3.Lerp, admits, float.PositiveInfinity, out unbounded, out reach))
                {
                    if (reach <= budget) { found = true; route = unbounded; cost = reach; }
                    else
                    {
                        result.RouteMetres = reach;
                        result.EndpointAdmitsStrike = true;
                        result.Detail = "beyond-grant(reach=" + reach.ToString("R") + ")";
                        return result;
                    }
                }
                else
                {
                    result.RouteMetres = cost;
                    result.Detail = "route-never-admits";
                    return result;
                }
            }
            result.EndpointAdmitsStrike = true;
            float arrival, destinationMetres;
            List<Vector3> toDestination;
            if (!TryChooseArrival(points, cost, budget, admits, out toDestination, out destinationMetres, out arrival))
            {
                // The opening is too narrow for any proven arrival disc within the grant: refused (no unverified carrier).
                result.RouteMetres = float.PositiveInfinity;
                result.Detail = "arrival-unverifiable(opening=" + cost.ToString("R") + ")";
                return result;
            }
            result.Route = toDestination;
            result.RouteMetres = destinationMetres;
            result.ArrivalRadius = arrival;
            result.Detail = "route(opening=" + cost.ToString("R") + ";radius=" + arrival.ToString("R") + ")";
            return result;
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

        // Review R6-F1: the destination (a point inside the opening, within the grant) and the largest proven arrival radius.
        internal static bool TryChooseArrival(IList<Vector3> points, float openingMetres, float budget, Func<Vector3, Vector3, Vector3, bool> admits,
            out List<Vector3> route, out float destinationMetres, out float radius)
        {
            route = null; destinationMetres = float.NaN; radius = float.NaN;
            if (openingMetres <= 0f)
            {
                route = new List<Vector3> { points[0] };
                destinationMetres = 0f; radius = ArrivalRadii[0];
                return true;
            }
            foreach (var candidate in ArrivalRadii)
            {
                var step = Math.Max(candidate * 0.5f, 0.002f);
                for (var s = openingMetres + candidate + 0.001f; s <= Math.Min(budget, openingMetres + ArrivalSearchMetres) + 1e-6f; s += step)
                {
                    Vector3 from, to;
                    List<Vector3> truncated;
                    var destination = PointAt(points, s, out from, out to, out truncated);
                    if (truncated == null || !DiscAdmits(destination, candidate, from, to, admits)) continue;
                    route = truncated; destinationMetres = s; radius = candidate;
                    return true;
                }
            }
            return false;
        }

        // Native arrival disc check: the centre and 16 directions on rings at the radius and half of it, at the segment's heading.
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

        // The point at arc length s along the route (horizontal mechanics distance), its segment and the route truncated there.
        private static Vector3 PointAt(IList<Vector3> points, float s, out Vector3 from, out Vector3 to, out List<Vector3> truncated)
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
                    var point = Vector3.Lerp(from, to, (s - travelled) / segment);
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
