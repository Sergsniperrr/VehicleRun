using _Project.UI.Windows;
using NUnit.Framework;

namespace _Project.Tests.EditMode.Windows
{
    public class WindowQueueTests
    {
        private WindowQueue _queue;
        private FakeWindow _first;
        private FakeWindow _second;
        private FakeWindow _third;

        [SetUp]
        public void SetUp()
        {
            _queue = new WindowQueue();
            _first = new FakeWindow(false);
            _second = new FakeWindow(false);
            _third = new FakeWindow(true);
        }

        [Test]
        public void FirstWindowOpensAtOnce()
        {
            _queue.Open(_first);

            Assert.That(_queue.Current, Is.SameAs(_first));
            Assert.That(_first.IsShown, Is.True);
            Assert.That(_queue.IsBlocking, Is.True);
        }

        [Test]
        public void SecondWindowWaitsForFirst()
        {
            _queue.Open(_first);
            _queue.Open(_second);

            Assert.That(_queue.Current, Is.SameAs(_first));
            Assert.That(_second.IsShown, Is.False);
            Assert.That(_queue.PendingCount, Is.EqualTo(1));
        }

        [Test]
        public void ClosingCurrentOpensNextInOrder()
        {
            _queue.Open(_first);
            _queue.Open(_second);
            _queue.Open(_third);

            _queue.Close(_first);

            Assert.That(_first.IsShown, Is.False);
            Assert.That(_queue.Current, Is.SameAs(_second));
            Assert.That(_second.IsShown, Is.True);
            Assert.That(_third.IsShown, Is.False);

            _queue.Close(_second);

            Assert.That(_queue.Current, Is.SameAs(_third));
        }

        [Test]
        public void ClosingPendingWindowRemovesItUnshown()
        {
            _queue.Open(_first);
            _queue.Open(_second);

            _queue.Close(_second);
            _queue.Close(_first);

            Assert.That(_second.ShowCount, Is.EqualTo(0));
            Assert.That(_queue.Current, Is.Null);
            Assert.That(_queue.IsBlocking, Is.False);
        }

        [Test]
        public void OpeningSameWindowTwiceKeepsOneEntry()
        {
            _queue.Open(_first);
            _queue.Open(_first);
            _queue.Open(_second);
            _queue.Open(_second);

            Assert.That(_first.ShowCount, Is.EqualTo(1));
            Assert.That(_queue.PendingCount, Is.EqualTo(1));
        }

        [Test]
        public void DismissReachesOnlyTopWindow()
        {
            _queue.Open(_first);
            _queue.Open(_second);

            _queue.DismissCurrent();

            Assert.That(_first.DismissCount, Is.EqualTo(1));
            Assert.That(_second.DismissCount, Is.EqualTo(0));
        }

        [Test]
        public void DismissWithoutWindowsDoesNothing()
        {
            Assert.DoesNotThrow(() => _queue.DismissCurrent());
            Assert.That(_queue.IsBlocking, Is.False);
        }

        [Test]
        public void BlockingStartsOncePerSeries()
        {
            int startedCount = 0;
            _queue.BlockingStarted += () => startedCount++;

            _queue.Open(_first);
            _queue.Open(_second);
            _queue.Close(_first);
            _queue.Close(_second);
            _queue.Open(_third);

            Assert.That(startedCount, Is.EqualTo(2));
        }

        [Test]
        public void BlockingEndsAfterLastWindow()
        {
            _queue.Open(_first);
            _queue.Open(_second);

            _queue.Close(_first);

            Assert.That(_queue.IsBlocking, Is.True);

            _queue.Close(_second);

            Assert.That(_queue.IsBlocking, Is.False);
        }

        [Test]
        public void ClosingUnknownWindowChangesNothing()
        {
            _queue.Open(_first);

            _queue.Close(_second);

            Assert.That(_queue.Current, Is.SameAs(_first));
            Assert.That(_first.IsShown, Is.True);
        }
    }
}
