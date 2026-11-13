using System;
using _Project.Features.Progression;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace _Project.UI.Hud
{
    public class PlayerLevelView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _badgeValue;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private Image _fill;
        [SerializeField] [Min(0f)] private float _fillSeconds = 0.4f;

        private PlayerLevel _playerLevel;
        private Tween _fillTween;

        private void Start()
        {
            _playerLevel.ExperienceChanged += OnExperienceChanged;
            _playerLevel.LevelReached += OnLevelReached;
            ShowLevel();
            _fill.fillAmount = _playerLevel.LevelProgress;
        }

        private void OnDestroy()
        {
            _playerLevel.ExperienceChanged -= OnExperienceChanged;
            _playerLevel.LevelReached -= OnLevelReached;
            _fillTween?.Kill();
        }

        [Inject]
        public void Construct(PlayerLevel playerLevel)
        {
            _playerLevel = playerLevel ?? throw new ArgumentNullException(nameof(playerLevel));
        }

        private void ShowLevel()
        {
            _badgeValue.text = _playerLevel.Level.ToString();
            _label.text = $"{_playerLevel.Experience}/{_playerLevel.ExperienceToNextLevel}";
        }

        private void AnimateFill()
        {
            _fillTween?.Kill();
            _fillTween = DOTween
                .To(() => _fill.fillAmount, value => _fill.fillAmount = value, _playerLevel.LevelProgress, _fillSeconds)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        private void OnExperienceChanged()
        {
            ShowLevel();
            AnimateFill();
        }

        private void OnLevelReached(int level)
        {
            _fillTween?.Kill();
            _fill.fillAmount = 0f;
        }
    }
}
