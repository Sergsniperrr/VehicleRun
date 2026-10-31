using System;
using _Project.Core.Vehicles;

namespace _Project.Features.Boxes
{
    public class Box
    {
        public Box(long instanceId, BoxSource source, VehicleDefinition content)
        {
            InstanceId = instanceId;
            Source = source;
            Content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public long InstanceId { get; }
        public BoxSource Source { get; }
        public VehicleDefinition Content { get; }
        public int Rank => Content.Rank;
    }
}
