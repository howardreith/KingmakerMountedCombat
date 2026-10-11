using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Utility;
using Kingmaker.View;

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
