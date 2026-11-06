using System.Collections.Generic;
using _Project.Core.Analytics;

namespace _Project.Tests.EditMode.Windows
{
    public class FakeAnalyticsService : IAnalyticsService
    {
        private readonly List<IAnalyticsEvent> _events = new List<IAnalyticsEvent>();

        public IReadOnlyList<IAnalyticsEvent> Events => _events;

        public void ReportEvent(IAnalyticsEvent analyticsEvent)
        {
            _events.Add(analyticsEvent);
        }
    }
}
