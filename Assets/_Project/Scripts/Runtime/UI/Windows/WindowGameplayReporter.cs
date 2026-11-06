using System;
using _Project.Core.Analytics;
using _Project.Core.Analytics.Events;
using VContainer.Unity;

namespace _Project.UI.Windows
{
    public class WindowGameplayReporter : IStartable, IDisposable
    {
        private readonly WindowQueue _windows;
        private readonly IAnalyticsService _analytics;
        private bool _isGameplayStopped;

        public WindowGameplayReporter(WindowQueue windows, IAnalyticsService analytics)
        {
            _windows = windows ?? throw new ArgumentNullException(nameof(windows));
            _analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));
        }

        public void Start()
        {
            _windows.CurrentChanged += OnCurrentChanged;
        }

        public void Dispose()
        {
            _windows.CurrentChanged -= OnCurrentChanged;
        }

        private void OnCurrentChanged()
        {
            bool isFullscreenOpen = _windows.IsBlocking && _windows.Current.IsFullscreen;

            if (isFullscreenOpen == _isGameplayStopped)
            {
                return;
            }

            _isGameplayStopped = isFullscreenOpen;

            if (isFullscreenOpen)
            {
                _analytics.ReportEvent(new GameplayStoppedEvent());
            }
            else
            {
                _analytics.ReportEvent(new GameplayStartedEvent());
            }
        }
    }
}
