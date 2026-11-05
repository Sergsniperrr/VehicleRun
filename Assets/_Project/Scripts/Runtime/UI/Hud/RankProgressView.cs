using System;
using _Project.Core.Vehicles;
using _Project.Features.Progression;
using TMPro;
using UnityEngine;
using VContainer;

namespace _Project.UI.Hud
{
    public class RankProgressView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _badgeValue;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private RectTransform _fill;

        private RankProgress _progress;
        private VehicleCatalogConfig _catalog;

        private void Start()
        {
            _progress.RankUnlocked += OnRankUnlocked;
            ShowProgress();
        }

        private void OnDestroy()
        {
            _progress.RankUnlocked -= OnRankUnlocked;
        }

        [Inject]
        public void Construct(RankProgress progress, VehicleCatalogConfig catalog)
        {
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        private void ShowProgress()
        {
            int maxRank = _progress.MaxRank;
            int lastRank = _catalog.MaxRank;
            float filledPart = (float)maxRank / lastRank;

            _badgeValue.text = maxRank.ToString();
            _label.text = $"{maxRank}/{lastRank}";
            _fill.anchorMax = new Vector2(filledPart, _fill.anchorMax.y);
        }

        private void OnRankUnlocked(int rank)
        {
            ShowProgress();
        }
    }
}
