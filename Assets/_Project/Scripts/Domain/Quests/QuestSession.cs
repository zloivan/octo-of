using System;
using System.Collections.Generic;
using System.Linq;

namespace OnlyFarms.Domain.Quests
{
    public class QuestSession
    {
        private readonly List<QuestInstance> _questsList;

        public QuestSession(IEnumerable<QuestDefinition> definitions)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions), "Quest definitions cannot be null.");
            }

            _questsList = definitions.Select(d => new QuestInstance(d)).ToList();
        }

        public QuestEventResult ReportEvent(string eventId)
        {
            foreach (var quest in GetVisibleQuests())
            {
                var objective = quest.TryReport(eventId);
                
                if (objective == null) 
                    continue;
                
                return new QuestEventResult(quest, objective);
            }

            return QuestEventResult.None;
        } 

        public IReadOnlyList<QuestInstance> GetAllQuestsList() =>
            _questsList;
        
        public IReadOnlyList<QuestInstance> GetVisibleQuests() =>
            GetAllQuestsList();

        public bool AreAllCompleted() =>
            _questsList.All(q => q.IsCompleted());

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
            };
        }

        public void LoadSnapshot(QuestSessionSnapshot snapshot)
        {
            foreach (var p in snapshot.ObjectiveProgress)
            {
                if (p.QuestIndex >= _questsList.Count)
                    continue;

                if (p.ObjectiveIndex >= _questsList[p.QuestIndex].GetObjectives().Length)
                    continue;

                var obj = _questsList[p.QuestIndex].GetObjectives()[p.ObjectiveIndex];
                
                for (var i = 0; i < p.CurrentCount; i++) 
                    obj.TryReport(obj.GetDefinition().EventId);
            }
        }
    }
}