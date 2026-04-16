using System.Collections.Generic;
using OnlyFarms.DataAccess;
using UnityEditor;

namespace OnlyFarms._Project.Scripts.DataAccess.Editor
{
    public static class LocationConfigCache
    {
        public static readonly List<string> LocationIds = new();
        public static readonly List<string> HotspotIds = new();
        public static bool IsBuilt = false;

        public static void Refresh()
        {
            LocationIds.Clear();
            HotspotIds.Clear();

            var guids = AssetDatabase.FindAssets("t:LocationConfigSO");
            if (guids.Length == 0)
            {
                IsBuilt = true;
                return;
            }

            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            var config = AssetDatabase.LoadAssetAtPath<LocationConfigSO>(path);
            if (config == null || config.Locations == null)
            {
                IsBuilt = true;
                return;
            }

            // Location IDs — поле публичное, читаем напрямую
            foreach (var loc in config.Locations)
            {
                if (!string.IsNullOrEmpty(loc.Id))
                    LocationIds.Add(loc.Id);
            }

            // Hotspot IDs — поле private, читаем через SerializedObject
            var so = new SerializedObject(config);
            var locationsProp = so.FindProperty("Locations");

            for (var i = 0; i < locationsProp.arraySize; i++)
            {
                var hotspotsProp = locationsProp
                    .GetArrayElementAtIndex(i)
                    .FindPropertyRelative("Hotspots");

                for (var j = 0; j < hotspotsProp.arraySize; j++)
                {
                    var idProp = hotspotsProp
                        .GetArrayElementAtIndex(j)
                        .FindPropertyRelative("_id");

                    if (!string.IsNullOrEmpty(idProp.stringValue))
                        HotspotIds.Add(idProp.stringValue);
                }
            }

            LocationIds.Sort();
            HotspotIds.Sort();
            IsBuilt = true;
        }
    }
}