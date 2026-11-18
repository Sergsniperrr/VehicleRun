namespace _Project.Core.Leaderboards
{
    public class LeaderboardRow
    {
        public LeaderboardRow(int rank, long score, string playerName, bool isCurrentPlayer)
        {
            Rank = rank;
            Score = score;
            PlayerName = playerName ?? string.Empty;
            IsCurrentPlayer = isCurrentPlayer;
        }

        public int Rank { get; }
        public long Score { get; }
        public string PlayerName { get; }
        public bool IsCurrentPlayer { get; }
    }
}
