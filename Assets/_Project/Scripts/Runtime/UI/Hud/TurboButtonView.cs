using System;
using _Project.Core.Localization;
using _Project.Core.Turbo;
using _Project.Features.Commands;
using _Project.Features.Effects;
using _Project.Features.Turbo;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace _Project.UI.Hud
{
    public class TurboButtonView : MonoBehaviour
    {
        private const string UnlockRankKey = "ui.common.rank";
        private const string TrackEmptyKey = "ui.hud.turbo_track_empty";
        private const int SecondsPerMinute = 60;
        private const int NoShownSeconds = -1;

        [SerializeField] private Button _button;
        [SerializeField] private Image _icon;
        [SerializeField] private GameObject _lock;
        [SerializeField] private TMP_Text _unlockLabel;
        [SerializeField] private GameObject _timer;
        [SerializeField] private Image _timerFill;
        [SerializeField] private TMP_Text _timerValue;
        [SerializeField] private Color _lockedTint = Color.gray;
        [SerializeField] private Color _activeFillColor = Color.yellow;
        [SerializeField] private Color _cooldownFillColor = Color.white;
        [SerializeField] private FloatingTextView _failureMessage;
        [SerializeField] private Vector2 _failureMessagePosition;

        private TurboService _turbo;
        private TurboConfig _config;
        private ILocalizationService _localization;
        private int _shownSeconds = NoShownSeconds;

        private void Awake()
        {
            _failureMessage.gameObject.SetActive(false);
            _button.onClick.AddListener(OnButtonClicked);
        }

        private void Start()
        {
            _unlockLabel.text = string.Format(_localization.Get(UnlockRankKey), _config.UnlockRank);
            ShowState();
        }

        private void Update()
        {
            ShowState();
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(OnButtonClicked);
        }

        [Inject]
        public void Construct(TurboService turbo, TurboConfig config, ILocalizationService localization)
        {
            _turbo = turbo ?? throw new ArgumentNullException(nameof(turbo));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        }

        private void ShowState()
        {
            bool isActive = _turbo.IsActive;
            bool isCoolingDown = _turbo.IsCoolingDown;
            bool isBright = isActive || _turbo.CanStartTurbo();

            _icon.color = isBright ? Color.white : _lockedTint;
            _lock.SetActive(_turbo.IsUnlocked == false);
            _timer.SetActive(isActive || isCoolingDown);

            if (isActive)
            {
                ShowTimer(_turbo.RemainingSeconds, _turbo.RemainingProgress, _activeFillColor);
            }
            else if (isCoolingDown)
            {
                ShowTimer(_turbo.CooldownRemainingSeconds, _turbo.CooldownProgress, _cooldownFillColor);
            }
            else
            {
                _shownSeconds = NoShownSeconds;
            }
        }

        private void ShowTimer(float remainingSeconds, float progress, Color fillColor)
        {
            _timerFill.fillAmount = progress;
            _timerFill.color = fillColor;
            ShowRemainingSeconds(Mathf.CeilToInt(remainingSeconds));
        }

        private void ShowRemainingSeconds(int remainingSeconds)
        {
            if (remainingSeconds == _shownSeconds)
            {
                return;
            }

            _shownSeconds = remainingSeconds;
            int minutes = remainingSeconds / SecondsPerMinute;
            int seconds = remainingSeconds % SecondsPerMinute;
            _timerValue.text = $"{minutes}:{seconds:00}";
        }

        private void OnButtonClicked()
        {
            CommandResult result = _turbo.StartTurbo();

            if (result.FailureReason == FailureReason.TrackEmpty)
            {
                _failureMessage.Show(_localization.Get(TrackEmptyKey), _failureMessagePosition);
            }
        }
    }
}
