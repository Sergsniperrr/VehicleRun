namespace _Project.Core.Analytics.Events
{
    public class GameplayStoppedEvent : IAnalyticsEvent
    {
        public void Report(IAnalyticsReporter reporter)
        {
            reporter.ReportGameplayStop();
        }
    }
}
