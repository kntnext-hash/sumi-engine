using System;
using System.Reflection;
using System.Collections.Generic;
using Bokumyaku.Simulation;
using NUnit.Framework;
using UnityEngine;
namespace Bokumyaku.Tests
{
    public class EnemyAndSystemTests
    {
        static RunSnapshot SealFixture(RunSnapshot s)
        {
            s.exactDoubleBits=(string)typeof(RunSimulator).GetMethod("EncodeExactDoubles",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{s});
            return s;
        }
        static void Finish(RunSimulator r) { while(r.State==RunStatus.Running)r.Tick(); }
        [Test] public void InitialSnapshotIsIndependentAndRestores()
        {
            var r=new RunSimulator(71); var s=r.CaptureState(); s.levels[0]=2;
            Assert.AreEqual(0,r.UpgradeLevels[0]);
            var restored=RunSimulator.Restore(r.CaptureState()); Assert.AreEqual(JsonUtility.ToJson(r.CaptureState()),JsonUtility.ToJson(restored.CaptureState()));
        }
        [TestCase(WeaponKind.Point)] [TestCase(WeaponKind.Pierce)] [TestCase(WeaponKind.Bleed)]
        [TestCase(WeaponKind.Sweep)] [TestCase(WeaponKind.Enso)] [TestCase(WeaponKind.Resonance)]
        public void JsonSnapshotRestoresExactFuture(WeaponKind weapon)
        {
            var a=new RunSimulator(42);a.SetBuild(BuildPreset.Farming);a.SetWeapon(weapon);
            for(int i=0;i<1800&&a.State==RunStatus.Running;i++)a.Tick();
            var b=RunSimulator.Restore(JsonUtility.FromJson<RunSnapshot>(JsonUtility.ToJson(a.CaptureState())));
            while(a.State==RunStatus.Running){a.Tick();b.Tick();Assert.AreEqual(a.Kills,b.Kills);Assert.AreEqual(a.EffectiveDamage,b.EffectiveDamage);}
            Assert.AreEqual(JsonUtility.ToJson(a.CaptureState()),JsonUtility.ToJson(b.CaptureState()));
        }
        [Test] public void InvalidSnapshotsAreRejected()
        {
            var r=new RunSimulator(1); var s=r.CaptureState();s.spawnCredit=double.NaN;Assert.Throws<ArgumentException>(()=>RunSimulator.Restore(s));
            s=r.CaptureState();s.branches[1].parentId=1;Assert.Throws<ArgumentException>(()=>RunSimulator.Restore(s));
            s=r.CaptureState();s.levels=new int[0];Assert.Throws<ArgumentException>(()=>RunSimulator.Restore(s));
            s=r.CaptureState();s.status=RunStatus.Defeated;Assert.Throws<ArgumentException>(()=>RunSimulator.Restore(s));
        }
        [Test] public void FiveEnemiesCountAndBossScheduleBalance()
        {
            var r=new RunSimulator(42);r.SetBuild(BuildPreset.Farming);Finish(r);
            for(int i=0;i<5;i++){Assert.Greater(r.SpawnedByType[i],0);int active=0;foreach(var e in r.Enemies)if((int)e.Kind==i)active++;Assert.AreEqual(r.SpawnedByType[i],r.KilledByType[i]+r.LeakedByType[i]+active);}
            Assert.AreEqual(r.Wave/10,r.SpawnedByType[(int)EnemyKind.Boss]);
        }
        static void Set(object target,string name,object value){target.GetType().GetProperty(name,BindingFlags.Public|BindingFlags.Instance).SetValue(target,value,null);}
        [Test] public void SplitChildrenNeverSplitOrRewardAgain()
        {
            var r=new RunSimulator(5);var list=(List<EnemyState>)typeof(RunSimulator).GetField("enemies",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(r);
            var e=new EnemyState();Set(e,"Id",1);Set(e,"Kind",EnemyKind.Splitter);Set(e,"Hp",100d);Set(e,"MaxHp",100d);Set(e,"Speed",35d);Set(e,"SpawnWave",15);list.Add(e);
            var hurt=typeof(RunSimulator).GetMethod("Hurt",BindingFlags.NonPublic|BindingFlags.Instance);hurt.Invoke(r,new object[]{e,100d});
            Assert.AreEqual(2,list.Count);Assert.AreEqual(35,list[0].Hp);Assert.IsTrue(list[0].IsSplitChild);int income=r.IncomeEarned;double points=r.RewardPoints;
            foreach(var child in list.ToArray())hurt.Invoke(r,new object[]{child,100d});Assert.AreEqual(0,list.Count);Assert.AreEqual(income,r.IncomeEarned);Assert.AreEqual(points,r.RewardPoints);
            hurt.Invoke(r,new object[]{e,100d});Assert.AreEqual(3,r.Kills);
        }
        [Test] public void RepairHasAtomicCostAndCaps()
        {
            var r=new RunSimulator(1);Assert.IsFalse(r.TryRepair());Set(r,"CoreHp",9);Set(r,"RunInk",10000);Assert.AreEqual(25,r.RepairCost);Assert.IsTrue(r.TryRepair());Assert.AreEqual(10,r.CoreHp);Assert.AreEqual(9975,r.RunInk);
            for(int i=1;i<5;i++){Set(r,"CoreHp",1);Assert.IsTrue(r.TryRepair());}Set(r,"CoreHp",1);Assert.IsFalse(r.TryRepair());Assert.AreEqual(5,r.RepairCount);
        }
        [Test] public void ClaimedRewardRemainsClaimedAfterRestore()
        {
            var r=new RunSimulator(2);Finish(r);var m=new MetaProgress();Assert.IsTrue(r.ClaimReward(m));var restored=RunSimulator.Restore(JsonUtility.FromJson<RunSnapshot>(JsonUtility.ToJson(r.CaptureState())));Assert.IsFalse(restored.ClaimReward(m));
            Assert.AreEqual((int)Math.Floor(r.RewardPoints*.08),r.CrystalsReward);
        }
        [Test] public void NewWeaponSwitchPreservesCooldown()
        {
            var r=new RunSimulator(2);var cooldown=typeof(BranchState).GetField("NextAttack",BindingFlags.NonPublic|BindingFlags.Instance);cooldown.SetValue(r.Branches[1],1000);
            foreach(WeaponKind weapon in Enum.GetValues(typeof(WeaponKind))){r.SetWeapon(weapon);Assert.AreEqual(1000,cooldown.GetValue(r.Branches[1]));}
        }
        [Test] public void TerminalBoundarySnapshotsRestore()
        {
            var s=new RunSimulator(3).CaptureState();s.tickCount=359;s.wave=1;s.completedWave=0;s.coreHp=1;s.nextEnemyId=1;s.spawnedByType[0]=1;
            s.enemies=new[]{new EnemySnapshot{id=1,kind=EnemyKind.Normal,x=12,y=0,hp=10,maxHp=10,speed=35,spawnWave=1}};
            s.fields=new[]{new FieldSnapshot{x=0,y=0,radius=20,damagePerTick=1,untilTick=360,weapon=WeaponKind.Bleed}};
            var r=RunSimulator.Restore(SealFixture(s));r.Tick();Assert.AreEqual(RunStatus.Defeated,r.State);Assert.AreEqual(0,r.CompletedWave);Assert.DoesNotThrow(()=>RunSimulator.Restore(r.CaptureState()));
            s=new RunSimulator(3).CaptureState();s.tickCount=RunSimulator.LongRunMaxTicks-1;s.wave=RunSimulator.LongRunWaveLimit;s.completedWave=RunSimulator.LongRunWaveLimit-1;
            r=RunSimulator.Restore(SealFixture(s));r.Tick();Assert.AreEqual(RunStatus.TestCompleted,r.State);Assert.DoesNotThrow(()=>RunSimulator.Restore(r.CaptureState()));
        }
        [Test] public void CapacityStopSnapshotRestoresWithoutReward()
        {
            var s=new RunSimulator(3).CaptureState();s.tickCount=360;s.wave=1;s.completedWave=1;s.spawnCredit=.99;s.nextEnemyId=96;s.spawnedByType[0]=96;s.enemies=new EnemySnapshot[96];
            for(int i=0;i<96;i++)s.enemies[i]=new EnemySnapshot{id=i+1,kind=EnemyKind.Normal,x=350,y=0,hp=100,maxHp=100,speed=35,spawnWave=1};
            var r=RunSimulator.Restore(SealFixture(s));r.Tick();Assert.AreEqual(RunStatus.CapacityExceeded,r.State);Assert.AreEqual(0,r.CrystalsReward);Assert.DoesNotThrow(()=>RunSimulator.Restore(r.CaptureState()));
        }
        [TestCase(WeaponKind.Sweep,5)] [TestCase(WeaponKind.Enso,6)] [TestCase(WeaponKind.Resonance,4)]
        public void AdditionalWeaponsHaveBoundedHits(WeaponKind weapon,int cap)
        {
            var r=new RunSimulator(7);r.SetWeapon(weapon);
            var list=(List<EnemyState>)typeof(RunSimulator).GetField("enemies",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(r);
            for(int i=0;i<12;i++){var e=new EnemyState();Set(e,"Id",i+1);Set(e,"Kind",EnemyKind.Normal);Set(e,"X",100d+i);Set(e,"Y",0d);Set(e,"Hp",10000d);Set(e,"MaxHp",10000d);Set(e,"Speed",.001d);Set(e,"SpawnWave",1);list.Add(e);}
            var cd=typeof(BranchState).GetField("NextAttack",BindingFlags.NonPublic|BindingFlags.Instance);
            cd.SetValue(r.Branches[2],100);cd.SetValue(r.Branches[3],100);r.Tick();Assert.AreEqual(cap,r.Attacks.Count);
            var ids=new HashSet<int>();foreach(var a in r.Attacks){Assert.IsTrue(ids.Add(a.EnemyId));Assert.AreEqual(weapon,a.Weapon);}
        }
        [Test] public void ExactPayloadRestoresAdjacentDoubleWithoutMutatingInput()
        {
            var r=new RunSimulator(9);var s=r.CaptureState();double original=s.branches[1].x;
            s.branches[1].x=BitConverter.Int64BitsToDouble(BitConverter.DoubleToInt64Bits(original)+1);
            double parsed=s.branches[1].x;var restored=RunSimulator.Restore(s);
            Assert.AreEqual(BitConverter.DoubleToInt64Bits(original),BitConverter.DoubleToInt64Bits(restored.Branches[1].X));
            Assert.AreEqual(parsed,s.branches[1].x);
            s=r.CaptureState();original=s.branches[2].x;s.branches[2].x=BitConverter.Int64BitsToDouble(BitConverter.DoubleToInt64Bits(original)+1);
            restored=RunSimulator.Restore(s);Assert.AreEqual(BitConverter.DoubleToInt64Bits(original),BitConverter.DoubleToInt64Bits(restored.Branches[2].X));
        }
        [Test] public void ExactPayloadRejectsChangedNumbersOrCorruptBits()
        {
            var r=new RunSimulator(9);var s=r.CaptureState();s.branches[1].x=BitConverter.Int64BitsToDouble(BitConverter.DoubleToInt64Bits(s.branches[1].x)+2);
            Assert.Throws<ArgumentException>(()=>RunSimulator.Restore(s));
            s=r.CaptureState();s.exactDoubleBits="not-base64";Assert.Throws<ArgumentException>(()=>RunSimulator.Restore(s));
            s=r.CaptureState();s.spawnCredit=double.PositiveInfinity;Assert.Throws<ArgumentException>(()=>RunSimulator.Restore(s));
        }
        [Test] public void ExactPayloadPreservesSignedZero()
        {
            var s=new RunSimulator(9).CaptureState();s.spawnCredit=BitConverter.Int64BitsToDouble(long.MinValue);SealFixture(s);s.spawnCredit=0;
            Assert.AreEqual(long.MinValue,BitConverter.DoubleToInt64Bits(RunSimulator.Restore(s).CaptureState().spawnCredit));
        }
    }
}
