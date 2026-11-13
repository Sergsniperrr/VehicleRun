using System;
using _Project.Features.Garage;
using NUnit.Framework;
using UnityEngine;

namespace _Project.Tests.EditMode.Garage
{
    public class GarageSlotLayoutTests
    {
        private const float Tolerance = 0.001f;
        private const float MaxScale = 1.5f;
        private const int MaxColumns = 3;
        private const int MinSlotCount = 4;
        private const int MaxSlotCount = 15;

        private readonly Vector2 _areaCenter = new Vector2(0f, 0.95f);
        private readonly Vector2 _areaSize = new Vector2(6.3f, 10.25f);
        private readonly Vector2 _baseCellSize = new Vector2(2.1f, 2.05f);
        private readonly Vector2 _slotCenterOffset = new Vector2(0f, 0.45f);

        private GarageSlotLayout _layout;

        [SetUp]
        public void SetUp()
        {
            _layout = new GarageSlotLayout(_areaCenter, _areaSize, _baseCellSize, _slotCenterOffset, MaxScale, MaxColumns);
        }

        [TestCase(4, 2)]
        [TestCase(6, 2)]
        [TestCase(8, 2)]
        [TestCase(9, 3)]
        [TestCase(12, 3)]
        [TestCase(15, 3)]
        public void ColumnCountGrowsWithSlotCount(int slotCount, int columnCount)
        {
            Assert.That(_layout.CalculateColumnCount(slotCount), Is.EqualTo(columnCount));
        }

        [TestCase(4, 1.5f)]
        [TestCase(6, 1.5f)]
        [TestCase(8, 1.25f)]
        [TestCase(9, 1f)]
        [TestCase(15, 1f)]
        public void FewerSlotsAreBigger(int slotCount, float scale)
        {
            Assert.That(_layout.CalculateScale(slotCount), Is.EqualTo(scale).Within(Tolerance));
        }

        [Test]
        public void ScaleNeverGrowsWithSlotCount()
        {
            float previousScale = float.MaxValue;

            for (int slotCount = MinSlotCount; slotCount <= MaxSlotCount; slotCount++)
            {
                float scale = _layout.CalculateScale(slotCount);

                Assert.That(scale, Is.LessThanOrEqualTo(previousScale + Tolerance));
                previousScale = scale;
            }
        }

        [TestCase(0, -2.1f, 4.6f)]
        [TestCase(1, 0f, 4.6f)]
        [TestCase(2, 2.1f, 4.6f)]
        [TestCase(7, 0f, 0.5f)]
        [TestCase(14, 2.1f, -3.6f)]
        public void FullGarageKeepsOriginalGrid(int slot, float x, float y)
        {
            GarageSlotPlacement placement = _layout.Calculate(slot, MaxSlotCount);

            Assert.That(placement.Position.x, Is.EqualTo(x).Within(Tolerance));
            Assert.That(placement.Position.y, Is.EqualTo(y).Within(Tolerance));
            Assert.That(placement.Scale, Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void FourSlotsStandInTwoRowsAroundAreaCenter()
        {
            GarageSlotPlacement topLeft = _layout.Calculate(0, MinSlotCount);
            GarageSlotPlacement bottomRight = _layout.Calculate(3, MinSlotCount);
            Vector2 topLeftCenter = topLeft.Position + _slotCenterOffset * topLeft.Scale;
            Vector2 bottomRightCenter = bottomRight.Position + _slotCenterOffset * bottomRight.Scale;
            Vector2 middle = (topLeftCenter + bottomRightCenter) / 2f;

            Assert.That(middle.x, Is.EqualTo(_areaCenter.x).Within(Tolerance));
            Assert.That(middle.y, Is.EqualTo(_areaCenter.y).Within(Tolerance));
            Assert.That(topLeftCenter.x, Is.LessThan(bottomRightCenter.x));
            Assert.That(topLeftCenter.y, Is.GreaterThan(bottomRightCenter.y));
        }

        [Test]
        public void IncompleteLastRowIsCentered()
        {
            GarageSlotPlacement lastSlot = _layout.Calculate(4, 5);

            Assert.That(lastSlot.Position.x, Is.EqualTo(_areaCenter.x).Within(Tolerance));
        }

        [Test]
        public void SlotsStayInsideArea()
        {
            for (int slotCount = MinSlotCount; slotCount <= MaxSlotCount; slotCount++)
            {
                for (int slot = 0; slot < slotCount; slot++)
                {
                    GarageSlotPlacement placement = _layout.Calculate(slot, slotCount);
                    Vector2 center = placement.Position + _slotCenterOffset * placement.Scale;
                    Vector2 halfSize = _baseCellSize * placement.Scale / 2f;

                    Assert.That(center.x - halfSize.x, Is.GreaterThanOrEqualTo(_areaCenter.x - _areaSize.x / 2f - Tolerance));
                    Assert.That(center.x + halfSize.x, Is.LessThanOrEqualTo(_areaCenter.x + _areaSize.x / 2f + Tolerance));
                    Assert.That(center.y - halfSize.y, Is.GreaterThanOrEqualTo(_areaCenter.y - _areaSize.y / 2f - Tolerance));
                    Assert.That(center.y + halfSize.y, Is.LessThanOrEqualTo(_areaCenter.y + _areaSize.y / 2f + Tolerance));
                }
            }
        }

        [Test]
        public void SlotOutsideCountIsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _layout.Calculate(-1, MinSlotCount));
            Assert.Throws<ArgumentOutOfRangeException>(() => _layout.Calculate(MinSlotCount, MinSlotCount));
            Assert.Throws<ArgumentOutOfRangeException>(() => _layout.CalculateColumnCount(0));
        }
    }
}
