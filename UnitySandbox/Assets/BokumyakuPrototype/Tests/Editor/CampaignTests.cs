using System;
using System.IO;
using Bokumyaku.Persistence;
using Bokumyaku.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace Bokumyaku.Tests
{
    public sealed class CampaignTests
    {
        string directory, path;
        [SetUp] public void Setup() { directory = Path.Combine(Path.GetTempPath(), "BokumyakuCampaignQA-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory); path = Path.Combine(directory, "campaign.json"); }
        [TearDown] public void Cleanup() { foreach (var f in Directory.GetFiles(directory)) File.Delete(f); Directory.Delete(directory); }
        static RunSimulator Finished(int seed, MetaProgress meta)
        {
            var run = new RunSimulator(seed, meta);
            for (int i = 0; i <= RunSimulator.MaxTicks && run.State == RunStatus.Running; i++) run.Tick();
            Assert.AreNotEqual(RunStatus.Running, run.State); return run;
        }
        [Test]
        public void LegacyMigrationPreservesOriginalAndAllMetaFields()
        {
            var legacy = Path.Combine(directory, "v1.json");
            MetaSaveStore.Save(legacy, new MetaProgress { Crystals = 35, Damage = 1, Supply = 2, Growth = 3, Harvest = 4 });
            string bytes = File.ReadAllText(legacy), warning;
            var c = CampaignSaveStore.Load(path, legacy, out warning);
            Assert.AreEqual(bytes, File.ReadAllText(legacy)); Assert.IsTrue(File.Exists(path));
            Assert.AreEqual(35, c.Meta.Crystals); Assert.AreEqual(4, c.Meta.Harvest); Assert.AreEqual(0, c.TotalRuns);
            Assert.IsNotEmpty(warning); Assert.IsFalse(c.HasActiveRun);
        }
        [Test]
        public void CorruptLegacyIsNotSilentlyMigratedAsZero()
        {
            var legacy = Path.Combine(directory, "v1.json"); File.WriteAllText(legacy, "bad-json");
            Assert.Catch(() => { string w; CampaignSaveStore.Load(path, legacy, out w); });
            Assert.IsFalse(File.Exists(path)); Assert.AreEqual("bad-json", File.ReadAllText(legacy));
        }
        [Test]
        public void ActiveCheckpointRestoresSameTickAndContinuesDeterministically()
        {
            var c = new CampaignProgress(); var live = new RunSimulator(42, c.Meta);
            c = CampaignSaveStore.BeginRun(path, c, live, 2); string id = c.ActiveRunId;
            for (int i = 0; i < 300; i++) live.Tick();
            c = CampaignSaveStore.SaveActive(path, c, live, 2);
            string warning; var loaded = CampaignSaveStore.Load(path, out warning); var restored = RunSimulator.Restore(loaded.ActiveRun);
            Assert.AreEqual(id, loaded.ActiveRunId); Assert.AreEqual(2, loaded.ActiveSpeed); Assert.AreEqual(300, restored.TickCount);
            for (int i = 0; i < 100; i++)
            {
                live.Tick(); restored.Tick();
                Assert.AreEqual(JsonUtility.ToJson(live.CaptureState()), JsonUtility.ToJson(restored.CaptureState()));
            }
        }
        [Test]
        public void SettlementAndReplayCreditExactlyOnceAcrossReload()
        {
            var old = CampaignSaveStore.BeginRun(path, new CampaignProgress(), new RunSimulator(42));
            var finished = Finished(42, old.Meta); string id = old.ActiveRunId;
            var settled = CampaignSaveStore.SettleRun(path, old, id, finished);
            Assert.IsFalse(settled.HasActiveRun); Assert.IsTrue(old.HasActiveRun); Assert.AreEqual(0, old.Meta.Crystals);
            Assert.AreEqual(1, settled.TotalRuns); Assert.AreEqual(finished.CrystalsReward + settled.LastResult.MilestoneCrystals, settled.Meta.Crystals);
            string warning; var reload = CampaignSaveStore.Load(path, out warning);
            var retry = CampaignSaveStore.SettleRun(path, old, id, finished);
            Assert.AreEqual(reload.Revision, retry.Revision); Assert.AreEqual(reload.Meta.Crystals, retry.Meta.Crystals);
            Assert.AreEqual(1, retry.History.Count); Assert.IsFalse(finished.CaptureState().rewardClaimed);
        }
        [Test]
        public void ThreeRunsRetainHistoryAndMilestoneDoesNotPayTwice()
        {
            var c = new CampaignProgress(); int firstBonus = -1;
            for (int i = 0; i < 3; i++)
            {
                c = CampaignSaveStore.BeginRun(path, c, new RunSimulator(42, c.Meta));
                c = CampaignSaveStore.SettleRun(path, c, c.ActiveRunId, Finished(42, c.Meta));
                if (i == 0) firstBonus = c.LastResult.MilestoneCrystals;
                else Assert.AreEqual(0, c.LastResult.MilestoneCrystals);
                Assert.AreEqual(i + 1, c.TotalRuns);
            }
            Assert.GreaterOrEqual(firstBonus, 0); Assert.AreEqual(3, c.History.Count);
            Assert.AreEqual(c.Meta.Crystals, c.TotalCrystals);
        }
        [Test]
        public void StaleWriterCannotReplaceNewerCheckpoint()
        {
            var stale = CampaignSaveStore.BeginRun(path, new CampaignProgress(), new RunSimulator(42));
            var newer = CampaignSaveStore.SaveSlot(path, stale, 0, new BuildSlot { Name = "Changed", Weapon = WeaponKind.Pierce, Speed = 2 });
            Assert.Throws<InvalidDataException>(() => CampaignSaveStore.DiscardRun(path, stale));
            string warning; Assert.AreEqual(newer.Revision, CampaignSaveStore.Load(path, out warning).Revision);
        }
        [Test]
        public void SaveFailureDoesNotMutateMemoryOrConsumeReward()
        {
            var c = CampaignSaveStore.BeginRun(path, new CampaignProgress(), new RunSimulator(42));
            var ended = Finished(42, c.Meta); string before = File.ReadAllText(path);
            using (var held = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                Assert.Catch(() => CampaignSaveStore.SettleRun(path, c, c.ActiveRunId, ended));
            Assert.AreEqual(before, File.ReadAllText(path)); Assert.AreEqual(0, c.Meta.Crystals); Assert.AreEqual(0, c.TotalRuns);
            Assert.IsTrue(c.HasActiveRun); Assert.IsFalse(ended.CaptureState().rewardClaimed);
            var retry = CampaignSaveStore.SettleRun(path, c, c.ActiveRunId, ended); Assert.AreEqual(1, retry.TotalRuns);
        }
        [Test]
        public void CorruptPrimaryRecoversBackupAndKeepsCorruptOriginalOnNextSave()
        {
            var c = CampaignSaveStore.Save(path, new CampaignProgress());
            c = CampaignSaveStore.SaveSlot(path, c, 0, new BuildSlot { Name = "new" });
            File.WriteAllText(path, "broken"); string warning;
            var recovered = CampaignSaveStore.Load(path, out warning); Assert.IsNotEmpty(warning);
            Assert.AreEqual("broken", File.ReadAllText(path));
            CampaignSaveStore.Save(path, recovered);
            Assert.AreEqual(1, Directory.GetFiles(directory, "*.corrupt-*").Length);
            Assert.AreEqual(1, CampaignSaveStore.Load(path + ".bak", out warning).Revision);
        }
        [Test]
        public void InvalidSchemaAndSlotAndSnapshotAreRejectedBeforeWrite()
        {
            var c = new CampaignProgress(); c.SchemaVersion = 99;
            Assert.Throws<InvalidDataException>(() => CampaignSaveStore.Save(path, c)); Assert.IsFalse(File.Exists(path));
            c = new CampaignProgress(); Assert.Throws<InvalidDataException>(() => CampaignSaveStore.SaveSlot(path, c, 0, new BuildSlot { Speed = 3 }));
            c = CampaignSaveStore.BeginRun(path, c, new RunSimulator(42)); string before = File.ReadAllText(path);
            c.ActiveRun.tickCount = -1;
            Assert.Throws<InvalidDataException>(() => CampaignSaveStore.Save(path, c)); Assert.AreEqual(before, File.ReadAllText(path));
        }
        [Test]
        public void WrongRunIdAndUnfinishedRunCannotSettle()
        {
            var run = new RunSimulator(42); var c = CampaignSaveStore.BeginRun(path, new CampaignProgress(), run);
            Assert.Throws<InvalidDataException>(() => CampaignSaveStore.SettleRun(path, c, c.ActiveRunId, run));
            Assert.Throws<InvalidDataException>(() => CampaignSaveStore.SettleRun(path, c, Guid.NewGuid().ToString("N"), Finished(42, c.Meta)));
            Assert.AreEqual(0, c.TotalRuns); Assert.AreEqual(0, c.Meta.Crystals);
        }
        [Test]
        public void DiscardAndThreeSlotsPersistWithoutGrantingRewards()
        {
            var c = CampaignSaveStore.BeginRun(path, new CampaignProgress(), new RunSimulator(42));
            for (int i = 0; i < 3; i++) c = CampaignSaveStore.SaveSlot(path, c, i, new BuildSlot { Name = "slot" + i, Weapon = (WeaponKind)i, Build = (BuildPreset)i, Policy = (GrowthPolicy)i, Speed = 2 });
            c = CampaignSaveStore.DiscardRun(path, c); Assert.AreEqual(0, c.Meta.Crystals); Assert.AreEqual(0, c.TotalRuns);
            string warning; var loaded = CampaignSaveStore.Load(path, out warning);
            for (int i = 0; i < 3; i++) { Assert.AreEqual("slot" + i, loaded.Slots[i].Name); Assert.AreEqual((WeaponKind)i, loaded.Slots[i].Weapon); }
            Assert.IsFalse(loaded.HasActiveRun);
        }
        [Test]
        public void HistoryRetainsOnlyNewestTwentyWithoutRegrantingMilestones()
        {
            var c = new CampaignProgress(); var terminal = Finished(42, c.Meta).CaptureState();
            string firstId = "";
            for (int i = 0; i < 21; i++)
            {
                c = CampaignSaveStore.BeginRun(path, c, new RunSimulator(42, c.Meta));
                if (i == 0) firstId = c.ActiveRunId;
                // Same deterministic terminal state with the current run-start meta snapshot.
                terminal.meta = new RunSimulator(42, c.Meta).CaptureState().meta;
                c = CampaignSaveStore.SettleRun(path, c, c.ActiveRunId, RunSimulator.Restore(terminal));
            }
            Assert.AreEqual(21, c.TotalRuns); Assert.AreEqual(20, c.History.Count);
            Assert.IsFalse(c.History.Exists(r => r.RunId == firstId));
            Assert.AreEqual(0, c.LastResult.MilestoneCrystals);
        }
        [Test]
        public void CapacityFailureCannotSetBestWaveOrGrantMilestone()
        {
            var c = CampaignSaveStore.BeginRun(path, new CampaignProgress(), new RunSimulator(42));
            var s = new RunSimulator(42).CaptureState(); s.status = RunStatus.CapacityExceeded;
            s.tickCount = 3600; s.wave = 10; s.completedWave = 10;
            c = CampaignSaveStore.SettleRun(path, c, c.ActiveRunId, RunSimulator.Restore(s));
            Assert.AreEqual(0, c.BestWave); Assert.AreEqual(0, c.Meta.Crystals); Assert.AreEqual(0, c.ClaimedMilestoneWave);
            Assert.AreEqual(RunStatus.CapacityExceeded, c.LastResult.Status);
        }
        [Test]
        public void WaveCapCompletionCanClaimFirstMilestones()
        {
            var c = CampaignSaveStore.BeginRun(path, new CampaignProgress(), new RunSimulator(42));
            var s = new RunSimulator(42).CaptureState(); s.status = RunStatus.TestCompleted;
            s.tickCount = RunSimulator.MaxTicks; s.wave = RunSimulator.WaveLimit; s.completedWave = RunSimulator.WaveLimit;
            c = CampaignSaveStore.SettleRun(path, c, c.ActiveRunId, RunSimulator.Restore(s));
            Assert.AreEqual(RunSimulator.WaveLimit, c.BestWave);
            Assert.AreEqual(RunSimulator.WaveLimit / 5 * 5, c.LastResult.MilestoneCrystals);
        }
    }
}
