using System;

namespace ConnectPiece.SumiEngine.Core
{
    /// <summary>
    /// Fixed-size CPU wet-paper simulation, a visual approximation (not real fluid mechanics).
    /// Mutable arrays stay private, with fixed/dry and suspended/wet pigment conserved up to clamping.
    /// </summary>
    public sealed class InkPaper
    {
        public int Width { get; }
        public int Height { get; }
        public int Seed { get; }
        readonly float[] fixedPigment, wetPigment, moisture, nextPigment, nextWater;
        readonly float[] grain;
        public float Diffusion = .95f;       // cells^2 / second
        public float WaterDiffusion = 1.5f;  // cells^2 / second
        public float Evaporation = .18f;     // 1 / second
        public float Binding = .75f;         // 1 / second
        public bool IsDirty { get; private set; } = true;

        public InkPaper(int width, int height, int paperSeed = 2026)
        {
            if (width < 16 || height < 16 || width > 2048 || height > 2048)
                throw new ArgumentOutOfRangeException(nameof(width), "Canvas must be 16..2048 on both axes.");
            Width = width; Height = height; Seed = paperSeed;
            int size = width * height;
            fixedPigment = new float[size]; wetPigment = new float[size]; moisture = new float[size];
            nextPigment = new float[size]; nextWater = new float[size]; grain = new float[size];
            for (int y = 0; y < height; ++y)
                for (int x = 0; x < width; ++x)
                {
                    int i = y * width + x;
                    float raw = InkMath.Noise01(x / 2, y / 3, Seed);
                    grain[i] = .75f + .25f * raw;
                }
        }

        public void Clear()
        {
            Array.Clear(fixedPigment, 0, fixedPigment.Length);
            Array.Clear(wetPigment, 0, wetPigment.Length);
            Array.Clear(moisture, 0, moisture.Length);
            Array.Clear(nextPigment, 0, nextPigment.Length);
            Array.Clear(nextWater, 0, nextWater.Length);
            IsDirty = true;
        }

        /// <summary>A single soft deposited bristle. Clipped to canvas; coordinates may be outside.</summary>
        public void Deposit(float px, float py, float radius, float pigment, float water, float roughness, int seed)
        {
            if (radius <= 0f || pigment <= 0f) return;
            int x0 = Math.Max(0, (int)Math.Floor(px - radius));
            int x1 = Math.Min(Width - 1, (int)Math.Ceiling(px + radius));
            int y0 = Math.Max(0, (int)Math.Floor(py - radius));
            int y1 = Math.Min(Height - 1, (int)Math.Ceiling(py + radius));
            float invR2 = 1f / (radius * radius);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float dx = x + .5f - px, dy = y + .5f - py;
                    float d2 = (dx * dx + dy * dy) * invR2;
                    if (d2 >= 1f) continue;
                    int i = y * Width + x;
                    float edge = (1f - d2); edge *= edge;
                    float gaps = InkMath.Noise01(x, y, seed);
                    // Fibrous dry brushing: gaps persist across frames because the noise is spatial.
                    float coverage = InkMath.Clamp01((gaps - roughness * (1f - water) * .7f) * 1.8f);
                    float amount = pigment * edge * coverage * grain[i];
                    wetPigment[i] = InkMath.Min(8f, wetPigment[i] + amount);
                    moisture[i] = InkMath.Min(1f, moisture[i] + water * edge * .17f);
                }
            IsDirty = true;
        }

        public void Step(float dt)
        {
            if (dt <= 0f) return;
            // Substeps keep a positive 4-neighbour explicit diffusion stencil.
            int n = Math.Max(1, (int)Math.Ceiling(dt * Math.Max(Diffusion, WaterDiffusion) * 5f));
            float h = dt / n;
            int len = Width * Height;
            for (int sub = 0; sub < n; sub++)
            {
                for (int y = 0; y < Height; y++)
                    for (int x = 0; x < Width; x++)
                    {
                        int i = y * Width + x;
                        int l = x > 0 ? i - 1 : i, r = x + 1 < Width ? i + 1 : i;
                        int d = y > 0 ? i - Width : i, u = y + 1 < Height ? i + Width : i;
                        float water = moisture[i];
                        float lapW = moisture[l] + moisture[r] + moisture[u] + moisture[d] - 4f * water;
                        float p = wetPigment[i];
                        // Conservative pigment flux: flux on each shared edge uses the same
                        // average moisture in both directions, preventing pigment gain/loss
                        // from an uneven wetness field (apart from float round-off).
                        float fluxP = .5f * (water + moisture[l]) * (wetPigment[l] - p)
                                    + .5f * (water + moisture[r]) * (wetPigment[r] - p)
                                    + .5f * (water + moisture[u]) * (wetPigment[u] - p)
                                    + .5f * (water + moisture[d]) * (wetPigment[d] - p);
                        float newWater = InkMath.Max(0f, water + h * (WaterDiffusion * lapW - Evaporation * water));
                        float newPigment = InkMath.Max(0f, p + h * Diffusion * fluxP);
                        float bind = InkMath.Min(newPigment, h * Binding * (0.18f + water) * newPigment);
                        nextPigment[i] = newPigment - bind;
                        nextWater[i] = newWater;
                        fixedPigment[i] += bind;
                    }
                Array.Copy(nextPigment, wetPigment, len);
                Array.Copy(nextWater, moisture, len);
            }
            IsDirty = true;
        }

        public float PigmentAt(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height) return 0;
            int i = y * Width + x;
            return fixedPigment[i] + wetPigment[i];
        }
        public float MoistureAt(int x, int y) => x < 0 || y < 0 || x >= Width || y >= Height
            ? 0f : moisture[y * Width + x];
        public float TotalPigment()
        {
            float sum = 0f;
            for (int i = 0; i < wetPigment.Length; i++) sum += fixedPigment[i] + wetPigment[i];
            return sum;
        }
        public void MarkRendered() { IsDirty = false; }

        /// <summary>RGBA pixels on an ivory paper surface. Caller owns output buffer.</summary>
        public void RenderRgba(byte[] output)
        {
            if (output == null || output.Length != Width * Height * 4)
                throw new ArgumentException("Required RGBA8888 byte buffer of Width*Height*4.");
            for (int i = 0; i < fixedPigment.Length; i++)
            {
                float ink = fixedPigment[i] + wetPigment[i];
                float a = 1f - InkMath.Exp(-.9f * ink);
                float brightness = (grain[i] - .75f) * 13f;
                int p = i * 4;
                output[p] = (byte)Math.Max(0, Math.Min(255, (int)(243f + brightness - 214f * a)));
                output[p + 1] = (byte)Math.Max(0, Math.Min(255, (int)(240f + brightness - 210f * a)));
                output[p + 2] = (byte)Math.Max(0, Math.Min(255, (int)(231f + brightness - 200f * a)));
                output[p + 3] = 255;
            }
            IsDirty = false;
        }
    }
}
