using System.Collections.Generic;
using System.Threading;
using _Project.Core.Leaderboards;
using Cysharp.Threading.Tasks;
using JTLStudio.SDK;

namespace _Project.Infrastructure.JTL
{
    public class JTLLeaderboardService : ILeaderboardService
    {
        private const string TotalCoinsLeaderboardId = "total_coins";
        private const int TopCount = 10;
        private const int AroundCount = 3;

        public bool CanLoad => JTLSDK.Leaderboards.CanLoad;

        public void SubmitScore(long score)
        {
            JTLSDK.Leaderboards.SetScore(TotalCoinsLeaderboardId, score);
        }

        public UniTask<LeaderboardTable> LoadAsync(CancellationToken cancellationToken)
        {
            UniTaskCompletionSource<LeaderboardTable> completion = new UniTaskCompletionSource<LeaderboardTable>();
            JTLSDK.Leaderboards.Load(TotalCoinsLeaderboardId, TopCount, AroundCount, page => completion.TrySetResult(CreateTable(page)));

            return completion.Task.AttachExternalCancellation(cancellationToken);
        }

        private LeaderboardTable CreateTable(LeaderboardPage page)
        {
            List<LeaderboardRow> rows = new List<LeaderboardRow>();
            HashSet<int> ranks = new HashSet<int>();

            foreach (LeaderboardEntry entry in page.Entries)
            {
                if (ranks.Add(entry.Rank))
                {
                    rows.Add(CreateRow(entry));
                }
            }

            if (page.CurrentPlayer.HasValue && ranks.Add(page.CurrentPlayer.Value.Rank))
            {
                rows.Add(CreateRow(page.CurrentPlayer.Value));
            }

            rows.Sort(CompareByRank);

            return new LeaderboardTable(rows);
        }

        private LeaderboardRow CreateRow(LeaderboardEntry entry)
        {
            return new LeaderboardRow(entry.Rank, entry.Score, entry.PlayerName, entry.IsCurrentPlayer);
        }

        private int CompareByRank(LeaderboardRow first, LeaderboardRow second)
        {
            return first.Rank.CompareTo(second.Rank);
        }
    }
}
