using PdfGlown.Documents;
using PdfGlown.Documents.Interaction.Annotations;
using PdfGlown.Documents.Interaction.Forms;
using PdfGlown.Files;
using PdfGlown.UI.Operations;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace PdfGlown.UI
{
    public class PdfDocumentViewModel : IDisposable, IPdfDocumentViewModel, INotifyPropertyChanged
    {
        public static readonly float Indent = 10;
        public static readonly float DoubleIndent = Indent * 2;
        public static PdfDocumentViewModel LoadFrom(string filePath)
        {
            var tempFilePath = GetTempPath(filePath);
            File.Copy(filePath, tempFilePath, true);
            var fileStream = new FileStream(tempFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            return LoadFrom(fileStream, filePath, tempFilePath);
        }

        public static PdfDocumentViewModel LoadFrom(Stream stream, string? filePath = null, string? tempFilePath = null)
        {
            if (string.IsNullOrEmpty(filePath)
               && stream is FileStream fileStream)
            {
                filePath = fileStream.Name;
                tempFilePath = GetTempPath(filePath);
                File.Copy(filePath, tempFilePath, true);
                fileStream.Close();
                stream = new FileStream(tempFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            }
            return new PdfDocumentViewModel(new PdfDocument(stream))
            {
                FilePath = filePath,
                TempFilePath = tempFilePath,
            };
        }

        private readonly List<PdfPageViewModel> pageViews = new();
        private readonly Dictionary<int, PdfPageViewModel> indexCache = new();
        private readonly Dictionary<PdfPage, PdfPageViewModel> pageCache = new();
        private Fields? fields;
        private SKPaint? pageBackgroundPaint;
        private SKColor pageBackgroundColor = SKColors.White;
        private SKRect? bounds;
        private SKSize size;
        private SKMatrix matrix = SKMatrix.Identity;
        private SKPaint? pageForegroundPaint;        

        public PdfDocumentViewModel(PdfDocument document)
        {
            Document = document;
            LoadPages();
        }

        public ManualResetEventSlim LockObject => Document.LockObject;

        public bool IsPaintComplete => LockObject?.IsSet ?? true;

        public PdfDocument Document { get; private set; }

        public string? Name { get; set; }

        public PdfCatalog Catalog => Document.Catalog;

        public PdfPages Pages => Catalog.Pages;

        public int PagesCount => pageViews.Count;

        public IEnumerable<PdfPageViewModel> PageViews => pageViews;

        IEnumerable<IPdfPageViewModel> IPdfDocumentViewModel.PageViews => PageViews;

        public Fields Fields
        {
            get
            {
                if (fields == null || fields != Catalog.Form.Fields)
                {
                    var iniFieldsCount = Catalog.Form.Fields.Count;
                    fields = Catalog.Form.Fields;
                }
                return fields;
            }
        }

        IEnumerable<Field>? IPdfDocumentViewModel.Fields => Fields;

        public string? FilePath { get; private set; }

        public string? TempFilePath { get; set; }

        public SKRect Bounds
        {
            get => bounds ??= CalculateBounds();
        }

        public SKSize Size
        {
            get => size;
            set
            {
                if (size != value)
                {
                    size = value;
                    OnPropertyChanged();
                    OnBoundsChanged();
                }
            }
        }

        public SKMatrix Matrix
        {
            get => matrix;
            internal set
            {
                if (matrix != value)
                {
                    matrix = value;
                    OnPropertyChanged();
                    OnBoundsChanged();
                }
            }
        }

        public float XOffset
        {
            get => matrix.TransX;
            set
            {
                if (XOffset != value)
                {
                    matrix.TransX = value;
                    OnPropertyChanged();
                    OnBoundsChanged();
                }
            }
        }

        public float YOffset
        {
            get => matrix.TransY;
            set
            {
                if (YOffset != value)
                {
                    matrix.TransY = value;
                    OnPropertyChanged();
                    OnBoundsChanged();
                }
            }
        }

        public float AvgHeigth { get; private set; }

        public bool IsClosed => Document == null;

        public SKColor PageBackgroundColor
        {
            get => pageBackgroundColor;
            set
            {
                if (pageBackgroundColor != value)
                {
                    pageBackgroundColor = value;

                    var temp = pageBackgroundPaint;
                    pageBackgroundPaint = null;
                    temp?.Dispose();
                    temp = pageForegroundPaint;
                    pageForegroundPaint = null;
                    temp?.Dispose();

                    if (Catalog != null)
                        Catalog.PageAlpha = value.Alpha == 255 ? null : value.Alpha / 255F;

                    OnPropertyChanged();
                    OnPropertyChanged(nameof(PageAlpha));
                    OnPropertyChanged(nameof(PageBackgroundPaint));
                    OnPropertyChanged(nameof(PageForegroundPaint));

                }
            }
        }

        public byte PageAlpha
        {
            get => PageBackgroundColor.Alpha;
            set => PageBackgroundColor = PageBackgroundColor.WithAlpha(value);
        }

        public SKPaint PageBackgroundPaint => pageBackgroundPaint ??= GetPageBackgroundPaint();

        public SKPaint? PageForegroundPaint => pageForegroundPaint ??= GetPageForegroundPaint();

        public float DefaultXOffset { get; internal set; }

        IPdfPageViewModel IPdfDocumentViewModel.this[int index] => this[index];

        public PdfPageViewModel this[int index]
        {
            get => pageViews[index];
        }

        public event PdfAnnotationEventHandler? AnnotationAdded;

        public event PdfAnnotationEventHandler? AnnotationRemoved;

        public event EventHandler? BoundsChanged;

        public event PropertyChangedEventHandler? PropertyChanged;

        private SKRect CalculateBounds()
        {
            var matrix = Matrix;
            return SKRect.Create(matrix.TransX, matrix.TransY, Size.Width * matrix.ScaleX, Size.Height * matrix.ScaleX);
        }

        private SKPaint GetPageBackgroundPaint()
        {
            return new SKPaint
            {
                Color = PageBackgroundColor,
                Style = SKPaintStyle.Fill
            };
        }

        private SKPaint? GetPageForegroundPaint()
        {
            if (PageAlpha == 255)
                return null;
            return new SKPaint
            {
                ColorFilter = SKColorFilter.CreateBlendMode(PageBackgroundColor, SKBlendMode.DstIn),
            };
        }

        public void LoadPages()
        {
            float totalWidth, totalHeight;

            totalWidth = 0F;
            totalHeight = 0F;
            pageViews.Clear();
            indexCache.Clear();
            pageCache.Clear();
            int order = 0;
            foreach (var page in Pages)
            {
                totalHeight += Indent;
                var box = page.RotatedBox;
                var dpi = 1F;
                var imageSize = new SKSize(box.Width * dpi, box.Height * dpi);
                page.Index = order;
                var pageView = new PdfPageViewModel(this, page)
                {
                    Order = order,
                    Index = order,
                    Size = imageSize
                };
                pageView.Matrix = pageView.Matrix.PostConcat(SKMatrix.CreateTranslation(Indent, totalHeight));
                indexCache[order] = pageView;
                pageCache[page] = pageView;
                pageViews.Add(pageView);
                if (imageSize.Width > totalWidth)
                    totalWidth = imageSize.Width;

                totalHeight += imageSize.Height;
                order++;
            }
            Size = new SKSize(totalWidth + DoubleIndent, totalHeight);

            AvgHeigth = totalHeight / pageViews.Count;

            foreach (var pageView in pageViews)
            {
                var indWidth = pageView.Size.Width + DoubleIndent;
                if (indWidth < Size.Width)
                {
                    pageView.Matrix = pageView.Matrix.PostConcat(SKMatrix.CreateTranslation((Size.Width - indWidth) / 2, 0));
                }
            }
        }

        public PdfDocumentViewModel GetDocumentView(PdfDocument document) => this;

        public PdfPageViewModel? GetPageView(int index) => indexCache.TryGetValue(index, out var pageView) ? pageView : null;

        public PdfPageViewModel? GetPageView(PdfPage page) => pageCache.TryGetValue(page, out var pageView) ? pageView : null;

        private void ClearPages()
        {
            foreach (var pageView in pageViews)
            {
                pageView.Dispose();
            }
            pageViews.Clear();
            indexCache.Clear();
            pageCache.Clear();
        }

        public void Dispose()
        {
            if (Document != null)
            {
                pageBackgroundPaint?.Dispose();
                pageBackgroundPaint = null;
                ClearPages();
                Document.Dispose();
                GC.Collect();
                if (TempFilePath != null)
                {
                    try { File.Delete(TempFilePath); }
                    catch { }
                }
            }
        }

        private static string GetTempPath(string filePath)
        {
            int? index = null;
            var tempPath = "";
            do
            {
                tempPath = $"{filePath}~{index}";
                index = (index ?? 0) + 1;
            }
            while (File.Exists(tempPath));
            return tempPath;
        }        

        public void Save(SerializationModeEnum mode = SerializationModeEnum.Standard)
        {
            if (FilePath == null)
                throw new Exception("FilePath not specified!");
            var path = FilePath;
            var dir = Path.GetDirectoryName(path);
            if (dir == null)
                throw new Exception("FilePath is invalid!");
            var tempPath = Path.Combine(dir, Path.GetFileNameWithoutExtension(path) + ".tmp~");
            Save(tempPath, mode);
            File.Copy(tempPath, path, true);
        }

        public void Save(string path)
        {
            Save(path, GetMode());
        }

        public void Save(string path, SerializationModeEnum mode)
        {
            Document.Save(path, mode);
        }

        public void SaveTo(Stream stream)
        {
            SaveTo(stream, GetMode());
        }

        public void SaveTo(Stream stream, SerializationModeEnum mode)
        {
            Document.Save(stream, mode);
        }

        private SerializationModeEnum GetMode()
        {
            return Catalog.HasSignatures
                ? SerializationModeEnum.Incremental
                : SerializationModeEnum.Standard;
        }

        public Field GetField(string name)
        {
            return Fields[name];
        }

        public Field AddField(Field field)
        {
            if (!Fields.ContainsKey(field.FullName))
            {
                Fields.Add(field);
            }
            return field;
        }

        public IEnumerable<Annotation> GetAllAnnotations() => PageViews.SelectMany(x => x.Annotations);

        public Annotation? FindAnnotation(string name, int? pageIndex = null)
        {
            var annotation = (Annotation?)null;
            if (pageIndex == null)
            {
                foreach (var pageView in PageViews)
                {
                    annotation = pageView.GetAnnotation(name);
                    if (annotation != null)
                        return annotation;
                }
            }
            else
            {
                var pageView = GetPageView((int)pageIndex);
                return pageView?.GetAnnotation(name);
            }
            return null;
        }

        public List<Annotation> AddAnnotation(Annotation annotation)
        {
            return AddAnnotation(annotation.Page, annotation);
        }

        public List<Annotation> AddAnnotation(PdfPage page, Annotation annotation)
        {
            var list = new List<Annotation>();
            if (page != null)
            {
                annotation.Page = page;

                if (annotation is Widget widget
                    && widget.NewField != null)
                {
                    AddField(widget.NewField);
                }
                if (!page.Annotations.Contains(annotation))
                {
                    page.Annotations.Add(annotation);
                    list.Add(annotation);
                }
                AnnotationAdded?.Invoke(new PdfAnnotationEventArgs(annotation));
                foreach (var item in annotation.Replies)
                {
                    if (item is Markup markup)
                    {
                        list.AddRange(AddAnnotation(page, item));
                    }
                }
            }
            if (annotation is Popup popup
                && popup.Parent != null)
            {
                list.AddRange(AddAnnotation(popup.Parent));
            }
            return list;
        }

        public List<Annotation> RemoveAnnotation(Annotation annotation)
        {
            var list = new List<Annotation>();

            if (annotation.Page != null)
            {
                foreach (var item in annotation.Page.Annotations.ToList())
                {
                    if (item is Markup markup
                        && markup.ReplyTo == annotation)//&& markup.ReplyType == Markup.ReplyTypeEnum.Thread
                    {
                        annotation.Replies.Add(item);
                        list.AddRange(RemoveAnnotation(markup));
                    }
                }
            }
            if (annotation is Widget widget
                && widget.NewField != null)
            {
                Fields.Remove(widget.NewField);
            }

            annotation.Remove();

            AnnotationRemoved?.Invoke(new PdfAnnotationEventArgs(annotation));
            if (annotation is Popup popup)
            {
                list.AddRange(RemoveAnnotation(popup.Parent));
            }
            list.Add(annotation);

            return list;
        }

        public IPdfDocumentViewModel Reload(EditorOperations operations)
        {
            if(FilePath == null)
                throw new ArgumentNullException(nameof(FilePath));
            var newDocument = LoadFrom(FilePath);
            if (operations.Count > 0 && !IsClosed)
            {
                var newOperations = new LinkedList<EditOperation>();
                foreach (var oldOperation in operations.Items)
                {
                    if (oldOperation.Document != this)
                        continue;
                    var newOperation = oldOperation.Clone(newDocument);

                    newOperation.Redo();

                    newOperations.AddLast(newOperation);
                }
                operations.Items = newOperations;
            }
            return newDocument;
        }

        public bool ContainsField(string fieldName) => Fields.ContainsKey(fieldName);

        public void UpdateYOffset(PdfPageViewModel page, float yOffset)
        {
            var diff = yOffset - page.YOffset;
            for (var i = pageViews.IndexOf(page); i < pageViews.Count; i++)
                pageViews[i].YOffset += diff;
            Size = new SKSize(Size.Width, Size.Height + diff);
        }

        private void OnBoundsChanged()
        {
            bounds = null;
            BoundsChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
