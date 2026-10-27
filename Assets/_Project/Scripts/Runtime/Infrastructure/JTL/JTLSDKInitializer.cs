using _Project.Core.Platform;
using Cysharp.Threading.Tasks;
using JTLStudio.SDK;

namespace _Project.Infrastructure.JTL
{
    public class JTLSDKInitializer : IPlatformInitializer
    {
        private UniTaskCompletionSource _initialization;

        public UniTask InitializeAsync()
        {
            if (_initialization != null)
            {
                return _initialization.Task;
            }

            _initialization = new UniTaskCompletionSource();

            if (JTLSDK.IsCreated == false)
            {
                JTLSDK.Create();
            }

            JTLSDK.WhenReady(() => _initialization.TrySetResult());

            return _initialization.Task;
        }
    }
}
