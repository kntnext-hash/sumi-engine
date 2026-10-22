using System;
using System.Collections;
using System.Reflection;
using Bokumyaku.Simulation;
using NUnit.Framework;

namespace Bokumyaku.Tests
{
    // Fixtures isolate targeting geometry without exposing mutable state in the production API.
    public sealed class WeaponTests
    {
        static void Set(object target, string name, object value)
        {
            var p = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null) p.SetValue(target, value, null);
            else target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(target, value);
        }
        static IList List(RunSimulator run, string name) => (IList)typeof(RunSimulator).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(run);
        static RunSimulator Fixture(WeaponKind weapon, double length = 0)
        {
            var run = new RunSimulator(42); run.SetWeapon(weapon);
            var branches = List(run, "branches"); branches.Clear();
            var tip = new BranchState(); Set(tip, "Id", 0); Set(tip, "ParentId", -1);
            Set(tip, "IsTip", true); Set(tip, "PathLength", length); branches.Add(tip);
            return run;
        }
        static EnemyState Enemy(RunSimulator run, int id, double x, double y = 0, double speed = .001, double hp = 1000)
        {
            var e = new EnemyState(); Set(e, "Id", id); Set(e, "X", x); Set(e, "Y", y);
            Set(e, "Speed", speed); Set(e, "Hp", hp); Set(e, "MaxHp", hp); Set(e, "SpawnWave", 1);
            List(run, "enemies").Add(e); return e;
        }
        static void DisableFire(RunSimulator run) { foreach (var b in run.Branches) Set(b, "NextAttack", int.MaxValue); }
        static int NextAttack(RunSimulator run) => (int)typeof(BranchState).GetField("NextAttack", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(run.Branches[0]);

        [Test]
        public void PointRemainsDefaultAndHitsOnlyOneTarget()
        {
            Assert.AreEqual(WeaponKind.Point, new RunSimulator(42).Weapon);
            var run = Fixture(WeaponKind.Point); var first = Enemy(run, 1, 80); var second = Enemy(run, 2, 100);
            run.Tick(); Assert.Less(first.Hp, 1000); Assert.AreEqual(1000, second.Hp);
            Assert.AreEqual(1, run.Attacks.Count); Assert.AreEqual(WeaponKind.Point, run.Attacks[0].Weapon);
        }

        [Test]
        public void PierceHitsFirstThreeOnForwardLineButNotBehindSidewaysOrBeyondRange()
        {
            var run = Fixture(WeaponKind.Pierce);
            var first = Enemy(run, 1, 80); var second = Enemy(run, 2, 100); var third = Enemy(run, 3, 120);
            var fourth = Enemy(run, 4, 140); var outside = Enemy(run, 5, 200);
            var side = Enemy(run, 6, 90, 30); var behind = Enemy(run, 7, -30, 0, .000001);
            run.Tick();
            foreach (var e in new[] { first, second, third }) Assert.Less(e.Hp, 1000, "Forward target " + e.Id);
            foreach (var e in new[] { fourth, outside, side, behind }) Assert.AreEqual(1000, e.Hp, "Excluded target " + e.Id);
        }

        [Test]
        public void PierceEqualProjectionUsesIdRatherThanInsertionOrder()
        {
            var run = Fixture(WeaponKind.Pierce); Enemy(run, 1, 80);
            var high = Enemy(run, 8, 100, 0); var middle = Enemy(run, 5, 100, 0); var low = Enemy(run, 2, 100, 0);
            run.Tick(); Assert.AreEqual(1000, high.Hp); Assert.Less(middle.Hp, 1000); Assert.Less(low.Hp, 1000);
        }

        [Test]
        public void PierceTradesSingleTargetDamageAndCooldownForRange()
        {
            var point = Fixture(WeaponKind.Point); var pierce = Fixture(WeaponKind.Pierce);
            var a = Enemy(point, 1, 100); var b = Enemy(pierce, 1, 100);
            point.Tick(); pierce.Tick();
            Assert.That(1000 - b.Hp, Is.EqualTo((1000 - a.Hp) * .8).Within(1e-9));
            Assert.Greater(NextAttack(pierce), NextAttack(point));
            point = Fixture(WeaponKind.Point); pierce = Fixture(WeaponKind.Pierce);
            a = Enemy(point, 1, 170); b = Enemy(pierce, 1, 170); point.Tick(); pierce.Tick();
            Assert.AreEqual(1000, a.Hp); Assert.Less(b.Hp, 1000);
        }

        [Test]
        public void BleedCreatesOnlyOneFieldAndExpiresAtItsDeclaredTick()
        {
            var run = Fixture(WeaponKind.Bleed); var enemy = Enemy(run, 1, 100);
            run.Tick(); Assert.AreEqual(1, run.Fields.Count);
            Assert.AreEqual(WeaponKind.Bleed, run.Fields[0].Weapon);
            int expiry = run.Fields[0].UntilTick; Assert.AreEqual(run.TickCount + 48, expiry);
            DisableFire(run);
            while (run.TickCount < expiry - 1) run.Tick();
            Assert.AreEqual(1, run.Fields.Count); double before = enemy.Hp;
            run.Tick(); Assert.AreEqual(0, run.Fields.Count); Assert.AreEqual(before, enemy.Hp);
        }

        [Test]
        public void BleedDamageTracksTipPressureAndDoesNotDoubleSpawnLegacyField()
        {
            var near = Fixture(WeaponKind.Bleed, 0); var far = Fixture(WeaponKind.Bleed, 1000);
            // Enable the generic bleed upgrade too: the weapon must still emit only its own field.
            foreach (var run in new[] { near, far })
            {
                var levels = (int[])typeof(RunSimulator).GetField("levels", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(run);
                levels[(int)UpgradeKind.Bleed] = 2; Enemy(run, 1, 100); run.Tick();
                Assert.AreEqual(1, run.Fields.Count);
                Assert.AreEqual(36, run.Fields[0].Radius);
                double basePressureDamage = RunSimulator.BaseAttackDamage * run.Pressure(run.Branches[0]);
                Assert.That(run.Attacks[0].Damage, Is.EqualTo(basePressureDamage * .45).Within(1e-9));
                Assert.That(run.Fields[0].DamagePerTick, Is.EqualTo(basePressureDamage * .85 * 1.24 / RunSimulator.TickRate).Within(1e-9));
            }
            Assert.Greater(near.Fields[0].DamagePerTick, far.Fields[0].DamagePerTick);
            Assert.That(near.Fields[0].DamagePerTick / far.Fields[0].DamagePerTick,
                Is.EqualTo(near.Pressure(near.Branches[0]) / far.Pressure(far.Branches[0])).Within(1e-9));
        }

        [Test]
        public void ExistingFieldPersistsAfterSwitchAndOnlyOldestTwoOverlap()
        {
            var run = Fixture(WeaponKind.Bleed); var enemy = Enemy(run, 1, 100);
            for (int i = 0; i < 3; i++) { Set(run.Branches[0], "NextAttack", 0); run.Tick(); }
            Assert.AreEqual(3, run.Fields.Count);
            Set(run.Fields[0], "DamagePerTick", 1.0); Set(run.Fields[1], "DamagePerTick", 2.0); Set(run.Fields[2], "DamagePerTick", 100.0);
            double expected = run.Fields[0].DamagePerTick + run.Fields[1].DamagePerTick;
            run.SetWeapon(WeaponKind.Point); DisableFire(run); double before = enemy.Hp;
            run.Tick(); Assert.AreEqual(3, run.Fields.Count);
            Assert.That(before - enemy.Hp, Is.EqualTo(expected).Within(1e-9));
        }

        [Test]
        public void FieldCapacityDropsNewFieldsWithoutUnboundedGrowth()
        {
            var run = Fixture(WeaponKind.Bleed);
            var branches = List(run, "branches");
            for (int i = 1; i < 100; i++)
            {
                var tip = new BranchState(); Set(tip, "Id", i); Set(tip, "IsTip", true); branches.Add(tip);
            }
            Enemy(run, 1, 100, 0, .001, 1000000); run.Tick();
            Assert.AreEqual(96, run.Fields.Count);
            DisableFire(run); run.Tick(); Assert.AreEqual(96, run.Fields.Count);
        }

        [Test]
        public void WeaponChangesCannotResetCooldownOrChangeFinishedRun()
        {
            var run = Fixture(WeaponKind.Point); Enemy(run, 1, 100); run.Tick(); int due = NextAttack(run);
            run.SetWeapon(WeaponKind.Pierce); Assert.AreEqual(due, NextAttack(run));
            run.Tick(); Assert.AreEqual(0, run.Attacks.Count);
            Assert.Throws<ArgumentOutOfRangeException>(() => run.SetWeapon((WeaponKind)999));
            Assert.AreEqual(WeaponKind.Pierce, run.Weapon);
            Set(run, "State", RunStatus.Defeated); run.SetWeapon(WeaponKind.Bleed);
            Assert.AreEqual(WeaponKind.Pierce, run.Weapon);
        }

        [Test]
        public void PiercingKillAwardsEachEnemyOnlyOnce()
        {
            var run = Fixture(WeaponKind.Pierce);
            for (int i = 0; i < 3; i++) Enemy(run, i + 1, 80 + i * 10, 0, .001, 1);
            run.Tick(); Assert.AreEqual(3, run.Kills); Assert.AreEqual(12, run.RunInk);
            run.Tick(); Assert.AreEqual(3, run.Kills); Assert.AreEqual(12, run.RunInk);
        }

        // Controlled geometry, not a natural-seed balance result. Every weapon receives the
        // exact same zero-cost/zero-upgrade state, enemy HP, positions, speed and tick budget.
        static double RoleDamage(WeaponKind weapon, bool dense)
        {
            var run = Fixture(weapon);
            if (dense)
            {
                Enemy(run, 1, 90, 0, 1); Enemy(run, 2, 100, 0, 1); Enemy(run, 3, 110, 0, 1);
                Enemy(run, 4, 95, 20, 1); Enemy(run, 5, 105, -20, 1);
            }
            else Enemy(run, 1, 140, 0, 180);
            int ticks = dense ? 48 : 20; // Before the first scheduled spawn, and before fast contact.
            for (int i = 0; i < ticks; i++) run.Tick();
            Assert.AreEqual(0, run.Kills); Assert.AreEqual(0, run.Leaks);
            // Rules2 adds Harvest; all seven upgrades remain unpurchased for equal-cost comparison.
            CollectionAssert.AreEqual(new int[7], run.UpgradeLevels);
            return run.EffectiveDamage;
        }

        [Test]
        public void SlowDenseGeometryRewardsPierceAndBleedAtEqualUpgradeCost()
        {
            double point = RoleDamage(WeaponKind.Point, true);
            Assert.Greater(RoleDamage(WeaponKind.Pierce, true), point);
            Assert.Greater(RoleDamage(WeaponKind.Bleed, true), point);
        }

        [Test]
        public void IsolatedFastGeometryRewardsPointAtEqualUpgradeCost()
        {
            double point = RoleDamage(WeaponKind.Point, false);
            Assert.Greater(point, RoleDamage(WeaponKind.Pierce, false));
            Assert.Greater(point, RoleDamage(WeaponKind.Bleed, false));
        }
    }
}
