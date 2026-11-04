using System;
using System.Threading;
using _Project.Features.Boxes;
using _Project.Features.Track;
using _Project.Features.Vehicles;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using VContainer;

namespace _Project.Features.Garage
{
    public class GarageInputView : MonoBehaviour
    {
        private const float LogicalScreenWidth = 360f;
        private const float LogicalScreenHeight = 800f;
        private const int MinDragThresholdPixels = 1;
        private const int NoSlot = -1;

        [SerializeField] private GarageView _garageView;
        [SerializeField] private Camera _camera;
        [SerializeField] private EventSystem _eventSystem;
        [SerializeField] private float _dragThreshold = 10f;
        [SerializeField] private GameObject _trackHighlight;
        [SerializeField] private float _laneHalfWidth = 1f;
        [SerializeField] private Collider2D _recycleZone;
        [SerializeField] private GameObject _recycleHighlight;

        private GarageBoard _garage;
        private GarageGestures _gestures;
        private TrackPath _path;
        private IRecycleConfirmation _recycleConfirmation;
        private int _dragSlot = NoSlot;
        private int _dragPointerId;
        private Vector3 _grabOffset;
        private bool _isConfirming;
        private int _screenWidth;
        private int _screenHeight;

        private bool IsDragging => _dragSlot != NoSlot;
        private bool IsBusy => IsDragging || _isConfirming;

        private void Start()
        {
            for (int slot = 0; slot < _garageView.SlotCount; slot++)
            {
                GarageSlotView view = _garageView.GetSlot(slot);
                view.Tapped += OnSlotTapped;
                view.DragStarted += OnSlotDragStarted;
                view.Dragged += OnSlotDragged;
                view.DragEnded += OnSlotDragEnded;
            }

            HideTargets();
            UpdateDragThreshold();
        }

        private void Update()
        {
            if (Screen.width != _screenWidth || Screen.height != _screenHeight)
            {
                UpdateDragThreshold();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus == false && IsDragging)
            {
                CancelDrag();
            }
        }

        private void OnDestroy()
        {
            for (int slot = 0; slot < _garageView.SlotCount; slot++)
            {
                GarageSlotView view = _garageView.GetSlot(slot);
                view.Tapped -= OnSlotTapped;
                view.DragStarted -= OnSlotDragStarted;
                view.Dragged -= OnSlotDragged;
                view.DragEnded -= OnSlotDragEnded;
            }
        }

        [Inject]
        public void Construct(GarageBoard garage, GarageGestures gestures, TrackPath path, IRecycleConfirmation recycleConfirmation)
        {
            _garage = garage ?? throw new ArgumentNullException(nameof(garage));
            _gestures = gestures ?? throw new ArgumentNullException(nameof(gestures));
            _path = path ?? throw new ArgumentNullException(nameof(path));
            _recycleConfirmation = recycleConfirmation ?? throw new ArgumentNullException(nameof(recycleConfirmation));
        }

        private void UpdateDragThreshold()
        {
            _screenWidth = Screen.width;
            _screenHeight = Screen.height;
            float pixelsPerUnit = Mathf.Min(_screenWidth / LogicalScreenWidth, _screenHeight / LogicalScreenHeight);
            _eventSystem.pixelDragThreshold = Mathf.Max(MinDragThresholdPixels, Mathf.RoundToInt(_dragThreshold * pixelsPerUnit));
        }

        private void StartDrag(int slot, PointerEventData eventData)
        {
            GarageSlotView view = _garageView.GetSlot(slot);
            _dragSlot = slot;
            _dragPointerId = eventData.pointerId;
            _grabOffset = view.ContentPosition - ToWorld(eventData.position);
            view.Lift();
            ShowTargets(slot);
        }

        private void FinishDrag(PointerEventData eventData)
        {
            int slot = _dragSlot;
            Vector3 dropPoint = ToWorld(eventData.position);
            _dragSlot = NoSlot;
            HideTargets();

            if (_recycleZone.OverlapPoint(dropPoint) && _gestures.CanRecycle(slot))
            {
                ConfirmRecycleAsync(slot, this.GetCancellationTokenOnDestroy()).Forget();

                return;
            }

            _garageView.GetSlot(slot).Land();

            if (TryFindSlot(dropPoint, out int targetSlot))
            {
                _gestures.DropOnSlot(slot, targetSlot);
            }
            else if (_path.MeasureDistanceToCenterline(dropPoint) <= _laneHalfWidth)
            {
                _gestures.DropOnTrack(slot);
            }
        }

        private void CancelDrag()
        {
            _garageView.GetSlot(_dragSlot).Land();
            _dragSlot = NoSlot;
            HideTargets();
        }

        private async UniTask ConfirmRecycleAsync(int slot, CancellationToken cancellationToken)
        {
            _isConfirming = true;
            bool isConfirmed;

            try
            {
                isConfirmed = await AskRecycleAsync(slot, cancellationToken);
            }
            finally
            {
                _isConfirming = false;
            }

            _garageView.GetSlot(slot).Land();

            if (isConfirmed)
            {
                _gestures.Recycle(slot);
            }
        }

        private UniTask<bool> AskRecycleAsync(int slot, CancellationToken cancellationToken)
        {
            if (_garage.TryGetVehicle(slot, out Vehicle vehicle))
            {
                return _recycleConfirmation.ConfirmVehicleAsync(vehicle, cancellationToken);
            }

            if (_garage.TryGetBox(slot, out Box box))
            {
                return _recycleConfirmation.ConfirmBoxAsync(box, cancellationToken);
            }

            throw new InvalidOperationException(nameof(slot));
        }

        private void ShowTargets(int slot)
        {
            for (int target = 0; target < _garageView.SlotCount; target++)
            {
                _garageView.GetSlot(target).ShowPairHighlight(_gestures.CanMerge(slot, target));
            }

            _trackHighlight.SetActive(_gestures.CanDropOnTrack(slot));
            _recycleHighlight.SetActive(_gestures.CanRecycle(slot));
        }

        private void HideTargets()
        {
            for (int target = 0; target < _garageView.SlotCount; target++)
            {
                _garageView.GetSlot(target).ShowPairHighlight(false);
            }

            _trackHighlight.SetActive(false);
            _recycleHighlight.SetActive(false);
        }

        private bool TryFindSlot(Vector2 point, out int slot)
        {
            for (int candidate = 0; candidate < _garageView.SlotCount; candidate++)
            {
                if (_garageView.GetSlot(candidate).Contains(point))
                {
                    slot = candidate;

                    return true;
                }
            }

            slot = NoSlot;

            return false;
        }

        private bool IsDragPointer(int slot, PointerEventData eventData)
        {
            return slot == _dragSlot && eventData.pointerId == _dragPointerId;
        }

        private Vector3 ToWorld(Vector2 screenPosition)
        {
            Vector3 point = _camera.ScreenToWorldPoint(screenPosition);

            return new Vector3(point.x, point.y, 0f);
        }

        private void OnSlotTapped(int slot, PointerEventData eventData)
        {
            if (IsBusy)
            {
                return;
            }

            _gestures.Tap(slot);
        }

        private void OnSlotDragStarted(int slot, PointerEventData eventData)
        {
            if (IsBusy || _gestures.CanDrag(slot) == false)
            {
                return;
            }

            StartDrag(slot, eventData);
        }

        private void OnSlotDragged(int slot, PointerEventData eventData)
        {
            if (IsDragPointer(slot, eventData) == false)
            {
                return;
            }

            _garageView.GetSlot(slot).MoveContent(ToWorld(eventData.position) + _grabOffset);
        }

        private void OnSlotDragEnded(int slot, PointerEventData eventData)
        {
            if (IsDragPointer(slot, eventData) == false)
            {
                return;
            }

            FinishDrag(eventData);
        }
    }
}
