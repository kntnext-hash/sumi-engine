using NUnit.Framework;
using ConnectPiece.SumiEngine.Core;

namespace ConnectPiece.SumiEngine.Tests
{
    public class InkCoreTests
    {
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void LegacyBrushWithZeroPressureDoesNotDeposit(int preset)
        {
            var brush = preset == 0 ? InkBrushSettings.Vein : preset == 1 ? InkBrushSettings.Slash : InkBrushSettings.Bloom;
            var core = new SumiEngineCore(64,64);
            core.BeginStroke(new InkPoint(10,10,0,0),brush,1);
            core.AppendPoint(new InkPoint(50,50,1,0));core.EndStroke();
            Assert.AreEqual(0,core.Paper.TotalPigment());
        }
        [TestCase(InkTexture.Fiber)]
        [TestCase(InkTexture.Bleed)]
        [TestCase(InkTexture.Pool)]
        [TestCase(InkTexture.Mist)]
        [TestCase(InkTexture.Splatter)]
        public void TexturedBrushReplaysAndConservesPigment(InkTexture texture)
        {
            var a=new SumiEngineCore(64,64);var b=new SumiEngineCore(64,64);
            var brush=InkBrushSettings.Bleed;brush.texture=texture;brush.radius=9;
            a.BeginStroke(new InkPoint(8,12,0,.4f),brush,-17);
            a.AppendPoint(new InkPoint(48,44,.5f,1));a.EndStroke();
            b.Replay(a.Session);
            var first=new byte[64*64*4];var second=new byte[first.Length];
            a.Paper.RenderRgba(first);b.Paper.RenderRgba(second);
            CollectionAssert.AreEqual(first,second);
            float before=a.Paper.TotalPigment();Assert.Greater(before,0);
            for(int i=0;i<20;i++)a.Step(.1f);
            Assert.That(a.Paper.TotalPigment(),Is.EqualTo(before).Within(before*.002f+.01f));
            for(int y=0;y<64;y++)for(int x=0;x<64;x++)
                Assert.That(a.Paper.PigmentAt(x,y),Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void EffectsStudyReplaysAndDifferentSeedsDiffer()
        {
            var a=new SumiEngineCore(192,256);var b=new SumiEngineCore(192,256);
            InkStudy.DrawEffects(a,731);b.Replay(a.Session);
            var first=new byte[192*256*4];var second=new byte[first.Length];
            a.Paper.RenderRgba(first);b.Paper.RenderRgba(second);
            Assert.AreEqual(50,a.Session.strokes.Count);CollectionAssert.AreEqual(first,second);
            InkStudy.DrawEffects(b,732);b.Paper.RenderRgba(second);
            CollectionAssert.AreNotEqual(first,second);
        }

        [Test]
        public void TexturedZeroPressureLeavesNoInkAndInvalidModePreservesCanvas()
        {
            var core=new SumiEngineCore(64,64);var brush=InkBrushSettings.Dense;
            core.BeginStroke(new InkPoint(16,16,0,0),brush,7);
            core.AppendPoint(new InkPoint(40,40,1,0));core.EndStroke();
            Assert.AreEqual(0,core.Paper.TotalPigment());
            brush.texture=(InkTexture)999;
            Assert.Throws<System.ArgumentOutOfRangeException>(()=>core.BeginStroke(new InkPoint(20,20,0),brush,7));
            Assert.AreEqual(1,core.Session.strokes.Count);
        }

        [Test]
        public void PoolHasDenserRimAndSplatterHasSatelliteDroplets()
        {
            var core=new SumiEngineCore(128,128);var brush=InkBrushSettings.Pool;brush.radius=30;
            core.BeginStroke(new InkPoint(64,64,0),brush,731);core.EndStroke();
            float inner=0,outer=0;int ni=0,no=0;
            for(int y=0;y<128;y++)for(int x=0;x<128;x++)
            {
                float r=(float)System.Math.Sqrt((x-64)*(x-64)+(y-64)*(y-64));
                if(r<10){inner+=core.Paper.PigmentAt(x,y);ni++;}
                if(r>22&&r<27){outer+=core.Paper.PigmentAt(x,y);no++;}
            }
            Assert.Greater(outer/no,inner/ni);
            core.Clear();brush=InkBrushSettings.Splatter;brush.radius=20;
            core.BeginStroke(new InkPoint(64,64,0),brush,731);core.EndStroke();
            float satellites=0;
            for(int y=0;y<128;y++)for(int x=0;x<128;x++)
                if((x-64)*(x-64)+(y-64)*(y-64)>25*25)satellites+=core.Paper.PigmentAt(x,y);
            Assert.Greater(satellites,0);
        }

        [Test]
        public void StudyReplaysExactlyAndSeedChangesTexture()
        {
            var a = new SumiEngineCore(192, 256);
            var b = new SumiEngineCore(192, 256);
            InkStudy.Draw(a, 731); b.Replay(a.Session);
            var first = new byte[192 * 256 * 4]; var replay = new byte[first.Length];
            a.Paper.RenderRgba(first); b.Paper.RenderRgba(replay);
            Assert.AreEqual(15, a.Session.strokes.Count);
            CollectionAssert.AreEqual(first, replay);
            InkStudy.Draw(b, 732); b.Paper.RenderRgba(replay);
            CollectionAssert.AreNotEqual(first, replay);
        }

        [Test]
        public void PaletteRecolorsWithoutChangingPigmentAndDefaultRemainsCompatible()
        {
            var paper = new InkPaper(32, 32);
            paper.Deposit(16, 16, 8, .8f, .8f, .2f, 3);
            float pigment = paper.TotalPigment();
            var original = new byte[32 * 32 * 4]; var explicitDefault = new byte[original.Length];
            var red = new byte[original.Length];
            paper.RenderRgba(original);
            paper.RenderRgba(explicitDefault, 29, 30, 31, 243, 240, 231, .9f);
            paper.RenderRgba(red, 220, 10, 10, 243, 240, 231, 8f);
            CollectionAssert.AreEqual(original, explicitDefault);
            Assert.AreEqual(pigment, paper.TotalPigment());
            int center = 0;
            float maximum = 0f;
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                    if (paper.PigmentAt(x, y) > maximum)
                    { maximum = paper.PigmentAt(x, y); center = (y * 32 + x) * 4; }
            Assert.Greater(maximum, 0f);
            Assert.Greater(red[center] - red[center + 1], original[center] - original[center + 1]);
            CollectionAssert.AreEqual(new[] {original[0], original[1], original[2]}, new[] {red[0], red[1], red[2]});
            Assert.AreEqual(255, red[center + 3]);
        }

        [Test]
        public void PaletteSupportsLightInkOnDarkPaperAndRejectsBadDensity()
        {
            var paper = new InkPaper(32, 32);
            paper.Deposit(16, 16, 8, 1f, 0f, 0f, 3);
            var pixels = new byte[32 * 32 * 4];
            paper.RenderRgba(pixels, 255, 255, 255, 0, 0, 0, 12f);
            Assert.Greater(pixels[(16 * 32 + 16) * 4], pixels[0]);
            var before = (byte[])pixels.Clone();
            Assert.Throws<System.ArgumentOutOfRangeException>(() => paper.RenderRgba(pixels, 0, 0, 0, 255, 255, 255, float.NaN));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => paper.RenderRgba(pixels, 0, 0, 0, 255, 255, 255, -1));
            CollectionAssert.AreEqual(before, pixels);
        }

        [Test]
        public void BlankPaperDoesNotBecomeDirtyOnSimulationStep()
        {
            var paper = new InkPaper(32, 32);
            paper.RenderRgba(new byte[32 * 32 * 4]);
            paper.Step(.1f);
            Assert.IsFalse(paper.HasActiveInk);
            Assert.IsFalse(paper.IsDirty);
        }

        [Test]
        public void SettledPaperConservesPigmentStopsAndReactivates()
        {
            var paper = new InkPaper(32, 32);
            paper.Deposit(16, 16, 5, .7f, .8f, .2f, 3);
            float before = paper.TotalPigment();
            int steps = 0;
            while (paper.HasActiveInk && steps++ < 2000) paper.Step(.1f);
            Assert.IsFalse(paper.HasActiveInk, "Must settle within 200 simulated seconds");
            Assert.That(paper.TotalPigment(), Is.EqualTo(before).Within(before * .002f + .01f));
            var a = new byte[32 * 32 * 4]; var b = new byte[a.Length];
            paper.RenderRgba(a);
            for (int i = 0; i < 100; i++) paper.Step(.1f);
            Assert.IsFalse(paper.IsDirty);
            paper.RenderRgba(b); CollectionAssert.AreEqual(a, b);
            paper.Deposit(16, 16, 5, .7f, .8f, .2f, 4);
            Assert.IsTrue(paper.HasActiveInk);
            paper.Clear(); Assert.IsFalse(paper.HasActiveInk);
        }

        [Test]
        public void InvalidBeginAndAppendPreserveActiveStrokeAndPixels()
        {
            var engine = new SumiEngineCore(64, 64);
            engine.BeginStroke(new InkPoint(32, 32, 0), InkBrushSettings.Vein, 1);
            float before = engine.Paper.TotalPigment();
            var bad = new InkPoint { x = float.NaN, y = 32, time = 0, pressure = 1 };
            Assert.Throws<System.ArgumentOutOfRangeException>(() => engine.BeginStroke(bad, InkBrushSettings.Vein, 2));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => engine.AppendPoint(bad));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => engine.AppendPoint(new InkPoint(float.MaxValue, 0, 0)));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => engine.AppendPoint(new InkPoint(8192, 0, 0)));
            var settings = InkBrushSettings.Vein; settings.radius = float.PositiveInfinity;
            Assert.Throws<System.ArgumentOutOfRangeException>(() => engine.BeginStroke(new InkPoint(1, 1, 0), settings, 2));
            Assert.AreEqual(before, engine.Paper.TotalPigment());
            Assert.AreEqual(0, engine.Session.strokes.Count);
            engine.AppendPoint(new InkPoint(40, 32, .1f)); engine.EndStroke();
            Assert.AreEqual(1, engine.Session.strokes.Count);
            Assert.AreEqual(1, engine.Session.strokes[0].seed);
            Assert.AreEqual(2, engine.Session.strokes[0].points.Count);
        }

        [Test]
        public void InvalidDirectBrushCommandPreservesActiveBrush()
        {
            var paper = new InkPaper(64, 64);
            var brush = new InkBrush(paper);
            brush.Begin(new InkPoint(32, 32, 0), InkBrushSettings.Vein, 1);
            float before = paper.TotalPigment();
            var bad = new InkPoint { x = 32, y = 32, time = float.NaN, pressure = 1 };
            Assert.Throws<System.ArgumentOutOfRangeException>(() => brush.Begin(bad, InkBrushSettings.Vein, 2));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => brush.Move(bad));
            Assert.IsTrue(brush.Active);
            Assert.AreEqual(before, paper.TotalPigment());
            brush.Move(new InkPoint(40, 32, .1f));
            Assert.Greater(paper.TotalPigment(), before);
        }

        [Test]
        public void InvalidReplayAndBezierPreserveHistoryAndPixels()
        {
            var engine = new SumiEngineCore(64, 64);
            engine.BeginStroke(new InkPoint(32, 32, 0), InkBrushSettings.Vein, 1); engine.EndStroke();
            float before = engine.Paper.TotalPigment();
            var invalid = new InkSession { width = 64, height = 64, paperSeed = 2026 };
            invalid.strokes.Add(new InkStrokeRecord { brush = InkBrushSettings.Vein });
            invalid.strokes[0].points.Add(new InkPoint(float.MaxValue, 0, 0));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => engine.Replay(invalid));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => engine.DrawBezier(new InkPoint(0, 0, 0), new InkPoint(float.MaxValue, 0, 0), new InkPoint(0, 0, 0), new InkPoint(0, 0, 0), InkBrushSettings.Vein, 1));
            Assert.AreEqual(before, engine.Paper.TotalPigment());
            Assert.AreEqual(1, engine.Session.strokes.Count);
        }

        [Test]
        public void ReplayOwnSessionReproducesPixelsAfterVerticalStroke()
        {
            var engine = new SumiEngineCore(64, 64);
            engine.BeginStroke(new InkPoint(32, 10, 0), InkBrushSettings.Slash, 100);
            engine.AppendPoint(new InkPoint(32, 50, .2f)); engine.EndStroke();
            var before = new byte[64 * 64 * 4]; var after = new byte[before.Length];
            engine.Paper.RenderRgba(before);
            engine.Replay(engine.Session);
            engine.Paper.RenderRgba(after);
            CollectionAssert.AreEqual(before, after);
        }

        [Test]
        public void ClearRestoresFreshBrushOrientation()
        {
            var used = new SumiEngineCore(64, 64);
            used.BeginStroke(new InkPoint(32, 10, 0), InkBrushSettings.Slash, 100);
            used.AppendPoint(new InkPoint(32, 50, .2f)); used.EndStroke(); used.Clear();
            var fresh = new SumiEngineCore(64, 64);
            foreach (var engine in new[] { used, fresh })
            {
                engine.BeginStroke(new InkPoint(32, 32, 0), InkBrushSettings.Slash, 100);
                engine.EndStroke();
            }
            var a = new byte[64 * 64 * 4]; var b = new byte[a.Length];
            used.Paper.RenderRgba(a); fresh.Paper.RenderRgba(b);
            CollectionAssert.AreEqual(a, b);
        }

        [Test]
        public void NegativeWaterIsClampedBeforeDeposit()
        {
            var paper = new InkPaper(32, 32);
            paper.Deposit(16, 16, 8, .8f, -10, .2f, 3);
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++) Assert.AreEqual(0f, paper.MoistureAt(x, y));
        }

        [Test]
        public void NegativeSimulationRateIsRejectedBeforeMutation()
        {
            var paper = new InkPaper(32, 32);
            paper.Deposit(16, 16, 8, .8f, .8f, .2f, 3);
            float before = paper.TotalPigment();
            paper.Binding = -1;
            Assert.Throws<System.ArgumentOutOfRangeException>(() => paper.Step(.1f));
            Assert.AreEqual(before, paper.TotalPigment());
        }

        [Test]
        public void NonfiniteInputsAndExcessiveStepAreRejected()
        {
            var paper = new InkPaper(32, 32);
            Assert.Throws<System.ArgumentOutOfRangeException>(() => paper.Deposit(float.NaN, 16, 8, .8f, .8f, .2f, 3));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => paper.Step(float.PositiveInfinity));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => paper.Step(float.MaxValue));
            Assert.AreEqual(0f, paper.TotalPigment());
        }

        [Test]
        public void ExtremeFiniteRadiusDepositsWithoutOverflow()
        {
            var paper = new InkPaper(32, 32);
            paper.Deposit(16, 16, float.MaxValue, .8f, .8f, .2f, 3);
            Assert.Greater(paper.TotalPigment(), 0f);
            Assert.IsFalse(float.IsNaN(paper.TotalPigment()));
        }

        [Test]
        public void BeginStrokeDepositsPigment()
        {
            var e = new SumiEngineCore(64, 64);
            e.BeginStroke(new InkPoint(32, 32, 0f, 1f), InkBrushSettings.Vein, 12);
            e.AppendPoint(new InkPoint(48, 32, .1f, 1f)); e.EndStroke();
            Assert.Greater(e.Paper.TotalPigment(), 0f);
        }
        [Test]
        public void DryingCannotCreateNegativePigment()
        {
            var paper = new InkPaper(32, 32);
            paper.Deposit(12, 12, 5, .7f, .8f, .2f, 43);
            for (int i = 0; i < 20; i++) paper.Step(.1f);
            for (int y = 0; y < paper.Height; y++)
                for (int x = 0; x < paper.Width; x++)
                    Assert.GreaterOrEqual(paper.PigmentAt(x, y), 0f);
        }
        [TestCase(32)]
        [TestCase(-17)]
        [TestCase(int.MaxValue)]
        public void SameSeedAndPointsReproducePixelBuffer(int strokeSeed)
        {
            var a = new SumiEngineCore(64, 64, 5);
            var b = new SumiEngineCore(64, 64, 5);
            foreach (var engine in new[] {a, b})
            {
                engine.DrawBezier(new InkPoint(4, 8, 0f, .4f),
                  new InkPoint(15, 40, .1f, .9f), new InkPoint(45, 55, .2f, .8f),
                  new InkPoint(60, 10, .3f, .2f), InkBrushSettings.Slash, strokeSeed);
                engine.Step(.2f);
            }
            var ab = new byte[64*64*4]; var bb = new byte[ab.Length];
            a.Paper.RenderRgba(ab); b.Paper.RenderRgba(bb);
            CollectionAssert.AreEqual(ab, bb);
        }
        [Test]
        public void DiffusionPreservesPigmentWithinTolerance()
        {
            var paper = new InkPaper(64, 64);
            paper.Deposit(18, 12, 9, .55f, .95f, .1f, 12);
            float before = paper.TotalPigment();
            for (int i = 0; i < 10; ++i) paper.Step(.1f);
            Assert.That(paper.TotalPigment(), Is.EqualTo(before).Within(before * .002f + .01f));
        }
        [Test]
        public void ReplayOwnSessionDoesNotEraseStrokes()
        {
            var engine = new SumiEngineCore(64, 64);
            engine.BeginStroke(new InkPoint(10, 10, 0), InkBrushSettings.Vein, 100);
            engine.AppendPoint(new InkPoint(20, 20, .1f)); engine.EndStroke();
            engine.Replay(engine.Session);
            Assert.AreEqual(1, engine.Session.strokes.Count);
            Assert.Greater(engine.Paper.TotalPigment(), 0f);
        }
        [Test]
        public void OutsideCanvasDoesNotThrow()
        {
            var paper = new InkPaper(32, 32);
            Assert.DoesNotThrow(() => paper.Deposit(-100, -100, 20, .8f, .8f, .2f, 4));
        }
        [Test]
        public void EndStoresReplayableCommands()
        {
            var e = new SumiEngineCore(64, 64);
            e.BeginStroke(new InkPoint(11, 12, 0), InkBrushSettings.Vein, 3);
            e.AppendPoint(new InkPoint(14, 19, .1f)); e.EndStroke();
            Assert.AreEqual(1, e.Session.strokes.Count);
            Assert.AreEqual(2, e.Session.strokes[0].points.Count);
        }
    }
}
