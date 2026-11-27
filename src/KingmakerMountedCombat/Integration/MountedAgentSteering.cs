using System.Collections.Generic;
using System.Reflection;
using Kingmaker.View;
using UnityEngine;

namespace KingmakerMountedCombat.Integration
{
    // PR #70 R6-F2 (round 8): the movement agent's LIVE steering state, the segment native actually executes on the next movement tick: the
    // agent's own path (UnitMovementAgent.Path, which may differ from the command's ForcedPath), its cursor m_NextPointIndex (0x04001193) and
    // the waypoint it steers to, m_NextWaypoint (0x04001196, x/z). The same fields NativeGroundMovementObservation reads; a missing or
    // changed field makes the state unreadable (the first-tick decision then refuses).
    internal static class MountedAgentSteering
    {
        private static readonly FieldInfo NextIndex = Field("m_NextPointIndex", 0x04001193, typeof(int));
        private static readonly FieldInfo NextWaypoint = Field("m_NextWaypoint", 0x04001196, typeof(Vector2));

        private static FieldInfo Field(string name, int token, System.Type type)
        {
            var field = typeof(UnitMovementAgent).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            return field != null && field.MetadataToken == token && field.FieldType == type ? field : null;
        }

        internal static bool TryRead(UnitMovementAgent agent, out List<Vector3> path, out int nextIndex, out Vector2 nextWaypoint)
        {
            path = null; nextIndex = -1; nextWaypoint = default(Vector2);
            if (agent == null || NextIndex == null || NextWaypoint == null) return false;
            path = agent.Path?.vectorPath;
            if (path == null) return false;
            nextIndex = (int)NextIndex.GetValue(agent);
            nextWaypoint = (Vector2)NextWaypoint.GetValue(agent);
            return true;
        }
    }
}
