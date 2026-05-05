using PdfGlown.Util.Math;
using SkiaSharp;
using System;
using System.Collections.Generic;

namespace PdfGlown.UI.ToolTip
{
    public abstract class ToolTipRenderer
    {
        public const int Indent = 3;
        public static readonly float MaxWidth = 360;

        protected List<LineOfText>? ContentLines;
        protected SKRect ContentBound;

        protected ToolTipRenderer()
        { }

        protected virtual void Clear()
        {
            if (ContentLines != null)
            {
                ContentLines.ForEach(x => x.Dispose());
                ContentLines.Clear();
            }
        }

        public virtual void Dispose()
        {
            Clear();
        }

        public List<LineOfText> MeasureText(string text, SKPaint paint, SKFont font, ref SKRect totalBounds)
        {
            var result = new List<LineOfText>();
            if (string.IsNullOrEmpty(text))
                return result;

            var enumerator = new StringSegmentEnumerator(text, font, paint, MaxWidth);
            while (enumerator.MoveNext())
            {
                var lof = MeasureLine(enumerator.Current, paint, font);

                lof.Bound.Offset(0, totalBounds.Bottom);

                totalBounds.Add(lof.Bound);

                result.Add(lof);
            }
            return result;
        }

        public LineOfText MeasureLine(ReadOnlySpan<char> text, SKPaint paint, SKFont font)
        {
            var textBlob = text.Length > 0 ? SKTextBlob.Create(text, font) : null;
            var contentBound = textBlob?.Bounds ?? new SKRect(0, 0, 0, 10);

            return new LineOfText
            {
                Start = new SKPoint(-contentBound.Left, -contentBound.Top),
                Bound = SKRect.Create(contentBound.Width, contentBound.Height),
                Text = textBlob,
                Paint = paint
            };
        }

        public abstract SKRect Measure();

        public void Draw(SKCanvas canvas, PdfViewState state, SKRect bound)
        {
            if (ContentLines == null)
                return;

            canvas.Save();
            canvas.SetMatrix(state.WindowMatrix);

            canvas.Translate(bound.Left, bound.Top);

            DrawContent(canvas);

            canvas.Restore();
        }

        private void DrawContent(SKCanvas canvas)
        {
            if (ContentLines == null)
                return;

            canvas.DrawRect(ContentBound, DefaultSKStyles.PaintToolTipBackground);
            canvas.DrawRect(ContentBound, DefaultSKStyles.PaintToolTipBorder);
            canvas.Translate(Indent, Indent);

            foreach (var line in ContentLines)
            {
                line.Draw(canvas);
            }
        }

        public abstract SKRect GetWindowBound(PdfViewState state);

        public class LineOfText : IDisposable
        {
            public SKRect Bound;
            public SKTextBlob? Text { get; set; }
            public SKPoint Start { get; set; }
            public SKPaint? Paint { get; set; }
            
            public void Dispose()
            {
                Text?.Dispose();
                Text = null;
            }

            public void Draw(SKCanvas canvas)
            {
                if (Text == null)
                    return;
#if NET9_0_OR_GREATER
                canvas.DrawText(Text, Start.X + Bound.Left, Start.Y + Bound.Top, Paint);
#else
                canvas.DrawText(Text, Start.X + Bound.Left, Start.Y + Bound.Top, Paint);
#endif
            }
        }

        private struct StringSegmentEnumerator
        {
            private readonly string _s;
            private readonly SKFont _font;
            private readonly SKPaint _paint;
            private int _start;
            private int _length;
            private float _maxWidth;

            public StringSegmentEnumerator(string s, SKFont font, SKPaint paint, float maxWidth)
            {
                _s = s;
                _font = font;
                _paint = paint;
                _start = 0;
                _length = 0;
                _maxWidth = maxWidth;
            }

            public readonly ReadOnlySpan<char> Current => _s.AsSpan(_start, _length);

            public bool MoveNext()
            {
                var currentPosition = _start + _length;

                if (currentPosition >= _s.Length)
                    return false;

                int start = -1;
                bool isBreak;
                char c;
                while (currentPosition < _s.Length)
                {
                    c = _s[currentPosition];
                    isBreak = IsBreak(c);
                    if (isBreak && start > -1)
                        break;
                    if (!isBreak && start == -1 && !IsTrimm(c))
                        start = currentPosition;
                    currentPosition++;
                }
                if (start == -1)
                    return false;

                if ((currentPosition - start) > 0 && float.IsFinite(_maxWidth))
                {
                    var span = _s.AsSpan(start, currentPosition - start);
#if NET9_0_OR_GREATER
                    var breakLength = (int)_font.BreakText(span, _maxWidth);
#else
                    var breakLength = (int)_paint.BreakText(span, _maxWidth);
#endif
                    currentPosition -= span.Length - breakLength;
                }

                _start = start;
                _length = currentPosition - start;

                return true;
            }

            private readonly bool IsBreak(char c)
            {
                return c == '\r'
                    || c == '\n';
            }

            private readonly bool IsTrimm(char c)
            {
                return c == ' ';
            }
        }
    }

}
