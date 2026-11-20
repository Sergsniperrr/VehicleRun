namespace _Project.Core.Leaderboards
{
    public class LeaderboardRow
    {
        public LeaderboardRow(int rank, long score, string playerName, string avatarUrl, bool isCurrentPlayer)
        {
            Rank = rank;
            Score = score;
            PlayerName = playerName ?? string.Empty;
            AvatarUrl = avatarUrl ?? string.Empty;
            IsCurrentPlayer = isCurrentPlayer;
        }

        public int Rank { get; }
        public long Score { get; }
        public string PlayerName { get; }
        public string AvatarUrl { get; }
        public bool IsCurrentPlayer { get; }
    }
}
