using System;
using System.Collections.Generic;
using _Project.Features.Economy;
using _Project.Features.Effects;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace _Project.Features.Track
{
    public class FinishPayoutsView : MonoBehaviour
    {
        private const string PayoutPrefix = "+";

        [SerializeField] private FloatingTextView _popupPrefab;
        [SerializeField] private Transform _spawnAnchor;

        private readonly List<FloatingTextView> _popups = new List<FloatingTextView>();
        private readonly Stack<FloatingTextView> _freePopups = new Stack<FloatingTextView>();

        private TrackRoster _roster;
        private CoinsFormatter _formatter;
        private IObjectResolver _resolver;

        private void Start()
        {
            _roster.FinishCrossed += OnFinishCrossed;
        }

        private void OnDestroy()
        {
            _roster.FinishCrossed -= OnFinishCrossed;

            foreach (FloatingTextView popup in _popups)
            {
                popup.Finished -= OnPopupFinished;
            }
        }

        [Inject]
        public void Construct(TrackRoster roster, CoinsFormatter formatter, IObjectResolver resolver)
        {
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
            _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        private FloatingTextView TakePopup()
        {
            if (_freePopups.Count > 0)
            {
                return _freePopups.Pop();
            }

            FloatingTextView popup = _resolver.Instantiate(_popupPrefab, transform);
            popup.Finished += OnPopupFinished;
            _popups.Add(popup);

            return popup;
        }

        private void OnFinishCrossed(TrackRunner runner, long payout)
        {
            string text = PayoutPrefix + _formatter.FormatShort(payout);
            Vector2 position = transform.InverseTransformPoint(_spawnAnchor.position);
            TakePopup().Show(text, position);
        }

        private void OnPopupFinished(FloatingTextView popup)
        {
            _freePopups.Push(popup);
        }
    }
}
