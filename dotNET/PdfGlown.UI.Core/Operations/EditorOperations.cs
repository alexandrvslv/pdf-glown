using PdfGlown.Documents.Interaction.Annotations;
using PdfGlown.Documents.Interaction.Annotations.ControlPoints;
using PdfGlown.UI.ToolTip;
using PdfGlown.Util.Invokers;
using PdfGlown.Util.Math;
using SkiaSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace PdfGlown.UI.Operations
{
    public class EditorOperations : IEnumerable<EditOperation>
    {
        private LinkedList<EditOperation> operations = new();
        private LinkedListNode<EditOperation>? lastLink;
        private OperationType currentType;
        private Markup? selectedMarkup;
        private Annotation? selectedAnnotation;
        private Annotation? hoverAnnotation;
        private ControlPoint? selectedPoint;
        private ControlPoint? hoverPoint;
        private bool handlePropertyChanged = true;
        private IPdfDocumentViewModel? document;
        private IPdfPageViewModel? currentPage;
        private IPdfPageViewModel? newPage;
        private Annotation? toolTipAnnotation;
        private ToolTipRenderer? toolTipRenderer;
        private SKRect? toolTipBound;
        private FloatEventArgs cacheScaleArgs = new(0F);
        private PdfPageEventArgs cacheCurrentArgs = new(null);
        private PdfViewState state;
        private SKPoint translateLocation;

        public EditorOperations(IPdfView pdfView)
        {
            Viewer = pdfView;
            state = new PdfViewState(pdfView);
        }

        public int Count => operations.Count;

        public IPdfView Viewer { get; set; }

        public bool HashOperations => lastLink != null;

        public bool CanRedo => (lastLink == null ? operations.First : lastLink?.Next) != null;

        public bool CanUndo => lastLink != null;

        public OperationType Current
        {
            get => currentType;
            set
            {
                if (currentType != value)
                {
                    var oldValue = currentType;
                    currentType = value;
                    OnCurrentOperationChanged(oldValue, value);
                }
            }
        }

        public LinkedList<EditOperation> Items
        {
            get => operations;
            set => operations = value;
        }

        public ControlPoint? SelectedPoint
        {
            get => selectedPoint;
            set
            {
                if (selectedPoint != value)
                {
                    selectedPoint = value;
                    if (selectedPoint != null)
                    {
                        SelectedAnnotation = selectedPoint.Annotation;
                    }
                    else
                    {
                        Current = OperationType.None;
                    }
                }
            }
        }

        public ControlPoint? HoverPoint
        {
            get => hoverPoint;
            set
            {
                if (hoverPoint != value)
                {
                    hoverPoint = value;
                    if (hoverPoint != null)
                    {
                        Viewer.Cursor = CursorType.Cross;
                    }
                    else
                    {
                        Viewer.Cursor = CursorType.Arrow;
                    }
                }
            }
        }

        public Annotation? HoverAnnotation
        {
            get => hoverAnnotation;
            set
            {
                if (hoverAnnotation != value)
                {
                    hoverAnnotation = value;
                    Viewer.InvalidatePaint();
                }
            }
        }

        public Annotation? SelectedAnnotation
        {
            get => selectedAnnotation;
            set
            {
                if (selectedAnnotation != value)
                {
                    var oldValue = selectedAnnotation;
                    selectedAnnotation = value;
                    SelectedPoint = null;
                    SelectedMarkup = value as Markup;
                    Current = OperationType.None;
                    if (oldValue != null)
                    {
                        SuspendAnnotationPropertyHandler(oldValue);
                    }
                    if (value != null)
                    {
                        ResumeAnnotationPropertyHandler(value);
                        if (value.IsNew)
                        {
                            if (value is TextMarkup
                                || (value is StickyNote note
                                && note.ReplyTo != null))
                            {
                                AddAnnotation(value).EndOperation();
                            }
                            else
                            {
                                Current = OperationType.AnnotationDrag;
                            }
                        }
                    }
                    SelectedAnnotationChanged?.Invoke(new PdfAnnotationEventArgs(value));
                    Viewer.InvalidatePaint();
                }
            }
        }

        public Markup? SelectedMarkup
        {
            get => selectedMarkup;
            set
            {
                if (selectedMarkup != value)
                {
                    selectedMarkup = value;
                    if (value != null)
                    {
                        SelectedAnnotation = value;
                    }
                }
            }
        }

        public IPdfDocumentViewModel? Document
        {
            get => document;
            set
            {
                if (document != value)
                {
                    if (document != null)
                    {
                        document.AnnotationAdded -= OnDocumentAnnotationAdded;
                        document.AnnotationRemoved -= OnDocumentAnnotationRemoved;
                        document.BoundsChanged -= OnDocumentBoundsChanged;
                    }
                    document = value;

                    SelectedAnnotation = null;
                    SelectedPoint = null;
                    HoverAnnotation = null;
                    HoverPoint = null;
                    ToolTipAnnotation = null;
                    UpdateMaximums();
                    if (document != null)
                    {
                        document.AnnotationAdded += OnDocumentAnnotationAdded;
                        document.AnnotationRemoved += OnDocumentAnnotationRemoved;
                        document.BoundsChanged += OnDocumentBoundsChanged;
                        CurrentPage = document.PageViews.FirstOrDefault();
                        if (currentPage != null)
                            Viewer.ScrollTo(currentPage);
                    }
                    else
                    {
                        Viewer.InvalidatePaint();
                    }
                    PagesCount = document?.PagesCount ?? 0;
                    DocumentChanged?.Invoke(new PdfDocumentEventArgs(value));
                }
            }
        }

        public ToolTipRenderer? ToolTipRenderer
        {
            get => toolTipRenderer;
            set
            {
                if (toolTipRenderer != value)
                {
                    toolTipRenderer?.Dispose();
                    toolTipRenderer = value;
                    toolTipBound = value?.GetWindowBound(state);
                }
            }
        }

        public Annotation? ToolTipAnnotation
        {
            get => toolTipAnnotation;
            set
            {
                if (toolTipAnnotation != value)
                {
                    toolTipAnnotation = value;
                    ToolTipRenderer = toolTipAnnotation switch
                    {
                        Markup markup => new MarkupToolTipRenderer(markup),
                        Link link => new LinkToolTipRenderer(link),
                        _ => null,
                    };
                    ToolTipRenderer?.Measure();
                    Viewer.InvalidatePaint();
                }
            }
        }

        public int PagesCount { get; private set; }

        public IPdfPageViewModel? CurrentPage
        {
            get
            {
                if (currentPage == null
                    || currentPage.Document != Document)
                {
                    CurrentPage = GetCenterPage(state);
                }
                return currentPage;
            }
            set
            {
                if (currentPage != value)
                {
                    OnCurrentPageChanged(value);
                }
            }
        }

        public int CurrentPageNumber
        {
            get => (CurrentPage?.Index ?? -1) + 1;
            set
            {
                if (Document == null
                    || PagesCount == 0)
                {
                    return;
                }
                int index = GetPageIndex(value);
                if ((index + 1) != CurrentPageNumber)
                {
                    CurrentPage = Document[index];
                }
            }
        }

        public IPdfPageViewModel? NewPage
        {
            get => newPage;
            private set
            {
                if (newPage != value)
                {
                    newPage = value;
                    if (newPage != null)
                        Viewer.ScrollTo(newPage);
                }
            }
        }

        public int NewPageNumber
        {
            get => NewPage != null
                ? NewPage.Index + 1
                : CurrentPageNumber;
            set
            {
                if (Document == null
                    || PagesCount == 0)
                {
                    return;
                }
                int index = GetPageIndex(value);
                if ((index + 1) != CurrentPageNumber)
                {
                    NewPage = Document[index];
                }
            }
        }

        public float Scale
        {
            get => state.Scale;
            set
            {
                if (Scale != value)
                {
                    state.Scale = value;
                    UpdateMaximums();
                    cacheScaleArgs.Value = value;
                    ScaleChanged?.Invoke(cacheScaleArgs);
                }
            }
        }

        public PdfViewState State => state;

        public event PdfDocumentEventHandler? DocumentChanged;

        public event EventHandler<DetailedPropertyChangedEventArgs>? AnnotationPropertyChanged;

        public event PdfAnnotationEventHandler? AnnotationAdded;

        public event PdfAnnotationEventHandler? AnnotationRemoved;

        public event PdfAnnotationEventHandler? SelectedAnnotationChanged;

        public event EventHandler? Changed;

        public event PdfAnnotationEventHandler? CheckCanRemove;

        public event OperationEventHandler? FinishOperation;

        public event PdfPageEventHandler? CurrentPageChanged;

        public event FloatEventHandler? ScaleChanged;

        public void NextPage() => NewPageNumber += 1;

        public bool CanNextPage() => CurrentPageNumber < PagesCount;

        public void PrevPage() => NewPageNumber -= 1;

        public bool CanPrevPage() => CurrentPageNumber > 1;

        public int GetDisplayPageIndex() => GetDisplayPageIndex(state);

        public int GetDisplayPageIndex(PdfViewState state)
        {
            var verticalValue = -(state.NavigationMatrix.TransY);
            var page = Document?.PageViews.FirstOrDefault(p => (p.Bounds.Bottom * state.NavigationMatrix.ScaleY) > verticalValue);
            return page?.Order ?? 0;
        }

        private int GetPageIndex(int value)
        {
            var index = value - 1;
            if (index < 0)
            {
                index = PagesCount - 1;
            }
            else if (index >= PagesCount)
            {
                index = 0;
            }

            return index;
        }

        private void OnCurrentPageChanged(IPdfPageViewModel? page)
        {
            if (newPage != null && newPage == page)
            {
                NewPage = null;
            }
            currentPage = page;
            cacheCurrentArgs.Page = page;
            CurrentPageChanged?.Invoke(cacheCurrentArgs);
        }

        public IPdfPageViewModel? GetCenterPage() => GetCenterPage(state);
        public IPdfPageViewModel? GetCenterPage(PdfViewState state)
        {
            if (Document is not IPdfDocumentViewModel document)
                return null;
            var area = state.WindowArea;
            area.Inflate(-area.Width / 3F, -area.Height / 3F);
            area = state.InvertNavigationMatrix.MapRect(area);
            for (int i = GetDisplayPageIndex(state); i < document.PagesCount; i++)
            {
                var pageView = document[i];
                if (pageView.Bounds.IntersectsWith(area))
                {
                    return pageView;
                }
            }
            return document.PageViews.FirstOrDefault();
        }

        private bool CanDragByPointer() => Viewer.ScrollByPointer
                && state.TouchButton == MouseButton.Left
                && Viewer.Cursor == CursorType.Arrow;

        private bool TouchTranslate()
        {
            if (state.TouchAction == TouchAction.Pressed)
            {
                translateLocation = state.PointerLocation;
                if (state.TouchButton == MouseButton.Middle)
                    Viewer.Cursor = CursorType.SizeAll;
                return true;
            }
            else if (state.TouchAction == TouchAction.Moved)
            {
                var vector = state.PointerLocation - translateLocation;
                Viewer.HValue -= vector.X;
                Viewer.VValue -= vector.Y;
                translateLocation = state.PointerLocation;
                return true;
            }
            return false;
        }

        public void OnTouch(TouchAction actionType, MouseButton mouseButton, SKPoint location)
        {
            state.TouchAction = actionType;
            state.TouchButton = mouseButton;
            state.PointerLocation = location;
            try
            {
                if (mouseButton == MouseButton.Middle
                    && TouchTranslate())
                {
                    return;
                }
                if (Document == null
                    || !Document.IsPaintComplete)
                {
                    return;
                }
                if (Touch(state))
                {
                    return;
                }
                if (Viewer.Operations.Current != OperationType.AnnotationDrag)
                    Viewer.Cursor = CursorType.Arrow;
                state.PageView = null;
            }
            finally
            {
                if (CanDragByPointer())
                {
                    TouchTranslate();
                }
            }
        }

        public bool Touch(PdfViewState state)
        {
            if (Document == null)
                return false;
            for (int i = GetDisplayPageIndex(state); i < Document.PagesCount; i++)
            {
                var pageView = Document[i];
                if (pageView.Bounds.Contains(state.ViewPointerLocation))
                {
                    pageView.Touch(state);
                    return true;
                }
            }
            return false;
        }

        public void Draw(SKCanvas canvas)
        {
            Draw(canvas, state);
        }

        public void Draw(SKCanvas canvas, PdfViewState state)
        {
            if (document == null)
                return;

            canvas.Save();
            canvas.SetMatrix(state.ViewMatrix);
            for (int i = GetDisplayPageIndex(state); i < document.PagesCount; i++)
            {
                var pageView = document[i];
                var pageBounds = pageView.Bounds;
                if (pageBounds.IntersectsWith(state.NavigationArea))
                {
                    pageView.Draw(canvas, state);
                }
                else if (pageBounds.Top > state.NavigationArea.Bottom)
                {
                    break;
                }
            }
            //DrawSelectionRect();
            DrawAnnotationToolTip(canvas, state);
            canvas.Restore();

        }

        private void DrawSelectionRect(SKCanvas canvas, PdfViewState state, Quad selectionRect)
        {
            if (selectionRect.Width > 0
                && selectionRect.Height > 0)
            {
                canvas.DrawRect(selectionRect.GetBounds(), DefaultSKStyles.PaintSelectionRect);
            }
        }

        public void ScaleToPointer(float delta) => ScaleToPointer(delta, state.PointerLocation);

        public void ScaleToPointer(float delta, SKPoint pointerLocation)
        {
            var scaleStep = 0.06F * Math.Sign(delta);
            var newScale = Scale + scaleStep + scaleStep * Scale;
            SetScale(newScale, pointerLocation);
        }

        public void SetScale(float newScale) => SetScale(newScale, new SKPoint(state.WindowArea.Width / 2F, state.WindowArea.Height / 2));

        public void SetScale(float newScale, SKPoint pointerLocation)
        {
            newScale = NormalizeScale(newScale);
            if (newScale != Scale)
            {
                var unscaleLocations = pointerLocation.UnScale(state.XScale, state.YScale);
                var oldSpacePoint = state.InvertNavigationMatrix.MapPoint(unscaleLocations);

                Scale = newScale;

                var newCurrentLocation = state.NavigationMatrix.MapPoint(oldSpacePoint);

                var vector = newCurrentLocation - unscaleLocations;
                if (Viewer.HBarVisible)
                {
                    Viewer.HValue += vector.X;
                }
                if (Viewer.VBarVisible)
                {
                    Viewer.VValue += vector.Y;
                }
            }
        }

        private static float NormalizeScale(float newScale)
        {
            if (newScale < 0.01F)
                newScale = 0.01F;
            if (newScale > 60F)
                newScale = 60F;
            return newScale;
        }

        public SKPoint FitToAndGetLocation(IPdfPageViewModel page)
        {
            if (page == null || Document == null)
            {
                return SKPoint.Empty;
            }
            var windowArea = state.WindowArea;
            switch (Viewer.FitMode)
            {
                case PdfViewFitMode.MaxWidth:
                    Scale = (float)windowArea.Width / Document.Size.Width;
                    break;
                case PdfViewFitMode.PageHeight:
                    Scale = (float)windowArea.Height / (page.Bounds.Height + 6);
                    break;
                case PdfViewFitMode.PageWidth:
                    Scale = (float)windowArea.Width / (page.Bounds.Width + 20);
                    break;
                case PdfViewFitMode.PageSize:
                    var vScale = (float)windowArea.Height / (page.Bounds.Height + 6);
                    var hScale = (float)windowArea.Width / (page.Bounds.Width + 20);
                    Scale = hScale < vScale ? hScale : vScale;
                    break;
            }

            var bound = state.ScaleMatrix.MapRect(page.Bounds);
            return new SKPoint(bound.Left - (windowArea.Width / 2 - bound.Width / 2),
                               bound.Height <= windowArea.Height
                                ? bound.Top - (windowArea.Height / 2 - bound.Height / 2)
                                : bound.Top - 3);
        }

        public SKPoint GetLocation(Annotation annotation)
        {
            if (annotation?.Page == null
                || Document == null
                || Document.GetPageView(annotation.Page) is not PdfPageViewModel pageView)
            {
                return SKPoint.Empty;
            }

            var matrix = SKMatrix.CreateScale(Scale, Scale)
                .PreConcat(pageView.Matrix)
                .PreConcat(pageView.Document.Matrix);
            var bound = annotation.GetViewBounds(matrix);
            return new SKPoint(bound.Left - (state.WindowArea.Width / 2 - bound.Width / 2),
                               bound.Top - (state.WindowArea.Height / 2 - bound.Height / 2));
        }

        public void UpdateMaximums()
        {
            UpdateNavigationMatrix();
            var size = Document?.Size ?? SKSize.Empty;
            Viewer.HMaximum = size.Width * Scale;
            Viewer.VMaximum = size.Height * Scale;
        }

        public void OnSizeAllocated(SKRect bound, float scale)
        {
            if (bound != state.WindowArea
                || state.XScale != scale)
            {
                state.SetWindowMatrix(scale, scale, bound.Left, bound.Top);
                state.WindowArea = bound;
                UpdateNavigationMatrix();
            }
        }

        public void UpdateNavigationMatrix()
        {
            var dSize = Document?.Size ?? SKSize.Empty;
            var wSize = state.WindowArea.Size;
            var maximumWidth = dSize.Width * Scale;
            var maximumHeight = dSize.Height * Scale;
            var dx = 0F; var dy = 0F;
            if (maximumWidth < wSize.Width)
            {
                dx = (float)((wSize.Width - 10) - maximumWidth) / 2;
            }

            if (maximumHeight < wSize.Height)
            {
                dy = (float)(wSize.Height - maximumHeight) / 2;
            }
            state.NavigationMatrix = new SKMatrix(
                Scale, 0, (-(float)Viewer.HValue) + dx,
                0, Scale, (-(float)Viewer.VValue) + dy,
                0, 0, 1);
            Viewer.InvalidatePaint();
        }

        private void OnDocumentBoundsChanged(object? sender, EventArgs e)
        {
            UpdateMaximums();
        }

        private void OnDocumentAnnotationAdded(PdfAnnotationEventArgs e)
        {
            AnnotationAdded?.Invoke(e);
            Viewer.InvalidatePaint();
        }

        private void OnDocumentAnnotationRemoved(PdfAnnotationEventArgs e)
        {
            if (e.Annotation == SelectedAnnotation)
                SelectedAnnotation = null;
            AnnotationRemoved?.Invoke(e);
            Viewer.InvalidatePaint();
        }

        private void SuspendAnnotationPropertyHandler(Annotation annotation)
        {
            if (annotation != null)
            {
                annotation.PropertyChanged -= OnAnnotationPropertyChanged;
            }
        }

        private void ResumeAnnotationPropertyHandler(Annotation annotation)
        {
            if (annotation != null)
            {
                annotation.PropertyChanged += OnAnnotationPropertyChanged;
            }
        }

        private void UpdateModificationDate(Annotation annotation)
        {
            try
            {
                handlePropertyChanged = false;
                annotation.ModificationDate = DateTime.UtcNow;
            }
            finally
            {
                handlePropertyChanged = true;
            }
        }

        public void CloseVertextShape(VertexShape vertexShape)
        {
            vertexShape.QueueRefreshAppearance();
            SelectedPoint = null;
            Current = OperationType.None;
        }

        private void OnAnnotationPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (!handlePropertyChanged
                || sender is not Annotation annotation)
            {
                return;
            }

            if (!string.Equals(e.PropertyName, nameof(Markup.ModificationDate), StringComparison.Ordinal))
            {
                UpdateModificationDate(annotation);
            }
            var details = (DetailedPropertyChangedEventArgs)e;
            var type = annotation.GetType();
            if (e.PropertyName == null
                || !Invoker.TryGetPropertyInvoker(type, e.PropertyName, out var invoker))
            {
                return;
            }
            switch (e.PropertyName)
            {
                case nameof(Markup.SKColor):
                    var colorDetails = (DetailedPropertyChangedEventArgs<SKColor>)details;
                    BeginOperation(annotation, OperationType.AnnotationProperty, invoker, colorDetails.OldValue, colorDetails.NewValue);
                    break;
                case nameof(Markup.Contents):
                case nameof(Markup.Subject):
                case nameof(Markup.RichContents):
                case nameof(Markup.DefaultStyle):
                    var stringDetails = (DetailedPropertyChangedEventArgs<string>)details;
                    BeginOperation(annotation, OperationType.AnnotationProperty, invoker, stringDetails.OldValue, stringDetails.NewValue);
                    break;
                case nameof(Markup.Border):
                    var borderDetails = (DetailedPropertyChangedEventArgs<Border>)details;
                    BeginOperation(annotation, OperationType.AnnotationProperty, invoker, borderDetails.OldValue, borderDetails.NewValue);
                    break;
                case nameof(Markup.Popup):
                    var popupDetails = (DetailedPropertyChangedEventArgs<Popup>)details;
                    BeginOperation(annotation, OperationType.AnnotationProperty, invoker, popupDetails.OldValue, popupDetails.NewValue);
                    break;
                case nameof(Markup.ReplyTo):
                    var annotationDetails = (DetailedPropertyChangedEventArgs<Annotation>)details;
                    BeginOperation(annotation, OperationType.AnnotationProperty, invoker, annotationDetails.OldValue, annotationDetails.NewValue);
                    break;
                case nameof(Markup.ReplyType):
                    var replyTypeDetails = (DetailedPropertyChangedEventArgs<ReplyTypeEnum?>)details;
                    BeginOperation(annotation, OperationType.AnnotationProperty, invoker, replyTypeDetails.OldValue, replyTypeDetails.NewValue);
                    break;
                case nameof(Markup.BorderEffect):
                    var borderEffectDetails = (DetailedPropertyChangedEventArgs<BorderEffect>)details;
                    BeginOperation(annotation, OperationType.AnnotationProperty, invoker, borderEffectDetails.OldValue, borderEffectDetails.NewValue);
                    break;
                case nameof(Line.StartStyle):
                case nameof(Line.EndStyle):
                    var lineEndDetails = (DetailedPropertyChangedEventArgs<LineEndStyleEnum>)details;
                    BeginOperation(annotation, OperationType.AnnotationProperty, invoker, lineEndDetails.OldValue, lineEndDetails.NewValue);
                    break;
            }
            AnnotationPropertyChanged?.Invoke(annotation, details);
            Viewer.InvalidatePaint();
        }

        public bool Undo()
        {
            if (lastLink != null)
            {
                var operation = lastLink.Value;
                lastLink = lastLink.Previous;
                try
                {
                    handlePropertyChanged = false;
                    operation.Undo();
                }
                finally
                {
                    handlePropertyChanged = true;
                }
                OnChanged();
                Viewer.InvalidatePaint();
                return true;
            }
            return false;
        }

        public bool Redo()
        {
            var operationLink = lastLink == null ? operations.First : lastLink?.Next;
            if (operationLink != null)
            {
                lastLink = operationLink;
                var operation = lastLink.Value;
                try
                {
                    handlePropertyChanged = false;
                    operation.Redo();
                }
                finally
                {
                    handlePropertyChanged = true;
                }
                OnChanged();
                Viewer.InvalidatePaint();
                return true;
            }
            return false;
        }

        public void EndOperation()
        {
            lastLink?.Value.EndOperation();
        }

        public void CancalOperation(EditOperation operation)
        {
            lastLink = operations.Find(operation);
            Undo();
        }

        public void RejectAll()
        {
            while (Undo()) { };
        }

        public IEnumerable<EditOperation> GetOperations()
        {
            return operations.Select(x => x);
        }

        public void ClearOperations()
        {
            operations.Clear();
            lastLink = null;
            SelectedAnnotation = null;
            OnChanged();
        }

        private void OnChanged()
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }

        public AnnotationOperation AddAnnotation(Annotation newValue)
        {
            var operation = BeginOperation(newValue, OperationType.AnnotationAdd);
            newValue.IsNew = false;
            return operation;
        }

        public IEnumerable<Annotation>? RemoveAnnotation(Annotation annotation)
        {
            if (!OnCheckCanRemove(annotation))
            {
                return null;
            }
            var operation = BeginOperation(annotation, OperationType.AnnotationRemove);
            if (annotation == SelectedAnnotation)
            {
                SelectedAnnotation = null;
            }

            var list = operation.EndOperation() as List<Annotation>;

            if (SelectedAnnotation is Annotation sAnnotation
                && (list?.Contains(sAnnotation) ?? false))
                SelectedAnnotation = null;

            AnnotationRemoved?.Invoke(new PdfAnnotationEventArgs(annotation));
            Viewer.InvalidatePaint();
            return list;
        }

        private bool OnCheckCanRemove(Annotation annotation)
        {
            if (CheckCanRemove != null)
            {
                var args = new PdfAnnotationEventArgs(annotation);
                CheckCanRemove(args);
                return !args.Cancel;
            }
            return true;
        }

        public void OnEndOperation(EditOperation operation, object? result)
        {
            FinishOperation?.Invoke(new OperationEventArgs(operation, result));
            OnChanged();
        }

        public AnnotationOperation BeginOperation(Annotation annotation, OperationType type, object? property = null, object? begin = null, object? end = null)
        {
            if (Document?.GetDocumentView(annotation.Document) is not PdfDocumentViewModel document)
                throw new InvalidOperationException();
            var operation = new AnnotationOperation(annotation, document, this)
            {
                Type = type,
                Property = property,
                OldValue = begin,
                NewValue = end
            };
            if (type == OperationType.AnnotationDrag
                || type == OperationType.AnnotationSize)
            {
                operation.OldValue = annotation.GetViewBounds();
            }
            if (property is ControlPoint controlPoint)
            {
                operation.OldValue = controlPoint.MappedPoint;
            }
            EnqueuOperation(operation);
            if (end != null)
            {
                OnChanged();
            }
            return operation;
        }

        private void EnqueuOperation(AnnotationOperation operation)
        {
            if (lastLink == null)
            {
                operations.Clear();
                lastLink = operations.AddFirst(operation);
            }
            else
            {
                var next = lastLink;
                while (next?.Next != null)
                {
                    next = next.Next;
                }
                while (next != null && next != lastLink)
                {
                    next = next.Previous;
                    if (next?.Next != null)
                        operations.Remove(next.Next);
                }
                lastLink = operations.AddAfter(lastLink, operation);
            }
        }

        private void OnCurrentOperationChanged(OperationType oldValue, OperationType newValue)
        {
            var selectedAnnotation = SelectedAnnotation;
            var lastOperation = lastLink?.Value;
            if (selectedAnnotation != null
                && lastOperation is AnnotationOperation annotationOperation
                && annotationOperation.Annotation == selectedAnnotation)
            {
                switch (oldValue)
                {
                    case OperationType.AnnotationAdd:
                    case OperationType.AnnotationDrag:
                    case OperationType.AnnotationSize:
                    case OperationType.PointMove:
                    case OperationType.PointAdd:
                    case OperationType.PointRemove:
                        lastOperation.EndOperation();
                        break;
                }
            }
            if (selectedAnnotation == null
                && newValue != OperationType.None)
            {
                throw new InvalidOperationException("SelectedAnnotation is not specified!");
            }
            if (selectedAnnotation == null)
            {
                Viewer.Cursor = CursorType.Arrow;
                return;
            }
            switch (newValue)
            {
                case OperationType.AnnotationDrag:
                    if (!selectedAnnotation.IsNew)
                    {
                        BeginOperation(selectedAnnotation, newValue, "Box");
                    }
                    else
                    {
                        Viewer.Cursor = CursorType.Cross;
                    }
                    break;
                case OperationType.AnnotationSize:
                    BeginOperation(selectedAnnotation, newValue, "Box");
                    Viewer.Cursor = CursorType.BottomRightCorner;
                    break;
                case OperationType.PointMove:
                case OperationType.PointAdd:
                case OperationType.PointRemove:
                    if (SelectedPoint == null)
                    {
                        throw new InvalidOperationException("SelectedPoint is not specified!");
                    }
                    BeginOperation(selectedAnnotation, newValue, SelectedPoint);
                    Viewer.Cursor = CursorType.Cross;
                    break;
            }
        }

        public bool OnKeyDown(string keyName, KeyModifiers modifiers)
        {
            if (string.Equals(keyName, "Delete", StringComparison.OrdinalIgnoreCase))
            {
                if (!Viewer.IsReadOnly)
                {
                    if (SelectedPoint is IndexControlPoint indexControlPoint)
                    {
                        BeginOperation(indexControlPoint.Annotation, OperationType.PointRemove, indexControlPoint, indexControlPoint.MappedPoint, indexControlPoint.MappedPoint);
                        ((VertexShape)indexControlPoint.Annotation).RemovePoint(indexControlPoint.Index);
                        return true;
                    }
                    else if (SelectedAnnotation is Annotation annotation)
                    {
                        RemoveAnnotation(annotation);
                        return true;
                    }
                }
            }
            else if (string.Equals(keyName, "Escape", StringComparison.OrdinalIgnoreCase))
            {
                if (SelectedPoint != null
                    && SelectedAnnotation is VertexShape vertexShape
                    && Current == OperationType.PointAdd)
                {
                    CloseVertextShape(vertexShape);
                    return true;
                }
            }
            else if (string.Equals(keyName, "Z", StringComparison.OrdinalIgnoreCase))
            {
                if (modifiers == KeyModifiers.Ctrl)
                {
                    Undo();
                    return true;
                }
                else if (modifiers == (KeyModifiers.Ctrl | KeyModifiers.Shift))
                {
                    Redo();
                    return true;
                }
            }
            return false;
        }

        public void MoveToLast()
        {
            if (operations.Any())
            {
                lastLink = operations.Last;
            }
        }


        private void DrawAnnotationToolTip(SKCanvas canvas, PdfViewState state)
        {
            if (ToolTipAnnotation?.Page == null
                || Document == null
                || toolTipBound is not SKRect bound)
                return;
            var pageView = Document.GetPageView(ToolTipAnnotation.Page);
            if (pageView?.Bounds.IntersectsWith(state.NavigationArea) ?? false)
            {
                ToolTipRenderer?.Draw(canvas, state, bound);
            }
        }

        public LinkedList<EditOperation>.Enumerator GetEnumerator() => operations.GetEnumerator();

        IEnumerator<EditOperation> IEnumerable<EditOperation>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    }
}
