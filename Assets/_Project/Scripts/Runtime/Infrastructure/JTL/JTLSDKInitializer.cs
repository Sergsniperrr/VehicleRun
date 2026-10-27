using System.Threading;
using _Project.Core.Platform;
using Cysharp.Threading.Tasks;
using JTLStudio.SDK;

namespace _Project.Infrastructure.JTL
{
    public class JTLSDKInitializer : IPlatformInitializer
    {
        private UniTaskCompletionSource _initialization;

        public UniTask InitializeAsync(CancellationToken cancellationToken = default)
        {
            if (_initialization == null)
            {
                _initialization = new UniTaskCompletionSource();
                CreateSdk();
            }

            return _initialization.Task.AttachExternalCancellation(cancellationToken);
        }

        private void CreateSdk()
        {
            if (JTLSDK.IsCreated == false)
            {
                JTLSDK.Create();
            }

            JTLSDK.WhenReady(OnSdkReady);
        }

        private void OnSdkReady()
        {
            _initialization.TrySetResult();
        }
    }
}
