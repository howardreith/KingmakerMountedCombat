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

        // PR #70 R8-F6 (round 10): the installed combat steering of one movement tick at the delta returned to native movement (see
        // MountedAgentSteering): direction RotateTowards(m, Lerp(m, d, 0.5), angular x delta x Deg2Rad, 1) with Unity's RotateTowards (rotate the
        // direction by at most the angle, move the magnitude by at most 1, MoveTowards when either vector is near zero or they are parallel),
        // the angular speed raised on the last segment when the turn would outlast the arrival; shift = new direction x speed x delta.
        internal static Vector2 NativeCombatShift(Vector2 moveDirection, Vector2 toWaypoint, float distance, bool onLastSegment, float speed,
            float combatAngularDegrees, float delta, out Vector2 newDirection)
        {
            var target = new Vector2((moveDirection.x + toWaypoint.x) * 0.5f, (moveDirection.y + toWaypoint.y) * 0.5f);
            var angular = combatAngularDegrees;
            if (onLastSegment)
            {
                var turnTime = Angle(moveDirection, target) / angular;
                var arrivalTime = distance / Math.Max(speed, 0.01f);
                if (turnTime * 2f > arrivalTime) angular *= 2f * turnTime / arrivalTime;
            }
            newDirection = RotateTowards(moveDirection, target, angular * delta * (float)(Math.PI / 180d), 1f);
            return new Vector2(newDirection.x * speed * delta, newDirection.y * speed * delta);
        }

        private static float Length(Vector2 v) => (float)Math.Sqrt(v.x * v.x + v.y * v.y);

        // Installed TickMovement: on the last segment a position this close to the path's end completes the movement (0.05 m).
        internal const float NativeLastSegmentArrivalMetres = 0.05f;

        // Unity Vector3.Angle (degrees; 0 when either vector is near zero).
        internal static float Angle(Vector2 a, Vector2 b)
        {
            var denominator = Math.Sqrt((a.x * a.x + a.y * a.y) * (double)(b.x * b.x + b.y * b.y));
            if (denominator < 1e-15) return 0f;
            var dot = Math.Max(-1d, Math.Min(1d, (a.x * b.x + a.y * b.y) / denominator));
            return (float)(Math.Acos(dot) * 180d / Math.PI);
        }

        private static Vector2 MoveTowards(Vector2 current, Vector2 target, float maxDelta)
        {
            var dx = target.x - current.x; var dy = target.y - current.y; var d = (float)Math.Sqrt(dx * dx + dy * dy);
            if (d <= maxDelta || d < 1e-12f) return target;
            return new Vector2(current.x + dx / d * maxDelta, current.y + dy / d * maxDelta);
        }

        // Unity Vector3.RotateTowards restricted to the horizontal plane.
        internal static Vector2 RotateTowards(Vector2 current, Vector2 target, float maxRadians, float maxMagnitude)
        {
            var currentLength = Length(current); var targetLength = Length(target);
            if (currentLength > 1e-5f && targetLength > 1e-5f)
            {
                var dot = (current.x * target.x + current.y * target.y) / (currentLength * targetLength);
                if (dot > 1f - 1e-6f) return MoveTowards(current, target, maxMagnitude);
                var angle = (float)Math.Acos(Math.Max(-1f, Math.Min(1f, dot)));
                var step = Math.Min(maxRadians, angle);
                var sign = current.x * target.y - current.y * target.x >= 0f ? 1f : -1f;
                var cos = (float)Math.Cos(step * sign); var sin = (float)Math.Sin(step * sign);
                var ux = current.x / currentLength; var uy = current.y / currentLength;
                var magnitude = currentLength + Math.Max(-maxMagnitude, Math.Min(maxMagnitude, targetLength - currentLength));
                return new Vector2((ux * cos - uy * sin) * magnitude, (ux * sin + uy * cos) * magnitude);
            }
            return MoveTowards(current, target, maxMagnitude);
        }

        // PR #70 R8-F6 (round 10): the restricted single tick predicted with the installed steering rule from the agent's live state: its own
        // path and cursor, MoveDirection, speed (MaxSpeed), combat angular speed and waypoint-skip facts. The allowance is a time
        // (allowedMetres / speed); a shift that would pass the waypoint the agent steers to is shortened (the delta returned is smaller), and
        // a waypoint native would skip at that delta is not predicted (refused, conservative). The stop is where this tick leaves the mount
        // (it moves no further under the single-action limit), judged with the production admission under the new direction. On success
        // Route is [origin, stop], RouteMetres the granted time x speed (the adapter returns that delta to native movement).
        internal static Result EvaluateFirstTickNative(UnitEntityData rider, UnitEntityData mount, UnitEntityData target, IList<Vector3> agentPath, int cursor,
            float allowedMetres, float speed, Vector2 moveDirection, float combatAngular, float currentAgentSpeed, bool followPrecomputedPath)
        {
            var result = new Result();
            if (rider?.View == null || mount?.View == null || target?.View == null || agentPath == null) { result.Detail = "unavailable"; return result; }
            var probe = new MountedPairSingleAttack(target, rider, mount, true, false);
            probe.Init(rider);
            result.Plan = PlanIdentity.Of(probe);
            result.PairRadius = probe.PairApproachRadius;
            var origin = mount.Position;
            if (cursor < 0 || cursor >= agentPath.Count) { result.Detail = "agent-cursor-outside-path"; return result; }
            var waypoint = agentPath[cursor];
            var onLast = cursor >= agentPath.Count - 1;
            var toWaypoint = new Vector2(waypoint.x - origin.x, waypoint.z - origin.z);
            var distance = Length(toWaypoint);
            var fullDelta = speed > 0f ? allowedMetres / speed : 0f;
            if (!(fullDelta > 0f) || !(distance > 1e-6f)) { result.Detail = "no-movement(allowed=" + allowedMetres.ToString("R") + ")"; return result; }
            if (followPrecomputedPath && !onLast && distance < currentAgentSpeed * fullDelta)
            {
                result.Detail = "native-waypoint-skip(cursor=" + cursor + ";distance=" + distance.ToString("R") + ";agentSpeed=" + currentAgentSpeed.ToString("R") + ")";
                return result;
            }
            var unit = new Vector2(toWaypoint.x / distance, toWaypoint.y / distance);
            Vector2 newDirection;
            var delta = fullDelta;
            var shift = NativeCombatShift(moveDirection, unit, distance, onLast, speed, combatAngular, delta, out newDirection);
            var stopsAtWaypoint = false;
            if (Length(shift) > distance)
            {
                stopsAtWaypoint = true;
                float low = 0f, high = fullDelta;
                for (var i = 0; i < 40; i++)
                {
                    var mid = (low + high) * 0.5f;
                    Vector2 ignored;
                    if (Length(NativeCombatShift(moveDirection, unit, distance, onLast, speed, combatAngular, mid, out ignored)) > distance) high = mid; else low = mid;
                }
                delta = low;
                shift = NativeCombatShift(moveDirection, unit, distance, onLast, speed, combatAngular, delta, out newDirection);
            }
            var stop = new Vector3(origin.x + shift.x, origin.y, origin.z + shift.y);
            // Installed TickMovement after the move: on the last segment, a waypoint passed (plane test against the previous point) or within
            // 0.05 m completes the movement in turn-based combat by moving the agent onto the path's end (Move(path.Last - position)).
            var previous = cursor > 0 ? agentPath[cursor - 1] : origin;
            var passed = distance <= 0.0001f || (waypoint.x - previous.x) * (waypoint.x - stop.x) + (waypoint.z - previous.z) * (waypoint.z - stop.z) < 0f;
            var snapped = onLast && (passed || GeometryUtils.MechanicsDistance(stop, waypoint) < NativeLastSegmentArrivalMetres);
            if (snapped) stop = new Vector3(waypoint.x, origin.y, waypoint.z);
            var heading = new Vector3(newDirection.x, 0f, newDirection.y);
            var admitted = Length(newDirection) > 1e-6f &&
                probe.EvaluateNativeAdmissionAtMountPosition(stop, heading) == MountedPairNativeAdmissionState.Admitted;
            var turn = Angle(moveDirection, new Vector2((moveDirection.x + unit.x) * 0.5f, (moveDirection.y + unit.y) * 0.5f));
            result.Detail = "first-tick(origin=" + origin + ";waypoint#" + cursor + "=" + waypoint + (onLast ? "(end)" : "(interior)") + ";segment=" +
                distance.ToString("R") + ";delta=" + delta.ToString("R") + ";shift=" + Length(shift).ToString("R") + ";travel=" + GeometryUtils.MechanicsDistance(origin, stop).ToString("R") +
                ";snapped=" + snapped + ";stop=" + stop + ";stopX=" + stop.x.ToString("R") +
                ";stopZ=" + stop.z.ToString("R") + ";turnToTarget=" + turn.ToString("R") + ";turnCapacity=" + (combatAngular * delta).ToString("R") +
                ";direction=(" + newDirection.x.ToString("R") + "," + newDirection.y.ToString("R") + ");" + (stopsAtWaypoint ? (onLast ? "reaches-route-end" : "stops-at-waypoint") : "interior") +
                ";admitted=" + admitted + ")";
            if (!admitted) return result;
            result.Path = PathOutcome.Complete;
            result.Route = new List<Vector3> { origin, stop };
            result.RouteMetres = delta * speed;
            result.ArrivalRadius = ArrivalRadii[ArrivalRadii.Length - 1];
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
            var length = RouteLength(remaining);
            // Round 10 (R8-F1): every reachable stopping position of the remaining route inside the arrival disc (native proximity can finish the
            // carrier there), each with its own segment heading (RouteInsideDiscAdmits, the click's own 1 mm sampling), and the disc under the final
            // heading (DiscAdmits, whose outer ring at the full radius also covers the disc's edge). The in-route samples stop BoundaryMarginMetres
            // inside that edge, below the sampling resolution, so float noise at a centre the click placed exactly at opening + radius cannot
            // refuse the route the click and the revalidation proved.
            var admitted = arrivalRadius > 0f && DiscAdmits(end, arrivalRadius, remaining[remaining.Count - 2], end, admits) &&
                RouteInsideDiscAdmits(remaining, length, end, Math.Max(0f, arrivalRadius - BoundaryMarginMetres), admits);
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
