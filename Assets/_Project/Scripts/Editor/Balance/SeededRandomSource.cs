using System;
using _Project.Core.Randomness;

namespace _Project.Editor.Balance
{
    public class SeededRandomSource : IRandomSource
    {
        private readonly Random _random;

        public SeededRandomSource(int seed)
        {
            _random = new Random(seed);
        }

        public int Range(int minInclusive, int maxInclusive)
        {
            if (maxInclusive < minInclusive)
            {
                throw new ArgumentOutOfRangeException(nameof(maxInclusive));
            }

            return _random.Next(minInclusive, maxInclusive + 1);
        }
    }
}
