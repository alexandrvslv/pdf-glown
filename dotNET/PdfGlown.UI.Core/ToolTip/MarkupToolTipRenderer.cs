using PdfGlown.Documents.Interaction.Annotations;
using SkiaSharp;

namespace PdfGlown.UI.ToolTip
{
    public class MarkupToolTipRenderer : AnnotationToolTipRenderer
    {
        public MarkupToolTipRenderer(Markup markup)
            : base(markup)
        {
        }

        public Markup Markup
        {
            get => (Markup)Annotation;
            set => Annotation = value;
        }

        public override SKRect Measure()
        {
            Clear();

            var athorText = Markup.Author ?? "User";
            var author = MeasureLine(athorText, DefaultSKStyles.PaintToolTipHeadText, DefaultSKStyles.FontToolTipHeadText);

            var contentBound = author.Bound;
            ContentLines = MeasureText(Markup.Contents ?? "<empty>", DefaultSKStyles.PaintToolTipText, DefaultSKStyles.FontToolTipText, ref contentBound);
            ContentLines.Insert(0, author);
            contentBound.Offset(Indent, Indent);
            contentBound.Inflate(Indent, Indent);

            return ContentBound = contentBound;
        }

    }

}
