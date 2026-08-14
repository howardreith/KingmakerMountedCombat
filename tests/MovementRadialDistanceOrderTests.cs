using System;
using KingmakerMountedCombat.Diagnostics;

namespace KingmakerMountedCombat.Tests
{
    internal static class MovementRadialDistanceOrderTests
    {
        internal static void Register(TestRunner runner)
        {
            runner.Run("turn route radial distances are local first", TurnRouteDistancesAreLocalFirst);
            runner.Run("turn route radial distances reject invalid values", TurnRouteDistancesRejectInvalidValues);
        }

        private static void TurnRouteDistancesAreLocalFirst()
        {
            var distances = MovementRadialDistanceOrder.CreateLocalFirst(5.0f, 8.0f, 11.0f);
            TestRunner.Equal(3, distances.Length, "Local-first distance plan changed cardinality.");
            TestRunner.Equal(5.0f, distances[0], "Shortest radial candidate was not first.");
            TestRunner.Equal(8.0f, distances[1], "Middle radial candidate changed.");
            TestRunner.Equal(11.0f, distances[2], "Existing maximum radial candidate changed.");
        }

        private static void TurnRouteDistancesRejectInvalidValues()
        {
            AssertThrows(() => MovementRadialDistanceOrder.CreateLocalFirst(0.0f, 8.0f, 11.0f),
                "A non-positive minimum was accepted.");
            AssertThrows(() => MovementRadialDistanceOrder.CreateLocalFirst(8.0f, 5.0f, 11.0f),
                "An unordered distance plan was accepted.");
            AssertThrows(() => MovementRadialDistanceOrder.CreateLocalFirst(float.NaN, 8.0f, 11.0f),
                "NaN minimum was accepted.");
            AssertThrows(() => MovementRadialDistanceOrder.CreateLocalFirst(5.0f, float.PositiveInfinity, 11.0f),
                "Infinite middle distance was accepted.");
            AssertThrows(() => MovementRadialDistanceOrder.CreateLocalFirst(5.0f, 8.0f, float.NaN),
                "NaN maximum was accepted.");
        }

        private static void AssertThrows(Action action, string message)
        {
            try
            {
                action();
            }
            catch (ArgumentOutOfRangeException)
            {
                return;
            }

            throw new InvalidOperationException(message);
        }
    }
}
