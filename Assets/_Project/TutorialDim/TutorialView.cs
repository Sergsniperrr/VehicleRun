using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.Runtime.UI.Tutorial
{
    public class TutorialView : MonoBehaviour
    {
        private const float Half = 0.5f;

        [SerializeField] private RectTransform _area;
        [SerializeField] private RectTransform _captionPanel;
        [SerializeField] private TMP_Text _caption;
        [SerializeField] private Button _skipButton;
        [SerializeField] private TMP_Text _skipLabel;
        [SerializeField] private CanvasGroup _dim;
        [SerializeField] private RectTransform _hand;
        [SerializeField] private int _focusSortingOrder = 11;
        [SerializeField] private Vector2 _captionPosition = new Vector2(0f, -205f);
        [SerializeField] private Vector2 _raisedCaptionPosition = new Vector2(0f, -62f);
        [SerializeField] private Vector2 _handOffset = new Vector2(24f, -24f);
        [SerializeField] [Range(0.5f, 1f)] private float _handPressScale = 0.85f;
        [SerializeField] [Min(0.05f)] private float _handPressDuration = 0.45f;
        [SerializeField] [Min(1f)] private float _charactersPerSecond = 45f;
        [SerializeField] [Min(0f)] private float _dimFadeDuration = 0.25f;

        private readonly Vector3[] _corners = new Vector3[4];
        private TutorialFocusTarget _focused;
        private Tween _typing;
        private Tween _press;
        private Tween _dimFade;

        public Button SkipButton => _skipButton;

        private void OnDisable()
        {
            _typing?.Kill();
            _press?.Kill();
            _dimFade?.Kill();
        }

        public void Show(string caption, bool isTyped)
        {
            gameObject.SetActive(true);
            _caption.text = caption;
            _typing?.Kill();

            if (isTyped == false)
            {
                _caption.maxVisibleCharacters = caption.Length;
                return;
            }

            _caption.maxVisibleCharacters = 0;
            _typing = DOTween.To(() => _caption.maxVisibleCharacters, value => _caption.maxVisibleCharacters = value, caption.Length, caption.Length / _charactersPerSecond).SetEase(Ease.Linear).SetUpdate(true).SetLink(gameObject);
        }

        public void ShowSkipLabel(string text)
        {
            _skipLabel.text = text;
        }

        public void Raise(bool isRaised)
        {
            _captionPanel.anchoredPosition = isRaised ? _raisedCaptionPosition : _captionPosition;
        }

        public void Hide()
        {
            ClearPointer();
            gameObject.SetActive(false);
        }

        public void Point(RectTransform target, bool isDimmed)
        {
            Focus(isDimmed ? target.GetComponent<TutorialFocusTarget>() : null);
            ShowDim(isDimmed);
            target.GetWorldCorners(_corners);
            Vector2 center = _area.InverseTransformPoint((_corners[0] + _corners[2]) * Half);

            if (_hand.gameObject.activeSelf == false)
            {
                _hand.gameObject.SetActive(true);
                _press?.Kill();
                _press = _hand.DOScale(_handPressScale, _handPressDuration).From(1f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true).SetLink(_hand.gameObject);
            }

            _hand.anchoredPosition = center + _handOffset;
        }

        public void ClearPointer()
        {
            Focus(null);
            ShowDim(false);
            _press?.Kill();
            _press = null;
            _hand.localScale = Vector3.one;
            _hand.gameObject.SetActive(false);
        }

        private void Focus(TutorialFocusTarget target)
        {
            if (target == _focused)
            {
                return;
            }

            if (_focused != null)
            {
                _focused.Release();
            }

            _focused = target;

            if (_focused != null)
            {
                _focused.Focus(_focusSortingOrder);
            }
        }

        private void ShowDim(bool isShown)
        {
            if (_dim.gameObject.activeSelf == isShown)
            {
                return;
            }

            _dimFade?.Kill();
            _dim.gameObject.SetActive(isShown);

            if (isShown)
            {
                _dim.alpha = 0f;
                _dimFade = _dim.DOFade(1f, _dimFadeDuration).SetUpdate(true).SetLink(_dim.gameObject);
            }
        }
    }
}
