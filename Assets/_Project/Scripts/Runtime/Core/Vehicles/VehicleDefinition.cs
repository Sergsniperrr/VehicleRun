using System;
using UnityEngine;

namespace _Project.Core.Vehicles
{
    [Serializable]
    public class VehicleDefinition
    {
        [SerializeField] private string _id;
        [SerializeField] private int _rank;
        [SerializeField] private string _nameKey;
        [SerializeField] private long _coinsPerLap;
        [SerializeField] private float _trackSpeed;
        [SerializeField] private Sprite _garageSprite;
        [SerializeField] private Sprite _trackSprite;
        [SerializeField] private Color _badgeColor = Color.white;

        public string Id => _id;
        public int Rank => _rank;
        public string NameKey => _nameKey;
        public long CoinsPerLap => _coinsPerLap;
        public float TrackSpeed => _trackSpeed;
        public Sprite GarageSprite => _garageSprite;
        public Sprite TrackSprite => _trackSprite;
        public Color BadgeColor => _badgeColor;

        public double CalculateAverageIncome(float lapLength)
        {
            if (lapLength <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(lapLength));
            }

            return _coinsPerLap * (double)_trackSpeed / lapLength;
        }
    }
}
