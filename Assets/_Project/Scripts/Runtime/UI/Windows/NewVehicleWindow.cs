using System;
using _Project.Core.Localization;
using _Project.Core.Track;
using _Project.Core.Vehicles;
using _Project.Features.Economy;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace _Project.UI.Windows
{
    public class NewVehicleWindow : Window
    {
        private const string RankKey = "ui.common.rank";
        private const string IncomeKey = "popup.vehicle.income";
        private const float MinVisibleSeconds = 0.4f;
        private const float RaysTurnSeconds = 12f;
        private const float FullTurnDegrees = 360f;
        private const int InfiniteLoops = -1;

        [SerializeField] private Image _icon;
        [SerializeField] private Image _rankBadge;
        [SerializeField] private TMP_Text _rankValue;
        [SerializeField] private TMP_Text _name;
        [SerializeField] private TMP_Text _rank;
        [SerializeField] private TMP_Text _income;
        [SerializeField] private RectTransform _rays;

        private ILocalizationService _localization;
        private CoinsFormatter _formatter;
        private TrackConfig _track;
        private Tween _raysSpin;
        private float _shownTime;

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _raysSpin?.Kill();
        }

        [Inject]
        public void Construct(ILocalizationService localization, CoinsFormatter formatter, TrackConfig track)
        {
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
            _track = track ?? throw new ArgumentNullException(nameof(track));
        }

        public void Show(VehicleDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            string income = _formatter.FormatIncome(definition.CalculateAverageIncome(_track.LapLength));

            _icon.sprite = definition.GarageSprite;
            _rankBadge.color = definition.BadgeColor;
            _rankValue.text = definition.Rank.ToString();
            _name.text = _localization.Get(definition.NameKey);
            _rank.text = string.Format(_localization.Get(RankKey), definition.Rank);
            _income.text = string.Format(_localization.Get(IncomeKey), income);
            Open();
        }

        public override void Show()
        {
            base.Show();
            _shownTime = Time.unscaledTime;
            StartRaysSpin();
        }

        public override void Hide()
        {
            _raysSpin?.Kill();
            base.Hide();
        }

        public override void Dismiss()
        {
            if (Time.unscaledTime - _shownTime < MinVisibleSeconds)
            {
                return;
            }

            base.Dismiss();
        }

        private void StartRaysSpin()
        {
            _raysSpin?.Kill();
            _rays.localRotation = Quaternion.identity;
            _raysSpin = _rays
                .DOLocalRotate(new Vector3(0f, 0f, -FullTurnDegrees), RaysTurnSeconds, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetLoops(InfiniteLoops, LoopType.Restart)
                .SetUpdate(true)
                .SetLink(gameObject);
        }
    }
}
