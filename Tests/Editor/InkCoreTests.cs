using NUnit.Framework;
using ConnectPiece.SumiEngine.Core;

namespace ConnectPiece.SumiEngine.Tests
{
    public class InkCoreTests
    {
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
        [Test]
        public void SameSeedAndPointsReproducePixelBuffer()
        {
            var a = new SumiEngineCore(64, 64, 5);
            var b = new SumiEngineCore(64, 64, 5);
            foreach (var engine in new[] {a, b})
            {
                engine.DrawBezier(new InkPoint(4, 8, 0f, .4f),
                  new InkPoint(15, 40, .1f, .9f), new InkPoint(45, 55, .2f, .8f),
                  new InkPoint(60, 10, .3f, .2f), InkBrushSettings.Slash, 32);
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
