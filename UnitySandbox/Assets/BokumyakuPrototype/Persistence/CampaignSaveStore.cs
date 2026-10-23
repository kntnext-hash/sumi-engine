using System;
using System.Collections.Generic;
using System.IO;
using Bokumyaku.Simulation;
using UnityEngine;

namespace Bokumyaku.Persistence
{
    /// <summary>Copy-on-write local transactions. Callers swap their reference only on success.</summary>
    public static class CampaignSaveStore
    {
        static readonly object Gate = new object();
        const int MaxFileBytes = 4 * 1024 * 1024;
        [Serializable] sealed class Header { public int SchemaVersion = 0; public string Config = null; }
        [Serializable] sealed class TierFields { public int SelectedTier = 0; public int[] BestWaveByTier = null; public int[] ClaimedMilestoneWaveByTier = null; public TierResult LastResult = null; public TierResult[] History = null; }
        [Serializable] sealed class TierResult { public int Tier = 0; }
        static void Require(bool ok, string message) { if (!ok) throw new InvalidDataException(message); }
        static bool Id(string value) { Guid parsed; return !string.IsNullOrEmpty(value) && Guid.TryParseExact(value, "N", out parsed); }
        static bool MetaValid(MetaProgress m) => m != null && m.Crystals >= 0 && m.Damage >= 0 && m.Damage <= MetaProgress.MaxLevel(MetaUpgrade.Damage) && m.Supply >= 0 && m.Supply <= MetaProgress.MaxLevel(MetaUpgrade.Supply) && m.Growth >= 0 && m.Growth <= MetaProgress.MaxLevel(MetaUpgrade.Growth) && m.Harvest >= 0 && m.Harvest <= MetaProgress.MaxLevel(MetaUpgrade.Harvest);
        static bool SameMeta(MetaProgress a, MetaProgress b) => MetaValid(a) && MetaValid(b) && a.Crystals == b.Crystals && a.Damage == b.Damage && a.Supply == b.Supply && a.Growth == b.Growth && a.Harvest == b.Harvest;
        static void SlotValid(BuildSlot s)
        {
            Require(s != null && s.Name != null && s.Name.Length <= 40 && (s.Speed == 1 || s.Speed == 2), "Invalid build slot.");
            Require(Enum.IsDefined(typeof(WeaponKind), s.Weapon) && Enum.IsDefined(typeof(BuildPreset), s.Build) && Enum.IsDefined(typeof(GrowthPolicy), s.Policy), "Invalid slot enum.");
        }
        static void ResultValid(CampaignResult r)
        {
            Require(r != null && r.RulesVersion >= 1 && r.RulesVersion <= RunSimulator.CurrentRulesVersion, "Unsupported result rules.");
            Require(r.Tier >= 1 && r.Tier <= TierBalance.Count && (r.RulesVersion >= 7 || r.Tier == 1), "Invalid result Tier.");
            int waveLimit = RunSimulator.WaveLimitForRules(r.RulesVersion), tickLimit = RunSimulator.TickLimitForRules(r.RulesVersion);
            Require(Id(r.RunId) && r.Ticks >= 0 && r.Ticks <= tickLimit && r.CompletedWave >= 0 && r.CompletedWave <= waveLimit && r.Crystals >= 0 && r.Crystals <= RunSimulator.CrystalLimitForRules(r.RulesVersion) && r.MilestoneCrystals >= 0 && r.MilestoneCrystals <= waveLimit, "Invalid result.");
            Require(r.Status == RunStatus.Defeated || r.Status == RunStatus.TestCompleted || r.Status == RunStatus.CapacityExceeded, "Result must be terminal.");
            Require(Enum.IsDefined(typeof(WeaponKind), r.Weapon) && Enum.IsDefined(typeof(BuildPreset), r.Build) && Enum.IsDefined(typeof(GrowthPolicy), r.Policy), "Invalid result configuration.");
        }
        public static void Validate(CampaignProgress c)
        {
            Require(c != null && c.SchemaVersion == 4 && c.Config == "bokumyaku-campaign-v4", "Unsupported campaign schema/config.");
            Require(c.Revision >= 0 && c.Revision < long.MaxValue && MetaValid(c.Meta), "Invalid campaign progress.");
            Require(c.ActiveSpeed == 1 || c.ActiveSpeed == 2, "Invalid active speed.");
            Require(c.BestWave >= 0 && c.BestWave <= RunSimulator.LongRunWaveLimit && c.TotalRuns >= 0 && c.TotalCrystals >= 0 && c.ClaimedMilestoneWave >= 0 && c.ClaimedMilestoneWave <= c.BestWave && c.ClaimedMilestoneWave % 5 == 0, "Invalid cumulative progress.");
            Require(c.BestWaveByTier != null && c.BestWaveByTier.Length == TierBalance.Count && c.ClaimedMilestoneWaveByTier != null && c.ClaimedMilestoneWaveByTier.Length == TierBalance.Count, "Invalid Tier progress arrays.");
            for (int i = 0; i < TierBalance.Count; i++)
            {
                Require(c.BestWaveByTier[i] >= 0 && c.BestWaveByTier[i] <= RunSimulator.LongRunWaveLimit && c.ClaimedMilestoneWaveByTier[i] >= 0 && c.ClaimedMilestoneWaveByTier[i] <= c.BestWaveByTier[i] && c.ClaimedMilestoneWaveByTier[i] % 5 == 0, "Invalid Tier progress.");
                Require(c.BestWaveByTier[i] == 0 || TierBalance.IsUnlocked(i + 1, c.BestWaveByTier), "Progress in locked Tier.");
            }
            Require(c.BestWave == c.BestWaveByTier[0] && c.ClaimedMilestoneWave == c.ClaimedMilestoneWaveByTier[0], "Tier 1 legacy mirrors differ.");
            Require(c.SelectedTier >= 1 && c.SelectedTier <= TierBalance.Count && TierBalance.IsUnlocked(c.SelectedTier, c.BestWaveByTier), "Selected Tier is locked or invalid.");
            Require(c.History != null && c.History.Count <= 20 && c.History.Count <= c.TotalRuns && c.Slots != null && c.Slots.Length == 3, "Invalid history/slots.");
            var ids = new HashSet<string>();
            long historyCrystals = 0;
            foreach (var r in c.History)
            {
                ResultValid(r); Require(TierBalance.IsUnlocked(r.Tier, c.BestWaveByTier), "History contains locked Tier."); Require(ids.Add(r.RunId), "Duplicate history run ID.");
                if (r.Status == RunStatus.CapacityExceeded) Require(r.Crystals == 0 && r.MilestoneCrystals == 0, "Invalid capacity reward.");
                else Require(c.BestWaveByTier[r.Tier - 1] >= r.CompletedWave, "Best wave below recorded result.");
                historyCrystals += (long)r.Crystals + r.MilestoneCrystals;
            }
            Require(historyCrystals <= c.TotalCrystals, "Cumulative rewards below history.");
            foreach (var s in c.Slots) SlotValid(s);
            if (c.TotalRuns > 0)
            {
                ResultValid(c.LastResult);
                Require(Id(c.LastSettledRunId) && c.LastResult.RunId == c.LastSettledRunId && c.History.Count > 0 && c.History[c.History.Count - 1].RunId == c.LastSettledRunId, "Inconsistent last result.");
                Require(JsonUtility.ToJson(c.LastResult) == JsonUtility.ToJson(c.History[c.History.Count - 1]), "Last result differs from history.");
            }
            else Require(string.IsNullOrEmpty(c.LastSettledRunId) && c.History.Count == 0, "Unexpected settled ID.");
            if (c.HasActiveRun)
            {
                Require(Id(c.ActiveRunId) && c.ActiveRunId != c.LastSettledRunId && !ids.Contains(c.ActiveRunId) && c.ActiveRun != null, "Invalid active run ID/snapshot.");
                Require(SameMeta(c.Meta, c.ActiveRun.meta) && !c.ActiveRun.rewardClaimed, "Active run meta differs or reward already claimed outside campaign.");
                try { var active = RunSimulator.Restore(c.ActiveRun); Require(TierBalance.IsUnlocked(active.Tier, c.BestWaveByTier) && active.Tier == c.SelectedTier, "Active Tier differs or is locked."); }
                catch (ArgumentException e) { throw new InvalidDataException("Invalid active run snapshot.", e); }
            }
            else Require(string.IsNullOrEmpty(c.ActiveRunId), "Inactive run has an ID.");
        }
        static CampaignProgress Clone(CampaignProgress c)
        {
            // Validate/decode the original payload before any additional JSON round trip.
            // Repeated JsonUtility parsing can otherwise accumulate adjacent-double rounding.
            Require(c != null, "Invalid campaign progress.");
            RunSnapshot active = null;
            try { if (c.HasActiveRun) active = RunSimulator.Restore(c.ActiveRun).CaptureState(); }
            catch (ArgumentException e) { throw new InvalidDataException("Invalid active run snapshot.", e); }
            var copy = JsonUtility.FromJson<CampaignProgress>(JsonUtility.ToJson(c));
            if (c.HasActiveRun) copy.ActiveRun = active;
            return copy;
        }
        static CampaignProgress Read(string path, bool previous = false, bool migration = false)
        {
            Require(new FileInfo(path).Length <= MaxFileBytes, "Campaign exceeds size limit.");
            CampaignProgress c;
            try
            {
                string json = File.ReadAllText(path);
                var header = JsonUtility.FromJson<Header>(json);
                Require(header != null && (previous ? header.SchemaVersion == 2 && header.Config == "bokumyaku-campaign-v2" : (header.SchemaVersion == 4 && header.Config == "bokumyaku-campaign-v4") || (migration && header.SchemaVersion == 3 && header.Config == "bokumyaku-campaign-v3")), "Missing or unsupported campaign header.");
                c = JsonUtility.FromJson<CampaignProgress>(json);
                if (header.SchemaVersion == 4)
                {
                    var fields = JsonUtility.FromJson<TierFields>(json);
                    Require(fields != null && fields.SelectedTier >= 1 && fields.BestWaveByTier != null && fields.ClaimedMilestoneWaveByTier != null && fields.History != null, "Missing Tier campaign fields.");
                    Require(fields.LastResult == null || fields.LastResult.Tier >= 1, "Missing result Tier.");
                    foreach (var result in fields.History) Require(result != null && result.Tier >= 1, "Missing history Tier.");
                }
                if (previous)
                {

                    if (c.HasActiveRun) Require(c.ActiveRun != null && c.ActiveRun.schemaVersion == 1 && c.ActiveRun.rulesVersion == 0 && c.ActiveRun.autoPurchaseCursor == 0 && c.ActiveRun.levels != null && c.ActiveRun.levels.Length == 6, "Previous campaign requires an original v5 snapshot.");
                    if (c.LastResult != null) { Require(c.LastResult.RulesVersion == 0 || c.LastResult.RulesVersion == 1, "Invalid previous result rules."); c.LastResult.RulesVersion = 1; }
                    if (c.History != null) foreach (var result in c.History) if (result != null) { Require(result.RulesVersion == 0 || result.RulesVersion == 1, "Invalid previous history rules."); result.RulesVersion = 1; }
                }
                if (header.SchemaVersion < 4)
                {
                    c.SchemaVersion = 4; c.Config = "bokumyaku-campaign-v4"; c.SelectedTier = 1;
                    c.BestWaveByTier = new[] { c.BestWave, 0, 0 };
                    c.ClaimedMilestoneWaveByTier = new[] { c.ClaimedMilestoneWave, 0, 0 };
                    if (c.LastResult != null) { Require(c.LastResult.RulesVersion <= 6, "Legacy result contains new Tier rules."); c.LastResult.Tier = 1; }
                    if (c.History != null) foreach (var result in c.History) if (result != null) { Require(result.RulesVersion <= 6, "Legacy history contains new Tier rules."); result.Tier = 1; }
                    if (c.HasActiveRun) Require(c.ActiveRun != null && c.ActiveRun.schemaVersion <= 2 && c.ActiveRun.rulesVersion <= 6, "Legacy campaign cannot contain new Tier rules.");
                }
            }
            catch (ArgumentException e) { throw new InvalidDataException("Malformed campaign JSON.", e); }
            Validate(c); return c;
        }
        static CampaignProgress ReadRecover(string path, out string warning, bool previous = false, bool migration = false)
        {
            warning = "";
            try { return Read(path, previous, migration); }
            catch (Exception e) when (e is IOException || e is InvalidDataException)
            {
                try { var c = Read(path + ".bak", previous, migration); warning = "Campaign recovered from backup; original preserved."; return c; }
                catch (Exception backup) when (backup is IOException || backup is InvalidDataException)
                { throw new InvalidDataException("Campaign and backup are unreadable; no data was replaced.", new AggregateException(e, backup)); }
            }
        }
        /// <summary>Generation 9 is the only writable Tier campaign; all previous files stay untouched.</summary>
        public static CampaignProgress LoadTierLatest(string path, string previousV8, string previousV7, string previousV6, string previousV5, string previousV4, string previousV3, string olderV2, string legacyMetaPath, out string warning)
        {
            lock (Gate)
            {
                foreach (var source in new[] { previousV8, previousV7, previousV6, previousV5, previousV4, previousV3, olderV2, legacyMetaPath })
                    if (!string.IsNullOrEmpty(source)) Require(!string.Equals(Path.GetFullPath(path), Path.GetFullPath(source), StringComparison.OrdinalIgnoreCase), "Migration destination must differ from every source.");
                if (File.Exists(path) || File.Exists(path + ".bak")) return ReadRecover(path, out warning);
                if (!string.IsNullOrEmpty(previousV8) && (File.Exists(previousV8) || File.Exists(previousV8 + ".bak")))
                {
                    var c = ReadRecover(previousV8, out warning, migration: true); Write(path, c);
                    warning = "Previous campaign migrated to Tier generation9; original and backup preserved. " + warning; return c;
                }
                return LoadLatest(path, previousV7, previousV6, previousV5, previousV4, previousV3, olderV2, legacyMetaPath, out warning);
            }
        }
        /// <summary>File generation 8 isolates research price generation 2; combat rules and schema remain unchanged.</summary>
        public static CampaignProgress LoadLatest(string path, string previousV7, string previousV6, string previousV5, string previousV4, string previousV3, string olderV2, string legacyMetaPath, out string warning)
        {
            lock (Gate)
            {
                foreach (var source in new[] { previousV7, previousV6, previousV5, previousV4, previousV3, olderV2, legacyMetaPath })
                    if (!string.IsNullOrEmpty(source)) Require(!string.Equals(Path.GetFullPath(path), Path.GetFullPath(source), StringComparison.OrdinalIgnoreCase), "Migration destination must differ from every source.");
                if (File.Exists(path) || File.Exists(path + ".bak")) return ReadRecover(path, out warning);
                if (!string.IsNullOrEmpty(previousV7) && (File.Exists(previousV7) || File.Exists(previousV7 + ".bak")))
                {
                    var c = ReadRecover(previousV7, out warning, migration: true);
                    Write(path, c);
                    warning = "Previous campaign copied to file generation8 without repricing existing progress; original and backup preserved. " + warning;
                    return c;
                }
                return LoadLatest(path, previousV6, previousV5, previousV4, previousV3, olderV2, legacyMetaPath, out warning);
            }
        }
        /// <summary>File generation 7 retains schema/config 3 and isolates rules6 progression.</summary>
        public static CampaignProgress LoadLatest(string path, string previousV6, string previousV5, string previousV4, string previousV3, string olderV2, string legacyMetaPath, out string warning)
        {
            lock (Gate)
            {
                foreach (var source in new[] { previousV6, previousV5, previousV4, previousV3, olderV2, legacyMetaPath })
                    if (!string.IsNullOrEmpty(source)) Require(!string.Equals(Path.GetFullPath(path), Path.GetFullPath(source), StringComparison.OrdinalIgnoreCase), "Migration destination must differ from every source.");
                if (File.Exists(path) || File.Exists(path + ".bak")) return ReadRecover(path, out warning);
                if (!string.IsNullOrEmpty(previousV6) && (File.Exists(previousV6) || File.Exists(previousV6 + ".bak")))
                {
                    var c = ReadRecover(previousV6, out warning, migration: true);
                    Write(path, c);
                    warning = "Previous schema3 campaign copied to file generation7; original and backup preserved. " + warning;
                    return c;
                }
                return LoadLatest(path, previousV5, previousV4, previousV3, olderV2, legacyMetaPath, out warning);
            }
        }
        /// <summary>File generation 6 retains schema/config 3 and isolates rules5 progress.</summary>
        public static CampaignProgress LoadLatest(string path, string previousV5, string previousV4, string previousV3, string olderV2, string legacyMetaPath, out string warning)
        {
            lock (Gate)
            {
                foreach (var source in new[] { previousV5, previousV4, previousV3, olderV2, legacyMetaPath })
                    if (!string.IsNullOrEmpty(source)) Require(!string.Equals(Path.GetFullPath(path), Path.GetFullPath(source), StringComparison.OrdinalIgnoreCase), "Migration destination must differ from every source.");
                if (File.Exists(path) || File.Exists(path + ".bak")) return ReadRecover(path, out warning);
                if (!string.IsNullOrEmpty(previousV5) && (File.Exists(previousV5) || File.Exists(previousV5 + ".bak")))
                {
                    var c = ReadRecover(previousV5, out warning, migration: true);
                    Write(path, c);
                    warning = "Previous schema3 campaign copied to file generation6; original and backup preserved. " + warning;
                    return c;
                }
                return LoadLatest(path, previousV4, previousV3, olderV2, legacyMetaPath, out warning);
            }
        }
        /// <summary>File generation 5 keeps schema/config 3 while isolating rules4 writes from older executables.</summary>
        public static CampaignProgress LoadLatest(string path, string previousV4, string previousV3, string olderV2, string legacyMetaPath, out string warning)
        {
            lock (Gate)
            {
                foreach (var source in new[] { previousV4, previousV3, olderV2, legacyMetaPath })
                    if (!string.IsNullOrEmpty(source)) Require(!string.Equals(Path.GetFullPath(path), Path.GetFullPath(source), StringComparison.OrdinalIgnoreCase), "Migration destination must differ from every source.");
                if (File.Exists(path) || File.Exists(path + ".bak")) return ReadRecover(path, out warning);
                if (!string.IsNullOrEmpty(previousV4) && (File.Exists(previousV4) || File.Exists(previousV4 + ".bak")))
                {
                    var c = ReadRecover(previousV4, out warning, migration: true);
                    Write(path, c);
                    warning = "Previous schema3 campaign copied to file generation5; original and backup preserved. " + warning;
                    return c;
                }
                return LoadLatest(path, previousV3, olderV2, legacyMetaPath, out warning);
            }
        }
        /// <summary>File generation 4 retains campaign schema/config 3. Previous files are read-only migration sources.</summary>
        public static CampaignProgress LoadLatest(string path, string previousCurrentPath, string olderCampaignPath, string legacyMetaPath, out string warning)
        {
            lock (Gate)
            {
                foreach (var source in new[] { previousCurrentPath, olderCampaignPath, legacyMetaPath })
                    if (!string.IsNullOrEmpty(source)) Require(!string.Equals(Path.GetFullPath(path), Path.GetFullPath(source), StringComparison.OrdinalIgnoreCase), "Migration destination must differ from every source.");
                if (File.Exists(path) || File.Exists(path + ".bak")) return ReadRecover(path, out warning);
                if (!string.IsNullOrEmpty(previousCurrentPath) && (File.Exists(previousCurrentPath) || File.Exists(previousCurrentPath + ".bak")))
                {
                    var c = ReadRecover(previousCurrentPath, out warning, migration: true);
                    Write(path, c);
                    warning = "Previous schema3 campaign copied to file generation4; original and backup preserved. " + warning;
                    return c;
                }
                return Load(path, olderCampaignPath, legacyMetaPath, out warning);
            }
        }
        public static CampaignProgress Load(string path, string previousCampaignPath, string legacyMetaPath, out string warning)
        {
            lock (Gate)
            {
                // An existing new save is authoritative, even when corrupt. Never roll progress back.
                if (File.Exists(path) || File.Exists(path + ".bak")) return ReadRecover(path, out warning);
                if (!string.IsNullOrEmpty(previousCampaignPath) && (File.Exists(previousCampaignPath) || File.Exists(previousCampaignPath + ".bak")))
                {
                    Require(!string.Equals(Path.GetFullPath(path), Path.GetFullPath(previousCampaignPath), StringComparison.OrdinalIgnoreCase), "Migration destination must differ from previous campaign.");
                    var c = ReadRecover(previousCampaignPath, out warning, true);
                    Write(path, c);
                    warning = "Previous campaign copied to v3; original and backup preserved. " + warning;
                    return c;
                }
                return Load(path, legacyMetaPath, out warning);
            }
        }
        public static CampaignProgress Load(string path, string legacyPath, out string warning)
        {
            lock (Gate)
            {
                if (File.Exists(path) || File.Exists(path + ".bak")) return ReadRecover(path, out warning);
                warning = ""; var c = new CampaignProgress();
                if (!string.IsNullOrEmpty(legacyPath) && (File.Exists(legacyPath) || File.Exists(legacyPath + ".bak")))
                {
                    Require(Path.GetFullPath(path) != Path.GetFullPath(legacyPath), "Migration destination must differ from legacy save.");
                    c.Meta = MetaSaveStore.LoadForMigration(legacyPath, out warning);
                    Write(path, c); warning = "Legacy progress copied to campaign; legacy original preserved. " + warning;
                }
                return c;
            }
        }
        public static CampaignProgress Load(string path, out string warning) => Load(path, null, out warning);
        static void Write(string path, CampaignProgress c)
        {
            Validate(c); string json = JsonUtility.ToJson(Clone(c), true);
            Require(System.Text.Encoding.UTF8.GetByteCount(json) <= MaxFileBytes, "Campaign exceeds size limit.");
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            string temp = path + ".tmp-" + Guid.NewGuid().ToString("N");
            try
            {
                File.WriteAllText(temp, json); Read(temp);
                if (File.Exists(path))
                {
                    bool valid = true;
                    try { Read(path); } catch (Exception e) when (e is IOException || e is InvalidDataException) { valid = false; }
                    if (valid) File.Replace(temp, path, path + ".bak");
                    else { File.Copy(path, path + ".corrupt-" + Guid.NewGuid().ToString("N")); File.Replace(temp, path, null); }
                }
                else File.Move(temp, path);
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }
        static CampaignProgress Current(string path, CampaignProgress expected)
        {
            Validate(expected);
            if (!File.Exists(path) && !File.Exists(path + ".bak")) { Require(expected.Revision == 0, "Saved campaign missing."); return expected; }
            string warning; var actual = ReadRecover(path, out warning);
            Require(actual.Revision == expected.Revision, "Campaign changed; reload before writing."); return actual;
        }
        static CampaignProgress Commit(string path, CampaignProgress current, CampaignProgress next)
        { next.Revision = checked(current.Revision + 1); Write(path, next); return next; }
        public static CampaignProgress Save(string path, CampaignProgress campaign)
        { lock (Gate) { var current = Current(path, campaign); return Commit(path, current, Clone(campaign)); } }
        public static CampaignProgress BeginRun(string path, CampaignProgress campaign, RunSimulator run, int speed = 1)
        {
            if (run == null || run.State != RunStatus.Running || run.TickCount != 0) throw new ArgumentException("BeginRun requires a fresh running simulation.");
            lock (Gate)
            {
                var current = Current(path, campaign); var next = Clone(current);
                Require(!current.HasActiveRun && TierBalance.IsUnlocked(run.Tier, current.BestWaveByTier), "Active run exists or Tier is locked.");
                next.SelectedTier = run.Tier;
                Require(SameMeta(current.Meta, run.CaptureState().meta), "Run must use current campaign meta.");
                next.HasActiveRun = true; next.ActiveRunId = Guid.NewGuid().ToString("N"); next.ActiveRun = run.CaptureState(); next.ActiveSpeed = speed;
                return Commit(path, current, next);
            }
        }
        static void MatchRun(CampaignProgress c, RunSimulator run)
        {
            Require(c.HasActiveRun && c.ActiveRun != null && run != null, "No active run to update.");
            var active = RunSimulator.Restore(c.ActiveRun);
            Require(active.RulesVersion == run.RulesVersion && active.Tier == run.Tier, "Active run rules or Tier changed.");
            Require(c.ActiveRun.seed == run.Seed && run.TickCount >= c.ActiveRun.tickCount, "Run identity or tick regressed.");
            Require(SameMeta(c.Meta, run.CaptureState().meta) && !run.CaptureState().rewardClaimed, "Run meta changed or reward already claimed.");
        }
        public static CampaignProgress SaveActive(string path, CampaignProgress campaign, RunSimulator run, int speed = 1)
        {
            lock (Gate) { var current = Current(path, campaign); MatchRun(current, run); var next = Clone(current); next.ActiveRun = run.CaptureState(); next.ActiveSpeed = speed; return Commit(path, current, next); }
        }
        public static CampaignProgress DiscardRun(string path, CampaignProgress campaign)
        {
            lock (Gate) { var current = Current(path, campaign); var next = Clone(current); next.HasActiveRun = false; next.ActiveRun = null; next.ActiveRunId = ""; return Commit(path, current, next); }
        }
        public static CampaignProgress SettleRun(string path, CampaignProgress campaign, string runId, RunSimulator run)
        {
            lock (Gate)
            {
                string warning;
                var disk = File.Exists(path) || File.Exists(path + ".bak") ? ReadRecover(path, out warning) : campaign;
                if (Id(runId) && disk.LastSettledRunId == runId) return Clone(disk);
                var current = Current(path, campaign); MatchRun(current, run);
                Require(current.ActiveRunId == runId && run.State != RunStatus.Running, "Wrong run ID or unfinished run.");
                var next = Clone(current);
                // Work on a restored copy: never consume the live run's claim flag on a failed write.
                var settled = RunSimulator.Restore(run.CaptureState());
                int baseReward = settled.State == RunStatus.CapacityExceeded ? 0 : settled.CrystalsReward;
                bool validResult = settled.State == RunStatus.Defeated || settled.State == RunStatus.TestCompleted;
                int milestone = validResult ? (settled.CompletedWave / 5 * 5) : 0;
                int bonus = Math.Max(0, milestone - current.ClaimedMilestoneWaveByTier[settled.Tier - 1]);
                next.Meta.Crystals = checked(next.Meta.Crystals + baseReward + bonus);
                if (validResult) next.BestWaveByTier[settled.Tier - 1] = Math.Max(next.BestWaveByTier[settled.Tier - 1], settled.CompletedWave);
                next.ClaimedMilestoneWaveByTier[settled.Tier - 1] = Math.Max(next.ClaimedMilestoneWaveByTier[settled.Tier - 1], milestone);
                next.BestWave = next.BestWaveByTier[0]; next.ClaimedMilestoneWave = next.ClaimedMilestoneWaveByTier[0];
                next.TotalRuns = checked(next.TotalRuns + 1); next.TotalCrystals = checked(next.TotalCrystals + baseReward + bonus);
                next.LastSettledRunId = runId;
                next.LastResult = new CampaignResult { RunId = runId, Tier = settled.Tier, RulesVersion = settled.RulesVersion, Seed = settled.Seed, CompletedWave = settled.CompletedWave, Ticks = settled.TickCount, Status = settled.State, Crystals = baseReward, MilestoneCrystals = bonus, Weapon = settled.Weapon, Build = settled.Build, Policy = settled.Policy };
                next.History.Add(next.LastResult); if (next.History.Count > 20) next.History.RemoveAt(0);
                next.HasActiveRun = false; next.ActiveRun = null; next.ActiveRunId = "";
                return Commit(path, current, next);
            }
        }
        public static CampaignProgress SelectTier(string path, CampaignProgress campaign, int tier)
        {
            lock (Gate)
            {
                var current = Current(path, campaign);
                Require(!current.HasActiveRun && tier >= 1 && tier <= TierBalance.Count && TierBalance.IsUnlocked(tier, current.BestWaveByTier), "Cannot select locked Tier or change Tier during a run.");
                var next = Clone(current); next.SelectedTier = tier; return Commit(path, current, next);
            }
        }
        public static CampaignProgress SaveMeta(string path, CampaignProgress campaign, MetaProgress meta)
        {
            Require(MetaValid(meta), "Invalid meta progress.");
            lock (Gate) { var current = Current(path, campaign); Require(!current.HasActiveRun, "Cannot change meta during an active run."); var next = Clone(current); next.Meta = JsonUtility.FromJson<MetaProgress>(JsonUtility.ToJson(meta)); return Commit(path, current, next); }
        }
        public static CampaignProgress SaveSlot(string path, CampaignProgress campaign, int index, BuildSlot slot)
        {
            if (index < 0 || index >= 3) throw new ArgumentOutOfRangeException(nameof(index)); SlotValid(slot);
            lock (Gate) { var current = Current(path, campaign); var next = Clone(current); next.Slots[index] = JsonUtility.FromJson<BuildSlot>(JsonUtility.ToJson(slot)); return Commit(path, current, next); }
        }
    }
}
