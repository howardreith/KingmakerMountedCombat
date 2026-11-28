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
    // PR #70 R8-F6 (rounds 9-10): the installed shift of one turn-based movement tick (UnitMovementAgent.TickMovement after CanMoveInTurnBased
    // returns its delta; asserted in scripts/Test-MmrTbApproachNativeContract.ps1). Warm-up returns without moving while m_WarmupTime > 0. The
    // waypoint is skipped forward while (not forced, m_FollowPrecomputedPath, not on the last segment) its distance is below m_Speed x delta.
    // Speed is MaxSpeed (MaxSpeedOverride, else the unit's CurrentSpeedMps), raised to MaxSpeed x m_MinSpeed; SlowDown returns at once for a
    // unit in turn-based combat; m_SlowDownTime and m_WarmupTime are zeroed by TickDelegated. In combat (CombatMode = Unit.IsInCombat, no
    // first-tick snap) the direction is RotateTowards(MoveDirection, Lerp(MoveDirection, toWaypoint, 0.5), m_CombatAngularSpeed x delta x
    // Deg2Rad, 1), with the angular speed raised on the last segment when the turn would outlast the arrival; the shift is that new
    // MoveDirection x speed x delta. PairedAttackClickProbe.NativeCombatShift computes exactly that from the facts read here.
    internal static class MountedAgentSteering
    {
        private static readonly FieldInfo NextIndex = Field(typeof(UnitMovementAgent), "m_NextPointIndex", 0x04001193, typeof(int));
        private static readonly FieldInfo NextWaypoint = Field(typeof(UnitMovementAgent), "m_NextWaypoint", 0x04001196, typeof(Vector2));
        private static readonly FieldInfo FollowPrecomputed = Field(typeof(UnitMovementAgent), "m_FollowPrecomputedPath", 0x040011AF, typeof(bool));
        private static readonly FieldInfo ForceMode = Field(typeof(UnitMovementAgent), "m_IsInForceMode", 0x040011AE, typeof(bool));
        private static readonly FieldInfo CombatAngularSpeed = Field(typeof(UnitMovementAgent).BaseType, "m_CombatAngularSpeed", 0x040011CD, typeof(float));
        private static readonly FieldInfo CurrentSpeed = Field(typeof(UnitMovementAgent).BaseType, "m_Speed", 0x040011D3, typeof(float));
        private static readonly PropertyInfo MaxSpeed = typeof(UnitMovementAgent).GetProperty("MaxSpeed",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        private static FieldInfo Field(System.Type owner, string name, int token, System.Type type)
        {
            var field = owner?.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
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

        // R9-F5 (round 10): NaN when the installed speed cannot be read, including a getter that throws (contained here).
        internal static float PhysicalSpeed(UnitMovementAgent agent)
        {
            try
            {
                if (agent == null || MaxSpeed == null || MaxSpeed.PropertyType != typeof(float)) return float.NaN;
                return (float)MaxSpeed.GetValue(agent, null);
            }
            catch (System.Exception) { return float.NaN; }
        }

        // R8-F6 (round 10): the remaining facts of the installed combat steering rule; false when any is unreadable.
        internal static bool TryReadSteering(UnitMovementAgent agent, out Vector2 moveDirection, out float combatAngularSpeed, out float currentSpeed,
            out bool followPrecomputedPath, out bool forceMode, out bool inCombat)
        {
            moveDirection = default(Vector2); combatAngularSpeed = currentSpeed = float.NaN; followPrecomputedPath = forceMode = inCombat = false;
            try
            {
                if (agent == null || FollowPrecomputed == null || ForceMode == null || CombatAngularSpeed == null || CurrentSpeed == null) return false;
                moveDirection = agent.MoveDirection;
                combatAngularSpeed = (float)CombatAngularSpeed.GetValue(agent);
                currentSpeed = (float)CurrentSpeed.GetValue(agent);
                followPrecomputedPath = (bool)FollowPrecomputed.GetValue(agent);
                forceMode = (bool)ForceMode.GetValue(agent);
                inCombat = agent.Unit?.EntityData != null && agent.Unit.EntityData.IsInCombat;
                return true;
            }
            catch (System.Exception) { return false; }
        }

        internal static Vector2 MoveDirection(UnitMovementAgent agent) => agent == null ? default(Vector2) : agent.MoveDirection;
    }
}
