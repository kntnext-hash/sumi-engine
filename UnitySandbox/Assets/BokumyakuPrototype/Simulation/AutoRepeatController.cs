using System;

namespace Bokumyaku.Simulation
{
    /// <summary>
    /// Real-time retry countdown. The caller schedules once after successfully saving
    /// a defeated run's reward, and cancels on manual start or save failure.
    /// This class does not start runs, grant rewards, or access storage.
    /// </summary>
    public sealed class AutoRepeatController
    {
        public const double DelaySeconds = 3;
        public bool Enabled { get; private set; }
        public bool IsPending { get; private set; }
        public double SecondsRemaining { get; private set; }

        public void SetEnabled(bool enabled)
        {
            Enabled = enabled;
            Cancel();
        }

        public bool Schedule(RunStatus status, bool rewardSaved)
        {
            Cancel();
            if (!Enabled || status != RunStatus.Defeated || !rewardSaved) return false;
            IsPending = true;
            SecondsRemaining = DelaySeconds;
            return true;
        }

        public bool Advance(double realSeconds, bool paused)
        {
            // Invalid deltas are rejected even when paused or idle; state is unchanged.
            if (double.IsNaN(realSeconds) || double.IsInfinity(realSeconds) || realSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(realSeconds), "Elapsed real time must be finite and nonnegative.");
            if (!IsPending || paused) return false;
            if (realSeconds < SecondsRemaining)
            {
                SecondsRemaining -= realSeconds;
                return false;
            }
            Cancel();
            return true;
        }

        public void Cancel()
        {
            IsPending = false;
            SecondsRemaining = 0;
        }
    }
}
