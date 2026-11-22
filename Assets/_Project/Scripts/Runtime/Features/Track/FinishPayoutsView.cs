using System;
using System.Collections.Generic;
using _Project.Features.Collector;
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
        [SerializeField] private ParticleSystem _coinBurst;
        [SerializeField] private ParticleSystem _collectorCoinBurst;
        [SerializeField] private int _coinBurstCount;

        private readonly List<FloatingTextView> _popups = new List<FloatingTextView>();
        private readonly Stack<FloatingTextView> _freePopups = new Stack<FloatingTextView>();

        private TrackRoster _roster;
        private CollectorService _collector;
        private TrackPath _path;
        private CoinsFormatter _formatter;
        private IObjectResolver _resolver;

        private void Start()
        {
            _roster.FinishCrossed += OnFinishCrossed;
            _collector.ChargeCollected += OnChargeCollected;
        }

        private void OnDestroy()
        {
            _roster.FinishCrossed -= OnFinishCrossed;
            _collector.ChargeCollected -= OnChargeCollected;

            foreach (FloatingTextView popup in _popups)
            {
                popup.Finished -= OnPopupFinished;
            }
        }

        [Inject]
        public void Construct(
            TrackRoster roster,
            CollectorService collector,
            TrackPath path,
            CoinsFormatter formatter,
            IObjectResolver resolver)
        {
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
            _collector = collector ?? throw new ArgumentNullException(nameof(collector));
            _path = path ?? throw new ArgumentNullException(nameof(path));
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

        private void ShowPayout(long payout, Vector2 position, ParticleSystem coinBurst)
        {
            string text = PayoutPrefix + _formatter.FormatShort(payout);
            TakePopup().Show(text, position);
            coinBurst.Emit(_coinBurstCount);
        }

        private void OnFinishCrossed(TrackRunner runner, long payout)
        {
            Vector2 position = transform.InverseTransformPoint(_spawnAnchor.position);
            ShowPayout(payout, position, _coinBurst);
        }

        private void OnChargeCollected(long amount)
        {
            ShowPayout(amount, _path.GetPoint(_collector.Distance), _collectorCoinBurst);
        }

        private void OnPopupFinished(FloatingTextView popup)
        {
            _freePopups.Push(popup);
        }
    }
}
