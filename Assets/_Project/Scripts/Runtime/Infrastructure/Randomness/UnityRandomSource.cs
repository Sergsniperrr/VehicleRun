using System;
using _Project.Core.Randomness;

namespace _Project.Infrastructure.Randomness
{
    public class UnityRandomSource : IRandomSource
    {
        public int Range(int minInclusive, int maxInclusive)
        {
            if (maxInclusive < minInclusive)
            {
                throw new ArgumentOutOfRangeException(nameof(maxInclusive));
            }

            return UnityEngine.Random.Range(minInclusive, maxInclusive + 1);
        }
    }
}
