using System;

namespace _Project.Features.Garage
{
    public interface IFieldInputBlocker
    {
        event Action BlockingStarted;

        bool IsBlocking { get; }
    }
}
