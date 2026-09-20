using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    internal static class MountedRiderGroundingPolicyTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("grounding policy suppresses only the exact active mounted rider view", SuppressesOnlyExactActiveMountedRiderView);
        }

        private static void SuppressesOnlyExactActiveMountedRiderView()
        {
            var riderView = new object();
            var otherView = new object();

            TestRunner.True(
                MountedRiderGroundingPolicy.ShouldSuppress(RelationshipState.Mounted, true, riderView, riderView),
                "Exact active mounted rider view was not suppressed.");
            TestRunner.Equal(
                false,
                MountedRiderGroundingPolicy.ShouldSuppress(RelationshipState.Unmounted, true, riderView, riderView),
                "Unmounted rider view was suppressed.");
            TestRunner.Equal(
                false,
                MountedRiderGroundingPolicy.ShouldSuppress(RelationshipState.Mounted, false, riderView, riderView),
                "Rider view without an active pair was suppressed.");
            TestRunner.Equal(
                false,
                MountedRiderGroundingPolicy.ShouldSuppress(RelationshipState.Mounted, true, riderView, otherView),
                "Non-rider view was suppressed.");
            TestRunner.Equal(
                false,
                MountedRiderGroundingPolicy.ShouldSuppress<object>(RelationshipState.Mounted, true, riderView, null),
                "Null candidate view was suppressed.");
        }
    }
}
