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
        string status = "Drag on the paper to paint | Drag to paint";
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
            paperRect = new Rect(margin, 60, Screen.width - 2 * margin,
                Mathf.Max(60, Screen.height - 258));
            GUI.DrawTexture(paperRect, canvas.Texture, ScaleMode.StretchToFill);
            Rect controls = new Rect(margin, Screen.height - 190, Screen.width - 2 * margin, 180);
            GUILayout.BeginArea(controls, GUI.skin.box);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Vein")) current = InkBrushSettings.Vein;
            if (GUILayout.Button("Slash")) current = InkBrushSettings.Slash;
            if (GUILayout.Button("Bloom")) current = InkBrushSettings.Bloom;
            if (GUILayout.Button("Clear")) canvas.Clear();
            if (GUILayout.Button("PNG")) SavePng();
            GUILayout.EndHorizontal();
            GUILayout.Label($"Pressure / Width: {current.radius:0.0}");
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
                    canvas.End(); drawing = false; e.Use();
                }
            }
        }
        InkPoint MapPoint(Vector2 position)
        {
            float x = Mathf.Clamp01((position.x - paperRect.x) / paperRect.width) * (canvas.width - 1);
            float y = (1f - Mathf.Clamp01((position.y - paperRect.y) / paperRect.height)) * (canvas.height - 1);
            return new InkPoint(x, y, clock, 1f);
        }
        void SavePng()
        {
            string name = $"SUMI_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            string path = Path.Combine(Application.persistentDataPath, name);
            try { File.WriteAllBytes(path, canvas.ExportPng()); status = $"Saved: {path}"; }
            catch (Exception e) { status = "PNG export failed: " + e.Message; }
        }
    }
}
