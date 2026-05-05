/*
  Copyright 2006-2012 Stefano Chizzolini. http://www.pdfclown.org

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

using PdfGlown.Objects;
using SkiaSharp;
using System;

namespace PdfGlown.Documents.Contents.ColorSpaces
{
    /// <summary>Device Red-Green-Blue color value [PDF:1.6:4.5.3].</summary>
    [Pdf(VersionEnum.PDF11)]
    public sealed class RGBColor : DeviceColor, IEquatable<RGBColor>
    {
        public static readonly RGBColor Black = Get(SKColors.Black);
        public static readonly RGBColor White = Get(SKColors.White);
        public static readonly RGBColor Red = Get(SKColors.Red);
        public static readonly RGBColor Green = Get(SKColors.Green);
        public static readonly RGBColor Blue = Get(SKColors.Blue);
        public static readonly RGBColor Cyan = Get(SKColors.Cyan);
        public static readonly RGBColor Magenta = Get(SKColors.Magenta);
        public static readonly RGBColor Yellow = Get(SKColors.Yellow);
        public static readonly RGBColor OrangeRed = Get(SKColors.OrangeRed);

        public static readonly RGBColor Default = Black;

        /// <summary>Gets the color corresponding to the specified system color.</summary>
        /// <param name="color">System color to convert.</param>
        public static RGBColor Get(SKColor? color)
        {
            return (color.HasValue
              ? new RGBColor(color.Value.Red / 255d, color.Value.Green / 255d, color.Value.Blue / 255d)
              : Default);
        }

        public RGBColor(double r, double g, double b)
            : this(RGBColorSpace.Default, new PdfArrayImpl(3) {
                NormalizeComponent(r),
                NormalizeComponent(g),
                NormalizeComponent(b)
            })
        { }

        internal RGBColor(DeviceColorSpace colorSpace, PdfArray components)
            : base(colorSpace, components)
        { }

        /// <summary>Gets/Sets the red component.</summary>
        public float R
        {
            get => this[0];
            set => this[0] = value;
        }

        /// <summary>Gets/Sets the green component.</summary>
        public float G
        {
            get => this[1];
            set => this[1] = value;
        }

        /// <summary>Gets/Sets the blue component.</summary>
        public float B
        {
            get => this[2];
            set => this[2] = value;
        }

        public override bool IsZero => Equals(Black);

        public bool Equals(RGBColor other)
        {
            if (other == null)
                return false;
            return R.Equals(other.R)
                && G.Equals(other.G)
                && B.Equals(other.B);
        }

    }
}