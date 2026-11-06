using System;
using _Project.Core.Localization;
using _Project.Core.Vehicles;
using _Project.Features.Boxes;
using _Project.Features.Commands;
using _Project.Features.Economy;
using _Project.Features.Effects;
using _Project.Features.Garage;
using _Project.Features.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace _Project.UI.Hud
{
    public class BoxButtonView : MonoBehaviour
    {
        private const string NoFreeSlotKey = "ui.hud.no_free_slot";
        private const string NotEnoughCoinsKey = "ui.hud.not_enough_coins";

        [SerializeField] private Button _button;
        [SerializeField] private Image _icon;
        [SerializeField] private Image _rankBadge;
        [SerializeField] private TMP_Text _rankValue;
        [SerializeField] private TMP_Text _price;
        [SerializeField] private FloatingTextView _failureMessage;
        [SerializeField] private Vector2 _failureMessagePosition;

        private BoxDelivery _delivery;
        private RankProgress _progress;
        private GarageBoard _garage;
        private VehicleCatalogConfig _catalog;
        private CoinsFormatter _formatter;
        private ILocalizationService _localization;

        private void Awake()
        {
            _failureMessage.gameObject.SetActive(false);
            _button.onClick.AddListener(OnButtonClicked);
        }

        private void Start()
        {
            _progress.RankUnlocked += OnRankUnlocked;
            _garage.BoxPlaced += OnBoxPlaced;
            ShowOffer();
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(OnButtonClicked);
            _progress.RankUnlocked -= OnRankUnlocked;
            _garage.BoxPlaced -= OnBoxPlaced;
        }

        [Inject]
        public void Construct(
            BoxDelivery delivery,
            RankProgress progress,
            GarageBoard garage,
            VehicleCatalogConfig catalog,
            CoinsFormatter formatter,
            ILocalizationService localization)
        {
            _delivery = delivery ?? throw new ArgumentNullException(nameof(delivery));
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _garage = garage ?? throw new ArgumentNullException(nameof(garage));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        }

        private void ShowOffer()
        {
            int boxRank = _progress.BoxRank;
            VehicleDefinition content = _catalog.GetByRank(boxRank);

            _icon.sprite = content.GarageSprite;
            _rankBadge.color = content.BadgeColor;
            _rankValue.text = boxRank.ToString();
            _price.text = _formatter.FormatShort(_delivery.BoxPrice);
        }

        private void ShowFailure(FailureReason reason)
        {
            string message = CreateFailureMessage(reason);
            _failureMessage.Show(message, _failureMessagePosition);
        }

        private string CreateFailureMessage(FailureReason reason)
        {
            switch (reason)
            {
                case FailureReason.NoFreeSlot:
                    return _localization.Get(NoFreeSlotKey);
                case FailureReason.NotEnoughCoins:
                    string missingCoins = _formatter.FormatExact(_delivery.MissingCoins);
                    return string.Format(_localization.Get(NotEnoughCoinsKey), missingCoins);
                default:
                    throw new ArgumentOutOfRangeException(nameof(reason));
            }
        }

        private void OnButtonClicked()
        {
            CommandResult result = _delivery.BuyBox();

            if (result.IsSuccess == false)
            {
                ShowFailure(result.FailureReason);
            }
        }

        private void OnRankUnlocked(int rank)
        {
            ShowOffer();
        }

        private void OnBoxPlaced(int slot, Box box)
        {
            ShowOffer();
        }
    }
}
