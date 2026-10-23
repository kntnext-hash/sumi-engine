using System;
using Bokumyaku.Presentation.Drawing;
using ConnectPiece.SumiEngine.Core;
using NUnit.Framework;
using UnityEngine;

namespace Bokumyaku.Drawing.Tests
{
    public sealed class DirtyInkSurfaceTests
    {
        [Test]
        public void SparseMarksUsePartialUploadsAndPreserveExistingPigment()
        {
            if (!DirtyInkSurface.IsSupported) Assert.Ignore("GPU region copy unavailable on test renderer.");
            using (var surface = new DirtyInkSurface(256, 256))
            {
                var brush = InkBrushSettings.Dense;
                brush.radius = 3; brush.texture = InkTexture.Bleed;
                surface.Begin(new InkPoint(30, 30, 0, 1), brush, 12);
                surface.End();
                Assert.Greater(surface.Flush(), 0);
                float original = surface.PigmentAt(30, 30);
                Assert.Greater(original, 0);
                long previousPixels = surface.UpdatedPixels;
                surface.Begin(new InkPoint(220, 220, 0, 1), brush, 44);
                surface.End();
                surface.Flush();
                Assert.AreEqual(original, surface.PigmentAt(30, 30));
                Assert.Less(surface.UpdatedPixels - previousPixels, 256 * 256 / 4);
                Assert.AreEqual(1, surface.FullInitializations);
                Assert.AreEqual(0, surface.Flush());
                Assert.AreEqual(0, surface.PendingTiles);
                AssertGpuMatchesPigment(surface);
            }
        }

        [Test]
        public void SplatterAndEdgeSegmentsCopyAllNonzeroPixels()
        {
            if (!DirtyInkSurface.IsSupported) Assert.Ignore("GPU region copy unavailable on test renderer.");
            // Non-multiple dimensions exercise clipped staging tile copies.
            using (var surface = new DirtyInkSurface(143, 137))
            {
                var brush = InkBrushSettings.Dense;
                brush.radius = 7; brush.texture = InkTexture.Splatter;
                surface.Begin(new InkPoint(2, 2, 0, 1), brush, 42);
                surface.Move(new InkPoint(30, 26, 1, .8f));
                surface.End();
                surface.Begin(new InkPoint(140, 130, 0, 1), brush, 73);
                surface.End();
                surface.Flush();
                AssertGpuMatchesPigment(surface);
                var texture = surface.Texture;
                surface.Clear();
                Assert.AreSame(texture, surface.Texture);
                Assert.AreEqual(1, surface.FullInitializations);
                Assert.AreEqual(1, surface.ClearCount);
                AssertGpuMatchesPigment(surface);
            }
        }

        [Test]
        public void RepeatedResetDiscardsPendingStrokeAndAcceptsNewPartialMarks()
        {
            if (!DirtyInkSurface.IsSupported) Assert.Ignore("GPU region copy unavailable on test renderer.");
            using (var surface = new DirtyInkSurface(143, 137))
            {
                var texture = surface.Texture;
                var brush = InkBrushSettings.Dense; brush.radius = 3;
                for (int i = 0; i < 3; i++)
                {
                    surface.Begin(new InkPoint(10, 10, 0, 1), brush, 42);
                    surface.Move(new InkPoint(35, 35, 1, 1)); // Deliberately not ended/flushed.
                    surface.Clear();
                    Assert.AreSame(texture, surface.Texture);
                    Assert.AreEqual(0, surface.PendingTiles);
                    Assert.AreEqual(0, surface.Flush());
                    Assert.AreEqual(0, surface.PigmentAt(10, 10));
                    AssertGpuMatchesPigment(surface);
                    long previousPixels = surface.UpdatedPixels;
                    surface.Begin(new InkPoint(130, 125, 0, 1), brush, 73);
                    surface.End(); surface.Flush();
                    Assert.Greater(surface.PigmentAt(130, 125), 0);
                    Assert.Less(surface.UpdatedPixels - previousPixels, surface.Width * surface.Height / 2);
                    AssertGpuMatchesPigment(surface);
                }
                Assert.AreEqual(1, surface.FullInitializations);
                Assert.AreEqual(3, surface.ClearCount);
            }
        }

        [Test]
        public void ConsecutiveFlushesPreserveGpuPigmentAndReuseStagingSource()
        {
            if (!DirtyInkSurface.IsSupported) Assert.Ignore("GPU region copy unavailable.");
            using (var surface = new DirtyInkSurface(143, 137))
            {
                var field = typeof(DirtyInkSurface).GetField("staging", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var source = (Texture2D)field.GetValue(surface);
                var brush = InkBrushSettings.Dense; brush.radius = 5;
                for (int pass = 0; pass < 2; pass++)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        // Repeated tile, distant tile, and clipped edge; no readback between flushes.
                        float x = i % 3 == 0 ? 20 : i % 3 == 1 ? 95 : 141;
                        surface.Begin(new InkPoint(x, 15 + i * 16, 0, .8f), brush, 42 + i);
                        surface.End(); surface.Flush();
                    }
                    AssertGpuMatchesPigment(surface);
                    surface.Clear();
                    Assert.AreSame(source, field.GetValue(surface));
                }
                Assert.AreEqual(1, surface.FullInitializations);
                AssertGpuMatchesPigment(surface);
            }
        }

        static void AssertGpuMatchesPigment(DirtyInkSurface surface)
        {
            var previous = RenderTexture.active;
            var target = RenderTexture.GetTemporary(surface.Width, surface.Height, 0, RenderTextureFormat.ARGB32);
            var readback = new Texture2D(surface.Width, surface.Height, TextureFormat.RGBA32, false);
            try
            {
                Graphics.Blit(surface.Texture, target);
                RenderTexture.active = target;
                readback.ReadPixels(new Rect(0, 0, surface.Width, surface.Height), 0, 0);
                readback.Apply();
                var pixels = readback.GetPixels32();
                int maxError = 0;
                for (int y = 0; y < surface.Height; y++) for (int x = 0; x < surface.Width; x++)
                {
                    int expected = Mathf.RoundToInt(255 * (1 - Mathf.Exp(-2.5f * surface.PigmentAt(x, y))));
                    maxError = Math.Max(maxError, Math.Abs(expected - pixels[y * surface.Width + x].a));
                }
                Assert.LessOrEqual(maxError, 1, "GPU pigment differs from complete CPU reference, including outside dirty tiles.");
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                UnityEngine.Object.DestroyImmediate(readback);
            }
        }
    }
}

