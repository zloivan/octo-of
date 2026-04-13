using OnlyFarms.Infrastructure.Services;

namespace OnlyFarms.Domain.Hotspots
{
    public interface IHotspotValidator
    {
        bool IsAvailable(HotspotData hotspotData);
    }
    
    public class AlwaysAvailableHotspotValidator : IHotspotValidator
    {
        public bool IsAvailable(HotspotData hotspotData) => true;
    }
}