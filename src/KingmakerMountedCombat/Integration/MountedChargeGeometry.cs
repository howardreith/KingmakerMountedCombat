using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Utility;
using Kingmaker.View;
using UnityEngine;

namespace KingmakerMountedCombat.Integration
{
    // Chunk 6B increment 6B.2: the charge geometry both the admission path and the running transaction read.
    //
    // These were private to the controller, which was fine while only admission read them. The running
    // transaction now revalidates the same mutable conditions before every repath or re-force and before the
    // approach-to-attack transition, and it must read them exactly as admission did rather than approximate
    // them, so the readers live here and both callers share one implementation.
    //
    // Every value is read from the MOUNT, which is the mover, exactly as the stock AbilityCustomCharge reads
    // caster position, speed and corpulence. Nothing here writes anything.
    internal static class MountedChargeGeometry
    {
        internal static float MaximumRange(UnitEntityData mount)
        {
            return mount.CombatSpeedMps * 6f;
        }

        // The native navmesh trace: a straight charge line exists only when the trace reaches the point.
        internal static bool StraightRoute(UnitEntityData mount, UnitEntityData target)
        {
            if (mount?.View == null || target?.View == null)
            {
                return false;
            }

            return ObstacleAnalyzer.TraceAlongNavmesh(mount.Position, target.Position) == target.Position;
        }

        // The stopping radius the rider's planned attack is admitted from, measured from the mount.
        private static float ReachRadius(UnitEntityData mount, UnitEntityData rider, UnitEntityData target) =>
            mount.View.Corpulence + target.View.Corpulence + (rider?.GetFirstWeapon()?.AttackRange.Meters ?? 0f);

        internal static bool WithinRiderReach(UnitEntityData mount, UnitEntityData rider, UnitEntityData target) =>
            mount?.View != null && target?.View != null && rider?.GetFirstWeapon() != null &&
            GeometryUtils.MechanicsDistance(mount.Position, target.Position) <= ReachRadius(mount, rider, target);

        // The native line is the centre of a body, and the mount's body is what travels. Along the lane, up to the landing point, both
        // flanks at the 0.8 of corpulence the native landing check allows must stay on the navmesh. The ground the mount already
        // occupies is not asked: it stands there lawfully, and a wall beside it does not close a lane leading away.
        internal static bool FootprintClear(UnitEntityData mount, UnitEntityData rider, UnitEntityData target)
        {
            if (mount?.View == null || target?.View == null) return false;
            var line = (target.Position - mount.Position).To2D();
            var total = line.magnitude;
            var length = total - ReachRadius(mount, rider, target);
            var corpulence = mount.View.Corpulence;
            if (length <= corpulence) return true;
            var side = new Vector2(-line.y, line.x).normalized * (corpulence * 0.8f);
            for (var along = corpulence; ; along = Mathf.Min(along + Mathf.Max(corpulence * 0.5f, 0.25f), length))
            {
                var centre = Vector3.Lerp(mount.Position, target.Position, along / total);
                if (!FlankClear(centre, side) || !FlankClear(centre, -side)) return false;
                if (along >= length) return true;
            }
        }

        private static bool FlankClear(Vector3 centre, Vector2 side)
        {
            var flank = centre + side.To3D();
            return (ObstacleAnalyzer.TraceAlongNavmesh(centre, flank).To2D() - flank.To2D()).sqrMagnitude < 0.0025f;
        }

        internal static bool MountAvoidanceDisabled(UnitEntityData mount)
        {
            return mount?.View != null && mount.View.MovementAgent != null && mount.View.MovementAgent.AvoidanceDisabled;
        }

        // The stock clearance check, read from the mount. The carried rider is never an obstacle to its own
        // mount, and the landing point is one rider weapon reach short of the target, as the stock check is.
        internal static bool LandingBlocked(UnitEntityData mount, UnitEntityData rider, UnitEntityData target)
        {
            if (mount?.View == null || target?.View == null)
            {
                return false;
            }

            var weapon = rider?.GetFirstWeapon();
            var separation = mount.View.Corpulence + target.View.Corpulence +
                (weapon == null ? 0f : weapon.AttackRange.Meters);
            var direction = (target.Position - mount.Position).To2D().normalized;
            var landing = target.Position.To2D() - direction * separation;
            var state = Game.Instance?.State;
            if (state?.AwakeUnits == null)
            {
                return false;
            }

            return state.AwakeUnits.Any(actor => actor != mount && actor != rider && actor != target &&
                actor.View != null && actor.View.MovementAgent != null && !actor.View.MovementAgent.AvoidanceDisabled &&
                (landing - actor.Position.To2D()).magnitude < (mount.View.Corpulence + actor.View.Corpulence) * 0.8f);
        }
    }
}
