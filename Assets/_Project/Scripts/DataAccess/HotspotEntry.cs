using System;
using OnlyFarms.Domain.Hotspots;
using UnityEngine;
using UnityEngine.Serialization;

namespace OnlyFarms.DataAccess
{
    [Serializable]
    public class HotspotEntry
    {
        [FormerlySerializedAs("Id")] [SerializeField]
        private string _id;

        [FormerlySerializedAs("Type")] [SerializeField]
        private HotspotType _type;

        [FormerlySerializedAs("Condition")] [SerializeField]
        private ActivationCondition _condition;

        [FormerlySerializedAs("ConditionValue")] [SerializeField]
        private string _conditionValue;

        [SerializeField] [LocationId] private string _targetLocationId;
        [SerializeField] private string _label;
        [SerializeField] private QuestDefinitionSO _questDefinitionSO;

        
        public HotspotData GetHotspotData(string locationID) =>
            new(_id, _type, _condition, _conditionValue, _targetLocationId, locationID, _label,
                _questDefinitionSO?.ToDefinition() ?? null);
    }
}