using PdfGlown.Viewer;
using PdfGlown.Viewer.UWP;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using System;
using System.ComponentModel;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Xamarin.Forms.Platform.UWP;

[assembly: ExportRenderer(typeof(SKGLScrollView), typeof(SKGLScrollViewRenderer))]
namespace PdfGlown.Viewer.UWP
{
    public class SKGLScrollViewRenderer : SKGLViewRenderer
    {
        private bool pressed;

        protected override void OnElementChanged(ElementChangedEventArgs<SKGLView> e)
        {
            base.OnElementChanged(e);
            if (Control != null)
            {
                Control.PointerPressed += OnControlPointerPressed;
                Control.PointerReleased += OnControlPointerReleased;
                Control.PointerMoved += OnControlMouseMove;
                Control.KeyDown += OnKeyDown;
                Control.KeyUp += OnKeyUp;
            }
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);
            if (e.PropertyName == SKScrollView.CursorProperty.PropertyName)
            {
                UpdateCursor();
            }
        }

        private void UpdateCursor()
        {
            if (Element is SKGLScrollView canvas)
            {
                switch (canvas.Cursor)
                {
                    case CursorType.Arrow:
                        Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);
                        break;
                    case CursorType.SizeWE:
                        Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.SizeWestEast, 0);
                        break;
                    case CursorType.SizeNS:
                        Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.SizeNorthSouth, 0);
                        break;
                    case CursorType.SizeNESW:
                        Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.SizeNortheastSouthwest, 0);
                        break;
                    case CursorType.SizeNWSE:
                        Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.SizeNorthwestSoutheast, 0);
                        break;
                    case CursorType.Hand:
                        Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Hand, 0);
                        break;
                    case CursorType.Wait:
                        Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Wait, 0);
                        break;
                    case CursorType.ScrollAll:
                        Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.SizeAll, 0);
                        break;
                    case CursorType.Cross:
                        Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Cross, 0);
                        break;
                }
            }
        }

        protected void OnControlPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var pointerPoint = e.GetCurrentPoint(Control);
            if (Element is SKGLScrollView canvas && canvas.ContainsCaptureBox(pointerPoint.Position.X, pointerPoint.Position.Y))
            {
                pressed = true;
                Control.CapturePointer(e.Pointer);
            }
            ((SKGLScrollView)Element).KeyModifiers = GetModifiers();
        }

        public static KeyModifiers GetModifiers()
        {
            var shiftState = CoreWindow.GetForCurrentThread().GetKeyState(VirtualKey.Shift);
            var ctrlState = CoreWindow.GetForCurrentThread().GetKeyState(VirtualKey.Control);
            var keyModifiers = KeyModifiers.None;
            if ((ctrlState & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down)
                keyModifiers |= KeyModifiers.Ctrl;
            if ((shiftState & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down)
                keyModifiers |= KeyModifiers.Shift;
            return keyModifiers;
        }

        protected void OnControlPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (pressed)
            {
                pressed = false;
                Control.ReleasePointerCaptures();
                e.Handled = true;
                //RaiseTouch(sender, e, SKTouchAction.Released);
            }
        }

        private void OnControlMouseMove(object sender, PointerRoutedEventArgs e)
        {
            if (pressed)
            {
                RaiseTouch(sender, e, SKTouchAction.Moved);
            }
        }

        private void RaiseTouch(object sender, PointerRoutedEventArgs e, SKTouchAction action)
        {
            var pointerPoint = e.GetCurrentPoint(Control);
            var scaleX = Control.CanvasSize.Width / Control.Width;
            var scaleY = Control.CanvasSize.Height / Control.Height;
            //var windowsPoint = view.PointToScreen(pointerPoint);
            var skPoint = true
                ? new SKPoint((float)pointerPoint.Position.X, (float)pointerPoint.Position.Y)
                : new SKPoint((float)(pointerPoint.Position.X * scaleX), (float)(pointerPoint.Position.Y * scaleY));
            var args = new SKTouchEventArgs(e.Pointer.PointerId, action, SKMouseButton.Left, SKTouchDeviceType.Mouse, skPoint, true);

            ((SKGLScrollView)Element).RaiseTouch(args);
        }

        private DependencyObject GetTopParent(UIElement control)
        {
            var parent = (DependencyObject)control;
            while (parent != null && VisualTreeHelper.GetParent(parent) != null)
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            return parent;
        }

        protected virtual void OnKeyUp(object sender, KeyRoutedEventArgs e)
        {
        }

        protected virtual void OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (Element is SKGLScrollView model)
            {
                e.Handled = model.OnKeyDown(e.Key.ToString(), GetModifiers());
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (Control != null)
                {
                    Control.PointerPressed -= OnControlPointerPressed;
                    Control.PointerReleased -= OnControlPointerReleased;
                    Control.PointerMoved -= OnControlMouseMove;
                }
            }
            base.Dispose(disposing);
        }
    }
}
