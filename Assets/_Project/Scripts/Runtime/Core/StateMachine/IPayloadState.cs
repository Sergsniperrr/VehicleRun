using Cysharp.Threading.Tasks;

namespace _Project.Core.StateMachine
{
    public interface IPayloadState<in TPayload> : IExitableState
    {
        UniTask Enter(TPayload payload);
    }
}
