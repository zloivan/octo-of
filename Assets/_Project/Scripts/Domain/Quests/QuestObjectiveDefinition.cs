using System;

namespace OnlyFarms.Domain.Quests
{
    [Serializable]
    public class QuestObjectiveDefinition
    {
        public QuestObjectiveType Type;
        public string DisplayText;
        public string EventId;
        public int RequiredCount;
    }
}