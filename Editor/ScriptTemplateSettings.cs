using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace derHugo.ScriptTemplateEditor
{
    internal partial class ScriptTemplateSettings : ScriptableObject
    {
        private const string TEMPLATE_FILE_EXTENSION = "*.txt";

        private const string AssetsFolder = "Assets/ScriptTemplates";
        private const string SettingsAssetFolder = AssetsFolder + "/Editor";
        private const string SettingsAssetPath = SettingsAssetFolder + "/" + nameof(ScriptTemplateSettings) + ".asset";

        private static readonly string BuiltInScriptTemplatesFolder = Path.Combine(Path.GetDirectoryName(EditorApplication.applicationPath), "Data", "Resources", "ScriptTemplates");
        private static readonly string CustomScriptTemplatesFolder = Path.Combine(Directory.GetCurrentDirectory(), AssetsFolder);

        private static ScriptTemplateSettings settings;

        private string[] BuiltInTemplateFiles;
        private string[] CustomTemplateFiles;
        private string[] BuiltInTemplateFileNames;

        public static ScriptTemplateSettings Settings
        {
            get
            {
                if (settings)
                {
                    return settings;
                }

                settings = AssetDatabase.LoadAssetAtPath<ScriptTemplateSettings>(SettingsAssetPath);

                if (settings)
                {
                    return settings;
                }

                if (!Directory.Exists(SettingsAssetFolder))
                {
                    Directory.CreateDirectory(SettingsAssetFolder);
                }

                settings = CreateInstance<ScriptTemplateSettings>();

                AssetDatabase.CreateAsset(settings, SettingsAssetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                settings = AssetDatabase.LoadAssetAtPath<ScriptTemplateSettings>(SettingsAssetPath);

                return settings;
            }
        }

        [SerializeField]
        private List<ScriptTemplate> ScriptTemplates = new();

        [SerializeField] private bool BuiltInExpanded;
        [SerializeField] private bool CustomExpanded;

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            var settings = Settings;
            settings.OnEnable();
        }

        private void OnEnable()
        {
            BuiltInTemplateFiles = Directory.GetFiles(BuiltInScriptTemplatesFolder, TEMPLATE_FILE_EXTENSION);
            CustomTemplateFiles = Directory.GetFiles(CustomScriptTemplatesFolder, TEMPLATE_FILE_EXTENSION);

            BuiltInTemplateFileNames = BuiltInTemplateFiles.Select(f => Path.GetFileName(f)).ToArray();

            ScriptTemplates.Clear();

            foreach (var customTemplatePath in CustomTemplateFiles)
            {
                var fileName = Path.GetFileName(customTemplatePath);

                var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(AssetsFolder + "/" + fileName);

                var customTemplate = new ScriptTemplate
                {
                    FullPath = customTemplatePath,
                    FileName = fileName,
                    TextAsset = textAsset,
                    Serialized = textAsset.text
                };

                ScriptTemplates.Add(customTemplate);
            }

            foreach (var templatePath in BuiltInTemplateFiles)
            {
                var fileName = Path.GetFileName(templatePath);

                if (ScriptTemplates.Find(o => o.FileName.Equals(fileName)) != null)
                {
                    continue;
                }

                var fileContent = File.ReadAllText(templatePath);

                var overrideEntry = new ScriptTemplate
                {
                    FullPath = templatePath,
                    FileName = fileName,
                    Serialized = fileContent,
                    TextAsset = null
                };

                ScriptTemplates.Add(overrideEntry);
            }
        }
    }
}
