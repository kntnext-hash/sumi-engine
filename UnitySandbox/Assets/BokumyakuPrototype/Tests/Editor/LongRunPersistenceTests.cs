using System;
using System.IO;
using Bokumyaku.Persistence;
using Bokumyaku.Simulation;
using NUnit.Framework;
using UnityEngine;
namespace Bokumyaku.Tests
{
    public sealed class LongRunPersistenceTests
    {
        string dir,path,old;
        [SetUp] public void Setup(){dir=Path.Combine(Path.GetTempPath(),"BokumyakuLongSave-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);path=Path.Combine(dir,"v6.json");old=Path.Combine(dir,"v5.json");}
        [TearDown] public void Cleanup(){foreach(var f in Directory.GetFiles(dir))File.Delete(f);Directory.Delete(dir);}
        static RunSnapshot Fixture(string name)=>JsonUtility.FromJson<RunSnapshot>(File.ReadAllText(Path.Combine(Application.dataPath,"BokumyakuPrototype/Tests/Fixtures",name)));
        // Structural serialization boundary fixtures, not claims of naturally reaching these waves.
        static CampaignProgress Result(int rules,int wave,int ticks,int reward)
        {
            var r=new CampaignResult { RunId=Guid.NewGuid().ToString("N"),RulesVersion=rules,CompletedWave=wave,Ticks=ticks,Crystals=reward,Status=RunStatus.TestCompleted };
            var c=new CampaignProgress { TotalRuns=1,LastSettledRunId=r.RunId,LastResult=r,BestWave=wave,BestWaveByTier=new[] { wave,0,0 },TotalCrystals=reward,Meta=new MetaProgress { Crystals=reward } };c.History.Add(r);return c;
        }
        [TestCase(30000)] [TestCase(100000)]
        public void LongResultRoundTripsLargeWaveAndReward(int wave)
        {
            var c=Result(5,wave,wave*360,12000000);c=CampaignSaveStore.Save(path,c);string warning;
            var read=CampaignSaveStore.Load(path,out warning);Assert.AreEqual(wave,read.BestWave);Assert.AreEqual(12000000,read.LastResult.Crystals);Assert.AreEqual(wave*360,read.LastResult.Ticks);
        }
        [TestCase(5,100001,36000000,1)] [TestCase(5,100000,36000001,1)] [TestCase(5,100000,36000000,12000001)]
        [TestCase(4,61,21600,1)] [TestCase(4,60,21601,1)] [TestCase(4,60,21600,80001)] [TestCase(9,60,21600,1)]
        public void UnsupportedResultBoundsFailBeforeWrite(int rules,int wave,int ticks,int reward)
        {Assert.Throws<InvalidDataException>(()=>CampaignSaveStore.Save(path,Result(rules,wave,ticks,reward)));Assert.IsFalse(File.Exists(path));}
        [Test] public void OldRulesStillEndAtSixtyAfterFileGenerationMigration()
        {
            var s=Fixture("v8-enso-tick21599.json");Assert.AreEqual(4,s.rulesVersion);
            var c=new CampaignProgress { HasActiveRun=true,ActiveRunId=Guid.NewGuid().ToString("N"),ActiveRun=s,Meta=s.meta };
            c=CampaignSaveStore.Save(old,c);c=CampaignSaveStore.Save(old,c);var original=File.ReadAllBytes(old);var backup=File.ReadAllBytes(old+".bak");string warning;
            var next=CampaignSaveStore.LoadLatest(path,old,null,null,null,null,out warning);var live=RunSimulator.Restore(next.ActiveRun);live.Tick();
            Assert.AreEqual(RunStatus.TestCompleted,live.State);Assert.AreEqual(21600,live.TickCount);Assert.AreEqual(60,live.CompletedWave);
            next=CampaignSaveStore.SettleRun(path,next,next.ActiveRunId,live);Assert.AreEqual(4,next.LastResult.RulesVersion);
            CollectionAssert.AreEqual(original,File.ReadAllBytes(old));CollectionAssert.AreEqual(backup,File.ReadAllBytes(old+".bak"));
        }
        [TestCase(false)] [TestCase(true)]
        public void SettlementOverflowLeavesDiskAndMemoryUnchanged(bool cumulativeOverflow)
        {
            var s=Fixture("v8-point-terminal.json");Assert.Greater(s.crystalsReward,0);
            if(!cumulativeOverflow)s.meta.Crystals=int.MaxValue;
            var live=RunSimulator.Restore(s);var c=new CampaignProgress { HasActiveRun=true,ActiveRunId=Guid.NewGuid().ToString("N"),ActiveRun=s,Meta=s.meta,TotalCrystals=cumulativeOverflow?long.MaxValue:0 };
            c=CampaignSaveStore.Save(path,c);var bytes=File.ReadAllBytes(path);var before=JsonUtility.ToJson(c);
            Assert.Throws<OverflowException>(()=>CampaignSaveStore.SettleRun(path,c,c.ActiveRunId,live));
            CollectionAssert.AreEqual(bytes,File.ReadAllBytes(path));Assert.AreEqual(before,JsonUtility.ToJson(c));Assert.IsFalse(live.CaptureState().rewardClaimed);Assert.IsFalse(File.Exists(path+".bak"));
        }
        [Test] public void BrokenLatestNeverFallsBackAndSamePathIsRejected()
        {
            CampaignSaveStore.Save(old,new CampaignProgress());File.WriteAllText(path,"broken");
            Assert.Throws<InvalidDataException>(()=>{string w;CampaignSaveStore.LoadLatest(path,old,null,null,null,null,out w);});Assert.AreEqual("broken",File.ReadAllText(path));
            Assert.Throws<InvalidDataException>(()=>{string w;CampaignSaveStore.LoadLatest(path,path,null,null,null,null,out w);});
        }
        [TestCase("v8-point-tick6000.json")] [TestCase("v8-enso-tick21599.json")]
        public void RepeatedLoadSaveKeepsExactActiveStateWithoutRelaxingValidation(string fixture)
        {
            var s=Fixture(fixture);var exact=JsonUtility.ToJson(RunSimulator.Restore(s).CaptureState());
            var c=new CampaignProgress { HasActiveRun=true,ActiveRunId=Guid.NewGuid().ToString("N"),ActiveRun=s,Meta=s.meta };
            c=CampaignSaveStore.Save(path,c);
            for(int i=0;i<8;i++)
            {
                string warning;c=CampaignSaveStore.Load(path,out warning);
                Assert.AreEqual(exact,JsonUtility.ToJson(RunSimulator.Restore(c.ActiveRun).CaptureState()));
                c=CampaignSaveStore.Save(path,c);
            }
            var bytes=File.ReadAllBytes(path);c.ActiveRun.enemies[0].x+=1;
            Assert.Throws<InvalidDataException>(()=>CampaignSaveStore.Save(path,c));
            CollectionAssert.AreEqual(bytes,File.ReadAllBytes(path));
        }
        [Test] public void MissingV5FallsBackToExistingV4Migration()
        {
            var v4=Path.Combine(dir,"v4.json");CampaignSaveStore.Save(v4,new CampaignProgress { Meta=new MetaProgress { Crystals=37 } });var bytes=File.ReadAllBytes(v4);
            string w;var c=CampaignSaveStore.LoadLatest(path,old,v4,null,null,null,out w);Assert.AreEqual(37,c.Meta.Crystals);CollectionAssert.AreEqual(bytes,File.ReadAllBytes(v4));Assert.IsFalse(File.Exists(old));
        }
    }
}
