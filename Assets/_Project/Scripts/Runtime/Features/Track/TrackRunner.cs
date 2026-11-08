using System;
using _Project.Features.Vehicles;

namespace _Project.Features.Track
{
    public class TrackRunner
    {
        public TrackRunner(int slot, Vehicle vehicle, float distance)
        {
            if (float.IsNaN(distance) || float.IsInfinity(distance) || distance < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(distance));
            }

            Slot = slot;
            Vehicle = vehicle ?? throw new ArgumentNullException(nameof(vehicle));
            Distance = distance;
        }

        public int Slot { get; }
        public Vehicle Vehicle { get; }
        public float Distance { get; private set; }
        public float TapBoostRemainingSeconds { get; private set; }

        internal int Drive(float distance, float lapLength)
        {
            float total = Distance + distance;
            int crossings = (int)Math.Floor(total / lapLength);
            Distance = Math.Max(0f, total - crossings * lapLength);

            return crossings;
        }

        internal void StartTapBoost(float seconds)
        {
            TapBoostRemainingSeconds = seconds;
        }

        internal void AdvanceTapBoost(float deltaTime)
        {
            TapBoostRemainingSeconds = Math.Max(0f, TapBoostRemainingSeconds - deltaTime);
        }
    }
}
