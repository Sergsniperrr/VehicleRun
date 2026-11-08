using System;
using _Project.Core.Vehicles;
using _Project.Features.Boxes;
using _Project.Features.Commands;
using _Project.Features.Economy;
using _Project.Features.Effects;
using _Project.Features.Garage;
using _Project.Features.Progression;
using _Project.UI.Boxes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace _Project.UI.Hud
{
    public class BoxButtonView : MonoBehaviour
    {
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
        private BoxPurchaseFailureText _failureText;

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
            BoxPurchaseFailureText failureText)
        {
            _delivery = delivery ?? throw new ArgumentNullException(nameof(delivery));
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _garage = garage ?? throw new ArgumentNullException(nameof(garage));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
            _failureText = failureText ?? throw new ArgumentNullException(nameof(failureText));
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
            string message = _failureText.Create(reason, _delivery.MissingCoins);
            _failureMessage.Show(message, _failureMessagePosition);
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
