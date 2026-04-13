using System;
using System.Collections.Generic;
using Naninovel;
using OnlyFarms.DataAccess;
using OnlyFarms.Domain;
using OnlyFarms.Utilities;

namespace OnlyFarms.Infrastructure.Services
{
    [InitializeAtRuntime]
    public class QuestService : IStatefulService<GameStateMap>, IQuestStatusSource
    {
        public event Action<QuestInstance> OnQuestAdded;
        public event Action<QuestInstance, QuestObjectiveInstance> OnQuestObjectiveTicked;
        public event Action<QuestInstance> OnQuestCompleted;
        public event Func<UniTask> OnAllQuestsCompleted;

        private GameConfig _config;

        public QuestService(GameConfig config) =>
            _config = config;

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

        public bool IsQuestCompleted(string questId)
        {
            OFLogger.Log("Is Quest Completed Called...");
            
            return false;
        }

        public IReadOnlyList<QuestInstance> GetVisibleQuests()
        {
            OFLogger.Log("Get visible quests called...");
            return Array.Empty<QuestInstance>();
        }

        public void ActivateDaySession(DayConfigSO dayConfig)
        {
            OFLogger.Log("Activate Day session called...");
        }

        public void ForceComplete() =>
            OnAllQuestsCompleted?.Invoke().Forget();
    }
}