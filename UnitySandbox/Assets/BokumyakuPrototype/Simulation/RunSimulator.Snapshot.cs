using System;
using System.Collections.Generic;
namespace Bokumyaku.Simulation
{
    public sealed partial class RunSimulator
    {
        public RunSnapshot CaptureState()
        {
            var s = new RunSnapshot {
                schemaVersion = 3, tier = Tier, rulesVersion = RulesVersion, autoPurchaseCursor = autoPurchaseCursor,
                seed = Seed,
                tickCount = TickCount,
                wave = Wave,
                completedWave = CompletedWave,
                coreHp = CoreHp,
                runInk = RunInk,
                kills = Kills,
                leaks = Leaks,
                crystalsReward = CrystalsReward,
                incomeEarned = IncomeEarned,
                status = State,
                policy = Policy,
                build = Build,
                weapon = Weapon,
                effectiveDamage = EffectiveDamage,
                nextEnemyId = nextEnemyId, growthCount = growthCount, repairCount = repairCount, lastBossWave = lastBossWave, spawnRng = spawnRng, growthRng = growthRng, spawnCredit = spawnCredit, growthCharge = growthCharge, rewardPoints = rewardPoints, rewardClaimed = rewardClaimed,
                meta = meta.Snapshot(), levels = (int[])levels.Clone(), spawnedByType = SpawnedByType, killedByType = KilledByType, leakedByType = LeakedByType,
                enemies = new EnemySnapshot[enemies.Count], branches = new BranchSnapshot[branches.Count], fields = new FieldSnapshot[fields.Count], attacks = new AttackSnapshot[attacks.Count]
            };
            for(int i=0;i<enemies.Count;i++) { var e=enemies[i]; s.enemies[i]=new EnemySnapshot {id=e.Id,kind=e.Kind,x=e.X,y=e.Y,hp=e.Hp,maxHp=e.MaxHp,speed=e.Speed,spawnWave=e.SpawnWave,isSplitChild=e.IsSplitChild}; }
            for(int i=0;i<branches.Count;i++) { var b=branches[i]; s.branches[i]=new BranchSnapshot {id=b.Id,parentId=b.ParentId,x=b.X,y=b.Y,pathLength=b.PathLength,isTip=b.IsTip,nextAttack=b.NextAttack}; }
            for(int i=0;i<fields.Count;i++) { var f=fields[i]; s.fields[i]=new FieldSnapshot {x=f.X,y=f.Y,radius=f.Radius,damagePerTick=f.DamagePerTick,untilTick=f.UntilTick,weapon=f.Weapon}; }
            for(int i=0;i<attacks.Count;i++) { var a=attacks[i]; s.attacks[i]=new AttackSnapshot {branchId=a.BranchId,enemyId=a.EnemyId,x=a.X,y=a.Y,targetX=a.TargetX,targetY=a.TargetY,damage=a.Damage,pressure=a.Pressure,weapon=a.Weapon}; }
            s.exactDoubleBits = EncodeExactDoubles(s);
            return s;
        }
        public static RunSimulator Restore(RunSnapshot s)
        {
            if(s==null) throw new ArgumentNullException(nameof(s));
            s = DecodeExactDoubles(s);
            Check(s.schemaVersion==1 || s.schemaVersion==2 || s.schemaVersion==3,"schemaVersion");
            int rules = s.schemaVersion == 1 ? LegacyRulesVersion : s.rulesVersion;
            Check(rules>=1 && rules<=CurrentRulesVersion,"rulesVersion");
            Check(s.schemaVersion!=1 || (s.rulesVersion==0 && s.autoPurchaseCursor==0),"legacy metadata");
            int tier = s.schemaVersion < 3 ? 1 : s.tier;
            Range(tier,1,TierBalance.Count,"tier");
            Check(s.schemaVersion >= 3 || rules <= 6,"legacy schema rules");
            Check(rules >= 7 || tier == 1,"legacy tier");
            Range(s.autoPurchaseCursor,0,5,"purchase cursor");
            Check(rules!=1 || s.autoPurchaseCursor==0,"legacy cursor");
            int maxWave=WaveLimitForRules(rules),maxTicks=TickLimitForRules(rules),maxCounter=rules>=5?LongRunCounterLimit:100000,maxIncome=rules>=5?LongRunIncomeLimit:10000000;
            Range(s.tickCount,0,maxTicks,"tickCount"); Range(s.wave,1,maxWave,"wave"); Range(s.completedWave,0,maxWave,"completedWave");
            Check(s.wave==Math.Min(maxWave,Math.Max(1,(s.tickCount-1)/360+1)),"wave/tick mismatch");
            Check(s.completedWave==(s.status==RunStatus.Defeated?Math.Max(0,s.tickCount-1)/360:s.tickCount/360),"completedWave/tick mismatch");
            Range(s.coreHp,0,10,"coreHp"); Range(s.runInk,0,maxIncome,"runInk"); Range(s.incomeEarned,s.runInk,maxIncome,"incomeEarned");
            Range(s.kills,0,maxCounter,"kills"); Range(s.leaks,0,maxCounter,"leaks"); Range(s.nextEnemyId,0,maxCounter,"nextEnemyId");
            Range(s.growthCount,0,64,"growthCount"); Range(s.repairCount,0,5,"repairCount"); Range(s.lastBossWave,0,s.wave,"lastBossWave"); Check(s.lastBossWave%10==0,"lastBossWave");
            Number(s.spawnCredit,0,1,"spawnCredit"); Number(s.growthCharge,0,rules>=5?LongRunGrowthChargeLimit:100000,"growthCharge"); Number(s.rewardPoints,0,rules>=5?LongRunRewardPointsLimit:1000000,"rewardPoints"); Number(s.effectiveDamage,0,rules>=9?FirstWeekEffectiveDamageLimit:rules>=6?LongRunEffectiveDamageLimit:1e15,"effectiveDamage");
            Check(Enum.IsDefined(typeof(RunStatus),s.status)&&Enum.IsDefined(typeof(BuildPreset),s.build)&&Enum.IsDefined(typeof(GrowthPolicy),s.policy)&&Enum.IsDefined(typeof(WeaponKind),s.weapon),"enum");
            Check(s.status!=RunStatus.Defeated || s.coreHp==0,"defeat requires zero HP");
            Check(s.status!=RunStatus.Running || (s.coreHp>0 && s.tickCount<maxTicks),"running state");
            Check(s.status!=RunStatus.TestCompleted || (s.tickCount==maxTicks && s.coreHp>0),"test-completed state");
            Check(!s.rewardClaimed || (s.status==RunStatus.Defeated || s.status==RunStatus.TestCompleted),"reward claimed state");
            int expectedReward=(s.status==RunStatus.Defeated || s.status==RunStatus.TestCompleted)?CalculateCrystalsReward(s.rewardPoints,rules,tier):0;
            Check(s.crystalsReward==expectedReward,"crystalsReward");
            Check(s.meta!=null,"meta"); s.meta.Snapshot(); Check(rules>=6 || s.meta.Damage<=20,"legacy meta Damage cap");
            Check(s.levels!=null && s.levels.Length==(rules==1?6:7),"levels"); int[] caps={rules>=6?100:30,25,25,9,10,10,HarvestMaxLevel}; for(int i=0;i<s.levels.Length;i++)Range(s.levels[i],0,caps[i],"level");
            Check(s.enemies!=null&&s.enemies.Length<=EnemyLimit&&s.branches!=null&&s.branches.Length>=4&&s.branches.Length<=64&&s.fields!=null&&s.fields.Length<=FieldLimit&&s.attacks!=null&&s.attacks.Length<=96,"array bounds");
            int typeCount=rules>=8?7:5;
            Check(s.spawnedByType!=null&&s.spawnedByType.Length==typeCount&&s.killedByType!=null&&s.killedByType.Length==typeCount&&s.leakedByType!=null&&s.leakedByType.Length==typeCount,"type counters");
            if(rules>=8 && tier==1)
                for(int i=5;i<typeCount;i++)Check(s.spawnedByType[i]==0&&s.killedByType[i]==0&&s.leakedByType[i]==0,"new enemy counters in Tier 1");
            int sk=0,sl=0,ss=0; var active=new int[typeCount]; var ids=new HashSet<int>();
            foreach(var e in s.enemies) { Check(e!=null,"enemy null"); Range(e.id,1,s.nextEnemyId,"enemy id"); Check(ids.Add(e.id),"duplicate enemy"); Check(Enum.IsDefined(typeof(EnemyKind),e.kind) && ((int)e.kind<5 || (rules>=8 && tier>=2)),"enemy kind");
                Number(e.x,-1024,1024,"enemy x"); Number(e.y,-1024,1024,"enemy y"); Number(e.maxHp,double.Epsilon,rules>=7?TierBalance.EnemyBaseHp(TierBalance.Count,LongRunWaveLimit)*12:1e12,"maxHP"); Number(e.hp,double.Epsilon,e.maxHp,"hp"); Number(e.speed,double.Epsilon,10000,"speed"); Range(e.spawnWave,1,s.wave,"spawnWave"); Check(!e.isSplitChild||e.kind==EnemyKind.Splitter,"split child kind"); active[(int)e.kind]++; }
            for(int i=0;i<typeCount;i++) { Range(s.spawnedByType[i],0,maxCounter,"spawn count"); Range(s.killedByType[i],0,maxCounter,"kill count"); Range(s.leakedByType[i],0,maxCounter,"leak count"); Check(s.spawnedByType[i]==s.killedByType[i]+s.leakedByType[i]+active[i],"type counter balance"); ss+=s.spawnedByType[i];sk+=s.killedByType[i];sl+=s.leakedByType[i]; }
            Check(ss==s.nextEnemyId&&sk==s.kills&&sl==s.leaks,"total counters");
            double minimumPoints=(rules>=8?2.0*(s.killedByType[5]+s.killedByType[6]):0)+s.killedByType[0]+1.1*s.killedByType[1]+2.0*s.killedByType[2]+12.0*s.killedByType[4];
            // Long sums can accumulate floating-point error; legacy tolerance stays unchanged.
            double pointsTolerance=rules>=5?Math.Max(1e-6,s.kills*1e-7):1e-6;
            Number(s.rewardPoints,Math.Max(0,minimumPoints-pointsTolerance),(minimumPoints+1.4*s.killedByType[3])*(rules==1?1:1+HarvestGainPerLevel*s.levels[6])+pointsTolerance,"reward point counters");
            int tips=0; var hasChild=new bool[s.branches.Length];
            for(int i=0;i<s.branches.Length;i++) {var b=s.branches[i];Check(b!=null&&b.id==i,"branch id"); Check(i==0?b.parentId==-1:b.parentId>=0&&b.parentId<i,"branch parent"); Number(b.x,-GrowthRadius,GrowthRadius,"branch x"); Number(b.y,-GrowthRadius,GrowthRadius,"branch y");Number(b.pathLength,0,10000,"pathLength");Range(b.nextAttack,0,maxTicks+1000,"cooldown");if(i>0){hasChild[b.parentId]=true;var p=s.branches[b.parentId];double dx=b.x-p.x,dy=b.y-p.y;Check(Math.Abs(b.pathLength-p.pathLength-Math.Sqrt(dx*dx+dy*dy))<1e-6,"path geometry");} if(b.isTip)tips++; }
            Check(tips>=3&&tips<=Math.Min(12,3+s.levels[3]),"tip count"); for(int i=0;i<hasChild.Length;i++)Check(s.branches[i].isTip!=hasChild[i],"tip topology");
            foreach(var f in s.fields){Check(f!=null&&Enum.IsDefined(typeof(WeaponKind),f.weapon),"field");Number(f.x,-1024,1024,"field x");Number(f.y,-1024,1024,"field y");Number(f.radius,0,1000,"field radius");Number(f.damagePerTick,0,1e9,"field damage");Range(f.untilTick,0,maxTicks+1000,"field time");}
            foreach(var a in s.attacks){Check(a!=null&&Enum.IsDefined(typeof(WeaponKind),a.weapon),"attack");Range(a.branchId,1,s.branches.Length-1,"attack branch");Range(a.enemyId,1,s.nextEnemyId,"attack enemy");Number(a.x,-1024,1024,"attack x");Number(a.y,-1024,1024,"attack y");Number(a.targetX,-1024,1024,"target x");Number(a.targetY,-1024,1024,"target y");Number(a.damage,0,1e9,"attack damage");Number(a.pressure,.45,1.65,"pressure");}
            var r=new RunSimulator(s.seed,s.meta,tier);
            r.RulesVersion=rules; r.autoPurchaseCursor=s.autoPurchaseCursor; r.levels=new int[s.levels.Length];
            r.Seed = s.seed;
            r.TickCount = s.tickCount;
            r.Wave = s.wave;
            r.CompletedWave = s.completedWave;
            r.CoreHp = s.coreHp;
            r.RunInk = s.runInk;
            r.Kills = s.kills;
            r.Leaks = s.leaks;
            r.CrystalsReward = s.crystalsReward;
            r.IncomeEarned = s.incomeEarned;
            r.State = s.status;
            r.Policy = s.policy;
            r.Build = s.build;
            r.Weapon = s.weapon;
            r.EffectiveDamage = s.effectiveDamage;
            r.nextEnemyId = s.nextEnemyId; r.growthCount = s.growthCount; r.repairCount = s.repairCount; r.lastBossWave = s.lastBossWave; r.spawnRng = s.spawnRng; r.growthRng = s.growthRng; r.spawnCredit = s.spawnCredit; r.growthCharge = s.growthCharge; r.rewardPoints = s.rewardPoints; r.rewardClaimed = s.rewardClaimed;
            Array.Copy(s.levels,r.levels,s.levels.Length);r.spawnedByType=(int[])s.spawnedByType.Clone();r.killedByType=(int[])s.killedByType.Clone();r.leakedByType=(int[])s.leakedByType.Clone();
            r.enemies.Clear();foreach(var e in s.enemies)r.enemies.Add(new EnemyState{Id=e.id,Kind=e.kind,X=e.x,Y=e.y,Hp=e.hp,MaxHp=e.maxHp,Speed=e.speed,SpawnWave=e.spawnWave,IsSplitChild=e.isSplitChild});
            r.branches.Clear();foreach(var b in s.branches)r.branches.Add(new BranchState{Id=b.id,ParentId=b.parentId,X=b.x,Y=b.y,PathLength=b.pathLength,IsTip=b.isTip,NextAttack=b.nextAttack});
            foreach(var f in s.fields)r.fields.Add(new InkFieldState{X=f.x,Y=f.y,Radius=f.radius,DamagePerTick=f.damagePerTick,UntilTick=f.untilTick,Weapon=f.weapon});
            foreach(var a in s.attacks)r.attacks.Add(new AttackEvent{BranchId=a.branchId,EnemyId=a.enemyId,X=a.x,Y=a.y,TargetX=a.targetX,TargetY=a.targetY,Damage=a.damage,Pressure=a.pressure,Weapon=a.weapon});
            return r;
        }
        static void Check(bool valid,string name){if(!valid)throw new ArgumentException("Invalid run snapshot: "+name);}
        static void Range(int value,int min,int max,string name){Check(value>=min&&value<=max,name);}
        static void Number(double value,double min,double max,string name){Check(!double.IsNaN(value)&&!double.IsInfinity(value)&&value>=min&&value<=max,name);}
    }
}
