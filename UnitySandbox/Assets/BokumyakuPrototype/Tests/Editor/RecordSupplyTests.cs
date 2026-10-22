using System;
using System.IO;
using System.Reflection;
using Bokumyaku.Simulation;
using NUnit.Framework;
using UnityEngine;
namespace Bokumyaku.Tests
{
    public class RecordSupplyTests
    {
        static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
        static string State(RunSimulator r)=>JsonUtility.ToJson(r.CaptureState());
        static RunSimulator Fixture(string name)=>RunSimulator.Restore(JsonUtility.FromJson<RunSnapshot>(File.ReadAllText(Path.Combine(Application.dataPath,"BokumyakuPrototype/Tests/Fixtures/"+name+".json"))));
        static RunSimulator Prepared(int supply,int damage,int ink)
        {
            var r=new RunSimulator(42);r.SetBuild(BuildPreset.Record);
            typeof(RunSimulator).GetField("levels",Flags).SetValue(r,new[]{damage,10,supply,6,10,10,0});
            typeof(RunSimulator).GetProperty("RunInk").SetValue(r,ink);
            // Make the regular cycle choose Speed, so priority is observable.
            typeof(RunSimulator).GetField("autoPurchaseCursor",Flags).SetValue(r,1);
            return r;
        }
        static void Buy(RunSimulator r)=>typeof(RunSimulator).GetMethod("AutoBuy",Flags).Invoke(r,null);
        [TestCase("farming")][TestCase("record")]
        public void ActualV7MatureRunPreservesFutureAndTerminal(string build)
        {
            var r=Fixture("v7-"+build+"-tick6000");Assert.AreEqual(3,r.RulesVersion);
            for(int i=0;i<900;i++)r.Tick();Assert.AreEqual(State(Fixture("v7-"+build+"-tick6900")),State(r));
            while(r.State==RunStatus.Running)r.Tick();Assert.AreEqual(State(Fixture("v7-"+build+"-terminal")),State(r));
        }
        [Test] public void LateSupplyIsPaidAndBranchWaitsForIt()
        {
            var r=Prepared(13,7,100000);int cost=r.UpgradeCost(UpgradeKind.Supply);
            Buy(r);Assert.AreEqual(14,r.UpgradeLevels[2]);Assert.AreEqual(6,r.UpgradeLevels[3]);Assert.AreEqual(100000-cost,r.RunInk);
            cost=r.UpgradeCost(UpgradeKind.Branch);int before=r.RunInk;Buy(r);
            Assert.AreEqual(7,r.UpgradeLevels[3]);Assert.AreEqual(before-cost,r.RunInk);
            Assert.AreEqual(1,r.CaptureState().autoPurchaseCursor);
        }
        [Test] public void InsufficientSupplyBudgetCannotBuyCheaperFallback()
        {
            var r=Prepared(13,7,200);Assert.Greater(r.UpgradeCost(UpgradeKind.Supply),r.RunInk);
            var before=State(r);Buy(r);Assert.AreEqual(before,State(r));
        }
        [Test] public void DamageRequirementStillGatesSupplyPriority()
        {
            var r=Prepared(13,6,100000);Buy(r);Assert.AreEqual(13,r.UpgradeLevels[2]);Assert.AreEqual(11,r.UpgradeLevels[1]);
        }
        [Test] public void Rules3BuildSwitchCannotEnableNewPriority()
        {
            var r=Prepared(13,7,100000);typeof(RunSimulator).GetProperty("RulesVersion").SetValue(r,3);
            r.SetBuild(BuildPreset.Farming);r.SetBuild(BuildPreset.Record);Buy(r);
            Assert.AreEqual(3,r.RulesVersion);Assert.AreEqual(13,r.UpgradeLevels[2]);Assert.AreEqual(11,r.UpgradeLevels[1]);
        }
    }
}
