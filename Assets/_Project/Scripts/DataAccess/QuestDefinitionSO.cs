using OnlyFarms.Domain;
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
    }
}