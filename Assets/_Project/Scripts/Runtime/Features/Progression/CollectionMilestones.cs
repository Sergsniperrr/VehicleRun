using System;
using System.Collections.Generic;
using _Project.Core.Economy;
using _Project.Features.Commands;
using _Project.Features.Economy;

namespace _Project.Features.Progression
{
    public class CollectionMilestones
    {
        private readonly RankProgress _progress;
        private readonly Wallet _wallet;
        private readonly EconomyConfig _economy;
        private readonly HashSet<int> _claimed = new HashSet<int>();

        public CollectionMilestones(RankProgress progress, Wallet wallet, EconomyConfig economy)
        {
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _economy = economy ?? throw new ArgumentNullException(nameof(economy));
        }

        public event Action<int> MilestoneClaimed;

        public IReadOnlyCollection<int> ClaimedRanks => _claimed;

        public MilestoneState GetState(int rank)
        {
            if (IsMilestone(rank) == false)
            {
                return MilestoneState.Locked;
            }

            if (_progress.IsRankUnlocked(rank) == false)
            {
                return MilestoneState.Locked;
            }

            if (_claimed.Contains(rank))
            {
                return MilestoneState.Claimed;
            }

            return MilestoneState.Claimable;
        }

        public long GetReward(int rank)
        {
            foreach (EconomyConfig.MilestoneReward reward in _economy.MilestoneRewards)
            {
                if (reward.Rank == rank)
                {
                    return reward.Coins;
                }
            }

            return 0;
        }

        public CommandResult Claim(int rank)
        {
            if (GetState(rank) != MilestoneState.Claimable)
            {
                return new CommandResult(FailureReason.MilestoneUnavailable);
            }

            _wallet.Add(GetReward(rank));
            _claimed.Add(rank);
            MilestoneClaimed?.Invoke(rank);

            return new CommandResult(FailureReason.None);
        }

        public void RestoreClaimed(IReadOnlyList<int> ranks)
        {
            if (ranks == null)
            {
                throw new ArgumentNullException(nameof(ranks));
            }

            _claimed.Clear();

            foreach (int rank in ranks)
            {
                if (IsMilestone(rank))
                {
                    _claimed.Add(rank);
                }
            }
        }

        private bool IsMilestone(int rank)
        {
            foreach (EconomyConfig.MilestoneReward reward in _economy.MilestoneRewards)
            {
                if (reward.Rank == rank)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
