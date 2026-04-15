using System;
using System.Collections.Generic;
using Naninovel;
using Naninovel.UI;
using OnlyFarms.Presentation;
using UnityEngine;

namespace OnlyFarms.UI
{
    public class QuestPanelUI : CustomUI
    {
        [SerializeField] private Transform _entriesContainer;
        [SerializeField] private QuestEntryView _entryPrefab;

        private QuestPanelViewModel _viewModel;

        private readonly Dictionary<QuestEntryViewModel, QuestEntryView> _entries = new();

        public override UniTask Initialize()
        {
            _viewModel = Engine.GetService<QuestPanelViewModel>();

            if (_viewModel == null)
                throw new NullReferenceException("View model is not provided");
            
            _viewModel.OnSessionActivated += Show;
            _viewModel.OnQuestAdded += AddEntry;
            _viewModel.OnQuestRemoved += RemoveEntry;
            _viewModel.OnAllQuestsCompleted += Hide;
            _viewModel.OnSessionActivated += ClearEntries;
            
            foreach (var vm in _viewModel.GetActiveQuests())
                AddEntry(vm);
            
            Hide();
            
            return UniTask.CompletedTask;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (_viewModel == null)
                return;

            _viewModel.OnSessionActivated -= Show;
            _viewModel.OnQuestAdded -= AddEntry;
            _viewModel.OnQuestRemoved -= RemoveEntry;
            _viewModel.OnAllQuestsCompleted -= Hide;
            _viewModel.OnSessionActivated -= ClearEntries;
        }

        private void AddEntry(QuestEntryViewModel vm)
        {
            if (_entries.ContainsKey(vm)) 
                return;

            var entry = Instantiate(_entryPrefab, _entriesContainer);
            entry.Bind(vm);
            _entries[vm] = entry;
        }

        private void RemoveEntry(QuestEntryViewModel vm)
        {
            if (!_entries.Remove(vm, out var entry)) 
                return;
            RemoveEntryAsync(entry).Forget();
        }

        private async UniTaskVoid RemoveEntryAsync(QuestEntryView entry)
        {
            await entry.PlayFadeOutAsync(destroyCancellationToken);
            
            if (entry != null) 
                entry.SelfDestroy();
        }
        
        private void ClearEntries()
        {
            foreach (var entry in _entries.Values)
                Destroy(entry.gameObject);

            _entries.Clear();
        }
    }
}