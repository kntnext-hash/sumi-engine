using System;
using System.IO;
using Bokumyaku.Persistence;
using Bokumyaku.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace Bokumyaku.Tests
{
    public sealed class TierPersistenceTests
    {
        string directory, path;
        [SetUp] public void Setup() { directory = Path.Combine(Path.GetTempPath(), "BokumyakuTierQA-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory); path = Path.Combine(directory, "v9.json"); }
        [TearDown] public void Cleanup() { foreach (var file in Directory.GetFiles(directory)) File.Delete(file); Directory.Delete(directory); }
        static CampaignProgress Unlocked() => new CampaignProgress { BestWave = 100, ClaimedMilestoneWave = 100, BestWaveByTier = new[] { 100, 0, 0 }, ClaimedMilestoneWaveByTier = new[] { 100, 0, 0 } };
        static RunSimulator Finish(RunSimulator run) { for (int i = 0; i < 30000 && run.State == RunStatus.Running; i++) run.Tick(); Assert.AreEqual(RunStatus.Defeated, run.State); return run; }
        CampaignProgress Migrate(string source) { string warning; return CampaignSaveStore.LoadTierLatest(path, source, null, null, null, null, null, null, null, out warning); }
        [Test] public void UnlockBoundaryAndSelectionAreEnforced()
        {
            var c = new CampaignProgress { BestWave = 99, BestWaveByTier = new[] { 99, 0, 0 } };
            Assert.Throws<InvalidDataException>(() => CampaignSaveStore.SelectTier(path, c, 2));
            Assert.Throws<InvalidDataException>(() => CampaignSaveStore.BeginRun(path, c, new RunSimulator(42, c.Meta, 2)));
            c = Unlocked(); c = CampaignSaveStore.SelectTier(path, c, 2);
            Assert.AreEqual(2, c.SelectedTier);
            Assert.Throws<InvalidDataException>(() => CampaignSaveStore.SelectTier(path, c, 3));
            c = CampaignSaveStore.BeginRun(path, c, new RunSimulator(42, c.Meta, 2));
            Assert.Throws<InvalidDataException>(() => CampaignSaveStore.SelectTier(path, c, 1));
            Assert.Throws<InvalidDataException>(() => CampaignSaveStore.SaveActive(path, c, new RunSimulator(42, c.Meta, 1)));
        }
        [Test] public void TierSettlementCreditsIndependentMilestonesAndCannotReplay()
        {
            var c = Unlocked(); var run = new RunSimulator(42, c.Meta, 2);
            c = CampaignSaveStore.BeginRun(path, c, run); string id = c.ActiveRunId; Finish(run);
            var settled = CampaignSaveStore.SettleRun(path, c, id, run);
            Assert.AreEqual(100, settled.BestWave); Assert.AreEqual(100, settled.ClaimedMilestoneWave);
            Assert.AreEqual(run.CompletedWave, settled.BestWaveByTier[1]); Assert.AreEqual(2, settled.LastResult.Tier);
            Assert.AreEqual(run.CompletedWave / 5 * 5, settled.LastResult.MilestoneCrystals);
            Assert.AreEqual(run.CrystalsReward + settled.LastResult.MilestoneCrystals, settled.Meta.Crystals);
            var replay = CampaignSaveStore.SettleRun(path, c, id, run);
            Assert.AreEqual(settled.Revision, replay.Revision); Assert.AreEqual(settled.Meta.Crystals, replay.Meta.Crystals);
            run = new RunSimulator(42, settled.Meta, 2); c = CampaignSaveStore.BeginRun(path, settled, run); Finish(run);
            c = CampaignSaveStore.SettleRun(path, c, c.ActiveRunId, run); Assert.AreEqual(0, c.LastResult.MilestoneCrystals);
        }
        [Test] public void LegacyMigrationPreservesRecordsOutsideHistoryAndDoesNotPayAgain()
        {
            string source = Path.Combine(directory, "v8.json"); var legacy = new CampaignProgress { SchemaVersion = 3, Config = "bokumyaku-campaign-v3", BestWave = 250, ClaimedMilestoneWave = 250 };
            string bytes = JsonUtility.ToJson(legacy); File.WriteAllText(source, bytes); var c = Migrate(source);
            Assert.AreEqual(bytes, File.ReadAllText(source)); Assert.AreEqual(4, c.SchemaVersion);
            CollectionAssert.AreEqual(new[] { 250, 0, 0 }, c.BestWaveByTier); CollectionAssert.AreEqual(new[] { 250, 0, 0 }, c.ClaimedMilestoneWaveByTier);
            var run = new RunSimulator(42, c.Meta); c = CampaignSaveStore.BeginRun(path, c, run); Finish(run);
            c = CampaignSaveStore.SettleRun(path, c, c.ActiveRunId, run); Assert.AreEqual(0, c.LastResult.MilestoneCrystals); Assert.AreEqual(250, c.BestWave);
        }
        [Test] public void LegacyActiveRunRemainsTier1AndKeepsItsDeterministicFuture()
        {
            var snapshot = JsonUtility.FromJson<RunSnapshot>(File.ReadAllText(Path.Combine(Application.dataPath, "BokumyakuPrototype/Tests/Fixtures/v10-point-tick6000.json")));
            var legacy = new CampaignProgress { SchemaVersion = 3, Config = "bokumyaku-campaign-v3", Meta = snapshot.meta, HasActiveRun = true, ActiveRun = snapshot, ActiveRunId = Guid.NewGuid().ToString("N"), ActiveSpeed = 2, BestWave = 150, ClaimedMilestoneWave = 150 };
            string source = Path.Combine(directory, "v8.json"), bytes = JsonUtility.ToJson(legacy); File.WriteAllText(source, bytes);
            var c = Migrate(source); var run = RunSimulator.Restore(c.ActiveRun); var control = RunSimulator.Restore(snapshot);
            Assert.AreEqual(1, run.Tier); Assert.AreEqual(6, run.RulesVersion); Assert.AreEqual(2, c.ActiveSpeed);
            for (int i = 0; i < 900; i++) { run.Tick(); control.Tick(); }
            Assert.AreEqual(JsonUtility.ToJson(control.CaptureState()), JsonUtility.ToJson(run.CaptureState()));
            c = CampaignSaveStore.SaveActive(path, c, run, 2); Assert.AreEqual(1, c.SelectedTier); Assert.AreEqual(bytes, File.ReadAllText(source));
        }
        [Test] public void ExistingMalformedCurrentNeverFallsBackToOldCampaign()
        {
            string source = Path.Combine(directory, "v8.json"); File.WriteAllText(source, JsonUtility.ToJson(new CampaignProgress { SchemaVersion = 3, Config = "bokumyaku-campaign-v3" }));
            string broken = JsonUtility.ToJson(new CampaignProgress()).Replace("\"SelectedTier\":1,", ""); File.WriteAllText(path, broken);
            Assert.Throws<InvalidDataException>(() => Migrate(source)); Assert.AreEqual(broken, File.ReadAllText(path));
        }
        [Test] public void Tier1MirrorsAndLockedRecordsCannotDiverge()
        {
            Assert.Throws<InvalidDataException>(() => CampaignSaveStore.Validate(new CampaignProgress { BestWave = 20 }));
            Assert.Throws<InvalidDataException>(() => CampaignSaveStore.Validate(new CampaignProgress { BestWaveByTier = new[] { 0, 1, 0 } }));
            Assert.Throws<InvalidDataException>(() => CampaignSaveStore.Validate(new CampaignProgress { SelectedTier = 0 }));
        }
        [Test] public void OldSchemaAtAuthoritativePathRequiresExplicitMigration()
        {
            File.WriteAllText(path, JsonUtility.ToJson(new CampaignProgress { SchemaVersion = 3, Config = "bokumyaku-campaign-v3" }));
            Assert.Throws<InvalidDataException>(() => { string warning; CampaignSaveStore.Load(path, out warning); });
        }
    }
}
