using System.Collections.Generic;
using UnityEngine;

namespace OnlyFarms.DataAccess
{
    [CreateAssetMenu(fileName = "QuestConfig", menuName = "Configs/Quests/QuestConfig", order = 0)]
    public class QuestConfigSO : ScriptableObject, IQuestRepository
    {
        [SerializeField] private List<DayConfigSO> _dayConfigList;

        //TODO: Implement
        public DayConfigSO GetDayConfig(string dayId) =>
            _dayConfigList[0];
    }
}