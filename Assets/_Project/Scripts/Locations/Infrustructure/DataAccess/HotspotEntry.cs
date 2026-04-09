using System;
using OnlyFarms.Locations.Domain;
using UnityEngine;

namespace OnlyFarms.Locations
{
    [Serializable]
    public class HotspotEntry
    {
        [SerializeField] private string Id; //TODO: String identifier not good
        [SerializeField] private HotspotType Type;
        [SerializeField] private ActivationCondition Condition;
        [SerializeField] private string ConditionValue; //TODO: String identifier not good
        [SerializeField] private string _targetLocationId;
        

        public HotspotData GetHotspotData(string locationID) =>
            new(Id, Type, Condition, ConditionValue, _targetLocationId, locationID);
    }
}