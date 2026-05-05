using PdfGlown.Documents;
using PdfGlown.Documents.Interaction.Forms;
using PdfGlown.UI.Operations;
using PdfGlown.Util.Math;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace PdfGlown.UI
{
    public class PdfMultiDocumentViewModel : IPdfDocumentViewModel
    {
        private ObservableCollection<PdfDocumentViewModel> items = new();
        private List<PdfMultiPageViewModel> pages = new();
        private SKSize? size;

        public PdfMultiDocumentViewModel() { }

        public PdfMultiDocumentViewModel(IEnumerable<PdfDocumentViewModel> documents)
        {
            foreach (var document in documents)
            {
                Add(document);
            }
            LoadPages();
        }

        public float AvgHeigth { get; private set; }

        public bool IsClosed => items.All(x => x.IsClosed);

        public bool IsPaintComplete => items.All(x => x.IsPaintComplete);

        public IEnumerable<IPdfPageViewModel> PageViews => pages;

        public SKRect Bounds => SKRect.Create(Size);

        public SKSize Size => size ??= CalculateSize();

        public int PagesCount => pages.Count;

        public IEnumerable<Field>? Fields => items.SelectMany(x => (IEnumerable<Field>)x.Fields);

        public ObservableCollection<PdfDocumentViewModel> Items { get => items; }

        public IPdfPageViewModel this[int index] => pages[index];

        public event PdfAnnotationEventHandler? AnnotationAdded;

        public event PdfAnnotationEventHandler? AnnotationRemoved;

        public event EventHandler? BoundsChanged;

        public void Add(PdfDocumentViewModel document)
        {
            items.Add(document);
            Subscribe(document);
        }

        private void Subscribe(PdfDocumentViewModel document)
        {
            document.PropertyChanged += OnItemPropertyChanged;
            document.BoundsChanged += OnItemBoundsChanged;            
            document.AnnotationAdded += OnItemAnnotationAdded;
            document.AnnotationRemoved += OnItemAnnotationRemoved;
        }

        private void Unsubscribe(PdfDocumentViewModel document)
        {
            document.PropertyChanged -= OnItemPropertyChanged;
            document.BoundsChanged -= OnItemBoundsChanged;
            document.AnnotationAdded -= OnItemAnnotationAdded;
            document.AnnotationRemoved -= OnItemAnnotationRemoved;
        }

        private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            BoundsChanged?.Invoke(this, e);
        }

        private void OnItemBoundsChanged(object? sender, EventArgs e)
        {
            size = null;
            BoundsChanged?.Invoke(this, e);
        }

        private void OnItemAnnotationRemoved(PdfAnnotationEventArgs e) => AnnotationRemoved?.Invoke(e);

        private void OnItemAnnotationAdded(PdfAnnotationEventArgs e) => AnnotationAdded?.Invoke(e);

        public PdfDocumentViewModel? GetDocumentView(PdfDocument document) => items.FirstOrDefault(x => x.Document == document);

        public PdfPageViewModel? GetPageView(PdfPage page) => GetDocumentView(page.Document)?.GetPageView(page);

        private SKSize CalculateSize()
        {
            var rect = items.First().Bounds;
            foreach (var item in items.Skip(1))
            {
                rect.Add(item.Bounds);
            }
            return rect.Size;
        }

        public void LoadPages()
        {
            pages.Clear();
            var maxCount = items.Max(x => x.PagesCount);
            for (int i = 0; i < maxCount; i++)
            {
                var iPages = items.Where(item => item.PagesCount > i).Select(x => x[i]);
                var maxYOffset = iPages.Max(x => x.YOffset);
                foreach (var page in iPages.Where(x => x.YOffset < maxYOffset))
                {
                    page.Document.UpdateYOffset(page, maxYOffset);
                }

                pages.Add(new PdfMultiPageViewModel(this, iPages)
                {
                    Order = i,
                    Index = i,
                });
            }
            var xOffset = 1F;
            foreach (var item in items)
            {
                item.DefaultXOffset =
                    item.XOffset = xOffset;

                xOffset += item.Size.Width + PdfDocumentViewModel.Indent;
            }

            AvgHeigth = items.Average(x => x.AvgHeigth);
        }

        public IPdfDocumentViewModel Reload(EditorOperations operations)
        {
            return new PdfMultiDocumentViewModel(items.Select(item => (PdfDocumentViewModel)item.Reload(operations)));
        }

        public void Dispose()
        {
            foreach (var item in items)
            {
                Unsubscribe(item);
                item.Dispose();
            }
            items.Clear();
        }

        public bool ContainsField(string fieldName) => items.Any(x => x.ContainsField(fieldName));

        public void Swap(int index1, int index2)
        {
            items.Move(index1, index2);
            foreach (var page in pages)
            {
                page.Swap(index1, index2);
            }
            BoundsChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
