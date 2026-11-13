using UnityEngine;

namespace _Project.Core.Audio
{
    [CreateAssetMenu(fileName = "GameSoundsConfig", menuName = "_Project/Audio/Game Sounds Config", order = 1)]
    public class GameSoundsConfig : ScriptableObject
    {
        [SerializeField] private AudioAsset _click;
        [SerializeField] private AudioAsset _window;
        [SerializeField] private AudioAsset _merge;
        [SerializeField] private AudioAsset _money;
        [SerializeField] private AudioAsset _levelUp;
        [SerializeField] private AudioAsset _boxOpen;
        [SerializeField] private AudioAsset _boxShake;
        [SerializeField] private AudioAsset _startMoving;
        [SerializeField] private AudioAsset _boost;
        [SerializeField] [Min(0.1f)] private float _startMovingFirstRankPitch = 1.3f;
        [SerializeField] [Min(0.1f)] private float _startMovingLastRankPitch = 0.7f;
        [SerializeField] [Min(0f)] private float _moneyMinIntervalSeconds = 0.5f;

        public AudioAsset Click => _click;
        public AudioAsset Window => _window;
        public AudioAsset Merge => _merge;
        public AudioAsset Money => _money;
        public AudioAsset LevelUp => _levelUp;
        public AudioAsset BoxOpen => _boxOpen;
        public AudioAsset BoxShake => _boxShake;
        public AudioAsset StartMoving => _startMoving;
        public AudioAsset Boost => _boost;
        public float StartMovingFirstRankPitch => _startMovingFirstRankPitch;
        public float StartMovingLastRankPitch => _startMovingLastRankPitch;
        public float MoneyMinIntervalSeconds => _moneyMinIntervalSeconds;
    }
}
