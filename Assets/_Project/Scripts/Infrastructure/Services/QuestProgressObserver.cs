using Naninovel;
using OnlyFarms.Domain;
using OnlyFarms.Domain.Locations;
using OnlyFarms.Utilities;

namespace OnlyFarms.Infrastructure.Services
{
    [InitializeAtRuntime]
    public class QuestProgressObserver : IEngineService
    {
        private readonly LocationService _locationService;
        private readonly IQuestProgressReporter _progressReporter;
        private readonly IQuestStatusSource _questSource;

        public QuestProgressObserver(LocationService locationService, QuestService progressReporter)
        {
            _locationService = locationService;
            _progressReporter = progressReporter;
            _questSource = progressReporter;
        }

        public UniTask InitializeService()
        {
            _locationService.OnLocationEnterCompleted += LocationService_OnLocationEnterCompleted;
            _locationService.OnItemPickedUp += LocationService_OnItemPickedUp;
            _questSource.OnQuestCompleted += QuestSource_OnQuestCompleted;

            OFLogger.Log("<color=blue>Initialized...</color>");
            return UniTask.CompletedTask;
        }

        public void DestroyService()
        {
            _locationService.OnLocationEnterCompleted -= LocationService_OnLocationEnterCompleted;
            _locationService.OnItemPickedUp -= LocationService_OnItemPickedUp;
            _questSource.OnQuestCompleted -= QuestSource_OnQuestCompleted;

            OFLogger.Log("<color=red>Destroyed</color>");
        }

        public void ResetService()
        {
        }

        private void QuestSource_OnQuestCompleted(QuestInstance obj) =>
            _locationService.RefreshConditionalHotspots();

        private void LocationService_OnItemPickedUp(string hotspotId)
        {
            OFLogger.Log($"Item picked: {hotspotId}");
            _progressReporter.ReportEvent(hotspotId);
        }

        private void LocationService_OnLocationEnterCompleted(LocationData locationData)
        {
            OFLogger.Log($"Location visited: {locationData.Id}");
            _progressReporter.ReportEvent(locationData.Id);
        }
    }
}