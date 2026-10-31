using System;
using KingmakerMountedCombat.Diagnostics;

namespace KingmakerMountedCombat.Tests
{
    // Sprint 2 MOVE-01 measurement: path and time must cover the same samples.
    internal static class SpeedLegSamplerTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("speed leg sampler measures constant-speed segments exactly, first segment included", ConstantSpeedIsExact);
            runner.Run("speed leg sampler reports the v1 unaligned interval as biased upward", UnalignedIntervalIsBiased);
            runner.Run("speed leg sampler ignores jitter below the displacement epsilon and rejects bad samples", EpsilonAndGuards);
        }

        private static void ConstantSpeedIsExact()
        {
            var sampler = new SpeedLegSampler();
            // At rest for two samples, then 2 m per second for three samples, then at rest.
            sampler.Observe(0d, 0d, 0d);
            sampler.Observe(1d, 0d, 0d);
            sampler.Observe(2d, 0d, 2d);
            sampler.Observe(3d, 0d, 4d);
            sampler.Observe(4d, 0d, 6d);
            sampler.Observe(5d, 0d, 6d);
            TestRunner.True(sampler.Moved, "Motion was not observed.");
            TestRunner.True(Math.Abs(sampler.PathLength - 6d) < 1e-9, "Path length is not 6 m.");
            TestRunner.True(Math.Abs(sampler.FirstMotionSeconds.Value - 1d) < 1e-9, "The motion interval did not open at the last at-rest sample.");
            TestRunner.True(Math.Abs(sampler.LastMotionSeconds.Value - 4d) < 1e-9, "The motion interval did not close at the last moving sample.");
            TestRunner.True(Math.Abs(sampler.MotionSeconds - 3d) < 1e-9, "Aligned motion time is not 3 s.");
            TestRunner.True(Math.Abs(sampler.EffectiveSpeed - 2d) < 1e-9, "Aligned speed is not the constant 2 m/s.");
        }

        private static void UnalignedIntervalIsBiased()
        {
            var sampler = new SpeedLegSampler();
            sampler.Observe(0d, 0d, 0d);
            sampler.Observe(1d, 2d, 0d);
            sampler.Observe(2d, 4d, 0d);
            sampler.Observe(3d, 6d, 0d);
            TestRunner.True(Math.Abs(sampler.EffectiveSpeed - 2d) < 1e-9, "Aligned speed is not 2 m/s when motion starts at the first interval.");
            TestRunner.True(Math.Abs(sampler.UnalignedMotionSeconds - 2d) < 1e-9, "The v1 interval did not omit the first segment.");
            TestRunner.True(Math.Abs(sampler.UnalignedEffectiveSpeed - 3d) < 1e-9, "The v1 figure is not the biased 3 m/s.");
            TestRunner.True(sampler.UnalignedEffectiveSpeed > sampler.EffectiveSpeed, "The v1 bias is not upward.");
        }

        private static void EpsilonAndGuards()
        {
            var sampler = new SpeedLegSampler();
            sampler.Observe(0d, 0d, 0d);
            sampler.Observe(1d, 0.0005d, 0d);
            TestRunner.True(!sampler.Moved && sampler.PathLength == 0d && sampler.EffectiveSpeed == 0d, "Sub-epsilon jitter counted as motion.");
            var threw = false;
            try { sampler.Observe(0.5d, 1d, 1d); } catch (ArgumentException) { threw = true; }
            TestRunner.True(threw, "A sample going back in time was accepted.");
            threw = false;
            try { sampler.Observe(2d, double.NaN, 0d); } catch (ArgumentException) { threw = true; }
            TestRunner.True(threw, "A non-finite position was accepted.");
        }
    }
}
