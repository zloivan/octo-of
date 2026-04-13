using System;
using System.Collections.Generic;
using System.Linq;

namespace OnlyFarms.Domain.Locations
{
    public class LocationLogic
    {
        private LocationData _currentLocation;
        private Stack<string> _locationHistoryStack = new();
        private readonly LocationData[] _locationsArray;

        public LocationLogic(ILocationRepository locationRepository)
        {
            if (locationRepository == null)
                throw new ArgumentNullException(nameof(locationRepository), "Location repository cannot be null.");
            
            _locationsArray = locationRepository.GetAllLocations();
        }

        public void Enter(string locationId)
        {
            if (_currentLocation!= null &&_currentLocation.Id == locationId)
                return;
            
            _currentLocation = GetLocation(locationId);
            _locationHistoryStack.Push(locationId);
        }

        public string GoBack()
        {
            if (!CanGoBack())
                return _currentLocation.Id;

            _locationHistoryStack.Pop();
            _currentLocation = GetLocation(_locationHistoryStack.Peek());
            
            return _currentLocation.Id;
        }

        public LocationData GetCurrentLocation() =>
            _currentLocation;

        public bool CanGoBack() =>
            _locationHistoryStack.Count > 1 && _currentLocation.HasBackButton;

        public LocationData GetLocation(string locationId) =>
            Array.Find(_locationsArray, l => l.Id == locationId);

        public LocationLogicSnapshot GetSnapshot() =>
            new(_currentLocation?.Id, _locationHistoryStack.ToArray());


        public void LoadSnapshot(LocationLogicSnapshot snapshot)
        {
            _currentLocation = !string.IsNullOrEmpty(snapshot.CurrentLocationId)
                ? GetLocation(snapshot.CurrentLocationId)
                : null;

            var history = snapshot.LocationHistory ?? Array.Empty<string>();
            _locationHistoryStack = new Stack<string>(history.Reverse());
        }

        public void Reset()
        {
            _currentLocation = null;
            _locationHistoryStack.Clear();
        }

        public override string ToString() =>
            $"LocationLogic(CurrentLocationId={_currentLocation?.Id}, LocationHistory Cout={_locationHistoryStack.Count}";
    }

    public readonly struct LocationLogicSnapshot
    {
        public readonly string CurrentLocationId;
        public readonly string[] LocationHistory;

        public LocationLogicSnapshot(string currentLocationId, string[] locationHistory)
        {
            CurrentLocationId = currentLocationId;
            LocationHistory = locationHistory;
        }
    }
}