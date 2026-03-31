using System;
using System.Linq;
using Core;
using Locations.Domain;
using Naninovel;
using Utilities;

namespace Locations
{
    [InitializeAtRuntime]
    public class LocationService : IStatefulService<GameStateMap>
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

        // Сохранение состояния в общую карту сессии
        public void SaveServiceState(GameStateMap stateMap)
        {
            var snapshot = _logic.GetSnapshot();
            var state = new LocationServiceState
            {
                CurrentLocationId = snapshot.CurrentLocationId,
                LocationHistoryArray = snapshot.LocationHistory,
                ConsumedItemsIdArray = snapshot.ConsumedItemIds
            };
            
            stateMap.SetState(state); // Упаковка кастомного объекта [4]
        }

        // Загрузка состояния из карты сессии
        public UniTask LoadServiceState(GameStateMap stateMap)
        {
            var state = stateMap.GetState<LocationServiceState>(); // Извлечение по типу [4]
            
            if (state != null)
            {
                _logic.LoadSnapshot(new LocationLogicSnapshot(
                    state.CurrentLocationId,
                    state.LocationHistoryArray ?? Array.Empty<string>(),
                    state.ConsumedItemsIdArray ?? Array.Empty<string>()));
            }
            
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

        public string GetCurrentLocationId() =>
            _logic.GetCurrentLocation()?.Id;
    }
}