using System;
using _Project.UI.Sounds;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace _Project.UI.Windows
{
    public class Window : MonoBehaviour, IWindow
    {
        private const float PopInStartScale = 0.01f;
        private const float PopInPeakScale = 1.06f;
        private const float PopInRiseSeconds = 0.25f;
        private const float PopInSettleSeconds = 0.18f;

        [SerializeField] private GameObject _root;
        [SerializeField] private RectTransform _frame;
        [SerializeField] private Button _closeButton;
        [SerializeField] private bool _isFullscreen;

        private WindowQueue _windows;
        private UiSounds _sounds;
        private Sequence _popIn;

        public bool IsFullscreen => _isFullscreen;

        protected virtual void Awake()
        {
            _root.SetActive(false);
            _closeButton.onClick.AddListener(OnCloseClicked);
        }

        protected virtual void OnDestroy()
        {
            _closeButton.onClick.RemoveListener(OnCloseClicked);
            _popIn?.Kill();
        }

        [Inject]
        public void Construct(WindowQueue windows, UiSounds sounds)
        {
            _windows = windows ?? throw new ArgumentNullException(nameof(windows));
            _sounds = sounds ?? throw new ArgumentNullException(nameof(sounds));
        }

        public virtual void Show()
        {
            _root.SetActive(true);
            PlayPopIn();
            PlayOpenSound();
        }

        public virtual void Hide()
        {
            _popIn?.Kill();
            _frame.localScale = Vector3.one;
            _root.SetActive(false);
        }

        public virtual void Dismiss()
        {
            Close();
        }

        public void Open()
        {
            _windows.Open(this);
        }

        protected void Close()
        {
            _windows.Close(this);
        }

        protected virtual void PlayOpenSound()
        {
            _sounds.PlayWindowOpened();
        }

        private void PlayPopIn()
        {
            _popIn?.Kill();
            _frame.localScale = Vector3.one * PopInStartScale;
            _popIn = DOTween.Sequence()
                .Append(_frame.DOScale(PopInPeakScale, PopInRiseSeconds).SetEase(Ease.OutQuad))
                .Append(_frame.DOScale(1f, PopInSettleSeconds).SetEase(Ease.InOutQuad))
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        private void OnCloseClicked()
        {
            Dismiss();
        }
    }
}
