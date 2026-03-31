using System;
using System.Linq;
using Locations.Domain;
using UnityEngine.AddressableAssets;

namespace Locations
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
            new()
            {
                Id = Id,
                OnEnterScript = OnEnterScript,
                HasBackButton = HasBackButton,
                Hotspots = Hotspots.Select(h => h.GetHotspotData()).ToArray()
            };
    }
}