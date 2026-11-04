using System;
using _Project.Core.Time;
using _Project.Features.Boxes;
using _Project.Features.Track;
using VContainer.Unity;

namespace _Project.Features.Economy
{
    public class EconomyTicker : ITickable
    {
        private readonly ITimeService _time;
        private readonly TrackRoster _roster;
        private readonly BoxDelivery _delivery;

        public EconomyTicker(ITimeService time, TrackRoster roster, BoxDelivery delivery)
        {
            _time = time ?? throw new ArgumentNullException(nameof(time));
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
            _delivery = delivery ?? throw new ArgumentNullException(nameof(delivery));
        }

        public void Tick()
        {
            float deltaTime = _time.DeltaTime;
            _roster.Advance(deltaTime);
            _delivery.Advance(deltaTime);
        }
    }
}
