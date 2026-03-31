using System;
using System.Linq;
using Core;
using Locations.Domain;
using Naninovel;
using Utilities;

namespace Locations
{
    [InitializeAtRuntime]
    public class LocationService : IStatefulService<LocationServiceState>
    {
        public event Action<LocationData> OnLocationEntered;

        private readonly LocationConfigSO _config;
        private LocationLogic _logic;

        public LocationService(GameConfig gameConfig) =>
            _config = gameConfig.LocationConfig;

        public UniTask InitializeService()
        {
            var data = _config.Locations.Select(l => l.ToLocationData()).ToArray();
            _logic = new LocationLogic(data);

            OFLogger.Log(
                $"LocationService initialized with [{data.Length} ]locations: [{string.Join(", ", data.Select(d => d.ToString()))}] ");
            return UniTask.CompletedTask;
        }


        public void ResetService()
        {
            _logic.Reset();
            OFLogger.Log("LocationService reset");
        }

        public void DestroyService()
        {
            OFLogger.Log("LocationService destroyed");
        }

        public void SaveServiceState(LocationServiceState stateMap)
        {
            var snapshot = _logic.GetSnapshot();

            stateMap.CurrentLocationId = snapshot.CurrentLocationId;
            stateMap.LocationHistoryArray = snapshot.LocationHistory;
            stateMap.ConsumedItemsIdArray = snapshot.ConsumedItemIds;


            OFLogger.Log("LocationService saved");
        }

        public UniTask LoadServiceState(LocationServiceState stateMap)
        {
            _logic.LoadSnapshot(new LocationLogicSnapshot(
                stateMap.CurrentLocationId,
                stateMap.LocationHistoryArray ?? Array.Empty<string>(),
                stateMap.ConsumedItemsIdArray ?? Array.Empty<string>()));

            OFLogger.Log("LocationService loaded");
            return UniTask.CompletedTask;
        }

        public void Enter(string locationId)
        {
            _logic.Enter(locationId);
            OnLocationEntered?.Invoke(_logic.GetCurrentLocation());
            OFLogger.Log($"LocationService entered {_logic.GetCurrentLocation().Id}");
        }

        public void OnItemClicked(string itemId)
        {
            _logic.MarkConsumed(itemId);
            OFLogger.Log($"LocationService item clicked {itemId}");
        }

        public void GoBack()
        {
            if (!_logic.CanGoBack())
            {
                OFLogger.Log("LocationService can't go back");
                return;
            }

            _logic.GoBack();
            OnLocationEntered?.Invoke(_logic.GetCurrentLocation());
            OFLogger.Log($"LocationService go back to: {_logic.GetCurrentLocation().Id}");
        }
    }
}