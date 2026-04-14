using Naninovel;
using OnlyFarms.Domain.Locations;
using OnlyFarms.Utilities;

namespace OnlyFarms.Infrastructure.Services
{
    [InitializeAtRuntime]
    public class QuestProgressObserver : IEngineService
    {
        private readonly LocationService _locationService;
        private readonly IQuestProgressReporter _progressReporter;

        public QuestProgressObserver(LocationService locationService, QuestProgressService progressReporter)
        {
            _locationService = locationService;
            _progressReporter = progressReporter;
        }

        public UniTask InitializeService()
        {
            _locationService.OnLocationEnterCompleted += LocationService_OnLocationEnterCompleted;
            _locationService.OnItemPickedUp += LocationService_OnItemPickedUp;

            OFLogger.Log("<color=blue>Initialized...</color>");
            return UniTask.CompletedTask;
        }

        public void DestroyService()
        {
            _locationService.OnLocationEnterCompleted -= LocationService_OnLocationEnterCompleted;
            _locationService.OnItemPickedUp -= LocationService_OnItemPickedUp;
        }

        public void ResetService()
        {
        }

        private void LocationService_OnItemPickedUp(string hotspotId) =>
            _progressReporter.ReportEvent(hotspotId);

        private void LocationService_OnLocationEnterCompleted(LocationData locationData) =>
            _progressReporter.ReportEvent(locationData.Id);
    }
}