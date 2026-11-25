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
    //    execution agree on the path, the stopping point, its line of sight and the rider offset. Reviews R5-F2, R6-F1, R6-F2:
    //    the walk is bounded by the mount's grant (the exact grant point is a sample; no endpoint lies beyond it), each point is
    //    evaluated with the rider and eye offsets rotated to the mount's heading on that segment, and the carrier's point
    //    command targets the verified endpoint itself with a small arrival radius.
    internal static class PairedAttackClickProbe
    {
        internal const float SampleStepMetres = 0.2f;

        internal sealed class Result
        {
            public bool AdmissibleNow;
            public float PairRadius = float.NaN;
            public bool RouteComplete;
            public float RouteMetres = float.NaN;
            public bool EndpointAdmitsStrike;
            public List<Vector3> Route;
            public string Detail = "<none>";
        }

        // budgetMetres: the mount's remaining grant; the walk never returns an endpoint beyond it (review R5-F2).
        internal static Result Evaluate(UnitEntityData rider, UnitEntityData mount, UnitEntityData target, float budgetMetres)
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
            List<Vector3> route;
            float cost;
            Func<Vector3, Vector3, Vector3, bool> admits = (point, from, to) =>
                probe.EvaluateNativeAdmissionAtMountPosition(point, to - from) == MountedPairNativeAdmissionState.Admitted;
            Func<Vector3, Vector3, float> distance = (from, to) => GeometryUtils.MechanicsDistance(from, to);
            if (MountedRouteWalker.Walk(points, distance, Vector3.Lerp, admits, Math.Max(0f, budgetMetres), out route, out cost))
            {
                result.RouteMetres = cost;
                result.EndpointAdmitsStrike = true;
                result.Route = route;
                result.Detail = "route";
                return result;
            }
            // Not within the grant: is the strike reachable on this route at all? Then it is insufficient movement: no route within
            // the grant exists, so the cost within the grant is infinite (the walk's reach is kept for the diagnostic only). This
            // keeps the policy's small tolerance from admitting a reach the bounded walk excluded.
            List<Vector3> unbounded;
            if (MountedRouteWalker.Walk(points, distance, Vector3.Lerp, admits, float.PositiveInfinity, out unbounded, out cost))
            {
                result.RouteMetres = float.PositiveInfinity;
                result.EndpointAdmitsStrike = true;
                result.Detail = "beyond-grant(reach=" + cost.ToString("R") + ")";
                return result;
            }
            result.RouteMetres = cost;
            result.Detail = "route-never-admits";
            return result;
        }
    }
}
