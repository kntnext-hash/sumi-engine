using System;

namespace ConnectPiece.SumiEngine.Core
{
    /// <summary>
    /// Lightweight virtual-bristle brush. Bends and dries as it moves, depositing a row
    /// of independent soft dabs. This is an intentionally stylized, not physical, model.
    /// </summary>
    public sealed class InkBrush
    {
        readonly InkPaper paper;
        InkBrushSettings settings;
        InkPoint last;
        float[] reservoir, wobble, bristleLag;
        float normalX = 0f, normalY = 1f;
        int seed;
        public bool Active { get; private set; }

        public InkBrush(InkPaper paper) { this.paper = paper ?? throw new ArgumentNullException(nameof(paper)); }

        public void Begin(InkPoint start, InkBrushSettings brush, int strokeSeed)
        {
            if (Active) End();
            settings = brush; settings.bristleCount = Math.Max(3, Math.Min(31, brush.bristleCount));
            settings.radius = Math.Max(.6f, brush.radius);
            settings.pigment = InkMath.Clamp01(brush.pigment);
            settings.water = InkMath.Clamp01(brush.water);
            settings.roughness = InkMath.Clamp01(brush.roughness);
            settings.bristleSpread = InkMath.Clamp01(brush.bristleSpread);
            seed = strokeSeed; last = start;
            reservoir = new float[settings.bristleCount];
            wobble = new float[settings.bristleCount];
            bristleLag = new float[settings.bristleCount];
            for (int i = 0; i < reservoir.Length; i++)
            {
                reservoir[i] = .7f + .3f * InkMath.Noise01(i, 0, seed);
                wobble[i] = (InkMath.Noise01(i, 23, seed) - .5f) * .32f;
            }
            Active = true;
            DrawDab(start.x, start.y, start.pressure, 0f, 0);
        }

        public void Move(InkPoint next)
        {
            if (!Active) return;
            float dx = next.x - last.x, dy = next.y - last.y;
            float dist = InkMath.Sqrt(dx * dx + dy * dy);
            if (dist < .0001f) { last = next; return; }
            float seconds = InkMath.Max(.001f, next.time - last.time);
            float speed = dist / seconds;
            float targetNX = -dy / dist, targetNY = dx / dist;
            // Brush head follows direction with a small amount of inertia.
            float response = InkMath.Clamp01(.3f + .5f * seconds * 60f);
            normalX = InkMath.Lerp(normalX, targetNX, response);
            normalY = InkMath.Lerp(normalY, targetNY, response);
            float nl = InkMath.Sqrt(normalX * normalX + normalY * normalY);
            if (nl > .001f) { normalX /= nl; normalY /= nl; }
            int count = Math.Max(1, (int)Math.Ceiling(dist / InkMath.Max(.6f, settings.radius * .23f)));
            for (int j = 1; j <= count; j++)
            {
                float t = j / (float)count;
                float p = InkMath.Lerp(last.pressure, next.pressure, t);
                // Fast motion creates a slight taper, but preserves user pressure control.
                p *= 1f - .17f * InkMath.Clamp01(speed / 1500f);
                DrawDab(InkMath.Lerp(last.x, next.x, t), InkMath.Lerp(last.y, next.y, t), p, speed, j);
            }
            last = next;
        }

        void DrawDab(float x, float y, float pressure, float speed, int step)
        {
            float radius = settings.radius * (.17f + .83f * InkMath.Sqrt(InkMath.Clamp01(pressure)));
            int n = reservoir.Length;
            float velocityStress = InkMath.Clamp01(speed / 1400f);
            for (int i = 0; i < n; i++)
            {
                float relative = n == 1 ? 0f : 2f * i / (n - 1f) - 1f;
                bristleLag[i] = InkMath.Lerp(bristleLag[i], wobble[i] * velocityStress, .13f);
                float offset = relative * settings.bristleSpread + bristleLag[i];
                float bristleRadius = radius * (.33f - .11f * Math.Abs(relative));
                float coverage = (1f - .4f * Math.Abs(relative)) * reservoir[i];
                float d = settings.pigment * coverage * (.15f + .18f / n);
                float amount = d * (1f - .42f * velocityStress);
                paper.Deposit(x + normalX * offset * radius,
                              y + normalY * offset * radius,
                              InkMath.Max(.45f, bristleRadius), amount,
                              settings.water, settings.roughness, seed + i * 31);
                reservoir[i] = InkMath.Max(.12f, reservoir[i] -
                               InkMath.Max(0f, settings.depletion) * (.2f + speed * .00025f) / n);
            }
        }

        public void End() { Active = false; }
    }
}
