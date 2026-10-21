using System;
using System.Collections.Generic;

namespace ConnectPiece.SumiEngine.Core
{
    /// <summary>Portable numerical core, with optional re-playable stroke history.</summary>
    public sealed class SumiEngineCore
    {
        public InkPaper Paper { get; }
        public InkSession Session { get; private set; }
        readonly InkBrush brush;
        InkStrokeRecord active;
        public SumiEngineCore(int width, int height, int paperSeed = 2026)
        {
            Paper = new InkPaper(width, height, paperSeed);
            brush = new InkBrush(Paper);
            Session = new InkSession { width = width, height = height, paperSeed = paperSeed };
        }
        public void BeginStroke(InkPoint start, InkBrushSettings settings, int seed)
        {
            InkInput.ValidatePoint(start);
            InkInput.ValidateSettings(settings);
            if (active != null) EndStroke();
            active = new InkStrokeRecord { seed = seed, brush = settings };
            active.points.Add(start);
            brush.Begin(start, settings, seed);
        }
        public void AppendPoint(InkPoint point)
        {
            InkInput.ValidatePoint(point);
            if (active == null) return;
            // Avoid an infinite JSON history of redundant pointer events.
            var points = active.points;
            InkPoint prev = points[points.Count - 1];
            InkInput.ValidateSegment(prev, point);
            if (Math.Abs(point.x - prev.x) + Math.Abs(point.y - prev.y) < .01f) return;
            brush.Move(point);
            points.Add(point);
        }
        public void EndStroke()
        {
            if (active == null) return;
            brush.End();
            Session.strokes.Add(active);
            active = null;
        }
        public void Step(float fixedDelta) => Paper.Step(fixedDelta);
        public void Clear()
        {
            if (active != null) EndStroke();
            Paper.Clear(); Session.strokes.Clear();
        }
        public void Replay(InkSession source)
        {
            if (source == null || source.width != Paper.Width || source.height != Paper.Height ||
                source.paperSeed != Paper.Seed)
                throw new ArgumentException("Session dimensions and paper seed must match canvas.");
            if (source.strokes == null || source.version != 1)
                throw new ArgumentException("Session must contain strokes and use version 1.", nameof(source));
            // Validate the entire replay before Clear, so a malformed record preserves the canvas.
            foreach (var record in source.strokes)
            {
                if (record == null || record.points == null)
                    throw new ArgumentException("Session contains a null stroke or point list.", nameof(source));
                InkInput.ValidateSettings(record.brush);
                for (int i = 0; i < record.points.Count; i++)
                {
                    InkInput.ValidatePoint(record.points[i]);
                    if (i > 0) InkInput.ValidateSegment(record.points[i - 1], record.points[i]);
                }
            }
            // Replay may be called with this engine's own session.
            var originals = new List<InkStrokeRecord>(source.strokes);
            Clear();
            foreach (var record in originals)
            {
                if (record.points == null || record.points.Count == 0) continue;
                BeginStroke(record.points[0], record.brush, record.seed);
                for (int i = 1; i < record.points.Count; ++i) AppendPoint(record.points[i]);
                EndStroke();
            }
        }
        public void DrawBezier(InkPoint a, InkPoint b, InkPoint c, InkPoint d,
                               InkBrushSettings style, int seed, int steps = 48)
        {
            InkInput.ValidatePoint(a); InkInput.ValidatePoint(b);
            InkInput.ValidatePoint(c); InkInput.ValidatePoint(d);
            InkInput.ValidateSettings(style);
            steps = Math.Max(2, Math.Min(4096, steps));
            var sampled = new List<InkPoint>(steps);
            InkPoint previous = a;
            for (int i = 1; i <= steps; i++)
            {
                float t = i / (float)steps, q = 1f - t;
                float w0 = q*q*q, w1 = 3f*q*q*t, w2 = 3f*q*t*t, w3 = t*t*t;
                var point = new InkPoint(w0*a.x + w1*b.x + w2*c.x + w3*d.x,
                                         w0*a.y + w1*b.y + w2*c.y + w3*d.y,
                                         InkMath.Lerp(a.time, d.time, t),
                                         w0*a.pressure + w1*b.pressure + w2*c.pressure + w3*d.pressure);
                InkInput.ValidateSegment(previous, point);
                sampled.Add(point); previous = point;
            }
            BeginStroke(a, style, seed);
            foreach (var point in sampled) AppendPoint(point);
            EndStroke();
        }
    }
}
