using System;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    internal static class MountedChargeConditionOwnershipTests
    {
        private static void Native(MountedChargeConditionOwnership owner, bool add, ref int counter, bool throwsAfter = false)
        {
            var operation = owner.Begin(add, counter);
            var returned = false;
            try
            {
                counter += add ? 1 : -1;
                owner.Observe(operation, counter);
                if (throwsAfter) throw new InvalidOperationException("native callback after mutation");
                returned = true;
            }
            finally { owner.End(operation, returned); }
        }

        internal static void Register(TestRunner runner)
        {
            runner.Run("charge condition releases only its contribution alongside changing foreign owners", () =>
            {
                var owner = new MountedChargeConditionOwnership(); var counter = 2;
                Native(owner, true, ref counter);
                counter += 3; // Three unrelated effects acquire native contributions.
                Native(owner, false, ref counter);
                TestRunner.True(counter == 5 && owner.Drained && owner.Additions == 1 && owner.Removals == 1,
                    "Cleanup required the intake total or consumed a foreign contribution.");
            });
            runner.Run("charge condition observes removal before a later throwing native callback", () =>
            {
                var owner = new MountedChargeConditionOwnership(); var counter = 1;
                Native(owner, true, ref counter);
                try { Native(owner, false, ref counter, true); } catch (InvalidOperationException) { }
                TestRunner.True(counter == 1 && owner.Drained && owner.NativeExceptions == 1,
                    "Known native decrement was lost and could be replayed against the foreign owner.");
            });
            runner.Run("charge condition missing marker retains debt despite matching final total", () =>
            {
                var owner = new MountedChargeConditionOwnership(); var counter = 2;
                Native(owner, true, ref counter);
                var operation = owner.Begin(false, counter); counter--;
                owner.End(operation, true);
                TestRunner.True(!owner.Drained && owner.Contributions == 1 && owner.Fault != null && counter == 2,
                    "A final shared count was substituted for exact mutation evidence.");
            });
            runner.Run("charge condition swallowed failure before decrement keeps exact owner", () =>
            {
                var owner = new MountedChargeConditionOwnership(); var counter = 0;
                Native(owner, true, ref counter);
                var operation = owner.Begin(false, counter); owner.End(operation, false);
                TestRunner.True(!owner.Drained && counter == 1 && owner.Contributions == 1,
                    "A swallowed native callback failure erased live condition ownership.");
            });
            runner.Run("charge condition duplicate entity-created acquisition cannot silently drain", () =>
            {
                var owner = new MountedChargeConditionOwnership(); var counter = 0;
                Native(owner, true, ref counter); Native(owner, true, ref counter); Native(owner, false, ref counter);
                TestRunner.True(!owner.Drained && owner.Contributions == 1 && counter == 1,
                    "A second native acquisition disappeared from the ledger.");
                Native(owner, false, ref counter);
                TestRunner.True(owner.Drained && counter == 0, "Two exact observed native removals did not drain two acquisitions.");
            });
            runner.Run("charge condition overflow and duplicate marker fail closed", () =>
            {
                var owner = new MountedChargeConditionOwnership();
                var operation = owner.Begin(true, 127); owner.Observe(operation, -128); owner.End(operation, true);
                TestRunner.True(!owner.Drained && owner.Fault != null, "Counter overflow became a lawful acquisition.");
                owner = new MountedChargeConditionOwnership(); operation = owner.Begin(true, 0);
                owner.Observe(operation, 1); owner.Observe(operation, 1); owner.End(operation, true);
                TestRunner.True(!owner.Drained && owner.Additions == 1, "Duplicate mutation marker changed owned balance.");
            });
            runner.Run("charge condition reentrant operation cannot steal a completion", () =>
            {
                var owner = new MountedChargeConditionOwnership(); var outer = owner.Begin(true, 0);
                var inner = owner.Begin(true, 0); owner.Observe(inner, 1); owner.End(inner, true); owner.End(outer, true);
                TestRunner.True(!owner.Drained && owner.Fault != null, "Reentrant component operation erased ambiguous ownership.");
            });
        }
    }
}
