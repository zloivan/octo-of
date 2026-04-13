#if UNITY_EDITOR
using System.Threading;
using Naninovel;
using OnlyFarms.DataAccess;
using OnlyFarms.Infrastructure;
using OnlyFarms.Infrastructure.Services;
using UnityEditor;
using UnityEngine;
using LocationService = OnlyFarms.Infrastructure.Services.LocationService;

namespace OnlyFarms._Project.Scripts.Tests.Editor
{
    public class LocationDebugPanel : EditorWindow
    {
        [MenuItem("OnlyFarms/Location Debug Panel")]
        public static void ShowWindow() =>
            GetWindow<LocationDebugPanel>("Location Debug");

        // ── Teleport state ──
        private string[] _locationIds = System.Array.Empty<string>();
        private int _selectedLocationIndex;

        // ── Flow state ──
        private string _returnScript = "";
        private string _returnLabel = "";

        // ── Scroll ──
        private Vector2 _scroll;

        private void OnEnable() => EditorApplication.playModeStateChanged += OnPlayModeChanged;
        private void OnDisable() => EditorApplication.playModeStateChanged -= OnPlayModeChanged;

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
                RefreshLocationList();
            Repaint();
        }

        private void RefreshLocationList()
        {
            var ids = GetLocationService()?.GetAllLocationIds();
            _locationIds = ids ?? System.Array.Empty<string>();
            _selectedLocationIndex = 0;
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
                if (GUILayout.Button("Refresh")) RefreshLocationList();
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawLocationSection();
            DrawSeparator();
            DrawHotspotSection();
            DrawSeparator();
            DrawConsumedSection();
            DrawSeparator();
            DrawHistorySection();
            DrawSeparator();
            DrawFlowSection();
            DrawSeparator();
            DrawStateSection();

            EditorGUILayout.EndScrollView();
        }

        // ═══════════════════════════════════════
        // TELEPORT
        // ═══════════════════════════════════════
        private void DrawLocationSection()
        {
            EditorGUILayout.LabelField("TELEPORT", EditorStyles.boldLabel);

            if (_locationIds.Length == 0)
            {
                if (GUILayout.Button("Load Locations")) RefreshLocationList();
                return;
            }

            _selectedLocationIndex = EditorGUILayout.Popup("Location", _selectedLocationIndex, _locationIds);

            if (GUILayout.Button("Enter"))
            {
                var id = _locationIds[_selectedLocationIndex];
                GetLocationService()?.Enter(id, CancellationToken.None);
            }
        }

        // ═══════════════════════════════════════
        // HOTSPOTS
        // ═══════════════════════════════════════
        private void DrawHotspotSection()
        {
            EditorGUILayout.LabelField("HOTSPOTS", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Force Show All"))
                GetLocationService()?.SetHotspotsVisible(true);
            if (GUILayout.Button("Force Hide All"))
                GetLocationService()?.SetHotspotsVisible(false);
            EditorGUILayout.EndHorizontal();
        }

        // ═══════════════════════════════════════
        // CONSUMED ITEMS
        // ═══════════════════════════════════════
        private void DrawConsumedSection()
        {
            EditorGUILayout.LabelField("CONSUMED ITEMS", EditorStyles.boldLabel);

            var consumed = GetLocationService()?.GetConsumedHotspotIds();

            if (consumed == null || consumed.Length == 0)
            {
                EditorGUILayout.LabelField("  (none)", EditorStyles.miniLabel);
            }
            else
            {
                foreach (var id in consumed)
                    EditorGUILayout.LabelField($"  • {id}", EditorStyles.miniLabel);
            }

            if (GUILayout.Button("Clear All Consumed"))
            {
                var svc = GetLocationService();
                if (svc == null) return;
                svc.ResetService();
                var currentId = svc.GetCurrentLocationId();
                if (!string.IsNullOrEmpty(currentId))
                    svc.Enter(currentId, CancellationToken.None);
            }
        }

        // ═══════════════════════════════════════
        // HISTORY
        // ═══════════════════════════════════════
        private void DrawHistorySection()
        {
            EditorGUILayout.LabelField("HISTORY", EditorStyles.boldLabel);

            var history = GetLocationService()?.PrintLocationHistory() ?? "(empty)";
            EditorGUILayout.LabelField(history, EditorStyles.wordWrappedMiniLabel);
        }

        // ═══════════════════════════════════════
        // FLOW
        // ═══════════════════════════════════════
        private void DrawFlowSection()
        {
            EditorGUILayout.LabelField("FLOW", EditorStyles.boldLabel);

            if (GUILayout.Button("Force Quests Complete"))
                Engine.GetService<QuestService>()?.ForceComplete();

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Set Return Point", EditorStyles.miniBoldLabel);
            _returnScript = EditorGUILayout.TextField("Script", _returnScript);
            _returnLabel = EditorGUILayout.TextField("Label (opt)", _returnLabel);

            if (GUILayout.Button("Set Return Point"))
            {
                var label = string.IsNullOrEmpty(_returnLabel) ? null : _returnLabel;
                Engine.GetService<GameFlowService>()?.SetSessionSource(
                    new HardcodedSessionsSource(_returnScript, label));
            }
        }

        // ═══════════════════════════════════════
        // STATE
        // ═══════════════════════════════════════
        private void DrawStateSection()
        {
            EditorGUILayout.LabelField("STATE", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset Service"))
                GetLocationService()?.ResetService();
            if (GUILayout.Button("Quick Save"))
                Engine.GetService<IStateManager>()?.QuickSave();
            if (GUILayout.Button("Quick Load"))
                Engine.GetService<IStateManager>()?.QuickLoad();
            EditorGUILayout.EndHorizontal();
        }

        // ═══════════════════════════════════════
        // Helpers
        // ═══════════════════════════════════════
        private static LocationService GetLocationService() =>
            Engine.GetService<LocationService>();

        private static void DrawSeparator()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.Space(2);
        }
    }
}
#endif