using DG.Tweening;
using TMPro;
using UnityEngine;

namespace _Project.UI.Tutorial
{
    public class TutorialView : MonoBehaviour
    {
        private const float Half = 0.5f;

        [SerializeField] private RectTransform _area;
        [SerializeField] private TMP_Text _caption;
        [SerializeField] private CanvasGroup _dim;
        [SerializeField] private RectTransform _hand;
        [SerializeField] private int _focusSortingOrder = 20;
        [SerializeField] private Vector2 _handOffset = new Vector2(24f, -24f);
        [SerializeField] private Vector2 _dragHintFrom = new Vector2(-192f, 340f);
        [SerializeField] private Vector2 _dragHintTo = new Vector2(-480f, 340f);
        [SerializeField] [Min(0.1f)] private float _dragHintDuration = 1.1f;
        [SerializeField] [Range(0.5f, 1f)] private float _handPressScale = 0.85f;
        [SerializeField] [Min(0.05f)] private float _handPressDuration = 0.45f;
        [SerializeField] [Min(1f)] private float _charactersPerSecond = 45f;
        [SerializeField] [Min(0f)] private float _dimFadeDuration = 0.25f;

        private readonly Vector3[] _corners = new Vector3[4];
        private readonly TutorialWorldFocus _worldFocus = new TutorialWorldFocus();

        private Camera _uiCamera;
        private int _focusSortingLayerId;
        private TutorialFocusTarget _focused;
        private Tween _typing;
        private Tween _press;
        private Tween _drag;
        private Tween _dimFade;

        private void Awake()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            _uiCamera = canvas.rootCanvas.worldCamera;
            _focusSortingLayerId = canvas.sortingLayerID;
        }

        private void OnDisable()
        {
            _typing?.Kill();
            _press?.Kill();
            _drag?.Kill();
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

        public void Hide()
        {
            ClearPointer();
            gameObject.SetActive(false);
        }

        public void Point(RectTransform target)
        {
            _worldFocus.Release();
            Focus(target.GetComponent<TutorialFocusTarget>());
            ShowDim(true);
            _dim.blocksRaycasts = true;
            target.GetWorldCorners(_corners);
            ShowHand(_area.InverseTransformPoint((_corners[0] + _corners[2]) * Half));
        }

        public void PointWorld(Transform target)
        {
            FocusWorld(target);
            Vector2 screenPoint = _uiCamera.WorldToScreenPoint(target.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_area, screenPoint, _uiCamera, out Vector2 center);
            ShowHand(center);
        }

        public void PointWorldDrag(Transform target)
        {
            FocusWorld(target);
            ShowDragHand();
        }

        public void ClearPointer()
        {
            Focus(null);
            _worldFocus.Release();
            ShowDim(false);
            StopDrag();
            StopPress();
            _hand.gameObject.SetActive(false);
        }

        private void FocusWorld(Transform target)
        {
            Focus(null);
            _worldFocus.Focus(target, _focusSortingLayerId, _focusSortingOrder);
            ShowDim(true);
            _dim.blocksRaycasts = false;
        }

        private void ShowHand(Vector2 center)
        {
            StopDrag();
            ActivateHand();
            StartPress();
            _hand.anchoredPosition = center + _handOffset;
        }

        private void ShowDragHand()
        {
            ActivateHand();
            StopPress();

            if (_drag != null && _drag.IsActive())
            {
                return;
            }

            _hand.anchoredPosition = _dragHintFrom;
            _drag = DOTween.To(() => _hand.anchoredPosition, value => _hand.anchoredPosition = value, _dragHintTo, _dragHintDuration).SetLoops(-1, LoopType.Restart).SetEase(Ease.InOutSine).SetUpdate(true).SetLink(_hand.gameObject);
        }

        private void ActivateHand()
        {
            if (_hand.gameObject.activeSelf)
            {
                return;
            }

            _hand.gameObject.SetActive(true);
        }

        private void StartPress()
        {
            if (_press != null && _press.IsActive())
            {
                return;
            }

            _press = _hand.DOScale(_handPressScale, _handPressDuration).From(1f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true).SetLink(_hand.gameObject);
        }

        private void StopPress()
        {
            _press?.Kill();
            _press = null;
            _hand.localScale = Vector3.one;
        }

        private void StopDrag()
        {
            _drag?.Kill();
            _drag = null;
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
                _focused.Focus(_focusSortingLayerId, _focusSortingOrder);
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
                _dimFade = DOTween.To(() => _dim.alpha, value => _dim.alpha = value, 1f, _dimFadeDuration).SetUpdate(true).SetLink(_dim.gameObject);
            }
        }
    }
}
