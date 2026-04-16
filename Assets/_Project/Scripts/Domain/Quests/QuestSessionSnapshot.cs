using System;

namespace OnlyFarms.Domain.Quests
{
    [Serializable]
    public class QuestSessionSnapshot
    {
        public QuestObjectiveProgress[] ObjectiveProgress;
    }
}