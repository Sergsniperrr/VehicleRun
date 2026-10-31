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
        [SerializeField] private long _incomePerSecond;
        [SerializeField] private Sprite _garageSprite;
        [SerializeField] private Sprite _trackSprite;

        public string Id => _id;
        public int Rank => _rank;
        public string NameKey => _nameKey;
        public long IncomePerSecond => _incomePerSecond;
        public Sprite GarageSprite => _garageSprite;
        public Sprite TrackSprite => _trackSprite;
    }
}
