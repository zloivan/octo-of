using System;

namespace Locations.Domain
{
    [Serializable]
    public class HotspotData
    {
        public string Id;
        public HotspotType Type;
        public ActivationCondition Condition;
        public string ConditionValue;
        public string TargetLocationId;
        
        public override string ToString() =>
            $"HotspotData(Id={Id}, Type={Type}, Condition={Condition}, ConditionValue={ConditionValue}, TargetLocationId={TargetLocationId})";
    }
}