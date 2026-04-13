namespace OnlyFarms.Locations.Domain
{
    public interface IHotspotRepository
    {
        HotspotData[] GetAllHotspots();
        HotspotData GetHotspot(string hotspotId);
    }
}