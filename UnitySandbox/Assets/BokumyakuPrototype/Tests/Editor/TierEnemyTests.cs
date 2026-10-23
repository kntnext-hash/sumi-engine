using System;
using System.Reflection;
using Bokumyaku.Simulation;
using NUnit.Framework;
using UnityEngine;
namespace Bokumyaku.Tests
{
    public class TierEnemyTests
    {
        static EnemyState Spawn(RunSimulator r,EnemyKind kind)
        { typeof(RunSimulator).GetMethod("Spawn",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(r,new object[]{(EnemyKind?)kind}); return r.Enemies[r.Enemies.Count-1]; }
        static void Set(EnemyState e,string name,double value) => typeof(EnemyState).GetProperty(name).SetValue(e,value);
        static void Hurt(RunSimulator r,EnemyState e,double damage) => typeof(RunSimulator).GetMethod("Hurt",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(r,new object[]{e,damage});
        [Test] public void HaloReducesDamageOnlyDuringShieldPhase()
        {
            var r=new RunSimulator(42,tier:2);var e=Spawn(r,EnemyKind.ShieldHalo);
            Assert.IsTrue(r.IsShieldActive(e));double before=e.Hp;Hurt(r,e,10);Assert.AreEqual(before-3.5,e.Hp,1e-10);
            while(r.IsShieldActive(e)) r.Tick();before=e.Hp;Hurt(r,e,10);Assert.AreEqual(before-10,e.Hp,1e-10);
        }
        [Test] public void LeechAttachesDrainsHealsAndStopsAfterDeath()
        {
            var r=new RunSimulator(42,tier:2);var e=Spawn(r,EnemyKind.Leech);Set(e,"X",35);Set(e,"Y",0);Set(e,"Hp",e.MaxHp*.5);
            // Remove branch attacks from this focused ability fixture.
            foreach(var b in r.Branches) typeof(BranchState).GetField("NextAttack",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(b,1000);
            Assert.IsTrue(r.IsLeechAttached(e));double hp=e.Hp;
            for(int i=0;i<90;i++)r.Tick();
            Assert.AreEqual(9,r.CoreHp);Assert.AreEqual(0,r.Leaks);Assert.Greater(e.Hp,hp);
            Hurt(r,e,e.MaxHp*2);for(int i=0;i<90;i++)r.Tick();Assert.AreEqual(9,r.CoreHp);
        }
        [Test] public void AbilityPhaseAndCountersSurviveSaveExactly()
        {
            var a=new RunSimulator(42,tier:2);var e=Spawn(a,EnemyKind.Leech);Set(e,"X",36);Set(e,"Y",0);Spawn(a,EnemyKind.ShieldHalo);
            for(int i=0;i<32;i++)a.Tick();var b=RunSimulator.Restore(JsonUtility.FromJson<RunSnapshot>(JsonUtility.ToJson(a.CaptureState())));
            for(int i=0;i<250;i++){a.Tick();b.Tick();}
            Assert.AreEqual(JsonUtility.ToJson(a.CaptureState()),JsonUtility.ToJson(b.CaptureState()));Assert.AreEqual(7,b.SpawnedByType.Length);
        }
        [TestCase(1,false)] [TestCase(2,true)] [TestCase(3,true)]
        public void NewEnemyCompositionIsLimitedToHigherTiers(int tier,bool expected)
        {
            var r=new RunSimulator(42,tier:tier);typeof(RunSimulator).GetProperty("Wave").SetValue(r,10);
            var spawn=typeof(RunSimulator).GetMethod("Spawn",BindingFlags.NonPublic|BindingFlags.Instance);
            for(int i=0;i<90;i++) spawn.Invoke(r,new object[]{null});
            Assert.AreEqual(expected,r.SpawnedByType[5]>0);Assert.AreEqual(expected,r.SpawnedByType[6]>0);
        }
        [Test] public void HigherTierCompositionRetainsEveryExistingAndNewKind()
        {
            var r=new RunSimulator(42,tier:2);
            typeof(RunSimulator).GetProperty("TickCount").SetValue(r,99*360);
            // Enter Wave100 through normal Tick: scheduled boss plus unforced regular rolls.
            typeof(RunSimulator).GetProperty("CoreHp").SetValue(r,10000);
            for(int i=0;i<1080;i++)r.Tick();
            var counts=r.SpawnedByType;
            for(int i=0;i<7;i++)Assert.Greater(counts[i],0,"missing naturally spawned kind "+(EnemyKind)i);
        }
        [Test] public void LeechDrainAtZeroCoreHpEndsRunWithoutCountingLeak()
        {
            var r=new RunSimulator(42,tier:2);var e=Spawn(r,EnemyKind.Leech);
            Set(e,"X",35);Set(e,"Y",0);
            typeof(RunSimulator).GetProperty("CoreHp").SetValue(r,1);
            foreach(var b in r.Branches)typeof(BranchState).GetField("NextAttack",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(b,1000);
            for(int i=0;i<90 && r.State==RunStatus.Running;i++)r.Tick();
            Assert.AreEqual(0,r.CoreHp);Assert.AreEqual(RunStatus.Defeated,r.State);Assert.AreEqual(0,r.Leaks);
            Assert.AreEqual(JsonUtility.ToJson(r.CaptureState()),JsonUtility.ToJson(RunSimulator.Restore(r.CaptureState()).CaptureState()));
        }
        [Test] public void TierOneRejectsNewEnemyHistoricalCounters()
        {
            var r=new RunSimulator(42,tier:2);var e=Spawn(r,EnemyKind.Leech);Hurt(r,e,e.MaxHp*2);
            var s=r.CaptureState();Assert.AreEqual(1,s.killedByType[5]);
            // All totals/reward bits remain internally consistent; only Tier is forged.
            s.tier=1;Assert.Throws<ArgumentException>(()=>RunSimulator.Restore(s));
        }
        [Test] public void RulesSevenContinuationKeepsFiveTypesAndNoNewEnemies()
        {
            var s=new RunSimulator(42,tier:2).CaptureState();s.rulesVersion=7;Array.Resize(ref s.spawnedByType,5);Array.Resize(ref s.killedByType,5);Array.Resize(ref s.leakedByType,5);
            var a=RunSimulator.Restore(s);a.SetBuild(BuildPreset.Farming);for(int i=0;i<6000;i++)a.Tick();var b=RunSimulator.Restore(a.CaptureState());
            for(int i=0;i<200;i++){a.Tick();b.Tick();}
            Assert.AreEqual(5,b.SpawnedByType.Length);Assert.AreEqual(JsonUtility.ToJson(a.CaptureState()),JsonUtility.ToJson(b.CaptureState()));foreach(var enemy in b.Enemies)Assert.Less((int)enemy.Kind,5);
        }
    }
}
