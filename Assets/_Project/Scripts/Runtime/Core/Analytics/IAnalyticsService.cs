namespace _Project.Core.Analytics
{
    public interface IAnalyticsService
    {
        void ReportEvent(IAnalyticsEvent analyticsEvent);
    }
}
