using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    internal static class NativeMountedControlPolicyTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("native controls lease Mount only to an eligible unmounted owner", LeasesUnmountedMountOnly);
            runner.Run("native controls lease contextual pair actions while mounted", LeasesMountedPairActions);
            runner.Run("native controls preserve exact RT and TB primary caster ownership", PreservesPrimaryCasterOwnership);
            runner.Run("native controls project unified primary ownership to the rider", ProjectsUnifiedPrimaryOwnership);
            runner.Run("native controls lease unified actions only to the rider principal", LeasesUnifiedActionsToRider);
            runner.Run("native primary intent shell preparation is exact and pair local", PreparesOnlyExactMountedPrimaryShell);
            runner.Run("native controls explain separate-turn primary ownership", ExplainsWrongTurn);
            runner.Run("queued native control cleanup preserves active effects and foreign orders", PendingControlCleanup);
        }

        private static void PendingControlCleanup()
        {
            foreach (var kind in new[] { NativeMountedControlKind.MountCompanion, NativeMountedControlKind.Dismount })
            {
                TestRunner.True(NativeMountedControlPolicy.OwnsPendingControl(kind, true, false, false), "Exact queued control escaped cleanup.");
                TestRunner.True(!NativeMountedControlPolicy.OwnsPendingControl(kind, false, false, false), "Foreign caster was consumed.");
                TestRunner.True(!NativeMountedControlPolicy.OwnsPendingControl(kind, true, true, false), "Started native action was classified as pending.");
                TestRunner.True(!NativeMountedControlPolicy.OwnsPendingControl(kind, true, false, true), "Terminated request could be replayed.");
            }
            TestRunner.True(!NativeMountedControlPolicy.OwnsPendingControl(NativeMountedControlKind.RiderPrimary, true, false, false), "Primary action entered the relationship-control queue policy.");
        }

        private static void LeasesUnmountedMountOnly()
        {
            TestRunner.True(
                NativeMountedControlPolicy.ShouldLease(
                    NativeMountedControlKind.MountCompanion, true, true, false, false, false, false),
                "Eligible owner did not receive Mount Companion.");
            TestRunner.True(
                !NativeMountedControlPolicy.ShouldLease(
                    NativeMountedControlKind.MountCompanion, true, false, false, false, false, false),
                "Owner without a supported mount received Mount Companion.");
            TestRunner.True(
                !NativeMountedControlPolicy.ShouldLease(
                    NativeMountedControlKind.Dismount, true, true, false, false, false, false),
                "Unmounted owner received Dismount.");
            TestRunner.True(
                !NativeMountedControlPolicy.ShouldLease(
                    NativeMountedControlKind.MountCompanion, false, true, false, false, false, false),
                "Disabled feature leased a native control.");
        }

        private static void LeasesMountedPairActions()
        {
            TestRunner.True(
                NativeMountedControlPolicy.ShouldLease(
                    NativeMountedControlKind.Dismount, true, false, true, false, true, false),
                "Mounted rider did not receive Dismount.");
            TestRunner.True(
                !NativeMountedControlPolicy.ShouldLease(
                    NativeMountedControlKind.Dismount, true, false, true, false, false, true),
                "Mounted mount received rider-only Dismount.");
            foreach (var kind in new[]
            {
                NativeMountedControlKind.RiderPrimary,
                NativeMountedControlKind.MountPrimary
            })
            {
                TestRunner.True(
                    NativeMountedControlPolicy.ShouldLease(kind, true, false, true, false, true, false),
                    kind + " was absent from the rider native surface.");
                TestRunner.True(
                    NativeMountedControlPolicy.ShouldLease(kind, true, false, true, false, false, true),
                    kind + " was absent from the mount native surface.");
            }

            TestRunner.True(
                NativeMountedControlPolicy.ShouldLease(
                    NativeMountedControlKind.Dismount, true, false, false, true, true, false),
                "Faulted relationship did not preserve the rider cleanup control.");
            TestRunner.True(
                !NativeMountedControlPolicy.ShouldLease(
                    NativeMountedControlKind.RiderPrimary, true, false, false, true, true, false),
                "Faulted relationship exposed a combat control before cleanup.");
        }

        private static void PreservesPrimaryCasterOwnership()
        {
            TestRunner.True(
                NativeMountedControlPolicy.IsExpectedPrimaryCaster(
                    NativeMountedControlKind.RiderPrimary, false, true, false),
                "RT Rider primary rejected the rider caster.");
            TestRunner.True(
                NativeMountedControlPolicy.IsExpectedPrimaryCaster(
                    NativeMountedControlKind.MountPrimary, false, true, false),
                "RT Mount primary rejected the selected rider handoff.");
            TestRunner.True(
                NativeMountedControlPolicy.IsExpectedPrimaryCaster(
                    NativeMountedControlKind.RiderPrimary, true, true, false),
                "TB Rider primary rejected the rider turn surface.");
            TestRunner.True(
                NativeMountedControlPolicy.IsExpectedPrimaryCaster(
                    NativeMountedControlKind.MountPrimary, true, false, true),
                "TB Mount primary rejected the mount turn surface.");
            TestRunner.True(
                !NativeMountedControlPolicy.IsExpectedPrimaryCaster(
                    NativeMountedControlKind.MountPrimary, true, true, false),
                "TB Mount primary accepted the rider turn surface.");
            TestRunner.True(
                !NativeMountedControlPolicy.IsExpectedPrimaryCaster(
                    NativeMountedControlKind.RiderPrimary, true, false, true),
                "TB Rider primary accepted the mount turn surface.");
        }

        private static void ExplainsWrongTurn()
        {
            TestRunner.Equal(
                "Rider primary belongs to the rider's turn.",
                NativeMountedControlPolicy.WrongTurnReason(
                    NativeMountedControlKind.RiderPrimary, "Horse"),
                "Rider wrong-turn feedback changed.");
            TestRunner.Equal(
                "Horse primary belongs to the Horse's turn.",
                NativeMountedControlPolicy.WrongTurnReason(
                    NativeMountedControlKind.MountPrimary, "Horse"),
                "Horse wrong-turn feedback changed.");
        }

        private static void ProjectsUnifiedPrimaryOwnership()
        {
            TestRunner.True(
                NativeMountedControlPolicy.IsExpectedPrimaryCaster(
                    NativeMountedControlKind.MountPrimary, true, true, true, false),
                "Unified Mount primary rejected the rider principal.");
            TestRunner.True(
                !NativeMountedControlPolicy.IsExpectedPrimaryCaster(
                    NativeMountedControlKind.MountPrimary, true, true, false, true),
                "Unified Mount primary admitted the hidden mount surface.");
            TestRunner.Equal(
                "Horse primary belongs to the rider-led shared turn.",
                NativeMountedControlPolicy.WrongTurnReason(
                    NativeMountedControlKind.MountPrimary, "Horse", true),
                "Unified wrong-principal feedback changed.");
        }

        private static void LeasesUnifiedActionsToRider()
        {
            TestRunner.True(
                NativeMountedControlPolicy.ShouldLease(
                    NativeMountedControlKind.MountPrimary, true, true, false, true, false, true, false),
                "Unified rider principal did not receive Mount primary.");
            TestRunner.True(
                !NativeMountedControlPolicy.ShouldLease(
                    NativeMountedControlKind.MountPrimary, true, true, false, true, false, false, true),
                "Unified mount retained a duplicate primary surface.");
        }

        private static void PreparesOnlyExactMountedPrimaryShell()
        {
            foreach (var kind in new[]
            {
                NativeMountedControlKind.RiderPrimary,
                NativeMountedControlKind.MountPrimary
            })
            {
                TestRunner.True(
                    NativeMountedControlPolicy.ShouldPreparePrimaryIntentShell(
                        kind, true, true, true, true),
                    kind + " did not delegate pre-dispatch approach ownership to KMC.");
            }

            TestRunner.True(
                !NativeMountedControlPolicy.ShouldPreparePrimaryIntentShell(
                    NativeMountedControlKind.MountCompanion, true, true, true, true),
                "Mount Companion lost its native approach contract.");
            TestRunner.True(
                !NativeMountedControlPolicy.ShouldPreparePrimaryIntentShell(
                    NativeMountedControlKind.Dismount, true, true, true, true),
                "Dismount lost its native approach contract.");
            TestRunner.True(
                !NativeMountedControlPolicy.ShouldPreparePrimaryIntentShell(
                    NativeMountedControlKind.RiderPrimary, true, false, true, true),
                "An unmounted primary shell received the mounted bypass.");
            TestRunner.True(
                !NativeMountedControlPolicy.ShouldPreparePrimaryIntentShell(
                    NativeMountedControlKind.RiderPrimary, true, true, false, true),
                "A foreign ability received the KMC primary bypass.");
            TestRunner.True(
                !NativeMountedControlPolicy.ShouldPreparePrimaryIntentShell(
                    NativeMountedControlKind.RiderPrimary, true, true, true, false),
                "A non-rider caster received the KMC primary bypass.");
            TestRunner.True(
                !NativeMountedControlPolicy.ShouldPreparePrimaryIntentShell(
                    NativeMountedControlKind.RiderPrimary, false, true, true, true),
                "A disabled KMC surface received the primary bypass.");
        }
    }
}
