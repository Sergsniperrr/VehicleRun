using System;
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
        private const int TopCount = 3;
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

            return new LeaderboardTable(SelectVisibleRows(rows));
        }

        private List<LeaderboardRow> SelectVisibleRows(List<LeaderboardRow> rows)
        {
            int currentIndex = rows.FindIndex(row => row.IsCurrentPlayer);
            List<LeaderboardRow> visibleRows = new List<LeaderboardRow>();

            foreach (LeaderboardRow row in rows)
            {
                bool isTop = row.Rank <= TopCount;
                bool isNearPlayer = currentIndex >= 0 && Math.Abs(row.Rank - rows[currentIndex].Rank) <= AroundCount;

                if (isTop || isNearPlayer)
                {
                    visibleRows.Add(row);
                }
            }

            return visibleRows;
        }

        private LeaderboardRow CreateRow(LeaderboardEntry entry)
        {
            return new LeaderboardRow(entry.Rank, entry.Score, entry.PlayerName, entry.AvatarUrl, entry.IsCurrentPlayer);
        }

        private int CompareByRank(LeaderboardRow first, LeaderboardRow second)
        {
            return first.Rank.CompareTo(second.Rank);
        }
    }
}
