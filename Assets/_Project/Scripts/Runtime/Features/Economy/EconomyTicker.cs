using System;
using _Project.Core.Time;
using _Project.Features.Boxes;
using _Project.Features.Track;
using _Project.Features.Turbo;
using VContainer.Unity;

namespace _Project.Features.Economy
{
    public class EconomyTicker : ITickable
    {
        private readonly ITimeService _time;
        private readonly TrackRoster _roster;
        private readonly TurboService _turbo;
        private readonly BoxDelivery _delivery;

        public EconomyTicker(ITimeService time, TrackRoster roster, TurboService turbo, BoxDelivery delivery)
        {
            _time = time ?? throw new ArgumentNullException(nameof(time));
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
            _turbo = turbo ?? throw new ArgumentNullException(nameof(turbo));
            _delivery = delivery ?? throw new ArgumentNullException(nameof(delivery));
        }

        public void Tick()
        {
            float deltaTime = _time.DeltaTime;

            _roster.Advance(deltaTime, _turbo);
            _turbo.Advance(deltaTime);
            _delivery.Advance(deltaTime);
        }
    }
}
