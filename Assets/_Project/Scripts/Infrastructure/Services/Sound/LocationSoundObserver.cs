using Naninovel;
using OnlyFarms.DataAccess;

namespace OnlyFarms.Infrastructure.Services.Sound
{
    [InitializeAtRuntime]
    public class LocationSoundObserver : IEngineService
    {
        private readonly GameSoundConfigSO _config;
        private readonly LocationService _locationService;
        private readonly IAudioManager _audioManager;

        public LocationSoundObserver(GameConfig gameConfig, LocationService locationService, IAudioManager audioManager)
        {
            _config = gameConfig.SoundConfig;
            _locationService = locationService;
            _audioManager = audioManager;
        }

        public UniTask InitializeService()
        {
            _locationService.OnNavigatedForward += LocationService_OnNavigatedForward;
            _locationService.OnNavigatedBack += LocationService_OnNavigatedBack;
            _locationService.OnItemPickedUp += LocationService_OnItemPickedUp;
            return UniTask.CompletedTask;
        }

        public void DestroyService()
        {
            _locationService.OnNavigatedForward -= LocationService_OnNavigatedForward;
            _locationService.OnNavigatedBack -= LocationService_OnNavigatedBack;
            _locationService.OnItemPickedUp -= LocationService_OnItemPickedUp;
        }

        public void ResetService()
        {
        }

        private void LocationService_OnItemPickedUp(string hotspotId) =>
            PlaySfx(_config.ItemPickupAudioId);

        private void LocationService_OnNavigatedForward() =>
            PlaySfx(_config.TransitionForwardAudioId);

        private void LocationService_OnNavigatedBack() =>
            PlaySfx(_config.TransitionBackAudioId);


        private void PlaySfx(string key)
        {
            if (string.IsNullOrEmpty(key))
                return;
            _audioManager?.PlaySfx(key).Forget();
        }
    }
}