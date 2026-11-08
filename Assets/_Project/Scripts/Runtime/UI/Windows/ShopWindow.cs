using System;
using System.Collections.Generic;
using _Project.Core.Localization;
using _Project.Core.Vehicles;
using _Project.Features.Boxes;
using _Project.Features.Commands;
using _Project.Features.Economy;
using _Project.Features.Effects;
using _Project.Features.Progression;
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
        private const float ViewportCenter = 0.5f;

        private readonly List<ShopRowView> _rows = new List<ShopRowView>();

        [SerializeField] private ShopRowView _rowPrefab;
        [SerializeField] private RectTransform _rowsRoot;
        [SerializeField] private ScrollRect _scroll;
        [SerializeField] private FloatingTextView _failureMessage;
        [SerializeField] private float _failureMessageRise;

        private BoxDelivery _delivery;
        private RankProgress _progress;
        private VehicleCatalogConfig _catalog;
        private CoinsFormatter _formatter;
        private ILocalizationService _localization;
        private BoxPurchaseFailureText _failureText;
        private IObjectResolver _resolver;

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
            }
        }

        [Inject]
        public void Construct(
            BoxDelivery delivery,
            RankProgress progress,
            VehicleCatalogConfig catalog,
            CoinsFormatter formatter,
            ILocalizationService localization,
            BoxPurchaseFailureText failureText,
            IObjectResolver resolver)
        {
            _delivery = delivery ?? throw new ArgumentNullException(nameof(delivery));
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _failureText = failureText ?? throw new ArgumentNullException(nameof(failureText));
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        public override void Show()
        {
            ShowCatalog();
            base.Show();
            ScrollToBoxRank();
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
            row.ShowPrice(_formatter.FormatShort(price));
        }

        private void ScrollToBoxRank()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(_rowsRoot);
            RectTransform boxRankRow = (RectTransform)_rows[_progress.BoxRank - FirstRank].transform;
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
    }
}
