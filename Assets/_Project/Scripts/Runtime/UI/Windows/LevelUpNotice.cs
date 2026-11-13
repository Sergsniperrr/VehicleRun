using System;
using _Project.Features.Garage;
using _Project.Features.Progression;
using UnityEngine;
using VContainer;

namespace _Project.UI.Windows
{
    public class LevelUpNotice : MonoBehaviour
    {
        [SerializeField] private LevelUpWindow _levelUpWindow;
        [SerializeField] private GarageView _garageView;

        private PlayerLevel _playerLevel;
        private int _shownRewardLevel;

        private void Update()
        {
            if (_playerLevel.HasPendingReward == false || _garageView.IsMergePlaying)
            {
                return;
            }

            int level = _playerLevel.PendingRewardLevel;

            if (level == _shownRewardLevel)
            {
                return;
            }

            _shownRewardLevel = level;
            _levelUpWindow.ShowPendingReward();
        }

        [Inject]
        public void Construct(PlayerLevel playerLevel)
        {
            _playerLevel = playerLevel ?? throw new ArgumentNullException(nameof(playerLevel));
        }
    }
}
