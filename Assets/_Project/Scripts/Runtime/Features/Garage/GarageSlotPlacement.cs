using UnityEngine;

namespace _Project.Features.Garage
{
    public readonly struct GarageSlotPlacement
    {
        public GarageSlotPlacement(Vector2 position, float scale)
        {
            Position = position;
            Scale = scale;
        }

        public Vector2 Position { get; }
        public float Scale { get; }
    }
}
