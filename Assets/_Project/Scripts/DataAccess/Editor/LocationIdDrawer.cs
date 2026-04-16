using OnlyFarms.Attributes;
using UnityEditor;
using UnityEngine;

namespace OnlyFarms._Project.Scripts.DataAccess.Editor
{
    [CustomPropertyDrawer(typeof(LocationIdAttribute))]
    public class LocationIdDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!LocationConfigCache.IsBuilt) LocationConfigCache.Refresh();

            EditorGUI.BeginProperty(position, label, property);

            const float refreshWidth = 60f;
            var popupRect = new Rect(position.x, position.y, position.width - refreshWidth - 4f, position.height);
            var refreshRect = new Rect(position.xMax - refreshWidth, position.y, refreshWidth, position.height);

            ConfigIdDrawerHelper.DrawPopup(popupRect, property, label, LocationConfigCache.LocationIds);

            if (GUI.Button(refreshRect, "Refresh")) LocationConfigCache.Refresh();

            EditorGUI.EndProperty();
        }
    }
}