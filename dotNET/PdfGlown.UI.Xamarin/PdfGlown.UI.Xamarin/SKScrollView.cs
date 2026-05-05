using SkiaSharp.Views.Forms;
using System;
using System.Threading;
using Xamarin.Forms;

namespace PdfGlown.UI
{
    public class SKScrollView : SKCanvasView, ISKScrollView
    {
        public static readonly BindableProperty CursorProperty = BindableProperty.Create(nameof(Cursor), typeof(CursorType), typeof(SKScrollView), CursorType.Arrow,
            propertyChanged: (bindable, oldValue, newValue) => ((SKScrollView)bindable).OnCursorChanged((CursorType)oldValue, (CursorType)newValue));

        private const string ahScroll = "ScrollAnimation";
        private const string ahVScroll = "VScrollAnimation";
        private const string ahHScroll = "HScrollAnimation";

        protected ScrollLogic scroll;
        public Action CapturePointerFunc;
        public Func<double> GetWindowScaleFunc;

        public SKScrollView()
        {
            Envir.Init();
            IsTabStop = true;
            IgnorePixelScaling = false;
            scroll = new ScrollLogic(this);
        }

        public CursorType Cursor
        {
            get => (CursorType)GetValue(CursorProperty);
            set => SetValue(CursorProperty, value);
        }

        public event EventHandler<CanvasKeyEventArgs> KeyDown;

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
            set => scroll.VValue = value;
        }

        public double HValue
        {
            get => scroll.HValue;
            set => scroll.HValue = value;
        }

        public bool VBarVisible => scroll.VBarVisible;

        public bool HBarVisible => scroll.HBarVisible;

        public Animation ScrollAnimation { get; private set; }

        public Animation VScrollAnimation { get; private set; }

        public Animation HScrollAnimation { get; private set; }

        public bool IsVScrollAnimation => (VScrollAnimation ?? ScrollAnimation) != null;

        public bool IsHScrollAnimation => (HScrollAnimation ?? ScrollAnimation) != null;

        public event EventHandler<SKPaintSurfaceEventArgs> PaintContent;

        public event EventHandler<SKPaintSurfaceEventArgs> PaintOver;

        public event EventHandler ScrollComplete;

        protected override void OnSizeAllocated(double width, double height) => scroll.OnSizeAllocated(width, height, GetWindowScaleFunc?.Invoke() ?? 1D);

        public virtual void OnTouch(TouchEventArgs e) => scroll.OnTouch(e);

        public virtual void OnScrolled(TouchEventArgs e)
        {
            VAnimateScroll(VValue - (e.WheelDelta * 1.5), 16, 160);
            e.Handled = true;
        }

        protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;

            canvas.Scale(scroll.WindowScale, scroll.WindowScale);
            canvas.Clear(BackgroundColor.ToSKColor());
            base.OnPaintSurface(e);

            OnPaintContent(e);
            scroll.PaintScrollBars(canvas);
            PaintOver?.Invoke(this, e);
        }

        protected virtual void OnPaintContent(SKPaintSurfaceEventArgs e)
        {
            PaintContent?.Invoke(this, e);
        }

        public void HAnimateScroll(double newValue, uint rate = DefaultSKStyles.MaxSize, uint legth = 400, Easing asing = default)
        {
            if (HValue == newValue)
                return;
            if (HScrollAnimation != null)
            {
                this.AbortAnimation(ahHScroll);
            }
            newValue = scroll.NormalizeHValue(newValue);
            HScrollAnimation = new Animation(v => HValue = v, HValue, newValue, asing);
            HScrollAnimation.Commit(this, ahHScroll, rate, legth, finished: OnScrollComplete);
        }

        public void VAnimateScroll(double newValue, uint rate = DefaultSKStyles.MaxSize, uint legth = 400, Easing asing = default)
        {
            if (VValue == newValue)
                return;
            if (VScrollAnimation != null)
            {
                this.AbortAnimation(ahVScroll);
            }
            newValue = scroll.NormalizeVValue(newValue);
            VScrollAnimation = new Animation(v => VValue = v, VValue, newValue, asing);
            VScrollAnimation.Commit(this, ahVScroll, rate, legth, finished: OnScrollComplete);
        }

        protected void AnimateScroll(float top, double left)
        {
            if (VValue == top
                && HValue == left)
                return;
            if (ScrollAnimation != null)
            {
                this.AbortAnimation(ahScroll);
            }
            top = (float)scroll.NormalizeVValue(top);
            left = (float)scroll.NormalizeHValue(left);
            ScrollAnimation = new Animation();
            ScrollAnimation.Add(0, 1, new Animation(v => VValue = v, VValue, top, Easing.SinOut));
            ScrollAnimation.Add(0, 1, new Animation(v => HValue = v, HValue, left, Easing.SinOut));
            ScrollAnimation.Commit(this, ahScroll, 16, 270, finished: OnScrollComplete);
        }

        private void OnCursorChanged(CursorType oldValue, CursorType newValue)
        {
        }

        public virtual bool OnKeyDown(string keyName, KeyModifiers modifiers)
        {
            var args = new CanvasKeyEventArgs(keyName, modifiers);
            KeyDown?.Invoke(this, args);
            return args.Handled;
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
            ScrollComplete?.Invoke(this, EventArgs.Empty);
        }

        public virtual void InvalidatePaint()
        {
            if (Envir.MainContext == SynchronizationContext.Current)
                InvalidateSurface();
            else
                Envir.MainContext.Post(state => InvalidateSurface(), null);
        }

        public void CapturePointer(long pointerId)
        {
            CapturePointerFunc?.Invoke();
        }
    }
}
