using UnityEditor;
using UnityEngine;

namespace derHugo.ScriptTemplateEditor
{
    internal static class EditorGUILayoutExtensions
    {
        public static void HorizontalSeperator()
        {
            EditorGUILayout.LabelField(GUIContent.none, GUI.skin.horizontalSlider, GUILayout.Height(2f));
            EditorGUILayout.Space(EditorGUIUtility.singleLineHeight / 1.25f);
        }
    }
}
