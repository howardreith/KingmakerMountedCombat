using System;
using KingmakerMountedCombat.Domain;
using UnityEngine;

namespace KingmakerMountedCombat.Integration
{
    // An original procedural atlas for the tack, painted once per session: a red wool pad with an ochre border, a brown leather
    // seat with a stitched edge, and a darker girth strap. Its columns match RidingDogTackGeometry's UV ranges.
    internal static class RidingDogTackTexture
    {
        private const int Width = 512, Height = 256;
        private static readonly Color Wool = new Color(0.46f, 0.13f, 0.10f), Trim = new Color(0.72f, 0.55f, 0.24f);
        private static readonly Color Leather = new Color(0.40f, 0.25f, 0.13f), Stitch = new Color(0.22f, 0.13f, 0.07f);
        private static readonly Color Strap = new Color(0.25f, 0.16f, 0.09f), StrapEdge = new Color(0.34f, 0.23f, 0.13f);

        internal static Texture2D CreateAtlas()
        {
            var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, true, false)
            {
                name = RidingDogTackService.ObjectName, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, anisoLevel = 4
            };
            var pixels = new Color32[Width * Height];
            for (var y = 0; y < Height; y++)
                for (var x = 0; x < Width; x++)
                    pixels[y * Width + x] = Paint((x + 0.5f) / Width, (y + 0.5f) / Height, x, y);
            texture.SetPixels32(pixels);
            texture.Apply(true, false);
            return texture;
        }

        // A neutral normal map, flat under the PC unpack Unity 2018 uses (x = R*A, y = G) and under plain AG unpacking alike.
        internal static Texture2D CreateFlatNormal()
        {
            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false, true) { name = RidingDogTackService.ObjectName + "_Normal" };
            var pixels = new Color32[16];
            for (var i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 128, 255, 128);
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return texture;
        }

        private static Color32 Paint(float s, float t, int x, int y)
        {
            Color color;
            if (s < (RidingDogTackGeometry.PadUvRight + RidingDogTackGeometry.SeatUvLeft) * 0.5f)
            {
                var u = (s - RidingDogTackGeometry.PadUvLeft) / (RidingDogTackGeometry.PadUvRight - RidingDogTackGeometry.PadUvLeft);
                var v = (t - 0.02f) / 0.96f;
                var edge = Math.Min(Math.Min(u, 1f - u), Math.Min(v, 1f - v));
                // Wool: a fine weave of crossing threads.
                var weave = 0.92f + 0.08f * (float)Math.Sin(x * 1.7f) * (float)Math.Sin(y * 1.7f) + 0.06f * (Noise(x / 6f, y / 6f) - 0.5f);
                color = edge < 0.06f ? Trim * (0.95f + 0.1f * Noise(x / 3f, y / 3f)) : Wool * weave;
                if (edge >= 0.06f && edge < 0.075f) color = Color.Lerp(color, Trim, 0.35f);
            }
            else if (s < (RidingDogTackGeometry.SeatUvRight + RidingDogTackGeometry.GirthUvLeft) * 0.5f)
            {
                var u = (s - RidingDogTackGeometry.SeatUvLeft) / (RidingDogTackGeometry.SeatUvRight - RidingDogTackGeometry.SeatUvLeft);
                var v = (t - 0.02f) / 0.96f;
                var edge = Math.Min(Math.Min(u, 1f - u), Math.Min(v, 1f - v));
                // Leather: soft grain, a darker welt and a dashed stitch line a little inside the edge.
                color = Leather * (0.88f + 0.2f * Noise(x / 4f, y / 4f) + 0.06f * Noise(x / 17f, y / 17f));
                if (edge < 0.04f) color = Color.Lerp(color, Stitch, 0.55f);
                else if (edge > 0.07f && edge < 0.085f && ((x + y) / 3) % 2 == 0) color = Color.Lerp(color, Trim, 0.6f);
            }
            else
            {
                var u = (s - RidingDogTackGeometry.GirthUvLeft) / (RidingDogTackGeometry.GirthUvRight - RidingDogTackGeometry.GirthUvLeft);
                color = (Math.Abs(u - 0.5f) > 0.38f ? StrapEdge : Strap) * (0.9f + 0.15f * Noise(x / 4f, y / 4f));
            }
            color.a = 1f;
            return color;
        }

        // Smooth value noise in [0, 1].
        private static float Noise(float x, float y)
        {
            int x0 = (int)Math.Floor(x), y0 = (int)Math.Floor(y);
            float fx = x - x0, fy = y - y0;
            fx = fx * fx * (3f - 2f * fx); fy = fy * fy * (3f - 2f * fy);
            float a = Hash(x0, y0), b = Hash(x0 + 1, y0), c = Hash(x0, y0 + 1), d = Hash(x0 + 1, y0 + 1);
            return a + (b - a) * fx + (c - a) * fy + (a - b - c + d) * fx * fy;
        }

        private static float Hash(int x, int y)
        {
            unchecked
            {
                var h = (uint)(x * 374761393 + y * 668265263);
                h = (h ^ (h >> 13)) * 1274126177u;
                return (h ^ (h >> 16)) / (float)uint.MaxValue;
            }
        }
    }
}
