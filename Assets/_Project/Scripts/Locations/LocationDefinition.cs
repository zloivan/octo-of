using System;
using System.Linq;
using OnlyFarms.Locations.Domain;
using UnityEngine.AddressableAssets;

namespace OnlyFarms.Locations
{
    [Serializable]
    public class LocationDefinition
    {
        public string Id; //TODO: String identifier not good
        public AssetReference VideoRef;
        public AssetReference HotspotPrefabRef;
        public string OnEnterScript; //TODO: String identifier not good
        public bool HasBackButton;
        public HotspotEntry[] Hotspots;

        public LocationData ToLocationData() =>
            new(Id, OnEnterScript, HasBackButton, Hotspots.Select(h => h.GetHotspotData()).ToArray());
    }
}