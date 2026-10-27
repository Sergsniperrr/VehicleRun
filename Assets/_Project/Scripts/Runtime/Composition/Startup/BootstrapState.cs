using System;
using _Project.Core.Platform;
using _Project.Core.StateMachine;
using Cysharp.Threading.Tasks;

namespace _Project.Composition.Startup
{
    public class BootstrapState : IState
    {
        private readonly IGameStateMachine _stateMachine;
        private readonly IPlatformInitializer _platformInitializer;

        public BootstrapState(IGameStateMachine stateMachine, IPlatformInitializer platformInitializer)
        {
            _stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
            _platformInitializer = platformInitializer ?? throw new ArgumentNullException(nameof(platformInitializer));
        }

        public async UniTask Enter()
        {
            await _platformInitializer.InitializeAsync();

            _stateMachine.Enter<LoadRunState>();
        }

        public UniTask Exit()
        {
            return UniTask.CompletedTask;
        }
    }
}
