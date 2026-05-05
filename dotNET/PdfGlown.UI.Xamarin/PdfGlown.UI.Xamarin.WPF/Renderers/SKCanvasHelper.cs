using SkiaSharp;
using System.Windows;
using System.Windows.Input;

namespace PdfGlown.UI.WPF
{
    public static class SKCanvasHelper
    {
        public static double GetWindowScale(FrameworkElement control)
        {
            var matrix = control != null ? PresentationSource.FromVisual(control)?.CompositionTarget?.TransformToDevice : null;
            return matrix?.M11 ?? 1D;
        }
        public static SKPoint GetScaledCoord(FrameworkElement control, double x, double y)
        {
            var factor = GetWindowScale(control);
            x = x * factor;
            y = y * factor;

            return new SKPoint((float)x, (float)y);
        }

        public static KeyModifiers GetModifiers()
        {
            var keyModifiers = KeyModifiers.None;
            if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt))
            {
                keyModifiers |= KeyModifiers.Alt;
            }
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                keyModifiers |= KeyModifiers.Ctrl;
            }
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                keyModifiers |= KeyModifiers.Shift;
            }

            return keyModifiers;
        }
    }
}
