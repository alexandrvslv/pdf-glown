using PdfGlown.Tools;
using SkiaSharp;
using System;
using System.Runtime.CompilerServices;

namespace PdfGlown.UI
{
    public sealed class ScrollLogic
    {
        private static readonly SKPaint bottonPaint = new()
        {
            Style = SKPaintStyle.StrokeAndFill,
            StrokeWidth = 1,
            IsAntialias = true,
            Color = new SKColor(190, 190, 190)
        };

        private static readonly SKPaint bottonPaintHover = new()
        {
            Style = SKPaintStyle.StrokeAndFill,
            StrokeWidth = 1,
            IsAntialias = true,
            Color = new SKColor(160, 160, 160)
        };

        private static readonly SKPaint bottonPaintPressed = new()
        {
            Style = SKPaintStyle.StrokeAndFill,
            StrokeWidth = 1,
            IsAntialias = true,
            Color = new SKColor(120, 120, 120)
        };

        private static readonly SKPaint scrollPaint = new()
        {
            Style = SKPaintStyle.StrokeAndFill,
            StrokeWidth = 1,
            IsAntialias = true,
            Color = new SKColor(240, 240, 240)
        };

        private static readonly SKPaint svgPaint = new()
        {
            IsAntialias = true,
            ColorFilter = SKColorFilter.CreateBlendMode(SKColors.Black, SKBlendMode.SrcIn),
        };

        public static readonly SvgImage UpSvg = SvgImage.GetCache(typeof(Orientation).Assembly, "caret-up");
        public static readonly SvgImage DownSvg = SvgImage.GetCache(typeof(Orientation).Assembly, "caret-down");
        public static readonly SvgImage LeftSvg = SvgImage.GetCache(typeof(Orientation).Assembly, "caret-left");
        public static readonly SvgImage RightSvg = SvgImage.GetCache(typeof(Orientation).Assembly, "caret-right");
        public static readonly SvgImage ShiftVSvg = SvgImage.GetCache(typeof(Orientation).Assembly, "line");
        public static readonly SvgImage ShiftHSvg = SvgImage.GetCache(typeof(Orientation).Assembly, "link");

        private EventHandler<ScrollEventArgs>? vScrolledHandler;
        private EventHandler<ScrollEventArgs>? нScrolledHandler;
        private SKPoint nullLocation;
        private Orientation nullDirection = Orientation.Vertical;

        private bool vHovered;
        private bool нHovered;
        private double vMax;
        private double hMax;
        private double vHeight;
        private double vWidth;
        private double vsHeight;
        private double hsWidth;
        private double kWidth;
        private double kHeight;
        private ISKScrollView scrollView;
        private SKRect vPadding = new SKRect(0, 0, 0, DefaultSKStyles.StepSize);
        private SKRect hPadding = new SKRect(0, 0, DefaultSKStyles.StepSize, 0);
        private SvgImage? hoverButton;
        private SvgImage? pressedButton;
        private double vValue;
        private double hValue;

        public ScrollLogic(ISKScrollView scrollView)
        {
            this.scrollView = scrollView;
        }

        public double Width { get; set; }

        public double Height { get; set; }

        public double VMaximum
        {
            get => vMax;
            set
            {
                if (vMax != value)
                    OnVMaximumChanged(vMax, value);
            }
        }

        public double HMaximum
        {
            get => hMax;
            set
            {
                if (hMax != value)
                    OnHMaximumChanged(hMax, value);
            }
        }

        public double VValue
        {
            get => vValue;
            set
            {
                value = NormalizeVValue(value);
                if (value != vValue)
                    OnVValueChanged(vValue, value);
            }
        }

        public double HValue
        {
            get => hValue;
            set
            {
                value = NormalizeHValue(value);
                if (value != hValue)
                    OnHValueChanged(hValue, value);
            }
        }

        public double VSize => VHovered ? DefaultSKStyles.MaxSize : DefaultSKStyles.MinSize;

        public double HSize => HHovered ? DefaultSKStyles.MaxSize : DefaultSKStyles.MinSize;

        public bool VBarVisible => VMaximum >= (Height + DefaultSKStyles.StepSize);

        public bool HBarVisible => HMaximum >= (Width + DefaultSKStyles.StepSize);

        public bool VHovered
        {
            get => vHovered;
            set
            {
                if (vHovered != value)
                {
                    vHovered = value;
                    OnPropertyChanged();
                    OnVMaximumChanged(1, VMaximum);
                    scrollView.InvalidatePaint();
                }
            }
        }

        public bool HHovered
        {
            get => нHovered;
            set
            {
                if (нHovered != value)
                {
                    нHovered = value;
                    OnPropertyChanged();
                    OnHMaximumChanged(1, HMaximum);
                    scrollView.InvalidatePaint();
                }
            }
        }

        public SKRect VPadding
        {
            get => vPadding;
            set
            {
                if (vPadding != value)
                {
                    vPadding = value;
                    OnVMaximumChanged(1, VMaximum);
                }
            }
        }

        public SKRect HPadding
        {
            get => hPadding;
            set
            {
                if (hPadding != value)
                {
                    hPadding = value;
                    OnHMaximumChanged(1, HMaximum);
                }
            }
        }

        public SvgImage? PressedButton
        {
            get => pressedButton;
            set
            {
                if (pressedButton != value)
                {
                    pressedButton = value;
                    scrollView.InvalidatePaint();
                }
            }
        }

        public SvgImage? HoverButton
        {
            get => hoverButton;
            set
            {
                if (hoverButton != value)
                {
                    hoverButton = value;
                    scrollView.InvalidatePaint();
                }
            }
        }

        public float WindowScale { get; set; } = 1;

        public event EventHandler<ScrollEventArgs> VScrolled
        {
            add => vScrolledHandler += value;
            remove => vScrolledHandler -= value;
        }

        public event EventHandler<ScrollEventArgs> HScrolled
        {
            add => нScrolledHandler += value;
            remove => нScrolledHandler -= value;
        }

        public double NormalizeVValue(double value)
        {
            if (!VBarVisible)
            {
                return 0;
            }

            var max = VMaximum - Height;// + DefaultSKStyles.StepSize;
            return value < 0 || max < 0 ? 0 : value > max ? max : value;
        }

        public double NormalizeHValue(double value)
        {
            var max = HMaximum - Width;// + DefaultSKStyles.StepSize;
            value = value < 0 || max < 0 ? 0 : value > max ? max : value;
            return value;
        }

        public void PaintScrollBars(SKCanvas canvas)
        {
            if (VBarVisible)
            {
                canvas.DrawRect(GetVScrollBounds(), scrollPaint);
                // if (Hovered)
                {
                    var upBound = GetUpBounds();
                    if (HoverButton == UpSvg)
                        canvas.DrawRect(upBound, PressedButton == UpSvg ? bottonPaintPressed : bottonPaintHover);
                    SvgImage.DrawImage(canvas, UpSvg, svgPaint, upBound, 0.5F);

                    var downBound = GetDownBounds();
                    if (HoverButton == DownSvg)
                        canvas.DrawRect(downBound, PressedButton == DownSvg ? bottonPaintPressed : bottonPaintHover);
                    SvgImage.DrawImage(canvas, DownSvg, svgPaint, downBound, 0.5F);
                }
                var valueBound = GetVValueBounds();
                valueBound.Inflate(-1, -1);
                canvas.DrawRect(valueBound, HoverButton == ShiftVSvg ? PressedButton == ShiftVSvg ? bottonPaintPressed : bottonPaintHover : bottonPaint);
            }

            if (HBarVisible)
            {
                canvas.DrawRect(GetHScrollBounds(), scrollPaint);
                // if (Hovered)
                {
                    var leftBound = GetLeftBounds();
                    if (HoverButton == LeftSvg)
                        canvas.DrawRect(leftBound, PressedButton == LeftSvg ? bottonPaintPressed : bottonPaintHover);
                    SvgImage.DrawImage(canvas, LeftSvg, svgPaint, leftBound, 0.5F);

                    var rightBound = GetRightBounds();
                    if (HoverButton == RightSvg)
                        canvas.DrawRect(rightBound, PressedButton == RightSvg ? bottonPaintPressed : bottonPaintHover);
                    SvgImage.DrawImage(canvas, RightSvg, svgPaint, rightBound, 0.5F);
                }
                var valueBound = GetHValueBounds();
                valueBound.Inflate(-1, -1);
                canvas.DrawRect(valueBound, HoverButton == ShiftHSvg ? PressedButton == ShiftHSvg ? bottonPaintPressed : bottonPaintHover : bottonPaint);
            }
        }

        private void OnVValueChanged(double oldValue, double newValue)
        {
            vValue = newValue;
            vScrolledHandler?.Invoke(this, new ScrollEventArgs((int)newValue));
            scrollView.InvalidatePaint();
        }

        private void OnHValueChanged(double oldValue, double newValue)
        {
            hValue = newValue;
            нScrolledHandler?.Invoke(this, new ScrollEventArgs((int)newValue));
            scrollView.InvalidatePaint();
        }

        private void OnVMaximumChanged(double oldValue, double newValue)
        {
            vMax = newValue;
            vsHeight = (Height - (vPadding.Top + vPadding.Bottom)) - VSize * 2;
            kHeight = vsHeight / newValue;
            vHeight = (float)((Height - DefaultSKStyles.StepSize) * kHeight);

            if (vHeight < VSize)
            {
                vHeight = VSize;
                vsHeight = (Height - (vPadding.Top + vPadding.Bottom)) - (VSize * 2 + vHeight);
                kHeight = vsHeight / newValue;
            }
            //else
            //{
            //    vHeight += VericalSize;
            //}

            VValue = VValue;
        }

        private void OnHMaximumChanged(double oldValue, double newValue)
        {
            hMax = newValue;
            hsWidth = (Width - (hPadding.Left + hPadding.Right)) - HSize * 2;
            kWidth = hsWidth / newValue;
            vWidth = (float)((Width - DefaultSKStyles.StepSize) * kWidth);

            //if (vWidth < HorizontalSize)
            //{
            //    vWidth = HorizontalSize;
            //    hsWidth = (Width - horizontalPadding.HorizontalThickness) - (HorizontalSize * 2 + vWidth);
            //    kWidth = hsWidth / newValue;
            //}
            //else
            //{
            //    vWidth += HorizontalSize;
            //}

            HValue = HValue;
        }

        public SKRect GetVScrollBounds()
        {
            return SKRect.Create((float)(Width - VSize),
                (float)vPadding.Top,
                (float)VSize,
                (float)(Height - (vPadding.Top + vPadding.Bottom)));
        }

        public SKRect GetHScrollBounds()
        {
            return SKRect.Create((float)hPadding.Left,
                (float)(Height - HSize),
                (float)(Width - (hPadding.Left + hPadding.Right)),
                (float)HSize);
        }

        private SKRect GetUpBounds()
        {
            return SKRect.Create((float)(Width - VSize),
                (float)vPadding.Top,
                (float)VSize, (float)VSize);
        }

        private SKRect GetDownBounds()
        {
            return SKRect.Create((float)(Width - VSize),
                (float)(Height - (VSize + vPadding.Bottom)),
                (float)VSize, (float)VSize);
        }

        private SKRect GetLeftBounds()
        {
            return SKRect.Create((float)hPadding.Left,
                (float)(Height - HSize),
                (float)HSize, (float)HSize);
        }

        private SKRect GetRightBounds()
        {
            return SKRect.Create((float)(Width - (HSize + hPadding.Right)),
                (float)(Height - HSize),
                (float)HSize, (float)HSize);
        }

        public SKRect GetVValueBounds()
        {
            var top = VSize + vPadding.Top + (float)(VValue * kHeight);
            return SKRect.Create((float)(Width - VSize),
                (float)top, (float)VSize, (float)vHeight);
        }

        public SKRect GetHValueBounds()
        {
            var left = HSize + hPadding.Left + (HValue * kWidth);
            return SKRect.Create((float)left, (float)(Height - HSize), (float)vWidth, (float)HSize);
        }

        public void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
        }

        public void OnSizeAllocated(double width, double height, double windowScale)
        {
            Width = width;
            Height = height;
            WindowScale = (float)windowScale;
            OnVMaximumChanged(1, VMaximum);
            OnHMaximumChanged(1, HMaximum);
        }

        public void OnTouch(TouchEventArgs e)
        {
            switch (e.ActionType)
            {
                case TouchAction.Released:
                    PressedButton = null;
                    if (nullLocation != SKPoint.Empty)
                    {
                        nullLocation = SKPoint.Empty;
                        e.Handled = true;
                    }
                    goto case TouchAction.Exited;
                case TouchAction.Exited:
                    HoverButton = null;
                    goto case TouchAction.Moved;
                case TouchAction.Moved:
                    if (nullLocation == SKPoint.Empty)
                    {
                        VHovered = VBarVisible && GetVScrollBounds().Contains(e.Location);
                        HHovered = HBarVisible && GetHScrollBounds().Contains(e.Location);

                        if (VHovered)
                        {
                            e.Handled = true;

                            if (GetUpBounds().Contains(e.Location))
                            {
                                HoverButton = UpSvg;
                            }
                            else if (GetDownBounds().Contains(e.Location))
                            {
                                HoverButton = DownSvg;
                            }
                            else if (GetVValueBounds().Contains(e.Location))
                            {
                                HoverButton = ShiftVSvg;
                            }
                            else
                            {
                                HoverButton = null;
                            }
                        }
                        else if (HHovered)
                        {
                            e.Handled = true;

                            if (GetLeftBounds().Contains(e.Location))
                            {
                                HoverButton = LeftSvg;
                            }
                            else if (GetRightBounds().Contains(e.Location))
                            {
                                HoverButton = RightSvg;
                            }
                            else if (GetHValueBounds().Contains(e.Location))
                            {
                                HoverButton = ShiftHSvg;
                            }
                            else
                            {
                                HoverButton = null;
                            }
                        }
                        else
                            HoverButton = null;
                    }
                    break;
                case TouchAction.WheelChanged:
                    //if (VerticalScrollBarVisible)
                    //{
                    //    var temp = VerticalValue;
                    //    VerticalValue = VerticalValue - step * Math.Sign(e.WheelDelta);
                    //    if (temp != VerticalValue)
                    //    {
                    //        e.Handled = true;
                    //    }
                    //}
                    break;
            }

            if ((!VBarVisible && !HBarVisible)
                || e.MouseButton != MouseButton.Left)
                return;
            switch (e.ActionType)
            {
                case TouchAction.Moved:
                    if (nullLocation != SKPoint.Empty)
                    {
                        var newLocation = e.Location - nullLocation;
                        if (nullDirection == Orientation.Vertical)
                        {
                            VValue += newLocation.Y / kHeight;
                        }
                        else if (nullDirection == Orientation.Horizontal)
                        {
                            HValue += newLocation.X / kWidth;
                        }
                        nullLocation = e.Location;
                        e.Handled = true;
                    }
                    break;
                case TouchAction.Pressed:
                    var verticalScrollBound = GetVScrollBounds();
                    var нorizontalScrollBound = GetHScrollBounds();
                    if (verticalScrollBound.Contains(e.Location))
                    {
                        e.Handled = true;
                        var upBound = GetUpBounds();
                        if (upBound.Contains(e.Location))
                        {
                            VValue -= DefaultSKStyles.StepSize;
                            PressedButton = UpSvg;
                            return;
                        }

                        var downBound = GetDownBounds();
                        if (downBound.Contains(e.Location))
                        {
                            VValue += DefaultSKStyles.StepSize;
                            PressedButton = DownSvg;
                            return;
                        }

                        var valueBound = GetVValueBounds();
                        if (valueBound.Contains(e.Location))
                        {
                            nullLocation = e.Location;
                            nullDirection = Orientation.Vertical;
                            PressedButton = ShiftVSvg;
                            scrollView.CapturePointer(e.PointerId);
                            return;
                        }

                        VValue = e.Location.Y / kHeight - Height / 2;
                    }
                    if (нorizontalScrollBound.Contains(e.Location))
                    {
                        e.Handled = true;
                        var leftBound = GetLeftBounds();
                        if (leftBound.Contains(e.Location))
                        {
                            HValue -= DefaultSKStyles.StepSize;
                            PressedButton = LeftSvg;
                            return;
                        }

                        var rightBound = GetRightBounds();
                        if (rightBound.Contains(e.Location))
                        {
                            HValue += DefaultSKStyles.StepSize;
                            PressedButton = RightSvg;
                            return;
                        }

                        var valueBound = GetHValueBounds();
                        if (valueBound.Contains(e.Location))
                        {
                            nullLocation = e.Location;
                            nullDirection = Orientation.Horizontal;
                            PressedButton = ShiftHSvg;
                            scrollView.CapturePointer(e.PointerId);
                            return;
                        }

                        HValue = e.Location.X / kWidth - Width / 2;
                    }
                    break;
            }
        }

        public void RaiseHScroll()
        {
            OnHValueChanged(0, hValue);
        }

        public void RaiseVScroll()
        {
            OnVValueChanged(0, vValue);
        }
    }
}
