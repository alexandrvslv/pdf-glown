using SkiaSharp;
using System;

namespace PdfGlown.UI
{
    public interface IBoundHandler
    {
        SKRect Bounds { get; }
        event EventHandler BoundsChanged;
    }
}