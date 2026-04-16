using System;
using System.Linq;

namespace OnlyFarms.Domain.Quests
{
    public class QuestInstance
    {
        private readonly QuestDefinition _definition;
        private readonly QuestObjectiveInstance[] _objectivesArray;

        public QuestInstance(QuestDefinition definition)
        {
            if (definition == null || definition.GetObjectives() == null)
            {
                throw new ArgumentException("QuestDefinition can't be null and it has to have Objectives");
            }

            _definition = definition;
            _objectivesArray = _definition.GetObjectives()
                .Select(d => new QuestObjectiveInstance(d))
                .ToArray();
        }

        public QuestObjectiveInstance TryReport(string eventId) =>
            _objectivesArray.FirstOrDefault(objective => objective.TryReport(eventId));

        public QuestDefinition GetDefinition() =>
            _definition;
        
        public QuestObjectiveInstance GetCurrentObjective() =>
            _objectivesArray.FirstOrDefault(o => !o.IsCompleted());

        public QuestObjectiveInstance[] GetObjectives() =>
            _objectivesArray;

        public bool IsCompleted() =>
            _objectivesArray.All(o => o.IsCompleted());
    }
}