using System;
using System.Collections.Generic;
using ConnectPiece.SumiEngine.Core;
using UnityEngine;

namespace ConnectPiece.SumiEngine
{
    /// <summary>
    /// Renderer integration sample only: auto-draws growing branches like BOKUMYAKU.
    /// No game-state, enemy, wave, or combat calculations are performed here.
    /// </summary>
    [RequireComponent(typeof(SumiCanvas))]
    public sealed class SumiVeinDemo : MonoBehaviour
    {
        struct Node
        {
            public float x, y, direction;
            public int depth;
            public Node(float x, float y, float direction, int depth)
            { this.x = x; this.y = y; this.direction = direction; this.depth = depth; }
        }
        readonly Queue<Node> queue = new Queue<Node>();
        SumiCanvas canvas;
        System.Random random;
        float elapsed;
        int made;
        const int Limit = 72;

        void Start()
        {
            canvas = GetComponent<SumiCanvas>(); canvas.Initialize();
            Restart();
        }
        void Restart()
        {
            canvas.Clear();
            queue.Clear(); made = 0; elapsed = 0;
            random = new System.Random(20261018);
            for (int i = 0; i < 7; i++)
                queue.Enqueue(new Node(canvas.width / 2f, canvas.height / 2f,
                    i * Mathf.PI * 2f / 7f, 0));
        }
        void Update()
        {
            if (canvas == null) return;
            elapsed += Time.unscaledDeltaTime;
            if (elapsed > .19f && queue.Count > 0 && made < Limit)
            {
                elapsed = 0f;
                Grow(queue.Dequeue());
            }
        }
        void Grow(Node n)
        {
            float length = Mathf.Max(13f, 44f - n.depth * 5.1f);
            float a = n.direction + (float)(random.NextDouble() - .5) * .45f;
            float ex = Mathf.Clamp(n.x + Mathf.Cos(a) * length, 5, canvas.width - 5);
            float ey = Mathf.Clamp(n.y + Mathf.Sin(a) * length, 5, canvas.height - 5);
            if (Vector2.Distance(new Vector2(ex, ey), new Vector2(n.x, n.y)) < 6f) return;
            float cx = (n.x + ex) * .5f + Mathf.Cos(a + Mathf.PI / 2f) * 3.5f;
            float cy = (n.y + ey) * .5f + Mathf.Sin(a + Mathf.PI / 2f) * 3.5f;
            InkBrushSettings style = InkBrushSettings.Vein;
            style.radius = Mathf.Max(2.3f, 9f - n.depth);
            style.pigment = Mathf.Max(.3f, .78f - n.depth * .06f);
            canvas.Core.DrawBezier(new InkPoint(n.x, n.y, 0f, .95f),
                new InkPoint(cx, cy, .08f, .7f), new InkPoint(cx, cy, .16f, .8f),
                new InkPoint(ex, ey, .3f, .12f), style, 232 + made * 157, steps: 20);
            made++;
            if (n.depth >= 7 || made >= Limit) return;
            int count = random.NextDouble() < .72 ? 2 : 1;
            for (int i = 0; i < count; i++)
            {
                float turn = count == 1 ? (float)(random.NextDouble() - .5) * .4f
                    : (i == 0 ? -.4f : .4f);
                queue.Enqueue(new Node(ex, ey, a + turn, n.depth + 1));
            }
        }
        void OnGUI()
        {
            if (canvas == null || canvas.Texture == null) return;
            var rect = new Rect(16, 64, Screen.width - 32, Screen.height - 136);
            GUI.DrawTexture(rect, canvas.Texture, ScaleMode.StretchToFill);
            GUI.Label(new Rect(16, 12, Screen.width - 32, 28), "BOKUMYAKU / INK VEIN GROWTH TEST");
            GUI.Label(new Rect(16, 35, Screen.width - 32, 25), $"BRANCHES {made}/{Limit}    INK SURFACE  {canvas.width} x {canvas.height}");
            if (GUI.Button(new Rect(16, Screen.height - 60, Screen.width - 32, 42), "Restart branch growth"))
                Restart();
        }
    }
}
