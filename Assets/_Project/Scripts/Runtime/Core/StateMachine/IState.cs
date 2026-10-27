using Cysharp.Threading.Tasks;

namespace _Project.Core.StateMachine
{
    public interface IState : IExitableState
    {
        UniTask Enter();
    }
}
