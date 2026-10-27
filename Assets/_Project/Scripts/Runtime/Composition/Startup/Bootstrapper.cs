using System;
using _Project.Core.StateMachine;
using VContainer.Unity;

namespace _Project.Composition.Startup
{
    public class Bootstrapper : IStartable
    {
        private readonly IGameStateMachine _stateMachine;

        public Bootstrapper(IGameStateMachine stateMachine)
        {
            _stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
        }

        public void Start()
        {
            _stateMachine.Enter<BootstrapState>();
        }
    }
}
