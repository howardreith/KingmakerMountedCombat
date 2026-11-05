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
            runner.Run("the ordinary Charge routes to the mounted Charge for the rider only", () =>
            {
                var id = MountedChargeSafetyPolicy.ChargeBlueprintId;
                TestRunner.Equal(MountedChargeRoute.Mounted,
                    MountedChargeSafetyPolicy.Route(RelationshipState.Mounted, true, true, id, true), "Mounted rider not routed.");
                TestRunner.Equal(MountedChargeRoute.Refused,
                    MountedChargeSafetyPolicy.Route(RelationshipState.Mounted, true, false, id, true), "Carried mount not refused.");
                TestRunner.Equal(MountedChargeRoute.Native,
                    MountedChargeSafetyPolicy.Route(RelationshipState.Unmounted, true, true, id, true), "Unmounted rider lost native Charge.");
                TestRunner.Equal(MountedChargeRoute.Native,
                    MountedChargeSafetyPolicy.Route(RelationshipState.Mounted, false, true, id, true), "Unrelated actor lost native Charge.");
                // Only the exact stock blueprint with the exact native component: a spell or ability that merely
                // resembles Charge, or carries Charge in its name, stays native.
                TestRunner.Equal(MountedChargeRoute.Native,
                    MountedChargeSafetyPolicy.Route(RelationshipState.Mounted, true, true, "4c349361d720e844e846ad8c19959b1e", true),
                    "Another ability was routed as Charge.");
                TestRunner.Equal(MountedChargeRoute.Native,
                    MountedChargeSafetyPolicy.Route(RelationshipState.Mounted, true, true, id, false),
                    "A Charge blueprint with replaced logic was routed.");
            });
            runner.Run("no Charge refusal tells the player to use a separate Mounted Charge", () =>
            {
                foreach (var text in new[] { MountedChargeSafetyPolicy.Feedback, MountedChargeSafetyPolicy.MountFeedback })
                    TestRunner.True(text.IndexOf("Mounted Charge", System.StringComparison.OrdinalIgnoreCase) < 0 &&
                        text.IndexOf("KMC.", System.StringComparison.Ordinal) < 0, "Dead-end or internal text: " + text);
            });
        }

        private static bool Reject(RelationshipState state, bool belongsToPair) =>
            MountedChargeSafetyPolicy.ShouldReject(state, belongsToPair, MountedChargeSafetyPolicy.ChargeBlueprintId, true);
    }
}
