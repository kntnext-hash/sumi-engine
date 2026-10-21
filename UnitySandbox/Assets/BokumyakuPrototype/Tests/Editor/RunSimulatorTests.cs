using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Bokumyaku.Simulation;
using NUnit.Framework;

namespace Bokumyaku.Tests
{
    public sealed class RunSimulatorTests
    {
        static void Finish(RunSimulator run)
        {
            for (int i = 0; i <= RunSimulator.MaxTicks && run.State == RunStatus.Running; i++) run.Tick();
            Assert.AreNotEqual(RunStatus.Running, run.State, "A run must terminate within the bounded prototype.");
        }

        // Compare state, geometry, enemies AND rendered attack events, not just the final score.
        static string Snapshot(RunSimulator run)
        {
            var s = new StringBuilder();
            Action<object> add = v => s.Append(Convert.ToString(v, CultureInfo.InvariantCulture)).Append('|');
            add(run.TickCount); add(run.State); add(run.Wave); add(run.CompletedWave); add(run.CoreHp);
            add(run.RunInk); add(run.Kills); add(run.Leaks); add(run.EffectiveDamage); add(run.Policy);
            foreach (int level in run.UpgradeLevels) add(level);
            foreach (var b in run.Branches) { add(b.Id); add(b.ParentId); add(b.X); add(b.Y); add(b.PathLength); add(b.IsTip); }
            foreach (var e in run.Enemies) { add(e.Id); add(e.Kind); add(e.X); add(e.Y); add(e.Hp); add(e.Speed); add(e.SpawnWave); }
            foreach (var a in run.Attacks) { add(a.BranchId); add(a.EnemyId); add(a.TargetX); add(a.TargetY); add(a.Damage); add(a.Pressure); }
            return s.ToString();
        }

        [TestCase(0)] [TestCase(42)] [TestCase(-17)] [TestCase(int.MaxValue)]
        public void SameSeedAndCommandsReplayEveryTick(int seed)
        {
            var a = new RunSimulator(seed); var b = new RunSimulator(seed);
            while (a.State == RunStatus.Running && a.TickCount <= RunSimulator.MaxTicks)
            {
                if (a.TickCount % 120 == 0) { a.TryBuy(UpgradeKind.Damage); b.TryBuy(UpgradeKind.Damage); }
                if (a.TickCount == 600) { a.SetPolicy(GrowthPolicy.Intercept); b.SetPolicy(GrowthPolicy.Intercept); }
                a.Tick(); b.Tick(); Assert.AreEqual(Snapshot(a), Snapshot(b));
            }
            Assert.AreNotEqual(RunStatus.Running, a.State);
        }

        [Test]
        public void TwoTicksPerFrameMatchesOneTickPerFrame()
        {
            var a = new RunSimulator(123); var b = new RunSimulator(123);
            for (int frame = 0; frame < RunSimulator.MaxTicks / 2 && a.State == RunStatus.Running; frame++)
            {
                a.Tick(); a.Tick();
                for (int i = 0; i < 2; i++) b.Tick();
                Assert.AreEqual(Snapshot(a), Snapshot(b));
            }
            Assert.AreEqual(a.State, b.State);
        }

        [Test]
        public void DifferentSeedsProduceDifferentEnemyPositions()
        {
            var a = new RunSimulator(1); var b = new RunSimulator(2);
            for (int i = 0; i < 100; i++) { a.Tick(); b.Tick(); }
            Assert.Greater(a.Enemies.Count, 0); Assert.Greater(b.Enemies.Count, 0);
            Assert.AreNotEqual(a.Enemies[0].X, b.Enemies[0].X);
        }

        [Test]
        public void UnaffordableOrInvalidPurchaseDoesNotMutateState()
        {
            var run = new RunSimulator(42); string before = Snapshot(run);
            foreach (UpgradeKind kind in Enum.GetValues(typeof(UpgradeKind))) Assert.IsFalse(run.TryBuy(kind));
            Assert.IsFalse(run.TryBuy((UpgradeKind)(-1)));
            Assert.IsFalse(run.TryBuy((UpgradeKind)999));
            Assert.AreEqual(before, Snapshot(run));
        }

        [Test]
        public void SuccessfulPurchaseChargesExactCostAndOnlyOneLevel()
        {
            var run = new RunSimulator(42, new MetaProgress { Damage = 20, Harvest = 20 });
            while (run.State == RunStatus.Running && run.RunInk < run.UpgradeCost(UpgradeKind.Damage)) run.Tick();
            int before = run.RunInk, cost = run.UpgradeCost(UpgradeKind.Damage);
            Assert.IsTrue(run.TryBuy(UpgradeKind.Damage));
            Assert.AreEqual(before - cost, run.RunInk); Assert.AreEqual(1, run.UpgradeLevels[0]);
            for (int i = 1; i < 6; i++) Assert.AreEqual(0, run.UpgradeLevels[i]);
        }

        [Test]
        public void UpgradeLevelArrayCannotMutateSimulation()
        {
            var run = new RunSimulator(42); var levels = run.UpgradeLevels; levels[0] = 30;
            Assert.AreEqual(0, run.UpgradeLevels[0]);
        }

        [Test]
        public void MetaIsSnapshottedAtRunStart()
        {
            var meta = new MetaProgress(); var run = new RunSimulator(42, meta);
            double before = run.RootSupply; meta.Supply = 20;
            Assert.AreEqual(before, run.RootSupply);
            Assert.Greater(new RunSimulator(42, meta).RootSupply, before);
        }

        [Test]
        public void BranchPurchaseDilutesPressureAtTheSamePathLength()
        {
            var run = new RunSimulator(42, new MetaProgress { Damage = 20, Harvest = 20, Growth = 20 });
            var reference = run.Branches[1]; double initial = run.Pressure(reference);
            while (run.State == RunStatus.Running && run.RunInk < run.UpgradeCost(UpgradeKind.Branch)) run.Tick();
            Assert.IsTrue(run.TryBuy(UpgradeKind.Branch));
            while (run.State == RunStatus.Running && run.TipCount == 3) run.Tick();
            Assert.AreEqual(4, run.TipCount, "Purchase must result in an actual split after earned growth.");
            Assert.Less(run.Pressure(reference), initial);
        }

        [TestCase(1)] [TestCase(42)] [TestCase(-17)]
        public void NaturalDefeatIsBoundedAndStopsAllEconomy(int seed)
        {
            var run = new RunSimulator(seed); Finish(run);
            Assert.AreEqual(RunStatus.Defeated, run.State); Assert.AreEqual(0, run.CoreHp);
            Assert.LessOrEqual(run.TickCount, RunSimulator.MaxTicks);
            Assert.Greater(run.Leaks, 0); Assert.GreaterOrEqual(run.RunInk, 0);
            int tick = run.TickCount, ink = run.RunInk, kills = run.Kills;
            for (int i = 0; i < 100; i++) run.Tick();
            Assert.AreEqual(tick, run.TickCount); Assert.AreEqual(ink, run.RunInk); Assert.AreEqual(kills, run.Kills);
            Assert.IsFalse(run.TryBuy(UpgradeKind.Damage));
        }

        [Test]
        public void GraphAndEnemyCapsHoldThroughoutRun()
        {
            var run = new RunSimulator(42, new MetaProgress { Damage = 20, Supply = 20, Growth = 20, Harvest = 20 });
            while (run.State == RunStatus.Running && run.TickCount <= RunSimulator.MaxTicks)
            {
                run.TryBuy(UpgradeKind.Branch); run.Tick();
                Assert.LessOrEqual(run.Enemies.Count, RunSimulator.EnemyLimit);
                Assert.LessOrEqual(run.Branches.Count, 64); Assert.LessOrEqual(run.TipCount, 12);
                var ids = new HashSet<int>();
                foreach (var branch in run.Branches)
                {
                    Assert.IsTrue(ids.Add(branch.Id));
                    if (branch.Id != 0) { Assert.IsTrue(ids.Contains(branch.ParentId)); Assert.Less(branch.ParentId, branch.Id); }
                    Assert.IsFalse(double.IsNaN(branch.PathLength)); Assert.GreaterOrEqual(branch.PathLength, 0);
                }
            }
        }

        [Test]
        public void RunningRunCannotClaimAndFinishedRunCannotClaimTwice()
        {
            var meta = new MetaProgress(); var run = new RunSimulator(42);
            Assert.IsFalse(run.ClaimReward(meta)); Assert.AreEqual(0, meta.Crystals);
            Finish(run); Assert.IsTrue(run.ClaimReward(meta));
            Assert.AreEqual(run.CrystalsReward, meta.Crystals);
            Assert.IsFalse(run.ClaimReward(meta)); Assert.AreEqual(run.CrystalsReward, meta.Crystals);
        }

        [Test]
        public void RewardOverflowRejectsWithoutConsumingClaim()
        {
            var run = new RunSimulator(42); Finish(run); Assert.Greater(run.CrystalsReward, 0);
            var meta = new MetaProgress { Crystals = int.MaxValue };
            Assert.IsFalse(run.ClaimReward(meta)); Assert.AreEqual(int.MaxValue, meta.Crystals);
            meta.Crystals = 0; Assert.IsTrue(run.ClaimReward(meta));
        }

        [TestCase(MetaUpgrade.Damage, 8)] [TestCase(MetaUpgrade.Supply, 10)]
        [TestCase(MetaUpgrade.Growth, 8)] [TestCase(MetaUpgrade.Harvest, 12)]
        public void MetaPurchaseCostsCapsAndInsufficientFunds(MetaUpgrade kind, int initialCost)
        {
            var meta = new MetaProgress { Crystals = initialCost - 1 };
            Assert.AreEqual(initialCost, meta.Cost(kind)); Assert.IsFalse(meta.TryBuy(kind));
            Assert.AreEqual(initialCost - 1, meta.Crystals); Assert.AreEqual(0, meta.Level(kind));
            meta.Crystals = 10000000;
            for (int level = 0; level < 20; level++)
            {
                int expected = (int)Math.Ceiling(initialCost * Math.Pow(1.48, level));
                int before = meta.Crystals; Assert.AreEqual(expected, meta.Cost(kind));
                Assert.IsTrue(meta.TryBuy(kind)); Assert.AreEqual(before - expected, meta.Crystals);
                Assert.AreEqual(level + 1, meta.Level(kind));
            }
            int balance = meta.Crystals; Assert.IsFalse(meta.TryBuy(kind)); Assert.AreEqual(balance, meta.Crystals);
        }

        [Test]
        public void InvalidMetaIsRejectedAtRunStart()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new RunSimulator(1, new MetaProgress { Crystals = -1 }));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RunSimulator(1, new MetaProgress { Damage = 21 }));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RunSimulator(1, new MetaProgress { Supply = -1 }));
        }

        [Test]
        public void ThreeRunsRetainMetaAndResetRunCurrency()
        {
            var meta = new MetaProgress(); int earned = 0, spent = 0;
            for (int i = 0; i < 3; i++)
            {
                var run = new RunSimulator(42 + i, meta); Assert.AreEqual(0, run.RunInk);
                Finish(run); Assert.AreEqual(RunStatus.Defeated, run.State);
                Assert.IsTrue(run.ClaimReward(meta)); earned += run.CrystalsReward;
                int cost = meta.Cost(MetaUpgrade.Damage);
                if (meta.TryBuy(MetaUpgrade.Damage)) spent += cost;
                Assert.AreEqual(earned - spent, meta.Crystals);
            }
            Assert.Greater(meta.Damage, 0, "Three natural runs should fund at least one permanent upgrade.");
        }
    }
}
