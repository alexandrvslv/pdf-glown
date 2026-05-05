/*
  Copyright 2008-2015 Stefano Chizzolini. http://www.pdfclown.org

  Contributors:
    * Stefano Chizzolini (original code developer, http://www.stefanochizzolini.it)

  This file should be part of the source code distribution of "PDF Glown library" (the
  Program): see the accompanying README files for more info.

  This Program is free software; you can redistribute it and/or modify it under the terms
  of the GNU Lesser General Public License as published by the Free Software Foundation;
  either version 3 of the License, or (at your option) any later version.

  This Program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY,
  either expressed or implied; without even the implied warranty of MERCHANTABILITY or
  FITNESS FOR A PARTICULAR PURPOSE. See the License for more details.

  You should have received a copy of the GNU Lesser General Public License along with this
  Program (see README files); if not, go to the GNU website (http://www.gnu.org/licenses/).

  Redistribution and use, with or without modification, are permitted provided that such
  redistributions retain the above copyright notice, license and disclaimer, along with
  this list of conditions.
*/

using PdfGlown.Documents.Contents.ColorSpaces;
using PdfGlown.Documents.Contents.Composition;
using PdfGlown.Documents.Contents.Objects;
using PdfGlown.Documents.Contents.XObjects;
using PdfGlown.Documents.Interaction.Annotations.ControlPoints;
using PdfGlown.Objects;
using PdfGlown.Util.Math;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;

namespace PdfGlown.Documents.Interaction.Annotations
{
    /// <summary>Free text annotation [PDF:1.6:8.4.5].</summary>
    /// <remarks>It displays text directly on the page. Unlike an ordinary text annotation, a free text
    /// annotation has no open or closed state; instead of being displayed in a pop-up window, the text
    /// is always visible.</remarks>
    [Pdf(VersionEnum.PDF13)]
    public sealed partial class FreeText : Markup
    {


        private static readonly JustificationEnum DefaultJustification = JustificationEnum.Left;
        private TextTopLeftControlPoint cpTexcTopLeft;
        private TextTopRightControlPoint cpTexcTopRight;
        private TextBottomLeftControlPoint cpTexcBottomLeft;
        private TextBottomRightControlPoint cpTexcBottomRight;
        private TextLineStartControlPoint cpLineStart;
        private TextLineEndControlPoint cpLineEnd;
        private TextLineKneeControlPoint cpLineKnee;
        private TextMidControlPoint cpTextMid;
        private SKRect? textBox;
        private CalloutLine line;
        private PdfPadding padding;

        public FreeText(PdfPage page, SKRect box, string text)
            : base(page, PdfName.FreeText, box, text)
        { }

        public FreeText(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        /// <summary>Gets/Sets the justification to be used in displaying the annotation's text.</summary>
        public JustificationEnum Justification
        {
            get => (JustificationEnum)GetInt(PdfName.Q);
            set
            {
                var oldValue = Justification;
                if (oldValue != value)
                {
                    Set(PdfName.Q, value != DefaultJustification ? (int)value : null);
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        public PdfArray Callout
        {
            get => Get<PdfArray>(PdfName.CL);
            set
            {
                var oldValue = Callout;
                if (!PdfArray.SequenceEquals(oldValue, value))
                {
                    this[PdfName.CL] = value;
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        public override string Contents
        {
            get => base.Contents;
            set
            {
                if (!string.Equals(Contents, value, StringComparison.Ordinal))
                {
                    base.Contents = value;
                    QueueRefreshAppearance();
                }
            }
        }

        /// <summary>Gets/Sets the callout line attached to the free text annotation.</summary>
        public CalloutLine Line
        {
            get => line ??= CalloutLine.Wrap(Callout, this);
            set
            {
                var oldValue = Line;
                Callout = value?.DataObject;
                line = value;
                if (value != null)
                {
                    // NOTE: To ensure the callout would be properly rendered, we have to declare the
                    // corresponding intent.
                    Intent = MarkupIntent.FreeTextCallout;
                    value.FreeText = this;
                }
                OnPropertyChanged(oldValue, value);
            }
        }

        /// <summary>Gets/Sets the style of the ending line ending.</summary>
        public LineEndStyleEnum LineEndStyle
        {
            get => LineEndStyleEnumExtension.Get(GetString(PdfName.LE));
            set
            {
                var oldValue = LineEndStyle;
                if (oldValue != value)
                {
                    SetName(PdfName.LE, value.GetName());
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        /// <summary>Popups not supported.</summary>
        public override Popup Popup
        {
            get => null;
            set => throw new NotSupportedException();
        }

        public SKRect UserTextBox
        {
            get => PageMatrix.MapRect(TextBox);
            set => TextBox = InvertPageMatrix.MapRect(value);
        }

        public SKRect TextBox
        {
            get => textBox ??= PrimitiveExtensions.Normalize(GetTextBox());
            set
            {
                var oldValue = textBox;
                var newValue = PrimitiveExtensions.Normalize(value).Round();
                if (oldValue != newValue)
                {
                    textBox = newValue;
                    OnPropertyChanged(oldValue, newValue);
                    QueueRefreshAppearance();
                }
            }
        }

        private SKRect GetTextBox()
        {
            var bounds = Box;
            var padding = Padding?.ToSKRect() ?? SKRect.Empty;
            return new SKRect(
                bounds.Left + padding.Left,
                bounds.Top + padding.Top,
                bounds.Right - padding.Right,
                bounds.Bottom - padding.Bottom);
        }

        public PdfPadding Padding
        {
            get => padding ??= PdfPadding.Wrap(PdfName.RD);
            set
            {
                var oldValue = Padding;
                if (!(oldValue?.Equals(value) ?? value == null))
                {
                    SetDirect(PdfName.RD, padding = value);
                    textBox = null;
                    OnPropertyChanged(oldValue, value);
                    QueueRefreshAppearance();
                }
            }
        }

        public SKPoint TextTopLeftPoint
        {
            get => new SKPoint(TextBox.Left, TextBox.Top);
            set => TextBox = new SKRect(value.X, value.Y, TextBox.Right, TextBox.Bottom);
        }

        public SKPoint TextTopRightPoint
        {
            get => new SKPoint(TextBox.Right, TextBox.Top);
            set => TextBox = new SKRect(TextBox.Left, value.Y, value.X, TextBox.Bottom);
        }

        public SKPoint TextBottomLeftPoint
        {
            get => new SKPoint(TextBox.Left, TextBox.Bottom);
            set => TextBox = new SKRect(value.X, TextBox.Top, TextBox.Right, value.Y);
        }

        public SKPoint TextBottomRightPoint
        {
            get => new SKPoint(TextBox.Right, TextBox.Bottom);
            set => TextBox = new SKRect(TextBox.Left, TextBox.Top, value.X, value.Y);
        }

        public SKPoint TextMidPoint
        {
            get => new SKPoint(TextBox.MidX, TextBox.MidY);
            set
            {
                var textBox = TextBox;
                var oldMid = new SKPoint(textBox.MidX, textBox.MidY);
                textBox.Location += value - oldMid;
                TextBox = textBox;
            }
        }

        public override bool ShowToolTip => false;

        protected override List<ContentObject> DAOperations
        {
            get => daOperation ?? GetDSOperations() ?? base.DAOperations;
            set => base.DAOperations = value;
        }

        private List<ContentObject> GetDSOperations()
        {
            if (Get<PdfString>(PdfName.DS) is not PdfString ds
                || string.IsNullOrWhiteSpace(ds.StringValue))
                return null;
            daOperation = new List<ContentObject>();
            var operations = ds.StringValue.Split(';', StringSplitOptions.RemoveEmptyEntries);
            float fontSize = 0;
            string fontName = string.Empty;
            string fontStyle = string.Empty;
            SKColor? color = null;
            foreach (var operation in operations)
            {
                var parts = operation.Split(':', StringSplitOptions.RemoveEmptyEntries);
                var key = parts[0].Trim();
                var value = parts[1].Trim();
                switch (key)
                {
                    case "font":
                        var fontParts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        foreach (var part in fontParts)
                        {
                            if (part.EndsWith("pt", StringComparison.OrdinalIgnoreCase))
                            {
                                fontSize = float.Parse(part.Replace("pt", string.Empty).Trim(), CultureInfo.InvariantCulture);
                            }
                            else if (fontName == string.Empty)
                            {
                                fontName = part.Split(',', StringSplitOptions.RemoveEmptyEntries).First().Trim();
                            }
                        }
                        break;
                    case "font-family":
                        fontName = value.Split(',', StringSplitOptions.RemoveEmptyEntries).First().Trim();
                        break;
                    case "font-size":
                        fontSize = float.Parse(value.Replace("pt", string.Empty).Trim(), CultureInfo.InvariantCulture);
                        break;
                    case "font-style":
                        fontStyle = value.Trim();
                        break;
                    case "font-weight":
                        break;
                    case "color":
                        if (SKColor.TryParse(value, out var parsed))
                            color = parsed;
                        break;
                }
            }
            if (fontName != string.Empty)
            {
                if (Appearance.Normal[null].Resources.Fonts.TryGetByName(fontName, out var result))
                    daOperation.Add(new SetFont(result.Key, fontSize));
            }
            //else if (fontSize != 0)
            //    list.Add(new SetFont(PdfName.Get(fontName), fontSize));
            if (color is SKColor colorValue)
                daOperation.Add(new SetRGBFillColor(RGBColor.Get(colorValue)));
            return daOperation;
        }

        protected override void OnPropertyChanged<T>(T oldValue, T newValue, [CallerMemberName] string propertyName = "")
        {
            if (propertyName == nameof(Rect))
            { textBox = null; }
            base.OnPropertyChanged(oldValue, newValue, propertyName);
        }

        protected override FormXObject GenerateAppearance()
        {
            if ((queueRefresh & RefreshAppearanceState.Move) == RefreshAppearanceState.Move)
                return null;

            var normalAppearance = ResetAppearance(out var matrix);
            var textBounds = TextBox;
            //SKRect box = Box;
            var font = DASetFont?.Name ?? normalAppearance.GetDefaultFont(out _);
            var fontSize = DASetFont?.Size ?? 10;
            var composer = new PrimitiveComposer(normalAppearance);
            {
                textBounds = matrix.MapRect(textBounds);

                if (Border != null || BorderEffect != null || Color != null)
                {
                    using var path = new SKPath();
                    path.AddRect(textBounds);

                    composer.BeginLocalState();
                    composer.SetStrokeColor(RGBColor.Default);
                    composer.SetFillColor(Color ?? RGBColor.White);

                    using var tpath = ApplyBorderAndEffect(composer, path);

                    composer.DrawPath(tpath);
                    composer.FillStroke();
                    composer.End();
                }

                if (Intent == MarkupIntent.FreeTextCallout && Line != null)
                {
                    composer.BeginLocalState();
                    composer.SetStrokeColor(RGBColor.Default);
                    composer.SetFillColor(Color ?? RGBColor.White);
                    ApplyBorder(composer);

                    var startPoint = matrix.MapPoint(Line.Start);
                    var endPoint = matrix.MapPoint(Line.End);
                    var kneePoint = Line.Knee is SKPoint knee ? matrix.MapPoint(knee) : (SKPoint?)null;
                    composer.StartPath(startPoint);
                    if (kneePoint != null)
                        composer.DrawLine(kneePoint.Value);
                    composer.DrawLine(endPoint);
                    composer.Stroke();
                    var normal = kneePoint != null
                        ? SKPoint.Normalize(startPoint - kneePoint.Value)
                        : SKPoint.Normalize(startPoint - endPoint);
                    var invertNormal = normal.Invert();
                    if (LineEndStyle == LineEndStyleEnum.Circle)
                    {
                        composer.DrawCircle(startPoint, 4);
                        composer.FillStroke();
                    }
                    else if (LineEndStyle == LineEndStyleEnum.Square)
                    {
                        composer.DrawQuad(startPoint, invertNormal.Multiply(4));
                        composer.FillStroke();
                    }
                    else if (LineEndStyle == LineEndStyleEnum.OpenArrow)
                    {
                        composer.AddOpenArrow(startPoint, invertNormal);
                        composer.Stroke();
                    }
                    else if (LineEndStyle == LineEndStyleEnum.ClosedArrow)
                    {
                        composer.AddClosedArrow(startPoint, invertNormal);
                        composer.FillStroke();
                    }
                    composer.End();
                }

                if (Contents != null)
                {
                    var block = new BlockComposer(composer);
                    block.Begin(textBounds, XAlignmentEnum.Left, YAlignmentEnum.Top);
                    var isFontSet = false;
                    var isFillColorSet = false;
                    if (DAOperations is List<ContentObject> daOperations)
                    {
                        foreach (var operation in daOperations)
                        {
                            composer.Add(operation);
                            if (operation is SetFont)
                                isFontSet = true;
                            else if (operation is SetColor)
                                isFillColorSet = true;
                        }
                    }
                    if (!isFontSet)
                    {
                        composer.SetFont(font, fontSize);
                    }
                    if (!isFillColorSet)
                    {
                        composer.SetFillColor(RGBColor.Default);
                    }

                    block.ShowText(Contents);
                    block.End();
                }

                composer.Flush();
            }
            return normalAppearance;
        }

        public override void MoveTo(SKRect newBox)
        {
            queueRefresh |= RefreshAppearanceState.Move;
            try
            {
                var oldBox = Box;
                var textBox = TextBox;

                var dif = SKMatrix.CreateIdentity()
                    .PreConcat(SKMatrix.CreateTranslation(newBox.MidX, newBox.MidY))
                    .PreConcat(SKMatrix.CreateScale(newBox.Width / oldBox.Width, newBox.Height / oldBox.Height))
                    .PreConcat(SKMatrix.CreateTranslation(-oldBox.MidX, -oldBox.MidY));

                if (Intent == MarkupIntent.FreeTextCallout && Line != null)
                {
                    Line.Start = dif.MapPoint(Line.Start);
                    Line.End = dif.MapPoint(Line.End);
                    if (Line.Knee != null)
                        Line.Knee = dif.MapPoint(Line.Knee.Value);
                }
                Box = newBox;
                TextBox = dif.MapRect(textBox);
                var padding = Padding?.ToSKRect();
                if (padding != null)
                {
                    Padding = new PdfPadding(new SKRect(
                       TextBox.Left - Box.Left,
                       TextBox.Top - Box.Top,
                       Box.Right - TextBox.Right,
                       Box.Bottom - TextBox.Bottom));
                }

                GenerateAppearance();
            }
            finally
            {
                queueRefresh &= ~RefreshAppearanceState.Move;
            }
        }

        public void CalcLine()
        {
            if (Intent == MarkupIntent.FreeTextCallout && Line != null)
            {
                var textBox = TextBox;
                var textBoxInflate = SKRect.Inflate(textBox, 15, 15);
                var midpoint = TextMidPoint;
                var start = Line.Start;
                if (start.X > (textBox.Left - 5) && start.X < (textBox.Right + 5))
                {
                    if (start.Y < textBox.Top)
                    {
                        Line.SetEnd(new SKPoint(textBox.MidX, textBox.Top));
                        if (Line.Knee != null)
                        {
                            Line.SetKnee(new SKPoint(textBoxInflate.MidX, textBoxInflate.Top));
                        }
                    }
                    else
                    {
                        Line.SetEnd(new SKPoint(textBox.MidX, textBox.Bottom));
                        if (Line.Knee != null)
                        {
                            Line.SetKnee(new SKPoint(textBoxInflate.MidX, textBoxInflate.Bottom));
                        }
                    }
                }
                else if (start.X < textBox.Left)
                {
                    Line.SetEnd(new SKPoint(textBox.Left, textBox.MidY));
                    if (Line.Knee != null)
                    {
                        Line.SetKnee(new SKPoint(textBoxInflate.Left, textBoxInflate.MidY));
                    }
                }
                else
                {
                    Line.SetEnd(new SKPoint(textBox.Right, textBox.MidY));
                    if (Line.Knee != null)
                    {
                        Line.SetKnee(new SKPoint(textBoxInflate.Right, textBoxInflate.MidY));
                    }
                }
            }
        }

        public override void RefreshBox()
        {
            if ((queueRefresh & RefreshAppearanceState.User) != RefreshAppearanceState.User)
                return;
            var textBox = TextBox;
            CalcLine();
            var box = textBox;
            ApplyBorderAndEffect(ref box);


            if (Intent == MarkupIntent.FreeTextCallout && Line != null)
            {
                box.Add(Line.Start);
                if (Line.Knee is SKPoint knee)
                    box.Add(knee);
                box.Add(Line.End);
                if (LineEndStyle != LineEndStyleEnum.None)
                {
                    box.Inflate(5, 5);
                }
            }
            Box = box;
            Padding = new PdfPadding(new SKRect(
                       textBox.Left - box.Left,
                       textBox.Top - box.Top,
                       box.Right - textBox.Right,
                       box.Bottom - textBox.Bottom));
        }

        public override IEnumerable<ControlPoint> GetControlPoints()
        {
            if (Intent == MarkupIntent.FreeTextCallout && Line != null)
            {
                yield return cpTexcTopLeft ??= new TextTopLeftControlPoint { Annotation = this };
                yield return cpTexcTopRight ??= new TextTopRightControlPoint { Annotation = this };
                yield return cpTexcBottomLeft ??= new TextBottomLeftControlPoint { Annotation = this };
                yield return cpTexcBottomRight ??= new TextBottomRightControlPoint { Annotation = this };
                yield return cpTextMid ??= new TextMidControlPoint { Annotation = this };

                yield return cpLineStart ??= new TextLineStartControlPoint { Annotation = this };
                yield return cpLineEnd ??= new TextLineEndControlPoint { Annotation = this };
                if (Line.Knee != null)
                {
                    yield return cpLineKnee ??= new TextLineKneeControlPoint { Annotation = this };
                }
            }

            foreach (var cpBase in GetDefaultControlPoint())
            {
                yield return cpBase;
            }

        }

        public override PdfObject Clone(Cloner cloner)
        {
            var cloned = (FreeText)base.Clone(cloner);
            cloned.cpTexcTopLeft = null;
            cloned.cpTexcTopRight = null;
            cloned.cpTexcBottomLeft = null;
            cloned.cpTexcBottomRight = null;
            cloned.cpLineStart = null;
            cloned.cpLineEnd = null;
            cloned.cpLineKnee = null;
            cloned.cpTextMid = null;
            return cloned;
        }
    }

    public abstract class FreeTextControlPoint : ControlPoint
    {
        public FreeText FreeText => (FreeText)Annotation;
    }

    public class TextLineStartControlPoint : FreeTextControlPoint
    {
        public override SKPoint GetPoint() => FreeText.Line.Start;

        public override void SetPoint(SKPoint point)
        {
            base.SetPoint(point);
            FreeText.Line.Start = point;
        }
    }

    public class TextLineEndControlPoint : FreeTextControlPoint
    {
        public override SKPoint GetPoint() => FreeText.Line.End;

        public override void SetPoint(SKPoint point)
        {
            base.SetPoint(point);
            FreeText.Line.End = point;
        }
    }

    public class TextLineKneeControlPoint : FreeTextControlPoint
    {
        public override SKPoint GetPoint() => FreeText.Line.Knee ?? SKPoint.Empty;

        public override void SetPoint(SKPoint point)
        {
            base.SetPoint(point);
            FreeText.Line.Knee = point;
        }
    }

    public class TextTopLeftControlPoint : FreeTextControlPoint
    {
        public override SKPoint GetPoint() => FreeText.TextTopLeftPoint;

        public override void SetPoint(SKPoint point)
        {
            base.SetPoint(point);
            FreeText.TextTopLeftPoint = point;
        }
    }

    public class TextTopRightControlPoint : FreeTextControlPoint
    {
        public override SKPoint GetPoint() => FreeText.TextTopRightPoint;

        public override void SetPoint(SKPoint point)
        {
            base.SetPoint(point);
            FreeText.TextTopRightPoint = point;
        }
    }

    public class TextBottomLeftControlPoint : FreeTextControlPoint
    {
        public override SKPoint GetPoint() => FreeText.TextBottomLeftPoint;

        public override void SetPoint(SKPoint point)
        {
            base.SetPoint(point);
            FreeText.TextBottomLeftPoint = point;
        }
    }

    public class TextBottomRightControlPoint : FreeTextControlPoint
    {
        public override SKPoint GetPoint() => FreeText.TextBottomRightPoint;

        public override void SetPoint(SKPoint point)
        {
            base.SetPoint(point);
            FreeText.TextBottomRightPoint = point;
        }
    }

    public class TextMidControlPoint : FreeTextControlPoint
    {
        public override SKPoint GetPoint() => FreeText.TextMidPoint;

        public override void SetPoint(SKPoint point)
        {
            base.SetPoint(point);
            FreeText.TextMidPoint = point;
        }
    }
}