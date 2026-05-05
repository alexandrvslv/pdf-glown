using System;

namespace PdfGlown.UI
{
    public class CanvasKeyEventArgs : EventArgs
    {
        public CanvasKeyEventArgs(string keyName, KeyModifiers modifiers)
        {
            KeyName = keyName;
            Modifiers = modifiers;
        }

        public string KeyName { get; }

        public KeyModifiers Modifiers { get; }

        public bool Handled { get; set; }
    }
}
