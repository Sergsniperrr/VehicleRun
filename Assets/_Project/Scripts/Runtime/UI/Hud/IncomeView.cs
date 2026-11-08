using System;
using _Project.Features.Economy;
using _Project.Features.Track;
using _Project.Features.Turbo;
using TMPro;
using UnityEngine;
using VContainer;

namespace _Project.UI.Hud
{
    [RequireComponent(typeof(TMP_Text))]
    public class IncomeView : MonoBehaviour
    {
        private const string IncomePrefix = "+";

        [SerializeField] private RectTransform _turboBadge;
        [SerializeField] private float _turboBadgeGap;

        private TMP_Text _label;
        private IncomeService _income;
        private TrackRoster _roster;
        private TurboService _turbo;
        private CoinsFormatter _formatter;

        private void Awake()
        {
            _label = GetComponent<TMP_Text>();
        }

        private void Start()
        {
            _roster.VehicleSent += OnVehicleSent;
            _roster.VehicleReturned += OnVehicleReturned;
            _turbo.TurboStarted += OnTurboStarted;
            _turbo.TurboEnded += OnTurboEnded;
            ShowIncome();
        }

        private void OnDestroy()
        {
            _roster.VehicleSent -= OnVehicleSent;
            _roster.VehicleReturned -= OnVehicleReturned;
            _turbo.TurboStarted -= OnTurboStarted;
            _turbo.TurboEnded -= OnTurboEnded;
        }

        [Inject]
        public void Construct(IncomeService income, TrackRoster roster, TurboService turbo, CoinsFormatter formatter)
        {
            _income = income ?? throw new ArgumentNullException(nameof(income));
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
            _turbo = turbo ?? throw new ArgumentNullException(nameof(turbo));
            _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
        }

        private void ShowIncome()
        {
            double incomePerSecond = _income.CalculateIncomePerSecond();
            _label.text = IncomePrefix + _formatter.FormatIncome(incomePerSecond);
            ShowTurboBadge();
        }

        private void ShowTurboBadge()
        {
            bool isTurboActive = _turbo.IsActive;
            _turboBadge.gameObject.SetActive(isTurboActive);

            if (isTurboActive == false)
            {
                return;
            }

            _label.ForceMeshUpdate();
            float textRightEdge = _label.textBounds.max.x;
            _turboBadge.anchoredPosition = new Vector2(textRightEdge + _turboBadgeGap, _turboBadge.anchoredPosition.y);
        }

        private void OnVehicleSent(TrackRunner runner)
        {
            ShowIncome();
        }

        private void OnVehicleReturned(TrackRunner runner)
        {
            ShowIncome();
        }

        private void OnTurboStarted()
        {
            ShowIncome();
        }

        private void OnTurboEnded()
        {
            ShowIncome();
        }
    }
}
