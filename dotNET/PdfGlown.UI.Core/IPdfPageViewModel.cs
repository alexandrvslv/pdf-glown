using PdfGlown.Documents;
using PdfGlown.Documents.Contents.Scanner;
using PdfGlown.Documents.Interaction.Annotations;
using SkiaSharp;
using System.Collections.Generic;

namespace PdfGlown.UI
{
    public interface IPdfPageViewModel : IBoundHandler
    {
        int Index { get; }
        SKMatrix Matrix { get; set; }
        int Order { get; }
        PdfPage? GetPage(PdfViewState state);
        IPdfDocumentViewModel Document { get; set; }

        bool Draw(SKCanvas canvas, PdfViewState state);
        void Touch(PdfViewState state);
        Annotation? GetAnnotation(string name);
        IEnumerable<Annotation> Annotations { get; }
        IEnumerable<ITextString> GetStrings();
    }
}