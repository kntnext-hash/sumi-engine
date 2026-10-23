using System;
using System.IO;
using Bokumyaku.Persistence;
using Bokumyaku.Simulation;
using NUnit.Framework;
using UnityEngine;
namespace Bokumyaku.Tests
{
    public sealed class ResearchPricingPersistenceTests
    {
        string dir,path,old;
        [SetUp] public void Setup(){dir=Path.Combine(Path.GetTempPath(),"BokumyakuResearchPrice-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);path=Path.Combine(dir,"v8.json");old=Path.Combine(dir,"v7.json");}
        [TearDown] public void Cleanup(){foreach(var f in Directory.GetFiles(dir))File.Delete(f);Directory.Delete(dir);}
        static RunSnapshot Fixture(string name)=>JsonUtility.FromJson<RunSnapshot>(File.ReadAllText(Path.Combine(Application.dataPath,"BokumyakuPrototype/Tests/Fixtures",name)));
        static string Canonical(CampaignProgress c){var active=c.HasActiveRun?RunSimulator.Restore(c.ActiveRun).CaptureState():null;var copy=JsonUtility.FromJson<CampaignProgress>(JsonUtility.ToJson(c));if(c.HasActiveRun)copy.ActiveRun=active;return JsonUtility.ToJson(copy);}
        [TestCase("v10-point")] [TestCase("v10-enso")] public void PriceMigrationDoesNotRefundOrChangeProgressHistoryOrActiveFuture(string prefix)
        {
            var s=Fixture(prefix+"-tick6000.json");Assert.AreEqual(6,s.rulesVersion);Assert.AreEqual(21,s.meta.Damage);var id=Guid.NewGuid().ToString("N");
            var result=new CampaignResult { RunId=id,RulesVersion=5,Status=RunStatus.Defeated,CompletedWave=10,Ticks=3601,Crystals=12 };
            var c=new CampaignProgress { Meta=s.meta,HasActiveRun=true,ActiveRun=s,ActiveRunId=Guid.NewGuid().ToString("N"),ActiveSpeed=2,BestWave=10,BestWaveByTier=new[] { 10,0,0 },TotalRuns=1,TotalCrystals=12,LastSettledRunId=id,LastResult=result };
            c.History.Add(result);c.Slots[1].Name="preserved";
            c=CampaignSaveStore.Save(old,c);c=CampaignSaveStore.Save(old,c);var bytes=File.ReadAllBytes(old);var backup=File.ReadAllBytes(old+".bak");var expected=Canonical(c);string w;
            var migrated=CampaignSaveStore.LoadLatest(path,old,null,null,null,null,null,null,out w);
            Assert.AreEqual(expected,Canonical(migrated));Assert.AreEqual(c.ActiveRun.exactDoubleBits,migrated.ActiveRun.exactDoubleBits);
            var live=RunSimulator.Restore(migrated.ActiveRun);for(int i=0;i<900;i++)live.Tick();
            Assert.AreEqual(JsonUtility.ToJson(RunSimulator.Restore(Fixture(prefix+"-tick6900.json")).CaptureState()),JsonUtility.ToJson(live.CaptureState()));
            migrated=CampaignSaveStore.SaveActive(path,migrated,live,2);
            while(live.State==RunStatus.Running){live.Tick();Assert.LessOrEqual(live.TickCount,live.RunTickLimit);}
            Assert.AreEqual(JsonUtility.ToJson(RunSimulator.Restore(Fixture(prefix+"-terminal.json")).CaptureState()),JsonUtility.ToJson(live.CaptureState()));
            CampaignSaveStore.SettleRun(path,migrated,migrated.ActiveRunId,live);CollectionAssert.AreEqual(bytes,File.ReadAllBytes(old));CollectionAssert.AreEqual(backup,File.ReadAllBytes(old+".bak"));
        }
        [Test] public void PurchasedLevelsAndRemainingCrystalsAreCopiedWithoutRepricing()
        {
            var c=CampaignSaveStore.Save(old,new CampaignProgress { Meta=new MetaProgress { Damage=35,Supply=18,Growth=17,Harvest=20,Crystals=12345 } });var bytes=File.ReadAllBytes(old);string w;
            var migrated=CampaignSaveStore.LoadLatest(path,old,null,null,null,null,null,null,out w);
            Assert.AreEqual(JsonUtility.ToJson(c),JsonUtility.ToJson(migrated));Assert.AreEqual(12345,migrated.Meta.Crystals);CollectionAssert.AreEqual(bytes,File.ReadAllBytes(old));
        }
        [Test] public void PriceChangeDoesNotPermitActiveResearchOrMutateBalanceOnFailure()
        {
            var funded=new MetaProgress { Damage=20 }; funded.Crystals=funded.Cost(MetaUpgrade.Damage);
            var c=new CampaignProgress { Meta=funded };c=CampaignSaveStore.BeginRun(path,c,new RunSimulator(42,c.Meta));
            var bought=JsonUtility.FromJson<MetaProgress>(JsonUtility.ToJson(c.Meta));Assert.IsTrue(bought.TryBuy(MetaUpgrade.Damage));var bytes=File.ReadAllBytes(path);var expected=Canonical(c);
            Assert.Throws<InvalidDataException>(()=>CampaignSaveStore.SaveMeta(path,c,bought));CollectionAssert.AreEqual(bytes,File.ReadAllBytes(path));Assert.AreEqual(expected,Canonical(c));
        }
        [Test] public void CorruptLatestNeverRollsBackAndAliasRejects()
        {
            CampaignSaveStore.Save(old,new CampaignProgress());File.WriteAllText(path,"broken-new");
            Assert.Throws<InvalidDataException>(()=>{string w;CampaignSaveStore.LoadLatest(path,old,null,null,null,null,null,null,out w);});Assert.AreEqual("broken-new",File.ReadAllText(path));
            Assert.Throws<InvalidDataException>(()=>{string w;CampaignSaveStore.LoadLatest(path,path,null,null,null,null,null,null,out w);});
        }
        [Test] public void MissingV7FallsBackToV6WithoutChangingItsBytes()
        {
            var v6=Path.Combine(dir,"v6.json");CampaignSaveStore.Save(v6,new CampaignProgress { Meta=new MetaProgress { Crystals=63 } });var bytes=File.ReadAllBytes(v6);string w;
            var c=CampaignSaveStore.LoadLatest(path,old,v6,null,null,null,null,null,out w);Assert.AreEqual(63,c.Meta.Crystals);CollectionAssert.AreEqual(bytes,File.ReadAllBytes(v6));Assert.IsFalse(File.Exists(old));
        }
    }
}
