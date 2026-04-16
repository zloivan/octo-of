// ── DataAccess/Editor/QuestEventIdDrawer.cs ───────────────────────────────────

using OnlyFarms.Attributes;
using UnityEditor;
using UnityEngine;

namespace OnlyFarms._Project.Scripts.DataAccess.Editor
{
    [CustomPropertyDrawer(typeof(QuestEventIdAttribute))]
    public class QuestEventIdDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!QuestConfigCache.IsBuilt) QuestConfigCache.Refresh();

            EditorGUI.BeginProperty(position, label, property);

            const float refreshWidth = 60f;
            var popupRect  = new Rect(position.x,              position.y, position.width - refreshWidth - 4f, position.height);
            var refreshRect = new Rect(position.xMax - refreshWidth, position.y, refreshWidth,                 position.height);

            ConfigIdDrawerHelper.DrawPopup(popupRect, property, label, QuestConfigCache.EventIds);

            if (GUI.Button(refreshRect, "Refresh")) QuestConfigCache.Refresh();

            EditorGUI.EndProperty();
        }
    }
}