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

        private BoxDelivery _delivery;
        private int _shownSeconds = NoShownSeconds;

        private void Update()
        {
            bool isWaiting = _delivery.HasReadyDelivery;

            _fill.fillAmount = _delivery.FreeBoxProgress;
            _waitingIcon.SetActive(isWaiting);
            _seconds.gameObject.SetActive(isWaiting == false);

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
