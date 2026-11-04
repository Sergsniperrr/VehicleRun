using System;
using _Project.Features.Vehicles;

namespace _Project.Features.Track
{
    public class TrackRunner
    {
        public TrackRunner(int slot, Vehicle vehicle)
        {
            Slot = slot;
            Vehicle = vehicle ?? throw new ArgumentNullException(nameof(vehicle));
        }

        public int Slot { get; }
        public Vehicle Vehicle { get; }
        public float Distance { get; private set; }

        internal int Drive(float distance, float lapLength)
        {
            float total = Distance + distance;
            int crossings = (int)Math.Floor(total / lapLength);
            Distance = Math.Max(0f, total - crossings * lapLength);

            return crossings;
        }
    }
}
