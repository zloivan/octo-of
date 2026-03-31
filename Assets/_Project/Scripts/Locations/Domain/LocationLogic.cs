using System;
using System.Collections.Generic;
using System.Linq;

namespace Locations.Domain
{
    public class LocationLogic
    {
        private LocationData _currentLocation;
        private bool _canGoBack;
        private string _currentLocationId;
        private Stack<string> _locationHistoryStack = new();
        private HashSet<string> _consumedItemIdSet = new();
        private readonly LocationData[] _locationsArray;


        public LocationLogic(LocationData[] locationArrayDefinitions) =>
            _locationsArray = locationArrayDefinitions;

        public void Enter(string locationId)
        {
            _currentLocationId = locationId;
            _locationHistoryStack.Push(locationId);
        }

        public void GoBack()
        {
            if (_locationHistoryStack.Count > 1)
                _locationHistoryStack.Pop();
        }

        public LocationData GetCurrentLocation() =>
            _currentLocation;

        public bool CanGoBack() =>
            _canGoBack;

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
            new(_currentLocationId, _locationHistoryStack.ToArray(), _consumedItemIdSet.ToArray());


        public void LoadSnapshot(LocationLogicSnapshot snapshot)
        {
            _currentLocationId = snapshot.CurrentLocationId;
            _locationHistoryStack = new Stack<string>(snapshot.LocationHistory);
            _consumedItemIdSet = new HashSet<string>(snapshot.ConsumedItemIds);
        }

        public void Reset()
        {
            _currentLocationId = null;
            _locationHistoryStack.Clear();
            _consumedItemIdSet.Clear();
        }
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