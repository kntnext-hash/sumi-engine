using System.IO;
using Bokumyaku.Simulation;
using NUnit.Framework;
using UnityEngine;
namespace Bokumyaku.Tests
{
    public class FirstWeekRulesTests
    {
        static RunSnapshot Fixture(string suffix) => JsonUtility.FromJson<RunSnapshot>(File.ReadAllText(Path.Combine(Application.dataPath,"BokumyakuPrototype/Tests/Fixtures/rules8-tier2-research-"+suffix+".json")));
        [Test] public void ActualRulesEightAssemblyRetainsExactResearchBattleFuture()
        {
            var run=RunSimulator.Restore(Fixture("tick1800"));
            Assert.AreEqual(8,run.RulesVersion);Assert.AreEqual(2,run.Tier);Assert.AreEqual(20,run.CaptureState().meta.Damage);
            for(int i=0;i<900;i++)run.Tick();
            var expected=RunSimulator.Restore(Fixture("tick2700"));
            Assert.AreEqual(JsonUtility.ToJson(expected.CaptureState()),JsonUtility.ToJson(run.CaptureState()));
        }
    }
}
