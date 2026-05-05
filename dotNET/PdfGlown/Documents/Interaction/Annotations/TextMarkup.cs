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

using PdfGlown.Documents.Contents;
using PdfGlown.Documents.Contents.ColorSpaces;
using PdfGlown.Documents.Contents.Composition;
using PdfGlown.Documents.Contents.XObjects;
using PdfGlown.Objects;
using PdfGlown.Util.Math;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PdfGlown.Documents.Interaction.Annotations
{
    /// <summary>Text markup annotation [PDF:1.6:8.4.5].</summary>
    /// <remarks>It displays highlights, underlines, strikeouts, or jagged ("squiggly") underlines in
    /// the text of a document.</remarks>
    [Pdf(VersionEnum.PDF13)]
    public sealed class TextMarkup : Markup
    {
        private static readonly Dictionary<TextMarkupType, PdfName> MarkupTypeEnumCodes;

        static TextMarkup()
        {
            MarkupTypeEnumCodes = new Dictionary<TextMarkupType, PdfName>
            {
                [TextMarkupType.Highlight] = PdfName.Highlight,
                [TextMarkupType.Squiggly] = PdfName.Squiggly,
                [TextMarkupType.StrikeOut] = PdfName.StrikeOut,
                [TextMarkupType.Underline] = PdfName.Underline
            };
        }

        /// <summary>Gets the code corresponding to the given value.</summary>
        private static PdfName ToCode(TextMarkupType value)
        {
            return MarkupTypeEnumCodes[value];
        }

        /// <summary>Gets the markup type corresponding to the given value.</summary>
        private static TextMarkupType ToMarkupTypeEnum(string value)
        {
            if (value == null)
                throw new Exception("Invalid markup type.");
            foreach (KeyValuePair<TextMarkupType, PdfName> markupType in MarkupTypeEnumCodes)
            {
                if (string.Equals(markupType.Value.StringValue, value, StringComparison.Ordinal))
                    return markupType.Key;
            }
            throw new Exception("Invalid markup type.");
        }

        private static readonly PdfName HighlightExtGStateName = PdfName.highlight;
        private IList<Quad> markupBoxes;
        private IList<Quad> pageMarkupBoxes;


        private static float GetMarkupBoxMargin(float boxHeight)
        {
            return boxHeight * .25f;
        }

        /// <summary>Creates a new text markup on the specified page, making it printable by default.
        /// </summary>
        /// <param name="page">Page to annotate.</param>
        /// <param name="markupBox">Quadrilateral encompassing a word or group of contiguous words in the
        /// text underlying the annotation.</param>
        /// <param name="text">Annotation text.</param>
        /// <param name="markupType">Markup type.</param>
        public TextMarkup(PdfPage page, Quad markupBox, string text, TextMarkupType markupType)
            : this(page, new List<Quad>() { markupBox }, text, markupType)
        { }

        /// <summary>Creates a new text markup on the specified page, making it printable by default.
        /// </summary>
        /// <param name="page">Page to annotate.</param>
        /// <param name="markupBoxes">Quadrilaterals encompassing a word or group of contiguous words in
        /// the text underlying the annotation.</param>
        /// <param name="text">Annotation text.</param>
        /// <param name="markupType">Markup type.</param>
        public TextMarkup(PdfPage page, IList<Quad> markupBoxes, string text, TextMarkupType markupType)
            : base(page, ToCode(markupType), markupBoxes[0].GetBounds(), text)
        {
            MarkupType = markupType;
            MarkupBoxes = markupBoxes;
            Printable = true;
        }

        public TextMarkup(Dictionary<PdfName, PdfDirectObject> baseObject) 
            : base(baseObject)
        { }

        public override PdfPage Page
        {
            get => base.Page;
            set
            {
                if (base.Page != value)
                {
                    base.Page = value;
                    if (PageMarkupBoxes.Count > 0)
                    {
                        QueueRefreshAppearance();
                    }
                }
            }
        }

        public PdfArray QuadPoints
        {
            get => Get<PdfArray>(PdfName.QuadPoints);
            set
            {
                var oldValue = QuadPoints;
                if (!PdfArray.SequenceEquals(oldValue, value))
                {
                    this[PdfName.QuadPoints] = value;
                    markupBoxes = null;
                    pageMarkupBoxes = null;
                    OnPropertyChanged(oldValue, value);
                    QueueRefreshAppearance();
                }
            }
        }

        /// <summary>Gets/Sets the quadrilaterals encompassing a word or group of contiguous words in the
        /// text underlying the annotation.</summary>
        public IList<Quad> PageMarkupBoxes
        {
            get => pageMarkupBoxes ??= GetMarkupBoxes();
            set
            {
                var quadPoints = new PdfArrayImpl();
                foreach (var quad in value)
                {
                    // NOTE: Despite the spec prescription, point 3 and point 4 MUST be inverted.
                    quadPoints.Add(quad.Point0.X); // x1.
                    quadPoints.Add(quad.Point0.Y); // y1.
                    quadPoints.Add(quad.Point1.X); // x2.
                    quadPoints.Add(quad.Point1.Y); // y2.
                    quadPoints.Add(quad.Point3.X); // x4.
                    quadPoints.Add(quad.Point3.Y); // y4.
                    quadPoints.Add(quad.Point2.X); // x3.
                    quadPoints.Add(quad.Point2.Y); // y3.
                }

                QuadPoints = quadPoints;
                pageMarkupBoxes = value;
                markupBoxes = null;
            }
        }

        public IList<Quad> MarkupBoxes
        {
            get => markupBoxes ??= TransformMarkupBoxes(PageMarkupBoxes, PageMatrix);
            set
            {
                PageMarkupBoxes = TransformMarkupBoxes(value, InvertPageMatrix);
                markupBoxes = value;
            }
        }

        /// <summary>Gets/Sets the markup type.</summary>
        public TextMarkupType MarkupType
        {
            get => ToMarkupTypeEnum(GetString(PdfName.Subtype));
            set
            {
                this[PdfName.Subtype] = ToCode(value);
                Color = value switch
                {
                    TextMarkupType.Highlight => new RGBColor(1, 1, 0),
                    TextMarkupType.Squiggly => new RGBColor(1, 0, 0),
                    _ => new RGBColor(0, 0, 0),
                };
            }
        }

        public override bool AllowSize => false;

        public override bool AllowDrag => false;

        public override void RefreshBox()
        {
            SKRect box = SKRect.Empty;
            foreach (var markupBox in PageMarkupBoxes)
            {
                if (box.IsEmpty)
                { box = markupBox.GetBounds(); }
                else
                { box = SKRect.Union(box, markupBox.GetBounds()); }
            }
            //NOTE: Box width is expanded to make room for end decorations (e.g. rounded highlight caps).
            float markupBoxMargin = GetMarkupBoxMargin(box.Height);
            box.Inflate(markupBoxMargin, 0);
            Box = box;
        }

        protected override FormXObject GenerateAppearance()
        {
            if (Page == null)
            {
                return null;
            }
            var normalAppearance = ResetAppearance(out var matrix);
            //SKRect box = Box;
            var composer = new PrimitiveComposer(normalAppearance);
            {
                var first = PageMarkupBoxes.FirstOrDefault();

                var markupType = MarkupType;
                switch (markupType)
                {
                    case TextMarkupType.Highlight:
                        {
                            ExtGState defaultExtGState;
                            {
                                var extGStates = normalAppearance.Resources.ExtGStates;
                                defaultExtGState = extGStates[HighlightExtGStateName];
                                if (defaultExtGState == null)
                                {
                                    if (extGStates.Count > 0)
                                    { extGStates.Clear(); }

                                    extGStates[HighlightExtGStateName] = defaultExtGState = new ExtGState(Document);
                                    defaultExtGState.AlphaShape = false;
                                    defaultExtGState.BlendMode = BlendModeEnum.Multiply;
                                }
                            }

                            composer.ApplyState(defaultExtGState);
                            composer.SetFillColor(Color ?? RGBColor.OrangeRed);
                            {
                                foreach (Quad markup in PageMarkupBoxes)
                                {
                                    var markupBox = Quad.Transform(markup, ref matrix);

                                    var sign = Math.Sign((markupBox.Point0 - markupBox.Point3).Y);
                                    sign = -(sign == 0 ? 1 : sign);

                                    float markupBoxHeight = markupBox.Height;
                                    float markupBoxMargin = GetMarkupBoxMargin(markupBoxHeight) * sign;

                                    composer.DrawCurve(
                                      markupBox.Point3,
                                      markupBox.Point0,
                                      GetMarginPoint(markupBox.Point0, markupBox.Point3, -markupBoxMargin),
                                      GetMarginPoint(markupBox.Point3, markupBox.Point0, -markupBoxMargin));
                                    composer.DrawLine(markupBox.Point1);
                                    composer.DrawCurve(
                                      markupBox.Point2,
                                      GetMarginPoint(markupBox.Point2, markupBox.Point1, markupBoxMargin),
                                      GetMarginPoint(markupBox.Point1, markupBox.Point2, markupBoxMargin));
                                    composer.Fill();
                                }
                            }
                        }
                        break;
                    case TextMarkupType.Squiggly:
                        {
                            composer.SetStrokeColor(Color ?? RGBColor.OrangeRed);
                            composer.SetLineCap(LineCapEnum.Round);
                            composer.SetLineJoin(LineJoinEnum.Round);
                            {
                                foreach (Quad markup in PageMarkupBoxes)
                                {
                                    var markupBox = Quad.Transform(markup, ref matrix);
                                    var sign = Math.Sign((markupBox.Point0 - markupBox.Point3).Y);
                                    sign = sign == 0 ? 1 : sign;

                                    float markupBoxHeight = markupBox.Height;
                                    float markupBoxWidth = markupBox.Width;
                                    float lineWidth = markupBoxHeight * .06f;
                                    float step = markupBoxHeight * .125f;
                                    float length = (float)Math.Sqrt(Math.Pow(step, 2) * 2);
                                    var bottomUp = SKPoint.Normalize(markupBox.Point3 - markupBox.Point0);
                                    bottomUp = new SKPoint(bottomUp.X * lineWidth, bottomUp.Y * lineWidth);
                                    var startPoint = markupBox.Point3 - (new SKPoint(bottomUp.X * 2, bottomUp.Y * 2));
                                    var leftRight = SKPoint.Normalize(markupBox.Point2 - markupBox.Point3);
                                    leftRight = new SKPoint(leftRight.X * step, leftRight.Y * step);
                                    var leftRightPerp = leftRight.GetPerp(step * sign);
                                    var stepPoint = startPoint + leftRight + leftRightPerp;
                                    bool phase = true;
                                    composer.SetLineWidth(lineWidth);
                                    composer.StartPath(startPoint);
                                    startPoint += leftRight;
                                    var x = 0F;
                                    while (x < markupBoxWidth)
                                    {
                                        composer.DrawLine(phase ? stepPoint : startPoint);
                                        startPoint += leftRight;
                                        stepPoint += leftRight;
                                        x += step;
                                        phase = !phase;
                                    }
                                }
                                composer.Stroke();
                            }
                        }
                        break;
                    case TextMarkupType.StrikeOut:
                    case TextMarkupType.Underline:
                        {
                            composer.SetStrokeColor(Color ?? RGBColor.OrangeRed);
                            {
                                float lineYRatio = 0;
                                switch (markupType)
                                {
                                    case TextMarkupType.StrikeOut:
                                        lineYRatio = -.5f;
                                        break;
                                    case TextMarkupType.Underline:
                                        lineYRatio = -.05f;
                                        break;
                                    default:
                                        throw new NotImplementedException();
                                }
                                foreach (Quad markup in PageMarkupBoxes)
                                {
                                    var markupBox = Quad.Transform(markup, ref matrix);
                                    float markupBoxHeight = markupBox.Height;
                                    float boxYOffset = markupBoxHeight * lineYRatio;
                                    var normal = SKPoint.Normalize(markupBox.Point3 - markupBox.Point0);
                                    normal = new SKPoint(normal.X * boxYOffset, normal.Y * boxYOffset);
                                    composer.SetLineWidth(markupBoxHeight * .065);
                                    composer.DrawLine(markupBox.Point3 + normal, markupBox.Point2 + normal);
                                }
                                composer.Stroke();
                            }
                        }
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
            composer.Flush();
            return normalAppearance;
        }

        private static SKPoint GetMarginPoint(SKPoint pointA, SKPoint pointB, float margin)
        {
            float absMargin = Math.Abs(margin);
            var normal = SKPoint.Normalize(pointA - pointB);
            normal = new SKPoint(normal.X * absMargin, normal.Y * absMargin);
            var perp = normal.GetPerp(margin);
            return (pointB + normal) + perp;
        }

        private List<Quad> TransformMarkupBoxes(IList<Quad> source, SKMatrix matrix)
        {
            var list = new List<Quad>(source.Count);
            foreach (var quad in source)
            {
                list.Add(Quad.Transform(quad, ref matrix));
            }
            return list;
        }

        private IList<Quad> GetMarkupBoxes()
        {
            var list = new List<Quad>();
            var quadPoints = QuadPoints;
            if (quadPoints != null)
            {
                var length = quadPoints.Count;

                for (int index = 0; index < length; index += 8)
                {
                    /// NOTE: Despite the spec prescription, point 3 and point 4 MUST be inverted.
                    var quad = new Quad(
                        new SKPoint(quadPoints.GetFloat(index), quadPoints.GetFloat(index + 1)),
                        new SKPoint(quadPoints.GetFloat(index + 2), quadPoints.GetFloat(index + 3)),
                        new SKPoint(quadPoints.GetFloat(index + 6), quadPoints.GetFloat(index + 7)),
                        new SKPoint(quadPoints.GetFloat(index + 4), quadPoints.GetFloat(index + 5)));
                    list.Add(quad);
                }
            }
            return list;
        }
    }

    /// <summary>Markup type [PDF:1.6:8.4.5].</summary>
    public enum TextMarkupType
    {
        /// <summary>Highlight.</summary>
        [Pdf(VersionEnum.PDF13)]
        Highlight,
        /// <summary>Squiggly.</summary>
        [Pdf(VersionEnum.PDF14)]
        Squiggly,
        /// <summary>StrikeOut.</summary>
        [Pdf(VersionEnum.PDF13)]
        StrikeOut,
        /// <summary>Underline.</summary>
        [Pdf(VersionEnum.PDF13)]
        Underline
    };
}