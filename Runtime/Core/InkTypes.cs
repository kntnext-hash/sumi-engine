using System;
using System.Collections.Generic;

namespace ConnectPiece.SumiEngine.Core
{
    /// <summary>Screen-independent coordinates in canvas pixels, origin bottom-left.</summary>
    [Serializable]
    public struct InkPoint
    {
        public float x, y, time, pressure;
        public InkPoint(float x, float y, float time, float pressure = 1f)
        { this.x = x; this.y = y; this.time = time; this.pressure = InkMath.Clamp01(pressure); }
    }

    /// <summary>Visual ink only. Gameplay resource "ink pressure" is a separate system.</summary>
    [Serializable]
    public struct InkBrushSettings
    {
        public float radius;
        public float pigment;
        public float water;
        public float roughness;
        public float depletion;
        public float bristleSpread;
        public int bristleCount;

        public static InkBrushSettings Vein => new InkBrushSettings
        { radius = 9f, pigment = .72f, water = .42f, roughness = .42f,
          depletion = .005f, bristleSpread = .9f, bristleCount = 11 };
        public static InkBrushSettings Slash => new InkBrushSettings
        { radius = 18f, pigment = .9f, water = .15f, roughness = .72f,
          depletion = .018f, bristleSpread = 1f, bristleCount = 15 };
        public static InkBrushSettings Bloom => new InkBrushSettings
        { radius = 14f, pigment = .35f, water = .95f, roughness = .15f,
          depletion = .003f, bristleSpread = .7f, bristleCount = 13 };
    }

    [Serializable]
    public sealed class InkStrokeRecord
    {
        public int seed;
        public InkBrushSettings brush;
        public List<InkPoint> points = new List<InkPoint>();
    }

    [Serializable]
    public sealed class InkSession
    {
        public int version = 1;
        public int width, height, paperSeed;
        public List<InkStrokeRecord> strokes = new List<InkStrokeRecord>();
    }

    public static class InkMath
    {
        public static float Clamp01(float a) => a < 0f ? 0f : a > 1f ? 1f : a;
        public static float Min(float a, float b) => a < b ? a : b;
        public static float Max(float a, float b) => a > b ? a : b;
        public static float Sqrt(float a) => (float)Math.Sqrt(a);
        public static float Exp(float a) => (float)Math.Exp(a);
        public static float Lerp(float a, float b, float t) => a + (b - a) * t;
        public static int Hash(int x, int y, int seed)
        {
            // A stable 32-bit integer mix. No System.Random or frame-dependent state.
            unchecked
            {
                uint h = (uint)(x * 73856093) ^ (uint)(y * 19349663) ^ (uint)seed;
                h ^= h >> 16; h *= 0x7feb352d;
                h ^= h >> 15; h *= 0x846ca68b;
                h ^= h >> 16;
                return (int)(h & 0x7fffffff);
            }
        }
        public static float Noise01(int x, int y, int seed) => Hash(x, y, seed) / 2147483647f;
    }
}
