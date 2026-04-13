using System;

namespace OnlyFarms.Infrastructure.Persistance
{
    [Serializable]
    public class LocationServiceState
    {
        public string CurrentLocationId;
        public string[] LocationHistoryArray;
        public string[] ConsumedItemsIdArray;
        public bool IsInFreeRoam;
    }
}