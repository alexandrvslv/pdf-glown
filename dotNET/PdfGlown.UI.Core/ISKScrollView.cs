namespace PdfGlown.UI
{
    public interface ISKScrollView
    {
        double VValue { get; set; }
        double HValue { get; set; }
        double VMaximum { get; set; }
        double HMaximum { get; set; }

        bool VBarVisible { get; }
        bool HBarVisible { get; }

        bool IsVScrollAnimation { get; }
        bool IsHScrollAnimation { get; }

        double Width { get; }
        double Height { get; }


        void InvalidatePaint();
        void CapturePointer(long pointerId);
    }
}