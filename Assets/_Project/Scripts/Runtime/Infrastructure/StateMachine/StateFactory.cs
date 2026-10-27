using System;
using _Project.Core.StateMachine;
using VContainer;

namespace _Project.Infrastructure.StateMachine
{
    public class StateFactory : IStateFactory
    {
        private readonly IObjectResolver _resolver;

        public StateFactory(IObjectResolver resolver)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        public TState Create<TState>() where TState : class, IExitableState
        {
            return _resolver.Resolve<TState>();
        }
    }
}
