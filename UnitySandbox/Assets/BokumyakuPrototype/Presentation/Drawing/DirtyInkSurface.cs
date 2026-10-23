using System;
using ConnectPiece.SumiEngine.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Bokumyaku.Presentation.Drawing
{
    /// <summary>Persistent SUMI pigment mask. Deposition only: no global wet-paper Step.
    /// Converts and uploads touched tiles, never Apply()s the destination after initialization.
    /// Main-thread owned. Coordinates use Unity texture bottom-left origin.</summary>
    public sealed class DirtyInkSurface : IDisposable
    {
        public const int TileSize = 32;
        readonly SumiEngineCore core;
        readonly Texture2D staging;
        readonly Texture2D clearTemplate;
        readonly bool[] dirty;
        readonly Color32[] tilePixels = new Color32[TileSize * TileSize];
        readonly int columns, rows;
        InkPoint previous;
        float extent;
        bool active, disposed;
        public Texture2D Texture { get; private set; }
        public int Width => core.Paper.Width;
        public int Height => core.Paper.Height;
        public int FullInitializations { get; private set; }
        public int ClearCount { get; private set; }
        public long UpdatedPixels { get; private set; }
        public int DirtyUploads { get; private set; }
        public RectInt LastDirtyRect { get; private set; }
        public int PendingTiles { get; private set; }
        public static bool IsSupported => (SystemInfo.copyTextureSupport & CopyTextureSupport.Basic) != 0;

        public DirtyInkSurface(int width, int height, int seed = 2026)
        {
            if (!IsSupported)
                throw new NotSupportedException("Persistent ink requires GPU region CopyTexture support; full texture upload fallback is intentionally disabled.");
            core = new SumiEngineCore(width, height, seed);
            columns = (width + TileSize - 1) / TileSize;
            rows = (height + TileSize - 1) / TileSize;
            dirty = new bool[columns * rows];
            staging = NewTexture(TileSize, TileSize, "SUMI dirty tile staging");
            clearTemplate = NewTexture(width, height, "SUMI transparent reset template");
            clearTemplate.SetPixels32(new Color32[width * height]);
            clearTemplate.Apply(false, true);
            InitializeTexture();
        }

        static Texture2D NewTexture(int width, int height, string name) =>
            new Texture2D(width, height, TextureFormat.RGBA32, false, false)
            { name = name, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };

        void InitializeTexture()
        {
            Texture = NewTexture(Width, Height, "SUMI persistent pigment");
            Texture.SetPixels32(new Color32[Width * Height]);
            Texture.Apply(false, true); // GPU-only destination; subsequent writes are region copies.
            FullInitializations++;
        }

        public void Begin(InkPoint point, InkBrushSettings settings, int seed)
        {
            ThrowIfDisposed();
            if (active) End();
            core.BeginStroke(point, settings, seed); // Validate before marking bounds.
            // Legacy head: spread <=1 + lag <=.16 + bristle radius <=.33.
            // Textured kernel: 1.4r; Splatter: satellite center <=2.6r + .185r*1.4.
            // 3r + 3 safely encloses all current kernels, rounding and minimum radii.
            extent = 3f * Mathf.Max(.6f, settings.radius) + 3f;
            previous = point;
            active = true;
            Mark(point.x, point.y, point.x, point.y);
        }

        public void Move(InkPoint point)
        {
            ThrowIfDisposed();
            if (!active) return;
            core.AppendPoint(point);
            Mark(previous.x, previous.y, point.x, point.y);
            previous = point;
        }

        public void End()
        {
            ThrowIfDisposed();
            core.EndStroke();
            // This is a persistent raster, not an editable stroke document. Bound history memory.
            core.Session.strokes.Clear();
            active = false;
        }

        void Mark(float x0, float y0, float x1, float y1)
        {
            double left = Math.Min(x0, x1) - (double)extent;
            double right = Math.Max(x0, x1) + (double)extent;
            double bottom = Math.Min(y0, y1) - (double)extent;
            double top = Math.Max(y0, y1) + (double)extent;
            if (right < 0 || top < 0 || left >= Width || bottom >= Height) return;
            int minX = (int)Math.Max(0, Math.Floor(left)) / TileSize;
            int maxX = (int)Math.Min(Width - 1, Math.Ceiling(right)) / TileSize;
            int minY = (int)Math.Max(0, Math.Floor(bottom)) / TileSize;
            int maxY = (int)Math.Min(Height - 1, Math.Ceiling(top)) / TileSize;
            for (int y = minY; y <= maxY; y++)
                for (int x = minX; x <= maxX; x++)
                {
                    int index = y * columns + x;
                    if (!dirty[index]) { dirty[index] = true; PendingTiles++; }
                }
        }

        /// <summary>Call once after a batch of new strokes. Returns uploaded tile count.</summary>
        public int Flush()
        {
            ThrowIfDisposed();
            int uploads = 0;
            if (PendingTiles == 0) return 0;
            int left = Width, bottom = Height, right = 0, top = 0;
            for (int ty = 0; ty < rows; ty++) for (int tx = 0; tx < columns; tx++)
            {
                int index = ty * columns + tx;
                if (!dirty[index]) continue;
                int px = tx * TileSize, py = ty * TileSize;
                int w = Math.Min(TileSize, Width - px), h = Math.Min(TileSize, Height - py);
                for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
                {
                    float pigment = core.Paper.PigmentAt(px + x, py + y);
                    byte alpha = (byte)Mathf.RoundToInt(255f * (1f - Mathf.Exp(-2.5f * pigment)));
                    tilePixels[y * TileSize + x] = new Color32(255, 255, 255, alpha);
                }
                staging.SetPixels32(tilePixels);
                staging.Apply(false, false);
                Graphics.CopyTexture(staging, 0, 0, 0, 0, w, h, Texture, 0, 0, px, py);
                dirty[index] = false;
                PendingTiles--;
                DirtyUploads++;
                UpdatedPixels += w * h;
                uploads++;
                left = Math.Min(left, px); bottom = Math.Min(bottom, py);
                right = Math.Max(right, px + w); top = Math.Max(top, py + h);
            }
            LastDirtyRect = new RectInt(left, bottom, right - left, top - bottom);
            core.Paper.MarkRendered();
            return uploads;
        }

        public void Clear()
        {
            ThrowIfDisposed();
            core.Clear();
            active = false;
            Array.Clear(dirty, 0, dirty.Length);
            PendingTiles = 0;
            // Run/reset boundary: one GPU copy, retaining the destination and bound sprites.
            // No full CPU upload, deferred Destroy, or new texture per restart.
            Graphics.CopyTexture(clearTemplate, Texture);
            ClearCount++;
            LastDirtyRect = new RectInt(0, 0, Width, Height);
        }

        public float PigmentAt(int x, int y) => core.Paper.PigmentAt(x, y);
        void ThrowIfDisposed() { if (disposed) throw new ObjectDisposedException(nameof(DirtyInkSurface)); }
        static void Destroy(UnityEngine.Object value)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(value);
            else UnityEngine.Object.DestroyImmediate(value);
        }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            Destroy(Texture); Destroy(staging); Destroy(clearTemplate);
        }
    }
}
