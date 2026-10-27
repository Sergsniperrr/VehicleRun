using _Project.Core.Platform;
using Cysharp.Threading.Tasks;
using PrimeGames.SDK;

namespace _Project.Infrastructure.Prime
{
    public class PrimeSDKInitializer : IPlatformInitializer
    {
        public UniTask InitializeAsync()
        {
            UniTaskCompletionSource source = new UniTaskCompletionSource();

            PrimeSDK.WaitForProviders(() => source.TrySetResult());

            return source.Task;
        }
    }
}
