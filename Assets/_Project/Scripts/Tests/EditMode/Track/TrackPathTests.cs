using _Project.Features.Track;
using _Project.Tests.EditMode.Configs;
using NUnit.Framework;
using UnityEngine;

namespace _Project.Tests.EditMode.Track
{
    public class TrackPathTests
    {
        private const float Tolerance = 1e-3f;

        private GameConfigFixture _fixture;
        private TrackPath _path;

        private float HalfStraight => _fixture.Track.StraightHalfLength;
        private float Radius => _fixture.Track.TurnRadius;

        [SetUp]
        public void SetUp()
        {
            _fixture = new GameConfigFixture();
            _path = new TrackPath(_fixture.Track);
        }

        [TearDown]
        public void TearDown()
        {
            _fixture.Dispose();
        }

        [Test]
        public void LapIsTwoStraightsAndTwoTurns()
        {
            float expectedLength = HalfStraight * 4f + Mathf.PI * Radius * 2f;

            Assert.That(_path.Length, Is.EqualTo(expectedLength).Within(Tolerance));
        }

        [Test]
        public void StartIsAtTopRightTurn()
        {
            Vector2 start = _path.GetPoint(0);
            Vector2 topCenter = new Vector2(0f, HalfStraight);

            Assert.That(Vector2.Distance(start, topCenter), Is.EqualTo(Radius).Within(Tolerance));
            Assert.That(start.x, Is.GreaterThan(0));
            Assert.That(start.y, Is.GreaterThan(HalfStraight));
        }

        [Test]
        public void VehiclesDriveClockwise()
        {
            _fixture.Change(_fixture.Track, "_startDistance", property => property.floatValue = 0);
            float halfTurn = Mathf.PI * Radius / 2f;
            float straight = HalfStraight * 2f;

            AssertPoint(0, new Vector2(0f, HalfStraight + Radius), Vector2.right);
            AssertPoint(halfTurn + 1f, new Vector2(Radius, HalfStraight - 1f), Vector2.down);
            AssertPoint(halfTurn * 2f + straight, new Vector2(0f, -HalfStraight - Radius), Vector2.left);
            AssertPoint(halfTurn * 3f + straight + 1f, new Vector2(-Radius, 1f - HalfStraight), Vector2.up);
        }

        [Test]
        public void WholeLapComesBackToStart()
        {
            AssertSamePoint(_path.GetPoint(_path.Length), _path.GetPoint(0));
            AssertSamePoint(_path.GetPoint(-1f), _path.GetPoint(_path.Length - 1f));
        }

        [Test]
        public void DistanceToCenterlineIsZeroOnPath()
        {
            for (float distance = 0f; distance < _path.Length; distance += 1f)
            {
                Assert.That(_path.MeasureDistanceToCenterline(_path.GetPoint(distance)), Is.EqualTo(0f).Within(Tolerance));
            }
        }

        [Test]
        public void DistanceToCenterlineGrowsAwayFromPath()
        {
            const float offset = 0.5f;

            Assert.That(_path.MeasureDistanceToCenterline(Vector2.zero), Is.EqualTo(Radius).Within(Tolerance));
            Assert.That(_path.MeasureDistanceToCenterline(new Vector2(Radius + offset, 0f)), Is.EqualTo(offset).Within(Tolerance));
            Assert.That(_path.MeasureDistanceToCenterline(new Vector2(offset - Radius, 1f)), Is.EqualTo(offset).Within(Tolerance));
            Assert.That(_path.MeasureDistanceToCenterline(new Vector2(0f, HalfStraight + Radius + offset)), Is.EqualTo(offset).Within(Tolerance));
            Assert.That(_path.MeasureDistanceToCenterline(new Vector2(0f, -HalfStraight - Radius + offset)), Is.EqualTo(offset).Within(Tolerance));
        }

        private void AssertPoint(float distance, Vector2 expectedPoint, Vector2 expectedDirection)
        {
            AssertSamePoint(_path.GetPoint(distance), expectedPoint);
            AssertSamePoint(_path.GetDirection(distance), expectedDirection);
        }

        private void AssertSamePoint(Vector2 actual, Vector2 expected)
        {
            Assert.That(Vector2.Distance(actual, expected), Is.LessThan(Tolerance), $"{actual} вместо {expected}");
        }
    }
}
