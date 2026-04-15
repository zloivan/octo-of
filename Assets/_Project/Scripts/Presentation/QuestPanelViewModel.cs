using System;
using System.Collections.Generic;
using Naninovel;
using OnlyFarms.Domain;
using OnlyFarms.Infrastructure.Services;
using OnlyFarms.Utilities;

namespace OnlyFarms.Presentation
{
    [InitializeAtRuntime]
    public class QuestPanelViewModel : IEngineService
    {
        public event Action<QuestEntryViewModel> OnQuestAdded;
        public event Action<QuestEntryViewModel> OnQuestRemoved;
        public event Action OnAllQuestsCompleted;
        public event Action OnSessionActivated;

        private readonly QuestService _questService;
        private readonly List<QuestEntryViewModel> _activeQuests = new();
        private readonly Dictionary<QuestInstance, QuestEntryViewModel> _vmMap = new();

        public QuestPanelViewModel(QuestService questService) =>
            _questService = questService;

        public UniTask InitializeService()
        {
            _questService.OnQuestAdded += QuestService_OnQuestAdded;
            _questService.OnQuestObjectiveTicked += QuestService_OnQuestObjectiveTicked;
            _questService.OnQuestCompleted += QuestService_OnQuestCompleted;
            _questService.OnAllQuestsCompleted += QuestService_OnAllQuestsCompleted;

            OFLogger.Log("<color=blue>Initialized</color>");
            return UniTask.CompletedTask;
        }

        public void DestroyService()
        {
            _questService.OnQuestAdded -= QuestService_OnQuestAdded;
            _questService.OnQuestObjectiveTicked -= QuestService_OnQuestObjectiveTicked;
            _questService.OnQuestCompleted -= QuestService_OnQuestCompleted;
            _questService.OnAllQuestsCompleted -= QuestService_OnAllQuestsCompleted;

            OFLogger.Log("<color=red>Destroyed</color>");
        }

        public void ResetService()
        {
            _activeQuests.Clear();
            _vmMap.Clear();
        }

        private void QuestService_OnQuestAdded(QuestInstance quest)
        {
            var vm = new QuestEntryViewModel(quest);
            _activeQuests.Add(vm);
            _vmMap[quest] = vm;

            if (_activeQuests.Count == 1)
                OnSessionActivated?.Invoke();

            OnQuestAdded?.Invoke(vm);
        }

        private void QuestService_OnQuestObjectiveTicked(QuestInstance quest, QuestObjectiveInstance obj)
        {
            if (!_vmMap.TryGetValue(quest, out var vm))
                return;

            vm.NotifyProgress(obj);
        }

        private void QuestService_OnQuestCompleted(QuestInstance quest)
        {
            if (!_vmMap.TryGetValue(quest, out var vm))
                return;

            vm.NotifyComplete();
            _activeQuests.Remove(vm);
            _vmMap.Remove(quest);
            OnQuestRemoved?.Invoke(vm);
        }

        private UniTask QuestService_OnAllQuestsCompleted()
        {
            OnAllQuestsCompleted?.Invoke();
            return UniTask.CompletedTask;
        }

        public IReadOnlyList<QuestEntryViewModel> GetActiveQuests() =>
            _activeQuests;
    }
}