using Microsoft.AspNetCore.Components;
using PdfGlown.Documents;
using PdfGlown.Documents.Interaction.Annotations;
using PdfGlown.UI.Operations;
using PdfGlown.UI.Text;
using PdfGlown.Util.Math;
using SkiaSharp;
using SkiaSharp.Views.Blazor;
using System.Runtime.Versioning;

namespace PdfGlown.UI.Blazor
{
    [SupportedOSPlatform("browser")]
    public partial class PdfView : SKScrollView, IPdfView
    {
        private bool showCharBound;
        private PdfViewFitMode fitMode = PdfViewFitMode.PageSize;
        private bool showMarkup = true;
        private bool isReadOnly;

        public PdfView()
        {
            TextSelection = new TextSelection();
            TextSelection.Changed += OnTextSelectionChanged;

            Operations = new EditorOperations(this);
            Operations.DocumentChanged += OnDocumentChanged;
            Operations.Changed += OnOperationsChanged;
            Operations.CurrentPageChanged += OnCurrentPageChanged;
            Operations.ScaleChanged += OnScaleChanged;

            scroll.VScrolled += OnVScrolled;
            scroll.HScrolled += OnHScrolled;
        }

        [Parameter]
        public PdfViewFitMode FitMode { get; set; }

        [Parameter]
        public EventCallback<PdfViewFitMode> FitModeChanged { get; set; }

        [Parameter]
        public float ScaleContent { get; set; }

        [Parameter]
        public EventCallback<float> ScaleContentChanged { get; set; }

        [Parameter]
        public bool ShowMarkup { get; set; } = true;

        [Parameter]
        public EventCallback<bool> ShowMarkupChanged { get; set; }

        public bool ScrollByPointer { get; set; } = true;

        [Parameter]
        public bool IsReadOnly { get; set; }

        [Parameter]
        public EventCallback<bool> IsReadOnlyChanged { get; set; }

        [Parameter]
        public bool ShowCharBound { get; set; }

        [Parameter]
        public EventCallback<bool> ShowCharBoundChanged { get; set; }

        public IPdfDocumentViewModel? Document
        {
            get => Operations.Document;
            set => Operations.Document = value;
        }

        public PdfPage? PdfPage
        {
            get => Page?.GetPage(Operations.State);
            set => Page = value == null ? null : Document?.GetPageView(value);
        }

        public IPdfPageViewModel? Page
        {
            get => Operations.CurrentPage;
            set => Operations.CurrentPage = value;
        }

        public TextSelection TextSelection { get; private set; }

        public EditorOperations Operations { get; private set; }

        [Parameter]
        public bool IsModified { get; set; }

        [Parameter]
        public EventCallback<bool>? IsModifiedChanged { get; set; }

        [Parameter]
        public int PagesCount { get; set; }

        [Parameter]
        public EventCallback<int> PagesCountChanged { get; set; }

        [Parameter]
        public int NewPageNumber { get; set; }

        [Parameter]
        public EventCallback<int> NewPageNumberChanged { get; set; }

        [Parameter]
        public int PageNumber { get; set; }

        [Parameter]
        public EventCallback<int> PageNumberChanged { get; set; }


        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            if (fitMode != FitMode)
            {
                OnFitModeChanged(fitMode, FitMode);
            }
            if (Operations.Scale != ScaleContent)
            {
                Operations.SetScale(ScaleContent);
            }
            if (showMarkup != ShowMarkup)
            {
                OnShowMarkupChanged(showMarkup, ShowMarkup);
            }
            if (isReadOnly != IsReadOnly)
            {
                OnIsReadOnlyChanged(isReadOnly, IsReadOnly);
            }
            if (showCharBound != ShowCharBound)
            {
                OnShowCharBoundChanged(showCharBound, ShowCharBound);
            }
            //if (state.CurrentPageNumber != PageNumber)
            //{
            //    state.CurrentPageNumber = PageNumber;
            //}
            if (Operations.NewPageNumber != NewPageNumber)
            {
                Operations.NewPageNumber = NewPageNumber;
            }
        }

        private void OnFitModeChanged(PdfViewFitMode oldValue, PdfViewFitMode newValue)
        {
            fitMode = newValue;
            //InvalidateSurface();
            if (Page != null)
            {
                ScrollTo(Page);
            }
        }

        private void OnShowMarkupChanged(bool oldValue, bool newValue)
        {
            showMarkup = newValue;
            InvalidatePaint();
        }

        private void OnShowCharBoundChanged(bool oldValue, bool newValue)
        {
            showCharBound = newValue;
            InvalidatePaint();
        }

        private void OnIsReadOnlyChanged(bool oldValue, bool newValue)
        {
            isReadOnly = newValue;
            InvalidatePaint();
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

#if __FORCE_GL__
        protected override void OnPaintContent(SKPaintGLSurfaceEventArgs e)
#else
        protected override void OnPaintContent(SKPaintSurfaceEventArgs e)
#endif
        {
            Operations.Draw(e.Surface.Canvas);
        }

        public override bool OnKeyDown(string keyName, KeyModifiers modifiers)
        {
            if (Operations.OnKeyDown(keyName, modifiers))
                return true;
            return base.OnKeyDown(keyName, modifiers);
        }

        private void OnDocumentChanged(PdfDocumentEventArgs e)
        {
            _ = PagesCountChanged.InvokeAsync(Operations.PagesCount);
        }

        private void OnCurrentPageChanged(PdfPageEventArgs e)
        {
            if (Operations.CurrentPageNumber != PageNumber)
                _ = PageNumberChanged.InvokeAsync(Operations.CurrentPageNumber);
            if (Operations.NewPageNumber != NewPageNumber)
                _ = NewPageNumberChanged.InvokeAsync(Operations.NewPageNumber);
        }

        private void OnScaleChanged(FloatEventArgs e)
        {
            if (e.Value != ScaleContent)
                _ = ScaleContentChanged.InvokeAsync(e.Value);
        }

        private void OnTextSelectionChanged(TextSelectionEventArgs args)
        {
            InvalidatePaint();
        }

        private void OnOperationsChanged(object? sender, EventArgs? e)
        {
            IsModifiedChanged?.InvokeAsync(Operations.HashOperations);
        }

        protected override void OnTouch(TouchEventArgs e)
        {
            base.OnTouch(e);
            if (e.Handled)
                return;
            Operations.OnTouch(e.ActionType, e.MouseButton, e.Location.Scale(scroll.WindowScale));
        }

        public override void OnScrolled(TouchEventArgs e)
        {
            if (e.KeyModifiers == KeyModifiers.None)
            {
                base.OnScrolled(e);
            }
            if (e.KeyModifiers == KeyModifiers.Ctrl)
            {
                Operations.ScaleToPointer(-e.WheelDelta);
            }
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
            Operations.OnSizeAllocated(SKRect.Create((float)width, (float)height), scroll.WindowScale);
            if (Page != null)
            {
                ScrollTo(Page);
            }
        }

        public void Reload()
        {
            var newDocument = Document?.Reload(Operations);
            Operations.MoveToLast();
            Close();
            Document = newDocument;
            InvalidatePaint();
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

        public void Close()
        {
            Operations.ClearOperations();
            TextSelection.Clear();
            var document = Document;
            Document = null;
            document?.Dispose();
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
}