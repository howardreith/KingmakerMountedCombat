using System;
using System.Collections.Generic;

namespace KingmakerMountedCombat.Domain
{
    // MMR-02 (reviews R4-F1, R5-F2, R6-F2, R7-F1). Walks an executable route (the mount agent's native path) from its start and
    // returns the route truncated at the FIRST point within the movement budget where the strike's production admission passes,
    // with the cost measured along it.
    //
    // Discovery, stated exactly (review R7-F1): admission is evaluated at
    //  - every point of a fixed 1 cm arc-length grid measured from the route's start (s = k x SampleStepMetres), independent
    //    of the budget, up to the budget;
    //  - the point at exactly the budget distance;
    //  - any extra point the caller supplies on a segment (the position established when the click was planned, so a
    //    revalidation re-examines exactly that position even when the budget, and with it the budget point, has changed);
    // and from the first admissible sample the walk refines backwards (bisection) to that opening's first admissible point. An
    // opening is therefore found when it contains a grid point, the budget point or a supplied point. Every opening at least
    // 1 cm long contains a grid point; a narrower one can be missed. Because the grid does not depend on the budget, raising
    // the budget never loses an opening found through a grid point; an opening found only through the budget point can be
    // missed at another budget, which is why the established position is supplied to every revalidation. The predicate
    // receives the segment the point lies on (from, to) so the caller can evaluate the mount's heading there (the start point
    // is passed with from == to, meaning the current orientation). Nothing beyond the budget is returned.
    public static class MountedRouteWalker
    {
        public const float SampleStepMetres = 0.01f;
        public const int RefineIterations = 14;

        public static bool Walk<TPoint>(IList<TPoint> points, Func<TPoint, TPoint, float> distance, Func<TPoint, TPoint, float, TPoint> lerp,
            Func<TPoint, TPoint, TPoint, bool> admissible, float budget, out List<TPoint> route, out float cost) =>
            Walk(points, distance, lerp, admissible, budget, null, out route, out cost);

        // extraSamples: for a segment (from, to), additional parameters t in [0, 1] to evaluate on it (may be null).
        public static bool Walk<TPoint>(IList<TPoint> points, Func<TPoint, TPoint, float> distance, Func<TPoint, TPoint, float, TPoint> lerp,
            Func<TPoint, TPoint, TPoint, bool> admissible, float budget, Func<TPoint, TPoint, IEnumerable<float>> extraSamples,
            out List<TPoint> route, out float cost)
        {
            route = null;
            cost = float.NaN;
            if (points == null || points.Count == 0 || distance == null || lerp == null || admissible == null || float.IsNaN(budget) || budget < 0f)
                return false;
            var start = points[0];
            var walked = new List<TPoint> { start };
            if (admissible(start, start, start))
            {
                route = walked;
                cost = 0f;
                return true;
            }
            var travelled = 0f;
            for (var index = 1; index < points.Count; index++)
            {
                var from = points[index - 1];
                var to = points[index];
                var segment = distance(from, to);
                if (segment <= 0f) continue;
                var remaining = budget - travelled;
                if (remaining <= 0f) break;
                // The part of this segment the budget reaches: all of it, or up to the exact budget point.
                var limit = remaining >= segment ? 1f : remaining / segment;
                var samples = new List<float>();
                var first = (int)Math.Ceiling(travelled / SampleStepMetres - 1e-4);
                for (var k = Math.Max(first, 0); k * SampleStepMetres <= travelled + segment * limit + 1e-6f; k++)
                {
                    var t = (k * SampleStepMetres - travelled) / segment;
                    if (t > 0f && t <= limit) samples.Add(t);
                }
                samples.Add(limit);
                if (extraSamples != null)
                    foreach (var t in extraSamples(from, to))
                        if (t > 0f && t <= limit) samples.Add(t);
                samples.Sort();
                var previous = 0f;
                foreach (var t in samples)
                {
                    if (t <= previous) continue;
                    if (!admissible(lerp(from, to, t), from, to)) { previous = t; continue; }
                    float low = previous, high = t;
                    for (var iteration = 0; iteration < RefineIterations; iteration++)
                    {
                        var middle = (low + high) * 0.5f;
                        if (admissible(lerp(from, to, middle), from, to)) high = middle; else low = middle;
                    }
                    walked.Add(lerp(from, to, high));
                    route = walked;
                    cost = travelled + segment * high;
                    return true;
                }
                if (limit < 1f)
                {
                    cost = budget;
                    return false;
                }
                walked.Add(to);
                travelled += segment;
            }
            cost = Math.Min(travelled, budget);
            return false;
        }
    }
}
