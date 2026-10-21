using System;

namespace ConnectPiece.SumiEngine.Core
{
    public sealed partial class InkPaper
    {
        // Procedural deposition, followed by the ordinary conservative wet-paper solver.
        // Pool rims and feathering are stylized deposition profiles, not a fluid simulation.
        internal void DepositTexture(float px, float py, float radius, float pigment,
            InkBrushSettings style, float nx, float ny, int seed)
        {
            if (radius <= .05f || pigment <= 0f) return;
            float extent = radius * 1.4f;
            int x0 = Math.Max(0, (int)Math.Floor(px - extent));
            int x1 = Math.Min(Width - 1, (int)Math.Ceiling(px + extent));
            int y0 = Math.Max(0, (int)Math.Floor(py - extent));
            int y1 = Math.Min(Height - 1, (int)Math.Ceiling(py + extent));
            bool fiber = style.texture == InkTexture.Fiber;
            float dry = style.roughness * (1f - style.water);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float dx = x + .5f - px, dy = y + .5f - py;
                    float r = InkMath.Sqrt(dx * dx + dy * dy) / radius;
                    float coarse = InkMath.SmoothNoise(x / 19f, y / 19f, seed);
                    float medium = InkMath.SmoothNoise(x / 6f, y / 6f, seed + 11);
                    float fine = InkMath.SmoothNoise(x / 1.7f, y / 1.7f, seed + 19);
                    float cloud = .55f * coarse + .3f * medium + .15f * fine;
                    float boundary = fiber ? .94f + .12f * medium + .06f * (fine - .5f) : .72f + .65f * cloud;
                    float q = r / boundary;
                    if (q >= 1f) continue;
                    float coverage;
                    if (fiber)
                    {
                        float across = (dx * nx + dy * ny) / radius;
                        float along = (-x * ny + y * nx) / 32f;
                        float hairs = InkMath.SmoothNoise(across * 34f, along, seed + 41);
                        float bundles = InkMath.SmoothNoise(across * style.bristleCount, along * .3f, seed + 73);
                        coverage = InkMath.Clamp01((hairs - dry * .73f) * 3.5f);
                        if (style.bristleCount <= 5)
                            coverage *= InkMath.Clamp01((bundles - .36f) * 5f);
                        // Contact belongs to the paper, so overlapping dabs cannot fill all
                        // dry gaps. An anisotropic field makes fine longitudinal breaks,
                        // while the isotropic field leaves irregular exposed paper islands.
                        float strands = InkMath.SmoothNoise((x * nx + y * ny) / 1.3f,
                            (-x * ny + y * nx) / 17f, seed + 89);
                        float contact = .65f * strands + .35f * fine;
                        float threshold = .08f + .67f * dry;
                        coverage *= InkMath.Clamp01((contact - threshold) * 7f);
                        coverage *= InkMath.Clamp01((1f - q) * (7f + 5f * medium));
                    }
                    else if (style.texture == InkTexture.Pool)
                    {
                        // A broad, interrupted accumulation band avoids a uniform drawn
                        // outline. Uneven interior deposits remain visible under layering.
                        float rimPosition = .77f + .1f * medium;
                        float rim = InkMath.Clamp01(1f - Math.Abs(q - rimPosition) / .2f);
                        float interior = .2f + .85f * cloud * cloud + .22f * medium;
                        float rimLoad = rim * (.12f + .38f * coarse);
                        coverage = (interior + rimLoad) * InkMath.Clamp01((1f - q) * 13f);
                    }
                    else
                    {
                        float edge = InkMath.Clamp01((1f - q) * (style.texture == InkTexture.Mist ? 2.2f : 8f));
                        coverage = edge * (.12f + 1.5f * cloud * cloud);
                        coverage *= .7f + .3f * fine;
                    }
                    int i = y * Width + x;
                    float amount = pigment * coverage * grain[i];
                    wetPigment[i] = InkMath.Min(8f, wetPigment[i] + amount);
                    moisture[i] = InkMath.Min(1f, moisture[i] + style.water * coverage * .35f);
                    if (amount > 0f) HasActiveInk = true;
                }
            IsDirty = true;
        }
    }
}
