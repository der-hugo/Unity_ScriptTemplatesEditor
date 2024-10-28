using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace derHugo.ScriptTemplateEditor
{
    internal partial class ScriptTemplateSettings
    {
        [CustomEditor(typeof(ScriptTemplateSettings))]
        internal class ScriptTemplateSettingsEditor : Editor
        {
            private const string PENDING_RESTART = nameof(ScriptTemplateSettings) + "_" + nameof(PENDING_RESTART);

            private enum TemplateType
            {
                BuiltIn,
                CustomOverride,
                AdditionalCustom,
                UninitializedCustom
            }

            private class TemplateState
            {
                public readonly SerializedProperty fullPath;
                public readonly SerializedProperty fileName;
                public readonly SerializedProperty textAssetProperty;
                public readonly TemplateType type;
                public readonly SerializedProperty serialized;
                public readonly TextAsset textAsset;
                public readonly bool hasUnsavedChanges;

                public TemplateState(SerializedProperty scriptTemplate, string[] BuiltInTemplateFileNames)
                {
                    fullPath = scriptTemplate.FindPropertyRelative(nameof(ScriptTemplate.FullPath));
                    fileName = scriptTemplate.FindPropertyRelative(nameof(ScriptTemplate.FileName));
                    textAssetProperty = scriptTemplate.FindPropertyRelative(nameof(ScriptTemplate.TextAsset));
                    serialized = scriptTemplate.FindPropertyRelative(nameof(ScriptTemplate.Serialized));
                    textAsset = textAssetProperty.objectReferenceValue as TextAsset;
                    hasUnsavedChanges = textAsset != null && serialized.stringValue != textAsset.text;

                    if (textAssetProperty.objectReferenceValue == null)
                    {
                        if (!string.IsNullOrWhiteSpace(fileName.stringValue))
                        {
                            type = TemplateType.BuiltIn;
                        }
                        else
                        {
                            type = TemplateType.UninitializedCustom;
                        }
                    }
                    else
                    {
                        if (BuiltInTemplateFileNames.Contains(fileName.stringValue))
                        {
                            type = TemplateType.CustomOverride;
                        }
                        else
                        {
                            type = TemplateType.AdditionalCustom;
                        }
                    }
                }
            }

            private class UninitializedTemplate
            {
                public int Order = 0;
                public string MenuName = "Scripting__New Custom Template";
                public string Title = "NewCustomScriptTemplate";
                public string FileExtension = "cs";

                public string FileName => $"{Order}-{MenuName}-{Title}.{FileExtension}.txt";

                public string Content;
            }

            private static readonly Color selectedFileColor = new Color(0.43f, 0.7f, 0.9f);

            private SerializedProperty scriptTemplatesProperty;
            private SerializedProperty builtInExpandedProperty;
            private SerializedProperty customExpandedProperty;

            private GUIStyle fileButtonStyle;

            private Vector2 mainScrollPosition;
            private float splitPosition = 200f;
            private bool isResizingSplit;
            private int currentselected = 0;

            private UninitializedTemplate uninitializedTemplate;

            private void OnEnable()
            {
                scriptTemplatesProperty = serializedObject.FindProperty(nameof(ScriptTemplates));
                builtInExpandedProperty = serializedObject.FindProperty(nameof(BuiltInExpanded));
                customExpandedProperty = serializedObject.FindProperty(nameof(CustomExpanded));
            }

            public override void OnInspectorGUI()
            {
                DrawScriptField();

                fileButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    alignment = TextAnchor.MiddleLeft
                };

                DrawPrendingRestartWarning();

                serializedObject.Update();

                var activeTemplate = GetCurrentActive();
                var activeTemplateState = new TemplateState(activeTemplate, settings.BuiltInTemplateFileNames);

                var rect = EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                {
                    var leftWidth = splitPosition;
                    var rightWidth = rect.width - splitPosition - 10;

                    using (new EditorGUILayout.VerticalScope(GUILayout.ExpandHeight(true), GUILayout.MinWidth(splitPosition), GUILayout.MaxWidth(splitPosition), GUILayout.Width(splitPosition), GUILayout.ExpandWidth(false)))
                    {
                        DrawBuiltInScriptTemplatesSection(activeTemplateState);

                        DrawCustomScriptTemplatesSection(activeTemplateState);
                    }

                    HandleSplit(rect.x, rect.width);

                    using (new EditorGUILayout.VerticalScope("Box", GUILayout.ExpandHeight(false)))
                    {
                        if (uninitializedTemplate != null)
                        {
                            EditorGUILayout.HelpBox("Configure the name of the new template file.\nSubmenues can be created by using __ as separator.", MessageType.Info, true);

                            using (new EditorGUILayout.HorizontalScope())
                            {
                                uninitializedTemplate.Order = EditorGUILayout.IntField(uninitializedTemplate.Order);
                                uninitializedTemplate.MenuName = EditorGUILayout.TextField(uninitializedTemplate.MenuName);
                                uninitializedTemplate.Title = EditorGUILayout.TextField(uninitializedTemplate.Title);
                                uninitializedTemplate.FileExtension = EditorGUILayout.TextField(uninitializedTemplate.FileExtension);
                            }

                            EditorGUILayout.LabelField(uninitializedTemplate.FileName);

                            uninitializedTemplate.Content = EditorGUILayout.TextArea(uninitializedTemplate.Content, GUILayout.Width(rightWidth), GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

                            if (GUILayout.Button("Apply", GUILayout.ExpandWidth(false)))
                            {
                                var path = Path.Combine(CustomScriptTemplatesFolder, uninitializedTemplate.FileName);

                                if (settings.CustomTemplateFiles.Any(o => o == path))
                                {
                                    if (!EditorUtility.DisplayDialog("Overwrite existing template?", $"A template with the name\n{uninitializedTemplate.FileName}\nalready exists!\n\nDo you want to overwrite the existing one?", "Overwrite", "Cancel"))
                                    {
                                        return;
                                    }
                                }

                                File.WriteAllText(path, uninitializedTemplate.Content);

                                OnModifications();

                                uninitializedTemplate = null;
                                currentselected = Array.FindIndex(settings.CustomTemplateFiles, o => o == path);
                            }

                            if (GUILayout.Button("Cancel", GUILayout.ExpandWidth(false)))
                            {
                                uninitializedTemplate = null;

                                currentselected = 0;
                            }
                        }
                        else
                        {
                            using (var scroll = new EditorGUILayout.ScrollViewScope(mainScrollPosition))
                            {
                                mainScrollPosition = scroll.scrollPosition;

                                using (new EditorGUI.DisabledGroupScope(activeTemplateState.type == TemplateType.BuiltIn))
                                {
                                    activeTemplateState.serialized.stringValue = EditorGUILayout.TextArea(activeTemplateState.serialized.stringValue, GUILayout.Width(rightWidth), GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                                }
                            }

                            using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandHeight(false)))
                            {
                                switch (activeTemplateState.type)
                                {
                                    case TemplateType.BuiltIn:
                                        if (GUILayout.Button("Customize", GUILayout.ExpandWidth(false)))
                                        {
                                            File.WriteAllText(Path.Combine(CustomScriptTemplatesFolder, activeTemplateState.fileName.stringValue), activeTemplateState.serialized.stringValue);

                                            OnModifications();
                                        }
                                        break;
                                    case TemplateType.CustomOverride:
                                        if (GUILayout.Button("Restore Built-In", GUILayout.ExpandWidth(false)))
                                        {

                                            var headerLabel = "Reset to Built-In?";
                                            var message = "The custom script template\n{fileName.stringValue}\nwill be deleted.\nThe built-in one will be used instead.";

                                            if (EditorUtility.DisplayDialog(headerLabel, message, "DELETE", "Cancel"))
                                            {
                                                AssetDatabase.DeleteAsset(AssetsFolder + "/" + activeTemplateState.fileName.stringValue);

                                                OnModifications();
                                            }
                                        }
                                        break;
                                    case TemplateType.AdditionalCustom:
                                        if (GUILayout.Button("Delete", GUILayout.ExpandWidth(false)))
                                        {
                                            var headerLabel = "Delete?";
                                            var message = $"The custom script template\n{activeTemplateState.fileName.stringValue}\nwill be deleted";

                                            if (EditorUtility.DisplayDialog(headerLabel, message, "DELETE", "Cancel"))
                                            {
                                                AssetDatabase.DeleteAsset(AssetsFolder + "/" + activeTemplateState.fileName.stringValue);

                                                OnModifications();
                                            }
                                        }
                                        break;
                                }

                                if (activeTemplateState.type == TemplateType.CustomOverride || activeTemplateState.type == TemplateType.AdditionalCustom)
                                {

                                    if (activeTemplateState.hasUnsavedChanges)
                                    {
                                        GUILayout.FlexibleSpace();

                                        if (GUILayout.Button("Apply Changes", GUILayout.ExpandWidth(false)))
                                        {
                                            File.WriteAllText(Path.Combine(CustomScriptTemplatesFolder, activeTemplateState.fileName.stringValue), activeTemplateState.serialized.stringValue);

                                            OnModifications();
                                        }

                                        if (GUILayout.Button("Cancel Edit", GUILayout.ExpandWidth(false)))
                                        {
                                            activeTemplateState.serialized.stringValue = activeTemplateState.textAsset.text;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();

                serializedObject.ApplyModifiedProperties();
            }

            private SerializedProperty GetCurrentActive()
            {
                currentselected = Mathf.Clamp(currentselected, 0, scriptTemplatesProperty.arraySize - 1);

                return scriptTemplatesProperty.GetArrayElementAtIndex(currentselected);
            }

            private void DrawBuiltInScriptTemplatesSection(TemplateState currentSelectedTemplateState)
            {
                builtInExpandedProperty.boolValue = EditorGUILayout.Foldout(builtInExpandedProperty.boolValue || currentSelectedTemplateState.type == TemplateType.BuiltIn, "Built-In Script Templates", true);

                if (builtInExpandedProperty.boolValue)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        var hasEntries = false;

                        for (var i = 0; i < scriptTemplatesProperty.arraySize; i++)
                        {
                            var scriptTemplate = scriptTemplatesProperty.GetArrayElementAtIndex(i);
                            var state = new TemplateState(scriptTemplate, settings.BuiltInTemplateFileNames);

                            if (state.type != TemplateType.BuiltIn)
                            {
                                continue;
                            }

                            hasEntries = true;

                            DrawFileButton(state, i);
                        }

                        if (!hasEntries)
                        {
                            EditorGUILayout.LabelField("No Elements");
                        }
                    }
                }
            }

            private void DrawFileButton(TemplateState state, int index)
            {
                var color = GUI.backgroundColor;
                if (currentselected == index)
                {
                    GUI.backgroundColor = selectedFileColor;
                }
                var buttonLabel = new GUIContent(state.fileName.stringValue, state.fullPath.stringValue);
                if (GUILayout.Button(buttonLabel, fileButtonStyle, GUILayout.Width(splitPosition)))
                {
                    currentselected = index;
                    uninitializedTemplate = null;
                }
                GUI.backgroundColor = color;
            }

            private void DrawCustomScriptTemplatesSection(TemplateState currentSelectedTemplateState)
            {
                customExpandedProperty.boolValue = EditorGUILayout.Foldout(customExpandedProperty.boolValue || currentSelectedTemplateState.type != TemplateType.BuiltIn, "Custom Script Templates", true);

                if (customExpandedProperty.boolValue)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        var hasEntries = false;

                        for (var i = 0; i < scriptTemplatesProperty.arraySize; i++)
                        {
                            var scriptTemplate = scriptTemplatesProperty.GetArrayElementAtIndex(i);
                            var state = new TemplateState(scriptTemplate, settings.BuiltInTemplateFileNames);

                            if (state.type == TemplateType.BuiltIn)
                            {
                                continue;
                            }

                            hasEntries = true;

                            DrawFileButton(state, i);
                        }

                        if (!hasEntries)
                        {
                            EditorGUILayout.LabelField("No Elements", GUILayout.Width(splitPosition));
                        }

                        EditorGUILayout.Space(5f);

                        using (new GUIBackgroundScope(Color.green))
                        {
                            if (GUILayout.Button("+ Add New Template", GUILayout.Width(splitPosition)))
                            {
                                uninitializedTemplate = new UninitializedTemplate();
                            }
                        }
                    }
                }
            }

            private void HandleSplit(float x, float width)
            {
                EditorGUILayout.LabelField(GUIContent.none, GUI.skin.verticalSlider, GUILayout.ExpandHeight(true), GUILayout.Width(10));

                var resizeHandleRect = GUILayoutUtility.GetLastRect();

                EditorGUIUtility.AddCursorRect(resizeHandleRect, MouseCursor.ResizeHorizontal);

                if (Event.current.type == EventType.MouseDown)
                {
                    if (resizeHandleRect.Contains(Event.current.mousePosition))
                    {
                        isResizingSplit = true;
                    }
                }
                else if (Event.current.rawType == EventType.MouseUp)
                {
                    isResizingSplit = false;
                }

                if (isResizingSplit && Event.current.rawType != EventType.Layout)
                {
                    var newSplitPosition = Event.current.mousePosition.x - x - 16;
                    newSplitPosition = Mathf.Clamp(newSplitPosition, 170, width - 170);

                    if (!Mathf.Approximately(newSplitPosition, splitPosition))
                    {
                        splitPosition = newSplitPosition;
                        Repaint();
                    }
                }
            }

            private void OnModifications()
            {
                serializedObject.ApplyModifiedProperties();

                AssetDatabase.Refresh();

                Settings.OnEnable();

                if (EditorUtility.DisplayDialog("Restart Unity Editor?", "Changes to script templates require the editor to restart in order to take effect.", "Restart Now", "Skip and restart manually later"))
                {
                    AssetDatabase.SaveAssets();
                    ReopenProject();
                }
                else
                {
                    SessionState.SetBool(PENDING_RESTART, true);
                }
            }

            private static void DrawScriptField()
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.ObjectField("Script", MonoScript.FromScriptableObject(settings), typeof(MonoScript), false);
                }

                EditorGUILayout.Space();
            }

            private static void DrawPrendingRestartWarning()
            {
                if (SessionState.GetBool(PENDING_RESTART, false))
                {
                    EditorGUILayout.HelpBox("Changes to script templates require the editor to restart in order to take effect.", MessageType.Warning, true);

                    using (new GUIBackgroundScope(new Color(1f, 0.76f, 0.03f)))
                    {
                        if (GUILayout.Button("Restart Now", GUILayout.Height(EditorGUIUtility.singleLineHeight * 2)))
                        {
                            AssetDatabase.SaveAssets();
                            ReopenProject();
                        }
                    }

                    EditorGUILayoutExtensions.HorizontalSeperator();
                }
            }

            private static void ReopenProject()
            {
                EditorApplication.OpenProject(Directory.GetCurrentDirectory());
            }
        }
    }
}
