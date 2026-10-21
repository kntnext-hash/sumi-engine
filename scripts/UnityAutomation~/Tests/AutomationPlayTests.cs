using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Sumi.Automation.Tests
{
    public sealed class AutomationPlayTests
    {
        GameObject cube;
        bool previousRunInBackground;

        [SetUp]
        public void SetUp()
        {
            previousRunInBackground = Application.runInBackground;
            Application.runInBackground = true;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (cube != null) Object.Destroy(cube);
            Application.runInBackground = previousRunInBackground;
            yield return null;
        }

        [UnityTest]
        public IEnumerator CubeAdvancesAndRotates()
        {
            cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var rotator = cube.AddComponent<AutomationRotator>();
            var initial = cube.transform.rotation;
            var deadline = Time.realtimeSinceStartup + 5f;
            while ((rotator.UpdateCount < 3 || Quaternion.Angle(initial, cube.transform.rotation) < 1f)
                   && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(rotator.UpdateCount, Is.GreaterThanOrEqualTo(3), "Player loop did not advance.");
            Assert.That(Quaternion.Angle(initial, cube.transform.rotation), Is.GreaterThan(1f));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
