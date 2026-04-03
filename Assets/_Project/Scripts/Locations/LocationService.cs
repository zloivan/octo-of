using System;
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

namespace OnlyFarms.Locations
{
    //TODO: Все зависимости созданные здесь, по идее должны идти из корня композиций
    [InitializeAtRuntime]
    public class LocationService : IStatefulService<GameStateMap>
    {
        private const string LOCATION_ACTOR = "location";
        public event Action<LocationData> OnLocationEntered;

        private readonly LocationConfigSO _config;
        private readonly IBackgroundManager _backgroundManager;
        private HotspotManager _hotspotManager;
        private LocationLogic _locationLogic;
        private HotspotLogic _hotspotLogic;
        private LocationHotspotController _locationHotspotController;
        private HotspotCursorController _hotspotCursorController;

        private bool _isInFreeRoam;

        public LocationService(GameConfig gameConfig, IBackgroundManager backgroundManager)
        {
            _config = gameConfig.LocationConfig;
            _backgroundManager = backgroundManager;
            //TODO: TESTING
            _isInFreeRoam = true;
        }

        public UniTask InitializeService()
        {
            _locationLogic = new LocationLogic(_config);
            _hotspotLogic = new HotspotLogic(_config, new AlwaysAvailableHotspotValidator());

            //TODO: Явно не обязанность этого сервиса, он должен только распределить обязанности
            //     временное решение, явно кто то другой должен отвечать за спаун и проверку подходит ли текущая локация или нет

            var inputGo = new GameObject("HotspotInput");
            UnityEngine.Object.DontDestroyOnLoad(inputGo);
            var mouseInput = inputGo.AddComponent<MouseHotspotInput>();

            _hotspotManager = new HotspotManager(mouseInput);
            _locationHotspotController = new LocationHotspotController(this, mouseInput);

            //var cursorGo = new GameObject("HotspotCursor");
            //UnityEngine.Object.DontDestroyOnLoad(cursorGo);
            _hotspotCursorController = new HotspotCursorController(mouseInput, _config.MouseTexture, _config.MouseTextureHotspot);
            //cursorGo.AddComponent<HotspotCursorController>().Initialize(mouseInput);


            ApplyInputWorkaroundsAsync().Forget();


            OFLogger.Log("LocationService initialized");
            return UniTask.CompletedTask;
        }

        public void ResetService()
        {
            _locationLogic.Reset();
            _hotspotLogic.Reset();
            OFLogger.Log("LocationService reset");
        }

        public void DestroyService()
        {
            _locationHotspotController.Dispose();
            _hotspotCursorController.Dispose();
            OFLogger.Log("LocationService destroyed");
        }

        public async UniTask Enter(string locationId, AsyncToken ct)
        {
            var definition = _config.GetLocationDefinition(locationId);
            _locationLogic.Enter(locationId);

            var availableHotpots = _hotspotLogic.GetAvailableHotspots(locationId);
            var bg = await _backgroundManager.GetOrAddActor(LOCATION_ACTOR);
            bg.ChangeVisibility(true,
                new Tween(0), token: ct).Forget();

            await UniTask.WhenAll(
                bg.ChangeAppearance(definition.BackgroundName,
                    new Tween(definition.TransitionDuration), token: ct),
                _hotspotManager.LoadAsync(
                    definition.HotspotPrefabRef,
                    availableHotpots,
                    definition.TransitionDuration,
                    ct)
            );

            OnLocationEntered?.Invoke(_locationLogic.GetCurrentLocation());

            OFLogger.Log($"LocationService entered {_locationLogic.GetCurrentLocation().Id}");
        }

        public async UniTask GoBack(AsyncToken ct)
        {
            if (!_locationLogic.CanGoBack())
            {
                OFLogger.Log("LocationService can't go back");
                return;
            }

            _locationLogic.GoBack();

            OFLogger.Log($"LocationService go back to: {_locationLogic.GetCurrentLocation().Id}");
            await Enter(_locationLogic.GetCurrentLocation().Id, ct);
        }

        public void OnHotspotClicked(string hotspotId)
        {
            OFLogger.Log($"LocationService item clicked {hotspotId}");
            if (_hotspotLogic.TryConsume(hotspotId))
            {
                _hotspotManager.DeactivateHotspot(hotspotId);
                OFLogger.Log($"LocationService consumed item {hotspotId}");
                return;
            }


            Enter(_hotspotLogic.GetHotspotData(hotspotId).TargetLocationId, CancellationToken.None).Forget();
        }

        public void SaveServiceState(GameStateMap stateMap)
        {
            var locationSnapshot = _locationLogic.GetSnapshot();
            var hotspotSnapshot = _hotspotLogic.GetSnapshot();

            var state = new LocationServiceState
            {
                CurrentLocationId = locationSnapshot.CurrentLocationId,
                LocationHistoryArray = locationSnapshot.LocationHistory,
                ConsumedItemsIdArray = hotspotSnapshot.ConsumedItemIds,
                IsInFreeRoam = _isInFreeRoam,
            };

            stateMap.SetState(state);
        }

        public UniTask LoadServiceState(GameStateMap stateMap)
        {
            var state = stateMap.GetState<LocationServiceState>();

            if (state == null)
                return UniTask.CompletedTask;

            _hotspotLogic.LoadSnapshot(new HotspotLogicSnapshot(
                state.ConsumedItemsIdArray ?? Array.Empty<string>()));

            _locationLogic.LoadSnapshot(new LocationLogicSnapshot(
                state.CurrentLocationId,
                state.LocationHistoryArray ?? Array.Empty<string>()));

            _isInFreeRoam = state.IsInFreeRoam;
            
            if (!string.IsNullOrEmpty(state.CurrentLocationId) && state.IsInFreeRoam)
                Enter(state.CurrentLocationId, CancellationToken.None).Forget();

            return UniTask.CompletedTask;
        }

        public string GetCurrentLocationId() =>
            _locationLogic.GetCurrentLocation()?.Id;

        //TODO: КОСТЫЛЬ, ИСПРАВЬ ПОЖАЛУЙСТА вынести отсюда или найти хорошее решение внтури Naninovel
        private async UniTask ApplyInputWorkaroundsAsync()
        {
            Engine.GetService<ICameraManager>().Camera.AddComponent<Physics2DRaycaster>();


            await UniTask.WaitUntil(() => Engine.Initialized);
            var uiManager = Engine.GetService<IUIManager>();
            var continueUI = uiManager.GetUI<ContinueInputUI>();
            if (continueUI != null)
                continueUI.GetComponent<GraphicRaycaster>().enabled = false;
        }
    }
}