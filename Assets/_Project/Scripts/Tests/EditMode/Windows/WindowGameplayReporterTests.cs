using _Project.Core.Analytics.Events;
using _Project.UI.Windows;
using NUnit.Framework;

namespace _Project.Tests.EditMode.Windows
{
    public class WindowGameplayReporterTests
    {
        private WindowQueue _queue;
        private FakeAnalyticsService _analytics;
        private WindowGameplayReporter _reporter;

        [SetUp]
        public void SetUp()
        {
            _queue = new WindowQueue();
            _analytics = new FakeAnalyticsService();
            _reporter = new WindowGameplayReporter(_queue, _analytics);
            _reporter.Start();
        }

        [TearDown]
        public void TearDown()
        {
            _reporter.Dispose();
        }

        [Test]
        public void FullscreenWindowStopsAndClosingStartsGameplay()
        {
            FakeWindow catalog = new FakeWindow(true);

            _queue.Open(catalog);

            Assert.That(_analytics.Events.Count, Is.EqualTo(1));
            Assert.That(_analytics.Events[0], Is.TypeOf<GameplayStoppedEvent>());

            _queue.Close(catalog);

            Assert.That(_analytics.Events.Count, Is.EqualTo(2));
            Assert.That(_analytics.Events[1], Is.TypeOf<GameplayStartedEvent>());
        }

        [Test]
        public void FullscreenWindowsInRowStopGameplayOnce()
        {
            FakeWindow catalog = new FakeWindow(true);
            FakeWindow shop = new FakeWindow(true);

            _queue.Open(catalog);
            _queue.Open(shop);
            _queue.Close(catalog);
            _queue.Close(shop);

            Assert.That(_analytics.Events.Count, Is.EqualTo(2));
            Assert.That(_analytics.Events[0], Is.TypeOf<GameplayStoppedEvent>());
            Assert.That(_analytics.Events[1], Is.TypeOf<GameplayStartedEvent>());
        }

        [Test]
        public void SmallWindowKeepsGameplay()
        {
            FakeWindow card = new FakeWindow(false);

            _queue.Open(card);
            _queue.Close(card);

            Assert.That(_analytics.Events, Is.Empty);
        }

        [Test]
        public void GameplayStartsWhenSmallWindowFollowsFullscreen()
        {
            FakeWindow catalog = new FakeWindow(true);
            FakeWindow card = new FakeWindow(false);

            _queue.Open(catalog);
            _queue.Open(card);
            _queue.Close(catalog);

            Assert.That(_analytics.Events.Count, Is.EqualTo(2));
            Assert.That(_analytics.Events[1], Is.TypeOf<GameplayStartedEvent>());
        }

        [Test]
        public void DisposedReporterSendsNothing()
        {
            _reporter.Dispose();

            _queue.Open(new FakeWindow(true));

            Assert.That(_analytics.Events, Is.Empty);
        }
    }
}
