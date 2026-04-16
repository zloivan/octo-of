namespace OnlyFarms.Domain.Hotspots
{
    public class HotspotData
    {
        public readonly string Id;
        public readonly HotspotType Type;
        public readonly ActivationCondition Condition;
        public readonly string ConditionValue;
        public readonly string TargetLocationId;
        public readonly string LocationId;
        public readonly string Label;
        public readonly QuestDefinition QuestDefinition;
        public readonly string RequiredObjectiveId;
        public readonly string ObjectiveEventId;

        public HotspotData(string id, HotspotType type, ActivationCondition condition, string conditionValue,
            string targetLocationId, string locationId, string label, QuestDefinition questDefinition,
            string requiredObjectiveId, string objectiveEventId)
        {
            Id = id;
            Type = type;
            Condition = condition;
            ConditionValue = conditionValue;
            TargetLocationId = targetLocationId;
            LocationId = locationId;
            Label = label;
            QuestDefinition = questDefinition;
            RequiredObjectiveId = requiredObjectiveId;
            ObjectiveEventId = objectiveEventId;
        }
    }
}