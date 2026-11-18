using System;
using System.Threading;
using _Project.Core.SceneManagement;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Project.Infrastructure.SceneManagement
{
    public class SceneLoader : ISceneLoader
    {
        private const float LoadedSceneProgress = 0.9f;

        public async UniTask LoadAsync(SceneId sceneId, Action loaded = null, CancellationToken cancellationToken = default)
        {
            AsyncOperation operation = StartLoading(sceneId);

            await operation.ToUniTask(cancellationToken: cancellationToken);

            loaded?.Invoke();
        }

        public async UniTask LoadAsync(SceneId sceneId, IProgress<float> progress, CancellationToken cancellationToken = default)
        {
            if (progress == null)
            {
                throw new ArgumentNullException(nameof(progress));
            }

            AsyncOperation operation = StartLoading(sceneId);
            IProgress<float> sceneProgress = Progress.Create<float>(value => progress.Report(value / LoadedSceneProgress));

            await operation.ToUniTask(sceneProgress, cancellationToken: cancellationToken);
        }

        private AsyncOperation StartLoading(SceneId sceneId)
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneId.ToString(), LoadSceneMode.Single);

            if (operation == null)
            {
                throw new InvalidOperationException(nameof(sceneId));
            }

            return operation;
        }
    }
}
