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
        [SerializeField] [Min(0f)] private float _moneyMinIntervalSeconds = 0.5f;

        public AudioAsset Click => _click;
        public AudioAsset Window => _window;
        public AudioAsset Merge => _merge;
        public AudioAsset Money => _money;
        public AudioAsset LevelUp => _levelUp;
        public AudioAsset BoxOpen => _boxOpen;
        public AudioAsset BoxShake => _boxShake;
        public float MoneyMinIntervalSeconds => _moneyMinIntervalSeconds;
    }
}
