using System;
using _Project.Core.Vehicles;
using _Project.Features.Progression;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace _Project.UI.Hud
{
    public class RankProgressView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _badgeValue;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private Image _fill;
        [SerializeField] [Min(0f)] private float _fillSeconds = 0.4f;

        private RankProgress _progress;
        private VehicleCatalogConfig _catalog;
        private Tween _fillTween;

        private void Start()
        {
            _progress.RankUnlocked += OnRankUnlocked;
            ShowRank();
            _fill.fillAmount = CalculateFilledPart();
        }

        private void OnDestroy()
        {
            _progress.RankUnlocked -= OnRankUnlocked;
            _fillTween?.Kill();
        }

        [Inject]
        public void Construct(RankProgress progress, VehicleCatalogConfig catalog)
        {
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        private void ShowRank()
        {
            int maxRank = _progress.MaxRank;
            int lastRank = _catalog.MaxRank;

            _badgeValue.text = maxRank.ToString();
            _label.text = $"{maxRank}/{lastRank}";
        }

        private float CalculateFilledPart()
        {
            return (float)_progress.MaxRank / _catalog.MaxRank;
        }

        private void AnimateFill()
        {
            _fillTween?.Kill();
            _fillTween = DOTween
                .To(() => _fill.fillAmount, value => _fill.fillAmount = value, CalculateFilledPart(), _fillSeconds)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        private void OnRankUnlocked(int rank)
        {
            ShowRank();
            AnimateFill();
        }
    }
}
