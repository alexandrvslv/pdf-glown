using PdfGlown.Documents;
using PdfGlown.Documents.Contents.Scanner;
using PdfGlown.Documents.Interaction.Actions;
using PdfGlown.Documents.Interaction.Annotations;
using PdfGlown.Documents.Interaction.Annotations.ControlPoints;
using PdfGlown.Documents.Interaction.Navigation;
using PdfGlown.Tools;
using PdfGlown.UI.Operations;
using PdfGlown.Util.Math;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace PdfGlown.UI
{
    public class PdfPageViewModel : IPdfPageViewModel, IDisposable
    {
        private SKPicture? picture;
        private SKMatrix matrix = SKMatrix.Identity;
        private PdfPage page;
        private SKImage? image;
        private float imageScale;
        private PageAnnotations? annotations;
        private SKRect? bounds;
        private SKSize size;

        public PdfPageViewModel(PdfDocumentViewModel document, PdfPage page)
        {
            this.Document = document;
            this.page = page;
        }

        public PdfDocumentViewModel Document { get; set; }

        IPdfDocumentViewModel IPdfPageViewModel.Document
        {
            get => Document;
            set => Document = (PdfDocumentViewModel)value;
        }

        public PdfPage Page
        {
            get => page;
            set => page = value;
        }

        public SKSize Size
        {
            get => size;
            set
            {
                if (size != value)
                {
                    size = value;
                    OnBoundsChanged();
                }
            }
        }

        public SKRect Bounds
        {
            get => bounds ??= CalculateBounds();
            set => bounds = value;
        }

        public int Index { get; internal set; }

        public int Order { get; internal set; }

        public float XOffset
        {
            get => matrix.TransX;
            set
            {
                if (XOffset != value)
                {
                    matrix.TransX = value;
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
                    OnBoundsChanged();
                }
            }
        }

        public SKMatrix Matrix
        {
            get => matrix;
            set
            {
                if (matrix != value)
                {
                    matrix = value;
                    OnBoundsChanged();
                }
            }
        }

        public event EventHandler? BoundsChanged;

        private SKRect CalculateBounds()
        {
            var matrix = Document.Matrix.PostConcat(Matrix);
            return SKRect.Create(matrix.TransX, matrix.TransY, Size.Width * matrix.ScaleX, Size.Height * matrix.ScaleY);
        }

        public Annotation GetAnnotation(string name) => Annotations[name];

        public PageAnnotations Annotations => annotations ??= Page.Annotations;

        IEnumerable<Annotation> IPdfPageViewModel.Annotations => Annotations;

        public bool Draw(SKCanvas canvas, PdfViewState state)
        {
            var pageRect = SKRect.Create(Size);
            state.PageView = this;
            canvas.Save();
            canvas.SetMatrix(state.PageViewMatrix);
            //state.Canvas.ClipRect(pageRect);
            canvas.DrawRect(pageRect, Document.PageBackgroundPaint);

            var picture = GetPicture(state.Viewer);
            if (picture != null)
            {
                canvas.DrawPicture(picture, Document.PageForegroundPaint);
                DrawTextSelection(canvas, state);
                if (state.Viewer.ShowMarkup && Annotations.Any())
                {
                    DrawAnnotations(canvas, state);
                }
                if (state.Viewer.ShowCharBound)
                {
                    DrawCharBounds(canvas, state);
                }
            }

            //var picture = GetImage(state.Viewer, state.Scale, state.Scale);
            //if (picture != null)
            //{
            //    canvas.DrawImage(picture, 0, 0, Document.PageForegroundPaint);
            //    DrawTextSelection(canvas, state);
            //    if (state.Viewer.ShowMarkup && Annotations.Any())
            //    {
            //        DrawAnnotations(canvas, state);
            //    }
            //    if (state.Viewer.ShowCharBound)
            //    {
            //        DrawCharBounds(canvas, state);
            //    }
            //}

            canvas.Restore();
            return picture != null;
        }

        private void DrawTextSelection(SKCanvas canvas, PdfViewState state)
        {
            var contextPageSelection = state.Viewer.TextSelection.GetPageSelection(page);
            if (contextPageSelection != null)
            {
                var path = contextPageSelection.GetPath();
                canvas.DrawPath(path, DefaultSKStyles.PaintTextSelectionFill);
            }
        }

        private void DrawCharBounds(SKCanvas canvas, PdfViewState state)
        {
            try
            {
                for (int b = 0; b < page.TextBlocks.Count; b++)
                {
                    var textBlock = page.TextBlocks[b];
                    foreach (var textString in textBlock.Strings)
                    {
                        foreach (var textChar in textString.Chars)
                        {
                            canvas.DrawPoints(SKPointMode.Polygon, textChar.Quad.GetClosedPoints(), DefaultSKStyles.PaintRed);
                        }
                        canvas.DrawPoints(SKPointMode.Polygon, textString.Quad.GetClosedPoints(), DefaultSKStyles.PaintBlue);
                    }
                    canvas.DrawRect(SKRect.Inflate(textBlock.Box, 0.5F, 0.51F), DefaultSKStyles.PaintGreen);
                }
            }
            catch { }
        }

        private void DrawAnnotations(SKCanvas canvas, PdfViewState state)
        {
            var self = page.RotateMatrix;
            canvas.Save();
#if NET9_0_OR_GREATER
            canvas.Concat(in self);
#else
            canvas.Concat(ref self);
#endif
            foreach (var annotation in Annotations)
            {
                if (annotation != null && annotation.Visible)
                {
                    DrawAnnotation(canvas, state, annotation);
                }
            }
            canvas.Restore();
        }

        private void DrawAnnotation(SKCanvas canvas, PdfViewState state, Annotation annotation)
        {
            try
            {
                if (annotation.IsNew)
                {
                    return;
                }
                var annotationBound = annotation.Draw(canvas);
                if (state.Operations.SelectedAnnotation is Annotation selectedAnnotation
                    && selectedAnnotation == annotation
                    && selectedAnnotation.Page == page)
                {
                    DrawSelectedAnnotation(canvas, state, annotation, annotationBound);
                }
            }
            catch { }
        }

        private void DrawSelectedAnnotation(SKCanvas canvas, PdfViewState state, Annotation annotation, SKRect bound)
        {
            var selectedPoint = state.Operations.SelectedPoint;
            if (state.Operations.Current == OperationType.None
            || annotation != selectedPoint?.Annotation)
            {
                bound.Inflate(1, 1);
                canvas.DrawRect(bound, DefaultSKStyles.PaintBorderSelection);
            }
            if (!state.Viewer.IsReadOnly)
            {
                foreach (var controlPoint in annotation.GetControlPoints())
                {
                    var pBound = controlPoint.GetBounds(state.PageViewMatrix);
                    canvas.DrawOval(pBound, DefaultSKStyles.PaintPointFill);
                    canvas.DrawOval(pBound, controlPoint == selectedPoint
                        ? DefaultSKStyles.PaintBorderSelection
                        : DefaultSKStyles.PaintBorderDefault);
                }
            }
        }

        public SKPicture? GetPicture(IPdfView canvasView)
        {
            if (picture == null && Document.LockObject.IsSet)
            {
                Document.LockObject.Reset();
                var task = new Task(state => Render((IPdfView)state!), canvasView);
                task.Start();
            }
            return picture;
        }

        public SKImage? GetImage(IPdfView canvasView, float scaleX, float scaleY)
        {
            var picture = GetPicture(canvasView);
            if (picture == null)
            {
                return null;
            }
            if (scaleX != imageScale || image == null)
            {
                imageScale = scaleX;
                image?.Dispose();
                var imageSize = new SKSizeI((int)(Size.Width * scaleX), (int)(Size.Height * scaleY));
                var matrix = SKMatrix.Identity;
                if (imageScale < 1F)
                {
                    //matrix = SKMatrix.CreateScale(scaleX, scaleY);
                }
                image = SKImage.FromPicture(picture, imageSize);//, Matrix, 
            }
            return image;
        }

        private void Render(IPdfView canvasView)
        {
            try
            {
                var rect = SKRect.Create(Size);
                using var recorder = new SKPictureRecorder();
#if NET9_0_OR_GREATER
                using var canvas = recorder.BeginRecording(rect, false);
#else
                using var canvas = recorder.BeginRecording(rect);
#endif
                try
                {
                    Page.Render(canvas, rect);
                }
                catch (Exception ex)
                {
                    DrawErrorText(canvas, ex);
                }
                picture = recorder.EndRecording();
                //text
                var positionComparator = new TextBlockPositionComparer<ITextBlock>();
                Page.TextBlocks.Sort(positionComparator);

                canvasView.InvalidatePaint();
            }
            finally
            {
                Document.LockObject.Set();
            }
        }

        private static void DrawErrorText(SKCanvas canvas, Exception ex)
        {
            using var paint = new SKPaint { Color = SKColors.DarkRed };
            using var font = new SKFont();
            canvas.Save();
            if (canvas.TotalMatrix.ScaleY < 0)
            {
                var matrix = SKMatrix.CreateScale(1, -1);
#if NET9_0_OR_GREATER
                canvas.Concat(in matrix);
#else
                canvas.Concat(ref matrix);
#endif
            }
            canvas.DrawText(ex.Message, 0, 0, font, paint);
            canvas.Restore();
        }

        public IEnumerable<ITextString> GetStrings() => Page.TextBlocks.SelectMany(x => x.Strings);

        public SKMatrix GetViewMatrix(PdfViewState state)
        {
            return state.ViewMatrix.PreConcat(Document.Matrix.PreConcat(Matrix));
        }

        public void Dispose()
        {
            image?.Dispose();
            picture?.Dispose();
        }

        public void Touch(PdfViewState state)
        {
            state.PageView = this;
            //SKMatrix.PreConcat(ref state.PageMatrix, state.PageView.InitialMatrix);

            switch (state.Operations.Current)
            {
                case OperationType.PointMove:
                    OnTouchPointMove(state);
                    return;
                case OperationType.PointAdd:
                    OnTouchPointAdd(state);
                    return;
                case OperationType.AnnotationSize:
                    OnTouchSized(state);
                    return;
                case OperationType.AnnotationDrag:
                    OnTouchDragged(state);
                    return;
                default:
                    break;
            }

            if (OnTouchAnnotations(state))
            {
                return;
            }
            if (OnTouchText(state))
            {
                return;
            }
            if (state.TouchAction == TouchAction.Released)
            {
                state.Operations.SelectedAnnotation = null;
            }
            else if (state.TouchAction == TouchAction.Pressed)
            {
                state.Viewer.TextSelection.Clear();
            }
            state.Viewer.Cursor = CursorType.Arrow;
            state.Operations.ToolTipAnnotation = null;
            state.Operations.HoverPoint = null;
        }

        private void OnTouchPointMove(PdfViewState state)
        {
            if (state.TouchAction == TouchAction.Moved)
            {
                if (state.TouchButton == MouseButton.Left
                    && state.Operations.SelectedPoint is ControlPoint point)
                {
                    point.MappedPoint = state.PagePointerLocation;
                }
            }
            else if (state.TouchAction == TouchAction.Released)
            {
                //CurrentPoint.Point = state.PagePointerLocation;

                state.Operations.Current = OperationType.None;
            }

            state.Viewer.InvalidatePaint();
        }

        private void OnTouchPointAdd(PdfViewState state)
        {
            var selectedAnnotation = state.Operations.SelectedAnnotation;
            if (state.TouchAction == TouchAction.Moved
                && state.Operations.SelectedPoint is ControlPoint cPoint)
            {
                cPoint.MappedPoint = state.PagePointerLocation;
            }
            else if (state.TouchAction == TouchAction.Released
                && selectedAnnotation is VertexShape vertexShape)
            {
                var rect = new SKRect(vertexShape.FirstPoint.X - 5, vertexShape.FirstPoint.Y - 5, vertexShape.FirstPoint.X + 5, vertexShape.FirstPoint.Y + 5);
                if (vertexShape.Points.Length > 2 && rect.Contains(vertexShape.LastPoint))
                {
                    //EndOperation(operationLink.Value);
                    //BeginOperation(annotation, OperationType.PointRemove, CurrentPoint);
                    //annotation.RemovePoint(annotation.Points.Length - 1);
                    state.Operations.CloseVertextShape(vertexShape);
                }
                else
                {
                    state.Operations.EndOperation();
                    var point = state.Operations.SelectedPoint = vertexShape.AddPoint(page.InvertRotateMatrix.MapPoint(state.PagePointerLocation));
                    state.Operations.BeginOperation(vertexShape, OperationType.PointAdd, point);
                    return;
                }

                state.Operations.Current = OperationType.None;
            }

            state.Viewer.InvalidatePaint();
        }

        private void OnTouchSized(PdfViewState state)
        {
            if (state.PressedLocation == null)
                return;
            if (state.TouchAction == TouchAction.Moved
                && state.Operations.SelectedAnnotation is Annotation annotation)
            {
                var bound = annotation.GetViewBounds(state.PageViewMatrix);
                bound.Size += new SKSize(state.PointerLocation - state.PressedLocation.Value);

                annotation.SetBounds(state.InvertPageViewMatrix.MapRect(bound));

                state.PressedLocation = state.PointerLocation;
            }
            else if (state.TouchAction == TouchAction.Released)
            {
                state.Operations.Current = OperationType.None;
            }
            state.Viewer.InvalidatePaint();
        }

        private void OnTouchDragged(PdfViewState state)
        {
            if (state.Operations.SelectedAnnotation is not Annotation annotation)
                return;
            if (state.TouchAction == TouchAction.Moved)
            {
                if (page != annotation.Page)
                {
                    if (!annotation.IsNew)
                    {
                        state.Operations.EndOperation();
                        state.Operations.BeginOperation(annotation, OperationType.AnnotationRePage, nameof(Annotation.Page), annotation.Page.Index, Index);
                    }
                    annotation.Page = page;
                    if (!annotation.IsNew)
                    {
                        state.Operations.BeginOperation(annotation, OperationType.AnnotationDrag, nameof(Annotation.Box));
                        state.PressedLocation = null;
                    }
                }
                var bound = annotation.GetViewBounds(state.PageViewMatrix);
                if (bound.Width == 0)
                    bound.Right = bound.Left + 1;
                if (bound.Height == 0)
                    bound.Bottom = bound.Top + 1;
                if (state.PressedLocation == null || state.TouchButton == MouseButton.Unknown)
                {
                    bound.Location = state.PointerLocation;
                }
                else
                {
                    bound.Location += state.PointerLocation - state.PressedLocation.Value;
                    state.PressedLocation = state.PointerLocation;
                }
                annotation.SetBounds(state.InvertPageViewMatrix.MapRect(bound));

                state.Viewer.InvalidatePaint();
            }
            else if (state.TouchAction == TouchAction.Pressed)
            {
                state.PressedLocation = state.PointerLocation;
                if (annotation.IsNew)
                {
                    state.Operations.AddAnnotation(annotation);
                    if (annotation is StickyNote sticky)
                    {
                        state.Operations.Current = OperationType.None;
                    }
                    else if (annotation is Line line)
                    {
                        line.StartPoint = state.InvertPageMatrix.MapPoint(state.PointerLocation);
                        state.Operations.SelectedPoint = line.GetControlPoints().OfType<LineEndControlPoint>().FirstOrDefault();
                        state.Operations.Current = OperationType.PointMove;
                    }
                    else if (annotation is VertexShape vertexShape)
                    {
                        vertexShape.FirstPoint = state.InvertPageMatrix.MapPoint(state.PointerLocation);
                        state.Operations.SelectedPoint = vertexShape.FirstControlPoint;
                        state.Operations.Current = OperationType.PointAdd;
                    }
                    else if (annotation is Shape shape)
                    {
                        state.Operations.Current = OperationType.AnnotationSize;
                    }
                    else if (annotation is FreeText freeText)
                    {
                        if (freeText.Line == null)
                        {
                            state.Operations.Current = OperationType.AnnotationSize;
                        }
                        else
                        {
                            freeText.Line.Start = state.InvertPageMatrix.MapPoint(state.PointerLocation);
                            state.Operations.SelectedPoint = annotation.GetControlPoints().OfType<TextMidControlPoint>().FirstOrDefault();
                            state.Operations.Current = OperationType.PointMove;
                        }
                    }
                    else
                    {
                        var controlPoint = annotation.GetControlPoints().OfType<BottomRightControlPoint>().FirstOrDefault();
                        if (controlPoint != null)
                        {
                            state.Operations.SelectedPoint = controlPoint;
                            state.Operations.Current = OperationType.PointMove;
                        }
                        else
                        {
                            state.Operations.Current = OperationType.AnnotationSize;
                        }
                    }
                }
            }
            else if (state.TouchAction == TouchAction.Released)
            {
                state.PressedLocation = null;
                state.Operations.Current = OperationType.None;
            }
        }

        private bool OnTouchAnnotations(PdfViewState state)
        {
            var selectedAnnotation = state.Operations.SelectedAnnotation;
            if (selectedAnnotation != null && selectedAnnotation.Page == page)
            {
                var bounds = selectedAnnotation.GetViewBounds(state.PageViewMatrix);
                bounds.Inflate(2, 2);
                if (bounds.Contains(state.PointerLocation))
                {
                    OnTouchAnnotation(state, selectedAnnotation, bounds);
                    return true;
                }
            }
            Annotation? tached = null;
            SKRect tachedBound = default;
            foreach (var annotation in Annotations)
            {
                if (annotation == null || !annotation.Visible)
                    continue;
                var bounds = annotation.GetViewBounds(state.PageViewMatrix);
                bounds.Inflate(2, 2);
                if (bounds.Contains(state.PointerLocation))
                {
                    if (tached != null
                    && bounds.Contains(tachedBound))
                    {
                        continue;
                    }
                    tached = annotation;
                    tachedBound = bounds;
                }
            }

            if (tached != null)
            {
                OnTouchAnnotation(state, tached, tachedBound);
                return true;
            }
            return false;
        }

        private void OnTouchAnnotation(PdfViewState state, Annotation annotation, SKRect bounds)
        {
            var selectedAnnotation = state.Operations.SelectedAnnotation;
            if (state.TouchAction == TouchAction.Moved)
            {
                state.Operations.ToolTipAnnotation = annotation;
                if (state.TouchButton == MouseButton.Left)
                {
                    if (!state.Viewer.IsReadOnly
                        && selectedAnnotation != null
                        && state.PressedLocation != null
                        && state.Viewer.Cursor == CursorType.Hand
                        && !(annotation is Widget))
                    {
                        if (state.Operations.Current == OperationType.None)
                        {
                            var dif = SKPoint.Distance(state.PointerLocation, state.PressedLocation.Value);
                            if (Math.Abs(dif) > 5)
                            {
                                state.Operations.Current = OperationType.AnnotationDrag;
                            }
                        }
                    }
                }
                else if (state.TouchButton == MouseButton.Unknown)
                {
                    if (annotation == selectedAnnotation)
                    {
                        foreach (var controlPoint in annotation.GetControlPoints())
                        {
                            if (controlPoint.GetViewBounds(state.PageViewMatrix).Contains(state.PointerLocation))
                            {
                                state.Operations.HoverPoint = controlPoint;
                                return;
                            }
                        }
                    }
                    state.Operations.HoverPoint = null;
                    if (!state.Viewer.IsReadOnly)
                    {
                        var rect = new SKRect(
                            bounds.Right - 10,
                            bounds.Bottom - 10,
                            bounds.Right,
                            bounds.Bottom);
                        if (rect.Contains(state.PointerLocation)
                           && annotation is Markup markup
                           && markup.AllowSize)
                        {
                            state.Viewer.Cursor = CursorType.BottomRightCorner;
                        }
                        else
                        {
                            state.Viewer.Cursor = CursorType.Hand;
                        }
                    }
                }
            }
            else if (state.TouchAction == TouchAction.Pressed)
            {
                if (state.TouchButton == MouseButton.Left)
                {
                    state.PressedLocation = state.PointerLocation;
                    state.Operations.SelectedAnnotation = annotation;
                    if (annotation == selectedAnnotation && state.Operations.HoverPoint != null && !state.Viewer.IsReadOnly)
                    {
                        state.Operations.SelectedPoint = state.Operations.HoverPoint;
                        state.Operations.Current = OperationType.PointMove;
                    }
                    else if (state.Viewer.Cursor == CursorType.BottomRightCorner && !state.Viewer.IsReadOnly)
                    {
                        state.Operations.Current = OperationType.AnnotationSize;
                    }
                }
            }
            else if (state.TouchAction == TouchAction.Released)
            {
                if (state.TouchButton == MouseButton.Left)
                {
                    state.PressedLocation = null;
                    if (annotation is Link link)
                    {
                        switch (link.Target)
                        {
                            case GoToLocal goToLocal:
                                if (goToLocal.Destination is LocalDestination gotoLocal
                                    && gotoLocal.Page is PdfPage gotoLocalPage)
                                {
                                    state.Viewer.ScrollTo(gotoLocalPage);
                                }
                                break;
                            case GoToURI goToUri:
                                var uri = goToUri.URI;
                                Process.Start(new ProcessStartInfo(uri.ToString()) { UseShellExecute = true });
                                break;

                            case LocalDestination localDestination:
                                if (localDestination.Page is PdfPage goToPage)
                                    state.Viewer.ScrollTo(goToPage);
                                break;
                        }

                    }
                }
            }
        }

        private bool OnTouchText(PdfViewState state)
        {
            var textSelection = state.Viewer.TextSelection;
            var raiseChanged = false;

            foreach (var textBlock in page.TextBlocks)
            {
                if (textBlock.Box.IsEmpty
                        || !textBlock.Box.Contains(state.PagePointerLocation))
                    continue;
                foreach (var textString in textBlock.Strings)
                {
                    if (textString.Quad.IsEmpty
                        || !textString.Quad.Contains(state.PagePointerLocation))
                        continue;
                    foreach (var textChar in textString.Chars)
                    {
                        if (textChar.Quad.Contains(state.PagePointerLocation))
                        {
                            state.Viewer.Cursor = CursorType.IBeam;
                            if (state.TouchAction == TouchAction.Pressed
                                && state.TouchButton == MouseButton.Left)
                            {
                                raiseChanged = true;
                                textSelection.SetStartChar(page, textBlock, textString, textChar);
                                textSelection.SetHoverChar(page, textBlock, textString, textChar);
                            }
                            // skip selection calculation
                            else if (!textSelection.SetHoverChar(page, textBlock, textString, textChar))
                                return true;

                            break;
                        }
                    }
                }
            }
            if ((raiseChanged || state.TouchAction == TouchAction.Moved)
                && state.TouchButton == MouseButton.Left
                && textSelection.StartPage != null
                && textSelection.StartString != null
                && page.TextBlocks.Count > 0)
            {
                raiseChanged = true;
                var pageSelection = textSelection.GetOrCreatePageSelection(page);

                var startChar = page == textSelection.StartPage
                    ? textSelection.StartChar
                    : page.Index > ((PdfPage)textSelection.StartPage).Index
                        ? page.TextBlocks.First(x => x.Strings.Count > 0).Strings.First(x => x.Chars.Count > 0).Chars.First()
                        : page.TextBlocks.Last().Strings.Last().Chars.Last();
                var firstMiddle = startChar.Quad.Middle ?? new SKPoint(startChar.Quad.MinX, startChar.Quad.MinY);
                var selectionRect = new SKRect(firstMiddle.X, firstMiddle.Y, state.PagePointerLocation.X, state.PagePointerLocation.Y)
                    .Standardized;
                var selectionLine = firstMiddle.Y > state.PagePointerLocation.Y
                    ? new SKLine(state.PagePointerLocation.X, state.PagePointerLocation.Y, firstMiddle.X, firstMiddle.Y)
                    : new SKLine(firstMiddle.X, firstMiddle.Y, state.PagePointerLocation.X, state.PagePointerLocation.Y);
                pageSelection.Clear();

                for (int b = 0; b < page.TextBlocks.Count; b++)
                {
                    var textBlock = page.TextBlocks[b];
                    if (textBlock.Box.IsEmpty
                           || !(textBlock.Box.IntersectsWith(selectionRect)
                            || textBlock.Box.Contains(selectionRect)
                            || selectionRect.Contains(textBlock.Box)))
                        continue;

                    for (int i = 0; i < textBlock.Strings.Count; i++)
                    {
                        var textString = textBlock.Strings[i];
                        var bounds = textString.Quad.GetBounds();
                        if (bounds.Bottom < selectionRect.Top
                            || bounds.Top > selectionRect.Bottom)
                            continue;
                        var isFirstLine = bounds.Top <= selectionRect.Top
                                        && bounds.Bottom >= selectionRect.Top;
                        var isLastLine = bounds.Bottom >= selectionRect.Bottom
                                        && bounds.Top <= selectionRect.Bottom;
                        if (isFirstLine || isLastLine)
                        {
                            var intersect = textString.Quad.ContainsOrIntersects(selectionLine);
                            var startIntersect = -1;
                            var finishIntersect = textString.Chars.Count;
                            if (intersect)
                            {
                                for (int j = 0; j < textString.Chars.Count; j++)
                                {
                                    var textChar = textString.Chars[j];
                                    if (isFirstLine && isLastLine && textChar.Quad.ContainsOrIntersects(selectionLine)
                                        || isFirstLine && textChar.Quad.Contains(selectionLine.A)
                                        || isLastLine && textChar.Quad.Contains(selectionLine.B))
                                    {
                                        if (startIntersect == -1)
                                        {
                                            startIntersect = j;
                                        }
                                    }
                                    else if (startIntersect >= 0)
                                    {
                                        finishIntersect = j;
                                        break;
                                    }
                                }
                            }

                            if (isFirstLine && isLastLine)
                            {
                                if (intersect)
                                {
                                    pageSelection.AddRange(textString.Chars
                                        .Skip(startIntersect)
                                        .Take(finishIntersect - startIntersect));
                                }
                            }
                            else if (isFirstLine && !isLastLine)
                            {
                                if (intersect)
                                    pageSelection.AddRange(textString.Chars.Skip(startIntersect));
                                else if (bounds.Left >= selectionLine.A.X)
                                    pageSelection.AddRange(textString.Chars);

                            }
                            else
                            {
                                if (intersect)
                                    pageSelection.AddRange(textString.Chars.Take(finishIntersect));
                                else if (bounds.Right <= selectionLine.B.X)
                                    pageSelection.AddRange(textString.Chars);
                            }
                        }
                        else
                        {
                            pageSelection.AddRange(textString.Chars);
                        }
                    }
                }
            }
            if (state.TouchAction == TouchAction.Released)
            {
                textSelection.ClearStartChar();
            }
            if (raiseChanged)
                textSelection.OnChanged();
            return raiseChanged;
        }

        public PdfPage? GetPage(PdfViewState state) => page;

        private void OnBoundsChanged()
        {
            bounds = null;
            BoundsChanged?.Invoke(this, EventArgs.Empty);
        }


    }

}

