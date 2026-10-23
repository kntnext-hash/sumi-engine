using System;
using NUnit.Framework;
using Bokumyaku.Simulation;

namespace Bokumyaku.Tests
{
    public class ResearchPricingTests
    {
        [TestCase(MetaUpgrade.Damage,8)]
        [TestCase(MetaUpgrade.Supply,10)]
        [TestCase(MetaUpgrade.Growth,8)]
        [TestCase(MetaUpgrade.Harvest,12)]
        public void PriceCurvePreservesEarlyCostsAndUsesRoundedAnchor(MetaUpgrade kind,int basis)
        {
            var m=new MetaProgress();int previous=0;
            for(int level=0;level<=MetaProgress.MaxLevel(kind);level++)
            {
                switch(kind){case MetaUpgrade.Damage:m.Damage=level;break;case MetaUpgrade.Supply:m.Supply=level;break;case MetaUpgrade.Growth:m.Growth=level;break;default:m.Harvest=level;break;}
                int expected=level<=10?(int)Math.Ceiling(basis*Math.Pow(1.48,level)):level<=20?(int)Math.Ceiling(Math.Ceiling(basis*Math.Pow(1.48,10))*Math.Pow(1.22,level-10)):(int)Math.Ceiling(2952*Math.Pow(1+.05*(level-20),2));
                Assert.AreEqual(expected,m.Cost(kind));Assert.Greater(m.Cost(kind),previous);previous=m.Cost(kind);
            }
        }
        [Test] public void PurchaseAtNewBoundaryIsAtomicAndDoesNotChangeBattleGeneration()
        {
            Assert.AreEqual(2,MetaProgress.ResearchPriceVersion);
            Assert.AreEqual(8,RunSimulator.CurrentRulesVersion);
            var m=new MetaProgress{Damage=20,Crystals=2951};
            Assert.IsFalse(m.TryBuy(MetaUpgrade.Damage));Assert.AreEqual(20,m.Damage);Assert.AreEqual(2951,m.Crystals);
            m.Crystals++;Assert.IsTrue(m.TryBuy(MetaUpgrade.Damage));Assert.AreEqual(21,m.Damage);Assert.AreEqual(0,m.Crystals);Assert.AreEqual(3255,m.Cost(MetaUpgrade.Damage));
        }
    }
}
