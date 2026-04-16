using System;
using OnlyFarms.Attributes;
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

        [FormerlySerializedAs("_questEventId")] [SerializeField] [QuestEventId]
        private string _triggerObjectiveEventId;

        [FormerlySerializedAs("_questDefinitionSO")] [SerializeField]
        private QuestDefinitionSO _requireQuest;

        [SerializeField] [QuestEventId] private string _requiredObjectiveEventId;

        public HotspotData GetHotspotData(string locationID) =>
            new(_id, _type, _condition, _conditionValue, _targetLocationId, locationID, _label,
                _requireQuest?.ToDefinition(), _requiredObjectiveEventId, _triggerObjectiveEventId);
    }
}