using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Skia;
using Avalonia.VisualTree;
using PdfGlown.Documents;
using PdfGlown.Documents.Interaction.Annotations;
using PdfGlown.UI.Operations;
using PdfGlown.UI.Text;
using PdfGlown.Util.Math;
using System;
using System.IO;

namespace PdfGlown.UI.Aval;

public partial class PdfView : SKScrollView, IPdfView
{
    public static readonly DirectProperty<PdfView, PdfViewFitMode> FitModeProperty = AvaloniaProperty.RegisterDirect<PdfView, PdfViewFitMode>(nameof(FitMode),
        o => o.FitMode,
        (o, v) => o.FitMode = v);

    public static readonly DirectProperty<PdfView, bool> IsReadOnlyProperty = AvaloniaProperty.RegisterDirect<PdfView, bool>(nameof(IsReadOnly),
        o => o.IsReadOnly,
        (o, v) => o.IsReadOnly = v);

    public static readonly DirectProperty<PdfView, bool> IsModifiedProperty = AvaloniaProperty.RegisterDirect<PdfView, bool>(nameof(IsModified),
        o => o.IsModified,
        (o, v) => { });

    public static readonly DirectProperty<PdfView, int> NewPageNumberProperty = AvaloniaProperty.RegisterDirect<PdfView, int>(nameof(NewPageNumber),
        o => o.NewPageNumber,
        (o, v) => o.NewPageNumber = v);

    public static readonly DirectProperty<PdfView, int> PagesCountProperty = AvaloniaProperty.RegisterDirect<PdfView, int>(nameof(PagesCount),
        o => o.PagesCount,
        (o, v) => { });

    public static readonly DirectProperty<PdfView, float> ScaleContentProperty = AvaloniaProperty.RegisterDirect<PdfView, float>(nameof(ScaleContent),
        o => o.ScaleContent,
        (o, v) => o.ScaleContent = v);

    public static readonly DirectProperty<PdfView, bool> ShowCharBoundProperty = AvaloniaProperty.RegisterDirect<PdfView, bool>(nameof(ShowCharBound),
        o => o.ShowCharBound,
        (o, v) => o.ShowCharBound = v);

    public static readonly DirectProperty<PdfView, bool> ShowMarkupProperty = AvaloniaProperty.RegisterDirect<PdfView, bool>(nameof(ShowMarkup),
        o => o.ShowMarkup,
        (o, v) => o.ShowMarkup = v);

    private bool showMarkup = true;
    private bool showCharBound;
    private bool isReadOnly;
    private TextSelection textSelection;
    private PdfViewFitMode fitMode = PdfViewFitMode.PageSize;
    private CursorType cursor;

    public PdfView()
    {
        Operations = new EditorOperations(this);
        Operations.DocumentChanged += OnDocumentChanged;
        Operations.Changed += OnOperationsChanged;
        Operations.CurrentPageChanged += OnCurrentPageChanged;
        Operations.ScaleChanged += OnScaleChanged;

        textSelection = new TextSelection();
        textSelection.Changed += OnTextSelectionChanged;

        scroll.VScrolled += OnVScrolled;
        scroll.HScrolled += OnHScrolled;        
    }

    public IPdfDocumentViewModel? Document
    {
        get => Operations.Document;
        set => Operations.Document = value;
    }

    public IPdfPageViewModel? Page
    {
        get => Operations.CurrentPage;
        set => Operations.CurrentPage = value;
    }

    public PdfPage? PdfPage
    {
        get => Page?.GetPage(Operations.State);
        set => Page = value == null ? null : Document?.GetPageView(value);
    }

    public bool ShowMarkup
    {
        get => showMarkup;
        set
        {
            if (SetAndRaise(ShowMarkupProperty, ref showMarkup, value))
                InvalidatePaint();
        }
    }

    public bool ShowCharBound
    {
        get => showCharBound;
        set
        {
            if (SetAndRaise(ShowCharBoundProperty, ref showCharBound, value))
                InvalidatePaint();
        }
    }
    public bool ScrollByPointer { get; set; }

    public EditorOperations Operations { get; }

    public bool IsReadOnly
    {
        get => isReadOnly;
        set
        {
            if (SetAndRaise(IsReadOnlyProperty, ref isReadOnly, value))
                InvalidatePaint();
        }
    }

    public bool IsModified
    {
        get => Operations.HashOperations;
    }

    public int PagesCount => Operations.PagesCount;

    public int PageNumber
    {
        get => Operations.CurrentPageNumber;
        set => Operations.CurrentPageNumber = value;
    }

    public int NewPageNumber
    {
        get => Operations.NewPageNumber;
        set => Operations.NewPageNumber = value;
    }

    public TextSelection TextSelection => textSelection;

    public PdfViewFitMode FitMode
    {
        get => fitMode;
        set
        {
            if (SetAndRaise(FitModeProperty, ref fitMode, value)
                && Page != null)
            {
                ScrollTo(Page);
            }
        }
    }

    public float ScaleContent
    {
        get => Operations.Scale;
        set
        {
            var oldValue = Operations.Scale;
            Operations.SetScale(value);
        }
    }

    CursorType IPdfView.Cursor
    {
        get => cursor;
        set
        {
            if (cursor != value)
            {
                cursor = value;
                base.Cursor = new Cursor((StandardCursorType)value);
            }
        }
    }    

    protected override void OnPaintContent(SKPaintSurfaceEventArgs e)
    {
        Operations.Draw(e.Canvas);
    }

    protected override void OnLayoutUpdated(object? sender, EventArgs e)
    {
        base.OnLayoutUpdated(sender, e);
        var bounds = this.GetTransformedBounds()?.Clip ?? Bounds;
        Operations.OnSizeAllocated(bounds.ToSKRect(), scroll.WindowScale);
        if (Page != null)
        {
            ScrollTo(Page);
        }
    }

    protected override void OnTouch(TouchEventArgs e)
    {
        base.OnTouch(e);
        if (e.Handled)
            return;
        var point = e.Location.Scale(scroll.WindowScale);
        point.Offset(Operations.State.WindowArea.Left, Operations.State.WindowArea.Top);
        Operations.OnTouch(e.ActionType, e.MouseButton, point);
    }

    protected override void OnScrolled(TouchEventArgs e)
    {
        if (e.KeyModifiers == KeyModifiers.None)
        {
            base.OnScrolled(e);
        }
        if (e.KeyModifiers == KeyModifiers.Ctrl)
        {
            Operations.ScaleToPointer(-e.WheelDelta);
            e.Handled = true;
        }
    }

    private void OnScaleChanged(FloatEventArgs e)
    {
        RaiseProperty(ScaleContentProperty, Operations.Scale, Operations.Scale);
    }

    private void OnCurrentPageChanged(PdfPageEventArgs e)
    {
        RaiseProperty(NewPageNumberProperty, Operations.NewPageNumber, Operations.NewPageNumber);
    }

    private void OnTextSelectionChanged(TextSelectionEventArgs args)
    {
        InvalidatePaint();
    }

    private void OnDocumentChanged(PdfDocumentEventArgs e)
    {
        RaiseProperty(PagesCountProperty, PagesCount, PagesCount);
    }

    private void OnOperationsChanged(object? sender, EventArgs? e)
    {
        RaiseProperty(IsModifiedProperty, IsModified, IsModified);
    }

    private void OnVScrolled(object? sender, ScrollEventArgs e)
    {
        Operations.UpdateNavigationMatrix();
        if (!IsVScrollAnimation)
        {
            Page = Operations.GetCenterPage();
        }
    }

    private void OnHScrolled(object? sender, ScrollEventArgs e)
    {
        Operations.UpdateNavigationMatrix();
    }

    public void Reload()
    {
        var newDocument = Document?.Reload(Operations);
        Operations.MoveToLast();
        Close();
        Document = newDocument;
        InvalidatePaint();
    }

    public void Close()
    {
        Operations.ClearOperations();
        TextSelection.Clear();
        var document = Document;
        Document = null;
        document?.Dispose();
    }

    public void Load(string filePath)
    {
        Close();
        Document = PdfDocumentViewModel.LoadFrom(filePath);
    }

    public void Load(Stream stream)
    {
        Close();
        Document = PdfDocumentViewModel.LoadFrom(stream);
    }

    public void ScrollTo(Annotation annotation)
    {
        var location = Operations.GetLocation(annotation);
        AnimateScroll(Math.Max(location.Y, 0), Math.Max(location.X, 0));
    }

    public void ScrollTo(PdfPage page)
    {
        if (Document?.GetPageView(page) is IPdfPageViewModel pageView)
            ScrollTo(pageView);
    }

    public void ScrollTo(IPdfPageViewModel page)
    {
        var location = Operations.FitToAndGetLocation(page);
        AnimateScroll(Math.Max(location.Y, 0), Math.Max(location.X, 0));
    }
}