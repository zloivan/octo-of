using System;
using System.Collections.Generic;
using Naninovel;
using OnlyFarms.Domain;
using OnlyFarms.Domain.Quests;

namespace OnlyFarms.Infrastructure
{
    public interface IQuestStatusSource
    {
        bool IsQuestCompleted(string quest);
        IReadOnlyList<QuestInstance> GetVisibleQuests();

        event Action<QuestInstance> OnQuestAdded; 
        event Action<QuestInstance, QuestObjectiveInstance> OnQuestObjectiveTicked; 
        event Action<QuestInstance> OnQuestCompleted;
        bool IsObjectiveCompleted(string objId);
        event Func<UniTask> OnAllQuestsCompleted;
    }
}