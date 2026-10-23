using System;
using System.IO;
using Bokumyaku.Persistence;
using Bokumyaku.Simulation;
using NUnit.Framework;
using UnityEngine;
namespace Bokumyaku.Tests
{
    public sealed class Rules4MigrationTests
    {
        string dir, latest, previous, previousV3, older;
        [SetUp] public void Setup() { dir=Path.Combine(Path.GetTempPath(),"BokumyakuRules4-"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(dir);latest=Path.Combine(dir,"v5.json");previous=Path.Combine(dir,"v4.json");previousV3=Path.Combine(dir,"v3.json");older=Path.Combine(dir,"v2.json"); }
        [TearDown] public void Cleanup() { foreach(var f in Directory.GetFiles(dir))File.Delete(f);Directory.Delete(dir); }
        static RunSnapshot Fixture(string name) => JsonUtility.FromJson<RunSnapshot>(File.ReadAllText(Path.Combine(Application.dataPath,"BokumyakuPrototype/Tests/Fixtures",name)));
        static string CanonicalCampaignJson(CampaignProgress value)
        {
            // Compare all campaign fields plus the strictly decoded state, not parser-adjacent doubles.
            // Decode the original once before cloning; never regenerate or relax its payload.
            var active=RunSimulator.Restore(value.ActiveRun).CaptureState();
            var copy=JsonUtility.FromJson<CampaignProgress>(JsonUtility.ToJson(value));
            copy.ActiveRun=active;
            return JsonUtility.ToJson(copy);
        }
        [TestCase("v7-farming")] [TestCase("v7-record")] public void PreviousRules3ActivePreservesFutureAndAllSourceBytes(string prefix)
        {
            var s=Fixture(prefix+"-tick6000.json");Assert.AreEqual(3,s.rulesVersion);
            var c=new CampaignProgress { HasActiveRun=true,ActiveRunId=Guid.NewGuid().ToString("N"),ActiveRun=s,Meta=s.meta,ActiveSpeed=2 };
            c=CampaignSaveStore.Save(previous,c);c=CampaignSaveStore.Save(previous,c);
            var bytes=File.ReadAllBytes(previous);var backup=File.ReadAllBytes(previous+".bak");
            string warning;var migrated=CampaignSaveStore.LoadLatest(latest,previous,previousV3,older,null,out warning);
            Assert.AreEqual(c.ActiveRun.exactDoubleBits,migrated.ActiveRun.exactDoubleBits);
            Assert.AreEqual(CanonicalCampaignJson(c),CanonicalCampaignJson(migrated));Assert.AreEqual(4,migrated.SchemaVersion);Assert.AreEqual("bokumyaku-campaign-v4",migrated.Config);
            var live=RunSimulator.Restore(migrated.ActiveRun);for(int i=0;i<900;i++)live.Tick();
            Assert.AreEqual(JsonUtility.ToJson(RunSimulator.Restore(Fixture(prefix+"-tick6900.json")).CaptureState()),JsonUtility.ToJson(live.CaptureState()));
            CampaignSaveStore.SaveActive(latest,migrated,live,2);
            CollectionAssert.AreEqual(bytes,File.ReadAllBytes(previous));CollectionAssert.AreEqual(backup,File.ReadAllBytes(previous+".bak"));
        }
        [Test] public void CorruptPreviousUsesItsBackupAndPreservesOriginal()
        {
            CampaignSaveStore.Save(previous,new CampaignProgress { Meta=new MetaProgress { Crystals=71 } });File.Copy(previous,previous+".bak");File.WriteAllText(previous,"broken");
            string w;var c=CampaignSaveStore.LoadLatest(latest,previous,previousV3,older,null,out w);Assert.AreEqual(71,c.Meta.Crystals);Assert.AreEqual("broken",File.ReadAllText(previous));Assert.IsTrue(File.Exists(previous+".bak"));
        }
        [Test] public void OldV2FallbackStillMigratesWithoutChangingSource()
        {
            var c=new CampaignProgress { SchemaVersion=2,Config="bokumyaku-campaign-v2",Meta=new MetaProgress { Crystals=39 } };var bytes=JsonUtility.ToJson(c);File.WriteAllText(older,bytes);
            string w;var migrated=CampaignSaveStore.LoadLatest(latest,previous,previousV3,older,null,out w);Assert.AreEqual(39,migrated.Meta.Crystals);Assert.AreEqual(4,migrated.SchemaVersion);Assert.AreEqual(bytes,File.ReadAllText(older));
        }
        [Test] public void BrokenLatestDoesNotRollBackToPrevious()
        {
            CampaignSaveStore.Save(previous,new CampaignProgress());File.WriteAllText(latest,"broken-latest");
            Assert.Throws<InvalidDataException>(()=>{string w;CampaignSaveStore.LoadLatest(latest,previous,previousV3,older,null,out w);});Assert.AreEqual("broken-latest",File.ReadAllText(latest));
        }
        [Test] public void SameSourceAndDestinationIsRejected()
        {
            Assert.Throws<InvalidDataException>(()=>{string w;CampaignSaveStore.LoadLatest(latest,latest,previousV3,older,null,out w);});Assert.IsFalse(File.Exists(latest));
        }
        [Test] public void MissingV4FallsBackToV3WithoutChangingItsBytes()
        {
            CampaignSaveStore.Save(previousV3,new CampaignProgress { Meta=new MetaProgress { Crystals=83 } });var bytes=File.ReadAllBytes(previousV3);
            string w;var c=CampaignSaveStore.LoadLatest(latest,previous,previousV3,older,null,out w);
            Assert.AreEqual(83,c.Meta.Crystals);CollectionAssert.AreEqual(bytes,File.ReadAllBytes(previousV3));Assert.IsFalse(File.Exists(previous));
        }
        [Test] public void CorruptV4DoesNotFallBackToOlderV3()
        {
            CampaignSaveStore.Save(previousV3,new CampaignProgress());File.WriteAllText(previous,"broken-v4");
            Assert.Throws<InvalidDataException>(()=>{string w;CampaignSaveStore.LoadLatest(latest,previous,previousV3,older,null,out w);});Assert.IsFalse(File.Exists(latest));Assert.AreEqual("broken-v4",File.ReadAllText(previous));
        }
        [TestCase(4,true)] [TestCase(7,true)] [TestCase(8,false)]
        public void ResultRulesHaveExplicitSupportedRange(int rules,bool valid)
        {
            var id=Guid.NewGuid().ToString("N");var r=new CampaignResult { RunId=id,RulesVersion=rules,Status=RunStatus.Defeated };
            var c=new CampaignProgress { TotalRuns=1,LastSettledRunId=id,LastResult=r };c.History.Add(r);
            if(valid)Assert.DoesNotThrow(()=>CampaignSaveStore.Save(latest,c));else Assert.Throws<InvalidDataException>(()=>CampaignSaveStore.Save(latest,c));
        }
    }
}
