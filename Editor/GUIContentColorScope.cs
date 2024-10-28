using System;
using UnityEngine;

namespace ScriptTemplateEditor
{
    public class GUIContentColorScope : IDisposable
    {
        private Color originalColor;

        public GUIContentColorScope(Color color)
        {
            originalColor = GUI.contentColor;
            GUI.contentColor = color;
        }

        public void Dispose()
        {
            GUI.contentColor = originalColor;
        }
    }
}
