using System;
using System.Collections.Generic;
using _Project.Features.Garage;
using _Project.Features.Turbo;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace _Project.Features.Track
{
    public class TrackView : MonoBehaviour
    {
        [SerializeField] private TrackVehicleView _vehiclePrefab;

        private readonly Dictionary<TrackRunner, TrackVehicleView> _vehicleViews = new Dictionary<TrackRunner, TrackVehicleView>();
        private readonly Stack<TrackVehicleView> _freeViews = new Stack<TrackVehicleView>();
        private readonly List<TrackVehicleView> _createdViews = new List<TrackVehicleView>();

        private TrackRoster _roster;
        private TrackPath _path;
        private TurboService _turbo;
        private IFieldInputBlocker _inputBlocker;
        private IObjectResolver _resolver;

        private void Start()
        {
            foreach (TrackRunner runner in _roster.Runners)
            {
                ShowRunner(runner);
            }

            _roster.VehicleSent += OnVehicleSent;
            _roster.VehicleReturned += OnVehicleReturned;
        }

        private void Update()
        {
            foreach (KeyValuePair<TrackRunner, TrackVehicleView> pair in _vehicleViews)
            {
                TrackRunner runner = pair.Key;
                TrackVehicleView view = pair.Value;
                view.Place(_path.GetPoint(runner.Distance), _path.GetDirection(runner.Distance));
                view.ShowBoost(_turbo.IsRunnerBoosted(runner));
            }
        }

        private void OnDestroy()
        {
            _roster.VehicleSent -= OnVehicleSent;
            _roster.VehicleReturned -= OnVehicleReturned;

            foreach (TrackVehicleView view in _createdViews)
            {
                view.Tapped -= OnVehicleTapped;
            }
        }

        [Inject]
        public void Construct(
            TrackRoster roster,
            TrackPath path,
            TurboService turbo,
            IFieldInputBlocker inputBlocker,
            IObjectResolver resolver)
        {
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
            _path = path ?? throw new ArgumentNullException(nameof(path));
            _turbo = turbo ?? throw new ArgumentNullException(nameof(turbo));
            _inputBlocker = inputBlocker ?? throw new ArgumentNullException(nameof(inputBlocker));
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        private void ShowRunner(TrackRunner runner)
        {
            TrackVehicleView view = _freeViews.Count > 0 ? _freeViews.Pop() : CreateView();
            view.Show(runner.Vehicle);
            view.Place(_path.GetPoint(runner.Distance), _path.GetDirection(runner.Distance));
            _vehicleViews.Add(runner, view);
        }

        private TrackVehicleView CreateView()
        {
            TrackVehicleView view = _resolver.Instantiate(_vehiclePrefab, transform);
            view.Tapped += OnVehicleTapped;
            _createdViews.Add(view);

            return view;
        }

        private bool TryFindRunner(TrackVehicleView view, out TrackRunner runner)
        {
            foreach (KeyValuePair<TrackRunner, TrackVehicleView> pair in _vehicleViews)
            {
                if (pair.Value == view)
                {
                    runner = pair.Key;

                    return true;
                }
            }

            runner = null;

            return false;
        }

        private void OnVehicleSent(TrackRunner runner)
        {
            ShowRunner(runner);
        }

        private void OnVehicleReturned(TrackRunner runner)
        {
            if (_vehicleViews.TryGetValue(runner, out TrackVehicleView view) == false)
            {
                return;
            }

            view.Hide();
            _vehicleViews.Remove(runner);
            _freeViews.Push(view);
        }

        private void OnVehicleTapped(TrackVehicleView view)
        {
            if (_inputBlocker.IsBlocking)
            {
                return;
            }

            if (TryFindRunner(view, out TrackRunner runner))
            {
                _turbo.BoostRunner(runner);
            }
        }
    }
}
