using System;
using Naninovel;
using Naninovel.UI;
using OnlyFarms.Locations.Domain;
using OnlyFarms.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace OnlyFarms.Locations.UI
{
    public class BackButtonUI : CustomUI
    {
        [SerializeField] private Button _backButton;
        private LocationService _locationService;

        public override UniTask Initialize()
        {
            _locationService = Engine.GetService<LocationService>();
            if (_locationService == null)
            {
                throw new NullReferenceException(
                    "LocationService not found. Ensure it is properly registered in the composition root.");
            }

            _backButton.onClick.AddListener(OnBackButtonClicked);
            _locationService.OnLocationRenderComplete += LocationService_OnLocationRenderComplete;
            _locationService.OnNavigatedBack += Hide;
            _locationService.OnNavigatedForward += Hide;

            Hide();

            return UniTask.CompletedTask;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (_locationService != null)
            {
                _locationService.OnLocationRenderComplete -= LocationService_OnLocationRenderComplete;
                _locationService.OnNavigatedBack -= Hide;
                _locationService.OnNavigatedForward -= Hide;
            }

            _backButton.onClick.RemoveListener(OnBackButtonClicked);
        }

        private void LocationService_OnLocationRenderComplete(LocationData obj)
        {
            OFLogger.Log("Location render complete, checking back button visibility...");
            if (_locationService.CanGoBack())
                Show();
            else
                Hide();
        }

        private void OnBackButtonClicked() =>
            _locationService.GoBack(destroyCancellationToken).Forget();
    }
}