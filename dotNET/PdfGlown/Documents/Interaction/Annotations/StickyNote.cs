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
using PdfGlown.Tools;
using PdfGlown.Util;
using SkiaSharp;
using System;
using System.Collections.Generic;

namespace PdfGlown.Documents.Interaction.Annotations
{
    /// <summary>Text annotation [PDF:1.6:8.4.5].</summary>
    /// <remarks>It represents a sticky note attached to a point in the PDF document.</remarks>
    [Pdf(VersionEnum.PDF10)]
    public sealed class StickyNote : Markup
    {
        public const int size = 28;

        private static readonly NoteImageEnum DefaultIconType = NoteImageEnum.Note;
        private static readonly bool DefaultOpen = false;
        private static readonly Dictionary<NoteImageEnum, PdfName> IconTypeEnumCodes;

        static StickyNote()
        {
            IconTypeEnumCodes = new Dictionary<NoteImageEnum, PdfName>
            {
                [NoteImageEnum.Comment] = PdfName.Comment,
                [NoteImageEnum.Help] = PdfName.Help,
                [NoteImageEnum.Insert] = PdfName.Insert,
                [NoteImageEnum.Key] = PdfName.Key,
                [NoteImageEnum.NewParagraph] = PdfName.NewParagraph,
                [NoteImageEnum.Note] = PdfName.Note,
                [NoteImageEnum.Paragraph] = PdfName.Paragraph
            };
        }

        /// <summary>Gets the code corresponding to the given value.</summary>
        private static PdfName ToCode(NoteImageEnum value) => IconTypeEnumCodes[value];

        /// <summary>Gets the icon type corresponding to the given value.</summary>
        private static NoteImageEnum ToIconTypeEnum(string value)
        {
            if (value == null)
                return DefaultIconType;
            foreach (KeyValuePair<NoteImageEnum, PdfName> iconType in IconTypeEnumCodes)
            {
                if (string.Equals(iconType.Value.StringValue, value, StringComparison.Ordinal))
                    return iconType.Key;
            }
            return DefaultIconType;
        }

        public StickyNote(PdfPage page, SKPoint location, string text)
            : base(page, PdfName.Text, SKRect.Create(location.X, location.Y, 0, 0), text)
        { }

        public StickyNote(Dictionary<PdfName, PdfDirectObject> baseObject) 
            : base(baseObject)
        { }

        /// <summary>Gets/Sets the icon to be used in displaying the annotation.</summary>
        public NoteImageEnum ImageName
        {
            get => ToIconTypeEnum(GetString(PdfName.Name));
            set
            {
                var oldValue = ImageName;
                if (oldValue != value)
                {
                    this[PdfName.Name] = (value != DefaultIconType ? ToCode(value) : null);
                    GenerateAppearance();
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        /// <summary>Gets/Sets whether the annotation should initially be displayed open.</summary>
        public bool IsOpen
        {
            get => GetBool(PdfName.Open, DefaultOpen);
            set
            {
                var oldValue = IsOpen;
                if (oldValue != value)
                {
                    Set(PdfName.Open, value != DefaultOpen ? value : null);
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        [Pdf(VersionEnum.PDF15)]
        public MarkupState? State
        {
            get => GetMarkupState(GetString(PdfName.State));
            set
            {
                var oldValue = State;
                if (oldValue != value)
                {
                    SetName(PdfName.State, GetMarkupStateCode(value));
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        public MarkupStateModel? StateModel
        {
            get => GetStateModel(GetString(PdfName.StateModel));
            set
            {
                var oldValue = StateModel;
                if (oldValue != value)
                {
                    SetName(PdfName.StateModel, GetCode(value));
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        public override bool AllowSize => false;

        protected override FormXObject GenerateAppearance()
        {
            SKRect bound = GetBound();
            var normalAppearance = ResetAppearance(bound, out var zeroMatrix);
            bound = zeroMatrix.MapRect(bound);
            var svg = SvgImage.GetCache(ImageName.ToString());
            var pathMatrix = SvgImage.GetMatrix(svg, bound, 2);
            using var tempPath = new SKPath();
            svg.Path.Transform(pathMatrix, tempPath);
            //var pathBounds = pathMatrix.MapRect(svg.Path.Bounds);
            //pathMatrix = pathMatrix.PostConcat(SKMatrix.CreateTranslation(0, bound.Height));
            var composer = new PrimitiveComposer(normalAppearance);
            composer.SetLineWidth(1);
            composer.SetStrokeColor(RGBColor.Default);
            composer.SetFillColor(Color ?? RGBColor.White);
            composer.DrawPath(tempPath);
            composer.FillStroke();
            composer.Flush();
            return normalAppearance;
        }

        protected override SKRect GetDrawBox()
        {
            return GetBound();
        }

        private SKRect GetBound()
        {
            if (Box.Width == 0 || Box.Height == 0)
            {
                SKPoint location = Box.Location;
                return Box = SKRect.Create(location + new SKPoint(0, -size), new SKSize(size, size));
            }
            return Box;
        }

        public override void SetBounds(SKRect value)
        {
            base.SetBounds(SKRect.Create(value.Left, value.Top, 0, 0));
        }

        public override SKRect GetViewBounds(SKMatrix viewMatrix)
        {
            base.GetViewBounds();
            var bound = GetBound();
            return viewMatrix.PreConcat(PageMatrix).MapRect(bound);
            //return SKRect.Create(bound.Left, bound.Top, size / Math.Abs(viewMatrix.ScaleX), size / Math.Abs(viewMatrix.ScaleY));
        }

        public override IEnumerable<ControlPoint> GetControlPoints()
        {
            yield break;
        }
    }

}