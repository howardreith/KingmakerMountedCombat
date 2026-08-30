using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    internal static class MountedStockAttackPolicyTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("mounted stock attack requires an exact frame-bounded native request", RequiresExactNativeRequest);
            runner.Run("mounted stock attack sequences rider before mount", SequencesRiderBeforeMount);
            runner.Run("mounted ranged stock attack never pulls mount into melee", RangedDoesNotForceMountMelee);
            runner.Run("mounted stock attack waits persistently only in real time", WaitsPersistentlyOnlyInRealTime);
            runner.Run("mounted stock attack cancels invalid target exactly", CancelsInvalidTarget);
        }

        private static void RequiresExactNativeRequest()
        {
            TestRunner.True(
                UnifiedMountedStockAttackPolicy.IsExactObservedPlayerRequest(
                    true, true, true, true, true, true, 100, 99),
                "Exact native hostile-click request was not admitted.");
            TestRunner.True(
                !UnifiedMountedStockAttackPolicy.IsExactObservedPlayerRequest(
                    true, true, true, true, true, true, 102, 99),
                "Stale native request was admitted.");
            TestRunner.True(
                !UnifiedMountedStockAttackPolicy.IsExactObservedPlayerRequest(
                    true, true, false, true, true, true, 100, 100),
                "Non-principal selection was admitted.");
        }

        private static void SequencesRiderBeforeMount()
        {
            TestRunner.Equal(
                MountedStockAttackDecision.DispatchRider,
                UnifiedMountedStockAttackPolicy.DecideNext(
                    true, true, false, true, true, true, false, true),
                "Shared attack did not dispatch rider first.");
            TestRunner.Equal(
                MountedStockAttackDecision.DispatchMount,
                UnifiedMountedStockAttackPolicy.DecideNext(
                    true, true, false, true, false, true, false, true),
                "Shared attack did not dispatch available mount second.");
        }

        private static void RangedDoesNotForceMountMelee()
        {
            TestRunner.Equal(
                MountedStockAttackDecision.Wait,
                UnifiedMountedStockAttackPolicy.DecideNext(
                    true, true, false, false, false, true, true, false),
                "Ranged intent forced a ready mount to approach melee.");
            TestRunner.Equal(
                MountedStockAttackDecision.DispatchMount,
                UnifiedMountedStockAttackPolicy.DecideNext(
                    true, true, false, false, false, true, true, true),
                "Ranged intent rejected a mount already in legal melee.");
        }

        private static void WaitsPersistentlyOnlyInRealTime()
        {
            TestRunner.Equal(
                MountedStockAttackDecision.Wait,
                UnifiedMountedStockAttackPolicy.DecideNext(
                    true, true, false, false, false, false, false, true),
                "RT intent did not persist across native cooldown.");
            TestRunner.Equal(
                MountedStockAttackDecision.CompleteTurnBasedIntent,
                UnifiedMountedStockAttackPolicy.DecideNext(
                    true, true, false, true, false, false, false, true),
                "Exhausted TB intent persisted past its shared turn.");
        }

        private static void CancelsInvalidTarget()
        {
            TestRunner.Equal(
                MountedStockAttackDecision.CancelInvalidIntent,
                UnifiedMountedStockAttackPolicy.DecideNext(
                    true, false, false, false, true, true, false, true),
                "Invalid target retained attack intent.");
            TestRunner.True(
                !UnifiedMountedStockAttackPolicy.IsValidTarget(true, true, false, true, true),
                "Incapacitated target remained valid.");
        }
    }
}
