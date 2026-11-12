using System;
using _Project.Core.Audio;
using _Project.Core.Vehicles;
using _Project.Features.Boxes;
using _Project.Features.Vehicles;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using VContainer;
using Random = UnityEngine.Random;

namespace _Project.Features.Garage
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class GarageSlotView : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private const float OpaqueAlpha = 1f;
        private const float BoxShakeSettleFactor = 0.5f;
        private const int EndlessLoops = -1;

        [SerializeField] private SpriteRenderer _place;
        [SerializeField] private Color _openPlaceColor = Color.white;
        [SerializeField] private Color _lockedPlaceColor = Color.white;
        [SerializeField] private GameObject _lock;
        [SerializeField] private Transform _content;
        [SerializeField] private SortingGroup _contentSorting;
        [SerializeField] private SpriteRenderer _vehicle;
        [SerializeField] private SpriteRenderer _box;
        [SerializeField] private ParticleSystem _boxOpenBurst;
        [SerializeField] private Transform _rankBadge;
        [SerializeField] private SpriteRenderer _rankBadgeBack;
        [SerializeField] private TMP_Text _rankValue;
        [SerializeField] private Vector2 _vehicleBadgePosition;
        [SerializeField] private SpriteRenderer _shadow;
        [SerializeField] private Color _shadowColor = Color.black;
        [SerializeField] private float _onTrackAlpha = 0.5f;
        [SerializeField] private int _liftedSortingOrder = 100;
        [SerializeField] private float _boxShakeMinIntervalSeconds = 2.5f;
        [SerializeField] private float _boxShakeMaxIntervalSeconds = 3.5f;
        [SerializeField] private float _boxShakeAngle = 9f;
        [SerializeField] private float _boxShakeSwingSeconds = 0.07f;
        [SerializeField] private SpriteRenderer _mergeLeft;
        [SerializeField] private SpriteRenderer _mergeRight;
        [SerializeField] private ParticleSystem _mergeBurst;
        [SerializeField] private float _mergeSpread = 0.9f;
        [SerializeField] private float _mergeSeconds = 0.5f;
        [SerializeField] private float _mergePopStartScale = 0.6f;
        [SerializeField] private float _mergePopSeconds = 0.25f;
        [SerializeField] private float _mergeBurstHoldSeconds = 0.5f;

        private BoxCollider2D _collider;
        private int _restingSortingOrder;
        private IAudioService _audio;
        private GameSoundsConfig _sounds;
        private Tween _boxShake;
        private Tween _merge;
        private bool _isMergeResultShown;

        public event Action<int, PointerEventData> Tapped;
        public event Action<int, PointerEventData> DragStarted;
        public event Action<int, PointerEventData> Dragged;
        public event Action<int, PointerEventData> DragEnded;

        public int Slot { get; private set; }
        public Vector3 ContentPosition => _content.position;
        public bool IsMerging => _merge != null;

        private bool IsInputLocked => IsMerging && _isMergeResultShown == false;

        private void Awake()
        {
            _collider = GetComponent<BoxCollider2D>();
            _restingSortingOrder = _contentSorting.sortingOrder;
            _shadow.gameObject.SetActive(false);
            HideMergingVehicles();
        }

        private void OnDestroy()
        {
            StopBoxShake();
            StopMerge();
        }

        [Inject]
        public void Construct(IAudioService audio, GameSoundsConfig sounds)
        {
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));
            _sounds = sounds ?? throw new ArgumentNullException(nameof(sounds));
        }

        public void Bind(int slot)
        {
            Slot = slot;
        }

        public bool Contains(Vector2 worldPoint)
        {
            return _collider.OverlapPoint(worldPoint);
        }

        public void ShowLocked()
        {
            ShowPlace(_lockedPlaceColor, true);
            StopBoxShake();
            StopMerge();
            _content.gameObject.SetActive(false);
        }

        public void ShowEmpty()
        {
            ShowPlace(_openPlaceColor, false);
            StopBoxShake();
            StopMerge();
            _content.gameObject.SetActive(false);
        }

        public void ShowVehicle(Vehicle vehicle, bool isOnTrack)
        {
            if (vehicle == null)
            {
                throw new ArgumentNullException(nameof(vehicle));
            }

            float alpha = isOnTrack ? _onTrackAlpha : OpaqueAlpha;
            ShowPlace(_openPlaceColor, false);
            _vehicle.sprite = vehicle.Definition.GarageSprite;
            _vehicle.color = WithAlpha(Color.white, alpha);
            _vehicle.gameObject.SetActive(true);
            _box.gameObject.SetActive(false);
            StopBoxShake();
            StopMerge();
            ShowRank(vehicle.Definition, alpha);
            _content.gameObject.SetActive(true);
        }

        public void ShowBox(Box box)
        {
            if (box == null)
            {
                throw new ArgumentNullException(nameof(box));
            }

            ShowPlace(_openPlaceColor, false);
            _vehicle.gameObject.SetActive(false);
            _box.gameObject.SetActive(true);
            _rankBadge.gameObject.SetActive(false);
            StopMerge();
            _content.gameObject.SetActive(true);
            StartBoxShake();
        }

        public void PlayBoxOpened()
        {
            _boxOpenBurst.Play();
        }

        public void PlayMerge(VehicleDefinition merged)
        {
            if (merged == null)
            {
                throw new ArgumentNullException(nameof(merged));
            }

            StopMerge();
            _isMergeResultShown = false;

            float centerX = _vehicle.transform.localPosition.x;
            PlaceMergingVehicle(_mergeLeft, merged, -_mergeSpread);
            PlaceMergingVehicle(_mergeRight, merged, _mergeSpread);
            _content.gameObject.SetActive(false);

            _merge = DOTween.Sequence()
                .Append(_mergeLeft.transform.DOLocalMoveX(centerX, _mergeSeconds).SetEase(Ease.InQuad))
                .Join(_mergeRight.transform.DOLocalMoveX(centerX, _mergeSeconds).SetEase(Ease.InQuad))
                .AppendCallback(ShowMergeResult)
                .Append(_content.DOScale(Vector3.one, _mergePopSeconds).SetEase(Ease.OutBack))
                .AppendInterval(_mergeBurstHoldSeconds)
                .OnComplete(FinishMerge);
        }

        public void Lift()
        {
            SpriteRenderer item = _vehicle.gameObject.activeSelf ? _vehicle : _box;
            _shadow.sprite = item.sprite;
            _shadow.color = _shadowColor;
            _shadow.transform.localPosition = item.transform.localPosition;
            _shadow.transform.localScale = item.transform.localScale;
            _shadow.gameObject.SetActive(true);
            _contentSorting.sortingOrder = _liftedSortingOrder;
            StopBoxShake();
        }

        public void MoveContent(Vector3 worldPosition)
        {
            _content.position = worldPosition;
        }

        public void Land()
        {
            _content.localPosition = Vector3.zero;
            _contentSorting.sortingOrder = _restingSortingOrder;
            _shadow.gameObject.SetActive(false);

            if (_content.gameObject.activeSelf && _box.gameObject.activeSelf)
            {
                StartBoxShake();
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (IsInputLocked || eventData.dragging || eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            Tapped?.Invoke(Slot, eventData);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (IsInputLocked || eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            DragStarted?.Invoke(Slot, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            Dragged?.Invoke(Slot, eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            DragEnded?.Invoke(Slot, eventData);
        }

        private void ShowPlace(Color color, bool isLocked)
        {
            _place.color = color;
            _lock.SetActive(isLocked);
        }

        private void ShowRank(VehicleDefinition definition, float alpha)
        {
            _rankBadge.localPosition = _vehicleBadgePosition;
            _rankBadge.gameObject.SetActive(true);
            _rankBadgeBack.color = WithAlpha(definition.BadgeColor, alpha);
            _rankValue.text = definition.Rank.ToString();
            _rankValue.alpha = alpha;
        }

        private void StartBoxShake()
        {
            if (_boxShake != null)
            {
                return;
            }

            float interval = Random.Range(_boxShakeMinIntervalSeconds, _boxShakeMaxIntervalSeconds);
            float settleAngle = _boxShakeAngle * BoxShakeSettleFactor;

            _boxShake = DOTween.Sequence()
                .AppendInterval(interval)
                .AppendCallback(PlayShakeSound)
                .Append(CreateBoxSwing(_boxShakeAngle))
                .Append(CreateBoxSwing(-_boxShakeAngle))
                .Append(CreateBoxSwing(settleAngle))
                .Append(CreateBoxSwing(-settleAngle))
                .Append(CreateBoxSwing(0f))
                .SetLoops(EndlessLoops);
        }

        private void StopBoxShake()
        {
            if (_boxShake == null)
            {
                return;
            }

            _boxShake.Kill();
            _boxShake = null;
            _box.transform.localRotation = Quaternion.identity;
        }

        private void PlayShakeSound()
        {
            if (_sounds.BoxShake == null)
            {
                return;
            }

            _audio.PlayOneShot(_sounds.BoxShake);
        }

        private Tween CreateBoxSwing(float angle)
        {
            Vector3 rotation = new Vector3(0f, 0f, angle);

            return _box.transform.DOLocalRotate(rotation, _boxShakeSwingSeconds).SetEase(Ease.InOutSine);
        }

        private void PlaceMergingVehicle(SpriteRenderer vehicle, VehicleDefinition definition, float offsetX)
        {
            Vector3 position = _vehicle.transform.localPosition;
            position.x += offsetX;

            vehicle.sprite = definition.GarageSprite;
            vehicle.transform.localPosition = position;
            vehicle.transform.localScale = _vehicle.transform.localScale;
            vehicle.gameObject.SetActive(true);
        }

        private void ShowMergeResult()
        {
            HideMergingVehicles();
            _isMergeResultShown = true;
            _content.localScale = Vector3.one * _mergePopStartScale;
            _content.gameObject.SetActive(true);
            _mergeBurst.Play();
        }

        private void FinishMerge()
        {
            _merge = null;
        }

        private void StopMerge()
        {
            if (_merge == null)
            {
                return;
            }

            _merge.Kill();
            _merge = null;
            HideMergingVehicles();
            _content.localScale = Vector3.one;
        }

        private void HideMergingVehicles()
        {
            _mergeLeft.gameObject.SetActive(false);
            _mergeRight.gameObject.SetActive(false);
        }

        private Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;

            return color;
        }
    }
}
