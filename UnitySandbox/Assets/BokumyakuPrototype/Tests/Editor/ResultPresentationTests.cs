using System;
using System.Reflection;
using NUnit.Framework;

namespace Bokumyaku.Tests
{
    public class ResultPresentationTests
    {
        // Keep the simulation test assembly independent of Unity presentation at compile time.
        static object Display(string method, params object[] arguments)
        {
            var type = Assembly.Load("Bokumyaku.Presentation").GetType("Bokumyaku.Presentation.ResultPresentation", true);
            return type.GetMethod(method, BindingFlags.Public | BindingFlags.Static).Invoke(null, arguments);
        }

        [TestCase(30, 120, 15)]
        [TestCase(30, 0, 0)]
        [TestCase(0, 120, 0)]
        public void NormalRateUsesOnlyNormalRewardAndBattleTime(int normal, double seconds, double expected)
        {
            Assert.That((double)Display("NormalCrystalsPerMinute", normal, seconds), Is.EqualTo(expected).Within(.00001));
        }

        [Test]
        public void NoInvasionHasNoMostCommonType()
        {
            Assert.That(Display("InvasionSummary", new int[5]), Is.EqualTo("核への侵入なし"));
        }

        [Test]
        public void TieUsesFirstEnemyKindAndDoesNotMutateCounts()
        {
            var counts = new[] { 0, 2, 2, 0, 0 };
            Assert.That(Display("InvasionSummary", counts), Is.EqualTo("侵入 4体 / 最多 速敵 2体"));
            CollectionAssert.AreEqual(new[] { 0, 2, 2, 0, 0 }, counts);
        }

        [Test]
        public void InvasionCountsBodiesNotCoreDamage()
        {
            Assert.That(Display("InvasionSummary", new[] { 0, 0, 2, 0, 1 }), Is.EqualTo("侵入 3体 / 最多 重敵 2体"));
            Assert.That(Display("InvasionSummary", new[] { 0, 0, 0, 0, 3 }), Is.EqualTo("侵入 3体 / 最多 ボス 3体"));
        }
    }
}