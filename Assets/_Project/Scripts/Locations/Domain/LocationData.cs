namespace Locations.Domain
{
    public sealed class LocationData
    {
        public readonly string Id;
        public readonly string OnEnterScript;
        public readonly bool HasBackButton;
        public readonly HotspotData[] Hotspots;

        public LocationData(string id, string onEnterScript, bool hasBackButton, HotspotData[] hotspots)
        {
            Id = id;
            OnEnterScript = onEnterScript;
            HasBackButton = hasBackButton;
            Hotspots = hotspots;
        }
        public override string ToString() =>
            $"LocationData(Id={Id}, OnEnterScript={OnEnterScript}, HasBackButton={HasBackButton}, Hotspots={Hotspots.Length})";        
        
    }
}