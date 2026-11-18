using System.Threading;
using Cysharp.Threading.Tasks;

namespace _Project.Core.Leaderboards
{
    public interface ILeaderboardService
    {
        bool CanLoad { get; }

        void SubmitScore(long score);

        UniTask<LeaderboardTable> LoadAsync(CancellationToken cancellationToken);
    }
}
