using System.ComponentModel;

namespace PdfGlown.UI
{
    public class ScrollEventArgs : HandledEventArgs
    {
        public ScrollEventArgs(int delta) : base(false)
        {
            WheelDelta = delta;
        }

        public int WheelDelta { get; }
    }
}
