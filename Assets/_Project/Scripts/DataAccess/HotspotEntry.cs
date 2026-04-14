using System;
using OnlyFarms.Domain.Hotspots;
using UnityEngine;

namespace OnlyFarms.DataAccess
{
    [Serializable]
    public class HotspotEntry
    {
        [SerializeField] private string Id; //TODO: String identifier not good
        [SerializeField] private HotspotType Type;
        [SerializeField] private ActivationCondition Condition;
        [SerializeField] private string ConditionValue; //TODO: String identifier not good
        [SerializeField][LocationId] private string _targetLocationId;
        [SerializeField] private string _label;
        [SerializeField] private QuestDefinitionSO _questDefinitionSO;


        public HotspotData GetHotspotData(string locationID) =>
            new(Id, Type, Condition, ResolveConditionValue(), _targetLocationId, locationID, _label);

        private string ResolveConditionValue() =>
            Condition switch
            {
                ActivationCondition.RequiresQuestId => _questDefinitionSO != null
                    ? _questDefinitionSO.name
                    : string.Empty,
                ActivationCondition.RequiresFlag => ConditionValue,
                _ => string.Empty
            };
    }
}