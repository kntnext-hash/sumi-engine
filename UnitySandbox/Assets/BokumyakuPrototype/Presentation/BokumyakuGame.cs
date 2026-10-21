using System;
using System.Collections.Generic;
using Bokumyaku.Simulation;
using Bokumyaku.Persistence;
using ConnectPiece.SumiEngine;
using ConnectPiece.SumiEngine.Core;
using UnityEngine;

namespace Bokumyaku.Presentation
{
    /// <summary>Game-only presentation. The reusable ink engine never knows about enemies or currency.</summary>
    [RequireComponent(typeof(SumiCanvas))]
    public sealed class BokumyakuGame : MonoBehaviour
    {
        public MetaProgress Meta = new MetaProgress();
        public RunSimulator Run { get; private set; }
        public int InitialSeed = 20261022;
        public bool IsPaused { get; private set; }
        public int Speed { get; private set; } = 1;
        public bool AwaitingStart { get; private set; } = true;
        public event Action ProgressChanged;
        string saveWarning = "";
        bool progressLoaded;
        Font japaneseFont;
        public string SavePathOverride { get; set; } // QA may isolate save writes from player progress.
        public string SavePath => SavePathOverride ?? System.IO.Path.Combine(Application.persistentDataPath, "bokumyaku-prototype-v1.json");
        SumiCanvas canvas;
        Texture2D disc;
        GUIStyle title, heading, body, small, button, number;
        readonly List<Flash> flashes = new List<Flash>();
        readonly Dictionary<int, float> growth = new Dictionary<int, float>();
        float tickClock;
        int drawnBranches;
        bool metaTab, rewarded;
        string notice = "核を守り、墨の枝を育てる。";
        readonly Color ink = new Color(.13f, .15f, .14f);
        readonly Color muted = new Color(.43f, .44f, .40f);
        readonly Color gold = new Color(.65f, .47f, .19f);
        readonly Color red = new Color(.69f, .22f, .16f);
        readonly Color paper = new Color(.95f, .94f, .90f);
        static readonly string[] UpgradeNames = { "筆撃", "筆速", "流墨", "分岐", "筆幅", "滲み" };
        static readonly string[] UpgradeNotes = { "一撃の威力を高める", "攻撃間隔を短くする", "末端へ分配する墨量を増やす", "成長できる末端数を増やす", "敵へ届く範囲を広げる", "着弾後も墨が敵を削る" };
        static readonly string[] MetaNames = { "祖墨", "深根", "生脈", "豊穣" };
        static readonly string[] MetaNotes = { "基礎攻撃力を永続強化", "基礎流墨を永続強化", "枝の成長を早める", "撃破で得る墨を増やす" };

        void Start()
        {
            LoadProgress();
            EnsureCanvas();
            if (Run == null)
            {
                Run = new RunSimulator(InitialSeed, Meta);
                DrawNewBranches();
            }
        }
        void LoadProgress()
        {
            if (progressLoaded) return;
            Meta = MetaSaveStore.Load(SavePath, out saveWarning);
            progressLoaded = true;
        }
        public bool SaveProgress()
        {
            try { MetaSaveStore.Save(SavePath, Meta); saveWarning = ""; return true; }
            catch (Exception e) when (e is System.IO.IOException || e is UnauthorizedAccessException || e is ArgumentException)
            { saveWarning = "保存に失敗しました。再試行してください。"; Debug.LogWarning("Bokumyaku save: " + e.Message); return false; }
        }
        void EnsureCanvas()
        {
            if (canvas != null) return;
            canvas = GetComponent<SumiCanvas>();
            canvas.Initialize();
            canvas.paperColor = paper;
            canvas.inkColor = ink;
            canvas.diffusionFps = 0; // Persistent branches are fixed. Combat effects are transient overlays.
            canvas.uploadFps = 15;
            canvas.displayDensity = 7f;
        }
        public void StartRun(int seed)
        {
            EnsureCanvas();
            LoadProgress();
            Run = new RunSimulator(seed, Meta);
            AwaitingStart = false; IsPaused = false; Speed = 1;
            tickClock = 0; drawnBranches = 0; rewarded = false;
            flashes.Clear(); growth.Clear(); canvas.Clear();
            notice = "撃破で得た墨で強化。枝が増えるほど末端の墨圧は分散する。";
            DrawNewBranches();
        }
        public bool BuyUpgrade(UpgradeKind kind)
        {
            if (Run == null || AwaitingStart) return false;
            bool bought = Run.TryBuy(kind);
            notice = bought ? UpgradeNames[(int)kind] + "を強化しました。" : "墨が不足、または強化上限に達しています。";
            return bought;
        }
        public bool BuyMeta(MetaUpgrade kind)
        {
            if (Run != null && !AwaitingStart && Run.State == RunStatus.Running) return false;
            bool bought = Meta.TryBuy(kind);
            notice = bought ? "研究完了。次の周回から反映します。" : "結晶が不足、または研究上限に達しています。";
            if (bought) { SaveProgress(); ProgressChanged?.Invoke(); }
            return bought;
        }
        public void SetSpeed(int speed) { Speed = Mathf.Clamp(speed, 1, 2); }
        public void SetPaused(bool paused) { IsPaused = paused; }
        public void SetPolicy(GrowthPolicy policy) { Run?.SetPolicy(policy); }
        void Update()
        {
            for (int i = flashes.Count - 1; i >= 0; i--)
            {
                flashes[i].Life -= Time.unscaledDeltaTime;
                if (flashes[i].Life <= 0) flashes.RemoveAt(i);
            }
            if (Run == null || AwaitingStart || IsPaused || Run.State != RunStatus.Running) return;
            tickClock += Mathf.Min(Time.unscaledDeltaTime, .25f) * Speed;
            int budget = 16;
            while (tickClock >= 1f / RunSimulator.TickRate && budget-- > 0 && Run.State == RunStatus.Running)
            {
                tickClock -= 1f / RunSimulator.TickRate;
                Run.Tick();
                foreach (var hit in Run.Attacks)
                {
                    if (flashes.Count >= 96) flashes.RemoveAt(0);
                    flashes.Add(new Flash { A = new Vector2((float)hit.X, (float)hit.Y), B = new Vector2((float)hit.TargetX, (float)hit.TargetY), Life = .20f });
                }
                DrawNewBranches();
            }
            if (Run.State != RunStatus.Running && !rewarded)
            {
                rewarded = Run.ClaimReward(Meta);
                if (rewarded) { SaveProgress(); ProgressChanged?.Invoke(); }
                notice = Run.State == RunStatus.CapacityExceeded ? "試作版の処理上限 No reward claimed." : "結晶で永続強化して、次の周回へ。";
            }
        }
        void DrawNewBranches()
        {
            var branches = Run.Branches;
            for (; drawnBranches < branches.Count; drawnBranches++)
            {
                var b = branches[drawnBranches];
                if (b.ParentId < 0) continue;
                var parent = branches[b.ParentId];
                // Gameplay branches must remain readable at the reduced battlefield size.
                // Dense fibers retain a brush edge without the sparse gaps of the Dry preset.
                var brush = InkBrushSettings.Dense;
                brush.radius = Mathf.Lerp(8f, 4f, Mathf.Clamp01((float)b.PathLength / 400f));
                brush.pigment = 1f; brush.water = .15f; brush.depletion = .002f;
                brush.roughness = .12f;
                float x0 = (float)parent.ViewX, y0 = (float)parent.ViewY;
                float x1 = (float)b.ViewX, y1 = (float)b.ViewY;
                float sx = canvas.width / 384f, sy = canvas.height / 512f;
                canvas.Begin(new InkPoint(x0 * sx, y0 * sy, 0, 1f), brush, unchecked(Run.Seed + b.Id * 7919));
                // A gently bowed centerline gives the growing branch a living brush gesture.
                for (int n = 1; n <= 8; n++)
                {
                    float t = n / 8f, bow = Mathf.Sin(t * Mathf.PI) * (b.Id % 2 == 0 ? 1.4f : -1.4f);
                    canvas.Move(new InkPoint((Mathf.Lerp(x0, x1, t) + bow) * sx,
                        Mathf.Lerp(y0, y1, t) * sy, t * .35f, Mathf.Lerp(1f, .7f, t)));
                }
                canvas.End(); growth[b.Id] = Time.unscaledTime;
            }
        }
        void InitStyles()
        {
            if (body != null) return;
            body = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true, alignment = TextAnchor.MiddleLeft };
            japaneseFont = Font.CreateDynamicFontFromOSFont(new[] { "Yu Gothic", "Meiryo", "Noto Sans CJK JP", "Arial" }, 16);
            if (japaneseFont != null) body.font = japaneseFont;
            body.normal.textColor = ink;
            small = new GUIStyle(body) { fontSize = 13 }; small.normal.textColor = muted;
            heading = new GUIStyle(body) { fontSize = 19, fontStyle = FontStyle.Bold };
            title = new GUIStyle(body) { fontSize = 32, fontStyle = FontStyle.Bold };
            number = new GUIStyle(body) { fontSize = 26, fontStyle = FontStyle.Bold };
            button = new GUIStyle(GUI.skin.button) { fontSize = 14, alignment = TextAnchor.MiddleCenter };
            button.font = body.font;
            button.normal.background = Texture2D.whiteTexture;
            button.hover.background = Texture2D.whiteTexture;
            button.active.background = Texture2D.whiteTexture;
            button.normal.textColor = ink; button.hover.textColor = ink; button.active.textColor = ink;
            disc = new Texture2D(48, 48, TextureFormat.RGBA32, false);
            disc.name = "Bokumyaku procedural node"; disc.hideFlags = HideFlags.DontSave;
            var pixels = new Color[48 * 48];
            for (int y = 0; y < 48; y++) for (int x = 0; x < 48; x++)
                pixels[y * 48 + x] = new Color(1, 1, 1, Mathf.Clamp01(24f - Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(24, 24))));
            disc.SetPixels(pixels); disc.Apply();
        }
        void OnGUI()
        {
            if (Run == null || canvas == null) return;
            InitStyles();
            var oldMatrix = GUI.matrix; var oldColor = GUI.color; var oldBackground = GUI.backgroundColor; bool oldEnabled = GUI.enabled;
            bool portrait = Screen.width < Screen.height * .95f;
            float width = portrait ? 440 : 1080, height = portrait ? 1060 : 760;
            float scale = Mathf.Min(Screen.width / width, Screen.height / height);
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - width * scale) / 2, (Screen.height - height * scale) / 2, 0), Quaternion.identity, Vector3.one * scale);
            try
            {
                Fill(new Rect(0, 0, width, height), paper);
                GUI.Label(new Rect(24, 14, 300, 43), "墨 脈", title);
                GUI.Label(new Rect(25, 55, 330, 20), "B O K U M Y A K U   /   墨が生きる防衛", small);
                Fill(new Rect(24, 85, width - 48, 1), new Color(.75f, .72f, .64f));
                Rect field = portrait ? new Rect(28, 111, 384, 512) : new Rect(55, 116, 432, 576);
                DrawField(field);
                if (portrait) DrawPanel(new Rect(24, 648, 392, 388), true);
                else DrawPanel(new Rect(558, 110, 494, 616), false);
                GUI.Label(new Rect(field.x, field.yMax + 7, field.width, 18), "墨の枝  ／  金の末端  ／  朱の敵", small);
                GUI.Label(new Rect(width - 170, 42, 146, 25), "縦切り試作  0.1", small);
            }
            finally { GUI.matrix = oldMatrix; GUI.color = oldColor; GUI.backgroundColor = oldBackground; GUI.enabled = oldEnabled; }
        }
        Vector2 View(Rect field, double x, double y) { return new Vector2(field.x + (float)x / 384f * field.width, field.yMax - (float)y / 512f * field.height); }
        Vector2 Logical(Rect field, Vector2 point) { return View(field, 192 + point.x * .48, 256 + point.y * .48); }
        void DrawField(Rect field)
        {
            GUI.DrawTexture(field, canvas.Texture, ScaleMode.StretchToFill);
            var center = View(field, 192, 256);
            Ring(center, field.width * .45f, new Color(.58f, .53f, .42f, .17f));
            Ring(center, field.width * .25f, new Color(.58f, .53f, .42f, .10f));
            foreach (var b in Run.Branches)
            {
                if (!b.IsTip) continue;
                var point = View(field, b.ViewX, b.ViewY);
                Dot(point, 5, gold);
                if (growth.TryGetValue(b.Id, out float started) && Time.unscaledTime - started < .8f)
                    Ring(point, 6 + (Time.unscaledTime - started) * 15, new Color(gold.r, gold.g, gold.b, .5f * (1 - (Time.unscaledTime - started) / .8f)));
            }
            foreach (var e in Run.Enemies)
            {
                Vector2 point = View(field, e.ViewX, e.ViewY);
                float radius = e.Kind == EnemyKind.Heavy ? 8 : e.Kind == EnemyKind.Fast ? 4 : 6;
                Dot(point, radius + 2, new Color(red.r, red.g, red.b, .15f)); Dot(point, radius, red);
                if (e.Kind == EnemyKind.Fast) Line(point + new Vector2(-9, 3), point + new Vector2(-4, 3), 1, red);
                if (e.Kind == EnemyKind.Heavy) Ring(point, 11, red);
                if (e.Hp < e.MaxHp) { Fill(new Rect(point.x - 9, point.y - radius - 6, 18, 2), new Color(0, 0, 0, .12f)); Fill(new Rect(point.x - 9, point.y - radius - 6, 18 * Mathf.Clamp01((float)(e.Hp / e.MaxHp)), 2), red); }
            }
            foreach (var flash in flashes)
            {
                float alpha = Mathf.Clamp01(flash.Life / .20f);
                Line(Logical(field, flash.A), Logical(field, flash.B), 1.6f, new Color(ink.r, ink.g, ink.b, alpha));
                Ring(Logical(field, flash.B), 3 + 10 * (1 - alpha), new Color(gold.r, gold.g, gold.b, alpha));
            }
            Dot(center, 14, ink); Dot(center, 5, gold);
            double minPressure = double.MaxValue, maxPressure = 0;
            foreach (var branch in Run.Branches)
            {
                if (!branch.IsTip) continue;
                double pressure = Run.Pressure(branch);
                minPressure = Math.Min(minPressure, pressure); maxPressure = Math.Max(maxPressure, pressure);
            }
            GUI.Label(new Rect(field.x + 12, field.yMax - 30, field.width - 24, 22),
                "流墨 Q " + Run.RootSupply.ToString("0.0") + "  ／  末端墨圧 P " + minPressure.ToString("0.00") + "–" + maxPressure.ToString("0.00"), small);
            GUI.Label(new Rect(field.x + 12, field.y + 8, field.width - 24, 25), "波 " + Run.Wave.ToString("00") + "  /  20", heading);
            GUI.Label(new Rect(field.x + 12, field.y + 35, 240, 22), "核 " + Run.CoreHp + "/10   ·   " + Run.TipCount + " 末端", small);
            Fill(new Rect(field.x + 12, field.y + 60, 100, 3), new Color(0, 0, 0, .10f));
            Fill(new Rect(field.x + 12, field.y + 60, 10 * Run.CoreHp, 3), gold);
            if (AwaitingStart || Run.State != RunStatus.Running)
            {
                var box = new Rect(field.x + 22, field.center.y - 77, field.width - 44, 154);
                Fill(box, new Color(.96f, .95f, .91f, .96f));
                string status = AwaitingStart ? "一滴から、命が芽吹く。" : Run.State == RunStatus.Defeated ? "墨が静まり、核が砕けた。" : Run.State == RunStatus.TestCompleted ? "二十の波を、耐え抜いた。" : "試作版の処理上限";
                GUI.Label(new Rect(box.x + 14, box.y + 12, box.width - 28, 35), status, heading);
                GUI.Label(new Rect(box.x + 14, box.y + 48, box.width - 28, 48), AwaitingStart ? "核を守る墨の枝。敵を倒すと枝が伸びる。六つの強化で、生き方を選ぶ。" : "到達 " + Run.CompletedWave + " 波突破  ·  " + Run.Kills + " 撃破\n獲得結晶: " + Run.CrystalsReward, body);
                if (Button(new Rect(box.x + 14, box.y + 108, box.width - 28, 32), AwaitingStart ? "出撃する" : "再び芽吹く", true)) StartRun(unchecked(InitialSeed + (AwaitingStart ? 0 : Run.Seed - InitialSeed + 1)));
            }
        }
        void DrawPanel(Rect panel, bool compact)
        {
            float x = panel.x, y = panel.y, w = panel.width;
            GUI.Label(new Rect(x, y, w * .48f, 17), "墨  ／  周回内", small);
            GUI.Label(new Rect(x, y + 16, w * .48f, 33), Run.RunInk.ToString(), number);
            GUI.Label(new Rect(x + w * .50f, y, w * .5f, 17), "結晶  ／  永続強化", small);
            GUI.Label(new Rect(x + w * .50f, y + 16, w * .5f, 33), Meta.Crystals.ToString(), number);
            y += compact ? 53 : 65;
            float bw = (w - 16) / 3;
            if (Button(new Rect(x, y, bw, 27), IsPaused ? "再開" : "一時停止", IsPaused)) IsPaused = !IsPaused;
            if (Button(new Rect(x + bw + 8, y, bw, 27), Speed == 1 ? "速度  1倍" : "速度  2倍", Speed == 2)) SetSpeed(Speed == 1 ? 2 : 1);
            if (Button(new Rect(x + (bw + 8) * 2, y, bw, 27), metaTab ? "周回強化へ" : "永続強化へ", metaTab)) metaTab = !metaTab;
            y += compact ? 34 : 44;
            if (metaTab)
            {
                GUI.Label(new Rect(x, y, w, 26), "命をつなぐ研究", heading); y += 29;
                GUI.Label(new Rect(x, y, w, compact ? 27 : 40), "周回の合間に購入。次の出撃から永続的に働く。", small); y += compact ? 30 : 49;
                for (int i = 0; i < 4; i++)
                {
                    var kind = (MetaUpgrade)i;
                    float rowHeight = compact ? 38 : 65;
                    GUI.Label(new Rect(x, y, w - 104, 21), MetaNames[i] + "  " + Meta.Level(kind), body);
                    if (!compact) GUI.Label(new Rect(x, y + 23, w - 104, 24), MetaNotes[i], small);
                    bool canBuy = (AwaitingStart || Run.State != RunStatus.Running) && Meta.Crystals >= Meta.Cost(kind) && Meta.Level(kind) < 20;
                    GUI.enabled = canBuy;
                    if (Button(new Rect(x + w - 96, y, 96, 29), Meta.Level(kind) >= 20 ? "MAX" : Meta.Cost(kind) + " 結晶", canBuy)) BuyMeta(kind);
                    GUI.enabled = true; y += rowHeight;
                }
            }
            else
            {
                GUI.Label(new Rect(x, y, w, 21), "枝の成長方針", compact ? body : heading); y += compact ? 23 : 30;
                var names = new[] { "均衡", "迎撃", "集中" };
                for (int i = 0; i < 3; i++) if (Button(new Rect(x + (bw + 8) * i, y, bw, 26), names[i], Run.Policy == (GrowthPolicy)i)) SetPolicy((GrowthPolicy)i);
                y += compact ? 31 : 44;
                int[] levels = Run.UpgradeLevels;
                int[] caps = { 30, 25, 25, 9, 10, 10 };
                for (int i = 0; i < 6; i++)
                {
                    var kind = (UpgradeKind)i;
                    float rowHeight = compact ? 29 : 53;
                    GUI.Label(new Rect(x, y, w - 100, 23), UpgradeNames[i] + "  " + levels[i], body);
                    if (!compact) GUI.Label(new Rect(x, y + 23, w - 104, 20), UpgradeNotes[i], small);
                    bool canBuy = !AwaitingStart && Run.State == RunStatus.Running && Run.RunInk >= Run.UpgradeCost(kind) && levels[i] < caps[i];
                    GUI.enabled = canBuy;
                    if (Button(new Rect(x + w - 94, y, 94, compact ? 25 : 31), levels[i] >= caps[i] ? "MAX" : Run.UpgradeCost(kind) + " 墨", canBuy)) BuyUpgrade(kind);
                    GUI.enabled = true; y += rowHeight;
                }
            }
            if (compact && string.IsNullOrEmpty(saveWarning))
                GUI.Label(new Rect(x, panel.yMax - 36, w, 34), notice, small);
            if (!compact)
            {
                y += 8; Fill(new Rect(x, y, w, 1), new Color(.76f, .73f, .66f)); y += 10;
                GUI.Label(new Rect(x, y, w, 42), notice, small);
                GUI.Label(new Rect(x, panel.yMax - 22, w, 22), "SEED " + Run.Seed + "   /   " + (Run.TickCount / 30f).ToString("0.0") + "s   /   " + Run.Kills + " 撃破", small);
            }
            if (!string.IsNullOrEmpty(saveWarning))
            {
                GUI.Label(new Rect(x, panel.yMax - 44, w - 100, 42), saveWarning, small);
                if (Button(new Rect(x + w - 96, panel.yMax - 41, 96, 27), "保存再試行", true)) SaveProgress();
            }
        }
        bool Button(Rect rect, string label, bool selected)
        {
            Color before = GUI.backgroundColor;
            GUI.backgroundColor = selected ? new Color(.82f, .72f, .51f) : new Color(.89f, .88f, .83f);
            bool clicked = GUI.Button(rect, label, button);
            GUI.backgroundColor = before;
            return clicked;
        }
        void Fill(Rect rect, Color color) { var old = GUI.color; GUI.color = color; GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = old; }
        void Dot(Vector2 center, float radius, Color color) { var old = GUI.color; GUI.color = color; GUI.DrawTexture(new Rect(center.x - radius, center.y - radius, radius * 2, radius * 2), disc); GUI.color = old; }
        void Line(Vector2 a, Vector2 b, float width, Color color)
        {
            var matrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg, a);
            Fill(new Rect(a.x, a.y - width * .5f, Vector2.Distance(a, b), width), color);
            GUI.matrix = matrix;
        }
        void Ring(Vector2 center, float radius, Color color)
        {
            Vector2 previous = center + Vector2.right * radius;
            for (int i = 1; i <= 48; i++)
            {
                float angle = i / 48f * Mathf.PI * 2;
                var next = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                Line(previous, next, .7f, color); previous = next;
            }
        }
        void OnDestroy() { if (disc != null) Destroy(disc); if (japaneseFont != null) Destroy(japaneseFont); }
        sealed class Flash { public Vector2 A, B; public float Life; }
    }
}



