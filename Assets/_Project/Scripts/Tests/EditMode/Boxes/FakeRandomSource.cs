using System;
using _Project.Core.Randomness;

namespace _Project.Tests.EditMode.Boxes
{
    public class FakeRandomSource : IRandomSource
    {
        public int Offset { get; set; }
        public int LastMin { get; private set; }
        public int LastMax { get; private set; }
        public int CallCount { get; private set; }

        public int Range(int minInclusive, int maxInclusive)
        {
            if (maxInclusive < minInclusive)
            {
                throw new ArgumentOutOfRangeException(nameof(maxInclusive));
            }

            LastMin = minInclusive;
            LastMax = maxInclusive;
            CallCount++;

            return Math.Min(maxInclusive, minInclusive + Offset);
        }
    }
}
