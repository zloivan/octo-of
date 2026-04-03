namespace OnlyFarms.Locations.Domain
{
    public interface ILocationRepository
    {
        LocationData[] GetAllLocations();
    }
}