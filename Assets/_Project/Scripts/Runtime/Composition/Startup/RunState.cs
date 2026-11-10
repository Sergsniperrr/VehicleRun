using System;
using _Project.Core.Analytics;
using _Project.Core.Analytics.Events;
using _Project.Core.Audio;
using _Project.Core.StateMachine;
using Cysharp.Threading.Tasks;

namespace _Project.Composition.Startup
{
    public class RunState : IState
    {
        private readonly IAnalyticsService _analytics;
        private readonly IAudioService _audio;
        private readonly AudioAsset _gameplayMusic;

        public RunState(IAnalyticsService analytics, IAudioService audio, AudioAsset gameplayMusic)
        {
            _analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));
            _gameplayMusic = gameplayMusic ?? throw new ArgumentNullException(nameof(gameplayMusic));
        }

        public UniTask Enter()
        {
            _analytics.ReportEvent(new GameReadyEvent());
            _analytics.ReportEvent(new GameplayStartedEvent());
            _audio.PlayMusic(_gameplayMusic);

            return UniTask.CompletedTask;
        }

        public UniTask Exit()
        {
            _analytics.ReportEvent(new GameplayStoppedEvent());

            return UniTask.CompletedTask;
        }
    }
}
