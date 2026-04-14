using System;
using System.Linq;
using OnlyFarms.Domain;
using UnityEngine;
using UnityEngine.Serialization;

namespace OnlyFarms.DataAccess
{
    [CreateAssetMenu(fileName = "QuestConfig", menuName = "Configs/Quests/QuestConfig", order = 0)]
    public class QuestConfigSO : ScriptableObject, IQuestRepository
    {
        [FormerlySerializedAs("_dayConfigList")] [SerializeField]
        private DayConfigSO[] _dayConfigArray;

        public QuestDefinition[] GetQuestsOfDay(string dayId) =>
            GetDayConfig(dayId).GetDayQuests();

        public DayConfigSO GetDayConfig(string dayId) =>
            _dayConfigArray.FirstOrDefault(d => d.GetDayId() == dayId)
            ?? throw new ArgumentException($"DayConfig not found: {dayId}");
    }
}