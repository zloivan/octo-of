using System;
using Naninovel;
using OnlyFarms.Infrastructure.DataAccess;
using OnlyFarms.Utilities;

namespace OnlyFarms.Infrastructure.Services
{
    [InitializeAtRuntime]
    public class QuestService : IStatefulService<GameStateMap>, IQuestStatusSource
    {
        public event Func<UniTask> OnAllQuestsCompleted;

        public UniTask InitializeService()
        {
            OFLogger.Log("QuestService initialized");
            return UniTask.CompletedTask;
        }

        public void ResetService()
        {
            OFLogger.Log("QuestService reset");
        }

        public void DestroyService()
        {
            OFLogger.Log("QuestService destroyed");
        }

        public void SaveServiceState(GameStateMap stateMap)
        {
            OFLogger.Log("QuestService state saved");
        }

        public UniTask LoadServiceState(GameStateMap stateMap)
        {
            OFLogger.Log("QuestService state loaded");

            return UniTask.CompletedTask;
        }

        public bool IsQuestCompleted(string conditionValue)
        {
            //TODO: TEMP TEST
            OFLogger.Log($"Checking if quest is completed: {conditionValue}");
            if (conditionValue == "test_quest")
            {
                return true;
            }

            return false;
        }

        public void ForceComplete() =>
            OnAllQuestsCompleted?.Invoke().Forget();
    }
}