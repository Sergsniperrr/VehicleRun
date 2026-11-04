using System;
using _Project.Core.Track;
using UnityEngine;

namespace _Project.Features.Track
{
    public class TrackPath
    {
        private const float TopAngle = Mathf.PI / 2f;
        private const float RightAngle = 0f;
        private const float LeftAngle = Mathf.PI;

        private readonly TrackConfig _config;

        public TrackPath(TrackConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public float Length => _config.LapLength;

        public Vector2 GetPoint(float distanceFromStart)
        {
            Locate(distanceFromStart, out Vector2 point, out Vector2 _);

            return point;
        }

        public Vector2 GetDirection(float distanceFromStart)
        {
            Locate(distanceFromStart, out Vector2 _, out Vector2 direction);

            return direction;
        }

        public float MeasureDistanceToCenterline(Vector2 point)
        {
            float halfStraight = _config.StraightHalfLength;
            float radius = _config.TurnRadius;

            if (point.y > halfStraight)
            {
                return Mathf.Abs(Vector2.Distance(point, new Vector2(0f, halfStraight)) - radius);
            }

            if (point.y < -halfStraight)
            {
                return Mathf.Abs(Vector2.Distance(point, new Vector2(0f, -halfStraight)) - radius);
            }

            return Mathf.Abs(Mathf.Abs(point.x) - radius);
        }

        private void Locate(float distanceFromStart, out Vector2 point, out Vector2 direction)
        {
            float halfStraight = _config.StraightHalfLength;
            float radius = _config.TurnRadius;
            float halfTurnLength = _config.TurnLength / 2f;
            Vector2 topCenter = new Vector2(0f, halfStraight);
            Vector2 bottomCenter = new Vector2(0f, -halfStraight);
            float distance = WrapDistance(_config.StartDistance + distanceFromStart);

            if (distance < halfTurnLength)
            {
                LocateOnTurn(topCenter, TopAngle, distance, out point, out direction);

                return;
            }

            distance -= halfTurnLength;

            if (distance < _config.StraightLength)
            {
                point = new Vector2(radius, halfStraight - distance);
                direction = Vector2.down;

                return;
            }

            distance -= _config.StraightLength;

            if (distance < _config.TurnLength)
            {
                LocateOnTurn(bottomCenter, RightAngle, distance, out point, out direction);

                return;
            }

            distance -= _config.TurnLength;

            if (distance < _config.StraightLength)
            {
                point = new Vector2(-radius, distance - halfStraight);
                direction = Vector2.up;

                return;
            }

            distance -= _config.StraightLength;
            LocateOnTurn(topCenter, LeftAngle, distance, out point, out direction);
        }

        private void LocateOnTurn(Vector2 center, float startAngle, float distance, out Vector2 point, out Vector2 direction)
        {
            float angle = startAngle - distance / _config.TurnRadius;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);
            point = center + new Vector2(cos, sin) * _config.TurnRadius;
            direction = new Vector2(sin, -cos);
        }

        private float WrapDistance(float distance)
        {
            float wrapped = distance % Length;

            if (wrapped < 0)
            {
                wrapped += Length;
            }

            return wrapped;
        }
    }
}
