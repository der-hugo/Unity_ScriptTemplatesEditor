using System;
using UnityEngine;

namespace derHugo.ScriptTemplateEditor
{
    internal class GUIBackgroundScope : IDisposable
    {
        private Color originalColor;

        public GUIBackgroundScope(Color color)
        {
            originalColor = GUI.backgroundColor;
            GUI.backgroundColor = color;
        }

        public void Dispose()
        {
            GUI.backgroundColor = originalColor;
        }
    }
}
