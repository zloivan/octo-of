using JetBrains.Annotations;
using Naninovel;
using OnlyFarms.Core;
using OnlyFarms.Locations.Domain;
using OnlyFarms.Utilities;

namespace OnlyFarms.Locations.Services
{
    [UsedImplicitly]
    [InitializeAtRuntime]
    public class GameFlowService : IEngineService
    {
        private readonly LocationService _locationService;
        private readonly IScriptPlayer _scriptPlayer;
        private readonly IQuestStatusSource _questStatusSource;

        private ILocationNarrativeSource _narrativeSource;
        private IFreeRoamSessionSource _sessionSource;
        private IItemNarrativeSource _itemNarrativeSource;

        public GameFlowService(LocationService locationService, IScriptPlayer scriptPlayer,
            QuestService questStatusSource, GameConfig gameConfig)
        {
            _locationService = locationService;
            _scriptPlayer = scriptPlayer;
            _questStatusSource = questStatusSource;
        }

        public UniTask InitializeService()
        {
            //TODO: Временно жестко заданный источник, потом нужно будет сделать возможность его настройки
            _narrativeSource = new AlwaysNullNarrativeSource();
            _itemNarrativeSource = new AlwaysNullItemNarrativeSource();

            _locationService.OnLocationEnterStarted += LocationService_OnLocationEnterStarted;
            _questStatusSource.OnAllQuestsCompleted += QuestStatusSource_OnAllQuestsCompleted;
            _locationService.OnItemPickedUp += LocationService_OnItemPickedUp;
            OFLogger.Log("GameFlowService initialized");
            return UniTask.CompletedTask;
        }

        private void LocationService_OnItemPickedUp(string hotspotId)
        {
            var script = _itemNarrativeSource.GetOnUseScript(hotspotId);

            if (string.IsNullOrEmpty(script))
                return;

            LaunchNarrativeAsync(script, _itemNarrativeSource.GetOnUseLabel(hotspotId)).Forget();
        }

        public void ResetService()
        {
            //TODO: Почему скидываем только этот сорс?
            _sessionSource = null;
            OFLogger.Log("GameFlowService reset");
        }

        public void DestroyService()
        {
            _locationService.OnLocationEnterStarted -= LocationService_OnLocationEnterStarted;
            _questStatusSource.OnAllQuestsCompleted -= QuestStatusSource_OnAllQuestsCompleted;
            _locationService.OnItemPickedUp -= LocationService_OnItemPickedUp;
            OFLogger.Log("GameFlowService destroyed");
        }

        //TODO: Временно публичный
        public void SetSessionSource(IFreeRoamSessionSource sessionSource) =>
            _sessionSource = sessionSource;

        //TODO: Временно жестко заданный источник, потом нужно будет сделать возможность его настройки
        public void SetLocationNarrativeSource(ILocationNarrativeSource narrativeSource)
        {
            _narrativeSource = narrativeSource;
            OFLogger.Log("LocationNarrativeSource changed...");
        }

        //TODO: Временно жестко заданный источник, потом нужно будет сделать возможность его настройки
        public void SetItemNarrativeSource(IItemNarrativeSource itemNarrativeSource)
        {
            _itemNarrativeSource = itemNarrativeSource;
            OFLogger.Log("ItemNarrativeSource changed...");
        }

        private void LocationService_OnLocationEnterStarted(LocationData locationData)
        {
            OFLogger.Log("Start processing on location enter...");
            var script = _narrativeSource.GetOnEnterScript(locationData.Id);
            if (string.IsNullOrEmpty(script))
            {
                OFLogger.Log($"No script attached to location {locationData.Id}");
                return;
            }

            LaunchNarrativeAsync(script, _narrativeSource.GetOnEnterLabel(locationData.Id)).Forget();
        }

        private async UniTask QuestStatusSource_OnAllQuestsCompleted()
        {
            var returnScript = _sessionSource?.GetReturnScript();

            if (string.IsNullOrEmpty(returnScript))
            {
                OFLogger.LogWarning("No return script defined for free roam session. Returning to free roam mode.");
                return;
            }

            _locationService.SetFreeRoamMode(false).Forget();

            await LaunchNarrativeAsync(returnScript, _sessionSource?.GetReturnLabel());
        }

        private async UniTask LaunchNarrativeAsync(string scriptName, string label)
        {
            await _locationService.SetFreeRoamMode(false);
            _scriptPlayer.Stop();

            if (!string.IsNullOrEmpty(label))
                await _scriptPlayer.LoadAndPlayAtLabel(scriptName, label);
            else
                await _scriptPlayer.LoadAndPlay(scriptName);

            OFLogger.Log($"Launching narrative: {scriptName} at label: {label}");
        }
    }
}