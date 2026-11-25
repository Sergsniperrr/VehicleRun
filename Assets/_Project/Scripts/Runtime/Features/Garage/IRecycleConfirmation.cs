using System;
using System.Threading;
using _Project.Features.Boxes;
using _Project.Features.Vehicles;
using Cysharp.Threading.Tasks;

namespace _Project.Features.Garage
{
    public interface IRecycleConfirmation
    {
        event Action PromptOpened;
        event Action<bool, bool> PromptAnswered;

        UniTask<bool> ConfirmVehicleAsync(Vehicle vehicle, CancellationToken cancellationToken);

        UniTask<bool> ConfirmBoxAsync(Box box, CancellationToken cancellationToken);
    }
}
