using System;
using UnityEngine.AddressableAssets;

namespace OnlyFarms.Locations
{
    [Serializable]
    public class HotspotEntry
    {
        public string Id;//TODO: String identifier not good
        public string LocationKey;//TODO: String identifier not good
        public AssetReferenceSprite SpriteRef;
        public HotspotType Type;
        public ActivationCondition Condition;
        public string ConditionValue;//TODO: String identifier not good
        public AssetReference ItemConfig;
    }
}