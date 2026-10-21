using System;
using ConnectPiece.SumiEngine.Core;
using UnityEngine;

namespace ConnectPiece.SumiEngine
{
    /// <summary>Unity texture adapter. No URP or external package required.</summary>
    public sealed class SumiCanvas : MonoBehaviour
    {
        [Min(64)] public int width = 384;
        [Min(64)] public int height = 512;
        public int paperSeed = 2026;
        [Range(2, 30)] public int uploadFps = 15;
        [Range(0, 30)] public int diffusionFps = 10;
        public SumiEngineCore Core { get; private set; }
        public Texture2D Texture { get; private set; }
        byte[] pixels;
        float uploadClock, simClock;

        void Awake() => Initialize();
        public void Initialize()
        {
            if (Core != null) return;
            width = Mathf.Clamp(width, 64, 2048);
            height = Mathf.Clamp(height, 64, 2048);
            Core = new SumiEngineCore(width, height, paperSeed);
            Texture = new Texture2D(width, height, TextureFormat.RGBA32, false, false)
            { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, name = "SUMI Paper" };
            pixels = new byte[width * height * 4];
            Upload();
        }
        void Update()
        {
            if (Core == null) return;
            simClock += Time.unscaledDeltaTime;
            uploadClock += Time.unscaledDeltaTime;
            if (diffusionFps > 0)
            {
                float step = 1f / diffusionFps;
                int iterations = 0;
                while (simClock >= step && iterations++ < 3)
                { Core.Step(step); simClock -= step; }
                simClock = Mathf.Min(simClock, step * 3f);
            }
            if (uploadClock >= 1f / Mathf.Max(1, uploadFps) && Core.Paper.IsDirty)
            { uploadClock = 0f; Upload(); }
        }
        public void Upload()
        {
            if (Core == null) Initialize();
            Core.Paper.RenderRgba(pixels);
            Texture.LoadRawTextureData(pixels);
            Texture.Apply(false, false);
        }
        public void Begin(InkPoint point, InkBrushSettings settings, int seed)
        { if (Core == null) Initialize(); Core.BeginStroke(point, settings, seed); }
        public void Move(InkPoint point) => Core?.AppendPoint(point);
        public void End() => Core?.EndStroke();
        public void Clear() { Core?.Clear(); Upload(); }
        public byte[] ExportPng()
        {
            Upload(); return Texture.EncodeToPNG();
        }
        void OnDestroy()
        {
            if (Texture != null) Destroy(Texture);
        }
    }
}
