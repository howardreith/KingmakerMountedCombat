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
            // PR #70 R8-F1 (round 11): the untruncated native route the destination was chosen on and the destination's arc length on it. The
            // first-debit revalidation evaluates the in-disc route on these same points (bit-identical samples to the click and revalidation),
            // so the full arrival radius is checked without float noise from a re-parameterized route.
            public List<Vector3> NativeRoute;
            public float DestinationMetres = float.NaN;
        }

        internal sealed class Result
        {
            public bool AdmissibleNow;
            public float PairRadius = float.NaN;
            public PathOutcome Path = PathOutcome.None;
            // The cost along the proven route to its destination; NaN unless a route is proven.
            public float RouteMetres = float.NaN;
            // Round 17 (R16-F4): the approved duration of a first-tick result (the candidate itself, never reconstructed from metres).
            public float Duration = float.NaN;
            // Reporting only (PR #70 R1-F1): the unbounded walk's first admissible point; never authorizes anything.
            public float DiagnosticReachMetres = float.NaN;
            public List<Vector3> Route;
            public float ArrivalRadius = float.NaN;
            public PlanIdentity Plan;
            public bool Lateral;
            public int Openings;
            public int LateralQueries;
            // PR #70 R8-F1 (round 11): the untruncated native route of the proven destination and its arc length there (see Approach).
            public List<Vector3> NativeRoute;
            public float DestinationMetres = float.NaN;
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
            float ignored;
            return NativeCombatShift(moveDirection, toWaypoint, distance, onLastSegment, speed, combatAngularDegrees, delta, out newDirection, out ignored);
        }

        // effectiveAngularDegrees: the angular speed this tick turns with (the last-segment raise included; PR #70 R10-F5, round 11).
        internal static Vector2 NativeCombatShift(Vector2 moveDirection, Vector2 toWaypoint, float distance, bool onLastSegment, float speed,
            float combatAngularDegrees, float delta, out Vector2 newDirection, out float effectiveAngularDegrees)
        {
            var target = new Vector2((moveDirection.x + toWaypoint.x) * 0.5f, (moveDirection.y + toWaypoint.y) * 0.5f);
            var angular = combatAngularDegrees;
            if (onLastSegment)
            {
                var turnTime = Angle(moveDirection, target) / angular;
                var arrivalTime = distance / Math.Max(speed, 0.01f);
                if (turnTime * 2f > arrivalTime) angular *= 2f * turnTime / arrivalTime;
            }
            effectiveAngularDegrees = angular;
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

        // PR #70 round 16 (R13-F5, R15-F1, R15-F2): ONE installed TickMovement tick of the agent's own path at a given duration, the single
        // production simulation shared by the first-tick decision's candidates, its interior-stop search and the step-executable duration. From
        // the agent's cursor native skips a (follow-precomputed, not last) waypoint while its distance from the mount is below m_Speed
        // (agentSpeed) x THIS duration, then shifts by the installed combat steering at MaxSpeed (speed) x the duration (NativeCombatShift: the
        // agent speed decides skipping, MaxSpeed the shift). Only when the selected waypoint is the path's last does the endpoint completion
        // apply (its plane passed against the previous point, or within NativeLastSegmentArrivalMetres), which moves the mount ONTO the path's
        // end. Stop is where the tick leaves the mount.
        internal struct NativeTick
        {
            public bool Moving;
            public int Selected;
            public bool OnLast;
            public float Distance;
            public Vector2 Unit, Shift, Direction;
            public float EffectiveAngular;
            public bool Passed, Snapped;
            public Vector3 Stop;
            // The shift passes the selected waypoint (a candidate of the waypoint resolution, not an interior stop).
            public bool PassesWaypoint => Length(Shift) > Distance;
            public override string ToString() => "waypoint#" + Selected + (OnLast ? "(end)" : "(interior)") + ",moving=" + Moving + ",shift=" + Length(Shift).ToString("R") +
                ",snapped=" + Snapped + ",stop=(" + Stop.x.ToString("R") + "," + Stop.z.ToString("R") + ")";
        }

        internal static NativeTick SimulateTick(IList<Vector3> path, int cursor, Vector3 origin, Vector2 moveDirection, float speed, float agentSpeed,
            float combatAngular, float delta, bool followPrecomputedPath)
        {
            var tick = new NativeTick { Selected = cursor, Stop = origin, OnLast = path != null && cursor >= path.Count - 1 };
            if (path == null || cursor < 0 || cursor >= path.Count || !(delta > 0f)) return tick;
            var index = cursor;
            var distance = PlanarDistance(path[index], origin);
            while (followPrecomputedPath && index < path.Count - 1 && distance < agentSpeed * delta) { index++; distance = PlanarDistance(path[index], origin); }
            var waypoint = path[index];
            tick.Selected = index;
            tick.OnLast = index >= path.Count - 1;
            tick.Distance = distance;
            if (!(distance > 1e-6f)) return tick;
            tick.Unit = new Vector2((waypoint.x - origin.x) / distance, (waypoint.z - origin.z) / distance);
            tick.Shift = NativeCombatShift(moveDirection, tick.Unit, distance, tick.OnLast, speed, combatAngular, delta, out tick.Direction, out tick.EffectiveAngular);
            tick.Moving = true;
            var stop = new Vector3(origin.x + tick.Shift.x, origin.y, origin.z + tick.Shift.y);
            var previous = index > 0 ? path[index - 1] : origin;
            tick.Passed = distance <= 0.0001f || (waypoint.x - previous.x) * (waypoint.x - stop.x) + (waypoint.z - previous.z) * (waypoint.z - stop.z) < 0f;
            tick.Snapped = tick.OnLast && (tick.Passed || GeometryUtils.MechanicsDistance(stop, waypoint) < NativeLastSegmentArrivalMetres);
            tick.Stop = tick.Snapped ? new Vector3(waypoint.x, origin.y, waypoint.z) : stop;
            return tick;
        }

        private static float PlanarDistance(Vector3 point, Vector3 origin) => Length(new Vector2(point.x - origin.x, point.z - origin.z));

        // Round 16: a step tick's settled physical travel is the straight displacement SettleStepTravel measures from the tick's start to where
        // installed TickMovement leaves the mount (endpoint completion included); it must stay within the physical step remainder. A completion
        // (snap) is executable only when the path's end lies within the remainder; an unsnapped stop on the selected last segment keeps
        // StepMarginMetres from both completion triggers (the 0.05 m disc and the final plane), so a rounding difference cannot turn it into an
        // unaffordable completion. An unsnapped shift is bounded by the allocation's physical reservation (speed x duration). physicalLeft
        // +Infinity (not a step): always executable.
        internal const float StepMarginMetres = 0.0001f;
        internal static bool StepSettlementAdmits(NativeTick tick, IList<Vector3> path, Vector3 origin, float physicalLeft)
        {
            if (float.IsPositiveInfinity(physicalLeft) || !tick.Moving) return true;
            if (tick.Snapped) return GeometryUtils.MechanicsDistance(origin, tick.Stop) <= physicalLeft;
            if (!tick.OnLast || GeometryUtils.MechanicsDistance(origin, path[path.Count - 1]) <= physicalLeft) return true;
            var waypoint = path[tick.Selected];
            var previous = tick.Selected > 0 ? path[tick.Selected - 1] : origin;
            double sx = waypoint.x - previous.x, sz = waypoint.z - previous.z, length = Math.Sqrt(sx * sx + sz * sz);
            var plane = length > 1e-6d ? ((waypoint.x - tick.Stop.x) * sx + (waypoint.z - tick.Stop.z) * sz) / length : double.MaxValue;
            return GeometryUtils.MechanicsDistance(tick.Stop, waypoint) >= NativeLastSegmentArrivalMetres + StepMarginMetres && plane >= StepMarginMetres;
        }

        // Round 16 (R13-F5, R15-F1, R15-F2): the longest duration within the permitted one whose installed tick (SimulateTick: the waypoint
        // native selects at THAT duration, its steering and its completion) settles within the physical step remainder. The permitted duration
        // itself when it settles within it; otherwise a downward scan at the established resolution (InDiscSampleMetres of shift, at least
        // InteriorStopCandidatesPerTick candidates), the boundary refined by bisection and backed off by StepMarginMetres of shift (re-checked);
        // 0 when no positive duration settles within the remainder. Selected is the tick at the returned duration.
        // Round 20 (R19-F1): EVERY positive return (the permitted duration, the scan back-off, the verified scan sample and the final bisection's
        // result) is Executable: its settled stop, the float-representable position SimulateTick computes (origin + shift, rounded as the world
        // position will be), displaces the mount by at least StepMinimumDisplacementMetres and by no more than the physical remainder, and keeps
        // the completion margin. Far from the origin a remainder smaller than the spacing of adjacent positions along the heading can round the
        // stop back onto the origin (no movement while the native counter advances) or forward beyond the remainder; such a duration is not
        // returned. A scan sample that settles but is not executable falls through to the classified bisection below; zero when no representable
        // legal move is verified (native's denial ends the step promptly). Not a step (physicalLeft +Infinity): the permitted duration, unchanged.
        internal static float StepExecutableDuration(IList<Vector3> path, int cursor, Vector3 origin, Vector2 moveDirection, float speed, float agentSpeed,
            float combatAngular, bool followPrecomputedPath, float physicalLeft, float permitted, out NativeTick selected, out int evaluated)
        {
            evaluated = 1;
            selected = SimulateTick(path, cursor, origin, moveDirection, speed, agentSpeed, combatAngular, permitted, followPrecomputedPath);
            if (!(permitted > 0f) || !(speed > 0f)) return Math.Max(0f, permitted);
            if (float.IsPositiveInfinity(physicalLeft) || Executable(selected, path, origin, physicalLeft)) return permitted;
            var step = Math.Max(InDiscSampleMetres / speed, permitted / InteriorStopCandidatesPerTick);
            var bad = permitted;
            var t = permitted;
            while (true)
            {
                t -= step;
                if (!(t > 0f)) break;
                evaluated++;
                if (!StepSettlementAdmits(SimulateTick(path, cursor, origin, moveDirection, speed, agentSpeed, combatAngular, t, followPrecomputedPath), path, origin, physicalLeft))
                { bad = t; continue; }
                float low = t, high = bad;
                for (var i = 0; i < 40; i++)
                {
                    var mid = (low + high) * 0.5f;
                    evaluated++;
                    if (StepSettlementAdmits(SimulateTick(path, cursor, origin, moveDirection, speed, agentSpeed, combatAngular, mid, followPrecomputedPath), path, origin, physicalLeft))
                        low = mid;
                    else high = mid;
                }
                // Round 18 (R16-F3): the margin back-off stays inside the verified interval [t, low]; when it would leave it, the interval's
                // midpoint is taken instead, and the verified sample t itself when that re-check fails (never zero while a verified positive
                // duration exists). Round 20 (R19-F1): both are returned only when Executable (settled representable displacement within
                // [floor, remainder], completion margin kept); otherwise the interval below the smallest unsettled sample is resolved by the
                // classified bisection.
                var backed = InsideVerified(low - StepMarginMetres / speed, t, low);
                evaluated++;
                var candidate = SimulateTick(path, cursor, origin, moveDirection, speed, agentSpeed, combatAngular, backed, followPrecomputedPath);
                if (Executable(candidate, path, origin, physicalLeft)) { selected = candidate; return backed; }
                evaluated++;
                candidate = SimulateTick(path, cursor, origin, moveDirection, speed, agentSpeed, combatAngular, t, followPrecomputedPath);
                if (Executable(candidate, path, origin, physicalLeft)) { selected = candidate; return t; }
                bad = high;
                break;
            }
            // Round 17 (R16-F3): the interval between zero and the last bad sample (below one scan step) is resolved before returning zero.
            // Round 19 (R18-F1): each bisection sample is classified by the existing evaluator (ClassifyStep) instead of one executable/not
            // result: a sample that does not settle within the remainder (overspends, or ends within StepMarginMetres of a completion trigger)
            // is too LONG and lowers the upper bound; a sample that settles but displaces the mount less than StepMinimumDisplacementMetres (or
            // does not move) is too SHORT and raises the lower bound; an executable sample is recorded as verified and raises the lower bound
            // toward the settlement limit. The installed tick is path dependent (waypoint selection, steering, completion), so the predicate is
            // not assumed monotonic: a too-short sample ABOVE a verified executable one contradicts the bracket and is treated conservatively as
            // an upper bound (the search never extends beyond it). Same 40 samples, same 0.01 mm floor and 0.1 mm margin, no new tolerance.
            float lowest = 0f, highest = bad, firstVerified = 0f, lastVerified = 0f;
            for (var i = 0; i < 40; i++)
            {
                var mid = (lowest + highest) * 0.5f;
                evaluated++;
                var sample = mid > 0f
                    ? ClassifyStep(SimulateTick(path, cursor, origin, moveDirection, speed, agentSpeed, combatAngular, mid, followPrecomputedPath), path, origin, physicalLeft)
                    : StepSample.TooLong;
                if (sample == StepSample.Executable)
                {
                    lowest = mid; lastVerified = mid;
                    if (!(firstVerified > 0f)) firstVerified = mid;
                }
                else if (sample == StepSample.TooShort && !(lastVerified > 0f)) lowest = mid;
                else highest = mid;
            }
            // Round 18 (R16-F3), round 19 (R18-F1): the back-off stays inside the verified interval [firstVerified, lastVerified] (both
            // endpoints executable samples; with no assumed monotonicity, zero is no longer a verified lower end): its midpoint when the margin
            // would leave it. The selected duration is re-checked through the current-tick simulation and settlement validation (Executable)
            // before it is returned: the back-off, else the verified lastVerified itself. Zero only when no positive, displacing duration was
            // verified: native's denial branch then ends the step promptly.
            if (lastVerified > 0f)
            {
                var lowBacked = InsideVerified(lastVerified - StepMarginMetres / speed, firstVerified, lastVerified);
                evaluated++;
                var candidate = SimulateTick(path, cursor, origin, moveDirection, speed, agentSpeed, combatAngular, lowBacked, followPrecomputedPath);
                if (Executable(candidate, path, origin, physicalLeft)) { selected = candidate; return lowBacked; }
                evaluated++;
                candidate = SimulateTick(path, cursor, origin, moveDirection, speed, agentSpeed, combatAngular, lastVerified, followPrecomputedPath);
                if (Executable(candidate, path, origin, physicalLeft)) { selected = candidate; return lastVerified; }
            }
            selected = SimulateTick(path, cursor, origin, moveDirection, speed, agentSpeed, combatAngular, 0f, followPrecomputedPath);
            return 0f;
        }

        // Round 18 (R16-F3): a backed-off duration kept strictly inside the verified interval (lower, upper]: the back-off itself when it stays
        // above lower, otherwise the interval's midpoint (positive whenever upper is).
        private static float InsideVerified(float backed, float lower, float upper) => backed > lower && backed > 0f ? backed : (lower + upper) * 0.5f;

        // Round 18: a duration of the interval above zero is executable when it settles within the step remainder AND displaces the mount by at
        // least StepMinimumDisplacementMetres (0.01 mm: a hundredth of the established 1 mm sampling resolution, below every lawful interval the
        // search must keep, e.g. the 0.025 mm of a 50.15 mm start). A shorter move is no movement: the search returns zero and native's denial
        // branch ends the step promptly instead of a series of ever-smaller moves toward the completion margin.
        // Round 19 (R18-F1): the same two guards, reported separately (ClassifyStep) so the search moves the bound the failing guard implies:
        // TooLong when the tick does not settle within the remainder (StepSettlementAdmits, which keeps the 0.1 mm completion margin), TooShort
        // when it settles but does not move or moves less than the floor. Executable is exactly ClassifyStep == Executable.
        // Round 20 (R19-F1): the settled stop is the float-representable position (SimulateTick: origin + shift); an unsnapped stop whose
        // settled displacement exceeds the physical remainder (a rounding forward) is TooLong as well, and one rounded back below the floor
        // (or onto the origin) TooShort.
        internal const float StepMinimumDisplacementMetres = 0.00001f;
        internal enum StepSample { Executable, TooShort, TooLong }
        internal static StepSample ClassifyStep(NativeTick tick, IList<Vector3> path, Vector3 origin, float physicalLeft)
        {
            if (!StepSettlementAdmits(tick, path, origin, physicalLeft)) return StepSample.TooLong;
            var settled = GeometryUtils.MechanicsDistance(origin, tick.Stop);
            if (tick.Moving && settled > physicalLeft) return StepSample.TooLong;
            return tick.Moving && settled >= StepMinimumDisplacementMetres ? StepSample.Executable : StepSample.TooShort;
        }
        private static bool Executable(NativeTick tick, IList<Vector3> path, Vector3 origin, float physicalLeft) =>
            ClassifyStep(tick, path, origin, physicalLeft) == StepSample.Executable;

        // PR #70 R8-F6 (round 10): the restricted single tick predicted with the installed steering rule from the agent's live state: its own
        // path and cursor, MoveDirection, speed (MaxSpeed), combat angular speed and waypoint-skip facts. The allowance is a time
        // (allowedMetres / speed); a shift that would pass the waypoint the agent steers to is shortened (the delta returned is smaller), and
        // a waypoint native would skip at that delta is not predicted (refused, conservative). The stop is where this tick leaves the mount
        // (it moves no further under the single-action limit), judged with the production admission under the new direction. On success
        // Route is [origin, stop], RouteMetres the granted time x speed (the adapter returns that delta to native movement).
        // Round 16 (R13-F5, R15-F1, R15-F2): every candidate's stop is the installed tick at that candidate's own duration (SimulateTick: native
        // selection with skipping, steering, completion only on a selected last segment), and physicalStepLeft (a step's physical remainder,
        // +Infinity otherwise) admits only a candidate whose settled travel stays within it (StepSettlementAdmits).
        // Round 17 (R16-F4): allowedTick is the permitted DURATION exactly as the adapter will return it (never allowedMetres / speed, whose
        // rounding can move native's strict skip threshold); every candidate is a duration and Duration is the selected one, returned as it is.
        internal static Result EvaluateFirstTickNative(UnitEntityData rider, UnitEntityData mount, UnitEntityData target, IList<Vector3> agentPath, int cursor,
            float allowedTick, float speed, Vector2 moveDirection, float combatAngular, float currentAgentSpeed, bool followPrecomputedPath, float physicalStepLeft)
        {
            var result = new Result();
            if (rider?.View == null || mount?.View == null || target?.View == null || agentPath == null) { result.Detail = "unavailable"; return result; }
            var probe = new MountedPairSingleAttack(target, rider, mount, true, false);
            probe.Init(rider);
            result.Plan = PlanIdentity.Of(probe);
            result.PairRadius = probe.PairApproachRadius;
            var origin = mount.Position;
            if (cursor < 0 || cursor >= agentPath.Count) { result.Detail = "agent-cursor-outside-path"; return result; }
            var fullDelta = speed > 0f && allowedTick > 0f ? allowedTick : 0f;
            if (!(fullDelta > 0f)) { result.Detail = "no-movement(allowedTick=" + allowedTick.ToString("R") + ")"; return result; }
            // Round 11 (R10-F1): the waypoint native steers to and the delta returned to native are resolved together. For a candidate waypoint
            // the delta is the full allowance, shortened when the shift would pass that waypoint; native skips a (follow-precomputed, not last)
            // waypoint while its distance from the mount is below m_Speed x THAT delta, and then steers to the next one, whose own delta is
            // resolved the same way. The result is accepted only when it is consistent: native's own selection at the final delta (SimulateTick)
            // is the resolved waypoint (every skipped waypoint skipped at that delta, the selected one not). Round 12 (R11-F4): when that stop does
            // not admit the strike (or is inconsistent), the longer executable alternative is evaluated before refusing: the resolution starts at
            // the next waypoint, so every waypoint before it must be skipped by native at the delta that alternative returns (checked), until the
            // route's last point was tried.
            string firstDetail = null;
            var alternatives = 0;
            for (var start = cursor; start < agentPath.Count; alternatives++)
            {
                var index = start;
                Vector3 waypoint = agentPath[index];
                Vector2 unit = default(Vector2), shift = default(Vector2), ignored;
                float distance = 0f, delta = fullDelta;
                bool onLast = false, stopsAtWaypoint = false, moving = true;
                for (;;)
                {
                    waypoint = agentPath[index];
                    onLast = index >= agentPath.Count - 1;
                    var toWaypoint = new Vector2(waypoint.x - origin.x, waypoint.z - origin.z);
                    distance = Length(toWaypoint);
                    if (!(distance > 1e-6f))
                    {
                        if (followPrecomputedPath && !onLast && currentAgentSpeed > 0f) { index++; continue; }
                        moving = false;
                        break;
                    }
                    unit = new Vector2(toWaypoint.x / distance, toWaypoint.y / distance);
                    delta = fullDelta;
                    stopsAtWaypoint = false;
                    shift = NativeCombatShift(moveDirection, unit, distance, onLast, speed, combatAngular, delta, out ignored);
                    if (Length(shift) > distance)
                    {
                        stopsAtWaypoint = true;
                        float low = 0f, high = fullDelta;
                        for (var i = 0; i < 40; i++)
                        {
                            var mid = (low + high) * 0.5f;
                            if (Length(NativeCombatShift(moveDirection, unit, distance, onLast, speed, combatAngular, mid, out ignored)) > distance) high = mid; else low = mid;
                        }
                        delta = low;
                    }
                    if (followPrecomputedPath && !onLast && distance < currentAgentSpeed * delta) { index++; continue; }
                    break;
                }
                string detail = null;
                var tick = default(NativeTick);
                if (!moving) detail = "no-movement(allowedTick=" + allowedTick.ToString("R") + ";start=" + start + ")";
                else
                {
                    tick = SimulateTick(agentPath, cursor, origin, moveDirection, speed, currentAgentSpeed, combatAngular, delta, followPrecomputedPath);
                    if (tick.Selected != index || !tick.Moving)
                        detail = "native-waypoint-unresolved(cursor=" + cursor + ";selected=" + index + ";nativeSelects=" + tick.Selected +
                            ";agentSpeed=" + currentAgentSpeed.ToString("R") + ";delta=" + delta.ToString("R") + ")";
                }
                var admitted = false;
                if (detail == null)
                {
                    var stop = tick.Stop;
                    var newDirection = tick.Direction;
                    var settles = StepSettlementAdmits(tick, agentPath, origin, physicalStepLeft);
                    admitted = settles && Length(newDirection) > 1e-6f &&
                        probe.EvaluateNativeAdmissionAtMountPosition(stop, new Vector3(newDirection.x, 0f, newDirection.y)) == MountedPairNativeAdmissionState.Admitted;
                    var turn = Angle(moveDirection, new Vector2((moveDirection.x + unit.x) * 0.5f, (moveDirection.y + unit.y) * 0.5f));
                    detail = "first-tick(origin=" + origin + ";cursor=" + cursor + ";waypoint#" + index + "=" + waypoint + (onLast ? "(end)" : "(interior)") + ";segment=" +
                        distance.ToString("R") + ";delta=" + delta.ToString("R") + ";shift=" + Length(tick.Shift).ToString("R") + ";travel=" + GeometryUtils.MechanicsDistance(origin, stop).ToString("R") +
                        ";snapped=" + tick.Snapped + ";stop=" + stop + ";stopX=" + stop.x.ToString("R") +
                        ";stopZ=" + stop.z.ToString("R") + ";turnToTarget=" + turn.ToString("R") + ";turnCapacity=" + (tick.EffectiveAngular * delta).ToString("R") +
                        ";baseTurnCapacity=" + (combatAngular * delta).ToString("R") +
                        ";direction=(" + newDirection.x.ToString("R") + "," + newDirection.y.ToString("R") + ");" + (stopsAtWaypoint ? (onLast ? "reaches-route-end" : "stops-at-waypoint") : "interior") +
                        (float.IsPositiveInfinity(physicalStepLeft) ? "" : ";stepTravelLeft=" + physicalStepLeft.ToString("R") + ";settlesWithinStep=" + settles) +
                        ";admitted=" + admitted + (alternatives > 0 ? ";alternative=" + alternatives + ";shorterRefused={" + firstDetail + "}" : "") + ")";
                }
                if (admitted)
                {
                    result.Detail = detail;
                    result.Path = PathOutcome.Complete;
                    result.Route = new List<Vector3> { origin, tick.Stop };
                    result.Duration = delta;
                    result.RouteMetres = delta * speed;
                    result.ArrivalRadius = ArrivalRadii[ArrivalRadii.Length - 1];
                    return result;
                }
                if (firstDetail == null) firstDetail = detail;
                if (onLast || index >= agentPath.Count - 1) break;
                start = index + 1;
            }
            // PR #70 R12-F2 (round 15): the viable INTERIOR stops of the current steered tick at the established resolution (InDiscSampleMetres of
            // shift, i.e. durations k x InDiscSampleMetres / speed below the full allowance), each the installed tick at that duration (round 16:
            // SimulateTick, the waypoint selection with skipping, its steering and its last-segment completion) that settles within a step's
            // physical remainder. The first (shortest) stop admitting the strike is returned with exactly that duration: a visible part of the
            // tick that ends before the full-duration or waypoint-distance candidate is not hidden by a larger grant. Round 18 (R17-F2): a
            // duration whose shift passes its selected waypoint is evaluated at its native settled stop as well (SimulateTick: on an interior
            // segment native does not snap back, the mount ends beyond the waypoint and the cursor advances; on the last segment the completion
            // puts it on the end), and the full permitted duration is the last candidate (its native stop, whatever the waypoint resolution above
            // shortened), so every candidate is judged at the stop native leaves the mount at.
            var interiorStep = Math.Max(InDiscSampleMetres / speed, fullDelta / InteriorStopCandidatesPerTick);
            var interiorTried = 0;
            for (var k = 1; ; k++)
            {
                var dt = k * interiorStep;
                if (!(dt < fullDelta))
                {
                    if ((k - 1) * interiorStep >= fullDelta) break;
                    dt = fullDelta;
                }
                var tick = SimulateTick(agentPath, cursor, origin, moveDirection, speed, currentAgentSpeed, combatAngular, dt, followPrecomputedPath);
                if (!tick.Moving || !StepSettlementAdmits(tick, agentPath, origin, physicalStepLeft)) continue;
                interiorTried++;
                var stop = tick.Stop;
                var newDirection = tick.Direction;
                if (!(Length(newDirection) > 1e-6f) ||
                    probe.EvaluateNativeAdmissionAtMountPosition(stop, new Vector3(newDirection.x, 0f, newDirection.y)) != MountedPairNativeAdmissionState.Admitted) continue;
                result.Detail = "first-tick-interior(origin=" + origin + ";cursor=" + cursor + ";waypoint#" + tick.Selected + "=" + agentPath[tick.Selected] + (tick.OnLast ? "(end)" : "(interior)") +
                    ";delta=" + dt.ToString("R") + ";fullDelta=" + fullDelta.ToString("R") + ";shift=" + Length(tick.Shift).ToString("R") + ";travel=" +
                    GeometryUtils.MechanicsDistance(origin, stop).ToString("R") + ";snapped=" + tick.Snapped + ";stop=" + stop + ";stopX=" + stop.x.ToString("R") + ";stopZ=" +
                    stop.z.ToString("R") + ";direction=(" + newDirection.x.ToString("R") + "," + newDirection.y.ToString("R") + ");passesWaypoint=" + tick.PassesWaypoint +
                    ";interiorCandidate=" + k +
                    ";admitted=True;fullAndWaypointRefused={" + firstDetail + "})";
                result.Path = PathOutcome.Complete;
                result.Route = new List<Vector3> { origin, stop };
                result.Duration = dt;
                result.RouteMetres = dt * speed;
                result.ArrivalRadius = ArrivalRadii[ArrivalRadii.Length - 1];
                return result;
            }
            result.Detail = firstDetail + (alternatives > 1 ? ";alternativesTried=" + (alternatives - 1) : "") + ";interiorStopsTried=" + interiorTried;
            return result;
        }

        // R12-F2 (round 15): the interior-stop search never coarsens below this many candidates per tick (1 mm of shift is finer for any real tick).
        internal const int InteriorStopCandidatesPerTick = 4096;

        // PR #70 R8-F1 (rounds 9-11): the route the agent will execute from its live position (its own path from its cursor) and the strike's
        // admission wherever native proximity can finish the carrier: the proven arrival disc around the destination under the destination
        // segment's heading (DiscAdmits) and every position of the remaining route inside the FULL arrival radius, each with its own segment
        // heading (RouteInsideDiscAdmits, the click's 1 mm sampling). Round 11: both are evaluated on the revalidated untruncated native route
        // (nativeRoute, destinationMetres: the points and arc length the click and the carrier's revalidation chose the destination on), so the
        // samples are those that proved the route and only a change of the live state (target, sight) can change the verdict; the part the mount
        // already travelled (before its projection on the segment toward the agent's cursor) is skipped, and its current pose is checked when it
        // already stands inside the disc. The agent's path must be that route truncated at the destination. Route is [current position,
        // remaining points]; RouteMetres its length; RouteProven only when every check admits the strike.
        internal static Result EvaluateRemainingRoute(UnitEntityData rider, UnitEntityData mount, UnitEntityData target, IList<Vector3> agentPath, int cursor,
            float arrivalRadius, IList<Vector3> nativeRoute, float destinationMetres)
        {
            var result = new Result();
            if (rider?.View == null || mount?.View == null || target?.View == null || agentPath == null) { result.Detail = "unavailable"; return result; }
            var probe = new MountedPairSingleAttack(target, rider, mount, true, false);
            probe.Init(rider);
            result.Plan = PlanIdentity.Of(probe);
            result.PairRadius = probe.PairApproachRadius;
            if (nativeRoute == null || nativeRoute.Count < 2 || float.IsNaN(destinationMetres) || !(arrivalRadius > 0f)) { result.Detail = "no-revalidated-route"; return result; }
            Vector3 destinationFrom, destinationTo;
            List<Vector3> truncated;
            var centre = PointAt(nativeRoute, destinationMetres, out destinationFrom, out destinationTo, out truncated);
            if (truncated == null || truncated.Count != agentPath.Count) { result.Detail = "agent-path-not-revalidated-route"; return result; }
            for (var i = 0; i < truncated.Count; i++)
                if (GeometryUtils.MechanicsDistance(truncated[i], agentPath[i]) > WaypointPassedMetres) { result.Detail = "agent-path-not-revalidated-route"; return result; }
            var position = mount.Position;
            var remaining = new List<Vector3> { position };
            for (var i = Math.Max(0, cursor); i < agentPath.Count; i++)
                if (GeometryUtils.MechanicsDistance(agentPath[i], remaining[remaining.Count - 1]) > WaypointPassedMetres) remaining.Add(agentPath[i]);
            if (remaining.Count < 2) { result.Detail = "no-remaining-route"; return result; }
            Func<Vector3, Vector3, Vector3, bool> admits = (point, from, to) =>
                probe.EvaluateNativeAdmissionAtMountPosition(point, to - from) == MountedPairNativeAdmissionState.Admitted;
            var length = RouteLength(remaining);
            // Round 14 (R11-F3): the mount executes the LIVE connecting leg [actual position -> the agent's cursor point] (the cursor point of the
            // path truncated at the destination). There is no displacement tolerance. Only when the mount stands EXACTLY (bit for bit, horizontally)
            // on the planned segment's start point is the live leg the planned segment itself (same points, same parameters): its established
            // samples are then kept unchanged (R8-F1: the click and the revalidation sample exactly the same points, so a tightly placed destination
            // whose disc begins at the opening's measured start is not refused by a float re-sampling). Any other position, however close, validates
            // the live leg wherever it can enter the FULL arrival radius (LiveLegAdmits, round 15): the live chord is sampled by its own distance at
            // the in-disc spacing with both ends, plus the live points projecting onto the established samples, all with the live heading; the arrival disc is taken
            // under the live heading when the cursor point is the destination, and the established samples are kept from the cursor point on.
            // The current pose is checked when the mount already stands inside the disc.
            var liveEnd = agentPath[Math.Max(0, Math.Min(cursor, agentPath.Count - 1))];
            var liveMetres = GeometryUtils.MechanicsDistance(position, liveEnd);
            var restFrom = 0d;
            for (var i = 1; i <= cursor && i < nativeRoute.Count; i++) restFrom += GeometryUtils.MechanicsDistance(nativeRoute[i - 1], nativeRoute[i]);
            var fromMetres = 0d;
            for (var i = 1; i < cursor && i < nativeRoute.Count; i++) fromMetres += GeometryUtils.MechanicsDistance(nativeRoute[i - 1], nativeRoute[i]);
            var plannedStart = cursor > 0 && cursor < nativeRoute.Count ? nativeRoute[cursor - 1] : agentPath[0];
            // R14-F1 (round 15): the planned-leg shortcut needs BOTH live endpoints to be the planned leg's: the start bit for bit and the live end
            // (the agent's cursor point) bit for bit the revalidated truncated route's point at that cursor (agentPath is admitted within 1 mm of it).
            var plannedEnd = cursor >= 0 && cursor < truncated.Count ? truncated[cursor] : liveEnd;
            var onPlanned = cursor >= 0 && cursor < agentPath.Count && position.x == plannedStart.x && position.z == plannedStart.z &&
                liveEnd.x == plannedEnd.x && liveEnd.z == plannedEnd.z;
            var destinationLeg = cursor >= agentPath.Count - 1;
            var liveHeading = !onPlanned && destinationLeg && liveMetres > WaypointPassedMetres;
            var poseAdmitted = GeometryUtils.SqrMechanicsDistance(position, centre) > arrivalRadius * arrivalRadius || (onPlanned && fromMetres <= 0d) || admits(position, position, position);
            bool admitted;
            var liveDetail = "planned-identity";
            if (onPlanned)
                admitted = poseAdmitted && DiscAdmits(centre, arrivalRadius, destinationFrom, destinationTo, admits) &&
                    RouteInsideDiscAdmits(nativeRoute, destinationMetres, centre, arrivalRadius, admits, fromMetres);
            else
                admitted = poseAdmitted &&
                    DiscAdmits(centre, arrivalRadius, liveHeading ? position : destinationFrom, liveHeading ? liveEnd : destinationTo, admits) &&
                    LiveLegAdmits(nativeRoute, destinationMetres, cursor, position, liveEnd, centre, arrivalRadius, admits, out liveDetail) &&
                    RouteInsideDiscAdmits(nativeRoute, destinationMetres, centre, arrivalRadius, admits, Math.Max(restFrom, 1e-9d));
            result.Detail = "remaining-route(cursor=" + cursor + ";routeMetres=" + length.ToString("R") + ";end=" + centre + ";arrivalDisc=" + arrivalRadius.ToString("R") +
                ";onPlannedSegment=" + onPlanned + ";fromMetres=" + fromMetres.ToString("R") + ";liveLegMetres=" + liveMetres.ToString("R") + ";liveLeg={" + liveDetail + "};restFromMetres=" + restFrom.ToString("R") +
                ";destinationMetres=" + destinationMetres.ToString("R") + ";liveHeading=" + liveHeading + ";admitted=" + admitted + ")";
            if (!admitted) return result;
            result.Path = PathOutcome.Complete;
            result.Route = remaining;
            result.RouteMetres = length;
            result.ArrivalRadius = arrivalRadius;
            return result;
        }

        // R11-F3 (round 14), R14-F1 (round 15): the live leg [position -> liveEnd] wherever it lies inside the FULL arrival radius. Its in-disc chord
        // [u0, u1] (u the live leg's own parameter) is computed in double precision and sampled BY DISTANCE ALONG THE LIVE CHORD itself: every
        // InDiscSampleMetres from its entry, both ends included, whatever its direction relative to the planned segment (a live chord almost
        // perpendicular to the planned segment projects onto a tiny part of it, so samples spaced on the planned segment could leave most of the
        // live chord unchecked). Where the live leg projects onto the planned segment's established in-disc samples (nativeRoute[cursor - 1] ->
        // nativeRoute[cursor], as RouteInsideDiscAdmits samples it), the live points projecting onto those samples are checked as well (retained
        // established samples). Every point is computed explicitly (position + u x leg), never with Vector3.Lerp (Unity clamps its parameter to
        // [0, 1]); every sample uses the live heading.
        internal static bool LiveLegAdmits(IList<Vector3> nativeRoute, float destinationMetres, int cursor, Vector3 position, Vector3 liveEnd, Vector3 centre,
            float radius, Func<Vector3, Vector3, Vector3, bool> admits, out string detail)
        {
            double ex = liveEnd.x - position.x, ey = liveEnd.y - position.y, ez = liveEnd.z - position.z;
            var liveLength = Math.Sqrt(ex * ex + ez * ez);
            var r2 = (double)radius * radius;
            if (liveLength <= 0d) { detail = "zero-length"; return true; }
            double cx = centre.x - position.x, cz = centre.z - position.z;
            var closestU = (cx * ex + cz * ez) / (liveLength * liveLength);
            double qx = ex * closestU - cx, qz = ez * closestU - cz;
            var perpendicular = Math.Sqrt(qx * qx + qz * qz);
            if (perpendicular * perpendicular > r2) { detail = "outside(perpendicular=" + perpendicular.ToString("R") + ")"; return true; }
            var halfU = Math.Sqrt(r2 - perpendicular * perpendicular) / liveLength;
            var u0 = Math.Max(0d, closestU - halfU); var u1 = Math.Min(1d, closestU + halfU);
            if (u1 < u0) { detail = "outside(perpendicular=" + perpendicular.ToString("R") + ")"; return true; }
            var chord = (u1 - u0) * liveLength;
            Func<double, Vector3> at = u => new Vector3((float)(position.x + ex * u), (float)(position.y + ey * u), (float)(position.z + ez * u));
            var head = "perpendicular=" + perpendicular.ToString("R") + ";liveChordMetres=" + chord.ToString("R");
            // (1) The live chord by its own distance, both ends included.
            var liveSamples = 0;
            var liveStep = InDiscSampleMetres / liveLength;
            for (var k = 0; ; k++)
            {
                var u = u0 + k * liveStep;
                if (u >= u1) u = u1;
                liveSamples++;
                if (!admits(at(u), position, liveEnd))
                {
                    var p = at(u);
                    detail = head + ";refusedOnLiveChordAt=" + u.ToString("R") + ";refusedPoint=(" + p.x.ToString("R") + "," + p.z.ToString("R") + ");liveSamples=" + liveSamples;
                    return false;
                }
                if (u >= u1) break;
            }
            // (2) The retained established samples of the planned chord, at the live points projecting onto them.
            var plannedSamples = 0;
            var plannedChord = "none";
            if (cursor > 0 && cursor < nativeRoute.Count)
            {
                var a = nativeRoute[cursor - 1]; var b = nativeRoute[cursor];
                double dx = b.x - a.x, dz = b.z - a.z;
                var length = Math.Sqrt(dx * dx + dz * dz);
                if (length > 0d)
                {
                    var tp = ((position.x - a.x) * dx + (position.z - a.z) * dz) / (length * length);
                    var te = ((liveEnd.x - a.x) * dx + (liveEnd.z - a.z) * dz) / (length * length);
                    var span = te - tp;
                    if (Math.Abs(span) * length > 1e-7d)
                    {
                        var travelled = 0d;
                        for (var i = 1; i < cursor; i++) travelled += GeometryUtils.MechanicsDistance(nativeRoute[i - 1], nativeRoute[i]);
                        var limit = Math.Min(1d, (destinationMetres - travelled) / length);
                        double fx = centre.x - a.x, fz = centre.z - a.z;
                        var closest = (fx * dx + fz * dz) / (length * length);
                        double px = a.x + dx * closest - centre.x, pz = a.z + dz * closest - centre.z;
                        var plannedPerpendicularSqr = px * px + pz * pz;
                        if (plannedPerpendicularSqr <= r2)
                        {
                            var half = Math.Sqrt(r2 - plannedPerpendicularSqr) / length;
                            var t0 = Math.Max(0d, closest - half);
                            var t1 = Math.Min(limit, closest + half);
                            if (t1 >= t0)
                            {
                                plannedChord = t0.ToString("R") + ".." + t1.ToString("R");
                                var step = InDiscSampleMetres / length;
                                for (var t = t0; ; t += step)
                                {
                                    if (t >= t1) t = t1;
                                    var u = (t - tp) / span;
                                    if (u >= u0 && u <= u1)
                                    {
                                        plannedSamples++;
                                        if (!admits(at(u), position, liveEnd)) { detail = head + ";refusedAtPlannedParameter=" + t.ToString("R") + ";liveSamples=" + liveSamples; return false; }
                                    }
                                    if (t >= t1) break;
                                }
                            }
                        }
                    }
                }
            }
            detail = head + ";liveChord=" + u0.ToString("R") + ".." + u1.ToString("R") + ";liveSamples=" + liveSamples + ";plannedChord=" + plannedChord +
                ";atPlannedParameters=" + plannedSamples;
            return true;
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
                    result.NativeRoute = new List<Vector3>(points);
                    result.DestinationMetres = destination;
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
        // PR #70 R8-F1 (round 11): fromMetres > 0 skips the route before that arc length (already travelled; the caller checks the current pose)
        // without changing any later sample, so a revalidation on the same points samples exactly what the click sampled.
        internal static bool RouteInsideDiscAdmits(IList<Vector3> points, float destinationMetres, Vector3 centre, float radius,
            Func<Vector3, Vector3, Vector3, bool> admits, double fromMetres = 0d)
        {
            if (points.Count == 0) return true;
            if (fromMetres <= 0d && GeometryUtils.SqrMechanicsDistance(points[0], centre) <= radius * radius && !admits(points[0], points[0], points[0])) return false;
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
                    if (fromMetres > travelled) t0 = Math.Max(t0, (fromMetres - travelled) / length);
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
