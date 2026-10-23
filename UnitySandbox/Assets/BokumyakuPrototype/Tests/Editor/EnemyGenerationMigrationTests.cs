using System;
using System.IO;
using Bokumyaku.Persistence;
using Bokumyaku.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace Bokumyaku.Tests
{
    public sealed class EnemyGenerationMigrationTests
    {
        string dir, current, v9, v8;
        [SetUp] public void Setup() { dir = Path.Combine(Path.GetTempPath(), "BokumyakuEnemyGeneration-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(dir); current = Path.Combine(dir, "v10.json"); v9 = Path.Combine(dir, "v9.json"); v8 = Path.Combine(dir, "v8.json"); }
        [TearDown] public void Cleanup() { foreach (var f in Directory.GetFiles(dir)) File.Delete(f); Directory.Delete(dir); }
        CampaignProgress Load() { string warning; return CampaignSaveStore.LoadTierLatest(current, v9, v8, null, null, null, null, null, null, null, out warning); }
        static string CanonicalCampaign(CampaignProgress campaign)
        {
            // JsonUtility may read a decimal double as its adjacent representable value.
            // Decode and validate the untouched exactDoubleBits before another JSON round trip.
            var exact = campaign.HasActiveRun ? RunSimulator.Restore(campaign.ActiveRun).CaptureState() : null;
            var copy = JsonUtility.FromJson<CampaignProgress>(JsonUtility.ToJson(campaign));
            if (campaign.HasActiveRun) copy.ActiveRun = exact;
            return JsonUtility.ToJson(copy);
        }
        [Test] public void RulesSevenActiveTierTwoKeepsFutureAndOriginalFiles()
        {
            // Tick-zero structural conversion to the documented rules7 five-counter layout.
            var snapshot = new RunSimulator(42, tier: 2).CaptureState(); snapshot.rulesVersion = 7;
            Array.Resize(ref snapshot.spawnedByType, 5); Array.Resize(ref snapshot.killedByType, 5); Array.Resize(ref snapshot.leakedByType, 5);
            var control = RunSimulator.Restore(snapshot); control.SetBuild(BuildPreset.Farming);
            for (int i = 0; i < 900; i++) control.Tick();
            var c = new CampaignProgress { SelectedTier = 2, BestWave = 100, BestWaveByTier = new[] { 100, 0, 0 }, HasActiveRun = true, ActiveRun = control.CaptureState(), ActiveRunId = Guid.NewGuid().ToString("N"), ActiveSpeed = 2 };
            c = CampaignSaveStore.Save(v9, c); c = CampaignSaveStore.Save(v9, c);
            var bytes = File.ReadAllBytes(v9); var backup = File.ReadAllBytes(v9 + ".bak");
            var migrated = Load();
            Assert.AreEqual(c.ActiveRun.exactDoubleBits, migrated.ActiveRun.exactDoubleBits);
            Assert.AreEqual(CanonicalCampaign(c), CanonicalCampaign(migrated));
            var resumed = RunSimulator.Restore(migrated.ActiveRun);
            Assert.AreEqual(JsonUtility.ToJson(control.CaptureState()), JsonUtility.ToJson(resumed.CaptureState()));
            Assert.AreEqual(7, resumed.RulesVersion); Assert.AreEqual(2, resumed.Tier); Assert.AreEqual(5, resumed.CaptureState().spawnedByType.Length);
            for (int i = 0; i < 900; i++) { control.Tick(); resumed.Tick(); }
            Assert.AreEqual(JsonUtility.ToJson(control.CaptureState()), JsonUtility.ToJson(resumed.CaptureState()));
            CampaignSaveStore.SaveActive(current, migrated, resumed, 2);
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(v9)); CollectionAssert.AreEqual(backup, File.ReadAllBytes(v9 + ".bak"));
        }
        [Test] public void V9WinsOverOlderV8()
        {
            CampaignSaveStore.Save(v9, new CampaignProgress { Meta = new MetaProgress { Crystals = 123 } });
            File.WriteAllText(v8, JsonUtility.ToJson(new CampaignProgress { SchemaVersion = 3, Config = "bokumyaku-campaign-v3", Meta = new MetaProgress { Crystals = 999 } }));
            Assert.AreEqual(123, Load().Meta.Crystals);
        }
        [Test] public void AbsentV9FallsBackToSchemaThreeV8()
        {
            string bytes = JsonUtility.ToJson(new CampaignProgress { SchemaVersion = 3, Config = "bokumyaku-campaign-v3", BestWave = 135, ClaimedMilestoneWave = 130 }); File.WriteAllText(v8, bytes);
            var c = Load(); Assert.AreEqual(135, c.BestWaveByTier[0]); Assert.AreEqual(130, c.ClaimedMilestoneWaveByTier[0]); Assert.AreEqual(bytes, File.ReadAllText(v8)); Assert.IsFalse(File.Exists(v9));
        }
        [Test] public void CorruptV9DoesNotFallBackToV8()
        {
            File.WriteAllText(v9, "broken-v9"); CampaignSaveStore.Save(v8, new CampaignProgress());
            Assert.Throws<InvalidDataException>(() => Load()); Assert.IsFalse(File.Exists(current)); Assert.AreEqual("broken-v9", File.ReadAllText(v9));
        }
        [Test] public void V9BackupIsRecoveredWithoutRewritingSources()
        {
            CampaignSaveStore.Save(v9, new CampaignProgress { Meta = new MetaProgress { Crystals = 37 } }); File.Copy(v9, v9 + ".bak"); File.WriteAllText(v9, "broken");
            var backup = File.ReadAllBytes(v9 + ".bak"); Assert.AreEqual(37, Load().Meta.Crystals);
            Assert.AreEqual("broken", File.ReadAllText(v9)); CollectionAssert.AreEqual(backup, File.ReadAllBytes(v9 + ".bak"));
        }
        [Test] public void CorruptV10CannotRollBackToV9()
        {
            CampaignSaveStore.Save(v9, new CampaignProgress()); File.WriteAllText(current, "broken-v10");
            Assert.Throws<InvalidDataException>(() => Load()); Assert.AreEqual("broken-v10", File.ReadAllText(current));
        }
        [Test] public void MigrationDestinationCannotAliasV9()
        {
            Assert.Throws<InvalidDataException>(() => { string warning; CampaignSaveStore.LoadTierLatest(v9, v9, v8, null, null, null, null, null, null, null, out warning); });
            Assert.IsFalse(File.Exists(v9));
        }
    }
}
