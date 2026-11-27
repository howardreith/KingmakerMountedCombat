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
            // PR #70 R6-F4 (round 7): proven under the route-SEARCH ceiling of the restricted single move's one tick; a carrier made from it is
            // executable only through its first-tick decision.
            public bool FirstTick;
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

        // Candidate positions on a ring just inside pair reach around the target. PR #70 R2-F3/R4-F3 (round 5): the rider's admission depends
        // on the heading the pair arrives with, which only the returned path establishes, so no guessed heading EXCLUDES a candidate. A
        // candidate where the strike is already admitted under some sampled heading (in particular a flank whose sight does not depend on the
        // heading) forms one tier; candidates no sampled heading admits (whose legal heading only a returned route can establish) the other;
        // each tier is ordered by distance from the mount. PR #70 R5-F1 (round 6): neither tier may crowd the other out of the bounded query
        // set. The tiers are merged by distance, but each tier is guaranteed half of MaxLateralQueries while the other still has candidates
        // (LateralTierShare): sampled-visible but unpathable endpoints cannot spend every query before a nearby unknown-heading candidate,
        // and nearby blind endpoints cannot spend every query before a heading-independent flank (R4-F3). The MaxLateralQueries cap applies
        // across both tiers, and the walk of each returned path decides with its actual headings (TryApproach).
        internal const int LateralTierShare = MaxLateralQueries / 2;

        // PR #70 R1-F6 (rounds 6-8): the restricted single-action move grants ONE tick, judged from LIVE state (R6-F2): the mount's current
        // position and the movement agent's executable segment, toward the waypoint at the agent's own cursor on the agent's own path (points
        // within a millimetre of the mount are passed, as the mover passes them). The tick carries the mount min(allowance, distance to that
        // waypoint) and never crosses it: the caller grants only that travel (the adapter shortens the tick to it). The strike is then judged with the production admission:
        // already admissible where the pair stands (in range: nothing moves); the route's end reached (the proven arrival disc, DiscAdmits); or
        // an interior stop, admitted under the heading of the segment travelled. carrierPath null: the in-place judgement only. On success the
        // result's Route is [current position, stop] and RouteMetres the travel.
        internal static Result EvaluateFirstTick(UnitEntityData rider, UnitEntityData mount, UnitEntityData target, IList<Vector3> carrierPath, int cursor,
            float arrivalRadius, float allowedMetres)
        {
            var result = new Result();
            if (rider?.View == null || mount?.View == null || target?.View == null) { result.Detail = "unavailable"; return result; }
            var probe = new MountedPairSingleAttack(target, rider, mount, true, false);
            probe.Init(rider);
            result.Plan = PlanIdentity.Of(probe);
            result.PairRadius = probe.PairApproachRadius;
            result.AdmissibleNow = probe.EvaluateCurrentNativeAdmission() == MountedPairNativeAdmissionState.Admitted;
            if (result.AdmissibleNow) { result.Detail = "in-range"; return result; }
            if (carrierPath == null || carrierPath.Count < 1) { result.Detail = "no-carrier-path"; return result; }
            var origin = mount.Position;
            // Round 8 (R6-F2): from the movement agent's own cursor (the waypoint it steers to), never from the path's start.
            var next = Math.Max(0, cursor);
            while (next < carrierPath.Count && GeometryUtils.MechanicsDistance(carrierPath[next], origin) <= WaypointPassedMetres) next++;
            if (next >= carrierPath.Count) { result.Detail = "carrier-path-exhausted(origin=" + origin + ")"; return result; }
            var waypoint = carrierPath[next];
            var finalWaypoint = next == carrierPath.Count - 1;
            var segment = GeometryUtils.MechanicsDistance(origin, waypoint);
            var travel = Math.Min(Math.Max(0f, allowedMetres), segment);
            if (!(travel > 0f)) { result.Detail = "no-movement(allowed=" + allowedMetres.ToString("R") + ")"; return result; }
            var stop = travel >= segment ? waypoint : Vector3.Lerp(origin, waypoint, travel / segment);
            var reachesEnd = finalWaypoint && travel >= segment - 1e-4f;
            Func<Vector3, Vector3, Vector3, bool> admits = (point, from, to) =>
                probe.EvaluateNativeAdmissionAtMountPosition(point, to - from) == MountedPairNativeAdmissionState.Admitted;
            var admitted = reachesEnd ? arrivalRadius > 0f && DiscAdmits(stop, arrivalRadius, origin, waypoint, admits) : admits(stop, origin, waypoint);
            result.Detail = "first-tick(origin=" + origin + ";waypoint#" + next + "=" + waypoint + (finalWaypoint ? "(end)" : "(interior)") + ";segment=" +
                segment.ToString("R") + ";travel=" + travel.ToString("R") + ";stop=" + stop + ";" + (reachesEnd ? "route-end-disc=" + arrivalRadius.ToString("R") :
                travel >= segment ? "stops-at-waypoint" : "interior") + ";admitted=" + admitted + ")";
            if (!admitted) return result;
            result.Path = PathOutcome.Complete;
            result.Route = new List<Vector3> { origin, stop };
            result.RouteMetres = travel;
            result.ArrivalRadius = reachesEnd ? arrivalRadius : ArrivalRadii[ArrivalRadii.Length - 1];
            return result;
        }

        // PR #70 R8-F1 (round 9): the route the agent will execute from its live position (its own path from its cursor) and the strike's
        // admission at its arrival: the proven arrival disc around the path's end with the current target and sight, under the last segment's
        // heading. Route is [current position, remaining points]; RouteMetres its length; RouteProven only when the arrival admits the strike.
        internal static Result EvaluateRemainingRoute(UnitEntityData rider, UnitEntityData mount, UnitEntityData target, IList<Vector3> agentPath, int cursor,
            float arrivalRadius)
        {
            var result = new Result();
            if (rider?.View == null || mount?.View == null || target?.View == null || agentPath == null) { result.Detail = "unavailable"; return result; }
            var probe = new MountedPairSingleAttack(target, rider, mount, true, false);
            probe.Init(rider);
            result.Plan = PlanIdentity.Of(probe);
            result.PairRadius = probe.PairApproachRadius;
            var remaining = new List<Vector3> { mount.Position };
            for (var i = Math.Max(0, cursor); i < agentPath.Count; i++)
                if (GeometryUtils.MechanicsDistance(agentPath[i], remaining[remaining.Count - 1]) > WaypointPassedMetres) remaining.Add(agentPath[i]);
            if (remaining.Count < 2) { result.Detail = "no-remaining-route"; return result; }
            Func<Vector3, Vector3, Vector3, bool> admits = (point, from, to) =>
                probe.EvaluateNativeAdmissionAtMountPosition(point, to - from) == MountedPairNativeAdmissionState.Admitted;
            var end = remaining[remaining.Count - 1];
            var admitted = arrivalRadius > 0f && DiscAdmits(end, arrivalRadius, remaining[remaining.Count - 2], end, admits);
            var length = RouteLength(remaining);
            result.Detail = "remaining-route(cursor=" + cursor + ";routeMetres=" + length.ToString("R") + ";end=" + end + ";arrivalDisc=" + arrivalRadius.ToString("R") +
                ";admitted=" + admitted + ")";
            if (!admitted) return result;
            result.Path = PathOutcome.Complete;
            result.Route = remaining;
            result.RouteMetres = length;
            result.ArrivalRadius = arrivalRadius;
            return result;
        }

        // The native mover passes a path point once it is this close to it (the harness mover's rule as well).
        internal const float WaypointPassedMetres = 0.001f;

        private static IEnumerable<Vector3> LateralCandidates(Vector3 mount, Vector3 target, float pairRadius, Func<Vector3, Vector3, Vector3, bool> admits)
        {
            var ring = pairRadius - LateralInsetMetres;
            if (!(ring > 0f)) yield break;
            var sampled = new List<KeyValuePair<float, Vector3>>();
            var unsampled = new List<KeyValuePair<float, Vector3>>();
            for (var k = 0; k < LateralDirections; k++)
            {
                var angle = k * 2d * Math.PI / LateralDirections;
                var point = new Vector3(target.x + ring * (float)Math.Cos(angle), target.y, target.z + ring * (float)Math.Sin(angle));
                var admitted = false;
                for (var h = 0; h < LateralDirections && !admitted; h++)
                {
                    var heading = h * 2d * Math.PI / LateralDirections;
                    admitted = admits(point, point, new Vector3(point.x + (float)Math.Sin(heading), point.y, point.z + (float)Math.Cos(heading)));
                }
                (admitted ? sampled : unsampled).Add(new KeyValuePair<float, Vector3>(GeometryUtils.MechanicsDistance(mount, point), point));
            }
            sampled.Sort((a, b) => a.Key.CompareTo(b.Key));
            unsampled.Sort((a, b) => a.Key.CompareTo(b.Key));
            int s = 0, u = 0;
            while (s < sampled.Count || u < unsampled.Count)
            {
                bool takeSampled;
                if (s >= sampled.Count) takeSampled = false;
                else if (u >= unsampled.Count) takeSampled = true;
                else if (s >= LateralTierShare && u < LateralTierShare) takeSampled = false;
                else if (u >= LateralTierShare && s < LateralTierShare) takeSampled = true;
                else takeSampled = sampled[s].Key <= unsampled[u].Key;
                yield return takeSampled ? sampled[s++].Value : unsampled[u++].Value;
            }
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
                bool measured;
                var openingEnd = OpeningEnd(points, openingStart, admits, out measured);
                List<Vector3> route;
                float destination, radius;
                if (TryChooseArrival(points, openingStart, openingEnd, budget, admits, established, out route, out destination, out radius))
                {
                    result.Route = route;
                    result.RouteMetres = destination;
                    result.ArrivalRadius = radius;
                    result.Detail = "route(opening=" + openingStart.ToString("R") + ".." + openingEnd.ToString("R") + (measured ? "" : "(route end)") + ";openings=" + result.Openings +
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

        // The last admissible arc length of the opening that begins at openingStart (1 cm march, then bisection). PR #70 R2-F2: measured
        // is false when the route ENDS while the strike is still admitted: the route end is then not the far edge of the legal area, and
        // the arrival disc around a destination near it is judged by its own samples (which reach beyond the route end).
        internal static float OpeningEnd(IList<Vector3> points, float openingStart, Func<Vector3, Vector3, Vector3, bool> admits) =>
            OpeningEnd(points, openingStart, admits, out _);

        internal static float OpeningEnd(IList<Vector3> points, float openingStart, Func<Vector3, Vector3, Vector3, bool> admits, out bool measured)
        {
            measured = true;
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
                if (s >= length) { measured = false; return length; }
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
            // PR #70 R1-F3: every candidate centre inside the opening and within the grant is judged by the disc and route checks
            // themselves; no inward margin pre-excludes a centre (for example a grid point that holds the smallest supported disc).
            var high = Math.Min(openingEnd, budget);
            if (high < openingStart) return false;
            foreach (var candidate in ArrivalRadii)
            {
                foreach (var s in Centres(openingStart, high, candidate, budget, establishedMetres))
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

        // Arrival centres tried for one radius, within [start, high]: the opening's start plus the radius, the established position, the
        // grant point, the 1 cm grid points of the walk, the middle, positions at half-radius spacing (bounded), and the latest.
        private static IEnumerable<float> Centres(float start, float high, float radius, float budget, float established)
        {
            if (start + radius <= high) yield return start + radius;
            if (!float.IsNaN(established) && established >= start && established <= high) yield return established;
            if (budget >= start && budget <= high) yield return budget;
            var count = 0;
            for (var k = (int)Math.Ceiling(start / MountedRouteWalker.SampleStepMetres); k * MountedRouteWalker.SampleStepMetres <= high && count < MaxCentreSamples; k++, count++)
                yield return k * MountedRouteWalker.SampleStepMetres;
            yield return (start + high) * 0.5f;
            var step = Math.Max(radius * 0.5f, InDiscSampleMetres);
            count = 0;
            for (var s = start + step; s < high && count < MaxCentreSamples; s += step, count++) yield return s;
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
        // proximity could complete the carrier) admits the strike. The mount's ACTUAL starting pose (current orientation, no travel) is
        // checked when the start already lies inside the disc, since native completes the carrier there at once. Each segment's part
        // inside the disc is computed stably in double precision (closest point, then the half chord from the perpendicular distance)
        // and sampled every InDiscSampleMetres plus both ends, each with its own segment heading.
        internal static bool RouteInsideDiscAdmits(IList<Vector3> points, float destinationMetres, Vector3 centre, float radius,
            Func<Vector3, Vector3, Vector3, bool> admits)
        {
            if (points.Count == 0) return true;
            if (GeometryUtils.SqrMechanicsDistance(points[0], centre) <= radius * radius && !admits(points[0], points[0], points[0])) return false;
            var travelled = 0d;
            for (var index = 1; index < points.Count && travelled < destinationMetres; index++)
            {
                var from = points[index - 1]; var to = points[index];
                double dx = to.x - from.x, dz = to.z - from.z;
                var length = Math.Sqrt(dx * dx + dz * dz);
                if (length <= 0d) continue;
                var limit = Math.Min(1d, (destinationMetres - travelled) / length);
                double fx = centre.x - from.x, fz = centre.z - from.z;
                var closest = (fx * dx + fz * dz) / (length * length);
                double px = from.x + dx * closest - centre.x, pz = from.z + dz * closest - centre.z;
                var perpendicularSqr = px * px + pz * pz;
                var r2 = (double)radius * radius;
                if (perpendicularSqr <= r2)
                {
                    var half = Math.Sqrt(r2 - perpendicularSqr) / length;
                    var t0 = Math.Max(0d, closest - half);
                    var t1 = Math.Min(limit, closest + half);
                    if (t1 >= t0)
                    {
                        var step = InDiscSampleMetres / length;
                        for (var t = t0; t < t1; t += step)
                            if (!admits(Vector3.Lerp(from, to, (float)t), from, to)) return false;
                        if (!admits(Vector3.Lerp(from, to, (float)t1), from, to)) return false;
                    }
                }
                travelled += length;
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
