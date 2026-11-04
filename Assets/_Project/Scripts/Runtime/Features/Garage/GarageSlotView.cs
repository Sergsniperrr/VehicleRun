using System;
using _Project.Core.Vehicles;
using _Project.Features.Boxes;
using _Project.Features.Vehicles;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

namespace _Project.Features.Garage
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class GarageSlotView : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private const float OpaqueAlpha = 1f;

        [SerializeField] private SpriteRenderer _place;
        [SerializeField] private Sprite _placeSprite;
        [SerializeField] private Sprite _pairPlaceSprite;
        [SerializeField] private Color _openPlaceColor = Color.white;
        [SerializeField] private Color _lockedPlaceColor = Color.white;
        [SerializeField] private GameObject _lock;
        [SerializeField] private Transform _content;
        [SerializeField] private SortingGroup _contentSorting;
        [SerializeField] private SpriteRenderer _vehicle;
        [SerializeField] private SpriteRenderer _box;
        [SerializeField] private Transform _rankBadge;
        [SerializeField] private SpriteRenderer _rankBadgeBack;
        [SerializeField] private TMP_Text _rankValue;
        [SerializeField] private Vector2 _vehicleBadgePosition;
        [SerializeField] private Vector2 _boxBadgePosition;
        [SerializeField] private SpriteRenderer _shadow;
        [SerializeField] private Color _shadowColor = Color.black;
        [SerializeField] private float _onTrackAlpha = 0.5f;
        [SerializeField] private int _liftedSortingOrder = 100;

        private BoxCollider2D _collider;
        private int _restingSortingOrder;

        public event Action<int, PointerEventData> Tapped;
        public event Action<int, PointerEventData> DragStarted;
        public event Action<int, PointerEventData> Dragged;
        public event Action<int, PointerEventData> DragEnded;

        public int Slot { get; private set; }
        public Vector3 ContentPosition => _content.position;

        private void Awake()
        {
            _collider = GetComponent<BoxCollider2D>();
            _restingSortingOrder = _contentSorting.sortingOrder;
            _shadow.gameObject.SetActive(false);
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
            _content.gameObject.SetActive(false);
        }

        public void ShowEmpty()
        {
            ShowPlace(_openPlaceColor, false);
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
            ShowRank(vehicle.Definition, _vehicleBadgePosition, alpha);
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
            ShowRank(box.Content, _boxBadgePosition, OpaqueAlpha);
            _content.gameObject.SetActive(true);
        }

        public void ShowPairHighlight(bool isVisible)
        {
            _place.sprite = isVisible ? _pairPlaceSprite : _placeSprite;
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
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.dragging || eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            Tapped?.Invoke(Slot, eventData);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
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

        private void ShowRank(VehicleDefinition definition, Vector2 badgePosition, float alpha)
        {
            _rankBadge.localPosition = badgePosition;
            _rankBadgeBack.color = WithAlpha(definition.BadgeColor, alpha);
            _rankValue.text = definition.Rank.ToString();
            _rankValue.alpha = alpha;
        }

        private Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;

            return color;
        }
    }
}
