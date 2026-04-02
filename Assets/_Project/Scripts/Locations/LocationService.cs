using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Naninovel;
using Naninovel.UI;
using OnlyFarms.Core;
using OnlyFarms.Locations.Domain;
using OnlyFarms.Locations.Input;
using OnlyFarms.Locations.UI;
using OnlyFarms.Utilities;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UniTaskExtensions = Cysharp.Threading.Tasks.UniTaskExtensions;

namespace OnlyFarms.Locations
{
    //TODO: Все зависимости созданные здесь, по идее должны идти из корня композиций
    [InitializeAtRuntime]
    public class LocationService : IStatefulService<GameStateMap>
    {
        public event Action<LocationData> OnLocationEntered;

        private readonly LocationConfigSO _config;
        private HotspotManager _hotspotManager;
        private LocationLogic _logic;
        private IHotspotInput _hotspotInput;

        public LocationService(GameConfig gameConfig) =>
            _config = gameConfig.LocationConfig;

        
        public UniTask InitializeService()
        {
            var data = _config.Locations.Select(l => l.ToLocationData()).ToArray();
            _logic = new LocationLogic(data);
            
            InitializeHotspotManager();
            
            OnInitialized();

            OFLogger.Log(
                $"LocationService initialized with [{data.Length} ]locations: [{string.Join(", ", data.Select(d => d.ToString()))}] ");
            return UniTask.CompletedTask;
        }

        //TODO: Очень грязно
        private async UniTask OnInitialized()
        {
            Engine.GetService<ICameraManager>().Camera.AddComponent<Physics2DRaycaster>();
            
            
             //TODO: КОСТЫЛЬ, ИСПРАВЬ ПОЖАЛУЙСТА
            
            
            await UniTask.WaitUntil(() => Engine.Initialized);
            var uiManager = Engine.GetService<IUIManager>();
            var continueUI = uiManager.GetUI<ContinueInputUI>();
            if (continueUI != null)
                continueUI.GetComponent<GraphicRaycaster>().enabled = false;
        }

        //TODO: Явно не обязанность этого сервиса, он должен только распределить обязанности
        // временное решение, явно кто то другой должен отвечать за спаун и проверку подходит ли текущая локация или нет
        private void InitializeHotspotManager()
        {
            var inputGo = new GameObject("HotspotInput");
            UnityEngine.Object.DontDestroyOnLoad(inputGo);
            var mouseInput = inputGo.AddComponent<MouseHotspotInput>();

            var hotspotLogic = new HotspotLogic();
            _hotspotManager = new HotspotManager(hotspotLogic, mouseInput);

            _hotspotInput = mouseInput;
            _hotspotInput.OnHotspotClicked += OnItemClicked;

            var cursorGo = new GameObject("HotspotCursor");
            UnityEngine.Object.DontDestroyOnLoad(cursorGo);
            
            cursorGo.AddComponent<HotspotCursorController>().Initialize(mouseInput);
            
            UniTaskExtensions.Forget(_hotspotManager.LoadAsync(
                _config.Locations.First().HotspotPrefabRef, 
                _config.Locations.First().Hotspots.Select(h => h.GetHotspotData()).ToArray(), 
                new HashSet<string>(),
                .3f, CancellationToken.None));
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
                ConsumedItemsIdArray = snapshot.ConsumedItemIds,
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
            OFLogger.Log($"LocationService item clicked {itemId}");
            _logic.MarkConsumed(itemId);
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