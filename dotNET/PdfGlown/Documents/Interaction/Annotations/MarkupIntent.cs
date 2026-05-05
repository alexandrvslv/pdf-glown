using PdfGlown.Objects;
using PdfGlown.Util;

namespace PdfGlown.Documents.Interaction.Annotations
{
    public enum MarkupIntent
    {
        Text,

        FreeText,
        FreeTextCallout,
        FreeTextTypeWriter,

        Line,
        LineArrow,
        LineDimension,

        Polygon,
        PolygonCloud,
        PolygonDimension,

        PolyLine,
        PolyLineDimension
    }    
}
