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

        public LoadRunState(IGameStateMachine stateMachine, ISceneLoader sceneLoader)
        {
            _stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
            _sceneLoader = sceneLoader ?? throw new ArgumentNullException(nameof(sceneLoader));
        }

        public async UniTask Enter()
        {
            await _sceneLoader.LoadAsync(SceneId.Game);

            _stateMachine.Enter<RunState>();
        }

        public UniTask Exit()
        {
            return UniTask.CompletedTask;
        }
    }
}
