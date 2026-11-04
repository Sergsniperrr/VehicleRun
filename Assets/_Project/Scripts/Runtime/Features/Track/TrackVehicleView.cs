using System;
using _Project.Features.Vehicles;
using UnityEngine;

namespace _Project.Features.Track
{
    public class TrackVehicleView : MonoBehaviour
    {
        private const float SpriteForwardAngle = 90f;

        [SerializeField] private SpriteRenderer _body;

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
            gameObject.SetActive(false);
        }

        public void Place(Vector2 point, Vector2 direction)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - SpriteForwardAngle;
            transform.localPosition = point;
            transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
