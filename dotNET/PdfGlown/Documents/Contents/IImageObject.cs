using PdfGlown.Bytes;
using PdfGlown.Documents.Contents.ColorSpaces;
using PdfGlown.Objects;
using SkiaSharp;
using System.Collections.Generic;

namespace PdfGlown.Documents.Contents
{
    public interface IImageObject
    {
        int BitsPerComponent { get; }
        ColorSpace ColorSpace { get; }
        PdfDirectObject Filter { get; }
        PdfDirectObject Parameters { get; }
        IInputStream Data { get; }
        SKSize Size { get; }
        IImageObject SMask { get; }
        PdfDirectObject Mask { get; }
        bool ImageMask { get; }
        PdfArray Matte { get; }
        IDictionary<PdfName, PdfDirectObject> Header { get; }
        float[] Decode { get; }
        SKImage Load(GraphicsState state);
    }
}