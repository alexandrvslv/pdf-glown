using PdfGlown.Documents;
using PdfGlown.Documents.Interaction.Annotations;
using PdfGlown.UI.Text;
using PdfGlown.UI.Operations;

namespace PdfGlown.UI
{
    public interface IPdfView: ISKScrollView
    {
        IPdfDocumentViewModel? Document { get; set; }
        IPdfPageViewModel? Page { get; set; }
        PdfPage? PdfPage { get; set; }
        CursorType Cursor { get; set; }
        bool ShowMarkup { get; set; }
        bool ShowCharBound { get; set; }
        bool ScrollByPointer { get; set; }
        EditorOperations Operations { get; }
        bool IsReadOnly { get; set; }
        bool IsModified { get; }
        int PagesCount { get; }
        int PageNumber { get; set; }
        int NewPageNumber { get; set; }

        TextSelection TextSelection { get; }

        PdfViewFitMode FitMode { get; set; }
        float ScaleContent { get; set; }

        void ScrollTo(Annotation annotation);
        void ScrollTo(PdfPage page);
        void ScrollTo(IPdfPageViewModel page);
        void Reload();
    }
}
