using UnityEngine;

namespace _Project.Core.Turbo
{
    [CreateAssetMenu(fileName = "TurboConfig", menuName = "_Project/Game/Turbo Config", order = 5)]
    public class TurboConfig : ScriptableObject
    {
        [SerializeField] private int _unlockRank;

        public int UnlockRank => _unlockRank;
    }
}
