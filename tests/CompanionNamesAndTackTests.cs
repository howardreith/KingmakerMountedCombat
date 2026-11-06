using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // PET-01 name policy and eligibility, and PET-02's fit on a synthetic body. Native dialog, labels and rendering are native evidence.
    internal static class CompanionNamesAndTackTests
    {
        internal static void Register(TestRunner runner)
        {
            runner.Run("a companion name keeps Unicode and inner spaces, trimming and collapsing only whitespace runs", () =>
            {
                Accepted("  Sir   Barks\ta-Lot ", "Sir Barks a-Lot");
                Accepted("Ébène", "Ébène");
                Accepted("Fido 🐕", "Fido 🐕");
                Accepted("Мухтар", "Мухтар");
                Accepted("Kiss‍me", "Kiss‍me");
            });
            runner.Run("a companion name refuses empty, overlong, control, invisible and markup input", () =>
            {
                Refused(null, CompanionNameVerdict.Empty);
                Refused(" \t ", CompanionNameVerdict.Empty);
                Refused(new string('a', CompanionNamePolicy.MaxTextElements + 1), CompanionNameVerdict.TooLong);
                Accepted(new string('a', CompanionNamePolicy.MaxTextElements), new string('a', CompanionNamePolicy.MaxTextElements));
                Refused("Bad\u0007Dog", CompanionNameVerdict.ControlCharacter);
                Refused("Zero​width", CompanionNameVerdict.ControlCharacter);
                Refused("Over‮ride", CompanionNameVerdict.ControlCharacter);
                Refused("Lone\uD800", CompanionNameVerdict.ControlCharacter);
                Refused("<b>Rex</b>", CompanionNameVerdict.MarkupCharacter);
                Refused("Rex {name}", CompanionNameVerdict.MarkupCharacter);
            });
            runner.Run("an accented letter written with a combining mark counts as one character", () =>
            {
                var combined = string.Concat(Enumerable.Repeat("é", CompanionNamePolicy.MaxTextElements));
                Accepted(combined, combined);
            });
            runner.Run("only the player's own permanent companion animal is renameable", () =>
            {
                TestRunner.True(CompanionRenameEligibility.Refusal(Owned()) == null, "An owned companion was refused.");
                TestRunner.True(CompanionRenameEligibility.Refusal(Owned(s => s.IsPet = false)) != null, "A non-pet was admitted.");
                TestRunner.True(CompanionRenameEligibility.Refusal(Owned(s => s.IsSummoned = true)) != null, "A summon was admitted.");
                TestRunner.True(CompanionRenameEligibility.Refusal(Owned(s => s.MasterOwnsThisPet = false)) != null, "A pet its master no longer owns was admitted.");
                TestRunner.True(CompanionRenameEligibility.Refusal(Owned(s => s.MasterIsPlayerCharacter = false)) != null, "Another faction's pet was admitted.");
                TestRunner.True(CompanionRenameEligibility.Refusal(Owned(s => s.IsPlayerFaction = false)) != null, "A hostile pet was admitted.");
                TestRunner.True(CompanionRenameEligibility.Refusal(null) != null, "No unit was admitted.");
            });
            runner.Run("confirmation renames only the unit and master the dialog opened for", () =>
            {
                TestRunner.True(CompanionRenameEligibility.ConfirmationRefusal(Owned(), Owned()) == null, "The same companion was refused.");
                TestRunner.True(CompanionRenameEligibility.ConfirmationRefusal(Owned(), Owned(s => s.UnitId = "other")) != null, "Another unit was renamed.");
                TestRunner.True(CompanionRenameEligibility.ConfirmationRefusal(Owned(), Owned(s => s.MasterId = "other")) != null, "A changed master was accepted.");
                TestRunner.True(CompanionRenameEligibility.ConfirmationRefusal(Owned(), Owned(s => s.IsPet = false)) != null, "A released companion was renamed.");
                TestRunner.True(CompanionRenameEligibility.ConfirmationRefusal(Owned(), null) != null, "A vanished companion was renamed.");
            });

            runner.Run("the tack drapes over the back, clears it, keeps a low seat between its rolls and is fully skinned", () =>
            {
                string failure;
                var fit = RidingDogTackGeometry.Build(Ellipsoid(), RidingDogTackGeometry.SeatForward, out failure);
                TestRunner.True(fit != null && failure == null, "The fit failed: " + failure);
                TestRunner.True(fit.Misses * 10 <= fit.Casts, "Too many casts missed: " + fit.Misses + "/" + fit.Casts);
                var expectedBack = Top(RidingDogTackGeometry.SeatForward);
                TestRunner.True(Math.Abs(fit.BackHeightAtSeat - expectedBack) < 0.01f, "Back height " + fit.BackHeightAtSeat + " vs " + expectedBack);
                var seatLift = fit.SeatTopAtSeat - fit.BackHeightAtSeat;
                TestRunner.True(seatLift > 0.015f && seatLift < 0.04f, "Seat lift " + seatLift);
                TestRunner.True(fit.CantleTop > fit.SeatTopAtSeat + 0.01f && fit.PommelTop > fit.SeatTopAtSeat + 0.01f, "Cantle or pommel do not rise above the seat.");
                TestRunner.True(fit.CantleTop - fit.BackHeightAtSeat < 0.08f && fit.PommelTop - fit.BackHeightAtSeat < 0.08f, "The rolls are too tall for a Small rider.");
                for (var i = 0; i < fit.VertexCount; i++)
                {
                    float x = fit.Positions[i * 3], y = fit.Positions[i * 3 + 1], z = fit.Positions[i * 3 + 2];
                    TestRunner.True(Inside(x, y, z) >= 1f, "A tack vertex is inside the body at " + x + "," + y + "," + z);
                    var total = 0f;
                    for (var k = 0; k < 4; k++) { total += fit.BoneWeights[i * 4 + k]; TestRunner.True(fit.BoneIndices[i * 4 + k] >= 0 && fit.BoneIndices[i * 4 + k] < 2, "A bone index is out of range."); }
                    TestRunner.True(Math.Abs(total - 1f) < 1e-4f, "Bone weights do not sum to one.");
                    TestRunner.True(fit.Uvs[i * 2] >= 0f && fit.Uvs[i * 2] <= 1f && fit.Uvs[i * 2 + 1] >= 0f && fit.Uvs[i * 2 + 1] <= 1f, "A UV is outside the atlas.");
                }
                TestRunner.True(fit.Triangles.Length > 300 && fit.Triangles.All(index => index >= 0 && index < fit.VertexCount), "Triangles are missing or out of range.");
                // Front faces (Unity: Cross(b - a, c - a)) point away from the body.
                int outward = 0, total3 = fit.Triangles.Length / 3;
                for (var t = 0; t < fit.Triangles.Length; t += 3)
                {
                    var a = V(fit, fit.Triangles[t]); var b = V(fit, fit.Triangles[t + 1]); var c = V(fit, fit.Triangles[t + 2]);
                    var n = Cross(Sub(b, a), Sub(c, a));
                    var centre = new[] { (a[0] + b[0] + c[0]) / 3f, (a[1] + b[1] + c[1]) / 3f, (a[2] + b[2] + c[2]) / 3f };
                    var radial = new[] { centre[0] / Rx / Rx, (centre[1] - Cy) / Ry / Ry, (centre[2] - Cz) / Rz / Rz };
                    if (n[0] * radial[0] + n[1] * radial[1] + n[2] * radial[2] > 0f) outward++;
                }
                TestRunner.True(outward * 100 >= total3 * 90, "Only " + outward + " of " + total3 + " triangles face outward.");
                // Left/right symmetric: the mirror of every vertex has a partner.
                var keys = new HashSet<string>();
                for (var i = 0; i < fit.VertexCount; i++) keys.Add(Key(fit.Positions[i * 3], fit.Positions[i * 3 + 1], fit.Positions[i * 3 + 2]));
                var mirrored = Enumerable.Range(0, fit.VertexCount).Count(i => keys.Contains(Key(-fit.Positions[i * 3], fit.Positions[i * 3 + 1], fit.Positions[i * 3 + 2])));
                TestRunner.True(mirrored * 100 >= fit.VertexCount * 95, "The tack is not left/right symmetric: " + mirrored + "/" + fit.VertexCount);
            });
            runner.Run("fur-card spikes on the body do not reach the pad: it stays a smooth drape", () =>
            {
                string failure;
                var body = Ellipsoid();
                // Every seventh vertex pushed 3 cm out along its normal, like a fringe of fur cards.
                for (var i = 0; i < body.Positions.Length / 3; i += 7)
                    for (var k = 0; k < 3; k++) body.Positions[i * 3 + k] += body.Normals[i * 3 + k] * 0.03f;
                var smooth = RidingDogTackGeometry.Build(Ellipsoid(), RidingDogTackGeometry.SeatForward, out failure);
                var spiky = RidingDogTackGeometry.Build(body, RidingDogTackGeometry.SeatForward, out failure);
                TestRunner.True(spiky != null && failure == null, "The spiky fit failed: " + failure);
                // The same grid on both bodies: the worst vertex displacement stays well under a spike's height.
                var count = Math.Min(smooth.VertexCount, spiky.VertexCount);
                var worst = 0.0; var at = -1;
                for (var i = 0; i < count; i++)
                {
                    double dx = spiky.Positions[i * 3] - smooth.Positions[i * 3], dy = spiky.Positions[i * 3 + 1] - smooth.Positions[i * 3 + 1], dz = spiky.Positions[i * 3 + 2] - smooth.Positions[i * 3 + 2];
                    var d = Math.Sqrt(dx * dx + dy * dy + dz * dz); if (d > worst) { worst = d; at = i; }
                }
                TestRunner.True(smooth.VertexCount == spiky.VertexCount && worst < 0.02, "A spike reached the tack: " + worst.ToString("F3") + " m at vertex " + at + " of " + count + " (" + smooth.Positions[at * 3].ToString("F3") + "," + smooth.Positions[at * 3 + 1].ToString("F3") + "," + smooth.Positions[at * 3 + 2].ToString("F3") + ") uv " + smooth.Uvs[at * 2].ToString("F3") + ".");
            });
            runner.Run("the tack fit refuses an incomplete or mismatched body mesh before casting", () =>
            {
                string failure;
                TestRunner.True(RidingDogTackGeometry.Build(null, 0.12f, out failure) == null && failure != null, "A missing mesh was accepted.");
                var body = Ellipsoid();
                body.BoneWeights = new float[4];
                TestRunner.True(RidingDogTackGeometry.Build(body, 0.12f, out failure) == null && failure != null, "Mismatched weights were accepted.");
                body = Ellipsoid();
                body.Triangles = new[] { 0, 1, body.Positions.Length };
                TestRunner.True(RidingDogTackGeometry.Build(body, 0.12f, out failure) == null && failure != null, "An out-of-range triangle was accepted.");
            });
        }

        private static void Accepted(string input, string expected)
        {
            string name;
            var verdict = CompanionNamePolicy.Normalize(input, out name);
            TestRunner.True(verdict == CompanionNameVerdict.Accepted && name == expected, "Expected '" + expected + "' from '" + input + "', got " + verdict + " '" + name + "'.");
        }

        private static void Refused(string input, CompanionNameVerdict expected)
        {
            string name;
            var verdict = CompanionNamePolicy.Normalize(input, out name);
            TestRunner.True(verdict == expected && name == null, "Expected " + expected + " for '" + input + "', got " + verdict + ".");
        }

        private static CompanionRenameSubject Owned(Action<CompanionRenameSubject> change = null)
        {
            var subject = new CompanionRenameSubject { UnitId = "pet", MasterId = "master", IsPet = true, MasterOwnsThisPet = true, MasterIsPlayerCharacter = true, IsPlayerFaction = true };
            change?.Invoke(subject);
            return subject;
        }

        // A dog-sized ellipsoid body: top of the back near 0.87 over z 0.05, two bones blended front to back.
        private const float Rx = 0.2f, Ry = 0.25f, Rz = 0.5f, Cy = 0.62f, Cz = 0.05f;
        private static float Top(float z) => Cy + Ry * (float)Math.Sqrt(1.0 - ((z - Cz) / Rz) * ((z - Cz) / Rz));
        private static float Inside(float x, float y, float z) => (x / Rx) * (x / Rx) + ((y - Cy) / Ry) * ((y - Cy) / Ry) + ((z - Cz) / Rz) * ((z - Cz) / Rz);

        private static TackSourceMesh Ellipsoid()
        {
            const int rings = 40, segments = 48;
            var positions = new List<float>(); var normals = new List<float>(); var bones = new List<int>(); var weights = new List<float>();
            for (var r = 0; r <= rings; r++)
            {
                var phi = Math.PI * r / rings;
                for (var s = 0; s < segments; s++)
                {
                    var theta = 2 * Math.PI * s / segments;
                    float nx = (float)(Math.Sin(phi) * Math.Cos(theta)), ny = (float)(Math.Sin(phi) * Math.Sin(theta)), nz = (float)Math.Cos(phi);
                    float x = Rx * nx, y = Cy + Ry * ny, z = Cz + Rz * nz;
                    positions.AddRange(new[] { x, y, z });
                    var g = new[] { x / Rx / Rx, (y - Cy) / Ry / Ry, (z - Cz) / Rz / Rz };
                    var length = (float)Math.Sqrt(g[0] * g[0] + g[1] * g[1] + g[2] * g[2]);
                    normals.AddRange(new[] { g[0] / length, g[1] / length, g[2] / length });
                    var front = Math.Max(0f, Math.Min(1f, (z - Cz) / Rz * 0.5f + 0.5f));
                    bones.AddRange(new[] { 0, 1, 0, 0 }); weights.AddRange(new[] { 1f - front, front, 0f, 0f });
                }
            }
            var triangles = new List<int>();
            for (var r = 0; r < rings; r++)
                for (var s = 0; s < segments; s++)
                {
                    int a = r * segments + s, b = r * segments + (s + 1) % segments, c = (r + 1) * segments + (s + 1) % segments, d = (r + 1) * segments + s;
                    triangles.AddRange(new[] { a, b, c, a, c, d });
                }
            return new TackSourceMesh { Positions = positions.ToArray(), Normals = normals.ToArray(), Triangles = triangles.ToArray(), BoneIndices = bones.ToArray(), BoneWeights = weights.ToArray() };
        }

        private static float[] V(TackMeshData fit, int i) => new[] { fit.Positions[i * 3], fit.Positions[i * 3 + 1], fit.Positions[i * 3 + 2] };
        private static float[] Sub(float[] a, float[] b) => new[] { a[0] - b[0], a[1] - b[1], a[2] - b[2] };
        private static float[] Cross(float[] a, float[] b) => new[] { a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0] };
        private static string Key(float x, float y, float z) => Math.Round(x, 3).ToString("F3") + "|" + Math.Round(y, 3).ToString("F3") + "|" + Math.Round(z, 3).ToString("F3");
    }
}
