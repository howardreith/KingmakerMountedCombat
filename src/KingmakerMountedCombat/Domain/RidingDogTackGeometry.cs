using System;
using System.Collections.Generic;

namespace KingmakerMountedCombat.Domain
{
    // The Riding Dog's own body mesh, in mount-local space (x right, y up, z forward) as its bind pose places it.
    public sealed class TackSourceMesh
    {
        public float[] Positions { get; set; }
        public float[] Normals { get; set; }
        public int[] Triangles { get; set; }
        public int[] BoneIndices { get; set; }
        public float[] BoneWeights { get; set; }
    }

    // A skinned tack mesh in the same space, weighted to the dog's own bones, plus what the fit measured.
    public sealed class TackMeshData
    {
        public float[] Positions { get; internal set; }
        public float[] Normals { get; internal set; }
        public float[] Uvs { get; internal set; }
        public int[] Triangles { get; internal set; }
        public int[] BoneIndices { get; internal set; }
        public float[] BoneWeights { get; internal set; }
        public int VertexCount => Positions == null ? 0 : Positions.Length / 3;
        public int Casts { get; internal set; }
        public int Misses { get; internal set; }
        public float BackHeightAtSeat { get; internal set; }
        public float SeatTopAtSeat { get; internal set; }
        public float CantleTop { get; internal set; }
        public float PommelTop { get; internal set; }
        // Highest pad or seat vertex above the body's own dorsal line at that vertex's z: a fin or a raised edge shows here even
        // when the seat itself sits correctly (r02, r03).
        public float MaxRiseAboveBack { get; internal set; }
        // Vertices [0, PadVertexEnd) are the pad and its rim, [PadVertexEnd, SeatVertexEnd) the seat, the rest the girth.
        public int PadVertexEnd { get; internal set; }
        public int SeatVertexEnd { get; internal set; }
    }

    // PET-02: an original saddle pad, seat and girth draped over the Riding Dog's actual back. Every point is a ray cast onto the
    // dog's surface, lifted a little along it and skinned with the bone weights of the triangle it landed on, so the tack bends
    // with the spine and the bite without any per-frame work. The texture atlas has three columns: pad, seat and girth.
    public static class RidingDogTackGeometry
    {
        // Mount-local, measured on the Medium Riding Dog: the rider's pelvis sits over z 0.12 (MountedRiderProfileCatalog).
        public const float SeatForward = 0.12f;
        private const float PadBack = 0.19f, PadFront = 0.16f, PadAngle = 0.95f, PadLift = 0.010f, RimLift = 0.004f;
        private const float SeatBack = 0.15f, SeatFront = 0.16f, SeatAngle = 0.62f, SeatBase = 0.012f;
        private const float CantleHeight = 0.032f, CantleOffset = -0.10f, PommelHeight = 0.026f, PommelOffset = 0.11f, RollWidth = 0.028f;
        private const float GirthOffset = 0.05f, GirthWidth = 0.036f, GirthLift = 0.016f;
        private const int PadRows = 14, PadColumns = 13, SeatRows = 12, SeatColumns = 9, GirthSegments = 40;
        internal const float PadUvLeft = 0.02f, PadUvRight = 0.48f, SeatUvLeft = 0.52f, SeatUvRight = 0.73f, GirthUvLeft = 0.77f, GirthUvRight = 0.98f;

        public static TackMeshData Build(TackSourceMesh source, float seatForward, out string failure)
        {
            failure = Validate(source);
            if (failure != null) return null;
            var builder = new Builder(source, seatForward);
            return builder.Run(out failure);
        }

        private static string Validate(TackSourceMesh source)
        {
            if (source == null || source.Positions == null || source.Normals == null || source.Triangles == null ||
                source.BoneIndices == null || source.BoneWeights == null) return "the body mesh is incomplete";
            var count = source.Positions.Length / 3;
            if (count < 3 || source.Positions.Length != count * 3 || source.Normals.Length != count * 3 ||
                source.BoneIndices.Length != count * 4 || source.BoneWeights.Length != count * 4 ||
                source.Triangles.Length < 3 || source.Triangles.Length % 3 != 0) return "the body mesh arrays disagree";
            foreach (var index in source.Triangles) if (index < 0 || index >= count) return "a body triangle is out of range";
            foreach (var value in source.Positions) if (float.IsNaN(value) || float.IsInfinity(value)) return "a body position is not finite";
            return null;
        }

        internal struct V3
        {
            internal readonly float X, Y, Z;
            internal V3(float x, float y, float z) { X = x; Y = y; Z = z; }
            public static V3 operator +(V3 a, V3 b) => new V3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
            public static V3 operator -(V3 a, V3 b) => new V3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
            public static V3 operator *(V3 a, float s) => new V3(a.X * s, a.Y * s, a.Z * s);
            internal static float Dot(V3 a, V3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;
            internal static V3 Cross(V3 a, V3 b) => new V3(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);
            internal float Length => (float)Math.Sqrt(Dot(this, this));
            internal V3 Normalized { get { var length = Length; return length > 1e-6f ? this * (1f / length) : new V3(0f, 1f, 0f); } }
        }

        private struct Hit
        {
            internal bool Valid;
            internal V3 Point, Normal;
            internal int[] Bones;
            internal float[] Weights;
        }

        private sealed class Builder
        {
            private readonly TackSourceMesh source;
            private readonly float seat;
            private readonly List<int> slab = new List<int>();
            private readonly Dictionary<int, float> axisCache = new Dictionary<int, float>();
            private readonly List<V3> positions = new List<V3>(), normals = new List<V3>();
            private readonly List<float> uvs = new List<float>(), weights = new List<float>();
            private readonly List<int> bones = new List<int>(), triangles = new List<int>();
            private int casts, misses;

            internal Builder(TackSourceMesh source, float seatForward) { this.source = source; seat = seatForward; }

            internal TackMeshData Run(out string failure)
            {
                failure = null;
                var low = seat - Math.Max(PadBack, SeatBack) - 0.05f;
                var high = seat + Math.Max(PadFront, Math.Max(SeatFront, GirthOffset + GirthWidth)) + 0.05f;
                var triangleList = source.Triangles;
                for (var t = 0; t < triangleList.Length; t += 3)
                {
                    float min = float.MaxValue, max = float.MinValue;
                    for (var k = 0; k < 3; k++) { var z = source.Positions[triangleList[t + k] * 3 + 2]; min = Math.Min(min, z); max = Math.Max(max, z); }
                    if (max >= low && min <= high) slab.Add(t);
                }
                if (slab.Count == 0) { failure = "no body surface under the seat"; return null; }
                if (float.IsNaN(AxisHeight(seat))) { failure = "no midline back at the seat"; return null; }

                var back = Cast(seat, 0f);
                if (!back.Valid) { failure = "no back surface at the seat"; return null; }
                var padTop = BuildPad();
                var padEnd = positions.Count;
                var seatTop = BuildSeat(out var cantleTop, out var pommelTop);
                var seatEnd = positions.Count;
                var maxRise = MaxRise(positions.Count);
                BuildGirth();
                if (padTop < 0 || seatTop < 0) { failure = "the pad or seat did not reach the back"; return null; }
                if (misses * 4 > casts) { failure = "too many casts missed the body (" + misses + " of " + casts + ")"; return null; }

                var data = new TackMeshData
                {
                    Positions = Flatten(positions), Normals = Flatten(normals), Uvs = uvs.ToArray(), Triangles = triangles.ToArray(),
                    BoneIndices = bones.ToArray(), BoneWeights = weights.ToArray(), Casts = casts, Misses = misses,
                    BackHeightAtSeat = back.Point.Y, SeatTopAtSeat = positions[seatTop].Y, CantleTop = cantleTop, PommelTop = pommelTop, MaxRiseAboveBack = maxRise,
                    PadVertexEnd = padEnd, SeatVertexEnd = seatEnd
                };
                return data;
            }

            private float MaxRise(int count)
            {
                var rise = float.MinValue;
                for (var i = 0; i < count; i++)
                {
                    var dorsal = DorsalVertex(positions[i].Z);
                    if (dorsal >= 0) rise = Math.Max(rise, positions[i].Y - source.Positions[dorsal * 3 + 1]);
                }
                return rise;
            }

            private static float[] Flatten(List<V3> values)
            {
                var flat = new float[values.Count * 3];
                for (var i = 0; i < values.Count; i++) { flat[i * 3] = values[i].X; flat[i * 3 + 1] = values[i].Y; flat[i * 3 + 2] = values[i].Z; }
                return flat;
            }

            // The pad: a rounded rectangle draped over the back and flanks, with a thin rim down to the coat at its edge.
            private int BuildPad()
            {
                var grid = new int[PadRows, PadColumns];
                var hits = new Hit[PadRows, PadColumns];
                var zs = new float[PadRows, PadColumns];
                var angles = new float[PadRows, PadColumns];
                var centre = -1;
                for (var i = 0; i < PadRows; i++)
                    for (var j = 0; j < PadColumns; j++)
                    {
                        float u = i / (float)(PadRows - 1), v = -1f + 2f * j / (PadColumns - 1);
                        zs[i, j] = PadZ(u, v); angles[i, j] = v * PadAngle;
                        hits[i, j] = Cast(zs[i, j], angles[i, j]);
                    }
                SmoothGrid(hits, zs, angles, false);
                for (var i = 0; i < PadRows; i++)
                    for (var j = 0; j < PadColumns; j++)
                    {
                        var hit = hits[i, j];
                        grid[i, j] = hit.Valid ? AddVertex(Lift(hit, PadLift), hit.Normal, PadUvLeft + (PadUvRight - PadUvLeft) * j / (PadColumns - 1), 0.02f + 0.96f * i / (PadRows - 1), hit) : -1;
                        if (i == PadRows / 2 && j == PadColumns / 2) centre = grid[i, j];
                    }
                AddGrid(grid, PadRows, PadColumns, false);
                if (centre < 0) return -1;
                // Rim: the perimeter, in order, joined to a ring just above the coat.
                var ring = new List<KeyValuePair<int, int>>();
                for (var j = 0; j < PadColumns; j++) ring.Add(new KeyValuePair<int, int>(0, j));
                for (var i = 1; i < PadRows; i++) ring.Add(new KeyValuePair<int, int>(i, PadColumns - 1));
                for (var j = PadColumns - 2; j >= 0; j--) ring.Add(new KeyValuePair<int, int>(PadRows - 1, j));
                for (var i = PadRows - 2; i > 0; i--) ring.Add(new KeyValuePair<int, int>(i, 0));
                var outer = new int[ring.Count];
                var inner = new int[ring.Count];
                var middle = positions[centre];
                for (var k = 0; k < ring.Count; k++)
                {
                    var cell = ring[k];
                    outer[k] = grid[cell.Key, cell.Value];
                    if (outer[k] < 0) { inner[k] = -1; continue; }
                    var hit = hits[cell.Key, cell.Value];
                    var away = positions[outer[k]] - middle;
                    var tangent = (away - hit.Normal * V3.Dot(away, hit.Normal)).Normalized;
                    var rimTop = AddVertex(positions[outer[k]], tangent, PadUvLeft + 0.004f, 0.02f + 0.96f * cell.Key / (PadRows - 1), hit);
                    outer[k] = rimTop;
                    inner[k] = AddVertex(Lift(hit, RimLift), tangent, PadUvLeft + 0.004f, 0.02f + 0.96f * cell.Key / (PadRows - 1), hit);
                }
                for (var k = 0; k < ring.Count; k++)
                {
                    var next = (k + 1) % ring.Count;
                    if (outer[k] < 0 || outer[next] < 0) continue;
                    var hint = normals[outer[k]] + normals[outer[next]];
                    AddTriangle(outer[k], outer[next], inner[next], hint);
                    AddTriangle(outer[k], inner[next], inner[k], hint);
                }
                return centre;
            }

            // Shorter towards the lower corners, so the pad's outline is rounded rather than square.
            private float PadZ(float u, float v)
            {
                var length = (PadBack + PadFront) * (1f - 0.18f * Math.Abs(v * v * v));
                var middle = seat + (PadFront - PadBack) * 0.5f;
                return middle + (u - 0.5f) * length;
            }

            // The seat: a low leather pad on top of the pad, with a cantle behind the rider and a pommel in front.
            private int BuildSeat(out float cantleTop, out float pommelTop)
            {
                cantleTop = pommelTop = float.MinValue;
                var grid = new int[SeatRows, SeatColumns];
                var hits = new Hit[SeatRows, SeatColumns];
                var zs = new float[SeatRows, SeatColumns];
                var angles = new float[SeatRows, SeatColumns];
                for (var i = 0; i < SeatRows; i++)
                    for (var j = 0; j < SeatColumns; j++)
                    {
                        zs[i, j] = seat - SeatBack + i / (float)(SeatRows - 1) * (SeatBack + SeatFront);
                        angles[i, j] = (-1f + 2f * j / (SeatColumns - 1)) * SeatAngle;
                        hits[i, j] = Cast(zs[i, j], angles[i, j]);
                    }
                SmoothGrid(hits, zs, angles, false);
                var centre = -1;
                var bestDistance = float.MaxValue;
                for (var i = 0; i < SeatRows; i++)
                    for (var j = 0; j < SeatColumns; j++)
                    {
                        float u = i / (float)(SeatRows - 1), v = -1f + 2f * j / (SeatColumns - 1);
                        var z = zs[i, j];
                        var hit = hits[i, j];
                        if (!hit.Valid) { grid[i, j] = -1; continue; }
                        var across = (float)Math.Pow(Math.Max(0f, 1f - v * v), 1.5);
                        var edge = Smooth(0f, 0.25f, 1f - Math.Abs(v)) * Smooth(0f, 0.12f, Math.Min(u, 1f - u));
                        var cantle = CantleHeight * Bump(z - (seat + CantleOffset)) * across;
                        var pommel = PommelHeight * Bump(z - (seat + PommelOffset)) * across;
                        var lift = PadLift + 0.001f + (SeatBase + cantle + pommel) * edge;
                        var point = Lift(hit, lift);
                        grid[i, j] = AddVertex(point, hit.Normal, SeatUvLeft + (SeatUvRight - SeatUvLeft) * j / (SeatColumns - 1), 0.02f + 0.96f * u, hit);
                        if (j == SeatColumns / 2)
                        {
                            if (z < seat) cantleTop = Math.Max(cantleTop, point.Y); else pommelTop = Math.Max(pommelTop, point.Y);
                            var distance = Math.Abs(z - seat);
                            if (distance < bestDistance) { bestDistance = distance; centre = grid[i, j]; }
                        }
                    }
                AddGrid(grid, SeatRows, SeatColumns, false);
                return centre;
            }

            private static float Bump(float offset) => (float)Math.Exp(-(offset / RollWidth) * (offset / RollWidth));

            private static float Smooth(float from, float to, float value)
            {
                var t = Math.Max(0f, Math.Min(1f, (value - from) / (to - from)));
                return t * t * (3f - 2f * t);
            }

            // The girth: a strap all the way round the chest just ahead of the seat. A cast that lands on a leg instead of the
            // chest (much further from the body's axis than its neighbours) takes the nearest good neighbour's place.
            private void BuildGirth()
            {
                var rows = new[] { -0.5f, 0f, 0.5f };
                var grid = new int[rows.Length, GirthSegments + 1];
                var hits = new Hit[rows.Length, GirthSegments];
                var zs = new float[rows.Length, GirthSegments];
                var angles = new float[rows.Length, GirthSegments];
                var usable = new bool[rows.Length];
                for (var r = 0; r < rows.Length; r++)
                {
                    var z = seat + GirthOffset + rows[r] * GirthWidth;
                    var axis = AxisHeight(z);
                    var ring = new Hit[GirthSegments];
                    var radii = new List<float>();
                    for (var k = 0; k < GirthSegments; k++)
                    {
                        zs[r, k] = z; angles[r, k] = (float)(-Math.PI + 2.0 * Math.PI * k / GirthSegments);
                        ring[k] = Cast(z, angles[r, k]);
                        if (ring[k].Valid) radii.Add(Radius(ring[k], axis));
                    }
                    if (radii.Count < GirthSegments / 2) continue;
                    usable[r] = true;
                    radii.Sort();
                    var limit = radii[radii.Count / 2] * 1.25f;
                    var good = new bool[GirthSegments];
                    for (var k = 0; k < GirthSegments; k++) good[k] = ring[k].Valid && Radius(ring[k], axis) <= limit;
                    for (var k = 0; k < GirthSegments; k++)
                    {
                        // The good neighbour's bone weights and distance from the axis, on this segment's own ray.
                        var near = ring[NearestGood(good, k)];
                        var outward = new V3((float)Math.Sin(angles[r, k]), (float)Math.Cos(angles[r, k]), 0f);
                        hits[r, k] = new Hit { Valid = true, Point = new V3(0f, axis, z) + outward * Radius(near, axis), Normal = outward, Bones = near.Bones, Weights = near.Weights };
                    }
                }
                SmoothGrid(hits, zs, angles, true);
                for (var r = 0; r < rows.Length; r++)
                    for (var k = 0; k <= GirthSegments; k++)
                    {
                        var hit = hits[r, k % GirthSegments];
                        grid[r, k] = usable[r] && hit.Valid ? AddVertex(Lift(hit, GirthLift), hit.Normal, GirthUvLeft + (GirthUvRight - GirthUvLeft) * r / (rows.Length - 1),
                            0.02f + 0.96f * k / GirthSegments, hit) : -1;
                    }
                AddGrid(grid, rows.Length, GirthSegments + 1, false);
            }

            private static int NearestGood(bool[] good, int k)
            {
                for (var step = 0; step < good.Length; step++)
                {
                    if (good[(k + step) % good.Length]) return (k + step) % good.Length;
                    if (good[(k - step + good.Length) % good.Length]) return (k - step + good.Length) % good.Length;
                }
                return k;
            }

            private static float Radius(Hit hit, float axis) => new V3(hit.Point.X, hit.Point.Y - axis, 0f).Length;

            // Each sample lies on its own ray from its own axis point. Its 3x3 neighbours are measured as distances from that same point in
            // the cross-section (never from their own axis points: a real body's axis height jumps between neighbouring z, r03); the median
            // drops isolated spikes (fur cards, a leg in the way), a second pass takes their mean, and normals come from the smoothed surface.
            private void SmoothGrid(Hit[,] hits, float[,] zs, float[,] angles, bool wrap)
            {
                int rows = hits.GetLength(0), columns = hits.GetLength(1);
                var valid = new bool[rows, columns];
                var points = new V3[rows, columns];
                var anchors = new V3[rows, columns];
                for (var i = 0; i < rows; i++)
                    for (var j = 0; j < columns; j++)
                    {
                        anchors[i, j] = new V3(0f, AxisHeight(zs[i, j]), zs[i, j]);
                        if (hits[i, j].Valid) { points[i, j] = hits[i, j].Point; valid[i, j] = true; }
                    }
                points = Project(Project(points, anchors, angles, valid, wrap, true), anchors, angles, valid, wrap, false);
                for (var i = 0; i < rows; i++)
                    for (var j = 0; j < columns; j++)
                    {
                        if (!valid[i, j]) continue;
                        var outward = Outward(angles[i, j]);
                        var normal = V3.Cross(Difference(points, valid, i, j, 1, 0, wrap), Difference(points, valid, i, j, 0, 1, wrap));
                        if (normal.Length < 1e-6f) normal = outward;
                        else if (V3.Dot(normal, outward) < 0f) normal = normal * -1f;
                        hits[i, j] = new Hit { Valid = true, Point = points[i, j], Normal = (normal.Normalized * 0.8f + outward * 0.2f).Normalized,
                            Bones = hits[i, j].Bones, Weights = hits[i, j].Weights };
                    }
            }

            private static V3 Outward(float angle) => new V3((float)Math.Sin(angle), (float)Math.Cos(angle), 0f);

            // One pass: each sample moves along its own ray to the median (or mean) of its neighbours' distances from its axis point.
            private static V3[,] Project(V3[,] points, V3[,] anchors, float[,] angles, bool[,] valid, bool wrap, bool median)
            {
                int rows = points.GetLength(0), columns = points.GetLength(1);
                var result = new V3[rows, columns];
                var window = new List<float>(9);
                for (var i = 0; i < rows; i++)
                    for (var j = 0; j < columns; j++)
                    {
                        if (!valid[i, j]) continue;
                        var outward = Outward(angles[i, j]);
                        window.Clear();
                        for (var di = -1; di <= 1; di++)
                            for (var dj = -1; dj <= 1; dj++)
                            {
                                int ii = i + di, jj = wrap ? (j + dj + columns) % columns : j + dj;
                                if (ii >= 0 && ii < rows && jj >= 0 && jj < columns && valid[ii, jj]) window.Add(new V3(points[ii, jj].X - anchors[i, j].X, points[ii, jj].Y - anchors[i, j].Y, 0f).Length);
                            }
                        float distance;
                        if (median) { window.Sort(); distance = window[window.Count / 2]; }
                        else { var sum = 0f; foreach (var value in window) sum += value; distance = sum / window.Count; }
                        result[i, j] = anchors[i, j] + outward * distance;
                    }
                return result;
            }

            // Central difference where both neighbours exist, one-sided at an edge, zero when neither does.
            private static V3 Difference(V3[,] points, bool[,] valid, int i, int j, int di, int dj, bool wrap)
            {
                int rows = points.GetLength(0), columns = points.GetLength(1);
                bool Usable(int r, int c) => r >= 0 && r < rows && c >= 0 && c < columns && valid[r, c];
                int Column(int c) => wrap ? (c + columns) % columns : c;
                int ai = i + di, aj = Column(j + dj), bi = i - di, bj = Column(j - dj);
                var forward = Usable(ai, aj);
                var backward = Usable(bi, bj);
                if (forward && backward) return points[ai, aj] - points[bi, bj];
                if (forward) return points[ai, aj] - points[i, j];
                if (backward) return points[i, j] - points[bi, bj];
                return new V3(0f, 0f, 0f);
            }

            private static V3 Lift(Hit hit, float lift) => hit.Point + hit.Normal * lift;

            private int AddVertex(V3 point, V3 normal, float u, float v, Hit hit)
            {
                positions.Add(point);
                normals.Add(normal.Normalized);
                uvs.Add(u); uvs.Add(v);
                SpineWeights(point.Z, out var spineBones, out var spineWeights);
                for (var k = 0; k < 4; k++) { bones.Add(spineBones[k]); weights.Add(spineWeights[k]); }
                return positions.Count - 1;
            }

            private void AddGrid(int[,] grid, int rows, int columns, bool unused)
            {
                for (var i = 0; i + 1 < rows; i++)
                    for (var j = 0; j + 1 < columns; j++)
                    {
                        int a = grid[i, j], b = grid[i + 1, j], c = grid[i + 1, j + 1], d = grid[i, j + 1];
                        if (a < 0 || b < 0 || c < 0 || d < 0) continue;
                        var hint = normals[a] + normals[b] + normals[c] + normals[d];
                        AddTriangle(a, b, c, hint);
                        AddTriangle(a, c, d, hint);
                    }
            }

            // Unity draws a triangle's front where Cross(b - a, c - a) points; each one is turned to face away from the body.
            private void AddTriangle(int a, int b, int c, V3 outward)
            {
                var facing = V3.Dot(V3.Cross(positions[b] - positions[a], positions[c] - positions[a]), outward);
                if (facing >= 0f) { triangles.Add(a); triangles.Add(b); triangles.Add(c); }
                else { triangles.Add(a); triangles.Add(c); triangles.Add(b); }
            }

            // Tack is rigid around the torso, so every vertex is skinned to the spine alone: the bone weights of the dorsal midline
            // vertex at its own z, mixed with those 2 cm before and behind. A cast that landed on fur, a leg or the neck would
            // otherwise lend its vertex to a limb that swings away under animation (r02).
            private void SpineWeights(float z, out int[] indices, out float[] values)
            {
                var mixed = new Dictionary<int, float>();
                foreach (var offset in new[] { -0.02f, 0f, 0.02f })
                {
                    var vertex = DorsalVertex(z + offset);
                    if (vertex >= 0) Accumulate(mixed, vertex, 1f);
                }
                Strongest(mixed, out indices, out values);
            }

            private int DorsalVertex(float z)
            {
                var key = (int)Math.Round(z * 1000f);
                if (dorsalCache.TryGetValue(key, out var cached)) return cached;
                var best = -1;
                foreach (var window in new[] { new[] { 0.03f, 0.05f }, new[] { 0.06f, 0.08f } })
                {
                    var top = float.MinValue;
                    var count = source.Positions.Length / 3;
                    for (var i = 0; i < count; i++)
                    {
                        if (Math.Abs(source.Positions[i * 3 + 2] - z) > window[0] || Math.Abs(source.Positions[i * 3]) > window[1]) continue;
                        if (source.Positions[i * 3 + 1] > top) { top = source.Positions[i * 3 + 1]; best = i; }
                    }
                    if (best >= 0) break;
                }
                dorsalCache[key] = best;
                return best;
            }

            private readonly Dictionary<int, int> dorsalCache = new Dictionary<int, int>();

            // Height of the body's axis at z: the dorsal line there, less the torso's half-depth measured once at the seat. The spine is
            // found at every z, while the lowest midline point is not: ahead of the seat the chest and legs reach the ground, and an
            // axis halfway down to them falls outside the body (r03).
            private float AxisHeight(float z)
            {
                var key = (int)Math.Round(z * 1000f);
                if (axisCache.TryGetValue(key, out var cached)) return cached;
                if (float.IsNaN(halfDepth))
                {
                    var middle = MidlineCentre(seat);
                    var spine = DorsalVertex(seat);
                    halfDepth = spine < 0 || float.IsNaN(middle) ? float.NaN : source.Positions[spine * 3 + 1] - middle;
                }
                var dorsal = DorsalVertex(z);
                var result = dorsal < 0 || float.IsNaN(halfDepth) ? float.NaN : source.Positions[dorsal * 3 + 1] - halfDepth;
                axisCache[key] = result;
                return result;
            }

            private float halfDepth = float.NaN;

            // Halfway between the coat on the spine and under the belly, at the seat only.
            private float MidlineCentre(float z)
            {
                var result = float.NaN;
                foreach (var window in new[] { new[] { 0.03f, 0.05f }, new[] { 0.06f, 0.08f } })
                {
                    float top = float.MinValue, bottom = float.MaxValue;
                    var count = source.Positions.Length / 3;
                    for (var i = 0; i < count; i++)
                    {
                        if (Math.Abs(source.Positions[i * 3 + 2] - z) > window[0] || Math.Abs(source.Positions[i * 3]) > window[1]) continue;
                        top = Math.Max(top, source.Positions[i * 3 + 1]);
                        bottom = Math.Min(bottom, source.Positions[i * 3 + 1]);
                    }
                    if (top > bottom) { result = (top + bottom) * 0.5f; break; }
                }
                return result;
            }

            // A ray in the plane z = const, from well outside the body towards its axis at the given angle (0 = straight up,
            // positive towards +x). The first front-facing surface it meets is the coat.
            private Hit Cast(float z, float angle)
            {
                casts++;
                var axis = AxisHeight(z);
                if (float.IsNaN(axis)) { misses++; return new Hit(); }
                var outward = new V3((float)Math.Sin(angle), (float)Math.Cos(angle), 0f);
                var origin = new V3(0f, axis, z) + outward * 2f;
                var direction = outward * -1f;
                var best = new Hit();
                var bestT = float.MaxValue;
                var positionsArray = source.Positions;
                foreach (var t in slab)
                {
                    int i0 = source.Triangles[t], i1 = source.Triangles[t + 1], i2 = source.Triangles[t + 2];
                    float z0 = positionsArray[i0 * 3 + 2], z1 = positionsArray[i1 * 3 + 2], z2 = positionsArray[i2 * 3 + 2];
                    if (Math.Min(z0, Math.Min(z1, z2)) > z || Math.Max(z0, Math.Max(z1, z2)) < z) continue;
                    V3 p0 = P(i0), p1 = P(i1), p2 = P(i2);
                    if (!Intersect(origin, direction, p0, p1, p2, out var distance, out var b1, out var b2) || distance >= bestT) continue;
                    var b0 = 1f - b1 - b2;
                    var normal = (N(i0) * b0 + N(i1) * b1 + N(i2) * b2).Normalized;
                    if (V3.Dot(normal, outward) <= 0f) continue;
                    bestT = distance;
                    best = new Hit { Valid = true, Point = origin + direction * distance, Normal = (normal * 0.6f + outward * 0.4f).Normalized };
                    BlendWeights(i0, i1, i2, b0, b1, b2, out best.Bones, out best.Weights);
                }
                if (!best.Valid) misses++;
                return best;
            }

            private V3 P(int i) => new V3(source.Positions[i * 3], source.Positions[i * 3 + 1], source.Positions[i * 3 + 2]);
            private V3 N(int i) => new V3(source.Normals[i * 3], source.Normals[i * 3 + 1], source.Normals[i * 3 + 2]);

            // Möller–Trumbore, both windings.
            private static bool Intersect(V3 origin, V3 direction, V3 p0, V3 p1, V3 p2, out float t, out float u, out float v)
            {
                t = u = v = 0f;
                var e1 = p1 - p0;
                var e2 = p2 - p0;
                var p = V3.Cross(direction, e2);
                var determinant = V3.Dot(e1, p);
                if (Math.Abs(determinant) < 1e-9f) return false;
                var inverse = 1f / determinant;
                var s = origin - p0;
                u = V3.Dot(s, p) * inverse;
                if (u < 0f || u > 1f) return false;
                var q = V3.Cross(s, e1);
                v = V3.Dot(direction, q) * inverse;
                if (v < 0f || u + v > 1f) return false;
                t = V3.Dot(e2, q) * inverse;
                return t > 0f;
            }

            // The three corners' bone weights mixed by the hit's barycentric position; the four strongest kept and renormalized.
            private void BlendWeights(int i0, int i1, int i2, float b0, float b1, float b2, out int[] indices, out float[] values)
            {
                var mixed = new Dictionary<int, float>();
                Accumulate(mixed, i0, b0); Accumulate(mixed, i1, b1); Accumulate(mixed, i2, b2);
                Strongest(mixed, out indices, out values);
            }

            // The four strongest bones, renormalized.
            private static void Strongest(Dictionary<int, float> mixed, out int[] indices, out float[] values)
            {
                var ordered = new List<KeyValuePair<int, float>>(mixed);
                ordered.Sort((left, right) => right.Value != left.Value ? right.Value.CompareTo(left.Value) : left.Key.CompareTo(right.Key));
                indices = new int[4];
                values = new float[4];
                var total = 0f;
                for (var k = 0; k < 4 && k < ordered.Count; k++) { indices[k] = ordered[k].Key; values[k] = ordered[k].Value; total += values[k]; }
                if (total <= 0f) { values[0] = 1f; return; }
                for (var k = 0; k < 4; k++) values[k] /= total;
            }

            private void Accumulate(Dictionary<int, float> mixed, int vertex, float share)
            {
                for (var k = 0; k < 4; k++)
                {
                    var weight = source.BoneWeights[vertex * 4 + k] * Math.Max(0f, share);
                    if (weight <= 0f) continue;
                    var bone = source.BoneIndices[vertex * 4 + k];
                    mixed.TryGetValue(bone, out var current);
                    mixed[bone] = current + weight;
                }
            }
        }
    }
}
