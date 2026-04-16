using System;

namespace OnlyFarms.Domain.Quests
{
    [Serializable]
    public class QuestObjectiveProgress
    {
        public int QuestIndex;
        public int ObjectiveIndex;
        public int CurrentCount;
    }
}