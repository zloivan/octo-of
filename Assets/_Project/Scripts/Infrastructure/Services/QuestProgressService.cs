using Naninovel;
using OnlyFarms.Utilities;

namespace OnlyFarms.Infrastructure.Services
{
    [InitializeAtRuntime]
    public class QuestProgressService : IEngineService, IQuestProgressReporter
    {
        public UniTask InitializeService() =>
            UniTask.CompletedTask;

        public void ResetService()
        {
            OFLogger.Log("Reset called...");
        }

        public void DestroyService()
        {
            OFLogger.Log("DestroyService called...");
        }

        public void ReportEvent(string eventId)
        {
            OFLogger.Log($"Quest event: {eventId} reported...");
        }
    }
}