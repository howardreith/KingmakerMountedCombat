using KingmakerMountedCombat.Domain;
namespace KingmakerMountedCombat.Tests
{
    internal static class MountedRangedRoutineCompletionTests
    {
        internal static void Register(TestRunner runner)
        {
            runner.Run("native RT mixed-range terminal preserves only a delivered ranged repeat order", () =>
            {
                TestRunner.True(Decide(), "Three native bow deliveries followed by an ineligible melee tail lost the repeat order.");
                TestRunner.True(!Decide(completed: 0), "Unacted interrupted windup became a repeatable completion.");
                TestRunner.True(!Decide(completed: 4), "A complete plan was mislabeled as a missing melee tail.");
                TestRunner.True(!Decide(completed: 5), "An impossible delivery count was admitted.");
            });
            runner.Run("Stop retarget and life invalidation never reuse an earlier native range rejection", () =>
            {
                TestRunner.True(!Decide(nativeTick: false), "A cancellation outside native sequence Tick retained intent.");
                TestRunner.True(!Decide(targetValid: false), "Dead, removed or replaced target retained intent.");
                TestRunner.True(!Decide(interrupted: false), "A fault or unrelated terminal was retried.");
            });
            runner.Run("mixed-range continuation preserves TB Primary and every native weapon reach", () =>
            {
                TestRunner.True(!Decide(tb: true), "TB costs or completion changed.");
                TestRunner.True(!Decide(ordinary: false), "Explicit Primary, mount or melee command acquired automatic repetition.");
                TestRunner.True(!Decide(prefixRanged: false), "A melee prefix acquired the ranged exception.");
                TestRunner.True(!Decide(ineligibleMeleeTail: false), "An eligible remaining attack was skipped.");
                TestRunner.True(!Decide(rangedInRange: false), "Target beyond bow reach or behind obstruction was retried.");
            });
        }
        private static bool Decide(bool tb = false, bool ordinary = true, bool nativeTick = true,
            bool interrupted = true, bool targetValid = true, int completed = 3, bool prefixRanged = true,
            bool ineligibleMeleeTail = true, bool rangedInRange = true) =>
            MountedRangedRoutineCompletionPolicy.CanRetainIntent(tb, ordinary, nativeTick, interrupted,
                targetValid, 4, completed, prefixRanged, ineligibleMeleeTail, rangedInRange);
    }
}