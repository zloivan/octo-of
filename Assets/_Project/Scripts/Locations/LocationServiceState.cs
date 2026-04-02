using System;

namespace OnlyFarms.Locations
{
    [Serializable]
    public class LocationServiceState
    {
        public string CurrentLocationId;
        public string[] LocationHistoryArray;
        public string[] ConsumedItemsIdArray;
    }
}