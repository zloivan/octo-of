using OnlyFarms.Infrastructure.Services;

namespace OnlyFarms.Input
{
    public class LocationHotspotMapper
    {
        private readonly LocationService _locationService;
        private readonly IHotspotInput _hotspotInput;

        public LocationHotspotMapper(LocationService locationService, IHotspotInput hotspotInput)
        {
            _locationService = locationService;
            _hotspotInput = hotspotInput;

            _hotspotInput.OnHotspotClicked += HandleHotspotClicked;
        }

        private void HandleHotspotClicked(string id) =>
            _locationService.OnHotspotClicked(id);

        public void Dispose() =>
            _hotspotInput.OnHotspotClicked -= HandleHotspotClicked;
    }
}