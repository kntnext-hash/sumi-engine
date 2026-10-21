using System;
using System.IO;
using ConnectPiece.SumiEngine.Core;
using UnityEngine;

namespace ConnectPiece.SumiEngine
{
    /// <summary>
    /// Drop on any object in an empty scene; hit Play. Editor menu can create it.
    /// IMGUI demo avoids UI prefabs and optional input-package dependencies.
    /// </summary>
    [RequireComponent(typeof(SumiCanvas))]
    public sealed class SumiBrushLab : MonoBehaviour
    {
        SumiCanvas canvas;
        InkBrushSettings current;
        bool drawing;
        int nextSeed = 100;
        float clock;
        float pressure = 1f;
        string status = "Drag on the paper to paint";
        Vector2 controlsScroll;
        bool showColors, editPaper;
        Rect paperRect;
        void Start()
        {
            canvas = GetComponent<SumiCanvas>();
            canvas.Initialize();
            SelectStudyBrush(InkBrushSettings.Dense);
        }
        void Update() { clock += Time.unscaledDeltaTime; }
        void OnGUI()
        {
            if (canvas == null || canvas.Texture == null) return;
            // Brush Lab may have no clearing camera: repaint the backdrop to avoid
            // previous status text and controls remaining in the Game View buffer.
            var previousColor = GUI.color;
            GUI.color = new Color(.055f, .06f, .065f, 1f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previousColor;
            GUI.skin.label.fontSize = 13;
            int margin = 16;
            GUI.Label(new Rect(margin, 10, Screen.width - 32, 24), "SUMI ENGINE / BRUSH LAB  v0.1");
            GUI.Label(new Rect(margin, 33, Screen.width - 32, 20), status);
            float controlsHeight = Mathf.Min(222f, Screen.height * .45f);
            float controlsTop = Screen.height - controlsHeight - 10f;
            float availableWidth = Mathf.Max(1f, Screen.width - 2 * margin);
            float availableHeight = Mathf.Max(1f, controlsTop - 76f);
            bool wide = Screen.width >= 720;
            if (wide)
            {
                availableWidth = Screen.width - 332f;
                availableHeight = Screen.height - 84f;
            }
            float scale = Mathf.Min(availableWidth / canvas.width, availableHeight / canvas.height);
            paperRect = new Rect((wide ? margin : 0) + ((wide ? availableWidth : Screen.width) - canvas.width * scale) * .5f, 60f,
                canvas.width * scale, canvas.height * scale);
            GUI.DrawTexture(paperRect, canvas.Texture, ScaleMode.StretchToFill);
            Rect controls = wide ? new Rect(Screen.width - 300f, 60f, 284f, Screen.height - 76f)
                : new Rect(margin, controlsTop, availableWidth, controlsHeight);
            GUILayout.BeginArea(controls, GUI.skin.box);
            controlsScroll = GUILayout.BeginScrollView(controlsScroll);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Vein")) current = InkBrushSettings.Vein;
            if (GUILayout.Button("Slash")) current = InkBrushSettings.Slash;
            if (GUILayout.Button("Bloom")) current = InkBrushSettings.Bloom;
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Dense")) SelectStudyBrush(InkBrushSettings.Dense);
            if (GUILayout.Button("Dry")) SelectStudyBrush(InkBrushSettings.Dry);
            if (GUILayout.Button("Split")) SelectStudyBrush(InkBrushSettings.Split);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Bleed")) SelectStudyBrush(InkBrushSettings.Bleed);
            if (GUILayout.Button("Pool")) SelectStudyBrush(InkBrushSettings.Pool);
            if (GUILayout.Button("Mist")) SelectStudyBrush(InkBrushSettings.Mist);
            if (GUILayout.Button("Splatter")) SelectStudyBrush(InkBrushSettings.Splatter);
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Stroke study (replaces paper)")) { StopDrawing(); InkStudy.Draw(canvas.Core); canvas.displayDensity = 7f; canvas.inkColor = new Color32(12, 12, 13, 255); canvas.Upload(); }
            if (GUILayout.Button("Effects study (replaces paper)"))
            {
                StopDrawing(); InkStudy.DrawEffects(canvas.Core); canvas.displayDensity = 7f;
                canvas.Upload(); status = "Dry / Split / Taper | Bleed / Pool / Splatter | Wash / Mist / Enso | Branch / Drips / Spiral";
            }
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear")) { StopDrawing(); canvas.Clear(); }
            if (GUILayout.Button("PNG")) SavePng();
            if (GUILayout.Button(showColors ? "Colors -" : "Colors +")) showColors = !showColors;
            GUILayout.EndHorizontal();
            if (showColors) DrawColorControls();
            GUILayout.Label($"Pressure: {pressure:0.00}");
            pressure = GUILayout.HorizontalSlider(pressure, 0f, 1f);
            GUILayout.Label($"Brush radius: {current.radius:0.0}");
            current.radius = GUILayout.HorizontalSlider(current.radius, 2f, 35f);
            GUILayout.Label($"Pigment: {current.pigment:0.00}       Water: {current.water:0.00}");
            GUILayout.BeginHorizontal();
            current.pigment = GUILayout.HorizontalSlider(current.pigment, 0f, 1f);
            current.water = GUILayout.HorizontalSlider(current.water, 0f, 1f);
            GUILayout.EndHorizontal();
            GUILayout.Label($"Roughness: {current.roughness:0.00}       Bristles: {current.bristleCount}");
            GUILayout.BeginHorizontal();
            current.roughness = GUILayout.HorizontalSlider(current.roughness, 0f, 1f);
            current.bristleCount = Mathf.RoundToInt(GUILayout.HorizontalSlider(current.bristleCount, 3f, 25f));
            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();
            GUILayout.EndArea();

            Event e = Event.current;
            if (e.isMouse && (e.type == EventType.MouseDown || e.type == EventType.MouseDrag || e.type == EventType.MouseUp))
            {
                if (e.type == EventType.MouseDown && e.button == 0 && paperRect.Contains(e.mousePosition))
                {
                    drawing = true;
                    canvas.Begin(MapPoint(e.mousePosition), current, nextSeed++);
                    e.Use();
                }
                else if (e.type == EventType.MouseDrag && drawing)
                {
                    canvas.Move(MapPoint(e.mousePosition));
                    e.Use();
                }
                else if (e.type == EventType.MouseUp && drawing && e.button == 0)
                {
                    canvas.Move(MapPoint(e.mousePosition));
                    StopDrawing(); e.Use();
                }
            }
        }
        InkPoint MapPoint(Vector2 position)
        {
            float x = Mathf.Clamp01((position.x - paperRect.x) / paperRect.width) * (canvas.width - 1);
            float y = (1f - Mathf.Clamp01((position.y - paperRect.y) / paperRect.height)) * (canvas.height - 1);
            return new InkPoint(x, y, clock, pressure);
        }
        void StopDrawing() { canvas?.End(); drawing = false; }
        void SelectStudyBrush(InkBrushSettings brush)
        {
            current = brush;
            canvas.displayDensity = 7f;
            canvas.inkColor = new Color32(12, 12, 13, 255);
            status = current.texture + ": drag to paint; Colors controls the whole paper";
        }
        void DrawColorControls()
        {
            GUILayout.Label("Palette recolors all strokes");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(editPaper ? "Ink" : "[Ink]")) editPaper = false;
            if (GUILayout.Button(editPaper ? "[Paper]" : "Paper")) editPaper = true;
            GUILayout.EndHorizontal();
            Color selected = editPaper ? canvas.paperColor : canvas.inkColor;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Sumi")) selected = new Color32(29, 30, 31, 255);
            if (GUILayout.Button("Vermilion")) selected = new Color32(190, 48, 35, 255);
            if (GUILayout.Button("Indigo")) selected = new Color32(30, 66, 119, 255);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Washi")) selected = new Color32(243, 240, 231, 255);
            if (GUILayout.Button("White")) selected = Color.white;
            if (GUILayout.Button("Black")) selected = Color.black;
            GUILayout.EndHorizontal();
            selected.r = ColorChannel("R", selected.r);
            selected.g = ColorChannel("G", selected.g);
            selected.b = ColorChannel("B", selected.b);
            selected.a = 1f;
            var swatch = GUILayoutUtility.GetRect(24f, 18f);
            var previous = GUI.color; GUI.color = selected;
            GUI.DrawTexture(swatch, Texture2D.whiteTexture); GUI.color = previous;
            GUILayout.Label("#" + ColorUtility.ToHtmlStringRGB(selected));
            if (editPaper) canvas.paperColor = selected; else canvas.inkColor = selected;
            GUILayout.Label($"Display density: {canvas.displayDensity:0.00}");
            canvas.displayDensity = GUILayout.HorizontalSlider(canvas.displayDensity, .1f, 12f);
            if (GUILayout.Button("Reset palette"))
            {
                canvas.inkColor = new Color32(29, 30, 31, 255);
                canvas.paperColor = new Color32(243, 240, 231, 255);
                canvas.displayDensity = .9f;
            }
        }
        static float ColorChannel(string label, float value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{label} {Mathf.RoundToInt(value * 255f)}", GUILayout.Width(50));
            value = GUILayout.HorizontalSlider(value, 0f, 1f);
            GUILayout.EndHorizontal();
            return value;
        }
        void OnDisable() => StopDrawing();
        void OnApplicationFocus(bool focused) { if (!focused) StopDrawing(); }
        void SavePng()
        {
            string name = $"SUMI_{DateTime.Now:yyyyMMdd_HHmmss_fff}_{Guid.NewGuid():N}.png";
            string path = Path.Combine(Application.persistentDataPath, name);
            try { File.WriteAllBytes(path, canvas.ExportPng()); status = $"Saved: {path}"; }
            catch (Exception e) { status = "PNG export failed: " + e.Message; }
        }
    }
}
