using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Core.Leaderboards
{
    public interface IAvatarLoader
    {
        UniTask<Sprite> LoadAsync(string url, CancellationToken cancellationToken);
    }
}
