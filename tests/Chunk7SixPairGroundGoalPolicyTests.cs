using System;
using System.Collections.Generic;
using KingmakerMountedCombat.Diagnostics;

namespace KingmakerMountedCombat.Tests
{
    // F3 r39: the pure six-pair ground goal search, hostile wander-envelope clearance and 2.1.7b roaming fallback.
    internal static class Chunk7SixPairGroundGoalPolicyTests
    {
        // Hostile 86c73c36's random roaming origin from the sixth-Horse save and F3 r39's eastern pair actor position.
        private const double OriginX = 12.8022385, OriginZ = 12.7261009;

        internal static void Register(TestRunner runner)
        {
            runner.Run("six-pair ground goal search keeps the declared +x 2 m route when it is clear", SearchKeepsDeclaredRoute);
            runner.Run("six-pair ground goal search takes the first admitted ring candidate and names each first refusal", SearchFirstAdmitted);
            runner.Run("six-pair ground goal search refuses as uncovered after every bounded candidate", SearchExhausted);
            runner.Run("six-pair ground goal spacing refuses a goal inside 1.75 m of another pair", Spacing);
            runner.Run("six-pair hostile clearance refuses F3 r39's eastern point inside the wander envelope", HostileRefusesF3Point);
            runner.Run("six-pair hostile clearance admits a goal clear of envelope, position and dismount reach", HostileAdmitsClearGoal);
            runner.Run("six-pair hostile clearance checks extended vision only after the wander clearance, per hostile", HostileExtendedOrder);
            runner.Run("six-pair wander envelope keeps standing, unreadable and random semantics", EnvelopeKinds);
            runner.Run("six-pair waypoint envelope includes the native random fallback disk only when a waypoint may yield none", WaypointFallbackDisk);
            runner.Run("six-pair waypoint fallback follows 2.1.7b AdvancePoint's two null directions", WaypointFallbackRule);
        }

        private static void SearchKeepsDeclaredRoute()
        {
            var refusals = new List<string>();
            TestRunner.True(Chunk7SixPairGroundGoalPolicy.Search((d, b) => null, (r, d, b) => refusals.Add(r), out var distance, out var bearing, out var tried),
                "A clear declared route was refused.");
            TestRunner.Equal(2f, distance, "The declared route distance changed.");
            TestRunner.Equal(0f, bearing, "The declared route bearing changed.");
            TestRunner.Equal(1, tried, "The declared route was not the first candidate.");
            TestRunner.Equal(0, refusals.Count, "A clear declared route recorded a refusal.");
        }

        private static void SearchFirstAdmitted()
        {
            var seen = new List<string>(); var calls = 0;
            Func<float, float, string> refusal = (d, b) =>
            {
                calls++;
                if (d < 4f) return Chunk7SixPairGroundGoalPolicy.HostileWanderVision;
                if (b != 45f) return "navmesh-projection";
                return null;
            };
            TestRunner.True(Chunk7SixPairGroundGoalPolicy.Search(refusal, (r, d, b) => seen.Add(r + "@" + d + "/" + b), out var distance, out var bearing, out var tried),
                "An admitted ring candidate was refused.");
            TestRunner.Equal(4f, distance, "The first admitted distance changed.");
            TestRunner.Equal(45f, bearing, "The first admitted bearing changed.");
            // Bearings 0, 22.5, -22.5, 45: the fourth candidate of the third ring.
            TestRunner.Equal(2 * 16 + 4, tried, "The candidate count is not the nearest-first ring position.");
            TestRunner.Equal(tried, calls, "The search evaluated a candidate after the admitted one.");
            TestRunner.Equal("hostile-wander-vision@2/0|navmesh-projection@4/0", string.Join("|", seen), "Each reason is not recorded once at its first candidate.");
        }

        private static void SearchExhausted()
        {
            var seen = 0;
            TestRunner.True(!Chunk7SixPairGroundGoalPolicy.Search((d, b) => "unit-footprint", (r, d, b) => seen++, out var distance, out var bearing, out var tried),
                "A fully refused search chose a goal.");
            TestRunner.Equal(9 * 16, tried, "The bounded search did not try every ring candidate once.");
            TestRunner.True(float.IsNaN(distance) && float.IsNaN(bearing), "A refused search reported a goal.");
            TestRunner.Equal(1, seen, "A repeated refusal reason was recorded more than once.");
        }

        private static void Spacing()
        {
            var others = new List<double[]> { new[] { 0d, 0d }, new[] { 10d, 10d } };
            TestRunner.True(!Chunk7SixPairGroundGoalPolicy.SpacingHolds(1.74, 0, others), "A goal inside the pair spacing was admitted.");
            TestRunner.True(Chunk7SixPairGroundGoalPolicy.SpacingHolds(1.75, 0, others), "A goal at the pair spacing was refused.");
            TestRunner.True(!Chunk7SixPairGroundGoalPolicy.SpacingHolds(10, 11.7, others), "A later pair's mount was not checked.");
        }

        private static SixHostile F3Hostile() =>
            new SixHostile(SixWanderEnvelope.RandomDisk(16.67, 12.62, 17.69, 9.07, OriginX, OriginZ, 7), 16.67, 12.62, 8.5f);

        private static void HostileRefusesF3Point()
        {
            // Current position 19.35 m away passed the before-stress check; the envelope does not.
            var reason = Chunk7SixPairGroundGoalPolicy.HostileRefusal(3.90, 1.32, new[] { F3Hostile() }, h => false, out var clearance);
            TestRunner.Equal(Chunk7SixPairGroundGoalPolicy.HostileWanderVision, reason, "F3 r39's eastern point was admitted.");
            var disk = Chunk7SixPairCombatEntry.Distance2D(3.90, 1.32, OriginX, OriginZ) - 8;
            TestRunner.True(Math.Abs(clearance - (disk - 4)) < 1e-9, "The refused clearance is not the envelope less the dismount reach.");
        }

        private static void HostileAdmitsClearGoal()
        {
            var none = Chunk7SixPairGroundGoalPolicy.HostileRefusal(0, 0, new SixHostile[0], h => false, out var empty);
            TestRunner.True(none == null && empty == double.MaxValue, "A goal without hostiles was refused or measured.");
            var reason = Chunk7SixPairGroundGoalPolicy.HostileRefusal(-14, -12, new[] { F3Hostile() }, h => false, out var clearance);
            TestRunner.True(reason == null, "A goal clear of the wander envelope was refused.");
            var expected = Chunk7SixPairCombatEntry.Distance2D(-14, -12, OriginX, OriginZ) - 8 - 4;
            TestRunner.True(Math.Abs(clearance - expected) < 1e-9 && clearance >= 8.5 + 3, "The admitted clearance is not the envelope less the dismount reach.");
            // The same goal against a hostile standing nearer than its envelope is measured from its current position.
            var near = new SixHostile(SixWanderEnvelope.Standing(false, -14, -24), -14, -24, 8.5f);
            TestRunner.Equal(Chunk7SixPairGroundGoalPolicy.HostileWanderVision,
                Chunk7SixPairGroundGoalPolicy.HostileRefusal(-14, -12, new[] { F3Hostile(), near }, h => false, out var both), "A stationary hostile inside vision plus reach was admitted.");
            TestRunner.True(Math.Abs(both - (12 - 4)) < 1e-9, "The clearance is not the smallest hostile reach.");
        }

        private static void HostileExtendedOrder()
        {
            var probed = new List<int>();
            var far = new SixHostile(SixWanderEnvelope.Standing(false, 100, 100), 100, 100, 8.5f);
            var reason = Chunk7SixPairGroundGoalPolicy.HostileRefusal(0, 0, new[] { far, far }, h => { probed.Add(h); return h == 1; }, out _);
            TestRunner.Equal(Chunk7SixPairGroundGoalPolicy.HostileExtendedVision, reason, "An extended vision area covering the goal was admitted.");
            TestRunner.Equal("0,1", string.Join(",", probed), "Extended vision was not checked per hostile in order.");
            probed.Clear();
            var close = new SixHostile(SixWanderEnvelope.Standing(false, 1, 0), 1, 0, 8.5f);
            Chunk7SixPairGroundGoalPolicy.HostileRefusal(0, 0, new[] { close }, h => { probed.Add(h); return true; }, out _);
            TestRunner.Equal(0, probed.Count, "Extended vision was checked for a hostile already refused by its wander clearance.");
        }

        private static void EnvelopeKinds()
        {
            TestRunner.Equal(5d, SixWanderEnvelope.Standing(false, 3, 4).Distance2D(0, 0), "A stationary unit is not measured where it stands.");
            TestRunner.Equal("roaming-disabled", SixWanderEnvelope.Standing(true, 3, 4).Kind, "A disabled roaming part is misnamed.");
            TestRunner.Equal(0d, SixWanderEnvelope.Unreadable(100, 100).Distance2D(0, 0), "An unreadable waypoint graph produced a clearance.");
            TestRunner.Equal(0d, SixWanderEnvelope.RandomDisk(100, 100, 100, 100, 50, 50, double.NaN).Distance2D(0, 0), "An unreadable roaming radius produced a clearance.");
            // Disk (origin 20,0 radius 5): 20 - 5 - 1 = 14; leg from (0,30) toward (0,10): 10 - 1 = 9.
            var random = SixWanderEnvelope.RandomDisk(0, 30, 0, 10, 20, 0, 5);
            TestRunner.True(Math.Abs(random.Distance2D(0, 0) - 9) < 1e-12, "The random envelope omitted its leg toward NextPoint.");
            TestRunner.True(Math.Abs(SixWanderEnvelope.RandomDisk(0, 30, 0, 30, 20, 0, 5).Distance2D(0, 0) - 14) < 1e-12, "The random envelope is not its native disk plus approach.");
        }

        private static void WaypointFallbackDisk()
        {
            var nodes = new List<double[]> { new[] { 50d, 0d }, new[] { 60d, 0d } };
            var legs = new List<double[]> { new[] { 50d, 0d, 60d, 0d }, new[] { 60d, 0d, 50d, 0d } };
            var graph = SixWanderEnvelope.WaypointGraph(55, 0, 50, 0, nodes, legs, false, 10, 0, 4);
            TestRunner.True(Math.Abs(graph.Distance2D(0, 0) - 49) < 1e-12, "The waypoint envelope is not its graph plus approach.");
            TestRunner.True(Math.Abs(graph.Distance2D(55, 3) - 2) < 1e-12, "A waypoint leg was not measured along the leg.");
            var fallback = SixWanderEnvelope.WaypointGraph(55, 0, 50, 0, nodes, legs, true, 10, 0, 4);
            TestRunner.True(Math.Abs(fallback.Distance2D(0, 0) - 5) < 1e-12, "A waypoint roamer that may fall back omitted its native random disk.");
            TestRunner.Equal(0d, SixWanderEnvelope.WaypointGraph(55, 0, 50, 0, nodes, legs, true, 10, 0, double.NaN).Distance2D(0, 0),
                "An unreadable fallback radius produced a clearance.");
            TestRunner.True(Math.Abs(SixWanderEnvelope.WaypointGraph(55, 0, 50, 0, nodes, legs, false, 10, 0, double.NaN).Distance2D(0, 0) - 49) < 1e-12,
                "A waypoint roamer that cannot fall back was measured by its unused disk.");
        }

        private static void WaypointFallbackRule()
        {
            TestRunner.True(Chunk7SixPairGroundGoalPolicy.WaypointMayFallBackToRandom(0, false, 0, false), "An isolated waypoint did not fall back.");
            TestRunner.True(!Chunk7SixPairGroundGoalPolicy.WaypointMayFallBackToRandom(1, false, 0, false), "A waypoint with a next point fell back.");
            TestRunner.True(!Chunk7SixPairGroundGoalPolicy.WaypointMayFallBackToRandom(0, false, 2, false), "A waypoint with a previous point fell back.");
            TestRunner.True(Chunk7SixPairGroundGoalPolicy.WaypointMayFallBackToRandom(1, true, 0, false), "A dead end whose next entry lacks its waypoint did not fall back.");
            TestRunner.True(!Chunk7SixPairGroundGoalPolicy.WaypointMayFallBackToRandom(2, true, 1, false), "A reversible waypoint fell back.");
            TestRunner.True(Chunk7SixPairGroundGoalPolicy.WaypointMayFallBackToRandom(2, true, 1, true), "Null entries in both directions did not fall back.");
        }
    }
}
