using System.Collections.Generic;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Utility;
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
    //    execution agree on the path, the stopping point, its line of sight and the rider offset.
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

        internal static Result Evaluate(UnitEntityData rider, UnitEntityData mount, UnitEntityData target)
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
            var route = new List<Vector3> { points[0] };
            var travelled = 0f;
            for (var index = 1; index < points.Count; index++)
            {
                var from = points[index - 1];
                var to = points[index];
                var segment = GeometryUtils.MechanicsDistance(from, to);
                var steps = Mathf.Max(1, Mathf.CeilToInt(segment / SampleStepMetres));
                for (var step = 1; step <= steps; step++)
                {
                    var point = Vector3.Lerp(from, to, (float)step / steps);
                    if (probe.EvaluateNativeAdmissionAtMountPosition(point) != MountedPairNativeAdmissionState.Admitted) continue;
                    route.Add(point);
                    result.RouteMetres = travelled + segment * step / steps;
                    result.EndpointAdmitsStrike = true;
                    result.Route = route;
                    result.Detail = "route";
                    return result;
                }
                route.Add(to);
                travelled += segment;
            }
            result.RouteMetres = travelled;
            result.Detail = "route-never-admits";
            return result;
        }
    }
}
