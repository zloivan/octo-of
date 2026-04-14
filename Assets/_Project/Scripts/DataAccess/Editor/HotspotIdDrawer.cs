using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(HotspotIdAttribute))]
public class HotspotIdDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (!LocationConfigCache.IsBuilt) LocationConfigCache.Refresh();

        EditorGUI.BeginProperty(position, label, property);

        var refreshWidth = 60f;
        var   popupRect    = new Rect(position.x, position.y, position.width - refreshWidth - 4f, position.height);
        var   refreshRect  = new Rect(position.xMax - refreshWidth, position.y, refreshWidth, position.height);

        ConfigIdDrawerHelper.DrawPopup(popupRect, property, label, LocationConfigCache.HotspotIds);

        if (GUI.Button(refreshRect, "Refresh")) LocationConfigCache.Refresh();

        EditorGUI.EndProperty();
    }
}