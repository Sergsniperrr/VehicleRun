using System;
using _Project.Core.Economy;

namespace _Project.Features.Progression
{
    public class CatalogNews : IDisposable
    {
        private readonly RankProgress _progress;
        private readonly CollectionMilestones _milestones;
        private readonly EconomyConfig _economy;

        public CatalogNews(RankProgress progress, CollectionMilestones milestones, EconomyConfig economy)
        {
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _milestones = milestones ?? throw new ArgumentNullException(nameof(milestones));
            _economy = economy ?? throw new ArgumentNullException(nameof(economy));
            SeenRank = economy.StartVehicleRank;
            _progress.RankUnlocked += OnRankUnlocked;
            _milestones.MilestoneClaimed += OnMilestoneClaimed;
        }

        public event Action Changed;
        public event Action Seen;

        public int SeenRank { get; private set; }
        public bool HasNews => HasClaimableMilestone();
        public bool HasUnseenVehicle => _progress.MaxRank > SeenRank;

        public void MarkSeen()
        {
            if (HasUnseenVehicle == false)
            {
                return;
            }

            SeenRank = _progress.MaxRank;
            Seen?.Invoke();
            Changed?.Invoke();
        }

        public void Restore(int seenRank)
        {
            if (seenRank < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(seenRank));
            }

            bool isSavedBeforeNews = seenRank == 0;
            SeenRank = isSavedBeforeNews ? _progress.MaxRank : Math.Min(seenRank, _progress.MaxRank);
        }

        public void Dispose()
        {
            _progress.RankUnlocked -= OnRankUnlocked;
            _milestones.MilestoneClaimed -= OnMilestoneClaimed;
        }

        private bool HasClaimableMilestone()
        {
            foreach (EconomyConfig.MilestoneReward reward in _economy.MilestoneRewards)
            {
                if (_milestones.GetState(reward.Rank) == MilestoneState.Claimable)
                {
                    return true;
                }
            }

            return false;
        }

        private void OnRankUnlocked(int rank)
        {
            Changed?.Invoke();
        }

        private void OnMilestoneClaimed(int rank)
        {
            Changed?.Invoke();
        }
    }
}
