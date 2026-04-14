// Assets/_Project/Scripts/Tests/Editor/QuestDebugPanel.cs
#if UNITY_EDITOR
using System.Collections.Generic;
using Naninovel;
using OnlyFarms.DataAccess;
using OnlyFarms.Domain;
using OnlyFarms.Infrastructure.Services;
using UnityEditor;
using UnityEngine;

namespace OnlyFarms._Project.Scripts.Tests.Editor
{
    public class QuestDebugPanel : EditorWindow
    {
        [MenuItem("OnlyFarms/Quest Debug Panel")]
        public static void ShowWindow() =>
            GetWindow<QuestDebugPanel>("Quest Debug");

        private string[]          _dayIds      = System.Array.Empty<string>();
        private int               _selectedDay = 0;
        private string            _eventId     = "";
        private Vector2           _scroll;
        private readonly List<string> _log     = new List<string>();
        private bool              _subscribed  = false;

        private void OnEnable() =>
            EditorApplication.playModeStateChanged += OnPlayModeChanged;

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            Unsubscribe();
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                _log.Clear();
                RefreshDayIds();
                Subscribe();
            }

            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                Unsubscribe();
                _log.Clear();
            }

            Repaint();
        }

        private void OnGUI()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to use the debug panel.", MessageType.Info);
                return;
            }

            if (!Engine.Initialized)
            {
                EditorGUILayout.HelpBox("Waiting for Naninovel to initialize...", MessageType.Warning);
                if (GUILayout.Button("Refresh")) RefreshDayIds();
                return;
            }

            if (!_subscribed) Subscribe();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawActivateSection();
            DrawSeparator();
            DrawQuestStateSection();
            DrawSeparator();
            DrawReportSection();
            DrawSeparator();
            DrawSaveLoadSection();
            DrawSeparator();
            DrawLogSection();
            EditorGUILayout.EndScrollView();
        }

        // ═══════════════════════════════════
        // AC#1 — ActivateDaySession
        // ═══════════════════════════════════
        private void DrawActivateSection()
        {
            EditorGUILayout.LabelField("AC#1 — ACTIVATE", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "После Activate → в логе OnQuestAdded для каждого квеста дня.",
                MessageType.None);

            if (_dayIds.Length == 0)
            {
                if (GUILayout.Button("Load Day IDs")) RefreshDayIds();
                return;
            }

            _selectedDay = EditorGUILayout.Popup("Day", _selectedDay, _dayIds);

            if (GUILayout.Button("Activate Day Session"))
            {
                _log.Clear();
                GetQuestService()?.ActivateDaySession(_dayIds[_selectedDay]);
            }
        }

        // ═══════════════════════════════════
        // Текущее состояние квестов
        // ═══════════════════════════════════
        private void DrawQuestStateSection()
        {
            EditorGUILayout.LabelField("QUEST STATE", EditorStyles.boldLabel);

            var svc = GetQuestService();
            var visible = svc?.GetVisibleQuests();

            if (visible == null || visible.Count == 0)
            {
                EditorGUILayout.LabelField("  (no active quests)", EditorStyles.miniLabel);
                return;
            }

            foreach (var quest in visible)
            {
                var current = quest.GetCurrentObjective();
                var questDone = quest.IsCompleted();
                var icon = questDone ? "✅" : "⏳";

                EditorGUILayout.LabelField(
                    $"{icon} {quest.GetDefinition().GetDisplayName()}",
                    EditorStyles.miniBoldLabel);

                if (current != null)
                {
                    var count = current.GetCurrentCount();
                    var required = current.GetDefinition().RequiredCount;
                    var counter = required > 1 ? $" ({count}/{required})" : "";

                    EditorGUILayout.LabelField(
                        $"      → {current.GetDefinition().DisplayText}{counter}",
                        EditorStyles.miniLabel);
                }
                else
                {
                    EditorGUILayout.LabelField(
                        "      → (все objectives выполнены)",
                        EditorStyles.miniLabel);
                }

                // Все objectives под квестом (сворачиваемые)
                foreach (var obj in quest.GetObjectives())
                {
                    var done = obj.IsCompleted() ? "☑" : "☐";
                    EditorGUILayout.LabelField(
                        $"         {done} [{obj.GetCurrentCount()}/{obj.GetDefinition().RequiredCount}] {obj.GetDefinition().EventId}",
                        EditorStyles.miniLabel);
                }
            }
        }

        // ═══════════════════════════════════
        // AC#2 + AC#3 — ReportEvent
        // ═══════════════════════════════════
        private void DrawReportSection()
        {
            EditorGUILayout.LabelField("AC#2/3 — REPORT EVENT", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "AC#2: введи EventId → OnObjectiveTicked, затем OnQuestCompleted если квест завершён.\n" +
                "AC#3: завершение последнего квеста → OnAllQuestsCompleted.",
                MessageType.None);

            _eventId = EditorGUILayout.TextField("Event ID", _eventId);

            EditorGUI.BeginDisabledGroup(string.IsNullOrWhiteSpace(_eventId));
            if (GUILayout.Button("Report Event"))
                GetQuestService()?.ReportEvent(_eventId);
            EditorGUI.EndDisabledGroup();
        }

        // ═══════════════════════════════════
        // AC#5 — Save / Load
        // ═══════════════════════════════════
        private void DrawSaveLoadSection()
        {
            EditorGUILayout.LabelField("AC#5 — SAVE / LOAD", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "1. Активируй сессию, отрепортируй несколько событий.\n" +
                "2. Quick Save.\n" +
                "3. Reset → Quick Load.\n" +
                "4. Quest State должен восстановить тот же прогресс.",
                MessageType.None);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Quick Save"))
                Engine.GetService<IStateManager>()?.QuickSave();

            if (GUILayout.Button("Quick Load"))
            {
                _log.Clear();
                _log.Add("── Quick Load ──");
                Engine.GetService<IStateManager>()?.QuickLoad();
            }

            if (GUILayout.Button("Reset"))
            {
                GetQuestService()?.ResetService();
                _log.Add("[Reset]");
            }

            EditorGUILayout.EndHorizontal();
        }

        // ═══════════════════════════════════
        // Event Log
        // ═══════════════════════════════════
        private void DrawLogSection()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("EVENT LOG", EditorStyles.boldLabel);
            if (GUILayout.Button("Clear", GUILayout.Width(50))) _log.Clear();
            EditorGUILayout.EndHorizontal();

            if (_log.Count == 0)
            {
                EditorGUILayout.LabelField("  (no events yet)", EditorStyles.miniLabel);
                return;
            }

            foreach (var entry in _log)
                EditorGUILayout.LabelField(entry, EditorStyles.miniLabel);
        }

        // ═══════════════════════════════════
        // Subscribe / Unsubscribe
        // ═══════════════════════════════════
        private void Subscribe()
        {
            var svc = GetQuestService();
            if (svc == null) return;

            svc.OnQuestAdded           += OnQuestAdded;
            svc.OnQuestObjectiveTicked += OnObjectiveTicked;
            svc.OnQuestCompleted       += OnQuestCompleted;
            svc.OnAllQuestsCompleted   += OnAllQuestsCompleted;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            var svc = GetQuestService();
            if (svc == null) { _subscribed = false; return; }

            svc.OnQuestAdded           -= OnQuestAdded;
            svc.OnQuestObjectiveTicked -= OnObjectiveTicked;
            svc.OnQuestCompleted       -= OnQuestCompleted;
            svc.OnAllQuestsCompleted   -= OnAllQuestsCompleted;
            _subscribed = false;
        }

        private void OnQuestAdded(QuestInstance q)
        {
            var obj = q.GetCurrentObjective();
            var text = obj?.GetDefinition().DisplayText ?? "(no objective)";
            _log.Add($"[OnQuestAdded] {q.GetDefinition().GetDisplayName()} → \"{text}\"");
            Repaint();
        }

        private void OnObjectiveTicked(QuestInstance q, QuestObjectiveInstance obj)
        {
            var count    = obj.GetCurrentCount();
            var required = obj.GetDefinition().RequiredCount;
            var next     = q.GetCurrentObjective();
            var nextText = next != null ? $" | next: \"{next.GetDefinition().DisplayText}\"" : "";

            _log.Add($"[OnObjectiveTicked] {q.GetDefinition().GetDisplayName()} [{count}/{required}]{nextText}");
            Repaint();
        }

        private void OnQuestCompleted(QuestInstance q)
        {
            _log.Add($"[OnQuestCompleted] {q.GetDefinition().GetDisplayName()}");
            Repaint();
        }

        private UniTask OnAllQuestsCompleted()
        {
            _log.Add("[OnAllQuestsCompleted] ✅");
            Repaint();
            return UniTask.CompletedTask;
        }

        // ═══════════════════════════════════
        // Helpers
        // ═══════════════════════════════════
        private void RefreshDayIds()
        {
            var ids   = new List<string>();
            var guids = AssetDatabase.FindAssets("t:DayConfigSO");
            foreach (var guid in guids)
            {
                var path   = AssetDatabase.GUIDToAssetPath(guid);
                var config = AssetDatabase.LoadAssetAtPath<DayConfigSO>(path);
                if (config != null && !string.IsNullOrEmpty(config.GetDayId()))
                    ids.Add(config.GetDayId());
            }
            _dayIds = ids.ToArray();
        }

        private static QuestService GetQuestService() =>
            Engine.GetService<QuestService>();

        private static void DrawSeparator()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.Space(2);
        }
    }
}
#endif