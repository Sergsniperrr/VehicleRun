using System;
using _Project.Core.Localization;
using _Project.Features.Boxes;
using _Project.Features.Garage;
using _Project.Features.Tutorial;
using _Project.Features.Vehicles;
using UnityEngine;
using VContainer;

namespace _Project.UI.Tutorial
{
    public class TutorialPresenter : MonoBehaviour
    {
        private const int NoSlot = -1;
        private const string CaptionKeyPrefix = "tutorial.step";
        private const string OpenBoxCaptionSuffix = ".open";
        private const int UpgradeRank = 2;

        [SerializeField] private TutorialView _view;
        [SerializeField] private GarageView _garageView;
        [SerializeField] private RectTransform _boxButton;
        [SerializeField] private Collider2D _recycleZone;
        [SerializeField] private RectTransform _recycleNoButton;
        [SerializeField] private Transform _trackRoad;
        [SerializeField] private Transform _trackVehicles;
        [SerializeField] private Collider2D _collectorBody;

        private TutorialService _tutorial;
        private GarageBoard _garage;
        private ILocalizationService _localization;
        private IFieldInputBlocker _inputBlocker;
        private string _shownCaptionKey;

        private void Update()
        {
            if (_tutorial == null)
            {
                return;
            }

            if (_tutorial.IsActive == false
                || (_inputBlocker.IsBlocking && _tutorial.Step != TutorialService.RecycleConfirmStep)
                || (_tutorial.Step == TutorialService.CollectorStep && _tutorial.IsCollectorPaused == false))
            {
                HideView();

                return;
            }

            ShowCaption();
            ShowPointer();
        }

        [Inject]
        public void Construct(
            TutorialService tutorial,
            GarageBoard garage,
            ILocalizationService localization,
            IFieldInputBlocker inputBlocker)
        {
            _tutorial = tutorial ?? throw new ArgumentNullException(nameof(tutorial));
            _garage = garage ?? throw new ArgumentNullException(nameof(garage));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _inputBlocker = inputBlocker ?? throw new ArgumentNullException(nameof(inputBlocker));
        }

        private void HideView()
        {
            if (_view.gameObject.activeSelf)
            {
                _view.Hide();
            }

            _shownCaptionKey = null;
        }

        private void ShowCaption()
        {
            string key = CaptionKey();

            if (_shownCaptionKey == key)
            {
                return;
            }

            _shownCaptionKey = key;
            _view.Show(_localization.Get(key), true);
        }

        private string CaptionKey()
        {
            if (_tutorial.IsPurchasedBoxWaiting)
            {
                return CaptionKeyPrefix + _tutorial.Step + OpenBoxCaptionSuffix;
            }

            return CaptionKeyPrefix + _tutorial.Step;
        }

        private void ShowPointer()
        {
            switch (_tutorial.Step)
            {
                case TutorialService.SendToTrackStep:
                    PointDragSlot(FindVehicleSlot(vehicle => true, false));
                    break;

                case TutorialService.OpenBoxStep:
                    PointSlot(FindBoxSlot());
                    break;

                case TutorialService.MergeStep:
                    PointMergePair();
                    break;

                case TutorialService.UpgradeTrackStep:
                    PointUpgrade();
                    break;

                case TutorialService.BuyBoxStep:
                    PointPurchase();
                    break;

                case TutorialService.RecycleStep:
                    PointRecycle();
                    break;

                case TutorialService.RecycleConfirmStep:
                    _view.Point(_recycleNoButton);
                    break;

                case TutorialService.TrackTapStep:
                    _view.PointTrack(_trackRoad, _trackVehicles);
                    break;

                case TutorialService.CollectorStep:
                    _view.PointCollector(_collectorBody);
                    break;
            }
        }

        private void PointPurchase()
        {
            if (_tutorial.IsPurchasedBoxWaiting)
            {
                PointSlot(FindBoxSlot());

                return;
            }

            _view.Point(_boxButton);
        }

        private void PointUpgrade()
        {
            int trackedOldSlot = FindVehicleSlot(vehicle => vehicle.Rank < UpgradeRank, true);

            if (trackedOldSlot >= 0)
            {
                PointSlot(trackedOldSlot);

                return;
            }

            PointDragSlot(FindVehicleSlot(vehicle => vehicle.Rank >= UpgradeRank, false));
        }

        private void PointSlot(int slot)
        {
            if (slot < 0)
            {
                _view.ClearPointer();

                return;
            }

            _view.PointWorld(_garageView.GetSlot(slot).transform);
        }

        private void PointDragSlot(int slot)
        {
            if (slot < 0)
            {
                _view.ClearPointer();

                return;
            }

            _view.PointWorldDrag(_garageView.GetSlot(slot).transform);
        }

        private void PointMergePair()
        {
            if (_tutorial.TryGetMergePair(out int sourceSlot, out int targetSlot) == false)
            {
                _view.ClearPointer();

                return;
            }

            _view.PointWorldPairDrag(_garageView.GetSlot(sourceSlot), _garageView.GetSlot(targetSlot));
        }

        private void PointRecycle()
        {
            if (_tutorial.TryGetRecycleSlot(out int slot) == false)
            {
                _view.ClearPointer();

                return;
            }

            _view.PointWorldToWorldDrag(_garageView.GetSlot(slot).transform, _recycleZone);
        }

        private int FindVehicleSlot(Func<Vehicle, bool> filter, bool isOnTrack)
        {
            for (int slot = 0; slot < _garage.SlotCount; slot++)
            {
                if (_garage.TryGetVehicle(slot, out Vehicle vehicle) && _garage.IsOnTrack(slot) == isOnTrack && filter(vehicle))
                {
                    return slot;
                }
            }

            return NoSlot;
        }

        private int FindBoxSlot()
        {
            for (int slot = 0; slot < _garage.SlotCount; slot++)
            {
                if (_garage.TryGetBox(slot, out Box _))
                {
                    return slot;
                }
            }

            return NoSlot;
        }

    }
}
