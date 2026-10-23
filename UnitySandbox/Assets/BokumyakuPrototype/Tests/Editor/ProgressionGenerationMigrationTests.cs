using System;
using System.IO;
using Bokumyaku.Persistence;
using Bokumyaku.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace Bokumyaku.Tests
{
    public sealed class ProgressionGenerationMigrationTests
    {
        string dir, current, v10, v9;
        [SetUp] public void Setup() { dir = Path.Combine(Path.GetTempPath(), "BokumyakuProgressionGeneration-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(dir); current = Path.Combine(dir, "v11.json"); v10 = Path.Combine(dir, "v10.json"); v9 = Path.Combine(dir, "v9.json"); }
        [TearDown] public void Cleanup() { foreach (var f in Directory.GetFiles(dir)) File.Delete(f); Directory.Delete(dir); }
        CampaignProgress Load() { string warning; return CampaignSaveStore.LoadTierLatest(current, v10, v9, null, null, null, null, null, null, null, null, out warning); }
        static string CanonicalCampaign(CampaignProgress campaign)
        {
            // JsonUtility may read a decimal double as its adjacent representable value.
            // Decode and validate the untouched exactDoubleBits before another JSON round trip.
            var exact = campaign.HasActiveRun ? RunSimulator.Restore(campaign.ActiveRun).CaptureState() : null;
            var copy = JsonUtility.FromJson<CampaignProgress>(JsonUtility.ToJson(campaign));
            if (campaign.HasActiveRun) copy.ActiveRun = exact;
            return JsonUtility.ToJson(copy);
        }
        [Test] public void RulesEightActiveTierTwoKeepsFutureAndOriginalFiles()
        {
            // Tick-zero structural conversion to the documented rules8 seven-counter layout.
            var snapshot = new RunSimulator(42, tier: 2).CaptureState(); snapshot.rulesVersion = 8;
            Array.Resize(ref snapshot.spawnedByType, 7); Array.Resize(ref snapshot.killedByType, 7); Array.Resize(ref snapshot.leakedByType, 7);
            var control = RunSimulator.Restore(snapshot); control.SetBuild(BuildPreset.Farming);
            for (int i = 0; i < 900; i++) control.Tick();
            var c = new CampaignProgress { SelectedTier = 2, BestWave = 100, BestWaveByTier = new[] { 100, 0, 0 }, HasActiveRun = true, ActiveRun = control.CaptureState(), ActiveRunId = Guid.NewGuid().ToString("N"), ActiveSpeed = 2 };
            c = CampaignSaveStore.Save(v10, c); c = CampaignSaveStore.Save(v10, c);
            var bytes = File.ReadAllBytes(v10); var backup = File.ReadAllBytes(v10 + ".bak");
            var migrated = Load();
            Assert.AreEqual(c.ActiveRun.exactDoubleBits, migrated.ActiveRun.exactDoubleBits);
            Assert.AreEqual(CanonicalCampaign(c), CanonicalCampaign(migrated));
            var resumed = RunSimulator.Restore(migrated.ActiveRun);
            Assert.AreEqual(JsonUtility.ToJson(control.CaptureState()), JsonUtility.ToJson(resumed.CaptureState()));
            Assert.AreEqual(8, resumed.RulesVersion); Assert.AreEqual(2, resumed.Tier); Assert.AreEqual(7, resumed.CaptureState().spawnedByType.Length);
            for (int i = 0; i < 900; i++) { control.Tick(); resumed.Tick(); }
            Assert.AreEqual(JsonUtility.ToJson(control.CaptureState()), JsonUtility.ToJson(resumed.CaptureState()));
            CampaignSaveStore.SaveActive(current, migrated, resumed, 2);
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(v10)); CollectionAssert.AreEqual(backup, File.ReadAllBytes(v10 + ".bak"));
        }
        [Test] public void PurchasedResearchAndCrystalBalanceAreNotRepriced()
        {
            var previous = CampaignSaveStore.Save(v10, new CampaignProgress { Meta = new MetaProgress { Damage = 21, Supply = 8, Growth = 7, Harvest = 6, Crystals = 12345 } });
            var bytes = File.ReadAllBytes(v10); var migrated = Load();
            Assert.AreEqual(JsonUtility.ToJson(previous.Meta), JsonUtility.ToJson(migrated.Meta));
            Assert.AreEqual(12345, migrated.Meta.Crystals); Assert.AreEqual(21, migrated.Meta.Damage);
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(v10));
        }
        [Test] public void V10WinsOverOlderV9()
        {
            CampaignSaveStore.Save(v10, new CampaignProgress { Meta = new MetaProgress { Crystals = 123 } });
            File.WriteAllText(v9, JsonUtility.ToJson(new CampaignProgress { Meta = new MetaProgress { Crystals = 999 } }));
            Assert.AreEqual(123, Load().Meta.Crystals);
        }
        [Test] public void AbsentV10FallsBackToSchemaFourV9()
        {
            string bytes = JsonUtility.ToJson(new CampaignProgress { BestWave = 135, ClaimedMilestoneWave = 130, BestWaveByTier = new[] { 135, 0, 0 }, ClaimedMilestoneWaveByTier = new[] { 130, 0, 0 } }); File.WriteAllText(v9, bytes);
            var c = Load(); Assert.AreEqual(135, c.BestWaveByTier[0]); Assert.AreEqual(130, c.ClaimedMilestoneWaveByTier[0]); Assert.AreEqual(bytes, File.ReadAllText(v9)); Assert.IsFalse(File.Exists(v10));
        }
        [Test] public void CorruptV10DoesNotFallBackToV9()
        {
            File.WriteAllText(v10, "broken-v10"); CampaignSaveStore.Save(v9, new CampaignProgress());
            Assert.Throws<InvalidDataException>(() => Load()); Assert.IsFalse(File.Exists(current)); Assert.AreEqual("broken-v10", File.ReadAllText(v10));
        }
        [Test] public void V10BackupIsRecoveredWithoutRewritingSources()
        {
            CampaignSaveStore.Save(v10, new CampaignProgress { Meta = new MetaProgress { Crystals = 37 } }); File.Copy(v10, v10 + ".bak"); File.WriteAllText(v10, "broken");
            var backup = File.ReadAllBytes(v10 + ".bak"); Assert.AreEqual(37, Load().Meta.Crystals);
            Assert.AreEqual("broken", File.ReadAllText(v10)); CollectionAssert.AreEqual(backup, File.ReadAllBytes(v10 + ".bak"));
        }
        [Test] public void CorruptV11CannotRollBackToV10()
        {
            CampaignSaveStore.Save(v10, new CampaignProgress()); File.WriteAllText(current, "broken-v11");
            Assert.Throws<InvalidDataException>(() => Load()); Assert.AreEqual("broken-v11", File.ReadAllText(current));
        }
        [Test] public void MigrationDestinationCannotAliasV10()
        {
            Assert.Throws<InvalidDataException>(() => { string warning; CampaignSaveStore.LoadTierLatest(v10, v10, v9, null, null, null, null, null, null, null, null, out warning); });
            Assert.IsFalse(File.Exists(v10));
        }
    }
}
