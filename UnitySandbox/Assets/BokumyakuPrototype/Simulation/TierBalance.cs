using System;

namespace Bokumyaku.Simulation
{
    // Independent tuning table. Run/save integration follows the versioned Tier migration.
    // These are original prototype coefficients, not values copied from another game.
    public static class TierBalance
    {
        public const int Count = 3;
        public const int UnlockCompletedWave = 100;
        public const int TableVersion = 1;

        public readonly struct Definition
        {
            public readonly string DisplayName;
            public readonly double StartingHpMultiplier, HpMultiplierAt100, CrystalMultiplier;
            public Definition(string name, double start, double at100, double crystals)
            { DisplayName = name; StartingHpMultiplier = start; HpMultiplierAt100 = at100; CrystalMultiplier = crystals; }
        }

        static readonly Definition[] Definitions =
        {
            new Definition("初墨・一段", 1, 1, 1),
            new Definition("幽墨・二段", 2.5, 3, 1.6),
            new Definition("朱天・三段", 6, 8.4, 2.5)
        };

        public static Definition Get(int tier)
        {
            if (tier < 1 || tier > Count) throw new ArgumentOutOfRangeException(nameof(tier));
            return Definitions[tier - 1];
        }

        public static double EnemyBaseHp(int tier, int wave)
        {
            var definition = Get(tier);
            double baseline = RunSimulator.BaseHp(wave); // Validates the existing Wave range.
            double progress = Math.Min(1, (wave - 1) / 99.0);
            double multiplier = definition.StartingHpMultiplier
                + (definition.HpMultiplierAt100 - definition.StartingHpMultiplier) * progress;
            return baseline * multiplier;
        }

        public static bool IsUnlocked(int tier, int[] bestCompletedWaves)
        {
            Get(tier);
            if (bestCompletedWaves == null || bestCompletedWaves.Length != Count)
                throw new ArgumentException("One highest completed Wave per Tier is required.", nameof(bestCompletedWaves));
            for (int i = 0; i < Count; i++)
                if (bestCompletedWaves[i] < 0 || bestCompletedWaves[i] > RunSimulator.LongRunWaveLimit)
                    throw new ArgumentOutOfRangeException(nameof(bestCompletedWaves));
            for (int i = 0; i < tier - 1; i++)
                if (bestCompletedWaves[i] < UnlockCompletedWave) return false;
            return true;
        }
    }
}
