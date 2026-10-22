using System;
using System.IO;
using Bokumyaku.Persistence;
using Bokumyaku.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace Bokumyaku.Tests
{
    public sealed class MigrationTests
    {
        string directory, current, previous;
        [SetUp] public void Setup() { directory = Path.Combine(Path.GetTempPath(), "BokumyakuMigration-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory); current = Path.Combine(directory, "v3.json"); previous = Path.Combine(directory, "v2.json"); }
        [TearDown] public void Cleanup() { foreach (var f in Directory.GetFiles(directory)) File.Delete(f); Directory.Delete(directory); }
        static string AsV2(CampaignProgress c) { c.SchemaVersion = 2; c.Config = "bokumyaku-campaign-v2"; return JsonUtility.ToJson(c).Replace("\"RulesVersion\":1,", ""); }
        static RunSnapshot LegacySnapshot(string name = "v5-run-tick1800.json") => JsonUtility.FromJson<RunSnapshot>(File.ReadAllText(Path.Combine(Application.dataPath, "BokumyakuPrototype/Tests/Fixtures", name)));
        [Test] public void FullCampaignMigrationPreservesProgressSlotsHistoryAndSourceBytes()
        {
            var snapshot = LegacySnapshot(); var live = RunSimulator.Restore(snapshot);
            var c = new CampaignProgress { HasActiveRun = true, ActiveRunId = Guid.NewGuid().ToString("N"), ActiveRun = snapshot };
            c = CampaignSaveStore.Save(current, c);
            while (live.State == RunStatus.Running) live.Tick();
            c = CampaignSaveStore.SettleRun(current, c, c.ActiveRunId, live);
            c.Slots[1] = new BuildSlot { Name = "saved", Weapon = WeaponKind.Pierce, Build = BuildPreset.Record, Policy = GrowthPolicy.Balanced, Speed = 2 };
            Assert.AreEqual(1, c.LastResult.RulesVersion);
            var expected = JsonUtility.ToJson(c); var bytes = AsV2(c);
            File.WriteAllText(previous, bytes); File.WriteAllText(previous + ".bak", bytes);
            File.Delete(current); if (File.Exists(current + ".bak")) File.Delete(current + ".bak");
            string warning; var migrated = CampaignSaveStore.Load(current, previous, null, out warning);
            Assert.AreEqual(expected, JsonUtility.ToJson(migrated)); Assert.IsNotEmpty(warning);
            Assert.AreEqual(bytes, File.ReadAllText(previous)); Assert.AreEqual(bytes, File.ReadAllText(previous + ".bak"));
            Assert.AreEqual(expected, JsonUtility.ToJson(CampaignSaveStore.Load(current, out warning)));
        }
        [Test] public void CorruptPreviousRecoversBackupWithoutChangingEitherSource()
        {
            var good = AsV2(new CampaignProgress { Meta = new MetaProgress { Crystals = 91 } });
            File.WriteAllText(previous, "broken"); File.WriteAllText(previous + ".bak", good);
            string warning; var c = CampaignSaveStore.Load(current, previous, null, out warning);
            Assert.AreEqual(91, c.Meta.Crystals); Assert.AreEqual("broken", File.ReadAllText(previous)); Assert.AreEqual(good, File.ReadAllText(previous + ".bak"));
        }
        [Test] public void CorruptNewSaveNeverRollsBackToPrevious()
        {
            File.WriteAllText(previous, AsV2(new CampaignProgress())); File.WriteAllText(current, "broken-new");
            Assert.Throws<InvalidDataException>(() => { string w; CampaignSaveStore.Load(current, previous, null, out w); });
            Assert.AreEqual("broken-new", File.ReadAllText(current));
        }
        [TestCase(1, "bokumyaku-campaign-v2")]
        [TestCase(3, "bokumyaku-campaign-v2")]
        [TestCase(2, "unknown")]
        public void PreviousHeaderMustBeExactlyV2(int schema, string config)
        {
            var c = new CampaignProgress { SchemaVersion = schema, Config = config };
            File.WriteAllText(previous, JsonUtility.ToJson(c));
            Assert.Throws<InvalidDataException>(() => { string w; CampaignSaveStore.Load(current, previous, null, out w); }); Assert.IsFalse(File.Exists(current));
        }
        [Test] public void ExistingNewProgressWinsOverPrevious()
        {
            CampaignSaveStore.Save(current, new CampaignProgress { Meta = new MetaProgress { Crystals = 123 } });
            File.WriteAllText(previous, AsV2(new CampaignProgress { Meta = new MetaProgress { Crystals = 900 } }));
            string warning; Assert.AreEqual(123, CampaignSaveStore.Load(current, previous, null, out warning).Meta.Crystals);
        }
        [Test] public void OldActiveSnapshotKeepsIdentityAndFutureAfterMigration()
        {
            var oldSnapshot = LegacySnapshot();
            var uninterrupted = RunSimulator.Restore(oldSnapshot);
            var c = new CampaignProgress { HasActiveRun = true, ActiveRunId = Guid.NewGuid().ToString("N"), ActiveRun = oldSnapshot, ActiveSpeed = 2 };
            var bytes = AsV2(c); File.WriteAllText(previous, bytes);
            string warning; var migrated = CampaignSaveStore.Load(current, previous, null, out warning);
            Assert.AreEqual(c.ActiveRunId, migrated.ActiveRunId); Assert.AreEqual(2, migrated.ActiveSpeed);
            var resumed = RunSimulator.Restore(migrated.ActiveRun);
            for (int i = 0; i < 900; i++) { uninterrupted.Tick(); resumed.Tick(); }
            Assert.AreEqual(JsonUtility.ToJson(uninterrupted.CaptureState()), JsonUtility.ToJson(resumed.CaptureState()));
            Assert.AreEqual(JsonUtility.ToJson(RunSimulator.Restore(LegacySnapshot("v5-run-tick2700.json")).CaptureState()), JsonUtility.ToJson(resumed.CaptureState()));
            migrated = CampaignSaveStore.SaveActive(current, migrated, resumed, 2);
            var loaded = CampaignSaveStore.Load(current, out warning);
            Assert.AreEqual(JsonUtility.ToJson(resumed.CaptureState()), JsonUtility.ToJson(RunSimulator.Restore(loaded.ActiveRun).CaptureState()));
            Assert.AreEqual(bytes, File.ReadAllText(previous));
        }
        [Test] public void ActiveRunRejectsDifferentRulesWithoutChangingSave()
        {
            var oldSnapshot = LegacySnapshot();
            var c = new CampaignProgress { HasActiveRun = true, ActiveRunId = Guid.NewGuid().ToString("N"), ActiveRun = oldSnapshot };
            c = CampaignSaveStore.Save(current, c); var bytes = File.ReadAllText(current);
            var newRulesRun = new RunSimulator(42); for (int i = 0; i < oldSnapshot.tickCount; i++) newRulesRun.Tick();
            Assert.AreEqual(oldSnapshot.seed, newRulesRun.Seed); Assert.AreEqual(oldSnapshot.tickCount, newRulesRun.TickCount);
            Assert.Throws<InvalidDataException>(() => CampaignSaveStore.SaveActive(current, c, newRulesRun));
            Assert.AreEqual(bytes, File.ReadAllText(current));
        }
        [TestCase(0)]
        [TestCase(99)]
        public void NewCampaignRejectsMissingOrUnknownResultRules(int rules)
        {
            var id = Guid.NewGuid().ToString("N");
            var result = new CampaignResult { RunId = id, RulesVersion = rules, Status = RunStatus.Defeated };
            var c = new CampaignProgress { TotalRuns = 1, LastSettledRunId = id, LastResult = result };
            c.History.Add(result);
            Assert.Throws<InvalidDataException>(() => CampaignSaveStore.Save(current, c)); Assert.IsFalse(File.Exists(current));
        }
        [Test] public void PreviousCampaignDoesNotNormalizeUnknownRules()
        {
            var id = Guid.NewGuid().ToString("N");
            var result = new CampaignResult { RunId = id, RulesVersion = 99, Status = RunStatus.Defeated };
            var c = new CampaignProgress { TotalRuns = 1, LastSettledRunId = id, LastResult = result };
            c.History.Add(result); var bytes = AsV2(c); File.WriteAllText(previous, bytes);
            Assert.Throws<InvalidDataException>(() => { string w; CampaignSaveStore.Load(current, previous, null, out w); });
            Assert.IsFalse(File.Exists(current)); Assert.AreEqual(bytes, File.ReadAllText(previous));
        }
        [Test] public void NewRulesSnapshotCannotMasqueradeAsPreviousCampaign()
        {
            var c = new CampaignProgress { HasActiveRun = true, ActiveRunId = Guid.NewGuid().ToString("N"), ActiveRun = new RunSimulator(42).CaptureState() };
            var bytes = AsV2(c); File.WriteAllText(previous, bytes);
            Assert.Throws<InvalidDataException>(() => { string w; CampaignSaveStore.Load(current, previous, null, out w); });
            Assert.IsFalse(File.Exists(current)); Assert.AreEqual(bytes, File.ReadAllText(previous));
        }
        [Test] public void FailedMigrationWriteDoesNotModifyPrevious()
        {
            var bytes = AsV2(new CampaignProgress()); File.WriteAllText(previous, bytes);
            Assert.Catch<IOException>(() => { string w; CampaignSaveStore.Load(Path.Combine(previous, "child.json"), previous, null, out w); });
            Assert.AreEqual(bytes, File.ReadAllText(previous));
        }
    }
}
