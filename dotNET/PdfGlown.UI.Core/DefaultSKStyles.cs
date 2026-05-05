using SkiaSharp;

namespace PdfGlown.UI
{
    public static class DefaultSKStyles
    {
        public static readonly SKPaint PaintPageBackground = new() { Style = SKPaintStyle.Fill, Color = SKColors.White };
        public static readonly SKPaint PaintRed = new() { Style = SKPaintStyle.Stroke, Color = SKColors.OrangeRed };
        public static readonly SKPaint PaintBlue = new() { Style = SKPaintStyle.Stroke, Color = SKColors.BlueViolet };
        public static readonly SKPaint PaintGreen = new() { Style = SKPaintStyle.Stroke, Color = SKColors.DarkGreen };
        public static readonly SKPaint PaintPointFill = new() { Color = SKColors.White, Style = SKPaintStyle.Fill };
        public static readonly SKPaint PaintSelectionRect = new() { Color = SKColors.LightGreen.WithAlpha(125), Style = SKPaintStyle.Fill };
        public static readonly SKPaint PaintTextSelectionFill = new() { Color = SKColors.LightBlue, Style = SKPaintStyle.Fill, BlendMode = SKBlendMode.Multiply, IsAntialias = true };
        public static readonly SKPaint PaintBorderDefault = new() { Color = SKColors.Silver, Style = SKPaintStyle.Stroke, StrokeWidth = 0, IsAntialias = true };
        public static readonly SKPaint PaintBorderSelection = new() { Color = SKColors.Blue, Style = SKPaintStyle.Stroke, StrokeWidth = 0, IsAntialias = true };
        public static readonly SKPaint PaintToolTipText = new() { Style = SKPaintStyle.StrokeAndFill, Color = SKColors.Black, IsAntialias = true };
        public static readonly SKFont FontToolTipText = new() { Size = 14 };
        public static readonly SKPaint PaintToolTipHeadText = new() { Style = SKPaintStyle.StrokeAndFill, Color = SKColors.Black, IsAntialias = true };
        public static readonly SKFont FontToolTipHeadText = new() { Size = 12, Embolden = true };
        public static readonly SKPaint PaintToolTipBackground = new() { Color = SKColors.LightGoldenrodYellow, Style = SKPaintStyle.Fill, IsAntialias = true };
        public static readonly SKPaint PaintToolTipBorder = new() { Color = SKColors.DarkGray, Style = SKPaintStyle.Stroke, IsAntialias = true };

        public const int MinSize = 8;
        public const int MaxSize = 16;
        public const int StepSize = MaxSize;
    }

}

