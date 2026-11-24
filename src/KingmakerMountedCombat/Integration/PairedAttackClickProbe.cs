using System;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.Utility;
using UnityEngine;

namespace KingmakerMountedCombat.Integration
{
    // MMR-02 (reviews R1-F3, R3-F2). The facts KMC needs to prove a dropped turn-based rider melee click legal by its own rule,
    // without changing anything:
    //  - the strike's PRODUCTION admission, evaluated on an unrun MountedPairSingleAttack for the exact rider, mount and target
    //    (mount origin, planned weapon range, native command line of sight; the object never enters a command container);
    //  - when movement is needed, a native route for the MOUNT's own agent (UnitMovementAgent.FindPath with the strike's pair
    //    radius and line of sight, the same call native's path preview makes), its cost along the route, and whether the
    //    route's endpoint admits the strike (UnitCommand.IsUnitCloseEnough from the endpoint, rider eye offset kept).
    internal static class PairedAttackClickProbe
    {
        internal sealed class Result
        {
            public bool AdmissibleNow;
            public float PairRadius = float.NaN;
            public bool RouteComplete;
            public float RouteMetres = float.NaN;
            public bool EndpointAdmitsStrike;
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
            var losObject = probe.GetTargetLOSObjectId();
            var path = agent.FindPath(target.Position, ignored => { }, result.PairRadius, true, losObject);
            if (path == null) { result.Detail = "agent-busy"; return result; }
            AstarPath.WaitForPath(path);
            var points = path.vectorPath;
            if (path.error || !path.IsDone() || points == null || points.Count == 0)
            {
                result.Detail = "route-failed";
                return result;
            }
            var length = 0f;
            for (var index = 1; index < points.Count; index++) length += GeometryUtils.MechanicsDistance(points[index - 1], points[index]);
            var endpoint = points[points.Count - 1];
            var riderEyeOffset = rider.EyePosition - mount.Position;
            result.RouteComplete = true;
            result.RouteMetres = length;
            result.EndpointAdmitsStrike = UnitCommand.IsUnitCloseEnough(target.Position, endpoint, endpoint + riderEyeOffset,
                result.PairRadius + Domain.MountedCombatSpatialPolicy.RangeTolerance, true, losObject);
            result.Detail = "route";
            return result;
        }
    }
}
