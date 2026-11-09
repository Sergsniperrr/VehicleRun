using System;
using System.Collections.Generic;
using _Project.Core.Localization;
using _Project.Core.Track;
using _Project.Core.Vehicles;
using _Project.Features.Commands;
using _Project.Features.Economy;
using _Project.Features.Effects;
using _Project.Features.Progression;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;

namespace _Project.UI.Windows
{
    public class CatalogWindow : Window
    {
        private const int FirstRank = 1;
        private const float ViewportCenter = 0.5f;
        private const string ConditionKey = "popup.catalog.unlock_condition";

        private readonly List<CatalogCardView> _cards = new List<CatalogCardView>();

        [SerializeField] private CatalogCardView _cardPrefab;
        [SerializeField] private RectTransform _cardsRoot;
        [SerializeField] private ScrollRect _scroll;
        [SerializeField] private CatalogMilestoneView[] _milestones;
        [SerializeField] private FloatingTextView _claimPopup;
        [SerializeField] private float _claimPopupRise;

        private RankProgress _progress;
        private VehicleCatalogConfig _catalog;
        private CoinsFormatter _formatter;
        private ILocalizationService _localization;
        private TrackConfig _track;
        private IObjectResolver _resolver;
        private CollectionMilestones _collection;

        protected override void Awake()
        {
            base.Awake();
            _claimPopup.gameObject.SetActive(false);

            foreach (CatalogMilestoneView milestone in _milestones)
            {
                milestone.Clicked += OnMilestoneClicked;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            foreach (CatalogMilestoneView milestone in _milestones)
            {
                milestone.Clicked -= OnMilestoneClicked;
            }
        }

        [Inject]
        public void Construct(
            RankProgress progress,
            VehicleCatalogConfig catalog,
            CoinsFormatter formatter,
            ILocalizationService localization,
            TrackConfig track,
            IObjectResolver resolver,
            CollectionMilestones collection)
        {
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _track = track ?? throw new ArgumentNullException(nameof(track));
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            _collection = collection ?? throw new ArgumentNullException(nameof(collection));
        }

        public override void Show()
        {
            ShowCatalog();
            base.Show();
            ScrollToNextGoal();
        }

        private void ShowCatalog()
        {
            CreateMissingCards(_catalog.MaxRank);

            for (int i = 0; i < _cards.Count; i++)
            {
                ShowCard(_cards[i], i + FirstRank);
            }

            ShowMilestones();
        }

        private void ShowMilestones()
        {
            foreach (CatalogMilestoneView milestone in _milestones)
            {
                milestone.Show(_collection.GetState(milestone.Rank));
            }
        }

        private void CreateMissingCards(int cardCount)
        {
            while (_cards.Count < cardCount)
            {
                CatalogCardView card = _resolver.Instantiate(_cardPrefab, _cardsRoot);
                _cards.Add(card);
            }
        }

        private void ShowCard(CatalogCardView card, int rank)
        {
            VehicleDefinition definition = _catalog.GetByRank(rank);

            if (_progress.IsRankUnlocked(rank))
            {
                string income = _formatter.FormatIncome(definition.CalculateAverageIncome(_track.LapLength));
                card.ShowVehicle(definition, _localization.Get(definition.NameKey), income);

                return;
            }

            card.ShowSilhouette(definition, string.Format(_localization.Get(ConditionKey), rank - 1));
        }

        private void ScrollToNextGoal()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(_cardsRoot);
            RectTransform goalCard = (RectTransform)_cards[NextGoalRank() - FirstRank].transform;
            float viewportHeight = _scroll.viewport.rect.height;
            float hiddenHeight = Mathf.Max(0f, _cardsRoot.rect.height - viewportHeight);
            float cardDepth = -(goalCard.localPosition.y + goalCard.rect.center.y);
            float scrollHeight = Mathf.Clamp(cardDepth - viewportHeight * ViewportCenter, 0f, hiddenHeight);
            _scroll.StopMovement();
            _cardsRoot.anchoredPosition = new Vector2(_cardsRoot.anchoredPosition.x, scrollHeight);
        }

        private int NextGoalRank()
        {
            for (int rank = FirstRank; rank <= _catalog.MaxRank; rank++)
            {
                if (_progress.IsRankUnlocked(rank) == false)
                {
                    return rank;
                }
            }

            return _catalog.MaxRank;
        }

        private void OnMilestoneClicked(CatalogMilestoneView milestone)
        {
            long reward = _collection.GetReward(milestone.Rank);
            CommandResult result = _collection.Claim(milestone.Rank);

            if (result.IsSuccess == false)
            {
                return;
            }

            milestone.Show(_collection.GetState(milestone.Rank));
            ShowClaimPopup(milestone, reward);
        }

        private void ShowClaimPopup(CatalogMilestoneView milestone, long reward)
        {
            Transform parent = _claimPopup.transform.parent;
            Vector3 localPosition = parent.InverseTransformPoint(milestone.Position);
            string text = "+" + _formatter.FormatShort(reward);
            _claimPopup.Show(text, new Vector3(localPosition.x, localPosition.y + _claimPopupRise, 0f));
        }
    }
}
