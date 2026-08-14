using KingmakerMountedCombat.Diagnostics;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    internal static class MovementNavigationBoundaryPolicyTests
    {
        internal static void Register(TestRunner runner)
        {
            runner.Run(
                "movement boundary aborts proven external combat immediately",
                ProvenExternalCombatAbortsImmediately);
            runner.Run(
                "movement boundary preserves unmounted doorway control",
                UnmountedDoorwayControlRemainsValid);
            runner.Run(
                "movement boundary fails closed on unexplained relationship loss",
                UnexplainedRelationshipLossFailsClosed);
            runner.Run(
                "combat selection ownership requires exact cleanup-boundary restoration",
                CombatSelectionOwnershipRequiresExactRestoration);
        }

        private static void ProvenExternalCombatAbortsImmediately()
        {
            var fromLiveCombat = MovementNavigationBoundaryPolicy.Classify(
                true,
                true,
                true,
                false,
                false,
                null);
            TestRunner.Equal(
                MovementNavigationBoundaryAction.AbortExternalCombat,
                fromLiveCombat,
                "Live pair combat did not abort expected-mounted navigation before its movement deadline.");

            var fromExactCleanup = MovementNavigationBoundaryPolicy.Classify(
                true,
                false,
                false,
                false,
                true,
                CleanupTrigger.CombatStarted);
            TestRunner.Equal(
                MovementNavigationBoundaryAction.AbortExternalCombat,
                fromExactCleanup,
                "An exact successful CombatStarted cleanup was not classified as external combat.");
            TestRunner.Equal(
                true,
                MovementNavigationBoundaryPolicy.SuppressesRemainingOutOfCombatRows(fromExactCleanup),
                "A proven combat invalidation did not suppress the remaining out-of-combat rows after cleanup.");
        }

        private static void UnmountedDoorwayControlRemainsValid()
        {
            var action = MovementNavigationBoundaryPolicy.Classify(
                false,
                false,
                false,
                false,
                false,
                null);
            TestRunner.Equal(
                MovementNavigationBoundaryAction.Continue,
                action,
                "The intentional unmounted doorway control was rejected as relationship loss.");
        }

        private static void UnexplainedRelationshipLossFailsClosed()
        {
            var action = MovementNavigationBoundaryPolicy.Classify(
                true,
                false,
                false,
                false,
                false,
                CleanupTrigger.CombatStarted);
            TestRunner.Equal(
                MovementNavigationBoundaryAction.AbortUnexpectedRelationshipLoss,
                action,
                "A failed or stale combat transition was accepted as proven external combat.");
            TestRunner.Equal(
                false,
                MovementNavigationBoundaryPolicy.SuppressesRemainingOutOfCombatRows(action),
                "An unproven relationship loss was mislabeled as the exact external-combat suite boundary.");
        }

        private static void CombatSelectionOwnershipRequiresExactRestoration()
        {
            TestRunner.Equal(
                true,
                MovementNavigationBoundaryPolicy.IsCombatControllerSelectionRewrite(
                    MovementNavigationBoundaryAction.AbortExternalCombat,
                    true,
                    false),
                "A next-frame selection rewrite after exact combat cleanup was not externally classified.");
            TestRunner.Equal(
                false,
                MovementNavigationBoundaryPolicy.IsCombatControllerSelectionRewrite(
                    MovementNavigationBoundaryAction.AbortExternalCombat,
                    false,
                    false),
                "Combat excused a selection snapshot that KMC never restored at its cleanup boundary.");
            TestRunner.Equal(
                false,
                MovementNavigationBoundaryPolicy.IsCombatControllerSelectionRewrite(
                    MovementNavigationBoundaryAction.AbortUnexpectedRelationshipLoss,
                    true,
                    false),
                "An unexplained relationship loss excused next-frame selection residue.");
        }
    }
}
