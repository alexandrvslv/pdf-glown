using PdfGlown.Documents;
using PdfGlown.Documents.Interaction.Annotations;
using PdfGlown.Documents.Interaction.Annotations.ControlPoints;
using PdfGlown.Objects;
using PdfGlown.Util.Invokers;
using SkiaSharp;

namespace PdfGlown.UI.Operations
{
    public class AnnotationOperation : EditOperation
    {
        private Annotation annotation;

        public AnnotationOperation(Annotation annotation, PdfDocumentViewModel document, EditorOperations operations) 
            : base(document, operations)
        {
            this.annotation = annotation;
        }

        public Annotation Annotation
        {
            get => annotation;
            set
            {
                annotation = value;
                if (annotation.Page != null)
                {
                    PageIndex = annotation.Page.Index;
                }
            }
        }

        public override EditOperation Clone(PdfDocumentViewModel document)
        {
            var cloned = (AnnotationOperation)base.Clone(document);

            cloned.Annotation = document.FindAnnotation(Annotation.Name)
                ?? (Annotation)Annotation.RefOrSelf.Clone(document.Document).Resolve(PdfName.Annot);

            if (cloned.Property is ControlPoint controlPoint)
                cloned.Property = controlPoint.Clone(cloned.Annotation);

            return cloned;
        }

        public override object? EndOperation()
        {
            var result = (object?)null;
            switch (Type)
            {
                case OperationType.AnnotationDrag:
                case OperationType.AnnotationSize:
                    result =
                        NewValue = Annotation.GetViewBounds();
                    break;
                case OperationType.AnnotationAdd:
                    result =
                        Document.AddAnnotation(Annotation);
                    break;
                case OperationType.AnnotationRemove:
                    result =
                        Document.RemoveAnnotation(Annotation);
                    break;
            }
            if (Property is ControlPoint controlPoint)
            {
                result =
                    NewValue = controlPoint.MappedPoint;
            }
            Operations.OnEndOperation(this, result);
            return result;
        }

        public override void Redo()
        {
            if (Annotation.Page?.Annotations.Contains(Annotation) ?? false)
            {
                Operations.SelectedAnnotation = Annotation;
            }
            switch (Type)
            {
                case OperationType.AnnotationAdd:
                    if ((Annotation.Page ?? Document.GetPageView(PageIndex)?.Page) is PdfPage page)
                        Document.AddAnnotation(page, Annotation);
                    break;
                case OperationType.AnnotationRemove:
                    Document.RemoveAnnotation(Annotation);
                    break;
                case OperationType.AnnotationDrag:
                    if (NewValue is SKRect drect)
                        Annotation.SetBounds(drect);
                    break;
                case OperationType.AnnotationSize:
                    if (NewValue is SKRect srect)
                        Annotation.SetBounds(srect);
                    break;
                case OperationType.AnnotationRePage:
                    if (NewValue is int rIndex)
                        Annotation.Page = Document.GetPageView(rIndex)?.Page;
                    break;
                case OperationType.AnnotationProperty:
                    if (Property is IInvoker invoker)
                        invoker.SetValue(Annotation, NewValue);
                    break;
                case OperationType.PointMove:
                    if (Property is ControlPoint mCPoint
                        && NewValue is SKPoint mPoint)
                    {
                        mCPoint.MappedPoint = mPoint;
                    }
                    break;
                case OperationType.PointAdd:
                    if (Property is IndexControlPoint aCPoint
                        && Annotation is VertexShape aVertexShape
                        && NewValue is SKPoint aPoint)
                    {
                        aVertexShape.InsertPoint(aCPoint.Index, aPoint);
                    }
                    break;
                case OperationType.PointRemove:
                    if (Property is IndexControlPoint rCPoint
                        && Annotation is VertexShape rVertexShape)
                    {
                        rVertexShape.RemovePoint(rCPoint.Index);
                    }
                    break;
            }
            if (Annotation.Page?.Annotations.Contains(Annotation) ?? false)
            {
                Operations.SelectedAnnotation = Annotation;
            }
        }

        public override void Undo()
        {
            if (Annotation.Page?.Annotations.Contains(Annotation) ?? false)
            {
                Operations.SelectedAnnotation = Annotation;
            }
            switch (Type)
            {
                case OperationType.AnnotationAdd:
                    Document.RemoveAnnotation(Annotation);
                    break;
                case OperationType.AnnotationRemove:
                    if ((Annotation.Page ?? Document.GetPageView(PageIndex)?.Page) is PdfPage page)
                        Document.AddAnnotation(page, Annotation);
                    break;
                case OperationType.AnnotationDrag:
                    if (OldValue is SKRect drect)
                        Annotation.SetBounds(drect);
                    break;
                case OperationType.AnnotationSize:
                    if (OldValue is SKRect srect)
                        Annotation.SetBounds(srect);
                    break;
                case OperationType.AnnotationRePage:
                    if (OldValue is int rIndex)
                        Annotation.Page = Document.Pages[rIndex];
                    break;
                case OperationType.AnnotationProperty:
                    if (Property is IInvoker invoker)
                        invoker.SetValue(Annotation, OldValue);
                    break;
                case OperationType.PointMove:
                    if (Property is ControlPoint mCPoint
                        && OldValue is SKPoint mPoint)
                    {
                        mCPoint.MappedPoint = mPoint;
                    }
                    break;
                case OperationType.PointAdd:
                    if (Property is IndexControlPoint aCPoint
                        && Annotation is VertexShape aVertexShape)
                    {
                        aVertexShape.RemovePoint(aCPoint.Index);
                    }
                    break;
                case OperationType.PointRemove:
                    if (Property is IndexControlPoint rCPoint
                        && Annotation is VertexShape rVertexShape
                        && NewValue is SKPoint rPoint)
                    {
                        rVertexShape.InsertPoint(rCPoint.Index, rPoint);
                    }
                    break;
            }

            if (Annotation.Page?.Annotations.Contains(Annotation) ?? false)
            {
                Operations.SelectedAnnotation = Annotation;
            }
        }
    }
}
