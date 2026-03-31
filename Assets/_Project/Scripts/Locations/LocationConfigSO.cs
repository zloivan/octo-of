using UnityEngine;

namespace Locations
{
    [CreateAssetMenu(fileName = "New Location Config", menuName = "Configs/Locations/Location Config", order = 0)]
    public class LocationConfigSO : ScriptableObject
    {
        public LocationDefinition[] Locations;
    }
}