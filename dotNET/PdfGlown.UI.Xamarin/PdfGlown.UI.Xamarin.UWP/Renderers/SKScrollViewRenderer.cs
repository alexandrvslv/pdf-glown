using PdfGlown.Viewer;
using PdfGlown.Viewer.UWP;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using System.ComponentModel;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Xamarin.Forms.Platform.UWP;

[assembly: ExportRenderer(typeof(SKScrollView), typeof(SKScrollViewRenderer))]
namespace PdfGlown.Viewer.UWP
{
    public class SKScrollViewRenderer : FocusableCanvasViewRenderer
    {
        private bool pressed;

        protected override void OnElementChanged(ElementChangedEventArgs<SKCanvasView> e)
        {
            base.OnElementChanged(e);
            if (Control != null)
            {
                Control.PointerPressed += OnControlPointerPressed;
                Control.PointerReleased += OnControlPointerReleased;
                Control.PointerMoved += OnControlMouseMove;
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
            if (Element is SKScrollView canvas)
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
                    case CursorType.IBeam:
                        Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.IBeam, 0);
                        break;
                }
            }
        }

        protected void OnControlPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var pointerPoint = e.GetCurrentPoint(Control);
            if (Element is SKScrollView canvas && canvas.ContainsCaptureBox(pointerPoint.Position.X, pointerPoint.Position.Y))
            {
                pressed = true;
                Control.CapturePointer(e.Pointer);
            }
            ((SKScrollView)Element).KeyModifiers = GetModifiers();
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
            if (Element is SKScrollView scrollView)
            {
                scrollView.KeyModifiers = GetModifiers();
            }
            if (pressed)
            {
                RaiseTouch(sender, e, SKTouchAction.Moved);
            }
        }

        private void RaiseTouch(object sender, PointerRoutedEventArgs e, SKTouchAction action)
        {
            var pointerPoint = e.GetCurrentPoint(Control);
            //var windowsPoint = view.PointToScreen(pointerPoint);
            var skPoint = Element.IgnorePixelScaling
                ? new SKPoint((float)pointerPoint.Position.X, (float)pointerPoint.Position.Y)
                : new SKPoint((float)(pointerPoint.Position.X * Control.Dpi), (float)(pointerPoint.Position.Y * Control.Dpi));
            var args = new SKTouchEventArgs(e.Pointer.PointerId, action, SKMouseButton.Left, SKTouchDeviceType.Mouse, skPoint, true);

            ((ISKCanvasViewController)Element).OnTouch(args);
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

        protected override void OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (Element is SKScrollView scrollView)
            {
                scrollView.KeyModifiers = GetModifiers();
                e.Handled = scrollView.OnKeyDown(e.Key.ToString(), scrollView.KeyModifiers);
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
