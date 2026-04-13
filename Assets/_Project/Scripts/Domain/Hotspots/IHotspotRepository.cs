namespace OnlyFarms.Domain.Hotspots
{
    public interface IHotspotRepository
    {
        HotspotData[] GetAllHotspots();
        HotspotData GetHotspot(string hotspotId);
    }
}