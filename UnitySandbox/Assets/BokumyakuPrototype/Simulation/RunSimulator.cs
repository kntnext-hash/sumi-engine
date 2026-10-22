using System;
using System.Collections.Generic;

namespace Bokumyaku.Simulation
{
    public enum WeaponKind { Point, Pierce, Bleed }
    public enum BuildPreset { Manual, Farming, Record }
    public enum RunStatus { Running, Defeated, TestCompleted, CapacityExceeded }
    public enum UpgradeKind { Damage, Speed, Supply, Branch, Width, Bleed }
    public enum GrowthPolicy { Balanced, Intercept, Focus }
    public enum EnemyKind { Normal, Fast, Heavy }
    public enum MetaUpgrade { Damage, Supply, Growth, Harvest }

    [Serializable]
    public sealed class MetaProgress
    {
        public int Crystals;
        public int Damage, Supply, Growth, Harvest;
        public int Level(MetaUpgrade kind) { return kind == MetaUpgrade.Damage ? Damage : kind == MetaUpgrade.Supply ? Supply : kind == MetaUpgrade.Growth ? Growth : Harvest; }
        public int Cost(MetaUpgrade kind) { return (int)Math.Ceiling((kind == MetaUpgrade.Supply ? 10 : kind == MetaUpgrade.Harvest ? 12 : 8) * Math.Pow(1.48, Level(kind))); }
        public bool TryBuy(MetaUpgrade kind)
        {
            if (!Enum.IsDefined(typeof(MetaUpgrade), kind) || Level(kind) >= 20 || Crystals < Cost(kind)) return false;
            Crystals -= Cost(kind);
            switch (kind) { case MetaUpgrade.Damage: Damage++; break; case MetaUpgrade.Supply: Supply++; break; case MetaUpgrade.Growth: Growth++; break; default: Harvest++; break; }
            return true;
        }
        internal MetaProgress Snapshot()
        {
            if (Crystals < 0 || Damage < 0 || Damage > 20 || Supply < 0 || Supply > 20 || Growth < 0 || Growth > 20 || Harvest < 0 || Harvest > 20) throw new ArgumentOutOfRangeException("meta");
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
    public sealed class RunSimulator
    {
        public const int TickRate = 30;
        public const int WaveLimit = 60;
        public const int MaxTicks = WaveLimit * 360;
        // v2 experimental tuning: shared by all builds, with original Wave/reward formulas retained.
        public const double BaseAttackDamage = 8;
        public const double BaseAttackRange = 150;
        public const double GrowthRadius = 150;
        public const int EnemyLimit = 96;
        readonly MetaProgress meta;
        readonly List<EnemyState> enemies = new List<EnemyState>();
        readonly List<BranchState> branches = new List<BranchState>();
        readonly List<AttackEvent> attacks = new List<AttackEvent>();
        public const int FieldLimit = 96;
        readonly List<InkFieldState> fields = new List<InkFieldState>();
        uint spawnRng, growthRng;
        int nextEnemyId, growthCount;
        double spawnCredit, growthCharge;
        bool rewardClaimed;
        readonly int[] levels = new int[6];
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
        public int TipCount { get { int count = 0; foreach (var b in branches) if (b.IsTip) count++; return count; } }
        public double RootSupply { get { return 12 * Math.Pow(1.09, levels[2]) * (1 + .035 * meta.Supply); } }
        public RunSimulator(int seed, MetaProgress meta = null)
        {
            Seed = seed; this.meta = (meta ?? new MetaProgress()).Snapshot();
            spawnRng = unchecked((uint)seed) ^ 0xa341316cu; growthRng = unchecked((uint)seed) ^ 0xc8013ea4u;
            State = RunStatus.Running; CoreHp = 10; Wave = 1;
            branches.Add(new BranchState { Id = 0, ParentId = -1 });
            for (int i = 0; i < 3; i++) AddBranch(branches[0], i * Math.PI * 2 / 3, 70);
        }
        public static double BaseHp(int wave) { return 16 * Math.Pow(1.072, wave - 1) * (1 + .12 * Math.Floor((wave - 1) / 10.0)); }
        public static double SpawnRate(int wave) { return Math.Min(4.8, .50 + .027 * wave + .0007 * wave * wave); }
        public double Pressure(BranchState branch) { return Math.Min(1.65, Math.Max(.45, Math.Sqrt((RootSupply / TipCount) * Math.Exp(-.0013 * branch.PathLength) / 3))); }
        public int UpgradeCost(UpgradeKind kind)
        {
            int index = (int)kind; if (index < 0 || index >= 6) throw new ArgumentOutOfRangeException("kind");
            int[] costs = { 10, 12, 15, 22, 12, 18 }; int l = levels[index];
            return (int)Math.Ceiling(costs[index] * Math.Pow(1.16, l) * (1 + .008 * l * l));
        }
        public bool TryBuy(UpgradeKind kind)
        {
            int i = (int)kind; if (i < 0 || i >= 6 || State != RunStatus.Running) return false;
            int[] caps = { 30, 25, 25, 9, 10, 10 }; int cost = UpgradeCost(kind);
            if (levels[i] >= caps[i] || RunInk < cost) return false;
            RunInk -= cost; levels[i]++; return true;
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
                int[] caps = { 30, 25, 25, 9, 10, 10 };
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
            attacks.Clear(); if (State != RunStatus.Running) return;
            TickCount++; if (TickCount % 30 == 0) AutoBuy(); Wave = Math.Min(WaveLimit, (TickCount - 1) / 360 + 1);
            int phase = (TickCount - 1) % 360;
            if (phase < 270)
            {
                spawnCredit += SpawnRate(Wave) / TickRate;
                while (spawnCredit >= 1) { spawnCredit -= 1; if (!Spawn()) { State = RunStatus.CapacityExceeded; return; } }
            }
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                var e = enemies[i]; double distance = Math.Sqrt(e.X * e.X + e.Y * e.Y);
                double step = e.Speed / TickRate;
                if (distance <= 12 + step) { CoreHp = Math.Max(0, CoreHp - (e.Kind == EnemyKind.Heavy ? 2 : 1)); Leaks++; enemies.RemoveAt(i); }
                else { e.X *= (distance - step) / distance; e.Y *= (distance - step) / distance; }
            }
            if (CoreHp == 0) { Finish(RunStatus.Defeated); return; }
            foreach (var b in branches)
            {
                if (!b.IsTip || b.NextAttack > TickCount) continue;
                EnemyState target = null; double eta = double.MaxValue;
                double range = (BaseAttackRange + 4 * levels[4]) * (Weapon == WeaponKind.Pierce ? 1.2 : 1);
                foreach (var e in enemies)
                {
                    double dx = e.X - b.X, dy = e.Y - b.Y;
                    if (dx * dx + dy * dy > range * range) continue;
                    double candidate = Math.Sqrt(e.X * e.X + e.Y * e.Y) / e.Speed;
                    if (target == null || candidate < eta || (candidate == eta && (e.Hp < target.Hp || (e.Hp == target.Hp && e.Id < target.Id)))) { target = e; eta = candidate; }
                }
                if (target == null) continue;
                double pressure = Pressure(b), baseDamage = BaseAttackDamage * Math.Pow(1.10, levels[0]) * (1 + .025 * meta.Damage);
                double damage = baseDamage * pressure * (Weapon == WeaponKind.Pierce ? .8 : Weapon == WeaponKind.Bleed ? .45 : 1);
                double interval = Weapon == WeaponKind.Pierce ? 1.35 : Weapon == WeaponKind.Bleed ? 1.1 : 1;
                b.NextAttack = TickCount + (int)Math.Ceiling(interval * TickRate / Math.Min(4.5, 1.25 * Math.Pow(1.07, levels[1])));
                if (Weapon == WeaponKind.Bleed)
                    AddField(target.X, target.Y, 32 + 3 * levels[4] + 2 * levels[5], baseDamage * pressure * .85 * (1 + .12 * levels[5]) / TickRate, 48);
                else if (levels[5] > 0)
                    AddField(target.X, target.Y, 10 + 2 * levels[5], baseDamage * .08 * levels[5] / TickRate, 21);
                if (Weapon == WeaponKind.Pierce) Pierce(b, target, range, damage, pressure);
                else Hit(b, target, damage, pressure);
            }
            RemoveExpiredFields();
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                var e = enemies[i]; int stacks = 0; double damage = 0;
                // The oldest two overlapping live fields win across all weapon types.
                foreach (var f in fields) { double dx = f.X - e.X, dy = f.Y - e.Y; if (dx * dx + dy * dy <= f.Radius * f.Radius) { damage += f.DamagePerTick; if (++stacks == 2) break; } }
                if (damage > 0) Hurt(e, damage);
            }
            double threshold = (4 + growthCount) * (1 - Math.Min(.35, .02 * meta.Growth));
            if (growthCharge >= threshold && branches.Count < 64 && Grow()) { growthCharge -= threshold; growthCount++; }
            CompletedWave = TickCount / 360;
            if (TickCount >= MaxTicks) Finish(RunStatus.TestCompleted);
        }
        void Finish(RunStatus status)
        {
            State = status; CompletedWave = (TickCount - (status == RunStatus.Defeated ? 1 : 0)) / 360;
            CrystalsReward = CompletedWave == 0 ? 0 : (int)Math.Floor(4 + .25 * Math.Pow(CompletedWave, 1.4));
        }
        bool Spawn()
        {
            if (enemies.Count >= EnemyLimit) return false;
            // Initial hypothesis: fast 25% from wave4, heavy 20% from wave8; otherwise normal.
            double roll = Random01(ref spawnRng), angle = Random01(ref spawnRng) * 2 * Math.PI;
            var kind = Wave >= 8 && roll < .20 ? EnemyKind.Heavy : Wave >= 4 && roll < (Wave >= 8 ? .45 : .25) ? EnemyKind.Fast : EnemyKind.Normal;
            double hp = BaseHp(Wave) * (kind == EnemyKind.Heavy ? 3.2 : kind == EnemyKind.Fast ? .65 : 1);
            enemies.Add(new EnemyState { Id = ++nextEnemyId, Kind = kind, X = 360 * Math.Cos(angle), Y = 360 * Math.Sin(angle), Hp = hp, MaxHp = hp, SpawnWave = Wave, Speed = 35 * (1 + .006 * (Wave - 1)) * (kind == EnemyKind.Heavy ? .65 : kind == EnemyKind.Fast ? 1.8 : 1) });
            return true;
        }
        void Hurt(EnemyState enemy, double damage)
        {
            if (enemy == null || enemy.Hp <= 0 || !enemies.Contains(enemy) || damage <= 0) return;
            EffectiveDamage += Math.Min(enemy.Hp, damage); enemy.Hp -= damage;
            if (enemy.Hp > 0) return;
            Kills++; growthCharge++; enemies.Remove(enemy);
            double yield = enemy.Kind == EnemyKind.Heavy ? 2 : enemy.Kind == EnemyKind.Fast ? 1.1 : 1;
            int reward = (int)Math.Round(4 * yield * (1 + .018 * enemy.SpawnWave) * (1 + .03 * meta.Harvest), MidpointRounding.ToEven);
            RunInk += reward; IncomeEarned += reward;
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
            AddBranch(chosen, angle + (split ? -.35 : (Random01(ref growthRng) - .5) * .5), length);
            if (split) AddBranch(chosen, angle + .35, length);
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
            if (target.Hp <= 0 || !enemies.Contains(target)) return;
            attacks.Add(new AttackEvent { BranchId = branch.Id, EnemyId = target.Id, X = branch.X, Y = branch.Y, TargetX = target.X, TargetY = target.Y, Damage = damage, Pressure = pressure, Weapon = Weapon });
            Hurt(target, damage);
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
