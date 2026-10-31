using System;
using UnityEngine;

namespace _Project.Core.Vehicles
{
    [CreateAssetMenu(fileName = "VehicleCatalogConfig", menuName = "_Project/Game/Vehicle Catalog Config", order = 1)]
    public class VehicleCatalogConfig : ScriptableObject
    {
        [SerializeField] private VehicleDefinition[] _vehicles = Array.Empty<VehicleDefinition>();

        public int Count => _vehicles.Length;
        public int MaxRank => _vehicles.Length;

        public VehicleDefinition GetAt(int index)
        {
            return _vehicles[index];
        }

        public VehicleDefinition GetByRank(int rank)
        {
            if (rank < 1 || rank > MaxRank)
            {
                throw new ArgumentOutOfRangeException(nameof(rank));
            }

            return _vehicles[rank - 1];
        }
    }
}
