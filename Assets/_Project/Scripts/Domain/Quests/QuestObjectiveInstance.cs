using System;

namespace OnlyFarms.Domain
{
    public class QuestObjectiveInstance
    {
        private readonly QuestObjectiveDefinition _definition;
        private int _currentCount;

        public QuestObjectiveInstance(QuestObjectiveDefinition definition)
        {
            if (definition == null || definition.RequiredCount < 1)
            {
                throw new ArgumentException("Definition must be not null and RequiredCount must be >= 1");
            }

            _definition = definition;
        }

        public int GetCurrentCount => _currentCount;

        public bool IsCompleted() =>
            _currentCount >= _definition.RequiredCount;

        public QuestObjectiveDefinition GetDefinition() =>
            _definition;

        public bool TryReport(string eventId)
        {
            if (IsCompleted() || eventId != _definition.EventId)
            {
                return false;
            }

            _currentCount++;
            return true;
        }
    }
}