using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Skia;
using Avalonia.Threading;
using PdfGlown.UI.Other;
using SkiaSharp;
using System;

namespace PdfGlown.UI.Aval;

public partial class SKScrollView : Control, ISKScrollView
{
    public static readonly DirectProperty<SKScrollView, double> VValueProperty = AvaloniaProperty.RegisterDirect<SKScrollView, double>(nameof(VValue),
            o => o.VValue,
            (o, v) => o.VValue = v);
    public static readonly DirectProperty<SKScrollView, double> HValueProperty = AvaloniaProperty.RegisterDirect<SKScrollView, double>(nameof(HValue),
            o => o.HValue,
            (o, v) => o.HValue = v);

    private const string ahScroll = "ScrollAnimation";
    private const string ahVScroll = "VScrollAnimation";
    private const string ahHScroll = "HScrollAnimation";

    protected ScrollLogic scroll;
    private SKScrollDrawOperation drawOp;
    private IPointer? pointer;

    public SKScrollView()
    {
        Envir.Init();
        ClipToBounds = true;
        Focusable = true;
        IsTabStop = true;
        scroll = new ScrollLogic(this);
        drawOp = new SKScrollDrawOperation(this);
        EffectiveViewportChanged += OnLayoutUpdated;
    }

    public double VMaximum
    {
        get => scroll.VMaximum;
        set => scroll.VMaximum = value;
    }

    public double HMaximum
    {
        get => scroll.HMaximum;
        set => scroll.HMaximum = value;
    }

    public double VValue
    {
        get => scroll.VValue;
        set
        {
            var old = scroll.VValue;
            scroll.VValue = value;
            RaiseProperty(VValueProperty, old, value);
        }
    }

    public double HValue
    {
        get => scroll.HValue;
        set
        {
            var old = scroll.HValue;
            scroll.HValue = value;
            RaiseProperty(HValueProperty, old, value);
        }
    }

    public bool VBarVisible => scroll.VBarVisible;

    public bool HBarVisible => scroll.HBarVisible;

    public TimerAnimation? ScrollAnimation { get; private set; }

    public TimerAnimation? VScrollAnimation { get; private set; }

    public TimerAnimation? HScrollAnimation { get; private set; }

    public bool IsVScrollAnimation => (VScrollAnimation ?? ScrollAnimation) != null;

    public bool IsHScrollAnimation => (HScrollAnimation ?? ScrollAnimation) != null;


    public event EventHandler<SKPaintSurfaceEventArgs>? PaintContent;

    public event EventHandler<SKPaintSurfaceEventArgs>? PaintOver;

    protected virtual void OnTouch(TouchEventArgs e)
    {
        scroll.OnTouch(e);
    }

    protected virtual void OnScrolled(TouchEventArgs e)
    {
        VAnimateScroll(e.WheelDelta, 16, 160);
        e.Handled = true;
    }

    protected internal virtual void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Canvas;

        //canvas.Scale(scroll.WindowScale, scroll.WindowScale);
        canvas.Clear(SKColors.DarkGray);

        OnPaintContent(e);
        scroll.PaintScrollBars(canvas);
        OnPaintOver(e);
    }

    protected virtual void OnPaintOver(SKPaintSurfaceEventArgs e)
    {
        PaintOver?.Invoke(this, e);
    }

    protected virtual void OnPaintContent(SKPaintSurfaceEventArgs e)
    {
        PaintContent?.Invoke(this, e);
    }

    public void HAnimateScroll(double delta, uint rate = DefaultSKStyles.MaxSize, uint legth = 400)
    {
        if (HScrollAnimation != null)
        {
            AbortAnimation(ahHScroll);
        }
        var newValue = scroll.NormalizeHValue(HValue + delta);
        HScrollAnimation = new TimerAnimation(v => HValue = v, HValue, newValue);
        HScrollAnimation.Commit(this, ahHScroll, rate, legth, finished: OnScrollComplete);
    }

    public void VAnimateScroll(double delta, uint rate = DefaultSKStyles.MaxSize, uint legth = 400)
    {
        if (VScrollAnimation != null)
        {
            AbortAnimation(ahVScroll);
        }
        var newValue = scroll.NormalizeVValue(VValue + delta);
        VScrollAnimation = new TimerAnimation(v => VValue = v, VValue, newValue);
        VScrollAnimation.Commit(this, ahVScroll, rate, legth, finished: OnScrollComplete);
    }

    protected void AnimateScroll(float top, double left)
    {
        if (VValue == top
            && HValue == left)
            return;
        if (ScrollAnimation != null)
        {
            AbortAnimation(ahScroll);
        }
        ScrollAnimation = new TimerAnimation();
        ScrollAnimation.Add(0, 1, new TimerAnimation(v => VValue = v, VValue, top));
        ScrollAnimation.Add(0, 1, new TimerAnimation(v => HValue = v, HValue, left));
        ScrollAnimation.Commit(this, ahScroll, 16, 270, finished: OnScrollComplete);
    }

    protected virtual void OnScrollComplete(double arg1, bool arg2)
    {
        if (HScrollAnimation != null)
        {
            HScrollAnimation = null;
            scroll.RaiseHScroll();
        }
        if (VScrollAnimation != null)
        {
            VScrollAnimation = null;
            scroll.RaiseVScroll();
        }
        if (ScrollAnimation != null)
        {
            ScrollAnimation = null;
            scroll.RaiseHScroll();
            scroll.RaiseVScroll();
        }
    }

    private void AbortAnimation(string ahScroll)
    {
        TimerAnimation.Abort(this, ahScroll);
    }

    protected virtual void OnLayoutUpdated(object? sender, EventArgs e)
    {
        var bounds = Bounds;
        scroll.OnSizeAllocated(bounds.Width, bounds.Height, GetWindowScale());
        drawOp.Bounds = new Rect(0, 0, scroll.Width, scroll.Height);
    }

    private static double GetWindowScale() => Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow?.DesktopScaling ?? 1D
            : 1D;

    public override void Render(DrawingContext context) => context.Custom(drawOp);

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        ProcessTouch(e, TouchAction.Entered);
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        ProcessTouch(e, TouchAction.Exited);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        ProcessTouch(e, TouchAction.Moved);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        ProcessTouch(e, TouchAction.Pressed);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        ProcessTouch(e, TouchAction.Released);
        base.OnPointerReleased(e);
    }

    private TouchEventArgs GenerateTouchArgs(PointerEventArgs e, TouchAction touchAction)
    {
        this.pointer = e.Pointer;

        var args = new TouchEventArgs(touchAction, GetMouseButton(e))
        {
            PointerId = e.Pointer.Id,
            Location = e.GetPosition(this).ToSKPoint(),
            KeyModifiers = (KeyModifiers)e.KeyModifiers,
        };
        return args;
    }

    private void ProcessTouch(PointerEventArgs e, TouchAction action)
    {
        var args = GenerateTouchArgs(e, action);
        OnTouch(args);
        e.Handled = args.Handled;
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        var args = GenerateTouchArgs(e, TouchAction.WheelChanged);
        args.WheelDelta = (float)e.Delta.Y * -100;
        OnScrolled(args);
        e.Handled = args.Handled;
    }

    private MouseButton GetMouseButton(PointerEventArgs e)
    {
        if (e is PointerReleasedEventArgs release)
            return (MouseButton)release.InitialPressMouseButton;
        return GetMouseButton(e.GetCurrentPoint(this));
    }

    private MouseButton GetMouseButton(PointerPoint pointer) => GetMouseButton(pointer.Properties);

    private MouseButton GetMouseButton(PointerPointProperties properties)
    {
        if (properties.IsLeftButtonPressed)
            return MouseButton.Left;
        else if (properties.IsRightButtonPressed)
            return MouseButton.Right;
        else if (properties.IsMiddleButtonPressed)
            return MouseButton.Middle;
        return MouseButton.Unknown;
    }

    public void InvalidatePaint()
    {
        if (Envir.IsMainContext)
            InvalidateVisual();
        else
            Dispatcher.UIThread.Post(InvalidateVisual);
    }

    public void CapturePointer(long pointerId)
    {
        pointer?.Capture(this);
    }

    protected void RaiseProperty<T>(DirectPropertyBase<T> property, T oldValue, T newValue)
    {
        if (Envir.IsMainContext)
            RaisePropertyChanged(property, oldValue, newValue);
        else
            Dispatcher.UIThread.Post(() => RaisePropertyChanged(property, oldValue, newValue));
    }
}
