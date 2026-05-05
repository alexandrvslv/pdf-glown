using System.Text;

namespace PdfGlown.Tokens
{
    internal static class Charset
    {
        static Charset()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }
        public static readonly Encoding ISO88591 = Encoding.GetEncoding("ISO-8859-1");
        public static readonly Encoding UTF16BE = Encoding.BigEndianUnicode;
        public static readonly Encoding UTF16LE = Encoding.Unicode;
        public static readonly Encoding ASCII = Encoding.ASCII;
        public static readonly Encoding UTF8 = Encoding.UTF8;

        public static Encoding GetEnconding(string name)
        {
            return Encoding.GetEncoding(name);
        }
    }
}

