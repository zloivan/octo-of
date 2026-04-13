using System;
using System.Collections.Generic;
using Naninovel;
using OnlyFarms.DataAccess;
using OnlyFarms.Domain;

namespace OnlyFarms.Infrastructure
{
    public interface IQuestStatusSource
    {
        bool IsQuestsCompleted(QuestDefinitionSO quest);
        IReadOnlyList<QuestInstance> GetVisibleQuests();

        event Action<QuestInstance> OnQuestAdded; 
        event Action<QuestInstance, QuestObjectiveInstance> OnQuestObjectiveTicked; 
        event Action<QuestInstance> OnQuestCompleted; 
        
        event Func<UniTask> OnAllQuestsCompleted;
    }
}