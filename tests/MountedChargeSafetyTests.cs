using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    internal static class MountedChargeSafetyTests
    {
        internal static void Register(TestRunner runner)
        {
            runner.Run("mounted native Charge rejects while unrelated and unmounted actors retain it", () =>
            {
                TestRunner.True(Reject(RelationshipState.Mounted, true), "Affected native Charge admitted.");
                TestRunner.True(!Reject(RelationshipState.Mounted, false), "Unrelated actor lost native Charge.");
                TestRunner.True(!Reject(RelationshipState.Unmounted, true), "Unmounted actor lost native Charge.");
            });
            runner.Run("Charge identity requires exact blueprint and native logic", () =>
            {
                TestRunner.True(!MountedChargeSafetyPolicy.ShouldReject(RelationshipState.Mounted, true,
                    "ordinary-attack-or-other-ability", true), "Another action was rejected.");
                TestRunner.True(!MountedChargeSafetyPolicy.ShouldReject(RelationshipState.Mounted, true,
                    MountedChargeSafetyPolicy.ChargeBlueprintId, false), "Unrelated ability logic was rejected.");
            });
            runner.Run("prepared Charge permission is reevaluated after relationship transitions", () =>
            {
                var state = RelationshipState.Unmounted;
                TestRunner.True(!Reject(state, true), "Unmounted preparation rejected.");
                state = RelationshipState.Mounted;
                TestRunner.True(Reject(state, true), "Earlier permission bypassed mounted execution restriction.");
                state = RelationshipState.Unmounted;
                TestRunner.True(!Reject(state, true), "Retired pair left stale rejection.");
            });
            runner.Run("Charge safety preserves a genuinely delivered native command", () =>
            {
                TestRunner.True(!MountedChargeSafetyPolicy.ShouldReject(RelationshipState.Mounted, true,
                    MountedChargeSafetyPolicy.ChargeBlueprintId, true, alreadyActed: true),
                    "Safety rejection would interrupt native cleanup after real expenditure.");
            });
        }

        private static bool Reject(RelationshipState state, bool belongsToPair) =>
            MountedChargeSafetyPolicy.ShouldReject(state, belongsToPair, MountedChargeSafetyPolicy.ChargeBlueprintId, true);
    }
}
