using System;
using OnlyFarms.Domain.Locations;
using UnityEngine.AddressableAssets;

namespace OnlyFarms.DataAccess
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