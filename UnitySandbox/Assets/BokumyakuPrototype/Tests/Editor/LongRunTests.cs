using System;
using System.IO;
using System.Reflection;
using Bokumyaku.Simulation;
using NUnit.Framework;
using UnityEngine;
namespace Bokumyaku.Tests
{
    public class LongRunTests
    {
        static RunSnapshot Seal(RunSnapshot s) { s.exactDoubleBits=(string)typeof(RunSimulator).GetMethod("EncodeExactDoubles",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{s});return s; }
        static RunSnapshot Read(string name) { return JsonUtility.FromJson<RunSnapshot>(File.ReadAllText(Path.Combine(Application.dataPath,"BokumyakuPrototype/Tests/Fixtures/"+name+".json"))); }
        static string State(RunSimulator r) { return JsonUtility.ToJson(r.CaptureState()); }
        static RunSimulator AtBoundary(int completedWave)
        {
            var s=new RunSimulator(42).CaptureState();s.tickCount=completedWave*360;s.wave=Math.Max(1,completedWave);s.completedWave=completedWave;
            return RunSimulator.Restore(s);
        }
        [Test] public void RulesHaveExplicitDifferentLimits()
        {
            for(int i=1;i<=4;i++){Assert.AreEqual(60,RunSimulator.WaveLimitForRules(i));Assert.AreEqual(21600,RunSimulator.TickLimitForRules(i));Assert.AreEqual(80000,RunSimulator.CrystalLimitForRules(i));}
            var r=new RunSimulator(1);Assert.AreEqual(100000,r.RunWaveLimit);Assert.AreEqual(36000000,r.RunTickLimit);
            Assert.Throws<ArgumentOutOfRangeException>(()=>RunSimulator.WaveLimitForRules(6));
        }
        [Test] public void AllSupportedWavesAreFiniteMonotoneAndEarlyFormulaExact()
        {
            double hp=0,reward=0,speed=0;
            for(int w=1;w<=RunSimulator.LongRunWaveLimit;w++)
            {
                double h=RunSimulator.BaseHp(w),p=RunSimulator.RewardWaveFactor(w),v=RunSimulator.EnemySpeedFactor(w);
                Assert.IsFalse(double.IsNaN(h)||double.IsInfinity(h));Assert.GreaterOrEqual(h,hp);Assert.GreaterOrEqual(p,reward);Assert.GreaterOrEqual(v,speed);
                Assert.Less(h*12,1e12);Assert.Less(p,10);Assert.LessOrEqual(v,2.5);Assert.LessOrEqual(RunSimulator.SpawnRate(w),4.8);
                if(w<=60){Assert.AreEqual(16*Math.Pow(1.072,w-1)*(1+.12*Math.Floor((w-1)/10.0)),h);Assert.AreEqual(1+.018*w,p);Assert.AreEqual(1+.006*(w-1),v);}
                hp=h;reward=p;speed=v;
            }
            Assert.Throws<ArgumentOutOfRangeException>(()=>RunSimulator.BaseHp(100001));Assert.Throws<ArgumentOutOfRangeException>(()=>RunSimulator.SpawnRate(0));
        }
        [TestCase("v8-point")] [TestCase("v8-enso")]
        public void ActualRules4DllFixtureRetainsFutureAndTerminal(string prefix)
        {
            var r=RunSimulator.Restore(Read(prefix+"-tick6000"));Assert.AreEqual(4,r.RulesVersion);
            for(int i=0;i<900;i++)r.Tick();Assert.AreEqual(State(RunSimulator.Restore(Read(prefix+"-tick6900"))),State(r));
            while(r.State==RunStatus.Running)r.Tick();Assert.AreEqual(State(RunSimulator.Restore(Read(prefix+"-terminal"))),State(r));
        }
        [Test] public void Rules4StopsAt60AndRules5ContinuesSameState()
        {
            var s=Read("v8-enso-tick21599");var old=RunSimulator.Restore(s);s.rulesVersion=5;var current=RunSimulator.Restore(s);
            old.Tick();current.Tick();Assert.AreEqual(RunStatus.TestCompleted,old.State);Assert.AreEqual(RunStatus.Running,current.State);Assert.AreEqual(60,current.CompletedWave);
            current.Tick();Assert.AreEqual(61,current.Wave);Assert.DoesNotThrow(()=>RunSimulator.Restore(current.CaptureState()));
            s=current.CaptureState();s.rulesVersion=4;Assert.Throws<ArgumentException>(()=>RunSimulator.Restore(s));
        }
        [TestCase(60)] [TestCase(999)] [TestCase(9999)] [TestCase(99999)]
        public void HighWaveJsonRoundTripRetainsEveryFollowingTick(int completedWave)
        {
            var a=AtBoundary(completedWave);var b=RunSimulator.Restore(JsonUtility.FromJson<RunSnapshot>(State(a)));
            for(int i=0;i<180;i++){a.Tick();b.Tick();Assert.AreEqual(State(a),State(b));Assert.DoesNotThrow(()=>RunSimulator.Restore(a.CaptureState()));}
        }
        [Test] public void HundredThousandWaveBoundaryIsFiniteAndRestorable()
        {
            var s=new RunSimulator(1).CaptureState();s.tickCount=RunSimulator.LongRunMaxTicks-1;s.wave=100000;s.completedWave=99999;
            var r=RunSimulator.Restore(s);r.Tick();Assert.AreEqual(RunStatus.TestCompleted,r.State);Assert.AreEqual(100000,r.CompletedWave);Assert.AreEqual(36000000,r.TickCount);
            Assert.AreEqual(State(r),State(RunSimulator.Restore(JsonUtility.FromJson<RunSnapshot>(State(r)))));
            r.Tick();Assert.AreEqual(36000000,r.TickCount);
        }
        [Test] public void ExplicitCurrencyAndCounterBoundsFitIntWithoutSaturation()
        {
            const long regular=4320001,boss=10000;
            Assert.Less(3*regular+boss,RunSimulator.LongRunCounterLimit);
            Assert.Less(128*regular+768*boss,RunSimulator.LongRunIncomeLimit);
            Assert.Less(16*(2*regular+12*boss),RunSimulator.LongRunRewardPointsLimit);
            Assert.Less(Math.Floor(16*(2*regular+12*boss)*.08),RunSimulator.LongRunCrystalLimit);
            Assert.Less(RunSimulator.LongRunIncomeLimit,int.MaxValue);
            var s=new RunSimulator(1).CaptureState();s.runInk=s.incomeEarned=RunSimulator.LongRunIncomeLimit+1;Assert.Throws<ArgumentException>(()=>RunSimulator.Restore(s));
            s=new RunSimulator(1).CaptureState();s.growthCharge=RunSimulator.LongRunGrowthChargeLimit+1;Assert.Throws<ArgumentException>(()=>RunSimulator.Restore(Seal(s)));
        }
    }
}
