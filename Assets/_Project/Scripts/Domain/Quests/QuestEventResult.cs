namespace OnlyFarms.Domain
{
    public readonly struct QuestEventResult
    {
        public static readonly QuestEventResult None = default;

        public readonly QuestInstance Quest;
        public readonly QuestObjectiveInstance Objective;
        public bool IsEmpty => Quest == null;
        
        public QuestEventResult(QuestInstance quest, QuestObjectiveInstance objective)
        {
            Quest = quest;
            Objective = objective;
        }
    }
}