using PdfGlown.Documents;
using PdfGlown.Documents.Interaction.Forms;
using PdfGlown.UI.Operations;
using SkiaSharp;
using System;
using System.Collections.Generic;

namespace PdfGlown.UI
{
    public interface IPdfDocumentViewModel : IDisposable, IBoundHandler
    {
        float AvgHeigth { get; }
        bool IsClosed { get; }
        bool IsPaintComplete { get; }
        IEnumerable<IPdfPageViewModel> PageViews { get; }
        SKSize Size { get; }
        int PagesCount { get; }
        IEnumerable<Field>? Fields { get; }
        IPdfPageViewModel this[int inde] { get; }

        event PdfAnnotationEventHandler AnnotationAdded;
        event PdfAnnotationEventHandler AnnotationRemoved;

        PdfDocumentViewModel? GetDocumentView(PdfDocument document);
        PdfPageViewModel? GetPageView(PdfPage page);
        IPdfDocumentViewModel Reload(EditorOperations operations);
        bool ContainsField(string fieldName);
    }
}