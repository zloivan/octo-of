using System;

namespace OnlyFarms.Domain
{
    [Serializable]
    public class QuestObjectiveProgress
    {
        public int QuestIndex;
        public int ObjectiveIndex;
        public int CurrentCount;
    }
}