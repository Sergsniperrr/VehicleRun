using System.Threading;
using Cysharp.Threading.Tasks;

namespace _Project.Core.Platform
{
    public interface IPlatformInitializer
    {
        UniTask InitializeAsync(CancellationToken cancellationToken = default);
    }
}
