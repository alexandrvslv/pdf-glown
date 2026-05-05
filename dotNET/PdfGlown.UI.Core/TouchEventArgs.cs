using SkiaSharp;
using System;
using System.ComponentModel;

namespace PdfGlown.UI
{
    public class TouchEventArgs : HandledEventArgs
    {
        public TouchEventArgs(TouchAction action, MouseButton button)
        {
            ActionType = action;
            MouseButton = button;
        }

        public TouchAction ActionType { get; }
        public MouseButton MouseButton { get; }
        public float WheelDelta { get; set; }
        public SKPoint Location { get; set; }
        public long PointerId { get; set; }
        public KeyModifiers KeyModifiers { get; set; }
    }
}
