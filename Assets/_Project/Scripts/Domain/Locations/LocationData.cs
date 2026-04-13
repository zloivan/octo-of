namespace OnlyFarms.Domain.Locations
{
    public sealed class LocationData
    {
        public readonly string Id;
        public readonly bool HasBackButton;

        public LocationData(string id, bool hasBackButton)
        {
            Id = id;
            HasBackButton = hasBackButton;
        }

        public override string ToString() =>
            $"LocationData(Id={Id}, HasBackButton={HasBackButton})";
    }
}