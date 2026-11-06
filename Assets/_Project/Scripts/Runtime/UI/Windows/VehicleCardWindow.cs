using System;
using _Project.Core.Localization;
using _Project.Core.Track;
using _Project.Core.Vehicles;
using _Project.Features.Economy;
using _Project.Features.Garage;
using _Project.Features.Vehicles;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace _Project.UI.Windows
{
    public class VehicleCardWindow : Window, IVehicleCard
    {
        private const string RankKey = "ui.common.rank";
        private const string IncomeKey = "popup.vehicle.income";

        [SerializeField] private Image _icon;
        [SerializeField] private Image _rankBadge;
        [SerializeField] private TMP_Text _rankValue;
        [SerializeField] private TMP_Text _name;
        [SerializeField] private TMP_Text _rank;
        [SerializeField] private TMP_Text _income;
        [SerializeField] private Button _blockerButton;

        private ILocalizationService _localization;
        private CoinsFormatter _formatter;
        private TrackConfig _track;

        protected override void Awake()
        {
            base.Awake();
            _blockerButton.onClick.AddListener(OnBlockerClicked);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _blockerButton.onClick.RemoveListener(OnBlockerClicked);
        }

        [Inject]
        public void Construct(ILocalizationService localization, CoinsFormatter formatter, TrackConfig track)
        {
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
            _track = track ?? throw new ArgumentNullException(nameof(track));
        }

        public void Show(Vehicle vehicle)
        {
            if (vehicle == null)
            {
                throw new ArgumentNullException(nameof(vehicle));
            }

            VehicleDefinition definition = vehicle.Definition;
            string income = _formatter.FormatIncome(definition.CalculateAverageIncome(_track.LapLength));

            _icon.sprite = definition.GarageSprite;
            _rankBadge.color = definition.BadgeColor;
            _rankValue.text = definition.Rank.ToString();
            _name.text = _localization.Get(definition.NameKey);
            _rank.text = string.Format(_localization.Get(RankKey), definition.Rank);
            _income.text = string.Format(_localization.Get(IncomeKey), income);
            Open();
        }

        private void OnBlockerClicked()
        {
            Dismiss();
        }
    }
}
