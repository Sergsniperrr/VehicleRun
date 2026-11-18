using System;
using _Project.Core.Leaderboards;
using UnityEngine;
using VContainer;

namespace _Project.UI.Hud
{
    public class LeaderboardButtonView : MonoBehaviour
    {
        private ILeaderboardService _leaderboard;

        private void Start()
        {
            gameObject.SetActive(_leaderboard.CanLoad);
        }

        [Inject]
        public void Construct(ILeaderboardService leaderboard)
        {
            _leaderboard = leaderboard ?? throw new ArgumentNullException(nameof(leaderboard));
        }
    }
}
