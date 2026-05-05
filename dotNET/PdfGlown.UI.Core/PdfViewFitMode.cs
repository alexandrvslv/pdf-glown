using System.Runtime.Serialization;

namespace PdfGlown.UI
{
    public enum PdfViewFitMode
    {
        [EnumMember(Value = "Page Size")]
        PageSize,
        [EnumMember(Value = "Page Width")]
        PageWidth,
        [EnumMember(Value = "Page Height")]
        PageHeight,
        [EnumMember(Value = "Max Width")]
        MaxWidth,
        [EnumMember(Value = "Zoom")]
        Zoom
    }
}
