using System;
using Bokumyaku.Simulation;
using NUnit.Framework;
using UnityEngine;
namespace Bokumyaku.Tests
{
    public class TierSimulationTests
    {
        static string State(RunSimulator r) => JsonUtility.ToJson(r.CaptureState());
        [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void TierRoundTripPreservesExactFutureAndReward(int tier)
        {
            var a = new RunSimulator(42, tier: tier); a.SetBuild(BuildPreset.Farming);
            for (int i=0;i<900;i++) a.Tick();
            var b = RunSimulator.Restore(JsonUtility.FromJson<RunSnapshot>(State(a)));
            Assert.AreEqual(tier,b.Tier); Assert.AreEqual(3,b.CaptureState().schemaVersion);
            for(int i=0;i<30000 && a.State==RunStatus.Running;i++) { a.Tick(); b.Tick(); }
            Assert.AreEqual(RunStatus.Defeated,a.State);
            Assert.AreEqual(State(a),State(b));
            Assert.AreEqual((int)Math.Floor(a.RewardPoints*.08*TierBalance.Get(tier).CrystalMultiplier),a.CrystalsReward);
            Assert.AreEqual(State(a),State(RunSimulator.Restore(a.CaptureState())));
        }
        [Test] public void TierOneRetainsRulesSixExactBattleAndIncome()
        {
            var a=new RunSimulator(73); a.SetBuild(BuildPreset.Farming);
            var old=a.CaptureState(); old.schemaVersion=2; old.rulesVersion=6; old.tier=0;
            var b=RunSimulator.Restore(old);
            Assert.AreEqual(1,b.Tier); Assert.AreEqual(6,b.RulesVersion);
            for(int i=0;i<30000 && a.State==RunStatus.Running;i++) { a.Tick(); b.Tick(); }
            var sa=a.CaptureState(); var sb=b.CaptureState(); sb.rulesVersion=7;
            Assert.AreEqual(JsonUtility.ToJson(sa),JsonUtility.ToJson(sb));
        }
        [Test] public void HigherTierScalesFirstEnemyHpWithoutChangingInk()
        {
            var a=new RunSimulator(42); var b=new RunSimulator(42,tier:3);
            for(int i=0;i<90 && a.Enemies.Count==0;i++) { a.Tick(); b.Tick(); }
            Assert.Greater(a.Enemies.Count,0);
            Assert.AreEqual(a.Enemies[0].MaxHp*6,b.Enemies[0].MaxHp);
            Assert.AreEqual(a.RunInk,b.RunInk);
        }
        [Test] public void TierThreeLongRunHpRoundTripsBeyondLegacyBound()
        {
            var r=new RunSimulator(42,tier:3);
            for(int i=0;i<90 && r.Enemies.Count==0;i++) r.Tick();
            double hp=TierBalance.EnemyBaseHp(3,RunSimulator.LongRunWaveLimit)*12;
            Assert.Greater(hp,1e12);
            var e=r.Enemies[0];
            typeof(EnemyState).GetProperty("Hp").SetValue(e,hp);
            typeof(EnemyState).GetProperty("MaxHp").SetValue(e,hp);
            Assert.AreEqual(hp,RunSimulator.Restore(r.CaptureState()).Enemies[0].Hp);
            var old=r.CaptureState(); old.tier=1; old.rulesVersion=6;
            Assert.Throws<ArgumentException>(()=>RunSimulator.Restore(old));
        }
        [TestCase(0)] [TestCase(-1)] [TestCase(4)]
        public void InvalidTierRejected(int tier)
        {
            Assert.Throws<ArgumentOutOfRangeException>(()=>new RunSimulator(42,tier:tier));
            var s=new RunSimulator(42).CaptureState(); s.tier=tier;
            Assert.Throws<ArgumentException>(()=>RunSimulator.Restore(s));
        }
        [Test] public void LegacyRulesCannotEnterHigherTierOrNewRulesUseOldSchema()
        {
            var s=new RunSimulator(42,tier:2).CaptureState(); s.rulesVersion=6;
            Assert.Throws<ArgumentException>(()=>RunSimulator.Restore(s));
            s=new RunSimulator(42).CaptureState(); s.schemaVersion=2;
            Assert.Throws<ArgumentException>(()=>RunSimulator.Restore(s));
            Assert.AreEqual(12000000,RunSimulator.CrystalLimitForRules(6));
            Assert.AreEqual(30000000,RunSimulator.CrystalLimitForRules(7));
        }
    }
}
