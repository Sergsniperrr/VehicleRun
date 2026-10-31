using System;
using _Project.Core.Vehicles;

namespace _Project.Features.Vehicles
{
    public class Vehicle
    {
        public Vehicle(long instanceId, VehicleDefinition definition)
        {
            InstanceId = instanceId;
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }

        public long InstanceId { get; }
        public VehicleDefinition Definition { get; }
        public int Rank => Definition.Rank;
    }
}
