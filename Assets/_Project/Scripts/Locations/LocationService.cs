using System;
using Locations.Domain;
using Naninovel;
using UnityEngine;

namespace Locations
{
    public class LocationService : IStatefulService<LocationServiceState>
    {
        public event Action<LocationData> OnLocationEntered;
        
        public UniTask InitializeService()
        {
            Debug.Log("LocationService initialized");
            return UniTask.CompletedTask;
        }


        public void ResetService()
        {
            Debug.Log("LocationService reset");
        }

        public void DestroyService()
        {
            Debug.Log("LocationService destroyed");
        }

        public void SaveServiceState(LocationServiceState stateMap)
        {
            Debug.Log("LocationService saved");
        }

        public UniTask LoadServiceState(LocationServiceState stateMap) =>
            UniTask.CompletedTask;

        public void Enter(string locationId)
        {
            Debug.Log($"LocationService entered {locationId}");
        }

        public void OnItemClicked(string itemId)
        {
            Debug.Log($"LocationService item clicked {itemId}");
        }
    }
}