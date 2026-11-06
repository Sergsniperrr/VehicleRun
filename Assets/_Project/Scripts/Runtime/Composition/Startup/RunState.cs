using System;
using _Project.Core.Analytics;
using _Project.Core.Analytics.Events;
using _Project.Core.StateMachine;
using Cysharp.Threading.Tasks;

namespace _Project.Composition.Startup
{
    public class RunState : IState
    {
        private readonly IAnalyticsService _analytics;

        public RunState(IAnalyticsService analytics)
        {
            _analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));
        }

        public UniTask Enter()
        {
            _analytics.ReportEvent(new GameReadyEvent());
            _analytics.ReportEvent(new GameplayStartedEvent());

            return UniTask.CompletedTask;
        }

        public UniTask Exit()
        {
            _analytics.ReportEvent(new GameplayStoppedEvent());

            return UniTask.CompletedTask;
        }
    }
}
