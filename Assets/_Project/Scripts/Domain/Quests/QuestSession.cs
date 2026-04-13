using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OnlyFarms.Domain
{
    public class QuestSession
    {
        private readonly List<QuestInstance> _questsList;
        private readonly bool _isSequential;
        private int _visibleCount;

        public QuestSession(IEnumerable<QuestDefinition> definitions)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions), "Quest definitions cannot be null.");
            }

            _questsList = definitions.Select(d => new QuestInstance(d)).ToList();
            _isSequential = _questsList.Count > 0 && _questsList[0].GetDefinition().IsSequential();
            _visibleCount = _isSequential ? Math.Min(1, _questsList.Count) : _questsList.Count;
        }

        public QuestEventResult ReportEvent(string eventId)
        {
            foreach (var quest in GetVisibleQuests())
            {
                var objective = quest.TryReport(eventId);
                
                if (objective == null) 
                    continue;
                
                if (!quest.IsCompleted()) 
                    continue;
                
                AdvanceVisibility();
                return new QuestEventResult(quest, objective);
            }

            return QuestEventResult.None;
        } 

        public IReadOnlyList<QuestInstance> GetAllQuestsList() =>
            _questsList;

        public IReadOnlyList<QuestInstance> GetVisibleQuests() =>
            _questsList.Take(_visibleCount).ToList();

        public bool AreAllCompleted() =>
            _questsList.All(q => q.IsCompleted());

        private void AdvanceVisibility()
        {
            if (_isSequential && _visibleCount < _questsList.Count)
                _visibleCount++;
        }

        public QuestSessionSnapshot GetSnapshot()
        {
            var progress = new List<QuestObjectiveProgress>();
            for (var qi = 0; qi < _questsList.Count; qi++)
            {
                for (var oi = 0; oi < _questsList[qi].GetObjectives().Length; oi++)
                {
                    if (_questsList[qi].GetObjectives()[oi].GetCurrentCount() > 0)
                    {
                        progress.Add(new QuestObjectiveProgress
                        {
                            QuestIndex = qi,
                            ObjectiveIndex = oi,
                            CurrentCount = _questsList[qi].GetObjectives()[oi].GetCurrentCount(),
                        });
                    }
                }
            }

            return new QuestSessionSnapshot
            {
                ObjectiveProgress = progress.ToArray(),
                VisibleCount = _visibleCount,
            };
        }

        public void LoadSnapshot(QuestSessionSnapshot snapshot)
        {
            foreach (var p in snapshot.ObjectiveProgress)
            {
                if (p.QuestIndex >= _questsList.Count)
                {
                    continue;
                }

                if (p.ObjectiveIndex >= _questsList[p.QuestIndex].GetObjectives().Length)
                {
                    continue;
                }

                var obj = _questsList[p.QuestIndex].GetObjectives()[p.ObjectiveIndex];
                for (var i = 0; i < p.CurrentCount; i++)
                {
                    obj.TryReport(obj.GetDefinition().EventId);
                }
            }

            _visibleCount = Mathf.Clamp(snapshot.VisibleCount, 0, _questsList.Count);
        }
    }
}