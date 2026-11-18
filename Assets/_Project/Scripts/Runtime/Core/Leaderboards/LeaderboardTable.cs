using System;
using System.Collections.Generic;

namespace _Project.Core.Leaderboards
{
    public class LeaderboardTable
    {
        public LeaderboardTable(IReadOnlyList<LeaderboardRow> rows)
        {
            Rows = rows ?? throw new ArgumentNullException(nameof(rows));
        }

        public IReadOnlyList<LeaderboardRow> Rows { get; }
    }
}
