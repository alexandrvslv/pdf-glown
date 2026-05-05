using System;

namespace PdfGlown.UI
{
    public delegate void PdfPageEventHandler(PdfPageEventArgs e);

    public class PdfPageEventArgs : EventArgs
    {
        public PdfPageEventArgs(IPdfPageViewModel? page)
        {
            Page = page;
        }

        public IPdfPageViewModel? Page { get; internal set; }
    }
}
