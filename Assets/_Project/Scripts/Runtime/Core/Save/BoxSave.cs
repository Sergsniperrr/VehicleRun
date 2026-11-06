using System;

namespace _Project.Core.Save
{
    [Serializable]
    public class BoxSave
    {
        public long InstanceId;
        public int Slot;
        public string VehicleId;
        public int Source;
    }
}
