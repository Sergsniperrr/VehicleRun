using UnityEngine;

namespace _Project.Core.Collector
{
    [CreateAssetMenu(fileName = "CollectorConfig", menuName = "_Project/Game/Collector Config", order = 7)]
    public class CollectorConfig : ScriptableObject
    {
        [SerializeField] private int _spawnIntervalMinSeconds;
        [SerializeField] private int _spawnIntervalMaxSeconds;
        [SerializeField] private float _lifetimeSeconds;
        [SerializeField] private int _minCharges;
        [SerializeField] private int _maxCharges;
        [SerializeField] private float _trackSpeed;

        public int SpawnIntervalMinSeconds => _spawnIntervalMinSeconds;
        public int SpawnIntervalMaxSeconds => _spawnIntervalMaxSeconds;
        public float LifetimeSeconds => _lifetimeSeconds;
        public int MinCharges => _minCharges;
        public int MaxCharges => _maxCharges;
        public float TrackSpeed => _trackSpeed;
    }
}
