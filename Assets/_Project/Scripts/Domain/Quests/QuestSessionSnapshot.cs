using System;

namespace OnlyFarms.Domain
{
    [Serializable]
    public class QuestSessionSnapshot
    {
        public string DayId;
        public QuestObjectiveProgress[] ObjectiveProgress;
        public int VisibleCount;
    }
}