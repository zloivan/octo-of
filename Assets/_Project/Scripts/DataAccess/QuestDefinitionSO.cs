using System;
using OnlyFarms.Domain;
using OnlyFarms.Utilities;
using UnityEngine;

namespace OnlyFarms.DataAccess
{
    [CreateAssetMenu(fileName = "QuestDefinition", menuName = "Configs/Quests/Quest Definition", order = 0)]
    public class QuestDefinitionSO : ScriptableObject
    {
        [SerializeField] private string _displayName;
        [SerializeField] private QuestObjectiveDefinition[] _objectivesList;
        [SerializeField] private bool _isSequential;

        public QuestDefinition ToDefinition() =>
            new(_displayName, _objectivesList, _isSequential);

        private void OnValidate()
        {
            if (_objectivesList == null || _objectivesList.Length == 0)
            {
                OFLogger.LogWarning($"{name}: Objectives is empty.", this);

                foreach (var obj in _objectivesList ?? Array.Empty<QuestObjectiveDefinition>())
                {
                    if (obj.RequiredCount < 1)
                    {
                        OFLogger.LogError($"{name}: RequiredCount must be >= 1", this);
                    }
                }
            }
        }
    }
}