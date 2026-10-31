using _Project.Core.Economy;
using _Project.Core.Garage;
using _Project.Core.Track;
using _Project.Core.Vehicles;
using UnityEngine;

namespace _Project.Core.Configs
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "_Project/Game/Game Config", order = 0)]
    public class GameConfig : ScriptableObject
    {
        [SerializeField] private int _version;
        [SerializeField] private VehicleCatalogConfig _vehicles;
        [SerializeField] private EconomyConfig _economy;
        [SerializeField] private GarageConfig _garage;
        [SerializeField] private TrackConfig _track;

        public int Version => _version;
        public VehicleCatalogConfig Vehicles => _vehicles;
        public EconomyConfig Economy => _economy;
        public GarageConfig Garage => _garage;
        public TrackConfig Track => _track;
    }
}
