using System;
using Bokumyaku.Simulation;
using NUnit.Framework;

namespace Bokumyaku.Tests
{
    public sealed class AutoRepeatControllerTests
    {
        static AutoRepeatController Pending()
        {
            var c = new AutoRepeatController(); c.SetEnabled(true);
            Assert.IsTrue(c.Schedule(RunStatus.Defeated, true)); return c;
        }
        [Test]
        public void DefaultOffCannotScheduleEvenSavedDefeat()
        {
            var c = new AutoRepeatController();
            Assert.IsFalse(c.Enabled); Assert.IsFalse(c.Schedule(RunStatus.Defeated, true));
            Assert.IsFalse(c.IsPending); Assert.AreEqual(0, c.SecondsRemaining);
            Assert.IsFalse(c.Advance(100, false));
        }
        [Test]
        public void ThreeRealSecondsTriggerExactlyOnce()
        {
            var c = Pending(); Assert.AreEqual(3, c.SecondsRemaining);
            Assert.IsFalse(c.Advance(1.25, false)); Assert.AreEqual(1.75, c.SecondsRemaining);
            Assert.IsFalse(c.Advance(1.5, false)); Assert.AreEqual(.25, c.SecondsRemaining);
            Assert.IsTrue(c.Advance(.25, false)); Assert.IsFalse(c.IsPending);
            Assert.AreEqual(0, c.SecondsRemaining); Assert.IsTrue(c.Enabled);
            Assert.IsFalse(c.Advance(100, false));
        }
        [Test]
        public void LargeDeltaTriggersOnceWithoutResidualReservation()
        {
            var c = Pending(); Assert.IsTrue(c.Advance(100, false));
            Assert.IsFalse(c.Advance(0, false)); Assert.IsFalse(c.Advance(3, false));
        }
        [Test]
        public void OffCancelsAndTurningOnDoesNotReserve()
        {
            var c = Pending(); c.Advance(2.5, false); c.SetEnabled(false);
            Assert.IsFalse(c.IsPending); Assert.AreEqual(0, c.SecondsRemaining);
            c.SetEnabled(true); Assert.IsTrue(c.Enabled); Assert.IsFalse(c.IsPending);
            Assert.IsFalse(c.Advance(3, false));
        }
        [Test]
        public void PausedCountdownDoesNotConsumeTime()
        {
            var c = Pending(); c.Advance(1, false);
            Assert.IsFalse(c.Advance(100, true)); Assert.AreEqual(2, c.SecondsRemaining);
            Assert.IsFalse(c.Advance(0, false)); Assert.AreEqual(2, c.SecondsRemaining);
            Assert.IsTrue(c.Advance(2, false));
        }
        [TestCase(RunStatus.Running)] [TestCase(RunStatus.CapacityExceeded)]
        [TestCase(RunStatus.TestCompleted)] [TestCase((RunStatus)999)]
        public void OtherStatusCannotReserveAndCancelsOldReservation(RunStatus status)
        {
            var c = Pending(); Assert.IsFalse(c.Schedule(status, true));
            Assert.IsFalse(c.IsPending); Assert.AreEqual(0, c.SecondsRemaining);
            Assert.IsFalse(c.Advance(3, false));
        }
        [Test]
        public void UnsavedRewardCannotReserveAndCancelsOldReservation()
        {
            var c = Pending(); Assert.IsFalse(c.Schedule(RunStatus.Defeated, false));
            Assert.IsFalse(c.IsPending); Assert.IsFalse(c.Advance(3, false));
        }
        [Test]
        public void ManualCancelKeepsPreferenceButPreventsStart()
        {
            var c = Pending(); c.Cancel(); c.Cancel();
            Assert.IsTrue(c.Enabled); Assert.IsFalse(c.IsPending);
            Assert.IsFalse(c.Advance(10, false));
        }
        [TestCase(false)] [TestCase(true)]
        public void InvalidDeltaRejectsWithoutChangingPendingState(bool paused)
        {
            var c = Pending(); c.Advance(.5, false);
            foreach (double bad in new[] { -1.0, double.NaN, double.PositiveInfinity, double.NegativeInfinity })
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => c.Advance(bad, paused));
                Assert.IsTrue(c.Enabled); Assert.IsTrue(c.IsPending); Assert.AreEqual(2.5, c.SecondsRemaining);
            }
        }
        [Test]
        public void IdleStillRejectsInvalidDelta()
        {
            var c = new AutoRepeatController();
            Assert.Throws<ArgumentOutOfRangeException>(() => c.Advance(double.NaN, true));
            Assert.IsFalse(c.Enabled); Assert.IsFalse(c.IsPending);
        }
        [Test]
        public void RescheduleRequiresFreshThreeSeconds()
        {
            var c = Pending(); c.Advance(2, false);
            Assert.IsTrue(c.Schedule(RunStatus.Defeated, true)); Assert.AreEqual(3, c.SecondsRemaining);
            Assert.IsFalse(c.Advance(1, false)); Assert.IsTrue(c.Advance(2, false));
        }
        [Test]
        public void ReapplyingEnabledPreferenceAlsoCancelsPending()
        {
            var c = Pending(); c.SetEnabled(true);
            Assert.IsTrue(c.Enabled); Assert.IsFalse(c.IsPending); Assert.IsFalse(c.Advance(3, false));
        }
    }
}
