namespace OnlyFarms.Locations.Domain
{
    public class HotspotData
    {
        public readonly string Id;
        public readonly HotspotType Type;
        public readonly ActivationCondition Condition;
        public readonly string ConditionValue;
        public readonly string TargetLocationId;
        public readonly string LocationId;

        public HotspotData(string id, HotspotType type, ActivationCondition condition, string conditionValue,
            string targetLocationId, string locationId)
        {
            Id = id;
            Type = type;
            Condition = condition;
            ConditionValue = conditionValue;
            TargetLocationId = targetLocationId;
            LocationId = locationId;
        }

        public override string ToString() =>
            $"HotspotData(Id={Id}, Type={Type}, Condition={Condition}, ConditionValue={ConditionValue}, TargetLocationId={TargetLocationId})";
    }
}