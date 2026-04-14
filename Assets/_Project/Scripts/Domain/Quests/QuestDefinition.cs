namespace OnlyFarms.Domain
{
    public class QuestDefinition
    {
        private readonly string _displayName;
        private readonly QuestObjectiveDefinition[] _objectivesArray;

        public QuestDefinition(string displayName, QuestObjectiveDefinition[] objectivesArray)
        {
            _displayName = displayName;
            _objectivesArray = objectivesArray;
        }

        public string GetDisplayName() =>
            _displayName;

        public QuestObjectiveDefinition[] GetObjectives() =>
            _objectivesArray;

       
    }
}