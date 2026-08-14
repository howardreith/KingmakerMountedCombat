using System;
using KingmakerMountedCombat.Diagnostics;

namespace KingmakerMountedCombat.Tests
{
    internal static class BoundaryFailureDrainTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("boundary failure finalizes immediately when loading is idle", IdleFailureIsReady);
            runner.Run("boundary failure remains pending throughout active loading", ActiveLoadRemainsPending);
            runner.Run("boundary failure becomes finalizable only after loading stops", LoadStopReleasesFailure);
            runner.Run("boundary failure preserves the first safety-significant cause", FirstFailureIsPreserved);
            runner.Run("boundary failure rejects an empty cause", EmptyFailureIsRejected);
            runner.Run("latched boundary failure forbids scenario advancement", LatchedFailureForbidsScenarioAdvancement);
        }

        private static void IdleFailureIsReady()
        {
            var drain = new BoundaryFailureDrain();
            drain.Request("timeout", false);

            TestRunner.Equal(BoundaryFailureDrainState.ReadyToFinalize, drain.State,
                "An idle failure was not immediately ready for safe finalization.");
            TestRunner.Equal("timeout", drain.Failure, "The failure cause was not retained.");
        }

        private static void ActiveLoadRemainsPending()
        {
            var drain = new BoundaryFailureDrain();
            drain.Request("authorization", true);

            TestRunner.Equal(BoundaryFailureDrainState.DrainingActiveLoad, drain.Observe(true),
                "An active loading pipeline released a pending failure.");
            TestRunner.Equal(BoundaryFailureDrainState.DrainingActiveLoad, drain.Observe(true),
                "Repeated active observations released a pending failure.");
        }

        private static void LoadStopReleasesFailure()
        {
            var drain = new BoundaryFailureDrain();
            drain.Request("exception", true);

            TestRunner.Equal(BoundaryFailureDrainState.ReadyToFinalize, drain.Observe(false),
                "A stopped loading pipeline did not release the failure for verification and cleanup.");
        }

        private static void FirstFailureIsPreserved()
        {
            var drain = new BoundaryFailureDrain();
            drain.Request("first", true);
            drain.Request("second", false);

            TestRunner.Equal("first", drain.Failure, "A later failure replaced the first safety-significant cause.");
            TestRunner.Equal(BoundaryFailureDrainState.DrainingActiveLoad, drain.State,
                "A later request bypassed the active-load drain.");
        }

        private static void EmptyFailureIsRejected()
        {
            var drain = new BoundaryFailureDrain();
            var threw = false;
            try
            {
                drain.Request(" ", false);
            }
            catch (ArgumentException)
            {
                threw = true;
            }

            TestRunner.True(threw, "An empty boundary failure cause was accepted.");
        }

        private static void LatchedFailureForbidsScenarioAdvancement()
        {
            var drain = new BoundaryFailureDrain();
            TestRunner.Equal(true, drain.MayAdvanceScenario,
                "An inactive failure drain unexpectedly blocked scenario advancement.");

            drain.Request("authorization", true);
            TestRunner.Equal(true, drain.IsLatched,
                "An active-load failure was not retained as a latched terminal request.");
            TestRunner.Equal(false, drain.MayAdvanceScenario,
                "A scenario could advance after an active-load failure was latched.");

            drain.Observe(false);
            TestRunner.Equal(BoundaryFailureDrainState.ReadyToFinalize, drain.State,
                "An idle pipeline did not make the latched failure finalizable.");
            TestRunner.Equal(false, drain.MayAdvanceScenario,
                "A scenario could advance after its latched failure became finalizable.");
        }
    }
}
