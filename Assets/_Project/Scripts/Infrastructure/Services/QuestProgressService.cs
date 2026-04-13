using Naninovel;
using OnlyFarms.Utilities;

namespace OnlyFarms.Infrastructure.Services
{
    [InitializeAtRuntime]
    public class QuestProgressService : IEngineService, IQuestProgressReporter
    {
        public UniTask InitializeService()
        {
            OFLogger.Log("<color=blue>Initialize...</color>");
            return UniTask.CompletedTask;
        }

        public void ResetService()
        {
            OFLogger.Log("<color=yellow>Reset called...</color>");
        }

        public void DestroyService()
        {
            OFLogger.Log("<color=red>DestroyService called...</color>");
        }

        public void ReportEvent(string eventId)
        {
            OFLogger.Log($"Quest event: {eventId} reported...");
        }
    }
}