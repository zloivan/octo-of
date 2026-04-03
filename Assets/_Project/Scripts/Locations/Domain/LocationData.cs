namespace OnlyFarms.Locations.Domain
{
    public sealed class LocationData
    {
        public readonly string Id;
        public readonly string OnEnterScript;
        public readonly bool HasBackButton;

        public LocationData(string id, string onEnterScript, bool hasBackButton)
        {
            Id = id;
            OnEnterScript = onEnterScript;
            HasBackButton = hasBackButton;
        }

        public override string ToString() =>
            $"LocationData(Id={Id}, OnEnterScript={OnEnterScript}, HasBackButton={HasBackButton})";
    }
}