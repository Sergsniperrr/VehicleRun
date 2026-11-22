using System;
using _Project.Features.Garage;
using _Project.Features.Track;
using UnityEngine;
using UnityEngine.EventSystems;
using VContainer;

namespace _Project.Features.Collector
{
    public class CollectorView : MonoBehaviour, IPointerDownHandler
    {
        private const float SpriteForwardAngle = 90f;

        [SerializeField] private GameObject _body;

        private CollectorService _collector;
        private TrackPath _path;
        private IFieldInputBlocker _inputBlocker;

        private void Start()
        {
            _collector.Appeared += OnAppeared;
            _collector.Disappeared += OnDisappeared;
            _body.SetActive(_collector.IsActive);
            Place();
        }

        private void Update()
        {
            if (_collector.IsActive)
            {
                Place();
            }
        }

        private void OnDestroy()
        {
            _collector.Appeared -= OnAppeared;
            _collector.Disappeared -= OnDisappeared;
        }

        [Inject]
        public void Construct(CollectorService collector, TrackPath path, IFieldInputBlocker inputBlocker)
        {
            _collector = collector ?? throw new ArgumentNullException(nameof(collector));
            _path = path ?? throw new ArgumentNullException(nameof(path));
            _inputBlocker = inputBlocker ?? throw new ArgumentNullException(nameof(inputBlocker));
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            if (_inputBlocker.IsBlocking)
            {
                return;
            }

            _collector.Collect();
        }

        private void Place()
        {
            Vector2 direction = _path.GetDirection(_collector.Distance);
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - SpriteForwardAngle;
            transform.localPosition = _path.GetPoint(_collector.Distance);
            transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void OnAppeared()
        {
            Place();
            _body.SetActive(true);
        }

        private void OnDisappeared()
        {
            _body.SetActive(false);
        }
    }
}
