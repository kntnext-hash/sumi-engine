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
        public bool MenuLoadBlocked => loadBlocked;
        public string MenuSaveWarning => saveWarning;
        public void MenuClick() => AudioClick();
        public string SelectedBuildDescription => BuildDescription(SelectedBuild);
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
        public Texture2D BranchOverlay => persistentInk?.Texture;
        public event Action ProgressChanged;
        string saveWarning = "";
        bool progressLoaded;
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
        Texture2D paperArt, landscapeArt, portraitLandscapeArt, ensoArt;
        readonly Texture2D[] weaponArt = new Texture2D[6];
        readonly List<Flash> flashes = new List<Flash>();
        readonly Dictionary<int, float> growth = new Dictionary<int, float>();
        float tickClock;
        int drawnBranches;
        bool rewarded;
        bool endHandled, rewardSaved;
        bool loadBlocked, storageFailed;
        float checkpointClock;
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
            canvas.enabled = false; // Game uses dirty-tile SUMI surface; Brush Lab keeps its original adapter.
            canvas.paperColor = paper;
            canvas.inkColor = ink;
            canvas.diffusionFps = 0; // Persistent branches are fixed. Combat effects are transient overlays.
            canvas.uploadFps = 15;
            canvas.displayDensity = 7f;
            paperArt = LoadArt("paper_ivory_01");
            landscapeArt = LoadArt("landscape_mist_01");
            portraitLandscapeArt = LoadArt("landscape_portrait_quiet_02");
            ensoArt = LoadArt("enso_frame_01");
            weaponArt[0] = LoadArt("weapon_point_01");
            weaponArt[1] = LoadArt("weapon_pierce_01");
            weaponArt[2] = LoadArt("weapon_bleed_01");
            EnsureBattlePresentation();
        }
        Texture2D LoadArt(string name)
        {
            var texture = Resources.Load<Texture2D>("BokumyakuArt/" + name);
            if (texture != null) LoadedArtCount++;
            return texture;
        }
        public void StartRun(int seed) { TryStartRun(seed); }
        public bool TryStartRun(int seed) { return BeginRun(seed, Speed); }
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
            drawnBranches = 0; inkBursts.Clear(); watchedEnemies.Clear(); ObservedInkBursts = 0; flashes.Clear(); growth.Clear(); persistentInk.Clear();battleWorld.Reset(Run);
            Array.Clear(observedLevels, 0, observedLevels.Length);
            Array.Clear(levelChangedAt, 0, levelChangedAt.Length);
            DrawRootFlourishes();
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
            AwaitingStart = true; IsPaused = true; rewarded = endHandled = rewardSaved = false;
            Page = GamePage.Home; ResetInk(); notice = "途中の周回を終了しました。報酬はありません。"; return true;
        }
        public bool BuyUpgrade(UpgradeKind kind)
        {
            if (Run == null || AwaitingStart || loadBlocked || storageFailed) return false;
            bool bought = Run.TryBuy(kind);
            notice = bought ? UpgradeNames[(int)kind] + "を強化しました。" : "墨が不足、または強化上限に達しています。";
            if (bought) { ObserveUpgrades(); AudioPurchase(); }
            return bought;
        }
        public bool TryRepair()
        {
            if (Run == null || AwaitingStart || Run.State != RunStatus.Running || loadBlocked || storageFailed) return false;
            bool repaired = Run.TryRepair();
            notice = repaired ? "核を修復しました。" : "墨不足・修復上限・核の状態を確認してください。";
            if (repaired) { SaveProgress(); AudioPurchase(); }
            return repaired;
        }
        public bool BuyMeta(MetaUpgrade kind)
        {
            if (campaign == null || campaign.HasActiveRun || loadBlocked || storageFailed) return false;
            var proposed = CopyMeta(Meta);
            if (!proposed.TryBuy(kind)) { notice = "結晶が不足、または研究上限に達しています。"; return false; }
            try { ApplyCampaign(CampaignSaveStore.SaveMeta(SavePath, campaign, proposed)); }
            catch (Exception e) when (IsSaveException(e)) { SaveFailed(e); return false; }
            AudioResearch(); notice = "研究完了。次の周回から反映します。"; ProgressChanged?.Invoke(); return true;
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
            Page = page;
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
                battleWorld.BeforeTick(Run);
                Run.Tick();
                battleWorld.AfterTick(Run,ReducedMotion);
                if (Run.Attacks.Count > 0) AudioAttack();
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
                brush.radius = Mathf.Lerp(14f, 4.5f, Mathf.Clamp01((float)b.PathLength / 400f));
                brush.pigment = 1f; brush.water = .15f; brush.depletion = .002f;
                brush.roughness = .42f;
                float x0 = (float)parent.ViewX, y0 = (float)parent.ViewY;
                float x1 = (float)b.ViewX, y1 = (float)b.ViewY;
                float sx = canvas.width / 384f, sy = canvas.height / 512f;
                // A gently bowed centerline gives the growing branch a living brush gesture.
                var normal = new Vector2(-(y1-y0), x1-x0).normalized;
                for(int pass=0;pass<2;pass++)
                {
                if(pass==1){brush.texture=InkTexture.Pool;brush.radius*=.52f;brush.roughness=.3f;brush.water=.35f;}
                persistentInk.Begin(new InkPoint(x0 * sx, y0 * sy, 0, 1f), brush, unchecked(Run.Seed + b.Id * 7919));
                for (int n = 1; n <= 16; n++)
                {
                    float t = n / 16f, bow = Mathf.Sin(t * Mathf.PI) * (b.Id % 2 == 0 ? 9f : -9f);
                    persistentInk.Move(new InkPoint((Mathf.Lerp(x0, x1, t) + normal.x * bow) * sx,
                        (Mathf.Lerp(y0, y1, t) + normal.y * bow) * sy, t * .35f, Mathf.Lerp(1f, .45f, t)));
                }
                persistentInk.End();
                }
                DrawBranchFibers(x0,y0,x1,y1,sx,sy,b.Id);
                growth[b.Id] = Time.unscaledTime;
            }
            if (changed) persistentInk.Flush();
        }
        static string FormatTime(double seconds)
        {
            long whole = (long)Math.Max(0, seconds);
            return (whole / 60).ToString("00") + ":" + (whole % 60).ToString("00");
        }
        void OnDestroy()
        {
            ReleaseBattlePresentation();
            ReleaseAudio();
            ReleaseInkMotifs();
            autoRepeat.Cancel();
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



