namespace _Project.Core.StateMachine
{
    public interface IStateFactory
    {
        TState Create<TState>() where TState : class, IExitableState;
    }
}
