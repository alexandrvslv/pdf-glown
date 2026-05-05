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
using PdfGlown.Documents.Contents.XObjects;
using PdfGlown.Documents.Interaction.Annotations.ControlPoints;
using PdfGlown.Objects;
using PdfGlown.Util;
using PdfGlown.Util.Math;
using SkiaSharp;
using System;
using System.Collections.Generic;

namespace PdfGlown.Documents.Interaction.Annotations
{
    /// <summary>Line annotation [PDF:1.6:8.4.5].</summary>
    /// <remarks>It displays displays a single straight line on the page.
    /// When opened, it displays a pop-up window containing the text of the associated note.</remarks>
    [Pdf(VersionEnum.PDF13)]
    public sealed class Line : Markup
    {
        private const int DefaultFontSize = 9;
        private const int StyleRadius = 4;
        private static readonly double DefaultLeaderLineExtension = 0;
        private static readonly double DefaultLeaderLineLength = 0;
        private static readonly double DefaultLeaderLineOffset = 0;
        private static readonly LineEndStyleEnum DefaultLineEndStyle = LineEndStyleEnum.None;
        private static readonly BiDictionary<LineCaptionPosition, PdfName> lcpCodes = new()
        {
            [LineCaptionPosition.Inline] = PdfName.Inline,
            [LineCaptionPosition.Top] = PdfName.Top
        };

        public static LineCaptionPosition? GetLCP(PdfName name) => name == null ? LineCaptionPosition.Inline : lcpCodes.GetKey(name);

        public static PdfName GetName(LineCaptionPosition? type) => type == null ? null : lcpCodes[type.Value];

        private SKPoint? captionOffset;
        private SKPoint? startPoint;
        private SKPoint? endPoint;

        private LineStartControlPoint cpStart;
        private LineEndControlPoint cpEnd;

        public Line(PdfPage page, SKPoint startPoint, SKPoint endPoint, string text, RGBColor color)
            : base(page, PdfName.Line, SKRect.Create(startPoint.X, startPoint.Y, endPoint.X - startPoint.X, endPoint.Y - startPoint.Y), text)
        {
            this[PdfName.L] = new PdfArrayImpl(4) { 0F, 0F, 0F, 0F };
            StartPoint = page.InvertRotateMatrix.MapPoint(startPoint);
            EndPoint = page.InvertRotateMatrix.MapPoint(endPoint);
            Color = color;
        }

        public Line(Dictionary<PdfName, PdfDirectObject> baseObject) : base(baseObject)
        { }

        public override string Contents
        {
            get => base.Contents;
            set
            {
                if (base.Contents != value)
                {
                    base.Contents = value;
                    QueueRefreshAppearance();
                }
            }
        }

        /// <summary>Gets/Sets whether the contents should be shown as a caption.</summary>
        [Pdf(VersionEnum.PDF16)]
        public bool CaptionVisible
        {
            get => GetBool(PdfName.Cap);
            set
            {
                var oldValue = CaptionVisible;
                if (oldValue != value)
                {
                    Set(PdfName.Cap, value);
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        [Pdf(VersionEnum.PDF17)]
        public LineCaptionPosition? CaptionPosition
        {
            get => GetLCP(Get<PdfName>(PdfName.CP));
            set
            {
                var oldValue = CaptionPosition;
                if (oldValue != value)
                {
                    this[PdfName.CP] = GetName(value);
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        [Pdf(VersionEnum.PDF17)]
        public SKPoint? CaptionOffset
        {
            get => captionOffset ??= Get<PdfArray>(PdfName.CO) is PdfArray offset
                    ? new SKPoint(offset.GetFloat(0), offset.GetFloat(1))
                    : new SKPoint(0F, 0F);
            set
            {
                var oldValue = CaptionOffset;
                if (oldValue != value)
                {
                    this[PdfName.CO] = value is SKPoint pValue
                        ? new PdfArrayImpl(2) { pValue.X, pValue.Y }
                        : null;
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        /// <summary>Gets/Sets the style of the starting line ending.</summary>
        [Pdf(VersionEnum.PDF14)]
        public LineEndStyleEnum StartStyle
        {
            get => Get<PdfArray>(PdfName.LE) is PdfArray endstylesObject
                  ? LineEndStyleEnumExtension.Get(endstylesObject.GetString(0))
                  : DefaultLineEndStyle;
            set
            {
                var oldValue = StartStyle;
                if (oldValue != value)
                {
                    EnsureLineEndStylesObject().SetName(0, value.GetName());
                    OnPropertyChanged(oldValue, value);
                    QueueRefreshAppearance();
                }
            }
        }

        /// <summary>Gets/Sets the style of the ending line ending.</summary>
        [Pdf(VersionEnum.PDF14)]
        public LineEndStyleEnum EndStyle
        {
            get => Get<PdfArray>(PdfName.LE) is PdfArray endstylesObject
                  ? LineEndStyleEnumExtension.Get(endstylesObject.GetString(1))
                  : DefaultLineEndStyle;
            set
            {
                var oldValue = EndStyle;
                if (oldValue != value)
                {
                    EnsureLineEndStylesObject().SetName(1, value.GetName());
                    OnPropertyChanged(oldValue, value);
                    QueueRefreshAppearance();
                }
            }
        }

        /// <summary>Gets/Sets the length of leader line extensions that extend
        /// in the opposite direction from the leader lines.</summary>
        [Pdf(VersionEnum.PDF16)]
        public double LeaderLineExtension
        {
            get => GetDouble(PdfName.LLE, DefaultLeaderLineExtension);
            set
            {
                var oldValue = LeaderLineExtension;
                if (oldValue != value)
                {
                    Set(PdfName.LLE, value);
                    // NOTE: If leader line extension entry is present, leader line MUST be too.
                    if (!ContainsKey(PdfName.LL))
                    {
                        LeaderLineLength = DefaultLeaderLineLength;
                    }
                    OnPropertyChanged(oldValue, value);
                    QueueRefreshAppearance();
                }
            }
        }

        [Pdf(VersionEnum.PDF17)]
        public double LeaderLineOffset
        {
            get => GetDouble(PdfName.LLO, DefaultLeaderLineOffset);
            set
            {
                var oldValue = LeaderLineOffset;
                if (oldValue != value)
                {
                    Set(PdfName.LLO, value);
                    // NOTE: If leader line extension entry is present, leader line MUST be too.
                    if (!ContainsKey(PdfName.LL))
                    {
                        LeaderLineLength = DefaultLeaderLineLength;
                    }
                    OnPropertyChanged(oldValue, value);
                    QueueRefreshAppearance();
                }
            }
        }

        /// <summary>Gets/Sets the length of leader lines that extend from each endpoint
        /// of the line perpendicular to the line itself.</summary>
        /// <remarks>A positive value means that the leader lines appear in the direction
        /// that is clockwise when traversing the line from its starting point
        /// to its ending point; a negative value indicates the opposite direction.</remarks>
        [Pdf(VersionEnum.PDF16)]
        public double LeaderLineLength
        {
            get => GetDouble(PdfName.LL, DefaultLeaderLineLength);
            set
            {
                var oldValue = LeaderLineOffset;
                if (oldValue != value)
                {
                    Set(PdfName.LL, value);
                    OnPropertyChanged(oldValue, value);
                    QueueRefreshAppearance();
                }
            }
        }

        public PdfArray LineData
        {
            get => Get<PdfArray>(PdfName.L);
            set
            {
                var oldValue = LineData;
                if (!PdfArray.SequenceEquals(oldValue, value))
                {
                    this[PdfName.L] = value;
                    if (startPoint != null)
                    {
                        startPoint = null;
                        endPoint = null;
                        QueueRefreshAppearance();
                    }
                }
            }
        }

        /// <summary>Gets/Sets the starting coordinates.</summary>
        public SKPoint StartPoint
        {
            get => startPoint ??= new SKPoint(LineData.GetFloat(0), LineData.GetFloat(1));
            set
            {
                var oldValue = StartPoint;
                if (oldValue != value)
                {
                    startPoint = value;
                    var coordinatesObject = LineData;
                    coordinatesObject.Set(0, value.X);
                    coordinatesObject.Set(1, value.Y);
                    OnPropertyChanged(coordinatesObject, coordinatesObject, nameof(LineData));
                    QueueRefreshAppearance();
                }
            }
        }


        /// <summary>Gets/Sets the ending coordinates.</summary>
        public SKPoint EndPoint
        {
            get => endPoint ??= new SKPoint(LineData.GetFloat(2), LineData.GetFloat(3));
            set
            {
                var oldValue = EndPoint;
                if (oldValue != value)
                {
                    endPoint = value;
                    var coordinatesObject = LineData;
                    coordinatesObject.Set(2, value.X);
                    coordinatesObject.Set(3, value.Y);
                    QueueRefreshAppearance();
                    OnPropertyChanged(LineData, LineData, nameof(LineData));
                }
            }
        }

        public override bool ShowToolTip => !CaptionVisible;

        public override bool AllowSize => false;

        public override void MoveTo(SKRect newBox)
        {
            var oldBox = Box;
            InvertBorderAndEffect(ref oldBox);
            InvertBorderAndEffect(ref newBox);
            if (oldBox.Width != newBox.Width
               || oldBox.Height != newBox.Height)
            {
                QueueRefreshAppearance();
            }
            var dif = SKMatrix.CreateIdentity()
                .PreConcat(SKMatrix.CreateTranslation(newBox.MidX, newBox.MidY))
                .PreConcat(SKMatrix.CreateScale(newBox.Width / oldBox.Width, newBox.Height / oldBox.Height))
                .PreConcat(SKMatrix.CreateTranslation(-oldBox.MidX, -oldBox.MidY));

            StartPoint = dif.MapPoint(StartPoint);
            EndPoint = dif.MapPoint(EndPoint);
            base.MoveTo(newBox);
        }

        private PdfArray EnsureLineEndStylesObject()
        {
            var endStylesObject = Get<PdfArray>(PdfName.LE);
            if (endStylesObject == null)
            {
                this[PdfName.LE] = endStylesObject = new PdfArrayImpl(2)
                {
                    PdfName.Get(DefaultLineEndStyle.GetName(), true),
                    PdfName.Get(DefaultLineEndStyle.GetName(), true)
                };
            }
            return endStylesObject;
        }

        protected override FormXObject GenerateAppearance()
        {
            var appearence = ResetAppearance(out var matrix);
            var paint = new PrimitiveComposer(appearence);
            paint.SetStrokeColor(Color ?? RGBColor.Default);
            paint.SetLineWidth(1);
            Border?.Apply(paint);
            paint.SetFillColor(InteriorColor ?? RGBColor.Default);


            var startPoint = matrix.MapPoint(StartPoint);
            var endPoint = matrix.MapPoint(EndPoint);
            var lineLength = SKPoint.Distance(startPoint, endPoint);
            var normal = SKPoint.Normalize(endPoint - startPoint);
            var invertNormal = new SKPoint(normal.X * -1, normal.Y * -1);
            if (CaptionVisible && !string.IsNullOrEmpty(Contents))
            {
                var fontName = appearence.GetDefaultFont(out var font);
                var textLength = (float)font.GetWidth(Contents, DefaultFontSize);

                var offset = (lineLength - textLength) / 2;

                var textLocation = startPoint + new SKPoint(normal.X * offset, normal.Y * offset);
                if (CaptionPosition == LineCaptionPosition.Inline)
                {
                    paint.StartPath(startPoint);
                    paint.DrawLine(startPoint + new SKPoint(normal.X * offset, normal.Y * offset));

                    paint.StartPath(endPoint);
                    paint.DrawLine(endPoint + new SKPoint(normal.X * -offset, normal.Y * -offset));
                }
                else
                {
                    paint.StartPath(startPoint);
                    paint.DrawLine(endPoint);
                }
                paint.Stroke();

                var horizontal = new SKPoint(1, 0);
                var theta = Math.Atan2(normal.X, normal.Y) - Math.Atan2(horizontal.X, horizontal.Y);

                while (theta <= -Math.PI)
                    theta += 2 * Math.PI;

                while (theta > Math.PI)
                    theta -= 2 * Math.PI;

                paint.BeginLocalState();
                paint.SetFillColor(RGBColor.Default);
                paint.SetFont(fontName, DefaultFontSize);
                paint.ShowText(Contents, textLocation, XAlignmentEnum.Left,
                    CaptionPosition == LineCaptionPosition.Inline ? YAlignmentEnum.Middle : YAlignmentEnum.Top,
                    MathUtils.ToDegrees(theta));
                paint.End();
            }
            else
            {
                paint.StartPath(startPoint);
                paint.DrawLine(endPoint);
                paint.Stroke();
            }
            if (StartStyle == LineEndStyleEnum.OpenArrow)
            {
                paint.AddOpenArrow(startPoint, normal);
                paint.Stroke();
            }
            else if (StartStyle == LineEndStyleEnum.ClosedArrow)
            {
                paint.AddClosedArrow(startPoint, normal);
                paint.FillStroke();
            }
            else if (StartStyle == LineEndStyleEnum.Circle)
            {
                paint.DrawCircle(startPoint, StyleRadius);
                paint.FillStroke();
            }

            if (EndStyle == LineEndStyleEnum.OpenArrow)
            {
                paint.AddOpenArrow(endPoint, invertNormal);
                paint.Stroke();
            }
            else if (EndStyle == LineEndStyleEnum.ClosedArrow)
            {
                paint.AddClosedArrow(endPoint, invertNormal);
                paint.FillStroke();
            }
            else if (EndStyle == LineEndStyleEnum.Circle)
            {
                paint.DrawCircle(endPoint, StyleRadius);
                paint.FillStroke();
            }

            paint.Flush();
            return appearence;
        }

        public override void RefreshBox()
        {
            if (LineData == null)
                return;
            var box = SKRect.Create(StartPoint, SKSize.Empty);
            box.Add(EndPoint);
            if (StartStyle != LineEndStyleEnum.None
                || EndStyle != LineEndStyleEnum.None
                || CaptionVisible)
            {
                box.Inflate(StyleRadius + 1, StyleRadius + 1);
            }
            ApplyBorderAndEffect(ref box);
            Box = box;
        }

        public override IEnumerable<ControlPoint> GetControlPoints()
        {
            yield return cpStart ??= new LineStartControlPoint { Annotation = this };
            yield return cpEnd ??= new LineEndControlPoint { Annotation = this };
        }

        public override PdfObject Clone(Cloner cloner)
        {
            var cloned = (Line)base.Clone(cloner);
            cloned.cpStart = null;
            cloned.cpEnd = null;
            return cloned;
        }

    }

    public enum LineCaptionPosition
    {
        Inline,
        Top
    }

    public abstract class LineControlPoint : ControlPoint
    {
        public Line Line => (Line)Annotation;
    }

    public class LineStartControlPoint : LineControlPoint
    {

        public override SKPoint GetPoint() => Line.StartPoint;

        public override void SetPoint(SKPoint point)
        {
            base.SetPoint(point);
            Line.StartPoint = point;
        }
    }

    public class LineEndControlPoint : LineControlPoint
    {
        public override SKPoint GetPoint() => Line.EndPoint;
        public override void SetPoint(SKPoint point)
        {
            base.SetPoint(point);
            Line.EndPoint = point;
        }
    }
}