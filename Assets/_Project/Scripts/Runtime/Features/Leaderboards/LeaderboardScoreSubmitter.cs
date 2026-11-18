using System;
using _Project.Core.Leaderboards;
using _Project.Core.Time;
using _Project.Features.Economy;
using VContainer.Unity;

namespace _Project.Features.Leaderboards
{
    public class LeaderboardScoreSubmitter : ITickable
    {
        private const float SubmitIntervalSeconds = 30f;
        private const long NoSubmittedScore = -1;

        private readonly ILeaderboardService _leaderboard;
        private readonly Wallet _wallet;
        private readonly ITimeService _time;

        private long _submittedScore = NoSubmittedScore;
        private float _secondsSinceSubmit;

        public LeaderboardScoreSubmitter(ILeaderboardService leaderboard, Wallet wallet, ITimeService time)
        {
            _leaderboard = leaderboard ?? throw new ArgumentNullException(nameof(leaderboard));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _time = time ?? throw new ArgumentNullException(nameof(time));
        }

        public void Tick()
        {
            _secondsSinceSubmit += _time.DeltaTime;

            if (_secondsSinceSubmit >= SubmitIntervalSeconds)
            {
                Submit();
            }
        }

        public void Submit()
        {
            _secondsSinceSubmit = 0;
            long score = _wallet.TotalEarned;

            if (score == _submittedScore)
            {
                return;
            }

            _submittedScore = score;
            _leaderboard.SubmitScore(score);
        }
    }
}
