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
    // PR #70 R8-F6 (round 9): the installed shift of one turn-based movement tick (UnitMovementAgent.TickMovement after CanMoveInTurnBased,
    // asserted in scripts/Test-MmrTbApproachNativeContract.ps1): warm-up returns without moving while m_WarmupTime > 0; UpdateAvoidance is
    // skipped in turn-based combat; the speed is MaxSpeed, raised to at least MaxSpeed x m_MinSpeed and cut only by m_SlowDownTime or by
    // SlowDown when the move direction must turn more than this tick's angular step; the shift is MoveDirection x speed x the hook's delta.
    // TickDelegated sets m_MinSpeed 1, m_WarmupTime 0 and m_SlowDownTime 0 for an allowed tick, so the shift is exactly MaxSpeed x delta
    // along MoveDirection when MoveDirection already points at the waypoint. PhysicalSpeed and MoveDirection expose those two facts.
    internal static class MountedAgentSteering
    {
        private static readonly FieldInfo NextIndex = Field("m_NextPointIndex", 0x04001193, typeof(int));
        private static readonly FieldInfo NextWaypoint = Field("m_NextWaypoint", 0x04001196, typeof(Vector2));
        private static readonly PropertyInfo MaxSpeed = typeof(UnitMovementAgent).GetProperty("MaxSpeed",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

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

        // NaN when the installed speed cannot be read (the first-tick decision then refuses a moving commit).
        internal static float PhysicalSpeed(UnitMovementAgent agent)
        {
            if (agent == null || MaxSpeed == null || MaxSpeed.PropertyType != typeof(float)) return float.NaN;
            return (float)MaxSpeed.GetValue(agent, null);
        }

        internal static Vector2 MoveDirection(UnitMovementAgent agent) => agent == null ? default(Vector2) : agent.MoveDirection;
    }
}
