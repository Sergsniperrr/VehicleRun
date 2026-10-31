using _Project.Core.Progression;
using UnityEngine;

namespace _Project.Core.Garage
{
    [CreateAssetMenu(fileName = "GarageConfig", menuName = "_Project/Game/Garage Config", order = 3)]
    public class GarageConfig : ScriptableObject
    {
        [SerializeField] private int _columns;
        [SerializeField] private int _rows;
        [SerializeField] private CapacitySchedule _openSlots = new CapacitySchedule();

        public int Columns => _columns;
        public int Rows => _rows;
        public int SlotCount => _columns * _rows;
        public CapacitySchedule OpenSlots => _openSlots;
    }
}
