using System;
using UnityEngine;

namespace _Project.Core.Vehicles
{
    [CreateAssetMenu(fileName = "VehicleCatalogConfig", menuName = "_Project/Game/Vehicle Catalog Config", order = 1)]
    public class VehicleCatalogConfig : ScriptableObject
    {
        [SerializeField] private VehicleDefinition[] _vehicles = Array.Empty<VehicleDefinition>();

        public int Count => _vehicles.Length;

        public VehicleDefinition GetAt(int index)
        {
            return _vehicles[index];
        }
    }
}
