using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace OnlyFarms._Project.Scripts.DataAccess.Editor
{
    public static class ConfigIdDrawerHelper
    {
        public static void DrawPopup(Rect rect, SerializedProperty property, GUIContent label, List<string> names)
        {
            var current = property.stringValue;
            var foundIndex = names.IndexOf(current);
            var isMissing = !string.IsNullOrEmpty(current) && foundIndex < 0;

            var options = new List<string> { "(None)" };
            if (isMissing) options.Add($"⚠ {current} (missing)");
            options.AddRange(names);

            int selectedIndex;
            if (string.IsNullOrEmpty(current)) selectedIndex = 0;
            else if (isMissing) selectedIndex = 1;
            else selectedIndex = foundIndex + 1;

            var prev = GUI.backgroundColor;
            if (isMissing) GUI.backgroundColor = new Color(1f, 0.45f, 0.45f);

            var newIndex = EditorGUI.Popup(rect, label.text, selectedIndex, options.ToArray());

            GUI.backgroundColor = prev;

            if (newIndex == 0) property.stringValue = string.Empty;
            else if (isMissing && newIndex == 1) property.stringValue = current;
            else property.stringValue = names[isMissing ? newIndex - 2 : newIndex - 1];
        }
    }
}