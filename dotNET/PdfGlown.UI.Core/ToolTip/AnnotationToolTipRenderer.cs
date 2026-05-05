using PdfGlown.Documents.Interaction.Annotations;
using SkiaSharp;

namespace PdfGlown.UI.ToolTip
{
    public abstract class AnnotationToolTipRenderer : ToolTipRenderer
    {
        protected AnnotationToolTipRenderer(Annotation annotation)
        {
            Annotation = annotation;
        }

        public Annotation Annotation { get; set; }

        public override SKRect GetWindowBound(PdfViewState state)
        {
            SKRect windowArea = state.WindowArea;
            SKRect annotation = state.InvertWindowMatrix.MapRect(Annotation.GetViewBounds(state.PageViewMatrix));
            SKRect toolTip = Measure();
            var left = annotation.MidX - toolTip.Width / 2;
            var right = left + toolTip.Width;
            return SKRect.Create(
                   left < windowArea.Left ? windowArea.Left : right > windowArea.Right ? left - (right - windowArea.Right) : left,
                   (annotation.Bottom + toolTip.Bottom + 10) > windowArea.Bottom ? (annotation.Top - toolTip.Height) : annotation.Bottom + 10,
                    toolTip.Width, toolTip.Height);
        }        
    }

}
