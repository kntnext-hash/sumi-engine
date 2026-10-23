using System;
namespace Bokumyaku.Simulation
{
    public sealed partial class RunSimulator
    {
        // Rules6: max hit8,126,828.93 and max field/tick506,572.34.
        // ceil(36m/7)*12*96 hits plus36m*96*2 field ticks bounds total below5.165e16.
        // 6e16 is a conservative validation bound, not a damage clamp; legacy stays1e15.
        public const double LongRunEffectiveDamageLimit = 6e16;
        public static readonly double FirstWeekEffectiveDamageLimit = LongRunEffectiveDamageLimit * (Math.Pow(1.18,20) / 1.5);
        public const int LegacyWaveLimit = 60;
        public const int LongRunWaveLimit = 100000;
        public const int LongRunMaxTicks = LongRunWaveLimit * 360;
        // At most4,320,001 regular spawns,10,000 bosses and two children per regular.
        // Regular yield<=2, boss yield12, Harvest<=16 and meta ink multiplier<=1.6.
        // Reward wave factor<9.5; using10 bounds total ink at560,640,128,
        // reward points at140,160,032 and enemy IDs at12,970,003. No silent saturation.
        public const int LongRunIncomeLimit = 600000000;
        public const int LongRunCounterLimit = 13000000;
        public const int LongRunRewardPointsLimit = 150000000;
        public const int LongRunCrystalLimit = 12000000;
        public const int LongRunGrowthChargeLimit = 5000000;
        public int RunWaveLimit { get { return WaveLimitForRules(RulesVersion); } }
        public int RunTickLimit { get { return TickLimitForRules(RulesVersion); } }
        public static int WaveLimitForRules(int rules)
        {
            if(rules<1 || rules>CurrentRulesVersion)throw new ArgumentOutOfRangeException(nameof(rules));
            return rules>=5?LongRunWaveLimit:LegacyWaveLimit;
        }
        public static int TickLimitForRules(int rules) { return WaveLimitForRules(rules)*360; }
        public static int CrystalLimitForRules(int rules) { WaveLimitForRules(rules); return rules>=7?(int)(LongRunCrystalLimit*TierBalance.Get(TierBalance.Count).CrystalMultiplier):rules>=5?LongRunCrystalLimit:80000; }
        static int CalculateCrystalsReward(double points, int rules, int tier)
        { return (int)Math.Floor(points * .08 * (rules >= 7 ? TierBalance.Get(tier).CrystalMultiplier : 1)); }
        public static double RewardWaveFactor(int wave)
        {
            ValidateLongWave(wave);
            return wave<=LegacyWaveLimit?1+.018*wave:2.08+Math.Log(1+(wave-LegacyWaveLimit)/60.0);
        }
        public static double EnemySpeedFactor(int wave)
        {
            ValidateLongWave(wave);
            return Math.Min(2.5,1+.006*(wave-1));
        }
        static void ValidateLongWave(int wave)
        {
            if(wave<1 || wave>LongRunWaveLimit)throw new ArgumentOutOfRangeException(nameof(wave));
        }
    }
}
