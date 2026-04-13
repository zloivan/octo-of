using System;
using System.Linq;
using System.Threading;
using Naninovel;
using Naninovel.UI;
using OnlyFarms.DataAccess;
using OnlyFarms.Domain.Hotspots;
using OnlyFarms.Domain.Locations;
using OnlyFarms.Infrastructure.Input;
using OnlyFarms.Infrastructure.Persistance;
using OnlyFarms.Infrastructure.Rendering;
using OnlyFarms.UI;
using OnlyFarms.Utilities;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OnlyFarms.Infrastructure.Services
{
    //TODO: Все зависимости созданные здесь, по идее должны идти из корня композиций
    [InitializeAtRuntime]
    public class LocationService : IStatefulService<GameStateMap>
    {
        private const string LOCATION_ACTOR = "location";
        public event Action<LocationData> OnLocationEnterCompleted;
        public event Action<LocationData> OnLocationEnterStarted;
        public event Action<string> OnItemPickedUp;

        public event Action OnNavigatedForward;
        public event Action OnNavigatedBack;
        public event Action OnFreeRoamEnded;

        private readonly LocationConfigSO _config;
        private readonly IBackgroundManager _backgroundManager;
        private HotspotManager _hotspotManager;
        private LocationLogic _locationLogic;
        private HotspotLogic _hotspotLogic;
        private LocationHotspotMapper _locationHotspotMapper;
        private HotspotCursorController _hotspotCursorController;
        private GraphicRaycaster _continueInputRaycaster;
        private bool _isInFreeRoam;
        private readonly IScriptPlayer _scriptPlayer;
        private CancellationTokenSource _cts = new();

        public LocationService(GameConfig gameConfig, IBackgroundManager backgroundManager, IScriptPlayer scriptPlayer)
        {
            _config = gameConfig.LocationConfig;
            _backgroundManager = backgroundManager;
            _scriptPlayer = scriptPlayer;
        }

        public UniTask InitializeService()
        {
            _locationLogic = new LocationLogic(_config);
            var questService = Engine.GetService<QuestService>();
            _hotspotLogic = new HotspotLogic(_config, new HotspotValidator(questService));

            //TODO: Явно не обязанность этого сервиса, он должен только распределить обязанности
            //     временное решение, явно кто то другой должен отвечать за спаун и проверку подходит ли текущая локация или нет

            var inputGo = new GameObject("HotspotInput");
            UnityEngine.Object.DontDestroyOnLoad(inputGo);
            var mouseInput = inputGo.AddComponent<MouseHotspotInput>();

            _hotspotManager = new HotspotManager(mouseInput);
            _locationHotspotMapper = new LocationHotspotMapper(this, mouseInput);
            _hotspotCursorController =
                new HotspotCursorController(mouseInput, _config.MouseTexture, _config.MouseTextureHotspot);

            ApplyInputWorkaroundsAsync().Forget();


            OFLogger.Log("<color=blue>Initialized</color>");
            return UniTask.CompletedTask;
        }

        public void ResetService()
        {
            _locationLogic.Reset();
            _hotspotLogic.Reset();
            OFLogger.Log("<color=yellow>Reset</color>");
        }

        public void DestroyService()
        {
            _locationHotspotMapper.Dispose();
            _hotspotCursorController.Dispose();
            OFLogger.Log("<color=red>Destroyed</color>");
        }

        public async UniTask Enter(string locationId, AsyncToken ct)
        {
            var definition = _config.GetLocationDefinition(locationId);

            if (definition == null)
            {
                Debug.LogError("LocationService failed to enter location. Definition not found for id: " + locationId);
                return;
            }

            _locationLogic.Enter(locationId);
            await RenderLocation(locationId, ct);
        }

        private async UniTask RenderLocation(string locationId, AsyncToken ct)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            var linked = CancellationTokenSource.CreateLinkedTokenSource(ct.CancellationToken, _cts.Token);
            var renderToken = new AsyncToken(linked.Token);


            OnLocationEnterStarted?.Invoke(_locationLogic.GetCurrentLocation());

            if (renderToken.Canceled)
                return;

            var definition = _config.GetLocationDefinition(locationId);
            var availableHotspots = _hotspotLogic.GetAvailableHotspots(locationId);
            var bg = await _backgroundManager.GetOrAddActor(LOCATION_ACTOR);

            _hotspotCursorController.ResetCursor();
            _hotspotManager.Unload();

            //BUG: Если первая локация для отображение, сразу показывается, при этом остальное показывается через твин,
            // выглядит как зависание во время загрузки локации
            bg.ChangeVisibility(true, new Tween(0), token: ct).Forget();

            await UniTask.WhenAll(
                bg.ChangeAppearance(definition.BackgroundName,
                    new Tween(definition.TransitionDuration), token: ct),
                _hotspotManager.LoadAsync(
                    definition.HotspotPrefabRef,
                    availableHotspots,
                    definition.TransitionDuration,
                    ct)
            );

            OnLocationEnterCompleted?.Invoke(_locationLogic.GetCurrentLocation());
            OFLogger.Log($"LocationService entered {_locationLogic.GetCurrentLocation().Id}");
        }

        public async UniTask GoBack(AsyncToken ct)
        {
            if (!_locationLogic.CanGoBack())
            {
                OFLogger.Log("LocationService can't go back");
                return;
            }

            var backLocationId = _locationLogic.GoBack();
            OnNavigatedBack?.Invoke();

            OFLogger.Log($"LocationService go back to: {backLocationId}");
            await RenderLocation(backLocationId, ct);
        }

        public void OnHotspotClicked(string hotspotId)
        {
            OFLogger.Log($"LocationService item clicked {hotspotId}");
            var hotspot = _hotspotLogic.GetHotspotData(hotspotId);

            switch (hotspot.Type)
            {
                case HotspotType.Transition:
                    OnNavigatedForward?.Invoke();
                    Enter(hotspot.TargetLocationId, CancellationToken.None).Forget();
                    break;
                case HotspotType.Item:
                    if (_hotspotLogic.TryConsume(hotspotId))
                    {
                        _hotspotManager.DeactivateHotspot(hotspotId);
                        OnItemPickedUp?.Invoke(hotspotId);
                        OFLogger.Log($"LocationService consumed item {hotspotId}");
                        break;
                    }

                    break;
                case HotspotType.MiniGame:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
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

        public async UniTask LoadServiceState(GameStateMap stateMap)
        {
            var state = stateMap.GetState<LocationServiceState>();

            if (state == null)
                return;

            _hotspotLogic.LoadSnapshot(new HotspotLogicSnapshot(
                state.ConsumedItemsIdArray ?? Array.Empty<string>()));

            _locationLogic.LoadSnapshot(new LocationLogicSnapshot(
                state.CurrentLocationId,
                state.LocationHistoryArray ?? Array.Empty<string>()));

            _isInFreeRoam = state.IsInFreeRoam;

            //TODO: Кажется тут происходит много всего IF, кажется что тут закостылено.
            // Нужно подумать как это сделать чище
            if (!string.IsNullOrEmpty(state.CurrentLocationId) && state.IsInFreeRoam)
            {
                _scriptPlayer.Stop();
                await RenderLocation(state.CurrentLocationId, CancellationToken.None);
            }
            else
            {
                _hotspotCursorController.ResetCursor();
                _hotspotManager.Unload();
            }
        }

        public string GetCurrentLocationId() =>
            _locationLogic.GetCurrentLocation()?.Id;

        public bool CanGoBack() =>
            _locationLogic.CanGoBack();

        //TODO: КОСТЫЛЬ, ИСПРАВЬ ПОЖАЛУЙСТА вынести отсюда или найти хорошее решение внтури Naninovel
        private async UniTask ApplyInputWorkaroundsAsync()
        {
            Engine.GetService<ICameraManager>().Camera.AddComponent<Physics2DRaycaster>();

            await UniTask.WaitUntil(() => Engine.Initialized);
            var uiManager = Engine.GetService<IUIManager>();
            var continueUI = uiManager.GetUI<ContinueInputUI>();
            if (continueUI != null)
            {
                _continueInputRaycaster = continueUI.GetComponent<GraphicRaycaster>();
                SetContinueInputEnabled(false);
            }
        }

        private void SetContinueInputEnabled(bool enabled)
        {
            if (_continueInputRaycaster != null)
                _continueInputRaycaster.enabled = enabled;
        }

        public string PrintLocationHistory()
        {
            var history = _locationLogic.GetSnapshot().LocationHistory;
            return string.Join(" -> ", history);
        }

        public LocationDefinition GetStartingLocation() =>
            _config.StartingLocation;

        public async UniTask SetFreeRoamMode(bool value, AsyncToken token = default)
        {
            _isInFreeRoam = value;
            SetContinueInputEnabled(!value);

            if (!value)
            {
                _cts?.Cancel();
                _hotspotCursorController.ResetCursor();
                _hotspotManager.Unload();
                var bg = await _backgroundManager.GetOrAddActor(LOCATION_ACTOR);
                bg.ChangeVisibility(false, new Tween(0.3f), token: token).Forget();

                OnFreeRoamEnded?.Invoke();
            }
        }

        public string[] GetAllLocationIds() =>
            _config.GetAllLocations().Select(l => l.Id).ToArray();

        public string[] GetConsumedHotspotIds() =>
            _hotspotLogic.GetConsumedHotspotIds();

        public void SetHotspotsVisible(bool value) =>
            _hotspotManager.SetVisible(value);
    }
}