using Cysharp.Threading.Tasks;

namespace _Project.Core.StateMachine
{
    public interface IExitableState
    {
        UniTask Exit();
    }
}
