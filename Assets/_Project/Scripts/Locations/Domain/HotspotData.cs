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
    }
}