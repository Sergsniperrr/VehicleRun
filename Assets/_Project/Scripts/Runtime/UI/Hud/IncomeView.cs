using System;
using _Project.Features.Economy;
using _Project.Features.Track;
using TMPro;
using UnityEngine;
using VContainer;

namespace _Project.UI.Hud
{
    [RequireComponent(typeof(TMP_Text))]
    public class IncomeView : MonoBehaviour
    {
        private const string IncomePrefix = "+";

        private TMP_Text _label;
        private IncomeService _income;
        private TrackRoster _roster;
        private CoinsFormatter _formatter;

        private void Awake()
        {
            _label = GetComponent<TMP_Text>();
        }

        private void Start()
        {
            _roster.VehicleSent += OnVehicleSent;
            _roster.VehicleReturned += OnVehicleReturned;
            ShowIncome();
        }

        private void OnDestroy()
        {
            _roster.VehicleSent -= OnVehicleSent;
            _roster.VehicleReturned -= OnVehicleReturned;
        }

        [Inject]
        public void Construct(IncomeService income, TrackRoster roster, CoinsFormatter formatter)
        {
            _income = income ?? throw new ArgumentNullException(nameof(income));
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
            _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
        }

        private void ShowIncome()
        {
            long incomePerSecond = (long)Math.Round(_income.CalculateIncomePerSecond(), MidpointRounding.AwayFromZero);
            _label.text = IncomePrefix + _formatter.Format(incomePerSecond);
        }

        private void OnVehicleSent(TrackRunner runner)
        {
            ShowIncome();
        }

        private void OnVehicleReturned(TrackRunner runner)
        {
            ShowIncome();
        }
    }
}
