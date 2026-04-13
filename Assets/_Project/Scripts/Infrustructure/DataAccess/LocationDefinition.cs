using System;
using OnlyFarms.Locations.Domain;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;

namespace OnlyFarms.Locations
{
    [Serializable]
    public class LocationDefinition
    {
        public string Id;
        public string BackgroundName;
        public AssetReference HotspotPrefabRef;
        public bool HasBackButton;
        public HotspotEntry[] Hotspots;
        public float TransitionDuration = 1f;

        public LocationData ToLocationData() =>
            new(Id, HasBackButton);
    }
}