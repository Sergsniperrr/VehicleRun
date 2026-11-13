using System;
using UnityEngine;

namespace _Project.Core.Progression
{
    [CreateAssetMenu(fileName = "PlayerLevelConfig", menuName = "_Project/Game/Player Level Config", order = 6)]
    public class PlayerLevelConfig : ScriptableObject
    {
        public const int FirstLevel = 1;

        private const int RewardSignificantDigits = 2;
        private const double DecimalBase = 10;

        [SerializeField] private long[] _mergeExperience = Array.Empty<long>();
        [SerializeField] private long _firstLevelExperience = 4;
        [SerializeField] private long _experienceStepPerLevel = 8;
        [SerializeField] private long _firstRewardCoins = 100;
        [SerializeField] private double _rewardGrowth = 1.7;

        public int MergeExperienceCount => _mergeExperience.Length;
        public long FirstLevelExperience => _firstLevelExperience;
        public long ExperienceStepPerLevel => _experienceStepPerLevel;
        public long FirstRewardCoins => _firstRewardCoins;
        public double RewardGrowth => _rewardGrowth;

        public long GetMergeExperience(int mergedRank)
        {
            if (mergedRank < 1 || mergedRank > _mergeExperience.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(mergedRank));
            }

            return _mergeExperience[mergedRank - 1];
        }

        public long GetExperienceToNextLevel(int level)
        {
            if (level < FirstLevel)
            {
                throw new ArgumentOutOfRangeException(nameof(level));
            }

            return _firstLevelExperience + _experienceStepPerLevel * (level - FirstLevel);
        }

        public long GetReward(int level)
        {
            if (level < FirstLevel)
            {
                throw new ArgumentOutOfRangeException(nameof(level));
            }

            if (level == FirstLevel)
            {
                return 0;
            }

            int rewardIndex = level - FirstLevel - 1;
            double coins = _firstRewardCoins * Math.Pow(_rewardGrowth, rewardIndex);

            return RoundReward(coins);
        }

        private long RoundReward(double coins)
        {
            if (coins >= long.MaxValue)
            {
                return long.MaxValue;
            }

            double digitCount = Math.Floor(Math.Log10(coins)) + 1;
            double roundingStep = Math.Pow(DecimalBase, digitCount - RewardSignificantDigits);

            if (roundingStep < 1)
            {
                return (long)Math.Round(coins);
            }

            double roundedCoins = Math.Round(coins / roundingStep) * roundingStep;

            if (roundedCoins >= long.MaxValue)
            {
                return long.MaxValue;
            }

            return (long)roundedCoins;
        }
    }
}
