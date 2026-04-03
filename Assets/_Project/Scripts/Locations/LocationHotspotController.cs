using OnlyFarms.Locations.Input;

namespace OnlyFarms.Locations
{
    public class LocationHotspotController
    {
        private readonly LocationService _locationService;
        private readonly IHotspotInput _hotspotInput;

        public LocationHotspotController(LocationService locationService, IHotspotInput hotspotInput)
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