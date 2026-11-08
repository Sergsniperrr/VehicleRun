using System;
using _Project.Features.Vehicles;
using UnityEngine;
using UnityEngine.EventSystems;

namespace _Project.Features.Track
{
    public class TrackVehicleView : MonoBehaviour, IPointerDownHandler
    {
        private const float SpriteForwardAngle = 90f;

        [SerializeField] private SpriteRenderer _body;
        [SerializeField] private TrailRenderer _boostTrail;

        public event Action<TrackVehicleView> Tapped;

        public void Show(Vehicle vehicle)
        {
            if (vehicle == null)
            {
                throw new ArgumentNullException(nameof(vehicle));
            }

            _body.sprite = vehicle.Definition.TrackSprite;
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            ShowBoost(false);
            _boostTrail.Clear();
            gameObject.SetActive(false);
        }

        public void Place(Vector2 point, Vector2 direction)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - SpriteForwardAngle;
            transform.localPosition = point;
            transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        public void ShowBoost(bool isBoosted)
        {
            _boostTrail.emitting = isBoosted;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            Tapped?.Invoke(this);
        }
    }
}
