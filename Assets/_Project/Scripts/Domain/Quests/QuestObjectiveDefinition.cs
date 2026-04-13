using System;

namespace OnlyFarms.Domain
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