using System;
using Naninovel;
using Naninovel.UI;
using OnlyFarms.Domain.Locations;
using OnlyFarms.Utilities;
using UnityEngine;
using UnityEngine.UI;
using LocationService = OnlyFarms.Infrastructure.Services.LocationService;

namespace OnlyFarms.UI
{
    public class BackButtonUI : CustomUI
    {
        [SerializeField] private Button _backButton;
        private LocationService _locationService;

        public override UniTask Initialize()
        {
            //TODO: Добавить сюда вью модель.
            _locationService = Engine.GetService<LocationService>();
            if (_locationService == null)
            {
                throw new NullReferenceException(
                    "LocationService not found. Ensure it is properly registered in the composition root.");
            }

            _backButton.onClick.AddListener(OnBackButtonClicked);
            _locationService.OnLocationEnterCompleted += LocationServiceOnLocationEnterCompleted;
            _locationService.OnNavigatedBack += Hide;
            _locationService.OnNavigatedForward += Hide;
            _locationService.OnFreeRoamEnded += Hide;

            Hide();

            return UniTask.CompletedTask;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (_locationService != null)
            {
                _locationService.OnLocationEnterCompleted -= LocationServiceOnLocationEnterCompleted;
                _locationService.OnNavigatedBack -= Hide;
                _locationService.OnNavigatedForward -= Hide;
                _locationService.OnFreeRoamEnded -= Hide;
            }

            _backButton.onClick.RemoveListener(OnBackButtonClicked);
        }

        private void LocationServiceOnLocationEnterCompleted(LocationData obj)
        {
            if (_locationService.CanGoBack())
                Show();
            else
                Hide();
        }

        private void OnBackButtonClicked() =>
            _locationService.GoBack(destroyCancellationToken).Forget();
    }
}