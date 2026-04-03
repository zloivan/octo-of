namespace OnlyFarms.Locations.Domain
{
    public interface IHotspotRepository
    {
        HotspotData[] GetAllHotspots();
    }
}