using System;
using System.Collections.Generic;

namespace KingmakerMountedCombat.Domain
{
    // MMR-02 (reviews R4-F1, R5-F2, R6-F2). Walks an executable route (the mount agent's native path) from its start and returns
    // the route truncated at the FIRST point within the movement budget where the strike's production admission passes, with
    // the cost measured along it. The budget (the mount's remaining grant) bounds the walk: on the segment where it ends, the
    // point at exactly the budget distance is itself a sample, so a sight window that contains the grant boundary but neither
    // neighbouring regular sample is still found; from the first admissible sample the walk refines backwards (bisection) to the
    // window's first admissible point, so the endpoint never lies beyond the budget. The predicate receives the segment the
    // point lies on (from, to) so the caller can evaluate the mount's heading there (the travel direction; the start point is
    // passed with from == to, meaning the current orientation). The same walk serves the click decision and every carrier
    // revalidation, so admission and execution use one computation from the current origin.
    public static class MountedRouteWalker
    {
        public const float SampleStepMetres = 0.05f;
        public const int RefineIterations = 14;

        public static bool Walk<TPoint>(IList<TPoint> points, Func<TPoint, TPoint, float> distance, Func<TPoint, TPoint, float, TPoint> lerp,
            Func<TPoint, TPoint, TPoint, bool> admissible, float budget, out List<TPoint> route, out float cost)
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
                var steps = Math.Max(1, (int)Math.Ceiling(segment * limit / SampleStepMetres));
                var previous = 0f;
                for (var step = 1; step <= steps; step++)
                {
                    var t = step == steps ? limit : limit * step / steps;
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
