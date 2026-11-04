using System;
using System.Collections.Generic;
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

        private TrackRoster _roster;
        private TrackPath _path;
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
                float distance = pair.Key.Distance;
                pair.Value.Place(_path.GetPoint(distance), _path.GetDirection(distance));
            }
        }

        private void OnDestroy()
        {
            _roster.VehicleSent -= OnVehicleSent;
            _roster.VehicleReturned -= OnVehicleReturned;
        }

        [Inject]
        public void Construct(TrackRoster roster, TrackPath path, IObjectResolver resolver)
        {
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
            _path = path ?? throw new ArgumentNullException(nameof(path));
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        private void ShowRunner(TrackRunner runner)
        {
            TrackVehicleView view = _freeViews.Count > 0 ? _freeViews.Pop() : _resolver.Instantiate(_vehiclePrefab, transform);
            view.Show(runner.Vehicle);
            view.Place(_path.GetPoint(runner.Distance), _path.GetDirection(runner.Distance));
            _vehicleViews.Add(runner, view);
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
    }
}
