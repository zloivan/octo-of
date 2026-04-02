using System;
using System.Collections.Generic;
using System.Linq;

namespace OnlyFarms.Locations.Domain
{
    public class LocationLogic
    {
        private LocationData _currentLocation;
        private Stack<string> _locationHistoryStack = new();
        private HashSet<string> _consumedItemIdSet = new();
        private readonly LocationData[] _locationsArray;


        public LocationLogic(LocationData[] locationArrayDefinitions) =>
            _locationsArray = locationArrayDefinitions;

        public void Enter(string locationId)
        {
            _currentLocation = GetLocation(locationId);
            _locationHistoryStack.Push(locationId);
        }

        public void GoBack()
        {
            if (!CanGoBack())
                return;
            
            _locationHistoryStack.Pop();
            _currentLocation = GetLocation(_locationHistoryStack.Peek());
        }

        public LocationData GetCurrentLocation() =>
            _currentLocation;

        public bool CanGoBack() =>
            _locationHistoryStack.Count > 1;

        public LocationData GetLocation(string locationId)
        {
            var result = Array.Find(_locationsArray, l => l.Id == locationId);
            return result ?? throw new ArgumentException($"Location not found: {locationId}");
        }

        public string[] GetTransitionTargets(string locationId)
        {
            var location = GetLocation(locationId);
            return location.Hotspots
                .Where(h => h.Type == HotspotType.Transition && !string.IsNullOrEmpty(h.TargetLocationId))
                .Select(h => h.TargetLocationId)
                .ToArray();
        }

        public void MarkConsumed(string itemId)
        {
            _consumedItemIdSet.Add(itemId);
        }

        public bool IsConsumed(string itemId) =>
            _consumedItemIdSet.Contains(itemId);

        public LocationLogicSnapshot GetSnapshot() =>
            new(_currentLocation?.Id, _locationHistoryStack.ToArray(), _consumedItemIdSet.ToArray());


        public void LoadSnapshot(LocationLogicSnapshot snapshot)
        {
            _currentLocation = !string.IsNullOrEmpty(snapshot.CurrentLocationId)
                ? GetLocation(snapshot.CurrentLocationId)
                : null;
            _locationHistoryStack = new Stack<string>(snapshot.LocationHistory);
            _consumedItemIdSet = new HashSet<string>(snapshot.ConsumedItemIds);
        }

        public void Reset()
        {
            _currentLocation = null;
            _locationHistoryStack.Clear();
            _consumedItemIdSet.Clear();
        }

        public override string ToString() =>
            $"LocationLogic(CurrentLocationId={_currentLocation?.Id}, LocationHistory={_locationHistoryStack.Count}, ConsumedItemIds={_consumedItemIdSet.Count})";
    }

    public readonly struct LocationLogicSnapshot
    {
        public readonly string CurrentLocationId;
        public readonly string[] LocationHistory;
        public readonly string[] ConsumedItemIds;

        public LocationLogicSnapshot(string currentLocationId, string[] locationHistory, string[] consumedItemIds)
        {
            CurrentLocationId = currentLocationId;
            LocationHistory = locationHistory;
            ConsumedItemIds = consumedItemIds;
        }
    }
}