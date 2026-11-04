using System;
using System.Collections.Generic;
using _Project.Features.Economy;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace _Project.Features.Track
{
    public class FinishPayoutsView : MonoBehaviour
    {
        private const string PayoutPrefix = "+";
        private const float FinishDistance = 0f;

        [SerializeField] private PayoutPopupView _popupPrefab;
        [SerializeField] private Vector2 _offset;

        private readonly List<PayoutPopupView> _popups = new List<PayoutPopupView>();
        private readonly Stack<PayoutPopupView> _freePopups = new Stack<PayoutPopupView>();

        private TrackRoster _roster;
        private TrackPath _path;
        private CoinsFormatter _formatter;
        private IObjectResolver _resolver;

        private void Start()
        {
            _roster.FinishCrossed += OnFinishCrossed;
        }

        private void OnDestroy()
        {
            _roster.FinishCrossed -= OnFinishCrossed;

            foreach (PayoutPopupView popup in _popups)
            {
                popup.Finished -= OnPopupFinished;
            }
        }

        [Inject]
        public void Construct(TrackRoster roster, TrackPath path, CoinsFormatter formatter, IObjectResolver resolver)
        {
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
            _path = path ?? throw new ArgumentNullException(nameof(path));
            _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        private PayoutPopupView TakePopup()
        {
            if (_freePopups.Count > 0)
            {
                return _freePopups.Pop();
            }

            PayoutPopupView popup = _resolver.Instantiate(_popupPrefab, transform);
            popup.Finished += OnPopupFinished;
            _popups.Add(popup);

            return popup;
        }

        private void OnFinishCrossed(TrackRunner runner, long payout)
        {
            string text = PayoutPrefix + _formatter.Format(payout);
            Vector2 position = _path.GetPoint(FinishDistance) + _offset;
            TakePopup().Show(text, position);
        }

        private void OnPopupFinished(PayoutPopupView popup)
        {
            _freePopups.Push(popup);
        }
    }
}
