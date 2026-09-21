using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker;
using Kingmaker.Controllers.Projectiles;

namespace KingmakerMountedCombat.Integration
{
    // A projectile reaches its target in one controller and resolves effects in
    // another. A save barrier cannot infer completed effects from IsHit alone.
    internal static class NativeSaveEffectBoundary
    {
        private static readonly System.Reflection.FieldInfo Pending =
            NativeCombatActorPersistence.Field(typeof(ProjectileController), "m_NewProjectiles",
                0x04005E2C, typeof(List<Projectile>));
        private static ConditionalWeakTable<Projectile, object> completed = new ConditionalWeakTable<Projectile, object>();
        private static readonly object completedMarker = new object();

        internal static void HitCompleted(Projectile projectile)
        {
            if (projectile != null) completed.GetValue(projectile, p => completedMarker);
        }
        internal static void Clear() => completed = new ConditionalWeakTable<Projectile, object>();

        internal static bool HasUnresolvedProjectiles() => HasUnresolvedProjectiles(Game.Instance?.ProjectileController);

        internal static bool HasUnresolvedProjectiles(ProjectileController controller)
        {
            if (controller == null) { Clear(); return false; }
            // Finish this native iterator: its updating flag is reset only after
            // full enumeration. Any/First directly on it would leave that flag set.
            var active = controller.Projectiles.ToArray()
                .Concat((List<Projectile>)Pending.GetValue(controller)).Distinct().ToArray();
            return active.Any(p => !p.Cleared && !completed.TryGetValue(p, out var marker));
        }
    }
}
