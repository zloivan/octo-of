using System.Collections.Generic;
using OnlyFarms.DataAccess;
using UnityEditor;

public static class QuestConfigCache
{
    public static readonly List<string> EventIds = new();
    public static bool IsBuilt = false;

    public static void Refresh()
    {
        EventIds.Clear();
        var seen = new HashSet<string>();

        // Читаем все QuestDefinitionSO в проекте — не важно к какому дню привязаны
        var guids = AssetDatabase.FindAssets("t:QuestDefinitionSO");
        foreach (var guid in guids)
        {
            var path  = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<QuestDefinitionSO>(path);
            if (asset == null) continue;

            var so         = new SerializedObject(asset);
            var objectives = so.FindProperty("_objectivesList");
            if (objectives == null) continue;

            for (var i = 0; i < objectives.arraySize; i++)
            {
                var eventIdProp = objectives
                    .GetArrayElementAtIndex(i)
                    .FindPropertyRelative("EventId");

                if (eventIdProp == null || string.IsNullOrEmpty(eventIdProp.stringValue))
                    continue;

                if (seen.Add(eventIdProp.stringValue))
                    EventIds.Add(eventIdProp.stringValue);
            }
        }

        EventIds.Sort();
        IsBuilt = true;
    }
}


