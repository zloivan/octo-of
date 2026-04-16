using Naninovel;
using OnlyFarms.DataAccess;
using OnlyFarms.Domain;
using OnlyFarms.Domain.Hotspots;
using OnlyFarms.Domain.Locations;
using OnlyFarms.Domain.Quests;
using OnlyFarms.Utilities;

namespace OnlyFarms.Infrastructure.Services
{
    [InitializeAtRuntime]
    public class QuestProgressObserver : IEngineService
    {
        private readonly LocationService _locationService;
        private readonly IQuestProgressReporter _progressReporter;
        private readonly IQuestStatusSource _questSource;
        private readonly IHotspotRepository _hotspotRepository;

        public QuestProgressObserver(LocationService locationService, QuestService progressReporter, GameConfig config)
        {
            _locationService = locationService;
            _progressReporter = progressReporter;
            _questSource = progressReporter;
            _hotspotRepository = config.LocationConfig;
        }

        public UniTask InitializeService()
        {
            _locationService.OnLocationEnterCompleted += LocationService_OnLocationEnterCompleted;
            _locationService.OnItemPickedUp += LocationService_OnItemPickedUp;
            _questSource.OnQuestCompleted += QuestSource_OnQuestCompleted;
            _questSource.OnQuestObjectiveTicked += QuestSource_OnQuestObjectiveTicked;
            OFLogger.Log("<color=blue>Initialized...</color>");
            return UniTask.CompletedTask;
        }

        public void DestroyService()
        {
            _locationService.OnLocationEnterCompleted -= LocationService_OnLocationEnterCompleted;
            _locationService.OnItemPickedUp -= LocationService_OnItemPickedUp;
            _questSource.OnQuestCompleted -= QuestSource_OnQuestCompleted;
            _questSource.OnQuestObjectiveTicked -= QuestSource_OnQuestObjectiveTicked;

            OFLogger.Log("<color=red>Destroyed</color>");
        }

        public void ResetService()
        {
        }

        private void QuestSource_OnQuestCompleted(QuestInstance obj) =>
            _locationService.RefreshConditionalHotspots();

        private void QuestSource_OnQuestObjectiveTicked(QuestInstance quest, QuestObjectiveInstance objective)
        {
            if (objective.IsCompleted())
                _locationService.RefreshConditionalHotspots();
        }
        
        private void LocationService_OnItemPickedUp(string hotspotId)
        {
            OFLogger.Log($"Item picked: {hotspotId}");
            var hotspotData = _hotspotRepository.GetHotspot(hotspotId);
            _progressReporter.ReportEvent(hotspotData.ObjectiveEventId);
        }

        private void LocationService_OnLocationEnterCompleted(LocationData locationData)
        {
            OFLogger.Log($"Location visited: {locationData.Id}");
            _progressReporter.ReportEvent(locationData.Id);
        }
    }
}