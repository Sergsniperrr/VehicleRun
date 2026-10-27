namespace _Project.Core.Analytics
{
    public interface IAnalyticsEvent
    {
        void Report(IAnalyticsReporter reporter);
    }
}
