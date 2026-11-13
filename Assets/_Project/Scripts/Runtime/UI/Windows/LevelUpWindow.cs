using System;
using _Project.Features.Economy;
using _Project.Features.Progression;
using _Project.UI.Sounds;
using DG.Tweening;
using TMPro;
using UnityEngine;
using VContainer;

namespace _Project.UI.Windows
{
    public class LevelUpWindow : Window
    {
        private const float MinVisibleSeconds = 0.4f;
        private const float RaysTurnSeconds = 12f;
        private const float FullTurnDegrees = 360f;
        private const int InfiniteLoops = -1;

        [SerializeField] private TMP_Text _levelValue;
        [SerializeField] private TMP_Text _rewardValue;
        [SerializeField] private RectTransform _rays;

        private PlayerLevel _playerLevel;
        private CoinsFormatter _formatter;
        private UiSounds _sounds;
        private Tween _raysSpin;
        private float _shownTime;

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _raysSpin?.Kill();
        }

        [Inject]
        public void Construct(PlayerLevel playerLevel, CoinsFormatter formatter, UiSounds sounds)
        {
            _playerLevel = playerLevel ?? throw new ArgumentNullException(nameof(playerLevel));
            _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
            _sounds = sounds ?? throw new ArgumentNullException(nameof(sounds));
        }

        public void ShowPendingReward()
        {
            if (_playerLevel.HasPendingReward == false)
            {
                throw new InvalidOperationException(nameof(_playerLevel));
            }

            int level = _playerLevel.PendingRewardLevel;
            long reward = _playerLevel.GetReward(level);
            string rewardText = _formatter.FormatShort(reward);

            _levelValue.text = level.ToString();
            _rewardValue.text = $"+{rewardText}";
            Open();
        }

        public override void Show()
        {
            base.Show();
            _shownTime = Time.unscaledTime;
            StartRaysSpin();
        }

        public override void Hide()
        {
            _raysSpin?.Kill();
            base.Hide();
        }

        public override void Dismiss()
        {
            if (Time.unscaledTime - _shownTime < MinVisibleSeconds)
            {
                return;
            }

            _playerLevel.ClaimReward();
            base.Dismiss();
        }

        protected override void PlayOpenSound()
        {
            _sounds.PlayLevelUp();
        }

        private void StartRaysSpin()
        {
            _raysSpin?.Kill();
            _rays.localRotation = Quaternion.identity;
            _raysSpin = _rays
                .DOLocalRotate(new Vector3(0f, 0f, -FullTurnDegrees), RaysTurnSeconds, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetLoops(InfiniteLoops, LoopType.Restart)
                .SetUpdate(true)
                .SetLink(gameObject);
        }
    }
}
