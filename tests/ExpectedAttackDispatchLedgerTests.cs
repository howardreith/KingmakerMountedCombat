using KingmakerMountedCombat.Diagnostics;

namespace KingmakerMountedCombat.Tests
{
    internal static class ExpectedAttackDispatchLedgerTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("expected attack dispatch ledger admits sequential validated actions", AdmitsSequentialActions);
        }

        private static void AdmitsSequentialActions()
        {
            var ledger = new ExpectedAttackDispatchLedger();

            TestRunner.True(!ledger.Started && ledger.MarkCount == 0,
                "Fresh expected-dispatch ledger was already marked.");
            TestRunner.True(ledger.Mark() && ledger.Started && ledger.MarkCount == 1,
                "First expected dispatch was not recorded exactly once.");
            TestRunner.True(ledger.Mark() && ledger.Started && ledger.MarkCount == 2,
                "Second independently validated dispatch was rejected as a duplicate.");
        }
    }
}
