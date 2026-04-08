using System;
using OnlyFarms.Locations.Domain;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;

namespace OnlyFarms.Locations
{
    [Serializable]
    public class LocationDefinition
    {
        public string Id; //TODO: String identifier not good
        [Obsolete][FormerlySerializedAs("VideoRef")] public AssetReference BackgroundRef;
        public string BackgroundName;
        public AssetReference HotspotPrefabRef;
        public string OnEnterScript; //TODO: String identifier not good
        public bool HasBackButton;
        public HotspotEntry[] Hotspots;
        public float TransitionDuration = 1f;

        public LocationData ToLocationData() =>
            new(Id, HasBackButton);
    }
}