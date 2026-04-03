namespace OnlyFarms.Locations.Domain
{
    public interface IHotspotValidator
    {
        bool IsAvalible(HotspotData hotspotData);
    }
    
    public class AlwaysAvailableHotspotValidator : IHotspotValidator
    {
        public bool IsAvalible(HotspotData hotspotData) => true;
    }
}