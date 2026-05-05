using PdfGlown.Documents.Interaction;
using PdfGlown.Documents.Interaction.Annotations;
using SkiaSharp;
using System.Collections.Generic;

namespace PdfGlown.UI.ToolTip
{
    public class LinkToolTipRenderer : AnnotationToolTipRenderer
    {
        public LinkToolTipRenderer(Link link)
            : base(link)
        { }

        public Link Link
        {
            get => (Link)Annotation;
            set => Annotation = value;
        }

        public override SKRect Measure()
        {
            Clear();

            var target = Link.Target is ITextDisplayable texted ? texted.GetDisplayName() : string.Empty;
            if (string.IsNullOrEmpty(target))
                return SKRect.Empty;

            var text = MeasureLine(target, DefaultSKStyles.PaintToolTipText, DefaultSKStyles.FontToolTipText);
            ContentLines = new List<LineOfText> { text };

            var contentBound = text.Bound;
            contentBound.Offset(Indent, Indent);
            contentBound.Inflate(Indent, Indent);

            return ContentBound = contentBound;
        }
    }

}
