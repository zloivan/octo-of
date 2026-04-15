using System;
using OnlyFarms.Domain;

namespace OnlyFarms.Presentation
{
    public class QuestEntryViewModel
    {
        public event Action OnProgressChanged;
        public event Action OnCompleted;

        private string _displayText;
        private string _objectiveText;
        private int _currentCount;
        private int _requiredCount;
        private bool _isCompleted;


        public QuestEntryViewModel(QuestInstance instance)
        {
            _displayText = instance.GetDefinition().GetDisplayName();
            RefreshFromInstance(instance);
        }

        private void RefreshFromInstance(QuestInstance instance)
        {
            var current = instance.GetCurrentObjective();
            if (current == null)
            {
                return;
            }

            _objectiveText = current.GetDefinition().DisplayText;
            _currentCount = current.GetCurrentCount();
            _requiredCount = current.GetDefinition().RequiredCount;
        }

        internal void NotifyProgress(QuestObjectiveInstance objective)
        {
            _currentCount = objective.GetCurrentCount();
            _requiredCount = objective.GetDefinition().RequiredCount;
            _objectiveText = objective.GetDefinition().DisplayText;

            OnProgressChanged?.Invoke();
        }

        internal void NotifyComplete()
        {
            _isCompleted = true;
            OnCompleted?.Invoke();
        }

        public string GetDisplayText() =>
            _displayText;

        public string GetObjectiveText() =>
            _objectiveText;

        public int GetCurrentCount() =>
            _currentCount;

        public int GetRequiredCount() =>
            _requiredCount;

        public bool GetIsCompleted() =>
            _isCompleted;
    }
}