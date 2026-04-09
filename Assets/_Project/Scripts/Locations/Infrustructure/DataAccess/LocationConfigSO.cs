using System.Linq;
using OnlyFarms.Locations.Domain;
using UnityEngine;

namespace OnlyFarms.Locations
{
    [CreateAssetMenu(fileName = "New Location Config", menuName = "Configs/Locations/Location Config", order = 0)]
    public class LocationConfigSO : ScriptableObject, ILocationRepository, IHotspotRepository
    {
        public LocationDefinition StartingLocation;
        public LocationDefinition[] Locations;
        public Texture2D MouseTexture;
        public Vector2 MouseTextureHotspot;

        public LocationDefinition GetLocationDefinition(string locationId)
        {
            foreach (var location in Locations)
            {
                if (location.Id == locationId)
                    return location;
            }

            Debug.LogError($"Location with id {locationId} not found in LocationConfigSO.");
            return null;
        }

        public LocationData[] GetAllLocations() =>
            Locations.Select(l => l.ToLocationData()).ToArray();

        public HotspotData[] GetAllHotspots() =>
            Locations.SelectMany(l => l.Hotspots.Select(h => h.GetHotspotData(locationID: l.Id))).ToArray();

        public HotspotData GetHotspot(string hotspotId) =>
            GetAllHotspots().FirstOrDefault(h => h.Id == hotspotId);

    }
}