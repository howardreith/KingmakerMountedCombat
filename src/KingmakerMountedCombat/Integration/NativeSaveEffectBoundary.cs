using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker;
using Kingmaker.Controllers.Projectiles;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;

namespace KingmakerMountedCombat.Integration
{
    // A projectile reaches its target in one controller and resolves effects in
    // another. A save barrier cannot infer completed effects from IsHit alone.
    internal static class NativeSaveEffectBoundary
    {
        private static readonly System.Reflection.FieldInfo Active =
            NativeCombatActorPersistence.Field(typeof(ProjectileController), "m_Projectiles",
                0x04005E2B, typeof(HashSet<Projectile>));
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
            // Game-thread reads only. The public Projectiles iterator changes
            // m_Updating, even when nested inside an existing native iteration.
            return ((HashSet<Projectile>)Active.GetValue(controller)).Any(Unresolved)
                || ((List<Projectile>)Pending.GetValue(controller)).Any(Unresolved);
        }

        internal static Projectile[] CaptureUnresolvedProjectiles(ProjectileController controller)
        {
            if (controller == null) return new Projectile[0];
            return ((HashSet<Projectile>)Active.GetValue(controller))
                .Concat((List<Projectile>)Pending.GetValue(controller)).Distinct().Where(Unresolved).ToArray();
        }

        internal static bool CommandNeedsSettlement(UnitCommand command) =>
            command != null && !command.IsFinished && command.GetType() != typeof(UnitMoveContiniously) &&
            // The mounted wrapper starts to own approach before UnitAttack.OnStart.
            // Match native unmounted approach: position/debt can snapshot until
            // the real attack sequence begins; never classify its effects as movement.
            (!(command is MountedPairAttackCommand mounted) || mounted.NativeSequenceStarted) &&
            (command.IsRunning || command is UnitAttackOfOpportunity);

        // Native reaction debt is charged when Run queues this command, before
        // Start. It must be allowed to deliver before a save can snapshot it.
        internal static bool MayStartDuringWait(UnitCommand command) => command is UnitAttackOfOpportunity;

        private static bool Unresolved(Projectile projectile) =>
            !projectile.Cleared && !completed.TryGetValue(projectile, out var marker);
    }
}
