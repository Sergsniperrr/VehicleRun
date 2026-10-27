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
        public async UniTask LoadAsync(SceneId sceneId, Action loaded = null, CancellationToken cancellationToken = default)
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneId.ToString(), LoadSceneMode.Single);

            if (operation == null)
            {
                throw new InvalidOperationException(nameof(sceneId));
            }

            await operation.ToUniTask(cancellationToken: cancellationToken);

            loaded?.Invoke();
        }
    }
}
