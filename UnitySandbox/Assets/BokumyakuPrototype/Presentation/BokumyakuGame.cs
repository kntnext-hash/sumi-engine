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
    public sealed partial class BokumyakuGame : MonoBehaviour
    {
        public enum GamePage { Home, Battle, Research, Records }
        public GamePage Page { get; private set; } = GamePage.Home;
        CampaignProgress campaign;
        public CampaignProgress Campaign => campaign == null ? null : JsonUtility.FromJson<CampaignProgress>(JsonUtility.ToJson(campaign));
        public string ActiveRunId => campaign == null ? "" : campaign.ActiveRunId;
        public bool ResumeAvailable => campaign != null && campaign.HasActiveRun && Run != null && Run.State == RunStatus.Running;
        public int BestWave => campaign == null ? 0 : campaign.BestWave;
        public CampaignResult LastResult => campaign == null || campaign.LastResult == null ? null : JsonUtility.FromJson<CampaignResult>(JsonUtility.ToJson(campaign.LastResult));
        public MetaProgress Meta = new MetaProgress();
        public RunSimulator Run { get; private set; }
        public int InitialSeed = 20261022;
        public bool IsPaused { get; private set; }
        public int Speed { get; private set; } = 1;
        public bool AwaitingStart { get; private set; } = true;
        public BuildPreset SelectedBuild { get; private set; } = BuildPreset.Manual;
        public GrowthPolicy SelectedPolicy { get; private set; } = GrowthPolicy.Balanced;
        public WeaponKind SelectedWeapon { get; private set; } = WeaponKind.Point;
        readonly AutoRepeatController autoRepeat = new AutoRepeatController();
        public bool AutoRepeatEnabled => autoRepeat.Enabled;
        public bool AutoRepeatPending => autoRepeat.IsPending;
        public double AutoRepeatSeconds => autoRepeat.SecondsRemaining;
        public int CompletedRuns { get; private set; }
        public long SessionCrystals { get; private set; }
        public int LoadedArtCount { get; private set; }
        public int BranchOverlayRebuilds { get; private set; }
        public Texture2D BranchOverlay => branchOverlay;
        public event Action ProgressChanged;
        string saveWarning = "";
        bool progressLoaded;
        Font japaneseFont;
        string savePathOverride;
        public string SavePathOverride
        {
            get => savePathOverride;
            set
            {
                if (savePathOverride == value) return;
                autoRepeat.SetEnabled(false); savePathOverride = value;
                campaign = null; Run = null; Meta = new MetaProgress(); progressLoaded = false;
                loadBlocked = storageFailed = false; rewarded = rewardSaved = endHandled = false;
                CompletedRuns = 0; SessionCrystals = 0; AwaitingStart = true; IsPaused = true;
                Page = GamePage.Home; saveWarning = "";
            }
        }
        public string SavePath => SavePathOverride ?? System.IO.Path.Combine(Application.persistentDataPath, "bokumyaku-campaign-v8.json");
        public string LatestPreviousCampaignPath => SavePathOverride == null ? System.IO.Path.Combine(Application.persistentDataPath, "bokumyaku-campaign-v7.json") : SavePathOverride + ".v7";
        public string PreviousV6CampaignPath => SavePathOverride == null ? System.IO.Path.Combine(Application.persistentDataPath, "bokumyaku-campaign-v6.json") : SavePathOverride + ".v6";
        public string PreviousV5CampaignPath => SavePathOverride == null ? System.IO.Path.Combine(Application.persistentDataPath, "bokumyaku-campaign-v5.json") : SavePathOverride + ".v5";
        public string PreviousV4CampaignPath => SavePathOverride == null ? System.IO.Path.Combine(Application.persistentDataPath, "bokumyaku-campaign-v4.json") : SavePathOverride + ".v4";
        public string PreviousCampaignPath => SavePathOverride == null ? System.IO.Path.Combine(Application.persistentDataPath, "bokumyaku-campaign-v3.json") : SavePathOverride + ".v3";
        public string OlderCampaignPath => SavePathOverride == null ? System.IO.Path.Combine(Application.persistentDataPath, "bokumyaku-campaign-v2.json") : SavePathOverride + ".v2";
        string LegacySavePath => SavePathOverride == null ? System.IO.Path.Combine(Application.persistentDataPath, "bokumyaku-prototype-v1.json") : SavePathOverride + ".legacy";
        SumiCanvas canvas;
        Texture2D disc;
        Texture2D paperArt, landscapeArt, ensoArt, branchOverlay;
        readonly Texture2D[] weaponArt = new Texture2D[6];
        Color32[] branchPixels;
        GUIStyle title, heading, body, small, button, number, iconLabel;
        readonly List<Flash> flashes = new List<Flash>();
        readonly Dictionary<int, float> growth = new Dictionary<int, float>();
        float tickClock;
        int drawnBranches;
        bool rewarded;
        bool endHandled, rewardSaved;
        bool loadBlocked, abandonConfirmation, storageFailed;
        float checkpointClock;
        Vector2 panelScroll;
        readonly int[] observedLevels = new int[7];
        readonly float[] levelChangedAt = new float[7];
        string notice = "核を守り、墨の枝を育てる。";
        readonly Color ink = new Color(.13f, .15f, .14f);
        readonly Color muted = new Color(.43f, .44f, .40f);
        readonly Color gold = new Color(.65f, .47f, .19f);
        readonly Color red = new Color(.69f, .22f, .16f);
        readonly Color paper = new Color(.95f, .94f, .90f);
        static readonly string[] UpgradeNames = { "筆撃", "筆速", "流墨", "分岐", "筆幅", "滲み", "採墨" };
        static readonly string[] UpgradeNotes = { "一撃の威力を高める", "攻撃間隔を短くする", "末端へ分配する墨量を増やす", "成長できる末端数を増やす", "敵へ届く範囲を広げる", "着弾後も墨が敵を削る", "購入後の有効撃破で通常結晶を増やす" };
        static readonly string[] MetaNames = { "祖墨", "深根", "生脈", "豊穣" };
        static readonly string[] MetaNotes = { "基礎攻撃力を永続強化", "基礎流墨を永続強化", "枝の成長を早める", "撃破で得る墨を増やす" };
        static readonly string[] BuildNames = { "手動", "採墨・周回", "供給・記録" };
        static readonly string[] WeaponNames = { "点墨", "貫筆", "滲墨", "払い", "円相", "共鳴" };
        static readonly string[] WeaponNotes = {
            "単体へ素早く連射。狙った敵を一体ずつ削る。",
            "直線上の最大3体を貫く払い。攻撃間隔は長い。",
            "紙面に残る範囲攻撃。速い敵は範囲から抜けやすい。",
            "前方へ扇状に払う。まとまった敵を迎え撃つ。",
            "末端を囲む環状攻撃。至近距離には当たらない。",
            "近くの敵へ連鎖する墨撃。敵の集まりを狙う。"
        };
        static readonly string[] BuildNotes = {
            "購入は自分で選ぶ。墨を貯めて好きな強化へ投資。",
            "採墨にも墨を使い、火力との配分を選ぶ。周回用の候補。",
            "流墨を先に整え、攻撃へ投資する。記録用の候補。"
        };
        string BuildDescription(BuildPreset build)
        {
            if (Run != null && Run.RulesVersion == 1)
                return "旧ルールの投資順で続行。採墨は次の新規出撃から利用できます。";
            if (build == BuildPreset.Farming)
                return "採墨は通常結晶 +" + (RunSimulator.HarvestGainPerLevel * 100).ToString("0.#")
                    + "%/Lv（最大" + RunSimulator.HarvestMaxLevel + "）。火力への投資を抑える周回候補。";
            return BuildNotes[(int)build];
        }

        void Start()
        {
            if (!progressLoaded) TryReloadProgress();
        }
        void LoadProgress()
        {
            if (!progressLoaded) TryReloadProgress();
        }
        static MetaProgress CopyMeta(MetaProgress meta)
        {
            return new MetaProgress { Crystals = meta.Crystals, Damage = meta.Damage, Supply = meta.Supply, Growth = meta.Growth, Harvest = meta.Harvest };
        }
        void ApplyCampaign(CampaignProgress next)
        {
            campaign = next; Meta = CopyMeta(next.Meta); saveWarning = ""; checkpointClock = 0; storageFailed = false;
        }
        public bool TryReloadProgress()
        {
            autoRepeat.SetEnabled(false); IsPaused = true;
            EnsureCanvas();
            try
            {
                bool firstNewCampaign = !progressLoaded && !SaveFamilyExists(SavePath) && !SaveFamilyExists(LatestPreviousCampaignPath) && !SaveFamilyExists(PreviousV6CampaignPath) && !SaveFamilyExists(PreviousV5CampaignPath) && !SaveFamilyExists(PreviousV4CampaignPath) && !SaveFamilyExists(PreviousCampaignPath) && !SaveFamilyExists(OlderCampaignPath) && !SaveFamilyExists(LegacySavePath);
                string warning;
                var loaded = CampaignSaveStore.LoadLatest(SavePath, LatestPreviousCampaignPath, PreviousV6CampaignPath, PreviousV5CampaignPath, PreviousV4CampaignPath, PreviousCampaignPath, OlderCampaignPath, LegacySavePath, out warning);
                if (firstNewCampaign) SelectedBuild = BuildPreset.Farming;
                var restored = loaded.HasActiveRun ? RunSimulator.Restore(loaded.ActiveRun) : new RunSimulator(InitialSeed, loaded.Meta);
                ApplyCampaign(loaded); Run = restored; progressLoaded = true; loadBlocked = false;
                saveWarning = warning; AwaitingStart = !loaded.HasActiveRun; IsPaused = true;
                if (loaded.HasActiveRun)
                {
                    SelectedBuild = Run.Build; SelectedPolicy = Run.Policy; SelectedWeapon = Run.Weapon; Speed = loaded.ActiveSpeed;
                }
                else { Run.SetBuild(SelectedBuild); Run.SetPolicy(SelectedPolicy); Run.SetWeapon(SelectedWeapon); }
                rewarded = rewardSaved = endHandled = false; tickClock = 0; Page = GamePage.Home;
                ResetInk(); notice = loaded.HasActiveRun ? (Run.RulesVersion == 1 ? "旧ルールで続行。採墨は次の新規出撃から利用できます。" : "途中の状態を読み込みました。続行を押すまで時間は進みません。") : "武器と構成を選び、出撃してください。";
                return true;
            }
            catch (Exception e) when (IsSaveException(e))
            {
                loadBlocked = true; progressLoaded = false; saveWarning = "保存を読めません。原本を保護し、操作を停止しています。";
                if (Run == null) { Run = new RunSimulator(InitialSeed); AwaitingStart = true; ResetInk(); }
                Debug.LogWarning("Bokumyaku load: " + e.Message); return false;
            }
        }
        static bool IsSaveException(Exception e)
        {
            return e is System.IO.IOException || e is System.IO.InvalidDataException || e is UnauthorizedAccessException || e is ArgumentException || e is InvalidOperationException || e is OverflowException;
        }
        static bool SaveFamilyExists(string path) => System.IO.File.Exists(path) || System.IO.File.Exists(path + ".bak");
        void SaveFailed(Exception e)
        {
            rewardSaved = false; autoRepeat.Cancel(); IsPaused = true; storageFailed = true;
            saveWarning = "保存に失敗。停止中です。再試行してください。";
            Debug.LogWarning("Bokumyaku save: " + e.Message);
        }
        public bool SaveProgress()
        {
            if (loadBlocked || !progressLoaded || campaign == null) return false;
            try
            {
                if (!AwaitingStart && Run != null && Run.State != RunStatus.Running && !rewarded)
                {
                    int previousRuns = campaign.TotalRuns; long previousCrystals = campaign.TotalCrystals;
                    var next = CampaignSaveStore.SettleRun(SavePath, campaign, campaign.ActiveRunId, Run);
                    int runsAdded = next.TotalRuns - previousRuns; long crystalsAdded = next.TotalCrystals - previousCrystals;
                    ApplyCampaign(next); CompletedRuns += runsAdded; SessionCrystals += crystalsAdded;
                    rewarded = rewardSaved = endHandled = true;
                    autoRepeat.Schedule(Run.State, true); ProgressChanged?.Invoke();
                }
                else if (campaign.HasActiveRun && Run != null)
                    ApplyCampaign(CampaignSaveStore.SaveActive(SavePath, campaign, Run, Speed));
                else
                {
                    ApplyCampaign(CampaignSaveStore.SaveMeta(SavePath, campaign, Meta));
                    bool wasUnsaved = rewarded && !rewardSaved; if (rewarded) rewardSaved = true;
                    if (wasUnsaved) autoRepeat.Schedule(Run.State, true);
                }
                return true;
            }
            catch (Exception e) when (IsSaveException(e)) { SaveFailed(e); return false; }
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
            paperArt = LoadArt("paper_ivory_01");
            landscapeArt = LoadArt("landscape_mist_01");
            ensoArt = LoadArt("enso_frame_01");
            weaponArt[0] = LoadArt("weapon_point_01");
            weaponArt[1] = LoadArt("weapon_pierce_01");
            weaponArt[2] = LoadArt("weapon_bleed_01");
        }
        Texture2D LoadArt(string name)
        {
            var texture = Resources.Load<Texture2D>("BokumyakuArt/" + name);
            if (texture != null) LoadedArtCount++;
            return texture;
        }
        public void StartRun(int seed) { TryStartRun(seed); }
        public bool TryStartRun(int seed) { return BeginRun(seed, 1); }
        bool BeginRun(int seed, int speed)
        {
            autoRepeat.Cancel();
            EnsureCanvas();
            LoadProgress();
            if (loadBlocked || campaign == null) return false;
            if (Run != null && !AwaitingStart && Run.State != RunStatus.Running && !rewarded && !SaveProgress()) return false;
            var nextRun = new RunSimulator(seed, campaign.Meta);
            nextRun.SetBuild(SelectedBuild); nextRun.SetPolicy(SelectedPolicy); nextRun.SetWeapon(SelectedWeapon);
            try { ApplyCampaign(CampaignSaveStore.BeginRun(SavePath, campaign, nextRun, speed)); }
            catch (Exception e) when (IsSaveException(e)) { SaveFailed(e); return false; }
            Run = nextRun; AwaitingStart = false; IsPaused = false; Speed = speed; Page = GamePage.Battle;
            tickClock = 0; drawnBranches = 0; rewarded = false; endHandled = false; rewardSaved = false;
            ResetInk();
            notice = "撃破で得た墨で強化。枝が増えるほど末端の墨圧は分散する。";
            return true;
        }
        void ResetInk()
        {
            drawnBranches = 0; inkBursts.Clear(); watchedEnemies.Clear(); ObservedInkBursts = 0; flashes.Clear(); growth.Clear(); canvas.Clear();
            Array.Clear(observedLevels, 0, observedLevels.Length);
            Array.Clear(levelChangedAt, 0, levelChangedAt.Length);
            DrawNewBranches();
        }
        public bool ResumeRun()
        {
            if (!ResumeAvailable || loadBlocked || storageFailed) return false;
            IsPaused = false; AwaitingStart = false; Page = GamePage.Battle; return true;
        }
        public bool AbandonRun()
        {
            autoRepeat.Cancel();
            if (campaign == null || loadBlocked) return false;
            if (Run != null && !AwaitingStart && Run.State != RunStatus.Running) return false;
            try { ApplyCampaign(CampaignSaveStore.DiscardRun(SavePath, campaign)); }
            catch (Exception e) when (IsSaveException(e)) { SaveFailed(e); return false; }
            Run = new RunSimulator(InitialSeed, Meta); Run.SetBuild(SelectedBuild); Run.SetPolicy(SelectedPolicy); Run.SetWeapon(SelectedWeapon);
            AwaitingStart = true; IsPaused = true; rewarded = endHandled = rewardSaved = false; abandonConfirmation = false;
            Page = GamePage.Home; ResetInk(); notice = "途中の周回を終了しました。報酬はありません。"; return true;
        }
        public bool BuyUpgrade(UpgradeKind kind)
        {
            if (Run == null || AwaitingStart || loadBlocked || storageFailed) return false;
            bool bought = Run.TryBuy(kind);
            notice = bought ? UpgradeNames[(int)kind] + "を強化しました。" : "墨が不足、または強化上限に達しています。";
            if (bought) ObserveUpgrades();
            return bought;
        }
        public bool TryRepair()
        {
            if (Run == null || AwaitingStart || Run.State != RunStatus.Running || loadBlocked || storageFailed) return false;
            bool repaired = Run.TryRepair();
            notice = repaired ? "核を修復しました。" : "墨不足・修復上限・核の状態を確認してください。";
            if (repaired) SaveProgress();
            return repaired;
        }
        public bool BuyMeta(MetaUpgrade kind)
        {
            if (campaign == null || campaign.HasActiveRun || loadBlocked || storageFailed) return false;
            var proposed = CopyMeta(Meta);
            if (!proposed.TryBuy(kind)) { notice = "結晶が不足、または研究上限に達しています。"; return false; }
            try { ApplyCampaign(CampaignSaveStore.SaveMeta(SavePath, campaign, proposed)); }
            catch (Exception e) when (IsSaveException(e)) { SaveFailed(e); return false; }
            notice = "研究完了。次の周回から反映します。"; ProgressChanged?.Invoke(); return true;
        }
        public void SetSpeed(int speed) { Speed = Mathf.Clamp(speed, 1, 2); }
        public void SetPaused(bool paused)
        {
            if (!paused && (loadBlocked || storageFailed)) return;
            IsPaused = paused;
            if (paused && progressLoaded && !AwaitingStart) SaveProgress();
        }
        public void SetPage(GamePage page)
        {
            if (!Enum.IsDefined(typeof(GamePage), page)) throw new ArgumentOutOfRangeException(nameof(page));
            Page = page; panelScroll = Vector2.zero;
            if (page != GamePage.Battle) SetPaused(true);
        }
        public bool SaveBuildSlot(int index)
        {
            if (campaign == null || loadBlocked || storageFailed) return false;
            var slot = new BuildSlot { Name = "構成" + (index + 1), Weapon = SelectedWeapon, Build = SelectedBuild, Policy = SelectedPolicy, Speed = Speed };
            try { ApplyCampaign(CampaignSaveStore.SaveSlot(SavePath, campaign, index, slot)); notice = slot.Name + "を保存しました。"; return true; }
            catch (Exception e) when (IsSaveException(e)) { SaveFailed(e); return false; }
        }
        public bool LoadBuildSlot(int index)
        {
            if (campaign == null || index < 0 || index >= campaign.Slots.Length || loadBlocked || storageFailed) return false;
            var slot = campaign.Slots[index]; SetWeapon(slot.Weapon); SetBuild(slot.Build); SetPolicy(slot.Policy); SetSpeed(slot.Speed);
            notice = slot.Name + "を呼び出しました。"; return true;
        }
        public void SetAutoRepeat(bool enabled)
        {
            if (autoRepeat.Enabled == enabled) return;
            autoRepeat.SetEnabled(enabled);
            if (enabled && !AwaitingStart && rewarded && rewardSaved && Run != null)
                autoRepeat.Schedule(Run.State, true);
        }
        public void SetPolicy(GrowthPolicy policy)
        {
            if (!Enum.IsDefined(typeof(GrowthPolicy), policy)) throw new ArgumentOutOfRangeException(nameof(policy));
            SelectedPolicy = policy;
            Run?.SetPolicy(policy);
        }
        public void SetBuild(BuildPreset build)
        {
            if (!Enum.IsDefined(typeof(BuildPreset), build)) throw new ArgumentOutOfRangeException(nameof(build));
            SelectedBuild = build;
            Run?.SetBuild(build);
            notice = BuildDescription(build);
        }
        public void SetWeapon(WeaponKind weapon)
        {
            if (!Enum.IsDefined(typeof(WeaponKind), weapon)) throw new ArgumentOutOfRangeException(nameof(weapon));
            SelectedWeapon = weapon;
            Run?.SetWeapon(weapon);
            notice = WeaponNotes[(int)weapon];
        }
        void Update()
        {
            AgeBursts();
            for (int i = flashes.Count - 1; i >= 0; i--)
            {
                flashes[i].Life -= Time.unscaledDeltaTime;
                if (flashes[i].Life <= 0) flashes.RemoveAt(i);
            }
            if (Run == null || AwaitingStart || loadBlocked || storageFailed) return;
            if (Run.State != RunStatus.Running)
            {
                CompleteRun();
                if (autoRepeat.Advance(Mathf.Min(Time.unscaledDeltaTime, .25f), IsPaused))
                {
                    int nextSeed = unchecked(Run.Seed + 1), previousSpeed = Speed;
                    BeginRun(nextSeed, previousSpeed);
                }
                return;
            }
            if (IsPaused) return;
            tickClock += Mathf.Min(Time.unscaledDeltaTime, .25f) * Speed;
            int budget = 16;
            while (tickClock >= 1f / RunSimulator.TickRate && budget-- > 0 && Run.State == RunStatus.Running)
            {
                tickClock -= 1f / RunSimulator.TickRate;
                WatchEnemies();
                Run.Tick();
                ObserveDefeats();
                foreach (var hit in Run.Attacks)
                {
                    if (flashes.Count >= 96) flashes.RemoveAt(0);
                    flashes.Add(new Flash { A = new Vector2((float)hit.X, (float)hit.Y), B = new Vector2((float)hit.TargetX, (float)hit.TargetY), Life = .20f, Weapon = hit.Weapon });
                }
                DrawNewBranches();
            }
            ObserveUpgrades();
            if (Run.State != RunStatus.Running) CompleteRun();
            else
            {
                checkpointClock += Mathf.Min(Time.unscaledDeltaTime, .25f);
                if (checkpointClock >= 10f) SaveProgress();
            }
        }
        void CompleteRun()
        {
            if (endHandled) return;
            endHandled = true;
            SaveProgress();
            notice = Run.State == RunStatus.CapacityExceeded ? "試作版の処理上限。報酬は付与されません。" : "結晶で永続強化して、次の周回へ。";
        }
        void OnApplicationFocus(bool focused) { if (!focused && progressLoaded && !loadBlocked && !AwaitingStart) { IsPaused = true; SaveProgress(); } }
        void OnApplicationPause(bool paused) { if (paused && progressLoaded && !loadBlocked && !AwaitingStart) { IsPaused = true; SaveProgress(); } }
        void OnApplicationQuit() { if (progressLoaded && !loadBlocked && !AwaitingStart) SaveProgress(); }
        void ObserveUpgrades()
        {
            int[] levels = Run.UpgradeLevels;
            for (int i = 0; i < levels.Length; i++)
            {
                if (levels[i] > observedLevels[i])
                {
                    levelChangedAt[i] = Time.unscaledTime;
                    notice = (SelectedBuild == BuildPreset.Manual ? "強化: " : "自動購入: ") + UpgradeNames[i] + " Lv." + levels[i];
                }
                observedLevels[i] = levels[i];
            }
        }
        void DrawNewBranches()
        {
            var branches = Run.Branches;
            bool changed = drawnBranches < branches.Count;
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
            if (changed) RebuildBranchOverlay();
        }
        void RebuildBranchOverlay()
        {
            // Game-only adapter: same optical density as RenderRgba, without its opaque paper.
            // Called only when branches change, never from OnGUI or a per-frame pixel loop.
            int width = canvas.Core.Paper.Width, height = canvas.Core.Paper.Height;
            if (branchOverlay == null)
            {
                branchOverlay = new Texture2D(width, height, TextureFormat.RGBA32, false, false)
                { name = "Bokumyaku transparent ink", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
                branchPixels = new Color32[width * height];
            }
            Color32 color = canvas.inkColor;
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
            {
                float alpha = 1f - Mathf.Exp(-canvas.displayDensity * canvas.Core.Paper.PigmentAt(x, y));
                branchPixels[y * width + x] = new Color32(color.r, color.g, color.b, (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha) * 255f));
            }
            branchOverlay.SetPixels32(branchPixels); branchOverlay.Apply(false, false);
            BranchOverlayRebuilds++;
        }
        void InitStyles()
        {
            if (body != null) return;
            EnsureInkMotifs();
            body = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true, alignment = TextAnchor.MiddleLeft };
            japaneseFont = Font.CreateDynamicFontFromOSFont(new[] { "Yu Gothic", "Meiryo", "Noto Sans CJK JP", "Arial" }, 16);
            if (japaneseFont != null) body.font = japaneseFont;
            SetLabelColor(body, ink);
            small = new GUIStyle(body) { fontSize = 13 }; SetLabelColor(small, muted);
            heading = new GUIStyle(body) { fontSize = 19, fontStyle = FontStyle.Bold };
            title = new GUIStyle(body) { fontSize = 32, fontStyle = FontStyle.Bold };
            number = new GUIStyle(body) { fontSize = 26, fontStyle = FontStyle.Bold };
            button = new GUIStyle(GUI.skin.button) { fontSize = 14, alignment = TextAnchor.MiddleCenter };
            button.font = body.font;
            button.normal.background = Texture2D.whiteTexture;
            button.hover.background = Texture2D.whiteTexture;
            button.active.background = Texture2D.whiteTexture;
            button.normal.textColor = ink; button.hover.textColor = ink; button.active.textColor = ink;
            iconLabel = new GUIStyle(body) { alignment = TextAnchor.MiddleCenter };
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
                if (portrait)
                {
                    Rect safe = Screen.safeArea;
                    float portraitScale = safe.width / 440f;
                    GUI.matrix = Matrix4x4.TRS(new Vector3(safe.x, Screen.height - safe.yMax, 0), Quaternion.identity, Vector3.one * portraitScale);
                    DrawPortrait(safe.height / portraitScale);
                    return;
                }
                Fill(new Rect(0, 0, width, height), paper);
                DrawArt(new Rect(0, 0, width, height), paperArt, .45f, ScaleMode.ScaleAndCrop);
                GUI.Label(new Rect(24, 14, 300, 43), "墨 脈", title);
                GUI.Label(new Rect(25, 55, 330, 20), "B O K U M Y A K U   /   墨が生きる防衛", small);
                Fill(new Rect(24, 85, width - 48, 1), new Color(.75f, .72f, .64f));
                Rect field = portrait ? new Rect(52, 111, 336, 448) : new Rect(55, 116, 432, 576);
                if (Page == GamePage.Home) DrawHome(field); else DrawField(field);
                if (portrait) DrawPanel(new Rect(24, 584, 392, 452), true);
                else DrawPanel(new Rect(558, 110, 494, 616), false);
                GUI.Label(new Rect(field.x, field.yMax + 7, field.width, 18), "墨の枝  ／  金の末端  ／  朱の敵", small);
                GUI.Label(new Rect(width - 185, portrait ? 20 : 42, 162, 25), "システム α  0.12", small);
            }
            finally { GUI.matrix = oldMatrix; GUI.color = oldColor; GUI.backgroundColor = oldBackground; GUI.enabled = oldEnabled; }
        }
        Vector2 View(Rect field, double x, double y) { return new Vector2(field.x + (float)x / 384f * field.width, field.yMax - (float)y / 512f * field.height); }
        Vector2 Logical(Rect field, Vector2 point) { return View(field, 192 + point.x * .48, 256 + point.y * .48); }
        void DrawHome(Rect field)
        {
            Fill(field, paper); DrawArt(field, landscapeArt, .6f, ScaleMode.ScaleAndCrop);
            DrawArt(new Rect(field.center.x - 116, field.y + 20, 232, 232), ensoArt, .25f, ScaleMode.ScaleToFit);
            GUI.Label(new Rect(field.x + 20, field.y + 66, field.width - 40, 45), "一滴から、再び。", title);
            GUI.Label(new Rect(field.x + 20, field.y + 119, field.width - 40, 60), "最高到達 " + BestWave + " 波\n累計 " + (campaign == null ? 0 : campaign.TotalRuns) + " 周  ／  " + (campaign == null ? 0 : campaign.TotalCrystals) + " 結晶", body);
            GUI.Label(new Rect(field.x + 20, field.y + 187, field.width - 40, 45), notice, small);
            bool enabled = GUI.enabled;
            GUI.enabled = !loadBlocked && !storageFailed;
            if (Button(new Rect(field.x + 20, field.y + 245, field.width - 40, 38), ResumeAvailable ? "途中から続行" : "出撃する", true))
            {
                if (ResumeAvailable) ResumeRun(); else TryStartRun(Run == null || AwaitingStart ? InitialSeed : unchecked(Run.Seed + 1));
            }
            if (ResumeAvailable)
            {
                if (Button(new Rect(field.x + 20, field.y + 292, field.width - 40, 34), abandonConfirmation ? "報酬なしで終了する（確定）" : "途中終了・帰還", false))
                {
                    if (abandonConfirmation) AbandonRun(); else abandonConfirmation = true;
                }
                GUI.Label(new Rect(field.x + 20, field.y + 333, field.width - 40, 40), "途中状態は約10秒ごとに保存。\n読込後は手動で続行するまで停止。", small);
            }
            else GUI.Label(new Rect(field.x + 20, field.y + 297, field.width - 40, 60), "六つの武器と投資方針を選ぶ。\n研究は周回の合間、記録はいつでも。", body);
            GUI.enabled = enabled;
        }
        void DrawField(Rect field, bool minimalHud = false)
        {
            Fill(field, paper);
            DrawArt(field, paperArt, 1f, ScaleMode.ScaleAndCrop);
            DrawArt(field, landscapeArt, .62f, ScaleMode.ScaleAndCrop);
            GUI.DrawTexture(field, branchOverlay != null ? branchOverlay : canvas.Texture, ScaleMode.StretchToFill);
            var center = View(field, 192, 256);
            Ring(center, field.width * .45f, new Color(.58f, .53f, .42f, .17f));
            Ring(center, field.width * .25f, new Color(.58f, .53f, .42f, .10f));
            // Read the simulation's active fields; visual rings never create gameplay damage.
            foreach (var area in Run.Fields)
            {
                var position = Logical(field, new Vector2((float)area.X, (float)area.Y));
                float radius = (float)area.Radius * .48f * field.width / 384f;
                bool bleed = area.Weapon == WeaponKind.Bleed;
                Color tint = bleed ? ink : gold;
                Dot(position, radius, new Color(tint.r, tint.g, tint.b, bleed ? .16f : .09f));
                Ring(position, radius, new Color(tint.r, tint.g, tint.b, .35f));
                if (bleed) Ring(position, radius * .78f, new Color(tint.r, tint.g, tint.b, .12f));
            }
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
                float radius = e.Kind == EnemyKind.Boss ? 11 : e.Kind == EnemyKind.Heavy ? 8 : e.Kind == EnemyKind.Fast ? 4 : 6;
                DrawInkEnemy(e.Kind, point, radius);
                if (e.Hp < e.MaxHp) { Fill(new Rect(point.x - 9, point.y - radius - 6, 18, 2), new Color(0, 0, 0, .12f)); Fill(new Rect(point.x - 9, point.y - radius - 6, 18 * Mathf.Clamp01((float)(e.Hp / e.MaxHp)), 2), red); }
            }
            foreach (var flash in flashes)
            {
                float alpha = Mathf.Sqrt(Mathf.Clamp01(flash.Life / .20f));
                Vector2 from = Logical(field, flash.A), to = Logical(field, flash.B);
                if (flash.Weapon == WeaponKind.Pierce)
                {
                    // A broad tapered stroke distinguishes the slower piercing brush.
                    BrushBeam(from, to, 60, alpha);

                }
                else if (flash.Weapon == WeaponKind.Point)
                {
                    BrushBeam(from, to, 30, alpha);
                    Dot(to, 3 + 3 * alpha, new Color(ink.r, ink.g, ink.b, alpha));
                    Ring(to, 3 + 8 * (1 - alpha), new Color(gold.r, gold.g, gold.b, alpha));
                }
                else if (flash.Weapon == WeaponKind.Sweep)
                {
                    Vector2 direction = (to - from).normalized;
                    for (int i = -2; i <= 2; i++)
                    {
                        float angle = i * .18f;
                        Vector2 turned = new Vector2(direction.x * Mathf.Cos(angle) - direction.y * Mathf.Sin(angle), direction.x * Mathf.Sin(angle) + direction.y * Mathf.Cos(angle));
                        BrushBeam(from, from + turned * Vector2.Distance(from, to), 38, alpha * .8f);
                    }
                }
                else if (flash.Weapon == WeaponKind.Enso)
                    InkStamp(coreInk, from, 28 + 65 * (1-alpha), new Color(ink.r, ink.g, ink.b, alpha));
                else if (flash.Weapon == WeaponKind.Resonance)
                {
                    BrushBeam(from, to, 38, alpha);
                    Ring(to, 5 + 8 * (1 - alpha), new Color(ink.r, ink.g, ink.b, alpha));
                }
                else InkStamp(burstInk, to, 26 + 16 * (1-alpha), new Color(ink.r, ink.g, ink.b, alpha * .8f));
            }
            DrawInkBursts(field);
            DrawInkCore(center);
            double minPressure = double.MaxValue, maxPressure = 0;
            foreach (var branch in Run.Branches)
            {
                if (!branch.IsTip) continue;
                double pressure = Run.Pressure(branch);
                minPressure = Math.Min(minPressure, pressure); maxPressure = Math.Max(maxPressure, pressure);
            }
            if (!minimalHud)
            {
                GUI.Label(new Rect(field.x + 12, field.yMax - 30, field.width - 24, 22),
                    "流墨 Q " + Run.RootSupply.ToString("0.0") + "  ／  末端墨圧 P " + minPressure.ToString("0.00") + "–" + maxPressure.ToString("0.00"), small);
                GUI.Label(new Rect(field.x + 12, field.y + 8, field.width - 24, 25), "波 " + Run.Wave.ToString("00") + "  /  " + Run.RunWaveLimit, heading);
                GUI.Label(new Rect(field.x + 12, field.y + 35, field.width - 24, 22), "核 " + Run.CoreHp + "/10 · " + Run.TipCount + " 末端 · 規則" + Run.RulesVersion + (Run.RulesVersion == 1 ? "（旧）" : ""), small);
                Fill(new Rect(field.x + 12, field.y + 60, 100, 3), new Color(0, 0, 0, .10f));
                Fill(new Rect(field.x + 12, field.y + 60, 10 * Run.CoreHp, 3), gold);
            }
            if (AwaitingStart || Run.State != RunStatus.Running)
            {
                var box = new Rect(field.x + 22, field.center.y - 126, field.width - 44, 252);
                Fill(box, new Color(.96f, .95f, .91f, .96f));
                DrawArt(new Rect(box.center.x - 110, box.center.y - 110, 220, 220), ensoArt, .10f, ScaleMode.ScaleToFit);
                string status = AwaitingStart ? "一滴から、命が芽吹く。" : Run.State == RunStatus.Defeated ? "墨が静まり、核が砕けた。" : Run.State == RunStatus.TestCompleted ? Run.RunWaveLimit + "の波を、耐え抜いた。" : "試作版の処理上限";
                int milestone = rewarded && campaign != null && campaign.LastResult != null ? campaign.LastResult.MilestoneCrystals : 0;
                GUI.Label(new Rect(box.x + 14, box.y + 12, box.width - 28, 35), status, heading);
                GUI.Label(new Rect(box.x + 14, box.y + 48, box.width - 28, 146), AwaitingStart
                    ? "核を守る墨の枝。敵を倒すと枝が伸びる。\n操作欄で武器と投資方針を選び、出撃する。\n武器: " + WeaponNames[(int)SelectedWeapon] + "\n投資: " + BuildNames[(int)SelectedBuild]
                    : "到達 " + Run.CompletedWave + " 波突破  ·  " + Run.Kills + " 撃破\n戦闘 " + FormatTime(Run.ElapsedSeconds)
                        + "  ／  獲得墨 " + Run.IncomeEarned + "\n" + ResultPresentation.InvasionSummary(Run.LeakedByType)
                        + "\n結晶 " + Run.CrystalsReward + " + 初達成 " + milestone + (rewarded ? "（保存済）" : "（保存待ち）")
                        + "\n武器: " + WeaponNames[(int)Run.Weapon] + " / 通常結晶/分 " + ResultPresentation.NormalCrystalsPerMinute(Run.CrystalsReward, Run.ElapsedSeconds).ToString("0.00"), body);
                if (Button(new Rect(box.x + 14, box.y + 206, box.width - 28, 32), AwaitingStart ? "出撃する" : "再び芽吹く", true)) StartRun(unchecked(InitialSeed + (AwaitingStart ? 0 : Run.Seed - InitialSeed + 1)));
            }
        }
        void DrawPanel(Rect panel, bool compact)
        {
            float x = panel.x, y = panel.y, w = panel.width;
            float tabWidth = (w - 18) / 4;
            string[] pages = { "ホーム", "戦闘", "研究", "記録" };
            for (int i = 0; i < 4; i++)
                if (Button(new Rect(x + i * (tabWidth + 6), y, tabWidth, 29), pages[i], Page == (GamePage)i)) SetPage((GamePage)i);
            y += 36;
            GUI.Label(new Rect(x, y, w * .48f, 17), "墨  ／  周回内", small);
            GUI.Label(new Rect(x, y + 16, w * .48f, 33), Run.RunInk.ToString(), number);
            GUI.Label(new Rect(x + w * .50f, y, w * .5f, 17), "結晶  ／  永続強化", small);
            GUI.Label(new Rect(x + w * .50f, y + 16, w * .5f, 33), Meta.Crystals.ToString(), number);
            y += 54;
            float bw = (w - 16) / 3;
            if (Button(new Rect(x, y, bw, 29), IsPaused ? "再開" : "一時停止", IsPaused)) SetPaused(!IsPaused);
            if (Button(new Rect(x + bw + 8, y, bw, 29), Speed == 1 ? "速度  1倍" : "速度  2倍", Speed == 2)) SetSpeed(Speed == 1 ? 2 : 1);
            if (Button(new Rect(x + (bw + 8) * 2, y, bw, 29), "途中保存", false)) SaveProgress();
            y += 38;
            if (Button(new Rect(x, y, 144, 30), AutoRepeatEnabled ? "自動周回  ON" : "自動周回  OFF", AutoRepeatEnabled)) SetAutoRepeat(!AutoRepeatEnabled);
            string repeatStatus = AutoRepeatPending
                ? "再出撃 " + AutoRepeatSeconds.ToString("0.0") + "秒" + (IsPaused ? " / 一時停止" : "")
                : !string.IsNullOrEmpty(saveWarning) ? "保存待ち / 再出撃しません"
                : AutoRepeatEnabled ? "敗北・保存後に再出撃" : "次の出撃は手動";
            GUI.Label(new Rect(x + 152, y, w - 152, 30), repeatStatus, small);
            GUI.Label(new Rect(x, y + 32, w, 24), "完了 " + CompletedRuns + " 周  ／  獲得 " + SessionCrystals + " 結晶  (この起動中)", small);
            y += 62;
            float footerHeight = string.IsNullOrEmpty(saveWarning) ? 29 : 48;
            Rect viewport = new Rect(x, y, w, panel.yMax - y - footerHeight);
            float contentWidth = w - 20;
            // The battlefield and pause/speed controls stay visible while the upgrade list scrolls.
            float contentHeight = Page == GamePage.Research ? 348 : Page == GamePage.Records ? 180 + (campaign == null ? 0 : campaign.History.Count) * 62 : 1182;
            panelScroll = GUI.BeginScrollView(viewport, panelScroll, new Rect(0, 0, contentWidth, contentHeight), false, true);
            try { if (Page == GamePage.Records) DrawRecords(contentWidth); else DrawUpgradeContent(contentWidth); }
            finally { GUI.EndScrollView(); }
            if (!string.IsNullOrEmpty(saveWarning))
            {
                GUI.Label(new Rect(x, panel.yMax - 46, w - 105, 44), saveWarning, small);
                if (Button(new Rect(x + w - 100, panel.yMax - 41, 100, 30), loadBlocked ? "読込再試行" : "保存再試行", true)) { if (loadBlocked) TryReloadProgress(); else SaveProgress(); }
            }
            else
                GUI.Label(new Rect(x, panel.yMax - 25, w, 24), FormatTime(Run.ElapsedSeconds) + " 生存  /  墨 " + Run.IncomePerMinute.ToString("0.0") + "/分  /  " + Run.Kills + " 撃破", small);
        }
        void DrawUpgradeContent(float w)
        {
            float y = 0, bw = (w - 16) / 3;
            if (Page == GamePage.Research)
            {
                GUI.Label(new Rect(0, y, w, 27), "命をつなぐ研究", heading); y += 32;
                GUI.Label(new Rect(0, y, w, 39), "周回の合間に購入。次の出撃から永続的に働く。", small); y += 43;
                for (int i = 0; i < 4; i++)
                {
                    var kind = (MetaUpgrade)i;
                    GUI.Label(new Rect(0, y, w - 110, 23), MetaNames[i] + "  Lv." + Meta.Level(kind) + "/" + MetaProgress.MaxLevel(kind), body);
                    GUI.Label(new Rect(0, y + 24, w - 110, 24), kind == MetaUpgrade.Damage ? "攻撃×" + Meta.DamageMultiplier.ToString("0.00") + (Meta.Damage < MetaProgress.MaxLevel(kind) ? " → ×" + new MetaProgress { Damage = Meta.Damage + 1 }.DamageMultiplier.ToString("0.00") : "（MAX）") : MetaNotes[i], small);
                    GUI.enabled = !loadBlocked && !storageFailed && campaign != null && !campaign.HasActiveRun && Meta.Crystals >= Meta.Cost(kind) && Meta.Level(kind) < MetaProgress.MaxLevel(kind);
                    if (Button(new Rect(w - 104, y + 2, 104, 44), Meta.Level(kind) >= MetaProgress.MaxLevel(kind) ? "MAX" : Meta.Cost(kind) + " 結晶", GUI.enabled)) BuyMeta(kind);
                    GUI.enabled = true; y += 56;
                }
            }
            else
            {
                GUI.Label(new Rect(0, y, w, 26), "武器  ／  投資方針とは独立", heading); y += 30;
                for (int i = 0; i < 6; i++)
                    if (WeaponButton(new Rect((bw + 8) * (i % 3), y + 78 * (i / 3), bw, 74), i)) SetWeapon((WeaponKind)i);
                y += 156;
                GUI.Label(new Rect(0, y, w, 42), WeaponNotes[(int)SelectedWeapon], small); y += 44;
                GUI.Label(new Rect(0, y, w, 26), "墨の投資方針", heading); y += 30;
                for (int i = 0; i < 3; i++)
                    if (Button(new Rect((bw + 8) * i, y, bw, 30), BuildNames[i], SelectedBuild == (BuildPreset)i)) SetBuild((BuildPreset)i);
                y += 34;
                GUI.Label(new Rect(0, y, w, 42), BuildDescription(SelectedBuild), small); y += 44;
                GUI.Label(new Rect(0, y, w, 23), "枝の成長方針", body); y += 26;
                var names = new[] { "均衡", "迎撃", "集中" };
                for (int i = 0; i < 3; i++) if (Button(new Rect((bw + 8) * i, y, bw, 28), names[i], SelectedPolicy == (GrowthPolicy)i)) SetPolicy((GrowthPolicy)i);
                y += 34;
                GUI.enabled = !loadBlocked && !storageFailed && !AwaitingStart && Run.State == RunStatus.Running && Run.CoreHp < 10 && Run.RepairCount < 5 && Run.RunInk >= Run.RepairCost;
                if (Button(new Rect(0, y, w, 32), "核を修復 +2  ／  " + Run.RepairCost + " 墨  ／  " + Run.RepairCount + "/5回", GUI.enabled)) TryRepair();
                GUI.enabled = true; y += 40;
                GUI.Label(new Rect(0, y, w, 23), Run.RulesVersion == 1 ? "旧ルールで続行 ／ 採墨は次の出撃から" : SelectedBuild == BuildPreset.Manual ? "手動購入  ／  七つの強化" : AwaitingStart || Run.State != RunStatus.Running ? "次の出撃で自動購入 ／ 手動購入も可能" : "自動購入中 ／ 貯蓄して順番に購入", small); y += 26;
                int[] levels = Run.UpgradeLevels;
                for (int i = 0; i < UpgradeNames.Length; i++)
                {
                    var kind = (UpgradeKind)i;
                    bool supported = i < levels.Length;
                    int level = supported ? levels[i] : 0;
                    int cap = supported ? Run.UpgradeMaxLevel(kind) : 0;
                    int cost = supported ? Run.UpgradeCost(kind) : 0;
                    bool changed = supported && levelChangedAt[i] > 0 && Time.unscaledTime - levelChangedAt[i] < 2;
                    if (changed) Fill(new Rect(0, y, w, 49), new Color(gold.r, gold.g, gold.b, .13f));
                    GUI.Label(new Rect(4, y, w - 112, 24), UpgradeNames[i] + (supported ? "  Lv." + level + "/" + cap : "  旧規則対象外") + (changed ? "  ↑" : ""), body);
                    string detail = kind == UpgradeKind.Harvest && supported
                        ? "以後の結晶×" + Run.HarvestMultiplier.ToString("0.00") + "・分裂子除外"
                        : supported ? UpgradeNotes[i] : "次の新規出撃から利用できます";
                    GUI.Label(new Rect(4, y + 24, w - 112, 24), detail, small);
                    GUI.enabled = supported && !loadBlocked && !storageFailed && !AwaitingStart && Run.State == RunStatus.Running && Run.RunInk >= cost && level < cap;
                    if (Button(new Rect(w - 104, y + 7, 104, 32), !supported ? "旧規則" : level >= cap ? "MAX" : cost + " 墨", GUI.enabled)) BuyUpgrade(kind);
                    GUI.enabled = true; y += 52;
                }
                y += 12; GUI.Label(new Rect(0, y, w, 27), "構成を保存・呼出", heading); y += 32;
                for (int i = 0; i < 3; i++)
                {
                    var slot = campaign == null ? null : campaign.Slots[i];
                    GUI.Label(new Rect(0, y, w - 176, 49), "構成" + (i + 1) + (slot == null ? "" : "  " + WeaponNames[(int)slot.Weapon] + "\n" + BuildNames[(int)slot.Build]), small);
                    GUI.enabled = !loadBlocked && !storageFailed && campaign != null;
                    if (Button(new Rect(w - 170, y + 7, 80, 32), "保存", false)) SaveBuildSlot(i);
                    if (Button(new Rect(w - 84, y + 7, 80, 32), "呼出", false)) LoadBuildSlot(i);
                    GUI.enabled = true; y += 54;
                }
            }
            GUI.Label(new Rect(0, y + 4, w, 43), notice, small);
        }
        void DrawRecords(float width)
        {
            GUI.Label(new Rect(0, 0, width, 29), "これまでの墨脈", heading);
            GUI.Label(new Rect(0, 34, width, 62), "最高到達 " + BestWave + " 波\n累計 " + (campaign == null ? 0 : campaign.TotalRuns) + " 周 / " + (campaign == null ? 0 : campaign.TotalCrystals) + " 結晶", body);
            GUI.Label(new Rect(0, 100, width, 48), "敵: 通常＝丸 / 速い＝尾 / 重い＝輪\n分裂＝双点 / ボス＝八つの棘", small);
            if (campaign == null || campaign.History.Count == 0) { GUI.Label(new Rect(0, 155, width, 35), "記録はまだありません。", body); return; }
            float y = 154;
            for (int i = campaign.History.Count - 1; i >= 0; i--)
            {
                var result = campaign.History[i];
                    GUI.Label(new Rect(0, y, width, 52), result.CompletedWave + " 波 / " + WeaponNames[(int)result.Weapon] + " / " + FormatTime(result.Ticks / 30.0) + " / 規則" + result.RulesVersion
                    + "\n結晶 " + result.Crystals + " + 初達成 " + result.MilestoneCrystals + "  / Seed " + result.Seed, small);
                y += 62;
            }
        }
        static string FormatTime(double seconds)
        {
            long whole = (long)Math.Max(0, seconds);
            return (whole / 60).ToString("00") + ":" + (whole % 60).ToString("00");
        }
        static void SetLabelColor(GUIStyle style, Color color)
        {
            // Dark Editor skin hover colors must not turn paper-screen labels white.
            foreach (var state in new[] { style.normal, style.hover, style.active, style.focused,
                style.onNormal, style.onHover, style.onActive, style.onFocused }) state.textColor = color;
        }
        bool Button(Rect rect, string label, bool selected)
        {
            Color before = GUI.backgroundColor;
            GUI.backgroundColor = selected ? new Color(.82f, .72f, .51f) : new Color(.89f, .88f, .83f);
            bool clicked = GUI.Button(rect, label, button);
            GUI.backgroundColor = before;
            return clicked;
        }
        bool WeaponButton(Rect rect, int index)
        {
            bool clicked = Button(rect, "", SelectedWeapon == (WeaponKind)index);
            // Preserve the supplied icon's aspect and transparent margins; no source image edits.
            if (weaponArt[index] != null)
                DrawArt(new Rect(rect.x + 8, rect.y + 4, rect.width - 16, 44), weaponArt[index], 1f, ScaleMode.ScaleToFit);
            else
            {
                Vector2 center = new Vector2(rect.center.x, rect.y + 25);
                if (index == 0) Dot(center, 5, ink);
                else if (index == 1) Line(center + new Vector2(-14, 6), center + new Vector2(14, -6), 4, ink);
                else if (index == 2) { Dot(center, 12, new Color(ink.r, ink.g, ink.b, .3f)); Ring(center, 12, ink); }
                else if (index == 3) { Line(center + new Vector2(-15, 8), center + new Vector2(15, -5), 6, ink); Line(center + new Vector2(-15, 8), center + new Vector2(12, -12), 2, ink); }
                else if (index == 4) Ring(center, 14, ink);
                else { Dot(center, 4, ink); Dot(center + new Vector2(-14, 6), 3, gold); Dot(center + new Vector2(14, -6), 3, gold); Line(center + new Vector2(-14, 6), center + new Vector2(14, -6), 1, ink); }
            }
            GUI.Label(new Rect(rect.x + 4, rect.yMax - 25, rect.width - 8, 22), WeaponNames[index], iconLabel);
            return clicked;
        }
        void DrawArt(Rect rect, Texture2D texture, float opacity, ScaleMode mode)
        {
            if (texture == null) return;
            Color previous = GUI.color;
            GUI.color = new Color(1, 1, 1, opacity);
            GUI.DrawTexture(rect, texture, mode, true);
            GUI.color = previous;
        }
        void Fill(Rect rect, Color color) { var old = GUI.color; GUI.color = color; GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = old; }
        void Dot(Vector2 center, float radius, Color color) { var old = GUI.color; GUI.color = color; GUI.DrawTexture(new Rect(center.x - radius, center.y - radius, radius * 2, radius * 2), disc); GUI.color = old; }
        // Compose in local GUI coordinates so portrait scaling and field groups share the same pivot.
        static void RotateLocal(float angle, Vector2 pivot)
        {
            GUI.matrix = GUI.matrix * Matrix4x4.Translate(pivot) * Matrix4x4.Rotate(Quaternion.Euler(0, 0, angle)) * Matrix4x4.Translate(-pivot);
        }
        void Line(Vector2 a, Vector2 b, float width, Color color)
        {
            var matrix = GUI.matrix;
            RotateLocal(Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg, a);
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
        void OnDestroy()
        {
            ReleaseInkMotifs();
            autoRepeat.Cancel();
            if (disc != null) Destroy(disc);
            if (branchOverlay != null) Destroy(branchOverlay);
            if (japaneseFont != null) Destroy(japaneseFont);
        }
        sealed class Flash { public Vector2 A, B; public float Life; public WeaponKind Weapon; }
    }
    // Display-only calculations. Efficiency uses battle time, excluding restart waits and milestone bonuses.
    public static class ResultPresentation
    {
        public static double NormalCrystalsPerMinute(int normalCrystals, double battleSeconds)
        {
            return battleSeconds > 0 ? normalCrystals * 60.0 / battleSeconds : 0;
        }

        public static string InvasionSummary(int[] leakedByType)
        {
            string[] names = { "通常", "速敵", "重敵", "分裂", "ボス" };
            int total = 0, most = 0, mostIndex = 0;
            for (int i = 0; i < leakedByType.Length; i++)
            {
                int count = leakedByType[i];
                total += count;
                // Strict comparison keeps ties stable in EnemyKind order; this is not a cause-of-death claim.
                if (count > most) { most = count; mostIndex = i; }
            }
            if (total == 0) return "核への侵入なし";
            string kind = mostIndex < names.Length ? names[mostIndex] : "不明";
            return "侵入 " + total + "体 / 最多 " + kind + " " + most + "体";
        }
    }
}
