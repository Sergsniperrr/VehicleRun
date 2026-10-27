namespace _Project.Core.Analytics.Events
{
    public class GameReadyEvent : IAnalyticsEvent
    {
        public void Report(IAnalyticsReporter reporter)
        {
            reporter.ReportGameReady();
        }
    }
}
