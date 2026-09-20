using System;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    internal static class OptionalPublicPropertyReaderTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run(
                "optional property telemetry resolves inherited ambiguous names without faulting",
                ResolvesInheritedAmbiguousNamesWithoutFaulting);
        }

        private static void ResolvesInheritedAmbiguousNamesWithoutFaulting()
        {
            var ambiguous = new DerivedWeaponObservation();
            TestRunner.Equal(
                "derived-blueprint",
                OptionalPublicPropertyReader.Read(ambiguous, "Blueprint"),
                "Optional telemetry did not prefer the most-derived public declaration.");

            var throwing = new ThrowingDerivedWeaponObservation();
            TestRunner.Equal(
                "base-blueprint",
                OptionalPublicPropertyReader.Read(throwing, "Blueprint"),
                "Optional telemetry did not survive a throwing most-derived getter.");
            TestRunner.Equal(
                null,
                OptionalPublicPropertyReader.Read(ambiguous, "Missing"),
                "Optional telemetry invented a missing property value.");
            TestRunner.Equal(
                null,
                OptionalPublicPropertyReader.Read(null, "Blueprint"),
                "Optional telemetry did not accept an absent foreign object.");
        }

        private class BaseWeaponObservation
        {
            public object Blueprint => "base-blueprint";
        }

        private sealed class DerivedWeaponObservation : BaseWeaponObservation
        {
            public new string Blueprint => "derived-blueprint";
        }

        private sealed class ThrowingDerivedWeaponObservation : BaseWeaponObservation
        {
            public new string Blueprint
            {
                get { throw new InvalidOperationException("synthetic optional getter failure"); }
            }
        }
    }
}
