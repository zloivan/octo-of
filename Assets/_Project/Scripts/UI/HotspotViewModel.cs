using OnlyFarms.Locations.Domain;

namespace OnlyFarms.Locations.UI
{
    public class HotspotViewModel
    {
        private readonly HotspotData _hotspotData;
        public HotspotViewModel(HotspotData hotspotData) =>
            _hotspotData = hotspotData;

        public string GetHotspotLabel() =>
            _hotspotData.Label;

        public bool HasLable() =>
            _hotspotData.Type == HotspotType.Transition;
    }
}