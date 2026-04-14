using System;
using System.Linq;
using OnlyFarms.Domain;
using OnlyFarms.Utilities;
using UnityEngine;

namespace OnlyFarms.DataAccess
{
    [CreateAssetMenu(fileName = "DayConfig", menuName = "Configs/Days/Day Config", order = 0)]
    public class DayConfigSO : ScriptableObject
    {
        [SerializeField] private string _dayId;
        [SerializeField] private QuestDefinitionSO[] _questsArray;
        [SerializeField] private NaniScriptReference _naniScriptReference;

        public string GetDayId() =>
            _dayId;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_dayId))
            {
                OFLogger.LogError($"{name}: DayId is empty.", this);
            }

            if (_questsArray == null || _questsArray.Length == 0)
            {
                OFLogger.LogWarning($"{name}: Quests is empty.", this);
            }

            if (string.IsNullOrWhiteSpace(_naniScriptReference.scriptName))
            {
                OFLogger.LogError($"{name}: ReturnScript is empty.", this);
            }
        }

        public QuestDefinition[] GetDayQuests() =>
            _questsArray.Select(qd => qd.ToDefinition()).ToArray();
        
        public string GetReturnScript() => _naniScriptReference.scriptName;
        public string GetReturnLabel()  => _naniScriptReference.labelName;
    }
}