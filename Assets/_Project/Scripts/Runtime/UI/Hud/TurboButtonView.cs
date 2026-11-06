using System;
using _Project.Core.Localization;
using _Project.Core.Turbo;
using _Project.Features.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace _Project.UI.Hud
{
    public class TurboButtonView : MonoBehaviour
    {
        private const string UnlockRankKey = "ui.common.rank";

        [SerializeField] private Image _icon;
        [SerializeField] private GameObject _lock;
        [SerializeField] private TMP_Text _unlockLabel;
        [SerializeField] private Color _lockedTint = Color.gray;

        private RankProgress _progress;
        private TurboConfig _turbo;
        private ILocalizationService _localization;

        private bool IsUnlocked => _progress.MaxRank >= _turbo.UnlockRank;

        private void Start()
        {
            _progress.RankUnlocked += OnRankUnlocked;
            ShowState();
        }

        private void OnDestroy()
        {
            _progress.RankUnlocked -= OnRankUnlocked;
        }

        [Inject]
        public void Construct(RankProgress progress, TurboConfig turbo, ILocalizationService localization)
        {
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _turbo = turbo ?? throw new ArgumentNullException(nameof(turbo));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        }

        private void ShowState()
        {
            bool isUnlocked = IsUnlocked;

            _icon.color = isUnlocked ? Color.white : _lockedTint;
            _lock.SetActive(isUnlocked == false);
            _unlockLabel.text = string.Format(_localization.Get(UnlockRankKey), _turbo.UnlockRank);
        }

        private void OnRankUnlocked(int rank)
        {
            ShowState();
        }
    }
}
