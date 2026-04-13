namespace OnlyFarms.Domain.Locations
{
    public interface ILocationRepository
    {
        LocationData[] GetAllLocations();
    }
}