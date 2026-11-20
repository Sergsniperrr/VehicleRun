using System;
using _Project.Features.Boxes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace _Project.UI.Hud
{
    public class FreeBoxView : MonoBehaviour
    {
        private const int NoShownSeconds = -1;

        [SerializeField] private Image _fill;
        [SerializeField] private TMP_Text _seconds;
        [SerializeField] private GameObject _waitingIcon;
        [SerializeField] private Color _normalColor = new Color(0.33f, 0.8f, 0.16f, 1f);
        [SerializeField] private Color _alertColor = new Color(0.9f, 0.2f, 0.17f, 1f);

        private BoxDelivery _delivery;
        private int _shownSeconds = NoShownSeconds;
        private bool _isAlertShown;

        private void Awake()
        {
            _isAlertShown = false;
            _fill.color = _normalColor;
        }

        private void Update()
        {
            bool isWaiting = _delivery.HasReadyDelivery;

            _fill.fillAmount = _delivery.FreeBoxProgress;
            _waitingIcon.SetActive(isWaiting);
            _seconds.gameObject.SetActive(isWaiting == false);
            ApplyFillColor(isWaiting);

            if (isWaiting == false)
            {
                ShowRemainingSeconds();
            }
        }

        [Inject]
        public void Construct(BoxDelivery delivery)
        {
            _delivery = delivery ?? throw new ArgumentNullException(nameof(delivery));
        }

        private void ApplyFillColor(bool isAlert)
        {
            if (isAlert == _isAlertShown)
            {
                return;
            }

            _isAlertShown = isAlert;
            _fill.color = isAlert ? _alertColor : _normalColor;
        }

        private void ShowRemainingSeconds()
        {
            int remainingSeconds = Mathf.CeilToInt(_delivery.FreeBoxRemainingSeconds);

            if (remainingSeconds == _shownSeconds)
            {
                return;
            }

            _shownSeconds = remainingSeconds;
            _seconds.text = remainingSeconds.ToString();
        }
    }
}
