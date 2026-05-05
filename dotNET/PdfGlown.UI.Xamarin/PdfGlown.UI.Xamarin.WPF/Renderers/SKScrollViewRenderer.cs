using PdfGlown.UI;
using PdfGlown.UI.WPF;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using SkiaSharp.Views.WPF;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using Xamarin.Forms.Platform.WPF;

[assembly: ExportRenderer(typeof(SKScrollView), typeof(SKScrollViewRenderer))]
namespace PdfGlown.UI.WPF
{
    public class SKScrollViewRenderer : SKCanvasViewRenderer
    {
        private bool pressed;

        protected override void OnElementChanged(ElementChangedEventArgs<SKCanvasView> e)
        {
            base.OnElementChanged(e);
           
            if (e.NewElement is SKScrollView scrollView)
            {
                scrollView.CapturePointerFunc = CaptureMouse;
                scrollView.GetWindowScaleFunc = GetWindowScale;
                if (Control != null)
                {
                    Control.Focusable = Element.IsTabStop;
                    Control.Loaded += OnControlLoaded;
                }
            }
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);
            if (string.Equals(e.PropertyName, SKScrollView.CursorProperty.PropertyName, StringComparison.Ordinal))
            {
                UpdateCursor();
            }
        }

        private void UpdateCursor()
        {
            if (Control != null && Element is SKScrollView canvas)
            {
                switch (canvas.Cursor)
                {
                    case UI.CursorType.Arrow:
                        Control.Cursor = Cursors.Arrow;
                        break;
                    case UI.CursorType.SizeWestEast:
                        Control.Cursor = Cursors.SizeWE;
                        break;
                    case UI.CursorType.SizeNorthSouth:
                        Control.Cursor = Cursors.SizeNS;
                        break;
                    case UI.CursorType.BottomLeftCorner:
                        Control.Cursor = Cursors.SizeNESW;
                        break;
                    case UI.CursorType.BottomRightCorner:
                        Control.Cursor = Cursors.SizeNWSE;
                        break;
                    case UI.CursorType.Hand:
                        Control.Cursor = Cursors.Hand;
                        break;
                    case UI.CursorType.Wait:
                        Control.Cursor = Cursors.Wait;
                        break;
                    case UI.CursorType.SizeAll:
                        Control.Cursor = Cursors.ScrollAll;
                        break;
                    case UI.CursorType.Cross:
                        Control.Cursor = Cursors.Cross;
                        break;
                    case UI.CursorType.IBeam:
                        Control.Cursor = Cursors.IBeam;
                        break;
                }
            }
        }        

        private void OnControlLoaded(object sender, RoutedEventArgs e)
        {
            Control.Loaded -= OnControlLoaded;
            Control.MouseWheel += OnControlMouseWheel;
            Control.MouseEnter += OnControlMouseEnter;
            Control.MouseLeave += OnControlMouseLeave;
            Control.PreviewKeyDown += OnControlKeyDown;
            Control.PreviewMouseDown += OnControlMouseDown;
            Control.PreviewMouseUp += OnControlMouseUp;
            Control.PreviewMouseMove += OnControlMouseMove;
        }

        private void OnControlMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var view = sender as FrameworkElement;
            if (Element is SKScrollView scrollView)
            {
                var args = new TouchEventArgs(TouchAction.WheelChanged, GetMouseButton(e))
                {
                    WheelDelta = e.Delta,
                    KeyModifiers = SKCanvasHelper.GetModifiers(),
                    Location = e.MouseDevice.GetPosition(view).ToSKPoint()
                };
                scrollView.OnScrolled(args);
                e.Handled = args.Handled;
            }
        }

        private void RaiseTouch(object sender, MouseEventArgs e, TouchAction touchAction)
        {
            var view = sender as FrameworkElement;
            if (Element is SKScrollView scrollView)
            {
                var args = new TouchEventArgs(touchAction, GetMouseButton(e))
                {
                    KeyModifiers = SKCanvasHelper.GetModifiers(),
                    Location = e.MouseDevice.GetPosition(view).ToSKPoint()
                };
                scrollView.OnTouch(args);
                e.Handled = args.Handled;
            }
        }

        private void OnControlMouseLeave(object sender, MouseEventArgs e)
        {
            RaiseTouch(sender, e, TouchAction.Exited);
        }

        private void OnControlMouseEnter(object sender, MouseEventArgs e)
        {
            RaiseTouch(sender, e, TouchAction.Entered);
        }

        private void OnControlMouseDown(object sender, MouseButtonEventArgs e)
        {
            RaiseTouch(sender, e, TouchAction.Pressed);
        }        

        private void OnControlMouseMove(object sender, MouseEventArgs e)
        {
            RaiseTouch(sender, e, TouchAction.Moved);
        }

        private void OnControlMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (pressed)
            {
                pressed = false;
                Mouse.Capture(null);
                e.Handled = true;
            }
            RaiseTouch(sender, e, TouchAction.Released);

            Element.Focus();
            Control.Focus();
        }

        private MouseButton GetMouseButton(MouseButtonEventArgs e)
        {
            return e.ChangedButton switch
            {
                System.Windows.Input.MouseButton.Left => MouseButton.Left,
                System.Windows.Input.MouseButton.Right => MouseButton.Right,
                System.Windows.Input.MouseButton.Middle => MouseButton.Middle,
                _ => MouseButton.Unknown,
            };
        }

        private MouseButton GetMouseButton(MouseEventArgs e)
        {
            if (e is MouseButtonEventArgs button)
                return GetMouseButton(button);
            if (e.LeftButton == MouseButtonState.Pressed)
                return MouseButton.Left;
            if (e.RightButton == MouseButtonState.Pressed)
                return MouseButton.Right;
            if (e.MiddleButton == MouseButtonState.Pressed)
                return MouseButton.Middle;
            return MouseButton.Unknown;
        }
        
        private void OnControlKeyDown(object sender, KeyEventArgs e)
        {
            if (Element is SKScrollView scrollView)
            {
                e.Handled = scrollView.OnKeyDown(e.Key.ToString(), SKCanvasHelper.GetModifiers());
            }
        }

        private double GetWindowScale()
        {
            return SKCanvasHelper.GetWindowScale(Control);
        }

        public void CaptureMouse()
        {
            pressed = true;
            Mouse.Capture(Control);
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {                
                if (Control != null)
                {
                    Control.Loaded -= OnControlLoaded;
                    Control.MouseWheel -= OnControlMouseWheel;
                    Control.MouseEnter -= OnControlMouseEnter;
                    Control.MouseLeave -= OnControlMouseLeave;
                    Control.PreviewKeyDown -= OnControlKeyDown;
                    Control.PreviewMouseDown -= OnControlMouseDown;
                    Control.PreviewMouseUp -= OnControlMouseUp;
                    Control.PreviewMouseMove -= OnControlMouseMove;
                }
            }
            base.Dispose(disposing);
        }

        public SKPoint GetScaledCoord(double x, double y)
        {
            if (Element.IgnorePixelScaling)
            {
                return new SKPoint((float)x, (float)y);
            }
            else
            {
                return SKCanvasHelper.GetScaledCoord(Control, x, y);
            }
        }
    }
}
