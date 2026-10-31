using _Project.Core.Progression;
using UnityEngine;

namespace _Project.Core.Track
{
    [CreateAssetMenu(fileName = "TrackConfig", menuName = "_Project/Game/Track Config", order = 4)]
    public class TrackConfig : ScriptableObject
    {
        [SerializeField] private float _lapDurationSeconds;
        [SerializeField] private float _straightHalfLength;
        [SerializeField] private float _turnRadius;
        [SerializeField] private CapacitySchedule _capacity = new CapacitySchedule();

        public float LapDurationSeconds => _lapDurationSeconds;
        public float StraightHalfLength => _straightHalfLength;
        public float TurnRadius => _turnRadius;
        public CapacitySchedule Capacity => _capacity;
    }
}
