using System;
using OnlyFarms.Domain.Hotspots;
using UnityEngine;

namespace OnlyFarms.Infrastructure.DataAccess
{
    [Serializable]
    public class HotspotEntry
    {
        [SerializeField] private string Id; //TODO: String identifier not good
        [SerializeField] private HotspotType Type;
        [SerializeField] private ActivationCondition Condition;
        [SerializeField] private string ConditionValue; //TODO: String identifier not good
        [SerializeField] private string _targetLocationId;
        [SerializeField] private string _label;


        public HotspotData GetHotspotData(string locationID) =>
            new(Id, Type, Condition, ConditionValue, _targetLocationId, locationID, _label);
    }
}