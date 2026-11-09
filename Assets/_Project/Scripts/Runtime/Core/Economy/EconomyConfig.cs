using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Core.Economy
{
    [CreateAssetMenu(fileName = "EconomyConfig", menuName = "_Project/Game/Economy Config", order = 2)]
    public class EconomyConfig : ScriptableObject
    {
        [Serializable]
        public class MilestoneReward
        {
            [SerializeField] private int _rank;
            [SerializeField] private long _coins;

            public int Rank => _rank;
            public long Coins => _coins;
        }

        [SerializeField] private long _startCoins;
        [SerializeField] private int _startVehicleRank;
        [SerializeField] private int _startVehicleCount;
        [SerializeField] private int _boxRankLag;
        [SerializeField] private List<long> _boxBasePrices = new List<long>();
        [SerializeField] private double _boxPriceGrowth;
        [SerializeField] private double _firstRankBoxPriceGrowth;
        [SerializeField] private bool _isBoxPriceGrowthLimited;
        [SerializeField] private int _boxPriceGrowthPurchaseLimit;
        [SerializeField] private float _freeBoxIntervalSeconds;
        [SerializeField] private float _offlineMinAwaySeconds;
        [SerializeField] private float _offlineMaxAwaySeconds;
        [SerializeField] private double _offlineIncomeRate;
        [SerializeField] private List<MilestoneReward> _milestoneRewards = new List<MilestoneReward>();

        public long StartCoins => _startCoins;
        public int StartVehicleRank => _startVehicleRank;
        public int StartVehicleCount => _startVehicleCount;
        public int BoxRankLag => _boxRankLag;
        public IReadOnlyList<long> BoxBasePrices => _boxBasePrices;
        public double BoxPriceGrowth => _boxPriceGrowth;
        public double FirstRankBoxPriceGrowth => _firstRankBoxPriceGrowth;
        public bool IsBoxPriceGrowthLimited => _isBoxPriceGrowthLimited;
        public int BoxPriceGrowthPurchaseLimit => _boxPriceGrowthPurchaseLimit;
        public float FreeBoxIntervalSeconds => _freeBoxIntervalSeconds;
        public float OfflineMinAwaySeconds => _offlineMinAwaySeconds;
        public float OfflineMaxAwaySeconds => _offlineMaxAwaySeconds;
        public double OfflineIncomeRate => _offlineIncomeRate;
        public IReadOnlyList<MilestoneReward> MilestoneRewards => _milestoneRewards;
    }
}
