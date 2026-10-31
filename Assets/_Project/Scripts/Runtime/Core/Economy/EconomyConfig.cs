using UnityEngine;

namespace _Project.Core.Economy
{
    [CreateAssetMenu(fileName = "EconomyConfig", menuName = "_Project/Game/Economy Config", order = 2)]
    public class EconomyConfig : ScriptableObject
    {
        [SerializeField] private long _startCoins;
        [SerializeField] private int _startVehicleRank;
        [SerializeField] private int _startVehicleCount;
        [SerializeField] private int _boxRankLag;
        [SerializeField] private double _boxBasePriceIncomeSeconds;
        [SerializeField] private double _boxPriceGrowth;
        [SerializeField] private int _boxPriceGrowthPurchaseLimit;
        [SerializeField] private float _freeBoxIntervalSeconds;
        [SerializeField] private float _offlineMinAwaySeconds;
        [SerializeField] private float _offlineMaxAwaySeconds;
        [SerializeField] private double _offlineIncomeRate;

        public long StartCoins => _startCoins;
        public int StartVehicleRank => _startVehicleRank;
        public int StartVehicleCount => _startVehicleCount;
        public int BoxRankLag => _boxRankLag;
        public double BoxBasePriceIncomeSeconds => _boxBasePriceIncomeSeconds;
        public double BoxPriceGrowth => _boxPriceGrowth;
        public int BoxPriceGrowthPurchaseLimit => _boxPriceGrowthPurchaseLimit;
        public float FreeBoxIntervalSeconds => _freeBoxIntervalSeconds;
        public float OfflineMinAwaySeconds => _offlineMinAwaySeconds;
        public float OfflineMaxAwaySeconds => _offlineMaxAwaySeconds;
        public double OfflineIncomeRate => _offlineIncomeRate;
    }
}
