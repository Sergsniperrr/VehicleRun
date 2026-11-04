using System.Threading;
using _Project.Features.Boxes;
using _Project.Features.Vehicles;
using Cysharp.Threading.Tasks;

namespace _Project.Features.Garage
{
    public interface IRecycleConfirmation
    {
        UniTask<bool> ConfirmVehicleAsync(Vehicle vehicle, CancellationToken cancellationToken);

        UniTask<bool> ConfirmBoxAsync(Box box, CancellationToken cancellationToken);
    }
}
