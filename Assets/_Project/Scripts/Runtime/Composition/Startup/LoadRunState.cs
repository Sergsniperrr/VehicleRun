using System;
using _Project.Core.SceneManagement;
using _Project.Core.StateMachine;
using Cysharp.Threading.Tasks;

namespace _Project.Composition.Startup
{
    public class LoadRunState : IState
    {
        private readonly IGameStateMachine _stateMachine;
        private readonly ISceneLoader _sceneLoader;
        private readonly LoadingProgress _loadingProgress;

        public LoadRunState(IGameStateMachine stateMachine, ISceneLoader sceneLoader, LoadingProgress loadingProgress)
        {
            _stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
            _sceneLoader = sceneLoader ?? throw new ArgumentNullException(nameof(sceneLoader));
            _loadingProgress = loadingProgress ?? throw new ArgumentNullException(nameof(loadingProgress));
        }

        public async UniTask Enter()
        {
            _loadingProgress.Report(0f);

            await _sceneLoader.LoadAsync(SceneId.Loading);
            await _sceneLoader.LoadAsync(SceneId.Game, _loadingProgress);

            _stateMachine.Enter<RunState>();
        }

        public UniTask Exit()
        {
            return UniTask.CompletedTask;
        }
    }
}
