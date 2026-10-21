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
        Rect paperRect;
        void Start()
        {
            canvas = GetComponent<SumiCanvas>();
            canvas.Initialize();
            current = InkBrushSettings.Vein;
        }
        void Update() { clock += Time.unscaledDeltaTime; }
        void OnGUI()
        {
            if (canvas == null || canvas.Texture == null) return;
            GUI.skin.label.fontSize = 13;
            int margin = 16;
            GUI.Label(new Rect(margin, 10, Screen.width - 32, 24), "SUMI ENGINE / BRUSH LAB  v0.1");
            GUI.Label(new Rect(margin, 33, Screen.width - 32, 20), status);
            float controlsHeight = Mathf.Min(222f, Screen.height * .45f);
            float controlsTop = Screen.height - controlsHeight - 10f;
            float availableWidth = Mathf.Max(1f, Screen.width - 2 * margin);
            float availableHeight = Mathf.Max(1f, controlsTop - 76f);
            float scale = Mathf.Min(availableWidth / canvas.width, availableHeight / canvas.height);
            paperRect = new Rect((Screen.width - canvas.width * scale) * .5f, 60f,
                canvas.width * scale, canvas.height * scale);
            GUI.DrawTexture(paperRect, canvas.Texture, ScaleMode.StretchToFill);
            Rect controls = new Rect(margin, controlsTop, availableWidth, controlsHeight);
            GUILayout.BeginArea(controls, GUI.skin.box);
            controlsScroll = GUILayout.BeginScrollView(controlsScroll);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Vein")) current = InkBrushSettings.Vein;
            if (GUILayout.Button("Slash")) current = InkBrushSettings.Slash;
            if (GUILayout.Button("Bloom")) current = InkBrushSettings.Bloom;
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear")) { StopDrawing(); canvas.Clear(); }
            if (GUILayout.Button("PNG")) SavePng();
            GUILayout.EndHorizontal();
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
