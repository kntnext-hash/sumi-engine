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
        static void Require(bool ok, string message) { if (!ok) throw new InvalidDataException(message); }
        static bool Id(string value) { Guid parsed; return !string.IsNullOrEmpty(value) && Guid.TryParseExact(value, "N", out parsed); }
        static bool MetaValid(MetaProgress m) => m != null && m.Crystals >= 0 && m.Damage >= 0 && m.Damage <= 20 && m.Supply >= 0 && m.Supply <= 20 && m.Growth >= 0 && m.Growth <= 20 && m.Harvest >= 0 && m.Harvest <= 20;
        static bool SameMeta(MetaProgress a, MetaProgress b) => MetaValid(a) && MetaValid(b) && a.Crystals == b.Crystals && a.Damage == b.Damage && a.Supply == b.Supply && a.Growth == b.Growth && a.Harvest == b.Harvest;
        static void SlotValid(BuildSlot s)
        {
            Require(s != null && s.Name != null && s.Name.Length <= 40 && (s.Speed == 1 || s.Speed == 2), "Invalid build slot.");
            Require(Enum.IsDefined(typeof(WeaponKind), s.Weapon) && Enum.IsDefined(typeof(BuildPreset), s.Build) && Enum.IsDefined(typeof(GrowthPolicy), s.Policy), "Invalid slot enum.");
        }
        static void ResultValid(CampaignResult r)
        {
            Require(r != null && (r.RulesVersion == 1 || r.RulesVersion == 2) && Id(r.RunId) && r.Ticks >= 0 && r.Ticks <= RunSimulator.MaxTicks && r.CompletedWave >= 0 && r.CompletedWave <= RunSimulator.WaveLimit && r.Crystals >= 0 && r.Crystals <= 80000 && r.MilestoneCrystals >= 0 && r.MilestoneCrystals <= RunSimulator.WaveLimit, "Invalid result.");
            Require(r.Status == RunStatus.Defeated || r.Status == RunStatus.TestCompleted || r.Status == RunStatus.CapacityExceeded, "Result must be terminal.");
            Require(Enum.IsDefined(typeof(WeaponKind), r.Weapon) && Enum.IsDefined(typeof(BuildPreset), r.Build) && Enum.IsDefined(typeof(GrowthPolicy), r.Policy), "Invalid result configuration.");
        }
        public static void Validate(CampaignProgress c)
        {
            Require(c != null && c.SchemaVersion == 3 && c.Config == "bokumyaku-campaign-v3", "Unsupported campaign schema/config.");
            Require(c.Revision >= 0 && c.Revision < long.MaxValue && MetaValid(c.Meta), "Invalid campaign progress.");
            Require(c.ActiveSpeed == 1 || c.ActiveSpeed == 2, "Invalid active speed.");
            Require(c.BestWave >= 0 && c.BestWave <= RunSimulator.WaveLimit && c.TotalRuns >= 0 && c.TotalCrystals >= 0 && c.ClaimedMilestoneWave >= 0 && c.ClaimedMilestoneWave <= c.BestWave && c.ClaimedMilestoneWave % 5 == 0, "Invalid cumulative progress.");
            Require(c.History != null && c.History.Count <= 20 && c.History.Count <= c.TotalRuns && c.Slots != null && c.Slots.Length == 3, "Invalid history/slots.");
            var ids = new HashSet<string>();
            long historyCrystals = 0;
            foreach (var r in c.History)
            {
                ResultValid(r); Require(ids.Add(r.RunId), "Duplicate history run ID.");
                if (r.Status == RunStatus.CapacityExceeded) Require(r.Crystals == 0 && r.MilestoneCrystals == 0, "Invalid capacity reward.");
                else Require(c.BestWave >= r.CompletedWave, "Best wave below recorded result.");
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
                try { RunSimulator.Restore(c.ActiveRun); }
                catch (ArgumentException e) { throw new InvalidDataException("Invalid active run snapshot.", e); }
            }
            else Require(string.IsNullOrEmpty(c.ActiveRunId), "Inactive run has an ID.");
        }
        static CampaignProgress Clone(CampaignProgress c) => JsonUtility.FromJson<CampaignProgress>(JsonUtility.ToJson(c));
        static CampaignProgress Read(string path, bool previous = false)
        {
            Require(new FileInfo(path).Length <= MaxFileBytes, "Campaign exceeds size limit.");
            CampaignProgress c;
            try
            {
                string json = File.ReadAllText(path);
                var header = JsonUtility.FromJson<Header>(json);
                Require(header != null && header.SchemaVersion == (previous ? 2 : 3) && header.Config == (previous ? "bokumyaku-campaign-v2" : "bokumyaku-campaign-v3"), "Missing or unsupported campaign header.");
                c = JsonUtility.FromJson<CampaignProgress>(json);
                if (previous)
                {
                    c.SchemaVersion = 3; c.Config = "bokumyaku-campaign-v3";
                    if (c.HasActiveRun) Require(c.ActiveRun != null && c.ActiveRun.schemaVersion == 1 && c.ActiveRun.rulesVersion == 0 && c.ActiveRun.autoPurchaseCursor == 0 && c.ActiveRun.levels != null && c.ActiveRun.levels.Length == 6, "Previous campaign requires an original v5 snapshot.");
                    if (c.LastResult != null) { Require(c.LastResult.RulesVersion == 0 || c.LastResult.RulesVersion == 1, "Invalid previous result rules."); c.LastResult.RulesVersion = 1; }
                    if (c.History != null) foreach (var result in c.History) if (result != null) { Require(result.RulesVersion == 0 || result.RulesVersion == 1, "Invalid previous history rules."); result.RulesVersion = 1; }
                }
            }
            catch (ArgumentException e) { throw new InvalidDataException("Malformed campaign JSON.", e); }
            Validate(c); return c;
        }
        static CampaignProgress ReadRecover(string path, out string warning, bool previous = false)
        {
            warning = "";
            try { return Read(path, previous); }
            catch (Exception e) when (e is IOException || e is InvalidDataException)
            {
                try { var c = Read(path + ".bak", previous); warning = "Campaign recovered from backup; original preserved."; return c; }
                catch (Exception backup) when (backup is IOException || backup is InvalidDataException)
                { throw new InvalidDataException("Campaign and backup are unreadable; no data was replaced.", new AggregateException(e, backup)); }
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
            Validate(c); string json = JsonUtility.ToJson(c, true);
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
                Require(SameMeta(current.Meta, run.CaptureState().meta), "Run must use current campaign meta.");
                next.HasActiveRun = true; next.ActiveRunId = Guid.NewGuid().ToString("N"); next.ActiveRun = run.CaptureState(); next.ActiveSpeed = speed;
                return Commit(path, current, next);
            }
        }
        static void MatchRun(CampaignProgress c, RunSimulator run)
        {
            Require(c.HasActiveRun && c.ActiveRun != null && run != null, "No active run to update.");
            Require(RunSimulator.Restore(c.ActiveRun).RulesVersion == run.RulesVersion, "Active run rules changed.");
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
                int bonus = Math.Max(0, milestone - current.ClaimedMilestoneWave);
                next.Meta.Crystals = checked(next.Meta.Crystals + baseReward + bonus);
                if (validResult) next.BestWave = Math.Max(next.BestWave, settled.CompletedWave);
                next.ClaimedMilestoneWave = Math.Max(next.ClaimedMilestoneWave, milestone);
                next.TotalRuns = checked(next.TotalRuns + 1); next.TotalCrystals = checked(next.TotalCrystals + baseReward + bonus);
                next.LastSettledRunId = runId;
                next.LastResult = new CampaignResult { RunId = runId, RulesVersion = settled.RulesVersion, Seed = settled.Seed, CompletedWave = settled.CompletedWave, Ticks = settled.TickCount, Status = settled.State, Crystals = baseReward, MilestoneCrystals = bonus, Weapon = settled.Weapon, Build = settled.Build, Policy = settled.Policy };
                next.History.Add(next.LastResult); if (next.History.Count > 20) next.History.RemoveAt(0);
                next.HasActiveRun = false; next.ActiveRun = null; next.ActiveRunId = "";
                return Commit(path, current, next);
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
