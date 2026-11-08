using System;
using _Project.Core.Track;
using _Project.Features.Track;
using _Project.Features.Turbo;

namespace _Project.Features.Economy
{
    public class IncomeService
    {
        private readonly TrackRoster _roster;
        private readonly TrackConfig _track;
        private readonly TurboService _turbo;

        public IncomeService(TrackRoster roster, TrackConfig track, TurboService turbo)
        {
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
            _track = track ?? throw new ArgumentNullException(nameof(track));
            _turbo = turbo ?? throw new ArgumentNullException(nameof(turbo));
        }

        public double CalculateIncomePerSecond()
        {
            return CalculateIncomePerSecondWithoutTurbo() * _turbo.SpeedMultiplier;
        }

        public double CalculateIncomePerSecondWithoutTurbo()
        {
            double income = 0;

            foreach (TrackRunner runner in _roster.Runners)
            {
                income += runner.Vehicle.Definition.CalculateAverageIncome(_track.LapLength);
            }

            return income;
        }
    }
}
