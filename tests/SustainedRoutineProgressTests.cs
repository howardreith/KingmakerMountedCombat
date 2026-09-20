using KingmakerMountedCombat.Diagnostics;

namespace KingmakerMountedCombat.Tests
{
    internal static class SustainedRoutineProgressTests
    {
        internal static void Register(TestRunner runner)
        {
            runner.Run("sustained rider repetition cannot renew a stalled mount deadline", StalledMount);
            runner.Run("sustained missing rider is not concealed by mount repetition", StalledRider);
            runner.Run("ranged measurement does not require an out-of-reach mount routine", RangedQuota);
            runner.Run("sustained case starts with independent progress", IndependentCases);
        }

        private static void StalledMount()
        {
            var progress = new SustainedRoutineProgress(true);
            TestRunner.True(!progress.Observe(0, 0), "Idle observation renewed the deadline.");
            TestRunner.True(progress.Observe(3, 2), "Actual completed routines were not progress.");
            for (var rider = 3; rider <= 19; rider++)
                TestRunner.True(!progress.Observe(rider, 2), "Extra rider routine concealed the missing Horse routine.");
            TestRunner.True(progress.Observe(19, 3), "The third Horse routine was not recognized as progress.");
            TestRunner.True(!progress.Observe(19, 3), "Duplicate observation renewed the deadline.");
        }

        private static void StalledRider()
        {
            var progress = new SustainedRoutineProgress(true);
            TestRunner.True(progress.Observe(2, 3), "Initial actor work was not observed.");
            TestRunner.True(!progress.Observe(2, 19), "Extra Horse routines concealed a missing rider routine.");
            TestRunner.True(progress.Observe(3, 19), "The third rider routine was not recognized.");
        }

        private static void RangedQuota()
        {
            var progress = new SustainedRoutineProgress(false);
            TestRunner.True(!progress.Observe(0, 3), "Ranged progress depended on mount attacks.");
            for (var rider = 1; rider <= 3; rider++)
                TestRunner.True(progress.Observe(rider, 3), "A required ranged routine did not renew the deadline.");
            TestRunner.True(!progress.Observe(4, 4), "Completed ranged quota kept renewing the deadline.");
        }

        private static void IndependentCases()
        {
            var held = new SustainedRoutineProgress(true);
            held.Observe(3, 3);
            var repeated = new SustainedRoutineProgress(true);
            TestRunner.True(repeated.Observe(1, 0), "Held-order progress leaked into the repeated-input case.");
        }
    }
}
