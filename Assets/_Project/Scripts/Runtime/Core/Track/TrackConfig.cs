using _Project.Core.Progression;
using UnityEngine;

namespace _Project.Core.Track
{
    [CreateAssetMenu(fileName = "TrackConfig", menuName = "_Project/Game/Track Config", order = 4)]
    public class TrackConfig : ScriptableObject
    {
        [SerializeField] private float _straightHalfLength;
        [SerializeField] private float _turnRadius;
        [SerializeField] private float _startDistance;
        [SerializeField] private CapacitySchedule _capacity = new CapacitySchedule();

        public float StraightHalfLength => _straightHalfLength;
        public float TurnRadius => _turnRadius;
        public float StartDistance => _startDistance;
        public CapacitySchedule Capacity => _capacity;
        public float StraightLength => _straightHalfLength * 2f;
        public float TurnLength => Mathf.PI * _turnRadius;
        public float LapLength => (StraightLength + TurnLength) * 2f;
    }
}
