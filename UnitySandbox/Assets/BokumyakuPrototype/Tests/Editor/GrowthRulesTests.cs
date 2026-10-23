using System;
using System.IO;
using Bokumyaku.Simulation;
using NUnit.Framework;
using UnityEngine;
namespace Bokumyaku.Tests
{
    public class GrowthRulesTests
    {
        static RunSnapshot Fixture(string name) { return JsonUtility.FromJson<RunSnapshot>(File.ReadAllText(Path.Combine(Application.dataPath,"BokumyakuPrototype/Tests/Fixtures/"+name+".json"))); }
        static string State(RunSimulator r) { return JsonUtility.ToJson(r.CaptureState()); }
        [TestCase("v9-point")] [TestCase("v9-enso")]
        public void ActualRules5DllRetainsExactFutureAndTerminal(string prefix)
        {
            var r=RunSimulator.Restore(Fixture(prefix+"-tick6000"));Assert.AreEqual(5,r.RulesVersion);Assert.AreEqual(30,r.UpgradeMaxLevel(UpgradeKind.Damage));
            for(int i=0;i<900;i++)r.Tick();Assert.AreEqual(State(RunSimulator.Restore(Fixture(prefix+"-tick6900"))),State(r));
            while(r.State==RunStatus.Running)r.Tick();Assert.AreEqual(State(RunSimulator.Restore(Fixture(prefix+"-terminal"))),State(r));
        }
        [Test] public void LegacySnapshotsRejectExpandedMetaAndRunDamage()
        {
            for(int rules=1;rules<=5;rules++)
            {
                var s=new RunSimulator(1).CaptureState();s.rulesVersion=rules;Array.Resize(ref s.spawnedByType,5);Array.Resize(ref s.killedByType,5);Array.Resize(ref s.leakedByType,5);if(rules==1)Array.Resize(ref s.levels,6);
                s.meta.Damage=21;Assert.Throws<ArgumentException>(()=>RunSimulator.Restore(s));
                s.meta.Damage=20;s.levels[0]=31;Assert.Throws<ArgumentException>(()=>RunSimulator.Restore(s));
                s.levels[0]=30;var r=RunSimulator.Restore(s);Assert.AreEqual(30,r.UpgradeMaxLevel(UpgradeKind.Damage));Assert.IsFalse(r.TryBuy(UpgradeKind.Damage));
            }
        }
        [Test] public void PaidRunDamagePurchasesKeepLegacyCostsAndStopAt100()
        {
            var s=new RunSimulator(1).CaptureState();s.runInk=s.incomeEarned=10000000;var r=RunSimulator.Restore(s);
            for(int level=0;level<100;level++)
            {
                int expected=level<=30?(int)Math.Ceiling(10*Math.Pow(1.16,level)*(1+.008*level*level)):(int)Math.Ceiling(RunSimulator.DamageCostAt30*Math.Pow(1+.03*(level-30),2));
                int before=r.RunInk;Assert.AreEqual(expected,r.UpgradeCost(UpgradeKind.Damage));Assert.IsTrue(r.TryBuy(UpgradeKind.Damage));Assert.AreEqual(before-expected,r.RunInk);
            }
            Assert.AreEqual(100,r.UpgradeMaxLevel(UpgradeKind.Damage));Assert.IsFalse(r.TryBuy(UpgradeKind.Damage));Assert.DoesNotThrow(()=>RunSimulator.Restore(r.CaptureState()));
            Assert.Throws<ArgumentOutOfRangeException>(()=>r.UpgradeMaxLevel((UpgradeKind)99));
        }
        [Test] public void ExpandedMetaMultiplierIsContinuousFiniteAndOtherCapsUnchanged()
        {
            Assert.AreEqual(2952,MetaProgress.DamageCostAt20);Assert.AreEqual(7040,RunSimulator.DamageCostAt30);
            double last=0;for(int level=0;level<=100;level++){var m=new MetaProgress{Damage=level};double expected=level<=20?1+.025*level:1.5*Math.Pow(1+.025*(level-20),2);Assert.AreEqual(expected,m.DamageMultiplier);Assert.Greater(m.DamageMultiplier,last);Assert.Greater(m.Cost(MetaUpgrade.Damage),0);last=m.DamageMultiplier;}
            Assert.AreEqual(100,MetaProgress.MaxLevel(MetaUpgrade.Damage));foreach(var kind in new[]{MetaUpgrade.Supply,MetaUpgrade.Growth,MetaUpgrade.Harvest})Assert.AreEqual(20,MetaProgress.MaxLevel(kind));
            Assert.Throws<ArgumentOutOfRangeException>(()=>MetaProgress.MaxLevel((MetaUpgrade)99));Assert.Throws<ArgumentOutOfRangeException>(()=>new RunSimulator(1,new MetaProgress{Damage=101}));
        }
        [Test] public void ExpandedStateJsonRoundTripHasIdenticalFuture()
        {
            var s=new RunSimulator(42,new MetaProgress{Damage=100,Supply=20,Growth=20,Harvest=20}).CaptureState();s.levels[0]=100;
            var a=RunSimulator.Restore(s);a.SetBuild(BuildPreset.Record);for(int i=0;i<900;i++)a.Tick();
            var b=RunSimulator.Restore(JsonUtility.FromJson<RunSnapshot>(State(a)));
            for(int i=0;i<900;i++){a.Tick();b.Tick();Assert.AreEqual(State(a),State(b));}
        }
        [Test] public void ConservativeFullHorizonDamageFitsExplicitBounds()
        {
            double hit=7.5*Math.Pow(1.1,30)*Math.Pow(1.12,70)*13.5*1.65;
            double field=hit*.85*2.2/30;
            double bound=Math.Ceiling(36000000/7.0)*12*96*hit+36000000.0*96*2*field;
            Assert.Less(hit,1e9);Assert.Less(field,1e9);Assert.Less(bound,RunSimulator.LongRunEffectiveDamageLimit);
        }
    }
}
