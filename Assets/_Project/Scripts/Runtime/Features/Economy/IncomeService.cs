using System;
using _Project.Core.Track;
using _Project.Features.Track;

namespace _Project.Features.Economy
{
    public class IncomeService
    {
        private readonly TrackRoster _roster;
        private readonly TrackConfig _track;

        public IncomeService(TrackRoster roster, TrackConfig track)
        {
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
            _track = track ?? throw new ArgumentNullException(nameof(track));
        }

        public double CalculateIncomePerSecond()
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
