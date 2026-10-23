using System;
using System.Collections.Generic;

namespace Bokumyaku.Simulation
{
    public enum WeaponKind { Point, Pierce, Bleed, Sweep, Enso, Resonance }
    public enum BuildPreset { Manual, Farming, Record }
    public enum RunStatus { Running, Defeated, TestCompleted, CapacityExceeded }
    public enum UpgradeKind { Damage, Speed, Supply, Branch, Width, Bleed, Harvest }
    public enum GrowthPolicy { Balanced, Intercept, Focus }
    public enum EnemyKind { Normal, Fast, Heavy, Splitter, Boss, Leech, ShieldHalo }
    public enum MetaUpgrade { Damage, Supply, Growth, Harvest }

    [Serializable]
    public sealed class MetaProgress
    {
        // Price generation is independent of battle rules. All early research shares a rounded level20 anchor.
        public const int ResearchPriceVersion = 3;
        public const int DamageCostAt20 = 1271172; // ceil(500*1.48^20).
        public int Crystals;
        public int Damage, Supply, Growth, Harvest;
        public int Level(MetaUpgrade kind) { return kind == MetaUpgrade.Damage ? Damage : kind == MetaUpgrade.Supply ? Supply : kind == MetaUpgrade.Growth ? Growth : Harvest; }
        public static int MaxLevel(MetaUpgrade kind) { if(!Enum.IsDefined(typeof(MetaUpgrade),kind))throw new ArgumentOutOfRangeException(nameof(kind)); return kind==MetaUpgrade.Damage?100:20; }
        public double DamageMultiplier { get { if(Damage<0 || Damage>100)throw new ArgumentOutOfRangeException(nameof(Damage));return Damage<=20?Math.Pow(1.18,Damage):Math.Pow(1.18,20)*Math.Pow(1+.025*(Damage-20),2); } }
        public int Cost(MetaUpgrade kind)
        {
            int level = Level(kind);
            if (level < 0 || level > MaxLevel(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            if (level <= 20) return (int)Math.Ceiling(500 * Math.Pow(1.48, level));
            return (int)Math.Ceiling(DamageCostAt20 * Math.Pow(1 + .05 * (level - 20), 2));
        }
        public bool TryBuy(MetaUpgrade kind)
        {
            if (!Enum.IsDefined(typeof(MetaUpgrade), kind) || Level(kind) >= MaxLevel(kind) || Crystals < Cost(kind)) return false;
            Crystals -= Cost(kind);
            switch (kind) { case MetaUpgrade.Damage: Damage++; break; case MetaUpgrade.Supply: Supply++; break; case MetaUpgrade.Growth: Growth++; break; default: Harvest++; break; }
            return true;
        }
        internal MetaProgress Snapshot()
        {
            if (Crystals < 0 || Damage < 0 || Damage > 100 || Supply < 0 || Supply > 20 || Growth < 0 || Growth > 20 || Harvest < 0 || Harvest > 20) throw new ArgumentOutOfRangeException("meta");
            return new MetaProgress { Crystals = Crystals, Damage = Damage, Supply = Supply, Growth = Growth, Harvest = Harvest };
        }
    }

    public sealed class EnemyState
    {
        public int Id { get; internal set; }
        public EnemyKind Kind { get; internal set; }
        public double X { get; internal set; }
        public double Y { get; internal set; }
        public double Hp { get; internal set; }
        public double MaxHp { get; internal set; }
        public double Speed { get; internal set; }
        public int SpawnWave { get; internal set; }
        public bool IsSplitChild { get; internal set; }
        public double ViewX { get { return 192 + X * .48; } }
        public double ViewY { get { return 256 + Y * .48; } }
    }
    public sealed class BranchState
    {
        public int Id { get; internal set; }
        public int ParentId { get; internal set; }
        public double X { get; internal set; }
        public double Y { get; internal set; }
        public double PathLength { get; internal set; }
        public bool IsTip { get; internal set; }
        public double ViewX { get { return 192 + X * .48; } }
        public double ViewY { get { return 256 + Y * .48; } }
        internal int NextAttack;
    }
    public sealed class InkFieldState
    {
        public double X { get; internal set; }
        public double Y { get; internal set; }
        public double Radius { get; internal set; }
        public double DamagePerTick { get; internal set; }
        public int UntilTick { get; internal set; }
        public WeaponKind Weapon { get; internal set; }
    }
    public sealed class AttackEvent
    {
        public WeaponKind Weapon { get; internal set; }
        public int BranchId { get; internal set; }
        public int EnemyId { get; internal set; }
        public double X { get; internal set; }
        public double Y { get; internal set; }
        public double TargetX { get; internal set; }
        public double TargetY { get; internal set; }
        public double Damage { get; internal set; }
        public double Pressure { get; internal set; }
    }

    // Fixed 30Hz three-enemy prototype. All balance and distribution choices remain hypotheses.
    public sealed partial class RunSimulator
    {
        public const int LegacyRulesVersion = 1;
        public const int CurrentRulesVersion = 9;
        // Rounded legacy run cost at Damage30: ceil(10*1.16^30*(1+.008*30^2)).
        public const int DamageCostAt30 = 7040;
        public const int HarvestMaxLevel = 10;
        public const double HarvestGainPerLevel = 1.5;
        public const double HarvestBaseCost = 30;
        public const double HarvestCostGrowth = 1.25;
        public int RulesVersion { get; private set; } = CurrentRulesVersion;
        public double HarvestMultiplier { get { return RulesVersion == LegacyRulesVersion ? 1 : 1 + HarvestGainPerLevel * levels[6]; } }
        int autoPurchaseCursor;
        public const int TickRate = 30;
        // Compatibility constants for legacy callers. New runs use RunWaveLimit/RunTickLimit.
        public const int WaveLimit = LegacyWaveLimit;
        public const int MaxTicks = WaveLimit * 360;
        // v2 experimental tuning: shared by all builds, with original Wave/reward formulas retained.
        public const double BaseAttackDamage = 7.5;
        public const double BaseAttackRange = 150;
        public const double GrowthRadius = 150;
        public const int EnemyLimit = 96;
        readonly MetaProgress meta;
        readonly List<EnemyState> enemies = new List<EnemyState>();
        readonly List<BranchState> branches = new List<BranchState>();
        readonly List<AttackEvent> attacks = new List<AttackEvent>();
        // Transient presentation notifications; excluded from saves and all battle decisions.
        readonly List<EnemyState> defeatedThisTick = new List<EnemyState>();
        public const int FieldLimit = 96;
        readonly List<InkFieldState> fields = new List<InkFieldState>();
        uint spawnRng, growthRng;
        int nextEnemyId, growthCount, repairCount, lastBossWave;
        double rewardPoints;
        int[] spawnedByType = new int[7], killedByType = new int[7], leakedByType = new int[7];
        public int[] SpawnedByType { get { return (int[])spawnedByType.Clone(); } }
        public int[] KilledByType { get { return (int[])killedByType.Clone(); } }
        public int[] LeakedByType { get { return (int[])leakedByType.Clone(); } }
        public double RewardPoints { get { return rewardPoints; } }
        public int RepairCount { get { return repairCount; } }
        public int RepairCost { get { return (int)Math.Ceiling(25 * Math.Pow(1.5, repairCount)); } }
        double spawnCredit, growthCharge;
        bool rewardClaimed;
        int[] levels = new int[7];
        public int[] UpgradeLevels { get { return (int[])levels.Clone(); } }
        public WeaponKind Weapon { get; private set; }
        public IReadOnlyList<InkFieldState> Fields { get { return fields.AsReadOnly(); } }
        public BuildPreset Build { get; private set; }
        public int IncomeEarned { get; private set; }
        public double ElapsedSeconds { get { return TickCount / 30.0; } }
        public double IncomePerMinute { get { return TickCount == 0 ? 0 : IncomeEarned * 1800.0 / TickCount; } }
        public int Seed { get; private set; }
        public RunStatus State { get; private set; }
        public GrowthPolicy Policy { get; private set; }
        public int TickCount { get; private set; }
        public int Wave { get; private set; }
        public int CompletedWave { get; private set; }
        public int CoreHp { get; private set; }
        public int RunInk { get; private set; }
        public int Kills { get; private set; }
        public int Leaks { get; private set; }
        public int CrystalsReward { get; private set; }
        public double EffectiveDamage { get; private set; }
        public IReadOnlyList<EnemyState> Enemies { get { return enemies.AsReadOnly(); } }
        public IReadOnlyList<BranchState> Branches { get { return branches.AsReadOnly(); } }
        public IReadOnlyList<AttackEvent> Attacks { get { return attacks.AsReadOnly(); } }
        public IReadOnlyList<EnemyState> DefeatedThisTick { get { return defeatedThisTick.AsReadOnly(); } }
        public int TipCount { get { int count = 0; foreach (var b in branches) if (b.IsTip) count++; return count; } }
        public double RootSupply { get { return 12 * Math.Pow(1.09, levels[2]) * (1 + .035 * meta.Supply); } }
        public int Tier { get; }
        public RunSimulator(int seed, MetaProgress meta = null, int tier = 1)
        {
            TierBalance.Get(tier); Tier = tier;
            Seed = seed; this.meta = (meta ?? new MetaProgress()).Snapshot();
            spawnRng = unchecked((uint)seed) ^ 0xa341316cu; growthRng = unchecked((uint)seed) ^ 0xc8013ea4u;
            State = RunStatus.Running; CoreHp = 10; Wave = 1;
            branches.Add(new BranchState { Id = 0, ParentId = -1 });
            for (int i = 0; i < 3; i++) AddBranch(branches[0], i * Math.PI * 2 / 3, 70);
        }
        public static double BaseHp(int wave) { ValidateLongWave(wave); if(wave>LegacyWaveLimit)return BaseHp(LegacyWaveLimit)*Math.Pow(1+.036*(wave-LegacyWaveLimit),2); return 16 * Math.Pow(1.072, wave - 1) * (1 + .12 * Math.Floor((wave - 1) / 10.0)); }
        public static double SpawnRate(int wave) { ValidateLongWave(wave); return Math.Min(4.8, .50 + .027 * wave + .0007 * wave * wave); }
        public double Pressure(BranchState branch) { return Math.Min(1.65, Math.Max(.45, Math.Sqrt((RootSupply / TipCount) * Math.Exp(-.0013 * branch.PathLength) / 3))); }
        public int UpgradeMaxLevel(UpgradeKind kind)
        {
            int index=(int)kind;if(index<0||index>=levels.Length)throw new ArgumentOutOfRangeException(nameof(kind));
            int[] caps={RulesVersion>=6?100:30,25,25,9,10,10,HarvestMaxLevel};return caps[index];
        }
        public int UpgradeCost(UpgradeKind kind)
        {
            int index = (int)kind; if (index < 0 || index >= levels.Length) throw new ArgumentOutOfRangeException("kind");
            if (kind == UpgradeKind.Harvest) return (int)Math.Ceiling(HarvestBaseCost * Math.Pow(HarvestCostGrowth, levels[6]));
            int[] costs = { 10, 12, 15, 22, 12, 18 }; int l = levels[index];
            if(RulesVersion>=6 && index==0 && l>=30)return (int)Math.Ceiling(DamageCostAt30*Math.Pow(1+.03*(l-30),2));
            return (int)Math.Ceiling(costs[index] * Math.Pow(1.16, l) * (1 + .008 * l * l));
        }
        public bool TryBuy(UpgradeKind kind)
        {
            int i = (int)kind; if (i < 0 || i >= levels.Length || State != RunStatus.Running) return false;
            int[] caps = { UpgradeMaxLevel(UpgradeKind.Damage), 25, 25, 9, 10, 10, HarvestMaxLevel }; int cost = UpgradeCost(kind);
            if (levels[i] >= caps[i] || RunInk < cost) return false;
            RunInk -= cost; levels[i]++; return true;
        }
        public bool TryRepair()
        {
            if (State != RunStatus.Running || CoreHp >= 10 || repairCount >= 5 || RunInk < RepairCost) return false;
            RunInk -= RepairCost; CoreHp = Math.Min(10, CoreHp + 2); repairCount++; return true;
        }
        public void SetPolicy(GrowthPolicy policy)
        {
            if (!Enum.IsDefined(typeof(GrowthPolicy), policy)) throw new ArgumentOutOfRangeException("policy");
            if (State == RunStatus.Running) Policy = policy;
        }
        public void SetBuild(BuildPreset build)
        {
            if (!Enum.IsDefined(typeof(BuildPreset), build)) throw new ArgumentOutOfRangeException("build");
            if (State == RunStatus.Running) Build = build;
        }
        public void SetWeapon(WeaponKind weapon)
        {
            if (!Enum.IsDefined(typeof(WeaponKind), weapon)) throw new ArgumentOutOfRangeException(nameof(weapon));
            // Existing cooldowns and fields survive a switch; switching grants no free shot.
            if (State == RunStatus.Running) Weapon = weapon;
        }
        void AutoBuy()
        {
            if (RulesVersion == LegacyRulesVersion) { AutoBuyLegacy(); return; }
            if (Build == BuildPreset.Manual) return;
            if (Build == BuildPreset.Farming)
            {
                int harvestTarget = Math.Min(HarvestMaxLevel, Math.Max(0, Wave < 15 ? 0 : 1 + (Wave - 15) / 3));
                if (levels[6] < harvestTarget) { TryBuy(UpgradeKind.Harvest); return; }
                AutoBuyLegacy(); return;
            }
            int next = levels[3] + 1;
            int branchCap = RulesVersion >= 4 ? 9 : 6;
            // Late branches still need paid supply first; unfinished older runs keep their purchase order.
            if (RulesVersion >= 4 && levels[3] >= 6 && levels[3] < branchCap && levels[0] >= next && levels[2] < 2 * next)
            { TryBuy(UpgradeKind.Supply); return; }
            if (levels[3] < branchCap && levels[2] >= 2 * next && levels[0] >= next) { TryBuy(UpgradeKind.Branch); return; }
            UpgradeKind[] cycle = { UpgradeKind.Supply, UpgradeKind.Speed, UpgradeKind.Damage, UpgradeKind.Width, UpgradeKind.Bleed, UpgradeKind.Damage };
            int[] caps = { UpgradeMaxLevel(UpgradeKind.Damage), 25, 25, 9, 10, 10 };
            for (int i = 0; i < cycle.Length; i++)
            {
                var kind = cycle[autoPurchaseCursor % cycle.Length];
                if (levels[(int)kind] >= caps[(int)kind]) { autoPurchaseCursor = (autoPurchaseCursor + 1) % cycle.Length; continue; }
                if (TryBuy(kind)) autoPurchaseCursor = (autoPurchaseCursor + 1) % cycle.Length;
                return;
            }
        }
        void AutoBuyLegacy()
        {
            if (Build == BuildPreset.Manual) return;
            // Reserve the entire balance for planned branches; no cheaper fallback can consume it.
            int targetBranches = Build == BuildPreset.Farming ? 6 : 9;
            int milestones = Build == BuildPreset.Farming ? 2 : 1;
            if (levels[3] < targetBranches && levels[0] + levels[1] + levels[2] >= levels[3] * milestones)
            { TryBuy(UpgradeKind.Branch); return; }
            UpgradeKind[] cycle = Build == BuildPreset.Farming
                ? new[] { UpgradeKind.Supply, UpgradeKind.Speed, UpgradeKind.Width, UpgradeKind.Damage, UpgradeKind.Bleed }
                : new[] { UpgradeKind.Damage, UpgradeKind.Supply, UpgradeKind.Speed, UpgradeKind.Damage, UpgradeKind.Width };
            int count = 0; foreach (int level in levels) count += level;
            // Wait for the selected upgrade rather than buying every affordable low-cost option.
            for (int offset = 0; offset < cycle.Length; offset++)
            {
                UpgradeKind kind = cycle[(count + offset) % cycle.Length];
                int[] caps = { UpgradeMaxLevel(UpgradeKind.Damage), 25, 25, 9, 10, 10 };
                if (levels[(int)kind] >= caps[(int)kind]) continue;
                TryBuy(kind); return;
            }
        }
        public bool ClaimReward(MetaProgress target)
        {
            if (target == null) throw new ArgumentNullException("target");
            if (State == RunStatus.Running || State == RunStatus.CapacityExceeded || rewardClaimed) return false;
            target.Snapshot();
            if (target.Crystals > int.MaxValue - CrystalsReward) return false;
            target.Crystals += CrystalsReward; rewardClaimed = true; return true;
        }
        public void Tick()
        {
            attacks.Clear(); defeatedThisTick.Clear(); if (State != RunStatus.Running) return;
            TickCount++; if (TickCount % 30 == 0) AutoBuy(); Wave = Math.Min(RunWaveLimit, (TickCount - 1) / 360 + 1);
            int phase = (TickCount - 1) % 360;
            if (phase == 0 && Wave % 10 == 0 && lastBossWave != Wave)
            {
                if (!Spawn(EnemyKind.Boss)) { StopAtCapacity(); return; }
                lastBossWave = Wave;
            }
            if (phase < 270)
            {
                spawnCredit += SpawnRate(Wave) / TickRate;
                while (spawnCredit >= 1) { spawnCredit -= 1; if (!Spawn()) { StopAtCapacity(); return; } }
            }
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                var e = enemies[i]; double distance = Math.Sqrt(e.X * e.X + e.Y * e.Y);
                double step = e.Speed / TickRate;
                if (RulesVersion >= 8 && e.Kind == EnemyKind.Leech && distance <= 36 + step)
                {
                    if (distance > 36) { e.X *= 36 / distance; e.Y *= 36 / distance; }
                    if (EnemyAbilityPhase(e) == 0) { CoreHp = Math.Max(0, CoreHp - 1); e.Hp = Math.Min(e.MaxHp, e.Hp + e.MaxHp * .10); }
                    continue;
                }
                if (distance <= 12 + step) { CoreHp = Math.Max(0, CoreHp - (e.Kind == EnemyKind.Boss ? 4 : e.Kind == EnemyKind.Heavy ? 2 : 1)); Leaks++; leakedByType[(int)e.Kind]++; enemies.RemoveAt(i); }
                else { e.X *= (distance - step) / distance; e.Y *= (distance - step) / distance; }
            }
            if (CoreHp == 0) { Finish(RunStatus.Defeated); return; }
            foreach (var b in branches)
            {
                if (State != RunStatus.Running) break;
                if (!b.IsTip || b.NextAttack > TickCount) continue;
                EnemyState target = null; double eta = double.MaxValue;
                double range = Weapon == WeaponKind.Enso ? 150 + 4 * levels[4] : (BaseAttackRange + 4 * levels[4]) * (Weapon == WeaponKind.Pierce ? 1.2 : 1);
                foreach (var e in enemies)
                {
                    double dx = e.X - b.X, dy = e.Y - b.Y;
                    if (dx * dx + dy * dy > range * range || (Weapon == WeaponKind.Enso && dx * dx + dy * dy < 20 * 20)) continue;
                    double candidate = Math.Sqrt(e.X * e.X + e.Y * e.Y) / e.Speed;
                    if (target == null || candidate < eta || (candidate == eta && (e.Hp < target.Hp || (e.Hp == target.Hp && e.Id < target.Id)))) { target = e; eta = candidate; }
                }
                if (target == null) continue;
                double pressure = Pressure(b), baseDamage = BaseAttackDamage * (RulesVersion>=6 && levels[0]>30?Math.Pow(1.10,30)*Math.Pow(1.12,levels[0]-30):Math.Pow(1.10, levels[0])) * (RulesVersion>=9?meta.DamageMultiplier:RulesVersion>=6?(meta.Damage<=20?1+.025*meta.Damage:1.5*Math.Pow(1+.025*(meta.Damage-20),2)):(1 + .025 * meta.Damage));
                double damage = baseDamage * pressure * (Weapon == WeaponKind.Pierce ? .8 : Weapon == WeaponKind.Bleed ? .45 : Weapon == WeaponKind.Sweep ? .85 : Weapon == WeaponKind.Enso ? .8 : Weapon == WeaponKind.Resonance ? 1 : 1);
                double interval = Weapon == WeaponKind.Pierce ? 1.35 : Weapon == WeaponKind.Bleed ? 1.1 : Weapon == WeaponKind.Sweep ? 1.2 : Weapon == WeaponKind.Enso ? 1.35 : Weapon == WeaponKind.Resonance ? 1.15 : 1;
                b.NextAttack = TickCount + (int)Math.Ceiling(interval * TickRate / Math.Min(4.5, 1.25 * Math.Pow(1.07, levels[1])));
                if (Weapon == WeaponKind.Bleed)
                    AddField(target.X, target.Y, 32 + 3 * levels[4] + 2 * levels[5], baseDamage * pressure * .85 * (1 + .12 * levels[5]) / TickRate, 48);
                else if (levels[5] > 0)
                    AddField(target.X, target.Y, 10 + 2 * levels[5], baseDamage * .08 * levels[5] / TickRate, 21);
                if (Weapon == WeaponKind.Pierce) Pierce(b, target, range, damage, pressure);
                else if (Weapon == WeaponKind.Sweep || Weapon == WeaponKind.Enso) AreaHit(b, target, range, damage, pressure);
                else if (Weapon == WeaponKind.Resonance) ChainHit(b, target, damage, pressure);
                else Hit(b, target, damage, pressure);
            }
            if (State != RunStatus.Running) return;
            RemoveExpiredFields();
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                if (State != RunStatus.Running) break;
                var e = enemies[i]; int stacks = 0; double damage = 0;
                // The oldest two overlapping live fields win across all weapon types.
                foreach (var f in fields) { double dx = f.X - e.X, dy = f.Y - e.Y; if (dx * dx + dy * dy <= f.Radius * f.Radius) { damage += f.DamagePerTick; if (++stacks == 2) break; } }
                if (damage > 0) Hurt(e, damage);
            }
            double threshold = (4 + growthCount) * (1 - Math.Min(.35, .02 * meta.Growth));
            if (growthCharge >= threshold && branches.Count < 64 && Grow()) { growthCharge -= threshold; growthCount++; }
            if (State == RunStatus.CapacityExceeded) return;
            CompletedWave = TickCount / 360;
            if (TickCount >= RunTickLimit) Finish(RunStatus.TestCompleted);
        }
        void StopAtCapacity()
        {
            State = RunStatus.CapacityExceeded; CompletedWave = TickCount / 360; CrystalsReward = 0;
        }
        void Finish(RunStatus status)
        {
            State = status; CompletedWave = (TickCount - (status == RunStatus.Defeated ? 1 : 0)) / 360;
            CrystalsReward = CalculateCrystalsReward(rewardPoints, RulesVersion, Tier);
        }
        bool Spawn(EnemyKind? forced = null)
        {
            if (enemies.Count >= EnemyLimit) return false;
            // Initial hypothesis: fast 25% from wave4, heavy 20% from wave8; otherwise normal.
            double roll = Random01(ref spawnRng), angle = Random01(ref spawnRng) * 2 * Math.PI;
            var kind = forced ?? (Wave >= 15 && roll >= .80 ? EnemyKind.Splitter : Wave >= 8 && roll < .20 ? EnemyKind.Heavy : Wave >= 4 && roll < (Wave >= 8 ? .45 : .25) ? EnemyKind.Fast : EnemyKind.Normal);
            if (!forced.HasValue && RulesVersion >= 8 && Tier >= 2)
            {
                if (Wave >= 10 && roll >= .70 && roll < .80) kind = EnemyKind.ShieldHalo;
                else if (Wave >= 6 && roll >= .60 && roll < .70) kind = EnemyKind.Leech;
            }
            double hp = (RulesVersion >= 7 ? TierBalance.EnemyBaseHp(Tier, Wave) : BaseHp(Wave)) * (kind == EnemyKind.Leech ? 1.4 : kind == EnemyKind.ShieldHalo ? 1.8 : kind == EnemyKind.Boss ? 12 : kind == EnemyKind.Splitter ? .9 : kind == EnemyKind.Heavy ? 3.2 : kind == EnemyKind.Fast ? .65 : 1);
            enemies.Add(new EnemyState { Id = ++nextEnemyId, Kind = kind, X = 360 * Math.Cos(angle), Y = 360 * Math.Sin(angle), Hp = hp, MaxHp = hp, SpawnWave = Wave, Speed = 35 * (RulesVersion>=5?EnemySpeedFactor(Wave):(1 + .006 * (Wave - 1))) * (kind == EnemyKind.Leech ? .7 : kind == EnemyKind.ShieldHalo ? .8 : kind == EnemyKind.Boss ? .55 : kind == EnemyKind.Heavy ? .65 : kind == EnemyKind.Fast ? 1.8 : 1) });
            spawnedByType[(int)kind]++;
            return true;
        }
        public int EnemyAbilityPhase(EnemyState enemy)
        { return (int)((TickCount + enemy.Id * 17L) % (enemy.Kind == EnemyKind.Leech ? 90 : 120)); }
        public bool IsShieldActive(EnemyState enemy)
        { return enemy != null && RulesVersion >= 8 && enemy.Kind == EnemyKind.ShieldHalo && EnemyAbilityPhase(enemy) < 75; }
        public bool IsLeechAttached(EnemyState enemy)
        { return enemy != null && RulesVersion >= 8 && enemy.Kind == EnemyKind.Leech && enemy.X * enemy.X + enemy.Y * enemy.Y <= 36 * 36 + 1e-6; }
        void Hurt(EnemyState enemy, double damage)
        {
            if (enemy == null || enemy.Hp <= 0 || !enemies.Contains(enemy) || damage <= 0) return;
            if (IsShieldActive(enemy)) damage *= .35;
            EffectiveDamage += Math.Min(enemy.Hp, damage); enemy.Hp -= damage;
            if (enemy.Hp > 0) return;
            Kills++; killedByType[(int)enemy.Kind]++; enemies.Remove(enemy);
            defeatedThisTick.Add(enemy);
            if (enemy.IsSplitChild) return;
            growthCharge++;
            double yield = enemy.Kind == EnemyKind.Leech || enemy.Kind == EnemyKind.ShieldHalo ? 2 : enemy.Kind == EnemyKind.Boss ? 12 : enemy.Kind == EnemyKind.Splitter ? 1.4 : enemy.Kind == EnemyKind.Heavy ? 2 : enemy.Kind == EnemyKind.Fast ? 1.1 : 1;
            rewardPoints += yield * HarvestMultiplier;
            int reward = (int)Math.Round(4 * yield * (RulesVersion>=5?RewardWaveFactor(enemy.SpawnWave):(1 + .018 * enemy.SpawnWave)) * (1 + .03 * meta.Harvest), MidpointRounding.ToEven);
            RunInk += reward; IncomeEarned += reward;
            if (enemy.Kind == EnemyKind.Splitter)
            {
                if (enemies.Count > EnemyLimit - 2) { StopAtCapacity(); return; }
                for (int i = 0; i < 2; i++)
                {
                    enemies.Add(new EnemyState { Id = ++nextEnemyId, Kind = EnemyKind.Splitter, IsSplitChild = true, X = enemy.X + (i == 0 ? -4 : 4), Y = enemy.Y,
                        Hp = enemy.MaxHp * .35, MaxHp = enemy.MaxHp * .35, Speed = enemy.Speed, SpawnWave = enemy.SpawnWave });
                    spawnedByType[(int)EnemyKind.Splitter]++;
                }
            }
        }
        bool Grow()
        {
            BranchState chosen = null; double best = double.NegativeInfinity;
            foreach (var b in branches)
            {
                if (!b.IsTip) continue;
                if (Math.Sqrt(b.X * b.X + b.Y * b.Y) >= GrowthRadius - 1 && TipCount >= Math.Min(12, 3 + levels[3])) continue;
                double threat = 0;
                foreach (var e in enemies) { double dx = e.X - b.X, dy = e.Y - b.Y; threat += (1 + e.Hp / e.MaxHp) / (30 + Math.Sqrt(dx * dx + dy * dy)); }
                // Bounded prototype candidate scoring; full GDD 12-sector threat remains future work.
                double score = Policy == GrowthPolicy.Intercept ? threat : Policy == GrowthPolicy.Focus ? -b.PathLength * .001 + threat * .1 : -b.PathLength * .001;
                score += Random01(ref growthRng) * .001;
                if (score > best) { best = score; chosen = b; }
            }
            if (chosen == null) return false;
            double angle = Math.Atan2(chosen.Y, chosen.X);
            bool split = TipCount < Math.Min(12, 3 + levels[3]) && branches.Count <= 62;
            chosen.IsTip = false;
            double length = 25 + Random01(ref growthRng) * 30;
            double splitAngle = RulesVersion >= 3 ? .65 : .35;
            AddBranch(chosen, angle + (split ? -splitAngle : (Random01(ref growthRng) - .5) * .5), length);
            if (split) AddBranch(chosen, angle + splitAngle, length);
            return true;
        }
        void AddBranch(BranchState parent, double angle, double length)
        {
            double x = parent.X + Math.Cos(angle) * length, y = parent.Y + Math.Sin(angle) * length;
            if (Math.Sqrt(parent.X * parent.X + parent.Y * parent.Y) >= GrowthRadius - 1)
            { x = GrowthRadius * Math.Cos(angle); y = GrowthRadius * Math.Sin(angle); }
            double r = Math.Sqrt(x * x + y * y); if (r > GrowthRadius) { x *= GrowthRadius / r; y *= GrowthRadius / r; }
            double dx = x - parent.X, dy = y - parent.Y;
            branches.Add(new BranchState { Id = branches.Count, ParentId = parent.Id, X = x, Y = y, PathLength = parent.PathLength + Math.Sqrt(dx * dx + dy * dy), IsTip = true });
        }
        static double Random01(ref uint state)
        {
            if (state == 0) state = 0x6d2b79f5u;
            state ^= state << 13; state ^= state >> 17; state ^= state << 5;
            return state / 4294967296.0;
        }
        void RemoveExpiredFields()
        {
            for (int i = fields.Count - 1; i >= 0; i--)
                if (fields[i].UntilTick <= TickCount) fields.RemoveAt(i);
        }
        void AddField(double x, double y, double radius, double damage, int duration)
        {
            RemoveExpiredFields();
            // Preserve the oldest-field priority. At capacity the new field is discarded.
            if (fields.Count >= FieldLimit) return;
            fields.Add(new InkFieldState { X = x, Y = y, Radius = radius, DamagePerTick = damage, UntilTick = TickCount + duration, Weapon = Weapon });
        }
        void Hit(BranchState branch, EnemyState target, double damage, double pressure)
        {
            if (State != RunStatus.Running || target.Hp <= 0 || !enemies.Contains(target)) return;
            attacks.Add(new AttackEvent { BranchId = branch.Id, EnemyId = target.Id, X = branch.X, Y = branch.Y, TargetX = target.X, TargetY = target.Y, Damage = damage, Pressure = pressure, Weapon = Weapon });
            Hurt(target, damage);
        }
        void AreaHit(BranchState branch, EnemyState target, double range, double damage, double pressure)
        {
            var targets = new List<EnemyState>();
            double ax = target.X - branch.X, ay = target.Y - branch.Y;
            double al = Math.Sqrt(ax * ax + ay * ay);
            foreach (var e in enemies)
            {
                double x = e.X - branch.X, y = e.Y - branch.Y, distance2 = x*x+y*y;
                if (distance2 > range*range) continue;
                if (Weapon == WeaponKind.Enso) { if (distance2 < 400) continue; }
                else if (al > 1e-9 && distance2 > 1e-18 && (x*ax+y*ay)/(Math.Sqrt(distance2)*al) < .7071067811865476) continue;
                targets.Add(e);
            }
            targets.Sort((a,b) => { double ad=(a.X-branch.X)*(a.X-branch.X)+(a.Y-branch.Y)*(a.Y-branch.Y), bd=(b.X-branch.X)*(b.X-branch.X)+(b.Y-branch.Y)*(b.Y-branch.Y); int c=ad.CompareTo(bd); return c!=0?c:a.Id.CompareTo(b.Id); });
            int cap = Weapon == WeaponKind.Enso ? 6 : 5;
            for (int i=0;i<Math.Min(cap,targets.Count);i++) Hit(branch,targets[i],damage,pressure);
        }
        void ChainHit(BranchState branch, EnemyState target, double damage, double pressure)
        {
            var hit = new HashSet<int>();
            for (int step=0;step<4 && target!=null;step++)
            {
                double x=target.X,y=target.Y; hit.Add(target.Id); Hit(branch,target,damage,pressure); damage*=.75;
                EnemyState next=null; double nearest=(70+2*levels[4])*(70+2*levels[4]);
                foreach(var e in enemies) { if(hit.Contains(e.Id)) continue; double dx=e.X-x,dy=e.Y-y,d=dx*dx+dy*dy;
                    if(d<nearest || (d==nearest && (next==null || e.Id<next.Id))) { next=e; nearest=d; } }
                target=next;
            }
        }
        void Pierce(BranchState branch, EnemyState target, double range, double damage, double pressure)
        {
            double dx = target.X - branch.X, dy = target.Y - branch.Y;
            double length = Math.Sqrt(dx * dx + dy * dy);
            if (length < 1e-9) { Hit(branch, target, damage, pressure); return; }
            dx /= length; dy /= length;
            double halfWidth = 12 + 2 * levels[4];
            var candidates = new List<EnemyState>();
            foreach (var enemy in enemies)
            {
                double x = enemy.X - branch.X, y = enemy.Y - branch.Y;
                double projection = x * dx + y * dy;
                double perpendicular = x * dy - y * dx;
                if (projection >= 0 && projection <= range && x * x + y * y <= range * range && Math.Abs(perpendicular) <= halfWidth)
                    candidates.Add(enemy);
            }
            candidates.Sort((a, b) =>
            {
                double ap = (a.X - branch.X) * dx + (a.Y - branch.Y) * dy;
                double bp = (b.X - branch.X) * dx + (b.Y - branch.Y) * dy;
                int order = ap.CompareTo(bp);
                return order != 0 ? order : a.Id.CompareTo(b.Id);
            });
            for (int i = 0; i < Math.Min(3, candidates.Count); i++) Hit(branch, candidates[i], damage, pressure);
        }
    }
}
