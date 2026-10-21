using System;
using System.IO;
using Bokumyaku.Persistence;
using Bokumyaku.Simulation;
using NUnit.Framework;

namespace Bokumyaku.Tests
{
    public sealed class MetaSaveStoreTests
    {
        string directory, path;
        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "BokumyakuQA-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory); path = Path.Combine(directory, "prototype.json");
        }
        [TearDown]
        public void TearDown()
        {
            // Only remove files created in this unique fixture directory; no recursion.
            if (Directory.Exists(directory))
            {
                foreach (string file in Directory.GetFiles(directory)) File.Delete(file);
                Directory.Delete(directory);
            }
        }
        [Test]
        public void MissingSaveCreatesZeroProgressWithoutWarning()
        {
            string warning; var loaded = MetaSaveStore.Load(path, out warning);
            Assert.AreEqual(0, loaded.Crystals); Assert.AreEqual(0, loaded.Damage);
            Assert.AreEqual("", warning); Assert.IsFalse(File.Exists(path));
        }
        [Test]
        public void SaveRoundTripsAllProgressAndKeepsPreviousVersion()
        {
            MetaSaveStore.Save(path, new MetaProgress { Crystals = 41, Damage = 1, Supply = 2, Growth = 3, Harvest = 4 });
            MetaSaveStore.Save(path, new MetaProgress { Crystals = 99, Damage = 4, Supply = 3, Growth = 2, Harvest = 1 });
            string warning; var loaded = MetaSaveStore.Load(path, out warning);
            Assert.AreEqual("", warning); Assert.AreEqual(99, loaded.Crystals);
            Assert.AreEqual(4, loaded.Damage); Assert.AreEqual(3, loaded.Supply);
            Assert.AreEqual(2, loaded.Growth); Assert.AreEqual(1, loaded.Harvest);
            var previous = MetaSaveStore.Load(path + ".bak", out warning);
            Assert.AreEqual(41, previous.Crystals); Assert.AreEqual(1, previous.Damage);
        }
        [Test]
        public void CorruptCurrentRecoversBackupAndPreservesOriginal()
        {
            MetaSaveStore.Save(path, new MetaProgress { Crystals = 41 });
            MetaSaveStore.Save(path, new MetaProgress { Crystals = 99 });
            File.WriteAllText(path, "not-json");
            string warning; var loaded = MetaSaveStore.Load(path, out warning);
            Assert.AreEqual(41, loaded.Crystals); Assert.IsNotEmpty(warning);
            Assert.AreEqual("not-json", File.ReadAllText(path));
            MetaSaveStore.Save(path, new MetaProgress { Crystals = 55 });
            Assert.AreEqual(1, Directory.GetFiles(directory, "*.corrupt-*").Length);
            Assert.AreEqual(41, MetaSaveStore.Load(path + ".bak", out warning).Crystals);
            Assert.AreEqual(55, MetaSaveStore.Load(path, out warning).Crystals);
        }
        [Test]
        public void InvalidMetaSaveRejectsWithoutOverwritingGoodData()
        {
            MetaSaveStore.Save(path, new MetaProgress { Crystals = 17 });
            string before = File.ReadAllText(path);
            Assert.Throws<InvalidDataException>(() => MetaSaveStore.Save(path, new MetaProgress { Damage = 21 }));
            Assert.Throws<InvalidDataException>(() => MetaSaveStore.Save(path, new MetaProgress { Crystals = -1 }));
            Assert.Throws<InvalidDataException>(() => MetaSaveStore.Save(path, new MetaProgress { Supply = -1 }));
            Assert.AreEqual(before, File.ReadAllText(path));
        }
        [TestCase("{\"schema\":99,\"config\":\"prototype-3enemy-v1\",\"meta\":{\"Crystals\":100}}")]
        [TestCase("{\"schema\":1,\"config\":\"future\",\"meta\":{\"Crystals\":100}}")]
        [TestCase("{\"schema\":1,\"config\":\"prototype-3enemy-v1\",\"meta\":{\"Crystals\":100,\"Damage\":21}}")]
        public void UnsupportedOrInvalidDataDoesNotBecomePlayableProgress(string invalid)
        {
            File.WriteAllText(path, invalid); string warning;
            var loaded = MetaSaveStore.Load(path, out warning);
            Assert.AreEqual(0, loaded.Crystals); Assert.AreEqual(0, loaded.Damage);
            Assert.IsNotEmpty(warning); Assert.AreEqual(invalid, File.ReadAllText(path));
        }
    }
}
