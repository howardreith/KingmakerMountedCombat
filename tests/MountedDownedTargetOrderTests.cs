using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // Mounted attacks on a prone, downed enemy. Native Kingmaker 2.1.7b keeps an explicit attack order on an enemy that
    // was already unconscious when ordered (UnitAttack.Init m_IsUnconsciousAtStartOfCommand, UpdateTarget), and an
    // unconscious unit lies prone (UnitProneController.ShouldBeProne). The mounted pair path required a conscious target
    // at every gate, so the same order from a mounted rider was silently cancelled.
    internal static class MountedDownedTargetOrderTests
    {
        internal static void Register(TestRunner runner)
        {
            runner.Run("downed target: an explicit order on an already unconscious enemy stays valid while it lives", ExplicitOrderAdmitsDowned);
            runner.Run("downed target: only a player order carries the allowance; brain proposals and cancellation never do", OnlyPlayerOrders);
            runner.Run("downed target: a mounted ranged order dispatches the rider's own shot without mount substitution", RangedDispatchUnchanged);
        }

        private static bool Admits(bool conscious, bool dead, bool finallyDead, bool ordered) =>
            UnifiedMountedStockAttackPolicy.TargetLifeAdmits(conscious, dead, finallyDead, ordered);

        private static void ExplicitOrderAdmitsDowned()
        {
            TestRunner.True(Admits(true, false, false, false), "A conscious standing or tripped enemy was refused.");
            TestRunner.True(Admits(false, false, false, true), "An enemy ordered while already unconscious was refused.");
            // An enemy that falls unconscious after the order is abandoned, exactly as native UpdateTarget does.
            TestRunner.True(!Admits(false, false, false, false), "A target that fell unconscious after the order stayed valid.");
            TestRunner.True(!Admits(false, true, false, true), "A dead target stayed valid.");
            TestRunner.True(!Admits(false, true, true, true) && !Admits(true, false, true, false), "A finally dead target stayed valid.");
            // The whole target check keeps every other requirement.
            TestRunner.True(!UnifiedMountedStockAttackPolicy.IsValidTarget(true, true, Admits(false, false, false, true), false, true),
                "A downed ally became a valid target.");
            TestRunner.True(!UnifiedMountedStockAttackPolicy.IsValidTarget(true, false, Admits(false, false, false, true), true, true),
                "A downed target outside the scene state became valid.");
        }

        private static void OnlyPlayerOrders()
        {
            var target = new object();
            var intent = new MountedAttackIntent<object, object>();
            intent.Begin(target, null, false, true, targetUnconsciousWhenOrdered: true);
            TestRunner.True(intent.TargetUnconsciousWhenOrdered && !intent.Automatic, "The player's order on a downed enemy was not recorded.");
            intent.Begin(target, null, false, true, automatic: true, targetUnconsciousWhenOrdered: true);
            TestRunner.True(!intent.TargetUnconsciousWhenOrdered, "A party-brain proposal received the explicit-order allowance.");
            // A click that makes a brain proposal the player's own does not invent the allowance (the brain targets the conscious).
            intent.PromoteToPlayer();
            TestRunner.True(!intent.TargetUnconsciousWhenOrdered, "Promotion granted the allowance.");
            intent.Begin(target, null, false, true, targetUnconsciousWhenOrdered: true);
            intent.Cancel();
            TestRunner.True(!intent.TargetUnconsciousWhenOrdered, "Cancellation retained the allowance.");
            intent.Begin(target, null, false, true);
            TestRunner.True(!intent.TargetUnconsciousWhenOrdered, "An order on a conscious target carried an earlier allowance.");
        }

        private static void RangedDispatchUnchanged()
        {
            var downedValid = UnifiedMountedStockAttackPolicy.IsValidTarget(true, true, Admits(false, false, false, true), true, true);
            // Real time: the rider shoots with its own Standard; the mount is not sent to melee for a ranged order.
            TestRunner.Equal(MountedStockAttackDecision.DispatchRider, UnifiedMountedStockAttackPolicy.DecideNext(
                true, downedValid, false, false, true, true, true, false), "The mounted ranged order on a downed enemy was not dispatched to the rider.");
            // Turn based, the rider's Standard spent: no mount substitution for a ranged order out of mount reach.
            TestRunner.Equal(MountedStockAttackDecision.CompleteTurnBasedIntent, UnifiedMountedStockAttackPolicy.DecideNext(
                true, downedValid, false, true, false, true, true, false), "A spent rider's ranged order was substituted with a mount attack.");
            // The same order on a target that died in the meantime is cancelled as before.
            var deadValid = UnifiedMountedStockAttackPolicy.IsValidTarget(true, true, Admits(false, true, false, true), true, true);
            TestRunner.Equal(MountedStockAttackDecision.CancelInvalidIntent, UnifiedMountedStockAttackPolicy.DecideNext(
                true, deadValid, false, false, true, true, true, false), "An order on a dead target was not cancelled.");
        }
    }
}
