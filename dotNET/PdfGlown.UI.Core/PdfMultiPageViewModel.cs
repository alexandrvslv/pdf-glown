using PdfGlown.Documents;
using PdfGlown.Documents.Contents.Scanner;
using PdfGlown.Documents.Interaction.Annotations;
using PdfGlown.Util.Math;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PdfGlown.UI
{
    public class PdfMultiPageViewModel : IPdfPageViewModel
    {
        private List<PdfPageViewModel> items = new();
        private SKMatrix matrix;
        private SKRect? bounds;


        public PdfMultiPageViewModel(PdfMultiDocumentViewModel document, IEnumerable<PdfPageViewModel> pages)
        {
            Document = document;
            foreach (var item in pages)
                Add(item);
        }

        public void Add(PdfPageViewModel item)
        {
            items.Add(item);
            item.BoundsChanged += OnItemBoundsChanged;
        }

        private void OnItemBoundsChanged(object? sender, EventArgs e)
        {
            bounds = null;
            BoundsChanged?.Invoke(this, e);
        }

        public PdfMultiDocumentViewModel Document { get; set; }

        IPdfDocumentViewModel IPdfPageViewModel.Document
        {
            get => Document;
            set => Document = (PdfMultiDocumentViewModel)value;
        }

        public SKRect Bounds
        {
            get => bounds ??= CalculateBounds();
        }

        public int Index { get; set; }

        public SKMatrix Matrix
        {
            get => matrix;
            set
            {
                if (matrix != value)
                {
                    matrix = value;
                    bounds = null;
                }
            }
        }

        public int Order { get; set; }

        public event EventHandler? BoundsChanged;

        private SKRect CalculateBounds()
        {
            var rect = items.First().Bounds;
            foreach (var item in items.Skip(1))
            {
                rect.Add(item.Bounds);
            }
            return rect;
        }

        public bool Draw(SKCanvas canvas, PdfViewState state)
        {
            var result = true;
            foreach (var item in items)
            {
                if (!item.Draw(canvas, state))
                    result = false;
            }
            return result;
        }

        public void Touch(PdfViewState state)
        {
            foreach (var item in items)
            {
                if (item.Bounds.Contains(state.ViewPointerLocation))
                    item.Touch(state);
            }
        }

        public Annotation? GetAnnotation(string name)
        {
            foreach (var item in items)
            {
                if (item.GetAnnotation(name) is Annotation annotation)
                    return annotation;
            }
            return null;
        }

        public IEnumerable<Annotation> Annotations => items.SelectMany(x => x.Annotations);

        public IEnumerable<ITextString> GetStrings() => items.SelectMany(x => x.GetStrings());

        public PdfPage? GetPage(PdfViewState state)
        {
            foreach (var item in items)
            {
                if (item.Bounds.IntersectsWith(state.NavigationArea))
                    return item.Page;
            }
            return null;
        }

        public void Swap(int index1, int index2)
        {
            var temp = items[index1];
            items[index1] = items[index2];
            items[index2] = temp;
        }
    }

}

