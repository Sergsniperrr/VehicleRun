using System;
using Cysharp.Threading.Tasks;

namespace _Project.Core.StateMachine
{
    public class GameStateMachine : IGameStateMachine
    {
        private readonly IStateFactory _stateFactory;

        private IExitableState _activeState;

        public GameStateMachine(IStateFactory stateFactory)
        {
            _stateFactory = stateFactory ?? throw new ArgumentNullException(nameof(stateFactory));
        }

        public void Enter<TState>() where TState : class, IState
        {
            ChangeState<TState>(state => state.Enter()).Forget();
        }

        public void Enter<TState, TPayload>(TPayload payload) where TState : class, IPayloadState<TPayload>
        {
            ChangeState<TState>(state => state.Enter(payload)).Forget();
        }

        private async UniTask ChangeState<TState>(Func<TState, UniTask> onEnter) where TState : class, IExitableState
        {
            if (_activeState != null)
            {
                await _activeState.Exit();
            }

            TState state = _stateFactory.Create<TState>();
            _activeState = state;

            await onEnter(state);
        }
    }
}
