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

        public static InkBrushSettings Dense => new InkBrushSettings
        { radius = 24f, pigment = 1f, water = .22f, roughness = .25f,
          depletion = .002f, bristleSpread = .65f, bristleCount = 25 };
        public static InkBrushSettings Dry => new InkBrushSettings
        { radius = 19f, pigment = 1f, water = .08f, roughness = .58f,
          depletion = .07f, bristleSpread = 1f, bristleCount = 5 };
        public static InkBrushSettings Split => new InkBrushSettings
        { radius = 25f, pigment = 1f, water = .08f, roughness = .48f,
          depletion = .025f, bristleSpread = 1f, bristleCount = 3 };

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

    /// <summary>CPU brush input limits. Invalid commands are rejected before drawing or recording.</summary>
    public static class InkInput
    {
        public const float MaxCoordinate = 8192f;
        public const float MaxRadius = 256f;
        public const float MaxSegmentLength = 4096f;

        public static void ValidatePoint(InkPoint point)
        {
            Finite(point.x, "point.x"); Finite(point.y, "point.y");
            Finite(point.time, "point.time"); Finite(point.pressure, "point.pressure");
            if (point.pressure < 0f || point.pressure > 1f)
                throw new ArgumentOutOfRangeException(nameof(point), "Stored pressure must be within 0..1.");
            if (Math.Abs(point.x) > MaxCoordinate || Math.Abs(point.y) > MaxCoordinate)
                throw new ArgumentOutOfRangeException(nameof(point), "Coordinates must be within +/-8192 canvas pixels.");
            if (Math.Abs(point.time) > 1e9f)
                throw new ArgumentOutOfRangeException(nameof(point), "Time must be within +/-1e9 seconds.");
        }

        public static void ValidateSettings(InkBrushSettings settings)
        {
            Finite(settings.radius, "settings.radius"); Finite(settings.pigment, "settings.pigment");
            Finite(settings.water, "settings.water"); Finite(settings.roughness, "settings.roughness");
            Finite(settings.depletion, "settings.depletion"); Finite(settings.bristleSpread, "settings.bristleSpread");
            if (settings.radius > MaxRadius)
                throw new ArgumentOutOfRangeException(nameof(settings), "CPU brush radius must not exceed 256 pixels.");
        }

        public static void ValidateSegment(InkPoint from, InkPoint to)
        {
            ValidatePoint(to);
            double dx = (double)to.x - from.x, dy = (double)to.y - from.y;
            if (dx * dx + dy * dy > MaxSegmentLength * MaxSegmentLength)
                throw new ArgumentOutOfRangeException(nameof(to), "Split brush segments longer than 4096 pixels.");
        }

        static void Finite(float value, string name)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                throw new ArgumentOutOfRangeException(name, "Value must be finite.");
        }
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
