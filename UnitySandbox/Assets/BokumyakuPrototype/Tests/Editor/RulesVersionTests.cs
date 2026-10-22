using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using Bokumyaku.Simulation;
using NUnit.Framework;
using UnityEngine;
namespace Bokumyaku.Tests
{
    public class RulesVersionTests
    {
        static RunSnapshot Fixture(string name) { return JsonUtility.FromJson<RunSnapshot>(File.ReadAllText(Path.Combine(Application.dataPath,"BokumyakuPrototype/Tests/Fixtures/"+name+".json"))); }
        static string State(RunSimulator r) { return JsonUtility.ToJson(r.CaptureState()); }
        [Test] public void ActualV5AssemblyFixtureRetainsExactFuture()
        {
            var r=RunSimulator.Restore(Fixture("v5-run-tick1800"));
            Assert.AreEqual(1,r.RulesVersion); Assert.AreEqual(6,r.UpgradeLevels.Length);
            for(int i=0;i<900;i++)r.Tick(); Assert.AreEqual(State(RunSimulator.Restore(Fixture("v5-run-tick2700"))),State(r));
            while(r.State==RunStatus.Running)r.Tick();
            Assert.AreEqual(State(RunSimulator.Restore(Fixture("v5-run-terminal"))),State(r));
        }
        [Test] public void LegacyCannotPurchaseHarvestOrUpgradeItsRules()
        {
            var r=RunSimulator.Restore(Fixture("v5-run-tick1800"));r.SetBuild(BuildPreset.Record);
            Assert.AreEqual(1,r.RulesVersion);Assert.IsFalse(r.TryBuy(UpgradeKind.Harvest));
            Assert.Throws<ArgumentOutOfRangeException>(()=>r.UpgradeCost(UpgradeKind.Harvest));
            Assert.AreEqual(2,r.CaptureState().schemaVersion);Assert.AreEqual(1,r.CaptureState().rulesVersion);
        }
        [TestCase("v6-run")]
        [TestCase("v6-record")]
        public void ActualV6AssemblyFixtureRetainsExactFuture(string prefix)
        {
            var r=RunSimulator.Restore(Fixture(prefix+"-tick6000"));
            Assert.AreEqual(2,r.RulesVersion);Assert.AreEqual(7,r.UpgradeLevels.Length);
            for(int i=0;i<900;i++)r.Tick();
            Assert.AreEqual(State(RunSimulator.Restore(Fixture(prefix+"-tick6900"))),State(r));
            while(r.State==RunStatus.Running)r.Tick();
            Assert.AreEqual(State(RunSimulator.Restore(Fixture(prefix+"-terminal"))),State(r));
        }
        [Test] public void SwitchingBuildDoesNotUpgradeV6Rules()
        {
            var r=RunSimulator.Restore(Fixture("v6-run-tick6000"));r.SetBuild(BuildPreset.Record);
            Assert.AreEqual(2,r.RulesVersion);Assert.AreEqual(2,RunSimulator.Restore(r.CaptureState()).RulesVersion);
            Assert.AreEqual(6,new RunSimulator(42).RulesVersion);
        }
        [TestCase(BuildPreset.Farming)] [TestCase(BuildPreset.Record)]
        public void NewRulesJsonRoundTripPreservesPurchaseCursorAndFuture(BuildPreset build)
        {
            var a=new RunSimulator(42);a.SetBuild(build);for(int i=0;i<6000;i++)a.Tick();
            var b=RunSimulator.Restore(JsonUtility.FromJson<RunSnapshot>(State(a)));
            Assert.AreEqual(6,b.RulesVersion);Assert.AreEqual(7,b.UpgradeLevels.Length);
            for(int i=0;i<900;i++){a.Tick();b.Tick();Assert.AreEqual(State(a),State(b));}
        }
        [Test] public void HarvestPurchaseIsPaidCappedAndNeverRetroactive()
        {
            var r=new RunSimulator(1);typeof(RunSimulator).GetProperty("RunInk").SetValue(r,100000);
            typeof(RunSimulator).GetField("rewardPoints",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(r,20d);
            for(int i=0;i<RunSimulator.HarvestMaxLevel;i++) {int before=r.RunInk,cost=r.UpgradeCost(UpgradeKind.Harvest);Assert.IsTrue(r.TryBuy(UpgradeKind.Harvest));Assert.AreEqual(before-cost,r.RunInk);Assert.AreEqual(20,r.RewardPoints);}
            Assert.IsFalse(r.TryBuy(UpgradeKind.Harvest));Assert.AreEqual(1+RunSimulator.HarvestMaxLevel*RunSimulator.HarvestGainPerLevel,r.HarvestMultiplier);
        }
        [Test] public void InvalidRulesAndLevelShapeAreRejected()
        {
            var s=new RunSimulator(1).CaptureState();s.rulesVersion=7;Assert.Throws<ArgumentException>(()=>RunSimulator.Restore(s));
            s=new RunSimulator(1).CaptureState();s.rulesVersion=1;Assert.Throws<ArgumentException>(()=>RunSimulator.Restore(s));
            s=new RunSimulator(1).CaptureState();s.autoPurchaseCursor=-1;Assert.Throws<ArgumentException>(()=>RunSimulator.Restore(s));
        }
        [Test] public void HarvestMultipliesOnlyFutureEligibleKillsNotInkOrChildren()
        {
            var r=new RunSimulator(1);typeof(RunSimulator).GetProperty("RunInk").SetValue(r,100000);
            var enemies=(List<EnemyState>)typeof(RunSimulator).GetField("enemies",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(r);
            var hurt=typeof(RunSimulator).GetMethod("Hurt",BindingFlags.Instance|BindingFlags.NonPublic);
            Action<bool> kill=child=>{var e=new EnemyState();foreach(var pair in new Dictionary<string,object>{{"Id",1},{"Kind",child?EnemyKind.Splitter:EnemyKind.Normal},{"Hp",1d},{"MaxHp",1d},{"SpawnWave",1},{"IsSplitChild",child}})typeof(EnemyState).GetProperty(pair.Key).SetValue(e,pair.Value);enemies.Add(e);hurt.Invoke(r,new object[]{e,1d});};
            kill(false);double before=r.RewardPoints;int income=r.IncomeEarned;
            Assert.IsTrue(r.TryBuy(UpgradeKind.Harvest));Assert.AreEqual(before,r.RewardPoints);
            kill(false);Assert.AreEqual(before+1+RunSimulator.HarvestGainPerLevel,r.RewardPoints);Assert.AreEqual(income*2,r.IncomeEarned);
            before=r.RewardPoints;income=r.IncomeEarned;kill(true);Assert.AreEqual(before,r.RewardPoints);Assert.AreEqual(income,r.IncomeEarned);
        }
        [Test] public void FullyCappedPurchaseLoopKeepsRestorableBoundedCursor()
        {
            var r=new RunSimulator(1);r.SetBuild(BuildPreset.Record);
            typeof(RunSimulator).GetField("levels",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(r,new[]{30,25,25,9,10,10,10});
            var buy=typeof(RunSimulator).GetMethod("AutoBuy",BindingFlags.Instance|BindingFlags.NonPublic);
            for(int i=0;i<RunSimulator.MaxTicks;i++)buy.Invoke(r,null);
            var s=r.CaptureState();Assert.That(s.autoPurchaseCursor,Is.InRange(0,5));
            Assert.AreEqual(State(r),State(RunSimulator.Restore(s)));
        }
    }
}
