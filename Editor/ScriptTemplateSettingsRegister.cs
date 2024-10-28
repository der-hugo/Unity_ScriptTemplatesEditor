using System.Collections.Generic;
using UnityEditor;

namespace derHugo.ScriptTemplateEditor
{
    internal static class ScriptTemplateSettingsRegister
    {
        [SettingsProvider]
        public static SettingsProvider CreateMyCustomSettingsProvider()
        {
            var editor = Editor.CreateEditor(ScriptTemplateSettings.Settings);

            // First parameter is the path in the Settings window.
            // Second parameter is the scope of this setting: it only appears in the Settings window for the Project scope.
            var provider = new SettingsProvider("Project/Custom Script Templates", SettingsScope.Project)
            {
                guiHandler = (searchContext) =>
                {
                    editor.OnInspectorGUI();
                },

                // Populate the search keywords to enable smart search filtering and label highlighting:
                keywords = new HashSet<string>(new[] { "Template", "Custom", "Editor" })
            };

            return provider;
        }
    }
}
