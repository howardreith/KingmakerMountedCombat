using System;
using System.Linq;
using System.Reflection;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.Utility;
using Kingmaker.View;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // Read-only state at native command boundaries and in the disposable route
    // comparison. Shared avoidance values are causal only inside that actor's Tick.
    internal static class NativeGroundMovementObservation
    {
        private static readonly FieldInfo NextIndex = Field("m_NextPointIndex", 0x04001193, typeof(int));
        private static readonly FieldInfo NextWaypoint = Field("m_NextWaypoint", 0x04001196, typeof(Vector2));
        private static readonly FieldInfo FirstTick = Field("m_FirstTick", 0x040011AD, typeof(bool));
        private static readonly FieldInfo Roaming = Field("m_Roaming", 0x04001192, typeof(bool));
        private static readonly FieldInfo NextVelocity = Field("m_NextVelocity", 0x040011B7, typeof(Vector3));
        private static readonly FieldInfo StuckTime = Field("m_StuckTimeStop", 0x040011A1, typeof(float));

        private static FieldInfo Field(string name, int token, Type type)
        {
            var field = typeof(UnitMovementAgent).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null || field.MetadataToken != token || field.FieldType != type)
                throw new MissingFieldException(typeof(UnitMovementAgent).FullName, name);
            return field;
        }

        private static JArray Point(Vector3 value) => new JArray(value.x, value.y, value.z);

        internal static JObject Capture(UnitEntityData actor, UnitMoveTo command)
        {
            var agent = actor?.View?.AgentASP;
            if (agent == null || command == null) return null;
            var points = agent.Path?.vectorPath;
            var end = points != null && points.Count > 0 ? (Vector3?)points[points.Count - 1] : null;
            var index = (int)NextIndex.GetValue(agent);
            var waypoint = (Vector2)NextWaypoint.GetValue(agent);
            var previous = points != null && index > 0 && index < points.Count ? (Vector3?)points[index - 1] : null;
            var position = agent.transform.position;
            return new JObject {
                ["pathPoints"] = points?.Count ?? 0,
                ["path"] = points == null ? new JArray() : new JArray(points.Select(Point)),
                ["pathEnd"] = end.HasValue ? Point(end.Value) : null,
                ["viewPosition"] = Point(position), ["dataPosition"] = Point(actor.Position),
                ["targetDistance"] = GeometryUtils.MechanicsDistance(position, command.ApproachPoint),
                ["pathEndDistance"] = end.HasValue ? (float?)GeometryUtils.MechanicsDistance(position, end.Value) : null,
                ["targetToPathEnd"] = end.HasValue ? (float?)GeometryUtils.MechanicsDistance(command.ApproachPoint, end.Value) : null,
                ["corpulence"] = actor.View.Corpulence, ["avoidanceDisabled"] = agent.AvoidanceDisabled,
                ["approachRadius"] = agent.ApproachRadius, ["maxApproachRadius"] = agent.MaxApproachRadius,
                ["reallyMoving"] = agent.IsReallyMoving, ["wantsToMove"] = agent.WantsToMove,
                ["nextIndex"] = index, ["nextWaypoint"] = new JArray(waypoint.x, waypoint.y),
                ["waypointPlaneDot"] = previous.HasValue ? (float?)Vector2.Dot(
                    waypoint - new Vector2(previous.Value.x, previous.Value.z),
                    waypoint - new Vector2(position.x, position.z)) : null,
                ["firstTick"] = (bool)FirstTick.GetValue(agent), ["roaming"] = (bool)Roaming.GetValue(agent),
                ["nextVelocity"] = Point((Vector3)NextVelocity.GetValue(agent)),
                ["moveDirection"] = new JArray(agent.MoveDirection.x, agent.MoveDirection.y),
                ["stuckTime"] = (float)StuckTime.GetValue(agent),
                ["sharedHasNavmeshObstacles"] = ObstacleAnalyzer.HasNavmeshObstacles,
                ["sharedDirectionBlockedByStatic"] = ObstacleAnalyzer.MainDirectionBlockedByStatic
            };
        }

        internal static JObject CaptureFootprint(UnitEntityData actor, Vector3 center)
        {
            var radius = Math.Max(0.5f, actor.View.Corpulence);
            var probes = new JArray();
            for (var index = 0; index < 8; index++)
            {
                var requested = center + Quaternion.Euler(0f, index * 45f, 0f) * Vector3.forward * radius;
                var endpoint = ObstacleAnalyzer.TraceAlongNavmesh(center, requested);
                probes.Add(new JObject { ["requested"] = Point(requested), ["endpoint"] = Point(endpoint),
                    ["residual"] = GeometryUtils.MechanicsDistance(requested, endpoint) });
            }
            return new JObject { ["center"] = Point(center), ["corpulence"] = actor.View.Corpulence,
                ["probeRadius"] = radius, ["probes"] = probes };
        }
    }
}
