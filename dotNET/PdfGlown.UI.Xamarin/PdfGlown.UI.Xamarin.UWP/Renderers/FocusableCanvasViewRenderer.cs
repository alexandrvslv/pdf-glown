using PdfGlown.Viewer.UWP;
using SkiaSharp.Views.Forms;
using System;
using System.ComponentModel;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Xamarin.Forms;
using Xamarin.Forms.Platform.UWP;

[assembly: ExportRenderer(typeof(SKCanvasView), typeof(FocusableCanvasViewRenderer))]
namespace PdfGlown.Viewer.UWP
{
    public class FocusableCanvasViewRenderer : SKCanvasViewRenderer
    {
        private UserControl focusable;

        protected override void OnElementChanged(ElementChangedEventArgs<SKCanvasView> e)
        {
            base.OnElementChanged(e);

            if (Control != null)
            {
                if (focusable == null)
                {
                    Element.FocusChangeRequested += OnFocusRequest;
                    Element.Touch += OnTouch;
                    Children.Remove(Control);
                    focusable = new UserControl
                    {
                        Content = Control,
                        IsEnabled = Element.IsEnabled,
                        IsTabStop = Element.IsTabStop,
                        TabIndex = Element.TabIndex,
                        IsTapEnabled = false,
                        AllowFocusOnInteraction = true,
                        AllowFocusWhenDisabled = false,
                        UseSystemFocusVisuals = false,
                        TabFocusNavigation = KeyboardNavigationMode.Once
                    };
                    focusable.KeyDown += OnKeyDown;
                    focusable.KeyUp += OnKeyUp;
                    focusable.GotFocus += OnFocusableGetFocus;
                    focusable.LostFocus += OnFocusableLostFocus;
                    focusable.LosingFocus += OnFocusableLosingFocus;

                    Children.Add(focusable);
                }
            }
        }

        private void OnTouch(object sender, SKTouchEventArgs e)
        {
            if (e.ActionType == SKTouchAction.Pressed)
            {
                Element.Focus();
            }
        }

        private void OnFocusRequest(object sender, VisualElement.FocusRequestArgs e)
        {
            Debug.WriteLine($"Focus requesrt: {e.Focus}");
            if (e.Focus && focusable.FocusState != FocusState.Unfocused
                || !e.Focus && focusable.FocusState == FocusState.Unfocused)
            {
                e.Result = true;
            }
            else
            {
                e.Result = focusable.Focus(e.Focus ? FocusState.Programmatic : FocusState.Unfocused);
            }
        }

        private void OnFocusableLosingFocus(UIElement sender, LosingFocusEventArgs args)
        {
            if (args.OldFocusedElement == focusable && args.NewFocusedElement is ScrollViewer)
            {
                args.Cancel = true;
            }
        }

        private void OnFocusableLostFocus(object sender, RoutedEventArgs e)
        {
            ((IVisualElementController)Element).SetValueFromRenderer(VisualElement.IsFocusedPropertyKey, false);
        }

        private void OnFocusableGetFocus(object sender, RoutedEventArgs e)
        {
            ((IVisualElementController)Element).SetValueFromRenderer(VisualElement.IsFocusedPropertyKey, true);
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);
            if (e.PropertyName == VisualElement.IsEnabledProperty.PropertyName)
            {
                focusable.IsEnabled = Element.IsEnabled;
            }
        }

        protected virtual void OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
        }

        protected virtual void OnKeyUp(object sender, KeyRoutedEventArgs e)
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (Element != null)
                {
                    Element.FocusChangeRequested -= OnFocusRequest;
                    Element.Touch -= OnTouch;
                }

                if (focusable != null)
                {
                    focusable.KeyDown -= OnKeyDown;
                    focusable.KeyUp -= OnKeyUp;
                    focusable.GotFocus -= OnFocusableGetFocus;
                    focusable.LostFocus -= OnFocusableLostFocus;
                    focusable.LosingFocus -= OnFocusableLosingFocus;
                }
            }
            base.Dispose(disposing);
        }
    }
}


