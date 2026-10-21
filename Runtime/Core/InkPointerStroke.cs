using System;
using System.Collections.Generic;

namespace ConnectPiece.SumiEngine.Core
{
    /// <summary>
    /// Mouse pressure adapter. Retains a short tail so lift-off tapers along the actual
    /// pointer path. Owns the core's active stroke until End; do not clear/replay it meanwhile.
    /// Recorded points contain the final pressure, so normal session replay needs no adapter.
    /// </summary>
    public sealed class InkPointerStroke
    {
        public const int MaximumBufferedPoints = 32;
        readonly SumiEngineCore core;
        readonly Queue<Sample> tail = new Queue<Sample>();
        // Accepted moves are >= .01 pixels, so the 2-pixel startup window holds at most 201 points.
        readonly List<InkPoint> pending = new List<InkPoint>();
        struct Sample { public InkPoint point; public float distance, pressureWithoutTaper; }
        InkPoint last;
        InkPoint initial;
        InkBrushSettings settings;
        int seed;
        bool taper, started;
        float distance, tailLength, leadLength;
        float pendingDistance;
        public bool Active { get; private set; }
        public int BufferedPointCount => tail.Count;

        public InkPointerStroke(SumiEngineCore core)
        { this.core = core ?? throw new ArgumentNullException(nameof(core)); }

        public void Begin(InkPoint point, InkBrushSettings brush, int strokeSeed, bool autoTaper = true)
        {
            InkInput.ValidatePoint(point);
            InkInput.ValidateSettings(brush);
            // Validate before ending an existing stroke.
            End();
            settings = brush; seed = strokeSeed; taper = autoTaper;
            initial = last = point; distance = pendingDistance = 0;
            pending.Clear(); started = false; Active = true;
            tailLength = Math.Max(4f, Math.Min(64f, brush.radius * 1.4f));
            leadLength = Math.Max(3f, Math.Min(48f, brush.radius));
            if (!taper) { core.BeginStroke(point, settings, seed); started = true; }
        }

        public void Move(InkPoint point)
        {
            InkInput.ValidatePoint(point);
            if (!Active) return;
            InkInput.ValidateSegment(last, point);
            float dx = point.x - last.x, dy = point.y - last.y;
            float length = (float)Math.Sqrt(dx * dx + dy * dy);
            if (length < .01f) return;
            if (!taper) { core.AppendPoint(point); last = point; return; }
            if (!started)
            {
                pending.Add(point); pendingDistance += length; last = point;
                if (pendingDistance < 2f) return;
                var start = initial; start.pressure *= .04f;
                core.BeginStroke(start, settings, seed); started = true;
                var previous = initial;
                foreach (var next in pending)
                {
                    AppendMotion(previous, next);
                    previous = next;
                }
                pending.Clear();
                return;
            }
            AppendMotion(last, point); last = point;
        }

        void AppendMotion(InkPoint from, InkPoint point)
        {
            float dx = point.x - from.x, dy = point.y - from.y;
            float length = (float)Math.Sqrt(dx * dx + dy * dy);
            // Resampling keeps a useful tail even when mouse events arrive far apart.
            int count = Math.Max(1, (int)Math.Ceiling(length / Math.Max(1f, tailLength / 12f)));
            float speed = length / Math.Max(.001f, point.time - from.time);
            float motionPressure = 1f - .45f * InkMath.Clamp01(speed / 1800f);
            for (int i = 1; i <= count; i++)
            {
                float t = i / (float)count;
                float along = distance + length * t;
                float lead = .04f + .96f * InkMath.Clamp01(along / leadLength);
                float inputPressure = InkMath.Lerp(from.pressure, point.pressure, t) * motionPressure;
                var sampled = new InkPoint(InkMath.Lerp(from.x, point.x, t),
                    InkMath.Lerp(from.y, point.y, t), InkMath.Lerp(from.time, point.time, t),
                    inputPressure * lead);
                tail.Enqueue(new Sample { point = sampled, distance = along, pressureWithoutTaper = inputPressure });
                while (tail.Count > MaximumBufferedPoints ||
                       (tail.Count > 1 && along - tail.Peek().distance >= tailLength))
                    core.AppendPoint(tail.Dequeue().point);
            }
            distance += length;
        }

        /// <summary>Call Move with the release position first, then End (also on focus loss).</summary>
        public void End()
        {
            if (!Active) return;
            if (!started) core.BeginStroke(initial, settings, seed); // Preserve taps including small hand jitter.
            pending.Clear();
            if (taper && tail.Count > 0)
            {
                float span = Math.Max(.001f, distance - tail.Peek().distance);
                bool shortStroke = distance < Math.Min(leadLength, tailLength) * .5f;
                while (tail.Count > 0)
                {
                    var sample = tail.Dequeue();
                    // Two overlapping taper ramps would erase a short mark. Keep its input load.
                    sample.point.pressure = shortStroke ? sample.pressureWithoutTaper :
                        sample.point.pressure * InkMath.Clamp01((distance - sample.distance) / span);
                    core.AppendPoint(sample.point);
                }
            }
            core.EndStroke(); Active = false; started = false;
        }
    }
}
