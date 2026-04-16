using System;
using System.Collections.Generic;
using OnlyFarms.Domain;
using OnlyFarms.Utilities;
using UnityEngine;

namespace OnlyFarms.DataAccess
{
    [CreateAssetMenu(fileName = "QuestDefinition", menuName = "Configs/Quests/Quest Definition", order = 0)]
    public class QuestDefinitionSO : ScriptableObject
    {
        [SerializeField] private string _displayName;
        [SerializeField] private QuestObjectiveDefinition[] _objectivesList;

        public QuestDefinition ToDefinition() =>
            new(_displayName, _objectivesList);

        private void OnValidate()
        {
            if (_objectivesList == null || _objectivesList.Length == 0)
            {
                OFLogger.LogWarning($"{name}: Objectives is empty.", this);
                return;
            }

            foreach (var obj in _objectivesList)
            {
                if (obj.RequiredCount < 1)
                    OFLogger.LogError($"{name}: RequiredCount must be >= 1", this);

                if (string.IsNullOrEmpty(obj.EventId))
                    OFLogger.LogError($"{name}: EventId cannot be empty", this);
            }

#if UNITY_EDITOR
            ValidateUniqueEventIds();
#endif
        }

#if UNITY_EDITOR
        private void ValidateUniqueEventIds()
        {
            var selfPath = UnityEditor.AssetDatabase.GetAssetPath(this);
            var guids = UnityEditor.AssetDatabase.FindAssets("t:QuestDefinitionSO");
            var seen = new Dictionary<string, string>();

            // Pre-populate from this (in-memory, always fresh)
            if (_objectivesList != null)
                foreach (var obj in _objectivesList)
                    if (!string.IsNullOrEmpty(obj.EventId))
                        seen[obj.EventId] = name;

            foreach (var guid in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                if (path == selfPath) continue; // skip self by path — reference equality is unreliable in OnValidate

                var so = UnityEditor.AssetDatabase.LoadAssetAtPath<QuestDefinitionSO>(path);
                if (so?._objectivesList == null) continue;

                foreach (var obj in so._objectivesList)
                {
                    if (string.IsNullOrEmpty(obj.EventId)) continue;

                    if (seen.TryGetValue(obj.EventId, out var existingAsset))
                        OFLogger.LogError(
                            $"Duplicate EventId '{obj.EventId}': found in '{so.name}' and '{existingAsset}'.", so);
                    else
                        seen[obj.EventId] = so.name;
                }
            }
        }
#endif
    }
}