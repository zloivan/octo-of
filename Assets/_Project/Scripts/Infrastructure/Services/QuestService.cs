using System;
using System.Collections.Generic;
using System.Linq;
using Naninovel;
using OnlyFarms.DataAccess;
using OnlyFarms.Domain;
using OnlyFarms.Utilities;
using UnityEngine;

namespace OnlyFarms.Infrastructure.Services
{
    [InitializeAtRuntime]
    public class QuestService : IStatefulService<GameStateMap>, IQuestStatusSource, IQuestProgressReporter
    {
        public event Action<QuestInstance> OnQuestAdded;
        public event Action<QuestInstance, QuestObjectiveInstance> OnQuestObjectiveTicked;
        public event Action<QuestInstance> OnQuestCompleted;
        public event Func<UniTask> OnAllQuestsCompleted;

        private readonly IQuestRepository _questRepository;
        private QuestSession _session;
        private string _activeDayId;

        public QuestService(GameConfig questRepository) =>
            _questRepository = questRepository.QuestConfig;

        public void ActivateDaySession(string dayId)
        {
            var dayQuests = _questRepository.GetQuestsOfDay(dayId);
            if (dayQuests == null || dayQuests.Length == 0)
            {
                OFLogger.LogWarning($"No quests found for day {dayId}");
                return;
            }

            _activeDayId = dayId;
            _session = new QuestSession(dayQuests);

            foreach (var visibleQuest in _session.GetVisibleQuests())
            {
                OnQuestAdded?.Invoke(visibleQuest);
            }
        }

        public void ReportEvent(string eventId)
        {
            if (_session == null)
                return;

            var result = _session.ReportEvent(eventId);

            if (result.IsEmpty)
                return;

            OnQuestObjectiveTicked?.Invoke(result.Quest, result.Objective);

            if (result.Quest.IsCompleted())
                OnQuestCompleted?.Invoke(result.Quest);

            if (_session.AreAllCompleted())
                OnAllQuestsCompleted?.Invoke().Forget();
        }

        public UniTask InitializeService()
        {
            OFLogger.Log("<color=blue>Initialized</color>");
            return UniTask.CompletedTask;
        }

        public void ResetService()
        {
            _session = null;
            _activeDayId = null;
            OFLogger.Log("<color=yellow>Reset/color>");
        }

        public void DestroyService() =>
            OFLogger.Log("<color=red>Destroyed");

        public void SaveServiceState(GameStateMap stateMap)
        {
            if (_session == null || string.IsNullOrEmpty(_activeDayId))
                return;

            stateMap.SetState(new QuestServiceState
            {
                Snapshot = _session.GetSnapshot(),
                DayId = _activeDayId,
            });

            OFLogger.Log("QuestService state saved");
        }

        public UniTask LoadServiceState(GameStateMap stateMap)
        {
            var state = stateMap.GetState<QuestServiceState>();
            if (state?.Snapshot == null)
            {
                OFLogger.LogWarning("Could not load state.");
                return UniTask.CompletedTask;
            }

            ActivateDaySession(state.DayId);
            _session.LoadSnapshot(state.Snapshot);

            foreach (var quest in _session.GetAllQuestsList())
                if (quest.IsCompleted())
                    OnQuestCompleted?.Invoke(quest);
            
            OFLogger.Log("QuestService state loaded");

            return UniTask.CompletedTask;
        }

        public bool IsQuestCompleted(string questId)
        {
            if (_session == null)
            {
                OFLogger.Log("Session is not initialized");
                return false;
            }

            var isCompleted = _session.GetAllQuestsList()
                .FirstOrDefault(q => q.GetDefinition().GetDisplayName() == questId)
                ?.IsCompleted() ?? false;

            OFLogger.Log($"Quest {questId} completed: {isCompleted}");
            return isCompleted;
        }

        public IReadOnlyList<QuestInstance> GetVisibleQuests()
        {
            var result = _session?.GetVisibleQuests() ?? Array.Empty<QuestInstance>();

            // OFLogger.Log($"Visible Quests Count: {result.Count}");
            return result;
        }

        public void ForceComplete()
        {
            OFLogger.Log("Force Quest Complete is called...");
            if (_session == null)
                return;

            foreach (var quest in _session.GetAllQuestsList())
            {
                foreach (var objective in quest.GetObjectives())
                {
                    while (!objective.IsCompleted())
                    {
                        objective.TryReport(objective.GetDefinition().EventId);
                    }
                }
            }

            foreach (var quest in _session.GetAllQuestsList())
                OnQuestCompleted?.Invoke(quest);

            if (_session.AreAllCompleted())
                OnAllQuestsCompleted?.Invoke().Forget();
        }
    }

    [Serializable]
    public class QuestServiceState
    {
        public QuestSessionSnapshot Snapshot;
        public string DayId;
    }
}