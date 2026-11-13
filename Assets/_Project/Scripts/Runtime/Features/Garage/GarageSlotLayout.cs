using System;
using UnityEngine;

namespace _Project.Features.Garage
{
    public class GarageSlotLayout
    {
        private const float HalfShare = 0.5f;
        private const float ScaleTolerance = 0.0001f;

        private readonly Vector2 _areaCenter;
        private readonly Vector2 _areaSize;
        private readonly Vector2 _baseCellSize;
        private readonly Vector2 _slotCenterOffset;
        private readonly float _maxScale;
        private readonly int _maxColumns;

        public GarageSlotLayout(
            Vector2 areaCenter,
            Vector2 areaSize,
            Vector2 baseCellSize,
            Vector2 slotCenterOffset,
            float maxScale,
            int maxColumns)
        {
            if (areaSize.x <= 0 || areaSize.y <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(areaSize));
            }

            if (baseCellSize.x <= 0 || baseCellSize.y <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(baseCellSize));
            }

            if (maxScale <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxScale));
            }

            if (maxColumns < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxColumns));
            }

            _areaCenter = areaCenter;
            _areaSize = areaSize;
            _baseCellSize = baseCellSize;
            _slotCenterOffset = slotCenterOffset;
            _maxScale = maxScale;
            _maxColumns = maxColumns;
        }

        public int CalculateColumnCount(int slotCount)
        {
            if (slotCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(slotCount));
            }

            int bestColumnCount = 1;
            float bestScale = 0f;

            for (int columnCount = 1; columnCount <= _maxColumns; columnCount++)
            {
                float scale = CalculateScale(slotCount, columnCount);

                if (scale >= bestScale - ScaleTolerance)
                {
                    bestScale = scale;
                    bestColumnCount = columnCount;
                }
            }

            return bestColumnCount;
        }

        public float CalculateScale(int slotCount)
        {
            return CalculateScale(slotCount, CalculateColumnCount(slotCount));
        }

        public GarageSlotPlacement Calculate(int slot, int slotCount)
        {
            if (slot < 0 || slot >= slotCount)
            {
                throw new ArgumentOutOfRangeException(nameof(slot));
            }

            int columnCount = CalculateColumnCount(slotCount);
            int rowCount = CountRows(slotCount, columnCount);
            int row = slot / columnCount;
            int column = slot % columnCount;
            int slotsInRow = Math.Min(columnCount, slotCount - row * columnCount);
            float scale = CalculateScale(slotCount, columnCount);
            float cellWidth = _areaSize.x / columnCount;
            float cellHeight = _areaSize.y / rowCount;
            float columnShift = column - (slotsInRow - 1) * HalfShare;
            float rowShift = (rowCount - 1) * HalfShare - row;
            Vector2 cellCenter = _areaCenter + new Vector2(columnShift * cellWidth, rowShift * cellHeight);

            return new GarageSlotPlacement(cellCenter - _slotCenterOffset * scale, scale);
        }

        private float CalculateScale(int slotCount, int columnCount)
        {
            int rowCount = CountRows(slotCount, columnCount);
            float widthScale = _areaSize.x / columnCount / _baseCellSize.x;
            float heightScale = _areaSize.y / rowCount / _baseCellSize.y;

            return Math.Min(_maxScale, Math.Min(widthScale, heightScale));
        }

        private int CountRows(int slotCount, int columnCount)
        {
            return (slotCount + columnCount - 1) / columnCount;
        }
    }
}
