using SkiaSharp;
using System;

namespace PdfGlown.UI.Aval;

public class SKPaintSurfaceEventArgs : EventArgs
{
    public SKPaintSurfaceEventArgs(SKSurface? surface, SKCanvas canvas)
    {
        Surface = surface;
        Canvas = canvas;
    }

    public SKCanvas Canvas { get; set; }
    public SKSurface? Surface { get; set; }

}
