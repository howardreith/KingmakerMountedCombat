using System;
using System.Collections.Generic;

namespace KingmakerMountedCombat.Domain
{
    // MMR-02 (reviews R4-F1, R5-F2). Walks an executable route (the mount agent's native path) from its start and returns the
    // route truncated at the FIRST point where the strike's production admission passes, with the cost measured along it.
    // Admission is sampled densely and the first admissible interval is refined by bisection, so a legal reach that begins
    // between samples is found at its boundary (a grant that ends just inside legal reach passes; one that ends short does
    // not). The same walk serves the click decision and every later carrier revalidation, so admission and execution use one
    // computation from the current origin.
    public static class MountedRouteWalker
    {
        public const float SampleStepMetres = 0.05f;
        public const int RefineIterations = 14;

        public static bool Walk<TPoint>(IList<TPoint> points, Func<TPoint, TPoint, float> distance, Func<TPoint, TPoint, float, TPoint> lerp,
            Func<TPoint, bool> admissible, out List<TPoint> route, out float cost)
        {
            route = null;
            cost = float.NaN;
            if (points == null || points.Count == 0 || distance == null || lerp == null || admissible == null) return false;
            var walked = new List<TPoint> { points[0] };
            if (admissible(points[0]))
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
                var steps = Math.Max(1, (int)Math.Ceiling(segment / SampleStepMetres));
                var previous = 0f;
                for (var step = 1; step <= steps; step++)
                {
                    var t = (float)step / steps;
                    if (!admissible(lerp(from, to, t))) { previous = t; continue; }
                    float low = previous, high = t;
                    for (var iteration = 0; iteration < RefineIterations; iteration++)
                    {
                        var middle = (low + high) * 0.5f;
                        if (admissible(lerp(from, to, middle))) high = middle; else low = middle;
                    }
                    walked.Add(lerp(from, to, high));
                    route = walked;
                    cost = travelled + segment * high;
                    return true;
                }
                walked.Add(to);
                travelled += segment;
            }
            cost = travelled;
            return false;
        }
    }
}
