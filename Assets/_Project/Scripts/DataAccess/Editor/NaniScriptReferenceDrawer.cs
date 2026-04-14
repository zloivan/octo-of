using System.Collections.Generic;
using System.IO;
using OnlyFarms.DataAccess;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(NaniScriptReference))]
public class NaniScriptReferenceDrawer : PropertyDrawer
{
    private static readonly List<string> _scriptNames = new();
    private static readonly Dictionary<string, List<string>> _labelCache = new();
    private static bool _cacheBuilt;

    private const float REFRESH_WIDTH = 60f;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
        EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (!_cacheBuilt) RefreshCache();

        var scriptProp = property.FindPropertyRelative("scriptName");
        var labelProp = property.FindPropertyRelative("labelName");

        var lineH = EditorGUIUtility.singleLineHeight;
        var spacing = EditorGUIUtility.standardVerticalSpacing;

        var row1 = new Rect(position.x, position.y, position.width, lineH);
        var row2 = new Rect(position.x, position.y + lineH + spacing, position.width, lineH);

        EditorGUI.BeginProperty(position, label, property);

        var scriptRect = new Rect(row1.x, row1.y, row1.width - REFRESH_WIDTH - 4f, row1.height);
        var refreshRect = new Rect(row1.xMax - REFRESH_WIDTH, row1.y, REFRESH_WIDTH, row1.height);

        DrawScriptPopup(scriptRect, scriptProp, labelProp);
        if (GUI.Button(refreshRect, "Refresh")) RefreshCache();

        DrawLabelPopup(row2, scriptProp, labelProp);

        EditorGUI.EndProperty();
    }

    // ── Script row ────────────────────────────────────────────────────────────

    private void DrawScriptPopup(Rect rect, SerializedProperty scriptProp, SerializedProperty labelProp)
    {
        var current = scriptProp.stringValue;
        var foundIndex = _scriptNames.IndexOf(current);
        var isMissing = !string.IsNullOrEmpty(current) && foundIndex < 0;

        var options = BuildOptions(_scriptNames, current, isMissing);

        var selectedIndex = ResolveIndex(current, foundIndex, isMissing);

        var prev = GUI.backgroundColor;
        if (isMissing) GUI.backgroundColor = new Color(1f, 0.45f, 0.45f);
        var newIndex = EditorGUI.Popup(rect, "Script", selectedIndex, options.ToArray());
        GUI.backgroundColor = prev;

        var newScript = MapSelection(newIndex, current, isMissing, _scriptNames);

        if (newScript != scriptProp.stringValue)
        {
            scriptProp.stringValue = newScript;
            labelProp.stringValue = string.Empty; // сброс лейбла при смене скрипта
        }
    }

    // ── Label row ─────────────────────────────────────────────────────────────

    private void DrawLabelPopup(Rect rect, SerializedProperty scriptProp, SerializedProperty labelProp)
    {
        var scriptName = scriptProp.stringValue;
        var hasScript = !string.IsNullOrEmpty(scriptName) && _scriptNames.Contains(scriptName);

        EditorGUI.BeginDisabledGroup(!hasScript);

        if (!hasScript)
        {
            EditorGUI.Popup(rect, "Label", 0, new[] { "(Select script first)" });
            EditorGUI.EndDisabledGroup();
            return;
        }

        var labels = GetLabels(scriptName);

        if (labels.Count == 0)
        {
            EditorGUI.Popup(rect, "Label", 0, new[] { "(No labels in script)" });
            EditorGUI.EndDisabledGroup();
            return;
        }

        var current = labelProp.stringValue;
        var foundIndex = labels.IndexOf(current);
        var isMissing = !string.IsNullOrEmpty(current) && foundIndex < 0;

        var options = BuildOptions(labels, current, isMissing);

        var selectedIndex = ResolveIndex(current, foundIndex, isMissing);

        var prev = GUI.backgroundColor;
        if (isMissing) GUI.backgroundColor = new Color(1f, 0.45f, 0.45f);
        var newIndex = EditorGUI.Popup(rect, "Label", selectedIndex, options.ToArray());
        GUI.backgroundColor = prev;

        labelProp.stringValue = MapSelection(newIndex, current, isMissing, labels);

        EditorGUI.EndDisabledGroup();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static List<string> BuildOptions(List<string> names, string current, bool isMissing)
    {
        var options = new List<string> { "(None)" };
        if (isMissing) options.Add($"⚠ {current} (missing)");
        options.AddRange(names);
        return options;
    }

    private static int ResolveIndex(string current, int foundIndex, bool isMissing)
    {
        if (string.IsNullOrEmpty(current)) return 0;
        if (isMissing) return 1;
        return foundIndex + 1;
    }

    private static string MapSelection(int newIndex, string current, bool isMissing, List<string> names)
    {
        if (newIndex == 0) return string.Empty;
        if (isMissing && newIndex == 1) return current;
        return names[isMissing ? newIndex - 2 : newIndex - 1];
    }

    // ── Cache ─────────────────────────────────────────────────────────────────

    private static List<string> GetLabels(string scriptName)
    {
        if (_labelCache.TryGetValue(scriptName, out var cached)) return cached;

        var result = new List<string>();
        _labelCache[scriptName] = result;

        foreach (var path in AssetDatabase.GetAllAssetPaths())
        {
            if (Path.GetExtension(path) != ".nani") continue;
            if (Path.GetFileNameWithoutExtension(path) != scriptName) continue;

            foreach (var line in File.ReadAllLines(path))
            {
                var trimmed = line.TrimStart();
                if (!trimmed.StartsWith("#")) continue;

                var labelName = trimmed.Substring(1).Trim();
                if (!string.IsNullOrEmpty(labelName))
                    result.Add(labelName);
            }

            break;
        }

        return result;
    }

    private static void RefreshCache()
    {
        _scriptNames.Clear();
        _labelCache.Clear();

        foreach (var path in AssetDatabase.GetAllAssetPaths())
        {
            if (Path.GetExtension(path) == ".nani")
                _scriptNames.Add(Path.GetFileNameWithoutExtension(path));
        }

        _scriptNames.Sort();
        _cacheBuilt = true;
    }
}