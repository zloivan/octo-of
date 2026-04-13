namespace OnlyFarms.Infrastructure
{
    public interface IQuestProgressReporter
    {
        void ReportEvent(string eventId);
    }
}