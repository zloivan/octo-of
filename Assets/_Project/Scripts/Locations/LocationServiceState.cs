using System;

namespace Locations
{
    [Serializable]
    public class LocationServiceState
    {
        public string CurrentLocationId;
        public string[] LocationHistoryArray;
        public string[] ConsumedItemsIdArray;
    }
}