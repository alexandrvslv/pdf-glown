using System;

namespace PdfGlown.UI
{
    [Flags]
    public enum KeyModifiers
    {
        None = 0,
        Alt = 1,
        Ctrl = 2,
        Shift = 4,
        Meta = 8,
    }
}
