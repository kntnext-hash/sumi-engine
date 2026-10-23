using System;
using System.IO;
using Bokumyaku.Persistence;
using Bokumyaku.Simulation;
using NUnit.Framework;
using UnityEngine;
namespace Bokumyaku.Tests
{
    public sealed class ProgressionPersistenceTests
    {
        string dir,path,old;
        [SetUp] public void Setup(){dir=Path.Combine(Path.GetTempPath(),"BokumyakuProgression-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);path=Path.Combine(dir,"v7.json");old=Path.Combine(dir,"v6.json");}
        [TearDown] public void Cleanup(){foreach(var f in Directory.GetFiles(dir))File.Delete(f);Directory.Delete(dir);}
        static RunSnapshot Fixture(string name)=>JsonUtility.FromJson<RunSnapshot>(File.ReadAllText(Path.Combine(Application.dataPath,"BokumyakuPrototype/Tests/Fixtures",name)));
        [TestCase(21)] [TestCase(100)]
        public void ExpandedDamageSavesAndStartsNewRun(int damage)
        {
            var c=CampaignSaveStore.Save(path,new CampaignProgress { Meta=new MetaProgress { Damage=damage } });string w;c=CampaignSaveStore.Load(path,out w);
            Assert.AreEqual(damage,c.Meta.Damage);var live=new RunSimulator(42,c.Meta);Assert.AreEqual(9,live.RulesVersion);
            c=CampaignSaveStore.BeginRun(path,c,live);Assert.AreEqual(damage,c.ActiveRun.meta.Damage);
        }
        [Test] public void Damage101RejectsWithoutChangingExistingSave()
        {
            var c=CampaignSaveStore.Save(path,new CampaignProgress());var bytes=File.ReadAllBytes(path);
            Assert.Throws<InvalidDataException>(()=>CampaignSaveStore.SaveMeta(path,c,new MetaProgress { Damage=101 }));
            Assert.AreEqual(0,c.Meta.Damage);CollectionAssert.AreEqual(bytes,File.ReadAllBytes(path));
        }
        [TestCase("v9-point")] [TestCase("v9-enso")]
        public void PreviousRules5MigrationPreservesExactFutureTerminalAndOriginal(string prefix)
        {
            var s=Fixture(prefix+"-tick6000.json");Assert.AreEqual(5,s.rulesVersion);
            var c=new CampaignProgress { HasActiveRun=true,ActiveRunId=Guid.NewGuid().ToString("N"),ActiveRun=s,Meta=s.meta };
            c=CampaignSaveStore.Save(old,c);c=CampaignSaveStore.Save(old,c);var bytes=File.ReadAllBytes(old);var backup=File.ReadAllBytes(old+".bak");string w;
            var migrated=CampaignSaveStore.LoadLatest(path,old,null,null,null,null,null,out w);
            Assert.AreEqual(c.ActiveRunId,migrated.ActiveRunId);Assert.AreEqual(c.Revision,migrated.Revision);Assert.AreEqual(c.ActiveRun.exactDoubleBits,migrated.ActiveRun.exactDoubleBits);
            var live=RunSimulator.Restore(migrated.ActiveRun);for(int i=0;i<900;i++)live.Tick();
            Assert.AreEqual(JsonUtility.ToJson(RunSimulator.Restore(Fixture(prefix+"-tick6900.json")).CaptureState()),JsonUtility.ToJson(live.CaptureState()));
            while(live.State==RunStatus.Running){live.Tick();Assert.LessOrEqual(live.TickCount,live.RunTickLimit);}
            Assert.AreEqual(JsonUtility.ToJson(RunSimulator.Restore(Fixture(prefix+"-terminal.json")).CaptureState()),JsonUtility.ToJson(live.CaptureState()));
            CampaignSaveStore.SettleRun(path,migrated,migrated.ActiveRunId,live);
            CollectionAssert.AreEqual(bytes,File.ReadAllBytes(old));CollectionAssert.AreEqual(backup,File.ReadAllBytes(old+".bak"));
        }
        [Test] public void OldRules5CannotImportExpandedMeta()
        {
            var s=Fixture("v9-point-tick6000.json");s.meta.Damage=21;
            Assert.Catch<ArgumentException>(()=>RunSimulator.Restore(s));
            var c=new CampaignProgress { HasActiveRun=true,ActiveRunId=Guid.NewGuid().ToString("N"),ActiveRun=s,Meta=s.meta };
            Assert.Throws<InvalidDataException>(()=>CampaignSaveStore.Save(path,c));Assert.IsFalse(File.Exists(path));
        }
        [Test] public void MetaPurchaseIsAppliedToNextRunOnlyAndActiveSaveRejectsChanges()
        {
            var funded=new MetaProgress { Damage=20 }; int startingCrystals=funded.Cost(MetaUpgrade.Damage); funded.Crystals=startingCrystals;
            var c=CampaignSaveStore.Save(path,new CampaignProgress { Meta=funded });
            var oldRun=new RunSimulator(42,c.Meta);c=CampaignSaveStore.BeginRun(path,c,oldRun);
            var bought=JsonUtility.FromJson<MetaProgress>(JsonUtility.ToJson(c.Meta));int cost=bought.Cost(MetaUpgrade.Damage);Assert.IsTrue(bought.TryBuy(MetaUpgrade.Damage));
            var bytes=File.ReadAllBytes(path);Assert.Throws<InvalidDataException>(()=>CampaignSaveStore.SaveMeta(path,c,bought));CollectionAssert.AreEqual(bytes,File.ReadAllBytes(path));
            Assert.AreEqual(20,oldRun.CaptureState().meta.Damage);Assert.AreEqual(startingCrystals,c.Meta.Crystals);
            c=CampaignSaveStore.DiscardRun(path,c);c=CampaignSaveStore.SaveMeta(path,c,bought);
            Assert.AreEqual(21,c.Meta.Damage);Assert.AreEqual(startingCrystals-cost,c.Meta.Crystals);Assert.AreEqual(20,oldRun.CaptureState().meta.Damage);Assert.AreEqual(21,new RunSimulator(43,c.Meta).CaptureState().meta.Damage);
        }
        [Test] public void FailedMetaWriteKeepsBalanceAndExistingData()
        {
            var funded=new MetaProgress { Damage=20 }; int startingCrystals=funded.Cost(MetaUpgrade.Damage); funded.Crystals=startingCrystals;
            var c=CampaignSaveStore.Save(path,new CampaignProgress { Meta=funded });var before=JsonUtility.ToJson(c);var bytes=File.ReadAllBytes(path);
            var bought=JsonUtility.FromJson<MetaProgress>(JsonUtility.ToJson(c.Meta));Assert.IsTrue(bought.TryBuy(MetaUpgrade.Damage));
            // A destination beneath a regular file cannot be created.
            var fresh=new CampaignProgress { Meta=c.Meta };
            Assert.Catch<IOException>(()=>CampaignSaveStore.SaveMeta(Path.Combine(path,"child.json"),fresh,bought));
            Assert.AreEqual(before,JsonUtility.ToJson(c));CollectionAssert.AreEqual(bytes,File.ReadAllBytes(path));
        }
        [Test] public void CorruptNewGenerationDoesNotRollBackAndSamePathRejects()
        {
            CampaignSaveStore.Save(old,new CampaignProgress());File.WriteAllText(path,"broken");
            Assert.Throws<InvalidDataException>(()=>{string w;CampaignSaveStore.LoadLatest(path,old,null,null,null,null,null,out w);});Assert.AreEqual("broken",File.ReadAllText(path));
            Assert.Throws<InvalidDataException>(()=>{string w;CampaignSaveStore.LoadLatest(path,path,null,null,null,null,null,out w);});
        }
    }
}
