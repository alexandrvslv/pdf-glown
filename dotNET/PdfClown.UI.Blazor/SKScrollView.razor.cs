using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using PdfClown.UI.Blazor.Internal;
using PdfClown.UI.Other;
using SkiaSharp;
using SkiaSharp.Views.Blazor;
using System.Runtime.Versioning;

namespace PdfClown.UI.Blazor
{
    [SupportedOSPlatform("browser")]
    public partial class SKScrollView : ComponentBase, ISKScrollView, IDisposable
    {
        private const string ahScroll = "ScrollAnimation";
        private const string ahVScroll = "VScrollAnimation";
        private const string ahHScroll = "HScrollAnimation";

#if __FORCE_GL__
        private SKGLView canvasView;
#else
        private SKCanvasView? canvasView;
#endif
        private SKHtmlScrollInterop? interop;
        private CursorType cursor;

        protected ScrollLogic scroll;

        public SKScrollView()
        {
            Envir.Init();
            //EnableTouchEvents = true;
            //this.Tou += OnTouch;
            scroll = new ScrollLogic(this);
        }

        [Inject]
        IJSRuntime JS { get; set; } = null!;

        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

        [Parameter]
        public string CanvasId { get; set; } = Guid.NewGuid().ToString("N");

        public CursorType Cursor
        {
            get => cursor;
            set
            {
                if (cursor != value)
                {
                    OnCursorChanged(cursor, value);
                }
            }
        }

        public virtual double Height
        {
            get => scroll.Height;
            set => scroll.Height = value;
        }

        public virtual double Width
        {
            get => scroll.Width;
            set => scroll.Width = value;
        }

        public bool AllowScrollView { get; set; } = true;

        public event EventHandler<CanvasKeyEventArgs>? KeyDown;

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

        public TimerAnimation? ScrollAnimation { get; private set; }

        public TimerAnimation? VScrollAnimation { get; private set; }

        public TimerAnimation? HScrollAnimation { get; private set; }

        public bool IsVScrollAnimation => (VScrollAnimation ?? ScrollAnimation) != null;

        public bool IsHScrollAnimation => (HScrollAnimation ?? ScrollAnimation) != null;

#if __FORCE_GL__
        public event EventHandler<SKPaintGLSurfaceEventArgs>? PaintContent;

        public event EventHandler<SKPaintGLSurfaceEventArgs>? PaintOver;
#else
        public event EventHandler<SKPaintSurfaceEventArgs>? PaintContent;

        public event EventHandler<SKPaintSurfaceEventArgs>? PaintOver;
#endif

        public event EventHandler? ScrollComplete;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            if (firstRender)
            {
                interop = await SKHtmlScrollInterop.ImportAsync(JS, CanvasId, OnPointerMove, OnSizeAllocated, OnMouseWheel);
                interop.Init();
            }
        }

        public void InvalidatePaint()
        {
            canvasView?.Invalidate();
        }

        protected virtual void OnTouch(TouchEventArgs e) => scroll.OnTouch(e);

        protected virtual void OnSizeAllocated(double width, double height) => scroll.OnSizeAllocated(width, height, SKHtmlScrollInterop.GetDPR());

        public virtual void OnScrolled(TouchEventArgs e)
        {
            if (e.WheelDelta == 0)
                return;
            VAnimateScroll(e.WheelDelta * 1.5, 16, 160);
        }

#if __FORCE_GL__
        protected virtual void OnPaintSurface(SKPaintGLSurfaceEventArgs e)
#else
        protected virtual void OnPaintSurface(SKPaintSurfaceEventArgs e)
#endif
        {
            var canvas = e.Surface.Canvas;

            canvas.Scale(scroll.WindowScale, scroll.WindowScale);
            canvas.Clear(SKColors.Silver);

            OnPaintContent(e);
            scroll.PaintScrollBars(canvas);
            PaintOver?.Invoke(this, e);
        }

#if __FORCE_GL__
        protected virtual void OnPaintContent(SKPaintGLSurfaceEventArgs e)
#else
        protected virtual void OnPaintContent(SKPaintSurfaceEventArgs e)
#endif
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

        private void AbortAnimation(string ahScroll)
        {
            TimerAnimation.Abort(this, ahScroll);
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

        private void OnCursorChanged(CursorType oldValue, CursorType newValue)
        {
            cursor = newValue;
            interop?.ChangeCursor(newValue);
        }

        public virtual bool OnKeyDown(string keyName, KeyModifiers modifiers)
        {
            var args = new CanvasKeyEventArgs(keyName, modifiers);
            KeyDown?.Invoke(this, args);
            return args.Handled;
        }

        private static KeyModifiers GetKeyModifiers(MouseEventArgs e)
        {
            var modifiers = KeyModifiers.None;
            if (e.AltKey)
                modifiers |= KeyModifiers.Alt;
            if (e.CtrlKey)
                modifiers |= KeyModifiers.Ctrl;
            if (e.ShiftKey)
                modifiers |= KeyModifiers.Shift;
            if (e.MetaKey)
                modifiers |= KeyModifiers.Meta;
            return modifiers;
        }

        private static KeyModifiers GetKeyModifiers(KeyboardEventArgs e)
        {
            var modifiers = KeyModifiers.None;
            if (e.AltKey)
                modifiers |= KeyModifiers.Alt;
            if (e.CtrlKey)
                modifiers |= KeyModifiers.Ctrl;
            if (e.ShiftKey)
                modifiers |= KeyModifiers.Shift;
            if (e.MetaKey)
                modifiers |= KeyModifiers.Meta;
            return modifiers;
        }

        private static KeyModifiers GetKeyModifiers(Microsoft.AspNetCore.Components.Web.TouchEventArgs e)
        {
            var modifiers = KeyModifiers.None;
            if (e.AltKey)
                modifiers |= KeyModifiers.Alt;
            if (e.CtrlKey)
                modifiers |= KeyModifiers.Ctrl;
            if (e.ShiftKey)
                modifiers |= KeyModifiers.Shift;
            if (e.MetaKey)
                modifiers |= KeyModifiers.Meta;
            return modifiers;
        }

        private static MouseButton GetMouseButton(MouseEventArgs e)
        {
            var button = e.Button;
            return GetMouseButton(button);
        }

        private static MouseButton GetMouseButton(long button)
        {
            return button switch
            {
                0 => MouseButton.Left,
                1 => MouseButton.Middle,
                2 => MouseButton.Right,
                _ => MouseButton.Unknown,
            };
        }

        private static MouseButton GetMouseButton(Microsoft.AspNetCore.Components.Web.TouchEventArgs e)
        {
            return MouseButton.Left;
        }

        private static SKPoint GetMouseLocation(MouseEventArgs e)
        {
            return new SKPoint((float)e.OffsetX, (float)e.OffsetY);
        }

        private static SKPoint GetMouseLocation(Microsoft.AspNetCore.Components.Web.TouchEventArgs e)
        {
            return new SKPoint((float)e.Touches[0].PageX, (float)e.Touches[0].PageY);
        }

        private void OnMouseWheel(double w, bool control)
        {
            OnScrolled(new TouchEventArgs(TouchAction.Released, MouseButton.Middle)
            {
                Location = new SKPoint(0,0),//  GetMouseLocation(e),
                WheelDelta = (float)w,
                KeyModifiers = control ? KeyModifiers.Ctrl : KeyModifiers.None,
            });
        }

        private void OnMouseWheel(WheelEventArgs e)
        {
            OnScrolled(new TouchEventArgs(TouchAction.Released, GetMouseButton(e))
            {
                Location = GetMouseLocation(e),
                WheelDelta = (float)e.DeltaY,
                KeyModifiers = GetKeyModifiers(e),
            });
        }

        private void OnPointerUp(PointerEventArgs e)
        {
            OnTouch(new TouchEventArgs(TouchAction.Released, GetMouseButton(e))
            {
                Location = GetMouseLocation(e),
                PointerId = e.PointerId,
                KeyModifiers = GetKeyModifiers(e),
            });
        }

        private void OnPointerDown(PointerEventArgs e)
        {
            OnTouch(new TouchEventArgs(TouchAction.Pressed, GetMouseButton(e))
            {
                Location = GetMouseLocation(e),
                PointerId = e.PointerId,
                KeyModifiers = GetKeyModifiers(e),
            });
        }

        private void OnPointerMove(int[] args)
        {
            OnTouch(new TouchEventArgs(TouchAction.Moved, GetMouseButton(args[1]))
            {
                Location = new SKPoint(args[2], args[3]),
                PointerId = args[0],
                KeyModifiers = (KeyModifiers)args[4],
            });
        }

        private void OnPointerMove(PointerEventArgs e)
        {
            OnTouch(new TouchEventArgs(TouchAction.Moved, GetMouseButton(e))
            {
                Location = GetMouseLocation(e),
                PointerId = e.PointerId,
                KeyModifiers = GetKeyModifiers(e),
            });
        }

        private void OnPointerEnter(PointerEventArgs e)
        {
            OnTouch(new TouchEventArgs(TouchAction.Entered, GetMouseButton(e))
            {
                Location = GetMouseLocation(e),
                PointerId = e.PointerId,
                KeyModifiers = GetKeyModifiers(e),
            });
        }

        private void OnPointerLeave(PointerEventArgs e)
        {
            OnTouch(new TouchEventArgs(TouchAction.Exited, GetMouseButton(e))
            {
                Location = GetMouseLocation(e),
                PointerId = e.PointerId,
                KeyModifiers = GetKeyModifiers(e),
            });
        }

        private void OnKeyDown(KeyboardEventArgs e)
        {
            OnKeyDown(e.Key, GetKeyModifiers(e));
        }

        public void Dispose()
        {
            interop?.DeInit();
            interop?.Dispose();
            interop = null;
        }

        public void CapturePointer(long pointerId)
        {
            interop?.SetCapture((int)pointerId);
        }
    }
}