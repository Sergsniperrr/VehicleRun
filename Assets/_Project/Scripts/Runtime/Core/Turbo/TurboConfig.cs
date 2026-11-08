using UnityEngine;

namespace _Project.Core.Turbo
{
    [CreateAssetMenu(fileName = "TurboConfig", menuName = "_Project/Game/Turbo Config", order = 5)]
    public class TurboConfig : ScriptableObject
    {
        [SerializeField] private int _unlockRank;
        [SerializeField] private float _speedMultiplier;
        [SerializeField] private float _freeDurationSeconds;
        [SerializeField] private float _freeCooldownSeconds;
        [SerializeField] private float _tapBoostSeconds;

        public int UnlockRank => _unlockRank;
        public float SpeedMultiplier => _speedMultiplier;
        public float FreeDurationSeconds => _freeDurationSeconds;
        public float FreeCooldownSeconds => _freeCooldownSeconds;
        public float TapBoostSeconds => _tapBoostSeconds;
    }
}
