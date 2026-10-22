using System;
namespace Bokumyaku.Simulation
{
    // Versioned, public-field-only DTOs for Unity JsonUtility. These are local save data,
    // not authenticated competitive results. Restore validates before accepting them.
    [Serializable] public sealed class RunSnapshot
    {
        public int schemaVersion = 1;
        public string exactDoubleBits;
        public int seed, tickCount, wave, completedWave, coreHp, runInk, kills, leaks, crystalsReward, incomeEarned;
        public int nextEnemyId, growthCount, repairCount, lastBossWave;
        public uint spawnRng, growthRng;
        public double spawnCredit, growthCharge, effectiveDamage, rewardPoints;
        public bool rewardClaimed;
        public RunStatus status;
        public GrowthPolicy policy;
        public BuildPreset build;
        public WeaponKind weapon;
        public MetaProgress meta;
        public int[] levels, spawnedByType, killedByType, leakedByType;
        public EnemySnapshot[] enemies;
        public BranchSnapshot[] branches;
        public FieldSnapshot[] fields;
        public AttackSnapshot[] attacks;
    }
    [Serializable] public sealed class EnemySnapshot
    {
        public int id, spawnWave;
        public EnemyKind kind;
        public double x,y,hp,maxHp,speed;
        public bool isSplitChild;
    }
    [Serializable] public sealed class BranchSnapshot
    {
        public int id,parentId,nextAttack;
        public double x,y,pathLength;
        public bool isTip;
    }
    [Serializable] public sealed class FieldSnapshot
    {
        public double x,y,radius,damagePerTick;
        public int untilTick;
        public WeaponKind weapon;
    }
    [Serializable] public sealed class AttackSnapshot
    {
        public int branchId,enemyId;
        public double x,y,targetX,targetY,damage,pressure;
        public WeaponKind weapon;
    }
}
