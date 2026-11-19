using System;
using System.Collections.Generic;
using _Project.Core.Advertising;
using _Project.Core.Localization;
using _Project.Core.Vehicles;
using _Project.Features.Boxes;
using _Project.Features.Commands;
using _Project.Features.Economy;
using _Project.Features.Effects;
using _Project.Features.Progression;
using _Project.Features.Shop;
using _Project.UI.Boxes;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;

namespace _Project.UI.Windows
{
    public class ShopWindow : Window
    {
        private const int FirstRank = 1;
        private const int NoRank = 0;
        private const float ViewportCenter = 0.5f;
        private const string RewardId = "shop_discount";
        private const string FreeLabelKey = "shop.reward.free";

        private readonly List<ShopRowView> _rows = new List<ShopRowView>();

        private int _freeOfferRank = NoRank;
        private int _discountOfferRank = NoRank;
        private bool _isWatchingAd;

        [SerializeField] private ShopRowView _rowPrefab;
        [SerializeField] private RectTransform _rowsRoot;
        [SerializeField] private ScrollRect _scroll;
        [SerializeField] private FloatingTextView _failureMessage;
        [SerializeField] private float _failureMessageRise;

        private BoxDelivery _delivery;
        private Wallet _wallet;
        private RankProgress _progress;
        private VehicleCatalogConfig _catalog;
        private CoinsFormatter _formatter;
        private ILocalizationService _localization;
        private BoxPurchaseFailureText _failureText;
        private IObjectResolver _resolver;
        private ShopRewards _rewards;
        private IAdvertisingService _advertising;

        protected override void Awake()
        {
            base.Awake();
            _failureMessage.gameObject.SetActive(false);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            foreach (ShopRowView row in _rows)
            {
                row.BuyClicked -= OnRowBuyClicked;
                row.RewardClicked -= OnRowRewardClicked;
            }
        }

        [Inject]
        public void Construct(
            BoxDelivery delivery,
            Wallet wallet,
            RankProgress progress,
            VehicleCatalogConfig catalog,
            CoinsFormatter formatter,
            ILocalizationService localization,
            BoxPurchaseFailureText failureText,
            IObjectResolver resolver,
            ShopRewards rewards,
            IAdvertisingService advertising)
        {
            _delivery = delivery ?? throw new ArgumentNullException(nameof(delivery));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _failureText = failureText ?? throw new ArgumentNullException(nameof(failureText));
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            _rewards = rewards ?? throw new ArgumentNullException(nameof(rewards));
            _advertising = advertising ?? throw new ArgumentNullException(nameof(advertising));
        }

        public override void Show()
        {
            ShowCatalog();
            base.Show();
            ScrollToBoxRank();
            ApplyRewardOffers();
            _wallet.CoinsChanged += OnCoinsChanged;
        }

        public override void Hide()
        {
            _wallet.CoinsChanged -= OnCoinsChanged;
            base.Hide();
        }

        private void ShowCatalog()
        {
            CreateMissingRows(_catalog.MaxRank);

            for (int i = 0; i < _rows.Count; i++)
            {
                ShowVehicle(_rows[i], i + FirstRank);
                ShowOffer(_rows[i]);
            }
        }

        private void CreateMissingRows(int rowCount)
        {
            while (_rows.Count < rowCount)
            {
                ShopRowView row = _resolver.Instantiate(_rowPrefab, _rowsRoot);
                row.BuyClicked += OnRowBuyClicked;
                row.RewardClicked += OnRowRewardClicked;
                _rows.Add(row);
            }
        }

        private void ShowVehicle(ShopRowView row, int rank)
        {
            VehicleDefinition definition = _catalog.GetByRank(rank);

            if (_progress.IsRankUnlocked(rank))
            {
                row.ShowVehicle(definition, _localization.Get(definition.NameKey));

                return;
            }

            row.ShowSilhouette(definition);
        }

        private void ShowOffer(ShopRowView row)
        {
            if (_delivery.IsRankForSale(row.Rank))
            {
                ShowPrice(row);

                return;
            }

            row.ShowLock();
        }

        private void ShowPrice(ShopRowView row)
        {
            long price = _delivery.GetPrice(row.Rank);
            bool isAffordable = _delivery.GetMissingCoins(row.Rank) == 0;
            row.ShowPrice(_formatter.FormatShort(price), isAffordable);
        }

        private void ScrollToBoxRank()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(_rowsRoot);
            RectTransform boxRankRow = (RectTransform)_rows[_progress.ShopMaxRank - FirstRank].transform;
            float viewportHeight = _scroll.viewport.rect.height;
            float hiddenHeight = Mathf.Max(0f, _rowsRoot.rect.height - viewportHeight);
            float rowDepth = -(boxRankRow.localPosition.y + boxRankRow.rect.center.y);
            float scrollHeight = Mathf.Clamp(rowDepth - viewportHeight * ViewportCenter, 0f, hiddenHeight);
            _scroll.StopMovement();
            _rowsRoot.anchoredPosition = new Vector2(_rowsRoot.anchoredPosition.x, scrollHeight);
        }

        private void ShowFailure(FailureReason reason, ShopRowView row)
        {
            string message = _failureText.Create(reason, _delivery.GetMissingCoins(row.Rank));
            Transform messageParent = _failureMessage.transform.parent;
            float buttonHeight = messageParent.InverseTransformPoint(row.BuyButtonPosition).y;
            _failureMessage.Show(message, new Vector3(0f, buttonHeight + _failureMessageRise, 0f));
        }

        private void OnRowBuyClicked(ShopRowView row)
        {
            CommandResult result = _delivery.BuyBox(row.Rank);

            if (result.IsSuccess)
            {
                ShowPrice(row);

                return;
            }

            ShowFailure(result.FailureReason, row);
        }

        private void OnCoinsChanged(long coins)
        {
            foreach (ShopRowView row in _rows)
            {
                ShowOffer(row);
            }
        }

        private void ApplyRewardOffers()
        {
            _freeOfferRank = NoRank;
            _discountOfferRank = NoRank;

            if (_rewards.IsReady == false)
            {
                HideAllRewards();

                return;
            }

            RewardOffer offer = _rewards.SelectOffer(SellableRanks());
            _freeOfferRank = offer.FreeRank;
            _discountOfferRank = offer.DiscountRank;
            ShowRewardButtons();
        }

        private List<int> SellableRanks()
        {
            List<int> sellable = new List<int>();

            for (int rank = FirstRank; rank <= _catalog.MaxRank; rank++)
            {
                if (_delivery.IsRankForSale(rank))
                {
                    sellable.Add(rank);
                }
            }

            return sellable;
        }

        private void ShowRewardButtons()
        {
            string freeLabel = _localization.Get(FreeLabelKey);
            string discountLabel = "-" + _rewards.DiscountPercent + "%";

            foreach (ShopRowView row in _rows)
            {
                if (row.Rank == _freeOfferRank)
                {
                    row.ShowReward(freeLabel);
                }
                else if (row.Rank == _discountOfferRank)
                {
                    row.ShowReward(discountLabel);
                }
                else
                {
                    row.HideReward();
                }
            }
        }

        private void HideAllRewards()
        {
            foreach (ShopRowView row in _rows)
            {
                row.HideReward();
            }
        }

        private void OnRowRewardClicked(ShopRowView row)
        {
            if (_isWatchingAd)
            {
                return;
            }

            bool isFree = row.Rank == _freeOfferRank;
            bool isDiscount = row.Rank == _discountOfferRank;

            if (isFree == false && isDiscount == false)
            {
                return;
            }

            if (_delivery.HasFreeSlot() == false)
            {
                ShowFailure(FailureReason.NoFreeSlot, row);

                return;
            }

            int rank = row.Rank;
            _isWatchingAd = true;
            _advertising.ShowRewarded(RewardId, () => OnRewardGranted(rank, isFree, row), () => _isWatchingAd = false);
        }

        private void OnRewardGranted(int rank, bool isFree, ShopRowView row)
        {
            CommandResult result = isFree ? _rewards.ClaimFree(rank) : _rewards.ClaimDiscount(rank);

            if (result.IsSuccess == false)
            {
                ShowFailure(result.FailureReason, row);

                return;
            }

            _freeOfferRank = NoRank;
            _discountOfferRank = NoRank;
            HideAllRewards();
            ShowOffer(row);
        }
    }
}
