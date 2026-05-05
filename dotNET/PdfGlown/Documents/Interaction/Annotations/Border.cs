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
using PdfGlown.Documents.Contents.Composition;
using PdfGlown.Objects;
using SkiaSharp;
using System;
using System.Collections.Generic;

namespace PdfGlown.Documents.Interaction.Annotations
{
    /// <summary>Border characteristics [PDF:1.6:8.4.3].</summary>
    [Pdf(VersionEnum.PDF11)]
    public sealed class Border : PdfDictionary, IEquatable<Border>
    {
        private static readonly LineDash DefaultLineDash = new(new float[] { 3, 1 });
        private static readonly BorderStyleType DefaultStyle = BorderStyleType.Solid;
        private static readonly double DefaultWidth = 1;

        private static readonly Dictionary<BorderStyleType, PdfName> StyleEnumCodes = new()
        {
            [BorderStyleType.Solid] = PdfName.S,
            [BorderStyleType.Dashed] = PdfName.D,
            [BorderStyleType.Beveled] = PdfName.B,
            [BorderStyleType.Inset] = PdfName.I,
            [BorderStyleType.Underline] = PdfName.U
        };
        private LineDash lineDash;
        

        /// <summary>Gets the code corresponding to the given value.</summary>
        private static PdfName GetName(BorderStyleType value)
        {
            return StyleEnumCodes[value];
        }

        /// <summary>Gets the style corresponding to the given value.</summary>
        private static BorderStyleType ToStyleEnum(string value)
        {
            if (value == null)
                return DefaultStyle;
            foreach (KeyValuePair<BorderStyleType, PdfName> style in StyleEnumCodes)
            {
                if (string.Equals(style.Value.StringValue, value, StringComparison.Ordinal))
                    return style.Key;
            }
            return DefaultStyle;
        }
        
        public Border()
            : this(1D)
        { }

        /// <summary>Creates a non-reusable instance.</summary>
        public Border(double width) 
            : this(null, width)
        { }

        /// <summary>Creates a non-reusable instance.</summary>
        public Border(double width, BorderStyleType style) 
            : this(null, width, style)
        { }

        /// <summary>Creates a non-reusable instance.</summary>
        public Border(double width, LineDash pattern) 
            : this(null, width, pattern)
        { }

        /// <summary>Creates a reusable instance.</summary>
        public Border(PdfDocument context, double width) 
            : this(context, width, DefaultStyle, null)
        { }

        /// <summary>Creates a reusable instance.</summary>
        public Border(PdfDocument context, double width, BorderStyleType style) 
            : this(context, width, style, null)
        { }

        /// <summary>Creates a reusable instance.</summary>
        public Border(PdfDocument context, double width, LineDash pattern) 
            : this(context, width, BorderStyleType.Dashed, pattern)
        { }

        private Border(PdfDocument context, double width, BorderStyleType style, LineDash pattern)
            : base(context, new Dictionary<PdfName, PdfDirectObject>(4) {
                { PdfName.Type, PdfName.Border }
            })
        {
            Width = width;
            Style = style;
            Pattern = pattern;
        }

        internal Border(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        /// <summary>Gets/Sets the dash pattern used in case of dashed border.</summary>
        public LineDash Pattern
        {
            get => lineDash ??= (Get<PdfArray>(PdfName.D) is PdfArray dashObject ? LineDash.Get(dashObject, null) : DefaultLineDash);
            set
            {
                if (Pattern != value)
                {
                    lineDash = value;
                    this[PdfName.D] = value != null ? new PdfArrayImpl(value.DashArray) : null;
                }
            }
        }

        /// <summary>Gets/Sets the border style.</summary>
        public BorderStyleType Style
        {
            get => ToStyleEnum(GetString(PdfName.S));
            set => this[PdfName.S] = value != DefaultStyle ? GetName(value) : null;
        }

        /// <summary>Gets/Sets the border width in points.</summary>
        public double Width
        {
            get => GetDouble(PdfName.W, DefaultWidth);
            set => Set(PdfName.W, value);
        }

        public void Apply(SKPaint paint, BorderEffect borderEffect)
        {
            paint.Style = SKPaintStyle.Stroke;
            paint.StrokeWidth = (float)Width;
            paint.IsAntialias = true;
            if (Style == BorderStyleType.Dashed)
            {
                Pattern?.Apply(paint);
            }
            borderEffect?.Apply(paint);
        }

        public void Apply(PrimitiveComposer paint)
        {
            paint.SetLineWidth((float)Width);

            if (Style == BorderStyleType.Dashed)
            {
                Pattern?.Apply(paint);
            }
        }

        public void Apply(ref SKRect box)
        {
            var indent = (float)Width;
            box.Inflate(indent, indent);
        }

        public void Invert(ref SKRect box)
        {
            var indent = (float)Width;
            var doubleIndent = indent + indent;
            if (box.Width > doubleIndent && box.Height > doubleIndent)
                box.Inflate(-indent, -indent);
        }

        public bool Equals(Border other)
        {
            if (other == null)
                return false;
            return Width == other.Width
                && Style == other.Style
                && Pattern.Equals(other.Pattern);
        }

    }

    /// <summary>Border style [PDF:1.6:8.4.3].</summary>
    public enum BorderStyleType
    {
        /// <summary>Solid.</summary>
        Solid,
        /// <summary>Dashed.</summary>
        Dashed,
        /// <summary>Beveled.</summary>
        Beveled,
        /// <summary>Inset.</summary>
        Inset,
        /// <summary>Underline.</summary>
        Underline
    };
}