using System;

namespace Locations.Domain
{
    [Serializable]
    public class LocationData
    {
        public string Id;
        public string OnEnterScript;
        public bool HasBackButton;
        public HotspotData[] Hotspots;
    }
}