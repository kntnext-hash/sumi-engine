using System;
using NUnit.Framework;
using ConnectPiece.SumiEngine.Core;

namespace ConnectPiece.SumiEngine.Tests
{
    public class InkPointerStrokeTests
    {
        [Test]
        public void TinyHandJitterKeepsTheOriginalTapAndAccumulatedMovementStartsStroke()
        {
            var tap = new SumiEngineCore(64, 64); var jitter = new SumiEngineCore(64, 64);
            var brush = InkBrushSettings.Dense; brush.radius = 12;
            var origin = new InkPoint(30, 30, 0, .8f);
            var direct = new InkPointerStroke(tap); direct.Begin(origin, brush, 17); direct.End();
            var pointer = new InkPointerStroke(jitter); pointer.Begin(origin, brush, 17);
            pointer.Move(new InkPoint(31, 30, .1f, .2f));
            pointer.Move(new InkPoint(30.5f, 30, .2f, .1f)); pointer.End();
            Assert.AreEqual(1, jitter.Session.strokes[0].points.Count);
            var point = jitter.Session.strokes[0].points[0];
            Assert.AreEqual(origin.x, point.x); Assert.AreEqual(origin.pressure, point.pressure);
            var a = new byte[64 * 64 * 4]; var b = new byte[a.Length];
            tap.Paper.RenderRgba(a); jitter.Paper.RenderRgba(b); CollectionAssert.AreEqual(a, b);
            jitter.Clear(); pointer.Begin(origin, brush, 17);
            // Returning near the origin still starts a stroke when travelled distance exceeds 2 pixels.
            pointer.Move(new InkPoint(31.2f, 30, .1f));
            pointer.Move(new InkPoint(30, 30, .2f));
            Assert.IsTrue(pointer.Active); Assert.Greater(pointer.BufferedPointCount, 0);
            pointer.End(); Assert.Greater(jitter.Session.strokes[0].points.Count, 1);
            Assert.Greater(jitter.Paper.TotalPigment(), 0, "A short stroke must not be erased by overlapping taper ramps.");
            Assert.Greater(jitter.Session.strokes[0].points[1].pressure, .5f);
            tap.Replay(jitter.Session); tap.Paper.RenderRgba(a); jitter.Paper.RenderRgba(b);
            CollectionAssert.AreEqual(a, b);
        }

        [Test]
        public void TaperStaysOnInputPathAndEndsAtReleasePosition()
        {
            var core = new SumiEngineCore(128, 128);
            var pointer = new InkPointerStroke(core);
            var brush = InkBrushSettings.Dense; brush.radius = 8;
            pointer.Begin(new InkPoint(10, 20, 0), brush, 7);
            pointer.Move(new InkPoint(90, 20, .5f));
            Assert.Greater(core.Paper.TotalPigment(), 0, "Dragging must draw before release.");
            pointer.Move(new InkPoint(90, 90, 1)); pointer.End();
            var points = core.Session.strokes[0].points;
            float peak = 0;
            foreach (var p in points)
            {
                Assert.IsTrue((Math.Abs(p.y - 20) < .001f && p.x >= 10 && p.x <= 90) ||
                    (Math.Abs(p.x - 90) < .001f && p.y >= 20 && p.y <= 90));
                peak = Math.Max(peak, p.pressure);
            }
            var end = points[points.Count - 1];
            Assert.AreEqual(90, end.x); Assert.AreEqual(90, end.y);
            Assert.AreEqual(0, end.pressure); Assert.Less(points[0].pressure, peak * .1f);
            Assert.Less(points[points.Count - 2].pressure, peak);
            Assert.IsFalse(pointer.Active); Assert.AreEqual(0, pointer.BufferedPointCount);
        }

        [Test]
        public void DisabledTaperPreservesPressureAndTapKeepsItsLoad()
        {
            var core = new SumiEngineCore(32, 32); var pointer = new InkPointerStroke(core);
            pointer.Begin(new InkPoint(3, 8, 0, .25f), InkBrushSettings.Dense, 3, false);
            pointer.Move(new InkPoint(25, 8, .2f, .8f)); pointer.End();
            var points = core.Session.strokes[0].points;
            Assert.AreEqual(2, points.Count);
            Assert.AreEqual(.25f, points[0].pressure); Assert.AreEqual(.8f, points[1].pressure);
            pointer.Begin(new InkPoint(16, 16, 1, .7f), InkBrushSettings.Dense, 4);
            pointer.Move(new InkPoint(16, 16, 2, .1f)); pointer.End(); pointer.End();
            Assert.AreEqual(2, core.Session.strokes.Count);
            Assert.AreEqual(1, core.Session.strokes[1].points.Count);
            Assert.AreEqual(.7f, core.Session.strokes[1].points[0].pressure);
        }

        [Test]
        public void LongSlowInputHasBoundedTailAndInvalidCommandsPreserveIt()
        {
            var core = new SumiEngineCore(32, 32); var pointer = new InkPointerStroke(core);
            var brush = InkBrushSettings.Dense; brush.radius = 2;
            pointer.Begin(new InkPoint(5, 5, 0), brush, 9);
            for (int i = 1; i <= 300; i++)
            {
                pointer.Move(new InkPoint(5 + i * .02f, 5, i * .01f));
                Assert.LessOrEqual(pointer.BufferedPointCount, InkPointerStroke.MaximumBufferedPoints);
            }
            int buffered = pointer.BufferedPointCount; float pigment = core.Paper.TotalPigment();
            var invalid = new InkPoint(5, 5, 4); invalid.x = float.NaN;
            Assert.Throws<ArgumentOutOfRangeException>(() => pointer.Move(invalid));
            Assert.Throws<ArgumentOutOfRangeException>(() => pointer.Move(new InkPoint(6000, 5, 4)));
            var badBrush = brush; badBrush.radius = float.NaN;
            Assert.Throws<ArgumentOutOfRangeException>(() => pointer.Begin(new InkPoint(1, 1, 5), badBrush, 1));
            Assert.IsTrue(pointer.Active); Assert.AreEqual(buffered, pointer.BufferedPointCount);
            Assert.AreEqual(pigment, core.Paper.TotalPigment()); Assert.AreEqual(0, core.Session.strokes.Count);
            pointer.End(); Assert.AreEqual(1, core.Session.strokes.Count);
        }

        [Test]
        public void PointerSequenceAndOrdinarySessionReplayAreDeterministic()
        {
            var a = new SumiEngineCore(64, 64); var b = new SumiEngineCore(64, 64);
            foreach (var core in new[] { a, b })
            {
                var pointer = new InkPointerStroke(core);
                var brush = InkBrushSettings.Split; brush.radius = 5;
                pointer.Begin(new InkPoint(5, 8, 0, .8f), brush, -12);
                pointer.Move(new InkPoint(35, 25, .1f, 1));
                pointer.Move(new InkPoint(55, 48, .3f, .7f)); pointer.End();
            }
            var first = new byte[64 * 64 * 4]; var second = new byte[first.Length];
            a.Paper.RenderRgba(first); b.Paper.RenderRgba(second);
            CollectionAssert.AreEqual(first, second);
            b.Replay(a.Session); b.Paper.RenderRgba(second);
            CollectionAssert.AreEqual(first, second);
        }
    }
}
