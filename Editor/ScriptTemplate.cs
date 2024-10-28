using System;
using UnityEngine;

namespace derHugo.ScriptTemplateEditor
{
    [Serializable]
    internal class ScriptTemplate
    {
        public string FullPath;
        public string FileName;
        public TextAsset TextAsset;
        [TextArea] public string Serialized;
    }
}
