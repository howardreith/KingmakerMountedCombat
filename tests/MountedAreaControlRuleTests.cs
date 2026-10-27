using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    internal static class MountedAreaControlRuleTests
    {
        private const string Control = MountedAreaControlRule.ControlRefusal;

        public static void Register(TestRunner runner)
        {
            runner.Run("capital arrival retires a companion pair the capital rule excludes", RetiresExcludedCompanionPair);
            runner.Run("capital arrival retires a pair whose only excluded unit is the mount", RetiresWhenOnlyMountExcluded);
            runner.Run("capital arrival never retires outside a capital", KeepsFailureOutsideCapital);
            runner.Run("capital arrival never retires for another refusal", KeepsFailureForAnotherRefusal);
            runner.Run("capital arrival never retires when something else would also refuse", KeepsFailureWhenOtherRefusalRemains);
            runner.Run("capital arrival never retires a main-character unit that lost control", KeepsFailureForUncontrollableMain);
            runner.Run("capital arrival never retires a controllable pair", KeepsControllablePair);
            runner.Run("pair candidate reports the exact control refusal", CandidateUsesExactControlRefusal);
        }

        private static void RetiresExcludedCompanionPair() =>
            TestRunner.True(MountedAreaControlRule.IsCapitalControlRetirement(true, Control, null, false, false, false, false),
                "A companion pair excluded by the native capital rule was not retired.");

        private static void RetiresWhenOnlyMountExcluded() =>
            TestRunner.True(MountedAreaControlRule.IsCapitalControlRetirement(true, Control, null, true, true, false, false),
                "A pair whose only excluded unit is a non-main mount was not retired.");

        private static void KeepsFailureOutsideCapital() =>
            TestRunner.True(!MountedAreaControlRule.IsCapitalControlRetirement(false, Control, null, false, false, false, false),
                "A control refusal outside a capital was retired instead of failing.");

        private static void KeepsFailureForAnotherRefusal() =>
            TestRunner.True(!MountedAreaControlRule.IsCapitalControlRetirement(true, "Both units must be alive and conscious.", null, false, false, false, false),
                "A non-control refusal in a capital was retired.");

        private static void KeepsFailureWhenOtherRefusalRemains() =>
            TestRunner.True(!MountedAreaControlRule.IsCapitalControlRetirement(true, Control, "Both units must be alive and conscious.", false, false, false, false),
                "A pair that would still refuse with control assumed was retired.");

        private static void KeepsFailureForUncontrollableMain() =>
            TestRunner.True(!MountedAreaControlRule.IsCapitalControlRetirement(true, Control, null, false, true, false, true),
                "A main-character pair that is not controllable was retired, but the capital rule admits it.");

        private static void KeepsControllablePair() =>
            TestRunner.True(!MountedAreaControlRule.IsCapitalControlRetirement(true, Control, null, true, false, true, false),
                "A controllable pair was retired.");

        private static void CandidateUsesExactControlRefusal()
        {
            var candidate = new MountedPairCandidate("rider", "mount") { RiderIsDirectlyControllable = false, MountIsDirectlyControllable = true };
            TestRunner.True(candidate.ValidateSavedRelationship() == Control, "The pair candidate control refusal differs from the capital rule's text.");
        }
    }
}
