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
        private const string SkipKey = "tutorial.skip";
        private const int UpgradeRank = 2;

        [SerializeField] private TutorialView _view;
        [SerializeField] private GarageView _garageView;
        [SerializeField] private RectTransform _boxButton;
        [SerializeField] private RectTransform _catalogButton;

        private TutorialService _tutorial;
        private GarageBoard _garage;
        private ILocalizationService _localization;
        private IFieldInputBlocker _inputBlocker;
        private string _shownCaptionKey;

        private void Start()
        {
            _view.SkipButton.onClick.AddListener(OnSkipClicked);
        }

        private void OnDestroy()
        {
            _view.SkipButton.onClick.RemoveListener(OnSkipClicked);
        }

        private void Update()
        {
            if (_tutorial == null)
            {
                return;
            }

            if (_tutorial.IsActive == false || _inputBlocker.IsBlocking)
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
            _view.ShowSkipLabel(_localization.Get(SkipKey));
            _view.SkipButton.gameObject.SetActive(_tutorial.CanSkip);
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
                    PointSlot(FindVehicleSlot(vehicle => true, false));
                    break;

                case TutorialService.OpenBoxStep:
                    PointSlot(FindBoxSlot());
                    break;

                case TutorialService.MergeStep:
                    PointSlot(FindMergeableSlot());
                    break;

                case TutorialService.UpgradeTrackStep:
                    PointUpgrade();
                    break;

                case TutorialService.BuyBoxStep:
                    PointPurchase();
                    break;

                case TutorialService.CatalogStep:
                    _view.Point(_catalogButton);
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

            PointSlot(FindVehicleSlot(vehicle => vehicle.Rank >= UpgradeRank, false));
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

        private int FindMergeableSlot()
        {
            for (int left = 0; left < _garage.SlotCount; left++)
            {
                if (IsGarageVehicle(left, out Vehicle leftVehicle) == false)
                {
                    continue;
                }

                for (int right = left + 1; right < _garage.SlotCount; right++)
                {
                    if (IsGarageVehicle(right, out Vehicle rightVehicle) && rightVehicle.Rank == leftVehicle.Rank)
                    {
                        return left;
                    }
                }
            }

            return NoSlot;
        }

        private bool IsGarageVehicle(int slot, out Vehicle vehicle)
        {
            return _garage.TryGetVehicle(slot, out vehicle) && _garage.IsOnTrack(slot) == false;
        }

        private void OnSkipClicked()
        {
            _tutorial.Skip();
        }
    }
}
