using PdfGlown.Documents;
using PdfGlown.Documents.Interaction.Annotations;
using PdfGlown.UI.Operations;
using PdfGlown.UI.Text;
using PdfGlown.Util.Math;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using System;
using System.IO;
using System.Windows.Input;
using Xamarin.Forms;

namespace PdfGlown.UI
{

    public partial class PdfView : SKScrollView, IPdfView
    {
        public static readonly BindableProperty FitModeProperty = BindableProperty.Create(nameof(FitMode), typeof(PdfViewFitMode), typeof(PdfView), PdfViewFitMode.PageSize,
            propertyChanged: (bindable, oldValue, newValue) => ((PdfView)bindable).OnFitModeChanged((PdfViewFitMode)oldValue, (PdfViewFitMode)newValue));
        public static readonly BindableProperty PageBackgroundProperty = BindableProperty.Create(nameof(PageBackground), typeof(Color), typeof(PdfView), Color.White,
            propertyChanged: (bindable, oldValue, newValue) => ((PdfView)bindable).OnPageBackgroundChanged((Color)oldValue, (Color)newValue));
        public static readonly BindableProperty ScaleContentProperty = BindableProperty.Create(nameof(ScaleContent), typeof(float), typeof(PdfView), 1F,
            propertyChanged: (bindable, oldValue, newValue) => ((PdfView)bindable).OnScaleContentChanged((float)oldValue, (float)newValue));
        public static readonly BindableProperty ShowMarkupProperty = BindableProperty.Create(nameof(ShowMarkup), typeof(bool), typeof(PdfView), true,
            propertyChanged: (bindable, oldValue, newValue) => ((PdfView)bindable).OnShowMarkupChanged((bool)oldValue, (bool)newValue));
        public static readonly BindableProperty IsReadOnlyProperty = BindableProperty.Create(nameof(IsReadOnly), typeof(bool), typeof(PdfView), false,
           propertyChanged: (bindable, oldValue, newValue) => ((PdfView)bindable).OnIsReadOnlyChanged((bool)oldValue, (bool)newValue));
        public static readonly BindableProperty ShowCharBoundProperty = BindableProperty.Create(nameof(ShowCharBound), typeof(bool), typeof(PdfView), false,
           propertyChanged: (bindable, oldValue, newValue) => ((PdfView)bindable).OnShowCharBoundChanged((bool)oldValue, (bool)newValue));

        internal readonly SKPaint paintPageBackground = new SKPaint { Style = SKPaintStyle.Fill, Color = SKColors.White };

        public PdfView()
        {
            TextSelection = new TextSelection();
            TextSelection.Changed += OnTextSelectionChanged;

            Operations = new EditorOperations(this);
            Operations.DocumentChanged += OnDocumentChanged;
            Operations.Changed += OnOperationsChanged;
            Operations.CurrentPageChanged += OnCurrentPageChanged;
            Operations.ScaleChanged += OnScaleChanged;

            UndoCommand = new Command(() => Operations.Undo(), () => Operations.CanUndo);
            RedoCommand = new Command(() => Operations.Redo(), () => Operations.CanRedo);
            PrevPageCommand = new Command(() => Operations.PrevPage(), Operations.CanPrevPage);
            NextPageCommand = new Command(() => Operations.NextPage(), Operations.CanNextPage);

            scroll.VScrolled += OnVScrolled;
            scroll.HScrolled += OnHScrolled;
        }

        public PdfViewFitMode FitMode
        {
            get => (PdfViewFitMode)GetValue(FitModeProperty);
            set => SetValue(FitModeProperty, value);
        }

        public Color PageBackground
        {
            get => (Color)GetValue(PageBackgroundProperty);
            set => SetValue(PageBackgroundProperty, value);
        }

        public float ScaleContent
        {
            get => (float)GetValue(ScaleContentProperty);
            set => SetValue(ScaleContentProperty, value);
        }

        public bool ScrollByPointer { get; set; }

        public bool ShowMarkup
        {
            get => (bool)GetValue(ShowMarkupProperty);
            set => SetValue(ShowMarkupProperty, value);
        }

        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        public bool ShowCharBound
        {
            get => (bool)GetValue(ShowCharBoundProperty);
            set => SetValue(ShowCharBoundProperty, value);
        }

        public IPdfDocumentViewModel Document
        {
            get => Operations.Document;
            set => Operations.Document = value;
        }

        public PdfPage PdfPage
        {
            get => Page?.GetPage(Operations.State);
            set => Page = Document.GetPageView(value);
        }

        public IPdfPageViewModel Page
        {
            get => Operations.CurrentPage;
            set => Operations.CurrentPage = value;
        }

        public TextSelection TextSelection { get; private set; }

        public EditorOperations Operations { get; private set; }

        public bool IsModified => Operations.HashOperations;

        public int PagesCount
        {
            get => Operations.PagesCount;
        }

        public int NewPageNumber
        {
            get => Operations.NewPageNumber;
            set => Operations.NewPageNumber = value;
        }

        public int PageNumber
        {
            get => Operations.CurrentPageNumber;
            set => Operations.CurrentPageNumber = value;
        }

        public ICommand NextPageCommand { get; set; }

        public ICommand PrevPageCommand { get; set; }

        public ICommand RedoCommand { get; set; }

        public ICommand UndoCommand { get; set; }

        private void OnFitModeChanged(PdfViewFitMode oldValue, PdfViewFitMode newValue)
        {
            if (Page != null)
                ScrollTo(Page);
        }

        private void OnPageBackgroundChanged(Color oldValue, Color newValue)
        {
            paintPageBackground.Color = newValue.ToSKColor();
        }

        private void OnScaleContentChanged(float oldValue, float newValue)
        {
            Operations.SetScale(newValue);
        }

        private void OnShowMarkupChanged(bool oldValue, bool newValue)
        {
            InvalidatePaint();
        }

        private void OnShowCharBoundChanged(bool oldValue, bool newValue)
        {
            InvalidatePaint();
        }

        private void OnIsReadOnlyChanged(bool oldValue, bool newValue)
        {
        }

        private void OnVScrolled(object sender, ScrollEventArgs e)
        {
            Operations.UpdateNavigationMatrix();
            if (!IsVScrollAnimation)
                Page = Operations.GetCenterPage();
        }

        private void OnHScrolled(object sender, ScrollEventArgs e)
        {
            Operations.UpdateNavigationMatrix();
        }

        protected override void OnPaintContent(SKPaintSurfaceEventArgs e)
        {
            Operations.Draw(e.Surface.Canvas);
        }

        public override bool OnKeyDown(string keyName, KeyModifiers modifiers)
        {
            if (Operations.OnKeyDown(keyName, modifiers))
                return true;
            return base.OnKeyDown(keyName, modifiers);
        }

        private void OnScaleChanged(FloatEventArgs e)
        {
            ScaleContent = e.Value;
        }

        private void OnCurrentPageChanged(PdfPageEventArgs e)
        {
            OnPropertyChanged(nameof(Page));
            OnPropertyChanged(nameof(PdfPage));
            OnPropertyChanged(nameof(PageNumber));
            OnPropertyChanged(nameof(NewPageNumber));
            ((Command)NextPageCommand).ChangeCanExecute();
            ((Command)PrevPageCommand).ChangeCanExecute();
        }

        private void OnTextSelectionChanged(TextSelectionEventArgs args)
        {
            InvalidatePaint();
        }

        private void OnDocumentChanged(PdfDocumentEventArgs e)
        {
            OnPropertyChanged(nameof(Document));
            OnPropertyChanged(nameof(PagesCount));
            ((Command)UndoCommand).ChangeCanExecute();
            ((Command)RedoCommand).ChangeCanExecute();
            ((Command)NextPageCommand).ChangeCanExecute();
            ((Command)PrevPageCommand).ChangeCanExecute();
        }

        private void OnOperationsChanged(object sender, EventArgs e)
        {
            OnPropertyChanged(nameof(IsModified));
            ((Command)UndoCommand).ChangeCanExecute();
            ((Command)RedoCommand).ChangeCanExecute();
        }

        public override void OnTouch(TouchEventArgs e)
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
                Operations.ScaleToPointer(e.WheelDelta);
                e.Handled = true;
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
            var newDocument = Document.Reload(Operations);
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

        public void ScrollTo(PdfPage page) => ScrollTo(Document.GetPageView(page));

        public void ScrollTo(IPdfPageViewModel page)
        {
            var location = Operations.FitToAndGetLocation(page);
            AnimateScroll(Math.Max(location.Y, 0), Math.Max(location.X, 0));
        }

        public void ScrollTo(Annotation annotation)
        {
            var location = Operations.GetLocation(annotation);
            AnimateScroll(Math.Max(location.Y, 0), Math.Max(location.X, 0));
        }
    }
}
