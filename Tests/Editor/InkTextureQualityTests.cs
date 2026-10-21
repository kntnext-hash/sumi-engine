using System;
using NUnit.Framework;
using ConnectPiece.SumiEngine.Core;

namespace ConnectPiece.SumiEngine.Tests
{
    public class InkTextureQualityTests
    {
        static SumiEngineCore Line(float roughness)
        {
            var core = new SumiEngineCore(160, 96);
            var style = InkBrushSettings.Dense;
            style.radius = 22; style.water = .05f; style.roughness = roughness;
            core.BeginStroke(new InkPoint(28, 48, 0), style, 731);
            core.AppendPoint(new InkPoint(132, 48, 1)); core.EndStroke();
            return core;
        }

        [Test]
        public void DryContactLeavesMorePaperInsideStrokeAndLessPigment()
        {
            var smooth = Line(0); var dry = Line(.95f);
            int smoothGaps = 0, dryGaps = 0;
            for (int y = 37; y < 59; y++)
                for (int x = 50; x < 110; x++)
                {
                    if (smooth.Paper.PigmentAt(x, y) < .01f) smoothGaps++;
                    if (dry.Paper.PigmentAt(x, y) < .01f) dryGaps++;
                }
            Assert.Greater(dryGaps, smoothGaps + 20, "Dry gaps must survive overlapping dabs in the stroke interior.");
            Assert.Less(dry.Paper.TotalPigment(), smooth.Paper.TotalPigment() * .8f);
            Assert.Greater(dry.Paper.TotalPigment(), 0);
        }

        [Test]
        public void DisplayDensityDarkensInkWithoutChangingDryContactOrMass()
        {
            var core = Line(.8f);
            float mass = core.Paper.TotalPigment();
            var pale = new byte[160 * 96 * 4]; var dense = new byte[pale.Length];
            core.Paper.RenderRgba(pale, 10, 10, 10, 240, 240, 240, 1);
            core.Paper.RenderRgba(dense, 10, 10, 10, 240, 240, 240, 8);
            int darkened = 0;
            for (int i = 0; i < pale.Length; i += 4)
            {
                Assert.LessOrEqual(dense[i], pale[i]);
                if (dense[i] < pale[i]) darkened++;
            }
            Assert.Greater(darkened, 100);
            Assert.AreEqual(mass, core.Paper.TotalPigment());
        }

        [Test]
        public void PoolHasVariableInteriorAndContinuousLayeringWithExactReplay()
        {
            var core = new SumiEngineCore(128, 128);
            var style = InkBrushSettings.Pool; style.radius = 36;
            core.BeginStroke(new InkPoint(64, 64, 0), style, 821); core.EndStroke();
            float low = float.MaxValue, high = 0;
            for (int y = 55; y <= 73; y++)
                for (int x = 55; x <= 73; x++)
                {
                    float p = core.Paper.PigmentAt(x, y);
                    low = Math.Min(low, p); high = Math.Max(high, p);
                }
            Assert.Greater(low, 0, "A pool interior must contain ink, not an empty ring.");
            Assert.Greater(high, low * 1.15f, "A pool interior should have spatially varied deposition.");
            float before = core.Paper.TotalPigment();
            core.BeginStroke(new InkPoint(67, 62, 1), style, 822); core.EndStroke();
            Assert.Greater(core.Paper.TotalPigment(), before);
            var replay = new SumiEngineCore(128, 128); replay.Replay(core.Session);
            for (int y = 0; y < 128; y++)
                for (int x = 0; x < 128; x++)
                {
                    float p = core.Paper.PigmentAt(x, y);
                    Assert.IsFalse(float.IsNaN(p) || float.IsInfinity(p));
                    Assert.GreaterOrEqual(p, 0);
                    Assert.AreEqual(p, replay.Paper.PigmentAt(x, y));
                }
        }
    }
}
