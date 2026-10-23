using System;
using System.Collections.Generic;
using Bokumyaku.Simulation;

namespace Bokumyaku.Persistence
{
    [Serializable]
    public sealed class BuildSlot
    {
        public string Name = "構成";
        public WeaponKind Weapon;
        public BuildPreset Build;
        public GrowthPolicy Policy;
        public int Speed = 1;
    }
    [Serializable]
    public sealed class CampaignResult
    {
        public string RunId;
        public int RulesVersion;
        public int Tier = 1;
        public int Seed, CompletedWave, Ticks, Crystals, MilestoneCrystals;
        public RunStatus Status;
        public WeaponKind Weapon;
        public BuildPreset Build;
        public GrowthPolicy Policy;
    }
    [Serializable]
    public sealed class CampaignProgress
    {
        public int SchemaVersion = 4;
        public string Config = "bokumyaku-campaign-v4";
        public long Revision;
        public MetaProgress Meta = new MetaProgress();
        public bool HasActiveRun;
        public RunSnapshot ActiveRun;
        public string ActiveRunId = "";
        public int ActiveSpeed = 1;
        public string LastSettledRunId = "";
        public CampaignResult LastResult;
        // Serialized legacy fields remain exact mirrors of Tier 1, never global maxima.
        public int BestWave, TotalRuns, ClaimedMilestoneWave;
        public int SelectedTier = 1;
        public int[] BestWaveByTier = new int[TierBalance.Count];
        public int[] ClaimedMilestoneWaveByTier = new int[TierBalance.Count];
        public long TotalCrystals;
        public List<CampaignResult> History = new List<CampaignResult>();
        public BuildSlot[] Slots = { new BuildSlot { Name = "構成1" }, new BuildSlot { Name = "構成2" }, new BuildSlot { Name = "構成3" } };
    }
}
