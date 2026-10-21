using System;

namespace ConnectPiece.SumiEngine.Core
{
    /// <summary>Original deterministic stroke study, not a reproduction of reference artwork.
    /// All marks are recorded brush input, so the study can be replayed before diffusion.</summary>
    public static class InkStudy
    {
        public static void Draw(SumiEngineCore core, int seed = 731)
        {
            if (core == null) throw new ArgumentNullException(nameof(core));
            core.Clear();
            float sx = core.Paper.Width / 768f, sy = core.Paper.Height / 1024f;
            for (int cell = 0; cell < 9; cell++)
            {
                float cx = 128 + cell % 3 * 256, cy = 850 - cell / 3 * 310;
                var style = cell % 3 == 0 ? InkBrushSettings.Dense : cell % 3 == 1 ? InkBrushSettings.Dry : InkBrushSettings.Split;
                style.radius *= Math.Min(sx, sy) * 1.5f;
                int strokes = cell < 3 ? 3 : 1;
                for (int line = 0; line < strokes; line++)
                {
                    for (int i = 0; i <= 120; i++)
                    {
                        float t = i / 120f, x, y, p;
                        if (cell < 3)
                        { x = cx - 78 + line * 62 + 35 * t; y = cy - 90 + 180 * t; p = (float)Math.Sin(Math.PI * t); }
                        else if (cell < 6)
                        { double a = (.12 + t * 1.85) * Math.PI; x = cx + (float)Math.Cos(a) * 86; y = cy + (float)Math.Sin(a) * 96; p = .15f + .85f * (float)Math.Sin(Math.PI * t); }
                        else
                        { x = cx - 100 + 200 * t; y = cy - 90 + 160 * t + 45 * (float)Math.Sin(t * Math.PI); p = (float)Math.Pow(Math.Sin(Math.PI * t), 1.8); }
                        var point = new InkPoint(x * sx, y * sy, t * 2f, p);
                        if (i == 0) core.BeginStroke(point, style, seed + cell * 7 + line);
                        else core.AppendPoint(point);
                    }
                    core.EndStroke();
                }
            }
        }
    }
}
