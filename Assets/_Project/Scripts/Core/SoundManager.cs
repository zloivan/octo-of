using Naninovel;
using OnlyFarms.Locations;

namespace OnlyFarms.Core
{
    [InitializeAtRuntime]
    public class SoundManager : IEngineService
    {
        private readonly GameSoundConfigSO _config;
        private readonly LocationService _locationService;
        private readonly IAudioManager _audioManager;

        public SoundManager(GameConfig gameConfig, LocationService locationService, IAudioManager audioManager)
        {
            _config = gameConfig.SoundConfig;
            _locationService = locationService;
            _audioManager = audioManager;
        }

        public UniTask InitializeService()
        {
            _locationService.OnNavigatedForward += LocationService_OnNavigatedForward;
            _locationService.OnNavigatedBack += LocationService_OnNavigatedBack;
            return UniTask.CompletedTask;
        }

        public void DestroyService()
        {
            _locationService.OnNavigatedForward -= LocationService_OnNavigatedForward;
            _locationService.OnNavigatedBack -= LocationService_OnNavigatedBack;
        }

        public void ResetService()
        {
        }

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