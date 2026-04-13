namespace OnlyFarms.Domain
{
    public class QuestDefinition
    {
        private readonly string _displayName;
        private readonly QuestObjectiveDefinition[] _objectivesArray;
        private readonly bool _isSequential;

        public QuestDefinition(string displayName, QuestObjectiveDefinition[] objectivesArray, bool isSequential)
        {
            _displayName = displayName;
            _objectivesArray = objectivesArray;
            _isSequential = isSequential;
        }

        public string GetDisplayName() =>
            _displayName;

        public QuestObjectiveDefinition[] GetObjectives() =>
            _objectivesArray;

        public bool IsSequential() =>
            _isSequential;
    }
}