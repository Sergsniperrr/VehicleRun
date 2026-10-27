namespace _Project.Core.Analytics.Events
{
    public class GameplayStartedEvent : IAnalyticsEvent
    {
        public void Report(IAnalyticsReporter reporter)
        {
            reporter.ReportGameplayStart();
        }
    }
}
