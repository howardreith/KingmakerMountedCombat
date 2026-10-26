using System.Linq;
using KingmakerMountedCombat.Diagnostics;

namespace KingmakerMountedCombat.Tests
{
    // F3 r07/r08: the independent Horse's native step admits only a fresh completed native preview that reaches its
    // destination and lies outside the click's native 0.3 m approach radius.
    internal static class NativeGroundStepDestinationPolicyTests
    {
        internal static void Register(TestRunner runner)
        {
            runner.Run("independent Horse step refuses F3 r07's preview ending where the Horse stands", RefusesPreviewAtStart);
            runner.Run("independent Horse step refuses a stale or incomplete native preview", RefusesStaleOrIncompletePreview);
            runner.Run("independent Horse step admits F2's reaching preview and the approach bound", AdmitsReachingPreview);
            runner.Run("independent Horse step tries the target bearing first and every bearing once", BearingOrder);
        }

        private static void RefusesPreviewAtStart()
        {
            // F3: shouldApproach=false on the first eligibility frame, so the consumed path ended within 0.3 m of the Horse.
            TestRunner.True(!NativeGroundStepDestinationPolicy.Admits(true, true, 0.5, 0.0), "A preview ending at the Horse was admitted.");
            TestRunner.True(!NativeGroundStepDestinationPolicy.Admits(true, true, 0.0, 0.29), "A preview inside the native approach radius was admitted.");
            TestRunner.True(!NativeGroundStepDestinationPolicy.Admits(true, true, 0.1, 0.39), "A preview leaving less than the declared native approach was admitted.");
            TestRunner.Equal("preview-ends-away-from-destination", NativeGroundStepDestinationPolicy.Reason(true, true, 0.5, 0.0), "Preview at the Horse was misnamed.");
            TestRunner.Equal("preview-ends-inside-approach-radius", NativeGroundStepDestinationPolicy.Reason(true, true, 0.0, 0.2), "Short preview was misnamed.");
        }

        private static void RefusesStaleOrIncompletePreview()
        {
            TestRunner.True(!NativeGroundStepDestinationPolicy.Admits(false, true, 0.0, 0.5), "A stale preview was admitted.");
            TestRunner.True(!NativeGroundStepDestinationPolicy.Admits(true, false, 0.0, 0.5), "An incomplete preview was admitted.");
            TestRunner.True(!NativeGroundStepDestinationPolicy.Admits(true, true, double.NaN, 0.5), "A nonnumeric residual was admitted.");
            TestRunner.True(!NativeGroundStepDestinationPolicy.Admits(true, true, 0.0, double.PositiveInfinity), "A nonnumeric displacement was admitted.");
            TestRunner.Equal("stale-native-preview", NativeGroundStepDestinationPolicy.Reason(false, true, 0.0, 0.5), "Stale preview was misnamed.");
            TestRunner.Equal("no-completed-native-preview", NativeGroundStepDestinationPolicy.Reason(true, false, 0.0, 0.5), "Incomplete preview was misnamed.");
        }

        private static void AdmitsReachingPreview()
        {
            // F2 PASS: the same 0.5 m click moved 0.5285 m natively.
            TestRunner.True(NativeGroundStepDestinationPolicy.Admits(true, true, 0.0, 0.5), "A fresh preview reaching its 0.5 m destination was refused.");
            TestRunner.True(NativeGroundStepDestinationPolicy.Admits(true, true, 0.3, 0.4001), "The native approach bound was refused.");
            TestRunner.Equal("admitted", NativeGroundStepDestinationPolicy.Reason(true, true, 0.0, 0.5), "Admitted preview was misnamed.");
            TestRunner.Equal(0.3f, NativeGroundStepDestinationPolicy.NativeGroundApproachRadius, "The native ground click approach radius changed.");
            TestRunner.Equal(0.5f, NativeGroundStepDestinationPolicy.StepDistance, "The declared step distance changed.");
        }

        private static void BearingOrder()
        {
            var offsets = NativeGroundStepDestinationPolicy.BearingOffsets();
            TestRunner.Equal(16, offsets.Length, "Bearing candidate count changed.");
            TestRunner.Equal(0f, offsets[0], "The target bearing is not first.");
            TestRunner.Equal(22.5f, offsets[1], "The nearest clockwise bearing is not second.");
            TestRunner.Equal(-22.5f, offsets[2], "The nearest counter-clockwise bearing is not third.");
            TestRunner.Equal(180f, offsets[15], "The opposite bearing is not last.");
            TestRunner.Equal(16, offsets.Select(value => ((value % 360f) + 360f) % 360f).Distinct().Count(), "A bearing repeats.");
            for (var i = 1; i < offsets.Length; i++)
                TestRunner.True(System.Math.Abs(offsets[i]) >= System.Math.Abs(offsets[i - 1]), "Bearings are not ordered by deviation from the target.");
        }
    }
}
